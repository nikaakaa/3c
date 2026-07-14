# Design: Tree / State 分层打断生命周期

## 目标

- Self / LowerPriority abort 能递归停止任意 child subtree。
- child 包含 SMNode 时，active State Root 停止并执行 OnExit。
- OnExit 可跨 Tick，父 Composite 在完成前不启动 replacement。
- Action、Timeline、动画和网络继续由各自层处理，不由 Tree edge 越层操作。
- State 间输入/window 抢占与 Tree 到 SMNode abort 共用 source exit 内核。

## 当前模型

```text
State = None | Running | Success | Failure

UpdateNode:
  not Running -> OnStart
  Running -> OnUpdate
  Success/Failure -> OnStop

StopNode:
  Running -> OnStop
  State = None
```

问题：

- `OnStop` 无法区分自然完成和外部 abort。
- `StopNode` 是同步 void，不能表达 pending cleanup。
- Selector 停止 child 后立即启动 replacement。
- SMNode 的 OnStop 直接 hard stop runtime，跳过 State.OnExit。
- Action lifecycle 不能从结构停止可靠收口。

## 运行模型

### 行为结果

`State` 保持 `None`、`Running`、`Success`、`Failure`，只回答节点本次行为结果。

### 生命周期阶段

新增非序列化 runtime phase：

```text
Dormant
Active
Stopping
```

- `Dormant`：未运行、自然 terminal 或已停止。
- `Active`：允许正常 Update。
- `Stopping`：只允许推进停止生命周期，不允许正常 Update。

停止完成后 phase 回到 Dormant；外部 stop 的行为 State 回到 None，自然完成保留 Success/Failure 供父节点读取。

### StopContext

```text
NodeStopContext
  OriginCause
  LocalLogicTick
  InitiatorEdgeGuid
  InitiatorNodeGuid
  SourceEdgeGuid
  SourceNodeGuid
  ReplacementEdgeGuid
  ReplacementNodeGuid
  ImmediateParentNodeGuid
  PropagationDepth
```

`OriginCause` 包含 `SelfAbort`、`LowerPriorityAbort`、`ExplicitParentStop`、`StateTransition`、`Reset`、`Shutdown`。它从 initiator 到最深 descendant 始终不变；容器传播时只更新 immediate parent、当前 source 和 propagation depth。Context 是 runtime transient data，不写入 asset、Pipeline Blackboard 或网络协议。

### StopStatus

```text
Running
Completed
Failed
```

`Running` 表示父节点下个逻辑 Tick继续调用 stop update，不是 BT `State.Running`。

## RunnableNode API

正式入口：

```text
UpdateNode()
RequestStop(context)
UpdateStopping()
ForceStop(context)
ResetNode()
```

虚方法：

```text
OnStart()
OnUpdate()
OnCompleted(result)
OnStopRequested(context)
OnStopping(context)
OnStopped(context)
OnForceStopped(context)
OnReset()
```

规则：

- `OnUpdate` 返回 Success/Failure 时调用 `OnCompleted`，不调用 stop lifecycle。
- `RequestStop` 只允许 Active 节点进入 Stopping；Dormant 节点直接 Completed。
- 默认 `OnStopRequested` 完成同步清理并返回 Completed。
- 返回 Running 时保存 context，后续只调用 `OnStopping`。
- 完成时调用一次 `OnStopped`，State=None，phase=Dormant。
- Failed 时调用一次 `OnStopped` 并保留 failure debug；父调度器不得启动 replacement。
- `ForceStop` 不等待 graceful callbacks，递归释放后回到 Dormant。
- 删除旧 `StopNode/OnStop` 入口，不保留 alias。

自然完成与外部 stop 分开后，Timeline 成功不再经过“取消 playback”回调；外部 abort 才取消。

## Composite 调度

### Selector Self Abort

```text
current edge condition false
  -> RequestStop(current, SelfAbort)
  -> Completed: 本 Tick重新扫描所有 slots
  -> Running: 保存 pending child，本 Tick返回 Running
  -> Failed: Selector Failure，不启动 replacement
```

### Selector LowerPriority Abort

```text
higher-priority edge with LowerPriority/Both becomes true
  -> RequestStop(current lower child, LowerPriorityAbort)
  -> Completed: 本 Tick重新扫描并选择当前最高优先级合法 child
  -> Running: pending stop
  -> Failed: Selector Failure
```

停止完成后必须重新扫描，因为原高优先级条件可能已经失效。旧 child 一旦进入 Stopping 不得恢复正常 Update。

### Sequence

- running child 的 Self 条件失效时请求停止。
- pending stop 期间 Sequence 返回 Running。
- stop Completed 后 Sequence 返回 Failure，由父层决定后续路径。

### Parallel

- 单个 child Self abort 时只等待该 child 停止，其余 active child MAY 继续正常 tick。
- Parallel 自身收到 ParentAbort 时向所有 active child 请求停止。
- Parallel 只有在全部 child Completed 后才报告 stop Completed。
- 任一 child stop Failed 时 Parallel stop Failed，不启动外层 replacement。

### Composite 自身被停止

Composite 将 StopContext 传播给 active descendants，保留原始 OriginCause 和 initiator identity，只更新 immediate parent、当前 source 和 propagation depth。未运行 child 不执行 stop callback。

## 节点传播

### 普通叶节点

默认同步 Completed，只清理自身 runtime 资源。

### Decorator / Root / SubTree

停止自己的 active child，并在 child pending 时等待。SubTree runtime 不再直接硬切内部 Root。

### TimelineNode

- 自然 playback `Succeeded`：返回 Success，走 `OnCompleted`，不发 cancel。
- graceful stop：取消 active playback handle，返回 Completed。
- ForceStop：直接取消/释放 handle，不提交 Action lifecycle。

### StateMachineNode

- 自然命中 StateMachine Exit：返回 Success。
- Parent Tree stop：调用 `StateMachineGraphRuntime.RequestExit(stopContext)`。
- pending State.OnExit：返回 stop Running。
- State exit 完成并 owner release：返回 stop Completed。
- ForceStop：直接停止 active State、释放 owner，不运行 OnExit。

## StateMachine Source Exit 内核

```text
BeginSourceExit(source, optional target, exitContext)
  -> StateBehaviorSubTree.StopStateRootForExit
  -> Update State.OnExit
  -> stop/dispose source state runtime
  -> target exists: publish owner transition, enter target
  -> target missing: publish owner release, SM stop completed
```

State Transition 的 target 非空；Parent Tree abort 的 target 为空。两者不得复制两套 State.OnExit 代码。

### StateExitContext

```text
StateExitContext
  Cause
  SourceStateGuid
  TargetStateGuid optional
  TransitionEdgeGuid optional
  ParentSourceEdgeGuid optional
  ReplacementNodeGuid optional
  LocalLogicTick
```

State runtime 在执行 OnExit scope 时通过正式 runtime access 暴露该 context。ConditionRuleGraph 使用纯 reader 查询，OnExit 行为节点不直接访问 Selector 实例。

## Action OnExit

新增纯 reader：

- `StateExitCauseInfoNode`
- `ActionContextActiveInfoNode`
- `ActionWindowActiveInfoNode`

只提供 typed value，组合继续使用 Equal、And、Or、Not。

```text
OnExit
-> Selector
   -> [ActionContext Active AND StateTransition AND ComboWindow]
      Submit Cancel(ComboWindow)
   -> [ActionContext Active AND Tree Abort]
      Submit Abort(TreeAbort)
   -> [Unconditional]
      Succeed
```

本次 Corin 资产只落地 combo Cancel；TreeAbort 分支能力进入 runtime/authoring 合同，不凭空创建高优先级测试 branch。

正常 root 已提交 Complete 时 Action Context 不再 active，OnExit 走 Succeed，避免第二条 terminal transition。

## Timeline Tick Barrier

```text
Logic Tick Begin
  -> PrepareDecisionFacts(active playback)
  -> RootTree Update
      -> Composite abort / State Transition
      -> RequestStop propagation
      -> Timeline cancel status
      -> OnExit lifecycle facts
      -> replacement 仅在 stop barrier 完成后启动
  -> CommitTimelines
      -> 先处理 cancelled playback
      -> 提交 decision window 一次
      -> 只推进存活旧 playback 和本 Tick新 playback
```

`PrepareDecisionFacts` 只预采样 ActionWindow，不修改 time/cycle/presentation segment，不提交 motion、cue、camera、animation，并使用与正式推进相同的时间段计算。

取消发生后：

- 决策所用 Window 可作为本 Tick已发生事实提交一次。
- 旧 playback 不提交本 Tick非决策贡献。
- outgoing pose 只来自上一正式 presentation plan，不继续 tick 旧 Timeline。

## Corin State Transition

Locomotion 条件区间：

```text
Stop = MoveMagnitude < MoveThreshold
Walk = MoveMagnitude >= MoveThreshold AND MoveMagnitude < RunThreshold
Run  = MoveMagnitude >= RunThreshold
Turn = Run AND AbsTurnAngle >= MovingTurnAngleThreshold
```

- 输入抢占边优先于 root completed 边。
- completed 边同时约束当前输入区间。
- 不新增 Locomotion 专用条件节点。
- WalkEnd 没有独立动画时不创建伪 Timeline，由 Transition blend 处理。

Attack 条件：

```text
Attack1 -> Attack2 = Attack1Cancel active AND HasRequest(Attack)
Attack2 -> Attack1 = Attack2Cancel active AND HasRequest(Attack)
```

request query 不消费，target activation 是唯一消费点。

## Debug

Debug 至少显示 Node lifecycle phase、initiating stop cause、pending child、source/replacement identity、stop elapsed ticks/status、active/exiting/target State、StateExitContext、Timeline terminal status 和 Action terminal lifecycle。Debug 只读 runtime，不参与条件求值。

## Failure Contract

- stop Failed 后旧 child 不恢复 normal Update。
- Composite 不启动 replacement，向父级返回 Failure 并记录完整 context。
- 不自动 ForceStop 后继续新 branch。
- Shutdown 可独立调用 ForceStop 释放剩余资源。
- OnExit 不得等待 animation blend、网络 ACK 或已取消 Timeline 完成。

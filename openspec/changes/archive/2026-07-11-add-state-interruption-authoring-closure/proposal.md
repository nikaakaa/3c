# Proposal: 重构 Tree / State 分层打断生命周期

## Why

当前 `RunnableNode` 只有 `None`、`Running`、`Success`、`Failure` 四个行为结果，并用同一个无参数、无返回值的 `OnStop()` 同时处理自然完成、Self abort、LowerPriority abort、父节点停止、State Transition、Reset 和 Shutdown。`CompositeNode.StopSlot()` 会同步调用 child `StopNode()`，随后 Selector 可在同一个 Tick 立即启动替代 child。

这个模型能递归释放普通节点和 Timeline，但遇到 `StateMachineNode` 时只会调用 `StateMachineGraphRuntime.Stop()` 硬释放 active State，不会执行 `StateBehaviorSubTree.OnExit`。因此上层 Tree 的 Self / LowerPriority abort 可以停止攻击 Timeline，却可能跳过 Action lifecycle，留下仍 active 的 ActionInstance。当前节点也无法表达“正在停止”，父 Composite 无法等待跨 Tick 的 State.OnExit。

StateMachine 内部 Transition 另有一条较完整的退出链路：停止 source root、运行 OnExit、进入 target。Corin 资产仍缺少 RunEnd 等一次性移动状态的输入抢占边，Attack Timeline 的 CancelWindow 也无法在同 Tick参与条件决策。只补 State 间 Transition 不能解决 Tree abort 到 SMNode 的生命周期缺口。

本变更将两者收敛为严格分层的一条正式协议：BT 调度层只决定抢占，Runnable 生命周期层传播可等待 stop，SMNode 将 stop 翻译成 State exit，State.OnExit 显式产生 Action terminal lifecycle，Timeline 只取消 playback，Presentation 只处理 outgoing pose 和 blend。

## What Changes

### Runnable 生命周期

- 保留 `State.None/Running/Success/Failure` 作为行为结果，不向其中加入 `Stopping`。
- 新增独立 `NodeLifecyclePhase`，至少区分 `Dormant`、`Active`、`Stopping`。
- 新增 `NodeStopContext`，携带稳定不变的 `OriginCause`（SelfAbort、LowerPriorityAbort、ExplicitParentStop、StateTransition、Reset、Shutdown）、传播深度，以及 initiator/source/replacement edge/node identity 和 tick。
- 新增 `NodeStopStatus`，至少包含 `Running`、`Completed`、`Failed`。
- 将自然 `Success/Failure` 与外部 stop 分开：自然完成进入 `OnCompleted(result)`；外部停止进入 `RequestStop(context) -> UpdateStopping() -> OnStopped(context)`。
- 新增 `ForceStop(context)`，只用于 Shutdown、Dispose 和强制 Reset。
- 删除语义模糊的旧 `StopNode()/OnStop()` 正式路径并迁移全部调用点，不保留兼容 alias。
- descendant 收到 stop 时 MUST 保留最初 OriginCause，只更新 immediate parent 和 propagation depth，不得把 LowerPriority 等真实来源覆盖成模糊 ParentAbort。
- 不为 Self、LowerPriority、ActionCancel 等原因增加专用回调；所有节点只接收统一 StopContext。

### Composite 抢占

- Self abort 在当前 child 条件失效时请求停止该 child；LowerPriority abort 在更高优先级条件成立时请求停止当前低优先级 child。
- child 立即完成 stop 时，Selector MAY 在同 Tick重新扫描并启动当前最高优先级合法 child。
- child 返回 `Running` 时，Composite MUST 进入 pending stop，不再 tick 旧 child 正常逻辑，也不得提前 tick replacement。
- stop 完成后重新扫描条件，不盲目进入停止开始时记录的旧候选。
- Sequence、Selector、Parallel、Decorator 和 SubTree 使用同一协议传播；Parallel 等待所有 active child 完成 stop。
- stop `Failed` 时 Composite 报告明确失败并禁止启动 replacement，不使用 force-stop fallback 偷偷继续。

### StateMachine 翻译层

- `StateMachineNode` 收到 Tree stop request 时，不再直接硬停 runtime，而是请求一次没有 target State 的 graceful State exit。
- `StateMachineGraphRuntime` 统一 State Transition 和父 Tree abort 的 source exit 内核：先停止 State Root，再运行 `StateBehaviorSubTree.OnExit`，最后发布 owner transition 或 owner release。
- State Transition 完成后进入 target；Tree abort 完成后 SMNode 整体 StopCompleted，不进入 target。
- 新增只读 `StateExitContext`，包含退出来源、source State、可选 target State、可选 Transition edge、父 Tree source/replacement identity。
- OnExit 通过纯 `StateExitCauseInfoNode`、Action Context reader 和通用 Equal/And/Or/Not 组合业务分支，不给每种打断新增专用节点。

### Action、Timeline 与表现

- Tree abort 和 State Transition 本身不得自动生成 Action lifecycle；持有 Action Context 的 State 必须在 OnExit 显式提交 `Complete`、`Cancel`、`Interrupt` 或 `Abort`。
- TimelineNode 收到 stop request 时取消 active playback 并立即完成 Node stop；它不解释 Action lifecycle。
- Presentation 不等待动画淡出才允许逻辑 stop 完成；旧逻辑关闭后可使用已有 outgoing playback plan 和 edge blend 继续表现过渡。
- Timeline scheduler 在 RootTree 前只准备无副作用的当前 Tick ActionWindow 决策事实，RootTree 后先处理 cancel，再提交存活 playback 的 motion/cue/camera/animation。
- 被抢占旧 branch 不得在 replacement 启动后继续产生 gameplay facts。

### Corin Authoring

- 补齐 WalkStart、WalkEnd、RunStart、RunEnd、RunLoop、MovingTurn 的输入抢占矩阵。
- 普通移动条件只使用 MoveMagnitude、Pipeline ExposedProperty 阈值、Compare、And、Or、Not 和状态运行事实。
- Attack1 -> Attack2 与 Attack2 -> Attack1 使用当前 Tick CancelWindow 和非消费 Attack request。
- Attack combo source OnExit 在 Action Context 仍 active 时提交 `Cancel(ComboWindow)`；正常 Complete 后走无操作成功分支。
- Corin RootTree 继续只表达主流程，不加入测试用高优先级打断 branch，也不创建一次性 SubTree/ConditionRuleGraph asset。

## Layer Contract

```text
BT Condition / AbortPolicy
  -> 决定谁被抢占
NodeStopContext
  -> 传播结构停止和完成屏障
StateExitContext
  -> SMNode 翻译 active State 退出
ActionLifecycleTransition
  -> State.OnExit 显式关闭业务事务
Timeline Cancel
  -> TimelineNode 释放 playback
Animation Owner Release / Blend
  -> Presentation 保留视觉连续性
SyncFacts / Network Adapter
  -> 只发送最终业务事实
```

上层 Tree edge MUST NOT 直接提交 Action lifecycle、取消 Timeline 或写网络事实。SMNode MUST NOT 自动猜测 Tree abort 对应 `Cancel`、`Interrupt` 还是 `Abort`。

## Corin Target Matrix

### Locomotion

- `Idle -> WalkStart`：Walk 区间。
- `Idle -> RunStart`：Run 区间。
- `WalkStart -> RunStart`：输入提升到 Run 区间。
- `WalkStart -> WalkEnd`：输入回到 Stop 区间。
- `WalkStart -> WalkLoop`：root 完成且仍为 Walk 区间。
- `WalkLoop -> RunStart`：输入提升到 Run 区间。
- `WalkLoop -> WalkEnd`：输入回到 Stop 区间。
- `WalkEnd -> RunStart`：输入恢复到 Run 区间。
- `WalkEnd -> WalkStart`：输入恢复到 Walk 区间。
- `WalkEnd -> Idle`：root 完成且仍为 Stop 区间。
- `RunStart -> RunEnd`：输入回到 Stop 区间。
- `RunStart -> WalkLoop`：输入下降到 Walk 区间。
- `RunStart -> RunLoop`：root 完成且仍为 Run 区间。
- `RunLoop -> RunEnd`：输入回到 Stop 区间。
- `RunLoop -> WalkLoop`：输入下降到 Walk 区间。
- `RunLoop -> MovingTurn`：保持 Run 且转角超过阈值。
- `RunEnd -> RunStart`：输入恢复到 Run 区间。
- `RunEnd -> WalkStart`：输入恢复到 Walk 区间。
- `RunEnd -> Idle`：root 完成且仍为 Stop 区间。
- `MovingTurn -> RunEnd`：输入回到 Stop 区间。
- `MovingTurn -> WalkLoop`：输入下降到 Walk 区间。
- `MovingTurn -> RunLoop`：root 完成且仍为 Run 区间。

### Action

- `None -> Attack1`：存在 Attack request，target 激活节点消费。
- `Attack1 -> Attack2`：`Attack1Cancel` active 且存在 Attack request。
- `Attack2 -> Attack1`：`Attack2Cancel` active 且存在 Attack request。
- `Attack1/Attack2 -> None`：source root 正常完成。

## Decisions and Tradeoffs

### 行为 State 与生命周期 Phase 分离

业务取舍：保持 Composite 只解释四种行为结果，避免 `Stopping` 成为第五种 BT 返回值污染所有节点；代价是 runtime 多维护一份 phase。采用该方案。

### 全部 RunnableNode 使用统一 stop 协议

业务取舍：相比只给 SMNode 增加 graceful interface，改动面更大；收益是 Composite、SubTree、Decorator、Parallel 和未来跨 Tick 节点不形成第二套停止路径。采用统一协议，不保留旧 StopNode 兼容入口。

### 等待逻辑退出屏障后再启动 replacement

业务取舍：OnExit 返回 Running 时高优先级 branch 会等待；收益是新旧 branch 不会同时持有 Action、motion 和窗口输出。采用等待逻辑退出。动画淡出属于 Presentation，不阻塞该屏障。

### 保留 ForceStop

业务取舍：Shutdown 时运行 gameplay OnExit 可能跨 Tick、发出无意义网络事实或重新激活节点；保留独立强制释放语义，但只允许 Shutdown、Dispose、强制 Reset 使用。它不是 gameplay abort fallback。

### 不在 BT Edge 配业务退出效果

业务取舍：Edge 直接配置 Action Cancel 响应快且直观，但会越过 SMNode/State.OnExit，使父树知道 child 内部 Action Context。拒绝该方案，Edge 只保留条件、AbortPolicy、flow order 等调度数据。

### Timeline 决策事实预采样

业务取舍：整体提前推进 Timeline 会让被输入抢占的 RunEnd 先提交一帧旧 motion；接受 scheduler 两阶段复杂度，换取同 Tick Window 决策和取消后无旧贡献。

## Out of Scope

- 不加入 Dodge、HitReaction、硬控、霸体、免疫或具体全局高优先级 branch。
- 不引入 GAS tag hierarchy、Ability Group 或并发 Action 框架。
- 不新增 Action 专用 BT edge 字段、网络 packet 或黑板打断 Bool。
- 不让 OnExit 等待动画播放、blend 或网络确认。
- 不新增测试，不运行 Unity batchmode。

## Spec Alignment Notes

- `refactor-bt-edge-condition-decorators` 已完成但未归档，其 Selector 语义当前是 StopSlot 后同 Tick立即 tick 高优先级 child。本变更修改为“stop 立即完成时可同 Tick切换，stop pending 时必须等待”，合并 current spec 时必须先归档或 rebase 该基础 change。
- `btsmtl-sm-node-authoring` 当前只要求父 Graph stop/reset 传播到 active State，没有要求父 Tree abort 执行 State.OnExit；本变更修改该合同。
- `btsmtl-runnable-timeline-node` 当前使用 stop/reset 取消 playback，但没有区分自然完成、graceful stop 和 ForceStop；本变更修改该合同。
- `character-action-authoring-closure` 已要求动作退出语义显式配置，本变更保持 Action lifecycle 属于 State.OnExit，不移到 Tree edge 或 SM runtime。
- `add-timeline-loop-playback-and-state-transition-blend` 已要求旧状态逻辑停止、表现层保留 outgoing plan。本变更保持逻辑停止屏障与动画 blend 分层。
- `character-pipeline-runtime` 当前 RootTree 后推进 Timeline；本变更增加无副作用 decision prepare，并保持正式输出在 RootTree 后提交。
- `openspec/project.md` 对 `add-pipeline-blackboard-authoring` 的状态仍滞后于 `openspec list`；本变更复用现有 ExposedProperty reader，不新增镜像数据源。

## Impact

- BTSMTL `RunnableNode`、Composite、Decorator、Root、SubTree 的生命周期 API 和调用点。
- BT Self / LowerPriority abort 的 pending-stop 调度。
- `StateMachineNode`、`StateMachineGraphRuntime`、`StateBehaviorSubTree` 的 graceful exit。
- `TimelineNode` 自然完成、graceful cancel 和 ForceStop 路径。
- Timeline decision facts 与正式输出提交顺序。
- ConditionRuleGraph 的 StateExit、ActionWindow、ActionContext typed reader。
- Corin inline Locomotion/Action StateMachine 与 Attack Timeline。

## Stop Conditions

- 如果某个现有 RunnableNode 无法明确迁移到自然完成、graceful stop 或 ForceStop 中唯一一种正式语义，实施必须停止说明缺口，不保留旧 StopNode 旁路。
- 如果 OnExit 无法在 parent Composite pending-stop 期间安全继续 tick，实施必须停止，不用后台并行旧 branch 绕过。
- 如果 Timeline API 无法无副作用预采样 ActionWindow，实施必须停止，不回退到上一 Tick window 或黑板 Bool。
- 如果 BTSMTL 序列化无法安全修改 Corin inline ConditionRuleGraph，实施必须停止，不创建一次性 asset。

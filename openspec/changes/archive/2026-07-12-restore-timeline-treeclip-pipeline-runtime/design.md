# Design: TreeClip Decision/Commit 管线执行模型

## Context

当前角色逻辑顺序为：

```text
CharacterBTSMTLPhase.Tick
  -> TimelinePlaybackScheduler.PrepareDecisionFacts
  -> BehaviorTreeRuntime.Tick
  -> TimelinePlaybackScheduler.Commit
```

这个屏障保证 Timeline 当前 Tick 的纯决策事实先对 StateMachine 可见，随后 RootTree 可以取消 source Timeline，最后只有仍合法的 playback 提交非决策输出。ActionWindow 已经遵守该顺序，TreeClip 当前完全不在 Scheduler 中。

旧 TreeClip 同时承担资产引用、runtime instance、Clip 生命周期和 Graph User：

```text
TreeClip.TreeAsset
TreeClip.TreeInstance
TreeInstance.InitTree(TreeClip)
```

这既不符合 private-first inline graph，也阻断了 `CharacterGraphContext -> IPipelineBlackboardRuntimeAccess`。

## Goals / Non-Goals

### Goals

- 修复 current spec 中 TreeClip 能力与 Scheduler 实现不一致的问题。
- 保持 TimelinePlaybackScheduler 为角色管线唯一 Timeline 播放权威。
- 允许 Timeline 时间段通过纯 Tree 决策写入当前 Tick Pipeline Blackboard。
- 保持 Decision、State Transition、Timeline cancel 和 Commit 副作用的严格顺序。
- 恢复普通 TreeClip 的持续 Tick 和停止生命周期。
- 让 TreeClip 私有 Graph 默认内联，shared asset 只用于显式复用。
- 将本地状态门与需要网络/action 身份的 ActionWindow 分开。

### Non-Goals

- 不让 Timeline TreeClip 直接执行最终 Transform、Animator、命中、扣血或网络发送。
- 不让 Decision TreeClip 执行 Running 节点、等待节点、TimelineNode 或有副作用节点。
- 不把 Hit、IFrame、Parry、Armor 或攻击连招窗口迁成普通 Blackboard bool。
- 不恢复旧 TimelinePlayer、PlayableGraph 或 `Timeline.Evaluate()` 运行权威。
- 不创建一次性 TimelineRunningTree asset 作为 inline 序列化失败时的替代路径。

## Decisions

### Decision: TreeClip 显式声明 Decision 或 Commit 阶段

TreeClip 必须保存唯一执行阶段：

```text
Decision
Commit
```

Decision TreeClip 在 `PrepareDecisionFacts` 中按目标 Timeline 时间判断是否 active，并执行一次纯决策 Graph。Commit TreeClip 在 `Commit` 中维护状态化 runtime，并执行 Enter、Update、Exit 和 Destroy。

新建 TreeClip 的正式默认阶段为 `Commit`。作者必须显式切换到 `Decision`，并通过纯度校验后才能进入 Prepare 阶段。

业务取舍：同一个通用 TreeClip 类型保留 Timeline 视觉和下钻心智，但阶段必须显式，避免任意 Tree 在状态决策前产生不可撤销副作用。相比让全部 TreeClip 在 Commit 执行，Decision 能保证 Transition 同 Tick响应；相比让全部 TreeClip 在 Prepare 执行，Commit 能保留复杂持续行为而不破坏取消屏障。

### Decision: Decision Tree 每 Tick无状态求值并只写 Frame Blackboard

Decision TreeClip 为每个 playback/clip runtime 创建一份隔离工作副本并复用该副本，但每次求值前必须重置本次决策节点状态，执行完成后不得保留 Running 节点状态。它只允许：

- Input、Actor Pose、Action Context 和 Blackboard 纯读取。
- Equal、Compare、And、Or、Not 等纯条件组合。
- ExposedProperty/Pipeline Blackboard 声明式写入。
- 无 gameplay/presentation/network 副作用的常量和值转换。

Decision 输出变量必须声明为 `Frame` scope 和 `Frame` lifetime。Frame 开始清理旧值，当前 clip active 时重新写入；Timeline 被取消后，该值仍保持到当前 Tick 的 State.OnExit 完成，Frame 结束统一清理。

业务取舍：每 Tick重建 gate 避免依赖 OnDisable 写 false，也避免旧 playback 清理覆盖新 playback。代价是 Decision Graph 不能用 Wait、Running 或跨帧记忆；跨帧状态应属于 State/Action/Blackboard 正式生命周期，而不是时间门。

### Decision: Commit Tree 保持状态化 Runnable 生命周期

每个 active Timeline playback、TreeClip 和 loop cycle 组合必须拥有稳定 Tree runtime identity。Commit TreeClip 在进入范围时创建或启用 runtime，在 active 区间每 Tick推进，在自然离开范围时 graceful stop，在 Timeline cancel/deactivate 时按正式 stop cause 停止，在 runtime Timeline 释放前执行 Destroy 清理。

Commit Tree 输出只能进入正式 pipeline output 或 blackboard；它在 RootTree 决策后执行，因此不能作为同 Tick Transition 条件来源。

业务取舍：恢复旧 TreeClip 的表达能力，同时保持 StateMachine 决策权仍在 RootTree。代价是作者必须理解 Commit 输出最早在后续决策 Tick可见，编辑器和 validator 必须明确显示该阶段。

Commit TreeClip 自然离开时间范围时使用 graceful stop，Scheduler 保存 stopping runtime 并继续推进停止协议；Once Timeline 只有在 duration 到达且自然 stopping runtime 全部完成后才能写回 Succeeded。State exit、Tree abort、reset、pipeline deactivate 或 dispose 取消整个 playback 时使用对应 cause 的 ForceStop，立即禁止后续业务输出，不等待 TreeClip 延长 source 状态生命周期。

业务取舍：自然播放允许 Tree 完成自己的退出清理，外部抢占则服从 source stop barrier 的及时性。若自然 stop 永不完成，Timeline 将保持 Running 并报告配置问题，不使用超时成功或强制完成 fallback。

### Decision: Scheduler 持有 Tree runtime，不调用旧 Timeline.Evaluate

`TimelinePlaybackScheduler.ActiveTimeline` 或下属 `TimelineTreeRuntimeSet` 负责：

- 从 runtime Timeline clone 解析 TreeTrack/TreeClip。
- 计算 previous/current time、active range 和 loop boundary。
- 创建、缓存、停止和释放隔离 Tree runtime。
- 在 Prepare 只执行 Decision。
- 在 Commit 只执行仍合法 playback 的 Commit Tree。
- 在 cancel、complete、deactivate 和 dispose 时关闭所有 Tree runtime。

禁止重新调用 `Timeline.Bind/Evaluate/Unbind` 作为 TreeTrack 特例，否则会和已有显式轨道采样形成第二推进权威。

### Decision: Graph User 与 Clip runtime context 分离

TimelineRunningTree 的 `BaseGraph.User` 必须是正式 `CharacterGraphContext` 或等价管线上下文，使现有 ExposedPropertyNode、TreeValueNode 和 Pipeline Blackboard 节点继续通过 `IPipelineBlackboardRuntimeAccess` 工作。

TreeClip、Timeline time、clip time、playback identity、owner、cycle 和 Action Context 必须通过独立的 `TimelineTreeClipRuntimeContext` 或等价绑定传入 TimelineRunningTree。TimelineValueNode 从该 Clip context 读取时间，不再把 Graph User 强制解释为 TreeClip。

业务取舍：避免包装 context 导致现有节点无法按 `CharacterGraphContext` 或接口取到正式服务，也避免让 TreeClip 冒充整个管线上下文。

### Decision: TreeClip 私有 Graph 默认 inline

TreeClip 创建时必须自动创建 inline `TimelineRunningTree` graph data，并提供双击或 `Open` 下钻。需要复用时作者可以 Extract Shared 到 `BaseTreeAsset`；inline 和 shared 只能有一个真数据来源。

现有项目没有 TreeClip 业务资产，因此可以删除旧 `TreeAsset + TreeInstance + TreeProperty` 默认模型并直接迁移 editor/runtime 代码，不保留兼容读取。shared graph 的 clip override 只有在确有复用需求时才通过正式 shared override 模型设计，不保留旧字段作为隐藏兼容层。

实施前必须验证当前 SerializeReference/managed-reference 能安全保存 Timeline asset 内的 inline TimelineRunningTree、节点、边、PropertyPort 和 ExposedProperty。若无法保证序列化与下钻保存安全，apply 必须停止并说明缺口，不得自动创建一次性 asset。

### Decision: 本地状态门与动作窗口分层

`CanDodgeMoveCancel` 只表达本地 State Transition eligibility：

```text
Decision TreeClip active
  -> CanDodgeMoveCancel = true

Dodge -> None
  StateRootCompleted
  OR
  (CanDodgeMoveCancel AND HasMove)
```

Dodge OnExit 在同 Tick读取 `CanDodgeMoveCancel AND HasMove`，提交 `Cancel(DodgeMoveToRun)`；自然完成提交 `Complete(DodgeComplete)`；Tree abort 提交 `Abort(TreeAbort)`。

该 gate 不产生 ActionWindowSample，不进入 ActionProfile window policy，也不写 SyncFacts。IFrame 继续由 ActionWindowTrack 产出。攻击连招 CancelWindow 继续作为 ActionWindow，因为它需要稳定 WindowId、ActionInstance、策略解析和网络/debug 身份。

业务取舍：Timeline 仍决定可取消时间，Tree 决定如何写本地决策变量，StateMachine 组合输入并选择状态。代价是服务端若未来需要验证 Dodge 恢复段，必须通过 Action lifecycle/config 设计正式校验合同，不能把本地 blackboard 值当网络事实。

### Decision: Editor 和 Validator 必须暴露阶段与限制

Timeline Editor 的 TreeClip 必须显示：

- `Decision` 或 `Commit` 阶段。
- inline/shared ownership。
- 下钻入口。
- Decision 输出 Blackboard declaration 摘要。
- 当前 Graph 中不允许的节点错误。

Timeline Preview 只有在正式 preview target 提供所需 Pipeline Context 时才能执行 Decision/Commit Tree；缺少上下文时显示不可执行状态，不写入 asset 默认值，也不创建 fallback context。

## Runtime Flow

```text
BeginFrame
  -> Clear Frame Blackboard

TimelinePlaybackScheduler.PrepareDecisionFacts
  -> Advance decision target time without commit
  -> Evaluate active Decision TreeClip once
  -> Write Frame Blackboard
  -> Prepare ActionWindow decision facts

BehaviorTreeRuntime.Tick
  -> ConditionRuleGraph reads Frame Blackboard
  -> State Transition may cancel source Timeline
  -> State.OnExit still reads same Tick Blackboard

TimelinePlaybackScheduler.Commit
  -> Drop cancelled playback non-decision outputs
  -> Advance retained playback
  -> Tick retained Commit TreeClip runtimes
  -> Sample Motion/Cue/Camera/ActionWindow outputs

EndFrame
  -> Clear Frame Blackboard
```

## Migration Plan

1. 修复 TreeClip 数据模型和 inline/shared ownership，不创建业务资产。
2. 将 Tree runtime 接入 Scheduler，并完成 Decision/Commit、loop 和 stop 生命周期。
3. 接入正式 CharacterGraphContext 与 Pipeline Blackboard declaration。
4. 完成 Timeline Editor、下钻、阶段 UI 和 validator。
5. 创建 Corin Dodge 的 inline Decision TreeClip 与 `CanDodgeMoveCancel` Frame variable。
6. 原子替换 Dodge Transition/OnExit 条件。
7. 删除旧 DodgeMoveToRun ActionWindow clip、policy 和 reader。
8. 清理旧 TreeClip runtime 字段与直接 Evaluate 残留。

## Risks / Trade-offs

- Decision Graph 白名单过宽会破坏取消屏障；过窄会降低 TreeClip 组合价值，因此 validator 必须按节点能力而不是类型名散列表表达纯度。
- TreeClip inline graph 位于 Timeline managed-reference 数据中，序列化稳定性是 apply 前置条件，不能以一次性 asset 绕过。
- Commit TreeClip 的输出晚于当前 Tick State Transition，这是明确阶段语义，不应通过读取旧 blackboard 值伪装同 Tick响应。
- active change 已经写入 Dodge CancelWindow。迁移必须一次完成并删除旧路径，不能让 ActionWindow 和 Decision Blackboard 同时决定同一条边。

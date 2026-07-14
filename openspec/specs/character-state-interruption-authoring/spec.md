# character-state-interruption-authoring Specification

## Purpose
定义State Transition与父Tree abort共用通用Runnable stop、source-exit、OnExit、Timeline cancel、Action lifecycle和Presentation Adapter的创作闭环。
## Requirements

### Requirement: 状态抢占必须复用分层停止协议

状态抢占 MUST继续复用通用 Runnable stop、StateMachine transition、State.OnExit 与 Timeline producer release。逻辑层 MUST在 stop barrier 内关闭 source State、Action、Timeline gameplay output，并在完成 priority/ownership 决策后为受影响 LayerId 提交唯一 AnimationLayerSelection。系统 MUST不让 source 逻辑为 fade 继续 Running，也 MUST不使用 StateMachine external animation、Tree Driver 或 Animation priority。

#### Scenario: RunEnd 被输入抢占

- **WHEN** RunEnd 命中更高优先级 State edge
- **THEN** StateMachine MUST完成 source exit 与 target activation
- **AND** Locomotion 逻辑 MUST选择 target playback
- **AND** AnimationPlaybackLifecycle MUST只消费该 selection 与 sample

#### Scenario: 上层 Selector 抢占 StateMachineNode

- **WHEN** LowerPriority replacement 停止整个 StateMachineNode
- **THEN** stop cause MUST沿 StateMachineNode 与 active State descendants 传播
- **AND** Action/Locomotion 逻辑 MUST在 barrier 完成后提交最终 selection
- **AND** MUST不读取 StateMachineNode external animation definition

#### Scenario: ForceStop

- **WHEN** pipeline/host ForceStop、deactivate 或 dispose
- **THEN** Pipeline MUST立即清理 logic owner、playback lifecycle、Animancer states 与 retention
- **AND** MUST不读取 transition duration 或等待 fade

### Requirement: StateExitContext 必须保持层间翻译边界

StateMachine runtime MUST 将 `NodeStopContext` 或 State Transition 选择翻译为 transient `StateExitContext`。StateExitContext MUST 包含退出原因、source State、可选 target State、可选 Transition edge 和可选 parent Tree source/replacement identity。它 MUST NOT 写入 authoring asset、Pipeline Blackboard 或网络协议。

#### Scenario: LowerPriority abort 进入 State.OnExit

- **WHEN** SMNode 因 parent LowerPriority abort 进入 active State.OnExit
- **THEN** OnExit MUST 能读取退出来源为 Tree LowerPriority abort
- **AND** target State identity MUST 为空
- **AND** replacement Tree node identity MAY 可读

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

OnExit 与 Transition 条件 MUST 使用 `StateExitCauseInfoNode`、Action Context reader、Pipeline Blackboard ValueNode 和通用 Equal/And/Or/Not 等纯条件节点组合。所有 Timeline 时间门，包括需要 ActionInstance、策略解析或同步/debug 身份的动作窗口，都 MUST 由 Decision TreeClip 写入 scope variable；ConditionRuleGraph MUST NOT 使用 ActionWindow reader 或专用 timeline decision window cache。Action terminal lifecycle MUST 由显式 lifecycle 节点提交，StateMachine runtime MUST NOT 自动推导 Action lifecycle。

#### Scenario: ComboWindow 离开攻击

- **WHEN** Attack1 的 `Attack1Cancel` Decision TreeClip 在当前 Tick写入 true
- **AND** Attack request 成立且 source Action Context 仍 active
- **THEN** Attack1 Transition MUST 通过 Blackboard Bool reader 离开 source State
- **AND** Attack1 OnExit MUST 显式提交 `Cancel(ComboWindow)`
- **AND** 同一 declaration 的 ActionWindow projection MUST 保持 ActionInstance、policy 和 debug 身份

#### Scenario: Dodge 本地恢复门离开动作

- **WHEN** Dodge Decision TreeClip 在当前 Tick写入 `CanDodgeMoveCancel=true`
- **AND** 当前移动输入成立且 source Action Context 仍 active
- **THEN** Dodge Transition MUST 能离开 source State
- **AND** Dodge OnExit MUST 显式提交 `Cancel(DodgeMoveToRun)`
- **AND** Projection=None 的本地 gate MUST NOT产生 ActionWindowSample

#### Scenario: Locomotion 状态抢占

- **WHEN** RunEnd 通过普通输入 Transition 离开
- **THEN** runtime MUST处理状态退出并发布通用Runnable/EdgeCommit facts
- **AND** MUST NOT生成 Action Cancel、Interrupt 或 Abort

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source State root、Action lifecycle、Timeline gameplay output 与逻辑所有权 MUST在 stop barrier 内关闭。AnimationPlaybackLifecycle MAY让已释放 source 以 Outgoing 视觉状态存在，并通过 PresentationRetention 接收 animation-only sample；Animancer MUST负责 fade。逻辑 release MUST不等于 outgoing visual retirement，但表现收尾 MUST不重新 tick source gameplay。

#### Scenario: CrossFade 收尾

- **WHEN** source 已逻辑退出且 Animancer 正在淡出其 state
- **THEN** source MAY保持 Outgoing 与只读 animation retention
- **AND** source MUST不再产生 gameplay、Tree、Timeline logic、Motion、root motion 或 SyncFacts

#### Scenario: target 首样本延迟

- **WHEN** source 已退出但 selected target 尚无第一份 sample
- **THEN** lifecycle MUST保持上一 Current 并记录 PendingFirstSample
- **AND** MUST不恢复 source 逻辑所有权或选择 fallback

#### Scenario: 结构 target

- **WHEN** logical target 本身不产 animation producer
- **THEN** RequireOutput layer 的逻辑提交 MUST省略该层更新并保持已提交的正式 producer，或直接选择目标状态的正式 producer
- **AND** AllowEmpty layer MAY显式选择 None
- **AND** Animation 模块 MUST不从 Runnable executed 或 Tree route 推断 target

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree/StateMachine terminal MUST只由逻辑停止协议决定，MUST不等待 Animancer fade。PendingFirstSample、Current、Outgoing 与 Retired MUST由 AnimationPlaybackLifecycle 在表现帧推进；fade progress MUST由 Animancer 使用 presentation delta 推进。teardown MUST确定性清理播放生命周期。

#### Scenario: 长淡出与新 child

- **WHEN** source SMNode 已 terminal 但 source Animancer state 仍为 Outgoing
- **THEN** parent Tree MUST能推进 replacement child
- **AND** replacement logic MUST能提交新 selection

#### Scenario: Host 销毁

- **WHEN** host 在 fade 运行时 dispose
- **THEN** lifecycle、retention 与 Animancer states MUST立即释放

### Requirement: 嵌套 StateMachine 停止必须逐层复用同一 source-exit 协议

当父 State root 中运行的嵌套 StateMachineNode 被 State transition、Tree graceful abort 或 ForceStop 停止时，stop context MUST 沿 execution path 逐层传播。内层 active State MUST 先停止 Root producer、运行 State.OnExit 并关闭 Action lifecycle；外层 State MUST 等待嵌套 StateMachineNode terminal 后完成自己的 OnExit。系统 MUST NOT 跳过内层 OnExit，也 MUST NOT 让父子 State 各自提交一条相同业务 terminal transition。

#### Scenario: 外层 Attack 被 Dodge replacement 抢占

- **WHEN** 外层 Attack State 收到指向 Dodge 的 replacement stop
- **AND** 内层 Attack1 仍 active
- **THEN** Attack1 Timeline MUST 在逻辑 stop barrier 内停止 gameplay 采样
- **AND** Attack1 OnExit MUST 根据原始 StateExitContext 提交一次 Cancel 或 Interrupt
- **AND** 外层 Attack OnExit MUST NOT 再提交 Action lifecycle
- **AND** replacement MUST 等待嵌套 stop 完成后启动

#### Scenario: Parent Tree LowerPriority abort

- **WHEN** LowerPriority abort 传播到包含嵌套 Attack StateMachineNode 的 Action StateMachineNode
- **THEN** inner leaf 读取的 OriginCause MUST 仍是 LowerPriorityAbort
- **AND** replacement edge/node identity 与 logic tick MUST 保持
- **AND** 内层和外层 MUST 共用同一 stop barrier

#### Scenario: 嵌套 ForceStop

- **WHEN** pipeline deactivate、dispose 或 Reset 对外层 StateMachineNode 执行 ForceStop
- **THEN** runtime MUST 立即释放所有 descendant State、Timeline、Blackboard、Action Context 和 animation membership
- **AND** runtime MUST NOT 伪造 gameplay Cancel、Interrupt 或 Abort
- **AND** 不得残留 descendant execution path frame

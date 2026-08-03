# character-state-interruption-authoring Specification

## Purpose
定义State Transition与父Tree abort共用通用Runnable stop、source-exit、OnExit、Timeline cancel、Action lifecycle和Presentation Adapter的创作闭环。
## Requirements
### Requirement: 状态抢占必须复用分层停止协议

Gameplay状态抢占authoring MUST继续表达通用Runnable stop、BTSMTL StateMachine transition、State.OnExit、Action lifecycle与有限Action Timeline producer release。Compiler MUST把它们生成为统一control-flow、stop barrier、Motion ownership与release operation；Program MUST在关闭source Action与Timeline Gameplay output后，为每个受影响的有限Action AnimationChannel输出至多一个producer command。持续Locomotion Pose transition MUST由PoseStateMachine处理，Program MUST不为视觉transition保持source Gameplay State active。

#### Scenario: Attack被Dodge抢占

- **WHEN** 更高优先级Dodge replacement停止Attack State
- **THEN** Gameplay MUST完成Attack source exit并激活Dodge
- **AND** FullBodyAction MUST提交Dodge playback
- **AND** Slot MUST独立处理Attack到Dodge的Pose transition

#### Scenario: 上层Selector抢占StateMachineNode

- **WHEN** LowerPriority replacement停止整个Gameplay StateMachine operation
- **THEN** stop cause MUST沿active descendant传播
- **AND** Program MUST释放受影响Action playback与Motion ownership
- **AND** MUST不读取PoseStateMachine active state

#### Scenario: ForceStop

- **WHEN** Session、Actor或Host ForceStop/deactivate/dispose
- **THEN** Pipeline Runtime MUST立即关闭logic activation并输出各channel retire lifecycle
- **AND** Presentation MUST清理Lifecycle、全部Player节点、Pose Graph workspace、Animancer sources与retention
- **AND** MUST不等待fade完成或读取Pose Graph transition

### Requirement: StateExitContext 必须保持层间翻译边界

StateMachine runtime MUST 将 `NodeStopContext` 或 State Transition 选择翻译为 transient `StateExitContext`。StateExitContext MUST 包含退出原因、source State、可选 target State、可选 Transition edge 和可选 parent Tree source/replacement identity。它 MUST NOT 写入 authoring asset、Pipeline Blackboard 或网络协议。

#### Scenario: LowerPriority abort 进入 State.OnExit

- **WHEN** SMNode 因 parent LowerPriority abort 进入 active State.OnExit
- **THEN** OnExit MUST 能读取退出来源为 Tree LowerPriority abort
- **AND** target State identity MUST 为空
- **AND** replacement Tree node identity MAY 可读

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

Transition MUST 用 Action Context、Blackboard ValueNode、`ActionWindowActiveInfoNode`、`CanActivateActionInfoNode` 与通用逻辑节点组合。Timeline 时间门 MUST 只由 Decision TreeClip 写 owner-local declaration；ActionWindow projection MUST 是 WindowType、ActionInstance、WindowId 和 Digest 的唯一来源。条件只读当前帧 candidate，MUST NOT 建 cache、registry、历史副本或目标专用节点。source leaf MUST 显式提交 terminal；StateMachine 与 target activation MUST NOT 自动取消 source。

条件可见范围 MUST 只包含祖先 graph、所在 StateMachine 和 source StateNode 直接 body，不包含 target、兄弟 state 或后代 leaf。Compiler、Agent、Inspector、Validator 与 runtime MUST 同规则。内层 leaf 读本地 window；外层 category 只在 `state_root_completed` 后选目标，不得再读 leaf window。

#### Scenario: Source transition 读取本地窗口

- **WHEN** source Timeline 投影 `RecoveryEarly`
- **THEN** source Transition MUST 读取当前 ActionInstance 的同一 candidate
- **AND** 其它 state 引用该 local declaration MUST 失败

#### Scenario: Action replacement

- **WHEN** `ComboAccept` 或 `RecoveryEarly` 与 request、target admission 成立
- **THEN** source MUST 显式 `Cancel(RecoveryCancel)` 后离开
- **AND** target MUST 在 stop barrier 后消费 request，MUST NOT 自动取消 source

#### Scenario: Dodge RecoveryOpen

- **WHEN** `RecoveryOpen` 与 Attack、Dodge 或 Move 条件成立
- **THEN** StateMachine MUST 按 edge priority 选择唯一 target
- **AND** MUST NOT 读取旧 cancel key

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source Gameplay State root、Action lifecycle、Timeline Gameplay output与Motion ownership MUST在stop barrier内关闭。`CharacterActionPlaybackRuntime` MAY让已释放的有限Action source以Retained视觉状态存在；AnimationSlot、BlendStack与Inertialization MUST按compiled route完成表现收尾。PoseStateMachine source relevance MUST只属于Presentation workspace。逻辑release MUST不等待Slot或PoseState transition，表现收尾 MUST不重新tick source Gameplay。

#### Scenario: Action Slot收尾

- **WHEN** Attack已经逻辑退出但FullBodyAction Slot仍在淡出
- **THEN** Attack source MAY保持Retained animation-only sample
- **AND** MUST不再产生Gameplay、Timeline logic、Motion或Window

#### Scenario: target首样本延迟

- **WHEN** source已退出但selected target尚无首样本
- **THEN** Lifecycle MUST保持上一份正式Player输出并记录PendingFirstSample
- **AND** MUST不恢复source逻辑ownership或选择fallback

#### Scenario: 结构target

- **WHEN** logical target本身不产animation producer
- **THEN** RequireOutput channel MUST保持已提交正式producer或由逻辑直接选择目标producer
- **AND** AllowEmpty channel MAY显式选择None
- **AND** Animation module MUST不从Runnable、Tree route或Pose Graph推断target

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree与Gameplay StateMachine terminal MUST只由逻辑停止协议决定，MUST不等待Animation Slot、PoseStateMachine transition、PendingFirstSample、BlendStack或Inertialization。Action playback lifecycle与PoseState workspace MUST由PresentationFrame推进；teardown MUST按编译Plan确定性清理。

#### Scenario: 长Action淡出与新child

- **WHEN** source Action State已经terminal但Action Pose仍为Retained
- **THEN** parent Tree MUST能推进replacement child
- **AND** 新Action MUST能提交新的playback generation

#### Scenario: Host销毁

- **WHEN** Host在Player连续化运行时dispose
- **THEN** Runtime MUST清理Lifecycle、全部Player节点、source、Pose Graph workspace与retention
- **AND** Tree terminal MUST不等待transition duration

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

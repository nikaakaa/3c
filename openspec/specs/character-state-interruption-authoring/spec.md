# character-state-interruption-authoring Specification

## Purpose
定义State Transition与父Tree abort共用通用Runnable stop、source-exit、OnExit、Timeline cancel、Action lifecycle和Presentation Adapter的创作闭环。
## Requirements
### Requirement: 状态抢占必须复用分层停止协议

状态抢占authoring MUST继续表达通用Runnable stop、StateMachine transition、State.OnExit与Timeline producer release。Compiler MUST将其生成为统一control-flow、stop barrier、ownership与release operation；Program MUST在关闭source State、Action与Timeline Gameplay output后，为每个受影响AnimationChannelId输出至多一个producer command。系统 MUST不让source逻辑为视觉transition继续active，也 MUST不使用StateMachine external animation、Tree Driver、Animation priority或CharacterGraphContext selection。

#### Scenario: RunEnd 被输入抢占

- **WHEN** RunEnd compiled edge 命中更高优先级条件
- **THEN** StateMachine operation MUST 完成 source exit 与 target activation
- **AND** Locomotion operation MUST 输出 target producer command
- **AND** AnimationPlaybackLifecycle MUST 只消费已提交 command 与 sample

#### Scenario: 上层 Selector 抢占 StateMachineNode

- **WHEN** LowerPriority replacement 停止整个 StateMachine operation
- **THEN** stop cause MUST 沿 active descendant operation 传播
- **AND** Action/Locomotion operation MUST 在 barrier 完成后输出最终 producer command
- **AND** MUST 不读取 StateMachineNode external animation definition

#### Scenario: ForceStop

- **WHEN** Session、Actor 或 Host ForceStop/deactivate/dispose
- **THEN** Pipeline Runtime MUST立即关闭 logic activation并输出 retire lifecycle
- **AND** Committer/Presentation MUST清理playback lifecycle、Blend Stack source、Animancer source playable与retention
- **AND** MUST不读取transition duration或等待视觉收尾

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

source State root、Action lifecycle、Timeline gameplay output与逻辑所有权 MUST在stop barrier内关闭。AnimationPlaybackLifecycle MAY让已释放source以Retained视觉状态存在，并通过PresentationRetention接收animation-only sample；对应PoseSlot Blend Stack MUST负责transition、Stored/Inertial状态与最终release。逻辑release MUST不等于retained visual retirement，但表现收尾 MUST不重新tick source gameplay。

#### Scenario: Blend Stack收尾

- **WHEN** source已逻辑退出且PoseSlot Stack仍保留其视觉贡献
- **THEN** source MAY保持Retained与只读animation retention
- **AND** source MUST不再产生 gameplay、Tree、Timeline logic、Motion、root motion 或 GameplayFacts

#### Scenario: target 首样本延迟

- **WHEN** source 已退出但 selected target 尚无第一份 sample
- **THEN** lifecycle MUST保持上一Retained输出并记录PendingFirstSample
- **AND** MUST不恢复 source 逻辑所有权或选择 fallback

#### Scenario: 结构 target

- **WHEN** logical target 本身不产 animation producer
- **THEN** RequireOutput channel的逻辑提交 MUST省略该channel更新并保持已提交的正式producer，或直接选择目标状态的正式producer
- **AND** AllowEmpty channel MAY显式选择None
- **AND** Animation 模块 MUST不从 Runnable executed 或 Tree route 推断 target

### Requirement: 动画 Transition 的完成不得反向阻塞 Tree terminal

Tree/StateMachine terminal MUST只由逻辑停止协议决定，MUST不等待视觉transition。PendingFirstSample、Selected、Retained与Retired MUST由AnimationPlaybackLifecycle在表现帧推进；transition progress MUST由每PoseSlot Blend Stack使用presentation delta推进。teardown MUST确定性清理播放生命周期。

#### Scenario: 长淡出与新 child

- **WHEN** source SMNode已terminal但source仍为Retained Stack entry
- **THEN** parent Tree MUST能推进 replacement child
- **AND** replacement logic MUST能提交新 selection

#### Scenario: Host 销毁

- **WHEN** host 在 fade 运行时 dispose
- **THEN** lifecycle、retention、Stack source与Animancer source playable MUST立即释放

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

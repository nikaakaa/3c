## MODIFIED Requirements

### Requirement: Corin Locomotion 必须使用 StateMachine + Timeline 编排

Corin locomotion MUST使用状态机表达基础移动与分层所有权状态。有独立时序内容的状态 MUST在状态行为内通过 TimelineNode 播放对应 Timeline；没有独立动画资源的状态 MUST使用 Transition blend 或无表现 ownership state 衔接，不得创建伪 Timeline 或 fallback clip。状态至少包含 `Idle`、`WalkStart`、`WalkLoop`、`WalkEnd`、`RunStart`、`RunLoop`、`RunEnd`、`MovingTurn` 和 `ActionOverride`。所有 start、end、loop、turn 和 ownership 状态 MUST通过明确 Transition 响应输入与 `HasActionLocomotionOwnership`，并复用统一 State source-exit 生命周期。RunEnd MUST只表达 locomotion 从实际 Run 状态停止，不得作为 Action 结束后的通用恢复状态。

#### Scenario: WalkStart 输入抢占

- **WHEN** WalkStart root 尚未完成且输入进入 Run 或 Stop 区间
- **THEN**状态机 MUST分别允许进入 RunStart 或 WalkEnd
- **AND** source TimelineNode MUST通过 State root stop 取消

#### Scenario: RunLoop 与 MovingTurn 输入抢占

- **WHEN** RunLoop 或 MovingTurn 的输入进入 Stop、Walk 或有效 Run/Turn 区间
- **THEN**状态机 MUST按明确 edge 切换到 RunEnd、WalkLoop、MovingTurn 或 RunLoop
- **AND**同 source 多条边 MUST使用稳定 priority

#### Scenario: Full-body Action 活跃时交出 locomotion 所有权

- **WHEN**任一普通 locomotion state 读取到 `HasActionLocomotionOwnership=true`
- **THEN** Locomotion StateMachine MUST以高优先级进入 ActionOverride
- **AND** ActionOverride MUST NOT播放动画、引用 Action Timeline 或提交 motion contribution

#### Scenario: Action 完成后有输入进入 RunLoop

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND**当前 MoveAxis 大于 stop threshold
- **THEN** Locomotion StateMachine MUST直接进入 RunLoop
- **AND** MUST NOT重复进入 RunStart

#### Scenario: Action 完成后无输入进入 Idle

- **WHEN** ActionOverride 读取到 `HasActionLocomotionOwnership=false`
- **AND**当前 MoveAxis 不大于 stop threshold
- **THEN** Locomotion StateMachine MUST直接进入 Idle
- **AND** MUST NOT播放 RunEnd

#### Scenario: ownership edge 优先级

- **WHEN**同一 source state 同时满足普通 Walk/Run/Turn 条件和 `HasActionLocomotionOwnership=true`
- **THEN** ActionOverride edge MUST使用稳定更高 priority 获胜
- **AND**状态机 MUST NOT创建重复 source-target edge

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 外层 Action StateMachine MUST只表达动作大类，并包含 `None`、`Attack` 和 `Dodge`。`Attack1`、`Attack2`、`Attack3`、`Attack4` 与 `Attack5` MUST位于 Attack StateNode body 内的 inline StateMachineNode；`DodgeBack` 与 `DodgeForward` MUST位于 Dodge StateNode body 内的 inline StateMachineNode。具体动作 leaf MUST使用 ActionProfile、独立 Action Context 和带 Action Context 的 inline TimelineNode。连段、恢复取消与外层 replacement MUST复用普通 ConditionRuleGraph、State edge、Runnable stop、source OnExit、Action lifecycle 和 Timeline cancel，不得创建 Action 专用旁路。没有成功闪避或成功格挡的 Combat Resolution 事实时，系统 MUST不保留 RushAttack、CounterAttack 或按上一状态推导特殊攻击的路由。

#### Scenario: 首次进入 Attack1

- **WHEN**外层 None 检测到 Attack request 且 `CanActivateAction(Attack)` 为 true
- **THEN**外层 Action StateMachine MUST进入 Attack category
- **AND**外层条件 MUST只查询而不消费 request
- **AND**内层 Attack StateMachine MUST进入 Attack1
- **AND** Attack1 target activation MUST消费 request 并创建新的 Action Context

#### Scenario: 五段普通攻击连段

- **WHEN** Attack1..4 的 `ComboAccept` active、存在 Attack request 且下一段 admission 为 true
- **THEN**内层 Attack StateMachine MUST按 Attack1→2→3→4→5 抢占
- **AND** source OnExit MUST提交一次 `Cancel(RecoveryCancel)`
- **AND** target activation MUST消费 request 并创建新的 Action Context

#### Scenario: Attack5 是有限连段终段

- **WHEN** Attack5 播放期间再次收到 Attack request
- **THEN**内层 StateMachine MUST不从 Attack5 replacement 到 Attack1
- **AND** Attack5 MUST只允许有效 Dodge、Move replacement 或 natural complete

#### Scenario: 攻击较早后摇被 Dodge 取消

- **WHEN**任一 Attack leaf 的 `RecoveryEarly` active
- **AND** Dodge request 与 Dodge admission 成立
- **THEN**该 source leaf 的内层 Exit edge MUST优先于 Combo、Move 和 natural complete
- **AND** source lifecycle MUST在 target Dodge activation 前明确关闭
- **AND**外层 Attack→Dodge edge MUST只在 inner `state_root_completed` 后结合 Dodge request 与 Dodge admission 路由，不得再次读取 `RecoveryEarly`

#### Scenario: 攻击较晚后摇被移动取消

- **WHEN**任一 Attack leaf 的 `RecoveryLate` active 且当前 MoveAxis 大于 stop threshold
- **AND**没有更高优先级 Dodge 或 Combo edge 获胜
- **THEN** leaf MUST先退出 Attack 内层 StateMachine
- **AND**外层 Attack→None Move edge MUST使用 `state_root_completed` AND Move input 路由
- **AND** Locomotion MUST在 ownership 释放后进入 RunLoop

#### Scenario: 攻击完整后摇自然结束

- **WHEN**当前 Attack leaf 没有有效 Dodge、Combo 或 Move replacement
- **THEN**其 End clip MUST完整播放到 Timeline root terminal
- **AND** leaf MUST提交一次 Complete 并退出到 None
- **AND**无移动输入时 Locomotion MUST回 Idle

#### Scenario: Dodge 恢复期接普通 Attack1

- **WHEN** DodgeBack 或 DodgeForward 的 `RecoveryOpen` active
- **AND** Attack request 与 Attack admission 成立
- **THEN** Dodge leaf MUST先通过内层 Exit edge 完成 Dodge StateMachine
- **AND**外层 Dodge→Attack edge MUST使用 `state_root_completed` AND Attack request AND Attack admission 路由
- **AND**外层 edge MUST NOT再次读取 `RecoveryOpen`
- **AND**内层 Attack StateMachine MUST通过普通 Enter 进入 Attack1
- **AND**只有 Attack1 target activation MUST消费 request

#### Scenario: Dodge 恢复期再次闪避或移动

- **WHEN** Dodge `RecoveryOpen` active
- **THEN** Attack edge MUST高于 Dodge re-entry，Dodge re-entry MUST高于 Move edge，Move MUST高于 natural complete
- **AND**所有 route MUST使用显式 State transition，不得由 runtime 全局 priority 推导

#### Scenario: Dodge 无输入自然结束

- **WHEN** Dodge Timeline 自然完成且没有有效 Attack、Dodge 或 Move replacement
- **THEN** Dodge leaf MUST提交一次 Complete 并退出到 None
- **AND** Locomotion MUST直接回 Idle
- **AND** MUST NOT经过 RunEnd

#### Scenario: 打开外层 Action StateMachine

- **WHEN**作者打开 Corin Action StateMachine
- **THEN**作者 MUST只看到 `None`、`Attack` 和 `Dodge` 动作大类
- **AND**作者下钻 Attack 或 Dodge state body 后 MUST能继续打开对应 inline StateMachine

### Requirement: Corin inline Timeline 迁移必须保留 TreeClip 事实链路

Corin Attack1..5、DodgeForward 和 DodgeBack inline Timeline MUST保留 Hit、IFrame、ComboAccept、RecoveryEarly、RecoveryLate 与 RecoveryOpen 的 frame range、phase、inline TimelineRunningTree、owner-local Blackboard declaration reference、fact projection、Action Context provenance、WindowId 和 Digest。Attack1..4 的现有 Cancel 时间范围与 Attack1..5 的 MoveCancel 时间范围迁移为语义 WindowType 时 MUST保持对应 TreeClip/declaration identity；Attack5 的旧循环 ComboAccept route 与 legacy RushAttack state/Timeline MUST删除。新增 RecoveryEarly MAY创建新 identity。迁移 MUST NOT恢复 Root-owned per-state Cancel declaration、ActionWindowTrack、ActionWindowClip、SubmitActionWindowSampleNode、timeline decision cache 或第二套窗口 registry。

#### Scenario: 迁移 Attack TreeClip

- **WHEN** Attack1..4 的旧 ComboCancel 与 Attack1..5 的旧 MoveCancel 窗口迁移
- **THEN**对应 TreeClip MUST分别使用 `ComboAccept` 与 `RecoveryLate`
- **AND** declaration MUST位于各自 inline Timeline owner 下
- **AND** WindowFactProjection MUST继续生成相同 ActionInstance、WindowId 与 Digest provenance

#### Scenario: 增加攻击较早恢复窗口

- **WHEN**每段 Attack End clip 需要允许 Dodge 较早取消
- **THEN**作者 MUST在同一 inline Timeline 中创建 `RecoveryEarly` local declaration 与 TreeClip
- **AND**该窗口 MUST不命名为 DodgeCancel 或绑定唯一 target

#### Scenario: 迁移 Dodge TreeClip

- **WHEN** DodgeForward 和 DodgeBack 旧恢复窗口迁移
- **THEN**两个 TreeClip MUST使用 `RecoveryOpen` 与 ActionWindow projection
- **AND** IFrame TreeClip 与 declaration identity MUST保持
- **AND**系统 MUST不保留 Projection=None 的 `CanDodgeMoveCancel` 副本

#### Scenario: 旧外部 Timeline assets 已迁移

- **WHEN** Corin tracks、clips、引用和 playback mode 已进入对应 inline TimelineNode
- **THEN**项目 MUST不恢复旧外部 Timeline asset 或 shared fallback
- **AND**所有窗口查询 MUST继续消费 inline owner 的正式 projection

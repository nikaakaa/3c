## ADDED Requirements
### Requirement: Locomotion transient 抢占退出规则
Locomotion graph MUST 能消费 FullBody Action 产生的一次性 Locomotion preemption fact，并用正式 Locomotion transition 结束被抢占的 transient motion source。抢占退出 MUST 根据当前移动输入和 Locomotion runtime facts 选择目标 Locomotion state，不得通过 Action state 节点或 Dodge 专用 transition 表达。

#### Scenario: TurnBack 被抢占且有移动输入时进入 MoveLoop
- **GIVEN** 当前 Locomotion graph active state 为 `Locomotion.TurnBack`
- **AND** context 中存在未消费的 Locomotion preemption fact
- **AND** 本帧存在移动输入
- **WHEN** Locomotion graph 评估 transition
- **THEN** graph MUST 以高于 TurnBack 自然出口的优先级进入 `Locomotion.MoveLoop`
- **AND** gait MUST 由 Locomotion intent、Run latch 或等价 Locomotion facts 决定
- **AND** transition MUST NOT 要求 Shift 仍处于 held 状态

#### Scenario: TurnBack 被抢占且无移动输入时进入 Idle
- **GIVEN** 当前 Locomotion graph active state 为 `Locomotion.TurnBack`
- **AND** context 中存在未消费的 Locomotion preemption fact
- **AND** 本帧没有移动输入
- **WHEN** Locomotion graph 评估 transition
- **THEN** graph MUST 以高于 TurnBack 自然出口的优先级进入 `Locomotion.Idle`
- **AND** 后续 frame MUST NOT 恢复旧 TurnBack motion source

#### Scenario: 抢占事实一次性消费并清理 TurnBack 残留
- **GIVEN** Locomotion graph 已经用 preemption fact 退出 `Locomotion.TurnBack`
- **WHEN** Locomotion runtime 提交该帧结果
- **THEN** preemption fact MUST 被标记为已消费或从下一帧 context 移除
- **AND** pending TurnBack intent MUST 被清除
- **AND** TurnBack motion playback window MUST 被重置

#### Scenario: Locomotion graph 不包含 Action 节点
- **WHEN** 设计者检查 Corin Locomotion graph
- **THEN** 抢占退出 MUST 表达为 `Locomotion.TurnBack -> Locomotion.MoveLoop`
- **AND** 抢占退出 MUST 表达为 `Locomotion.TurnBack -> Locomotion.Idle`
- **AND** graph MUST NOT 新增 `Action.Dodge` 节点
- **AND** graph MUST NOT 新增 `Action.Dodge -> Locomotion.*` transition

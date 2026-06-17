## MODIFIED Requirements
### Requirement: 移动 TurnBack 逻辑状态
系统 MUST 将移动反向急转表达为 `FullBody/Locomotion/TurnBack` 逻辑状态。角色在 MoveLoop 中收到与上一有效移动方向夹角达到阈值的移动输入时 MUST 进入 TurnBack；TurnBack MUST 播放 `Locomotion.Turn.Back`；TurnBack 自然结束后 MUST 根据当前输入回到 MoveLoop 或 Idle。若 TurnBack 被 FullBody Action 抢占，系统 MUST 正式结束当前 TurnBack motion source，并根据当前输入回到 MoveLoop 或 Idle，而不得在 FullBody Action 结束后恢复旧 TurnBack 位移曲线。

#### Scenario: MoveLoop 反向输入进入 TurnBack
- **GIVEN** 角色处于 `FullBody/Locomotion/MoveLoop`
- **AND** runtime blackboard 记录上一有效移动方向
- **WHEN** 当前移动输入方向与上一有效移动方向夹角达到 TurnBack 阈值
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/TurnBack`
- **AND** 当前 locomotion phase MUST 为 `TurnBack`

#### Scenario: TurnBack 动画结束后退出
- **GIVEN** 角色处于 `FullBody/Locomotion/TurnBack`
- **WHEN** locomotion animation facts 显示 `Locomotion.Turn.Back` 已结束
- **AND** 当前仍有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/MoveLoop`
- **WHEN** locomotion animation facts 显示 `Locomotion.Turn.Back` 已结束
- **AND** 当前没有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/Idle`

#### Scenario: FullBody Action 抢占 TurnBack 后不恢复旧 root motion
- **GIVEN** 角色处于 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack motion window 尚未自然结束
- **WHEN** FullBody Action 产生并提交 Locomotion preemption fact
- **THEN** 当前 TurnBack motion source MUST 被视为 interrupted
- **AND** 后续 FullBody Action 结束后 MUST NOT 继续执行被打断前剩余的 TurnBack baked planar delta 或 yaw delta
- **AND** 有移动输入时 MUST 回到 `FullBody/Locomotion/MoveLoop`
- **AND** 无移动输入时 MUST 回到 `FullBody/Locomotion/Idle`

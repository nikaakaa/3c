## ADDED Requirements
### Requirement: TurnBack Intent 到请求事实的单向准入
状态请求仲裁入口 MUST 将 `LocomotionTurnBackIntent` 视为 TurnBack 请求的候选输入，并在 priority、resistance、force、过期和 timeline window 规则全部通过后，才生成可被统一状态机消费的 TurnBack request fact。仲裁 rejected 时 MUST NOT 生成 accepted request fact。

#### Scenario: intent 构建候选请求
- **GIVEN** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveLoop`
- **AND** gait 和时间窗口允许 TurnBack 候选请求被提交
- **WHEN** 状态请求仲裁入口处理本帧请求
- **THEN** 系统 MUST 构建 TurnBack 候选 request
- **AND** 该 request MUST 携带 priority、origin tick、expire tick 和 world direction

#### Scenario: accepted 后生成状态机事实
- **GIVEN** TurnBack 候选 request 匹配策略
- **AND** request priority 高于有效 resistance
- **AND** timeline window 条件满足
- **WHEN** `ActionInterruptArbiter` 返回 accepted decision
- **THEN** 状态请求仲裁入口 MUST 生成 `CharacterInputRequestFact(InputRequestKind.TurnBack)`

#### Scenario: rejected 后不生成状态机事实
- **GIVEN** TurnBack 候选 request 存在
- **AND** `ActionInterruptArbiter` 因优先级、抗性、过期、策略缺失或 window 条件拒绝该 request
- **WHEN** 状态请求仲裁入口返回本帧结果
- **THEN** 结果 MUST NOT 包含 accepted TurnBack request fact
- **AND** 统一状态机 MUST 无法因该 rejected request 进入 TurnBack

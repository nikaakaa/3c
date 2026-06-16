## ADDED Requirements
### Requirement: TurnBack 入口只消费仲裁请求事实
统一状态机默认 TurnBack 进入路径 MUST 只消费已经被状态请求仲裁入口接受的 `CharacterInputRequestFact(InputRequestKind.TurnBack)` 或等价 accepted request fact。`LocomotionTurnBackIntent` MAY 作为候选事实存在，但 MUST NOT 直接作为默认 `MoveStart -> TurnBack` 或 `MoveLoop -> TurnBack` transition 的权威条件。

#### Scenario: accepted TurnBack request 进入状态
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveLoop`
- **AND** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 状态请求仲裁入口接受 TurnBack 请求并生成 accepted TurnBack request fact
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST 进入 `FullBody/Locomotion/TurnBack`
- **AND** TurnBack 方向 MUST 来自 accepted request fact 的 world direction

#### Scenario: intent-only 不进入状态
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart` 或 `FullBody/Locomotion/MoveLoop`
- **AND** locomotion facts 中存在有效 `LocomotionTurnBackIntent`
- **AND** 本帧没有 accepted TurnBack request fact
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`

#### Scenario: rejected TurnBack request 不进入状态
- **GIVEN** 输入方向满足 TurnBack 候选条件
- **AND** 状态请求仲裁入口拒绝 TurnBack 请求
- **WHEN** 统一状态机推进本帧
- **THEN** 状态机 MUST NOT 进入 `FullBody/Locomotion/TurnBack`
- **AND** rejected 请求 MUST NOT 被转换为状态机 accepted request fact

#### Scenario: transition evaluator 不重复裁决 TurnBack
- **WHEN** 检查默认 TurnBack 进入 transition
- **THEN** 该 transition MUST 使用 `HasInputRequest(InputRequestKind.TurnBack)` 或等价 accepted request fact 条件
- **AND** MUST NOT 使用 `MoveTurnBackRequested` 或等价 intent 直读条件作为进入权威
- **AND** transition evaluator MUST NOT 重新计算 TurnBack priority、resistance 或 window policy

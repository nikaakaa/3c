## ADDED Requirements
### Requirement: 动作准入与 Transition 条件求值分离
动作打断仲裁入口 MUST 继续负责请求 priority、resistance、force 和 request window 准入；transition condition evaluator MUST 只消费已 accepted 的 input fact 或纯数据状态事实，不得重新执行动作准入策略。

#### Scenario: Accepted fact 后再求值
- **GIVEN** ActionInterruptArbiter 接受一个 Dodge、Attack 或等价请求
- **WHEN** 统一状态机求值 `HasInputRequest` 或等价 condition
- **THEN** evaluator MUST 只检查 accepted input fact
- **AND** MUST NOT 再读取 ActionInterruptPolicy

#### Scenario: Rejected 请求不进入 evaluator
- **GIVEN** ActionInterruptArbiter 拒绝某个请求
- **WHEN** 统一状态机推进本帧
- **THEN** transition evaluator MUST 看不到该 rejected 请求 fact
- **AND** MUST NOT 因该请求进入目标状态

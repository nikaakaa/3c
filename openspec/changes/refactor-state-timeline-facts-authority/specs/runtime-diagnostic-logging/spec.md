## ADDED Requirements
### Requirement: Timeline Facts 诊断由外围提交
Timeline facts、projected facts、target facts 和 transition facts trace 的日志提交 MUST 由 Character diagnostics adapter 或等价外围模块负责。状态机 runner MUST NOT 直接提交 runtime diagnostic log。

#### Scenario: 外围提交 current facts 日志
- **GIVEN** Character frame context 生成 current timeline facts
- **WHEN** diagnostics adapter 处理本帧 trace
- **THEN** 日志 MUST 包含 current facts 的 state id、source step、elapsed seconds、active window ids 和 active fact ids
- **AND** 日志 MUST 标识该 facts 来源为 current

#### Scenario: 外围提交 projected 和 target facts 日志
- **GIVEN** runner 在 transition evaluation 中生成 projected 或 target facts trace
- **WHEN** diagnostics adapter 处理 runner trace
- **THEN** 日志 MUST 能区分 projected facts 和 target facts
- **AND** 日志 MUST NOT 要求 runner 直接调用 `RuntimeDiagnosticLog.Submit`

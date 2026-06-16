## ADDED Requirements
### Requirement: Transition Condition 诊断由 Trace Adapter 提交
Transition condition 求值诊断 MUST 通过纯数据 trace 传递给 runtime diagnostic adapter。runner 和 evaluator MUST NOT 直接提交单个业务条件日志。

#### Scenario: Condition trace 可诊断失败原因
- **GIVEN** 某个 transition condition 求值失败
- **WHEN** diagnostics adapter 消费 condition trace
- **THEN** 日志 MUST 能说明 condition key、当前状态、目标状态、source step 和失败原因

#### Scenario: 业务探针不留在 runner
- **WHEN** 检查 runner 源码
- **THEN** runner MUST NOT 包含 TurnBack、Attack、Jump、HitReact 或等价业务条件专用日志方法
- **AND** 业务条件诊断 MUST 来自 evaluator trace

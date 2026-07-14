## MODIFIED Requirements

### Requirement: BT edge decorator 必须保持网络后端无关

BT edge decorator、Composite runtime 和 ConditionRuleGraph MUST 不引用 Network Model、model policy、model packet、endpoint、transport、SessionHost 或 model runtime。网络可见结果 MUST 只来自显式 gameplay facts，并由 Character fact stage 与 model-owned adapter 接入当前模型。BTSMTL MUST 不因 endpoint 或 Network Model 改变而修改 edge 条件执行。

#### Scenario: LocalLoopback 下执行 Selector

- **WHEN** Session 使用 ServerAuthoritativeHybrid + LocalLoopback
- **THEN** Selector MUST 只按 edge condition 和 AbortPolicy 执行
- **AND** MUST 不访问 model endpoint 或 packet queue

#### Scenario: 未来接入 Fantasy endpoint

- **WHEN** 后续 change 将 endpoint 改为 Fantasy
- **THEN** BT edge runtime MUST 不需要修改
- **AND** gameplay facts MUST 继续从正式 Character 边界进入同一模型 adapter


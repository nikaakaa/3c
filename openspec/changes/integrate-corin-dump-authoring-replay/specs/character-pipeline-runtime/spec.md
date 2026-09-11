## ADDED Requirements

### Requirement: Corin Actor注册必须绑定同一配置和产物身份

Character runtime注册Corin Actor时 MUST 同时绑定Definition identity、Program identity/hash、Projection identity/hash、Numeric Target、Session Composition和World/Prefab binding。运行时 MUST 拒绝混用不同Corin配置或不同Target的产品。

#### Scenario: Actor注册身份不一致

- **WHEN** Corin Prefab引用的Program、Projection、Definition或Session身份不一致
- **THEN** Actor registration MUST 失败并输出每个身份的差异

## ADDED Requirements

### Requirement: ServerAuthoritative恢复状态必须覆盖VerticalVelocity

Prediction History、Authority Baseline、Checkpoint、Canonical Egress、Baseline merge与HardRecovery MUST保存、比较并恢复每个owner Body的VerticalVelocity。Correction restore/replay MUST从恢复后的VerticalVelocity继续Body Motion Prepare。受影响schema MUST单路升级并拒绝旧payload；系统 MUST不以零、actual Velocity.Y、Grounded或客户端Transform补齐缺失字段。

#### Scenario: Authority纠正下落中的Owner

- **WHEN** Authority Baseline包含与本地prediction不同的VerticalVelocity
- **THEN** Body误差裁决 MUST把该差异纳入正式恢复状态
- **AND** restore/replay下一Tick MUST从Authority VerticalVelocity继续积分


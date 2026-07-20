## ADDED Requirements

### Requirement: Program必须声明Body Motion descriptor与能力身份

Target Program MUST保存由Definition正式Profile降低得到的Body Motion descriptor，包括GravityAcceleration、MaximumFallSpeed与semantic version，并 MUST将其纳入canonical bytes、ProgramHash、source revision和required world capabilities。Float32与Fixed Program MUST从同一numeric-neutral descriptor产生各自Target数值payload。Program runtime MUST不读取authoring Profile、Scene默认或Network Model配置补齐descriptor；旧ABI或缺失descriptor的artifact MUST被拒绝。

#### Scenario: Fixed Program降低Body Motion配置

- **WHEN** Compiler从同一Semantic IR生成Fixed Program
- **THEN** GravityAcceleration与MaximumFallSpeed MUST按Fixed Target规则降低
- **AND** Program MUST要求AirborneVerticalMotion
- **AND** descriptor或semantic version变化 MUST形成新的Program identity


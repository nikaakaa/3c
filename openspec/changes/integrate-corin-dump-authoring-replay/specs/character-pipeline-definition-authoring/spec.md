## ADDED Requirements

### Requirement: CharacterPipelineDefinition必须是Corin配置闭包根

Corin的CharacterPipelineDefinition MUST唯一装配Control、Input、Skill、Gameplay、Motion、Presentation、Session和来源绑定。Graph artifact、领域／表现binding和Replay数据 MUST作为Definition的派生产物或验证输入记录，不能反向成为Definition引用源。

#### Scenario: Definition闭包可重建

- **WHEN** 使用同一Definition和同一来源manifest重新导出Corin配置
- **THEN** 系统 MUST 得到相同的稳定作者identity和可比较的依赖闭包
- **AND** 不得依赖目录顺序、显示名或上一次Build残留

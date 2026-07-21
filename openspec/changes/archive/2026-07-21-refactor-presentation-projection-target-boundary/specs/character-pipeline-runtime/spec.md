## ADDED Requirements

### Requirement: Character Presentation 装配必须使用唯一 Target-Neutral Contract

每个Character Presentation Host或Remote Presentation Adapter MUST先严格加载所属Numeric Target Program或正式semantic producer manifest，再通过对应Adapter生成不可变`CharacterPresentationSemanticContract`。`CharacterPresentationProjectionAsset` MUST只提供一个按该contract加载Projection的Interface，并 MUST精确校验ProgramId、Gameplay SourceRevision、SemanticHash、ContractHash与ordered producer contract。Float32、Fixed、Rollback、Preview与Remote Presentation MUST不维护不同Projection匹配规则，也 MUST不按ProgramHash、NumericProfile或ABI选择Presentation资源。

Numeric Target Program MUST继续由ProgramAsset、Catalog与Session composition精确校验ProgramHash、LayoutHash、NumericProfile、Target ABI和State codec。Presentation contract校验 MUST不替代或放宽该Program校验。

#### Scenario: Float32 Host创建Presentation

- **WHEN** Float32 Host已严格加载Float32 Program
- **THEN** Float32 Adapter MUST生成Presentation contract并通过唯一Projection Load Interface创建Presentation
- **AND** Projection MUST不读取Float32 ProgramHash或ABI

#### Scenario: Fixed Host创建Presentation

- **WHEN** Fixed Host已严格加载Fixed Program
- **THEN** Fixed Adapter MUST生成与Frontend相同的Presentation contract并通过同一Projection Load Interface创建Presentation
- **AND** Host MUST不手工拼接producer identity数组或调用较弱校验分支

#### Scenario: Target Program producer contract不一致

- **WHEN** 任一Target Program的ordered producer contract与Projection ContractHash不一致
- **THEN** Character Host MUST在创建Presentation和注册Actor之前失败
- **AND** MUST不按ProgramId、名称、旧Projection或部分producer集合继续运行

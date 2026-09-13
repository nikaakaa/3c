## REMOVED Requirements

### Requirement: Compiler Diagnostics 与 Agent 必须复用正式 Frontend 和 Target 阶段

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“Compiler Diagnostics 与C#作者调用必须复用正式 Frontend 和 Target 阶段”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

## ADDED Requirements

### Requirement: Character generated product必须使用精确独立Build生命周期

系统 MUST在同一Unity MCP连接中提供`character.build_float32_products`与`character.build_fixed_products`。Float32工具 MUST只接收精确`definition_asset_path`并原子发布该Definition的Float32 wrapper与Presentation Projection。Fixed工具 MUST只接收精确`definition_asset_path`和精确`wrapper_asset_path`并原子发布指定Fixed wrapper与同一Projection。两个工具 MUST拒绝未知参数，MUST不读取selection、扫描目录、猜测Definition或自动触发。

#### Scenario: 作者保存后显式重建Corin产物

- **WHEN** 正式authoring资产保存成功且调用方传入精确Corin Definition路径
- **THEN** Float32工具 MUST发布该Definition的Float32 wrapper与Projection
- **AND** Fixed工具 MUST只把Fixed wrapper发布到调用方指定destination
- **AND** response MUST返回Program、Projection、Numeric ABI、hash与编译诊断

### Requirement: Compiler Diagnostics 与C#作者调用必须复用正式 Frontend 和 Target 阶段

Definition diagnostics、C#作者调用 和其它 Editor caller MAY执行不发布 Program/Projection 的 dry-run，但 MUST复用正式 Authoring Discovery、Semantic Emission、artifact codec 和 Target Compiler。Dry-run result MUST以 artifact descriptor/identity 和分阶段 report 表达 Semantic 成功，不得依赖旧 `CharacterSimulationCompileResult.SemanticIr` 直通对象，也不得维护第二个 validator operation table。

#### Scenario: C#作者调用检查Corin

- **WHEN** C#作者调用 对修改后的 Corin authoring 执行正式编译校验
- **THEN** MUST通过同一 Frontend 生成并校验 Semantic artifact payload
- **AND** MUST不自行发射 Semantic operations 或直接调用 raw Float32 lowerer

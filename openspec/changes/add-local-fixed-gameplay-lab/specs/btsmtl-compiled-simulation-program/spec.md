## ADDED Requirements

### Requirement: Program SourceMap必须canonical保存作者内容hash

`ProgramSourceMapEntry` MUST保存稳定作者identity、执行target、display route与作者容器content hash。该content hash MUST进入Semantic IR canonical payload、Float Program canonical payload与Fixed Program canonical payload，并参与SemanticHash及各Target ProgramHash。SourceMap payload结构变化 MUST提升Semantic IR与Target Program artifact版本；旧artifact MUST被拒绝并通过正式Frontend与Target Compiler重新生成，MUST不提供旧reader、字段猜测或兼容转换。

#### Scenario: 同一Timeline生成Float与Fixed Program

- **WHEN**同一validated Semantic IR分别降低为Float与Fixed Program
- **THEN**两份Program SourceMap MUST保存相同Timeline identity与content hash
- **AND**两份Program MAY拥有不同ProgramHash但 MUST能与同一Timeline authoring fingerprint精确匹配

#### Scenario: 加载缺少作者内容hash的旧Program

- **WHEN**Host或Editor读取旧SourceMap payload版本
- **THEN**artifact codec MUST在创建Runtime Debug Source Map前拒绝该artifact
- **AND**MUST不把ProgramHash填入缺失字段继续运行

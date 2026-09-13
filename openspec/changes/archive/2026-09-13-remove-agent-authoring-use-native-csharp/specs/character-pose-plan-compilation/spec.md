## MODIFIED Requirements

### Requirement: 每种Pose节点必须只有一个Node Definition真相

Editor MUST提供唯一`CharacterPoseNodeDefinitionModule`，并为每个正式Node Kind注册恰好一个`CharacterPoseNodeDefinition` Adapter。Definition MUST集中声明Capability identity、Payload类型、字段合同、固定端口、条件`portVariants`、动态端口政策、允许Graph Role、Execution Domain、Operation Family、Authoring codec、Graph dependency投影、局部Payload/Rig校验、typed lowering和Source Map命名。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Canvas、C#作者API、Clipboard、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency与typed lowering。系统不得复制第二字段表、端口表、compiler binding或NodeKind特例目录。

#### Scenario: 新增正式Pose节点

- **WHEN** 项目增加一个新的正式Pose Node Kind
- **THEN** 人工与C#作者创建、Clipboard、统一Port Shape、Graph dependency、局部校验和typed lowering MUST由同一个Definition Adapter及其Capability投影提供
- **AND** 缺少任一必要合同 MUST使Editor初始化或Character Build失败而不得由调用方补默认逻辑

#### Scenario: 同一Node Kind重复定义

- **WHEN** Definition Module发现两个Adapter声明相同Node Kind或Capability identity
- **THEN** 系统 MUST拒绝目录并报告两个定义来源
- **AND** MUST不按注册顺序选择其中一个

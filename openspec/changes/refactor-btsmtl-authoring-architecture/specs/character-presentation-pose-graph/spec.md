## MODIFIED Requirements

### Requirement: State-local source必须由Profile binding和provider解析

`ClipPlayer`、`BlendSpacePlayer`与`SelectedPosePlayer` MUST引用类型匹配的Graph-owned Source Slot对象。Projection Compiler MUST从精确Definition/Profile解析唯一Binding；Clip Binding MUST直接提供AnimationClip。ClipPlayer MUST只保存Source Slot、Play Rate、Initial Time与Clock Source，不得保存Loop或Topology副本；Finite/Cyclic MUST只从AnimationClip正式Loop设置编译。Provider MUST发布带dense source index、generation、Projection revision与frame lease的`PresentationPoseSourceSample`。Pose Graph MUST不保存Sequence、AnimationClip副本、作者source字符串或Gameplay producer。

#### Scenario: ClipPlayer首次采样Idle

- **WHEN** Idle State的ClipPlayer获得entry relevance
- **THEN** provider MUST从Profile direct Clip Binding发布Ready sample
- **AND** Player MUST不解析Sequence或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段

- **WHEN** 人工Capability或Document v5为ClipPlayer提供Loop、Topology或等价override
- **THEN** typed parser或Validator MUST在Compiler前拒绝该字段
- **AND** MUST不覆盖AnimationClip正式Loop设置


### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST继续使用共享typed domain document。每个正式Node Kind MUST通过唯一`CharacterPoseNodeDefinition` Adapter声明Payload字段、固定端口、条件`portVariants`、动态端口政策、Graph Role、Execution Domain、Operation Family、Graph dependency与typed lowering。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Canvas、Document v5 Exporter/strict parser/Target Mapper、Clipboard、Reconciler、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency、typed lowering与Source Map。系统 MUST不保留第二节点目录、重复字段switch、`ICharacterPoseCompilerHandler`布尔能力矩阵或独立Compiler binding真相。跨节点拓扑规则 MUST只属于唯一Topology Pass。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、Document v5、Clipboard、统一Port Shape、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少Document投影

- **WHEN** 一个Definition无法为正式Document/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** Agent authoring MUST不使用通用SerializedProperty或自由文本绕过

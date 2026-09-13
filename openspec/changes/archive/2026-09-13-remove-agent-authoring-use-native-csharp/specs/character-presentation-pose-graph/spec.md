## MODIFIED Requirements

### Requirement: State-local source必须由Profile binding和provider解析

`ClipPlayer`、`BlendSpacePlayer`与`SelectedPosePlayer` MUST声明类型匹配的Graph-owned Source Slot，原生资源由Character Presentation Profile Binding提供。Projection Compiler MUST从Graph Slot与Profile Binding生成dense source usage；ClipPlayer MUST保存Source Slot、Play Rate、Initial Time、Loop与Clock Source，不得保存Character资源、Profile Binding或Topology副本。Provider MUST发布带dense source index、generation、Projection revision与frame lease的`PresentationPoseSourceSample`。Pose Graph MUST不保存Sequence包装资产、作者source字符串或Gameplay producer。

#### Scenario: ClipPlayer首次采样Idle

- **WHEN** Idle State的ClipPlayer获得entry relevance
- **THEN** provider MUST从Profile direct Clip Binding发布Ready sample
- **AND** Player MUST不解析Sequence或AssetDatabase

#### Scenario: ClipPlayer提交Loop字段

- **WHEN** 人工Capability或C#作者API为ClipPlayer提供合法Loop策略
- **THEN** Compiler MUST按该Player usage编译Finite或Cyclic时间行为
- **AND** MUST不复制素材曲线或创建Sequence包装资产

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

UI MUST使用Clip Player、Blend Space Player、Selected Pose Player、Animation State Machine、Slot、Layered Blend Per Bone、Inertialization、Locomotion Phase Group、Pose Watch和Output Pose等准确术语。序列化、C#作者API、Mutation、Compiler source map和Diagnostics MUST使用同一Clip命名；MUST不保留Clip Player显示名或旧node kind alias。

#### Scenario: 作者添加单Clip播放器

- **WHEN** 作者在Pose Graph添加单Clip state-local player并选择类型匹配的Source Slot
- **THEN** Capability、节点标题、C#作者API kind和编译诊断 MUST统一显示Clip Player
- **AND** MUST不存在Clip Player兼容名称

### Requirement: Pose StateMachine layout必须是独立纯作者数据

每个root-owned PoseStateMachine MUST在`CharacterPresentationPoseGraphAsset`中拥有按稳定`PoseStateMachineId`索引的唯一layout owner。Layout MAY稀疏保存Entry、State与Alias的显式二维位置；缺少显式位置时 MUST按元素类型和稳定identity使用唯一确定性排布。Layout MUST拒绝重复identity、未知元素和非有限坐标，且 MUST不保存Transition edge位置。Layout变化 MUST进入typed Presentation Mutation、Undo、dirty、保存，但 MUST不修改PoseStateMachine `ContentRevision`、不得使Presentation Projection变为Stale，也不得触发Compile或Build。Compiler与Runtime MUST不读取layout。

#### Scenario: 作者拖动Locomotion State

- **WHEN** 作者把Pose StateMachine中的Locomotion State拖到新位置
- **THEN** 系统 MUST通过Pose StateMachine layout Mutation保存该State的稳定identity与位置
- **AND** 重新打开工作区后 MUST从同一layout owner恢复位置
- **AND** Pose StateMachine运行语义与Projection revision MUST保持不变

#### Scenario: 现有State没有显式位置

- **WHEN** 现有Pose StateMachine layout没有某个State的显式位置
- **THEN** 工作区 MUST按稳定identity使用唯一确定性位置
- **AND** MUST不在打开窗口、selection变化或AssetDatabase刷新时自动保存生成位置

#### Scenario: layout引用已删除State

- **WHEN** layout包含当前Pose StateMachine中不存在的State identity
- **THEN** Validator MUST报告悬空layout元素并拒绝正式提交
- **AND** MUST不忽略该元素或按显示名重绑定

## REMOVED Requirements

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“Pose authoring必须通过正式Capability和Presentation Mutation”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

## ADDED Requirements

### Requirement: Pose authoring必须通过正式Capability和Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST继续使用共享typed domain document。每个正式Node Kind MUST通过唯一`CharacterPoseNodeDefinition` Adapter声明Payload字段、固定端口、条件`portVariants`、动态端口政策、Graph Role、Execution Domain、Operation Family、Graph dependency与typed lowering。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Canvas、C#作者API、Clipboard、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency、typed lowering与Source Map。系统 MUST不保留第二节点目录、重复字段switch、`ICharacterPoseCompilerHandler`布尔能力矩阵或独立Compiler binding真相。跨节点拓扑规则 MUST只属于唯一Topology Pass。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、C#作者API、Clipboard、统一Port Shape、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少C#作者API投影

- **WHEN** 一个Definition无法为正式C#作者API/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** C#作者API MUST不使用通用SerializedProperty或自由文本绕过

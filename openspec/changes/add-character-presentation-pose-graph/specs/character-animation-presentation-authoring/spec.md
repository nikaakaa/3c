## MODIFIED Requirements

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`。Profile MUST唯一引用`CharacterPresentationPoseGraphAsset`、`CharacterAnimationBlendLibrary`、`CharacterAnimationRigDefinition`，保存稳定producer resource bindings，以及显式Foot Placement Analysis Mode与Analysis Source Asset GUID。Pose Graph MUST唯一保存Pose Slot声明、AnimationChannelId binding、Bone Mask composition、Pose Parameter policy与Output topology；Blend Library MUST唯一保存每slot transition数据。Profile MUST不保存Layer catalog、Animancer TransitionLibrary或内联Pose Graph副本。Graph、StateMachine、Timeline、Presenter、Prefab重复数值、旧SO或独立Pipeline表 MUST不保存同一配置的第二份真相。

#### Scenario: Corin配置Pose Graph

- **WHEN** Corin Profile引用正式Pose Graph、Blend Library、Rig Definition和Foot Analysis Source
- **THEN** Profile validation MUST能精确解析四个identity
- **AND** Definition MUST不内联复制slot、node、transition、bone或analysis字段

#### Scenario: shared BTSMTL Graph被两个角色使用

- **WHEN** 两个Definition复用同一BTSMTL Graph或Timeline
- **THEN** 两个Profile MAY引用不同Pose Graph、Blend Library、Rig和Analysis Source
- **AND** BTSMTL Graph/Timeline MUST不保存任一角色的Pose composition配置

### Requirement: CharacterAnimationPresentationProfile Inspector 必须是唯一 Presentation 配置入口

系统 MUST在`CharacterAnimationPresentationProfile` Inspector中唯一编辑Pose Graph、Blend Library、Rig Definition、producer resource binding、Foot Analysis Mode和Analysis Source GUID。Pose Slot、Bone Mask、Additive、Pose Parameter和Output topology MUST从该Inspector进入正式Pose Graph Editor编辑；transition matrix MUST从该Inspector进入Blend Library Inspector编辑。Timeline Editor继续唯一编辑producer-local Clip、Marker与registered Curve。Profile Inspector MUST不恢复Layer catalog、Animancer TransitionLibrary字段或第二张producer flow graph。

#### Scenario: 从Profile打开Pose Graph

- **WHEN** 作者在Corin Definition context选择Open Pose Graph
- **THEN** Editor MUST通过共享Graph Authoring Editor Shell打开Profile引用的唯一Pose Graph asset
- **AND** Undo/dirty MUST作用于Pose Graph真实owner

#### Scenario: shared Timeline用于不同Pose Graph

- **WHEN** 两个Profile复用同一Timeline但使用不同Pose Graph、Rig或Analysis Source
- **THEN** 各自ProjectionRevision MUST独立计算
- **AND** shared Timeline MUST不保存任一角色的slot、mask或Rig数据

### Requirement: Equipment Presentation 不得拥有动画空间拓扑

Equipment Feature authoring MUST不保存LayerId、BlendMode、OutputPolicy或Presentation producer requirement。`EquipmentFeatureRouteImplementation.RequiredProducerIds` MUST仅表达Gameplay route完整性，MUST不进入Presentation Projection的channel、slot、transition或Animancer resolution。Equipment Presentation Profile与Projection MUST只保存VisualBinding、Prefab/socket、Renderer登记与local pose资源绑定，MUST不改名或复制旧字段为PoseSlotId，也 MUST不提前创建Equipment到Pose Graph的动态替换接口。

#### Scenario: Equipment Feature声明Gameplay route producer

- **WHEN** Equipment route使用RequiredProducerIds校验Gameplay Graph实现完整性
- **THEN** Semantic/Gameplay compiler MAY保留该纯route依赖
- **AND** Projection Compiler MUST不把它解释为AnimationChannel、PoseSlot或表现层producer binding

#### Scenario: 武器需要动态替换Pose Graph

- **WHEN** 未来武器业务需要替换整段Pose实现
- **THEN** 系统 MUST由独立change定义正式输入、Projection schema与Runtime生命周期
- **AND** 当前Equipment Visual链 MUST不提供passthrough、兼容Layer或临时PoseSlot接口

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

`CharacterAnimationPresentationProfile` Inspector MUST在显式Definition context下，从正式Projection读取可达animation producer，并按stable identity显示AnimationChannelId、PoseSlotId、来源Timeline与resource binding。Inspector MUST不重新编译BTSMTL Graph、不推导StateMachine producer flow、不保存Tree node/edge副本，也 MUST不复制Pose Graph topology到列表。Runtime MUST不依赖该可视列表做selection、transition或composition。

#### Scenario: 查看Attack1与RunLoop

- **WHEN** 作者检查包含Attack1与RunLoop的Corin Definition
- **THEN** Inspector MUST显示Attack1属于FullBodyAction/FullBodyActionSlot且RunLoop属于BaseLocomotion/BaseLocomotionSlot
- **AND** MUST不把Pose Graph全身覆盖关系显示成Gameplay edge

#### Scenario: binding指向未知slot

- **WHEN** producer channel无法解析到Pose Graph slot
- **THEN** Inspector MUST显示Projection invalid diagnostic
- **AND** MUST不按producer名称或旧LayerId猜测slot

### Requirement: Marker Group 必须在 Projection 构建前完整校验

MarkerGroup的Timeline duration MUST为有限正值；MarkerAuthoringId和frame MUST在track内唯一，MarkerId MUST非空且 MAY重复。每个相邻marker MUST形成非零有向segment，AnimationTrack在marker覆盖区内 MUST持续产生正式animation output。同一AnimationChannelId、PoseSlotId与canonical SyncGroupId内的所有producer MUST拥有相同有向marker pair集合。Marker Group MUST不跨channel/slot借助Pose Graph共同可见期建立同步。Inspector、Compiler、Projection Builder和Agent Validator MUST复用唯一校验服务。

#### Scenario: Walk与Run同slot同步

- **WHEN** WalkLoop和RunLoop属于BaseLocomotion/BaseLocomotionSlot并拥有相同有向marker pair集合
- **THEN** Compiler MUST接受不同时序的各自marker occurrence
- **AND** Projection MUST保存各producer自己的映射

#### Scenario: Attack与Locomotion误用同组

- **WHEN** FullBodyActionSlot的Attack与BaseLocomotionSlot的Run声明相同SyncGroupId
- **THEN** Compiler MUST报告跨slot同步错误
- **AND** MUST不依赖Pose Graph全身mask建立handoff

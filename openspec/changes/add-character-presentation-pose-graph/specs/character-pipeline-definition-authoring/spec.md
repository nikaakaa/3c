## MODIFIED Requirements

### Requirement: CharacterPipelineDefinition 必须是配置装配根

`CharacterPipelineDefinition` MUST只保存RootTree、SimulationTickRate、InputProfile、GameplayEffectProfile、ActionProfile、GameplayBehaviorProfile、CharacterAnimationPresentationProfile与generated Program/Projection的正式引用。Definition MUST不内联保存Animation Channel、Pose Slot、Pose Graph、Blend Library、Rig、producer binding、Graph、Timeline、runtime lifecycle或compiler report数据。

#### Scenario: 打开角色Definition

- **WHEN** 作者选择Corin CharacterPipelineDefinition
- **THEN** Inspector MUST优先显示角色引用的正式Config
- **AND** MUST不平铺Pose Slot、Pose节点、transition matrix、producer binding或Program Hash

#### Scenario: 缺失动画表现Profile

- **WHEN** Definition没有CharacterAnimationPresentationProfile引用
- **THEN** configuration validation与Compiler MUST报告明确错误
- **AND** 系统 MUST不创建内联Profile、默认Pose Graph或从Blend Library猜测配置

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、node-local Blend/Inertialization Policy与Rig Definition，并保存稳定producer resource bindings、Foot Placement Analysis Mode与Analysis Source Asset GUID。AnimationChannel Selection Input、Player、Bone Mask、Additive、Pose Parameter、FootPlacement和Output topology MUST只保存在Pose Graph；Stack Policy与Inertialization Policy作者数据 MUST只由对应显式节点引用。Definition、BTSMTL Graph、Timeline、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些配置的可写副本。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Blend Library、Rig、producer binding和Foot Analysis Source唯一入口
- **AND** Pose topology mutation MUST作用于Pose Graph真实owner而不是Profile镜像字段

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Animation Presentation Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler或内联显示node/mask参数

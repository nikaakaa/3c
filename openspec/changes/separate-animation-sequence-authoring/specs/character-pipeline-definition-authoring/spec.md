## MODIFIED Requirements

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、PoseStateMachine topology、node-local Blend/Inertialization Policy、角色Rig、Profile-owned typed Source Binding、有限Action producer binding及其它角色级表现配置。Pose Graph MUST唯一拥有typed Source Slot子资产，并保存Presentation Fact Input、PoseStateMachine、SequencePlayer、BlendSpacePlayer、SelectedPosePlayer、ActionPlaybackInput、AnimationSlot、Player、Mask、Additive、Pose Parameter、TwoBoneIK、LocalToComponentPose、FootPlacement、typed双腿targets、LegIK、ComponentToLocalPose与Output topology；Player只引用精确Source Slot对象，Policy MUST只由对应transition owner或节点引用。Graph-owned Sequence Source Slot的Binding MUST只引用精确Animation Sequence；Sequence自身唯一保存AnimationClip、Rig、Loop/Finite、Marker、素材Curve、Notify与Analysis Source。Blend Space sample与Action Timeline Segment同样只引用Sequence。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline Track、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些作者配置或Sequence素材的可写副本。

#### Scenario: Definition装配Run Sequence

- **WHEN** Corin Profile的Run Binding引用正式Run Sequence
- **THEN** Definition Inspector MUST显示Profile、Binding与Sequence引用关系
- **AND** MUST不内联显示可写Marker、Curve或Notify正文

#### Scenario: Sequence Rig不兼容

- **WHEN** Binding引用的Sequence Rig与Profile角色Rig不兼容
- **THEN** Definition validation MUST定位Binding与Sequence并失败
- **AND** MUST不在Binding保存Rig override

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Sequence/其它source binding、Action producer、Policy与角色Rig入口
- **AND** Definition Inspector MUST不内联这些字段或Sequence素材正文

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler或内联显示Sequence Marker/Curve

## MODIFIED Requirements

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、PoseStateMachine topology、node-local Blend/Inertialization Policy与Rig Definition，保存Profile-owned typed Source Binding子资产、有限Action producer binding、显式Foot Placement Analysis Mode、Analysis Source对象引用与Locomotion Sync Group。Pose Graph MUST唯一拥有typed Source Slot子资产，并保存Presentation Fact Input、PoseStateMachine、ClipPlayer、BlendSpacePlayer、SelectedPosePlayer、ActionPlaybackInput、AnimationSlot、Player、Mask、Additive、Pose Parameter、TwoBoneIK、LocalToComponentPose、FootPlacement、typed双腿targets、LegIK、ComponentToLocalPose与Output topology。Clip Binding MUST直接引用AnimationClip；Blend Space和Timeline MAY只通过各自正式owner直接引用AnimationClip。Profile、Binding与Timeline MUST不复制素材Curve、Marker、Rig或Analysis配置。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些角色级装配配置的可写副本。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Clip source、Action producer binding、Locomotion Sync Group、Policy、Rig和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Animation Presentation Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler或内联显示node、Clip、Group或mask参数

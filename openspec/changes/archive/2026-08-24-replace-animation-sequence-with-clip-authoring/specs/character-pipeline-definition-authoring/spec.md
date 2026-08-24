## MODIFIED Requirements

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、PoseStateMachine topology、node-local Blend/Inertialization Policy、角色Rig Definition与FullBodyIK Profile，保存Profile-owned typed Source Binding子资产、有限Action producer引用、显式Foot Placement Analysis Mode、Analysis Source对象引用与Locomotion Sync Group。Pose Graph MUST唯一拥有typed Source Slot子资产，并保存Presentation Fact Input、PoseStateMachine、ClipPlayer、BlendSpacePlayer、SelectedPosePlayer、ActionPlaybackInput、AnimationSlot、Player、Mask、Additive、Pose Parameter、LocalToComponentPose、Component Pose controls、FootPlacement、PoseBoneIKGoals、Goal Assembler、FullBodyIK、ComponentToLocalPose与Output topology。Clip Binding MUST直接引用AnimationClip；Blend Space和Timeline MAY只通过各自正式owner直接引用AnimationClip。Clip Binding、Action producer binding与Timeline MUST不复制素材注册Curve、角色Rig或Analysis identity；Action producer binding MUST只保存producer到Timeline/Track的正式引用。Blend Space与Motion Matching资源内部Artifact compatibility identity只用于校验与Profile角色配置一致，不得成为第二角色配置owner。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些角色级装配配置的可写副本。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Clip source、Action producer binding、Locomotion Sync Group、Policy、Rig、FullBodyIK和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: Action producer解析Foot Analysis

- **WHEN** Definition Build编译一个直接AnimationClip的有限Action producer
- **THEN** Compiler MUST从Profile Analysis Source、角色Rig与Clip Analysis Input Hash解析Artifact
- **AND** Action producer binding MUST不保存Foot Analysis identity副本

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Animation Presentation Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler或内联显示node、Clip、Group或mask参数

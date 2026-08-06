## MODIFIED Requirements

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、PoseStateMachine topology、node-local Blend/Inertialization Policy、Rig Definition v4、唯一Foot Placement Profile与FullBodyIK Profile，保存Profile-owned typed Source Binding子资产、有限Action producer source binding，以及显式Foot Placement Analysis Mode与Analysis Source对象引用。Pose Graph MUST唯一拥有typed Source Slot子资产，并保存Presentation Fact Input、PoseStateMachine、SequencePlayer、BlendSpacePlayer、SelectedPosePlayer、ActionPlaybackInput、AnimationSlot、Player、Mask、Additive、Pose Parameter、LocalToComponentPose、PoseBoneIKGoals、PredictiveFootPlacement、typed Full Body IK Goals、FullBodyIK、ComponentToLocalPose与Output topology；Player只引用精确Source Slot对象，Binding保存resource、marker、source-local Foot Placement Weight与analysis配置。Policy MUST只由对应transition owner或节点引用。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些作者配置的可写副本，也 MUST不保存FinalIK组件、Grounder配置、BipedReferences或旧TwoBone/LegIK配置。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Pose source、Action producer binding、Policy、Rig v4、Foot Placement Profile、FullBodyIK Profile和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: Definition Inspector显示Projection状态

- **WHEN** 作者只选择CharacterPipelineDefinition
- **THEN** Inspector MUST只显示Animation Presentation Profile引用与Projection Ready/Stale/Missing摘要
- **AND** MUST不运行Pose Graph Compiler、FinalIK Rig validation或内联显示solver参数

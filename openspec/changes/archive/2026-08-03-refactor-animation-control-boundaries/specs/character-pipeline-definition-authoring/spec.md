# character-pipeline-definition-authoring Specification

## MODIFIED Requirements

### Requirement: Animation Presentation Profile 必须是唯一表现配置资产

`CharacterAnimationPresentationProfile` MUST作为ScriptableObject唯一引用Pose Graph、PoseStateMachine topology、node-local Blend/Inertialization Policy与Rig Definition，以typed Binding子资产保存持续Source Slot的resource、marker、source-local Foot Placement Weight与analysis binding、有限Action producer source binding，以及显式Foot Placement Analysis Mode与Analysis Source Asset GUID。Pose Graph MUST拥有typed Source Slot子资产，并唯一保存Presentation Fact Input、PoseStateMachine、SequencePlayer、BlendSpacePlayer、SelectedPosePlayer、ActionPlaybackInput、AnimationSlot、Player、Mask、Additive、Pose Parameter、TwoBoneIK、FootPlacement与Output topology；Marker时间映射 MUST只属于PoseState Transition或AnimationSlot的source-local plan，不得保存独立MarkerSync节点。Policy MUST只由对应transition owner或节点引用。Definition、Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter、Program、Runtime Prefab或独立EditorWindow MUST不保存这些作者配置的可写副本。

#### Scenario: 一个Profile被一个Definition引用

- **WHEN** 作者选择CharacterAnimationPresentationProfile
- **THEN** Profile Inspector MUST提供Pose Graph、Pose source、Action producer binding、Policy、Rig和Foot Analysis唯一入口
- **AND** Definition Inspector MUST不内联这些字段

#### Scenario: 多个Definition共享Profile

- **WHEN** 多个Definition引用同一Profile
- **THEN** 每个Definition/Profile/Projection组合 MUST独立校验Gameplay producer与Source Slot/Binding对象闭包
- **AND** Profile MUST不保存反向Definition owner引用

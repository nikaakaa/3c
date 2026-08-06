## MODIFIED Requirements

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存动画表现数据。Profile MUST唯一引用Pose Graph、Profile-owned Pose source binding子资产、有限Action producer source binding、node-local Policy、Rig v4、Foot Placement Profile、FullBodyIK Profile与Foot Analysis配置。Foot Placement Profile MUST在同一资产中分组FinalIK Grounding-backed设置和Project Predictive Extension设置，不得保存backend选择、fallback或Grounder组件副本。Pose Graph MUST唯一保存Presentation Fact Input、PoseStateMachine、Graph-owned Source Slot子资产、SequencePlayer、AnimationSlot、Selection Player、composition、PoseBoneIKGoals、PredictiveFootPlacement、FullBodyIK与Output topology。Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter与Prefab MUST不复制这些配置或挂载正式FinalIK组件。

#### Scenario: Corin配置动画表现

- **WHEN** Corin Definition引用正式Animation Presentation Profile
- **THEN** Profile MUST为PoseStateMachine Source Slot、Action Slot producer、Rig v4、Foot Placement Profile和FullBodyIK Profile提供唯一资源绑定
- **AND** Definition MUST不内联Run Clip、State transition、IK chain或solver policy

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile、Rig v4、Foot Placement Profile、FullBodyIK Profile和Analysis Source
- **AND** 每个Profile MUST为shared Source Slot对象提供自己的binding子资产
- **AND** shared Graph/Timeline MUST不保存角色级资源、分析Rig、校准或FinalIK BipedReferences

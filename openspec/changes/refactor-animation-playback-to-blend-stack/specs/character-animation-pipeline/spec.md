## ADDED Requirements

### Requirement: Blend Stack必须固定装配在每个Pose Slot输入之前

每个已编译PoseSlotId MUST固定装配唯一AnimationBlendStackRuntime与AnimationSlotBlendPoseEvaluator，并由Pose Graph唯一PoseSlotInput读取其PoseSlotFrame。Blend Stack MUST不成为BTSMTL Graph、StateMachine、Timeline或Pose Graph中的可选节点；作者 MUST不在edge、producer或graph branch重复配置Stack开关。Pose Graph MUST不能直接连接AnimationClip或source backend绕过Stack。

#### Scenario: ALS状态切换

- **WHEN** Program改变BaseLocomotion channel selection
- **THEN** 新playback MUST自动经过BaseLocomotionSlot唯一Stack
- **AND** 作者 MUST不额外放置Blend Stack、Stored或Inertial节点

#### Scenario: 后续Motion Matching接入

- **WHEN** Motion Matching成为某AnimationChannel的上游producer
- **THEN** 其request MUST复用该PoseSlot Stack与Pose Graph
- **AND** MUST不建立私有crossfade或绕过Stack

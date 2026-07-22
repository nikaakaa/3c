## MODIFIED Requirements

### Requirement: 预览采样必须复用正式动画播放链路

纯动画Timeline Preview MUST通过`CharacterPresentationProjection`将稳定producer identity解析为AnimationChannelId、PoseSlotId和source资源，并复用正式CommandQueue、AnimationPlaybackLifecycle、PoseSlot Blend Stack、Animancer source backend、Pose Graph native job与final frame publisher。Preview session MUST为每个AnimationChannelId生成零或一个独立preview EventId/playback generation的command和pose request；它 MUST不比较Priority、直接播放Clip、跳过Stack或实现第二套Pose composition。

#### Scenario: 当前时间采样

- **WHEN** preview time位于BaseLocomotion与FullBodyAction AnimationTrack范围
- **THEN** session MUST分别提交两个channel的唯一preview command与sample
- **AND** Pose Graph MUST按正式slot binding生成最终preview pose

#### Scenario: 同channel多个producer

- **WHEN** 一次preview evaluation发现多个producer声明同一AnimationChannelId
- **THEN** session MUST明确拒绝该evaluation
- **AND** MUST不按Track顺序、Priority、Pose Slot或Graph node选择winner

#### Scenario: 非连续seek

- **WHEN** preview time非连续跳转
- **THEN** session MUST retire旧preview EventId并清理对应channel Lifecycle与slot Stack
- **AND** 目标时间 MUST以新generation重建command/sample并重新求值正式Pose Graph

### Requirement: Timeline Live Debug 必须显示正式 Sync Relation

Timeline Live Debug MUST从共享RuntimeDebugSession的正式Animation trace显示source/target PlaybackId、AnimationChannelId、PoseSlotId、canonical SyncGroupId、有向marker pair、source fraction、target occurrence、raw/effective time、effective cycle、relation depth、lifecycle phase与detach/failure reason。Live Debug MAY关联同帧PoseSlotFrame与OutputPose identity，但 MUST不按authoring游标重采样、不推导StateMachine transition、不求值Pose Graph或维护第二份relation状态。

#### Scenario: 观察连续切换

- **WHEN** BaseLocomotion发生`Walk -> Run -> Turn`relation chain
- **THEN** Live Debug MUST按playback generation显示同channel/slot relation与depth
- **AND** 显示值 MUST来自正式runtime snapshot

#### Scenario: Action与Locomotion同时可见

- **WHEN** FullBodyActionSlot覆盖正在同步的BaseLocomotionSlot
- **THEN** Live Debug MUST分别显示两个slot事实与最终Pose Graph贡献
- **AND** MUST不建立跨slot Marker relation

# character-equipment-presentation Specification

## MODIFIED Requirements

### Requirement: Equipment动画必须继续通过Timeline producer提交

Equipment Feature触发的有限Action动画 MUST由正式Action Timeline AnimationTrack产生typed producer command，并经过有限Action channel、`CharacterActionPlaybackRuntime`、AnimationSlot与Pose Graph。Equipment Host、Action runtime和Visual runtime MUST不直接调用Animancer、Animator.Play、CrossFade或修改Slot/Player weight。持续装备姿态变体若以后接入Locomotion MUST通过独立typed Presentation Fact或Pose source selection设计，不得伪装成常驻Timeline producer。

#### Scenario: 装备动作播放

- **WHEN** Equipment Gameplay route激活一个有限换装或武器Action
- **THEN** 动画 MUST通过正式Action Timeline playback进入配置Slot
- **AND** Equipment Visual Runtime MUST不直接播放动画

#### Scenario: 武器改变待机姿态

- **WHEN** 装备状态需要改变持续Idle Pose
- **THEN** 后续能力 MUST通过PoseState source或表现参数明确建模
- **AND** MUST不创建永不结束的Equipment Action Timeline作为Locomotion fallback

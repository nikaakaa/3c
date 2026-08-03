# character-action-authoring-closure Specification

## MODIFIED Requirements

### Requirement: Full-body Action 必须通过唯一 pipeline blackboard 事实公布 locomotion ownership

Attack、Dodge与未来Action MUST通过正式Action/Motion arbitration事实声明是否取得Character Motor控制权。该事实 MAY保存在pipeline Blackboard，但其语义 MUST只控制Locomotion Motion contribution，不得控制基础Pose是否输出。Locomotion PoseStateMachine MUST继续根据committed Body与Intent生成基础Pose。系统 MUST删除`HasActionLocomotionOwnership -> ActionOverride -> RunLoop/Idle`表现路由，MUST不按Action种类选择恢复Pose State。

#### Scenario: Dodge取得Motion authority

- **WHEN** Dodge Action成功激活并提交动作Motion
- **THEN** Motion arbitration MUST阻止冲突Locomotion Motion contribution
- **AND** FullBodyAction Slot MUST独立播放Dodge Pose

#### Scenario: Action结束

- **WHEN** Action terminal lifecycle释放Motion authority和Action playback
- **THEN** Locomotion Gameplay control MUST恢复正常Motor contribution
- **AND** Slot MUST回到当前PoseStateMachine Source Pose而不是Gameplay指定RunLoop或Idle

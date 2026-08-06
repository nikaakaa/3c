## MODIFIED Requirements

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费committed Body/Intent、构造typed Presentation Fact，并消费完整有限Action Selection batch与Parameter page；随后按Projection编译的ordered stage table执行PoseState selection、State source demand/capture、Action playback、Marker time resolve、AnimationSlot、Transition Routing、Local Pose composition、显式Local/Component转换、Component Pose骨骼控制、world-aware FootPlacement规划与pelvis输出、typed双腿targets、pure pose LegIK、后续Pose stage与FinalPublication。只有唯一OutputPose及全部必需stage完成后才可由唯一final writer发布`FinalAnimationPoseFrame`并推进Camera；任一Fact、source、MarkerSync、Player、Slot、转换、Pose operation、world query、Planner、targets validation或LegIK solver失败 MUST阻止部分最终结果发布，不得沿用上一帧、只发布pelvis Pose或绕过节点。

#### Scenario: FootPlacement targets与LegIK Pose不匹配

- **WHEN** 同帧targets CompletionIdentity或Rig revision与LegIK Component Pose输入不一致
- **THEN** PresentationFrame MUST阻断LegIK、后续stage和FinalPublication
- **AND** MUST不使用上一次targets或按节点顺序猜测配对

#### Scenario: 完整Foot Placement链成功

- **WHEN** FootPlacement发布合法pelvis Pose与targets且LegIK完成左右腿求解
- **THEN** FinalAnimationPoseFrame MUST包含LegIK输出及全部后续Pose操作
- **AND** Runtime MUST不保留第二Foot Placement或图外Leg IK结果


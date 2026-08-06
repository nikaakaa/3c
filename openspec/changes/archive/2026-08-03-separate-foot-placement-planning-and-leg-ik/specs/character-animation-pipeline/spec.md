## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Runtime MUST消费committed Body/Intent、Program parameter和有限Action command，构造Presentation Fact，并按Projection编译的ordered staged Pose Plan执行PoseStateMachine、state-local provider demand/readiness、ActionPlaybackInput、AnimationSlot、Local Pose composition、显式`LocalToComponentPose`、Component Pose骨骼控制、world-aware FootPlacement规划与pelvis输出、typed双腿targets、pure pose LegIK、显式`ComponentToLocalPose`、后续Pose stage及FinalPublication。所有Player、Routing、Inertialization、source capture、空间转换、target value和output completion MUST位于同一帧固定计划和同一次PlayableGraph Evaluate。任一stage失败 MUST阻断后续stage与FinalPublication；若已跨过Animancer Evaluate Barrier，同一Actor Animation Runtime MUST进入Faulted且不得逆序恢复状态或Physical Bone快照。Runtime MUST不创建图外基础动画、Stack、FootPlacement、LegIK、隐式Pose空间转换、world-aware postprocess、第二Pose Graph或第二final writer。

#### Scenario: FootPlacement完成后LegIK失败

- **WHEN** FootPlacement已发布pelvis Pose与targets但LegIK报告退化bend plane
- **THEN** Runtime MUST阻断ComponentToLocalPose与FinalPublication并进入正式Faulted路径
- **AND** MUST不发布只有pelvis补偿而没有双腿求解的部分Pose

#### Scenario: 正常Foot Placement表现帧

- **WHEN** FootPlacement与LegIK依次完成且全部completion匹配
- **THEN** Runtime MUST只发布LegIK及后续stage形成的唯一OutputPose
- **AND** MUST不在图外再次执行Foot Placement或腿部solver


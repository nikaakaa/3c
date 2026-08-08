## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Runtime MUST消费committed Body/Intent、Program parameter和有限Action command，构造Presentation Fact，并按Projection编译的ordered staged Pose/Value DAG执行PoseStateMachine、state-local provider demand/readiness、ActionPlaybackInput、AnimationSlot、Local Pose composition、显式`LocalToComponentPose`、Component Pose控制、从同一Component Pose分支执行Lyra Foot Plant等价普通FootGrounding、可选Swing脚PredictiveFootPlacementModifier与PoseBoneIKGoals、汇聚全部最终Goal value到唯一pure pose FullBodyIK、显式`ComponentToLocalPose`、后续Pose stage及FinalPublication。所有Player、Routing、Inertialization、source capture、空间转换、Goal value和output completion MUST位于同一帧固定计划和同一次PlayableGraph Evaluate。Goal Source的调度先后 MUST不被解释为多个IK串行。任一stage失败 MUST阻断后续stage与FinalPublication；若已跨过Animancer Evaluate Barrier，同一Actor Animation Runtime MUST进入Faulted且不得逆序恢复状态或Physical Bone快照。Runtime MUST不创建图外基础动画、Stack、Foot Placement、LegIK、TwoBoneIK、FinalIK Grounding、FinalIK组件、隐式Pose空间转换、world-aware postprocess、第二Pose Graph或第二final writer。

#### Scenario: FootPlacement Goals完成后FullBodyIK失败

- **WHEN** FootGrounding及可选Modifier已发布Body/Feet Goals但FullBodyIK报告Rig mapping或solver failure
- **THEN** Runtime MUST阻断ComponentToLocalPose与FinalPublication并进入正式Faulted路径
- **AND** MUST不发布只有pelvis pre-solve调整、只有手臂或只有单腿完成的部分Pose

#### Scenario: 正常预测式Foot Placement表现帧

- **WHEN** Foot与Hand Goal Source从同一Component Pose完成并且唯一FullBodyIK completion匹配
- **THEN** Runtime MUST只发布FullBodyIK及后续stage形成的唯一OutputPose
- **AND** MUST不在图外再次执行Foot Placement、TwoBoneIK、LegIK或FinalIK

#### Scenario: Commit Attack producer

- **WHEN** Program提交FullBodyAction Attack command
- **THEN** Runtime MUST把Action frame送入绑定的ActionPlaybackInput与AnimationSlot
- **AND** 如何覆盖Source Pose与何时释放 MUST只由compiled Routing Plan决定

#### Scenario: Locomotion target Pending

- **WHEN** PoseStateMachine选择新target但provider尚未Ready
- **THEN** Runtime MUST保持现有合法Source Pose
- **AND** MUST不启动target transition或使用旧Timeline fallback

#### Scenario: Player source槽位复用

- **WHEN** consumer发布retirement permission且backend完成旧source物理释放
- **THEN** Runtime MAY把workspace槽位分配给新source
- **AND** 旧CaptureJob与新CaptureJob MUST不在同一次Evaluate写入同一槽位

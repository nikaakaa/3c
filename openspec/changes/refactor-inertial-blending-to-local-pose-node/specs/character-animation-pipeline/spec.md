## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

唯一编译Pose Plan MUST在native Pose阶段按拓扑执行SelectedPosePlayer、Inertialization、BlendStack与其它Pose节点，并在world-aware阶段执行FootPlacement。Inertialization source capture、residual job与output completion MUST位于同一帧计划和同一次PlayableGraph Evaluate，MUST不建立第二条pose写回路径。

#### Scenario: Locomotion发生连续两次jump

- **WHEN** Player在连续帧发布两个合法Discontinuity
- **THEN** Pose Plan MUST按同一Inertialization节点依次capture和rebase
- **AND** 每帧 MUST只发布一次该节点完成结果


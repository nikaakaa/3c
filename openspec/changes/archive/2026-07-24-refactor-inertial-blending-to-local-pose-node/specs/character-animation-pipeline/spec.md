## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Presentation Runtime MUST消费committed Animation Selection与参数，执行Projection编译的Selection、Player、native pose composition、world-aware postprocess和final publication阶段，并在IK/Solver exact completion后发布唯一`FinalAnimationPoseFrame`。唯一编译Pose Plan MUST在native Pose阶段按拓扑执行SelectedPosePlayer、Inertialization、BlendStack与其它Pose节点；Inertialization source capture、residual job与output completion MUST位于同一帧计划和同一次PlayableGraph Evaluate。Runtime MUST不自动创建图外Stack、图外Foot Placement、第二Pose Graph、第二pose写回路径或第二final writer。

#### Scenario: Commit Attack producer

- **WHEN** Program提交FullBodyAction channel的Attack Selection
- **THEN** Runtime MUST把Selection送入Pose Graph中绑定该channel的输入节点
- **AND** 最终是否经过BlendStack、如何覆盖Base以及是否执行FootPlacement MUST只由编译Pose Plan决定

#### Scenario: Selection经过MarkerSync

- **WHEN** 编译Pose Plan包含`AnimationSelectionInput -> MarkerSync -> BlendStack`
- **THEN** Runtime MUST先生成Player source usage，再由MarkerSync解析effective sample page，最后采样与混合source
- **AND** Timeline sampler MUST保持只提交raw visual time

#### Scenario: SelectedPosePlayer切换复用物理source槽位

- **WHEN** SelectedPosePlayer完成旧source到新source的Marker时间映射并声明旧source release
- **THEN** Runtime MUST在注册和采样新source前断开并释放旧source的CapturePlayable
- **AND** 旧CaptureJob与新CaptureJob MUST不在同一图评估中写入同一复用workspace槽位

#### Scenario: Locomotion发生连续两次jump

- **WHEN** Player在连续帧发布两个合法Discontinuity
- **THEN** Pose Plan MUST按同一Inertialization节点依次capture和rebase
- **AND** 每帧 MUST只发布一次该节点完成结果

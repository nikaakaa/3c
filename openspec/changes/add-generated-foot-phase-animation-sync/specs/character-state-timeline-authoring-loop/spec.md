## RENAMED Requirements

- FROM: `### Requirement: Corin Walk与Run MAY共享Locomotion.Gait`
- TO: `### Requirement: Corin Walk与Run必须使用生成式Locomotion.Gait同步`

## MODIFIED Requirements

### Requirement: Corin Walk与Run必须使用生成式Locomotion.Gait同步

Corin Walk Loop与Run Loop Presentation Pose source MUST在同一Locomotion PoseStateMachine可达分支中共享`Locomotion.Gait` SyncGroup，并 MUST明确选择`GeneratedFootPhase`。两项source binding MUST按真实AnimationClip配置完整Cyclic marker sequence、合法SyncRole与同一Foot Analysis Source；Character Build MUST从精确artifact为Walk→Run与Run→Walk各自编译有向双脚warp plan。source-local映射 MUST只影响Pose effective sample time，不得改变Pose transition rule、Gameplay movement、Motion request或WorldSolver结果。Walk与Run MUST不为同步恢复Timeline producer、Transition脚步条件或独立FootPhase资产。

Corin Locomotion Input Motion与MovingTurn Timeline Motion Curve MUST分别从自己的deterministic lifecycle提交`CommittedMovementPlaybackClock`。MovingTurn写入Movement motion channel时，clock owner MUST是Timeline operation而不是Locomotion Input operation；Sprint、Attack与Dodge MUST保持Action playback clock。现有Start、End与MovingTurn Marker authoring不得被“无Marker覆盖”的旧假设覆盖。

#### Scenario: Walk Pose切换Run Pose

- **WHEN** PoseStateMachine从Walk handoff到Run且两侧artifact、marker与GeneratedFootPhase plan完整
- **THEN** source-local plan MUST在当帧把Walk marker occurrence与leader fraction映射到Run warped follower fraction
- **AND** Gameplay Program MUST不产生WalkLoop或RunLoop playback，也不得等待下一次脚接触

#### Scenario: Run Pose切换Walk Pose

- **WHEN** PoseStateMachine从Run handoff到Walk
- **THEN** Compiler与Runtime MUST使用Run→Walk自己的有向warp plan
- **AND** MUST不假设它与Walk→Run reduction结果或plan identity相同

#### Scenario: Corin GeneratedFootPhase产物过期

- **WHEN** Walk或Run的Clip、Calibration、Foot Analysis algorithm、marker或warp algorithm改变
- **THEN** Corin Presentation Projection MUST变为Stale并要求明确Character Build
- **AND** Runtime MUST不继续消费旧线性Locomotion计划

#### Scenario: Sprint进入MovingTurn

- **WHEN** Action lifecycle改变移动状态并使MovingTurn Timeline Motion成为当前Movement producer
- **THEN** Motion与Movement clock MUST在同一simulation result中切换到Timeline owner和generation
- **AND** Sprint Action clock与被保留的outgoing Pose source MUST不被重写成该Timeline clock

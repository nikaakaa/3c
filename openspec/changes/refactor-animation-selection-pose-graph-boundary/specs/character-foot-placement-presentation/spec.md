## RENAMED Requirements

- FROM: `### Requirement: Foot Placement 必须是唯一 Presentation Pose Post Process Pass`
- TO: `### Requirement: Foot Placement必须是Pose Graph中唯一world-aware postprocess节点`

## MODIFIED Requirements

### Requirement: Foot Placement必须是Pose Graph中唯一world-aware postprocess节点

启用Foot Placement的Character Presentation Pose Graph MUST显式包含一个`FootPlacement`节点。Pose Graph Compiler MUST把该节点降低为唯一world-aware postprocess阶段，复用正式Planner、PhysicsScene query、Rig Calibration和`ICharacterFootPlacementSolver`，并让`OutputPose`等待该阶段exact completion。Runtime MUST不在图外自动追加第二Foot Placement Pass，不得由Final IK、Animator、MonoBehaviour或其它manager自主更新形成第二骨骼写入路径。

#### Scenario: 一个表现帧更新Corin

- **WHEN** Corin Pose Plan包含FootPlacement节点且ComposedAnimationPoseFrame完成
- **THEN** Runtime MUST执行一次Planner、query与Solver
- **AND** FinalAnimationPoseFrame MUST只在Solver完成后发布

#### Scenario: Pose Graph没有FootPlacement节点

- **WHEN** 编译Pose Graph不包含FootPlacement节点
- **THEN** Runtime MUST不构造Foot Placement Planner或Solver
- **AND** Profile或Prefab MUST不自动补建默认节点

### Requirement: Foot Placement Planner与骨骼Solver必须分离

`FootPlacement`节点的world-aware阶段 MUST让Presentation core唯一拥有contact、prediction、support envelope、constraint、pelvis与`CharacterFootPlacementPlan`；骨骼Solver MUST只消费plan、匹配Rig姿势并应用双脚target和pelvis offset。Pose Graph authoring决定该节点在最终拓扑中的位置，但不得把Planner状态、Physics query或Solver vendor对象写入Gameplay State、Selection或普通native pose节点。

#### Scenario: FootPlacement节点执行一帧计划

- **WHEN** Planner基于ComposedAnimationPoseFrame生成合法CharacterFootPlacementPlan
- **THEN** 配置的Solver MUST按固定顺序应用pelvis和两个Limb结果
- **AND** OutputPose MUST等待同一completion


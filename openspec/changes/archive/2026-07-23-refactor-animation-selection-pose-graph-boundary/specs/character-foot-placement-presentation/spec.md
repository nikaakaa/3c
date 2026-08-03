## MODIFIED Requirements

### Requirement: Foot Placement必须是Pose Graph中唯一world-aware postprocess节点

启用Foot Placement的Character Presentation Pose Graph MUST显式包含一个`FootPlacement`节点。Pose Graph Compiler MUST把该节点降低为唯一world-aware postprocess阶段，复用正式Planner、PhysicsScene query、Rig Calibration和`ICharacterFootPlacementSolver`，并让`OutputPose`等待该阶段exact completion。Runtime MUST不在图外自动追加第二Foot Placement Pass，不得由Final IK、Animator、MonoBehaviour或其它manager自主更新形成第二骨骼写入路径。

#### Scenario: 一个表现帧更新Corin

- **WHEN** Corin Pose Plan包含FootPlacement节点且ComposedAnimationPoseFrame完成
- **THEN** Runtime MUST执行一次Planner、query与Solver
- **AND** FinalAnimationPoseFrame MUST只在Solver完成后发布

#### Scenario: Final IK组件仍启用自主更新

- **WHEN** rig validation发现任一参与solver仍会由Unity lifecycle自主更新
- **THEN** runtime创建 MUST失败
- **AND** 系统 MUST不接受同帧双求解

### Requirement: Foot Placement Planner与骨骼Solver必须分离

`FootPlacement`节点的world-aware阶段 MUST让Presentation core唯一拥有contact、prediction、support envelope、constraint、pelvis与`CharacterFootPlacementPlan`；骨骼Solver MUST只消费plan、匹配Rig姿势并应用双脚target和pelvis offset。Pose Graph authoring决定该节点在最终拓扑中的位置，但不得把Planner状态、Physics query或Solver vendor对象写入Gameplay State、Selection或普通native pose节点。Final IK adapter MUST位于独立命名程序集；`ThirdPersonClient.Runtime` MUST不引用RootMotion类型，Final IK vendor源码 MUST不被修改。

#### Scenario: Final IK应用一帧计划

- **WHEN** Planner输出双脚target、rotation、weight和pelvis offset
- **THEN** Final IK adapter MUST按固定顺序应用pelvis和两个Limb solver
- **AND** MUST不重新query地面或改变Planner约束状态

#### Scenario: 后续替换Solver实现

- **WHEN** 后续增加另一个`ICharacterFootPlacementSolver`
- **THEN** contact、prediction、constraint和pelvis runtime MUST不需要修改
- **AND** 新adapter MUST不成为第二个planner

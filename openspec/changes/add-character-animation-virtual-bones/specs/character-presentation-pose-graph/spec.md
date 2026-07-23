## MODIFIED Requirements

### Requirement: Pose节点必须使用有限typed端口

正式端口类型 MUST至少包含`AnimationSelection`、`Pose`、typed Pose Discontinuity、typed Program Parameter与Output。正式运行节点 MUST限于`AnimationSelectionInput`、`MotionMatchingSelectionInput`、`ProgramParameterInput`、`MarkerSync`、`SelectedPosePlayer`、`BlendSpacePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`PoseSubgraph`、`ModifyBone`、`TwoBoneIK`、`FootPlacement`与`OutputPose`。`GraphInput`/`GraphOutput` MUST只用于subgraph编译边界。`TwoBoneIK` MUST消费并输出普通Pose Value，读取Physical或Virtual Pose Bone reference，只写由三个Physical Bone组成的肢体链，并在native composition阶段执行。

#### Scenario: 端口类型不匹配

- **WHEN** 作者把AnimationSelection直接连接到Pose输入
- **THEN** Validator MUST拒绝连接并定位两端节点与端口

#### Scenario: TwoBoneIK连接在Pose链中

- **WHEN** 作者把composition后的Pose连接到TwoBoneIK并把其输出连接到FootPlacement或其它Pose节点
- **THEN** Compiler MUST把TwoBoneIK降低为显式native composition operation
- **AND** Runtime MUST不创建图外IK pass或隐藏Transform target


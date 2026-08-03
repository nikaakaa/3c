## RENAMED Requirements

- FROM: `### Requirement: Pose Graph必须显式选择连续性节点`
- TO: `### Requirement: Pose Graph必须显式选择Player与局部Inertialization语义`
- FROM: `### Requirement: Pose节点必须使用有限typed端口`
- TO: `### Requirement: Pose节点必须使用有限typed端口与局部discontinuity边界`
- FROM: `### Requirement: Pose Graph必须编译为固定分阶段计划与有界Workspace`
- TO: `### Requirement: Pose Graph必须编译为包含局部惯性节点的固定分阶段计划`
- FROM: `### Requirement: Preview、Runtime与Live Debug必须复用同一Pose Plan`
- TO: `### Requirement: Preview、Runtime与Live Debug必须复用同一局部Pose Plan`

## MODIFIED Requirements

### Requirement: Pose Graph必须显式选择Player与局部Inertialization语义

`SelectedPosePlayer` MUST只采样当前Selection并输出PoseDiscontinuity事实；`BlendStack` MUST唯一拥有其多source CrossFade、Stored Pose、Per-Bone CrossFade Profile、clock与source retention；`Inertialization` MUST唯一拥有直接Player局部Pose流的完成history、每骨骼residual、clock与rebase。Compiler和Runtime MUST不把三种节点静默互换，也 MUST不在OutputPose前补建全局连续化节点。

#### Scenario: 上半身Action单独惯性化

- **WHEN** Action Player连接局部Inertialization后再进入LayeredBoneBlend
- **THEN** 惯性残差 MUST只影响Action分支进入LayeredBoneBlend的Pose
- **AND** BaseLocomotion分支 MUST不共享该节点history或Accumulator

### Requirement: Pose节点必须使用有限typed端口与局部discontinuity边界

Pose Graph MUST使用版本化typed端口连接Selection、Pose Value、Parameter与world-aware阶段。Inertialization的输入 MUST由Compiler证明直接来自同一SelectedPosePlayer的Pose与PoseDiscontinuity；v1 MUST不允许discontinuity跨BlendPose、LayeredBoneBlend、AdditivePose、ModifyBone或PoseSubgraph隐藏传播。节点、端口、Policy和Rig identity任一不匹配 MUST使构建失败。

#### Scenario: 上半身请求跨Layered节点传播到全身

- **WHEN** 作者试图让Action Player的discontinuity穿过LayeredBoneBlend后由全身Inertialization消费
- **THEN** Validator MUST拒绝该连接
- **AND** MUST要求作者把局部节点放在Action分支进入LayeredBoneBlend之前

### Requirement: Pose Graph必须编译为包含局部惯性节点的固定分阶段计划

Pose Graph MUST编译为固定Selection、source capture、native Pose、world-aware Pose与final publication阶段。Inertialization MUST位于native Pose阶段并使用预分配workspace；FootPlacement与IK MUST位于其后。每帧所有source、Player、Blend Stack、Inertialization、composition与final writer MUST只执行一次正式计划和一次PlayableGraph Evaluate。

#### Scenario: 同一图有两个局部惯性节点

- **WHEN** Base和Action分支分别显式配置Inertialization
- **THEN** Compiler MUST为两个PoseNodeId分配互不重叠的history和residual workspace
- **AND** Runtime MUST按拓扑各执行一次且不创建共享全局Accumulator

### Requirement: Preview、Runtime与Live Debug必须复用同一局部Pose Plan

Timeline Preview、MM Query Fixture、Live Debug和正式Runtime MUST使用相同Projection revision、compiled Pose Plan、Player discontinuity与Inertialization节点状态机。Diagnostics MUST按PoseNodeId显示局部作用域、rule、capture/rebase和输出completion；不得通过Animancer state或最终Animator骨骼反推惯性状态。

#### Scenario: Preview中连续切换两次

- **WHEN** Preview按连续播放语义从A切B并在残差未完成时切C
- **THEN** Preview MUST执行与Runtime相同的Rebase
- **AND** overlay MUST显示匹配节点的第二次discontinuity与Accumulator generation

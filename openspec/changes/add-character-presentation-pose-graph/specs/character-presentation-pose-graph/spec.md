## ADDED Requirements

### Requirement: Pose Graph必须显式表达完整动画表现拓扑

Character Presentation Pose Graph MUST从Animation Selection与typed Program Parameter开始，显式经过Player、可选Inertialization、Blend/Layered/Additive、参数解析、骨骼修改、FootPlacement与唯一OutputPose。Runtime MUST不在图外自动补建Blend Stack、Inertialization、Layer、IK、FootPlacement或第二个最终Pose路径。

#### Scenario: 作者检查正式角色图

- **WHEN** 作者打开Corin正式Pose Graph
- **THEN** 图 MUST能沿typed edge追踪Selection到FinalAnimationPoseFrame的完整路径

### Requirement: Selection Input必须显式绑定逻辑输出

`AnimationSelectionInput` MUST显式绑定Program AnimationChannelId；`MotionMatchingSelectionInput` MUST显式绑定MM producer output。Input MUST只读取同帧不可变Selection cache，MUST不重新执行Gameplay winner或MM查询。同一Selection可以fan-out，但每个下游Player MUST拥有独立播放状态。

#### Scenario: 同一Channel被两个Player读取

- **WHEN** 两个Player读取同一AnimationChannel Selection
- **THEN** Compiler MUST只建立一份frame Selection cache
- **AND** 两个Player MUST保持独立source lifetime

### Requirement: Pose Graph必须显式选择连续性节点

`SelectedPosePlayer` MUST采样当前Selection，并在source identity变化时输出新Pose与typed PoseDiscontinuity；没有下游Inertialization时明确硬切。`BlendStack` MUST唯一拥有多source历史、CrossFade、Stored Pose、Per-Bone Blend Profile、clock与source release。`Inertialization` MUST唯一拥有单Pose完成history、residual与rebase。Compiler和Runtime MUST不静默插入或替换这些节点。

#### Scenario: MM使用局部Inertialization

- **WHEN** MotionMatchingSelectionInput连接SelectedPosePlayer再连接Inertialization
- **THEN** Player MUST发布source jump事实
- **AND** residual MUST只由该Inertialization节点处理

#### Scenario: Action使用Blend Stack

- **WHEN** FullBodyAction Selection连接BlendStack
- **THEN** Stack MUST按node-local Blend Policy执行CrossFade与Stored Pose
- **AND** MUST不执行Inertial residual

### Requirement: Pose节点必须使用有限typed端口

正式端口类型 MUST至少包含`AnimationSelection`、`Pose`、typed Pose Discontinuity、typed Program Parameter与Output。正式运行节点 MUST限于`AnimationSelectionInput`、`MotionMatchingSelectionInput`、`ProgramParameterInput`、`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`PoseSubgraph`、`ModifyBone`、`FootPlacement`与`OutputPose`。GraphInput/GraphOutput MUST只用于subgraph编译边界。

#### Scenario: 端口类型不匹配

- **WHEN** 作者把AnimationSelection直接连接到Pose输入
- **THEN** Validator MUST拒绝连接并定位两端节点与端口

### Requirement: Pose Graph必须显式处理Optional Pose与最终输出有效性

每个Player和composition节点 MUST声明RequirePose或AllowEmpty。NoPose MUST是typed availability，不得用bind pose、零矩阵、上一帧缓存或默认动画伪装。唯一OutputPose MUST要求完整有效路径，并且只有所有必需阶段完成后发布FinalAnimationPoseFrame。

#### Scenario: 必需Selection缺失

- **WHEN** RequireSelection输入没有有效Selection
- **THEN** Pose Plan MUST发布typed失败且不得使用旧Pose或默认Clip

### Requirement: Bone Mask、Additive与ModifyBone必须依赖稳定Rig Identity

LayeredBoneBlend的Bone Mask、AdditivePose的Rig Reference和ModifyBone的BoneId MUST引用同一精确RigId/revision。Compiler MUST把local/mesh-space操作降低为确定顺序，拒绝未知BoneId、跨Rig引用、重复写冲突与非法依赖。Runtime MUST不按骨骼名称或Transform path补全。

#### Scenario: ModifyBone引用未知BoneId

- **WHEN** ModifyBone引用Rig中不存在的BoneId
- **THEN** Compiler MUST失败并定位节点与BoneId

### Requirement: Pose Parameter必须通过typed输入和显式解析传播

Pose Graph MUST声明稳定ParameterId、类型、默认值与允许来源。`ProgramParameterInput` MUST读取committed parameter page；source-local curve参数 MUST随Pose Value传播；`PoseParameterResolve` MUST按显式`Base | Overlay | Weighted | Max | Min`规则合成。节点 MUST不按字符串、GameplayTag或State名称查找参数。

#### Scenario: Blend权重来自BTSMTL参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** Compiler MUST校验ParameterId与类型

### Requirement: FootPlacement必须是显式且唯一的world-aware节点

Pose Graph MUST允许作者显式放置唯一`FootPlacement`节点。Compiler MUST把它降低为world-aware阶段，复用既有Planner、PhysicsScene query、CharacterFootPlacementPlan与IK Solver。Runtime MUST不在图外另行追加默认FootPlacement，也 MUST不在Animation Job内复制Physics或IK算法。

#### Scenario: 完整Runtime执行FootPlacement

- **WHEN** 图包含FootPlacement且world context有效
- **THEN** Pose Plan MUST在native composition完成后执行一次world-aware阶段
- **AND** FinalAnimationPoseFrame MUST只在Solver完成后发布

#### Scenario: Preview缺少world context

- **WHEN** Preview执行到FootPlacement但缺少正式world context
- **THEN** Preview MUST标记Unavailable并停在ComposedAnimationPoseFrame

### Requirement: Pose Graph必须编译为固定分阶段计划与有界Workspace

Compiler MUST验证唯一Output、无非法环、端口类型、Rig identity、Selection/Parameter binding、Player Policy、workspace上界与阶段依赖，并生成不可变`CharacterPresentationPosePlan`。Plan MUST明确Selection、Source/Native Pose、World-Aware Post Process与Final Publication阶段。Runtime MUST只执行Plan，不遍历authoring对象或动态发现节点。

#### Scenario: 编译完整图

- **WHEN** 图包含Selection、Player、Inertialization、LayeredBoneBlend、ModifyBone、FootPlacement和OutputPose
- **THEN** Compiler MUST生成固定node order、workspace layout与阶段completion

### Requirement: Preview、Runtime与Live Debug必须复用同一Pose Plan

Timeline Preview、MM Query Fixture、Live Debug与正式Runtime MUST使用相同Projection revision、编译Pose Plan、节点语义和source map。Diagnostics MUST按PoseNodeId显示Selection、Player、Discontinuity、Stack或Inertialization状态、Pose availability、参数来源、world-aware completion与最终输出，不得重新求值图。

#### Scenario: 图中没有连续性节点

- **WHEN** Preview执行只连接SelectedPosePlayer的图
- **THEN** Preview MUST显示明确硬切
- **AND** MUST不创建隐藏Stack或Inertialization

### Requirement: PoseSubgraph必须保持模块化而不形成动态双路径

PoseSubgraph MUST只有显式GraphInput/GraphOutput端口与稳定资产identity。Compiler MUST静态调用subgraph并检测递归，Runtime MUST不动态加载subgraph、不在主图外执行第二次Evaluate，也不得让subgraph读取Gameplay Graph对象。

#### Scenario: Subgraph递归引用

- **WHEN** PoseSubgraph依赖形成递归
- **THEN** Compiler MUST失败并输出完整依赖链

## MODIFIED Requirements

### Requirement: Pose Graph必须显式选择source Player和transition owner

`ClipPlayer`、`BlendSpacePlayer`、`SelectedPosePlayer`和`BlendStack` MUST只消费自身绑定或连接的state-local source。`PoseStateMachine` MUST唯一拥有State到State的transition workspace；`AnimationSlot` MUST唯一拥有Source Pose与有限Action playback之间的插入和handoff；显式`BlendStack` MUST只拥有其连接source的多entry连续性；`Inertialization` MUST只拥有直接上游Player或transition consumer的residual与rebase。Compiler与Runtime MUST不在Provider、AnimationChannel、Graph branch或OutputPose背后自动插入Player、Stack、Slot或Inertialization。

#### Scenario: Idle使用ClipPlayer

- **WHEN** PoseStateMachine保持Idle State
- **THEN** ClipPlayer MUST按Presentation时间采样正式direct Clip binding
- **AND** Action playback lifecycle MUST不创建对应producer

#### Scenario: Corin Locomotion切换

- **WHEN** Corin PoseState edge从Turn切换到RunLoop
- **THEN** edge MUST执行显式编译的Standard Blend与可选Phase relation
- **AND** MUST不自动插入Inertialization

### Requirement: Locomotion Phase映射必须属于source-local采样计划

Direct Clip与Blend Space Locomotion source MUST各自编译为`AnimationSourcePhasePlan`。Direct Clip endpoint MUST使用该Clip的forward/inverse Phase plan；Blend Space endpoint MUST使用显式Phase Reference Sample作为raw clock carrier，并让全部正权重Dynamic Sample通过各自per-clip inverse plan采样同一unwrapped phase。PoseState source同步 MUST由Compiler根据Transition两侧唯一source usage和共同Profile Locomotion Sync Group生成relation；Transition MUST不保存同步开关。Runtime MUST在source采样前生成effective phase/time，MUST不读取AnimationCurve、Profile或Foot Analysis，也 MUST不按State名、Clip名、weight或最高权重样本动态选择leader。

#### Scenario: Walk State切换Run State

- **WHEN** Transition两侧source endpoint属于同一Locomotion Sync Group
- **THEN** relation MUST把leader source phase映射到target source endpoint
- **AND** MUST不创建BaseLocomotion Gameplay Selection

#### Scenario: Transition两侧没有共同同步组

- **WHEN** 两侧source endpoint不属于同一Profile Group
- **THEN** 两侧Player MUST使用各自raw source time
- **AND** Compiler MUST生成None relation

#### Scenario: Phase plan损坏

- **WHEN** source endpoint的Clip identity、Curve hash、coverage或inverse knots无效
- **THEN** Runtime MUST报告稳定typed invalid并阻止本帧Pose publication
- **AND** MUST不回退normalized time、Marker或Animancer自动同步

### Requirement: Animancer必须只负责source采样

Animancer source backend MUST只按完整Action playback或Presentation Pose source identity创建或复用Clip/ManualMixer playable，应用compiled effective sample、loop、play rate和source-local clip weight，并把source capture job安装到同一PlayableGraph。它 MUST不仲裁Pose State或Action winner、不解析AnimationClip Curve、不选择Phase leader、不查询Transition Policy、不拥有跨source weight、不执行AnimationSlot、Layer composition、IK或FootPlacement，也 MUST不发布最终Pose。

#### Scenario: PoseState transition共同采样两个source

- **WHEN** Standard Blend要求source与target同时可见
- **THEN** Animancer MUST分别提供两份source capture
- **AND** source间weight MUST只由PoseState transition计算

## REMOVED Requirements

### Requirement: Marker时间映射必须属于source-local采样计划

该Requirement被Locomotion Phase source endpoint取代；Marker topology、occurrence、SyncRole与Action Marker relation全部删除。

#### Scenario: Runtime收到Marker source plan

- **WHEN** Projection包含Marker pair、segment fraction或occurrence cursor
- **THEN** Projection schema validation MUST失败
- **AND** Runtime MUST不执行兼容映射

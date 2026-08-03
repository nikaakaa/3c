# character-animation-blend-stack Specification

## Purpose
TBD - created by archiving change refactor-animation-playback-to-blend-stack. Update Purpose after archive.
## Requirements
### Requirement: 每个显式Blend Stack节点必须拥有唯一有序状态

编译后的每个`BlendStack` Pose节点 MUST按稳定PoseNodeId创建唯一运行时实例。实例 MUST唯一拥有active entry、push order、CrossFade clock、Stored Pose、source usage与workspace。未连接`BlendStack`节点的图分支 MUST不创建Stack，Runtime和Preview MUST不自动补建Stack或fade。

#### Scenario: 两个节点读取同一Selection

- **WHEN** 两个显式Blend Stack节点读取同一AnimationSelectionFrame
- **THEN** 两个节点 MUST拥有互不共享的entry历史与clock
- **AND** 任一节点的中断 MUST不修改另一节点状态

#### Scenario: 图分支使用直接Player

- **WHEN** Selection只连接SelectedPosePlayer
- **THEN** Runtime MUST不为该分支分配Blend Stack状态

### Requirement: CrossFade必须使用独立Clock、Curve与每骨骼规范化Weight

每个CrossFade entry MUST拥有独立Fade Clock、canonical curve、base duration与Blend Profile。Evaluator MUST按push depth与每骨骼duration multiplier计算nested residual weight，并对每根骨骼的live与Stored贡献规范化。连续sample MUST只更新当前source时间；新selection generation MUST创建新entry。

#### Scenario: 连续中断

- **WHEN** A向B淡入期间同一节点收到新generation C
- **THEN** C MUST创建独立entry与clock
- **AND** push边界输出Pose MUST等于中断前A/B混合Pose

#### Scenario: 不同骨骼使用不同duration

- **WHEN** Blend Profile为上身和下身配置不同duration multiplier
- **THEN** Evaluator MUST按BoneId分别计算weight
- **AND** Runtime MUST不把单一scalar weight当作完整骨骼贡献

### Requirement: Blend Stack必须只发布source usage而不得拥有Marker Sync

Blend Stack MUST在source采样前声明当前与尚未exact release的live `PlayerSourceUsage`，并在source退出时发布精确release。连接显式MarkerSync时，Stack MUST只消费该节点为usage生成的effective sample page；未连接时 MUST使用Selection raw visual time。Stack MUST不读取MarkerId或SyncRole、不选择leader、不建立relation、不映射segment fraction，也 MUST不按blend weight推导同步方向。

#### Scenario: Walk向Run CrossFade并显式同步

- **WHEN** Blend Stack的Selection输入经过MarkerSync且usage同时包含Walk与Run
- **THEN** MarkerSync MUST独立解析两者effective time
- **AND** Blend Stack MUST独立计算两者CrossFade与per-bone weight

#### Scenario: 同一Stack没有MarkerSync

- **WHEN** Selection直接连接Blend Stack
- **THEN** Stack MUST让每个live source按raw visual time采样
- **AND** Runtime MUST不因两者属于同一SyncGroup而后台建立relation

### Requirement: Blend Stack容量必须通过Stored Pose连续压缩

每个Blend Stack节点 MUST显式配置至少为2的`MaxActiveSourceEntries`。push超过容量或命中快速替换阈值时，Evaluator MUST在切换边界捕获当前完整local pose、pose velocity、Pose Parameter与左右脚feature aggregate为唯一Stored Pose，再原子移除被取代entry。Stored Pose MUST使用预分配workspace，MUST不引用AnimationClip、Selection、Marker或Gameplay事件。

#### Scenario: 容量压缩

- **WHEN** 新push使active source超过节点容量
- **THEN** 节点 MUST先捕获当前输出再释放被压缩source
- **AND** capture边界每根骨骼、参数与脚feature MUST连续

### Requirement: Per-Bone Blend Profile必须依赖稳定Rig Identity

`CharacterAnimationBlendProfile` MUST引用精确RigId与revision，并按稳定dense BoneId保存有限正duration multiplier。Compiler MUST拒绝未知BoneId、重复BoneId、非有限值、非正值与Rig不匹配。Runtime MUST不按骨骼名称、path、Humanoid枚举或层级搜索补全。

#### Scenario: Blend Profile与Rig不匹配

- **WHEN** Blend Stack节点引用的Profile Rig identity与Pose Plan Rig不同
- **THEN** Compiler MUST失败并定位节点与Profile

### Requirement: Animancer必须只作为Source Pose采样后端

Animancer MUST只按完整source identity创建AnimationClip state或producer内部ManualMixer、写入sample time与child weight、捕获source pose并管理playable寿命。Animancer MUST不读取Blend Policy，不执行Layer Play/Fade，不决定entry weight，不保存Pose Graph拓扑，也不写最终Pose。

#### Scenario: Blend Stack推进

- **WHEN** Blend Stack节点推进CrossFade
- **THEN** transition clock与weight MUST只由节点Evaluator推进
- **AND** Animancer state MUST只按resolved sample descriptor采样

### Requirement: Blend Stack节点必须由固定Animation Job输出统一Pose Value

Runtime MUST按Rig bone count、节点数量和各节点容量预分配source、Stored、parameter、feature与weight Native workspace。每个节点 MUST原子提交不可变frame plan；source capture、节点blend job、下游Pose composition和final writer MUST位于同一PlayableGraph并在单次Evaluate中按编译依赖完成。节点 MUST输出统一Pose Value，MUST不读取Inertial residual、下游Bone Mask、执行跨分支Override/Additive、写Gameplay Body或写最终Animator Pose。

#### Scenario: 同一图包含两个Stack节点

- **WHEN** 编译图同时包含Locomotion与Action两个Blend Stack节点
- **THEN** 两个节点 MUST分别发布匹配PoseNodeId的Pose Value
- **AND** 下游LayeredBoneBlend MUST只按typed edge消费它们

### Requirement: Node-local Blend Policy必须是该节点唯一转场权威

每个Blend Stack节点 MUST引用唯一`CharacterAnimationBlendPolicy`。Policy MUST保存容量、Stored Pose策略、`Linear | EaseIn | EaseOut | EaseInOut | Custom` Blend Mode、条件式强类型Custom Curve Asset、强类型Blend Profile、authoring default rule与exact source-target override；不得保存第二种inline curve作者格式。Compiler MUST把每条规则降低为canonical curve与dense profile，只枚举该节点可达Selection endpoint与Empty组合，并把default和override物化为完整CrossFade exact table；Runtime缺少pair MUST失败，MUST不fallback到默认时长、线性曲线、Inertial或Animancer fade。

#### Scenario: 两个节点复用同一Policy

- **WHEN** 两个Blend Stack节点显式引用同一Policy资产
- **THEN** Compiler MUST为两个节点分别生成可达exact table
- **AND** 两个运行时节点 MUST不共享mutable entry或clock状态

#### Scenario: 可达pair缺失

- **WHEN** 某节点可达source-target pair无法由default或override精确物化
- **THEN** Compiler MUST失败并定位PoseNodeId与pair

### Requirement: Pose Value必须完整表达Stack输出

每个完成的Blend Stack节点 MUST发布不可变Pose Value，包含PoseNodeId、completion identity、Pose/NoPose/Invalid availability、dense local pose、Pose Parameter、live/Stored contribution、左右脚feature aggregate与continuity identity。Pose Value MUST不声称已经经过下游Inertialization、Bone Mask、ModifyBone或FootPlacement，也 MUST不携带Gameplay state或authoring object。

#### Scenario: AllowEmpty节点没有Selection

- **WHEN** AllowEmpty Blend Stack节点本帧没有Selection且历史已经退出
- **THEN** Pose Value MUST为NoPose并且source contribution为空

### Requirement: Blend Stack调试必须完整解释节点Pose来源

正式snapshot MUST按PoseNodeId显示Selection、entry identity、source identity、push order、CrossFade duration、elapsed、raw/eased alpha、选定BoneId weight、Stored capture、Pose Value、source usage与retirement原因。Preview与Live Debug MUST只读取snapshot，不得重新求值curve、weight、capacity或下游最终贡献。

#### Scenario: 检查一次中断

- **WHEN** 作者在Live Debug选择一个Blend Stack节点和BoneId
- **THEN** snapshot MUST解释该骨骼当前全部live与Stored贡献
- **AND** diagnostics MUST不改变节点时钟或source lifetime

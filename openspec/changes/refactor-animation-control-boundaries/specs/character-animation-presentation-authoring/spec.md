# character-animation-presentation-authoring Specification

## ADDED Requirements

### Requirement: Presentation Profile必须唯一绑定Pose source

Pose Graph MUST拥有类型化`CharacterPresentationPoseSourceSlot`子资产；`CharacterAnimationPresentationProfile` MUST保存Profile-owned typed Binding子资产，并通过精确Slot对象引用唯一绑定AnimationClip resource、Rig identity、loop capability、marker topology、marker sequence、source-local Foot Placement Weight typed curve与Foot Analysis identity。Pose Graph SequencePlayer MUST只引用类型匹配的Source Slot对象。Gameplay Graph、BTSMTL StateMachine、Timeline、ActionProfile、Prefab与generated Program MUST不复制持续Locomotion source binding。

#### Scenario: 作者替换Run动画

- **WHEN** 作者在Corin Presentation Profile把Run source绑定到新AnimationClip
- **THEN** PoseStateMachine topology与Gameplay Program MUST不需要修改
- **AND** Projection必须通过明确Build命令重建

#### Scenario: shared Pose Graph用于不同角色

- **WHEN** 两个Profile使用同一Pose Graph但绑定不同Rig兼容资源
- **THEN** SequencePlayer MUST保持相同Source Slot对象引用
- **AND** 每个Projection MUST解析各自Profile binding

### Requirement: PoseStateMachine工作区必须对齐UE作者口径

Pose Graph Workspace MUST显示State Machine、State、Transition Rule、State Alias、Sequence Player、Blend Space Player、Slot、Blend Logic与Inertialization等作者术语。StateMachine内部图、State inline Pose图和Transition Rule图 MUST使用明确下钻导航。Workspace MUST显示compiled active state、target state、transition progress、Slot playback、source usage和route，不得展示BTSMTL Gameplay State为Pose State。

#### Scenario: 作者打开Locomotion PoseStateMachine

- **WHEN** 作者双击PoseStateMachine节点
- **THEN** Workspace MUST显示Entry、State、Alias和Transition edge
- **AND** MUST不显示Gameplay Action或Timeline control edge

#### Scenario: 作者查看Action Slot

- **WHEN** 作者选择FullBodyAction Slot
- **THEN** Details MUST显示绑定channel、Blend Policy和compiled route摘要
- **AND** MUST不提供Action admission或Motion配置

### Requirement: Action producer authoring必须只允许有限Timeline Action

`AnimationProducerPresentationBinding`、Profile Inspector与正式authoring mutation MUST只允许有限Action Timeline producer。Motion Matching、Blend Space与Sequence source MUST只通过`PresentationPoseSourceBinding`和PoseState source provider descriptor配置，MUST不作为Gameplay producer、AnimationChannel candidate或Action Playback Input。Projection Compiler MUST分别建立Action-only binding index与Pose source/provider binding index。

#### Scenario: 作者为Attack Timeline建立producer

- **WHEN** Timeline包含合法有限Action AnimationTrack
- **THEN** Profile MAY建立对应Action producer binding与AnimationChannel关联
- **AND** Binding MUST可解析到Action Playback Input和AnimationSlot

#### Scenario: 作者配置Locomotion Blend Space

- **WHEN** PoseState inline graph引用BlendSpacePlayer
- **THEN** Profile MUST建立Presentation Pose source binding
- **AND** Inspector MUST不提供Gameplay producer或Action channel选项

#### Scenario: 作者配置Motion Matching

- **WHEN** PoseState inline graph引用Motion Matching Player
- **THEN** Projection MUST建立state-local provider binding
- **AND** MUST不创建ProgramProducerId或AnimationPlaybackId

## MODIFIED Requirements

### Requirement: Foot Analysis Source必须是显式可验证的表现作者输入

Editor-only `CharacterFootPlacementAnalysisSource` MUST拥有稳定identity、算法版本、固定sample rate、确定性reduction参数、精确Sampling Rig Asset GUID和唯一Rig Calibration。Profile MUST保存可解析的Analysis Source identity；每个Pose source binding与有限Action producer source binding MUST显式引用其使用的Analysis Source identity。Inspector MAY通过对象选择写入identity，但Profile、binding与Source MUST不保存Sampling Rig `GameObject`强引用。Projection Builder MUST只按明确identity与GUID解析Source和Prefab，不得反向搜索角色、骨骼名称、Humanoid mapping或默认Rig补全。

#### Scenario: Run Pose source缺少Analysis Source

- **WHEN** Run binding启用GeneratedPerFootFeatures但没有合法Analysis Source identity
- **THEN** Profile validation与Definition Build MUST失败并定位Run source
- **AND** 系统 MUST不从旧RunLoop Timeline或场景角色推断配置

### Requirement: Foot Analysis必须由正式Projection Build生成

单AnimationClip feature MUST先由正式Artifact Builder按精确AnimationClip、Analysis Source、Sampling Rig、Calibration和算法输入生成Editor-only artifact。Definition Build MUST收集Pose source binding与有限Action producer binding的全部可达stable clip，精确校验或生成所需artifact，再把feature写入对应Projection binding。Projection发布 MUST发生在正式Build Transaction中；artifact本身不得成为Runtime或作者真相。

#### Scenario: 同一AnimationClip被Pose source与Action producer引用

- **WHEN** 两个stable binding使用相同AnimationClip和Analysis Source
- **THEN** Build MAY复用同一artifact payload
- **AND** 两个Projection binding MUST分别保留自己的source identity与精确映射

#### Scenario: 单Pose source预分析

- **WHEN** 作者在Profile source工具中提前生成一个clip artifact
- **THEN** 该操作 MUST不发布Program或Projection
- **AND** 后续Definition Build MUST重新校验artifact后才能消费

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存动画表现数据。Profile MUST唯一引用Pose Graph、Pose source binding、有限Action producer source binding、node-local Policy、Rig与Foot Analysis配置。Pose Graph MUST唯一保存Presentation Fact Input、PoseStateMachine、SequencePlayer、AnimationSlot、Selection Player、composition、FootPlacement与Output topology。Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter与Prefab MUST不复制这些配置。

#### Scenario: Corin配置动画表现

- **WHEN** Corin Definition引用正式Animation Presentation Profile
- **THEN** Profile MUST提供PoseStateMachine source与Action Slot producer的唯一资源绑定
- **AND** Definition MUST不内联Run Clip、State transition或Slot policy

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个有限Action Timeline animation producer MUST拥有稳定authoring producer identity；每个持续Pose source MUST拥有稳定Unity Source Slot与Binding子资产身份。Compiler MUST把Action identity写入Program source map与Projection binding，并把可达Pose source降低为Presentation Projection内连续dense source index和只读source map。Runtime identity MUST不使用显示名、作者数组index、asset path或当前State名称作为fallback。

#### Scenario: Timeline Track重排

- **WHEN** Attack AnimationTrack在Timeline中重排
- **THEN** Action producer identity MUST保持
- **AND** FullBodyAction Slot binding MUST不改变

#### Scenario: Pose State重命名

- **WHEN** 作者重命名Run Pose State显示名
- **THEN** Sequence source identity MUST保持
- **AND** Projection MUST不按State名称重新绑定Clip

### Requirement: Blend Policy必须属于显式Blend Stack节点

Blend Policy MUST属于明确的transition owner：PoseState Transition edge、AnimationSlot或保留的显式BlendStack。PoseState edge MUST只保存该edge的Blend Logic与数学配置；AnimationSlot MUST按全部可达Action endpoint物化完整exact rule table；普通BlendStack MUST继续只管理连接到自身的多source历史。Timeline、Gameplay State edge、SequencePlayer、ActionProfile与Prefab MUST不保存第二份transition表，Animancer backend MUST不决定fade。

#### Scenario: Locomotion State transition

- **WHEN** 作者配置Start到Locomotion的Blend Logic
- **THEN** Policy MUST由该PoseState Transition owner引用
- **AND** BTSMTL Locomotion edge MUST不保存动画duration

#### Scenario: FullBodyAction Slot

- **WHEN** 作者配置Attack到Dodge exact rule
- **THEN** Policy MUST由FullBodyAction Slot引用
- **AND** Action Timeline MUST不保存该Pose transition

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Pose source binding、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis Mode与Analysis Source。Timeline Editor继续唯一编辑Action producer-local Clip、Window、Motion、Cue和Timeline marker。持续Locomotion Sequence source的Clip、marker和analysis归属Profile binding；系统 MUST不要求为该source创建Timeline。

#### Scenario: 编辑Locomotion marker

- **WHEN** 作者选择PoseState中的Run Sequence source
- **THEN** Workspace MUST导航到Profile的Run source binding
- **AND** MUST不创建或打开RunLoop Gameplay Timeline

#### Scenario: 编辑Attack1窗口

- **WHEN** 作者修改Attack1 HitWindow
- **THEN** 必须导航到Attack1 Action Timeline
- **AND** Profile source binding MUST不保存该Gameplay window

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

Profile Inspector MUST在显式Definition context下分别显示持续Pose source与有限Action producer。Pose source MUST按Source Slot业务名与Binding子资产显示消费它的PoseState/Sequence/BlendSpace节点、resource、marker、Foot Placement Weight与analysis状态；Action producer MUST按Timeline/Track业务信息显示AnimationChannel、ActionPlaybackInput/AnimationSlot consumer与resource binding。服务 MUST不从显示名、目录、旧BaseLocomotion channel或generated产物反推authoring。

#### Scenario: 查看Run Pose source

- **WHEN** 作者从Corin Definition展开Locomotion PoseStateMachine
- **THEN** Inspector MUST显示Run Source Slot、consumer、Profile Binding和实际资源
- **AND** MUST不要求RunLoop Timeline producer

#### Scenario: 查看Attack1到Attack2

- **WHEN** 作者展开FullBodyAction Slot
- **THEN** Inspector MUST显示两个Timeline producer identity与Slot binding
- **AND** MUST不复制Action逻辑transition

### Requirement: Animation Marker Sync 必须由实际source owner唯一拥有

有限Action producer的Marker Sync数据 MUST继续由对应Timeline AnimationTrack唯一拥有。持续Pose source的Marker Sync数据 MUST由Profile中的对应Pose source binding唯一拥有。两类owner都 MUST保存明确None或MarkerGroup、SyncGroupId、Finite/Cyclic topology、SyncRole与ordered Point Marker；它们 MUST不互相复制，也不得把marker写入Gameplay StateMachine、Pose transition Rule、Blackboard、ActionProfile或FootPhase资产。

#### Scenario: 编辑Attack marker

- **WHEN** 作者修改Attack1的finite marker
- **THEN** Timeline Editor MUST成为唯一写入口
- **AND** Profile MUST不复制该marker

#### Scenario: 编辑Run marker

- **WHEN** 作者修改Run Pose source的Locomotion.Gait marker
- **THEN** Profile Pose source editor MUST成为唯一写入口
- **AND** Timeline Editor MUST不创建RunLoop Track副本

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

有限Action Animation Clip MUST继续由Timeline Clip唯一保存Weight、Ease、Foot Placement Weight等已注册typed Curve Channel。持续Pose source MUST只在Profile binding保存source-local Foot Placement Weight typed curve；State transition的blend curve MUST继续由Transition Policy拥有。Timeline Clip、Pose source与Transition Policy MUST不双写同一curve，generated每脚feature MUST不成为可编辑Curve Channel。

#### Scenario: 编辑Attack Foot Placement Weight

- **WHEN** 作者展开Attack Clip曲线
- **THEN** Timeline Curve Editor MUST编辑该Clip的typed channel
- **AND** Profile Pose source MUST不保存副本

#### Scenario: 编辑Run Foot Placement Weight

- **WHEN** 作者选择Run Pose source
- **THEN** Profile source curve editor MUST编辑source-local typed curve
- **AND** MUST不创建Run Timeline Clip

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession与CharacterPipelineHost调试视图 MUST作为Presentation Fact、PoseState active/target/transition、Pose source relevance、committed Action producer、Timeline visual sample、Action Playback lifecycle、AnimationChannel、AnimationSlot、Player source usage、Stack/Stored、Inertialization residual、Marker relation、Pose contribution与Output completion的唯一调试入口。Definition Inspector、Profile Inspector、Timeline Editor与Pose Graph Workspace MUST只读取该正式Trace，不得复制另一套生命周期状态、重新运行Gameplay Graph、重采样Timeline、求值Pose Graph或从Animancer weight重建事实。

#### Scenario: 排查攻击切换

- **WHEN** FullBodyAction从None变为Attack1且Locomotion PoseState继续更新
- **THEN** Live Debug MUST显示当前PoseState、Attack1 command与首样本、Slot transition和最终Output贡献
- **AND** 数据 MUST来自同一正式Trace

### Requirement: Marker Group 必须支持 Finite 与 Cyclic 序列

Action AnimationTrack与Presentation Pose source的MarkerGroup都 MUST声明Finite或Cyclic。Cyclic Action producer的全部Timeline call site MUST为Loop；Cyclic Pose source的Sequence/BlendSpace player MUST允许循环。Finite Action producer和Finite Pose source MUST覆盖各自完整duration且不回绕。冲突调用或非法topology MUST编译失败。

#### Scenario: Run Pose source为Cyclic

- **WHEN** Run source配置Locomotion.Gait/Cyclic
- **THEN** SequencePlayer MUST以loop方式采样
- **AND** 末marker到首marker MUST形成回绕segment

#### Scenario: Stop Pose source为Finite

- **WHEN** Stop source配置Finite
- **THEN** 首末marker MUST覆盖完整Clip duration
- **AND** source MUST不回绕

### Requirement: Marker Group 必须显式声明 handoff 同步角色

每个Action AnimationTrack与Pose source MarkerGroup MUST显式声明`CanBeLeader`、`AlwaysLeader`或`AlwaysFollower`。PoseState Source Sync Plan与exact Selection MarkerSync MUST使用同一角色解析规则；角色冲突 MUST失败，MUST不按State、Action或Clip名称猜测leader。

#### Scenario: Stop保持自身节奏

- **WHEN** incoming Stop Pose source声明AlwaysLeader
- **THEN** State transition sync MUST让Stop使用自身raw节奏
- **AND** outgoing Locomotion source MUST作为follower

### Requirement: Marker Group 必须在 Projection 构建前完整校验

Projection Build MUST分别校验Action AnimationTrack和Presentation Pose source的duration、marker identity、frame/time、有向pair、topology、role、resource coverage与共同可达SyncGroup pair集合。Pose source使用AnimationClip duration与Profile binding；Action producer使用Timeline duration与Track binding。任一缺失或跨owner冲突 MUST阻止发布，MUST不回退normalized time。

#### Scenario: Pose source缺少有向pair

- **WHEN** Walk与Run声明同组但Run缺少Walk拥有的有向pair
- **THEN** Projection Build MUST失败并定位Run Pose source
- **AND** MUST不恢复Timeline marker

### Requirement: Presentation Projection 必须保存规范化 Marker Sync 映射

Projection Compiler MUST把Action producer和Presentation Pose source的同步模式、canonical SyncGroupId、topology、role、duration、ordered marker与有向pair occurrence索引编入Projection。Action mapping MUST关联producer binding；Pose source mapping MUST关联dense source index、只读Source Slot/Binding定位信息与State source consumer。两类映射 MUST只服务表现采样，不进入Gameplay Program ABI、State codec、Snapshot或Network协议。

#### Scenario: PoseState Marker Sync改变sample time

- **WHEN** Walk到Run State transition启用Source Sync
- **THEN** Projection mapping MUST只改变Run effective sample time
- **AND** Gameplay movement与State Rule MUST保持不变

### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Pose Graph Navigator MUST要求精确Definition context，并从Profile、Pose Graph和Gameplay composition roots分别投影Pose source与有限Action producer。Locomotion分组 MUST显示PoseState、Sequence/BlendSpace/MM consumer和Pose source binding；Action分组 MUST显示Timeline、Track、AnimationChannel与AnimationSlot consumer。Navigator MUST不读取generated Program/Projection完成bootstrap，不按显示名猜测，也不得保存第二份binding。

#### Scenario: 查看Locomotion sources

- **WHEN** 作者从Corin Definition展开Locomotion
- **THEN** Navigator MUST列出Idle、Start、Move、Stop、Turn的正式Pose source
- **AND** MUST不列出BaseLocomotion Timeline producer

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Track和Profile Pose source的resource、marker、curve、Policy、Rig与analysis状态。修改Action Clip、marker、window或curve MUST导航到Timeline Editor；修改Pose source resource、marker或Foot Placement Weight MUST导航到Profile source editor；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。系统 MUST不复制字段或提供第二mutation命令。

#### Scenario: 从Pose Graph调整Run marker

- **WHEN** 作者在State source引用面板选择Open Source
- **THEN** 必须打开Profile中的Run Pose source editor
- **AND** Pose Graph节点 MUST保持只读引用

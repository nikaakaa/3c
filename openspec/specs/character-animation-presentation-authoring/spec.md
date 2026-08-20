# character-animation-presentation-authoring Specification

## Purpose

定义角色动画表现配置的唯一作者边界：CharacterPipelineDefinition只引用CharacterAnimationPresentationProfile，Profile唯一引用Pose Graph、state-local Pose source、有限Action producer、node-local Policy、Rig与Foot Analysis，Profile Inspector和Pose Graph Workspace提供各自唯一入口，并由编译链生成CharacterPresentationProjection。
## Requirements
### Requirement: Presentation Profile必须唯一绑定Pose source

Pose Graph MUST唯一拥有typed Source Slot，`CharacterAnimationPresentationProfile` MUST为每个Slot拥有唯一类型匹配Binding。Clip Binding MUST直接引用精确AnimationClip；Blend Space与Motion Matching Binding MUST继续引用各自正式资源。Profile MUST唯一引用角色Rig Definition、Foot Analysis Source、有限Action producer binding和Locomotion Sync Group。Clip Binding与Action producer binding MUST不保存Sequence、Rig副本、Analysis identity副本、素材Curve、Marker、Group、Role、Topology或Time Mapping。Blend Space与Motion Matching资源内部为各自Artifact保存的Rig/Analysis compatibility identity MAY保留，但只能作为Profile选择的准入约束，不得成为第二角色配置owner。

#### Scenario: ClipPlayer解析RunLoop

- **WHEN** ClipPlayer引用RunLoop Source Slot
- **THEN** Projection Compiler MUST从Profile唯一Clip Binding解析AnimationClip并分配dense source index
- **AND** MUST不经过Sequence资产或作者字符串查找

### Requirement: PoseStateMachine工作区必须对齐UE作者口径

Pose Graph Workspace MUST显示State Machine、State、Transition Rule、State Alias、Clip Player、Blend Space Player、Slot、Blend Logic与Inertialization等作者术语。StateMachine内部图、State Pose Graph和Transition Rule图 MUST使用明确下钻导航。Workspace MUST显示compiled active state、target state、transition progress、Slot playback、source usage和route，不得展示BTSMTL Gameplay State为Pose State。

#### Scenario: 作者打开Locomotion PoseStateMachine

- **WHEN** 作者双击PoseStateMachine节点
- **THEN** Workspace MUST显示Entry、State、Alias和Transition edge
- **AND** MUST不显示Gameplay Action或Timeline control edge

### Requirement: Action producer authoring必须只允许有限Timeline Action

`AnimationProducerPresentationBinding`、Profile Inspector与正式authoring mutation MUST只允许有限Action Timeline producer。Motion Matching、Blend Space与Clip source MUST只通过Graph-owned Source Slot对象、Profile-owned typed binding子资产和PoseState source provider配置，MUST不作为Gameplay producer、AnimationChannel candidate或Action Playback Input。Projection Compiler MUST分别建立Action-only binding index与Pose source/provider dense binding index。

#### Scenario: 作者配置Locomotion Blend Space

- **WHEN** PoseState Pose Graph中的BlendSpacePlayer引用一个Blend Space Source Slot
- **THEN** Profile MUST为该Slot建立类型匹配的Blend Space binding子资产
- **AND** Inspector MUST不提供Gameplay producer或Action channel选项

### Requirement: Foot Analysis Source必须是显式可验证的表现作者输入

Editor-only `CharacterFootPlacementAnalysisSource` MUST拥有稳定identity、算法版本、固定sample rate、确定性reduction参数、精确Sampling Rig Asset GUID和唯一Rig Calibration。Profile MUST保存可解析的Analysis Source identity；Clip Binding与Action producer binding MUST不复制Analysis Source identity；全部可达Clip统一消费Profile选择的唯一Analysis Source。Inspector MAY通过对象选择写入identity，但Profile、binding与Source MUST不保存Sampling Rig `GameObject`强引用。Projection Builder MUST只按明确identity与GUID解析Source和Prefab，不得反向搜索角色、骨骼名称、Humanoid mapping或默认Rig补全。

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

#### Scenario: 单Clip预分析

- **WHEN** 作者在Timeline工具中提前生成一个clip artifact
- **THEN** 该操作 MUST不发布Program或Projection
- **AND** 后续Definition Build MUST重新校验artifact后才能消费

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存动画表现数据。Profile MUST唯一引用Pose Graph、Profile-owned Pose source binding子资产、有限Action producer source binding、node-local Policy、Rig与Foot Analysis配置。Pose Graph MUST唯一保存Presentation Fact Input、PoseStateMachine、Graph-owned Source Slot子资产、ClipPlayer、AnimationSlot、Selection Player、composition、FootPlacement与Output topology。Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter与Prefab MUST不复制这些配置。

#### Scenario: Corin配置动画表现

- **WHEN** Corin Definition引用正式Animation Presentation Profile
- **THEN** Profile MUST为PoseStateMachine Source Slot和Action Slot producer提供唯一资源绑定
- **AND** Definition MUST不内联Run Clip、State transition或Slot policy

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile和Analysis Source
- **AND** 每个Profile MUST为shared Source Slot对象提供自己的binding子资产
- **AND** shared Graph/Timeline MUST不保存角色级资源、分析Rig或校准

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个有限Action Timeline animation producer MUST拥有稳定authoring producer identity。每个持续Pose source在authoring层 MUST由稳定Unity Source Slot对象和Profile binding对象精确表达，不得保存作者可编辑Source Id字符串。Projection Compiler MUST把Action identity写入Program source map与Projection binding，并把Pose source对象关系降低为Projection-local dense source index与只读source map。Runtime MUST只在匹配Projection revision内使用dense source index、Player identity和generation，不得使用显示名、数组index、asset path或当前State名称作为fallback。

#### Scenario: Timeline Track 重排

- **WHEN** 作者重排 AnimationTrack 或 Clip
- **THEN** 原 producer identity MUST 保持
- **AND** Program 与 Projection binding MUST 不因列表 index 变化而 orphan

#### Scenario: Source binding重排

- **WHEN** 作者重排Profile中的Pose source binding卡片
- **THEN** Source Slot与binding对象引用 MUST保持
- **AND** Compiler MUST重新确定性生成dense index而不改变任何作者引用

#### Scenario: binding 指向未知Source Slot

- **WHEN** Profile binding引用不属于当前Pose Graph闭包的Source Slot对象
- **THEN** Compiler/Validator MUST报告orphan binding并定位对象owner
- **AND** Runtime MUST拒绝Program/Projection组合，不能按名称或Clip猜测目标

### Requirement: Blend Policy必须属于明确transition owner

Blend Policy MUST属于明确的transition owner：PoseState Transition edge、AnimationSlot或保留的显式BlendStack。PoseState edge MUST保存该edge的`Standard Blend | Inertialization`、duration、`Linear | EaseIn | EaseOut | EaseInOut | Custom` Blend Mode、条件式强类型Custom Curve Asset与强类型Blend Profile；Custom MUST有Curve Asset，非Custom MUST不保留Curve Asset，非Hard Cut MUST有Blend Profile。AnimationSlot MUST按全部可达Action endpoint物化完整exact rule table；普通BlendStack MUST继续只管理连接到自身的多source历史。Timeline、Gameplay State edge、ClipPlayer、ActionProfile与Prefab MUST不保存第二份transition表，Animancer backend MUST不决定fade。

#### Scenario: Locomotion State transition

- **WHEN** 作者配置Start到Locomotion的Blend Logic
- **THEN** Policy MUST由该PoseState Transition owner引用
- **AND** BTSMTL Locomotion edge MUST不保存动画duration

#### Scenario: FullBodyAction Slot

- **WHEN** 作者配置Attack到Dodge exact rule
- **THEN** Policy MUST由FullBodyAction Slot引用
- **AND** Action Timeline MUST不保存该Pose transition

#### Scenario: 旧Policy包含Inertial override

- **WHEN** Build读取仍包含Inertial technique的Blend Policy
- **THEN** Build MUST失败并要求迁移到具体Inertialization节点Policy

#### Scenario: 作者为单条Locomotion edge配置Custom曲线

- **WHEN** 作者把Walk Start到Locomotion的Blend Mode设为Custom并选择合法Curve Asset与Blend Profile
- **THEN** Transition MUST保存两个强类型资产引用并使Projection变为Stale
- **AND** Inspector修改 MUST不自动Compile或Build

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Profile-owned Pose source binding子资产、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis Mode与Analysis Source。Timeline Editor继续唯一编辑Action producer-local Clip Segment、Window、Motion、Cue和Timeline-local Curve。持续Locomotion Clip的骨骼与注册曲线归属原生AnimationClip，角色Rig、Analysis Source与Locomotion Sync Group归属Profile；系统 MUST不要求为该source创建Timeline。Inspector MUST通过类型受限Unity对象选择器和可读业务名编辑资源，不得要求作者输入Source Id、Provider Id、GUID、local file id、revision或hash。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

Profile Inspector MUST在显式Definition context下分别显示持续Pose source与有限Action producer。Pose source MUST按Source Slot业务名与实际Unity资源显示消费它的PoseState/Clip/BlendSpace/MM节点、resource、注册Curve状态与Profile Analysis装配；Action producer MUST按Timeline、Track与资源业务名显示AnimationChannel、ActionPlaybackInput/AnimationSlot consumer与resource binding。稳定identity、revision、GUID、local file id、hash与compiled index MUST默认隐藏，只能在显式Diagnostics区域只读显示。服务 MUST不从显示名、目录、旧BaseLocomotion channel或generated产物反推authoring。

#### Scenario: 查看Run Pose source

- **WHEN** 作者从Corin Definition展开Locomotion PoseStateMachine
- **THEN** Inspector MUST显示Run Source Slot、实际动画资源、consumer和Profile binding owner
- **AND** MUST不要求RunLoop Timeline producer或显示可编辑Source Id

#### Scenario: 查看 Action 覆盖 Locomotion

- **WHEN** Definition context 同时包含 Action 与 Locomotion producer
- **THEN** Inspector MUST用各自业务名与资源对象分组显示binding
- **AND** MUST不推断覆盖关系或创建 Driver、Priority

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession与CharacterPipelineHost调试视图 MUST作为Presentation Fact、PoseState active/target/transition、Pose source relevance、committed Action producer、Timeline visual sample、Action Playback lifecycle、AnimationChannel、AnimationSlot、Player source usage、Stack/Stored、Inertialization residual、Phase relation、Pose contribution与Output completion的唯一调试入口。Definition Inspector、Profile Inspector、Timeline Editor与Pose Graph Workspace MUST只读取该正式Trace，不得复制另一套生命周期状态、重新运行Gameplay Graph、重采样Timeline、求值Pose Graph或从Animancer weight重建事实。

#### Scenario: 排查攻击切换

- **WHEN** FullBodyAction从None变为Attack1且Locomotion PoseState继续更新
- **THEN** Live Debug MUST显示当前PoseState、Attack1 command与首样本、Slot transition和最终Output贡献
- **AND** 数据 MUST来自正式 Trace

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

项目表现控制曲线 MUST由唯一channel catalog注册，并直接保存于可写原生AnimationClip。Unity Animation Window MUST成为人工Curve key编辑入口；Agent Document MUST只读写同一注册Curve。Projection MUST把Curve降低为Runtime canonical plan。Profile、Timeline、Blend Space和Foot Analysis artifact MUST不保存可写Curve副本。

#### Scenario: 修改Foot Placement Weight

- **WHEN** 作者在Animation Window修改Clip的`presentation.foot-placement-weight`
- **THEN** 完整Clip dependency与Registered Curve Hash MUST变化并使Projection stale
- **AND** AnimationClipAnalysisInputHash与匹配Foot Analysis Artifact MUST保持不变
- **AND** Runtime MUST只在显式Build后消费新的Projection curve

### Requirement: Equipment Presentation 不得拥有动画空间拓扑

Equipment Feature authoring MUST不保存LayerId、BlendMode、OutputPolicy或Presentation producer requirement。`EquipmentFeatureRouteImplementation.RequiredProducerIds` MUST仅表达Gameplay route完整性，MUST不进入Presentation Projection的channel、Player、transition或Animancer resolution。Equipment Presentation Profile与Projection MUST只保存VisualBinding、Prefab/socket、Renderer登记与local pose资源绑定，MUST不复制ActionPlaybackInput、AnimationSlot、Pose source或PoseNode字段，也 MUST不提前创建Equipment到Pose Graph的动态替换接口。

#### Scenario: Equipment Feature声明Gameplay route producer

- **WHEN** Equipment route使用RequiredProducerIds校验Gameplay Graph实现完整性
- **THEN** Semantic/Gameplay compiler MAY保留该纯route依赖
- **AND** Projection Compiler MUST不把它解释为AnimationChannel、PoseNode或表现层producer binding

#### Scenario: 武器需要动态替换Pose Graph

- **WHEN** 未来武器业务需要替换整段Pose实现
- **THEN** 系统 MUST由独立change定义正式输入、Projection schema与Runtime生命周期
- **AND** 当前Equipment Visual链 MUST不提供passthrough、兼容Layer或临时Player接口

### Requirement: Equipment Visual binding必须属于唯一Equipment Presentation Profile

`CharacterEquipmentPresentationProfile` authoring MUST提供按稳定VisualBindingId配置`ExistingRigObject`与`SpawnedVisualAsset`的唯一入口，并通过正式Rig/Socket binding catalog选择目标。Gameplay Equipment Profile只引用VisualBindingId；Animation Profile、RootTree与Feature graph MUST不直接编辑GameObject路径、Renderer数组或Prefab实例。

#### Scenario: 配置Corin existing weapon

- **WHEN** 作者为CorinSawblade选择ExistingRigObject
- **THEN** Inspector MUST从正式Rig binding catalog选择Renderer set
- **AND** serialized binding MUST不依赖显示名称搜索

#### Scenario: Feature尝试内嵌Animation Layer

- **WHEN** Feature authoring尝试创建Layer或保存Animancer transition副本
- **THEN** authoring validator MUST拒绝
- **AND** 作者 MUST继续使用唯一Presentation Profile Inspector

### Requirement: Pose Graph Producer Navigator必须从显式Definition上下文投影

Pose Graph Navigator MUST要求精确Definition context，并从Profile、Pose Graph和Gameplay composition roots分别投影Pose source与有限Action producer。Locomotion分组 MUST显示PoseState、Clip/BlendSpace/MM consumer、Source Slot业务名、Profile binding与实际资源名；Action分组 MUST显示Timeline、Track、AnimationChannel与AnimationSlot业务名。Navigator MUST不读取generated Program/Projection完成bootstrap，不按显示名猜测，不显示机器identity作为项目项名称，也不得保存第二份binding。

#### Scenario: 查看Locomotion sources

- **WHEN** 作者从Corin Definition展开Locomotion
- **THEN** Navigator MUST列出Idle、Start、Move、Stop、Turn的Source Slot及其实际资源
- **AND** MUST不列出BaseLocomotion Timeline producer或Source Id字符串

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索使用该图的任意角色或使用上一次窗口context

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Segment、Profile direct Clip Binding、Locomotion Sync Group、Clip注册Curve、Policy、Rig与Analysis状态。修改Action Segment编排 MUST导航到Timeline Editor；修改Clip骨骼或注册Curve MUST打开Unity Animation Window中的精确Clip与Preview Target；修改Profile Binding或Sync Group MUST导航到Profile；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。人工入口与Document v4 Reconciler MUST分别调用同一正式Mutation和资产事务，系统 MUST不复制字段、提供第二mutation命令、按窗口类型分叉写链或保留字符串binding镜像。

#### Scenario: 从Pose Graph调整Run Phase

- **WHEN** 作者在State source引用面板选择Open Source Curve
- **THEN** 必须打开RunLoop原生AnimationClip与正式Preview Target
- **AND** Pose Graph节点与Profile Binding MUST保持只读Clip引用摘要

### Requirement: Animation authoring工作区不得自动发布generated产物

打开Profile、Pose Graph或Timeline，选择producer、切换Details页签、修改authoring、切换Preview Target、保存资产、窗口focus、domain reload和AssetDatabase refresh MUST不自动执行Program Build、Projection Build、Foot Analysis batch或Motion Matching Database Build。工作区 MUST显示Dirty、Invalid、Stale、Ready或显式Building状态，只有明确Compile/Build命令 MAY调用现有正式发布事务。

#### Scenario: 选择Stale producer

- **WHEN** 作者在Navigator选择一个Projection已Stale的producer
- **THEN** Details MUST显示Stale来源与受影响revision
- **AND** 系统 MUST不因selection自动重建Projection或Foot Analysis

### Requirement: Locomotion Sync Group必须只装配直接Clip成员

`CharacterAnimationPresentationProfile` MUST唯一保存Locomotion Sync Group的稳定GroupId与精确AnimationClip成员引用。一个Clip MUST最多属于一个Group。Group成员 MUST具有合法Locomotion Phase曲线。Group MUST不保存素材同步点、Time Mapping、leader role、Topology、pairwise warp或Transition副本。

#### Scenario: Walk与Run加入同一Group

- **WHEN** 作者把WalkLoop与RunLoop加入`Locomotion.Gait`
- **THEN** Compiler MUST从两项Clip的Phase Curve与Loop事实构建可达relation
- **AND** MUST不要求两项Clip复制Group策略或Marker序列

### Requirement: Presentation Projection必须保存per-clip Phase与可达relation计划

Projection Compiler MUST为每个Locomotion Group成员编译固定容量forward/inverse Phase plan，把Direct Clip或Blend Space降低为`AnimationSourcePhasePlan`，并只为PoseState实际可达edge保存source-to-source relation。Direct Clip endpoint MUST引用自身Clip plan；Blend Space endpoint MUST引用显式Phase Reference Sample作为clock carrier和全部Dynamic Sample的per-clip inverse plan。Relation MUST包含RelationIdentity、TransitionId、两侧source plan identity、编译期固定leader、正式clock authority、实际有限秒域coverage与Artifact validation identity。Foot Analysis质量门槛不通过 MUST阻止Projection发布。Projection MUST不保存Editor AnimationCurve、Phase Validation samples、Marker occurrence、pairwise warp knot或Sequence identity。

#### Scenario: MovingTurn实际只播放28帧

- **WHEN** MovingTurn Clip长度为71帧但Gameplay committed clock只覆盖0至28帧
- **THEN** relation compiler MUST只校验和编译0至28帧实际coverage
- **AND** MUST不使用28帧后的Phase或Foot样本证明出口合法

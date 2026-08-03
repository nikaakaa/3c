# character-animation-presentation-authoring Specification

## Purpose

定义角色动画表现配置的唯一作者边界：CharacterPipelineDefinition只引用CharacterAnimationPresentationProfile，Profile唯一引用Pose Graph、state-local Pose source、有限Action producer、node-local Policy、Rig与Foot Analysis，Profile Inspector和Pose Graph Workspace提供各自唯一入口，并由编译链生成CharacterPresentationProjection。
## Requirements
### Requirement: Presentation Profile必须唯一绑定Pose source

Pose Graph MUST唯一拥有typed Source Slot子资产，`CharacterAnimationPresentationProfile` MUST为每个Slot唯一拥有类型匹配的typed Source Binding子资产。Sequence Binding MUST保存精确AnimationClip resource、Rig、loop、marker topology、marker sequence、source-local Foot Placement Weight typed curve与Foot Analysis配置；Pose Graph SequencePlayer MUST只引用精确Sequence Source Slot对象。Gameplay Graph、BTSMTL StateMachine、Timeline、ActionProfile、Prefab与generated Program MUST不复制持续Locomotion source binding。

#### Scenario: 作者替换Run动画

- **WHEN** 作者在Corin Presentation Profile把Run source绑定到新AnimationClip
- **THEN** PoseStateMachine topology与Gameplay Program MUST不需要修改
- **AND** Projection必须通过明确Build命令重建

### Requirement: 持续Sequence Pose Source必须拥有完整时间编辑表面

Presentation Profile MUST为每个持续Sequence Pose Source提供唯一`Pose Source Editor`。该表面 MUST使用正式时间尺、Sync Marker lane、typed Curve lane、Foot Analysis候选与Preview，并支持marker新增/删除/拖动、curve key多选/框选/精确值/切线/weighted tangent/复制粘贴和单次Undo事务。编辑结果 MUST写回该Profile binding，不得创建Timeline、Clip副本或第二curve资产。GUID、revision与hash MUST只出现在Diagnostics。

#### Scenario: 作者精确调整Run曲线

- **WHEN** 作者在Run Pose Source Editor框选多个Foot Placement Weight key并编辑weighted tangent
- **THEN** 唯一Profile binding typed curve MUST原子更新
- **AND** Run MUST不需要Timeline或普通Inspector CurveField

#### Scenario: 作者应用左脚接触候选

- **WHEN** 当前Foot Analysis artifact与source输入identity一致
- **THEN** 作者 MAY把选中的Left Foot候选显式应用为该binding的marker
- **AND** generated artifact MUST保持只读

### Requirement: PoseStateMachine工作区必须对齐UE作者口径

Pose Graph Workspace MUST显示State Machine、State、Transition Rule、State Alias、Sequence Player、Blend Space Player、Slot、Blend Logic与Inertialization等作者术语。StateMachine内部图、State Pose Graph和Transition Rule图 MUST使用明确下钻导航。Workspace MUST显示compiled active state、target state、transition progress、Slot playback、source usage和route，不得展示BTSMTL Gameplay State为Pose State。

#### Scenario: 作者打开Locomotion PoseStateMachine

- **WHEN** 作者双击PoseStateMachine节点
- **THEN** Workspace MUST显示Entry、State、Alias和Transition edge
- **AND** MUST不显示Gameplay Action或Timeline control edge

### Requirement: Action producer authoring必须只允许有限Timeline Action

`AnimationProducerPresentationBinding`、Profile Inspector与正式authoring mutation MUST只允许有限Action Timeline producer。Motion Matching、Blend Space与Sequence source MUST只通过`PresentationPoseSourceBinding`和PoseState source provider descriptor配置，MUST不作为Gameplay producer、AnimationChannel candidate或Action Playback Input。Projection Compiler MUST分别建立Action-only binding index与Pose source/provider binding index。

#### Scenario: 作者配置Locomotion Blend Space

- **WHEN** PoseState Pose Graph引用BlendSpacePlayer
- **THEN** Profile MUST建立Presentation Pose source binding
- **AND** Inspector MUST不提供Gameplay producer或Action channel选项

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

#### Scenario: 单Clip预分析

- **WHEN** 作者在Timeline工具中提前生成一个clip artifact
- **THEN** 该操作 MUST不发布Program或Projection
- **AND** 后续Definition Build MUST重新校验artifact后才能消费

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存动画表现数据。Profile MUST唯一引用Pose Graph、Pose source binding、有限Action producer source binding、node-local Policy、Rig与Foot Analysis配置。Pose Graph MUST唯一保存Presentation Fact Input、PoseStateMachine、SequencePlayer、AnimationSlot、Selection Player、composition、FootPlacement与Output topology。Gameplay Graph、BTSMTL StateMachine、Timeline、Presenter与Prefab MUST不复制这些配置。

#### Scenario: Corin配置动画表现

- **WHEN** Corin Definition引用正式Animation Presentation Profile
- **THEN** Profile MUST提供PoseStateMachine source与Action Slot producer的唯一资源绑定
- **AND** Definition MUST不内联Run Clip、State transition或Slot policy

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile和Analysis Source
- **AND** 每个角色 MUST生成与自身Calibration匹配的Projection
- **AND** shared Graph/Timeline MUST不保存角色级分析Rig或校准

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个有限Action Timeline animation producer MUST拥有稳定authoring producer identity；每个持续Pose source MUST由Graph-owned typed Source Slot与Profile-owned typed Binding对象表达。Compiler MUST把Action identity写入Program source map与Projection binding，把可达Slot/Binding按稳定对象身份降低为Presentation Projection内的连续dense source index。作者与Runtime MUST不保存Pose Source Id字符串，也 MUST不使用显示名、数组index、asset path或当前State名称作为fallback。

#### Scenario: Timeline Track 重排

- **WHEN** 作者重排 AnimationTrack 或 Clip
- **THEN** 原 producer identity MUST 保持
- **AND** Program 与 Projection binding MUST 不因列表 index 变化而 orphan

#### Scenario: 复制 inline Timeline producer

- **WHEN** 作者复制一个 inline TimelineNode 或 animation producer
- **THEN** 新 producer MUST 获得新 identity
- **AND** 系统 MUST 不让两个 producer 共用同一 Program source 或 playback state key

#### Scenario: binding 指向未知 producer

- **WHEN** Projection binding 无法解析到 Program manifest 中的 producer identity
- **THEN** Compiler/Validator MUST 报告 orphan binding
- **AND** Runtime MUST 拒绝 Program/Projection 组合，不能按名称或 Clip 猜测目标

### Requirement: Blend Policy必须属于明确transition owner

Blend Policy MUST属于明确的transition owner：PoseState Transition edge、AnimationSlot或保留的显式BlendStack。PoseState edge MUST保存该edge的`Standard Blend | Inertialization`、duration、`Linear | EaseIn | EaseOut | EaseInOut | Custom` Blend Mode、条件式强类型Custom Curve Asset与强类型Blend Profile；Custom MUST有Curve Asset，非Custom MUST不保留Curve Asset，非Hard Cut MUST有Blend Profile。AnimationSlot MUST按全部可达Action endpoint物化完整exact rule table；普通BlendStack MUST继续只管理连接到自身的多source历史。Timeline、Gameplay State edge、SequencePlayer、ActionProfile与Prefab MUST不保存第二份transition表，Animancer backend MUST不决定fade。

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

Profile Inspector MUST唯一编辑Pose Graph、Pose source binding、Blend Policy、Inertialization Policy、Rig Definition、有限Action producer source binding、Foot Analysis Mode与Analysis Source。持续Sequence source MUST从Profile进入Pose Source Editor编辑Clip、marker、SyncRole、typed curve、analysis与preview；BlendSpace与Motion Matching MUST从同一binding入口导航到各自正式编辑器。Timeline Editor继续唯一编辑Action producer-local Clip、Window、Motion、Cue、Timeline marker与curve。系统 MUST不要求为持续source创建Timeline，也 MUST不在普通Inspector保留marker文本或CurveField写入口。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

Profile Inspector MUST在显式Definition context下分别显示持续Pose source与有限Action producer。Pose source MUST按Source Slot、Binding与实际资源业务名显示消费它的PoseState/Sequence/BlendSpace/MM节点、resource、marker、Foot Placement Weight与analysis配置；Action producer MUST按Timeline、Track与资源业务名显示AnimationChannel、ActionPlaybackInput/AnimationSlot consumer与resource binding。GUID、local file id、revision、hash与compiled index只允许在显式Diagnostics中只读显示；服务 MUST不从显示名、目录、旧BaseLocomotion channel或generated产物反推authoring。

#### Scenario: 查看Run Pose source

- **WHEN** 作者从Corin Definition展开Locomotion PoseStateMachine
- **THEN** Inspector MUST显示Run source identity、consumer和Profile binding
- **AND** MUST不要求RunLoop Timeline producer

#### Scenario: 查看 Action 覆盖 Locomotion

- **WHEN** Definition context 同时包含 Action 与 Locomotion producer
- **THEN** Inspector MUST只列出各自的稳定 identity 与 binding
- **AND** MUST不推断覆盖关系或创建 Driver、Priority

### Requirement: Animation Marker Sync 必须由实际source owner唯一拥有

有限Action producer的Marker Sync数据 MUST继续由对应Timeline AnimationTrack唯一拥有。持续Pose source的Marker Sync数据 MUST由Profile中的对应Pose source binding唯一拥有。两类owner都 MUST保存明确None或MarkerGroup、SyncGroupId、Finite/Cyclic topology、SyncRole与ordered Point Marker；它们 MUST不互相复制，也不得把marker写入Gameplay StateMachine、Pose transition、Pose transition Rule、Blackboard、ActionProfile、FootPhase资产或独立Pose Graph MarkerSync节点。PoseState Compiler MUST只根据Transition两侧State的唯一Sequence或BlendSpace source binding推导可选同步计划，不得要求Transition作者重复选择同步模式。

#### Scenario: 编辑Attack marker

- **WHEN** 作者修改Attack1的finite marker
- **THEN** Timeline Editor MUST成为唯一写入口
- **AND** Profile MUST不复制该marker

#### Scenario: 编辑Run marker

- **WHEN** 作者修改Run Pose source的Locomotion.Gait marker
- **THEN** Profile Pose source editor MUST成为唯一写入口
- **AND** Timeline Editor MUST不创建RunLoop Track副本

#### Scenario: source明确不参与同步

- **WHEN** 作者把Action track或Pose source配置为`None`
- **THEN** 对应owner MUST原子清空SyncGroupId、topology、SyncRole和markers
- **AND** Runtime MUST保持该source的原始表现时间

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession与CharacterPipelineHost调试视图 MUST作为Presentation Fact、PoseState active/target/transition、Pose source relevance、committed Action producer、Timeline visual sample、Action Playback lifecycle、AnimationChannel、AnimationSlot、Player source usage、Stack/Stored、Inertialization residual、Marker relation、Pose contribution与Output completion的唯一调试入口。Definition Inspector、Profile Inspector、Timeline Editor与Pose Graph Workspace MUST只读取该正式Trace，不得复制另一套生命周期状态、重新运行Gameplay Graph、重采样Timeline、求值Pose Graph或从Animancer weight重建事实。

#### Scenario: 排查攻击切换

- **WHEN** FullBodyAction从None变为Attack1且Locomotion PoseState继续更新
- **THEN** Live Debug MUST显示当前PoseState、Attack1 command与首样本、Slot transition和最终Output贡献
- **AND** 数据 MUST来自正式 Trace

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

#### Scenario: AnimationClip内容变化

- **WHEN** AnimationClip imported content revision改变但Timeline作者曲线未改变
- **THEN** Projection Foot Analysis MUST变为Stale
- **AND** Timeline资产 MUST不被自动写入任何生成key

### Requirement: Marker Group 必须支持 Finite 与 Cyclic 序列

Action AnimationTrack与Presentation Pose source的MarkerGroup都 MUST声明Finite或Cyclic。Cyclic Action producer的全部Timeline call site MUST为Loop；Cyclic Pose source的Sequence/BlendSpace player MUST允许循环。Finite Action producer和Finite Pose source MUST覆盖各自完整duration且不回绕。冲突调用或非法topology MUST编译失败。

#### Scenario: shared Timeline全部以Loop调用

- **WHEN** 多个TimelineNode引用同一个Cyclic shared AnimationTrack
- **AND** 每个call site均使用Loop
- **THEN** Compiler MUST接受共享producer同步配置
- **AND** 每次activation MUST使用独立AnimationPlaybackId generation

#### Scenario: shared Timeline混合Once与Loop

- **WHEN** 同一AnimationTrack被一个Once与一个Loop TimelineNode调用
- **THEN** Compiler MUST报告明确的topology冲突及全部call site identity
- **AND** MUST不为不同调用点生成隐式同步覆盖或网络字段

### Requirement: Marker Group 必须显式声明 handoff 同步角色

每个Action AnimationTrack与Pose source MarkerGroup MUST显式声明`CanBeLeader`、`AlwaysLeader`或`AlwaysFollower`。PoseState Source Sync Plan与exact Action Selection MarkerSync MUST使用同一角色解析规则；角色冲突 MUST失败，MUST不按State、Action或Clip名称猜测leader。

#### Scenario: 有限停步保持自身节奏

- **WHEN** RunEnd被配置为MarkerGroup/Finite/AlwaysLeader并从RunLoop进入
- **THEN** Projection MUST保留AlwaysLeader角色
- **AND** Runtime MUST让RunEnd领导共同可见期

#### Scenario: 冲突角色不允许猜测

- **WHEN** 一次handoff的两侧都要求AlwaysLeader或都要求AlwaysFollower
- **THEN** Runtime MUST返回typed invalid reason
- **AND** Runtime MUST不按generation、名称或fallback猜测方向

### Requirement: Marker Group 必须在 Projection 构建前完整校验

Projection Build MUST分别校验Action AnimationTrack和Presentation Pose source的duration、marker identity、frame/time、有向pair、topology、role、resource coverage与共同可达SyncGroup pair集合。Pose source使用AnimationClip duration与Profile binding；Action producer使用Timeline duration与Track binding。任一缺失或跨owner冲突 MUST阻止发布，MUST不回退normalized time。

#### Scenario: Walk与Run使用不同时序

- **WHEN** WalkLoop和RunLoop属于同一组并拥有相同有向marker pair集合
- **AND** 两个producer的marker frame和segment时长不同
- **THEN** Compiler MUST接受该配置
- **AND** Projection MUST保存各producer自己的marker occurrence时间

#### Scenario: 有限序列重复MarkerId

- **WHEN** Finite producer使用`LeftPlant -> RightPlant -> LeftPlant`覆盖完整one-shot
- **THEN** Validator MUST接受重复LeftPlant语义id
- **AND** 两个LeftPlant occurrence MUST拥有不同稳定AuthoringId与frame

#### Scenario: 同组缺少有向segment

- **WHEN** 同组某target producer缺少其它producer可能成为source的有向marker pair
- **THEN** Compiler MUST报告group compatibility错误
- **AND** MUST不生成依赖运行时normalized-time fallback的Projection

#### Scenario: marker覆盖区存在无输出空洞

- **WHEN** marker映射可能落入AnimationTrack没有任何合法clip sample的区间
- **THEN** Validator MUST报告output coverage错误
- **AND** MUST不依赖RequireOutput、隐藏Idle或Animancer自动同步填补

### Requirement: Presentation Projection 必须保存规范化 Marker Sync 映射

Projection Compiler MUST把Action producer和Presentation Pose source的同步模式、canonical SyncGroupId、topology、role、duration、ordered marker与有向pair occurrence索引编入Projection。Action mapping MUST关联producer binding；Pose source mapping MUST关联Projection-local dense source index、typed source plan与State source consumer。两类映射 MUST只服务表现采样，不进入Gameplay Program ABI、State codec、Snapshot或Network协议。

#### Scenario: Marker Sync改变producer表现时间

- **WHEN** Runtime把incoming producer同步到新的VisualSampleTime
- **THEN** Foot Analysis MUST按该producer新的sample time采样
- **AND** MUST不读取MarkerId或segment名称作为plant事实

#### Scenario: AnimationClip或Calibration变化

- **WHEN** 任一生成输入revision改变
- **THEN** ProjectionRevision MUST更新且旧Projection MUST被拒绝
- **AND** Float32与Fixed Gameplay operation语义 MUST保持不变

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

Pose Graph Navigator MUST要求精确Definition context，并从Profile、Pose Graph和Gameplay composition roots分别投影Pose source与有限Action producer。Locomotion分组 MUST显示PoseState、Sequence/BlendSpace/MM consumer和Pose source binding；Action分组 MUST显示Timeline、Track、AnimationChannel与AnimationSlot consumer。Navigator MUST不读取generated Program/Projection完成bootstrap，不按显示名猜测，也不得保存第二份binding。

#### Scenario: 查看Locomotion sources

- **WHEN** 作者从Corin Definition展开Locomotion
- **THEN** Navigator MUST列出Idle、Start、Move、Stop、Turn的正式Pose source
- **AND** MUST不列出BaseLocomotion Timeline producer

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索使用该图的任意角色或使用上一次窗口context

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Workspace、Navigator与Details MAY只读显示Action Timeline Track和Profile Pose source的resource、marker、curve、Policy、Rig与analysis状态。修改Action Clip、marker、window或curve MUST导航到Timeline Editor；修改Pose source resource、marker或Foot Placement Weight MUST导航到Profile source editor；修改State transition与Slot Policy MUST导航到Pose Graph/Policy owner。人工Workspace与Document v3 Reconciler MUST把目标状态降低到同一typed Presentation Mutation和资产事务；系统 MUST不复制字段、提供第二mutation命令或按窗口类型分叉写链。

#### Scenario: 从Pose Graph调整Run marker

- **WHEN** 作者在State source引用面板选择Open Source
- **THEN** 必须打开Profile中的Run Pose source editor
- **AND** Pose Graph节点 MUST保持只读引用

### Requirement: Animation authoring工作区不得自动发布generated产物

打开Profile、Pose Graph或Timeline，选择producer、切换Details页签、修改authoring、切换Preview Target、保存资产、窗口focus、domain reload和AssetDatabase refresh MUST不自动执行Program Build、Projection Build、Foot Analysis batch或Motion Matching Database Build。工作区 MUST显示Dirty、Invalid、Stale、Ready或显式Building状态，只有明确Compile/Build命令 MAY调用现有正式发布事务。

#### Scenario: 选择Stale producer

- **WHEN** 作者在Navigator选择一个Projection已Stale的producer
- **THEN** Details MUST显示Stale来源与受影响revision
- **AND** 系统 MUST不因selection自动重建Projection或Foot Analysis

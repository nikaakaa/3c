# character-animation-presentation-authoring Specification

## Purpose

定义角色动画表现配置的唯一作者边界：CharacterPipelineDefinition只引用CharacterAnimationPresentationProfile，Profile唯一引用Pose Graph、node-local Blend/Inertialization Policy、Rig并保存稳定producer source binding，Profile Inspector提供唯一入口，并由编译链生成CharacterPresentationProjection。
## Requirements
### Requirement: Foot Analysis Source必须是显式可验证的表现作者输入

Editor-only `CharacterFootPlacementAnalysisSource` MUST拥有稳定identity、算法版本、固定sample rate、确定性reduction参数、精确Sampling Rig Asset GUID和唯一Rig Calibration。`CharacterAnimationPresentationProfile` MUST只保存该Source的Asset GUID；Inspector MAY通过对象选择写入GUID，但Profile与Source MUST不保存Sampling Rig `GameObject`强引用。Projection Builder MUST只按两个GUID解析精确Source与Prefab。Sampling Rig MUST通过显式`CharacterFootPlacementRig`绑定骨骼并引用同一Calibration；Compiler MUST不通过AssetDatabase反向搜索Runtime Prefab、骨骼名称、Humanoid mapping或默认Rig补全Source。Analysis Source与Sampling Rig MUST不进入Gameplay SourceRevision或Player依赖闭包。

#### Scenario: Profile缺少Sampling Rig

- **WHEN** GeneratedPerFootFeatures模式的Source没有合法Sampling Rig Asset GUID或该GUID无法解析
- **THEN** Profile validation与Definition Build MUST失败
- **AND** 系统 MUST不选择场景角色或任意Corin Prefab作为fallback

### Requirement: Foot Analysis必须由正式Projection Build生成

单AnimationClip feature MUST先由正式Artifact Builder按精确AnimationClip、Analysis Source、Sampling Rig、Calibration和算法输入生成Editor-only artifact。Definition Build MUST收集全部可达stable clip binding，精确校验或生成所需artifact，再把feature写入对应Projection binding。Projection发布仍 MUST发生在正式Build Transaction中；artifact本身不得成为Runtime或作者真相。

#### Scenario: 同一AnimationClip被多个producer引用

- **WHEN** 多个stable clip binding使用相同AnimationClip和Analysis Source
- **THEN** Build MAY复用同一artifact payload
- **AND** 每个Projection binding MUST仍按自己的Timeline/Track/Clip identity保存精确映射

#### Scenario: 单Clip预分析

- **WHEN** 作者在Timeline工具中提前生成一个clip artifact
- **THEN** 该操作 MUST不发布Program或Projection
- **AND** 后续Definition Build MUST重新校验artifact后才能消费

### Requirement: Pipeline Definition 必须引用唯一 Animation Presentation Profile

`CharacterPipelineDefinition` MUST引用唯一`CharacterAnimationPresentationProfile`，不得内联保存Animation Presentation数据。该Profile MUST唯一引用`CharacterPresentationPoseGraphAsset`、node-local `CharacterAnimationBlendPolicy`、`CharacterPoseInertializationPolicy`、`CharacterAnimationRigDefinition`，保存稳定producer source bindings，以及显式Foot Placement Analysis Mode与Analysis Source Asset GUID。Pose Graph MUST唯一保存Selection Input、MarkerSync、Player、Bone Mask composition、Pose Parameter policy、FootPlacement与Output topology；Policy只由对应节点引用。Analysis Source MUST是Editor-only Projection生成输入，只负责生成表现特征，不得保存Graph flow、State、Action、Gameplay contact或运行时IK状态。Profile MUST不持有Analysis Source或Sampling Rig对象强引用。Graph、StateMachine、Timeline、Presenter、Prefab重复数值、旧SO或独立Pipeline表 MUST不保存同一配置的第二份真相。

#### Scenario: Corin启用生成Foot Analysis

- **WHEN** Corin Profile选择GeneratedPerFootFeatures
- **THEN** Profile MUST保存可精确解析到唯一Analysis Source的Asset GUID
- **AND** Source MUST显式引用Sampling Rig与Rig Calibration
- **AND** Definition MUST不内联复制这些字段

#### Scenario: shared Graph被多个角色使用

- **WHEN** 两个CharacterPipelineDefinition引用同一个shared Graph/Timeline
- **THEN** 两个角色 MAY引用不同CharacterAnimationPresentationProfile和Analysis Source
- **AND** 每个角色 MUST生成与自身Calibration匹配的Projection
- **AND** shared Graph/Timeline MUST不保存角色级分析Rig或校准

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个 Timeline animation producer MUST 拥有稳定 authoring producer identity。Compiler MUST 将该 identity 同时写入 CharacterSimulationProgram source map 与 CharacterPresentationProjection binding；Runtime playback identity MUST 由 Program producer identity、ActorId、activation identity 和 playback generation 组合，不得使用显示名、数组 index、asset path、breadcrumb 或当前 Tree object identity 作为 fallback。

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

### Requirement: Blend Policy必须属于显式Blend Stack节点

`CharacterAnimationPresentationProfile` MUST通过Pose Graph中的显式Blend Stack节点引用`CharacterAnimationBlendPolicy`。Policy MUST保存Stack容量、Stored Pose policy、canonical curve、Blend Profile、authoring default和exact override；Compiler MUST只为引用该Policy的节点物化可达endpoint完整table。Timeline、BTSMTL Graph、Program、SelectedPosePlayer、Equipment Feature与Prefab MUST不保存第二份transition表。Animancer source backend MUST只复用和采样source playable，不得调用TransitionLibrary、AnimancerLayer.Play、StartFade或FadeGroup决定转场。

#### Scenario: 播放目标producer

- **WHEN** selected producer收到第一份合法sample
- **THEN** 对应显式BlendStack MUST按Projection中的exact source-target transition开始时间混合
- **AND** Animancer source backend MUST只提供该source pose sample

#### Scenario: FullBodyAction淡出到Empty

- **WHEN** FullBodyAction channel提交None且当前action source仍有贡献
- **THEN** FullBodyAction BlendStack MUST使用节点Policy中的source-to-Empty transition连续淡出
- **AND** 系统 MUST不从TransitionLibrary、Animancer state或默认duration补值

### Requirement: CharacterAnimationPresentationProfile Inspector必须是唯一Presentation配置入口

Profile Inspector MUST唯一编辑Pose Graph、Blend Policy、Inertialization Policy、Rig Definition、producer source binding、Foot Analysis Mode与Analysis Source GUID。Pose Graph Editor MUST编辑Selection Input、MarkerSync、SelectedPosePlayer、Blend Stack、Inertialization、Blend/Layered/Additive、Parameter、ModifyBone、FootPlacement和Output拓扑；Timeline Editor继续唯一编辑producer-local Clip、SyncGroup、Topology、SyncRole、Point Marker、Window与registered Curve。MarkerSync节点 MUST不复制Track marker数据；Profile、Timeline、Definition或Prefab MUST不保存Pose Graph节点配置副本。

#### Scenario: 从Profile打开Timeline Analysis

- **WHEN** 作者从精确Profile上下文打开Timeline并选择AnimationClip
- **THEN** Analysis provider MAY把该Profile的Source作为显式初始选择
- **AND** Timeline资产 MUST不因打开或分析而变脏

#### Scenario: shared Timeline用于不同角色

- **WHEN** 两个Profile使用同一shared Timeline但不同Analysis Source
- **THEN** 各自 MUST生成不同artifact identity与Projection
- **AND** shared Timeline MUST不保存任一角色的Analysis Source

### Requirement: Profile Inspector 必须按正式 identity 显示 producer binding

`CharacterAnimationPresentationProfile` Inspector MUST在显式Definition context下调用唯一`CharacterAnimationPresentationAuthoringService`，从该Definition的RootTree与正式composition roots递归发现可达Graph、Timeline与AnimationTrack，并按`TimelineAuthoringId + TrackAuthoringId`稳定producer identity显示AnimationChannelId、Selection Input PoseNodeId、source clip identity与resource binding。服务 MUST从Pose Graph的正式Selection Input声明解析binding，再允许后续Projection compile；MUST不读取已生成Program或Projection来完成bootstrap，也 MUST不按Layer、显示名、目录、列表index或旧binding猜测producer。Inspector MUST不推导StateMachine producer flow、不保存Tree node/edge副本，也 MUST不复制Pose Graph topology到第二张列表。正式运行时 MUST不依赖该只读authoring列表做selection、transition或composition。

#### Scenario: 查看 Attack1 到 Attack2

- **WHEN** 作者在包含 Attack1 与 Attack2 的 Definition context 下检查 Profile
- **THEN** Inspector MUST分别显示 Attack1 与 Attack2 的 producer identity、AnimationChannelId、Selection Input PoseNodeId 与 binding
- **AND** 状态 edge MUST只保存 condition、priority 与 interruption
- **AND** Inspector MUST不复制 Attack1 到 Attack2 的逻辑 edge

#### Scenario: 查看 Action 覆盖 Locomotion

- **WHEN** Definition context 同时包含 Action 与 Locomotion producer
- **THEN** Inspector MUST只列出各自的稳定 identity 与 binding
- **AND** MUST不推断覆盖关系或创建 Driver、Priority

### Requirement: Animation Marker Sync 必须由 AnimationTrack 唯一拥有

每个可达AnimationTrack MUST显式配置`Unspecified`、`None`或`MarkerGroup`同步模式，发布前 MUST拒绝`Unspecified`。`None` MUST不保留同步组、序列拓扑、同步角色或marker；`MarkerGroup` MUST在同一track保存非空SyncGroupId、`Finite`或`Cyclic`拓扑、`CanBeLeader`、`AlwaysLeader`或`AlwaysFollower`同步角色，以及至少两个包含稳定AuthoringId、语义MarkerId和Timeline frame的离散Point Marker。相邻Point Marker MUST隐式定义有向segment与fraction，系统 MUST不保存或推导第二条步态phase曲线。CharacterAnimationPresentationProfile、TimelineNode、Graph edge、StateMachine、Blackboard、ActionProfile、Foot Placement曲线、FootPhase资产、Distance曲线或独立同步Profile MUST不保存同一数据的第二份真相。

#### Scenario: 循环移动producer加入同步组

- **WHEN** 作者在Timeline Editor选择WalkLoop AnimationTrack
- **THEN** 作者 MUST能将其配置为`MarkerGroup/Cyclic`
- **AND** MUST能在同一track配置Locomotion.Gait与左右支撑marker
- **AND** MUST不要求作者再绘制步态phase曲线

#### Scenario: Timeline提供同组Marker名称候选

- **WHEN** Editor从同AnimationChannelId、同显式MarkerSync可达集合、同SyncGroup的正式AnimationTrack投影已使用MarkerId候选
- **THEN** 该候选 MUST只用于作者选择
- **AND** 唯一持久化真相 MUST仍是各AnimationTrack上的实际Point Marker
- **AND** MUST不创建全局Marker catalog、同步Profile或Projection反向写入入口
- **AND** CharacterAnimationPresentationProfile MUST不复制这些字段

#### Scenario: 有限动作producer加入同步组

- **WHEN** 作者选择一个由Once TimelineNode播放的RunEnd或Turn AnimationTrack
- **THEN** 作者 MUST能将其配置为`MarkerGroup/Finite`
- **AND** marker MUST覆盖该Timeline从frame 0到DurationFrame的完整有限序列

#### Scenario: producer明确不参与同步

- **WHEN** 作者将Attack或Dodge AnimationTrack配置为`None`
- **THEN** 该track MUST原子清空SyncGroupId、topology、SyncRole和markers
- **AND** Runtime MUST保持该producer的原始Timeline表现时间

#### Scenario: 发现未迁移track

- **WHEN** Definition inventory存在同步模式为Unspecified的可达AnimationTrack
- **THEN** Compiler与Agent Validator MUST拒绝发布
- **AND** 系统 MUST不按track、clip、state或action名称猜测模式

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession与CharacterPipelineHost调试视图 MUST作为committed producer command、Timeline visual sample、PendingFirstSample、AnimationChannelId、PoseNodeId、Player source usage、Stack entry/clock/Stored、Inertialization residual、source identity、Marker relation、Pose Graph contribution与OutputPose completion的唯一生命周期调试入口。CharacterPipelineDefinition Inspector与CharacterAnimationPresentationProfile Inspector MUST不复制该Trace UI。Editor MUST不重新运行Graph、重建Program command、重采样Gameplay Timeline、求值Pose Graph、自行混合或从Animancer weight重建事实。

#### Scenario: 排查攻击切换

- **WHEN** FullBodyAction committed producer从None变为Attack1且BaseLocomotion继续输出
- **THEN** Host Live Debug MUST显示两个channel command EventId、Attack1首样本、Action BlendStack PoseNodeId与最终OutputPose贡献
- **AND** 数据 MUST来自正式 Trace

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

Animation Clip MUST继续唯一保存Weight、Ease In、Ease Out与单一Foot Placement Weight曲线。Timeline Curve Channel Catalog MUST只为这些正式可写曲线提供稳定ChannelId、domain、完整读取、mutation和validator。左右脚sole速度、高度、plant confidence、next landing confidence、delay与offset MUST由Compiler生成并保存在CharacterPresentationProjection，不得注册为可写Curve Channel、保存回Animation Clip或CharacterAnimationPresentationProfile。Editor MAY使用独立只读Analysis renderer显示生成曲线，但 MUST不改变既有typed Curve mutation语义。

#### Scenario: 展开Animation Clip作者曲线

- **WHEN** 作者展开Animation Track的CURVES分组
- **THEN** MUST继续显示Weight、Ease In、Ease Out与Foot Placement Weight四个registered channel
- **AND** Left/Right Plant或Landing数据 MUST不出现在可写Catalog

#### Scenario: AnimationClip内容变化

- **WHEN** AnimationClip imported content revision改变但Timeline作者曲线未改变
- **THEN** Projection Foot Analysis MUST变为Stale
- **AND** Timeline资产 MUST不被自动写入任何生成key

### Requirement: Marker Group 必须支持 Finite 与 Cyclic 序列

MarkerGroup producer MUST声明`Finite`或`Cyclic`拓扑。Cyclic producer的全部可达TimelineNode call site MUST使用Loop，末marker到首marker MUST形成回绕segment；Finite producer的全部call site MUST使用Once，首marker MUST位于frame 0、末marker MUST位于DurationFrame且 MUST不回绕。同一shared producer存在混合Once/Loop call site时 MUST编译失败，不得按call site覆盖track同步配置或扩张PresentationCommand身份。

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

MarkerGroup producer MUST显式声明`CanBeLeader`、`AlwaysLeader`或`AlwaysFollower`。`CanBeLeader` MUST在没有强制角色时保持outgoing Current领导incoming target；`AlwaysLeader` MUST保持该producer自己的raw表现节奏并让另一侧映射到它；`AlwaysFollower` MUST映射到另一侧。`None` MUST不保留同步角色，发布前 MUST拒绝Unspecified角色。

#### Scenario: 有限停步保持自身节奏

- **WHEN** RunEnd被配置为MarkerGroup/Finite/AlwaysLeader并从RunLoop进入
- **THEN** Projection MUST保留AlwaysLeader角色
- **AND** Runtime MUST让RunEnd领导共同可见期

#### Scenario: 冲突角色不允许猜测

- **WHEN** 一次handoff的两侧都要求AlwaysLeader或都要求AlwaysFollower
- **THEN** Runtime MUST返回typed invalid reason
- **AND** Runtime MUST不按generation、名称或fallback猜测方向

### Requirement: Marker Group 必须在 Projection 构建前完整校验

MarkerGroup的Timeline duration MUST为有限正值；MarkerAuthoringId和frame MUST在track内唯一，MarkerId MUST非空且 MAY重复。每个相邻marker MUST形成非零有向segment，AnimationTrack在marker覆盖区内 MUST持续产生正式animation output。同一AnimationChannelId、同一显式MarkerSync可达集合与canonical SyncGroupId内的所有producer MUST拥有相同的有向`PreviousMarkerId -> NextMarkerId`集合，允许相同pair出现次数和frame不同。Inspector、Compiler、Projection Builder和Agent Validator MUST复用唯一校验服务。

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

Compiler MUST将每个animation producer的同步模式、canonical SyncGroupId、Finite/Cyclic topology、同步角色、duration、按时间排序的marker、按有向pair建立的occurrence索引，以及每个Animation Clip与其stable identity匹配的生成Foot Analysis编入CharacterPresentationProjection。Marker Sync与Foot Analysis MUST分别属于表现采样时间映射和每脚动画特征，不得互相成为真相。二者 MUST只属于Presentation resource binding与ProjectionRevision，不得进入Gameplay Semantic operation payload、Numeric Target Program ABI、Character state codec、StateHash、Snapshot或Network协议。Runtime MUST只读取与Program、producer、clip、Analysis Source和Calibration identity匹配的Projection，不得读取authoring TimelineData或运行时AnimationClip分析。

#### Scenario: Marker Sync改变producer表现时间

- **WHEN** Runtime把incoming producer同步到新的VisualSampleTime
- **THEN** Foot Analysis MUST按该producer新的sample time采样
- **AND** MUST不读取MarkerId或segment名称作为plant事实

#### Scenario: AnimationClip或Calibration变化

- **WHEN** 任一生成输入revision改变
- **THEN** ProjectionRevision MUST更新且旧Projection MUST被拒绝
- **AND** Float32与Fixed Gameplay operation语义 MUST保持不变

### Requirement: Equipment Presentation 不得拥有动画空间拓扑

Equipment Feature authoring MUST不保存LayerId、BlendMode、OutputPolicy或Presentation producer requirement。`EquipmentFeatureRouteImplementation.RequiredProducerIds` MUST仅表达Gameplay route完整性，MUST不进入Presentation Projection的channel、Player、transition或Animancer resolution。Equipment Presentation Profile与Projection MUST只保存VisualBinding、Prefab/socket、Renderer登记与local pose资源绑定，MUST不复制Selection Input或PoseNode字段，也 MUST不提前创建Equipment到Pose Graph的动态替换接口。

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

Pose Graph Producer Navigator MUST要求精确`CharacterPipelineDefinition`上下文，并调用唯一`CharacterAnimationPresentationAuthoringService`从该Definition的composition roots递归发现可达Graph、Timeline、AnimationTrack和stable producer identity。Navigator MUST按AnimationChannel和source owner分组显示producer、Clip identity、Sync模式与可用导航，不得扫描目录、读取generated Program/Projection完成bootstrap、按显示名或列表index猜测，也不得保存第二份producer binding或flow。

#### Scenario: 查看BaseLocomotion producers

- **WHEN** 作者从Corin Definition上下文打开Pose Graph并展开BaseLocomotion
- **THEN** Navigator MUST列出该Definition正式可达的Idle、Start、Loop、Turn与End producer identity
- **AND** 每个条目 MUST精确定位其Timeline、Track和Clip owner
- **AND** Pose Graph资产 MUST不因展开、搜索或定位而变脏

#### Scenario: 缺少Definition上下文

- **WHEN** 作者直接打开shared Pose Graph且没有精确Definition call-site context
- **THEN** Producer Navigator MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索使用该图的任意角色或使用上一次窗口context

### Requirement: 跨资产表现配置必须保持唯一写入口

Pose Graph Navigator、Details与Bottom Dock MAY只读显示AnimationTrack的Clip、SyncGroup、Topology、SyncRole、Marker，Profile/Rig/Policy owner以及generated Foot Analysis状态。修改Clip、Marker和registered Curve MUST精确导航到Timeline Editor；修改Profile、Rig、Blend Policy、Inertialization Policy和Analysis Source MUST精确导航到各自正式Inspector。Pose Graph Workspace MUST不复制这些字段、直接写SerializedProperty或提供第二mutation命令。

#### Scenario: 从Sync面板调整脚接触Marker

- **WHEN** 作者在Pose Graph Sync面板查看WalkLoop与RunLoop Marker
- **THEN** 面板 MUST保持只读并提供Open Source Timeline
- **AND** Timeline Editor MUST成为移动Marker的唯一正式入口
- **AND** Pose Graph与Profile MUST不保存Marker副本

### Requirement: Animation authoring工作区不得自动发布generated产物

打开Profile、Pose Graph或Timeline，选择producer、切换Details页签、修改authoring、切换Preview Target、保存资产、窗口focus、domain reload和AssetDatabase refresh MUST不自动执行Program Build、Projection Build、Foot Analysis batch或Motion Matching Database Build。工作区 MUST显示Dirty、Invalid、Stale、Ready或显式Building状态，只有明确Compile/Build命令 MAY调用现有正式发布事务。

#### Scenario: 选择Stale producer

- **WHEN** 作者在Navigator选择一个Projection已Stale的producer
- **THEN** Details MUST显示Stale来源与受影响revision
- **AND** 系统 MUST不因selection自动重建Projection或Foot Analysis


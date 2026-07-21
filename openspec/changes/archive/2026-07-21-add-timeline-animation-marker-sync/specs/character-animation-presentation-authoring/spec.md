## ADDED Requirements

### Requirement: Animation Marker Sync 必须由 AnimationTrack 唯一拥有

每个可达AnimationTrack MUST显式配置`Unspecified`、`None`或`MarkerGroup`同步模式，发布前 MUST拒绝`Unspecified`。`None` MUST不保留同步组、序列拓扑、同步角色或marker；`MarkerGroup` MUST在同一track保存非空SyncGroupId、`Finite`或`Cyclic`拓扑、`CanBeLeader`、`AlwaysLeader`或`AlwaysFollower`同步角色，以及至少两个包含稳定AuthoringId、语义MarkerId和Timeline frame的离散Point Marker。相邻Point Marker MUST隐式定义有向segment与fraction，系统 MUST不保存或推导第二条步态phase曲线。CharacterAnimationPresentationProfile、TimelineNode、Graph edge、StateMachine、Blackboard、ActionProfile、Foot Placement曲线、FootPhase资产、Distance曲线或独立同步Profile MUST不保存同一数据的第二份真相。

#### Scenario: 循环移动producer加入同步组

- **WHEN** 作者在Timeline Editor选择WalkLoop AnimationTrack
- **THEN** 作者 MUST能将其配置为`MarkerGroup/Cyclic`
- **AND** MUST能在同一track配置Locomotion.Gait与左右支撑marker
- **AND** MUST不要求作者再绘制步态phase曲线

#### Scenario: Timeline提供同组Marker名称候选

- **WHEN** Editor从同Layer同SyncGroup的正式AnimationTrack投影已使用MarkerId候选
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

### Requirement: Animation Clip控制曲线必须作为typed Curve Channel编辑

Animation Clip MUST继续唯一保存Weight、Ease In、Ease Out与单一Foot Placement Weight曲线。Timeline Curve Channel Catalog MUST为四条曲线提供稳定ChannelId、ClipNormalized时间域、`[0,1]`值域、完整AnimationCurve读取与正式Animation Clip mutation。CharacterAnimationPresentationProfile MUST不复制这些曲线；Marker Sync MUST不读取任何curve作为phase；Foot Placement MUST只读取Foot Placement Weight channel而不读取Weight、Ease或Marker。Editor通用化 MUST不改变CharacterPresentationProjection对这些曲线的既有投影与消费语义。

#### Scenario: 展开Animation Clip曲线

- **WHEN** 作者在Timeline展开Animation Track的CURVES分组
- **THEN** MUST显示Weight、Ease In、Ease Out与Foot Placement Weight四个registered channel
- **AND** 四条curve MUST继续由各自Animation Clip持有
- **AND** Foot Placement Weight MUST不再由专用Curve View硬编码

#### Scenario: 编辑Animation Weight曲线

- **WHEN** 作者修改Animation Clip的Weight channel
- **THEN** Curve Editor MUST通过该channel的正式MutationAdapter原子替换完整curve
- **AND** CharacterPresentationProjection MUST沿既有producer内部weight链重新生成
- **AND** Marker Sync binding MUST保持不变

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

MarkerGroup的Timeline duration MUST为有限正值；MarkerAuthoringId和frame MUST在track内唯一，MarkerId MUST非空且 MAY重复。每个相邻marker MUST形成非零有向segment，AnimationTrack在marker覆盖区内 MUST持续产生正式animation output。同一Layer与canonical SyncGroupId内的所有producer MUST拥有相同的有向`PreviousMarkerId -> NextMarkerId`集合，允许相同pair出现次数和frame不同。Inspector、Compiler、Projection Builder和Agent Validator MUST复用唯一校验服务。

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

Compiler MUST将每个animation producer的同步模式、canonical SyncGroupId、Finite/Cyclic topology、同步角色、duration、按时间排序的marker和按有向pair建立的occurrence索引编入CharacterPresentationProjection。同步映射 MUST只属于Presentation resource binding与source revision，不得进入Gameplay Semantic operation payload、Numeric Target Program ABI、Character state codec、StateHash、Snapshot或Network协议。Runtime MUST只读取与Program identity匹配的Projection，不得读取authoring TimelineData。

#### Scenario: 作者移动一个marker

- **WHEN** 作者移动WalkLoop的LeftPlant marker并重新编译Definition
- **THEN** source revision与Presentation Projection MUST更新
- **AND** Float32与Fixed Gameplay operation语义 MUST保持不变

#### Scenario: Runtime加载不匹配Projection

- **WHEN** Program与Projection的source revision、producer identity或sync binding不匹配
- **THEN** Host MUST在创建Presentation runtime前拒绝该组合
- **AND** MUST不从Timeline资产即时补建marker map

## MODIFIED Requirements

### Requirement: CharacterAnimationPresentationProfile Inspector 必须是唯一 Presentation 配置入口

系统 MUST在`CharacterAnimationPresentationProfile` Inspector中唯一编辑Layer catalog、TransitionLibrary引用与producer presentation binding。CharacterPipelineDefinition Inspector MUST只编辑Profile引用并提供打开该资产的导航，不得保存或显示这些数据的可写副本。系统 MUST不提供独立Animation Presentation窗口；Graph Inspector与StateMachine Editor MUST不提供这些数据的可写副本。Timeline Editor继续独占LayerId、clip、time、loop、ease、producer内部Weight、producer-local Marker Sync以及Animation Clip注册的Continuous Curve Channel；Timeline Editor MUST不编辑Layer catalog、TransitionLibrary或producer-to-transition binding。

#### Scenario: 编辑producer transition

- **WHEN** 作者在CharacterAnimationPresentationProfile Inspector选择一个animation producer
- **THEN** 作者 MUST能查看其layer、stable key与Animancer transition binding
- **AND** transition细节 MUST通过Animancer正式authoring API或窗口编辑
- **AND** Graph/Timeline逻辑资产 MUST保持不变

#### Scenario: 编辑Timeline clip、marker与curve

- **WHEN** 作者需要修改clip时间、Marker Sync或registered Curve Channel
- **THEN** CharacterAnimationPresentationProfile Inspector MUST导航到独立Timeline Editor
- **AND** MUST不复制这些producer-local字段

#### Scenario: 同时观察逻辑与Timeline

- **WHEN** 作者从CharacterAnimationPresentationProfile Inspector打开来源Graph和Timeline
- **THEN** Graph与Timeline MUST保持两个可同时观察的独立窗口
- **AND** Timeline MUST不进入Graph页签栈
- **AND** 系统 MUST不创建第三个Presentation窗口

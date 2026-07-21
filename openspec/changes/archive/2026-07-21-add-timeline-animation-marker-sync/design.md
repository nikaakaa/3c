# Design: Timeline 动画 Marker Sync

## Context

当前 AnimationTrack 已经是动画 producer 的正式作者单位：

```text
AnimationProducerId = TimelineAuthoringId + TrackAuthoringId
AnimationPlaybackId = AnimationProducerId + generation
```

Program/Projection、PresentationCommand、网络 checkpoint 与 playback lifecycle 都只认识 producer 和 generation。TimelineNode 只决定某个 Timeline 以 `Once` 或 `Loop` 方式激活，不进入动画表现身份。一个 AnimationTrack 还可能包含多个重叠 AnimationClip，最终由 Timeline 在同一 producer 内采样 membership、clip time、ease 与 weight。

因此 Marker Sync 必须调整“整个 producer 在什么 Timeline 时间被采样”，不能只改某一个 clip 的 Animancer state，也不能把策略挂在 State edge、TimelineNode call site 或 AnimationPresentationProfile pair table 上。

公开实现提供了相同方向：Unreal 的 Sync Group/Sync Marker 让相关 Animation Player 显式加入组，并按共同 marker 区间同步；Animancer 的 Mixer synchronization 也是显式 opt-in，Idle 等不适合的动画应关闭；Unity Blend Tree 要求相似动作的脚接触位于一致 normalized phase。项目不能直接使用 Animancer 自动同步，因为本项目 Timeline 每个表现帧都会重新提交 clip membership、time 和内部 weight，Animancer 若同时推进 normalized time 会形成两个时钟。

参考：

- Unreal Animation Sync Groups: https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-sync-groups-in-unreal-engine
- Animancer Mixer Synchronization: https://kybernetik.com.au/animancer/docs/manual/blending/mixers/synchronization/
- Unity Blend Trees: https://docs.unity3d.com/6000.5/Documentation/Manual/class-BlendTree.html

## Goals

- 所有 animation-producing Timeline 都使用同一套可选 Marker Sync 能力。
- 同时支持循环序列和有限时长序列，不按 Locomotion/Attack 名称分叉 runtime。
- 在整个 source/target 共同可见期持续同步，避免一次性 offset 后重新漂移。
- 只在实际 AnimationPlaybackLifecycle outgoing Current 与 incoming target 之间按 SyncRole 解析 leader，不复制 Gameplay 选择或动画层仲裁。
- 保持 Timeline raw time、effective sample time和 Animancer fade 三个职责清楚。
- 让 Editor、Compiler、Projection、Runtime、Preview、Live Debug 与 Agent 共享同一作者模型和校验。

## Non-Goals

- 不改变 Gameplay 状态切换、Action window、Motion、World solve 或网络结果。
- 不从骨骼、动画事件、clip 名称或状态名自动推断 marker。
- 不让 marker 表示 Gameplay hit、cancel、IFrame 或 Foot Placement contact。
- 不创建通用多候选动画图、BlendTree leader 选举或自定义 crossfade。
- 不把 Unity AnimationClip 内部全部骨骼/属性曲线复制进 Timeline，也不把 Timeline 扩张为 Animation Sequence 资源编辑器。

## Terms

### Raw Visual Time

`AnimationSamplingState.ResolveTime` 根据已提交 logic sample、cycle、interpolation alpha 和 Presentation delta 得到的 producer 原始表现时间。它仍是 Timeline 进度的连续结果。

### Effective Visual Time

Marker Sync 在正式 producer sampler 前得到的最终采样时间。不同组或 `None` producer 的 effective time 等于 raw time。

### Marker Group

同一 Layer 内一组具有共同相邻 marker 语义的 producer。运行时 group key 为：

```text
LayerId + CanonicalSyncGroupId
```

组名只在作者与 Projection 构建阶段规范化；PresentationFrame 不扫描字符串建立组。

### Marker Segment

按 Timeline frame 相邻的两个 marker 形成一个有向 segment：

```text
PreviousMarkerId -> NextMarkerId
SegmentFraction
```

MarkerId 可以在同一有限序列中重复，例如 `LeftPlant -> RightPlant -> LeftPlant`，因为稳定寻址由 MarkerAuthoringId 完成。重复 MarkerId 允许表达有限动作覆盖一个完整步态周期。

### Sync Relation

一次 source playback 与 target playback 在共同可见期间的纯表现时间依赖：

```text
SourcePlaybackId -> TargetPlaybackId
TargetSegmentOccurrence
LastSourceEffectiveTime
LastTargetEffectiveTime
```

它不是 Gameplay transition，也不是 Animancer fade 记录。

## Timeline Authoring Content Model

Timeline 作者界面先按内容的时间语义分成三类：

```text
Span Clip
  在起止帧之间持续存在
  AnimationClip、MotionCurveClip、MotionWarpClip、TreeClip

Point Marker
  只发生在一个整数帧
  AnimationSyncMarker、Action Cue 等离散事实

Continuous Curve
  在一段归一化或绝对时间上连续求值
  Foot Placement Weight、未来独立的 Distance Curve
```

该分类不是新的统一序列化基类，也不是新的 Runtime Track 类型。三类内容只在 Editor 层共享以下基础能力：

- 同一 Timeline frame geometry 与整数帧吸附。
- stable owner/element identity 驱动的选择与 Inspector 定位。
- pointer capture 期间的本地草稿预览。
- Pointer Up 或意外 Capture Out 时的一次正式 Undo 提交。
- Pointer Cancel 时丢弃草稿。
- 提交后的 dirty、RebindTimeline、校验摘要和 Authoring Preview 刷新。

数据所有权继续按业务语义分开：Span Clip 由正式 Track 持有；Animation Sync Point Marker 由父 AnimationTrack 持有；Foot Placement Continuous Curve 由对应 Animation Clip 持有。任何共享 Editor 交互都不得引入宽 DTO、第二份 TimelineData 或运行时多态分派。

### 为什么 Marker Sync 是 Point Marker

步调同步需要回答的是“当前位于哪两个命名姿态事件之间，以及走过了多少比例”：

```text
RightFootContact -> LeftFootContact -> RightFootContact(next cycle)
```

左右接触是离散点，区间 fraction 由相邻点的时间自动计算，不需要作者再画一条 phase 曲线。曲线适合表达连续权重或距离，不适合同时承担 Marker 名称、顺序和循环拓扑。若把步调做成曲线，Editor 无法直接校验同组 producer 是否拥有相同有向 pair，也会与 Foot Placement Weight 和未来 Distance Matching 形成含义重叠。

### 与 UE 作者语义的对应

本项目借鉴 UE Marker-Based Sync 的作者语义，而不复制其 AnimGraph 所有权：作者在时间轴 Marker Lane 放置命名 Sync Marker，运行时用当前 Marker Pair 与 fraction 映射 follower。区别是本项目的播放单位是 Timeline AnimationTrack producer，而不是单个 Animation Sequence Player，因此 marker 继续由 AnimationTrack 拥有并沿 producer 时间轴编辑。

同一 AnimationTrack 可以包含重叠 clip、ClipIn、ease 与内部 weight。若 marker 改为 AnimationClip 资源所有，同一个 producer 会出现多个并列 marker 时钟，无法确定哪份数据对应最终 producer sample。因此不能为了外观接近 UE 而改变正式所有权。

### Typed Continuous Curve Channel

Curve Editor不直接认识`FootPlacementCurve`、`PositionX`或`YawProgressCurve`字段，而是消费显式注册的typed channel descriptor：

```text
TimelineCurveChannelDescriptor
  ChannelId
  OwnerType
  DisplayName
  Color
  TimeDomain
  ValueDomain
  Unit
  DefaultCurveFactory
  ReadAdapter
  MutationAdapter
  Validator
```

- `ChannelId`是稳定代码identity，不使用Inspector显示名或C#字段名寻址。
- `OwnerType`明确曲线由哪个正式Clip类型持有。
- `TimeDomain`明确key time如何映射到Timeline。现有首批channel均为`ClipNormalized[0,1]`，显示时由descriptor映射到owner Clip的StartFrame..EndFrame。
- `ValueDomain`明确`Bounded(min,max)`或`Unbounded`，避免把位移和Yaw错误Clamp到权重范围。
- `ReadAdapter`只返回owner当前完整AnimationCurve副本。
- `MutationAdapter`只调用owner正式authoring API原子替换完整曲线。
- `Validator`由对应领域拥有，Curve Editor只显示结果，不复制业务规则。

Catalog通过代码显式注册，不使用反射、`SerializedProperty.propertyPath`、字段名扫描或任意字符串setter。新增curve channel必须同时提供正式owner、mutation、校验与已存在的Compiler/Projection/Program消费者；Catalog本身不赋予曲线运行时含义。

### 首批 Curve Channel Catalog

| Owner | Channel | Time | Value |
|---|---|---|---|
| Animation Clip | Weight | Clip normalized | `[0,1]` |
| Animation Clip | Ease In | Clip normalized | `[0,1]` |
| Animation Clip | Ease Out | Clip normalized | `[0,1]` |
| Animation Clip | Foot Placement Weight | Clip normalized | `[0,1]` |
| MotionCurve Clip | Weight | Clip normalized | `[0,1]` |
| MotionCurve Clip | Position X/Y/Z | Clip normalized | unbounded，meter |
| MotionCurve Clip | Yaw | Clip normalized | unbounded，degree |
| MotionCurve Clip | Ease In/Out | Clip normalized | `[0,1]` |
| MotionWarp Clip | Position Progress | Clip normalized | `[0,1]`且单调 |
| MotionWarp Clip | Yaw Progress | Clip normalized | `[0,1]`且单调 |
| CameraStateClip、CameraResponseClip | Weight | Clip normalized | `[0,1]` |
| CameraStateClip、CameraResponseClip | Ease In/Out | Clip normalized | `[0,1]` |

该清单只覆盖仓库当前Timeline Clip已经拥有的正式AnimationCurve。RootMotionCurveAsset属于外部烘焙资产，不被复制成Timeline内联曲线；导入AnimationClip内部的骨骼、BlendShape与属性曲线也不进入Catalog。

### Curve Key身份与原子修改

Unity `Keyframe`没有稳定AuthoringId。系统不为每个key另造序列化identity，也不允许Agent按数组index发单key补丁。Curve修改以以下单位原子完成：

```text
OwnerAuthoringId + ChannelId + Full AnimationCurve
```

Editor在一个打开的curve revision内可以使用`ChannelId + keyIndex`作为临时选择句柄；一旦owner被外部修改或重新绑定，临时选择必须失效并重新读取。Agent Snapshot输出完整有序key，Patch原子替换整个channel。这样不会为简单Keyframe创建第二套identity系统，也不会因key重排把修改写到错误key。

完整Keyframe字段必须无损保留：

```text
time
value
inTangent / outTangent
inWeight / outWeight
WeightedMode
preWrapMode / postWrapMode
```

任何只保存time/value的Editor或Agent路径都属于数据损坏，必须删除。

## Authoring Ownership

### AnimationTrack 配置

每个可达 AnimationTrack 保存：

```text
AnimationSyncMode
  Unspecified
  None
  MarkerGroup

MarkerGroup:
  SyncGroupId
  SequenceTopology: Finite | Cyclic
  SyncRole: CanBeLeader | AlwaysLeader | AlwaysFollower
  Markers[]
    AuthoringId
    MarkerId
    Frame
```

- `Unspecified` 只用于发现未迁移资产，Compiler 与 Agent Validator必须拒绝发布。
- `None` 明确表示不参与 Marker Sync，并原子清空 group、topology、role 和 markers。
- `MarkerGroup` 表示 producer 可与同层、同组 producer 建立 relation。
- `CanBeLeader` 表示在没有强制角色时沿用 outgoing Current 领导 incoming target。
- `AlwaysLeader` 表示该 producer 参与 handoff 时必须保持自己的 raw 表现节奏，另一侧映射到它。
- `AlwaysFollower` 表示该 producer 参与 handoff 时必须映射到另一侧；两个 AlwaysLeader 或两个 AlwaysFollower 相遇属于配置冲突，运行时明确失败。

同步配置不属于 CharacterAnimationPresentationProfile。Profile 继续只保存 Layer catalog、Animancer TransitionLibrary 与 producer transition binding。Marker 位于 producer 时间轴，必须与 clip/time/ease 在同一 Timeline Editor 中编辑。

### 为什么不放在 TimelineNode

同一个 shared Timeline 可以被多个 TimelineNode 调用，但它仍生成同一个 producer identity。若不同 call site 能覆盖 group 或 marker，Projection 和 runtime command 无法判断本次激活应使用哪份配置，除非把 Node identity 加入 Program producer、PresentationCommand 和网络 codec。这会让纯表现 authoring 污染 Gameplay ABI。

因此规则固定为：

- `Cyclic` track 的全部可达 call site 必须是 `TimelinePlaybackMode.Loop`。
- `Finite` track 的全部可达 call site 必须是 `TimelinePlaybackMode.Once`。
- 同一 shared producer 出现混合 Once/Loop call site 时编译失败；业务确实需要两种语义时必须拆成两个 producer。

### Marker 合法性

所有 MarkerGroup producer必须满足：

- SyncGroupId 规范化后非空。
- Timeline duration 为有限正值。
- MarkerAuthoringId 非空且在 track 内唯一。
- MarkerId 非空、无首尾空白；允许在同一 track 重复。
- marker frame 单调、唯一且位于合法时间范围。
- 任一相邻 marker 形成非零长度 segment。
- AnimationTrack 在 marker 覆盖区内始终能产生正式 animation output。

拓扑附加规则：

- `Cyclic` marker frame 范围为 `[0, DurationFrame)`，末 marker 到首 marker 形成回绕 segment。
- `Finite` 不回绕；首 marker 必须位于 frame 0，末 marker 必须位于 Timeline DurationFrame，使整个 one-shot 都有明确覆盖。
- 同一 Layer + SyncGroup 的每个 producer必须拥有相同的有向 `PreviousMarkerId -> NextMarkerId` 集合；允许 marker 出现次数和 frame 不同。
- 相同有向 pair 在一个 target 中可以出现多次，Compiler预构建 occurrence 表。

该组合同使任一 source segment 在任一 target 中都有合法映射，不需要运行时按 normalized time fallback。

## Projection Contract

Compiler把合法作者数据降为不可变 Presentation binding：

```text
AnimationMarkerSyncBinding
  Mode
  CanonicalGroupId
  SequenceTopology
  DurationSeconds
  OrderedMarkers[]
    MarkerAuthoringId
    CanonicalMarkerId
    TimeSeconds
  SegmentOccurrencesByPair
```

Projection构建阶段完成：

- group key 规范化。
- marker 排序与 frame-to-seconds 转换。
- Finite/Cyclic segment 构建。
- directed pair set 兼容性校验。
- occurrence 索引构建。
- output coverage 校验。

Runtime只读 Projection，不读取 TimelineData、AnimationTrack、Asset YAML 或 PresentationProfile。

同步数据的身份影响固定为：

- 进入 Definition source revision 与 CharacterPresentationProjection。
- Program wrapper 因 source revision 变化重新生成。
- 不进入 Gameplay Semantic operation payload。
- 不提升 Float32/Fixed Numeric Target ABI 或 Character state codec。
- 不进入 Snapshot、StateHash、packet、checkpoint 或 reconciliation policy。

## Runtime Flow

```text
Committed PresentationCommand
  -> AnimationSamplingState 解析 raw visual time
  -> AnimationPlaybackLifecycle 收集 Current/Pending/Outgoing demand
  -> MarkerSyncRuntime 维护 relation 与依赖顺序
  -> source effective segment + fraction
  -> target segment occurrence + mapped effective time
  -> CharacterPresentationAnimationBinding.Sample(effective time)
  -> AnimationPlaybackLifecycle Apply
  -> Animancer Play/UpdateSample/Evaluate
  -> retire notification
  -> relation detach/rebase
```

### Pairwise Leader Resolution

当前生命周期一次只提交一个 outgoing Current 与一个 incoming target，不实现组内多候选权重选举。relation 方向按两侧 Projection 中的 SyncRole 解析：

```text
incoming AlwaysLeader 或 outgoing AlwaysFollower
  -> incoming source -> outgoing target

outgoing AlwaysLeader、incoming AlwaysFollower，或双方 CanBeLeader
  -> outgoing source -> incoming target
```

两个 AlwaysLeader 或两个 AlwaysFollower 没有唯一合法方向，必须返回 typed invalid reason。incoming 成为 leader 时，runtime 删除 outgoing 上旧的上游 relation，再以本帧 raw sample 建立新的反向 relation；不得保留同一 playback 的双 source，也不得按 generation 假定 source 一定更老。求值继续按 relation 依赖递归拓扑排序并检测环。

这使 finite Start/End/Turn 可以使用 AlwaysLeader 保住自己的节奏：进入它时循环动画跟随它，离开它时后续循环动画也先跟随它。WalkLoop/RunLoop 使用 CanBeLeader，普通循环切换仍由当前可见 playback 领导。

### Timeline Marker Child Lane

Timeline Editor 为每个 AnimationTrack 绘制一个固定、可折叠的子轨：

```text
AnimationTrack clip row
  SYNC MARKERS child lane
  CURVES child lane
```

两个子轨都不加入 `TimelineData.Tracks`，不拥有独立 AuthoringId，不接受 Clip，也不执行 Tick。`SYNC MARKERS` 只投影父 AnimationTrack 的 SyncMode、SyncRole、Group、Topology 与 markers；`CURVES` 只投影 Animation Clip 自己拥有的连续控制曲线。折叠时 Track Handle 显示摘要，展开时使用与主时间轴相同的 frame geometry。Track 重排、滚动、选择与 Live Debug 都使用同一组合行布局，不能再把 marker 或曲线覆盖在 clip 上。

`None` 的 Marker 子轨显示禁用摘要。`MarkerGroup` 子轨按整数 Timeline frame显示点和短标签。Cyclic 序列额外显示末 Marker 指向下一周期首 Marker 的闭合提示；Finite 序列只显示首尾覆盖，不绘制回绕。Preview 游标所在 segment突出显示有向 pair 与 fraction。

### Relation 建立

target 第一次获得合法 sample 前，运行时读取该 Layer 在 selection commit 前的实际 Current：

1. 没有 Current：不建立 relation，target effective time等于 raw time。
2. source 或 target 为 `None`：不建立 relation。
3. Layer 或 CanonicalGroupId 不同：不建立 relation。
4. 二者同层同组：按两侧 SyncRole 解析 source/target，再建立唯一 relation。

这是明确的适用性判断，不是错误 fallback。声明同组但 Projection 数据损坏、segment 缺失或 sampling state 缺失则进入 Invalid。

### 首次 target occurrence 选择

source effective time定位到有向 marker pair和 fraction。target 可能有多个相同 pair occurrence：

- 先计算每个 occurrence 按同一 fraction 得到的 candidate time。
- `Finite` 使用 candidate 与 target raw local time 的绝对距离。
- `Cyclic` 使用模 duration 的最短时间距离，并保留与 raw cycle 最近的展开周期。
- 最小距离者胜出；距离相同按更小起始 frame、再按 MarkerAuthoringId 排序。
- relation 保存选中的 occurrence，后续帧不重新做最近候选跳转。

### 持续同步

只要 source 与 target 都仍是该 Layer 的共同可见 playback：

1. 先解析 source 当帧 effective time。
2. 定位 source 当帧 segment 和 fraction。
3. target relation 按 marker 越界顺序推进到对应 occurrence。
4. 每帧重新计算 target effective time。
5. 使用 effective time重新采样 target 整个 producer，包括所有重叠 clip、ClipIn、ease 和内部 weight。

这不是固定 offset。Walk 1.0 秒与 Run 0.6 秒在 fade 的每一帧都保持 marker phase 对齐，不会在第一次匹配后各自漂移。

### Relation chain

快速 `A -> B -> C` 时，B 可能仍跟随 outgoing A，而 C 又以当前 B 为 source。运行时按 relation 的 playback generation 建立无环依赖，并从最老 source 到最新 target求值：

```text
A effective -> B effective -> C effective
```

不得依赖 Dictionary 遍历顺序。若检测到环、同一 target 多 source 或跨 layer relation，立即 Invalid；不得选择任意一条。

### Detach 与继续推进

当 source 被 Animancer/lifecycle 正式退休：

- 记录 target 当帧最后 effective time与对应 raw time作为 continuation anchor。
- 删除 source -> target relation。
- 后续 target effective time = anchor effective time + target raw time自anchor后的delta。
- Cyclic target规范化 cycle/local time；Finite target不得越过自己的合法 duration。

这样 target 不会在 source 消失时跳回原始 Timeline time。target 若仍作为新 relation 的 source，其下游继续读取 target 的 effective time。

### SyncRole 边界

SyncRole 只决定已经被 Gameplay 选中的两个 playback 之间谁保持表现节奏，不选择状态、不比较逻辑 Priority，也不决定 Animancer weight。`CanBeLeader` 保留常规 outgoing 领导；`AlwaysLeader` 允许 Start、End、Turn 等有限动画保持自己的作者节奏；`AlwaysFollower` 用于明确声明只接受映射的 producer。若未来同层同时存在多个独立逻辑候选，再单独增加多候选 leader selection capability，不能扩张当前 pairwise 角色为第二套动画仲裁。

## Finite And Cyclic Semantics

支持组合：

```text
Cyclic -> Cyclic
Cyclic -> Finite
Finite -> Cyclic
Finite -> Finite
```

- Cyclic source的末 marker到首 marker参与segment查找。
- Finite source只在首末 marker覆盖内查找，不回绕。
- Cyclic target可选择最近展开周期并持续回绕。
- Finite target只能沿其有序 occurrence前进，不能跳回较早segment。
- relation期间 source推进到target有限序列无法继续表达的segment时为配置/资源错误，不静默断开同步。

作者若不能为 one-shot 提供完整覆盖，应把它配置为 `None`。这是明确业务选择，不是运行时猜测。

## Animancer Boundary

Animancer继续唯一拥有：

- state/ManualMixerState创建与复用。
- Layer、mask、blend mode。
- TransitionLibrary、FadeMode、source-to-target duration modifier与easing。
- 当前 state weight、outgoing fade与最终 Animator pose。

MarkerSyncRuntime只向正式 producer sampler提供 effective Timeline time。AnimancerPlaybackAdapter继续为 Timeline 控制的 child调用`DontSynchronize`，并按每帧 sample写入 state time和child weight。

不使用 Animancer自动同步的原因不是其能力不足，而是当前 Timeline 已经拥有 producer clip membership 与时间。启用第二个 normalized-time driver会在下一帧被 Timeline覆盖，或者造成两个时间权威。

## Timeline Editor

Timeline Editor在AnimationTrack上提供：

- SyncMode选择。
- MarkerGroup时的SyncGroupId与Finite/Cyclic topology。
- marker列表的添加、重命名、frame修改和删除。
- track内marker竖线、短标签、选中态和整数frame拖动。
- 同组producer摘要、directed pair coverage和call site拓扑错误。
- 空白 Marker Lane 的右键 `Add Sync Marker`。
- Marker 点的右键选择、定位、重命名和删除。
- `SYNC MARKERS` 与 `CURVES` 独立折叠；折叠不改变任何作者数据。

### 视口布局与手势所有权

Timeline使用一个纵向轨道视口。左侧Track Handle与右侧Track内容分别滚动，但必须共享同一个纵向offset；左侧工具栏和右侧时间标尺固定在视口顶部。轨道内容按从上到下的正常布局参与滚动，不使用反向Flex和基于`worldBound`的工具栏位置补偿。展开Marker与Curve后，顶部Track仍可通过纵向滚动重新到达，左右组合行不得错位。

参考UE Sequencer与Curve Editor的输入职责划分，并针对“Curve内联在Timeline”这一差异采用以下唯一手势：

```text
Wheel             -> 纵向浏览Track
Shift + Wheel     -> Timeline横向平移
Ctrl + Wheel      -> Timeline横向缩放
Middle Drag       -> Timeline横向平移
Alt + Wheel       -> 当前unbounded Curve纵向缩放
Alt + Shift+Wheel -> 当前unbounded Curve纵向平移
Left Drag         -> Clip、Marker或Curve Key的直接编辑
Right Click       -> 当前内容的上下文菜单
```

内联Curve Lane不得占用Middle Drag，否则同一屏幕位置同时属于Curve与Timeline时会出现两个手势权威。普通Wheel也不得被Curve或Timeline横向缩放抢占。Bounded Curve不提供纵向缩放，因为其值域已经由descriptor固定。

Point Marker使用比绘制线更宽的透明命中区域，指针位置必须直接换算到父Track的Timeline坐标，不能通过Marker自身已经移动过的局部layout反推。`SyncMode=None`时，Marker Lane只选择Track并把正式配置显示到Inspector；新增菜单必须明确要求先配置MarkerGroup，不得调用必然失败的`AddMarker`。

### 同组 Marker 名称候选

Editor 从当前 Character Definition 的正式 Marker Sync authoring context建立只读名称候选：

```text
LayerId + CanonicalSyncGroupId
  -> 该组全部合法 AnimationTrack 上出现过的 MarkerId 去重集合
```

右键新增 Marker 时优先显示该集合，作者仍可显式输入新的合法 MarkerId。首个组成员没有候选时只能显式输入新名称。候选索引不序列化、不进入 AnimationTrack、Projection、Program或Snapshot，只是已有 Track 数据的 Editor 投影；保存后唯一真相仍是每个 Track 上的 Marker 点。

不增加全局 Marker catalog 的原因是：名称只有处于同一 Layer、同一 Sync Group时才有兼容意义。独立资产会要求维护引用和删除生命周期，并形成“catalog声明了名称但Track没有实际点”的第二真相。

### Marker 拖动事务

Marker 拖动使用与连续曲线 key 相同的交互生命周期，但调用 Point Marker 自己的正式 authoring API：

1. Pointer Down记录原始frame并开始本地草稿，不修改资产。
2. Pointer Move只更新Marker View、frame标签和本地pair预览。
3. Pointer Up或意外失去pointer capture时，把最后草稿frame通过一次正式mutation提交为一个Undo事务。
4. Pointer Cancel丢弃草稿并恢复原始位置。
5. 提交成功后统一刷新Timeline布局、Inspector、唯一校验摘要和Authoring Preview。

拖动期间不能每帧调用`ApplyModify`，否则一次手势会产生多个Undo记录、反复重建Projection并使Marker在重绘时断开。刷新也不能依赖作者重新选择Track。

所有操作必须通过Timeline正式authoring API进入Undo、dirty、identity与RebindTimeline。不得用SerializedProperty/YAML任意写入。

Marker Sync不显示步态相位曲线，也不读取`Foot Placement Weight`。`CURVES`分组中的单一Foot Placement曲线只表达最终姿势后处理允许介入多少；未来Distance Matching若实施，使用独立的距离曲线与独立runtime capability，不能借用Marker pair或Foot Placement曲线。

### Curve Group与Channel Lane

每个拥有registered curve channel的Track显示一个可折叠`CURVES`分组。展开后每个ChannelId占一条稳定lane：

```text
CURVES
  Weight
  Ease In
  Ease Out
  Foot Placement Weight
```

左侧Track Handle显示颜色swatch、名称、单位和值域摘要；右侧每个Clip只在自己的StartFrame..EndFrame范围内绘制该channel曲线。两个重叠Clip各自保留边界、背景和key，不能在作者层预混成一条曲线。Timeline滚动、缩放和Track重排必须让Clip行、Marker行与全部Curve行保持一个组合布局。

作者可以逐channel隐藏或显示；`CURVES`整体折叠只改变布局。显示状态属于Editor session，不写入Timeline资产、Program或Projection。

### Curve绘制

- 使用完整AnimationCurve与`Evaluate`结果绘制Hermite/weighted插值，不用key之间的直线假装最终曲线。
- 显示Clip起止边界、原始key、选中key、切线handle、当前游标采样点和值。
- Bounded channel使用固定min/max参考线；Foot Placement和Weight显示`0/0.5/1`。
- Unbounded channel根据当前可见Clip和key执行vertical fit，并显示单位与零线；作者可以垂直缩放和平移，不改变key值。
- 横轴始终使用Timeline frame geometry；descriptor负责Timeline frame与curve local time互换。
- 曲线采样数量按可见像素有界，不在重绘中分配无界数组、反射字段或扫描全部Timeline。

### Curve直接编辑

Curve Lane必须支持：

- 点击选择单key，Shift追加选择，框选多个key。
- 双击空白处或右键`Add Key`，以当前位置对应time/value创建key。
- 拖动一个或多个key的time/value；time按Timeline整数帧吸附，value按typed domain约束。
- Delete或右键删除；需要满足领域最小key数量时由validator拒绝，不由UI静默保留。
- Ctrl+C/Ctrl+V复制粘贴完整key字段；只允许粘贴到兼容TimeDomain与ValueDomain的channel，不做单位转换fallback。
- Inspector精确编辑Timeline frame、normalized time、value、in/out tangent、in/out weight和WeightedMode。
- Context Menu设置Auto、Clamped Auto、Linear、Constant、Free与Weighted tangent；操作必须无损保留未修改侧的字段。
- `Frame Selected`将当前channel的vertical view适配到选中key，不改变Timeline主横轴。

所有操作都修改本地完整curve草稿。一次手势或一次Inspector提交只通过descriptor MutationAdapter产生一个Undo事务；提交后重新读取owner curve并刷新Timeline、Inspector、validation与可用的Authoring Preview。Editor不得调用Unity未公开内部CurveEditor API形成版本依赖，也不得直接写SerializedProperty。

### Curve运行边界

通用Curve Editor只负责作者视图。提交后仍沿各领域既有链路：

```text
Animation control curve -> CharacterPresentationProjection -> Presentation consumer
MotionCurve              -> Semantic IR -> Numeric Program -> Motion evaluator
MotionWarp progress      -> Semantic IR -> Numeric Program -> MotionWarp modifier
Camera control curve     -> 对应Camera Timeline compile/presentation consumer
```

不存在`GenericTimelineCurveRuntime`。Catalog不在Player中按ChannelId选择业务执行器，也不让Curve Editor直接预览Gameplay Motion、MotionWarp或Camera状态。纯动画Authoring Preview只能消费已有Projection能力；其它curve的运行结果继续通过正式Session和Live Debug观察。

不创建：

- FootPhase SO或registry。
- Animation Sync Profile。
- 第三个动画配置窗口。
- Graph页签中的Timeline副本。

## Preview And Live Debug

以 `refactor-timeline-authoring-preview-to-presentation-only` 的最终边界为准：

- Authoring Preview只使用Projection、CharacterAnimationPlaybackRuntime、lifecycle与Animancer。
- 单producer预览显示当前raw/effective time、marker segment和fraction。
- 作者可显式选择同一Projection、同Layer、同Group的source producer进行纯表现handoff比较；Preview只生成现有preview command，不创建Simulation Session。
- Preview relation必须复用正式MarkerSyncRuntime，不能直接写Animancer normalized time或维护第二份offset cache。
- Live Debug只读显示真实runtime relation、source/target playback、segment、fraction、candidate occurrence、effective time、chain depth与detach reason。

TreeClip、Action、MotionCurve、MotionWarp和WorldSolver不在Authoring Preview执行；它们继续通过正式运行Session和Live Debug观察。

## Agent v14

当前v13已经支持Marker Sync与Foot Placement专用curve operation。本change将唯一Agent schema原子提升为`agent-character-controller-synthesis.v14`。Snapshot继续为每个AnimationTrack输出Marker事实，并为每个可编辑Curve owner输出：

```text
syncMode
syncGroupId
sequenceTopology
syncRole
syncMarkers[]
  authoringId
  markerId
  frame

curveChannels[]
  channelId
  ownerAuthoringId
  timeDomain
  valueDomain
  unit
  preWrapMode
  postWrapMode
  keys[]
    time / value
    inTangent / outTangent
    inWeight / outWeight
    weightedMode
```

Patch增加typed operation：

```text
configure_animation_track_marker_sync
ensure_animation_sync_marker
move_animation_sync_marker
delete_animation_sync_marker
configure_timeline_curve_channel
```

规则：

- marker target只接受Timeline/Track/Marker stable identity或前序operation output。
- curve target只接受owner stable identity、Catalog登记的ChannelId与完整curve payload，不接受字段名或key index。
- lowerer一次生成immutable command plan。
- dry-run和apply消费同一plan。
- marker handler只调用AnimationTrack正式authoring API。
- curve handler只调用对应descriptor MutationAdapter与owner正式authoring API。
- validator分别复用Marker Sync与curve owner领域的唯一校验服务。
- MCP bridge只转发Snapshot/Patch/Validation。
- v13及更早reader、Foot Placement专用curve operation、operation alias、converter和兼容错误提示全部删除。
- Timeline UI 与 Agent handler调用同一AnimationTrack正式authoring API；Editor不得维护Marker专用SerializedProperty写入。

## Corin Migration

Corin迁移按真实动画资源逐track完成：

1. 全部可达AnimationTrack消除`Unspecified`。
2. WalkLoop与RunLoop配置为`MarkerGroup/Cyclic/CanBeLeader`，共享`Locomotion.Gait`，至少覆盖左右支撑的两个有向segment。
3. RunStart、RunEnd、MovingTurn等one-shot只有在真实clip可提供frame 0到duration的完整同组segment覆盖时才配置为`MarkerGroup/Finite`；否则明确`None`并记录资源缺口。
4. Attack1..5与Dodge不会因为名称自动加入Locomotion组。连段仍由ComboAccept/Recovery窗口和State transition决定。
5. 某组Attack变体若业务确实要求共同姿态同步，可建立独立Action Marker Group；不得借此替代combo窗口或动作时序。
6. 没有AnimationTrack的WalkEnd等状态不创建伪Timeline、伪clip或伪marker。

资产必须通过正式Agent v14事务迁移：

```text
export_snapshot
  -> dry_run_patch
  -> apply_patch
  -> export_snapshot
  -> validate
  -> compile Semantic IR / Float32 / Fixed wrapper / Projection
```

不得直接修改managed-reference YAML，也不创建一次性migrator。

## Diagnostics

每个relation snapshot包含：

```text
LayerId
SourcePlaybackId
TargetPlaybackId
CanonicalSyncGroupId
SourcePreviousMarkerId
SourceNextMarkerId
SourceSegmentFraction
TargetOccurrenceIndex
TargetEffectiveTime
TargetEffectiveCycle
RelationDepth
LifecyclePhase
ApplicabilityOrFailureReason
```

稳定reason至少区分：

- `NoCurrentSource`
- `SourceExplicitNone`
- `TargetExplicitNone`
- `DifferentLayer`
- `DifferentGroup`
- `RelationCreated`
- `RelationContinued`
- `SourceRetiredRebased`
- `InvalidProjection`
- `MissingSegmentPair`
- `FiniteCoverageExceeded`
- `RelationCycle`

Diagnostics不得重新采样Timeline、读取脚骨Transform或推导StateMachine transition。

## Performance

- Projection构建时完成marker排序、pair set校验与occurrence索引。
- runtime按demanded playback数量维护预分配raw/effective sample buffer。
- 每个relation只保存索引与时间anchor，不复制marker数组。
- PresentationFrame不使用LINQ、反射、资产路径、字符串扫描或完整Projection遍历。
- group/segment查找使用Projection内预构建稳定索引。
- producer采样继续复用现有clip sample buffer与Animancer state。

## Tradeoffs

### Track所有权，而不是TimelineNode覆盖

收益：与现有producer identity、Projection和网络command完全一致，不污染Gameplay ABI。

代价：shared Timeline共享同步语义；若同一内容确实需要Once与Loop两种拓扑，作者必须拆成不同producer。

### 持续relation，而不是一次性offset

收益：不同动画速度在fade期间不会重新漂移，连续切换读取当前effective phase。

代价：每个表现帧多一次小型segment映射，并需要严格管理relation chain和detach。

### 显式SyncRole，而不是按状态名猜方向

收益：有限 Start、End、Turn 可以保持作者节奏，循环切换仍有稳定默认；运行时不读取状态名、逻辑 Priority 或 Animancer weight。

代价：每个 MarkerGroup producer 必须多配置一个角色；两个 AlwaysLeader 或两个 AlwaysFollower 相遇会明确失败，作者必须修正组内语义。

### 支持Finite与Cyclic，而不是只做Walk/Run

收益：Start、End、Turn、Dodge或特定Attack变体能复用同一能力，不再写状态专用matcher。

代价：Finite必须覆盖完整时间并满足group directed pair契约；资源不具备时只能显式None。

### 命名marker，而不是统一normalized time

收益：可以表达左右支撑时长不对称，Walk和Run不必在相同normalized位置落脚。

代价：作者必须维护marker语义与组兼容性，Editor和Agent校验更严格。

### Marker Sync不向Foot Placement提供contact

收益：动画时间选择与最终pose地面约束各自单一负责，网络与Gameplay不受影响。

代价：marker对齐不能独立解决坡面脚滑、骨盆高度或移动平台脚锁，这些仍由Foot Placement处理。

### 三类作者内容共享交互，而不是共享数据模型

收益：Clip、Marker和Curve的时间坐标、选择、拖动事务与刷新行为一致，后续增加新的Point Marker或Continuous Curve不必复制输入生命周期。

代价：仍需保留各领域自己的数据和校验API；不能用一个任意字段Timeline item快速承载所有内容。这是为了避免Editor便利反向污染Runtime与序列化边界。

### 同组名称动态投影，而不是独立Marker Catalog

收益：作者能像使用共享Marker词表一样直接复用名称，但仓库只有Track上的实际Marker点这一份真相。

代价：候选集合需要Definition authoring context；单独打开一个没有Definition上下文的Timeline时只能显示该Timeline已经使用的名称并允许显式创建新名称，不能假装知道其它producer。

### 直接点编辑，而不是只依赖Inspector列表

收益：作者可以在看到动画区间、循环边界和相邻Marker的同时完成新增、拖动和删除，步调关系可直接读懂。

代价：Timeline Editor必须完整处理pointer capture、context menu、局部预览和单次Undo，交互实现比只画竖线更严格。

### 显式Curve Channel Catalog，而不是反射AnimationCurve字段

收益：每条曲线的owner、时间域、值域、单位、mutation与runtime消费者都可审查；新增业务曲线不会因为字段名相同被错误接入。

代价：每个新curve channel需要一条正式descriptor注册和领域mutation API，不能仅添加一个`AnimationCurve`字段就自动出现在UI中。

### 通用Curve Editor，而不是通用Curve Runtime

收益：Animation、Motion、MotionWarp和Camera共享高质量作者交互，同时继续沿各自Compiler和Runtime链执行，不改变Gameplay与Presentation所有权。

代价：Editor需要通过typed adapter处理不同owner和值域，不能把所有曲线降低成同一个无业务含义的float列表。

### 完整曲线原子替换，而不是Key稳定identity

收益：符合Unity AnimationCurve数据结构，Snapshot/Patch不会因key重排或切线编辑寻址错误，也不增加大量一次性Key GUID。

代价：Agent修改一个key时也必须提交完整channel，并通过source revision/preflight拒绝陈旧curve；Editor中的key选择只在当前打开revision内有效。

### 同一Timeline内联Curve Lane，而不是独立Curve窗口

收益：作者能同时看到Clip边界、Marker、Curve和主时间游标，不需要在第三个窗口比对时间。

代价：Track组合行高度和折叠状态更复杂，必须提供逐组折叠、channel显示过滤和有界绘制，避免Timeline变成无法扫描的长面板。

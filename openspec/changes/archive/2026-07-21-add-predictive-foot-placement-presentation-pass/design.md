# Design: 预测式足部放置表现 Pass

## Context

当前每个表现帧的唯一协调入口是：

```text
CharacterSimulationPresentationRuntime.Present
  -> CharacterBodyPresentationRuntime.Present
       -> 写入唯一 VisualRoot
       -> 输出 CharacterBodyPresentationFrame
  -> CharacterAnimationPlaybackRuntime.Present
       -> Timeline visual sample
       -> AnimationPlaybackLifecycle
       -> AnimancerPlaybackAdapter.Evaluate
  -> CharacterCameraPresentationRuntime.Present
```

`CharacterBodyPresentationFrame` 已包含 visible position/rotation/velocity、Grounded、previous/current tick、sample alpha、correction状态以及 `ResetSequence/ResetReason`。这已经足够让足部表现使用同一可见轨迹，并在分支替换或selected stream reset时丢弃旧世界锚点。

Final IK 已导入在 `Assets/Plugins/RootMotion`。插件的 `Grounding` 包含脚速、简单速度预测、Ray/Sphere/Capsule cast、脚旋转和骨盆阻尼；`SolverManager` 与 `IKExecutionOrder` 也允许外部安排solver顺序。但是当前插件没有安装命名asmdef，全部runtime源码编译进`Assembly-CSharp-firstpass`，而正式角色代码位于`ThirdPersonClient.Runtime`。直接引用当前程序集或让Grounder自行LateUpdate都会绕过项目的模块和帧边界。

## Public Reference Baseline

本设计只采用有公开技术说明的原则：

- Ubisoft GDC 2016《Fitting the World: A Biomechanical Approach to Foot IK》：预测优先、保留原动画、独立双脚、Locked/Sliding/Unlocked、支撑腿骨盆、ascent/descent差异、足部路径和ground envelope。
  - https://gdcvault.com/play/1023009/Fitting-the-World-A-Biomechanical
  - https://media.gdcvault.com/gdc2016/Presentations/Roche_Clifford_Fitting%20the%20World.pdf
- Naughty Dog GDC 2021《Motion Matching in The Last of Us Part II》：动画后处理持续观察脚踝速度，低速时建立Foot Plant并锁定世界位置。
  - https://media.gdcvault.com/GDC%2B2021/Motion_Matching_In_TLOU2.pdf
- Unreal Engine Foot Placement：Plant、Trace、Pelvis、Interpolation、replant、腿伸展与球形查询采用独立参数组。
  - https://dev.epicgames.com/documentation/unreal-engine/API/Plugins/AnimationWarpingRuntime/FAnimNode_FootPlacement
  - https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/FootPlacementPlantSettings?application_version=5.6
  - https://dev.epicgames.com/documentation/en-us/unreal-engine/python-api/class/FootPlacementTraceSettings?application_version=5.4
- Unreal Engine动画作者边界：Animation Sequence时间轴直接创建和编辑Animation Curves；持续窗口由Notify State表达；Motion Warping窗口位于Montage时间轴；IK Rig和solver链仍由IK Rig/AnimGraph配置。
  - https://dev.epicgames.com/documentation/en-us/unreal-engine/animation-curves-in-unreal-engine
  - https://dev.epicgames.com/documentation/unreal-engine/animation-notifies-in-unreal-engine
  - https://dev.epicgames.com/documentation/unreal-engine/motion-warping-in-unreal-engine
  - https://dev.epicgames.com/documentation/en-us/unreal-engine/ik-rig-in-animation-blueprints-in-unreal-engine
- Final IK：只作为已经导入的骨骼solver和数学实现，不把其默认组件生命周期当作项目架构。

《绝区零》只作为视觉验收参考。没有公开资料能够证明其具体算法、solver或动画资产组织，本设计不声称复现其内部实现。

## Goals

- 在现有唯一PresentationFrame中加入可审查、可配置、可重置的Foot Placement Pass。
- 对规则楼梯、斜坡、台阶边缘和移动平台提供稳定双脚落点与骨盆响应。
- 保留Corin动画原本的水平脚步、攻击轮廓和Animancer crossfade，只在需要时修正接触、高度、脚掌方向和骨盆。
- LocalOwner、完整模拟Actor与网络ObservedActor复用同一实现。
- 将项目算法与Final IK solver分离，未来替换solver不修改contact、prediction、constraint和pelvis逻辑。
- 角色级算法参数显式来自正式Profile和rig binding，动画相对影响显式来自Timeline Animation Clip曲线，缺失配置直接失败。
- Presentation hot path使用预分配workspace和NonAlloc查询，不为每脚每帧创建容器。

## Non-Goals

- 不让Foot Placement成为Grounded、碰撞、台阶或位移的Gameplay权威。
- 不把Foot Placement状态写入Program State、World State、Snapshot、Hash或网络包。
- 不使用Motion Matching、Stride Warping或完整全身程序化locomotion。
- 不实现任意数量、任意priority的通用Pass插件图。
- 不提供按Network Model、角色名称、Action名称或clip名称的运行时分支。
- 不让Final IK组件自行Update/LateUpdate。

## Decision 1: 增加固定Pose Post Process插槽，不增加自主MonoBehaviour时钟

唯一帧顺序修改为：

```text
Body.Present
  -> Animation.Present
       -> Animancer.Evaluate
  -> PosePostProcess.Present
       -> FootPlacement Planner
       -> FootPlacement Solver
  -> Camera.Present
  -> acknowledge/clear frame signals
```

Core合同为：

```text
ICharacterPosePostProcessPass
  Present(CharacterPosePostProcessFrame frame)
  Reset(CharacterPosePostProcessReset reset)
  Dispose()
```

`CharacterPosePostProcessFrame`只包含同帧Body frame、presentation delta、只读visible animation contribution和明确的reset identity。第一版Factory只装配一个`CharacterFootPlacementRuntime`，不增加priority、动态排序或运行时注册表。固定插槽用于明确所有权，不是新的通用节点系统。

### Tradeoff

- 收益：Animancer、Foot Placement和Camera共用一个frame delta与销毁顺序；不会出现Final IK先算一次、管线后算一次。
- 代价：以后增加Aim/Hand/Recoil时必须重新明确固定pose pass顺序，不能靠任意priority热插拔。这比引入另一套表现仲裁更可审查。

## Decision 2: Foot Placement读取最终动画姿势，不拥有动画或Body时钟

每帧输入固定为：

```text
CharacterBodyPresentationFrame
  VisiblePosition / VisibleRotation / VisibleVelocity
  TargetGrounded
  ResetSequence / ResetReason

AnimationPoseFrame
  PoseSourceLayerId
  ProducerId / PlaybackGeneration
  VisualSampleTime / NormalizedTime / Cycle
  AnimancerVisualWeight

CharacterFootPlacementRig
  VisualRoot / Pelvis
  Left Hip / Knee / Ankle / Toe
  Right Hip / Knee / Ankle / Toe
  Sole offsets

CharacterFootPlacementProfile
Unity PhysicsScene query context
```

脚骨骼位置必须在`Animancer.Evaluate`后读取。上一个表现帧的脚速历史保存在Foot Placement Runtime；本帧Animancer姿势覆盖上一帧IK结果后再采样，因此速度描述动画姿势变化，不累计solver自身的偏移。

Foot Placement不修改`AnimationSampleTick/Alpha`、producer lifecycle、Animancer state time或Body presentation cursor。

### Tradeoff

- 收益：IK永远作用于当前真正可见的crossfade姿势，不需要复制Timeline sampler或猜当前State。
- 代价：Animator必须持续更新完整骨骼。Corin rig必须显式配置可用的culling/update mode；不能在骨骼未被动画写入时静默沿用旧姿势。

## Decision 3: 接触事实来自生成动画特征、世界接触速度与表面距离，不复制Gait Phase

Editor Analyzer使用与Runtime相同的Rig Calibration和Sampling Rig。它先从校准绑定姿势得到唯一脚底地面参考高度，再分别采样heel/toe；最低接触点生成高度，heel/toe中点轨迹生成速度和下一落地offset。Plant速度只使用脚底垂直速度，不能把InPlace动画的局部水平移动误判为离地。旧算法artifact通过format、algorithm identity和完整输入hash自然Stale，不保留旧reader。

Runtime对每个visible producer使用Marker Sync后的连续视觉时间推进生成特征。动画局部脚速必须乘本帧有效视觉时间倍率；暂停或重定位首帧倍率为零，持续rebase只在进入时重新锚定，之后继续按有效时间差推进。随后构造世界接触点速度：

```text
GeneratedSoleLocalVelocity * VisualTimeScale
+ BodyVisibleLinearVelocity
+ cross(BodyVisibleYawAngularVelocity, SolePosition - VisualRootPosition)
= SoleWorldVelocity
SoleDistanceToSupport
Descending
BodyGrounded
```

接触进入条件至少要求：

- Body frame为Grounded。
- producer视觉Foot Placement权重大于显式阈值。
- 脚底在plant distance内。
- 世界接触点平面速度和垂直速度处于plant阈值。
- 脚处于下降或稳定阶段。

释放使用另一组更宽阈值，形成迟滞，避免边界抖动。Contact classifier不会读取BTSMTL State、Action、Timeline Window、Blackboard、GameplayTag或`add-timeline-animation-marker-sync`定义的marker/effective phase。

### Tradeoff

- 收益：CrossFade、播放倍率、Marker Sync、InPlace动画和Body世界运动被放在各自正确的来源中，Contact不再依赖最终混合姿势的伪速度。
- 代价：AnimationClip或Calibration变化必须重建Editor-only artifact和Presentation Projection；Player不允许即时分析。

## Decision 4: 动画相对权重属于Timeline Animation Clip，全局算法参数属于Foot Placement Profile

采用Unreal成熟编辑职责：与动画时间对齐的连续值由Animation Sequence/Montage时间轴曲线编辑，区间事件才使用Notify State，Rig和程序化求解参数留在IK Rig/AnimGraph一侧。本项目不复制UE资产类型，而复用现有Timeline Animation Clip与Projection链路。

`CharacterFootPlacementProfile`由Unity角色表现装配显式引用，不由`CharacterPipelineDefinition`引用，只保存角色级算法参数：

```text
TraceSettings
ContactSettings
PredictionSettings
ConstraintSettings
PelvisSettings
FootRotationSettings
SmoothingSettings
PoseSourceLayerId
```

每个Timeline `AnimationClip`使用自身stable clip identity只保存一条归一化总权重曲线：

```text
FootPlacementWeightCurve
```

曲线必须至少包含key，时间和值都位于`[0,1]`且顺序合法。它只回答“这个动画时刻Foot Placement整体介入多少”。Prediction如何预测、Pelvis如何调整、Foot Rotation如何贴合坡面继续由`CharacterFootPlacementProfile`和同一planner负责。旧Timeline资产经正式Agent Snapshot审计后，仅在四条旧曲线逐项完全一致时保留原Placement形状并通过正式Agent事务重写；不保留缺曲线返回1或读取旧字段的兼容逻辑。零值是作者显式关闭Foot Placement，不是runtime fallback。

Timeline Editor参考Unreal Animation Sequence Editor与Curve Editor的信息层级，在每个AnimationTrack中依次显示Clip行、Marker Sync行和`CURVES`分组标题。分组默认折叠，展开后只显示`Foot Placement Weight`曲线行。主时间轴按Clip的`StartFrame..EndFrame`显示`0/0.5/1`参考线、插值曲线和原始key；重叠Clip分别显示自身曲线，不预先混成作者数据。点击曲线段选择唯一Animation Clip，拖动key直接编辑时间和值，双击增加key，右键删除非唯一key，全部复用Timeline唯一Undo、dirty和Projection重建路径。曲线行没有AuthoringId、不进入`TimelineData.Tracks`、不接受Clip、不会执行Tick，也不保存第二份采样结果。

Curves分组默认折叠，作者需要核对Attack、Dodge与Recovery恢复区间时再展开。展开后的曲线行必须具有足够高度区分平直段、过渡段和key。折叠状态只属于Timeline Editor会话，不写入Timeline资产或Projection。组合行高度由单一布局度量计算，Track View、Track Handle、滚动范围和拖动重排必须同步；普通Track保持原有高度。

Projection复制单一曲线，并让同一animation binding按visual sample time复用正式clip range、Weight与Ease采样。Foot Placement Runtime先在每个visible producer内部按clip实际weight混合总权重，再按Animancer adapter输出的实际state/layer weight完成crossfade混合；同一结果统一调制Placement、Prediction、Pelvis与Rotation算法，不引入逻辑priority、新winner或第二套Timeline采样公式。

作者Weight只在最终求解权重中应用一次。摆脚clearance、support target和rotation target先计算完整几何目标；接触constraint solve weight、自由脚clearance weight和Pelvis再消费同一个作者Weight，禁止在clearance、target rotation、IK weight和Pelvis中重复相乘。

曲线进入Presentation Projection和其source revision，因为这是正式的动画表现编译数据；它们不进入Semantic IR或Gameplay Program。Profile算法参数仍不进入Definition、Program或Projection，调整地面查询和solver手感不触发任何authoring编译。

### Tradeoff

- 收益：作者只调一条“整体介入量”，不会在每个Clip重复维护IK算法内部四个子步骤；clip混合、producer crossfade和重入仍沿正式动画链自然插值。
- 代价：不能逐动画单独关闭Prediction而保留Pelvis；这种特殊差异必须通过确有业务含义的新算法参数提出，而不是提前暴露四份常年相同的曲线。修改曲线仍会重建Presentation Projection。

## Decision 5: 预测落脚点保留动画水平轨迹

预测不直接把脚拉向角色前方。每帧先按每个visible producer的有效视觉时间采样生成的下一落地delay/offset，再计算有限预测：

```text
PredictionTime = clamp(generated landing delay / visual time scale, MinLookAhead, MaxLookAhead)
PredictedRootPose = extrapolate visible body pose by visible linear/yaw velocity
PredictedFootLocal = generated landing local offset
PredictedFootWorld = PredictedRootPose * PredictedFootLocal
```

如果脚已Locked，预测只用于判断前方支撑和replant，不移动当前锁点。脚处于Free时，预测位置只确定未来support与摆腿clearance；最终水平轨迹仍来自动画，直到进入Locked/Sliding约束。

Body frame需要补齐只读visible/target yaw velocity，以便预测根朝向；该字段仍属于Presentation，不进入网络或Simulation。

### Tradeoff

- 收益：保留Corin步幅、左右摆动和角色风格，避免IK把所有步子变成相同直线。
- 代价：极端急转时线性look-ahead仍可能不准。Profile的最大预测时间与replant规则限制误差，第一版不增加未来动作规划器。

## Decision 6: 使用路径采样形成有限Support Envelope

每只脚拥有固定容量query workspace。每帧分别针对当前heel与toe执行NonAlloc Ray/Sphere查询，保留两个独立Current Support；再针对当前sole、路径中点和predicted footprint执行NonAlloc Ray/Sphere/Capsule查询，并按路径fraction稳定排序候选。

候选过滤顺序固定为：

```text
LayerMask
-> 排除Character自身Collider
-> 有限命中值
-> 最大可站立坡度
-> 最大step up/down
-> hip到foot可达范围
-> 相邻候选高度连续性
-> surface identity稳定排序
```

Current heel/toe同时合法时，以两接触点中点和同时通过两点的有限法向构造唯一virtual support plane，并按高度与稳定identity显式选择移动surface owner；只有一侧合法时，将该侧support plane投影到脚底中心。路径候选只形成从当前foot到predicted footprint的分段support envelope，不得再次覆盖Current Support。目标脚底不得低于该envelope；前方envelope抬升时，Free脚增加有限swing clearance。

第一版不构建Ubisoft完整三维凸包，但必须同时采样当前、路径和预测端点，不能退化为单Ray。

### Tradeoff

- 收益：规则楼梯边缘和小缝隙不会让脚在相邻帧跳向完全不同高度；不需要场景额外提供隐藏ramp collider。
- 代价：复杂屋顶、碎石和多层交叠表面的连续性不如完整convex ground envelope。该能力在真实业务需要时另开change扩展同一query owner。

## Decision 7: 每只脚只有Free、Locked、Sliding三态

```text
Free
  动画拥有脚；可应用有限swing clearance与落脚orientation预对齐。

Locked
  世界/移动surface锚点拥有目标；动画只通过IK weight影响接管强度。

Sliding
  目标在同一support surface上受限移动，用于保留动画轮廓并避免硬锁扭腿。
```

正式转换：

```text
Free -> Locked
  Contact classifier满足plant条件且support可达

Locked -> Sliding
  动画目标离开lock但仍在slide/reach限制内

Sliding -> Locked
  相对速度回落且目标重新稳定

Locked|Sliding -> Free
  Body airborne、policy释放、surface失效、超出replant阈值、腿不可达或reset
```

Plant/release使用连续constraint solve weight和half-life，不增加Planting/Releasing伪状态。超过Replant阈值时先进入Free并把旧constraint weight连续释放到零；只有释放完成后的后续表现帧才能提交新Current Support。自由脚clearance可以继续求解，但不得反向维持旧constraint weight。状态变化保存结构化reason用于diagnostics。

### Tradeoff

- 收益：状态数量有限，能解释脚为何移动，又允许轻微滑动保留动作轮廓。
- 代价：不表达复杂脚趾roll或多接触点phase；第一版以ankle/sole目标为单位。

## Decision 8: Moving Surface锚点保存在Presentation本地

Locked脚保存：

```text
Collider reference
Surface Transform
Local Position
Local Normal
Stable runtime instance identity
```

每帧从surface transform重建世界锚点。Collider销毁、禁用、layer失配或局部点不再通过reach/slope检查时，脚以明确`SurfaceInvalid`原因释放。该引用只存在于Presentation runtime，不进入Snapshot、Hash、Program或Network。

### Tradeoff

- 收益：移动平台上脚不会留在旧世界坐标；无需Gameplay为纯视觉脚锁同步平台局部点。
- 代价：不同客户端的动态场景视觉可能略有差异，但不会改变权威角色Body或命中结果。

## Decision 9: 骨盆由支撑腿可达区间决定

Planner根据动画pelvis位置、两侧hip、计划foot目标和leg length计算每条腿允许的pelvis垂直区间。先求两个区间的交集，再选择最接近动画pelvis且满足主要支撑腿的目标：

- 上楼时优先避免高处支撑腿过伸，并允许有限抬高。
- 下楼时优先避免低处支撑腿悬空，并允许有限降低。
- 双脚权重相近时按plant weight与vertical load连续混合，不在左右腿间瞬切。
- 区间无交集时按Profile上限夹紧，标记不可达侧并触发该脚replant/release。

骨盆offset使用presentation delta和显式half-life推进临界阻尼，分别限制最大上移、最大下移和每秒变化。第一版只输出pelvis组件空间竖直偏移，不旋转pelvis、spine或VisualRoot。

### Tradeoff

- 收益：楼梯上身体不会跟两脚目标逐帧跳动，也不会由FBBIK意外改变上半身武器姿势。
- 代价：极端高差下不做全身脊柱补偿，效果上限低于完整FBBIK；该取舍优先保证当前动作游戏攻击轮廓。

## Decision 10: Project planner与Final IK solver严格分离

Core输出：

```text
CharacterFootPlacementPlan
  LeftFoot(TargetPosition, TargetRotation, Weight, ConstraintState)
  RightFoot(TargetPosition, TargetRotation, Weight, ConstraintState)
  PelvisComponentVerticalOffset
  ResetSequence
```

Core只依赖：

```text
ICharacterFootPlacementSolver
  RigSnapshot CaptureAnimatedRig()
  Apply(CharacterFootPlacementPlan plan)
  Reset()
  Dispose()
```

`PelvisComponentVerticalOffset`是沿VisualRoot组件空间up轴的标量，不是pelvis父骨空间local Y。Solver adapter应用计划时必须先把`VisualRoot.up * offset`转换到pelvis父骨空间，再叠加到本帧动画pelvis local position。Corin的`Bip001`父骨带有固定预旋转，直接叠加`Vector3.up * offset`会把大部分竖直补偿错误地转成横向位移。

Final IK adapter使用两个显式`LimbIK`链：hip、knee、ankle。Toe只用于sole和方向采样，不作为第三个solver链点。Adapter在Animancer Evaluate后：

1. 将planner给出的pelvis组件空间竖直offset转换到父骨空间并应用。
2. 设置左右Limb solver的IK position、rotation和weight。
3. 按固定Left/Right顺序显式更新solver。
4. 发布最终solver snapshot。

两个LimbIK组件禁止自主Update/LateUpdate；adapter直接初始化和更新底层solver。项目不使用`GrounderBipedIK`、`GrounderFBBIK`或`GrounderIK`作为运行owner，也不修改Final IK源码。

### Tradeoff

- 收益：contact、prediction、surface、constraint和pelvis都是项目可审查代码；Final IK只替代复杂可靠的腿部数学。以后换Unity Animation Rigging只需新增solver adapter。
- 代价：无法直接享受Final IK Grounder Inspector的一键参数；这些参数必须迁入正式CharacterFootPlacementProfile，避免双主源。

## Decision 11: Final IK位于独立adapter程序集

安装插件自带unitypackage中的：

```text
Assets/Plugins/RootMotion/RootMotion.Runtime.asmdef -> RootMotion
Assets/Plugins/RootMotion/Editor/RootMotion.Editor.asmdef -> RootMotionEditor
```

新增：

```text
ThirdPersonCharacter.Presentation.FinalIK
  references:
    ThirdPersonClient.Runtime
    RootMotion
```

`ThirdPersonClient.Runtime`声明vendor-neutral solver合同并只持有显式`MonoBehaviour` adapter引用，经严格类型校验转成`ICharacterFootPlacementSolver`。它不引用`RootMotion`。Final IK adapter不引用Simulation Target、Network Model、BTSMTL或Editor程序集。

### Tradeoff

- 收益：第三方依赖被限制在一个叶子程序集，公共Presentation不被Final IK类型贯穿。
- 代价：Host序列化字段需要保存明确adapter组件，并在创建时做接口类型校验；Unity Inspector不能直接序列化接口本身。

## Decision 12: Reset优先于脚锁连续性

Foot Placement Runtime缓存上一次`ResetSequence`。发生以下任一事件时，当前帧先Reset，再读取新的动画姿势：

- Initialization。
- CommittedBranchReplacement。
- SelectedStreamReset。
- `ICharacterPresentationRuntime.Reset()`。
- Actor/Presentation dispose。
- 动画尚未产生RequireOutput首样本。
- Profile允许范围之外的pose/root不连续。

Reset清除脚速历史、surface anchor、constraint state、solve weight、prediction workspace和pelvis阻尼状态，并以当前动画姿势重新锚定。不能保留旧脚世界位置再平滑追赶新Body。

正常Animancer producer切换和crossfade不触发硬Reset；Timeline曲线采样按实际视觉state weight连续混合。

### Tradeoff

- 收益：rollback纠偏和hard recovery不会把角色骨架拉回旧脚锁；错误不会被长时间平滑掩盖。
- 代价：较大网络reset当帧脚可能短暂回到动画姿势。这比保持错误世界锚点更正确，后续帧会重新plant。

## Decision 13: Local、Simulated、Observed共用同一Pass

Factory所有完整角色表现入口都显式接收：

```text
CharacterFootPlacementProfile
ICharacterFootPlacementSolver
PhysicsScene query context
Self-collider binding
```

SourceMode只决定Body frame来自CommittedStream还是SelectedStream，不选择Foot Placement算法。Camera capability也不决定是否创建Foot Placement。Authority Worker或纯逻辑Host没有Character Presentation，因此不创建Pass。

纯动画Timeline Preview继续只运行AnimationPlaybackRuntime，不伪造Body/Ground，也不创建Foot Placement Pass。项目不再拥有独立Preview Simulation；Play Mode中的Local、Simulated和Observed角色通过正式Factory、显式rig与所属Scene PhysicsScene运行同一Pass。

### Tradeoff

- 收益：本地和远端脚感一致，Network Model不拥有IK特化代码。
- 代价：远端角色也支付场景查询与solver成本。当前2v2vE规模先保持完整质量；未来LOD必须作为显式Presentation quality change设计，不能偷偷按距离跳帧。

## Decision 14: Diagnostics解释计划，不参与计划

每帧只读snapshot至少包含：

```text
ActorId / RenderFrame / Body ticks / ResetSequence
PoseSourceLayerId
Visible producer contributions and weights
Per foot:
  ConstraintState / TransitionReason
  RelativeVelocity / SurfaceDistance / Descending
  PredictedFootprint / SupportCandidateCount
  Surface identity / Point / Normal
  Lock anchor / Replant error
  Placement/Prediction/Rotation weight
Pelvis target/current offset / support foot
Query count / rejected candidate counts
Solver applied / final target
```

Profiler marker至少区分`FootPlacement.Plan`、`FootPlacement.Query`和`FootPlacement.Solve`。Query hit buffer、candidate buffer、foot state和diagnostics snapshot复用固定容量，不在presentation hot path使用LINQ、字符串查找或临时List。

Live Debug复用现有RuntimeDebugSession/Host视图，不增加独立IK EditorWindow。Scene gizmo只读取最新snapshot，不能重新query或修改状态。

## Authoring Model

Corin正式资产关系：

```text
Corin prefab / CharacterPipelineHost
  -> CharacterBodyPresentationProfile
  -> CharacterFootPlacementProfile
  -> FinalIkLimbFootPlacementSolver component
       -> CharacterFootPlacementRig
       -> Left LimbIK
       -> Right LimbIK

CharacterPipelineDefinition
  -> CharacterAnimationPresentationProfile
  -> Program / Projection
```

Foot Placement Profile不进入Definition。Profile Inspector只提供分组的角色级算法参数。动画相对的单一Foot Placement Weight曲线只在独立Timeline窗口选中Animation Clip后编辑，dirty owner是实际Timeline；Graph窗口不复制曲线，CharacterAnimationPresentationProfile也不保存副本。

Corin rig必须显式保存VisualRoot、pelvis、左右hip/knee/ankle/toe、heel/toe sole offsets和self-collider root。不得通过`Animator.GetBoneTransform`、名称、同名子节点、`GetComponentInChildren`或Avatar类型补全。

## Runtime Data Flow

```text
Committed/Selected Body interval
  -> CharacterBodyPresentationRuntime
  -> CharacterBodyPresentationFrame

Presentation command queue
  -> CharacterAnimationPlaybackRuntime
  -> Projection clip curve sampling
  -> AnimationPlaybackLifecycle
  -> AnimancerPlaybackAdapter.Evaluate
  -> AnimationPoseFrame + sampled Foot Placement channels

Body frame + AnimationPoseFrame + animated rig pose + FootPlacementProfile
  -> Contact Classifier
  -> Footprint Predictor
  -> Support Envelope Query
  -> Constraint Resolver
  -> Pelvis Resolver
  -> CharacterFootPlacementPlan
  -> FinalIkLimbFootPlacementSolver
  -> final skeleton pose

CharacterBodyPresentationFrame
  -> Camera Runtime
```

没有任何箭头从final skeleton pose返回Simulation、Body history、Animation selection或Network。

## Error Semantics

以下情况必须在runtime创建前或首次合法帧直接失败：

- Foot Placement Profile、solver adapter、rig或PhysicsScene context缺失。
- adapter组件未实现`ICharacterFootPlacementSolver`或已经绑定另一个active runtime。
- RootMotion命名程序集不可用，或adapter程序集意外引用Assembly-CSharp/firstpass。
- hip/knee/ankle/toe、pelvis、VisualRoot或self-collider binding缺失、重复、跨Actor或层级非法。
- 左右Limb solver链未初始化、自动更新仍启用或同帧被更新两次。
- PoseSourceLayerId不在Projection中，或该层没有animation producer。
- Profile参数或Timeline Animation Clip曲线包含NaN/Infinity、范围倒置、缺key或不满足`[0,1]`约束。
- Ground LayerMask为空、包含明确禁止的Character层，或场景query返回非有限hit。
- Body frame Actor/reset identity与Pass绑定不匹配。

不得自动关闭IK、使用Default层、按名称查骨骼、改用Final IK Grounder、吞掉solver错误或退化为单Ray。

## Lifecycle

创建顺序：

```text
Projection clip curves/Profile/Rig validation
  -> Final IK adapter initialization
  -> Foot Placement workspace
  -> CharacterFootPlacementRuntime
  -> CharacterSimulationPresentationRuntime published
```

每帧顺序：

```text
Body Present
  -> Animation command/sample/lifecycle
  -> Animancer Evaluate
  -> detect/reset Foot Placement
  -> capture animated rig
  -> classify/predict/query/resolve
  -> apply Final IK once
  -> Camera Present
  -> acknowledge frame batch
```

销毁顺序：

```text
Camera
  -> Foot Placement Runtime
  -> Final IK adapter reset/dispose
  -> Animation Runtime
  -> Body Runtime
```

## Parallel Work Boundary

- Rollback input传播：完全并行，Foot Placement只读最终Body frame。
- Program MotionWarp：完全分层，Warp在WorldSolver前改变Gameplay request，Foot Placement只处理WorldSolver后的可见骨骼。
- Corin targeted demo：core和adapter可并行；最终Host/prefab装配必须在其角色role改动后按最新结构合并。
- Gait phase matching：Foot Placement不消费或产生gait phase。若另一change扩张到authoritative foot contact，必须先决定唯一owner再apply。
- 后续Hand/Aim IK：复用Pose Post Process固定插槽的明确顺序，但不得在本change预建空实现或priority系统。

## 实施后成熟度审计

本change已经闭环唯一Presentation Pass和Final IK叶子adapter，但实机视觉反馈表明“预测式第一版”与成熟动作游戏Foot Placement仍有结构差距。正式调研、代码对照、已确认问题和后续演进边界见：

- `maturity-research.md`

审计结论不改变本change已安装的所有权边界：Foot Placement继续只属于Presentation，Final IK继续只负责最终Limb解算，Simulation、Network、Timeline逻辑事实和VisualRoot均不读取IK结果。

以下内容已经作为结构修正进入同一唯一链路，不再留给Profile调参掩盖：

- Corin鞋底参考点与语义foot frame尚未完成真实资产校准。
- 生成Foot Analysis使用统一Calibration地面参考、heel/toe最低接触高度和垂直Plant速度。
- Marker Sync后的视觉时间倍率进入生成速度与landing delay，最终混合姿势差分不再独占Contact。
- Current heel/toe独立查询并形成virtual support plane，路径Envelope不再覆盖当前支撑。
- Replant沿Free与constraint solve weight完成释放后再提交，作者Weight在每条求解链只应用一次。

仍未完成的Corin鞋底几何数值校准必须由作者观察真实鞋底模型后写入共享Calibration，不能由代码猜测。完整三维凸Ground Envelope、水平Pelvis重平衡、Stride Warping与Motion Matching仍不属于本change；不得追加fallback、Final IK Grounder或第二条自主MonoBehaviour路径。

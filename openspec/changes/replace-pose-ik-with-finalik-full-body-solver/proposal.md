# Change: 重建统一GDC Foot Placement并保持单次FinalIK求解

## Why

现有实现虽然已经使用唯一FinalIK FBBIK，但Foot Placement仍然不是GDC动作级预测。自动run `01396a864cc04a4ebcfd5935e9b4577a`与`00214d2c4c164be991ae4b2a382318ca`共同证明：

- 上楼右脚在Executable Plan内出现单帧Goal Y `+0.448m`，FBBIK position residual约`3e-7m`，错误发生在Solver之前；
- 下楼大量Plan被拒绝后视觉反而更好，说明当前预测接管会破坏响应式结果；
- 被拒绝计划并非没有查询。下楼左脚10个`NoFutureLanding`计划执行1090次查询并获得1316个原始命中，失败发生在错误末端落点的选择与可达性；
- `CharacterPredictiveFootPlacementModifier.TryEvaluateFootTarget`虽然计算了完整Path，却从响应式`baselineAnklePosition`出发只加Component-Up高度差，预测Path的XZ从未进入最终Goal。
- 旧改版把约`3.58m`至`3.60m`的同脚周期冻结世界路线直接写入Swing Goal，而同计划实际角色位移只有约`2.81m`至`3.30m`，导致脚被旧世界路线水平拉走；
- 重诊断帧中248个Render Frame只推进87个Simulation Tick，Presentation时间经过`5.86s`而Simulation只经过`1.45s`，证明独立`PresentationDelta`时钟会让动画事件、计划进度和实际角色移动失配；
- 下楼候选在预测Root/Hip仍保持原高度时先做Reach过滤，合法低踏面被错误拒绝；上楼候选反而更易通过并执行错误的全XYZ路线。
- 工作区曾把Corin的in-place Walk/Run从原正式`ConstantSpeed`改成`ActionMotionCurve`，同时Foot Analysis又保存一份Action Root路线。这是IK迭代引入的错误运动链：角色实际移动、预测Root和动画脚路线拥有了多份距离事实，直接解释Corin速度变化、平地Debug路线长度错误及楼梯计划跳变。
- 自动run `8b8ba82f8c254e95af838cdd792b6cc1`又证明Committed Body Yaw也不是位移曲率：冻结值达到`±720°/s`，左右Final Goal最大逐帧位移约`1.05m`，左脚穿透约`6.7cm`，solver residual最高约`13.4/19.9cm`。Simulation实际把世界输入向量作为位移方向，并独立让身体朝向有限追随该方向；二者必须分离。

因此问题不是参数不足，而是四项所有权错误：in-place移动被Action Motion Curve替换、冻结查询路线被当成最终脚轨迹、表现时钟脱离Simulation动作时钟、Reach在地形修正Hip之前执行。继续扩大Cast、放宽Reach或提高Plan接受率只会让错误计划接管更多帧。

## What Changes

- 把Foot Placement收敛为一个world-aware执行owner：同一Pose completion内依次消费权威Action Step Fact、生成不可变预测计划、查询Ground Path、解析Stance与Pelvis、发布唯一最终Goal Set。
- Current Ground Query只提供当前合法支撑、接触捕获和落地后的Stance事实；它不再先生成Swing空间目标供预测器叠加。
- Corin in-place Pose Clip只提供root-local Foot、Ankle、Hip、Clearance与接触事件；Simulation Locomotion唯一拥有作者Move Speed。预测计划在创建时冻结同帧碰撞求解后的committed Body Target Velocity作为当前路径切线，并保留Simulation Motion Timeline描述Timed段与确定Continuation，不读取或生成Action Motion位移曲线。
- 每个同脚Landing Event以25个等Action Phase样本原子保存root-local Foot、Ankle、Hip、平面路线、Animation Clearance与约束事实；旧7点折线会削平Corin落地前回摆并把Ground Path映射到错误踏面，必须通过新artifact与projection schema整体淘汰，不得运行时补点或兼容读取。
- 每脚事件以“上一次同姿态Landing -> 下一次同姿态Landing”定义稳定身份与时间域。每只脚只允许在当前权威事件已经进入PreSwing后创建唯一计划，不再为尚未成为当前动作的incoming事件冻结世界路线或晋升候选。计划以创建帧Native Sole、未来Body轨迹和同相位root-local脚姿态共同重基；`Locked`由同一Stance owner持有，Predictive在正式`ReleasePhase -> LiftOffPhase`区间按SmoothStep从零淡入并与旧Anchor退场互补，LiftOff后取得完整Swing所有权。
- Swing脚的最终XZ与基础旋转继续来自当前上游原动画Component Pose。冻结路线不得作为世界Ankle轨迹把脚水平拉向旧计划。
- Plan的Ground Probe、Ground Envelope、Landing与Foot Rate在创建事务中一次冻结。Ground Probe由本脚Swing起点、权威对侧Landing和本脚Landing组成分段直线；查询按空间长度采样。Foot Rate由本脚冻结动画路线对整条Ground Probe做最近投影并按Action Phase单调化，对侧事件Phase不得强迫本脚经过折线顶点。最终Swing Y唯一等于`GroundPathY(FootRate) + AnimationClearanceY(ActionPhase)`；当前动画只继续拥有XZ和Sole-to-Ankle几何，不再与预测Y逐帧取较高者。最后只满足同帧Current Grounding合法支撑面的向上物理安全下界。
- 世界位移路线只消费创建Tick的Committed Movement Timeline当前段与Continuation速度；身体朝向按`MaximumYawVelocity`有限收敛到各段世界速度方向并在对齐后停止。`YawVelocityDegreesPerSecond`只描述身体朝向变化，不再冒充位移轨迹曲率。相邻Render Frame导数、Visible Body朝向和原始Camera角度都不得直接生成路线。
- 动画Pose、Landing Event与Clearance只消费Simulation提交的Locomotion动作时钟；删除Sequence和Plan按`presentationDeltaSeconds`独立推进的第二时钟。
- Ground Path只拥有高度、法线、边缘和可达性。它不得改变当前动画Foot XZ，也不得按地形三维弧长重参数化动作Root。
- Future Query先建立真实支撑高度，再按该高度平移预测Root/Hip，最后执行Step、Edge和Ankle Reach过滤；同一预测Hip只输入现有Pelvis owner。
- Future Query的向下发现Sweep以同相位预测Hip作为最低顶面，而不是只从Foot Route加固定`CastAbove`开始；这样上楼踏面即使高于无IK脚路线，也必须先进入唯一查询，再由Step、Edge与Reach决定是否可通行。
- Future Capsule命中近竖直边缘时，以`Hit Point + 平面外法线 * Swing Capsule Radius`得到胶囊中心的接触位置并写入Edge Fraction；Upper Envelope必须在鞋底配置空间中提前上楼边缘、延后下楼边缘，不能把墙面点本身当作脚中心路径。
- 对侧脚权威空间Landing是Ground Probe精确顶点，不再把其高度搬到“该Phase的本脚位置”。删除不可通行点后，完整同脚事件的全部合法样本共同形成一次连续Upper Hull，不在对侧顶点强制拆成两个凸包。
- 同一Landing Event默认只提交一个冻结计划；真实Swing期间若新的Committed Movement Timeline使剩余世界落点位移与当前Plan偏差超过现有鞋底/查询几何半径，允许在同一Predictive owner内创建一次离散Revision。旧、新计划必须按同一权威Action Phase从当前最终鞋底重基并连续交叉淡化；若新查询Rejected，旧Plan必须连续退场到上游动画/当前支撑，不得继续拉向失效落点。相机变化只有先进入Simulation并形成新的Committed世界速度后才可能触发Revision，禁止每帧重投影当前脚或重画自适应Path。
- Stance Stabilization继续唯一拥有`Locked / Sliding / Unlocked`、Anchor和Pelvis。`Sliding`只允许脚在支撑面内滑动，不允许鞋底垂直脱离同帧Current Grounding合法支撑面；接触脚必须把Heel/Toe到该面的有符号最小距离写回同一个Foot Offset状态，不能继续等待Swing用Grounding Spring慢慢回落。Current Pelvis只消费真实Stance支撑；Predictive Body Path则由一个完整Executable Plan Sequence持续提供其地形修正Root/Hip，直到该Sequence正式结束，不能因同脚短暂进入`Unsupported`而每帧掉权。双脚Plan重叠时只保留一个Body Path owner，旧Sequence仍有效时不得切换，失效后按与上一Body Target的连续性原子交接；不得平均左右脚独立Path或用钟形Action Progress让身体目标每步归零。
- FinalIK继续只执行一次FBBIK，不查询世界、不规划、不锁脚、不做后处理。
- 删除独立Predictive Modifier作为响应式Goal后处理的语义与旧作者配置；预测成为唯一Foot Placement owner内部的可选动作级能力。
- Scene、Game、CSV只消费同一计划快照。Rejected只显示真实查询与拒绝几何，不画伪Path或悬空Landing。

## GDC Alignment

本change只采用GDC 2016《Fitting the World: A Biomechanical Approach to Foot IK》的核心结构：

```text
Authoritative Action Step
  -> Frozen Collision-resolved KCC Body Translation and finite Body Facing
  -> Ground Detection along Query Route
  -> Terrain-shifted Root/Hip
  -> Reachability and Ground Path
  -> Native Animation Foot XZ + Ground Path + Animation Clearance
  -> Single Support Leg Body Path + Stance/Pelvis constraints
  -> One Full Body IK solve
```

最终鞋底运动固定为：

```text
SwingStartPhase = max(GenerationPhase, LiftOffPhase)
QueryRoute = Polyline(
    LockedSoleAtSwingStart,
    OpposingLanding(KccTranslation, BodyFacing),
    OwnLanding(KccTranslation, BodyFacing))
FootRatePlan = MonotonicFreezeProject(FrozenAnimationSole(ActionPhase), QueryRoute)
GroundPathProgress(t) = Sample(FootRatePlan, SimulationActionStepPhase(t))
BodyPathProgress(t) = Normalize(SimulationActionStepPhase(t), PlanStartPhase, LandingPhase)
FinalSoleXZ(t) = NativeAnimatedSoleXZ(t)
PredictedSoleY(t) = GroundPathY(GroundPathProgress(t)) + AnimationClearanceY(SimulationActionPhase(t))
FinalSwingSoleY(t) = PredictedSoleY(t)
FinalSafeSoleY(t) = MaxPhysicalClearance(
    FinalSwingSoleY(t),
    ExistingCurrentGroundingSupport(t))
```

不是：

```text
FrozenWorldAnkleXYZ(ActionClock(t))
```

## Impact

- 影响Foot Analysis动作事实、Pose Graph Foot Placement操作、运行时Plan/Query/Stance/Pelvis、诊断快照、Gizmo、CSV和Character Build产品。
- 删除旧Modifier后处理合同和旧生成Projection；不提供旧reader、fallback或兼容节点。
- Flat、Slope、Upstairs与Downstairs都走同一Foot Placement owner；没有第二Grounding、Heel/Toe Current Query、第二Pelvis、LegIK/TwoBoneIK或FBBIK后处理。

## Acceptance

- Ground Probe起点必须是Swing开始时的Stance锁脚点；若计划在LiftOff前创建，生成到LiftOff期间的Body位移不得进入Swing Foot Route。对侧Landing和本脚Landing必须由同一冻结KCC轨迹与Body旋转还原成精确折线顶点；
- Simulation Locomotion必须保持Corin正式Constant Speed；Foot Prediction必须报告创建帧冻结的committed Body Target Velocity、Simulation Continuation、最大转向能力、Body Yaw诊断、Plan Revision误差/阈值/Blend、计划剩余时间和预测Root位移；Foot Analysis不得发布Action Root或第二速度真相；最终脚XZ保持当前原动画Pose；
- 上楼Executable Plan不再出现未解释的Goal正负往返或腿部乱飞；
- 上楼Future Query不得因为踏面高于Foot Route的固定扫描顶面而穿过楼梯并选择底层地板；Query快照必须证明首个未来踏面进入接受或明确拒绝链。
- 下楼Ground Path可以下降，Final Heel/Toe没有未解释下陷；
- 当前原动画鞋底相对Pose Root的位置与同一Action Phase烘焙Foot Route的平面误差必须显著低于v79下楼左脚P95 `16.86cm`、最大`25.05cm`和右脚P95 `11.29cm`、最大`16.23cm`的失败基线；
- Rejected不是正常效果来源，稳定楼梯动作的大多数合法步必须形成Executable Plan；
- 同一计划的Landing、Ground Probe、Ground Envelope、Foot Rate和Gizmo快照保持不变；Foot Rate必须来自本脚动画路线对整条折线的单调空间投影，不得按对侧脚事件Phase强制本脚穿过折线顶点；当前Swing高度不得在Native Y与Predicted Y之间逐帧换owner；Current Grounding只可作为最终鞋底物理安全下界，不能规划路线或替代Rejected计划；
- 双脚Executable Plan重叠时唯一Pelvis owner必须保持原`Foot Side + Plan Sequence`，直到该Sequence正式失效才原子切换；左右脚Path不得同时加权成第三条身体路径，Plan Sequence、候选位移、切换和最终Target必须进入Runtime Trace与CSV；
- FBBIK residual保持在正式容差内；
- Runtime/Editor编译、精确Float32与Fixed Character Build、OpenSpec strict validate、单一路径静态搜索和Unity Console检查通过；
- 固定双向短测优于本proposal中的失败基线后，才进入30分钟与8小时回归。

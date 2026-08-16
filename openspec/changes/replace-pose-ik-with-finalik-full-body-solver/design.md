# 设计：动画生物力学数据驱动的GDC Foot Placement

## 1. 设计目标

目标不是继续修补一个“提前向下查询”的响应式IK，而是建立可证明的数据链：

```text
动画本来如何迈步
  + 角色在该步内将如何移动与转向
  + 脚下可通行地形
  + 支撑腿与身体约束
  = 唯一Foot/Pelvis Goals
  -> 一次FinalIK FBBIK
```

GDC原始分享给出的关键语义固定为：

- 预测角色运动主要通过hips体现；
- 脚的前向运动来自动画；
- 动画脚高度表示高于Foot Path的高度；
- 脚绝不能低于Foot Path；
- 锁脚约束由数据定义，区分Locked、Sliding、Unlocked；
- hips使用支撑腿并区分上坡与下坡；
- 脚掌朝向区分上坡、下坡与跑步；
- 身体旋转支点应靠近接触脚；
- Foot Path通过Virtual Ground、Capsule检测、法线与Edge Plane、Reachability和Convex Hull形成连续feet-only Ground Envelope。

## 2. 唯一执行边界

正式对外链：

```text
FootGrounding -> optional PredictiveFootPlacementModifier -> FinalIK FBBIK
```

内部数据流：

```text
Original Component Pose
  + Action Biomechanical Step Fact
  + Committed Future Body Transform Trajectory
  + FootGrounding Current Support
        |
        v
Predictive Plan: Foot Route -> Ground Query -> Ground Envelope
        |
        v
Stance: Constraint -> Landing -> Anchor -> Support Leg -> Pelvis -> Body Pivot
        |
        v
One Final Goal Set -> One FBBIK Solve
```

`FootGrounding`只拥有Current Support、surface identity、接触平面和鞋底安全事实。它不得先创建响应式Swing Goal。Predictive关闭、Unavailable或Rejected时，Swing保持上游原动画；Stance仍可约束真实接触脚，但不得把该结果标记为预测成功。

Predictive可以作为统一FootPlacement节点内部模块存在，不要求恢复独立作者节点。模块边界和数据所有权必须保留，不能把它重新写成Current Goal后的高度补丁。

## 3. 所有权

| 事实 | 唯一owner | 禁止来源 |
|---|---|---|
| 动画脚、踝、膝、髋局部运动 | Biomechanical Step Artifact | 输入幅值、Ground Query、当前Transform反推 |
| Action Step身份与相位 | Projection的权威source | Plan私有elapsed、Render Delta、Stored Pose |
| 世界身体平移与朝向 | Simulation/KCC Future Body Trajectory | 动画步幅、Visible导数、Body Yaw猜曲率 |
| 当前支撑 | FootGrounding唯一查询 | 默认地面、KCC Ramp、第二Heel/Toe查询 |
| 未来地形包络 | Predictive Plan创建事务 | 每帧重查、Current Grounding fallback |
| Locked/Sliding/Unlocked与Anchor | Stance owner | Predictor私有Anchor、FBBIK |
| 支撑腿、Pelvis、Body Pivot | 同一个Stance/Pelvis owner | 左右脚各自Pelvis、Ground Envelope直接驱动 |
| 骨骼求解 | FinalIK FBBIK | LegIK、TwoBoneIK、GrounderFBBIK、后处理 |

## 4. Animation Biomechanical Step Artifact

现有`AnimationFootAnalysisArtifact`原地升级schema，不建立并行资产。

### 4.1 Identity

Identity必须覆盖：

- AnimationClip import dependency；
- Rig v4、Sampling Rig、Calibration v4与Geometry Validation；
- Analyzer algorithm、format、采样域与固定重建容差；
- source looping、event segmentation与对侧配对算法；
- constraint、clearance、support leg、orientation与pivot算法身份。

任一字段变化都使旧artifact Stale。旧v26 reader、字段补齐和运行时重建全部删除。

### 4.2 每个Landing Event的原子数据

```text
EventIdentity
FootSide
SourceIdentity / Cycle / EventOrdinal
ReleasePhase / LiftOffPhase / ApproachContactPhase / LandingPhase
StepDuration / TimeToLanding

RootLocalHeelPositionRoute
RootLocalToePositionRoute
RootLocalSolePositionRoute
RootLocalSoleRotationRoute
RootLocalAnklePositionRoute
RootLocalAnkleRotationRoute
RootLocalKneePositionRoute
RootLocalHipPositionRoute

AnimationFootPlanarRoute
AnimationClearanceAboveReferenceFootPath

ConstraintModeIntervals
ConstraintWeightRoute
SupportWeightRoute
SupportLegLengthRoute
SupportLegCompressionReserveRoute
SupportKneeBendPlaneRoute
SupportFootPivotPositionRoute
SupportFootPivotWeightRoute

OrientationPolicy
OpposingLandingIdentity / Time / RootLocalSolePose
```

连续路线使用由schema固定的等Action Phase采样域。采样数不是作者参数；若当前采样数无法通过重建容差，提升schema与format version并整体重建，不允许Runtime插值补点掩盖。

### 4.3 In-place边界

Artifact只表达骨骼相对角色的动画事实：

```text
LocalBoneMotion = AnimationClip sampled pose relative to Visual Root
```

它不表达：

```text
WorldCharacterTranslation
MoveSpeed
ActionMotionCurve
KccTravel
CameraRelativeInput
```

Corin世界位移与转向只来自Simulation/KCC。

### 4.4 Flat Reconstruction Gate

Editor必须用artifact和同一Action Phase重建原AnimationClip的Heel、Toe、Sole、Ankle、Knee、Hip位置以及Sole/Ankle旋转。

输出至少包含：

- 每语义骨位置最大误差与P95；
- Sole/Ankle旋转最大角误差与P95；
- Release、LiftOff、ApproachContact、Landing相位误差；
- 左右事件交替、cycle连续和对侧配对；
- 原Foot Planar Route与重建路线的弧长、侧向范围和端点误差。

超过固定容差时artifact不得进入Projection Build。该门禁不是自动化测试副本，而是正式Artifact有效性合同。

## 5. Projection与Action Step所有权

Pose允许连续混合；Biomechanical Step Fact必须原子选择。

```text
同一脚本帧输出 = 一个Source的完整Landing Event + 同一个Action Step Clock
```

禁止分别混合路线、Clearance、约束、支撑腿、orientation或pivot。逐脚Pose权重可以混合当前Heel/Toe/Sole运动，但不能改变离散event identity。

Start、Loop、Stop与MovingTurn必须在LiftOff前提供当前脚PreSwing事件。目标source在Pose权重暂时为0时仍可拥有事件事实；退出源、Stored Pose和Inertial History不能夺回事件时钟。

## 6. Committed Future Body Transform Trajectory

Simulation/KCC为剩余Action Step提供：

```text
B(t) = Position(t), Facing(t), LinearVelocity(t), AngularVelocity(t)
```

该轨迹必须使用正式移动规则、碰撞和朝向限制。Foot Placement只读消费，不从输入或表现结果重新求解。

初始Plan冻结一个trajectory identity。Predictive world route定义为：

```text
PredictedSoleWorld(t) = B(t) * RootLocalSole(t)
PredictedAnkleWorld(t) = B(t) * RootLocalAnkle(t)
PredictedHipWorld(t) = B(t) * RootLocalHip(t)
```

`B(t)`包含位置与旋转，不能把Facing固定为创建帧，也不能把最大转速或Body Yaw直接积分成位移圆弧。

### 6.1 离散Revision

新的committed trajectory只有在剩余Landing位置或朝向误差超过鞋底/查询几何边界时创建后继Revision。每个Revision仍是不可变计划。

Plan创建时必须原子冻结Action Step时长、Future Body轨迹时间范围和`phase -> trajectory time`映射。运行时Action Step Clock只推进同一事件的权威phase；若正式动作时长变化会改变剩余Landing，则它属于新的committed trajectory输入，必须经Revision替换，不能直接改写旧Plan的时间尺度。否则新时长会超出旧Future Body范围，或在不报错时悄悄改变冻结路线的采样位置。

后继计划从当前已执行结果连续重基：

```text
Position_new(phase0) = CurrentFinalSolePosition
LinearVelocity_new(phase0) = CurrentExecutedSoleVelocity
AngularVelocity_new(phase0) = CurrentExecutedBodyAngularVelocity
```

旧、新计划交叉期间同时保留各自geometry和identity。新计划未进入Executing前不能删除旧输出；Rejected后继只允许旧输出连续退到原动画，不允许Current Grounding伪造一条新预测Path。

同脚的下一Landing Event由Projection提前发布为`IncomingPredictedStep`。Planner必须在该事件仍为PreSwing时使用现有Revision槽生成唯一Event Successor，并以旧Plan已经查询提交的Landing Sole与Surface作为下一步Ground Path起点。Successor成为Current事件前只冻结geometry和时钟，不参与Goal；事件换代后按同一权威phase进入Executing并与旧输出连续交接。若等旧Plan结束后才为Current事件创建计划，低表现帧率下事件可能已经越过LiftOff，随后整段Swing只能返回原动画或Current Grounding，形成上坡踏空与迟到托举。

## 7. Foot Route与Ground Envelope

### 7.1 动画Foot Route

脚的平面运动来自动画与未来身体Transform：

```text
FutureFootRoute = FutureBodyTransform * RootLocalAnimationFootRoute
```

局部X、Z和旋转都必须保留。Plan的查询路线从`PathStartPhase`开始，一次刚性重基必须使该相位的Artifact Foot Route与当前已提交的接触点或已执行Sole正下方重合；该变换整步冻结，不随当前脚逐帧更新。事件生成相位早于LiftOff时，不得拿更早的Native Sole去对齐更晚的路线起点。

地面路线起点和净空连续性是两个事实：

```text
RouteStart = ProjectCommittedSoleToCommittedSupport(CurrentExecutedSole)
ClearanceStart = CurrentExecutedSole - RouteStart
```

初始PreSwing使用Locked Contact；意图Revision使用当前已执行Sole投影到旧计划当前支撑面。禁止让新动画路线从当前Sole开始、Ground Probe却从旧Envelope的另一个XZ点开始。

当前最终Swing XZ继续来自同相位Native Animated Sole。Artifact重建和Projection时钟正确时，它应与计划当前样本重合；偏差只能触发明确invalid或离散Revision，不能让冻结路线水平拉脚。

### 7.2 Virtual Ground

本脚Foot Path在期间权威对侧接触处分段：

```text
Current/Previous Own Contact
  -> Opposing Contact
  -> Predicted Own Landing
```

对侧点是空间拓扑，不是强迫本脚在对侧事件时刻经过该点。

`Virtual Ground`就是上述分段直线查询拓扑；它与`FutureFootRoute`是两个事实。前者只用于发现脚下地面与构造包络，后者保留动画脚的平面运动、落点和Foot Rate。最终脚XZ不得沿Virtual Ground移动，对侧接触也不得改写本脚动作时钟。

### 7.3 GDC Ground Path顺序

1. 沿Virtual Ground各分段执行Sphere/Capsule检测，先保存全部位置与法线，不对尚未收集完整的相邻端点提前判不可达；
2. 按路线前后排序，再按同位置的高低排序；
3. 验证坡度法线并建立Edge Plane；
4. 对排序后的正式支撑链检查Step Up/Down、间隙与腿长，对真实Edge Plane检查垂直断裂；
5. 删除不可通行点或将计划标记为明确Rejected；
6. 对剩余点构造二维上侧Convex Hull；
7. 得到连续分段直线Ground Envelope。

同一个Sphere查询在台阶边缘可能同时返回上下两个踏面。Physics Cast的距离排序只表示先撞到谁，不表示路线所有权；正式采样必须按前一已提交支撑做有向可达筛选，并在可达候选中选择最高踏面。Capsule命中与正式Sphere支撑落在同一Foot Rate时可以合并高度，但必须保留正式支撑身份。最终Landing Surface、Body Support终点和Ground Envelope终点必须从可达链的同一个末端样本提交，禁止继续使用排序前预选的另一份Landing。

Ground Envelope只属于feet。它不保存动画Clearance、不改变Foot XZ、不驱动Pelvis、不产生默认支撑。

### 7.4 最终Foot Motion

```text
GroundHeight = SampleGroundEnvelope(FootRate)
Clearance = SampleAnimationClearance(ActionPhase)
FinalSoleXZ = NativeAnimatedSoleXZ
FinalSoleY = GroundHeight + Clearance
```

Heel/Toe只使用Calibration在该Sole Pose上重建，并沿Component Up满足唯一支撑平面的最小物理净空；禁止第二Heel/Toe Current Query。

## 8. Constraint、Landing与Anchor

唯一状态顺序：

```text
Locked -> Sliding/Releasing -> Unlocked Swing -> Approaching -> LandingBlend -> Locked
```

- Locked：完整世界位置锁定，允许受限旋转；
- Sliding：保持唯一支撑面垂直接触，允许有限面内滑动；
- Unlocked：不消费旧Anchor，完整保留动画Swing轮廓；
- LandingBlend：使用同一冻结Landing Surface、同一Sole Pose和同一identity交给Anchor。

约束模式和连续权重来自动画数据；FootGrounding只验证真实surface、距离、坡度和reach。ApproachContact不是接触许可，预测Goal也不能自证接触。

Landing必须是一笔原子事务：

```text
Plan Landing Pose
Support Surface Identity
Anchor Local Point/Normal
Committed Sole Pose
Successor Step Start
```

这五项不能分别查询、分别换代或用Current Surface代替。

## 9. Predictive Body、Support Leg与Pelvis

Foot Ground Envelope不能驱动身体。身体使用独立但同源的Body Support Path：

```text
Last Committed Support -> Opposing Support -> Predicted Own Landing
```

Hip目标组合为：

```text
PredictedHip = BodySupportPath + AnimationHipRelativeToBodyPath
```

支撑腿身份与权重由同一Step Fact和已提交接触决定。上坡、下坡分别使用明确support policy；双脚事实不足时报告Unavailable，不做隐式平均。

Pelvis处理顺序：

1. 直接应用Body Support Path要求的位移；
2. 根据支撑腿长度、压缩余量与膝盖弯曲平面求可达区间；
3. 临界spring只增加支撑腿pull并消除bounce；
4. 输出一个Pelvis Pre-Solve Transform；
5. 不可达时沿同一Stance Blend释放对应约束，不单帧Clear Anchor。

### 9.1 Foot Orientation

策略由动作数据和运行时坡向共同决定：

- 上坡：脚掌趋于水平，避免脚尖过度贴坡拉低hips；
- 下坡：脚掌趋于与支撑面平行；
- 跑步：默认保留动画，不应用坡面orientation；
- pitch与roll受Rig/leg reach限制，超出时通过同一Pelvis owner处理。

### 9.2 Body Pivot

临近支撑接触且支撑脚Locked时，有限body/pelvis rotation围绕数据提供的Support Foot Pivot计算；Unlocked脚随身体运动，Locked脚世界Goal保持不变。Pivot权重来自同一Step Fact，不按当前表面或Render Frame临时切换。

## 10. FinalIK边界

FinalIK只消费原始Component Pose和一个最终Goal Set：

- Pelvis Pre-Solve Transform；
- Left/Right Foot position与rotation；
- 其它不重叠Hand Goals。

它只执行一次FBBIK，不查询世界、不读Artifact、不决定接触、不修改Plan、不锁脚、不平滑Pelvis，也不执行后处理。

## 11. 诊断闭环

### GameplayLab自动反馈环

普通`Local Fixed`入口始终保留玩家自由输入和中立Target。独立的`Foot IK Automatic` Variant只实例化同一玩家Fixed Character Host并替换其Control Source，通过正式Input System提交`MoveAxis`，ActionTarget输入正式为`None`；它不接管`LookAxis`、不写Transform、不修改速度或Time Scale，也不运行与脚步回归无关的第二套角色表现。

动画Source的Clip Catalog属于编译后不可变结构，只能在Source创建或换代时完整校验并建立索引。逐帧Prepare只校验会变化的采样计划、时间和权重，只清理上一帧实际激活及本帧实际写入的Clip；禁止按完整Catalog或最大容量重复遍历、重复校验和重复清空。

Foot Feature与Biomechanical Step曲线同样属于已编译Source的不可变数据。Sequence Player创建时必须完整校验一次；运行帧只采样当前相位。同脚同Landing事件的后继事件只允许搜索一次并缓存绝对候选时刻，后续帧只计算该时刻相对当前权威时钟的剩余时间。Source、Continuity或Landing occurrence变化时才允许重新搜索。

共享测试环境提供一条正式宽课程：30米宽、24级上楼、6米平台、24级下楼。Gameplay碰撞继续使用两条连续Traversal Ramp，Foot Placement只消费48个逐级踏面。自动源不再循环双向长路线，而是对齐起点后进入第一段楼梯，直接通过正式Input System提交`MoveAxis.x=-1/+1`的`A 1秒 -> D 2秒 -> A 1秒`事务。事务完成后提交零输入，写入完成快照并注销采样路由，避免1217列CSV在无新信息时继续增长。它保留实时相机Basis和LookAxis，不用世界空间横向路点抵消相机缓动。课程整体位置由场景中的唯一Course与Start/End决定，不写死世界X坐标。

课程启动门禁必须验证：唯一Course、唯一Start/End、48个踏面、A/D横向安全边界、两条Traversal Ramp和唯一Deterministic Collision World。路线阶段、正式MoveAxis、实际速度和事务分段进入现有流式CSV/manifest，使输入变化与Plan Revision可以对账。短事务仍保留Step、Future Body、Ground、Path、Landing、Anchor、Pelvis与FBBIK完整因果列；缩短的是重复时间，不是诊断字段。

自动writer只登记`LiveState`诊断兴趣，从完成帧流直接构造行并交给后台压缩线程；不得自动附加`RuntimeDebugSession`或启动`Continuous`内存捕获。Inspector手动Capture与自动CSV是两个观察入口，但只能消费同一完成快照，不能同时保存第二份无界帧历史。

### Artifact阶段

- 原Clip与Artifact重建路线；
- position/rotation误差；
- event、constraint、support leg、orientation、pivot；
- artifact/schema/projection identity。

### Runtime阶段

- authoritative source、event与Action Phase；
- Future Body Position/Facing/Linear/Angular路线；
- Native、Predicted与Final Sole/Ankle；
- Virtual Ground、全部Capsule request/hit、法线、Edge Plane、Rejected原因；
- Reachability过滤前后点集与Ground Envelope；
- Active/Revision identity、交接位置/线速度/角速度和Blend；
- Constraint、Anchor、Support Leg、Body Support Path、Pelvis、Orientation、Pivot；
- FBBIK Goal、solver node、physical foot与两层residual。

Scene、Game与CSV只读取同一完成快照。Gizmo不显示文字；不同语义使用稳定颜色，Rejected不得画伪Path或悬空Landing。

## 12. 失败处理

以下情况必须明确Unavailable、Rejected或typed failure，不得fallback：

- artifact旧版本或平地重建失败；
- source/event/clock不连续；
- Future Body trajectory缺失或identity不匹配；
- Foot Route自交、非有限或与Native当前样本不一致；
- Landing无合法支撑；
- edge、gap、step或leg reach不可通行；
- Revision无法连续交接；
- Landing事务支撑identity不一致；
- Final Goal非有限或FBBIK残差超限。

## 13. 取舍

- Artifact体积、Build时间和Projection payload会增加，但换来可验证、不可拆分的动画事实；
- 第一阶段不会立刻改善楼梯观感，因为必须先证明动画数据正确；这是停止runtime反复打补丁的必要成本；
- Predictive关闭或失败时保留原动画Swing，可能继续穿过复杂地形，但不会由响应式路径伪装成预测成功；
- 完整GDC身体层比单脚清障范围大，但它是解决上坡hips下陷、支撑腿不自然和转向扭曲的唯一结构性路线；
- 不保留旧artifact、旧Projection或旧Plan兼容路径，迁移期间允许明确Build失败。

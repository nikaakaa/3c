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
Original Component Pose + Step Facts + Body Trajectory + World Context
  -> CharacterFootPlacementRuntime.EvaluateFrame
  -> FootPlacement Final Goal Set
  -> FinalIK FBBIK
```

内部数据流：

```text
Frame Input Snapshot
  -> Current Support Query Facts
  -> Predictive Plan Create/Evaluate Facts
  -> Left/Right Constraint Proposals
  -> Pelvis Reach Arbitration
  -> Landing/Anchor Atomic Commit
  -> Left/Right Final Foot Results
  -> Write Pelvis/LeftFoot/RightFoot Goals Once
  -> Frame Result + Completed Diagnostics
```

Pose Plan只认识`CharacterFootPlacementRuntime`这一个world-aware Goal producer。`Current Support`、`Predictive Plan`、`Stance`和`Pelvis`都是该深模块内部的单向步骤，不暴露需要调用方维持顺序的领域协议。调用方不得执行`Prepare -> GetStanceInput -> ObserveStance -> Resolve`，也不得先取得一套Grounding Goals再请求Predictive覆盖。

`CharacterFootPlacementRuntime.EvaluateFrame`接收不可变`CharacterFootPlacementFrameInput`并返回不可变`CharacterFootPlacementFrameResult`。它读取上一完成帧Committed Foot状态，构造本帧Pending Foot状态；只有完整Goal Set、后续FullBodyIK和表现帧完成后，Pending才随外层Presentation事务Seal。失败、Discard、Reset或Fault不得提交部分左右脚状态。

Current Support只发布surface identity、接触平面、距离、法线、鞋底安全和有限filter结果，不拥有Swing、Plan、Anchor或Goal。Current Grounding spring的数值记忆属于对应脚的唯一执行状态。Landing接触许可可以读取动画鞋底到冻结Landing Surface的距离，但该距离不得作为增量反复写回spring。

Predictive只负责创建不可变Plan和按当前Clock/Root求值Plan。Predictive关闭、Unavailable或Rejected时，Swing保持上游原动画；Stance仍可约束真实接触脚，但不得把该结果标记为预测成功。Executable Swing一次性组合Ground Envelope、动画净空、Foot Orientation与Heel/Toe安全目标，最终结果不得再被Current Support末端改写。

## 3. 所有权

| 事实 | 唯一owner | 禁止来源 |
|---|---|---|
| 动画脚、踝、膝、髋局部运动 | Biomechanical Step Artifact | 输入幅值、Ground Query、当前Transform反推 |
| Action Step身份与相位 | Projection的权威source | Plan私有elapsed、Render Delta、Stored Pose |
| 世界身体平移与朝向 | Simulation/KCC Future Body Trajectory | 动画步幅、Visible导数、Body Yaw猜曲率 |
| Current Support事实 | Current Support Query/Resolver | 默认地面、KCC Ramp、第二Heel/Toe查询 |
| 未来地形包络 | Predictive Plan Builder创建事务 | 每帧重查、Current Grounding fallback |
| 单脚Constraint Phase、spring、Anchor、Plan与Transition | `CharacterFootExecutionState` | Grounding `FootState`、Predictive `FootPlanRuntime`、Plan内部状态 |
| Landing事实 | `CharacterFootLandingCommit`原子值 | 分散的Plan、Current Surface、Anchor和Successor字段 |
| 支撑腿、Pelvis、Body Pivot | 单次Pelvis Reach Arbitration | 左右脚各自Pelvis、Ground Envelope直接驱动 |
| Foot Placement最终Goals | Frame Result唯一Goal assembler | Grounding baseline、Predictive覆盖、FBBIK后处理 |
| 骨骼求解 | FinalIK FBBIK | LegIK、TwoBoneIK、GrounderFBBIK、后处理 |

### 3.1 Frame Input与Frame Result

`CharacterFootPlacementFrameInput`是本帧唯一输入快照，至少包含：

```text
Actor / Frame / Completion / Reset identity
Original Component Pose
Left/Right authoritative Step Facts and Clock
Committed Body and Future Body Trajectory
Foot Placement Weight
Rig / Calibration / Profile identity
PhysicsScene world context
Presentation transaction identity
```

`CharacterFootPlacementFrameResult`只在全部内部阶段成功后可用，至少包含：

```text
Availability or typed failure
Pelvis Goal
Left Foot Goal
Right Foot Goal
Left/Right resolved foot outcome
Pending state mutation identity
Completed diagnostics snapshot
```

Frame Result不暴露中间Grounding Goal、Predictive覆盖结果或可由调用方再次组合的mutable对象。

### 3.2 每脚唯一执行状态

每只脚只有一个`CharacterFootExecutionState`：

```text
Constraint Phase
Current Support filter state
Anchor
Active immutable Plan handle
Optional Plan Transition
Last completed Original/Final Sole and Ankle
Landing Commit
Query Attempt identity
```

`Plan Transition`是执行状态内部的值，保存`IntentRevision`或`EventSuccessor`、旧/新Plan引用、唯一Blend和相对Original动画的连续性修正。它不是第二个owner。Intent Revision与Event Successor继续顺序复用一个槽。

退出预测也复用同一Transition槽并标记为`PredictiveExit`，不得再维护独立Fade owner。运行诊断和CSV逐脚发布`PlanTransitionKind`，使后续异常帧可以直接区分Intent换路、Event后继和预测退出。

左右脚从同一个Committed Frame读取，分别生成约束提案；在Pelvis仲裁与Landing验证完成前不得直接改写Committed状态。Pelvis拒绝某个约束时，拒绝结果回到本帧Pending状态的单次Finalize步骤，而不是回调Grounding重新跑一遍。

### 3.3 Predictive Plan只保存不可变事实

`CharacterPredictiveFootPlan`由Builder在预分配候选槽内完整构造并Seal。Seal后只包含：

```text
Plan / Event / Source / Trajectory identity
冻结的phase到trajectory time映射
Foot/Ankle/Hip/Body路线
Virtual Ground与Query快照
Ground Envelope与Surface链
Landing候选与Body Support Path
创建结果或精确Rejected原因
```

Plan不得再保存或推进`Active`、`Revision`、Fade、Anchor观察、Action Step当前相位、Ground Path当前进度、输出连续性和本帧world projection。`CharacterPredictiveFootPlanEvaluator`以`Plan + Current Clock + Current Root + Original Pose`为输入，返回当帧只读样本，不修改Plan。

Rejected是一次候选结果，不是可以提升为Active的Plan状态。上一有效Active、Transition与Anchor如何保留或退出，只由`CharacterFootExecutionState`决定。

### 3.4 Query是事实函数

`CharacterFootPlacementWorldQueryBackend`继续作为唯一PhysicsScene adapter。Current Support Query与Predictive Ground Path Query都使用显式request/result：

```text
QueryRequest + WorldContext -> QueryResult
```

Query可以使用预分配workspace，但不得读取或修改Foot Execution State、Plan Transition、Anchor、Pelvis或Goals。Plan Builder只消费Query Result并把必要快照Seal进Plan。

### 3.5 单向Foot与Pelvis仲裁

每脚状态机先生成`CharacterFootConstraintProposal`，包含Original、Current Support、Predictive样本、候选Landing和reach输入，但尚不提交Anchor。唯一Pelvis resolver同时消费左右提案和Body Support Path，返回：

```text
Pelvis Pre-Solve result
Left constraint disposition
Right constraint disposition
Support Leg and Body Pivot result
```

随后Frame Finalizer只执行一次：接受或释放左右约束、原子安装Landing Commit与Anchor、保存上一完成输出，并从最终状态构造脚目标。Pelvis resolver不得直接改写Foot状态，Foot状态机也不得在Pelvis完成后被Predictive再次改写。

### 3.6 Landing与Goal发布

`CharacterFootLandingCommit`是不可拆分值：

```text
Plan Sequence
Landing Event identity
Landing Sole Pose
Support Surface identity and local plane
Anchor local pose
Committed Sole Pose
Successor Step origin
```

任一字段无效则整笔Landing不提交。Landing只在Pelvis仲裁接受相应约束后进入Pending Foot状态。

Foot Placement最终只写三个Goal槽：Pelvis、Left Foot、Right Foot。Goal assembler必须在左右脚和Pelvis全部完成后一次写入Goal workspace；不存在`CharacterFootGroundingPlan` baseline，也不存在Predictive `Resolve`对已写Goal的覆盖。

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
IncomingStepEvent / Clock / Route / BiomechanicalFacts
```

连续路线使用由schema固定的等Action Phase采样域。采样数不是作者参数；若当前采样数无法通过重建容差，提升schema与format version并整体重建，不允许Runtime插值补点掩盖。

`ApproachContactPhase`不是固定的“Landing前一个采样点”。Analyzer先用本脚前一支撑、权威对侧支撑和本脚Landing构造同一条参考Foot Path，再计算每个Swing采样的`Sole Y - Foot Path Y`；从LiftOff到Landing之间首次达到最大非负Clearance的采样点定义为进入最终下降段。该边界与Animation Clearance来自同一数据和同一Action Phase域，使Incoming Event Successor在脚实际下降期间获得稳定预建窗口，Runtime不得用帧数或时间阈值补出该窗口。

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

Pose允许连续混合；Biomechanical Step Fact必须原子选择。Artifact的每个采样点同时提供Current与Incoming Step；Incoming不是Runtime向未来扫描Current曲线得到的临时结果，而是Analyzer在同一事件分段、同一Action Phase域中生成的完整后继事实。

```text
同一脚本帧输出 = 一个Source的完整Landing Event + 同一个Action Step Clock
```

禁止分别混合路线、Clearance、约束、支撑腿、orientation或pivot。逐脚Pose权重可以混合当前Heel/Toe/Sole运动，但不能改变离散event identity。

`Current + Incoming`是一个不可拆分的Projection值。Sequence绑定Marker occurrence、BlendSpace/TreeClip贡献竞选、StateMachine target预取和Slot选择都必须一次选择同一source的整对事实。禁止Current按一个score竞选、Incoming按另一个score竞选，也禁止保留当前source的Current后从待切换source挑选“更早Incoming”。StateMachine已同步的Predictive Target即使Pose权重为0，也只能整对接管Step事实；连续Sole速度、高度和Plant Confidence仍可按Pose规则混合。

Pose contribution weight只描述最终骨骼Pose由哪些动画贡献，不拥有Biomechanical Step输出权重。权威Step一旦被Projection原子选择，预测脚输出只由该Step的Release/LiftOff、Plan生命周期、Revision和现有Stance交接控制；不得再乘逐脚Pose weight，否则目标source在0权重预取后会经历第二次`0 -> 1`所有权切换。

Projection完成后的Action Frame属于轻量不可变快照。完整左右脚Current/Incoming Step值写入现有Action Sample Workspace的预分配Foot Feature页，Frame只携带受同一lease约束的只读Buffer句柄；不得把持续增长的Biomechanical payload作为巨型值类型经由Dictionary或接口按值复制，也不得为规避复制改成逐帧分配巨型托管对象。Native Pose workspace中的单脚Feature继续使用固定布局值类型，Workspace页与Frame句柄只改变传输方式，不得拆出另一条事实链。

Start、Loop、Stop与MovingTurn必须在LiftOff前提供当前脚PreSwing事件。目标source在Pose权重暂时为0时仍可拥有事件事实；退出源、Stored Pose和Inertial History不能夺回事件时钟。

Sequence Player把Artifact occurrence绑定到Locomotion Marker时，必须直接使用source-bound Landing Cycle与Event Ordinal选择正式Marker occurrence。`TimeToLanding`只负责连续动作时钟，不得再次通过`ContinuousTime + delay`就近搜索Marker；否则同一事件会同时拥有Artifact cycle和Runtime时间距离两个身份判定。

Analyzer必须为Current与Incoming分别烘焙离散`SourceLandingCycleOffset`。该字段与Event Ordinal共同定义事件分段，事件换代处的Clock、路线、Clearance、Constraint和Biomechanical事实必须整组阶跃，禁止跨两个occurrence线性插值。Runtime只把该offset加到当前source cycle，不得从插值后的`TimeToLanding`反推cycle。

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

Trajectory Curvature必须由相邻Simulation committed Intent的Desired Planar Velocity、Authority Tick和正式Tick Rate计算，并在整个Simulation tick区间保持同一值。Presentation插值只采样该已提交曲率；禁止对相邻Render Frame的插值速度再次求导，否则表现帧率和摄像机缓动会成为Future Body路线输入。

### 6.1 Step模式、执行投影与离散Revision

参考文章4把“每帧预测”拆成两种成本完全不同的工作：Foot Lock到Foot Unlock建立一次可执行路径；之后每帧只把该路径对账到当前角色运动，只有误差超过边界才重新执行Capsule Sweep和Ground Path构造。本项目采用同一分层，不把逐帧显示更新误写成逐帧Physics重规划。

每个Action Step在Release前原子选择`Predictive`或`Traditional`模式。接近最大加速、接近最大减速、低速急转、没有有效前一步历史、空中或动作事实过期时可选择Traditional；其余选择Predictive。模式在整个Step内保持不变。Traditional属于统一Foot Placement owner的正式策略，不是Predictive Query失败后临时交给Current Grounding的fallback。

Predictive Plan冻结Step identity、Action Clock、Artifact路线、查询结果与Ground Envelope。运行帧读取Simulation/KCC发布的当前正式Root，并与同相位Future Body Expected Root比较，得到只含Component Up轴Yaw和水平位移的刚体差。Foot Route、Ground Envelope、Landing、Future Support与Body Path统一消费这一变换；不重新查询、不改写Plan、不读取Camera或Visible导数。进入`ApproachingContact`时冻结最后一次执行投影，使Landing、Anchor候选和后继Step起点在接触窗口内保持同一个世界事实。

只有新的committed trajectory使剩余Landing位置或朝向误差超过鞋底/查询几何边界，才创建昂贵Revision。查询资格按`源Plan sequence + trajectory generation + authority tick`记账：同一权威tick至多查询一次，下一正式tick若误差仍存在可以重试。一个Rejected候选不能永久封死该源Plan，也不能删除仍有效Active。

Plan创建时必须原子冻结Action Step时长、Future Body轨迹时间范围和`phase -> trajectory time`映射。运行时Action Step Clock只推进同一事件的权威phase；若正式动作时长变化会改变剩余Landing，则它属于新的committed trajectory输入，必须经Revision替换，不能直接改写旧Plan的时间尺度。否则新时长会超出旧Future Body范围，或在不报错时悄悄改变冻结路线的采样位置。

后继计划从当前已执行结果连续重基：

```text
Position_new(phase0) = CurrentFinalSolePosition
LinearVelocity_new(phase0) = CurrentExecutedSoleVelocity
AngularVelocity_new(phase0) = CurrentExecutedBodyAngularVelocity
```

旧、新计划交叉期间同时保留各自geometry和identity。Revision必须先在过渡槽完成Query并Commit，成功后才允许参与Blend或提升。Rejected候选只发布精确原因并等待下一权威tick；它不得清空Active、清除完整Debug Path或使Grounding接管Swing。初始候选Rejected时，当前Step仍保留Predictive身份并继续正式重试，直至成功或Step结束；不得把一次查询失败解释成Traditional模式切换。

意图Revision的创建起点只能读取上一完成帧已经送入唯一FBBIK的Final Ankle/Sole、同帧Original Animated Ankle/Hip、该输出所属Active Plan的Ground Path与支撑面，以及同一完成帧的Body Path Root/Hip。Ground Probe由Final Sole沿Component Up投影到该支撑面；不得重新求值旧Plan的理论Target冒充已执行结果。Revision创建时冻结的是`Final Ankle - Animated Ankle`、对应旋转差与`Body Path - Animated Hip`，Ground Path与Support继续保持环境世界事实；Blend期间旧侧由当前同相位Original动画加上述冻结修正组成，新侧先用自己的Ground Envelope、动画净空、方向与Reach完整求出安全目标，再只按Revision Blend混合一次最终Ankle。禁止把Swing的世界Ankle绝对锁死，也禁止在Blend 0.5按Plan Sequence再启动一层Output Continuity或硬切Support identity。Promotion只在Blend完成后原子替换Plan和Support身份，不改变已经连续的Goal。若旧Plan尚未产生属于自身的完成输出，则没有需要保留的预测连续性；它必须在首次贡献Goal前以`MotionDeviationExceeded`退出，并从当前真实Sole、Support与committed trajectory原子重建Current Event计划。

同脚的下一Landing Event由Projection提前发布为`IncomingPredictedStep`。Planner必须在当前Plan进入`ApproachingContact`、Intent Revision已经结束且现有Revision槽空闲时，为仍处于PreSwing的Incoming生成唯一Event Successor。由于Planner准备发生在本帧Stance提交之前，若上一完成帧尚无真实Anchor，只允许用当前Active已经查询验证并冻结的Projected Landing Sole与Surface预建不参与Goal的geometry；事件换代提升前必须与Stance真实提交的Anchor Sole与Surface对账，失配则丢弃候选并从Committed事实重建。Intent Revision与Event Successor只可先后复用同一槽，不能并存，也不能让提前准备的Successor阻断当前Active Plan的意图修订。

Event Successor成为Current事件前只冻结geometry和时钟，绝不参与Goal或Revision Blend。事件identity换代时必须先按当前权威phase对账其冻结身体轨迹，并验证预建起点与上一完成帧Committed Anchor属于同一Surface且仍在鞋底/查询几何边界内；有效时原子提升为Active，已过期或起点失配则拒绝，并从当前真实Sole、Support与committed trajectory重新创建Current Event计划。候选查询失败可以让旧输出连续退回Original Component Pose，但不得把FadeOut变成禁止后续正式tick重试的锁；新候选成功且已验证Committed起点时必须同帧接管，不能再输出一帧旧Landing目标。新Active只按自身权威phase和`Release -> LiftOff`输出权重接管Swing，旧Landing则由同一Stance/Anchor事务接管。禁止在新事件已经进入Swing后，再按Render Delta把旧Landing世界目标与新Swing世界目标交叉混合；低帧率会把该时间权重变成单帧大位移。若Successor不可执行，不能让Current Grounding伪造预测Swing。

### 决策：Plan Goal采用单次Geometry候选和分阶段裁决

每个Presentation Completion中，Active与Revision各自至多执行一次Plan Geometry求值。该阶段只读取已冻结Plan、同帧Original Animated Foot Pose和Component Up，输出不可变`CharacterPredictiveFootGoalCandidate`：

```text
Plan Sequence
+ Ground Path / Support
+ Authored Ankle / Sole Clearance
+ Geometry Ankle / Rotation
+ Geometry typed reject reason
```

Stance观察、Approaching Contact候选、Pelvis输入和Goal合成都必须引用该同一候选。唯一Pelvis resolver完成后，只允许在该候选上追加腿长Reach裁决，产生保持相同Plan Sequence的Reach Candidate；不得重新采样Plan、Ground Path、动画Sole或鞋底净空。之后唯一Transition生成Pre-Continuity Goal，Finalizer生成最终Goal，FBBIK只消费最终Goal。

该设计不改变动作效果算法，而是先消除旧链的自相矛盾：旧实现会在Pelvis前用无限Reach得到有效目标，又在Pelvis后重新执行整条Plan求值并得到`ReachExceeded`。诊断只能看到第二次结果，无法判断错误来自Path、Pelvis还是所有权。分阶段候选使每一级输入、输出和拒绝原因都可按同一Completion对账。

业务取舍是多保存两份每脚小型不可变值，换取确定的数据血缘和可诊断性；不增加Physics Query、不增加Grounding、不改变FBBIK调用次数，也不保留旧兼容路径。

Committed Anchor只用于证明预建Successor与上一Landing事务一致，不能成为Current Event重新规划的资格。事件换代时若没有Committed Anchor，预建Successor必须拒绝；只要新事件仍处于PreSwing，Planner就在同一帧使用当前动画真实Sole、FootGrounding发布的唯一Current Support与同一committed trajectory重新执行正式Query。若该Query失败，上一完成预测修正只允许相对当前Original动画连续淡出；不得先输出一帧Grounding Baseline，也不得等待事件进入Swing后再补Plan。

Plan输出所有权起点与Ground Path几何起点是两个不同边界。Plan必须在`ReleasePhase`进入Executing，使`Release -> LiftOff`的权威约束权重真实参与旧Anchor到预测Swing的交接；Ground Path的空间进度仍在`PathStartPhase/LiftOffPhase`前保持为零。不得用`PathStartPhase`门控Plan状态，否则整段Release淡入会成为死代码，并在LiftOff首帧把预测权重直接从零切到一。

## 7. Foot Route与Ground Envelope

### 7.1 动画Foot Route

脚的平面运动来自动画与未来身体Transform：

```text
FutureFootRoute = FutureBodyTransform * RootLocalAnimationFootRoute
```

局部X、Z和旋转都必须保留。Plan的查询路线从`PathStartPhase`开始，一次Artifact平面刚性重基必须使该相位的Foot Route与当前已提交的接触点或已执行Sole正下方重合；该Artifact对齐整步冻结，不能拿当前脚逐帧改写。除此之外，执行层每帧还必须应用当前正式Root相对Expected Root的平面刚体投影，使同一不可变路线跟随真实角色位移和转向；该投影不改变Artifact语义，也不执行Physics Query。事件生成相位早于LiftOff时，不得拿更早的Native Sole去对齐更晚的路线起点。

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

`FootRate`不是动画脚到整条Virtual Ground的全局最近点。转向或路线折返时，全局最近点会让相邻Action Phase重新关联到远处Segment，即使Ground Envelope几何连续，采样进度也会跨过半条包络。正式映射必须先用权威Action Phase定位`GroundProbeRoute`的同相位局部Segment，再只在该Segment内投影Animation Foot Route，并按有序Route Fraction生成单调Foot Rate；对侧接触只增加明确的相位分段，不能参与全局最近点竞选。

Heel/Toe只使用Calibration在该Sole Pose上重建。Ground Envelope样本是当前Foot Rate处的有限高度下界；Native Sole仍位于现有SoleSupportRadius局部覆盖内时，Segment Surface法线可参与坡面净空与Foot Orientation，超出该范围后预测净空只沿Component Up比较Heel/Toe与样本高度，不得把局部斜面作为无限平面外推。Current Grounding只在Current/Stance所有权内对真实查询平面执行一次最小物理净空；Executable Swing的Plan目标不得在Revision/输出连续性之后再与Current Support取最大值。禁止第二Heel/Toe Current Query。

## 8. Constraint、Landing与Anchor

唯一状态顺序：

```text
Locked -> Sliding/Releasing -> Unlocked Swing -> Approaching -> LandingBlend -> Locked
```

- Locked：完整世界位置锁定，允许受限旋转；
- Sliding：保持唯一支撑面垂直接触，允许有限面内滑动；
- Unlocked：不消费旧Anchor，完整保留动画Swing轮廓；
- LandingBlend：使用同一冻结Landing Surface、同一Sole Pose和同一identity交给Anchor。

从运动进入`GroundedStationary`本身就是Idle锁脚的进入事件。该事件必须显式武装一次Idle Anchor捕获，不能等待Foot Placement权重先低于1；普通运行中该权重可以始终为1。若仍有运动Anchor，则先通过现有Stance淡出退出，再从Current Support约束后的安全Baseline捕获Idle Anchor，并只使用同一Anchor Blend连续淡入。

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

Landing交接只允许一层互补所有权。Anchor开始淡入后，本帧已经完成Transition与Reach裁决的预测输出是旧侧，已提交Anchor的世界Ankle Pose是新侧：

```text
LandingGoal = Lerp(ResolvedPredictiveOrTransitionGoal, CommittedAnchorGoal, AnchorBlend)
```

`AnchorBlend`同时约束Foot Goal与Pelvis支撑权重。实现不得先把Predictive权重乘以`1 - AnchorBlend`，再从Original动画重新补足剩余权重；这种组合会在部分Anchor期间形成`Predictive -> Original -> Anchor`三个owner，并把动画空中脚重新暴露到已经提交的支撑事务中。只有没有合法Plan/Transition且没有合法Stance/Anchor时，Original动画才能独占该脚。

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

Foot Feature与Biomechanical Step曲线同样属于已编译Source的不可变数据。Sequence Player创建时必须完整校验一次；运行帧只从同一normalized sample原子读取Current与Incoming Step并绑定同一source occurrence。后继事件选择、相对当前采样的Landing时间和完整路线在Artifact Build中一次确定；Runtime不得扫描曲线、缓存候选或从当前事件补建Incoming。

共享测试环境提供一条正式宽课程：30米宽、24级上楼、6米平台、24级下楼。Gameplay碰撞继续使用两条连续Traversal Ramp，Foot Placement只消费48个逐级踏面。自动源不再循环双向长路线，而是对齐起点后进入第一段楼梯，直接通过正式Input System持续循环提交`MoveAxis.x=-1/+1`的`A 1秒 -> D 2秒 -> A 1秒`压力事务。由于角色朝向和相机Basis变化后该输入的世界位移不会天然闭合，每轮压力事务结束后必须通过同一正式MoveAxis驱动角色回到压力区起点，禁止直接改Transform；回归完成才递增lap。采样路由在同一run内持续发布，CSV继续流式分块写盘；只有手动停止Play才释放虚拟输入、注销路由并封口manifest，不得在一轮事务结束时自动归零或退出。它保留实时相机Basis和LookAxis，不用世界空间横向路点抵消相机缓动。课程整体位置由场景中的唯一Course与Start/End决定，不写死世界X坐标。

课程启动门禁必须验证：唯一Course、唯一Start/End、48个踏面、A/D横向安全边界、两条Traversal Ramp和唯一Deterministic Collision World。路线阶段、正式MoveAxis、实际速度、事务分段与lap进入现有流式CSV/manifest，使输入变化与Plan Revision可以对账。每个lap都保留Step、Future Body、Ground、Path、Landing、Anchor、Pelvis与FBBIK完整因果列；持续运行通过分块写盘约束内存，不得靠删减诊断字段或自动停测控制数据量。

普通Free Play与Foot IK Automatic必须消费同一个由当前GameplayLab场景烘焙出的Deterministic Collision World。Course、Traversal Ramp、平台或场景位置发生变化后，旧Collision Artifact视为失效，必须在任何回归前从当前唯一Authoring重新烘焙；不得让Unity Physics场景已更新而Fixed KCC继续读取旧几何。World Bounds只定义允许坐标范围，不证明范围内存在可行走碰撞面。

自动writer只登记`LiveState`诊断兴趣，从完成帧流直接构造行并交给后台压缩线程；不得自动附加`RuntimeDebugSession`或启动`Continuous`内存捕获。Inspector手动Capture与自动CSV是两个观察入口，但只能消费同一完成快照，不能同时保存第二份无界帧历史。

### Artifact阶段

- 原Clip与Artifact重建路线；
- position/rotation误差；
- event、constraint、support leg、orientation、pivot；
- artifact/schema/projection identity。

### Runtime阶段

- authoritative source、event与Action Phase；
- Future Body Position/Facing/Linear/Angular路线、当前Trajectory Curvature及availability；
- Native、Predicted与Final Sole/Ankle；
- Virtual Ground、全部Capsule request/hit、法线、Edge Plane、Rejected原因；
- Reachability过滤前后点集与Ground Envelope；
- Active/Revision identity、冻结Trajectory Curvature及availability、交接位置/线速度/角速度和Blend；
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

## RENAMED Requirements

- FROM: `### Requirement: Foot Placement规划与Leg IK求解必须在Pose Graph中显式分段`
- TO: `### Requirement: 统一Foot Placement与Full Body IK必须在Pose Graph中显式分段`
- FROM: `### Requirement: Foot Placement Planner与Leg IK Solver必须使用typed目标合同`
- TO: `### Requirement: 统一Foot Placement与Full Body IK必须使用typed目标合同`
- FROM: `### Requirement: Leg IK必须保持Physical腿链长度`
- TO: `### Requirement: Full Body IK必须由成熟后端保持biped约束`
- FROM: `### Requirement: Foot Placement与Leg IK必须提供分层诊断且保持热路径有界`
- TO: `### Requirement: 统一Foot Placement与Full Body IK必须提供分层诊断且保持热路径有界`
- FROM: `### Requirement: Foot contact 必须由最终姿势运动学和表面距离判断`
- TO: `### Requirement: Foot contact必须由动作约束与世界支撑共同确认`

## MODIFIED Requirements

### Requirement: Foot Placement 必须只消费表现帧正式输入

统一Foot Placement MUST只读消费同帧Original Component Pose、权威Biomechanical Step Fact与Action Step Clock、committed Future Body Transform Trajectory、显式Foot Placement Profile、Rig v4、Calibration v4、Body Grounded事实和当前PhysicsScene。Pose、Step Fact、Clock与Foot Placement Weight MUST共享同一source sample identity、cycle、completion与Rig revision。

Foot Placement MUST不读取AnimationClip、Library Artifact、Tree、Blackboard、Timeline私有状态、原始键鼠输入、Camera角度、Visible导数、当前Transform历史或旧Projection补建缺失事实。Predictive关闭、Unavailable或Rejected时，Swing MUST保持Original Component Pose并暴露明确状态；系统 MUST不创建响应式Swing fallback。

#### Scenario: 上游Pose正在Blend

- **WHEN** Start与Loop共同形成当前Component Pose
- **THEN** Foot Placement MUST消费该完成Pose和一个权威source的完整Biomechanical Step Fact
- **AND** MUST不逐字段混合两个Landing Event或遍历source重建第二结果

#### Scenario: Projection缺少Support Leg事实

- **WHEN** 当前source payload缺少Support Weight、Leg Length或Knee Bend Plane
- **THEN** world-aware stage MUST报告typed Unavailable并阻止Predictive执行
- **AND** MUST不从当前骨骼、速度阈值或默认腿长现场补建

### Requirement: Foot contact必须由动作约束与世界支撑共同确认

每只脚的Locked、Sliding、Unlocked期望模式和连续Constraint Weight MUST来自同一个Biomechanical Step Event。FootGrounding MUST只通过唯一Current Query发布当前合法支撑平面、surface identity、距离、坡度与Body Grounded证据。Heel与Toe MUST由Calibration从同一Sole/Ankle Pose重建并只对该唯一平面计算距离；系统 MUST不增加Heel Current Query、Toe Current Query或第二Grounding。

动作约束只表达意图，不能在无支撑时创造接触；Current Support只验证世界条件，不能改变Action Step Clock或把Swing改成Locked。ApproachContact只表示接近Landing，不能用已贴地的预测Goal自证接触。

#### Scenario: in-place支撑脚相对Root快速后移

- **WHEN** Biomechanical Step声明Locked且唯一Current Support合法
- **THEN** Stance MUST允许保持接触，不得以局部平面脚速否决Locked意图
- **AND** 世界支撑失效时 MUST明确释放，不得使用默认地面

#### Scenario: Swing脚经过当前踏面上方

- **WHEN** 动作约束为Unlocked且Current Query命中脚下踏面
- **THEN** FootGrounding MAY报告只读安全平面，但 MUST不向下吸脚或捕获Anchor
- **AND** Predictive或原动画 MUST继续拥有Swing

### Requirement: Footprint prediction 必须保留动画水平脚步

PredictiveFootPlacementModifier MUST只在当前权威Landing Event进入PreSwing后创建不可变Plan。Plan MUST冻结同一Action Step的Future Body Position、Facing、Linear Velocity与Angular Velocity路线，并通过该Transform与Artifact root-local Sole、Ankle、Hip路线建立未来世界Foot Route。Foot Route MUST保留动画局部X、Z和旋转，不得使用输入幅值缩放、丢弃局部X、读取Action Motion Curve或把Foot Route当角色位移。

Plan创建帧 MAY执行一次平面刚性重基，使同相位Artifact Sole与Native Sole重合；该变换 MUST整步冻结。最终Swing Sole XZ MUST继续来自同相位Original Component Pose。计划当前样本与Native Sole的平面偏差 MUST进入有效性与诊断，不能通过冻结世界XYZ水平拉脚。

已提交Plan MUST不按Render Delta、当前Pose、Visible Root、Body Yaw、Camera变化或相邻帧速度导数逐帧改写。只有Simulation提交新的Future Body trajectory且剩余Landing位置或朝向误差超过`max(SoleSupportRadius, PathSphereRadius, SwingCapsuleRadius)`时，MAY创建一个离散后继Revision。每个不可变源Plan MUST至多尝试一次Intent Revision；Revision提升为新的Active后，新Plan MAY在committed trajectory再次越过同一几何边界时继续离散修订。每只脚同一时刻 MUST仍只有一个Active与一个过渡槽，不得逐帧改写Plan或并行执行多条Swing路径。

#### Scenario: 平地直行预测步

- **WHEN** Artifact Flat Reconstruction合法且Future Body trajectory为直行
- **THEN** 计划当前Sole样本与Native Animated Sole MUST在固定容差内匹配
- **AND** Debug Foot Route弧长、侧向轮廓与Landing距离 MUST保持原动画语义

#### Scenario: A/D圆周移动

- **WHEN** Simulation提交带有限Facing与Angular Velocity的Future Body trajectory
- **THEN** Foot、Ankle与Hip未来路线 MUST消费同一个Position与Facing函数
- **AND** Predictor MUST不使用固定零曲率、Maximum Yaw或Body Yaw猜测另一条路线

#### Scenario: camera缓动但committed trajectory未实质改变

- **WHEN** Camera或Visible Body仍变化而Landing位置和朝向误差未跨越几何阈值
- **THEN** 当前Plan MUST继续执行同一不可变几何
- **AND** MUST不创建Revision或重画一条自适应Path

#### Scenario: committed意图实质改变

- **WHEN** 新Future Body trajectory使剩余Landing误差跨越正式几何阈值
- **THEN** 后继Revision MUST从当前已执行Sole位置、线速度和Body角速度连续重基
- **AND** 新Revision未进入Executing前，仍在运动有效性边界内的旧输出 MUST保持；Intent Revision被Rejected但旧Plan已经过期时 MUST以`MotionDeviationExceeded`连续撤出
- **AND** MUST不在后继尚未有效时清空旧Plan或切换到Grounding Swing Goal

### Requirement: 地面查询必须形成有限连续 Support Envelope

统一Foot Placement MUST只有一个World Query owner。Current Query只属于FootGrounding当前支撑；Future Query只在Plan或Revision创建事务中执行一次。Future Query MUST沿Future Foot Route和权威对侧接触构成的Virtual Ground Polyline执行Capsule检测并保存位置与法线。

正式Query Mask MUST包含共享`Ground`与真实踏面`FootPlacementSurface`，并排除Gameplay专用`CharacterTraversal`。KCC Traversal Ramp、Collision Artifact support identity、隐藏Collider、默认平面和无IK脚高度 MUST不成为Current Support、Future Landing或Ground Envelope。系统 MUST不同时查询Ramp与真实踏面后按命中优先级选择。

Ground Path处理顺序 MUST固定为：

1. 按路线前后排序命中，并在同位置按高低排序；
2. 验证法线与最大坡度，建立Edge Plane；
3. 按垂直边高差、gap、鞋底范围、step与Support Leg Reach检查可达性；
4. 在Convex Hull前删除不可通行点或明确Rejected；
5. 对剩余点构造连续二维上侧Convex Hull。

Ground Envelope MUST只作为feet-only地形下界。它 MUST不携带Animation Clearance、不改变Foot XZ、不驱动Pelvis、不重参数化Action Step Clock，也不得从KCC Ramp、默认平面或无IK脚高度生成支撑。

#### Scenario: 同脚步幅跨过对侧接触

- **WHEN** 本脚下一Landing前存在权威对侧Landing
- **THEN** 对侧Sole世界姿态 MUST成为Virtual Ground精确空间顶点
- **AND** 本脚Foot Rate MUST继续由本脚动画Foot Route投影得到，不得在对侧事件phase强制跳到顶点

#### Scenario: Capsule命中台阶立面

- **WHEN** 命中法线接近竖直
- **THEN** 该命中 MUST只建立考虑SoleSupportRadius与Capsule半径的Edge Plane
- **AND** 墙面hit point MUST不直接成为可站立Surface或脚中心位置

#### Scenario: 路线包含不可达高差

- **WHEN** 任一Edge垂直变化超过Support Leg Reach或正式step边界
- **THEN** 对应点 MUST在Hull前删除或使计划以精确原因Rejected
- **AND** MUST不先构造包含该点的Envelope再在最终Goal处clamp

#### Scenario: 合法楼梯Foot Path

- **WHEN** Capsule路径取得连续合法踏面且所有边可达
- **THEN** Ground Envelope MUST形成连续分段直线上侧包络
- **AND** Scene、Game与CSV MUST从同一Plan快照显示完整路线和实际消费点

### Requirement: 每只脚必须使用有限约束生命周期

每只脚 MUST沿唯一顺序执行：

```text
Locked -> Sliding/Releasing -> Unlocked Swing -> Approaching -> LandingBlend -> Locked
```

Locked MUST保持完整世界Goal；Sliding MUST保持同一支撑面垂直接触并只允许有限面内移动；Unlocked MUST完全释放旧Anchor；LandingBlend MUST用同一冻结Landing Pose与Surface identity连续交给Anchor。Constraint Weight、Support Weight与Pivot Weight MUST来自同一Biomechanical Step Clock。

Landing MUST作为一笔事务提交Plan Landing Sole Pose、Surface identity、Anchor local point/normal、Committed Sole Pose与Successor Step Start。Successor MUST消费Stance上一完成帧真实提交的Anchor Sole与Surface，而不是重新求值Plan理论Landing。上述事实任一不一致，或Plan身体轨迹已经越过正式运动几何边界时，MUST保持未捕获或明确失败，不得换用Current Query的另一踏面。

Current Grounding spring MUST只消费Current Query与Current Sole Clearance target。动画鞋底到冻结Landing Surface的接触许可距离 MUST NOT作为逐帧增量写入该spring状态。鞋底安全 MUST在Current Grounding与Anchor混合后只执行一次最终单边平面投影，且不得建立第二套Grounding或第二时间状态。

#### Scenario: Landing接触连续保持期间

- **WHEN** 同一Landing Surface连续多帧保持接触且动画鞋底仍低于该平面
- **THEN** 每帧安全投影 MUST从当帧实际混合后的Ankle Goal重新求值
- **AND** MUST不得把完整平面修正累加到前一帧spring state，造成Current Offset逐帧增长

#### Scenario: 事件换代但后继Plan未Executing

- **WHEN** 当前事件结束而下一事件事实已出现
- **THEN** Event Successor成为Current前 MUST只保存不可变geometry和时钟，不得参与Goal或Revision Blend
- **AND** 事件identity换代时 MUST先按当前身体位置和朝向验证Successor；有效时才原子提升为Active，由新Plan自身`Release -> LiftOff`权重和旧Landing Anchor互补交接
- **AND** 已过期Successor MUST以`MotionDeviationExceeded`拒绝，并从当前真实Sole、Support与committed trajectory重新创建Current Event计划
- **AND** MUST不在新事件已经进入Swing后按Render Delta混合旧Landing世界目标与新Swing世界目标；Successor不可执行时 MUST连续退到Original Component Pose

#### Scenario: Intent Revision与Event Successor复用过渡槽

- **WHEN** 当前Swing因committed意图改变需要Intent Revision，且同脚Incoming事件也已可见
- **THEN** 当前Swing的Intent Revision MUST优先在Unsupported阶段完成
- **AND** Event Successor MUST等当前Plan进入ApproachingContact且过渡槽空闲后预建
- **AND** 两者 MUST只顺序复用同一槽，不得并存或让Successor阻断当前Swing的唯一Intent Revision

#### Scenario: Plan输出所有权先于Ground Path空间推进

- **WHEN** 权威Step位于`ReleasePhase`与`LiftOffPhase/PathStartPhase`之间
- **THEN** Plan MUST已经进入Executing并按`Release -> LiftOff`连续增加预测输出权重
- **AND** Ground Path空间进度 MUST保持为零直到`PathStartPhase`
- **AND** MUST不因几何路径尚未推进而把预测权重压为零，并在LiftOff首帧直接切到全权重

#### Scenario: Landing Surface与Current Surface不同

- **WHEN** Plan冻结Surface A而Current Query命中相邻Surface B
- **THEN** Landing MUST只校验并提交Surface A
- **AND** Surface A无效时 MUST保持未捕获或Rejected，不能静默改用Surface B

#### Scenario: 静止Idle

- **WHEN** 无权威Step且Motion Phase为GroundedStationary
- **THEN** 现有Stance owner MUST把旧运动Anchor连续释放到同帧原动画Sole经唯一Current Support约束后的安全Baseline，再捕获Idle Anchor
- **AND** MUST不捕获仍在收敛的spring current、不创建第二Anchor或永久禁止Idle锁脚

### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解

Stance Stabilization MUST是唯一Anchor、Support Leg、Pelvis与Body Pivot owner。Predictive Body Support Path MUST独立于Foot Ground Envelope，并由`last committed support -> optional opposing support -> predicted own landing`构造。它 MUST与Future Body trajectory共享Action Step Clock和Landing identity，但 MUST不复制Foot Hull或离散KCC台阶Y。

预测Hip MUST组合Body Support Path与Artifact Animation Hip Relative Route。Support Leg MUST由同一Biomechanical Step Support Weight和已提交接触选择，并使用Leg Length、Compression Reserve与Knee Bend Plane建立可达区间；上坡与下坡 MUST使用明确且不同的support policy。

Pelvis MUST先直接应用Body Support Path displacement，再使用唯一临界spring增加support-leg pull并消除bounce。Spring MUST不成为预测目标本身，不得反向扣除权威Root上移，也不得平均左右脚独立Path。结果 MUST是一个Pelvis Pre-Solve Transform。

#### Scenario: 上楼预测身体

- **WHEN** 下一合法Landing高于当前支撑
- **THEN** Body Support Path MUST连续抬升预测Hip并选择合法支撑腿
- **AND** Pelvis MUST不等待Current Grounding到达高踏面后才响应，也不得复制Foot Envelope高点

#### Scenario: 下楼预测身体

- **WHEN** 下一合法Landing低于当前支撑
- **THEN** Body Support Path与Support Leg policy MUST允许Hip连续下降
- **AND** MUST不以较高旧支撑、双脚平均或spring residual长期维持旧高度

#### Scenario: 支撑腿暂时不可达但目标可达

- **WHEN** Spring Current暂时在Leg Reach外而同一Pelvis Target仍在可达区间
- **THEN** Stance MUST保留Anchor并允许spring收敛
- **AND** 只有Current与Target共同不可达时才可沿同一Blend释放约束

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

Biomechanical Step Fact、Action Step Clock、Foot Placement Weight与生成当前Component Pose的权威source MUST使用同一effective sample time与cycle。Pose MAY连续Blend；Landing Event、Constraint、Support Leg、Orientation与Pivot MUST完整来自一个source。Presentation只能插值Simulation提交的动作时钟，不得独立累计Plan elapsed。

#### Scenario: Start进入Loop

- **WHEN** Start与Loop发生Marker同步和Pose Blend
- **THEN** 当前脚事件 MUST在LiftOff前连续转移到目标Loop并保持Cycle与Phase单调
- **AND** Stored Pose、退出source与Inertial History MUST不复活旧事件

#### Scenario: Render Frame快于Simulation Tick

- **WHEN** 两个Simulation样本之间产生多个Presentation Frame
- **THEN** Pose、Step Fact、Constraint、Support Leg与Clearance MUST保持同一动作相位
- **AND** MUST不按每帧Delta重复推进Landing或Plan

### Requirement: Body与Presentation重置必须原子清除Foot Placement历史

Body branch、Presentation reset、Rig/Projection replacement、Artifact identity变化、invalid pose或dispose MUST在下一帧前清除Plan、Revision、Ground Query、Stance、Anchor、Support Leg、Pelvis、Body Pivot与诊断快照。正常source Blend和同事件Revision交接 MUST不触发硬reset。

#### Scenario: Projection升级到新Artifact schema

- **WHEN** Runtime检测到Projection或Artifact identity变化
- **THEN** 旧Foot Placement历史 MUST整体失效并等待新正式产品
- **AND** MUST不把旧v26 Plan、Anchor或Pelvis状态迁入新schema

## ADDED Requirements

### Requirement: 统一Foot Placement必须直接生成唯一最终Goal Set

统一Foot Placement MUST从Original Component Pose、FootGrounding事实和可选Predictive输出生成一个Pelvis、Left Foot与Right Foot最终Goal Set。FootGrounding不得先发布响应式Swing Goal，Predictive不得作为第二Goal producer覆盖Current结果。Stance、Anchor、Support Leg、Orientation、Pivot与Pelvis MUST在Goal Set发布前由同一owner完成。

#### Scenario: Executable Swing Plan

- **WHEN** 一只脚处于Unlocked Swing且Plan Executing
- **THEN** Final Ankle MUST由Native Sole XZ、Ground Envelope、Animation Clearance、Sole/Ankle旋转和当前Sole-to-Ankle几何重建
- **AND** Current Grounding与冻结Query XYZ MUST不成为Swing空间基准

#### Scenario: Rejected Swing Plan

- **WHEN** Plan因Landing或Reachability失败而Rejected
- **THEN** Swing MUST保持Original Component Pose并明确报告无预测输出
- **AND** MUST不把响应式Goal、默认地面或旧Plan描述为预测结果

### Requirement: 最终Foot Motion必须组合Ground Path与动画净空

统一Foot Placement MUST使用：

```text
FutureFootRoute = FutureBodyTransform * RootLocalAnimationFootRoute
FootRate = FrozenMonotonicProjection(AnimationFootRoute, VirtualGroundRoute)
GroundHeight = Sample(GroundEnvelope, FootRate(ActionPhase))
Clearance = Sample(AnimationClearance, ActionPhase)
FinalSoleXZ = NativeAnimatedSoleXZ
FinalSoleY = GroundHeight + Clearance
```

当前动画世界Y MUST不再与预测Y逐帧取`max`。Heel/Toe只可沿Component Up执行满足唯一支撑平面的最小安全修正；该修正不得改变Foot XZ、Landing或Ground Envelope。

#### Scenario: 上楼保留动画抬脚轮廓

- **WHEN** Ground Envelope比起点高0.20m且Animation Clearance为0.10m
- **THEN** 候选Sole高度 MUST约为Ground Envelope加0.10m
- **AND** MUST不只抬到0.20m后贴着Hull滑动

#### Scenario: 下楼Ground Envelope下降

- **WHEN** FootRate沿合法包络下降
- **THEN** FinalSoleY MUST沿该包络下降并保留Animation Clearance
- **AND** Current Grounding只可执行真实当前支撑的向上物理安全修正，不得规划另一条下降路径

### Requirement: Foot Orientation与Body Pivot必须进入同一全身Goal事务

Foot Orientation MUST由Biomechanical Step Policy、冻结移动方向与Ground Path法线共同计算：上坡walking使Foot趋于水平，下坡walking使Foot趋于支撑面，running默认保留动画。Pitch与Roll MUST受同一Support Leg reach和Rig limit约束。

当Biomechanical Step提供非零Support Foot Pivot Weight且该脚Locked时，唯一Pelvis owner MUST围绕该Support Foot Pivot计算有限body/pelvis rotation；Locked Foot世界Goal MUST保持不变，Unlocked Foot MAY随身体Transform移动。该rotation MUST进入同一个Pelvis Pre-Solve Transform，不得由第二body owner或FBBIK后处理实现。

#### Scenario: Walking上坡

- **WHEN** Ground Path上升且Orientation Policy为上坡趋于水平
- **THEN** Foot rotation MUST在有限范围内趋于水平并将reach影响交给同一Pelvis owner
- **AND** MUST不简单复制踏面法线把脚尖压低

#### Scenario: Locked脚附近发生身体转向

- **WHEN** Support Foot Pivot Weight有效且身体Facing变化
- **THEN** body/pelvis rotation MUST围绕锁定脚附近pivot计算
- **AND** Locked Foot MUST不因围绕角色原点旋转而发生世界位置跳变

### Requirement: Full Body IK必须在统一Foot Placement之后保持单次成熟biped求解

FullBodyIK MUST复用FinalIK FBBIK核心数学，在同一Pending Component Pose中应用唯一Pelvis与最终Foot Goals并执行一次solve。它 MUST不查询world、不读取Biomechanical Step、不规划、不锁脚、不做Orientation或Pivot决策，也 MUST不调用FinalIK Grounding、LegIK、TwoBoneIK或第二solver。

#### Scenario: Goal连续但Solved Foot异常

- **WHEN** Final Goal有限连续而solver或physical residual超过正式容差
- **THEN** FullBodyIK MUST返回typed failure并阻断Final Pose发布
- **AND** Diagnostics MUST保留Goal、solver node与physical结果

### Requirement: 统一Foot Placement诊断必须与Full Body IK结果保持同一完成快照

每帧完成快照 MUST覆盖：

- Artifact/Projection identity与Flat Reconstruction误差摘要；
- authoritative source、Landing Event、Action Phase与Constraint；
- Future Body Position、Facing、Linear Velocity、Angular Velocity与trajectory identity；
- Native、Artifact、Predicted和Final Sole/Ankle；
- Virtual Ground、全部Capsule query/hit、法线、Edge Plane与逐原因Rejected；
- Reachability过滤前后点集、Ground Envelope、Foot Rate与实际消费点；
- Active/Revision identity、交接位置/线速度/角速度与Blend；
- Anchor、Support Leg、Body Support Path、Pelvis、Orientation与Pivot；
- FBBIK Goal、solver node、physical foot与两层residual。

Scene、Game与CSV MUST只读同一完成快照。Gizmo MUST不显示文字；Executable只画真实完整Path，Rejected只画真实query与拒绝几何，不得画伪Landing、悬空点或把Virtual Ground冒充最终脚轨迹。

#### Scenario: Debug Path与最终脚对账

- **WHEN** Scene绘制Executing Plan
- **THEN** Current Ground sample MUST等于本帧实际消费的同一Ground Envelope sample
- **AND** Final Foot XZ MUST等于同帧Native Animated Foot XZ
- **AND** Active与Revision并存时 MUST分别显示真实不可变几何与Blend，不能插值成一条不存在的Path

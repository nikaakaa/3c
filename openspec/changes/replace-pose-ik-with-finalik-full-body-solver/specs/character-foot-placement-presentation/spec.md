## RENAMED Requirements

- FROM: `### Requirement: Foot Placement规划与Leg IK求解必须在Pose Graph中显式分段`
- TO: `### Requirement: 统一Foot Placement与Full Body IK必须在Pose Graph中显式分段`
- FROM: `### Requirement: Foot Placement Planner与Leg IK Solver必须使用typed目标合同`
- TO: `### Requirement: 统一Foot Placement与Full Body IK必须使用typed目标合同`
- FROM: `### Requirement: Leg IK必须保持Physical腿链长度`
- TO: `### Requirement: Full Body IK必须由成熟后端保持biped约束`
- FROM: `### Requirement: Foot Placement与Leg IK必须提供分层诊断且保持热路径有界`
- TO: `### Requirement: 统一Foot Placement与Full Body IK必须提供分层诊断且保持热路径有界`

## MODIFIED Requirements

### Requirement: Footprint prediction 必须保留动画水平脚步

统一Foot Placement MUST在计划创建时冻结同帧committed Locomotion Intent请求平面速度、世界Root、方向、Native Sole和同相位脚骨局部姿态，并与同一权威Landing Event的root-local Foot姿态序列生成不可变世界Query Route。请求速度乘动作剩余时间 MUST表示角色沿未来地面的路程预算，不得把CharacterController碰撞后的Body水平投影或输入幅值当作第二速度模型。`constraintReleasePhase` MUST取离散Constraint从`Locked`切到非`Locked`时最近采样规则真正生效的交界，并 MUST不晚于LiftOff；`planStartPhase = max(generationPhase, constraintReleasePhase)`。

唯一Future Query owner MUST在同一个Plan创建事务中先沿覆盖路程预算的发现路线取得合法支撑并构造分段Upper Envelope，再以该Envelope三维弧长把动作路程预算反解为不可变水平Route Progress，最终生成唯一正式Query Route、Ground Path与Landing。只有正式结果 MAY提交、诊断和执行；发现阶段 MUST不成为第二Plan、第二Grounding或可执行Path。地形映射 MUST只改变Root平面位移，脚局部变化、Clearance、Constraint和Support仍按原Action Phase采样；平地映射 MUST为恒等。PreSwing计划的首点是预测Constraint Release Sole，终点 MUST是同一事件Landing。系统 MUST不读取Action Motion Curve，不把完整同脚周期重复为未来路线，提交后不得按Body速度、当前输入、当前脚或新查询重映射，也 MUST不丢弃局部X或用`FootRouteWorldAlignment`渐退掩盖不一致。

Swing最终Foot Goal的XZ与基础旋转 MUST来自当前上游原动画Component Pose；Ground Path MUST只提供高度、法线、边缘和可达性。系统 MUST不以响应式Ankle为基准只叠加预测Lift，也 MUST不把冻结Query Route完整XYZ写入最终Goal。

#### Scenario: 平地预测步

- **WHEN** Ground Path高度保持不变且计划正在执行
- **THEN** Final Foot XZ MUST保持当前原动画Pose的平面运动
- **AND** Gizmo Query Route、CSV Native Foot与Final Goal MUST来自同一完成快照且能明确区分
- **AND** Query Route长度 MUST只等于当前计划相位到Landing的剩余动作位移与root-local Foot变化

#### Scenario: 楼梯预测步

- **WHEN** 同一Foot Route经过多个合法踏面
- **THEN** Query Route MUST按冻结Upper Envelope弧长映射保持同一动作路程预算且不随帧重建
- **AND** 地形变化 MUST进入冻结水平Route Progress、Ground Path高度、支撑法线和地形修正后的预测Hip，但 MUST不改变动画局部Foot相位

### Requirement: 地面查询必须形成有限连续 Support Envelope

统一Foot Placement MUST只有一个World Query owner。Current查询只用于当前合法支撑与Stance接触证据；Future Query只允许在同一Plan创建事务内完成发现Envelope与正式Route两阶段，计划提交后不得再次查询。每个路线采样 MUST先选出与上一正式支撑连通的唯一正式支撑；相邻正式支撑之间的连续Sweep命中只有在同时可连接前后两端时才可进入Ground Envelope，近竖直命中 MUST只作为Edge Plane。查询 MUST先得到合法支撑高度，以该高度相对参考支撑的变化平移同相位预测Root/Hip，再执行相邻Step、Edge和Ankle Reach过滤。Ground高度 MUST只来自地形接触，不得被Query Route或无IK脚高度向上钳制；Reach MUST不使用尚未应用地形高度的Hip。不可通行候选 MUST在Convex Hull之前删除。

本脚前后Landing之间若存在权威对侧脚Landing，Ground Path MUST在该对侧接触Phase建立Virtual Ground分割，并对前后区间分别构造连续Upper Hull。分割点 MUST使用同一动作时钟和唯一Future Query得到的对侧支撑，在本脚Plan提交时冻结；MUST不逐帧移动、不得把对侧Landing冒充本脚终点，也不得对完整同脚步幅只做一次全局Hull。

末端Landing失败 MUST保留具体无命中或几何拒绝原因，MUST不把所有失败只表示为`NoFutureLanding`。Rejected计划 MUST不发布Executable Path或悬空Landing。

#### Scenario: 查询命中但末端不可达

- **WHEN** Future Landing查询命中几何但候选违反Reach或Step
- **THEN** 计划 MUST以具体原因Rejected
- **AND** Gizmo与CSV MUST保留真实请求、命中和拒绝几何

#### Scenario: 下楼预测Hip

- **WHEN** Future Query命中低于当前支撑的合法踏面
- **THEN** Reach MUST使用随该踏面下降的预测Hip判断候选
- **AND** MUST不因Hip仍停在上一级而把合法下楼Landing拒绝为`ReachExceeded`

#### Scenario: 下楼Query Route高于未来地面

- **WHEN** 无IK Query Route仍位于起始台阶高度而唯一连通支撑链逐级下降
- **THEN** Ground Path MUST使用各地形接触的真实下降高度
- **AND** MUST不把Query Route高度写入Ground Envelope造成末端单段瞬降

#### Scenario: 同脚步幅跨过对侧接触

- **WHEN** 本脚下一Landing之前存在身份有效且时间更早的对侧脚Landing
- **THEN** 本脚Ground Path MUST在对应动作Phase包含一个冻结Virtual Ground分割点
- **AND** 分割前后 MUST分别形成连续Upper Hull，再与本脚Animation Clearance合成最终高度
- **AND** Gizmo与CSV MUST能区分本脚最终Landing和对侧Virtual Ground分割点

### Requirement: 每只脚必须使用有限约束生命周期

每只脚 MUST只有一个`Planned / Executing / Rejected / Completed`计划生命周期和一个`Locked / Sliding / Unlocked` Stance生命周期。动作相位 MUST消费身份匹配的Simulation Locomotion Clock；Plan不得按Presentation Delta维护私有时钟。Ground Path采样进度 MUST只由该Action Step Clock在`planStartPhase -> landingPhase`区间的规范化相位产生；当前原动画鞋底、Body位移或Render Frame不得重新决定进度或逐帧重建计划。

Stance约束从`Locked`开始释放时，同一Plan MUST在相同Action Step Phase进入Executing；系统 MUST不等待更晚的LiftOff再开始计划。`Constraint Release -> LiftOff`区间 MUST始终由同一个Stance Anchor Blend与同一个Executing Plan连续交接，不得出现Anchor Blend已经归零但Plan仍为Planned的Current Grounding所有权空窗。

事件身份替换、Phase回退、动作中断或Stance捕获Landing MUST结束旧计划。相同事件不得重试或逐帧重映射。

#### Scenario: Pose Contribution被替换

- **WHEN** 当前Landing Event身份不再匹配Executing Plan
- **THEN** 旧Plan MUST在该帧结束
- **AND** Final Goal与Gizmo MUST不再消费旧路线

#### Scenario: Constraint早于LiftOff释放

- **WHEN** 烘焙Constraint按最近采样在LiftOff之前从`Locked`切到`Sliding`或`Unlocked`
- **THEN** Plan MUST在该精确交界开始Executing并从同一冻结Query Route的起点采样
- **AND** 现有Anchor MUST只通过同一个Blend连续交接，不得让Current Grounding在Release与LiftOff之间接管Swing

### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解

Stance Stabilization MUST是唯一Anchor与Pelvis owner。它 MAY消费当前支撑、预测Landing、Support Phase和Hip Route，但 MUST只输出一个Pelvis Pre-Solve Goal。地形修正后的预测Root/Hip MUST只进入该Goal的唯一目标与连续Spring；腿长Reach MUST不在Spring之后再次修改Pelvis。锁定Anchor对Spring当前值不可达时，Stance MUST释放该Anchor并沿现有Anchor Blend连续退出，不得立即Clear Anchor或把Foot Goal权重单帧归零。Predictive Plan MUST不创建第二Pelvis、第二Anchor或FBBIK后处理。

#### Scenario: Swing脚接近高踏面

- **WHEN** Executing Plan进入ApproachingContact且Landing合法
- **THEN** 未来Hip与Landing事实 MUST进入现有Pelvis与接触解析
- **AND** Landing捕获后 MUST只由同一Stance owner锁脚

#### Scenario: Anchored脚进入预测Swing

- **WHEN** Action Step Clock越过Constraint Release且同一Foot仍保留非零Anchor Blend
- **THEN** Stance MUST保留同一个Anchor并连续衰减现有Blend，不得在Release帧立即清除
- **AND** 唯一最终Foot Goal MUST按该Blend从Stance Goal连续交接到同一Executing Plan的Swing Goal

#### Scenario: 锁定支撑腿对当前Pelvis不可达

- **WHEN** 唯一Pelvis Spring当前值落在某个锁定Anchor的腿长可达区间之外
- **THEN** Reach MUST只把该Anchor转入现有连续释放
- **AND** Pelvis Resolved MUST保持等于同帧Spring Current
- **AND** Foot Goal MUST保留同帧位置与权重，并由后续Anchor Blend连续退出
- **AND** 交接 MUST不结束Plan、不创建第二Anchor，也不得退回响应式Swing目标

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

Action Step Fact MUST与生成当前Component Pose的Pose Contribution同源，原子携带Landing身份、Action Step Clock、root-local Foot、Ankle、Hip、Clearance、约束策略，以及本脚下一次Landing前的对侧Landing身份与时间，不得携带Action Root或运行速度。Locomotion Sequence、Pose、Step Fact与Clearance MUST从Simulation提交的Locomotion elapsed tick投影到同一相位；Presentation只能插值，不得独立累计第二动作时间。计划创建帧的请求平面速度 MUST来自同帧committed Locomotion Intent Fact，不得从碰撞后Body水平速度反推。Blend或source替换 MUST生成明确新身份。Virtual Ground MUST只消费本脚Action Step Fact携带的原子对侧配对，不得独立选择或混合另一只脚的事实。

#### Scenario: Blend winner改变

- **WHEN** 当前Component Pose的权威Foot Contribution改变
- **THEN** Foot Placement MUST消费新Contribution的动作事实
- **AND** 旧Contribution计划 MUST不按私有时钟继续执行

#### Scenario: Editor诊断降低Simulation推进速度

- **WHEN** 一个wall-clock区间包含的Presentation Frame多于Simulation Tick
- **THEN** Locomotion Pose、Action Step Fact与Clearance MUST仍保持同一Simulation动作相位
- **AND** MUST不因Presentation Delta累计而提前到达Landing
- **AND** Realtime、Rate Playback、Pause或Step切换 MUST以同帧已呈现Body Sample Cursor计算动画Delta，不得从`LocalLogicTick + InterpolationAlpha`建立会因Accumulator变化而倒退的第二时钟
- **AND** Committed Body Sample Cursor未前进时 MUST保持上一帧已提交Physical Pose并跳过Fact、Animation、Foot Placement与FBBIK执行，不得向Grounding传入零值、上一帧值或人为最小Delta

### Requirement: Body与Presentation重置必须原子清除Foot Placement历史

Body branch、Presentation reset、Rig/Projection replacement、invalid pose或dispose MUST在下一帧前清除Plan、Stance、Anchor、Pelvis与诊断快照。不得保留跨branch Goal或查询结果。

#### Scenario: Presentation branch替换

- **WHEN** Reset Sequence改变
- **THEN** 下一帧 MUST从新的Pose completion和动作身份建立Foot Placement状态
- **AND** FBBIK MUST不读取旧Goal Set

## ADDED Requirements

### Requirement: 统一Foot Placement必须直接生成唯一最终Goal Set

Pose Graph MUST只有一个world-aware Foot Placement owner从同一上游Component Pose生成Pelvis、Left Foot和Right Foot最终Goal。Current Grounding不得先发布Swing空间Goal再由Predictive Modifier覆盖；独立Predictive Modifier后处理节点、第二Goal链和fallback MUST不存在。

无Executable Plan时，Swing MUST保持上游动画姿势并明确报告计划不可用；Contact/Anchored脚 MAY由同一Stance owner输出约束Goal。

#### Scenario: Executing Swing Plan

- **WHEN** 一只脚处于Swing且拥有Executable Plan
- **THEN** Final Ankle MUST由当前原动画Foot XZ、Ground Path、Animation Clearance和当前动画Sole-to-Ankle几何重建
- **AND** 响应式Current Grounding与冻结Query Route XYZ MUST不作为该Swing目标的空间基准

#### Scenario: Rejected Swing Plan

- **WHEN** 一只Swing脚的计划Rejected
- **THEN** 系统 MUST不把响应式修正描述为预测结果
- **AND** 诊断 MUST明确该脚没有Executable预测输出

### Requirement: 最终Foot Motion必须组合Ground Path与动画净空

统一Foot Placement MUST按下列分工计算：

```text
PathProgress = Normalize(SimulationActionStepPhase, PlanStartPhase, LandingPhase)
FinalSoleXZ = NativeAnimatedSoleXZ
FinalSoleY = GroundPathY(PathProgress) + AnimationClearanceY(SimulationActionPhase)
```

目标Ankle MUST由该鞋底与当前动画Sole-to-Ankle几何重建。Heel/Toe MAY沿同一Component Up执行最小安全修正，但不得改变原动画Foot XZ或创建第二支撑面。

#### Scenario: 上楼保留抬脚弧线

- **WHEN** 动画净空为0.10m且Ground Path比起点高0.20m
- **THEN** 目标鞋底高度 MUST约为Ground Path加0.10m
- **AND** MUST不退化成`max(AnimationHeight, GroundHeight)`

#### Scenario: 下楼路径下降

- **WHEN** Ground Path随Progress下降
- **THEN** 目标鞋底 MUST沿该路径下降并保留动画净空
- **AND** MUST不被只允许非负Lift的逻辑阻止

### Requirement: Full Body IK必须在统一Foot Placement之后保持单次成熟biped求解

FullBodyIK MUST复用FinalIK FBBIK核心数学，在同一Pending Component Pose中应用唯一Pelvis与最终Foot Goals并执行一次solve。它 MUST不查询world、不读取Action Step、不规划、不锁脚，也 MUST不调用FinalIK Grounding、LegIK、TwoBoneIK或第二solver。

#### Scenario: Goal连续但Solved Foot异常

- **WHEN** Final Goal有限连续而solver或physical residual超过正式容差
- **THEN** FullBodyIK MUST返回typed failure并阻断Final Pose发布
- **AND** Diagnostics MUST保留Goal、solver与physical结果

### Requirement: 统一Foot Placement诊断必须与Full Body IK结果保持同一完成快照

每帧诊断 MUST覆盖动作身份与Phase、Plan状态、当前Route/Ground/Clearance采样、Stance/Pelvis、Final Goal和FBBIK结果。每计划不可变快照 MUST覆盖完整Route、Ground Path、Landing、Query requests、接受支撑和拒绝几何。

Scene、Game、CSV MUST只读同一完成快照。Executable只画真实完整Path；Rejected只画真实查询与拒绝几何；Completed或Inactive不得继续画旧Path；不得显示文字。

诊断 MUST同时保存计划创建帧committed请求平面速度、Generation/Constraint Release/Plan Start/LiftOff/Landing Phase、剩余时间、地形路程预算、冻结正式Route与预测Root位移、完整同脚周期距离、剩余Query Route距离、生成帧Native Sole、预测Plan Start Sole以及执行到Plan Start时它与同帧Native Sole的误差。发现路线被画成可执行Path、完整周期距离被误画成剩余路线、平地映射不为恒等、坡面预测Root未遵守冻结Envelope弧长映射、Plan Start执行误差不连续或`Plan=Planned && AnchorBlend=0`时 MUST是明确invalid，不得只显示一条看似合法的线。

#### Scenario: Debug Path与脚目标对账

- **WHEN** Scene或Game绘制Executing Plan
- **THEN** 当前Ground sample MUST等于统一Foot Placement实际消费的同一冻结Path sample
- **AND** Final Foot XZ MUST等于同帧Native Animated Foot XZ，诊断 MUST不把Query Route点冒充最终落脚点

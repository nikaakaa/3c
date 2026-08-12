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

因此问题不是参数不足，而是四项所有权错误：in-place移动被Action Motion Curve替换、冻结查询路线被当成最终脚轨迹、表现时钟脱离Simulation动作时钟、Reach在地形修正Hip之前执行。继续扩大Cast、放宽Reach或提高Plan接受率只会让错误计划接管更多帧。

## What Changes

- 把Foot Placement收敛为一个world-aware执行owner：同一Pose completion内依次消费权威Action Step Fact、生成不可变预测计划、查询Ground Path、解析Stance与Pelvis、发布唯一最终Goal Set。
- Current Ground Query只提供当前合法支撑、接触捕获和落地后的Stance事实；它不再先生成Swing空间目标供预测器叠加。
- Corin in-place Pose Clip只提供root-local Foot、Ankle、Hip、Clearance与接触事件；Simulation Locomotion恢复并唯一拥有作者Move Speed。预测计划在创建时只冻结同帧committed Simulation平面速度、方向和到Landing的剩余时间，不再读取或生成Action Motion位移曲线。
- 每脚事件以“上一次同姿态Landing -> 下一次同姿态Landing”定义稳定身份与时间域。计划可以在PreSwing创建，但可执行Query Route只覆盖`max(generationPhase, liftOffPhase) -> landingPhase`；每个未来点的世界位置由创建帧冻结的Simulation速度和同一in-place Pose中的脚骨局部姿态差共同预测，不读取动画位移曲线，也不得把已经过去的半个同脚周期再次画入未来路线。
- Swing脚的最终XZ与基础旋转继续来自当前上游原动画Component Pose。冻结路线不得作为世界Ankle轨迹把脚水平拉向旧计划。
- Plan以同一Simulation Action Step Phase确定性采样冻结Ground Path；当前脚世界位置不得重新决定进度、改写路线或形成逐帧自适应。最终Y等于该相位的Ground Path高度加同相位Animation Clearance。
- 动画Pose、Landing Event与Clearance只消费Simulation提交的Locomotion动作时钟；删除Sequence和Plan按`presentationDeltaSeconds`独立推进的第二时钟。
- Ground Path只拥有高度、法线、边缘和可达性。它不得改变当前动画Foot XZ，也不得按地形三维弧长重参数化动作Root。
- Future Query先建立真实支撑高度，再按该高度平移预测Root/Hip，最后执行Step、Edge和Ankle Reach过滤；同一预测Hip只输入现有Pelvis owner。
- 同一Landing Event只规划一次。事件身份替换、动作中断或Stance捕获落点会结束旧计划；已提交路线不得逐帧追随当前脚、Body速度或新查询。
- Stance Stabilization继续唯一拥有`Locked / Sliding / Unlocked`、Anchor和Pelvis；Predictive Plan只提供未来Landing、Hip与支撑相事实。
- FinalIK继续只执行一次FBBIK，不查询世界、不规划、不锁脚、不做后处理。
- 删除独立Predictive Modifier作为响应式Goal后处理的语义与旧作者配置；预测成为唯一Foot Placement owner内部的可选动作级能力。
- Scene、Game、CSV只消费同一计划快照。Rejected只显示真实查询与拒绝几何，不画伪Path或悬空Landing。

## GDC Alignment

本change只采用GDC 2016《Fitting the World: A Biomechanical Approach to Foot IK》的核心结构：

```text
Authoritative Action Step
  -> Frozen Simulation-Velocity Query Route and Landing
  -> Ground Detection along Query Route
  -> Terrain-shifted Root/Hip
  -> Reachability and Ground Path
  -> Native Animation Foot XZ + Ground Path + Animation Clearance
  -> Stance/Pelvis constraints
  -> One Full Body IK solve
```

最终鞋底运动固定为：

```text
QueryRouteXZ(phase) = FrozenWorldBinding(
    FrozenSimulationPlanarVelocity * TimeFromPlanStart(phase),
    RootLocalSoleXZ(phase))
PathProgress(t) = Normalize(SimulationActionStepPhase(t), planStartPhase, landingPhase)
FinalSoleXZ(t) = NativeAnimatedSoleXZ(t)
FinalSoleY(t) = GroundPathY(PathProgress(t)) + AnimationClearanceY(SimulationActionPhase(t))
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

- 平地Query Route起点必须是同一计划预测的LiftOff鞋底，执行到LiftOff时必须与该帧Native Sole连续；终点必须是同一事件的下一Landing。路线长度只能包含`liftOffOrGenerationPhase -> landingPhase`，但未来坐标必须从计划生成帧累积冻结Simulation位移与脚骨局部姿态差，不得把完整同脚周期或约两倍位移画成未来路线；
- Simulation Locomotion必须保持Corin正式Constant Speed，Foot Prediction必须报告计划创建帧冻结的committed Simulation速度、计划剩余时间和预测Root位移；Foot Analysis不得发布Action Root或第二速度真相；最终脚XZ保持当前原动画Pose；
- 上楼Executable Plan不再出现未解释的Goal正负往返或腿部乱飞；
- 下楼Ground Path可以下降，Final Heel/Toe没有未解释下陷；
- Rejected不是正常效果来源，稳定楼梯动作的大多数合法步必须形成Executable Plan；
- 同一计划的Landing、Query Route、Ground Path和Gizmo快照保持不变；Simulation Action Clock与原动画Pose、Clearance和Landing身份一致；
- FBBIK residual保持在正式容差内；
- Runtime/Editor编译、精确Float32与Fixed Character Build、OpenSpec strict validate、单一路径静态搜索和Unity Console检查通过；
- 固定双向短测优于本proposal中的失败基线后，才进入30分钟与8小时回归。

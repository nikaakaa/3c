# Foot IK关键经验

## 1. 四条路线不能混为一个Path

- `Future Body Trajectory`：KCC按正式世界速度段和碰撞预测身体XYZ，Body Facing独立有限追随速度方向。
- `Animation Foot Route`：in-place动画中脚相对身体的局部运动。
- `Ground Probe`：本脚Swing起点、对侧Landing、本脚Landing组成的查询折线。
- `Ground Envelope`：合法踏面形成的分段线性上包络，只是环境高度下界。

最终Foot XZ仍由当前动画Pose拥有；Ground Probe和Envelope都不能直接成为世界Ankle轨迹。

## 2. 计划只冻结当前将执行的动作

Landing身份、Action Phase、未来Body轨迹、Ground Probe、Foot Rate和Envelope必须在当前权威事件的PreSwing中一次冻结。执行期只采样，不追当前脚、不读新输入、不重查地面。

不能提前按“下一动作”冻结世界几何再晋升。该方案在台阶边缘产生约`28.8cm`脚/Path错位，Current支撑安全下界随后一次补高约`22.7cm`，正是“脚已迈出又吸到高一级”的来源。改为当前PreSwing唯一创建边界后，错位P95降到左`5.15cm`、右`7.53cm`，额外补高最大降到左`2.17cm`、右约`0.001cm`。

## 3. PreSwing锁脚与Swing路线必须同相位重基

计划可在LiftOff前创建，但脚仍由Stance锁定。Swing路线必须以创建帧锁脚Native Sole为世界起点，同时从`PathStartPhase = max(GenerationPhase, LiftOffPhase)`减去Body与局部脚基值：

```text
FootTravel = KccTravel(phase) - KccTravel(pathStartPhase)
           + RotatedLocalFoot(phase) - RotatedLocalFoot(pathStartPhase)
```

不能把生成到LiftOff的锁脚等待位移写进Swing，也不能把计划私有时钟和动画相位并行推进。高度还要在Path Start计算一次确定的连续偏移；否则PreSwing创建的Plan会在LiftOff把`Ground Envelope + Clearance`以权重1硬切进Goal。

## 4. 位移路线与身体朝向不能共用Yaw

`8b8...`证明Committed Body Yaw达到`±720°/s`时，拿它积分位移圆弧会产生约`1.05m`单帧Goal跳变。当前Simulation的真实语义是：

- Committed Movement Timeline当前段与Continuation世界速度分段积分Body平移；
- Body Facing以`Maximum Yaw Velocity`有限追随各段速度方向，对齐后停止；
- 实际YawVelocity只描述身体朝向变化，不是位移轨迹曲率。

相邻Render Frame速度导数、Camera角度和Visible Body朝向同样不能生成路线。没有独立权威曲率源时，路径曲率就是零；局部Foot、Hip、Ankle只随有限Body Facing旋转。

## 5. 对侧Landing只提供空间拓扑

对侧Landing是Ground Probe的真实空间拐点，不表示本脚在同一时刻位于该点。本脚Foot Rate必须由本脚冻结动画路线投影到整条折线并单调化，不能按对侧事件Phase强制经过拐点。

## 6. GDC高度合成只有一个结果

```text
PredictedSoleY = GroundEnvelopeY(FootRate) + AnimationClearanceY(ActionPhase)
FinalSwingSoleY = PredictedSoleY
```

当前动画只继续拥有XZ和鞋底相对踝几何，不再与预测Y逐帧取`max`。旧`max(CurrentAnimatedSoleY, PredictedSoleY)`在一次固定run中左右脚分别切换59和58次，并出现26和16次往返，会把连续路径变成两个高度owner抢脚。Current Grounding只保留为同帧真实支撑面的最终向上物理安全下界，不参与预测高度选择。

## 7. 台阶边缘必须在鞋底配置空间中建包络

Capsule Sweep命中立面时，`hit.Point`是墙面接触点，不是胶囊中心沿路线开始碰撞的位置。若直接用`hit.Point`计算Path Fraction，高踏面会延迟约一个胶囊半径才进入Envelope，脚尖已经到高踏面而Path仍在低到高的斜线中间。

边缘Fraction必须使用：

```text
SoleSupportRadius = max(planarDistance(Sole, Heel), planarDistance(Sole, Toe))
ExpandedEdgePoint = HitPoint + PlanarOutwardNormal * max(CapsuleRadius, SoleSupportRadius)
```

上楼时高包络提前到脚尖抵达边缘前，下楼时高包络保持到后跟完全越过边缘。只用Capsule半径会忽略长鞋底；`MaximumEdgeGap`又只是几何间隙，不能冒充鞋底安全范围。该处理不是固定高度补偿，而是把地形转换成实际鞋底中心可走的配置空间。

## 8. 被数据否决的方案

- 每帧重规划：会退化成响应式并在边缘反复换面。
- 下一动作世界计划晋升：动作相位尚未成为当前事实，路线会与真实脚错位。
- 全局抛物线或样条直接当最终脚轨迹：吞掉动画轮廓，端点障碍还会要求巨大过抬。
- `max`动画世界Y和预测Y：形成双高度owner和分支抖动。
- Spring平滑错误Path：只把错误延迟，随后产生穿模或更大追赶。
- 用Pelvis、Reach或FBBIK后处理掩盖路径错误：制造第二所有权。

## 9. 最小闭环

每个异常只对账：当前Landing身份与相位、冻结曲率与KCC圆弧、三点Ground Probe、单调Foot Rate、Envelope、Final Goal、Current安全下界、FBBIK结果和Heel/Toe物理距离。CSV不能只验总列数，还必须验列名唯一、左右脚字段对称以及Header/Value替换偏移一致；基础Writer扩列时必须同步耐久Writer的基础宽度、每脚宽度、序列前偏移和最终宽度，宽度正确但字段错位的数据同样作废。文档只保留会改变正式算法、诊断可信度或否决方案的证据。

## 10. 锁脚、Landing交接与骨盆是三个身份

`HasAnchor`不等于已经锁住；只有合法接触、有效Anchor和完整所有权同时成立，脚才可报告Anchored。Anchor应在当前安全Goal处原子捕获，释放时才使用既有Blend。Sliding只保留支撑面垂直接触，不等于完整世界位置锁定。

预测Body Path也不等于当前支撑腿。摆脚在Swing中必然是`Unlocked / Unsupported`，但它的下一Landing仍可驱动唯一预测Pelvis；若代码要求同一摆脚仍在Supporting才允许Body Path，预测骨盆会在最需要上楼时失效。

冻结计划不解释原始输入。键鼠和摄像机变化必须先由Simulation形成新的Committed世界速度；Foot Placement只比较同一剩余时间内“旧Plan预定位移”和“最新Timeline预定位移”。误差超过现有鞋底/查询半径才创建离散Revision，回到半阈值后才重武装。Revision从上一帧最终鞋底和同一Action Phase重基并交叉淡化；Rejected时旧预测连续退场，不能永久把脚拉回旧落点。这是动作级计划修订，不是每帧自适应。

in-place支撑脚会相对Root向后移动；用Heel/Toe全三维局部速度判断锁脚会把正常支撑长期误判为Sliding。锁定事实应来自步事件阶段：Supporting锁定、LiftOff前Releasing允许滑动、Swing解锁；运行时只验证，不修补旧烘焙。

离散所有权不能借用连续几何的25点采样格。若LiftOff位于两个样本之间，最近点采样会让真实相位已经进入Swing时仍读到`Sliding / Releasing`，Anchor交接和Predictive接管因此错开。Artifact应保存精确`Release / LiftOff / ApproachContact`边界，Runtime用同一个Action Phase直接比较；25点只负责Foot、Ankle、Hip与Clearance连续几何。

## 11. 动作所有权、Landing支撑面与Root位移不能跨时钟混用

冻结KCC轨迹是否仍有效，不能用`Action Phase`推测Simulation何时切换Motion，也不能拿台阶碰撞后的瞬时`Body.TargetVelocity`与生成帧速度比较。固定run中真实Root与冻结KCC Root仍重合，瞬时平面速度却会因逐级升降在约`6m/s`与`5.36m/s`之间变化。Visible Body朝向也不是位移轨迹切线；用两者夹角取消路线，会在实际平面位置几乎无误差时仍产生假中断。Root偏差可以进入诊断，但Plan结束只能来自权威事件生命周期。

预测Landing的位置、旋转和支撑面是同一个冻结事实。Stance交接若采用预测Ankle却用Current Grounding本帧命中的另一个Surface，Anchor会在两个台阶面之间捕获，表现为接近落地时突然跳动，或者`AnchorBlend = 0`后由响应式路径接管。`ApproachingContact`也不能再用in-place鞋底相对Root速度做Capture门禁；该速度在真实落地附近仍可达`4–6.7m/s`，会否决全部预测Anchor。冻结Surface无效或鞋底距离未通过时必须保持Swing，不能静默换成Current Surface。

权威Root竖直位移负责把整个身体带上台阶，Pelvis spring只负责支撑腿产生的附加位移。不能在Root上移时从Pelvis spring状态中减去同样高度；这会让Root已经上楼而骨盆被旧负偏移留在楼下，并造成“上楼下陷、下楼相对正常”的方向性错误。

`Ground Envelope`仍然只服务脚部净空，不能直接成为Pelvis Path。预测骨盆的输入必须来自权威Root位移、当前支撑腿和下一Landing身份；否则只是把脚部障碍包络复制成第二个骨盆owner。

## 12. 碰撞KCC轨迹不等于身体支撑坡线

Future Body Trajectory的Y已经包含KCC逐级跨台阶的碰撞结果。Query若再使用`SupportY - PlanStartSupportY`平移同相位Root/Hip，会把整段台阶高度重复叠加；正确残差只能是`SupportY - 同相位GroundProbeY`。同样，Body Support Path不能只是一个有效布尔值后继续转发原始KCC Root/Hip：那会把胶囊逐台阶离散抬升直接灌进Pelvis。正式Body Path应冻结当前支撑、可选对侧Landing和本脚Landing的支撑修正高度，按Action Step Phase分段插值Component Up；XZ仍来自同一冻结KCC，且不得消费Foot Ground Envelope。

## 13. 回退实验保留结论

本轮最终回到提交`bfb571868a58edf1b9d3c1b19844a57e4d022491`。回退不代表后续问题不存在，只表示后续多变量修改让观感持续退化，已经失去可比较的稳定基线。

GDC语义必须保持简单：动画拥有脚的平面运动和相对脚下路径的抬脚轮廓，Ground Envelope只是不允许穿过的连续下界。in-place动画不能提供角色Root位移；烘焙路线只能表达脚相对身体的动画事实，未来身体位移与旋转必须来自正式Movement Timeline和同源Future KCC，不能用输入幅值缩放动画路线，也不能丢弃动画局部X。

冻结路线必须在Plan创建帧以`GenerationPhase`的Native Sole对齐同相位烘焙样本，并整步保持同一变换；Foot Rate表示动画脚沿该路线的空间进度，Ground Probe只提供高度下界。Future Landing与普通Ground Probe的候选排序目的不同，Capsule擦到端点碰撞体或Box锐边不等于新的可站立面；起点和Landing体积内的边缘命中不得再次进入Upper Envelope，Envelope边法线也不得拥有脚掌方向。

Landing是一个完整支撑事务。旧Step的冻结Landing、本步Stance、Anchor和后继Step必须消费同一位置与支撑面；`ApproachingContact`只表示动画接近落地，不是物理接触许可。Locked固定完整世界Goal，Sliding/Releasing只保留同一支撑面的垂直所有权，Unlocked才交给Predictive Swing。Pelvis必须在唯一owner内消费最终两脚Goal，FinalIK只执行该Goal；多数坏帧是FBBIK准确执行了错误输入。

三个实验已被否决：`70c808...`在路线未闭环时开放有符号Swing高度，直接把路线误差压进地面；`7370fa...`把整个ApproachingContact当作Anchor捕获许可，产生数十厘米交接；`bf876c...`只增加下一表现帧LiftOff门禁，1495行、1205列完整采样仍表现浮空和鬼畜。平滑、迟滞和阈值不能修复错误路线、错误支撑身份或错误高度语义。

再次推进时必须一次只改一个owner：先证明冻结Animation Foot Route与同相位Native Sole的XZ映射，再证明Foot Rate，然后证明Ground Envelope端点与边缘，随后闭合Landing支撑事务，最后才恢复`Ground Envelope + Animation Clearance`的有符号高度。每一步同时看自动CSV和现场观感；编译、Character Build及Console 0 Error不代表IK效果通过。

## 14. 静止锁脚必须先更换旧步态身份

`GroundedStationary`既不能直接复用最后一步Landing Anchor，也不能永久禁止Anchor。前者会把停步姿势冻结在旧落点，后者只剩`PlantContact`而没有世界锁脚，Idle动画和Current Grounding每帧变化都会继续移动脚。正确边界是同一个Stance owner内的两段事务：旧运动Anchor先沿既有Blend退到Current Grounding安全Baseline，清零后再在该安全Goal原子捕获Idle Anchor。旧Anchor退场期间不拥有鞋底面与Pelvis Reach；Idle Anchor捕获后重新拥有同一支撑事实并保持世界Goal。这样没有第二Grounding、第二Anchor或速度权重，也不会把旧Landing直接改名成静止支撑。

Idle安全Baseline不能等同于Lyra Offset Spring的当前值。Spring Current只表示收敛过程中的瞬时输出；在它尚未到达目标时捕获Idle Anchor，会把停步残差永久冻结，看起来就是“已经锁脚但没有归位”。Idle交接必须以同帧原动画Ankle保持XZ，以唯一Current支撑面决定旋转，再只沿Component Up把Heel/Toe贴到该面；旧Anchor向这个目标淡出，新Idle Anchor也捕获同一个目标，才能同时保证连续归位和静止世界锁定。

## 15. 停止过渡不能冒充稳定Idle

`GroundedStationary`是身体运动事实，不是动画已经到达Idle的证明。若RunEnd与Idle的`presentation.foot-placement-weight`都恒为`1`，速度先归零时Stance会把RunEnd中间姿势直接捕获成Idle Anchor；之后世界锁脚工作正常，角色却永久停在错误姿势。正式归位应让RunEnd源权重为`0`、Idle为`1`，利用动画图已有过渡得到连续淡出与淡入。Runtime还必须记录本次停步确实经历过非完整权重，只在Idle重新取得完整权重后捕获；否则无Anchor的停止首帧仍可能在曲线尚未下降前误锁。该门禁描述动画所有权交接，不使用速度阈值，也不建立第二IK权重系统。

## 16. Plan不能由表现导数取消，但允许Committed意图修订

A/D时摄像机缓动会让Render Frame方向导数持续变化；`99909...csv`中10个Executable Plan因此全部提前结束，Swing Goal最大单帧跳到左`27.1cm`、右`26.6cm`，而FBBIK残差约`1e-7m`。Visible Root偏差、Body朝向和Render导数只能诊断。真正的修订证据必须是Simulation已提交的剩余世界位移发生几何上足以改变落点的变化；旧、新Plan必须各自冻结且连续交接。

## 17. Swing、Landing与Lock必须是一条所有权状态机

唯一顺序是`Locked -> Releasing -> Swing -> Landing -> Locked`。`Releasing`不能只淡出Anchor、再在LiftOff把Predictive从0硬切到1；必须直接使用资产已有`ReleasePhase -> LiftOffPhase`做SmoothStep接管。事件身份变化不能硬清仍有权重的旧Anchor；Landing接触必须用当前动画Heel/Toe到冻结Surface的真实距离，不能用已贴地的预测Goal自证。Revision未提交完不能让旧Surface先捕获，冻结Landing Target缺失也不能退回Current Surface。Capture只提交同一安全Goal，Anchor从零以SmoothStep进入，Predictive同步退出；只有接触、有效Anchor和完整Blend同时成立才是`Anchored`。新Revision不可执行时也要把旧预测连续退场，不能硬切或继续锁住错误落点。

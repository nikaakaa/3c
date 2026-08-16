# Foot IK关键经验

本文只保留会改变正式架构或否决方案的结论，不记录逐轮修补流水账。

## 1. 当前不是完整GDC数据层

现有实现已经具备单次FBBIK、当前支撑、Foot/Ankle/Hip位置路线、Clearance、Ground Envelope和诊断框架，但Artifact v26缺少：

- Sole/Ankle旋转路线；
- 支撑腿长度、压缩余量、膝盖弯曲平面和支撑权重；
- 数据定义的连续Constraint Weight；
- 上坡、下坡、跑步Foot Orientation策略；
- 真正参与求解的Support Foot Body Pivot；
- 可阻断Build的平地原动画重建误差合同。

因此当前能力只能称为“预测脚部清障基础”。在这些数据缺失时继续调runtime，不可能稳定复制GDC整体效果。

## 2. GDC的核心不是提前射线

本地原始幻灯片给出的完整关系是：

```text
Predictive Character Motion through hips
+ Animation Foot Forward Motion
+ Animation Height Above Foot Path
+ Data-defined Foot Constraint
+ Support Leg Hip Stabilization
+ Foot Orientation
+ Rotation near Contact Foot
+ Virtual Ground / Reachability / Feet-only Ground Envelope
```

最终鞋底高度是：

```text
Ground Path Height + Animation Height Above Foot Path
```

不是`max(AnimationWorldY, GroundY)`，也不是把Convex Hull直接当最终脚轨迹。

参考：`tmp/pdfs/Roche_Clifford_Fitting-the-World_GDC2016.pdf`。

## 3. 四条路线必须分开

- `Animation Foot Route`：in-place动画中脚相对角色的局部位置与旋转；
- `Future Body Transform Trajectory`：Simulation/KCC提交的角色世界Position、Facing、线速度和角速度；
- `Ground Query Route / Virtual Ground`：由未来Foot Route和对侧接触形成的查询拓扑；
- `Ground Envelope`：合法地形点形成的连续feet-only上侧包络。

它们不能互相冒充：

- Animation Route不是角色位移；
- Query Route不是最终脚轨迹；
- Ground Envelope不是Pelvis Path；
- Body Facing不是位移曲率；
- Current Grounding不是Rejected Predictive Plan的fallback。

## 4. In-place动画不提供世界位移

Corin世界移动由Simulation/KCC拥有。Artifact只保存骨骼相对Visual Root的动画事实。

已经被数据否决的做法：

- 用输入幅值缩放动画路线；
- 用Action Motion Curve或Plant轨迹重建Root位移；
- 丢弃动画局部X只保留Z；
- 用Visible Velocity、Body Yaw或相邻Render Frame导数猜未来轨迹；
- 固定零曲率后仍声称支持A/D圆周移动。

这些做法会产生第二移动距离、路线约两倍、侧向漂移或计划与角色脱节。

## 5. 必须先证明Artifact能还原原动画

在楼梯上调Path之前，先用同一Action Phase比较：

```text
Original AnimationClip Pose
vs
Artifact Reconstructed Heel/Toe/Sole/Ankle/Knee/Hip Pose
```

必须同时看位置、旋转、路线弧长、侧向范围、Landing端点和事件相位。若平地重建不一致，楼梯上的Ground Query、Foot Rate、Landing和Pelvis都没有可信输入。

当前“平地能踩到，但Path不像原动画”就是这个门禁缺失的直接表现。

## 6. 冻结计划与意图变化并不矛盾

预测必须在一个动作事务内确定，但真实输入可能改变。正确做法不是逐帧自适应，也不是永远锁死旧计划：

```text
Committed trajectory A
  -> immutable Plan A

Simulation提交实质不同的trajectory B
  -> immutable Successor Plan B
  -> 从当前已执行位置、线速度、角速度连续交接
```

每个Plan保持不可变；只有committed世界落点或朝向误差超过鞋底几何边界才创建Revision。摄像机缓动本身不是证据，必须先进入Simulation并改变正式未来身体轨迹。

最新压力采样证明，当前Revision仍有三类错误：

- 大量Executing帧的剩余落点误差已超过阈值但没有有效新计划；
- 事件换代会在后继尚未Executable时删除旧输出；
- generic `NonFinite`把任意未重写Executing帧归为数值错误，掩盖真实owner切换。

## 7. 连续曲线不能保证跨owner连续

一条Path内部连续，只能证明同一Plan的几何连续。以下边界仍可产生跳变：

- Active Plan变成Revision；
- 当前Landing Event变成下一Event；
- Predictive输出变成Grounding或原动画；
- Swing变成Landing Anchor；
- Foot Goal被Reach拒绝；
- Pelvis owner换腿；
- Body Pivot或Foot Orientation策略切换。

`foot-ik-2979d902bbc64705b95da4c9dbae2340.csv`中，左脚Executing输出曾在相邻帧出现约`-1.23m / +1.21m / -1.03m` Goal Y往返，右脚也有约`-0.52m`变化；FBBIK多数帧准确跟随Goal。问题发生在Plan/Goal owner，不是曲线绘制或solver平滑。

## 8. Ground Envelope的顺序不能颠倒

GDC顺序是：

1. Capsule检测Foot Path，取得位置与法线；
2. 按前后与高低排序；
3. 验证法线并建立Edge Plane；
4. 检查边的垂直高差和可达性；
5. 删除不可通行点；
6. 对剩余点构造连续Convex Hull；
7. Ground Envelope只服务feet。

先把不可达点放进Hull，再在最终Goal处Reach clamp，会先承诺错误路线，再让脚在最后一刻跳变或悬空。

台阶边缘必须在鞋底配置空间中处理：墙面`hit.Point`不是鞋底中心开始碰撞的位置。Edge Plane必须考虑鞋底Heel/Toe范围与Capsule半径，但不能用固定高度补偿。

## 9. Foot Lock是数据意图加世界验证

GDC的Locked、Sliding、Unlocked由动画数据定义；世界查询只验证它能否成立。

- Locked：完整世界位置锁定；
- Sliding：保持同一支撑面垂直接触，允许有限面内移动；
- Unlocked：完整释放世界锁，不让旧Anchor继续拉Swing脚。

Landing必须同时提交Plan Landing Pose、Surface identity、Anchor局部点/法线、Committed Sole Pose和后继Step起点。不能用预测Ankle配Current Query的另一踏面，也不能用已经贴地的预测Goal自证接触。

静止归位与Idle锁脚仍属于同一个Stance owner：旧运动Anchor先退到同帧原动画Sole经Current Support约束后的安全Baseline，再捕获Idle Anchor。不能捕获仍在收敛的spring current，也不能永久禁用Idle Anchor。

## 10. Pelvis必须拥有身体坡线而不是脚部包络

GDC明确要求：预测角色运动主要通过hips发生；支撑腿决定Hip高度；上坡与下坡不同；直接使用位移，spring只增加pull并消除bounce。

因此：

- Future Body Trajectory的KCC Y不是Body Support Path；
- Foot Ground Envelope不是Body Support Path；
- 左右脚各自Path不能平均成第三个Pelvis；
- 权威Root竖直位移不能再从Pelvis spring中反向扣除；
- spring current不能成为预测Pelvis目标本身。

正式身体输入应是`last support -> opposing support -> predicted landing`，再叠加动画Hip相对Body Path的运动，并通过一个支撑腿和一个Pelvis owner做reach与临界spring。

## 11. Foot Orientation与Body Pivot不是装饰字段

GDC要求：

- 上坡脚掌趋于水平；
- 下坡脚掌趋于贴坡；
- 跑步不使用该坡面orientation；
- 身体旋转支点靠近接触脚，Locked脚保持锁定且允许有限旋转。

当前binary orientation和由LiftOff推导的pivot枚举不足。若它们只进入diagnostics，转向时腿仍会被身体中心旋转拉扭，坡面脚掌也会把错误力传给hips。

## 12. 被否决的补丁方向

- 每帧重规划Landing或重投影当前脚；
- 用下一动作的旧世界计划直接晋升；
- 完整世界Route接管Foot XYZ；
- `max(CurrentAnimatedY, PredictedY)`双高度owner；
- 全局抛物线或样条替代动画脚轮廓；
- Spring、Blend、Reach或Pelvis平滑错误Path；
- 用响应式Current Goal兜底Rejected计划；
- 用FBBIK后处理追回不连续Goal；
- 增加第二Grounding、第二Pelvis或第二IK solver。

这些方案最多改变症状，不改变错误数据和所有权。

## 13. 固定验收顺序

```text
Artifact重建原动画
-> Projection事件与时钟连续
-> 平地Future Foot Route匹配Native Pose
-> A/D Future Body Transform与Revision连续
-> Ground Query与Reachability正确
-> Ground Envelope与Animation Clearance正确
-> Landing与Anchor原子交接
-> Support Leg / Pelvis / Orientation / Pivot正确
-> FBBIK residual正确
```

前一层未通过时，不修改后一层参数。编译、Character Build、Console 0 Error和CSV列数正确都不能代替运动效果闭环。

## 14. 自动输入只有穿过故障地形才是证据

曾经的自动源在角色出生点先跑1.2秒左转和右转，之后才对齐短楼梯。它能证明Input Action收到值，却不能复现“A/D上楼时Plan取消、跳变或浮空”。世界空间折线路点也不等于真实A/D：它会每帧反算Camera Basis并抵消摄像机缓动，恰好绕开自由操作中的意图变化。正式A/D回归必须在楼梯中直接提交Camera-relative `MoveAxis.x`，让相机Basis、角色Facing和Plan Revision走与键盘相同的链。

课程位置属于场景作者事实。Start/End、踏面和Traversal Ramp保持同一相对闭包时，整体平移不改变测试语义；把世界X写死会让有效的场景调整无法启动。门禁应验证唯一身份、长度、踏面数量、横向安全范围和表面闭包，而不是要求固定绝对坐标。

普通Play与自动Variant必须分开；自动源只接管MoveAxis，LookAxis继续给观察者。场景同步必须生成课程和起终点并校验Collision/Foot Surface覆盖，否则CSV运行时间没有诊断价值。

自动场中的所有Actor都属于同一Fixed世界事务。即使中立Target不参与路线，也必须出生在正式支撑面上；否则它持续下落越过World Bounds，会让玩家尚未进入楼梯时整笔世界求解失败，产生与IK无关的假失败。

虚拟Input System设备与正式Input Action在`零输入 -> 移动`或`移动 -> 零输入`边界允许出现一帧状态传播差。该帧必须把正式Action实际读到的值提交给Simulation，并在下一帧要求它收敛到虚拟设备；不能把一次传播延迟当成永久断连，也不能绕过Action直接写Simulation输入。

1217列诊断的成本主要取决于重复帧数，不取决于是否保留完整因果字段。定位A/D上楼问题时，优先运行一笔最小但完整的事务：进入第一段楼梯后执行`A 1秒 -> D 2秒 -> A 1秒`，记录完成帧后立即注销采样路由。它覆盖输入反转、Future Body Revision、Swing/Landing/Lock交接和楼梯边缘查询；双向、多场景和长时间静止只会放大文件，不能增加首个错误owner的证据质量。

采样路由的结束不能按Fixed Tick立即注销。低渲染帧率下，同一Render Frame会连续执行多个Fixed Tick；如果第一个Tick发布Complete、下一个Tick立即Remove，Presentation Writer从未观察到Complete。结束快照必须至少保持到下一个Render Frame，随后再注销，才能同时保证结束因果可见和文件停止增长。

## 15. 最小完整A/D样本已经足够定位首个错误owner

正式短测run `08522dd60084489a9f39de8e048ad700`共155行、5个流式分块，每个Header和Value均为1217列。它完整记录了对齐、接近、稳定、楼梯内`A 60 tick -> D 120 tick -> A 60 tick`和输入归零后的Complete快照；Complete后采样没有继续增长。因此后续修复不需要扩大测试时长或删减诊断列。

这份数据把首个错误owner限定在Predictive Plan Revision交接：

- 左脚在tick 360和432分别出现约17.9cm和25.8cm的单采样Goal垂直修正跳变；当时没有Anchor和Contact换代，Revision Blend为0，Ground Path支撑高度已经切到另一踏面；
- 多个Revision只经历`0 -> 约0.55`便被替换或晋升，没有保持输出位置与速度连续；
- 当前样本中的Plan结束均记为`EventReplaced`，Landing、Anchor和后继Plan没有形成完成事务；
- 大跳帧的FBBIK位置残差通常约为`1e-7m`，物理穿透接近零，说明Solver准确执行了已经跳变的输入。

固定修复顺序是：先保证Active Plan到Successor Revision的C0位置连续和C1速度连续，再收口Landing、Anchor与后继Plan的同一事务；在此之前不调Ground Envelope、Pelvis spring或FBBIK参数。

## 16. Revision连续性必须来自同一可微Step轨迹

身体转向角速度与移动轨迹曲率不是同一事实。Simulation提交的Body Yaw可在A/D反转时达到约`±720°/s`，直接把它积分为Foot Path会画出错误圆弧；正式`TrajectoryCurvature`约为`±84°/s`，接入后Revision数量与P95跳变明显下降。Pose source的逐脚权重也不能拥有Landing Event：source混合中权重短暂变成0，不代表Step身份结束。

物理Anchor释放后，Predictive恢复必须先保持上一实际Sole位置；该C0交接已经消除了`PolicyReleased`同帧的大跳。但C1不能从相邻表现帧差分，也不能在当前分段Ground Envelope外再套Hermite。实验run `a864d239f8c84f0f82759a5d43a8c93c`把修正P95恶化到左/右`18.1/17.2cm`；改用旧Plan解析切线的`b50830992f1242cf8cc00abba03503c7`仍为`15.2/15.4cm`，都差于有效基线`11.9/10.8cm`。原因是v26的Animation Clearance与Surface Envelope在段交界只有位置连续，强加速度边界会把折点放大成过冲。

因此C1的前提是同一个Biomechanical Step Artifact原子发布路线、净空、约束和支撑事实，并由Ground Path合成出一条有明确分段语义的执行轨迹。后继Revision必须从该轨迹的当前值和切线重基；不能从Final Goal历史、Visible骨骼或额外平滑器猜切线。上述两个失败实验已撤销，不保留配置或兼容分支。

## 17. Foot Route起点与净空起点不能共用一个位置

正式A/D短测已经稳定输出151帧、1217列，Header与Value等宽，覆盖输入、Action Event、Plan/Revision、完整Route/Envelope、Landing、Anchor、Pelvis、Goal与FBBIK。因此该问题不缺诊断字段，也不需要扩大采样时长。

对账发现，旧实现创建或替换Plan时混用了两个起点：

- `Animation Foot Route`按当前动画或执行Sole重基；
- `Ground Probe`从旧Anchor或旧Plan的Envelope采样点开始。

压力转向下两者XZ最大相差约80cm。随后Query用整段路线把这个差值逐渐拉回Landing，所以即使每条曲线内部连续，脚下Path、Debug线和Revision交接仍会形成长斜线与可见跳变。FinalIK只是在准确执行这个错误Goal。

正式语义必须拆开：

```text
PathStartPhase的Foot Route XZ = 已提交接触点或当前已执行Sole的XZ
Ground Probe Start = 当前已执行Sole沿Component Up投影到同一支撑面
Clearance Continuity = 当前已执行Sole相对该Ground Probe Start的高度
```

事件生成相位早于LiftOff时，不能在生成相位对齐路线、却从LiftOff开始采样；意图Revision也不能用旧Envelope上不同XZ的点作为新查询起点。对侧落点只提供Virtual Ground拓扑，不替代本脚由动画与Future Body共同形成的曲线。

另有两个实验已经由数据否决：冻结旧脚计划的世界Body Support会使支撑目标最大跳到约155cm；只修改Revision混合相位或提前结束旧事件，仍会留下约126cm的目标跳变。它们证明问题不是Blend速度，而是新旧计划的几何起点和路线语义不一致。

## 18. 可达性必须发生在完整地面采集之后

`Ground Probe Start`与冻结动画路线起点的最大偏差已从`82.4cm`降为`0cm`，但A/D反向时仍出现`FutureLandingHeightDiscontinuity`。失败帧的旧Plan落点在`Y=0`，同一输入反向后的有效后继落点在`Y=0.72m`；楼梯踏面间隔约`0.18m`，原Query却在Capsule段扫描前直接比较稀疏Sphere端点。路线一旦横跨两个踏面，`0.36m`端点差会先触发`0.35m`断裂判定，中间踏面永远没有机会进入排序与Hull。

GDC顺序要求先沿Virtual Ground收集位置、法线和Edge Plane，再按前后与高低排序，最后删除不可通行点。`MaximumHeightDiscontinuity`应判断真实边缘平面的断裂；正式支撑链使用Step Up/Down、gap与reach。提前用稀疏端点判断Height Discontinuity会把可跨越楼梯误判为悬崖，导致Revision消失；保留旧Plan也不可取，实验已使Pelvis P95从`4.95cm`恶化到`7.06cm`、最大值从`9.21cm`恶化到`13.17cm`。

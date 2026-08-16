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

`84dc902`随后尝试裁剪不可达的Capsule中间点，但错误地要求每个中间点同时一步连接前后两个正式Sphere支撑。正式支撑跨越三至四级台阶时，逐级踏面本应共同组成可达链，却会因任何单点无法直连两端而被全部删除，造成Ground Envelope缺踏面、Plan拒绝和踏空。可达性必须在排序点集上计算从当前支撑到Landing的完整有向链，只保留同时可由起点到达且可继续到终点的点；不能把多段路径压缩成每个点的双端直连测试。

## 19. 冻结Plan不能同步改写时间尺度

失败run `e7996d5c9acf4563bb8176e44c86aa7a`在frame 209触发`CharacterFutureBodyTrajectory.Evaluate`越界。右脚同一个Plan sequence 22创建时Action Step时长为`0.5167s`，运行中被同事件Clock改成`0.5471s`；Future Body轨迹仍只覆盖创建时范围，`ObserveWorldMotionDeviation`却用新时长计算旧轨迹采样时间。

这不是浮点容差、Query或FBBIK问题，而是冻结Plan内部出现两个时钟owner：路线与Future Body使用创建时长，Plan诊断与偏差检测使用运行时长。正确合同是Plan创建时原子冻结Action Step时长、Future Body时间范围和相位到秒的映射；运行时只同步权威phase。动作时长变化若足以改变Landing，应创建离散Revision并完成连续交接，不能原地修改旧Plan，也不能用clamp把越界隐藏成轨迹末端停滞。

## 20. 下一事件必须在Incoming阶段预建

run `0bbeaa210a994a1d96de17d6bec0ca2b`中右脚有`21/80`个正式路线帧没有Predictive Plan，拒绝原因为`LandingEventNotPreSwing`。frame 77至82已经连续发布下一Landing Event `8202675261206838019`，phase为`0.03125 -> 0.07911`且仍早于LiftOff；Planner却继续只维护旧Plan sequence 20。frame 83该事件成为Current时phase已为`0.09158`并越过该贡献的LiftOff `0.08824`，frame 84旧Plan被移除后Current phase已到`0.20922`，因此新Plan不再具备PreSwing创建资格，直到下一事件才恢复。

这段空窗中FBBIK没有收到预测Ground Envelope，Current Grounding只能看当前脚下支撑；上楼时脚会先穿过或踏空前方踏面，移动到踏面上方后才由鞋底安全下界迟到托起。旧的立即鞋底净空修复能阻止最终穿透，却会把离散踏面切换直接写入Goal而产生抖动；两者不能互相替代。

正式合同是：Projection提前提供Incoming事件，Planner在PreSwing内用现有Revision槽预建Event Successor，起点使用旧Plan已提交的Landing Sole与Surface；预建计划在成为Current前不输出，换代后按权威phase连续接管。不能放宽“Current过LiftOff仍可临时建Plan”，那只是把缺失预测改成迟到响应式计划。

## 21. 不踏空与不抖动不能分别靠“全收”或“全拒”

Git历史证明两种局部修法各自只解决了一半。旧Query直接采用向下Cast按距离排序的第一个合法命中；上楼时它通常是最高踏面，因此预测Plan不容易缺席，但路线轻微变化就可能把首选从下一级切到上一级，离散高度直接进入Goal后表现为抖动和跳变。`84dc902`随后删除无法同时直连前后两个正式端点的中间点，又把逐级楼梯误删成稀疏端点，形成明确踏空回归。`4882895`改为前后可达图后保住了逐级链，但正式Sphere采样仍沿用“第一个命中”语义；首选支撑与完整链冲突时，后继Plan会以`FutureLandingStepExceeded`被拒绝并淡出，脚再次退回Current Grounding。

自动课程每级实际升高约`0.18m`，正式`MaximumStepUp=0.45m`，因此正常单级台阶并未超过配置能力。问题是查询所有权而不是阈值：Physics Cast距离只表示几何命中顺序，不表示沿路线应提交哪个支撑。正式采样应以前一支撑筛出有向可达候选，并在其中选择最高踏面；随后仍由完整点集的前后可达图验证整条路线，不能跳过最终链校验。

同一Foot Rate还可能同时存在正式Sphere支撑与Capsule地面命中。高度折叠可以保留较高的安全下界，但不得丢掉正式支撑身份；否则可达图的最后一个Support不再是Landing，包络终点、Body Support终点和Plan保存的Future Support会分裂。最终三者必须从验证后可达链的同一个末端样本提交。

## 22. 单次查询内贪心选择只会转移错误

逐Sphere“从前一支撑可达的候选里立即选最高点”实验已经撤销。相同上楼区间中，它把左脚无Plan空窗从34帧降到0，却把右脚空窗从16帧增到22；右Heel浮空P95/最大值从约`0.77/1.14m`恶化到`1.03/1.27m`。这证明局部选择会改变后续Cast范围并把错误从一只脚搬到另一只脚，不能代表完整Ground Path。

正式实现必须先保留每个路线采样的全部合法Sphere命中和每段Capsule/Edge几何，再以相邻路线采样组构造从当前真实支撑到Landing的唯一有向链。只允许该链的正式支撑进入包络；Capsule几何仍只提供feet-only安全下界。Landing Surface、Body Support终点和Envelope终点必须从链末端一次提交，不能继续引用收集阶段的临时候选。

## 23. 流式CSV不能同时开启无界内存Capture

自动Foot IK writer曾在登记`LiveState`兴趣并流式写1217列CSV的同时，又自动启动`RuntimeDebugSession Continuous`。两条观察路径消费同一完成帧，但后者继续把完整帧历史保存在内存，实测即使Profiler关闭仍持续分配约10MB/帧；运行越久，GC与临时录制越重，低帧率又会改变Presentation采样密度，使IK数据失去比较价值。

正式自动采样只保留一条链：`LiveState -> Completed Frame Stream -> 后台压缩CSV`。手动Inspector Capture只能由用户显式启动，不能被自动回归隐式附加。性能验收先关闭全局Profiler，再看稳定帧时间与每帧分配；Profiler的`recording=false`不代表Profiler本身已经Disabled。

Foot IK专项Variant曾同时运行玩家与中立Target的完整Animation/PoseGraph/FBBIK，性能报告因此每个渲染帧恰好出现两次Animation事务。Target不参与路线、支撑或CSV因果链，却消耗近一半表现预算。专项Variant应只运行唯一玩家并把ActionTarget输入正式提交为`None`；普通Local Fixed仍保留Target，不能为了专项性能改变自由测试入口。

单角色后运动帧仍曾在`Animation.Prepare`消耗约60ms。直接代码审计确认，同一Source的编译后Clip Catalog被每帧重建、做重复索引检查、逐计划线性搜索，并按Catalog最大容量清权重和临时数组。Catalog合法性与Clip引用一致性是Source创建/换代合同，不是逐帧事实；运行帧只需要验证动态采样时间、权重和实际使用的Clip。正式实现应在创建时建立ClipBindingIndex到ClipState的固定索引，后续按实际激活数量更新和清理，不能用防御性全量校验替代正确生命周期所有权。

进一步审计发现，Loop Sequence为取得`IncomingPredictedStep`，曾在每个运动帧对左右脚各做最多256次完整Foot Artifact采样；每次采样又重新校验全部Foot、Ankle、Hip、Heel、Toe、旋转和支撑路线。把搜索改为每个Landing occurrence缓存一次只能解决性能，仍然让Runtime决定Artifact本应拥有的后继事件。正式所有权必须继续上移：Analyzer在同一事件分段和Action Phase域中为每个采样点同时烘焙Current与Incoming Step；Sequence Player创建时一次校验，运行帧只原子采样并绑定source occurrence。

## 24. Current与Incoming必须是同一个Artifact事实

`IncomingPredictedStep`不是Planner优化，也不是低帧率补丁。它决定下一步的Event identity、Clock、Foot Route、Landing和Biomechanical约束；若它由Runtime扫描Current曲线获得，就会与Analyzer事件分段、Projection source选择和当前sample occurrence形成第四个所有权。

正式Artifact因此升级为同时保存Current与Incoming。Incoming完整复制下一事件的路线、Heel/Toe/Ankle/Knee/Hip、旋转、Clearance、Constraint、Support Leg、Pivot与对侧Landing事实，只把Time To Landing换算为相对当前Artifact采样点的时间。Runtime不再寻找“后面第一个看起来像PreSwing的采样”，也不保留候选缓存。这样事件身份、时钟和路线在同一次Artifact采样中换代，后续才能判断Projection是否又把不同source的Current与Incoming拆开。

## 25. Marker occurrence不能再由时间距离反推

双步Artifact首次运行在Walk Loop第27帧明确失败：Incoming已经带有正确的Source Landing Cycle和Event Ordinal，Sequence Player却仍用`ContinuousTime + TimeToLanding`寻找25ms内最近的Marker。这样Artifact occurrence与Runtime连续时间各自决定一次Landing身份；同步中的预测source只要两个时间表示不完全重合，完整合法的Incoming也会被拒绝。

正式绑定直接使用`Source Landing Cycle + Event Ordinal + Foot Side`选择作者Marker occurrence，再叠加已对齐的Marker Epoch ordinal offset。`TimeToLanding`继续驱动连续时钟，但不再拥有事件身份。有限Sequence要求cycle为0；循环Sequence按source-bound cycle选择同一作者Marker。对侧Landing使用同一Owned cycle加Artifact提供的Opposing cycle offset，不做第二次时间搜索。

## 26. Projection不能把完整Artifact重新拆开

Artifact同时保存Current与Incoming仍不足以保证运行时原子性。旧Projection在两个位置再次拆分这对事实：Foot Feature Blend按两个独立score分别选择Current和Incoming；StateMachine Predictive Target保留输出source的Current，再从待切换source中挑“更早Incoming”，甚至把该Incoming单独升格为Current。这样每个子结构本身都合法，但Step Event、Clock、Route、Constraint和下一事件不再属于同一个source。

正式规则是把`Current + Incoming`当作一个Projection值。BlendSpace、TreeClip、StateMachine预取、Slot和Marker绑定都只能一次选择整对；连续Sole速度、高度和Plant Confidence仍可混合。已同步的Predictive Target即使Pose权重暂时为0，也只能整对接管事件事实，不能通过逐字段择优构造一个从未被任何Artifact发布过的Step。

## 27. Step事实不能再乘Pose贡献权重

Projection原子选择新Step后，旧实现仍用该Step所属动画贡献的逐脚Pose weight乘预测输出。目标source在权重0时已拥有事件，但Foot Placement输出仍为0；随后Pose weight上升，预测输出再次从0升到1，期间Stance、旧Plan与新Plan轮流取得脚的控制。事件换代因此被错误地执行了两次。

正式输出只消费权威Step的Release/LiftOff、Plan生命周期、Revision与Stance交接。Pose contribution weight继续作为动画混合诊断，但不再是Foot Placement所有权曲线。

## 28. Revision不能从旧Plan理论Target冒充已执行Sole

Projection与输出权重收口后，剩余大跳集中在Revision首帧：新计划从旧Plan重新求值出的理论Target开始，而不是从上一完成帧真正送入FBBIK的Final Sole开始；同一帧又立即推进Blend，低帧率时首个权重可直接达到约`0.38~0.68`。smoothstep只能平滑权重，不能消除两个目标原本已有的空间差。

正式意图Revision只消费`Last Final Sole + 同Active Plan Support`。Ground Probe是该Sole沿Component Up投影到支撑面的位置；输出身份与Active Plan不一致时不得创建Revision。创建帧Blend固定为0，从下一完成帧才推进。这个规则只闭合C0位置所有权；C1仍必须来自同一Artifact与Future Body执行轨迹的当前切线，不能重新引入表现帧差分或额外Hermite修正。

短测`dbe66bccb11143bd83c56b0141b596cc`否决了“只修Revision起点即可闭环”：楼梯A/D段左/右仍创建`26/22`次Revision，部分Intent Landing误差达到约`5~8.5m`，Goal相对Baseline的额外单帧变化仍达约`20.8/38.3cm`。同时物理穿透与FBBIK位置残差仍只有约`0.12/0.25µm`，错误继续位于Future Body与Revision输入。

## 29. Future Body曲率不能由Render Frame速度差分

旧Fact Projector先在Simulation Intent之间插值Desired Velocity，再用相邻Render Frame的插值结果除以`presentationDeltaSeconds`生成Trajectory Curvature。同一Simulation事实会因显示帧率、帧间隔和Camera-relative输入插值方式得到不同曲率；Foot Placement随后把它当成Future Body的冻结输入，导致同一步反复Revision。

正式曲率改为只使用相邻Simulation committed Intent：`SignedAngle(previous desired velocity, current desired velocity) / committed tick duration`，并由同一Motion Timeline的最大转速验证。该值在整个Simulation tick区间保持不变，Presentation不再拥有导数。瞬时反向或Timeline换代没有有限曲率时明确标为Unavailable，不用Body Yaw冒充脚下移动轨迹曲率。

短测`3fd444fe57bf4c0082252a55e506b34b`运行稳定，但不能证明曲率修复有效：楼梯A/D段仍有左/右`27/23`次Revision，Final Goal相对Baseline的额外单帧变化最大约`28.2/25.9cm`。现有1217列只记录Future Body朝向Yaw和最大转速，没有记录本次新增的平面Trajectory Curvature及availability；二者不能混为一列。补齐该输入与Plan冻结值前，不得把`±720°/s`的朝向列当作曲率证据，也不得继续修改Revision阈值。

正式诊断现已同时发布每帧committed Trajectory Curvature、其availability、Active Plan冻结曲率和冻结availability，流式CSV schema由1217列增至1225列。该改动不改变Future Body或Revision行为；只有新run证明这些字段如何换代后，才能判断是上游曲率不稳定、Unavailable被映射为零，还是Revision另有owner。

## 30. 原子Step payload不能等同于巨型值类型复制

Current与Incoming同时进入左右脚Feature后，`ActionAnimationPlaybackFrame`达到10072字节。它仍作为值类型进入托管`Dictionary`时，Unity Mono在ActionSampling首次执行泛型传参便抛出`InvalidProgramException: Passing an argument of size '10072'`；C#编译和静态校验都无法发现这个运行时边界。

正式分层是：单脚`AnimationFootFeatureSample`继续保持固定布局值类型，供NativeArray、Pose Graph和作业链使用；左右脚完整事实写入既有Action Sample Workspace的预分配Foot Feature页，Action Frame恢复为只携带leased Buffer的轻量值快照。把10KB帧直接改成class虽能绕开JIT限制，却会产生逐Action、逐表现帧的大对象分配，因此不作为正式实现保留。以后扩展Artifact时必须分别审计Native数据布局、Workspace生命周期和托管快照尺寸，不能用“原子”作为整块结构按值复制或逐帧分配的理由。

正式Workspace版本用同一自动入口完整产生run `dbbfcf471ddf4e168e4d0769e504f93f`：257行、5个压缩分块、Header/Value均为1217列，Unity Console为0 Error，未再出现第3帧ActionSampling失败或buffer lease错误。该结果只证明运行与传输合同修复，不代表IK效果完成。

## 31. Incoming事件边界不能由连续Clock代替

run `4fe6c339ed5946658dd582ec103ecaff`证明曲率不是本轮首因：运动中Current曲率仅5帧Unavailable，全部Active Plan均保存有效冻结曲率。真正的左脚断点发生在frame 236：Current仍是cycle N，Incoming却提前从N+1跨到N+2，已预建Successor因此被取消；frame 239预测输出归零，Final Goal单帧变化约`1.60m`。生成资产给出直接原因：Current的`TimeToLanding`在事件边界为阶跃，Incoming却从`0.6s`线性斜插到`1.183s`，而二者的Event Phase与Event Ordinal不足以区分同脚Loop occurrence。

此前“Marker occurrence已由Artifact拥有”的记录不完整：Marker查找虽已删除，`BindSource`仍用插值后的`sample time + TimeToLanding`反推Landing cycle。正式修复必须把`SourceLandingCycleOffset`作为Current/Incoming Artifact字段，以`cycle offset + event ordinal`切分全部事件曲线，Runtime只绑定该离散身份。右脚同run另有独立问题：Intent Revision创建失败会立即淡出唯一Active Plan；必须在事件身份修复并重新采样后单独处理，不能把两种错误混成一个平滑参数。

## 32. 弹簧不能修复同一步多次换路

修复离散Landing cycle后的run `c63017bdf0564e90aa0c0ec86dbbe29c`证明运动事件的`Previous Incoming == New Current`已经闭合，但248帧内左/右仍有`198/187`帧携带Revision，同一Landing Event内Active Plan分别换代`31/26`次。左脚Applied Lift曾单帧从`50.4cm`降到`9.2cm`，右脚Offset spring速度达到`-5.38m/s`。弹簧只是在追逐不断替换的目标，增加阻尼不能恢复确定路径。

正式Step因此每个Landing Event至多消费一笔Intent Revision事务；成功或Rejected都要等下一事件重新取得资格。Rejected Intent Revision保留原Executable Plan到正常Landing边界，不能淡出唯一输出。该规则用不超过一个剩余Step的意图响应延迟，换取GDC所需的单步确定路径；后续仍需由Simulation提交真正离散的Future Body trajectory identity，不能把这条事件边界误报为完整4.1。

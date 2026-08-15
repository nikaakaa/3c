# 设计：确定性Foot Path与单次FinalIK

## 系统边界

正式链保持：

```text
FootGrounding
    -> optional PredictiveFootPlacementModifier
    -> FinalIK FBBIK
```

Foot Placement内部只有一个Future Query、一个Stance/Anchor和一个Pelvis owner。预测层生成一个鞋底高度候选，FinalIK只执行最终Goal。不得增加第二Grounding、第二Pelvis、LegIK、TwoBoneIK、默认地面、固定高度、兼容路径或FBBIK后处理。

## GDC核心语义

预测是一次Landing Event级事务，不是逐帧向前检测：

1. 动画发布同脚Landing身份、Action Step Clock、root-local Foot/Ankle/Hip、Clearance和接触约束。
2. Simulation/KCC一次生成覆盖Landing的碰撞求解后未来Body轨迹。
3. Plan冻结Swing Foot路线、对侧Landing和本脚Landing。
4. 唯一Future Query沿冻结Ground Probe取得合法支撑并构造连续分段线性Upper Envelope。
5. 执行期只按同一Action Phase采样冻结Foot Rate和Envelope。
6. 当前动画继续拥有Foot XZ；预测只提供环境高度候选；最终Goal进入一次FBBIK。

Ground Envelope是脚不能穿过的地形下界，不是最终脚轨迹。动画抬脚弧线仍由Animation Clearance提供。

## 输入所有权

### Animation Analysis

每个Landing Event原子发布：

- 同脚前后Landing的稳定身份和Action Step Clock；
- 25点root-local Foot、Ankle、Hip与Animation Clearance连续几何；
- 精确Release、LiftOff、ApproachContact事件边界，以及由边界唯一解析的Constraint、Support、Orientation和Body Pivot；
- 期间权威对侧Landing的身份、时间和root-local落点。

Corin是in-place动画。分析产物不保存角色位移、速度或Action Motion Curve。

### Simulation/KCC

Plan创建时请求一次冻结未来Body轨迹。KCC按正式Movement Timeline、碰撞和地面约束输出XYZ位置样本。世界位移和身体朝向是两个事实：

```text
PlanarTranslation：Committed Movement Timeline当前段与Continuation的世界速度分段积分
BodyFacing：从创建朝向以MaximumYawVelocity有限追随各段世界速度方向，对齐后停止
YawVelocity：身体朝向变化诊断，不是位移轨迹曲率
```

当前Simulation把键鼠经Camera解释后的世界输入方向直接提交为世界速度，身体再独立转向该方向。因此最大转速、实际Body Yaw和相邻Render Frame方向导数都不能替代位移曲率；当前没有独立权威曲率时，路线曲率必须为零。Foot、Ankle、Hip和鞋底局部几何随有限Body Facing旋转，但KCC平移只按Committed世界速度积分。角色身体朝向与位移切线的夹角不得成为Plan失效证据。

计划提交后不逐帧重写。但真实Swing期间，Simulation可能因A/D、W/S或Camera-relative输入变化提交新的世界速度。运行时比较“当前Plan剩余预定位移”和“同一剩余时间内最新Committed Movement Timeline预定位移”；误差超过`max(SoleSupportRadius, PathSphereRadius, SwingCapsuleRadius)`才创建离散Revision，回落到半阈值后才重新武装。Revision从上一帧最终鞋底与当前权威Action Phase重基，旧、新预测修正使用现有Stance过渡速度做SmoothStep交叉淡化；新查询Rejected时旧预测修正以同一曲线退到零并结束，不得继续拉向旧Landing。Camera或Visible Pose本身不是输入，只有已经提交成世界速度的变化才能触发该事务。

## Swing Foot路线

计划生成可能早于LiftOff。生成到LiftOff期间脚由Stance锁在世界支撑点，不能把这段Body位移写入Swing路线。定义：

```text
SwingStartPhase = max(GenerationPhase, LiftOffPhase)
SwingStartSole  = 生成帧锁定的Native Sole

WorldFoot(phase) = SwingStartSole
                 + KccTravel(phase) - KccTravel(SwingStartPhase)
                 + BodyRotation(phase) * LocalFoot(phase)
                 - BodyRotation(SwingStartPhase) * LocalFoot(SwingStartPhase)
```

若计划在Swing中生成，`SwingStartPhase = GenerationPhase`，同一公式自然从当前脚开始。Root与Hip仍从Generation Phase预测，只有Swing Foot按锁脚事实重基。

Plan提交时必须在`SwingStartPhase`计算一次高度连续偏移，使`GroundEnvelope + AnimationClearance + Continuity`在LiftOff等于同一个锁脚鞋底。Predictive输出从资产正式`ReleasePhase`到`LiftOffPhase`按SmoothStep由零变为一，并与同一Stance Anchor退场权重相乘形成连续交接；该偏移只随Swing进度确定衰减，不读取当前Pose，也不把硬切交给FBBIK掩盖。

## Ground Probe与Foot Rate

Ground Probe是冻结三点折线：

```text
本脚Swing起点 -> 期间权威对侧Landing -> 本脚下一Landing
```

没有合法对侧事件时退化为起终点线段。对侧Landing由同一KCC未来位置和Body旋转还原，因此A/D时折线随圆弧形成转角；它不是把一条直线事后旋转成Debug图形。

Future Query按折线平面长度均匀采样。逐点Sphere取得踏面，相邻Capsule Sweep补齐边缘；近竖直命中只形成Edge Plane。Slope、Step、Edge、Center和Reach在Upper Hull之前过滤。

Edge Plane必须描述整个鞋底何时会碰到立面，而不是只保存墙面`hit.Point`。Plan创建时从同一Calibration重建Heel/Toe，以两者相对鞋底中点的最大平面距离得到`SoleSupportRadius`；近竖直命中沿平面外法线扩张`max(SwingCapsuleRadius, SoleSupportRadius)`后再投影为Ground Probe Fraction。这样Upper Envelope位于实际鞋底配置空间：上楼高踏面在脚尖碰撞边缘前进入包络，下楼高踏面保持到后跟越过边缘；不新增查询、配置或固定高度。

Foot Rate与查询采样分开冻结：

```text
FootRate(phase) = ProjectPlanar(FrozenAnimationFoot(phase), GroundProbePolyline)
```

投影在整条非自交折线上选择最近点，再按Action Phase做单调化。对侧Landing只塑造地面路线，不能强迫本脚在对侧脚落地的同一时刻经过折线拐点，否则会产生离散进度跳变。执行期不再从当前Pose或Root重投影。

## Goal合成

```text
PredictedSoleY = GroundEnvelopeY(FootRate(ActionPhase))
               + AnimationClearanceY(ActionPhase)

FinalSoleXZ = CurrentAnimatedSoleXZ
FinalSwingSoleY = PredictedSoleY
```

Ground Envelope与Animation Clearance共同拥有唯一Swing高度；当前动画只拥有XZ和Sole-to-Ankle几何。随后以该鞋底重建Ankle，并沿Component Up满足预测支撑面和同帧Current Grounding合法支撑面的最小Heel/Toe物理净空。Current Grounding不得参与预测高度选择或改变Landing，只能在真实当前支撑已经高于候选时做最终向上安全修正。

## Stance与Pelvis所有权

普通Current支撑只能在同帧Current Grounding证明合法支撑后捕获。Executing Plan进入`ApproachingContact`时，预测Ankle、旋转和该Plan冻结的Contact Surface属于同一个Landing事实；Stance必须重建并校验该Surface的Collider、Layer与坡度，并用当前动画Heel/Toe到该冻结平面的真实距离决定接触，不能让已贴地的预测Ankle自证接触。权威ApproachingContact不得被in-place鞋底相对Root速度否决；该速度不是世界接触速度。不得采用预测Ankle却改用Current Query的另一踏面，也不得在预测Surface无效时静默回退。Revision未完成交叉淡化时Landing事实尚未唯一提交，不得用旧Plan Surface捕获；冻结Landing Target缺失时也不得改用Current Surface。`Releasing`阶段Predictive由`ReleasePhase -> LiftOffPhase`的SmoothStep权重接管，Landing捕获只提交Anchor目标且Anchor从零按现有Blend连续进入；Predictive与Anchor权重互补，只有`PlantContact + 有效Anchor + 完整Blend`可报告`Anchored`。`Locked -> Releasing -> Swing -> Landing -> Locked`是每脚唯一所有权顺序；旧Anchor未退场时不得硬清再以全权重新捕获。

`GroundedStationary + 无权威Step`不复用最后一步Landing身份，但正常静止仍需要世界锁脚。进入该状态时，若仍有运动或Landing Anchor，现有Stance先用同一个`AnchorBlendSpeed`把它连续释放到Current Grounding安全Baseline；释放期间旧Anchor不得拥有鞋底支撑面或Pelvis Reach，也不得被直接改名为Idle支撑。该Baseline的平面位置必须来自同帧原动画Ankle，旋转来自唯一Current支撑面，并只沿Component Up把Calibration Heel/Toe贴到该面；不得把仍在收敛的Lyra Offset Spring Current捕获成Idle Anchor。

停步归位使用既有正式`presentation.foot-placement-weight`，不新增速度权重：Locomotion保持`1`，RunEnd保持`0`，动画图现有过渡负责从`1 -> 0 -> 1`连续混合到Idle。进入过停止过程的Idle支撑必须先观察到权重离开完整所有权，随后等Idle把权重恢复为`1`，才允许在同一安全Baseline原子捕获Idle Anchor；不得在速度先归零但RunEnd姿势仍参与时捕获。初始直接处于Idle时可立即捕获。Idle Anchor继续使用同一Anchor存储、支撑面、鞋底净空和Pelvis Reach，并在静止期间保持完整世界Goal；新权威Step或移动约束到来后再按原有Stance规则释放或转交。该过程不增加第二Grounding、第二Anchor owner、配置或兼容路径。

Corin locomotion是in-place：支撑脚相对Root向后运动是抵消KCC前进的动画事实，不能用该局部全速度判断世界锁定。离线Artifact从同一Plant区间提取精确`Release / LiftOff / ApproachContact`边界；Runtime按权威Action Phase唯一解析`Supporting=Locked、Releasing=Sliding、Swing=Unlocked`。连续几何仍使用25点路线，离散所有权状态不得再量化到该采样格。旧Artifact通过算法身份变更整体失效，不保留兼容改写。

Predictive Body Support Path描述下一Landing对身体的未来地形位移，它与“该摆脚当前是否Supporting”是两个事实。Executable Plan进入Swing后，即使同一脚是`Unlocked / Unsupported`，其Body Support Path仍可作为唯一Pelvis owner候选；真实支撑腿身份继续只拥有Current支撑、Anchor与Reach。

Future Body Trajectory已经是碰撞求解后的KCC XYZ。Future Query对正式支撑只能应用`SupportY - 同相位GroundProbeY`残差，不能再把`SupportY - PlanStartSupportY`叠加到KCC Root/Hip，否则上楼高度会被重复计算。Executable Plan必须冻结当前支撑、可选对侧Landing和本脚Landing对应的支撑修正Root/Hip；执行时平面位置继续来自同一冻结KCC轨迹，Component Up高度按Action Step Phase在这些锚点间分段插值。Foot Ground Envelope仍只服务脚部，不参与该身体坡线。

权威Pose Root的世界竖直位移必须直接带动身体；Pelvis Spring只能输出Current支撑腿与选中Body Support Path要求的附加偏移。禁止在Root上移时从Spring Current中减去同样高度，也禁止用Foot Ground Envelope驱动骨盆。前者会把已经上楼的Root与仍滞留楼下的Pelvis人为分离，形成只在上坡出现的持续负偏移。

## 已验证约束

- 把冻结Query Route完整XYZ写入Goal会产生约15–18cm的XZ偏差和接近49cm的切换跳变；最终XZ必须保持动画所有权。
- `max(CurrentAnimatedSoleY, PredictedSoleY)`在v88固定楼梯run中左右脚分别切换高度分支59和58次，并出现26和16次往返；这会让连续Ground Path变成两个高度owner抢脚，必须删除该分支。
- 把对侧高度放到“该相位的本脚位置”会让真实对侧点与查询拐点错开约0.59–0.81m；对侧Landing必须是Ground Probe精确顶点。
- 强制本脚在对侧事件相位穿过该顶点会让Foot Rate离散跳约20%；必须做整条折线最近投影。
- LiftOff前不重基会让首步Foot Rate跳约18–33%；按Stance锁脚点重基后，固定双向楼梯run降到左约8.1%、右约7.4%。
- 单一全局抛物线为覆盖靠近路线端点的楼梯边缘，需要约6–18m拱高；不能替代分段线性Upper Envelope。
- incoming世界计划晋升会让脚与Path在台阶边缘错位约28.8cm，并触发约22.7cm当前支撑补高；改为当前PreSwing唯一创建后，错位P95降到左约5.15cm、右约7.53cm，额外补高最大降到左约2.17cm、右约0.001cm。
- v89去掉双高度分支后，两个边缘帧在鞋底/Path XZ仅差约0.1–0.6cm时仍需Current支撑补高约12.6–13.1cm。对应Envelope把`0.24m -> 0.477m`高差从墙面接触点之后继续线性插值，证明边缘Fraction使用了墙面点而不是胶囊中心接触点。
- v90固定run保持1207列旧合同，v92删除无执行语义的Incoming Plan字段后保持1189列历史合同；两者均无超过1mm的Heel/Toe物理穿透，且有效权重帧FBBIK residual接近0。回退后的`99909...csv`实际基础Writer为1101列；本轮加入每脚冻结Movement Yaw与最终路线Yaw后，基础Writer合同为1105列，加入完整序列与因果字段的耐久Writer合同为1203列，必须重新验证列名唯一、左右对称、Header/Value等宽和序列替换偏移。
- c190固定run在恒定满输入下仍有26次`ActionInterrupted`，原因是用Action Phase猜Simulation Continuation边界；同一run上坡时Pelvis Target已为0而Current长期约`-0.195m`，证明旧Root向上换基在抵消权威身体上移。两者分别归属于Motion身份检查和Pelvis Spring基准，不是FBBIK或阈值问题。
- d15固定run删除Root向上换基后，上坡且Pelvis Target接近0的Current中位由约`-0.193m`变为约`+0.0005m`。但主路线仍在真实Root与冻结KCC Root重合时因`Body.TargetVelocity`随台阶升降变化而产生33次假中断；全部ApproachingContact也因in-place局部脚速`4–6.7m/s`停在`WaitingForCaptureSpeed`。因此Plan有效性必须比较同相位Root状态，预测Landing捕获必须相信权威事件、冻结Surface和几何距离。
- `99909...csv`共240帧、1101列，左右脚10个Executable Plan全部以`ActionInterrupted`提前结束，Predictive Final Source只覆盖左5帧、右19帧；结束后Swing Goal最大单帧位移为左27.1cm、右26.6cm，而FBBIK位置残差约`1e-7m`。其中多次中断的平面位置偏差接近0，主要误差来自把Visible Body朝向与冻结轨迹切线比较；另一些计划的冻结曲率与实际Root转向反号，证明相邻表现帧速度导数既不能生成稳定A/D路线，也不能取消本步。

## 尚未闭合

- 连续A/D圆周运动已经证明不能使用Movement节点`720°/s`最大转向能力，也不能使用相邻Render Frame速度方向导数。当前改为同Tick Committed Movement Timeline实际YawVelocity后，仍需复核KCC圆弧、三点Ground Probe和Animation Foot Route的同计划一致性。
- Start、Loop、Stop、MovingTurn的当前Landing身份仍需统一；Foot Placement不再保存或晋升incoming世界计划。
- 固定采样以66.7ms推进一帧，一帧可能跨过一级台阶；它适合因果对账，不能代替正常帧率下的视觉连续性验收。

## 已否决方向

- 逐帧重算Landing、Path或当前脚投影：会退化成响应式并在边缘A-B-A切换。
- 为尚未成为当前动作的incoming事件冻结世界路线并在下一动作晋升：会把旧动作相位和旧空间起点带入新Swing。
- Visible Velocity、输入幅值或Action Motion Curve：会形成第二移动距离。
- Visible Body朝向与冻结路线切线的夹角：两者不是同一物理量，会把正常转弯判为动作中断。
- 对侧Landing只提供高度、不进入Ground Probe：会在错误XZ提前切换台阶。
- 对侧事件相位强制本脚经过拐点：混淆两只脚的时间与空间事实。
- 全局抛物线/样条直接充当最终脚轨迹：端点障碍需要巨大过抬，且吞掉动画抬脚轮廓。
- 当前动画世界Y与预测世界Y逐帧取`max`：会形成双高度owner和分支往返。
- 用Pelvis、Reach、Spring或FBBIK后处理掩盖错误Path：会产生第二所有权。

## 验收口径

编译、Build和Console 0 Error只代表工程门禁。效果完成还要求：

- 平地、上下楼与连续A/D都有身份稳定的Executable Plan；
- 同一Plan Revision的KCC平移、Body Facing、Ground Probe、Landing、Foot Rate和Envelope不变；Revision交接必须由Sequence、误差、阈值和Blend完整解释；
- LiftOff处冻结Swing路线从锁脚点连续开始；
- Foot Rate单调且不存在未解释的大跨度；
- 上楼Goal无边缘正负往返，下楼Heel/Toe无下陷或浮空；
- 只有Final Goal连续安全而Solver结果异常时才归责FBBIK。

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

统一Foot Placement MUST在当前权威Landing事件进入PreSwing后请求并冻结覆盖Landing的Simulation/KCC未来可执行Body XYZ轨迹，同时冻结世界Root、Native Sole、同相位脚骨局部姿态、Movement最大转向能力和实际Trajectory Curvature。KCC轨迹 MUST消费正式Movement Timeline并执行与运行时同源的世界碰撞；Trajectory Curvature MUST来自连续正式平面速度方向的有符号变化率，并同时驱动KCC未来圆弧以及Foot、Hip、Ankle和Sole-to-Ankle的root-local几何旋转。Movement最大转向能力 MUST只验证方向变化是否连续，不得作为路线曲率。系统 MUST不使用包含Presentation插值或纠错的Visible Velocity，不得用输入幅值建立第二速度模型，也不得把Body朝向与移动方向的单帧夹角写回路线。`planGenerationPhase = generationPhase`，`swingPathStartPhase = max(generationPhase, liftOffPhase)`；Swing Foot Route MUST以生成帧Native Sole和同一Generation Phase的KCC/root-local Foot共同重基。`Locked / Sliding`期间 MUST由同一Stance owner拥有脚部输出，Predictive MUST只在真实LiftOff后通过既有Anchor Blend连续接管。

计划创建使用的root-local Foot、Ankle、Hip、平面路线和Animation Clearance MUST直接消费同一原子事件的25点Action Phase路线，不得降采样到7点或用当前Pose逐帧修补烘焙误差。Foot Rate投影的输入路线若不能保留原动画落地前回摆，计划 MUST在进入Future Query之前视为无效输入，不能让错误水平位置读取Ground Envelope高度。

唯一Future Query owner MUST在同一个Plan创建事务中沿当前Swing接触、权威对侧Landing和本脚Landing组成的非自交Ground Probe Polyline取得合法支撑并构造唯一连续Upper Envelope。查询采样与Foot Rate MUST是两个冻结结果：查询按Polyline平面长度采样；Foot Rate把同一冻结动画脚路线按各Action Phase投影到整条Polyline的最近平面点，再按Action Phase单调化。对侧Landing MUST只成为Polyline空间顶点，不得按其事件Phase强迫本脚Foot Rate经过该顶点。近竖直边缘 MUST使用同一Calibration Heel/Toe相对鞋底中点的最大平面范围与Swing Capsule半径中的较大值，把墙面接触点扩张到鞋底中心接触位置后再生成Edge Fraction；`MaximumEdgeGap`不得被解释为鞋底安全边距。Foot Rate MUST在`generationPhase -> swingPathStartPhase`保持0，并把`swingPathStartPhase -> Landing`映射为`0 -> 1`。无法形成正向有效区间的计划 MUST在Commit前以typed原因Rejected。执行期Action Phase只采样该冻结Foot Rate，不得以当前Pose、当前Root、Body速度、当前输入或新查询反推Ground Path Progress。计划进入真实LiftOff后，运行时 MUST按同一Action Phase求值冻结KCC Root位置与朝向，并将当前Root的平面位置偏差及剩余路线上的朝向偏差换算为Landing平面误差；只有总误差超过现有鞋底查询半径时才可结束计划。系统 MUST不以Action Phase猜测Simulation段切换，不得以台阶碰撞后瞬时`Body.TargetVelocity`变化或Motion Generation变化单独结束计划。Animation Clearance、Constraint与Support MUST继续直接按权威Action Phase采样；Ground Envelope MUST只作为地形下界，不得以自身三维弧长改变动作时钟。

Swing最终Foot Goal的XZ与基础旋转 MUST来自当前上游原动画Component Pose；Ground Path MUST只提供高度、法线、边缘和可达性。唯一预测鞋底高度 MUST等于`GroundPathY + AnimationClearanceY`，不得再与当前动画完整鞋底世界Y逐帧取较高者或形成第二高度owner。系统 MUST不把冻结Query Route完整XYZ写入最终Goal。随后系统 MUST同时验证预测Path支撑面和同帧Current Grounding已经命中的合法鞋底支撑面，并只沿Component Up应用满足两者所需的最大最小物理净空平移；Current Grounding不得参与预测高度选择、移动Landing或重建Path。

#### Scenario: 平地预测步

- **WHEN** Ground Path高度保持不变且计划正在执行
- **THEN** Final Foot XZ MUST保持当前原动画Pose的平面运动
- **AND** Gizmo Query Route、CSV Native Foot与Final Goal MUST来自同一完成快照且能明确区分
- **AND** CSV MUST记录该完成快照的实际PoseRoot世界位置与旋转，使Native Foot和Query Path能相对各自Root按同一坐标语义对账
- **AND** Query Route长度 MUST只等于当前计划相位到Landing的剩余动作位移与root-local Foot变化

#### Scenario: 楼梯预测步

- **WHEN** 同一Ground Probe经过多个合法踏面
- **THEN** Ground Probe、Ground Envelope、动画脚路线与Foot Rate MUST在Plan创建后保持不变
- **AND** Ground Path Progress MUST由冻结Foot Rate单调推进，Action Progress、Animation Clearance、Constraint与Support MUST仍由同一Action Phase采样
- **AND** 接触期Ground Path Progress MUST保持0，LiftOff后的最终鞋底高度 MUST等于`GroundPathY(FootRate) + AnimationClearanceY(ActionPhase)`并继续服从唯一Current Grounding安全下界

#### Scenario: LiftOff边界的空间进度连续

- **WHEN** Plan在LiftOff前已冻结且KCC Body在生成相位到LiftOff之间已经移动
- **THEN** LiftOff对应的Foot Rate MUST仍为0，首个后LiftOff样本 MUST从该摆动起点连续增加
- **AND** Swing Foot Route在LiftOff MUST等于Stance锁定的Native Sole，生成相位到LiftOff的Body位移与绝对投影距离 MUST不进入Swing Ground Path Progress
- **AND** 若LiftOff到Landing无法形成正向空间区间，Plan MUST以`FootRateInvalid`结束为Rejected且不得进入FBBIK输入

#### Scenario: 角色在下一步内转向

- **WHEN** 角色以A/D持续输入形成稳定圆周运动
- **THEN** 正式Query Route MUST沿冻结Trajectory Curvature同时积分KCC位置圆弧并旋转root-local Foot、Hip与Ankle
- **AND** Native Foot与Query Path相对各自Root的运动方向 MUST保持一致
- **AND** 计划不得使用Movement最大转向能力替代实际曲率、逐帧读取当前Transform重写朝向或继续沿生成帧旧切线查询脚不会经过的地面

#### Scenario: 输入变化仍在冻结Landing容差内

- **WHEN** committed Body速度或Trajectory Curvature相对计划创建帧发生变化，但按剩余步时积分后的Landing平面误差仍不超过现有鞋底查询半径
- **THEN** 当前Plan MUST继续按原Action Step Clock采样同一冻结Route、Foot Rate与Envelope
- **AND** 系统 MUST不因Desired Input为零、转向布尔变化或角速度符号测试结束计划
- **AND** Action Phase跨过某个比例、Motion Generation变化或台阶碰撞改变瞬时Target Velocity MUST不被单独解释为输入已经改变
- **AND** 当前Root仍与同相位冻结KCC位置/朝向保持在现有鞋底查询半径内时，计划 MUST继续执行
- **AND** 只有积分误差越过该物理容差时才可用typed `ActionInterrupted`结束计划，不得原地重规划

### Requirement: 地面查询必须形成有限连续 Support Envelope

统一Foot Placement MUST只有一个World Query owner。Current查询只用于当前合法支撑与Stance接触证据；Future Query只允许在同一Plan创建事务内完成发现Envelope与正式Route两阶段，计划提交后不得再次查询。Future Query逐点Sphere与相邻点Capsule的向下Sweep顶面 MUST不低于同相位预测Hip高度；Foot Route加固定`CastAbove`只能形成更高的覆盖，MUST不限制骨盆工作区内未来踏面的发现。每个路线采样 MUST先选出与上一正式支撑连通的唯一正式支撑；相邻正式支撑之间的连续Sweep命中只有在同时可连接前后两端时才可进入Ground Envelope，近竖直命中 MUST只作为Edge Plane。查询 MUST先得到合法支撑高度，并只以`合法支撑高度 - 同相位Ground Probe高度`残差平移同相位预测Root/Hip，再执行相邻Step、Edge和Ankle Reach过滤；碰撞求解后的Future Body XYZ已经包含台阶位移，MUST不得再叠加相对计划起点的整段地形高度。每个正式支撑的同采样Root/Hip残差 MUST进入冻结Body Support Path，后续支撑不得被重设为新的零高度参考。Ground高度 MUST只来自地形接触，不得被Query Route或无IK脚高度向上钳制；Reach MUST不使用尚未应用地形残差的Hip。不可通行候选 MUST在Convex Hull之前删除。

本脚前后Landing之间若存在权威对侧脚Landing，Future Query MUST以Swing起点、该对侧接触空间点和本脚预测Landing构造按平面弧长采样的分段直线Ground Probe Polyline。对侧点 MUST以本脚原子事件携带的对侧root-local落点和Phase，经同一冻结KCC位置轨迹与Trajectory Curvature还原并成为Polyline精确顶点；MUST不使用“该Phase上的本脚位置”、不得逐帧移动、不得把对侧Landing冒充本脚终点。逐点Sphere和相邻Capsule Sweep MUST只消费该Polyline。Capsule Sweep命中近竖直Edge Plane时，Edge Fraction MUST以`Hit Point + 平面外法线 * Swing Capsule Radius`对应的胶囊中心接触位置投影，MUST不把墙面接触点直接当作脚中心路径位置。删除不可通行点后，完整同脚事件的全部合法样本 MUST共同构造一次连续Upper Hull；对侧样本只有位于上包络时 MAY成为Ground Envelope顶点，MUST不作为强制断点分别构造两个Hull。

末端Landing失败 MUST保留具体无命中或几何拒绝原因，MUST不把所有失败只表示为`NoFutureLanding`。Rejected计划 MUST不发布Executable Path或悬空Landing。

#### Scenario: 查询命中但末端不可达

- **WHEN** Future Landing查询命中几何但候选违反Reach或Step
- **THEN** 计划 MUST以具体原因Rejected
- **AND** Gizmo与CSV MUST保留真实请求、命中和拒绝几何

#### Scenario: 上楼踏面高于无IK Foot Route扫描顶面

- **WHEN** 未来合法踏面高于`Foot Route + CastAbove`但仍低于同相位预测Hip
- **THEN** 唯一Future Query MUST从预测Hip高度向下发现该踏面
- **AND** 系统 MUST接受它或记录Step、Edge、Reach等明确拒绝原因，不得穿过楼梯后把底层地板作为无拒绝的Executable Ground Path

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
- **THEN** 本脚Ground Probe MUST以对侧真实空间落点作为冻结折线顶点
- **AND** 本脚Foot Rate MUST继续由本脚动画位置对整条折线做最近投影，不得在对侧动作Phase强制跳到该顶点
- **AND** 分割前后的合法样本 MUST共同形成覆盖完整同脚事件的一次连续Upper Hull，再与本脚Animation Clearance合成最终高度
- **AND** Gizmo与CSV MUST能区分本脚最终Landing和对侧Virtual Ground分割点
- **AND** CSV MUST直接保存对侧世界落点、Ground Probe顶点、动作Phase与两者平面误差

### Requirement: 每只脚必须使用有限约束生命周期

每只脚在任一时刻 MUST只有一个`Planned / Executing / Rejected / Completed`当前计划拥有输出，并只有一个`Locked / Sliding / Unlocked` Stance生命周期。Foot Placement MUST只为已经成为当前离散事实且处于PreSwing的权威Landing创建世界计划；它 MUST不保存、查询或晋升incoming世界候选。Start、Loop、Stop和MovingTurn若要预测，动画事务 MUST在LiftOff前把对应Landing发布为当前权威PreSwing事实，禁止Foot Placement在LiftOff后按当前Pose补造事件。Plan不得按Presentation Delta维护私有时钟。Action Step Clock MUST唯一拥有Landing身份、Action Progress、Animation Clearance、Constraint与Support；Ground Path Progress MUST只由同一Plan创建时冻结的Foot Rate映射。当前Body世界位移、当前鞋底、Render Frame和私有Elapsed不得推进计划；运行时空间投影不得重建路线、移动Landing或触发新查询。

PreSwing的`Locked / Sliding`区间 MUST由同一个Current Support/Stance安全目标完全拥有脚，Ground Path Progress MUST保持0且Predictive权重 MUST为0。Plan提交时 MUST以`Swing Path Start`锁脚鞋底计算一次确定的Animation Clearance连续偏移；真实LiftOff之后Predictive权重 MUST为1，且首个预测Goal MUST与同一个Stance Goal位置连续。若仍有非零Anchor Blend，最终Goal MUST继续按该Blend从Stance连续交接到预测Swing。Current Contact不得推进Foot Rate、重建Plan或成为第二时钟。

`Sliding` MUST只解除支撑面内的位置锁定，MUST不解除鞋底的垂直接触。权威`Locked / Sliding + Supporting / Releasing`已经由同帧Current Grounding合法支撑和距离确认接触后，Stance MUST用Heel/Toe到该唯一支撑面的有符号最小Component-Up平移修正同一个Foot Offset状态；向上防穿透和向下消除浮空 MUST使用同一几何约束。该接触约束 MUST不新增查询、Anchor、平滑器、参数、固定高度或FBBIK后处理，且 MUST不在`Unlocked` Swing期间向下吸脚。

#### Scenario: Sliding接触脚沿支撑面移动

- **WHEN** 动作约束为`Sliding + Supporting/Releasing`、同帧Current Grounding支撑合法且鞋底位于该面上方
- **THEN** Stance MUST保持面内滑动语义并沿Component Up把Heel或Toe中较低者约束到该支撑面
- **AND** 同一个Foot Offset连续状态 MUST同步到修正后位置，FBBIK MUST只执行该最终Goal
- **AND** 系统 MUST不等待Swing用Grounding Spring跨多个接触帧回落

当本帧没有权威Landing Event且Plan为`Inactive`时，统一Foot Placement MUST进入Idle Current Support模式，继续消费同一个Current Grounding安全Baseline，直到现有Stance捕获真实接触。该模式 MUST不创建第二Grounding或响应式前置，也 MUST不在权威动作存在但Plan为`Rejected`时介入；Rejected仍须显式暴露预测失败。

事件身份替换、Phase回退、动作中断或Stance捕获Landing MUST结束旧当前计划。新当前事件只有在自身PreSwing边界 MAY创建一次新计划；相同事件不得重试、晋升旧世界候选或逐帧重映射。

#### Scenario: Pose Contribution被替换

- **WHEN** 当前Landing Event身份不再匹配Executing Plan
- **THEN** 旧Plan MUST在该帧结束
- **AND** Final Goal与Gizmo MUST不再消费旧路线

#### Scenario: 循环步态的新当前事件

- **WHEN** 下一同脚Landing成为当前离散事实并进入PreSwing
- **THEN** 统一Foot Placement MUST以该帧Native Sole、KCC未来圆弧和同相位动画几何创建唯一新Plan
- **AND** MUST不晋升上一动作期间冻结的世界候选，也不得在LiftOff后重新查询、读取当前Pose投影或在台阶边缘改换Landing

#### Scenario: in-place动作烘焙支撑约束

- **WHEN** in-place动作的支撑脚为表现角色前进而相对Root向后移动
  - **THEN** 离线分析 MUST烘焙精确`Release / LiftOff / ApproachContact`边界，Runtime按权威Action Phase解析：`Supporting=Locked`、LiftOff前最后的`Releasing=Sliding`、Swing为`Unlocked`
  - **AND** MUST不以Heel/Toe相对Root的全三维速度幅值把正常支撑误判为Sliding或Unlocked
  - **AND** Runtime MUST不从25点几何路线近邻采样该离散事实，禁止为兼容旧Artifact临时改写Constraint

#### Scenario: 停步后等待Stance捕获

- **WHEN** Locomotion Landing Event已经退出、Plan为`Inactive`且该脚尚未满足现有Stance捕获条件
- **THEN** Final Foot Goal MUST保留同帧Current Grounding已经计算的安全Baseline
- **AND** 系统 MUST不把Goal改回可能穿过当前支撑面的原动画脚
- **AND** 该所有权 MUST在Stance捕获后由同一个Anchor正常接管

#### Scenario: 固定计划按权威动作时钟执行并保留当前支撑安全下界

- **WHEN** 同一Executing Plan的冻结Foot Route几何不变，但当前原动画鞋底与同相位Query Route存在平面误差
- **THEN** Animation Clearance、Constraint与Support MUST继续使用同帧Action Step Clock采样，Ground Path MUST使用同一Plan的冻结Foot Rate采样
- **AND** 当前鞋底空间投影 MUST只报告误差，不得推进或重建Foot Rate
- **AND** 若合成后的Heel或Toe低于同帧Current Grounding合法支撑面，统一Foot Placement MUST只沿Component Up应用满足该支撑面的最小净空平移
- **AND** Query Route、Ground Path、Landing、Query快照和Plan身份 MUST保持不变

#### Scenario: 静止动作仍有残余鞋底速度

- **WHEN** Motion Phase为`GroundedStationary`、没有权威Action Constraint且Current Query提供合法近距离支撑
- **THEN** 现有Stance MUST允许捕获该真实接触，不得仅因in-place动画残余鞋底速度或Plant Confidence拒绝锁脚
- **AND** Surface坡度、距离与唯一World Query有效性门禁 MUST继续生效

#### Scenario: 预测Landing按同一支撑面交给Stance

- **WHEN** Executing Plan进入`Unlocked + ApproachingContact`并提供冻结的Landing Ankle、旋转与Contact Surface
- **THEN** Stance MUST把三者作为同一个Landing事实，重建并校验该Surface的Collider、Layer与坡度，再检查当前鞋底到该平面的距离
- **AND** 权威`ApproachingContact` MUST不被in-place鞋底相对Root速度否决；该局部速度只能保留为诊断，不能作为世界接触门禁
- **AND** Stance MUST不采用预测Ankle却切换到Current Query的另一踏面；预测Surface无效时不得静默回退为Current Surface
- **AND** 捕获帧 MUST在该预测Surface上的安全Goal处原子建立完整Anchor；条件未通过时保持Swing，不得把响应式Goal伪装为预测Landing

#### Scenario: Current Anchor在安全Goal处捕获

- **WHEN** 同帧Current Grounding、距离和Capture条件共同通过且Stance捕获Anchor
- **THEN** Anchor MUST存储该帧已经完成鞋底安全约束的世界Goal并原子取得完整位置所有权
- **AND** 捕获不得因为从零推进Anchor Blend而让已接触脚继续移动
- **AND** 只有`PlantContact + 有效Anchor + 完整Blend` MAY报告`Anchored`；释放Blend期间 MUST报告Contact

#### Scenario: 权威支撑不被in-place残余脚速否决

- **WHEN** 动作已经声明`Locked / Sliding + Supporting / Releasing`且同帧Current Grounding提供合法近距离支撑
- **THEN** Stance MUST允许该权威支撑进入或保持Contact，不得再次用in-place残余鞋底速度否决
- **AND** Future Ground Path、第二查询、固定高度或第二Anchor MUST不参与该接触证明

### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解

Stance Stabilization MUST是唯一Anchor与Pelvis owner。它 MAY消费当前支撑、预测Landing、Support Phase和Hip Route，但 MUST只输出一个Pelvis Pre-Solve Goal。Current Pelvis候选 MUST只来自同帧真实支撑腿：权威`Unsupported`或`ApproachingContact`摆动脚的Current Query不得参与最低支撑；已锁定脚 MUST继续使用同一Anchor支撑平面，当前Query命中相邻踏面不得替换该平面。Predictive Body Support Path MUST与Foot Ground Envelope分离：每个Executable Plan MUST以计划创建时的当前合法支撑、时间更早的权威对侧Landing和本脚Landing冻结一条按Action Step Phase参数化的分段身体支撑坡线；Ground Envelope Upper Hull MUST保持feet-only，不得携带或驱动Root/Hip。双脚Plan重叠时，唯一Pelvis owner MUST选择`RemainingSeconds`最小的下一Landing所属Plan；相同Landing次序只可保持现有Plan Sequence，下一Landing发生或计划失效后才可原子交接。真实支撑腿身份 MUST继续只拥有Current Pelvis、Anchor和接触事实，不得与Predictive Body Plan身份合并。Pelvis Target MUST直接消费选中Body Support Path的地形修正Root位移，再由现有唯一临界Spring消除弹跳；MUST不平均左右脚独立Path，也不得以`4p(1-p)`或其它按步归零权重混合身体目标。权威Pose Root的世界竖直位移 MUST直接带动身体；唯一Spring只可输出支撑腿与Body Support Path要求的附加Component偏移，不得从Spring Current中扣除Root向上位移、不得把Root下降反向加入Spring，也不得消费Foot Ground Envelope。不得为此增加第二Spring、固定限速或新参数。腿长Reach MUST不在Spring之后再次修改Pelvis。锁定Anchor只有在Spring当前值与同一控制目标都位于该腿物理可达区间之外时，Stance才 MUST释放该Anchor并沿现有Anchor Blend连续退出；目标可达而Current尚在收敛时不得释放。系统不得立即Clear Anchor或把Foot Goal权重单帧归零。Predictive Plan MUST不创建第二Pelvis、第二Anchor或FBBIK后处理。

同一Plan中的Foot Ground Path与Body Support Path MUST使用不同但同源的确定采样：Foot Ground Path MUST消费计划创建时冻结的Foot Rate与feet-only Ground Envelope；地形修正Root/Hip MUST消费权威Action Step Clock和冻结Body Support Path。摆脚在动画中的快速前摆、回折、Upper Hull顶点或空间投影 MUST不得推进身体Root/Hip。两者共享同一Plan Sequence、Landing事实和Action Step Clock，但 MUST不共享Ground Envelope；不得建立第二Plan时钟或逐帧自适应路线。

#### Scenario: 下一Landing驱动身体支撑坡线

- **WHEN** 双脚Executable Plan中某一Plan的合法Landing最先发生
- **THEN** 该Plan的Body Support Path MAY拥有唯一Pelvis Target，而该脚的Current Query仍不得冒充真实支撑
- **AND** Landing捕获后 MUST只由同一Stance owner锁脚，Predictive Body Plan身份与真实支撑腿身份 MUST分别记录

#### Scenario: 下一Landing交接身体路径

- **WHEN** 当前选中Plan的下一Landing已经发生，另一脚提供更早的后续合法Landing
- **THEN** 唯一Pelvis owner MUST在同帧原子切换到后续Landing所属Sequence并消费其Body Support Path
- **AND** 同一次Landing次序未变化时 MUST保持原Sequence，不得按Support Phase往返切换
- **AND** Runtime Trace与CSV MUST记录左右候选事实、选中腿、切换帧、Current Target和Selected Target
- **AND** 系统 MUST不把两条独立Foot Path平均成第三条身体路径

#### Scenario: Anchored脚进入预测Swing

- **WHEN** Action Step Clock越过LiftOff且同一Foot仍保留非零Anchor Blend
- **THEN** Stance MUST保留同一个Anchor并连续衰减现有Blend，不得在LiftOff帧立即清除
- **AND** 唯一最终Foot Goal MUST按该Blend从Stance Goal连续交接到同一Executing Plan的Swing Goal

#### Scenario: Sliding支撑脚保持唯一所有权

- **WHEN** 权威Action Step Clock仍为PreSwing但离散Constraint已是`Sliding`
- **THEN** Plan MUST保持同一冻结Ground Probe、Ground Envelope、Foot Rate与Landing，Ground Path Progress MUST保持0
- **AND** Predictive输出权重 MUST保持0，现有Stance MUST继续拥有脚直到真实LiftOff
- **AND** Current Contact MUST不改变该权重、重建Plan或形成第二输出owner

#### Scenario: 锁定支撑腿等待Pelvis收敛

- **WHEN** 唯一Pelvis Spring当前值暂时落在某个锁定Anchor的腿长可达区间之外，但同一Pelvis Target仍在该区间内
- **THEN** Reach MUST保留该Anchor并允许唯一Spring向该Target收敛
- **AND** Pelvis Resolved MUST保持等于同帧Spring Current
- **AND** 只有Current与Target都不可达时才 MUST把Foot Goal沿现有Anchor Blend连续退出
- **AND** 交接 MUST不结束Plan、不创建第二Anchor，也不得退回响应式Swing目标

#### Scenario: 楼梯边缘Current Query切换踏面

- **WHEN** 一只脚已锁定低踏面Anchor，而同脚Current Query在相邻帧改为命中高一级踏面
- **THEN** Current Pelvis MUST继续消费该Anchor支撑平面的高度，直到同一Stance生命周期正式释放该Anchor
- **AND** 摆动脚或新Query MUST不得让Pelvis Target、Foot Goal和Anchor所有权发生单帧高度交接

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

Action Step Fact MUST与生成当前Component Pose的Pose Contribution同源，原子携带Landing身份、Action Step Clock、root-local Foot、Ankle、Hip、Clearance、约束策略，以及本脚下一次Landing前的对侧Landing身份与时间，不得携带Action Root或运行速度。Locomotion Sequence、Pose、Step Fact与Clearance MUST从Simulation提交的Locomotion elapsed tick投影到同一相位；Presentation只能插值，不得独立累计第二动作时间。Plan创建帧 MUST从同帧committed Body读取碰撞求解后的Target Velocity和Movement最大转向能力，并消费Presentation Fact已由连续正式速度方向确认的Trajectory Curvature；提交后不得重读。Blend或source替换 MUST生成明确新身份。Virtual Ground MUST只消费本脚Action Step Fact携带的原子对侧配对，不得独立选择或混合另一只脚的事实。

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
ActionProgress = Normalize(SimulationActionStepPhase, PlanStartPhase, LandingPhase)
FootRatePlan = FreezeProject(PredictedAnimationSole(ActionPhase), GroundProbe)
GroundPathProgress = Sample(FootRatePlan, SimulationActionStepPhase)
FinalSoleXZ = NativeAnimatedSoleXZ
PredictedSoleY = GroundPathY(GroundPathProgress) + AnimationClearanceY(SimulationActionPhase)
FinalSwingSoleY = PredictedSoleY
```

目标Ankle MUST由该鞋底与当前动画Sole-to-Ankle几何重建。Heel/Toe MAY沿同一Component Up执行最小安全修正，但不得改变原动画Foot XZ或创建第二支撑面。

#### Scenario: 上楼保留抬脚弧线

- **WHEN** 动画净空为0.10m且Ground Path比起点高0.20m
- **THEN** 预测候选鞋底高度 MUST约为Ground Path加0.10m
- **AND** 当前动画世界Y MUST不与该候选逐帧取`max`，当前动画只继续拥有XZ和Sole-to-Ankle几何

#### Scenario: 下楼路径下降

- **WHEN** Ground Path随Progress下降
- **THEN** 预测候选 MUST沿该路径下降并保留动画净空
- **AND** Predictive Modifier MUST直接消费下降后的唯一预测Y；Current Grounding只可在真实当前支撑更高时执行最终向上物理安全修正

### Requirement: Full Body IK必须在统一Foot Placement之后保持单次成熟biped求解

FullBodyIK MUST复用FinalIK FBBIK核心数学，在同一Pending Component Pose中应用唯一Pelvis与最终Foot Goals并执行一次solve。它 MUST不查询world、不读取Action Step、不规划、不锁脚，也 MUST不调用FinalIK Grounding、LegIK、TwoBoneIK或第二solver。

#### Scenario: Goal连续但Solved Foot异常

- **WHEN** Final Goal有限连续而solver或physical residual超过正式容差
- **THEN** FullBodyIK MUST返回typed failure并阻断Final Pose发布
- **AND** Diagnostics MUST保留Goal、solver与physical结果

### Requirement: 统一Foot Placement诊断必须与Full Body IK结果保持同一完成快照

每帧诊断 MUST覆盖动作身份与Phase、Plan状态、Action Progress、Ground Path Progress、当前Ground/Clearance采样、Stance/Pelvis、Final Goal和FBBIK结果。每计划不可变快照 MUST覆盖完整Ground Probe、动画脚路线、Foot Rate、Ground Envelope、Clearance Path、Landing、Query requests、接受支撑和拒绝几何。

Scene、Game、CSV MUST只读同一完成快照。该快照中的Ground Probe MUST直接复制Query实际使用的全部Route Fraction和世界点，不得按点数重新生成均匀近似路线；Virtual Ground插入点 MUST保留。Executable只画真实完整Ground Probe、动画脚路线、Ground Envelope与Clearance Path；Rejected只画真实查询与拒绝几何；Completed或Inactive不得继续画旧Path；不得显示文字。

诊断 MUST同时保存计划创建帧committed Body Target Velocity、Simulation Continuation、Movement最大转向能力、Trajectory Curvature、Generation/Plan Start/LiftOff/Landing Phase、Constraint Mode、剩余时间、冻结Ground Probe、动画脚路线、Foot Rate、Ground Envelope、预测Root位移、生成帧Native Sole、当前Ground Path采样和最终Goal。Ground Probe被画成最终脚轨迹、最大转向能力被用作路线曲率、Foot Rate随帧变化、Action Progress与Ground Path Progress无法区分、PreSwing Ground Path Progress不为0、Locked或Sliding期间Predictive Final Goal被改写、Clearance Path未使用Foot Rate或同一Plan的几何Hash变化时 MUST是明确invalid，不得只显示一条看似合法的线。

#### Scenario: Debug Path与脚目标对账

- **WHEN** Scene或Game绘制Executing Plan
- **THEN** 当前Ground sample MUST等于统一Foot Placement实际消费的同一冻结Path sample
- **AND** Final Foot XZ MUST等于同帧Native Animated Foot XZ，诊断 MUST不把Query Route点冒充最终落脚点

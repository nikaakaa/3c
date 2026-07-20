# character-foot-placement-presentation Specification

## ADDED Requirements

### Requirement: Foot Placement 必须是唯一 Presentation Pose Post Process Pass

`CharacterSimulationPresentationRuntime` MUST在每个合法PresentationFrame中按 `Body -> Animation/Animancer Evaluate -> Foot Placement -> Camera` 固定顺序推进角色表现。Foot Placement MUST由该runtime显式创建、更新、reset和dispose，MUST不依赖Final IK、Animator、MonoBehaviour或其它manager的自主`Update`、`LateUpdate`或`FixedUpdate`形成第二个姿势写入路径。

#### Scenario: 一个表现帧更新Corin

- **WHEN** Body frame有效且Animancer完成本帧Evaluate
- **THEN** Foot Placement MUST读取本帧最终动画姿势并只执行一次
- **AND** Camera MUST在Foot Placement完成后执行

#### Scenario: Final IK组件仍启用自主更新

- **WHEN** rig validation发现任一参与solver仍会由Unity lifecycle自主更新
- **THEN** runtime创建 MUST失败
- **AND** 系统 MUST不接受同帧双求解

### Requirement: Foot Placement 必须只消费表现帧正式输入

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、Animancer Evaluate后的骨骼姿势、只读visible playback contribution、显式`CharacterFootPlacementProfile`、显式rig binding和当前Unity PhysicsScene查询结果。它 MUST不读取BTSMTL runtime、State、Action、Blackboard、GameplayTag、MotionWarp target、WorldSolver对象、Network Model私有状态或logic Transform作为替代真相。

#### Scenario: 读取网络ObservedActor

- **WHEN** ObservedActor从SelectedStream得到一个Body frame
- **THEN** Foot Placement MUST使用该frame的visible pose、velocity、Grounded和reset identity
- **AND** MUST不回到authority packet或Prediction history选择另一Body tick

### Requirement: Foot contact 必须由最终姿势运动学和表面距离判断

每只脚 MUST在VisualRoot局部空间维护动画ankle/toe位置和相对速度，并结合下降趋势、sole到合法support的距离、Body Grounded、producer视觉权重以及显式enter/exit迟滞阈值判断plant/release。Foot Placement MUST不创建或读取第二份authoritative gait phase、Timeline Foot Window、Blackboard foot变量或按动画名称硬编码的接触表。

#### Scenario: 脚在楼梯踏面上减速

- **WHEN** 脚处于下降阶段、相对速度低于plant阈值且sole进入合法support距离
- **THEN** Contact classifier MUST允许该脚plant
- **AND** 结果 MUST不依赖当前State或Action名称

#### Scenario: 阈值边缘轻微抖动

- **WHEN** 已plant脚的速度在plant阈值附近波动但未超过release阈值
- **THEN** 该脚 MUST保持已有约束状态
- **AND** MUST不逐帧反复锁定和释放

### Requirement: Footprint prediction 必须保留动画水平脚步

Footprint predictor MUST使用visible Body线速度和yaw速度、当前动画foot局部位置、foot相对速度以及Profile有限look-ahead计算预测落点。Free脚的预测 MUST只影响support选择、摆腿clearance和落脚准备；在进入约束前，脚的水平运动 MUST继续来自动画姿势，不得被替换为统一程序化步幅。

#### Scenario: Corin向前跑上楼梯

- **WHEN** Free脚沿动画轨迹向下一踏面摆动
- **THEN** predictor MUST在该脚到达踏面前得到有限预测点
- **AND** MUST保留该动画原有的左右偏移和步幅

#### Scenario: 角色急转导致预测失效

- **WHEN** 预测点超过Profile的look-ahead、角速度或可达限制
- **THEN** predictor MUST夹紧或拒绝该预测并记录原因
- **AND** MUST不把脚瞬移到不可达位置

### Requirement: 地面查询必须形成有限连续 Support Envelope

每只脚 MUST使用固定容量workspace，对当前heel/toe、路径采样点和预测落点执行NonAlloc Sphere/Capsule查询，并按layer、self-collider、有限值、最大坡度、最大step up/down、腿长可达性、相邻高度连续性与稳定surface identity过滤候选。最终support MUST来自该连续路径；正式实现 MUST不退化为只查询当前脚下的一条Ray，也 MUST不要求隐藏ramp collider作为fallback。

#### Scenario: 脚跨过两个楼梯边缘

- **WHEN** 当前脚与预测落点之间存在多个高度连续的合法踏面候选
- **THEN** Support Envelope MUST保留路径顺序并选择可达终端support
- **AND** 摆腿clearance MUST不低于路径中的合法envelope

#### Scenario: 预测路径跨越不可达高差

- **WHEN** 相邻候选高度超过Profile允许的step或leg reach
- **THEN** 后续候选 MUST被拒绝
- **AND** Foot Placement MUST记录明确的不可达原因

### Requirement: 每只脚必须使用有限约束生命周期

每只脚 MUST且只能使用`Free`、`Locked`和`Sliding`表达约束所有权。Free到Locked MUST需要合法plant和support；Locked到Sliding MUST只在动画目标偏离lock但仍处于同一surface与允许slide/reach范围时发生；Locked或Sliding MUST在airborne、policy释放、surface失效、超过replant限制、腿不可达或reset时进入Free。Plant/release权重 MUST按presentation delta连续推进，不得增加隐藏状态或固定帧计时器。

#### Scenario: 动画脚在锁点附近继续移动

- **WHEN** Locked脚的动画目标离开锁点但仍在允许slide范围内
- **THEN** 该脚 MUST进入Sliding并在surface上受限移动
- **AND** MUST不保持无限硬锁造成膝盖扭曲

#### Scenario: 锁点超出腿长

- **WHEN** hip到lock target超过Profile最大leg extension
- **THEN** 该脚 MUST按`Unreachable`原因释放或replant
- **AND** solver MUST不强拉骨骼到非法长度

### Requirement: Locked Foot 必须支持移动 Surface

Locked或Sliding脚 MUST将support point和normal保存为命中Collider Transform的局部锚点，并在后续表现帧从同一surface重建世界目标。Surface被销毁、禁用、移出合法layer或不再满足坡度和reach时 MUST以明确原因释放。Surface引用和局部锚点 MUST只属于Presentation runtime。

#### Scenario: 角色站在移动平台

- **WHEN** 已锁定脚所在平台Transform在下一表现帧移动
- **THEN** 该脚世界目标 MUST由原局部锚点随平台更新
- **AND** Network、Snapshot和WorldState MUST不保存该脚锚点

### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解

Pelvis resolver MUST从动画pelvis、双侧hip、计划foot target、leg length、plant weight和Profile ascent/descent限制计算唯一垂直offset。Resolver MUST优先选择最接近动画pelvis且满足主要支撑腿的可达区间，并以presentation delta和显式half-life做临界阻尼；上移、下移和速度 MUST有独立有限上限。第一版 MUST不旋转pelvis、spine或VisualRoot。

#### Scenario: 左脚踏上更高台阶

- **WHEN** 左脚成为高处主要支撑且右腿仍可达
- **THEN** pelvis target MUST连续抬高以避免左腿过伸
- **AND** 上半身与VisualRoot MUST不被Foot Placement旋转

#### Scenario: 双腿可达区间不相交

- **WHEN** 两个leg reach区间没有合法交集
- **THEN** pelvis target MUST按Profile边界夹紧
- **AND** 不可达脚 MUST触发明确replant/release原因

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

`CharacterFootPlacementProfile` MUST只声明PoseSourceLayerId和角色级算法参数。每个Timeline Animation Clip MUST以stable clip identity保存一条归一化`Foot Placement Weight`曲线，表达该动画时间点允许Foot Placement整体介入多少；Prediction、Pelvis和Foot Rotation MUST继续由Profile与planner算法负责，不得成为逐Clip重复作者曲线。曲线 MUST随Timeline编译进Presentation Projection。Projection采样 MUST先按producer内部clip weight混合，Runtime再按Animancer实际visible state/layer weight混合`AnimationPoseContribution`。系统 MUST不使用逻辑priority、State、Action、Tag、clip名、asset path或数组index选择策略，也 MUST不在Profile保存第二份producer策略表。

#### Scenario: Attack淡出到Run

- **WHEN** Attack和Run在同一Animancer layer中同时以outgoing/incoming weight可见
- **THEN** Foot Placement总权重 MUST先使用各自当前Animation Clip曲线采样，再按两者实际视觉weight连续混合
- **AND** MUST不重新判断哪个逻辑状态优先

#### Scenario: 编辑Attack动画的Foot Placement恢复区间

- **WHEN** 作者在Timeline选择Attack Animation Clip并修改Foot Placement Weight曲线
- **THEN** dirty owner MUST是该Timeline且正式Presentation Projection MUST重建
- **AND** Gameplay Program、Profile和Graph MUST保持不变

#### Scenario: 在Timeline直接核对Animation Clip曲线

- **WHEN** Timeline包含一个或多个Animation Clip
- **THEN** 每个AnimationTrack MUST在Clip与Marker Sync下方显示默认折叠的独立Curves分组
- **AND** Clip视图 MUST只占据Clip行且不得遮挡Marker Sync或Curves区域
- **AND** 展开后 MUST只显示一条`Foot Placement Weight`曲线行以及`0/0.5/1`参考线、插值曲线和原始key
- **AND** 每段曲线 MUST按其唯一Clip的StartFrame与EndFrame对齐绘制
- **AND** 点击曲线段 MUST选择该Clip，拖动key MUST修改时间和值，双击 MUST增加key，右键 MUST删除非唯一key
- **AND** key拖动 MUST在pointer capture期间只更新曲线行的本地预览，即使指针离开曲线行也必须连续；释放或意外失去capture时 MUST以一个Undo事务提交最后预览值，Pointer Cancel则不得修改资产
- **AND** 全部编辑 MUST复用Timeline唯一Undo、dirty和Projection重建路径
- **AND** 曲线子轨 MUST不进入TimelineData.Tracks、不拥有AuthoringId、不执行Tick且不保存第二份曲线

#### Scenario: 曲线缺失或非法

- **WHEN** Animation Clip的Foot Placement Weight曲线缺少key、包含非有限值或时间/值超出`[0,1]`
- **THEN** Presentation Projection编译 MUST失败
- **AND** Runtime MUST不使用常量一或按动画名称推断的fallback

### Requirement: Foot Placement Planner 与骨骼 Solver 必须分离

项目Presentation core MUST唯一拥有contact、prediction、support envelope、constraint、pelvis与`CharacterFootPlacementPlan`。骨骼实现 MUST只通过`ICharacterFootPlacementSolver`消费plan、捕获显式rig姿势、应用双脚target和pelvis offset。Final IK adapter MUST位于独立命名程序集，使用两个显式Limb solver并由Pass单次驱动；`ThirdPersonClient.Runtime` MUST不引用RootMotion类型，Final IK vendor源码 MUST不被修改。

#### Scenario: Final IK应用一帧计划

- **WHEN** Planner输出双脚target、rotation、weight和pelvis offset
- **THEN** Final IK adapter MUST按固定顺序应用pelvis和两个Limb solver
- **AND** MUST不重新query地面或改变Planner约束状态

#### Scenario: 后续替换Solver实现

- **WHEN** 后续增加另一个`ICharacterFootPlacementSolver`
- **THEN** contact、prediction、constraint和pelvis runtime MUST不需要修改
- **AND** 新adapter MUST不成为第二个planner

### Requirement: Body与Presentation重置必须原子清除Foot Placement历史

Foot Placement Runtime MUST跟踪`CharacterBodyPresentationFrame.ResetSequence`。Initialization、CommittedBranchReplacement、SelectedStreamReset、Presentation Reset、dispose、动画尚无正式输出或显式非法pose不连续 MUST在应用新计划前清除脚速历史、surface anchor、constraint、solve weight、prediction workspace和pelvis阻尼，并从当前动画姿势重新锚定。正常producer crossfade MUST不触发硬reset。

#### Scenario: Rollback替换当前Body分支

- **WHEN** Body frame的ResetSequence因CommittedBranchReplacement增加
- **THEN** 两只脚 MUST在同一PresentationFrame释放旧世界锚点
- **AND** MUST不从旧锁点向新Body缓慢拉伸骨骼

#### Scenario: Dodge淡出到Run

- **WHEN** Body没有reset且Animancer正常crossfade producer
- **THEN** Foot Placement MUST保留合法surface lifecycle
- **AND** policy权重 MUST按当前视觉graph连续变化

### Requirement: Foot Placement 必须与Simulation和Network单向隔离

LocalOwner、SimulatedActor和ObservedActor MUST通过同一Factory、Profile、Foot Placement Runtime与solver合同处理；SourceMode和Camera capability MUST不选择不同IK算法。Foot target、constraint、surface anchor、pelvis offset和solver结果 MUST不进入Character/World state、MotionRequest、WorldSolver、GameplayFact、Blackboard、Snapshot、StateHash或网络packet，也 MUST不写VisualRoot。

#### Scenario: 两个客户端显示同一远端角色

- **WHEN** 两个客户端消费相同authority Body但本地PresentationFrame时刻不同
- **THEN** 两端 MAY各自计算纯视觉Foot Placement
- **AND** 结果差异 MUST不改变任何Gameplay或网络确认

### Requirement: Foot Placement 配置和Rig必须显式且可验证

每个启用Foot Placement的角色表现装配 MUST显式提供`CharacterFootPlacementProfile`、实现`ICharacterFootPlacementSolver`的adapter、VisualRoot、pelvis、左右hip/knee/ankle/toe、sole offsets、self-collider root、PhysicsScene和非空Ground LayerMask。系统 MUST不使用Animator Humanoid映射、名称、层级扫描、`GetComponentInChildren`、Default layer或单Ray作为缺失配置fallback。

#### Scenario: Corin缺少右Toe绑定

- **WHEN** Host创建Presentation runtime但rig没有显式右Toe
- **THEN** configuration validation MUST报告精确缺失字段并拒绝创建
- **AND** MUST不按骨骼名称自动搜索

### Requirement: Foot Placement 必须提供统一诊断且保持热路径有界

Runtime diagnostics MUST只读暴露Body tick/reset、visible producer weight、每只脚的constraint和transition reason、相对速度、surface distance、预测点、候选数、surface identity、lock/replant误差、最终权重、骨盆target/current offset、query计数和solver结果。Runtime MUST为Plan、Query和Solve提供Profiler marker，并复用固定容量query/candidate/snapshot workspace；表现热路径 MUST不使用LINQ、反射、字符串查找、临时List或每帧托管分配。Diagnostics和Scene gizmo MUST不重新query或修改计划。

#### Scenario: 排查楼梯上右脚滑动

- **WHEN** 右脚从Locked进入Sliding
- **THEN** Live Debug MUST显示surface、动画目标偏差、slide限制和transition reason
- **AND** Debug读取 MUST不改变下一帧constraint状态

### Requirement: Preview 必须遵守正式世界上下文边界

Play Mode中的完整Gameplay角色在具有显式Body、rig、Profile和PhysicsScene时 MUST复用正式Foot Placement Pass。项目 MUST不为Foot Placement恢复独立Preview Simulation。纯动画Timeline Preview没有正式Body和scene query上下文时 MUST明确不创建Foot Placement Runtime，且 MUST不生成默认平面、假Grounded或临时PhysicsScene来伪造效果。

#### Scenario: 纯动画预览攻击clip

- **WHEN** Timeline窗口只创建AnimationPlaybackRuntime进行纯动画采样
- **THEN** Preview MUST只显示Animancer最终动画姿势
- **AND** MUST明确Foot Placement不可用而不创建另一套preview solver

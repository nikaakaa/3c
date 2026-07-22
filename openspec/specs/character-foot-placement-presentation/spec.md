# character-foot-placement-presentation Specification

## Purpose

定义角色Foot Placement的唯一Presentation Pose Post Process边界，包括最终动画姿势接触判断、预测落脚、连续地面查询、有限脚约束、骨盆求解、Timeline曲线authoring、Final IK adapter、重置、诊断以及与Simulation和Network的单向隔离。

## Requirements

### Requirement: Rig Calibration必须同时约束Editor分析与Runtime Solver

Editor Foot Analyzer的Sampling Rig与Runtime `CharacterFootPlacementRig` MUST引用同一Calibration identity和content revision。Projection MUST保存该identity，Runtime composition MUST在创建Foot Placement前精确匹配。Calibration变化 MUST使Projection stale；系统 MUST不允许Editor与Runtime分别维护sole offset、semantic frame或pole方向。

#### Scenario: 作者修改Corin左脚toe sole offset

- **WHEN** Calibration content revision改变
- **THEN** 全部引用该Calibration的Definition Projection MUST变为Stale
- **AND** 全部Runtime Prefab MUST继续引用同一资产而不复制新值

### Requirement: Foot Rotation必须应用语义foot frame差值

Planner MUST从动画ankle rotation与Calibration计算Animated Semantic Foot Frame，再从CurrentSupport normal、动画semantic forward和Profile限制计算Desired Semantic Foot Frame，并以两者旋转差生成目标ankle rotation。速度响应、ascent/descent alignment与ankle twist reduction MUST在该语义空间中有界应用。Planner MUST不把`Quaternion.LookRotation`产生的semantic surface frame直接赋给ankle骨。

#### Scenario: Corin ankle骨轴不是标准forward/up

- **WHEN** Calibration声明的semantic frame与ankle骨局部轴不同
- **THEN** 目标鞋底 MUST按support normal对齐
- **AND** ankle骨 MUST保留rig-specific固定旋转关系

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

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、Animancer Evaluate后带有效lease的`FinalAnimationPoseFrame`、最终骨骼姿势、显式`CharacterFootPlacementProfile`、显式rig binding、同identity Rig Calibration和当前Unity PhysicsScene查询结果。Profile构造runtime settings时 MUST从`Projection.PoseProgram.Parameters`一次性绑定唯一`animation.foot-placement-weight`的`PoseParameterId`、dense index与`PoseProgramHash`；Present MUST核对同帧Completion、Availability、ProgramHash、最终Foot Features和有限归一化Weight。它 MUST不读取visible playback列表、Layer、producer binding，不再次采样Projection或AnimationClip，也 MUST不读取BTSMTL runtime、State、Action、Blackboard、GameplayTag、Animation Marker语义、MotionWarp target、WorldSolver对象、Network Model私有状态或logic Transform作为替代真相。

#### Scenario: 读取CrossFade后的最终姿态帧

- **WHEN** Outgoing与Current source经Blend Stack和Pose Graph共同形成最终姿势
- **THEN** Foot Placement MUST只消费该次Completion对应的最终Foot Features和最终`animation.foot-placement-weight`
- **AND** MUST不遍历source重新计算一次混合结果

#### Scenario: Runtime Projection缺少生成特征

- **WHEN** 启用Foot Placement的角色加载不含匹配Calibration与clip feature的Projection
- **THEN** Host创建 MUST失败并定位缺失identity
- **AND** Runtime MUST不即时分析AnimationClip或退回最终姿势差分独占路径

### Requirement: Foot contact 必须由最终姿势运动学和表面距离判断

每只脚 MUST在VisualRoot局部空间维护最终动画ankle/toe/sole姿势，并结合`FinalAnimationPoseFrame`已经按同一effective visual sample time/cycle解析完成的sole局部速度、高度与plant confidence、当前sole到合法CurrentSupport的距离、Body Grounded、最终作者Foot Placement Weight以及显式enter/exit/replant迟滞判断plant/release。生成局部速度 MUST已经由唯一动画source/Stack/PoseGraph链完成视觉时间倍率解析；Foot Placement只将其与Body visible线速度及yaw角速度在sole接触点产生的切向速度合成为世界接触速度。plant、release与Sliding稳定判断 MUST复用该世界速度。暂停、时间重定位或进入rebase的首帧 MUST由最终帧连续性与Reset重新锚定且不得伪造速度或未来落地。最终混合姿势差分 MUST只用于非法姿势不连续、当前世界位置和生成特征偏差诊断，不得独占contact真相。Foot Placement MUST不创建或读取第二份authoritative gait phase、Timeline Foot Window、Blackboard foot变量、Marker语义或按动画名称硬编码的接触表。

#### Scenario: CrossFade制造最终姿势假速度

- **WHEN** 两个可见producer中的同一只脚均具有高plant confidence但最终混合姿势因fade发生位移
- **THEN** Contact classifier MUST以各producer生成特征和当前合法support共同判断
- **AND** MUST不只因最终混合姿势差分超过旧阈值而释放

#### Scenario: InPlace动画随角色移动并改变播放速度

- **WHEN** 生成脚轨迹位于VisualRoot局部空间、Body在世界移动且Marker Sync改变视觉时间推进速度
- **THEN** Contact classifier MUST以缩放后的生成局部脚速、Body线速度和yaw接触点速度合成唯一世界脚速
- **AND** MUST不把InPlace局部水平轨迹或暂停帧误判为世界Plant/Release

#### Scenario: 生成特征显示接触但脚下无表面

- **WHEN** plant confidence达到进入阈值但CurrentSupport缺失或Body未Grounded
- **THEN** 该脚 MUST保持Free
- **AND** 生成特征 MUST不成为Gameplay或世界接触事实

#### Scenario: 阈值边缘轻微抖动

- **WHEN** 已plant脚的confidence、速度或surface distance在进入阈值附近波动但未超过释放阈值
- **THEN** 该脚 MUST保持已有约束状态
- **AND** MUST不逐帧反复锁定和释放

### Requirement: Footprint prediction 必须保留动画水平脚步

Footprint predictor MUST只对`FinalAnimationPoseFrame`中具有非零next landing confidence的最终每脚特征使用下一落地delay与VisualRoot局部landing offset，并结合visible Body线速度、yaw速度、当前动画foot局部位置以及Profile有限时间、距离、角速度和reach边界计算预测落点。source之间的landing MUST已由唯一Stack/PoseGraph链合成；没有有效最终特征时 MUST返回明确invalid。`FutureLandingSupport` MUST只影响未来落面选择、Ground Envelope和落脚准备；Free脚的当前水平运动 MUST继续来自最终动画姿势，不得被替换为统一程序化步幅或提前牵引到未来踏面。

#### Scenario: Corin向前跑上楼梯

- **WHEN** Free脚沿动画轨迹摆向下一踏面且生成特征给出有限landing delay与offset
- **THEN** predictor MUST在该脚到达踏面前得到有限Future Landing Support
- **AND** 当前脚X/Z MUST继续保留动画原有左右偏移和步幅

#### Scenario: 有限动画没有下一落地

- **WHEN** Once或Hold动画在当前sample之后没有生成的下一landing segment
- **THEN** predictor MUST返回明确的NoFutureLanding原因
- **AND** MUST不回绕到clip开头或使用默认look-ahead伪造落点

#### Scenario: 角色急转导致预测失效

- **WHEN** 预测点超过Profile的时间、距离、yaw速度或leg reach限制
- **THEN** predictor MUST夹紧或拒绝该预测并记录原因
- **AND** MUST不把脚瞬移到不可达位置

### Requirement: 地面查询必须形成有限连续 Support Envelope

每只脚 MUST使用固定容量workspace，分别对当前heel与toe执行NonAlloc Ray/Sphere查询并保留独立合法support，再对动画脚路径和Future Landing位置执行NonAlloc Ray/Sphere/Capsule查询。两个当前support同时合法时，Query MUST以heel/toe接触点构造唯一virtual support plane，并按明确高度与稳定identity选择移动surface owner；只有一侧合法时 MUST将该侧support plane投影到脚底中心。路径候选 MUST只按layer、self-collider、有限值、最大坡度、最大step up/down、腿长可达性、surface identity、edge gap与路径连续性构造有序Ground Envelope segment，不得覆盖Current Support。Query MUST分别输出heel support、toe support、`CurrentSupport`、`FutureLandingSupport`与每段minimum allowed sole height；virtual ground MUST来自合法有限命中，不得是隐藏Collider、默认平面或fallback。正式实现 MUST不退化为单Ray，也 MUST不把路径最远命中直接当作当前脚目标。

#### Scenario: 脚跨过两个楼梯边缘

- **WHEN** 当前脚与预测落点之间存在多个高度连续的合法踏面
- **THEN** Ground Envelope MUST保留surface和edge分段顺序
- **AND** Free脚只在动画Y低于minimum envelope时抬高
- **AND** 当前脚X/Z MUST不被FutureLandingSupport替换

#### Scenario: 预测路径跨越不可达高差

- **WHEN** 相邻候选高度、edge gap或reach超过Profile允许范围
- **THEN** 后续segment MUST被裁剪并记录明确原因
- **AND** FutureLandingSupport MUST不跨越该中断

### Requirement: 每只脚必须使用有限约束生命周期

每只脚 MUST且只能使用`Free`、`Locked`和`Sliding`表达约束所有权。Free到Locked MUST需要合法生成plant倾向与CurrentSupport；Locked到Sliding MUST只在动画目标偏离lock但仍处于同一surface与允许slide/reach范围时发生；Locked或Sliding MUST在airborne、policy释放、surface失效、超过replant限制、腿不可达、过度ankle twist或reset时进入Free。双脚最小分离、heel lift、ankle twist reduction与速度响应 MUST作为有限Plan约束或权重，不得成为隐藏状态或固定帧计时器。不可达脚 MUST在发现当帧立即以零IK权重重建Plan。

超过Replant限制释放旧锚点后，旧constraint solve weight MUST在Free中按release half-life连续衰减到零；在此之前 MUST禁止提交新Current Support。自由脚摆动clearance MAY继续求解，但 MUST不反向维持旧constraint weight，也不得在释放同一表现帧满权重重新锁定。

#### Scenario: 转身时双脚目标交叉

- **WHEN** 左右计划目标低于Profile最小分离距离
- **THEN** Constraint resolver MUST在有限reach内分离目标或释放次要支撑脚
- **AND** MUST不增加Turn专用状态或按动画名称选择规则

#### Scenario: 锁点超出腿长

- **WHEN** hip到lock target超过Profile最大leg extension
- **THEN** 该脚 MUST在同一render frame按Unreachable原因进入Free并输出零position/rotation weight
- **AND** solver MUST不继续使用旧target拉扯一帧

#### Scenario: Replant目标在释放帧已经可用

- **WHEN** Locked脚超过Replant限制且同帧查询到另一个合法Current Support
- **THEN** 该脚 MUST先进入Free并连续释放旧constraint solve weight
- **AND** 只有旧权重归零后的后续表现帧才 MAY提交新锚点

### Requirement: Locked Foot 必须支持移动 Surface

Locked或Sliding脚 MUST将support point和normal保存为命中Collider Transform的局部锚点，并在后续表现帧从同一surface重建世界目标。Surface被销毁、禁用、移出合法layer或不再满足坡度和reach时 MUST以明确原因释放。Surface引用和局部锚点 MUST只属于Presentation runtime。

#### Scenario: 角色站在移动平台

- **WHEN** 已锁定脚所在平台Transform在下一表现帧移动
- **THEN** 该脚世界目标 MUST由原局部锚点随平台更新
- **AND** Network、Snapshot和WorldState MUST不保存该脚锚点

### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解

Pelvis resolver MUST从动画pelvis、双侧hip、计划foot target、leg length、plant weight和可用heel lift计算独立Reach Offset。`CharacterFootPlacementProfile` MUST显式选择`AllPlantedFeet`或`DirectionalSlopeSupport`高度模式；前者按全部plant脚的权重求解，后者在双脚plant时以Body可见水平移动方向、脚在移动方向上的前后顺序与脚面高度差选择唯一主要支撑脚。Directional模式上坡 MUST选择移动方向前方且更高的plant脚，下坡 MUST选择较低的plant脚；只有一只脚plant时 MUST选择该脚。移动方向、前后顺序或高度差证据不足时 MUST输出typed `Neutral`或`Unavailable`及原因，不得隐式退回双脚平均。Corin MUST使用`DirectionalSlopeSupport`。

Body Presentation Frame MUST同时提供当前正式区间`SourceTranslationDelta`、Reset安全的当前表现帧`VisibleTranslationDelta`、`VisibleVelocity`、`GroundedBefore/After`和`ResetSequence`，不得以`VisibleVelocity * deltaSeconds`近似实际Body位移。Profile MUST显式提供方向最低速度、脚前后最小距离、坡面最小高度差，以及`ComponentSpace`、`WorldSpace`、`SuddenMotionOnly` Actor Movement Compensation模式、Sudden垂直阈值、补偿上限、half-life和maximum speed；Corin MUST使用`SuddenMotionOnly`。Resolver MUST分别维护Reach Offset与Actor Movement Compensation的offset/velocity，再合成唯一有界的VisualRoot组件空间竖直标量。骨骼solver MUST把`VisualRoot.up * offset`转换到pelvis父骨空间后再叠加本帧动画local position，不得把父骨local Y假定为角色竖直方向。Resolver MUST不读取KCC step phase、不产生水平pelvis位移，也 MUST不旋转pelvis、spine或VisualRoot。

#### Scenario: Pelvis父骨带有预旋转

- **WHEN** 角色骨架的pelvis父骨local Y不与VisualRoot up轴重合
- **THEN** 同一个正负垂直offset MUST仍只沿VisualRoot组件竖直方向移动pelvis
- **AND** solver MUST不把竖直补偿转成横向或前后位移

#### Scenario: 左脚踏上更高台阶

- **WHEN** 左脚成为高处主要支撑且heel lift仍有可用范围
- **THEN** resolver MUST先使用有限heel lift再计算剩余pelvis上移
- **AND** 右腿、上半身和VisualRoot MUST保持合法连续

#### Scenario: 双脚plant时向上坡移动

- **WHEN** Directional模式具有有效移动方向，前方plant脚比后方plant脚高出Profile阈值
- **THEN** Pelvis MUST以该前方脚作为唯一高度支撑并记录`UphillForwardFoot`
- **AND** MUST不使用双脚平均降低前方踏步响应

#### Scenario: 双脚plant时向下坡移动

- **WHEN** Directional模式具有有效移动方向，前方plant脚比后方plant脚低出Profile阈值
- **THEN** Pelvis MUST选择较低plant脚并记录`DownhillLowerFoot`
- **AND** MUST不以较高后脚维持旧骨盆高度

#### Scenario: 方向化支撑证据不足

- **WHEN** Body水平速度、脚前后距离或坡面高度差不足以形成方向化选择
- **THEN** Pelvis MUST输出`Neutral`或`Unavailable`及精确原因
- **AND** MUST不隐式调用`AllPlantedFeet`平均策略

#### Scenario: Body沿平滑地面或移动平台连续移动

- **WHEN** `ComponentSpace`模式下可见Body本帧发生有限垂直移动
- **THEN** Pelvis MUST跟随该Body移动且Actor Movement Compensation保持为零
- **AND** Foot Placement MUST不产生第二份Body trajectory filter

#### Scenario: 接地角色跨上离散台阶

- **WHEN** `SuddenMotionOnly`模式下`GroundedBefore/After`均为真且正式区间垂直位移超过阈值
- **THEN** Pelvis MUST按该区间内每个表现帧的`VisibleTranslationDelta.y`积累等量反向补偿
- **AND** 补偿 MUST通过独立有界临界阻尼恢复为零

#### Scenario: WorldSpace持续保持

- **WHEN** `WorldSpace`模式下接地Body连续发生垂直移动
- **THEN** Pelvis MUST对全部可见垂直位移积累反向补偿并由独立弹簧回收
- **AND** Reach Offset状态 MUST不被该补偿覆盖

#### Scenario: 表现流重置或角色离地

- **WHEN** Body `ResetSequence`变化、动画输出缺失、Rig重建或`GroundedAfter`为假
- **THEN** Foot Placement MUST清除Actor Movement Compensation及脚锁状态
- **AND** MUST不把旧台阶偏移带入新分支、空中或下一段动作

### Requirement: Animation Clip Foot Placement曲线必须沿正式表现投影采样

每个Timeline Animation Clip MUST继续唯一保存一条可写`Foot Placement Weight`曲线，表达Foot Placement总体介入量。左右脚sole速度、高度、plant confidence与landing feature MUST由Editor-only artifact生成并在Definition Build时嵌入Projection；它们不得成为Timeline Track lane、editable Curve Channel、Undo数据、Blackboard或Agent Patch字段。Timeline与Motion Matching source MUST在各自同一effective visual sample time/cycle把唯一`animation.foot-placement-weight`和生成Foot Features写入正式source pose payload，Blend Stack与Pose Graph MUST按显式policy形成`FinalAnimationPoseFrame`唯一结果。Foot Placement MUST只读取最终参数和最终特征。摆脚clearance、support target与rotation target MUST先生成完整几何结果；同一作者Weight对constraint position、rotation、free clearance与Pelvis各自最终求解链 MUST只应用一次，不得在目标生成、IK weight和Pelvis中重复相乘。

#### Scenario: 编辑Foot Placement Weight

- **WHEN** 作者在Timeline编辑Foot Placement Weight
- **THEN** Timeline MUST只修改该Animation Clip作者曲线
- **AND** generated artifact MUST不被当作可写曲线同步修改

#### Scenario: 查看generated feature

- **WHEN** 作者通过Animation Analysis面板查看Plant metric
- **THEN** 面板 MUST读取精确artifact并保持只读
- **AND** AnimationTrack主行与CURVES分组 MUST不增加generated channel

#### Scenario: Marker Sync后的最终特征

- **WHEN** Marker Sync改变某source的effective visual sample time
- **THEN** 该source MUST在同一时间写入Foot Features与Foot Placement Weight
- **AND** Foot Placement MUST读取Pose Graph最终结果且不重新采样该source
- **AND** MUST不读取MarkerId作为plant/contact真相

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

每个启用Foot Placement的角色表现装配 MUST显式提供`CharacterFootPlacementProfile`、实现`ICharacterFootPlacementSolver`的adapter、`CharacterFootPlacementRig`、共享`CharacterFootPlacementRigCalibration`、VisualRoot、pelvis、左右hip/knee/ankle/toe、self-collider root、PhysicsScene和非空Ground LayerMask。Calibration MUST唯一提供左右heel/toe sole offset、semantic forward/up frame和knee pole方向；Runtime Rig Calibration identity MUST与Projection Foot Analysis Calibration identity完全匹配。系统 MUST不使用Animator Humanoid映射、名称、层级扫描、`GetComponentInChildren`、零offset、默认axis、Default layer或单Ray补全缺失配置。

#### Scenario: Corin Runtime Prefab与Projection使用不同Calibration

- **WHEN** Host创建Presentation runtime但Rig Calibration revision不匹配Projection
- **THEN** configuration validation MUST报告两端identity并拒绝创建
- **AND** MUST不以Prefab局部字段或旧Projection继续运行

#### Scenario: semantic foot frame退化

- **WHEN** 任一forward/up axis非有限、近零或不满足正交边界
- **THEN** Calibration validation MUST拒绝保存或Build
- **AND** Runtime MUST不使用Vector3.forward/up作为fallback

### Requirement: Foot Placement 必须提供统一诊断且保持热路径有界

Runtime diagnostics MUST只读暴露Body tick/reset、Calibration/Analysis identity、PoseProgramHash、CompletionIdentity、Pose Continuity、最终Foot Placement参数identity/index/value、最终source contribution、每只脚的生成plant confidence/局部速度/合成世界速度/高度/landing、heel/toe/current/future support identity、constraint和transition reason、Ground Envelope segment与拒绝原因、surface identity、lock/replant/twist/separation误差、heel lift、最终权重、Pelvis Height mode/decision/reason/support foot、pelvis target/current offset、query计数和solver结果。Runtime MUST为Feature、Plan、Query和Solve提供Profiler marker，并复用固定容量contribution、feature、query、candidate、segment和snapshot workspace；表现热路径 MUST不采样AnimationClip，不使用LINQ、反射、字符串查找、临时List或每帧托管分配。Diagnostics和Scene gizmo MUST不重新分析动画、query或修改计划。

#### Scenario: 排查CrossFade误释放

- **WHEN** 一只脚在CrossFade后的最终姿态中从Locked进入Free
- **THEN** Live Debug MUST显示对应Completion、最终参数、source contribution和生成confidence
- **AND** Debug读取 MUST不改变下一帧Pose Graph结果或constraint状态

### Requirement: Preview 必须遵守正式世界上下文边界

Play Mode中的完整Gameplay角色在具有显式Body、rig、Profile和PhysicsScene时 MUST复用正式Foot Placement Pass。项目 MUST不为Foot Placement恢复独立Preview Simulation。纯动画Timeline Preview没有正式Body和scene query上下文时 MUST明确不创建Foot Placement Runtime，且 MUST不生成默认平面、假Grounded或临时PhysicsScene来伪造效果。

#### Scenario: 纯动画预览攻击clip

- **WHEN** Timeline窗口只创建AnimationPlaybackRuntime进行纯动画采样
- **THEN** Preview MUST只显示Animancer最终动画姿势
- **AND** MUST明确Foot Placement不可用而不创建另一套preview solver

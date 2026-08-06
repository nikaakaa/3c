# character-foot-placement-presentation Specification

## Purpose

定义角色Foot Placement在staged Pose Graph中的有状态world-aware规划职责，以及通过typed双腿目标连接独立Leg IK解析式求解的正式链路，包括最终动画姿势接触判断、预测落脚、连续地面查询、有限脚约束、骨盆补偿、腿链求解、时间曲线authoring、重置、诊断以及与Simulation和Network的单向隔离。
## Requirements
### Requirement: Rig Calibration必须同时约束Editor分析与Runtime Solver

Editor Foot Analyzer MUST显式引用Rig Definition v3、Sampling Rig与Calibration v3；Runtime MUST通过同一Rig v3和通用Animation Rig Binding解析骨骼，通过World-Aware Binding取得self-collider排除与world fixture。Calibration v3 MUST以每脚heel/toe contact offset、由heel-to-toe与VisualRoot up派生的单一ankle-local sole frame rotation，以及由精确Calibration Preview `Hip -> Knee -> Ankle`姿势派生的VisualRoot-local preferred bend direction作为唯一几何真相。Artifact、Projection与Runtime MUST精确匹配Rig、Sampling Rig和Calibration三方identity/revision。Calibration或Rig变化 MUST使Projection stale；系统 MUST不允许Editor与Runtime分别维护contact、sole frame、bend direction或腿骨。

#### Scenario: 作者修改Corin左脚toe sole offset

- **WHEN** Calibration content revision改变
- **THEN** 全部引用该Calibration的Definition Projection MUST变为Stale
- **AND** 全部Runtime Prefab MUST继续引用同一资产而不复制新值

### Requirement: Rig Calibration必须在精确Sampling Rig上下文可视化编辑

Sole frame MUST只通过heel/toe接触点间接编辑。Editor MUST以heel-to-toe平面投影作为前轴、VisualRoot up作为上轴自动派生完整frame，并 MUST不提供独立rotation handle。

系统 MUST从`CharacterFootPlacementAnalysisSource`提供显式`Edit Rig Calibration`作者入口，并以该Source精确引用的Sampling Rig和Calibration建立唯一Editor session。Scene View MUST只允许编辑左右heel/toe contact；sole frame MUST由heel-to-toe与VisualRoot up自动派生，preferred bend direction MUST由当前Calibration Preview的`Hip -> Knee -> Ankle`弯曲方向自动派生。Scene View MUST把sole frame、preferred bend、统一参考地面、sole长度、左右手性、hip-knee-ankle弯曲平面和参考平地ankle correction作为只读诊断显示，MUST不提供手动Knee Bend位置、方向或pole override。正式提交 MUST先同时获得左右腿有限非退化的自动bend direction并通过统一几何validator，再以单次Undo更新Calibration content revision和dirty；非法draft MUST保留旧正式数据。系统 MUST不允许作者在缺少精确Analysis Source/Sampling Rig上下文时编辑裸几何坐标，也 MUST不在`OnInspectorGUI`、selection、repaint或handle拖动期间执行AnimationClip分析、artifact rebuild、Compile或Build。

`CharacterFootPlacementAnalysisSource` MUST显式配置持久化的Calibration Preview Clip与归一化预览时间。进入校准session时，Editor MUST在独立Animation Mode driver拥有的临时PlayableGraph中把该固定帧采样到Sampling Rig；退出、切换Prefab Stage或采样失败时 MUST恢复进入前姿势并释放preview graph。Preview Pose MUST只改变作者看到和操作的姿势，MUST不生成第二套Calibration数据，也 MUST不进入Runtime Foot Placement链路。

#### Scenario: 作者校准Corin右脚鞋底

- **WHEN** 作者从Corin Analysis Source执行`Edit Rig Calibration`
- **THEN** Scene View MUST在精确Corin Sampling Rig上允许编辑右脚heel/toe，并只读显示自动sole frame和由预览姿势派生的膝盖首选方向
- **AND** Apply MUST只写入该Source引用的唯一Calibration资产

### Requirement: 腿部弯曲稳定必须保留动画平面并使用有限伸展区间

Planner MUST从最终动画hip、knee和ankle姿势计算每脚animated bend plane normal，并从目标脚位置和有限leg length计算`LegExtensionRatio`。`CharacterFootPlacementProfile` MUST显式提供严格有序的最小/最大伸展比例、稳定介入区间和最大弯曲稳定权重。Calibration的preferred bend direction MUST只用于生成vendor-neutral `PreferredBendPlaneNormal`；targets MUST分别输出position、rotation与bend weight。

LegIK ABI MUST把最终混合结果解释为`BendPlaneNormal`，以应用pelvis后的`Hip -> TargetAnkle`轴计算有限`KneeDirection = Cross(TargetAxis, BendPlaneNormal)`，不得把平面法线直接当作膝盖方向。在安全伸展区间内bend weight MUST为零并保留动画平面；接近过度伸直或动画平面退化时Planner MUST连续增加有限稳定权重。目标低于最小或超过最大可解伸展范围时Planner MUST将position、rotation和bend权重归零，不得把不可解目标提交给LegIK硬拉。

#### Scenario: 正常Walk动画膝盖弯曲清晰

- **WHEN** leg extension位于Profile安全区间且animated bend plane normal有限
- **THEN** targets的Bend Stabilization Weight MUST为零
- **AND** LegIK MUST由动画平面计算膝盖方向而不得把normal作为direction使用

#### Scenario: 脚目标接近腿完全伸直

- **WHEN** Leg Extension Ratio从Stabilization Start接近Stabilization Full
- **THEN** Planner MUST连续混向Calibration preferred bend normal并限制最大权重
- **AND** LegIK MUST在目标腿轴空间重新计算有限Knee Direction

### Requirement: Foot Rotation必须应用语义foot frame差值

Planner MUST从动画ankle rotation与Calibration计算Animated Semantic Foot Frame，再从CurrentSupport normal、动画semantic forward和Profile限制计算Desired Semantic Foot Frame，并以两者旋转差生成目标ankle rotation。速度响应、ascent/descent alignment与ankle twist reduction MUST在该语义空间中有界应用。Planner MUST不把`Quaternion.LookRotation`产生的semantic surface frame直接赋给ankle骨。

#### Scenario: Corin ankle骨轴不是标准forward/up

- **WHEN** Calibration声明的semantic frame与ankle骨局部轴不同
- **THEN** 目标鞋底 MUST按support normal对齐
- **AND** ankle骨 MUST保留rig-specific固定旋转关系

### Requirement: Foot Placement规划与Leg IK求解必须在Pose Graph中显式分段

启用Foot Placement的Character Presentation Pose Graph MUST显式包含一个`FootPlacement`与一个`LegIK`节点。`FootPlacement` MUST是每个最终Output路径唯一有状态`WorldAwarePose`节点，接收Component Pose与唯一Foot Placement Weight，输出只应用pelvis component offset的Component Pose及同帧typed `component.biped-leg-targets`。`LegIK` MUST是无状态`PurePose`节点，同时消费该FootPlacement输出的Component Pose与targets，只修改Rig v3左右Physical腿链并输出已求解Component Pose。Compiler MUST禁止把Planner与solver重新降低为同一个复合operation，也 MUST禁止在图外、Animator、MonoBehaviour或其它manager追加Foot Placement或Leg IK。每个targets输出 MUST且只能由同call-site的一个LegIK消费。

#### Scenario: 一个表现帧更新Corin

- **WHEN** Corin Pose Plan执行FootPlacement并得到合法Component Pose与双腿targets
- **THEN** Runtime MUST先完成world query、contact、pelvis与targets发布，再执行独立LegIK pure stage
- **AND** FinalAnimationPoseFrame MUST只在LegIK及全部后续stage完成后发布

#### Scenario: FootPlacement缺少LegIK消费方

- **WHEN** 到达OutputPose的图路径包含FootPlacement但其targets未连接LegIK
- **THEN** Graph Validator与Build MUST拒绝该图
- **AND** Runtime MUST不隐藏补建LegIK或把未求解Pose当作最终结果

### Requirement: Foot Placement 必须只消费表现帧正式输入

Foot Placement MUST只读取同帧`CharacterBodyPresentationFrame`、带有效lease的上游Component Pose Value、最终Pose contribution与Foot Features、显式`CharacterFootPlacementProfile`、Rig v3、同identity Rig Calibration和当前Unity PhysicsScene查询结果。Profile构造runtime settings时 MUST从`Projection.PoseProgram.Parameters`一次性绑定唯一`animation.foot-placement-weight`的`PoseParameterId`、dense index与`PoseProgramHash`；operation MUST核对同帧Completion、Availability、ProgramHash和有限归一化Weight。若上游包含Inertialization或其它composition，Foot Placement MUST读取其实际输出，MUST不遍历source重新计算混合结果。它 MUST不读取visible playback列表、Layer、producer binding、AnimationClip、BTSMTL runtime、State、Action、Blackboard、GameplayTag、Marker语义、MotionWarp target、Network Model私有状态或logic Transform作为替代真相。

#### Scenario: 读取CrossFade后的最终姿态

- **WHEN** Outgoing与Current source经Blend Stack和上游Pose节点共同形成Component Pose
- **THEN** Foot Placement MUST只消费该次Completion对应的Pose、Foot Features和`animation.foot-placement-weight`
- **AND** MUST不遍历source重新计算一次混合结果

#### Scenario: Runtime Projection缺少生成特征

- **WHEN** Foot Placement需要的dense Foot Feature未被Projection发布
- **THEN** world-aware stage MUST失败并报告确切缺失字段
- **AND** MUST不从AnimationClip或Transform现场重建特征

#### Scenario: 左脚分支正在惯性衰减

- **WHEN** 上游Local Pose惯性化后转换为Component Pose且左脚贡献正在衰减
- **THEN** Foot Placement MUST使用最终传播到节点输入的左脚Feature与Weight
- **AND** MUST不读取Inertialization私有Accumulator决定接触

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

Foot Placement正式查询Mask MUST包含普通共享`Ground`与真实踏面`FootPlacementSurface`，并 MUST排除Gameplay专用`CharacterTraversal`。连续楼梯的无Renderer Traversal Ramp MUST不成为heel、toe、Current Support、Future Landing Support、Ground Envelope或Locked Surface；真实踏面Collider即使不进入Deterministic Collision Artifact，也 MAY作为合法Presentation Surface。系统 MUST不同时查询Ramp和踏面后按命中优先级选择，不得从KCC support identity、Step阶段或Collision Artifact重建Foot Surface。

#### Scenario: 脚跨过两个楼梯边缘

- **WHEN** 当前脚与预测落点之间存在多个高度连续的合法踏面
- **THEN** Ground Envelope MUST保留surface和edge分段顺序
- **AND** Free脚只在动画Y低于minimum envelope时抬高
- **AND** 当前脚X/Z MUST不被FutureLandingSupport替换

#### Scenario: 预测路径跨越不可达高差

- **WHEN** 相邻候选高度、edge gap或reach超过Profile允许范围
- **THEN** 后续segment MUST被裁剪并记录明确原因
- **AND** FutureLandingSupport MUST不跨越该中断

#### Scenario: Body沿Gameplay Ramp上楼

- **WHEN** KCC Body沿`CharacterTraversal`连续升高且脚下存在`FootPlacementSurface`真实踏面
- **THEN** Foot Placement MUST从真实踏面生成heel、toe、Current Support和Future Landing
- **AND** MUST不使用Ramp法线或Ramp高度替代可见踏面

#### Scenario: Foot Placement Profile包含CharacterTraversal

- **WHEN** Profile配置会让Foot查询命中Gameplay Ramp
- **THEN** Profile或楼梯组合校验 MUST失败并报告冲突Layer
- **AND** Runtime MUST不以踏面优先级或Collider名称消解重叠命中

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

每个有限Action Timeline Animation Clip或持续Pose Source Binding MUST继续唯一保存一条可写`Foot Placement Weight`曲线，表达Foot Placement总体介入量。左右脚sole速度、高度、plant confidence与landing feature MUST由Editor-only artifact生成并在Definition Build时嵌入Projection；它们不得成为Timeline Track lane、editable generated Curve Channel、Blackboard或Agent Patch字段。各source MUST在同一effective visual sample time/cycle把唯一`animation.foot-placement-weight`和生成Foot Features写入正式source pose payload，Blend Stack与Pose Graph MUST形成唯一最终参数和特征。

唯一Weight MUST只由FootPlacement operation消费一次，并在其contact、constraint target、free clearance、pelvis及最终targets weight中形成结果。LegIK MUST不读取该参数，也不得把相同Weight再次乘到pelvis、position、rotation或bend。摆脚clearance、support target与rotation target MUST先生成完整几何结果，再按各自最终求解链应用一次Weight。

#### Scenario: Foot Placement Weight为一半

- **WHEN** Pose Graph最终`animation.foot-placement-weight`为0.5
- **THEN** FootPlacement MUST按0.5生成最终pelvis与targets weights
- **AND** LegIK MUST直接消费这些weights而不得形成0.25平方响应

#### Scenario: Marker Sync后的最终特征

- **WHEN** Marker Sync改变某source的effective visual sample time
- **THEN** 该source MUST在同一时间写入Foot Features与Foot Placement Weight
- **AND** FootPlacement MUST读取Pose Graph最终结果且LegIK只读取其targets
- **AND** 两节点 MUST不读取MarkerId作为plant/contact真相

### Requirement: Foot Placement Planner与Leg IK Solver必须使用typed目标合同

`CharacterFootPlacementPlanner` MUST只根据正式输入和world query生成vendor-neutral`CharacterFootPlacementPlan`，不得求解左右Physical腿链或写Transform。FootPlacement operation MUST把Plan中的pelvis component offset应用到其Component Pose输出，并把左右ankle target、rotation、animated/preferred bend plane normal、bend stabilization weight、position/rotation weight、extension ratio、constraint state与decision reason发布为同帧固定workspace中的`component.biped-leg-targets`。`LegIK` MUST只根据FootPlacement输出Component Pose、Rig v3 chain与targets求解双腿，不得查询world、读取AnimationClip/Profile/Body、决定contact lifecycle、重新应用pelvis或读取第二Foot Placement Weight。Targets MUST携带Frame、Completion与Rig identity，不得跨帧、跨Rig、扇出、序列化进作者资产或进入Gameplay/Network状态。

#### Scenario: LegIK应用一帧目标

- **WHEN** FootPlacement发布匹配当前Pose Completion与Rig revision的左右腿targets
- **THEN** LegIK MUST在独立output workspace求解左右腿并发布Component Pose
- **AND** final writer之前 MUST不存在Transform写入

#### Scenario: Pose与targets来自不同call-site

- **WHEN** LegIK的Component Pose和targets不共享同一个FootPlacement CompletionIdentity
- **THEN** Validator或Runtime stage MUST明确失败
- **AND** MUST不按最新targets、节点顺序或Rig名称猜测配对

### Requirement: Leg IK必须保持Physical腿链长度

LegIK MUST先以Position Weight混合动画Ankle Position与Target Ankle Position，再对effective target执行完整解析式两骨求解。输出Hip-Knee长度与Knee-Ankle长度 MUST保持当前Rig v3 Physical chain长度；Runtime MUST不在完整求解后分别线性插值Knee与Ankle Component Position。Ankle Rotation MUST只按Rotation Weight混合；受影响Physical descendant与Virtual依赖 MUST在同一output workspace重建。退化plane、零长度chain、非法target或数值失败 MUST产生typed failure并阻断FinalPublication，不得使用默认pole、默认axis或上一帧结果。

#### Scenario: Foot Placement Position Weight为一半

- **WHEN** 输入Ankle与目标Ankle之间的Position Weight为0.5
- **THEN** LegIK MUST先得到中间effective target再完整求解
- **AND** 输出上下腿长度 MUST与输入Rig链长一致

#### Scenario: Bend Plane退化

- **WHEN** 最终BendPlaneNormal投影到TargetAxis正交平面后退化
- **THEN** LegIK MUST报告明确solver failure并阻断后续stage
- **AND** MUST不使用世界前方、角色前方或旧Knee Direction继续求解

### Requirement: Body与Presentation重置必须原子清除Foot Placement历史

Foot Placement Runtime MUST跟踪`CharacterBodyPresentationFrame.ResetSequence`。Initialization、CommittedBranchReplacement、SelectedStreamReset、Presentation Reset、dispose、动画尚无正式输出或显式非法pose不连续 MUST在应用新计划前清除脚速历史、surface anchor、constraint、solve weight、prediction workspace和pelvis阻尼，并从当前动画姿势重新锚定。正常producer crossfade MUST不触发硬reset。

#### Scenario: Rollback替换当前Body分支

- **WHEN** Body frame的ResetSequence因CommittedBranchReplacement增加
- **THEN** 两只脚 MUST在同一PresentationFrame释放旧世界锚点
- **AND** MUST不从旧锁点向新Body缓慢拉伸骨骼

#### Scenario: Dodge淡出到Run

- **WHEN** Body没有reset且显式Player正常transition producer
- **THEN** Foot Placement MUST保留合法surface lifecycle
- **AND** policy权重 MUST按FinalAnimationPoseFrame连续变化

### Requirement: Foot Placement 必须与Simulation和Network单向隔离

LocalOwner、SimulatedActor和ObservedActor MUST通过同一Factory、Profile、Foot Placement Runtime与solver合同处理；SourceMode和Camera capability MUST不选择不同IK算法。Foot target、constraint、surface anchor、pelvis offset和solver结果 MUST不进入Character/World state、MotionRequest、WorldSolver、GameplayFact、Blackboard、Snapshot、StateHash或网络packet，也 MUST不写VisualRoot。

#### Scenario: 两个客户端显示同一远端角色

- **WHEN** 两个客户端消费相同authority Body但本地PresentationFrame时刻不同
- **THEN** 两端 MAY各自计算纯视觉Foot Placement
- **AND** 结果差异 MUST不改变任何Gameplay或网络确认

### Requirement: Foot Placement配置和Rig必须显式且通过发布验证

FootPlacement节点 MUST显式引用Profile与Calibration；Definition MUST显式引用Rig v3与唯一Animation Rig Binding；Foot Analysis Source MUST显式引用同一Rig v3、Sampling Rig与Calibration。Rig v3 MUST唯一声明pelvis及左右Hip、Knee、Ankle、Toe Physical BoneId。Calibration Apply和Foot Analyzer MUST在精确Sampling Rig与Preview Pose上执行统一geometry validator并生成稳定validation identity。Foot Analysis artifact identity MUST包含该validation identity；Definition Build MUST拒绝缺失、过期或与当前Rig、Sampling Rig、Calibration revision不匹配的validation identity，并把合法identity发布进Projection。Runtime create MUST精确匹配Projection、Artifact、Calibration与Rig identity，不得访问Sampling Rig，也不得只以数值级`Calibration.RequireValid()`代替已发布几何验证。系统 MUST不按名字、Humanoid Avatar、Prefab旧组件或默认轴补全。

#### Scenario: Calibration数值有限但鞋底方向错误

- **WHEN** Calibration Quaternion合法但Sampling Rig中的sole forward、sole up或平地修正超过geometry边界
- **THEN** Apply、Artifact Build或Definition Build MUST失败
- **AND** Runtime MUST不因字段有限和revision匹配就接受该Calibration

#### Scenario: Runtime与Projection验证identity不同

- **WHEN** Runtime Calibration revision或geometry validation identity与Projection不一致
- **THEN** Runtime create MUST失败并报告精确identity
- **AND** MUST不读取Sampling Rig现场重建或继续使用旧targets

### Requirement: Foot Placement与Leg IK必须提供分层诊断且保持热路径有界

FootPlacement diagnostics MUST只读暴露Body/reset、Calibration/Analysis/validation identity、Pose Completion、唯一作者Weight、生成Foot Features、support、prediction、constraint、surface、目标Ankle、pelvis与最终targets weights。LegIK diagnostics MUST只读暴露匹配targets Completion、输入/输出Physical腿链、BendPlaneNormal、转换后的KneeDirection、Upper/Lower Leg Length、target/effective/solve distance、reach state、residual与typed failure。Pose Watch MUST分别观察FootPlacement应用pelvis后的Component Pose和LegIK已求解Pose；Scene gizmo MUST区分动画输入、FootPlacement目标和LegIK结果。Diagnostics MUST复用固定容量workspace，不得重新分析AnimationClip、query、求解或遍历Transform反推。

#### Scenario: 排查膝盖侧翻

- **WHEN** LegIK输出膝盖方向与动画腿平面不一致
- **THEN** Live Debug MUST同时显示FootPlacement BendPlaneNormal和LegIK KneeDirection
- **AND** Debug读取 MUST不改变当前或下一帧constraint与Pose结果

### Requirement: Preview 必须遵守正式世界上下文边界

Foot Placement Preview MUST通过共享AnimationPreviewRuntime执行同一staged Pose Plan。只有精确CharacterPipelineHost提供匹配Definition、Rig v3、Animation Rig Binding、World-Aware Binding、Body fixture与实际PhysicsScene时，Preview才可执行query与solver。上下文缺失时 MUST在FootPlacement节点报告typed Unavailable，MUST不创建假地面、默认solver或历史Pose。

#### Scenario: 纯动画预览攻击clip

- **WHEN** Timeline或Pose Source预览只有动画资源而没有精确Host world context
- **THEN** 动画source与pure-pose阶段 MAY继续显示
- **AND** FootPlacement输出与FinalAnimationPoseFrame MUST明确Unavailable

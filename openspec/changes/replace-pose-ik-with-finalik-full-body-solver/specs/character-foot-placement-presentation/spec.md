## RENAMED Requirements

- FROM: `### Requirement: Foot Placement规划与Leg IK求解必须在Pose Graph中显式分段`
- TO: `### Requirement: Predictive Foot Placement与Full Body IK必须在Pose Graph中显式分段`
- FROM: `### Requirement: Foot Placement Planner与Leg IK Solver必须使用typed目标合同`
- TO: `### Requirement: Predictive Foot Placement与Full Body IK必须使用typed目标合同`
- FROM: `### Requirement: Leg IK必须保持Physical腿链长度`
- TO: `### Requirement: Full Body IK必须由成熟后端保持biped约束`
- FROM: `### Requirement: Foot Placement与Leg IK必须提供分层诊断且保持热路径有界`
- TO: `### Requirement: Predictive Foot Placement与Full Body IK必须提供分层诊断且保持热路径有界`
- FROM: `### Requirement: Foot Rotation必须应用语义foot frame差值`
- TO: `### Requirement: Foot Rotation必须应用FinalIK Grounding旋转与语义sole frame`
- FROM: `### Requirement: 地面查询必须形成有限连续 Support Envelope`
- TO: `### Requirement: 地面查询必须区分FinalIK当前Grounding与预测Support Envelope`
- FROM: `### Requirement: Pelvis 必须由支撑腿和腿长约束统一求解`
- TO: `### Requirement: Pelvis必须由逐腿可达区间统一规划`

## ADDED Requirements

### Requirement: Predictive Foot Placement的通用Grounding必须复用FinalIK成熟数学

正式`PredictiveFootPlacement` MUST是唯一world-aware Foot Goal Source，并 MUST通过中立FinalIK Grounding backend复用本地`Grounding.Leg`现有的cast组合、velocity prediction基线、命中点/平面到脚高、坡面rotation offset与foot interpolation数学。Backend MAY把`Transform`、`Time.time`和默认Physics调用替换为显式Component Transform、frame delta、精确PhysicsScene、self-collider filter与固定命中workspace；它 MUST先生成不受动画`PlantConfidence`连续缩放的完整Current Grounding结果，`GroundingFootInput` MUST不携带`PlantWeight`或同义权重。Backend MUST把stock pelvis lower/lift权重固定为零且不发布stock pelvis结果，也 MUST不把上述脚部数学复制到项目新类或用第二套结果覆盖。`GrounderFBBIK`组件 MUST不进入正式Runtime。

FinalIK Grounding没有的动画Foot Feature、source contribution、相位驱动Future Landing、Current/Future Support、Ground Envelope、surface identity、moving surface anchor、Free/Locked/Sliding与逐腿可达区间 MUST由同一节点内范围明确的Project Predictive Extension补充。Project Predictive Extension MUST不重新计算与FinalIK竞争的当前脚高、坡面rotation或foot smoothing；Pelvis Reach Planner MUST只消费最终Foot Goal与Rig腿长，不得query world。系统 MUST不把FinalIK的简单velocity prediction表述为动画相位落点预测。

#### Scenario: 当前脚站到斜坡

- **WHEN** 当前heel/toe Grounding请求命中合法斜坡且Foot Placement Weight有效
- **THEN** FinalIK Grounding backend MUST产生脚高与坡面rotation基础结果
- **AND** Project Predictive Extension MUST只添加contact、support、anchor和Goal权重语义而不第二次计算坡面对齐

#### Scenario: 移动动画中的摆动脚跨过低一级踏面

- **WHEN** 最终表现姿势的摆动脚仍有合法Current Grounding命中且Body Grounded
- **THEN** FinalIK Grounding Goal MUST保留动画Ankle相对Root参考平面的离地高度，并按Foot Placement Weight应用踏面相对Root的高度与rotation变化
- **AND** 该摆动脚在未进入Plant Contact时 MUST不创建plant anchor、lock或slide约束
- **AND** 该摆动腿在`AllPlantedFeet`模式下 MUST不参与Pelvis Reach Planner

#### Scenario: FinalIK缺少Future Landing语义

- **WHEN** 动画Foot Feature提供下一次落脚delay与local offset
- **THEN** Project Predictive Extension MUST构造Future Landing与Ground Envelope请求并通过同一world-aware节点发布结果
- **AND** 系统 MUST明确把该部分诊断为Project Predictive Extension而不是FinalIK stock prediction

#### Scenario: Grounding接入要求复制核心数学

- **WHEN** 实施审计发现显式world query或Pose输入必须复制FinalIK脚高、rotation或foot interpolation方程
- **THEN** 本change实施 MUST停止并报告精确源码依赖
- **AND** MUST不保留项目重复Grounding、Grounder组件或旧Foot Placement作为fallback

### Requirement: Placement、Plant Support与Contact必须使用独立信号

每脚 MUST同时保留彼此独立的`PlacementWeight`、`PlantConfidence`、`AnimationFootSpeed`、`PlantSupportWeight`与`ContactWeight`。`PlacementWeight` MUST只由唯一Foot Placement作者权重、Body Grounded和合法Current Grounding命中决定，并 MUST唯一控制未约束FinalIK Grounding Foot Goal的Position/Rotation Weight。Runtime MUST不使用脚速、Plant Confidence、sole到surface距离或首帧历史状态连续缩放普通Foot Goal。

`PlantConfidence` MUST只表达最终Pose contribution中混合后的源动画接触意图，并只通过显式enter/exit迟滞参与Plant Contact状态。`AnimationFootSpeed` MUST等于最终Pose contribution中已按source权重和visual time scale混合的烘焙`SoleLocalVelocity.magnitude`；Runtime MUST不把它与Body可见线速度、actor世界平移、yaw点速度或相邻最终sole世界位置差拼接。Profile MUST只提供严格有序的`PlantSpeedThreshold`与`UnalignmentSpeedThreshold`。Plant Contact进入 MUST要求`PlantConfidence`达到Enter且`AnimationFootSpeed <= PlantSpeedThreshold`；退出 MUST在`PlantConfidence`达到Exit或`AnimationFootSpeed >= UnalignmentSpeedThreshold`时发生。

`PlantSupportWeight` MUST在Plant Contact成立时等于`PlacementWeight`，否则为0，并 MUST只表达Pelvis Reach Planner的普通支撑腿选择。`ContactWeight` MUST只在Plant Contact成立、Plant Policy允许约束且surface anchor有效时使用，并 MUST在两个速度阈值间连续渐退，只控制anchor、lock与slide；`Unlocked`策略下 MUST为0。系统 MUST删除旧`PlantWeight`、`GroundAlignmentWeight`、world planar/vertical速度门控、surface distance门控、`0.5 -> 1`连续重映射、旧Plant/Release与Alignment速度距离字段及兼容别名。

#### Scenario: Run混合得到中间Plant Confidence

- **WHEN** 左脚`PlantConfidence`为`0.65`、Body Grounded且Current Grounding命中合法踏面
- **THEN** 左脚Placement Weight MUST等于有效Foot Placement Weight
- **AND** MUST不因`InverseLerp(0.5, 1, 0.65)`把Goal权重压成`0.3`

#### Scenario: 持续输入让actor世界速度升高

- **WHEN** 角色持续跑动导致左右sole世界位置都包含actor平移
- **THEN** 普通Foot Goal Weight MUST继续等于Placement Weight，不得因actor世界速度归零
- **AND** Plant Contact MUST只读取混合后的动画Sole Local Velocity与Plant Confidence

#### Scenario: Unlocked普通基线

- **WHEN** Corin使用`Unlocked`且Body Grounded并有合法Current Grounding命中
- **THEN** Foot Goal MUST按Placement Weight应用FinalIK Grounding目标
- **AND** Contact Weight MUST为0且不得创建anchor、lock或slide

## MODIFIED Requirements

### Requirement: Rig Calibration必须同时约束Editor分析与Runtime Solver

Editor Foot Analyzer MUST显式引用Rig Definition v4、Sampling Rig与Calibration v4；Runtime MUST通过同一Rig v4和通用Animation Rig Binding解析骨骼，通过World-Aware Binding取得self-collider排除与world fixture。Calibration v4 MUST以每脚heel/toe contact offset及由heel-to-toe与VisualRoot up派生的单一ankle-local sole frame rotation作为唯一鞋底几何真相，MUST不保存preferred bend、Knee Direction、pole或solver orientation。四肢solver root、spine、arm/leg chain与reference bend plane MUST只属于Rig v4和FullBodyIK backend。Artifact、Projection与Runtime MUST精确匹配Rig、Sampling Rig和Calibration三方identity/revision。Calibration或Rig变化 MUST使Projection stale；系统 MUST不允许Editor与Runtime分别维护contact、sole frame或biped bone mapping。

#### Scenario: 作者修改Corin左脚toe sole offset

- **WHEN** Calibration content revision改变
- **THEN** 全部引用该Calibration的Definition Projection MUST变为Stale
- **AND** 全部Runtime Prefab MUST继续引用同一资产而不复制新值

#### Scenario: 作者需要修正膝盖弯曲

- **WHEN** Rig v4 reference bend plane退化或方向不合法
- **THEN** 作者 MUST在Rig reference pose或FullBodyIK Profile边界修正并重新Build
- **AND** Foot Calibration MUST不出现Knee Bend、Pole或Preferred Bend编辑字段

### Requirement: Rig Calibration必须在精确Sampling Rig上下文可视化编辑

Sole frame MUST只通过heel/toe接触点间接编辑。Editor MUST以heel-to-toe平面投影作为前轴、VisualRoot up作为上轴自动派生完整frame，并 MUST不提供独立rotation handle。

系统 MUST从`CharacterFootPlacementAnalysisSource`提供显式`Edit Rig Calibration`作者入口，并以该Source精确引用的Sampling Rig和Calibration建立唯一Editor session。Scene View MUST只允许编辑左右heel/toe contact，并只读显示sole frame、统一参考地面、sole长度、左右手性和参考平地ankle correction。Scene View MUST不显示或编辑preferred bend、Knee Direction、pole target或solver chain。正式提交 MUST通过统一鞋底geometry validator，再以单次Undo更新Calibration content revision和dirty；非法draft MUST保留旧正式数据。系统 MUST不允许作者在缺少精确Analysis Source/Sampling Rig上下文时编辑裸几何坐标，也 MUST不在`OnInspectorGUI`、selection、repaint或handle拖动期间执行AnimationClip分析、artifact rebuild、Compile或Build。

`CharacterFootPlacementAnalysisSource` MUST显式配置持久化的Calibration Preview Clip与归一化预览时间。进入校准session时，Editor MUST在独立Animation Mode driver拥有的临时PlayableGraph中把该固定帧采样到Sampling Rig；退出、切换Prefab Stage或采样失败时 MUST恢复进入前姿势并释放preview graph。Preview Pose MUST只改变作者看到和操作的鞋底姿势，MUST不生成第二套Calibration数据，也 MUST不进入Runtime Foot Placement或FullBodyIK链路。

#### Scenario: 作者校准Corin右脚鞋底

- **WHEN** 作者从Corin Analysis Source执行`Edit Rig Calibration`
- **THEN** Scene View MUST在精确Corin Sampling Rig上只允许编辑右脚heel/toe并显示自动sole frame
- **AND** Apply MUST只写入该Source引用的唯一Calibration v4资产

### Requirement: 腿部弯曲稳定必须保留动画平面并使用有限伸展区间

Predictor MUST从最终动画hip、knee和ankle姿势与目标脚位置计算有限`LegExtensionRatio`。`CharacterFootPlacementProfile` MUST显式提供严格有序的最小/最大可达伸展比例。Locked或Sliding锚点低于最小或超过最大范围时，Predictor MUST在同一表现帧释放旧锚点并返回FinalIK Grounding当帧目标，不得把旧锚点提交给FullBodyIK硬拉，也不得因为旧锚点失效而把合法的普通Grounding脚目标权重硬切为零。Predictor MUST不生成或保存Knee Direction、BendPlaneNormal、PreferredBendPlaneNormal或pole。

FullBodyIK MUST使用FinalIK既有bend constraint，以Rig v4非退化reference plane初始化，并从本帧输入动画pose、目标轴与effector rotation保持四肢弯曲连续。Foot Placement Profile、Calibration和Goal Set MUST不复制solver bend orientation。Reference plane退化、零长度chain、非法target或数值失败 MUST产生typed failure并阻断FinalPublication，不得使用世界前方、角色前方或上一帧方向补值。

#### Scenario: 正常Walk动画膝盖弯曲清晰

- **WHEN** leg extension位于Profile安全范围且输入动画腿平面有限
- **THEN** Predictor MUST只发布合法Foot goal
- **AND** FullBodyIK MUST由FinalIK bend constraint保留动画弯曲连续性

#### Scenario: 锁定脚目标超过最大可解伸展

- **WHEN** Locked或Sliding锚点的Leg Extension Ratio超过Profile最大值
- **THEN** Predictor MUST释放旧锚点并记录decision reason
- **AND** 同帧Foot goal MUST使用FinalIK Grounding当帧目标及其合法权重
- **AND** FullBodyIK MUST不接收一个被强制clamp到腿长极限的旧锚点

### Requirement: Foot Rotation必须应用FinalIK Grounding旋转与语义sole frame

FinalIK Grounding backend MUST是当前地面法线对齐、最大旋转角限制与rotation interpolation的唯一计算权威。Adapter MUST以动画ankle rotation和Calibration v4的ankle-local semantic sole frame解释Grounding返回的world-space rotation offset，并把该offset应用到动画ankle rotation后发布Foot Goal；它 MUST不把surface frame直接赋给ankle骨，也 MUST不假定ankle局部轴就是sole forward/up。Calibration转换只负责在ankle frame与semantic sole frame之间表达同一个FinalIK rotation offset，MUST不重算坡面normal、最大角度或插值。Project Predictive Extension MUST不再拥有另一套`Quaternion.LookRotation`坡面对齐、速度响应、ascent/descent rotation或ankle twist smoothing算法。

#### Scenario: Corin ankle骨轴不是标准forward/up

- **WHEN** Calibration声明的semantic sole frame与ankle骨局部轴不同且FinalIK Grounding命中合法斜坡
- **THEN** Goal应用后semantic鞋底 MUST按FinalIK Grounding rotation offset对齐support normal
- **AND** ankle骨 MUST保留rig-specific固定旋转关系

#### Scenario: 预测扩展试图覆盖当前坡面旋转

- **WHEN** Project Predictive Extension产生与FinalIK Grounding当前脚rotation不同的第二rotation结果
- **THEN** Validator或Runtime MUST明确失败
- **AND** MUST不按confidence、命中数量或节点顺序择优

### Requirement: 地面查询必须区分FinalIK当前Grounding与预测Support Envelope

每只脚的当前地面采样 MUST只由FinalIK Grounding按已选择Quality执行其stock Ray、heel/toe/side Ray或heel Ray加Capsule组合，并通过项目唯一world-query backend访问精确PhysicsScene、Profile LayerMask、自碰撞排除和固定容量命中页。FinalIK Grounding输出的current hit、脚高与rotation MUST是唯一当前Grounding结果。Project Predictive Extension MAY从这些current hits派生`CurrentSupport`、surface identity和contact证据，但 MUST不再次查询并覆盖当前脚高或rotation。

当Plant Policy需要脚掌双支点时，同一个FinalIK Grounding owner MAY在`Fastest`或`Best`质量下额外执行一个typed secondary Toe Ray，并发布稳定surface identity的toe plant hit。该命中 MUST只服务plant point、heel lift与toe pivot，不得参与或覆盖stock质量的grounded裁决、脚高、坡面rotation或foot interpolation。系统 MUST不以Project Predictive Extension的第二查询或默认平面补建toe plant hit。

#### Scenario: Best质量在台阶边缘取得脚尖支点

- **WHEN** stock heel Ray与foot-center Capsule生成合法当前Grounding结果且secondary Toe Ray命中同一合法踏面
- **THEN** Goal Source MUST同时保留stock ankle目标与独立toe plant point
- **AND** toe命中 MUST不改变stock脚高或坡面rotation
- **AND** Diagnostics MUST分别显示Heel、Toe与Foot Center查询用途和命中

Future Landing位置与动画脚路径查询 MUST由Project Predictive Extension使用独立typed Future Landing与Path Sample请求执行，因为FinalIK Grounding不提供该语义。两类预测请求 MUST与Grounding共用同一个world-query backend、Layer裁决、self-collider filter、stable surface identity和fixed hit page合同。预测结果 MUST只生成`FutureLandingSupport`、有序Ground Envelope segment与每段minimum allowed sole height，不得覆盖Current Support或把路径最远命中直接当当前脚目标。所有结果 MUST来自合法有限命中，不得使用隐藏Collider、默认平面或fallback。

Foot Placement正式查询Mask MUST包含普通共享`Ground`与真实踏面`FootPlacementSurface`，并 MUST排除Gameplay专用`CharacterTraversal`。连续楼梯的无Renderer Traversal Ramp MUST不成为Current Support、Future Landing Support、Ground Envelope或Locked Surface；系统 MUST不同时查询Ramp和踏面后按优先级择优。

#### Scenario: 脚跨过两个楼梯边缘

- **WHEN** 预测路径存在多个高度连续的合法踏面
- **THEN** Project Predictive Extension MUST保留surface和edge分段顺序
- **AND** Free脚只在动画Y低于minimum envelope时抬高
- **AND** 当前脚X/Z及FinalIK当前Grounding结果 MUST不被FutureLandingSupport替换

#### Scenario: 预测路径跨越不可达高差

- **WHEN** 相邻候选高度、edge gap或reach超过Profile允许范围
- **THEN** 后续segment MUST被裁剪并记录明确原因
- **AND** FutureLandingSupport MUST不跨越该中断

#### Scenario: Body沿Gameplay Ramp上楼

- **WHEN** KCC Body沿`CharacterTraversal`连续升高且脚下存在`FootPlacementSurface`真实踏面
- **THEN** 当前Grounding与预测Support MUST只查询合法真实踏面Layer
- **AND** MUST不使用Ramp法线或Ramp高度替代可见踏面

#### Scenario: Foot Placement Profile包含CharacterTraversal

- **WHEN** Profile配置会让Foot查询命中Gameplay Ramp
- **THEN** Profile或楼梯组合校验 MUST失败并报告冲突Layer
- **AND** Runtime MUST不以踏面优先级或Collider名称消解重叠命中

### Requirement: Plant Policy必须显式决定脚掌锁定与支点

`CharacterFootPlacementProfile` MUST显式声明`Unlocked`、`PivotAroundToe`、`PivotAroundAnkle`或`LockRotation`之一，不得由Runtime按命中数量或地形类型切换policy。`PivotAroundToe` MUST使用Calibration v4 toe contact和同一Grounding owner发布的toe plant hit保存移动表面局部锚点；目标rotation变化时 MUST围绕该toe plant point反推ankle目标。`PivotAroundAnkle` MUST锁定ankle position并接受当前Grounding rotation；`LockRotation` MUST同时锁定ankle position与rotation；`Unlocked` MUST不创建plant anchor。

Profile MAY通过`AdjustHeelBeforePlanting`让未锁定但toe plant hit合法的脚提前绕toe point适配当前Grounding rotation，并 MUST通过`HeelLiftRatio`在普通ankle offset与toe-preserving offset之间连续混合。`HeelLiftRatio` MUST进入Live Tuning且只影响下一表现帧；Plant Policy与`AdjustHeelBeforePlanting`属于正式作者配置，修改后 MUST重新产生Profile revision和Projection依赖，不得作为隐藏Runtime开关。

#### Scenario: Corin普通基线站在同一斜坡踏面

- **WHEN** Corin配置`Unlocked`、`AdjustHeelBeforePlanting=false`且Body Grounded并且当前脚命中合法踏面
- **THEN** Goal Source MUST以Placement Weight应用FinalIK Grounding ankle Position/Rotation目标
- **AND** Goal Source MUST不创建anchor、toe plant pivot或提前Heel Lift
- **AND** Pelvis MUST只来自有效Plant Support或Contact支撑脚参与的逐腿可达区间

#### Scenario: Toe命中丢失

- **WHEN** 当前Grounding仍合法但secondary Toe Ray没有合法命中
- **THEN** Goal Source MUST使用stock ankle目标而不发布Toe Plant Pivot
- **AND** MUST不使用默认平面、旧toe命中或第二查询补点

### Requirement: Pelvis必须由逐腿可达区间统一规划

Pelvis基础结果 MUST且只能由同一`PredictiveFootPlacement`中的Pelvis Reach Planner计算。Planner MUST按每腿Hip、动画Ankle、最终Foot Goal、Position Weight、Rig reference leg length、minimum extension ratio与maximum extension ratio计算允许的竖直pelvis offset区间，并按Profile的`AllLegs`、`AllPlantedFeet`或`DirectionalSlopeSupport`模式选择贡献腿。每腿支撑权重 MUST为`max(PlantSupportWeight, ContactWeight)`，MUST不直接读取或重映射`PlantConfidence`，也 MUST不把普通Placement Weight自动视为`AllPlantedFeet`支撑。Planner MUST从贡献脚的最终Foot Goal相对动画Ankle的竖直变化和支撑权重求唯一首选高度：有效目标没有共同竖直变化时首选高度为0，目标共同抬高或降低时骨盆 MUST连续跟随，双脚高低不同时 MUST按贡献权重平衡。区间有交集时 MUST把首选高度夹入共同区间；区间无交集或单腿目标超出最大水平调整/最大升降范围时 MUST保留主要支撑脚并把不可满足Foot Goal权重清零，不得通过无限下蹲、拉长腿或上一帧目标掩盖冲突。

Planner MUST通过Profile的最大降低、最大抬升、插值速度与dead zone连续更新唯一状态，并 MUST显式使用`FollowBody`或`HoldWorldDuringInterpolation` Actor Movement Compensation Mode。`FollowBody`不得从pelvis offset中扣除actor/root位移；`HoldWorldDuringInterpolation` MAY按VisualRoot up扣除有限root delta。FinalIK stock `lowerPelvisWeight`、`liftPelvisWeight`、`pelvisSpeed`与`pelvisDamper` MUST退出Profile且在adapter中固定为不产生输出。Planner MUST不query world、不重新计算脚高/rotation、不执行IK。结果 MUST作为唯一`PelvisPreSolveTranslation` Goal发布；PredictiveFootPlacement MUST不写pelvis Pose，FullBodyIK MUST在同一个Pending output中先应用该Component Space translation再设置effectors并执行一次FBBIK。

Pelvis translation MUST沿VisualRoot Component up表达。Pose Buffer adapter MUST把该translation转换到pelvis父骨空间后叠加本帧动画local position，不得假定父骨local Y是角色竖直方向。Planner与FullBodyIK MUST不产生水平pelvis位移，也 MUST不旋转pelvis、spine或VisualRoot。

#### Scenario: Pelvis父骨带有预旋转

- **WHEN** 角色骨架的pelvis父骨local Y不与VisualRoot up轴重合
- **THEN** Pelvis Reach Planner产生的正负vertical offset MUST仍只沿VisualRoot Component up移动pelvis
- **AND** backend MUST不把竖直补偿转成横向或前后位移

#### Scenario: 左脚踏上更高台阶

- **WHEN** 左脚Plant Goal与右脚Plant Goal的逐腿允许区间存在交集
- **THEN** Pelvis Reach Planner MUST按两脚相对动画Ankle的竖直Goal变化与支撑权重求首选offset并夹入交集
- **AND** FullBodyIK MUST只应用该一个pelvis pre-solve Goal

#### Scenario: 双脚共同踩上高平台

- **WHEN** 左右Plant Foot Goal相对各自动画Ankle产生相同方向的有限上移且共同可达区间合法
- **THEN** Pelvis Reach Planner MUST按Goal权重连续上移骨盆而不是保持0
- **AND** 高度差 MUST不全部由双膝压缩吸收

#### Scenario: Body发生突然竖直移动

- **WHEN** 显式root component delta包含有限竖直位移且Profile选择`FollowBody`
- **THEN** Pelvis Reach Planner MUST保持相对动画Pose的当前平滑offset，不得反向扣除该位移
- **AND** Runtime MUST不存在stock damper或第二Actor Movement Compensation状态

#### Scenario: 双腿目标没有共同可达区间

- **WHEN** 左右Foot Goal的允许pelvis区间不相交
- **THEN** Planner MUST按Plant Support/Contact权重、区间到0的距离与支撑高度稳定选择主要支撑脚
- **AND** 次要Foot Goal MUST以`PelvisRangeConflictReleased`原因清零Position与Rotation Weight

### Requirement: Predictive Foot Placement与Full Body IK必须在Pose Graph中显式分段

启用预测式Foot Placement的Character Presentation Pose Graph MUST显式包含一个`PredictiveFootPlacement`Goal Source与一个`FullBodyIK`solver。`PredictiveFootPlacement` MUST是每个最终Output路径唯一有状态`WorldAwareValue`目标生成节点，接收原始Component Pose与唯一Foot Placement Weight，通过FinalIK Grounding backend和Project Predictive Extension只输出同帧typed `component.full-body-ik-goals`，MUST不输出/改写Pose或执行IK。其它Goal Source MAY从同一Component Pose分支读取并发布Goal value。`FullBodyIK` MUST是无world query的`PurePose`节点，同时消费原始Component Pose与全部Goal Sets，在一次FinalIK FBBIK中修改Rig v4 Physical biped chain并输出Solved Component Pose。Compiler MUST把Pose edge和Goal value edge编译为同一DAG，MUST不把Goal Source的有序调度描述为多个IK串联，也 MUST禁止图外FinalIK组件链。每个Foot Goals输出 MUST且只能由同call-site的一个FullBodyIK消费。

#### Scenario: 一个表现帧更新Corin

- **WHEN** PredictiveFootPlacement完成world query、contact、pelvis plan与Body/Feet goals
- **THEN** Runtime MUST把Foot Goals和显式Hand Goals交给同一个FullBodyIK stage
- **AND** FinalAnimationPoseFrame MUST只在FullBodyIK及全部后续stage完成后发布

#### Scenario: PredictiveFootPlacement缺少FullBodyIK消费方

- **WHEN** 到达OutputPose的图路径包含PredictiveFootPlacement但其Goals未连接FullBodyIK
- **THEN** Graph Validator与Build MUST拒绝该图
- **AND** Runtime MUST不隐藏补建solver或忽略Goals

### Requirement: Predictive Foot Placement与Full Body IK必须使用typed目标合同

`CharacterFootPlacementPlanner` MUST是FinalIK Grounding backend与Project Predictive Extension的唯一编排边界，只根据正式输入和world query生成vendor-neutral`CharacterFootPlacementPlan`，不得求解Physical biped chain或写Pose/Transform。Plan MUST区分FinalIK Grounding结果与Project Predictive Extension结果，并把最终pelvis pre-solve translation、左右Foot target Component Transform、position/rotation weight、extension ratio、constraint state与decision reason发布为同帧固定workspace中的`component.full-body-ik-goals`。`FullBodyIK` MUST只根据输入Component Pose、Rig v4、FullBodyIK Profile与全部Goal Sets执行一次FinalIK FBBIK，不得查询world、决定contact lifecycle、重新计算pelvis plan或读取第二Foot Placement Weight。Goal Sets MUST携带Frame、Completion与Rig identity，不得跨帧、跨Rig、序列化进作者资产或进入Gameplay/Network状态。

#### Scenario: FullBodyIK应用一帧Foot目标

- **WHEN** PredictiveFootPlacement发布匹配当前Pose Completion与Rig revision的Body/Feet goals
- **THEN** FullBodyIK MUST在独立output workspace与Hand goals一起求解biped Pose
- **AND** final writer之前 MUST不存在Transform写入

#### Scenario: Pose与Goals来自不同call-site

- **WHEN** FullBodyIK的Component Pose和Foot Goals不共享当前Frame、Completion或Rig lineage
- **THEN** Validator或Runtime stage MUST明确失败
- **AND** MUST不按最新Goals、节点顺序或Rig名称猜测配对

### Requirement: Full Body IK必须由成熟后端保持biped约束

FullBodyIK MUST使用FinalIK FBBIK现有chain、effector、bend constraint、FABRIK/trigonometric pass和mapping数学完成Body、双臂与双腿联动。输入Physical segment reference length MUST来自Rig v4；FullBodyIK MUST不通过分别线性插值Knee/Ankle或Elbow/Hand Component Position伪造部分解。Foot Goal MUST显式标记`GroundingEffectorTarget`应用语义：position以pelvis pre-solve后的foot bone到Grounding目标的差值写入`positionOffset`，rotation在FBBIK ReadPose前按Goal Rotation Weight预乘到Ankle，且对应effector position/rotation weight保持为零；受影响Physical descendant与Virtual依赖 MUST在同一output workspace重建。Invalid Rig、退化reference plane、非法Goal、mapping失败或数值失败 MUST产生typed failure并阻断FinalPublication，不得使用项目自研TwoBone、默认pole或上一帧结果。

#### Scenario: Foot Position Weight为一半

- **WHEN** LeftFoot Goal Position Weight为0.5
- **THEN** FinalIK left foot effector positionOffset MUST应用从pelvis平移后foot bone到Grounding目标差值的0.5
- **AND** left foot effector positionWeight MUST保持为0以保留stock GrounderFBBIK bend语义
- **AND** Body、spine与其它effectors MUST继续按同一solver约束联动

#### Scenario: Rig reference plane退化

- **WHEN** Rig v4无法初始化Right Leg bend constraint
- **THEN** Runtime preparation MUST报告明确binding failure并拒绝Actor Animation Runtime
- **AND** MUST不恢复旧LegIK或自研解析式solver

### Requirement: Foot Placement配置和Rig必须显式且通过发布验证

PredictiveFootPlacement节点 MUST显式引用唯一Foot Placement Profile与Calibration v4；Foot Placement Profile MUST在同一资产中明确分组FinalIK Grounding-backed设置与Project Predictive Extension设置，并 MUST显式保存FinalIK Quality、Pelvis Height Mode、Actor Movement Compensation Mode、最大升降范围、pelvis interpolation speed、dead zone与最大水平脚调整，不得保存stock Pelvis Lower/Lift/Damper/Speed、Grounder组件副本、backend选择或fallback。Corin MUST配置`Best`、`AllPlantedFeet`与`FollowBody`，Runtime MUST不按性能、平台或命中结果自动降级或切换。Definition MUST显式引用Rig v4、唯一Animation Rig Binding与FullBodyIK Profile；Foot Analysis Source MUST显式引用同一Rig v4、Sampling Rig与Calibration v4。Rig v4 MUST唯一声明Solver Root、Pelvis、ordered Spine、左右Arm、左右Hip/Knee/Ankle/Toe及可选Head/Clavicle Physical BoneId。Calibration Apply和Foot Analyzer MUST在精确Sampling Rig与Preview Pose上执行统一鞋底geometry validator并生成稳定validation identity。Rig Apply或Build MUST独立验证完整FBBIK biped binding与reference bend planes。Foot Analysis artifact identity MUST包含当前Rig v4与Calibration v4 validation identity；Definition Build MUST拒绝缺失、过期或不匹配的identity，并把合法identity、Grounding backend identity与FullBodyIK backend/profile identity发布进Projection。系统 MUST不按名字、Humanoid Avatar、FinalIK auto-detect、Prefab旧组件或默认轴补全。

#### Scenario: Calibration数值有限但鞋底方向错误

- **WHEN** Calibration Quaternion合法但Sampling Rig中的sole forward、sole up或平地修正超过geometry边界
- **THEN** Apply、Artifact Build或Definition Build MUST失败
- **AND** Runtime MUST不因字段有限和revision匹配就接受该Calibration

#### Scenario: Rig v4缺少FullBodyIK左臂

- **WHEN** Corin仍有LeftHand Goal但Rig没有合法Left Arm chain
- **THEN** Definition Build MUST失败并报告Goal与缺失Rig slot
- **AND** MUST不保留旧TwoBoneIK处理该手臂

### Requirement: Predictive Foot Placement与Full Body IK必须提供分层诊断且保持热路径有界

PredictiveFootPlacement diagnostics MUST只读分组暴露FinalIK Grounding backend identity、current query requests/hits、stock velocity prediction、脚高/rotation，以及Project Predictive Extension的Foot Features、Plant Contact迟滞、Animation Foot Speed、surface distance、Current/Future Support、Ground Envelope、surface anchor、constraint、左右腿允许pelvis区间、target/resolved pelvis offset、冲突释放、最终Foot目标和彼此独立的Placement、Plant Support、Contact weights。Body Grounded诊断 MUST分别暴露Target、Before与After来源，不得只发布合并结果。FullBodyIK diagnostics MUST只读暴露匹配Goal Set Completion、backend identity、Profile revision、输入/输出Physical biped Pose、每effector目标/权重/residual、chain reach、bend constraint、iterations与typed failure。Pose Watch MUST分别观察Goal Source输入Pose与FullBodyIK solved Pose；Target Watch MUST观察Goal Set。Scene gizmo MUST区分动画输入、FinalIK Grounding当前命中、预测扩展目标和FullBodyIK结果。Diagnostics MUST复用固定容量workspace，不得重新query、求解或遍历Transform反推。

#### Scenario: 排查膝盖侧翻

- **WHEN** FullBodyIK输出膝盖方向与输入动画腿平面不一致
- **THEN** Live Debug MUST同时显示输入reference plane、Foot effector与FinalIK bend constraint结果
- **AND** Debug读取 MUST不改变当前或下一帧constraint与Pose结果

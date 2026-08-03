# character-foot-placement-presentation Specification Delta

## ADDED Requirements

### Requirement: Rig Calibration必须在精确Sampling Rig上下文可视化编辑

系统 MUST从`CharacterFootPlacementAnalysisSource`提供显式`Edit Rig Calibration`作者入口，并以该Source精确引用的Sampling Rig和Calibration建立唯一Editor session。Scene View MUST允许编辑左右heel/toe contact与preferred bend direction，并按`ProjectOnPlane(toe - heel, VisualRoot.up)`前轴和`VisualRoot.up`上轴唯一自动派生sole frame；系统 MUST删除sole frame手动旋转入口。Scene View MUST只读显示自动sole frame、统一参考地面、sole长度、左右手性、hip-knee-ankle弯曲平面和参考平地ankle correction。正式提交 MUST先通过统一几何validator，再以单次Undo更新Calibration content revision和dirty；非法draft MUST保留旧正式数据。系统 MUST不允许作者在缺少精确Analysis Source/Sampling Rig上下文时编辑裸几何坐标，也 MUST不在`OnInspectorGUI`、selection、repaint或handle拖动期间执行AnimationClip分析、artifact rebuild、Compile或Build。

Analysis Source MUST显式引用一个Calibration Preview AnimationClip与固定归一化时间。进入校准session时，系统 MUST以独立Animation Mode driver和PlayableGraph在Sampling Rig上显示该固定姿势；关闭session、切换Prefab Stage或发生异常时 MUST停止该driver并恢复全部动画属性。预览 MUST不保存Sampling Rig骨骼Override、不修改AnimationClip、不进入Runtime Projection，也 MUST不成为第二份contact、sole frame或bend direction数据。

#### Scenario: 作者校准Corin右脚鞋底

- **WHEN** 作者从Corin Analysis Source执行`Edit Rig Calibration`
- **THEN** Scene View MUST在精确Corin Sampling Rig上显示右脚heel/toe、sole frame和膝盖首选方向
- **AND** Apply MUST只写入该Source引用的唯一Calibration资产

#### Scenario: 作者完成heel与toe接触点定位

- **WHEN** 作者移动任一heel或toe接触点且鞋底基线有限
- **THEN** Editor MUST立即从接触基线和VisualRoot up自动重新生成该脚sole frame
- **AND** 作者 MUST不需要也不能通过独立rotation handle维护第二个方向

#### Scenario: 校准draft的toe点悬在heel上方

- **WHEN** heel/toe统一地面误差超过正式边界
- **THEN** Apply MUST拒绝该draft并显示右脚实测误差和允许边界
- **AND** MUST不更新Calibration revision或自动重建artifact

#### Scenario: Inspector持续重绘

- **WHEN** 作者选中Analysis Source或Calibration但没有执行显式命令
- **THEN** Inspector MUST只读取轻量状态和最近验证结果
- **AND** MUST不实例化Playable、遍历AnimationClip或发布任何生成产物

#### Scenario: Generic Rig使用Idle姿势校准

- **WHEN** 作者从Corin Analysis Source进入校准且Source配置了正式Idle clip固定帧
- **THEN** Scene View MUST在该Idle姿势中显示同一Sampling Rig与校准handle
- **AND** 关闭校准后 MUST恢复Prefab绑定姿势且不产生骨骼Override

### Requirement: 腿部弯曲稳定必须保留动画平面并使用有限伸展区间

Planner MUST从最终动画hip、knee和ankle姿势计算每脚animated bend normal，并从目标脚位置和有限leg length计算`LegExtensionRatio`。`CharacterFootPlacementProfile` MUST显式提供`MinimumLegExtensionRatio`、`MaximumLegExtensionRatio`、`BendStabilizationStartRatio`、`BendStabilizationFullRatio`与`MaximumBendStabilizationWeight`，且它们 MUST形成严格有序的有限可解区间。Calibration的preferred bend direction MUST只用于生成vendor-neutral `PreferredBendNormal`；Plan MUST分别输出position weight、rotation weight与`BendStabilizationWeight`。

在安全伸展区间内，`BendStabilizationWeight` MUST为零并保留最终动画弯曲平面。接近过度伸直、过度压缩或动画弯曲平面退化时，Planner MUST按Profile连续增加有限稳定权重，并输出typed decision reason。目标超出最小或最大可解伸展范围时，Planner MUST按现有约束生命周期当帧释放或拒绝该脚并将position、rotation和bend权重归零，MUST不把不可解目标提交给骨骼solver硬拉。Foot Placement作者Weight MUST对最终position、rotation、pelvis和bend求解各应用一次，不得让bend权重直接复制position权重。

#### Scenario: 正常Walk动画膝盖弯曲清晰

- **WHEN** leg extension位于Profile安全区间且animated bend normal有限
- **THEN** Plan的Bend Stabilization Weight MUST为零
- **AND** solver MUST保留动画自己的膝盖弯曲平面

#### Scenario: 脚目标接近腿完全伸直

- **WHEN** Leg Extension Ratio从Stabilization Start接近Stabilization Full
- **THEN** Planner MUST连续混向Calibration preferred bend normal并限制最大权重
- **AND** MUST不在单帧把膝盖切换到静态reference pole

#### Scenario: 锁脚目标超过最大腿长

- **WHEN** 目标Leg Extension Ratio超过Maximum Leg Extension Ratio
- **THEN** 该脚 MUST在同一表现帧按Unreachable原因进入Free并输出三个零权重
- **AND** 解析式Limb Pose Solver MUST不收到需要硬拉的目标

#### Scenario: 目标让腿过度压缩

- **WHEN** 目标Leg Extension Ratio低于Minimum Leg Extension Ratio
- **THEN** Planner MUST拒绝该不可解目标并记录CompressedBeyondLimit
- **AND** MUST不依赖骨骼Solver自行选择翻面方向

## MODIFIED Requirements

### Requirement: Rig Calibration必须同时约束Editor分析与Runtime Solver

Editor Foot Analyzer的Sampling Rig、Rig v3与Runtime `CharacterAnimationRigBinding` MUST引用同一Calibration identity、schema和content revision。Projection MUST保存该identity，Runtime创建Foot Placement operation前 MUST精确匹配。Calibration变化 MUST使Projection stale；系统 MUST不允许Editor与Runtime分别维护heel/toe contact、sole frame、preferred bend direction或腿链。

Calibration正式提交、Editor Foot Analyzer、Definition Build与Runtime composition MUST使用同一几何验证合同。Identity一致但Sampling Rig几何验证失败时，系统 MUST拒绝分析、发布或创建Runtime，MUST不将“使用同一份错误数据”视为合法配置。

#### Scenario: 作者修改Corin左脚toe contact

- **WHEN** Calibration content revision改变
- **THEN** 全部引用该Calibration的Definition Projection MUST变为Stale
- **AND** 全部Runtime Prefab MUST继续引用同一资产而不复制新值

#### Scenario: Editor与Runtime引用相同但几何非法的Calibration

- **WHEN** 相同Calibration中的sole frame与Sampling Rig鞋底基线不一致
- **THEN** Analyzer、Definition Build和Runtime composition MUST按同一诊断代码失败
- **AND** MUST不生成artifact或使用默认axis继续运行

### Requirement: Foot Rotation必须应用语义foot frame差值

Calibration MUST以每脚唯一ankle-local `Sole Frame Rotation`表达正交语义foot frame，MUST不保存可与heel/toe鞋底基线互相矛盾的独立forward/up作者向量。Planner MUST从动画ankle rotation与Calibration计算Animated Semantic Foot Frame，再从CurrentSupport normal、动画semantic forward和Profile限制计算Desired Semantic Foot Frame，并以两者旋转差生成目标ankle rotation。速度响应、ascent/descent alignment与ankle twist reduction MUST在该语义空间中有界应用。Planner MUST不把`Quaternion.LookRotation`产生的semantic surface frame直接赋给ankle骨。

Sampling Rig几何validator MUST证明sole frame前轴与heel-to-toe接触基线一致、上轴与参考鞋底外法线一致、左右脚使用同一手性，并证明参考姿势平地ankle correction位于正式边界内。

#### Scenario: Corin ankle骨轴不是标准forward/up

- **WHEN** Calibration声明的Sole Frame Rotation与ankle骨局部轴不同
- **THEN** 目标鞋底 MUST按support normal对齐
- **AND** ankle骨 MUST保留rig-specific固定旋转关系

#### Scenario: Sole Frame与heel-to-toe方向相反

- **WHEN** Sampling Rig参考姿势显示semantic forward和鞋底基线前后颠倒
- **THEN** Calibration validation MUST拒绝提交和Build
- **AND** Runtime MUST不在平地对ankle应用大角度补偿

### Requirement: Foot Placement Planner与骨骼Solver必须分离

`FootPlacement`节点的world-aware阶段 MUST让Presentation core唯一拥有contact、prediction、support envelope、constraint、pelvis、leg extension、bend stabilization与`CharacterFootPlacementPlan`；`CharacterLimbPoseSolver` MUST只消费plan、同帧上游Component Pose、Rig v3腿链与Calibration，并在节点output workspace应用双脚target、独立preferred bend normal/weight和pelvis offset。Plan MUST使用vendor-neutral数值表达bend normal、extension ratio、weight和decision reason，MUST不引用Transform、IKSolver或RootMotion类型。Pose Graph authoring决定该节点在最终拓扑中的位置，但不得把Planner状态、Physics query或Solver对象写入Gameplay State、Selection或其它Pose节点。Runtime MUST不依赖Final IK、`ICharacterFootPlacementSolver`或MonoBehaviour solver。

解析式Limb Pose Solver MUST把Plan的preferred bend normal转换为有限当帧bend target，并且bend权重 MUST只取Plan的`BendStabilizationWeight`。当该权重为零时，solver MUST保留同帧上游Component Pose的动画bend normal；它 MUST不将foot position weight复制为bend权重，不得重新计算extension或选择constraint生命周期。

#### Scenario: 解析式Solver应用一帧计划

- **WHEN** Planner输出双脚target、rotation、独立bend normal/weight和pelvis offset
- **THEN** CharacterLimbPoseSolver MUST按固定顺序应用pelvis和两个腿链
- **AND** MUST不重新query地面或改变Planner约束状态

#### Scenario: 安全区间不需要膝盖稳定

- **WHEN** Plan的Bend Stabilization Weight为零
- **THEN** CharacterLimbPoseSolver MUST不以静态goal覆盖动画bend normal
- **AND** foot position IK MAY继续按其独立权重执行

#### Scenario: 后续替换Solver实现

- **WHEN** 后续替换保持同一Component Pose合同的解析式数值实现
- **THEN** contact、prediction、constraint、pelvis和bend stabilization runtime MUST不需要修改
- **AND** 新实现 MUST不成为第二个planner或第二作者配置

### Requirement: Foot Placement 配置和Rig必须显式且可验证

每个启用Foot Placement的角色表现装配 MUST显式提供`CharacterFootPlacementProfile`、Rig v3、`CharacterAnimationRigBinding`、`CharacterWorldAwarePresentationBinding`、共享`CharacterFootPlacementRigCalibration`、self-collider root、PhysicsScene和非空Ground LayerMask。Rig v3 MUST唯一声明pelvis与左右hip-knee-ankle-toe Physical链；Calibration MUST唯一提供左右heel/toe contact offset、ankle-local Sole Frame Rotation和Preferred Bend Direction；Profile MUST唯一提供有限leg extension与bend stabilization区间。Runtime Rig与Calibration identity/schema/revision MUST与Projection Foot Analysis完全匹配。系统 MUST不使用Animator Humanoid映射、名称、层级扫描、`GetComponentInChildren`、零offset、默认axis、静态满权重pole、Default layer或单Ray补全缺失配置。

Calibration validator MUST在精确Sampling Rig绑定姿势中验证contact基线长度、统一地面误差、sole frame方向/手性、参考平地ankle correction、hip-knee-ankle弯曲平面以及preferred bend direction。Profile validator MUST验证最小伸展、稳定介入和最大伸展区间严格有序。任何失败 MUST定位资产、脚侧、指标、实测值和允许边界。

#### Scenario: Corin Runtime Prefab与Projection使用不同Calibration

- **WHEN** Host创建Presentation runtime但Rig Calibration revision不匹配Projection
- **THEN** configuration validation MUST报告两端identity并拒绝创建
- **AND** MUST不以Prefab局部字段或旧Projection继续运行

#### Scenario: Sole Frame退化或手性错误

- **WHEN** 任一Sole Frame非有限、不可归一、与heel-to-toe基线冲突或左右手性不一致
- **THEN** Calibration validation MUST拒绝保存或Build
- **AND** Runtime MUST不使用`Vector3.forward/up`作为fallback

#### Scenario: Profile腿部区间无序

- **WHEN** Bend Stabilization Full不严格位于Start和Maximum Extension之间
- **THEN** Profile validation MUST拒绝Build并显示全部区间值
- **AND** Runtime MUST不重排或夹紧配置继续运行

### Requirement: Foot Placement 必须提供统一诊断且保持热路径有界

Runtime diagnostics MUST只读暴露Body tick/reset、Calibration schema/identity/revision、Sampling Rig几何验证摘要、PoseProgramHash、CompletionIdentity、Pose Continuity、最终Foot Placement参数identity/index/value、最终source contribution、每只脚的生成plant confidence/局部速度/合成世界速度/高度/landing、heel/toe/current/future support identity、constraint和transition reason、Ground Envelope segment与拒绝原因、surface identity、lock/replant/twist/separation误差、heel lift、semantic sole frame、animated/preferred/final bend normal、leg extension ratio、bend stabilization decision/weight、position/rotation/bend最终权重、Pelvis Height mode/decision/reason/support foot、pelvis target/current offset、query计数和solver结果。Runtime MUST为Feature、Plan、Query和Solve提供Profiler marker，并复用固定容量contribution、feature、query、candidate、segment和snapshot workspace；表现热路径 MUST不采样AnimationClip，不使用LINQ、反射、字符串查找、临时List或每帧托管分配。Diagnostics和Scene gizmo MUST只读取正式Feature、Plan、Query和Solve snapshot，不得重新分析动画、query或修改计划。

#### Scenario: 排查CrossFade误释放

- **WHEN** 一只脚在CrossFade后的最终姿态中从Locked进入Free
- **THEN** Live Debug MUST显示对应Completion、最终参数、source contribution和生成confidence
- **AND** Debug读取 MUST不改变下一帧Pose Graph结果或constraint状态

#### Scenario: 排查转身时膝盖翻面

- **WHEN** 一只脚在Turn到Locomotion过渡中出现bend stabilization
- **THEN** Live Debug与gizmo MUST显示animated、preferred、final bend normal、extension ratio、独立权重和decision reason
- **AND** 诊断 MUST不创建新的bend target或重新执行解析式solver

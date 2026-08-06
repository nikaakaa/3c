## MODIFIED Requirements

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

### Requirement: 腿部弯曲稳定必须保留动画平面并使用有限伸展区间

Planner MUST从最终动画hip、knee和ankle姿势计算每脚animated bend plane normal，并从目标脚位置和有限leg length计算`LegExtensionRatio`。`CharacterFootPlacementProfile` MUST显式提供严格有序的最小/最大伸展比例、稳定介入区间和最大弯曲稳定权重。Calibration的preferred bend direction MUST只用于生成vendor-neutral `PreferredBendPlaneNormal`；targets MUST分别输出position、rotation与bend weight。

LegIK ABI MUST把最终混合结果解释为`BendPlaneNormal`，以应用pelvis后的`Hip -> TargetAnkle`轴计算有限`KneeDirection = Cross(TargetAxis, BendPlaneNormal)`，不得把平面法线直接当作膝盖方向。在安全伸展区间内bend weight MUST为零并保留动画平面；接近过度伸直或动画平面退化时Planner MUST连续增加有限稳定权重。目标低于最小或超过最大可解伸展范围时Planner MUST将position、rotation和bend权重归零，不得把不可解目标提交给LegIK硬拉。

#### Scenario: 正常Walk动画膝盖弯曲清晰

- **WHEN** leg extension位于Profile安全区间且animated bend plane normal有限
- **THEN** targets的Bend Stabilization Weight MUST为零
- **AND** LegIK MUST由动画平面计算膝盖方向而不得把normal作为direction使用

#### Scenario: 脚目标接近腿完全伸直

- **WHEN** Leg Extension Ratio从Stabilization Start接近Stabilization Full
- **THEN** Planner MUST连续混向Calibration preferred bend plane normal并限制最大权重
- **AND** LegIK MUST在目标腿轴空间重新计算有限Knee Direction

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


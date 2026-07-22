## ADDED Requirements

### Requirement: Pose Graph必须是跨Pose Slot空间合成的唯一权威

每个启用角色动画Presentation的`CharacterAnimationPresentationProfile` MUST引用唯一`CharacterPresentationPoseGraphAsset`。Editor Compiler MUST将该asset编译为不可变、target-neutral的`CharacterPresentationPoseProgram`并嵌入`CharacterPresentationProjection`；Runtime MUST只执行该Program，不得解释authoring节点、Graph asset或ScriptableObject。Pose Graph MUST唯一拥有跨Pose Slot拓扑、Bone Mask、Override/Additive、Pose Parameter解析、最终source contribution与最终Animator Animation Pose。Blend Stack、Animancer、Lifecycle、Foot Placement或Presenter MUST不保存第二套跨slot composition order。

#### Scenario: Base与FullBody Action同时有输出

- **WHEN** BaseLocomotionSlot与FullBodyActionSlot在同一PresentationFrame产生合法PoseSlotFrame
- **THEN** Character Presentation Pose Graph native job MUST按编译后的LayeredBoneBlend节点和dense全身Mask生成唯一最终pose
- **AND** Blend Stack MUST不再次按Layer order合成两个slot

#### Scenario: Runtime缺少Pose Program

- **WHEN** Projection缺少有效Pose Program或其PoseGraph identity与Profile不匹配
- **THEN** Presentation Runtime创建 MUST失败
- **AND** MUST不退回Animancer Layer、只播Base或bind pose

### Requirement: Animation Channel与Pose Slot必须保持一对一显式绑定

Pose Graph MUST声明稳定`PoseSlotId`、对应`AnimationChannelId`和`RequireOutput | AllowEmpty`。Projection Compiler MUST要求每个可达Animation Channel精确绑定一个Pose Slot、每个Pose Slot至多绑定一个Animation Channel、每个声明slot恰好由一个根图`PoseSlotInput`消费。Animation Channel只负责逻辑选择，Pose Slot只负责表现入口；Presentation MUST不在channel之间仲裁，Program MUST不读取Pose Graph topology。

#### Scenario: 两个channel绑定同一slot

- **WHEN** BaseLocomotion与FullBodyAction被配置到同一个PoseSlotId
- **THEN** Projection Build MUST失败并报告两个AnimationChannelId和PoseSlotId
- **AND** Runtime MUST不按priority、声明顺序或最后command选择winner

#### Scenario: 可达channel没有slot

- **WHEN** Semantic producer contract包含一个可达AnimationChannelId但Pose Graph没有匹配slot binding
- **THEN** Projection Build MUST失败并定位producer与channel
- **AND** MUST不创建隐式Base slot或按名称匹配slot

### Requirement: 每个Pose Slot必须通过唯一固定Blend Stack进入Pose Graph

每个编译后的Pose Slot MUST拥有一个固定`AnimationBlendStackRuntime`和一个`PoseSlotInput`。PoseSlotInput MUST只读取该Stack完成的`PoseSlotFrame`，不得创建、配置、跳过或重复推进Stack。Blend Stack MUST作为Presentation Runtime固定模块存在，不得成为可选Pose Graph节点。一个slot输出分支到多个下游节点时 MUST复用同一帧缓存，不得重复采样source或推进Fade Clock。

#### Scenario: 一个slot输出被两个节点读取

- **WHEN** BaseLocomotionSlot同时连接LayeredBoneBlend与PoseCurveResolve
- **THEN** 两个节点 MUST读取同一个PoseSlotFrame identity和缓存
- **AND** Base slot Stack MUST在该PresentationFrame只推进一次

#### Scenario: Authoring尝试绕过Stack

- **WHEN** Pose Graph asset包含直接AnimationClip、Animancer State或Raw Producer Input节点
- **THEN** Pose Graph Validator MUST拒绝该节点类型
- **AND** MUST不允许source直接连接OutputPose

#### Scenario: slot标量权重为零但仍有骨骼输出

- **WHEN** PoseSlotFrame的OutputWeight为零但dense per-bone output仍至少有一个非零权重
- **THEN** 该frame的Availability MUST保持Pose
- **AND** Pose Graph空间合成 MUST读取dense per-bone weight而不得用OutputWeight作为空间门槛
- **AND** OutputWeight MUST只用于非空间Pose Parameter、AllowEmpty判断与诊断概览

### Requirement: Pose Graph节点必须使用有限typed Pose合同

Pose Graph Runtime MUST只安装`PoseSlotInput`、`LayeredBoneBlend`、`AdditivePose`、`PoseCurveResolve`和`OutputPose`及其版本化typed ports。Authoring MAY额外安装静态`PoseSubgraph`与compiler-only `GraphInput`/`GraphOutput`边界；Compiler MUST在发布Program前消除三者。Pose edge MUST传递Availability、dense local TRS pose、PoseParameter buffer、source contribution和continuity identity。节点 MUST不读取State、Action、Blackboard、Input、GameplayTag、Timeline Window、MotionWarp target、业务priority或runtime Unity Transform。未知node code、port kind或payload version MUST在Build或Runtime创建时失败。

#### Scenario: Pose节点读取Blackboard

- **WHEN** authoring或serialized payload尝试给Pose节点增加Blackboard/ConditionRule输入
- **THEN** node catalog与validator MUST拒绝该端口
- **AND** 系统 MUST不通过反射或object port让该依赖进入Runtime

#### Scenario: Projection包含未知node code

- **WHEN** Runtime加载包含当前Pose Program ABI不支持的operation code
- **THEN** Projection validation MUST在创建Evaluator前失败
- **AND** MUST不跳过该节点或把输入直通输出

### Requirement: Pose Graph必须显式处理Optional Pose与最终输出有效性

`AllowEmpty` Pose Slot MUST输出typed NoPose与零贡献，不得生成bind pose或保留上一帧残留pose。接受Optional overlay的合成节点 MUST在合同中显式定义NoPose行为；`RequireOutput` slot和根`OutputPose` MUST在所有合法slot availability组合下得到有效pose。Compiler MUST静态验证这些路径，Runtime发现合同破坏时 MUST发布typed invalid completion并阻止Pose Post Process读取残留骨骼。

#### Scenario: FullBody Action退出

- **WHEN** FullBodyActionSlot完成到Empty的正式transition
- **THEN** LayeredBoneBlend MUST输出未经action覆盖的BaseLocomotion pose
- **AND** MUST不显示bind pose、上一帧action或隐藏Idle

#### Scenario: BaseLocomotion没有正式输出

- **WHEN** RequireOutput BaseLocomotionSlot既没有Selected/Retained pose也没有合法Pending output
- **THEN** Pose Graph evaluation MUST失败并发布明确slot invalid reason
- **AND** Foot Placement MUST不对上一帧残留pose继续求解

### Requirement: Bone Mask与Additive必须依赖稳定Rig Identity

所有LayeredBoneBlend和AdditivePose节点 MUST引用匹配`CharacterAnimationRigDefinition`的稳定Mask/Reference identity。当前安装的唯一Additive reference identity MUST是公开常量`AnimationAdditiveReferencePoseIds.RigReference`，其值固定为`animation.rig-reference`；Node默认与构造默认 MUST使用该identity，Validator MUST拒绝其它任意字符串，系统 MUST不安装第二份Reference catalog或fallback。Projection Compiler MUST按父节点优先BoneId顺序展开dense mask与additive reference descriptor：`Local` MUST原样保存Rig的dense ReferenceLocal TRS，`Mesh` MUST按Rig parent index逐骨组合parent TRS为dense mesh-space reference。Runtime计算Mesh reference delta后 MUST按同一Rig parent index转换回local pose，不得把mesh-space delta直接写入local bone。Runtime MUST不读取AvatarMask path、Humanoid mapping、骨骼名称、Transform path或层级搜索补全。Additive source、reference identity、reference space、scale policy或Rig revision不匹配 MUST阻止发布。

#### Scenario: UpperBody Mask不包含脚

- **WHEN** 某overlay的dense mask为LeftFoot与RightFoot保存零权重
- **THEN** 最终两脚pose与source contribution MUST完全来自Base输入
- **AND** overlay slot weight MUST不稀释两脚Foot Analysis

#### Scenario: Mask引用旧Rig revision

- **WHEN** Pose Graph Mask由旧Rig revision编译且Profile已切换到新Rig
- **THEN** Projection Build MUST失败并报告Mask与Rig identity
- **AND** Runtime MUST不截断或重排dense数组

#### Scenario: Additive节点使用任意Reference名称

- **WHEN** AdditivePose保存的ReferencePoseId不是`animation.rig-reference`
- **THEN** Validator与Projection Build MUST拒绝该节点
- **AND** MUST不按名称查找资产、创建catalog entry或改用Rig bind pose fallback

#### Scenario: 编译Mesh reference

- **WHEN** AdditivePose选择Mesh reference space
- **THEN** Compiler MUST按Rig父节点优先顺序把dense ReferenceLocal TRS组合成mesh-space reference
- **AND** Runtime MUST使用Rig parent index把mesh-space additive结果转换回local pose

### Requirement: Pose Parameter曲线必须随Pose显式解析

Pose Graph MUST以稳定`PoseParameterId`和有限标量值携带动画表现参数。每个参数声明 MUST包含显式default；每个跨pose合成节点 MUST为全部可达参数声明`Base`、`Overlay`、`Weighted`、`Max`或`Min`等已安装resolve policy。`PoseCurveResolve` MUST拥有两个有序Pose输入：Input A为Base Pose，Input B为Parameter Source Pose；骨骼pose、source contribution和左右脚feature MUST保持Base，Input B MUST只参与dense parameter policy、output weight与continuity求值。Parameter Source为NoPose时 MUST保持Base，Invalid时 MUST产生typed invalid。Slot Stack和Pose Graph MUST按各自职责生成唯一参数流，OutputPose MUST发布唯一final parameter stream。Runtime MUST不按字符串同名覆盖、缺失policy默认选择或从Gameplay Curve补值。

#### Scenario: Base与Action都有Foot IK权重参数

- **WHEN** FullBodyAction全身覆盖Base且两路都输出同一PoseParameterId
- **THEN** 合成节点 MUST按该参数显式policy生成唯一值
- **AND** Foot Placement MUST只读取OutputPose发布的正式映射值

#### Scenario: 新参数没有合成策略

- **WHEN** source新增PoseParameterId但任一可达blend节点没有该参数policy
- **THEN** Pose Graph Compiler MUST拒绝发布并定位node和parameter
- **AND** Runtime MUST不选择Base、Overlay或零作为fallback

#### Scenario: Parameter Source为空或无效

- **WHEN** PoseCurveResolve的Parameter Source输入为NoPose
- **THEN** 输出 MUST保持Base的骨骼、贡献、foot feature与参数
- **AND** Parameter Source为Invalid时 MUST产生typed invalid而不是读取旧参数

### Requirement: Pose Graph必须编译为固定DAG与有界Workspace

Pose Graph Compiler MUST拒绝cycle、dangling edge、非法fan-in、重复Output、缺失Output和不兼容port。Compiler MUST生成v2 Pose Program schema、v2 runtime ABI与v2 operation payload、稳定topological operation顺序、固定pose/parameter/contribution workspace、公共子图frame cache、output index与source map。`FrameCacheCount` MUST精确等于Operations数量，operation index MUST是唯一frame-cache index；operation hash MUST继续包含payload version、Input A、Input B以及Additive reference identity、space和完整TRS。Runtime MUST拒绝v1 Program或operation payload，按Projection声明容量一次预分配，并在每个PresentationFrame只求值一次；不得动态创建节点、扩容buffer、遍历ScriptableObject或逐骨骼操作场景Transform。

#### Scenario: 两条边形成cycle

- **WHEN** authoring graph包含从下游返回上游的Pose edge
- **THEN** Compiler MUST报告完整node/port identity chain
- **AND** MUST不按编辑器节点位置或边顺序打断cycle

#### Scenario: Runtime workspace小于Program需求

- **WHEN** 创建时workspace不能容纳Program声明的pose value、parameter或contribution slot
- **THEN** Presentation Runtime创建 MUST失败
- **AND** 表现帧 MUST不临时分配或禁用source contribution

#### Scenario: 加载v1 Pose Program

- **WHEN** Projection包含v1 schema、runtime ABI或operation payload
- **THEN** Runtime创建 MUST失败
- **AND** MUST不兼容读取、跳过Input B或复用旧frame cache布局

### Requirement: Pose Graph必须发布最终Pose贡献与连续性

`AnimationPosePlayableGraphRuntime` MUST在Pose Graph native job完成最终AnimationStream写回后发布唯一lease-protected `FinalAnimationPoseFrame`，包含pose completion identity、final PoseParameter、按最终Bone Mask传播的source contribution、Left/Right foot actual contribution与连续性状态。Foot Placement、Preview与Debug MUST只消费该frame；不得从source playable weight、单个slot scalar或authoring graph重建最终贡献。

#### Scenario: FullBody Action全身覆盖Locomotion

- **WHEN** action slot在LeftFoot骨骼上的最终mask与slot weight均为1
- **THEN** FinalAnimationPoseFrame的LeftFoot contribution MUST来自action slot
- **AND** BaseLocomotion的LeftFoot feature MUST不继续参与Foot Placement输入

#### Scenario: Pose Graph本帧无效

- **WHEN** 任一operation产生非有限pose或参数并使completion Invalid
- **THEN** Runtime MUST阻止Foot Placement读取该frame并执行正式reset
- **AND** MUST不发布上一帧final contribution冒充当前结果

### Requirement: 静态PoseSubgraph必须保持模块化而不形成动态双路径

PoseSubgraph MUST支持owner-private inline data和显式shared Pose Graph asset，使用typed Pose/Parameter接口并由Compiler递归静态展开。根图 MUST禁止`GraphInput`/`GraphOutput`并恰好包含一个`OutputPose`；子图 MUST恰好包含一个只含output port的`GraphInput`和一个只含input port的`GraphOutput`，MUST禁止`OutputPose`且至少导出一个Pose。每个边界port MUST拥有独立稳定`InterfacePortId`；每个调用点本地port MUST显式绑定该identity且不得用node-local `PosePortId`代替。Validator MUST校验接口identity唯一、完整一对一coverage、kind/direction/required一致、重复或未绑定port、required边界悬空与inline/shared cycle。Inline与shared只能有一个真数据来源；Compiler MUST用call-site-scoped稳定identity克隆内部node/port，重接边界edge并保留完整source-map call chain。`PoseSubgraph`、`GraphInput`、`GraphOutput` MUST不出现在Runtime Program中。Runtime MUST不动态替换subgraph class、反射查找实现或维护另一套Linked Layer evaluator。

#### Scenario: 根图误用子图边界

- **WHEN** 根Pose Graph包含GraphInput或GraphOutput，或子图包含OutputPose
- **THEN** Validator MUST拒绝该拓扑
- **AND** Compiler MUST不生成部分Program

#### Scenario: 调用点漏绑Parameter接口

- **WHEN** PoseSubgraph调用点缺少、重复或错误绑定任一Pose/Parameter InterfacePortId
- **THEN** Validator MUST报告精确call site与interface identity
- **AND** MUST不按端口名称、顺序或node-local PortId猜测binding

#### Scenario: shared子图静态展开

- **WHEN** 两个call site引用同一shared子图并通过各自接口输入
- **THEN** Compiler MUST为两个call site生成各自稳定作用域的内部node/port identity与source-map chain
- **AND** Runtime Program MUST不包含PoseSubgraph、GraphInput、GraphOutput operation或动态dispatch

#### Scenario: 从inline抽取shared PoseSubgraph

- **WHEN** 作者显式执行Extract Shared
- **THEN** 系统 MUST创建独立Pose Graph asset并把owner切换到shared引用
- **AND** owner MUST清除原inline真数据

#### Scenario: 两个角色复用shared PoseSubgraph

- **WHEN** 两个Profile引用同一shared PoseSubgraph但使用不同Rig revision
- **THEN** 每个Projection Build MUST分别校验并编译自己的dense payload
- **AND** shared asset MUST不保存任一Runtime workspace或角色反向引用

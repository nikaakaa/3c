# Design: 角色动画虚拟骨骼与姿势内双骨骼IK

## Context

当前动画表现的目标链路是：

```text
AnimationSelection
  -> MarkerSync
  -> SelectedPosePlayer / BlendSpacePlayer / BlendStack
  -> Inertialization
  -> Blend / Layered Blend / Additive / ModifyBone
  -> FootPlacement
  -> OutputPose
```

`CharacterAnimationRigDefinition.Bones`同时承担四项职责：

- 描述Animator层级中的真实骨骼。
- 决定source pose workspace的dense长度。
- 决定Bone Mask、Blend Profile和PoseGraph的dense长度。
- 决定source capture的`TransformStreamHandle`与final Physical Transform writer binding的数量。

这四项在只有真实骨骼时数值相同，但Virtual Bone加入后不再相同。Virtual Bone需要占据Pose槽位并参与混合，却没有场景Transform，也不能被final writer写回。若只向现有`Bones`追加条目，`CharacterAnimationRigBinding`会要求一个不存在的Transform，`BindStreamTransform`也会失败；若把Virtual Bone保存在PoseGraph节点或FinalIK adapter中，它又无法跟随每个source分支、Stored Pose和Inertialization一起流动。

Corin Rig已经包含左右手、完整双臂链和独立于手臂分支的`Bip001_Prop1`武器骨骼分支。基础动画与FullBody Action会同时动画武器和双手，但local bone CrossFade、分层组合或未来Additive会通过不同层级传播到武器与手，最终相对关系可能偏离每个source动画本来的握持关系。这是Virtual Bone与TwoBoneIK的首个明确业务消费者。

## Goals

- 用唯一Rig资产表达Physical Bone与Virtual Bone，不创建第二份参考点配置。
- 让Virtual Bone从每个真实source pose派生，并完整经过Player、Blend、Stack、Stored Pose和Inertialization。
- 让作者通过Bone Mask明确决定某一层是否改变Virtual Bone参考。
- 用显式PoseGraph `TwoBoneIK`消费Virtual Bone，不创建图外IK或场景target。
- 保持Animator绑定、Physical Transform写回、蒙皮与FootPlacement只操作Physical Bone。
- 让Projection、Preview、Runtime、Pose Watch和Live Debug使用同一数据与执行计划。
- 以Corin武器双手稳定作为完整样例，而不是只安装未使用的数据结构。

## Non-Goals

- DCC骨架修改、FBX辅助骨骼生成或动画文件重写。
- 链式Virtual Bone、Virtual Bone DAG或运行时动态增删。
- Virtual Bone scale动画、蒙皮、socket或场景Transform。
- FABRIK、CCD、多链约束、全身IK、Control Rig或物理解算。
- 世界锚点、Foot Lock、预测落脚、地面接触与鞋底语义。
- Gameplay、Simulation、World、Network或Agent写入能力。

## Implementation Staging And Module Boundaries

### 接入门禁

当前动画闭环运行期间只实施第一阶段独立模块，不修改现有生产链、serialized资产或generated产物。只有用户明确确认闭环结束并解除门禁后，才进入第二阶段统一接入。依赖change的任务数量、代码已存在或某个模块可单独编译，都不能自动解除门禁。

### 串行合同冻结

并行工作开始前只冻结以下最小合同：

- `CharacterPoseBoneKind`：只表达Physical或Virtual。
- Physical/Pose显式数量合同，不提供含糊`BoneCount`。
- Virtual Bone immutable descriptor：identity、Source physical index、Target physical index与dense pose index。
- TwoBoneIK immutable descriptor：chain physical indices、Effector pose index、Joint Target pose index、offset、rotation mode与weight。
- typed result/failure：不引用Projection、Graph node、Animator、Preview或场景对象。

此步骤只冻结模块间输入输出，不改`CharacterAnimationRigDefinition`、Projection payload或任何serialized schema。

### 并行模块A：Virtual Bone Pose Derivation

输入：

- parent-first Physical local pose只读span。
- Physical parent index只读span。
- Virtual Bone descriptor只读span。
- 调用方提供的component scratch与完整Pose Bone输出span。

处理：

- 建立Physical component pose。
- 计算Target相对Source的Virtual local position与rotation。
- 固定Virtual local scale为1。
- 对数量、索引和非有限输入返回typed failure。

输出：

- 完整Pose Bone page。
- 精确失败阶段与VirtualBoneId。

该模块不读取AnimationStream、不持有previous pose、不计算velocity，也不注册到Animancer source capture、Preview或final writer。

### 并行模块B：Two Bone IK Pose Solver

输入：

- 完整输入Pose只读span。
- parent-first Pose parent index只读span。
- 已解析的TwoBoneIK descriptor。
- 调用方提供的component scratch与输出Pose span。

处理：

- 复制完整输入Pose。
- 在component space无拉伸求解唯一Physical Root/Joint/End chain。
- 使用显式Joint Target建立弯曲平面。
- 限制可达距离并计算残差。
- 只改三个Physical chain local pose，保留全部scale和其它Physical/Virtual槽位。

输出：

- 完整输出Pose。
- `ReachClamped`、残差和typed failure。

该模块不声明`CharacterPoseNodeKind`、不注册operation code、不创建native program数组，也不接PoseGraph、FootPlacement、FinalIK或场景Transform。

### 并行模块C：Pose Constraint Diagnostics Contract

输入：

- 已完成Pose page。
- Virtual Bone descriptor。
- TwoBoneIK求解结果。

处理：

- 复制Virtual local/component pose、Source/Target identity、chain identity、reach状态和残差。
- 使用固定容量页，不重新派生Virtual Bone或重新执行IK。

输出：

- 只读有界诊断记录。

该模块不注册Pose Watch、Live Debug、Runtime snapshot或Authoring Preview入口。第二阶段接入时，这些入口只能消费该合同，不能创建第二计算路径。

### 第一阶段依赖边界

```text
最小合同冻结
  ├─ Virtual Bone Pose Derivation
  ├─ Two Bone IK Pose Solver
  └─ Pose Constraint Diagnostics Contract
```

三个模块可以并行实现，但都不得引用现有Rig资产、Projection compiler、PoseGraph compiler、Animator sampling backend、workspace窗口或Corin资产。第一阶段完成只表示正式模块可供接入，不表示Virtual Bone能力已经安装、Runtime已经可用或Corin已经迁移。

### 第二阶段统一接入

第二阶段按唯一方向接入：

```text
Rig v3 authoring
  -> Projection v2
  -> source capture调用Virtual Bone Pose Derivation
  -> Pose运输与Mask/Profile
  -> PoseGraph调用Two Bone IK Pose Solver
  -> diagnostics入口消费Pose Constraint Diagnostics Contract
  -> Physical-only final writer
  -> 通用资产与Corin原子迁移
```

接入阶段直接删除旧Rig v1、旧数量API、旧Mask/Profile数据和旧generated Projection，不保留第一阶段专用入口、兼容层或未接线的第二实现。

## Concept Model

### Physical Bone

Physical Bone同时存在于：

- Rig authoring与Projection payload。
- Animator Transform层级。
- source/final Pose page。
- source capture的`TransformStreamHandle`与final Physical Transform writer/binding。

它可以驱动蒙皮、附件或真实辅助骨骼。

### Virtual Bone

Virtual Bone只存在于：

- Rig authoring与Projection payload。
- source/final Pose page。
- Blend、Mask、Stored、Inertialization与PoseGraph workspace。
- Preview、Pose Watch与diagnostics。

它没有Transform，没有AnimationClip轨道，不驱动蒙皮。它表达：

```text
Target Physical Bone在Source Physical Bone空间中的当前姿势
```

### Stored Pose、Virtual Bone与Foot Lock的区别

| 概念 | 数据范围 | 是否跨帧 | 空间 | 业务用途 |
|---|---|---:|---|---|
| Stored Pose | 整套Pose | 是或由Stack持有 | Pose local | CrossFade与source释放 |
| Inertialization | 整套Pose的residual/velocity | 是 | Pose local | 单Pose不连续平滑 |
| Virtual Bone | 一个Source/Target关系 | 否 | Source Bone space | 在后续层后保留动画参考 |
| Foot Lock | 一只脚的世界/支撑面锚点 | 是 | World/Support | 落地后防滑 |

Virtual Bone不是历史缓存，也不能替代世界锁点。

## Responsibility Model

| 层 | 输入 | 唯一职责 | 输出 |
|---|---|---|---|
| Rig Inspector | Physical Bone catalog | 声明Virtual Bone identity与Source/Target | Rig v3 authoring |
| Projection Compiler | Rig v3与PoseGraph | 校验并降低dense索引、reference pose与IK描述 | immutable Projection |
| Animancer source capture | Physical Bone AnimationStream | 采样Physical Pose并派生Virtual Pose | 完整source Pose page |
| Player/Stack/Inertialization | 完整source Pose | 时间连续性和Pose运输 | 完整Pose Value |
| Blend/Mask/Additive | 完整Pose Value | 按显式权重组合Physical与Virtual槽位 | 完整Pose Value |
| TwoBoneIK | 一个完整Pose Value | 读取Effector/Joint reference并修改Physical limb | 完整Pose Value |
| FootPlacement | composition后Physical leg pose与world context | 地面查询、脚腿与pelvis修正 | world-aware最终Physical pose |
| Final writer | 最终Pose Value | 只通过Physical Transform binding写Physical Bone | Animator Physical Transform |

## Decision 1: Rig显式区分Physical Bone与Virtual Bone

目标authoring模型：

```text
CharacterAnimationRigDefinition
  RigId
  Revision
  PhysicalBones[]
    BoneId
    ParentPhysicalIndex
    ReferenceLocalPose
  VirtualBones[]
    VirtualBoneId
    DisplayName
    SourcePhysicalBoneId
    TargetPhysicalBoneId
  RootPhysicalBoneId
  LeftFootPhysicalBoneId
  RightFootPhysicalBoneId
```

Physical Bone继续保持parent-first稳定顺序。Virtual Bone按authoring稳定顺序追加到Pose catalog：

```text
Pose index [0, PhysicalBoneCount)                   = Physical Bone
Pose index [PhysicalBoneCount, PoseBoneCount)       = Virtual Bone
PoseBoneCount                                       = Physical + Virtual
```

Virtual Bone的Pose parent index固定为Source Physical Bone的dense index。Target只决定派生值，不成为Pose层级parent。

所有`AnimationBoneId`在Physical与Virtual全集内必须唯一。Source与Target必须存在、不同且都是Physical Bone。本change不允许Virtual Bone引用Virtual Bone，避免在首个版本引入DAG排序、环检测和多阶段重算。

### Tradeoff

- 优点：Transform binding与Pose数据数量不再含糊；Virtual Bone仍能复用现有dense Pose算法。
- 代价：需要一次性修改所有把`Rig.Bones.Count`当作统一数量的代码，旧schema不能继续读取。

## Decision 2: Virtual Bone在每个source capture中派生一次

每个source完成Physical Bone local pose采样后执行：

```text
Physical local pose
  -> Physical component pose
  -> Source component pose
  -> Target component pose
  -> Virtual local position/rotation
  -> append Virtual Pose slots
  -> differentiate previous/current完整Pose
```

位置与旋转语义：

```text
VirtualPosition = SourceComponent.InverseTransformPoint(TargetComponent.Position)
VirtualRotation = inverse(SourceComponent.Rotation) * TargetComponent.Rotation
VirtualScale    = one
```

Rig reference pose用同一算法建立Virtual reference local pose。Runtime与Preview必须调用相同math实现。

派生发生在每个source自己的capture page内，而不是最终composition之后。否则下游Additive改变Target后，Virtual Bone也会被重新对齐到已经漂移的Target，参考信息立即丢失。

### 数值例子

```text
基础source：Weapon=20，LeftHand=25，VB_Weapon_LeftHand local=5
上身层后：Weapon=21，LeftHand=27，Virtual local仍由Mask保留为5
最终Virtual component=26
TwoBoneIK把真实LeftHand从27修正到26
```

如果换弹或攻击source本来让LeftHand相对Weapon移动到8，source capture会直接产生8；Virtual Bone不会把手永远钉在5。

### Tradeoff

- 优点：参考随每个动画source变化，同时能被后续层选择性保留。
- 代价：每个活跃source需要额外component pose计算和少量Pose槽位；通过预分配workspace和少量Virtual Bone限制成本。

## Decision 3: Virtual Bone进入所有Pose运输算法，但保持只读数据骨骼

以下模块统一使用`PoseBoneCount`：

- source current/previous pose与velocity。
- SelectedPosePlayer与BlendSpacePlayer输出。
- BlendStack entry、Stored Pose、per-bone weights与release前pose。
- Inertialization pose history、residual与velocity。
- BlendPose、LayeredBoneBlend、AdditivePose与PoseSubgraph。
- Pose Watch与final pose diagnostics。

以下模块继续使用`PhysicalBoneCount`：

- `CharacterAnimationRigBinding` Transform数组。
- source capture的Animator `TransformStreamHandle`数组。
- Foot Analysis Sampling Rig层级校验。
- Motion Matching实际骨骼特征catalog，除非其source schema以后显式选择Virtual Bone。
- final Physical Transform writer/binding。

Virtual Bone可以被Blend、Additive、Mask、Stack和Inertialization间接修改，因为这些操作表达Pose来源贡献。`ModifyBone`、TwoBoneIK chain输出和FootPlacement不得直接写Virtual Bone。这样它始终是参考数据，不会变成没有Transform却可任意修改的第二种控制骨骼。

### Tradeoff

- 优点：一份Pose page同时运输最终骨骼与参考坐标，不需要额外side-channel。
- 代价：每个使用bone count的合同都必须明确选择Physical或Pose数量，编译期和运行时校验更严格。

## Decision 4: Bone Mask和Blend Profile必须显式覆盖Virtual Bone

`CharacterAnimationBoneMaskAsset`与per-bone Blend Profile升级为基于完整Pose Bone catalog校验。每个Physical和Virtual Bone都必须出现一次，缺失即编译失败。

不采用以下自动规则：

- Virtual Bone继承Target Bone权重。
- Virtual Bone继承Source Bone权重。
- 新增Virtual Bone默认0。
- 新增Virtual Bone默认1。

这些规则在不同业务下结论相反：

- FullBody Action应让Virtual Bone跟随动作source更新，通常使用1。
- 只想保留动作前手部参考的Additive层应排除Virtual Bone，使用0。
- per-bone transition可能需要与Effector Target或武器骨骼使用相同duration multiplier，但必须由作者明确选择。

Rig revision变化会使旧Mask/Profile失效。正式迁移工具一次性补齐作者明确给出的权重后保存新revision，不在Runtime补全。

### Tradeoff

- 优点：每层是否改变参考关系完全可审查，不存在隐藏继承。
- 代价：增加Virtual Bone后必须迁移所有Mask/Profile；这是一次明确的数据成本。

## Decision 5: 新增显式TwoBoneIK作为首个消费者

`TwoBoneIK`是普通Pose输入/输出节点，位于native composition阶段。节点authoring至少保存：

- `EndPhysicalBoneId`：被控制链末端。
- `EffectorPoseBoneId`：目标参考，可以是Physical或Virtual Bone，但不能属于被控制链。
- `EffectorLocalPositionOffset`与`EffectorLocalRotationOffset`。
- `JointTargetReferencePoseBoneId`与非零`JointTargetOffset`。
- `EndRotationMode`：`PreserveInput`或`MatchEffector`。
- 有限`Weight`默认值与可选typed weight输入。

Compiler由End Bone向上精确取得Joint与Root两个Physical parent，形成三关节链：

```text
Root Physical Bone -> Joint Physical Bone -> End Physical Bone
```

节点在component space执行不拉伸Two Bone IK：

1. 从输入Pose计算Root、Joint、End、Effector与Joint Target component pose。
2. 使用当前两段长度求解Root与Joint旋转。
3. 将目标距离限制在两段长度构成的合法可达区间，超出时输出`ReachClamped`而不拉伸。
4. 使用显式Joint Target决定弯曲平面。
5. 保持两段长度与全部local scale。
6. 按Weight混合输入与求解旋转；`MatchEffector`时再匹配End旋转。
7. 重建受影响Physical链的local pose并输出完整Pose。

节点不允许stretch，也不在Joint Target退化时猜测旧帧、reference pose、世界轴或隐藏pole。配置在reference pose中退化时Compiler失败；Runtime输入变成非有限、零长度或共线退化时节点发布typed failure，最终必需路径不得继续发布旧Pose。

多个TwoBoneIK通过普通Pose edge确定顺序。节点不拥有Transform、MonoBehaviour、Playable或独立Update。

### 为什么不复用FinalIK LimbIK

旧Final IK adapter依赖场景Transform、显式solver lifecycle和外部target。把手部Virtual Bone送入Final IK会要求创建隐藏GameObject或在PosePlan外再跑一次组件更新，形成第二执行链。TwoBoneIK数学直接在dense Pose上运行，才能与Preview和Runtime共用同一计划。

### Tradeoff

- 优点：首个消费者与Virtual Bone使用同一Pose、Mask、Projection和执行顺序，能力有完整业务出口。
- 代价：项目需要拥有一个边界明确的native Two Bone IK实现；首版不提供插件的高级stretch、twist或多链功能。

## Decision 6: TwoBoneIK位于FootPlacement之前

目标顺序：

```text
source capture including Virtual Bones
  -> Player / Stack / Inertialization
  -> Blend / Layered / Additive
  -> arm TwoBoneIK
  -> FootPlacement world-aware phase
  -> OutputPose / FinalAnimationPoseFrame
```

TwoBoneIK只需要Pose数据，不读取PhysicsScene，因此属于native composition。FootPlacement需要Body frame、PhysicsScene、support和solver，因此继续是唯一world-aware阶段。

Corin首个TwoBoneIK只修改双臂，不与FootPlacement腿链重叠。未来若作者把TwoBoneIK用于腿，图顺序仍明确表示FootPlacement最后可以覆盖腿部结果；系统不为此建立另一条leg IK权威。

## Decision 7: Runtime binding和writer只认识Physical Bone

`CharacterAnimationRigBinding`目标合同：

```text
Animator
RigId / RigRevision
PhysicalBoneTransforms[PhysicalBoneCount]
```

它不保存Virtual Bone占位null，也不创建隐藏Transform。`AnimancerPoseSamplingBackend`只为Physical Bone建立handle和reference capture输入，但把完整Pose page与compiled Virtual Bone descriptor交给capture job。

final writer执行：

```text
for index in [0, PhysicalBoneCount):
    write Pose[index] through PhysicalTransformBinding[index]
```

Virtual区域永远不进入final Physical Transform writer。任何代码尝试为Virtual Bone请求Physical Transform binding、Humanoid mapping或FootPlacement rig binding都必须失败；source capture继续只为Physical Bone持有`TransformStreamHandle`。

### Tradeoff

- 优点：没有场景对象开销，也不会把数据骨骼误当蒙皮骨骼。
- 代价：现有把`Bones.Count`同时用于binding与Pose的API必须破坏性改名和迁移。

## Decision 8: Projection保存完整不可变Rig/Pose ABI

compiled Rig payload至少包含：

- Rig schema、RigId与RigRevision。
- Physical Bone payload与parent-first index。
- Virtual Bone payload、Source/Target physical index与dense pose index。
- Physical/Pose统一BoneId到dense index catalog与Bone Kind。
- Physical reference local/component pose。
- Virtual reference local pose。
- `PhysicalBoneCount`、`VirtualBoneCount`、`PoseBoneCount`。
- Root、left foot、right foot physical index。

PosePlan Compiler使用完整catalog解析Mask、Profile、ModifyBone与TwoBoneIK。ProjectionRevision与content hash必须包含Virtual Bone稳定顺序、identity、display-independent Source/Target关系和TwoBoneIK描述。DisplayName只影响authoring显示，不应单独改变Runtime hash；若项目统一revision策略要求任意asset mutation改变revision，则仍通过RigRevision使Projection stale。

Gameplay SemanticHash、Numeric ProgramHash、State codec和Network ABI不包含Virtual Bone，因为它只属于Presentation Projection。

## Decision 9: Authoring入口属于唯一Rig Inspector

Rig Inspector以两个明确区域显示：

```text
Physical Bones
Virtual Bones
```

Virtual Bone操作：

- `Add Virtual Bone`创建稳定VirtualBoneId和可编辑DisplayName。
- Source与Target从同一Rig的Physical Bone catalog选择。
- Remove只删除精确VirtualBoneId，并让引用它的Mask/Profile/PoseGraph进入Invalid/Stale。
- Rename只改DisplayName，不按名称重建identity。
- Reorder改变dense Virtual Bone顺序与Rig revision，必须明确Undo/dirty。

PoseGraph TwoBoneIK Details只从当前Profile Rig提供Bone picker：

- End只显示Physical Bone。
- Effector显示合法Physical/Virtual Pose Bone并排除chain。
- Joint Target reference显示合法Pose Bone。
- Details显示Virtual Bone Source/Target只读摘要和精确Rig跳转。

Rig编辑、Mask编辑、TwoBoneIK编辑和Preview target切换只标记Dirty/Invalid/Stale，不自动Build Projection、Foot Analysis或Motion Matching Database。

## Decision 10: Preview与Diagnostics观察同一完成页

Authoring Preview执行正式Projection和PosePlan，不能为Virtual Bone创建简化计算器。Pose Watch可按VirtualBoneId订阅：

- local position/rotation。
- component position/rotation。
- Source/Target Physical Bone。
- 当前节点前后贡献。
- Mask/Profile权重。

TwoBoneIK diagnostics按PoseNodeId发布：

- Root/Joint/End physical index。
- Effector与Joint Target reference identity。
- Weight与rotation mode。
- target distance、两段长度与reach状态。
- solve前后End component pose与残差。
- typed failure code。

数据只从已完成Pose workspace复制到有界diagnostic page，不重新求值Virtual Bone或TwoBoneIK，不保存无界历史。

## Decision 11: Corin使用武器相对双手参考

Corin Rig新增两项：

```text
VB_Weapon_LeftHand
  Source = Bip001_Prop1
  Target = Bip001_L_Hand

VB_Weapon_RightHand
  Source = Bip001_Prop1
  Target = Bip001_R_Hand
```

正式BoneId使用完整稳定identity，不依赖上述显示短名。两项Virtual Bone都从每个Timeline、Blend Space或Motion Matching source自己的采样姿势派生。

Corin PoseGraph在FullBody Action composition和参数解析之后、FootPlacement之前串联左右臂TwoBoneIK：

```text
Base/Action composition
  -> Resolve Pose Parameters
  -> Left Arm TwoBoneIK(VB_Weapon_LeftHand)
  -> Right Arm TwoBoneIK(VB_Weapon_RightHand)
  -> FootPlacement
  -> OutputPose
```

FullBody Action Mask对两项Virtual Bone使用1，使攻击、闪避与换武器动作能够更新自己的握持意图。未来只造成漂移的呼吸/瞄准Additive Mask可对它们使用0，使该层改变真实上身但不改变武器相对手部参考。本change不为了展示功能伪造一条呼吸动画source。

per-bone Blend Profile为两项Virtual Bone显式配置transition multiplier；具体值以对应武器/手部过渡设计为准，不能由代码自动继承。

### Tradeoff

- 优点：直接使用Corin已经存在的武器与手臂骨骼，在现有FullBody Action过渡中形成真实消费者。
- 代价：双手都由独立动画武器分支驱动的假设必须由Corin Rig和动画资产满足；若某角色的武器实际绑定在主手下，应按该角色正式Rig选择不同Source，而不是复用Corin配置。

## Decision 12: FootPlacement和Virtual Bone保持严格隔离

FootPlacement继续读取：

- final physical ankle/toe/sole pose。
- Foot Analysis feature。
- Foot Placement Weight。
- Body visible motion。
- PhysicsScene support query。
- Foot Lock与prediction状态。
- `CharacterFootPlacementRigCalibration`。

它不读取Virtual Bone列表，不用Virtual Bone表示heel/toe offset、未来落点、surface anchor或pole。Rig v3唯一声明pelvis与左右hip-knee-ankle-toe Physical chain，`CharacterAnimationRigBinding`只提供对应Physical Transform绑定。

这样手部TwoBoneIK是纯Pose约束，脚部FootPlacement是world-aware接触约束，两者不会争夺同一数据真相。

## Identity And Rebuild

Rig schema提升会改变RigRevision并使以下产物明确Stale：

- Character Presentation Projection。
- Bone Mask与per-bone Blend Profile的Rig binding。
- 引用Rig revision的Foot Analysis artifact。
- 引用Rig revision的Motion Matching Database或Blend Space编译产物。

所有重建只能由现有明确Build命令触发。Inspector、asset selection、domain reload和Preview不得自动重建。旧产物不兼容读取，也不在Runtime根据Physical骨骼相同而跳过identity检查。

## Migration

### 第一阶段：不接入

1. 冻结最小公共合同。
2. 并行完成Virtual Bone Pose Derivation。
3. 并行完成Two Bone IK Pose Solver。
4. 并行完成Pose Constraint Diagnostics Contract。
5. 审计三个模块没有生产注册、serialized schema修改、资产修改或自动Build触发。

第一阶段不迁移任何数据，也不让Runtime、Preview或Corin消费新模块。

### 第二阶段：解除门禁后统一接入

1. 再次确认Selection/PoseGraph/BlendStack/Inertialization/workspace依赖已经安装。
2. 合并`add-character-presentation-blend-space`对最终node catalog、source capture与Corin PoseGraph的改动。
3. 将Rig Definition与payload schema升级为Physical/Virtual模型。
4. 原子迁移所有Rig v1资产为Rig v3；旧`Bones` serialized字段和reader删除。
5. 迁移Rig Binding、Foot Analysis、Motion Matching与所有显式数量语义。
6. 让source capture和全部Pose workspace复用第一阶段Virtual Bone模块。
7. 扩展Mask/Profile并迁移全部资产显式覆盖完整Pose Bone。
8. 安装TwoBoneIK authoring、compiler与native operation，并只调用第一阶段solver。
9. 把第一阶段diagnostics合同接入Preview、Pose Watch与Live Debug。
10. 向Corin Rig加入两项武器手部Virtual Bone，向最终PoseGraph加入双臂TwoBoneIK。
11. 删除旧generated Projection并通过明确Build入口生成匹配新Rig/PosePlan的唯一Projection。
12. 更新current specs与`openspec/project.md`，确认没有旧Rig v1、图外Virtual Bone缓存或第二IK循环。

第二阶段迁移期间不得让v1/v2 Rig同时被Runtime接受，也不得让没有Virtual Bone的旧Mask/Profile由默认权重临时运行。

## Failure Semantics

- Rig结构非法：Rig authoring validator失败并定位VirtualBoneId、Source或Target。
- Mask/Profile缺少Virtual Bone：Projection Compiler失败并定位asset与缺失BoneId。
- Runtime Rig Binding数量不匹配：Presentation创建失败；不搜索Transform补齐。
- source capture出现非有限component pose：当前source capture失败并保留typed原因；不使用上一帧Virtual Pose。
- TwoBoneIK chain、Effector或Joint Target跨Rig/非法：PoseGraph Compiler失败。
- TwoBoneIK运行时退化：节点typed failure，必需Output路径不得发布旧Pose。
- Projection/Rig revision不匹配：Presentation创建或Preview启动失败；不读取旧Projection。
- world context缺失：仍只影响FootPlacement阶段，不改变Virtual Bone或TwoBoneIK的纯Pose执行。

## Risks

- Rig数量语义拆分会影响大量数组长度和索引。通过破坏性改名`PhysicalBoneCount`/`PoseBoneCount`并删除含糊API，避免静默选错。
- Virtual Bone增加每个活跃source的Pose与velocity成本。通过固定少量authoring项、append-only dense layout和预分配scratch限制成本。
- TwoBoneIK可能在极端姿势退化。通过显式Joint Target、无stretch、编译期reference检查和typed runtime failure暴露问题，不猜默认pole。
- Virtual Bone/TwoBoneIK业务配置已经完成；后续Pose authoring重构只在一次Document v3事务中重新编码最终Corin图。Blend Space与Motion Matching的独立内容不得再覆盖Corin Rig、Mask、Profile或PoseGraph。
- 作者可能把Virtual Bone误当世界锚点。通过Inspector帮助、Bone Kind、FootPlacement隔离和诊断空间标签明确语义。

## Rejected Alternatives

### 在MonoBehaviour中缓存手部Transform

实现简单，但缓存只看到最终场景Transform，不知道每个source/Blend/Mask的贡献，也无法与Preview共用。会形成图外第二姿势链，拒绝。

### 创建隐藏GameObject作为Virtual Bone

IK插件容易引用，但需要生命周期、层级、同步顺序和销毁规则，也会让Binding和场景成为第二份Rig真相，拒绝。

### 要求美术在FBX中增加辅助骨骼

真实辅助骨骼适合明确需要AnimationClip轨道或附件挂点的情况；本需求只需由现有Source/Target自动派生的数据关系。强制修改FBX与全部动画来源成本更高，拒绝作为Virtual Bone实现。

### 在TwoBoneIK节点入口临时计算Source/Target关系

无需扩展Pose ABI，但该关系无法经过Blend Stack、Stored Pose、Inertialization和Bone Mask，仍然不是基础动画分支的参考，拒绝。

### 在最终composition后统一重算Virtual Bone

可以保证Virtual Bone与Target始终重合，但这会抹掉上游参考，TwoBoneIK读取它没有任何修正作用，拒绝。

### 把手部稳定接入FinalIK LimbIK

可以复用第三方solver，但需要Transform target与图外更新，并与FootPlacement adapter生命周期耦合。纯Pose TwoBoneIK边界更清晰，拒绝。

### 只增加Virtual Bone数据而不增加消费者

数据模型可以提前准备，但项目不会产生可见业务价值，也无法验证Mask和执行顺序是否正确。首个change必须同时安装TwoBoneIK与Corin武器双手消费者，拒绝空基础设施。

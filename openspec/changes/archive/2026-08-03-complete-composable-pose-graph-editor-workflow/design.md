# Design: 可组合 Pose Graph、世界感知骨骼控制与完整作者链

## Context

当前运行链名义上是：

```text
Pose Graph FootPlacement node
  -> native operation复制输入Pose
  -> ComposedAnimationPoseFrame
  -> CharacterSimulationPresentationRuntime.PresentPosePostProcess
  -> CharacterFootPlacementRuntime
  -> ICharacterFootPlacementSolver
  -> FinalIK LimbIK直接写Transform
  -> FinalAnimationPoseFrame
```

这条链有三个根本问题：

1. 作者拓扑与真实执行拓扑不一致。
2. Foot Placement结果不属于节点输出，无法继续连接、观察或在同一workspace中诊断。
3. Pose空间没有进入类型系统，骨骼控制依赖实现者记住隐含坐标约定。

目标链为：

```text
Fact / Parameter / Source Demand
  -> Source Capture once
  -> Local Pose stages
  -> Local To Component
  -> Component Pose controls
       Modify Bone
       Two Bone IK
       Foot Placement(world-aware planner + analytic limb solve)
  -> Component To Local
  -> later Local Pose stages
  -> Output Pose
  -> Final Physical Writer once
```

## Goals

- 让作者图、编译计划、运行结果、Preview与Diagnostics表达同一条真实拓扑。
- 让Pose空间成为正式端口类型，不依赖隐含约定。
- 让world-aware节点能出现在DAG中间并继续组合。
- 保持source一次采样、一次PlayableGraph Evaluate与一次final write。
- 让Foot Placement规划与骨骼求解实现分离，但保持一个作者节点。
- 让Rig、Calibration、Analysis、Projection和Runtime共享唯一identity。
- 让持续Pose Source拥有和Timeline同级的时间编辑能力，但不改变数据owner。
- 不保留旧路径、fallback、兼容reader或第二作者入口。

## Non-Goals

- 不把Pose Graph变成递归虚调用runtime。
- 不把world query放进Animation Job或Burst job。
- 不把Foot Placement计划暴露为作者必须连接的业务数据。
- 不要求所有骨骼控制都world-aware。
- 不把Locomotion Sequence包装为Action Timeline。
- 不自动Build或自动修复非法图。

## Decision 1: Pose空间是正式端口类型

Pose端口使用两个stable type：

- `pose.local`：每根骨骼相对父骨的Local Pose。
- `pose.component`：每根骨骼相对角色Animation Root的Component Pose。

转换只能通过显式节点：

- `LocalToComponentPose`
- `ComponentToLocalPose`

Capability为每个port声明数据类型、Pose空间、方向、multiplicity与availability。Graph Canvas根据Capability投影不同端口颜色和空间标签；Details显示节点工作空间；Validator与Compiler读取同一声明。

Compiler不得自动插入转换。自动插入会让serialized authoring图与实际执行图再次分裂，也会让转换成本和Pose Watch位置不可见。作者可以把多个Component Pose控制节点放在一对转换之间，避免重复全骨架转换。

节点空间分配：

- Local：Players、PoseStateMachine、AnimationSlot、BlendStack、Inertialization、BlendPose、LayeredBoneBlend、AdditivePose、PoseParameterResolve、PoseSubgraph、RootOrientationWarp。
- Component：ModifyBone、TwoBoneIK、FootPlacement。
- GraphInput/GraphOutput：由subgraph签名显式声明Local或Component，不做通配。
- OutputPose：只接收Local Pose。

业务取舍：显式节点增加两次作者连线，但换来可读拓扑、可预测成本、可靠验证和可扩展骨骼控制组合。隐藏转换只减少表面节点数量，却重新制造隐式执行。

## Decision 2: Compiler生成有序staged Pose Plan

作者图仍是typed DAG。Compiler先按依赖拓扑排序，再按执行领域切分连续stage：

- `FactAndDemand`
- `SourceCapture`
- `PurePose`
- `WorldAwarePose`
- `FinalPublication`

stage不是作者数据，不保存进Graph JSON。一个图可以形成：

```text
PurePose 0
WorldAwarePose 0
PurePose 1
WorldAwarePose 1
PurePose 2
FinalPublication
```

每个operation仍拥有稳定PoseNodeId、typed input/output workspace index、space、completion slot和diagnostic slot。world-aware stage在主线程读取同一帧Body frame与精确world context，完成后把Pose workspace交给下一stage。PurePose stage可使用现有Native/Burst适合的数据实现，但不得重新采样source。

约束：

- 同一个source demand与capture每帧最多一次。
- PlayableGraph每帧最多Evaluate一次。
- 节点每个call-site每帧最多完成一次。
- Physical Transform只由final writer写一次。
- stage失败阻断后续stage与FinalPublication，不发布部分Final Pose。
- Animancer Evaluate Barrier前失败只Discard Pending；stage失败已跨过Barrier时，同一Actor Animation Presentation Runtime进入Faulted，不逆序恢复状态或Physical Bone快照。

业务取舍：staged executor比固定尾后处理更复杂，但这是world-aware节点可组合且保持单次采样的必要基础。为降低作者心智负担，stage只在Compiled Plan/Diagnostics显示，不要求作者手工排stage。

## Decision 3: Foot Placement是一个复合Skeletal Control作者节点

`FootPlacement`接收并输出`pose.component`。它内部调用两个正式模块：

```text
CharacterFootPlacementPlanner
  input: Body frame, upstream component pose, generated foot features,
         profile, calibration, physics scene, prior lifecycle state
  output: CharacterFootPlacementPlan

CharacterComponentPoseLimbSolver
  input: upstream component pose, Rig v3 chains, plan
  output: corrected component pose
```

Planner不写骨骼；solver不做world query、不判断contact、不读取AnimationClip。`CharacterFootPlacementPlan`保留为runtime与diagnostics合同，但不成为Graph port。

不拆成作者可见的`Foot Placement Plan -> Leg IK`两个节点，原因是当前产品只有一个确定的消费关系：Foot Placement的双脚与pelvis计划必须原子应用。暴露Plan port会要求额外的数据lineage、双脚数组、pelvis约束和completion规则，却没有给作者带来可替换的常见组合。通用单肢需求继续使用独立`TwoBoneIK`节点。

该节点继续允许每个最终Output路径最多一个有状态Foot Placement实例，防止两套contact/lock lifecycle互相竞争；框架本身允许未来出现其它world-aware节点。

## Decision 4: 解析式Pose solver替换Final IK Transform adapter

项目已有Native TwoBoneIK Pose solver，可提取共享的Physical chain解析式求解内核。Foot Placement solver执行：

1. 复制上游Component Pose到输出workspace。
2. 按计划求解pelvis component translation。
3. 对左右腿分别应用reach clamp、保留动画bend plane、near-extension稳定、ankle semantic rotation。
4. 重算受影响Physical descendants与依赖Virtual Bones。
5. 发布每腿reach、bend、rotation和completion diagnostics。

Final IK只被旧Foot Placement和Gameplay Lab校验使用，因此删除`FinalIKLimbFootPlacementSolver`、`ICharacterFootPlacementSolver`及其启动校验不会影响其它业务。Core Pose runtime不再引用`RootMotion.FinalIK`。

业务取舍：Final IK提供现成MonoBehaviour solver，但它直接写Transform，天然绕开Pose workspace与节点输出；解析式solver需要承担完整腿部数值边界，却能保证组合、预览、诊断、单次final writer和vendor-neutral runtime。

## Decision 5: Rig v3唯一拥有腿部语义链

Rig v3在Physical/Virtual catalog之外增加：

- Pelvis BoneId
- Left Leg：Hip、Knee、Ankle、Toe BoneId
- Right Leg：Hip、Knee、Ankle、Toe BoneId

旧LeftFootBoneId/RightFootBoneId删除。`CharacterFootPlacementRig`组件删除。`CharacterAnimationRigBinding`继续只负责`Physical BoneId -> Transform`绑定，world query排除信息由通用`CharacterWorldAwarePresentationBinding`提供，不保存第二份腿骨或solver。

Sampling Rig工具增加Rig Mapping页：从精确prefab的正式bone catalog选择pelvis和双腿chain，显示parent chain与长度，拒绝重复、悬空、非Physical或非父子链。Calibration页只直接编辑heel/toe contact；sole frame由heel-to-toe与VisualRoot up自动派生，preferred bend reference由精确校准预览姿势的`Hip -> Knee -> Ankle`弯曲方向自动派生并只读显示。工具不得把bend direction伪装成从hip出发的可拖位置，也不得提供第二份手工pole配置。两页使用同一个预览姿势和同一个Rig identity。

Foot Analysis Source正式保存：

- Rig Definition identity/revision
- Sampling Rig prefab identity/revision
- Calibration identity/revision
- Preview clip/time只作为editor view配置

Analyzer artifact key与Projection依赖hash包含前三者。禁止按Transform名称、Humanoid avatar或左右命名猜测骨骼。

## Decision 6: Pose Source Editor复用时间编辑模块，不复用Timeline数据

Presentation Profile的Pose Source binding仍是持续source唯一owner。`Open Source`进入按source kind分派的正式编辑页：

- Sequence：Source/Sync Markers/Curves/Analysis/Preview。
- BlendSpace：进入BlendSpace编辑器，并在sample编辑上下文复用Marker、Curve与Analysis模块。
- Motion Matching：进入Source Set/Database编辑器，显示source artifact与query fixture。

Sequence Source页面复用Timeline Field已经成熟的模块：

- 时间尺、zoom/pan、playhead与seek。
- Marker lane的拖动、新增、删除、分组、循环闭合与identity。
- Curve lane的实际插值、key/tangent、weighted tangent、multi-select、box-select、copy/paste与Undo transaction。
- Foot Analysis候选显示、过期诊断与显式Apply。

这些模块通过typed authoring adapter读写当前owner；它们不依赖Timeline Track/Clip类型，也不把Pose Source复制成Timeline。`SyncMode/SyncGroupId/Topology/SyncRole/Markers`与typed Foot Placement Weight curve完整进入binding schema。

业务取舍：共用交互实现能让两类动画作者体验一致；保持数据owner分离则避免Locomotion生命周期被误建成有限Action。

## Decision 7: Pose Graph Preview只接受Fact fixture

Pose Graph Preview控制：

- Grounded
- Horizontal Speed
- Acceleration
- Vertical Speed
- Movement Direction
- Desired Direction
- Facing Error
- Motion Phase
- Capability登记的typed parameters

Preview调用现有`EvaluatePoseGraphPreview`路径，不显示Action producer selector。Action Timeline Preview继续由Timeline页面负责。

World context只能来自作者明确选择的精确`CharacterPipelineHost`：Definition、Presentation Profile、Animation Rig Binding、World-Aware Binding、Body fixture和该GameObject所在Scene的PhysicsScene必须完整且identity匹配。完整时Preview执行同一staged executor；不完整时在第一个world-aware节点返回typed Unavailable。禁止创建无限平面、默认地面、默认Rig或历史Pose。

Preview target互斥租约、非连续seek重置与Projection revision规则沿用共享`AnimationPreviewRuntime`。

## Decision 8: Pose Watch与Live Debug读取节点completion

每个Pose operation完成时向预分配diagnostic slot发布：

- PoseNodeId/call-site
- stage index/execution domain
- Local或Component空间
- availability/completion/generation
- contribution与source lineage摘要
- world capability状态
- Foot Placement planner/solver摘要

Pose Watch从同一frame workspace复制选中节点输出，不再次求值。Foot Placement Watch因此能看到pelvis与腿部已修正Pose。Live Debug只读取Trace/snapshot，不遍历Transform或Final IK组件反推结果。

## Decision 9: Capability、Document与Mutation共同闭合新语义

Capability Catalog为每个Pose节点声明：

- stable kind与graph kind
- typed payload
- static/dynamic ports及Pose空间
- execution domain
- required references
- Details presenter
- validator/compiler handler

Document只保存作者真相：节点、typed payload、显式转换、edge与binding。stage index、workspace、compiled Bone index、PhysicsScene与runtime completion不进入editable JSON。

Rig v3、Calibration、Foot Analysis artifact、Projection和compiled stage plan作为只读context输出。Reconciler只通过正式Presentation Mutation修改作者资产。现有五个Document生命周期工具不变。

Marker Sync可写owner规则统一为：

- 有限Action：Timeline AnimationTrack。
- 持续Pose Source：Presentation Profile binding。

两者使用同一schema和validator，但不复制数据。

## Decision 10: 原子迁移并删除旧链

迁移顺序：

1. 安装Pose空间、转换节点、Capability和Document schema。
2. 安装Rig v3与Sampling Rig Mapping/Calibration authoring。
3. 安装staged compiler/executor与component-pose solver。
4. 把Foot Placement接入真实节点输出。
5. 安装Pose Source Editor和Fact Preview/Pose Watch/Live Debug。
6. 通过正式Mutation迁移Corin Rig、Profile、sources和Pose Graph。
7. 删除旧通用Pose端口、Rig v2、Final IK、composition、图外postprocess和旧Inspector/Preview。
8. 在合法Calibration/Rig上下文中显式生成artifacts、Projection与Native Pose Program。

迁移不提供旧asset reader或运行时兼容。任一必需资产未迁移时Build明确失败。

## Risks

- Component/Local全骨架转换会增加Pose workspace与运算量。通过一对转换包住连续Component controls，并由Compiler显示stage/转换成本解决；不隐藏转换。
- 解析式腿部solver替换成熟第三方solver可能暴露数值边界。通过复用现有TwoBoneIK内核、保留Calibration v2语义、显式reach/bend diagnostics解决；不保留Final IK fallback。
- Timeline Field模块当前可能直接依赖Timeline数据类型。实施时先提取纯交互/几何/渲染合同，再让Timeline与Pose Source分别提供typed adapter；禁止复制一套简化curve editor。
- staged executor会触及当前Native Job和main-thread边界。通过固定operation/workspace、一次source capture和单一outer transaction保持热路径有界。
- Rig v3是破坏性资产变更。通过唯一迁移命令和严格identity校验完成；不在Inspector生命周期自动迁移。

## Open Questions

无。实现按本设计的唯一链路推进；若实施发现必须绕过共享Graph、正式Mutation、单次source capture或单次final writer，必须停止并重新提案。

# Design: 以不可变Program和深Module重构Pose Graph

## Context

Pose Graph的正式业务链已经明确：

```text
Presentation Fact
-> PoseStateMachine
-> state-local Player
-> AnimationSlot
-> Local Pose stages
-> LocalToComponentPose
-> Component Pose controls
-> Goal Contributions
-> Goal Assembler
-> FullBodyIK
-> ComponentToLocalPose
-> OutputPose
-> FinalAnimationPoseFrame
```

问题发生在这条链的实现层。当前结构更接近：

```text
PosePlanExecutionRuntime
├─ PoseState、Player、Stack、Slot、Routing
├─ Clip/BlendSpace/MM source与Animancer
├─ Physical Source与release
├─ Native Program与Workspace
├─ 每帧构造Staged Executor
├─ Foot、Goal、FBBIK、Writer
└─ Diagnostics

CharacterPoseGraphNativeProgram
├─ 静态Operation/Stage/常量
├─ 运行控制页
├─ Pending/Committed状态
└─ Goal workspace

CharacterPresentationPosePlanCompiler.CompilationState
├─ Graph closure
├─ typed端口
├─ 节点特例
├─ Value与Workspace
├─ Operation
└─ Stage
```

这种结构导致三个根问题：

1. **所有权不清楚**：外层Runtime、Executor、Constraint和Diagnostics都能理解同一个Operation或结果。
2. **数据寿命不清楚**：不可变Program、每Actor跨帧状态和当前Frame Pending页混在相同对象里。
3. **知识不集中**：一个Node Kind或Operation字段的定义分布在作者、编译、验证、Runtime和诊断多个位置。

本change以删除测试作为Module深度标准：删除一个Module后，如果其复杂度只是消失而不会重新散到调用方，它是浅包装；如果删除后调用方必须重新实现大量顺序、不变量和业务知识，它才提供足够Depth。本change不以文件数或类数作为架构结果。

## Goals

- 外层Animation Runtime只编排帧阶段，不理解节点、source资源、Constraint数学、Workspace布局或Writer细节。
- 静态Program、Actor跨帧状态、Frame Pending结果和Diagnostics具有完全不同的类型与Owner。
- 每个Operation在每帧只有一个执行Owner，每个业务结果只有一个写入Owner。
- Node authoring、Document、Clipboard、Validation与Lowering共享一个Node Definition真相。
- Compiler每个Pass具有不可变输入输出，错误可以定位到明确阶段。
- Runtime ABI只允许Operation Family的合法字段组合，不能用`-1`表达任意非法组合。
- 迁移保持实施前已验收动画行为，不同时改变算法政策。
- 旧路径在对应正式替代接入后直接删除，不保留兼容或fallback。

## Non-Goals

- 不把Pose Graph改造成运行时对象解释器。
- 不把所有内部Implementation都抽象成接口；只有真实存在多个Adapter的Seam才暴露可替换Interface。
- 不让Node Definition承担跨节点全局拓扑规则。
- 不让Source Module决定PoseState、Transition、Slot或Blend权重。
- 不让Program Runtime直接访问Unity World Query、FinalIK对象或Physical Transform。
- 不改变动画数学、Foot政策、FBBIK策略、Transition Routing或内容资产。

## Decision 1: 最终Module结构

正式运行结构固定为：

```text
CharacterAnimationPresentationRuntime
│
├─ CharacterPoseProgramRuntime
│  ├─ CharacterPoseProgramImage
│  ├─ CharacterPoseActorState
│  ├─ CharacterPoseFrameTransaction
│  ├─ Persistent Executor Implementation
│  └─ node-local state implementations
│
├─ CharacterPoseSourceModule
│  ├─ provider sample adapter
│  ├─ Animancer/Playable source backend
│  ├─ Physical Pose Source Registry
│  └─ prepared/deferred resource lifecycle
│
├─ CharacterPoseConstraintRuntime
│  ├─ Foot Placement Module
│  ├─ PoseBone Goal Source
│  ├─ Goal Assembler / Goal Set
│  └─ FBBIK / BendHistory / Solver Result
│
├─ CharacterFinalPosePublication
│  ├─ complete Final Pose validation
│  ├─ Physical Writer binding/apply
│  └─ Committed/Pending publication result
│
└─ CharacterPoseDiagnosticsProjector
   └─ committed typed results -> snapshot
```

`CharacterAnimationPresentationRuntime`是唯一帧级协调根，但不成为新的巨型业务Owner。它只知道五个事实：

1. 当前Frame Lease与Completion identity。
2. Module调用的固定顺序。
3. Animancer Evaluate Barrier之前与之后的失败规则。
4. 所有Module必须提交同一lineage。
5. 成功时原子Seal，失败时Discard或Fault。

各Module内部可以包含多个Implementation文件，但外部Interface必须保持窄而typed。调用方不得为了使用Module而知道Native页数量、offset、Operation index、内部state枚举或具体release列表。

## Decision 2: 不使用共享可变黑板，使用显式typed Result流

每帧数据流固定为：

```text
Committed Presentation Facts + Action Inputs
    |
    v
CharacterPoseProgramRuntime.PlanControl
    -> CharacterPoseSourceDemandResult
    |
    v
CharacterPoseSourceModule.Prepare
    -> CharacterPoseSourceFrameResult
    |
    v
CharacterPoseProgramRuntime.PrepareEvaluation
    -> CharacterPosePreparedEvaluation
    |
    v
Validate Animancer Barrier
    |
    v
Animancer Evaluate / Source Capture
    |
    v
CharacterPoseProgramRuntime.CompleteEvaluation
    -> 在每个Constraint Family Operation位置调用对应typed入口一次
    -> Constraint Complete只验证闭包并发布最终Result
    -> CharacterPoseProgramResult
    |
    v
CharacterFinalPosePublication.Apply
    -> CharacterFinalPosePublicationResult
    |
    v
Seal同一Frame Transaction
    |
    v
CharacterPoseDiagnosticsProjector
```

结果类型至少共享：

```text
FrameIdentity
CompletionIdentity
ProgramIdentity / ProjectionRevision
RigIdentity
Availability / Outcome
ResultPageLease
```

每个Module只获得自己需要的输入视图。例如Source Module接收Demand，不接收整个Program Workspace；Constraint Runtime接收Component Pose、Foot/PoseBone facts和Frame lineage，不接收Operation数组；Diagnostics Projector接收Committed Result，不接收Module实例。

`CharacterPoseFrameTransaction`可以在内部持有预分配页，但它不是允许任意Module读写的无类型黑板。每一页只有一个写入Owner，并通过只读typed view交给下游。

业务收益：数据从“谁都能看的一堆数组”变为“上一步明确产出的事实”，控制权能够沿链路追踪。

代价：需要定义更多Result类型；这些类型只表达阶段产物，不复制Implementation状态，因此不会形成旧式大DTO。

## Decision 3: 静态、Actor与Frame三种寿命彻底分离

### CharacterPoseProgramImage

Program Image在Projection Build后不可变，并作为`CharacterPresentationProjection`内部唯一Pose程序随同一ProjectionRevision原子发布。Runtime直接读取该Image并装配Actor Module，不再把Projection Pose Plan复制或转换为第二个Native Program容器。Program Image包含：

```text
SchemaVersion
ProgramIdentity / ProjectionRevision / ContractHash
RigIdentity
OperationHeader[]
Operation Family Payload pages
Typed Value Reference table
Stage Schedule
Constant pages
Source Map
Workspace Layout
Capacity Manifest
```

它不包含：

```text
当前Frame identity
Pending/Committed页索引
PoseState当前State
Player generation
Blend/Slot clock
Inertialization residual
Source ownership
Foot Context
Goal Result
Diagnostics数据
```

### CharacterPoseActorState

Actor State只保存会影响下一Presentation Frame的已提交状态：

```text
PoseState状态与时间
Player continuity与generation
Slot/BlendStack/Transition Routing状态
Inertialization history与accumulator
program-local persistent control state
```

Source物理资源状态归Source Module，Foot/Goal/FBBIK状态归Constraint Runtime，Committed/Pending Final Pose物理页归Final Publication；Program Actor State只保存Pose Program节点状态，不复制其它Module真相。

### CharacterPoseFrameTransaction

Frame Transaction只保存当前Pending结果：

```text
Frame Lease与唯一lineage
Pending node control state
Source Demand与Source Binding只读页
Pose/Value workspace，Final Output只保存Publication Pending页的typed write handle
Operation completion页
Module Result引用
固定mutation journal
interest-gated diagnostics页
```

成功Seal才提升Actor State和各Module Pending页；Discard不会改变Committed；跨Barrier失败进入现有Faulted政策。

业务收益：修改Program schema不会误碰Actor状态，Reset不会修改静态Program，Diagnostics不会读取被丢弃的Frame。

代价：构造Runtime时必须按Program Image容量一次性装配各类状态页，缺少容量直接失败。

## Decision 4: Program Runtime拥有逻辑节点，Source Module拥有物理采样

Pose Graph节点的逻辑Owner保持现有业务口径：

```text
PoseStateMachine -> State选择与Transition workspace
Player -> source endpoint、continuity与discontinuity
AnimationSlot -> Source/Action插入与handoff
BlendStack -> live/Stored entry与blend clock
Inertialization -> residual、history与rebase
```

这些状态由`CharacterPoseProgramRuntime`的Actor State持有，因为它们是编译节点的执行语义。

`CharacterPoseSourceModule`只负责：

```text
接收typed Source Demand
调用Clip/BlendSpace/MotionMatching/Action sample Adapter
解析Pending/Ready/Invalid
创建或复用Animancer/Playable source
安装source capture binding
维护Physical Source ownership
消费Program发布的usage/retirement permission
准备新资源与延迟释放旧资源
发布Source Frame Result和release completion
```

Source Module不得：

```text
选择PoseState
提交Transition generation
计算跨source blend weight
拥有Slot handoff
执行Inertialization
决定OutputPose
调用Foot Placement或FBBIK
```

Clip、Blend Space、Motion Matching和有限Action是Source Module内部的真实Adapter；因此该Seam有多个实际Adapter。Final Writer当前只有一个Implementation，不因“以后可能替换”建立平行抽象。

## Decision 5: Pose Constraint是Program调用的深Module，不是Program布局的一部分

前置Foot change完成后，`CharacterPoseConstraintRuntime`已经拥有Foot、Goal、Assembler、FBBIK和BendHistory。本change只收紧其外部Interface，并保持Graph中的Constraint Operation仍由Program Runtime逐个调度：

```text
BeginFrame(FrameLineage, ConstraintFrameFacts)
ExecuteFootPlacement(FootPlacementHandle, ComponentPoseView)
ExecutePoseBoneContribution(PoseBoneContributionHandle, ComponentPoseView)
ExecuteGoalAssembler(GoalAssemblerHandle)
ExecuteFullBodyIk(FullBodyIkHandle, ComponentPoseWriteView)
Complete()
-> CharacterPoseConstraintResult
```

每个typed Handle只表达对应Family的编译身份、typed Value引用与固定Result slot，不暴露Program数组布局。每个Constraint Operation在自己的Stage位置恰好调用一次对应入口并写入唯一Operation completion；`Complete`只验证整个Constraint闭包并发布最终Result，不重新执行任何Operation。调用方只能看到业务输入、阶段Outcome和最终Result；不得看到：

```text
NativeSlice<CharacterFullBodyIkGoal>
Goal offset/count
Operation index
Callsite index
Foot内部Context
Bank内部页
Diagnostics页
```

Program Image中的Constraint Operation Payload只保存上述typed编译Handle。Program Runtime遇到每个Constraint Family Operation时调用对应Constraint入口一次，并把返回的per-operation Result映射到Program Value；Constraint Module不得扫描Program或维护第二份Stage Schedule，外层Runtime不得提前执行，Executor不得再次验证业务算法。

Foot行为、Support、Pelvis、Goal编码和Bend策略以依赖change归档结果为Oracle，本change不调整公式或阈值。

## Decision 6: 每个Operation只有Program Runtime一个执行Owner

`CharacterPoseProgramRuntime`按Stage Schedule执行：

```text
FactAndDemand
SourceCapture
PurePose
WorldAwareValue
PureValue
FinalPublication preparation
```

每个Operation Header在一个Stage中出现一次，Program Runtime的持久Executor根据Operation Family调用内部Implementation或正式Module。运行中不允许：

- 外层Runtime预执行World-aware Operation。
- Staged Executor重新解释已执行Operation。
- Diagnostics为了取值再次执行Operation。
- Constraint Module扫描Program寻找自己的节点。
- Source Module扫描Operation决定State或权重。
- Writer从作者拓扑推断Output。

World Context在Frame开始以typed Adapter准备；Program Runtime只在编译标记的WorldAware阶段把该Adapter和业务输入传给Constraint Module。World Context缺失继续产生现有typed Unavailable/Fault结果，不插入默认地面或跳过节点。

业务收益：Foot、Linked Pose、Goal或Output问题可以从唯一Operation completion定位，不再发生“外层已经写值，Executor又认为未完成”。

## Decision 7: Final Pose Publication独占Physical写入

`CharacterFinalPosePublication`是具体深Module，拥有：

```text
Committed Final Pose物理页
Pending Final Pose物理页
完整Physical Bone binding
Final Writer Job binding
整Rig预验证
一次Apply
Publication Result
```

Program Image的Output Family不拥有第二Final Pose buffer，只持有指向Publication Pending页的typed write handle。Program Runtime执行Output Operation时通过该handle写入Pending Final Pose并发布只读`ProgramOutputPoseResult`；Final Publication随后只接收该Result与同一lineage，不读取Graph节点、Goal来源、Foot状态或Constraint内部Result。写任何Physical Bone前验证全部binding、Pose availability、Rig、continuity和completion；合法时一次写完整Pending Pose，不合法时保持Committed Pose并遵守现有Barrier/Fault规则。

Physical Writer成功后不得再运行会因业务数据失败的计算。Seal只发布已经验证的Program、Constraint、Source lifecycle和Final Publication结果。

## Decision 8: 唯一Node Definition Module

每种Node Kind由一个`CharacterPoseNodeDefinition` Adapter集中描述：

```text
NodeKind / CapabilityIdentity
PayloadType
FieldSchema / AuthoringCodec
FixedPortSchema
DynamicPortPolicy
AllowedGraphRoles
ExecutionDomain
OperationFamily
LocalPayloadValidation
RigValidation hook
TypedLowering
SourceMap naming
```

调用关系：

```text
Canvas/Create Menu --------┐
Document v4/Mutation ------|
Clipboard -----------------|-> CharacterPoseNodeDefinitionModule
Local Validator -----------|
Typed IR Lowering ---------|
Source Map ----------------┘
```

Definition只负责单节点局部合同。以下全局规则仍由Topology Pass拥有：

- Graph closure、递归与call graph。
- typed edge两端兼容。
- 唯一Output、Assembler、Goal Set、FBBIK和Writer。
- 重复Goal Slot、跨分支写冲突。
- Stage顺序、World-aware依赖和生命周期闭包。

不继续使用`Player/BlendPolicy/StateMachine/AnimationSlot/Inertialization/...`二十多个布尔能力。调用方需要业务信息时读取结构化定义，例如Ports、Placement、OperationFamily或SourceBindingRequirement，而不是自行switch NodeKind。

Agent Document、Clipboard和Editor不再复制Payload字段表。Definition只向现有Graph Authoring Capability、Document模型/strict codec、Exporter、Reconciler、typed Mutation与Validator投影节点局部字段、端口和role语义；它不得接管Document package路径、文件闭包、diff、Undo、rollback、save或reverse export事务。Definition变化如果改变authoring语义，必须同步这些正式调用者，但Reconciler与Document Transaction Service继续分别拥有唯一对账和事务生命周期，不建立Definition专用apply入口或第二catalog。

## Decision 9: Compiler使用固定不可逆Pass

唯一外部Interface：

```text
Compile(CharacterPoseCompilationRequest)
-> CharacterPoseCompilationResult
```

固定Pass：

```text
1. GraphClosurePass
   root flat catalog + linked entries
   -> CharacterPoseGraphClosure

2. TypedLoweringPass
   closure + Node Definitions
   -> CharacterPoseTypedIr

3. TopologyPass
   typed nodes/edges
   -> CharacterPoseTopologyPlan

4. SymbolicFamilyLoweringPass
   typed IR + topology
   -> CharacterPoseSymbolicOperationPlan

5. StageSchedulePass
   topology + symbolic operations + execution domains
   -> CharacterPoseStageSchedule

6. ValueLifetimePass
   topology + symbolic operations + stage schedule
   -> CharacterPoseValuePlan

7. WorkspacePlanPass
   topology + symbolic operations + schedule + values + Rig + capacities
   -> CharacterPoseWorkspacePlan

8. BindFamilyPayloadPass
   symbolic operations + stage/value/workspace handles
   -> CharacterPoseFamilyPayloadPlan

9. SealProgramImagePass
   全部不可变结果
   -> CharacterPoseProgramImage
```

每个Pass：

- 不修改输入Result。
- 不依赖后续Pass的可变字段。
- 只发布本阶段typed Result或结构化Diagnostic。
- Diagnostic携带GraphId、NodeId、PortId、Pass、稳定reason和source path。
- 不在Runtime重新执行。

`CharacterPresentationPoseGraphCompiler`若保留，只能成为这一唯一Compiler Module的薄入口；如果删除该入口会让复杂度直接消失而不会重新出现在调用方，则应直接删除，不保留pass-through。

## Decision 10: 分段typed Operation ABI

公共调度数据固定为：

```text
CharacterPoseOperationHeader
    OperationIndex
    OperationCode / Family
    ExecutionDomain
    PayloadIndex
    InputValueRefStart / Count
    OutputValueRefStart / Count
```

Value引用使用独立typed表：

```text
CharacterPoseValueReference
    ValueKind
    ValueIndex
    Access
```

Operation-specific数据进入Family页。全部现行Operation Code在ABI切换前必须进入以下唯一映射，不能遗漏后再保留旧Operation reader：

```text
ParameterInputPayload[]          <- ProgramParameterInput
ParameterResolvePayload[]        <- PoseParameterResolve
PlayerPayload[]                  <- SelectedPosePlayer / ClipPlayer / BlendSpacePlayer
StateMachinePayload[]            <- PoseStateMachine / StatePoseOutput
ActionInputPayload[]              <- ActionPlaybackInput
AnimationSlotPayload[]            <- AnimationSlot
BlendPayload[]                    <- BlendStack / BlendPose
InertializationPayload[]          <- Inertialization
CompositionPayload[]              <- LayeredBoneBlend / AdditivePose
SpaceConversionPayload[]          <- LocalToComponentPose / ComponentToLocalPose
ComponentControlPayload[]         <- ModifyBone / RootOrientationWarp
MotionMatchingPayload[]           <- MotionMatchingPose / ChooserResolve / EntrySourceCapture / EntryProcessing / InternalBlend
PoseHistoryPayload[]              <- PoseHistoryRead / PoseHistoryCommit
GoalContributionPayload[]         <- FootPlacement / PoseBoneIKGoals
GoalAssemblerPayload[]            <- FullBodyIkGoalAssembler
FullBodyIkPayload[]               <- FullBodyIK
LinkedPosePayload[]               <- LinkedPoseCall
OutputPayload[]                   <- OutputPose
```

`SymbolicFamilyLoweringPass`先产生上述Family、symbolic typed value依赖、跨帧状态需求、Frame页需求与Workspace需求，不分配物理index。`StageSchedulePass`固定执行位置后，`ValueLifetimePass`才能按真实消费阶段计算寿命；`WorkspacePlanPass`据此分配容量；最后`BindFamilyPayloadPass`只把symbolic引用绑定为typed handle，不得发现新的Operation、状态页或容量需求。

同一Family可在内部再用小型typed variant，但不得恢复全系统万能record。Seal时必须证明：

- Header的Family与Payload页一致。
- PayloadIndex在对应页范围内。
- 输入输出Value Kind与Definition端口一致。
- Stage与Execution Domain一致。
- 所有workspace handle位于预编译布局内。
- Operation只写自己的输出与完成页。
- 不存在无法由类型表达的`-1`字段组合。

Projection schema、Program Image schema、ContractHash与ProjectionRevision直接提升。旧Projection reader、旧万能Operation codec、字段默认补齐和版本fallback删除；旧generated资产必须显式重建。

代价是Payload数组数量增加，换来节点变化只影响对应Family页，Runtime无法构造大量无关字段组合。

## Decision 11: 持久Executor隐藏Workspace布局

当前Staged Executor每帧构造并复制大量NativeArray字段。新`CharacterPoseProgramRuntime`在Actor Runtime创建时按Program Image建立持久Executor Implementation：

```text
ProgramImage只读引用
ActorState页引用
FrameTransaction页引用
Source Result只读view
Constraint Module handle
Final Publication preparation handle
```

Executor内部可以按Family拆分Evaluator，但这些属于Program Runtime Implementation，不暴露给外层协调器。每帧只绑定新的Frame lease和页索引，不重新展开全部数组为大构造参数。

Workspace按数据职责分页：

```text
Pose Value pages
Parameter pages
Node control pending pages
Inertialization pages
Contribution/Goal handles
Operation completion pages
Final Output typed handle，指向Publication Pending Final Pose物理页
```

Program Runtime是这些页的唯一布局解释者。Constraint和Source Module只通过typed Handle/Result交换，不索引Program内部数组。

## Decision 12: Diagnostics只投影Committed Result

Frame开始冻结Diagnostics interest和容量。存在interest时，各Module在自己的Pending结果完成后向预分配诊断页深冻结允许观察的数据；不存在interest时不生成大页。

成功Seal后Projector只读取：

```text
CharacterPoseSourceCommittedResult
CharacterPoseProgramCommittedResult
CharacterPoseConstraintCommittedResult
CharacterFinalPosePublicationCommittedResult
```

所有Result必须匹配Frame、Completion、Program、Projection、Rig和Actor identity。Projector不得持有Runtime Module引用，不得读取：

```text
ProgramImage内部可变假设
Pending Workspace
ActorState私有页
Foot Context
FBBIK Vendor对象
Physical Transform反推
```

Pose Watch所需Pose、Goal和Contribution在运行帧完成时按interest冻结，Watch只读取这些Committed页，不重跑source、world query或FBBIK。

## Decision 13: Preview与Runtime只使用Adapter差异

正式Runtime和Preview使用相同：

```text
Program Image
Program Runtime
Source Module
Constraint Module
Final Publication
Frame Transaction
Completion语义
```

真实差异只存在于已有Seam的Adapter：

```text
Presentation Fact输入
Action输入
World Context
Source sample输入
Physical Rig host
Diagnostics target
```

Preview缺少精确World Context时返回typed Unavailable；不得使用简化Foot、跳过Constraint、临时Program、第二Executor或绑定默认地面。Stale Projection继续停止Preview并要求显式Build。

## Decision 14: Barrier与失败政策保持现行合同

```text
Barrier前
    identity/capacity/readiness/source/binding失败
    -> Discard Pending
    -> Committed不变

Animancer Evaluate Barrier内或之后
    Operation/Constraint/Writer/Unity失败
    -> 不发布Pending
    -> Actor Animation Runtime Faulted
    -> 不恢复Physical Transform后继续运行

成功
    -> Final Writer完成
    -> no-throw Seal统一提升Module结果
    -> acknowledge与deferred release
    -> 发布Final Pose与Diagnostics
```

Module不能拥有不同Frame identity或独立决定提前Seal。它们可以维护内部双页和journal，但只响应根Frame Transaction的唯一Seal/Discard结果。

## Decision 15: 物理目录与程序集Locality

实现完成后的职责目录建议固定为：

```text
Runtime/Character/Pipeline/Animation/PoseGraph/
    Contracts/
    Program/
    Runtime/
    Sources/
    Constraints/
    Publication/
    Diagnostics/

Editor/CharacterSimulation/Compilation/Presentation/PoseGraph/
    Definitions/
    Passes/
    Validation/
    Projection/
```

目录不是验收本身。类型放置以所有权和依赖方向为准：Contracts不引用Implementation；Runtime不引用Editor；Source/Constraint/Publication不反向引用外层协调器；Diagnostics只引用公开Result合同。

本change不因为目录建议创建新程序集。只有现有程序集依赖无法表达上述方向时才调整asmdef；不得建立循环引用或为了“模块化”复制合同。

## Migration

迁移在同一change中顺序完成，但不保留并行运行路径：

1. 等待Foot两项change和Blend Space完成归档，固定完整动画行为与Projection作为Oracle。
2. 建立Frame lineage、typed Result和Module依赖规则，只让现有单一路径携带新身份与结果合同，不提前创建空壳Module或收窄根Runtime。
3. 将`CharacterPoseConstraintRuntime`移入正式目录并按每个Constraint Family Operation收窄typed Handle/Result Interface；删除布局泄露调用。
4. 提取`CharacterPoseSourceModule`，原子迁移provider、Animancer、Physical Source和release所有权；从旧Runtime删除对应字段与方法。
5. 建立Projection内部唯一`CharacterPoseProgramImage + CharacterPoseActorState + CharacterPoseFrameTransaction`，把旧Native Program可变状态迁入唯一Owner。
6. 建立持久`CharacterPoseProgramRuntime`和Executor，迁移PoseState、Player、Slot、Blend、Inertialization与Operation执行；删除外层World-aware Operation扫描和旧Staged Executor双Owner。
7. 建立`CharacterFinalPosePublication`，迁移唯一Committed/Pending Final Pose物理页、完整验证和Writer；Output Family只保留typed write handle，旧Runtime不再直接操作Physical Bones。
8. 在四个Module都接通后收窄`CharacterAnimationPresentationRuntime`，删除其节点、offset、Constraint、source和Writer知识，只保留Frame阶段协调与唯一Seal/Discard/Fault。
9. 建立Node Definition Module，一次性迁移Capability、Authoring、Document节点局部投影、Clipboard、Mutation、local validation和lowering；保留唯一Document Reconciler与Transaction Service，删除旧Handler Registry与重复switch。
10. 将Compiler拆成`Symbolic Family -> Schedule -> Value Lifetime -> Workspace -> Bind Payload`固定Pass，删除中央`CompilationState`和pass-through入口。
11. 完成全部现行Operation Code到Family/Owner/Domain映射后，原子切换分段Operation ABI、Projection内Program Image schema和Runtime reader；删除万能Operation、第二Native Program容器与旧reader。
12. 将Diagnostics改为Committed Result Projector；删除对内部Program/Workspace/Constraint的读取。
13. 迁移Preview到同一Factory和Program Image，删除简化或重复执行路径。
14. 搜索并删除旧类、旧字段、旧codec、旧validator知识、兼容版本和未引用路径，更新project truth并完成编译与严格校验。

每一步替代完成后立即删除旧Owner。允许中间commit暂时无法编译，但不允许为了保持可运行而让新旧Owner同时执行、双写或通过开关切换。

## Risks And Tradeoffs

### 1. 一个大change还是多个独立change

将Runtime、Compiler和ABI拆成三个完全独立active change，单个文档更小，但它们共享同一核心类型和删除目标，容易在中间形成兼容Adapter或双Program。这里使用一个总change、内部按任务阶段串行实施，确保最终只有一条链。

代价是change较大，因此tasks必须按可编译的小闭环拆分，行为Oracle必须冻结。

### 2. Runtime所有权先于ABI

先切分段ABI能快速减少万能字段，但Owner仍混乱时，新Payload页仍会被多个调用方读取，问题只会换一种数据格式。因此先收Runtime所有权与寿命，再切Compiler知识和ABI。

代价是迁移早期仍短暂使用当前正式ABI；它不是fallback，而是尚未被后续任务替换的当前单一路径。

### 3. Stage与Family需求先于Value/Workspace物理布局

先分配Value和Workspace再生成Family与Stage，会迫使后续Pass回写容量或让Topology提前承载全部Family Implementation知识。这里先产生symbolic Family Operation并固定Stage，再计算Value寿命与Workspace，最后只绑定typed handle。

代价是Compiler多一个symbolic Operation Result，但它集中表达编译需求，不进入Runtime，也不复制最终Payload页。

### 4. Node Definition集中与全局Validator分离

把所有规则塞进Node Definition可以看起来“单一真相”，但唯一Output、Goal Slot冲突和Stage依赖本质上是Graph全局关系。强塞会让Definition相互查询并重新形成中央耦合。因此Definition只拥有局部语义，Topology Pass拥有全局规则。

### 5. Program Runtime较深

Program Runtime内部仍会包含多种节点Implementation和大量Native页；这是有意的Depth，不意味着回到巨型类。区别在于外部Interface很小、内部按Family保持Locality，且Program、Actor State、Frame数据已经分型。

### 6. Source Module不拥有逻辑选择

把Player、Blend和Transition一起放进Source Module可以减少表面Module数量，但会让Animancer资源生命周期重新成为动画选择权威。这里保留Program节点作为逻辑Owner，Source Module只实现采样与物理资源。

### 7. 不建立通用Operation插件系统

运行时可注册任意Handler能让节点“看起来可扩展”，但会破坏固定Projection、AOT可知容量、Stage静态证明和错误Locality。新增节点仍通过Editor Node Definition和编译后的有限Operation Family扩展；Runtime Family目录是显式封闭ABI，不使用反射或服务定位器。

### 8. Projection破坏性重建

分段ABI要求提升Projection schema并重建全部generated Projection。保留旧reader可减少一次内容重建，但会永久保留两套Operation解释规则，因此本change选择显式重建并删除旧schema。

## Rejected Alternatives

- **只拆分大文件**：不改变Owner和Interface，调用方仍理解全部内部字段。
- **给现有Runtime再包一层Facade**：删除Facade后复杂度直接消失，是浅Module。
- **保留旧Staged Executor并新增World-aware Executor**：会继续产生同一Operation两个Owner。
- **让Diagnostics直接读取Workspace以减少复制**：会让诊断与运行状态生命周期绑定，并可能观察Pending或被丢弃结果。
- **把Program Image与Actor State放在同一对象但标注readonly字段**：对象仍同时拥有不同寿命与Dispose/Reset语义，无法证明隔离。
- **保留旧Operation并增加typed payload可选字段**：形成双ABI和更多非法组合。
- **为FinalIK、Writer、World Query统一建立插件接口**：当前实际Adapter数量和变化方向不同，会制造假Seam；只保留已有真实Adapter位置。

## Spec Conflicts And Resolution

- active Foot change与本change都会修改`character-animation-pipeline`和`character-presentation-pose-graph`。通过明确顺序解决：先归档Foot架构与行为change，本change delta以归档后的Goal Contribution/Assembler/FBBIK合同为基线。
- active Blend Space change会修改Pose node、Projection和source runtime。通过先归档Blend Space解决；本change迁移其最终节点Definition与Source Adapter，不修改算法。
- current Pose authoring requirement写明`compiler handler`。本change明确修改为唯一Node Definition Adapter并删除Handler Registry。
- current `graph-authoring-domain-framework`要求Capability直接保存Compiler Handler。本change同步修改为Pose Node Definition向共享Capability投影UI与Document语义，Compiler从同一Definition取得typed lowering；Definition不得接管Document Reconciler或Transaction Service。
- current `btsmtl-compiled-simulation-program`与`project.md`并存`CharacterPresentationPoseProgram`、Native Pose Program称呼。本change统一为`CharacterPresentationProjection`内部唯一`CharacterPoseProgramImage`，Runtime不生成第二程序容器。
- 前置Pose Constraint change归档后，`character-foot-placement-presentation`会把Physical diagnostics放入Constraint根Bank。本change同步迁移为Constraint Committed Result只拥有Foot/Goal/FBBIK事实，Physical结果只属于Final Publication Committed Result，由Diagnostics Projector按同lineage组合。
- current动画事务允许各Module拥有内部Pending页，但没有明确Program Image、Actor State和Frame Transaction。新增runtime architecture spec把三种寿命分型，同时保留现有Barrier和Dense/Journal政策。
- current Diagnostics允许从完成workspace复制。本change收紧为运行帧内按interest冻结、Seal后只读取Committed Result，避免跨Owner读取；可观察字段不减少。
- archived设计曾表达Module分层，但archive不作为current truth。本change不依赖archive实现，只把仍有价值且与current spec一致的职责重新形成正式delta。

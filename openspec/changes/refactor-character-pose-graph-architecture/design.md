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

这种结构导致五个根问题：

1. **所有权不清楚**：外层Runtime、Executor、Constraint和Diagnostics都能理解同一个Operation或结果。
2. **数据寿命不清楚**：不可变Program、每Actor跨帧状态和当前Frame Pending页混在相同对象里。
3. **知识不集中**：一个Node Kind或Operation字段的定义分布在作者、编译、验证、Runtime和诊断多个位置。
4. **静态真相与执行存储混淆**：Projection中的序列化Program与每Actor构造的NativeArray容器没有正式分型，旧Native Program因此同时承担Compile、存储与状态Owner。
5. **在线调参没有独立Owner**：现行实现直接修改每Actor Native Operation、Player、Blend、Inertialization、Foot与FBBIK对象；共享不可变Program后如果不迁移这条链，会污染其它Actor或丢失作者调参能力。

本change以删除测试作为Module深度标准：删除一个Module后，如果其复杂度只是消失而不会重新散到调用方，它是浅包装；如果删除后调用方必须重新实现大量顺序、不变量和业务知识，它才提供足够Depth。本change不以文件数或类数作为架构结果。

项目现已导入ParadoxNotion CanvasCore、NodeCanvas与FlowCanvas源码。当前Pose作者入口仍由项目自建`GraphAuthoringCanvasView`、Pose节点／端口View、独立StateMachine表面和`CharacterTypedPoseGraph`存储组成；Canvas源码尚未进入Pose作者、Mutation或Compiler链。用户将本次新增范围限定为Pose Graph接入Canvas，BTSMTL与AI继续使用各自现行作者实现。共享边界因此只统一Capability、Document、Mutation、事务、Clipboard、Selection和Diagnostics合同，不再统一具体Canvas类型。`TrainingEnemy`当前同时拥有未闭合的Gameplay、AI、Character Pipeline、Presentation、PoseGraph、Prefab和生成资产，且已不具备正式构建闭环；用户2026-09-05决定整套删除，不让该半成品进入Canvas资产迁移。

## Current Implementation Baseline

用户指定行为基线为`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`，不是实施时最新HEAD或任意较晚IK版本。已核对该提交对应动画时钟、Pose混合、Foot输入与Lifecycle、Pelvis、Goal、FBBIK、Writer和Bank链路；具体入口与保护项见[行为保护清单](behavior-baseline.md)。2026-09-01检查时动画／IK、Corin配置及相关诊断目录与该提交无差异；后续相关差异必须单独报告，不自动吸收或回退。Foot／IK未完成和未归档不阻塞本change，已知问题不伪装成已通过。

| 对象 | 本次允许变化 | 必须保持 |
|---|---|---|
| Foot／Pelvis／Goal／FBBIK | 外层typed调用、存储归属与Result封装 | 内部算法、数值顺序、准入、权重、Profile值、正常初始化／Reset结果 |
| 根Bank与Frame事务 | 统一lineage、Pending页所有权、Seal／Discard连接 | 当前连续历史与提交／丢弃后的业务结果 |
| PoseGraph／Compiler／ABI | Program Image、节点定义、Pass、执行布局与单一调度Owner | 节点业务语义、source时间、最终Pose |
| Runtime诊断投影 | 从内部页读取改为消费同帧Result，并发布PoseGraph-owned具体`CharacterFootIkCommittedCaptureView`短租约 | 通用AOT生成、Generated Lifecycle、typed packet与Schema-driven Host由`add-generated-diagnostic-sampling-framework`拥有；Foot生命周期Event、字段、Sampler、Analyzer、Publisher和评分由`refactor-foot-ik-diagnostic-sampling`拥有 |
| TrainingEnemy内容 | 整套退役并删除配置、Prefab、场景组合、目标绑定、专用工具与生成产物 | 保留的Corin内容和无目标攻击语义不受影响 |

Foot输入继续是同帧Component Pose、正式Foot Motion、Body／World事实、Rig和Profile，输出继续是当前Foot结果、Pelvis Result及三个Goal Contribution；后续Goal Set、FBBIK与Physical Pose必须保持相同业务结果。Foot当前由Lifecycle使用Transition、State Target、Interpolation与Post Constraint，并在Pelvis后完成Landing；这里只记录已存在链路，不把这些内部阶段重新设计成PoseGraph公开合同。

当前保留的Reach观察／Landing资格与已经撤除的业务层硬挪骨盆、末端夹脚必须区分。不得依据旧spec恢复夹紧，不迁入被否决的SmoothKnee。当前未解决的抖动、穿透、离面或反弯保留为已知问题，不能在同一架构提交中修算法或改评分掩盖。

本次Goal按用户要求串行执行：`refactor-character-ik-maintenance-boundaries`先完成并验证Foot请求／最终结果、Interpolation历史、Solver Reset独立修正与诊断列绑定。本change以第一阶段通过提交作为接入点，保持这些成果并迁移唯一外部边界，不恢复旧结构或形成第二Owner。总源码与行为对照仍为`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`；Reset允许差异单独引用第一阶段证据。

## Goals

- 外层Animation Runtime只编排帧阶段，不理解节点、source资源、Constraint数学、Workspace布局或Writer细节。
- 静态Program、Actor跨帧状态、Frame Pending结果和Diagnostics具有完全不同的类型与Owner。
- 序列化Program Image是唯一语义真相；可选Native执行存储只能是同identity、共享、只读的物理View。
- 每个Operation在每帧只有一个执行Owner，每个业务结果只有一个写入Owner。
- Node authoring、Document、Clipboard、Validation、Graph Closure与Lowering共享一个Node Definition真相，并继续通过唯一Capability与Port Shape Projector投影作者端口。
- Pose Graph只使用一份基于CanvasCore的作者资产和一个具体Canvas，旧`CharacterTypedPoseGraph`存储与旧Pose Canvas在迁移后删除。
- Canvas迁移清单只包含保留的正式Corin内容；`TrainingEnemy`在迁移前整套删除，不生成Canvas资产或占位配置。
- Pose Canvas的创建、连线、删除、复制粘贴、Undo和Details编辑全部进入现有typed Presentation Mutation与Document事务。
- Compiler每个Pass具有不可变输入输出，错误可以定位到明确阶段。
- Compiler在第一次封存新ABI时同时产出Worker Batch Plan；Pure Pose通过有限Family Kernel执行，多个Actor由唯一会话级Scheduler合批。
- Runtime ABI只允许Operation Family的合法字段组合，不能用`-1`表达任意非法组合。
- 迁移保持实施前已验收动画行为，不同时改变算法政策。
- 旧路径在对应正式替代接入后直接删除，不保留兼容或fallback。

## Non-Goals

- 不把Pose Graph改造成运行时对象解释器。
- 不使用FlowCanvas的Flow／Value委托、事件、反射方法或Graph Update作为Pose Runtime。
- 不把BTSMTL或AI作者资产迁移到ParadoxNotion Canvas。
- 不引入UAF式运行时Evaluation Program或Trait执行框架。
- 不把所有内部Implementation都抽象成接口；只有真实存在多个Adapter的Seam才暴露可替换Interface。
- 不让Node Definition承担跨节点全局拓扑规则。
- 不让Source Module决定PoseState、Transition、Slot或Blend权重。
- 不让Program Runtime直接访问Unity World Query、FinalIK对象或Physical Transform。
- 不改变保留内容的动画数学、Foot政策、FBBIK策略或Transition Routing；`TrainingEnemy`整套内容删除是本次明确批准的范围变化。
- 不在本change引入动画预算、Phase Offset、跳帧、旧Pose复用或插值补帧。
- 不删除现有actor-local在线调参；只迁移调参Owner、原子生效和回滚边界。

## Decision 1: 最终Module结构

正式运行结构固定为：

```text
CharacterPoseWorkerScheduler (session-level)
└─ typed Actor Batch leases / Family Kernel / Completion

CharacterAnimationPresentationRuntime
│
├─ CharacterPoseFrameTransaction
│  ├─ Frame lineage / phase / outcome
│  └─ typed Module leases and results
│
├─ CharacterPoseProgramRuntime
│  ├─ CharacterPoseProgramImage / actor-local Execution View
│  ├─ CharacterPoseActorState
│  ├─ CharacterPoseProgramFramePages
│  ├─ Persistent Worker/Managed Execution Implementation
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

1. 当前根Frame Transaction、Lease与Completion identity。
2. Module调用的固定顺序。
3. Animancer Evaluate Barrier之前与之后的失败规则。
4. 所有Module必须提交同一lineage与Tuning Generation。
5. 成功时原子Seal，失败时Discard或Fault。

各Module内部可以包含多个Implementation文件，但外部Interface必须保持窄而typed。调用方不得为了使用Module而知道Native页数量、offset、Operation index、内部state枚举或具体release列表。

## Decision 2: 不使用共享可变黑板，使用显式typed Result流

每帧数据流固定为：

```text
Pending Tuning Request
    |
    v
Prepare Program/Source/Constraint Tuning
    -> Atomic CharacterPoseTuningSnapshot generation
    |
    v
CharacterAnimationPresentationRuntime.BeginRootFrame
    |
    v
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
Seal根CharacterPoseFrameTransaction
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
TuningGeneration
Availability / Outcome
ResultPageLease
```

每个Module只获得自己需要的输入视图。例如Source Module接收Demand，不接收整个Program Workspace；Constraint Runtime接收Component Pose、Foot/PoseBone facts和Frame lineage，不接收Operation数组；Diagnostics Projector接收Committed Result，不接收Module实例。

`CharacterPoseFrameTransaction`只持有根lineage、阶段、Module lease/result和统一Outcome，不持有任一Module内部Workspace。Program、Source、Constraint与Final Publication分别拥有自己的预分配Pending页，每一页只有一个写入Owner，并通过只读typed view交给下游。

业务收益：数据从“谁都能看的一堆数组”变为“上一步明确产出的事实”，控制权能够沿链路追踪。

代价：需要定义更多Result类型；这些类型只表达阶段产物，不复制Implementation状态，因此不会形成旧式大DTO。

## Decision 3: Program Image、Execution View、Actor、Owned Frame与根事务彻底分离

### CharacterPoseProgramImage

Program Image在Projection Build后不可变，并作为`CharacterPresentationProjection`内部唯一Pose程序随同一ProjectionRevision原子发布。它是唯一语义程序，不包含任何运行时内存地址。Runtime直接读取该Image并装配Actor Module，不得重新编译、重排或补齐字段。Program Image包含：

```text
SchemaVersion
ProgramIdentity / ProjectionRevision / PoseProgramImageHash
RigIdentity
OperationHeader[]
Operation Family Payload pages
Typed Value Reference table
Stage Schedule
Worker Batch Plan
Family Kernel Set / Execution Policy identity
Rig execution layout
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

### CharacterPoseProgramExecutionView

Unity执行层如果需要NativeArray、NativeSlice或其它不可序列化存储，Runtime MAY按Program Image建立一份`CharacterPoseProgramExecutionView`。该View必须：

- 逐值materialize同一个Program Image，不产生新Operation、不重排Stage、不补默认字段。
- 携带并验证相同Program identity、ProjectionRevision、PoseProgramImageHash与Rig identity。
- 每个`CharacterPoseProgramRuntime`最多建立一份只读View；Actor状态与Frame页不得进入View。
- 由对应Program Runtime唯一拥有生命周期，并在该Runtime Dispose时释放。
- 不暴露旧`CharacterPoseGraphNativeProgram`类型、旧schema reader或运行时Compile入口。

因此系统只有一个Program语义真相，同时允许Unity使用适合执行的物理存储。把逐Actor NativeArray副本继续称为Program、允许View修改Operation Weight或让View拥有Pending页都属于第二程序路径。

### CharacterPoseActorState

Actor State只保存会影响下一Presentation Frame的已提交状态：

```text
PoseState状态与时间
Player continuity与generation
ActionPlaybackInput lifecycle、command cursor与generation
Slot/BlendStack/Transition Routing状态
Inertialization history与accumulator
program-local persistent control state
```

Source物理资源状态归Source Module，Foot/Goal/FBBIK状态归Constraint Runtime，Committed/Pending Final Pose物理页归Final Publication；Program Actor State只保存Pose Program节点状态，不复制其它Module真相。

### CharacterPoseFrameTransaction与Owned Frame Pages

根Frame Transaction只保存：

```text
Frame Lease与唯一lineage
Tuning Generation
当前阶段与Barrier状态
Program / Source / Constraint / Publication typed lease
各Module最终Result引用
统一Seal / Discard / Fault Outcome
```

Program Runtime自有`CharacterPoseProgramFramePages`保存Pending node control、Source Demand只读输出、Pose/Value workspace、Operation completion和Program diagnostics。Source Module自有Source Binding、prepared/deferred resource与release journal页；Constraint Runtime自有Constraint Bank；Final Publication自有唯一Committed/Pending Final Pose物理页。根事务不得索引或暴露这些内部页。

成功Seal才要求各Module提升与根lineage匹配的Pending页；Discard不会改变Committed；跨Barrier失败进入现有Faulted政策。Module可以返回typed lease/result，但不能把页所有权转交根事务或Program Runtime。

业务收益：修改Program schema不会误碰其它Module状态，Reset不会修改静态Program，根Runtime不会重新变成共享黑板，Diagnostics不会读取被丢弃的Frame。

代价：构造Runtime时必须按Program Image容量一次性装配各类状态页，并为根事务保存固定数量typed lease；缺少容量直接失败。

## Decision 4: Program Runtime拥有逻辑节点，Source Module拥有物理采样

Pose Graph节点的逻辑Owner保持现有业务口径：

```text
PoseStateMachine -> State选择与Transition workspace
Player -> source endpoint、continuity与discontinuity
ActionPlaybackInput -> PendingFirstSample、Selected、Retained、Retired、command cursor与generation
AnimationSlot -> Source/Action插入与handoff
BlendStack -> live/Stored entry与blend clock
Inertialization -> residual、history与rebase
```

这些状态由`CharacterPoseProgramRuntime`的Actor State持有，因为它们是编译节点的执行语义。

`CharacterPoseSourceModule`只负责：

```text
接收typed Source Demand
调用Clip/BlendSpace/MotionMatching/Action sample Adapter
发布Action sample readiness与物理source completion，但不推进Action lifecycle
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
仲裁Action winner或推进ActionPlaybackInput lifecycle
提交Transition generation
计算跨source blend weight
拥有Slot handoff
执行Inertialization
决定OutputPose
调用Foot Placement或FBBIK
```

Clip、Blend Space、Motion Matching和有限Action是Source Module内部的真实Adapter；因此该Seam有多个实际Adapter。Final Writer当前只有一个Implementation，不因“以后可能替换”建立平行抽象。

## Decision 5: Pose Constraint是Program调用的深Module，不是Program布局的一部分

当前`CharacterPoseConstraintRuntime`已经拥有Foot、Goal、Assembler、FBBIK和BendHistory；这些已保留IK实现直接作为本次输入，不要求其它Foot／IK change先完成。本change只收紧其外部Interface，并保持Graph中的Constraint Operation仍由Program Runtime逐个调度：

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

Foot行为、Support、Pelvis、Goal编码、Bend策略、连续状态和正常Reset以`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`为基线。只移动根Bank的外部持有关系、typed入口和结果发布，不重排Foot内部流程、不引入新的脚请求／最终结果模型、不修复Solver Reset方向，不调整公式、阈值或配置。发现同输入差异先定位外层迁移，不能修改已保留IK来适配新架构。

## Decision 6: 每个Operation只有Program Runtime一个执行Owner

`CharacterPoseProgramRuntime`按Stage Schedule与Worker Batch Plan执行：

```text
FactAndDemand
SourceCapture
PurePose
WorldAwareValue
PureValue
FinalPublication preparation
```

每个Operation Header在一个Stage中出现一次，Program Runtime根据Operation Family与Execution Domain调用Worker Family Kernel、内部Managed Implementation或正式Module。Pure Pose只能由Program Image指定的Family Kernel执行；World Query、Foot Placement、Goal、FinalIK/FBBIK和Final Publication保持Compiler明确指定的Managed域。运行中不允许：

- 外层Runtime预执行World-aware Operation。
- Staged Executor重新解释已执行Operation。
- Diagnostics为了取值再次执行Operation。
- Constraint Module扫描Program寻找自己的节点。
- Source Module扫描Operation决定State或权重。
- Writer从作者拓扑推断Output。
- Pure Pose在Managed调用线程重算或因Burst不可用切回旧Executor。

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

Program Image的Output Family不拥有第二Final Pose buffer，只保存稳定`CharacterFinalPosePublicationLayoutHandle`。该handle只表达Program Image中的Output layout slot，不包含Actor页指针；Actor Runtime创建时由Final Publication把它绑定到当前Actor唯一Pending Final Pose页，Program Runtime执行Output Operation时通过actor-local binding写入并发布只读`ProgramOutputPoseResult`。Final Publication随后只接收该Result与同一lineage，不读取Graph节点、Goal来源、Foot状态或Constraint内部Result。写任何Physical Bone前验证全部binding、Pose availability、Rig、continuity和completion；合法时一次写完整Pending Pose，不合法时保持Committed Pose并遵守现有Barrier/Fault规则。

Compiler Topology只证明唯一OutputPose、唯一Final Publication requirement及其typed layout；Program Image Seal证明唯一Output handle且无第二Final Pose workspace。具体Final Publication实例、Physical Bone binding和Writer唯一性由Runtime Factory与Final Publication构造验证，Compiler不得引用具体Writer Implementation或制造Writer Graph节点。

Physical Writer成功后不得再运行会因业务数据失败的计算。Seal只发布已经验证的Program、Constraint、Source lifecycle和Final Publication结果。

## Decision 8: 唯一Node Definition Module

每种Node Kind由一个`CharacterPoseNodeDefinition` Adapter集中描述：

```text
NodeKind / CapabilityIdentity
PayloadType
FieldSchema / AuthoringCodec
FixedPortSchema
ConditionalPortVariants
DynamicPortPolicy
AllowedGraphRoles
ExecutionDomain
OperationFamily
GraphDependencyProjection
LocalPayloadValidation
RigValidation hook
TypedLowering
SourceMap naming
```

调用关系：

```text
CharacterPoseNodeDefinitionModule
    ├─ GraphDependencyProjection -> GraphClosurePass
    ├─ TypedLowering ------------> TypedLoweringPass
    └─ Capability projection
         -> GraphAuthoringCapabilityCatalog
         -> GraphAuthoringNodePortShapeProjector
            ├─ Canvas/Create Menu
            ├─ Document Exporter / strict parser / Target Mapper
            ├─ Clipboard
            ├─ Reconciler / Mutation preflight
            └─ Local Validator
```

Definition只负责单节点局部合同和直接Graph dependency投影。Graph Closure Pass唯一拥有可达closure、递归与call graph验证；以下全局执行规则由Topology Pass拥有：

- typed edge两端兼容。
- 唯一Output、Assembler、Goal Set、FBBIK和Final Publication requirement；具体Writer唯一性由Runtime Factory验证。
- 重复Goal Slot、跨分支写冲突。
- Stage顺序、World-aware依赖和生命周期闭包。

不继续使用`Player/BlendPolicy/StateMachine/AnimationSlot/Inertialization/...`二十多个布尔能力。调用方需要业务信息时读取结构化定义，例如Ports、Placement、OperationFamily或SourceBindingRequirement，而不是自行switch NodeKind。

Agent Document、Clipboard和Editor不再复制Payload字段表。Definition向现有Graph Authoring Capability投影节点局部字段、固定端口、条件`portVariants`、动态端口政策和role语义；唯一`GraphAuthoringNodePortShapeProjector`再把完整端口形状提供给Canvas、Document Exporter、strict parser、Target Mapper、Reconciler、Mutation preflight与Validator。上述调用方不得直接重新判断mode、构造默认Node或从现有edge反推端口。

Definition另向Compiler提供Graph dependency与typed lowering，但不得接管Document package路径、文件闭包、diff、Undo、rollback、save或reverse export事务。Definition变化如果改变Agent能看到、创建、连接或必须验证的语义，必须同步Document v4模型、Presentation codec/exporter、唯一Reconciler、typed Presentation Mutation、Validator及`btsmtl-agent-authoring`当前合同；五个MCP生命周期与Document Transaction Service继续保持不变，不建立Definition专用apply入口或第二catalog。

## Decision 9: Compiler使用固定不可逆Pass

唯一外部Interface：

```text
Compile(CharacterPoseCompilationRequest)
-> CharacterPoseCompilationResult
```

固定Pass：

```text
1. GraphClosurePass
   root flat catalog + State references + Node Definitions
   -> Definition GraphDependencyProjection
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

Graph Closure只允许直接读取Graph catalog、PoseState引用和Node Definition的`GraphDependencyProjection`。Subgraph与Linked Pose call target属于节点局部结构依赖，必须由匹配Definition解析；中央Compiler不得按NodeKind、Payload C#类型或显示名重新建立call switch。

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

Projection schema、Program Image schema、PoseProgramImageHash与ProjectionRevision直接提升。Gameplay-owned`CharacterPresentationSemanticContract.ContractHash`、SemanticHash、Float32/Fixed ProgramHash与Network identity保持不变。旧Projection reader、旧万能Operation codec、字段默认补齐和版本fallback删除；旧generated资产必须显式重建。

代价是Payload数组数量增加，换来节点变化只影响对应Family页，Runtime无法构造大量无关字段组合。

## Decision 11: 持久Executor隐藏Workspace布局

当前Staged Executor每帧构造并复制大量NativeArray字段。新`CharacterPoseProgramRuntime`在Actor Runtime创建时取得Program Image只读引用或建立自己唯一的actor-local Execution View，并建立持久Executor Implementation：

```text
ProgramImage只读引用或actor-local ExecutionView
ActorState页引用
ProgramFramePages引用
RootFrameLease只读引用
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
Final Output layout handle，由Actor-local Publication binding解析为唯一Pending Final Pose页
```

Program Runtime是这些页的唯一布局解释者。Constraint和Source Module只通过typed Handle/Result交换，不索引Program内部数组。根Frame Transaction只持有Program Frame lease，不取得上述页引用。

## Decision 12: 在线调参必须使用actor-local原子Snapshot

Program Image和actor-local Execution View只保存Build默认值，运行调参不得修改两者。每个Actor拥有一个`CharacterPoseTuningSnapshot`与单调递增`TuningGeneration`，Snapshot按真实Owner分区：

```text
Program Tuning
    node weight / PoseState / Slot / BlendStack / Routing / Inertialization
Source Tuning
    Clip / BlendSpace / MotionMatching / Action sample-local参数
Constraint Tuning
    Foot Placement / FBBIK参数
```

根Runtime在新Frame开始前接收Pending Tuning Block，依次请求三个Module构造不可变Candidate Snapshot并完成容量、identity与值域预验证。全部成功后根Runtime一次提升TuningGeneration并让三个Module切换到同generation；任一失败时三个Committed Snapshot保持不变。模块不得通过“先修改、失败后反向Apply旧值”的方式回滚，也不得把调参写入Program Image、Execution View或其它Actor。

`resetOwnerState`继续按冻结基线的作者语义作用于对应Module的Actor状态，但必须作为同一Candidate Tuning事务的一部分预声明。这里只改Tuning失败时的原子提交边界，不顺手修复IK初始化、BendHistory清空或Vendor方向政策；相同成功调参必须保持同一IK结果。调参生效时机、Preview入口与Runtime结果保持不变；如果未来决定删除在线调参，必须建立独立行为change，不能在本架构迁移中顺手删除。

## Decision 13: PoseGraph只从Committed Result发布具体Foot IK短租约View

Frame开始由PoseGraph冻结`CharacterFootIkCaptureInterest`与该View所需固定容量。存在interest时，各Module在自己的Pending结果完成后向各自预分配诊断页深冻结允许观察的数据；不存在interest时不生成Foot IK capture payload。这里的容量只属于PoseGraph Owner页，不是框架packet容量。

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

Runtime Projector与Generated Diagnostic Sampling Framework、Foot IK插件不是同一个职责。Projector只在成功Seal后按Runtime diagnostics interest读取上述Owner Committed Result，不为外部采样发布Capture View、Event、DTO或第二Snapshot。PoseGraph只保留既有Source、Program、Constraint与Final Publication事实根及其正式Post-Commit有效期，不知道Capability、字段Attribute、Sampler Definition、Schema Compiler、Generated Program、typed packet、CSV、geometry、manifest或Host产物。

`add-generated-diagnostic-sampling-framework`提供通用Attribute、Schema Compiler、Source Generator、Generated Program ABI、Generated Lifecycle、typed packet、Capability Session、Writer、Schema-driven Host Reader／Finalizer和`DiagnosticCapabilitySet`身份合同；`refactor-foot-ik-diagnostic-sampling`只保留`character-foot-ik`字段／Sampler／Program Definition、薄`CharacterFootIkGeneratedCapture`与Editor workflow。外部Generated Capture在既有Post-Commit边界直接读取同lineage事实根并交给Generated Lifecycle，Schema-driven Host产出的主表、子表、CSV与manifest就是最终诊断输出。旧Sampler二次join、Bridge、Host Adapter、手写Column／CsvBinding、Analyzer、Publisher、评分与Store链已经直接删除；PoseGraph不得重新建立Event、Capture View、临时DTO、兼容Adapter或第二报告链。

目标／实际Foot、Pelvis、Goal、Solved和Physical字段仍保持现有业务含义与事实来源；其对账由生成Program与Host产物完成，不再经过旧评分或报告层解释。确实需要改变外部采样字段含义时由对应诊断change显式升级schema，本change只保证PoseGraph事实根的lineage、提交时机与运行行为不变；历史原包保持。

## Decision 14: Preview与Runtime只使用Adapter差异

正式Gameplay与Scene Play Preview通过正式Character Session使用相同：

```text
Program Image
actor-local Execution View规则
Program Runtime
Source Module
Constraint Module
Final Publication
根Frame Transaction
actor-local Tuning Snapshot
Completion语义
```

真实差异只存在于场景实例和已选正式输入：

```text
Preview Scene实例
正式输入序列
观察目标
```

Pose作者窗口只发起创建、播放、暂停、单步、时间定位和观察命令，不拥有Scene、Session、播放时钟、Program Runtime或Gameplay状态。非连续时间定位由Scene Play销毁当前预览实例，从正式场景初始状态按已选输入重新推进Session到目标时间；不得直接写PoseState、Action、Player、Inertialization、Foot、Goal或FBBIK状态。缺少精确World Context时同一Program Runtime返回typed Unavailable；不得使用简化Foot、临时Program、第二Executor或默认地面。Stale Projection停止Scene Play并要求显式Build。

## Decision 15: Barrier与失败政策保持现行合同

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

Module不能拥有不同Frame identity或独立决定提前Seal。它们可以维护内部双页和journal，但只响应根Frame Transaction的唯一Seal/Discard结果。在线调参Candidate必须在根Frame开始前完成原子提升，不能混入已经打开的Frame或在Barrier后变更TuningGeneration。

## Decision 16: 物理目录与程序集Locality

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

## Behavior Preservation

[行为保护清单](behavior-baseline.md)是本次迁移的输入合同，不是另外一条运行链。拆Module、改Family布局、复用Workspace和集中Diagnostics都不得改变source推进次数、Transition／Slot时机、混合数值顺序、Foot dominant contribution选择、IK内部调用顺序或Writer根骨策略。State字段按实际消费者分类，名称中包含Fact／Diagnostics不意味着可删除；凡被下一帧读取的值都按基线保留。

该保护只覆盖继续保留的Corin正式内容。`TrainingEnemy`已由用户明确退役，不要求迁移或维持其Actor、AI、动画、Pose、Profile、Prefab和生成数据行为；清理边界必须保证Corin在无目标时仍按现行`OptionalSnapshot`攻击语义运行，不能顺带删除通用target input或MotionWarp能力。

每个代码小步须同时比较指定基线和上一保留小步的已有正式输入／输出；只复用现有Replay、Proof、Analyzer、Publisher以及`refactor-foot-ik-diagnostic-sampling`迁移后的内建Full Adapter，不新增测试工程或临时验证链。先对账输入、Body、时钟和IK输入，再对账中间量和最终骨骼，不能只比较最终总分。纯生成身份差异要按稳定Node／call-site／Source／Event证明一一映射，不能用重标身份掩盖额外Reset或额外查询。可见业务数值差异、成功Reset差异或覆盖缺口必须报告，未解释前不继续堆叠迁移或调参。

已有真实失败／初始化行为也不能因“清理旧路径”被默改：Physical Writer的根骨排除与Committed／Reference选择、无有效Goal时跳过Vendor Update、错误后停止与提交边界需要分别保留。若现有代码与目标的no-throw Seal／诊断发布顺序存在无法证明等价的冲突，必须先报告，不能把错误处理重设计伪装为纯迁移。

## Decision 17: 验证只由对应边界负责

| 边界 | 唯一负责的检查 | 下游不再重复 |
|---|---|---|
| Character Build | typed拓扑、静态写冲突、Operation Family、Value／Workspace布局与schema | Runtime不重新扫描Graph或证明同一静态布局 |
| Runtime创建／替换 | 已发布产物身份、实际Rig／资源绑定、固定容量 | 普通帧不重复整份资产和Profile检查 |
| 根Frame／跨Owner交接 | 当前lease、source readiness、generation、动态容量使用与completion | 成功的typed交接结果向下传递，不逐层重新解析全部identity和payload |
| Final Publication | 当前Output完成状态、最终Pose有效性与Physical binding仍可写 | 不重算上游Foot／Goal／FBBIK，也不重做Build拓扑证明 |

不删除当前算法必要的动态检查，也不改变原Fault边界；这里只防止迁移时把同一检查复制到根Runtime、Module、Executor和每个Result中。业务失败在拥有对应输入的边界报告，下游传播typed失败，不再检查一遍后生成第二原因。

## Decision 18: Worker执行必须在本change首次Compiler／ABI切换时一起落地

Worker Batch、Family Kernel和跨Actor调度不是PoseGraph重构完成后的第二阶段。当前Node Definition已经开始收口，而固定Compiler Pass与分段ABI尚未落地；线程安全事实、批次布局、Kernel identity和Completion依赖必须在这次Program Image schema第一次封存时成为正式合同。若先发布纯串行新ABI，再用独立change加入Worker，Program Image、Compiler Pass、Execution View、Runtime调度和诊断lineage都要二次破坏性迁移，并在中间形成新ABI加旧Executor的分裂路径。

固定Compiler顺序改为：

```text
Graph Closure
-> Typed Lowering
-> Topology
-> Symbolic Family Lowering
-> Stage Schedule
-> Value Lifetime
-> Workspace Plan
-> Worker Batch Plan
-> Bind Family Payload
-> Seal Program Image
```

`Worker Batch Plan`只消费已经固定的Stage、Value寿命、Workspace布局与Node Definition线程安全事实，生成：

```text
Batch identity
Stage dependency wave
Family Kernel identity
Actor Batch Key
Rig execution layout
Typed read/write ranges
Input and output Completion
Workspace range
Required Kernel Set and Execution Policy
```

它不得发现新Operation、扩容Workspace或把不安全Operation留给Runtime现场判断。Node Definition中的Execution Domain与线程安全能力由代码固定，是Compiler输入，不是作者参数；Canvas、Inspector、Document和MCP不出现Burst、线程、Job、Batch size或调度策略开关。

## Decision 19: 只并行Pure Pose有限Family，不把Playable／FinalIK伪装成线程安全

执行域固定为：

| 工作 | 执行域 | 原因 |
|---|---|---|
| Control、Demand、Animancer／Playable source准备与Evaluate | Managed SourceCapture | 需要PlayableGraph与Unity对象所有权 |
| Local／Component Pose混合、空间转换、纯骨骼与纯Value数学 | Pure Pose Worker | 只消费固定Native布局，可由Burst/HPC# AOT Kernel执行 |
| Physics／World Query、Foot Placement事实读取 | Managed World-aware | 依赖场景与查询后端 |
| Goal Assembly、FinalIK/FBBIK Vendor Solver | Managed Constraint | 依赖Vendor对象与现有solver状态 |
| Final Pose验证与Transform写入 | Managed Final Publication | 独占Physical binding与提交边界 |

Worker实现按有限Operation Family提供AOT可知Kernel，不为每个Node、每个Bone或每个Graph实例生成Job。一个Kernel处理同Family、同Rig执行布局、同依赖波次的一组Actor Work Item；这样Job粒度足够大，Program Runtime也不需要暴露每个骨骼的调度细节。Kernel不得捕获托管对象、抛出异常或动态分配；失败只写入固定Outcome与Completion页，由Actor根事务按原Fault政策处理。

## Decision 20: 唯一会话级Scheduler跨Actor合批，Actor根仍拥有自己的事务

仅在单个Actor内部把Pure Pose放进Job，然后立刻`Complete`，通常只增加调度成本，无法利用2v2vE场景中的多个角色。正式结构增加唯一会话级`CharacterPoseWorkerScheduler`：

```text
Session Presentation Phase
-> collect prepared Actor work
-> run each Actor Animancer SourceCapture at its legal point
-> group ready Pure Pose work by Program / Rig layout / wave / Family Kernel
-> schedule fixed cross-Actor batches
-> complete exact dependencies before each Actor Managed World/Constraint stage
-> schedule later Pure Pose wave when its managed dependency becomes ready
-> Final Publication and actor-local Seal
```

Scheduler只理解typed Batch Lease、Program／Rig／Kernel identity、依赖与Completion，不理解Node、Bone语义、Foot数学或Workspace内部字段。每个Actor的`CharacterAnimationPresentationRuntime`仍拥有唯一根Frame Transaction、Actor State、Pending页与Fault结果；同一批次中Actor A失败不得污染Actor B。Worker资源紧张不得改变Actor更新频率、Delta、source时间或Final Publication语义；本change只改变物理执行位置和跨Actor重叠，不增加动画预算。

Reset、Projection Replacement与Dispose必须先经唯一Scheduler fence完成该Actor全部Outstanding Job，再释放Execution View、Frame页与Module状态。Diagnostics、Pose Watch和Live Debug只能在匹配Batch／Completion全部完成且根事务成功Seal后读取Committed Result，不得为了补字段等待、重放或重新调度Kernel。

Program Image封存`Kernel Set`、`Execution Policy`、Rig执行布局与平台能力identity。Preview与Player消费相同Program语义和Kernel Set；缺少Kernel、Burst/AOT闭包或身份不匹配时，Runtime创建直接失败并要求正式Character Build，不保留串行Executor、Managed重算或按平台隐式fallback。

## Decision 21: Pose作者资产使用CanvasCore，保持唯一业务数据

Pose作者资产结构固定为：

```text
CharacterPresentationPoseGraphAsset
├─ RootGraph -> CharacterPoseCanvasGraph
└─ GraphCatalog[] -> CharacterPoseCanvasGraph

CharacterPoseCanvasGraph : CanvasCore Graph
├─ CharacterPoseCanvasNode[]
│  ├─ PoseNodeId
│  ├─ CharacterPoseNodeKind
│  └─ CharacterPoseNodePayload
└─ CharacterPoseCanvasConnection[]
   ├─ PoseEdgeId
   ├─ SourceNodeId / SourcePortId
   └─ TargetNodeId / TargetPortId
```

`CharacterPresentationPoseGraphAsset`继续作为Profile引用和Graph catalog所有者，但不再保存`CharacterTypedPoseGraph`副本。Canvas Graph／Node／Connection是唯一可编辑拓扑；PoseStateMachine定义、状态元素和布局继续作为匹配节点的typed作者数据，由同一Pose Canvas显示，不建立独立可写状态机资产。

Pose使用CanvasCore的Graph、Node、Connection和编辑器交互基础（编辑表面按Decision 25采用CanvasCore `GraphEditor`），不以FlowCanvas的`FlowScript`、`FlowNode`、`BinderConnection`或自动TypeConverter作为正式Pose模型。原因是Pose需要Local／Component、Goal Contribution、Goal Set等严格业务类型，而FlowCanvas允许面向通用C#值和调用的连接与转换。代价是项目需要实现Pose专用Port与Connection外观；收益是Canvas不会携带第二运行语义，也不会放宽现有拓扑合同。

## Decision 22: Node Definition投影Canvas，Mutation拥有全部写入

新增`CharacterPoseCanvasDefinitionProjection`，输入是唯一`CharacterPoseNodeDefinition`及节点typed payload，输出是Canvas节点标题、字段、固定／条件／动态端口、Pose空间、颜色、创建项和只读线程能力。该Projection只负责Editor显示，不保存第二字段表或端口表；绑定Graph、节点Mutation或Definition revision变化时重建对应View，不在`OnInspectorGUI`或普通Repaint中扫描、编译整图。

所有作者操作固定通过：

```text
Canvas gesture
-> CharacterPoseCanvasMutationAdapter（编辑器表面为CanvasCore GraphEditor时经由Graph原生API入口路由，见Decision 25）
-> CharacterPresentationMutation
-> Document Transaction / Undo（编辑器会话内CanvasCore Undo唯一，见Decision 25）
-> mutate the sole CharacterPoseCanvasGraph
-> refresh affected Canvas projection
```

Pose Graph子类必须关闭CanvasCore直接添加、删除、连接、复制或字段写入的默认命令入口，并把右键菜单、拖线、Delete、Paste、Details与StateMachine编辑转交上述Adapter。Mutation preflight使用同一Capability与`GraphAuthoringNodePortShapeProjector`判断端口和字段合法性；Canvas不得先改Graph再回滚，也不得让Graph自身Undo与Document Transaction各记录一次。

## Decision 23: Compiler通过只读Authoring View消费Canvas，不执行Canvas

新增`CharacterPoseCanvasAuthoringView`作为Compiler唯一Canvas边界。它只枚举稳定Graph、Node、Payload、Port、Edge、Graph Role、布局来源和资源引用；不会提供Flow调用、Value getter、Event、Update、GraphOwner或运行状态。`CharacterPoseCompilationRequest`从该View取得root catalog，Graph Closure和Typed Lowering继续只通过Node Definition解析dependency与payload，不建立`CharacterTypedPoseGraph`中间作者副本。

```text
CharacterPoseCanvasGraph
-> CharacterPoseCanvasAuthoringView
-> Graph Closure
-> Typed Lowering
-> 现有Topology／Schedule／Workspace／Worker／Binding Pass
-> CharacterPoseProgramImage
```

Canvas中没有唯一Pose Node Definition的节点在进入IR前直接失败；Compiler不得执行或包装通用Flow、Event、Method或反射节点。Program Image schema、Operation Family、Stage、Worker Batch、Runtime、Source、Constraint、FBBIK和Final Publication不因作者资产迁移增加第二版本。作者模型变化只提升作者revision及由正式Build产生的新ProjectionRevision／PoseProgramImageHash。

## Decision 24: TrainingEnemy整套退役，不进入Canvas迁移

`TrainingEnemy`不是一个可单独删除的PoseGraph文件。它同时占有GameplayLab第二Actor、玩家显式目标、AI authoring、Character Pipeline、Action／Timeline、动画与Root Motion、Foot Placement、Rig、Presentation Profile、运行／表现Prefab、AssetBundle collector、默认目录和专用构建工具。正式Gameplay、AI与动画资产都未闭合时继续迁移其中任一层，会迫使后续每次架构变化维护一套不能作为验收对象的内容。

本change因此把它视为一个完整内容岛，一次删除：

- 删除`Assets/Configs/Character/TrainingEnemy`及目录内Gameplay、AI、动画、PoseGraph、Rig、Foot、Profile和generated资产。
- 删除`TrainingEnemyMonster.prefab`、`TrainingEnemyMonsterPresentation.prefab`及GameplayLab中的嵌套实例。
- 从Session roster删除`corin-training-enemy`，移除玩家绑定该Actor的target provider配置；保留通用target input和MotionWarp能力，当前玩家按既有`OptionalSnapshot`无目标语义继续执行攻击。
- 删除TrainingEnemy专用作者／构建脚本，并从GameplayLab builder、launcher、startup validator、root hierarchy builder、AssetBundle collector和默认目录中删除专用分支、路径和GUID。
- 不生成新的Canvas资产，不保留空Profile、Prefab壳、旧GUID占位或迁移清单例外。未来训练敌人或中立怪物从届时正式的Gameplay、AI、Character Pipeline、Presentation与Canvas链重新建立。

删除顺序固定为先清除外部引用和场景组合，再删除资产与专用工具，最后按名称、路径、ActorId和GUID做引用闭包检查。这样不会留下Missing Script、Missing Asset或仍被collector打包的孤儿内容。

## Decision 25: Pose编辑表面采用CanvasCore GraphEditor，Mutation路由保唯一写入

接入补充（2026-09-08）：根图与Pose子图直接使用正式Canvas作者对象。StateMachine和TransitionRule仍保存原typed作者数据；同一GraphEditor中的`CharacterPoseDocumentCanvas`只持有稳定identity与显示投影，不保存、不进入Document或Compiler、不独立运行，修改仍提交原Document Mutation。这是编辑视图，不是第二套可写状态机资产。窗口导航、详情和既有观察界面由`CharacterPoseGraphWorkspace`组合，不再创建第二个Pose EditorWindow。

图操作先对完整候选preflight，再在实际Canvas owner上记录Undo及序列化；仍存在的节点对象保持身份，减少拖动和选择失效。状态机内容的Undo必须包含其实际Canvas子资产，布局仍归原布局owner；撤销后按Graph／Node稳定identity重新读取定义，不复用撤销前对象。重绘、选择和全图吸附不得偷偷写入作者布局，运行高亮只保留在视图状态中。当前实现与尚未通过的验收见`canvas-integration.md`。

（2026-09-08新增，取代execution记录21.7"保留自建GraphView"的旧取舍。）Pose作者编辑表面从877行自建`CharacterPoseCanvasView`切换为CanvasCore `GraphEditor`（`OpenWindow(graph)`入口），自建View随22.x完成后退役删除。CanvasCore以vendored源码形式取得第一方地位：允许在其源码内加最小钩子，全部改动点以`// 3C`标记，便于将来与上游版本手工对账。

写入路径：`CharacterPoseCanvasGraph`的8个写方法（AddNode/AddNode<T>/RemoveNode/ConnectNodes/RemoveConnection等）从抛异常改为把CanvasCore编辑器的原语调用翻译成typed Mutation——端口索引反查端口ID、粘贴时重建NodeId、经`CharacterPoseCanvasMutationPreflight`校验后应用。Mutation合同（17.2/17.3）不变，硬锁变正门。节点位置与名称这类CanvasCore直接写字段的操作，在CanvasCore侧加变更事件钩子路由进Document记录，保持Layout进Undo与迁移对账。

Undo owner定界：编辑器会话内CanvasCore Undo唯一（Mutation应用后不双记Document），外部入口（MCP、Inspector、Clipboard、正式写入命令）Document Transaction唯一。创建菜单只放行`CharacterPoseNodeDefinition`注册的类型（16.3禁令不变），菜单文案、分组与颜色继续由Definition投影提供；命名端口视觉与Pose Watch、StateMachine子图导航、Preview Dock挂进GraphEditor面板体系属于后续增量，不阻塞画布编辑可用性。

代价：CanvasCore升级需人工diff对账（改动点已标记）；NodeCanvas默认端口视觉在命名端口适配完成前降级显示（数据与编译不受影响）。收益：编辑器交互、连线渲染、Undo、复制粘贴、小地图等由成熟框架承载，项目不再自养GraphView。


## Migration

迁移在同一change中顺序完成，但不保留并行运行路径：

1. 对照指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`及behavior-baseline.md核对当前动画／IK源码、配置、节点行为、产物与已有诊断证据；后续差异单独报告，先接入第一阶段已通过的IK维护成果，不等待其它Foot待办或全部归档，不并入其它未实施行为或已撤销实验。
2. 建立根Frame lineage、typed Result、Module lease与Owner规则，让现有单一路径先携带新身份；根事务只保存阶段和typed lease/result，不提前创建共享页、空壳Module或第二Frame事务。
3. 保留当前Foot、Pelvis、Goal与FBBIK内部实现，只迁移`CharacterPoseConstraintRuntime`及根Bank外部归属，并按各Constraint Operation收窄typed Handle/Result；删除布局泄露调用，不重排IK算法。
4. 提取`CharacterPoseSourceModule`，原子迁移provider、有限Action sample、Animancer、Physical Source和release所有权；从旧Runtime删除对应字段与方法。
5. 建立Projection内部唯一`CharacterPoseProgramImage`与`PoseProgramImageHash`，让每个Program Runtime最多建立一份identity精确匹配的actor-local只读Execution View；把旧Native Program可变状态分别迁入Actor State、Program Frame Pages和对应Module Owner。
6. 建立持久`CharacterPoseProgramRuntime`和Executor，迁移PoseState、Player、ActionPlaybackInput lifecycle、Slot、Blend、Inertialization与Operation调度；删除外层World-aware Operation扫描和旧Staged Executor双Owner。
7. 建立`CharacterFinalPosePublication`，迁移唯一Committed/Pending Final Pose物理页、完整验证和Writer；Output Family只保留稳定layout handle并在Actor创建时绑定Publication页，旧Runtime不再直接操作Physical Bones。
8. 建立actor-local Tuning Snapshot与原子Tuning Generation，迁移Program、Source、Constraint调参并删除直接修改Program Image/Execution View及失败后反向Apply旧值的路径。
9. 在四个Module都接通后收窄`CharacterAnimationPresentationRuntime`，让它唯一拥有根Frame Transaction并删除其节点、offset、Constraint、source和Writer知识，只保留Tuning协调、Frame阶段与统一Seal/Discard/Fault。
10. 建立Node Definition Module，一次性迁移Capability、Port Shape Projector、Authoring、Document v4 Exporter/strict parser/Target Mapper/Reconciler、Clipboard、Mutation preflight、local validation、Graph dependency和typed lowering；同时为每个Operation Family固定Execution Domain与只读线程安全能力，不增加作者可编辑线程字段；保留唯一Document Transaction Service，删除旧Handler Registry与重复switch。
11. 将Compiler拆成`Graph Closure -> Typed Lowering -> Topology -> Symbolic Family Lowering -> Stage Schedule -> Value Lifetime -> Workspace Plan -> Worker Batch Plan -> Bind Family Payload -> Seal Program Image`固定Pass，生成Worker Batch、Rig执行布局、Kernel Set、Execution Policy与Completion依赖，删除中央`CompilationState`和pass-through入口。
12. 完成全部现行Operation Code到Family/Owner/Domain/Kernel映射后，在同一次schema切换中原子接入分段Operation ABI、Worker Family Kernel、唯一会话级Scheduler、Projection内Program Image和Runtime reader；提升PoseProgramImageHash但保持Gameplay ContractHash不变，删除万能Operation、旧Native Program语义容器、旧串行Staged Executor、Managed重算、旧reader与Burst fallback。
13. 将Runtime Diagnostics改为只读全部Worker／Managed Completion完成后的Committed Result Projector；外部薄Generated Capture在既有Post-Commit边界直接消费正式事实根并交给通用AOT Program／Generated Lifecycle／packet／Schema-driven Host，Host产物就是最终诊断输出。删除Capture View、Event、Bridge、Foot Host Adapter、跨Owner内部读取、旧Sampler二次join、Analyzer、Publisher、评分、Store与第二报告链。
14. 迁移Preview到同一Factory、Program Image、Worker Batch Plan、Kernel Set、actor-local Execution View规则、Tuning Snapshot和根Frame Transaction，删除简化、串行或重复执行路径；完成Reset、Replacement与Dispose的Outstanding Job fence。
15. 搜索并删除旧类、旧字段、旧codec、旧validator知识、兼容版本和未引用路径，使用既有Replay对账行为并执行正式IL2CPP Performance Capture，更新project truth并完成编译与严格校验。
16. 从GameplayLab composition、Session roster、玩家target provider、AssetBundle collector、默认目录和构建入口清除TrainingEnemy引用，随后删除其完整配置目录、运行／表现Prefab、专用作者／构建脚本和生成产物；不得把其中PoseGraph转换为Canvas资产。
17. 在独立Pose作者程序集建立CanvasCore依赖和`CharacterPoseCanvasGraph/Node/Connection`，固定Canvas只承担作者数据与交互，禁止FlowCanvas运行入口进入Character装配。
18. 实现Definition Projection与Canvas Mutation Adapter，把创建、连线、删除、复制粘贴、Undo、Details和StateMachine表面切到唯一typed Mutation链。
19. 实现只读Canvas Authoring View并在同一次切换中让Graph Closure与Typed Lowering直接读取新资产；只迁移明确保留的Corin Pose Graph、状态机、节点、端口、边、布局、子图、identity和资源引用，然后通过正式Character Build重建Projection。
20. 新资产与新Compiler输入闭合后删除`CharacterTypedPoseGraph`存储、旧Pose `GraphAuthoringCanvasView`、旧StateMachine画布、旧codec和所有镜像／双写／反向同步路径。
21. 将Pose窗口的Preview命令与观察选择接入`rebuild-btsmtl-preview-with-scene-play`提供的正式入口，删除独立`AnimationPreviewRuntime`、第二播放时钟和直接状态Seek。

每一步替代完成后立即删除旧Owner，不允许为了保持可运行而让新旧Owner同时执行、双写或通过开关切换。代码可以在同一个未提交小步中暂时不可编译，但每次小步提交前必须恢复可编译闭环并完成对应静态检查。

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

分段ABI要求提升Projection schema、PoseProgramImageHash并重建全部generated Projection。保留旧reader可减少一次内容重建，但会永久保留两套Operation解释规则，因此本change选择显式重建并删除旧schema。Gameplay ContractHash和Float32/Fixed ProgramHash不变，避免纯表现迁移扩散到Gameplay与Network identity。

### 9. Program Image与Native执行存储

完全禁止Native执行View可以让“只有一个Program对象”更直观，但会迫使所有Runtime常量改为managed数组并重写现有NativeSlice链。按Projection共享View可以减少静态常量内存，却需要新增跨Actor缓存、引用计数和Editor/Preview释放根。项目固定为小规模2v2vE Gameplay demo，本change选择唯一序列化Program Image加每个Program Runtime最多一份identity精确匹配、actor-local、只读的Execution View；代价是少量静态常量按Actor复制，收益是生命周期直接归Program Runtime，不引入全局缓存或隐式共享路径。

### 10. 在线调参继续保留

删除运行时调参可以简化不可变Program，但会降低Pose Graph作者迭代效率，而且属于行为变化。继续直接修改Native Operation会破坏共享Image与跨Actor隔离。本change保留调参业务，通过actor-local不可变Snapshot和一次Tuning Generation提升完成迁移；代价是Program、Source与Constraint都要实现Candidate预验证。

### 11. 冻结当前IK还是等待全部IK工作完成

等待全部IK工作完成可以得到更晚的统一行为基线，但会把剩余效果改进与PoseGraph结构迁移绑定。冻结当前保留实现允许立即开展结构迁移，代价是保留已知效果问题，后续IK工作需适配新外部边界。用户已选择后者，并指定`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`；本change不再要求Foot／IK全部归档，也不借迁移继续做效果实验。

### 12. Worker现在并入还是重构完成后另做

重构完成后另做可以让本change表面更小，但新Compiler、Program Image、ABI、Execution View和Runtime调度会先按串行假设封存一次，随后全部二次迁移，并产生“新Program加旧Executor”的中间分裂路径。现在并入会扩大第10～12阶段的单次改动范围，但线程安全、read/write set、Kernel identity和跨ActorCompletion从第一版就是完整合同。这里选择现在并入；代价通过固定Pass、小步提交、既有Replay与正式Performance Capture控制，不通过兼容开关降低风险。

### 13. 每Actor独立Job还是会话级跨Actor批次

每Actor独立Job最容易接到现有Runtime，但小规模骨骼工作很可能在`Schedule -> Complete`之间没有足够并行窗口，调度成本反而更明显。会话级Scheduler需要显式Actor Batch Key、跨Actor隔离与生命周期fence，但能够把相同Program／Rig／Family工作组合为足够大的批次。项目有2v2vE多个角色压力场景，因此选择会话级批次；Actor根事务、Fault和Final Publication仍完全独立。

### 14. CanvasCore作者基础与FlowCanvas运行语言

直接使用FlowCanvas可以立即得到泛型Value端口、自动转换和委托执行，但这些能力同时引入Pose不允许的隐式类型转换、Flow生命周期和第二执行入口。本change选择CanvasCore作为交互与序列化基础，由Pose Definition投影严格端口并继续编译到现有Program Image。

代价是Pose需要自己的Port／Connection表现和Mutation接线；收益是Local／Component与Goal类型保持严格，运行时仍只有一套Pose Program。

### 15. Pose单领域迁移与全领域Canvas迁移

一次迁移BTSMTL、AI和Pose可以统一具体画布，但会同时改动正在实施的控制作者架构、Document和大量内容资产。用户已经把本change限定为Pose Graph，因此这里只迁移Pose的唯一作者资产和具体Canvas，并让跨领域共享停留在Capability、Document、Mutation、事务和Diagnostics合同。

代价是不同领域继续有不同具体Canvas实现；收益是Pose作者能力可以独立闭合，不触碰BTSMTL与AI资产或运行语义。

### 16. 破坏性作者资产迁移

保留旧`CharacterTypedPoseGraph`并让Canvas投影或双写，可以降低一次迁移压力，但会产生两个可编辑拓扑和长期对账问题。本change选择显式一次性迁移：在新资产、Document往返和Compiler输出闭合后立即删除旧存储与入口。

代价是保留的Pose内容必须在同一迁移步骤完整转换；收益是后续作者、Agent和Compiler始终只有一个数据来源。

### 17. 保留TrainingEnemy半成品还是整套删除

保留但暂不引用可以留下动画和怪物内容样本，却仍要求每次Pose、AI、Character Build、Profile schema和资源收集变化维护这些资产，并持续制造“目录存在但不能正式构建”的误导。整套删除会暂时失去Standalone第二Actor和有目标MotionWarp演示，玩家攻击只能走既有无目标语义；收益是Canvas迁移只处理正式Corin资产，Character Build和内容清单不再背负不可验收数据。

用户选择整套删除。未来需要训练敌人时重新建立正式内容闭环，不从旧Profile、旧生成产物或旧Prefab恢复。

## Rejected Alternatives

- **只拆分大文件**：不改变Owner和Interface，调用方仍理解全部内部字段。
- **给现有Runtime再包一层Facade**：删除Facade后复杂度直接消失，是浅Module。
- **保留旧Staged Executor并新增World-aware Executor**：会继续产生同一Operation两个Owner。
- **让Diagnostics直接读取Workspace以减少复制**：会让诊断与运行状态生命周期绑定，并可能观察Pending或被丢弃结果。
- **把Program Image与Actor State放在同一对象但标注readonly字段**：对象仍同时拥有不同寿命与Dispose/Reset语义，无法证明隔离。
- **保留旧Operation并增加typed payload可选字段**：形成双ABI和更多非法组合。
- **为FinalIK、Writer、World Query统一建立插件接口**：当前实际Adapter数量和变化方向不同，会制造假Seam；只保留已有真实Adapter位置。
- **先完成纯串行新ABI，再另开Worker change**：会让Compiler、Program Image、Execution View和Runtime调度发生第二次破坏性迁移，并在中间保留新旧执行假设。
- **每个Node或Bone一个Job**：Job数量随Graph与Rig膨胀，调度开销和依赖图成为Runtime第二编译器；固定Family Kernel按跨ActorWork Item处理。
- **缺少Burst时回退旧Managed Executor**：形成两套算法与两套产物身份，Preview和Player结果无法保证一致；正式创建直接失败并要求重建。
- **把Canvas作为`CharacterTypedPoseGraph`的可编辑镜像**：会产生两份Pose拓扑、双写和冲突恢复政策。
- **直接以FlowCanvas运行图替换Pose Runtime**：会绕过Program Image、Stage、Worker、Frame事务、Constraint和Final Publication所有权。
- **只复用FlowCanvas泛型端口并允许自动类型转换**：无法保持Local／Component、Goal Contribution与Goal Set的严格连接合同。
- **让Canvas直接修改Graph，再由Mutation补记Document**：Undo、保存和Agent事务会产生两个写入Owner。
- **继续在Pose窗口维护独立Preview Runtime**：会与正式Scene Play形成第二Session、第二时钟和第二状态定位语义。
- **只从GameplayLab移除TrainingEnemy实例并保留资产目录**：仍会让Build、collector、默认路径和后续schema迁移维护一套不能验收的内容。

## Spec Conflicts And Resolution

- 本change旧前置要求Foot完成并归档，用户2026-09-01已明确改为冻结当前保留IK后开始PoseGraph重构。Foot／IK未完成状态不阻塞，重叠的后续行为修改不能与本次同Owner迁移混写；出现实际冲突才由用户裁决。
- current Foot spec已不固定中央状态机类名，旧PoseGraph delta仍写`CharacterFootStateMachine`，本次删除该过期条款，保留现有Lifecycle内部实现。
- 当前保留实现已撤除业务层骨盆Reach硬夹紧与末端夹脚，SmoothKnee候选已撤销；部分旧current／active文字尚未同步。本change明确按用户当前保留行为迁移，不借旧文档恢复政策，并保留第一阶段已经验证的请求／结果边界及独立Reset修正，本阶段不另改其业务行为。
- 已落地诊断存储与评分change继续作为业务语义输入，是否归档不影响本次接线；Runtime事实来源由PoseGraph Projector迁移，通用生成／packet／Host生命周期由`add-generated-diagnostic-sampling-framework`承接，Foot字段／Sampler／Analyzer／Publisher由`refactor-foot-ik-diagnostic-sampling`承接，明细存储、评分规则含义与历史证据不重做。
- active Blend Space change的通用Clip/Blend Space能力已经进入current specs，剩余任务只建立独立演示内容。本change直接迁移current节点Definition与Source Adapter，不依赖该演示归档；两者不得并行修改通用Runtime或Compiler合同。
- current `character-presentation-pose-graph`仍写明`compiler handler`，而current `graph-authoring-domain-framework`已经要求唯一Pose Node Definition并禁止第二Handler；现行代码仍保留Handler Registry。本change以Framework current requirement为方向修改Pose Graph requirement并删除旧实现，明确这是current specs之间的冲突，不把代码现状误写成current truth。
- current `graph-authoring-domain-framework`已经固定Definition、共享Capability、Reconciler与Document Transaction Service边界。本change补足唯一`GraphAuthoringNodePortShapeProjector`、Document v4 Exporter/strict parser/Target Mapper/Mutation preflight调用链，以及Graph Closure读取Definition dependency的边界；Definition不得接管Document事务或五个MCP生命周期。
- current `graph-authoring-domain-framework`要求BTSMTL、AI和Pose复用同一具体Canvas。本次用户明确只迁移Pose，因此delta把统一边界收窄为Capability、Document、Mutation、事务、Clipboard、Selection和Diagnostics合同，并要求每个领域内部只有一个具体Canvas和一套作者资产。不得保留旧Pose Canvas作为兼容入口。
- current `character-presentation-pose-graph`以`CharacterTypedPoseGraph`和项目自建Canvas表达唯一作者图。本change以CanvasCore Graph／Node／Connection直接替换该存储，保留稳定identity、typed payload和Node Definition语义，并要求迁移完成后删除旧数据模型。
- current Pose Preview仍允许独立Adapter持有Preview Runtime和非连续Seek。已确认的Scene Play方向要求正式Session拥有Scene、时钟、输入推进和重建；本change只保留Pose窗口的命令与Committed观察职责，实际预览生命周期由`rebuild-btsmtl-preview-with-scene-play`负责。
- current `character-targeted-motion-warp-demo`明确要求Standalone注册玩家与`corin-training-enemy`两个Actor，并把玩家target provider绑定训练敌人。用户2026-09-05已改为整套删除TrainingEnemy；删除后GameplayLab只保留Corin，target input为None，五段攻击按`OptionalSnapshot`继续运行原始MotionCurve。该capability需要删除训练敌人、显式绑定和训练敌人范围三组现行Requirement，但当前change没有该capability的delta文件；实施前必须先补齐该delta，不能声称current spec已经同步。
- current `btsmtl-compiled-simulation-program`与`project.md`并存`CharacterPresentationPoseProgram`、Native Pose Program称呼。本change统一为Projection内部唯一`CharacterPoseProgramImage`，每个Program Runtime只可建立一份同identity的actor-local只读Execution View，不生成第二语义程序。
- current Pose Constraint合同已经把Foot/Goal/FBBIK结果收敛进根Bank。本change让Constraint Committed Result只拥有Foot/Goal/FBBIK事实，Physical结果只属于Final Publication Committed Result，由Diagnostics Projector按同lineage组合。
- current动画事务允许各Module拥有内部Pending页，但没有明确Program Image、Actor State、根Frame Transaction和Module Owned Frame Pages。新增runtime architecture spec把静态、Actor、Program Frame、Module Frame与根事务分型，同时保留现有Barrier和Dense/Journal政策。
- current实现允许actor-local在线调参并在失败时反向Apply旧值，但current spec没有给共享不可变Program下的调参值安排Owner。本change把它固定为actor-local Tuning Snapshot与原子Generation，不改变调参能力或生效时机。
- current Presentation Projection只在Gameplay producer语义变化时改变`CharacterPresentationSemanticContract.ContractHash`。本change只提升ProjectionRevision与PoseProgramImageHash，明确禁止Pose ABI迁移改变Gameplay ContractHash、Float32/Fixed ProgramHash或Network identity。
- current Diagnostics允许从完成workspace复制。本change收紧为运行帧内按interest冻结、Seal后只读取Committed Result并发布短租约View，避免跨Owner读取；通用框架只处理View之后的AOT生成与packet，Foot插件只处理领域字段与Sampler，可观察业务含义不减少，PoseGraph不取得任一下游编译器、packet、Host或Performance Build身份。
- archived设计曾表达Module分层，但archive不作为current truth。本change不依赖archive实现，只把仍有价值且与current spec一致的职责重新形成正式delta。

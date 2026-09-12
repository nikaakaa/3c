# Design: 已完成PoseGraph运行基础的文档归档

## 归档范围与输入输出

输入是原change中已完成的7组记录及其对应设计正文；输出是本归档的60条原完成记录、53项实现/文档任务、6个既有设计章节，以及原change保留的后续工作。7条验证或边界记录只保存在completion-record.md，不列入tasks.md。任务编号与Decision编号不重排，不重新宣称当前全部实现满足每个历史架构目标。

本归档没有运行实现差异。Source、Program、Constraint、Final Publication和根帧事务仍由现有系统拥有；不建立替代模块、第二变量存储或第二执行链。

## 文档所有权

| 内容 | 所有者 |
|---|---|
| 原任务1、2、4、5、6、7、8组的60条完成记录 | 本归档completion-record.md；其中53项实现/文档任务列于tasks.md |
| Decision 3、4、6、7、11、12的完成部分设计 | 本归档design.md |
| 其它任务组、全部原未完成项、只读Blackboard范围 | 原refactor-character-pose-graph-architecture；验证/撤销记录单独存入verification-history.md |
| 运行过程、历史失败、基线和跨阶段证据 | 原change的execution.md等证据文档 |
| FlowCanvas事件图、变量更新与Set的完整方案 | 独立事件图规划窗口 |

## 取舍与限制

按完整任务组分离可以避免同组中尚未完成的部分被误归档；代价是原change仍保留部分已勾选子任务，它们与该组的剩余项共同表达尚未收口范围。

保留原编号和链接，可以继续定位旧提交、执行记录和后续引用。当前正式合同仍以current specs为准；本归档不把历史设计覆盖到current specs，也不删除原始证据。

后续协调根收窄、Compiler/Definition质量、Scene Play观察、资源复用和Blackboard设计继续在原change处理。本次不改变它们的完成状态。

## 已完成设计正文

以下为从原design.md移出的正文。章节中的历史“当前”“新”及类名以原设计发生时为背景，现行代码与版本必须重新核对；这些章节只界定对应60项任务已交付的基础，不扩大本次归档范围。

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

# Change: 重构角色Pose Graph架构

BTSMTL技能后续作者迁移由[独立技能提案](../integrate-native-fsm-skill-authoring/proposal.md)管理，通用观察归[观察收尾](../finish-skill-runtime-observation/proposal.md)；本目录只负责PoseGraph，不要求两者采用相同runtime。

## 2026-09-08 当前增量范围

本节及后续旧版What Changes保留原重构范围；完成状态以tasks.md为准。2026-09-12已完成运行基础与只读输入独立变更的去向见下节，不能重复执行已归档任务。

当前已确定：保留Pose编译及Native／Job运行，只复用FlowCanvas作者UI，并在Unity Play中观察已有角色。该作者接入由[独立提案](../integrate-pose-flowcanvas-editor-preview/proposal.md)负责；技能留在独立技能提案，两边都不创建预览执行器。

原生FlowCanvas runtime实验与替换计划已撤回，历史见[决策记录](flowcanvas-experiment.md)。以下正式运行重构成果继续保留；旧专用画布目标由新提案替代，不自动完成未验收项或归档本change。

## 2026-09-10 UI、Preview与Build口径修正

Pose作者界面直接使用FlowCanvas原生`GraphEditor`。画布、breadcrumb、节点/连线Inspector、创建菜单、子图下钻、selection、clipboard和编辑器Undo均属于FlowCanvas作者表面；Pose Adapter只负责Capability、typed字段/端口、Mutation和诊断数据，不再设计第二套Pose画布或常驻Details作者面板。

正式Preview已经由独立Preview Fixture/Scene Session链负责，PoseGraph窗口不创建第二Preview播放器、时钟、临时Program或简化Executor。Build继续使用精确Character Definition的正式生命周期；Fixed入口同时发布Float32、Fixed和共享Presentation Projection，Pose-only输入只是Build内部的隔离输入，不是另一套Build路径。

PoseGraph的作者图必须可复用。`CharacterPresentationPoseGraphAsset`/FlowCanvas Graph只拥有稳定拓扑、参数、状态机和抽象能力合同；Character Animation Presentation Profile或独立Binding拥有Rig、Source、Slot、Policy、IK、Foot与Action资源。Graph不再把Corin资源、Character Definition或角色Profile当作自身所有者；Projection和Program仍按具体Definition生成角色实例化产物。

## 2026-09-12 完成归档与后续任务分离

原任务1、2、4、5、6、7、8组共60条完成记录及对应设计已移入[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/proposal.md)。其余旧架构任务及历史验证记录保留于本change，不因分离而改变完成状态。

原第25组的12项只读Blackboard任务、Decision 26设计和对应spec已整体移至[refine-pose-graph-readonly-blackboard](../refine-pose-graph-readonly-blackboard/proposal.md)。本change不再拥有该消费侧实现；EventGraph事件、变量更新与Set由独立事件图规划窗口负责。

## Why

当前Pose Graph的作者拓扑、typed端口、单次PlayableGraph Evaluate、唯一Goal Assembler、唯一FullBodyIK和唯一Final Writer方向正确，但运行与编译实现没有形成同等清晰的所有权。

以下为原重构启动时的问题背景，已完成部分见运行基础归档，不能把这些历史类名和规模当作当前源码状态：

- `PosePlanExecutionRuntime`仍超过4500行，同时持有Pose source、Player、PoseState、Blend Stack、AnimationSlot、Animancer backend、Physical Source、Motion Matching、Native Program、Workspace、Inertialization、Pose Constraint、Final Writer、release与Diagnostics。
- `CharacterPoseGraphStagedExecutor`仍超过4000行，把Program、Workspace、Slot、Value、Inertialization、Goal和Diagnostics的大量Native页展开到单个执行器中，调用Interface几乎暴露全部Implementation布局。
- `CharacterPoseGraphNativeProgram`同时保存不可变操作表、可变控制页、Pending/Committed状态和Goal workspace，因此“Program”并非不可变程序。
- `CharacterPresentationPoseOperation`使用约40项字段表达全部节点，大量`-1`既表示“不适用”又参与合法性判断；任一Goal、Linked Pose、Player或Control字段变化都要求跨Compiler、Projection、Native Program、Executor、Validator和Diagnostics同步。
- `CharacterPresentationPosePlanCompiler.CompilationState`是中央可变黑板；闭包、端口、节点语义、Value、Workspace、Operation和Stage规划互相原地修改，无法从一个阶段的固定输出定位错误。
- 节点语义分散在Capability Catalog、Authoring Adapter、Clipboard、Document、Compiler Handler、Projection Validator和Payload Codec。现有`ICharacterPoseCompilerHandler`暴露大量节点特例布尔值，但中央Compiler仍识别具体节点，形成没有隐藏复杂度的浅Module。
- 前置Pose Constraint重构已经把Foot、Goal Assembler、FBBIK和Writer收进单数根Bank，但外层Runtime仍扫描World-aware Operation并构造包含Operation字段、Goal workspace offset和内部Pose binding的输入，Staged Executor再调用暴露NativeSlice、Operation index和call-site index的Constraint Interface；Diagnostics又分别读取Native Program、Workspace和Pose Constraint，导致同一节点、结果和完成身份仍存在多个知识Owner。

结果不是单纯“文件太长”，而是业务改动需要理解太多内部布局：新增一个节点、修改一个Goal字段、调整一次Source生命周期或诊断字段都会横跨多处；任何遗漏都可能表现为运行期非法组合、半帧状态、重复控制权或行为回归。

本change不重新设计动画效果，而是把已批准的动画行为放入最终PoseGraph架构：用唯一Node Definition、固定Compiler Pass、不可变Program Image、每Actor状态、每帧事务、深Runtime Module、单一Operation执行Owner和Committed Result诊断链，取代当前巨型协调器、中央黑板、万能Operation与跨Module内部页读取。

当前Pose Graph作者入口已经切换为ParadoxNotion的FlowCanvas/NodeCanvas `GraphEditor`，Pose节点、端口、连接、breadcrumb和Inspector均由该作者表面承载；项目自己的Adapter只负责typed Projection和Mutation。正式Preview与Build不由作者画布执行，分别走正式Preview Fixture/Scene Session和精确Definition Build生命周期。

本次新增范围只迁移Pose Graph作者层：ParadoxNotion Canvas成为Pose Graph唯一具体画布和唯一作者资产模型，BTSMTL与AI作者实现不在本change范围。各领域继续共享Capability、Document、Mutation、事务和诊断合同，但不再要求使用同一个具体Canvas实现。Pose Compiler与Runtime继续消费唯一Pose作者数据并生成现有Program Image，不建立Canvas执行路径、第二Pose资产或双向同步。尚未形成正式闭环的`TrainingEnemy`不进入迁移清单；用户已决定将其整套内容退役，避免Canvas迁移继续背负不可构建、不可验收的半成品资产。

## What Changes

- **BREAKING**：将Pose Graph唯一作者资产和编辑表面迁移为ParadoxNotion Canvas Graph／Node／Connection。迁移器一次性把保留的正式Pose Graph、PoseStateMachine、节点、端口、边、布局与子图写入新资产；迁移完成后删除旧`CharacterTypedPoseGraph`作者存储、Pose专用`GraphAuthoringCanvasView`入口和旧状态机画布，不保留镜像资产、双写、反向同步或兼容读取。
- **BREAKING**：在作者资产迁移前整套删除`TrainingEnemy`内容，包括`Assets/Configs/Character/TrainingEnemy`、运行与表现Prefab、GameplayLab嵌套实例、`corin-training-enemy` roster与玩家target-provider绑定、AssetBundle collector、默认目录注册、专用作者／构建脚本和生成产物。该内容不转换到Canvas、不保留空Profile或占位配置；未来训练敌人或中立怪物必须在正式Gameplay、AI、Character Pipeline和Presentation链准备完成后重新建立。
- 将共享Graph Authoring边界收窄为领域无关的Capability、Document、Mutation、Clipboard、Selection、Undo、事务与Diagnostics合同。Pose通过唯一FlowCanvas Adapter把Node Definition的字段、固定／条件／动态typed端口、Pose空间和创建菜单投影到FlowCanvas原生GraphEditor；BTSMTL与AI继续使用各自当前具体画布，本change不迁移其资产或编辑器。
- 新增唯一`CharacterPoseNodeDefinitionModule`。每种Pose节点通过一个Definition Adapter集中声明Payload类型、字段合同、固定/条件/动态端口、允许Graph Role、Execution Domain、Authoring codec、Graph dependency投影、局部校验、typed lowering和Operation Family。Definition先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把固定端口、条件`portVariants`与node-local动态端口投影给Canvas、Document v4、Clipboard、Reconciler、Mutation preflight与Validator；Compiler只从同一Definition读取Graph dependency与typed lowering。删除旧Compiler Handler布尔矩阵和重复node-kind switch。
- 将Pose Plan Compiler重构为固定不可逆Pass：`Graph Closure -> Typed Lowering -> Topology -> Symbolic Family Lowering -> Stage Schedule -> Value Lifetime -> Workspace Plan -> Worker Batch Plan -> Bind Family Payload -> Seal Program Image`。Graph Closure通过Node Definition的结构化Graph dependency投影展开Subgraph与Linked Pose call，不直接解释具体Payload；先固定Operation及调度，再计算Value寿命与Workspace容量，然后按Execution Domain、Rig执行布局、typed read/write range与平台Kernel能力生成固定Worker Batch，最后绑定物理handle。每个Pass只消费明确前置的不可变Result并产生结构化诊断，删除中央可变`CompilationState`。
- 用不可变`CharacterPoseProgramImage`替换当前混合状态的`CharacterPoseGraphNativeProgram`。Program Image作为`CharacterPresentationProjection`内部唯一Pose程序保存Projection identity、`PoseProgramImageHash`、Rig、typed stage schedule、operation headers、family payload pages、常量页、value layout、workspace layout、Worker Batch Plan、Kernel Set、Rig执行布局、Execution Policy、source map与容量，不保存Actor状态、Pending/Committed页、平台线程对象或当前Frame结果。Runtime如需NativeArray等执行存储，每个`CharacterPoseProgramRuntime`只能建立一份actor-local、只读、identity精确匹配的`CharacterPoseProgramExecutionView`；该View只做逐值物理materialization，不得编译、重排、拆批、补字段或成为第二语义程序，并由对应Program Runtime唯一Dispose。
- 将每Actor的Pose Program逻辑状态收敛为`CharacterPoseActorState`，保存PoseState、Player continuity、ActionPlaybackInput lifecycle、Slot、Blend Stack、Routing、Inertialization等会影响下一帧的已提交状态。唯一根`CharacterPoseFrameTransaction`由`CharacterAnimationPresentationRuntime`拥有，只保存Frame lineage、阶段、各Module typed lease/result与统一Seal/Discard/Fault结果；Program Value、Operation completion和pending node state进入Program Runtime自有Frame页，Source、Constraint、Final Publication分别拥有自己的Pending页，根事务不成为共享可变黑板。
- 用分段typed ABI替换万能`CharacterPresentationPoseOperation`：公共Header只保存调度与typed value引用，Parameter Input/Resolve、Player、StateMachine、Action Input、Slot、Blend、Inertialization、Composition、Space Conversion、Component Control、Motion Matching、Pose History、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose与Output等Operation Family分别拥有固定Payload页。迁移前必须为全部现行Operation Code建立唯一Family、状态Owner、Frame页Owner、Execution Domain与删除字段映射；旧万能字段、`-1`组合、旧reader与兼容Projection直接删除。
- 新增深`CharacterPoseSourceModule`，唯一拥有provider sample装配、有限Action sample Adapter、Animancer/Playable source、Physical Source Registry、capture binding、prepared resource、usage、retirement与deferred release。PoseState、Player、ActionPlaybackInput lifecycle、Transition、Slot与Blend的逻辑决定仍由Pose Program节点拥有；Source Module不得成为第二Action winner、选择器或权重Owner。
- 新增深`CharacterPoseProgramRuntime`，唯一持有Program Image只读引用或自己的actor-local Execution View、Actor State、Program自有Frame页、Value workspace、持久执行Implementation和全部Pose Operation调度。它接收根Frame lease，按编译Stage与Worker Batch Plan恰好调度每个Operation一次；Pure Pose Family只进入AOT可知的Burst/HPC# Family Kernel，World Query、Foot Placement、Goal、FinalIK/FBBIK和Final Publication仍进入Compiler明确指定的Managed执行域。外层Runtime、Source Module、Constraint Module和Diagnostics不得再次解释Operation。
- 新增唯一会话级`CharacterPoseWorkerScheduler`。它从当前表现帧收集已准备Actor的typed工作，按Program Image、Rig执行布局、Stage依赖波次与Family Kernel identity组成跨Actor批次，只提交Compiler已经证明的read/write set和Completion依赖；不得为每个Node或Bone创建Job，也不得让每ActorRuntime各自`Schedule`后立即`Complete`。Actor的Frame、状态、故障、Reset、Replacement和Dispose保持隔离，所有Outstanding Job必须通过该Scheduler完成fence后才能释放actor-local页。
- 保留当前`CharacterPoseConstraintRuntime`及其内部IK实现，仅收紧外部接口和存储归属，唯一拥有Foot Placement、PoseBone Goal、Goal Contribution、Goal Assembler、唯一Goal Set、FBBIK、BendHistory和Solver Result。Program Runtime在每个Constraint Family Operation的编译位置通过typed编译Handle恰好调用一次对应入口并写入该Operation唯一completion；Constraint Module不扫描Program、不拥有第二份Stage Schedule，并在完整闭包结束后发布一个typed Constraint Result。其Interface不得暴露NativeSlice、Goal offset、Operation index、Callsite index或内部Bank页。
- 新增具体`CharacterFinalPosePublication` Module，唯一拥有Committed/Pending Final Pose物理页、完整Final Pose验证、Physical Writer binding、一次整Rig写入与Publication Result；Program Image中的Final Output只保存稳定Publication layout handle，Actor Runtime创建时将其绑定到当前Final Publication Pending页，不在共享Program Image中保存Actor页引用，也不分配第二Final Pose页。Topology只证明唯一Output与Publication requirement，具体Writer唯一性由Runtime Factory和Final Publication构造验证。当前只有一个Writer Implementation，不建立假设性可替换接口。
- 将`CharacterAnimationPresentationRuntime`收窄为actor帧级协调根：`Apply Pending Tuning -> Begin Root Transaction -> Plan Control/Demand -> Prepare Sources -> Prepare Program/Worker Lease -> Validate Barrier -> Animancer Evaluate -> Worker/Managed Program Execution -> Publish Final Pose -> Seal`。它拥有唯一根Frame Transaction，只交换typed Lease/Result，不保存节点业务状态、不理解Operation字段、不执行Foot/Goal/FBBIK数学；会话级Scheduler只接收多个actor root提交的typed就绪工作并归还匹配lineage的Completion，不接管actor业务状态。现有在线调参必须通过actor-local `CharacterPoseTuningSnapshot`按Program、Source与Constraint Owner分区预验证并原子提升Tuning Generation；Program Image只保存Build默认值，任何调参不得修改共享Image或Execution View。
- 将Runtime Diagnostics改为单向Projector。PoseGraph在Frame开始冻结Foot IK typed interest与View容量，只从同一成功Frame提交的`Source Result + Program Result + Constraint Result + Final Publication Result`及interest-gated冻结页生成具体`CharacterFootIkCommittedCaptureView`，并唯一拥有其Post-Seal短租约的生产、有效期与失效。删除跨Module内部页读取和万能Committed View；PoseGraph不拥有Diagnostic Capability、Sampler、Schema、Generated Program、typed packet、CSV、Analyzer或Publisher。下游`refactor-foot-ik-diagnostic-sampling`只在该具体租约内调用框架生成的Foot Capture Program并声明字段／Sampler／Host业务；`add-generated-diagnostic-sampling-framework`只拥有通用生成、packet、Session、Writer／Reader和manifest生命周期。现有诊断与七维评分业务语义保持不变。
- Pose Graph作者窗口不再拥有独立`AnimationPreviewRuntime`、第二播放时钟或直接修改Gameplay状态的Seek路径。预览由`rebuild-btsmtl-preview-with-scene-play`中的正式Scene Play会话负责，直接运行同一Simulation Program、Presentation Projection、Program Image、Pose Runtime、Source、Constraint、Final Publication和World Context；Pose窗口只发起场景预览、选择观察目标并读取Committed Diagnostics。
- 迁移完成后删除旧`PosePlanExecutionRuntime`巨型Implementation、旧`CharacterPoseGraphNativeProgram`混合容器、旧`CharacterPoseGraphStagedExecutor`串行执行链、旧`CharacterPresentationPoseOperation`万能ABI、旧Compiler Handler Registry、旧中央CompilationState、重复Validator/Codec知识和Diagnostics内部读取，不保留Managed重算、串行fallback、wrapper、开关或双运行链。actor-local Execution View只能是Program Image的只读物理视图，不得保留旧Native Program的Actor状态、Frame页、运行时Compile、独立schema或第二Job计划。

## Implementation Baseline

用户指定的唯一行为基线固定为`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`，不随HEAD、工作区或其它change进度自动更新。已核对该提交的动画／IK核心执行链；代码入口、必须保持的计算顺序、状态与既有回放证据见[行为保护清单](behavior-baseline.md)。

- Foot／IK已经到用户当前保留阶段，本change不要求全部待办完成或归档，也不把“差不多”解释为所有视觉问题已经解决。
- 实施前对照该提交检查实际源码、配置与产物。之后的相关改动必须单独列出，由用户裁决冲突，不能覆盖或静默并入基线；无关工作不回退。
- Foot、Pelvis、Goal、FBBIK公式、数值顺序、准入、权重、配置、Anchor、Rotation、连续历史与正常Reset保持；第一阶段已批准并独立验证的Reset修正作为明确例外继续保留。保留该提交中的有符号膝向运输；已撤除的骨盆Reach硬夹紧、末端夹脚、SmoothKnee和CurrentSupport替代Swing包络候选不恢复。
- 本次只迁移外层调用、存储归属、根事务、Compiler、ABI和最终Pose发布。不借迁移重写Foot内部流程、修复Vendor历史方向、改变动画时钟／混合顺序、调整地面查询或重新调IK。
- 已知抖动、穿透、离面、反弯及未覆盖输入原样记录；不能通过改算法、参数、评分或分母掩盖。不能只用“编译通过”或“总分不变”宣称行为等价。
- 用户本次Goal已授权串行实施：先完成并验证IK维护重构，再实施本change；不将未完成的新架构提前写入current specs或project truth。

## Dependencies And Sequencing

1. current specs中唯一Goal Contribution、Assembler、Goal Set、FBBIK和Writer的外部数量与隔离合同继续保留；实际动画与IK行为按上面指定提交迁移。
2. `build-character-foot-motion-data-foundation`已归档；`stabilize-character-foot-path-and-landing`未完成或未归档不再阻塞本change。其已保留实现属于基线，剩余行为任务不自动接管、不要求先做完。
3. 用户本次Goal明确将`refactor-character-ik-maintenance-boundaries`列为本change的已验证接入前置。Foot请求／最终结果、Interpolation历史、独立验证的Solver Reset修正及诊断列绑定由第一阶段完成；本change保留其通过成果，只迁移外部架构，不恢复第一阶段删除的旧结构。
4. ClipPlayer与BlendSpacePlayer通用能力已进入current specs；独立Blend Space演示内容不作为前置。Linked Pose、Motion Matching、Transition Routing、Blend Stack与Inertialization只迁移已存在的正式节点和source生命周期，不补装未运行内容。
5. `compact-foot-diagnostic-publication`与`consolidate-foot-diagnostic-scoring`已落地的分析、明细存储和评分业务语义继续使用，不要求先归档；PoseGraph与`add-generated-diagnostic-sampling-framework`可分别完成具体Foot IK短租约View和通用AOT生成／packet合同，两个接口严格闭合后，`refactor-foot-ik-diagnostic-sampling`再以首个领域插件替换现有Foot Sampler、手写Column和二次join。不得先给旧Sampler增加临时Adapter、Foot专属Generator、表达式委托、反射路径或第二Snapshot。
6. 本次按指定提交冻结动画与IK算法、时钟和配置。若其它任务改动同一Owner、同一字段或同一行为基线，必须先报告具体冲突由用户决定，不覆盖已经改对的内容；无关任务不构成本change的全局等待条件。
7. Worker Batch、Family Kernel和跨Actor调度属于本change第10～12阶段的正式实现，不等待本change先完成或归档。它们必须在Node Definition线程安全事实、Compiler Pass和分段ABI第一次封存时一起落地，禁止先发布纯串行新ABI再开后续change重做；动画预算、Phase Offset、跳帧与插值补帧不在本change内。
8. Pose作者层依赖项目中已经导入的ParadoxNotion Canvas源码。本change只迁移Pose资产与编辑入口，不修改BTSMTL、AI的作者资产、编辑器或运行语义；共享合同的实现必须允许不同领域使用不同具体Canvas，同时继续保持每个领域唯一资产、唯一Mutation与唯一事务。
9. Pose场景预览的会话、输入推进、暂停、单步、重建与时间定位归`rebuild-btsmtl-preview-with-scene-play`。本change只删除Pose窗口中的平行预览运行链并接入该正式入口，不复制其Scene、Session或时间推进实现。
10. `TrainingEnemy`不是行为保护对象，也不是Canvas迁移输入。先从GameplayLab composition、目标输入、构建入口和内容收集中移除全部引用，再删除其配置、Prefab、专用脚本与生成数据；保留的Corin内容不得因清理第二Actor而改变动画、IK或无目标攻击语义。

## Capabilities

### New Capabilities

- `character-pose-graph-runtime-architecture`：定义Pose Runtime唯一Owner、帧事务、状态寿命、诊断和最终发布边界。
- `character-pose-plan-compilation`：定义Pose作者数据到不可变Program Image的编译合同。

### Modified Capabilities

- `btsmtl-compiled-simulation-program`：保持Pose输入继续来自正式编译Program与提交事实。
- `character-animation-pipeline`：保持唯一表现事务、Source、Constraint和Final Publication边界。
- `character-animation-selection-runtime`：保持Player、Action、Slot、Blend与Inertialization所有权。
- `character-foot-placement-presentation`：保持Foot、Goal、FBBIK与Writer的唯一执行链。
- `character-presentation-pose-graph`：将Pose唯一作者资产和编辑表面迁移到ParadoxNotion Canvas，保持唯一编译和运行链；只读输入专项由独立change维护。
- `graph-authoring-domain-framework`：共享Capability、Document、Mutation与事务合同，但允许不同领域使用各自唯一具体Canvas。

## Impact

- Affected specs: `character-presentation-pose-graph`、`character-animation-pipeline`、`character-animation-selection-runtime`、`character-foot-placement-presentation`、`graph-authoring-domain-framework`、`btsmtl-compiled-simulation-program`
- Added specs: `character-pose-graph-runtime-architecture`、`character-pose-plan-compilation`
- Deferred spec delta: `character-targeted-motion-warp-demo`需要删除TrainingEnemy相关Requirement并保留通用MotionWarp与无目标攻击语义；该delta必须在实施删除前补齐
- Affected runtime: Animation Presentation协调根、会话级Worker调度、Pose source lifecycle、Pose Program、Native workspace、Burst/HPC# Family Kernel、staged execution、Pose Constraint装配、Final Pose publication、Diagnostics
- Affected editor/compiler: ParadoxNotion Canvas Pose Graph/Node/Connection、Pose capability/definition、Document/Clipboard/Mutation adapter、Projection Compiler、Topology Validator、Source Map、Projection codec与Build validation
- Affected authoring data: 保留的Corin Pose Graph、PoseStateMachine、节点、端口、边、布局和子图需要一次性迁移；旧Pose作者资产模型与自建具体Canvas在迁移后删除；`TrainingEnemy`整套内容直接退役，不进入迁移器
- Affected generated data: Character Presentation Projection schema、Pose Program Image schema、Operation ABI、Workspace layout、Worker Batch Plan、Kernel Set、Rig执行布局、Execution Policy、`PoseProgramImageHash`与ProjectionRevision；Gameplay `ContractHash`保持不变
- 除删除GameplayLab中的`corin-training-enemy`第二Actor、玩家对它的显式目标绑定和对应训练AI内容外，不改变保留角色的Gameplay Program、Simulation Tick、KCC、Root Motion移动决策、Network Model、Rollback Snapshot或业务Timeline语义；玩家攻击继续按既有`OptionalSnapshot`无目标语义执行

## Current Spec Comparison

- current `character-presentation-pose-graph`已经规定唯一typed拓扑、有序Stage、一次PlayableGraph Evaluate、唯一FBBIK与唯一Writer。本change保留这些外部语义，只补足Program Image、Node Definition、Compiler Pass、Operation Family ABI和Runtime Owner。
- current Pose Constraint合同已经把Goal Source收敛为typed Goal Contribution、唯一Assembler和唯一Goal Set。本change不恢复plural Goal Set、Empty Goal、LegIK、TwoBoneIK或第二Solver。
- current `character-animation-pipeline`已经规定唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`事务、Dense双页、稀疏journal与Fault边界。本change不推翻事务政策，而是把Program Image、Execution View、Actor State、Program/Module Owned页、根Frame Transaction和Module Result明确分层，删除各内部对象重复持有Pending/Committed知识。
- current `character-animation-pipeline`已经要求Pose Job直接写Pending页，但current Compiler、Program Image与Runtime specs尚未定义线程安全事实、Worker Batch、Kernel identity、跨Actor唯一Scheduler或生命周期fence；现行固定Pass也仍在`Workspace Plan`后直接`Bind Family Payload`。本change在首次新ABI Seal前补齐这些缺口，并保持Animancer Evaluate、Managed World／Constraint、Final Publication和每ActorFault语义；不存在另一个后续Worker change或串行兼容期。
- current `character-animation-selection-runtime`已经规定PoseState、Player、Transition、Slot、Blend Stack、Inertialization和Animancer各自业务权限。本change保持这些节点语义；Source Module只收物理采样与资源生命周期，Program Runtime收逻辑节点状态，二者不得互相抢权。
- current `character-presentation-pose-graph`仍要求新增节点注册typed payload与compiler handler，但current `graph-authoring-domain-framework`已经要求唯一Pose Node Definition并禁止第二Compiler Handler，两份current spec存在明确矛盾；现行代码仍保留`ICharacterPoseCompilerHandler`与Registry。本change以Framework current requirement为方向，修改Pose Graph requirement并删除旧Handler实现，不把现行代码误写成current架构真相。
- current `graph-authoring-domain-framework`已经固定Definition向共享Capability投影、Reconciler与Document Transaction Service继续拥有唯一事务。本change进一步固定`Node Definition -> GraphAuthoringCapabilityCatalog -> GraphAuthoringNodePortShapeProjector -> Canvas/Document Exporter/strict parser/Target Mapper/Reconciler/Mutation preflight/Validator`唯一作者链；Compiler只直接读取同一Definition的Graph dependency与typed lowering，BTSMTL与AI领域继续使用各自正式Definition/Capability Adapter，不建立Pose专用Framework。
- current `graph-authoring-domain-framework`同时要求BTSMTL、AI和Pose复用同一具体Canvas。本次用户明确把范围限定为Pose Graph接入ParadoxNotion Canvas，因此本change修改该要求：跨领域共享Capability、Document、Mutation、事务和Diagnostics合同，各领域分别只有一个具体Canvas和一套作者资产。取舍是Pose可以使用导入Canvas的节点交互且不牵动BTSMTL资产，但通用视觉交互不再由一个具体Canvas实现统一；不得通过双Canvas Pose视图或镜像资产补偿该差异。
- current `btsmtl-compiled-simulation-program`仍使用`CharacterPresentationPoseProgram`描述Projection内Pose程序，`project.md`仍同时出现Native Pose Program。本change统一为Projection内部唯一`CharacterPoseProgramImage`，并只允许每个Program Runtime建立一份identity精确匹配的actor-local只读Execution View；Gameplay Numeric Target、`CharacterPresentationSemanticContract.ContractHash`与Presentation Projection分离规则不变。
- current Constraint根Bank提交Foot、Goal与FBBIK结果。本change保留该根Bank，把Final Pose页、Writer与Physical Result迁入Final Publication，并让Diagnostics Projector按同lineage组合Constraint与Publication Committed Result。
- current Preview、Pose Watch和Live Debug允许读取完成workspace，但当前实现仍跨Native Program、Constraint与Workspace取值。本change把读取对象收敛为Committed Result和interest-gated诊断页，不改变可观察业务内容。
- `rebuild-btsmtl-preview-with-scene-play`已经选择正式Scene Play会话拥有Scene、Session、输入推进和时间定位。本change只让Pose窗口发起该入口并读取Committed结果，删除独立`AnimationPreviewRuntime`、第二时钟和直接修改Gameplay状态的Seek，不重复定义预览执行链。
- current `character-targeted-motion-warp-demo`要求Standalone注册玩家与`corin-training-enemy`两个Actor，并把玩家target provider绑定到训练敌人；这与用户2026-09-05决定整套删除`TrainingEnemy`直接冲突。删除后的正式语义是GameplayLab只保留Corin，target input为None，五段攻击继续执行原始MotionCurve。当前change尚无该capability的delta文件，实施前必须补齐，不能把现行spec伪装成已同步。
- 当前实现没有保持外层协调器与Pose Plan执行职责的Locality。本change把该边界写成可执行的current delta，并明确删除旧巨型Owner的完成标准，避免只新增类名而不迁移责任。

- current Foot spec已不固定内部状态机类名，而本change旧delta仍要求`CharacterFootStateMachine`；本次删除该过期约束，只保留当前Foot输入、结果与唯一事务边界，不重做已经完成的Lifecycle拆分。
- 部分current／active文字仍按旧Reach夹紧与“Pelvis先读最终Resolved”描述；当前保留代码和用户最新裁决已撤除业务层骨盆／末端夹脚。这里明确保护已保留行为，不利用旧文字恢复政策，并在第一阶段通过后保留其内部请求／最终结果边界。
- 已落地诊断已有唯一Analyzer、Publisher、版本化小报告／明细存储和七维评分业务语义。本change只迁移Runtime事实来源；通用AOT生成与packet链由`add-generated-diagnostic-sampling-framework`唯一拥有，Foot字段／Sampler／Host业务由`refactor-foot-ik-diagnostic-sampling`拥有，不新增第二采样、重新评分或旧格式兼容路径。

## Non-Goals

- 不改变保留的Corin PoseState选择、Transition rule、Standard Blend、Blend Stack、Inertialization、Slot、Linked Pose、Blend Space、Motion Matching、Foot Placement、Goal、FBBIK或Writer的逐帧业务结果。
- 不删除或降级现有actor-local在线调参能力；调参只迁移Owner与事务，不得修改共享Program Image、跨Actor传播或改变生效时机。
- 不新增角色专属Pose节点、Layer、Control Rig、传统Animator Controller、第二PlayableGraph、第二Animator、第二IK solver或GPU动画路径。只读输入与曲线绑定的通用Capability调整已归独立只读Blackboard change；EventGraph事件和Set的作者/执行能力属于独立规划。
- 不重新设计Foot Contact Plan、Heel/Toe、脚掌旋转、Reactive、移动平台或上下楼专用动画；不恢复已撤销Reach夹紧或SmoothKnee，不接管任何未实施IK行为任务，不再改变第一阶段通过后的正常初始化／Reset结果。
- 不改变Gameplay/Presentation边界，不把Pose、Program Actor State或Frame workspace写入Rollback snapshot或网络协议。
- 不创建通用插件容器、运行时反射Handler、任意Operation注册表、Solver抽象接口或只有一个Adapter的假设性Seam。
- 不以拆分文件行数作为完成标准；如果调用方仍需理解内部页、offset、index、Seal顺序或节点特例，视为重构未完成。静态合法性只在Build证明、固定绑定只在Runtime创建检查；逐帧只检查变化事实和跨Owner交接，不在每层重复完整验证。
- 不新增自动测试或手动验证任务；实施复用现有编译、静态检查与OpenSpec严格校验，并保留现有正式输入／诊断基线供同语义对账。重建或回放遵守项目显式入口，不创建验证旁路。

## Success Criteria

```text
CharacterAnimationPresentationRuntime只编排Frame阶段和typed Result
根CharacterPoseFrameTransaction只拥有lineage、阶段、Module lease/result与统一Seal/Discard/Fault
Pose source物理资源只有CharacterPoseSourceModule一个Owner
Pose Operation只有CharacterPoseProgramRuntime一个执行Owner
ActionPlaybackInput lifecycle只有CharacterPoseProgramRuntime一个逻辑Owner
Foot/Goal/FBBIK只有CharacterPoseConstraintRuntime一个业务Owner
Physical Bone只有CharacterFinalPosePublication一个写入Owner

Program Image完全不可变且不保存Actor或Frame状态
Program Image只存在于CharacterPresentationProjection内部且Runtime不构造第二程序真相
每个Program Runtime最多一个Execution View，只逐值materialize同一Image并由该Runtime唯一释放
Actor State只保存跨帧Committed状态
Program、Source、Constraint与Publication各自只写Owned Pending页
根Frame Transaction不保存任一Module内部Workspace
Pending Final Pose只有Final Publication一个物理页Owner
actor-local Tuning Snapshot不修改Program Image或其它Actor

每种节点语义只在一个Node Definition Adapter中声明
Graph Closure只通过Definition的Graph dependency投影发现Subgraph与Linked Pose call
端口形状只通过GraphAuthoringNodePortShapeProjector投影
Compiler没有中央可变CompilationState
Operation ABI没有万能40字段记录和无意义-1组合
每个Operation Family只读取自己的typed Payload页
全部现行Operation Code都具有唯一Family、状态Owner、Frame页Owner与Execution Domain映射

Runtime、Preview与Diagnostics消费同一Program Image和完成语义
Diagnostics只读取Committed Result，不读取内部Workspace或重新执行节点
不存在旧Runtime wrapper、旧Program容器、旧Executor构造、旧Handler Registry、旧ABI reader或fallback
TrainingEnemy配置、Prefab、场景实例、roster、目标绑定、构建入口、collector、默认目录和生成产物均不存在残留引用

保留的Corin动画、Foot Placement、Goal、FBBIK和Final Pose业务结果保持ad3527e103cc3235a63e8a1c1dbd26df5155e0ba基线
IK维护重构通过后串行接入；其它Foot待办与归档状态不是开工前置
保留第一阶段已通过的IK成果；不吸收其余未实施或已撤销方案，不掩盖基线已有问题
Foot诊断字段业务含义、Analyzer、Publisher、明细存储与七维评分数学保持；通用AOT生成、typed packet与Host生命周期由`add-generated-diagnostic-sampling-framework`拥有，Foot插件由`refactor-foot-ik-diagnostic-sampling`拥有
Gameplay ContractHash、Float32/Fixed ProgramHash与Network identity不因Pose ABI重构改变
```

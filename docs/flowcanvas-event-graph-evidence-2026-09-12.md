# FlowCanvas 事件图：当前代码与规范核对

日期：2026-09-12。对应[规划 r1](flowcanvas-event-graph-plan-2026-09-12.md)。本文件记录静态读取证据，不是实现或运行通过报告。

## 1. 读取基线与保护范围

工作区：`D:/Unity_Project_1/3C`。读取开始时 HEAD 为 `9f50331949b29bc3d1ef22b391341fe5e0b0a04d`，期间其它任务推进到 `751a9e6b4`。工作区有未提交代码、作者资产与生成产物，因此本次是工作树源码核对，不是单一 commit 的可重现运行验证。实施前必须重新读取精确文件及差异。

本次创建文档前已见的交叉改动包括：

- `Assets/Configs/Animation/Presentation/PoseGraphs/LocomotionFullBodyPoseGraph.asset`。
- `Assets/GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/AgentAuthoringDocumentExporter.cs`、`AgentAuthoringDocumentTransactionService.cs`、`AgentDocumentMutationReconciler.cs`。
- `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/SkillDocument/` 多个文件。
- `Assets/GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/` 多个文件。
- `Assets/ParadoxNotion/CanvasCore/Framework/Design/PartialEditor/Editors/BlackboardEditor.cs`。
- Corin Definition/Skill/Timeline/Profile、prefab、scene、generated products 与诊断文件等其它内容。

这里的 `Assets/` 均相对 `3cDemo/Client/3C_Client/`。本规划未修改这些文件。BlackboardEditor 当前差异涉及只读列表仍可拾取、只读值禁改和拖拽交互，后续不得回退这批行为来添加 EventGraph 的作者变量。

## 2. FlowCanvas 原生能力

| 代码入口 | 当前事实 | 对规划的约束 |
|---|---|---|
| [FlowGraph.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowGraph.cs) | 原生图基类及现有 `CanAuthorNodeType`、连接、菜单等项目扩展点 | 可直接接新领域，不需要重写画布；FlowGraph 基类本身不等于完整 FlowScript 执行编排 |
| [FlowScript.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowScript.cs) | 初始化 `IUpdatable`、Macro、IInvokable；第二遍绑定端口；Update 遍历 updatable | 原生事件执行现成；运行基类应复用这个实现 |
| [Graph.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Graph.cs) | `StartGraph` 支持 `UpdateMode.Manual`；非 Manual 才注册 MonoManager；`UpdateGraph(float)` 写 delta/elapsedTime 后更新与推进同步协程 | 原生 runtime 可以由宿主手动推进，不能把“自动 Unity Update 无法控制”当拒绝理由；也不能声称传 delta 后所有节点均不读全局时间 |
| [FlowScriptController.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowScriptController.cs) | `GraphOwner<FlowScript>`，提供自定义函数调用 | 动画宿主无须再挂这个自动 owner |
| [UpdateEvent.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Events/Graph/UpdateEvent.cs) | IUpdatable 调用 FlowOutput；interval 非正时每次调用；正值依赖 graph.elapsedTime 与 lastUpdatedTime | interval 是状态语义，不能忽略后宣称节点被完整编译/回滚 |
| [StartEvent.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Events/Graph/StartEvent.cs) | OnPostGraphStarted 执行；graphAgent 为 GraphOwner 且尚未 Start 时会订阅回调；节点保存 called | 宿主不用 GraphOwner 代理 Unity Start；先绑定有效输入再 Start，Reset 重建实例 |
| [GetVariable.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Variables/GetVariable.cs) | BBParameter 同时支持常量/链接变量，ValueOutput 在读取时返回当前值 | 能直接复用；“值线只计算一次”的全图缓存不符合原生行为 |
| [SetVariable.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Variables/SetVariable.cs) | FlowInput 执行赋值后调用 Out；值输出读取目标当前值；部分 AssignOp/perSecond 使用 Time.deltaTime | 宿主必须约束全局时间模式；不能只说所有 Set 已天然服从表现 delta |
| [Variable.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Variables/Variable.cs) | Variable<T> 保存原生值与 ID，支持 BindGetSet；setter 路径有 value-change 行为 | 可接只读宿主数据与实例变量；不可把只读 UI 当作唯一写权限校验 |
| [SwitchBool.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/FlowControllers/Switchers/SwitchBool.cs) | 先调用 True 或 False，再调用 Then | 与二选一 Branch 接口有细节差异，文档和适配必须保留 |
| [Split.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/FlowControllers/Other/Split.cs) | Instant 顺序执行；Timed 使用 coroutine、tracks 与运行状态 | 同步动画合同可原样接 Instant；不能把 Timed 当相同能力放开 |
| [Sequence.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/FlowControllers/Togglers/Sequence.cs) | 显示名 Flip Flop，每次调用推进 current，并支持 Reset | 不等于 UE 风格同次顺序执行所有输出；复制 Blackboard 不含该状态 |
| [FlowScriptExtensions.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowScriptExtensions.cs) | Simplex 与 reflected method/field/constructor/extractor 菜单和 wrapper | 可复用现有方法调用；方法白名单、AOT 与领域权限需接入，不能开放任意副作用 |
| [Time.cs](<../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Nodes/Functions/Implemented/Implemented Nodes/Time.cs>) | Wait/WaitUntil 等跨帧；DeltaTimed 直接读 Time.deltaTime | 不能将“原生节点都可运行”推导为“全部适合一次同步动画输入更新” |
| [Ports.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/Ports.cs) | 编辑器条件分支捕获异常并 Node.Error；ValueInput 可能继续返回此前 result；原生断点可经 coroutine 延后执行 | 要有正式按图失败通知与同步调用限制；外层 try/catch 不构成完整错误处理 |
| [Node.cs](../3cDemo/Client/3C_Client/Assets/ParadoxNotion/CanvasCore/Framework/Runtime/Graphs/Node.cs) | Error 记录日志并设 Status.Error | 应利用/扩展真实错误边界，不靠 Console 日志推断成功 |

## 3. 项目作者与运行入口

下列文件相对 `Assets/GameScripts/Main/`。

| 入口 | 当前事实 |
|---|---|
| `Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillFlowGraph.cs` | Skill/Subgraph/StateMachine/ConditionRule/StateBody/TimelineBody roles；独立 BlackboardDeclarations 和节点准入；OnGraphInitialize 明确拒绝原生执行 |
| `Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillNativeNodeCatalog.cs` | 当前登记 3 个逻辑节点和 12 个 Float/Int 比较 wrapper，映射到 SimulationOperationCode；不证明全部 FlowCanvas 可编译 |
| `Runtime/BTSMTL/TreeDesigner/Scripts/Authoring/GraphAuthoringCapabilityCatalog.cs` | 共享领域、字段、端口、作者类型/稳定 kind 注册 |
| `Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPoseCanvasGraph.cs` | FlowGraph 作者资产；Parameters 的只读 editor Blackboard；拖拽走 EditorWriteRouter.CreateParameterGet；拒绝原生 runtime |
| 同目录 `CharacterPresentationPoseGraphAsset.cs` | root Graph、flat GraphCatalog、state layout、source/resource slots；不是现成的动画事件宿主 |
| 同目录 `CharacterPoseAuthoringContracts.cs` | 参数声明有 Float/Int/Bool 与 Control/AnimatedProperty；默认值当前统一 float，不能据此声称一般 typed 状态已完整支持 |
| `Editor/CharacterSimulation/Compilation/Presentation/CharacterPoseIrCompilation.cs` | ProgramParameterInput 定义输出 `pose.parameter`；FactAndDemand 域；没有证明任意 native ValueOutput 已可编入 Pose |
| `Editor/CharacterSimulation/Compilation/Presentation/CharacterPoseCompilerModule.cs` 与 `PoseGraph/Passes/` | 唯一固定编译链，含 Closure/Typed/Topology/Symbolic/Stage/Value/Workspace/Worker/Payload/Seal；不存在通用原生事件图 lowering |
| `Runtime/Character/ThirdPersonClient.Runtime.asmdef` | 已引用 NodeCanvas/FlowCanvas/ParadoxNotion；无需复制另一套插件源码 |
| `Editor/CharacterPipeline/Authoring/PresentationDocument/` | 现有 Package Codec/Exporter/Validator、Reconciler、Mutation Plans，PoseGraph/StateMachine/Linked Pose 等正式闭包 |

### 当前正式动画主链

1. `Runtime/Character/Pipeline/Presentation/CharacterPresentationRuntimeFactory.cs` 创建 `CharacterSimulationPresentationRuntime`。
2. 后者 `BeginPresentationFrame` 在有效正 delta 下调用 `CharacterPresentationFactProjector.Project`。
3. `CharacterPresentationProgramParameterFrame.FromFact` 生产固定 motor 输入，再传入 `CharacterAnimationPresentationRuntime.BeginPresentation`。该类位于文件 `CharacterPresentationRuntime.cs`。
4. 动画根建立现有 FrameTransaction，推进 Action lifecycle/sample、PoseAdvance、MM、FinalizePoseState、SourceDemand/Prepare，之后走既有 Evaluate/Seal。
5. `Animation/PoseGraph/CharacterPoseFrameCoordinator.cs`、`Program/CharacterPoseProgramRuntime.cs`、`CharacterPoseProgramActorRuntime.cs` 将参数帧传到 `Animation/Presentation/CharacterPoseStateSourceRuntime.cs`。
6. 后者为活动 BlendSpace 设置帧，`AnimationBlendSpacePlayerRuntime.PrepareCapture` 按 X/Y ParameterId Require；Transition 当前独立消费 FactFrame。

`Presentation/CharacterPresentationFrameCoordinator.cs` 虽有相似代码，本轮全 GameScripts 的类名搜索只找到定义/构造声明，不能用它说明当前入口，也不能直接把宿主同时接到两条链。

### 固定参数桥的精确问题

`CharacterPresentationProgramParameterFrame` 提供 `MotorPlanarSpeed`、`MotorLocalVelocityX`、`MotorLocalVelocityY` 三个 ID。`FromFact` 使用 HorizontalSpeed 和 MovementDirection 分量生成后两个值；`FromBody` 则通过 VisibleRotation 的逆变换计算局部速度。这两条生产公式并不完全相同，迁移不能仅凭名字互换坐标含义。

`CharacterPresentationPoseSourcePlanCompiler` 明确调用 `Supports` 限制 BlendSpace 轴 ID。`AnimationPreviewEngine` 仍调用 FromDirect/FromFact；必须核对其真实用途后完整迁移，不能只删除主运行处一行就声称旧桥消失。

FactProjector 持有历史 Intent、previous frame/velocity、presentation time 和 discontinuity。Project 在 Pose 根事务外更新这些历史，计算速度、方向、加速度、朝向误差和 MotionPhase。它既做输入对齐也有派生事实；本任务没有证据可将整个类型删除。

Pose 的 source-local parameter 页还参与 State 混合、Inertialization、Final Property Writer 等。事件图变量应进入独立的只读控制输入合同，不能直接把这些曲线页替换成原生 Blackboard。

## 4. 规范与关联任务对账

| 来源 | 当前条款/范围 | 本任务关系及处理 |
|---|---|---|
| [project.md](../openspec/project.md) | Program/Session 唯一链；Pose Program Image、根事务和四个业务 Owner；直接资源 Player 等当前口径 | N 的原生事件输入角色尚未声明；必须明确新边界，不能说现行规范已经允许。Pose 核心继续唯一 |
| [graph-authoring-domain-framework](../openspec/specs/graph-authoring-domain-framework/spec.md) | Authoring 节点与 runtime 分離；领域 Compiler 将 graph 编译为 runtime program；共享 Capability 和 typed Mutation | N 与笼统编译要求有直接冲突；保留共享作者语义，需用户决定事件领域的原生运行合同 |
| [character-pose-graph-runtime-architecture](../openspec/specs/character-pose-graph-runtime-architecture/spec.md) | 四 Owner、不可变 Program、Pending/Committed 页、根 Seal/Discard/Fault、只读观察 | N 不移动 Pose Operation 或 Writer；新增输入状态不随 Pose Discard 回退，应明确标出保证范围变化 |
| [character-pose-plan-compilation](../openspec/specs/character-pose-plan-compilation/spec.md) | 固定 Pass、唯一 Node Definition、typed value 生命周期、一次 Seal、旧 ABI 破坏替换 | 两个方案都保持 Pose 部分；N 只增加输入绑定，C 才增加事件操作 lowering |
| [character-presentation-pose-graph](../openspec/specs/character-presentation-pose-graph/spec.md) | 第 178 行起 ProgramParameterInput 读 committed parameter page，curve 随 Pose 传播；第 80 行 Transition 只读 Fact/TimeInState/remaining time | 自建变量同次消费、Bool/Int 进入条件需增订；N 的输入发布身份不同于 Pose 成功提交，不能混用 committed 一词 |
| 同一 Pose spec | 一处要求 Profile source binding，一处可复用图禁止直接资源；project 与作者 change 又有直接资源 Player 方向 | 现有文档之间已有差异。事件图不裁决/接管资源作者改革，不以旧条款阻止当前用户讨论 |
| [graph-authoring-editor-shell](../openspec/specs/graph-authoring-editor-shell/spec.md) | 尚有 GraphView、自有共享 Shell/BaseGraph 等条款 | 与当前 FlowCanvas 原生 GraphEditor 代码和 active Pose 作者方案不完全一致；本任务只沿用当前原生入口，不恢复旧 Shell |
| [character-pipeline-blackboard](../openspec/specs/character-pipeline-blackboard/spec.md) | Gameplay Blackboard 在 CharacterSimulationState/compiled slots 内；仍有旧 authority/syncPolicy 等文本 | 不是动画实例 Blackboard；不按它扩建网络同步或复活旧字段，不把新动画状态塞入 Gameplay |
| [refactor-character-pose-graph-architecture](../openspec/changes/refactor-character-pose-graph-architecture/proposal.md) 与 execution | 已撤回原生 FlowCanvas 替换 Pose runtime；保留 Native/Job、根事务、作者图和迁移；实际审查/实施历史不等于本次完成 | 本任务不重开该撤回计划。新增事件输入域与替换 Pose runtime 是不同决定；共享参数/类型文件有交叉需精确归属 |
| [integrate-pose-flowcanvas-editor-preview](../openspec/changes/integrate-pose-flowcanvas-editor-preview/proposal.md) 与 design | UE 式图分工、独立动画编译、直接资源 Player、Input Contract、Slot/Timeline、曲线与 Document；明确不执行 Pose 作者图 | 变量输入/类型端口、Document 和原生 UI 是交叉点。本任务只提供事件图生产者与消费接口，不接管整个作者改革 |
| [unify-skill-authoring-data-model](../openspec/changes/unify-skill-authoring-data-model/proposal.md) 与 design | 只抽共同节点参数/规则，保留原生拓扑；不改 runtime、不自行决定 Document 版本 | 可复用已抽共同定义；不得改其范围为通用事件编译器，也不借本任务迁移 Skill |
| [integrate-native-fsm-skill-authoring](../openspec/changes/integrate-native-fsm-skill-authoring/proposal.md) | 原生 FSM 作者、既有 IR/Program，计划 Document v8 与完整资产事务 | 与 EventGraph 图种扩展共享版本/目录所有权。等待实际已发布合同，不能重复定义 v8 或恢复旧 reader |

触发窗口说明：此前 `openspec-update-change` 针对参数/曲线的检查仅为只读，未得到逐 artifact 确认，没有写入 proposal/design/tasks/spec。那份“只外部 Get、不给作者 Set”的拟议方向不是本次已批准的限制。本轮也没有对旧 change 写入补丁。

本任务完成的是把冲突定位并提出正式接法。未来如需修改上述 OpenSpec 文档，按用户明确授权及各文档所有权处理；本轮不擅自替其它规划窗口对账写回。

## 5. 资产与验证边界

`LocomotionFullBodyPoseGraph.asset` 当前静态检索得到 43 个不同参数名、473 个序列化声明出现位置；包含 41 个 BlendShape 名及 `animation.action-weight`、`animation.foot-placement-weight`。这证明不能把出现次数当独立变量数量，也不能从 `Usage.Control` 推导新事件图写权限。

以 `CharacterAnimationBlendSpaceAsset.cs.meta` 的脚本 GUID `49be86cd214744ad81b3f554089e3b44` 搜索 `Assets/Configs/**/*.asset` 未命中。未检查其它目录是否有此类资产，也未通过 Unity 确认可运行资源；不把“有 BlendSpace runtime 类型”描述为“Corin 已接 BlendSpace”。

本轮只读源文件和现行/活跃文档，没有运行 Unity、编译、Build、Play 或测试。所有时间、事务、输入连线和节点执行结论来自源码；未来端到端条件见主方案第 13 节。

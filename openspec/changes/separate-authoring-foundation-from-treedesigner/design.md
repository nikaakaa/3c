## Context

本提案当前处于文档阶段。2026-09-19 已统一正文与场景：旧图和旧 UI 的功能要求迁入原生编辑入口，场景正文不再要求创建或运行旧实现；现行黑板 fact projection 条款完整保留。本次不代表代码迁移已经完成。

动机见 proposal.md。这里区分四种不同的东西：作者保存的图与变量、项目定义的业务规则、编辑器呈现、每次运行产生的状态。当前混乱主要来自它们共同使用 TreeDesigner 的目录、类型和程序集名字，不能按目录整块删除。

### 当前真实链路

以下路径以 3cDemo/Client/3C_Client/Assets/GameScripts/Main 为基准：

| 入口或模块 | 当前承担的职责 | 本次处理 |
| --- | --- | --- |
| Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillFlowGraph.cs、BtsmtlSkillNativeStateMachine.cs | FlowCanvas 技能图、NodeCanvas FSM，保存作者结构与参数 | 保留原生数据；迁移其依赖的自有合同 |
| Editor/CharacterSimulation/Compilation/Skills/GameplayAbilityAuthoringCompilationModel.cs、BtsmtlSkillGraphCompiler.cs | 直接读取原生技能图并形成正式执行内容 | 继续直接编译原生图，不增加 BaseGraph 转换 |
| Runtime/Character/Pipeline/Animation/Contracts/Pose/CharacterPresentationPoseGraphAsset.cs、CharacterPoseCanvasGraph.cs | Pose 图、子图和领域状态机数据 | 保留当前资产和原生运行实例 |
| Runtime/BTSMTL/TreeDesigner/Scripts/ExposedProperty/ExposedProperty.cs | 混放有效的 scope、lifetime、input binding、fact projection 与旧 BaseExposedProperty | 拆出有效定义，删除失去消费者的旧变量实现 |
| Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillBlackboardDeclaration.cs | 通过 VariableId 给原生变量补充项目作用域和生命周期 | 保留 ID 关联，引用独立黑板合同 |
| Runtime/BTSMTL/TreeDesigner/Scripts/Authoring | 字段、逻辑端口、能力、文档投影和 mutation 合同 | 分开领域规则、框架适配和编辑描述 |
| Runtime/BTSMTL/TreeDesigner/Editor/Scripts/View/GraphAuthoringProjectionCanvas.cs | 同一文件中既有正在使用的绑定/剪贴板合同，也有旧画布 | 先拆出有效合同，再删除旧渲染实现 |
| Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.cs | 原生 GraphEditor 加自研 Details、Navigator、Selection、Undo | 保留这些作者能力，移除 TreeDesigner 依赖 |
| Runtime/BTSMTL/Timeline/Scripts/TimelineData.cs、Tree/TimelinePlaybackTreeContracts.cs | 正式 Timeline 内容与仍接收 BaseGraph 的旧调用合同 | 保留内容；调用身份不再携带整张旧图 |
| Runtime/Character/Pipeline/Graph/ActionRuntimeNodes.cs | 旧解释器节点，部分入口已拒绝正式执行 | 迁出仍有效业务值后删除失去消费者的节点 |

Simulation/Core 已有独立执行内容、Provider 和分型黑板槽位，不能因为目录清理把它重新接到 Unity 图或编辑器。Pose 的资源准备也不是废弃 IR 编译器，仍要保留。

### 两个容易混淆的黑板维度

- 数据归谁管：Character State、Input/TargetData、Attributes/GameplayTags、Equipment 等由各领域 Provider 提供；Skill Local 属于技能内部变量。一个面板展示它们，不代表一个字典保存所有真值。
- 一个局部值活多久：Graph、State、ActionInstance、Character、Frame 等声明规则决定寻址和重置边界，由运行时槽位与 activation/generation 实现。

FlowCanvas/NodeCanvas 原生 Variable 提供变量身份、名字、类型和默认值；项目自己的声明补充 scope、lifetime、authority、sync、input binding、fact projection。两部分通过稳定 VariableId 对应。已有显式 InputDerived 投射继续按正式 input-to-state binding 工作；这不授权把所有 Provider 数据镜像为可写变量。

## Goals / Non-Goals

**Goals:**

- 独立合同只表达领域数据与合法操作，不需要旧画布、旧节点执行器或 GraphEditor 就能被消费者引用。
- 编辑器、C# authoring 和编译准备对同一字段、端口、引用得到相同业务结论。
- 删除旧类型之前，其每个有效消费者、合法内容和作者操作都有明确的现有正式归宿。

**Non-Goals:**

- 不把 FlowCanvas/NodeCanvas 再包装成一套通用可写图，不迁移为另一种资产格式。
- 不改变 Skill 编译执行、Pose 原生图求值、Timeline 直接运行的领域选择。
- 不处理 Motion 累计、Pose pending/commit、Timeline Marker 等其他任务的运行修复。
- 不重构已工作的 typed slot/address、generation、provenance、变量重置或 Provider 算法；运行消费者只调整迁出的作者类型引用。不得把原生框架用于保存变量的合法容器当成旧运行字典删除。
- 不清理与 TreeDesigner 退出无关的 Pose 调参、Foot IK、网络模型或 AI 系统，不恢复退役 Agent Document/Exporter/Reconciler 协议。

## Decisions

### 1. 结构继续归原生图，业务规则独立于框架对象

采用现有 FlowGraph/FSM 的节点、连接和序列化格式。节点字段、端口稳定 ID、值类型、方向、执行域和连接约束仍由项目领域定义。FlowCanvas 的 Port/BinderConnection 是这些定义在框架中的对象和持久化连接，不是业务规则的第二来源。

例如技能分支新增一个有稳定 ID 的出口：领域定义决定该出口含义和可接目标；适配层向 FlowCanvas 注册端口；作者连接后由原生 Connection 保存端点 ID；编译器通过同一语义读取连接。显示顺序、颜色或重绘不改变端点身份。实际值仍只在当前节点 payload 中保存，不复制进 metadata。

可选做法是另建自有图 DTO，再向 FlowCanvas 投影。它便于更换编辑框架，但会引入资产格式迁移和两个结构之间的一致性成本；本次目标是整理已有自研业务，采用保留原生结构、抽出规则的方案，接受资产仍依赖框架类型。

### 2. 共享合同与领域实现分开，编辑展示放到 Editor

目标目录和程序集：

| 模块 | 位置与命名 | 输入、输出与依赖 |
| --- | --- | --- |
| 共享作者合同 | Runtime/BTSMTL/Authoring/Contracts，BTSMTL.Authoring.Contracts | 字段/端口/能力/类型化变更等公共合同；不引用 TreeDesigner、FlowCanvas、NodeCanvas 或 UnityEditor，不拥有具体 Skill/Pose 节点目录 |
| 黑板作者合同 | 上述模块的 Blackboard 子目录，BTSMTL.Authoring.Blackboard 命名空间 | 自有声明、scope/lifetime 合法组合、输入绑定、事实投射；允许必要 Unity 序列化值类型，不引入 runtime 字典或窗口上下文 |
| Skill/Pose 等领域定义与适配 | 保持当前各领域目录与 owner | 领域负责业务语义；原生节点适配字段和端口对象；准备/编译仍在现有领域模块 |
| 通用编辑集成 | Editor/GraphAuthoring，BTSMTL.Authoring.Editor 程序集 | Details/Navigator/Selection/Undo/Clipboard 等共享合同和呈现；具体领域实现仍由 ThirdPersonClient.Editor 装配，通用层不反向引用它 |
| 可移植执行 | 现有 Simulation/Core 和 Timeline 运行模块 | 接收现有正式执行内容与调用合同，不引用上述编辑程序集或原生图对象 |

引用方向是领域实现引用公共合同、领域 Editor 引用通用 Editor 和自己的领域；通用 Editor 通过合同接收领域提供者。新程序集只为这些明确边界建立，不为每个 enum 建程序集。

GraphAuthoringCapabilityCatalog 中字段类型、端口规则等迁入公共合同；菜单标题、颜色、Details renderer 等移入编辑描述。CharacterPoseNodeDefinitionModule 等原领域继续唯一声明执行域、字段合法性和端口形状，不能从 UI descriptor 的字符串反推运行语义。混在同一文件的类型按职责拆分。

只移动目录并保留 TreeDesigner 类型名，迁移成本更低，但今后仍无法从引用看清依赖；集中重写所有节点定义则会扩大业务变更。本方案迁移有效公共合同，保留领域实现，清理对应旧名。

### 3. 保留分域黑板与局部声明，删除旧变量容器

保留现有 Provider 查询以及 BtsmtlSkillBlackboardDeclaration 与原生 Variable 的 ID 关联。独立合同负责声明规则，现有运行时负责 typed slot、owner 与 generation。面板继续区分可写局部变量和只读外部来源，拖拽生成正式领域查询或变量引用。

scope/lifetime 等序列化声明与 authority/sync 等正式编译语义按当前真实来源迁移，不因列出这些概念就给原生 Variable、declaration 和编译内容各补一份字段。字段迁移清单必须写明现有来源与实际消费者。现行 `Blackboard declaration 必须显式声明 fact projection` 和 `Blackboard 写入 provenance 必须支撑事实投影` 两项要求不修改、不删除：Bool/Frame/SyncFact、WindowType/WindowId/Digest、ActionInstance/EventId、同帧不同动作独立 candidate 及 Model Egress 的 fact-kind coverage 限制继续完整生效。InputDerived 的合法显式投射也保留，不等同于复制所有 Provider 数据。

ExposedProperty.cs 中通用声明规则迁出；面向 BaseGraph/BaseExposedProperty 的枚举、查找和校验实现随旧调用方退役。ActionTargetSnapshot.cs 等混合文件中的正式业务值必须保留，只去掉旧 exposed-property 适配。

直接用原生黑板统一所有来源会简化面板开发，但会丢失属性/标签和输入等数据的领域所有权与运行寿命。完全重写变量容器又会重新迁移现有资产。因此保留原生变量加项目声明的现有组合，消除旧容器，不建立镜像字典。

### 4. 原生编辑器负责画布，项目保留业务编辑功能

现有 GraphEditor 继续负责画布和框架交互；本项目保留领域 Provider、资源选择、专用节点参数、Details、Navigator、多资产 Undo、C# 显式导出/生成和只读 diagnostics。Pose FSM 的临时编辑投影只读取/修改真实 Pose owner，不保存第二份 FSM，更不参与运行。

具体处理分三类：

| 保留 | 迁出后保留 | 删除 |
| --- | --- | --- |
| 原生 Skill/FSM/Pose 图编辑入口、Slate Timeline | GraphAuthoring 共享合同、领域黑板辅助、仍使用的 Details/Navigator/绑定/Undo/Clipboard | 旧 TreeWindow/BaseTreeWindow/TreeView、旧 GraphView/NodeView/PortView、旧菜单/Browser/Inspector 打开路径 |
| 当前 C# authoring API 和资源选择 | 混合文件中的有效 TargetData/声明/引用合同 | 无消费者的 BaseGraph/BaseTree/BaseTreeAsset/BaseNode/PropertyPort/BaseExposedProperty 及其解释器、编译重载和适配 |

“只用插件默认 Inspector”会丢失业务上下文和跨资产编辑；保留旧画布又会继续维护两套窗口。本方案保留自研业务编辑功能，退出旧画布。重操作继续显式触发，不移入 OnInspectorGUI 或重绘回调。

### 5. Timeline 调用传必要身份，执行方式保持领域所有权

正式技能调用继续通过现有 AbilityTimelineInvocationSource 与 provenance 提供能力、Action、图和调用身份；有真实非 Skill 消费者时保留其 typed 调用身份。播放服务只接收所需内容、调用身份、上下文和能力，不接收用于“顺便拿一个 ID”的 BaseGraph。

Timeline TreeClip 继续以精确图身份/revision 调用既有技能服务；播放、Clip/cycle、ActionInstance、Step/generation 等不能因删除旧参数而丢失。TimelineData、portable 内容、调度顺序、Marker、Commit/Discard 不随此次抽离重写。

身份按已有调用阶段传递：内容准备读取内容与依赖版本；播放请求携带真实调用来源，播放 owner 建立播放身份；进入 TreeClip 时才传递对应 Clip/cycle 与精确图调用身份。不要求普通 Timeline Prepare 提前拥有尚未创建的播放或 Clip 身份，也不把各阶段字段堆成新通用上下文。

保留 BaseGraph 参数改动少，但 Timeline 仍被旧图程序集牵住；把所有调用改成假技能则破坏独立 Timeline 业务。采用现有正式身份合同承接真实调用方，无消费者的旧 overload 直接删除。

### 6. 先迁移合法内容和引用，再完整删除旧实现

原生资产继续使用当前序列化器和物理所有权。私有内容仍随 owner 管理，共享资产仍显式引用；不因“抽出底层”批量把原生子资产改成旧 inline BaseTree，或强制抽取成共享资源。

移动现有脚本保留对应 .meta GUID。自有类型迁名涉及 SerializeReference、FullSerializer 或程序集限定名时，建立精确旧类型到新类型映射，通过正式序列化/作者操作一次性更新仓库内容和声明引用。未知类型或无法保持业务语义的旧节点必须报告具体资产和冲突，不能猜测替换或直接删除合法内容。仅本次待删除模型的旧资产需要内容迁移，已是原生图的资产不重建。

保持 Graph/Node/Port/Variable/Declaration/Timeline 的业务 ID、字段值、默认值、连接与所有权；格式或类型名改变产生的新内容 revision 按正式发布流程更新，不伪造旧 hash。C# authoring 生成内容也通过正式入口更新。一次性迁移在删除旧程序集前完成，交付结果不留兼容类型、MovedFrom、空程序集、双读或运行时迁移器。

保留旧命名可减少序列化改动，但留下长期概念负担；一边读旧一边写新能渐进上线，但形成用户明确不要的双路径。本方案做成同一批完整迁移，代价是受影响内容必须一起落地。

## Risks / Trade-offs

- 有效编辑合同藏在旧渲染文件中 → 按实际类型和消费者迁出，不按目录名直接删除；GraphAuthoringProjectionCanvas 是明确的拆文件对象。
- 第三方原生 Port 仍承载框架执行回调 → Skill authoring 继续拒绝原生执行，Pose 保留其正式原生运行；不能全局改写 FlowCanvas Port 执行行为。
- 调用身份删减导致变量串实例或 TreeClip 丢 provenance → 迁移保留既有 owner、activation、generation、clip/cycle 与精确图版本；缺失即报错，不补默认上下文。
- 程序集和类型迁名造成序列化丢字段 → 使用精确映射和正式序列化操作；不做 YAML 正则替换，不以重新生成整角色覆盖当前内容。
- 工作区正在有其他修改 → 本轮只新增此 change 目录。实施时按文件确认修改归属；若目标与正在修改的正确逻辑重合，明确指出具体冲突，由用户决策，不覆盖或另开 worktree。
- 当前 specs 与未归档变更口径不一致 → 以下表格明确替代关系；不以旧条款为理由重建旧图，也不自动归档其他任务。

## Migration Plan

1. 在主工作区整理本次类型、消费者和序列化引用清单，标出合法旧内容和当前其他任务占用的文件。
2. 迁出公共作者与黑板合同，领域业务定义与编辑描述分开；同步更新所有正式消费者，保持每一步只有一份真实定义。
3. 迁移原生编辑器仍需的自研功能和 Timeline 调用合同，移除无消费者的旧 adapter/overload。
4. 对仍含旧模型的合法内容执行一次性正式迁移；迁移类型引用及生成代码，删除迁移用途代码，不保留运行兼容层。
5. 删除旧窗口、画布、旧图模型/执行链及孤立资源、菜单、程序集引用与配置；保留第三方框架和所有有效领域数据。
6. 同步主规范正文与 Purpose，调整下列重叠变更的过时设计约束，不替其他任务标记完成或归档。

实施按完整职责切片做中文小步提交，每个切片同时完成消费者更新和对应旧路径删除。若切片无法保持当前合法内容，停止该切片并报告冲突；需要撤销时只撤销本次完整切片，不回退用户或其他任务的修改。此次 proposal 阶段不执行这些步骤，不增加测试或验证任务。

## 现行规范对照与冲突处理

对照的是当前工作区主 spec，包括已存在的未提交修改；不是仅依据 archive 历史。

| 规范 | 现行要求与本次关系 | 本次处理 |
| --- | --- | --- |
| btsmtl-graph-core | 强制 BaseGraph/PropertyPort/BaseTreeAsset 唯一链路、私有 BaseTree 内联、旧图克隆与初始化入口 | 删除这些实现约束，改为当前原生图、唯一结构、稳定身份、私有/共享所有权和领域运行隔离 |
| graph-authoring-domain-framework | 强制从旧 BTSMTL UI 原地抽象；要求 Pose 编译为 IR；还保留退役 Agent metadata 条款 | 改为公共合同与原生编辑集成；Skill 编译、Pose 原生求值；删除 Agent 协议要求 |
| graph-authoring-editor-shell | 强制旧 GraphAuthoringCanvasView/GraphView 和固定旧窗口迁移方式 | 改为现有原生 GraphEditor 加共享业务面板；保留布局隔离、mutation owner 和只读诊断 |
| btsmtl-tree-inspector-information-architecture | 以 TreeWindow/TreeView 实现定义 Data/Details、模式与模块归属 | 替换为与窗口实现无关的信息分类、业务 Details 与只读观察边界 |
| character-pipeline-blackboard | BaseExposedProperty 被指定为唯一作者数据，目录直接读取旧类型 | 改为原生 Variable 加独立声明，通过稳定 ID 关联；保留 Provider 所有权、寻址和生命周期；fact projection 与 provenance 原条款不做删除或简写替代 |
| btsmtl-skill-authoring-model | 要求同义适配共用定义，但未明确哪些旧适配退出 | 明确原生节点/端口适配保留，旧节点无消费者时删除；保留现有合法技能数据和冲突上报要求 |
| btsmtl-timeline-direct-runtime | 当前直接运行、精确 TreeClip 身份和提交协议与目标一致 | 只新增调用合同脱离旧图类型的要求，不重写调度和运行协议 |
| unity-simulation-assembly-ownership | 普通程序集迁移要求 namespace/type 完全不变，与本次清理 TreeDesigner 名称冲突 | 只为本次自有作者类型迁名增加精确内容迁移规则；其它 Unity 组件移动仍保留原规则 |
| character-presentation-pose-graph、native-flowcanvas-pose-runtime、character-domain-runtime、character-csharp-authoring | 已确定 Pose/Skill/Timeline 的不同执行方式和正式 C# 作者路径 | 保持，不借本次清理恢复旧 IR 或额外同步协议 |

重叠未归档变更包括 integrate-native-fsm-skill-authoring、integrate-pose-flowcanvas-editor-preview、refine-pose-graph-readonly-blackboard、design-btsmtl-authoring-runtime-workbench。本次承接它们已迁移的原生资产与编辑能力，并替代旧图/旧 UI 仍需长期保留的约束；各自未完成的运行修复和领域功能仍归原任务。实施时只调整发生冲突的条款，不能整份重写或自动归档。

本目录的 delta 处理上述规范性 Requirements。项目校验要求 MODIFIED 保留现行 Requirement 和 Scenario 标题以追踪既有行为，因此部分标题仍含 TreeWindow、RootTree 或 Projection；这些标题仅用于精确匹配，具体对象与行为以已改写的正文为准，不构成保留旧类型的要求。重复新场景合并到对应原场景，原有业务约束继续保留。OpenSpec delta 不会更新现有 Purpose；btsmtl-graph-core 等仍描述旧实现的 Purpose，必须在实际规范合并时同步改写。本轮不覆盖当前已有修改的主 spec，因此主规范在本提案获实施并合并前仍存在表中已指出的不一致。

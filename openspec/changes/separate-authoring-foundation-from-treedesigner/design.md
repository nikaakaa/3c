## Context

2026-09-27 范围更新：把真正独立的公共数据定义和编辑能力从 TreeDesigner 中分出，具体运行实现各归其位，删除被原生入口替代的旧 UI。目标包含模块依赖归位，不止是在 TreeDesigner 内拆小文件。完整退出旧图执行类型、重写技能/Pose/Timeline 运行逻辑仍不属于本次。当前只更新文档，未开始代码整理。

### 已确认的代码事实

以下路径以 `3cDemo/Client/3C_Client/Assets/GameScripts/Main` 为基准。

| 对象 | 当前职责 | 本次处理 |
| --- | --- | --- |
| Runtime/BTSMTL/TreeDesigner/Scripts/ExposedProperty/ExposedProperty.cs | 声明规则与旧变量实现混放 | 拆出仍被引用的 scope/lifetime、InputBinding、fact projection 等定义，行为不变 |
| Runtime/BTSMTL/TreeDesigner/Scripts/Authoring/GraphAuthoringCapabilityCatalog.cs | 字段、端口、能力与部分显示描述 | 拆分业务定义与显示内容，不重建节点体系或 metadata 框架 |
| Runtime/BTSMTL/TreeDesigner/Scripts/PropertyPort/PropertyPort.cs | 类型、身份、值、源连接及旧节点缓存 | 逐项确认消费者和语义去向，不预先删除或认定完全被替代 |
| Runtime/BTSMTL/TreeDesigner/Editor/Scripts/View/GraphAuthoringProjectionCanvas.cs | 有效编辑交接对象与旧画布混放 | 迁出当前入口仍用的部分，再删旧画布 |
| Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.cs | 原生编辑器与 Details、Navigator、Selection、Undo 集成 | 保留行为，只调整迁出类型的引用 |
| Runtime/BTSMTL/Timeline/Scripts/Tree/TimelinePlaybackTreeContracts.cs | 仍有 BaseGraph 参数 | 记录真实调用者；本次不以删除参数为目标重写调用模型 |

2026-09-27 补查得到以下具体边界：

- `IPipelineBlackboardRuntimeAccess` 的签名包含 `BaseGraph`、`BaseExposedProperty` 和 `StateMachineExecutionScope`，属于具体图运行接入，不是可以直接迁入公共层的接口。
- `PipelineBlackboardVariableReference` 的数据字段可以独立，但构造函数接收 `BaseExposedProperty`；`PipelineBlackboardVariablePolicy` 也混有接收该类型的校验。必须按成员拆开数据、纯规则与具体对象读取，不能把整个类型连依赖搬入公共层。
- `BaseTreeView.cs` 中的 `GraphAuthoringCanvasView` 继承 `UnityEditor.Experimental.GraphView.GraphView`。`GraphAuthoringProjectionCanvas.cs` 和 `GraphAuthoringStateMachineProjection.cs` 同时包含共享绑定合同及这个旧画布的视图/partial 实现。
- `CharacterPoseCanvasBinding` 已通过 `NodeCanvas.Editor.GraphEditor` 使用原生画布，同时仍读取上述绑定合同。绑定合同的消费者不能证明旧 GraphView 仍需要保留。
- `RuntimeDebugSourceNavigator` 仍有打开 `BaseTreeWindow` 的代码，`BaseTreeInspector`、`TreeWindowUtility` 仍有旧资产打开入口。这些引用是否仍由有效业务触发，要沿调用及资产记录判定，不能仅凭类名删除或保留。

技能原生图经过编译运行，Pose 使用角色独立原生实例，Timeline 由自己的调度器执行。这些不同领域不等于分裂路径。类型或接口仍存在，也不证明存在另一套实际运行链。

### 值端口和求值的现状

| 能力 | 代码证据 | 当前结论 |
| --- | --- | --- |
| 作者值连线编译 | Editor/CharacterSimulation/Compilation/Skills/BtsmtlSkillGraphFlowEmitter.cs 的 EmitEdges | ValueOutput 转为 Value 边，保留源/目标操作和端口身份 |
| 有类型输入绑定 | Runtime/Simulation/Core/Program/OperationValuePortContracts.cs；Fixed/Execution/GameplayAbilityExecutionLayout.cs，Float32 有对应实现 | 执行布局按边建立 CompiledValueInputBinding，包含端口序号、值类型、操作来源或常量索引 |
| 递归求值 | Runtime/Simulation/Core/Fixed/Execution/FixedValueRuntime.cs 的 Evaluate/ReadInputs；Float32ValueRuntime 同类方法 | 按依赖递归取值；ValueStack 检测循环，不是结果缓存 |
| 脏标记与结果复用 | 上述两套 ValueRuntime；旧 BaseNode.InputValue 和 ValueNode.OutputValue | 已检查基础链未发现通用依赖失效缓存；共享上游可能重复求值，不把其他系统的 dirty 当成此能力 |
| 短路 | TreeDesigner/Scripts/Node/Value/Operate/OrNode.cs；两套 ValueRuntime | 旧 OR 未定义 TREERUNNER_DEBUG 时按需读右侧；编译链先 ReadInputs 再执行 OR，右侧仍求值。编译 AND 同样先读取全部输入 |
| 类型和节点覆盖 | Editor/CharacterSimulation/Compilation/Skills/BtsmtlSkillFlowLeafEmitter.cs 的 Kind/Emit | 只支持明确映射的类型与节点；不能把旧 PropertyPort<T> 的可表达范围视为自动完整迁入 |

这些结论来自源码检查，不是性能测量或全节点等价证明。尚未完成旧节点、转换规则、求值顺序、共享依赖和运行实例状态的全量对应。此次整理不得删除尚未证明有完整去向的语义，也不把脏缓存、短路修复、公共子表达式合并或执行器重写列为已交付。

## Goals / Non-Goals

**Goals:**

- 公共定义移出 TreeDesigner，具体实现单向引用公共定义；混合文件按实际成员依赖拆开，必要引用随之更新。
- 保留类型、端口身份、值来源、默认值、连接关系、求值行为及当前作者功能。
- 删除旧 UI 及其确认无人使用的附属代码，不留兼容 UI 或第二写入口。
- 运行热路径严格 0 GC；不通过目录整理引入包装分配、临时集合、装箱、闭包或字符串构造。

**Non-Goals:**

- 不整体退出 BaseGraph/BaseNode/PropertyPort，不把类名作为删除依据。
- 不建立新的通用图、求值接口、端口生成系统、metadata 系统或编译层。
- 不更改技能和 Pose 执行方式、变量寻址、generation、provenance、Provider、Timeline 调度与调用语义。
- 不在本次顺带补缓存、短路或优化整个编译图，不据此宣称既有运行链 0 GC 或语义完整。

## Decisions

### 1. 公共定义独立，具体实现引用公共定义

公共声明与描述统一归入 `Runtime/BTSMTL/Authoring`，由独立 `BTSMTL.Authoring` 程序集拥有，按 `Blackboard` 和 `Graph` 分目录。该程序集只承载现有独立数据与规则，不引用 TreeDesigner、具体领域运行器或 Editor；必要 Unity 值类型和序列化能力不等于图运行依赖。TreeDesigner、领域作者定义和各自消费者按实际用途引用公共层，不为方便让无需求的 Simulation/Core 引入 Unity 作者层。

共享编辑合同与有效面板统一归入 `Editor/GraphAuthoring`，由独立 `BTSMTL.Authoring.Editor` 程序集拥有，不能通过父目录默认进入高层 Character Editor 程序集。公共编辑集成可引用公共定义及实际需要的原生编辑框架，不反向引用 Character Editor、Timeline Editor 或旧窗口。仍需读取 `PropertyPort`、`BaseGraph` 等具体类型的编辑服务留在对应领域 Editor；领域 Editor 依赖公共编辑集成。若某面板仍包含领域读取，先使用已有领域合同分离，不能把整个领域依赖塞进公共面板。

公共类型使用 `BTSMTL.Authoring` 下与职责一致的命名空间；具体类型留在其领域。迁名范围与资产处理逐类型列明，不保留旧 namespace 别名、重复模型或接口包装。输入是现有定义、实现和直接消费者，输出是同一份业务数据及实现的正确归属与单向引用。

| 方案 | 作者与业务收益 | 成本与边界 |
| --- | --- | --- |
| TreeDesigner 内按文件整理 | 阅读和定位更直接，程序集及资产身份变化少 | 共享消费者仍依赖 TreeDesigner，不能交付本次要求的公共定义独立 |
| 公共定义移出，具体实现单向依赖 | 领域只为需要的定义建立依赖，后续维护不必进入旧图运行与窗口代码 | 需要完整处理程序集和实际受影响的序列化引用；本次按此方案实施 |

不把完全消除所有旧图类型作为第三个整理方案；那需要重新决定运行合同，属于不同业务范围。

### 1.1 混合类型按成员拆分，不按名字判断抽象

`PipelineBlackboardVariableReference` 在公共层保留原序列化字段与值语义，以现有身份字段构造引用数据；从 `BaseExposedProperty` 提取这些字段的代码归具体声明实现或现有领域辅助。直接调用者统一改用唯一正式创建入口，删除旧耦合构造入口，不新增第二份 reference 或反射转换器。该调整只改变数据从哪里读取，不改变 owner、身份或类型匹配规则。

scope/lifetime 组合等只依赖公共值的规则随公共定义迁出；接收 `BaseExposedProperty` 的声明校验和事实投射对象读取留在具体实现。`IPipelineBlackboardRuntimeAccess` 归 `TreeDesigner/Scripts/Blackboard` 的运行接入文件，继续引用公共值定义和原图类型，不以改签名抽象整个运行器。能力目录与端口描述同样按成员检查，已有领域定义继续提供业务规则，不生成另一套 metadata 或节点注册表。

### 2. 旧 UI 删除，有效编辑功能保留

FlowCanvas/NodeCanvas 原生画布及 Slate 保留。Details、Navigator、资源选择、Selection、Clipboard、Undo、C# authoring 和只读观察按实际消费者迁出。旧 TreeWindow、旧 GraphView、独有节点视图、菜单和回调在这些依赖迁出后删除。框架自身的 UI 类型不在删除范围。

只从旧画布文件迁出有效的绑定合同、剪贴板接口、状态机页面合同和被正式入口使用的数据。`GraphAuthoringCanvasView`、`GraphAuthoringProjected*View` 及其 GraphView partial 实现不作为公共编辑能力搬迁保留；先解除有效消费者，再连同旧画布资源清理。原生框架的节点与端口类型保持原归属，不和这些旧视图混为一类。

旧窗口文件中的注册表、打开入口、资源加载和回调必须分开记录。有效的 Inspector、资产打开、调试定位和下钻请求接入对应正式入口；确实无业务消费者的旧入口删除。若合法内容仍只能由旧窗口编辑且缺少等价去向，停止该删除切片并列出缺失功能，不删入口隐藏内容，不新建兼容窗口。

只用插件默认面板会丢失项目业务编辑；保留旧窗口又继续维护两个入口。本次保留有效面板实现、删除已替代画布，不重做交互或业务规则。OnInspectorGUI、重绘和选择回调不得新增编译、构建或资源加工。

### 3. 删除以消费者和语义证据为准

每个切片先列：旧类型/文件、实际职责、代码及注册消费者、序列化或生成引用、目标位置、保留字段与行为、删除对象。没有消费者且没有合法资产引用的附属代码直接删除。仍承载有效语义的代码保留或原样归位，这不是兼容层。

若删除旧 UI 暴露出真实运行依赖，且解除依赖需要改变求值或业务合同，停止受影响切片，报告具体引用、缺失语义以及保留当前实现与另行修改的业务取舍，由用户决定。不复制实现、不做临时桥接，也不把未核实称作“已废弃”。

### 4. 资产和调用只处理直接影响

移动脚本保留 .meta GUID。类型或程序集迁名确实影响序列化时，以精确映射和正式序列化操作更新该批引用，保留参数、端口、连线、VariableId 与所有权。无影响资产不动，不重建整图，不将旧合法内容强制转换成新格式。无法无损处理时报告具体资产，不加 MovedFrom、兼容类型或运行时迁移器。

Timeline 仅更新本次迁出类型的引用；仍有真实消费者的 BaseGraph 参数按现有语义保留，不强制变成新上下文。彻底解除旧类型依赖可能便于以后独立使用，但需要明确全部调用语义，不能作为分文件的隐含成本。

### 5. 0 GC 是实施约束，不是现状背书

此次触及的运行热路径必须 0 GC。需要的存储沿用正式准备阶段的既有容量与生命周期，不在每帧求值中创建容器、扩容、装箱、创建委托闭包或构造诊断字符串。编辑器显式操作与运行热路径分开说明，不把 Editor 分配数据混为运行指标。

现有 ValueRuntime 可见按深度扩展输入缓冲、List/HashSet 存储及诊断字符串构造，不能仅凭类型名称断言每帧必然分配或已经 0 GC。若本切片触及的实际调用存在分配且需要算法或容量合同变更才能消除，明确报出并交用户决定范围，不能允许分配后宣称完成。无相关证据时不标记性能达标。

## Migration Plan

1. 形成当前切片的类型、消费者、语义和资产引用清单。
2. 按公共定义、具体运行实现和编辑功能分责；公共定义移出 TreeDesigner，同步直接引用、程序集与必要资产。
3. 删除旧 UI 及确认无消费者的附属代码；有业务缺口的删除项单独报告。
4. 保留当前原生图、编译求值、变量机制与 Timeline 运行，交付实际保留/归位/删除清单及未决冲突。
5. 按完整职责切片中文小步提交，不新增测试或验证任务，不建立 worktree，不覆盖其他任务的正确修改。

## 现行规范对照与冲突处理

| 现行规范 | 本次处理与仍存在的边界 |
| --- | --- |
| btsmtl-graph-core | 新增整理和入口删除边界，修订“不新增 Graph 分裂路径”中的共享旧画布要求；不整体删除 BaseGraph 结构、克隆和初始化合同，剩余旧底座适用范围仍需按消费者裁决 |
| graph-authoring-domain-framework | delta 修订共享框架、旧 UI 提取、Canvas 与状态机表面的实现约束，复用正式原生交互和项目编辑合同。仍存在的 Pose IR/编译器表述属于运行历史冲突，见下文，不据此恢复旧执行链 |
| graph-authoring-editor-shell、btsmtl-tree-inspector-information-architecture | delta 补齐“Graph Authoring Editor Shell必须提供可组合工作区区域”的修订，解除强制使用同一旧 GraphAuthoringCanvasView 的遗漏；同时保留 Data/Details、导航、Undo 和只读观察。Scenario 标题保留用于匹配，正文指向当前正式入口 |
| character-pipeline-blackboard | 只新增声明归位与行为保留要求；原 fact projection、provenance、寻址与生命周期条款不删除或改写。旧 ExposedProperty 唯一表面说法不作为删除有效类型的依据 |
| btsmtl-skill-authoring-model | 新增语义保留与覆盖证据边界，撤回原生端口存在即等价的暗示 |
| btsmtl-timeline-direct-runtime | 撤回强制改写调用身份接口，限定直接引用更新 |
| unity-simulation-assembly-ownership | 公共 Runtime 与公共 Editor 建立真实单向程序集边界，具体图适配留在所属领域；只对本次列入清单的自有类型允许精确迁名，仍有有效类型的旧程序集保留 |
| native-flowcanvas-pose-runtime、character-presentation-pose-graph | 保持原生 Pose 实例和正式观察，不恢复 Pose IR 或独立预览执行器 |

本次不替其它 change 完成或归档，不沿用旧文档对它们“未归档”的状态判断。本轮只修改 change 与 delta，主规格在实施与正式同步前保持原状。下面这些历史冲突仍需明确指出：

- `btsmtl-graph-core` 的“BaseGraph 承载唯一图结构数据”及旧 asset 入口适用范围仍覆盖过宽；本次没有全量旧图消费者及合法资产迁移方案，不能借整理整体删除该运行合同。跨领域共享旧 Canvas 的要求已在本次 delta 中修订。
- `graph-authoring-domain-framework` 的“Authoring节点与Runtime执行描述必须分离”仍要求 Pose 编译为 IR 并拒绝直接执行作者图，与 `native-flowcanvas-pose-runtime` 的原生执行要求冲突；Capability 条款也仍含 Pose lowering/Compiler 描述。本次保留现有原生 Pose 执行及领域定义，不把解决历史运行合同算作文件归位成果。
- `character-pipeline-blackboard` 的 `BaseExposedProperty` 唯一声明表面说法不能被解释为所有公共黑板值定义都必须留在 TreeDesigner；本次不改变声明 owner 和存储实现，也不据此引入第二套声明。

本次直接涉及的旧画布约束由 delta 明确修订；上述更广的运行合同不允许实现者任选路径。若具体迁移触及语义冲突，列出对象、调用者和缺失去向交用户决定。

## 文件级实施方案

本节把方案落到现有文件。实施时先完成一个切片的生产者、消费者和旧入口，再进入下一个切片。迁移中的新文件只能承载已经存在的类型和行为，不建立第二套运行或编辑实现。

### 现有文件与目标归属

| 编号 | 当前文件 | 当前混合职责 | 实施后的归属 | 处理方式 |
| --- | --- | --- | --- | --- |
| A1 | `Runtime/BTSMTL/TreeDesigner/Scripts/ExposedProperty/ExposedProperty.cs` | 黑板公共数据、具体声明、对象读取、运行访问混合 | 公共值与纯规则进入 `Runtime/BTSMTL/Authoring/Blackboard`；具体声明留在 `TreeDesigner/Scripts/ExposedProperty`；运行接入归 `TreeDesigner/Scripts/Blackboard` | reference 数据与耦合构造职责拆开；`IPipelineBlackboardRuntimeAccess` 不进公共层，序列化迁名按精确清单处理 |
| A2 | `Runtime/BTSMTL/TreeDesigner/Scripts/ExposedProperty/ExposedProperty_Extension.cs`、`Scripts/Utility/ExposedPropertyUtility.cs` | 声明查找、类型映射和运行辅助 | 只依赖公共值的规则随 A1 迁出；具体对象读取、类型映射及声明校验留在 TreeDesigner 对应目录 | 按成员依赖分配，保持唯一方法与调用语义，不通过复制或反射消除引用 |
| B1 | `Runtime/BTSMTL/TreeDesigner/Scripts/Authoring/GraphAuthoringCapabilityCatalog.cs`、`GraphAuthoringDomainContracts.cs` | 字段、端口、能力、文档描述、命令和显示描述 | 独立定义进入 `Runtime/BTSMTL/Authoring/Graph` 下的 `FieldContracts.cs`、`PortContracts.cs`、`CapabilityCatalog.cs`、`DomainContracts.cs` | 公共定义闭包一起迁出；具体领域提供者保留原领域，Editor 实现不进入 Runtime，类型身份精确迁移 |
| B2 | `Runtime/BTSMTL/TreeDesigner/Scripts/PropertyPort/PropertyPort.cs`、`Scripts/Attribute/BaseAttributes.cs` | 运行端口、端口身份、连接状态和声明特性 | 原 Runtime 目录的 `PropertyPort` 与 `Attribute` | 不迁出运行语义；只清楚标明编辑器读取点，禁止把它们复制到 Editor |
| B3 | `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/Window/PropertyPortAuthoringService.cs` | 具体端口声明读取、编辑描述和连接查找 | `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/Authoring/PropertyPortAuthoringService.cs` | 属于具体图的 Editor 适配；读取 B2 并使用公共描述，不把 TreeDesigner 依赖带入共享 Editor |
| C1 | `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/View/GraphAuthoringProjectionCanvas.cs`、`GraphAuthoringStateMachineProjection.cs` | 有效绑定合同与旧 GraphView 视图/partial 实现混放 | 有效合同进入 `Editor/GraphAuthoring/Contracts`；旧视图进入 C5 删除清单 | 提取 Clipboard、Projection/StateMachine binding 和有效页面合同；不搬迁保留旧 Canvas 与 Projected View |
| C2 | `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/View/GraphAuthoringDetailsPresenter.cs`、`GraphAuthoringStateMachineDetailsPresenter.cs`、`GraphAuthoringNavigatorPresenter.cs`、`GraphAuthoringBottomDockPresenter.cs`、`GraphDataCatalog.cs` | 面板显示与数据源接入 | 独立面板进入 `Editor/GraphAuthoring/Details`、`Navigation`、`Catalog`；具体数据源留在领域 Editor | 按实际依赖提取，保留正式入口使用的行为；未被使用的附属显示直接清理 |
| C3 | `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/Window/GraphAuthoringEditorShell.cs`、`GraphAuthoringStateMachineContracts.cs`、`BtsmtlGraphAuthoringAdapters.cs`、`BtsmtlSharedAuthoringWorkspaceRegistry.cs`、`GraphAuthoringClipboardController.cs` | 公共编辑合同、具体适配、旧窗口装配 | 公共合同/交互进入 `Editor/GraphAuthoring/Contracts`、`Interaction`；具体图适配留领域 Editor；旧窗口装配进入 C5 | 不整文件迁移 Shell；逐项处理 Selection、Undo、Clipboard、资源选择和注册释放，解除有效能力对旧窗口的依赖 |
| C4 | `Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphWorkspace.cs`、`CharacterPoseCanvas*`、`CharacterPoseGraphAuthoringAdapter.cs` | 当前原生 Pose 编辑入口的实际消费者 | 原路径保留，引用 C1/C2/C3 的归位类型 | 这是共享集成文件，只允许集成人修改，不能由 A/B/C 各自改一份 |
| C5 | `Runtime/BTSMTL/TreeDesigner/Editor/Scripts/Window/BaseTreeWindow.cs`、`SubTreeWindow.cs`、`TreeBrowserWindow.cs`、`NodeReferenceWindow.cs`、相关 `TreeWindowUtility.cs` | 旧 TreeDesigner 窗口、浏览器和节点引用界面 | 删除确认无消费者的窗口和回调 | 必须先完成 C1-C4；任何仍被当前编辑入口使用的功能先迁出，不能整目录删除 |
| C6 | `Editor/CharacterPipeline/Diagnostics/RuntimeDebugSourceNavigator.cs`、TreeDesigner 的 `BaseTreeInspector.cs`、旧 `BaseTreeView.cs` 和窗口/画布资源 | 调试定位、资产打开、旧画布基类及资源 | 有效导航保留所属领域并接正式入口；无消费者的旧视图和资源删除 | 方法调用、静态注册、Inspector/菜单、UXML/USS 和资源字符串一并纳入清单；合法内容无去向则报告缺口 |

### 直接消费者和唯一修改者

| 消费者 | 使用内容 | 本次唯一修改者 |
| --- | --- | --- |
| `BaseGraphAuthoring`、`NestedGraphValidation`、`BtsmtlSkillGraph*` 编译器 | `BaseExposedProperty`、黑板字段、端口身份和图连接 | A/B 集成者 |
| `BtsmtlSharedGraphAuthoringAdapters`、`CharacterPipelineAuthoringContext` | 黑板声明、能力目录和 `PropertyPort` 编辑描述 | A/B 集成者；C 不直接改黑板语义 |
| `CharacterPoseGraphWorkspace`、`CharacterPoseCanvasEditorWriteSession`、`CharacterPoseCanvasCommands` | 投影绑定、剪贴板、命令执行和选择状态 | C 集成者 |
| `GraphAuthoringEditorShell`、现有原生 Pose 入口 | Details、Navigator、Selection、Undo、资源选择 | C 集成者 |
| Timeline Tree contracts 和技能编译器 | 仅使用已有 `BaseGraph` 参数和作者数据 | 不在本 change 中改写；发现迁移需求时记录冲突 |

### 实施顺序和停点

1. **基线清单**：记录 A1-C6 的类型/成员、消费者、程序集和资产引用。按当前 diff 确认与其它工作的交集；只有实际冲突的文件或切片需要停下裁决，不把其它窗口的改动或报错当作全局阻塞。
2. **A 黑板合同**：建立公共 Runtime 边界，拆出独立值与纯规则；耦合构造读取留在具体声明侧，运行访问接口归具体实现，直接消费者同时切换。
3. **B 作者能力与端口**：能力及领域描述的独立定义闭包迁入公共层；具体端口 Runtime 与 Editor 读取服务归各自实现，不增加通用包装层或重写合法访问协议。
4. **C 编辑辅助**：先拆投影绑定、Details、Navigator、Clipboard、Undo 和资源选择，再把 Pose 原生入口全部切到新位置。
5. **C5/C6 旧 UI 删除**：有效入口全部接入正式入口后，逐文件删除旧窗口、GraphView、独有视图、资源和回调；不删除第三方 NodeCanvas、FlowCanvas、Slate，也不删除仍被运行时或资产引用的 TreeDesigner Runtime 类型。
6. **规格收口**：只更新本 change 及受直接影响的主 spec；把未能无损迁出的语义、共享文件冲突和 0 GC 冲突列为未决，不勾选完成。

每个切片的完成条件是：生产者已归位、所有直接消费者已切换、旧文件中的对应类型已不存在、程序集引用保持单向、没有兼容别名或第二入口。公共层仍反向引用 TreeDesigner，或旧画布仅被换目录保留，都不算完成。切片依次推进，共享消费者统一修改；表中修改者表示切片责任，不要求多代理或新建 worktree，手动验收不写入 tasks。

### 2026-09-27 已知交集与边界

- `CharacterPoseGraphAuthoringAdapter.cs`、Pose 节点定义和表现资源文件当前存在未提交改动，属于 C4 共享集成范围，不能由 A/B 独立切片顺手修改。
- `Runtime/BTSMTL/Timeline`、`TreeDesigner/Scripts/Edge/BaseEdge.cs` 和 `Scripts/Debugger/TreeRuntimeDiagnostics.cs` 存在未提交改动；实施时重新读取实际 diff，只处理本次归位直接引用，不碰 Timeline 语义和运行协议。
- `PropertyPort` 同时被 Runtime 图连接、Editor 描述服务和共享作者适配器使用；在 B3 完成前不能删除或重命名 Runtime 类型。
- 共享 Editor 移入高层 Character Editor 的默认程序集会造成低层 Editor 反向依赖风险，必须使用前述独立程序集；具体图编辑服务留在其领域，不把领域适配倒灌进共享层。若仍形成循环，报告具体引用并纠正归属，不新增适配壳。

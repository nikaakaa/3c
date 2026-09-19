## Context

本次范围以用户最后确认的“分一下文件，旧 UI 不要了”为准。早先“完整退出旧执行链”的目标撤回。当前处于文档阶段，未开始代码整理。

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

- 把有效代码放到能解释其职责的位置，混合文件拆开，必要引用随之更新。
- 保留类型、端口身份、值来源、默认值、连接关系、求值行为及当前作者功能。
- 删除旧 UI 及其确认无人使用的附属代码，不留兼容 UI 或第二写入口。
- 运行热路径严格 0 GC；不通过目录整理引入包装分配、临时集合、装箱、闭包或字符串构造。

**Non-Goals:**

- 不整体退出 BaseGraph/BaseNode/PropertyPort，不把类名作为删除依据。
- 不建立新的通用图、求值接口、端口生成系统、metadata 系统或编译层。
- 不更改技能和 Pose 执行方式、变量寻址、generation、provenance、Provider、Timeline 调度与调用语义。
- 不在本次顺带补缓存、短路或优化整个编译图，不据此宣称既有运行链 0 GC 或语义完整。

## Decisions

### 1. 文件归位，不重新设计已有能力

共享作者声明可归入 Runtime/BTSMTL/Authoring，编辑辅助可归入 Editor/GraphAuthoring；领域专用实现留在原领域。只为真实程序集依赖边界建立必要 asmdef，不预设必须新增 Contracts/Adapter/Registry 层。输入是现有类型和直接消费者，输出是同一实现的新位置及更新后的引用。

仅改文件夹而完全保留旧命名改动较少，但职责仍不易识别；本次对确认可迁移的自有类型使用准确命名，并同步必要引用。重新统一全部定义系统能扩大扩展能力，但会增加业务变化与资产风险，超出本次整理范围。

### 2. 旧 UI 删除，有效编辑功能保留

FlowCanvas/NodeCanvas 原生画布及 Slate 保留。Details、Navigator、资源选择、Selection、Clipboard、Undo、C# authoring 和只读观察按实际消费者迁出。旧 TreeWindow、旧 GraphView、独有节点视图、菜单和回调在这些依赖迁出后删除。框架自身的 UI 类型不在删除范围。

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
2. 按黑板定义、端口定义、编辑辅助等职责搬动或拆文件，同步直接引用与必要资产。
3. 删除旧 UI 及确认无消费者的附属代码；有业务缺口的删除项单独报告。
4. 保留当前原生图、编译求值、变量机制与 Timeline 运行，交付实际保留/归位/删除清单及未决冲突。
5. 按完整职责切片中文小步提交，不新增测试或验证任务，不建立 worktree，不覆盖其他任务的正确修改。

## 现行规范对照与冲突处理

| 现行规范 | 本次处理与仍存在的边界 |
| --- | --- |
| btsmtl-graph-core | 撤回原 delta 对 BaseGraph 结构、克隆、初始化等运行合同的整体删除；新增整理边界。主 spec 强制旧底座与现行原生实现的差异仍需独立按消费者裁决，不以本次整理替代全量迁移设计 |
| graph-authoring-domain-framework | 本次只约束已有合同归位，不重写完整 metadata/节点能力框架。主 spec 中旧 UI 与 Pose IR 等历史描述并未因此全部解决，执行方式仍以当前 native-flowcanvas-pose-runtime 为依据；实际冲突报出，不恢复旧运行路径 |
| graph-authoring-editor-shell、btsmtl-tree-inspector-information-architecture | 更新旧画布/窗口实现要求，保留原作者行为；历史 Scenario 标题按项目校验要求保留用于匹配，正文使用原生入口 |
| character-pipeline-blackboard | 只新增声明归位与行为保留要求；原 fact projection、provenance、寻址与生命周期条款不删除或改写。旧 ExposedProperty 唯一表面说法不作为删除有效类型的依据 |
| btsmtl-skill-authoring-model | 新增语义保留与覆盖证据边界，撤回原生端口存在即等价的暗示 |
| btsmtl-timeline-direct-runtime | 撤回强制改写调用身份接口，限定直接引用更新 |
| unity-simulation-assembly-ownership | 只对本次列入清单的自有类型允许精确迁名；仍有有效类型的程序集不是空壳，不要求整体删除 |
| native-flowcanvas-pose-runtime、character-presentation-pose-graph | 保持原生 Pose 实例和正式观察，不恢复 Pose IR 或独立预览执行器 |

未归档的原生 FSM、Pose 编辑、只读黑板和 Workbench 方案不由本次自动完成或归档。主规格中的历史冲突明确保留为未决事实；这不意味着实现可以任选一套路径。遇到本切片实际冲突必须给出具体对象和决定，不以宽泛“统一”扩大范围。

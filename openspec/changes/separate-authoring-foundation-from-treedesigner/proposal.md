## Why

当前自研黑板声明、字段与端口描述、具体图运行实现、编辑辅助和旧 UI 混放在 TreeDesigner 中。公共定义的消费者因此依附旧实现，原生编辑入口仍从旧画布文件读取有效绑定合同。本次将真正独立的公共定义移出 TreeDesigner，建立具体实现依赖公共定义的单向关系；迁出有效编辑能力后删除被替代的旧 UI。仅在 TreeDesigner 内拆小文件不构成本次完成。

## What Changes

- 将独立的黑板声明数据、字段和逻辑端口描述归入 `Runtime/BTSMTL/Authoring`；具体图运行实现保留在所属领域，公共定义不反向引用 TreeDesigner、领域运行器或 Editor。
- 按成员依赖拆分混合类型：`IPipelineBlackboardRuntimeAccess` 继续归属具体图运行接入；变量引用的数据与从 `BaseExposedProperty` 读取数据的构造职责分离，不把旧图依赖带进公共层。
- 将共享绑定合同与有效面板归入 `Editor/GraphAuthoring`，领域适配保留在对应领域；同步生产者、直接消费者、程序集和实际受影响的序列化引用，不重写业务规则。
- 保留 FlowCanvas／NodeCanvas 原生资产和 Slate Timeline，保留 Details、Navigator、资源选择、Selection、Undo、C# authoring 与只读观察等有效功能。
- **BREAKING**：删除被替代的旧窗口、画布、独有视图、菜单和回调；删除前先迁出其中仍被当前编辑入口使用的代码。
- `GraphAuthoringCanvasView` 属于旧 Unity GraphView，不作为 NodeCanvas 画布搬迁保留；有效的投影绑定、剪贴板和状态机编辑合同先迁出。Inspector、调试导航和资产打开入口必须有明确去向，不能删入口掩盖业务缺口。
- 仅删除已有代码、注册和资产引用证据证明无消费者的旧代码。BaseGraph、BaseNode、PropertyPort、BaseExposedProperty 不是预定删除名单；有有效职责的类型保留或原样归位。
- 仅更新此次移动直接影响的命名空间、程序集与序列化引用。已正确工作的编译、求值、黑板和 Timeline 调用不借机重构，不增加通用中间模型或新适配层。
- 严格遵循 0 GC：此次涉及的运行热路径不得产生托管分配；发现既有分配阻碍这项要求时，报告具体调用和取舍，不以“已有问题”宣告通过，也不擅自扩展为性能重构。

## Capabilities

### New Capabilities

无。只整理现有能力的文件与职责归属。

### Modified Capabilities

- `btsmtl-graph-core`：明确代码归位和删除证据边界，不授权整体删除旧图运行类型。
- `graph-authoring-domain-framework`：分离公共定义与具体实现，修订强制复用旧 GraphView 的条款，保留现有领域规则和有效作者功能。
- `graph-authoring-editor-shell`：原生画布与自研业务面板保留，旧 UI 退出。
- `btsmtl-tree-inspector-information-architecture`：保留数据、详情、导航与观察功能，解除旧窗口实现绑定。
- `character-pipeline-blackboard`：迁移现有作者声明引用，保持变量所有权、寻址、生命周期和事实投射。
- `btsmtl-skill-authoring-model`：保留端口和求值语义，不能以原生端口存在推断完整等价。
- `btsmtl-timeline-direct-runtime`：仅调整此次移动直接影响的引用，不强制重写 BaseGraph 参数接口。
- `unity-simulation-assembly-ownership`：必要的类型归位、精确序列化引用迁移及运行热路径 0 GC。

## Impact

主要修改公共定义的模块归属、混合类型的职责、必要引用和旧 UI。公共层具有真实单向依赖边界，不用新的包装对象、接口体系或中间模型实现名义解耦。用户操作仍是原生图编辑、项目业务面板和 Slate Timeline。旧合法资产不因清理而迁成另一种图格式，不批量重建角色资产。

值端口绑定与递归求值已存在；通用脏标记缓存未在已检查基础链中发现，编译 OR 与旧非调试 OR 存在短路行为差异。这些是已知问题，不是本次完成事项，也不能据此把优化自动加入本次任务。具体证据和范围见 design.md。

2026-09-27 本轮只修订本 change 文档及 delta，不实施代码、不提前同步主规格或归档其它任务。此次直接涉及的旧画布约束在 delta 中修订；其它历史运行合同冲突在 design.md 中逐项列明，不以本次整理冒充已完成迁移。格式校验通过不代表代码实现或性能达标。

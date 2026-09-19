## Why

当前自研黑板声明、节点与端口定义、编辑辅助和旧 UI 混放在 TreeDesigner 中。用户要把仍在使用的代码按职责分出来，并删除已不用的旧 UI。本次整理不以完整退出旧图运行类型为目标，也不把旧类型仍存在认定为有两条正式运行路径。

## What Changes

- 将仍在使用的自研声明、端口定义和编辑辅助归位；混合文件按职责拆开，同步必要引用，不重写业务规则。
- 保留 FlowCanvas／NodeCanvas 原生资产和 Slate Timeline，保留 Details、Navigator、资源选择、Selection、Undo、C# authoring 与只读观察等有效功能。
- **BREAKING**：删除被替代的旧窗口、画布、独有视图、菜单和回调；删除前先迁出其中仍被当前编辑入口使用的代码。
- 仅删除已有代码、注册和资产引用证据证明无消费者的旧代码。BaseGraph、BaseNode、PropertyPort、BaseExposedProperty 不是预定删除名单；有有效职责的类型保留或原样归位。
- 仅更新此次移动直接影响的命名空间、程序集与序列化引用。已正确工作的编译、求值、黑板和 Timeline 调用不借机重构，不增加通用中间模型或新适配层。
- 严格遵循 0 GC：此次涉及的运行热路径不得产生托管分配；发现既有分配阻碍这项要求时，报告具体调用和取舍，不以“已有问题”宣告通过，也不擅自扩展为性能重构。

## Capabilities

### New Capabilities

无。只整理现有能力的文件与职责归属。

### Modified Capabilities

- `btsmtl-graph-core`：明确代码归位和删除证据边界，不授权整体删除旧图运行类型。
- `graph-authoring-domain-framework`：保留已有定义及消费者，只分离文件中的业务和显示职责。
- `graph-authoring-editor-shell`：原生画布与自研业务面板保留，旧 UI 退出。
- `btsmtl-tree-inspector-information-architecture`：保留数据、详情、导航与观察功能，解除旧窗口实现绑定。
- `character-pipeline-blackboard`：迁移现有作者声明引用，保持变量所有权、寻址、生命周期和事实投射。
- `btsmtl-skill-authoring-model`：保留端口和求值语义，不能以原生端口存在推断完整等价。
- `btsmtl-timeline-direct-runtime`：仅调整此次移动直接影响的引用，不强制重写 BaseGraph 参数接口。
- `unity-simulation-assembly-ownership`：必要的类型归位、精确序列化引用迁移及运行热路径 0 GC。

## Impact

主要修改文件组织、混合类型的位置、必要引用和旧 UI。用户操作仍是原生图编辑、项目业务面板和 Slate Timeline。旧合法资产不因清理而迁成另一种图格式，不批量重建角色资产。

值端口绑定与递归求值已存在；通用脏标记缓存未在已检查基础链中发现，编译 OR 与旧非调试 OR 存在短路行为差异。这些是已知问题，不是本次完成事项，也不能据此把优化自动加入本次任务。具体证据和范围见 design.md。

本轮只修订本 change 文档，不实施代码、不修改主规格或归档其它任务。现行规格中仍有过时图底座约束，冲突单独列出；格式校验通过不代表语义完整或性能达标。

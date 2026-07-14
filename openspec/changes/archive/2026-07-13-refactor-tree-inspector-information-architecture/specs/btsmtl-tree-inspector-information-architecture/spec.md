# btsmtl-tree-inspector-information-architecture Specification

## Purpose

定义 Tree Inspector 左侧面板的正式信息架构：作者数据、选中对象编辑和运行时观察必须使用彼此明确且不重叠的 UI 边界。

## ADDED Requirements

### Requirement: Tree Inspector 必须将 Data 与 Inspector 作为互斥工作页

Tree Inspector MUST 提供 `Data` 与 `Inspector` 两个互斥工作页。`Data` 页 MUST 只承载唯一 Graph Data Catalog；`Inspector` 页 MUST 只承载当前选中 Node/Edge 的 authoring 内容，或无选择时的 Graph Authoring Settings。Graph Data Catalog MUST NOT 在 Inspector 页显示、响应输入或复制为第二套面板。

#### Scenario: 打开角色 RootTree

- **WHEN** 作者从 Character Pipeline 打开 RootTree
- **THEN** 左侧默认显示 Data 页和唯一 Graph Data Catalog
- **AND** Data 页 MUST NOT 显示通用 Tree runtime 字段

#### Scenario: 选择 Transition edge

- **WHEN** 作者在 Graph 画布选择一条 Transition edge
- **THEN** 左侧 MUST 切换到 Inspector 页
- **AND** Inspector MUST 显示该 edge 的正式 priority、ownership、rule 与 handoff authoring 内容
- **AND** Graph Data Catalog MUST 不在该页显示

#### Scenario: 手动查看无选择 Inspector

- **WHEN** 当前没有选中 Node 或 Edge，作者切换到 Inspector 页
- **THEN** 页面 MUST 显示当前 Graph 的合法 authoring settings 或明确的空图设置状态
- **AND** 系统 MUST NOT 用节点、边、runtime state 或伪默认配置填充该页面

### Requirement: Graph Authoring Settings 必须排除运行时生命周期字段

TreeWindow 与 BaseTreeAsset Inspector 的图级属性投影 MUST 只显示可保存且可编辑的 authoring 配置。非序列化的 Tree lifecycle 状态，包括 `Running`、`State` 及等价 runtime status，MUST NOT 通过通用属性扫描、字段注解或独立 Inspector 区块显示。

#### Scenario: Authoring 模式查看 RunnableTree

- **WHEN** 作者在 Authoring 模式打开任意 RunnableTree
- **THEN** 左侧 Data 页和 Inspector 页 MUST NOT 显示 `Running` 或 `State` 字段
- **AND** 系统 MUST NOT 将这些状态写入 authoring asset

#### Scenario: Live Debug 观察执行状态

- **WHEN** 作者切换 TreeWindow 到 Live Debug 并选择有效 runtime target/instance
- **THEN** Graph 的运行状态 MUST 由 RuntimeDebugSession 的 source-mapped Trace overlay 呈现
- **AND** 系统 MUST NOT 恢复直接读取 runtime Tree/Node 字段的 Inspector 路径

### Requirement: Data 页筛选必须在窄栏中保持 source-aware

Data 页 MUST 始终提供文本搜索和显式 `All`、`Input`、`Blackboard` source 切换。Blackboard 专属的 Context 与 Scope 条件 MUST 只在需要时呈现；Input 条目 MUST NOT 被赋予虚假的 Blackboard scope、owner 或写入能力。筛选、分组折叠和条目展开状态 MUST 保持 editor-only view state。

#### Scenario: 只查看 Input

- **WHEN** 作者选择 Input source
- **THEN** Data 页 MUST 显示 Input Values 与 Action Requests
- **AND** Blackboard Context/Scope 控件 MUST 不占用默认数据列表空间
- **AND** Input 条目 MUST 保持 external read-only 语义

#### Scenario: 过滤当前图的 Blackboard

- **WHEN** 作者在 Blackboard source 下选择 Current Context 或具体 Scope
- **THEN** Catalog MUST 只显示匹配的 Blackboard declaration
- **AND** 系统 MUST 不修改 declaration 的 owner、scope、identity 或 runtime address

### Requirement: TreeWindow 运行时模式必须保持窗口级边界

`Authoring / Live Debug` MUST 是整个 TreeWindow 的模式，而不是 Data 页、Inspector 页或 Graph Settings 的局部状态。Live Debug 下 authoring 命令 MUST 保持只读；target、instance、history 和 runtime overlay MUST 继续使用共享 RuntimeDebugSession。

#### Scenario: 在 Live Debug 中切换左侧页签

- **WHEN** 作者在 Live Debug 模式下从 Data 切换到 Inspector 或反向切换
- **THEN** 页签切换 MUST 不改变 runtime target、instance、Trace history 或 source revision
- **AND** 作者不得通过任一页签写入 Graph、Blackboard、Input 或 runtime state

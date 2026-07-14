# btsmtl-tree-inspector-information-architecture Specification

## Purpose
定义 Tree Inspector 左侧面板的正式信息架构：作者数据、选中对象编辑和运行时观察必须使用彼此明确且不重叠的 UI 边界。
## Requirements

### Requirement: Tree Inspector 必须将 Data 与 Inspector 作为互斥工作页

Tree Inspector MUST提供 Data 与 Inspector 两个互斥工作页。Data 页 MUST只承载唯一 Graph Data Catalog；Inspector 页 MUST只承载当前选中 Node/Edge 的 BTSMTL authoring 内容，或无选择时的 Graph Authoring Settings。角色动画 Layer、producer binding、transition、fade、playback lifecycle 和 Animancer 配置 MUST不进入 Tree Inspector 可写内容。

#### Scenario: 打开角色 RootTree

- **WHEN** 作者从 Character Pipeline 打开 RootTree
- **THEN** 左侧默认显示 Data 页和唯一 Graph Data Catalog
- **AND** Data 页 MUST不显示动画播放生命周期字段

#### Scenario: 选择 Transition edge

- **WHEN** 作者选择一条 StateMachine Transition edge
- **THEN** Inspector MUST显示 priority、condition ownership、rule 与 interruption
- **AND** Inspector MUST不显示 HandoffRole、animation strategy、duration、curve 或 producer binding

#### Scenario: 手动查看无选择 Inspector

- **WHEN** 当前没有选中 Node 或 Edge，作者切换到 Inspector 页
- **THEN** 页面 MUST显示当前 Graph 的合法 authoring settings 或明确空状态
- **AND** 系统 MUST不使用 runtime lifecycle 或伪默认 Presentation 配置填充

#### Scenario: 打开动画表现配置

- **WHEN** 作者需要调整 producer transition 或 animation layer
- **THEN** 系统 MUST定位 CharacterPipelineDefinition Inspector 或 Animancer Transition Library 正式入口
- **AND** Tree Inspector MUST不创建同一数据的第二写入口

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

`Authoring / Live Debug` MUST 是整个 TreeWindow 的模式，而不是 Data 页、Inspector 页或 Graph Settings 的局部状态。Live Debug 下 authoring 命令 MUST 保持只读。TreeWindow MUST 通过共享 RuntimeDebugSession 为当前 binding 获取/释放 Graph 与 StateMachine Live interest，并读取共享 provider current state 或显式 Capture history；它持有只属于当前 TreeWindow 的 Graph runtime binding。

#### Scenario: 在 Live Debug 中切换左侧页签

- **WHEN** 作者在 Live Debug 模式下从 Data 切换到 Inspector 或反向切换
- **THEN** 页签切换 MUST 不改变共享 target 或 Capture history position
- **AND** 页签切换 MUST 不重置当前 TreeWindow 的 Graph Follow / Pin binding
- **AND** 作者不得通过任一页签写入 Graph、Blackboard、Input 或 runtime state

#### Scenario: Graph 与 Timeline 同时打开

- **WHEN** 作者同时打开 TreeWindow 和 TimelineEditorWindow
- **THEN** TreeWindow MUST 只修改自己的 Graph runtime binding
- **AND** TimelineEditorWindow 的 Timeline playback binding MUST 保持不变
- **AND** 停止 Capture 后两个窗口 MUST 在同一 shared Capture history position 显示各自 overlay

#### Scenario: 创建 TreeWindow

- **WHEN** Editor 创建 TreeWindow 与其 Inspector 视觉树
- **THEN** USS MUST 使用当前 Unity 支持的选择器
- **AND** 创建过程 MUST 不因 :first-child 或 :last-child 产生 stylesheet parser error

#### Scenario: Play Mode domain reload 后恢复当前 Graph

- **WHEN** 当前 TreeWindow 经历 Play Mode domain reload 并重建 UI
- **THEN** 窗口 MUST 只按已保存的 serialized owner、property path 与 GraphAuthoringId 恢复当前 Graph
- **AND** 窗口 MUST 重建自己的 Graph runtime binding，不得恢复旧 runtime instance
- **AND** locator 缺失或 identity 不一致时 MUST 停止恢复，不得按名称、路径近似或窗口顺序选择其它 Graph

# Change: 收口 Tree Inspector 的数据、选择与运行时观察信息架构

## Why

统一 Graph Data Catalog 已经成为 Input 与 Pipeline Blackboard 的唯一作者数据入口，但当前 Tree Inspector 的 UXML 层级仍保留旧的通用 Tree 属性页：`Graph Data` 是 `Graph` 页和 `Inspector` 页之外的同级元素，因此会同时出现在两个页签；`Graph` 页顶部还通过通用反射属性扫描显示 `RunnableTree` 的非序列化 `Running` 与 `State`。

这会让作者无法判断左侧面板当前是在管理图数据、编辑选中节点，还是观察运行时。`Running` 与 `State` 既不是可保存的 authoring 配置，也不是新的 RuntimeDebugSession 观察合同，保留它们会重新引入一条旧的直接运行时状态展示路径。

## What Changes

- 将 Tree Inspector 固定为 `Data` 与 `Inspector` 两个互斥页签：`Data` 仅承载唯一 Graph Data Catalog；`Inspector` 仅承载当前选中 Node/Edge 的作者属性，或在没有选择时承载图级 authoring settings。
- 将 Graph Data Catalog 移入 `Data` 页的唯一内容区，删除其作为两个页签同级元素的布局；`Inspector` 页不得重复显示、复制或嵌入 Catalog。
- 删除 `RunnableTree.Running` 与 `RunnableTree.State` 的通用 `ShowInInspector` 展示入口，并让 Tree/Asset Inspector 的图级属性投影只展示真正可编辑、可序列化的 authoring 配置。
- 保持 `Authoring / Live Debug` 为窗口级模式。Live Debug 的运行状态继续只来自 `RuntimeDebugSession`、Source Map 与 Trace overlay，不在 Data 或普通 Inspector 中恢复直接 runtime state 字段。
- 收紧左侧窄面板的 Data 筛选：Source 使用紧凑的 `All / Input / Blackboard` 切换；Blackboard 专属的 Context/Scope 仅在需要时显示，Input 不伪造 scope 或 owner。
- 保持现有 Graph Data Catalog source、declaration identity、可见性、拖拽建节点、详情、创建与删除语义不变；不新建变量表、Input 面板或运行时调试数据源。

## Impact

- 影响 TreeDesigner Editor 的 `BaseTreeInspectorView`、`BaseTreeInspector`、`BaseTreeInspectorInside.uxml/.uss`，以及 `RunnableTree` 的旧 Inspector 注解。
- 影响 current `btsmtl-graph-data-catalog-authoring`：其“唯一 Graph Data Catalog”要求补充为只能位于 `Data` 页，不能在 `Inspector` 页重复出现。
- 新增 `btsmtl-tree-inspector-information-architecture` 能力，用于规定页签职责、空选择行为和 authoring/runtime 边界。
- 不改变 Graph、Node、Edge、InputProfile、Pipeline Blackboard、RuntimeDebugSession、Trace、资产序列化或 gameplay runtime 数据。

## Current Spec Comparison

- current `btsmtl-graph-data-catalog-authoring` 已要求唯一 Catalog，但未约束页签归属；本 change 修改该 requirement，补齐 `Data` 页唯一承载与 `Inspector` 页排他性。
- current `btsmtl-graph-core` 要求左侧 Inspector 展示选中节点、边的引用 ownership 与编辑入口；本 change 保持该行为，只补充无选择时的图级 authoring settings，不冲突。
- `add-btsmtl-compiled-runtime-debugging` 仍是未完成的 active change，其已落地合同要求 Editor 不直接绑定 runtime clone、Graph Live Debug 使用共享 Session，但 Graph/Timeline live overlay 仍有未完成任务。本 change 不修改其 Trace 合同，而是删除仍泄漏到普通 Inspector 的旧 `Running / State` 显示，和该约束一致。
- 未发现需要删除的 current spec；也不需要新增兼容、迁移资产或平行 UI 路径。

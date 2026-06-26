# Change: 抽离 Taco BaseGraph 图底座

## Why
当前 Taco 的 `BaseTree` 同时承担图数据、图编辑操作、Unity 资产入口、编辑器窗口入口和运行时 Tree 命名语义。SM、Timeline、BT 之后都要在同一套节点/边/端口链路上组合，如果继续把所有图能力都绑死在 `BaseTree` 上，会让“Graph 是底层结构、Tree/SM/Timeline 是上层语义”这个边界变模糊。

这次变更只抽底层图结构，不重做编辑器 UI，不新增 Workbench 路径，也不把状态机或 Timeline 的业务闭环塞进 `BaseGraph`。

## What Changes
- 新增 `BaseGraph` 作为 Taco 图数据和结构编辑操作的共同底座。
- 保持 `BaseTree : BaseGraph`，让现有编辑器 UI、Inspector、节点搜索、资产打开入口继续面向 `BaseTree`。
- 将节点、边、属性边、暴露属性、GUID 映射、创建/删除节点、连接/断开边、刷新/校验等图结构职责从 `BaseTree` 收敛到 `BaseGraph`。
- 将 `BaseNode`、`BaseEdge`、`PropertyEdge` 的 Owner/Init 图归属从只认 `BaseTree` 调整为认 `BaseGraph`。
- 保留 Graph 引用和下钻入口的资产类型为 `BaseTree`，因为当前可打开的编辑器资产仍然是 `BaseTree` 及其子类。
- 保持 `RunnableTree : BaseTree`、`StateMachineGraph : BaseTree`、`TimelineNode : RunnableNode` 的现有业务边界。
- 不新增 `BaseGraphWindow`、并行端口系统、Workbench 图路径、旧数据 fallback 或运行时编译导出。

## Impact
- 影响的规格：`taco-graph-core`
- 关联但不替代的变更：
  - `refactor-taco-componentized-node-authoring`
  - `add-runnable-timeline-node`
  - `add-unified-sm-node-authoring`
- 影响的代码：
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/BaseTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/Tree_Extension.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/RunnableTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/OneRootTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/SubTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/BaseNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Edge/BaseEdge.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Edge/PropertyEdge.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/StateMachineGraph.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/NestedGraphValidation.cs`
  - Taco 编辑器里直接绑定 `BaseTree` 序列化和窗口入口的代码

## Conflicts / Alignment
- 与 `add-unified-sm-node-authoring` 不冲突：该变更要求 `StateMachineGraph : BaseTree`，本提案保持这个继承链，只是在 `BaseTree` 下方增加 `BaseGraph`。
- 与 `refactor-taco-componentized-node-authoring` 不冲突：该变更要求继续使用 Taco 原生 `PropertyPort`/`PropertyEdge`，本提案不新增并行端口描述符。
- 与 `add-runnable-timeline-node` 不冲突：该变更要求 `TimelineNode : RunnableNode`，本提案不改变 TimelineNode 的节点身份，只为后续执行上下文整理提供更清晰的图归属边界。

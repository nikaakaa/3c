# 变更：重构 Taco 组件化节点创作链路

## 背景

当前 Taco 的 `PropertyPort` 本身可以表达 Timeline、StateMachine、Tree 节点之间的类型化值连接，嵌套能力的主要限制不在端口，而在字段扫描、端口身份、节点创建范围和嵌套图引用规则。

现有链路把端口和面板字段默认绑定到 `BaseNode` 子类字段上：`BaseNode.BeforeInit()`、`BaseNode.Refresh()`、`BaseNodeView.GeneratePropertyPorts()`、`NodePanelView.Refresh()`、`NodeInputFieldContainerView.Refresh()` 都直接使用 `FieldInfo.GetValue(node)` 和节点字段名。这样会阻止“节点由功能模块组合”的模型，也会让 TimelineNode、StateMachineNode、TreeNode 难以作为同一类 Taco 节点在一个图内组合和下钻。

本变更要直接破坏性改造 Taco 原链路，保留 `PropertyPort` / `PropertyEdge` 作为唯一端口系统，不再引入 `WorkbenchPortDescriptor` 之类并列协议。

## 变更内容

- **破坏性变更**：Taco 属性端口连接身份从字段名/显示名切换为稳定 `PortId`。
- **破坏性变更**：Taco 字段扫描从 `FieldInfo` 列表升级为 `NodeFieldAccessor`，统一支持节点字段和节点模块字段。
- 新增 Taco 节点模块创作模型，使节点能力由可序列化功能模块组合，而不是继续依赖专用继承树扩张。
- 改造 `BaseNode` 初始化、刷新、反序列化、端口映射构建和 `IsConnected`，让其通过字段访问器和稳定端口 ID 工作。
- 改造 `NodePanelView`、`NodeInputFieldContainerView`、`BaseNodeView`、`NodePortContainerView`，让编辑器 UI 不再假设字段一定在节点上。
- 统一 Timeline / StateMachine / Tree 引用节点的创建语义，使它们可以作为 Taco 原生节点出现在同一个创作图中。
- 定义嵌套图引用、打开下钻、作用域和循环校验的边界：嵌套由节点字段/模块承载，端口只负责连接和值流。
- 明确禁止新增并列端口描述符、并列注册表、并列 WorkbenchTree 代码路径。

## 影响范围

- 影响的规格：
  - `taco-componentized-node-authoring`
- 影响的代码：
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/BaseNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Node_Extension.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/PropertyPort/PropertyPort.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Edge/PropertyEdge.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/BaseTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/Tree_Extension.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/Node/BaseNodeView.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/Node/NodePanelView.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/Node/NodeInputFieldContainerView.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/Node/NodePortContainerView.cs`
  - Taco Timeline / Tree 节点创建相关代码

## 非目标

- 不新增 `Workbench` 目录。
- 不新增 `WorkbenchPortDescriptor`、`WorkbenchTypeRegistry` 或其它并列端口协议。
- 不做运行时编译导出。
- 不迁移旧 Locomotion、Action、BodyClaim、FootPhase 数据。
- 不把 Timeline 编辑器重写到节点图里；Timeline 仍是引用/下钻目标，轨道编辑由已有 Timeline 编辑器负责。
- 不跑 Unity batchmode。

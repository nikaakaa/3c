# 设计：Taco 原生节点组件化与嵌套图创作

## 背景

Taco 当前有可复用的核心：

- `BaseTree` 保存节点、边、属性边。
- `BaseNode` 保存 GUID、Owner、属性端口映射和生命周期入口。
- `PropertyPort<T>` 保存类型化值和输入/输出属性边 GUID。
- `PropertyEdge` 保存起点/终点节点和起点/终点端口名称。
- `BaseNodeView`、`NodePanelView`、`NodeInputFieldContainerView` 已经能显示端口和字段。

真正的问题是字段发现协议太窄：所有地方都默认字段属于节点本身。组件化之后，端口和可编辑字段会出现在节点模块内，原代码无法拿到目标对象和序列化属性路径。

## 目标

- 保留 Taco `PropertyPort` / `PropertyEdge` 作为唯一属性端口系统。
- 让 `BaseNode` 能扫描节点字段和模块字段。
- 让端口连接身份从字段名升级为稳定 `PortId`。
- 让编辑器 UI 通过统一字段访问器读取字段、端口、特性和序列化属性。
- 让 TimelineNode、StateMachineNode、TreeReferenceNode 可以在同一个 Taco 创作图里创建和组合。
- 让嵌套图引用成为节点/模块字段，不让端口承担下钻语义。

## 非目标

- 不创建新的 Workbench 创作图。
- 不做第二套端口描述符。
- 不通过兜底兼容配置保留旧错误路径。
- 不引入运行时编译器。
- 不把 StateMachine、Timeline、Tree 作为并列运行时系统重做。

## 当前问题

### BaseNode 字段扫描

`BaseNode.BeforeInit()` 当前扫描 `this.GetAllFields()`，然后执行 `fieldInfo.GetValue(this)`。这只能访问节点字段。

### BaseNode 编辑器刷新

`BaseNode.Refresh()` 当前把 `propertyPort.Name` 强制同步成 `fieldInfo.Name`。这会把字段名、显示名、连接键混在一起。

### PropertyEdge 连接身份

`PropertyEdge` 构造时把 `startPropertyPort.Name` 和 `endPropertyPort.Name` 写入 `BaseEdge`。组件化后，不同模块内可能出现同名字段，字段重命名也不应该破坏连接。

### 编辑器 UI

`BaseNodeView.GeneratePropertyPorts()`、`NodePanelView.Refresh()`、`NodeInputFieldContainerView.Refresh()` 都直接读取节点字段，并通过 `m_Node.GetNodeSerializedProperty(fieldName)` 绑定序列化属性。模块字段需要 `m_Modules.Array.data[index].fieldName` 这类路径，原接口表达不了。

### 节点创建和 Tree 类型

Timeline / StateMachine / Tree 的创建范围被 Tree 类型、`AcceptableNodePaths` 和专用节点继承关系拆开。端口可以连，但节点不一定能在同一个创作图里被创建和解释。

## 决策

### 决策 1：引入 NodeFieldAccessor

新增 Taco 原生 `NodeFieldAccessor`，作为字段扫描唯一返回值。

它应表达：

- 所属节点。
- 目标对象。
- 字段信息。
- 字段键。
- 序列化属性路径。
- 可选模块 ID。
- 字段特性。
- 读写字段值。

`BaseNode.GetFieldAccessors()` 返回节点自身字段和模块字段。旧 `GetAllFields()` 不再作为编辑器/运行时主入口。

### 决策 2：引入 NodeModule

新增可序列化 `NodeModule` 基类或接口。它不是运行时 ECS 组件，而是节点创作功能分类。

模块可以提供：

- 属性端口字段。
- 面板字段。
- 子图引用字段。
- Timeline 引用字段。
- StateMachine 数据字段。
- 编辑器策略 / 能力数据。

节点通过 `[SerializeReference]` 保存模块列表。节点子类可以声明默认模块组合。

### 决策 3：PropertyPort 增加稳定身份

`PropertyPort` 增加稳定字段：

- `PortId`
- `DisplayName`
- `OwnerModuleId`
- `FieldKey`

连接、查找和 `PropertyPortMap` 使用 `PortId`。显示文本使用 `DisplayName` 或特性标签。字段名只作为默认生成来源，不作为持久连接协议。

### 决策 4：PropertyEdge 改用 PortId

`PropertyEdge` 持久化起点/终点端口 ID。`Init()` 通过 `BaseNode.PropertyPortMap[portId]` 恢复端口引用。

这会破坏旧基于字段名的属性边数据。按当前清理原则，不保留旧兜底兼容。

### 决策 5：编辑器 UI 全部走字段访问器

这些位置必须改造为字段访问器驱动：

- `BaseNodeView.GeneratePropertyPorts()`
- `BaseNodeView.RefreshPropertyPorts()`
- `NodePanelView.Refresh()`
- `NodePanelView.Rebind()`
- `NodeInputFieldContainerView.Refresh()`
- `NodeInputFieldContainerView.Rebind()`
- `NodePortContainerView` 的属性端口映射键

UI 不能再通过 `fieldInfo.GetValue(m_Node)` 或 `GetNodeSerializedProperty(fieldName)` 假设字段在节点上。

### 决策 6：嵌套由节点/模块字段表达

Timeline / StateMachine / Tree 嵌套不通过端口本身表达。

建议薄节点模型：

- `TimelineNode`
  - Timeline 引用模块
- `StateMachineNode`
  - 带作用域的 Graph 引用模块
- `TreeReferenceNode`
  - Tree 引用模块

下钻、打开子图、防循环和作用域属于图、编辑器、结构规则。

### 决策 7：统一节点创建范围

节点搜索和创建应允许同一创作图创建普通逻辑节点、TimelineNode、StateMachineNode、TreeReferenceNode 和值节点。

可以保留 Taco 的 `AcceptableNodePaths` 机制，但不能让 Timeline/StateMachine/Tree 节点被硬性隔离到专用 Tree 才能创建。若保留路径过滤，它只能是显示分类，不是能力边界。

## 取舍

### 直接改 Taco 原链路

优点：

- 不产生第二套 UI、端口、注册表。
- 旧 Taco 编辑器能继续作为基座。
- 嵌套和组件化问题在源头解决。

代价：

- 破坏面集中在 Taco 核心类。
- 需要一次性替换多处 `FieldInfo` 直读。
- 旧字段名属性边数据会失效。

### 保留 PropertyPort

优点：

- 类型化值、输入输出边 GUID、端口视图、颜色和现有值节点逻辑继续可用。
- 当前局限主要是身份和扫描，不是表达能力。

代价：

- `PropertyPort.Name` 原有语义需要收缩，不再承担连接身份。
- 一些旧节点的动态端口改型逻辑要跟随 `PortId` 更新。

## 迁移计划

1. 删除并保持不存在 Workbench 并列代码路径。
2. 新增字段访问器类型，但先让节点字段通过字段访问器走通。
3. 把 `BaseNode` 初始化和刷新改为字段访问器驱动。
4. 把编辑器 UI 改为字段访问器驱动。
5. 给 `PropertyPort` 增加稳定身份，并切换映射键和边键。
6. 引入 `NodeModule`，让模块字段进入字段访问器。
7. 新增薄 Timeline/StateMachine/Tree 引用节点。
8. 放开同一创作图的节点创建范围。
9. 增加嵌套图作用域和循环校验入口。

## 风险

- 风险：字段扫描替换不完整会导致端口显示和保存不一致。
  - 处理：所有扫描入口必须统一到 `GetFieldAccessors()`。
- 风险：旧动态端口节点仍按字段名处理。
  - 处理：把 `SetPropertyPort`、`AddPropertyPort`、`RemovePropertyPort` 一起切到端口 ID。
- 风险：Timeline/StateMachine 节点过早带入运行时语义。
  - 处理：第一阶段只做创作引用和下钻，不做执行。
- 风险：Tree 类型隔离残留。
  - 处理：搜索/创建规则以节点类型能力为准，路径只作为显示分类。

## 待确认问题

- 无阻塞问题。当前决策按“激进清理、保留 Taco 原端口、破坏性升级原链路”执行。

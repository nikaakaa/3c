## ADDED Requirements

### Requirement: BaseGraph 承载图结构数据
系统 MUST 提供 `BaseGraph` 作为 Taco 图结构底座。`BaseGraph` MUST 承载节点、普通边、属性边、暴露属性和对应 GUID 映射。`BaseTree` MUST NOT 在迁移后继续保存一套重复的图结构集合。

#### Scenario: 读取图节点
- **WHEN** 编辑器打开一个 `BaseTree` 资产
- **THEN** 编辑器 MUST 能通过继承自 `BaseGraph` 的节点集合读取节点
- **AND** `BaseTree` MUST NOT 维护第二套节点集合

#### Scenario: 读取图边
- **WHEN** 编辑器刷新一个 GraphView
- **THEN** 编辑器 MUST 能通过继承自 `BaseGraph` 的普通边和属性边集合重建连线
- **AND** 系统 MUST NOT 从并行边集合恢复连线

### Requirement: BaseGraph 承载图结构编辑操作
系统 MUST 在 `BaseGraph` 上提供正式的图结构编辑操作，包括创建/删除节点、连接/断开普通边、连接/断开属性边、刷新图结构和校验初始化数据。`BaseTree` MUST NOT 与 `BaseGraph` 并列保留另一套可修改实现。

#### Scenario: 创建节点
- **WHEN** `BaseTreeView` 请求创建节点
- **THEN** 请求 MUST 调用 `BaseTree` 继承自 `BaseGraph` 的正式创建逻辑
- **AND** 新节点 MUST 被加入同一套节点集合和 GUID 映射

#### Scenario: 连接属性端口
- **WHEN** 用户连接两个 PropertyPort
- **THEN** 系统 MUST 通过 `BaseGraph` 的正式属性边逻辑创建 `PropertyEdge`
- **AND** 该连接 MUST 继续使用 Taco 原生 `PropertyPort` / `PropertyEdge` 主链路

### Requirement: BaseTree 保持编辑器资产入口
系统 MUST 保持 `BaseTree : BaseGraph`。现有 Taco 编辑器 UI 第一阶段 MUST 继续以 `BaseTree` 作为打开、显示、Inspector 和节点搜索入口。系统 MUST NOT 为本变更新增 `BaseGraphWindow`。

#### Scenario: 打开 BaseTree 资产
- **WHEN** 用户打开一个 `BaseTree` 或其子类资产
- **THEN** `TreeWindowUtility` MUST 继续通过 `BaseTree` 打开窗口
- **AND** 打开的窗口 MUST 能显示继承自 `BaseGraph` 的图数据

#### Scenario: 节点搜索过滤
- **WHEN** 用户在编辑器中打开节点搜索
- **THEN** 搜索结果 MUST 继续通过当前 `BaseTree` 实例的 `CanCreateNodeType` 过滤
- **AND** 图结构抽层 MUST NOT 绕过现有节点路径和创建规则

### Requirement: 节点和边归属指向 BaseGraph
系统 MUST 让 `BaseNode`、`BaseEdge` 和 `PropertyEdge` 的图归属指向 `BaseGraph`。节点和边 MUST NOT 要求 Owner 一定是 `BaseTree` 才能初始化、恢复端点或访问图结构映射。

#### Scenario: 初始化节点
- **WHEN** 一个 Graph 初始化它的节点
- **THEN** 节点 Owner MUST 被设置为当前 `BaseGraph`
- **AND** 节点 MUST 能通过该 Owner 访问图结构和端口映射

#### Scenario: 初始化边
- **WHEN** 一个 Graph 初始化它的普通边或属性边
- **THEN** 边 Owner MUST 被设置为当前 `BaseGraph`
- **AND** 边 MUST 能通过该 Owner 的 GUID 映射恢复起点和终点

### Requirement: Graph 引用保持 BaseTree 资产边界
系统 MUST 保持 Graph 引用模块和下钻 UI 使用 `BaseTree` 资产引用。`BaseGraph` 抽层 MUST NOT 让用户创建无法被现有 Taco 编辑器打开的裸 `BaseGraph` 引用。

#### Scenario: 打开子 Graph
- **WHEN** 节点或模块暴露一个子 Graph 引用
- **THEN** 该引用 MUST 指向 `BaseTree` 或其子类资产
- **AND** 编辑器 MUST 继续通过 `OpenTree()` 打开该引用

#### Scenario: 不保存并行引用字段
- **WHEN** 节点模块需要保存下钻 Graph
- **THEN** 模块 MUST 保存一条正式的 `BaseTree` 引用字段
- **AND** 模块 MUST NOT 同时保存 `BaseTree` 和 `BaseGraph` 两套引用字段

### Requirement: 运行时生命周期不下沉到 BaseGraph
系统 MUST 保持 `BaseGraph` 只表达图结构。`RunnableTree` MUST 继续表达可执行 Tree 生命周期，并继续承载 `Running`、`State`、`DeltaTime`、`UpdateTree` 和 `ResetTree`。`BaseGraph` MUST NOT 默认拥有运行状态。

#### Scenario: 普通编辑 Graph
- **WHEN** 一个 Graph 只用于编辑或作为状态机图资产
- **THEN** 它 MUST NOT 因为继承 `BaseGraph` 而自动拥有运行时状态字段

#### Scenario: 可执行 Tree
- **WHEN** 一个 Graph 需要被 tick
- **THEN** 它 MUST 通过 `RunnableTree` 或其子类表达可执行生命周期
- **AND** 系统 MUST NOT 通过 `BaseGraph` 直接执行节点

### Requirement: 不新增分裂路径
系统 MUST 不因为 `BaseGraph` 抽层新增 Workbench 图路径、并行端口描述符、旧数据 fallback 或重复序列化集合。迁移后的正式链路 MUST 只有一套图数据、一套 Taco 原生端口系统和一套编辑器打开入口。

#### Scenario: 搜索并行端口系统
- **WHEN** 代码中存在新 Graph 抽层
- **THEN** 不得出现为 `BaseGraph` 单独新增的 Workbench 端口描述符或边协议

#### Scenario: 搜索重复图集合
- **WHEN** 迁移完成后搜索 `m_Nodes`、`m_Edges` 和 `m_PropertyEdges`
- **THEN** 这些正式集合 MUST 只归属于 `BaseGraph`
- **AND** `BaseTree` MUST NOT 保留兼容用镜像字段

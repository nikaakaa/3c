# taco-graph-core Specification

## Purpose
定义 Taco 图底座：`BaseGraph` 承载唯一图结构数据、编辑操作和运行上下文；`BaseTree : BaseGraph` 继续作为 Unity 资产和编辑器入口；执行生命周期留在 `RunnableTree`、`StateMachineGraphRuntime`、`TimelineNode` 等上层 Module。

## Requirements
### Requirement: BaseGraph 承载唯一图结构数据
系统 MUST 使用 `BaseGraph` 保存节点、普通边、属性边、暴露属性和对应 GUID 映射。`BaseTree` MUST NOT 再保存第二套节点、边或属性边集合。

#### Scenario: 编辑器读取图数据
- **WHEN** Taco 编辑器打开 `BaseTree` 资产
- **THEN** 编辑器 MUST 从继承自 `BaseGraph` 的正式集合读取节点、边和属性边
- **AND** 系统 MUST NOT 从并行集合恢复 GraphView

### Requirement: BaseGraph 承载结构编辑操作
系统 MUST 在 `BaseGraph` 上提供正式结构编辑操作，包括创建/删除节点、连接/断开普通边、连接/断开属性边、刷新和初始化清理。所有入口 MUST 使用同一套集合和 GUID 映射。

#### Scenario: 创建节点
- **WHEN** 编辑器、粘贴流程或脚本请求创建节点
- **THEN** 请求 MUST 通过当前 Graph 的正式创建逻辑
- **AND** 新节点 MUST 被加入同一节点集合和 GUID 映射

#### Scenario: 连接属性端口
- **WHEN** 用户连接两个 `PropertyPort`
- **THEN** 系统 MUST 创建 Taco 原生 `PropertyEdge`
- **AND** 连接 MUST 保存在正式属性边集合中

### Requirement: 节点创建尊重图类型规则
系统 MUST 让 `BaseGraph.CreateNode(Type)` 尊重当前图的 `CanCreateNodeType(Type)`。节点搜索、拖拽、粘贴和脚本创建 MUST 不绕过该规则。

#### Scenario: StateMachineGraph 拒绝非法节点
- **WHEN** 创建路径尝试向 `StateMachineGraph` 创建 `StateMachineNode`、`RootNode` 或普通 runnable 节点
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 系统 MUST NOT 把该节点加入正式节点集合

#### Scenario: StateMachineGraph 接受条件节点
- **WHEN** 创建路径尝试向 `StateMachineGraph` 创建 `ValueNode`
- **THEN** 创建逻辑 MUST 允许该节点作为 Transition 条件来源

### Requirement: BaseTree 保持资产和编辑器入口
系统 MUST 保持 `BaseTree : BaseGraph`。直接打开 Graph 资产 MUST 继续通过 `OpenTree()` 或等价 `TreeWindowUtility` 入口打开，不新增 `BaseGraphWindow`。

#### Scenario: 直接打开资产
- **WHEN** 用户从 Project、Inspector 或 Tree Browser 打开 `BaseTree`
- **THEN** Taco MUST 以该资产作为当前编辑窗口的根 Graph
- **AND** 打开流程 MUST NOT 需要来源节点上下文

### Requirement: Graph 引用和页面栈保持 editor-only
系统 MUST 让节点或模块通过正式 `BaseTree` 引用表达下钻 Graph。当前窗口内从节点引用下钻时，编辑器 MAY 使用页面栈和 breadcrumb 表达访问路径；页面栈 MUST NOT 写入 Graph 资产，也 MUST NOT 参与 runtime。

#### Scenario: 节点下钻
- **WHEN** 用户从节点的 Graph 引用打开子 Graph
- **THEN** 编辑器 MUST 打开该 `BaseTree` 引用
- **AND** 当前窗口 MAY push 页面栈以记录来源节点和引用 key

#### Scenario: 保存 Graph
- **WHEN** 用户保存当前 Graph
- **THEN** 页面栈、breadcrumb 和返回状态 MUST NOT 序列化到 `BaseTree`、`BaseNode` 或 `NodeModule`

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期
系统 MUST 允许 `BaseGraph` 保存非序列化运行上下文，包括 `User`、`DeltaTime` 和类型化上下文读取能力。`BaseGraph` MUST NOT 拥有 `Running`、`State`、`UpdateTree` 或 `ResetTree`。

#### Scenario: RunnableTree tick
- **WHEN** `RunnableTree.UpdateTree(deltaTime)` 被调用
- **THEN** 它 MUST 将 `deltaTime` 写入继承自 `BaseGraph` 的上下文
- **AND** 节点执行生命周期 MUST 仍由 `RunnableTree` 表达

#### Scenario: StateMachineGraphRuntime tick
- **WHEN** `StateMachineGraphRuntime.Update(deltaTime)` 解释 `StateMachineGraph`
- **THEN** 它 MUST 将 `deltaTime` 写入该 Graph 的 `BaseGraph.DeltaTime`
- **AND** 它 MUST NOT 要求 `StateMachineGraph` 继承 `RunnableTree`

#### Scenario: 子 Graph 继承上下文
- **WHEN** 节点初始化下钻 Graph
- **THEN** 子 Graph MUST 接收父 Graph 的正式 `User`
- **AND** 系统 MUST NOT 使用父节点、父 Graph 或 runner 自身作为 fallback 上下文

### Requirement: 不新增 Graph 分裂路径
系统 MUST 保持一套图数据、一套 Taco 原生端口系统和一套编辑器资产入口。系统 MUST NOT 因 `BaseGraph` 抽层新增 Workbench 图、并行端口协议、旧数据 fallback 或重复序列化集合。

#### Scenario: 结构链路唯一
- **WHEN** 新 Graph 能力接入 Taco
- **THEN** 它 MUST 使用现有 `BaseGraph` 集合、`PropertyPort` / `PropertyEdge` 和 `BaseTree` 编辑入口
- **AND** 它 MUST NOT 新增并行 Workbench 或 fallback 数据链路

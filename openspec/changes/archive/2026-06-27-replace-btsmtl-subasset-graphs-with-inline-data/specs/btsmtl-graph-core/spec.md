# btsmtl-graph-core Specification Delta

## MODIFIED Requirements
### Requirement: BaseGraph 承载唯一图结构数据
系统 MUST 使用普通 C# 可序列化的 `BaseGraph` 保存节点、普通边、属性边、暴露属性和对应 GUID 映射。节点、普通边、属性边、模块、端口和私有下钻 Graph MUST 作为所属 owner 的内联序列化数据保存。`BaseTreeAsset` 或其它 Unity asset 外壳 MUST NOT 再保存第二套节点、边或属性边集合。

#### Scenario: 编辑器读取图数据
- **WHEN** BTSMTL 编辑器打开 graph asset
- **THEN** 编辑器 MUST 从该 asset 外壳持有的正式 `BaseGraph` 数据读取节点、边和属性边
- **AND** 系统 MUST NOT 从并行集合恢复 GraphView

#### Scenario: 节点和边内联保存
- **WHEN** 用户在 Graph 中创建节点或连接普通边
- **THEN** 节点 MUST 保存于该 Graph 的节点集合
- **AND** 普通边 MUST 保存于该 Graph 的边集合
- **AND** 系统 MUST NOT 为普通节点或普通边创建 Unity asset 或 sub-asset

#### Scenario: 私有下钻 Graph 内联保存
- **WHEN** 用户创建拥有私有下钻 Graph 的节点或边
- **THEN** 私有下钻 Graph MUST 保存为该节点或边内部的普通 C# 图数据
- **AND** 系统 MUST NOT 为该私有下钻 Graph 创建 Unity asset 或 sub-asset

### Requirement: BaseTreeAsset 保持资产和编辑器入口
系统 MUST 保持 `BaseTreeAsset` 或等价 graph asset 类型作为 Unity Project、Inspector 和 BTSMTL 编辑器可打开入口。`BaseTreeAsset` MUST 作为 asset 外壳持有正式 `BaseTree` / `BaseGraph` 图数据，`BaseTree` 和 `BaseGraph` MUST NOT 混入 Unity asset 身份。直接打开 Graph asset MUST 继续通过 `OpenTree()` 或等价 `TreeWindowUtility` 入口打开，不新增并行 `BaseGraphWindow`。

#### Scenario: 直接打开资产
- **WHEN** 用户从 Project、Inspector 或 Tree Browser 打开 graph asset
- **THEN** BTSMTL MUST 以该 asset 持有的 `BaseGraph` 数据作为当前编辑窗口的根 Graph
- **AND** 打开流程 MUST NOT 需要来源节点上下文
- **AND** 编辑器 MUST NOT 创建第二套 graph window 或 Workbench window

### Requirement: Graph 引用和页面栈保持 editor-only
系统 MUST 让节点、边或模块通过正式 graph reference 表达下钻 Graph。graph reference MUST 支持默认内联 graph data 和显式 shared graph asset。当前窗口内从引用下钻时，编辑器 MAY 使用页面栈和 breadcrumb 表达访问路径；页面栈 MUST NOT 写入 Graph 数据、节点、边或模块，也 MUST NOT 参与 runtime。

#### Scenario: 节点下钻到内联 Graph
- **WHEN** 用户从节点的 inline graph reference 打开子 Graph
- **THEN** 编辑器 MUST 打开该节点内部持有的 `BaseGraph` 数据
- **AND** 当前窗口 MAY push 页面栈以记录来源节点和引用 key

#### Scenario: 节点下钻到 shared Graph
- **WHEN** 用户从节点的 shared graph reference 打开子 Graph
- **THEN** 编辑器 MUST 打开 shared graph asset 持有的 `BaseGraph` 数据
- **AND** UI MUST 显示该引用是 `Shared Asset`

#### Scenario: 保存 Graph
- **WHEN** 用户保存当前 Graph
- **THEN** 页面栈、breadcrumb 和返回状态 MUST NOT 序列化到 Graph 数据、节点、边或模块

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期
系统 MUST 允许 `BaseGraph` 保存非序列化运行上下文，包括 `User`、`DeltaTime` 和类型化上下文读取能力。`BaseGraph` MUST NOT 拥有 `Running`、`State`、`UpdateTree` 或 `ResetTree`。运行时必须从 resolved authoring graph data 创建运行工作副本，MUST NOT 依赖 `Object.Instantiate(ScriptableObject graph)` 作为通用图运行机制。

#### Scenario: RunnableTree tick
- **WHEN** `RunnableTree.UpdateTree(deltaTime)` 被调用
- **THEN** 它 MUST 将 `deltaTime` 写入正式 `BaseGraph` 运行上下文
- **AND** 节点执行生命周期 MUST 仍由 `RunnableTree` 表达

#### Scenario: StateMachineGraphRuntime tick
- **WHEN** `StateMachineGraphRuntime.Update(deltaTime)` 解释 `StateMachineGraph`
- **THEN** 它 MUST 将 `deltaTime` 写入运行工作副本的 `BaseGraph.DeltaTime`
- **AND** 它 MUST NOT 要求 `StateMachineGraph` 继承 `RunnableTree`

#### Scenario: 子 Graph 继承上下文
- **WHEN** 节点初始化下钻 Graph 运行工作副本
- **THEN** 子 Graph MUST 接收父 Graph 的正式 `User`
- **AND** 系统 MUST NOT 使用父节点、父 Graph 或 runner 自身作为 fallback 上下文

### Requirement: 私有下钻 Graph 默认 inline data
系统 MUST 将“默认创建即私有可编辑”作为下钻 Graph 的创作心智。用户创建拥有下钻内容的 owner 节点或边时，编辑器 MUST 自动创建普通 C# 内联 graph data 并绑定到 owner。用户 MUST NOT 被要求先手动创建、保存或拖拽一个 Graph asset 才能使用新建节点或边。

#### Scenario: 创建拥有下钻 Graph 的节点
- **WHEN** 用户创建需要下钻 Graph 的节点
- **THEN** 编辑器 MUST 自动创建该节点私有的 inline graph data
- **AND** 节点或模块 MUST 保存该 inline graph data
- **AND** 用户 MUST 能立即通过双击或 `Open` 命令进入该 Graph
- **AND** 创建流程 MUST NOT 要求 owner graph asset 已保存

#### Scenario: 创建拥有下钻 Graph 的边
- **WHEN** 用户为边创建私有规则或其它下钻 Graph
- **THEN** 编辑器 MUST 在该边内部创建 inline graph data
- **AND** 边 MUST 保存该 inline graph data
- **AND** 系统 MUST NOT 创建 subasset

#### Scenario: 显式复用 Graph
- **WHEN** 用户需要复用某个私有 Graph
- **THEN** 用户 MUST 通过 `Extract Shared`、`Create Shared` 或显式分配已有 asset 将其作为 shared asset 使用
- **AND** UI MUST 显示该引用是 `Shared Asset`
- **AND** 系统 MUST 清理 owner 内联副本，避免 inline 和 shared 同时作为真数据存在
- **AND** 系统 MUST NOT 把 shared asset 当作 owner 私有数据删除

#### Scenario: 删除 owner
- **WHEN** 用户删除拥有 inline Graph 的节点或边
- **THEN** inline Graph MUST 随 owner 序列化数据一起被删除
- **AND** 系统 MUST NOT 执行 subasset 删除
- **AND** 如果引用的是 shared asset，系统 MUST 只删除 owner 或断开引用，不删除 shared asset

### Requirement: 下钻引用 UI 表达编辑意图
系统 MUST 让默认下钻操作表现为 `Open`、双击或等价下钻命令。Inspector 可以配置引用 ownership、shared asset 和抽取复用，但 MUST NOT 把“创建 inline graph”作为普通节点初始化入口。普通节点或边创建后若必须拥有私有 Graph，创建流程 MUST 已经完成 inline graph 初始化。

#### Scenario: 默认下钻
- **WHEN** 用户创建拥有私有下钻 Graph 的节点
- **THEN** UI MUST 提供 `Open` 或双击下钻入口
- **AND** UI MUST NOT 要求用户在 Inspector 中点击 `Create Inline` 才能使用该节点

#### Scenario: 选中 owner 查看引用
- **WHEN** 用户选中拥有 graph reference 的节点或边
- **THEN** 左侧 Inspector MUST 显示当前引用是 `Inline`、`Shared Asset` 或 `Missing`
- **AND** shared asset 选择、抽取复用和清除引用 MUST 只作为显式复用/解绑操作出现
- **AND** 当当前引用不是 `Shared Asset` 时，Inspector MUST NOT 显示值为 `None` 的 shared asset 字段
- **AND** 节点画布本体 MUST NOT 因 shared graph 字段暴露而强制显示配置齿轮

#### Scenario: 编辑节点显示名
- **WHEN** 用户选中任意节点
- **THEN** 左侧 Inspector MUST 提供可编辑的 `Display Name`
- **AND** 空 `Display Name` MUST 回退到节点类型显示名
- **AND** 画布节点标题、Inspector 标题和 Transition 端点显示 MUST 使用同一解析后的显示名

#### Scenario: 从 inline 抽取 shared asset
- **WHEN** 用户显式执行 `Extract Shared`
- **THEN** 系统 MUST 从当前 inline graph data 创建独立 `BaseTreeAsset`
- **AND** owner MUST 切换到 shared asset 引用
- **AND** owner MUST 清理原 inline 真数据

## ADDED Requirements
### Requirement: Shared Graph Asset 只是复用外壳
系统 MUST 允许 graph data 被显式保存到独立 ScriptableObject asset 以支持复用。Shared graph asset MUST 只作为项目文件、复用和直接打开入口，不得成为默认私有 graph 的保存方式。

#### Scenario: 创建 shared asset
- **WHEN** 用户显式创建 shared graph asset
- **THEN** 系统 MUST 创建独立 ScriptableObject asset
- **AND** 该 asset MUST 持有一份正式 `BaseGraph` 数据
- **AND** 该 asset MUST 能被 Project、Inspector 或 BTSMTL 编辑器直接打开

#### Scenario: 使用 shared asset
- **WHEN** 节点、边或模块引用 shared graph asset
- **THEN** resolved graph MUST 来自该 asset 持有的 `BaseGraph` 数据
- **AND** owner MUST NOT 再持有同一引用的 inline 真数据

### Requirement: Graph 运行工作副本来自数据克隆
系统 MUST 为运行时创建 graph data 工作副本。多个运行实例引用同一 inline graph template 或 shared graph asset 时，它们 MUST 拥有互相隔离的运行状态。

#### Scenario: 多个运行实例引用同一 shared graph
- **WHEN** 两个角色或两个节点同时运行同一个 shared graph asset
- **THEN** 每个运行实例 MUST 获得独立工作副本
- **AND** 一个实例的节点状态、暴露属性临时值或运行上下文 MUST NOT 污染另一个实例

#### Scenario: inline graph 运行
- **WHEN** 节点运行自己持有的 inline graph data
- **THEN** runtime MUST 从该 inline graph data 创建工作副本
- **AND** runtime MUST NOT 直接修改 authoring graph data 的序列化字段

# btsmtl-graph-core Specification

## Purpose
定义 BTSMTL 图底座：`BaseGraph` 承载唯一图结构数据、编辑操作和运行上下文；`BaseTree : BaseGraph` 是普通 C# 图数据类型；`BaseTreeAsset` 作为 Unity 资产和编辑器入口持有一份 `BaseTree` 数据；节点、边、模块、端口和默认私有下钻 Graph 都内联在所属 owner 中，只有显式复用时才使用 shared asset；执行生命周期留在 `RunnableTree`、`StateMachineGraphRuntime`、`TimelineNode` 等上层 Module。
## Requirements
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

### Requirement: BaseGraph 承载结构编辑操作
系统 MUST 在 `BaseGraph` 上提供正式结构编辑操作，包括创建/删除节点、连接/断开普通边、连接/断开属性边、刷新和初始化清理。所有入口 MUST 使用同一套集合和 GUID 映射。

#### Scenario: 创建节点
- **WHEN** 编辑器、粘贴流程或脚本请求创建节点
- **THEN** 请求 MUST 通过当前 Graph 的正式创建逻辑
- **AND** 新节点 MUST 被加入同一节点集合和 GUID 映射

#### Scenario: 连接属性端口
- **WHEN** 用户连接两个 `PropertyPort`
- **THEN** 系统 MUST 创建 BTSMTL 原生 `PropertyEdge`
- **AND** 连接 MUST 保存在正式属性边集合中

### Requirement: 节点创建尊重图类型规则
系统 MUST 让 `BaseGraph.CreateNode(Type)` 尊重当前图的 `CanCreateNodeType(Type)`。节点搜索、拖拽、粘贴和脚本创建 MUST 不绕过该规则。`StateMachineGraph` MUST 只接收状态结构节点；`ConditionRuleGraph` MUST 只接收纯条件求值节点。

#### Scenario: StateMachineGraph 拒绝非法节点
- **WHEN** 创建路径尝试向 `StateMachineGraph` 创建 `StateMachineNode`、`RootNode`、普通 runnable 节点或条件 `ValueNode`
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 系统 MUST NOT 把该节点加入正式节点集合

#### Scenario: ConditionRuleGraph 接受条件节点
- **WHEN** 创建路径尝试向 `ConditionRuleGraph` 创建 InputAction、黑板读取、Value、Compare、Logic 或 `ConditionRuleResultNode`
- **THEN** 创建逻辑 MUST 允许该节点作为规则图求值节点
- **AND** 这些节点 MUST 继续使用正式字段访问器和 typed `PropertyPort`

#### Scenario: ConditionRuleGraph 拒绝行为节点
- **WHEN** 创建路径尝试向 `ConditionRuleGraph` 创建 `RunnableNode`、`TimelineNode`、`StateMachineNode`、`StateNode` 或状态机控制节点
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 系统 MUST NOT 把该节点加入正式节点集合

### Requirement: BaseTreeAsset 保持资产和编辑器入口
系统 MUST 保持 `BaseTreeAsset` 或等价 graph asset 类型作为 Unity Project、Inspector 和 BTSMTL 编辑器可打开入口。`BaseTreeAsset` MUST 作为 asset 外壳持有正式 `BaseTree` / `BaseGraph` 图数据，`BaseTree` 和 `BaseGraph` MUST NOT 混入 Unity asset 身份。直接打开 Graph asset MUST 继续通过 `OpenTree()` 或等价 `TreeWindowUtility` 入口打开，不新增并行 `BaseGraphWindow`。

#### Scenario: 直接打开资产
- **WHEN** 用户从 Project、Inspector 或 Tree Browser 打开 graph asset
- **THEN** BTSMTL MUST 以该 asset 持有的 `BaseGraph` 数据作为当前编辑窗口的根 Graph
- **AND** 打开流程 MUST NOT 需要来源节点上下文
- **AND** 编辑器 MUST NOT 创建第二套 graph window 或 Workbench window

### Requirement: Graph 引用和页面栈保持 editor-only

系统 MUST 让节点、边、模块、TimelineNode 和 Timeline Clip 通过正式 authoring reference 表达下钻内容。默认私有 Graph 和 Timeline MUST 支持 inline data，需要复用时才显式使用 shared asset。BaseTreeWindow 的作者页面栈 MUST 只支持 Graph page 和 TreeClip resolved Graph page；Timeline MUST 由独立 TimelineEditorWindow 编辑，不得进入 Graph breadcrumb。页面栈、窗口绑定、selection restore 和来源 identity MUST 保持 editor-only，不得参与 runtime 或序列化到业务数据。

#### Scenario: 节点下钻到内联 Graph

- **WHEN** 用户从节点的 inline graph reference 打开子 Graph
- **THEN** 编辑器 MUST push 该节点内部持有的 Graph page
- **AND** page entry MUST记录来源节点和引用 key

#### Scenario: 节点下钻到 shared Graph

- **WHEN** 用户从节点的 shared graph reference 打开子 Graph
- **THEN** 编辑器 MUST push shared graph asset 持有的 Graph page
- **AND** UI MUST显示该引用是 Shared Asset

#### Scenario: TimelineNode 下钻到 inline Timeline

- **WHEN** 用户从 TimelineNode 执行 Open 或双击
- **THEN** 来源 Graph 窗口 MUST保持当前 Graph page 不变
- **AND** 独立 TimelineEditorWindow MUST绑定该节点持有的 TimelineData
- **AND** TimelineEditorWindow MUST保存 serialized owner/path 与来源 authoring context
- **AND** Timeline MUST NOT进入 Graph 页面栈或 breadcrumb

#### Scenario: TimelineNode 下钻到 shared Timeline

- **WHEN** TimelineNode 使用 Shared Asset ownership 并执行 Open
- **THEN** 独立 TimelineEditorWindow MUST绑定 shared TimelineAsset 的 TimelineData
- **AND** UI MUST显示当前来源为 Shared Asset
- **AND** 来源 Graph 的 authoring context MUST继续可用于 TreeClip 下钻

#### Scenario: Timeline 下钻到 TreeClip

- **WHEN** 用户从 TimelineEditorWindow 打开 TreeClip
- **THEN** 来源 Graph 窗口 MUST push resolved TimelineRunningTree Graph page
- **AND** TimelineEditorWindow MUST保持当前 Timeline 可见
- **AND** Graph breadcrumb MUST只表达 Graph 与 TreeClip 来源路径，不得加入 Timeline page
- **AND** TreeClip Graph page MUST继承可见 Blackboard declarations

#### Scenario: 保存双窗口内容

- **WHEN** 用户在 Graph、TreeClip Graph 或 TimelineEditorWindow 修改数据
- **THEN** dirty 与 Undo MUST作用于当前数据的真实 serialized owner
- **AND** Graph 页面栈、Timeline 窗口绑定、breadcrumb、preview state 和返回位置 MUST NOT序列化到 Graph、TimelineData、TimelineAsset、节点、Track 或 Clip

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

### Requirement: BaseGraph 承载运行上下文但不承担执行生命周期

系统 MUST允许 `BaseGraph` 保存非序列化运行上下文，包括 `User`、`DeltaTime`和类型化上下文读取能力。`BaseGraph` MUST NOT拥有 `Running`、`State`、`UpdateTree`或 `ResetTree`。通用 BTSMTL解释器 MAY从 resolved authoring graph data创建隔离运行工作副本，但正式 Character runtime MUST将同一 authoring编译为 `CharacterSimulationProgram`，并由 Session Pipeline的标准 Program Step Pass执行，不得通过 `RunnableTree`、`StateMachineGraphRuntime`或运行时 Graph clone执行角色 Gameplay。两种用途 MUST不共享或回写运行状态。

#### Scenario: Character 正式运行

- **WHEN** CharacterPipelineDefinition已生成有效 Program artifact且 Session Pipeline进入 Active
- **THEN** Program Evaluate/Finalize Pass MUST只执行 Program operation
- **AND** MUST不创建 BaseGraph运行工作副本或调用通用解释器

#### Scenario: 非角色通用 RunnableTree tick

- **WHEN** 非 Character组合显式调用 `RunnableTree.UpdateTree(deltaTime)`
- **THEN** 它 MUST将 `deltaTime`写入自己的隔离 `BaseGraph`运行上下文
- **AND** MUST不读取 CharacterSimulationState或注册 Character Session Pipeline Pass

### Requirement: 不新增 Graph 分裂路径

系统 MUST在BTSMTL领域保持一套`BaseGraph`数据、一套`PropertyPort`/`PropertyEdge`端口系统和一套`BaseTreeAsset`入口。StateMachineGraph、ConditionRuleGraph和BT edge decorator MUST继续使用该正式BTSMTL链路，不得新增Workbench、并行BTSMTL端口协议、旧数据fallback或重复序列化集合。跨领域的Character Presentation Pose Graph MUST使用独立Pose数据、typed Pose端口、validator和compiler，同时与BTSMTL共享唯一Graph Authoring Domain Framework中的Canvas、Node/Port View、selection、clipboard、Details、Navigator与StateMachine作者交互。共享层 MUST只消费domain document、Capability、typed payload、port policy和mutation adapter；Pose MUST不继承BTSMTL runtime node/edge语义，也 MUST不成为第二个BTSMTL Graph执行路径。

#### Scenario: BTSMTL新增规则图能力

- **WHEN** StateMachine Transition或BT edge decorator需要条件求值图
- **THEN** 它 MUST继续使用ConditionRuleGraph、PropertyPort和BaseTree authoring入口
- **AND** MUST不使用Pose Graph或通用Shell payload代替BTSMTL数据

#### Scenario: 打开Presentation Pose Graph

- **WHEN** 作者通过共享Editor Shell打开Pose Graph asset
- **THEN** Shell MUST装配Pose domain document与port policy
- **AND** MUST不创建BaseGraph、BaseNode、BaseEdge、Blackboard或runtime evaluation context

#### Scenario: 复用节点编辑交互

- **WHEN** BTSMTL Graph和Pose Graph都需要搜索、clipboard、Undo和Inspector宿主
- **THEN** 两者 MUST复用同一Graph Authoring Editor Shell实现
- **AND** 每个领域 MUST只修改自己的正式serialized owner

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

### Requirement: TreeWindow 支持 editor-only authoring context
系统 MUST 允许 `BaseTreeWindow` 持有 editor-only authoring context，用于 Tree Inspector 中依赖业务上下文的 authoring 区块展示当前打开入口提供的信息。该 context MUST NOT 序列化到 `BaseGraph`、`BaseTree`、`BaseTreeAsset`、节点、边或 property port 中。下钻 inline graph 或 shared graph 时，窗口 MUST 保持同一个 authoring context。

#### Scenario: 从业务定义打开 RootTree
- **WHEN** editor 通过业务定义打开某个 `BaseTreeAsset`
- **THEN** `BaseTreeWindow` MUST 接收该业务定义提供的 authoring context
- **AND** Graph 数据本身 MUST NOT 保存该 context

#### Scenario: 直接打开孤立 TreeAsset
- **WHEN** 用户直接打开一个普通 `BaseTreeAsset`
- **THEN** `BaseTreeWindow` MAY 没有业务 authoring context
- **AND** Inspector 中依赖业务 context 的区块 MUST 显示缺失上下文状态，而不是写入 fallback 配置

#### Scenario: 下钻 Graph
- **WHEN** 用户从 RootTree 下钻到 inline graph、shared graph 或 transition rule graph
- **THEN** 子页面 MUST 继承当前窗口的 authoring context
- **AND** 子 Graph MUST NOT 单独保存一份 context

### Requirement: BaseGraph declaration 必须保持局部所有权并支持显式外层引用

每个 `BaseGraph` MUST 只序列化自己拥有的 `BaseExposedProperty` declarations。Graph 节点 MAY 通过正式 variable reference 引用 authoring context 中可见的外层 declaration，但该 reference MUST NOT 把 declaration 复制进当前 Graph。Graph 克隆、inline ownership 和 shared asset 解析 MUST 保持 declaration identity 与 owner 关系。

#### Scenario: inline graph 创建局部 declaration

- **WHEN** 作者在 State body inline Graph 中创建 Graph scope declaration
- **THEN** declaration MUST 保存于该 inline Graph 的 exposed property 集合
- **AND** owner StateNode 被删除时该 declaration MUST 随 inline Graph 删除

#### Scenario: inline graph 引用 RootTree declaration

- **WHEN** inline Graph 中的节点引用 RootTree Character declaration
- **THEN** inline Graph MUST 只保存 variable reference
- **AND** inline Graph 的 exposed property 集合 MUST NOT 增加该 Character declaration 副本

#### Scenario: shared graph 运行实例

- **WHEN** 两个 owner 运行同一个 shared Graph
- **THEN** shared Graph declaration identity MUST 保持一致
- **AND** Graph scope runtime value MUST 由各自运行工作副本 identity 隔离

### Requirement: Graph evaluation context 必须携带变量访问所有权

Graph runtime 和下钻 evaluation context MUST 能向统一 blackboard resolver 提供当前 Graph runtime、active State、ActionInstance 和 local logic tick ownership。节点 MUST NOT 自行拼接字符串地址或从 asset path 推断 runtime owner。缺少 declaration 所需 owner 时读取或写入 MUST 失败。

#### Scenario: ConditionRuleGraph 继承 active State

- **WHEN** StateMachine runtime 求值 active State 的 Transition rule
- **THEN** ConditionRuleGraph MUST 继承 owner StateMachineGraph 的 runtime context 与 active `StateMachineExecutionScope`
- **AND** State scope variable reference MUST 解析到当前 activation bucket

#### Scenario: 孤立 Graph 缺少 Action context

- **WHEN** Graph 在没有 `ActionInstanceId` 的上下文中读取 ActionInstance scope declaration
- **THEN** resolver MUST 报告缺失 owner context
- **AND** 系统 MUST NOT 回退到 Character、Graph 或默认值

### Requirement: TreeClip 私有下钻 Graph 必须默认 inline

Timeline TreeClip 作为拥有下钻 Graph 的 authoring owner 时，编辑器 MUST 自动创建并保存 inline `TimelineRunningTree` graph data。作者需要复用时 MAY 显式 Extract Shared 到 `BaseTreeAsset`。Inline 与 shared MUST 共享同一 resolved authoring graph 合同，并且同一 TreeClip 只能有一个真数据来源。`TimelineRunningTree` 在正式 Character runtime 中 MUST 只作为 Compiler 输入，不能被克隆为 playback runtime。

#### Scenario: 新建 TreeClip

- **WHEN** 作者在 Timeline 中创建 TreeClip
- **THEN** Clip MUST 自动拥有 inline TimelineRunningTree authoring data
- **AND** 作者 MUST 能通过双击或 Open 下钻编辑
- **AND** 创建流程 MUST NOT 弹出或要求分配 BaseTreeAsset

#### Scenario: 抽取 shared Tree

- **WHEN** 作者对 inline TreeClip 执行 Extract Shared
- **THEN** 系统 MUST 创建持有同一 Graph data 的 shared BaseTreeAsset
- **AND** TreeClip MUST 切换到 shared 引用
- **AND** 原 inline 真数据 MUST 被清理

#### Scenario: 多 playback 使用同一 TreeClip

- **WHEN** 多个 Timeline playback 使用同一 inline 或 shared TimelineRunningTree authoring template
- **THEN** Compiler MUST 让它们引用同一不可变 operation/catalog 数据
- **AND** 每个 playback/clip activation MUST 在 CharacterSimulationState 中获得独立 state address
- **AND** 系统 MUST 不创建 TimelineRunningTree runtime clone

### Requirement: Graph 必须拥有统一稳定 authoring identity

每个 `BaseGraph` MUST 持有稳定 `GraphAuthoringId`，Node 和 Edge MUST 继续持有各自稳定 authoring GUID。Graph runtime clone MUST 保留这些 source identities，但 MUST 使用独立 runtime instance identity。Pipeline Blackboard declaration owner、Agent Snapshot、Debug Source Map 和 editor navigation MUST 引用同一个 Graph authoring identity。

#### Scenario: 创建 inline Graph

- **WHEN** owner 创建新的 inline Graph
- **THEN** Graph MUST 获得新的稳定 `GraphAuthoringId`
- **AND** Graph 内 Node/Edge MUST 获得各自稳定 identity

#### Scenario: 创建 runtime clone

- **WHEN** runtime 从 authoring Graph 创建工作副本
- **THEN** clone MUST 保留 Graph/Node/Edge authoring identity
- **AND** clone MUST 获得新的 runtime instance identity

#### Scenario: 迁移 Blackboard owner identity

- **WHEN** 实现将旧 `BlackboardOwnerId` 提升为 `GraphAuthoringId`
- **THEN** 现有 declaration owner reference MUST 一次性迁移到同一 identity value
- **AND** 旧字段、旧 API 和第二份 debug Graph id MUST 删除

### Requirement: Graph 运行时初始化必须收敛到统一非虚入口

明确保留的非 Character 通用解释器 MAY 通过 `BaseGraph` 公开非虚入口完成 root/nested route、runtime identity、节点、边和通用上下文初始化。正式 Character runtime MUST 不调用该入口；Character Graph、StateMachine 与 Timeline TreeClip 必须由 Compiler 解析为 Program operation。`TimelineRunningTree` MUST 不再提供 Character gameplay 专用运行时初始化入口。

#### Scenario: 初始化非 Character 嵌套 Graph

- **WHEN** 明确装配的通用解释器初始化子 Graph
- **THEN** 统一入口 MUST 先建立 parent/route
- **AND** 派生节点引用 MUST 在核心 maps 建立后解析
- **AND** 该工作副本 MUST 与 Character state 隔离

#### Scenario: 编译 Character Timeline TreeClip

- **WHEN** Compiler 解析 TimelineRunningTree authoring data
- **THEN** Compiler MUST 校验 TreeClip owner、clip identity、Blackboard reference 与 operation emitter
- **AND** MUST 不调用 `InitTimelineTree` 或普通 `InitTree`

#### Scenario: 尝试运行时初始化 Character TreeClip

- **WHEN** Character runtime 尝试创建或初始化 TimelineRunningTree 工作副本
- **THEN** 组合或编译校验 MUST 明确失败
- **AND** 系统 MUST 不创建半初始化 Graph 或 fallback context

### Requirement: TreeWindow runtime 状态必须通过只读 diagnostics overlay 表达

`BaseTreeWindow` MUST 继续绑定 authoring Graph，并通过 `RuntimeDebugSession` 和 source identity 显示选中 runtime instance 的 Node、Edge、StateMachine 和生命周期状态。TreeWindow MUST NOT 打开 runtime clone 作为 authoring page，也 MUST NOT 直接读取 authoring Node 的 runtime `State` 字段。

#### Scenario: Live Debug 高亮运行节点

- **WHEN** Session 为当前 Graph source 提供匹配 revision 的 Node execution snapshot
- **THEN** 对应 NodeView MUST 显示 Running、Success、Failure、Stopping 或其它正式 debug 状态
- **AND** authoring Node 数据 MUST 不被修改

#### Scenario: 下钻运行中的 inline Graph

- **WHEN** 用户从 authoring Graph 下钻 StateMachine、State body、ConditionRuleGraph 或 TreeClip Graph
- **THEN** 页面栈 MUST 继续打开对应 authoring Graph
- **AND** overlay MUST 使用当前 Session 选中的 runtime child instance
- **AND** 页面栈 MUST NOT 保存 runtime object reference

#### Scenario: 旧 direct-state 高亮

- **WHEN** 新 diagnostics overlay 接管 NodeView runtime 状态
- **THEN** `BaseNodeView` 直接读取 `RunnableNode.State` 的旧高亮路径 MUST 删除
- **AND** 窗口 MUST NOT 保留两套节点运行状态来源

### Requirement: Graph节点兼容性必须由稳定Authoring Capability裁决

每个可进入受限Graph的节点类型 MUST声明稳定authoring capability。Graph Role MUST通过唯一policy定义允许的capability；`CanCreateNodeType`、Node Search、拖拽、粘贴、脚本创建与Compiler Validator MUST复用该policy。系统 MUST为后续自动authoring暴露同一只读policy查询，但本change MUST NOT修改Agent schema。系统 MUST NOT按NodePath字符串、显示名、继承层次或窗口类型猜测节点兼容性。

#### Scenario: AI Graph创建Character动作节点

- **WHEN** 搜索、粘贴、脚本或Compiler尝试在AIControllerTree创建CharacterExecution节点
- **THEN** 统一Graph policy MUST拒绝该节点
- **AND** Graph数据 MUST不发生修改

#### Scenario: AI Graph创建共享纯值节点

- **WHEN** 作者在AIControllerTree创建声明为SharedPureValue的Compare节点
- **THEN** 统一Graph policy MUST允许该节点
- **AND** Editor与Compiler MUST读取同一capability identity

#### Scenario: AI节点缺少能力声明

- **WHEN** 未声明authoring capability的新节点尝试进入AIControllerTree
- **THEN** 创建与发布 MUST失败并报告节点类型和Graph Role
- **AND** 系统 MUST不按默认Base节点处理

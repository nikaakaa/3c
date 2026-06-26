# taco-sm-node-authoring Specification

## REMOVED Requirements

### Requirement: StateNode 在状态机图中支持 Transition 和行为输出
系统 MUST NOT 继续让 `StateNode` 在 `StateMachineGraph` 中提供 `Behavior` flow port。状态具体行为 MUST 通过 `StateNode` 引用的 `SubTree` 表达。

#### Scenario: 删除 inline behavior
- **WHEN** 用户打开 `StateMachineGraph`
- **THEN** `StateNode` MUST NOT 暴露 `Behavior` port
- **AND** `StateMachineGraph` MUST NOT 允许 `StateNode.Behavior -> RunnableNode.Input` 连接

### Requirement: StateNode 行为来源互斥
系统 MUST NOT 继续支持 inline behavior 与 child graph 双来源模型。`StateNode` 的正式行为来源 MUST 是 `SubTree` 引用。

#### Scenario: 单一行为来源
- **WHEN** 用户配置 `StateNode` 行为
- **THEN** 用户 MUST 通过 `SubTree` 引用配置状态行为
- **AND** 系统 MUST NOT 再验证 inline behavior 与 child graph 的互斥关系

## MODIFIED Requirements

### Requirement: StateMachineGraph 承载状态机图语义
系统 MUST 保持普通 `BaseTree` 的 Taco 原有图语义。`StateMachineGraph` MUST 只表达状态机结构，允许 `Enter`、`AnyState`、`Exit`、`StateNode` 和条件用 `ValueNode`。`StateMachineGraph` MUST NOT 创建 Taco 原生 `RootNode`、`OnEnter`、`OnExit`、状态机 Root 节点、`StateMachineNode` 或普通 runnable 行为节点。

#### Scenario: 新建状态机 Graph
- **WHEN** 用户创建 `StateMachineGraph` 资产
- **THEN** 新图 MUST 默认包含一个 `Enter`、一个 `AnyState` 和一个 `Exit`
- **AND** 新图 MUST NOT 默认包含 `RootNode`
- **AND** 新图 MUST NOT 默认包含状态机 Root 节点

#### Scenario: StateMachineGraph 创建节点的统一边界
- **WHEN** 用户、编辑器菜单、粘贴流程或脚本路径尝试向 `StateMachineGraph` 创建节点
- **THEN** 系统 MUST 统一通过 `CanCreateNodeType()` 判定可创建类型
- **AND** `StateMachineGraph` MUST 允许创建状态机控制节点、`StateNode` 和条件所需的 `ValueNode`
- **AND** `StateMachineGraph` MUST NOT 允许创建 `StateMachineNode`、Taco 原生 `RootNode` 或普通 Taco runnable 节点

### Requirement: 复用 Taco 树控制流端口
系统 MUST 保留 Taco 原有 `RunnableTree`、`RootNode`、`CompositeNode`、`DecoratorNode` 和 `RunnableNode` 生命周期控制流。状态机 MUST 复用现有 flow port view 和 `BaseEdge` 数据表达 Transition，MUST NOT 新增 `TransitionNode`、并行 port registry 或 `IRunnableGraph` 运行入口。

#### Scenario: 状态机图内只复用 Transition edge
- **WHEN** `StateNode` 位于 `StateMachineGraph` 中
- **THEN** 系统 MUST 将 `StateOut -> StateIn` edge 数据解释为状态机 Transition
- **AND** 系统 MUST NOT 将普通 `Output -> Input` edge 解释为状态机图内行为 flow

### Requirement: Flow port 声明支持 Graph 上下文
系统 MUST 允许节点根据所在 `BaseGraph` 生成 flow port 声明。默认节点 MUST 继续从现有 `InputAttribute` 和 `OutputAttribute` 生成声明。`StateNode` MUST 在 `StateMachineGraph` 中生成状态机专用 flow port。该机制 MUST 只改变 flow port 声明来源，MUST NOT 改变 `PropertyPort` 值口链路、`PropertyEdge` 序列化链路或 `BaseEdge` 数据模型。

#### Scenario: StateNode 在 StateMachineGraph 中
- **WHEN** `StateNode` 的 owner graph 是 `StateMachineGraph`
- **THEN** 编辑器 MUST 为它生成 `StateIn` flow port
- **AND** 编辑器 MUST 为它生成 `StateOut` flow port
- **AND** `StateIn` MUST 允许多条入边
- **AND** `StateOut` MUST 允许多条出边
- **AND** 编辑器 MUST NOT 为它生成 `Behavior` flow port

### Requirement: 状态机图边界完整性
每一层 `StateMachineGraph` MUST 且只能包含一个 `Enter`、一个 `AnyState` 和一个 `Exit`。每一层 `StateMachineGraph` MUST 至少包含一个 `StateNode`。`StateMachineGraph` MUST NOT 包含 Root 节点。

#### Scenario: 缺少 Enter
- **WHEN** 状态机 Graph 没有 `Enter`
- **THEN** 验证结果 MUST 报告缺少 Enter

#### Scenario: 缺少 AnyState
- **WHEN** 状态机 Graph 没有 `AnyState`
- **THEN** 验证结果 MUST 报告缺少 AnyState

#### Scenario: 缺少 Exit
- **WHEN** 状态机 Graph 没有 `Exit`
- **THEN** 验证结果 MUST 报告缺少 Exit

#### Scenario: 状态机图包含 Root
- **WHEN** 状态机 Graph 包含 Root 节点
- **THEN** 验证结果 MUST 报告非法状态机结构

### Requirement: SMNode 支持递归下钻
系统 MUST 允许 `StateMachineNode` 持有一个入口 `StateMachineGraph` 引用。系统 MUST 允许 `StateNode` 持有状态内部 `SubTree` 引用。层级状态机 MUST 通过该 `SubTree` 内部的 `StateMachineNode` 表达。引用模块 MUST NOT 接受 `StateMachineGraph` 作为 `StateNode` 的直接行为引用。

#### Scenario: 父级入口引用状态机图
- **WHEN** Locomotion `StateMachineNode` 引用 LocomotionGraph
- **THEN** LocomotionGraph MUST 是 `StateMachineGraph`
- **AND** 父级 `StateMachineNode` MUST 从 LocomotionGraph 的 `Enter` 入口开始解释

#### Scenario: StateNode 下钻到 SubTree
- **WHEN** 用户打开 Idle `StateNode`
- **THEN** 编辑器 MUST 打开 Idle 引用的 `SubTree`
- **AND** 用户 MUST 能在该 SubTree 中创建 Timeline 引用、Action、Value、BT 子图引用或另一个 `StateMachineNode`
- **AND** 该 SubTree MUST 包含 Taco 原生 `RootNode`

### Requirement: StateBehaviorSubTree 提供固定生命周期入口
系统 MUST 允许 `StateNode` 引用普通 `SubTree` 或 `StateBehaviorSubTree`。普通 `SubTree` MUST 只表达 Root 行为入口，不强制拥有 `OnEnter` 或 `OnExit`。`StateBehaviorSubTree` MUST 固定拥有 `OnEnter`、`RootNode` 和 `OnExit` 生命周期入口。`OnEnter` 和 `OnExit` MUST 使用 Taco 原生 flow port 连接普通 `RunnableNode`，MUST NOT 成为 `StateMachineGraph` 中的 Transition 端点。

#### Scenario: 新建普通 SubTree
- **WHEN** 用户创建普通 `SubTree`
- **THEN** 新图 MUST 默认包含一个 `RootNode`
- **AND** 新图 MUST NOT 默认包含 `OnEnter`
- **AND** 新图 MUST NOT 默认包含 `OnExit`

#### Scenario: 新建 StateBehaviorSubTree
- **WHEN** 用户创建 `StateBehaviorSubTree`
- **THEN** 新图 MUST 默认包含一个 `OnEnter`
- **AND** 新图 MUST 默认包含一个 `RootNode`
- **AND** 新图 MUST 默认包含一个 `OnExit`

#### Scenario: 生命周期节点唯一
- **WHEN** `StateBehaviorSubTree` 缺少或重复 `OnEnter`、`RootNode` 或 `OnExit`
- **THEN** 嵌套 Graph 校验 MUST 报告该生命周期入口非法

#### Scenario: 普通 SubTree 不承载生命周期入口
- **WHEN** 普通 `SubTree` 中存在 `OnEnter` 或 `OnExit`
- **THEN** 嵌套 Graph 校验 MUST 报告普通 `SubTree` 生命周期边界非法

#### Scenario: 进入状态先执行 OnEnter
- **WHEN** `StateMachineGraphRuntime` 切换到 Idle `StateNode`
- **AND** Idle 引用的是 `StateBehaviorSubTree`
- **THEN** Idle MUST 先 tick `OnEnter`
- **AND** `OnEnter` 完成后 MUST tick `RootNode`

#### Scenario: 离开状态前执行 OnExit
- **WHEN** Idle `StateNode` 的 Transition 命中 Walk 或 Exit
- **AND** Idle 引用的是 `StateBehaviorSubTree`
- **THEN** runtime MUST 先 tick Idle 的 `OnExit`
- **AND** `OnExit` 完成后 MUST 切换 active state 或完成本层状态机

#### Scenario: 普通 SubTree 状态没有回调
- **WHEN** Idle `StateNode` 引用的是普通 `SubTree`
- **THEN** runtime MUST 直接 tick 该 `SubTree` 的 `RootNode`
- **AND** 进入或离开 Idle 时 MUST NOT 查找 `OnEnter` 或 `OnExit`

### Requirement: Transition 是同层边语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内的 edge 语义。Transition MUST NOT 表达为单独节点。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。

#### Scenario: Enter 进入 Idle
- **WHEN** 用户连接 `Enter -> Idle`
- **THEN** 系统 MUST 将 `Enter.StateOut -> Idle.StateIn` 解释为当前层状态机的初始 Transition
- **AND** Idle MUST 是同层 `StateNode`

#### Scenario: 多来源进入同一状态
- **WHEN** Idle 同时可以从 Enter、Walk、Attack 或 AnyState 进入
- **THEN** Idle `StateNode.StateIn` MUST 允许多条入边
- **AND** 每条入边 MUST 保留为独立 Transition

### Requirement: Transition 端点规则
系统 MUST 验证 Transition 的起点和终点。合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`。普通 runnable 节点、`TimelineNode`、`StateMachineNode`、`RootNode` 和 `ValueNode` MUST NOT 成为 Transition 端点。

#### Scenario: 拖线阶段过滤非法端点
- **WHEN** 用户在 `StateMachineGraph` 中从 flow port 拖拽创建 Transition
- **THEN** 编辑器 MUST 只把合法 Transition 端点作为兼容 port 候选
- **AND** `StateNode.Behavior` MUST NOT 存在
- **AND** 该过滤 MUST NOT 影响 `PropertyPort` 值口连接

### Requirement: Timeline 通过状态行为链路接入
系统 MUST NOT 将 `TimelineNode` 作为 `StateMachineGraph` 同层状态节点或 Transition 端点创建。Timeline MUST 通过 `StateNode` 下钻到状态行为 `SubTree` 后接入。

#### Scenario: Idle 状态播放 Timeline
- **WHEN** Idle `StateNode` 需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** `TimelineNode` MUST 从所在 `SubTree` 获得 deltaTime

### Requirement: 状态机运行时解释
系统 MUST 让父级 Graph tick 到 `StateMachineNode` 时，由该 SMNode 负责进入并驱动自己引用的 `StateMachineGraph`，再把结果以 `Running/Success/Failure` 返回给父级 Graph。`StateMachineGraphRuntime` MUST 以 `StateNode` 作为 active state，并从 `Enter` 入口开始解释。active state 引用普通 `SubTree` 时 MUST tick `RootNode`；引用 `StateBehaviorSubTree` 时 MUST 按 `OnEnter -> RootNode -> OnExit` 生命周期执行。

#### Scenario: 父级行为图 tick Locomotion
- **WHEN** 父级行为图 tick 到 Locomotion `StateMachineNode`
- **THEN** Locomotion MUST 进入自己引用的 LocomotionGraph
- **AND** LocomotionGraph MUST 从 `Enter` 开始解释
- **AND** 父级行为图 MUST NOT 直接 tick IdleGraph 或 WalkGraph 内部节点

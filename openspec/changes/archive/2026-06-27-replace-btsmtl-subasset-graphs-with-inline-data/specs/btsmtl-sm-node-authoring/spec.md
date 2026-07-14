# btsmtl-sm-node-authoring Specification Delta

## MODIFIED Requirements
### Requirement: 状态机层级角色分离
系统 MUST 使用 `StateMachineNode` 表达父级行为图进入状态机图的入口，使用 `StateMachineGraph` 表达状态结构，使用 `StateNode` 表达状态机图内普通状态。`StateMachineNode` 本身 MUST 是父级 Graph 节点集合中的内联节点；它默认拥有的 `StateMachineGraph` MUST 是该节点内部的普通 C# 内联图数据。状态行为 MUST 位于 `StateNode` 内联持有或显式 shared 的 `SubTree` / `StateBehaviorSubTree` 图数据中。

#### Scenario: 父级行为图创建入口
- **WHEN** 用户在普通行为图中创建状态机入口
- **THEN** 创建结果 MUST 是 `StateMachineNode`
- **AND** 编辑器 MUST 自动创建并绑定一个 inline `StateMachineGraph` 数据
- **AND** 用户 MUST 能立即打开该状态机图
- **AND** 普通行为图 MUST NOT 创建 `StateNode`、`Enter`、`AnyState` 或 `Exit`

#### Scenario: 状态机图显式复用
- **WHEN** 用户需要多个 `StateMachineNode` 复用同一个状态机结构
- **THEN** 用户 MUST 通过显式 `Extract Shared`、`Create Shared` 或分配已有 `StateMachineGraph` asset 使用 shared graph
- **AND** UI MUST 显示该引用是 `Shared Asset`
- **AND** 删除 `StateMachineNode` 时 MUST NOT 删除 shared `StateMachineGraph` asset
- **AND** 切换到 shared graph 后 MUST 清理该节点的 inline graph 真数据

#### Scenario: 删除私有状态机入口
- **WHEN** 用户删除拥有 inline `StateMachineGraph` 的 `StateMachineNode`
- **THEN** 该 inline `StateMachineGraph` MUST 随节点序列化数据一起删除
- **AND** 系统 MUST NOT 执行 subasset 删除

#### Scenario: 状态机图创建状态
- **WHEN** 用户在 `StateMachineGraph` 中创建 Idle、Walk 或 Attack
- **THEN** 创建结果 MUST 是 `StateNode`
- **AND** `StateNode` MUST 保存为该 `StateMachineGraph` 的内联节点数据
- **AND** 系统 MUST NOT 创建业务特化状态节点

### Requirement: StateNode 下钻状态行为 SubTree
系统 MUST 允许 `StateNode` 通过正式状态行为 graph reference 拥有默认 inline 状态行为图数据，或显式引用 shared `SubTree` / `StateBehaviorSubTree` asset。默认创建状态行为图时 MUST 创建普通 C# 内联图数据并自动绑定。`StateNode` MUST NOT 在 `StateMachineGraph` 本层暴露 `Behavior` flow port，也 MUST NOT 直接引用子 `StateMachineGraph`。

#### Scenario: State 下钻到行为图
- **WHEN** 用户打开 Idle `StateNode` 的状态行为引用
- **THEN** 编辑器 MUST 打开该 StateNode resolved 状态行为图
- **AND** 用户 MUST 能在该图中创建 Timeline、Action、Composite、Decorator、Tree 引用或嵌套 `StateMachineNode`

#### Scenario: 创建私有状态行为图
- **WHEN** 用户从 `StateNode` 创建状态行为图
- **THEN** 系统 MUST 默认创建 inline `SubTree` 或 `StateBehaviorSubTree` graph data
- **AND** 系统 MUST 自动绑定到该 `StateNode`
- **AND** 用户 MUST NOT 被要求先手动创建、保存或拖拽一个 tree asset

#### Scenario: 显式复用状态行为图
- **WHEN** 多个 `StateNode` 需要复用同一份状态行为
- **THEN** 用户 MUST 显式创建、抽取或分配 shared tree asset
- **AND** 切换到 shared asset 后 MUST 清理该 StateNode 的 inline 状态行为真数据

#### Scenario: 没有状态行为
- **WHEN** active `StateNode` 没有配置状态行为图
- **THEN** 该状态 MUST 保持 `Running`
- **AND** 状态切换 MUST 继续由同层 Transition 决定

### Requirement: Transition 是同层 BaseEdge 语义
系统 MUST 将状态转换表达为 `StateMachineGraph` 内联保存的 `BaseEdge`，MUST NOT 新增 `TransitionNode`，也 MUST NOT 为 Transition 本体创建 asset。Transition MUST 只连接同层 `Enter`、`AnyState`、`StateNode` 和 `Exit`。Transition 条件默认 MUST 是该 edge 内部的 inline `TransitionRuleGraph` 数据；需要复用时才显式绑定 shared `TransitionRuleGraph` asset。

#### Scenario: 合法端点
- **WHEN** 用户连接 Transition flow
- **THEN** 合法连接 MUST 是 `Enter -> StateNode`、`AnyState -> StateNode|Exit`、`StateNode -> StateNode|Exit`
- **AND** `RootNode`、`StateMachineNode`、普通 `RunnableNode`、`TimelineNode`、Tree 节点和 `ValueNode` MUST NOT 成为 Transition 端点

#### Scenario: 条件和优先级
- **WHEN** Transition 配置条件
- **THEN** 条件 MUST 通过该 Transition resolved `TransitionRuleGraph` 表达
- **AND** 创建合法 Transition edge 时 MUST 立即创建该 edge 内部的 inline `TransitionRuleGraph`
- **AND** 默认规则图 MUST 是该 edge 内部的 inline graph data
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** `AnyState` Transition MUST 配置规则图条件
- **AND** runtime MUST 按 Transition 优先级选择可通过的边

#### Scenario: Transition 显式复用规则图
- **WHEN** 多条 Transition 需要复用同一套规则
- **THEN** 用户 MUST 显式抽取或分配 shared `TransitionRuleGraph` asset
- **AND** 删除 Transition 时 MUST 只断开 shared 引用，不删除 shared asset
- **AND** 切换到 shared asset 后 MUST 清理该 Transition 的 inline rule graph 真数据

#### Scenario: Shared TransitionRuleGraph asset 被删除
- **WHEN** Transition 引用的 shared `TransitionRuleGraph` asset 已经被删除或不再能解析为 `TransitionRuleGraph`
- **THEN** 编辑器刷新或校验该 `StateMachineGraph` 时 MUST 自动清理该 shared 引用
- **AND** 该 Transition MUST 回到 owner 内部 inline `TransitionRuleGraph`
- **AND** 系统 MUST NOT 保留 Missing asset 引用

### Requirement: 状态机运行时解释
系统 MUST 让 `StateMachineNode` 驱动自己 resolved `StateMachineGraph` 数据，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，并在每帧写入当前运行工作副本的 `BaseGraph.DeltaTime`。运行时 MUST 从 inline 或 shared authoring graph data 创建隔离工作副本。

#### Scenario: 父级 tick 状态机入口
- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入 resolved `StateMachineGraph` 运行工作副本
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick
- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active `StateNode` resolved 状态行为图的运行工作副本
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: AnyState 和 Exit
- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: Stop 和 Reset 传播
- **WHEN** 父级 Graph stop 或 reset `StateMachineNode`
- **THEN** 当前 active `StateNode` MUST stop 或 reset 自己的状态行为图运行工作副本

### Requirement: 状态机创作 UI 遵守 inline-first 心智
系统 MUST 让 `StateMachineNode`、`StateNode` 和 Transition 的默认 UI 操作与 inline-first 数据模型一致。默认创建必须可立即下钻；左侧 Inspector 负责查看和显式切换复用状态；普通创建路径 MUST NOT 暴露“先创建内部 graph”的旧心智。

#### Scenario: StateMachineNode 默认 UI
- **WHEN** 用户选中 `StateMachineNode`
- **THEN** Inspector MUST 显示状态机引用 ownership
- **AND** 用户 MUST 能通过 `Open` 或双击进入 resolved `StateMachineGraph`
- **AND** 节点画布本体 MUST NOT 因 `Shared Graph` 字段暴露而强制显示配置齿轮

#### Scenario: StateNode 默认 UI
- **WHEN** 用户选中 `StateNode`
- **THEN** Inspector MUST 显示状态行为引用 ownership
- **AND** 用户 MUST 能通过 `Open` 或双击进入 resolved `SubTree` / `StateBehaviorSubTree`
- **AND** shared 状态行为 asset 只能作为显式复用配置

#### Scenario: Transition Rule UI
- **WHEN** 用户选中 StateMachine Transition edge
- **THEN** Inspector MUST 显示 priority、ownership、shared rule asset 和 rule graph 操作
- **AND** 已有 rule graph 时 `Open Rule` MUST 是主操作
- **AND** 合法 Transition MUST NOT 显示 `Create Rule` 或等价创建按钮
- **AND** 缺失 rule graph MUST 由图结构修复自动补齐

## ADDED Requirements
### Requirement: 状态机默认图数据初始化
系统 MUST 在创建 `StateMachineNode` 时初始化一份可立即下钻编辑的 inline `StateMachineGraph` 数据。默认图必须提供状态机闭环所需的最小结构。

#### Scenario: 创建默认状态机图
- **WHEN** 用户创建 `StateMachineNode`
- **THEN** inline `StateMachineGraph` MUST 默认包含一个 `Enter`、一个 `AnyState`、一个 `Exit` 和一个 `StateNode`
- **AND** `Enter` MUST 默认连接到该 `StateNode`
- **AND** 这些控制节点和状态节点 MUST 保存为 inline graph data

#### Scenario: 不依赖已保存 asset
- **WHEN** owner graph asset 尚未保存到磁盘
- **THEN** `StateMachineNode` 创建仍 MUST 能生成 inline `StateMachineGraph` 数据
- **AND** 创建流程 MUST NOT 调用 subasset 创建 API

### Requirement: Transition Rule 默认图数据初始化
系统 MUST 在创建合法 Transition 或编辑器修复缺失规则图的 Transition 时初始化一份可立即下钻编辑的 inline `TransitionRuleGraph` 数据。规则图是条件求值图，不是普通行为树。

#### Scenario: 创建默认 Transition rule
- **WHEN** 用户连接合法 Transition 或编辑器修复缺失规则图的 Transition
- **THEN** 系统 MUST 创建 inline `TransitionRuleGraph` 数据
- **AND** 默认规则图 MUST 包含规则输出入口
- **AND** 默认规则图在未连接条件时 MUST 允许 Transition 通过
- **AND** 用户 MUST 能立即下钻编辑条件节点和属性连线
- **AND** 创建流程 MUST NOT 创建 subasset

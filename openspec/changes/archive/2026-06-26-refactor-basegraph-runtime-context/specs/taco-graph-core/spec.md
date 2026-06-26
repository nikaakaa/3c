## MODIFIED Requirements

### Requirement: 运行时生命周期不下沉到 BaseGraph
系统 MUST 保持 `BaseGraph` 不直接承担执行生命周期。`BaseGraph` MAY 承担运行上下文，包括非序列化 `User`、非序列化 `DeltaTime` 和类型化上下文读取能力。`BaseGraph` MUST NOT 拥有 `Running`、`State`、`UpdateTree` 或 `ResetTree`。`RunnableTree` MUST 继续表达可执行 Tree 生命周期，并在 tick 时把本帧时间写入继承自 `BaseGraph` 的上下文。

#### Scenario: 普通编辑 Graph
- **WHEN** 一个 Graph 只用于编辑或作为状态机图资产
- **THEN** 它 MAY 在初始化后拥有 `User` 和 `DeltaTime` 上下文
- **AND** 它 MUST NOT 因为继承 `BaseGraph` 而自动拥有 `Running`、`State`、`UpdateTree` 或 `ResetTree`

#### Scenario: 可执行 Tree
- **WHEN** 一个 `RunnableTree` 被 `UpdateTree(deltaTime)` tick
- **THEN** 它 MUST 将 `deltaTime` 写入 `BaseGraph.DeltaTime`
- **AND** 它 MUST 继续通过 `RunnableTree` 自己的生命周期执行节点

#### Scenario: 状态机解释器
- **WHEN** `StateMachineGraphRuntime.Update(deltaTime)` 解释一个 `StateMachineGraph`
- **THEN** 它 MUST 将 `deltaTime` 写入该 `StateMachineGraph` 的 `BaseGraph.DeltaTime`
- **AND** 它 MUST NOT 要求 `StateMachineGraph` 继承 `RunnableTree`

## ADDED Requirements

### Requirement: Graph 执行上下文正式传播
系统 MUST 通过 `BaseGraph.InitTree(object user)` 接收外部运行上下文，并通过同一个 `BaseGraph.User` 传递给子 Graph。系统 MUST NOT 使用父节点、父 Graph 或 runner 自身作为隐式 fallback 上下文。

#### Scenario: 根 Graph 初始化
- **WHEN** `TreeRunner` 初始化根 Tree
- **THEN** 它 MUST 将正式配置的 runtime user 传给 `InitTree(object user)`
- **AND** 它 MUST NOT 在未配置 runtime user 时自动传入 `this`

#### Scenario: 子 Graph 初始化
- **WHEN** `StateMachineNode` 初始化下钻 Graph
- **THEN** 子 Graph MUST 接收父 Graph 的 `User`
- **AND** 子 Graph MUST NOT 接收父节点本身作为 fallback 上下文

#### Scenario: 上下文缺失
- **WHEN** 节点运行需要的 provider 不存在于 `BaseGraph.User`
- **THEN** 该节点 MUST 以运行失败或验证错误暴露缺失依赖
- **AND** 系统 MUST NOT 自动创建或查找替代 provider

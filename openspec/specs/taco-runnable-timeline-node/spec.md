# taco-runnable-timeline-node Specification

## Purpose
定义 Graph 驱动 Timeline 的正式节点链路：`Timeline` 继续是资产模型，`TimelineNode : RunnableNode` 在行为 Graph 中引用并播放 Timeline，运行上下文来自所属 `BaseGraph`，不新增 Timeline 状态节点、场景对象 fallback 或并行端口协议。

## Requirements
### Requirement: Timeline 资产不承载节点生命周期
系统 MUST 保持 `Timeline` 作为 `ScriptableObject` 资产模型，负责 tracks、clips、binding 和 evaluation。系统 MUST NOT 让 `Timeline` 继承 `RunnableNode` 或直接作为 Graph 节点。

#### Scenario: Graph 播放 Timeline
- **WHEN** Graph 需要播放 Timeline
- **THEN** Graph MUST 通过 `TimelineNode` 引用 Timeline 资产
- **AND** Graph MUST NOT 直接 tick Timeline 资产本身

### Requirement: TimelineNode 是普通可执行节点
系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中播放 Timeline 的节点。`TimelineNode` MUST 通过 `TimelineReferenceModule` 引用 Timeline 资产，并可在状态行为 `SubTree` 中创建。

#### Scenario: 状态行为播放 Timeline
- **WHEN** 用户在 `StateNode` 引用的 `SubTree` 或 `StateBehaviorSubTree` 中创建 Timeline 播放节点
- **THEN** 创建结果 MUST 是 `TimelineNode`
- **AND** 系统 MUST NOT 创建 `TimelineStateNode`

### Requirement: TimelineNode 生命周期映射 Timeline 播放
系统 MUST 将 `TimelineNode` 的 `RunnableNode` 生命周期映射到 Timeline 播放生命周期。`TimelineNode` MUST 使用所属 `BaseGraph.DeltaTime` 驆动 Timeline，不要求 Owner 是 `RunnableTree`。

#### Scenario: 开始播放
- **WHEN** `TimelineNode` 第一次被 tick
- **THEN** 节点 MUST 从引用的 Timeline 资产创建独立运行实例
- **AND** 节点 MUST 初始化并绑定该运行实例

#### Scenario: 持续播放
- **WHEN** `TimelineNode` 处于 Running
- **THEN** 节点 MUST 使用 `Owner.DeltaTime` 调用 `Timeline.Evaluate(deltaTime)`
- **AND** 节点 MUST 通过同一 TimelinePlayer 评估本帧 PlayableGraph

#### Scenario: 停止或重置
- **WHEN** `TimelineNode` 播放完成、停止或 reset
- **THEN** 节点 MUST 清理当前运行实例和本次运行状态

### Requirement: TimelinePlayer 来自正式执行上下文
系统 MUST 从 `BaseGraph.User` 获取 TimelinePlayer provider。Graph 执行上下文 MUST 通过 `ITimelinePlayerProvider.GetTimelinePlayer()` 或等价正式接口提供 TimelinePlayer。系统 MUST NOT 在 `TimelineNode`、`TreeRunner` 或场景对象搜索中保存 fallback 引用。

#### Scenario: 上下文提供 TimelinePlayer
- **WHEN** `BaseGraph.User` 实现 TimelinePlayer provider
- **THEN** `TimelineNode` MUST 使用该 TimelinePlayer 绑定 Timeline

#### Scenario: 上下文缺失 TimelinePlayer
- **WHEN** `BaseGraph.User` 没有实现 TimelinePlayer provider
- **THEN** `TimelineNode` MUST 返回 `Failure`
- **AND** 系统 MUST NOT 自动创建或 `GetComponent` 查找 TimelinePlayer

### Requirement: TimelineNode 播放状态隔离
系统 MUST 隔离 `TimelineNode` 的运行播放状态。多个 `TimelineNode` 引用同一个 Timeline 资产时，它们 MUST 使用独立运行实例。

#### Scenario: 多节点引用同一 Timeline
- **WHEN** 两个 `TimelineNode` 引用同一个 Timeline 资产
- **THEN** 每个节点 MUST 拥有自己的播放时间、binding 和 TimelinePlayer 状态
- **AND** 一个节点的 Unbind MUST NOT 影响另一个节点

### Requirement: 保留 Timeline 驱动 Tree 链路
系统 MUST 保留 Taco 原有 `TreeTrack` / `TreeClip` / `TimelineRunningTree` 能力。`TimelineNode` 只补充 Graph 驱动 Timeline，不替代 Timeline 内嵌 Tree。

#### Scenario: Timeline 中播放 TreeClip
- **WHEN** Timeline 轨道中存在 `TreeClip`
- **THEN** `TreeClip` MUST 继续能够驱动 `TimelineRunningTree.UpdateTree(deltaTime)`
- **AND** 该链路 MUST NOT 依赖 `TimelineNode`

### Requirement: TimelineNode 不参与状态机同层状态
系统 MUST 保持 `TimelineNode` 为状态内部行为节点。`TimelineNode` MUST NOT 被解释为 `StateMachineGraph` 的同层 State，也 MUST NOT 成为 Transition 端点。

#### Scenario: Idle 状态播放 Timeline
- **WHEN** Idle `StateNode` 需要播放 Timeline
- **THEN** 用户 MUST 在 Idle 引用的状态行为 `SubTree` 中创建 `TimelineNode`
- **AND** 状态机同层 Transition MUST 仍只连接控制节点、`StateNode` 和 `Exit`

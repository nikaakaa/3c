## MODIFIED Requirements

### Requirement: TimelineNode 生命周期映射 Timeline 播放
系统 MUST 将 `TimelineNode` 的 `RunnableNode` 生命周期映射到 Timeline 的播放生命周期。`TimelineNode` MUST 从所属 `BaseGraph` 获取本帧 `DeltaTime`，MUST NOT 要求所属 Graph 是 `RunnableTree`。

#### Scenario: 开始播放
- **WHEN** `TimelineNode` 第一次被 tick
- **THEN** 节点 MUST 从引用的 Timeline 资产创建运行时 Timeline 实例
- **AND** 节点 MUST 初始化该运行实例
- **AND** 节点 MUST 使用 Graph 执行上下文提供的 TimelinePlayer 绑定该运行实例

#### Scenario: 持续播放
- **WHEN** `TimelineNode` 处于 Running
- **THEN** 节点 MUST 使用 `Owner.DeltaTime` 调用运行实例的 `Timeline.Evaluate(deltaTime)`
- **AND** 节点 MUST 通过同一 TimelinePlayer 评估本帧 PlayableGraph
- **AND** Timeline 未播放完成时节点 MUST 返回 `Running`
- **AND** 节点 MUST NOT 判断 `Owner is RunnableTree` 才能播放

#### Scenario: 播放完成
- **WHEN** Timeline 播放时间达到或超过 Duration
- **THEN** `TimelineNode` MUST 返回 `Success`
- **AND** 节点 MUST 停止本次播放生命周期

#### Scenario: 播放失败
- **WHEN** `TimelineNode` 缺失 Timeline 引用或 TimelinePlayer
- **THEN** 节点 MUST 返回 `Failure`
- **AND** 验证或运行错误 MUST 指向具体节点和缺失依赖

#### Scenario: Reset
- **WHEN** `TimelineNode` 被 reset
- **THEN** 节点 MUST 清理当前运行实例
- **AND** 节点 MUST 清理本次运行状态

### Requirement: TimelinePlayer 由执行上下文提供
系统 MUST 从 `BaseGraph.User` 获取 TimelinePlayer provider。Graph 执行上下文 MUST 通过 `ITimelinePlayerProvider.GetTimelinePlayer()` 或等价正式接口提供 TimelinePlayer。系统 MUST NOT 在 `TimelineNode`、`TreeRunner` 或场景对象搜索中保存 fallback 引用。

#### Scenario: 上下文提供 TimelinePlayer
- **WHEN** `BaseGraph.User` 实现 TimelinePlayer provider
- **THEN** `TimelineNode` MUST 使用该 TimelinePlayer 绑定 Timeline

#### Scenario: 上下文缺失 TimelinePlayer
- **WHEN** `BaseGraph.User` 没有实现 TimelinePlayer provider
- **THEN** `TimelineNode` MUST 返回 `Failure`
- **AND** 系统 MUST NOT 自动创建 TimelinePlayer
- **AND** 系统 MUST NOT 通过 `GetComponent` 自动查找 TimelinePlayer

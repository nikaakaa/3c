# btsmtl-runnable-timeline-node Specification Delta

## MODIFIED Requirements

### Requirement: TimelineNode 是普通可执行节点
系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中请求播放 Timeline 的节点。`TimelineNode` MUST 通过 `TimelineReferenceModule` 引用 Timeline 资产，并可在状态行为 `SubTree` 中创建。`TimelineNode` MUST NOT 直接成为 Timeline 播放器，也 MUST NOT 新增 `TimelineStateNode` 或其它特化状态节点。

#### Scenario: 状态行为请求播放 Timeline
- **WHEN** 用户在 `StateNode` 引用的 `SubTree` 或 `StateBehaviorSubTree` 中创建 Timeline 节点
- **THEN** 创建结果 MUST 是 `TimelineNode`
- **AND** 节点 MUST 通过正式管线上下文提交 Timeline 播放请求
- **AND** 系统 MUST NOT 创建 `TimelineStateNode`

### Requirement: TimelineNode 生命周期映射 Timeline 播放
系统 MUST 将 `TimelineNode` 的 `RunnableNode` 生命周期映射到 Timeline 播放请求生命周期。`TimelineNode` MUST 使用所属 `BaseGraph.User` 中的正式管线上下文提交、查询和取消请求。`TimelineNode` MUST NOT 自己实例化 runtime Timeline、绑定 TimelinePlayer、调用 `Timeline.Evaluate(deltaTime)` 或评估 `PlayableGraph`。

#### Scenario: 开始播放
- **WHEN** `TimelineNode` 第一次被 tick
- **THEN** 节点 MUST 使用引用的 Timeline 资产提交一个独立播放请求
- **AND** 节点 MUST 保存该请求的稳定 handle
- **AND** 节点 MUST NOT 在自身内部创建 runtime Timeline 实例

#### Scenario: 持续播放
- **WHEN** `TimelineNode` 处于 Running
- **THEN** 节点 MUST 通过请求 handle 查询管线维护的播放状态
- **AND** 节点 MUST 根据状态返回 `Running`、`Success` 或 `Failure`
- **AND** 节点 MUST NOT 直接推进 Timeline 时间

#### Scenario: 停止或重置
- **WHEN** `TimelineNode` 被停止或 reset
- **THEN** 节点 MUST 通过正式管线上下文取消未完成请求
- **AND** 节点 MUST 清理自己的请求 handle

### Requirement: TimelinePlayer 来自正式执行上下文
系统 MUST NOT 让 `TimelineNode` 直接消费 `TimelinePlayer`。角色管线模式下，`TimelinePlayer` 或等价 PlayableGraph adapter MAY 由表现层持有和使用；Timeline 播放请求、播放状态和动画输出 MUST 通过正式管线上下文、BTSMTL 内部 TimelinePlaybackScheduler 和 PresentationStage 传递。系统 MUST NOT 在 `TimelineNode`、`TreeRunner` 或场景对象搜索中保存 fallback 引用。

#### Scenario: 上下文提供 Timeline 请求入口
- **WHEN** `BaseGraph.User` 提供 Timeline 请求接口
- **THEN** `TimelineNode` MUST 使用该接口提交和查询播放请求
- **AND** `TimelineNode` MUST NOT 从上下文获取 TimelinePlayer 直接播放

#### Scenario: 上下文缺失 Timeline 请求入口
- **WHEN** `BaseGraph.User` 没有提供 Timeline 请求接口
- **THEN** `TimelineNode` MUST 返回 `Failure`
- **AND** 系统 MUST NOT 自动创建、`GetComponent` 查找或全局搜索 TimelinePlayer

### Requirement: TimelineNode 播放状态隔离
系统 MUST 隔离 `TimelineNode` 的播放请求状态。多个 `TimelineNode` 引用同一个 Timeline 资产时，它们 MUST 提交独立请求，并由 `TimelinePlaybackScheduler` 或等价 Timeline runtime owner 维护独立 active record。

#### Scenario: 多节点引用同一 Timeline
- **WHEN** 两个 `TimelineNode` 引用同一个 Timeline 资产
- **THEN** 每个节点 MUST 拥有不同请求 handle
- **AND** 每个 active record MUST 拥有自己的播放时间和状态
- **AND** 一个节点的取消 MUST NOT 影响另一个节点

## ADDED Requirements

### Requirement: Timeline 资产不承载节点生命周期
系统 MUST 保持 `Timeline` 作为 `ScriptableObject` 资产模型，负责 tracks、clips、binding 和 evaluation。系统 MUST NOT 让 `Timeline` 继承 `RunnableNode` 或直接作为 Graph 节点。

#### Scenario: Timeline 作为资产
- **WHEN** 用户创建或编辑 Timeline
- **THEN** Timeline MUST 继续作为资产保存 tracks 和 clips
- **AND** Timeline MUST NOT 直接出现在 Graph 节点列表中作为可 tick 节点

#### Scenario: Graph 需要播放 Timeline
- **WHEN** Graph 需要播放 Timeline
- **THEN** Graph MUST 通过可执行 Timeline 节点引用 Timeline 资产
- **AND** Graph MUST NOT 直接 tick Timeline 资产本身

### Requirement: TimelineNode 是 Graph 中的可执行节点
系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中播放 Timeline 的节点。该节点 MUST 通过 `TimelineReferenceModule` 引用 Timeline 资产。

#### Scenario: 在状态行为 Graph 中创建 TimelineNode
- **WHEN** 用户在 SMNode 下钻的状态行为 Graph 中创建 Timeline 播放节点
- **THEN** 创建结果 MUST 是 `TimelineNode`
- **AND** 该节点 MUST 继承或接入 `RunnableNode` 生命周期
- **AND** 该节点 MUST 通过 `TimelineReferenceModule` 引用 Timeline

#### Scenario: 不创建 TimelineStateNode
- **WHEN** 用户需要让 Idle 或 Walk 播放 Timeline
- **THEN** 用户 MUST 在 IdleGraph 或 WalkGraph 内创建 `TimelineNode`
- **AND** 系统 MUST NOT 创建 `TimelineStateNode`

### Requirement: TimelineNode 生命周期映射 Timeline 播放
系统 MUST 将 `TimelineNode` 的 `RunnableNode` 生命周期映射到 Timeline 的播放生命周期。

#### Scenario: 开始播放
- **WHEN** `TimelineNode` 第一次被 tick
- **THEN** 节点 MUST 从引用的 Timeline 资产创建运行时 Timeline 实例
- **AND** 节点 MUST 初始化该运行实例
- **AND** 节点 MUST 使用 Graph 执行上下文提供的 TimelinePlayer 绑定该运行实例

#### Scenario: 持续播放
- **WHEN** `TimelineNode` 处于 Running
- **THEN** 节点 MUST 使用当前 Graph deltaTime 调用运行实例的 `Timeline.Evaluate(deltaTime)`
- **AND** 节点 MUST 通过同一 TimelinePlayer 评估本帧 PlayableGraph
- **AND** Timeline 未播放完成时节点 MUST 返回 `Running`

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
系统 MUST 从 Graph 执行上下文获取 TimelinePlayer。Graph 执行上下文 MUST 通过 `ITimelinePlayerProvider.GetTimelinePlayer()` 或等价正式接口提供 TimelinePlayer。系统 MUST NOT 在 `TimelineNode` 上保存场景对象 fallback 引用。

#### Scenario: 上下文提供 TimelinePlayer
- **WHEN** Graph 执行上下文实现 TimelinePlayer provider
- **THEN** `TimelineNode` MUST 使用该 TimelinePlayer 绑定 Timeline

#### Scenario: 上下文缺失 TimelinePlayer
- **WHEN** Graph 执行上下文没有实现 TimelinePlayer provider
- **THEN** `TimelineNode` MUST 返回 `Failure`
- **AND** 系统 MUST NOT 自动创建 TimelinePlayer

### Requirement: TimelineNode 播放状态隔离
系统 MUST 隔离 TimelineNode 的运行播放状态。多个 TimelineNode 引用同一个 Timeline 资产时，它们 MUST 使用独立运行实例，不能共享 `Time/Binding/TimelinePlayer` 状态。

#### Scenario: 多个节点引用同一 Timeline
- **WHEN** 两个 `TimelineNode` 引用同一个 Timeline 资产
- **THEN** 每个节点 MUST 创建自己的运行时 Timeline 实例
- **AND** 一个节点的播放时间 MUST NOT 修改另一个节点的播放时间
- **AND** 一个节点的 Unbind MUST NOT 影响另一个节点的绑定

### Requirement: 保留 Taco 原有 Timeline 驱动 Tree 链路
系统 MUST 保留 `TreeTrack/TreeClip/TimelineRunningTree` 的原有能力。新增 `TimelineNode` MUST 只补充 Graph 驱动 Timeline 的方向，不得替换 Timeline 内嵌 Tree。

#### Scenario: Timeline 中播放 TreeClip
- **WHEN** Timeline 轨道中存在 TreeClip
- **THEN** TreeClip MUST 继续能够驱动 `TimelineRunningTree.UpdateTree(deltaTime)`
- **AND** 该链路 MUST NOT 依赖 `TimelineNode`

#### Scenario: Graph 中播放 Timeline
- **WHEN** Graph 中存在 `TimelineNode`
- **THEN** Graph MUST 通过 `TimelineNode` 驱动 Timeline
- **AND** 该链路 MUST NOT 删除或改写 TreeClip 的运行方式

### Requirement: 状态行为 Graph 集成
系统 MUST 允许 `TimelineNode` 出现在 SMNode 下钻的行为 Graph 中，并作为状态具体行为的一部分被 tick。

#### Scenario: IdleGraph 播放 IdleTimeline
- **WHEN** LocomotionGraph 当前 active SMNode 是 Idle
- **THEN** Idle SMNode MUST 能下钻到 IdleGraph
- **AND** IdleGraph MUST 能 tick `TimelineNode`
- **AND** `TimelineNode` MUST 能播放 IdleTimeline

#### Scenario: TimelineNode 不参与同层状态转换
- **WHEN** TimelineNode 位于状态行为 Graph 内
- **THEN** 它 MUST NOT 被解释为状态机 Graph 的同层 State
- **AND** 状态机同层 Transition MUST 仍然只连接 `StateMachineNode`

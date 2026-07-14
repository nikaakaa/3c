## MODIFIED Requirements

### Requirement: Timeline 资产不承载节点生命周期

系统 MUST 将 Timeline authoring data 与 Unity asset 身份分离。普通 C# 可序列化 `TimelineData` MUST 作为 tracks、clips、scale 和采样结构的唯一数据模型；`TimelineAsset` MUST 只作为显式 shared 复用和 Project 直接打开的 ScriptableObject 外壳持有一份 TimelineData。TimelineData 与 TimelineAsset MUST NOT 继承 `RunnableNode`，也 MUST NOT 直接承担 Graph 节点生命周期。

#### Scenario: Graph 播放 inline Timeline

- **WHEN** Graph 中的 TimelineNode 使用默认 Inline ownership
- **THEN** TimelineNode MUST 解析自己持有的 TimelineData 并提交正式播放请求
- **AND** 系统 MUST NOT 要求作者先创建 TimelineAsset
- **AND** Graph MUST NOT 直接 tick authoring TimelineData

#### Scenario: Graph 播放 shared Timeline

- **WHEN** TimelineNode 显式选择 Shared Asset ownership
- **THEN** resolved TimelineData MUST 来自 TimelineAsset 持有的数据
- **AND** TimelineNode MUST NOT 同时保留一份生效的 inline TimelineData
- **AND** Graph MUST NOT 直接 tick TimelineAsset

### Requirement: TimelineNode 是普通可执行节点

系统 MUST 提供 `TimelineNode : RunnableNode` 作为 Graph 中请求播放 Timeline 的节点。TimelineNode MUST 通过正式 Timeline ownership module 默认持有可立即编辑的 inline TimelineData，并 MAY 显式切换为 shared TimelineAsset。TimelineNode MUST 只暴露输入控制流 port，不得暴露、持久化或解析输出控制流 port。TimelineNode MUST NOT 直接成为 Timeline 播放器，也 MUST NOT 新增 TimelineStateNode 或其它特化状态节点。

#### Scenario: 创建 TimelineNode

- **WHEN** 用户在 StateNode 的状态行为 Graph 中创建 TimelineNode
- **THEN** 创建结果 MUST 自动拥有一份 inline TimelineData
- **AND** 用户 MUST 能立即下钻编辑 tracks、clips 和 TreeClip
- **AND** 创建流程 MUST NOT要求 TimelineAsset 已存在

#### Scenario: 显式复用 Timeline

- **WHEN** 用户对 inline Timeline 执行 Extract Shared 或选择已有 TimelineAsset
- **THEN** TimelineNode MUST 切换为 Shared Asset ownership
- **AND** owner 内的 inline 真数据 MUST 被清理
- **AND** UI MUST 明确显示 Shared Asset

#### Scenario: Timeline 播放完成

- **WHEN** TimelineNode resolved TimelineData 的播放请求返回成功
- **THEN** TimelineNode MUST 返回 Success
- **AND** TimelineNode MUST NOT tick 子节点或输出控制流目标

### Requirement: TimelineNode 生命周期映射 Timeline 播放

系统 MUST 将 TimelineNode 的 RunnableNode 生命周期映射到 Timeline 逻辑播放请求生命周期。TimelineNode MUST 使用所属 BaseGraph.User 中的正式管线上下文提交、查询和取消 resolved TimelineData 请求，并让请求捕获当前正式 animation owner scope。TimelinePlaybackScheduler MUST 从 resolved authoring TimelineData 创建独立 runtime data clone；TimelineNode MUST NOT 自己创建 runtime clone、直接推进 Timeline 时间、绑定旧播放器、评估 PlayableGraph 或释放 Registry entries。

#### Scenario: 开始播放 inline Timeline

- **WHEN** Inline TimelineNode 第一次被 tick
- **THEN** 节点 MUST 使用自己的 resolved TimelineData 提交独立播放请求
- **AND** 节点 MUST 保存该请求的稳定 handle
- **AND** scheduler MUST 为请求创建隔离的 TimelineData 工作副本
- **AND** runtime MUST NOT 修改节点内的 authoring TimelineData

#### Scenario: 开始播放 shared Timeline

- **WHEN** 两个 TimelineNode 引用同一个 shared TimelineAsset 并开始播放
- **THEN** 两个请求 MUST 分别从同一 source TimelineData 创建独立工作副本
- **AND** 一个请求的 time、Track runtime、TreeClip runtime 或取消 MUST NOT 污染另一个请求
- **AND** shared TimelineAsset MUST 不保存 runtime 状态

#### Scenario: 停止或重置未完成请求

- **WHEN** TimelineNode 在逻辑播放尚未完成时被停止或 reset
- **THEN** 节点 MUST 通过正式管线上下文取消未完成请求
- **AND** 节点 MUST 清理自己的逻辑请求 handle
- **AND** 节点 MUST NOT修改 inline 或 shared authoring TimelineData

### Requirement: TimelineNode 播放状态隔离

系统 MUST 隔离 TimelineNode 的播放请求状态。多个 TimelineNode 无论解析到各自 inline TimelineData 还是同一个 shared TimelineAsset，都 MUST 提交独立请求，并由 TimelinePlaybackScheduler 维护独立 TimelineData 工作副本与 active record。

#### Scenario: 同一个 inline TimelineNode 再次激活

- **WHEN** 同一个 TimelineNode 在后续 State activation 中再次提交自己的 inline TimelineData
- **THEN** 新请求 MUST 获得新的 playback handle 和 runtime clone
- **AND** 前一次播放的 time、cycle 和 TreeClip runtime state MUST NOT被复用

#### Scenario: 多节点复用 shared Timeline

- **WHEN** 两个 TimelineNode 显式引用同一个 shared TimelineAsset
- **THEN** 每个节点 MUST 拥有不同请求 handle
- **AND** 每个 active record MUST 拥有自己的播放时间和状态
- **AND** 一个节点的取消 MUST NOT影响另一个节点

### Requirement: 保留 Timeline 驱动 Tree 链路

系统 MUST 保留 BTSMTL `TreeTrack / TreeClip / TimelineRunningTree` 能力。TreeClip MUST 位于 resolved TimelineData 的正式 Track/Clip 集合中，默认拥有 inline TimelineRunningTree，并 MAY 显式引用 shared Tree asset。TimelineNode 只负责提交 resolved TimelineData 播放请求，不替代 Timeline 内嵌 Tree，也不得把 TreeClip 数据复制到 Graph 节点旁路。

#### Scenario: inline Timeline 中运行 TreeClip

- **WHEN** TimelineNode 的 inline TimelineData 包含 TreeClip
- **THEN** TimelinePlaybackScheduler MUST 从 runtime TimelineData clone 求值对应 TimelineRunningTree
- **AND** 该链路 MUST 使用正式 Clip runtime context
- **AND** authoring TimelineRunningTree MUST 不保存 runtime state

#### Scenario: shared Timeline 中运行 TreeClip

- **WHEN** shared TimelineAsset 的 TimelineData 包含 TreeClip
- **THEN** 每个 playback request MUST 克隆隔离的 TreeClip 与 TimelineRunningTree runtime
- **AND** TreeClip MUST NOT依赖 TimelineNode 静态 membership 推导 Action Context


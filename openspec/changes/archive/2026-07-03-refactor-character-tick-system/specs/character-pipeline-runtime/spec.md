## REMOVED Requirements

### Requirement: CharacterPipelineRunner 是统一 tick 源

系统不再使用 `CharacterPipelineRunner` 作为角色 pipeline 的统一 tick 源。该要求被 `GameplayTickSystem` 和 `IGameplayTickTarget` 替代。

#### Scenario: 移除场景 Runner

- **WHEN** 本变更完成
- **THEN** 正式 runtime MUST 不再依赖场景中的 `CharacterPipelineRunner`
- **AND** `CharacterPipelineHost` MUST 不再注册到 `CharacterPipelineRunner`

## ADDED Requirements

### Requirement: CharacterPipeline 是 GameplayTickSystem 的 tick target

系统 MUST 使用 `GameplayTickSystem` 作为 gameplay 统一 tick 源。`CharacterPipeline` MUST 作为 `IGameplayTickTarget` 注册到 `GameplayTickSystem`，由它统一调度本地逻辑 tick 和表现帧。`CharacterPipeline` MUST NOT 自己拥有 Unity `Update`、`LateUpdate`、`FixedUpdate` 或其它自主 tick 来源。

#### Scenario: 多个角色被统一调度

- **WHEN** 场景中存在多个启用的 `CharacterPipelineHost`
- **THEN** 每个 Host 创建的 `CharacterPipeline` MUST 注册到同一个 `GameplayTickSystem`
- **AND** tick system MUST 在同一 `LocalLogicTick` 中按注册列表调度它们
- **AND** 单个 `CharacterPipeline` MUST NOT 自己从 Unity 生命周期拉取 tick

#### Scenario: 角色被禁用

- **WHEN** 某个 `CharacterPipelineHost` 被禁用
- **THEN** 该 Host 的 pipeline MUST 从 `GameplayTickSystem` 反注册
- **AND** 后续 tick system MUST NOT 再调度该 pipeline

## MODIFIED Requirements

### Requirement: CharacterPipeline 是纯 C# 运行时主体

系统 MUST 将 `CharacterPipeline` 实现为纯 C# 对象。`CharacterPipeline` MUST 通过 `GameplayTickSystem` 传入的 logic tick context 和 presentation frame context 执行。`CharacterPipeline` MUST NOT 直接读取 Unity `Time.deltaTime`，MUST NOT 自增 `LocalLogicTick`，MUST NOT 自增 `ServerTick`。Logic context MUST 至少表达 fixed delta、render frame、local logic tick、input sequence 和 authority mode。Presentation context MUST 至少表达 scaled delta、unscaled delta、render frame、最近 local logic tick、interpolation alpha 和 authority mode。

#### Scenario: TickSystem 传入 logic context

- **WHEN** `GameplayTickSystem` 调用 pipeline logic tick
- **THEN** tick system MUST 传入包含 fixed delta、render frame、local logic tick、input sequence 和 authority mode 的 logic context
- **AND** pipeline MUST 使用该 context 推进输入、BTSMTL、Action、Motion 和网络输出收集

#### Scenario: TickSystem 传入 presentation context

- **WHEN** `GameplayTickSystem` 调用 pipeline presentation frame
- **THEN** tick system MUST 传入包含 scaled delta、unscaled delta、render frame、最近 local logic tick 和 interpolation alpha 的 presentation context
- **AND** pipeline MUST 使用该 context 推进表现层、动画应用、cue 和插值

#### Scenario: Pipeline 被释放

- **WHEN** Host 销毁或明确释放 pipeline
- **THEN** pipeline MUST 释放 BTSMTL RootTree 运行实例、Graph context 和 stage 缓存
- **AND** pipeline MUST NOT 继续持有场景对象引用

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

系统 MUST 将角色处理拆成明确 logic tick 和 presentation frame。Logic tick MUST 至少包含 network receive、input、BTSMTL、action、motion resolve 和 network send collection。Presentation frame MUST 至少包含 presentation resolve、动画应用、cue 应用、插值和 frame end cleanup。Phase MUST 通过 frame/context/output 交换数据，MUST NOT 互相直接控制对方的内部状态。

#### Scenario: Logic tick

- **WHEN** pipeline logic tick 执行
- **THEN** NetworkReceiveStage MUST 先读取已注入的 server snapshot、action decision 或 correction 缓存
- **AND** InputStage MUST 更新当前本地逻辑 tick 的输入快照
- **AND** CharacterBTSMTLPhase MUST 使用 logic context tick BTSMTL RootTree 和 active Timeline playback
- **AND** MotionStage MUST 消费 motion intent、motion contribution 和 motion modifier 数据并产生 motion result
- **AND** NetworkSendStage MUST 收集 client command、action request、window digest 或 correction acknowledgement

#### Scenario: Presentation frame

- **WHEN** pipeline presentation frame 执行
- **THEN** PresentationStage MUST 消费 animation contribution、presentation cue、最新 motion result 或 snapshot interpolation 数据
- **AND** 表现层 MUST 使用 presentation context 的 delta 和 interpolation alpha
- **AND** frame transient 数据 MUST 在明确 frame end 被清理

### Requirement: Timeline 和动画 tick 权威归属 pipeline

系统 MUST 让 `GameplayTickSystem` 通过角色 pipeline 成为角色 pipeline 模式下的 Timeline 和动画图推进入口。Timeline 播放请求 MUST 由 `CharacterBTSMTLPhase` 内部的 `TimelinePlaybackScheduler` 在 logic tick 中推进。`TimelinePlayer` 或等价 PlayableGraph adapter MAY 位于表现层边界内，MUST NOT 与 `TimelineNode` 在同一帧重复推进同一 Timeline。

#### Scenario: TimelineNode 提交 Timeline 请求

- **WHEN** `TimelineNode` 在 CharacterBTSMTLPhase 内执行
- **THEN** `TimelineNode` MUST 提交 Timeline 播放请求
- **AND** `TimelinePlaybackScheduler` MUST 使用 logic tick context 推进该请求
- **AND** TimelinePlayer MUST NOT 在自己的自主 tick 中再次推进同一运行实例

#### Scenario: 选择外部 tick 策略

- **WHEN** 项目启用 `CharacterPipeline`
- **THEN** TimelinePlayer 的运行方式 MUST 被收敛为 pipeline 显式 tick
- **AND** 系统 MUST NOT 长期保留 pipeline tick 和 TimelinePlayer autonomous tick 两条权威路径

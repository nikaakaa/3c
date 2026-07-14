## REMOVED Requirements

### Requirement: Pipeline 输出分为 strict、presentation 和 network

该 requirement 被 `Pipeline 输出分为 strict、presentation 和 sync facts` 替代。第三层输出不再使用 `NetworkOutput` 作为正式 pipeline fact 口径。

#### Scenario: 删除 NetworkOutput 正式口径

- **WHEN** 实现本变更
- **THEN** `CharacterPipelineOutput` MUST NOT 暴露 `NetworkOutput` 或 `.Network` 作为本帧事实输出层
- **AND** 系统 MUST 使用 `SyncFacts` 或等价命名表达可同步事实

## ADDED Requirements

### Requirement: Pipeline 输出分为 strict、presentation 和 sync facts

系统 MUST 将 `CharacterPipelineOutput` 分为 `StrictGameplayOutput`、`PresentationOutput` 和 `SyncFacts`。`SyncFacts` MUST 表达本 tick 已发生、可被记录、调试、回放、loopback 或网络 backend 消费的同步事实。`SyncFacts` MUST NOT 等同于网络包或 transport API。

#### Scenario: 写入 strict gameplay output

- **WHEN** Graph、Timeline 或 MotionStage 产出 active state、motion result、gameplay window 或本地 gameplay 决策字段
- **THEN** 这些字段 MUST 写入 `StrictGameplayOutput`
- **AND** 这些字段 MUST NOT 因为未来可能调试而默认进入 `SyncFacts`

#### Scenario: 写入 presentation output

- **WHEN** Timeline 或 Graph 产出本地 animation command、VFX、SFX、camera cue、hit stop 或后处理 cue
- **THEN** 这些字段 MUST 写入 `PresentationOutput`
- **AND** local-only 表现 MUST NOT 被强制写入 `SyncFacts`

#### Scenario: 写入 sync facts

- **WHEN** 本地预测角色产生 input command、action activation、action lifecycle transition、window sample、motion digest、gameplay result、state effect 或 replicated cue
- **THEN** 这些事实 MUST 写入 `SyncFacts` 中对应的 SyncDomain output
- **AND** 没有网络 backend 时，这些事实 MAY 只被 debug、record、loopback 或无人消费

## MODIFIED Requirements

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

系统 MUST 将角色每帧处理拆成明确 phase。第一阶段 MUST 至少包含 network receive、input、BTSMTL、motion resolve、presentation resolve、sync fact collection/network send boundary 和 frame end cleanup。Phase MUST 通过 frame/context/output 交换数据，MUST NOT 互相直接控制对方的内部状态。Network receive MUST 发生在 input 和 Graph 前；network send boundary MUST 发生在本 tick 可同步事实产生后，并由 adapter 交给 GameplaySyncRuntime 或后续 backend。

#### Scenario: Update phase

- **WHEN** pipeline update phase 执行
- **THEN** NetworkReceiveStage MUST 先读取已注入的 snapshot、action decision、gameplay result 或 correction 缓存
- **AND** InputStage MUST 更新当前帧输入快照
- **AND** CharacterBTSMTLPhase MUST 使用当前 frame/context tick BTSMTL RootTree 和 active Timeline playback
- **AND** CharacterBTSMTLPhase 输出的可同步事实 MUST 写入 `CharacterPipelineOutput.SyncFacts`

#### Scenario: Late phase

- **WHEN** pipeline late phase 执行
- **THEN** MotionStage MUST 消费 `MotionIntent`、`MotionContribution` 和 motion modifier 数据并产生 `MotionResult`
- **AND** PresentationStage MUST 消费 `AnimationContribution` 或 `PresentationCue`
- **AND** NetworkSendStage MUST 从 `SyncFacts` 收集 client command、action activation、lifecycle transition、motion snapshot、gameplay result 或 window digest
- **AND** frame transient 数据 MUST 在帧末被清理

### Requirement: CharacterPipeline 支持混合架构 authority mode

系统 MUST 明确区分角色 pipeline 的 authority mode。第一阶段 MUST 至少定义 `LocalPredicted`、`RemoteProxy` 和 `PresentationOnly`。不同 mode MUST 使用同一 `CharacterPipeline` 主线，不得新增第二套角色控制器路径。

#### Scenario: 本地预测角色

- **WHEN** pipeline 处于 `LocalPredicted`
- **THEN** pipeline MUST 允许本地输入立即驱动 Graph、Timeline、Motion 和 Presentation
- **AND** pipeline MUST 通过 `SyncFacts` 暴露后续服务端确认需要的 action request、input sequence、lifecycle transition 和 motion snapshot

#### Scenario: 远端代理角色

- **WHEN** pipeline 处于 `RemoteProxy`
- **THEN** pipeline MUST 允许 server snapshot 和 interpolation 数据驱动表现
- **AND** pipeline MUST NOT 要求远端角色完整重放本地输入 Graph

#### Scenario: 表现专用角色

- **WHEN** pipeline 处于 `PresentationOnly`
- **THEN** pipeline MUST 只消费表现输入或快照
- **AND** pipeline MUST NOT 产生本地 action request

### Requirement: NetworkStage 是正式边界但不实现真实 transport

系统 MUST 在 `CharacterPipeline` 中保留 `CharacterNetworkReceiveStage` 和 `CharacterNetworkSendStage` 作为角色管线内部的网络输入/输出边界。真实 transport、Fantasy Session、本地 loopback 和通用 peer MUST 位于 GameplaySyncRuntime 或 adapter 外侧。CharacterPipeline MUST NOT 直接实现 Fantasy transport、服务端 handler、完整网络裁决或 peer 逻辑。

#### Scenario: 接收网络输入缓存

- **WHEN** 本帧开始前 CharacterGameplaySyncAdapter 已注入 motion correction、action decision、gameplay result、state/effect 或 cue 输入
- **THEN** NetworkReceiveStage MUST 将它们放入 `CharacterPipelineFrame`、graph context 或正式输入缓存
- **AND** NetworkReceiveStage MUST NOT 直接修改 Transform、BTSMTL 节点状态或 ActionRuntime state

#### Scenario: 收集同步事实

- **WHEN** 本帧产生 `SyncFacts`
- **THEN** NetworkSendStage MUST 收集这些事实供 CharacterGameplaySyncAdapter 转换为 GameplaySync outgoing packet
- **AND** NetworkSendStage MUST NOT 直接发送 Fantasy 消息或调用 peer


## MODIFIED Requirements

### Requirement: NetworkStage 是正式边界但不实现真实 transport
系统 MUST 在 `CharacterPipeline` 中保留 `NetworkReceiveStage` 和 `NetworkSendStage` 作为角色管线内部的网络输入/输出收集边界。真实 transport、Fantasy Session、本地 loopback 和通用 peer MUST 位于 GameplaySyncRuntime 或 adapter 外侧。CharacterPipeline MUST NOT 直接实现 Fantasy transport、服务端 handler、完整网络裁决或 peer 逻辑。

#### Scenario: 接收网络输入缓存
- **WHEN** 本帧开始前 CharacterGameplaySyncAdapter 已注入 motion correction、action decision、gameplay result、state/effect 或 cue 输入
- **THEN** NetworkReceiveStage MUST 将它们放入 `CharacterPipelineFrame`、graph context 或正式输入缓存
- **AND** NetworkReceiveStage MUST NOT 直接修改 Transform、BTSMTL 节点状态或 ActionRuntime state

#### Scenario: 收集网络输出
- **WHEN** 本帧产生 `NetworkOutput`
- **THEN** NetworkSendStage MUST 收集这些输出供 CharacterGameplaySyncAdapter 转换为 GameplaySync outgoing packet
- **AND** NetworkSendStage MUST NOT 直接发送 Fantasy 消息或调用 peer

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界
系统 MUST 将角色每帧处理拆成明确 phase。第一阶段 MUST 至少包含 network receive、input、BTSMTL、motion resolve、presentation resolve、network send 和 frame end cleanup。Phase MUST 通过 frame/context/output 交换数据，MUST NOT 互相直接控制对方的内部状态。Network receive MUST 发生在 input 和 Graph 前；network send MUST 发生在本 tick 需要同步的输出产生后，并由 adapter 交给 GameplaySyncRuntime。

#### Scenario: Update phase
- **WHEN** pipeline update phase 执行
- **THEN** NetworkReceiveStage MUST 先读取已注入的 snapshot、action decision、gameplay result 或 correction 缓存
- **AND** InputStage MUST 更新当前帧输入快照
- **AND** CharacterBTSMTLPhase MUST 使用当前 frame/context tick BTSMTL RootTree 和 active Timeline playback
- **AND** CharacterBTSMTLPhase 输出的数据 MUST 写入 `CharacterPipelineOutput`

#### Scenario: Late phase
- **WHEN** pipeline late phase 执行
- **THEN** MotionStage MUST 消费 `MotionIntent`、`MotionContribution` 和 motion modifier 数据并产生 `MotionResult`
- **AND** PresentationStage MUST 消费 `AnimationContribution` 或 `PresentationCue`
- **AND** NetworkSendStage MUST 从 `NetworkOutput` 收集 client command、action activation、motion snapshot、gameplay result 或 window digest
- **AND** frame transient 数据 MUST 在帧末被清理

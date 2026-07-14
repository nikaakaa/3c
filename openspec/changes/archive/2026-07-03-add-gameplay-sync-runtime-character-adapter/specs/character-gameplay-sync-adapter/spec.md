## ADDED Requirements

### Requirement: CharacterPipeline 必须通过 adapter 接入 GameplaySyncRuntime
系统 MUST 使用 `CharacterGameplaySyncAdapter` 或等价 adapter 连接 CharacterPipeline 与 GameplaySyncRuntime。CharacterPipeline MUST NOT 直接持有 `IGameplaySyncPeer`、Fantasy Session、transport client 或服务端对象。Adapter MUST 只负责 Character network stage 数据与 GameplaySync packets 之间的映射。

#### Scenario: 本地预测角色发送输出
- **WHEN** CharacterPipeline 在本地预测 tick 内产生 `NetworkOutput`
- **THEN** CharacterGameplaySyncAdapter MUST 从 `CharacterNetworkSendStage` 收集输出并转换为 GameplaySync outgoing packets
- **AND** CharacterPipeline MUST NOT 直接发送 packet 给 peer

#### Scenario: 接收服务端输入
- **WHEN** GameplaySyncRuntime 中存在属于该角色 actor 的 incoming packets
- **THEN** CharacterGameplaySyncAdapter MUST 将它们转换并推入 `CharacterNetworkReceiveStage`
- **AND** GameplaySyncRuntime MUST NOT 直接修改 CharacterGraphContext、ActionRuntime 或 Transform

### Requirement: Character outgoing adapter 必须按 SyncDomain 映射输出
Character outgoing adapter MUST 将 Character network outputs 映射到对应 SyncDomain packet。Client command frame MUST 进入 MotionSyncDomain；action activation、action end 和 action window digest MUST 进入 ActionSyncDomain；action-scoped motion digest MAY 进入 MotionSyncDomain 并携带 action instance 来源；cue MUST 进入 PresentationSyncDomain；命中、伤害、目标点或 PvE 结果 MUST 进入 GameplayResultSyncDomain。

#### Scenario: 动作启动输出
- **WHEN** `CharacterNetworkSendStage` 收集到 `ActionActivationRequest`
- **THEN** adapter MUST 生成 ActionSyncDomain action activation outgoing packet
- **AND** packet MUST 携带 action id、action instance id 或 prediction key、input sequence 和 actor identity

#### Scenario: 旧 ActionCombatEvent 输出
- **WHEN** 实现阶段遇到 `ActionCombatEvent` 或等价旧命名输出
- **THEN** 系统 MUST 将其迁移为 GameplayResult 命名和 GameplayResultSyncDomain packet
- **AND** 系统 MUST NOT 保留 `ActionCombatEvent` 兼容别名作为正式输出路径

### Requirement: Character incoming adapter 必须按 SyncDomain 注入输入
Character incoming adapter MUST 将 GameplaySync incoming packets 注入 Character pipeline 的正式输入位置。Motion correction 和 snapshot MUST 进入 motion/network input；ActionInstanceDecision MUST 进入 action decision input；GameplayResult MUST 进入 gameplay result input；StateEffect packet MUST 进入 state/effect input；Presentation cue packet MUST 进入 presentation cue input。

#### Scenario: 动作确认
- **WHEN** adapter 收到 ActionSyncDomain `ActionInstanceDecision(Confirmed)`
- **THEN** 它 MUST 将 decision 推入 CharacterNetworkReceiveStage 或等价正式 action decision input
- **AND** 它 MUST NOT 直接调用 ActionRuntime confirm

#### Scenario: 运动校正
- **WHEN** adapter 收到 MotionSyncDomain correction
- **THEN** 它 MUST 将 correction 推入 CharacterNetworkReceiveStage 的 correction 输入
- **AND** 最终位置修正 MUST 仍由 MotionStage 或正式 correction stage 处理

### Requirement: Character 网络 stage 必须保持 adapter stage 职责
`CharacterNetworkReceiveStage` 和 `CharacterNetworkSendStage` MUST 保持角色管线内部的输入/输出收集职责。它们 MAY 保留 Character 命名，但 MUST NOT 成为 peer、transport、Fantasy adapter 或通用 GameplaySyncRuntime。

#### Scenario: NetworkSendStage 收集输出
- **WHEN** CharacterPipeline LogicTick 执行到 NetworkSendStage
- **THEN** stage MUST 只收集本 tick 的 Character network outputs
- **AND** stage MUST NOT 直接认识 local loopback 或 Fantasy peer

#### Scenario: NetworkReceiveStage 收集输入
- **WHEN** CharacterPipeline LogicTick 执行到 NetworkReceiveStage
- **THEN** stage MUST 只把已注入输入放入 frame 或 graph context
- **AND** stage MUST NOT 直接从 peer 拉取 packet

### Requirement: Character adapter tick 必须服从 CharacterPipelineRunner
CharacterGameplaySyncAdapter 或 driver MUST 使用 `CharacterPipelineRunner` 的 local logic tick 进行注入、收集和 pump。系统 MUST NOT 为网络 adapter 创建第二套 gameplay tick。

#### Scenario: Tick 前注入 incoming
- **WHEN** runner 准备 tick 某个 CharacterPipeline
- **THEN** driver MUST 先从 GameplaySyncRuntime 取出该 actor incoming packets 并通过 adapter 注入
- **AND** `NetworkReceiveStage.Collect` MUST 仍在 InputStage 前消费这些输入

#### Scenario: Tick 后收集 outgoing
- **WHEN** CharacterPipeline LogicTick 完成
- **THEN** driver MUST 通过 adapter 读取 `NetworkSendStage` 输出并写入 GameplaySyncRuntime outgoing queue
- **AND** driver MUST NOT 重新 tick Graph 或 MotionStage

## MODIFIED Requirements

### Requirement: CharacterPipeline 必须通过 adapter 接入 GameplaySyncRuntime

系统 MUST 使用 `CharacterGameplaySyncAdapter` 或等价 adapter 连接 CharacterPipeline 与 GameplaySyncRuntime。CharacterPipeline MUST NOT 直接持有 `IGameplaySyncPeer`、Fantasy Session、transport client 或服务端对象。Adapter MUST 只负责 CharacterNetworkSendStage 收集到的 SyncFacts 与 GameplaySync packets 之间的映射。

#### Scenario: 本地预测角色发送输出

- **WHEN** CharacterPipeline 在本地预测 tick 内产生 `SyncFacts`
- **THEN** CharacterGameplaySyncAdapter MUST 从 `CharacterNetworkSendStage` 收集这些事实并转换为 GameplaySync outgoing packets
- **AND** CharacterPipeline MUST NOT 直接发送 packet 给 peer

#### Scenario: 接收服务端输入

- **WHEN** GameplaySyncRuntime 中存在属于该角色 actor 的 incoming packets
- **THEN** CharacterGameplaySyncAdapter MUST 将它们转换并推入 `CharacterNetworkReceiveStage`
- **AND** GameplaySyncRuntime MUST NOT 直接修改 CharacterGraphContext、ActionRuntime 或 Transform

### Requirement: Character outgoing adapter 必须按 SyncDomain 映射输出

Character outgoing adapter MUST 将 Character SyncFacts 映射到对应 SyncDomain packet。Client command frame MUST 进入 MotionSyncDomain；action activation、action lifecycle transition 和 action window digest MUST 进入 ActionSyncDomain；action-scoped motion digest MAY 进入 MotionSyncDomain 并携带 action instance 来源；cue MUST 进入 PresentationSyncDomain；命中、伤害、目标点或 PvE 结果 MUST 进入 GameplayResultSyncDomain。

#### Scenario: 动作启动输出

- **WHEN** `CharacterNetworkSendStage` 收集到 action activation fact
- **THEN** adapter MUST 生成 ActionSyncDomain action activation outgoing packet
- **AND** packet MUST 携带 action id、action instance id 或 prediction key、input sequence 和 actor identity

#### Scenario: 动作生命周期输出

- **WHEN** `CharacterNetworkSendStage` 收集到 `ActionLifecycleTransition`
- **THEN** adapter MUST 生成 ActionSyncDomain lifecycle transition outgoing packet
- **AND** adapter MUST NOT 将其降级为旧单一 end 语义

#### Scenario: 旧 ActionCombatEvent 输出

- **WHEN** 实现阶段遇到 `ActionCombatEvent` 或等价旧命名输出
- **THEN** 系统 MUST 将其迁移为 GameplayResult 命名和 GameplayResultSyncDomain packet
- **AND** 系统 MUST NOT 保留 `ActionCombatEvent` 兼容别名作为正式输出路径

### Requirement: Character 网络 stage 必须保持 adapter stage 职责

`CharacterNetworkReceiveStage` 和 `CharacterNetworkSendStage` MUST 保持角色管线内部的输入/输出收集职责。它们 MAY 保留 Character Network 命名，但 MUST NOT 成为 peer、transport、Fantasy adapter、local loopback 或通用 GameplaySyncRuntime。

#### Scenario: NetworkSendStage 收集输出

- **WHEN** CharacterPipeline LogicTick 执行到 NetworkSendStage
- **THEN** stage MUST 只从本 tick 的 `SyncFacts` 收集要交给网络 adapter 的输出
- **AND** stage MUST NOT 直接认识 local loopback 或 Fantasy peer

#### Scenario: NetworkReceiveStage 收集输入

- **WHEN** CharacterPipeline LogicTick 执行到 NetworkReceiveStage
- **THEN** stage MUST 只把已注入输入放入 frame 或 graph context
- **AND** stage MUST NOT 直接从 peer 拉取 packet

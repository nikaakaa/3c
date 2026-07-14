## ADDED Requirements

### Requirement: Outgoing adapter 必须消费策略解析结果

Character outgoing adapter MUST 使用 `ActionNetworkPolicyResolver` 或等价 effective policy 结果决定 `SyncFacts` 是否映射为 outgoing packet、映射到哪个 SyncDomain、携带哪些 digest 或 history 数据。Adapter MUST NOT 形成第二套隐藏 action/window/motion/cue/result 策略。

#### Scenario: 发送 HitWindow digest

- **WHEN** `CharacterNetworkSendStage` 收集到 HitWindow SyncFact
- **THEN** Adapter MUST 读取 resolved window policy
- **AND** 如果策略为 digest only，Adapter MUST 生成 ActionSyncDomain digest packet
- **AND** Adapter MUST NOT 通过硬编码 WindowType 自行决定同步策略

#### Scenario: 过滤本地 Cue

- **WHEN** `CharacterNetworkSendStage` 收集到 local only cue fact
- **THEN** Adapter MUST 根据 resolved cue policy 不生成 outgoing packet
- **AND** Debug MUST 能记录该 fact 被策略过滤

### Requirement: Adapter packet preview 必须复用正式映射

ActionProfile Inspector 或 Runtime Debug 的 packet 预览 MUST 复用 Character outgoing adapter 的正式映射规则或共享映射描述。预览 MUST NOT 手写一套与 Runtime 不一致的展示逻辑。

#### Scenario: Inspector 预览 MotionSyncDomain

- **WHEN** 作者在 ActionProfile Inspector 查看 RootMotion 预览
- **THEN** 预览 MUST 使用与 adapter 一致的映射规则展示 MotionSyncDomain packet
- **AND** Runtime 发送结果 MUST 与预览保持同一口径

#### Scenario: 修改 Cue 策略

- **WHEN** 作者把某个 cue 从 local only 改为 server confirmed
- **THEN** Inspector 预览和 Runtime adapter 输出 MUST 通过同一映射规则更新
- **AND** 不需要在 adapter 里另行配置 cue id

### Requirement: Adapter 必须保持协议边界而不是作者配置入口

CharacterGameplaySyncAdapter MUST 只消费 SyncFacts、resolved policy 和 GameplaySyncRuntime 队列。Adapter MUST NOT 持有 ActionProfile 列表、Graph 节点引用、Timeline asset 引用或 Inspector-only 配置。

#### Scenario: Pipeline tick 后收集输出

- **WHEN** CharacterPipeline tick 完成并产出 SyncFacts
- **THEN** Adapter MUST 通过正式 send stage 读取 facts
- **AND** MUST 通过 resolver 或已附带的 effective policy 判断 packet 输出
- **AND** MUST NOT 回头读取 Graph 或 Timeline 来补策略

#### Scenario: 接收服务端 decision

- **WHEN** Adapter 收到 ActionInstanceDecision packet
- **THEN** Adapter MUST 注入 CharacterNetworkReceiveStage
- **AND** MUST NOT 直接修改 ActionRuntime、GraphContext 或 Transform

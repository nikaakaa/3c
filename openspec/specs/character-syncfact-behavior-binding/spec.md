# character-syncfact-behavior-binding Specification

## Purpose
定义 SyncFact 到稳定 BehaviorId、BehaviorProfile 或 Transaction ActionProfile 的显式绑定、策略解析和调试追踪边界。
## Requirements
### Requirement: SyncFact 必须作为 BehaviorId 的网络边界绑定点

系统 MUST 让进入网络边界的同步事实能够显式携带或解析到 `BehaviorId`。`BehaviorId` MUST 用于查询 Transaction `ActionProfile`、generic `GameplayBehaviorProfile` 或 `GameplayEffectDefinition`，再由当前 Network Model Profile 解析 effective policy。系统 MUST NOT 要求每个 Graph node、Timeline clip 或 Blackboard key 成为网络策略绑定点。

#### Scenario: Gameplay Effect lifecycle fact 使用 Effect 身份

- **WHEN** Gameplay Effect Runtime 产出 `Effect.CrowdControl.Stun` 的 lifecycle fact
- **THEN** 该 fact MUST 携带 `BehaviorId = Effect.CrowdControl.Stun`
- **AND** GameplayEffectDefinition MUST 提供同一 EffectId/BehaviorId 身份
- **AND** 模型 Adapter MUST 使用该 BehaviorId 从自己的模型 Profile 解析 Effect policy

#### Scenario: Cue 显式选择表现行为

- **WHEN** Timeline 或 Presentation runtime 产出 `Cue.HitSpark`
- **THEN** 该 cue fact MUST 携带或解析到 `BehaviorId = Cue.HitSpark`
- **AND** local-only、owner-only、broadcast 或 server-confirmed 语义 MUST 来自该 BehaviorProfile

### Requirement: Transaction facts 必须继续从 ActionProfile 解析 BehaviorId

Transaction facts MUST 继续通过 ActionId/ActionInstanceId 解析到 ActionProfile 的稳定 ActionId，并将其作为 BehaviorId。Network Model policy MUST 再使用该 ID 查询 model profile；ActionProfile MUST 不作为 effective network policy 来源。

#### Scenario: Attack Window fact

- **WHEN** adapter 收到带 ActionInstanceId 的 HitWindow fact
- **THEN** MUST 通过 ActionRuntime 解析 ActionId
- **AND** MUST 通过 ActionId 查询 ServerAuthoritative Action policy

### Requirement: 系统事实必须使用正式 SyncFact behavior binding

系统 MUST 为输入帧、motion correction acknowledgement 等没有 Graph/Timeline/Effect Definition 来源的系统 fact 提供正式 behavior binding。该 binding MUST 是统一配置表或等价正式配置，MUST NOT 是 Adapter hidden fallback。Gameplay Effect lifecycle fact MUST 直接使用 GameplayEffectDefinition 的 EffectId/BehaviorId，不得再配置一个固定 Effect behavior 槽位。Behavior binding 只决定 fact 的网络策略，MUST NOT 配置 MotionStage 的 correction application 算法。

#### Scenario: ClientCommandFrame 绑定 locomotion behavior

- **WHEN** `CharacterInputFrame` 生成 `ClientCommand`
- **THEN** 系统 MUST 从正式 SyncFact behavior binding 写入或解析 `Movement.Locomotion.Move`
- **AND** Adapter MUST 使用该 BehaviorId 解析 Stream policy
- **AND** Adapter MUST NOT 通过硬编码 fact type 自动选择 policy

#### Scenario: MotionCorrectionAck 绑定 correction behavior

- **WHEN** MotionStage 成功应用 correction 并产出 `MotionCorrectionAcknowledgement` fact
- **THEN** 该 fact MUST 携带或解析到 correction ack behavior
- **AND** resolver MUST 根据该 Stream behavior 的 authority 和 replication 解析网络可见性
- **AND** 缺失 binding 时 Adapter MUST 记录 Missing policy 并过滤
- **AND** Adapter MUST NOT 复用 incoming Correction payload

#### Scenario: Gameplay Effect 不使用固定槽位

- **WHEN** 同一 tick 产出多个 GameplayEffectLifecycleFact
- **THEN** 每条 fact MUST 使用自己的 EffectId/BehaviorId 解析模型 policy
- **AND** 系统 MUST NOT 通过固定 Effect behavior binding 合并它们的策略

### Requirement: Adapter 必须逐条 fact 解析 effective policy

Character outgoing Adapter MUST 对每条 outgoing fact 逐条解析 effective policy。Adapter MUST 使用 fact-level BehaviorId、ActionId 或 ActionInstanceId 查找 policy source。Adapter MUST NOT 因 fact type 相同而默认共用同一个 profile，除非该 fact 显式来自同一个 behavior binding。

#### Scenario: 同一 tick 多个 Gameplay Effect

- **WHEN** 同一 tick 同时产出 `Effect.CrowdControl.Stun` 和 `Effect.Defense.Invulnerable` 的 lifecycle fact
- **THEN** Adapter MUST 分别用两个 BehaviorId 解析 policy
- **AND** 两者 MAY 得到不同 replication、history 或 authority 策略

#### Scenario: 缺失 BehaviorId

- **WHEN** 非事务 fact 没有 BehaviorId 且没有正式 binding
- **THEN** Adapter MUST 记录 Missing policy
- **AND** 该 fact MUST 不发送 outgoing packet
- **AND** 系统 MUST NOT 使用隐藏默认策略补齐

### Requirement: Authoring UI 必须选择 BehaviorProfile 而不是手填完整策略

作者界面 MUST 让作者从 `CharacterPipelineDefinition` 的 Behavior registry 或等价资产引用中选择 BehaviorProfile。Graph node、Timeline clip 或 binding table MAY 引用 BehaviorProfile 或 BehaviorId，但 MUST NOT 暴露完整 prediction、authority、replication、snapshot、history 或 command policy 字段。GameplayEffectDefinition MUST 直接提供 Effect BehaviorId，模型 Profile MUST 单独配置该 BehaviorId 的模型策略。BehaviorProfile 与 CharacterPipelineDefinition MUST NOT 新增当前 direct correction 算法参数。

#### Scenario: 配置 Gameplay Effect 输出

- **WHEN** 作者配置 `Effect.CrowdControl.Stun`
- **THEN** GameplayEffectDefinition MUST 直接提供 EffectId/BehaviorId
- **AND** UI MUST NOT 为同一 Effect 再创建 generic GameplayBehaviorProfile
- **AND** UI MUST NOT 在 Effect Definition 或 Graph node 上重复配置完整网络 policy

#### Scenario: 查看 PipelineDefinition

- **WHEN** 作者选中 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 显示 Action、generic Behavior 与 Effect 的统一 Behavior registry
- **AND** 每个条目 MUST 显示 BehaviorId、BehaviorKind 和目标 SyncDomain

### Requirement: Debug 必须按 fact 展示 Behavior 解析结果

Model Debug MUST 按 fact 显示 BehaviorId、BehaviorKind、ModelId、model policy id、packet kind、发送/过滤状态和原因。Transaction fact MUST 同时显示 ActionProfile identity 与 model Action policy 来源，避免把两者视为同一资产。

#### Scenario: 查看被过滤 Cue

- **WHEN** ServerAuthoritative policy 过滤 local-only Cue
- **THEN** Debug MUST 显示 gameplay BehaviorId/ActionId
- **AND** MUST 显示当前 ModelId 和过滤 policy source


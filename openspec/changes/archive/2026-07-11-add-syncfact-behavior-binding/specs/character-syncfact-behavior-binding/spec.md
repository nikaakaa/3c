# character-syncfact-behavior-binding Specification

## ADDED Requirements

### Requirement: SyncFact 必须作为 BehaviorId 的网络边界绑定点

系统 MUST 让进入网络边界的同步事实能够显式携带或解析到 `BehaviorId`。`BehaviorId` MUST 用于查询 `GameplayBehaviorProfile` 或 Transaction `ActionProfile` 并解析 effective policy。系统 MUST NOT 要求每个 Graph node、Timeline clip 或 Blackboard key 成为网络策略绑定点。

#### Scenario: StateEffect 显式选择状态行为

- **WHEN** Graph 或 runtime 产出 `State.Stun` 的 StateEffect fact
- **THEN** 该 fact MUST 携带或解析到 `BehaviorId = State.Stun`
- **AND** Adapter MUST 使用该 BehaviorId 查询 State behavior policy
- **AND** Adapter MUST NOT 使用统一 `StateEffectBehavior` 固定槽位解释所有 StateEffect

#### Scenario: Cue 显式选择表现行为

- **WHEN** Timeline 或 Presentation runtime 产出 `Cue.HitSpark`
- **THEN** 该 cue fact MUST 携带或解析到 `BehaviorId = Cue.HitSpark`
- **AND** local-only、owner-only、broadcast 或 server-confirmed 语义 MUST 来自该 BehaviorProfile

### Requirement: Transaction facts 必须继续从 ActionProfile 解析 BehaviorId

系统 MUST 将 Transaction facts 的 BehaviorId 解析保持在 ActionProfile / ActionInstance 链路中。Action activation、lifecycle、window、action motion、action cue 和 action-sourced gameplay result MUST 通过 `ActionId` 或 `ActionInstanceId` 查询 Transaction behavior。系统 MUST NOT 为同一 Transaction fact 额外维护与 ActionProfile 冲突的 BehaviorId。

#### Scenario: ActionWindow 使用 ActionInstance

- **WHEN** Adapter 处理 `ActionWindowSample(ActionInstanceId = X, WindowType = Hit)`
- **THEN** Adapter MUST 通过 ActionInstanceId 找到对应 ActionProfile
- **AND** 该 ActionProfile 的 ActionId MUST 作为 Transaction BehaviorId
- **AND** WindowType 只选择该 ActionProfile 内的 window policy，不成为独立 BehaviorProfile

#### Scenario: 非 action 来源 result

- **WHEN** `GameplayResultEvent` 没有来源 ActionInstanceId
- **THEN** 该 result MUST 使用 fact-level BehaviorId 查询 Event behavior
- **AND** 系统 MUST NOT 假装它属于某个默认 ActionProfile

### Requirement: 系统事实必须使用正式 SyncFact behavior binding

系统 MUST 为输入帧、motion correction ack 等没有 Graph/Timeline 来源的系统 fact 提供正式 behavior binding。该 binding MUST 是统一配置表或等价正式配置，MUST NOT 是 Adapter hidden fallback。固定字段 `ClientCommandBehavior`、`StateEffectBehavior`、`MotionCorrectionAckBehavior` MUST 在迁移完成后删除或合并进统一 binding。

#### Scenario: ClientCommandFrame 绑定 locomotion behavior

- **WHEN** `CharacterInputFrame` 生成 `ClientCommand`
- **THEN** 系统 MUST 从正式 SyncFact behavior binding 写入或解析 `Movement.Locomotion.Move`
- **AND** Adapter MUST 使用该 BehaviorId 解析 Stream policy
- **AND** Adapter MUST NOT 通过硬编码 fact type 自动选择 policy

#### Scenario: MotionCorrectionAck 绑定 correction behavior

- **WHEN** MotionStage 产出 correction acknowledgement fact
- **THEN** 该 fact MUST 携带或解析到 correction ack behavior
- **AND** 缺失 binding 时 Adapter MUST 记录 Missing policy 并过滤

### Requirement: Adapter 必须逐条 fact 解析 effective policy

Character outgoing adapter MUST 对每条 outgoing fact 逐条解析 effective policy。Adapter MUST 使用 fact-level BehaviorId、ActionId 或 ActionInstanceId 查找 policy source。Adapter MUST NOT 因 fact type 相同而默认共用同一个 profile，除非该 fact 显式来自同一个 behavior binding。

#### Scenario: 同一 tick 多个 StateEffect

- **WHEN** 同一 tick 同时产出 `State.Stun` 和 `State.Invincible`
- **THEN** Adapter MUST 分别用两个 BehaviorId 解析 policy
- **AND** 两者 MAY 得到不同 replication、history 或 authority 策略

#### Scenario: 缺失 BehaviorId

- **WHEN** 非事务 fact 没有 BehaviorId 且没有正式 binding
- **THEN** Adapter MUST 记录 Missing policy
- **AND** 该 fact MUST 不发送 outgoing packet
- **AND** 系统 MUST NOT 使用隐藏默认策略补齐

### Requirement: Authoring UI 必须选择 BehaviorProfile 而不是手填完整策略

作者界面 MUST 让作者从 `CharacterPipelineDefinition` 的 Behavior registry 或等价资产引用中选择 BehaviorProfile。Graph node、Timeline clip 或 binding table MAY 引用 BehaviorProfile 或 BehaviorId，但 MUST NOT 暴露完整 prediction、authority、replication、correction policy 字段。

#### Scenario: 配置 StateEffect 输出

- **WHEN** 作者配置一个会产出 `State.Stun` 的节点或运行时输出
- **THEN** UI MUST 允许选择 `State.Stun` BehaviorProfile 或从 registry 中解析它
- **AND** UI MUST NOT 让该节点重复配置完整网络 policy

#### Scenario: 查看 PipelineDefinition

- **WHEN** 作者选中 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 显示 Behavior registry 和 SyncFact behavior bindings
- **AND** 每个 binding MUST 显示 fact kind、BehaviorId、BehaviorKind 和目标 SyncDomain

### Requirement: Debug 必须按 fact 展示 Behavior 解析结果

Runtime Debug MUST 能按 outgoing fact 展示 fact kind、BehaviorId、BehaviorKind、resolved SyncDomain、packet kind、policy id、发送或过滤状态和原因。Debug MUST 能区分 Transaction facts 的 ActionProfile 来源和非事务 facts 的 BehaviorProfile 来源。

#### Scenario: 查看被过滤 Cue

- **WHEN** `Cue.CameraShake` 配置为 local-only 并产出 cue fact
- **THEN** Runtime Debug MUST 显示 `BehaviorId = Cue.CameraShake`
- **AND** Debug MUST 显示该 fact 被 local-only policy 过滤

#### Scenario: 查看缺失配置

- **WHEN** `GameplayResultEvent` 没有 ActionInstanceId 且没有 BehaviorId
- **THEN** Runtime Debug MUST 显示 Missing policy
- **AND** Debug MUST 显示缺失的是 fact-level BehaviorId 或 registry profile

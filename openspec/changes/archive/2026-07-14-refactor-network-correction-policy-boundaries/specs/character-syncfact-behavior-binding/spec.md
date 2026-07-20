## MODIFIED Requirements

### Requirement: 系统事实必须使用正式 SyncFact behavior binding

系统 MUST 为输入帧、motion correction acknowledgement 等没有 Graph/Timeline/Effect Definition 来源的系统 fact 提供正式 behavior binding。该 binding MUST 是统一配置表或等价正式配置，MUST NOT 是 Adapter hidden fallback。Gameplay Effect lifecycle fact MUST 直接使用 GameplayEffectDefinition 的 EffectId/BehaviorId，不得配置固定 Effect behavior 槽位。固定字段 `ClientCommandBehavior`、`StateEffectBehavior`、`MotionCorrectionAckBehavior` MUST 在迁移完成后删除。Behavior binding 只决定 fact 的网络策略，MUST NOT 配置 MotionStage 的 correction application 算法。

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
- **AND** Adapter MUST NOT 复用 incoming Correction payload 或查询 correction application result 决定是否发送

### Requirement: Authoring UI 必须选择 BehaviorProfile 而不是手填完整策略

作者界面 MUST 让作者从 `CharacterPipelineDefinition` 的 Behavior registry 或等价资产引用中选择 BehaviorProfile。Graph node、Timeline clip 或 binding table MAY 引用 BehaviorProfile 或 BehaviorId，但 MUST NOT 暴露完整 prediction、authority、replication、snapshot、history 或 command policy 字段。本 change MUST NOT 在 BehaviorProfile 或 CharacterPipelineDefinition 中新增当前 direct correction 算法参数。

#### Scenario: 配置 Gameplay Effect 输出

- **WHEN** 作者配置 `Effect.CrowdControl.Stun`
- **THEN** GameplayEffectDefinition MUST 直接提供 EffectId/BehaviorId
- **AND** 模型 Profile MUST 按该 BehaviorId 配置完整 Effect policy
- **AND** UI MUST NOT 为该 Effect 创建固定 StateEffect binding 或重复配置完整网络 policy

#### Scenario: 查看 PipelineDefinition

- **WHEN** 作者选中 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 显示 Action、generic Behavior 与 Effect 的统一 Behavior registry 和系统 SyncFact behavior bindings
- **AND** 每个 binding MUST 显示 fact kind、BehaviorId、BehaviorKind 和目标 SyncDomain
- **AND** Ack binding MUST NOT 显示 Smooth、Force、partial/full application 或 Reject 配置

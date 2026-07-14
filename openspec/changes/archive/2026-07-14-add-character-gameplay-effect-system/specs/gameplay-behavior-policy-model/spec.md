## MODIFIED Requirements

### Requirement: Gameplay Behavior 必须提供统一行为身份

系统 MUST 使用 Gameplay Behavior 或等价模型为所有 gameplay 行为提供统一作者身份。每个 behavior MUST 至少声明稳定 `BehaviorId`、`BehaviorKind`、tags、display name、debug category 和网络策略摘要。Gameplay Behavior MUST 是作者和策略层身份，MUST NOT 直接替代 Graph 节点、Timeline clip、ActionInstance、MotionContribution、GameplayEffectLifecycleFact 或 CueEvent。

#### Scenario: 作者配置轻攻击

- **WHEN** 作者配置 `Attack.Light.01`
- **THEN** 该行为 MUST 有稳定 `BehaviorId = Attack.Light.01`
- **AND** 该行为 MUST 标记为 `BehaviorKind.Transaction`
- **AND** 它的运行时执行 MUST 继续通过 ActionInstance 和 ActionSyncDomain，而不是通过 Graph 路径或 Timeline asset 身份同步

#### Scenario: 作者配置普通移动

- **WHEN** 作者配置普通 locomotion 或移动输入行为
- **THEN** 该行为 MUST 有稳定 BehaviorId，例如 `Movement.Locomotion.Move`
- **AND** 该行为 MUST 标记为 `BehaviorKind.Stream`
- **AND** 系统 MUST NOT 为每一帧普通移动创建 ActionInstance

### Requirement: BehaviorKind 必须决定运行时同步单位

系统 MUST 使用 `BehaviorKind` 决定 behavior 的运行时同步单位。`Transaction` MUST 使用 ActionInstance 和 ActionSyncDomain；`Stream` MUST 使用 input command、MotionSyncDomain、snapshot 和 correction；`Effect` MUST 使用 EffectInstanceId 和 GameplayEffectSyncDomain；`Event` MUST 根据 policy 使用 GameplayResultSyncDomain 或 PresentationSyncDomain。系统 MUST NOT 把所有 behavior 强制映射到同一种 runtime identity。

#### Scenario: 连续移动和攻击同帧发生

- **WHEN** 本地玩家同一 tick 内持续移动并启动轻攻击
- **THEN** 移动 behavior MUST 通过 Stream 语义进入 MotionSyncDomain
- **AND** 攻击 behavior MUST 通过 Transaction 语义进入 ActionSyncDomain
- **AND** 两者 MAY 共享 input sequence 或 actor identity，但 MUST 使用不同同步单位

#### Scenario: Effect 来源于动作

- **WHEN** `Guard.Counter` 成功后产生短暂无敌 Effect
- **THEN** 无敌 MUST 作为 Effect behavior 进入 GameplayEffectSyncDomain
- **AND** 它 MAY 记录来源 `ActionInstanceId`
- **AND** 它自身 EffectInstance 生命周期 MUST NOT 等同于来源 ActionInstance

## ADDED Requirements

### Requirement: Effect behavior 必须由 GameplayEffectDefinition 直接提供

`GameplayBehaviorKind.Effect` 的正式 gameplay identity profile MUST 由 `GameplayEffectDefinition` 直接实现。`EffectId` MUST 等于该 profile 的 `BehaviorId`，并由统一 gameplay registry 参与重复身份与 runtime lookup 校验。EffectDefinition MUST NOT 保存任何具体 Network Model policy；模型 Profile MUST 使用该 BehaviorId 完成自己的 coverage、policy 完整性与 resolver lookup。系统 MUST NOT 为同一 Effect 建立 generic BehaviorProfile 副本。

#### Scenario: 解析眩晕同步策略

- **WHEN** GameplayEffectLifecycleFact 引用 `Effect.CrowdControl.Stun`
- **THEN** GameplayEffectDefinition MUST 提供对应 Effect BehaviorId
- **AND** ServerAuthoritative resolver MUST 通过模型 Profile 中该 BehaviorId 的条目解析有效 Effect policy
- **AND** Graph、GE Runtime 或 Character Adapter MUST NOT 硬编码该 Effect 的网络策略

#### Scenario: Generic profile 与 Effect 身份冲突

- **WHEN** generic BehaviorProfile 和 GameplayEffectDefinition 声明同一 BehaviorId
- **THEN** authoring validation MUST 报告重复身份并拒绝 registry
- **AND** resolver MUST NOT 按注册顺序挑选 profile

### Requirement: 旧 State behavior kind 必须删除

系统 MUST 将 `GameplayBehaviorKind.State` 一次性改名为 `GameplayBehaviorKind.Effect`，并更新 registry、resolver、Inspector、diagnostics 与模型 Profile。系统 MUST NOT 保留 State 枚举别名、兼容 switch 分支或把 objective state 继续解释为 Effect behavior。

#### Scenario: 解析旧 State 枚举值

- **WHEN** 迁移完成后的配置或运行时数据仍引用 GameplayBehaviorKind.State
- **THEN** 配置构建 MUST 报告已经删除的旧行为种类
- **AND** 系统 MUST NOT 自动映射或 fallback 到 GameplayBehaviorKind.Effect

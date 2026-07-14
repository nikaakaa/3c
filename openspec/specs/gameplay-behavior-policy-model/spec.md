# gameplay-behavior-policy-model Specification

## Purpose
定义 Transaction、Stream、Effect 和 Event 行为的统一 BehaviorId、BehaviorKind、authoring profile 与网络策略解析模型。
## Requirements
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

### Requirement: ActionProfile 必须收敛为 Transaction behavior 入口

ActionProfile MUST 继续作为 Transaction gameplay identity 与动作定义入口，并使用 ActionId 作为 BehaviorId。ActionProfile MUST 保存 gameplay tags、block/cancel 和 target 语义，但 MUST 不保存任何具体 Network Model 的 prediction、authority、replication、window/motion/cue/result 网络策略。

#### Scenario: Attack.Light.01

- **WHEN** CharacterPipelineDefinition 注册 `Attack.Light.01` ActionProfile
- **THEN** ActionRuntime MUST 使用它建立动作身份和 gameplay 约束
- **AND** ServerAuthoritative model MUST 通过 ActionId 在自己的 profile 中解析网络策略

### Requirement: Stream behavior 必须显式配置连续运动网络策略

需要进入 ServerAuthoritative 模型的 Stream behavior MUST 在 `ServerAuthoritativeCharacterSyncProfile` 中显式配置 command send、prediction、authority、snapshot、remote presentation、replication 和 history。Gameplay behavior identity MUST 不保存这些字段，且模型 profile 缺失时 MUST 配置失败。

#### Scenario: Locomotion Stream policy

- **WHEN** Locomotion resolved motion 需要进入当前模型
- **THEN** model profile MUST 存在对应 BehaviorId 的 Stream policy
- **AND** adapter MUST 不从 GameplayBehavior identity 读取默认策略

### Requirement: Behavior policy resolver 必须输出统一 effective policy

每个 Network Model MUST 提供自己的 Behavior policy resolver。ServerAuthoritative resolver MUST 使用 model profile、BehaviorId、BehaviorKind、fact kind 和可选输出类型解析 effective packet policy。系统 MUST 不提供跨所有模型的统一 packet resolver，也 MUST 不让 CharacterPipeline 调用 model resolver。

#### Scenario: 解析 resolved motion

- **WHEN** ServerAuthoritative adapter 处理 resolved motion fact
- **THEN** resolver MUST 从当前 model profile 解析是否发送、history 和 packet kind
- **AND** 其它模型 MUST 不需要复用该 effective policy 类型

### Requirement: BehaviorId 不得替代 SyncFacts 边界

BehaviorId MUST 只作为 gameplay identity 与 model policy lookup key。Graph、Timeline 和 runtime MUST 继续产生正式 facts；model adapter MUST 不因为拥有 BehaviorId 而重新读取 Graph 或凭空构造未发生的事实。

#### Scenario: Window policy 存在但窗口未发生

- **WHEN** ServerAuthoritative profile 配置了 HitWindow policy，但本 tick 没有 HitWindow fact
- **THEN** adapter MUST 不生成 window packet
- **AND** policy MUST 不驱动 Timeline 或 Blackboard

### Requirement: Authoring 和 Debug 必须按 Behavior 展示同步闭环

Gameplay behavior authoring MUST 展示 identity 和 gameplay kind；model-specific authoring MUST 在自己的 profile Inspector 展示 prediction、authority、replication、history、snapshot 和 expected packet。Runtime Debug MUST 同时显示 BehaviorId 与 ModelId，避免把 model policy 误认为 gameplay identity 自身字段。

#### Scenario: 查看 Locomotion policy

- **WHEN** 作者查看 ServerAuthoritative Character Sync Profile 中的 Locomotion
- **THEN** UI MUST 显示引用的 gameplay BehaviorId
- **AND** MUST 显示该模型的 effective policy 与 packet preview

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

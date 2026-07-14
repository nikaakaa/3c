## REMOVED Requirements

### Requirement: StateEffectSyncDomain 必须处理状态和效果实例

旧 `StateEffectSyncDomain` 将角色 Effect、资源/冷却与 objective state 混成一个通用状态桶，并使用 `StateId` 或 `EffectInstanceId` 作为不稳定的二选一身份。该 requirement MUST 被正式 `GameplayEffectSyncDomain` requirement 替代，旧合同不得保留。

#### Scenario: 迁移旧状态效果占位

- **WHEN** 本变更实施到 Character semantic fact 和网络模型边界
- **THEN** 系统 MUST 删除旧 StateEffectSyncDomain requirement 与运行时合同
- **AND** 系统 MUST NOT 用兼容别名或双写继续维持旧状态桶

## ADDED Requirements

### Requirement: GameplayEffectSyncDomain 必须处理角色 Effect 生命周期与 Attribute 数值

系统 MUST 使用 `GameplayEffectSyncDomain` 表达正式 Gameplay Effect 实例生命周期与 Gameplay Attribute 数值。Effect 生命周期事实的稳定同步键 MUST 是 `EffectInstanceId + LifecycleRevision`，并 MUST 携带稳定 `EffectId/BehaviorId`；Attribute 数值事实的稳定同步键 MUST 是 `AttributeId + ValueRevision`。Buff、Debuff、Stun、Invulnerability、Dead、Downed、Revive、Resource 和 Cooldown 只有通过正式 Effect/Attribute Definition 表达时才 MAY 进入该同步域。

#### Scenario: 应用眩晕效果

- **WHEN** gameplay result 对角色应用 `Effect.CrowdControl.Stun`
- **THEN** GameplayEffectSyncDomain MUST 产出 `GameplayEffectLifecycleFact`
- **AND** 该事实 MUST 使用 EffectId、EffectInstanceId 和 lifecycle operation 维护效果实例

#### Scenario: 动作触发无敌效果

- **WHEN** `Guard.Counter` 成功后给予短暂无敌效果
- **THEN** GameplayEffectSyncDomain MUST 表达该 Effect 实例的生命周期
- **AND** 该事实 MAY 记录来源 ActionInstanceId，但 EffectInstanceId MUST 是自身同步身份

#### Scenario: 属性被效果修改

- **WHEN** Damage Effect 修改目标 Health
- **THEN** GameplayEffectSyncDomain MUST 产出带 AttributeId、Base、Current 和 ValueRevision 的 `GameplayAttributeValueFact`
- **AND** 网络边界 MUST NOT 复制完整 Attribute Store 或 Modifier 容器

### Requirement: GameplayEffectSyncDomain 必须使用类型化模型无关事实

`GameplayEffectSyncDomain` MUST 使用 `GameplayEffectLifecycleFact` 与 `GameplayAttributeValueFact`。Effect fact MUST 至少携带 EffectId/BehaviorId、EffectInstanceId、source actor、target actor、stack、lifecycle operation、LifecycleRevision、context、DefinitionRevision 和 logic tick；Attribute fact MUST 至少携带 AttributeId、old/new value、ValueRevision、context 和 logic tick。共享模型合同 MUST 为 model-neutral 类型，不得引用 UnityEngine、ScriptableObject、Graph、Timeline 或场景对象。

#### Scenario: 服务端确认预测效果

- **WHEN** incoming GameplayEffectLifecycleFact 使用相同 PredictionKey 和 GameplayResultId 确认本地预测 Effect
- **THEN** Gameplay Effect Runtime MUST 协调对应 active instance 和 journal entry
- **AND** 系统 MUST NOT 额外创建重复 Effect

#### Scenario: 远端属性修正

- **WHEN** incoming GameplayAttributeValueFact 携带更高 ValueRevision 的权威 CurrentValue
- **THEN** target pipeline MUST 通过 Gameplay Effect authority input 协调该属性
- **AND** Adapter MUST NOT 把 Unity asset 或 runtime modifier object 放入共享模型

### Requirement: 旧 GameplayState 和 StateEffect 合同必须删除

系统 MUST 删除 `GameplayStateEffectFact`、`StateEffectSyncDomainInput`、`StateEffectSyncDomainOutput`、`GameplayBehaviorKind.State`、旧 `StateId` 与旧 `PayloadDigest` 解析路径。正式合同 MUST 分别命名为 `GameplayEffectLifecycleFact`、`GameplayEffectSyncDomainInput`、`GameplayEffectSyncDomainOutput` 与 `GameplayBehaviorKind.Effect`。系统 MUST NOT 保留旧类型别名、兼容构造函数、枚举别名、双写或 fallback 解析。

#### Scenario: 迁移完成后扫描旧合同

- **WHEN** Runtime 与 current specs 完成 Gameplay Effect 迁移
- **THEN** 正式运行时路径 MUST 不再引用 GameplayStateEffectFact、StateEffectSyncDomain 或 GameplayBehaviorKind.State
- **AND** GameplayEffectLifecycleFact MUST 不再使用 StateId 或 PayloadDigest 表达效果语义

### Requirement: Objective 状态不得进入角色 GameplayEffectSyncDomain

Objective ownership、capture progress、contested、team control 等目标玩法状态 MUST NOT 使用角色 `GameplayEffectSyncDomain`、EffectId 或 EffectInstanceId 表达。离散 objective 结果 MUST 继续归属 `GameplayResultSyncDomain`；未来若需要持续 objective 状态，系统 MUST 新增独立 Objective/Event 合同。

#### Scenario: 目标点归属变化

- **WHEN** 服务端确认目标点从 contested 变成 TeamA captured
- **THEN** 系统 MUST 产出 objective GameplayResult
- **AND** 系统 MUST NOT 为目标点创建角色 Gameplay Effect 实例

### Requirement: Gameplay Effect 网络策略必须由模型 Profile 解析

EffectDefinition MUST 只用 EffectId 提供稳定 BehaviorId 和 Effect kind。Gameplay Effect output 的有效 prediction、authority、replication、sync 和 history policy MUST 由当前 Network Model 在自己的 Profile 中按该 BehaviorId 解析；当前 `ServerAuthoritativeHybrid` MUST 只从 `ServerAuthoritativeCharacterSyncProfile` 解析。CharacterGameplayEffectAdapter、FactProjector、CharacterNetworkSendStage、Effect component 和 Graph node MUST NOT 保存或解析模型策略。Model-owned Adapter MUST 根据 resolver 结果过滤或构造 packet，不得回读 Effect 配置推断 policy。

#### Scenario: LocalOnly Effect

- **WHEN** ServerAuthoritative model profile 将某个 Effect BehaviorId 配置为 LocalOnly
- **THEN** 本地 Gameplay Effect 与 diagnostics MAY 处理其生命周期事实
- **AND** Model-owned Adapter MUST 不为该 GameplayEffectLifecycleFact 构造 outgoing packet

#### Scenario: ClientPredicted Effect

- **WHEN** ServerAuthoritative model profile 为 Effect BehaviorId 配置 action-scoped history
- **THEN** 输出事实 MUST 携带 PredictionKey 和可协调 context
- **AND** ServerAuthoritative model history MUST 只记录该 Effect/Action 所需变更，不得强制全角色世界回滚

## MODIFIED Requirements

### Requirement: Action 和 Presentation 输出必须继续归属同步域

动作窗口、动作生命周期、玩法结果、Gameplay Effect 生命周期、Attribute 状态和表现 cue 的网络可见输出 MUST 继续分别归属 Action、GameplayResult、GameplayEffect 或 Presentation SyncDomain。Blackboard MAY 缓存最近输出，但缓存身份 MUST NOT 成为网络同步单位。

#### Scenario: 攻击窗口输出

- **WHEN** `Attack1Hit` window 产生
- **THEN** 可同步事实 MUST 进入 ActionSyncDomain
- **AND** blackboard 中的最近 window 缓存 MUST NOT 替代 `ActionInstanceId`、window id 和 tick 等同步身份

#### Scenario: 本地表现 cue

- **WHEN** Timeline 触发 local-only camera cue
- **THEN** 该 cue MAY 写入 blackboard 或 presentation output
- **AND** 只有 policy 标记为 replicated 的 cue 才 MAY 进入 PresentationSyncDomain outgoing

#### Scenario: Effect 生命周期输出

- **WHEN** Gameplay Effect Runtime 应用或移除一个 Effect 实例
- **THEN** 可同步事实 MUST 进入 GameplayEffectSyncDomain
- **AND** Blackboard MUST NOT 成为 Effect 或 Attribute 的状态真相

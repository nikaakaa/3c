# character-network-sync-domain-contract Specification

## Purpose
定义 Graph 主体下的网络同步域合同：Graph、Timeline 和 runtime 只产出 typed output，Pipeline 和 NetworkSendStage 按 Motion、Action、GameplayResult、GameplayEffect 和 Presentation 等 SyncDomain 处理稳定身份、策略和同步边界。
## Requirements
### Requirement: 网络 SyncDomain 必须表达输出同步语义

系统 MUST 使用 SyncDomain 对 Character gameplay facts 进行稳定业务分类，使 recording、debug 和具体 Network Model 可以识别 Motion、Action、GameplayResult、GameplayEffect 与 Presentation。SyncDomain MUST NOT 定义 packet kind、prediction/correction 算法、snapshot 策略、endpoint 或 transport。Graph 节点路径、SubTree membership 和 Timeline membership MUST NOT 成为同步单位。

#### Scenario: 同一事实进入当前模型

- **WHEN** CharacterPipeline 产生 ActionWindow fact
- **THEN** fact MUST 保持 ActionSyncDomain 和稳定 action/window identity
- **AND** 是否生成 ServerAuthoritative digest packet MUST 由该模型 policy 决定

### Requirement: MotionSyncDomain 必须处理连续运动同步

MotionSyncDomain MUST 表达 canonical input frame identity、本地 prediction result、external pose input 和 correction application result 等连续运动语义。CharacterPipeline MUST 不生成 model packet、ClientCommandFrame、MotionCommand 或 CorrectionAck；具体模型 adapter MUST 选择所需事实并构造自己的命令和 acknowledgement。`ResolvedCharacterMotionFact` MUST 表达本地已经发生的运动结果，MUST NOT 被通用合同定义为服务端 canonical motion intent。

#### Scenario: 本地运动完成

- **WHEN** CharacterMotionStage 完成本 tick LocalSolver 结算
- **THEN** MUST 产生 resolved motion fact
- **AND** ServerAuthoritative adapter MAY 将它用于 prediction comparison、diagnostics 或 correction provenance
- **AND** 服务端权威模拟 MUST 从 canonical input/action state 独立生成 motion intent

#### Scenario: 未来模型消费 canonical input

- **WHEN** Network Model 需要在远端重演或独立模拟角色运动
- **THEN** model adapter MUST 从正式 input/action facts 构造模型命令
- **AND** MUST 不把客户端 actual displacement 当作唯一权威输入

### Requirement: ActionSyncDomain 必须处理离散动作事务
系统 MUST 使用 ActionSyncDomain 表达有明确 activation、confirm、reject、cancel、correct 或 end 生命周期的离散动作事务。ActionSyncDomain 的稳定同步键 MUST 是 `ActionInstanceId` 或等价 action instance identity。

#### Scenario: 启动轻攻击
- **WHEN** Graph 提交 `ActionActivationRequest(ActionId = Attack.Light.01)`
- **THEN** ActionRuntime MUST 在接受后创建 action instance identity
- **AND** ActionSyncDomain MUST 能按该 identity 聚合 activation、window、action-scoped motion、cue、result 和 end 输出

#### Scenario: 普通 locomotion 不进入 ActionSyncDomain
- **WHEN** Graph 只处理走跑跳等连续运动
- **THEN** 系统 MUST 使用 MotionSyncDomain 输出
- **AND** ActionSyncDomain MUST NOT 强制参与该帧同步

### Requirement: GameplayResultSyncDomain 必须处理权威玩法结果
系统 MUST 使用 GameplayResultSyncDomain 表达命中、伤害、格挡、破防、硬直、受击确认、objective 结果、PvE aggro/threat、revive/respawn 和 score/result event 等权威玩法结果。GameplayResultSyncDomain 的稳定同步键 MUST 是 `GameplayResultId` 或等价 result identity。GameplayResultSyncDomain MAY 关联来源 `ActionInstanceId`，但 MUST NOT 依赖 action 才能表达事件。

#### Scenario: 攻击命中
- **WHEN** 服务端或 hit/result solver 确认某个 hit window 命中目标
- **THEN** GameplayResultSyncDomain MUST 产出 gameplay result，包含 gameplay result id、source actor、target actor、tick 和结果摘要
- **AND** 如果该命中来源于 action window，result MUST 能携带对应 `ActionInstanceId` 和 window id

#### Scenario: 环境伤害
- **WHEN** 角色受到非 action 来源的环境伤害
- **THEN** GameplayResultSyncDomain MUST 能产出 gameplay result
- **AND** 该 result MUST NOT 需要 `ActionInstanceId`

#### Scenario: 目标点归属变化
- **WHEN** 服务端确认目标点从 contested 变成 TeamA captured
- **THEN** GameplayResultSyncDomain MUST 能产出 objective result
- **AND** 该 result MUST NOT 需要 `ActionInstanceId` 或 action window

### Requirement: PresentationSyncDomain 必须处理表现事件
系统 MUST 使用 PresentationSyncDomain 表达 VFX、SFX、camera shake、hit stop、post-process cue 和本地 animation cue。PresentationSyncDomain 的稳定同步键 MUST 是 `CueEventId` 或等价表现事件 identity。PresentationSyncDomain 默认 MAY 是 local-only，只有 policy 要求时才复制。

#### Scenario: 本地攻击特效
- **WHEN** Timeline 触发 `slash_vfx`
- **THEN** PresentationSyncDomain MAY 本地播放该 cue
- **AND** 如果 cue 来源于 action，cue event MAY 携带 `ActionInstanceId`

#### Scenario: 远端需要看到表现
- **WHEN** 某个 cue policy 配置为 replicated
- **THEN** NetworkSendStage MUST 能从 PresentationSyncDomain 生成 cue packet
- **AND** Graph 或 Timeline MUST NOT 直接发送该 cue

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包

CharacterNetworkSendStage 或等价输出 stage MUST 只收集 CharacterInputFrame、resolved motion 和 SyncFacts，并保留 BehaviorId、ActionId、SyncDomain 与稳定 identity。它 MUST 不解析 model policy 或构造 packet。Model-owned adapter MUST 使用当前 model profile 决定过滤、history 和 packet 映射，并 MUST 区分 canonical command input 与本地 prediction result。

#### Scenario: 本地预测角色输出一帧事实

- **WHEN** 本 tick 产生 input、resolved motion、action activation 和 window facts
- **THEN** output stage MUST 原样暴露对应 gameplay facts
- **AND** ServerAuthoritative adapter MUST 从 canonical input/action facts 构造权威端命令
- **AND** resolved motion MAY 作为 prediction comparison metadata，但 MUST 不替代权威端模拟

#### Scenario: 没有 Network Model

- **WHEN** CharacterPipeline 以单机方式运行且没有 model session 消费 facts
- **THEN** Pipeline MUST 继续正常执行
- **AND** facts MAY 只供 debug 或 recording 使用

### Requirement: NetworkReceiveStage 必须按 SyncDomain 注入网络结果

CharacterNetworkReceiveStage 或等价输入 stage MUST 只接收 Character/gameplay 语义输入，例如 `ActionLifecycleTransition`、ExternalPoseCorrection、ExternalPoseSample、`IncomingGameplayResult`、`GameplayEffectLifecycleFact`、`GameplayAttributeValueFact` 和 `GameplayCueFact`。Model packet MUST 先由 model-owned adapter 转换；stage MUST NOT 引用 packet、endpoint、history 或 transport。

#### Scenario: 服务端动作确认

- **WHEN** ServerAuthoritative adapter 收到 ActionDecision packet
- **THEN** MUST 先转换为 `ActionLifecycleTransition`
- **AND** input stage MUST 只把该通用生命周期输入交给 Character action runtime

#### Scenario: 运动校正

- **WHEN** model adapter 收到 MotionCorrection
- **THEN** MUST 转换为 ExternalPoseCorrection
- **AND** 最终应用 MUST 仍由 CharacterMotionStage 完成

### Requirement: History 必须按 policy 使用而非强制全局回滚

History 的存储内容、保留范围和恢复方式 MUST 由当前 Network Model 拥有。Character SyncFacts MAY 携带稳定 tick、sequence 和 instance identity，但 CharacterPipeline MUST 不持有 model packet history，也 MUST 不把 model correction history 描述为未来 Rollback history。

#### Scenario: 当前模型记录动作事务

- **WHEN** ServerAuthoritative policy 要求记录 Action activation/window digest
- **THEN** 该 history MUST 保存于 ServerAuthoritative model session
- **AND** ActionRuntime MUST 只保存自己的 gameplay lifecycle 状态

### Requirement: Blackboard 变量不得默认网络同步

系统 MUST NOT 默认同步 Pipeline Blackboard 的所有变量。Blackboard variable 只有在声明了明确 sync policy，并由正式 resolver 映射成 SyncFacts 后，才 MAY 被 NetworkSendStage 消费。系统 MUST NOT 引入通用 blackboard key/value 网络包作为角色 pipeline 的默认同步路径。

#### Scenario: 本地调试变量

- **WHEN** 某个 blackboard variable 只用于本地 debug 或状态内部判断
- **THEN** 该变量 MUST 保持 local-only
- **AND** NetworkSendStage MUST 不读取该变量
- **AND** outgoing packet MUST 不包含该变量 key/value

#### Scenario: 变量声明为 SyncFact

- **WHEN** 某个变量声明的 sync policy 要求输出为同步事实
- **THEN** resolver MUST 将该变量或事件转换为对应 SyncDomain output
- **AND** NetworkSendStage MUST 只读取转换后的 SyncFacts

### Requirement: 可调参数必须通过配置身份对齐

可调参数类 blackboard variable MUST 通过 pipeline 配置版本、角色 loadout identity、ActionProfile identity 或等价配置 hash 对齐。系统 MUST NOT 将 WalkThreshold、RunThreshold、TurnAngle 等可调参数作为每帧同步事实发送，除非后续 spec 明确要求热更新配置同步。

#### Scenario: 本地预测移动阈值

- **WHEN** 本地和服务端需要使用同一套 locomotion 阈值
- **THEN** 它们 MUST 通过角色配置身份或配置 hash 确认一致
- **AND** 输入帧同步 MUST 不携带每个阈值的逐帧值

#### Scenario: 配置版本不一致

- **WHEN** 接收端发现角色 pipeline 配置版本不一致
- **THEN** 系统 MUST 将其作为配置不一致问题报告
- **AND** 系统 MUST NOT 用网络包中的临时阈值覆盖本地正式配置

### Requirement: 输入派生变量不得作为独立同步事实

由输入帧、tick、配置和当前状态计算出的 Blackboard variable SHOULD NOT 作为独立 gameplay fact。具体 Network Model MUST 选择 canonical input、resolved motion 或权威结果作为自己的同步合同，不得通过同步全部派生 Blackboard 值绕过正式模型设计。

#### Scenario: MoveAxisMagnitude

- **WHEN** Graph 从 MoveAxis 计算输入幅度
- **THEN** 该值 MAY 留在本地 Blackboard
- **AND** ServerAuthoritative adapter MUST 不把它作为独立 packet 字段，除非正式 model contract 明确要求

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


## Context

当前项目已经把“动作执行”和“角色状态”分开：ActionRuntime 管 ActionInstance 事务，BTSMTL 管行为编排，Motion/Animation/Presentation 各自处理结果，SyncFacts 暴露业务事实。GE 应填补的是角色持续状态与属性计算，而不是重新发明一套 Ability 执行框架。

旧轻量实现提供了三个有价值的原型：

```text
BuffData / BuffInfo / BuffHandler
  -> Definition、Active Instance、叠层和增删队列

PropertyHandler / BasicProperty / Modifier
  -> Base、Modifier、聚合和脏标记

test.puml
  -> 应用许可、数值修改、反射与循环保护的业务分类
```

旧代码的问题不是概念错误，而是 ownership 和链路不适合当前工程：Buff 自己使用 MonoBehaviour Update，属性通过 Coroutine 维护时长，模块使用 `params object[]`，标签和属性都是字符串，且 Buff 与 Property 没有正式连接。新设计保留概念，重建为 CharacterPipeline 内的一条正式链路。

## Goals

- 让伤害、治疗、资源消耗、冷却、Buff、Debuff、Stun、无敌和移动速度修正使用同一 Effect 语义。
- 让玩家、敌人和 PvE CharacterPipeline 共用同一 GE 规则内核。
- 让 BTSMTL 像项目蓝图一样读取和调用 GE，复杂计算留在 C# 领域实现。
- 让 Action、StateMachine、Motion、网络和表现消费 GE 结果，但不互相取得对方的所有权。
- 让本地预测的资源消耗和冷却可以跟随 Action Confirm/Reject 收口，而不做全世界 Rollback。
- 让旧轻量 GE 的有效设计得到迁移，同时彻底删除不适合当前项目的生命周期和字符串路径。

## Terms

- **Gameplay Tag**：有稳定身份和父子层级的状态标记。运行时按来源计数。
- **Gameplay Attribute**：角色可计算数值，保存 Base 与 Current，并由 Modifier 聚合。
- **Gameplay Effect Definition**：作者创建的只读效果定义，描述持续策略、组件、条件、叠层和表现绑定。
- **Gameplay Effect Context**：一次应用的来源、目标、ActionInstance、PredictionKey、GameplayResult 和 Tick 信息。
- **Gameplay Effect Spec**：根据 Definition 与 Context 创建的一次应用规格，锁定声明式参数和需要快照的数值。
- **Active Gameplay Effect**：目标 Character 上仍在生效的 Duration/Infinite 实例。
- **Gameplay Effect Runtime**：通用 Gameplay 程序集中的单实体状态内核，唯一持有 Tag、Attribute、Active Effect 与 prediction journal，不认识 CharacterPipeline。
- **Character Gameplay Effect Adapter**：由 CharacterPipeline 唯一持有的薄适配层，负责 Character semantic input、GE command/change set 和 Character facts 之间的翻译与时序，不实现 GE 业务规则。
- **Gameplay Effect Port**：Action、Motion、BTSMTL 和 authority input 分别使用的窄读写合同；消费者不会得到完整 Runtime 或 Container。
- **Gameplay Effect ChangeSet**：Runtime 在当前 Tick 产生的 Effect、Attribute、Tag 和 Cue 结构化变化，是 Projector 的唯一输入。
- **Gameplay Effect Lifecycle Fact**：表达 Effect 实例 apply、confirm、stack、inhibit、remove、expire 和 correct 的模型无关语义事实，不是 packet，也不拥有网络策略。
- **Gameplay Attribute Value Fact**：表达 Attribute Base/Current 与 ValueRevision 的模型无关语义事实，不是另一个属性容器。

## Target Chain

```text
CharacterPipelineDefinition
  CharacterGameplayEffectProfile
    TagCatalog
    AttributeDefinitions + InitialValues
    EffectDefinitions
            |
            v
CharacterPipeline
  CharacterGameplayEffectAdapter
    InputMapper -> GameplayEffectRuntime
                     GameplayTagContainer
                     GameplayAttributeStore
                     ActiveGameplayEffectContainer
                     PredictionJournal
                          |
                          +-> GameplayEffectChangeSet
    FactProjector  <- ChangeSet -> Character SyncFacts
    CueProjector   <- ChangeSet -> GameplayCueFact
    TraceProjector <- ChangeSet -> GameplayEffect trace
            |
            +-> ActionRuntime receives IGameplayTagReader only
            +-> CharacterGraphContext receives query/command ports only
            +-> Motion receives IGameplayAttributeReader only
```

通用 Gameplay 域只提供合同、数据结构和计算规则；只有 CharacterPipeline 创建并推进 CharacterGameplayEffectAdapter，Adapter 再唯一持有 GameplayEffectRuntime。不存在 GE MonoBehaviour、静态单例、独立 Update 或独立网络 Runtime。接入 Pipeline 是生命周期装配，不是让 GE 核心依赖 Pipeline。

## Module Ownership

```text
Runtime/Gameplay
  ThirdPersonGameplay.asmdef
  Contracts
    GameplayBehaviorKind、IGameplayBehaviorProfile
    IGameplayTagReader、IGameplayAttributeReader、IGameplayEffectCommandSink
    GameplayEffectTickContext、GameplayEffectAuthorityInput、GameplayEffectChangeSet
  Tags
    TagId、Catalog、Query、source-counted container
  Attributes
    AttributeId、Definition、Value、Modifier、Aggregator、Store
  Effects
    Definition、Context、Spec、Handle、ActiveEffect、Components、Container
  Runtime
    GameplayEffectRuntime、immutable runtime definition、prediction journal

Runtime/Character/Pipeline/GameplayEffect
  CharacterGameplayEffectAdapter
  CharacterGameplayEffectInputMapper
  CharacterGameplayEffectFactProjector
  CharacterGameplayCueProjector
  CharacterGameplayEffectTraceProjector
```

`ThirdPersonGameplay` 是编译边界，不只是目录约定。它不得引用 `ThirdPersonCharacter`、BTSMTL、Character SyncFacts、ServerAuthoritativeHybrid、Presentation 或 Diagnostics。当前位于 `ThirdPersonCharacter.Behavior` 的通用 behavior identity enum/interface 必须迁移到 Gameplay Contracts，避免 EffectDefinition 为实现 Effect identity 形成反向依赖。Character 适配层可以依赖 Gameplay 程序集，反向依赖不成立。

Character 适配层不拥有自己的 Tick 来源，只由 CharacterPipeline 调用。它不能重新实现叠层、聚合、周期、预测协调或网络策略；这些规则分别归属 GameplayEffectRuntime 与模型专属 Adapter/Profile。

## Ports and Data Exchange

正式端口保持按消费者拆分：

```text
IGameplayTagReader
  HasTag(TagId)
  Matches(TagQuery)

IGameplayAttributeReader
  TryGetValue(AttributeId)

IGameplayEffectCommandSink
  CanApply(ApplyRequest)
  Apply(ApplyRequest)
  Remove(RemoveRequest)

IGameplayEffectAuthorityInputSink
  Reconcile(AuthorityInput)
```

同一个 CharacterGameplayEffectAdapter 可以实现或持有这些端口，但构造 ActionRuntime、Motion 和 CharacterGraphContext 时必须按最窄接口注入。任何端口不得暴露 ActiveGameplayEffect 列表、Attribute Store、Tag source dictionary、prediction journal 或 Runtime 引用。

Runtime 正式输入输出为：

```text
Input
  GameplayEffectRuntimeDefinition
  GameplayEffectTickContext
  GameplayEffectApplyRequest / RemoveRequest
  GameplayEffectAuthorityInput

Output
  GameplayEffectApplyResult / RemoveResult
  GameplayEffectChangeSet
```

状态 mutation 同步提交，保证同 Tick 后续查询看到新值；Effect、Attribute、Tag 和 Cue 变化只进入 ChangeSet。Adapter 在 CommitFacts 时把同一 ChangeSet 分别交给 Fact、Cue 和 Trace Projector。核心不得直接写 CharacterPipelineFrame、CharacterPipelineOutput、SyncFacts 或 RuntimeDiagnosticsContext，也不得使用全局事件总线、静态 ServiceLocator 或任意回调发现消费者。

## Definition、Spec 与 Active Effect

### GameplayEffectDefinition

Definition 是 ScriptableObject authoring 入口，直接实现 Gameplay Contracts 中的 `IGameplayBehaviorProfile`：

```text
BehaviorId = EffectId
BehaviorKind = Effect
DisplayName
DebugCategory
EffectTags
DurationPolicy
DurationMagnitude
PeriodMagnitude
ExecuteOnApplication
ApplicationRequirements
OngoingRequirements
RemovalRequirements
StackingPolicy
Components
CueBindings
DeclaredSetByCallerParameters
```

Definition 和内联 Component Definition 必须无运行时状态。一个 Effect 可以被多个角色和实例共用。

EffectDefinition 只提供 gameplay identity、tag 和 effect 规则，不保存 ServerAuthoritative prediction、authority、replication、history、packet 或 endpoint policy。当前网络模型必须在 `ServerAuthoritativeCharacterSyncProfile` 中按 Effect BehaviorId 配置完整 Effect policy；未来其他模型使用自己的 Profile/Resolver，不修改 EffectDefinition。

CharacterPipeline 初始化时，Authoring Definition 必须先构建为不可变 `GameplayEffectRuntimeDefinition` 与 `GameplayEffectDefinitionData`。GameplayEffectRuntime 只持有这些不可变运行数据，不持有 CharacterPipelineDefinition、Inspector context、Graph、Timeline 或场景对象。

### GameplayEffectContext

Context 保存一次业务来源：

```text
SourceActorId
TargetActorId
SourceActionInstanceId
PredictionKey
GameplayResultId
SourceLogicTick
ApplicationMode
```

Context 不保存 GameObject、Graph clone、Timeline clip 或 Network Model packet。

### GameplayEffectSpec

Spec 在应用时创建，保存：

- Definition stable identity 与配置 revision。
- Context。
- 已声明 SetByCaller 值。
- Source/Target Tag snapshot。
- 明确要求快照的 Attribute magnitude。
- 已换算成固定 Tick 的 Duration、Period 和首次 Period Tick。
- Stack key。

Spec 创建失败必须返回明确拒绝原因，不能用 0、空字符串或默认 Effect 继续运行。

### ActiveGameplayEffect

Active Effect 保存：

```text
EffectInstanceId / Handle
Spec
StartTick
EndTick
NextPeriodTick
StackCount
Inhibited
LifecycleRevision
Applied Modifier handles
Granted Tag source handle
Prediction journal identity
```

Instant Effect 不进入 Active Container；它执行 Base mutation、Execution 和 Cue 后立即完成。

## Gameplay Tags

Tag Catalog 是角色相关 Tag 的唯一正式定义来源。TagId 使用层级路径，例如：

```text
State.Control.Stunned
State.Defense.Invulnerable
Action.Attack
Cooldown.Attack.Light
```

查询 `State.Control` 必须匹配 `State.Control.Stunned`。Query 正式支持 All、Any 和 None 三组条件，不执行任意字符串 contains。

运行时 Container 按 source handle 计数：

```text
Character base source
ActionInstance source
ActiveGameplayEffect source
```

两个 Effect 同时授予 `State.Control.Stunned` 时，移除其中一个只能减少一个来源，Tag 仍保持有效。ActionInstance 终态只移除自己的 Tag source。

ActionProfile、GameplayBehaviorProfile 和 GameplayEffectDefinition 必须引用同一个 Catalog 的 TagId。旧 `List<string>`、`ActionRuntime.m_Tags` 和 `SetTag()` 删除，不保留字符串重载。

## Gameplay Attributes

首批 Attribute 统一使用 float，避免旧 GenericMath 和多个数值类型导致的配置分支。一个 Attribute 只有一个稳定 AttributeId，不再拆成 `Value-Config/Value-Buff/Mul-Buff` 等字符串属性图。

每个值保存：

```text
BaseValue
CurrentValue
Revision
```

聚合顺序固定为：

```text
value = BaseValue
value += all Additive
value *= all Multiplicative
value = highest-priority Override when present
value = final Clamp
```

最终 Clamp 在 Override 后执行，确保 Health、Stamina 和 MoveSpeed 不越过正式边界。边界可以是常量或另一 Attribute，例如 `Health <= MaxHealth`；依赖必须形成无环图，缺失引用和循环配置直接失败。

Instant Effect 修改 BaseValue；Duration/Infinite Effect 向 Aggregator 添加带 ActiveEffectHandle 来源的 Modifier。移除 Active Effect 时按 handle 精确移除 Modifier，不按数值或名字搜索。

Magnitude 支持：

- Constant。
- Definition 声明过的 SetByCaller。
- Source/Target Attribute Snapshot。
- Target Attribute Live dependency。

首批不支持跨角色 Source Attribute Live dependency；该配置必须被 authoring validator 拒绝。原因是跨角色实时依赖需要生命周期订阅和远端状态一致性，当前战斗需要通过 Spec snapshot 即可完整表达。

旧 ComputedProperty 任意公式图不迁移。业务上常见的 Health、Stamina、Poise、MoveSpeed 使用固定 Aggregator；确实需要伤害公式时进入无状态 Effect Execution，而不是把每个公式拆成隐藏属性节点。

## Effect Components

GameplayEffectDefinition 使用内联、无状态 Component Definition 组合行为：

- `GameplayModifierComponentDefinition`：向 Attribute 添加 Modifier 或执行 Instant Base mutation。
- `GrantedTagsComponentDefinition`：Active 时授予 Tag。
- `TagRequirementsComponentDefinition`：Source/Target 应用条件、持续条件或移除条件。
- `AttributeRequirementsComponentDefinition`：Attribute 比较条件。
- `GameplayEffectExecutionComponentDefinition`：执行伤害、治疗、Poise 等明确计算并输出 Attribute mutation。
- `AdditionalEffectsComponentDefinition`：在 Applied、Period、Removed 或 Overflow 时施加声明过的其他 Effect。
- `GameplayCueBindingComponentDefinition`：把生命周期映射为现有 Presentation cue。

Additional Effect 引用图必须在配置期检测循环。Runtime 不用深度上限 fallback 掩盖循环定义；配置有环直接失败。

## Duration、Period 与 Stacking

DurationPolicy：

- Instant：立即执行，不创建 Active Effect。
- Duration：创建 `[StartTick, EndTick)` Active Effect。
- Infinite：创建无 EndTick Active Effect，直到正式移除。

时间由固定 Logic Tick 表达。Authoring 可以显示秒，但保存或构建 Spec 时必须使用 Gameplay Tick 配置换算为整数 Tick；没有正式 Tick 配置时拒绝创建 Spec。

Period 执行规则：

- `ExecuteOnApplication` 明确决定是否在 StartTick 执行。
- 后续只在 `NextPeriodTick < EndTick` 时执行。
- 同一 Tick 先消费 authoritative input/reconciliation，再执行到期 Period 和 Expire，之后 BTSMTL 读取最终状态。
- 一个 Logic Tick 中 Period、Stack、Remove 和 Expire 使用稳定 insertion sequence 排序，不能依赖 List 遍历偶然顺序。

StackingPolicy：

- Independent：每次应用创建独立实例。
- AggregateBySource：相同 EffectId 与 SourceActorId 共用实例。
- AggregateByTarget：目标上相同 EffectId 共用实例。

定义同时声明 MaxStacks、DurationUpdate、PeriodUpdate 和 OverflowPolicy。DurationUpdate 支持 Keep、Refresh、Extend；PeriodUpdate 支持 Keep、Reset；Overflow 支持 Reject、ReplaceOldest 或 ApplyOverflowEffects。所有策略都产出结构化生命周期结果。

## Application、Removal 与 Inhibition

应用过程固定为：

```text
Resolve Definition
-> Validate Context and declared parameters
-> Capture requested Tags/Attributes
-> Evaluate Application Requirements
-> Resolve Stack Key
-> Create or update Active Effect / execute Instant
-> Apply Modifiers and Granted Tags
-> Emit lifecycle result
```

Ongoing Requirement 失败时 Active Effect 进入 Inhibited：时间继续推进，但 Modifier、Granted Tag、Period 和 WhileActive Cue 停止；条件恢复后按原实例恢复。Removal Requirement 命中时正式移除实例。

移除入口支持精确 Handle、EffectId、SourceActorId 和 Tag Query 组合。Graph 优先使用 Handle；按查询批量移除必须返回实际移除的 handle 列表。

## Prediction and Reconciliation

项目不做全世界 Rollback，但本地预测动作需要立即看到耐力消耗、冷却和无敌。Spec Context 因此可以携带 ActionInstanceId 与 PredictionKey。

Predicted application 为每次 mutation 记录 effect-scoped journal：

```text
created active effect
stack before/after
base attribute before/after
modifier handles
granted tag source
emitted predicted cue identities
```

- Confirm：将 journal 标记为 confirmed，并按 authoritative EffectInstanceId/Revision 对齐。
- Reject：按 journal 精确恢复 Base、Stack、Modifier 和 Tag，并发出 Rejected/Removed lifecycle。
- Correct：先撤销预测 journal，再应用 authoritative typed state facts。

该日志只覆盖来源 ActionInstance 的 GE mutation，不恢复 Graph、Motion、Timeline 或其他角色。Predicted mutation 必须具有稳定来源，缺失 PredictionKey 时不得伪装成可回滚预测。

## CharacterPipeline Integration

CharacterPipeline 构造顺序增加薄 Adapter。Adapter 根据已校验的 CharacterGameplayEffectProfile 构建不可变 runtime definition，再唯一创建 GameplayEffectRuntime。Pipeline、ActionRuntime 和 CharacterGraphContext 都不得取得 Runtime 本体：

```text
CharacterPipeline
  -> CharacterGameplayEffectAdapter
       -> GameplayEffectRuntime
       -> CharacterGameplayEffectInputMapper
       -> CharacterGameplayEffectFactProjector
       -> CharacterGameplayCueProjector
       -> CharacterGameplayEffectTraceProjector

ActionRuntime <- IGameplayTagReader + scoped Action tag source sink
MotionStage <- IGameplayAttributeReader
CharacterGraphContext <- query/command port source
```

Adapter 只翻译以下边界：

```text
Character semantic input -> GameplayEffectAuthorityInput
Action/Graph source context -> GameplayEffectContext
GameplayEffectChangeSet -> Character SyncFacts / GameplayCueFact / Trace
```

Adapter 不包含 Effect switch、伤害公式、Modifier 聚合、Stack/Period/Expire、prediction journal 算法、Action cancellation 或模型 policy。新增 GE 业务规则必须进入通用 Component/Runtime；新增 Character 输出只增加 Projector，不修改 Runtime。

Logic Tick 固定顺序：

```text
ActionRuntime.BeginLogicTick
Frame.Begin
GraphContext.BeginFrame
NetworkReceiveStage.Collect
ActionLifecycleInputStage.Resolve
CharacterGameplayEffectAdapter.BeginLogicTick
  - InputMapper maps Action confirm/reject/correct
  - InputMapper maps incoming GameplayEffectLifecycleFact/GameplayAttributeValueFact by revision
  - GameplayEffectRuntime reconciles authority inputs
  - GameplayEffectRuntime.Advance executes due Period and expiry
InputStage.Update
CharacterBTSMTLPhase.Tick
  - read state
  - synchronously apply/remove self effects
MotionStage.Update
CharacterGameplayEffectAdapter.CommitFacts
  - drain one GameplayEffectChangeSet
  - project Character facts, cue and trace
NetworkSendStage.Collect
PresentationStage.CaptureLogicSample
```

Graph 内 Apply/Remove 通过 `IGameplayEffectCommandSink` 同步更新当前 GameplayEffectRuntime 的 Tag 与 Attribute，因此同一 Tick 后续节点和 Motion 可读取新值。事实、Cue 和 Trace 只写入 ChangeSet，并在 CommitFacts 由 Projector 批量提交；Adapter 不通过回调或全局事件直接通知消费者。

Deactivate/Dispose 必须移除全部 Active Effect、Modifier 和 Tag source，清空 prediction journal，并释放跨 Attribute dependency；不能触发新的 Gameplay 业务输出。

## BTSMTL Boundary

正式节点：

```text
HasGameplayTagValueNode
MatchGameplayTagQueryValueNode
ReadGameplayAttributeValueNode
CanApplyGameplayEffectValueNode
ApplyGameplayEffectNode
RemoveGameplayEffectNode
```

ValueNode 只读 `IGameplayTagReader` 或 `IGameplayAttributeReader`，可进入普通 Graph、ConditionRuleGraph 和 Decision TreeClip。Apply/Remove 只使用 `IGameplayEffectCommandSink`，属于 ActionNode，只允许 RootTree/State body/Timeline Commit 的正常行为图使用。节点不得持有 CharacterGameplayEffectAdapter、GameplayEffectRuntime、Container 或 ActiveEffect instance。

Decision TreeClip 继续保持纯决策；Validator 必须拒绝 Apply/Remove、Attribute mutation 和任何 Effect lifecycle 写操作。Blackboard 可以保存一次判断结果，但不能保存 Attribute、TagContainer 或 ActiveEffect 作为第二份真相。

Graph 对外部目标不直接获取另一个 CharacterGameplayEffectAdapter 或 GameplayEffectRuntime。自施加 Effect 用于 Cost、Cooldown、Invulnerability 等；命中其他角色继续提交 GameplayResult，权威 solver/model 产生目标的 `GameplayEffectLifecycleFact`。

## Action Integration

ActionRuntime 保持事务职责：

- 激活前通过 IGameplayTagReader 查询 BlockTags。
- 新 Action 的 CancelTags 与当前 ActionProfile Tags 继续决定是否能替换当前事务。
- Action 激活后以 ActionInstanceId 作为 source handle 授予 ActionProfile Tags。
- Action 终态移除该 source handle 的全部 Tags。
- ActionRuntime 不创建 EffectSpec、不推进 Duration/Period、不计算 Attribute、不直接处理 Buff。

Effect 授予 `State.Control.Stunned` 后，新 Action 激活可以被阻止；当前 Action 是否中断仍由 StateMachine/Graph 读取 Tag 后提交 `ActionLifecycleTransition.Interrupt`，GE 不直接越权取消动作。

## GameplayResult and Cross-Actor Effects

GameplayResult 表达命中、格挡、破防等权威事件；`GameplayEffectLifecycleFact` 与 `GameplayAttributeValueFact` 表达该事件在目标角色上造成的 Effect/Attribute 变化。两者可以关联同一个 GameplayResultId，但不能互相替代。

```text
Hit confirmed GameplayResult
  -> target receives Apply Damage GameplayEffectLifecycleFact
  -> target CharacterGameplayEffectInputMapper creates AuthorityInput
  -> target GameplayEffectRuntime resolves EffectDefinitionData + SetByCaller Damage
  -> Health changes
  -> optional Stun/Dead Effect follows
```

客户端 Timeline 和 HitWindow 只提出 Result/Window 事实，不能直接修改目标 Health。单机或 LocalLoopback 也必须通过同一语义路由，不建立“本地直接扣血”的快捷路径。

## Gameplay Effect Lifecycle and Attribute Facts

`GameplayEffectSyncDomainOutput` 迁移为两类 typed list：

```text
GameplayEffectLifecycleFact
  BehaviorId / EffectId
  EffectInstanceId
  Operation
  SourceActorId
  SourceActionInstanceId
  PredictionKey
  GameplayResultId
  StartTick / EndTick
  StackCount
  LifecycleRevision
  declared SetByCaller values
  DefinitionRevision

GameplayAttributeValueFact
  AttributeId
  BaseValue
  CurrentValue
  ValueRevision
  SourceTick
  CauseEffectInstanceId
```

Operation 至少包含 Applied、Confirmed、Rejected、StackChanged、Inhibited、Resumed、PeriodExecuted、Removed、Expired 和 Corrected。

固定 Modifier、Tag 和 Cue 配置由 EffectId + Definition revision 解析，不逐包复制。接收端缺失 Definition 或 revision 不一致时必须报告配置错误并拒绝事实，不使用旧 `PayloadDigest` 或默认 Effect 补跑。

旧占位一次性删除并改名：

```text
GameplayStateEffectFact             -> GameplayEffectLifecycleFact
StateEffectSyncDomainInput/Output   -> GameplayEffectSyncDomainInput/Output
GameplayBehaviorKind.State          -> GameplayBehaviorKind.Effect
ServerAuthoritativeStateEffect      -> ServerAuthoritativeGameplayEffect
```

这不是保留旧 State 概念的别名迁移。旧 `StateId`、`PayloadDigest`、State 枚举值和 StateEffect 容器全部删除，不双写、不兼容读取。角色 GE 同步域只接收由 Effect Definition 和 Attribute Definition 定义的角色效果事实。objective ownership、capture、contest 等目标玩法状态不属于角色 GE，继续由 GameplayResult 的 objective result 表达；以后若需要持续目标状态，应新增独立 Objective/Event 合同。

CharacterGameplayEffectFactProjector 负责将 GameplayEffectChangeSet 投影为上述 Character 语义事实；它不解析任何模型 policy。ServerAuthoritativeHybrid Adapter 再按 `ServerAuthoritativeCharacterSyncProfile` 中以 Effect BehaviorId 或 GameplayAttributeValueFact binding 配置的正式策略映射 packet、replication 和 history。GameplayEffectRuntime、CharacterGameplayEffectAdapter、CharacterNetworkReceiveStage 和 CharacterNetworkSendStage 都不引用模型 policy、packet、endpoint 或 history。

## Presentation

Effect lifecycle 只映射到现有 Presentation 事实：

```text
Applied / Resumed -> OnActive
PeriodExecuted / Instant -> Executed
Active and not inhibited -> WhileActive
Removed / Rejected -> Removed
Expired -> Expired
```

Effect Definition 只保存 CueId 和触发点，不保存 VFX/SFX 运行对象。PresentationStage 继续负责播放，本地/复制策略继续属于 Network Model profile。当前以 Action 命名但可表达非 Action 来源的 Cue fact 必须迁移为通用 GameplayCueFact，旧类型直接删除，不保留别名。

## Diagnostics

RuntimeTraceChannel 新增 `GameplayEffect`，统一显示：

- Effect applied/rejected/confirmed/corrected/removed/expired。
- Stack 与 inhibition 变化。
- Period execution。
- Attribute Base/Current 与 revision 变化。
- Tag source count 变化。
- Prediction journal confirm/rollback。

Trace 继续使用现有 RuntimeDiagnosticsTarget、Live State、Capture、Source Map 和 Host Inspector。没有 Live interest/Capture 时不得构造 GameplayEffect payload。GE 不创建专用窗口或第二份历史。

## Authoring

`CharacterPipelineDefinition` 新增唯一 `CharacterGameplayEffectProfile` 引用，内容包括：

```text
GameplayTagCatalog
GameplayAttributeDefinition[]
InitialGameplayAttributeValue[]
InitialGameplayTag[]
GameplayEffectDefinition[]
```

CharacterPipelineDefinition 继续是组合根。EffectDefinition 可以复用到多个 CharacterGameplayEffectProfile，但当前角色使用的 Effect、Attribute、Tag 和 BehaviorId 必须全部由该 Definition 闭包解析。

Authoring Definition 只负责 Unity 序列化与配置校验。创建角色 Runtime 前必须一次性构建不可变 `GameplayEffectRuntimeDefinition`；构建失败则拒绝创建 CharacterPipeline，不生成空 registry、默认 Effect 或运行时资产查找。GameplayEffectRuntime 不回读 ScriptableObject，也不按 asset name、path 或 Addressables key 查找定义。

Graph Data Catalog 在原窗口新增 Gameplay Effect source：

- Tags：拖出 HasTag/MatchQuery 节点。
- Attributes：拖出 ReadAttribute 节点。
- Effects：在允许写操作的 Graph 拖出 ApplyEffect，在只读 Graph 只提供 CanApply/详情。

Inspector 和 Validator 必须报告重复 ID、断裂引用、跨 Catalog Tag、属性依赖环、非法 Tick、未声明 SetByCaller、Additional Effect 循环、非法预测配置和 Effect/BehaviorId 冲突。

## Old Lightweight GE Migration

旧实现只作为语义来源：

| 旧类型 | 新类型 | 处理 |
|---|---|---|
| BuffData | GameplayEffectDefinition | 迁移 duration、period、stack、tags、modules 语义 |
| BuffInfo | GameplayEffectSpec + ActiveGameplayEffect | 拆分应用快照与目标持续状态 |
| BuffHandler list/queues | ActiveGameplayEffectContainer | 保留稳定增删队列思想，删除 MonoBehaviour |
| BuffModuleBase | Effect Component Definition | 改为内联无状态定义和类型化输出 |
| PropertyHandler | GameplayAttributeStore | 删除 MonoBehaviour 和公开 Dictionary |
| BasicProperty | GameplayAttributeValue | 增加可变 Base/Current/Revision |
| Modifier classes | GameplayModifierSpec | 保留运算语义，改为稳定来源 handle |
| ComputedProperty | Effect Execution / explicit bound | 不迁移任意依赖公式图 |
| TagCollection | GameplayTagContainer/Query | 增加 Catalog、层级和来源计数 |
| Update/Coroutine | Gameplay Logic Tick | 彻底删除 |
| Addressables name load | Definition registry identity | 彻底删除 |
| params object[] | typed Context/Spec/Execution result | 彻底删除 |

本项目不把旧目录复制进来再包一层。实现时只在正式新命名下迁移必要算法，旧工程不成为 assembly、asset 或 runtime dependency。

## Decisions and Tradeoffs

### 独立 Gameplay Runtime + 薄 Character Adapter vs Pipeline 直接实现 GE

Pipeline 直接持有 Tag、Attribute 和 Effect Container 文件较少，但 Action、Graph、网络、表现会逐步依赖 Character 内部结构，其他实体也无法复用。独立 GameplayEffectRuntime 只处理通用状态，Character Adapter 只翻译 semantic input/change set，增加少量边界类型，却能让业务规则与项目装配清楚分离。这里选择独立 Runtime 与薄 Adapter。

### 编译期程序集边界 vs 仅靠目录约定

只用 namespace 和目录初期改动少，但编译器不能阻止 Gameplay 代码引用 Character 或网络。新增 `ThirdPersonGameplay.asmdef` 并把通用 Behavior identity contract 移入 Gameplay Contracts 会触及现有 Action、Behavior 和模型引用，却能建立真实单向依赖；角色专用 BTSMTL 节点留在接入侧，避免通用程序集引用 BTSMTL。这里选择编译期边界，不创建独立运行时或第二条 Tick。

### 窄端口 + ChangeSet vs 暴露 Runtime/事件总线

直接暴露 GameplayEffectRuntime 最容易调用，但任何消费者都能越权修改 Container；全局事件总线隐藏时序和所有权。按 Tag read、Attribute read、Effect command、Authority input 拆分端口，并用每 Tick ChangeSet 统一投影，需要更多显式 DTO，却能让 Action、Motion、Graph、网络和调试只拥有必要能力。这里选择窄端口和 ChangeSet。

### 项目内通用 GE 域 vs 复制 UE ASC

项目内通用域让玩家、敌人和 PvE 共用同一 Effect 规则，同时由 CharacterPipeline 保持唯一 Tick 和状态所有权。复制 ASC 会附带 ActorComponent、Ability grant、Prediction Window、Replication container 和 Blueprint 生态，能更接近 UE API，但会与当前 Action/BTSMTL/Network Model 重叠。这里选择前者。

### 迁移旧轻量实现 vs 原样复用

原样复用能快速得到 BuffHandler 和 PropertyHandler，但会引入第二个 MonoBehaviour Tick、GameObject/Addressables 依赖和字符串状态。迁移重构需要重写 ownership 与类型，却能保留有价值的结构并接入固定 Logic Tick。这里选择迁移重构，不建立兼容层。

### 固定 Attribute Aggregator vs 任意 Property 依赖图

任意依赖图能表达复杂公式，但每个属性会拆出大量字符串节点，缺失依赖和环很难调试。固定 Aggregator 覆盖当前动作游戏需要的资源和数值修正，伤害公式进入显式 Execution。这里选择固定 Aggregator。

### Graph 同步应用 vs 帧末命令队列

帧末队列容易避免重入，但同 Tick 后续状态判断看不到刚产生的 Cost、Cooldown 或 Stun。同步应用让玩法反应直接，内部 Active Container 仍用 mutation buffer 防止遍历中修改。这里选择业务状态同步生效、事实和表现帧末提交。

### 类型化 Effect 生命周期事实 vs 复制完整 Active Container

完整容器快照容易做远端覆盖，但包体、配置重复和预测合并成本高。类型化 lifecycle + Attribute revision fact 保留 EffectId/InstanceId/Stack/参数，固定配置由本地 Definition 解析，更符合当前 SyncFacts/Network Model 分层。这里选择类型化 Effect/Attribute 事实，不保留通用 State fact。

### Effect-scoped prediction journal vs 只等服务器

只等服务器最简单，但本地攻击资源消耗、冷却和无敌会产生明显延迟。完整 Rollback 超出项目目标。Effect-scoped journal 只恢复来源动作造成的 GE mutation，复杂度可控并保持手感。这里选择局部 journal。

### Graph 直接修改外部角色 vs 通过 Result/Effect Fact 路由

直接获取目标 CharacterPipeline 对单机简单，却会绕过命中权威、actor route 和网络模型。通过 `GameplayResult -> GameplayEffectLifecycleFact` 多一步事实传递，但本地、Loopback 和未来 Fantasy 使用同一链。这里选择正式路由。

### 内联 Component Definition vs 大量模块资产

独立 ScriptableObject 模块方便跨 Effect 复用，却会产生大量小资产并允许共享对象误存运行时状态。内联 SerializeReference 定义让 Effect 一处可读，复用通过完整 EffectDefinition 和明确 Additional Effect 完成。这里选择内联无状态 Component Definition。

## Risks and Mitigations

- `refactor-gameplay-network-model-boundary` 已完成任务但尚未归档，仍会决定同一批 semantic fact 和 profile 的 current spec。GE 核心设计可以并行推进，但共享文件集成必须基于其归档后的正式命名，不得临时兼容两套类型。
- 新 `ThirdPersonGameplay` 程序集无法引用当前默认程序集中的 Behavior identity contract。实现时必须一次性把通用 enum/interface 迁移到 Gameplay Contracts，并更新 Action、generic Behavior 和 ServerAuthoritative 引用；不得复制一份同名接口或保留 Character 旧合同。
- Adapter 容易演变为第二个业务核心。代码边界必须限制为 input mapping、端口委托、Tick 调度和 change projection；Effect/Attribute/Tag/预测规则不得进入 CharacterGameplayEffectAdapter。
- Attribute live dependency 可能形成环。Definition 构建时必须拓扑校验，Runtime 不尝试自动打断环。
- Prediction stack rollback 容易覆盖 confirmed state。Journal 必须记录具体 revision 和 mutation before/after，只允许撤销自己持有的 revision。
- Additional Effect 可能递归。配置期检测完整引用图，运行时遇到未注册或 revision 不一致直接失败。
- Graph 同步应用可能在 Active Container 遍历中重入。Container 使用稳定 operation sequence 和内部 mutation buffer，当前操作完成后按顺序提交结构变化。
- EffectDefinition 直接成为 Effect behavior 会改变 Behavior registry。CharacterPipelineDefinition 必须对 Action、generic Behavior 和 Effect 做统一 BehaviorId 唯一校验。
- Cue 通用命名迁移会触及 Action 和网络 Adapter。必须一次性改为 GameplayCueFact 并删除旧 ActionCueEvent 类型，不能长期双写。

## Migration Order

1. 归档已经完成的网络模型边界 change，确认 Character semantic input/output 与模型 Adapter 最终命名。
2. 创建 `ThirdPersonGameplay` 程序集，把通用 Behavior identity contract 迁入 Gameplay Contracts 并更新全部正式引用。
3. 在该程序集实现 Tag、Attribute、Effect、GameplayEffectRuntime、窄端口和 ChangeSet，不接 Character 或旧 Buff/Property runtime。
4. 实现 CharacterGameplayEffectAdapter、InputMapper 和 Fact/Cue/Trace Projector，再由 CharacterPipeline 接入固定 Tick。
5. 迁移 Action/Behavior 标签、注入窄 reader/source sink，并删除 ActionRuntime 私有标签。
6. 通过 CharacterGraphContext 端口接入 BTSMTL 节点和 Validator，不暴露 Adapter/Runtime。
7. 删除旧 GameplayState/StateEffect 合同，将 Gameplay Effect lifecycle/Attribute facts 与 Cue fact 收口到模型无关类型，再更新 ServerAuthoritative Adapter/Profile。
8. 接入统一 Diagnostics 和 Graph Data Catalog。
9. 创建首批正式 Attribute/Effect 资产并迁移 Corin/Sandbox。
10. 删除所有新链路中已经无用的旧字段、旧命名和临时资产引用。

每一步只能向最终链路前进；不允许为提前运行而保留 ActionRuntime string tags、BuffHandler、Coroutine duration、旧 GameplayState/StateEffect 类型或旧新事实双写。

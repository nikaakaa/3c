# Change: 在项目内新增通用 Gameplay Effect 内核并通过适配层接入 CharacterPipeline

## Why

当前角色管线已经具备项目专用的 GA 等价层：`ActionProfile + ActionInstance + ActionRuntime + BTSMTL` 负责动作事务和行为编排；网络边界中则只有旧 `GameplayStateEffectFact + StateEffectSyncDomain` 占位。这个占位仅保存 `BehaviorId / StateId / EffectInstanceId / PayloadDigest` 并由收发层搬运，没有正式生产者、玩法消费者、属性模型或效果生命周期，不能理解为已经存在的 Gameplay State 或 GE Runtime。工程仍缺少角色属性、统一 Gameplay Tag、持续效果实例、周期、叠层、资源消耗、冷却、Buff、Debuff 和状态来源管理，导致伤害、耐力、硬直、无敌和控制状态无法通过一条正式链路表达。

旧 `KaaKaaFramework/Buff + Property` 已经验证了 Definition/Instance 分离、模块化 Buff、增删队列和 Add/Multiply/Clamp/Override 属性聚合等方向，可以作为实现输入；它当前依赖 `MonoBehaviour.Update`、`GameObject`、Addressables、Coroutine、字符串属性依赖和无类型回调，且 Buff 与 Property 没有真正接通，不能作为运行时依赖直接接入本项目。

本变更需要把其中有效设计迁移进 `3C_Client` 正式模块，并由现有 `CharacterPipeline`、BTSMTL、Action、SyncFacts、Presentation 和 Diagnostics 共同使用。系统必须只有一条角色状态真相和一条逻辑 Tick 链路，不能创建独立 GAS 包、第二个 Update 或旧新兼容路径。

## What Changes

- 在本工程 `Assets/GameScripts/Main/Runtime/Gameplay` 内新增 `ThirdPersonGameplay` 程序集，提供项目内通用的 Behavior identity contract、Gameplay Tag、Gameplay Attribute、Gameplay Effect 和 `GameplayEffectRuntime`；该程序集只建立编译依赖边界，不是外置包或独立运行系统，不引用 Character、BTSMTL、网络模型或表现模块，也不拥有 Unity 生命周期。
- 将当前位于 Character 命名空间的通用 `GameplayBehaviorKind`、`IGameplayBehaviorProfile` 身份合同迁移到 Gameplay Contracts；Action、generic Behavior、Effect 与模型 Profile 统一依赖该合同，Gameplay 核心不得为实现 Effect behavior 身份反向引用 Character。
- 在 `Runtime/Character/Pipeline/GameplayEffect` 新增薄 `CharacterGameplayEffectAdapter`，由 `CharacterPipeline` 唯一持有。Adapter 只把 Character 语义输入翻译为通用 GE 命令，把 `GameplayEffectChangeSet` 投影为 Character facts、cue 和 trace，不实现 Tag、Attribute、Effect、预测或网络业务规则。
- 使用 `IGameplayTagReader`、`IGameplayAttributeReader`、`IGameplayEffectCommandSink` 和 authority input 等窄端口接入 Action、Motion、BTSMTL 与网络语义输入；任何消费者不得取得 `GameplayEffectRuntime`、Active Effect Container 或 Adapter 的完整可写引用。
- 建立 `GameplayEffectDefinition -> GameplayEffectSpec -> ActiveGameplayEffect` 三层模型，分别表达作者定义、一次应用时锁定的上下文与参数、目标角色上的持续实例。
- 支持 Instant、Duration、Infinite、周期执行、应用时执行、按来源/目标聚合叠层、最大层数、持续时间刷新/延长、周期重置和溢出处理。
- 支持 Constant、声明式 SetByCaller 和属性捕获 magnitude；SetByCaller 参数必须由 Effect Definition 声明，缺失参数直接拒绝应用。
- 建立 float Gameplay Attribute 的 Base/Current 模型，固定聚合顺序为 `Base -> Additive -> Multiplicative -> Override -> final Clamp`；Instant Effect 修改 Base，Duration/Infinite Effect 通过有来源的 Modifier 改变 Current。
- 建立正式 Gameplay Tag Catalog、层级匹配、All/Any/None Query 和来源计数；Character 初始 Tag、ActionInstance 与 Active Effect 共用同一 Tag 状态。
- 删除 `ActionRuntime.m_Tags`、`SetTag()` 和字符串标签真相；`ActionProfile`、`GameplayBehaviorProfile`、Effect Definition 全部迁移到正式 TagId，并由 ActionRuntime 只读查询统一状态。
- 让 Effect Definition 直接提供 `GameplayBehaviorKind.Effect` 的稳定 BehaviorId，禁止同一 Effect 再配置一份重复 `GameplayBehaviorProfile`；EffectDefinition 只提供 gameplay identity，ServerAuthoritative prediction、authority、replication 和 history 策略继续唯一归属模型 Profile。
- 在 BTSMTL 中增加 Tag/Attribute 只读 ValueNode，以及 Apply/Remove Effect ActionNode；Decision TreeClip 和 ConditionRuleGraph 只能读取，不能修改角色状态。
- 让自施加的资源消耗、冷却、无敌等效果同步生效，使同一 Logic Tick 后续节点和 Motion 能读取新状态；外部目标伤害继续由 `GameplayResult -> GameplayEffectLifecycleFact` 语义链进入目标 CharacterPipeline，不允许 Graph 直接查找并修改另一角色。
- 用 Effect 来源 ActionInstance/PredictionKey 建立局部预测日志；Action Confirm 保留预测状态，Reject/Correct 按日志撤销或替换预测的 Instant mutation、Active Effect、Tag 和 Modifier，不实现全世界 Rollback。
- 删除旧 `GameplayStateEffectFact`，新增类型化 `GameplayEffectLifecycleFact` 和 `GameplayAttributeValueFact`；`CharacterGameplayEffectFactProjector` 只负责从 GE ChangeSet 产生模型无关事实，具体 ServerAuthoritative packet、history 和 replication 继续由模型 Adapter 与模型 Profile 决定。
- 将旧 `StateEffectSyncDomainInput/Output` 一次性改名并收口为 `GameplayEffectSyncDomainInput/Output`，将 `GameplayBehaviorKind.State` 一次性改为 `GameplayBehaviorKind.Effect`；不保留旧枚举值、旧类型别名、双写或兼容解析。
- 将 objective ownership、capture、contest 等目标玩法状态移出角色 GE 同步域；它们继续归属 `GameplayResultSyncDomain` 的 objective result，若以后需要持续目标状态则单独定义 Objective/Event 合同，不复用角色 Effect identity。
- 将 Effect 生命周期映射到现有 Presentation SyncDomain，不创建第二套 Cue Manager；将运行状态写入统一 Runtime Diagnostics 的 `GameplayEffect` channel，不创建独立调试窗口。
- 扩展现有 Graph Data Catalog，以当前 CharacterPipelineDefinition 为上下文只读展示正式 Tag、Attribute 和 Effect，并按 Graph 能力创建对应节点。
- 迁移旧轻量实现的有效结构与计算语义，不引用 `D:/Unity_Project/KaaKaaFrameWork`，不保留 `Buff*`、`PropertyHandler`、Addressables 名称加载、Coroutine 时钟或兼容适配器。

## Scope

本变更完成后必须形成以下可运行闭环：

```text
CharacterPipelineDefinition
  -> CharacterGameplayEffectProfile / Tag / Attribute / Effect Registry
  -> 构建不可变 GameplayEffectRuntimeDefinition
  -> CharacterPipeline 创建 CharacterGameplayEffectAdapter
  -> Adapter 唯一持有 GameplayEffectRuntime
  -> CharacterGameplayEffectInputMapper 转换 incoming semantic inputs
  -> GameplayEffectRuntime 固定 Logic Tick 处理确认、撤销、周期和过期
  -> BTSMTL 只通过 Query/Command 端口查询或施加 Effect
  -> Action/Motion/StateMachine 读取统一 Tag 与 Attribute
  -> GameplayEffectChangeSet
  -> CharacterGameplayEffectFact/Cue/Trace Projector
  -> GameplayEffectLifecycleFact / GameplayAttributeValueFact + Presentation cue + Diagnostics
  -> Network Model Adapter 或本地消费者
```

首批正式业务覆盖 Health、Stamina、Poise、MoveSpeed、攻击资源消耗、攻击冷却、短暂无敌、Stun、Damage 和 Heal。它们必须全部使用同一 GE 链路，不允许某些资源继续走 Blackboard、Coroutine 或专用字段。

## Non-Goals

- 不实现 UE `AbilitySystemComponent`、`GameplayAbility`、Ability grant、AbilityTask 或 Blueprint 反射生态，不创建脱离本工程的 GAS/GE 包。
- 不替换当前 `ActionProfile + ActionInstance + BTSMTL` 项目专用 GA 等价层。
- 不让 GE tick Graph、控制 StateMachine、播放 Timeline、直接修改 Transform 或直接裁决命中。
- 不实现装备、背包、技能树、职业、等级曲线、随机 Proc、完整 MMO 状态复制或完整世界 Rollback。
- 不复制旧框架的 CommandMediator、InterceptorPipeline 或 ConflictArbiter；免疫进入应用条件，数值修正进入 Execution，反射进入权威 GameplayResult 链。
- 不提供任意泛型属性和运行时公式图；首批 Gameplay Attribute 使用 float 与显式边界。
- 不允许 Graph 直接定位并修改另一个 CharacterPipeline；跨角色结果通过正式 `GameplayResult -> GameplayEffectLifecycleFact` 路由。
- 不新增测试或人工验证任务，不运行 Unity batchmode。

## Current Spec Comparison

- `character-network-sync-domain-contract` 当前仍使用旧 `StateEffectSyncDomain`，并把 Buff、Debuff、Stun、Resource、Cooldown 与 objective state 混在同一占位。本变更将角色效果收口为 `GameplayEffectSyncDomain` 的类型化 Effect/Attribute 事实，并明确删除 objective state 这类非角色 GE 语义。
- `refactor-gameplay-network-model-boundary` 已完成任务但尚未归档，本变更以其归档后的 current spec 和最终代码命名为实施前置条件。GE 不引用 ServerAuthoritative 类型；共享的 CharacterPipeline、Semantic Fact、ActionProfile 和 BehaviorProfile 集成只能基于归档后的单一路径收口。
- `gameplay-behavior-policy-model` 当前仍规定 `GameplayBehaviorKind.State -> StateEffectSyncDomain`。本变更将它改为 `GameplayBehaviorKind.Effect -> GameplayEffectSyncDomain`，并让 GameplayEffectDefinition 自身成为 Effect behavior 身份来源，不再额外复制 GameplayBehaviorProfile。
- `character-syncfact-behavior-binding` 当前仍用 StateEffect 描述非事务事实绑定。本变更将相关场景和调试口径改为 Gameplay Effect lifecycle fact，并继续要求每条 Effect fact 使用自己的 BehaviorId，不恢复固定 `StateEffectBehavior` 槽位。
- `refactor-gameplay-network-model-boundary` 已规定 ServerAuthoritative 完整网络策略只能位于 `ServerAuthoritativeCharacterSyncProfile`。本变更只让 EffectDefinition 提供 BehaviorId/BehaviorKind；EffectDefinition、GE Runtime、Character Adapter 和 NetworkSendStage 都不得保存或解析模型策略。
- `character-action-activation-flow` 要求 ActionRuntime 只负责动作事务。本变更保持该职责，只把私有 Tag 存储迁移为统一 Tag 查询；Effect 不直接取消 Action，StateMachine/Graph 仍提交正式 ActionLifecycleTransition。
- `character-action-instance-runtime` 已删除旧 Ability 执行单元。本变更不恢复 Ability、AbilityTree 或 ASC，GE 只提供状态和属性能力。
- `character-pipeline-blackboard` 已区分 Blackboard variable 与 SyncFact。本变更明确 Attribute、Tag 和 Active Effect 是角色状态真相，不进入 Blackboard；Graph 通过专用 ValueNode 读取。
- `character-pipeline-runtime` 已固定 CharacterPipeline 和 GameplayTickSystem 的 Tick 权威。本变更只在同一 Logic Tick 中加入角色状态前置结算和事实提交，不增加 Unity Update。
- `btsmtl-runtime-diagnostics` 当前固定六类 channel。本变更把它扩展为包含 `GameplayEffect` 的七类 channel，这是明确的 current spec 修改。
- `btsmtl-graph-data-catalog-authoring` 当前只有 Input 与 Blackboard 等来源。本变更新增 Gameplay Effect 只读来源，不建立第二个目录窗口。
- active `add-local-two-client-gameplay-network-closure` 与本变更都会触及旧 StateEffect payload、角色路由和模型 Adapter。两者不能基于不同事实合同同时 apply；后应用的 change 必须先按已经落地的正式事实类型更新文档和任务。
- `openspec/project.md` 已明确 Character facts 与模型 Adapter/Profile 分离，但尚未描述独立 Gameplay Effect 程序集、Character 适配层和第七个 diagnostics channel；本变更实施完成时必须同步这些正式口径。

## Impact

- 新能力：`gameplay-tag-runtime`、`gameplay-attribute-runtime`、`gameplay-effect-runtime`、`character-gameplay-effect-integration`、`character-gameplay-effect-authoring`。
- 修改规范：`character-action-activation-flow`、`character-pipeline-runtime`、`character-pipeline-blackboard`、`character-network-sync-domain-contract`、`character-syncfact-behavior-binding`、`gameplay-behavior-policy-model`、`btsmtl-graph-data-catalog-authoring`、`btsmtl-runtime-diagnostics`。
- 运行时代码：独立 `Runtime/Gameplay` 程序集与 Contracts/Effect runtime、Character Action/Behavior、Character Pipeline GameplayEffect Adapter/InputMapper/Projectors、Graph/Logic/Network/Presentation/Diagnostics。
- Editor/Agent：CharacterPipelineDefinition Inspector、Action/Behavior Inspector、Graph Data Catalog、Graph Validator、Agent snapshot/emitter/validator。
- 网络模型：Character 语义事实与 ServerAuthoritativeHybrid Adapter/Packet/Profile 的 GameplayEffect 映射；GE 核心不引用模型模块。
- 资产：Corin CharacterPipelineDefinition、CharacterGameplayEffectProfile、ActionProfile、GameplayBehaviorProfile、GameplayEffectDefinition、首批 Attribute/Effect 配置和 Sandbox 装配。
- 参考来源：`D:/Unity_Project/KaaKaaFrameWork/Assets/Scripts/KaaKaaFramework/Buff` 与 `Property` 只作为迁移参考，不成为项目依赖。

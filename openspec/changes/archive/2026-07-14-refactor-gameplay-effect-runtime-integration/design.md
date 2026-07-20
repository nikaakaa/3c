# Design

## Context

现行架构已经有正确的外部依赖方向：`ThirdPersonGameplay` 不引用 Character 和网络；每个 `CharacterPipeline` 持有一个 `CharacterGameplayEffectAdapter`；Network Model 由 Session 统一装配。但当前实现停在中间态：

- `GameplayEffectRuntime` 集中了命令入口、Spec 构建、Magnitude、应用、移除、叠层、溢出、周期、到期、抑制、Component Hook、预测日志、Confirm/Reject/Correct 和 ChangeSet。
- `CharacterGameplayEffectAdapter.BeginLogicTick` 只传空 authority input。
- `CharacterGameplayEffectAdapter.CommitFacts` 只把 ChangeSet 存入 `LastChangeSet`，没有正式消费者。
- `ActionRuntime` 已经开始使用统一 Tag ports，但 Graph 尚未获得 Attribute Reader 和 Effect Command Sink。
- Character 与 `ServerAuthoritativeHybrid` 仍然生产和消费旧 StateEffect、ActionCue 类型。
- current spec 的部分 Purpose 和早期 Requirement 仍保留 State/StateEffect/ActionCue 旧词；两个未归档 active change 也保留旧 delta。

本变更不是增加另一套 GE，而是把已经归档的 GE 目标收敛成唯一可运行链路。

## Goals

- 让 `GameplayEffectRuntime` 成为职责稳定的公开门面，而不是规则大类。
- 保持 GE 的 Tag、Attribute、Effect、预测和 ChangeSet 状态只有一份。
- 完成 Character 对 GE 的输入、命令、查询和输出桥接。
- 让单机与联网复用同一 Character 事实链；是否联网只取决于是否有 model session 消费事实。
- 让 Effect 的网络策略按 EffectId/BehaviorId 在当前 Network Model Profile 中解析。
- 一次性删除旧 StateEffect 和 ActionCue 路径。

## Non-Goals

- 不把内部服务做成可热插拔策略框架。
- 不让作者为每个 Effect 选择 Network Model。
- 不让 CharacterPipeline 解析 ServerAuthoritative policy。
- 不把 Attribute Store、ActiveEffect Container 或 PredictionJournal 暴露给 Graph。
- 不通过全局事件、反射或 `params object[]` 连接模块。

## Decision 1: Runtime 是唯一门面，规则拆为内部协作者

`GameplayEffectRuntime` 继续实现现有窄合同，并唯一持有运行状态。内部按业务职责拆为：

```text
GameplayEffectRuntime
├─ GameplayEffectSpecFactory
├─ GameplayEffectApplicationService
├─ GameplayEffectLifecycleScheduler
├─ GameplayEffectComponentExecutor
├─ GameplayEffectPredictionReconciler
└─ GameplayEffectChangeRecorder
```

- `GameplayEffectSpecFactory`：Definition lookup、请求校验、SetByCaller、Tag/Attribute snapshot、Magnitude 与秒到 Tick 转换。
- `GameplayEffectApplicationService`：固定应用事务、Instant/Active 分流、StackKey、叠层、溢出、移除和 mutation commit。
- `GameplayEffectLifecycleScheduler`：每 Tick 的 period、expiry、ongoing requirement、inhibit/resume。
- `GameplayEffectComponentExecutor`：执行 Modifier、GrantedTag、Requirement、Execution、AdditionalEffect 和 Cue 的类型化组件操作。
- `GameplayEffectPredictionReconciler`：PredictionJournal、Confirm、Reject、Correct 与 revision 冲突。
- `GameplayEffectChangeRecorder`：收集本 Tick 唯一 Effect、Attribute、Tag 和 Cue 变化并 drain。

拆分依据是业务职责和数据所有权，不规定机械行数。内部服务不得拥有第二份容器，不得直接对外暴露，也不得各自推进 Tick。

### Tradeoff

保留单一 Runtime 门面可以让 Character 只面对少量稳定接口；拆分内部职责可以独立修改叠层、调度或预测，而不反复修改总控类。代价是内部协作者数量增加，因此不引入通用 DI 容器，每个协作者只由 Runtime 显式构造。

## Decision 2: Component 产出类型化操作，由同一事务提交

Component Executor 不直接寻找 Character、网络或表现对象。组件只读取执行上下文并产出类型化 mutation、tag、additional effect 和 cue 操作。ApplicationService 在同一个 `GameplayEffectMutationTransaction` 中校验和提交这些操作。

Additional Effect 继续在当前事务中按 Definition 闭包执行，但通过事务内命令队列回到 ApplicationService，不让 Component 反向持有 Runtime，从而避免 Runtime、ApplicationService 和 ComponentExecutor 的循环依赖。

### Tradeoff

类型化操作比直接回调多一层数据结构，但能够保证失败时不留下部分 Attribute、Tag、ActiveEffect 或 Cue，也让预测日志和 ChangeSet 从同一提交结果生成。

## Decision 3: Character Adapter 只编排 Mapper、Runtime 和 Projector

目标 Adapter 调用顺序固定为：

```text
BeginLogicTick(frame)
  CharacterGameplayEffectInputMapper.Map(frame.NetworkInput.GameplayEffect)
  GameplayEffectRuntime.BeginLogicTick(tickContext, authorityInputs)

CommitFacts(frame)
  changeSet = GameplayEffectRuntime.DrainChangeSet()
  CharacterGameplayEffectFactProjector.Project(changeSet, frame.Output.SyncFacts.GameplayEffect)
  CharacterGameplayCueProjector.Project(changeSet, frame.Output.SyncFacts.Presentation)
  CharacterGameplayEffectTraceProjector.Project(changeSet, diagnostics)
```

删除 `LastChangeSet`。ChangeSet 在 Commit 中只 drain 一次，并在同一调用栈内交给三个只读 Projector。Projector 不保存 GE 状态，也不反向修改 Runtime。

### Tradeoff

直接让 Runtime 发布 C# event 会减少显式参数，但会隐藏消费顺序、增加生命周期泄漏风险，并使单 Tick 的唯一输出难以证明。显式 Projector 更啰嗦，但数据来源和执行时机清楚。

## Decision 4: Graph 只获得能力端口

`CharacterGraphContext` 获得不可变 `CharacterGameplayEffectGraphPorts`：

```text
TagReader
AttributeReader
EffectCommandSink
```

Condition、Decision 和 Value 节点只使用 Reader；Apply/Remove 节点只能使用 CommandSink。Graph 不获得 AuthorityInputSink、Adapter、Runtime、ActiveEffect 集合或 PredictionJournal。

ActionRuntime 继续通过 TagReader 和 scoped TagSourceSink 注册 ActionInstance tags。Effect 不能直接结束 Action；Graph 或 lifecycle coordinator 根据 Tag/Attribute 查询提交正式 ActionLifecycleTransition。

## Decision 5: Network Model 在 Session 选择，Effect policy 在模型 Profile 解析

网络分为三层：

```text
Gameplay Effect
  只产生和协调 gameplay state

Character semantic facts
  GameplayEffectLifecycleFact
  GameplayAttributeValueFact
  GameplayCueFact

Network Model + Endpoint
  决定 authority、prediction、replication、history 和 packet
```

Session 继续唯一选择 `GameplayNetworkModelDefinition`。EffectDefinition 不选择模型，只提供 EffectId/BehaviorId。当前 `ServerAuthoritativeHybrid` 对每条 LifecycleFact 使用 BehaviorId 查询 `ServerAuthoritativeCharacterSyncProfile`，再决定过滤、history 与 packet。AttributeValueFact 如果由 Effect 引起，必须携带可解析的 cause EffectId/BehaviorId；无 Effect 来源的权威 Attribute correction 必须使用模型 Profile 中显式 fact binding，不允许隐藏默认策略。

### Tradeoff

按角色或 Effect 选择 Network Model 看起来更灵活，但同一 Session 会出现互不兼容的确认、历史和副作用提交规则。Session 级唯一模型保证整场业务语义一致；逐 Behavior policy 仍然允许不同 Effect 使用 LocalOnly、ServerConfirmed 或 ClientPredicted 等不同策略。

## Decision 6: 新旧合同一次替换

正式 Character 合同固定为：

- `GameplayEffectLifecycleFact`
- `GameplayAttributeValueFact`
- `GameplayEffectSyncDomainInput`
- `GameplayEffectSyncDomainOutput`
- `GameplayCueFact`

正式 ServerAuthoritative 映射使用 GameplayEffect 和 GameplayCue 命名，不再出现 StateEffect 或 ActionCue。迁移提交必须同步更新 producer、frame bucket、receive/send stage、model adapter、packet、payload、resolver、history、debug、Inspector 和资产引用，然后删除旧类型。不存在双写阶段。

## Data Flow

### 本地自身消耗

```text
BTSMTL ApplyEffect node
→ GraphPorts.EffectCommandSink.Apply
→ GameplayEffectRuntime
→ Attribute/Tag 立即可读
→ Tick Commit ChangeSet
→ Character facts/cue/trace
→ 当前 Network Model 可选消费
```

### 收到权威效果

```text
ServerAuthoritativeHybrid packet
→ model-owned Character adapter
→ GameplayEffectLifecycleFact / GameplayAttributeValueFact
→ CharacterNetworkReceiveStage
→ CharacterGameplayEffectInputMapper
→ GameplayEffectAuthorityInput
→ GameplayEffectPredictionReconciler
```

### 跨角色伤害

```text
Source Character GameplayResult
→ 正式 result routing / Network Model
→ Target Character NetworkReceive
→ InputMapper 构造 Apply authority input
→ Target 自己的 GameplayEffectRuntime
```

来源角色不得直接调用目标角色 Runtime。

## Migration Order

1. 固定现有输入输出和旧引用清单，并修正未归档 active change 的过期 delta。
2. 在不改变外部合同的前提下拆分 Runtime 内部职责。
3. 建立 Character graph ports、InputMapper 和三个 Projector。
4. 将 Pipeline Begin/Commit 改为显式 frame 输入输出，删除 `LastChangeSet`。
5. 一次替换 Character StateEffect/ActionCue 合同和所有生产消费位置。
6. 一次替换 ServerAuthoritative packet、payload、resolver、profile、history 和 debug。
7. 更新正式资源、Inspector、diagnostics 和 current project 口径。
8. 扫描旧符号、编译并严格校验 OpenSpec。

## Active Change Conflicts

- `refactor-animation-presentation-authoring-boundary` 的 delta 仍把 `ActionCueEvent` 列为正式 gameplay fact，必须改为 `GameplayCueFact` 后才能归档。
- `refactor-network-correction-policy-boundaries` 的 delta 仍引用 `StateEffectBehavior` 和 StateEffect 输出，必须改为按 Effect BehaviorId 解析或显式 GameplayEffect fact binding 后才能归档。
- `character-network-sync-domain-contract`、`character-pipeline-runtime`、`character-gameplay-pipeline-closure` 和 `gameplay-behavior-policy-model` 的部分旧 Purpose/Requirement 与同文件后段正式 GE 要求矛盾，必须随本变更收口。

## Risks

- Runtime 拆分时可能改变应用顺序。必须保留 current spec 的固定事务顺序和同 Tick 可见性。
- Cue 统一会同时影响 Timeline、GE、Presentation 和 Network Model，不能只改类型定义。
- Prediction correction 会修改 EffectInstanceId 和 revision 映射，容器索引与 journal 必须在同一事务更新。
- 现有角色资产缺少 GE Profile 或 Effect policy 时会直接配置失败；这是正式配置缺失，不提供空配置 fallback。

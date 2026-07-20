# 实施清单

## GameplayEffectRuntime 当前职责

- 公开入口：构造、`BeginLogicTick`、`DrainChangeSet`、Tag Reader、Tag Source Sink、Attribute Reader、`CanApply`、`Apply`、`Remove`、`Reconcile`、`Dispose`。
- Spec 构建：Definition lookup、Context 和 SetByCaller 校验、Source/Target Tag 与 Attribute snapshot、Magnitude、duration/period Tick、StackKey。
- 应用事务：Instant、新 ActiveEffect、叠层、溢出、移除和拒绝结果。
- 生命周期：period、expiry、ongoing requirement、inhibit、resume 和 Component Hook。
- 预测协调：journal、Confirm、Reject、Correct、Attribute revision 冲突和 authoritative lifecycle。
- 输出：lifecycle、attribute、tag、cue change 收集和 ChangeSet drain。

## 当前唯一状态所有者

- `GameplayEffectRuntime` 直接持有 `GameplayTagContainer`、`GameplayAttributeStore`、`ActiveGameplayEffectContainer`、`GameplayEffectPredictionJournal`、lifecycle revision 表、当前 ChangeSet、当前 Tick 和三个稳定序列计数。
- `GameplayEffectRuntimeDefinition` 是不可变配置输入，不保存角色运行状态。
- Character、Graph、ActionRuntime 和 Network Model 均不得新增第二份 GE 状态。

## CharacterPipeline 当前 GE 生命周期

```text
构造 RuntimeDefinition
→ 创建 CharacterGameplayEffectAdapter
→ 用 Adapter TagReader/TagSourceSink 创建 ActionRuntime
→ Pipeline Activate 调用 Adapter.Activate
→ 每 Tick NetworkReceive/ActionLifecycle 后调用 Adapter.BeginLogicTick
→ Motion 后调用 Adapter.CommitFacts
→ Pipeline Deactivate 调用 Adapter.Deactivate
→ Pipeline Dispose 调用 Adapter.Dispose
```

## 当前桥接端口

- `CharacterGameplayEffectAdapter` 只实现 `IGameplayTagReader` 和 `IGameplayTagSourceSink`。
- `ActionRuntime` 已通过这两个端口注册、查询和移除 ActionInstance tags。
- `CharacterGraphContext` 尚未获得 GE Tag Reader、Attribute Reader 或 Effect Command Sink。
- Motion 和 Presentation 尚未获得 GE 窄 Reader。
- `LastChangeSet` 只在 Adapter 内写入和清空，没有外部消费者。

## 旧 Character 事实位置

- `GameplayStateEffectFact`：Character network semantic facts、receive stage、input bucket、output bucket、send stage、ServerAuthoritative adapter。
- `StateEffectSyncDomainInput/Output`：Character network input、pipeline output、receive/send stage。
- `ActionCueEvent`：Action output contracts、ActionRuntime、Graph node/context、Timeline scheduler、pipeline diagnostics、presentation input/output、receive stage、ServerAuthoritative adapter、Agent validator。

## 旧 ServerAuthoritative 位置

- `ServerAuthoritativeDomain.StateEffect`
- `ServerAuthoritativeFactKind.StateEffect`
- `ServerAuthoritativePacketKind.StateEffect`
- `ServerAuthoritativeStateEffect`
- StateEffect packet factory、resolver、profile coverage、adapter collect/push、history/debug 映射

## 正式完成链路

```text
Character semantic input
→ CharacterGameplayEffectInputMapper
→ GameplayEffectAuthorityInput
→ GameplayEffectRuntime
→ 单一 GameplayEffectChangeSet
→ Fact/Cue/Trace Projector
→ Character SyncFacts/Presentation/Diagnostics
→ 当前 Session 的 Network Model 可选消费
```

旧 StateEffect 和 ActionCue 路径必须在同一次迁移中删除，不保留兼容入口。

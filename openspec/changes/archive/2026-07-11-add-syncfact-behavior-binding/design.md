# Design: SyncFact Behavior Binding

## 核心判断

“任意显式标记”不应该落在任意 Graph 节点上，而应该落在进入网络边界的 SyncFact 上。

```text
不是：Node -> NetworkPolicy
而是：SyncFact -> BehaviorId -> BehaviorProfile -> EffectivePolicy
```

这样 Graph/Timeline 仍然负责业务编排，网络层只理解事实和策略。

## 当前中间态

当前代码里非事务 policy 路径是：

```text
fact type
  -> CharacterPipelineDefinition 固定槽位
  -> GameplayBehaviorProfile
  -> BehaviorNetworkPolicyResolver
```

例如：

```text
ClientCommandFrame      -> ClientCommandBehavior
StateEffect             -> StateEffectBehavior
MotionCorrectionAck     -> MotionCorrectionAckBehavior
```

这比 Adapter 硬编码好，但仍不是完整 B 方案。完整方案应是：

```text
具体 fact
  -> fact.BehaviorId
  -> BehaviorRegistry
  -> BehaviorNetworkPolicyResolver
```

## 数据边界

### Transaction facts

Transaction facts 不需要新增平行 BehaviorId 字段：

- `ActionActivationOutput.ActionId`
- `ActionLifecycleTransition.ActionInstanceId`
- `ActionWindowSample.ActionInstanceId`
- `ActionMotionSample.ActionInstanceId`
- `ActionCueEvent.ActionInstanceId`
- `GameplayResultEvent.ActionInstanceId`

这些已经能通过 `ActionRuntime` 的 transaction policy source 找到 ActionProfile。ActionProfile 的 `ActionId` 就是 Transaction BehaviorId。

### Non-transaction facts

非事务 fact 需要显式 BehaviorId：

- `ClientCommand.BehaviorId`
- `GameplayStateEffectEvent.BehaviorId`
- 非 action 来源的 `GameplayResultEvent.BehaviorId`
- 非 action 来源的 cue/event fact 的 `BehaviorId`
- correction ack 的 `BehaviorId` 或等价包装 fact

### Binding source

有些 fact 来自输入或系统阶段，没有直接 Graph 节点来源。它们不能靠手填字符串，也不能靠 hidden fallback。需要正式配置：

```text
CharacterPipelineDefinition
  -> SyncFactBehaviorBindings
      ClientCommandFrame -> Movement.Locomotion.Move
      MotionCorrectionAck -> Movement.Correction.Ack
      DefaultStateEffect(optional if no explicit State behavior)
```

这里的 binding 是正式配置，不是 fallback。实现阶段如果选择保留 default binding，必须命名为 `SyncFactBehaviorBinding` 之类的通用表，不能保留三个特化字段。

## Adapter 解析规则

```text
ActionActivation / Lifecycle / Window / ActionMotion:
  ActionId 或 ActionInstanceId -> ActionProfile -> Transaction policy

ClientCommand / StateEffect / Event / non-action GameplayResult:
  BehaviorId -> GameplayBehaviorProfile -> Stream/State/Event policy

缺失 BehaviorId 或 profile:
  Record Missing policy
  不发包
```

## UI 和作者心智

作者不应该在每个节点配置完整网络策略。

作者应该看到：

- `CharacterPipelineDefinition` 的 Behavior registry。
- `SyncFactBehaviorBindings` 表，用于输入帧、correction ack 等系统事实。
- 具体 Graph/Timeline 输出如果需要网络策略差异，只选择 BehaviorProfile 或 BehaviorId。
- Runtime Debug 显示 fact kind、BehaviorId、BehaviorKind、resolved policy、发送/过滤原因。

## 为什么不是大一统 SyncFact 类

当前 domain output 已经分好了 Motion、Action、GameplayResult、StateEffect、Presentation。第一阶段只需要在现有 typed fact 上增加 BehaviorId，不需要创建一个 `SyncFact` union 重新包所有输出。

大一统类会增加迁移成本，也会让现有 adapter、debug、packet mapping 全部改动。当前目标是把策略绑定点收口，不是重写 output 模型。

## 完成后的形态

```text
CharacterPipelineDefinition
  -> BehaviorProfiles
  -> SyncFactBehaviorBindings
  -> ActionProfiles(Transaction)

SyncFacts
  -> typed facts with BehaviorId where needed

CharacterGameplaySyncAdapter
  -> resolve per fact
  -> enqueue packet
  -> record debug
```

固定字段 `ClientCommandBehavior`、`StateEffectBehavior`、`MotionCorrectionAckBehavior` 在迁移完成后应删除，避免长期混杂。

# Design: Sync Facts 和网络边界拆分

## 问题本质

角色 pipeline 每 tick 会产出两类东西：

- 本地立即消费的状态或表现，例如 strict gameplay state、motion result、animation contribution、local cue。
- 可以被记录、调试、回放、loopback、网络 backend 消费的同步事实，例如 action activation、lifecycle transition、window sample、motion digest、gameplay result、state effect、replicated cue。

后者现在叫 `NetworkOutput`，但它不是网络包，也不应该要求网络存在。更准确的模型是：

```text
Graph / Runtime / Stage
  -> CharacterPipelineOutput.SyncFacts
  -> CharacterNetworkSendStage
  -> CharacterGameplaySyncAdapter
  -> GameplaySyncRuntime / LocalLoopback / Fantasy backend
```

`SyncFacts` 是 pipeline 的输出层，`NetworkSendStage` 是网络 adapter 前的收集边界，`GameplaySyncAdapter` 才负责 packet 映射。

## 术语

- `Fact`：本 tick 已发生的业务事实，可以被记录、调试、回放、同步或忽略。
- `SyncFacts`：角色管线产出的可同步事实集合，不等于网络包。
- `SyncDomainOutput`：按 Motion、Action、GameplayResult、StateEffect、Presentation 分类的 fact bucket。
- `NetworkSendStage`：网络发送前的角色管线边界，只收集 SyncFacts，不生产 gameplay fact。
- `GameplaySyncAdapter`：把 SyncFacts 映射为 GameplaySync packet 的 adapter。

## 输出层

第一阶段保留三层输出，但重命名第三层：

```text
CharacterPipelineOutput
  StrictGameplay
  Presentation
  SyncFacts
```

`StrictGameplay` 表达本地 gameplay 决策和 motion 结算要用的强语义输出。

`Presentation` 表达本地动画、cue 和表现输出。

`SyncFacts` 表达可能跨系统消费的事实：

```text
SyncFacts.Motion
SyncFacts.Action
SyncFacts.GameplayResult
SyncFacts.StateEffect
SyncFacts.Presentation
```

## 单机模式

单机不需要为了普通 Timeline、locomotion、动画表现创建 `ActionContext` 或 `ActionInstanceId`。

如果单机逻辑没有产生可同步事实，`SyncFacts` 可以为空或无人消费。

如果单机逻辑选择使用 ActionInstance 做调试、回放或事务归属，它可以产生 SyncFacts，但仍不需要网络 backend。

## 网络可插拔边界

后续 backend 应该只消费 SyncFacts：

```text
None
  不消费 SyncFacts

LocalLoopback
  消费 SyncFacts 并回写模拟 incoming

FantasyBackend
  消费 SyncFacts 并发给服务端
```

Graph、ActionRuntime、MotionStage、TimelineNode 都不直接认识这些 backend。

## Action cancel 单一来源

新 action 覆盖旧 action 时，正式事实来源必须是 `ActionRuntime`：

```text
ActionRuntime.ActivateAction(request)
  -> ActionActivationOutcome
       Result
       Handle
       GeneratedLifecycleTransitions
```

GraphContext 只把 `GeneratedLifecycleTransitions` 写入 `SyncFacts.Action.LifecycleTransitions`。

GraphContext 不再根据 previous context 重新构造 `Cancel(CancelledByNewAction)`。这样 runtime debug、SyncFacts、网络包、回放记录都能看到同一条生命周期事实。

## 命名边界

需要删除的正式口径：

- `NetworkOutput` 作为 pipeline output 类型。
- `CharacterPipelineOutput.Network` 作为本帧事实输出属性。
- spec 中把 action lifecycle transition 叫作 `action end` 的旧文案。

可以暂时保留的口径：

- `CharacterNetworkSendStage`：它确实是网络 adapter 前的收集 stage。
- `CharacterNetworkReceiveStage`：它确实是 incoming 网络结果注入边界。
- `GameplaySyncRuntime`、`GameplaySyncPacket`、`SyncDomain`：这些属于同步运行时和 packet 层。

## 迁移原则

- 不保留 `NetworkOutput` 兼容别名。
- 不新增 fallback 属性。
- 所有 pipeline 读写点一次性改到 `SyncFacts`。
- 旧 `ActionEnd` 口径不回退，继续使用 `ActionLifecycleTransition`。
- 如果发现某个输出只服务本地表现，不需要同步或记录，应留在 `Presentation`，不迁移到 `SyncFacts`。


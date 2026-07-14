# Proposal: 拆分 Sync Facts 和 Network Output 语义

## Why

当前角色管线把一组可同步、可记录、可调试的业务事实命名为 `NetworkOutput`。这个名字会把两个职责混在一起：

- Graph、ActionRuntime、Motion、Timeline 产出的是本帧已经发生的 gameplay/sync fact。
- NetworkSendStage、CharacterGameplaySyncAdapter、loopback 或未来 Fantasy backend 才负责把这些 fact 转换成网络包或外部同步行为。

这会导致两个误导：

- 单机或本地表现模式看起来也必须“写 NetworkOutput”，让普通单机逻辑像被网络绑死。
- 后续做可插拔网络 backend 时，容易让 pipeline 直接依赖网络实现，而不是只暴露可消费的事实。

同时，新 action 覆盖旧 action 时，当前实现存在 runtime 内部应用 cancel、GraphContext 再构造 cancel 输出的风险。正式事实必须只有一个来源，否则 debug、回放和网络同步会看到不一致的生命周期记录。

## What Changes

- 将 `CharacterPipelineOutput.Network` 的正式口径改为 `CharacterPipelineOutput.SyncFacts`。
- 将 `NetworkOutput` 的正式类型口径改为 `CharacterSyncFacts` 或等价命名，不保留 `NetworkOutput` 兼容别名。
- 保留 Motion、Action、GameplayResult、StateEffect、Presentation 这些 SyncDomain output，但它们归属于 SyncFacts，而不是 NetworkOutput。
- `CharacterNetworkSendStage` 从 SyncFacts 收集数据，并继续只作为网络 adapter 前的收集边界；它不成为 fact 生产者，也不直接认识 peer、Fantasy 或 transport。
- `CharacterGameplaySyncAdapter` 只把 SyncFacts 映射为 GameplaySync packet；没有 backend 时，SyncFacts 可以被忽略、debug 或本地 loopback 消费。
- `ActionRuntime` 在新 action 覆盖旧 action 时必须生成并返回同一条 `ActionLifecycleTransition(CancelledByNewAction)`，Graph/Pipeline 只能转发该 fact，不再重新构造等价 transition。
- 更新现有 spec 中的 `NetworkOutput`、`network outputs`、`action end` 等旧口径为 SyncFacts 和 lifecycle transition。

## Non-Goals

- 不实现 `ActionScope` 或状态机 exit semantics。
- 不实现真实 Fantasy backend、local loopback backend 或 transport。
- 不实现完整 rollback/replay。
- 不新增测试任务；用户会端到端验证。
- 不把普通单机 Timeline、locomotion 或表现输出强制改成 action-scoped fact。

## 决策和 Tradeoff

### 方案 A：保留 `NetworkOutput`

- 优点：代码改动最少，现有 adapter 命名不需要迁移。
- 缺点：单机和网络语义继续混在一起；后续做可插拔 backend 时，pipeline 输出层仍像网络专属 API。
- 业务取舍：短期省事，但会继续让作者和实现者误以为“产出可同步事实”等于“必须接网络”。

### 方案 B：改成 `GameplayFacts`

- 优点：强调这是 gameplay 已发生事实，单机心智更自然。
- 缺点：范围太宽，容易把 strict gameplay state、表现 cue、调试日志都混进同一个层。
- 业务取舍：名字亲和，但边界不够硬，后续 SyncDomain policy、prediction、correction 的职责会变模糊。

### 方案 C：改成 `SyncFacts`

- 优点：明确这些输出是“可同步/可记录/可校验的事实”，不等于一定发网络；和现有 SyncDomain 口径一致。
- 缺点：仍然带一点同步色彩，普通单机作者需要理解“不消费也可以存在”。
- 业务取舍：最适合当前混合网络压力 demo：既不把单机绑到网络，也保留预测、loopback、Fantasy backend 的统一输入。

本 proposal 选择方案 C。

## 与现有 Spec 的关系

- `character-pipeline-runtime` 当前要求输出分为 strict、presentation 和 network；本变更把第三层改为 SyncFacts。
- `character-gameplay-sync-adapter` 当前要求 adapter 消费 `NetworkOutput`；本变更改为消费 SyncFacts。
- `character-network-sync-domain-contract` 已定义 SyncDomain；本变更让 SyncDomain output 明确落在 SyncFacts，而不是 NetworkOutput。
- `character-action-instance-runtime` 已要求新 action 覆盖旧 action 时 runtime 生成 cancel transition；本变更要求该 transition 作为同一条 SyncFact 转发。


# Proposal: 让 SyncFact 显式绑定 BehaviorId

## Why

当前非事务网络策略已经从 Adapter 硬编码提升到了 `GameplayBehaviorProfile`，但仍然通过 `CharacterPipelineDefinition` 上的三个固定槽位接入。这会让 Behavior 模型看似通用，实际只能服务 ClientCommand、StateEffect、MotionCorrectionAck 三类固定 fact。需要把策略绑定点收口到 SyncFact 边界，让每条需要同步的事实能携带或解析到明确 BehaviorId。

## What Changes

- 新增 fact-level behavior binding 合同，用 `SyncFactBehaviorBinding` 或等价正式配置表表达系统 fact 到 `GameplayBehaviorProfile` 的关系。
- 为非事务 SyncFact 补正式 BehaviorId 来源，包括 `ClientCommand`、`GameplayStateEffectEvent`、非 action 来源的 `GameplayResultEvent` 和 cue/event fact。
- Adapter 改为逐条 fact 解析 effective policy，不再按 `ClientCommandBehavior`、`StateEffectBehavior`、`MotionCorrectionAckBehavior` 三个固定槽位预解析。
- `CharacterPipelineDefinition` 删除固定非事务行为槽位，保留 `ActionProfiles`、`BehaviorProfiles` 和统一 SyncFact binding 表。
- Inspector 和 debug 按 fact kind、BehaviorId、BehaviorKind、SyncDomain、packet kind 和过滤原因展示结果。

## 背景

`add-gameplay-behavior-policy-model` 已经建立了 `GameplayBehaviorProfile`、`BehaviorKind` 和 `BehaviorNetworkPolicyResolver`，并把旧的 `ClientCommandFrame`、`StateEffect`、`MotionCorrectionAck` 隐藏策略迁移到了显式配置。但当前实现仍然处在中间态：非事务 fact 通过 `CharacterPipelineDefinition` 上的固定槽位找到行为策略，而不是由具体 SyncFact 自己携带或解析到 BehaviorId。

这会造成作者心智混杂：

- Behavior 看起来是通用行为策略目录。
- 但 runtime 只认 `ClientCommandBehavior`、`StateEffectBehavior`、`MotionCorrectionAckBehavior` 三个固定入口。
- State、Event、GameplayResult、Cue 等具体业务事实不能各自选择不同 BehaviorProfile。

## 目标

- 让进入网络边界的 SyncFact 可以显式携带 `BehaviorId`。
- 让 Adapter 优先按 fact-level `BehaviorId` 查询 `GameplayBehaviorProfile` 或 Transaction `ActionProfile`，再解析 effective policy。
- 将当前三个固定行为槽位迁移为统一的 SyncFact behavior binding 配置，或在能直接产出 BehaviorId 的 fact 上删除这些槽位。
- 保持 Graph、Timeline、节点不保存完整网络策略；它们最多引用 BehaviorProfile 或输出 BehaviorId。
- 保持 `SyncFacts` 仍是 Pipeline 和网络 Adapter 之间的唯一事实出口。

## Out of Scope

- 不实现 Fantasy transport。
- 不实现服务端权威裁决、rollback 或 rewind。
- 不把每个 Graph node 变成网络同步单位。
- 不让节点、Timeline clip、Blackboard 分散保存完整 prediction/authority/replication/correction policy。
- 不恢复 ActionModule、ActionSO、AbilityNodeTree 或 locomotion 特化 SO/config。

## 当前代码事实

- `CharacterPipelineOutput.SyncFacts` 已经按 Motion、Action、GameplayResult、StateEffect、Presentation 分域。
- 当前 fact 类型包括 `ClientCommand`、`ActionActivationOutput`、`ActionLifecycleTransition`、`ActionWindowSample`、`ActionMotionSample`、`GameplayResultEvent`、`GameplayStateEffectEvent`、`ActionCueEvent`、`Correction`。
- Transaction facts 可由 `ActionId` 或 `ActionInstanceId` 找到 `ActionProfile`，因此已经天然有 Transaction BehaviorId。
- 非事务 facts 目前缺少 BehaviorId：
  - `ClientCommand`
  - `GameplayStateEffectEvent`
  - 非 action 来源的 `GameplayResultEvent`
  - 非 action 来源的 `ActionCueEvent` 或后续通用 cue fact
  - `Correction` acknowledgement
- Adapter 当前仍通过 `policySource.ClientCommandBehavior`、`policySource.StateEffectBehavior`、`policySource.MotionCorrectionAckBehavior` 解析非事务 policy。

## 方案

采用“SyncFact 显式行为绑定”：

```text
Graph / Timeline / Runtime / Input
  -> SyncFact(BehaviorId 或 Transaction identity)
  -> CharacterNetworkSendStage
  -> BehaviorNetworkPolicyResolver
  -> CharacterGameplaySyncAdapter
  -> GameplaySyncPacket
```

第一阶段不需要引入一个大而全的 `SyncFact` 基类。保留现有 domain output 结构，但给需要策略分歧的 fact 类型增加稳定 `BehaviorId` 或等价 binding context。

## 取舍

### 选择 SyncFact 级 BehaviorId

优点：

- 行为策略标记落在网络边界事实上，网络层不用反查 Graph 路径、SubTree、Timeline 结构或 Blackboard。
- 可以表达 `State.Stun`、`State.Invincible`、`Cue.HitSpark`、`Cue.CameraShake`、`Result.Damage` 等不同策略。
- Action 事务仍然使用 ActionProfile，不把普通移动或状态塞进 ActionInstance。

缺点：

- 需要改多个 fact payload 和 Adapter 解析路径。
- 作者 UI 需要提供 BehaviorProfile 选择入口，否则会回到手填字符串。

### 不选择节点级完整网络策略

优点是看起来直观，但会污染 Graph/Timeline，导致同一个业务策略散落在节点、Timeline clip、ActionProfile、Adapter 中。该方案不符合当前 spec 的“Graph/Timeline 只产出 typed output，不保存完整网络策略”边界。

### 不选择只按 SyncDomain 默认策略

优点是最简单，但它只能表达“所有 StateEffect 一个策略”“所有 Cue 一个策略”，无法支持混合网络架构里不同业务事实的细粒度策略。它适合更小 demo，但不满足用户当前要的“任意显式标记”。

## 迁移策略

- 保留现有 `GameplayBehaviorProfile` 和 `ActionProfile` 的关系。
- 新增统一 `SyncFactBehaviorBinding` 或等价配置表，用于 input-derived 或 correction-derived 这类没有节点来源的 fact。
- `ClientCommand` 从 input frame 创建时必须获得正式 BehaviorId。
- `GameplayStateEffectEvent`、`GameplayResultEvent`、cue fact 必须能显式携带 BehaviorId。
- Adapter 解析规则：
  1. Transaction fact 使用 `ActionInstanceId` / `ActionId` 找 ActionProfile。
  2. 非事务 fact 使用 fact-level BehaviorId 找 GameplayBehaviorProfile。
  3. 缺失 BehaviorId 或 profile 时记录 Missing policy 并过滤，不使用隐藏 fallback。
- 当所有当前固定槽位都迁移完成后，删除 `m_ClientCommandBehavior`、`m_StateEffectBehavior`、`m_MotionCorrectionAckBehavior` 这类固定字段。

## Impact

- 会修改 `CharacterPipelineDefinition` 的序列化字段，需要把现有 Corin 配置里的三个固定引用迁移成 binding 表。
- 会修改多个 fact payload 的只读结构，使非事务 fact 在进入 Adapter 前能携带 BehaviorId。
- 会收紧 Adapter 语义：缺失 BehaviorId、缺失 binding 或 profile 类型不匹配时记录 Missing 并过滤，不再通过固定槽位继续发送。

## 风险

- 如果引入一个过大的 SyncFact 抽象，会让当前 domain output 结构变复杂。
- 如果允许手填 BehaviorId 字符串而没有 registry 选择，会产生资产错误和拼写风险。
- 如果固定槽位不删除，会继续形成“通用模型 + 固定入口”的混杂状态。

## Existing Spec Alignment

- 延续 `character-network-sync-domain-contract`：SyncDomain 仍是同步单位，不是 Graph path。
- 延续 `character-gameplay-sync-adapter`：Adapter 只消费 SyncFacts、resolved policy 和 runtime queue。
- 延续 `gameplay-behavior-policy-model`：Behavior 是身份和策略层，不替代执行模块。
- 收紧当前中间态：固定非事务行为槽位必须迁移为 fact-level binding 或统一 binding 表，不能长期作为特化配置。

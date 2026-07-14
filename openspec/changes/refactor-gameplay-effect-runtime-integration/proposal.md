# Change: 重构 Gameplay Effect Runtime 并完成唯一接入链路

## Why

当前 current specs 已经规定 Gameplay Effect 是不依赖 Character、BTSMTL、Network Model、Presentation 和 Diagnostics 的通用业务模块，也规定 Character 通过薄 Adapter、InputMapper 与 Projector 接入。但现有代码只完成了部分结构：`GameplayEffectRuntime` 同时承担 Spec 构建、应用事务、叠层、时间推进、Component 执行、预测和权威协调，单类已经超过一千行；`CharacterGameplayEffectAdapter` 只接入 Tag 和固定 Tick，权威输入始终为空，`CommitFacts` 只保存未消费的 `LastChangeSet`；旧 `GameplayStateEffectFact`、`StateEffectSyncDomain` 和 `ActionCueEvent` 仍是正式网络与表现路径。

继续在当前结构上追加功能会让 GE 规则、Character 编排和网络模型重新耦合，并长期保留两套 Effect/Cue 事实来源。本变更必须在不建立兼容层的前提下，把 GE Runtime 拆为单职责内部服务，完成 Character 双向桥接，并让当前 Session 选择的 Network Model 只通过模型无关事实连接 GE。

## What Changes

- 保留 `GameplayEffectRuntime` 作为 GE 唯一公开门面和状态所有者，将 Spec 构建、应用事务、生命周期调度、Component 执行、预测协调和 ChangeSet 记录拆入内部单职责服务。
- 所有内部服务共享同一个 Tag、Attribute、ActiveEffect、PredictionJournal 和 mutation transaction，不创建第二套容器、时钟、命令入口或事件总线。
- 将 `CharacterGameplayEffectAdapter` 收敛为固定 Tick 编排和翻译边界：Begin 使用 `CharacterGameplayEffectInputMapper` 转换语义输入，Commit 只消费一次 ChangeSet 并调用 Fact、Cue、Trace Projector。
- 为 `CharacterGraphContext` 提供只包含 Tag Reader、Attribute Reader 和 Effect Command Sink 的 Graph ports；BTSMTL 节点不持有 Adapter 或 Runtime。
- 将 Character 网络语义合同一次性迁移为 `GameplayEffectLifecycleFact`、`GameplayAttributeValueFact`、`GameplayEffectSyncDomainInput/Output` 和 `GameplayCueFact`。
- 删除 `GameplayStateEffectFact`、`StateEffectSyncDomainInput/Output`、`ActionCueEvent`、`ServerAuthoritativeStateEffect` 及其 packet、resolver、history、debug 和 authoring 引用，不保留别名、双写或 fallback。
- 保持 Network Model 在 Session 启动时唯一选择。GE Definition 只提供 EffectId/BehaviorId；当前 `ServerAuthoritativeHybrid` 只在自己的 Profile 和 Adapter 中逐条解析 Effect policy、构造模型 packet 并映射权威事实。
- 修正 current specs 与未归档 active changes 中仍使用 `State`、`StateEffect` 和 `ActionCueEvent` 的过期口径，防止后续归档重新引入旧合同。

## Impact

- Affected specs:
  - `gameplay-effect-runtime`
  - `character-gameplay-effect-integration`
  - `character-network-sync-domain-contract`
  - `character-gameplay-pipeline-closure`
  - `character-pipeline-runtime`
  - `gameplay-behavior-policy-model`
- Affected code:
  - `Assets/GameScripts/Main/Runtime/Gameplay/Effects`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/GameplayEffect`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/Graph`
  - `Assets/GameScripts/Main/Runtime/Character/Pipeline/Network`
  - `Assets/GameScripts/Main/Runtime/Character/Action`
  - `Assets/GameScripts/Main/Runtime/Networking/ServerAuthoritativeHybrid`
  - 相关 Editor、Diagnostics 和正式角色配置资产
- Breaking changes:
  - 旧 StateEffect、ActionCue 和模型 payload 类型直接删除。
  - CharacterPipeline 的 GE Begin/Commit 签名改为显式接收当前 frame 输入与输出。
  - GraphContext 构造时必须获得正式 GE graph ports。
  - ServerAuthoritative Profile 必须按 Effect BehaviorId 提供完整 policy，不存在默认 Effect policy。
- Out of scope:
  - 不新增第二种 Network Model。
  - 不新增 Fantasy Endpoint。
  - 不把 GE 变成独立 Tick、MonoBehaviour、全局 Manager 或服务定位器。
  - 不实现完整世界 rollback。

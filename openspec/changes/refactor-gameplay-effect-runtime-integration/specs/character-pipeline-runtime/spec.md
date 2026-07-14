## MODIFIED Requirements

### Requirement: Pipeline 输出事实必须继续通过 SyncFacts 边界产生

系统 MUST 保持 `CharacterPipelineOutput.SyncFacts` 作为 pipeline 输出事实边界。Blackboard variable MAY 为 Graph 提供运行时上下文；只有显式合法 fact projection 或 GameplayEffect ChangeSet projection 才能将当前写入转换为 Action、GameplayResult、GameplayEffect 或 Presentation SyncDomain output。NetworkSendStage MUST 只读取投影后的 SyncFacts，不得直接读取 Blackboard values、GE Container 或 Adapter 缓存。

#### Scenario: 投影 Action window

- **WHEN** WindowFactProjection 收到合法 ActionWindow-bound variable candidate
- **THEN** runtime MUST 生成 ActionWindowSample
- **AND** MUST 将其写入 `SyncFacts.Action.WindowSamples`
- **AND** NetworkSendStage MUST 继续从 SyncFacts 收集该事实

#### Scenario: 投影 Gameplay Effect 变化

- **WHEN** CharacterGameplayEffectAdapter Commit 当前 Tick ChangeSet
- **THEN** FactProjector MUST 将 lifecycle 和 attribute facts 写入 `SyncFacts.GameplayEffect`
- **AND** NetworkSendStage MUST 不直接读取 GameplayEffectRuntime 或 LastChangeSet

#### Scenario: 写入 local-only 临时值

- **WHEN** 节点写入 Projection=None 的本地 Blackboard variable
- **THEN** 该值 MUST NOT 自动进入 SyncFacts
- **AND** NetworkSendStage MUST NOT 因该变量存在生成 outgoing packet

#### Scenario: 缺失 projection provenance

- **WHEN** ActionWindow-bound 写入缺少显式 Action Context
- **THEN** runtime MUST 拒绝生成 ActionWindowSample 并报告原因

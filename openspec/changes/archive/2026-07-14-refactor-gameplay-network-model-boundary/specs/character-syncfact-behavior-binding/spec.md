## MODIFIED Requirements

### Requirement: SyncFact 必须作为 BehaviorId 的网络边界绑定点

进入 model adapter 的 SyncFact MUST 携带或解析到稳定 BehaviorId。BehaviorId MUST 指向 gameplay ActionProfile 或 Behavior identity；具体 Network Model MUST 使用该 ID 查询自己的 policy profile。SyncFact MUST 不直接携带跨模型通用 prediction、authority、replication 或 packet policy。

#### Scenario: Stream fact 进入当前模型

- **WHEN** resolved motion fact 携带 Locomotion BehaviorId
- **THEN** ServerAuthoritative adapter MUST 在自己的 profile 中查询该 ID
- **AND** GameplayBehavior identity MUST 不提供 model policy fallback

### Requirement: Transaction facts 必须继续从 ActionProfile 解析 BehaviorId

Transaction facts MUST 继续通过 ActionId/ActionInstanceId 解析到 ActionProfile 的稳定 ActionId，并将其作为 BehaviorId。Network Model policy MUST 再使用该 ID 查询 model profile；ActionProfile MUST 不作为 effective network policy 来源。

#### Scenario: Attack Window fact

- **WHEN** adapter 收到带 ActionInstanceId 的 HitWindow fact
- **THEN** MUST 通过 ActionRuntime 解析 ActionId
- **AND** MUST 通过 ActionId 查询 ServerAuthoritative Action policy

### Requirement: 系统事实必须使用正式 SyncFact behavior binding

Correction application result、StateEffect 和其它非事务系统事实需要模型处理时，MUST 通过 model profile 中的显式 fact kind 到 BehaviorId binding 解析。CharacterPipelineDefinition MUST 不再拥有 network policy binding；缺失 binding MUST 配置失败。

#### Scenario: Correction application result

- **WHEN** ServerAuthoritative adapter 需要构造 CorrectionAck
- **THEN** MUST 使用 model profile 的正式 BehaviorId binding
- **AND** MUST 不使用隐藏 `Character.Motion.CorrectionAck` 默认值

### Requirement: Adapter 必须逐条 fact 解析 effective policy

Model-owned adapter MUST 对每条 fact 使用当前 model resolver 解析 effective policy。ServerAuthoritative adapter MUST 不复用一个跨模型 packet policy，也 MUST 不在 CharacterNetworkSendStage 中提前写死 packet kind。

#### Scenario: 同 tick 两种 Cue

- **WHEN** 同 tick 产生 local camera cue 与 replicated VFX cue
- **THEN** model adapter MUST 分别按 BehaviorId/ActionId 解析 policy
- **AND** MUST 只为允许的 fact 构造 packet

### Requirement: Authoring UI 必须选择 BehaviorProfile 而不是手填完整策略

Gameplay authoring MUST 选择 ActionProfile 或 Behavior identity；ServerAuthoritative model authoring MUST 在自己的 profile 中引用这些稳定 identity 并编辑完整模型策略。Graph、Timeline、Blackboard projection 和 CharacterPipelineDefinition MUST 不手填或复制完整 model policy。

#### Scenario: 配置 StateEffect

- **WHEN** 作者为 StateEffect fact 配置 ServerAuthoritative policy
- **THEN** MUST 在 model profile 中选择正式 Behavior identity
- **AND** Graph/Timeline MUST 不保存该 policy

### Requirement: Debug 必须按 fact 展示 Behavior 解析结果

Model Debug MUST 按 fact 显示 BehaviorId、BehaviorKind、ModelId、model policy id、packet kind、发送/过滤状态和原因。Transaction fact MUST 同时显示 ActionProfile identity 与 model Action policy 来源，避免把两者视为同一资产。

#### Scenario: 查看被过滤 Cue

- **WHEN** ServerAuthoritative policy 过滤 local-only Cue
- **THEN** Debug MUST 显示 gameplay BehaviorId/ActionId
- **AND** MUST 显示当前 ModelId 和过滤 policy source


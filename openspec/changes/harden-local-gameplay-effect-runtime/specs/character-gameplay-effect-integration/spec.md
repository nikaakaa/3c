## MODIFIED Requirements

### Requirement: BTSMTL 必须通过分离的 Query 与 Self Command ports 使用 Gameplay Effect

`CharacterGraphContext` MUST 分别暴露只读 `CharacterGameplayEffectQueryPorts` 与受控 `CharacterGameplayEffectCommandPorts`。Query ports MUST 只包含 TagReader 和 AttributeReader；Command ports MUST 只提供对当前 Character 的 ApplySelf/RemoveSelf。ApplySelf 的 source actor 与 target actor MUST 由 Adapter 使用当前 Character ActorId 构造，Graph 节点 MUST NOT 手填 actor identity 或假装路由其他角色。节点 MUST NOT 持有 Adapter、Runtime、ActiveEffect collection 或 prediction journal。

#### Scenario: Graph 对自身应用资源消耗

- **WHEN** ApplyEffect 节点提交 Stamina Cost Effect
- **THEN** Command ports MUST 使用当前 Character ActorId 构造 source=target 的 Context
- **AND** 同 Tick 后续 Query ports MUST 读取到已提交的 Stamina

#### Scenario: 作者尝试填写远端目标

- **WHEN** 作者配置 ApplyEffect 节点
- **THEN** 节点 MUST 不提供 SourceActorId 或 TargetActorId 字符串字段
- **AND** 跨角色 Effect MUST 继续经过正式 GameplayResult 路由

### Requirement: Character 子阶段必须只获得 Gameplay Effect 最小能力

ActionRuntime MAY 获得 TagReader 和 scoped TagSourceSink；Graph MAY 获得分离的 Query 与 Self Command ports；MotionStage MAY 获得 AttributeReader 或专用 motion context，但 MUST NOT 获得 GameplayEffectRuntime、Adapter、AttributeStore 或 Effect command 能力。MotionStage 为读取 Action target 和 diagnostics 使用的 context MUST 是不暴露 Graph mutation 与 GE command 的专用接口。

#### Scenario: Motion 解析 MotionWarp target

- **WHEN** MotionStage 需要读取 ActionInstance target snapshot
- **THEN** 它 MUST 通过专用 motion context 查询
- **AND** 该 context MUST 不暴露 Gameplay Effect Apply/Remove 能力

### Requirement: Character ActorId 必须由 Character 实例唯一拥有

每个可运行 `CharacterPipelineHost` MUST 配置唯一非空 ActorId，并传入 CharacterPipeline、GraphContext 和 GameplayEffectAdapter。Network Model binding MAY 复制该 ActorId 作为 subject identity，但 MUST NOT 保存第二份可独立配置的 SubjectActorId。GameplayEffectContext 的 Self source/target MUST 使用该 ActorId。

#### Scenario: 创建本地 CharacterPipeline

- **WHEN** CharacterPipelineHost 使用合法配置创建 pipeline
- **THEN** Pipeline、Graph Self command 与 GE Adapter MUST 共享同一 ActorId
- **AND** 缺失 ActorId MUST 阻止 runtime 创建

#### Scenario: 模型 binding 注册角色

- **WHEN** ServerAuthoritative binding 注册 Character
- **THEN** binding MUST 读取 CharacterPipelineHost.ActorId
- **AND** Inspector 或场景资产 MUST 不再保存独立 SubjectActorId

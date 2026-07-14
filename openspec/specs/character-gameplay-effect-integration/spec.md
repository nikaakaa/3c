# character-gameplay-effect-integration Specification

## Purpose
规定通用 Gameplay Effect Runtime 接入 CharacterPipeline 的唯一装配、固定 Tick、Self 命令、只读查询、事实投影和跨角色边界，保证 Character 只做翻译与编排，不复制 GE 规则或网络模型职责。
## Requirements
### Requirement: CharacterPipeline 必须唯一持有 Gameplay Effect Adapter

系统 MUST 由每个 `CharacterPipeline` 唯一创建、持有和释放一个 `CharacterGameplayEffectAdapter`，并由该 Adapter 唯一持有一个通用 `GameplayEffectRuntime`。Pipeline MUST 只调度 Adapter 的 Begin/Commit 生命周期，不得实现或访问 Tag、Attribute、Active Effect Container 与 prediction journal。系统 MUST NOT 创建独立 `MonoBehaviour.Update`、Coroutine、静态全局管理器或第二套角色效果容器。

#### Scenario: Host 创建角色管线

- **WHEN** `CharacterPipelineHost` 使用有效 `CharacterPipelineDefinition` 创建 pipeline
- **THEN** pipeline MUST 同时创建唯一 `CharacterGameplayEffectAdapter`
- **AND** Adapter MUST 创建并唯一持有一个 `GameplayEffectRuntime`
- **AND** Host MUST NOT 单独创建 Tag、Attribute 或 Gameplay Effect runtime

#### Scenario: Pipeline 被释放

- **WHEN** `CharacterPipeline` dispose
- **THEN** Adapter MUST 释放 `GameplayEffectRuntime` 并使其全部 active effect、modifier handle、tag source 和预测记录失效
- **AND** 任何后续 effect 操作 MUST 明确失败

### Requirement: Character ActorId 必须由 Character 实例唯一拥有

每个可运行 `CharacterPipelineHost` MUST 配置唯一非空 ActorId，并传入 CharacterPipeline、CharacterGraphContext 和 CharacterGameplayEffectAdapter。Network Model binding MAY 读取该 ActorId 作为 subject identity，但 MUST NOT 保存第二份可独立配置的 SubjectActorId。GameplayEffectContext 的 Self source/target MUST 使用该 ActorId。

#### Scenario: 创建本地 CharacterPipeline

- **WHEN** CharacterPipelineHost 使用合法配置创建 pipeline
- **THEN** Pipeline、Graph Self command 与 GE Adapter MUST 共享同一 ActorId
- **AND** 缺失 ActorId MUST 阻止 runtime 创建

#### Scenario: 模型 binding 注册角色

- **WHEN** ServerAuthoritative binding 注册 Character
- **THEN** binding MUST 读取 CharacterPipelineHost.ActorId
- **AND** Inspector 或场景资产 MUST 不再保存独立 SubjectActorId

### Requirement: Character Gameplay Effect Adapter 必须保持薄翻译边界

`CharacterGameplayEffectAdapter` MUST 只负责 Character semantic input 到通用 authority/command input 的映射、固定 Tick 调度、窄端口委托和 `GameplayEffectChangeSet` 投影。Effect/Attribute/Tag 规则、叠层、周期、聚合和 prediction journal MUST 位于 `GameplayEffectRuntime`；ServerAuthoritative policy、packet、history 和 endpoint MUST 位于模型模块。Adapter MUST NOT 按 EffectId 编写业务 switch，MUST NOT 直接修改 Runtime Container，MUST NOT 使用全局事件总线或 ServiceLocator。

#### Scenario: 收到权威 Effect 输入

- **WHEN** Character semantic input 包含一条 Confirmed GameplayEffectLifecycleFact
- **THEN** `CharacterGameplayEffectInputMapper` MUST 把它转换为通用 authority input并交给 Runtime
- **AND** Adapter MUST NOT 自己创建 ActiveEffect、Modifier 或 Tag source

#### Scenario: GE 产生状态变化

- **WHEN** GameplayEffectRuntime 返回当前 Tick ChangeSet
- **THEN** Fact、Cue 和 Trace Projector MUST 从同一 ChangeSet 产生各自 Character 输出
- **AND** GameplayEffectRuntime MUST NOT 直接访问 CharacterPipelineOutput、Presentation 或 Diagnostics

### Requirement: Gameplay Effect 必须进入角色固定逻辑 tick 的正式顺序

每个逻辑 tick MUST 按 `NetworkReceive -> ActionLifecycleInput -> GameplayEffectAdapter Begin -> Input -> BTSMTL -> Motion -> GameplayEffectAdapter CommitFacts -> NetworkSend -> FrameCleanup` 的顺序处理角色 Gameplay Effect。Adapter Begin MUST 先用 InputMapper 转换权威输入，再推进 Runtime 的到期、周期触发和抑制状态刷新；CommitFacts MUST drain 当前 Tick 唯一 ChangeSet 并由 Projector 提交属性、效果、cue 和 trace。系统 MUST NOT 按 render frame delta 推进 duration 或 period。

#### Scenario: 同一逻辑 tick 收到眩晕结果

- **WHEN** `NetworkReceiveStage` 在 tick 起点注入一个对本角色生效的眩晕 GameplayResult
- **THEN** Adapter Begin MUST 在 Input 和 BTSMTL 前让 Runtime 应用对应 effect 与 granted tag
- **AND** 同 tick 的 Graph MUST 能读取该 tag 并决定动作生命周期输出

#### Scenario: 单个 render frame 执行多个逻辑 tick

- **WHEN** `GameplayTickSystem` 在一个 render frame 内补跑多个 logic tick
- **THEN** effect duration、period 和 prediction journal MUST 分别按每个 logic tick 推进
- **AND** presentation frame MUST NOT 额外推进 Gameplay Effect

### Requirement: BTSMTL 必须通过分离的 Query 与 Self Command ports 使用 Gameplay Effect

`CharacterGraphContext` MUST 分别暴露只读 `CharacterGameplayEffectQueryPorts` 与受控 `CharacterGameplayEffectCommandPorts`。Query ports MUST 只包含 TagReader 和 AttributeReader；Command ports MUST 只提供对当前 Character 的 ApplySelf/RemoveSelf。ApplySelf 的 source actor 与 target actor MUST 由 Adapter 使用当前 Character ActorId 构造，Graph 节点 MUST NOT 手填 actor identity 或假装路由其他角色。节点 MUST NOT 持有 Adapter、Runtime、ActiveEffect collection 或 prediction journal。

#### Scenario: Transition 读取硬直标签

- **WHEN** Condition Rule Graph 使用 `HasTag(State.HitReact.Stagger)`
- **THEN** 节点 MUST 通过 `IGameplayTagReader` 从当前角色统一 Gameplay Effect 读取
- **AND** 节点 MUST NOT 从 Blackboard 或 `ActionRuntime` 私有字符串集合读取

#### Scenario: Graph 对自身应用消耗效果

- **WHEN** 命令节点提交一个以自身为 target 的 Instant stamina cost effect
- **THEN** Command ports MUST 使用当前 Character ActorId 构造 source=target 的 Context
- **AND** 后续同 tick Query ports MUST 看到已提交的属性结果

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

### Requirement: ActionRuntime 与 Gameplay Effect 必须保持事务和持续状态边界

`ActionRuntime` MUST 继续只管理动作事务身份、激活和 lifecycle。动作阻断、取消许可、状态要求与持续效果 MUST 通过统一 Tag、Attribute 和 Active Effect 查询表达。Gameplay Effect MUST NOT 直接修改 `ActionInstance` 生命周期；需要取消、打断或拒绝动作时，Graph 或正式协调 stage MUST 根据 Gameplay Effect 事实提交 `ActionLifecycleTransition`。

#### Scenario: 眩晕打断攻击

- **WHEN** Gameplay Effect 已授予 `State.CrowdControl.Stun`
- **THEN** Graph 或 action lifecycle coordinator MUST 读取该 tag 并提交 `Interrupt`
- **AND** Gameplay Effect runtime MUST NOT 直接关闭 `ActionInstance`

#### Scenario: 动作激活要求资源

- **WHEN** Graph 准备激活一个要求 stamina 下限且被沉默 tag 阻断的动作
- **THEN** Graph MUST 从 Gameplay Effect 读取属性与 tag query 后再提交 activation request
- **AND** `ActionRuntime` MUST NOT 维护另一份资源值或状态 tag

### Requirement: 跨角色 Gameplay Effect 必须经过 GameplayResult 路由

角色 pipeline 的 Adapter MUST 只向自身 GameplayEffectRuntime 提交命令。攻击者对目标造成伤害、控制或增益时，来源角色 MUST 产出携带 source、target、effect identity、context 和结果身份的 `GameplayResult`；目标角色 MUST 由正式结果路由在自己的 `NetworkReceive` 边界消费。系统 MUST NOT 通过节点持有目标 Adapter/Runtime 引用或跨 pipeline 直接调用 apply。

#### Scenario: 攻击命中目标

- **WHEN** 攻击者裁决一次命中并需要对目标应用 damage 与 stagger effect
- **THEN** 攻击者 MUST 生成面向目标的正式 `GameplayResult`
- **AND** 目标 MUST 在自己的 receive/result 路由中应用 effect

#### Scenario: 自身消耗

- **WHEN** 动作只对自身扣除 stamina
- **THEN** 当前 pipeline MAY 通过本地同步 mutation 应用 Instant effect
- **AND** 系统 MUST NOT 为同角色同步读取额外建立第二条 result loop

### Requirement: Gameplay Effect 输出必须进入统一事实与表现边界

GameplayEffectRuntime MUST 把 effect apply/remove/inhibit、attribute change、tag change 和 cue request 写入唯一 `GameplayEffectChangeSet`。Character 接入层 MUST 由 Fact/Cue/Trace Projector 分别把同一 ChangeSet 投影为 `CharacterPipelineOutput` 正式事实、`GameplayCueFact` 和 diagnostics。模型层事实 MUST 使用 model-neutral 类型；系统 MUST 删除 `ActionCueEvent` 的专用类型和链路，不得同时保留 action cue 与 gameplay cue 两套事实。

#### Scenario: 属性发生正式变化

- **WHEN** active effect 使 Health current value变化
- **THEN** `CharacterGameplayEffectAdapter.CommitFacts` MUST 通过 FactProjector 生成携带 attribute id、old value、new value、revision 和 context 的属性事实
- **AND** NetworkSend 与 diagnostics MUST 消费同一事实来源

#### Scenario: 效果触发命中特效

- **WHEN** effect component 产生一个需要表现的 cue
- **THEN** PresentationStage MUST 从正式 `GameplayCueFact` 消费它
- **AND** effect runtime MUST NOT 直接调用 Animancer、VFX、Audio 或场景组件

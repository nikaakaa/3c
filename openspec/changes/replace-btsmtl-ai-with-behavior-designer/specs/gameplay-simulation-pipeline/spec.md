## MODIFIED Requirements

### Requirement: 有状态 Pass 必须进入正式 Snapshot 或重建合同

Pass MUST将状态声明为 Stateless、Reconstructible、SnapshotParticipant或 ExternalSource。任何影响未来模拟、restore、replay、hash或 output disposition的状态 MUST由 SnapshotParticipant提供 canonical capture/restore/hash，或由 Reconstructible声明完整重建依据。Execution Backend MUST以 `SimulationPipelineStateSnapshot` 按稳定 PassId顺序聚合 participant，并与 Character/World snapshot在同一 Session restore transaction中校验和恢复。

Session首次启动 MUST显式选择从已激活Pass捕获默认Pipeline state，或恢复一份完整给定snapshot。Backend MUST在Launch Plan生成前取得真实participant集合；MUST不以人工空participant snapshot代替包含SnapshotParticipant的Pipeline初始状态。恢复给定snapshot后 MUST重新捕获并核对相同canonical hash。

只有其影响被完整表达为正式不可改写输入的生产者状态，才能作为 ExternalSource 留在输入生产端。已冻结输入/捕获身份/生产 frontier 与插件实例不随角色或消费事务恢复；输入消费 cursor、模型 eligible 调度、history 和 EventId 处置仍 MUST由 SnapshotParticipant 或具有完整依据的 Reconstructible 所有者负责。系统 MUST不以整个 Source 标为 ExternalSource 来漏掉这些可恢复状态，也不得把影响模拟的隐藏状态塞入输入生产者旁路。

#### Scenario: Prediction history 影响后续纠偏

- **WHEN** 一个 Pipeline Pass保存会改变后续 replay或 output disposition的 cursor
- **THEN** Pass MUST声明正式 snapshot或 reconstruct owner
- **AND** 若没有该合同，composition MUST创建失败

#### Scenario: Endpoint socket 不属于 Gameplay Snapshot

- **WHEN** Network Model Source持有 transport socket和接收队列
- **THEN** 它 MUST声明为 ExternalSource状态并留在 Source所有权
- **AND** MUST不把 socket或 packet object编码进 Character、World或 Pipeline Gameplay snapshot

#### Scenario: 有状态 Prediction Pipeline 首次启动

- **WHEN** Pipeline包含Prediction History SnapshotParticipant且Session选择默认初始状态
- **THEN** Backend MUST在Pass激活后捕获该participant的Tick 0 canonical payload
- **AND** Launch Plan MUST记录包含该participant的Pipeline state hash

#### Scenario: 插件决策作为外部输入生产者

- **WHEN** 插件仅通过 Source 冻结的正式输入影响角色
- **THEN** 插件实例状态与不可改写生产事实 MUST明确声明外部输入所有权
- **AND** 模型消费/调度/输出处置状态 MUST继续进入原有恢复合同

### Requirement: Standard Local Pipeline 必须保持唯一正式单机执行链

Float32 Local组合 MUST继续只安装`LocalInputIngressPass -> LocalSingleStepSchedulePass -> Float32ProgramEvaluatePass -> Float32WorldResolveBatchPass -> Float32ProgramFinalizePass -> LocalImmediateOutputPass`。`LocalInputIngressPass` MUST提升为唯一Local Control Input Ingress和`CanonicalInputBatch`唯一writer：它读取锁定Program roster、Prepared Control Source roster与上一轮committed Actor Observation，按稳定ActorId准备Player、Neutral与AI输入，验证完整roster后一次发布batch。Fixed Local MUST继续使用已安装的对应 Fixed 同型链。输入生产 MUST通过正式 Source 批量准备，并将已冻结输入与可恢复消费状态分开；Ingress MUST不再捕获旧 AI 候选状态或回滚插件实例。需要恢复的输入消费/调度状态仍 MUST进入正式 participant/reconstruct 合同。Pipeline MUST不增加AI专用Ingress、第二input writer、endpoint、history、correction、restore schedule或replay；旧固定`SimulationSessionRuntime`与`LocalSimulationDriver` MUST不恢复。

#### Scenario: 玩家与AI Actor单机运行

- **WHEN** Standard Local composition包含一个Player Control Source和一个AI Control Source并收到LocalLogicTick
- **THEN** LocalInputIngressPass MUST从同一committed Observation准备两个Actor输入并写入一个CanonicalInputBatch
- **AND** 两个Character Program MUST进入一个SimulationTick和一个World ResolveBatch

#### Scenario: AI输入准备后Character执行失败

- **WHEN** 插件输入已完整冻结但同一outer Tick的Character Evaluate或WorldSolver失败
- **THEN** Backend MUST恢复Tick前Character、World及正式消费状态，Source MUST保留原输入事实与生产序号
- **AND** LocalImmediateOutputPass与Committer MUST不发布该Tick结果

### Requirement: Pipeline 失败必须保持外层事务原子

Execution Backend MUST在一个 outer LogicTick内使用 working Character/World/Pipeline state执行全部 restore和内部 step。任一 Pass、product、Kernel、Solver、Finalize、snapshot或 output disposition失败时，MUST不发布该 outer Tick的 working state或外部副作用；若 Solver已接触实际 world body，MUST通过正式 restore/reconstruct合同恢复。Committer在 state publish后失败时 Session MUST fail-stop，MUST不伪造已触发副作用的回滚。

本条事务外部副作用指由 working 模拟结果派生的玩法、表现和模型结果发布。正式 Source 已冻结/发送的输入事实与传输控制消息具有其明确的外部输入所有权，不随角色事务撤销；系统 MUST不为它们再建立 Gameplay Commit。模拟失败后的可允许重试 MUST读取原输入而不重新执行插件，生产中途失败 MUST终止对应会话，MUST不继续半恢复的插件状态。

#### Scenario: 第二个 Replay Step 失败

- **WHEN** Restore 100后 Replay 101成功但 Replay 102的 WorldResult identity错误
- **THEN** Backend MUST拒绝整个 outer transaction并恢复 outer Tick前正式 state
- **AND** Replay 101的 Presentation或 Network输出 MUST不被提交

#### Scenario: 本端输入已发送但模拟事务失败

- **WHEN** 正式 Source 已发布本 Tick 输入，而后续角色/世界执行失败
- **THEN** Backend MUST撤销本轮模拟结果，输入 Source MUST保留已发布输入身份
- **AND** 重发/允许的重试 MUST使用相同内容，不得生成另一个同 Tick 输入

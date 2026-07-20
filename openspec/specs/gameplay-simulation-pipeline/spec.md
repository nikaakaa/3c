# gameplay-simulation-pipeline Specification

## Purpose
定义 Simulation Pipeline 从显式 Definition 编译为不可变四阶段计划，并以唯一 Schedule、World Step、Egress 和原子 Commit 执行 Gameplay Session。
## Requirements
### Requirement: Session Pipeline 必须由显式 Definition 编译为不可变计划

系统 MUST以 `SimulationPipelineDefinition` 显式保存 PipelineId、Revision 以及 Ingress、Schedule、Step、Egress 四阶段的 ordered Pass Definition。Session preparation MUST将其转换为 portable descriptor，再由所选 Execution Backend 编译为不可变 backend-specific plan。Host MUST不通过反射扫描、类型名、默认列表、Network Model 隐藏注入或运行时 callback 拼装 Pipeline；Active 后 MUST不增加、删除、替换或重排 Pass。

#### Scenario: 创建标准 Local Pipeline

- **WHEN** Local composition 引用合法的 Standard Local Pipeline Definition
- **THEN** preparation MUST得到包含精确 PassId、version、config hash 和稳定顺序的 compiled plan
- **AND** Session descriptor MUST记录 PipelineId、Revision 与 PipelineHash

#### Scenario: 运行中修改 Pipeline 资产

- **WHEN** Active Session 对应的 Pipeline Definition 或 Pass config 被修改
- **THEN** 当前 Session MUST拒绝热替换并进入明确失败或保持已锁定旧组合直到销毁
- **AND** MUST不在下一个 Tick 偷换 compiled plan

### Requirement: Pipeline 必须以四阶段和固定 Commit 边界执行

Pipeline schema MUST只定义 Ingress、Schedule、Step 与 Egress 四个顶层阶段。Ingress MUST只产生 source/input/ingress 产品；Schedule MUST恰有一个 `SimulationSessionExecutionPlan` producer；Step Pass MUST对 plan 中每个内部 SimulationStep 执行；Egress MUST只消费 step result并生成 snapshot/history/hash/source output 与 EventId disposition。最终 state publish 与 external Commit MUST由 Execution Backend 的固定事务边界拥有，不得实现为可替换 Pass。

#### Scenario: Local 外层 Tick

- **WHEN** GameplayTickSystem 向 Local runtime handle提交一个 LocalLogicTick
- **THEN** Pipeline MUST按 Ingress、Schedule、一个 Step sequence和 Egress执行
- **AND** 只有全部阶段校验成功后 Backend MAY原子发布 state并调用 Committer

#### Scenario: Pass 尝试跨阶段越权

- **WHEN** Ingress Pass尝试直接替换 Character state或 Egress Pass尝试重新执行 Program operation
- **THEN** phase contract或 product ownership校验 MUST拒绝该组合
- **AND** MUST不通过万能 Context 暴露越权写入口

### Requirement: Schedule 必须能产生零到多个内部 SimulationStep

唯一 Schedule producer MUST生成带 outer source tick、可选完整 restore directive和 ordered SimulationStep sequence的 `SimulationSessionExecutionPlan`。Plan MAY为 Pending并执行零个 step，也 MAY在一次 outer LogicTick 中先 restore再执行一个或多个 Forward、Replay、Current或 Authoritative step。GameplayTickSystem MUST始终只推进一次 Session runtime handle；内部 replay MUST不注册第二个 logic target、私有 Update、协程或 Task loop。

#### Scenario: Prediction correction 重放多个 Tick

- **WHEN** Schedule Pass收到权威 Tick 100并发现本地未确认输入覆盖 101 至 103
- **THEN** Plan MAY声明 Restore 100以及 Replay 101、Replay 102、Current 103的 ordered sequence
- **AND** Backend MUST在一个 outer transaction中执行这些 step并只向外提交合法最终输出

#### Scenario: Network Source 尚未准备好输入

- **WHEN** Schedule Pass无法为当前 source tick生成完整 canonical input
- **THEN** Plan MAY返回 Pending并执行零个 SimulationTick
- **AND** MUST不伪造 neutral input、空 Tick或 Local fallback

### Requirement: Pass 必须声明正式 Product 和唯一写入所有权

每个 Pass Definition MUST声明稳定 PassId、implementation version、phase、config hash、consume/produce product、exclusive/append-only/readonly权限、NumericProfile、Target ABI、Backend/Solver capability、execution kind支持和 state class。每个 exclusive product MUST恰有一个 producer；append-only product MUST声明稳定 ActorId/Tick/sequence/provenance顺序。Pass MUST不通过 `Dictionary<string, object>`、字符串黑板、static、closure共享字段或未声明 mutable context传值。

#### Scenario: 两个 Pass 生成同一个 WorldSolveBatchResult

- **WHEN** Pipeline 中两个 Step Pass都声明自己是 `WorldSolveBatchResult` 的 exclusive producer
- **THEN** Pipeline compile MUST失败并报告两个 Pass identity
- **AND** MUST不按列表最后一个、优先级或类型名选择胜者

#### Scenario: 第三方 Pass 增加正式产品

- **WHEN** 第三方 Pass需要传递基础产品以外的数据
- **THEN** 它 MUST提供稳定 ProductId、schema version、owner、canonical identity和 diagnostics shape
- **AND** consumer MUST显式声明该产品依赖

### Requirement: Pipeline Compiler 必须在 Active 前完成完整兼容校验

Pipeline Compiler MUST在 Runtime创建前校验 phase/order、Schedule唯一性、product producer/consumer、依赖环、Pass factory/version、Source port、Program Runtime ABI、Execution Backend semantic version、Solver capability、Replay/Restore requirement和 state ownership。编译 MUST产生稳定 PipelineHash和不可变 plan；unknown Pass、unknown product、缺失 factory或 unsupported capability MUST明确失败，不得跳过、替换或降级。

#### Scenario: Rollback Pass 配置到 Unity Local 组合

- **WHEN** Pipeline Pass要求 DeterministicReplay和 Snapshotable Solver，但 composition使用 Float32 Local Program Runtime与 Unity CharacterController Solver
- **THEN** Pipeline compile MUST在首 Tick前失败并列出缺失能力
- **AND** MUST不删除 Rollback Pass或改用 Local单步执行

### Requirement: 有状态 Pass 必须进入正式 Snapshot 或重建合同

Pass MUST将状态声明为 Stateless、Reconstructible、SnapshotParticipant或 ExternalSource。任何影响未来模拟、restore、replay、hash或 output disposition的状态 MUST由 SnapshotParticipant提供 canonical capture/restore/hash，或由 Reconstructible声明完整重建依据。Execution Backend MUST以 `SimulationPipelineStateSnapshot` 按稳定 PassId顺序聚合 participant，并与 Character/World snapshot在同一 Session restore transaction中校验和恢复。

Session首次启动 MUST显式选择从已激活Pass捕获默认Pipeline state，或恢复一份完整给定snapshot。Backend MUST在Launch Plan生成前取得真实participant集合；MUST不以人工空participant snapshot代替包含SnapshotParticipant的Pipeline初始状态。恢复给定snapshot后 MUST重新捕获并核对相同canonical hash。

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

### Requirement: Program、Pipeline 与 Backend 身份必须相互独立并共同锁定

ProgramHash MUST只表示 Numeric Target Program，MUST不包含 Pipeline、Source、Backend、Solver或 Network Model。Active Session、Snapshot、diagnostics和后续网络 handshake MUST另外锁定 PipelineId/Revision/PipelineHash、BackendId/semantic version、SourceId和 Solver identity。同一 Program MAY由多个合法 Pipeline使用，但不同 PipelineHash或 Backend semantic version的 Session snapshot MUST不可互换。

#### Scenario: 同一 Corin Program 用于 Local 与 Prediction

- **WHEN** Local Pipeline和 ServerAuthoritative Prediction Pipeline使用同一 Float32 Corin Program
- **THEN** 两者 ProgramHash MAY相同
- **AND** 两者 Session composition hash和 PipelineHash MUST不同

### Requirement: 普通扩展必须使用 Pass，完整执行技术替换必须使用 Backend

新增 input validation、correction、history、replay scheduling、hash、snapshot export或其它能由四阶段产品合同表达的处理 MUST作为正式 Pass实现并复用已安装 Backend。需要更换状态布局、执行技术或 Pipeline执行机制的实现 MAY提供新的 Execution Backend，但该 Backend MUST消费 versioned Pipeline descriptor、声明支持的 Program Runtime ABI与 semantic version，并返回同一 numeric-neutral runtime handle。Network Model MUST不复制 Common Host、Commit事务或 BTSMTL业务 evaluator来伪装 Backend。

#### Scenario: 增加 Lag Compensation Pass

- **WHEN** ServerAuthoritative 模型增加正式 world query/rewind产品合同可表达的 lag compensation
- **THEN** 模型 MAY在自己的 Pipeline中显式增加对应 Pass
- **AND** MUST不修改 CharacterPipelineHost或创建第二 runtime handle

#### Scenario: 增加 ECS 执行实现

- **WHEN** 第三方需要使用不同状态布局和 ECS执行 Pipeline
- **THEN** 它 MUST提供新的 Execution Backend和匹配 Program Runtime ABI
- **AND** MUST不把 ECS状态塞进 Float32 CSharp Pass的隐藏字段

### Requirement: Standard Local Pipeline 必须保持唯一正式单机执行链

当前可运行 Local组合 MUST只安装 `LocalInputIngressPass -> LocalSingleStepSchedulePass -> Float32ProgramEvaluatePass -> Float32WorldResolveBatchPass -> Float32ProgramFinalizePass -> LocalImmediateOutputPass`。它 MUST保持现有 stable Actor order、一次 World ResolveBatch、Character/World state原子性、EventId顺序和 immediate Publish语义，同时 MUST不创建 endpoint、history、correction、restore或 replay。旧固定 `SimulationSessionRuntime` 与 `LocalSimulationDriver` MUST删除，MUST不作为兼容路径保留。

#### Scenario: Corin 单机运行

- **WHEN** Corin Local composition进入 Active并收到一个 LocalLogicTick
- **THEN** Standard Local Pipeline MUST执行一个 SimulationTick和一个 World batch
- **AND** 新外部 EventId MUST由 Local Egress Pass生成 Publish后交给唯一 Committer

### Requirement: Pipeline 失败必须保持外层事务原子

Execution Backend MUST在一个 outer LogicTick内使用 working Character/World/Pipeline state执行全部 restore和内部 step。任一 Pass、product、Kernel、Solver、Finalize、snapshot或 output disposition失败时，MUST不发布该 outer Tick的 working state或外部副作用；若 Solver已接触实际 world body，MUST通过正式 restore/reconstruct合同恢复。Committer在 state publish后失败时 Session MUST fail-stop，MUST不伪造已触发副作用的回滚。

#### Scenario: 第二个 Replay Step 失败

- **WHEN** Restore 100后 Replay 101成功但 Replay 102的 WorldResult identity错误
- **THEN** Backend MUST拒绝整个 outer transaction并恢复 outer Tick前正式 state
- **AND** Replay 101的 Presentation或 Network输出 MUST不被提交

### Requirement: Completed Step必须发布唯一Canonical State Candidate

Float32与Fixed Pipeline working state MUST直接持有当前immutable `SimulationWorldStateSet`的canonical引用。每个completed simulation step MUST在Actor result与World result确定后只构造一个next candidate；BeginSimulationStep MUST发布当前引用，ApplyCompletedStep MUST替换为该candidate引用，后续step MUST直接消费它，最终StateStore publish MUST接收同一实例。Pipeline MUST不通过`ToStateSet`、重复Actor roster排序、`FreezeActors`或等价包装重建同一状态。Snapshot与StateHash仍只在execution plan明确要求时创建独立持久数据。

#### Scenario: Local单Step完成

- **WHEN** Local Pipeline完成一个SimulationStep
- **THEN** CompleteStep MUST构造一个next state candidate
- **AND** working apply与StateStore publish MUST复用该candidate实例

#### Scenario: 一个Outer Transaction包含多个Step

- **WHEN** Prediction、Rollback replay或其它合法schedule在同一outer transaction产生多个step
- **THEN** 每个step MUST只构造自己的一个candidate
- **AND** 下一step MUST直接以前一candidate为输入，不得重新freeze相同Actor roster

#### Scenario: Restore应用后失败

- **WHEN** restore candidate已准备但后续participant validation失败
- **THEN** working state MUST原子恢复outer transaction开始前的canonical引用
- **AND** MUST不留下Character与World来自不同candidate的混合状态

## MODIFIED Requirements

### Requirement: Pipeline 必须以四阶段和固定 Commit 边界执行

Pipeline schema MUST只定义 Ingress、Schedule、Step 与 Egress 四个顶层阶段。Ingress MUST只产生 source/input/ingress 产品；Schedule MUST恰有一个 `SimulationSessionExecutionPlan` producer；Step Pass MUST对 plan 中每个内部 SimulationStep 执行；Egress MUST只消费 step result并生成 snapshot/history/hash/source output 与 EventId disposition。最终 state publish 与 external Commit MUST由 Execution Backend 的固定事务边界拥有，不得实现为可替换 Pass。

#### Scenario: Local 外层 Tick

- **WHEN** GameplayTickSystem 向 Local runtime handle提交一个 LocalLogicTick
- **THEN** Pipeline MUST按 Ingress、Schedule、一个 Step sequence和 Egress执行
- **AND** 只有全部阶段校验成功后 Backend MAY原子发布 state并调用 Committer

#### Scenario: Pass 尝试跨阶段越权

- **WHEN** Ingress Pass尝试直接替换 Character state或 Egress Pass尝试重新执行 角色玩法 operation
- **THEN** phase contract或 product ownership校验 MUST拒绝该组合
- **AND** MUST不通过万能 Context 暴露越权写入口


### Requirement: Pipeline Compiler 必须在 Active 前完成完整兼容校验

Pipeline Compiler MUST在 Runtime创建前校验 phase/order、Schedule唯一性、product producer/consumer、依赖环、Pass factory/version、Source port、Gameplay Runtime ABI、Execution Backend semantic version、Solver capability、Replay/Restore requirement和 state ownership。编译 MUST产生稳定 PipelineHash和不可变 plan；unknown Pass、unknown product、缺失 factory或 unsupported capability MUST明确失败，不得跳过、替换或降级。

#### Scenario: Rollback Pass 配置到 Unity Local 组合

- **WHEN** Pipeline Pass要求 DeterministicReplay和 Snapshotable Solver，但 composition使用 Float32 Local Gameplay Runtime与 Unity CharacterController Solver
- **THEN** Pipeline compile MUST在首 Tick前失败并列出缺失能力
- **AND** MUST不删除 Rollback Pass或改用 Local单步执行


### Requirement: Program、Pipeline 与 Backend 身份必须相互独立并共同锁定

GameplayContentHash MUST只表示 Numeric Target 角色玩法，MUST不包含 Pipeline、Source、Backend、Solver或 Network Model。Active Session、Snapshot、diagnostics和后续网络 handshake MUST另外锁定 PipelineId/Revision/PipelineHash、BackendId/semantic version、SourceId和 Solver identity。同一 角色玩法 MAY由多个合法 Pipeline使用，但不同 PipelineHash或 Backend semantic version的 Session snapshot MUST不可互换。

#### Scenario: 同一 Corin Program 用于 Local 与 Prediction

- **WHEN** Local Pipeline和 ServerAuthoritative Prediction Pipeline使用同一 Float32 Corin Program
- **THEN** 两者 GameplayContentHash MAY相同
- **AND** 两者 Session composition hash和 PipelineHash MUST不同


### Requirement: 普通扩展必须使用 Pass，完整执行技术替换必须使用 Backend

新增 input validation、correction、history、replay scheduling、hash、snapshot export或其它能由四阶段产品合同表达的处理 MUST作为正式 Pass实现并复用已安装 Backend。需要更换状态布局、执行技术或 Pipeline执行机制的实现 MAY提供新的 Execution Backend，但该 Backend MUST消费 versioned Pipeline descriptor、声明支持的 Gameplay Runtime ABI与 semantic version，并返回同一 numeric-neutral runtime handle。Network Model MUST不复制 Common Host、Commit事务或 BTSMTL业务 evaluator来伪装 Backend。

#### Scenario: 增加 Lag Compensation Pass

- **WHEN** ServerAuthoritative 模型增加正式 world query/rewind产品合同可表达的 lag compensation
- **THEN** 模型 MAY在自己的 Pipeline中显式增加对应 Pass
- **AND** MUST不修改 CharacterPipelineHost或创建第二 runtime handle

#### Scenario: 增加 ECS 执行实现

- **WHEN** 第三方需要使用不同状态布局和 ECS执行 Pipeline
- **THEN** 它 MUST提供新的 Execution Backend和匹配 Gameplay Runtime ABI
- **AND** MUST不把 ECS状态塞进 Float32 CSharp Pass的隐藏字段


### Requirement: Standard Local Pipeline 必须保持唯一正式单机执行链

当前可运行Local组合 MUST只安装`LocalInputIngressPass -> LocalSingleStepSchedulePass -> Float32ProgramEvaluatePass -> Float32WorldResolveBatchPass -> Float32ProgramFinalizePass -> LocalImmediateOutputPass`。`LocalInputIngressPass` MUST提升为唯一Local Control Input Ingress和`CanonicalInputBatch`唯一writer：它读取锁定角色玩法 roster、Prepared Control Source roster与上一轮committed Actor Observation，按稳定ActorId准备Player、Neutral与AI输入，验证完整roster后一次发布batch。拥有AI State时，该Ingress MUST作为正式Pipeline state participant捕获checkpoint、canonical state与hash，并随outer transaction恢复或提交candidate AI State。Pipeline MUST不增加AI专用Ingress、第二input writer、endpoint、history、correction、restore schedule或replay；旧固定`SimulationSessionRuntime`与`LocalSimulationDriver` MUST不恢复。

#### Scenario: 玩家与AI Actor单机运行

- **WHEN** Standard Local composition包含一个Player Control Source和一个AI Control Source并收到LocalLogicTick
- **THEN** LocalInputIngressPass MUST从同一committed Observation准备两个Actor输入并写入一个CanonicalInputBatch
- **AND** 两个Character 角色玩法 MUST进入一个SimulationTick和一个World ResolveBatch

#### Scenario: AI输入准备后Character执行失败

- **WHEN** AI已经产生candidate state和prepared input但同一outer Tick的Character Evaluate或WorldSolver失败
- **THEN** Backend MUST恢复Tick前AI、Character与World state
- **AND** LocalImmediateOutputPass与Committer MUST不发布该Tick结果


## ADDED Requirements

### Requirement: 网络Pass必须保留并调用角色领域执行接口

Pipeline MUST保留 Ingress、Schedule、Step、Egress 和原子 Commit、Product 读写、能力校验及状态 participant。Step 中 Evaluate／WorldResolveBatch／Finalize MUST分别调用角色领域运行、批量世界求解和统一状态提交，不得改成绕过 Pass 的固定 C# 网络循环。恢复和重放 MUST复用同一执行接口。

#### Scenario: Prediction执行纠正后的重放
- **WHEN** Schedule 产生恢复和多个有序重放 Step
- **THEN** 原 Pipeline MUST按原阶段调用领域运行接口并按 EventId disposition 提交结果，不得直接执行 Pose 或网络消息中的代码

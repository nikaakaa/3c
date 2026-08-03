# gameplay-simulation-pipeline Specification

## MODIFIED Requirements

### Requirement: Standard Local Pipeline 必须保持唯一正式单机执行链

普通 Local 运行 MUST 继续通过 Standard Local Pipeline 的正式 Ingress、Schedule、Step、Egress 与 Backend outer transaction 执行。Float32 Standard Local Pipeline MUST保持`LocalInputIngressPass -> LocalSingleStepSchedulePass -> Float32ProgramEvaluatePass -> Float32WorldResolveBatchPass -> Float32ProgramFinalizePass -> LocalImmediateOutputPass`作为唯一正式单机执行链。`LocalInputIngressPass` MUST作为唯一Local Control Input Ingress和`CanonicalInputBatch`唯一writer：它读取锁定Program roster、Prepared Control Source roster与上一轮committed Actor Observation，按稳定ActorId准备Player、Neutral与AI输入，验证完整roster后一次发布batch。拥有AI State时，该Ingress MUST作为正式Pipeline state participant捕获checkpoint、canonical state与hash，并随outer transaction恢复或提交candidate AI State。Pipeline MUST不增加AI专用Ingress、第二input writer、endpoint、correction runner或旧固定`SimulationSessionRuntime`与`LocalSimulationDriver`。

Local Fixed 调试录制与回放 MAY 作为 Standard Fixed Local Pipeline 的正式 debug capability 安装，但 MUST 使用同一 Session Source、同一 Runtime Handle、同一 Schedule/ExecutionPlan/Backend transaction和同一 Committer。未显式开启 recording 时，debug history MUST不捕获 per-tick replay snapshot。开启 recording 后，debug history、checkpoint、restore directive 与 replay steps MUST 由显式 Source port、Schedule product、Egress/history product和 SnapshotParticipant 声明，不得通过隐藏字段、Diagnostics Capture、Transform 或 Animation workspace 实现。

#### Scenario: 玩家与AI Actor单机运行

- **WHEN** Standard Local composition包含一个Player Control Source和一个AI Control Source并收到LocalLogicTick
- **THEN** LocalInputIngressPass MUST从同一committed Observation准备两个Actor输入并写入一个CanonicalInputBatch
- **AND** 两个Character Program MUST进入一个SimulationTick和一个World ResolveBatch

#### Scenario: AI输入准备后Character执行失败

- **WHEN** AI已经产生candidate state和prepared input但同一outer Tick的Character Evaluate或WorldSolver失败
- **THEN** Backend MUST恢复Tick前AI、Character与World state
- **AND** LocalImmediateOutputPass与Committer MUST不发布该Tick结果

#### Scenario: Local Fixed 开启调试录制

- **WHEN** 作者对 Active Local Fixed Session 显式开始 recording
- **THEN** Standard Fixed Local Pipeline MUST 通过正式 debug history product 记录 canonical input、hash 和 checkpoint
- **AND** 该记录 MUST 跟随 outer transaction commit 成功后发布
- **AND** MUST 不保存骨骼 Pose 或 Presentation workspace

#### Scenario: Local Fixed 执行调试回放

- **WHEN** Debug Schedule 从 Tick 100 checkpoint 回放到 Tick 130
- **THEN** ExecutionPlan MUST 包含合法 restore directive 和 ordered Replay steps
- **AND** Backend MUST 在一个 outer transaction 内执行
- **AND** Committer MUST 只提交最终分支

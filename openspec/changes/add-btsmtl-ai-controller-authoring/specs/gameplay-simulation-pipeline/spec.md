## MODIFIED Requirements

### Requirement: Standard Local Pipeline 必须保持唯一正式单机执行链

当前可运行Local组合 MUST只安装`LocalInputIngressPass -> LocalSingleStepSchedulePass -> Float32ProgramEvaluatePass -> Float32WorldResolveBatchPass -> Float32ProgramFinalizePass -> LocalImmediateOutputPass`。`LocalInputIngressPass` MUST提升为唯一Local Control Input Ingress和`CanonicalInputBatch`唯一writer：它读取锁定Program roster、Prepared Control Source roster与上一轮committed Actor Observation，按稳定ActorId准备Player、Neutral与AI输入，验证完整roster后一次发布batch。拥有AI State时，该Ingress MUST作为正式Pipeline state participant捕获checkpoint、canonical state与hash，并随outer transaction恢复或提交candidate AI State。Pipeline MUST不增加AI专用Ingress、第二input writer、endpoint、history、correction、restore schedule或replay；旧固定`SimulationSessionRuntime`与`LocalSimulationDriver` MUST不恢复。

#### Scenario: 玩家与AI Actor单机运行

- **WHEN** Standard Local composition包含一个Player Control Source和一个AI Control Source并收到LocalLogicTick
- **THEN** LocalInputIngressPass MUST从同一committed Observation准备两个Actor输入并写入一个CanonicalInputBatch
- **AND** 两个Character Program MUST进入一个SimulationTick和一个World ResolveBatch

#### Scenario: AI输入准备后Character执行失败

- **WHEN** AI已经产生candidate state和prepared input但同一outer Tick的Character Evaluate或WorldSolver失败
- **THEN** Backend MUST恢复Tick前AI、Character与World state
- **AND** LocalImmediateOutputPass与Committer MUST不发布该Tick结果

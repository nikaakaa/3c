## MODIFIED Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST只加载并校验 CharacterPipelineDefinition对应的 CharacterSimulationProgramAsset与 Projection，建立显式 ActorId、World body binding、可选 local input、Presentation/output ports和 diagnostics metadata，并向显式 SimulationSessionHost提供不可变 Actor registration。CharacterPipelineHost MUST不创建 ProgramCatalog、Session Source、WorldSolver、Program Runtime、Execution Backend、Pipeline Runtime、Snapshot codec、Committer aggregate或 Logic target，也 MUST不选择 Network Model或 Pipeline。

#### Scenario: 注册单机 Corin

- **WHEN** Sandbox中的 Corin CharacterPipelineHost启用
- **THEN** MUST向显式 SimulationSessionHost提交一个 Actor registration
- **AND** Local Source、标准 Pipeline、Float32 Backend与 Unity Solver MUST只由 Session composition创建

### Requirement: Character ActorId 必须由 Host 单点装配

每个 CharacterPipelineHost MUST显式提供唯一非空 ActorId，并在 Actor registration中绑定 ProgramId、ProgramHash、LayoutHash、World body与 Presentation identity。SimulationSessionHost MUST在 Active前验证完整 roster中 ActorId唯一且 binding精确匹配；Program operation、Projection、Solver、Pipeline Pass、Session Source与 Network Model MUST不生成替代 identity，Active后 MUST不修改 ActorId或 roster binding。

#### Scenario: Local Corin 注册

- **WHEN** Corin registration加入 Local Session launch plan
- **THEN** roster MUST使用该显式 ActorId、Program binding与 World body binding
- **AND** Session Host MUST不按 GameObject instance id、名称或数组 index生成 ActorId

### Requirement: Pipeline 输出分为 strict、presentation 和 sync facts

SimulationStepResult MUST类型化分离 Gameplay facts、body/world observations、presentation commands、model-neutral SyncDomain facts与 Trace records。Egress Pass与 Committer MAY按正式产品/端口消费，MUST不让 Presentation output反向改变 Gameplay state，也 MUST不把 packet或 Pipeline私有状态写入结果。

#### Scenario: Attack Tick 输出

- **WHEN** Attack产生 Window、Motion和 animation command
- **THEN** Step Finalize MUST以独立 typed channels保存并共享同一 Event identity
- **AND** Egress MUST只决定外部 EventId disposition

### Requirement: Simulation Session 必须作为显式 diagnostics target

每个 Active Simulation Session与其 Actor roster MUST注册明确 diagnostics target/session identity，并提供 Program revision、Source Map、BackendId、PipelineId/Hash、compiled Pass order、SourceId、Solver identity、默认关闭的 Live/Capture store和只读 metadata。Editor MUST不持有 runtime Graph、mutable Character/World/Pipeline state、Pass runtime或 Solver object。

#### Scenario: Local Session 激活

- **WHEN** Corin Session完成创建
- **THEN** diagnostics registry MUST注册 Session/Actor target、ProgramHash与 PipelineHash
- **AND** MUST能显示当前标准 Local Pass顺序

### Requirement: Program Operation Execution Context 必须是唯一角色逻辑上下文

Kernel MUST为 operation提供只读 Program、SimulationTick、Actor input、SimulationIngress、Character state accessor、上一 body observation、typed output writer和 Source Map identity。Operation MUST不获得 Host、GameObject、Session Source、Pipeline Runtime/Pass、Execution Backend、WorldSolver、Presentation或 model session reference。

#### Scenario: Condition operation 读取输入与 Blackboard

- **WHEN** operation求值移动状态条件
- **THEN** MUST只通过 execution context的 portable input/state accessor读取

### Requirement: Simulation Session 必须是 GameplayTickSystem 的 logic target

GameplayTickSystem MUST只注册 SimulationSessionHost/runtime handle作为同一 Session的 Input/Logic target，不得为每个 Character、Pass、Session Source或 Network Model注册独立 LogicTick。Character Presentation target MAY按 Actor独立注册，但 MUST只消费当前 Session Committer发布的 samples/commands，并由 Session composition统一激活和释放。

#### Scenario: Session 包含两个 Actor

- **WHEN** fixed LocalLogicTick到达
- **THEN** GameplayTickSystem MUST只推进一次 Session runtime handle
- **AND** 两个 Character Presentation runtime MUST不各自推进 Gameplay Kernel或 Pipeline

## REMOVED Requirements

### Requirement: Program 与 SimulationSessionRuntime 是纯 C# 运行时主体

**Reason**: 固定 `SimulationSessionRuntime` 将单 Tick Driver、Evaluate、World Solve、Finalize和 OutputPlan写死为一个不可组合实现，无法表达正式多步 replay或第三方 Pipeline。

**Migration**: 由 portable Program Runtime、compiled Pipeline plan、Execution Backend与 numeric-neutral runtime handle共同形成纯 C#运行主体；旧类型删除。

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

**Reason**: 该 requirement把具体 `Driver TickPlan -> Evaluate -> Resolve -> Finalize -> BuildOutputPlan` 顺序固化为唯一 Pipeline，阻止 correction、rollback和特殊 Pass组合。

**Migration**: 由新的 `gameplay-simulation-pipeline` 能力定义 Ingress、Schedule、Step、Egress和固定 Commit；标准 Local Pipeline保持原有业务顺序。

## ADDED Requirements

### Requirement: Program Runtime 与 Execution Backend 必须形成唯一纯 CSharp 运行主体

正式 Character gameplay runtime MUST由 portable Program Runtime、compiled Pipeline plan、Execution Backend runtime handle、Character/World/Pipeline state与明确 ports构成。可变 Gameplay state MUST不隐藏在 CharacterPipeline stage、RunnableNode clone、Timeline scheduler、GraphContext、Pass Definition或 Unity component内。Unity Host/Adapter MUST留在 composition boundary。

#### Scenario: 普通 DotNet Host 编译 Runtime

- **WHEN** 后续普通 .NET Host引用 Program Runtime与兼容 Execution Backend源码
- **THEN** MUST不需要 CharacterPipelineHost、ScriptableObject或 UnityEngine执行 Gameplay Program
- **AND** MUST使用同一 Pipeline descriptor与 Session transaction合同

### Requirement: Character Pipeline 必须通过可组合 Session Pipeline 执行

正式逻辑链 MUST收口为 `Ingress Passes -> one Schedule plan -> zero or more Step sequences -> Egress Passes -> atomic state publish -> Committer`。标准 Step Pass MUST调用唯一 Program/Kernel Evaluate、WorldSolver ResolveBatch与 Kernel Finalize；Graph、StateMachine、Timeline、Action、Effect和 Motion resolve MUST属于 Program/Kernel，world mutation MUST属于 WorldSolver，Network Model与 Presentation MUST位于正式 Source/Egress/Commit端口。Egress disposition MUST不决定 staged Gameplay state是否生效。

#### Scenario: 一个 Local Tick

- **WHEN** Standard Local Pipeline推进一个 SimulationTick
- **THEN** Step Pass MUST完成全部 Actor logic和一个 world batch
- **AND** Committer MUST只在 outer Pipeline transaction原子成功后处理副作用

#### Scenario: 一次纠偏执行多个内部 Tick

- **WHEN** 后续 Prediction Pipeline生成 restore和多个 replay/current step
- **THEN** 每个 step MUST复用相同 Kernel/Solver/Finalize Pass合同
- **AND** Replay中间输出 MUST不绕过 Egress与 Commit事务

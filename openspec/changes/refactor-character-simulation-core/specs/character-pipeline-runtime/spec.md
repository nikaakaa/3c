# character-pipeline-runtime Specification

## MODIFIED Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST只负责加载与 CharacterPipelineDefinition source revision 匹配的 CharacterSimulationProgram 和 CharacterPresentationProjection，并显式装配 SimulationState、Input Adapter、Simulation Driver、World Solver、Committer 和 diagnostics target。Host MUST不在运行时 clone authoring Graph/Timeline 或自动查找默认组件。

#### Scenario: 创建单机 Corin

- **WHEN** Host 使用 LocalSimulationDriver 创建 Corin
- **THEN** MUST显式绑定 Program、State、Unity Solver、Projection 和 Committer

### Requirement: CharacterPipeline 是纯 C# 运行时主体

Character runtime facade MUST由纯 C# SimulationKernel、SimulationState 和明确 ports 构成。可变 gameplay state MUST不隐藏在 CharacterPipeline stage、RunnableNode clone、Timeline scheduler 或 Unity component 内。Unity Host/Adapter MUST留在装配边界。

#### Scenario: DotNet 编译 Core

- **WHEN** 普通 .NET csproj 编译 Program/State/Kernel source set
- **THEN** MUST不需要 CharacterPipelineHost 或 UnityEngine

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

系统 MUST收口为 `Input Adapter -> Driver -> SimulationKernel -> World Solver -> SimulationOutput -> Committer`。Graph、StateMachine、Timeline、Action、Effect 和 Motion resolve MUST属于 Kernel 内部稳定执行顺序；model adapter 和 Presentation MUST留在 Kernel 外。

#### Scenario: 一个 Local Tick

- **WHEN** Local Driver 推进一个 SimulationTick
- **THEN** Kernel MUST完成逻辑与运动结算后一次输出事实和 commands
- **AND** Committer MUST在 Kernel 返回后处理副作用

### Requirement: CharacterPipeline 是 GameplayTickSystem 的 tick target

Character Host MUST继续作为 GameplayTickSystem 的 target，但 target callback MUST只把固定 Tick context 交给当前 Simulation Driver。Host MUST不自行重复推进 Graph、Timeline 或 Motion stage。

#### Scenario: GameplayTickSystem 触发逻辑 Tick

- **WHEN** target 收到 fixed logic tick
- **THEN** MUST仅调用当前 Driver 的 Tick 入口

### Requirement: Pipeline 输出分为 strict、presentation 和 sync facts

SimulationOutput MUST将 gameplay state/facts、presentation commands 和 model-neutral sync facts 以类型化 channel 暴露。Kernel MUST不从 presentation 反向推导 gameplay，model adapter MUST不要求第二套 NetworkSend/Receive stage 双写相同事实。

#### Scenario: Attack Window 产生

- **WHEN** Timeline operation 产生 ActionWindow
- **THEN** Window MUST先成为 SimulationOutput typed fact
- **AND** Presentation 与 model adapter MAY从各自端口消费

## REMOVED Requirements

### Requirement: CharacterPipeline 支持混合架构 authority mode

**Reason**：LocalSolver、ExternalPose 和 None 是不同 Driver/actor binding 策略，不应由 CharacterPipeline enum 分支 Kernel 执行。

**Migration**：删除 CharacterMotionAuthority，Session/Driver 显式装配 Actor 的模拟或表现采样路径。

#### Scenario: 迁移单机 Actor

- **WHEN** Sandbox 创建本地 Corin
- **THEN** MUST装配 Local Driver 与 Unity Solver
- **AND** MUST不使用 authority enum

### Requirement: NetworkStage 是正式边界但不实现真实 transport

**Reason**：公共 Character NetworkStage 会与 model-owned Driver/adapter 双写，并迫使不同模型共享 correction/history 语义。

**Migration**：Kernel 只暴露 portable input/output ports；packet、history、endpoint 和 transport 继续归具体 Network Model。

#### Scenario: 迁移 NetworkStage

- **WHEN** 现有 ServerAuthoritative adapter 接入新 core
- **THEN** MUST直接消费/生成正式 simulation ports
- **AND** MUST删除 Character NetworkStage 双写

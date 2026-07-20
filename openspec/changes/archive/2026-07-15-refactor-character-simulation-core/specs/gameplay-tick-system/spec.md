# gameplay-tick-system Specification

## MODIFIED Requirements

### Requirement: GameplayTickSystem 必须由 TEngine frame source 驱动

GameplayTickSystem MUST继续由 TEngine RootModule、UpdateDriver 或正式 runtime entry 提供 frame source。TEngine MUST不直接 Tick Program operation、单个 Character、WorldSolver、Network Model 或 Presentation adapter。Simulation Session logic target 与 Presentation target MUST通过 GameplayTickSystem 调度。

#### Scenario: TEngine 驱动 Local Session

- **WHEN** 项目 runtime 已通过 TEngine 启动
- **THEN** FrameUpdate MUST推进 GameplayTickSystem accumulator
- **AND** fixed Tick MUST调用唯一 Local Simulation Session target
- **AND** FrameLateUpdate MUST调用 Presentation target

#### Scenario: 缺少 TickSystem

- **WHEN** Simulation Session Host 启用但 GameplayTickSystem 未初始化
- **THEN** Host MUST报告启动顺序错误
- **AND** MUST不创建私有 runner

### Requirement: Gameplay Tick 系统必须区分本地逻辑 tick、表现帧和服务端 tick

GameplayTickSystem MUST继续区分 fixed LocalLogicTick、PresentationFrame 和网络输入中的 ServerTick。SimulationTick MUST是某个 SimulationSessionRuntime 内的执行 identity，由 Driver 显式映射 source clock；系统 MUST不假定 LocalLogicTick、ServerTick 与 SimulationTick 数值相同，RenderFrame MUST不产生 SimulationTick。

#### Scenario: Local Simulation

- **WHEN** GameplayTickSystem 产生一个 fixed LocalLogicTick
- **THEN** Local Driver MUST将其映射为当前 Local Session 的下一个 SimulationTick
- **AND** Program/Kernel MUST不读取 Unity frame time 或 ServerTick

### Requirement: GameplayTickSystem 必须通过 target 接口调度业务对象

GameplayTickSystem MUST以 Simulation Session 作为 logic target，而不是为同一 Session 中每个 Character 分别注册旧 Pipeline LogicTick。Target MUST将 fixed tick context 交给当前 Driver/SessionRuntime，SessionRuntime MUST统一推进 actor roster 与一个 world batch。

#### Scenario: 双 Actor Session 被调度

- **WHEN** 同一 Session roster 包含 ActorA 与 ActorB
- **THEN** GameplayTickSystem MUST只调用一次 Session logic target
- **AND** SessionRuntime MUST按 stable ActorId 顺序处理两个 Actor

### Requirement: GameplayTickSystem 必须每表现帧推进 PresentationFrame

PresentationFrame MUST继续以 render/presentation delta 推进 visual interpolation、Timeline visual sampling、Animancer fade、Camera 和 committed command lifecycle。PresentationFrame MUST不调用 Kernel Evaluate/Finalize、WorldSolver.ResolveBatch 或修改 Character/World state。

#### Scenario: 高渲染帧率下的表现帧

- **WHEN** 两个 SimulationTick 之间发生多个 PresentationFrame
- **THEN** 表现 MUST继续插值和淡入淡出
- **AND** SessionRuntime MUST不被额外推进

### Requirement: 服务端 tick 必须只通过网络输入进入角色管线

ServerTick MUST只存在于具体 Network Model packet/Driver state 或被转换后的 SimulationIngress/restore provenance 中。GameplayTickSystem MUST不自增 ServerTick，Local Driver MUST不从 LocalLogicTick 推导 ServerTick，Kernel MUST不读取 ServerTick 作为 Program 时间。

#### Scenario: 后续模型收到权威 observation

- **WHEN** model endpoint 收到携带 ServerTick 的消息
- **THEN** model Driver MUST在自己的 history 中保存 ServerTick
- **AND** 只把 model-neutral ingress、restore request 或 OutputPlan metadata交给 Core

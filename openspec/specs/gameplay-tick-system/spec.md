# gameplay-tick-system Specification

## Purpose
定义 gameplay 层统一 tick 系统：`GameplayTickSystem` 区分 `LocalLogicTick`、`RenderFrame` 和模型输入中的 `ServerTick`，并分别通过 render input、logic 和 presentation target 接口调度正式 Session 与表现消费者。
## Requirements
### Requirement: Gameplay Tick 系统必须区分本地逻辑 tick、表现帧和服务端 tick

GameplayTickSystem MUST继续区分 fixed LocalLogicTick、PresentationFrame和网络 Source中的 ServerTick。SimulationTick MUST是 Session ExecutionPlan内部 step identity，由唯一 Schedule Pass显式映射 source clock；系统 MUST不假定 LocalLogicTick、ServerTick与 SimulationTick数值相同，PresentationFrame MUST不产生 SimulationTick。一次 LocalLogicTick MAY对应零个、一个或多个内部 SimulationTick。DeterministicRollback Source/Schedule Pass MUST在 model内将 outer fixed tick映射为 predicted/confirmed SimulationTick并标记 forward/replay step；ServerTick MUST不被伪装为 Rollback canonical tick或直接写入 Kernel。

#### Scenario: Local Simulation

- **WHEN** GameplayTickSystem产生一个 fixed LocalLogicTick
- **THEN** Local Schedule Pass MUST将其映射为当前 Local Session的下一个 SimulationTick
- **AND** Program/Kernel MUST不读取 Unity frame time或 ServerTick

#### Scenario: Replay 一段 Tick

- **WHEN** Rollback Pipeline重演 Tick T到 N
- **THEN** MUST使用 model-owned SimulationTick/step context通过 compiled Pipeline调用 Kernel
- **AND** GameplayTickSystem MUST不伪造多个 render frame

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

### Requirement: GameplayTickSystem 必须使用固定步长推进本地逻辑

系统 MUST使用固定步长 accumulator推进 `LocalLogicTick`。本地逻辑 tick rate和 tick time source MUST来自正式 `GameplayTickSettings`或等价配置。系统 MUST限制单帧 catch-up tick数，避免卡顿后无限补帧。Rollback forward/replay MUST使用与 Program TickRate匹配的固定 step；Source/Schedule Pass MUST不以 render delta、Unity `Time.deltaTime`、网络到包时间或自适应浮点 step推进 deterministic gameplay。

#### Scenario: 一帧内推进多个本地逻辑 tick

- **WHEN** 当前帧配置 time source对应的 delta大于本地 fixed delta
- **THEN** `GameplayTickSystem` MUST按 fixed delta循环推进一个或多个 `LocalLogicTick`
- **AND** 每次推进 MUST调用已注册 `IGameplayLogicTickTarget`的 `LogicTick`
- **AND** 每帧推进次数 MUST不超过正式配置的最大 catch-up tick数

#### Scenario: 计算表现插值

- **WHEN** 本帧本地逻辑 tick推进完成
- **THEN** `GameplayTickSystem` MUST根据剩余 accumulator计算 interpolation alpha
- **AND** `PresentationFrame` MUST能读取该 alpha

#### Scenario: 网络包在表现帧中到达

- **WHEN** canonical bundle 在任意 render frame 到达
- **THEN** Rollback Pipeline MUST在正式 fixed simulation boundary处理 forward/rollback

### Requirement: GameplayTickSystem 必须通过 target 接口调度业务对象

GameplayTickSystem MUST以 SimulationSessionHost/runtime handle作为每个 Session唯一 Input/Logic target，而不是为同一 Session中的 Character、Pass、Session Source、Endpoint或 Network Model分别注册 LogicTick。Target在 Preparing状态 MAY推进正式 preparation但 MUST不执行 Program；进入 Active后 MUST将每个 source tick只交给 runtime handle一次，Pipeline Runtime再按 compiled ExecutionPlan推进 roster、内部 step和 world batch。

#### Scenario: 双 Actor Session 被调度

- **WHEN** 同一 Session roster包含 ActorA与 ActorB
- **THEN** GameplayTickSystem MUST只调用一次 Session logic target
- **AND** 每个内部 Step MUST按 stable ActorId顺序处理两个 Actor

#### Scenario: Network Model 尚在 Preparing

- **WHEN** Session preparation正在等待 endpoint handshake、launch roster或 Pipeline factory
- **THEN** GameplayTickSystem MAY推进一次 preparation step
- **AND** MUST不创建 SimulationTick、执行 Kernel或注册第二个 Model/Pipeline runner

### Requirement: GameplayTickSettings 必须显式配置 tick time source

系统 MUST 通过正式配置声明 `GameplayTickSystem` 的 tick time source。系统 MUST NOT 在 `GameplayTickSystem` 内硬编码使用 scaled delta 或 unscaled delta。默认 gameplay 配置 SHOULD 使用 scaled delta；调试、暂停外模拟或工具模式 MAY 显式使用 unscaled delta。

#### Scenario: 普通 gameplay 使用 scaled delta

- **WHEN** `GameplayTickSettings` 的 time source 是 `Scaled`
- **THEN** `GameplayTickSystem.FrameUpdate` MUST 使用 scaled delta 累积本地逻辑 tick
- **AND** `Time.timeScale` 或 TEngine scaled delta 变化 MUST 能影响本地角色逻辑推进

#### Scenario: 调试模式使用 unscaled delta

- **WHEN** `GameplayTickSettings` 的 time source 是 `Unscaled`
- **THEN** `GameplayTickSystem.FrameUpdate` MUST 使用 unscaled delta 累积本地逻辑 tick
- **AND** 该选择 MUST 来自正式 settings，不得在调用点临时绕过 tick system

### Requirement: GameplayTickSystem 必须每表现帧推进 PresentationFrame

PresentationFrame MUST继续以 render/presentation delta推进 visual interpolation、Timeline visual sampling、Animancer fade、Camera和 committed command lifecycle。Rollback replay MUST只产生 EventId output replacement，MUST不直接把 PresentationFrame回卷或用 logic tick代替 render delta。PresentationFrame MUST不调用 Kernel Evaluate/Finalize、WorldSolver.ResolveBatch或修改 Character/World state。

#### Scenario: 高渲染帧率下的表现帧

- **WHEN** 两个 SimulationTick之间发生多个 PresentationFrame
- **THEN** 表现 MUST继续插值和淡入淡出
- **AND** Session runtime handle MUST不被额外推进

#### Scenario: Replay 后替换动画选择

- **WHEN** Output Disposition Pass产生 EventId replacement
- **THEN** PresentationFrame MUST从当前视觉状态处理新 command
- **AND** MUST继续以 presentation delta 推进 Animancer

### Requirement: 服务端 tick 必须只通过网络输入进入角色管线

ServerTick MUST只存在于具体 Network Model Source/packet/history或被转换后的 Pipeline source product、ExecutionPlan provenance中。GameplayTickSystem MUST不自增 ServerTick，Local Source/Schedule Pass MUST不从 LocalLogicTick推导 ServerTick，Kernel MUST不读取 ServerTick作为 Program时间。

#### Scenario: 后续模型收到权威 observation

- **WHEN** Model Endpoint收到携带 ServerTick的消息
- **THEN** Model Source MUST在自己的 ExternalSource state中保存 ServerTick
- **AND** 只把 model-neutral ingress、restore directive或 schedule provenance交给 Pipeline

### Requirement: 模型输入命令必须保留 InputSequence 和 LocalLogicTick

具体 Network Model Source/Ingress Pass MUST从 portable CharacterSimulationInput构造模型输入命令，并保留 InputSequence与来源 LocalLogicTick。模型 Endpoint MAY按 20/30Hz flush多个 command，但 flush频率 MUST NOT改变 LocalLogicTick或 Schedule Pass产生的 SimulationTick语义。

#### Scenario: 本地 60Hz 逻辑与 20Hz 网络发送

- **WHEN** Input Adapter/Ingress Pass以 60Hz生成带 InputSequence的 CharacterSimulationInput
- **AND** 网络 peer以 20Hz flush
- **THEN** 每个 command MUST保留自己的 InputSequence与 LocalLogicTick
- **AND** peer MAY在一包中发送多个 command，MUST不把 flush序号当作 SimulationTick


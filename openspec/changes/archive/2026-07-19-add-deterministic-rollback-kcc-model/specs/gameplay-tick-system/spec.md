# gameplay-tick-system Specification

## MODIFIED Requirements

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

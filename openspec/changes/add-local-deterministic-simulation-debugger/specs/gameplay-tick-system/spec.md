# gameplay-tick-system Specification

## MODIFIED Requirements

### Requirement: GameplayTickSystem 必须使用固定步长推进本地逻辑

系统 MUST使用固定步长 accumulator推进 `LocalLogicTick`。本地逻辑 tick rate和 tick time source MUST来自正式 `GameplayTickSettings`或等价配置。系统 MUST限制单帧 catch-up tick数，避免卡顿后无限补帧。Rollback forward/replay与Local Fixed debug replay MUST使用与 Program TickRate匹配的固定 step；Source/Schedule Pass MUST不以 render delta、Unity `Time.deltaTime`、网络到包时间或自适应浮点 step推进 deterministic gameplay。

GameplayTickSystem MUST拥有正式 Debug Drive Policy，用于 Realtime、Paused、ManualStep 与 RatePlayback。Debug Drive Policy MUST只控制 fixed tick admission，不得改变 fixed delta。Paused MUST冻结自动 accumulator 推进；ManualStep MUST按命令数量生成精确 LocalLogicTick；RatePlayback MUST按显式倍率调整 admission rate并继续受最大单帧预算限制。所有模式下，GameplayTickSystem MUST仍只通过已注册 target 接口调度业务对象。

#### Scenario: 一帧内推进多个本地逻辑 tick

- **WHEN** 当前帧配置 time source对应的 delta大于本地 fixed delta
- **THEN** `GameplayTickSystem` MUST按 fixed delta循环推进一个或多个 `LocalLogicTick`
- **AND** 每次推进 MUST调用已注册 `IGameplayLogicTickTarget`的 `LogicTick`
- **AND** 每帧推进次数 MUST不超过正式配置的最大 catch-up tick数

#### Scenario: 暂停后单 Tick 推进

- **WHEN** Debug Drive Policy处于 Paused 且收到 StepOne 命令
- **THEN** 下一次 `FrameUpdate` MUST只推进一个 fixed `LocalLogicTick`
- **AND** 该 Tick MUST使用与实时模式相同的 target 调度和 fixed delta
- **AND** accumulator MUST不因为 render delta 自动补进额外 Tick

#### Scenario: 计算表现插值

- **WHEN** 本帧本地逻辑 tick推进完成
- **THEN** `GameplayTickSystem` MUST根据剩余 accumulator和当前 drive policy计算 interpolation alpha
- **AND** `PresentationFrame` MUST能读取该 alpha

#### Scenario: 网络包在表现帧中到达

- **WHEN** canonical bundle 在任意 render frame 到达
- **THEN** Rollback Pipeline MUST在正式 fixed simulation boundary处理 forward/rollback

### Requirement: GameplayTickSystem 必须每表现帧推进 PresentationFrame

PresentationFrame MUST继续以render/presentation delta或正式 Debug Presentation Clock推进visual interpolation、Timeline visual sampling、显式Player节点clock、Animancer source sampling、Character Pose Graph Plan、FootPlacement world-aware阶段、Camera与committed command lifecycle。Rollback replay与Local Fixed debug replay MUST只产生EventId output replacement或committed stream reset，MUST不直接回卷PresentationFrame或用logic tick代替presentation delta。PresentationFrame MUST不调用Kernel Evaluate/Finalize、Gameplay WorldSolver.ResolveBatch或修改Character/World state。

正式 Debug Presentation Clock MUST支持 `LivePresentation` 与 `LogicLockedPresentation`。`LivePresentation` MUST保持当前 render delta 行为。`LogicLockedPresentation` 在 Paused 时 MUST向 Presentation target 提供 0 presentation delta；ManualStep 成功提交后 MAY提供一个 fixed presentation pulse，用于逐 Tick 观察正式 committed 输出。该 pulse MUST来自 TickSystem context，不得由 Presentation target 私自读取 debug service。

#### Scenario: 高渲染帧率下的表现帧

- **WHEN** 两个SimulationTick之间发生多个PresentationFrame
- **THEN** Body插值、slot淡入淡出、source sampling与Pose Graph输出 MUST连续推进
- **AND** Session runtime handle MUST不被额外推进

#### Scenario: LogicLocked 暂停观察

- **WHEN** Debug Presentation Clock处于 LogicLockedPresentation 且 drive mode 为 Paused
- **THEN** PresentationFrame MUST继续被调用以刷新UI和诊断
- **AND** gameplay presentation delta MUST为 0
- **AND** Presentation target MUST不自行推进 Timeline visual time

#### Scenario: Replay后替换动画选择

- **WHEN** Output Disposition Pass产生FullBodyAction EventId replacement
- **THEN** PresentationFrame MUST从该slot当前视觉结果处理新command
- **AND** MUST继续通过正式 Debug Presentation Clock或 render delta推进唯一Pose Plan及其中显式Player节点

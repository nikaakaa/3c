## ADDED Requirements

### Requirement: Gameplay Tick 系统必须区分本地逻辑 tick、表现帧和服务端 tick

系统 MUST 明确区分 `LocalLogicTick`、`RenderFrame` 和 `ServerTick`。`LocalLogicTick` 表达客户端本地固定步长逻辑推进；`RenderFrame` 表达本地表现帧；`ServerTick` 表达服务端权威快照、确认、拒绝或校正来源。系统 MUST NOT 使用单一 `SimulationTick` 同时表达这三类时间。

#### Scenario: 本地高帧率表现和服务端低频快照并存

- **WHEN** 客户端以 120fps 渲染
- **AND** 本地逻辑以 60Hz 推进
- **AND** 服务端以 20Hz 下发快照
- **THEN** `RenderFrame` MUST 按本地表现帧递增
- **AND** `LocalLogicTick` MUST 按本地固定逻辑步长递增
- **AND** `ServerTick` MUST 只来自服务端或 loopback packet
- **AND** 系统 MUST NOT 把 `RenderFrame` 或 `LocalLogicTick` 写成服务端权威 tick

### Requirement: GameplayTickSystem 必须由 TEngine frame source 驱动

系统 MUST 使用 `GameplayTickSystem` 作为 gameplay 的统一 tick 调度系统。`GameplayTickSystem` MUST 由 TEngine `RootModule`、`UpdateDriver` 或项目正式 runtime 入口提供 frame source。TEngine MUST NOT 直接 tick BTSMTL Graph、Timeline、ActionRuntime、MotionStage、网络 peer 或单个 `CharacterPipeline`。

#### Scenario: TEngine 驱动角色 tick 系统

- **WHEN** 项目 runtime 已通过 TEngine 启动
- **THEN** 正式 bootstrap MUST 将 TEngine frame source 接入 `GameplayTickSystem.FrameUpdate`
- **AND** 正式 bootstrap MUST 将 TEngine late frame source 接入 `GameplayTickSystem.FrameLateUpdate`
- **AND** `CharacterPipeline` MUST 只作为 `IGameplayTickTarget` 被 `GameplayTickSystem` 调度

#### Scenario: 缺少 TickSystem

- **WHEN** `CharacterPipelineHost` 启用但 `GameplayTickSystem` 未初始化
- **THEN** Host MUST 报告正式配置或启动顺序错误
- **AND** 系统 MUST NOT 自动创建 fallback `CharacterPipelineRunner`

### Requirement: GameplayTickSystem 必须使用固定步长推进本地逻辑

系统 MUST 使用固定步长 accumulator 推进 `LocalLogicTick`。本地逻辑 tick rate 和 tick time source MUST 来自正式 `GameplayTickSettings` 或等价配置。系统 MUST 限制单帧 catch-up tick 数，避免卡顿后无限补帧。

#### Scenario: 一帧内推进多个本地逻辑 tick

- **WHEN** 当前帧配置 time source 对应的 delta 大于本地 fixed delta
- **THEN** `GameplayTickSystem` MUST 按 fixed delta 循环推进一个或多个 `LocalLogicTick`
- **AND** 每次推进 MUST 调用已注册 `IGameplayTickTarget` 的 `LogicTick`
- **AND** 每帧推进次数 MUST 不超过正式配置的最大 catch-up tick 数

#### Scenario: 计算表现插值

- **WHEN** 本帧本地逻辑 tick 推进完成
- **THEN** `GameplayTickSystem` MUST 根据剩余 accumulator 计算 interpolation alpha
- **AND** `PresentationFrame` MUST 能读取该 alpha

### Requirement: GameplayTickSystem 必须通过 target 接口调度业务对象

系统 MUST 定义 `IGameplayTickTarget` 或等价接口作为 gameplay tick 消费边界。`GameplayTickSystem` MUST 注册和调度该接口，而不是直接持有 `CharacterPipeline` 专用列表。`CharacterPipeline` MAY 实现该接口成为首个消费者；后续网络本地 peer、投射物、战斗历史或 AI MAY 接入同一个 tick system，但 MUST NOT 创建第二套本地逻辑 tick。

#### Scenario: 角色管线作为 tick target

- **WHEN** `CharacterPipelineHost` 启用
- **THEN** Host 创建的 `CharacterPipeline` MUST 作为 `IGameplayTickTarget` 注册到 `GameplayTickSystem`
- **AND** `GameplayTickSystem` MUST 通过 target 接口调用 `BeginRenderFrame`、`LogicTick` 和 `PresentationFrame`
- **AND** `GameplayTickSystem` MUST NOT 调用 Character 专用 API 才能推进 tick

#### Scenario: 未来网络消费者接入同一 tick

- **WHEN** 后续本地 loopback peer 或 Fantasy adapter 需要按 gameplay tick pump
- **THEN** 该消费者 MUST 复用 `GameplayTickSystem` 的 `LocalLogicTick` 或 hook
- **AND** 系统 MUST NOT 为网络额外自增第二套 local logic tick

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

系统 MUST 在每个本地表现帧推进 `PresentationFrame`。表现帧 MAY 使用 scaled delta、unscaled delta、最近 local logic tick 和 interpolation alpha。表现帧 MUST NOT 创建新的本地逻辑输入、ActionActivationRequest 或 ClientCommand。

#### Scenario: 表现帧高于本地逻辑 tick

- **WHEN** 当前渲染帧没有新的 `LocalLogicTick`
- **THEN** `GameplayTickSystem` 仍 MUST 调用 `PresentationFrame`
- **AND** Presentation MUST 使用最近的 logic snapshot、interpolation alpha 或网络 snapshot buffer 平滑表现
- **AND** Presentation MUST NOT 再次 tick BTSMTL RootTree

### Requirement: 服务端 tick 必须只通过网络输入进入角色管线

系统 MUST 让 `ServerTick` 只通过 `ServerSnapshot`、`Correction`、Action decision 或后续 Fantasy/loopback incoming packet 进入角色管线。`GameplayTickSystem` MUST NOT 自增 `ServerTick`，`CharacterPipeline` MUST NOT 从本地 frame 推导 `ServerTick`。

#### Scenario: 收到服务器校正

- **WHEN** Fantasy adapter 或 loopback peer 收到 correction packet
- **THEN** packet MUST 携带 `ServerTick`
- **AND** packet SHOULD 携带 `InputSequence` 或等价已处理输入身份
- **AND** NetworkReceiveStage MUST 将其作为网络输入缓存
- **AND** 本地 `LocalLogicTick` MUST NOT 因该 packet 被重写

### Requirement: 网络发送 MUST 使用 InputSequence 和 LocalLogicTick

系统 MUST 让本地预测产生的 `ClientCommand` 使用 `InputSequence` 和 `LocalLogicTick` 作为本地身份。系统 MAY 在后续网络 peer 中按 20/30Hz flush 多个 command，但 flush 频率 MUST NOT 改变本地逻辑 tick 语义。

#### Scenario: 本地 60Hz 逻辑与 20Hz 网络发送

- **WHEN** `GameplayTickSystem` 以 60Hz 生成本地 `ClientCommand`
- **AND** 网络 peer 以 20Hz flush
- **THEN** 每个 command MUST 保留自己的 `InputSequence` 和 `LocalLogicTick`
- **AND** peer MAY 在一包中发送多个 command
- **AND** 系统 MUST NOT 把网络 flush 序号当作 `LocalLogicTick`

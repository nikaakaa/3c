# Proposal: 重构 Gameplay Tick 系统

## Why

当前角色管线把 Unity 每帧 `Update` 直接等同为 `simulation tick`：

```text
CharacterPipelineRunner.Update
-> frameIndex++
-> simulationTick++
-> CharacterPipeline.UpdatePhase(deltaTime, frameIndex, simulationTick)
```

这会把三种不同业务时间混在一起：

- 本地渲染帧：动画、相机、插值、cue，可能 60/120/144fps。
- 本地逻辑 tick：输入、BTSMTL、Action、Motion、本地预测，应该是固定步长。
- 服务端 tick：服务端权威快照、确认、拒绝和校正，可能是 20/30fps，只能来自服务端或 loopback peer。

如果继续沿用 `SimulationTick` 混名，后续 `add-local-network-loopback-peer`、Fantasy adapter、prediction/reconciliation、remote snapshot interpolation 都会把本地帧序号误认为服务端权威 tick，返工成本会很高。

本变更要先把 tick 语义拆干净：TEngine 只提供帧驱动，`GameplayTickSystem` 才是 gameplay tick 调度系统；`CharacterPipeline` 只是第一个 `IGameplayTickTarget`，不再接收笼统 `simulation tick`，而是接收本地逻辑 tick 和表现帧上下文；服务端 tick 只存在于网络输入、快照、确认和校正数据中。

## What Changes

- 用 `GameplayTickSystem` 替代 `CharacterPipelineRunner` 作为 gameplay 统一调度源。
- `GameplayTickSystem` 由 TEngine frame source 驱动，但自己维护固定步长 accumulator、`LocalLogicTick` 和 `RenderFrame`。
- `GameplayTickSettings` 显式声明 tick time source，普通 gameplay 默认使用 scaled delta，调试模式可配置 unscaled delta。
- 新增 `IGameplayTickTarget` 作为 gameplay tick consumer 接口；`CharacterPipeline` 实现该接口。
- `CharacterPipeline` 从 `UpdatePhase/LatePhase` 收口为 `LogicTick` 和 `PresentationFrame` 两个入口。
- `CharacterPipelineTickContext` 拆为 `GameplayLogicTickContext` 和 `GameplayPresentationFrameContext`。
- `CharacterAuthorityMode` 收束为 `GameplayAuthorityMode`，因为本地预测、远端代理和表现只读是 gameplay 权威模式，不是角色私有网络类型。
- `CharacterInputStage` 在表现帧锁存连续输入和触发边沿，在 logic tick 消费锁存输入，避免 catch-up 多 tick 重复触发 request。
- `SimulationTick` 语义改名为 `LocalLogicTick`；服务端权威 tick 使用 `ServerTick`，只能由 `ServerSnapshot`、`Correction`、action decision 或后续 Fantasy packet 带入。
- `CharacterInputFrame`、`ClientCommand`、`ActionActivationRequest` 等本地预测数据使用 `LocalLogicTick + InputSequence` 关联。
- `NetworkReceiveStage` 继续只缓存外部输入；`NetworkSendStage` 继续只收集输出；网络 peer 或 Fantasy handler 不直接 tick pipeline、不直接改 Transform、不直接改 BTSMTL。
- `add-local-network-loopback-peer` 后续必须把 `simulation tick` 口径改为 `LocalLogicTick`、`ServerTick` 和 `LatencyLocalTicks`，避免 loopback 自己制造第二套 tick 语义。

## Current Facts

- `CharacterPipelineRunner` 是 `MonoBehaviour`，在 `Update()` 中每 Unity frame 自增 `m_SimulationTick`。
- `CharacterPipelineHost.OnEnable()` 直接注册到 `CharacterPipelineRunner`。
- `CharacterPipeline.UpdatePhase()` 当前执行 NetworkReceive、Input、BTSMTL；`LatePhase()` 执行 Motion、Presentation、NetworkSend 和清理。
- `CharacterPipelineTickContext` 当前包含 `DeltaTime`、`FrameIndex`、`SimulationTick`、`InputSequence` 和 `AuthorityMode`。
- `CharacterInputFrame`、`ClientCommand`、`ServerSnapshot`、`Correction`、`ConfirmedEvent` 当前都使用 `SimulationTick` 命名。
- TEngine `RootModule.Update()` 已经调用 `ModuleSystem.Update(GameTime.deltaTime, GameTime.unscaledDeltaTime)`。
- TEngine `UpdateDriver` 提供 `AddUpdateListener`、`AddLateUpdateListener` 等帧事件注册。
- `tengine-hotupdate-foundation` current spec 仍写着角色运行由 `CharacterPipelineRunner` 调度。
- `character-pipeline-runtime` current spec 仍写着 `CharacterPipelineRunner` 是统一 tick 源。
- `add-local-network-loopback-peer` active change 仍使用 `simulation tick` 描述 peer 延迟和 packet 对齐。

## Non-Goals

- 不实现真实 Fantasy 协议、服务端 handler 或协议导出。
- 不实现完整 rollback/replay。
- 不实现完整 remote snapshot interpolation。
- 不把 `CharacterPipeline`、BTSMTL Graph、Timeline 或 ActionRuntime 做成 TEngine Module。
- 不让服务端跑 BTSMTL Graph、Unity Timeline、Animancer 或表现逻辑。
- 不恢复旧 BBB 状态机、旧 Workbench、旧 locomotion/action/footphase/bodyclaim 数据源。
- 不新增第二套角色控制器、第二套网络 peer 口径或 fallback tick 路径。

## 业务取舍

### 选择 TEngine 提供帧驱动，但 Gameplay 自己维护 tick

好处：TEngine 继续作为项目启动和生命周期底座，gameplay tick 不再需要额外场景 Runner；同时角色、网络、投射物、战斗结算等业务 tick 不会被塞进 TEngine 框架模块，边界清楚。

代价：需要新增一个项目级 `GameplayTickSystem` 初始化点，并让 Host 注册自己的 `CharacterPipeline` tick target。

### 选择本地逻辑 tick 和服务端 tick 分离

好处：本地 60Hz 手感、120fps 表现和 20/30Hz 服务端权威可以并存。服务端快照不会污染本地预测 tick，本地输入历史也不会误用服务器 tick。

代价：所有命名为 `SimulationTick` 的本地字段都要迁移，影响面比只改 Runner 大。

### 第一版不做完整 rollback

好处：先把 tick 语义、input history、command 输出和 correction 入口整理干净，能支撑 loopback 和 Fantasy adapter。

代价：收到 correction 后第一版仍可能先做 smooth correction，而不是完全重放未确认输入。

### 不把 GameplayTickSystem 做成 TEngine Module

好处：TEngine Module 保持基础设施职责，Gameplay tick 归属 `Assets/GameScripts/Main/Runtime/Gameplay/Tick`；后续角色、网络、本地 bot 或测试 runner 能复用同一个纯 C# tick system。

代价：需要一个正式 bootstrap 将 TEngine frame source 接到 `GameplayTickSystem.FrameUpdate/FrameLateUpdate`。

## Impact

- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Unity`
  - 删除或替换 `CharacterPipelineRunner`。
  - `CharacterPipelineHost` 改为注册到 `GameplayTickSystem`。
- `Assets/GameScripts/Main/Runtime/Gameplay/Tick`
  - 新增 gameplay tick system、settings、target interface、logic context、presentation context。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Tick`
  - 删除旧角色私有 tick 路径。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Runtime`
  - `CharacterPipeline` 拆分 logic tick 和 presentation frame。
  - `CharacterPipelineTickContext` 拆分或迁移。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Input`
  - `CharacterInputFrame`、history、request buffer 使用 `LocalLogicTick`。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Network`
  - `ClientCommand` 使用 `LocalLogicTick`。
  - `ServerSnapshot`、`Correction`、action decision 使用 `ServerTick`，并保留 `InputSequence` 用于确认和校正。
- `Assets/GameScripts/Main/Runtime/Character/Action`
  - 动作激活和输出合同中的 `SimulationTick` 迁移为 `LocalLogicTick` 或明确的 `ServerTick`，按数据来源区分。
- OpenSpec
  - 修改 `character-pipeline-runtime`、`character-input-pipeline`、`tengine-hotupdate-foundation`、`character-action-activation-flow`。

## Open Questions

- 本地逻辑 tick 第一版使用 60Hz 还是 30Hz。默认建议 60Hz，配置名必须是正式配置，不做 fallback。
- 网络 flush 第一版是否跟本地逻辑 tick 同频，还是以 20/30Hz 批量发送 `ClientCommand`。本 proposal 只要求 tick 语义支持两者，不强制第一版实现批量发送。
- `ActionOutputContracts` 中现有 `SimulationTick` 字段需要逐个按来源拆为 `LocalLogicTick` 或 `ServerTick`，实现时不能统一机械替换。

## 与现行 Spec 的矛盾

- `character-pipeline-runtime` 当前要求 `CharacterPipelineRunner` 是统一 tick 源；本变更将移除该要求，改为 `GameplayTickSystem` 统一调度 `IGameplayTickTarget`。
- `tengine-hotupdate-foundation` 当前要求 `CharacterPipelineRunner` 调度角色运行；本变更将改为 TEngine 只提供 frame source，gameplay tick 权威属于 `GameplayTickSystem`。
- `character-input-pipeline` 当前把输入帧保存到 `simulation tick`；本变更将其改为 `LocalLogicTick`。
- `character-action-activation-flow` 当前要求 `ActionActivationRequest` 携带 `simulation tick`；本变更将其改为 `LocalLogicTick`。
- active change `add-local-network-loopback-peer` 当前使用 `simulation tick` 描述延迟和 packet 对齐；实施本变更前后必须同步该 active change，否则 loopback 会沿用旧混名。

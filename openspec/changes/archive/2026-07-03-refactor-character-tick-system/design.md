# Design: Gameplay 多时钟 Tick 系统

## 目标模型

Gameplay 运行时必须同时处理三类时间：

```text
TE Frame
  -> RenderFrame
     -> PresentationFrame

GameplayTickSystem accumulator
  -> LocalLogicTick
     -> Input
     -> BTSMTL
     -> Action
     -> Motion
     -> Network output collection

Network packets
  -> ServerTick
     -> Snapshot
     -> Action decision
     -> Correction
```

`RenderFrame` 是本地每帧编号；`LocalLogicTick` 是本地固定步长逻辑编号；`ServerTick` 是服务端权威编号。三者不能复用一个字段。

## TEngine 关系

TEngine 负责：

- 启动项目 runtime。
- 维护 `RootModule`、`GameTime` 和基础模块生命周期。
- 提供 `UpdateDriver` 或 `RootModule.Update()` 作为 frame source。

TEngine 不负责：

- 判定角色一帧要跑几个本地逻辑 tick。
- 维护本地预测 tick。
- 管理服务端 tick。
- 直接 tick BTSMTL Graph、Timeline、ActionRuntime 或 MotionStage。

推荐第一版接法：

```text
GameApp.Entrance 或正式项目 runtime 初始化
  -> GameplayTickSystem.Initialize(settings)
  -> TEngine frame source 注册:
       FrameUpdate(GameTime.deltaTime, GameTime.unscaledDeltaTime)
       FrameLateUpdate(GameTime.deltaTime, GameTime.unscaledDeltaTime)
```

如果后续发现 `UpdateDriver.AddUpdateListener` 的注册时机不稳定，可以改为在项目 runtime 主入口显式调用 `GameplayTickSystem.FrameUpdate`，但仍然只允许一条 TE frame source。

## GameplayTickSystem

`GameplayTickSystem` 是纯 C# 项目级服务，位于正式 Gameplay runtime 路径，不放进 TEngine package，也不放在 Character pipeline 私有路径。

核心状态：

```text
Targets
RenderFrame
LocalLogicTick
FixedDeltaSeconds
AccumulatorSeconds
MaxCatchUpTicks
PresentationAlpha
```

核心入口：

```text
Register(IGameplayTickTarget)
Unregister(IGameplayTickTarget)
FrameUpdate(scaledDelta, unscaledDelta)
FrameLateUpdate()
Dispose()
```

`FrameUpdate` 负责：

```text
RenderFrame++
Accumulator += SelectDeltaBySettingsTimeSource(scaledDelta, unscaledDelta)
while Accumulator >= FixedDeltaSeconds and catchup < MaxCatchUpTicks:
  LocalLogicTick++
  target.LogicTick(GameplayLogicTickContext)
  Accumulator -= FixedDeltaSeconds
PresentationAlpha = Accumulator / FixedDeltaSeconds
```

`FrameLateUpdate` 负责：

```text
target.PresentationFrame(GameplayPresentationFrameContext)
```

如果一帧卡顿导致 accumulator 超过最大补帧上限，系统必须选择一种正式策略：

- 截断多余 accumulator，保持响应稳定。
- 或保留剩余 accumulator，但限制每帧 catch-up 数。

第一版建议限制 catch-up 数并记录 dropped local tick count，不新增 fallback 配置。普通 gameplay 默认使用 scaled delta；调试、暂停外模拟或工具模式必须通过正式 `GameplayTickSettings.TimeSource` 显式选择 unscaled delta。

## Gameplay Tick Target

`IGameplayTickTarget` 是 `GameplayTickSystem` 面向业务对象的唯一消费接口。

```text
AuthorityMode
BeginRenderFrame(renderFrame)
LogicTick(GameplayLogicTickContext context)
PresentationFrame(GameplayPresentationFrameContext context)
```

`CharacterPipeline` 是当前第一个 target。后续网络本地 peer、投射物、战斗历史或 AI 如果需要跟随同一 gameplay tick，必须接入同一个 target/hook 模型，而不是创建第二套局部 tick。

## CharacterPipeline 入口

现有入口：

```text
UpdatePhase(deltaTime, frameIndex, simulationTick)
LatePhase(deltaTime, frameIndex, simulationTick)
```

目标入口：

```text
LogicTick(GameplayLogicTickContext context)
PresentationFrame(GameplayPresentationFrameContext context)
```

`LogicTick`：

```text
Frame.Begin
GraphContext.BeginFrame
NetworkReceiveStage.Collect
InputStage.Update
BTSMTLPhase.Tick
MotionStage.Update
NetworkSendStage.Collect
```

`PresentationFrame`：

```text
PresentationStage.Update
Animation layer apply
Cue apply
Remote snapshot interpolation
Correction smoothing presentation
Frame.ClearTransient
```

业务取舍：

- Motion 放在 logic tick：移动和动作窗口是 gameplay 事实，应与输入、BTSMTL 和网络输出对齐。
- Presentation 放在 render frame：动画、相机、VFX、SFX 和插值需要高频平滑，不应被服务端 20fps 或本地 60Hz 限死。

## Context 字段

`GameplayLogicTickContext`：

```text
FixedDeltaSeconds
RenderFrame
LocalLogicTick
InputSequence
AuthorityMode
```

`GameplayPresentationFrameContext`：

```text
ScaledDeltaSeconds
UnscaledDeltaSeconds
RenderFrame
LocalLogicTick
InterpolationAlpha
AuthorityMode
```

`ServerTick` 不进入 `GameplayLogicTickContext` 的主时钟字段。需要读取服务器状态时，通过 `NetworkInput`、`CharacterGraphContext` 或后续 correction/snapshot buffer 读取。

## 输入和预测

`CharacterInputStage` 在 `RenderFrame` 边界锁存 Unity InputAction 状态，在 `LocalLogicTick` 中消费锁存输入。

```text
RenderFrame
  -> latch continuous commands
  -> latch request trigger edges

LocalLogicTick
  -> build CharacterInputFrame from latched commands
  -> consume pending request trigger edges once

CharacterInputFrame
  InputSequence
  LocalLogicTick
  AuthorityMode
  ContinuousCommands
  NewRequests
```

如果一个表现帧内补多个 logic tick，同一个触发边沿只能被消费一次；如果一个表现帧没有推进 logic tick，触发边沿必须保留到下一次 logic tick。`InputSequence` 仍然是确认和校正的核心身份。`LocalLogicTick` 用于本地历史和调试；服务端返回时必须带 `lastProcessedInputSequence` 或等价字段，不能要求客户端本地 tick 与服务端 tick 同频。

## 网络边界

本变更不实现 Fantasy transport，但定义 tick 语义：

```text
ClientCommand
  InputSequence
  LocalLogicTick
  Commands
  ActionRequests

ServerSnapshot
  ServerTick
  LastProcessedInputSequence
  Position
  Rotation
  State

Correction
  ServerTick
  InputSequence
  Position
  Rotation
  Reason
```

本地玩家：

```text
LocalLogicTick 产出 ClientCommand
NetworkSendStage 收集
peer/Fantasy flush
收到 ServerSnapshot/Correction
NetworkReceiveStage 推入
LogicTick 消费
```

远端角色：

```text
ServerSnapshot 进入 snapshot buffer
PresentationFrame 按 render alpha 或 snapshot interpolation alpha 显示
远端角色不完整重放本地 BTSMTL 输入图
```

`add-local-network-loopback-peer` 中的延迟 tick 应改名为 `LatencyLocalTicks` 或明确字段，表达 loopback 在本地调试中延迟多少本地逻辑 tick 后回推 incoming packet；packet 内部仍应能携带 `ServerTick`，用于模拟服务端权威序号。

## Timeline 和动画

`TimelinePlaybackScheduler` 继续在 BTSMTL phase 内由 logic tick 推进 gameplay 时间。

如果某条 Timeline 轨道产出 strict gameplay window、motion contribution、action cue fact，采样结果属于 logic tick。

如果某条轨道只影响最终动画权重、VFX、SFX、camera cue，它可以在 `PresentationFrame` 中被表现层应用，但输入数据必须来自 logic tick 已经确定的 active timeline state。

不允许恢复 `TimelinePlayer` autonomous tick。

## 方案对比

### A. 保留 CharacterPipelineRunner，只改字段名

好处：改动最小。

问题：仍然需要场景 MonoBehaviour 作为角色 tick 源；TEngine `RootModule` 和 runner 双入口并存；Unity frame 仍容易被误认为本地逻辑 tick；后续无头 bot、loopback 和 Fantasy adapter 都会绕回来补抽象。

结论：不采用。

### B. CharacterPipeline 做成 TEngine Module

好处：直接进入 `ModuleSystem.Update`，不需要额外 bootstrap。

问题：角色 gameplay 会混入 TEngine 基础设施；每个角色 pipeline 的注册、销毁、authority mode、场景对象引用不适合放在全局 framework module 内；以后服务端/无头复用也更难。

结论：不采用。

### C. TEngine 驱动 GameplayTickSystem

好处：TEngine 仍是唯一 Unity frame source，gameplay tick 语义在项目层清晰维护；本地逻辑 tick、渲染帧和服务端 tick 可以分离，角色和网络共享同一个时间身份。

代价：需要新增正式 bootstrap 和 tick system。

结论：采用。

## 迁移顺序

1. 新增 gameplay tick context、target interface 和 tick system 类型。
2. 让 `CharacterPipeline` 暴露 `LogicTick` 和 `PresentationFrame`。
3. 让 `CharacterPipelineHost` 注册到 tick system。
4. 删除 `CharacterPipelineRunner`。
5. 迁移 `SimulationTick` 本地字段为 `LocalLogicTick`。
6. 迁移网络包字段，服务端来源改为 `ServerTick`。
7. 同步 active loopback proposal/spec 的 tick 口径。

## 风险

- 如果 `CharacterPipelineHost` 在 `GameplayTickSystem` 初始化前启用，必须直接报错并停止注册，不能创建 fallback runner。
- 如果 active loopback change 未同步，后续实现会继续使用 `simulation tick` 混名。
- 如果 PresentationStage 仍依赖 logic context 中的 tick 字段，需要明确改为 presentation context 或从最新 logic snapshot 读取。

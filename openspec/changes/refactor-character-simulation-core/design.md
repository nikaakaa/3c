## Context

本 change 是后续多种 Network Model 的共同前置，但它本身不实现新网络模型。它只回答一个问题：同一份 Corin Gameplay 规则如何脱离 Unity authoring object，以明确输入、明确状态和明确输出执行。

当前 CharacterPipeline 的模块分层已经存在，但可变状态仍藏在多个对象中，而且模块通过 Unity object reference 串起来。只抽一个 `ICharacterSimulation` 包裹现有 runtime 不能解决这个问题：纯 .NET 仍然无法加载，snapshot 仍然不完整，后续 rollback 只能再写一套 runtime。

## Terms

- **Authoring Source**：Graph、Node、Edge、StateMachine、Timeline、TreeClip、Blackboard declaration、Action、Behavior、GameplayEffect 和 motion curve 等 Unity 编辑数据。
- **CharacterSimulationProgram**：Compiler 产生的不可变 portable gameplay 程序。
- **SimulationState**：一个 Actor 全部会影响未来 Tick 的可变 gameplay 数据。
- **SimulationKernel**：对 Program、State、Tick、Input 和 World Solver 执行一次纯逻辑 Step 的纯 C# 执行器。
- **Simulation Driver**：决定 Tick 来源、有效输入、history/replay/commit 策略的 Session 级扩展点。本 change 只提供 Local Driver。
- **World Solver**：把 portable motion request 放入具体世界约束并返回 portable body result 的扩展点。本 change 只提供 Unity CharacterController adapter。
- **Presentation Projection**：从同一 authoring source 编译的客户端 AnimationClip、Animancer、Camera 和 Cue 绑定。
- **Committer**：将已接受 simulation output 提交到表现、网络事实或 diagnostics 的副作用边界。

## End-to-End Architecture

```text
CharacterPipelineDefinition
  RootTree / StateMachine / Timeline / TreeClip / Blackboard
  Action / Behavior / GameplayEffect / MotionCurve
        |
        v
CharacterSimulationProgramCompiler
        |
        +-> CharacterSimulationProgram
        |     manifest / operations / state layout / portable catalogs
        |     source map / capabilities / ProgramHash
        |
        +-> CharacterPresentationProjection
              animation / camera / cue Unity bindings

Local Session
  Input Adapter -> CharacterSimulationInput
  LocalSimulationDriver
    -> SimulationKernel.Step
    -> UnityCharacterControllerWorldSolver
    -> SimulationState + facts + EventId commands
    -> SimulationCommitter
    -> Animation / Camera / Cue / Diagnostics
```

## Decisions

### 1. 编译 authoring，不包装旧 runtime

Compiler 以 CharacterPipelineDefinition 为根，将所有引用解析为 Program 内 index。每个 authoring element 生成稳定 operation handle 与 Source Map entry。Runtime 不按 asset path、display name、Unity instance id 或反射寻找 gameplay 数据。

不选择“用新接口包住 RunnableTree”，因为这仍然是 Unity object interpreter，也无法证明状态完整性。

### 2. Authoring Node 与 Runtime Operation 一对一

Node 类继续负责编辑名称、端口、参数和 ownership。Compiler registry 为可执行 Node/Module 提供唯一 emitter，Kernel 只执行 operation。不建立 ordinary/deterministic 两套 node，也不允许 emitter 在不同 Driver 下生成不同业务规则。

缺少 emitter 是 Program build error，不回退到虚方法解释。

### 3. 可变状态只能进入 State Layout

State layout 覆盖：

```text
Runnable lifecycle / child cursor / stop barrier
StateMachine active / pending / exiting / transition
Timeline playback / loop / TreeClip cycle
Blackboard values / scope owner generation
Action instances / request buffer / prediction identity
GameplayEffect tags / attributes / active effects / journal
Body pose / velocity / grounded / collision summary
RNG / sequence / handle allocator / event sequence
```

任何会影响后续 Tick 的字段都不得留在 operation、emitter、solver adapter 或 authoring object 内。Capture/Restore 按 state layout 完整拷贝或序列化，ProgramHash 不同时拒绝恢复。

### 4. 核心数值 portable，但不声称 Unity Solver 确定

Program/State 使用明确的整数 identity、SimulationTick、量化方向/旋转和 `SimScalar/SimVector`。时间只来自 Tick，随机只来自 SimulationState RNG，迭代顺序必须稳定。

Unity CharacterController 可在 adapter 内使用 float 和 Unity API，然后返回量化 body result。这只证明 core portable/snapshotable，不证明不同机器上 Unity Solver 完全确定。

### 5. Driver 与 Solver 是两个独立维度

Driver 决定“何时跑、用什么输入、是否恢复、什么结果可提交”。Solver 只决定“这个 motion request 在当前世界中得到什么 body result”。Graph 不知道 Driver 或 Solver 类型。

本 change 只安装 Local Driver + Unity Solver。后续 proposal 必须通过 capability manifest 验证组合，不能在 Kernel 内增加 model switch。

### 6. 表现是投影和提交，不是模拟状态

Program 保存 producer identity、Timeline gameplay sampling 和 presentation command identity，但不保存 AnimationClip、Animancer state、Camera object 或 VFX reference。Projection 把 producer identity 映射到 Unity 资源。Committer 只消费 Driver 已接受的 output。

EventId 由 ActorId、Program operation、activation identity、SimulationTick 和 local sequence 稳定构成，为后续 rollback 提供去重/替换身份；本 change 的 Local Driver 只提交一次，不实现 replay policy。

### 7. 现有 ServerAuthoritative 只做端口迁移

为了不在核心 apply 后留下编译断裂或两套 Character adapter，现有 ServerAuthoritativeHybrid binding 必须改用最终 Driver/Input/Output 端口，correction/history 归回该 model。本 change 不增加真实 endpoint、服务端或 Demo，LocalLoopback/disconnected 能力不扩张。

这不是临时 bridge：后续 ServerAuthoritative proposal 直接扩展同一 model Driver 和 endpoint，不再替换 core contract。

## Ownership Matrix

| 数据/策略 | 唯一所有者 |
|---|---|
| Graph/SM/Timeline/Action/Effect 不可变逻辑 | CharacterSimulationProgram |
| Actor 可变 gameplay 状态 | SimulationState |
| 单 Tick 业务执行顺序 | SimulationKernel |
| Tick、input、history、restore、commit 策略 | Simulation Driver |
| 世界约束与 body result | World Solver |
| Unity 表现资源定位 | CharacterPresentationProjection |
| 外部副作用 | SimulationCommitter |
| packet、endpoint、model history | 具体 Network Model |

## Failure Policy

- ProgramHash、compiler version、TickRate 或 source revision 不一致：拒绝加载。
- 缺失 emitter、断裂 reference、不支持 Unity API、状态布局冲突：编译失败并定位 source identity。
- 缺失 Driver、Solver、Projection 或 Committer：Host 创建失败。
- Snapshot 与 ProgramHash 不匹配：拒绝 restore。
- 任何失败都不回退旧 interpreter、Transform 直写、默认 solver 或 LocalLoopback。

## Dependency Contract for Follow-Ups

核心 change 完成并归档后，后续模块只能依赖以下稳定表面：

```text
CharacterSimulationProgram bytes + ProgramHash + CapabilityManifest
CharacterSimulationInput
SimulationState capture/restore/hash
SimulationKernel.Step
ISimulationDriver composition contract
ICharacterWorldSolver request/result/state contract
SimulationOutput + EventId commands
CharacterPresentationProjection + SimulationCommitter
Debug Source Map + structured Trace
```

ServerAuthoritative 和 DeterministicRollback 可在核心归档后并行实施，但不得各自修改 Program operation 语义或添加专用节点。DotRecast backend 可并行开发 Solver/.NET Host，最终网络 Demo 集成依赖 ServerAuthoritative host contract。

## Context

项目已经有三块可复用基础：

1. `GameplayNetworkSessionHost -> GameplayNetworkModelDefinition -> model session` 已经把完整网络模型放在 Session 级装配，并禁止 Graph/Character 持有 model packet。
2. `MotionContribution -> MotionIntent -> ICharacterMotionExecutor -> MotionResult` 已经把 gameplay motion 与 Unity CharacterController 分开。
3. Debug Source Map 与 structured Trace 已经按 authoring identity 映射 runtime，而不是让 Editor 绑定 runtime clone。

缺口在于 BTSMTL 与 CharacterPipeline 仍然是 Unity 对象运行时。`RunnableNode`、`StateMachineGraphRuntime`、`TimelinePlaybackScheduler`、Pipeline Blackboard、ActionRuntime 和 GameplayEffectRuntime 分别拥有隐藏可变状态；Tree/Timeline 通过 authoring clone 隔离实例；部分节点读取 `Time.deltaTime`、Unity Random、Input System 或 Camera snapshot。`CharacterPipeline.LogicTick` 同时推进网络语义输入、Graph、Motion、网络事实、Presentation sample 和 Camera。这样的对象图不能作为纯 .NET 服务端程序，也不能被 Session 级 rollback 原子恢复。

本 change 选择一次性把 gameplay 执行迁移成编译式模拟内核。迁移期间允许代码暂时不能运行，但最终不保留两套正式执行路径。

## Terms

- **Authoring Source**：现有 BTSMTL Graph、Node、Edge、StateMachine、Timeline、TreeClip、Blackboard declaration、Action/Behavior/GameplayEffect profile 等 Unity 编辑数据。
- **CharacterSimulationProgram**：由 Authoring Source 编译出的不可变、无 Unity 对象引用的角色 gameplay instruction/data 聚合。
- **Presentation Projection**：同一 authoring source 编译出的客户端专用动画、相机和可视采样定位数据；它不是第二份 gameplay 数据源。
- **SimulationState**：一个 Actor 的 Node、SM、Timeline、Blackboard、Action、Effect、Body、RNG、counter 等全部可变 gameplay 状态。
- **SimulationWorldState**：一个 Session 的 SimulationTick、按稳定 ActorId 排序的 Actor state、world solver state、command cursor 和 model-owned state。
- **SimulationKernel**：只根据 Program、旧状态、Tick 输入和 World Solver 产生新状态与输出的纯 C#执行器。
- **Simulation Driver**：决定谁推进 Tick、哪些输入有效、是否保存 history、何时 restore/replay、哪些输出可以 commit 的 Session 级策略。
- **World Solver**：把 portable motion request 放入具体世界约束并返回 portable body result 的后端。
- **Committer**：把 simulation output 投影到动画、相机、音效、VFX、网络事实和 diagnostics 的边界。

## Goals / Non-Goals

### Goals

- 同一份 Corin authoring 只编译出一个 canonical gameplay Program。
- 单机、ServerAuthoritative 和 DeterministicRollback 共用同一 SimulationKernel 和 Program。
- Program/State/Kernel 不依赖 UnityEngine，能由 Unity 客户端、Unity server process 和 .NET server 编译执行。
- 全部 gameplay 状态可以完整 capture/restore；同一 Program、State、Tick 输入和 deterministic world result 得到相同 state/hash。
- Network Model、Endpoint、Transport、Host 和 World Solver 保持独立组合，Graph 不感知组合。
- 三个 Demo 复用同一角色规则、同一协议身份和同一表现链，并提供可比较诊断。
- 迁移后删除旧执行路径和旧配置。

### Non-Goals

- 不把所有 Unity 业务都搬入 deterministic world；Presentation 与非 gameplay Unity 系统继续留在客户端。
- 不承诺 DotRecast 是 KCC，也不承诺 Unity CharacterController 具有确定性。
- 不支持 Session 内热切模型、热切 solver 或旧/new Program state 迁移。
- 不保留 generic node 对任意 Unity API 的调用能力；unsupported gameplay operation 编译失败。

## End-to-End Architecture

```text
Unity Authoring
  CharacterPipelineDefinition
  RootTree / StateMachine / Timeline / TreeClip / Blackboard
  Action / Behavior / GameplayEffect
        |
        v
CharacterSimulationProgramCompiler
        |
        +-> CharacterSimulationProgram bytes
        |     program manifest / operation tables / state layout
        |     timeline gameplay data / portable catalogs / source map
        |
        +-> CharacterPresentationProjection
              animation producer ids / Unity clip bindings / camera bindings

Gameplay Session composition root
  -> exactly one Simulation Driver
  -> exactly one compatible World Solver
  -> optional model Endpoint + Transport
  -> one SimulationWorldState
       -> ActorId -> Program + SimulationState

Simulation Tick
  Driver canonicalizes CharacterSimulationInput
  -> SimulationKernel.Step actors in stable order
  -> WorldSolver executes motion
  -> state + facts + EventId commands
  -> Driver records/acknowledges/restores/replays
  -> Committer publishes accepted/predicted presentation and network outputs
```

## Decisions

### 1. 编译 authoring，不在运行时 clone authoring object

Compiler 以 `CharacterPipelineDefinition` 为编译根，递归解析 RootTree、inline/shared Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard declaration、Action、Behavior、GameplayEffect 和 motion curve。每个 authoring element 生成稳定 operation handle 与 Source Map entry；所有引用在编译期解析为 Program 内 index，运行时不再按 Unity object、显示名、asset path 或反射查找。

Program 至少包含：

```text
ProgramId / Revision / TickRate / ProgramHash
Operation table / constants / control-flow table
State layout / scope layout / blackboard layout
StateMachine transition table
Timeline segment / TreeClip / motion sample table
Action / Behavior / GameplayEffect portable runtime definitions
Input schema / output schema
Debug Source Map
Capability manifest
```

Compiler 必须生成稳定 bytes：相同 authoring content、compiler version 和 TickRate 产生相同 ProgramHash。CharacterSimulationProgram 不包含 `UnityEngine.Object`、AnimationClip、Animancer transition、Endpoint、Transport、Network Model 或 server backend。

不选择“运行时反射 authoring node 并自动序列化私有字段”，因为它无法证明状态完整性，也不能让纯 .NET 服务端脱离 Unity assemblies。

### 2. Authoring Node 与 Runtime Operation 一对一映射，不复制业务节点

Node 类继续提供名称、端口、参数、inline/shared ownership 和 Editor UI。Compiler registry 为可执行 node/module 类型提供唯一 emitter，生成 portable operation。SimulationKernel 只执行 operation；单机、权威和 rollback 不拥有各自 node 实现。

控制流、Bool/Int/定点数运算、If/And/Or、StateMachine、Timeline、Blackboard、Action 和 GameplayEffect 使用统一 operation。时间读取固定 SimulationTick；随机读取 SimulationState 中的 RNG；world query 通过 World Solver；Animation/Camera/Cue 只生成 command。

无法编译的 node 在 Program build 时报告精确 source identity 和原因。系统不为它创建 interpreted fallback，也不在 deterministic model 中静默跳过。

### 3. 可变状态集中进入显式 State Layout

Program 为每个有状态 operation 分配 state slot；SimulationState 不保存 authoring node 或 runtime clone。State layout 覆盖：

```text
Runnable lifecycle / child cursor / stop barrier
StateMachine active/exiting/pending state and transition
Timeline playback time/cycle/TreeClip membership
Blackboard values and scope owner generations
Action instances / prediction identities / request buffer
GameplayEffect tag/attribute/active effect/prediction journal
Body pose/velocity/grounded/collision summary
RNG / sequence / handle allocator / event sequence
```

Capture/Restore 使用 Program state layout 深复制或序列化完整 state；不得让模块额外保存影响未来 Tick 的隐藏字段。ProgramHash 不匹配时 snapshot 必须拒绝加载。

### 4. Gameplay 数值与 deterministic capability

Portable gameplay core 使用明确的 `SimScalar`、`SimVector2/3`、量化 rotation、Tick duration 和稳定整数 identity。Deterministic Program 禁止把 `float/double`、Unity Quaternion、AnimationCurve evaluator、系统时间、未保存种子的随机数或无序迭代结果写入 gameplay state。

Authoring 中的 float 参数由 Compiler 按 Program 数值格式量化，并在 Inspector 显示最终 runtime 值。Timeline gameplay motion curve 按固定 Tick 烘焙为 portable sample；动画 visual sampling 继续保留 presentation float time，不进入 state hash。

Unity/DotRecast solver 可以在 adapter 内使用 float，但必须把结果量化回 portable body result；它们因此可用于 Local/ServerAuthoritative，不获得 Deterministic capability。DeterministicRollback 只接受全程 deterministic 的 Program 和 KCC solver。

### 5. SimulationKernel 与 World Solver 的职责

SimulationKernel 拥有业务推进顺序：input request、Decision TreeClip、RootTree/StateMachine、Window projection、Timeline commit、Action/Effect、Motion resolution 和事实输出。World Solver 只接受 body state 与 motion request，返回 world-constrained result；它不读取 Graph、Action、Timeline、Network packet、prediction 或 presentation。

World Solver capability 至少区分：

```text
Portable
Deterministic
Snapshotable
StaticNavigationSurface
CharacterCapsuleCollision
```

缺少模型要求的能力在 Session 启动前失败，不切换 solver。

### 6. Driver 是唯一模型策略执行者

`ISimulationDriver` 位于 Session 级 composition root，拥有 Actor binding、Tick 调度、command acceptance、history、restore/replay 和 commit policy。Graph/Kernel 不读取 Driver 类型。

- `LocalSimulationDriver`：每个本地 Tick 推进一次，不保存 rollback history，结果立即进入 Committer。
- `ServerAuthoritativeHybridDriver`：Owner 本地预测；服务端从 canonical input 独立推进；Owner 收到权威状态后由 model-owned reconciliation 恢复或校正；Remote 消费 server snapshots。当前 MotionStage partial/full correction 被删除，正式策略属于该 Driver。
- `DeterministicRollbackDriver`：服务端生成 canonical input bundle，所有参与者按同一 SimulationTick 推进；迟到输入触发 world snapshot restore/replay；周期交换 state hash；服务端 snapshot 只用于加入、超出 history 或 hash 失配恢复。

`CharacterInputSource` 可以保留为 Unity 输入适配器内部来源概念；`CharacterMotionAuthority` 和 ExternalPose 作为 CharacterPipeline 执行分支删除。模型通过 actor binding 决定 actor 是本地模拟、canonical 模拟还是 snapshot sampled，不把角色伪装成另一种 Pipeline。

### 7. 第一、第二 Demo 共用同一个 Network Model

Unity authoritative process 和 pure .NET DotRecast host 都实现 `ServerAuthoritativeHybrid` 的同一 command/snapshot/action protocol 与 Driver 语义。差异只在服务端 Host 和 World Solver：

| Demo | Network Model | Host | World Solver |
|---|---|---|---|
| Unity Authoritative | ServerAuthoritativeHybrid | Unity process | Unity CharacterController |
| DotRecast Authoritative | ServerAuthoritativeHybrid | .NET process | DotRecast navigation surface |
| Deterministic Rollback | DeterministicRollback | .NET/Unity shared core | Deterministic KCC |

DotRecast solver 只在已烘焙静态 NavMesh 上约束位置和高度，支持 Demo 所需移动、转向、闪避和动作位移；动态碰撞、台阶物理、刚体推挤和通用 KCC 不在能力声明中。这样能诚实比较“轻量导航约束权威端”，又不违反当前 spec 的“DotRecast 不等于 KCC”。

### 8. Pure C# core 使用单一源码

Program contracts、SimulationState、Kernel、portable Action/Effect runtime、Driver 公共合同和 deterministic KCC 位于不引用 UnityEngine 的共享源码程序集。Unity asmdef 与 .NET server csproj 编译同一组 canonical source，不复制文件或生成第二份实现。Unity authoring/compiler/presentation adapter 单向依赖 core；server host 单向依赖 core 和自己的 solver/transport。

任何 Unity object 到 Program data 的转换只发生在 Editor Compiler；server 不加载 Unity YAML 或 ScriptableObject。

### 9. 输入先归一，再进入 Program

Unity Input System、Camera basis 和外部网络事实都在 Kernel 外转换为 `CharacterSimulationInput`。输入保存稳定 InputId、Tick、Sequence、typed value 和 request edge。相机相对移动必须在采集时变成量化 world direction，或把量化 camera yaw 作为正式输入；Program 不读取客户端 CameraState。

本地输入 capture history 与模型 replay history分开：Input adapter 可保存原始 frame 供诊断；ServerAuthoritative/DeterministicRollback Driver 保存自己实际接受的 canonical simulation input。Rollback 不从 CharacterInputStage 的偶然缓存恢复。

### 10. 表现命令延迟提交且可去重

Simulation output 分为：

```text
Persistent gameplay state
Gameplay facts
Presentation commands
Network/model observations
```

每条 Presentation command 使用 `ProgramId + ActorId + SimulationTick + EventSequence` 形成稳定 EventId。Local/预测 Driver 可以立即显示允许预测的动画和相机；replay 重新生成相同 EventId 时只替换当前预测记录，不重复播放一次性 Cue。权威 Reject/Correct 通过 Committer 撤销或替换尚未确认的预测表现。Animancer fade、Timeline visual sampling、Camera 和 VFX 仍按 RenderFrame 推进，不进入 snapshot 或 state hash。

### 11. Editor 只配置完整组合，不把网络配置放进 Graph

`CharacterPipelineDefinition` Inspector 提供 Compile、ProgramHash、TickRate、capability、source revision 和错误入口。Graph/Timeline 窗口只显示当前 Program compatibility 和 source-mapped compile diagnostics，不出现 Network Model 字段。

`GameplayNetworkSessionHost` 继续引用具体 `GameplayNetworkModelDefinition` asset。Model Inspector 显示 model、endpoint、transport、required program capability、server host manifest 和 world solver capability。只有 runtime、配置、actor binding、协议和 Demo 全部存在的 definition 才可创建；DeterministicRollback 在本 change 完整实现前不进入可选列表。

三个 Demo 使用独立正式 definition/manifest 资产，不在一个资产中提供 runtime model switch。

### 12. Diagnostics 与比较数据共用正式事实

Compiler 复用现有 Debug Source Map identity。Simulation Trace 记录 Program revision、SimulationTick、pass kind（Forward/Replay）、ActorId、operation handle、state transition、world solver result 和 EventId；Rollback Trace 记录 restore tick、replay range、hash 和 recovery cause。Editor overlay 继续通过 Source Map 映射到 Graph/Timeline，不绑定 SimulationState 对象。

Comparison HUD/Inspector 只读取 model-owned metrics：RTT、bytes、queue health、prediction error、correction、rollback count、replayed ticks、hash mismatch。它不参与 command acceptance、solver、commit 或 state hash。

### 13. 迁移顺序必须保持一条正式链路

实现顺序固定为：

```text
Program/Core contracts
-> Compiler and state layout
-> compile all Corin-reachable semantics
-> Local Driver + Unity solver + existing Presentation
-> migrate Corin and delete interpreted Character runtime
-> model capability authoring
-> Unity authoritative Demo
-> DotRecast authoritative Demo
-> DeterministicRollback Demo
-> remove obsolete network change/docs/assets
```

在 Local Driver 完整接管前不把新 Program 作为可选 runtime 暴露；接管时直接切换正式入口并删除旧入口。后续 Demo 只增加 Driver/Solver/Host 插件，不回到 Graph 或 Kernel 增加 model switch。

## Tradeoffs

### 编译式统一内核与保留对象解释器

选择编译式统一内核。代价是 BTSMTL runtime、Timeline、Blackboard、Action/Effect integration 都要迁移，前置工作远大于增加一个 Motion Executor。收益是单一业务规则、可移植 server、完整 snapshot 和稳定 program hash；保留对象解释器会永久形成 singleplayer/deterministic 双路径，不符合项目清理原则。

### 全 gameplay deterministic-compatible 与只确定性化 KCC

选择让三个 Demo 共用的 Corin gameplay Program deterministic-compatible。单机与 ServerAuthoritative 不启用 rollback，但仍运行同一 Program。只确定性化 KCC 无法避免 BTSMTL transition、Timeline window、Action 和 Effect 分歧，只能称为确定性移动预测，不能完成第三个 Demo。

### 固定/量化 gameplay 数值与跨平台 float

选择固定/量化 gameplay 数值。它会限制任意 Unity math 和 AnimationCurve 直接进入 gameplay，也需要作者看到量化结果；但跨进程 deterministic rollback 不能依赖“同平台大概率相同”的 float。Unity/DotRecast adapter 仍可内部使用 float，并明确失去 deterministic capability。

### DotRecast navigation solver 与伪装通用 KCC

选择诚实限制 Demo2 为静态 NavMesh 表面约束。优点是能验证纯 C# 权威 host、portable Program 和轻量服务端；代价是它不能代表复杂台阶、动态障碍和角色推挤。补一套临时碰撞算法会让 Demo2 变成未经设计的 KCC 分支，因此不做。

### 一个总 change 与多个互相兼容的过渡 change

选择一个 change 内按里程碑串行实施。它很大，但可以在迁移完成前允许编译断裂，并在切换时删除旧运行时。拆成多个可单独运行 change 会迫使旧解释器和新 Program 共同存在，违反“不保留临时桥接和分裂路径”。tasks 仍按小闭环细分并保持一次只做一项。

## Risks

- 当前节点类型多且部分直接调用 Unity API；Compiler inventory 不完整会导致 Corin 或其它正式资产无法编译。实现必须先生成完整 source/type inventory，并对 unsupported source 明确失败。
- Action、GameplayEffect 和 Behavior profile 当前是 ScriptableObject；portable runtime definition 必须由同一 compiler 投影，不能让 server 重新解释 YAML。
- Program state 漏掉一个 counter、handle、scope generation 或 journal 都会造成 replay 分歧；state ownership inventory 是实现前置，不允许靠反射补漏。
- Unity 和 DotRecast solver 的结果不同是两个 Demo 后端的真实差异；比较工具必须展示结果，不得把一个后端结果作为另一个的 fallback。
- Deterministic KCC 的 world scope必须保持静态、可排序和可 snapshot。若实现过程中发现现有动作需要未声明的 Unity Physics、动态刚体或无法量化的查询，本 change 必须停止并说明业务 tradeoff，不能绕过 Kernel。
- 现有 Timeline Preview 是 Editor presentation 工具；它可以消费 Presentation Projection，但不得重新成为 gameplay Timeline 解释器。
- active `add-local-two-client-gameplay-network-closure` 与本 change 重叠；apply 前必须确认不再并行执行，并在需求吸收后撤销其目录。

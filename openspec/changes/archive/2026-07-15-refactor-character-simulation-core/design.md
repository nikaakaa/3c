## Context

本 change 的目的不是把现有 `CharacterPipeline` 包进一个新接口，而是建立一条可由 Unity 客户端、Unity 权威进程和普通 .NET 宿主共同执行的 Gameplay 规则链。BTSMTL 继续作为 authoring 基座，但 runtime 不再依赖 Unity object clone。

当前最大的结构问题不是“缺一个 Network Model 接口”，而是 Character 自己同时拥有逻辑执行、世界移动、网络输入输出和表现提交。只抽 `ICharacterSimulation` 会把隐藏状态和单角色 `CharacterController.Move` 原样包住，仍然不能批量求解、完整快照或安全重演。

## Terms

- **Authoring Source**：Graph、Node、Edge、StateMachine、Timeline、TreeClip、Blackboard declaration、Action、Behavior、GameplayEffect 和 motion curve 等 Unity 编辑数据。
- **Gameplay Semantic IR**：Compiler Frontend 从 Authoring Source 生成的 numeric-neutral 不可变语义层，只保存 operation 语义、控制流、状态声明、稳定 identity、原始数值字面量和能力要求，不是 Runtime Program。
- **Numeric Target**：将 Semantic IR 降低为一个完整执行 ABI 的编译目标，定义 scalar/vector、Program constant、Input、State、WorldRequest/Result、GameplayFact 和 Snapshot codec。Target 在 Session 创建前锁定，不能在 Tick 中切换。
- **CharacterSimulationProgram**：Target Compiler 从 Semantic IR 生成的不可变 portable Gameplay 程序，不保存 Unity object；每个 Program 只对应一个 NumericProfile。
- **SimulationProgramCatalog**：Session 启动时锁定的 ProgramId 到 ProgramHash/LayoutHash/capability 的不可变有序映射。
- **CharacterPresentationProjection**：从同一 authoring source 生成的客户端资源绑定，只保存 AnimationClip/Animancer/Camera/Cue 等表现引用。
- **CharacterSimulationState**：单个 Actor 会影响未来 Gameplay Tick 的逻辑状态。
- **WorldSimulationState**：当前 WorldSolver 拥有的 Actor body、碰撞和必要世界可变状态。
- **SimulationWorldSnapshot**：某 Tick 的 ProgramCatalog、每 Actor Program binding、solver/world identity、按 ActorId 排序的 Character state 与 World state 原子快照。
- **SimulationKernel**：无状态的 Program operation 执行器，只负责 Evaluate 与 Finalize。
- **SimulationSessionRuntime**：一个模拟 Session 的唯一执行协调器，拥有当前 actor state set、world state 和四阶段顺序。
- **Simulation Ingress**：Driver 在 Tick 开始时提交的模型无关 typed lifecycle/result facts，不包含 packet、history 或 transport metadata。
- **Simulation Driver**：为 Session 提供 Tick plan、Actor control input、typed ingress、restore request 和外部 `SimulationOutputPlan` 的策略插件。
- **World Solver**：批量解析 portable world requests，唯一拥有世界约束与 WorldSimulationState 的插件。
- **Simulation Committer**：在 Gameplay state 已原子发布后消费 `SimulationOutputPlan`，并调用表现与模型 adapter 等有副作用的外部端口。

## End-to-End Architecture

```text
CharacterPipelineDefinition
  RootTree / StateMachine / Timeline / TreeClip / Blackboard
  Action / Behavior / GameplayEffect / MotionCurve
        |
        v
CharacterSimulationCompiler Frontend
        |
        +-> Gameplay Semantic IR
        |     operation semantics / control flow / state declarations
        |     source literals / source map / capability requirements
        |             |
        |             v
        |     Float32 Target Compiler (本 change)
        |             |
        |             v
        |     Float32 CharacterSimulationProgram
        |       target manifest / typed operations / state layout
        |       constants / source map / capabilities / ProgramHash
        |
        +-> CharacterPresentationProjection
              animation / camera / cue Unity bindings

Future FixedQ32.32 Target Compiler
  consumes the same Semantic IR
  produces a different ProgramHash / LayoutHash / State-Snapshot ABI

SimulationProgramCatalog
  ordered ProgramId -> ProgramHash / LayoutHash / capabilities
  locked ActorId -> ProgramId roster binding

GameplayTickSystem
        |
        v
ISimulationDriver.PrepareTick
  SimulationTickPlan
  ordered ActorInput set
  ordered SimulationIngress set
  optional RestoreRequest
        |
        v
SimulationSessionRuntime
  1. validate + atomic restore
  2. Kernel.Evaluate(all actors, stable ActorId order)
  3. WorldSolver.ResolveBatch(one call)
  4. Kernel.Finalize(all actors, same order)
        |
        v
SimulationTickResult
        |
        v
ISimulationDriver.BuildOutputPlan
        |
        v
validated SimulationOutputPlan
        |
        v
SimulationSessionRuntime atomic state publish
        |
        v
SimulationCommitter
        |
        v
Animation / Camera / Cue / VFX / UI / Model Adapter
```

Structured Trace 从 Compiler、Kernel、SessionRuntime、WorldSolver adapter、Driver 与 Committer 各自边界进入只读 Diagnostics sink，不经过 OutputPlan，失败 Tick 和 replay 也必须保留明确 disposition。PresentationFrame 只读取已发布的 body samples 与 presentation command lifecycle。它不参与 Evaluate、ResolveBatch 或 Finalize，也不改变 SimulationWorldSnapshot。

## Decisions

### 1. 编译 authoring，不包装旧 runtime

Compiler 以 `CharacterPipelineDefinition` 为唯一根，将全部可达 authoring identity 解析为 Program 内 index。每个可执行 authoring type 必须由唯一 Emitter 产生一个或多个明确 typed operation；Emitter 同时声明读取的 constant、state slot、input channel、world request 和 output channel。

Runtime 不按 asset path、display name、Unity instance id、反射或节点虚方法寻找规则。缺少 Emitter、Emitter 未声明状态、循环引用或 source identity 冲突都属于编译错误，不回退旧 RunnableTree。

选择这一方案是因为纯 .NET 与完整 snapshot 是明确目标。代价是所有当前 Corin 可达节点、StateMachine、Timeline 和模块都必须一次迁移；本 change 用单机 Corin 完整切换作为验收，不保留双 runtime。

### 2. Program operation 与 authoring identity 保持可追踪，不强制类结构一对一

一个 Node 可以生成多个 operation，一个 operation 也可以引用共享 catalog entry。正式约束是每个 operation 都必须有唯一 source map、明确状态布局和稳定执行顺序，而不是机械要求“一个 Node 类等于一个 runtime 类”。

这样可以避免把旧对象模型原样搬进 Program，同时保持 Graph/Timeline 调试可定位。

### 3. Gameplay 语义统一，数值格式按编译目标隔离

Compiler Frontend 先生成 numeric-neutral Gameplay Semantic IR。IR 中数值保存为带 source identity 的 canonical source literal，不提前承诺 Float32、Fixed64 或具体 scale。Operation kind、控制流、状态所有权、事件顺序和能力要求只定义一次。

Target Compiler 将同一 IR 降低为完整且封闭的执行 ABI：

```text
Float32 Target
  Float32 Program / Input / CharacterState / WorldRequest-Result
  Float32 GameplayFact / Snapshot codec / Kernel specialization

FixedQ32.32 Target
  Fixed Program / Input / CharacterState / WorldRequest-Result
  Fixed GameplayFact / Snapshot codec / Kernel specialization
```

同一 Session 的 ProgramCatalog、Kernel、Driver Input Adapter、WorldSolver 与 Snapshot codec 必须属于同一 NumericProfile。ProgramHash 与 LayoutHash 包含 NumericProfile；Float32 与 Fixed artifact 即使来自同一 source revision 也不能互换 state、snapshot 或 world result。运行时不按 Driver、Network packet 或 Node 分支切换数值 backend。

Operation 的业务语义由版本化 Semantic IR opcode contract 唯一定义。Numeric Target backend 负责 constant lowering、Program/State codec 和该 opcode set 的数值执行；它 MAY因 Float/Fixed 算术实现不同而拥有不同 backend code，但 MUST逐项实现同一 operation-set version，不能改变控制流、状态所有权或事件顺序。禁止出现 `FloatMoveNode`、`DeterministicMoveNode`、Rollback 专用 Graph、模型专用 opcode 或两份 Authoring 业务运行时。

本 change 只安装 Float32 Target。它拒绝 NaN、Infinity、非法 normalized direction、非稳定集合顺序和平台 Random，但不宣称跨机器 bitwise deterministic。Unity CharacterController Solver 使用 Float32 ABI并只声明 Reconstructible。未来 Rollback 必须同时安装 Fixed Target、符合该 ABI 的 Deterministic WorldSolver 和模型 history/replay/commit，不能把 Float32 Program 量化后冒充 Fixed Program。

### 4. Character state、World state、Driver state 与 Presentation state 分离

`CharacterSimulationState` 覆盖：

```text
Runnable lifecycle / child cursor / stop barrier
StateMachine active / pending / exiting / transition
Timeline playback / loop / TreeClip cycle
Blackboard value / scope owner generation
Action instance / request buffer / lifecycle
GameplayEffect tag / attribute / active effect / journal
Gameplay motion accumulator / pending world request
RNG / activation / handle / fact / event sequence
```

`WorldSimulationState` 覆盖：

```text
ordered Actor body state
solver-owned contact / grounding / query state when required
mutable world data explicitly supported by the solver
world revision / static artifact identity
```

Driver state 保存 input history、prediction metadata、ack、rollback cursor 或网络 queue；Presentation state 保存 animation playback、fade、camera 和 visual interpolation。两者不进入 Gameplay state hash。Driver 要让已接受的外部 Action lifecycle、GameplayResult 或 Effect lifecycle 进入模拟时，必须先转换为 model-neutral SimulationIngress；packet metadata 留在 Driver state。

Driver 若需要恢复，必须保存 `SimulationWorldSnapshot` 和自己的 model snapshot，并通过 SessionRuntime 提交原子 restore 请求。Driver 不获得 Character state 的可变引用。

### 5. 世界求解采用批次，不在 Character Step 内调用 Solver

每个 Tick 先对全部 Actor Evaluate，产生 `WorldSolveBatchRequest`。SessionRuntime 只调用一次 `ResolveBatch`，随后使用一一匹配的 ActorId/request sequence Finalize。

Local Sandbox 当前只有一个 Actor，Unity Solver 可以按稳定 ActorId 顺序逐个调用现有 CharacterController adapter；合同仍然是 batch。未来多角色碰撞或共享世界查询可以在 Solver 内统一实现，不需要修改 Graph、Kernel 或 Driver。

不采用单角色 `Kernel.Step(..., solver)`，因为它把 actor iteration、world mutation 和 collision order藏进外层调用顺序，无法建立完整 world snapshot。

### 6. Kernel 使用 Evaluate/Finalize 两阶段

Evaluate 输入 Program、Tick、Character input、当前 Actor 的 ordered SimulationIngress、只读 Character state 与上一 Tick body observation，输出：

```text
PendingCharacterEvaluation
WorldRequest set
pre-world gameplay facts
```

Finalize 输入 pending evaluation 和精确 WorldSolver result，输出：

```text
new CharacterSimulationState
typed gameplay facts
body sample
presentation commands with EventId
structured trace records
```

Pending evaluation 只在当前 Tick 内存在，不可跨 Tick、不可序列化为第二份状态，也不可被 Driver 修改。World result 缺失、重复、ActorId/sequence 不匹配时整个 Tick 失败，不部分提交。

Evaluate 内部必须先应用 SimulationIngress、推进 GameplayEffect 和整理输入请求，再处理 Timeline Decision 预采样。每个 Running Timeline 在预采样前必须校验 retention slot 保存的 ActionInstance 仍是对应 Action Context 的当前 active instance；实例已终止或被替换时，Timeline request 进入 `ActionContextEnded` 停止，先释放 producer/camera 并完成 TreeClip stop barrier，不再采样 Decision、Commit、motion、cue 或 window。停止完成只表示 TimelineNode 请求已经终止并允许 ConditionRuleGraph 继续选边，不代表 Action 业务成功，也不得由 Timeline 推导新的 Action lifecycle。

### 7. SessionRuntime 是唯一状态执行者，Driver 保持最小

`ISimulationDriver` 只参与以下边界：

```text
PrepareTick(frame context, actor roster) -> SimulationTickPlan
TryBuildRestoreRequest() -> optional restore request
ObserveTickResult(readonly result)
BuildOutputPlan(readonly result) -> SimulationOutputPlan
```

Driver 可以决定本 Tick 没有可执行计划，也可以在执行前申请 restore；它不能调用 operation、调用 Solver、直接替换 state、写 scene object 或播放表现。`BuildOutputPlan` 只为每个外部 EventId 选择 Publish、Replace、Retire 或 Suppress，不具有 Gameplay state 接受/拒绝权。

Tick plan 中的 CharacterSimulationInput 与含数值 SimulationIngress payload 必须匹配 Session NumericProfile；Ingress header 继续使用 Core 声明的 target-neutral typed gameplay identity，并按 ActorId、source tick、sequence 和 fact identity 稳定排序。Local Driver 的 ingress 为空。ServerAuthoritative 和 Rollback 后续可以把协议结果映射为当前 target ingress、保存不同 history、产生 restore，并对外部 EventId 产生不同 OutputPlan，但 packet 与策略不进入公共 Driver base。

SessionRuntime 必须先校验 OutputPlan 覆盖关系，再原子发布全部 staged Character/World state，随后调用 Committer。OutputPlan 无效会让当前 Tick 在 state publish 前失败；Committer port 在 publish 后失败时 Session 必须 fail-stop 并报告精确 EventId，不得回滚已触发的外部副作用或自动重放。

Local Driver 使用 GameplayTickSystem 的 LocalLogicTick，提供本地 Input Adapter 采样，并对全部新输出 EventId 选择 Publish。

### 8. Tick identity 不混淆时间域

`SimulationTick` 只表示某个 SimulationSessionRuntime 内的有序执行位置。Local Driver 从 LocalLogicTick 建立映射；权威 Driver 可以从 server simulation clock 建立映射；Rollback Driver 可以重演既有 SimulationTick。RenderFrame 永不转换成 SimulationTick。

Tick plan 必须携带 source clock identity 和 Program/world revision。系统不得假定 LocalLogicTick、ServerTick 和 SimulationTick 数值相等。

### 9. Snapshot 与 Hash 分层

`CharacterStateHash` 只覆盖 ProgramHash、NumericProfile、TargetAbiVersion、Character layout 和 canonical Character state bytes。

`SimulationWorldHash` 覆盖：

```text
Program set hash + NumericProfile + TargetAbiVersion
world revision + solver identity/version
SimulationTick
stable ActorId roster
all Character state bytes
WorldSimulationState bytes
```

只有 Numeric Target、组合中的 Program 与 Solver 都声明 DeterministicReplay 时，跨机器 WorldHash 才可作为确定性判定。Float32 Local Unity 组合仍可生成本地 hash 用于 capture 一致性和 diagnostics，但不得宣传为跨机器 determinism。

Snapshot restore 必须在 Tick 开始前完成，校验全部 identity 后原子替换。失败时保持旧 world 不变，不做 partial restore 或 Transform fallback。

### 10. WorldSolver 能力必须真实声明

Solver manifest 必须先声明 NumericProfile 与 TargetAbiVersion，再区分：

- `Reconstructible`：可从 portable WorldSimulationState 重新建立运行状态；
- `Snapshotable`：可编码并精确恢复额外可变 solver state；
- `DeterministicReplay`：同 Program/input/world state 可产生 canonical 相同结果；
- 具体 world feature：Ground、Slope、Step、WallSlide、DynamicObstacle、ActorCollision 等。

Unity CharacterController Solver 在本 change 只实现 Float32 ABI并声明实际支持的 Local/Reconstructible 与 world feature，不声明 Snapshotable hidden Unity internals 或 DeterministicReplay。NumericProfile、ABI 或能力不匹配的 Driver/Model 组合在创建时失败。

### 11. 表现是投影和提交，不是模拟状态

Program 保存 producer identity、Timeline gameplay sampling 和 command identity，不保存 AnimationClip、Animancer state、Camera object 或 VFX reference。Projection 使用相同 producer identity 定位 Unity 资源。

EventId 由 Program operation、ActorId、activation identity、SimulationTick 和 local event sequence 稳定构成。Committer 接收 OutputPlan 的 `Publish/Replace/Retire/Suppress` lifecycle；本 change 的 Local Driver 只为新 EventId 产生 Publish。Kernel replay 本身不会触发外部端口。

Presentation adapter 在 Committer 边界把当前 target 的只读 sample 转为 Unity float。Animation selection、Timeline visual sampling 和 Animancer fade 继续在 PresentationFrame 推进；Presentation 不反向规定 Numeric Target。Gameplay Timeline time 和 MotionCurve 只在逻辑 Tick 推进。

### 12. 核心不迁移未确认的 ServerAuthoritative 行为

现有 `CharacterServerAuthoritativeBinding/Adapter` 直接依赖 NetworkSendStage、NetworkReceiveStage、ExternalPoseCorrection 和 MotionStage correction。将它机械改成新类型会把旧 correction/remote 语义伪装成最终 Driver。

本 change 删除这些 Character adapter/binding 和旧公共 stages。ServerAuthoritative packet/session/endpoint 等明确属于模型的代码可以保留，但 ModelDefinition 在缺少正式 Simulation Driver factory/actor binding 时必须报告 unavailable，不能进入 Inspector 可运行列表。Local Sandbox 不装配 Network SessionHost。

后续 ServerAuthoritative proposal 必须重新确认 prediction、authoritative observation、restore/replay、remote actor 和 OutputPlan policy，再实现唯一 model-owned Driver。核心不提供兼容桥。

### 13. Network Model 目录只认完整组合，不解释模型

Common SessionHost 只锁定 ModelDefinition、创建 model session 和注册 Simulation Driver composition。ModelDefinition 必须声明 Program、Driver、WorldSolver、Endpoint 和 Host 需要的能力，且只有完整实现才能被选择。

Common Host 不定义 packet、history、correction、rollback、snapshot recovery 或 remote presentation。Graph、Program 和 Character state 不保存 ModelId。

### 14. Corin 一次切换，不保留双 runtime

Compiler 与 Kernel operation 可以按任务顺序开发，但正式 Corin Sandbox 只在全部可达类型、state layout、Timeline、Action、GameplayEffect、Motion 和 Presentation 已迁移后切换。切换提交同时删除旧 runtime clone、旧 stages、旧字段和 migrator。

无法为某个 Corin 可达类型建立正式 Emitter、无法安全生成 Program artifact，或 Unity Solver 无法从明确 World state 重建时必须停止并说明缺口，不增加 interpreted fallback。

### 15. 通用 BTSMTL 解释器与 Character Preview 不得形成第二条角色主线

本 change 不要求删除所有非 Character 用途的 `RunnableTree` 或 `StateMachineGraphRuntime`。它们只能作为明确装配的通用 BTSMTL 工具存在，拥有自己的隔离 state，不能加载 Character Program、访问 CharacterSimulationState 或成为 stale artifact 的 fallback。

Timeline Editor 的 TreeClip Preview 也不能继续解释旧 runtime clone。需要执行 Gameplay 时，Preview target 必须提供匹配的 Program、Projection、隔离 Preview Session state、输入与 WorldSolver capability，并通过相同 Kernel 和 Session 四阶段推进。只查看动画时可以直接采样 Projection，但不得生成任何 Gameplay 事实。

`TimelineRunningTree` 名称在本 change 后只表示 TreeClip 的 authoring graph data。它不再拥有 Character gameplay runtime 初始化、playback clone 或隐藏节点状态；多 playback 共享不可变 Program operation，并通过 state address 隔离。

这样保留 BTSMTL 作为可复用 authoring/工具底座，同时保证角色 Gameplay 只有一套运行语义。代价是 TreeClip Preview 必须先完成编译并装配隔离 Session，不能再用临时 GraphContext 快速预览。

### 16. Session 使用不可变 ProgramCatalog 与启动时锁定 roster

`SimulationProgramCatalog` 按 ProgramId 稳定排序，并以 canonical ProgramId、ProgramHash、LayoutHash、NumericProfile、operation-set version 与 capability manifest 计算 CatalogHash。Catalog 中全部 Program 必须使用同一 TickRate、NumericProfile 和 ABI version，Session required world capabilities 取全部 Program requirement 的并集。每个 Actor roster entry 显式保存 ActorId、ProgramId、Character layout binding 与 World body binding；SessionRuntime 按该绑定选择 Program 执行，不假定全部 Actor 使用同一角色定义。

本 change 只支持 Session 启动前完成 roster composition，启动后 roster 与 ProgramCatalog 均不可变。Driver 看见只读 roster，不能在 TickPlan 中隐式加入未知 Actor。Snapshot 必须保存 CatalogHash 和全部 Actor Program binding，restore 只能恢复到完全匹配的 catalog/roster。

这让同一 world batch 可以容纳不同 Program 的角色，也避免后续网络模型私自维护 Actor 到 Program 的第二份映射。动态 spawn/despawn 仍需要额外设计 world-level roster transaction、初始化 state、body insertion/removal 与 rollback 语义，不在本 change 中偷偷预留半成品入口。

## Ownership Matrix

| 数据或策略 | 唯一所有者 |
|---|---|
| Graph/SM/Timeline/Action/Effect 数值无关语义 | Gameplay Semantic IR |
| 目标数值、Program ABI、constant lowering 与 codec | Numeric Target Compiler |
| 当前 Session 不可变执行规则 | CharacterSimulationProgram |
| Session 可用 Program 集与 Actor Program binding | SimulationProgramCatalog / SimulationSessionRuntime |
| 单 Actor Gameplay 可变状态 | CharacterSimulationState |
| 世界 body、碰撞和 Solver 可变状态 | WorldSimulationState / ICharacterWorldSolver |
| Evaluate/Finalize operation 顺序 | SimulationKernel |
| 多 Actor Tick 四阶段编排 | SimulationSessionRuntime |
| Tick plan、输入来源、typed ingress、restore 请求、外部 OutputPlan | ISimulationDriver 实现 |
| 网络 packet/history/reconciliation/rollback metadata | 具体 Network Model Driver |
| Unity 表现资源定位 | CharacterPresentationProjection |
| 表现与模型外部副作用 | SimulationCommitter 及其端口 |
| 成功、失败、restore 与 replay Trace | 各正式边界 / 只读 Diagnostics sink |

## Failure Policy

- ProgramHash、compiler version、operation-set version、NumericProfile、layout hash、TickRate 或 source revision 不一致：拒绝加载。
- 缺失 Emitter、断裂 reference、状态声明缺失、source literal 非法、target lowering 超范围或不支持 operation：编译失败并定位 source identity。
- Program、Kernel specialization、Input Adapter、WorldSolver 或 Snapshot codec 的 NumericProfile 不一致：组合创建失败，不做边界量化 fallback。
- Actor roster、World request/result identity 或 batch sequence 不一致：当前 Tick 整体失败，不提交部分 actor。
- Solver capability、world revision 或 static artifact identity 不满足 Driver 要求：组合创建失败。
- Snapshot 与 Program/solver/world/roster 不匹配：保持当前 state，不执行 partial restore。
- Driver OutputPlan 缺失 EventId、引用未知旧 EventId 或重复处置：当前 Tick 在 state publish 前失败。
- Committer port 在 state publish 后失败：Session fail-stop 并报告 EventId，不自动重试、回滚副作用或继续下一 Tick。
- 缺失 Projection 或 Committer：需要表现的 Unity Host 创建失败。
- 缺少正式 Simulation Driver 的 Network Model：不可选，不回退 Local Driver 或 LocalLoopback。
- 任何失败都不回退旧 interpreter、runtime compile、Transform 直写、默认 Solver、旧 NetworkStage 或 ExternalPose。

## Tradeoffs

### Semantic IR + Numeric Target

- 业务收益：本地与普通权威模式使用直接、易调试的 Float32 ABI；Rollback 以后可使用真正的 Fixed ABI；两者仍共享同一 Graph、operation 语义和 authoring source。
- 成本：Compiler、Program、State、Kernel、Solver 和 Snapshot contract 必须显式参数化目标，Program artifact 数量会按实际安装的 target 增加。
- 未选择的方案一：所有模式强制定点。它把 Rollback 的技术成本泄漏到单机、Unity 权威、Timeline、Blackboard 与 GE。
- 未选择的方案二：Float 与 Fixed 各写一套节点/Kernel。它会产生真正的业务分裂和长期行为漂移。
- 未选择的方案三：Program 内同时保存 float/fixed 并按 Driver switch。它让 Snapshot/Hash 含义不稳定，也无法在 Session 创建时证明 Solver ABI 匹配。

### 批量 WorldSolver

- 业务收益：未来多角色、服务端世界和 rollback snapshot 有统一边界。
- 成本：当前单角色 Unity CharacterController 也要经过 batch request/result，代码比直接 Move 多一层。
- 未选择的方案：每个 Character 自己调用 Solver。该方案无法明确世界顺序和原子快照。

### 核心结束时暂不提供网络模型

- 业务收益：不把旧 ExternalPose/correction 行为伪装成最终模型，核心合同不被未确认网络策略污染。
- 成本：现有 LocalLoopback 网络调试在 ServerAuthoritative follow-up 完成前不可运行。
- 未选择的方案：保留旧 adapter bridge。该方案违反单一路径要求，并会迫使后续再次迁移。

### 完整编译迁移而非包装

- 业务收益：纯 .NET、snapshot、source map 和多模型真正共享同一 runtime。
- 成本：变更范围大，所有 Corin 可达类型必须有 Emitter 和 State Layout。
- 未选择的方案：在旧 RunnableTree 外包接口。该方案仍依赖 Unity object 和隐藏状态。

### 不可变 ProgramCatalog 与锁定 roster

- 业务收益：同一 Session 可让不同角色 Program 共享一个 WorldSolver batch，Network Model 不需要维护第二份 Actor/Program 真相。
- 成本：动态加入、离开和换 Program 不能在本 change 内完成，Session 启动前必须确定 roster。
- 未选择的方案：只保存单一 ProgramHash。该方案能跑单个 Corin，却会让后续 PvE 或不同角色重新修改 Snapshot 和 Session contract。

## Deferred Network Decisions

以下问题不由本 change 决定，下游 proposal 必须分别确认：

- ServerAuthoritative 使用 Unity process 还是纯 .NET world backend；
- Owner 预测整个 Program 还是限定领域；
- authoritative observation 是完整 snapshot、领域状态还是事实流；
- correction 使用 restore/replay、领域修正还是 hard recovery；
- remote actor 是否执行 Program；
- Deterministic Rollback 使用受限 deterministic solver、完整 KCC 还是记录 BodyResult 的逻辑重演；
- rollback input delay、history window、confirmed horizon、desync recovery 和网络拓扑。

## Stable Dependency Contract

核心完成并归档后，下游只能依赖：

```text
CharacterSimulationProgram canonical bytes + ProgramHash + CapabilityManifest
Gameplay Semantic IR operation/source contract
NumericProfile + operation-set version + target ABI manifest
CharacterSimulationInput
SimulationIngress typed fact set
CharacterSimulationState codec/hash
WorldSimulationState codec + WorldSolver capability
SimulationWorldSnapshot atomic capture/restore
SimulationKernel.Evaluate / Finalize
SimulationSessionRuntime four-phase execution
ISimulationDriver tick-plan / restore / observe / output-plan boundary
ICharacterWorldSolver.ResolveBatch
SimulationTickResult + EventId command lifecycle
CharacterPresentationProjection + SimulationCommitter
Debug Source Map + structured Trace
```

下游不得修改 Semantic IR operation 语义、增加网络专用节点、复制业务 evaluator 或绕过 SessionRuntime 直接调用 Solver。下游 MAY新增正式 Numeric Target，但必须生成独立 ProgramHash/LayoutHash、提供完整 Kernel/State/Solver/Snapshot ABI，并通过同一 source map 追溯到原 Authoring。

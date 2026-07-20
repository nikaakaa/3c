## Context

当前 Simulation Core 已经能执行多 Actor roster，但 Unity composition 仍以 Character 为 owner：

```text
CharacterPipelineHost A -> Local Driver A -> Unity Solver A -> SessionRuntime A
CharacterPipelineHost B -> Local Driver B -> Unity Solver B -> SessionRuntime B
```

当前 Session 扩展也只有一个单 Tick Driver：

```text
Driver.PrepareTick -> one SimulationTickPlan
Driver.TryBuildRestoreRequest -> zero/one restore
SessionRuntime -> fixed Evaluate/Solve/Finalize
Driver.BuildOutputPlan -> one tick output
```

这能表达 Local，但不能干净表达一次外层 Tick 中的 restore + 多 Tick replay，也不能让第三方在不复制 SessionRuntime 的情况下增加 lag compensation、input validation、history capture、hash、snapshot export 或多阶段世界查询。

本设计把“稳定合同”和“具体处理步骤”拆开：

1. 外层 Session Host/Lifecycle/Tick/Commit 与 Numeric Target 无关。
2. Session Source 决定输入、endpoint、history 与模型资源从哪里来。
3. Pipeline Definition 显式决定运行哪些 Pass、顺序和产品依赖。
4. Program Runtime 决定 Program/State/Kernel/Snapshot 的 Numeric ABI。
5. Execution Backend 决定如何执行 compiled Pipeline plan。
6. WorldSolver 只负责声明范围内的世界求解。

`refactor-simulation-operation-runtime-modules` 处理的是 `SimulationKernel.Evaluate` 内部的 operation/control/Timeline/Action/GE/Motion 模块；本 change 的 Pipeline Pass 位于 Session 级，不能进入 operation evaluator，也不能把每个 BTSMTL operation 变成 Pipeline Pass。

## Target Architecture

```text
CharacterPipelineHost A ---- ActorRegistration A ----+
CharacterPipelineHost B ---- ActorRegistration B ----+---- SimulationSessionHost
                                                        |      CompositionDefinition
                                                        |        ProgramRuntimeDefinition
                                                        |        ExecutionBackendDefinition
                                                        |        PipelineDefinition
                                                        |        SessionSourceDefinition
                                                        |        WorldSolverDefinition
                                                        |             |
                                                        |             v
                                                        |      SessionPreparation
                                                        |             |
                                                        |      compiled PipelinePlan
                                                        |      immutable LaunchPlan
                                                        |             |
                                                        +---- Target-specific Composer
                                                                       |
                                                               RuntimeHandle
                                                                       |
                                                     GameplayTickSystem one logic target
```

本 change 安装的唯一可运行组合：

```text
Float32ProgramRuntimeDefinition
+ Float32PassExecutionBackendDefinition
+ StandardLocalSimulationPipelineDefinition
+ LocalSimulationSessionSourceDefinition
+ UnityCharacterControllerWorldSolverDefinition
-> Float32 target-specific Composer
-> Float32PassPipelineRuntimeHandle
```

后续组合示意：

```text
Float32 Program Runtime
+ Float32 Pass Backend
+ ServerAuthoritativePredictionPipeline
+ ServerAuthoritative Session Source
+ Unity Solver
```

```text
FixedQ32.32 Program Runtime
+ Deterministic Pass Backend
+ DeterministicRollbackPipeline
+ Rollback Session Source
+ Deterministic KCC
```

DotRecast 是服务端 Solver/Host backend，不是新的 Network Model，也不自动改变 Pipeline。

## Composition Ownership

### SimulationSessionCompositionDefinition

这是场景/Session 级显式配置，不进入 `CharacterPipelineDefinition`。它固定引用：

- 一个 Program Runtime Definition。
- 一个 Execution Backend Definition。
- 一个 Pipeline Definition。
- 一个 Session Source Definition。
- 一个 WorldSolver Definition。
- Session/World identity 与明确启动配置。

Host 不按 enum、类型名、已安装包、第一个可用对象或 Inspector 默认值猜测引用。缺少任一组成部分直接失败，不生成 Local、默认 Pipeline 或默认 Solver。

### Program Runtime Definition

Program Runtime Definition 只声明并创建特定 NumericProfile/Target ABI 的 ProgramCatalog、State、Kernel、Snapshot codec 与 Target services。它不决定 Pipeline 顺序、Network Model、Endpoint、Solver implementation 或 Presentation。

当前只安装 Float32。未来 Fixed 使用独立 Program/State/Kernel/Snapshot ABI，但通过相同外层 composition descriptor 与 runtime handle 接入。

### Execution Backend Definition

Execution Backend Definition 负责：

- 验证自己支持的 Pipeline schema、Program Runtime ABI 与 Solver capability。
- 将 portable Pipeline descriptor 编译为 backend-specific immutable plan。
- 创建并推进 Pipeline runtime。
- 执行 restore transaction、内部 working state、失败回滚与最终原子 publish。
- 返回 numeric-neutral runtime handle。

当前 `Float32PassExecutionBackend` 直接执行 C# Pass。未来 ECS/Burst 或其它实现可以提供新的 Backend，但不能忽略 Pipeline identity、绕过 Session commit、在外部私建 Tick runner 或重新解释 BTSMTL authoring。

### Session Source Definition

Session Source Definition 是 Session 外部资源入口：

- Local Source 创建 local input/source clock ports。
- Gameplay Network Model Source 创建 model session、endpoint、history 与模型专属 ports。

Source 不执行 Program operation、不调用 Solver、不播放 Presentation，也不在 Host 中隐藏插入 Pass。模型专属 Pipeline Definition 必须显式引用对应 Pass Definition；Pass factory 在 composition 时通过窄、强类型端口绑定 Source，Active 后不再查找服务。

### WorldSolver Definition

Solver Definition 只创建匹配 Program Runtime ABI 的 `ICharacterWorldSolver` 并声明真实 capability。Pipeline 的 WorldSolve Pass 使用该 Solver；其它 Pass 只能提交或消费正式 WorldRequest/Result 产品，不能直接修改 Transform、WorldSimulationState 或 Solver hidden state。

ServerAuthoritative 可以分别装配 Unity 或 DotRecast 服务端 Solver而不产生两个 Network Model。Rollback 必须装配声明 Snapshotable/DeterministicReplay 的匹配 KCC。

## Pipeline Model

### 四个阶段

Pipeline 固定只有四个顶层阶段，具体 Pass 列表可配置：

```text
Ingress -> Schedule -> Step loop -> Egress -> fixed Commit
```

- `Ingress`：把 local input、packet、authoritative observation 或 remote input 降低为正式 source products；不得直接修改 Character/World state。
- `Schedule`：唯一计划 producer 生成 `SimulationSessionExecutionPlan`；可输出 Pending、可选完整 restore directive 与零到多个 ordered step。
- `Step`：对 plan 中每个 SimulationStep 按顺序执行；标准 Pass 是 Program Evaluate、World ResolveBatch 与 Program Finalize。
- `Egress`：消费全部 step result，保存 history/snapshot/hash，生成外部 EventId disposition 与 source output；不得反向改写已完成的 Gameplay step。
- `Commit`：不作为可替换 Pass。Backend 在全部 plan、state、product 与 output 校验通过后原子发布最终 Character/World/Pipeline state，再由唯一 Committer 触发外部端口。

阶段边界固定是为了定义数据和失败事务，不代表固定具体 Pass。Pipeline 可以增加、替换或重排符合阶段合同的 Pass。

### SimulationSessionExecutionPlan

Schedule 阶段只允许一个独占 producer。Plan 至少包含：

- Source clock 与 outer tick provenance。
- `Pending` 或 executable 状态。
- 可选完整 restore snapshot identity。
- ordered SimulationStep sequence。
- 每个 step 的 SimulationTick、source identity、Actor input set、typed ingress 与 replay/current/authoritative provenance。
- 本次 plan 的 working-state、snapshot 与 output policy requirement。

示例：

```text
Local: [Current 21]

Prediction correction:
Restore 100
[Replay 101, Replay 102, Current 103]

Rollback:
Restore 80
[Replay 81 ... Replay 120]
```

GameplayTickSystem 仍只调用一次 runtime handle。多个内部 SimulationTick 由一个 Pipeline transaction 拥有，不能注册第二个 replay runner。

### Pass Definition 与 Runtime Pass

Pass Definition 是不可变配置与 factory identity，至少声明：

- PassId、implementation version、phase 与稳定配置 hash。
- consume/produce product contract。
- 独占 producer、append-only producer 或 readonly consumer 权限。
- NumericProfile、Target ABI、Pipeline Backend 与 Solver capability requirement。
- Forward、Replay、Restore、Authoritative 等执行支持。
- Stateless、Reconstructible、SnapshotParticipant 或 ExternalSource state class。
- 明确的 Source port requirement 与 diagnostics identity。

Pass Runtime 只获得 phase-specific context、声明过的产品端口和绑定完成的窄依赖。系统不提供万能 mutable context、字符串黑板、反射 registry、运行时 service lookup 或任意 callback。

### Pipeline Product

Pass 之间只通过 versioned product contract 传递数据。基础产品包括：

```text
CanonicalInputs
TypedIngress
ExecutionPlan
PendingActorEvaluations
WorldSolveBatchRequest
WorldSolveBatchResult
FinalizedStepResult
PipelineSnapshotContribution
OutputDispositionSet
SourceEgress
```

每个独占产品必须恰有一个 producer；append-only 产品必须声明稳定排序和 EventId/provenance。第三方需要新产品时，必须提供稳定 ProductId、schema version、owner、canonical identity 和 diagnostics shape，不能使用 `Dictionary<string, object>` 隐式传值。

### Pipeline Compiler

Session preparation 将 Pipeline Definition 编译为不可变 plan，并在 Active 前校验：

- phase 和显式顺序合法。
- 每个 required product 在使用前已由唯一合法 producer 产生。
- 没有重复独占 owner、未消费必需产品或环形依赖。
- Pass factory 已安装且版本精确匹配。
- Source ports、Program Runtime、Backend、Solver、Snapshot codec 与 Pass requirement 完整匹配。
- replay/restore/deterministic requirement 被完整组合支持。
- PipelineId、Revision、PipelineHash 与 Backend semantic version 可稳定计算。

编译失败时 Session 不创建 Runtime，也不删除不认识的 Pass、跳过 unknown product 或选择默认 Pipeline。

## Pipeline State And Snapshot

Pass 的状态分为四类：

- `Stateless`：当前调用结束后不保存可变数据。
- `Reconstructible`：可从正式 Character/World/Source identity 重建，并声明重建规则。
- `SnapshotParticipant`：影响未来模拟、restore、replay、hash 或 output disposition，必须提供 canonical capture/restore/hash。
- `ExternalSource`：transport queue、socket、外部 authoritative history 等不属于 Gameplay snapshot 的 Source 状态；它不能被 Pass 当作可回滚 Character/World state。

Backend 以 `SimulationPipelineStateSnapshot` 聚合全部 SnapshotParticipant 的 PassId/version/state payload，并与 Character/World snapshot 在一个 Session restore transaction 中校验和恢复。Snapshot、Session descriptor 与网络握手必须包含 PipelineHash、BackendId/semantic version；ProgramHash 本身不包含 Pipeline，因为同一 Program 可被多个合法 Pipeline 使用。

首次启动也必须有真实 Pipeline state。`SimulationPipelineInitialStateSource` 只有两种显式模式：

- `CaptureActivatedDefaults`：Backend 激活全部 Pass、完成 reconstruct与Solver重建后，从实际 SnapshotParticipant捕获 Tick 0 canonical state。
- `RestoreProvidedSnapshot`：Backend恢复调用方给定的完整 Pipeline snapshot，再重新捕获并要求 SnapshotHash完全一致。

空 participant列表只表示当前 Pipeline确实没有 SnapshotParticipant，不再被用来替代未来有状态 Pass的初始数据。

如果一个 Pass 影响未来结果却没有声明 snapshot/reconstruct 责任，Pipeline composition 必须失败。禁止把该状态藏在 MonoBehaviour、static、closure 或 Endpoint adapter 中。

## Standard Local Pipeline

本 change 安装的唯一 Local Pipeline：

```text
Ingress
  LocalInputIngressPass

Schedule
  LocalSingleStepSchedulePass

Step
  Float32ProgramEvaluatePass
  Float32WorldResolveBatchPass
  Float32ProgramFinalizePass

Egress
  LocalImmediateOutputPass
```

`LocalSingleStepSchedulePass` 将一个 LocalLogicTick 映射为一个 SimulationTick。Local Pipeline 不建立 endpoint、history、correction、restore 或 replay。当前固定 `SimulationSessionRuntime` 的 state atomicity、stable Actor order、world batch、EventId 与 immediate Publish 结果必须迁入这些 Pass/Backend 后保持不变。

Preview 使用独立 Preview Source 与 Preview Pipeline Definition，但复用同一个 Float32 Program Runtime、Pass Backend 和标准 Step Pass；它不借用 active gameplay Session 的 mutable state。

## Session Preparation And Lifecycle

Session Host lifecycle 固定为：

```text
Uninitialized -> Preparing -> Active -> Failed -> Disposed
```

- `Uninitialized`：只持有五个显式 Definition 和 Actor registrations。
- `Preparing`：Source 可创建 endpoint/model session、等待 handshake/roster；Pipeline compiler 可解析 Pass factory 和 capability，但不得运行 Program。
- `Active`：ProgramCatalog、Pipeline plan/hash、Backend、Source ports、Solver、Snapshot codec、Committer、World、roster 与 diagnostics identity 全部锁定。
- `Failed`：释放已创建资源，不切换 Local、Pipeline、Backend、Solver 或 Endpoint。
- `Disposed`：按 Runtime/Pass -> Source/Endpoint -> Solver -> Actor/Presentation registration 顺序释放。

`SimulationSessionLaunchPlan` 至少包含：

- Session identity、source clock 与 TickRate。
- Program Runtime identity、NumericProfile、Target ABI 与 operation-set identity。
- BackendId、semantic version、PipelineId/Revision/Hash 与 compiled plan。
- ProgramCatalog identity 与完整 Actor roster。
- Source ports、Solver、Snapshot codec、Committer 与 diagnostics identity。
- initial Character/World/Pipeline state identity。
- 可选 Model/Endpoint identity。

Preparing 只由 GameplayTickSystem 正式 Session target 推进，不创建私有 `Update`、协程、Task loop 或 Network runner。

## Numeric And Backend Boundary

公共 Host 只持有 numeric-neutral `ISimulationSessionRuntimeHandle`：

```text
Descriptor
LogicTick(source context)
Dispose()
```

它不暴露 Program、Character/World/Pipeline state、Source、Solver、Snapshot 或 scalar/vector 类型。

Target-specific Composer 负责把 Program Runtime、Backend、compiled Pipeline plan、Source ports、Solver 与 Committer 强类型装配。当前 Float32 Composer 只接受 Float32 Program Runtime 和兼容的 Float32 Backend/Solver。未来 Fixed Composer 使用自己的内部 ABI，但返回相同外层 handle。

当前唯一正式 Float32 Composer 位于 `ThirdPersonSimulation.Float32` portable source set。它负责 Pipeline compile、composition descriptor、snapshot codec、Launch Plan和Backend request；Unity `UnityFloat32SimulationSessionComposer`只负责把五个显式 Definition、Unity Solver实例、Source ports与Actor registration适配为 portable request。普通 .NET Authority Host直接调用同一个 portable Composer，不复制 Unity装配代码。

同一 Numeric Target 可以安装多个 Backend；同一 Backend 也可以支持多个 Pipeline Definition。系统不使用 `object` state、dynamic、反射调用、Float/Fixed conversion adapter 或运行时 backend switch。

## Actor Registration And Presentation

`CharacterPipelineHost` 只建立不可变 Actor registration：

- 显式 ActorId。
- Program asset/canonical artifact identity。
- Presentation Projection。
- 可选 local input port。
- World body binding。
- Presentation/output ports。
- Diagnostics metadata。

Session Host 在 Active 前消费 registrations 并建立 roster。重复 ActorId、Program/Projection mismatch、Pipeline/Source/Solver 所需端口缺失都在 Runtime 创建前失败。Active 后 roster 不可变。

Actor activation是事务：input、diagnostics和presentation target任一步失败，都逆序释放已经取得的资源。Session fail/dispose无论某个 unregister或Dispose是否抛错，都继续尝试释放其余 Runtime/Pass、Source、Solver与Actor资源，并保留最初的业务失败作为 Session Failure。

`SimulationOutputDisposition`显式携带ActorId。Commit batch按实际 GameplayFact/PresentationCommand核对 EventId与ActorId；Unity output aggregate直接按ActorId路由，不维护跨Tick的无界EventId字典。Local Egress只产生Publish；需要Replace、Retire或Suppress历史语义的模型Egress必须以SnapshotParticipant journal验证历史EventId，使restore/replay后仍得到同一处置结果。

Session Host 注册一个 Input/Logic target；同一 Session 每个 LocalLogicTick 只推进一次 runtime handle。Presentation Runtime 仍按 Actor 独立，因为动画、相机和 visual root 不属于 Simulation Session。PresentationFrame 只消费 committed samples/commands，不读取 Pipeline Definition、Source、Solver 或 working state。

## Formal Target Program Artifact

`.csir` 是所有 Numeric Target 共用的 semantic artifact；`.csim` 是具体 Numeric Target 的 canonical executable Program artifact。

正式路径：

```text
Library/CharacterSimulation/Programs/
  <definition-guid>/
    <numeric-profile-id>-abi<version>.csim
```

Build transaction：

```text
validated .csir
-> Target lowering
-> canonical .csim bytes
-> temp write + flush
-> re-read + identity validation
-> Projection build/identity validation
-> ProgramAsset exact-byte wrapper
-> atomic publish
```

任一步失败时恢复旧完整组合。Unity Player 只从 ProgramAsset 内嵌 bytes 加载；普通 .NET Host 直接消费 `.csim`。Pipeline identity 不写入 `.csim`，由 Session composition 独立锁定。

## Network Model Plugin Boundary

`GameplayNetworkModelDefinition` 是一种 Session Source Definition。完整模型模块可以提供：

- Endpoint/model session/source ports。
- 模型专属 Pipeline Pass Definition/Factory。
- 一份或多份显式 Pipeline Definition。
- Program Runtime、Backend、Solver、Snapshot 与 protocol capability requirement。

模型不能在 Common Host 中隐藏插入 Pass。Composition 必须显式选择 Pipeline，Pipeline compiler 必须验证它与当前 Model Source 匹配。Packet、history、correction、rollback、hash 和 ack 不进入 Common Host、Character registration 或 Program。

ServerAuthoritative correction 与 Deterministic Rollback 后续可各自交付完整 Pipeline：

```text
PredictionCorrectionPipeline
  authoritative ingress
  correction schedule
  restore/replay plan
  standard step passes
  prediction history/output
```

```text
DeterministicRollbackPipeline
  canonical input merge
  divergence schedule
  restore/replay plan
  deterministic step passes
  hash/confirmed output
```

两者复用 Host、Pipeline descriptor、transaction 与 outer handle，但不共享模型状态、Endpoint、Program ABI 或 Snapshot payload。

## Failure Policy

- Program/Projection/`.csim` identity 不一致：composition 创建失败。
- Program Runtime、Backend、Pipeline Pass、Source、Solver、Snapshot codec 的 NumericProfile/ABI/capability 不一致：首 Tick 前失败。
- Pipeline product 缺失、重复 owner、unknown Pass/version、非法 phase/order 或 state ownership 不完整：Pipeline compile 失败。
- Schedule 产生未知 Actor、重复 Tick、非法 restore 或不连续 replay plan：当前 outer Tick 失败且不发布 working state。
- Stateful Pass snapshot 缺失或 restore/hash identity 不匹配：restore 拒绝。
- Model factory、Endpoint、Source port 或 preparation 缺失：Model 不可选择。
- Active 后修改 Composition、Pipeline、Pass config、roster、Program、Backend、Solver 或 Source：拒绝热切换并停止当前 Session。
- 任一阶段失败：不回退 Local、默认 Pipeline、旧 SessionRuntime、Transform 直写或其它 Backend。
- `.csim` 写入或 Unity asset publish 失败：恢复旧完整 artifact 组合。

## Migration And Deletion

完成后删除：

- `CharacterPipelineHost` 内创建 Local Driver、Unity Solver、Kernel、SessionRuntime 与 Logic target 的代码。
- 旧单 Tick `ISimulationDriver`、`SimulationTickPlan` 单计划入口和固定 `SimulationSessionRuntime`。
- `LocalCharacterSimulationSessionTickTarget`。
- `SimulationDriverCompositionPart` 与 `SimulationDriverCompositionCapability`。
- 旧 `GameplayNetworkSessionHost`。
- Preview/Local/Network 各自直接构造 SessionRuntime 的路径。
- Program 手工 `.csim` 导出路径和 ProgramAsset 独立 re-encode 路径。
- Corin 场景中的每角色 Session ownership 与重复 Tick registrations。

不保留 facade、兼容 MonoBehaviour、旧 Driver wrapper、默认 Local/Pipeline fallback 或双写 artifact。

## Decisions And Tradeoffs

### 采用 phase-based Pipeline Pass，而不是只扩展 Driver

- 收益：prediction、reconciliation、rollback、validation、history、hash 和 snapshot export 可作为差异 Pass 组合，标准 Evaluate/Solve/Finalize 只保留一份。
- 成本：需要 product ownership、Pipeline compiler、Pass identity、state snapshot 和 diagnostics，基座范围显著增加。
- 业务取舍：项目明确要对比多种网络模型；单 Tick Driver 无法表达多步 replay，只能把复杂度推到隐藏 runner 或复制 Runtime。

### 固定四个阶段和 Commit，而不是完全自由的 callback 链

- 收益：能定义输入、计划、内部 step、外部输出和失败原子性；用户仍可在阶段内增加、替换和排序 Pass。
- 成本：跨阶段任意跳转或自定义第五阶段需要扩展 versioned Pipeline schema，不能即时拼接任意代码。
- 业务取舍：完全自由 callback 对小型 Demo 上手快，但无法可靠回答 replay 时是否重跑、状态是否进入 Snapshot、谁拥有 World mutation 等网络正确性问题。

### 保留完整 Execution Backend 扩展，而不是每个模型复制 Backend

- 收益：普通网络差异复用 Pass Backend；ECS/Burst/不同执行技术仍有正式替换入口。
- 成本：Composition 多一个 Backend 维度，兼容矩阵和身份校验更复杂。
- 业务取舍：ServerAuthoritative 与 Local 通常共享 Float32 Step 实现，不值得复制；真正更换状态布局和执行技术时仅靠 Pass 又不够。

### Pipeline 由 Composition 显式选择，而不是 Model 隐藏注入

- 收益：Inspector、diagnostics、handshake 和审查都能看到实际 Pipeline；同一 Model 可以明确提供不同实验 Pipeline。
- 成本：配置项更多，Model 包必须同时交付合法 Source 与 Pipeline 资产。
- 业务取舍：隐藏注入会让“选了哪个模型”无法解释实际执行步骤，也会重新形成代码决定配置的分裂路径。

### Pipeline 在 Active 前编译和锁定，而不是运行中热插拔

- 收益：PipelineHash、Snapshot、Replay 与 peer handshake 稳定；hot path 不做反射查找和顺序解析。
- 成本：切换实验方案需要销毁并重建 Session。
- 业务取舍：当前 Demo 不需要不停机切换网络模型，稳定可审查比运行时热插拔更重要。

### ProgramHash 与 PipelineHash 分离

- 收益：同一 Corin Program 可在 Local、ServerAuthoritative 与 Rollback Pipeline 中复用；Pipeline 差异仍进入 Session/Snapshot/handshake identity。
- 成本：Session identity 不能只比较 ProgramHash，诊断和协议需要多一个 PipelineHash。
- 业务取舍：把 Pipeline 写入 ProgramHash 会迫使每种网络模型重新编译角色业务 Program，违背网络模型与 BTSMTL authoring 解耦。

### Local 是 Session Source，不是 Network Model

- 收益：单机不需要 endpoint/model session，也不会为通用性伪造网络配置。
- 成本：Local 与 Network Model 有不同 Source Definition。
- 业务取舍：二者共享 Pipeline composition，但业务语义不同；把 Local 命名成 Network Model 会污染 Inspector 和失败策略。

### Active 前锁定 roster，不在本 change 实现动态 roster

- 收益：Pipeline plan、WorldSnapshot 和 stable Actor ordering 可直接锁定。
- 成本：网络模型必须先完成 handshake/roster，运行中 join/leave 需要后续正式扩展。
- 业务取舍：动态 roster 同时改变 ProgramCatalog、Solver state、Snapshot hash、Presentation 和协议，不应夹带进本次基座。

## Downstream Order

```text
refactor-character-simulation-core
refactor-character-semantic-frontend-artifact
refactor-gameplay-session-composition-boundary
        |
        +-> refactor-server-authoritative-hybrid-runtime
        |       +-> add-dotrecast-authoritative-server-backend
        |
        +-> add-deterministic-rollback-kcc-model
```

`refactor-simulation-operation-runtime-modules` 与本 change 语义正交，但实施必须串行，避免同时编辑 `SimulationKernel` 调用点。基座归档后，ServerAuthoritative correction 与 Deterministic Rollback 可并行实现各自 Source/Pass/Pipeline/Backend/Solver 模块；最终都只能通过同一个 SimulationSessionHost 和 Pipeline composition 接入。

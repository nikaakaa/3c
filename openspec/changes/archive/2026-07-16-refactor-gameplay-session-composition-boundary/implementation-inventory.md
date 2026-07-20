# Gameplay Session Composition 实施盘点

## 基座状态

- `refactor-character-simulation-core`、`refactor-character-semantic-frontend-artifact` 与 `refactor-character-pipeline-definition-config-boundary` 已归档。
- `refactor-simulation-operation-runtime-modules` 已完成，唯一 `Float32OperationEvaluator` 位于单 Actor Program Evaluate 内部；Session Pipeline 不进入 operation evaluator。
- Graph、StateMachine、Timeline、Blackboard、Action、GameplayEffect、Motion 与 Animation 的业务语义和 Corin Program identity 未因本 change 改变。
- 当前只安装 Float32 Local 与隔离 Preview 组合；Fixed、DotRecast、Prediction、Authority 与 Rollback 组合不建立空实现或 selectable fallback。

## 正式所有权

| 模块 | 唯一 owner |
|---|---|
| portable Runtime/Source/Solver descriptor、Pipeline identity、product、plan、snapshot 与 outer runtime handle | `ThirdPersonSimulation.Core` |
| Float32 Program Runtime、唯一 portable Composer、State、Kernel、标准 Step Pass、Pipeline transaction/backend/runtime handle | `ThirdPersonSimulation.Float32` |
| Program Runtime、Execution Backend、Pipeline、Session Source、WorldSolver Definition | Unity Simulation composition 模块 |
| Session preparation、compiled runtime handle、locked roster、Input/Logic target、World state 与销毁顺序 | `SimulationSessionHost` |
| Program/Projection、Input、Body、Visual/Camera、Diagnostics 与 Presentation ports | `CharacterPipelineHost` 创建的 `CharacterSimulationActorRegistration` |
| `.csir` store、正式 `.csim` store 与 Program/Projection publish transaction | CharacterSimulation Editor build 模块 |
| Pipeline/Composition Inspector | CharacterSimulation Editor 模块 |

`CharacterPipelineHost` 不创建 Session Source、Kernel、WorldSolver、Execution Backend、Pipeline runtime、Commit aggregate 或 Logic target。场景内每个 Session 只有一个 `SimulationSessionHost` 注册 GameplayTickSystem Input/Logic target；每 Actor 只注册独立 Presentation target。

## 五项显式 Composition

每个 `SimulationSessionCompositionDefinition` 必须显式引用：

1. `SimulationProgramRuntimeDefinition`
2. `SimulationExecutionBackendDefinition`
3. `SimulationPipelineDefinition`
4. `SimulationSessionSourceDefinition`
5. `SimulationWorldSolverDefinition`

Inspector 只显示引用与正式 compatibility compiler 结果，不创建默认资产、不扫描类型、不静默修复、不重排 Pass。Active 前必须得到 immutable `SimulationSessionLaunchPlan`、compiled Pipeline plan 与 outer runtime handle。

## Standard Local Pipeline

Corin Local composition：

| 项目 | 值 |
|---|---|
| SessionId | `Corin.Sandbox.Local` |
| WorldId | `Sandbox.World` |
| WorldRevision | `Sandbox.World.v1` |
| SourceClockId | `Sandbox.LocalLogic` |
| TickRate | `60` |
| Program Runtime | Float32 IEEE754 ABI 1 |
| Execution Backend | Float32 Pass Backend |
| Pipeline | Standard Local Pipeline |
| Session Source | Local Source |
| WorldSolver | Unity CharacterController Solver |

Pipeline identity：

| 项目 | 值 |
|---|---|
| PipelineId | `thirdperson.simulation.pipeline.standard-local` |
| Revision | `1` |
| Schema | `1` |
| PipelineHash | `ae6ecdc9adb8d3ed997ffd1ae1ffb79882ffd26ffbffd6ebf740d582d5c7fa5d` |
| DescriptorHash | `0ecb35f1c0744877cb0676fb201c65873457009a21b8452b709387a910968a42` |
| PlanHash | `81c0b27b0d78446ea12a3366b65f92772a6fb1076be4cc262de3e8f53334291c` |

Pass 顺序：

```text
Ingress  thirdperson.simulation.local-input-ingress@1
Schedule thirdperson.simulation.local-single-step-schedule@1
Step     thirdperson.simulation.float32-program-evaluate@1
Step     thirdperson.simulation.float32-world-resolve-batch@1
Step     thirdperson.simulation.float32-program-finalize@1
Egress   thirdperson.simulation.local-immediate-output@1
```

Local Input/Schedule/Egress 只属于 Standard Local Pass。Program Evaluate、World ResolveBatch 与 Program Finalize 是正式共享 Step Pass。一次 outer LogicTick 产生一个 Current step，Egress 将新 EventId 全部标记为 Publish，随后统一 atomic Commit。

Float32 composition正式入口为 portable `Float32SimulationSessionComposer.Compose`。Unity adapter不再编译Pipeline或构造Launch Plan；普通 .NET Authority Host可用相同 request合同直接装配。Backend激活Pass后按`CaptureActivatedDefaults`捕获真实Tick 0 Pipeline state；后续有状态模型也可显式选择`RestoreProvidedSnapshot`，恢复后必须重新捕获出相同SnapshotHash。

OutputDisposition包含EventId与ActorId。Commit batch在state publish前核对两者与实际输出一致，Unity aggregate只按Actor路由，不再永久保存每个已发布EventId。需要跨Tick判断Replace/Retire/Suppress的模型必须由自己的SnapshotParticipant Egress journal保存有界历史。

## Preview

Corin Preview 使用独立 `CorinPreviewSimulationSessionComposition`、Preview Source 与 Preview Pipeline，但复用同一 Float32 Program Runtime、Pass Backend、标准 Step Pass 和 Unity Solver Definition。Preview 拥有隔离 input、hidden body、World state、runtime handle、output 与 diagnostics，不读取或修改 Active gameplay Session mutable state。

## Corin Program 身份

| 项目 | 当前值 |
|---|---|
| Definition GUID | `c7a7c1e3f7e64d81b5a04a90cbeb8d4e` |
| ProgramId | `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e` |
| Compiler | `character-simulation-compiler/13` |
| Operation Set | `character-gameplay-operations/3` |
| SourceRevision | `2840b8b1ad6240f3a95808da7251041c258045949a8466b5d2bc9d3891546eb3` |
| SemanticHash | `f6785b6c35dd3b32baf2b131dd16468d5e093fb88085a41055143e60abeb004e` |
| ProgramHash | `5f39ddaeb5b39290657e5e162de75e9e6b130c2de275b64acf2e7b60e22b39aa` |
| LayoutHash | `0618222660eaf877db0331ceee8056060b914614d1a6e1d234bf9c30b4215d6e` |
| NumericProfile / ABI | `float32-ieee754` / `1` |
| Canonical bytes | `1,285,649` |
| Canonical bytes SHA-256 | `3314aedca2d7d253702ea12df65f634598b8b88581d813b168e3755b5827ee9d` |
| Operations / State slots / Producers / Source map | `485 / 804 / 16 / 2636` |
| SourceMapContentHash | `0d2468714ced0c6741ad3ff938bc112a4e12deb15adbd8397234a13888fb5748` |
| ActorId / Body binding | `LocalActor` / `Corin.LocalBody` |

PipelineHash 不进入 ProgramHash。ProgramAsset、Projection 与正式 `.csim` 保持同一 Program/Source/Producer identity；PipelineHash、PlanHash、Source、Backend、Solver 与 roster 进入 Session descriptor、snapshot、handshake 和 diagnostics identity。

## 正式 Target Artifact

正式文件：

```text
Library/CharacterSimulation/Programs/c7a7c1e3f7e64d81b5a04a90cbeb8d4e/float32-ieee754-abi1.csim
```

`CharacterSimulationBuildOrchestrator` 先通过 `CharacterTargetProgramArtifactStore.Stage` 写入并重读校验 `.csim`，再让 ProgramAsset exact-byte 包装 transaction commit 返回的同一 canonical bytes，同时发布 Projection 与 Definition 引用。当前 Corin `.csim` 和 ProgramAsset 内嵌 bytes 均为 1,285,649 字节，逐字节相同，SHA-256 均为 `3314aedca2d7d253702ea12df65f634598b8b88581d813b168e3755b5827ee9d`。

Unity Player 只读取 ProgramAsset wrapper，不读取 `Library` 或 `.csir`。普通 .NET host 通过 portable Reader/loader 读取正式 `.csim`。

## 下游扩展入口

### ServerAuthoritativeHybrid

- 复用公共 Host、Actor registration、`.csim`/ProgramAsset loader、Float32 Program Runtime、Pass Backend、Pipeline compiler、标准 Step Pass 与 atomic Commit。
- 只新增 Prediction/Authority Session Source、phase-specific Pass、两个显式 Pipeline、Fantasy Endpoint/Room、Unity Authority Worker、baseline merge/restore/replay、EventId disposition、remote presentation 与 Demo 配置。
- Prediction 与 Authority PipelineHash 可以不同，但必须由 model protocol 声明显式兼容 pair；Authority snapshot 不能伪装成 Prediction snapshot。

### DotRecast

- 复用 ServerAuthoritative Authority Pipeline descriptor/compiled plan与公共 composition descriptor。
- 只新增 NavigationSurfaceArtifact、DotRecast Solver、纯 .NET Authority Worker Runner及 Program Runtime/Backend/Solver launch组合。
- 不创建第二 Network Model、Pipeline或 Session Host。

### Deterministic Rollback

- 复用唯一 SimulationSessionHost、Actor registration、portable Pipeline descriptor/compiler、composition descriptor、outer runtime handle、operation topology/control runtime和同一 Semantic IR。
- 只新增 Fixed Program Runtime、Deterministic Backend、Rollback Source/Pass/Pipeline、Fixed Snapshot、KCC、协议与 Demo。
- 不复制公共 Host、Actor registration、Pipeline compiler、Runnable/Composite/StateMachine语义或 Float32 ABI。

## 已移除旧所有权

- 单 Tick `ISimulationDriver`、`LocalSimulationDriver`、`PreviewSimulationDriver` 与 TickPlan/restore/OutputPlan入口。
- 固定 `SimulationSessionRuntime`。
- 每 Actor `LocalCharacterSimulationSessionTickTarget`。
- `GameplayNetworkSessionHost`。
- `SimulationDriverCompositionPart` 与 `SimulationDriverCompositionCapability`。
- Character Host/Preview直接构造 Kernel、Solver、Session、Commit aggregate和 Logic target的路径。
- ProgramAsset从 Program对象再次独立 encode与人工 `.csim` writer。

最终静态清理与编译结果在 tasks 15 记录；发现任何旧入口仍可实例化时，不保留兼容 wrapper，直接删除并修复正式引用。

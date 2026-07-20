# Change: 将 Gameplay Session 收口为可组合执行管线与网络模型基座

## Why

`refactor-character-simulation-core` 已建立 Program、State、Kernel、WorldSolver、Snapshot、OutputPlan 与 EventId 合同，`refactor-character-semantic-frontend-artifact` 也已把 authoring 编译链收口为 `.csir -> Numeric Target Program`。但当前 Unity 运行装配仍以 Character 为 owner：每个 `CharacterPipelineHost` 都会创建自己的 `LocalSimulationDriver`、`UnityCharacterControllerWorldSolver`、`SimulationKernel` 与 `SimulationSessionRuntime`。这既不能形成多 Actor 的唯一 world batch，也不能让网络模型在不修改 Character Host 的情况下接管完整 Session。

更深的缺口是当前扩展边界只允许更换一个单 Tick Driver。现有 `ISimulationDriver` 每次只能产生一个 `SimulationTickPlan`、一个可选 restore request 和一个 `SimulationOutputPlan`，`SimulationSessionRuntime` 则把 `Evaluate all -> ResolveBatch -> Finalize all -> publish -> commit` 写成固定实现。Local 可以运行，但 owner prediction/reconciliation 需要“恢复权威快照 -> 重放多个未确认 Tick -> 执行当前 Tick -> 只发布最终结果”，Deterministic Rollback 需要“合并迟到输入 -> 恢复旧世界 -> 重放一段 Tick -> 计算 hash -> 按 confirmed horizon 提交”。若继续只扩展 Driver，下游只能在 Runtime 外私建循环、修改公共 Runtime 或复制完整 Session，都会产生分裂路径。

另一方面，把每个网络模型都实现成一套完整 Backend 也会复制 Program Evaluate、World Solve、Finalize、Snapshot 与 Commit。正确边界应当同时支持两种扩展：普通网络差异通过正式、可校验、可组合的 Pipeline Pass 表达；执行技术体系完全不同的实现通过完整 Execution Backend 表达。公共系统固定的是 Session lifecycle、事务、身份和提交合同，不是固定的 Pass 清单。

此外，Float32 Program 已有 canonical codec，Unity `CharacterSimulationProgramAsset` 也保存 canonical bytes，但正式 build transaction 仍只发布 Unity Asset；`Library/CharacterSimulation/Program/*.csim` 不是由正式 store 管理的 Target artifact。普通 .NET 服务端若现在开始实现，只能依赖人工导出或重新包装 Program bytes。

如果直接 apply `refactor-server-authoritative-hybrid-runtime` 或 `add-deterministic-rollback-kcc-model`，它们将被迫复制 Session Host、Pipeline、artifact loader 或 Runtime 构造。本 change 必须先建立唯一正式基座。

## Dependencies

- `refactor-character-simulation-core`、`refactor-character-semantic-frontend-artifact` 与 `refactor-character-pipeline-definition-config-boundary` MUST已完成并归档。
- `refactor-simulation-operation-runtime-modules` 只负责单 Actor `SimulationKernel.Evaluate` 内部的 operation/control/领域模块，不负责 Session Pipeline；两个 change MAY按任一顺序串行实施，但 MUST不并行修改同一 Kernel 调用点，且不得互相复制职责。
- `refactor-server-authoritative-hybrid-runtime`、`add-dotrecast-authoritative-server-backend` 与 `add-deterministic-rollback-kcc-model` MUST在本 change 归档后再进入最终 Session 集成。
- 本 change 只安装 Float32 Program Runtime、Float32 Pass Execution Backend、Local Session Source、标准 Local Pipeline 与 Unity CharacterController Solver；Fixed、DotRecast 和具体 Network Model Pipeline 不提前建立空实现。

## What Changes

- 新增唯一 `SimulationSessionHost`，作为 Unity 场景中 Session preparation、composition、Pipeline runtime handle、Actor roster、Tick target 与销毁顺序的唯一 owner。
- 新增显式 `SimulationSessionCompositionDefinition`，固定引用 Program Runtime Definition、Execution Backend Definition、Pipeline Definition、Session Source Definition 与 WorldSolver Definition；Host 不通过 enum、类型扫描、默认值或已安装包猜测组合。
- 将 Numeric Target 与 Execution Backend 分离：Program Runtime 只拥有 Program/State/Kernel/Snapshot ABI，Execution Backend 只负责编译和执行 Pipeline；同一 Float32 Program Runtime MAY与不同 Backend 组合，未来 Fixed 也不污染公共 Host。
- 新增 `SimulationPipelineDefinition`、phase-specific Pass Definition/Factory、portable Pipeline descriptor 与不可变 compiled Pipeline plan。Pipeline 固定分为 Ingress、Schedule、Step、Egress 四个阶段，阶段内 Pass 可显式增加、替换和排序。
- 新增零到多步 `SimulationSessionExecutionPlan`。唯一 Schedule producer 可声明 Pending、可选完整 restore directive 和 ordered SimulationStep sequence；Backend 在一次外层 LogicTick 中可以执行零个、一个或多个内部 SimulationTick。
- 新增正式 Pipeline product contract。每个 Pass 必须声明 consume/produce、唯一写入者或 append-only 规则、Numeric/ABI/Backend/Solver requirement、Replay policy、state ownership 与稳定 PassId/version；不得使用字符串黑板、反射回调、全局 service locator 或未声明 mutable state 传值。
- 新增 Pipeline state snapshot/hash 合同。任何影响未来模拟、restore、replay 或 output disposition 的有状态 Pass 必须提供 canonical capture/restore/hash；transport queue、外部 endpoint 与纯 diagnostics state 必须明确声明为 Session Source 外部状态，不能伪装成可回滚 Gameplay state。
- 将 Program Runtime、Source 与 Solver 的运行描述合同以及唯一 Float32 Composer 放入 portable source set。Unity Composer 只把显式资产、Actor registration、Source、Solver和输出端口转成 portable request；Pipeline compile、Session descriptor、初始 Pipeline state与 Backend runtime创建只发生在 portable Composer/Backend。
- 初次启动不伪造空 Pipeline snapshot。Backend 必须在全部 Pass runtime 激活后捕获 SnapshotParticipant 默认状态，或按显式 Restore 模式恢复给定 snapshot并核对 canonical hash，再生成 Launch Plan。
- 新增 `SimulationExecutionBackendDefinition` 与 numeric-neutral `ISimulationSessionRuntimeHandle`。当前只安装 `Float32PassExecutionBackend`；后续 ECS/Burst 或其它完整执行技术 MAY实现新的 Backend，但必须消费同一 versioned Pipeline descriptor、遵守相同 Session transaction/output 合同并返回相同外层 handle。
- 将 Local Driver 重构为正式 `LocalSimulationSessionSourceDefinition` 及 Local Pass 端口。Local 仍是普通 Simulation Source，不伪装成 Network Model。
- 安装唯一标准 Local Pipeline：Local input ingress、single-step schedule、Program evaluate、world batch solve、Program finalize 与 immediate output。它迁移当前可运行行为，但不再由固定 `SimulationSessionRuntime` 私有写死。
- 将 `GameplayNetworkModelDefinition` 收口为 Network Model Session Source：它创建 endpoint/model session/history/source ports，并声明 Pipeline/Backend/Target/Solver requirement；具体模型通过自己的 Pipeline Definition 显式引用模型 Pass，不得在 Host 内隐藏注入。
- 新增 `ISimulationSessionPreparation` 与不可变 `SimulationSessionLaunchPlan`。Local 可立即 Ready；后续 Network Model 可在 Preparing 完成 handshake、roster 与 endpoint 准备，但 Active 前必须返回完整且锁定的 Source ports、Pipeline plan 与 composition identity。
- 将 `CharacterPipelineHost` 降为 Actor registration 与 Presentation owner：它加载 Program/Projection，建立显式 ActorId、Input/Body/Presentation/Diagnostics ports，但不再创建 Source、Solver、Kernel、Pipeline Runtime、Committer aggregate 或 Logic target。
- 将 OutputDisposition 的 ActorId 作为正式因果字段；Backend 在 commit batch中核对 EventId与Actor归属，Unity output aggregate只负责路由，不保存无界 EventId owner历史。需要 Replace/Retire历史判断的模型必须由自己的 SnapshotParticipant Egress journal负责。
- 将 GameplayTickSystem 的 Input/Logic 注册收口为每 Session 一个 target。Presentation 仍按 Actor 独立推进，但注册和释放由 Active Session composition 统一持有。
- 将 Float32 canonical Program 提升为正式 `.csim` Target artifact，原子写入 `Library/CharacterSimulation/Programs/<definition-guid>/<numeric-profile>-abi<version>.csim`；ProgramAsset 精确包装 store 重读校验后的同一 bytes。
- 删除旧单 Tick `ISimulationDriver`、固定 `SimulationSessionRuntime`、`LocalCharacterSimulationSessionTickTarget`、`SimulationDriverCompositionPart` 能力位与旧 `GameplayNetworkSessionHost`，不保留 wrapper、兼容入口或双运行路径。
- 迁移 Corin Sandbox 到唯一显式 Local Session composition 与标准 Local Pipeline，保持当前 Program、动作、Timeline、Motion、GameplayEffect、Animation 和 Presentation 业务结果不变。
- 更新三个下游 Network Model change：ServerAuthoritative correction 与 Deterministic Rollback 各自提供独立 Pipeline Definition/Pass/Source；DotRecast 只替换服务端 Solver/Host，不创建第二 Session Host 或第二 Network Model。

## Non-Goals

- 不在本 change 实现 ServerAuthoritative correction Pipeline、Fantasy endpoint、Room 或双客户端 Demo。
- 不在本 change 实现 FixedQ32.32 Program Runtime、Deterministic Pipeline Backend、Deterministic KCC、rollback history/replay 或 lockstep。
- 不实现 DotRecast package、NavigationSurfaceArtifact 或普通 .NET authoritative runner。
- 不实现 active Session 动态增删 Actor；后续模型必须在 Preparing 锁定 launch roster，或由独立 roster lifecycle change 扩展。
- 不提供运行中热插拔 Pass、Pipeline、Backend、Source、Solver 或 Numeric Target；切换完整组合必须销毁并重建 Session。
- 不建立任意 C# callback/middleware、运行时反射 registry、字符串 service lookup 或没有状态/产品声明的自由脚本入口。
- 不实现可视化 Pipeline Graph 编辑器；本 change 只提供按阶段分组、可添加/替换/排序的正式 Pipeline Inspector。
- 不改变 Graph、StateMachine、Timeline、Blackboard、Action、GameplayEffect、Motion、动画或 `SimulationKernel.Evaluate` 内部 operation 业务语义。
- 不让 Pipeline 配置进入 BTSMTL Graph、CharacterPipelineDefinition 或 Program；Pipeline 是 Session composition 真相，不是角色业务 authoring。
- 不让 Runtime 加载 `.csir`，也不让 Unity Player 从 `Library` 路径读取 `.csim`。
- 不新增 Network Model enum、Solver enum、default model、default Pipeline、default Solver、断线 fallback 或 LocalLoopback fallback。

## Current Spec Comparison

- `character-simulation-kernel` 当前强制一个 `SimulationSessionRuntime` 按单 Tick 固定顺序执行，并把 Driver 定义为唯一 TickPlan/restore/OutputPlan owner。这与 prediction/reconciliation 和 rollback 的零到多步计划冲突。本 change 将其改为“固定 Session 事务 + compiled Pipeline plan + phase Pass”，并删除旧单 Tick Driver 合同。
- `character-pipeline-runtime` 当前把固定 `Driver -> Evaluate -> Solve -> Finalize -> OutputPlan` 作为唯一完整 Pipeline。本 change 保留 Kernel、Solver、state publish 与 Commit 的所有权规则，但将具体执行步骤迁入显式 Pipeline Definition。
- `character-pipeline-runtime` 当前要求 `CharacterPipelineHost` 装配 Session、Local Driver 与 Unity Solver。本 change 将其修改为 Actor registration/Presentation owner，并把完整运行装配迁到 `SimulationSessionHost`。
- `gameplay-tick-system` 已要求 Session 是唯一 Logic target，但当前每个 Character Host 仍注册自己的 target。本 change 使同一外层 LogicTick 只调用一次 Session runtime handle，同时允许该 handle 按 ExecutionPlan 执行多个内部 SimulationTick。
- `gameplay-network-model-boundary` 与 `gameplay-sync-backend-selection` 重复描述 Model/Endpoint/选择规则，后者仍保留旧 Host 口径。本 change 将完整规则收口到 Network Model Session Source、`gameplay-simulation-session-composition` 与新的 `gameplay-simulation-pipeline`，并移除重复 requirements。
- `character-simulation-kernel` 已要求完整 Numeric Target 组合，但没有区分 Program Runtime 与 Execution Backend，也没有 PipelineHash/Backend identity。本 change补齐该边界，不把 Float32 类型提升为所有 Target 的公共 ABI。
- `btsmtl-compiled-simulation-program` 已要求 Program bytes portable，但正式生成产物仍以 Unity Asset 为主。本 change 将 `.csim` 变为正式 Target artifact，并让 Unity Asset 与普通 .NET Host 消费同一 canonical bytes。PipelineHash 不进入 ProgramHash，而进入 Session composition/snapshot/handshake identity。
- `btsmtl-graph-core`、`agent-character-controller-synthesis`、`btsmtl-runtime-diagnostics`、`character-animation-pipeline` 与 `character-input-pipeline` 仍在场景、诊断、动画提交或 history 描述中引用旧 `SimulationSessionRuntime`、Driver 或 Driver OutputPlan。本 change 同步改为 Program Step Pass、Session Source、Pipeline Runtime 与 Egress OutputDisposition，不留下只换实现却不换架构文档的残留。
- `character-gameplay-sync-adapter`、`character-network-sync-domain-contract`、`character-syncfact-behavior-binding` 与 `gameplay-sync-runtime` 仍把模型边界写成 Driver/TickPlan/OutputPlan adapter。本 change 将模型输入输出改为 Source ports、Pipeline products、ExecutionPlan 与 Egress；packet/history 继续归具体模型，不进入 Common Host。
- `character-motion-simulation-boundary` 仍让固定 SessionRuntime拥有 world batch并让 Driver拥有 correction。本 change 将唯一 world batch迁到正式 WorldSolve Pass，将 correction迁到具体 Model Source及 Ingress/Schedule/Egress Pass，同时保持 Solver是唯一 world mutation owner。
- `local-gameplay-sync-loopback` 与 `server-authoritative-hybrid-sync-model` 仍以“Simulation Driver已安装”判断模型完整。本 change 将完整性改为 Source、Pipeline/Pass、Program Runtime、Backend、Solver与 Endpoint全部可创建；缺少任一项都不可选择。
- `refactor-simulation-operation-runtime-modules` 负责单 Actor Evaluate 内部模块化，明确保持 Session 固定顺序。本 change 会修改该“固定顺序”前提，但不会让 Session Pass 进入 Operation evaluator，也不会让 Operation module 成为 Pipeline Pass。
- `refactor-server-authoritative-hybrid-runtime` 后续只实现 Source、correction/replay Pass、Endpoint、Room 与 Demo；不再修改公共 Session Runtime。
- `add-deterministic-rollback-kcc-model` 后续实现 Fixed Program Runtime、Deterministic Backend、Rollback Source/Pass、KCC 与 Demo；它复用 Pipeline descriptor、Host 和 outer handle，不复用 Float32 ABI。
- `add-dotrecast-authoritative-server-backend` 后续只实现服务端 Solver/Host，并复用正式 `.csim` loader 与所选 ServerAuthoritative Pipeline。

## Impact

- 新能力：`gameplay-simulation-session-composition`、`gameplay-simulation-pipeline`。
- 修改能力：`btsmtl-compiled-simulation-program`、`btsmtl-graph-core`、`btsmtl-runtime-diagnostics`、`agent-character-controller-synthesis`、`character-pipeline-runtime`、`character-simulation-kernel`、`character-animation-pipeline`、`character-input-pipeline`、`character-motion-simulation-boundary`、`character-gameplay-sync-adapter`、`character-network-sync-domain-contract`、`character-syncfact-behavior-binding`、`gameplay-network-model-boundary`、`gameplay-sync-runtime`、`gameplay-tick-system`、`local-gameplay-sync-loopback`、`server-authoritative-hybrid-sync-model`。
- 移除重复能力要求：`gameplay-sync-backend-selection`。
- Portable Runtime：新增 Pipeline identity/descriptor/product/state contracts、ExecutionPlan 与 outer runtime handle。
- Float32 Runtime：新增标准 Pass、compiled Pipeline runtime 与 Float32 Pass Backend；删除固定 SessionRuntime 和旧 Driver。
- Unity Runtime：新增 Session Host、composition definitions、Pipeline/Pass definitions、preparation/launch plan 与 Actor registration。
- Simulation Editor/Artifacts：新增 Pipeline Inspector、正式 `.csim` store/status/transaction 与 ProgramAsset exact-byte wrapper。
- Networking：删除能力位和旧 GameplayNetworkSessionHost，保留实际可实现的 Network Model Session Source 与 Pipeline requirement 合同。
- Corin 资产/场景：新增唯一 Local Session composition、标准 Local Pipeline 和 Host，并迁移现有 Character Host 引用。
- 下游：ServerAuthoritative correction 与 Deterministic Rollback 可在基座归档后分别实现自己的 Pipeline，不修改公共 Host 或复制 Local Runtime。

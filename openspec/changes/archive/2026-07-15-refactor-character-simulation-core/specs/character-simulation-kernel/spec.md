# character-simulation-kernel Specification

## ADDED Requirements

### Requirement: SimulationKernel 必须分离 Evaluate 与 Finalize

SimulationKernel MUST提供无状态 Evaluate 与 Finalize。Evaluate MUST只接收 NumericProfile 完全匹配的 CharacterSimulationProgram、CharacterSimulationInput、CharacterSimulationState、SimulationIngress、SimulationTick 和上一 Tick body observation，并输出当前 Tick 的 PendingCharacterEvaluation 与 WorldRequest；Finalize MUST只接收同一 target ABI 的 pending evaluation 和精确匹配的 WorldSolverResult，并输出新 CharacterSimulationState 与 SimulationOutput。Kernel MUST不读取 Unity Time、Camera、InputAction、Transport、Network packet 或 Presentation object。

#### Scenario: Local Session 推进一个角色

- **WHEN** Local Driver 为当前 Actor 提交 Tick 与 portable input
- **THEN** Evaluate MUST产生 pending state 与 world request
- **AND** Finalize MUST等待匹配的 world result 后才产生新状态和输出

### Requirement: SimulationSessionRuntime 必须按四阶段原子推进全部 Actor

SimulationSessionRuntime MUST先按 ActorId、source tick、sequence 和 fact identity 校验/排序 SimulationIngress，再按 stable ActorId order 对当前 roster 执行 `Evaluate all -> ResolveWorldBatch once -> Finalize all -> BuildOutputPlan -> atomic state publish -> Committer`。任一 ingress、Actor Evaluate、world solve、Finalize 或 OutputPlan 校验失败时，当前 Tick MUST不替换任何 Actor state、World state 或提交部分输出。

#### Scenario: 第二个 Actor Finalize 失败

- **WHEN** 当前 Tick 的 ActorA 已 Finalize 而 ActorB 的 world result identity 不匹配
- **THEN** SessionRuntime MUST拒绝整个 Tick
- **AND** ActorA、ActorB 与 World state MUST保持 Tick 前状态

### Requirement: Simulation Session 必须锁定 ProgramCatalog 与 Actor roster

SimulationSessionRuntime MUST在启动前接收完整 SimulationProgramCatalog 与 ordered actor roster，并校验每个 ActorId 的 ProgramId、LayoutHash 和 World body binding。Session active 后 Catalog 与 roster MUST不可变；Driver TickPlan MUST只能为已有 Actor 提交 input/ingress，不能隐式 spawn、despawn、换 Program 或加入未知 Actor。

#### Scenario: Local Corin Session 启动

- **WHEN** Host 以一个 Corin Program 和一个 Actor binding 创建 Session
- **THEN** Session MUST锁定 CatalogHash 与 roster identity
- **AND** 后续 Tick MUST按该 binding 执行

#### Scenario: Driver 提交未知 Actor 输入

- **WHEN** TickPlan 包含不在锁定 roster 中的 ActorId
- **THEN** 当前 Tick MUST在 Evaluate 前失败
- **AND** MUST不自动创建默认 Character state 或 World body

#### Scenario: 运行中需要动态 spawn

- **WHEN** 后续业务需要在 active Session 中加入新 Actor
- **THEN** 当前 Core contract MUST明确拒绝该操作
- **AND** 实现方 MUST通过后续正式 roster lifecycle change 扩展，不能旁路修改 Session 或 WorldSolver state

### Requirement: Character 与 World 状态必须分属不同 owner

CharacterSimulationState MUST只保存单 Actor Gameplay 逻辑状态；WorldSimulationState MUST保存 ordered body state、solver-owned mutable state、world revision 和 static world identity。Driver state 与 Presentation state MUST不进入上述两个状态容器。

#### Scenario: 动画淡出继续推进

- **WHEN** Animancer fade 在两个 SimulationTick 之间推进
- **THEN** CharacterSimulationState 与 WorldSimulationState MUST不改变

### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

系统 MUST以 SimulationWorldSnapshot 聚合 ProgramCatalogHash、每 Actor ProgramId/ProgramHash/LayoutHash binding、solver/world identity、SimulationTick、stable Actor roster、全部 CharacterSimulationState 和 WorldSimulationState。Restore MUST在 Tick 开始前校验并原子替换完整 world，MUST不只恢复 Transform、单 Actor 或部分模块。

#### Scenario: 恢复 Attack2 中的双 Actor world

- **WHEN** Driver 请求恢复一个 ActorA 正在 Attack2、ActorB 正在移动的合法 snapshot
- **THEN** 两个 Character state 与 World state MUST在同一 restore transaction 中恢复
- **AND** 任一 payload 失败时当前 world MUST保持不变

### Requirement: State Hash 必须区分 Character 与 World 有效性

系统 MUST提供 CharacterStateHash 与 SimulationWorldHash。CharacterStateHash MUST覆盖当前 Actor binding 的 ProgramHash、NumericProfile、Character layout 和 canonical Character state bytes；WorldHash MUST再覆盖 ProgramCatalogHash、全部 Actor Program binding、target ABI、solver identity/version、world revision、SimulationTick、stable roster 和 WorldSimulationState。只有 Numeric Target、Catalog 中全部 Program 与 Solver composition 都声明 DeterministicReplay 时，WorldHash MAY被声明为跨机器确定性判定。

#### Scenario: Unity Solver 产生本地 WorldHash

- **WHEN** Local Session 使用 UnityCharacterControllerWorldSolver
- **THEN** 系统 MAY生成本地 capture 一致性 hash
- **AND** diagnostics MUST标记该 WorldHash 不具备跨机器 deterministic validity

### Requirement: Simulation Driver 必须保持最小策略边界

Session MUST装配唯一 ISimulationDriver。Driver MUST只提供 SimulationTickPlan、ordered Actor input、ordered typed SimulationIngress、可选完整 snapshot restore request、Tick result observation 和 SimulationOutputPlan。Driver MUST不执行 Program operation、不调用 WorldSolver、不获得 mutable Character/World state，也 MUST不直接触发 Presentation。SimulationIngress MUST不保存 packet、endpoint、history 或 model policy。

#### Scenario: Rollback Driver 申请恢复

- **WHEN** 后续 Driver 发现需要回到旧 SimulationTick
- **THEN** Driver MUST提交完整 RestoreRequest
- **AND** SessionRuntime MUST负责校验和原子恢复

#### Scenario: Driver 处理成功 Tick 输出

- **WHEN** Driver 观察到成功 SimulationTickResult
- **THEN** BuildOutputPlan MUST 只为外部 EventId 选择 Publish、Replace、Retire 或 Suppress
- **AND** Driver MUST 不接受、拒绝或改写 staged Character/World state

### Requirement: Local Driver 必须只实现本地立即提交语义

LocalSimulationDriver MUST将 GameplayTickSystem 的 LocalLogicTick 映射为当前 Session SimulationTick，从 Unity Input Adapter 取得本地 Actor input，并在 Tick 成功后为全部新 EventId 生成 Publish disposition。Local Driver MUST不建立网络 history、correction、rollback 或 endpoint fallback。

#### Scenario: 单机 Corin Tick

- **WHEN** LocalLogicTick 到达且 Corin Session active
- **THEN** Local Driver MUST生成一个 Tick plan
- **AND** 成功结果 MUST在同 Tick 产生 OutputPlan

### Requirement: World Solver 必须批量解决世界约束

ICharacterWorldSolver MUST只接收同一 NumericProfile 的 WorldSimulationState、WorldSolveBatchRequest 和 Tick context，并一次返回同一 target ABI 的 WorldSolveBatchResult 与新 WorldSimulationState。每个 request MUST按 ActorId 与 request identity 精确匹配一个 result。Solver MUST不读取 Graph、Action、Timeline、Network Model、server tick、ack 或 correction packet。

#### Scenario: Unity Solver 处理单 Actor batch

- **WHEN** Local Session 的 batch 只有 Corin 一个 request
- **THEN** Unity adapter MAY在内部调用一次 CharacterController.Move
- **AND** MUST通过同一 batch result 合同返回 portable body result

### Requirement: World Solver 必须声明真实恢复与确定性能力

WorldSolver MUST显式声明 NumericProfile、ABI version、Reconstructible、Snapshotable、DeterministicReplay 和实际 world feature。ProgramCatalog、Kernel specialization 或 Driver/Model 要求未满足时 composition MUST创建失败。系统 MUST不因 Solver 返回量化 result 就自动声明 DeterministicReplay。

#### Scenario: Rollback Model 尝试使用 Unity Solver

- **WHEN** 后续模型要求 Snapshotable 与 DeterministicReplay
- **AND** Unity Solver 只声明 Reconstructible
- **THEN** composition MUST拒绝创建
- **AND** MUST不降级为近似 replay

### Requirement: Simulation Session 必须锁定完整 Numeric Target 组合

一个 Simulation Session 的 ProgramCatalog、Kernel specialization、CharacterSimulationInput、CharacterSimulationState、WorldRequest/Result、GameplayFact、WorldSolver 和 Snapshot codec MUST使用同一 NumericProfile 与 ABI version。Composition Root MUST在创建 Session 前完成匹配校验；Runtime MUST不按 Driver、Network Model、Actor、Graph operation 或 packet 切换数值 backend。本 change 的 Local Session MUST使用 Float32 target。

#### Scenario: Float Program 误配 Fixed Solver

- **WHEN** Float32 ProgramCatalog 与未来 FixedQ32.32 WorldSolver 被装入同一 Session
- **THEN** composition MUST在首 Tick 前失败并报告两个 NumericProfile
- **AND** MUST不量化 request、转换 state 或选择默认 Solver

### Requirement: Kernel Backend 必须实现同一 Semantic Operation Set

Semantic IR 的 versioned operation set MUST唯一规定 Runnable、StateMachine、Timeline、Blackboard、Action、GameplayEffect 和 Motion 的控制流、状态所有权、事件顺序与输入输出语义。Numeric Target MAY提供不同 arithmetic/backend implementation，但 MUST完整实现同一 operation-set version，并在不支持时拒绝 build/composition。不同 target Program MUST不要求不同 authoring node、Graph 或 Model-specific operation。

#### Scenario: 后续增加 Fixed Kernel Specialization

- **WHEN** Rollback change 安装 FixedQ32.32 target
- **THEN** Fixed Kernel backend MUST执行与 Float32 相同版本的 Semantic IR operation set
- **AND** MUST不新增 DeterministicMoveNode、RollbackTimelineRuntime 或第二套 Action Runtime

### Requirement: CharacterSimulationInput 必须与设备和模型解耦

Kernel MUST只消费当前 NumericProfile 的 portable CharacterSimulationInput。Input Adapter 或具体 Driver MUST在 Kernel 外将 InputAction、Camera-relative 方向或 canonical external command 转换为稳定 InputId、target scalar/vector value、request、sequence 和 source tick。Graph operation MUST不读取 Camera、InputAction 或 model packet。

#### Scenario: 相机相对移动

- **WHEN** Unity Input Adapter 采样移动轴与 Camera yaw
- **THEN** Adapter MUST在 Tick plan 前产生 portable 世界方向或 yaw
- **AND** Program operation MUST只读取该 input

### Requirement: SimulationIngress 必须只承载模型无关 Gameplay 事实

SimulationIngress MUST只承载 Core 已声明的 typed Action lifecycle、GameplayResult、GameplayEffect lifecycle、Attribute value 或其它模型无关 ingress contract，并带 ActorId、source tick、sequence 与稳定 fact identity。Model adapter MUST在进入 Tick plan 前移除 packet、authority metadata、endpoint 和 transport 类型。

#### Scenario: 服务端拒绝预测动作

- **WHEN** 后续 ServerAuthoritative Driver 收到一个 Action reject decision
- **THEN** Driver MUST将其转换为 typed ActionLifecycle ingress
- **AND** Kernel MUST不读取原始 ActionDecision packet

### Requirement: SimulationOutput 必须通过稳定 EventId 提交副作用

Gameplay facts 与 presentation commands MUST使用由 Program operation、ActorId、activation identity、SimulationTick 和 local event sequence 构成的稳定 EventId。Kernel MUST不播放动画、发送 packet 或触发相机/VFX。Driver MUST为外部事件生成 Publish、Replace、Retire 或 Suppress disposition；SessionRuntime MUST在 OutputPlan 校验后原子发布 staged state，再将 Plan 交给 SimulationCommitter。

#### Scenario: Timeline 产生 Cue

- **WHEN** Timeline operation 在当前 Tick 产生 Cue command
- **THEN** Finalize MUST输出带 EventId 的 command
- **AND** Local Driver 生成 Publish 后只有 Committer MAY触发外部 Cue port

### Requirement: Gameplay State 发布与外部输出处置必须分离

Driver OutputPlan MUST只控制外部 EventId 生命周期，不得控制 Gameplay state 是否生效。SessionRuntime MUST在全部 Actor Finalize 与 OutputPlan 校验成功后一次替换全部 CharacterSimulationState 和 WorldSimulationState；Committer MUST在 state publish 后执行外部端口。Committer 端口失败时 Session MUST fail-stop 并报告精确 EventId，MUST不自动重试、回滚已触发副作用或继续下一 Tick。

#### Scenario: OutputPlan 引用未知 EventId

- **WHEN** Driver 对本 Tick不存在且历史未发布的 EventId 生成 Replace 或 Retire
- **THEN** SessionRuntime MUST在 state publish 前拒绝当前 Tick
- **AND** Character/World state MUST保持 Tick 前值

#### Scenario: Cue port 发布失败

- **WHEN** state 已发布但 Cue Committer port 抛出失败
- **THEN** Session MUST进入 fail-stop
- **AND** diagnostics MUST记录失败 EventId 与已完成的 state Tick

### Requirement: Structured Trace 不得受 Driver OutputPlan 控制

Compiler、SimulationKernel、SimulationSessionRuntime、WorldSolver adapter、Driver 与 SimulationCommitter MUST在各自正式边界向只读 diagnostics sink 发布 structured Trace。Trace MUST记录成功、失败、restore、replay 与 OutputPlan disposition，Driver MUST不能通过 Publish/Replace/Retire/Suppress 隐藏或改写 Trace。Diagnostics MUST不反向改变 Character/World state 或外部输出。

#### Scenario: Rollback replay 抑制重复 Cue

- **WHEN** Driver 对重复 Cue EventId 生成 Suppress
- **THEN** Committer MUST不再次触发 Cue port
- **AND** Diagnostics MUST仍记录 replay operation、EventId 与 Suppress disposition

#### Scenario: Finalize 失败

- **WHEN** 某 Actor Finalize 失败导致当前 Tick 不发布 state
- **THEN** Diagnostics MUST记录失败位置与未发布 disposition
- **AND** Driver MUST不能从 Trace 中删除该失败

### Requirement: Portable Core 必须由 Unity 与普通 DotNet 共享源集

Semantic IR contracts、target compiler contracts、operation-set contract、Program、Character state、World state contracts、Input、Output、SessionRuntime、Driver 和 WorldSolver contract shape MUST来自 canonical portable source set，并可由 Unity asmdef 与普通 .NET csproj 编译。当前 Float32 Kernel backend MUST由 Unity 与普通 .NET host 共享源码。系统 MUST不复制 server Kernel 或网络专用 operation runtime；未来 Fixed backend 的差异只能来自 Numeric Target，不得复制 Authoring 业务模型。

#### Scenario: DotNet 项目引用 Core

- **WHEN** 后续普通 .NET host 引用 portable core
- **THEN** MUST编译同一份 Program reader、Kernel 与 SessionRuntime 源码
- **AND** MUST不需要 UnityEngine、CharacterPipelineHost 或 authoring asset

### Requirement: 确定性 Numeric Target 与 WorldSolver 必须作为独立完整能力实现

Portable Gameplay core MUST提供稳定 Semantic IR operation、Numeric Target extension、snapshot ownership 和 batch solve 形状，但 MUST不在 Float32 Kernel 内实现 Fixed arithmetic、KCC、DotRecast、Unity physics 或 Network Model。完整 deterministic replay MUST由具体 Model 同时装配 Fixed Numeric Target、匹配的 Program/Kernel/State/Snapshot ABI，以及声明 Snapshotable 与 DeterministicReplay 的 WorldSolver。

#### Scenario: 核心只安装 Unity Solver

- **WHEN** 本 change 完成且 Deterministic KCC 尚未实现
- **THEN** Local Corin MUST可以运行
- **AND** 系统 MUST只安装 Float32 target，不显示可运行的 Deterministic Rollback 组合

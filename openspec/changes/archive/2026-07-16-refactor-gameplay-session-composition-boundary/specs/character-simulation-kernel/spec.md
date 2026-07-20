## MODIFIED Requirements

### Requirement: Simulation Session 必须锁定 ProgramCatalog 与 Actor roster

Session Pipeline Runtime MUST在启动前接收完整 SimulationProgramCatalog与 ordered Actor roster，并校验每个 ActorId的 ProgramId、LayoutHash与 World body binding。Session Active后 Catalog与 roster MUST不可变；Ingress/Schedule产品只能为已有 Actor提交 input/ingress，不能隐式 spawn、despawn、换 Program或加入未知 Actor。

#### Scenario: Schedule 提交未知 Actor 输入

- **WHEN** ExecutionPlan包含不在锁定 roster中的 ActorId
- **THEN** 当前 outer Tick MUST在 Step阶段前失败
- **AND** MUST不自动创建默认 Character state、World body或 registration

### Requirement: Character 与 World 状态必须分属不同 owner

CharacterSimulationState MUST只保存单 Actor Gameplay逻辑状态；WorldSimulationState MUST保存 ordered body state、solver-owned mutable state、world revision与 static world identity。影响未来 Pipeline执行的 Pass状态 MUST进入独立 SimulationPipelineStateSnapshot或正式 reconstruct合同；Session Source external state与 Presentation state MUST不进入 Character/World状态容器。

#### Scenario: 动画淡出继续推进

- **WHEN** Animancer fade在两个 SimulationTick之间推进
- **THEN** CharacterSimulationState、WorldSimulationState与 Pipeline Gameplay state MUST不改变

### Requirement: SimulationWorldSnapshot 必须原子 Capture 与 Restore

Session snapshot MUST聚合 ProgramCatalogHash、每 Actor Program binding、BackendId/version、PipelineId/Hash、Pipeline state participant identity、Solver/world identity、SimulationTick、stable roster、全部 CharacterSimulationState、WorldSimulationState与需要回滚的 Pipeline state。Restore MUST在 step loop开始前校验并原子替换完整 working world，MUST不只恢复 Transform、单 Actor、部分 Pass或部分模块。

#### Scenario: 恢复 Attack2 中的双 Actor Pipeline world

- **WHEN** Schedule Plan请求恢复一个 ActorA正在 Attack2、ActorB正在移动且包含合法 Pipeline participant状态的 snapshot
- **THEN** 两个 Character state、World state与 Pipeline state MUST在同一 restore transaction中恢复
- **AND** 任一 payload或 PipelineHash失败时当前正式 world MUST保持不变

### Requirement: State Hash 必须区分 Character 与 World 有效性

系统 MUST提供 CharacterStateHash与 SimulationWorldHash。CharacterStateHash MUST覆盖 ProgramHash、NumericProfile、Character layout与 canonical Character state bytes；WorldHash MUST再覆盖 ProgramCatalogHash、全部 Actor binding、Target ABI、BackendId/semantic version、PipelineHash、Pipeline snapshot participant state、Solver identity/version、world revision、SimulationTick、stable roster与 WorldSimulationState。只有 Program Runtime、Backend、Pipeline全部 Pass、Catalog全部 Program与 Solver都声明 DeterministicReplay时，WorldHash MAY被声明为跨机器确定性判定。

#### Scenario: Unity Solver 产生本地 WorldHash

- **WHEN** Local Session使用 Float32 Pass Backend与 UnityCharacterControllerWorldSolver
- **THEN** 系统 MAY生成本地 capture一致性 hash
- **AND** diagnostics MUST标记该 WorldHash不具备跨机器 deterministic validity

### Requirement: World Solver 必须声明真实恢复与确定性能力

WorldSolver MUST显式声明 NumericProfile、ABI version、Reconstructible、Snapshotable、DeterministicReplay与实际 world feature。Program/Pass capability union或 Source/Backend requirement未满足时 composition MUST创建失败。系统 MUST不因 Solver返回量化 result就自动声明 DeterministicReplay。

#### Scenario: Rollback Pipeline 尝试使用 Unity Solver

- **WHEN** 后续 Pipeline要求 Snapshotable与 DeterministicReplay
- **AND** Unity Solver只声明 Reconstructible
- **THEN** composition MUST拒绝创建
- **AND** MUST不降级为近似 replay或删除相关 Pass

### Requirement: Simulation Session 必须锁定完整 Numeric Target 组合

一个 Session的 ProgramCatalog、Program Runtime、Kernel specialization、CharacterSimulationInput、CharacterSimulationState、WorldRequest/Result、GameplayFact、Snapshot codec、Execution Backend、Pipeline Pass与 WorldSolver MUST使用兼容 NumericProfile、Target ABI与 operation-set version。Target-specific Composer MUST在创建 runtime handle前完成 ProgramHash、LayoutHash、PipelineHash、Backend、roster、initial state、Source port、Solver capability与 codec identity校验；公共 Host MUST不按 Source、Network Model、Actor、Pass、Graph operation、packet或 Tick切换数值 backend。当前 Local组合 MUST只使用 Float32 Program Runtime与 Float32 Pass Backend。

#### Scenario: Float Program 误配 Fixed Backend

- **WHEN** Float32 ProgramCatalog、未来 Fixed Execution Backend或错误 ABI Pass被提交给同一 Composer
- **THEN** composition MUST在首 Tick前失败并报告各组成部分 identity
- **AND** MUST不量化 product、转换 state、包装 object adapter或选择默认 Backend/Solver

### Requirement: CharacterSimulationInput 必须与设备和模型解耦

Kernel MUST只消费当前 NumericProfile的 portable CharacterSimulationInput。Input Adapter、Ingress Pass或具体 Session Source MUST在 Kernel外将 InputAction、Camera-relative方向或 canonical external command转换为稳定 InputId、target scalar/vector value、request、sequence与 source tick。Graph operation MUST不读取 Camera、InputAction、Pipeline Definition或 model packet。

#### Scenario: 相机相对移动

- **WHEN** Unity Input Adapter采样移动轴与 Camera yaw
- **THEN** Adapter MUST在 Ingress产品生成前产生 portable世界方向或 yaw
- **AND** Program operation MUST只读取该 input

### Requirement: SimulationIngress 必须只承载模型无关 Gameplay 事实

SimulationIngress MUST只承载 Core已声明的 typed Action lifecycle、GameplayResult、GameplayEffect lifecycle、Attribute value或其它模型无关 ingress contract，并带 ActorId、source tick、sequence与稳定 fact identity。Session Source/Ingress Pass MUST在进入 Step前移除 packet、authority metadata、endpoint与 transport类型。

#### Scenario: 服务端拒绝预测动作

- **WHEN** 后续 ServerAuthoritative Source收到 Action reject decision
- **THEN** 对应 Ingress Pass MUST将其转换为 typed ActionLifecycle ingress
- **AND** Kernel MUST不读取原始 ActionDecision packet

### Requirement: SimulationActorTickResult 必须通过稳定 EventId 提交副作用

Gameplay facts与 presentation commands MUST使用由 Program operation、ActorId、activation identity、SimulationTick与 local event sequence构成的稳定 EventId。Kernel MUST不播放动画、发送 packet或触发相机/VFX。Egress Pass MUST为外部事件生成带显式ActorId的Publish、Replace、Retire或 Suppress disposition；Execution Backend MUST核对本次EventId与Actor归属，并在 disposition与全部 working state校验后原子发布最终 state，再将 Plan交给 SimulationCommitter。需要跨Tick判断历史EventId的Egress MUST以SnapshotParticipant journal拥有该历史，Unity output adapter MUST不保存无界owner字典。

#### Scenario: Timeline 产生 Cue

- **WHEN** Timeline operation在当前 Step产生 Cue command
- **THEN** Finalize MUST输出带 EventId的 command
- **AND** Local Egress生成 Publish后只有 Committer MAY触发外部 Cue port

### Requirement: Gameplay State 发布与外部输出处置必须分离

Egress OutputDisposition MUST只控制外部 EventId生命周期，不得控制 Gameplay working state是否生效。Execution Backend MUST在全部内部 Step、Egress与 disposition校验成功后一次发布最终 CharacterSimulationState、WorldSimulationState与 Pipeline state；Committer MUST在 state publish后执行外部端口。Committer端口失败时 Session MUST fail-stop并报告精确 EventId，MUST不自动重试、伪造已触发副作用的回滚或继续下一 Tick。

#### Scenario: Egress 引用未知 EventId

- **WHEN** Egress对本次 plan不存在且历史未发布的 EventId生成 Replace或 Retire
- **THEN** Backend MUST在 state publish前拒绝当前 outer transaction
- **AND** Character/World/Pipeline正式 state MUST保持 outer Tick前值

### Requirement: Portable Core 必须由 Unity 与普通 DotNet 共享源集

Semantic IR、Target Compiler、operation-set、Program、Character/World state、Input/Output、Pipeline descriptor/product/execution-plan/snapshot contracts、runtime handle与 WorldSolver合同 MUST来自 canonical portable source set，并可由 Unity asmdef与普通 .NET csproj编译。当前 Float32 Program Runtime、标准 Pass与 Pass Backend MUST由 Unity和普通 .NET Host共享源码。系统 MUST不复制 server Kernel、网络专用 operation runtime或第二 Pipeline compiler；未来 Fixed差异只能来自 Program Runtime、Target Pass实现、Backend与 Solver，不得复制 Authoring业务模型。

#### Scenario: DotNet 项目引用 Core

- **WHEN** 后续普通 .NET Host引用 portable source
- **THEN** MUST编译同一 Program reader、Pipeline descriptor/compiler合同与 Kernel源码
- **AND** MUST不需要 UnityEngine、CharacterPipelineHost或 authoring asset

## REMOVED Requirements

### Requirement: Structured Trace 不得受 Driver OutputPlan 控制

**Reason**: 旧 Driver/OutputPlan合同删除，外部事件处置迁入显式 Egress Pass与 OutputDisposition产品。

**Migration**: Trace必须独立记录 Pipeline/Pass/Step与 Egress disposition，不能被 Publish、Replace、Retire或 Suppress隐藏。

### Requirement: SimulationSessionRuntime 必须按四阶段原子推进全部 Actor

**Reason**: 固定单 Tick顺序无法表达一次 outer LogicTick中的 restore与多 Tick replay，也无法组合正式网络/特殊处理 Pass。

**Migration**: 由 Execution Backend按 compiled Ingress/Schedule/Step/Egress Pipeline执行 working transaction，固定保留最终原子 state publish与 Commit边界。

### Requirement: Simulation Driver 必须保持最小策略边界

**Reason**: 旧 Driver同时拥有单 Tick计划、restore与 OutputPlan，扩展 correction/rollback时会变成隐藏 Pipeline或私建 replay runner。

**Migration**: Session Source只提供外部资源和窄端口；Ingress/Schedule/Egress Pass显式拥有计划与输出策略，所有 Pass都进入 Pipeline Definition与 identity。

### Requirement: Local Driver 必须只实现本地立即提交语义

**Reason**: Local行为迁移为可审查的标准 Local Pipeline，不再保留特殊 Driver Runtime。

**Migration**: Local Source、LocalInputIngressPass、LocalSingleStepSchedulePass与 LocalImmediateOutputPass共同表达本地语义。

## ADDED Requirements

### Requirement: Structured Trace 不得受 Egress OutputDisposition 控制

Compiler、SimulationKernel、Pipeline Runtime/Pass、WorldSolver adapter、Session Source与 SimulationCommitter MUST在各自正式边界向只读 diagnostics sink发布 structured Trace。Trace MUST记录 PipelineHash、PassId、product、成功、失败、restore、replay与 OutputDisposition；Egress MUST不能通过 Publish、Replace、Retire或 Suppress隐藏或改写 Trace。Diagnostics MUST不反向改变 Character/World/Pipeline state或外部输出。

#### Scenario: Rollback replay 抑制重复 Cue

- **WHEN** 后续 Egress Pass对重复 Cue EventId生成 Suppress
- **THEN** Committer MUST不再次触发 Cue port
- **AND** Diagnostics MUST仍记录 replay Step、Pass、EventId与 Suppress disposition

### Requirement: Execution Backend 必须按 Pipeline 事务原子推进零到多个 Step

Execution Backend MUST先运行 Ingress和唯一 Schedule producer，再按 ExecutionPlan可选 restore并执行零到多个 ordered Step。每个标准 Step MUST按 stable ActorId order执行 Program Evaluate、一次 World ResolveBatch与 Program Finalize；多个 replay step MUST只推进 working state。全部 Step与 Egress成功后 Backend MUST原子发布最终 Character/World/Pipeline state并 Commit外部输出。任一阶段失败时 MUST不发布部分 working state或副作用。

#### Scenario: 第二个 Replay Step Finalize 失败

- **WHEN** Replay 101成功而 Replay 102的 ActorB world result identity不匹配
- **THEN** Backend MUST拒绝整个 outer transaction
- **AND** Replay 101的 state和外部输出 MUST不成为正式结果

### Requirement: Session Source 必须保持外部资源边界

Session Source MUST只拥有 source clock、local input/endpoint、packet/history等外部资源及其显式 ports。它 MUST不执行 Program operation、不调用 WorldSolver、不获得 mutable Character/World/Pipeline working state、不驱动 Presentation，也 MUST不在 Common Host中隐藏注入 Pass。Local与 Network Model Source的差异 MUST通过显式 Source Definition和所选 Pipeline Pass表达。

#### Scenario: Network Model 提供 Correction 数据

- **WHEN** 后续 Model Source收到 authoritative snapshot
- **THEN** 它 MUST通过声明的 Source port供 Ingress/Schedule Pass消费
- **AND** MUST不直接调用 Backend restore或修改 Character Transform

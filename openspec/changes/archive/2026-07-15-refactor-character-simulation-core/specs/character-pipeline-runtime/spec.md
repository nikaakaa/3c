# character-pipeline-runtime Specification

## MODIFIED Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

CharacterPipelineHost MUST只负责加载与 CharacterPipelineDefinition source revision 匹配的 CharacterSimulationProgram/Projection，将其注册进当前 SimulationProgramCatalog 与 Projection registry，并显式装配 Simulation Session、Local Driver、Unity WorldSolver、Input Adapter、Committer 和 diagnostics target。Host MUST不在运行时 clone authoring Graph/Timeline、自动寻找默认组件或选择 Network Model。

#### Scenario: 创建单机 Corin

- **WHEN** Sandbox Host 创建本地 Corin
- **THEN** MUST显式绑定 ProgramCatalog entry、Projection、Local Driver、Unity Solver 和 Committer

### Requirement: Character ActorId 必须由 Host 单点装配

Simulation Session Host MUST在 Session 启动前为每个 roster entry 分配或接受唯一非空 ActorId，并显式绑定 ProgramCatalog 中的 ProgramId、Character layout 与 World body。该 ActorId MUST用于 CharacterSimulationState、input、ingress、facts、EventId 和 diagnostics。Program operation、Projection、Solver adapter 与 Model adapter MUST不自行生成另一 identity，Session 启动后 Driver MUST不增删 roster entry。

#### Scenario: Local Corin 注册

- **WHEN** Corin Host 加入 Local Session
- **THEN** roster MUST建立唯一 ActorId、ProgramId、layout 与 World body binding
- **AND** Session 启动时 MUST锁定该 binding

### Requirement: CharacterPipeline 是纯 C# 运行时主体

正式 Character gameplay runtime MUST由 portable Program、CharacterSimulationState、SimulationKernel、SimulationSessionRuntime 和明确 ports 构成。可变 gameplay state MUST不隐藏在 CharacterPipeline stage、RunnableNode clone、Timeline scheduler、GraphContext 或 Unity component 内。Unity Host/Adapter MUST留在 composition boundary。

#### Scenario: DotNet 编译 Core

- **WHEN** 普通 .NET csproj 编译 Core source set
- **THEN** MUST不需要 CharacterPipelineHost、ScriptableObject 或 UnityEngine

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

正式逻辑链 MUST收口为 `Driver TickPlan/input/ingress -> all Actor Evaluate -> one World ResolveBatch -> all Actor Finalize -> Driver BuildOutputPlan -> atomic state publish -> Committer`。Graph、StateMachine、Timeline、Action、Effect 和 Motion resolve MUST属于 Program/Kernel；world mutation MUST属于 WorldSolver；model adapter 与 Presentation MUST位于正式外部端口。Driver OutputPlan MUST不决定 Gameplay state 是否生效。

#### Scenario: 一个 Local Tick

- **WHEN** Local Driver 推进一个 SimulationTick
- **THEN** SessionRuntime MUST先完成全部 Actor logic 与 world batch
- **AND** Committer MUST只在 Tick 原子成功后处理副作用

### Requirement: 节点和 Timeline 不直接结算最终 Transform

Compiled Node 与 Timeline operation MUST只产生 state mutation、typed fact、MotionContribution 或 WorldRequest。只有 WorldSolver MAY改变 WorldSimulationState body，只有 Presentation adapter MAY写 visual root；两者 MUST不形成可反写的第二逻辑真值。

#### Scenario: Dodge Timeline 位移

- **WHEN** MotionCurve operation 产生 displacement
- **THEN** MUST经统一 WorldRequest batch取得 actual body result

### Requirement: Timeline 和动画 tick 权威归属 pipeline

Gameplay Timeline logic time MUST归 Program/CharacterSimulationState 并按 SimulationTick推进；animation visual sampling 与 Animancer fade MUST归 PresentationFrame。SessionRuntime 与 Presentation MUST通过 committed producer/playback identity连接，MUST不共享 mutable clock。

#### Scenario: 无新 Logic Tick 的 RenderFrame

- **WHEN** PresentationFrame 到达但没有新 SimulationTick
- **THEN** animation MAY继续采样和淡出
- **AND** Timeline Gameplay state MUST不改变

### Requirement: CharacterPipeline 是 GameplayTickSystem 的 tick target

GameplayTickSystem MUST注册 Simulation Session 作为 logic target，不再为同一 Session 的每个 Character 调用旧 CharacterPipeline.LogicTick。Presentation target MAY独立注册并只消费 published samples/commands。

#### Scenario: Session 包含两个 Actor

- **WHEN** fixed LocalLogicTick 到达
- **THEN** GameplayTickSystem MUST只推进一次 Session 四阶段链

### Requirement: Pipeline 输出分为 strict、presentation 和 sync facts

SimulationTickResult MUST类型化分离 Gameplay facts、body/world observations、presentation commands、model-neutral SyncDomain facts 与 Trace records。Driver/Committer MAY按端口消费，MUST不让 Presentation output 反向改变 Gameplay state。

#### Scenario: Attack Tick 输出

- **WHEN** Attack 产生 Window、Motion 和 animation command
- **THEN** Tick result MUST以独立 typed channels 保存并共享同一 Event identity

### Requirement: CharacterPipelineDefinition 持有角色输入合同

CharacterPipelineDefinition MUST继续持有 InputProfile authoring identity；Compiler MUST将 InputId、value type、range、request policy 和量化规则写入 Program input catalog。Unity Input Adapter MUST引用同一 catalog转换设备输入，Kernel MUST不读取 InputProfile asset。

#### Scenario: 编译 Move Input

- **WHEN** Definition 引用合法 InputProfile
- **THEN** Program MUST包含对应 portable InputId/catalog

### Requirement: Pipeline 输出事实必须继续通过 SyncFacts 边界产生

Compiled Program MUST保持 typed SyncDomain facts 作为角色 Gameplay 输出事实边界。Blackboard variable MAY为 Program operation 提供运行上下文；只有显式合法 fact projection 才能产生 Action、GameplayResult、GameplayEffect 或 Presentation fact。SimulationOutput MUST保存投影后的 typed facts，Model adapter MUST不直接读取 Blackboard state。

#### Scenario: 投影 Action Window

- **WHEN** Window projection 收到合法 declaration、write provenance 与 Action Context
- **THEN** Finalize MUST生成 ActionWindow fact并写入 Tick result

#### Scenario: 写入 local-only 临时值

- **WHEN** operation 写入 Projection=None 的 Blackboard variable
- **THEN** 该值 MUST不进入 SimulationOutput

### Requirement: Pipeline Blackboard 生命周期必须进入 frame cleanup

CharacterSimulationState MUST按 Program layout在 Frame、State、ActionInstance、Graph activation 与 Character lifecycle终点清理对应 Blackboard owner slots。Cleanup MUST由 compiled lifecycle operation 执行，MUST不依赖节点手动写 null 或 CharacterGraphContext dictionary clear。

#### Scenario: Frame scope 清理

- **WHEN** 当前 SimulationTick Finalize 完成
- **THEN** Frame owner slots MUST在下一 Tick 前清理

### Requirement: 角色管线必须保留跨 logic tick 的动画生命周期命令

SimulationCommitter MUST使用 presentation-owned 持久队列保存未消费的 producer selection、sample、complete、release 与 EventId lifecycle。Queue MUST独立于 transient Tick result，并按 SimulationTick、event sequence 和 playback generation保序；queue MUST不保存 Character/World mutable state。

#### Scenario: 一个 PresentationFrame 前多个 SimulationTick

- **WHEN** Committer 连续提交多个 generation
- **THEN** queue MUST保留 Complete/Release 顺序直到 Presentation acknowledge

### Requirement: PresentationFrame 必须输出逐层最终动画结果

Presentation diagnostics snapshot MUST保存每层 AnimationPlaybackLifecycle 状态，包括 selected、PendingFirstSample、Current、Outgoing、Retired、visual sample time 与 Animancer fade。Snapshot MUST只用于 diagnostics，MUST不进入 SimulationWorldSnapshot 或 Kernel决策。

#### Scenario: Base 等待第一 Sample

- **WHEN** target selection 已提交但首个 sample 未到
- **THEN** snapshot MUST同时显示 Current 与 PendingFirstSample

### Requirement: CharacterPipeline 必须作为显式 diagnostics target

每个 active Simulation Session 与其 Actor roster MUST注册明确 diagnostics target/session identity，并提供 Program revision、Source Map、默认关闭的 Live/Capture store 和只读 metadata。Editor MUST不持有 runtime Graph、mutable Character state、World state 或 Solver object。

#### Scenario: Local Session 激活

- **WHEN** Corin Session 完成创建
- **THEN** diagnostics registry MUST注册 Session/Actor target 与 ProgramHash

### Requirement: Pipeline domain debug 必须进入统一 Trace

Input、ingress、Program operation、StateMachine、Timeline、Blackboard、WorldRequest/Result、Action、Effect、commit、Animation 和 Camera diagnostics MUST进入统一 structured Trace/view model。Inspector MUST不遍历旧 stage 或 runtime service 私有集合形成平行调试链。

#### Scenario: 查看一次 Dodge Tick

- **WHEN** Debug Session 定位 Dodge EventId
- **THEN** MUST关联 input、operation、world batch 与 committed animation command

### Requirement: CharacterPipeline 必须提交逻辑侧唯一动画选择

Program Finalize MUST在 State、Action、interruption 与 Timeline request 处理后为每个 LayerId 最多产生一个 selected producer/playback command。Committer 与 Presentation MUST不重新仲裁逻辑候选。

#### Scenario: 同层所有权冲突

- **WHEN** Program 无法为 Base layer产生唯一选择
- **THEN** 当前 Tick MUST报告明确冲突
- **AND** Presentation MUST不选择默认赢家

### Requirement: PresentationFrame 必须原子提交动画播放生命周期

PresentationFrame MUST按固定顺序读取 Committer queue、采样 selected/retained producer、更新 AnimationPlaybackLifecycle、调用 Animancer adapter、推进 fade、退休 outgoing 并 acknowledge batch。该阶段 MUST不执行 Program、TreeClip、Motion、Action、Effect 或 WorldSolver。

#### Scenario: Selection 与首个 Sample 同批

- **WHEN** target selection 与合法 sample 同批到达
- **THEN** lifecycle MUST原子切换 Current/Outgoing

### Requirement: CharacterPipeline 必须编排唯一 Gameplay Effect 阶段

Compiled Program MUST唯一拥有 GE catalog/operations，CharacterSimulationState MUST唯一拥有 GE state。Evaluate MUST开始并推进当前 Tick GE transaction，Finalize MUST唯一 drain ChangeSet并输出 facts；Host、Committer 与 Presentation MUST不持有第二个 GameplayEffectRuntime。

#### Scenario: Local Tick 推进 Effect

- **WHEN** Effect period 在当前 SimulationTick 到期
- **THEN** Program MUST在当前 Tick产生唯一 ChangeSet facts

## ADDED Requirements

### Requirement: Program Operation Execution Context 必须是唯一角色逻辑上下文

Kernel MUST为 operation 提供只读 Program、SimulationTick、Actor input、SimulationIngress、Character state accessor、上一 body observation、typed output writer 和 Source Map identity。Operation MUST不获得 Host、GameObject、Driver、WorldSolver、Presentation 或 model session reference。

#### Scenario: Condition operation 读取输入与 Blackboard

- **WHEN** operation 求值移动状态条件
- **THEN** MUST只通过 execution context 的 portable input/state accessor读取

## REMOVED Requirements

### Requirement: CharacterGraphContext 必须通过 Pipeline Blackboard 暴露黑板

**Reason**：Runtime CharacterGraphContext 聚合 Input、Action、Pose、GE、Blackboard 与 diagnostics mutable service，无法进入明确 State Layout，也会保留节点对象执行入口。

**Migration**：Compiler 将 Blackboard declaration/reference编入 Program；operation只通过 CharacterSimulationState typed accessor读写。

#### Scenario: 删除 Runtime GraphContext Blackboard

- **WHEN** compiled operation读取 Blackboard
- **THEN** MUST使用 Program address与 Character state slots
- **AND** MUST不调用 CharacterGraphContext dictionary API

### Requirement: Graph 执行上下文来自 CharacterGraphContext

**Reason**：CharacterGraphContext 同时持有 InputStage、ActionRuntime、LogicPosePort、GameplayEffect adapter、Blackboard 与 diagnostics mutable service，无法完整进入 Program state layout。

**Migration**：Runtime 改用 Program Operation Execution Context；Editor authoring context 可保留但不得执行 Gameplay。

#### Scenario: 删除 Runtime GraphContext

- **WHEN** Corin Program 运行
- **THEN** operation MUST不访问 CharacterGraphContext runtime object

### Requirement: BTSMTLPhase 驱动 BTSMTL RootTree 和 Timeline playback

**Reason**：BTSMTLPhase 通过 runtime clone 与 TimelinePlaybackScheduler执行单角色阶段，无法进入 portable snapshot/batch world链。

**Migration**：RootTree、StateMachine、Timeline 与 TreeClip全部编译为 Kernel operation。

#### Scenario: 删除 BTSMTLPhase

- **WHEN** Local Session Evaluate Corin
- **THEN** MUST只执行 Program operations

### Requirement: CharacterPipeline 支持混合架构 authority mode

**Reason**：LocalSolver、ExternalPose 和 None 混合了 input、simulation、world solve 与 remote presentation。

**Migration**：删除 CharacterMotionAuthority；Session composition显式选择 Driver 与 WorldSolver。

#### Scenario: 迁移单机 Actor

- **WHEN** Sandbox 创建 Corin
- **THEN** MUST装配 Local Driver 与 Unity WorldSolver

### Requirement: NetworkStage 是正式边界但不实现真实 transport

**Reason**：公共 Character NetworkStage 与具体 Model Driver 重复保存输入输出，并固定 ExternalPose/correction语义。

**Migration**：删除 Character NetworkSendStage/ReceiveStage；模型只通过 Tick plan/ingress/restore/result/commit ports接入。

#### Scenario: 核心完成但 Network Model 未完成

- **WHEN** Local Session 运行
- **THEN** MUST不创建任何 Character NetworkStage

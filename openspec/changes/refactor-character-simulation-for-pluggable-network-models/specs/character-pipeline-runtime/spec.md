# character-pipeline-runtime Specification

## MODIFIED Requirements

### Requirement: CharacterPipeline 是纯 C# 运行时主体

系统 MUST将原 CharacterPipeline gameplay authority 拆为纯 C# `CharacterSimulationKernel`、Session-owned Simulation Driver 和客户端 Presentation adapter。Kernel MUST由 Simulation Driver 提供 SimulationTick、pass kind、portable input 和 World Solver；MUST不保存具体 Network Model、transport、endpoint、presentation adapter、Unity Time、`CharacterInputSource` 或 `CharacterMotionAuthority`。`CharacterPipeline` 名称若继续保留，只能作为 actor/presentation facade，MUST不再同时拥有模拟调度、网络策略和表现提交权威。

#### Scenario: Local Driver 驱动角色

- **WHEN** GameplayTickSystem 推进 Local Session
- **THEN** Local Driver MUST调用同一 SimulationKernel
- **AND** Character presentation facade MUST只消费 Step 输出

#### Scenario: Rollback Driver 重演角色

- **WHEN** model session 恢复 world snapshot 并重演
- **THEN** Kernel MUST不依赖 CharacterPipeline MonoBehaviour 或 scene object
- **AND** Presentation MUST不作为 replay state 的一部分

### Requirement: BTSMTLPhase 驱动 BTSMTL RootTree 和 Timeline playback

系统 MUST将原 `CharacterBTSMTLPhase` 的执行顺序编译进 CharacterSimulationProgram，并由 SimulationKernel 按 `Decision TreeClip -> RootTree/StateMachine -> WindowFactProjection -> Timeline Commit` 顺序执行。正式 runtime MUST执行 operation/state slot，MUST不持有 BehaviorTreeRuntime、TimelinePlaybackScheduler authoring clone 或节点工作副本。相同 Program 的 Local、Authoritative 和 Replay pass MUST使用同一阶段顺序。

#### Scenario: Window 触发同 Tick 状态抢占

- **WHEN** Decision TreeClip 在当前 SimulationTick 写入 Cancel Frame variable
- **THEN** compiled Transition MUST在同 Tick 读取该值
- **AND** cancelled Timeline MUST不产生该 Tick 后续非决策贡献

#### Scenario: Replay 执行同一 Tick

- **WHEN** Driver 重演曾经执行过的 Timeline/State Tick
- **THEN** Kernel MUST按同一 operation order 重建 state 和 facts
- **AND** MUST不调用旧 BTSMTLPhase 或 authoring clone

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

系统 MUST将输入适配、SimulationKernel、World Solver、Network Model Driver 和 Presentation Committer 设为独立边界。输入适配 MUST在 Kernel 前生成 portable simulation input；Kernel/solver MUST只生成 state/facts/commands；Model Driver MUST在 Kernel 外处理 packet、history、authority、restore/replay 和 commit；Presentation MUST在 RenderFrame 消费 committed/predicted command。系统 MUST不再由一个 CharacterPipeline `LogicTick` 同时收包、执行 Graph、应用模型 correction、发包并捕获表现。

#### Scenario: ServerAuthoritative 收到权威结果

- **WHEN** model Driver 收到 authoritative state
- **THEN** Driver MUST在自己的 history/reconciliation 阶段处理
- **AND** Kernel、Motion operation 和 Presentation MUST不解析原始 model packet

### Requirement: 节点和 Timeline 不直接结算最终 Transform

Graph/Timeline compiled operation MUST只产出 gameplay intent、window、command 或 cue。最终 gameplay body MUST由当前 Driver 装配的正式 World Solver 写入 SimulationState；Unity logic/visual Transform MUST只镜像 Driver 选择的 simulation/presentation sample。Graph、Timeline、Action、Network Model 和 Presentation MUST不直接调用 concrete solver 或写 gameplay Transform。

#### Scenario: Timeline 产出动作位移

- **WHEN** compiled Timeline motion operation 产出 Action channel request
- **THEN** request MUST进入 portable MotionResolver
- **AND** World Solver MUST返回唯一 body result
- **AND** Unity Transform MUST不成为 canonical state

### Requirement: NetworkStage 是正式边界但不实现真实 transport

Character simulation output/input adapter MUST只暴露 portable simulation input、gameplay facts、body state 和 presentation commands。Model-owned Driver/adapter MUST在 Kernel 外完成 policy、packet、history、snapshot 和 replay 映射。旧 CharacterNetworkReceiveStage/SendStage 若只服务 monolith pipeline MUST删除；不得保留一套 stage 与新 Driver 双写相同事实。

#### Scenario: Model 构造网络消息

- **WHEN** ServerAuthoritative 或 Rollback model 发送当前 Tick 数据
- **THEN** model adapter MUST从正式 Driver input/output ledger 构造 packet
- **AND** SimulationKernel MUST不创建或保存 packet

### Requirement: Timeline 和动画 tick 权威归属 pipeline

SimulationKernel MUST拥有 Timeline gameplay time，Presentation Committer/Stage MUST拥有 Timeline visual sample 请求，Animancer MUST拥有动画状态与 fade。Driver replay MAY重建 Timeline gameplay state，但 MUST不推进 Animancer；PresentationFrame MUST按最新 accepted/predicted simulation sample 和真实 render delta 连续采样。

#### Scenario: 30Hz gameplay 与 120Hz presentation

- **WHEN** 两个 SimulationTick 之间执行多个 RenderFrame
- **THEN** Presentation MUST连续重采样 Timeline visual time和 Animancer fade
- **AND** MUST不创建额外 gameplay Tick 或事实

## ADDED Requirements

### Requirement: CharacterPipelineHost 必须绑定 Program Actor 与客户端 Adapter

CharacterPipelineHost MUST只负责引用 Character authoring/compiled Program、Unity input adapter、Unity World Solver adapter、Presentation adapter 与 camera/visual resources，并把 actor 注册到当前 Session Driver。Host MUST不选择 Network Model 内部策略，不持有 history/replay/correction，也 MUST不直接 tick Program。

#### Scenario: Host 加入 Local Session

- **WHEN** 场景角色没有网络 Session binding
- **THEN** Host MUST通过正式 Local Driver 创建 actor
- **AND** MUST使用 compiled Program 而不是 RootTree runtime clone

#### Scenario: Host 加入网络 Session

- **WHEN** model binding 将 ActorId 与 Host 关联
- **THEN** Host MUST把 Program/state/presentation ports 注册给 model Driver
- **AND** MUST不根据 model id 修改 Graph 或 solver

## REMOVED Requirements

### Requirement: CharacterPipeline 支持混合架构 authority mode

**Reason**：`CharacterInputSource + CharacterMotionAuthority` 把 actor 调度和位姿来源作为 CharacterPipeline 内部分支，无法表达 Session 级 rollback world，也让 ExternalPose 与 LocalSolver 成为模型策略泄漏。新设计由 Simulation Driver actor binding 决定本地模拟、权威模拟或 snapshot sampling。

**Migration**：删除 `CharacterMotionAuthority`、ExternalPose/None motion branch 和 Host 序列化字段；Local、ServerAuthoritative 与 Rollback 都通过正式 Driver + World Solver 组合。

#### Scenario: 迁移远端角色

- **WHEN** ServerAuthoritative model 创建 remote actor
- **THEN** model Driver MUST消费 snapshot 并向 Presentation 提供 sample
- **AND** MUST不创建 ExternalPose CharacterPipeline 作为兼容路径


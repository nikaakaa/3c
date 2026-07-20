# Change: 将角色 Gameplay 收口为分阶段可移植模拟核心

## Why

当前 `CharacterPipeline.LogicTick` 在单个角色对象内部依次推进 Input、NetworkReceive、BTSMTL runtime clone、StateMachine、Timeline、Motion、GameplayEffect、NetworkSend 和 Presentation sample。影响未来 Tick 的状态分散在 RunnableNode、StateMachineGraphRuntime、TimelinePlaybackScheduler、PipelineBlackboardRuntime、ActionRuntime、GameplayEffectRuntime、CharacterMotionStage 和 Unity scene object 中。

这条链可以运行当前 Unity 单机角色，但不能成为多种运行模型的共同核心：

- 纯 .NET 宿主无法加载 Unity authoring object 或依赖 MonoBehaviour 生命周期；
- 单个角色在自己的 Tick 内直接调用 `CharacterController.Move`，无法为多角色世界建立统一输入批次、稳定求解顺序和世界快照；
- 状态只能按模块局部恢复，不能原子恢复 Character gameplay、Body 和 World Solver 状态；
- 现有 ServerAuthoritative adapter 直接抓取 Character NetworkSend/ReceiveStage，网络模型与旧 Pipeline 阶段绑定；
- 逻辑重演会重新触发动画、相机、Cue、VFX、UI 和网络发送等外部副作用。

原设计还把未来 Deterministic Rollback 的定点要求提升成所有 Simulation Program 的公共 ABI，迫使本地单机、Unity 权威进程、普通客户端预测、DotRecast 服务端、Timeline、Blackboard 和 GameplayEffect 全部使用同一 `SimScalar`。这会让一种网络模型的数值约束污染所有执行模型，也与 current spec 中“确定性数值归独立完整 Network Model”的边界冲突。

本 change 只建立共同模拟核心，不决定具体网络拓扑、预测范围、修正算法、远端角色策略或确定性碰撞实现。它把现有 authoring 编译为 `CharacterSimulationProgram`，由 `SimulationSessionRuntime` 按 `Evaluate -> ResolveWorldBatch -> Finalize -> PublishState` 四阶段推进。完成时只交付正式 `LocalSimulationDriver + UnityCharacterControllerWorldSolver` 单机闭环，并完整迁移 Corin 当前业务。

## What Changes

- **BREAKING** 将正式角色 gameplay runtime 从 Unity authoring clone/节点虚方法解释执行迁移为 `Authoring -> numeric-neutral Gameplay Semantic IR -> explicit Numeric Target compiler -> immutable CharacterSimulationProgram -> SimulationSessionRuntime`。
- 以 `CharacterPipelineDefinition` 为唯一编译根，递归解析 RootTree、inline/shared Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 motion curve。
- Semantic IR 只表达稳定 identity、operation 语义、控制流、状态声明、数值字面量和能力要求，不保存 Float/Fixed runtime value，也不按 Network Model 复制业务 operation。
- Numeric Target 是 Program、Input、CharacterState、WorldRequest/Result、GameplayFact 和 Snapshot ABI 的编译目标。ProgramHash 必须包含 source revision、compiler version、operation-set version、NumericProfile 和 required world capability。
- 本 change 只实现并安装 `Float32` target，用于 Local、Unity authoritative 和后续普通 C# authoritative 组合；`FixedQ32.32` target、确定性数值库和确定性 operation 审核留给 `add-deterministic-rollback-kcc-model`，不在公共 Core 中预埋伪实现。
- 编译产物分为无 Unity 引用的 `CharacterSimulationProgram` 与客户端专属 `CharacterPresentationProjection`；两者共享稳定 producer/source identity，但 Presentation 不是第二份 gameplay 数据源。
- 将单角色逻辑状态收口为 `CharacterSimulationState`，将世界位姿、碰撞体和 Solver 可变数据收口为 `WorldSimulationState`，由 `SimulationWorldSnapshot` 原子聚合 ProgramCatalog、Tick、按 ActorId 排序的角色绑定/状态和世界状态。
- 新增不可变 `SimulationProgramCatalog`。一个 Session 可以装配多个 Program，但每个 Actor roster entry 必须显式绑定唯一 ProgramId/ProgramHash/LayoutHash；运行中不得热替换 Program 或悄悄迁移 state。
- 每个 Program artifact 显式声明唯一 NumericProfile；同一 Session 只能装配 NumericProfile、Kernel specialization、Driver input adapter、WorldSolver 和 State/Snapshot codec 完全匹配的组合，运行中不能切换 target 或跨 target 恢复 Snapshot。
- Float32 target 对 NaN、Infinity、非法 normalized direction、非稳定遍历和平台 Random 明确失败；它不宣称跨机器 bitwise deterministic，也不承担 Fixed target 的量化范围和溢出策略。
- 将设备输入、Camera-relative 方向和外部命令归一为 portable `CharacterSimulationInput`；Program operation 不读取 InputAction、Camera、Transport 或 Network packet。
- 新增无状态 `SimulationKernel.Evaluate` 与 `SimulationKernel.Finalize`：Evaluate 只执行角色业务决策并产生 pending state 与 motion/world requests；Finalize 只消费精确匹配的 world result 并生成新角色状态、gameplay facts 和带稳定 EventId 的 presentation commands。
- 新增 session 级 `SimulationSessionRuntime`，对一个 Tick 的全部 actor 按稳定 ActorId 顺序 Evaluate，一次调用 `ICharacterWorldSolver.ResolveBatch`，再按同序 Finalize。Kernel 不在单角色调用内直接执行 concrete solver。
- 收窄 `ISimulationDriver`：Driver 只提供 Tick plan、按 ActorId 排序的控制输入、模型无关 typed `SimulationIngress`、可选原子 restore 请求和 `SimulationOutputPlan`；Driver 不实现 Program operation、World Solver 或 Presentation，也不决定 Gameplay state 是否生效。
- `ICharacterWorldSolver` 只拥有世界约束、WorldSimulationState 和批量 body result。Solver 必须声明 `Reconstructible`、`Snapshotable`、`DeterministicReplay` 等真实能力，组合根不得把缺失能力当作默认支持。
- `SimulationSessionRuntime` 在全部 Finalize 和 OutputPlan 校验成功后原子发布 Character/World state；`SimulationCommitter` 随后只消费 OutputPlan，将 animation、camera、cue、VFX、UI 和 model adapter 等外部副作用送往各自端口。Structured Trace 由各正式边界独立送入只读 diagnostics，不受 OutputPlan 控制。Kernel replay 不直接触发表现或模型端口。
- 本 change 只安装 Local Driver 与 Unity CharacterController Solver。Unity Solver 允许 float 内部计算并从显式 body state 重建，但不声明 `DeterministicReplay`。
- 删除 `CharacterMotionAuthority`、Character NetworkSend/ReceiveStage、MotionStage correction、ExternalPose 公共分支及依赖它们的旧 ServerAuthoritative Character adapter/binding。现有 ServerAuthoritative model 在没有正式 Simulation Driver adapter 时必须不可选，不保留桥接。
- 保留 SessionHost/ModelDefinition 的 model-neutral 生命周期边界，但核心不替后续模型定义 packet、history、prediction、reconciliation、rollback、remote actor 或 endpoint 语义。
- 迁移 Corin 正式资产后删除角色主线旧 object interpreter、runtime clone、隐藏 gameplay state、runtime compile fallback、一次性 migrator 和旧序列化配置。

## Confirmed Design Boundary

1. Authoring 继续使用当前 BTSMTL、StateMachine、Timeline、TreeClip 和 Blackboard，不增加 ordinary/deterministic 两套节点。
2. 同一 Authoring 与同一 Semantic IR operation 语义服务 Local、ServerAuthoritative 和未来 Rollback；不同 Numeric Target 生成不同 ProgramHash、State Layout、Kernel specialization 和 Snapshot ABI，具体模型不得复制节点或 gameplay 业务实现。
3. 本 change 的 Float32 Program 必须有稳定遍历、事件顺序与 canonical serialization，但不宣称跨平台 bitwise deterministic；完整确定性要求 Fixed Numeric Target 与 Deterministic WorldSolver 同时成立。
4. KCC 不是 Kernel 的职责。本 change 不实现 Deterministic KCC，也不把 DotRecast 当作 KCC。
5. 世界求解必须是 session 级批次，不能由每个 Character 在自己的 Tick 中直接移动世界对象。
6. Character state、World state、Driver/model state 和 Presentation state 必须分属不同 owner；只有前两者组成 gameplay world snapshot。
7. Driver 可以提交 typed ingress、保存 snapshot/history 并申请 restore，但只能通过 SimulationSessionRuntime 的正式入口改变模拟，不能直接修改 Kernel 内部对象。
8. 本 change 结束时网络模型可以明确不可用；不得为了保住旧 LocalLoopback 演示而维持旧 NetworkStage 或 correction 路径。
9. Session roster 在启动时锁定；本 change 不允许 Driver 在运行 Tick 中增删 Actor。每个 roster entry 必须绑定 ProgramCatalog 中的唯一 Program，动态 spawn/despawn 留给单独 change 设计 world-level lifecycle。
10. Driver 的 OutputPlan 只决定外部 EventId 的 Publish/Replace/Retire/Suppress，不得接受、拒绝或改写 Finalize 产生的 Gameplay state。
11. Numeric Target 只能在 Session 创建前作为完整组合选择，不能由 Graph、Node、Driver Tick 或 Network packet 在运行中切换。

## Acceptance Boundary

1. Corin 单机 Sandbox 只经过 compiled Program、SimulationSessionRuntime、Local Driver、Unity batch Solver 和 Committer 运行。
2. 闪避、转身、RunStart/Loop/End、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve、GameplayEffect 和动画表现保持当前业务语义。
3. 一个 Tick 的顺序固定为 `Driver plan -> atomic restore -> all actors Evaluate -> one ResolveBatch -> all actors Finalize -> Driver BuildOutputPlan -> atomic state publish -> Committer`。
4. Semantic IR、Float32 Program reader、Float32 Kernel、State/Snapshot 和 batch contracts 不依赖 Unity authoring object，并可由 Unity asmdef 与普通 .NET csproj 共享编译。
5. `CharacterSimulationState` 与 `WorldSimulationState` 可独立编码，并可通过匹配 ProgramCatalogHash、每 Actor Program/layout binding、solver identity、world revision、roster 和 Tick 的 `SimulationWorldSnapshot` 原子恢复。
6. Unity Solver 只声明其真实能力；Float32 target 与 Unity CharacterController 均不得被标记为 DeterministicReplay。
7. 任意影响未来 Tick 的状态必须归属 Character state、World state 或具体 Driver state；缺失 Emitter、未声明状态、stale Program、断裂引用和能力不匹配直接失败。
8. 不存在 interpreted fallback、runtime compile fallback、新旧 Timeline runtime、两套 Blackboard、每角色直接 WorldSolver 调用、公共 NetworkStage 或旧 ServerAuthoritative bridge。

## Non-Goals

- 不实现 Fantasy endpoint、Room、动态 roster/spawn/despawn、Unity 权威服务端或双客户端 Demo；Local composition 只装配启动时锁定的 Corin roster。
- 不决定 ServerAuthoritative 的预测范围、correction 粒度、remote actor 执行方式或复制 payload。
- 不引入 DotRecast、NavigationSurface Solver 或纯 .NET server runner。
- 不实现 Deterministic KCC、canonical input bundle、rollback protocol、state-hash exchange 或全局帧同步。
- 不实现 FixedQ32.32 Program、Fixed Kernel specialization 或任何可选择但不可运行的 deterministic Numeric Target。
- 不支持多角色碰撞、moving platform、Rigidbody、动态破坏或命中判定；批量 WorldSolver 合同只为这些能力保留正确 ownership。
- 不保证 Unity CharacterController、Unity scene query 或 Presentation 跨平台确定。
- 不为普通节点增加 IsServer、IsRollback、Network Model enum 或数值 backend switch。
- 不新增测试或人工验证 task，不运行 Unity batchmode。

## Current Spec Comparison

- `character-pipeline-runtime` 当前以 CharacterPipeline stages 和 runtime object 为执行主体；本 change 替换为 Program、SessionRuntime、Kernel、Driver、WorldSolver 和 Committer。
- `btsmtl-sm-node-authoring`、`btsmtl-runnable-timeline-node` 和 `character-pipeline-blackboard` 当前以 runtime clone/object identity 隔离状态；本 change 保留 authoring ownership，改用 operation handle、state slot 和 activation owner address。
- `character-motion-simulation-boundary` 当前使用单角色 MotionStage、LogicPosePort 和 MotionExecutor，并明确确定性模型拥有自己的确定性数值。本 change 保留该数值隔离原则，同时将共享部分提升为 numeric-neutral Semantic IR、批量 WorldSolver 形状和独立 Character/World ownership；本 change 的正式执行 ABI 为 Float32。
- `character-input-pipeline` 当前由 CharacterInputStage 和 GraphContext 保存输入/history；本 change 将设备采样留在 Adapter，将有效 Tick input 写入 Driver plan，模型 history 仍归具体 Driver。
- `character-animation-pipeline` 当前由 TimelinePlaybackScheduler 同时推进 Gameplay TreeClip 与 visual sampling；本 change 将 Gameplay Timeline 编译进 Program，只保留纯表现采样与 Animancer lifecycle。
- `character-gameplay-effect-integration` 当前由 CharacterGameplayEffectAdapter 持有独立 runtime state；本 change 将 GE catalog 编译进 Program、GE state 纳入 CharacterSimulationState，并用 typed SimulationIngress 替代 NetworkReceive 注入。
- `character-motion-semantics` 与 `character-root-motion-curves` 当前以 CharacterMotionStage/MotionExecutor 为单角色执行边界；本 change 改为 Evaluate motion resolve、session batch WorldSolver 与 Finalize actual result。
- `character-network-sync-domain-contract` 当前保留 Character NetworkSend/ReceiveStage、ExternalPose 和 MotionStage correction；本 change 删除这些公共阶段，模型只能通过 Driver input/ingress/restore/OutputPlan 与 Committer ports 连接。
- `gameplay-network-model-boundary` 当前把 ServerAuthoritativeHybrid 描述为唯一完整可选模型；旧 adapter 删除后它在正式 Driver 未实现前必须不可选，Local 单机不需要 Network SessionHost。
- `local-gameplay-sync-loopback` 当前描述了基于旧 Character adapter 的最小闭环；本 change 只允许保留模型专属 endpoint/packet 模块，不能把它宣传为已接入新核心的 playable model。
- `gameplay-tick-system` 当前按每个 Character target 调用 LogicTick；本 change 将一个 Simulation Session 作为 logic target，统一批量推进其 actor，PresentationFrame 仍独立。
- `btsmtl-runtime-diagnostics` 已要求 Editor 与 runtime clone 解耦；本 change 将 Program operation、state slot、world batch、snapshot 和 EventId 纳入现有 Source Map/Trace。
- `project.md` 仍包含已归档 change、已删除 `add-local-two-client-gameplay-network-closure` 和旧 ExternalPose/NetworkStage 口径，apply 末尾必须更新，不能继续作为实现真相。

## Downstream Changes

- `refactor-server-authoritative-hybrid-runtime`、`add-dotrecast-authoritative-server-backend` 和 `add-deterministic-rollback-kcc-model` 当前只视为下游草案，不反向规定本 change 的接口。
- 核心完成并归档后，三个下游 change 必须重新审查并更新 proposal/design/spec/tasks。Unity authoritative 与 DotRecast authoritative 默认复用 Float32 target；如果服务端选择其它 target，客户端预测 ProgramHash 与 Snapshot 合同也必须显式匹配，不能声称“同一 Program bytes”。
- `add-deterministic-rollback-kcc-model` 必须新增正式 FixedQ32.32 Numeric Target、target lowering、Kernel specialization、State/Snapshot codec 和 deterministic operation capability 审核，并与 Deterministic WorldSolver/KCC 共同装配；它复用 Semantic IR，不复用 Float32 Program ABI。

## Impact

- 新能力：`btsmtl-gameplay-semantic-ir`、`btsmtl-compiled-simulation-program`、`character-simulation-kernel`。
- 修改能力：`btsmtl-graph-core`、`btsmtl-node-interruption-lifecycle`、`btsmtl-sm-node-authoring`、`btsmtl-runnable-timeline-node`、`btsmtl-runtime-diagnostics`、`btsmtl-timeline-editor-preview`、`character-action-activation-flow`、`character-action-instance-runtime`、`character-animation-layer-runtime`、`character-animation-pipeline`、`character-animation-presentation-authoring`、`character-camera-pipeline`、`character-gameplay-effect-authoring`、`character-gameplay-effect-integration`、`character-gameplay-pipeline-closure`、`character-gameplay-sync-adapter`、`character-input-pipeline`、`character-motion-semantics`、`character-motion-simulation-boundary`、`character-network-sync-domain-contract`、`character-pipeline-blackboard`、`character-pipeline-runtime`、`character-presentation-interpolation`、`character-root-motion-curves`、`character-state-interruption-authoring`、`character-state-timeline-authoring-loop`、`gameplay-effect-runtime`、`gameplay-tick-system`、`gameplay-network-model-boundary`、`gameplay-sync-backend-selection`、`gameplay-sync-runtime`、`local-gameplay-sync-loopback`、`server-authoritative-hybrid-sync-model`、`tengine-hotupdate-foundation`。
- 客户端：Compiler/Program/State/Kernel、SimulationSessionRuntime、Local Driver、Unity batch Solver adapter、Presentation Projection/Committer、Program Inspector 和 Diagnostics。
- 资产：Corin Program artifact、Presentation Projection、Local simulation composition 和最终单机 Sandbox 配置。
- 删除：角色主线旧 interpreter/runtime clone、CharacterMotionAuthority、LogicPosePort 双真值、MotionStage correction、Character NetworkSend/ReceiveStage、ExternalPose 公共分支、旧 ServerAuthoritative Character adapter/binding、一次性 migrator 和 fallback。

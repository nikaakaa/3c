# Change: 将角色模拟重构为可插拔网络模型

## Why

当前项目已经把 Network Model、Endpoint、Transport、角色运动语义和具体 `CharacterController` 执行器分开，但“可替换运动执行器”还不等于“可替换完整模拟模型”。`CharacterPipeline.LogicTick` 仍把输入采集、BTSMTL 对象解释执行、Timeline、Motion、网络语义输入输出、表现采样和调试串在同一个角色对象中；`CharacterMotionStage` 仍按 `CharacterMotionAuthority` 分支，并直接拥有当前 ServerAuthoritative 的位姿 correction 算法。BTSMTL runtime 还依赖 authoring clone、节点私有字段、`UnityEngine`、`Time.deltaTime`、Unity Random 和运行时对象引用。

这套结构可以继续增加 Unity Motion Executor，却不能让同一份 Corin 业务规则同时运行在 Unity 权威进程、纯 C# DotRecast 权威服务端和确定性 KCC rollback 三种组合中：纯 C# 服务端无法执行 Unity authoring object；rollback 无法完整 capture/restore 节点、StateMachine、Timeline、Blackboard、Action、GameplayEffect 和 world state；历史重演还会重复发送网络事实或提交动画、相机、Cue 等副作用。

目标不是为第三种模型复制一套“确定性节点”，也不是让 Graph 保存网络模式。目标是把 BTSMTL authoring 编译为唯一、不可变、可移植的 `CharacterSimulationProgram`，由唯一 `SimulationKernel` 对集中式 `SimulationState` 执行。单机、ServerAuthoritative 和 DeterministicRollback 只在 Session composition root 选择 Driver；Unity CharacterController、DotRecast navigation solver 和 Deterministic KCC 只作为 World Solver 组合。Graph、StateMachine、Timeline、Action 和 GameplayEffect 业务规则始终只有一份。

## What Changes

- **BREAKING** 将 BTSMTL 正式 gameplay runtime 从 authoring object clone/节点虚方法解释执行迁移为 `Authoring -> Compiler -> immutable CharacterSimulationProgram -> SimulationKernel`。节点资产继续负责编辑数据，runtime operation 和全部可变状态进入纯 C# program/state；迁移完成后删除角色主线旧解释执行、runtime clone、隐藏节点状态和 runtime authoring-object 访问路径。
- 新增确定性、可移植的 Simulation 数据合同：固定 Tick、稳定 Actor/Program/Execution identity、定点或整数 gameplay 数值、集中式 Node/StateMachine/Timeline/Blackboard/Action/GameplayEffect/Body/RNG 状态、完整 capture/restore、稳定执行顺序和程序 hash。
- `CharacterPipelineDefinition` 继续作为唯一角色 authoring 聚合入口，但运行时只加载由其编译出的 `CharacterSimulationProgramAsset` 与客户端 Presentation projection。编译产物包含 Program Manifest、state layout、operation tables、Timeline/TreeClip 逻辑数据、portable gameplay catalog、Debug Source Map 和内容 hash；纯 .NET 服务端读取同一 canonical program bytes。
- 将本地设备和外部输入先归一为 model-neutral `CharacterSimulationInput`。相机相对移动在输入适配层转换为量化世界方向或量化 camera yaw；Graph runtime 不读取本地 Camera、InputAction、transport 或 model packet。
- 将角色逻辑执行收口为无网络、无表现副作用的 `CharacterSimulationKernel.Step`。Kernel 只接收 Tick、输入、当前 SimulationState 和 `ICharacterWorldSolver`，输出新状态、gameplay facts、motion result 与带稳定 EventId 的待提交表现命令。
- 将 Session 级调度拆为 `ISimulationDriver`。`LocalSimulationDriver` 单次推进并立即提交；现有 `ServerAuthoritativeHybrid` model session 拥有 prediction/authority/snapshot/correction Driver；新增完整 `DeterministicRollback` model session，拥有 canonical input bundle、world snapshot、history、restore/replay、state hash 和 side-effect commit。
- 将世界执行拆为 `ICharacterWorldSolver`。Unity CharacterController adapter、纯 C# DotRecast navigation-surface solver 和 Deterministic KCC 分别实现正式能力；DotRecast Demo 明确只覆盖静态 NavMesh 表面约束，不冒充通用 KCC 或确定性物理。
- **BREAKING** 从 Character 主线删除 `CharacterMotionAuthority`、`ExternalPose` 总控分支以及 MotionStage 内的模型 correction policy。Actor 是否本地模拟、外部采样或参与 rollback 由当前 model Driver 的 actor binding 决定；World Solver 只执行当前模型交给它的 motion request。
- 保持 `GameplayNetworkSessionHost -> GameplayNetworkModelDefinition -> model session` 为唯一网络模型插件入口。Model definition 声明所需 Program/World/Host 能力；Inspector 只显示已完整实现且组合校验通过的模型，不把 Local、Endpoint、Transport 或 World Solver 伪装成 Network Model。
- 交付三个使用同一 Corin Program、同一地图和同一输入语义的本地双客户端 Demo：
  - `ServerAuthoritativeHybrid + Unity authoritative process + Unity CharacterController`；
  - `ServerAuthoritativeHybrid + pure .NET host + DotRecast navigation-surface solver`；
  - `DeterministicRollback + canonical input stream + Deterministic KCC`。
- 保持动画、相机、音效、VFX 和 UI 在 Presentation/Commit 层。网络不复制 AnimationClip、Timeline producer 或 Animancer state；rollback 重演只重建 simulation output，表现命令按 EventId 去重、替换或撤销，不重复提交外部副作用。
- 保留并扩展现有 Debug Source Map/structured Trace 合同，使编译 instruction、SimulationState、snapshot、rollback、ProgramHash、solver result 和 model Driver 都可回到原 Graph/Timeline source；Diagnostics 继续只读且不进入状态 hash。
- 直接迁移 Corin RootTree、nested StateMachine、Timeline/TreeClip、Blackboard、Action、GameplayEffect、motion curve 和正式配置。删除旧 runtime clone、旧自执行节点路径、旧 correction 主线、废弃 authority 配置、重复 command/history 和一次性 migrator，不保留兼容 parser、fallback 或双写。
- 本 change 完整吸收并取代未实施的 `add-local-two-client-gameplay-network-closure`；两者不得并行 apply。其 Fantasy endpoint、双人 Room、roster、remote presentation、协议和有界诊断需求按新的 Program/Driver/World Solver 边界实现。

## Demo Scope

三个 Demo 共用以下 gameplay 纵切：

1. 两个本地客户端各控制一个 Corin，并看到另一个角色。
2. 移动、转向、按一下 Shift 闪避、闪避结束按输入进入 Run、MovingTurn、Attack1/Attack2 连段、打断和 Timeline Window 正常工作。
3. Timeline motion curve、Action/GameplayEffect/Blackboard 结果由 SimulationKernel 产生；动画、相机和 Cue 由客户端 Presentation 消费。
4. Unity 与 DotRecast 方案使用服务端权威状态、Owner prediction/reconciliation 和 Remote snapshot interpolation。
5. DeterministicRollback 方案按 canonical input stream 推进两名角色，保存有界 world snapshot，处理迟到输入并重演，周期交换 state hash，严重失配时使用正式权威 snapshot 恢复。
6. 三个 Demo 暴露一致的 RTT、带宽、prediction error、correction、rollback count、replayed ticks、state hash 和 queue health 数据。

Demo 世界固定为静态场景约束和角色 capsule；不增加 Rigidbody 玩法。DotRecast 只验证静态 NavMesh 表面运动；Deterministic KCC 只保证本 Demo 已声明的静态世界、角色 body、Corin gameplay state 和 stable actor order，不宣称支持任意 Unity Physics 世界。

## Non-Goals

- 不在 Session 运行中切换 Network Model、World Solver 或 Program。
- 不为普通节点增加 `IsServer`、`IsClient`、`IsRollback`、`IsSinglePlayer` 或 model enum。
- 不维护普通节点与 deterministic node 两套 authoring、两套业务图或两套 gameplay runtime。
- 不把 Endpoint、Transport、Unity process 和 DotRecast 称为独立 Network Model。
- 不把 DotRecast `MoveAlongSurface` 宣称为完整 KCC、动态碰撞或确定性求解。
- 不实现任意 Rigidbody、布料、破坏、动态 NavMesh、完整 PvE、Objective、命中伤害、lag compensation、账号、匹配、数据库或断线续局。
- 不同步动画 clip time、Animancer state、相机、音效、VFX 或 UI。
- 不允许 unsupported operation、ProgramHash mismatch、缺失 solver、缺失 endpoint 或快照损坏时回退旧解释器、LocalLoopback、ExternalPose 或直接 Transform。
- 不新增测试或人工验证 task，不运行 Unity batchmode。

## Current Spec Comparison

- `gameplay-network-model-boundary` 的 Session 级唯一模型、Model/Endpoint/Transport 分层和 BTSMTL 不保存网络配置仍然正确；本 change 增加 model capability contract，并在完整实现后让 `DeterministicRollback` 成为第二个正式 definition。
- `character-pipeline-runtime` 当前要求 `CharacterPipeline` 自身持有 `CharacterInputSource`、`CharacterMotionAuthority` 并串行拥有 input/BTSMTL/motion/presentation/network stages；目标改为 SimulationKernel、model Driver 和 Presentation adapter 分层，因此相关要求必须修改，旧 monolith 不保留。
- `character-motion-simulation-boundary` 当前把 correction phase 和 `CharacterMotionAuthority` 放在 MotionStage，并让未来 deterministic model 不受 float executor 合同约束；目标将模型 correction 移回 ServerAuthoritative Driver，并新增独立 deterministic solver contract，修改但不恢复 concrete backend 侵入 Graph。
- `btsmtl-sm-node-authoring` 与 `btsmtl-runnable-timeline-node` 当前要求从 authoring graph/timeline 创建隔离 runtime clone；目标用编译 instruction 和 state slot 隔离实例，必须删除 clone 作为正式 runtime 语义。
- `character-pipeline-blackboard` 当前按 runtime object identity 和 dictionary address 保存值；目标把 declaration 和 scope owner 编译成稳定 state layout/address，保持 authoring scope 语义但修改 runtime 存储。
- `character-input-pipeline` 当前让 GraphContext 读取 `CharacterInputFrame`，并把 History 放在 Character input stage；目标保留 authoring input id 和本地 capture，但 Kernel 读取 portable simulation input，replay history 归当前 Model Driver。
- `gameplay-tick-system` 当前只区分 LocalLogicTick、RenderFrame 和 ServerTick，并规定 ServerTick 只作为网络输入；目标新增 Session SimulationTick 与 replay pass，且 Presentation 仍保持独立 RenderFrame。
- `btsmtl-runtime-diagnostics` 已明确支持未来 compiled instruction，并禁止 Editor 绑定 runtime clone；该方向保持，只补充 Program/state/snapshot/rollback 的正式 trace identity。
- `character-presentation-interpolation` 的 visual root、Timeline visual sampling 和 Animancer 权威保持不变；只把输入改为 Driver committed/predicted simulation samples，并加入 rollback replacement/dedup 语义。
- `server-authoritative-hybrid-sync-model` 的 packet/history/model ownership 保持；Unity 与 DotRecast 是该模型的两个正式 Demo 部署组合，不创建两个 model definition。
- `character-gameplay-pipeline-closure` 当前仍以单个 CharacterPipeline、NetworkSend/Receive stage 和“第一阶段 LocalLoopback”表达业务闭环；目标改为 Program/Kernel/Driver/Committer 唯一主线，并将 Demo 口径改为同一纵切下的三种组合。
- `character-network-sync-domain-contract` 当前仍以 Character NetworkSendStage、NetworkReceiveStage、ExternalPoseCorrection 和 MotionStage correction 表达输入输出；目标改为 model-owned input/output adapter 与 Driver history，保留 SyncDomain 的业务分类但删除旧 stage。
- `gameplay-sync-backend-selection` 的 SessionHost/Model/Endpoint 归属方向保持；完整模型从唯一 ServerAuthoritativeHybrid 扩展为 ServerAuthoritativeHybrid 与 DeterministicRollback，不将 Host 或 World Solver 当成模型。
- `gameplay-sync-runtime` 的 Common Session 保持 model-neutral，但必须能装配两个完整模型；packet、history、snapshot、correction 和 rollback 仍分别归属具体模型。
- `project.md` 当前明确“不做全局确定性帧同步”且当前模型唯一为 ServerAuthoritativeHybrid；本 change 完成后该口径过期，必须更新为“作品主线仍是 ServerAuthoritativeHybrid，DeterministicRollback 是隔离的对比 Demo 模型”。
- `add-local-two-client-gameplay-network-closure` 的前提是先选唯一 backend，且明确不做 rollback。它与本 change 的多组合对比目标冲突；有效需求并入本 change 后必须撤销该 active change，不能继续 apply。

## Impact

- 新能力：`btsmtl-compiled-simulation-program`、`character-simulation-kernel`、`deterministic-rollback-network-model`、`network-model-comparison-demo`。
- 修改能力：`btsmtl-sm-node-authoring`、`btsmtl-runnable-timeline-node`、`btsmtl-runtime-diagnostics`、`character-gameplay-pipeline-closure`、`character-pipeline-runtime`、`character-input-pipeline`、`character-motion-simulation-boundary`、`character-network-sync-domain-contract`、`character-pipeline-blackboard`、`character-presentation-interpolation`、`gameplay-tick-system`、`gameplay-network-model-boundary`、`gameplay-sync-backend-selection`、`gameplay-sync-runtime`、`server-authoritative-hybrid-sync-model`。
- 客户端：BTSMTL compiler/program/state、Character simulation core、Local/ServerAuthoritative/Rollback drivers、Unity solver adapter、Presentation committer、model/profile Inspector、Demo launcher 与 diagnostics。
- 服务端：共享纯 C# SimulationKernel source、Program loader、Fantasy endpoint/protocol、Unity authoritative host、.NET DotRecast host、Deterministic KCC host、Room/roster/tick/history/hash。
- 资产：Corin Definition 的 compiled program、presentation projection、三个完整 Demo definitions/manifests、两客户端场景和正式 server launch 配置。
- 删除：角色正式旧解释执行、runtime authoring clone、`CharacterMotionAuthority`、MotionStage 模型 correction、旧 ExternalPose 主线、未实施双客户端 change、旧 FrameSync 协议、静态 Session facade、重复 history/command/schema、一次性 migrator、fallback 与兼容路径。

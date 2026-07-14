# Change: 将角色 Gameplay 收口为可移植模拟核心

## Why

当前 `CharacterPipeline.LogicTick` 同时编排输入、BTSMTL authoring clone、StateMachine、Timeline、Motion、Network stage、Presentation sample 和 diagnostics。可变 gameplay 状态分散在 RunnableNode、StateMachine runtime、Timeline scheduler、Blackboard、ActionRuntime 和 GameplayEffectRuntime 内，部分节点还直接读取 Unity `Time`、Random、Camera 或 Input System。

这条路径能够运行 Unity 单机角色，却无法作为后续网络模型的共同执行核心：纯 .NET 宿主不能加载 Unity authoring object；完整状态不能原子 capture/restore；历史重放会重复提交动画、相机、Cue 和网络副作用。如果分别为 ServerAuthoritative、DotRecast 和 Rollback 增加自己的角色执行器，会直接生成多套业务节点和运行真相。

本 change 只建立共同核心：把现有角色 authoring 编译为唯一 `CharacterSimulationProgram`，由纯 C# `SimulationKernel` 对集中式 `SimulationState` 执行。本 change 完成时只提供 `LocalSimulationDriver + UnityCharacterControllerWorldSolver` 一个可玩闭环，并把 Corin 当前移动、闪避、转身、攻击连段、打断、Timeline Window 和 GameplayEffect 完整迁移进来。

## What Changes

- **BREAKING** 将角色正式 gameplay runtime 从 authoring object clone/节点虚方法解释执行迁移为 `Authoring -> Compiler -> immutable CharacterSimulationProgram -> SimulationKernel`。
- 以 `CharacterPipelineDefinition` 作为唯一编译根，递归解析 RootTree、inline/shared Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 motion curve。
- 新增无 `UnityEngine.Object` 引用的 Program manifest、operation table、state layout、scope layout、portable catalog、source map、capability manifest 和稳定 ProgramHash。
- 将 Runnable、StateMachine、Timeline、Blackboard、Action、GameplayEffect、Body、RNG 和 sequence 的全部可变数据收口到 `SimulationState`，提供完整 capture/restore 和稳定 state hash。
- 将设备输入、Camera-relative 方向和外部输入归一为 portable `CharacterSimulationInput`；Kernel 不读取 InputAction、Camera、Transport 或 Model packet。
- 新增无网络、无表现副作用的 `SimulationKernel.Step`：输入 Program、旧 State、SimulationTick、Input 和 `ICharacterWorldSolver`，输出新 State、gameplay facts、body result 和带稳定 EventId 的 presentation commands。
- 建立最终 `ISimulationDriver` 与 `ICharacterWorldSolver` 插件合同；本 change 只实现 Local Driver 和 Unity CharacterController Solver，不增加网络模型开关。
- 将 Timeline 逻辑采样与客户端 AnimationClip/Animancer 绑定分为 Program 与 `CharacterPresentationProjection`；Projection 不是第二份 gameplay 数据源。
- 将动画、相机、Cue、VFX、UI 和 diagnostics 收口到 Committer/Presentation；Kernel replay-safe output 不直接触发 Unity 副作用。
- 将现有 ServerAuthoritativeHybrid 的 Character binding 迁移到最终 Driver/input/output 端口，但不在本 change 新增 Fantasy endpoint、Unity 权威服务端、双客户端 Demo 或新网络策略。
- 迁移 Corin 正式资产后删除角色主线旧 object interpreter、runtime clone、隐藏状态、`CharacterMotionAuthority`、公共 NetworkSend/Receive 双写 stage、MotionStage model correction 和一次性 migrator。

## Acceptance Boundary

1. Corin 在单机 Sandbox 只经过 compiled Program、Kernel、Local Driver、Unity Solver 和 Committer 运行。
2. 闪避、转身、RunStart/Loop/End、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve、GameplayEffect 和动画表现保留当前业务语义。
3. Runtime gameplay assembly 不依赖 Unity authoring object，纯 C# Program/State/Kernel 源集可被 Unity asmdef 与普通 .NET csproj 共享编译。
4. 任意影响后续 Tick 的状态都必须进入 State Layout；缺失 emitter、不可移植调用、stale Program 或断裂引用直接失败。
5. 不存在 interpreted fallback、runtime compile fallback、新旧 Timeline runtime、两套 Blackboard 或两套 motion 结算。

## Non-Goals

- 不实现 Unity 权威服务端、Fantasy 真实 endpoint、Room、roster 或双客户端 Demo。
- 不引入 DotRecast、纯 .NET 权威服务端部署或 NavigationSurface Solver。
- 不实现 Deterministic KCC、canonical input bundle、history replay、rollback protocol 或全局帧同步。
- 不保证 Unity CharacterController 输出具有跨平台确定性。
- 不为普通节点增加 `IsServer`、`IsRollback` 或 Network Model enum。
- 不保留旧解释执行器作为未支持 operation 的 fallback。
- 不新增测试或人工验证 task，不运行 Unity batchmode。

## Current Spec Comparison

- `character-pipeline-runtime` 当前要求 CharacterPipeline 自身串行拥有 input/BTSMTL/motion/presentation/network stages；本 change 将其拆成 Program/Kernel/Driver/Solver/Committer，Host 只装配和注册。
- `btsmtl-sm-node-authoring` 与 `btsmtl-runnable-timeline-node` 当前把 runtime clone 作为隔离单元；本 change 保留 authoring ownership，改用 operation handle 与 State slot 隔离实例。
- `character-pipeline-blackboard` 当前按 runtime object identity/dictionary 寻址；本 change 保留 declaration/scope 语义，改用 Program 稳定 layout 与 owner generation 寻址。
- `character-input-pipeline` 当前让 GraphContext 读取 CharacterInputFrame；本 change 保留 authoring InputId，Kernel 只读取 portable CharacterSimulationInput。
- `character-motion-simulation-boundary` 当前仍由 CharacterMotionAuthority 与 MotionStage correction 分支决定运动；本 change 改为 Driver actor binding 与唯一 World Solver，model correction 不再泄漏进公共 motion。
- `character-network-sync-domain-contract` 当前保留 Character NetworkSend/ReceiveStage；本 change 保留 SyncDomain facts，改由 model-owned adapter 在 Kernel 外构造或注入模型语义。
- `server-authoritative-hybrid-sync-model` 的 model/session/packet/history ownership 保持；本 change 只将它的 Character adapter 迁到最终 core port，不扩大网络 Demo。
- `btsmtl-runtime-diagnostics` 已要求 Editor 与 runtime object layout 解耦；本 change 把 compiled operation、state slot、ProgramHash 和 snapshot identity 纳入现有 Source Map/Trace。
- `project.md` 当前把 BTSMTL 视为 authoring 基座而不强制照搬 runtime，与本 change 一致；Network Boundary 中 CharacterPipeline/MotionStage 旧执行口径必须随实现更新。

## Follow-Up Changes

- `refactor-server-authoritative-hybrid-runtime` 依赖本 change，实现 Fantasy + Unity 权威双客户端闭环。
- `add-dotrecast-authoritative-server-backend` 依赖本 change和 ServerAuthoritative host contract，实现纯 .NET + DotRecast 权威后端。
- `add-deterministic-rollback-kcc-model` 只依赖本 change 的 Program/State/Kernel/Driver/Solver 合同，可与 ServerAuthoritative 方向并行实施。

## Impact

- 新能力：`btsmtl-compiled-simulation-program`、`character-simulation-kernel`。
- 修改能力：`btsmtl-sm-node-authoring`、`btsmtl-runnable-timeline-node`、`btsmtl-runtime-diagnostics`、`character-gameplay-pipeline-closure`、`character-pipeline-runtime`、`character-input-pipeline`、`character-motion-simulation-boundary`、`character-network-sync-domain-contract`、`character-pipeline-blackboard`、`character-presentation-interpolation`、`gameplay-tick-system`、`server-authoritative-hybrid-sync-model`。
- 客户端：Compiler/Program/State/Kernel、Local Driver、Unity Solver adapter、Presentation Projection/Committer、Program Inspector 和 Diagnostics。
- 资产：Corin Definition 编译产物、Presentation Projection 和最终单机 Sandbox 配置。
- 删除：角色主线旧 interpreter/runtime clone、隐藏 gameplay state、CharacterMotionAuthority、MotionStage model correction、Character NetworkSend/Receive 双写 stage、一次性 migrator 和 fallback。

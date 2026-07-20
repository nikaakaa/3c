# Change: 增加 DeterministicRollback Network Model 与确定性 KCC Demo

## Why

ServerAuthoritativeHybrid 通过 owner prediction/reconciliation、远端 snapshot interpolation 和服务端权威状态提供实用的动作客户端闭环，但它不能回答另一个技术问题：同一 BTSMTL/Timeline/Action/Effect Semantic IR artifact 能否生成独立 Fixed Program，并在确定性世界求解下按 canonical input 完整 restore/replay，在多端产生相同 state hash。

这不是给 ServerAuthoritative加一个开关或 correction mode。它是第二个完整 Network Model：`DeterministicRollback`，拥有自己的 Model Source、Endpoint/Protocol、Rollback Pipeline与 Pass、canonical input bundle、world snapshot history、late-input rollback、restore/replay、state hash、desync recovery和 side-effect commit policy。

本 change依赖 `refactor-character-simulation-core` 的公共 operation-set/Session边界、`refactor-character-semantic-frontend-artifact` 的 validated `.csir`输入、`refactor-simulation-operation-runtime-modules` 的 portable topology/control runtime，以及 `refactor-gameplay-session-composition-boundary` 的唯一 SimulationSessionHost、Actor registration、Pipeline descriptor/compiler与 outer runtime handle。它不复用 Float32 Program/State/Kernel ABI，也不依赖 ServerAuthoritative实现。它新增 Fixed Numeric Target、Fixed Program Runtime、Deterministic Pass Execution Backend、Rollback Source/Pass/Pipeline和限定范围的 Deterministic KCC，只支持固定静态几何、fixed capsule actor、ground/slope/step/wall slide以及按稳定 ActorId 顺序批量求解的 Actor 硬接触，不支持 Unity Physics、Rigidbody、moving platform、动态破坏或任意第三方 gameplay system。

## Dependencies

- `refactor-character-simulation-core` MUST已完成并归档。
- `refactor-character-semantic-frontend-artifact` MUST已完成并归档；Rollback Target MUST只消费 validated Semantic IR artifact。
- `refactor-simulation-operation-runtime-modules` MUST已完成并通过 strict validation；Fixed Target MUST复用 portable `OperationExecutionTopology` 与 `OperationControlRuntime<TTarget>`，不得复制 Runnable、Composite、StateMachine或 stop propagation。
- `refactor-gameplay-session-composition-boundary` MUST已完成并通过 strict validation；Rollback MUST复用唯一 SimulationSessionHost、Actor registration、portable Pipeline descriptor/compiler、composition descriptor与 outer runtime handle，只自行提供 Fixed Program Runtime、Deterministic Backend、Rollback Source/Pass/Pipeline及 KCC。
- `refactor-character-state-transaction-runtime` MUST已完成并通过 strict validation；Fixed Target MUST复用typed state schema与`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partitions、numeric values、canonical codec和transaction specialization，MUST不复用或转换Float32 committed/mutable State。
- 本 change MAY与 `refactor-server-authoritative-hybrid-runtime` 和 DotRecast Solver/Host 开发并行，但不得修改 Float32 ABI、复制业务语义或改变公共 operation-set。
- 两个网络模型 MUST通过 SimulationSessionHost下的 `GameplayNetworkModelDefinition` Session Source同级安装，不互相调用 history/correction/replay，也不得交换 Float32/Fixed Program Runtime、Backend或 Snapshot。

## Current Implementation Gap

当前实现已经闭合 Fixed Program、Rollback Source/Pipeline、静态 Collision Artifact、静态 KCC、完整 World Snapshot、hash、Endpoint 与双 Peer Demo，但 `DeterministicKccWorldSolver` 仍逐 Actor 独立求解静态世界，没有把同一 batch 中的其他 Actor 作为接触体。`WorldFeature.ActorCollision` 已存在于公共能力枚举，Rollback KCC却没有声明或实现该能力。

因此现有“完成”状态只能证明静态世界移动同步，不能证明双 Actor 硬接触。两名角色相向移动、冲刺或转身穿过彼此时，各端虽然可能继续得到相同 hash，但业务结果仍然是错误的。必须补齐 Fixed batch Actor contact 后再重新编译、校验并恢复完成状态。

## What Changes

- 新增 FixedQ32.32 Numeric Target Compiler，从同一 Corin `.csir`生成独立 Fixed Program、State Layout、codec、Kernel backend、Fixed Program Runtime Definition与 ProgramHash；不复制业务 Graph、节点或 semantic evaluator。
- 新增 Deterministic Pass Execution Backend、Rollback Source Definition、phase-specific Pass、Rollback Pipeline Definition与 PipelineHash；复用公共 compiler、composition descriptor、Actor registration和 outer runtime handle。
- 新增 DeterministicRollbackModelDefinition、model Source preparation、capability validation和模型专属配置。
- 新增模型专属 EndpointDefinition/Protocol，由 session host/relay 组装每 Tick 全部 Actor 的 canonical input bundle，并传输 join/leave、bundle、hash、snapshot request/response 和 diagnostics。
- 新增版本化 DeterministicCollisionWorldArtifact，保存量化静态几何、material/surface、bounds、stable primitive order 和 content hash。
- 新增 DeterministicKccWorldSolver，使用核心 fixed/quantized math 实现限定 capsule movement、grounding、slope、step、wall slide、连续 Actor pair sweep、初始重叠去穿透和 stable query/pair order。
- 同一 SimulationStep 必须先为全部 Actor 生成静态世界 candidate，再按稳定 ActorId pair order 执行 `SolidBodyBlock` 接触：静止目标阻挡主动移动者且不被隐式推行，双方移动时裁剪相对闭合法向并保留切向移动；接触修正后重新约束静态世界，最终原子提交全部 BodyResult。
- Actor contact shape、固定迭代次数、容量和接触策略必须进入 KCC 配置身份与 hash；Solver 只有完整实现后才能声明 `WorldFeature.ActorCollision`。
- 为 Fixed ABI 实现与现有核心同形的 `SimulationWorldStateSet`、`WorldSimulationState` 与 `SimulationWorldSnapshot`，按 stable ActorId order 聚合 SimulationTick、Actor SimulationState、KCC world/actor state、RNG、command cursor 和模型必要状态；不新增第二种总世界状态聚合模型。
- 新增有界 canonical input/world snapshot history，迟到 input 进入时原子 restore 最早受影响 Tick，按 canonical bundle 重演到当前 Tick。
- Canonical Host 的墙钟只决定“何时到期”，共同显式输入前沿决定“是否允许生成”；每个 Tick 都必须保持完整 input delay lead。Peer 的 predicted completed frontier不得超过 canonical contiguous frontier加MaximumRollbackDepthTicks，慢端停顿时快端必须等待而不是耗尽 history。
- CanonicalConfirmation携带的最终bundle与confirmed frontier必须在同一次Source ingress原子交付；回放输出只禁止修改outer transaction开始前已经确认的历史，本事务内因回放结果一致而新确认的Tick仍必须完成replace/cancel/confirm提交。
- 新增 periodic state hash 交换与分层 desync diagnostics；无法自愈的 hash/history 失配通过模型正式 authoritative world snapshot 恢复，不回退 ServerAuthoritative correction。
- 新增表现提交策略：可预测表现按 EventId 替换/撤销，不可撤销副作用延迟到 confirmed horizon，replay 不重复触发 animation/camera/audio/VFX/UI。
- 交付一个两客户端 DeterministicRollback Demo，使用同一 Corin `.csir` 生成并锁定相同 Fixed Program、限定静态地图和相同 fixed Actor contact profile，覆盖移动、转身、闪避、Actor 阻挡/沿边滑动、Attack1/Attack2、连段、打断、Timeline Window 和 GameplayEffect。
- 通过 Bootstrap 进入隔离的 DeterministicRollback Peer Scene；Peer A/B以不同显式launch identity复用该Scene。Bootstrap不持有Rollback Session/Endpoint/history，切换Scene必须销毁旧模型资源而不是热切换活动Session。
- 扩展 diagnostics：input delay、predicted/confirmed tick、late input、rollback count/depth、replayed ticks、state hash、desync scope、snapshot recovery 和 presentation replacement。

## Non-Goals

- 不修改 ServerAuthoritativeHybrid，不复用它的 correction packet、prediction history 或 snapshot interpolation 作为 rollback 实现。
- 不让 Graph、StateMachine、Timeline、Action 或 Blackboard 保存 Network Model 开关。
- 不为 rollback 复制 deterministic node、Timeline runtime、Action runtime 或 GameplayEffect runtime。
- 不让 Rollback 加载 Float32 Program/State/Kernel，也不要求单机或 ServerAuthoritative 改用 Fixed ABI。
- 不支持 Unity Physics/Rigidbody、moving platform、动态几何、破坏、布料、质量/冲量/弹性、通用刚体推挤或任意 MonoBehaviour gameplay。
- 不把攻击击退、霸体、队伍穿透、ghost、RVO 或动态障碍塞进 Actor contact。击退与主动推行必须继续由正式 Gameplay/MotionRequest表达，再进入同一个 WorldSolver。
- 不实现完整竞技匹配、反作弊、host migration、断线续局或全局 PvPvE。
- 不宣称浮点 Unity Presentation具有确定性；只有 Fixed Program/State/KCC与 SnapshotParticipant Pipeline state进入 hash。
- 不在 unsupported Program/KCC/world capability 时回退 Unity Solver 或 ServerAuthoritative model。

## Current Spec Comparison

- `gameplay-simulation-session-composition` 与 `gameplay-network-model-boundary` 已提供 SimulationSessionHost、Actor registration、Model Source preparation、Pipeline descriptor/compiler与 outer runtime handle。本 change只新增 DeterministicRollbackModelDefinition/Endpoint、Fixed Program Runtime、Deterministic Backend、Rollback Source/Pass/Pipeline与 KCC，不修改 Common Host或引入中央 model switch。
- `gameplay-tick-system` 允许一个 outer LogicTick执行零到多个内部 SimulationStep；本 change由 Rollback Schedule Pass产生 forward/restore/replay plan并维护 confirmed horizon，PresentationFrame仍独立。
- `character-presentation-interpolation` 当前没有 replay 后 EventId 替换/撤销语义；本 change 必须补充，同时保持 Animancer 对动画混合的执行权威。
- `character-motion-simulation-boundary` 已明确确定性模拟必须属于独立完整 Network Model；本 change 实现该要求，不把 Deterministic KCC 塞进 ServerAuthoritative correction。
- `add-server-authoritative-predicted-actor-contact` 解决 Float32 ServerAuthoritative Prediction 中远端权威 Body 的 `ObservedKinematic` 约束；本 change解决 Fixed Rollback中全部 Active Actor 同步确定性求解。二者只共享 `SolidBodyBlock` 业务语义和公共 `WorldFeature.ActorCollision` 含义，不共享数值实现、history、request schema或 runtime。
- `project.md` 当前明确“当前不做全局 rollback”；本 change 完成后必须更新为“作品主线仍是 ServerAuthoritativeHybrid，DeterministicRollback 是隔离的对比 Demo 模型”。

## Impact

- 新能力：Fixed Numeric Target、`deterministic-rollback-network-model`、`deterministic-kcc-world-solver`、`deterministic-rollback-two-client-demo`。
- 修改能力：`gameplay-tick-system`、`character-presentation-interpolation`、`character-motion-simulation-boundary`。
- 客户端：Rollback Model Source/Pass/Pipeline/Endpoint、canonical input history、world snapshot/replay/hash、包含 Actor pair contact 的 KCC adapter与 Presentation output disposition policy。
- 宿主/协议：canonical input bundle assembler、join/leave、hash exchange、snapshot recovery 和 bounded diagnostics。
- 资产/配置：DeterministicCollisionWorldArtifact、Rollback model/endpoint definition、KCC profile 和双客户端 Demo。
- 删除：临时 deterministic node、ServerAuthoritative correction 复用、Unity Physics fallback、非确定 world query、双写 history/command 和一次性 migrator。

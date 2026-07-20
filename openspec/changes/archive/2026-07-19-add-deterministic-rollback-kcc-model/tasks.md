## 1. 确认核心依赖与模型隔离

- [x] 1.1 确认 `refactor-character-simulation-core`、`refactor-character-semantic-frontend-artifact` 已归档，`refactor-simulation-operation-runtime-modules` 与 `refactor-gameplay-session-composition-boundary` 已完成并通过 strict validation。
- [x] 1.2 核对 `.csir` ProgramId/SourceRevision/SemanticHash/operation-set、Numeric Target、Source/Pipeline/Solver/EventId合同。
- [x] 1.3 通过 validated Corin `.csir` 盘点 operation、numeric literal 与 World capability，列出 Fixed Target deterministic-compatible 结果。
- [x] 1.4 盘点 ServerAuthoritativeHybrid 命名空间/资产/配置，锁定 Rollback 不得引用的 packet/history/correction 类型。
- [x] 1.5 锁定 Rollback Model、Endpoint/Protocol、KCC、Collision Artifact、History、Committer 和 Demo 的 assembly/asset ownership。
- [x] 1.6 确认 ServerAuthoritative与 Rollback只共享公共 Program/Pipeline/composition合同、标准业务语义和 Actor registration，不共享 mutable model runtime。
- [x] 1.7 定义 FixedQ32.32 NumericProfile、Target ABI version、rounding/overflow 和 canonical codec。
- [x] 1.8 建立只消费 `ValidatedSemanticIrArtifact` 的 Fixed Target Compiler，禁止读取 Definition、Graph、Timeline 或 Float32 Program。
- [x] 1.9 建立Fixed Program Runtime、Fixed typed partitions/numeric values/canonical codec/State Transaction/Snapshot/Kernel、`FixedOperationEvaluator`、Fixed numeric/domain leaf modules与Deterministic Pass Backend source set；只复用typed semantic与事务生命周期形状，不修改、转换或复用Float32 ABI/State/transaction/leaf implementation。
- [x] 1.10 复用 portable `OperationExecutionTopology`、`OperationControlRuntime<TTarget>` 与 control cursor；为统一 operation-set 的全部 numeric/domain leaf 建立 Fixed backend，缺失 leaf 直接编译失败，不复制 Runnable、Composite、StateMachine 或 stop propagation。
- [x] 1.11 核对 Fixed Program 保留 `.csir` ProgramId、SourceRevision、SemanticHash 和 producer/source-map identity，同时生成独立 Fixed ProgramHash/LayoutHash。
- [x] 1.12 让 Deterministic Backend消费公共 `SimulationSessionCompositionDescriptor`、compiled Pipeline plan并返回 outer runtime handle，复用唯一 SimulationSessionHost/Actor registration生命周期。
- [x] 1.13 删除 Float32 Program/Backend转换为 Fixed、运行时 `.csir` lowering、复制的 Pipeline compiler/control evaluator、model-specific operation runtime和 rollback专用节点路径；Fixed只保留正式 Target evaluator coordinator。

## 2. 建立 DeterministicRollback Model 与配置

- [x] 2.1 建立 DeterministicRollbackModelDefinition 与稳定 ModelId。
- [x] 2.2 建立 Rollback 专属配置：input delay、history length、hash cadence、max rollback depth、confirmed horizon 和 snapshot authority。
- [x] 2.3 建立 SemanticHash/Fixed ProgramHash/Fixed ABI/World/KCC/Protocol capability 校验，只在完整组合可创建时显示 ModelDefinition。
- [x] 2.4 复用公共 Actor registration与 preparation launch roster，建立 Rollback Session Source lifecycle和窄 Source ports。
- [x] 2.5 建立稳定 `thirdperson.simulation.pipeline.deterministic-rollback` PipelineId、Revision、schema及 input ingress、schedule、history、hash/output disposition Pass identity和顺序。
- [x] 2.6 建立运行中锁定 ModelDefinition、SemanticHash、Fixed ProgramHash、world artifact、KCC 和 endpoint 组合。
- [x] 2.7 删除通用 model enum/switch、ServerAuthoritative correction 复用和不完整 Rollback option。

## 3. 建立 Rollback Endpoint 与 Canonical Input Protocol

- [x] 3.1 建立模型专属 RollbackEndpointDefinition 和 connection lifecycle。
- [x] 3.2 建立 handshake，校验 ModelId、SemanticHash、Fixed ProgramHash、TickRate、CollisionWorldHash、KccId 和 protocol version。
- [x] 3.3 建立 join/leave/roster 与 stable PlayerId/ActorId 协议。
- [x] 3.4 建立每 Actor 上行 CharacterSimulationInput 与 sequence/source tick 协议。
- [x] 3.5 建立 host/relay canonical input bundle assembler，按 Tick 和 stable ActorId order 生成完整 bundle。
- [x] 3.6 建立缺失连续输入的显式 neutral/last-continuous 规则。
- [x] 3.7 建立离散 request 去重与 canonical provenance，不由 last-input 重复触发。
- [x] 3.8 建立 state hash、desync detail、world snapshot request/response 和 diagnostics 协议。
- [x] 3.9 建立有界 bundle/input/hash/snapshot 队列与明确溢出规则。
- [x] 3.10 删除重复 transport runtime、ServerAuthoritative packet envelope 复用和 endpoint fallback。
- [x] 3.11 让Host在每个canonical Tick生成前检查共同显式连续输入仍保持完整input delay lead，禁止墙钟越过输入前沿。
- [x] 3.12 让Endpoint Source在发送后Pump再次排空canonical queue，把Confirmation最终bundle与confirmed frontier原子交付给同一个IngressBatch。
- [x] 3.13 让Unity Fixed输入适配器在同一次RenderFrame锁存输入与CameraBasis，相机相对移动只读该快照，且仅在Program显式声明时把CameraBasis写入网络payload。

## 4. 建立 Deterministic Collision World Artifact

- [x] 4.1 定义 artifact version、MapId、quantization、bounds、surface/material catalog、primitive order 和 content hash。
- [x] 4.2 定义限定静态几何的 canonical primitive/triangle/plane 数据布局。
- [x] 4.3 建立从固定 Demo 场景生成量化 artifact 的正式 Editor/build 入口。
- [x] 4.4 建立无 Unity 依赖的 artifact loader 与版本/hash/bounds/order 校验。
- [x] 4.5 建立 KCC world query acceleration data 的稳定 build/order。
- [x] 4.6 删除 runtime Unity Physics baking、scene scanning、浮点 mesh fallback 和临时 parser。

## 5. 实现 Deterministic KCC World Solver

- [x] 5.1 建立 DeterministicKccWorldSolver 的 SolverId、capabilities 和固定数值参数。
- [x] 5.2 建立 capsule body、ground contact、slope/step state、velocity 和 query cache 的显式 solver state。
- [x] 5.3 实现稳定 broadphase candidate order 与固定迭代上限。
- [x] 5.4 实现 deterministic capsule cast/overlap 与 contact sorting。
- [x] 5.5 实现 ground probing、snap 与 grounded/airborne transition。
- [x] 5.6 实现 slope limit、projected movement 和 steep-slope handling。
- [x] 5.7 实现限定 step up/step down 顺序与最大高度。
- [x] 5.8 实现 wall slide、penetration resolution 和固定 contact iteration。
- [x] 5.9 实现 yaw/forward motion 与 root-motion request 的确定性执行顺序。
- [x] 5.10 明确 actor collision 能力：若实现，按 stable ActorId pair order 执行；若未实现，capability MUST显式拒绝该需求。
- [x] 5.11 将 KCC actor/world state 纳入 canonical capture/restore/hash。
- [x] 5.12 对 overflow、query capacity、iteration non-convergence、unsupported dynamic body 明确失败。
- [x] 5.13 删除 Unity Physics/CharacterController fallback、float gameplay state 和非稳定集合迭代。
- [x] 5.14 定义 Fixed ActorContactShape，包含radius、height、skin和canonical codec，并禁止从Unity Collider运行时读取形状。
- [x] 5.15 将ActorContactShape、pair capacity、iteration count和`SolidBodyBlock`策略版本纳入KccId/WorldConfigurationHash。
- [x] 5.16 让KCC在同一ResolveBatch中先生成全部Actor的静态世界candidate，禁止逐Actor提前提交BodyResult。
- [x] 5.17 按stable ActorId建立有界pair table，并对重复ActorId、无序输入和容量溢出明确失败。
- [x] 5.18 实现fixed垂直区间重叠过滤，只让高度区间相交的capsule pair进入平面接触。
- [x] 5.19 使用双方BeforeBody到StaticCandidateBody的相对位移实现连续平面圆盘sweep，覆盖高速闪避和Timeline motion。
- [x] 5.20 实现初始重叠去穿透，并以ActorId和固定轴规则解决零距离法向tie-break。
- [x] 5.21 实现移动Actor撞静止Actor时只裁剪移动侧闭合法向、静止侧不产生隐式推行位移。
- [x] 5.22 实现双方移动时裁剪相对闭合法向并保留切向位移，不引入质量、冲量、弹性或动量交换。
- [x] 5.23 在每轮pair修正后重新执行静态世界约束，防止Actor接触将Body推入墙体、台阶或陡坡。
- [x] 5.24 使用固定pair迭代次数和稳定遍历顺序，迭代后仍穿透时使整个World Step失败。
- [x] 5.25 在提交前同时验证全部Actor的静态penetration和有效pair最小间距。
- [x] 5.26 原子生成全部Actor BodyResult与next world state，并把pair、TOI、法向裁剪、去穿透和失败原因写入结构化诊断。
- [x] 5.27 完整实现后声明`WorldFeature.ActorCollision`，并让缺失该能力的Rollback Composition在Preparing阶段失败。
- [x] 5.28 确认Fixed KCC Actor contact不引用DotRecast Float32 ActorContactSolver、Unity Physics、Network role、Action或Animation producer。

## 6. 建立 Fixed World State/Snapshot 与 Rollback History

- [x] 6.1 为 Fixed ABI 建立与核心同形的 `SimulationWorldStateSet`、`WorldSimulationState` 与 `SimulationWorldSnapshot`，包含 Tick、stable Actor table、Actor SimulationState、KCC state、RNG 和 command cursor，不新增平行总状态 aggregate。
- [x] 6.2 建立canonical world snapshot serialization与SemanticHash、Fixed ABI、Fixed ProgramHash、LayoutHash、Fixed State codec identity、world/KCC hash header，只保存committed canonical bytes。
- [x] 6.3 建立有界 canonical input history 与 predicted/canonical provenance。
- [x] 6.4 建立有界 world snapshot ring 与 history floor/ceiling。
- [x] 6.5 建立按Tick原子capture所有Actor committed Fixed State canonical bytes与KCC state的流程，禁止捕获active Fixed transaction或mutable partition。
- [x] 6.6 建立按 Tick 原子 restore 完整 Fixed `SimulationWorldSnapshot` 到唯一 `SimulationWorldStateSet` 的流程。
- [x] 6.7 建立 world/actor/module/KCC 分层 state hash。
- [x] 6.8 对 SemanticHash、Fixed ProgramHash、world hash、KCC id、layout 或 Actor roster 不匹配拒绝 restore。
- [x] 6.9 删除部分 Actor/Transform-only snapshot、双 history 和无界历史结构。

## 7. 实现 Forward、Rollback 与 Replay Pipeline

- [x] 7.1 让 Rollback Schedule Pass实现 predicted input的 forward SimulationStep计划。
- [x] 7.2 让 Rollback Ingress/Schedule Pass实现 canonical bundle到达后的 predicted/canonical input对比。
- [x] 7.3 计算最早受影响 Tick 与 rollback depth。
- [x] 7.4 由 Schedule Pass产生恢复最早受影响 Tick前完整 world snapshot的 restore directive。
- [x] 7.5 由 Schedule Pass按 canonical bundle、Tick和 stable ActorId order产生 replay/current steps，并由 Deterministic Backend在同一 outer transaction执行。
- [x] 7.6 由 Output Disposition Pass将 replay output与旧 EventId output对比并产生 keep/replace/cancel/confirm结果。
- [x] 7.7 推进 confirmed horizon 并释放旧 input/snapshot/output history。
- [x] 7.8 对 late input 早于 history floor 触发正式 world snapshot recovery。
- [x] 7.9 对 rollback depth/cost 超过模型配置明确进入 recovery，不静默丢弃 canonical input。
- [x] 7.10 删除 ServerAuthoritative correction/reconciliation复用、私有 replay runner、第二 Logic target和 Kernel内 model switch。
- [x] 7.11 使用MaximumRollbackDepthTicks限制Peer predicted completed frontier；到达上限时返回NoStep，收到差异时仍允许只回放已有Tick。
- [x] 7.12 修复历史Pipeline projection恢复后的confirmed-history释放：confirmed frontier不回退且未继续增长时仍清理已确认input/applied-input hash，禁止旧记录累积进后续snapshot。

## 8. 实现 Hash 交换与 Desync Recovery

- [x] 8.1 按固定 cadence 发布 confirmed world state hash。
- [x] 8.2 对齐同 Tick 各端 hash 并区分 Program/world/roster/actor/module/KCC subhash。
- [x] 8.3 在 history 可覆盖时使用 canonical bundles 重演并再次比较 hash。
- [x] 8.4 建立 snapshot authority 选择与完整 world snapshot request/response。
- [x] 8.5 验证 snapshot header/hash 后原子替换 world state 并重建 history floor。
- [x] 8.6 建立 desync incident 的有界 diagnostics segment，不将 trace 写入 state hash。
- [x] 8.7 删除以 Transform teleport、ServerAuthoritative correction packet 或忽略 hash 差异作为 recovery 的路径。

## 9. 实现 Rollback Presentation Commit Policy

- [x] 9.1 由 Rollback Output Disposition Pass将 Fixed `SimulationActorTickResult` commands分类为 predictable/reversible与 confirmed-only。
- [x] 9.2 建立 EventId -> committed presentation record 的有界 registry。
- [x] 9.3 实现 forward predicted output 的立即提交和 provenance。
- [x] 9.4 实现 replay 后 keep/replace/cancel 对 animation selection、visual pose、camera/VFX state 的提交。
- [x] 9.5 实现 confirmed-only audio/UI/external one-shot 在 confirmed horizon 后提交。
- [x] 9.6 将 replay Timeline visual sample 与 AnimationPlaybackLifecycle/Animancer 现有状态对齐。
- [x] 9.7 确保 replay 不重复触发 Cue、Camera impulse、Audio、VFX 或 UI。
- [x] 9.8 删除 Rollback 专用动画状态机、CrossFade runtime 或 Presentation 反向写 SimulationState 路径。
- [x] 9.9 记录outer transaction开始前的confirmed floor，让Output Committer只保护旧已确认历史并允许本事务内刚确认的replay输出完成替换。
- [x] 9.10 为Rollback表现输出建立`BeginCommit -> publish/replace/cancel -> CompleteCommit|AbortCommit`事务边界，同一outer transaction只向表现层提交每个状态槽的最终结果。
- [x] 9.11 让Unity Presentation Adapter维护已确认基线与未确认EventId历史，在confirmed horizon后折叠旧记录并释放已结束playback generation，删除以扩大active record容量掩盖泄漏的路径。
- [x] 9.12 为`ICharacterPresentationRuntime`与Animation Playback Runtime接通原子Replace，同一playback sample替换保留当前视觉时间，selection替换继续由Animancer从当前视觉graph接管。
- [x] 9.13 将每个outer transaction产生的全部Fixed BodyResult纳入同一个Rollback Output Commit，不再在动画命令提交结束后逐Replay Step直接修改visual history。
- [x] 9.14 为每个Actor按Tick暂存并覆盖BodySample，使用正式HistoryLengthTicks约束单次事务容量，只把Replay后的最终Body分支提交给Presentation。
- [x] 9.15 为Committed Body Stream实现原子Body transaction：整批校验连续性、只触发一次branch replacement、保留一次当前visual pose并在后续PresentationFrame连续收敛。
- [x] 9.16 本地与远端Rollback Actor继续消费predicted current timeline；不切换confirmed delayed stream，不增加业务输入延迟，不让visual recovery写回Fixed State、KCC、Snapshot或Hash。
- [x] 9.17 让Committed Body表现时钟在Replay分支替换期间保持单调推进，只在显式stream reset或HardRecovery时重建游标。
- [x] 9.18 将Body visual recovery改为每个PresentationFrame按render delta衰减，使连续canonical纠偏不会反复重置恢复进度并钉住visual root。

## 10. 建立双客户端 Rollback Demo

- [x] 10.1 建立两个客户端与一个 canonical input host/relay 的正式 launch 组合。
- [x] 10.2 两端加载同一 Corin SemanticHash、Fixed ProgramHash、CollisionWorldHash、KccId 和 Rollback ModelDefinition。
- [x] 10.3 按 stable ActorId order 创建两个 Corin SimulationState/KCC body。
- [x] 10.4 接通移动、转身、闪避、Run 和 Timeline motion curve。
- [x] 10.5 接通 Attack1/Attack2、连段、打断和 Timeline TreeClip Window。
- [x] 10.6 接通 GameplayEffect、Attribute、Cue 与 Presentation commands。
- [x] 10.7 将 Demo 能力限制为静态几何/capsule KCC/双 Actor，不声称 Unity Physics 确定性。
- [x] 10.8 将 Demo UI 显示 ModelId、predicted/confirmed tick、input delay、rollback depth、hash 和 recovery。
- [x] 10.9 在Network Test Bootstrap中增加显式DeterministicRollback Peer Scene跳转，禁止Bootstrap创建Rollback运行组件。
- [x] 10.10 建立隔离Rollback Peer Scene并显式引用Rollback Composition、Actor/出生点、量化world binding、Endpoint与diagnostics。
- [x] 10.11 让Peer A/B通过不同launch profile复用同一Peer Scene，并在Preparing前锁定PeerId、ActorId、Fixed Program、CollisionWorldArtifact与KccId。
- [x] 10.12 场景切换时完整释放旧Session、Actor registration、Endpoint、history与KCC world，禁止通过DontDestroyOnLoad跨Scene存活。
- [x] 10.13 禁止Bootstrap或Peer Scene提供Active Session内Float32/Fixed、Local/Rollback或Solver热切换与fallback。
- [x] 10.14 为两个Corin Actor配置显式Fixed contact shape，并由正式Rollback Composition持有唯一contact profile引用。
- [x] 10.15 让Rollback Demo Composition显式要求`WorldFeature.ActorCollision`并锁定更新后的KccId/WorldConfigurationHash。
- [x] 10.16 让移动、转身、闪避和Timeline motion全部通过同一个KCC batch contact路径，不新增动作专属碰撞节点。
- [x] 10.17 将Actor pair、sweep TOI、penetration、法向裁剪、pair iteration和最终间距接入现有Rollback diagnostics。
- [x] 10.18 让Rollback Build从当前Character Definition重新生成Semantic IR、Presentation Projection与Fixed Program，禁止静默复用旧图产物。
- [x] 10.19 将Unity Build与portable FixedProgramBuildTool统一到同一个Fixed artifact compiler，并在Player Build前校验semantic producer identity。

## 11. Diagnostics、清理、文档与校验

- [x] 11.1 暴露 canonical/predicted input provenance、late input、rollback count/depth 和 replayed ticks。
- [x] 11.2 暴露 world/actor/module/KCC hash 与 desync scope。
- [x] 11.3 暴露 KCC query count/contact count/iteration/capacity 和 solver duration。
- [x] 11.4 暴露 presentation keep/replace/cancel/confirmed-only queue 数据。
- [x] 11.5 将 Rollback trace 与 Program source、ActorId、SimulationTick、replay pass 和 EventId 对齐。
- [x] 11.6 删除 deterministic node副本、ServerAuthoritative correction复用、Unity Physics fallback、双 history/command、公共 Host/Actor registration/Pipeline compiler副本和一次性 migrator。
- [x] 11.7 更新 `openspec/project.md`，把Rollback KCC当前静态世界口径迁移为明确的Fixed ActorCollision/SolidBodyBlock能力。
- [x] 11.8 更新本 change 影响的 current specs，并确认不与ServerAuthoritative ObservedKinematic接触语义形成同名不同义或分裂实现。
- [x] 11.9 以 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 重新编译 portable Core、Fixed Target、Rollback/KCC、Endpoint 与 Unity client 相关工程。
- [x] 11.10 编译结束后立即执行 `dotnet build-server shutdown`。
- [x] 11.11 在全部Actor contact实施任务完成后运行 `openspec validate add-deterministic-rollback-kcc-model --strict --no-interactive`。
- [x] 11.12 补充移动同步方案调研，区分ServerAuthoritative prediction、deterministic rollback、lockstep、状态同步和取消硬碰撞的业务取舍。

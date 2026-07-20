## Context

核心 change已经把角色 Gameplay authoring收口为 portable Semantic IR artifact，并建立 Numeric Target、Pipeline、Session、ICharacterWorldSolver与 EventId output边界；operation runtime模块化基座提供 numeric-neutral `OperationExecutionTopology`、`OperationControlRuntime<TTarget>`和受约束 Target port；Session composition基座提供唯一 SimulationSessionHost、Actor registration、Pipeline descriptor/compiler、composition descriptor与 outer runtime handle。这些是 rollback的必要条件，但当前可运行的 Float32 Program Runtime、Pass Backend、Program/State/Kernel与 Float32 leaf modules不是确定性 Rollback的执行 ABI。

本 change 新增的 Network Model 只负责 canonical input、history、restore/replay、hash 和 commit。Deterministic KCC 负责确定性静态世界运动与同 batch Actor 身体接触。两者都使用核心最终合同，不得把模型分支写进 Program operation。

## Architecture

```text
Client A / Client B
  local CharacterSimulationInput
        |
        v
DeterministicRollback Endpoint/Protocol
  Host/Relay assembles CanonicalInputBundle(Tick, all actors)
        |
        v
DeterministicRollback Source + Pipeline
  input ingress + snapshot history passes
  forward / restore / replay schedule + confirmed horizon
        |
        v
same validated Corin .csir
        |
        v
FixedQ32.32 Target Compiler
  Fixed Program + Fixed State ABI + Fixed Kernel
  Fixed Program Runtime + Deterministic Pass Backend
        |
        v
DeterministicKccWorldSolver
  DeterministicCollisionWorldArtifact
  static-world candidates + stable Actor pair contacts
        |
        v
Fixed SimulationWorldStateSet/WorldSimulationState/SimulationWorldSnapshot
  + StateHash + EventId commands
        |
        v
Rollback Output Disposition Pass
  replace/cancel predictable output
  delay irreversible output until confirmed
```

## Decisions

### 1. Rollback 是独立 Network Model

DeterministicRollbackModelDefinition与 ServerAuthoritativeHybridModelDefinition作为 `GameplayNetworkModelDefinition` Session Source同级安装。SimulationSessionHost一次只激活一份显式 composition和 runtime handle。Rollback不读取 ServerAuthoritative packet/history/correction profile，ServerAuthoritative也不读取 rollback bundle/snapshot/hash。

### 2. 业务语义只有一份，但 Numeric Program 按 Target 独立

Corin Authoring 只生成一份 canonical `.csir`。Float32 Target 与 FixedQ32.32 Target 分别生成不同 Program/State/Kernel ABI、ProgramHash 和 Snapshot 格式；二者共享 ProgramId、SourceRevision、SemanticHash、operation-set version、portable topology contract 与 portable control runtime，但不能互换 Program、State 或 Snapshot。Fixed Target 建立自己的 `FixedOperationEvaluator` 作为一次 Actor/Tick 的事务协调器，只实现 Fixed control-state adapter、Condition、Value、Timeline numeric sampling、GameplayEffect magnitude、Motion blending 等 numeric/domain leaf modules；Runnable、Composite、StateMachine 和 stop propagation 必须调用共享 portable control runtime。Rollback Model 在 Session 创建前检查 Fixed Program 不包含 unsupported operation/world requirement，KCC/world artifact 满足所有能力。不支持的 semantic operation 由 Fixed Target 编译时直接失败，不生成 deterministic node 副本、复制 control evaluator、model-specific operation runtime 或跳过 operation。

Fixed Target复用的只是typed semantic、stable address和状态事务生命周期：每个Actor/Step从Fixed committed State开始一个Fixed transaction，Evaluate与Finalize共享它，WorldSolver不访问它，成功后Commit、失败后Abort。Fixed Target必须拥有自己的partition/value/page、GameplayEffect aggregate、savepoint、codec identity与Snapshot bytes；Rollback History只保存Fixed committed canonical bytes和Fixed Program/Layout/codec identity，不保存active transaction，也不转换Float32 Snapshot。

### 3. Canonical Input Bundle 是唯一网络 Gameplay 输入

Host/relay 为每 SimulationTick 按 stable ActorId order 组装完整 input bundle。缺失 input 按模型显式 neutral/last-continuous 规则表达，并保留 predicted/canonical provenance。离散 request 不得由未确认的 last-input 隐式重复。

Endpoint 使用模型专属 UDP 数据报，不建立 TCP/KCP 全局有序字节流。每个 Peer 每 Tick 将当前 input 与最近三个 Tick 按连续 Tick 排序编码为一个不可靠 `ActorInputBatch`，而不是为冗余 Tick 分开发包；state hash 使用周期数据报。handshake、roster、canonical bundle、canonical confirmation、snapshot request/response 使用独立 message sequence、确认与重传。可靠消息之间不互相等待，Snapshot response 超过 MTU 时按 message sequence 分片、独立重组和确认，因此旧控制消息或大 Snapshot 丢包不得阻塞新 input/canonical bundle。所有发送、接收、可靠待确认、重组和应用层队列都必须有界；同线程发送暂存区达到容量时先正式写入 UDP Socket，Socket失败或无法释放的可靠窗口才终止 Endpoint，不切换 TCP、ServerAuthoritative packet 或第二 transport。

Host 的 canonical clock 在本 Demo 使用 4 Tick input delay；本地 Peer仍立即预测自己的输入，因此该 delay不增加本地移动/动作响应，只增加 canonical与 confirmed-only输出的等待时间。启动时 Host在双 Actor的 Tick 1显式输入齐备后立即发布 bootstrap bundle，使 Peer不因等待第一份 canonical而停在 Tick 1；随后 Host等待显式输入前沿达到 `NextCanonicalTick + 4`，再锁定 canonical epoch并始终保持完整 4 Tick lead。Host 为迟到 Tick保存每 Tick输入推导状态，只从受影响 Tick前一状态重建到当前，禁止每次从整个 history floor重新演算。一个 Host Pump内同 Tick多次修订只发送最终版本；重建后的 GameplayHash未变化时只更新最终 provenance/sequence状态，不广播无业务变化的普通 revision，最终版本由可靠 confirmation区间携带。

墙钟到期不是生成canonical bundle的充分条件。Host每次进入catch-up循环都必须重新检查全部Actor共同的显式连续前沿仍覆盖`NextCanonicalTick + InputDelayTicks`；任一Peer变慢时Host暂停，而不是用missing-input连续追赶墙钟。Peer可以继续预测，但其completed frontier最多领先本地canonical contiguous frontier `MaximumRollbackDepthTicks`；到达上限后Source继续重发同一待执行Tick输入，Schedule返回NoStep。若上限处收到canonical差异，Schedule可以restore/replay已有Tick，但不得顺便再生成一个新的forward Tick。

普通 canonical bundle到达后可以立即触发 rollback，但不得由 Peer自行推断 confirmed horizon。Host只有在全部 Actor显式输入连续、超过 confirmation delay且该 Tick不再允许修订后，才发送带完整最终 bundle区间的可靠`CanonicalConfirmation(previousConfirmedTick, confirmedTick, finalBundles)`。Confirmation区间拥有独立连续前沿，Peer可以缓冲乱序 confirmation而不阻塞普通 canonical input；只有收到最终 bundle区间后才释放 input/output history。确认后晚到的旧普通 bundle属于过期副本并丢弃，未确认但早于 history floor的 bundle仍进入正式 snapshot recovery。

Endpoint在一次Source `Read`中可以执行两次网络Pump，但第二次Pump后必须再次排空canonical queue，再把final bundles与新的confirmed frontier一起构造IngressBatch。Pipeline记录outer transaction开始前的confirmed floor；History Pass可在replay完成后推进当前confirmed frontier，Output Committer只使用事务开始floor判断“是否修改旧已确认输出”，随后按推进后的frontier提交confirmed-only输出并释放历史。restore只回退可重演的Pipeline projection，不回退单调递增的confirmed frontier；历史projection恢复后，即使本次confirmed frontier没有继续增长，History Pass也必须按当前frontier再次释放已确认的input与applied-input hash，禁止旧snapshot把已确认历史重新带回后续snapshot。

### 4. Rollback 恢复整个 Fixed World Snapshot

Fixed Target MUST实现与核心相同的 `SimulationWorldStateSet -> WorldSimulationState -> SimulationWorldSnapshot` 所有权形状。Snapshot 覆盖 SimulationTick、所有 Actor SimulationState、Deterministic KCC actor/world state、RNG、event/command cursor 和模型必要状态。不允许再引入一个平行 `SimulationWorldState` aggregate，也不允许只回滚位置、单个 Actor 或单个 Timeline。

迟到 canonical input改变 Tick T时，Rollback Schedule Pass生成恢复 T前 snapshot及后续 replay/current steps，Deterministic Backend在一个 outer transaction内按 Tick/Actor stable order重演到当前 predicted tick。

### 5. Deterministic KCC 覆盖静态世界与限定 Actor 硬接触

Collision artifact 使用量化几何、stable primitive order 和 content hash。KCC 使用固定数值与固定迭代次数处理 capsule cast/overlap、grounding、slope limit、step up/down、wall slide 和 penetration resolution。

同一 `ResolveBatch` 必须先为全部 Active Actor生成静态世界 candidate，不允许完成一个 Actor并写入世界后再开始下一个 Actor。随后使用固定 capsule contact shape、stable ActorId pair order、固定 pair capacity和固定 iteration count处理 Actor接触：

- 垂直区间不重叠的 pair不参与平面接触。
- 使用双方 `BeforeBody -> StaticCandidateBody` 的相对位移做连续平面圆盘 sweep，避免闪避、Timeline motion或高速度移动直接穿透。
- 初始重叠使用固定法向和 ActorId tie-break去穿透，禁止依赖容器顺序、浮点 epsilon或随机方向。
- 一方静止、一方主动闭合时只裁剪主动移动者的闭合法向，不把静止目标隐式推走。
- 双方移动时裁剪相对闭合法向并保留各自切向移动，不计算质量、冲量、弹性或动量交换。
- 每轮 pair修正后重新执行静态世界约束，避免角色接触把任一 Actor推入墙体；最后同时验证静态 penetration和 Actor最小间距。
- 全部 Actor BodyResult与next world state一次原子提交；容量溢出、固定迭代后仍穿透或静态/Actor约束无法同时满足时整个 Step失败。

该策略命名为 `SolidBodyBlock`。它表达当前 2v2vE Demo需要的“角色实体阻挡”，不是通用物理。攻击击退、霸体、队伍穿透、ghost、RVO、moving platform和动态刚体不属于它；未来若需要击退，Gameplay必须生成显式 MotionRequest，再由同一 KCC按相同接触约束求解。

Contact radius、height、skin、pair capacity、iteration count和策略版本进入 KccId/WorldConfigurationHash。Solver完整实现后声明 `WorldFeature.ActorCollision`。接触求解不保留跨 Tick warm-start cache；如果后续引入任何会影响未来结果的pair cache，必须进入 world snapshot和state hash。

本 Demo仍不支持 moving platform、dynamic rigidbody、破坏、动态 mesh 或 Unity Physics query。它也不得引用并行 DotRecast分支的 Float32 `ActorContactSolver`；二者只共享 `SolidBodyBlock` 业务规则，分别在各自 Numeric Target内实现。

### 6. 移动同步模型不是只有一种

Actor碰撞如何计算，与移动结果如何在网络上传播，是两个不同决策。只要业务要求角色互相硬阻挡，所有参与端最终必须服从一份接触真相，但可以用不同网络模型建立这份真相：

| 方案 | 运行方式 | 碰撞真相 | 业务取舍 |
|---|---|---|---|
| 服务端权威 + owner prediction/reconciliation | 客户端立即预测自己，服务端重演并确认/纠偏，远端按权威快照插值 | Authority完整 roster求解；客户端用权威远端 Body作短时观察约束 | 不要求所有平台严格确定，反作弊和后端控制更清晰；远端突然改向时仍会产生纠偏 |
| 确定性 input predict/rollback | 每个 Peer运行相同完整世界，只同步输入；canonical input变化后恢复并重演 | 每个 Peer使用同一 Fixed batch solver得到相同结果 | 本地响应快、共享交互自然；所有会影响结果的逻辑、碰撞和状态都必须确定、可快照且可重演 |
| 延迟 lockstep | 等待该 Tick全部输入后再推进相同确定模拟 | 每端同算且不预测 | 一致性简单、无需回滚；输入延迟直接进入动作手感，不适合作为本 Demo主体验 |
| 权威状态同步 + remote interpolation | 只有 Authority求解，客户端不预测或只做视觉外推 | Authority唯一 | 实现和算力成本最低；owner响应和近身接触手感最差，适合非动作核心对象 |
| 取消/软化 Actor硬碰撞 | 网络仍可使用上述任意模型 | 不需要共享硬接触结果 | 大幅减少穿透纠偏，但改变“玩家能实体阻挡”的玩法，不是技术等价替代 |

本 change选择第二项，因为目标就是展示 Fixed deterministic rollback，而不是因为移动同步只能这么做。项目现有 ServerAuthoritative主线继续选择第一项；两个模型共享 authoring与业务语义，但不共享执行状态或接触实现。严禁各 Peer独立使用 Unity Physics后只同步Transform，因为不同接触结果无法通过rollback收敛，表现插值也只能遮住结果差异，不能修复 gameplay truth。

更完整的调研、官方资料和适用边界见 `movement-synchronization-research.md`。

### 7. Hash 检测与 Snapshot Recovery 是两层机制

每端按固定周期上报/peer exchange world state hash。Hash不同时先比较 SemanticHash、Fixed ProgramHash、Rollback PipelineHash、world artifact hash、Tick、Actor subhash、KCC subhash和 module subhash。若 history可恢复，Rollback Schedule Pass生成重演计划；若仍失配或 history不足，从模型指定的 snapshot authority请求完整 world snapshot。

Snapshot authority 只用于严重失配恢复，不把本模型变成每 Tick ServerAuthoritative correction。

### 8. 表现按 EventId 管理 Replay

Rollback Output Disposition Pass将 output分为：

```text
Predictable/reversible: animation selection, visual pose, reversible camera/VFX state
Confirmed-only: irreversible audio, one-shot external event, non-reversible UI/result
```

Predictable output可以立即提交，replay后按 EventId保留、替换或撤销。Confirmed-only output延迟到 confirmed horizon。Animancer仍是动画混合/淡入淡出执行权威，Rollback Pipeline不实现第二套动画状态机。

Rollback Output Committer必须把同一个outer transaction中的全部forward/replay输出作为一次表现提交事务。Unity Presentation Adapter在事务内维护每个动画表现状态槽的已确认基线与未确认EventId历史，先完成全部publish/replace/cancel，再只把最终状态交给`ICharacterPresentationRuntime`。同一事务产生的Fixed BodyResult也必须先按ActorId/Tick暂存并覆盖，整批Replay结束后只提交最终连续Body分支；不得在动画事务结束后逐Replay Step直接清空或重建visual history。Body事务容量由Rollback正式HistoryLengthTicks约束，不复用动画active record容量。confirmed horizon推进后，Adapter折叠旧历史但保留仍生效的基线；已经确认结束的playback generation必须释放sample与terminal记录。记录容量用于约束真实未确认历史，不得通过扩大容量掩盖generation泄漏。

`ICharacterPresentationRuntime.Replace(current, replacement)`是原子动画表现接管语义，`CaptureBodyTransaction(intervals)`是原子Body分支接管语义。Animation selection替换不得清空Animancer Layer；同一playback的sample替换必须保留当前视觉采样时间，并在后续PresentationFrame向纠正后的sample推进。Body分支替换只保留一次当前visual pose并建立一次visual recovery，后续渲染帧从该姿态连续收敛到Replay最终Body。Rollback replay只改变最终desired command与最终Body分支，不可把中间历史动画或Body状态逐条显示。Local/remote Rollback Actor都继续消费predicted current timeline；confirmed horizon只控制不可逆副作用和历史释放，不把远端角色切换为延迟确认流。

Committed Body分支被高频替换时，Presentation Tick游标必须保持单调推进，只有显式stream reset或HardRecovery才允许重建时钟。每次分支替换从当前可见姿态重新计算visual offset，但offset必须在同一个PresentationFrame按render delta继续指数衰减，不能用“重新开始固定时长恢复”的计时器把进度反复归零。这样移动和转身的连续canonical修订不会把visual root钉在旧姿态后再跳变。

Unity Fixed输入适配器在一次`CaptureRenderFrame`内同时锁存Input System值和CameraBasisSnapshot。相机相对移动必须只使用这份锁存basis转换，不能在后续SimulationTick读取live camera。锁存camera仅用于本地输入坐标转换；只有Fixed Program显式声明CameraBasis输入时才允许把basis字段写入网络输入payload，不能因为存在camera-relative Vector2就隐式扩大数据报合同。

## Parallel Work Boundary

composition与 operation runtime基座完成并通过 strict validation后，Rollback分支可独立实现：

```text
DeterministicRollback Model Source/Pass/Pipeline
Endpoint/Protocol and canonical bundle assembler
Fixed Program Runtime/State/Kernel Target + Deterministic Backend
Fixed SimulationWorldSnapshot history/restore/replay/hash
DeterministicCollisionWorldArtifact
DeterministicKccWorldSolver + Fixed Actor pair contact
Rollback Output Disposition Pass
two-client Demo
```

它不依赖 ServerAuthoritative packet、Room 或 Unity/DotRecast server backend。两个并行分支唯一共享表面是已归档的 core contracts。

## Isolated Rollback Test Scene

Network Test Bootstrap Scene只按显式TestScenarioId跳转到DeterministicRollback Peer Scene，不创建Rollback Session、Endpoint、history、Fixed Program Runtime或KCC。Peer A/B使用不同launch profile复用同一个Peer Scene，并在Session准备前锁定PeerId、ActorId、Endpoint、Fixed Program、CollisionWorldArtifact与KccId。

Peer Scene显式引用完整Rollback Composition、Actor/出生点、量化world binding、Endpoint和diagnostics。返回Bootstrap或进入其它模型Scene时必须释放旧Session、Actor registration、Endpoint、history与KCC world；这些owner不得通过`DontDestroyOnLoad`跨Scene存活。Scene选择不允许转换为Active Session内的Float32/Fixed、Local/Rollback或Solver热切换。

## Failure Policy

- `.csir` 包含 Fixed Target 不支持的 operation/numeric conversion：Fixed Program 编译失败。
- Fixed Program 不满足 deterministic capability：Model 不可创建。
- Collision artifact 缺失/hash 不同：handshake/spawn 拒绝。
- Snapshot SemanticHash/Fixed ProgramHash/layout/world hash 不匹配：restore 拒绝。
- 迟到 input 早于 history floor：请求正式 world snapshot，不忽略 input。
- Peer 未收到连续 `CanonicalConfirmation`：继续保留有界 input/output history，不自行确认或释放。
- 已确认 Tick 的旧普通 canonical bundle 晚到：按 confirmation最终性丢弃；Host若尝试生成确认后的新修订则明确失败。
- Replay 后 hash 仍不同：报告 desync scope 并进入 snapshot recovery。
- KCC query 溢出、迭代不收敛或 unsupported world state：明确失败，不回退 Unity Physics。
- Actor pair容量溢出、连续 sweep无法得到合法TOI、固定迭代后仍穿透或静态/Actor约束无法同时满足：整个Step失败，不跳过该pair、不移动单侧Transform。
- Scene卸载后仍检测到旧Rollback Session、Endpoint、history或KCC world：新Session拒绝启动并报告owner conflict。

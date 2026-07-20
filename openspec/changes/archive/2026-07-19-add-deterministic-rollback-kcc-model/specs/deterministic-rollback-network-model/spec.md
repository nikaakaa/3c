# deterministic-rollback-network-model Specification

## ADDED Requirements

### Requirement: DeterministicRollback 必须是独立完整 Network Model

系统 MUST通过 DeterministicRollbackModelDefinition创建独立 Session Source、Rollback Pipeline和 Deterministic Backend。该模型 MUST自己拥有 canonical input、history、restore/replay、state hash、snapshot recovery和 output disposition policy，MUST不使用 ServerAuthoritative correction、packet或 history作为实现。

#### Scenario: SessionHost 创建 Rollback Session

- **WHEN** SessionHost 选择完整 DeterministicRollbackModelDefinition
- **THEN** MUST创建 Rollback Source/Endpoint/Pipeline/History/Output Disposition
- **AND** MUST不创建 ServerAuthoritative session

### Requirement: Rollback Model 必须严格校验 Deterministic Capability

Model MUST在创建前校验 SemanticHash、Fixed ProgramHash、Fixed ABI、Program deterministic capability、TickRate、CollisionWorldHash、KccId/capabilities、protocol version 和 actor roster 规则。任一项不满足 MUST拒绝创建，MUST不跳过 operation、加载 Float32 Program 或回退其他 Solver/Model。

#### Scenario: Program 包含 Nondeterministic Operation

- **WHEN** Fixed Program capability manifest 不满足 deterministic-compatible
- **THEN** Rollback model option MUST不可创建

### Requirement: Canonical Input Bundle 必须是唯一 Gameplay 网络输入

Endpoint/Host MUST按 SimulationTick 与 stable ActorId order 组装包含所有 Actor CharacterSimulationInput 的 canonical bundle。Peer MUST将固定 Tick冗余编码为一个连续`ActorInputBatch`，Host MUST在同一 Pump合并同 Tick中间修订并发布最终版本。Host MUST在 Tick 1显式输入齐备后发布 bootstrap bundle，并在输入前沿达到正式 input delay后锁定 canonical epoch；之后每次生成bundle前 MUST重新确认全部Actor共同显式连续前沿仍覆盖`NextCanonicalTick + InputDelayTicks`。墙钟到期 MUST不允许Host越过该前沿。Bundle MUST保留 predicted/canonical provenance、input sequence 和离散 request identity。重建后 GameplayHash未变化的 provenance/sequence更新 MUST不产生普通 revision广播，最终版本 MUST由 CanonicalConfirmation携带。Program/Kernel MUST不读取 endpoint packet。

#### Scenario: 某 Actor Input 迟到

- **WHEN** Host 之后收到 Tick T 的不同 input
- **THEN** MUST发布新 canonical bundle 并保留 revision/provenance

#### Scenario: Host 建立 Canonical Epoch

- **WHEN** 双 Actor Tick 1显式输入已经齐备但正式 input delay尚未填满
- **THEN** Host MUST先发布 Tick 1 bootstrap bundle
- **AND** MUST等待输入前沿达到配置的 delay后再连续推进后续 canonical tick

#### Scenario: 一个 Peer 的模拟速度低于 Host 墙钟

- **WHEN** canonical Tick已经到期但共同显式连续输入未保持配置的input delay lead
- **THEN** Host MUST暂停canonical推进
- **AND** MUST不通过missing input连续发布并在迟到后重建大段revision

#### Scenario: 同一渲染帧采集相机相对移动

- **WHEN** Unity Peer在RenderFrame采集camera-relative Vector2输入并在后续SimulationTick构造ActorInputBatch
- **THEN** 移动方向 MUST使用该RenderFrame锁存的CameraBasisSnapshot转换
- **AND** 输入值与可选CameraBasis字段 MUST来自同一次采样
- **AND** Program未声明CameraBasis输入时 MUST不把basis字段加入网络payload

### Requirement: Confirmed Horizon 必须由 Host 最终 Bundle 区间推进

Peer MUST不根据本地到包顺序、canonical contiguous tick或固定延迟自行确认。Host只有在全部Actor显式输入连续且超过confirmation delay后，才可发送包含完整最终bundle区间的可靠`CanonicalConfirmation`。Peer MUST按`previousConfirmedTick -> confirmedTick`连续应用乱序到达的confirmation；Endpoint Source MUST在暴露新的confirmed frontier前，于同一次IngressBatch交付该confirmation携带的全部最终bundle。普通canonical bundle MUST继续独立到达并触发rollback。确认后的旧普通bundle晚到时 MUST按过期副本丢弃，Host MUST拒绝确认后的新修订。

Pipeline restore MUST不回退单调递增的confirmed frontier。历史Pipeline projection恢复后，即使confirmed frontier本次没有继续增长，History Pass也 MUST按当前frontier再次释放已确认input与applied-input hash；后续snapshot MUST不重新包含这些已确认记录。

#### Scenario: 旧修订晚于确认消息到达

- **WHEN** Peer先收到覆盖Tick T的CanonicalConfirmation，之后才收到Tick T的旧普通canonical bundle
- **THEN** Peer MUST保留confirmation携带的最终bundle并忽略旧副本
- **AND** MUST不rollback或修改已经释放的confirmed output

#### Scenario: Source 第二次网络 Pump 收到 Confirmation

- **WHEN** 一次Source Read的发送后Pump推进confirmed frontier并接收最终bundle区间
- **THEN** Source MUST再次排空canonical queue
- **AND** Pipeline MUST在同一个IngressBatch先记录最终bundle再记录新的confirmed frontier

#### Scenario: Restore 恢复较早的 Pipeline Projection

- **WHEN** Rollback恢复的历史projection包含现在已经落在confirmed frontier内的applied-input记录
- **THEN** History Pass MUST在后续step capture前按当前confirmed frontier释放这些记录
- **AND** MUST不让确认水位回退或让旧记录累积进新snapshot

### Requirement: Rollback History 必须保存完整 Fixed SimulationWorldSnapshot

Rollback History Pass MUST保存有界 canonical input history与Fixed world snapshot history。Fixed Target MUST复用typed state schema和`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partition、numeric value、canonical codec与transaction specialization。World snapshot MUST包含SimulationTick、Fixed Program/Layout/codec identity、stable actor table、所有Actor committed SimulationState canonical bytes、Deterministic KCC actor/world state、RNG、Event/Command cursor和模型必要状态，MUST不保存active transaction、mutable typed partition或Float32 State/Snapshot，也 MUST不新增平行总世界状态aggregate。

Peer的predicted completed frontier MUST不超过本地canonical contiguous frontier加`MaximumRollbackDepthTicks`。达到上限时Ingress MAY继续接收canonical并重发同一待执行Tick输入，但Schedule MUST不新增predicted history；canonical差异触发的restore/replay仍 MUST执行。

#### Scenario: Capture Tick T

- **WHEN** History Pass保存 Tick T snapshot
- **THEN** MUST原子 capture 全部 Actor 和 KCC/world state
- **AND** MUST不只保存 Transform 或单个 Actor

#### Scenario: 快 Peer 达到最大预测领先

- **WHEN** 下一个predicted Tick会超过canonical contiguous frontier加MaximumRollbackDepthTicks
- **THEN** Schedule MUST返回NoStep并等待canonical推进
- **AND** input history MUST不因两个进程运行速度不同而无限增长

### Requirement: Late Input 必须触发原子 Restore 与 Replay

当 canonical input改变 Tick T时，Rollback Schedule Pass MUST产生恢复 T前最近完整 world snapshot的 restore directive，并按 Tick和 stable ActorId order用 canonical bundle产生 replay/current steps；Deterministic Backend MUST在同一 outer transaction使用同一 Fixed Program、Fixed Kernel和 Deterministic KCC执行全部步骤。

#### Scenario: Tick T 的 Attack Request 迟到

- **WHEN** 新 canonical bundle 修改 Tick T 的 Attack request
- **THEN** Rollback Pipeline MUST恢复完整 world并重演 T到当前 Tick

### Requirement: State Hash 必须支持分层 Desync 定位

Model MUST按固定 cadence 交换 confirmed world state hash，并能分解 Program/world/roster/actor/module/KCC subhash。Diagnostics 与 Presentation state MUST不进入 hash。

#### Scenario: 两端 World Hash 不同

- **WHEN** 同一 confirmed Tick 的 hash 不同
- **THEN** Model MUST报告首个不同的分层 scope

### Requirement: 严重 Desync 必须通过正式 World Snapshot 恢复

若 history不足、replay后 hash仍不同或 roster/world发生无法自愈的差异，Rollback Source MUST向模型指定 snapshot authority请求完整 Fixed `SimulationWorldSnapshot`，Schedule Pass校验并生成正式 restore directive，校验后原子恢复到唯一 Fixed `SimulationWorldStateSet`。MUST不改用 ServerAuthoritative correction或 Transform teleport。

#### Scenario: Late Input 早于 History Floor

- **WHEN** 受影响 Tick 已不在本地 snapshot history
- **THEN** Rollback Source MUST请求正式 world snapshot

### Requirement: Rollback 表现副作用必须按 EventId 提交

Rollback Output Disposition Pass MUST将 Fixed `SimulationActorTickResult`分为 predictable/reversible与 confirmed-only。Predictable output MAY立即提交并在 replay后按 EventId keep/replace/cancel；confirmed-only output MUST延迟到 confirmed horizon。Replay对旧已确认输出的保护边界 MUST取自outer transaction开始时的confirmed frontier；本事务内replay结果一致后新推进的confirmed Tick MUST仍完成replace/cancel并随后确认提交。Replay MUST不重复触发外部副作用。

#### Scenario: Replay 移除一个 Cue

- **WHEN** replay 后原 EventId Cue 不再存在
- **THEN** reversible Cue MUST撤销或替换
- **AND** confirmed-only one-shot MUST不在确认前提交

#### Scenario: Replay Tick 在同一事务内变为 Confirmed

- **WHEN** Tick T在事务开始时未确认且replay后与最终canonical一致并推进confirmed frontier
- **THEN** Output Committer MUST允许Tick T完成输出替换
- **AND** MUST只拒绝修改事务开始前已经确认的Tick

#### Scenario: 一个事务重演多个历史 Tick

- **WHEN** 一次outer transaction包含多个replay Step并对同一表现状态槽产生多次replace/cancel
- **THEN** Presentation Adapter MUST先完成整个事务的EventId历史更新
- **AND** MUST只向表现Runtime提交该状态槽的最终净结果
- **AND** MUST不逐条显示中间replay动画状态

#### Scenario: Playback Generation 已确认结束

- **WHEN** CompleteProducer或ReleaseProducer所在Tick进入confirmed horizon
- **THEN** Presentation Adapter MUST释放该generation的sample与terminal历史记录
- **AND** MUST保留仍生效状态槽的已确认基线
- **AND** MUST不通过扩大记录容量掩盖生命周期泄漏

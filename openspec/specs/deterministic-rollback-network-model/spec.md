# deterministic-rollback-network-model Specification

## Purpose

定义独立 DeterministicRollback 网络模型的 Fixed Program、canonical input、history、restore/replay、snapshot、hash、output disposition 和确定性能力锁定边界。

## Requirements
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

### Requirement: Gameplay 输入必须沿单一 Raw-to-Canonical 生命周期传播

Endpoint/Dedicated Relay Server MUST将每个输入事实按`ActorId + SimulationTick + InputSequence + GameplayHash`唯一识别，并沿`Local Explicit -> Relayed Explicit -> Canonical -> Confirmed`单向晋升。Peer MUST将固定Tick冗余编码为连续`ActorInputBatch`；配置的冗余帧数 MUST是历史上限，发送端 MUST从当前Tick向前选择可完整放入单个unreliable datagram的最大连续后缀。当前Tick单帧仍超过payload预算时 MUST明确失败，MUST不调大MTU、分片unreliable input或静默丢字段。Relay Server MUST校验发送Peer与Actor所有权、去重同一输入身份，并在接收后立即向其它Peer转发Relayed Explicit frame，同时把同一frame提交给canonical assembler。立即转发 MUST不等待同Tick其它Actor、canonical lead或confirmation delay。Canonical assembler MUST只在`NextCanonicalTick`的完整roster显式输入齐备后，按stable ActorId顺序生成不可变canonical bundle。Canonical bundle MUST是最终Gameplay排序的唯一bundle表示，但 MUST不再承担原始输入首次投递。相同GameplayHash的阶段晋升 MUST不触发replay；同一Actor/Tick/Sequence出现不同GameplayHash MUST视为协议冲突。Program/Kernel MUST不读取endpoint packet。

#### Scenario: Relay Server收到一个合法Actor Input Batch

- **WHEN** Peer A提交其Actor的连续冗余输入批次
- **THEN** Relay Server MUST先校验和去重frame，再立即向Peer B转发Relayed Explicit input
- **AND** MUST不等待Peer B同Tick输入或canonical bundle生成

#### Scenario: 配置的输入冗余超过单包预算

- **WHEN** 当前Tick加全部历史冗余无法装入一个unreliable datagram
- **THEN** Peer MUST发送包含当前Tick的最大连续历史后缀
- **AND** MUST不发送超过预算的数据报或丢弃当前Tick

#### Scenario: 当前 Tick 的完整 roster 输入齐备

- **WHEN** canonical assembler已经持有NextCanonicalTick的全部Actor显式输入
- **THEN** MUST按stable ActorId生成一个不可变canonical bundle
- **AND** 后续相同GameplayHash的冗余输入 MUST不产生普通revision

#### Scenario: Canonical 只提升输入阶段

- **WHEN** Peer已经用Relayed Explicit frame执行Tick T且后续canonical bundle包含相同GameplayHash
- **THEN** Source MUST只推进canonical provenance/frontier
- **AND** MUST不产生restore、replay或表现分支替换

#### Scenario: 同一输入身份内容冲突

- **WHEN** Relay Server收到同一Actor、Tick和Sequence但GameplayHash不同的frame
- **THEN** MUST报告协议冲突并结束该Session
- **AND** MUST不选择任一版本继续模拟

#### Scenario: 同一渲染帧采集相机相对移动

- **WHEN** Unity Peer在RenderFrame采集camera-relative Vector2输入并在后续SimulationTick构造ActorInputBatch
- **THEN** 移动方向 MUST使用该RenderFrame锁存的CameraBasisSnapshot转换
- **AND** 输入值与可选CameraBasis字段 MUST来自同一次采样
- **AND** Program未声明CameraBasis输入时 MUST不把basis字段加入网络payload

### Requirement: Confirmed Horizon 必须由 Dedicated Relay Server 最终 Canonical 区间推进

Peer MUST不根据本地到包顺序、relayed explicit contiguous tick、canonical contiguous tick或固定表现延迟自行确认。Relay Server只有在完整不可变canonical区间超过独立`ConfirmationDelayTicks`后，才可发送包含完整最终bundle区间的可靠`CanonicalConfirmation`。Peer MUST按`previousConfirmedTick -> confirmedTick`连续应用乱序到达的confirmation；Endpoint Source MUST在暴露新的confirmed frontier前，于同一次IngressBatch交付该confirmation携带的全部最终bundle。确认后的旧重复消息 MUST按输入身份丢弃，Relay Server MUST拒绝确认后的内容变化。

Pipeline restore MUST不回退单调递增的confirmed frontier。历史Pipeline projection恢复后，即使confirmed frontier本次没有继续增长，History Pass也 MUST按当前frontier再次释放已确认input与applied-input hash；后续snapshot MUST不重新包含这些已确认记录。Confirmed horizon MUST不作为远端Body或动画的固定表现缓冲。

#### Scenario: Confirmation 晚于普通 Canonical 到达

- **WHEN** Peer已收到Tick T的普通canonical bundle，之后收到覆盖Tick T的CanonicalConfirmation
- **THEN** Peer MUST将同一bundle晋升为Confirmed并释放对应最终输出
- **AND** MUST不因阶段晋升重新执行Tick T

#### Scenario: Source 第二次网络 Pump 收到 Confirmation

- **WHEN** 一次Source Read的发送后Pump推进confirmed frontier并接收最终bundle区间
- **THEN** Source MUST再次排空canonical/confirmation queue
- **AND** Pipeline MUST在同一个IngressBatch先记录最终bundle再记录新的confirmed frontier

#### Scenario: Restore 恢复较早的 Pipeline Projection

- **WHEN** Rollback恢复的历史projection包含现在已经落在confirmed frontier内的applied-input记录
- **THEN** History Pass MUST在后续step capture前按当前confirmed frontier释放这些记录
- **AND** MUST不让确认水位回退或让旧记录累积进新snapshot

### Requirement: Rollback History 必须保存完整 Fixed SimulationWorldSnapshot

Rollback History Pass MUST保存有界 canonical input history与Fixed world snapshot history。Fixed Target MUST复用typed state schema和`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partition、numeric value、canonical codec与transaction specialization。World snapshot MUST包含SimulationTick、Fixed Program/Layout/codec identity、stable actor table、所有Actor committed SimulationState canonical bytes、Deterministic KCC actor/world state、RNG、Event/Command cursor和模型必要状态，MUST不保存active transaction、mutable typed partition或Float32 State/Snapshot，也 MUST不新增平行总世界状态aggregate。

Peer的predicted completed frontier MUST不超过本地canonical contiguous frontier加`MaximumPredictionLeadTicks`。达到上限时Ingress MAY继续接收canonical并重发同一待执行Tick输入，但Schedule MUST不新增predicted history；canonical差异触发的restore/replay仍 MUST执行。`MaximumRollbackDepthTicks` MUST只用于restore/replay深度、history保护和deep recovery判定。

#### Scenario: Capture Tick T

- **WHEN** History Pass保存 Tick T snapshot
- **THEN** MUST原子 capture 全部 Actor 和 KCC/world state
- **AND** MUST不只保存 Transform 或单个 Actor

#### Scenario: 快 Peer 达到最大预测领先

- **WHEN** 下一个predicted Tick会超过canonical contiguous frontier加MaximumPredictionLeadTicks
- **THEN** Schedule MUST返回NoStep并等待canonical推进
- **AND** input history MUST不因两个进程运行速度不同而无限增长

### Requirement: Late Input 必须触发原子 Restore 与 Replay

当Relayed Explicit或canonical input的GameplayHash改变已经执行的Tick T时，Rollback Schedule Pass MUST产生恢复T前最近完整world snapshot的restore directive，并按Tick和stable ActorId order产生replay/current steps；同一outer transaction内多个晚到输入 MUST合并到最早受影响Tick。Deterministic Backend MUST在同一outer transaction使用同一Fixed Program、Fixed Kernel和Deterministic KCC执行全部步骤。只改变provenance而GameplayHash不变的输入 MUST不触发restore/replay。

#### Scenario: Tick T 的 Attack Request 迟到

- **WHEN** Tick T已使用空request预测且后续Relayed Explicit input包含Attack request
- **THEN** Rollback Pipeline MUST恢复完整world并重演T到当前Tick
- **AND** MUST在该outer transaction结束后只发布最终Body与动画分支

#### Scenario: Canonical 内容与 Relayed Explicit 相同

- **WHEN** Tick T的canonical GameplayHash与已应用Relayed Explicit input一致
- **THEN** MUST只推进canonical frontier
- **AND** MUST不增加rollback count

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

Rollback Output Disposition Pass MUST将 Fixed `SimulationActorTickResult`分为 predictable/reversible与 confirmed-only。有限Action `SelectProducer`与`SampleProducer` MAY立即提交并在replay后按EventId keep/replace/cancel；`CompleteProducer`与`ReleaseProducer` MUST属于confirmed-only，必须等待confirmed horizon后才提交到Action Playback Runtime。其它predictable output MAY立即提交并在 replay后按 EventId keep/replace/cancel；confirmed-only output MUST延迟到 confirmed horizon。Replay对旧已确认输出的保护边界 MUST取自outer transaction开始时的confirmed frontier；本事务内replay结果一致后新推进的confirmed Tick MUST仍完成replace/cancel并随后确认提交。Replay MUST不重复触发外部副作用。

Action predict/replay 的最终输出 MUST先在Fixed Presentation Adapter中合并为outer transaction级最终branch revision，再原子提交给Action Playback Runtime。撤销已消费的未确认`SelectProducer`或`SampleProducer` MUST不合成业务`ReleaseProducer`。

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

#### Scenario: 未确认 terminal 被 rollback 替换

- **WHEN** replay移除或替换尚未进入confirmed horizon的`CompleteProducer`或`ReleaseProducer`
- **THEN** Action Playback Runtime MUST从未观察到该terminal
- **AND** 最终分支的同generation `SampleProducer` MUST可以继续提交

### Requirement: Rollback Peer 必须优先使用目标 Tick 的远端显式输入

每个Peer MUST为每个远端Actor保存有界Relayed Explicit input history。构造predicted current bundle时，目标Tick存在显式输入则 MUST使用其完整连续值和离散request；缺失时 MAY使用最近连续值或neutral values，但 MUST清空离散request，MUST不预测不存在的动作。显式输入晚到时 MUST记录最早受影响Tick；confirmed frontier推进后 MUST裁剪不再需要的历史。

#### Scenario: 远端移动输入提前到达

- **WHEN** Peer B在执行Tick T前已收到Peer A的Tick T Relayed Explicit input
- **THEN** Peer B MUST用该exact input模拟Peer A
- **AND** MUST不使用上一Tick canonical input进行预测

#### Scenario: 远端离散动作尚未到达

- **WHEN** Peer B执行Tick T时只有Peer A上一Tick的连续输入
- **THEN** MAY延续连续values
- **AND** Tick T的远端requests MUST为空

### Requirement: Rollback 输入延迟必须按业务类别选择性应用

DeterministicRollback policy MUST不再拥有全局`InputDelayTicks`。连续input value与Immediate request MUST不增加模型Tick延迟；Offensive request MUST按显式`OffensiveRequestDelayTicks`调度，Corin双Peer Demo固定为2 Tick。离散request scheduler MUST保持capture sequence，后捕获request MUST不越过尚未eligible的前序Offensive request。该调度状态 MUST属于Rollback Source checkpoint，不得进入CharacterSimulationState或Presentation。

#### Scenario: 玩家持续改变移动方向

- **WHEN** MoveAxis每Tick产生新值
- **THEN** 本地Peer MUST在同Tick使用该值
- **AND** Relay Server MUST在收到后立即向远端Peer传播

#### Scenario: 玩家按下 Offensive Attack

- **WHEN** Corin Rollback Demo在Tick T捕获标记为Offensive的Attack request
- **THEN** request MUST在Tick T+2进入正式Fixed input frame
- **AND** 连续MoveAxis MUST不等待该request到期

### Requirement: DeterministicRollback Relay Server必须保持Relay-only DS职责

DeterministicRollback Dedicated Relay Server MUST只拥有网络会话、Peer/Actor roster、输入身份与所有权校验、immediate fanout、canonical排序、confirmation和hash/snapshot路由。Server MUST不执行Fixed Program、Deterministic KCC、WorldState、Animation或Presentation，也 MUST不成为Snapshot Gameplay authority。完整world snapshot MUST继续由model policy指定的Peer提供并经Server路由。

#### Scenario: 两端State Hash不一致

- **WHEN** Relay Server收到同一confirmed Tick的不同WorldHash
- **THEN** MUST按正式协议路由desync与snapshot恢复流程
- **AND** MUST不自行计算WorldState或选择隐藏的Gameplay结果

### Requirement: Rollback必须恢复确定性垂直动力状态

Deterministic Rollback完整Fixed `SimulationWorldSnapshot`、History、WorldStateHash、分层desync hash与Snapshot Recovery MUST包含每个Actor的`VerticalVelocity`。Restore与Replay MUST同时恢复Body pose、actual Velocity、VerticalVelocity、Grounded、Collision和KCC stable support state，并在下一Tick执行唯一Fixed Body Motion Prepare。Rollback model/protocol semantic version MUST分别为6/5，Fixed WorldState/WorldSnapshot/SessionSnapshot MUST分别使用v3/v4/v3；旧snapshot或缺失VerticalVelocity的payload MUST被拒绝，MUST不按当前KCC Grounded或actual Velocity.Y重建。

#### Scenario: Late Input回退到自由落体Tick

- **WHEN** Late Input使Peer回退到Actor处于自由落体的历史Tick
- **THEN** Restore MUST恢复该Tick完整VerticalVelocity
- **AND** Replay MUST对相同input和world contact产生相同后续轨迹与Hash

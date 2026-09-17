## MODIFIED Requirements

### Requirement: Rollback Model 必须严格校验 Deterministic Capability

Model MUST在创建前校验 SemanticHash、Fixed GameplayContentHash、Fixed ABI、角色玩法 deterministic capability、TickRate、CollisionWorldHash、KccId/capabilities、protocol version 和 actor roster 规则。任一项不满足 MUST拒绝创建，MUST不跳过 operation、加载 Float32 领域运行 或回退其他 Solver/Model。

#### Scenario: Program 包含 Nondeterministic Operation

- **WHEN** Fixed 领域运行 capability manifest 不满足 deterministic-compatible
- **THEN** Rollback model option MUST不可创建

#### Scenario: Graph包含Nondeterministic Operation

- **WHEN** Fixed Graph/domain capability manifest 不满足 deterministic-compatible
- **THEN** Rollback model option MUST不可创建


### Requirement: Gameplay 输入必须沿单一 Raw-to-Canonical 生命周期传播

Endpoint/Dedicated Relay Server MUST将每个输入事实按`ActorId + SimulationTick + InputSequence + GameplayHash`唯一识别，并沿`Local Explicit -> Relayed Explicit -> Canonical -> Confirmed`单向晋升。Peer MUST将固定Tick冗余编码为连续`ActorInputBatch`；配置的冗余帧数 MUST是历史上限，发送端 MUST从当前Tick向前选择可完整放入单个unreliable datagram的最大连续后缀。当前Tick单帧仍超过payload预算时 MUST明确失败，MUST不调大MTU、分片unreliable input或静默丢字段。Relay Server MUST校验发送Peer与Actor所有权、去重同一输入身份，并在接收后立即向其它Peer转发Relayed Explicit frame，同时把同一frame提交给canonical assembler。立即转发 MUST不等待同Tick其它Actor、canonical lead或confirmation delay。Canonical assembler MUST只在`NextCanonicalTick`的完整roster显式输入齐备后，按stable ActorId顺序生成不可变canonical bundle。Canonical bundle MUST是最终Gameplay排序的唯一bundle表示，但 MUST不再承担原始输入首次投递。相同GameplayHash的阶段晋升 MUST不触发replay；同一Actor/Tick/Sequence出现不同GameplayHash MUST视为协议冲突。角色玩法/Kernel MUST不读取endpoint packet。

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


### Requirement: Rollback History 必须保存完整 Fixed SimulationWorldSnapshot

Rollback History Pass MUST保存有界 canonical input history与Fixed world snapshot history。Fixed Target MUST复用typed state schema和`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partition、numeric value、canonical codec与transaction specialization。World snapshot MUST包含SimulationTick、Fixed 领域运行/Layout/codec identity、stable actor table、所有Actor committed SimulationState canonical bytes、Deterministic KCC actor/world state、RNG、Event/Command cursor和模型必要状态，MUST不保存active transaction、mutable typed partition或Float32 State/Snapshot，也 MUST不新增平行总世界状态aggregate。

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

当Relayed Explicit或canonical input的GameplayHash改变已经执行的Tick T时，Rollback Schedule Pass MUST产生恢复T前最近完整world snapshot的restore directive，并按Tick和stable ActorId order产生replay/current steps；同一outer transaction内多个晚到输入 MUST合并到最早受影响Tick。Deterministic Backend MUST在同一outer transaction使用同一Fixed 领域运行、Fixed Kernel和Deterministic KCC执行全部步骤。只改变provenance而GameplayHash不变的输入 MUST不触发restore/replay。

#### Scenario: Tick T 的 Attack Request 迟到

- **WHEN** Tick T已使用空request预测且后续Relayed Explicit input包含Attack request
- **THEN** Rollback Pipeline MUST恢复完整world并重演T到当前Tick
- **AND** MUST在该outer transaction结束后只发布最终Body与动画分支

#### Scenario: Canonical 内容与 Relayed Explicit 相同

- **WHEN** Tick T的canonical GameplayHash与已应用Relayed Explicit input一致
- **THEN** MUST只推进canonical frontier
- **AND** MUST不增加rollback count


### Requirement: State Hash 必须支持分层 Desync 定位

Model MUST按固定 cadence 交换 confirmed world state hash，并能分解 角色玩法/world/roster/actor/module/KCC subhash。Diagnostics 与 Presentation state MUST不进入 hash。

#### Scenario: 两端 World Hash 不同

- **WHEN** 同一 confirmed Tick 的 hash 不同
- **THEN** Model MUST报告首个不同的分层 scope


### Requirement: DeterministicRollback Relay Server必须保持Relay-only DS职责

DeterministicRollback Dedicated Relay Server MUST只拥有网络会话、Peer/Actor roster、输入身份与所有权校验、immediate fanout、canonical排序、confirmation和hash/snapshot路由。Server MUST不执行Fixed 领域运行、Deterministic KCC、WorldState、Animation或Presentation，也 MUST不成为Snapshot Gameplay authority。完整world snapshot MUST继续由model policy指定的Peer提供并经Server路由。

#### Scenario: 两端State Hash不一致

- **WHEN** Relay Server收到同一confirmed Tick的不同WorldHash
- **THEN** MUST按正式协议路由desync与snapshot恢复流程
- **AND** MUST不自行计算WorldState或选择隐藏的Gameplay结果


## ADDED Requirements

### Requirement: 回滚必须保留Fixed领域模拟和完整恢复

回滚 MUST保留 Fixed 数值规则、确定性世界、输入生命周期、快照、Hash、恢复重放与确认输出。角色状态 MUST迁为精确版本的领域分区；Pose 图及节点实例 MUST不进入快照或协议。Relay MUST继续只负责输入与一致性产品路由，不得运行角色或世界。

#### Scenario: LateInput触发多Tick重放
- **WHEN** 合法延迟输入要求恢复并重放
- **THEN** 系统 MUST恢复完整 Fixed 角色／世界／Pipeline 状态并使用原模型调度
- **AND** Pose MUST只消费模型正式提交的表现输入，不按重放 Tick 重复推进

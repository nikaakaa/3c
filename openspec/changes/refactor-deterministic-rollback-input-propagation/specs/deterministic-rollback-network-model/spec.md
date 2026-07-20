# deterministic-rollback-network-model Delta

## RENAMED Requirements

- FROM: `### Requirement: Canonical Input Bundle 必须是唯一 Gameplay 网络输入`
- TO: `### Requirement: Gameplay 输入必须沿单一 Raw-to-Canonical 生命周期传播`
- FROM: `### Requirement: Confirmed Horizon 必须由 Host 最终 Bundle 区间推进`
- TO: `### Requirement: Confirmed Horizon 必须由 Dedicated Relay Server 最终 Canonical 区间推进`

## MODIFIED Requirements

### Requirement: Gameplay 输入必须沿单一 Raw-to-Canonical 生命周期传播

Endpoint/Dedicated Relay Server MUST将每个输入事实按`ActorId + SimulationTick + InputSequence + GameplayHash`唯一识别，并沿`Local Explicit -> Relayed Explicit -> Canonical -> Confirmed`单向晋升。Peer MUST将固定Tick冗余编码为连续`ActorInputBatch`；配置的冗余帧数 MUST是历史上限，发送端 MUST从当前Tick向前选择可完整放入单个unreliable datagram的最大连续后缀。当前Tick单帧仍超过payload预算时 MUST明确失败，MUST不调大MTU、分片unreliable input或静默丢字段。Relay Server MUST校验发送Peer与Actor所有权、去重同一输入身份，并在接收后立即向其它Peer转发Relayed Explicit frame，同时把同一frame提交给canonical assembler。立即转发 MUST不等待同Tick其它Actor、canonical lead或confirmation delay。

Canonical assembler MUST只在`NextCanonicalTick`的完整roster显式输入齐备后，按stable ActorId顺序生成不可变canonical bundle。Canonical bundle MUST是最终Gameplay排序的唯一bundle表示，但 MUST不再承担原始输入首次投递。相同GameplayHash的Relayed到Canonical阶段晋升 MUST不触发replay；同一Actor/Tick/Sequence出现不同GameplayHash MUST视为协议冲突。Program/Kernel MUST不读取endpoint packet。

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

## ADDED Requirements

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

### Requirement: DeterministicRollback Relay Server必须保持Relay-only DS职责

DeterministicRollback Dedicated Relay Server MUST只拥有网络会话、Peer/Actor roster、输入身份与所有权校验、immediate fanout、canonical排序、confirmation和hash/snapshot路由。Server MUST不执行Fixed Program、Deterministic KCC、WorldState、Animation或Presentation，也 MUST不成为Snapshot Gameplay authority。完整world snapshot MUST继续由model policy指定的Peer提供并经Server路由。

#### Scenario: 两端State Hash不一致

- **WHEN** Relay Server收到同一confirmed Tick的不同WorldHash
- **THEN** MUST按正式协议路由desync与snapshot恢复流程
- **AND** MUST不自行计算WorldState或选择隐藏的Gameplay结果

#### Scenario: 玩家按下 Offensive Attack

- **WHEN** Corin Rollback Demo在Tick T捕获标记为Offensive的Attack request
- **THEN** request MUST在Tick T+2进入正式Fixed input frame
- **AND** 连续MoveAxis MUST不等待该request

#### Scenario: Offensive 后捕获另一个离散请求

- **WHEN** 前序Offensive request尚未eligible且后续request已经捕获
- **THEN** scheduler MUST保持capture sequence
- **AND** 后续request MUST不越过前序request进入更早Tick

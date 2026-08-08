## MODIFIED Requirements

### Requirement: Rollback History 必须保存完整 Fixed SimulationWorldSnapshot

Rollback History Pass MUST保存有界 canonical input history与Fixed world snapshot history。Fixed Target MUST复用typed state schema和`Begin -> Evaluate -> Finalize -> Commit|Abort`事务生命周期形状，但 MUST实现自己的Fixed partition、numeric value、canonical codec与transaction specialization。World snapshot MUST包含SimulationTick、Fixed Program/Layout/codec identity、stable actor table、所有Actor committed SimulationState canonical bytes、Deterministic KCC actor/world state、RNG、Event/Command cursor和模型必要状态，MUST不保存active transaction、mutable typed partition或Float32 State/Snapshot，也 MUST不新增平行总世界状态aggregate。

Peer的predicted completed frontier MUST不超过本地canonical contiguous frontier加`MaximumPredictionLeadTicks`。达到上限时Ingress MAY继续接收canonical并重发同一待执行Tick输入，但Schedule MUST不新增predicted history；canonical差异触发的restore/replay仍 MUST执行。`MaximumRollbackDepthTicks` MUST只用于restore/replay深度、history保护和deep recovery判定。

#### Scenario: Capture Tick T

- **WHEN** History Pass保存 Tick T snapshot
- **THEN** MUST原子 capture 全部 Actor 和 KCC/world state
- **AND** MUST不只保存 Transform 或单个 Actor

#### Scenario: 快 Peer 达到最大预测领先

- **WHEN** 下一个predicted Tick会超过canonical contiguous frontier加`MaximumPredictionLeadTicks`
- **THEN** Schedule MUST返回`NoStep`并等待canonical推进
- **AND** Ingress MUST继续处理explicit、canonical和confirmation
- **AND** input history MUST不因两个进程运行速度不同而无限增长

#### Scenario: 晚到输入需要深度回滚

- **WHEN** 已执行Tick的输入变化并需要restore/replay
- **THEN** Schedule MUST按`MaximumRollbackDepthTicks`判断普通回滚或deep recovery
- **AND** MUST不使用`MaximumPredictionLeadTicks`代替回滚深度判断

### Requirement: Rollback 表现副作用必须按 EventId 提交

Rollback Output Disposition Pass MUST将 Fixed `SimulationActorTickResult`分为predictable/reversible与 confirmed-only。有限Action `SelectProducer`与`SampleProducer` MAY立即提交并在replay后按EventId keep/replace/cancel；`CompleteProducer`与`ReleaseProducer` MUST属于confirmed-only，必须等待confirmed horizon后才提交到Action Playback Runtime。其它predictable output MAY立即提交并在replay后按 EventId keep/replace/cancel；其它confirmed-only output MUST延迟到 confirmed horizon。Replay对旧已确认输出的保护边界 MUST取自outer transaction开始时的confirmed frontier；本事务内replay结果一致后新推进的confirmed Tick MUST仍完成replace/cancel并随后确认提交。Replay MUST不重复触发外部副作用。

Action predict/replay 的最终输出 MUST先在 Fixed Presentation Adapter 中合并为outer transaction级最终branch revision，再原子提交给Action Playback Runtime。撤销已消费的未确认Select/Sample MUST不合成业务Release。

#### Scenario: Replay 移除一个 Cue

- **WHEN** replay 后原 EventId Cue 不再存在
- **THEN** reversible Cue MUST撤销或替换
- **AND** confirmed-only one-shot MUST不在确认前提交

#### Scenario: Replay Tick 在同一事务内变为 Confirmed

- **WHEN** Tick T在事务开始时未确认且replay后与最终canonical一致并推进confirmed frontier
- **THEN** Output Committer MUST允许Tick T完成输出replace
- **AND** MUST只拒绝修改事务开始前已经确认的Tick

#### Scenario: 一个事务重演多个历史 Tick

- **WHEN** 一次outer transaction包含多个replay Step并对同一Action playback产生多次replace/cancel
- **THEN** Presentation Adapter MUST先完成整个事务的EventId历史更新
- **AND** MUST只向Action Playback Runtime提交该playback的最终branch revision
- **AND** MUST不逐条显示中间replay动画状态

#### Scenario: 未确认 terminal 被 rollback 替换

- **WHEN** replay移除或替换尚未进入confirmed horizon的CompleteProducer或ReleaseProducer
- **THEN** Action Playback Runtime MUST从未观察到该terminal
- **AND** 最终分支的同generation Sample MUST可以继续提交

#### Scenario: Playback Generation 已确认结束

- **WHEN** CompleteProducer或ReleaseProducer所在Tick进入confirmed horizon
- **THEN** Presentation Adapter MUST先把terminal提交到Action Playback Runtime
- **AND** 提交成功后 MUST释放该generation的sample与terminal rollback历史记录
- **AND** MUST不通过扩大记录容量掩盖生命周期泄漏

## ADDED Requirements

### Requirement: 预测领先边界必须锁定在模型身份中

`MaximumPredictionLeadTicks` MUST进入Model Policy configuration hash、model semantic identity、Server Manifest、Product manifest和Client/Relay handshake compatibility。任一Peer或Relay的prediction lead配置不一致时，Session MUST拒绝进入Active，MUST不运行时协商或使用默认值。

#### Scenario: Peer 使用不同预测领先边界

- **WHEN** Client提交的Model configuration hash与Relay Manifest不一致
- **THEN** Relay MUST拒绝该Client
- **AND** SimulationTick MUST不开始

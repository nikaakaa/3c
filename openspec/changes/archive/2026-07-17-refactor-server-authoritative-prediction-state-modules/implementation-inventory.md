# 实现清单

## 外部合同基线

唯一Source port为`IServerAuthoritativePredictionStatePort`，其`State`继续返回唯一`ServerAuthoritativePredictionState`。三个Pass只持有该aggregate root：

| Pass | Pass-facing入口 | SnapshotParticipant |
|---|---|---|
| Correction Schedule | `ApplyAck`、`ObserveAuthorityClock`、`ScheduleRequests`、`Decide`、`GetReplayAfter`、`BuildRestore` | `server-authoritative.prediction.correction` / `server-authoritative-correction-state/3` / schema version 3 |
| Prediction History Egress | `AddHistory` | `server-authoritative.prediction.history` / `server-authoritative-prediction-history/1` / schema version 1 |
| Output Disposition | `WasCommitted`、`Record`、`SealHistoryJournalCursor` | `server-authoritative.prediction.output-journal` / `server-authoritative-event-disposition-journal/2` / schema version 2 |

Pipeline state内三个Participant顺序固定为Correction Schedule、History Egress、Output Disposition。`CaptureCheckpoint`在各Pass进入outer transaction时取得对应内存快照，rollback只恢复对应模块；`CaptureState`与`PrepareRestore`继续通过aggregate root映射同一Participant。

## 当前字段与所有权

迁移前单体字段为：按Tick排序的`History`、按EventId排序的`Journal`、按sequence排序的`PendingRequests`、`JournalCursor`、`LastRejectedCount`、confirmed input/event cursor、authority ack/baseline/clock cursor，以及只读`Policy`、`Program`、`Compatibility`与`RestorePort`。

迁移后唯一所有权固定为：

- Confirmation/Request：confirmed input/event、authority ack/baseline/clock与pending requests。
- History：history records、replay查询、journal cursor seal、capacity与confirmed pruning。
- Journal：EventId entries、cursor、last rejected count、authority reconciliation、capacity与prune。
- Reconciler：只读Program/Compatibility/RestorePort、identity校验、decision与restore snapshot构造，不拥有可变集合。
- Codec：三个canonical payload与nested Pipeline projection exact-byte读写，不拥有活动状态。

aggregate root公开属性保持`Policy`、`ConfirmedInputSequence`、`ConfirmedEventHorizon`、`LastAuthorityAckTick`、`LastBaselineTick`、`LastAuthorityClockEstimate`、`JournalCursor`、`JournalCount`、`HistoryCount`、`PendingRequestCount`、`LastRejectedCount`和`LastPredictedInputSequence`。

## Canonical schema冻结

Correction payload固定为magic `0x43524153`、version 3，顺序为confirmed input sequence、event horizon sequence/EventId、last ack tick、last baseline tick、authority clock、pending count，以及按sequence排序的request id/sequence/source tick/expire tick/priority。count上限为`HistoryCapacity`。

History payload固定为magic `0x48524153`、version 1，按Tick排序写入tick、actor、source tick、input sequence、canonical input、composition identity、world snapshot、pipeline projection和journal cursor。count上限为`HistoryCapacity`。nested Pipeline projection继续写Pipeline identity、Backend identity、last completed tick以及最多64个按原顺序保存的Participant。

Journal payload固定为magic `0x4a524153`、version 2，顺序为journal cursor、entry count，以及按EventId排序的EventId/tick/sequence/disposition。count上限为`HistoryCapacity * 64`；`LastRejectedCount`仍是transaction内诊断状态，不进入canonical payload。

## 容量与淘汰基线

- History满时只可从最早Tick淘汰`InputSequence <= ConfirmedInputSequence`的record；否则错误必须包含firstTick、firstSequence、confirmedSequence、lastAckTick和lastBaselineTick。
- confirmed pruning删除`InputSequence <= confirmed`的history；replay返回`InputSequence > confirmed`的record并保持Tick顺序。
- Journal新增不同EventId前先按first retained history tick剪枝；容量固定为`HistoryCapacity * 64`。
- `PredictedCommitted`和`SuppressedDuplicate`属于live event，不因history tick剪枝；已终结entry且早于first retained tick才删除。
- pending request按sequence去重，同sequence内容变化失败，容量固定为`HistoryCapacity`。

## 转换与identity基线

迁移前`ApplyAck`按Actor、ack tick回退和event horizon同sequence同EventId规则校验，再重分类Journal，最后推进ack/confirmed cursor。NoCorrection baseline会重分类Journal、推进confirmed/baseline、prune History和Journal。纠偏baseline在`BuildRestore`选择同Tick或最后history frame，合并完整Character/World/Pipeline snapshot，清理旧history并写入RestorePort。

模块化后这些业务结果不变，但prepare阶段必须先完成全部校验和完整候选快照构造，再一次提交模块状态。codec读取也必须先返回完整不可变checkpoint，不能边读边改活动集合。

Baseline identity校验冻结为NumericProfile、Target ABI、StateCodecIdentity、ProgramHash、LayoutHash、OperationSetVersion、SolverId、SolverVersion、SolverCapabilities；随后解码Character state验证Program，并在history存在时验证ActorId、local Program/Layout、Solver identity与WorldRevision。World merge继续只替换同Actor/Program/Layout的权威Character和Body，Pipeline merge继续删除旧三个Prediction Participant后按固定顺序重建。

## 不变与删除边界

本change不修改packet、Fantasy protocol/generated文件、Network Checkpoint、baseline/ack DTO、Pipeline identity/hash规则、Model Policy、HistoryCapacity、MaximumReplayTicks、position/yaw tolerance或HardRecovery policy。迁移完成后删除单体类中的三个集合、codec/header/count helper、capacity/prune helper、identity/merge helper和旧checkpoint DTO；不保留委托桥接、兼容reader或双写payload。

## 最终实现结果

- `ServerAuthoritativePredictionState`由971行降为340行，只保留aggregate编排、原子提交和既有Pass-facing入口。
- Confirmation、History、Disposition Journal、Reconciler与Codec分别进入独立内部文件；三个checkpoint跟随各自模块，不形成第四份持久状态。
- `ApplyAck`先准备Journal重分类与Confirmation checkpoint；NoCorrection baseline先准备Journal、History和Confirmation候选状态；`BuildRestore`先完成三个canonical payload、完整World/Pipeline snapshot与directive，再写入RestorePort并提交模块状态。
- 三个restore reader均先完整解码为不可变checkpoint，解码失败不修改活动状态。
- Pass仍只引用唯一aggregate root；Correction Schedule、History Egress与Output Disposition的生产职责、StateOwner、SchemaId、SchemaVersion及Participant顺序不变。
- `3C_Client.sln`使用禁用build server与shared compilation参数编译通过，结果为5个既有警告、0个错误；随后已关闭MSBuild和编译器服务器。
- 本change strict validation通过；全仓strict validation为63项通过、0项失败。
- 未运行Unity batchmode，未新增测试，未归档change。

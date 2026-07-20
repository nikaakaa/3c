## Context

当前一个`ServerAuthoritativePredictionState`直接保存：

```text
PendingRequests
ConfirmedInputSequence / EventHorizon
LastAuthorityAckTick / LastBaselineTick / AuthorityClock
PredictionHistory<Tick, Record>
DispositionJournal<EventId, Entry>
JournalCursor / LastRejectedCount
Program / Compatibility / RestorePort
```

并同时实现Schedule request retention、ack、baseline correction decision、restore build、history/journal pruning、checkpoint clone及三种canonical codec。Correction Schedule、History Egress和Output Disposition Pass都取得同一State引用，这保证了唯一真相，但所有不变量集中在单一文件，修改局部规则时容易误伤其它Participant。

## Goals

- 保持一个Prediction State port与一个aggregate root。
- 将Confirmation、History、Journal与Reconciliation不变量放入各自深模块。
- 保持三个SnapshotParticipant、schema identity与canonical bytes完全不变。
- 让跨模块ack、baseline与restore操作先完成全部验证，再原子提交状态变化。
- 保持现有Pass调用与Source/Network外部合同不变。

## Non-Goals

- 不把内部模块变成可替换策略接口。
- 不让每个Pass各自创建一份Prediction状态。
- 不修改当前history与journal容量公式。
- 不改变baseline merge、position/yaw tolerance或replay范围算法。

## Target Modules

```text
ServerAuthoritativePredictionState
  aggregate root / orchestration
  |
  +-- ServerAuthoritativePredictionConfirmationState
  |     pending requests, ack/baseline/clock cursors
  |
  +-- ServerAuthoritativePredictionHistory
  |     records, replay range, capacity, confirmed pruning
  |
  +-- ServerAuthoritativePredictionDispositionJournal
  |     EventId disposition, cursor, reconcile, pruning
  |
  +-- ServerAuthoritativePredictionReconciler
  |     identity validation, decision, merge, restore plan
  |
  +-- ServerAuthoritativePredictionStateCodec
        exact Correction/History/Journal/Pipeline bytes
```

这些模块均为portable ServerAuthoritative程序集内部实现。Pass只继续看到aggregate root，不直接持有子模块，防止重新形成三份状态所有权。

## Decision 1: 保留Prediction State作为aggregate root

`IServerAuthoritativePredictionStatePort.State`继续返回唯一`ServerAuthoritativePredictionState`。现有Pass-facing方法名与调用时序保持不变；aggregate root将调用降低到内部模块，并负责需要跨模块的顺序与原子提交。

### Tradeoff

- 让三个Pass分别取得History、Journal和Correction port可以减少外观方法，但会把一致性责任推给Pipeline绑定，并允许模块生命周期不一致。
- 保留一个aggregate root使Source、Pass与restore transaction仍共享一个owner，因此选择该方案。

## Decision 2: Confirmation与pending request属于同一模块

Correction state canonical payload当前同时保存confirmation cursor、authority clock和pending request。为保持Participant身份和bytes不变，这些字段由`ServerAuthoritativePredictionConfirmationState`统一拥有：

- `ConfirmedInputSequence`。
- `ConfirmedEventHorizon`。
- `LastAuthorityAckTick`。
- `LastBaselineTick`。
- `LastAuthorityClockEstimate`。
- 按sequence排序的pending `SimulationInputRequest`。

该模块只管理cursor与request集合，不读取World snapshot、计算body误差或写EventId journal。

## Decision 3: History与Journal分别拥有容量和淘汰规则

History模块唯一决定：

- history record按SimulationTick唯一排序。
- 新record加入前能否淘汰已确认record。
- replay查询和confirmed sequence pruning。
- record的journal cursor seal。

Journal模块唯一决定：

- EventId disposition去重与cursor递增。
- authority horizon下confirmed/rejected重分类。
- live predicted event不能被错误淘汰。
- 按first retained history tick剪枝。

History不得直接修改Journal集合。aggregate root把`firstRetainedTick`作为值传给Journal prune；Journal不得反向持有History引用。

## Decision 4: Reconciler只计算和构造，不拥有可变集合

Reconciler持有Program、Compatibility与RestorePort等只读依赖，负责：

- 校验baseline与Program/Layout/OperationSet/Solver/Actor/World identity。
- 比较Character state hash、body position与yaw。
- 计算NoCorrection、RestoreReplay或HardRecovery decision。
- 从合法local history frame与baseline构造新的World/Pipeline snapshot和restore directive。

Reconciler不保存history、journal或confirmation cursor。aggregate root先取得不可变输入，完成decision/restore plan构造后，再提交Confirmation、History与Journal变化。

## Decision 5: 跨模块状态转换必须原子

主要转换固定为：

```text
ApplyAck
  validate ack and actor
  compute merged horizon
  prepare journal reconciliation
  commit journal changes
  commit confirmation cursor

AdvanceBaseline
  validate baseline and compute reconciliation
  commit journal reconciliation
  commit confirmation/baseline cursor
  prune confirmed history
  prune journal against retained history

BuildRestore
  select local frame
  validate and build complete restore snapshot/plan
  commit baseline confirmation
  clear/prune history and journal
  store complete restore snapshot
```

任何prepare阶段失败不得修改模块。Pipeline Pass的checkpoint/restore transaction继续作为outer transaction回滚保障，但内部实现不得依赖“先写坏再等外层回滚”作为正常控制流。

## Decision 6: Canonical codec集中且版本冻结

`ServerAuthoritativePredictionStateCodec`集中实现：

- Correction magic `0x43524153`、version `3`。
- History magic `0x48524153`、version `1`。
- Journal magic `0x4a524153`与当前`JournalStateSchemaVersion`。
- History内World snapshot与Pipeline projection exact-byte嵌套编码。

字段顺序、count上限、排序、magic、version、StateOwner、StateSchemaId和SnapshotParticipant顺序全部冻结。迁移不引入新reader、schema升级或兼容分支；旧单体codec在新codec接管后删除。

## Decision 7: Checkpoint DTO按模块收敛

Correction、History与Journal继续各自提供in-memory checkpoint，用于Pass transaction的capture/rollback。Checkpoint为不可变模块快照，不序列化、不成为第四份状态，也不跨Session保存。旧checkpoint DTO迁入对应模块或由统一内部record替代，aggregate root只转发Pass所需的capture/restore入口。

## Sequencing With DotRecast

本change不引用Unity或DotRecast，但`add-dotrecast-authoritative-server-backend`后续Client Prediction与Authority checkpoint任务会直接消费这些类型。为避免一边新增调用一边迁移State，DotRecast change保持暂停；本change完成后更新其inventory并继续，不创建适配层。

## Failure And Deletion Rules

- canonical payload或hash变化：迁移失败，不升级schema。
- 同一字段被两个模块保存：迁移失败并删除重复owner。
- Pass直接持有子模块或创建子模块：迁移失败。
- ack/baseline/restore失败后出现部分模块变化：实现失败。
- 旧集合、codec、checkpoint helper或委托桥接仍可调用：删除后才能完成。

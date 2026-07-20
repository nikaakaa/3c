## Context

当前一次 Float32 Actor Step 的状态链是：

```text
Committed CharacterSimulationState
-> ToBuilder：复制全部 StateSlot
-> Program Evaluate
-> BuildPending：物化中间 CharacterSimulationState
-> PendingCharacterEvaluation.StagedState
-> ToBuilder：Finalize再次复制全部 StateSlot
-> 写 Motion fact/sequence并清理 transient bytes
-> Build：物化最终 CharacterSimulationState
```

领域模块同时把复杂状态写入通用 bytes slot：

```text
InputRequestBuffer       -> bytes/request
ActionRequest            -> bytes/request
ActionInstance           -> bytes/instance
ActionLifecycle          -> bytes/重复镜像
TimelineRetentionIdentity-> bytes/完整ActionInstance副本
GameplayEffect           -> 五份bytes + cursor
Motion pending           -> 两份相同bytes
```

`CharacterSimulationStateCodec`需要 bytes是正确的，因为 Snapshot、Hash、Network和普通 .NET Host都需要稳定交换格式。错误在于领域执行也依赖同一 bytes表示，使“状态是什么”与“状态怎样传输”无法分开。

## Goals

- 一次 Actor Step只创建一个未提交 State Transaction，并从 Evaluate延续到 Finalize。
- Committed Character State保持不可变，失败时可直接丢弃 transaction，不触碰正式 working state。
- Runtime state使用明确类型，Canonical bytes只存在于 codec边界。
- State读写成本按dirty state增长，不按全部 StateSlot数量固定复制。
- Program Layout、Snapshot、Hash和Network仍有稳定、可版本化、可由普通 .NET Host读取的正式ABI。
- Float32和未来 Fixed共享同一状态语义、地址与事务生命周期，不共享具体数值实现或mutable对象。

## Decision

### 1. 唯一状态链

采用以下唯一链路：

```text
Committed typed Character State
-> Begin Float32CharacterStateTransaction
-> Evaluate写入同一transaction
-> PendingCharacterEvaluation持有transaction + WorldRequest
-> ResolveBatch不访问transaction
-> Finalize校验WorldResult并继续写同一transaction
-> Transaction.Commit(Tick)
-> 新Committed typed Character State
-> Pipeline working world
-> Egress成功后outer atomic publish
```

`Transaction.Commit`只表示生成新的不可变 Actor state，不能发布 Session正式 state。Pipeline Backend仍拥有outer working world与最终publish/Committer边界。

### 2. Program State地址与typed storage分离

`ProgramStateSlot.Index`、Owner、Semantic和SourceMap继续是稳定的Program地址。StateSlot新增或使用明确的typed value kind；`ProgramExecutionLayout`在Program校验时把每个slot降低为：

```text
TypedStateAddress
  SlotIndex
  ValueKind
  PartitionIndex
  PageIndex
  Offset
```

Committed `CharacterSimulationState`按kind保存不可变partition。Primitive、Blackboard动态值与领域aggregate可以使用不同partition，但外部仍只能按Program声明的slot访问。运行时不得通过反射、object字典、字符串type name或service locator解析state kind。

### 3. Copy-on-write页与write-set

选择不可变分页storage + transaction write-set：

- Begin只绑定base state，不复制全部slot。
- 首次写某页时复制该页；同页后续写复用transaction页。
- 未修改页在Commit后与base state共享。
- 领域aggregate使用typed copy-on-write root；未修改aggregate直接共享，首次修改建立transaction-owned mutable working copy。
- Commit只冻结dirty页和dirty aggregate，并生成新state root。
- Abort/Dispose丢弃dirty页、aggregate和输出staging。

页大小是Float32 State ABI内部常量，MUST进入实现版本和diagnostics，但不进入authoring。不得根据角色、Network Model或Tick动态选择不同页策略。

### 4. 不采用原地修改 + undo journal

原地修改可以减少成功路径复制，但它要求每个primitive、集合增删、排序、GE modifier与嵌套Additional Effect都写完整undo记录。任何漏记都会破坏Pipeline失败原子性，且mutable引用可能泄漏到Snapshot或并行Actor处理。因此选择copy-on-write transaction；GE局部事务使用同一transaction的typed savepoint，而不是第二套undo系统。

### 5. Transaction与领域port

领域模块不能取得万能transaction对象。Evaluation Frame按Program级policy创建窄port：

| Port | 可访问状态 |
|---|---|
| Control state port | Runnable、Composite、StateMachine lifecycle primitive |
| Blackboard state port | declaration对应typed value、scope owner/generation/provenance |
| Input state port | typed request entries |
| Action state port | typed activation request、instance、reference、sequence |
| Timeline state port | playback/loop/time primitive与typed retained action reference |
| GameplayEffect state port | 单一typed GE aggregate |
| Finalize state port | fact sequence和Finalize允许的committed字段 |

每个port只接受预计算typed address。非法kind、semantic或owner在Program Layout创建时失败；Tick内不扫描StateSlots寻找owner。

### 6. PendingCharacterEvaluation不是State Snapshot

`PendingCharacterEvaluation`是单个Step内的target-specific临时产品：

- 持有未提交transaction。
- 持有WorldRequest、pre-world facts、presentation和trace staging。
- 只能被同一Kernel specialization、Actor、Tick、Program/Layout和一次Finalize消费。
- 不可编码、不可进入Snapshot、不可进入History、不可发送网络、不可重复Finalize。

删除`StagedState`与`BuildPending()`。WorldSolver只接收WorldRequest，不得取得transaction。

### 7. Input typed state

每个编译后的input request拥有预计算typed address，值为明确的Request State：request id由layout绑定，运行值保存sequence、source tick、expire tick、priority和consumed状态。Input Adapter仍只提交`CharacterSimulationInput`；Input module将request写入transaction并按typed address查询/消费。

不存在第二个request buffer object或bytes镜像。Program级request index是immutable layout service，不是Gameplay state。

### 8. Action与Timeline retention typed state

Action状态收敛为：

- `ActionActivationRequestState`。
- `ActionInstanceState`，包含lifecycle phase/state/last transition/reason。
- `ActionInstanceReference`，只保存Timeline持续运行需要的最小ActionId/ContextId/InstanceId/PredictionKey身份。
- `ActionTargetSnapshot`作为明确typed Blackboard value。
- Action Event Sequence primitive。

删除单独`ActionLifecycle` bytes镜像和`ActionContext` UInt64镜像。当前active context由typed ActionInstance与Program级Action index唯一解析。Timeline retention不复制完整ActionInstance；它通过reference向Action state port验证当前实例。

### 9. GameplayEffect typed aggregate与savepoint

一个Actor只有一个正式`GameplayEffectState` aggregate，包含canonical ordered：

- Tag sources。
- Attributes与modifiers。
- Active effects。
- Period schedule。
- Prediction journal与lifecycle revision。
- Change cursor。

Program Layout只声明一个GE aggregate地址，不再声明五份bytes slot和独立cursor镜像。GE Runtime在Evaluation开始取得transaction-owned typed view，不执行Load；Evaluation结束不执行Save编码。

Effect Apply/Remove/Period/Additional Effect需要局部原子性时创建typed savepoint。Savepoint记录aggregate root和GE change projection cursor；失败恢复同一transaction内的typed root，成功释放。Canonical codec不参与GE业务事务。

### 10. Motion是同Step临时数据

MotionContribution、MotionAccumulator和最终`CharacterMotionRequest`只属于Evaluation Frame/Pending product。它们影响当前WorldSolve，但在Finalize后不影响未来Tick，因此不进入Committed Character State。

删除`MotionAccumulator`和`PendingWorldRequest` StateSlot semantic及其重复bytes。Finalize直接从Pending product取得expected WorldRequest并生成Motion fact/body sample，不再“清理”state slot。

### 11. Canonical codec边界

`CharacterSimulationStateCodec`按Program Layout的slot稳定顺序编码typed committed state：

```text
Header
  NumericProfile
  TargetAbi
  ProgramId/ProgramHash/LayoutHash
  StateCodecVersion
  LastCompletedTick
Values
  stable SlotIndex order
  kind-specific canonical payload
```

领域typed codec负责值内容，顶层State codec负责顺序、kind和完整性。Decode必须先校验Program/Layout/ABI，再构造不可变typed partitions。未知kind、重复entry、非canonical顺序、非法有限数值或payload残留直接失败。

Snapshot Capture对每个Actor只编码一次并复用同一bytes计算CharacterStateHash与构造ActorSnapshot。Network Baseline直接携带该ActorSnapshot bytes和identity，不重新遍历mutable domain state。

### 12. ABI与正式迁移

这是破坏性Float32 State ABI迁移：

- 提升Target ABI与State codec version。
- Program State Layout改变，LayoutHash和ProgramHash改变。
- 旧`.csim`、ProgramAsset metadata、Character State bytes、Snapshot和Network Baseline全部失效。
- Editor Build Transaction从同一`.csir`重新lower并发布Corin Program/Projection。
- Runtime只接受新ABI，不提供旧reader、自动state migrator、兼容enum、双codec或stale artifact fallback。

Authoring、SemanticHash和业务operation不因本change改变；若Frontend state declaration需要从Bytes改成typed semantic token，只改变Target state schema，不新增模型专用节点或第二Semantic IR。

### 13. 与网络模型的并行边界

可以继续并行的ServerAuthoritative工作：

- Model/Endpoint/Source Definition与preparation lifecycle。
- Fantasy连接、消息生成、Room路由、Actor ownership和有界queue。
- ProductId、PassId、Pipeline descriptor和diagnostics schema。
- Program/Pipeline/roster/solver identity handshake。

必须在新State ABI后接线的工作：

- Prediction History中的Character state payload与hash。
- Authority Baseline capture/decode。
- Baseline Merge、Correction restore、Replay与HardRecovery。
- SnapshotParticipant codec绑定。
- 任何直接引用`CharacterSimulationStateBuilder`、`CharacterStateValue.Bytes`或旧State codec version的网络实现。

网络分支若已经写入旧类型，迁移时直接替换并删除旧引用，不新增adapter或同时支持两种payload。

### 14. Diagnostics

State diagnostics只读暴露：

- Base State identity。
- Transaction Actor/Tick/Program binding。
- dirty partition/page/value计数。
- typed domain dirty summary。
- committed/aborted状态。
- canonical encode bytes长度与hash identity。

Diagnostics不能枚举mutable集合引用、强制Commit、触发Snapshot或改变dirty状态。

## Rejected Alternatives

### 每Tick开始统一decode、结束统一encode

比当前多次codec稍好，但bytes仍是运行态真相，GE/Input/Action每Tick仍支付与状态体积相关的固定成本，Fixed Target也会复制同样设计，因此拒绝。

### 保留Builder，只缓存领域对象

会形成`Builder bytes`与`cached typed object`双真相，需要dirty同步和失效规则；Snapshot或某模块绕过cache写slot时会产生分裂状态，因此拒绝。

### 原地修改Committed State并记录undo

成功路径快，但嵌套GE事务、集合排序、Pipeline失败和并行Actor处理使undo完整性难以证明，mutable引用也容易泄漏，因此拒绝。

### Commit时重新复制全部StateSlot

实现简单，但仍然让每Tick成本与Program总slot数绑定。既然本change已经提升ABI，采用分页copy-on-write一次解决，不保留半完成结构。

### 继续允许通用Bytes StateSlot给未来扩展

通用bytes无法在编译期检查状态语义，也会鼓励领域模块自行维护私有codec。未来扩展必须增加versioned typed state kind与正式codec，不能使用opaque bytes逃逸。

### 把State Transaction做成Network Model或Pipeline Pass

Transaction是Numeric Target Kernel内部的单Actor写集；Network Model和Pipeline处理的是多Actor working world、history、restore和外部输出。提升层级会混淆原子边界，因此拒绝。

## Migration

1. 固定旧Bytes StateSlot、Builder、codec和网络引用清单。
2. 定义typed state kind、typed address、partition与新ABI identity。
3. 建立不可变分页Character State和Transaction，不接第二条Kernel路径。
4. 一次切换Kernel Evaluate/Finalize到同一transaction并删除StagedState。
5. 依次迁移Input、Action、Timeline retention、GameplayEffect和Motion transient。
6. 切换State codec、Snapshot、Hash与Network baseline合同。
7. 更新Compiler/Lowerer并正式重建Corin artifacts。
8. 删除Builder、BuildPending、Bytes state kind、领域bytes codec、旧semantic slots和旧ABI读取。
9. 更新active网络change依赖与任务措辞。
10. 静态确认仓库只有一条typed state/transaction/codec链。

## Failure Policy

- Program声明未知typed kind或semantic/kind不匹配：Program Layout创建失败。
- Transaction绑定错误Program/Layout/Actor/Tick：Begin失败。
- 同一Pending被重复Finalize、跨Kernel使用或WorldResult不匹配：Finalize失败并Abort。
- typed savepoint越级恢复或Commit后继续写：当前Step失败。
- Codec遇到旧ABI、旧State version、非canonical payload或非法typed value：Decode失败。
- Snapshot/Network Baseline identity与Program/Layout不匹配：Restore/Merge失败。
- 任一失败不得回退Builder、旧bytes codec、旧Program或默认空state。

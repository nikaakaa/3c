# 角色状态事务迁移清单

## 正式状态链

```text
Committed CharacterSimulationState
-> Float32CharacterStateTransaction
-> Program Evaluate
-> PendingCharacterEvaluation(Transaction + WorldRequest)
-> ResolveBatch(WorldRequest)
-> Program Finalize(Transaction + WorldResult)
-> Transaction Commit
-> Pipeline working world
-> Egress成功后atomic publish
```

`CharacterSimulationState`只保存会影响后续Tick的类型化状态。`MotionContribution`、`Float32MotionAccumulator`、`CharacterMotionRequest`和`PendingWorldRequest`只存在于当前Step，不进入State codec、Snapshot或Network Baseline。

## 删除与替换

| 旧实现 | 正式替换 | 结果 |
|---|---|---|
| `CharacterSimulationStateBuilder`、`ToBuilder()` | `Float32CharacterStateTransaction` | 删除全量StateSlot复制入口 |
| `BuildPending()`、`PendingCharacterEvaluation.StagedState` | Evaluate与Finalize共享同一Transaction | 删除中间Committed State |
| Character State通用`ProgramStateValueKind.Bytes` | 明确typed state kind与`TypedStateAddress` | 旧Bytes kind不再是State ABI合法值 |
| Input request runtime bytes codec | `Float32InputRequestState` | Tick内直接读写typed state |
| Action request/instance/lifecycle/context bytes镜像 | `Float32ActionActivationRequestState`、`Float32ActionInstanceState` | 生命周期归入唯一ActionInstance |
| Timeline保存完整ActionInstance bytes | `Float32ActionInstanceReference` | Timeline只保留最小身份引用 |
| Action target snapshot bytes | `SimulationActionTargetSnapshot` typed value | 仅canonical codec边界编码 |
| GE五份bytes与独立cursor slot | `GameplayEffectStateAggregate` | 单一typed aggregate拥有Tags、Attributes、ActiveEffects、Periods、Journal和cursor |
| GE每Tick Load/Save | 首次写入时创建`SimulationGameplayEffectState` working root | 只读查询复用committed aggregate，业务事务使用typed savepoint；五段writer/reader只存在于`GameplayEffectStateAggregateCodec` |
| MotionAccumulator/PendingWorldRequest committed slots | Evaluation workspace与Pending product | 删除Snapshot/Hash中的同Tick临时状态 |
| Float32 ABI 1、Program artifact 6、State旧codec | Float32 ABI 2、Program artifact 7、`character-state/float32/v3` | 旧产物直接拒绝，不提供reader或migrator |
| 旧Action phase/state/motion-source runtime类型 | `SimulationActionPhase`、`SimulationActionState`与typed action transaction state | 删除无消费者的旧角色Action运行时身份 |
| 旧GE Apply/Remove/Reconcile合同、Attribute mutable DTO、TagContainer | Float32 GameplayEffect aggregate与operation runtime | Authoring层只保留ID、query、modifier和cue配置类型 |
| 未消费的Animation selection batch、RootMotion evaluator、GameplayResult motion node、Pipeline factory/store接口 | Animation command queue、compiled root-motion curve、正式pass factory catalog | 删除无执行者和无调用者的公共表面 |

## 保留的Bytes边界

以下bytes是正式序列化边界，不是运行时状态容器：

- Semantic IR和Float32 Program artifact的canonical payload。
- Program Catalog中的GameplayEffect definition payload。
- Program常量中的不可变Action target snapshot默认值。
- `CharacterSimulationStateCodec`、ActorSnapshot、WorldSnapshot和Network Baseline中的canonical committed state bytes。

## 删除约束

- 不存在旧Program reader、旧State codec版本分支、state migrator或Runtime重新编译fallback。
- Network Model只能保存canonical committed bytes和Program/Layout/codec identity，不能持有Transaction或mutable typed state。
- Fixed Target复用状态语义和事务生命周期，不复用Float32 partition、numeric value、codec或Transaction实现。

## Corin最终产物

- Compiler：`character-simulation-compiler/14`。
- Numeric Target：`float32-ieee754`，ABI `2`。
- Character State codec：`character-state/float32/v3`。
- ProgramHash：`6842f5788b07d0d5c3146994a2c2395334c3d789a6af2b3eec5f688cf5cb031a`。
- LayoutHash：`6a2736b38de94fb76adf69eb5e6005517bf943b5d2744ec2d49004418881de37`。
- SourceRevision：`028b7113a8cd40b983887211afea86e93828e206ad087cb019487238233925d9`。
- Program store与ProgramAsset内嵌canonical bytes完全一致，长度均为`1298353` bytes。
- State Layout包含`793`个slot、`10`个typed partition、`0`个Motion transient slot和`1`个GameplayEffect aggregate slot。
- ProgramAsset与PresentationProjection的ProgramId、ProgramHash和SourceRevision一致。
- `float32-ieee754-abi1.csim`与旧`Library/CharacterSimulation/Program` store已删除。

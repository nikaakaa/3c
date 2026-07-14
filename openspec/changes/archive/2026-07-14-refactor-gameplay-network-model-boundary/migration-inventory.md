## 旧实现职责

| 旧路径/类型 | 真实职责 | 迁移归属 |
|---|---|---|
| `GameplaySyncRuntime` | 当前模型 queue、history、debug、endpoint pump | `ServerAuthoritativeHybridSession` |
| `GameplaySyncPacket`、payload、envelope | 当前模型命令、快照、修正、确认与动作事务合同 | `ServerAuthoritativeHybrid` model |
| `IGameplaySyncPeer` | 当前模型 endpoint 合同 | `IServerAuthoritativeEndpoint` |
| `LocalGameplaySyncLoopbackPeer` | 进程内权威端模拟 | `LocalServerAuthoritativeEndpoint` |
| `FantasyGameplaySyncPeerContract` | 未实现的字符串协议占位 | 删除 |
| `GameplaySyncActorIdentity` | 混合路由和业务参与者字段 | 路由收口为唯一 `SubjectActorId` |
| `CharacterGameplaySyncAdapter` | Character fact 与当前模型 packet 的双向映射 | ServerAuthoritative Character adapter |
| `CharacterGameplaySyncDriver` | 每角色重复持有 model runtime、endpoint 和配置 | 单一 SessionHost + model-owned Character binding |
| `ActionNetworkPolicyResolver`、`BehaviorNetworkPolicyResolver` | 当前模型 policy 解析 | ServerAuthoritative model |

Character、Action、Behavior、Pipeline、GameplayTick 和 Editor 对旧模型的直接依赖已经定位到旧 adapter/driver、receive input、policy resolver、profile inspector、pipeline inspector 与 Agent snapshot/validator。`GameplayAuthorityMode` 的引用位于 GameplayTick context/target、Character host/pipeline/input/frame/graph/motion modifier 和 Sandbox scene serialization。

## Corin 正式迁移输入

### Attack

- Transaction：Prediction `LocalPredicted`，Authority `ServerAuthoritative`，Replication `Broadcast`。
- Window：Hit=`ServerCorrectable/IncludeInCombatHistory/DigestOnly/write`；Cancel=`LocalPredicted/IncludeDigestOnly/OwnerOnly/write`；IFrame=`ServerCorrectable/IncludeDigestOnly/OwnerOnly/write`。
- Motion：RootMotion、MotionWarp、MotionCurve 均为 `LocalPredicted`。
- Cue：Gameplay=`LocalPredicted`，Camera=`LocalOnly`，VFX=`LocalPredicted`。
- Result：`ClientProposal/IncludeInCombatHistory/Broadcast/write`。

### Dodge

- Transaction：Prediction `LocalPredicted`，Authority `ServerAuthoritative`，Replication `Broadcast`。
- Window：IFrame=`ServerCorrectable/IncludeDigestOnly/OwnerOnly/write`。
- Motion：MotionCurve=`LocalPredicted`。
- Cue：无。
- Result：`ServerOnly/None/None/no-write`。

### Behavior

- `Movement.Locomotion.Move`：Stream，Motion，`LocalPredicted/ServerAuthoritative/OwnerOnly`，Snapshot=`ServerSnapshot`，Remote=`RemoteInterpolated`，History=`IncludeDigestOnly`，Command=`EveryTick`。
- `Movement.Correction.Ack`：Stream，Motion，`None/ServerConfirmed/OwnerOnly`，Snapshot/Remote/Command=`None`，History=`IncludeDigestOnly`。
- `State.Effect.Default`：State，StateEffect，`ServerConfirmed/ServerAuthoritative/Broadcast`，Snapshot/Remote/Command=`None`，History=`IncludeInGameplayHistory`。
- Fact binding：ClientCommandFrame -> Locomotion；MotionCorrectionAck -> CorrectionAck；StateEffect -> StateEffect。

## 边界确认

- `add-local-two-client-gameplay-network-closure` 当前为 `0/184`，本 change 完成前不实施。
- BTSMTL Runtime/Editor 不引用旧 GameplaySync packet、runtime、endpoint 或 Fantasy 类型。
- BTSMTL 的 `PipelineBlackboardVariableAuthority.ServerAuthoritative` 是模型中立的变量写入权语义，不读取当前模型 policy，也不参与 packet/endpoint 生命周期。

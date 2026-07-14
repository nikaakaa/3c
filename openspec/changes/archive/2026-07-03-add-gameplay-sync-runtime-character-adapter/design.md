# Design: GameplaySyncRuntime 到 Character Pipeline 的接入边界

## 统一口径

| 名称 | 含义 | 不是什么 |
| --- | --- | --- |
| GameplaySyncRuntime | Gameplay 层最小同步运行时，管理 packet 队列、peer、tick、history、debug | 不是 Fantasy 协议，不 tick Graph |
| GameplaySyncPacket | 使用 SyncDomain、stable id、actor identity 和 policy 的同步数据合同 | 不是 Graph 节点路径 |
| IGameplaySyncPeer | 消费 outgoing packet、产出 incoming packet 的 peer 合同 | 不是角色专属接口 |
| CharacterGameplaySyncAdapter | CharacterPipeline 与 GameplaySyncRuntime 之间的转换层 | 不是 transport，不裁决命中 |
| LocalGameplaySyncLoopbackPeer | 本地调试 peer，模拟延迟、确认、拒绝、校正和快照 | 不是完整权威服务端 |
| FantasyGameplaySyncPeer | 未来 Fantasy adapter，映射 GameplaySyncPacket 与 C2S/S2C 消息 | 不引入第二套 action decision |

## 推荐目录

```text
Assets/GameScripts/Main/Runtime/Gameplay/Sync
  GameplaySyncRuntime
  GameplaySyncPacket
  IGameplaySyncPeer
  GameplaySyncHistory
  LocalGameplaySyncLoopbackPeer

Assets/GameScripts/Main/Runtime/Character/Pipeline/Networking
  CharacterGameplaySyncAdapter
  Character sync packet mapping

Assets/GameScripts/HotFix/GameLogic/Network/Fantasy
  FantasyGameplaySyncPeer
  Fantasy message mapping
```

通用 GameplaySync 放在 `Main/Runtime`，因为 Character、PvE、Objective 都要用。Fantasy adapter 可以后续放在热更层或网络层，但不能反过来污染通用 packet 合同。

## Tick 接法

外部 driver 负责在同一个 `LocalLogicTick` 上驱动 sync runtime 和角色 pipeline：

```text
CharacterPipelineRunner / GameplaySyncDriver
  -> GameplaySyncRuntime.Pump(localLogicTick)
  -> CharacterGameplaySyncAdapter.DrainIncoming(actorId)
  -> CharacterNetworkReceiveStage.Push(...)
  -> CharacterPipeline.LogicTick(context)
       -> NetworkReceiveStage.Collect
       -> InputStage.Update
       -> BTSMTLPhase.Tick
       -> MotionStage.Update
       -> NetworkSendStage.Collect
  -> CharacterGameplaySyncAdapter.CollectOutgoing(actorId)
  -> GameplaySyncRuntime.EnqueueOutgoing(...)
  -> GameplaySyncRuntime.FlushOutgoingToPeer()
```

`CharacterPipeline` 仍然不直接持有 peer。它只暴露 receive/send stage 和 output。

## Packet 合同

Packet 不按角色类名组织，按 SyncDomain 组织：

```text
MotionSyncDomain:
  ClientCommandFrame
  MotionSnapshot
  MotionCorrection
  MotionCorrectionAck

ActionSyncDomain:
  ActionActivation
  ActionEnd
  ActionInstanceDecision
  ActionWindowDigest

GameplayResultSyncDomain:
  GameplayResult
  ResultDigest
  HitResult
  ObjectiveResult
  PvEResult

StateEffectSyncDomain:
  StateSnapshot
  EffectEvent

PresentationSyncDomain:
  CueEvent
```

共同 envelope：

```text
PacketId
SyncDomain
PolicyId
OwnerPlayerId
TeamId
ActorId
ControlledActorId
PerformerActorId
TargetActorId
StableId
PredictionKey
InputSequence
LocalLogicTick
ServerTick
```

`StableId` 的含义按 SyncDomain 解释：

```text
MotionSyncDomain        -> EntityId + Tick/InputSequence
ActionSyncDomain        -> ActionInstanceId
GameplayResultSyncDomain -> GameplayResultId
StateEffectSyncDomain   -> StateId / EffectInstanceId
PresentationSyncDomain  -> CueEventId
```

## Character Adapter 映射

Character outgoing：

```text
NetworkOutput.ClientCommands
  -> MotionSyncDomain.ClientCommandFrame

NetworkOutput.ActionActivationRequests
  -> ActionSyncDomain.ActionActivation

NetworkOutput.ActionEndRequests
  -> ActionSyncDomain.ActionEnd

NetworkOutput.ActionWindowSamples / WindowDigests
  -> ActionSyncDomain.ActionWindowDigest

NetworkOutput.ActionMotionSamples
  -> MotionSyncDomain.MotionSnapshot or Action-scoped MotionDigest

NetworkOutput.ActionCueEvents
  -> PresentationSyncDomain.CueEvent

NetworkOutput.ActionCombatEvents
  -> GameplayResultSyncDomain.GameplayResult
```

`ActionCombatEvents` 是当前代码旧名，实施时必须迁移为 GameplayResult 命名，不保留兼容别名。

Character incoming：

```text
MotionCorrection
  -> CharacterNetworkReceiveStage.Push(Correction)

ActionInstanceDecision
  -> Character network input action decision queue

GameplayResult
  -> Gameplay result input queue

StateSnapshot / EffectEvent
  -> state/effect input queue

CueEvent
  -> presentation cue input queue
```

`ConfirmedEvent(eventId, payload)` 只能作为待清理旧入口，不能作为正式 action decision。

## Local Loopback

Loopback 只模拟同步语义：

```text
收到 ActionActivation
  -> 根据配置生成 ActionInstanceDecision Confirmed / Rejected / Corrected

收到 ClientCommandFrame
  -> 可选生成 MotionCorrection 或 ServerSnapshot

收到 ActionWindowDigest
  -> 可选记录 history/debug

收到 GameplayResult intent/digest
  -> 第一阶段只记录或回显，不做完整命中裁决
```

调试配置：

```text
LatencyLocalLogicTicks
ActionDecisionMode
RejectReason
CorrectionMode
CorrectionOffset
EmitMotionSnapshot
PacketDropRate
DefenseFavorApplied
```

Loopback 不直接访问 `ActionRuntime`、`GraphContext`、`MotionStage`、`PresentationStage` 或 `Transform`。

## Fantasy 之后怎么接

Fantasy 只替换 peer：

```text
GameplaySyncPacket outgoing
  -> FantasyGameplaySyncPeer
  -> C2S message

S2C message
  -> FantasyGameplaySyncPeer
  -> GameplaySyncPacket incoming
  -> GameplaySyncRuntime
```

服务端第一版只需要成为 loopback 的真实版：

```text
C2S_ClientCommandFrame
  -> server actor input intent
  -> S2C_MotionSnapshot / S2C_MotionCorrection

C2S_ActionActivation
  -> action/profile mirror 校验
  -> S2C_ActionInstanceDecision

C2S_ActionWindowDigest
  -> hit/result history validation
  -> S2C_GameplayResult
```

服务端不跑 BTSMTL Graph、不跑 Unity Timeline、不跑 Animancer。

## 设计取舍

### CharacterNetworkPeer

不选择作为正式主抽象。

- 好处：第一版文件少，接角色最快。
- 代价：目标点、PvE、队伍、match event 后续没有自然归属。
- 业务影响：会把 PvPvE 的通用同步问题伪装成角色问题。

### 完整 Fantasy 优先

不选择作为第一步。

- 好处：更早看到真实网络链路。
- 代价：客户端预测、packet 语义、action decision、correction 都还没稳定，直接上服务端会扩大排查面。
- 业务影响：容易把求职 demo 的重点从 Gameplay 客户端拉偏到网络框架工程。

### GameplaySyncRuntime + Character Adapter

选择。

- 好处：Character、PvE、Objective 共用同步合同。
- 好处：本地 loopback 能先把动作手感、确认、拒绝、校正闭环跑通。
- 代价：需要多一层 adapter，并且必须迁移旧 `ActionCombatEvent` / `ConfirmedEvent` 语义。
- 业务影响：最贴合 `Network-aware Third Person Action Combat Demo`，展示的是“混合网络压力下的客户端动作管线”。

## 与当前实现的差距

- 当前没有通用 `GameplaySyncRuntime`。
- 当前没有通用 `IGameplaySyncPeer`。
- 当前 `NetworkOutput` 仍是角色内平铺列表。
- 当前 `CharacterNetworkReceiveStage` 缺少 action decision、gameplay result、state/effect、cue 的正式 incoming queue。
- 当前 `CharacterNetworkSendStage` 暴露 `ActionCombatEvents`，需要迁移到 GameplayResult 命名。
- 当前 `ConfirmedEvent` 是字符串 payload，不能继续作为 action decision 合同。

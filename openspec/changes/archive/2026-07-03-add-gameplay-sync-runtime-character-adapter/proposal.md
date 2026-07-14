# Proposal: 添加 GameplaySyncRuntime 与 Character Pipeline 适配层

## Why

当前问题不是“Character 里缺一个网络 peer”，而是缺一个独立于角色管线的最小 gameplay 同步边界。

`CharacterPipeline` 已经有 `NetworkReceiveStage` 和 `NetworkSendStage`，并且 `ActionRuntime`、`ActionActivationRequest`、`NetworkOutput` 已经开始形成动作事务链路。但 PvPvE 场景不只有角色：

```text
Character
PvE unit
Objective
Projectile
Area effect
Team score
Match phase
```

如果继续把网络模块做成 `CharacterNetworkPeer`，目标点、PvE、队伍结果和局内事件后面都要借角色管线走，网络边界会被角色私有语义绑死。

本变更要把边界抬到通用 gameplay 层：

```text
GameplaySyncRuntime
  -> 通用 packet / peer / tick / history / debug

CharacterGameplaySyncAdapter
  -> CharacterPipeline output/input 与 GameplaySyncRuntime 的映射

LocalGameplaySyncLoopbackPeer
  -> 本地调试 peer，复用同一套 GameplaySync packet 合同
```

Graph、Timeline、ActionRuntime、MotionStage 仍然不认识 transport，也不直接同步 Graph 结构。

## What Changes

本变更替代旧 `add-local-network-loopback-peer` 的角色专属 peer 口径，改为：

- 新增 `gameplay-sync-runtime` 能力：定义通用 `GameplaySyncRuntime`、SyncDomain packet 合同、peer 合同、prediction key、stable id、history 和 debug 边界。
- 新增 `character-gameplay-sync-adapter` 能力：定义 Character Pipeline 如何把 `NetworkReceiveStage` / `NetworkSendStage` 接到 `GameplaySyncRuntime`。
- 新增 `local-gameplay-sync-loopback` 能力：定义本地 loopback peer，消费通用 outgoing packet，并延迟产出 incoming packet。
- 修改 `character-pipeline-runtime`：明确 Character 网络 stage 是 adapter 边界，不是 transport、不是角色专属网络框架。

最小接入链路：

```text
Tick Begin
  -> GameplaySyncRuntime.Pump(localTick)
  -> CharacterGameplaySyncAdapter.DrainIncoming(actorId)
  -> CharacterNetworkReceiveStage.Push(...)
  -> CharacterPipeline.LogicTick(...)
      -> NetworkReceiveStage.Collect
      -> InputStage
      -> BTSMTLPhase
      -> MotionStage
      -> NetworkSendStage.Collect
  -> CharacterGameplaySyncAdapter.CollectOutgoing(actorId, NetworkSendStage)
  -> GameplaySyncRuntime.EnqueueOutgoing(...)
  -> Peer.Send/Pump
Tick End
```

## Current State

- `CharacterPipeline.LogicTick` 中 `NetworkReceiveStage.Collect` 已经位于 `InputStage.Update` 和 `BTSMTLPhase.Tick` 之前。
- `CharacterPipeline.LogicTick` 中 `NetworkSendStage.Collect` 已经位于 `MotionStage.Update` 之后。
- `CharacterNetworkReceiveStage` 当前只支持 `ServerSnapshot`、`ConfirmedEvent` 和 `Correction`。
- `CharacterNetworkSendStage` 当前从 `frame.Output.Network` 收集 client command、action activation、action window、action motion、action cue、`ActionCombatEvent`、window digest 和 correction ack。
- `NetworkOutput` 当前仍是角色内平铺列表，还不是按 `SyncDomain` 分组的 packet 合同。
- `define-character-network-sync-domains` 已经规划 `MotionSyncDomain`、`ActionSyncDomain`、`GameplayResultSyncDomain`、`StateEffectSyncDomain` 和 `PresentationSyncDomain`。
- Goal 文档明确网络是混合架构：连续运动 prediction/reconciliation、离散动作 transaction sync、玩法结果 server authoritative + 局部 rewind、PvE/objective event replication。

## Non-Goals

- 不实现真实 Fantasy 协议、服务端 handler 或协议导出。
- 不实现完整 PvPvE 业务、账号、匹配、房间、背包或完整断线重连。
- 不实现完整 rollback 或全局确定性帧同步。
- 不把 Graph、SubTree、Timeline、NodeModule 或 ActionProfile 变成网络同步单位。
- 不让 loopback、Fantasy adapter 或 GameplaySyncRuntime 直接调用 Graph、ActionRuntime、MotionStage、PresentationStage 或 Unity Transform。
- 不保留 `CharacterNetworkPeer` 作为正式 peer 抽象；角色只保留 adapter/stage。

## Dependencies

- 依赖 `define-character-network-sync-domains`：本变更使用 `SyncDomain + stable id + policy` 作为 packet 语义。
- 依赖 `character-pipeline-runtime`：本变更沿用现有 `NetworkReceiveStage` 前置、`NetworkSendStage` 后置的角色管线位置。
- 依赖 `character-action-activation-flow` 和 `character-action-instance-runtime`：动作同步只使用 `ActionActivationRequest`、`ActionEndRequest`、`ActionInstanceId`、`PredictionKey` 等动作事务身份。
- 替代旧 `add-local-network-loopback-peer` 的角色 peer 方向；后续实现不应再创建 `ICharacterNetworkPeer` 作为正式主抽象。

## Decisions And Tradeoffs

### 方案 A：继续做 Character 专属网络模块

- 优点：短期接入最少，直接服务当前角色管线。
- 缺点：PvE、objective、team score、match event 后续都要绕进 Character，业务边界会被角色污染。
- 业务取舍：适合只做单角色动作测试，不适合 `2v2vE / 2v2 + PvE` 压力展示。

### 方案 B：先完整接 Fantasy 服务端

- 优点：可以更早验证真实 transport、Session、handler 和协议。
- 缺点：当前 packet、SyncDomain、ActionInstance confirm/reject/correct 还在收口，直接服务端会把协议 bug 和管线 bug 混在一起。
- 业务取舍：能证明网络工程能力，但会挤占动作客户端主线，不符合当前求职 demo 第一目标。

### 方案 C：先做 GameplaySyncRuntime + Character Adapter + Local Loopback

- 优点：最小通用网络边界先稳定，Character、PvE、Objective 之后都能接同一套 packet/peer/history/debug。
- 优点：本地 loopback 能先验证预测、拒绝、校正、快照、结果注入，不被真实服务端干扰。
- 缺点：第一版要多做一层 adapter，不能直接把 packets 写在 CharacterNetworkStage 里。
- 业务取舍：最适合当前目标，既不做完整网游，也不会把 PvPvE 压力场景锁死在角色私有实现里。

本 proposal 选择方案 C。

## Spec Comparison

- 与 `character-pipeline-runtime` 不冲突：现有 spec 要求 NetworkStage 不实现真实 transport，本变更仍保持这一点，只让 adapter 把 stage 数据交给通用 `GameplaySyncRuntime`。
- 与 `define-character-network-sync-domains` 一致：本变更不重新定义 SyncDomain，只定义 SyncDomain 如何进入通用 packet/peer/runtime。
- 与 `tengine-hotupdate-foundation` 一致：Fantasy 仍是未来服务端边界；TEngine 不替代 gameplay tick 或网络同步语义。
- 需要清理的旧口径：旧 active change 中的 `CharacterNetworkPeer` 抽象层级过低，已被本 proposal 替换为 `IGameplaySyncPeer` 和 `CharacterGameplaySyncAdapter`。

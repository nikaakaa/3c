# Design: Graph 主体下的网络 SyncDomain 边界

## 核心口径

`SyncDomain` 表示一类 pipeline/runtime 输出的生命周期、仲裁方式和同步方式。中文口径是“同步域”。它不是 Graph port，不是特殊 node，也不是 profile 表。

```text
Graph/BTSMTL
  -> typed output
  -> PipelineOutput SyncDomain
  -> NetworkSendStage policy packing
  -> NetworkPeer / Fantasy adapter
  -> NetworkReceiveStage SyncDomain injection
  -> GraphContext / Runtime / Stage consume
```

Graph 仍然负责“发生什么”，Network 只处理“哪些稳定语义要怎么同步”。

## SyncDomain 定义

### MotionSyncDomain

职责：

- 连续 locomotion、速度、位置、grounded、yaw、motion correction。
- 消费 `MotionIntent`、`MotionContribution`、motion modifier、network correction。
- 由 `CharacterMotionStage` 统一结算最终 `MotionResult`。

稳定键：

```text
EntityId + Tick/InputSequence
```

Action 产生的 root motion、motion warp 或 knockback MAY 携带 `ActionInstanceId` 作为来源归因，但最终仲裁和校正仍属于 MotionSyncDomain。

### ActionSyncDomain

职责：

- 离散动作事务：攻击、翻滚、格挡、受击、交互、支援动作。
- 管理 activation、confirm/reject/cancel/end、window sample、action-scoped cue/motion/result 归因。

稳定键：

```text
ActionInstanceId
```

ActionSyncDomain 不拥有 Graph/Timeline 执行结构，只拥有动作事务身份和该事务的网络策略。

### GameplayResultSyncDomain

职责：

- 命中、伤害、格挡、破防、硬直、受击确认。
- objective ownership、capture/contest、PvE aggro/threat/break、revive/respawn、score/result event。
- 服务端权威或局部 rewind 裁决。

稳定键：

```text
GameplayResultId
```

如果 gameplay result 来自动作窗口，则同时携带 `ActionInstanceId` 和 `WindowId`。

### StateEffectSyncDomain

职责：

- buff、debuff、stun、dead、downed、revive、resource/cooldown 等状态实例。

稳定键：

```text
StateId / EffectInstanceId
```

Action 可以触发状态变化，但状态生命周期不属于 ActionInstance。

### PresentationSyncDomain

职责：

- VFX、SFX、camera shake、hit stop、post-process cue、local animation cue。

稳定键：

```text
CueEventId
```

默认 local-only 或 predicted；只有需要远端一致表现时才复制。Cue 可以可选携带 `ActionInstanceId` 作为来源。

## Graph 如何接

Graph 节点不是“进入同步域”，而是提交 typed output：

```text
Submit MotionIntent        -> MotionSyncDomain
Request ActionActivation   -> ActionSyncDomain
Submit GameplayResult      -> GameplayResultSyncDomain
Submit StateChange         -> StateEffectSyncDomain
Submit CueEvent            -> PresentationSyncDomain
```

SubTree、StateNode、TimelineNode、NodeModule 只负责组织和执行，不是同步身份。

## Action 输出归属

Action 需要额外约束，因为一个离散动作会横跨多个 SyncDomain：

```text
Attack.Light.01
  -> animation / cue
  -> root motion / motion warp
  -> hit window
  -> gameplay result
  -> cancel window
```

这些输出不能靠 Graph 结构归属，也不能靠 ambient current active action 偷读。它们需要显式 action context：

```text
ActionActivationRequest
  -> ActionRuntime 接受
  -> ActionInstance / ActionInstanceHandle
  -> TimelinePlaybackRequest 或 output submit 显式携带该 context
  -> 输出写入 ActionInstanceId
```

第一阶段可以选择实现为：

- `ActionInstanceHandle` value type + `PropertyPort<ActionInstanceHandle>`。
- 或 `ActionRuntimeContext` 显式参数，只传给 Timeline playback request 和 output submit API。

不论实现细节，spec 约束是：动作输出归属必须来自显式 context，不来自静态结构 membership，也不来自隐式全局 current active。

## NetworkSendStage

`NetworkSendStage` 读取 `NetworkOutput` 或后续 SyncDomain output，并根据 policy 打包：

```text
MotionSyncDomain:
  ClientCommand / MotionSnapshot / MotionCorrectionAck

ActionSyncDomain:
  ActionActivationPacket / ActionEndPacket / ActionInstanceDecisionAck / WindowDigest

GameplayResultSyncDomain:
  GameplayResultPacket / ResultDigest

StateEffectSyncDomain:
  StateSnapshot / EffectEvent

PresentationSyncDomain:
  CueEventPacket, only if policy requires replication
```

打包规则 MUST NOT 读取 Graph 节点路径、SubTree membership、Timeline clip membership。

## NetworkReceiveStage

`NetworkReceiveStage` 按 SyncDomain 注入：

```text
Motion correction
  -> MotionSyncDomain correction queue / input history alignment

Action decision
  -> ActionRuntime confirm/reject/correct/end

Gameplay result
  -> GameplayResultSyncDomain result queue / presentation feedback

State/effect event
  -> StateEffectSyncDomain state cache

Remote cue
  -> PresentationSyncDomain cue queue
```

NetworkReceiveStage 不直接 tick Graph、不改 Transform、不调用 TimelinePlayer、不播放表现。

## Policy

Policy 不挂在 SyncDomain 本身，而是挂在同步域内的业务配置上：

```text
ActionProfile
  -> ActionSyncDomain policy
  -> action window/motion/cue policy

MotionSyncPolicy
  -> MotionSyncDomain prediction/correction policy

GameplayResultPolicy
  -> GameplayResultSyncDomain authority/rewind policy

StateEffectPolicy
  -> StateEffectSyncDomain replication policy

PresentationCuePolicy
  -> PresentationSyncDomain local/predicted/replicated policy
```

第一阶段可以只正式化 ActionProfile，并预留其它 policy resolver 位置。

## 为什么不是完整 rollback

项目口径是不做全局确定性帧同步、不做完整 rollback。History 是按 policy 使用：

- `ClientPredicted Action` 记录 activation、prediction key、input sequence、output digest 和 decision。
- `MotionSyncDomain` 记录 input history 和 correction base。
- `ServerAuthoritative` 只记录 intent 和 server decision。
- `LocalOnly/None` 可以只保留 debug，不进入网络 history。

## 与 UE CMC/GAS 的对应

- CMC 启发 MotionSyncDomain：连续运动按 input/tick/state 预测和校正。
- GAS 启发 ActionSyncDomain：离散动作按 activation、prediction key、confirm/reject/cancel/end 事务同步。

本项目不照搬 GAS。Graph 仍是编排主体，ActionSyncDomain 只提供动作事务身份和同步边界。

## 风险

- 如果 SyncDomain 过早做成编辑器概念，会污染 Graph authoring；因此 SyncDomain 必须先是 runtime contract。
- 如果 ActionInstanceHandle 线在 Graph 上到处拉，会退化成更糟的 SubTree 方案；因此 Graph/SubTree 可以组织动作内容，但运行时归属仍必须是显式 context。
- 如果 `NetworkOutput` 继续只是平铺列表，后续 packet 组装会重复写规则；因此 NetworkSendStage 需要统一 SyncDomain grouping/policy resolver。

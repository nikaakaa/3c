# Design

## 问题本质

当前已经解决的是“网络 packet 通过哪个 peer 进出”的代码边界，尚未解决的是“作者如何明确选择当前角色使用哪个同步后端”。现在唯一 Unity 组件叫 `CharacterGameplaySyncLoopbackDriver`，这会把本地模拟工具误读成网络同步主入口。

这个 change 要补的是装配层语义，不是网络协议。

## 作者心智模型

作者在角色对象上配置：

- `CharacterPipelineHost`：角色管线本体。
- `CharacterGameplaySyncDriver`：该角色是否接入 gameplay sync backend，以及接入哪个 backend。

作者不需要在 Graph、Timeline、ActionProfile 或 NetworkStage 里选择 loopback/Fantasy。Graph 和 Timeline 仍然只产出 `SyncFacts`；adapter 仍然只把 `SyncFacts` 映射为 `GameplaySyncPacket`；driver 只负责把 packet 交给当前 backend。

## 运行链路

```text
CharacterPipeline
  -> CharacterNetworkSendStage
  -> CharacterGameplaySyncAdapter
  -> GameplaySyncRuntime
  -> IGameplaySyncPeer
       None: null peer
       LocalLoopback: LocalGameplaySyncLoopbackPeer
       Future: FantasyGameplaySyncPeer
```

incoming 方向反过来：

```text
IGameplaySyncPeer
  -> GameplaySyncRuntime
  -> CharacterGameplaySyncAdapter
  -> CharacterNetworkReceiveStage
  -> CharacterPipeline
```

## 后端模式

### None

`None` 是正式关闭同步后端的模式。它不是 fallback。它表达“这个角色当前只运行本地 pipeline，不把 `SyncFacts` 交给任何外部同步后端”。

业务取舍：

- 优点：单机调试和纯表现角色不需要伪造网络对象。
- 优点：不会为了不开网络而删除组件或依赖空引用。
- 缺点：如果作者以为它会保存网络 debug，就会看不到 peer pending/incoming 记录，因此 Inspector 必须明确显示 backend 为 None。

### LocalLoopback

`LocalLoopback` 是本地网络模拟后端。它复用现有 `LocalGameplaySyncLoopbackPeer`，用于延迟确认、拒绝、运动校正、快照和 result 回显。

业务取舍：

- 优点：可以在不启动服务端的情况下调试预测动作手感、防守优先、校正和 debug 链路。
- 优点：完全复用 `IGameplaySyncPeer`，不会绕过正式 packet/adapter。
- 缺点：它不是权威服务端，不能证明 hit validation、objective、PvE threat 或真实延迟下的可靠性。

### Future Fantasy

Fantasy 后端不在本 change 中实现。原因不是 Fantasy 不重要，而是当前还缺协议、Session 生命周期、服务端裁决切片和端到端连接目标。现在加入不可用 Fantasy mode 会制造假闭环。

业务取舍：

- 优点：先把客户端管线和本地模拟闭环稳定，后续真实网络只替换 peer。
- 缺点：暂时不能暴露真实 Fantasy 网络问题。

## 为什么不把 backend 配进 Graph 或 ActionProfile

Graph 表达行为编排，ActionProfile 表达动作策略，Timeline 表达时间窗口和表现输出。backend selection 是运行环境装配问题，不是动作语义。把 backend 放进 Graph 或 ActionProfile 会让同一个动作资产在本地、联机、压测场景中产生不同资产副本，违背当前统一数据链路。

## 为什么不继续保留 LoopbackDriver

保留旧 driver 作为兼容会产生两个入口：

- `CharacterGameplaySyncLoopbackDriver`
- `CharacterGameplaySyncDriver`

这会让后续实现时无法判断哪个组件才是正式网络装配点。因此实现阶段应激进迁移：新 driver 覆盖旧职责，旧 loopback driver 类型删除或改名，不保留兼容入口。

## 与 Action 网络策略提案的边界

`add-action-network-policy-authoring-closure` 负责回答“一个动作输出是否预测、复制、记录、校正”。本 change 负责回答“这些已经被 adapter 映射出来的 packet 交给哪个后端”。两者不要合并：

- 策略决定是否产生 packet。
- backend selection 决定 packet 去哪里。

## 后续扩展

真实 Fantasy 接入应作为后续 change：

- 新增 `FantasyGameplaySyncPeer`。
- 映射 `FantasyGameplaySyncPeerContract` 中的 C2S/S2C 消息。
- 通过 Fantasy Unity Session 发送和接收。
- 保持 `CharacterGameplaySyncDriver`、`GameplaySyncRuntime`、`CharacterGameplaySyncAdapter` 的主接口不变。

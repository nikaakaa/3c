# Add Gameplay Sync Backend Selection

## Why

当前角色网络同步底座已经拆出 `GameplaySyncRuntime`、`IGameplaySyncPeer`、`CharacterGameplaySyncAdapter` 和 `LocalGameplaySyncLoopbackPeer`。代码层面已经能通过 `GameplaySyncRuntime.SetPeer()` 替换 peer，但 Unity 装配层仍然只有 `CharacterGameplaySyncLoopbackDriver`，Inspector 和调试入口也只认识 loopback driver。

这会造成作者心智污染：场景里看到的是 loopback driver，而不是“角色选择一个 gameplay sync backend”。之后接 Fantasy 时，如果继续在这个组件上堆字段，会让本地模拟、真实网络、单机关闭网络混在一个 loopback 语义里。

## 目标

- 增加正式的 gameplay sync backend selection 语义。
- 将 `CharacterGameplaySyncLoopbackDriver` 收口为后端无关的 `CharacterGameplaySyncDriver` 或等价正式组件。
- 第一阶段只提供正式 `None` 和 `LocalLoopback` 后端。
- 保持 `CharacterPipeline`、`CharacterNetworkSendStage`、`CharacterNetworkReceiveStage` 和 `CharacterGameplaySyncAdapter` 不直接依赖具体 peer。
- 保留未来 `FantasyGameplaySyncPeer` 接入位置，但本 change 不实现真实 Fantasy 网络、协议、Handler 或服务端裁决。

## 非目标

- 不实现 Fantasy 客户端 peer。
- 不新增 Fantasy `.proto`、Handler、Session 发送或服务端 ECS 逻辑。
- 不实现真实 server authority、断线重连、协议序列化或登录/房间流程。
- 不改变 ActionProfile 网络策略作者入口；该范围归属 `add-action-network-policy-authoring-closure`。
- 不改变 `SyncFacts`、SyncDomain 或 CharacterPipeline 的主运行语义。

## 当前缺口

- `IGameplaySyncPeer` 已经是可替换接口，但缺少正式 backend 枚举或配置入口。
- `GameplaySyncRuntime` 已经允许 `SetPeer(null)`，但 Unity 组件没有把“无网络/单机”表达为作者可见的正式模式。
- `LocalGameplaySyncLoopbackPeer` 是 peer，但当前 `CharacterGameplaySyncLoopbackDriver` 同时承担 Unity 装配、tick hook、identity 配置、adapter 调用和 loopback 创建，职责过窄且命名误导。
- `CharacterPipelineHostEditor` 目前直接查找 `CharacterGameplaySyncLoopbackDriver` 展示网络 debug，导致 editor debug 也绑死 loopback 命名。
- `FantasyGameplaySyncPeerContract` 只有协议映射意图，不是可用 peer。

## 方案

引入一个正式的 Character gameplay sync driver 作为 Unity 装配点。该 driver 持有 actor identity、backend mode、backend-specific settings 和 `GameplaySyncRuntime`，在 tick hook 中继续执行现有顺序：

1. `BeforeLogicTick`：pump runtime，然后用 adapter 将 incoming packet 注入 `CharacterNetworkReceiveStage`。
2. CharacterPipeline 正常 tick。
3. `AfterLogicTick`：adapter 从 `CharacterNetworkSendStage` 收集 outgoing packet，然后 flush 给当前 peer。

backend 第一阶段只包含：

- `None`：不创建 peer，`GameplaySyncRuntime.SetPeer(null)`，保留本地 pipeline 和 `SyncFacts`，但不同步到外部后端。
- `LocalLoopback`：创建 `LocalGameplaySyncLoopbackPeer`，复用现有本地延迟、确认、拒绝、校正、快照和 debug 能力。

未来 `Fantasy` 后端只作为显式扩展点，不在本 change 中出现可选但不可用的假配置。等真实 Fantasy peer、协议和连接生命周期准备好后，再新增一个独立 change。

## 影响

- 场景作者看到的组件语义从“LoopbackDriver”变成“GameplaySyncDriver”。
- 单机关闭网络成为正式配置，而不是不挂组件或利用空 peer 的隐式行为。
- 本地网络模拟仍然可用，但 loopback 只作为 backend 实现存在。
- 后续真实网络只需要新增 peer factory/backend 分支，不需要改 CharacterPipeline 和 adapter 主线。

## 与现行 spec 对比

- 符合 `gameplay-sync-runtime`：peer 是通用合同，Fantasy peer 未来替换 loopback 时不替换 gameplay 语义。
- 符合 `local-gameplay-sync-loopback`：loopback 只作为本地调试 peer，不直接修改 CharacterPipeline 或 Unity Transform。
- 符合 `character-gameplay-sync-adapter`：角色管线通过 adapter 接入 GameplaySyncRuntime，不直接持有 peer、Fantasy Session 或 transport。
- 符合 `character-pipeline-runtime`：NetworkStage 是正式边界但不实现真实 transport。
- 与 `add-action-network-policy-authoring-closure` 不重叠：本 change 不做 ActionProfile 策略编辑和 resolver，只处理 backend 装配。

## 风险

- 如果第一阶段暴露 `Fantasy` 选项但不实现，会形成假配置和误导路径，因此本 change 不加入 Fantasy mode。
- 如果保留旧 `CharacterGameplaySyncLoopbackDriver` 作为兼容组件，会形成双入口，因此实现阶段应迁移命名并删除旧 loopback driver 类型。
- 如果把 backend settings 放进 CharacterPipelineDefinition，会把场景网络调试和角色资产绑定过深。第一阶段应优先让 backend selection 归属 Unity 装配组件。

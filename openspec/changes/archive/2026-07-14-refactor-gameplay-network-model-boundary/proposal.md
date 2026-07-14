# Change: 重构 Gameplay 网络模型边界

## Why

当前代码只真正抽象了 `IGameplaySyncPeer`，但名为通用的 `GameplaySyncRuntime`、`GameplaySyncPacket`、History、Debug 和 Character adapter 已经直接保存 `ClientCommandFrame`、`MotionSnapshot`、`MotionCorrection`、`ActionDecision` 等“Owner 本地预测、服务端裁决、Remote 快照”的模型语义。`SetPeer()` 只能替换 Loopback/Fantasy 等收发端，不能替换同步模型。

模型语义还继续进入 `GameplayAuthorityMode.LocalPredicted/RemoteProxy`、`ActionProfile`、`GameplayBehaviorProfile`、`ActionContext`、`CharacterNetworkReceiveStage` 和 `CharacterPipelineDefinition`。如果直接实施 `add-local-two-client-gameplay-network-closure`，这些语义会进一步进入 Fantasy 协议、Session 和服务端 Handler，之后再增加其它同步模型只能在 CharacterPipeline 内堆分支，或者复制第二套角色管线。

本 change 先把现有模型诚实地收口为 `ServerAuthoritativeHybrid`：Gameplay/BTSMTL 只保留动作身份、输入、运行事实和角色语义；服务端权威模型独占 packet、history、prediction/correction、snapshot、replication 和网络策略。它建立模型级装配边界，但不实现 Rollback，也不暴露尚不存在的模型选项。

## What Changes

- **BREAKING** 建立 Session 级 `GameplayNetworkModelDefinition` / runtime 装配边界；一个 Session 启动前只绑定一个完整模型，运行中、角色级、Graph 级和动作级均不得切换模型。
- **BREAKING** 将当前通用命名的 `GameplaySyncRuntime`、`GameplaySyncPacket`、`IGameplaySyncPeer`、History、Debug、payload 和 Loopback 实现迁移并重命名为 `ServerAuthoritativeHybrid` 模型内部合同；删除旧通用类型，不保留别名或包装层。
- 将“网络模型”“模型 endpoint”“底层 transport”分开：当前唯一模型是 `ServerAuthoritativeHybrid`；未引用 EndpointDefinition 表示明确断开，LocalLoopback 由独立 EndpointDefinition 创建；未来 Fantasy 是同一模型的真实 endpoint，不是第二个网络模型。
- 将 per-character `CharacterGameplaySyncDriver` 迁移为 SessionHost + model-owned Character binding；Runtime、endpoint、queue、history 和 debug 由 Session 唯一持有，角色只提交/消费自身的稳定 actor 端口。
- 让 CharacterPipeline 只暴露 `CharacterInputFrame`、resolved motion result、Action lifecycle、window、GameplayResult、StateEffect、Cue 和 correction application result 等 gameplay/runtime 事实；`MotionCommand`、CorrectionAck、Snapshot packet 和 ActionDecision packet 由 `ServerAuthoritativeHybrid` adapter 构造或转换。
- 用互相独立的 `CharacterInputSource` 与 `CharacterMotionAuthority` 取代把输入所有权、运动结算和网络预测混在一起的 `GameplayAuthorityMode`。服务端权威 Owner 使用 LocalDevice + LocalSolver；后续 RemoteProxy 使用 ExternalFacts + ExternalPose；未来其它模型可以组合不同来源而不修改 CharacterPipeline 分支。
- 从 `ActionProfile`、`GameplayBehaviorProfile` 和 `ActionContext` 删除 prediction、authority、replication、history、snapshot、command send、window/motion/cue/result 网络策略；动作资产只保留 gameplay 身份、tags、block/cancel、target 等动作语义。
- 新增模型专属 `ServerAuthoritativeCharacterSyncProfile`，按稳定 `BehaviorId/ActionId` 唯一保存 Stream、Transaction、State、Event 的服务端权威同步策略；Character binding 引用该 profile，模型 resolver 和 Inspector 只从这里解析。
- 将 `CharacterGameplaySyncAdapter`、`BehaviorNetworkPolicyResolver`、`ActionNetworkPolicyResolver` 和 packet preview 迁移为 ServerAuthoritative 专属 adapter/resolver/preview；Graph、Timeline、BTSMTL Editor 和 Agent Patch 不获得网络模型写入口。
- 迁移 Corin 的 Attack、Dodge、Locomotion、CorrectionAck、StateEffect 网络策略和 Sandbox 装配到唯一 ServerAuthoritative profile + SessionHost；删除 Action/Behavior 资产中的旧序列化网络字段和场景中的 per-character backend ownership。
- 把 `add-local-two-client-gameplay-network-closure` 声明为后继 change；在本 change 完成前不得 apply，完成后必须按新的模型、endpoint、policy 和 binding 名称重写其 proposal/design/tasks/spec deltas。

## Scope

本 change 完成后的可见运行能力保持为单机 CharacterPipeline 与断开/LocalLoopback 调试闭环。它只重构 ownership、命名、策略归属和数据边界，不增加真实 Fantasy 连接、第二客户端、RemoteProxy roster、服务端 Room、命中伤害或 Rollback。

正式链路收口为：

```text
Character Input / Graph / Timeline / Motion / Action
  -> model-neutral Character facts and semantic inputs
  -> CharacterServerAuthoritativeAdapter
  -> ServerAuthoritativeHybridSession
  -> disconnected or LocalLoopback EndpointDefinition
  -> exact actor incoming
  -> semantic Character external input
```

## Non-Goals

- 不实现确定性 lockstep、Rollback、Snapshot/Restore、Replay、Checksum 或确定性 KCC。
- 不新增 `Rollback` 枚举、空 factory、空 runtime、占位 profile 或不可用 Inspector 选项。
- 不实现 Fantasy Session、协议生成、服务端 Handler、双客户端 roster 或 RemoteProxy 表现插值。
- 不把 BTSMTL Graph、Timeline、Blackboard 或 Animation 改成网络模型配置入口。
- 不建立同时兼容旧 `GameplaySync*` 与新 `ServerAuthoritative*` 的桥接层。
- 不新增测试或人工验证任务，不运行 Unity batchmode。

## Current Spec Comparison

- `gameplay-sync-runtime` 当前把带 MotionCorrection、Snapshot 和 ActionDecision 的 Runtime/Packet/Peer称为“通用”；本 change 删除该错误口径，改为 model-neutral Session composition + model-owned runtime。
- `gameplay-sync-backend-selection` 当前把 `None/LocalLoopback/Fantasy` 视为同一层 backend；本 change 删除 backend enum：断开由未配置 endpoint 表达，LocalLoopback/Fantasy 由各自 EndpointDefinition 表达，network model selection 是更上层且只能选择已完整实现的模型。
- `character-gameplay-sync-adapter` 当前直接把 Character facts 映射为通用 GameplaySync packet；本 change 将其改为 ServerAuthoritative 专属 adapter，并禁止 model packet payload 进入 CharacterPipeline。
- `character-network-sync-domain-contract` 当前要求 NetworkSendStage 生成 ClientCommandFrame/packet，并让 NetworkReceiveStage 接收 snapshot/decision/correction packet；本 change 改为 stage 只暴露事实和语义输入，由模型 adapter 负责 wire contract。
- `gameplay-behavior-policy-model`、`character-action-network-policy-authoring`、`character-syncfact-behavior-binding` 和 `character-action-authoring-closure` 当前把服务端权威策略放在 GameplayBehaviorProfile/ActionProfile；本 change 把这些字段迁移到唯一模型专属 profile，保留稳定 BehaviorId/ActionId 作为引用。
- `local-gameplay-sync-loopback` 当前把 Loopback 和未来 Fantasy 描述为“通用 peer”；本 change 将两者改为同一 ServerAuthoritative 模型的 endpoint 实现。
- `character-pipeline-runtime` 当前的 `SyncFacts` 已明确不是 packet，可继续作为模型输入；但 `GameplayAuthorityMode` 和 network receive payload 仍与模型耦合，本 change 进一步拆成输入来源、运动权威和语义外部输入。
- `btsmtl-graph-data-catalog-authoring`、`btsmtl-runnable-timeline-node`、`character-action-activation-flow` 和 `character-pipeline-blackboard` 仍把 ActionWindow 的完整网络策略写成由 ActionProfile 解析；本 change 保留 WindowType、WindowId、Digest、Action Context 与事实投影语义，只把 effective policy owner 改为当前 Network Model profile。
- active `add-local-two-client-gameplay-network-closure` 当前会继续扩张旧通用 `GameplaySync*` 命名和 policy ownership，与本 change 冲突。两者必须串行，本 change 在前，后继 change 文档必须重写后才能 apply。

## Impact

- 新能力：`gameplay-network-model-boundary`、`server-authoritative-hybrid-sync-model`。
- 受影响规范：`gameplay-sync-runtime`、`gameplay-sync-backend-selection`、`local-gameplay-sync-loopback`、`character-gameplay-sync-adapter`、`character-network-sync-domain-contract`、`gameplay-behavior-policy-model`、`character-action-network-policy-authoring`、`character-syncfact-behavior-binding`、`character-action-authoring-closure`、`character-pipeline-runtime`、`btsmtl-graph-data-catalog-authoring`、`btsmtl-runnable-timeline-node`、`character-action-activation-flow`、`character-pipeline-blackboard`。
- 运行时代码：`Runtime/Networking/GameplaySync`、Character Action/Behavior policy、Character Pipeline Input/Motion/Network/Runtime/Unity、Gameplay tick role、Runtime Debug。
- Editor/Agent：ActionProfile Inspector、GameplayBehaviorProfile Inspector、CharacterPipelineDefinition Inspector、ServerAuthoritative profile Inspector、Agent snapshot/validator 的网络只读投影。
- 资产：Corin ActionProfile、BehaviorProfile、CharacterPipelineDefinition、Sandbox 场景和新的 ServerAuthoritative Character Sync Profile。
- 后继 change：`add-local-two-client-gameplay-network-closure` 必须改为“扩展 ServerAuthoritativeHybrid 的 Fantasy endpoint 与双客户端纵切”，不得重新创建通用 GameplaySync 模型。

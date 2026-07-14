## Context

当前调用链是：

```text
CharacterPipeline
  -> CharacterNetworkSendStage
  -> CharacterGameplaySyncAdapter
  -> GameplaySyncRuntime
  -> IGameplaySyncPeer
```

这条链看似分层，但真正可替换的只有最后一个 Peer。`GameplaySyncPacket` 直接拥有 MotionCorrection、MotionSnapshot、ActionDecision 等 payload；`GameplayBehaviorProfile` 和 `ActionProfile` 直接保存 LocalPredicted、ServerAuthoritative、Snapshot、Replication 与 History；`GameplayAuthorityMode` 又让 CharacterPipeline 根据 LocalPredicted/RemoteProxy 改变输入和运动行为。模型没有边界，只是分散在多个模块中。

本 change 不尝试设计一个可以证明适配所有未来模型的万能 API。它先完成两件可验证的工作：

1. Gameplay/Character runtime 不再依赖当前模型 packet 和 policy 类型。
2. 当前模型的所有语义进入一个明确命名、明确 ownership 的 `ServerAuthoritativeHybrid` 模块。

## Terms

- **Gameplay Fact**：角色本 tick 已发生的输入、resolved motion、Action lifecycle、window、result、state 或 cue 事实。它不是 packet，也不决定如何联网。
- **Network Model**：决定谁运行模拟、发送什么、保存什么历史、如何确认、如何修正以及 Remote actor 如何推进的一整套同步方法。
- **Model Session**：某个网络模型在一个客户端 Session 内的唯一运行实例，拥有 actor bindings、queue、history、debug 和 tick coordination。
- **Endpoint**：模型消息的另一端实现。LocalLoopback 在进程内模拟服务端权威；Fantasy endpoint 将同一模型消息映射到真实 Session。
- **Transport**：KCP、Fantasy Session、连接、心跳和字节收发。Transport 不解释 MotionCommand 或 ActionDecision 的 gameplay 含义。
- **Model Policy**：某个 Behavior/Action 在指定网络模型下的 prediction、authority、replication、history、snapshot 和 send 规则。
- **Character Input Source**：角色输入来自 LocalDevice、ExternalFacts 或 None。
- **Character Motion Authority**：角色逻辑位姿由 LocalSolver、ExternalPose 或 None 结算。

## Target Chain

```text
BTSMTL / Character Gameplay
  CharacterInputFrame
  ResolvedMotionFact
  ActionLifecycleFact
  Window / Result / State / Cue
            |
            v
CharacterServerAuthoritativeAdapter
  + ServerAuthoritativeCharacterSyncProfile
            |
            v
ServerAuthoritativeHybridSession
  packet / history / correction / snapshot / debug
            |
            v
ServerAuthoritativeEndpoint
  disconnected | LocalLoopback definition | future Fantasy definition
            |
            v
Character semantic external input
  ActionLifecycleTransition
  ExternalPoseCorrection
  ExternalPoseSample
  GameplayResult / State / Cue
```

未来其它模型只能在 Session composition 层增加完整实现：

```text
GameplayNetworkSessionHost
  -> ServerAuthoritativeHybridSession
  OR
  -> future CompleteRollbackSession
```

它不能通过 CharacterPipeline 内的 model switch 与现有模型混跑。

## Decisions

### 1. 一个 Session 只能装配一个完整模型

`GameplayNetworkSessionHost` 只持有一个显式 `GameplayNetworkModelDefinition`。Definition 负责创建自己的 Model Session、校验 model-specific 配置并提供 model identity。SessionHost 只管理生命周期和唯一 ownership，不解释 packet、prediction、snapshot 或 correction。

模型在连接和 actor binding 前锁定；运行中修改、每角色选择、每动作选择和 Graph 节点选择全部拒绝。Inspector 只枚举真正存在且配置完整的 Definition 类型。当前只有 `ServerAuthoritativeHybrid`，因此不出现 Rollback 选项。

选择 Definition asset 而不是扩张 enum，是为了让后续模型由独立模块提供自己的配置和 factory，不要求修改 CharacterPipeline 的 switch。代价是必须严格限制 common Definition API，不能把所有模型参数塞进基类。

### 2. 当前 Runtime、Packet 和 Peer 全部归属 ServerAuthoritativeHybrid

以下通用命名删除并迁移：

```text
GameplaySyncRuntime       -> ServerAuthoritativeHybridSession
GameplaySyncPacket        -> ServerAuthoritativePacket
IGameplaySyncPeer         -> IServerAuthoritativeEndpoint
GameplaySyncHistory       -> ServerAuthoritativeHistory
GameplaySyncRuntimeDebug  -> ServerAuthoritativeDebug
LocalGameplaySyncLoopbackPeer -> LocalServerAuthoritativeEndpoint
```

Packet 的具体结构不在本 change 中重新发明。当前 union/envelope 可以在模型内部先保留，但它不再宣称 transport-neutral 或 model-neutral。后继 Fantasy change 可以在该模型边界内将 MotionCommand 映射为生成协议。

### 3. CharacterPipeline 输出事实，模型 Adapter 构造命令

CharacterPipeline 不再生成 `ClientCommandFrame`、`ServerAuthoritativeMotionCommand` 或 packet。MotionStage 在完成本 tick resolve 后输出 `ResolvedCharacterMotionFact`，包含 input sequence、logic tick、实际位移/转角、最终逻辑位姿、grounded 和移动摘要。InputStage 继续拥有 `CharacterInputFrame`。

ServerAuthoritative adapter 根据当前模型 policy 选择事实并构造 MotionCommand。未来 Rollback 模型若实现，将从 canonical input 入口读取输入，而不是复用 MotionCommand。两者共用的是 gameplay 输入/结果，不是 wire payload。

CorrectionAck 同理：CharacterMotionStage 只输出 `MotionCorrectionApplicationResult`；ServerAuthoritative adapter 在确认成功应用后构造 model ack。Snapshot/ActionDecision packet 先由模型 adapter 转成 Character 已有的 external pose 或 `ActionLifecycleTransition` 语义输入，再进入 Pipeline。PredictionKey、AuthorityTick 和 defense-favor 等裁决元数据留在模型 packet/history/debug 内，不再伪装成 Character 通用 DTO。

### 4. 输入来源和运动权威必须分开

`GameplayAuthorityMode` 当前同时回答“谁提供输入”“谁结算位姿”“是否只表现”，导致 RemoteProxy/LocalPredicted 成为 Character 和 GameplayTickSystem 的基础枚举。它被删除，改为两个正交配置：

```text
CharacterInputSource
  LocalDevice
  ExternalFacts
  None

CharacterMotionAuthority
  LocalSolver
  ExternalPose
  None
```

组合语义：

| 业务角色 | InputSource | MotionAuthority |
|---|---|---|
| 单机/服务端权威 Owner | LocalDevice | LocalSolver |
| 后续服务端权威 RemoteProxy | ExternalFacts | ExternalPose |
| 后续 Rollback 本地 Owner | LocalDevice | LocalSolver |
| 后续 Rollback 远端 Actor | ExternalFacts | LocalSolver |
| 纯展示对象 | None | None |

GameplayTickSystem 只调度 target，不认识 LocalPredicted、RemoteProxy 或网络模型。CharacterPipeline 根据输入来源和运动权威决定启用 Input System、MotionStage 与外部 pose，不根据 model id 分支。

### 5. Gameplay Action/Behavior 与模型网络策略分离

`ActionProfile` 继续唯一拥有 ActionId、显示、tags、block/cancel 和 target 等 gameplay 语义。`GameplayBehaviorProfile` 迁移为只保存 BehaviorId、BehaviorKind、显示和 tags 的 gameplay identity 定义。`ActionContext` 不再携带 prediction/authority/replication。

新增 `ServerAuthoritativeCharacterSyncProfile`，由 ServerAuthoritative Character binding 唯一引用，内容包括：

- 按 BehaviorId 保存 Stream/State/Event 模型策略。
- 按 ActionId 保存 Transaction、window、motion、cue 和 gameplay result 模型策略。
- 保存 SyncFact kind 到 BehaviorId 的正式绑定。
- 校验引用的 ActionId/BehaviorId 必须存在于对应 CharacterPipelineDefinition。

这不是两份动作数据：ActionProfile 只回答“动作是什么”，model profile 只回答“该动作在 ServerAuthoritativeHybrid 下如何同步”。两边只通过稳定 ID 引用，不复制 tags、Timeline、Graph、Motion curve 或动画资源。

### 6. Policy Resolver 和 Editor 归属模型模块

`BehaviorNetworkPolicyResolver`、`ActionNetworkPolicyResolver`、effective packet policy 和 packet preview 全部迁移为 `ServerAuthoritative*` 命名并只读取模型 profile。ActionProfile Inspector 删除 Network、Windows network policy、Motion network policy、Cue network policy 和 packet preview；ServerAuthoritative profile Inspector 成为这些字段的唯一作者入口。

Graph、Timeline、Blackboard projection 和 Agent Patch 继续只声明 ActionId、BehaviorId、WindowType、Motion source 和 CueType，不写 model policy。需要只读追踪时，Character host/模型 profile Inspector 可以显示 identity 是否有匹配 policy。

模型 profile 保留逐 Behavior/Action 的显式正式策略，不在本 change 引入会隐式推导 packet/history 的预设。原因是 Corin 现有 Attack、Dodge、Locomotion、CorrectionAck、StateEffect 的 window、motion、cue、result 策略并不相同，当前没有经过业务批准的少量模式可以无损表达这些差异。Inspector 通过分组、coverage 校验和 effective packet mapping 降低读取成本；如果后续要增加“本地预测动作”等预设，必须先定义预设覆盖哪些事实和允许哪些 override，再以独立 change 收口，而不是把默认值当 fallback。

### 7. EndpointDefinition 是模型扩展边界

`ServerAuthoritativeHybridModelDefinition` 只引用一个模型专属 `ServerAuthoritativeEndpointDefinition`。Definition 自己创建 endpoint；LocalLoopback 使用独立 definition，未来 Fantasy 新增自己的 definition，不修改模型核心的 enum 或 switch。未引用 definition 表示明确 disconnected，Session 仍可消费本地角色事实并将未发送 packet 记录为 dropped，不自动创建其它 endpoint。

LocalLoopback 消费 ServerAuthoritativePacket，并按设置产生 Confirm/Reject/Correction/Snapshot。它模拟的是当前模型中的“服务端一侧”，因此归属 ServerAuthoritativeHybrid 模块。

未来 Fantasy endpoint 也实现同一模型 endpoint 合同，但内部使用 Fantasy Session 和生成协议。两者可替换的原因是模型相同，不代表网络模型可替换。Fantasy 失败时不得回退 Loopback。

### 8. Session ownership 在本 change 中先完成

模型天然是房间/Session 级选择，因此本 change 直接删除 per-character Runtime/Peer/backend ownership。Sandbox 建立唯一 SessionHost；Character binding 只保存 host、ActorId 和 ServerAuthoritative Character Sync Profile。即使当前只有一个本地 Actor，queue/history/debug 也由 Session 唯一拥有。

这会提前完成 `add-local-two-client-gameplay-network-closure` 中一部分 Session ownership 和精确 actor route 工作。后继 change 必须删除这些重复任务，并从现有 SessionHost 扩展 Fantasy、roster 和 RemoteProxy。

### 9. 不用第二实现证明通用性

本 change 只要求 common host 不引用 ServerAuthoritative packet/policy 类型，并要求 Character/BTSMTL 不引用模型类型。它不声称现有 model lifecycle API 已经证明适合 Rollback。

未来增加第二模型时，如果发现 common host 合同无法表达该模型，可以通过新的 OpenSpec change 修改 common composition boundary；不得为了维持当前接口而把 Rollback 伪装成 Correction/Snapshot。

### 10. 队列按业务可靠性处理容量

MotionCommand 和 MotionSnapshot 是连续流；队列满时只允许用同一 SubjectActorId、同一 packet kind 的新样本替换旧样本。Action、GameplayResult、CorrectionAck 等事务事实不能静默丢弃，容量不足必须立即失败并保留明确错误。该规则同时用于 outgoing 与按 actor 分区的 incoming queue，避免“保持有界”以动作生命周期分裂为代价。

## Tradeoffs

### 模型策略独立资产 vs 继续放在 ActionProfile

独立 `ServerAuthoritativeCharacterSyncProfile` 会增加一个明确资产引用，但能让 ActionProfile 保持 gameplay-only，并允许另一个模型提供完全不同的策略结构。继续放在 ActionProfile 编辑更集中，却会让每个动作永久携带 ServerAuthoritative 专用字段，模型替换只能继续加字段和条件 UI。

### 现在建立 SessionHost vs 等 Fantasy change 再做

现在建立 SessionHost 会扩大本 change 的资产迁移范围，但模型选择从第一天就是 Session 级，不会先做 per-character abstraction 再迁移一次。推迟到 Fantasy change 改动较少，却会让新的模型 Definition 仍挂在角色 Driver 上，边界不成立。

### 只做命名迁移 vs 同时移出 policy 和 authority mode

只重命名 Runtime/Packet 成本低，但 CharacterPipeline、ActionContext 和 ActionProfile 仍然依赖当前模型，未来仍要分裂。同步移出 policy 与 authority mode 改动更大，却是“网络模型隔离”真正成立所需的最小完整范围。

### 立即实现模型下拉 vs 只显示完整模型

预先显示 Rollback 能让 UI 看起来可扩展，但没有 runtime、状态恢复和确定性模拟，属于不可用配置。本 change 只允许选择已安装且完整的 model definition；当前 UI 只显示 ServerAuthoritativeHybrid，未来第二模型完成后才出现选择。

## Risks / Migration

- ActionProfile 和 GameplayBehaviorProfile 现有 YAML 包含完整网络策略。必须先读取并迁移到新的 ServerAuthoritative profile，再删除旧字段；不得先删字段再凭默认值重建。
- `GameplayAuthorityMode` 进入 GameplayTickContext、InputStage、MotionModifier 和 Host。迁移必须一次性完成，不保留 enum 映射或双判断。
- CharacterNetworkReceiveStage 当前直接引用 GameplaySync payload。必须先建立 Character 语义输入类型和 adapter 映射，再删除 payload 引用。
- SessionHost 改变 Sandbox 组件 ownership。必须迁移 scene 引用并删除旧 Driver，不保留 wrapper。
- Agent validator 和 Inspector 当前从 ActionProfile 读取网络策略。必须同步迁移到 model profile 的只读/编辑入口，否则 Editor 编译会断裂。
- active 双客户端 change 与本 change 在 Runtime、Peer、SessionHost、MotionCommand、Binding 和 policy 上重叠。不得并行 apply；本 change 完成后必须先重写其文档和 tasks。

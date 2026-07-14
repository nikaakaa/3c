# Design: 角色 Gameplay 管线闭环

## 目标

本设计把当前系统收成一个第一阶段可演示纵切：

```text
Authoring
  -> Input
  -> Action Request
  -> StateMachine / Graph Decision
  -> ActionInstance Runtime
  -> Timeline Facts
  -> Motion / Presentation
  -> SyncFacts
  -> GameplaySyncRuntime / LocalLoopback
  -> Debug
```

这不是新增一个总控模块，而是让已有深模块各自守住自己的 Interface，并通过 `CharacterPipelineFrame`、`CharacterGraphContext` 和 `CharacterPipelineOutput` 交换正式事实。这样可以保持 Locality：motion 的 bug 留在 MotionStage，动作事务的 bug 留在 ActionRuntime，同步策略的 bug 留在 GameplaySyncAdapter。

## Module 和 Seam

### Authoring Assembly Module

正式装配入口仍是 `CharacterPipelineDefinition` 和 `CharacterPipelineHost`。

Interface：

- RootTree / StateMachine authoring 入口。
- CharacterInputProfile。
- ActionProfile 列表。
- Animation layer 表。
- 后续 demo 目标/objective/result 配置只允许作为正式定义进入，不通过场景搜索补齐。

Implementation 可以由 editor inspector、BTSMTL 图窗口和 asset 引用组成，但 runtime 只看正式定义，不读旧 SO/config。

业务取舍：这样作者心智是“配置一个角色管线”，不是“到处拖旧配置”。缺点是早期配置会显得集中，但它避免了旧路径和新路径同时生效。

### CharacterPipeline Module

`CharacterPipeline` 是 runtime 主体，Interface 是 tick 输入和阶段顺序：

```text
BeginRenderFrame
LogicTick
PresentationFrame
Dispose
```

内部阶段保持固定：

```text
NetworkReceive
ActionNetworkResolve
Input
BTSMTL
Motion
NetworkSend
Presentation
Cleanup
```

业务取舍：固定阶段少一点自由度，但对网络压力 demo 更好讲。输入、动作、motion、presentation 和 sync 的因果顺序在代码里可追踪。

### CharacterGraphContext Module

`CharacterGraphContext` 是 Graph 访问 gameplay runtime 的 seam。

Interface 应继续暴露：

- Input value / request 查询和消费。
- Timeline 播放请求。
- ActionRuntime 激活和生命周期提交。
- Action Context 查询。
- Window / Motion / Cue / GameplayResult 输出提交。
- NetworkInput 只读访问。
- Blackboard / tag / resource。

不应暴露：

- Transport / Fantasy Session。
- `CharacterController`。
- Animancer 播放器。
- 场景对象搜索。
- 网络包类型。

业务取舍：Graph 能表达玩法决策，但不能越权做底层动作。缺点是某些节点写起来要多走一次 context 方法，但这正是可调试和可同步的代价。

### ActionRuntime Module

`ActionRuntime` 负责动作事务，不负责播放 Timeline 或计算运动。

Interface：

- 注册 ActionProfile。
- 激活 ActionInstance。
- 应用生命周期 transition。
- 查询 ActionProfile / ActionInstance。
- 记录 Action output debug。

业务取舍：ActionInstance 成为动作期间所有事实的身份锚点。缺点是普通 Timeline 表现也要显式区分是否有 Action Context，但这能避免所有 Timeline 都被误当成可同步攻击。

### TimelinePlaybackScheduler Module

Timeline 只采样时间内容，输出事实：

```text
AnimationContribution
MotionContribution
MotionWarpWindow
ActionWindowSample
ActionCueEvent
ActionMotionSample
```

Timeline 不直接：

- 改 Transform。
- 判命中。
- 扣血。
- 发送网络包。
- 操作 ActionRuntime 生命周期，除非通过正式节点/事实。

业务取舍：Timeline 对作者仍然直观，但 runtime 语义被收束为事实输出。缺点是命中结果不能在 Timeline clip 里偷懒写死，但这是服务端权威和 combat rewind 的前提。

### MotionStage Module

MotionStage 是最终位移 seam。它依赖 `refactor-character-motion-arbitration` 后形成固定顺序：

```text
Locomotion contribution
Action contribution
GameplayResult contribution
MotionWarp modifier
Network correction phase
CharacterController.Move
MotionResult
```

业务取舍：所有位移来源都必须排队进入 MotionStage，调试能解释谁赢。缺点是不能随手在节点里移动角色，但这能防止网络 correction、root motion 和受击击退互相污染。

### PresentationStage Module

Presentation 只消费表现输出：

```text
AnimationContribution
PresentationCue
AnimationLayerPlaybackPlan
```

它不拥有 gameplay 决策，也不推进 Timeline 权威。

业务取舍：动画表现和 gameplay 事实可以分开调试。缺点是本地特效和相机 cue 需要通过正式 cue 输出，但后续 replay/debug 才能看到。

### Sync Module

`SyncFacts` 是 gameplay 事实，不是网络包。同步链路：

```text
SyncFacts
  -> CharacterNetworkSendStage
  -> CharacterGameplaySyncAdapter
  -> ActionNetworkPolicyResolver
  -> GameplaySyncRuntime
  -> Peer
```

第一阶段 peer 只包含：

- `None`
- `LocalLoopback`

业务取舍：现在能在本地模拟预测、确认、拒绝、correction 和 gameplay result，未来 Fantasy 是 Adapter 替换。缺点是第一阶段不是实时多人真联机，但它能先验证动作客户端的网络约束。

## 打通顺序

### 第一层：本地可玩链路

```text
InputValue / ActionRequest
  -> StateMachine / Graph
  -> ActionActivationRequest
  -> ActionInstance
  -> Timeline
  -> Animation / Motion
  -> Move / Animancer
```

这一层证明角色动作手感成立。

### 第二层：事实可同步链路

```text
ActionActivationOutput
ActionLifecycleTransition
ActionWindowSample
ActionMotionSample
ActionCueEvent
GameplayResultEvent
MotionCorrectionAck
  -> SyncFacts
  -> GameplaySyncRuntime history/debug
```

这一层证明不是本地脚本，而是网络约束下的事实流。

### 第三层：loopback 压力链路

```text
Local predicted action
  -> LocalLoopback delay/reject/correction/result
  -> NetworkReceiveStage
  -> ActionNetworkResolveStage / MotionStage / PresentationStage
  -> Debug
```

这一层证明服务端权威压力下仍能保持手感。

### 第四层：2v2vE demo 最小业务链路

第一阶段只要求最小事实：

- actor identity：owner player、team、actor、controlled actor、performer、target。
- action facts：攻击、防守、闪避、支援动作。
- window facts：HitWindow、IFrameWindow、ParryWindow、CancelWindow。
- result facts：HitConfirmed、Blocked、Interrupted、Knockback、ObjectiveProgress。
- objective facts：占点/争夺/归属变化只作为 server event replication 事实，不做完整玩法系统。

## 可插拔设计

可插拔只放在已经有真实变化的 seam：

- Input authoring 可以扩展 input value / action request 类型，但进入 `CharacterInputFrame` 后统一。
- ActionProfile 可以扩展策略字段，但网络策略仍由 resolver 集中解析。
- Timeline 可以扩展 track 输出事实，但事实必须进入 `CharacterPipelineOutput`。
- Motion 可以扩展 contribution source，但最终必须由 MotionResolver / MotionStage 仲裁。
- GameplaySync 可以替换 peer adapter，但 `SyncFacts` 和 gameplay 语义不变。
- Debug backend 可以扩展展示方式，但读取同一份 runtime debug record。

不做以下伪插件化：

- 不让每个节点自带 transport 策略。
- 不让每个 Timeline clip 自己发包。
- 不让 motion resolver 变成任意公式解释器。
- 不让 Fantasy 后端提前以 fake 配置进入正式 Inspector。
- 不新增第二套 pipeline 只服务 demo。

## 依赖和风险

- 本变更依赖 `refactor-character-motion-arbitration` 完成，否则 motion 闭环仍无法解释 correction 和 root motion 的优先级。
- 如果 ActionRuntime 继续只允许单 active action，第一阶段可以做轻攻击/闪避/受击闭环，但复杂支援、叠加 buff 或并行动作需要后续 change 扩展动作槽位。
- 如果 LocalLoopback 只做动作确认，不做 result/correction 注入，demo 无法展示服务端权威压力，需要扩展 loopback 的事实模拟能力。
- 如果 debug 没有按 ActionInstance 串链路，面试展示会变成多个面板各看各的，工程价值下降。

## 与 Spec 的一致性检查

- 不冲突 `gameplay-sync-backend-selection` 的第一阶段后端限制：本设计不新增 Fantasy 选项。
- 不冲突 `character-motion-semantics`：本设计要求 MotionStage 是唯一 Move seam。
- 不冲突 `character-action-activation-flow`：Timeline 仍是动作输出来源，不是动作身份来源。
- 不冲突 `character-input-node-authoring`：BTSMTL 输入 authoring 不暴露 `ClientCommand`。
- 不冲突 `btsmtl-graph-core`：不新增 Workbench、并行 port registry 或 graph 数据结构。

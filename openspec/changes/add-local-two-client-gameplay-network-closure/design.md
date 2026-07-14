## Context

前置 change 已经完成以下所有权：

```text
GameplayNetworkSessionHost
  -> ServerAuthoritativeHybridModelDefinition
  -> ServerAuthoritativeHybridSession
       actor bindings / exact queues / history / debug

CharacterPipeline
  -> Character facts
  -> CharacterServerAuthoritativeAdapter
  -> CharacterServerAuthoritativeBinding
```

当前只缺真实远端。LocalLoopback 与 model session 都在 `Assembly-CSharp`；现有 `GameLogic.asmdef` 可以被 `Assembly-CSharp` 引用，却不能反向引用其中的 `IServerAuthoritativeEndpoint`。如果直接在 GameLogic 复制 packet/peer，或让 Character 调用静态 Fantasy facade，会立刻恢复分裂实现。

本 change 因此只抽出 ServerAuthoritative 模型自己的 endpoint/wire contracts，并在此基础上增加 Fantasy endpoint、Room、roster 与远端角色消费链。现有 SessionHost、Session、Adapter、Binding、policy profile 和 Character facts 不重建。

## Terms

- **ServerAuthoritative Contracts Assembly**：只保存该模型的 packet/payload/identity、endpoint 接口、EndpointDefinition 基类和必要 debug record；它不是跨模型 GameplaySync core。
- **Fantasy Endpoint**：实现 `IServerAuthoritativeEndpoint`，唯一拥有一条 Fantasy client Session，并在 model packet 与生成消息之间映射。
- **Session Event**：JoinCompleted、ActorJoined、ActorLeft、ClockUpdated、Disconnected 等 model session 级事实，不进入 Character per-actor packet queue。
- **Owner Character**：`CharacterInputSource.LocalDevice + CharacterMotionAuthority.LocalSolver`，允许产生 outgoing。
- **Remote Character**：`CharacterInputSource.ExternalFacts + CharacterMotionAuthority.ExternalPose`，只消费模型输入，不产生 outgoing。
- **ExternalActionActivation**：Character 可理解的外部动作激活语义，保存 ActionId、ActionInstanceId、source sequence/tick；不包含 model packet 或动画资源。
- **Remote Snapshot Buffer**：模型 binding 拥有的有界 server-tick 样本缓冲，负责去重、排序、stale 与采样。

## End-to-End Chain

### Owner outgoing

```text
Input System
  -> CharacterInputStage
  -> Corin Graph / StateMachine / Timeline
  -> CharacterMotionStage / ActionRuntime
  -> Character facts
  -> existing CharacterServerAuthoritativeAdapter + model profile
  -> canonical input/action command + prediction comparison metadata
  -> existing ServerAuthoritativeHybridSession
  -> FantasyServerAuthoritativeEndpoint
  -> generated C2G message
  -> Fantasy Session
```

### Server

```text
generated Handler
  -> SessionGameplayActorComponent ownership
  -> bounded Room queues
  -> one Scene-owned 30 Hz tick
  -> sequence validation / action transaction
  -> selected authoritative motion simulation backend
  -> canonical actor state
  -> Owner decision/correction
  -> remote snapshot/action replication
```

### Owner incoming

```text
generated G2C Handler
  -> Fantasy endpoint incoming
  -> ServerAuthoritativeHybridSession exact actor queue
  -> existing Character binding / adapter
  -> ActionLifecycleTransition or ExternalPoseCorrection
  -> existing Action lifecycle / CharacterMotionStage
```

### Remote incoming

```text
MotionSnapshot
  -> model-owned RemoteSnapshotBuffer
  -> latest Character ExternalPoseSample for logic
  -> sampled Character ExternalPresentationPose for visual root

ActionReplication activation
  -> ExternalActionActivation with server ActionInstanceId
  -> existing Action StateMachine / Timeline / animation chain

ActionReplication terminal
  -> existing ActionLifecycleTransition
```

## Decisions

### 1. 只抽出 ServerAuthoritative endpoint contracts

新增 `ThirdPersonGameplay.Networking.ServerAuthoritativeHybrid.Contracts` asmdef。迁入范围只包含 endpoint 两侧都必须认识的模型类型：packet/envelope/payload、identity、packet enums、endpoint 接口、EndpointDefinition 基类和 endpoint debug record。

`ServerAuthoritativeHybridSession`、profile/resolver、Character adapter/binding、LocalLoopback、Inspector 继续留在现有模块。`GameLogic.asmdef` 只增加该 contracts asmdef 与 GameProto 引用。这样 GameLogic 可以实现 Fantasy EndpointDefinition，但不能访问 CharacterPipeline 或模型 policy。

不创建 `GameplaySync.Contracts`，因为这些 packet 明确包含 MotionCorrection、ActionDecision、Snapshot 等当前模型语义，不能冒充其它网络模型的公共协议。

### 2. Fantasy endpoint 是 Session 的唯一 Fantasy owner

`FantasyServerAuthoritativeEndpointDefinition` 显式保存 host、port、KCP、connect timeout、heartbeat 和 queue capacities，创建唯一 `FantasyServerAuthoritativeEndpoint`。Endpoint 通过正式 `Start`/connection state 进入 Connecting、Connected、Faulted、Disconnected；Session 的 Pump 只推进 endpoint 和收取模型 packet/session event。

现有静态 `FantasyClientBootstrap.SessionFacade` 删除。Fantasy 平台初始化可以保持进程级幂等，但 `FantasySessionFacade`、Fantasy Session、handler component 和消息队列都归属 endpoint 实例。连接失败或中途断开不会创建 LocalLoopback，也不会自动重连。

### 3. Roster 使用 model session event，不伪装成 Character packet

JoinCompleted、ActorJoined、ActorLeft 和 server clock 是 Session 级事实。Fantasy endpoint 将生成消息映射为 `ServerAuthoritativeSessionEvent`，model session 放入独立有界 queue；Character per-actor queue 仍只接收有 SubjectActorId 的 gameplay packet。

model-owned roster host 读取 session event。JoinCompleted 配置并激活本地 Owner；ActorJoined 创建远端 Character；ActorLeft 释放远端。Common `GameplayNetworkSessionHost` 不解析 roster、Room 或 Fantasy message。

### 4. MotionCommand 只承载权威端可重演的 canonical 输入

CharacterPipeline 已分别输出 `CharacterInputFrame`、Action facts 和 `ResolvedCharacterMotionFact`。现有 adapter/policy ownership 保持不变，但 MotionCommand 映射必须改为以 canonical input/action facts 为服务端执行输入；`ResolvedCharacterMotionFact` 只作为同 tick prediction comparison metadata、诊断和 correction provenance。本 change 不新增 Character MotionCommand、Behavior binding 或第二 resolver。

Wire message 保存服务端实际需要的 SubjectActorId、input sequence、local logic tick、输入轴/朝向请求、accepted ActionInstance/Action phase 和配置 identity。可选 predicted pose/result 必须有明确 prediction metadata 身份，不能被 backend 读取为 canonical displacement。模型 packet 中不参与当前服务端切片的字段不得在服务端形成第二套 gameplay 解释器。

### 5. 服务端运动依赖一个已批准的独立权威 backend

Owner 保持当前 Graph、Timeline motion curve、Motion resolver 与正式 client Motion Executor 进行本地预测。服务端从 Session 解析 Actor ownership，拒绝非法 identity、NaN/Infinity、零/倒序 sequence 和非法 action phase，再把 canonical input/action state 交给唯一 authoritative simulation backend。backend 必须从当前 canonical body state 独立生成 motion intent、执行世界约束并返回新的 canonical pose。

本 change apply 前必须由独立前置 change 在以下方案中选定并完整实现一个：

- Unity authoritative process：服务端运行可复用的 Character gameplay/motion 语义和 Unity Motion Executor；
- Fantasy 纯 CSharp KCC：服务端运行正式 canonical action/motion 语义和纯 CSharp KCC/world query。

两种 backend 都属于同一 `ServerAuthoritativeHybrid` 模型，不能同 tick 双算、互相投票或故障回退。DotRecast 只可作为 navigation query 输入，不能替代 KCC。确定性 KCC/lockstep/rollback 不进入本 change，而是另一完整 Network Model。

### 6. 远端角色使用现有正交控制模式

不恢复 `LocalPredicted/RemoteProxy` 总控枚举。Owner 固定为 LocalDevice + LocalSolver；远端固定为 ExternalFacts + ExternalPose。`CharacterPipelineHost` 增加只允许激活前调用的正式配置入口和只读控制模式，运行中修改直接失败。

`CharacterServerAuthoritativeBinding` 仍只序列化 SessionHost、CharacterHost、SubjectActorId、SyncProfile。它根据 CharacterHost 控制模式注册 outgoing eligibility：只有 LocalDevice + LocalSolver 可发送；ExternalFacts + ExternalPose 只 drain。不存在额外 authority role 字段或名称判断。

### 7. Snapshot buffer 属于模型，Character 只接收 resolved pose

MotionSnapshot 的 server tick、clock offset、interpolation delay 和 stale 规则属于 ServerAuthoritative 模型。每个远端 binding 拥有一个有界 buffer，按 server tick 去重、排序，容量固定为 32。

逻辑 tick 前，binding 将最新合法样本转换为 `ExternalPoseSample` 和移动摘要，供 ExternalPose motion authority 与 ExternalFacts locomotion 使用。表现帧前，binding 按 Join 返回的 server tick rate 和 4 tick delay 在两个样本间采样，输出 model-neutral `ExternalPresentationPose`；Character PresentationStage 只把该 pose 应用到 visual root。超过 30 server tick 未更新时冻结并报告 stale，不无限外推。

这使 server clock/interpolation policy 留在模型，Character 不认识 ServerAuthoritativeSnapshot；同时 visual root、Timeline visual sampling 和 Animancer fade 仍由同一个 PresentationFrame 推进。

### 8. 远端动作传 gameplay lifecycle，不传动画

服务端接受 ActionActivation 后向 Owner 返回 ActionDecision Confirm，并向其它 Session 广播 ActionReplication activation。远端 adapter 将其转换为 `ExternalActionActivation`，ActionRuntime 使用服务端 ActionInstanceId，现有 Corin Action StateMachine 进入 Attack/Dodge。Terminal replication 转为同一实例的 `ActionLifecycleTransition`。

协议不包含 TimelineId、TrackId、ClipId、AnimationClip、producer id 或 Animancer transition。远端仍走现有 Graph -> Timeline -> AnimationLayerSelection -> Queue -> AnimationPlaybackLifecycle -> Animancer。远端产生的 window/cue/result facts可以用于本地表现和 debug，但 outgoing eligibility 会阻止 echo。

### 9. 一个双人 Room 和一个权威 simulation tick 足够当前纵切

Gate Scene 唯一创建 `GameplayRoomComponent`，最多两条 Session。Join 分配 PlayerId、ActorId、TeamId 和两个固定出生位；第三名返回 RoomFull。每条 Session 挂一个 `SessionGameplayActorComponent`，Handler 从该 component 解析 Actor，不信任客户端 ActorId。

Room 使用一个 30 Hz Scene-owned timer，按 sequence 消费每 actor 有界 input/action queue，调用所选 backend 更新 canonical state，并以 15 Hz 广播 snapshot。断线 DestroySystem 移除 Actor、清队列并广播 ActorLeft。Room 不接数据库、账号、匹配、Map/SubScene 或 Roaming。

### 10. Owner 与远端都从 inactive staging 激活

Sandbox 的 network roster host 引用 inactive Owner root 和同一 Corin clone source。收到 JoinCompleted 后，先写 Owner SubjectActorId、spawn pose、LocalDevice + LocalSolver，再激活。收到远端 descriptor 后，克隆同一 Corin 层级到 inactive staging，先写 ExternalFacts + ExternalPose、SubjectActorId、初始 pose并移除相机依赖，再激活。

这避免 OnEnable 先以错误身份注册，也不需要等待式半配置 binding。ActorLeft 按 binding -> buffers -> pipeline -> clone 顺序释放，不影响 Owner Session。

### 11. Health 只观察正式 owner

Fantasy endpoint、model session、roster 和 server room各自记录自己拥有的连接状态、队列当前/峰值、最近收发、server tick、duplicate、stale、overflow、correction 和 disconnect reason。Inspector 从这些正式对象只读聚合，不复制状态。

所有存储固定容量。Motion stream 继续遵守可替换流规则；Action/Result/Ack/roster 等可靠事实容量不足时明确 Faulted。launcher 只启动正常 server/client 进程并分离日志，不自动恢复故障。

## Tradeoffs

### 模型专属 contracts asmdef vs GameLogic 静态桥

contracts asmdef 增加一次文件迁移，但让 Fantasy endpoint 直接实现正式接口且依赖方向可编译。静态桥改动较少，却会让 Assembly-CSharp 与 GameLogic 通过全局单例和重复 DTO 交换，形成第二条网络入口。

### Unity authoritative backend vs 纯 CSharp KCC backend

Unity authoritative backend 最容易复用现有 Corin Graph、Timeline motion curve 和 CharacterController 手感，业务闭环快；代价是部署更重、服务端带 Unity runtime。纯 CSharp KCC backend 部署更轻、权威边界更清楚；代价是必须正式实现 canonical action/motion 语义和世界碰撞，不能直接复用 Unity authoring runtime。两者都是有效实验，但一次纵切只能选一个，选择必须在 apply 本 change 前由独立 proposal 固定。

### 模型层 snapshot sampling vs Character 内识别 server tick

模型层 sampling 让 server clock/delay/stale 都留在 ServerAuthoritative，Character 只收 pose；代价是 binding 需要正式 presentation hook。把 server tick buffer 放进 CharacterStage 看似少一层，却会把当前模型的时间策略重新耦合进角色主线。

### 远端运行同一 Graph vs 网络专用动画状态机

同一 Graph 保持 Attack combo、Dodge、Timeline 和动画事实唯一；代价是远端仍运行轻量 gameplay 图。网络专用 Animator 状态表会复制动作生命周期和动画映射，每次修改 Corin 都要双改，因此不采用。

### 不做自动重连

显式 Faulted/Disconnected 能暴露通宵运行中的真实问题；代价是连接中断后需要重启客户端。身份恢复、未确认事务和 Actor replacement 是独立完整能力，不在本 change 隐式处理。

## Risks / Migration

- contracts asmdef 的迁移范围必须保持模型专属；Character adapter/profile/session 文件不得一起移入导致 GameLogic 依赖 Character。
- EndpointDefinition 从 Assembly-CSharp 迁入 contracts 后，现有 LocalLoopback definition 和 model asset GUID/脚本引用必须保持有效。
- ProtocolExportTool 必须先确定 GameProto/client 与 Server 输出目录；生成文件不能手写。
- 现有静态 SessionFacade 删除时，GameApp 初始化与 shutdown 必须只管理 Fantasy 平台，不再拥有 gameplay Session。
- Owner/remote GameObject 必须在 inactive 状态完成 host/binding 配置；不得增加空 SubjectActorId 等待路径。
- Remote ActionReplication 必须以 actor + action instance + phase + server tick 去重，避免重复启动 Timeline。
- authoritative backend 必须在本 change apply 前存在正式 definition/runtime/config；缺失时本 change 保持阻塞，不能回退客户端 resolved displacement。
- prediction metadata 与 canonical command fields 必须使用不同字段和解释路径，服务端 backend 不得误读 predicted pose 为 authority input。
- snapshot、endpoint、session event、room command、health 和 diagnostics 全部必须有固定容量，且可靠事实不能静默丢弃。
- current specs 在前置 change 归档后才是本提案的正式基线；apply 前必须确认没有重新出现 GameplaySync 通用类型或 per-character peer ownership。

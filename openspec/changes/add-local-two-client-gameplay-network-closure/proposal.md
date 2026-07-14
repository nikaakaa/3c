# Change: 增加本地双客户端 ServerAuthoritative 网络闭环

## Why

`refactor-gameplay-network-model-boundary` 已经把当前网络链收口为：

```text
Character facts
  -> CharacterServerAuthoritativeAdapter
  -> shared ServerAuthoritativeHybridSession
  -> ServerAuthoritativeEndpointDefinition
```

当前 Sandbox 已有唯一 `GameplayNetworkSessionHost`、ServerAuthoritative model definition、Character binding、精确 SubjectActorId 队列和 LocalLoopback endpoint，但仍只能在单进程内模拟远端。`refactor-character-motion-simulation-boundary` 已把 gameplay intent、世界约束执行和逻辑位姿拆开；项目仍没有为该网络纵切选择并实现唯一服务端权威运动 backend，也没有真实 Fantasy endpoint、生成协议、服务端 Room、Join/roster、远端角色实例、server-tick snapshot sampling 或远端动作复制。

后继工作必须直接扩展现有 `ServerAuthoritativeHybrid`，不能恢复通用 `GameplaySyncRuntime/Peer`、每角色 Session、Character 内 backend enum 或网络策略。apply 前必须先批准并完成一个服务端权威运动 backend change，在 Unity authoritative process 与 Fantasy 纯 CSharp KCC 中选择一个作为本纵切唯一实现；两者不得同时运行、互相校验或在故障时互为 fallback。之后本 change 才增加一条可运行的本地纵切：一个模型 Session、两个 Unity 客户端、每端一个本地 Owner 与一个远端 Corin。

## What Changes

- 将 `ServerAuthoritativePacket`、endpoint 接口、EndpointDefinition 基类和必要 model wire/debug 合同迁入 ServerAuthoritative 专属 contracts asmdef，使 `GameLogic` 可以实现同一模型的 Fantasy endpoint；该程序集不命名为 GameplaySync，也不包含 Character、Graph、Timeline 或 Animation 类型。
- 新增 `FantasyServerAuthoritativeEndpointDefinition` 与 `FantasyServerAuthoritativeEndpoint`。Endpoint 唯一拥有 Fantasy Session、连接状态、heartbeat、生成消息映射和收发队列；连接失败明确 Faulted，不回退 LocalLoopback。
- 删除现有静态 `FantasyClientBootstrap.SessionFacade` ownership；平台初始化可以保持进程级，具体 Session 必须由 Fantasy endpoint 实例持有。
- 用正式 Outer proto 定义 Join/roster、canonical input/action MotionCommand、MotionSnapshot/Correction/Ack、ActionActivation/Lifecycle/Decision/Replication，并通过 ProtocolExportTool 生成 client/server C#；删除旧 FrameSync 协议、opcode 和 parser。
- 扩展现有 `ServerAuthoritativeHybridSession` 处理有界 session event queue、endpoint connection/health 和 Owner identity；不重建 SessionHost、Character binding、精确 actor queue 或 policy profile。Character adapter 必须从 canonical input/action facts 构造 command，resolved motion 只进入 prediction comparison metadata。
- 新增本地双人 Room。每条连接只拥有一个 Actor，服务端分配 PlayerId、ActorId、TeamId、出生 pose 和 server clock，并维护唯一 canonical actor pose 与动作事务。
- Room 按 sequence 接受 canonical input/action command，调用前置 change 已选定的唯一 authoritative simulation backend，独立生成 motion intent 与 canonical pose。服务端不得累加客户端 applied displacement；backend 缺失或失败时明确停止，不回退 envelope validation。
- Owner 继续使用 `LocalDevice + LocalSolver`，立即运行现有 Corin Graph、Timeline、Motion 和动画；服务端 ActionDecision 仍由 adapter 转换为 Character `ActionLifecycleTransition`，MotionCorrection 仍由 CharacterMotionStage 唯一应用。
- 远端 Corin 使用 `ExternalFacts + ExternalPose`，不新增 `RemoteProxy` authority enum。MotionSnapshot 在模型 binding 内进入有界 server-tick buffer；模型在正式 tick/presentation 边界输出 Character external input 和 resolved visual pose。
- 新增 model-neutral `ExternalActionActivation` 语义输入，使远端 ActionReplication 可携带服务端 ActionInstanceId 进入现有 ActionRuntime；terminal replication 继续使用既有 `ActionLifecycleTransition`。协议不携带动画、Timeline 或 producer identity。
- 新增 model-owned roster host，在 inactive staging root 中先配置 SubjectActorId、输入来源、运动权威和相机依赖，再激活 Owner/远端 CharacterPipeline；ActorLeft 精确释放对应 binding、buffer、pipeline 和 clone。
- 增加有界 health、连接/队列/duplicate/stale/correction/disconnect diagnostics，以及正常可见的本地 server/双客户端启动脚本；不使用 Unity batchmode，不自动重连或重启。

## Scope

本 change 的可见闭环固定为：

1. 两个本地 Unity 客户端各自通过一个 Fantasy endpoint 连接 `127.0.0.1:20000`。
2. 服务端为每端分配唯一 Actor；每端各自控制一个 Corin，并看到另一个远端 Corin。
3. 客户端移动、转向、闪避、Attack1/Attack2 连段通过现有 Character/Graph/Timeline/Animation 主链预测；服务端从 canonical input/action state 独立推进权威运动。
4. Owner 动作经过服务端 Confirm/Reject；Owner pose 偏差经过现有 MotionCorrection application；远端 pose 使用 server snapshot buffer 和表现插值。
5. 任一客户端离开后，另一端删除对应远端角色，Owner 与 Session 继续运行。
6. 长时间空闲或持续收发时，客户端和服务端全部正式队列与 history 保持有界并可诊断。

攻击只闭环到“服务端确认动作事务、权威运动 backend 推进对应动作运动语义，并让远端播放同一动作”。本 change 不实现命中检测、伤害、生命值、受击、PvE、完整 replay/rollback、lag compensation、账号、匹配、数据库或断线恢复。世界碰撞能力以所选前置 backend 的正式范围为准，不在本 change 临时补算法。

## Non-Goals

- 不创建第二 Network Model，不实现 Lockstep 或 Rollback。
- 不恢复 `GameplaySyncRuntime`、`IGameplaySyncPeer`、backend enum、per-character peer 或 generic packet。
- 不把 Fantasy message、Session、Room 或 endpoint 类型放进 CharacterPipeline、BTSMTL、Timeline 或 Animation。
- 不创建网络专用 Animator、AnimationClip 映射、Timeline、动作状态机或远端角色配置资产。
- 不把客户端 Graph runtime、Timeline runtime、Animation 或 Animancer 复制到纯 CSharp backend；服务端使用前置 backend 已批准的 canonical action/motion 语义。
- 不把 resolved-motion 限幅、客户端 pose 接受或 envelope validation 宣传为服务端权威运动。
- 不新增测试或人工验证 task，不运行 Unity batchmode。

## Current Spec Comparison

- `gameplay-network-model-boundary` 已要求一个 Session 只装配一个完整模型；本 change 只为现有 `ServerAuthoritativeHybrid` 增加 Fantasy EndpointDefinition，不增加模型选择分支。
- `server-authoritative-hybrid-sync-model` 已拥有 packet、Session、精确 actor route、队列可靠性和 Character policy；本 change 只增加 endpoint contracts asmdef、session events、Fantasy 实现和真实远端数据。
- `gameplay-sync-backend-selection` 在边界重构后使用 EndpointDefinition 而不是 backend enum；本 change 让 Fantasy definition 成为第二个已实现 endpoint，Disconnected 与 LocalLoopback 语义不变。
- `character-gameplay-sync-adapter` 已完成事实到模型 packet、模型 decision 到 `ActionLifecycleTransition` 的映射；本 change 只补充远端 ActionReplication 与 snapshot buffer 的语义转换。
- `character-network-sync-domain-contract` 已区分 canonical input/action facts 与 resolved prediction result；本 change 的模型 Adapter 只能以前者构造权威端命令，后者只用于 prediction comparison/diagnostics。
- `character-motion-simulation-boundary` 已规定服务端必须从 canonical input/action state 独立推进，并由唯一 backend 产生 canonical pose；本 change 依赖所选 backend 的完整实现，不自行创建第二 motor。
- `character-pipeline-runtime` 已使用 `CharacterInputSource` 与 `CharacterMotionAuthority`；本 change 用 `ExternalFacts + ExternalPose` 组合远端角色，不恢复 LocalPredicted/RemoteProxy 总控枚举。
- `character-presentation-interpolation` 已分离 logic root 与 visual root；本 change 增加由模型 snapshot sampler 提供的 external visual pose，不创建第二个表现 Update。

## Impact

- 新能力：`fantasy-server-authoritative-endpoint`、`local-two-client-gameplay-session`。
- 受影响规范：`gameplay-sync-backend-selection`、`character-gameplay-sync-adapter`、`character-network-sync-domain-contract`、`character-pipeline-runtime`、`character-presentation-interpolation`。
- 客户端模型代码：ServerAuthoritative contracts asmdef、Session event/health、Fantasy EndpointDefinition/Endpoint、generated message mapper。
- 客户端角色代码：既有 Character binding 的 pre-activation 配置与 outgoing eligibility、external action activation、external pose visual input、roster host。
- 服务端代码：Outer proto/generated code、双人 Room、Session actor ownership、canonical input/action handler、所选权威 motion backend integration、固定 server tick、snapshot/action broadcast 和 leave cleanup。
- 资产与入口：Sandbox Fantasy endpoint definition、现有 model definition 引用、inactive Owner/staging root、远端 clone source、本地 server 与双客户端启动脚本。
- 删除项：旧 FrameSync 协议、静态 SessionFacade ownership、字符串 placeholder、任何 generic GameplaySync/per-character peer 残留，以及后继实现中出现的 fallback 或双写入口。

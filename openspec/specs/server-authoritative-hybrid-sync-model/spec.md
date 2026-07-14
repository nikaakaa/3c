# server-authoritative-hybrid-sync-model Specification

## Purpose
TBD - created by archiving change refactor-gameplay-network-model-boundary. Update Purpose after archive.
## Requirements
### Requirement: 当前混合同步语义必须归属 ServerAuthoritativeHybrid

系统 MUST 将 Owner 本地预测、服务端动作裁决、Motion correction、Remote snapshot interpolation、Action replication 和局部 combat history 的组合明确命名为 `ServerAuthoritativeHybrid` 或等价模型。该模型 MUST NOT 使用 `GameplaySync` 通用命名暗示 packet 或 history 可直接用于 Rollback。

#### Scenario: 查看模型运行时

- **WHEN** Runtime Debug 显示当前 Session 网络状态
- **THEN** MUST 显示 model id 为 `ServerAuthoritativeHybrid`
- **AND** correction、snapshot 和 action decision MUST 显示为该模型的事实

### Requirement: ServerAuthoritative 模型必须唯一拥有 Packet 与 History

`ServerAuthoritativePacket`、Envelope、payload、History、Debug 和 endpoint 合同 MUST 归属 ServerAuthoritative 模块。Common networking、CharacterPipeline、BTSMTL 和 Animation 模块 MUST 不引用这些具体类型。

#### Scenario: 保存 MotionCorrection history

- **WHEN** 模型发送或接收 MotionCorrection
- **THEN** 对应 packet history MUST 保存在 ServerAuthoritative model session
- **AND** CharacterPipeline history MUST 不保存该 packet 对象

### Requirement: ServerAuthoritative Character Policy 必须有唯一模型资产

系统 MUST 使用 `ServerAuthoritativeCharacterSyncProfile` 或等价模型专属资产，按稳定 BehaviorId/ActionId 唯一保存 Stream、Transaction、State 和 Event 的 prediction、authority、replication、history、snapshot、command send 和 output policy。ActionProfile、GameplayBehavior identity、Graph、Timeline 和 Blackboard MUST 不复制这些策略。

#### Scenario: 配置 Attack 网络策略

- **WHEN** 作者配置 `Attack.Light.01` 在当前模型下的 window、motion、cue 和 result 策略
- **THEN** 配置 MUST 只写入 ServerAuthoritative Character Sync Profile
- **AND** `Attack.Light.01` ActionProfile MUST 只保存 gameplay 动作定义

#### Scenario: 缺失 Action policy

- **WHEN** Character facts 包含某个需要同步的 ActionId，但模型 profile 没有对应 policy
- **THEN** 模型配置 MUST 明确失败
- **AND** resolver MUST 不使用默认 ActionProfile、名称搜索或 local-only fallback

### Requirement: ServerAuthoritative Adapter 必须是唯一 Packet 映射入口

系统 MUST 使用 model-owned Character adapter 将 canonical Character input/action facts 映射为 ServerAuthoritative outgoing commands，并将 incoming packets 映射为 Character 语义输入。Adapter MUST 复用 model policy resolver，MUST NOT 回读 Graph、Timeline 或 Animation 结构补齐策略。客户端 resolved motion MAY 进入模型 prediction history 或 diagnostics，但 MUST NOT 被映射为服务端唯一 canonical displacement。

#### Scenario: 构造 MotionCommand

- **WHEN** adapter 收到 canonical input frame、相关 action request 和当前 policy
- **THEN** adapter MUST 构造供权威端独立模拟的 ServerAuthoritative MotionCommand
- **AND** MotionCommand MUST 不把客户端 actual displacement 作为服务端唯一运动输入
- **AND** MotionStage MUST 不直接构造 packet

#### Scenario: 记录 prediction result

- **WHEN** adapter 收到同 tick resolved motion fact
- **THEN** model MAY 将其记录为 prediction comparison metadata
- **AND** authority backend MUST 不直接采用该 pose 作为 canonical pose

#### Scenario: 构造 CorrectionAck

- **WHEN** CharacterMotionStage 输出成功的 correction application result
- **THEN** adapter MUST 构造模型 acknowledgement
- **AND** CharacterPipeline MUST 不持有 endpoint 或 packet id

### Requirement: ServerAuthoritative Endpoint 必须可替换但不得改变模型语义

模型 MUST 提供 `IServerAuthoritativeEndpoint` 与模型专属 `ServerAuthoritativeEndpointDefinition` 或等价合同。每个 EndpointDefinition MUST 自己创建 endpoint；LocalLoopback 与未来 Fantasy MUST 使用独立 definition 并消费、产出同一模型消息语义。新增 endpoint MUST NOT 修改模型核心 enum 或 switch，切换 endpoint MUST 不改变 Character adapter、model policy 或 action/motion lifecycle。

#### Scenario: 明确断开

- **WHEN** model definition 未引用 EndpointDefinition
- **THEN** model session MUST 保持明确 disconnected/local 状态
- **AND** MUST 不创建 Loopback 或 Fantasy fallback

#### Scenario: LocalLoopback endpoint

- **WHEN** model definition 引用 LocalLoopback EndpointDefinition
- **THEN** endpoint MUST 按正式模型设置产生 confirm/reject/correction/snapshot
- **AND** MUST 不直接修改 CharacterPipeline 或 Transform

### Requirement: ServerAuthoritative 队列必须区分连续流与可靠事实

Model session 的 outgoing 与 per-actor incoming queue MUST 保持有界。MotionCommand 和 MotionSnapshot MAY 只替换同一 SubjectActorId、同一 packet kind 的旧流样本；Action、GameplayResult、CorrectionAck 和其它事务事实 MUST NOT 静默丢弃，容量不足时 MUST 明确失败。

#### Scenario: MotionSnapshot 队列已满

- **WHEN** 同一 actor 的新 MotionSnapshot 到达已满队列
- **THEN** session MAY 替换该 actor 最旧的 MotionSnapshot
- **AND** MUST 不移除 Action 或 Result packet 腾出空间

#### Scenario: ActionDecision 队列已满

- **WHEN** 新 ActionDecision 到达已满队列
- **THEN** session MUST 报告可靠队列溢出并停止本次入队
- **AND** MUST 不静默丢弃旧或新动作事实

### Requirement: ServerAuthoritative Session 必须拥有精确 Actor 路由

Model session MUST 使用唯一 SubjectActorId 路由 incoming，并使用有界 per-actor queue。OwnerPlayerId、TeamId、PerformerActorId 和 TargetActorId MAY 作为业务 metadata，但 MUST NOT 参与 drain 匹配。

#### Scenario: ActorA 攻击 ActorB

- **WHEN** packet 的 SubjectActorId 是 ActorA 且 TargetActorId 是 ActorB
- **THEN** packet MUST 只进入 ActorA binding queue
- **AND** ActorB MUST 不因为 target identity 消费同一 packet

### Requirement: ServerAuthoritative Session 必须是 Session-level ownership

一个客户端 Session MUST 只有一个 ServerAuthoritative model runtime 和 endpoint。Character binding MUST 只保存 SessionHost、SubjectActorId、Character runtime port 和模型 policy profile，不得各自创建 runtime、peer、history 或 backend。

#### Scenario: 当前单角色 Loopback

- **WHEN** Sandbox 只有一个本地 Owner
- **THEN** 该 Owner binding MUST 复用 SessionHost 唯一 model session
- **AND** 旧 per-character sync driver MUST 不再存在

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid 的权威端 MUST 从已接受的 canonical input、action state、角色配置和当前 canonical body state 生成 motion intent，并调用唯一正式 authoritative simulation backend 产生 canonical pose。Backend MUST 由 model/server composition root 显式装配。系统 MUST NOT 同时运行 Unity backend 与纯 CSharp KCC backend 后选择结果，也 MUST NOT 在 backend 缺失时累加客户端 resolved displacement 作为 fallback。

#### Scenario: 使用 Unity authoritative process

- **WHEN** model definition 选择 Unity authoritative backend
- **THEN** 服务端 MUST 在 Unity process 内独立推进角色 motion semantics
- **AND** MUST 使用正式 Unity Motion Executor 执行 world constraint

#### Scenario: 使用纯 CSharp KCC server

- **WHEN** model definition 选择纯 CSharp KCC backend
- **THEN** 服务端 MUST 在纯 CSharp runtime 内独立推进角色 motion semantics
- **AND** MUST 使用正式 KCC/world query implementation 产生 canonical pose
- **AND** navigation/pathfinding library MUST 不被当作完整碰撞 motor

#### Scenario: backend 缺失

- **WHEN** ServerAuthoritativeHybrid 要求权威运动但没有配置完整 backend
- **THEN** model session 启动 MUST 失败
- **AND** MUST 不回退到 envelope validation 或 client pose acceptance


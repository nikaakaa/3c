# server-authoritative-hybrid-sync-model Specification

## ADDED Requirements

### Requirement: ServerAuthoritativeHybrid 必须是完整独立的 Network Model

系统 MUST将 owner prediction、authority correction、remote replication、baseline、ack 与模型 history 明确归属 ServerAuthoritativeHybrid。该模型 MUST通过正式 Prediction Source、Authority Source、Prediction Pipeline、Authority Pipeline、Fantasy Endpoint、Program Runtime、Execution Backend 与 WorldSolver 形成完整组合，不得把公共 Gameplay 合同重新命名为模型专属实现。

#### Scenario: 查看模型身份

- **WHEN** ServerAuthoritative Session 进入 Active
- **THEN** diagnostics MUST显示明确 ModelId、protocol、Prediction/Authority Pipeline identity
- **AND** correction、baseline、ack 与 replication MUST显示为该模型的事实

### Requirement: 模型协议与历史必须由模型模块唯一拥有

Generated Fantasy control protocol、gameplay datagram codec、Network Checkpoint Layout、baseline、ack、replication product、prediction history、correction journal、queue 与 diagnostics schema MUST归 ServerAuthoritativeHybrid 模块。Common Simulation、Character、BTSMTL、Animation 与 Presentation MUST不引用具体 transport message、history container 或 correction policy。

#### Scenario: 保存 Owner Prediction History

- **WHEN** Prediction Pipeline提交一个未确认 SimulationTick
- **THEN** 模型 SnapshotParticipant MUST保存该 Tick所需的 input、state 与 EventId journal
- **AND** CharacterPipelineHost 与 Common Session Host MUST不保存该 history

### Requirement: 模型策略必须集中在模型 Definition 与 Pass 配置

ServerAuthoritativeHybrid 的 prediction、authority、replication、history、snapshot、command send、missing-input 与 output disposition policy MUST只保存在模型 Definition、Profile 或 Pass config，并通过稳定 ActionId、BehaviorId、ProducerId 与 Fact identity引用 Gameplay。ActionProfile、GameplayEffectDefinition、Graph、Timeline 与 Blackboard MUST不复制模型策略；缺失必需 policy 时配置 MUST失败。

#### Scenario: Attack Fact 缺少复制策略

- **WHEN** Corin Program可能输出需要复制的 Attack Fact
- **AND** 模型配置没有对应稳定 identity policy
- **THEN** ModelDefinition 或 Pipeline compile MUST明确失败
- **AND** MUST不按名称、ActionProfile 或默认规则推断

### Requirement: ServerAuthoritative Endpoint必须显式组合控制与Gameplay数据面

ServerAuthoritativeHybrid ModelDefinition MUST显式引用模型专属Fantasy Control Endpoint与Gameplay Datagram Endpoint Definition。Control Endpoint MUST只负责KCP连接、generated control/reliable message与Source port；Datagram Endpoint MUST只负责ticket handshake、command/snapshot bytes与Source port。二者不得修改prediction、authority、baseline、replay或output disposition语义。系统 MUST不保留LocalLoopback或KCP gameplay fallback；任一Endpoint缺失或连接失败 MUST使Source preparation失败。

#### Scenario: 创建 Fantasy Endpoint

- **WHEN** Prediction或 Authority Source preparation读取合法Control/Data Endpoint Definition
- **THEN** Endpoint MUST创建匹配process role的Fantasy control connection、ticket lifecycle与typed datagram Source port
- **AND** 连接失败 MUST不回退 Standard Local Pipeline

### Requirement: ServerAuthoritative 队列必须区分连续流与可靠事实

Model Source、Authority Worker与Fantasy Room的队列 MUST保持有界。每Actor command queue与每Client snapshot baseline MUST相互隔离；Roster、Action/Effect/Cue EventId batch、Full Checkpoint、Session failure与其它可靠事务 MUST不得静默丢弃。可靠队列容量不足时 MUST使当前 Room/Session明确失败，不得删除旧可靠事实或挤占其它Actor队列。

#### Scenario: Actor A Command Stream积压

- **WHEN** Actor A command queue到达容量或tick窗口边界
- **THEN** Worker MUST按明确stale/overflow策略拒绝Actor A sample并记录原因
- **AND** MUST不阻塞Actor B或删除可靠事实腾出空间

#### Scenario: 可靠 Event Batch 队列已满

- **WHEN** 新 AuthorityReplicationEventBatch到达已满可靠队列
- **THEN** Room/Session MUST fail-stop并记录精确 Actor/Tick/Event范围
- **AND** MUST不静默丢弃旧或新 EventId

### Requirement: ServerAuthoritative Session 必须拥有精确 Actor 路由

Fantasy Room MUST使用RoomId、SessionId、PlayerId、SubjectActorId与owner connection精确路由worker register、ticket、roster、full checkpoint与可靠facts。Authority Worker和Model Source MUST使用ticket锁定的Room/Session/Player/SubjectActorId/remote endpoint精确路由command与snapshot。TargetActorId、TeamId与其它业务metadata MUST不得替代SubjectActorId进行queue drain。Unknown、duplicate、stale、role-mismatched或owner-mismatched route MUST明确拒绝，MUST不广播到猜测Actor。

#### Scenario: Client A 提交 Actor B 输入

- **WHEN** Client A data endpoint发送SubjectActorId为Actor B的command datagram
- **THEN** Authority Worker MUST按ticket owner route拒绝该消息
- **AND** MUST不写入Actor B command queue

### Requirement: ServerAuthoritative Session 必须是 Session-level ownership

一个客户端 Prediction Session MUST只有一个ServerAuthoritative Prediction Source、一个Fantasy control connection、一个Gameplay datagram endpoint、一个Prediction Pipeline runtime与一个owner simulation roster；一个Unity Authority Worker MUST只有一个Authority Source、一个Fantasy control connection、一个Gameplay datagram endpoint、一个Authority Pipeline runtime与一个完整canonical roster。Character registration MUST不创建自己的model runtime、Endpoint、history、Pipeline或WorldSolver。Remote presentation registration MAY共享当前client Model Source，但 MUST不创建第二Gameplay Session。

#### Scenario: Client A 显示 Owner 与 Remote Actor

- **WHEN** Client A roster包含 Actor A owner与 Actor B remote
- **THEN** Actor A MUST由当前 Prediction Session模拟
- **AND** Actor B MUST通过同一 Source的 committed remote presentation output显示
- **AND** 两者 MUST不各自创建 Network Session

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid权威端 MUST通过Authority Session Source与显式Authority Pipeline向同一SimulationProgramCatalog提供worker校验后的canonical input，并由Float32 Program Runtime、Float32 Pass Backend、标准Evaluate/WorldSolve/Finalize Pass与唯一UnityCharacterControllerWorldSolver独立产生canonical Character/World state。Fantasy Room MUST不执行Program或Solver。客户端predicted displacement、Transform或Body sample MUST不成为权威位移输入。Authority Egress MUST只生成checkpoint/snapshot、ack、typed facts与presentation identity，不得接受或拒绝已完成的权威Gameplay state。

#### Scenario: Unity Authority Worker 推进两个 Actor

- **WHEN** Authority Pipeline收到 Room校验后的 Actor A/B input batch
- **THEN** worker MUST按稳定 ActorId执行同一 Program并进行一次 World ResolveBatch
- **AND** MUST从 finalized canonical state生成 baseline与 replication

#### Scenario: 权威组合缺少 Unity Solver

- **WHEN** Authority composition缺少匹配 Float32 ABI的 Unity Solver或 required capability
- **THEN** Pipeline compile或 Source preparation MUST失败
- **AND** MUST不接受 client pose、切换 Local或创建第二 Solver

### Requirement: ServerAuthoritative 模型必须显式区分 Fantasy Room 与 Unity Authority Worker

Fantasy .NET进程 MUST只拥有连接、Room、roster、sequence校验与消息路由；Unity Authority Worker MUST拥有 Program、canonical roster、Authority Pipeline、WorldSolver与权威 state。Worker MUST以专用 process role注册并锁定 identity，普通 Client MUST不能提升为 Worker。Worker断开时当前 Room MUST fail-stop，不得由 Client接管 authority。

#### Scenario: Worker 注册 Room

- **WHEN** Unity Authority Worker提交完整 Program/Pipeline/Solver identity
- **THEN** Room MUST锁定唯一 Worker connection
- **AND** clients MUST只在该 identity与自己的 Prediction组合兼容后进入 Active

### Requirement: ServerAuthoritative 模型缺少正式 Source 或 Pipeline 时必须不可用

ServerAuthoritativeHybrid ModelDefinition MUST只有在 Prediction Source、Authority Source、Prediction Pipeline、Authority Pipeline、全部 Pass factory、Fantasy Endpoint、Float32 Program Runtime/Backend、Unity Solver与 Actor/presentation registration全部可创建时才可使用。仅保留 protocol、profile、Source壳、Room或旧 session facade MUST不构成完整模型。

#### Scenario: 只有 Fantasy 协议与 Room

- **WHEN** Fantasy Room可连接但 Prediction Correction Pass或 Authority Pipeline缺失
- **THEN** ModelDefinition MUST报告不可用并阻止 Session Active
- **AND** MUST不创建 Standard Local Pipeline代替

### Requirement: ServerAuthoritative 握手必须锁定 Program 与 Pipeline 兼容 Pair

Authority Worker与 Client MUST使用相同 ProgramHash、LayoutHash、operation-set与 TickRate。Prediction PipelineHash与 Authority PipelineHash MAY不同，但 MUST由同一 model protocol明确声明为兼容 pair，并分别锁定 Backend与 Solver capability。Room MUST不以显示名、asset path或仅 ProgramHash猜测兼容性。

#### Scenario: Program 相同但 Pipeline Pair 未知

- **WHEN** Client ProgramHash匹配 Worker但 Prediction PipelineHash不在 Worker声明的兼容 pair中
- **THEN** join MUST失败并返回明确 identity错误

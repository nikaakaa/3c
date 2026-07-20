# server-authoritative-hybrid-sync-model Specification

## MODIFIED Requirements

### Requirement: ServerAuthoritative Endpoint必须显式组合控制与Gameplay数据面

ServerAuthoritativeHybrid ModelDefinition MUST显式声明模型控制合同与Gameplay Datagram Endpoint Definition。Client与外部Unity Authority Worker MUST使用模型专属Fantasy Outer Control Endpoint；Fantasy Server内的DotRecast Authority Scene MUST使用正式Inner/Address control adapter。所有Host MUST复用同一host-neutral control transport产品。Datagram Endpoint MUST只负责ticket handshake、command/snapshot bytes与Source port。控制adapter与Datagram Endpoint均不得修改prediction、authority、baseline、replay或output disposition语义。系统 MUST不保留LocalLoopback、Inner/Outer gameplay relay或KCP gameplay fallback；当前Host Profile要求的任一Endpoint或control route缺失 MUST使Source preparation失败。

#### Scenario: 创建InProcess DotRecast Endpoint

- **WHEN** DotRecast Authority Scene preparation读取合法Inner control route与Data Endpoint Definition
- **THEN** Authority Scene MUST创建匹配Host identity的control adapter、ticket lifecycle与typed datagram Source port
- **AND** control route失败 MUST不回退Standard Local Pipeline或外部Worker

### Requirement: ServerAuthoritative 队列必须区分连续流与可靠事实

Model Source、Authority Host与Fantasy Room的队列 MUST保持有界。每Actor command queue与每Client snapshot baseline MUST相互隔离；Roster、Action/Effect/Cue EventId batch、Full Checkpoint、Session failure与其它可靠事务 MUST不得静默丢弃。可靠队列容量不足时 MUST使当前Room/Session明确失败，不得删除旧可靠事实或挤占其它Actor队列。Authority Host MAY是外部Unity Worker或Fantasy Server内DotRecast Authority Scene，但队列语义 MUST只由portable Authority Source拥有。

#### Scenario: Actor A Command Stream积压

- **WHEN** Actor A command queue到达容量或tick窗口边界
- **THEN** Authority Host MUST按明确stale/overflow策略拒绝Actor A sample并记录原因
- **AND** MUST不阻塞Actor B或删除可靠事实腾出空间

#### Scenario: 可靠 Event Batch 队列已满

- **WHEN** 新AuthorityReplicationEventBatch到达已满可靠队列
- **THEN** Room/Session MUST fail-stop并记录精确Actor/Tick/Event范围
- **AND** MUST不静默丢弃旧或新EventId

### Requirement: ServerAuthoritative Session 必须拥有精确 Actor 路由

Fantasy Room MUST使用RoomId、SessionId、PlayerId、SubjectActorId与owner connection精确路由authority host register、ticket、roster、full checkpoint与可靠facts。Authority Host和Model Source MUST使用ticket锁定的Room/Session/Player/SubjectActorId/remote endpoint精确路由command与snapshot。External Unity Worker register MUST绑定精确worker Session；InProcess DotRecast register MUST绑定精确Authority Scene Address。TargetActorId、TeamId与其它业务metadata MUST不得替代SubjectActorId进行queue drain。Unknown、duplicate、stale、role-mismatched或owner-mismatched route MUST明确拒绝，MUST不广播到猜测Actor。

#### Scenario: Client A 提交 Actor B 输入

- **WHEN** Client A data endpoint发送SubjectActorId为Actor B的command datagram
- **THEN** 当前Authority Host MUST按ticket owner route拒绝该消息
- **AND** MUST不写入Actor B command queue

### Requirement: ServerAuthoritative Session 必须是 Session-level ownership

一个客户端Prediction Session MUST只有一个ServerAuthoritative Prediction Source、一个Fantasy control connection、一个Gameplay datagram endpoint、一个Prediction Pipeline runtime、一个显式WorldSolver与一个owner simulation roster。一个Authority Host MUST只有一个Authority Source、一个Gameplay datagram endpoint、一个Authority Pipeline runtime、一个显式WorldSolver与一个完整canonical roster；External Unity Worker额外拥有一个Fantasy Outer control connection，InProcess DotRecast Authority Scene拥有一个Fantasy Inner control adapter。Character registration MUST不创建自己的model runtime、Endpoint、history、Pipeline或WorldSolver。Remote presentation registration MAY共享当前client Model Source，但 MUST不创建第二Gameplay Session。当前Room MUST只锁定一个Authority Host route。

#### Scenario: DotRecast Client A显示Owner与Remote Actor

- **WHEN** Client A roster包含Actor A owner与Actor B remote且当前HostProfile为InProcessDotRecast
- **THEN** Actor A MUST由当前Prediction Session和DotRecast Solver模拟
- **AND** Actor B MUST通过同一Source的committed remote presentation output显示
- **AND** 两者 MUST不各自创建Network Session或WorldSolver

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid权威端 MUST通过唯一Authority Source与显式Authority Pipeline向同一SimulationProgramCatalog提供Host校验后的canonical input，并由Float32 Program Runtime、唯一Float32 Pass Backend、标准Evaluate/WorldSolve/Finalize Pass与当前Authority Host Profile的唯一WorldSolver独立产生canonical Character/World state。External Unity CharacterController Host与InProcess DotRecast Authority Scene MUST使用相同ProgramHash、BackendId、Authority Pipeline descriptor/hash和model products，只允许HostProfile、Solver、World与control route identity不同。Fantasy Gate Scene MUST不执行Program或Solver；Fantasy Server内的独立Authority Scene MAY作为普通.NET Host执行它们。客户端predicted displacement、Transform、Body sample或position字段 MUST不成为权威位移输入。

#### Scenario: InProcess DotRecast Authority Scene推进两个Actor

- **WHEN** Authority Pipeline收到Actor A/B canonical input batch且当前HostProfile为InProcessDotRecast
- **THEN** Authority Scene MUST按稳定ActorId执行同一Float32 Program并进行一次DotRecast World ResolveBatch
- **AND** MUST从finalized canonical state生成checkpoint与replication

### Requirement: ServerAuthoritative 模型缺少正式 Source 或 Pipeline 时必须不可用

ServerAuthoritativeHybrid Network Model MUST只保存协议、Prediction/Authority Pipeline Pair、同步策略以及Numeric/ABI/Backend/Solver能力要求，MUST不保存具体Program Runtime、Execution Backend或WorldSolver引用。客户端Session Composition MUST显式选择Prediction Source、Prediction Pipeline、客户端Pass factory、Fantasy Endpoint、Float32 Program Runtime/Backend、Prediction Solver与Actor/presentation registration。每个Authority Host Profile MUST另外提供完整portable Authority Source runtime、Authority Pipeline catalog、Float32 Program Runtime/Backend、匹配Authority Solver、locked roster、World、Host Profile要求的control adapter和gameplay datagram endpoint。InProcess DotRecast环境的Prediction与Authority MUST锁定相同SolverId/version、NavigationSurfaceArtifactHash和QueryProfileHash。仅保留protocol、profile、Room、Authority Scene或外部Worker壳 MUST不构成可启动Host。

#### Scenario: DotRecast Client仍配置CC Solver

- **WHEN** Client launch profile要求InProcessDotRecast World identity但Prediction Composition配置CC Solver
- **THEN** Client preparation MUST失败
- **AND** MUST不连接Room后依赖correction掩盖不匹配

### Requirement: ServerAuthoritative 握手必须锁定 Program 与 Pipeline 兼容 Pair

Authority Host与Client MUST使用相同ProgramHash、LayoutHash、operation-set与TickRate。Prediction PipelineHash与Authority PipelineHash MAY不同，但 MUST由同一model protocol明确声明为兼容pair，并分别锁定Backend、AuthorityHostProfile、SolverId/version/capabilities/features、WorldId、MapId、WorldRevision与WorldConfigurationHash。要求同构Solver的HostProfile MUST另外锁定相同NavigationSurfaceArtifactHash与QueryProfileHash。External Unity Worker通过Outer register提交identity，InProcess DotRecast Authority Scene通过Inner register提交identity；Room MUST降低为同一Authority identity，MUST不以route kind、显示名、asset path、仅ProgramHash或Client请求猜测兼容性。

#### Scenario: Program相同但Navigation World不同

- **WHEN** Client Program与Pipeline pair匹配但NavigationSurfaceArtifactHash不同
- **THEN** join MUST失败并返回明确world identity错误
- **AND** Client MUST不要求Room切换Host或在本地改用CC Solver

## REMOVED Requirements

### Requirement: ServerAuthoritative 模型必须显式区分 Fantasy Room 与 Unity Authority Worker

Fantasy .NET进程 MUST只拥有连接、Room、roster、sequence校验与消息路由；Unity Authority Worker MUST拥有 Program、canonical roster、Authority Pipeline、WorldSolver与权威 state。Worker MUST以专用 process role注册并锁定 identity，普通 Client MUST不能提升为 Worker。Worker断开时当前 Room MUST fail-stop，不得由 Client接管 authority。

#### Scenario: Worker 注册 Room

- **WHEN** Unity Authority Worker提交完整 Program/Pipeline/Solver identity
- **THEN** Room MUST锁定唯一 Worker connection
- **AND** clients MUST只在该 identity与自己的 Prediction组合兼容后进入 Active

## ADDED Requirements

### Requirement: ServerAuthoritative 模型必须显式区分Gate Room与Authority Host

Fantasy Gate Scene MUST只拥有Client control connection、Room、roster、ticket、可靠事务路由与失败传播。Authority Host MUST拥有Program、canonical roster、portable Authority Source/Pipeline、gameplay datagram endpoint、WorldSolver与权威state。Authority Host MAY是外部Unity Worker或同一Fantasy Server进程内的独立DotRecast Authority Scene；同一OS进程 MUST不消除Scene级所有权。Room MUST锁定唯一Host route，普通Client MUST不能提升为Host。Host断开或Authority Scene失活时当前Room MUST fail-stop，不得由Client接管、在Active中切换Host或启动fallback。

#### Scenario: DotRecast Authority Scene注册Room

- **WHEN** DotRecast Authority Scene通过Inner route提交完整Program/Pipeline/Host/Solver/World identity
- **THEN** Room MUST锁定唯一InProcess Authority Scene Address与Data endpoint
- **AND** clients MUST只在本地Prediction组合与该identity兼容后进入Active

#### Scenario: Unity Authority Worker注册Room

- **WHEN** Unity Authority Worker通过Outer route提交完整Program/Pipeline/Host/Solver/World identity
- **THEN** Room MUST锁定唯一External Worker Session与Data endpoint
- **AND** 同一Room MUST不再接受InProcess Authority Scene注册

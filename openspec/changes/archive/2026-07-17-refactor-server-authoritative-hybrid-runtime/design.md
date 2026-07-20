## Context

完成 `refactor-gameplay-session-composition-boundary` 后，Local 已由以下唯一组合运行：

```text
Float32 Program Runtime
+ Float32 Pass Backend
+ Standard Local Pipeline
+ Local Session Source
+ Unity CharacterController Solver
-> SimulationSessionHost
```

ServerAuthoritativeHybrid 不应替换 Common Host，也不应把所有网络语义塞回一个 Driver。它需要在相同 composition 合同下提供两种显式 Session：客户端 Prediction Session和 Unity Worker Authority Session。

当前 WorldSolver只有 Unity CharacterController 实现，因此普通 Fantasy .NET进程不能成为 gameplay authority。Fantasy进程负责连接与 Room；单独 Unity进程负责权威模拟。后续 DotRecast/C# authority会替换 worker Host/Solver，但继续复用同一模型协议和 Authority Pipeline语义。

## Goals

- 保持 Local Pipeline完整独立、可运行且不携带网络成本。
- 让 owner prediction、server correction和 replay通过正式 Pipeline Pass表达。
- 让 Authority Worker独立执行相同 Corin Program，而不是回显客户端位移。
- 让 Fantasy Room只拥有连接、身份、队列和路由，不成为第二 gameplay runtime。
- 让客户端与权威端使用不同 PipelineHash但相同 ProgramHash，并明确兼容 pair。
- 让 correction恢复完整 gameplay state，不把 pose correction伪装成 reconciliation。
- 让 remote actor只走权威复制到 Presentation，不运行伪 owner逻辑。
- 删除旧 Driver、LocalLoopback和手写 packet双路径。

## Character State ABI Boundary

Prediction Pipeline与Authority Pipeline只在标准Finalize成功后取得committed Character State。Prediction History在本地保存`character-state/float32/v3`产生的一份canonical bytes、StateHash、NumericProfile、Target ABI、ProgramHash、LayoutHash与codec identity。Authority Egress从同一committed state生成覆盖全部committed slot的Network Checkpoint；routine网络payload不得直接复用本地State codec bytes。任一Capture不得读取`Float32CharacterStateTransaction`、`PendingCharacterEvaluation`、typed mutable page或GameplayEffect working view。

Client先以Network Checkpoint codec重建并校验完整committed State，再由Baseline Merge构造当前Prediction PipelineHash下的restore snapshot。Restore/Replay的每个内部Step都由Kernel重新Begin自己的State Transaction；Transaction不跨Tick、不进入History、不进入packet，也不在Authority与Prediction进程之间传递。旧Builder、旧Bytes value、旧codec version与兼容decode不得进入本change。

## Process Topology

```text
Fantasy .NET Process
  Gate Scene
  ServerAuthoritativeRoomRegistry
  ServerAuthoritativeRoom
    AuthorityWorkerConnection
    Player A Connection -> Actor A
    Player B Connection -> Actor B
    ticket/control/reliable route validation

Unity Authority Worker Process
  Fantasy Worker Connection
  Gameplay UDP Endpoint
  SimulationSessionHost
  Authority Session Source
  Authority Pipeline
  Float32 Program Runtime / Pass Backend
  UnityCharacterControllerWorldSolver
  Actor A + Actor B canonical roster

Unity Client A Process
  Fantasy Player Connection
  Gameplay UDP Endpoint
  SimulationSessionHost
  Prediction Session Source
  Prediction Pipeline
  Actor A predicted simulation
  Actor B remote presentation

Unity Client B Process
  Fantasy Player Connection
  Gameplay UDP Endpoint
  SimulationSessionHost
  Prediction Session Source
  Prediction Pipeline
  Actor B predicted simulation
  Actor A remote presentation
```

Fantasy server、Authority Worker和两个客户端是四个独立 lifecycle owner。任何一个 Client都不能把自己提升为 Authority Worker；Room同一时刻只接受一个匹配 launch identity的 worker。

## Isolated Test Scenes

```text
Network Test Bootstrap Scene
  TestScenarioId = ServerAuthoritativeClient | UnityAuthorityWorker
              |
              +-> ServerAuthoritative Client Scene
              |     Client A/B share Scene
              |     explicit launch role chooses PlayerId/ActorId
              |
              +-> Unity Authority Worker Scene
                    explicit Authority Worker composition
```

Bootstrap Scene只执行单场景跳转，不创建或持有Session、Endpoint、Program Runtime、Backend、Pipeline、Source、Solver或Actor registration。Client A/B可以复用同一个Client Scene，但必须分别使用Client A/Client B launch definition；不能根据启动顺序、对象名称或默认endpoint推断身份。

Client Scene和Authority Worker Scene分别显式引用完整Composition、launch profile、Actor/出生点、World binding和diagnostics。切换或返回Bootstrap时必须先释放当前`SimulationSessionHost`、Actor registration、Fantasy Endpoint和模型队列，再由新Scene创建新Session；这些对象不得通过`DontDestroyOnLoad`跨Scene存活。Scene跳转不构成Active Session热切换，也不得提供运行时Model下拉或Local fallback。

Client Scene中可见且参与Gameplay阻挡的静态障碍必须同时存在于Authority Worker的World binding中。Demo墙体由两个场景引用同一个`wall.prefab`及相同Transform，碰撞尺寸只在该正式Prefab中定义；不得让Client独占可见墙体，也不得在Worker复制一份独立BoxCollider配置。最终是否穿透只由Authority Session中的Unity WorldSolver裁决。

### Network Test Player构建与启动

普通产品Build Settings仍可保留项目Bootstrap作为第一场景，不能为了网络Demo永久改变产品启动入口。ServerAuthoritative测试通过独立Editor菜单构建专用Player，明确传入且只传入以下场景顺序：

```text
1. ServerAuthoritativeNetworkTestBootstrap
2. ServerAuthoritativeClient
3. ServerAuthoritativeAuthorityWorker
```

仓库内启动脚本负责启动Fantasy Server、Authority Worker、Client A和Client B。脚本必须在启动前拒绝比正式源码更旧的Player/Server产物，在启动后检查四个进程均未提前退出，并确认三个Unity进程都建立网络endpoint；任一条件失败时必须终止本次新进程并报告具体缺口，不能打印四进程已启动的假成功信息。

## Composition Definitions

### Client Prediction Composition

```text
Float32ProgramRuntimeDefinition
+ Float32PassExecutionBackendDefinition
+ ServerAuthoritativePredictionPipelineDefinition
+ ServerAuthoritativePredictionSessionSourceDefinition
+ UnityCharacterControllerWorldSolverDefinition
```

Prediction Source在Preparing阶段完成Fantasy control连接、join、roster、ticket和worker UDP handshake。Ready LaunchPlan中的simulation roster只包含当前owner actor。Remote roster作为模型专属presentation registration进入Source output，不进入owner Program Evaluate。

### Authority Worker Composition

```text
Float32ProgramRuntimeDefinition
+ Float32PassExecutionBackendDefinition
+ ServerAuthoritativeAuthorityPipelineDefinition
+ ServerAuthoritativeAuthoritySessionSourceDefinition
+ UnityCharacterControllerWorldSolverDefinition
```

Authority Source在Preparing阶段注册worker、发布UDP endpoint、取得完整两Actor roster与ticket并锁定control/data route。Authority Session在一个world中按稳定ActorId顺序batch solve，产生唯一canonical Character/World state。

## Pipeline Definitions

### Prediction Pipeline

```text
Ingress
  OwnerInputIngressPass
  ServerAuthoritativeObservationIngressPass

Schedule
  PredictionCorrectionSchedulePass

Step
  Float32ProgramEvaluatePass
  Float32WorldResolveBatchPass
  Float32ProgramFinalizePass

Egress
  PredictionHistoryEgressPass
  PredictionOutputDispositionPass
  CommandDatagramEgressPass
  RemotePresentationEgressPass
```

Pass职责：

| Pass | 输入 | 输出 | 状态 |
|---|---|---|---|
| OwnerInputIngress | local input port、source tick | OwnerCanonicalInput | Stateless |
| AuthoritativeObservationIngress | Source receive port | AuthoritativeObservationBatch | Stateless |
| PredictionCorrectionSchedule | input、observation、history query | ExecutionPlan、CorrectionDecision | SnapshotParticipant |
| PredictionHistoryEgress | finalized step、ack/baseline | predicted input/state history | SnapshotParticipant |
| PredictionOutputDisposition | replay/current outputs、EventId journal | OutputDispositionSet | SnapshotParticipant |
| CommandDatagramEgress | owner input/sequence/snapshot ack | SourceEgress | Stateless |
| RemotePresentationEgress | remote replication batch | committed remote output | Reconstructible |

Prediction history与output journal可以位于同一个 canonical Pipeline snapshot聚合中，但必须由各自 PassId独立编码，不能藏在 Source connection或MonoBehaviour。

### Authority Pipeline

```text
Ingress
  AcceptedCommandIngressPass

Schedule
  AuthorityTickSchedulePass

Step
  Float32ProgramEvaluatePass
  Float32WorldResolveBatchPass
  Float32ProgramFinalizePass

Egress
  AuthorityReplicationEgressPass
```

Authority schedule只产生 `Authoritative` step。一次 authority source tick可以 Pending或执行一个 SimulationTick；本 change不让 authority worker在一个 outer tick追赶任意无界 Tick。若输入暂缺，使用模型配置中唯一、明确的 missing-input policy：连续 move/facing值在有界 hold window内沿用最后 accepted sample，离散 request永不重复；超过窗口后使用显式 neutral input。该 policy属于 Authority Source/Pipeline配置并进入 PipelineHash。

## Model Products

模型新增下列 versioned产品：

- `OwnerCanonicalInputBatch`：ActorId、target authority tick、input sequence、CharacterSimulationInput与request identity。
- `AuthoritativeObservationBatch`：重建后的baseline、ack、remote snapshot、reliable facts及接收顺序。一次Prediction Ingress对每个owner最多提交一个baseline；若帧间积累多个已校验snapshot，Source只提交authority tick最新的owner baseline用于Correction，同时保留期间全部remote body、producer和reliable event时间序列。
- `AuthoritativeActorBaseline`：由Network Checkpoint重建的owner完整committed Character state、owner body/world baseline、Program/Layout/checkpoint identity、authority tick、state/body hash、confirmed EventId horizon；该产品不是wire payload。
- `PredictionCorrectionDecision`：NoCorrection、RestoreReplay或HardRecovery及原因、restore tick、replay range。
- `AcceptedAuthorityInputBatch`：Authority Worker相对自身时钟校验后的每Actor canonical input和sequence。
- `AuthorityReplicationBatch`：每Actor checkpoint candidate、ack、remote snapshot sample和reliable fact/event batch，由Source按UDP/KCP职责拆分发送。
- `RemotePresentationBatch`：remote body samples、producer commands、facts和EventId。

每个产品都必须声明稳定 ProductId、schema version、canonical排序和diagnostics shape。Fantasy generated message与datagram bytes只是运输编码；Pipeline product不直接持有Fantasy Session、socket或message对象。

## Prediction And Correction

### History

Prediction History Pass按 SimulationTick保存：

- owner canonical input和input sequence。
- committed Character state。
- owner World/body state。
- Prediction Pipeline snapshot。
- state/body hash。
- committed EventId disposition journal cursor。

History容量由ModelDefinition显式配置并进入Pass config hash。达到容量时只删除已被authority ack确认且不再参与replay的最旧记录；如果未确认记录无法容纳，Session明确失败或请求formal hard recovery，不能静默覆盖仍需重放的数据。

### Baseline不是跨Pipeline Snapshot

Authority PipelineHash和Prediction PipelineHash不同，Authority Session snapshot不能直接恢复到Client Session。worker发送Full/Delta Network Checkpoint，client重建`AuthoritativeActorBaseline`后，correction Pass按以下顺序生成本地完整restore directive：

1. 精确找到同authority tick的本地Prediction history record。
2. 校验ProgramHash、LayoutHash、ActorId、Tick和baseline schema。
3. 用authority Character state和owner body baseline替换本地snapshot中权威拥有的部分。
4. 按Prediction Pass正式merge/reconstruct规则恢复history cursor、ack和EventId journal。
5. 重新编码为当前Prediction PipelineHash下的完整Character/World/Pipeline restore snapshot。
6. 生成Replay steps和Current step。

禁止把Authority Pipeline snapshot改写PipelineHash后直接使用，也禁止只写Transform/body后继续旧Action/Timeline/GE state。

### Correction Decision

```text
authoritative baseline T
  |
  +-- identity invalid -> Session Failed
  |
  +-- exact state/body match -> acknowledge T, no restore
  |
  +-- history covers T -> Restore T + Replay T+1..latest + Current
  |
  +-- history does not cover T -> formal HardRecovery at T
```

Gameplay state使用canonical state hash比较；Body使用模型配置的position/yaw tolerance决定是否需要视觉纠偏，但body tolerance不能掩盖Character state hash差异。HardRecovery会清除无法证明有效的unacked history，以baseline建立新的Prediction snapshot并继续后续输入；它不是Local fallback，也不直接写visual Transform。

### Output Disposition

- Replay step的Gameplay/Presentation/Network output先进入working transaction，不能逐step外发。
- 已提交EventId在journal中命中时标记SuppressDuplicate。
- 当前最终Body/Animation持续状态按reconciled state重新提交。Owner Presentation维护独立于outer tick alpha的simulation sample时钟；restore/replay覆盖旧预测分支时，它保留上一帧可见姿态，并在6个simulation tick时长内只在visual root上收敛到新canonical body。该恢复不修改World body、Prediction state或后续Solver输入。
- 新EventId可以在最终transaction提交。
- 已经播放但后来被权威否定的one-shot Cue不倒放；记录PredictedRejected diagnostics。持续性owner状态必须通过新select/release/body sample纠正。
- Authority worker的reliable fact/event batch按EventId去重并经KCP单次发送；UDP remote snapshot不得挤占可靠队列。

## Authority Worker

Authority Worker不是Fantasy Room的线程，也不是普通Client owner。它拥有：

- 独立Unity Player lifecycle和launch role。
- 一个Authority SimulationSessionHost。
- 两Actor canonical simulation roster。
- 同一Corin ProgramAsset canonical bytes。
- Authority Pipeline和Unity Solver。
- worker heartbeat、accepted input receive port和replication send port。

worker从ProgramAsset内嵌canonical bytes加载Program；Unity Player不读取`Library/*.csim`。Room通过worker register消息锁定ProgramHash、LayoutHash、operation-set、TickRate、Authority PipelineHash、Backend和Solver capability。客户端join时必须匹配Program identity，并携带协议声明的Prediction PipelineHash；Room验证该pair已由worker/model manifest声明兼容。

worker断开时Room关闭当前gameplay Session并通知客户端失败；不选举客户端、不切换Local、不接受client pose。

## Fantasy Room And Protocol

### Ownership

Room位于Fantasy Gate Scene，由Scene-owned RoomRegistry创建和释放。当前Demo只允许一个固定Room和两个player slot，不引入SubScene、匹配或分布式Room服务。

Room拥有：

- RoomId和protocol version。
- 唯一AuthorityWorker connection。
- PlayerId到owned ActorId映射。
- 最新worker authority tick和公开data endpoint。
- 每个player的一次性session ticket与ticket lifecycle。
- 有界可靠事务队列。
- join/leave/worker failure lifecycle。

Room不拥有Program对象、Character state、World state、Prediction history、correction threshold、command queue、snapshot baseline或Presentation，也不转发高频gameplay datagram。

### Outer Messages

正式Outer协议包含：

- `AuthorityRegisterRequest/Response`，注册完整worker、Program、Pipeline、Backend、Solver、tick和data endpoint identity。
- `ClientJoinRequest/Response`与`RosterChanged`，锁定固定双Actor ownership。
- `DataPlaneTicket`与ticket revoke，授权精确Room/Session/Player/Actor连接worker UDP endpoint。
- `ReliableGameplayEventBatch`，承载必须可靠到达的Action/Effect/Cue EventId事实，每批只通过KCP发送一次。
- `FullCheckpointRequest/Response`，用于初始化、baseline丢失、布局重置或delta超限。
- `SessionFailed/Leave`。

Command和Snapshot不进入Fantasy Outer协议；它们使用model-owned datagram codec。KCP heartbeat只判断control session与worker lifecycle，不能代替snapshot age或command lead诊断。

### Control Plane与Gameplay Data Plane

本模型只安装以下两条正式链路：

```text
Fantasy KCP control/reliable
  register -> join -> roster -> ticket -> reliable event/full checkpoint/failure

Client <-> Authority Worker UDP gameplay data plane
  redundant command datagram -> per-actor command queue
  per-client delta snapshot datagram <- authority output
```

Room先向worker下发ticket，再把worker endpoint与同一ticket交给精确client。Client发送`DataPlaneHello`，worker校验Room/Session/Player/Actor/ticket/nonce后锁定remote endpoint并返回`DataPlaneHelloAck`。ticket只使用一次且有过期时间；unknown、reused、actor mismatch或endpoint change直接拒绝。当前本地Demo不实现NAT穿透、Internet relay或断线续连。

UDP连接失败、datagram长期不可达或ticket失效使Session失败，不将command/snapshot回退到KCP。Fantasy connection失败同样使Session失败，不保留半连接数据面。

### 独立频率与时钟纪律

Corin正式配置为：

| 策略 | 值 | 业务含义 |
|---|---:|---|
| SimulationTickRate | 60Hz | Program、Pipeline和WorldSolver固定步进 |
| CommandPacketRate | 30Hz | 每包冗余当前及前3个input sample |
| SnapshotPacketRate | 20Hz | owner correction与remote presentation采样 |
| CommandSlackTicks | 3 | client prediction目标领先authority的tick数 |
| RemoteInterpolationDelayTicks | 6 | remote表现默认缓冲100ms |
| MaxGameplayDatagramBytes | 1200 | UDP command/snapshot不分片预算 |

这些配置独立进入model configuration identity，不存在统一`ObservationCadenceTicks`。

握手由worker返回当前authority tick与clock sample。Prediction Schedule把本地预测tick映射到target authority tick并维持`CommandSlackTicks`：正常产生一个Current step，领先不足时可产生两个Current step，领先过多时可产生零个Current step。零步/双步是有界时钟校正，不建立第二Update或私有simulation runner；Replay仍由同一个Schedule生成。Owner Presentation不再用outer tick interpolation alpha直接假设本次一定完成一个simulation step，而是缓存Committer提交的body sample并按presentation delta推进自己的sample时钟：零步时停在当前终点，双步时按顺序消费两个区间。

每个预测outer tick都生成一个不可变输入采样。若Schedule本次产生零Current step，连续值可在下一outer tick重新采样，但Attack、Dodge、Combo等离散请求必须进入Correction Schedule的pending request状态；该状态进入checkpoint、canonical pipeline snapshot和restore，直到下一次首个Current step消费。若一次产生两个Current step，离散请求只属于第一步。30Hz command datagram包含packet sequence、最新target authority tick、最近snapshot ack，以及当前预测分支的当前和前3个input sample。Prediction Correction回滚后若新的input sequence复用或回退到已保留的target authority tick，Source先删除该tick及之后的旧分支样本，再加入新分支样本；同一datagram不得混合旧、新预测分支。重复发送同一分支样本时保持相同input sequence与离散request identity，worker按sequence去重。

Authority Worker按自己的60Hz时钟持续推进，绝不等待双Actor共同horizon。每个Actor拥有按target authority tick索引的独立command queue。worker以当前authority tick校验lead/lag；当前tick没有样本时，连续move/facing只在有界hold window沿用，Attack/Dodge/Combo等离散请求清空，超出窗口后使用neutral input。

Remote Presentation收到首个Body sample时不得立即启动表现时钟。它先保持最早可用Body，直到缓冲内`LastTick - FirstTick`达到`RemoteInterpolationDelayTicks`；随后从`LastTick - RemoteInterpolationDelayTicks`启动，并仅由每帧presentation delta推进。这样20Hz snapshot的到达抖动由6 tick缓冲吸收，60Hz Logic Tick与渲染帧仍保持独立。

### Network Checkpoint与Snapshot

`character-state/float32/v3`继续用于本地Prediction History和完整状态语义，但不再作为routine网络payload。Model preparation根据ProgramHash/LayoutHash生成稳定`NetworkCheckpointLayout`，为全部committed Character state slot分配dense index与固定value codec；不得通过复制policy省略Action、Timeline、Blackboard或GameplayEffect slot。

Full checkpoint包含完整dense values、owner body/world基线、Program/Layout/checkpoint schema、authority tick、state/body hash、input/event确认边界。它只经KCP用于初始化、baseline丢失、布局重置或delta超限。

Routine snapshot使用UDP并包含：

- SnapshotSequence、AuthorityTick和acked InputSequence。
- BaseSnapshotSequence与client已确认baseline identity。
- owner changed-slot bitset与changed values。
- owner body/world correction sample与state/body hash。
- remote body、producer sample time和连续presentation state。
- reliable event horizon，但不重复可靠event payload。

worker只相对client已经确认的baseline编码delta。Client必须先重建完整authoritative checkpoint并校验hash，再把它降低为现有`AuthoritativeActorBaseline`供Correction Schedule使用。未知base或hash失败触发`FullCheckpointRequest`；单纯SnapshotSequence缺口不触发恢复，只要后续delta仍引用本地已知base即可继续重建。未收到新ack时worker相对最后已确认base持续发送新delta，不使用single-flight snapshot门闩阻塞后续状态。

Command与snapshot datagram必须不超过1200 bytes且不得UDP分片。delta超限时worker停止发送该delta，标记checkpoint required，并经KCP发送full checkpoint；不得把超大payload切成无界UDP fragments。

### 可靠业务事实

Action、Effect和Cue继续以稳定EventId表达预测提交、权威确认和remote一次性表现。Authority经KCP发送一次`ReliableGameplayEventBatch`，Room只做精确owner/remote路由；EventId用于业务去重和rollback disposition，不在可靠KCP之上再做每snapshot重发。

可靠event携带原始authority tick和event sequence。Client将remote body、producer command和reliable event都按authority tick写入同一remote presentation时间域，并在`RemoteInterpolationDelayTicks`形成的horizon到达后发布；不得让事件或producer反向修改Gameplay state。队列溢出仍fail-stop。

协议在`3cDemo/Tools/NetworkProtocol/Outer`定义，经现有ProtocolExportTool生成Client/Server控制消息。Datagram codec位于portable ServerAuthoritative模型模块，由Unity client/worker和后续兼容authority host复用，不复制手写DTO。

### Routing And Queue

- Room控制消息必须匹配Fantasy connection拥有的PlayerId/ActorId。
- Worker gameplay datagram必须匹配已消费ticket锁定的remote endpoint与ActorId。
- Command packet sequence或sample sequence重复、回退以及target authority tick越过相对authority clock边界时明确丢弃并记录原因；Prediction Source内部因Correction形成的target tick回退必须先替换旧预测分支，再构造严格有序datagram。
- 每Actor command queue和每Client snapshot baseline独立有界；一个Actor的迟到或积压不得阻塞另一个Actor。
- action/effect/cue/roster/failure/full checkpoint等可靠事务不得静默丢弃。
- reliable queue overflow、checkpoint恢复失败或持续datagram超限使Session fail-stop。

## Remote Presentation

客户端只对owner创建Prediction simulation Actor。roster中的remote actor由Source创建model-owned remote presentation registration，绑定Corin Projection、visual root和output port。

Authority worker发送：

- replaceable Body samples用于插值。
- reliable producer select/release、Action/Effect/Cue facts及EventId。

RemotePresentationEgressPass在Prediction Pipeline最终Commit边界把这些输出提交给既有`CharacterSimulationPresentationRuntime`。它不创建remote CharacterSimulationState、不运行remote Program、不注入伪input，也不直接调用Animancer或写Transform。

Remote body registration在缓冲尚未形成完整interpolation horizon时保持最早样本，不追赶最新snapshot；缓冲形成后才选择相邻authority tick的Body并按presentation time插值。SampleProducer可以提前发布到当前Body区间的右端tick，只用于缓存该producer的Timeline sample；Select/Complete/Release和GameplayFact仍只在authority presentation time到达后发布。Animation sampling按前后稀疏sample tick插值，不把20Hz sample间隔当作过期后自由运行。整个过程不建立按snapshot直接跳Transform的旁路。

remote spawn/despawn只在Preparing roster或Session终止发生；本change不实现Active动态join/leave。任一玩家掉线终止当前demo Room，而不是热改roster。

## Lifecycle

### Authority

```text
Launch role Authority
-> Connect Fantasy
-> Register worker identity
-> Receive locked roster
-> Build Authority LaunchPlan
-> Active authority ticks
-> Room/session failure or dispose
```

### Client

```text
Launch role ClientA/ClientB
-> Connect Fantasy
-> Join fixed Room
-> Wait worker + complete roster
-> Validate Program/Pipeline pair
-> Build Prediction LaunchPlan + remote presentation registrations
-> Active prediction/correction ticks
-> Room/session failure or dispose
```

Preparing由GameplayTickSystem正式Session target推进，不建立额外Update、Task loop或Coroutine。Fantasy callback只写Source receive queue；Pipeline在LogicTick边界消费。

## Diagnostics

Diagnostics统一记录：

- process role、RoomId、SessionId、PlayerId、ActorId。
- ModelId、EndpointId、protocol version。
- ProgramHash、LayoutHash、Prediction/Authority PipelineHash、Backend、Solver。
- local source tick、authority tick、SimulationTick、input sequence、ack cursor。
- control/command/snapshot/reliable各自packet/s、bytes/s、payload bytes和queue depth。
- control heartbeat outstanding、应用层可靠/full checkpoint队列压力、UDP丢包/乱序、datagram超限、RTT和jitter；不反射Fantasy内部KCP发送窗口。
- command lead、clock correction、snapshot age、baseline命中与remote interpolation occupancy。
- state hash match、body error、correction decision、restore tick、replayed ticks。
- hard recovery、duplicate EventId、predicted rejected one-shot和Session failure。

Diagnostics只读，不持有packet queue或history，不改变Pass policy。

## Industry Reference And Scope

- Valve Source公开模型把server simulation、client command packet与snapshot packet分开，command packet携带多个user command，snapshot使用delta并由remote interpolation消费。本设计采用独立60/30/20Hz、冗余command和delta snapshot，不复制Source的引擎实现。[Source Multiplayer Networking](https://developer.valvesoftware.com/wiki/Source_Multiplayer_Networking)
- Unreal Character Movement使用SavedMove合并、server ack/correction与client replay。本设计保留现有Prediction History和Correction Schedule，并把clock slack与网络checkpoint接到同一正式Pipeline，不引入Unreal专属CharacterMovement组件。[Understanding Networked Movement](https://dev.epicgames.com/documentation/en-us/unreal-engine/understanding-networked-movement-in-the-character-movement-component-for-unreal-engine)
- Unity Netcode for Entities允许simulation tick与network tick不同，使用command slack和冗余command，并区分predicted owner与interpolated remote。本设计采用这些时钟与角色职责，不引入Entities/Ghost运行时。[Introduction to Prediction](https://docs.unity.cn/Packages/com.unity.netcode%401.5/manual/intro-to-prediction.html)
- Ubisoft公开资料只证明For Honor迁移dedicated server后消除了host migration与NAT依赖，没有公开其command/snapshot wire protocol。本change不把For Honor当成具体协议证据。[For Honor Dedicated Servers](https://www.ubisoft.com/en-us/game/for-honor/news-updates/2JYwgPXpb5XTEc0rPBJBwz/season-five-announced-dedicated-servers-and-hero-updates-coming)

这些资料只用来校验职责与时钟划分。Corin的完整Gameplay restore、Program/Layout identity、Timeline/Action/GE状态和EventId disposition仍由本项目现有Program/Pipeline合同决定。

## Failure Policy

- Composition基座未完成或公共合同仍在变化：停止本change实施。
- Program/Layout/operation-set/TickRate不匹配：worker register或client join失败。
- Prediction/Authority Pipeline pair不匹配：client join失败。
- worker缺失、重复或断开：Room和clients fail-stop。
- Actor route、owner connection、sequence、tick范围不合法：Room拒绝消息并返回ErrorCode。
- ticket、UDP endpoint或datagram identity不合法：worker拒绝数据面消息；持续失败终止Session。
- command缺失：按Actor独立执行有界continuous hold、离散清空和neutral策略，不等待另一Actor。
- snapshot base未知、hash失败或delta超限：请求可靠full checkpoint；恢复前不应用不完整correction。
- baseline identity不完整或state bytes无法解码：client Session失败，不做pose-only correction。
- history覆盖但restore/replay失败：outer transaction失败且不提交部分state/output。
- history不覆盖：执行正式HardRecovery；若baseline也不能构造完整restore，Session失败。
- reliable queue overflow：Session失败；replaceable stream只替换同Actor同kind旧样本。
- Fantasy连接失败：Source preparation Failed，不回退LocalLoopback或Local Pipeline。
- Active后修改Program、Pipeline、Source、Solver、roster或launch role：拒绝热切换。
- Scene卸载后仍存在旧Session、Actor registration、Endpoint或模型队列：新Scene拒绝创建Session并报告lifecycle owner冲突。

## Rejected Alternatives

### 继续实现一个ServerAuthoritative Driver

Driver无法正式表达多step replay、Pass state snapshot、产品所有权和output disposition，并且基座会删除旧Driver合同，因此拒绝。

### 让Fantasy .NET进程直接运行UnityCharacterController

普通.NET进程没有UnityEngine和场景physics world。这样做要么无法编译，要么需要隐藏Unity进程桥接，因此拒绝。当前显式使用Unity Authority Worker。

### 让客户端发送resolved displacement给Room做权威pose

这只是client-authoritative pose relay，无法独立裁决Action、Timeline、GE和world constraint，因此拒绝。

### 将Authority Pipeline snapshot直接恢复到Prediction Pipeline

两个PipelineHash和Pass state不同，直接互换会破坏Snapshot identity。使用model-owned baseline加正式merge/reconstruct规则。

### 保留LocalLoopback便于调试

Standard Local Pipeline已经覆盖无网络本地运行；LocalLoopback会形成第二个伪ServerAuthoritative闭环并掩盖Fantasy/worker缺失，因此删除。

### Remote Actor也运行owner prediction

会增加双算、伪input和纠偏冲突，且remote只需要权威表现。remote只消费committed replication output。

### 所有Gameplay消息继续走单条Fantasy KCP

实现最少，但大checkpoint或丢包重传会阻塞更新的command与snapshot，无法为动作Demo提供可解释的延迟边界，因此只保留KCP控制与可靠事务。

### 让Fantasy Room中继所有UDP Gameplay数据

可隐藏worker endpoint，但会保留额外hop、拷贝与Room高频队列，并让后续DotRecast authority继续依赖Gate数据面。当前固定本地Demo选择ticket授权后的Client直连worker。

### 继续发送完整Character State canonical bytes

该格式包含本地codec identity并服务存档/Hash语义，不具备网络delta和MTU约束。网络使用Program/Layout锁定的dense checkpoint codec，Correction前再恢复为完整committed state。

## Migration And Deletion

1. 完成并锁定composition基座。
2. 固定旧ServerAuthoritative代码/资产inventory和删除清单。
3. 建立model产品、Source、Pass、Pipeline和SnapshotParticipant。
4. 建立Fantasy控制协议、Room ticket和可靠路由。
5. 建立direct UDP command/snapshot数据面、时钟纪律与checkpoint codec。
6. 建立Unity Authority Worker composition。
7. 建立Client Prediction composition、correction和remote presentation。
8. 一次切换ModelDefinition到新Source/Pipeline/Endpoint资产。
9. 删除旧KCP gameplay frame、统一cadence、共同horizon、完整state routine baseline及旧Driver路径。
10. 静态确认运行时Gameplay只有正式Local、Prediction和Authority三种显式Pipeline组合，且ServerAuthoritative只有一条control/reliable和一条UDP gameplay数据链。

最终不得保留旧wrapper、兼容constructor、LocalLoopback、手写协议镜像、dual packet adapter、Transform correction或model-specific SessionHost。

## Delivery Boundary

本change完成时，代码和资产必须达到可由用户构建并启动Fantasy server、Authority Worker、Client A与Client B进行端到端测试的状态。由于禁止Unity batchmode，本change不自动生成Player build，也不把人工运行步骤写入tasks；实施者必须完成非batchmode静态编译、协议生成、资产引用和strict validation，并明确列出仍需用户在Unity中验证的运行行为。

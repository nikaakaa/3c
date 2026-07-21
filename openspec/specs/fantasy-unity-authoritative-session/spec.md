# fantasy-unity-authoritative-session Specification

## Purpose
定义 Fantasy Gate/Room、外部 Unity Authority Worker 与 Unity Client 之间的控制面、UDP Gameplay 数据面、固定 Roster 和四进程权威 Session 生命周期。
## Requirements
### Requirement: Fantasy Room与Unity Authority Worker必须是明确的不同进程

本地ServerAuthoritative纵切 MUST由一个Fantasy .NET Gate/Room进程、一个Unity Authority Worker进程和一个或多个Unity Client进程组成。Fantasy进程 MUST只拥有控制连接、Room、roster、ticket和可靠事务路由；Unity worker MUST拥有权威Program、Pipeline、WorldSolver、canonical state、每Actor command queue和snapshot baseline。系统 MUST不假装Fantasy进程可以调用UnityCharacterController。

#### Scenario: 启动Authority Session

- **WHEN** Unity worker连接Fantasy Room并完成register
- **THEN** Room MUST保存worker connection和locked identity
- **AND** gameplay simulation MUST只在worker的Authority Session推进

### Requirement: Worker注册必须先于Client Active并锁定权威Identity

Authority Worker register MUST提交RoomId、process role、protocol version、ProgramHash、LayoutHash、operation-set、TickRate、Authority PipelineHash、Backend、Solver capability和gameplay data endpoint。Room MUST只接受一个完整且匹配的worker。Client MAY连接和等待，但在worker与完整roster就绪前 MUST不创建Active Prediction runtime或伪造Local gameplay。

#### Scenario: Client先于Worker连接

- **WHEN** Client join时Room尚无合法worker
- **THEN** join/preparation MUST保持Pending或返回明确未就绪错误
- **AND** MUST不创建Local Session代替

### Requirement: Fantasy Room必须拥有精确控制路由与数据面Ticket

Room MUST显式保存AuthorityWorker connection、PlayerId、owned ActorId和client connection映射。Room MUST向worker与精确client签发绑定Room、Session、Player、Actor、data endpoint和过期时间的一次性ticket。Roster、可靠Event、full checkpoint与failure MUST按控制连接精确路由；高频command和snapshot MUST不经Room转发。Unknown、duplicate、stale或role mismatch route MUST返回业务ErrorCode。

#### Scenario: 普通Client发送Authority消息

- **WHEN** Player connection发送worker专属FullCheckpointResponse
- **THEN** Room MUST按process role拒绝消息
- **AND** MUST不转发给其它client

### Requirement: Authority Worker必须相对自身时钟校验每Actor输入

Authority Worker MUST为每Actor维护独立command queue，并相对当前authority tick校验packet sequence、input sequence、target authority tick和配置的lead/lag范围。duplicate、regressed、stale或过度超前样本 MUST拒绝。Room MUST不读取CharacterSimulationInput业务字段来决定Action、不执行Program、不构造correction或修改checkpoint。

#### Scenario: Input Sequence回退

- **WHEN** Actor A已接受sequence 42又收到sequence 41
- **THEN** Worker MUST拒绝该sample并记录明确drop reason
- **AND** Actor B command queue MUST不受影响

### Requirement: Fantasy协议必须使用正式Outer生成链路

Worker register、client join、roster、data-plane ticket、可靠Event、full checkpoint request/response、leave和failure MUST定义在正式Outer proto，并通过仓库ProtocolExportTool生成client/server代码。Command与snapshot MUST使用ServerAuthoritative模型自有的versioned datagram codec。实施 MUST不手写generated `.g.cs`或建立重复packet DTO，Handler MUST通过Fantasy source generator注册并使用`FTask`与ErrorCode。

#### Scenario: 修改Baseline协议

- **WHEN** AuthoritativeActorBaseline schema发生版本化修改
- **THEN** MUST修改Outer proto并重新导出两端代码
- **AND** MUST不只修改client手写DTO

### Requirement: Room可靠队列必须按Actor隔离

Roster、Action/Effect/Cue Event batch、Full Checkpoint、Session failure和其它可靠消息 MUST使用有界可靠队列；可靠队列溢出 MUST终止当前Room Session，MUST不静默丢弃或挪用其它Actor容量。Room MUST不再维护Body、routine baseline或command replaceable queue。

#### Scenario: Actor A Reliable Event频率过高

- **WHEN** Actor A reliable queue已满
- **THEN** Room MUST fail-stop当前Session并记录Event范围
- **AND** MUST不删除Actor B消息腾出容量

### Requirement: Control Plane与Gameplay Data Plane必须分离

Fantasy KCP MUST只承载register、join、roster、ticket、可靠Event、full checkpoint、failure和leave。Client与Authority Worker MUST通过ticket授权的直接UDP数据面交换command与snapshot。UDP失败 MUST使Session失败，不得把高频gameplay stream回退到KCP；Room MUST不成为command/snapshot relay。

#### Scenario: Client完成Data Plane握手

- **WHEN** Room向worker和Client A签发同一个未消费ticket
- **THEN** Client A MUST用该ticket向worker发送DataPlaneHello
- **AND** worker MUST锁定精确Player/Actor/remote endpoint后才接受command

### Requirement: Simulation、Command、Snapshot与Remote采样策略必须独立

`SimulationTickRate`、`CommandPacketRate`、`SnapshotPacketRate`、`CommandSlackTicks`、`MaximumRemoteBodyExtrapolationTicks`和`MaxGameplayDatagramBytes` MUST是独立、进入模型configuration identity的策略。系统 MUST删除统一`ObservationCadenceTicks`与Remote Presentation独立Body delay，MUST不让任一传输频率改变Program或WorldSolver固定步进。

#### Scenario: Corin使用正式Demo频率

- **WHEN** `SimulationTickRate=60`、`CommandPacketRate=30`且`SnapshotPacketRate=20`
- **THEN** Program、Pipeline和WorldSolver MUST按60Hz推进
- **AND** command与snapshot MUST分别按自己的packet cadence发送
- **AND** Prediction Schedule MUST按正式Remote Body采样策略选择target tick
- **AND** Remote Presentation MUST消费Schedule提交的selected Body

#### Scenario: Prediction建立Remote Body anchor

- **WHEN** Data Plane已Ready但locked remote roster尚无合法Body anchor
- **THEN** Prediction Schedule MUST保持RemoteObservationPriming并产生零Current step
- **AND** 首个完整anchor集合到达后 MUST按目标tick选择Body
- **AND** Remote Presentation MUST不建立独立authority Body cursor或delay

### Requirement: Authority World必须包含客户端可见的Gameplay阻挡

Client测试场景中可见且参与Gameplay阻挡的静态障碍 MUST存在于Authority Worker的World binding中。Client与Authority Worker MUST引用同一正式碰撞资产及一致Transform，MUST不分别维护独立Collider尺寸。Client可见但Authority缺失的阻挡 MUST视为无效Demo配置，不得以客户端Transform修正伪装权威碰撞。

#### Scenario: Corin接近测试墙体

- **WHEN** Client显示由正式`wall.prefab`构成的测试墙体
- **THEN** Authority Worker MUST在相同位置引用同一`wall.prefab`
- **AND** Unity Authority WorldSolver MUST以该Collider裁决最终Body
- **AND** Client MUST只表现权威结果，不得单独阻挡或放行角色

### Requirement: Authority不得等待双Actor共同Input Horizon

Authority Worker MUST按自己的60Hz authority clock持续生成双Actor World batch。每个Actor MUST独立选择当前target tick的input；一个Actor迟到 MUST不阻塞另一个Actor。缺样本时连续move/facing MAY在有界hold window保持，Attack、Dodge、Combo等离散请求 MUST清空，超出窗口 MUST使用neutral input。

#### Scenario: Client B输入迟到

- **WHEN** Authority Tick 120已有Actor A sample但没有Actor B sample
- **THEN** Authority MUST使用Actor A精确sample和Actor B missing-input policy执行Tick 120
- **AND** MUST不等待Client B或停止Actor A

### Requirement: Command Datagram必须冗余输入并使用正式Clock Discipline

每个预测tick MUST生成带精确target authority tick、input sequence和离散request identity的immutable canonical input sample。Command datagram MUST带packet sequence、最近snapshot ack以及当前预测分支的当前和配置数量历史sample；重复sample MUST保持同一identity供worker去重。Prediction Correction产生新的input sequence但复用或回退到已保留的target authority tick时，Prediction Source MUST删除该tick及之后的旧分支sample，再加入新分支sample，MUST不在同一datagram混合旧、新预测分支。握手 MUST提供authority tick基准，Prediction Schedule MUST通过有界零步/双Current step维持显式command slack，MUST不建立第二simulation runner。

#### Scenario: 一个Command Datagram丢失

- **WHEN** 下一datagram仍包含丢失包中的历史input sample
- **THEN** Worker MAY按未见过的input sequence补入对应target tick
- **AND** MUST不重复执行相同Dodge或Attack request

#### Scenario: Prediction Correction重建Command分支

- **WHEN** 已保留的Command历史包含target Tick 330至332，Correction随后生成更大input sequence并重新指向Tick 330
- **THEN** Prediction Source MUST删除旧分支的Tick 330至332 sample后加入新的Tick 330 sample
- **AND** 下一Command datagram MUST只包含严格有序的当前预测分支sample

### Requirement: Routine Snapshot必须使用有界Delta Checkpoint

Routine snapshot MUST使用ProgramHash/LayoutHash锁定且覆盖全部committed Character state slot的Network Checkpoint Layout，以已确认base snapshot为基准发送changed-slot bitset、changed values、owner body/world correction、remote body/producer、state/body hash、input ack和event horizon。它 MUST不携带逐slot codec字符串或完整`character-state/float32/v5`bytes，也 MUST不按复制policy省略Action、Timeline、Blackboard、GameplayEffect或Motion Modifier state。Client MUST重建并校验完整checkpoint后才向Correction Pipeline提交baseline。

Full Checkpoint与Delta Snapshot MUST共享单调SnapshotSequence。Worker在未收到新base ack时 MUST继续相对最后已确认base发送新delta；Client发现SnapshotSequence缺口但仍拥有该BaseSnapshotSequence时 MUST继续重建，MUST不因单帧丢失阻塞后续snapshot或无条件请求Full Checkpoint。

#### Scenario: Snapshot Base未知

- **WHEN** Client收到引用未知BaseSnapshotSequence的delta
- **THEN** Client MUST拒绝该delta并通过KCP请求Full Checkpoint
- **AND** MUST不应用pose-only correction继续旧Gameplay state

### Requirement: Gameplay Datagram必须遵守MTU且不得分片

Command与snapshot datagram MUST不超过`MaxGameplayDatagramBytes`，Corin正式值为1200 bytes。超过预算的delta MUST不经UDP分片；worker MUST切换为checkpoint-required状态并通过可靠KCP发送Full Checkpoint。Full Checkpoint MUST只用于初始化、baseline丢失、布局重置或delta超限，不得按routine snapshot cadence发送。

#### Scenario: Owner Delta超过1200 Bytes

- **WHEN** 当前owner checkpoint delta编码后超过预算
- **THEN** Worker MUST不发送分片snapshot
- **AND** MUST经可靠控制通道发送或触发Full Checkpoint

### Requirement: 可靠Event必须单次发送且保留EventId语义

Action、Effect和Cue可靠事实 MUST经KCP发送一次并携带原始authority tick、event sequence和EventId。EventId MUST继续用于预测确认、duplicate suppression和rollback disposition，但系统 MUST不在每个snapshot重复Event payload，也 MUST不在可靠KCP之上建立按snapshot ack重发循环。

#### Scenario: 两个Snapshot之间产生Attack Cue

- **WHEN** Actor A产生一个可靠Cue EventId
- **THEN** Authority MUST经KCP可靠事务链发送一次
- **AND** Snapshot MAY只推进event horizon而不得重复Cue payload

### Requirement: 网络诊断必须按通道记录容量与时延

Diagnostics MUST分别记录control、command、snapshot和reliable通道的packet/s、bytes/s、payload bytes、queue depth、control heartbeat outstanding、应用层可靠/full checkpoint队列压力、UDP丢包/乱序、datagram超限、RTT、jitter、command lead、snapshot age、baseline命中和interpolation occupancy。实现 MUST不通过反射或私有API绑定Fantasy内部KCP发送窗口，系统 MUST不再只用消息数量推断传输瓶颈。

#### Scenario: Snapshot到达频率下降

- **WHEN** Client观察到snapshot age持续增长
- **THEN** diagnostics MUST能区分worker未生成、UDP丢失、payload超限和KCP full checkpoint积压

### Requirement: Worker或Player断开必须遵守固定Roster失败策略

本change的Demo roster MUST在Active前锁定为一个worker和两个player。worker断开 MUST终止Room gameplay并通知clients；任一player断开 MUST终止当前固定roster Session。系统 MUST不选举新worker、不热增删Actor、不切换AI/Local或保留半个Room继续权威模拟。

#### Scenario: Client B断开

- **WHEN** 双客户端Session已经Active
- **THEN** Room MUST发布SessionFailed/Leave并释放当前Room
- **AND** worker与Client A MUST停止对应Session

### Requirement: Fantasy回调必须只写Source边界队列

Unity侧Fantasy callback MUST只验证消息外壳并写入Prediction或Authority Source receive queue。Program、Pipeline、Solver、History merge和Presentation提交 MUST只在GameplayTickSystem推进的正式Session runtime中执行。不得从网络callback运行SimulationTick、修改Transform或调用Animancer。

#### Scenario: Client收到Authority Baseline

- **WHEN** Fantasy push Handler接收baseline消息
- **THEN** Handler MUST将typed observation写入Source queue
- **AND** Correction Schedule MUST在后续LogicTick消费

### Requirement: Unity Player必须从ProgramAsset Canonical Bytes加载Program

Authority Worker与Clients MUST从各自build中的ProgramAsset exact-byte wrapper加载相同canonical Float32 Program。Unity Player MUST不读取Editor `Library/*.csim`路径、不加载`.csir`、不运行authoring discovery或Numeric Target lowering。Room MUST只保存和比较identity，不保存Program对象。

#### Scenario: Worker与Client ProgramHash不同

- **WHEN** Client join identity与worker locked ProgramHash不同
- **THEN** Room MUST拒绝join
- **AND** MUST不要求任一端运行时重新编译

### Requirement: Transport失败不得改变Network Model语义

Fantasy连接、worker register、join、ticket、data-plane handshake、首个snapshot、active snapshot liveness或route失败 MUST使Source preparation或Active Session进入明确Failed并释放资源。系统 MUST不回退LocalLoopback、Standard Local Pipeline、KCP gameplay stream、disconnected prediction、client-authoritative pose或其它Endpoint。

#### Scenario: Authority Snapshot超时

- **WHEN** data plane锁定后的首帧等待或Active snapshot超过配置liveness时限
- **THEN** 当前Room MUST fail-stop并通知clients
- **AND** clients MUST不继续未连接的本地预测Session

### Requirement: 四进程Demo必须具有可验证的正式启动入口

Network Test Player MUST通过显式构建入口固定Bootstrap、Client和Authority Worker场景顺序，且Network Test Bootstrap MUST是第一场景。Unity Authority Build MUST发布独立`ThirdPerson.UnityAuthority.Server`产品；该产品 MUST只包含Gate Scene、共享Gate模块与external Unity Worker route模块，MUST不包含DotRecast runtime、DotRecast Authority Scene模块或Authority Scene artifact。四进程启动入口 MUST从匹配ServerProductId和manifest启动Fantasy Server、Authority Worker、Client A和Client B，并在报告成功前验证三个Unity角色均存活且已建立网络endpoint。旧Player、旧通用`Main.exe`、错误Server Product、漏启角色或未进入网络场景 MUST fail-fast，不能作为成功的双客户端测试环境。

#### Scenario: Client B启动语句未执行

- **WHEN** 启动入口未能创建Client B进程或Client B在检查前退出
- **THEN** 启动入口 MUST报告Client B缺失并终止本次新进程

#### Scenario: Unity Demo混入DotRecast Server模块

- **WHEN** Unity Authority Build或Run发现server product manifest包含DotRecast模块、DotRecast Authority Scene或错误ServerProductId
- **THEN** Build或Run MUST在启动四进程前失败并报告具体产品闭包错误
- **AND** MUST不删除文件后继续运行或回退旧`Main.exe`

### Requirement: Unity Fantasy Endpoint必须由唯一Connection Coordinator拥有生命周期

Unity侧Fantasy endpoint MUST保持一个正式endpoint interface和唯一control/datagram网络路径，但其内部control session、datagram channel、checkpoint reconstruction、prediction evidence/metrics MUST由职责独立的内部模块实现。唯一Connection Coordinator MUST拥有endpoint state transition、共享资源、failure和dispose顺序；内部模块 MUST只接收窄输入并返回typed result/event，MUST不独立启动Simulation、切换endpoint state、释放共享session/socket或建立第二transport。

#### Scenario: Data-plane handshake完成

- **WHEN** Control Session取得ticket且Datagram Channel完成handshake
- **THEN** 两个模块 MUST向Connection Coordinator提交typed result
- **AND** 只有Coordinator MAY将endpoint推进到可接收Gameplay datagram的状态

#### Scenario: Delta checkpoint缺少baseline

- **WHEN** Checkpoint Reconstruction收到无法应用的delta checkpoint
- **THEN** 模块 MUST返回包含sequence/baseline identity的明确失败结果
- **AND** Coordinator MUST按正式Source/Model失败策略处理
- **AND** 模块 MUST不自行切换KCP gameplay stream或创建近似baseline

#### Scenario: Endpoint释放

- **WHEN** Player离开、Worker失败或Host dispose当前endpoint
- **THEN** Coordinator MUST按固定顺序停止callback ingress、释放datagram/control资源并完成Source failure
- **AND** 任一内部模块 MUST不保留独立heartbeat、socket或后台发送循环

### Requirement: Endpoint内部拆分不得改变Source Callback边界

Fantasy handler与network callback MUST继续只校验消息外壳并写入正式Source receive queue。Checkpoint merge、ack/horizon推进、Program、Pipeline、WorldSolver和Presentation MUST继续由GameplayTickSystem驱动的Session runtime消费。内部模块拆分 MUST不新增callback simulation、Transform写入或Animancer调用。

#### Scenario: Client收到routine snapshot datagram

- **WHEN** Datagram Channel收到合法routine snapshot
- **THEN** 它 MUST将typed packet/result写入Prediction Source边界队列
- **AND** Prediction Schedule与Remote Presentation MUST在后续正式Tick处理


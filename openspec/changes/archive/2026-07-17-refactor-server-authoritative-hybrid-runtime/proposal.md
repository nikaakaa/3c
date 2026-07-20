# Change: 建立 ServerAuthoritativeHybrid 预测纠偏与 Unity 权威闭环

## Why

`refactor-gameplay-session-composition-boundary` 将提供唯一 `SimulationSessionHost`、显式 Session composition、正式 `.csim` artifact、Actor registration、Float32 Pass Backend、四阶段 Pipeline、零到多步 ExecutionPlan、Pipeline state snapshot 和可运行 Standard Local Pipeline。Local 完成后，网络模型不应再通过旧 `ISimulationDriver`、固定 `SimulationSessionRuntime` 或 Character 私有 stage 接管模拟。

现有 ServerAuthoritativeHybrid proposal 仍把 accepted input history、prediction、reconciliation、remote snapshot 和 output policy 收进一个“ServerAuthoritative Driver”，并让 Unity 权威端直接创建旧 SessionRuntime。该方案已经与新基座冲突：它无法声明 correction/replay Pass 的产品、状态、Snapshot 和输出所有权，也会恢复即将删除的 Driver 路径。

现有文档还混淆了 Fantasy .NET 进程和 Unity 权威进程。当前可用权威 WorldSolver 是 `UnityCharacterControllerWorldSolver`，普通 Fantasy .NET 进程不能执行它；而 DotRecast/C# Solver 属于独立后续 change。因此本 change 明确使用四进程本地纵切：

```text
Fantasy Gate/Room process
+ Unity Authority Worker process
+ Client A process
+ Client B process
```

Fantasy Gate/Room 只拥有控制连接、角色身份、Room、ticket与可靠事务路由。Unity Authority Worker 作为唯一 gameplay authority，加载与客户端相同 canonical Float32 Program bytes，运行独立 Authoritative Pipeline 和 Unity Solver，直接接收command并发布snapshot与可靠事实。每个客户端只预测自己的 owner actor，通过 Prediction/Correction Pipeline 完成 `authoritative baseline -> restore -> replay unacked input -> current step -> output disposition`；remote actor 只消费权威复制进入 Presentation，不执行伪本地预测。

本 change 的目标不是通用网络框架，而是为 Corin 提供一个可运行、可审查的 ServerAuthoritativeHybrid 纵切：移动、转身、闪避、Attack1/Attack2、连段、打断、Timeline TreeClip Window、motion curve、GameplayEffect、动画表现、owner correction 和 remote presentation 全部走正式 Program/Session/Pipeline 链路。

实施后的协议审计确认，现有第18章把四种不同职责错误压成一个`ObservationCadenceTicks`，让Room等待双Actor共同input horizon，并把完整`character-state/float32/v3`字节作为常规20Hz baseline经单条可靠有序KCP链路发送。该方案会让较慢玩家阻塞较快玩家，让大快照阻塞新输入，并把存档格式误当网络格式；现有消息数量日志也不足以证明实际瓶颈。因此本change在归档前重新打开网络数据面任务，保留已经成立的Session/Pipeline骨架，删除错误传输实现并收敛为一条正式协议链。

## Dependencies

- `refactor-character-simulation-core`、`refactor-character-semantic-frontend-artifact`、`refactor-character-pipeline-definition-config-boundary` 与 `refactor-simulation-operation-runtime-modules` MUST 已完成。
- `refactor-character-state-transaction-runtime` MUST 已完成并通过 strict validation；Prediction History继续消费Float32 ABI 2、`character-state/float32/v3`产生的本地committed canonical bytes；Authority网络输出必须从同一committed state生成Program/Layout锁定的Network Checkpoint，Restore/Replay与HardRecovery只能消费完整重建后的committed state，任何路径都MUST不持有active State Transaction、Pending evaluation、typed mutable partition或GameplayEffect working view。
- `refactor-gameplay-session-composition-boundary` MUST 在本 change 前串行完成全部任务并通过 strict validation。若尚未归档，本 change MUST 以其最终代码和 spec delta 为准重新核对 Source、Pass、Pipeline、ExecutionPlan、Snapshot、Composer 与 Host 合同；两个 change MUST 不并行编辑公共基座。
- Standard Local Pipeline MUST 已成为唯一 Local 运行链。本 change MUST 不修改 Local Pipeline 行为，也 MUST 不以 LocalLoopback 模拟网络闭环。
- 本 change MUST 复用正式 Float32 Program Runtime、Float32 Pass Backend、标准 Program Evaluate/WorldSolve/Finalize Pass、唯一 Session Host、Actor registration、Pipeline compiler、Commit transaction 和 ProgramAsset canonical bytes。
- `add-dotrecast-authoritative-server-backend` 与 `add-deterministic-rollback-kcc-model` MUST 不在本 change 实施期间修改相同 Session composition、协议或 Solver 接入点。

## What Changes

- 将 `ServerAuthoritativeHybridModelDefinition` 改为正式 Network Model Session Source Definition，显式声明protocol、Fantasy Control Endpoint、Gameplay Datagram Endpoint、Prediction Pipeline、Authority Pipeline、Program Runtime、Backend、Solver、history、checkpoint、clock和presentation capability requirement。
- 新增客户端 `ServerAuthoritativePredictionSessionSource`，唯一持有Fantasy control connection、ticket/data endpoint lifecycle、owner input sequence、authoritative snapshot/reliable observation queue、ack cursor和模型外部状态；Source不执行Program、不调用Solver、不直接改Character/World state。
- 新增 Unity worker `ServerAuthoritativeAuthoritySessionSource`，唯一持有worker registration、Room control route、ticket/data endpoint、per-Actor command queue、server clock、snapshot baseline和reliable output port；它不复制Common Host或Pipeline Runtime。
- 新增显式 `ServerAuthoritativePredictionPipelineDefinition`：Owner input ingress、authoritative observation ingress、prediction/correction/clock schedule、复用标准Float32 Step Pass、prediction history、EventId output disposition、command datagram egress和remote presentation egress。
- 新增显式 `ServerAuthoritativeAuthorityPipelineDefinition`：accepted input ingress、authoritative tick schedule、复用标准 Float32 Step Pass、authoritative baseline/ack/fact replication egress。
- 新增 model-owned versioned Pipeline products，覆盖 owner input batch、authoritative observation、baseline candidate、correction decision、accepted input batch、replication batch 和 remote presentation batch；每个产品拥有稳定 identity、schema、producer、consumer 和 diagnostics shape。
- 建立 Prediction Pipeline SnapshotParticipant：保存有界 predicted input/state history、ack cursor、authoritative baseline cursor 和 EventId disposition journal。socket、Fantasy Session 和 raw packet queue 继续是 Source 的 ExternalSource state，不进入 Gameplay snapshot。
- 建立正式 correction schedule：无 correction 时只执行 Current step；收到可覆盖 baseline 时构造完整客户端 restore snapshot并执行 Replay/Current steps；history 不可覆盖时执行明确定义的 hard recovery，不能直接写 Transform或切换 Local Pipeline。
- 定义跨 Pipeline baseline 规则：权威 Worker 发送的是 model-owned `AuthoritativeActorBaseline`，不是可直接恢复的 Authority Pipeline snapshot。客户端必须将 baseline 与同 Tick 的本地 Prediction Pipeline snapshot按正式规则合成为本客户端 PipelineHash下的完整 restore directive，禁止互换不同 PipelineHash 的 Snapshot。
- 权威 baseline 必须包含恢复 owner gameplay 所需的完整 Character state、owner body/world state identity、SimulationTick、Program/Layout identity、state hash和事实确认边界；仅发送 pose 或 motion snapshot不能触发 gameplay reconciliation。
- 明确 predicted side effect policy：owner 的连续 Body/Animation 状态可即时表现并在 correction 后重投影；one-shot presentation command 以 EventId journal 去重；Replay 不重复提交已发布 EventId；不存在的历史 one-shot 不伪造反向播放。
- 将Fantasy KCP限定为控制与可靠事务通道：worker/client注册、roster、数据面ticket、可靠Action/Effect/Cue事件、full checkpoint请求/响应、failure和leave。可靠事件在KCP上发送一次，继续使用EventId做业务去重与预测提交，不在每个snapshot重复重发。
- 新增ServerAuthoritative模型自有的直接UDP数据面。Room向worker和client签发有界session ticket并发布worker endpoint；client完成ticket challenge后直接向worker发送command datagram，worker直接向client发送snapshot datagram。Room不再中继高频input、body或routine baseline，UDP失败也不得回退到KCP gameplay stream。
- 将60Hz gameplay simulation、30Hz command packet、20Hz snapshot packet、full checkpoint、reliable event flush和remote interpolation delay拆成独立策略并纳入模型身份。删除统一`ObservationCadenceTicks`和双Actor共同horizon。
- 客户端每个预测tick生成精确target authority tick的canonical input，command packet冗余携带当前及前若干样本。Authority按自己的60Hz时钟持续推进，并对每个Actor独立消费input queue；缺样本时只在有界窗口保持连续输入，离散请求永不重复。
- 建立正式clock discipline：握手锁定authority tick，Prediction Schedule维持显式command slack并允许稀有的零步或双Current step校正领先量。Room/worker必须相对当前authority tick验证lead/lag，不能只比较相邻client tick。
- 让Owner Presentation使用独立simulation sample时钟消费Prediction Schedule的零步、单步和双步结果；restore/replay替换预测分支时保留上一帧可见姿态并在表现层收敛到新canonical body，不重复播放旧outer tick插值区间。
- 让Prediction Schedule在零Current step时正式保存尚未进入simulation的Attack、Dodge和Combo请求，将其纳入correction SnapshotParticipant，并只在下一次首个Current step消费一次。
- 用ProgramHash/LayoutHash锁定的Network Checkpoint Layout替代常规网络中的完整State codec字节。Full checkpoint使用dense slot index编码；routine snapshot使用已确认baseline上的changed-slot bitset与changed values，同时携带owner correction、remote body/producer、state hash、input ack和snapshot identity。Full与Delta共享单调SnapshotSequence；未收到新ack时worker仍可相对最后已确认base持续发送更新delta，不以单帧丢失阻塞后续快照。
- UDP gameplay datagram必须有明确MTU预算且不得分片；超过预算时停止发送该delta并通过可靠控制通道请求/发送full checkpoint。Full checkpoint只用于初始化、baseline丢失、布局重置或delta超限，不按20Hz发送。
- 在 Fantasy Gate Scene 保留唯一 demo Room entity/registry。Room 拥有Room、worker、两个player slot、PlayerId/ActorId、ticket和可靠事务路由；Authority Worker拥有每Actor command queue、authority clock、snapshot baseline和UDP endpoint。两者都不执行对方职责。
- Unity Authority Worker 通过专用 launch role 连接 Room，先锁定 ProgramHash、LayoutHash、operation-set、TickRate、Authority PipelineHash、Solver capability 和 protocol version；worker 缺失或 identity 不匹配时玩家不得进入 Active gameplay。
- Client handshake 同时校验同一 Program identity和明确兼容的 Prediction/Authority Pipeline pair。客户端 PipelineHash与权威 PipelineHash允许不同，但 pair 必须由 ModelDefinition/协议版本明确声明，不能只比较显示名。
- 客户端 Simulation roster 只包含本地预测 owner；remote actor 通过 model-owned remote presentation registration和正式 committed replication output驱动既有 Presentation runtime，不创建第二 gameplay Session、不注入 remote 伪输入。
- Remote Presentation必须提前缓存当前Body插值区间的SampleProducer，并按稀疏authority sample tick插值Timeline动画时间；不得把20Hz动画采样当作每帧自由运行起点。
- 交付 Corin 本地四进程 Demo 配置：一个 Fantasy server、一个 Unity Authority Worker、Client A 和 Client B。Unity 角色进程使用显式 launch role/identity，不通过场景搜索、默认 endpoint 或运行时自动降级决定角色。
- 交付显式 Network Test Player 构建入口和仓库内四进程启动脚本。构建入口必须固定使用 Network Test Bootstrap、Client、Authority Worker 三个场景并让 Bootstrap 排在第一位；启动脚本必须拒绝旧构建、验证三个 Unity 角色都存活且已创建网络 endpoint，不能在漏启 Client B 或实际未联网时报告成功。
- 使用 Bootstrap Scene 进入隔离的 ServerAuthoritative Client Scene 或 Unity Authority Worker Scene。Bootstrap 只选择测试环境；每个角色 Scene 显式引用完整 Composition/launch profile，旧 Session 与 Endpoint 不跨 Scene 存活，Client A/B 通过显式 launch identity 复用同一客户端 Scene。
- 删除旧 `IServerAuthoritativeEndpoint` object packet facade、LocalLoopback endpoint、旧 `ServerAuthoritativeHybridSession` Driver/queue/history facade、endpoint enum/switch、Character network binding、旧 packet DTO、旧 capability 位和一次性迁移器。最终只保留 Source、Pipeline Pass、Fantasy generated protocol和正式模型资产。
- 扩展 diagnostics：按control/command/snapshot/reliable通道记录packet/s、bytes/s、payload bytes、control heartbeat outstanding、应用层可靠/full checkpoint队列压力、UDP丢包/乱序、datagram超限、RTT、jitter、authority tick、command lead、snapshot age、baseline命中、replay ticks、correction rate和EventId disposition。不得反射或绑定Fantasy内部KCP发送窗口；未取得这些可维护数据前不得再用消息数量猜测瓶颈。

## Non-Goals

- 不实现 DotRecast、纯 .NET gameplay authority、C# KCC、第二 WorldSolver 或 Unity/DotRecast 双算。
- 不实现 FixedQ32.32、deterministic rollback、lockstep、全局帧同步或跨平台确定性。
- 不实现命中裁决、伤害目标路由、combat rewind、lag compensation、2v2vE、PvE、Objective、匹配、数据库、断线续局或动态扩容。
- 不实现通用Transport框架、商业级加密、NAT穿透、Internet relay、断线重连或抗DDoS；直接UDP数据面只服务当前显式本地四进程Demo，但协议、codec和endpoint生命周期必须可由后续DotRecast authority复用。
- 不让 Fantasy Room 读取 `.csir`、执行 `.csim`、运行 Program operation、修改 Character state或调用 Unity API。
- 不同步 AnimationClip、Animancer state、Camera、VFX runtime object或 Timeline visual time；网络只复制 canonical gameplay baseline、typed facts和稳定 presentation producer/event identity。
- 不在 Character、Graph、StateMachine、Timeline、Action或 GameplayEffect operation中加入 owner/server/remote分支。
- 不支持运行中切换 Local/Prediction/Authority Pipeline、Endpoint、Program、Solver或 Actor ownership。
- 不保留 LocalLoopback endpoint、disconnected gameplay、默认 Pipeline、默认 Solver、client pose acceptance或连接失败 fallback。
- 不自动运行 Unity batchmode、不生成 standalone player build、不新增测试或人工验证 task。

## Current Spec Comparison

- `gameplay-simulation-session-composition` 与 `gameplay-simulation-pipeline` 将 Source、Pipeline Pass、ExecutionPlan、Pipeline Snapshot和固定 Commit定义为唯一扩展边界。本 change 只新增模型 Source/Pass/Pipeline和产品，不创建 Driver、第二 SessionRuntime或 Common Host修改。
- 旧 `server-authoritative-hybrid-sync-model` current spec因只描述已删除的 Driver、SessionRuntime、手写 packet/profile与 LocalLoopback而已移除。本 change以 Prediction/Authority Source、两个显式 Pipeline、generated Fantasy protocol和 Unity Authority Worker重新新增完整模型能力。
- `character-network-sync-domain-contract` 已要求 packet先降低为 canonical input、typed ingress、restore candidate或 Egress metadata。本 change补充权威 baseline和 replication batch，原始 Fantasy message不进入 Kernel或 Character Host。
- 当前change自身的`fantasy-unity-authoritative-session` delta曾要求统一`ObservationCadenceTicks`与双Actor共同horizon，该要求与独立Authority clock、按Actor missing-input policy和动作网络的低延迟目标冲突；本次修订直接替换该要求，不保留旧协议兼容。
- `add-dotrecast-authoritative-server-backend`必须复用本change的portable datagram codec、Network Checkpoint codec、独立Authority clock、command/snapshot Source语义和同一generated control/reliable协议；它只替换Authority Host/Solver，不得恢复Room accepted-input relay、baseline cadence、完整State routine baseline或另一套endpoint协议。
- `add-deterministic-rollback-kcc-model`拥有独立Fixed Program、peer input与world snapshot协议，不复用本模型的command slack、checkpoint delta或correction history；当前没有发现同一协议文件的设计依赖，但实施时仍不得并行编辑公共generated protocol输出。
- `character-input-pipeline` 已把 prediction history所有权交给模型 Source或有状态 Pass。本 change选择 Prediction History Pass作为 SnapshotParticipant，Source只保存 transport/handshake/receive queue等 ExternalSource状态。
- `gameplay-tick-system` 已允许一个 outer LogicTick产生零到多个 SimulationStep。本 change的 correction schedule直接使用该能力，不创建 replay runner、私有 Update或第二 Logic target。
- `character-presentation-interpolation` 已要求 visual root只消费 committed samples。本 change让 remote replication和 correction后的 owner sample都通过 Committer/Presentation port进入该链，不直接写 visual Transform。
- `btsmtl-compiled-simulation-program` 已区分 ProgramHash与 PipelineHash。本 change要求客户端与 worker使用相同 ProgramHash，但显式锁定兼容的 Prediction/Authority PipelineHash pair，不要求两个 PipelineHash相同。
- 现有 `unity-authoritative-two-client-demo` delta只描述结果，没有定义 Fantasy server与 Unity worker之间的进程边界、角色注册、baseline、Pipeline pair和启动失败策略。本 change补齐这些合同。

## Impact

- 新能力：`server-authoritative-hybrid-sync-model`、`server-authoritative-prediction-correction-pipeline`、`fantasy-unity-authoritative-session`、`unity-authoritative-two-client-demo`。
- 修改能力：`character-gameplay-pipeline-closure`。
- Client Runtime：Prediction Source、observation ingress、correction schedule、history/output disposition、Fantasy egress和 remote presentation。
- Authority Runtime：Authority Source、accepted input ingress、authority schedule、authoritative replication egress和 Unity worker launch role。
- Fantasy Server：generated control/reliable Outer protocol、Gate handlers、Room entity/registry、ticket/worker/player route、可靠事务queue和错误码。
- Network Data Plane：model-owned UDP endpoint、command/snapshot codec、clock discipline、checkpoint layout/delta codec、per-client baseline与MTU策略。
- Assets：ModelDefinition、Fantasy Endpoint、Prediction Pipeline、Authority Pipeline、Pass definitions、client A/B composition、authority composition和 launch definitions。
- 删除：旧 Driver/session facade、LocalLoopback endpoint、手写 packet/history/queue、旧 binding、旧 endpoint switch和 selectable incomplete model。

## 1. 锁定依赖、现状与删除边界

- [x] 1.1 使用UTF-8重读本change的proposal、design、tasks与全部spec deltas。
- [x] 1.2 确认`refactor-simulation-operation-runtime-modules`已完成并保持唯一`Float32OperationEvaluator`。
- [x] 1.3 确认`refactor-gameplay-session-composition-boundary`全部任务真实完成并通过strict validation。
- [x] 1.4 记录最终Source、Pass、Product、Pipeline、ExecutionPlan、Snapshot、Composer、Host与Actor registration合同。
- [x] 1.5 记录Standard Local Pipeline的Pass顺序、PipelineHash和Corin Local composition资产。
- [x] 1.6 确认本change不修改Standard Local Pipeline行为或公共Pipeline compiler语义。
- [x] 1.7 盘点`GameplayNetworkModelDefinition`迁移后的正式Session Source扩展入口。
- [x] 1.8 盘点现有ServerAuthoritative ModelDefinition、Session、Endpoint、Packet、Payload、History、Policy、Debug与Editor文件。
- [x] 1.9 盘点现有ServerAuthoritative资产、场景引用、asmdef和meta。
- [x] 1.10 盘点Fantasy server的Main、Entity、Hotfix、Fantasy.config、Gate Scene和协议生成路径。
- [x] 1.11 盘点Unity客户端Fantasy连接、Session、generated protocol与Handler路径。
- [x] 1.12 记录ProtocolExportTool输入、server输出和client输出目录。
- [x] 1.13 记录Corin ProgramId、ProgramHash、LayoutHash、operation-set、TickRate和canonical bytes identity。
- [x] 1.14 记录Float32 Program Runtime、Pass Backend和Unity Solver identity/capability。
- [x] 1.15 建立旧Driver/session facade、LocalLoopback、手写packet/history/queue、binding和endpoint switch删除清单。
- [x] 1.16 建立新模块所有权清单：model contracts、client Source/Pass、authority Source/Pass、Fantasy Room、protocol、worker、presentation和assets。
- [x] 1.17 确认普通Fantasy .NET进程不引用UnityEngine、Float32 runtime或WorldSolver。
- [x] 1.18 确认Unity Authority Worker是本change唯一gameplay authority进程。
- [x] 1.19 确认DotRecast、C# KCC、Fixed、rollback和combat rewind不进入本change。
- [x] 1.20 若公共composition仍需修改才能容纳模型Source/Pass，停止并先修订两个change的tradeoff，不建立旁路Host。

## 2. 建立模型Identity与Pipeline Product合同

- [x] 2.1 定义稳定ServerAuthoritativeHybrid ModelId和model protocol version。
- [x] 2.2 定义Authority Worker、Client A与Client B的显式process role identity。
- [x] 2.3 定义RoomId、PlayerId、ActorId和worker identity的canonical格式与校验。
- [x] 2.4 定义Prediction Pipeline与Authority Pipeline兼容pair identity。
- [x] 2.5 让兼容pair覆盖ProgramHash、LayoutHash、operation-set、TickRate、Backend和Solver capability requirement。
- [x] 2.6 定义`OwnerCanonicalInputBatch` ProductId、schema和稳定排序。
- [x] 2.7 让owner input product保存ActorId、source tick、input sequence和canonical CharacterSimulationInput。
- [x] 2.8 定义`AuthoritativeObservationBatch` ProductId、schema和接收顺序。
- [x] 2.9 定义`AuthoritativeActorBaseline` ProductId与canonical codec。
- [x] 2.10 让baseline覆盖完整Character state bytes、owner body/world baseline、Tick和Program/Layout identity。
- [x] 2.11 让baseline覆盖state/body hash、confirmed input sequence和confirmed EventId horizon。
- [x] 2.12 定义`PredictionCorrectionDecision` ProductId、decision kind和原因码。
- [x] 2.13 定义`AcceptedAuthorityInputBatch` ProductId、每Actor输入和稳定ActorId顺序。
- [x] 2.14 定义`AuthorityReplicationBatch` ProductId、baseline、ack、body stream和reliable facts。
- [x] 2.15 定义`RemotePresentationBatch` ProductId、body sample、producer command、fact和EventId。
- [x] 2.16 为每个exclusive product指定唯一producer和consumer phase。
- [x] 2.17 为append-only fact/event产品指定ActorId、Tick、sequence和EventId稳定顺序。
- [x] 2.18 为全部model产品建立diagnostics shape与schema version失败策略。
- [x] 2.19 保证Pipeline product不保存Fantasy Session、generated message、Unity object或mutable Source queue。
- [x] 2.20 保证raw packet只能在Endpoint/Source边界转换为正式产品。

## 3. 重构ServerAuthoritative ModelDefinition与Session Source

- [x] 3.1 将`ServerAuthoritativeHybridModelDefinition`改为正式Network Model Session Source Definition。
- [x] 3.2 让ModelDefinition显式引用Fantasy Endpoint Definition。
- [x] 3.3 让ModelDefinition显式引用Prediction Pipeline Definition。
- [x] 3.4 让ModelDefinition显式引用Authority Pipeline Definition。
- [x] 3.5 让ModelDefinition声明Float32 Program Runtime和Pass Backend requirement。
- [x] 3.6 让ModelDefinition声明Unity Solver capability requirement。
- [x] 3.7 让ModelDefinition声明history容量、baseline cadence、input lead/lag和hard recovery policy。
- [x] 3.8 让全部model policy进入稳定config hash或PipelineHash。
- [x] 3.9 禁止ModelDefinition按类型扫描、endpoint enum、默认资产或已安装包猜测配置。
- [x] 3.10 建立`ServerAuthoritativePredictionSessionSourceDefinition`。
- [x] 3.11 建立Prediction Source preparation和Pending/Ready/Failed lifecycle。
- [x] 3.12 让Prediction Source preparation完成Fantasy连接、join、worker identity和完整roster。
- [x] 3.13 让Prediction Source Ready时锁定owner simulation registration与remote presentation registration。
- [x] 3.14 建立Prediction Source的local input、authoritative receive和network send窄端口。
- [x] 3.15 将Fantasy connection、raw receive queue和transport state声明为ExternalSource。
- [x] 3.16 禁止Prediction Source执行Program、构造Kernel、调用Solver或直接提交Presentation。
- [x] 3.17 建立`ServerAuthoritativeAuthoritySessionSourceDefinition`。
- [x] 3.18 建立Authority Source preparation和worker register lifecycle。
- [x] 3.19 让Authority Source Ready时锁定两个Actor canonical roster和Room route。
- [x] 3.20 建立Authority Source的accepted input receive、authority clock和replication send窄端口。
- [x] 3.21 禁止Authority Source复制Pipeline runtime、Kernel evaluator、WorldSolver或Committer。
- [x] 3.22 让Source Dispose按connection、queue和registration owner顺序只执行一次。
- [x] 3.23 让连接、join或worker register失败直接返回Failed且不选择Local Source。
- [x] 3.24 删除旧Simulation Driver capability和model-owned Driver创建入口。

## 4. 建立Prediction Pipeline Definition与Pass Factory

- [x] 4.1 建立稳定`ServerAuthoritativePredictionPipelineId`、Revision和schema version。
- [x] 4.2 建立`OwnerInputIngressPassDefinition`与稳定PassId/version。
- [x] 4.3 让Owner Input Pass只消费local input Source port并产生OwnerCanonicalInputBatch。
- [x] 4.4 建立`FantasyAuthoritativeObservationIngressPassDefinition`。
- [x] 4.5 让Observation Pass只消费Source receive port并产生AuthoritativeObservationBatch。
- [x] 4.6 建立`PredictionCorrectionSchedulePassDefinition`。
- [x] 4.7 让Correction Schedule成为Prediction Pipeline唯一ExecutionPlan producer。
- [x] 4.8 声明Correction Schedule支持Forward、Restore和Replay execution kind。
- [x] 4.9 声明Correction Schedule为SnapshotParticipant并绑定canonical state codec。
- [x] 4.10 建立`PredictionHistoryEgressPassDefinition`。
- [x] 4.11 声明Prediction History为SnapshotParticipant并绑定history codec。
- [x] 4.12 建立`PredictionOutputDispositionPassDefinition`。
- [x] 4.13 声明Output Disposition为SnapshotParticipant并绑定EventId journal codec。
- [x] 4.14 建立`FantasyInputCommandEgressPassDefinition`。
- [x] 4.15 让Input Command Egress只产生SourceEgress且不修改已完成step state。
- [x] 4.16 建立`RemotePresentationEgressPassDefinition`。
- [x] 4.17 让Remote Presentation Egress只消费RemotePresentationBatch并产生committed output route。
- [x] 4.18 将标准Float32 Program Evaluate Pass装入Prediction Step阶段。
- [x] 4.19 将标准Float32 World ResolveBatch Pass装入Prediction Step阶段。
- [x] 4.20 将标准Float32 Program Finalize Pass装入Prediction Step阶段。
- [x] 4.21 建立唯一`ServerAuthoritativePredictionPipelineDefinition`资产类型。
- [x] 4.22 固定Prediction Pipeline四阶段Pass顺序。
- [x] 4.23 声明全部Pass consume/produce、exclusive/append-only权限和Source port requirement。
- [x] 4.24 声明Prediction Pipeline的Float32 ABI、Backend、Unity Solver、Restore和Replay requirement。
- [x] 4.25 让Pipeline compiler在Active前验证全部Pass factory和product ownership。
- [x] 4.26 禁止Prediction Pipeline隐藏注入Pass、运行时重排或调用旧SessionRuntime。

## 5. 实现Prediction History与Baseline Merge合同

- [x] 5.1 定义每Tick predicted history record的canonical schema。
- [x] 5.2 让history record保存owner input和input sequence。
- [x] 5.3 让history record保存`character-state/float32/v3` committed canonical bytes、NumericProfile、Target ABI、ProgramHash、LayoutHash、codec identity和state hash，禁止保存active State Transaction或typed mutable引用。
- [x] 5.4 让history record保存owner World/body state和body hash。
- [x] 5.5 让history record保存Prediction Pipeline snapshot identity和bytes。
- [x] 5.6 让history record保存EventId disposition journal cursor。
- [x] 5.7 按SimulationTick建立有界history索引且禁止字符串查找。
- [x] 5.8 保证history stable order和canonical capture/hash。
- [x] 5.9 保证history restore后input、state、ack和journal cursor一致。
- [x] 5.10 只释放authority ack确认且不再参与replay的最旧record。
- [x] 5.11 对未确认history容量不足明确触发formal recovery或Session failure。
- [x] 5.12 定义authority baseline的NumericProfile、Target ABI、ProgramHash、LayoutHash、State codec identity、ActorId和Tick精确校验。
- [x] 5.13 校验baseline Character state bytes可由当前Program/Layout的唯一正式codec解码，拒绝旧version与fallback decode。
- [x] 5.14 校验baseline owner body/world identity与当前Solver capability匹配。
- [x] 5.15 定义baseline与同Tick本地Prediction snapshot的merge owner表。
- [x] 5.16 用authority Character state替换本地snapshot中权威Character部分。
- [x] 5.17 用authority owner body baseline替换本地World中owner body部分。
- [x] 5.18 按正式规则保留或重建Prediction history cursor、ack和EventId journal。
- [x] 5.19 重新编码当前Prediction PipelineHash下的完整restore snapshot。
- [x] 5.20 禁止改写Authority Pipeline snapshot的PipelineHash后直接恢复。
- [x] 5.21 禁止仅修正Transform或Body而保留旧Action/Timeline/GE state。
- [x] 5.22 为baseline merge失败建立精确diagnostics和fail-stop。

## 6. 实现Correction Schedule与Replay事务

- [x] 6.1 定义NoCorrection、RestoreReplay和HardRecovery decision kind。
- [x] 6.2 在baseline identity非法时让Schedule明确失败。
- [x] 6.3 在state hash和body tolerance均匹配时只推进ack且不restore。
- [x] 6.4 保证Character state hash不匹配时不得被body tolerance掩盖。
- [x] 6.5 在history覆盖baseline Tick时构造完整restore directive。
- [x] 6.6 按Tick顺序构造baseline后全部未确认Replay steps。
- [x] 6.7 为当前local input构造唯一Current step。
- [x] 6.8 保证Replay step使用历史canonical input而不是重新读取设备。
- [x] 6.9 保证Current step只使用本outer tick的canonical input。
- [x] 6.10 保证restore、全部Replay和Current属于同一outer transaction，同时每个内部Step只创建自己的Kernel State Transaction且不得把该transaction写入history或packet。
- [x] 6.11 保证任一Replay失败时不发布部分state或output。
- [x] 6.12 限制单outer tick最大replay步数并让超限进入明确policy结果。
- [x] 6.13 在history不覆盖时用latest完整baseline构造HardRecovery。
- [x] 6.14 让HardRecovery清除无法证明有效的unacked history。
- [x] 6.15 让HardRecovery建立新的Prediction snapshot和ack cursor。
- [x] 6.16 让HardRecovery通过正式restore transaction而不是Transform teleport。
- [x] 6.17 baseline不足以构造完整HardRecovery时让Session失败。
- [x] 6.18 让Correction decision记录误差、restore tick、replay range和reason。
- [x] 6.19 禁止correction Pass调用Presentation、Fantasy transport或Unity Transform。
- [x] 6.20 禁止创建私有replay Update、Coroutine、Task loop或第二Logic target。

## 7. 实现Prediction Output Disposition与Owner表现纠偏

- [x] 7.1 定义EventId disposition journal的canonical schema。
- [x] 7.2 让journal区分PredictedCommitted、AuthorityConfirmed、SuppressedDuplicate和PredictedRejected。
- [x] 7.3 在Replay输出命中已提交EventId时产生SuppressDuplicate。
- [x] 7.4 保证SuppressDuplicate不再次触发Animation、Cue、VFX、Audio或Network egress。
- [x] 7.5 允许最终reconciled state产生的新EventId在Commit后发布。
- [x] 7.6 将authority confirmed EventId推进journal confirmation horizon。
- [x] 7.7 将被authority否定的predicted one-shot记录为PredictedRejected diagnostics。
- [x] 7.8 禁止为已播放one-shot伪造反向Cue或回滚外部世界。
- [x] 7.9 让owner最终Body sample通过正式Committer提交。
- [x] 7.10 让owner最终Animation producer select/release通过正式Presentation command提交。
- [x] 7.11 复用现有visual recovery/interpolation处理body correction。
- [x] 7.12 禁止Presentation反向修改Character/World/Pipeline state。
- [x] 7.13 将journal capture/restore/hash纳入Prediction Pipeline snapshot。
- [x] 7.14 保证outer transaction失败时journal也不发布部分更新。

## 8. 建立Authority Pipeline Definition与Pass

- [x] 8.1 建立稳定`ServerAuthoritativeAuthorityPipelineId`、Revision和schema version。
- [x] 8.2 建立`FantasyAcceptedInputIngressPassDefinition`。
- [x] 8.3 让Accepted Input Pass只消费Authority Source port并产生AcceptedAuthorityInputBatch。
- [x] 8.4 建立`AuthorityTickSchedulePassDefinition`。
- [x] 8.5 让Authority Schedule成为Authority Pipeline唯一ExecutionPlan producer。
- [x] 8.6 让Authority Schedule只产生Authoritative step或Pending。
- [x] 8.7 定义连续input有界hold和离散request不重复的missing-input policy。
- [x] 8.8 让missing-input policy配置进入Pass config hash和PipelineHash。
- [x] 8.9 让超过hold window的actor使用显式neutral canonical input。
- [x] 8.10 按稳定ActorId顺序建立每Tick input set。
- [x] 8.11 将标准Float32 Program Evaluate Pass装入Authority Step阶段。
- [x] 8.12 将标准Float32 World ResolveBatch Pass装入Authority Step阶段。
- [x] 8.13 将标准Float32 Program Finalize Pass装入Authority Step阶段。
- [x] 8.14 保证每authority SimulationTick只执行一次multi-actor World batch。
- [x] 8.15 建立`AuthorityReplicationEgressPassDefinition`。
- [x] 8.16 从finalized step生成每ActorAuthoritativeActorBaseline。
- [x] 8.17 从finalized step生成accepted input ack和latest authority tick。
- [x] 8.18 将Body sample分类为actor-scoped replaceable stream。
- [x] 8.19 将Action/Effect/Cue/roster/failure facts分类为reliable EventId batch。
- [x] 8.20 保证Authority Egress不接受client resolved displacement或Transform。
- [x] 8.21 建立唯一`ServerAuthoritativeAuthorityPipelineDefinition`资产类型。
- [x] 8.22 固定Authority Pipeline四阶段Pass顺序。
- [x] 8.23 声明全部Pass产品、state class、Source port和Solver requirement。
- [x] 8.24 让Pipeline compiler在Active前校验Authority组合完整性。
- [x] 8.25 禁止Authority Pipeline复制Float32 evaluator、Session Host或Commit transaction。

## 9. 建立Fantasy Outer协议与生成链路

- [x] 9.1 在正式Outer proto中定义process role和协议版本枚举。
- [x] 9.2 定义AuthorityRegisterRequest/Response消息。
- [x] 9.3 让worker register携带Room、Program、Layout、operation-set、TickRate、Pipeline、Backend和Solver identity。
- [x] 9.4 定义AuthorityHeartbeat消息和latest authority tick。
- [x] 9.5 定义ClientJoinRequest/Response消息。
- [x] 9.6 让client join携带Player、Program、Prediction Pipeline和protocol identity。
- [x] 9.7 定义完整Roster和RosterChanged消息。
- [x] 9.8 定义ClientInputCommandBatch消息和canonical input fields。
- [x] 9.9 定义AcceptedInputBatchToAuthority消息。
- [x] 9.10 定义AuthorityBaselineBatch消息。
- [x] 9.11 定义AuthorityRemoteBodyStream消息。
- [x] 9.12 定义AuthorityReplicationEventBatch消息和EventId。
- [x] 9.13 定义ClientAck和FullBaselineRequest消息。
- [x] 9.14 定义SessionFailed和Leave消息。
- [x] 9.15 定义业务ErrorCode而不是用异常表达join/route拒绝。
- [x] 9.16 保证Outer协议不序列化Unity object、Pipeline runtime对象或raw Character builder。
- [x] 9.17 保证baseline/state bytes有明确schema/version/length边界。
- [x] 9.18 通过现有ProtocolExportTool正式导出client/server generated代码。
- [x] 9.19 禁止手写或修改generated `.g.cs`。
- [x] 9.20 删除与generated protocol重复的旧ServerAuthoritativePacket/Payload DTO。
- [x] 9.21 更新OpCode cache并确认消息identity稳定。
- [x] 9.22 核对client/server Fantasy版本和generated协议接口一致。

## 10. 建立Fantasy Gate Room与精确路由

- [x] 10.1 在Fantasy Gate Scene建立Scene-owned `ServerAuthoritativeRoomRegistry` Entity/Component。
- [x] 10.2 建立固定Demo Room创建、查找和销毁lifecycle。
- [x] 10.3 建立`ServerAuthoritativeRoom` Entity并限定两个player slot。
- [x] 10.4 让Room保存唯一AuthorityWorker connection identity。
- [x] 10.5 让Room保存PlayerId到owned ActorId的精确映射。
- [x] 10.6 让Room保存每Actor最后accepted input sequence。
- [x] 10.7 让Room保存latest authority tick和worker heartbeat状态。
- [x] 10.8 建立worker register Handler并校验完整identity。
- [x] 10.9 拒绝重复worker或不匹配的worker register。
- [x] 10.10 建立client join Handler并等待合法worker和空player slot。
- [x] 10.11 校验client Program与worker Program identity一致。
- [x] 10.12 校验Prediction/Authority Pipeline pair由当前model protocol允许。
- [x] 10.13 为Client A/B分配稳定PlayerId和ActorId且不按连接顺序猜测显示名。
- [x] 10.14 完整roster锁定后才允许gameplay input路由。
- [x] 10.15 建立client input Handler并校验connection owner ActorId。
- [x] 10.16 拒绝duplicate、regressed、unknown actor和owner mismatch input。
- [x] 10.17 按latest authority tick校验input lead/lag边界。
- [x] 10.18 将accepted input只路由给AuthorityWorker connection。
- [x] 10.19 建立authority baseline/body/event Handler并只接受registered worker。
- [x] 10.20 将owner baseline/ack路由给精确owner client。
- [x] 10.21 将remote body/event路由给非owner client。
- [x] 10.22 建立actor-scoped replaceable body/baseline stream queue。
- [x] 10.23 建立有界reliable roster/action/effect/cue/failure queue。
- [x] 10.24 reliable queue overflow时fail-stop且不删除旧事实腾空间。
- [x] 10.25 worker断开时关闭Room gameplay并通知两个clients。
- [x] 10.26 player断开时终止当前固定roster Demo Session。
- [x] 10.27 使用Fantasy Scene lifecycle和FTask，不建立裸Task/thread loop。
- [x] 10.28 使用Fantasy source-generator注册Handler，不手写registration。
- [x] 10.29 保证Room不引用Program runtime、Character state、WorldSolver或Unity类型。
- [x] 10.30 保证Room不计算correction decision或修改authority baseline。

## 11. 建立Unity Authority Worker Host闭环

- [x] 11.1 定义Unity process launch role参数和Authority role parser。
- [x] 11.2 建立Authority Worker专用scene/launch definition。
- [x] 11.3 建立Authority composition asset并显式引用五项Definition。
- [x] 11.4 为Authority composition绑定Float32 Program Runtime。
- [x] 11.5 为Authority composition绑定Float32 Pass Backend。
- [x] 11.6 为Authority composition绑定Authority Pipeline。
- [x] 11.7 为Authority composition绑定Authority Session Source。
- [x] 11.8 为Authority composition绑定Unity CharacterController Solver。
- [x] 11.9 让worker从ProgramAsset内嵌canonical bytes加载Corin Program。
- [x] 11.10 禁止worker Unity Player读取Library `.csim`路径或运行时lowering。
- [x] 11.11 建立Actor A/B稳定authority Actor registration。
- [x] 11.12 为两个authority actor绑定独立world body和CharacterController。
- [x] 11.13 在worker register前校验Program/Layout/Pipeline/Backend/Solver identity。
- [x] 11.14 在完整roster前保持Authority Source Preparing且不执行Program。
- [x] 11.15 Ready后通过唯一Float32 Composer创建Authority runtime handle。
- [x] 11.16 让GameplayTickSystem每authority source tick只推进一次Session handle。
- [x] 11.17 让Authority Pipeline按稳定ActorId执行同一Program和一次World batch。
- [x] 11.18 将authority output只交给Replication Egress和diagnostics。
- [x] 11.19 禁止Authority Worker创建Presentation、Camera或本地player input fallback。
- [x] 11.20 worker dispose时按Runtime、Source connection、Solver和Actor registration顺序释放。

## 12. 建立Client Prediction与Remote Presentation闭环

- [x] 12.1 定义Client A/B显式launch role和Player identity参数。
- [x] 12.2 建立Client A prediction composition asset。
- [x] 12.3 建立Client B prediction composition asset。
- [x] 12.4 为两个client composition显式绑定Float32 Program Runtime。
- [x] 12.5 为两个client composition显式绑定Float32 Pass Backend。
- [x] 12.6 为两个client composition显式绑定Prediction Pipeline。
- [x] 12.7 为两个client composition显式绑定Prediction Session Source。
- [x] 12.8 为两个client composition显式绑定Unity CharacterController Solver。
- [x] 12.9 让Client A simulation roster只包含Actor A owner。
- [x] 12.10 让Client B simulation roster只包含Actor B owner。
- [x] 12.11 从完整roster建立remote presentation registration。
- [x] 12.12 为remote registration绑定Corin Projection和visual output port。
- [x] 12.13 建立remote body snapshot buffer和authority time映射。
- [x] 12.14 让remote body sample走现有Presentation interpolation。
- [x] 12.15 将remote producer select/release通过正式Presentation command queue提交。
- [x] 12.16 将remote Action/Effect/Cue EventId通过正式output route提交。
- [x] 12.17 让remote spawn在Session Active前完成且Active后roster锁定。
- [x] 12.18 Session终止时释放remote animation playback和visual registration。
- [x] 12.19 禁止remote actor创建CharacterSimulationState、local input或owner prediction。
- [x] 12.20 禁止remote output直接调用Animancer、写Transform或修改owner Session state。

## 13. 迁移Corin业务纵切与模型配置

- [x] 13.1 将Corin Program/Projection绑定到Authority Worker和两个Client composition。
- [x] 13.2 校验三端Program canonical bytes和ProgramHash一致。
- [x] 13.3 校验Authority与Prediction PipelineHash形成显式兼容pair。
- [x] 13.4 为move/facing连续输入配置authority missing-input policy。
- [x] 13.5 为Attack、Dodge和combo离散request配置永不重复语义。
- [x] 13.6 让移动、转身、闪避和Run继续只由compiled Program推进。
- [x] 13.7 让Attack1/Attack2、combo和打断继续只由compiled Program推进。
- [x] 13.8 让Timeline TreeClip Window和motion curve继续只由compiled Program推进。
- [x] 13.9 让GameplayEffect、Attribute和Cue继续输出typed fact/EventId。
- [x] 13.10 将authority Action/Effect/Cue事实映射到reliable replication batch。
- [x] 13.11 将owner prediction事实映射到EventId disposition journal。
- [x] 13.12 将remote事实映射到Projection/Presentation而不恢复Graph runtime。
- [x] 13.13 禁止Model policy复制ActionProfile、Timeline window或Blackboard业务配置。
- [x] 13.14 缺失需要复制的producer/fact policy时让ModelDefinition配置失败。

## 14. 建立Demo启动配置与Inspector

- [x] 14.1 建立唯一ServerAuthoritativeHybrid ModelDefinition正式资产。
- [x] 14.2 建立唯一Fantasy Endpoint Definition正式资产。
- [x] 14.3 建立Prediction Pipeline和全部Pass Definition正式资产。
- [x] 14.4 建立Authority Pipeline和全部Pass Definition正式资产。
- [x] 14.5 建立Authority Worker launch definition。
- [x] 14.6 建立Client A launch definition。
- [x] 14.7 建立Client B launch definition。
- [x] 14.8 建立固定RoomId、endpoint address和role identity配置。
- [x] 14.9 建立ModelDefinition Inspector的Source/Pipeline/Endpoint/capability分组。
- [x] 14.10 显示Program、Prediction Pipeline、Authority Pipeline和protocol identity。
- [x] 14.11 显示history、baseline、lead/lag、replay和missing-input正式配置。
- [x] 14.12 对缺失Source、Pass、Pipeline、Endpoint、Program Runtime、Backend或Solver显示精确错误。
- [x] 14.13 禁止Inspector自动创建默认endpoint、Pipeline、Solver或Local fallback。
- [x] 14.14 建立四进程角色配置所需的正式场景/launch引用。
- [x] 14.15 保持Unity build产物由用户正式构建，不在Editor脚本中调用batchmode。
- [x] 14.16 保证无Authority Worker时Client composition停留Preparing或Failed而不是进入Local。
- [x] 14.17 建立只按显式TestScenarioId跳转的Network Test Bootstrap Scene，禁止其创建或持有Session组合组件。
- [x] 14.18 建立隔离的ServerAuthoritative Client Scene并显式引用Prediction Composition、Endpoint、Actor/出生点、World binding与diagnostics。
- [x] 14.19 让Client A/B通过不同launch definition复用同一Client Scene，禁止按连接顺序、对象名或默认值推断PlayerId/ActorId。
- [x] 14.20 让Authority Worker专用Scene显式引用Authority Composition、完整双Actor roster、World binding与worker launch definition。
- [x] 14.21 场景切换时完整释放旧SimulationSessionHost、Actor registration、Fantasy Endpoint与模型队列，禁止这些owner通过DontDestroyOnLoad跨Scene存活。
- [x] 14.22 禁止Bootstrap或角色Scene提供Active Session网络模型下拉、enum switch、Local fallback或跨Scene热切换。

## 15. Diagnostics闭环

- [x] 15.1 定义process role、Room、Session、Player和Actor diagnostics字段。
- [x] 15.2 记录Model、Endpoint和protocol identity。
- [x] 15.3 记录ProgramHash、LayoutHash和operation-set。
- [x] 15.4 记录Prediction/Authority PipelineHash、Backend和Solver identity。
- [x] 15.5 记录local source tick、authority tick、SimulationTick和input sequence。
- [x] 15.6 记录RTT、queue depth、input lead/lag和baseline age。
- [x] 15.7 记录state hash match、body error和correction decision。
- [x] 15.8 记录restore tick、replayed ticks、hard recovery和ack cursor。
- [x] 15.9 记录EventId duplicate suppression和PredictedRejected。
- [x] 15.10 将model trace接入现有RuntimeDebugSession且保持只读。
- [x] 15.11 为Fantasy Room使用框架日志记录route/error code而不泄露mutable state。
- [x] 15.12 禁止diagnostics改变Source queue、history、correction或output policy。

## 16. 删除旧ServerAuthoritative路径

- [x] 16.1 删除旧`ServerAuthoritativeHybridSession` Driver/session facade。
- [x] 16.2 删除旧`IServerAuthoritativeEndpoint` object packet facade。
- [x] 16.3 删除`LocalServerAuthoritativeEndpoint`和Definition文件及meta。
- [x] 16.4 删除旧`ServerAuthoritativePacket`和手写payload DTO。
- [x] 16.5 删除旧`ServerAuthoritativeHistory`重复packet history。
- [x] 16.6 删除旧incoming/outgoing model queue实现。
- [x] 16.7 删除endpoint enum、switch factory和disconnected gameplay路径。
- [x] 16.8 删除旧GameplayNetworkSessionHost或Character binding残留引用。
- [x] 16.9 删除旧Simulation Driver composition capability引用。
- [x] 16.10 删除Transform teleport、ExternalPose和MotionStage correction残留。
- [x] 16.11 删除client resolved displacement作为authority input的任何字段和映射。
- [x] 16.12 删除旧LocalLoopback资产、场景引用和Editor入口。
- [x] 16.13 删除一次性migrator和旧serialized字段。
- [x] 16.14 使用`rg`确认不存在旧`ISimulationDriver`模型实现。
- [x] 16.15 使用`rg`确认不存在`LocalServerAuthoritativeEndpoint`。
- [x] 16.16 使用`rg`确认不存在`new SimulationSessionRuntime`。
- [x] 16.17 使用`rg`确认Fantasy server不引用Unity或Simulation Kernel实现。
- [x] 16.18 使用`rg`确认client/worker只通过唯一SimulationSessionHost运行。
- [x] 16.19 使用`rg`确认Program Evaluate只由正式Step Pass调用。
- [x] 16.20 使用`rg`确认WorldSolver只由正式WorldSolve Pass调用。
- [x] 16.21 使用`rg`确认remote actor不运行Program或local input。
- [x] 16.22 使用`rg`确认不存在手写generated protocol镜像。
- [x] 16.23 使用`rg`确认不存在Local/Fantasy连接失败fallback。
- [x] 16.24 使用`rg`确认运行时Gameplay只选择Local、Prediction和Authority三份显式Pipeline资产；隔离Editor Preview Pipeline不计入Gameplay组合。

## 17. 文档、编译与严格校验

- [x] 17.1 更新`openspec/project.md`记录Local、Prediction和Authority三种正式组合。
- [x] 17.2 更新`openspec/project.md`记录Fantasy Room控制面、Unity Authority Worker gameplay数据面与进程边界。
- [x] 17.3 更新`openspec/project.md`记录checkpoint、baseline merge、history和output disposition所有权。
- [x] 17.4 更新`openspec/project.md`删除统一cadence、共同horizon、旧Driver、LocalLoopback和SessionRuntime口径。
- [x] 17.5 核对`add-dotrecast-authoritative-server-backend`复用portable datagram codec与Authority Pipeline语义且不复制endpoint协议。
- [x] 17.6 核对`add-deterministic-rollback-kcc-model`不复用本模型history、checkpoint或protocol语义。
- [x] 17.7 更新本change tasks勾选并保证每项与真实文件一致。
- [x] 17.8 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的命令编译portable Simulation与Float32工程。
- [x] 17.9 前项编译后立即执行`dotnet build-server shutdown`。
- [x] 17.10 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的命令编译Unity Runtime与Editor工程。
- [x] 17.11 前项编译后立即执行`dotnet build-server shutdown`。
- [x] 17.12 使用带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`的命令编译Fantasy Server solution。
- [x] 17.13 前项编译后立即执行`dotnet build-server shutdown`。
- [x] 17.14 使用`openspec validate refactor-server-authoritative-hybrid-runtime --strict --no-interactive`执行严格校验。
- [x] 17.15 使用`openspec validate gameplay-simulation-session-composition --strict --no-interactive`确认已归档基座的current spec仍有效。
- [x] 17.16 使用`openspec validate add-dotrecast-authoritative-server-backend --strict --no-interactive`确认后续Solver change无冲突。
- [x] 17.17 使用`openspec validate add-deterministic-rollback-kcc-model --strict --no-interactive`确认Rollback change无职责回流。

## 18. 撤回错误传输合同并锁定正式数据面

- [x] 18.1 在实施盘点中记录现有KCP gameplay frame、统一cadence、共同horizon和完整state routine baseline路径。
- [x] 18.2 记录现有四进程日志能证明的authority生成速率、client接收速率与history overflow边界。
- [x] 18.3 明确当前日志不能仅凭消息数量确定瓶颈。
- [x] 18.4 删除Model policy中的`ObservationCadenceTicks`。
- [x] 18.5 删除`ObservationCadenceTicks`的ConfigurationHash输入。
- [x] 18.6 删除Authority register中的统一transport cadence字段。
- [x] 18.7 删除Room双Actor共同source tick horizon规则。
- [x] 18.8 删除Room高频input pending merge职责。
- [x] 18.9 删除Room高频body与routine baseline replaceable queue职责。
- [x] 18.10 定义独立`SimulationTickRate`策略。
- [x] 18.11 定义独立`CommandPacketRate`策略。
- [x] 18.12 定义独立`SnapshotPacketRate`策略。
- [x] 18.13 定义独立`CommandSlackTicks`策略。
- [x] 18.14 定义独立`RemoteInterpolationDelayTicks`策略。
- [x] 18.15 定义独立`MaxGameplayDatagramBytes`策略。
- [x] 18.16 将六项策略纳入Model configuration identity。
- [x] 18.17 将Corin正式配置为60Hz simulation、30Hz command、20Hz snapshot、3 tick slack、6 tick interpolation与1200-byte datagram。
- [x] 18.18 让Model Inspector按Simulation、Command、Snapshot、Interpolation和Budget分组显示策略。
- [x] 18.19 对缺失、零值、互相不兼容或超出安全范围的策略让ModelDefinition配置失败。

## 19. 建立Fantasy控制面与Ticket生命周期

- [x] 19.1 将Fantasy KCP职责限定为register、join、roster、ticket、reliable event、full checkpoint、failure和leave。
- [x] 19.2 在Outer proto中删除`ClientInputCommandBatch`。
- [x] 19.3 在Outer proto中删除`AcceptedInputBatchToAuthority`。
- [x] 19.4 在Outer proto中删除`AuthorityReplicationFrame`。
- [x] 19.5 在Outer proto中删除`ClientObservationFrame`。
- [x] 19.6 在Outer proto中定义worker data endpoint identity。
- [x] 19.7 在Outer proto中定义一次性`DataPlaneTicket`。
- [x] 19.8 让ticket绑定RoomId、SessionId、PlayerId、ActorId、worker endpoint、nonce和过期时间。
- [x] 19.9 让Room只在worker identity与双人roster锁定后签发ticket。
- [x] 19.10 让Room把同一ticket分别交给worker与精确client。
- [x] 19.11 让worker只接受一次未过期且identity完全匹配的ticket。
- [x] 19.12 拒绝unknown、reused、expired、actor mismatch和session mismatch ticket。
- [x] 19.13 ticket消费后锁定client remote endpoint并禁止Active期间静默变更。
- [x] 19.14 worker或client断开时撤销对应ticket和data endpoint。
- [x] 19.15 让control connection与data endpoint任一失效都终止Session。
- [x] 19.16 禁止data endpoint失败后回退KCP gameplay stream。
- [x] 19.17 保证Room不引用datagram payload、Network Checkpoint或Character state。
- [x] 19.18 保证ticket与control route不进入Gameplay snapshot或StateHash。

## 20. 建立Model-owned UDP Datagram Endpoint

- [x] 20.1 在portable ServerAuthoritative模块定义versioned gameplay datagram header。
- [x] 20.2 让header携带protocol、Room、Session、Player、Actor、packet kind、packet sequence和payload length。
- [x] 20.3 定义`DataPlaneHello` codec。
- [x] 20.4 定义`DataPlaneHelloAck` codec并携带authority tick与clock sample。
- [x] 20.5 定义`CommandDatagram` codec。
- [x] 20.6 定义`SnapshotDatagram` codec。
- [x] 20.7 对unknown version、kind、identity、length和trailing bytes直接拒绝。
- [x] 20.8 建立Prediction角色的UDP endpoint lifecycle。
- [x] 20.9 建立Authority角色的UDP endpoint lifecycle。
- [x] 20.10 让Fantasy callback与socket receive callback只写model Source queue。
- [x] 20.11 禁止socket callback执行Program、Pipeline、Solver、Correction或Presentation。
- [x] 20.12 为send/receive queue建立有界容量。
- [x] 20.13 对queue overflow、socket failure和持续liveness失败执行fail-stop。
- [x] 20.14 让endpoint dispose关闭socket、清空queue并撤销ticket route。
- [x] 20.15 保证datagram codec与endpoint不引用UnityEngine。
- [x] 20.16 保证Unity client/worker只复用同一codec与endpoint实现而不复制packet DTO。

## 21. 建立Command Stream与Authority Clock Discipline

- [x] 21.1 为每个预测tick生成immutable `CanonicalInputSample`。
- [x] 21.2 让sample携带target authority tick、input sequence和canonical input payload。
- [x] 21.3 为Attack、Dodge、Combo等离散输入保留稳定request identity。
- [x] 21.4 让command packet携带packet sequence和最近收到的snapshot/base ack。
- [x] 21.5 让command packet携带当前及前3个input sample。
- [x] 21.6 保证冗余sample保持原input sequence与request identity。
- [x] 21.7 让Prediction endpoint按30Hz发送command packet但每60Hz tick生成sample。
- [x] 21.8 让worker按packet sequence拒绝重复与回退packet。
- [x] 21.9 让worker按input sequence去重冗余sample。
- [x] 21.10 为每Actor建立按target authority tick索引的独立有界command queue。
- [x] 21.11 相对当前authority tick校验每个sample的lead/lag窗口。
- [x] 21.12 删除相对上一client tick计算MaximumInputLead/Lag的旧逻辑。
- [x] 21.13 让Authority Source不依赖任一client input horizon持续60Hz推进。
- [x] 21.14 让当前tick精确sample优先于held continuous input。
- [x] 21.15 缺样本时只在配置窗口保持move/facing连续值。
- [x] 21.16 缺样本时清空Attack、Dodge、Combo等离散请求。
- [x] 21.17 超出hold window后提交显式neutral input。
- [x] 21.18 让Actor A缺样本不阻塞Actor B输入选择。
- [x] 21.19 用HelloAck和snapshot authority tick初始化client authority clock estimate。
- [x] 21.20 在Prediction Pipeline state中保存有界clock discipline游标而不保存socket对象。
- [x] 21.21 扩展唯一Prediction Schedule按target slack正常生成一个Current step。
- [x] 21.22 领先不足时让同一Schedule有界生成两个Current step。
- [x] 21.23 领先过多时让同一Schedule有界生成零个Current step。
- [x] 21.24 保证clock correction与Restore/Replay在同一ExecutionPlan中稳定排序。
- [x] 21.25 禁止通过TimeScale、动画速度或第二Update修正simulation clock。

## 22. 建立Network Checkpoint与Delta Snapshot

- [x] 22.1 从validated Program Layout生成稳定`NetworkCheckpointLayout`。
- [x] 22.2 为全部committed Character state slot分配dense index。
- [x] 22.3 为每种committed value kind定义无字符串的固定network codec。
- [x] 22.4 将checkpoint schema、ProgramHash和LayoutHash纳入layout identity。
- [x] 22.5 定义Full Checkpoint dense codec。
- [x] 22.6 让Full Checkpoint包含完整Character state、owner body/world、tick与确认边界。
- [x] 22.7 定义changed-slot bitset codec。
- [x] 22.8 定义changed values delta codec。
- [x] 22.9 定义owner body/world correction codec。
- [x] 22.10 定义remote body、producer和sample time codec。
- [x] 22.11 让snapshot携带SnapshotSequence与BaseSnapshotSequence。
- [x] 22.12 让snapshot携带AuthorityTick与acked InputSequence。
- [x] 22.13 让snapshot携带state/body hash与reliable event horizon。
- [x] 22.14 为worker建立每Client已确认baseline状态。
- [x] 22.15 只相对client已确认baseline编码delta。
- [x] 22.16 让client在command packet中冗余确认最新完整snapshot/base。
- [x] 22.17 让client完整重建checkpoint后校验identity、length和hash。
- [x] 22.18 将重建成功的checkpoint降低为现有Correction输入产品。
- [x] 22.19 未知base或hash错误时拒绝delta；仅SnapshotSequence缺口且base已知时继续重建。
- [x] 22.20 通过KCP发送`FullCheckpointRequest`恢复未知base。
- [x] 22.21 仅在初始化、baseline丢失、布局重置或delta超限时发送Full Checkpoint。
- [x] 22.22 删除routine snapshot中的完整`character-state/float32/v3`bytes。
- [x] 22.23 删除routine snapshot中的逐slot codec identity字符串。
- [x] 22.24 确认checkpoint不按复制policy省略Action、Timeline、Blackboard或GameplayEffect committed slot。
- [x] 22.25 对command与snapshot编码结果执行1200-byte上限检查。
- [x] 22.26 禁止对超限gameplay datagram进行UDP分片。
- [x] 22.27 delta超限时进入checkpoint-required并经KCP发送Full Checkpoint。
- [x] 22.28 Full Checkpoint到达前不应用pose-only或部分state correction。

## 23. 收敛可靠Event与Remote Presentation

- [x] 23.1 在Outer proto中定义`ReliableGameplayEventBatch`。
- [x] 23.2 让可靠Event携带Actor、EventId、event sequence和原始authority tick。
- [x] 23.3 让Authority reliable egress只发送新Event一次。
- [x] 23.4 删除每个snapshot重复携带Event payload的路径。
- [x] 23.5 删除可靠KCP之上的snapshot-ack event重发循环。
- [x] 23.6 保留EventId用于PredictedCommitted、AuthorityConfirmed与SuppressDuplicate。
- [x] 23.7 让Room只按owner/remote控制路由转发可靠Event。
- [x] 23.8 让Client按event sequence拒绝重复与回退batch。
- [x] 23.9 让snapshot只携带reliable event horizon。
- [x] 23.10 让remote event按authority tick与interpolation horizon进入Presentation output。
- [x] 23.11 让remote body/producer只来自20Hz snapshot buffer。
- [x] 23.12 将remote interpolation delay从snapshot cadence中独立配置。
- [x] 23.13 让buffer按authority tick处理丢包、乱序和迟到sample。
- [x] 23.14 禁止remote presentation反向写Gameplay state。

## 24. 建立可证明的网络诊断

- [x] 24.1 为control通道记录packet/s、bytes/s、payload bytes和heartbeat outstanding。
- [x] 24.2 为command通道记录packet/s、bytes/s、payload bytes和queue depth。
- [x] 24.3 为snapshot通道记录packet/s、bytes/s、payload bytes和queue depth。
- [x] 24.4 为reliable event/full checkpoint记录packet/s、bytes/s与应用层queue/pending压力。
- [x] 24.5 禁止反射Fantasy内部KCP发送窗口，以control heartbeat outstanding与应用层可靠/full checkpoint压力作为稳定诊断。
- [x] 24.6 记录UDP packet sequence gap、duplicate、out-of-order和drop reason。
- [x] 24.7 记录datagram编码大小、超限次数和checkpoint-required次数。
- [x] 24.8 记录RTT、jitter与snapshot age。
- [x] 24.9 记录每Actor command lead/lag、missing sample和hold/neutral选择。
- [x] 24.10 记录clock discipline零步、单步和双步次数。
- [x] 24.11 记录baseline hit/miss、delta bytes、full checkpoint bytes和reconstruction失败。
- [x] 24.12 记录remote interpolation buffer occupancy和extrapolation拒绝。
- [x] 24.13 记录correction rate、replay ticks和hard recovery。
- [x] 24.14 将全部指标接入现有只读RuntimeDebugSession。
- [x] 24.15 禁止diagnostics改变发送节奏、queue、clock、correction或failure policy。

## 25. 协议迁移、删除与最终校验

- [x] 25.1 通过正式ProtocolExportTool重新生成client/server控制协议代码。
- [x] 25.2 删除旧KCP input/accepted-input/replication/observation generated消息与Handler。
- [x] 25.3 删除旧统一cadence policy、Inspector字段和资产序列化数据。
- [x] 25.4 删除Room common horizon与pending input merge实现。
- [x] 25.5 删除Room routine baseline/body replaceable queue实现。
- [x] 25.6 删除Authority Endpoint按cadence聚合完整baseline/body/event的实现。
- [x] 25.7 删除Client ObservationFrame原子解包旧路径。
- [x] 25.8 删除routine full Character State network codec调用。
- [x] 25.9 删除可靠Event重复重发与双重确认状态。
- [x] 25.10 更新Prediction与Authority Source port为正式control/data endpoint输入输出。
- [x] 25.11 更新Corin Model、Endpoint、Prediction与Authority composition资产引用。
- [x] 25.12 更新Client与Authority Scene的control/data endpoint配置。
- [x] 25.13 更新实施盘点记录最终协议链、输入输出和删除项。
- [x] 25.14 使用`rg`确认不存在`ObservationCadenceTicks`。
- [x] 25.15 使用`rg`确认不存在common input horizon和Room gameplay relay。
- [x] 25.16 使用`rg`确认routine snapshot不写完整State codec bytes或slot codec字符串。
- [x] 25.17 使用`rg`确认不存在KCP gameplay fallback或第二datagram codec。
- [x] 25.18 使用规定参数编译portable Simulation与Float32工程并立即关闭build server。
- [x] 25.19 使用规定参数编译Unity Runtime与Editor工程并立即关闭build server。
- [x] 25.20 使用规定参数编译Fantasy Server solution并立即关闭build server。
- [x] 25.21 更新`openspec/project.md`与受影响current spec口径。
- [x] 25.22 严格校验本change、DotRecast change与Deterministic Rollback change。

## 26. 收敛四进程构建与启动入口

- [x] 26.1 建立ServerAuthoritative Network Test Player专用Editor构建菜单。
- [x] 26.2 让专用构建固定Bootstrap、Client和Authority Worker三场景顺序且Bootstrap位于第一位。
- [x] 26.3 建立仓库内四进程PowerShell启动脚本并一次启动Server、Authority、Client A和Client B。
- [x] 26.4 为Authority Worker配置显式UDP bind endpoint并纳入launch identity。
- [x] 26.5 让启动脚本检查control endpoint与gameplay data endpoint端口不冲突。
- [x] 26.6 让启动脚本的产物新鲜度检查覆盖datagram codec、checkpoint codec和控制协议源。
- [x] 26.7 让启动脚本在报告成功前验证四进程存活、三个Fantasy control endpoint与两个Client data-plane握手。
- [x] 26.8 让启动失败清理本次创建的全部进程并报告具体角色或构建缺口。
- [x] 26.9 使用规定参数编译Unity Editor程序集并立即关闭build server。
- [x] 26.10 执行本change OpenSpec strict validation。

## 27. 修复积压Snapshot的Observation批处理

- [x] 27.1 从`20260717-112515`日志锁定Client B首个异常为同一`AuthoritativeObservationBatch`包含重复ActorId baseline。
- [x] 27.2 让Prediction Source一次Drain只提交authority tick最新的owner baseline。
- [x] 27.3 保证owner baseline收敛时保留期间全部remote body、producer command和reliable event。
- [x] 27.4 使用规定参数编译Unity Runtime与Editor工程并立即关闭build server。
- [x] 27.5 更新design/spec/实施盘点并执行本change strict validation。

## 28. 修复Prediction回滚后的Command冗余分支

- [x] 28.1 从`20260717-113607`日志锁定Client A首个异常为新预测分支与旧Command历史混合后破坏严格顺序。
- [x] 28.2 让Prediction Command历史在目标authority tick回退或复用时删除该边界及之后的旧分支样本。
- [x] 28.3 使用规定参数编译Unity Runtime与Editor工程并立即关闭build server。
- [x] 28.4 更新design/spec/实施盘点并执行本change strict validation。

## 29. 闭合Authority碰撞场景与Remote插值缓冲

- [x] 29.1 确认Client测试场景的正式墙体来自`wall.prefab`且Authority Worker缺少该碰撞源。
- [x] 29.2 让Authority Worker引用与Client相同位置和尺寸的正式`wall.prefab`。
- [x] 29.3 让Remote Presentation积满`RemoteInterpolationDelayTicks`后才启动authority表现时钟。
- [x] 29.4 使用规定参数编译Unity Runtime与Editor工程并立即关闭build server。
- [x] 29.5 更新design/spec/实施盘点并执行本change strict validation。

## 30. 闭合Prediction变步长表现与离散输入生命周期

- [x] 30.1 从正式日志和代码链锁定本地表现卡顿、循环动画相位漂移与动作请求丢失的独立根因。
- [x] 30.2 让Owner Presentation使用独立simulation sample时钟消费0/1/2步结果，不再重复使用outer tick interpolation alpha。
- [x] 30.3 在Prediction restore/replay替换body历史时保留上一帧可见姿态并在表现层收敛到新canonical body。
- [x] 30.4 让Remote Presentation提前缓存当前body插值区间的SampleProducer，并按稀疏authority tick区间插值Timeline动画时间。
- [x] 30.5 让Prediction Schedule在零Current step时保存Attack、Dodge、Combo等离散请求，并只在下一次首个Current step消费一次。
- [x] 30.6 将待调度离散请求纳入Prediction correction SnapshotParticipant的checkpoint、canonical state与restore。
- [x] 30.7 使用规定参数编译Unity Runtime与Editor工程并立即关闭build server。
- [x] 30.8 更新design/spec/实施盘点并执行本change strict validation。

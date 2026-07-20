# server-authoritative-prediction-correction-pipeline Specification

## Purpose
定义 ServerAuthoritative 客户端 Prediction Pipeline 的输入、时钟、历史、Baseline 合并、Restore/Replay、Output Disposition 和 Remote Presentation 语义。
## Requirements
### Requirement: Client Prediction必须由显式四阶段Pipeline实现

系统 MUST提供`ServerAuthoritativePredictionPipelineDefinition`，显式装配Owner Input Ingress、Authoritative Observation Ingress、Prediction Correction Schedule、标准Float32 Evaluate/WorldSolve/Finalize Step、Prediction History、Output Disposition、Fantasy Command Egress和Remote Presentation Egress Pass。Model Source MUST不隐藏注入Pass，Character Host MUST不创建第二prediction runtime。

#### Scenario: 编译Prediction Pipeline

- **WHEN** Client composition进入Preparing
- **THEN** Pipeline Compiler MUST验证全部Pass/product/Source port/Replay/Restore/Solver requirement
- **AND** MUST产生稳定Prediction PipelineHash和唯一compiled plan

### Requirement: Prediction History必须是正式SnapshotParticipant

Prediction History Pass MUST按SimulationTick有界保存owner canonical input、input sequence、由正式Character State codec产生的committed state canonical bytes、NumericProfile、Target ABI、ProgramHash、LayoutHash、State codec identity、owner World/body state、Prediction Pipeline snapshot、state/body hash、EventId journal cursor，以及该tick实际使用的`ObservedWorldConstraintFrame` canonical bytes与frame hash。History模块 MUST唯一拥有按authority tick排序的Remote Body timeline，并将其capture、restore与hash纳入同一正式SnapshotParticipant；MUST不保存active State Transaction、Pending evaluation、typed mutable partition或GameplayEffect working view，也 MUST不保存在Fantasy Session、MonoBehaviour、static或Character binding中。

#### Scenario: 保存包含远端接触的未确认Tick

- **WHEN** Owner完成SimulationTick 103且该step使用Actor B的ObservedKinematic frame
- **THEN** History MUST同时保存owner input、完整restore identity与精确观察frame
- **AND** connection queue或Remote Presentation MUST不成为该frame的唯一副本

### Requirement: Authority Baseline必须覆盖完整Owner Gameplay恢复状态

网络层 MUST以ProgramHash/LayoutHash锁定的Full/Delta Network Checkpoint表达owner权威状态。Client MUST先通过dense layout重建并校验完整committed Character state、owner body/world baseline、SimulationTick、NumericProfile、Target ABI、checkpoint schema、state/body hash、confirmed input sequence和confirmed EventId horizon，再产生`AuthoritativeActorBaseline`供Correction使用。Routine snapshot MUST不直接携带完整State codec bytes；仅包含position/yaw、motion delta或Animation state的消息 MUST不得用于gameplay reconciliation。

#### Scenario: 收到Pose-only Snapshot

- **WHEN** Observation缺少完整Character state或Program/Layout identity
- **THEN** Correction Schedule MUST拒绝其作为restore baseline

### Requirement: Observation Ingress必须按Actor收敛积压Baseline

Source在一次Prediction Ingress前 MAY已重建同一owner的多个连续snapshot。`AuthoritativeObservationBatch`对每个Actor MUST最多包含一个owner baseline，并 MUST选择authority tick最新的完整baseline供Correction使用；被更新baseline覆盖的中间owner baseline MUST不重复进入同一Actor-scoped product。同期收到的remote body、producer command和reliable event MUST按原authority tick全部保留。Remote body MUST进入Prediction History模块拥有的唯一Remote Body timeline，MUST不绕过Schedule直接成为World constraint，也 MUST不旁路给另一份可独立选择tick的Presentation Body timeline。

#### Scenario: 一帧积累多个Snapshot

- **WHEN** Client主线程一次Drain前已重建owner Tick 300、303和306三个baseline，并收到remote Actor Tick 300至306的Body样本
- **THEN** Observation Ingress MUST只提交Tick 306 owner baseline
- **AND** Remote Body timeline MUST保留形成合法区间所需的全部remote样本
- **AND** MUST不直接写Transform或跳过Schedule生成接触frame

### Requirement: Prediction Schedule必须维持显式Command Slack

Prediction Schedule MUST使用worker握手与snapshot提供的authority tick建立目标预测领先量。正常outer tick产生一个Current step；领先不足 MAY有界产生两个Current step，领先过多 MAY产生零个Current step。clock correction MUST与Restore/Replay由同一Schedule排序，MUST不创建第二Update、Coroutine或runner，也 MUST不通过改变动画播放速度隐藏simulation drift。零Current step时尚未进入simulation的离散request MUST由Schedule正式保存并进入SnapshotParticipant；下一次产生Current step时 MUST只在第一步消费一次。

#### Scenario: Client低于目标Slack两个Tick

- **WHEN** 当前prediction tick相对authority tick低于配置CommandSlackTicks
- **THEN** Schedule MAY在当前outer transaction产生两个有序Current step
- **AND** 两步 MUST各自生成精确target authority tick的input/history

#### Scenario: Client领先目标且本帧按下Attack

- **WHEN** 当前outer transaction产生零Current step且canonical input包含Attack request
- **THEN** Schedule MUST保存该request identity而不是丢弃或发送伪step
- **AND** 下一次首个Current step MUST消费该request一次
- **AND** 同次第二个Current step MUST不重复携带该离散request

### Requirement: 不同Pipeline之间必须通过Baseline Merge而不是交换Snapshot

Authority Pipeline snapshot MUST不得直接恢复到Prediction Pipeline。Correction Pass MUST将合法Authority Baseline与同Tick本地Prediction history record按明确owner规则合并，生成当前Prediction PipelineHash下的完整Character/World/Pipeline restore directive。Pipeline state的history、ack和EventId journal MUST按正式merge/reconstruct合同处理，不得只替换Hash或忽略Pass state。

#### Scenario: Tick 100发生纠偏

- **WHEN** Client拥有Prediction Tick 100 snapshot并收到Authority Baseline 100
- **THEN** Pass MUST用权威Character/body替换权威owned部分
- **AND** MUST重新构造合法Prediction Pipeline snapshot后再Restore

### Requirement: Correction Schedule必须在一个Outer Transaction中恢复和重放

Prediction Correction Schedule MUST是唯一ExecutionPlan producer。无纠偏时MUST只产生Current step；history覆盖时MUST产生完整Restore directive以及严格有序Replay/Current steps；history不覆盖时MUST产生formal HardRecovery或明确失败。GameplayTickSystem MUST只调用一次runtime handle，MUST不创建第二replay runner。

#### Scenario: Restore并重放未确认输入

- **WHEN** authority baseline为100且本地未确认输入为101至103
- **THEN** plan MUST声明Restore 100以及Replay 101、Replay 102、Current 103
- **AND** Backend MUST只在全部step成功后发布最终state和output

### Requirement: HardRecovery必须是正式状态替换而不是Transform Teleport

当baseline Tick已不在history中时，HardRecovery MUST使用最新完整baseline建立新的Prediction Character/World/Pipeline snapshot，清除无法证明有效的unacked history并重置ack/journal cursor。若baseline不能构成完整restore，Session MUST失败。HardRecovery MUST不调用Transform teleport、Motion correction contribution或Presentation反向写Gameplay state。

#### Scenario: History窗口不足

- **WHEN** authority baseline早于client最旧history record
- **THEN** Schedule MAY执行formal HardRecovery
- **AND** MUST不以pose snap保留旧Action/Timeline/GE state

### Requirement: Character状态与Body误差必须分别裁决

Prediction correction MUST对Character state使用canonical hash/identity比较，并对Body使用模型显式position/yaw tolerance。Body tolerance MAY避免无意义视觉恢复，但 MUST不得掩盖Character state、Action、Timeline、Blackboard或GameplayEffect差异。所有threshold MUST进入Pass config hash和PipelineHash。

#### Scenario: Pose接近但Action State不同

- **WHEN** Body误差低于阈值但Character state hash不同
- **THEN** Correction Schedule MUST执行Gameplay restore/replay

### Requirement: Replay Output必须经过EventId Disposition

Prediction Output Disposition Pass MUST以SnapshotParticipant EventId journal处理PredictedCommitted、AuthorityConfirmed、SuppressedDuplicate和PredictedRejected。Replay产生的已提交EventId MUST不再次触发外部副作用；最终reconciled持续Body/Animation状态 MUST重新提交；新EventId MAY在最终Commit发布。已经播放且被权威否定的one-shot MUST记录diagnostics，但MUST不伪造倒放。

#### Scenario: Replay再次产生Attack Cue

- **WHEN** Replay产生已在预测Tick提交过的同一Cue EventId
- **THEN** OutputDisposition MUST标记SuppressDuplicate
- **AND** Cue consumer MUST不再次播放

### Requirement: Authority输入缺失策略必须区分连续值与离散请求

Authority Tick Schedule MUST使用显式、进入PipelineHash的missing-input policy。连续move/facing MAY在有界hold window内沿用最后accepted sample；Attack、Dodge、Combo等离散request MUST永不重复；超过hold window后MUST使用显式neutral input。Room或worker MUST不按未声明默认值猜测输入。

#### Scenario: 某Actor当前Authority Tick无新输入

- **WHEN** 上一accepted sample仍在hold window内
- **THEN** continuous move MAY保持
- **AND** discrete requests MUST为空

### Requirement: Authority Pipeline必须独立执行Canonical Gameplay

系统 MUST提供显式`ServerAuthoritativeAuthorityPipelineDefinition`，装配Accepted Input Ingress、Authority Tick Schedule、标准Float32 Evaluate/WorldSolve/Finalize Step和Authority Replication Egress。Authority Pipeline MUST对完整canonical roster按稳定ActorId执行一次World batch，MUST不消费client applied displacement或prediction state作为权威真值。

#### Scenario: 两Actor Authority Tick

- **WHEN** Authority Schedule产生Actor A/B的Authoritative step
- **THEN** Program/Kernel MUST独立产生两ActorWorldRequest
- **AND** Unity Solver MUST在同一batch返回canonical Body results

### Requirement: Prediction与Authority失败必须保持Session事务边界

任一baseline decode、restore、Replay step、WorldSolve、Finalize、history capture或OutputDisposition失败时，当前outer transaction MUST不发布部分Character/World/Pipeline state或外部output。Authority worker、Fantasy connection或reliable queue失败时Session MUST fail-stop，MUST不切换Local Pipeline、旧Driver或client pose authority。

#### Scenario: Replay中WorldResult不匹配

- **WHEN** Replay 102收到错误Solver identity
- **THEN** Backend MUST拒绝整个outer transaction
- **AND** Replay 101产生的表现与网络输出 MUST不被提交

### Requirement: Prediction State必须由唯一aggregate root协调内部模块

`IServerAuthoritativePredictionStatePort` MUST只暴露一个`ServerAuthoritativePredictionState` aggregate root。该root MUST唯一拥有Confirmation/Request、Prediction History、EventId Disposition Journal与Reconciliation内部模块，并负责跨模块操作顺序。Correction Schedule、History Egress和Output Disposition Pass MUST不分别创建状态、直接持有子模块或维护重复cursor与集合。

#### Scenario: 三个Prediction Pass绑定同一Source port

- **WHEN** Prediction Pipeline创建Correction、History与Disposition Pass runtime
- **THEN** 三者 MUST取得同一个Prediction State aggregate root
- **AND** 每个可变字段 MUST只有一个内部模块owner

### Requirement: Prediction内部模块必须保持明确状态所有权

Confirmation/Request模块 MUST唯一拥有confirmed input/event cursor、authority ack/baseline/clock cursor与pending request；History模块 MUST唯一拥有history record、replay查询和history capacity；Disposition Journal模块 MUST唯一拥有EventId entry、journal cursor、confirmation/rejection与journal capacity；Reconciler MUST只验证identity、计算decision并构造restore plan，不得拥有这些可变集合。

#### Scenario: Authority Ack推进确认

- **WHEN** Prediction State收到合法Authority Ack
- **THEN** Journal模块 MUST计算EventId重分类，Confirmation模块 MUST推进ack与confirmed cursor
- **AND** Reconciler与History MUST不保存第二份confirmation cursor

### Requirement: Prediction跨模块转换必须原子提交

Ack、Authority Baseline与Restore构造 MUST先完成全部identity、horizon、history和capacity验证，再提交Confirmation、History与Journal变化。任一prepare或restore store失败 MUST不得留下部分模块已推进的活动状态；outer Pipeline transaction的checkpoint/rollback MUST继续覆盖三个正式SnapshotParticipant。

#### Scenario: Baseline identity在restore前失败

- **WHEN** Authority Baseline的Program、Solver、Actor或World identity不匹配
- **THEN** Prediction State MUST拒绝该Baseline
- **AND** confirmed cursor、history、journal与pending request MUST全部保持调用前状态

### Requirement: Prediction容量与淘汰策略必须保持单一实现

History模块 MUST继续只淘汰confirmed input record，并在容量不足且最早record仍未确认时明确失败；Journal模块 MUST继续保留live predicted event并使用现有capacity上界。Aggregate root、Pass、Source与Endpoint MUST不复制容量判断、扩大容量、吞掉异常或增加fallback淘汰路径。

#### Scenario: History最早输入仍未确认

- **WHEN** 新history record到达且HistoryCapacity已满，而最早input sequence大于confirmed sequence
- **THEN** History模块 MUST以包含firstTick、firstSequence、confirmedSequence、lastAckTick与lastBaselineTick的结构化上下文失败
- **AND** MUST不删除未确认record或改用另一个history容器

### Requirement: Remote Actor必须保持非Program观察体边界

Client Character simulation roster MUST仍只包含本地owner。Remote actor MUST不创建CharacterSimulationState、不执行Program、不注入伪input、不产生客户端Gameplay output，也 MUST不直接调用Animancer或Transform。ServerAuthoritative Prediction MUST由Schedule选择Remote Body timeline；声明`ObservedKinematicActorContact`能力的Composition MAY把该选择转换为`ObservedKinematic` World constraint，并通过唯一WorldSolve Pass与本地owner一起进入Session装配的WorldSolver。未声明该能力的Composition MUST提交正式空观察frame。Observed actor MUST不产生`CharacterWorldSolveResult`或进入`NextWorldState`。

#### Scenario: Client A预测撞向Actor B

- **WHEN** Actor B的权威Body timeline可为要求观察接触能力的Client A当前step提供合法观察frame
- **THEN** Schedule MUST把Actor B作为ObservedKinematic约束放入同一World batch
- **AND** Client A MUST不运行Actor B的Action Program

#### Scenario: 缺少远端观察frame

- **WHEN** 要求观察接触能力的Current step无法在正式采样策略内取得Actor B的合法Body frame
- **THEN** Prediction transaction MUST失败或进入既有formal HardRecovery
- **AND** MUST不以空约束继续预测

### Requirement: Remote Body采样必须由唯一Schedule决定

Prediction History模块 MUST唯一拥有Remote Body timeline。Prediction Schedule MUST按Current step目标tick执行Exact、区间Interpolation或显式上限内的ConstantVelocityExtrapolation。声明观察接触能力时，Schedule MUST将采样方式、来源authority tick、目标tick和frame hash写入`ObservedWorldConstraintFrame`；未声明时 MUST产生正式空观察frame。最大外推tick MUST进入Model policy、PipelineHash与handshake compatibility。Replay MUST使用History record保存的精确frame，不得用当前最新remote sample重采样过去。

#### Scenario: Current step晚于最新Remote样本

- **WHEN** 目标tick晚于最新权威remote Body且跨度未超过配置上限
- **THEN** Schedule MUST按最后权威速度生成有身份的短时外推frame
- **AND** selected Body frame MUST进入Remote Body表现
- **AND** 声明观察接触能力时同一选择 MUST进入World接触

#### Scenario: Replay已有历史观察frame

- **WHEN** baseline触发Tick 101至103重放
- **THEN** 每个Replay step MUST复用对应History record的观察frame与hash
- **AND** Tick 103之后到达的新remote sample MUST不改变过去World request

### Requirement: Prediction启动必须完成Remote观察预热

Prediction Source完成route与data plane握手后，Prediction Schedule MUST从locked roster确定全部非owner Actor，并保持显式`RemoteObservationPriming`，直到每个remote Actor都拥有合法Body采样anchor。Priming期间 MUST产生零Current step、按既有pending request合同保存离散输入，并 MUST不构造空观察约束或发布selected remote Body。正常Current调度开始后，remote样本缺失或超过外推上限 MUST按正式失败或HardRecovery处理，MUST不退回Priming。

#### Scenario: Data plane Ready但首个Remote Snapshot尚未到达

- **WHEN** Client已完成握手但Remote Body timeline尚不能覆盖locked remote roster
- **THEN** Schedule MUST保持Observation Priming并产生零Current step
- **AND** MUST不启动缺少远端selected Body anchor的预测

#### Scenario: 运行中Remote样本断档

- **WHEN** Prediction已经进入正常调度且目标tick超过remote外推上限
- **THEN** 当前transaction MUST失败或进入既有formal HardRecovery
- **AND** MUST不把运行中断档伪装成新的启动预热

### Requirement: Prediction状态schema必须单路覆盖完整Body恢复状态

Prediction State participant顺序 MUST继续为Correction、History、Journal。Correction MUST破坏性升级为v4，History MUST破坏性升级为v3，Journal MUST保持v2。History canonical payload MUST保存Remote Body timeline、每条record的ObservedWorldConstraintFrame、采样identity、frame hash与包含VerticalVelocity的完整Body恢复状态；Correction checkpoint MUST保存同一Body语义。系统 MUST删除旧Correction/History reader、magic与旧exact-byte要求，MUST不增加兼容reader、双写payload、运行时migrator或第四份Prediction状态。

#### Scenario: Capture History v3

- **WHEN** Prediction aggregate包含远端样本和未确认owner records
- **THEN** History v3 canonical bytes与StateHash MUST覆盖timeline、每tick观察frame及VerticalVelocity
- **AND** Restore后 MUST能生成与原记录相同的Replay World request hash

### Requirement: ServerAuthoritative恢复状态必须覆盖VerticalVelocity

Prediction History、Authority Baseline、Checkpoint、Canonical Egress、Baseline merge与HardRecovery MUST保存、比较并恢复每个owner Body的`VerticalVelocity`。Correction restore/replay MUST从恢复后的VerticalVelocity继续Body Motion Prepare。当前Prediction Correction/History schema MUST分别为v4/v3，Authority Baseline schema MUST为v3；受影响payload MUST单路升级并拒绝旧版本。系统 MUST不以零、actual Velocity.Y、Grounded或客户端Transform补齐缺失字段。

#### Scenario: Authority纠正下落中的Owner

- **WHEN** Authority Baseline包含与本地prediction不同的VerticalVelocity
- **THEN** Body误差裁决 MUST把该差异纳入正式恢复状态
- **AND** restore/replay下一Tick MUST从Authority VerticalVelocity继续积分

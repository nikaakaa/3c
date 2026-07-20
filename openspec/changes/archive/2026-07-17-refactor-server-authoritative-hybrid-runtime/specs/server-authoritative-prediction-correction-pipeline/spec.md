# server-authoritative-prediction-correction-pipeline Specification

## ADDED Requirements

### Requirement: Client Prediction必须由显式四阶段Pipeline实现

系统 MUST提供`ServerAuthoritativePredictionPipelineDefinition`，显式装配Owner Input Ingress、Authoritative Observation Ingress、Prediction Correction Schedule、标准Float32 Evaluate/WorldSolve/Finalize Step、Prediction History、Output Disposition、Fantasy Command Egress和Remote Presentation Egress Pass。Model Source MUST不隐藏注入Pass，Character Host MUST不创建第二prediction runtime。

#### Scenario: 编译Prediction Pipeline

- **WHEN** Client composition进入Preparing
- **THEN** Pipeline Compiler MUST验证全部Pass/product/Source port/Replay/Restore/Solver requirement
- **AND** MUST产生稳定Prediction PipelineHash和唯一compiled plan

### Requirement: Prediction History必须是正式SnapshotParticipant

Prediction History Pass MUST按SimulationTick有界保存owner canonical input、input sequence、由正式Character State codec产生的committed state canonical bytes、NumericProfile、Target ABI、ProgramHash、LayoutHash、State codec identity、owner World/body state、Prediction Pipeline snapshot、state/body hash和EventId journal cursor。该状态 MUST提供canonical capture、restore与hash并进入`SimulationPipelineStateSnapshot`；MUST不保存active State Transaction、Pending evaluation、typed mutable partition或GameplayEffect working view，也 MUST不保存在Fantasy Session、MonoBehaviour、static或Character binding中。

#### Scenario: 保存未确认Tick

- **WHEN** Owner完成SimulationTick 103且authority只ack到100
- **THEN** History MUST保存101至103的canonical input与完整restore identity
- **AND** connection queue MUST不成为该history的唯一副本

### Requirement: Authority Baseline必须覆盖完整Owner Gameplay恢复状态

网络层 MUST以ProgramHash/LayoutHash锁定的Full/Delta Network Checkpoint表达owner权威状态。Client MUST先通过dense layout重建并校验完整committed Character state、owner body/world baseline、SimulationTick、NumericProfile、Target ABI、checkpoint schema、state/body hash、confirmed input sequence和confirmed EventId horizon，再产生`AuthoritativeActorBaseline`供Correction使用。Routine snapshot MUST不直接携带完整State codec bytes；仅包含position/yaw、motion delta或Animation state的消息 MUST不得用于gameplay reconciliation。

#### Scenario: 收到Pose-only Snapshot

- **WHEN** Observation缺少完整Character state或Program/Layout identity
- **THEN** Correction Schedule MUST拒绝其作为restore baseline

### Requirement: Observation Ingress必须按Actor收敛积压Baseline

Source在一次Prediction Ingress前 MAY已重建同一owner的多个连续snapshot。`AuthoritativeObservationBatch`对每个Actor MUST最多包含一个baseline，并 MUST选择authority tick最新的完整baseline供Correction使用；被更新baseline覆盖的中间baseline MUST不重复进入同一Actor-scoped product。同期收到的remote body、producer command和reliable event MUST按原authority tick全部保留，MUST不随owner baseline收敛而丢失。

#### Scenario: 一帧积累多个Snapshot

- **WHEN** Client主线程一次Drain前已重建owner Tick 300、303和306三个baseline
- **THEN** Observation Ingress MUST只提交Tick 306 owner baseline
- **AND** Tick 300至306期间收到的remote presentation samples和reliable events MUST全部保留
- **AND** MUST不直接写Transform继续旧Action/Timeline/GE state

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

### Requirement: Remote Actor必须只消费权威复制进入Presentation

Client simulation roster MUST只包含本地owner。Remote actor MUST通过model-owned remote presentation registration消费authority Body stream、producer commands和reliable EventId facts，并在Prediction Pipeline最终Commit边界进入既有Presentation runtime。Remote actor MUST提前缓存当前Body插值区间右端的SampleProducer，并按前后authority sample tick重采样Timeline动画时间；可靠Select/Complete/Release MUST仍只在authority presentation horizon到达后生效。Remote actor MUST不创建CharacterSimulationState、不执行Program、不注入伪input或直接调用Animancer/Transform。

#### Scenario: Client A显示Actor B攻击

- **WHEN** authority复制Actor B的Attack producer与EventId
- **THEN** Client A MUST通过Projection和共享Animation lifecycle显示remote攻击
- **AND** MUST不在Client A运行Actor B的Action Program

### Requirement: Prediction与Authority失败必须保持Session事务边界

任一baseline decode、restore、Replay step、WorldSolve、Finalize、history capture或OutputDisposition失败时，当前outer transaction MUST不发布部分Character/World/Pipeline state或外部output。Authority worker、Fantasy connection或reliable queue失败时Session MUST fail-stop，MUST不切换Local Pipeline、旧Driver或client pose authority。

#### Scenario: Replay中WorldResult不匹配

- **WHEN** Replay 102收到错误Solver identity
- **THEN** Backend MUST拒绝整个outer transaction
- **AND** Replay 101产生的表现与网络输出 MUST不被提交

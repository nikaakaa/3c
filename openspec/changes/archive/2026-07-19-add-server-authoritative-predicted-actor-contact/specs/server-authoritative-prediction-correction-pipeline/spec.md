## MODIFIED Requirements

### Requirement: Prediction History必须是正式SnapshotParticipant

Prediction History Pass MUST按SimulationTick有界保存owner canonical input、input sequence、由正式Character State codec产生的committed state canonical bytes、NumericProfile、Target ABI、ProgramHash、LayoutHash、State codec identity、owner World/body state、Prediction Pipeline snapshot、state/body hash、EventId journal cursor，以及该tick实际使用的`ObservedWorldConstraintFrame` canonical bytes与frame hash。History模块 MUST唯一拥有按authority tick排序的Remote Body timeline，并将其capture、restore与hash纳入同一正式SnapshotParticipant；MUST不保存active State Transaction、Pending evaluation、typed mutable partition或GameplayEffect working view，也 MUST不保存在Fantasy Session、MonoBehaviour、static或Character binding中。

#### Scenario: 保存包含远端接触的未确认Tick

- **WHEN** Owner完成SimulationTick 103且该step使用Actor B的ObservedKinematic frame
- **THEN** History MUST同时保存owner input、完整restore identity与精确观察frame
- **AND** connection queue或Remote Presentation MUST不成为该frame的唯一副本

### Requirement: Observation Ingress必须按Actor收敛积压Baseline

Source在一次Prediction Ingress前 MAY已重建同一owner的多个连续snapshot。`AuthoritativeObservationBatch`对每个Actor MUST最多包含一个owner baseline，并 MUST选择authority tick最新的完整baseline供Correction使用；被更新baseline覆盖的中间owner baseline MUST不重复进入同一Actor-scoped product。同期收到的remote body、producer command和reliable event MUST按原authority tick全部保留。Remote body MUST进入Prediction History模块拥有的唯一Remote Body timeline，MUST不绕过Schedule直接成为World constraint，也 MUST不旁路给另一份可独立选择tick的Presentation Body timeline。

#### Scenario: 一帧积累多个Snapshot

- **WHEN** Client主线程一次Drain前已重建owner Tick 300、303和306三个baseline，并收到remote Actor Tick 300至306的Body样本
- **THEN** Observation Ingress MUST只提交Tick 306 owner baseline
- **AND** Remote Body timeline MUST保留形成合法区间所需的全部remote样本
- **AND** MUST不直接写Transform或跳过Schedule生成接触frame

## REMOVED Requirements

### Requirement: Remote Actor必须只消费权威复制进入Presentation

**Reason**: 该要求正确禁止了客户端运行远端Program，却错误禁止远端权威Body作为model-neutral World约束进入唯一WorldSolver，导致Client Prediction永远看不见Authority使用的Actor硬接触体。

#### Scenario: 删除Presentation独占Body约束

- **WHEN** Client Prediction为本地owner构造World step
- **THEN** 系统 MUST不再强制remote Body只能进入Presentation
- **AND** 远端Actor仍 MUST不运行Program或拥有CharacterSimulationState

### Requirement: Prediction State模块化不得改变Snapshot身份与字节

**Reason**: History v1没有字段保存Remote Body timeline和每tick实际使用的观察接触frame，继续冻结其exact bytes会使Replay无法重建原World request。本change一次性升级History，Correction与Journal身份继续保持。

#### Scenario: 删除History v1冻结合同

- **WHEN** 新Prediction runtime保存包含ObservedKinematic frame的history
- **THEN** 系统 MUST不再写入或读取History v1 payload

## ADDED Requirements

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

### Requirement: Prediction History schema必须单路升级到v2

Prediction State participant顺序 MUST继续为Correction、History、Journal。Correction v3与Journal v2 MUST保持现有schema；History MUST从v1破坏性升级为v2，并在canonical payload中保存Remote Body timeline、每条record的ObservedWorldConstraintFrame、采样identity与frame hash。系统 MUST删除History v1 reader、magic与旧exact-byte要求，MUST不增加兼容reader、双写payload、运行时migrator或第四份Prediction状态。

#### Scenario: Capture History v2

- **WHEN** Prediction aggregate包含远端样本和未确认owner records
- **THEN** History v2 canonical bytes与StateHash MUST覆盖timeline及每tick观察frame
- **AND** Restore后 MUST能生成与原记录相同的Replay World request hash

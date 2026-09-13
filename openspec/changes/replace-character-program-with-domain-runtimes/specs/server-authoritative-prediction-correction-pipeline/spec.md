## MODIFIED Requirements

### Requirement: Prediction History必须是正式SnapshotParticipant

Prediction History Pass MUST按SimulationTick有界保存owner canonical input、input sequence、由正式Character State codec产生的committed state canonical bytes、NumericProfile、Target ABI、GameplayContentHash、StateSchemaHash、State codec identity、owner World/body state、Prediction Pipeline snapshot、state/body hash、EventId journal cursor，以及该tick实际使用的`ObservedWorldConstraintFrame` canonical bytes与frame hash。History模块 MUST唯一拥有按authority tick排序的Remote Body timeline，并将其capture、restore与hash纳入同一正式SnapshotParticipant；MUST不保存active State Transaction、Pending evaluation、typed mutable partition或GameplayEffect working view，也 MUST不保存在Fantasy Session、MonoBehaviour、static或Character binding中。

#### Scenario: 保存包含远端接触的未确认Tick

- **WHEN** Owner完成SimulationTick 103且该step使用Actor B的ObservedKinematic frame
- **THEN** History MUST同时保存owner input、完整restore identity与精确观察frame
- **AND** connection queue或Remote Presentation MUST不成为该frame的唯一副本


### Requirement: Authority Baseline必须覆盖完整Owner Gameplay恢复状态

网络层 MUST以GameplayContentHash/StateSchemaHash锁定的Full/Delta Network Checkpoint表达owner权威状态。Client MUST先通过dense layout重建并校验完整committed Character state、owner body/world baseline、SimulationTick、NumericProfile、Target ABI、checkpoint schema、state/body hash、confirmed input sequence和confirmed EventId horizon，再产生`AuthoritativeActorBaseline`供Correction使用。Routine snapshot MUST不直接携带完整State codec bytes；仅包含position/yaw、motion delta或Animation state的消息 MUST不得用于gameplay reconciliation。

#### Scenario: 收到Pose-only Snapshot

- **WHEN** Observation缺少完整Character state或角色玩法/Layout identity
- **THEN** Correction Schedule MUST拒绝其作为restore baseline


### Requirement: Authority Pipeline必须独立执行Canonical Gameplay

系统 MUST提供显式`ServerAuthoritativeAuthorityPipelineDefinition`，装配Accepted Input Ingress、Authority Tick Schedule、标准Float32 Evaluate/WorldSolve/Finalize Step和Authority Replication Egress。Authority Pipeline MUST对完整canonical roster按稳定ActorId执行一次World batch，MUST不消费client applied displacement或prediction state作为权威真值。

#### Scenario: 两Actor Authority Tick

- **WHEN** Authority Schedule产生Actor A/B的Authoritative step
- **THEN** 角色玩法/Kernel MUST独立产生两ActorWorldRequest
- **AND** Unity Solver MUST在同一batch返回canonical Body results


### Requirement: Prediction跨模块转换必须原子提交

Ack、Authority Baseline与Restore构造 MUST先完成全部identity、horizon、history和capacity验证，再提交Confirmation、History与Journal变化。任一prepare或restore store失败 MUST不得留下部分模块已推进的活动状态；outer Pipeline transaction的checkpoint/rollback MUST继续覆盖三个正式SnapshotParticipant。

#### Scenario: Baseline identity在restore前失败

- **WHEN** Authority Baseline的角色玩法、Solver、Actor或World identity不匹配
- **THEN** Prediction State MUST拒绝该Baseline
- **AND** confirmed cursor、history、journal与pending request MUST全部保持调用前状态


### Requirement: Remote Actor必须保持非Program观察体边界

Client Character simulation roster MUST仍只包含本地owner。Remote actor MUST不创建CharacterSimulationState、不执行角色玩法、不注入伪input、不产生客户端Gameplay output，也 MUST不直接调用Animancer或Transform。ServerAuthoritative Prediction MUST由Schedule选择Remote Body timeline；声明`ObservedKinematicActorContact`能力的Composition MAY把该选择转换为`ObservedKinematic` World constraint，并通过唯一WorldSolve Pass与本地owner一起进入Session装配的WorldSolver。未声明该能力的Composition MUST提交正式空观察frame。Observed actor MUST不产生`CharacterWorldSolveResult`或进入`NextWorldState`。

#### Scenario: Client A预测撞向Actor B

- **WHEN** Actor B的权威Body timeline可为要求观察接触能力的Client A当前step提供合法观察frame
- **THEN** Schedule MUST把Actor B作为ObservedKinematic约束放入同一World batch
- **AND** Client A MUST不运行Actor B的Action Program

#### Scenario: 缺少远端观察frame

- **WHEN** 要求观察接触能力的Current step无法在正式采样策略内取得Actor B的合法Body frame
- **THEN** Prediction transaction MUST失败或进入既有formal HardRecovery
- **AND** MUST不以空约束继续预测


## ADDED Requirements

### Requirement: Baseline必须原子恢复完整领域状态

Authority baseline 和 Prediction History MUST保存同一 Tick 已提交的控制机器、输入请求、技能调用／Timeline／实例、效果、装备和其它跨 Tick 玩法状态，并与原 World／Pipeline 状态共同恢复。原 state/body 误差裁决、remote observed actor、journal 和输出去重 MUST保持；恢复 MUST不只设置位置或活动技能名。

#### Scenario: 恢复活动技能与控制状态
- **WHEN** 纠正点位于动作中段且存在有效控制状态与效果
- **THEN** 系统 MUST先完整解码候选，再原子恢复各领域与世界并按原策略重放


# character-network-sync-domain-contract Specification

## MODIFIED Requirements

### Requirement: MotionSyncDomain 必须处理连续运动同步

MotionSyncDomain MUST表达 portable Actor input identity、WorldRequest identity、predicted/committed body observation 和 WorldSolverResult summary。Simulation Core MUST不生成 packet、correction command 或 ack；model-owned Driver/adapter MUST从 simulation ports 映射自己的协议，并 MUST不把客户端 actual displacement 定义为服务端 canonical motion intent。

#### Scenario: Local Motion 完成

- **WHEN** SessionRuntime 完成当前 Tick 的 WorldSolver batch
- **THEN** Tick result MUST产生 portable body/motion observation
- **AND** MUST不包含 ServerAuthoritative correction policy

### Requirement: History 必须按 policy 使用而非强制全局回滚

Simulation Core MUST只提供 canonical Character/World snapshot、atomic restore 和 hash 能力，MUST不默认创建网络 history。History schema、容量、保留、restore/replay、authoritative recovery 和 OutputPlan policy MUST由具体 Network Model Driver 拥有。

#### Scenario: Local Driver 不保存 History

- **WHEN** Session 使用 Local Driver
- **THEN** Core MUST允许不创建任何 model history

### Requirement: PresentationSyncDomain 必须处理表现事件

PresentationSyncDomain MUST表达 VFX、SFX、camera shake、hit stop、post-process cue 和 animation cue，并使用稳定 EventId。Presentation command 默认 MAY为 local-only；需要复制时，具体 Model Output Adapter MAY从 committed typed fact 构造 packet。Program/Timeline MUST不直接发送，Committer 本地状态 MUST不进入 packet。

#### Scenario: 远端需要看到表现

- **WHEN** model policy 要求复制某个 committed cue
- **THEN** model-owned Output Adapter MAY从对应 PresentationSyncDomain fact 构造消息
- **AND** MUST不读取 Camera/Animancer runtime state

### Requirement: Blackboard 变量不得默认网络同步

Pipeline Blackboard variable MUST不默认进入 SimulationOutput 或模型同步。只有 declaration 配置合法 fact projection 且当前写入具有所需 provenance 时，Program MUST生成 typed SyncDomain fact；Model Output Adapter MUST只消费投影后的 fact，MUST不读取 CharacterSimulationState Blackboard slots。

#### Scenario: 本地调试变量

- **WHEN** Blackboard variable 只用于本地条件或 diagnostics
- **THEN** MUST保持在 CharacterSimulationState
- **AND** model adapter MUST不可读取其 key/value

#### Scenario: 变量投影为 SyncFact

- **WHEN** declaration 与当前写入满足正式 fact projection
- **THEN** Program MUST生成带稳定 identity 的 typed fact
- **AND** 后续 Model MAY按自己的 policy 消费该 fact

### Requirement: Gameplay Effect 网络策略必须由模型 Profile 解析

EffectDefinition MUST只提供稳定 EffectId、BehaviorId 和 Effect kind。GameplayEffect output 的 prediction、authority、replication、sync 与 history policy MUST由具体 Network Model profile/Driver 按 BehaviorId 解析。Program、CharacterSimulationState、GameplayEffect operation、Committer 和旧 Character stage MUST不保存或解析模型策略。

#### Scenario: LocalOnly Effect

- **WHEN** 后续模型 profile 将 Effect BehaviorId 配置为 LocalOnly
- **THEN** Local Gameplay 与 diagnostics MAY处理其 lifecycle fact
- **AND** model-owned Output Adapter MUST不构造 outgoing packet

#### Scenario: ClientPredicted Effect

- **WHEN** 模型为 Effect 配置 action-scoped history
- **THEN** Driver history MUST只记录该模型所需 snapshot/fact identity
- **AND** Core MUST不强制全世界 rollback

## ADDED Requirements

### Requirement: Model Output Adapter 必须从 Tick Result 构造模型输出

Model-owned Output Adapter MUST只消费 SimulationTickPlan input identity、SimulationTickResult、SimulationWorldSnapshot identity 和 typed SyncDomain facts，并保留 ActorId、BehaviorId、ActionInstanceId、SimulationTick 与 EventId。Packet mapping、filter、queue 和 history MUST归具体 model。

#### Scenario: 后续模型生成 Motion Command

- **WHEN** model Driver 需要发送 canonical input command
- **THEN** MUST从 portable Tick plan input 构造并由 model session 入队
- **AND** MUST不读取 Character NetworkSendStage

### Requirement: Model Input Adapter 必须通过 Driver 提交模型语义

Model-owned Input Adapter MUST把 incoming packet 转换为 Driver-owned canonical control input、typed SimulationIngress、完整 restore candidate 或 OutputPlan metadata。Kernel MUST不接收原始 packet，Common SessionHost MUST不解释这些模型语义。

#### Scenario: 服务端动作确认

- **WHEN** 后续 ServerAuthoritative adapter 收到 ActionDecision
- **THEN** model Driver MUST按 Actor/ActionInstance/history 对齐该 observation并生成 typed ActionLifecycle ingress
- **AND** MUST通过正式 Tick input/ingress/restore/OutputPlan 边界影响 gameplay 或表现

## REMOVED Requirements

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包

**Reason**：公共 Character NetworkSendStage 与 model Driver 同时保存和投影输出会形成双写，并把模型发送时序固定进 Core。

**Migration**：删除 CharacterNetworkSendStage。后续 model-owned Output Adapter 只消费 Tick plan/result/snapshot ports。

#### Scenario: 删除旧输出 Stage

- **WHEN** Local Core 完成迁移
- **THEN** Character runtime MUST不存在 NetworkSendStage
- **AND** MUST不保留无模型归属的 outgoing queue

### Requirement: NetworkReceiveStage 必须按 SyncDomain 注入网络结果

**Reason**：不同 Network Model 的 canonical input、authoritative observation、restore 和 remote presentation 不共享一个 Character receive stage。

**Migration**：删除 CharacterNetworkReceiveStage、ExternalPoseSample、ExternalPoseCorrection 与公共 Action lifecycle 注入 stage。后续 model Driver 通过自己的正式边界接入。

#### Scenario: 删除旧输入 Stage

- **WHEN** 旧 ServerAuthoritative Character adapter 被删除
- **THEN** Core MUST不存在 ExternalPose/correction 公共入口
- **AND** MUST不建立兼容 wrapper

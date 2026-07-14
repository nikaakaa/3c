# character-network-sync-domain-contract Specification

## MODIFIED Requirements

### Requirement: MotionSyncDomain 必须处理连续运动同步

MotionSyncDomain MUST表达portable simulation input identity、predicted body result、authoritative body observation、solver result和model commit/recovery observation。Character SimulationKernel MUST不生成model packet、correction command或ack；具体Model Driver/adapter MUST选择所需input/state/facts并构造协议。Resolved motion MUST只表达某次Program/solver执行结果，MUST不被定义为服务端canonical intent。

#### Scenario: 本地运动完成

- **WHEN** Kernel/World Solver 完成本Tick
- **THEN** MUST产生portable body/motion observation
- **AND** ServerAuthoritative MAY用于prediction comparison
- **AND** Rollback MAY用于state hash

### Requirement: History 必须按 policy 使用而非强制全局回滚

History 内容、容量、恢复和提交规则 MUST由当前 Network Model Driver拥有。ServerAuthoritative history MUST服务prediction/reconciliation和remote snapshot；DeterministicRollback history MUST保存canonical input和完整world snapshot。SimulationKernel、Character authoring和SyncFacts MUST不持有model history或把两类history混用。

#### Scenario: 两个模型保存历史

- **WHEN**相同Corin Program分别运行在两个模型中
- **THEN**每个model MUST使用自己的history schema和policy
- **AND**Program/Graph MUST不因模型改变

## ADDED Requirements

### Requirement: Model Output Adapter 必须按 SyncDomain 构造模型输出

Simulation output adapter MUST只暴露portable input、body result和SyncDomain facts，并保留BehaviorId、ActionId、ActorId、SimulationTick和稳定identity。Model-owned adapter MUST决定filter、history和packet mapping；旧CharacterNetworkSendStage若与Driver双写 MUST删除。

#### Scenario: 单机没有Network Model

- **WHEN** Local Driver运行且没有model session
- **THEN** SimulationKernel MUST继续产生相同state/facts
- **AND** facts MAY只供Presentation、recording和diagnostics消费

### Requirement: Model Input Adapter 必须将协议转换为模拟输入

Model-owned Driver MUST把incoming packet转换为canonical simulation input、authoritative world snapshot、action/effect semantic facts或presentation sample。SimulationKernel MUST不接收原始packet；旧ExternalPoseCorrection、ExternalPoseSample和MotionStage correction输入 MUST不再作为通用Character主线。

#### Scenario: 服务端动作确认

- **WHEN** ServerAuthoritative adapter 收到ActionDecision
- **THEN** Driver MUST对齐对应Actor/ActionInstance/history
- **AND** Kernel/runtime MUST只消费正式lifecycle/state更新

#### Scenario: Rollback 输入bundle

- **WHEN** Rollback adapter 收到canonical bundle
- **THEN** Driver MUST写入model history并决定forward或replay
- **AND** MUST不转换为ServerAuthoritative correction

## REMOVED Requirements

### Requirement: NetworkSendStage 必须按 SyncDomain 和 policy 打包

**Reason**：packet、filter 和 history 是具体 Network Model 的语义，公共 Character NetworkSendStage 会与 Driver 形成双写。

**Migration**：SimulationKernel 只输出 portable facts/state observations，model-owned Output Adapter 按自己的协议构造 packet 与 history。

#### Scenario: 迁移输出

- **WHEN** 旧 NetworkSendStage 与 Driver 消费同一事实
- **THEN** 必须删除旧 stage
- **AND** 仅保留 model-owned Output Adapter

### Requirement: NetworkReceiveStage 必须按 SyncDomain 注入网络结果

**Reason**：ServerAuthoritative correction 与 Rollback canonical input 不是同一种公共“网络结果”，通用 receive stage 会泄漏模型策略。

**Migration**：各 Model Input Adapter 将 packet 转换为canonical input、authoritative snapshot 或模型自有 history 记录，再由 Driver 决定 forward、reconcile 或 replay。

#### Scenario: 迁移输入

- **WHEN** 旧 stage 接收 ExternalPoseCorrection 或 ExternalPoseSample
- **THEN** 必须迁移到对应 model Driver 合同
- **AND** Character 主线不得保留通用 correction 入口

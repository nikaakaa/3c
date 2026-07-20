## MODIFIED Requirements

### Requirement: Model Output Adapter 必须从 Tick Result 构造模型输出

Model-owned Output Adapter/Egress Pass MUST只消费 ExecutionPlan input identity、SimulationStepResult、Session Snapshot identity与 typed SyncDomain facts，并保留 ActorId、BehaviorId、ActionInstanceId、SimulationTick与 EventId。Packet mapping、filter、queue和 history MUST归具体 Model Source/Pass，MUST不读取 Character私有 stage或 mutable Program state。

#### Scenario: 后续模型生成 Motion Command

- **WHEN** Model Source需要发送 canonical input command
- **THEN** MUST从 portable ExecutionPlan step input构造并由 model session入队
- **AND** MUST不读取 Character私有输出 stage

## REMOVED Requirements

### Requirement: Model Input Adapter 必须通过 Driver 提交模型语义

**Reason**: 单 Tick Driver删除，模型输入改由 Session Source和显式 Ingress/Schedule Pass提交。

**Migration**: packet必须先转换为 Source-owned canonical input、typed ingress、restore candidate或 Schedule/Egress metadata。

## ADDED Requirements

### Requirement: Model Input Adapter 必须通过 Source 与 Pipeline Pass 提交模型语义

Model-owned Input Adapter/Ingress Pass MUST把 incoming packet转换为 Source-owned canonical control input、typed SimulationIngress、完整 restore candidate或 Schedule/Egress metadata。Kernel MUST不接收原始 packet，Common SessionHost MUST不解释这些模型语义，Source MUST不直接修改 Character/World/Pipeline working state。

#### Scenario: 服务端动作确认

- **WHEN** 后续 ServerAuthoritative adapter收到 ActionDecision
- **THEN** Model Source/Ingress Pass MUST按 Actor、ActionInstance和 history对齐 observation并生成 typed ActionLifecycle ingress
- **AND** MUST通过正式 Pipeline product、ExecutionPlan或 Egress边界影响 Gameplay和表现


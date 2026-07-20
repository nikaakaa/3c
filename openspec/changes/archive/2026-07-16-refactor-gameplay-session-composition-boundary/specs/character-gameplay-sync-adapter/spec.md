## MODIFIED Requirements

### Requirement: Character 必须通过 Model-owned Adapter 接入网络模型

完整 Network Model MUST通过 Model-owned Session Source、Input/Output adapter和显式 Pipeline Pass接入唯一 Session Pipeline。Adapter MUST只连接 Source ports、typed Pipeline products、ExecutionPlan restore/step与 Egress OutputDisposition。Character Core MUST不引用具体 model packet、profile、endpoint、history或 adapter type；Egress OutputDisposition MUST不拥有 Gameplay state接受权。

#### Scenario: 当前没有完整模型

- **WHEN** 旧 CharacterServerAuthoritativeAdapter已删除且新 Model Source/Pipeline尚未实现
- **THEN** 显式 Local Composition MUST正常运行
- **AND** SessionHost MUST不创建半成品 Model adapter或回退 Local Pipeline

### Requirement: Outgoing Adapter 必须从事实构造模型命令

Model-owned Output Adapter/Egress Pass MUST只从 ExecutionPlan identity、SimulationStepResult、Session Snapshot identity与 typed SyncDomain facts构造模型命令。Adapter MUST不读取 Blackboard state、Graph path、WorldSolver internals、Pipeline working state或 Presentation runtime state。

#### Scenario: 构造 canonical input command

- **WHEN** 后续模型需要发送 Actor control input
- **THEN** Adapter MUST从 ExecutionPlan Step的 portable input构造
- **AND** actual body result MAY只按模型策略作为 observation metadata

### Requirement: Incoming Adapter 必须先转换为 Character 语义输入

Model-owned Input Adapter/Ingress Pass MUST将 packet转换为 canonical control input、typed SimulationIngress、完整 restore candidate或 Schedule/Egress metadata。Kernel、Program、Execution Backend与 Common SessionHost MUST不接收原始 packet。

#### Scenario: 收到 Action reject

- **WHEN** 后续模型收到 ActionDecision reject
- **THEN** Adapter MUST按 model history对齐后产生 ActionLifecycle ingress
- **AND** MUST不调用 mutable Action object或绕过正式 Pipeline product

### Requirement: Adapter Policy 必须只来自当前模型 Profile

Filter、prediction、authority、history、restore、replication与 OutputDisposition policy MUST只来自当前模型 Profile、Session Source和模型 Pass config。Program、ActionProfile、GameplayEffectDefinition、Graph、Timeline、Common Pipeline Compiler和 Committer MUST不解析模型 policy。

#### Scenario: Effect 为 LocalOnly

- **WHEN** Model Profile将某 Effect BehaviorId配置为 LocalOnly
- **THEN** 模型 Egress Pass MUST不为其构造 outgoing message
- **AND** Program产生的 Gameplay Fact MUST保持不变

### Requirement: Character Binding 必须精确绑定 Session Actor

Model actor binding MUST以稳定 ActorId将 model session roster与 SimulationSessionHost锁定 roster精确绑定。Binding MUST不各自创建 Session Source、Pipeline Runtime、WorldSolver、history或 endpoint，也 MUST不按 GameObject name、Graph identity或 TargetActorId猜测 subject。

#### Scenario: 两个 Actor 共享 Session

- **WHEN** 后续 Model session绑定 ActorA与 ActorB
- **THEN** 两个 binding MUST复用同一 Model Source、Pipeline Runtime与 composition
- **AND** ingress/result MUST按 Subject ActorId精确路由


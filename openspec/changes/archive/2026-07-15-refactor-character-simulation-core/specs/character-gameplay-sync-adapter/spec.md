# character-gameplay-sync-adapter Specification

## MODIFIED Requirements

### Requirement: Character 必须通过 Model-owned Adapter 接入网络模型

完整 Network Model MUST通过 model-owned Simulation Driver 与 adapter 接入 SimulationSessionRuntime。Adapter MUST只连接 Tick plan control input、typed SimulationIngress、restore request、Tick result observation 和 SimulationOutputPlan。Character Core MUST不引用具体 model packet、profile、endpoint、history 或 adapter type；OutputPlan MUST不拥有 Gameplay state 接受权。

#### Scenario: 当前没有完整模型

- **WHEN** 旧 CharacterServerAuthoritativeAdapter 已删除且新 Driver 未实现
- **THEN** Local Session MUST正常运行
- **AND** SessionHost MUST不创建半成品 model adapter

### Requirement: Outgoing Adapter 必须从事实构造模型命令

Model-owned Output Adapter MUST只从 Driver 保存的 Tick plan identity、SimulationTickResult、SimulationWorldSnapshot identity 与 typed SyncDomain facts 构造模型命令。Adapter MUST不读取 Blackboard state、Graph path、WorldSolver internals 或 Presentation runtime state。

#### Scenario: 构造 canonical input command

- **WHEN** 后续模型需要发送 Actor control input
- **THEN** Adapter MUST从 Tick plan 的 portable input 构造
- **AND** actual body result MAY只按模型策略作为 observation metadata

### Requirement: Incoming Adapter 必须先转换为 Character 语义输入

Model-owned Input Adapter MUST在 Driver 内将 packet 转换为 canonical control input、typed SimulationIngress、完整 restore candidate 或 Driver-owned OutputPlan metadata。Kernel、Program 与 Common SessionHost MUST不接收原始 packet。

#### Scenario: 收到 Action reject

- **WHEN** 后续模型收到 ActionDecision reject
- **THEN** Adapter MUST按 model history 对齐后产生 ActionLifecycle ingress
- **AND** MUST不调用 ActionRuntime object 或旧 NetworkReceiveStage

### Requirement: Adapter Policy 必须只来自当前模型 Profile

Filter、prediction、authority、history、restore、replication 与 OutputPlan policy MUST只来自当前模型的 Profile/Driver configuration。Program、ActionProfile、GameplayEffectDefinition、Graph、Timeline 和 Committer MUST不解析模型 policy。

#### Scenario: Effect 为 LocalOnly

- **WHEN** model profile 将某 Effect BehaviorId 配置为 LocalOnly
- **THEN** Output Adapter MUST不为其构造模型消息

### Requirement: Character Binding 必须精确绑定 Session Actor

Model actor binding MUST以稳定 ActorId 将 model session roster 与 SimulationSessionRuntime roster 精确绑定。Binding MUST不各自创建 Driver、WorldSolver、history 或 endpoint，也 MUST不按 GameObject name、Graph identity 或 TargetActorId 猜测 subject。

#### Scenario: 两个 Actor 共享 Session

- **WHEN** 后续 model session 绑定 ActorA 与 ActorB
- **THEN** 两个 binding MUST复用同一 model Driver composition
- **AND** ingress/result MUST按 Subject ActorId 精确路由

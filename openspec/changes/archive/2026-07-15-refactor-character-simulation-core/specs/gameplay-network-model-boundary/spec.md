# gameplay-network-model-boundary Specification

## MODIFIED Requirements

### Requirement: Gameplay Network Model 必须是 Session 级唯一装配

GameplayNetworkSessionHost MUST通过显式 GameplayNetworkModelDefinition 创建唯一 model session 与 model-owned Simulation Driver composition。Common Host MUST不硬编码已知 model 类型，Character、Graph、Program、Kernel 和 WorldSolver MUST不保存 model selection。没有完整 Network Model 时，Local Simulation Session MUST可独立运行且不创建 SessionHost fallback。

#### Scenario: 当前核心只运行 Local Session

- **WHEN** 旧 ServerAuthoritative Character adapter 已删除且正式 Model Driver 尚未实现
- **THEN** Sandbox MUST只创建 Local Simulation Session
- **AND** MUST不把 Local Driver 宣称为 Network Model

### Requirement: Model、Endpoint 和 Transport 必须分层

每个 ModelDefinition MUST通过自己的 EndpointDefinition 合同创建 endpoint/protocol adapter，并通过 model-owned factory 创建 Driver composition。Common Host、Character 和 Graph MUST不保存 endpoint enum/switch；Endpoint/Transport MUST不改变模型的输入、history、restore 或 commit 语义。

#### Scenario: Model 创建 Endpoint

- **WHEN** 后续完整 ModelDefinition 创建 session
- **THEN** MUST只接受兼容其 protocol 与 capability 的 EndpointDefinition

### Requirement: 只允许选择完整实现的 Network Model

ModelDefinition MUST声明所需 Program、Driver、Actor binding、WorldSolver、Endpoint/Protocol 和 Host capabilities。Inspector/Host MUST只列出全部能力可创建的完整 model。只有 packet/session/endpoint 或旧 adapter 残留的 definition MUST不可选。

#### Scenario: ServerAuthoritative 缺少新 Driver Adapter

- **WHEN** ServerAuthoritative module 仍有 packet/session/LocalLoopback 但没有正式 Simulation Driver composition
- **THEN** Inspector MUST报告缺失能力并禁止运行
- **AND** MUST不回退旧 NetworkStage adapter

### Requirement: Common Session Host 不得解释模型消息

Common SessionHost MUST只管理 ModelDefinition、model session lifecycle、Simulation Driver composition ownership 和 actor roster registration。Packet、canonical input、history、correction、rollback、snapshot recovery、hash exchange 和 commit policy MUST归具体 model，Common Host MUST不解析。

#### Scenario: Endpoint 收到 Model Message

- **WHEN** 当前 Endpoint 收到模型消息
- **THEN** MUST交给该 model session/Driver 处理
- **AND** Common Host MUST不把消息转换成 Character stage input

### Requirement: Character Runtime 必须通过事实和语义输入连接模型

Character Core MUST只暴露 CharacterSimulationInput、SimulationTickResult、SimulationWorldSnapshot、typed gameplay facts、body observations 和 EventId commands。Model-owned Driver/adapter MUST通过这些 ports 连接，MUST不让 Core 引用 packet、history、policy、server tick 或 correction DTO。

#### Scenario: Model Adapter 接入角色模拟

- **WHEN** 后续完整 ModelDefinition 绑定 Actor roster
- **THEN** MUST通过 Driver tick plan、restore request、result observation 与 SimulationOutputPlan 接入
- **AND** MUST不恢复 Character NetworkSend/ReceiveStage

### Requirement: Character 输入来源与运动权威必须正交

Actor control input MUST由当前 Driver 的 Tick plan 提供；world constraint 与 body result MUST由 Session 装配的唯一 WorldSolver 提供。Program 与 Character state MUST不使用 CharacterMotionAuthority、LocalPredicted、RemoteProxy 或具体 Network Model enum 总控行为。后续模型可以为不同 Actor 提供不同 input/ingress，但同一 Session 的 world mutation仍必须经过统一 batch Solver。

#### Scenario: Local Session Owner

- **WHEN** Local Session 创建 Corin
- **THEN** Local Driver MUST提供设备 input
- **AND** Unity WorldSolver MUST提供 body result

#### Scenario: 后续模型拥有远端 Actor

- **WHEN** Network Model 为远端 Actor 提供 canonical input 或 typed ingress
- **THEN** MUST通过 Driver Tick plan 接入
- **AND** MUST不把 Actor 伪装成 CharacterPipeline RemoteProxy 模式

### Requirement: BTSMTL Authoring 不得拥有 Network Model 配置

Graph、StateMachine、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 CharacterSimulationProgram MUST不保存 ModelId、Endpoint、Transport、history、correction、rollback 或 WorldSolver implementation selection。Program MAY只声明 model-neutral required capabilities。

#### Scenario: 复用同一 Program

- **WHEN** 同一 Program 被 Local 与后续 Network Driver 使用
- **THEN** BTSMTL authoring MUST保持不变

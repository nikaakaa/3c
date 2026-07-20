## MODIFIED Requirements

### Requirement: Gameplay Network Model 必须是 Session 级唯一装配

Gameplay Network Model MUST作为 `SimulationSessionSourceDefinition` 的一种实现，通过实际 runtime factory创建 model session、Endpoint、history与显式 Source ports。唯一 `SimulationSessionHost` MUST使用同一 Composition Definition将该 Source与显式 Program Runtime、Execution Backend、Pipeline Definition、WorldSolver、ProgramCatalog、roster、Committer和 diagnostics组合。Common Host MUST不硬编码已知 Model类型，Character、Graph、Program、Kernel、Pipeline Backend和 WorldSolver MUST不保存 Model selection。Local Source MUST可独立使用同一 Session Host，但 MUST不被声明为 Network Model。

#### Scenario: 当前核心只运行 Local Session

- **WHEN** 已安装 Network Model都没有完整 Source factory与合法 Pipeline
- **THEN** Local Session MUST通过显式 Local Source和 Standard Local Pipeline正常创建
- **AND** Host MUST不创建 GameplayNetworkModelSession或把 Local当作 fallback Model

### Requirement: Model、Endpoint 和 Transport 必须分层

每个 GameplayNetworkModelDefinition MUST通过自己的 EndpointDefinition与 runtime factory创建 endpoint/protocol adapter、history和 Source ports。Model模块 MAY提供模型专属 Pass Definition与 Pipeline Definition，但 Composition MUST显式选择 Pipeline，Model MUST不在 Host中隐藏注入。WorldSolver implementation、Program Runtime、Character authoring、Execution Backend和 Presentation playback MUST不归 Model。Endpoint/Transport MUST不改变模型的 input/history/restore/commit语义。

#### Scenario: ServerAuthoritative 使用不同服务端 Solver

- **WHEN** 同一 ServerAuthoritative Model与同一模型 Pipeline分别搭配 Unity authoritative Solver或 DotRecast authoritative Solver
- **THEN** ModelId、packet/history、Source ports与客户端 correction语义 MUST保持同一实现
- **AND** Solver backend MUST由服务端 Composition显式选择，不得生成 DotRecastNetworkModel

### Requirement: 只允许选择完整实现的 Network Model

Network Model只有在 ModelDefinition、Source runtime factory、EndpointDefinition、protocol capability、所选 Pipeline及全部模型 Pass factory、Program Runtime/Backend requirement、Solver capability requirement与 preparation合同完整时才 MAY被 Session composition选择。手写 capability位、存在 packet/session类、空 factory、旧 adapter或只有 Pipeline显示名 MUST不能让 Model被视为完整。Host MUST在 preparation Ready与 Pipeline compile后再次校验实际 LaunchPlan。

#### Scenario: ServerAuthoritative 缺少 Correction Pass factory

- **WHEN** ServerAuthoritative模块只有 packet、queue、history、Endpoint与 Source factory，但所选 Prediction Pipeline的一个 Pass factory缺失
- **THEN** composition MUST报告缺少精确 Pass/version
- **AND** MUST不回退旧 NetworkStage、Local Pipeline或 capability位伪装可用

### Requirement: Common Session Host 不得解释模型消息

SimulationSessionHost MUST只管理 Composition Definition、preparation lifecycle、compiled Pipeline identity、numeric-neutral runtime handle、Actor registration与 Tick registration。Packet、canonical input、history、correction、rollback、snapshot recovery、hash exchange、ack和模型 commit policy MUST归具体 Model Source与模型 Pass。Common Host MUST不解析 Model Message，也 MUST不把消息转换成 Character input或 Pipeline product。

#### Scenario: Endpoint 收到 Model Message

- **WHEN** 当前 Endpoint收到 ServerAuthoritative或 Rollback消息
- **THEN** MUST交给对应 Model Source runtime及显式 Ingress/Schedule Pass处理
- **AND** Session Host MUST只观察 preparation/runtime lifecycle与 Pipeline diagnostics

### Requirement: Character Runtime 必须通过事实和语义输入连接模型

Character Core MUST只暴露 CharacterSimulationInput、typed SimulationIngress、SimulationStepResult、Session Snapshot、typed Gameplay facts、body observations与 EventId commands。Model Source与模型 Pass MUST通过正式 Source ports、Pipeline products、ExecutionPlan和 Egress disposition连接，MUST不让 Kernel引用 packet、history、policy、server tick或 correction DTO。

#### Scenario: Model 接入角色模拟

- **WHEN** 后续完整 Model绑定 Actor roster
- **THEN** MUST通过 Ingress products、Schedule plan、Snapshot restore和 Egress products接入
- **AND** MUST不恢复 Character NetworkSend/ReceiveStage或私有 replay runner

### Requirement: Character 输入来源与运动权威必须正交

Actor control input MUST由当前 Source与 Ingress/Schedule Pass产生；world constraint与 body result MUST由 Session装配的唯一 WorldSolver和正式 WorldSolve Pass产生。Program与 Character state MUST不使用 authority总控枚举或具体 Network Model分支。后续模型 MAY为不同 Actor提供不同 input/ingress，但同一 SimulationStep的 world mutation仍必须经过统一 batch Solver。

#### Scenario: Local Session Owner

- **WHEN** Local Session创建 Corin
- **THEN** Local Source与 Local Input Pass MUST提供设备 input
- **AND** Unity WorldSolver与 WorldSolve Pass MUST提供 body result

#### Scenario: 后续模型拥有远端 Actor

- **WHEN** Network Model为远端 Actor提供 canonical input或 typed ingress
- **THEN** MUST通过正式 Source port和 Pipeline product接入
- **AND** MUST不把 Actor伪装成 CharacterPipeline RemoteProxy模式

## ADDED Requirements

### Requirement: Network Model Pipeline 必须显式可见且可独立替换

一个 Network Model模块 MAY交付多个合法 Pipeline Definition用于不同实验，但每个 Session Composition MUST显式选择一个完整 Pipeline。Model Source MUST声明 required Pass/Product/Backend/Solver capability，Pipeline Compiler MUST验证匹配。模型切换或 Pipeline切换 MUST销毁并重建 Session；不得在 Active状态按 packet、Actor或 correction结果热插拔 Pass。

#### Scenario: 对比 Prediction 与 Rollback Pipeline

- **WHEN** 用户分别创建 ServerAuthoritative Prediction Composition与 Deterministic Rollback Composition
- **THEN** 两者 MUST显示不同 Source、PipelineId/Hash、Backend与 Solver组合
- **AND** 两者 MUST复用同一 Common Session Host合同而不覆盖对方代码


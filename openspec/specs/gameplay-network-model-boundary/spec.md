# gameplay-network-model-boundary Specification

## Purpose
定义唯一 SimulationSessionHost、GameplayNetworkModelDefinition、model-owned Session Source/Pipeline、Actor roster与具体 protocol/history/endpoint实现之间的插件边界。
## Requirements
### Requirement: Gameplay Network Model 必须是 Session 级唯一装配

Gameplay Network Model MUST作为`SimulationSessionSourceDefinition`的一种实现，通过实际runtime factory创建model session、Endpoint、history、显式Source ports与匹配Target ABI的Runtime Launcher。唯一`SimulationSessionHost` MUST使用同一Composition Definition将该Source与显式Program Runtime、Execution Backend、Pipeline Definition、WorldSolver、ProgramCatalog、roster、Committer和diagnostics组合。Common Host、target-specific Unity Composer与通用Pipeline runtime package builder MUST不硬编码已知Model、Prepared Source或Pipeline Definition具体类型。Character、Graph、Program、Kernel、Pipeline Backend和WorldSolver MUST不保存Model selection。Local Source MUST可独立使用同一Session Host，但 MUST不被声明为Network Model。

#### Scenario: 新增完整Float32 Network Model

- **WHEN** 新模型提供Source preparation、Endpoint、Pipeline Runtime Package、Pass factories与Runtime Launcher
- **THEN** MUST可通过现有五项Composition和公共Unity Float32 request lowering进入唯一portable Composer
- **AND** MUST不修改公共Session Host、Unity Float32 Composer或通用Package Builder

#### Scenario: 当前核心运行Local Session

- **WHEN** 已安装Network Model都没有完整Source factory、Runtime Launcher与合法Pipeline Runtime Package
- **THEN** Local Session MUST通过显式Local Source、Standard Launcher和Standard Local Pipeline正常创建
- **AND** Host MUST不创建GameplayNetworkModelSession或把Local当作fallback Model

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

Actor control input MUST由当前Source与Ingress/Schedule Pass产生；world constraint与body result MUST由Session装配的唯一WorldSolver和正式WorldSolve Pass产生。Program与Character state MUST不使用authority总控枚举或具体Network Model分支。Network Model Schedule MAY把权威观察到的非Program Actor轨迹编译为model-neutral、tick-bound World constraint，但 MUST不自行求解接触、不提交Body，也 MUST不让Packet、Endpoint或Presentation Transform进入Solver。后续模型 MAY为不同Program Actor提供不同input/ingress，但同一SimulationStep的world mutation仍必须经过统一batch Solver。

#### Scenario: Local Session Owner

- **WHEN** Local Session创建Corin且没有外部观察Actor
- **THEN** Local Source与Local Input Pass MUST提供设备input
- **AND** Step MUST携带正式空观察frame
- **AND** Unity WorldSolver与WorldSolve Pass MUST提供body result

#### Scenario: ServerAuthoritative Prediction观察远端Actor

- **WHEN** Model Source拥有远端Actor的权威Body timeline但没有其canonical input
- **THEN** 声明观察接触能力的Schedule MAY产生ObservedKinematic World constraint
- **AND** 唯一WorldSolver MUST只为本地Program actor提交FinalBody
- **AND** MUST不把远端Actor伪装成CharacterPipeline RemoteProxy或第二Program actor

#### Scenario: Model拥有远端canonical input

- **WHEN** 另一个Network Model为远端Actor提供正式canonical input与typed ingress
- **THEN** 该Actor MAY通过完整roster进入Program执行
- **AND** MUST不与ObservedKinematic约束使用同一ActorId双重注册

### Requirement: 观察World约束必须是Step级正式输入

Float32 Simulation Step MUST显式携带按tick绑定、按ActorId稳定排序的`ObservedWorldConstraintFrame`。该frame MUST进入World request canonical bytes与RequestHash，并 MUST验证与active roster不重复。每个observed参与者 MUST携带Solver锁定接触形状的configuration hash；具体形状数据 MUST继续由WorldSolver configuration拥有。空frame MUST是带tick的正式值；系统 MUST不使用`null`、隐藏Source状态、MonoBehaviour集合或Presentation缓存表示约束缺失。

#### Scenario: Pipeline构造无观察Actor的Authority step

- **WHEN** Authority完整roster已经由active Character requests表达
- **THEN** Schedule MUST显式提供空观察frame
- **AND** WorldSolve Pass MUST不从Network Model类型猜测约束

#### Scenario: Pipeline构造带观察Actor的Prediction step

- **WHEN** Schedule为Actor B选择了合法远端Body轨迹且Composition声明观察接触能力
- **THEN** 观察frame MUST随该step进入唯一World batch和request hash
- **AND** Replay MUST能从History恢复同一frame

### Requirement: BTSMTL Authoring 不得拥有 Network Model 配置

Graph、StateMachine、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 CharacterSimulationProgram MUST不保存 ModelId、Endpoint、Transport、history、correction、rollback 或 WorldSolver implementation selection。Program MAY只声明 model-neutral required capabilities。

#### Scenario: 复用同一 Program

- **WHEN** 同一 Program被 Local Source与后续 Network Model Source使用
- **THEN** BTSMTL authoring MUST保持不变

### Requirement: Network Model Pipeline 必须显式可见且可独立替换

一个 Network Model模块 MAY交付多个合法 Pipeline Definition用于不同实验，但每个 Session Composition MUST显式选择一个完整 Pipeline。Model Source MUST声明 required Pass/Product/Backend/Solver capability，Pipeline Compiler MUST验证匹配。模型切换或 Pipeline切换 MUST销毁并重建 Session；不得在 Active状态按 packet、Actor或 correction结果热插拔 Pass。

#### Scenario: 对比 Prediction 与 Rollback Pipeline

- **WHEN** 用户分别创建 ServerAuthoritative Prediction Composition与 Deterministic Rollback Composition
- **THEN** 两者 MUST显示不同 Source、PipelineId/Hash、Backend与 Solver组合
- **AND** 两者 MUST复用同一 Common Session Host合同而不覆盖对方代码

### Requirement: Network Model插件边界必须具有物理程序集所有权

model-neutral Network Model Definition与具体Model Unity实现 MUST位于不同程序集。具体Model程序集 MAY引用公共Simulation、model-neutral Definition、自己的portable Model与Transport程序集以及所需Host adapter，但公共Simulation、model-neutral Definition、Program、Kernel和WorldSolver合同程序集 MUST不反向引用具体Model程序集。

#### Scenario: 增加第二个Unity Network Model

- **WHEN** 第二个Network Model提供自己的Endpoint、Source、Pipeline与Runtime Launcher
- **THEN** 它 MUST以独立模型程序集接入现有公共Composition
- **AND** MUST不修改或重新编译公共程序集源码来登记模型类型

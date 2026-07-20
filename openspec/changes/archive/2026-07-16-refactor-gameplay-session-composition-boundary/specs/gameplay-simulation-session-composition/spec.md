## ADDED Requirements

### Requirement: SimulationSessionHost 必须是 Unity Session Composition 的唯一 owner

Unity gameplay场景 MUST以唯一 `SimulationSessionHost` 持有 Session preparation、Composition Definition、compiled Pipeline plan、runtime handle、Actor launch roster、GameplayTickSystem logic registration与销毁顺序。单个 `CharacterPipelineHost` MUST不创建 Session Source、WorldSolver、Program Runtime、Execution Backend、Pipeline Runtime或独立 Logic target。一个 Active Session无论包含多少 Actor，MUST只存在一个 runtime handle和一个正式 world owner。

#### Scenario: 两个 Actor 进入同一 Local Session

- **WHEN** SimulationSessionHost使用两个合法 Actor registration创建 Local Session
- **THEN** MUST只创建一个 Float32 Pass Pipeline runtime和一个 Unity WorldSolver
- **AND** 两个 CharacterPipelineHost MUST不各自创建 Session

### Requirement: Session Composition 必须显式选择五个组成部分

`SimulationSessionCompositionDefinition` MUST显式引用一个 Program Runtime Definition、一个 Execution Backend Definition、一个 Pipeline Definition、一个 Session Source Definition与一个 WorldSolver Definition。Host MUST不通过 enum、类型名、已安装实现扫描、第一个可用对象或默认值选择任何组成部分。Local Source与 Gameplay Network Model Source MAY复用相同 composition contract，但 Local MUST不被声明为 Network Model。

#### Scenario: Local Float32 组合

- **WHEN** 作者配置 Float32 Program Runtime、Float32 Pass Backend、Standard Local Pipeline、Local Session Source与 Unity CharacterController Solver
- **THEN** Host MUST只按五个显式引用创建组合
- **AND** 缺少任一引用时 MUST在创建 Runtime前失败

### Requirement: Program Runtime 与 Execution Backend 必须是独立选择维度

Program Runtime Definition MUST只拥有 NumericProfile、Target ABI、Program/State/Kernel/Snapshot codec与 Target services；Execution Backend Definition MUST只拥有 Pipeline descriptor编译、Pass runtime、working transaction与 outer runtime handle创建。Target-specific Composer MUST强类型校验二者兼容。同一 Program Runtime MAY与多个兼容 Backend组合，但 Common Host MUST不做 Float/Fixed转换、反射调用或 runtime backend switch。

#### Scenario: 同一 Float32 Runtime 选择不同 Backend

- **WHEN** 后续安装另一个明确支持 Float32 Program ABI的 Execution Backend
- **THEN** Composition MAY显式选择该 Backend与合法 Pipeline
- **AND** MUST不修改 CharacterPipelineHost或把 Backend类型写进 Program

### Requirement: Session Source 必须通过 Preparation 产生完整 Launch Plan

Session Source MUST创建 `ISimulationSessionPreparation`，并只返回 Pending、Ready或 Failed。只有 Ready preparation MAY产生一次不可变 `SimulationSessionLaunchPlan`；Launch Plan MUST包含 Session identity、TickRate、Program Runtime/Target ABI、Backend、compiled Pipeline plan/hash、ProgramCatalog、完整 Actor roster、Source ports、Solver、Snapshot codec、Committer、initial Character/World/Pipeline state与 diagnostics identity。Preparation MUST不把半成品 Runtime暴露给 Host，也 MUST不在失败时切换其它 Source、Pipeline、Backend或 Solver。

#### Scenario: Network Model 等待 roster

- **WHEN** 后续 Network Model endpoint已连接但 canonical roster尚未到齐
- **THEN** preparation MUST保持 Pending且不得创建 Pipeline Runtime
- **AND** roster完整后 MUST以一个锁定 Launch Plan进入 Ready

### Requirement: Session Host 必须使用 Numeric-Neutral Runtime Handle

公共 Session Host MUST只持有 `ISimulationSessionRuntimeHandle` 与 `SimulationSessionCompositionDescriptor`。Runtime handle MUST提供外层 LogicTick、状态/身份查询与 Dispose，不得暴露 Float32/Fixed Program、Character/World/Pipeline state、Source、Solver、Snapshot或可变 World数据。Host MUST不转换 Numeric Target数据，也 MUST不解释 Pipeline Pass。

#### Scenario: 后续 Fixed Rollback Session 接入 Host

- **WHEN** Fixed Program Runtime与 Deterministic Backend返回合法 runtime handle
- **THEN** 同一个 SimulationSessionHost lifecycle MUST能推进该 handle
- **AND** Host MUST不引用 Fixed scalar、Fixed state、Rollback Pass或 KCC具体类型

### Requirement: Target-specific Composer 必须唯一创建完整 Runtime

每个已安装 Program Runtime/Execution Backend组合 MUST通过唯一强类型 Composer集中校验并创建 ProgramCatalog、compiled Pipeline plan、roster、initial state、Source ports、Kernel services、WorldSolver、Snapshot codec、Committer与 diagnostics。当前 Float32 Pass Backend MUST只有一个位于 portable source set的正式 Composer入口；Unity adapter、Character Host、Network Model、Preview和 Demo MUST不复制 runtime构造、Pipeline compile、Launch Plan、identity或 capability校验。

#### Scenario: Local 与后续 ServerAuthoritative 共用 Float32 基座

- **WHEN** Local Pipeline与后续 Prediction Pipeline都选择 Float32 Program Runtime和 Float32 Pass Backend
- **THEN** 两者 MUST通过同一个 target-specific Composer创建 Runtime
- **AND** 差异 MUST存在于 Source和 Pipeline Pass，不得存在两份 Float32 Session构造器

#### Scenario: 普通 DotNet Authority Host 装配 Float32 Session

- **WHEN** 普通 .NET Host提供合法的Float32 Program Runtime、Source ports、Pipeline、Solver与输出端口
- **THEN** MUST调用与Unity相同的portable Float32 Composer
- **AND** MUST不复制Unity Composer、Pipeline compiler或Launch Plan构造逻辑

### Requirement: Actor Registration 必须在 Active 前形成不可变 roster

Character Actor Host MUST提供带显式 ActorId、Program artifact、Projection、World body binding、可选 local input、Presentation/output port与 diagnostics metadata的不可变 registration。Session preparation MUST在 Active前校验 ActorId唯一性、Program/Projection identity、ProgramCatalog binding、当前 Pipeline/Source/Solver所需端口与 initial state；Active后 MUST不增删 Actor、不换 Program或修改 binding。

#### Scenario: 重复 ActorId

- **WHEN** 两个 Actor registration使用相同 ActorId
- **THEN** composition MUST在创建 ProgramCatalog/World/Pipeline Runtime前失败
- **AND** MUST不按 GameObject name或 registration order自动改名

### Requirement: Session Host 必须按正式 Tick 生命周期推进 Preparation 与 Runtime

SimulationSessionHost MUST通过 GameplayTickSystem正式 Input/Logic target推进 Preparation与 Active Runtime，不得创建私有 Update、协程、Task loop或 Network Model专用 runner。Preparing状态 MUST不执行 Program Tick；Active状态每个 LocalLogicTick MUST只调用一次 runtime handle。该 handle MAY按 compiled ExecutionPlan执行零到多个内部 SimulationTick。Presentation target MAY按 Actor独立存在，但其注册和释放 MUST归当前 Active composition。

#### Scenario: 一个外层 Tick 触发多个 Replay Step

- **WHEN** Active runtime handle收到一个 LocalLogicTick并由 Schedule Pass生成三个内部 step
- **THEN** GameplayTickSystem MUST仍只调用一次 Session target
- **AND** 三个 step MUST由同一个 Pipeline transaction推进

### Requirement: Session Composition 必须锁定完整身份与真实 capability

Active descriptor MUST记录 SessionId、source clock、TickRate、Program Runtime/NumericProfile/Target ABI、ProgramCatalogHash、roster、BackendId/semantic version、PipelineId/Revision/Hash、SourceId、Solver identity/version/capabilities、Snapshot codec、Committer与可选 Model/Endpoint identity。Composer MUST在首 Tick前校验所有 identity与 Program/Pass capability union；显示名、Inspector状态或 capability位 MUST不能代替实际对象和 factory校验。

#### Scenario: Pipeline 要求 Solver 未支持能力

- **WHEN** Program与 Pass capability union包含当前 Solver未声明的能力
- **THEN** Composer MUST拒绝创建 runtime handle
- **AND** Host MUST不改用另一个 Solver、删除 Pass或忽略 capability

### Requirement: Composition 失败必须 fail-closed 并按 owner 释放资源

Preparation、Pipeline compile、Composer或 Active Runtime任一阶段失败时，Session Host MUST进入 Failed，按明确 owner释放 Runtime/Pass、Source/Endpoint、Solver与 registration lifecycle，并停止后续 LogicTick。Actor activation与Host cleanup MUST是异常安全的：一个资源释放失败不得阻止其余已取得资源释放，且不得覆盖最初的Session Failure。系统 MUST不回退 Local、其它 Network Model、默认 Pipeline、默认 Solver、Transform直写或旧 Character Session。

#### Scenario: Pipeline factory 缺失

- **WHEN** Pipeline引用的一个 Pass factory未安装或 version不匹配
- **THEN** Host MUST保持 Failed并释放 preparation已创建资源
- **AND** MUST不跳过该 Pass或创建 Standard Local Pipeline继续运行

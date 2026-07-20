## MODIFIED Requirements

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid权威端 MUST通过 Model-owned Session Source与显式 authoritative Pipeline向同一 SimulationProgramCatalog提供 canonical input、accepted action lifecycle与 typed ingress，并由所选 Program Runtime、Execution Backend、标准 Step Pass和唯一 `ICharacterWorldSolver`独立产生 canonical body state。模型 MAY选择 Unity authoritative Host/Solver或纯 CSharp Host/Solver，但 MUST不复制 Gameplay Kernel、Common Pipeline Compiler、Session Host或 Commit事务，不得直接调用旧 Motion Executor、同时运行两个 Solver后选结果，或累加客户端 resolved displacement作为 fallback。Egress OutputDisposition MUST只控制外部复制/表现事件，不得接受或拒绝权威 Gameplay state。

#### Scenario: 使用 Unity authoritative process

- **WHEN** 后续 Model Composition选择 Unity authoritative Host/Solver
- **THEN** 服务端 MUST在 Unity process内执行共享 Program Runtime与所选 authoritative Pipeline
- **AND** MUST通过唯一 Unity WorldSolve Pass解析 world constraint

#### Scenario: 使用纯 CSharp KCC server

- **WHEN** 后续 Model Composition选择纯 CSharp KCC Host/Solver
- **THEN** 服务端 MUST在普通 .NET runtime内执行相同 Program与 Pipeline descriptor
- **AND** MUST通过唯一 KCC WorldSolve Pass产生 canonical body result
- **AND** navigation/pathfinding library MUST不被当作完整碰撞 motor

#### Scenario: backend 缺失或能力不足

- **WHEN** ServerAuthoritativeHybrid要求的 Source、Pipeline Pass、Program Runtime、Backend、WorldSolver或 capability不完整
- **THEN** Model session启动 MUST失败
- **AND** MUST不回退 envelope validation、client pose acceptance、旧 Motion Executor或 LocalLoopback Character bridge

## REMOVED Requirements

### Requirement: ServerAuthoritative 模型缺少正式 Simulation Driver 时必须不可用

**Reason**: 旧 Simulation Driver合同删除，完整性现在取决于 Session Source、Prediction/Correction Pipeline、Pass factory、Backend、Solver与 Endpoint。

**Migration**: 缺少任一正式组成部分时 ModelDefinition必须 unavailable，不得回退旧 Adapter或 Local Pipeline。

## ADDED Requirements

### Requirement: ServerAuthoritative 模型缺少正式 Source 或 Pipeline 时必须不可用

ServerAuthoritativeHybrid ModelDefinition MUST只有在 Model-owned Session Source、Prediction/Correction Pipeline及全部 Pass factory、Actor binding、required Program Runtime/Execution Backend、WorldSolver/Host capability与 Endpoint capability全部可创建时才可被 SimulationSessionHost选择。仅保留 packet、session、profile、Source壳或 LocalLoopback endpoint MUST不构成完整模型。

#### Scenario: 核心迁移后旧 Adapter 已删除

- **WHEN** CharacterServerAuthoritativeBinding/Adapter已随旧 NetworkStage删除
- **AND** 新 ServerAuthoritative Source或 Prediction Pipeline尚未由后续 change实现
- **THEN** ModelDefinition MUST报告 unavailable
- **AND** MUST不创建旧 LocalLoopback Character闭环或 Local Pipeline fallback


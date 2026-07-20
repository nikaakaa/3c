# server-authoritative-hybrid-sync-model Specification

## ADDED Requirements

### Requirement: ServerAuthoritative 模型缺少正式 Simulation Driver 时必须不可用

ServerAuthoritativeHybrid ModelDefinition MUST只有在 model-owned Simulation Driver、actor binding、required WorldSolver/Host capability 和 Endpoint capability 全部可创建时才可被 SessionHost 选择。仅保留 packet、session、profile 或 LocalLoopback endpoint MUST不构成完整模型。

#### Scenario: 核心迁移后旧 Adapter 已删除

- **WHEN** CharacterServerAuthoritativeBinding/Adapter 已随旧 NetworkStage 删除
- **AND** 新 ServerAuthoritative Driver 尚未由后续 change 实现
- **THEN** ModelDefinition MUST报告 unavailable
- **AND** MUST不创建旧 LocalLoopback Character 闭环

## MODIFIED Requirements

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid 权威端 MUST 通过 model-owned Simulation Driver 向同一 `SimulationProgramCatalog` 和 `SimulationSessionRuntime` 提供 canonical input、accepted action lifecycle、typed ingress、restore 与 SimulationOutputPlan，并由 Session 装配的唯一 `ICharacterWorldSolver` 解析 canonical body state。模型 MAY 选择 Unity authoritative process 或纯 CSharp WorldSolver，但 MUST 不复制 Gameplay Kernel、直接调用旧 Motion Executor、同时运行两个 Solver 后选结果，或累加客户端 resolved displacement 作为 fallback。OutputPlan MUST只控制外部复制/表现事件，不得接受或拒绝权威 Gameplay state。

#### Scenario: 使用 Unity authoritative process

- **WHEN** 后续 model definition 选择 Unity authoritative backend
- **THEN** 服务端 MUST 在 Unity process 内执行共享 Program 与 SessionRuntime
- **AND** MUST 通过唯一 Unity WorldSolver ResolveBatch 解析 world constraint

#### Scenario: 使用纯 CSharp KCC server

- **WHEN** 后续 model definition 选择纯 CSharp KCC backend
- **THEN** 服务端 MUST 在普通 DotNet runtime 内执行相同 Program 与 SessionRuntime
- **AND** MUST 通过唯一 KCC WorldSolver 产生 canonical body result
- **AND** navigation/pathfinding library MUST 不被当作完整碰撞 motor

#### Scenario: backend 缺失或能力不足

- **WHEN** ServerAuthoritativeHybrid 要求的 Driver、Program、WorldSolver 或 capability 不完整
- **THEN** model session 启动 MUST 失败
- **AND** MUST 不回退到 envelope validation、client pose acceptance、旧 Motion Executor 或 LocalLoopback Character bridge

## REMOVED Requirements

### Requirement: ServerAuthoritative Adapter 必须是唯一 Packet 映射入口

**Reason**：当前 Adapter 直接依赖 CharacterNetworkSendStage、CharacterNetworkReceiveStage、ExternalPose 和 MotionStage correction。网络模型的预测、恢复、remote actor 和 commit 语义尚未重新确认，不能把旧行为机械迁移成所谓最终 Adapter。

**Migration**：删除旧 CharacterServerAuthoritativeBinding/Adapter。后续 ServerAuthoritative change 必须基于 Simulation Driver tick plan、restore request、Tick result observation 和 SimulationOutputPlan 重新增加模型专属 adapter。

#### Scenario: 删除旧 Adapter

- **WHEN** Character Core 完成 Program/SessionRuntime 迁移
- **THEN** 旧 Adapter 与 binding MUST删除
- **AND** MUST不保留 NetworkStage wrapper 或 ExternalPose bridge

# server-authoritative-hybrid-sync-model Specification

## MODIFIED Requirements

### Requirement: 当前混合同步语义必须归属 ServerAuthoritativeHybrid

系统 MUST将 Owner prediction、服务端 Action/Effect/Body authority、accepted input history、reconciliation、Remote snapshot interpolation、Action replication 和局部 combat history 归属 `ServerAuthoritativeHybrid`。该模型 MUST使用 compiled SimulationProgram/Kernel 执行 gameplay，但 MUST不声称其 Unity 或 DotRecast solver 具有 deterministic rollback 语义，也 MUST不复用 `DeterministicRollback` input bundle/history/hash。

#### Scenario: Unity 与 DotRecast 后端

- **WHEN** 两个 Demo 分别运行 Unity server 与 DotRecast .NET server
- **THEN** 两者 MUST共享同一 ServerAuthoritativeHybrid model/protocol语义
- **AND** backend 差异 MUST只存在于 server host/solver composition

### Requirement: ServerAuthoritative Adapter 必须是唯一 Packet 映射入口

ServerAuthoritative model adapter MUST是 portable simulation input/facts 与该模型 packet 的唯一映射入口。Adapter MUST从 ActorId、SimulationTick、sequence、typed input 和 Action/Effect facts构造 command；predicted body result MAY作为 comparison metadata，但 MUST不作为 canonical displacement。Incoming authoritative state/action/effect MUST先进入 model Driver history/reconciliation，再以正式 state/sample/semantic transition进入 actor runtime。

#### Scenario: 构造 MotionCommand

- **WHEN** Owner Driver 完成当前预测 Tick
- **THEN** Adapter MUST从该 Tick portable input 构造 command
- **AND** MUST不从 Presentation pose 或 Animancer state构造 command

### Requirement: ServerAuthoritative Session 必须是 Session-level ownership

一个客户端 Session MUST只有一个 ServerAuthoritative model runtime、Simulation Driver 和 endpoint。Character binding MUST只保存 SessionHost、SubjectActorId、compiled Program/actor port、presentation commit sink 和模型 policy profile；不得各自创建 Driver、peer、history、solver 或 backend。

#### Scenario: 两个角色共享 Session

- **WHEN** Owner 与 Remote Corin 绑定同一 Session
- **THEN** 两者 MUST归属同一 Driver 的 actor registry
- **AND** history、server clock、endpoint 和 roster MUST保持 Session 级唯一

### Requirement: ServerAuthoritative 权威运动必须拥有独立模拟后端

ServerAuthoritativeHybrid 的权威端 MUST加载 canonical CharacterSimulationProgram，从 accepted input、Action/Effect state、角色配置和当前 SimulationState body 生成 motion request，并调用 server launch manifest 选定的唯一 World Solver。正式 Demo MAY选择 Unity authoritative process + Unity CharacterController solver，或 pure .NET host + DotRecast navigation-surface solver；两者 MUST不在同一 Session 双算、投票或故障回退。DotRecast MUST不被描述为完整 KCC。

#### Scenario: Unity authoritative process

- **WHEN** launch manifest 选择 Unity host/solver
- **THEN** server MUST独立执行 Program 和 Unity solver
- **AND** MUST发送 authoritative state

#### Scenario: DotRecast authoritative host

- **WHEN** launch manifest 选择 .NET/DotRecast
- **THEN** server MUST独立执行同一 Program 和 navigation-surface solver
- **AND** MUST明确暴露 solver capability限制

#### Scenario: backend 缺失

- **WHEN** server manifest 没有完整且兼容的 World Solver
- **THEN** server Session MUST启动失败
- **AND** MUST不累加客户端 resolved displacement、启动 LocalLoopback 或选择其它 solver

### Requirement: ServerAuthoritative 模型必须唯一拥有 Packet 与 History

ServerAuthoritativeHybrid MUST唯一拥有该模型的 command、snapshot、action/effect decision、ack、prediction history 和 reconciliation。History MUST按 ActorId、SimulationTick 与 sequence 保存 accepted input、predicted state 和 authoritative state；旧 MotionStage correction queue/history MUST删除。模型 MAY进行 bounded restore/replay 以对账 Owner prediction，但 MUST不把该能力宣传为全 Session deterministic rollback。

#### Scenario: Owner 状态发生偏差

- **WHEN** authoritative state 与本地预测不同
- **THEN** model Driver MUST使用自己的 history/reconciliation 收口
- **AND** Character Motion operation MUST不读取 correction packet


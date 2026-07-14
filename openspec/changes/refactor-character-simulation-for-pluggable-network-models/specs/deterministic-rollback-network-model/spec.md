# deterministic-rollback-network-model Specification

## ADDED Requirements

### Requirement: DeterministicRollback 必须是独立完整 Network Model

系统 MUST提供独立 `DeterministicRollback` model definition、session、actor binding、protocol、history、restore/replay、state hash、snapshot recovery 和 presentation commit。该模型 MUST不复用 ServerAuthoritative correction/snapshot packet 假装 rollback，也 MUST不把 rollback 配置放入 Character、Graph、Timeline、Action 或 Blackboard。

#### Scenario: 作者选择 Rollback 模型

- **WHEN** DeterministicRollback runtime、KCC、协议和配置全部安装完成
- **THEN** GameplayNetworkSessionHost MAY创建该 model session
- **AND** Session MUST要求 Deterministic、Snapshotable Program 与 solver

#### Scenario: 模型实现不完整

- **WHEN** KCC、history、replay、commit 或 recovery 任一能力缺失
- **THEN** UI MUST不把该 definition 作为可运行模型
- **AND** MUST不创建空 runtime 或 fallback 到 ServerAuthoritativeHybrid

### Requirement: Rollback 模型必须使用 canonical input bundle 推进世界

服务端 MUST按 SimulationTick、ActorId 和 sequence 验证并排序输入，生成 canonical input bundle。客户端和服务端 MUST使用同一 Program、TickRate、actor catalog 和 deterministic world data 执行 bundle；packet 到达顺序不得成为模拟顺序。

#### Scenario: 两个客户端输入到达顺序不同

- **WHEN** 同一 Tick 的 ActorA/ActorB 输入以不同网络顺序到达参与者
- **THEN** 所有参与者 MUST按 canonical bundle 顺序执行
- **AND** state hash MUST不因 arrival order 改变

### Requirement: Rollback 模型必须拥有有界 world snapshot 与 replay

Model session MUST保存有界 canonical input history 和 SimulationWorldSnapshot ring。迟到或修正输入在 history 范围内时，Driver MUST选择最早受影响 Tick 前的 snapshot，restore 完整 world state，并按 canonical bundle 重演到当前预测 Tick。Graph、StateMachine、Timeline、Action、Effect 和 KCC MUST使用同一正式 Kernel/solver 重演。

#### Scenario: 收到迟到输入

- **WHEN** Tick 120 的 canonical input 在本地已经预测到 Tick 126 后到达
- **THEN** Driver MUST恢复 Tick 119 snapshot
- **AND** MUST重演 Tick 120 到 126
- **AND** MUST不调用 replay 专用 Graph 或节点

#### Scenario: 输入超出 history

- **WHEN** 所需 restore tick 已被有界 history 淘汰
- **THEN** Client MUST请求正式权威 world snapshot recovery
- **AND** MUST不使用当前 pose 加误差或旧 MotionStage correction 继续模拟

### Requirement: Rollback 模型必须检测状态分歧并正式恢复

Model MUST按固定周期计算 canonical world state hash，并交换 ProgramHash、SimulationTick 和 state hash。ProgramHash 不一致 MUST拒绝加入；state hash 不一致 MUST定位 actor/world section，并在无法通过 history replay 收敛时使用服务端权威 snapshot 恢复。

#### Scenario: ProgramHash 不一致

- **WHEN** 客户端 ProgramHash 与 Session manifest 不一致
- **THEN** Join MUST失败并报告双方 hash
- **AND** MUST不下载、转换或兼容运行另一版本 Program

#### Scenario: replay 后仍 hash mismatch

- **WHEN** 客户端 restore/replay 后 state hash 仍与服务端不同
- **THEN** model MUST应用正式权威 snapshot recovery
- **AND** MUST记录 mismatch tick、section 和 recovery cause

### Requirement: Deterministic KCC 必须覆盖 Demo 声明的世界范围

Rollback model MUST使用 portable、deterministic、snapshotable 的 KCC World Solver，覆盖静态世界 collision、角色 capsule、ground、slide、项目声明的 step/slope 和 stable actor order。它 MUST不调用 Unity Physics、CharacterController 或 DotRecast float result作为 canonical deterministic state。

#### Scenario: 闪避 motion curve 参与碰撞

- **WHEN** Dodge Timeline 在当前 Tick 提交 deterministic motion request
- **THEN** KCC MUST在同一 deterministic world state 中执行该 request
- **AND** resulting body state MUST进入 snapshot 与 state hash

### Requirement: Rollback 重演不得重复提交表现和网络副作用

Replay pass MUST只重建 SimulationState、facts 和 command ledger。网络发送、一次性 Cue、音效、VFX、UI 和外部回调 MUST由 Driver/Committer 按 EventId 与 confirmed tick 收口；Diagnostics MAY记录 Replay pass，但 MUST不改变 state hash。

#### Scenario: Attack1 启动 Tick 被重演三次

- **WHEN** 同一 Attack1 start EventId 在多次 replay 中产生
- **THEN** 客户端 MUST最多保留一个对应表现事件
- **AND** 服务端 MUST不因 replay 重复广播三次 Action activation


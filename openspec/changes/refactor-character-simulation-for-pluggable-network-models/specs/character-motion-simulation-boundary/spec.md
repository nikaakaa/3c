# character-motion-simulation-boundary Specification

## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

系统 MUST将角色运动拆分为 portable gameplay MotionRequest、model-selected World Solver 和 SimulationState body。SimulationKernel MUST解析 MotionContribution/Modifier 为最终 request；World Solver MUST只执行具体世界约束并返回 portable result；SimulationState body MUST是 canonical gameplay pose。Graph、Timeline、Action、Network packet、Presentation 和 Unity Transform MUST不直接调用 solver 或拥有第二份逻辑位姿。

#### Scenario: Local Driver 执行运动

- **WHEN** Kernel 完成当前 Tick motion resolve
- **THEN** Driver MUST调用唯一正式 World Solver
- **AND** solver result MUST写回 SimulationState body
- **AND** Presentation MUST只镜像该结果

### Requirement: Motion Executor 合同不得依赖 Unity 或业务作者结构

正式 `ICharacterWorldSolver` 合同 MUST使用项目自有 portable body、request、result 和 capability。Unity CharacterController、DotRecast 和 Deterministic KCC MUST作为独立 implementation；合同 MUST不暴露 Graph、Timeline、Action、Animancer、Network packet、transport 或 concrete Unity collision type。Deterministic model MUST使用独立 deterministic 数值与 solver state，不被迫经过 float-only adapter。

#### Scenario: Unity solver

- **WHEN** Unity adapter 调用 CharacterController.Move
- **THEN** concrete Unity API MUST只存在于 adapter
- **AND** result MUST量化回 portable body

#### Scenario: Deterministic solver

- **WHEN** Rollback model 执行 KCC
- **THEN** request、world query、result 和 snapshot MUST保持 deterministic 数值
- **AND** MUST不转换为 Unity float 后再作为 canonical result

### Requirement: Logic Pose Port 必须唯一拥有逻辑位姿读写

SimulationState body MUST成为唯一 canonical 逻辑位姿。Unity scene adapter MAY把 committed/predicted body sample 应用到 logic proxy 或 visual root，但 MUST不反向成为 simulation truth。权威 snapshot recovery MUST通过 Driver restore SimulationWorldState；MUST不通过 Logic Pose Port 或 Transform 单独重定位 actor。

#### Scenario: 权威 snapshot recovery

- **WHEN** Driver 应用完整 world snapshot
- **THEN** actor body、Graph、Timeline、Action 和 Effect state MUST一起恢复
- **AND** MUST不只写 Transform 造成状态分裂

### Requirement: 权威服务端必须独立生成并执行 canonical motion

ServerAuthoritative 服务端 MUST加载同一 CharacterSimulationProgram，从 accepted portable input、Action/Effect state 和 canonical body 独立生成 motion request，并调用配置的唯一 World Solver。Unity authoritative 与 DotRecast authoritative MUST复用同一模型语义和 Program；客户端 resolved motion MUST不成为 canonical displacement。

#### Scenario: DotRecast 权威服务端

- **WHEN** server launch manifest 选择 DotRecast navigation solver
- **THEN** .NET host MUST执行同一 Program
- **AND** solver MUST只声明静态 NavigationSurface 能力
- **AND** MUST不被描述为完整 KCC

### Requirement: 确定性模拟必须属于独立完整 Network Model

Deterministic KCC、canonical input、world snapshot、history、restore/replay、state hash 和 side-effect commit MUST共同属于独立 `DeterministicRollback` Network Model。该模型 MAY复用同一 gameplay Program/Kernel，但 MUST使用 deterministic Program capability 与 World Solver；ServerAuthoritative correction/snapshot packet MUST不作为 rollback 实现。

#### Scenario: Rollback 模型完整安装

- **WHEN** runtime、KCC、protocol、history、replay、commit 和配置全部存在
- **THEN** model definition MAY进入 Session authoring UI
- **AND** Character/Graph MUST不增加 model switch

## ADDED Requirements

### Requirement: ServerAuthoritative correction 必须由模型 Driver 编排

ServerAuthoritative prediction reconciliation MUST由该模型 Driver 的 accepted input history、authoritative snapshot 和 commit policy拥有。Motion operation 与 World Solver MUST不读取 server tick、ack、correction packet 或 model policy。权威 hard recovery MUST恢复正式 simulation state，visual recovery MUST留在 Presentation。

#### Scenario: Owner 收到 authoritative state

- **WHEN** 服务端确认 Tick 与本地预测不同
- **THEN** ServerAuthoritative Driver MUST按其正式 history/reconciliation 规则处理
- **AND** Motion operation MUST不把误差作为额外 gameplay contribution

## REMOVED Requirements

### Requirement: CharacterMotionAuthority 必须决定所需运动端口

**Reason**：actor 是否本地求解、外部采样或参与 rollback 是 Session Driver 的 actor binding 策略，不应由 CharacterPipeline 内 enum 选择。

**Migration**：删除 LocalSolver/ExternalPose/None motion authority 和对应 Host 分支；Driver 显式装配 actor、World Solver 与 Presentation sample source。

#### Scenario: 迁移 LocalSolver

- **WHEN** 单机 Corin 使用 Unity CharacterController
- **THEN** Local Driver MUST显式装配 Unity World Solver
- **AND** MUST不通过 CharacterMotionAuthority enum 分支

### Requirement: Correction 必须由 MotionStage 编排并通过正式端口应用

**Reason**：correction 是 ServerAuthoritative 的 prediction/reconciliation 策略，放在公共 MotionStage 会让单机、rollback 和 World Solver 被迫理解该模型。

**Migration**：删除 MotionStage partial/full correction plan 与 acknowledgement provenance；ServerAuthoritative Driver 拥有 history、reconciliation 和 recovery，Presentation 只处理视觉过渡。

#### Scenario: 迁移 correction

- **WHEN** Owner 收到 authoritative state
- **THEN** 必须由 ServerAuthoritative Driver 处理
- **AND** MotionStage 不得保留 model correction 分支

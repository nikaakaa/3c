# character-motion-simulation-boundary Specification

## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST生成 portable contribution/request，SimulationKernel MUST按统一 resolve/modifier 顺序选出最终 request，ICharacterWorldSolver MUST返回唯一 body result，SimulationState BodyState MUST成为唯一逻辑位姿真值。Graph、Timeline、Action 和 Presentation MUST不直接写 Transform 或调用 concrete solver。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** compiled Timeline 在当前 Tick 产生 Action motion contribution
- **THEN** Kernel MUST生成 portable request 并调用当前唯一 World Solver
- **AND** BodyState MUST记录 solver actual result

### Requirement: Motion Executor 合同不得依赖 Unity 或业务作者结构

ICharacterWorldSolver contract MUST只使用 portable body/request/query/result/capability 数据。UnityCharacterControllerWorldSolver MAY在 adapter 内使用 Unity API，但 Program、Kernel 和合同 assembly MUST不引用 CharacterController、Transform 或 UnityEngine 数值类型。

#### Scenario: Unity Solver

- **WHEN** Local Driver 调用 UnityCharacterControllerWorldSolver
- **THEN** CharacterController.Move MUST只出现在该 adapter 内

### Requirement: Logic Pose Port 必须唯一拥有逻辑位姿读写

SimulationState BodyState MUST替代分散 Logic Pose Port/Transform 作为 core 逻辑位姿。Unity Host MUST只在 Solver/Presentation adapter 边界将 BodyState 与 Unity 场景对象对齐，MUST不建立第二份逻辑真值。

#### Scenario: 应用 Solver Result

- **WHEN** Solver 返回 actual body result
- **THEN** Kernel MUST更新 SimulationState BodyState
- **AND** Presentation MUST只从 committed/predicted body sample 驱动 visual root

### Requirement: Unity CharacterController 必须只存在于正式 adapter 内

Unity CharacterController 引用与 Move 调用 MUST只存在 UnityCharacterControllerWorldSolver/Host binding 内。Graph、Timeline、Kernel、Network Model、Committer 和 Diagnostics MUST不直接调用它。

#### Scenario: 搜索 CharacterController.Move

- **WHEN** 迁移完成后检查 Character gameplay 主线
- **THEN** concrete Move 调用 MUST只存在正式 Unity Solver adapter

## ADDED Requirements

### Requirement: Model Correction 必须由具体 Driver 拥有

Prediction reconciliation、authoritative restore、ack 和 visual correction policy MUST归需要它们的具体 Network Model Driver。SimulationKernel、motion operation 和 World Solver MUST不读取 server tick、ack、correction packet 或 model policy。

#### Scenario: ServerAuthoritative 发现预测差异

- **WHEN** 现有 ServerAuthoritative Driver 发现 authoritative state 不同
- **THEN** MUST在 model-owned history/reconciliation 中处理
- **AND** MUST不向 motion resolver 注入 model correction contribution

## REMOVED Requirements

### Requirement: CharacterMotionAuthority 必须决定所需运动端口

**Reason**：Actor 的模拟/采样策略属于 Session Driver binding，不应由 CharacterPipeline enum 分支公共 motion。

**Migration**：删除 LocalSolver/ExternalPose/None 总控，Driver 显式绑定 Program Actor、Solver 或 remote presentation sample source。

#### Scenario: 迁移 LocalSolver

- **WHEN** Sandbox 使用 Unity CharacterController
- **THEN** Local Driver MUST显式绑定 Unity Solver

### Requirement: Correction 必须由 MotionStage 编排并通过正式端口应用

**Reason**：Correction 是具体 ServerAuthoritative 预测策略，放在公共 MotionStage 会泄漏 model history/ack 并阻塞 Local/Rollback Driver。

**Migration**：删除 MotionStage partial/full correction plan，将 reconciliation/recovery 迁到 ServerAuthoritative Driver，visual recovery 留在 Presentation。

#### Scenario: 迁移 Correction

- **WHEN** Owner 收到 authoritative observation
- **THEN** MUST由具体 Driver 处理
- **AND** MotionStage MUST不保留 model correction 分支

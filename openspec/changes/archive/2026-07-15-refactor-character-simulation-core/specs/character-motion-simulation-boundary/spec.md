# character-motion-simulation-boundary Specification

## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST在 Evaluate 阶段产生 portable contribution 与 WorldRequest；SimulationSessionRuntime MUST汇总当前 Session 全部 Actor request 并调用一次 ICharacterWorldSolver.ResolveBatch；Finalize MUST使用精确匹配的 WorldSolverResult 更新 Character/World state 并产生唯一 MotionResult。Graph、Timeline、Action、Driver 和 Presentation MUST不直接写 Transform 或调用 concrete solver。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** compiled Timeline 在当前 Tick 产生 Action motion contribution
- **THEN** Evaluate MUST生成 portable world request
- **AND** request MUST与同 Tick 其它 Actor request 一起进入唯一 ResolveBatch
- **AND** Finalize MUST记录 Solver actual result

### Requirement: Motion Executor 合同不得依赖 Unity 或业务作者结构

ICharacterWorldSolver contract shape MUST只使用当前 NumericProfile 的 portable world state、batch request/result、capability 和 Tick identity。UnityCharacterControllerWorldSolver MUST实现 Float32 target ABI并 MAY在 adapter 内使用 Unity API，但 Program、Kernel 和合同 assembly MUST不引用 CharacterController、Transform 或 UnityEngine 数值类型。

#### Scenario: Unity Solver

- **WHEN** Local Session 调用 UnityCharacterControllerWorldSolver
- **THEN** CharacterController.Move MUST只出现在该 adapter concrete implementation 内
- **AND** adapter MUST返回 portable batch result

### Requirement: Logic Pose Port 必须唯一拥有逻辑位姿读写

WorldSimulationState 中的 BodyState MUST替代 Logic Pose Port/Transform 作为 core 逻辑位姿真值。Unity Host MUST只在 WorldSolver binding 与 Presentation adapter 边界将 BodyState 对齐到场景对象，MUST不让 Transform、MotionStage 或 Presentation 保存第二份可反写逻辑真值。

#### Scenario: 应用 Solver Result

- **WHEN** Solver 返回 Actor 的 actual body result
- **THEN** SessionRuntime MUST在 Tick commit 时更新唯一 WorldSimulationState
- **AND** Presentation MUST只从 committed body sample 驱动 visual root

### Requirement: Unity CharacterController 必须只存在于正式 adapter 内

Unity CharacterController 引用、Move 调用和场景 body binding MUST只存在于 UnityCharacterControllerWorldSolver/Host adapter 内。Graph、Timeline、Kernel、Driver、Network Model、Committer 和 Diagnostics MUST不直接调用它。

#### Scenario: 搜索 CharacterController.Move

- **WHEN** 迁移完成后检查 Character gameplay 主线
- **THEN** concrete Move 调用 MUST只存在正式 Unity WorldSolver implementation

### Requirement: 确定性模拟必须属于独立完整 Network Model

Portable Core MUST统一 Gameplay Semantic IR operation、target compiler contract、batch world solve 形状和 snapshot ownership。确定性 Network Model MUST另外安装 Fixed Numeric Target 及其 Program/Kernel/CharacterState/WorldRequest/Snapshot ABI，拥有 canonical input history、world snapshot history、replay、hash exchange、OutputPlan policy，并装配 NumericProfile 匹配且声明 Snapshotable 与 DeterministicReplay 的正式 WorldSolver。模型 MUST不复制 authoring node 或业务 evaluator，也 MUST不把 Float32 Unity Solver、DotRecast navigation query 或记录 BodyResult 自动描述为完整确定性 KCC。

#### Scenario: 后续实现确定性模型

- **WHEN** 后续 change 只完成 rollback Driver 而没有符合能力的 deterministic WorldSolver
- **THEN** authoring UI MUST不显示完整可运行的 deterministic gameplay 组合
- **AND** Graph、Timeline 与 Action authoring MUST不增加另一套 deterministic node

#### Scenario: Rollback 试图复用 Float32 Program ABI

- **WHEN** 后续模型只有 Fixed WorldSolver，但仍加载本 change 的 Float32 Program/State
- **THEN** composition MUST拒绝创建
- **AND** MUST要求从同一 Semantic IR 生成正式 Fixed target artifact

## ADDED Requirements

### Requirement: World Solver 必须一次处理当前 Session 的 Actor batch

SimulationSessionRuntime MUST按 stable ActorId 顺序构造 WorldSolveBatchRequest，并在所有 Actor Evaluate 完成后调用一次当前唯一 WorldSolver。Solver MAY按自己的正式语义顺序处理角色或共同求解，但 MUST返回与 request set 精确一一对应的 result。

#### Scenario: 两个 Actor 同 Tick 请求移动

- **WHEN** ActorA 与 ActorB 在同一 SimulationTick 都产生 MotionRequest
- **THEN** 两个 request MUST进入同一个 batch
- **AND** MUST不由两个 Character runtime 各自独立调用 world mutation

### Requirement: Model Correction 必须由具体 Driver 拥有

Prediction reconciliation、authoritative restore、ack 和 visual recovery policy MUST归需要它们的具体 Network Model Driver。Kernel、motion operation 和 WorldSolver MUST不读取 server tick、ack、correction packet 或 model policy。Driver 只能申请完整 snapshot restore 或通过正式 model input 影响后续 Tick，MUST不向 motion contribution 注入公共 correction。

#### Scenario: ServerAuthoritative 发现预测差异

- **WHEN** 后续 ServerAuthoritative Driver 发现 authoritative observation 与预测状态不同
- **THEN** MUST在 model-owned history/reconciliation 中决定处理方式
- **AND** MUST不恢复 MotionStage correction branch

## REMOVED Requirements

### Requirement: CharacterMotionAuthority 必须决定所需运动端口

**Reason**：LocalSolver、ExternalPose 和 None 把 Driver、世界求解和远端表现混在 Character enum 中，无法表达 session batch 或独立模型策略。

**Migration**：删除 CharacterMotionAuthority。Local Session 显式装配 Local Driver 与 Unity WorldSolver；后续 remote/rollback 行为由完整 Model Driver 定义。

#### Scenario: 迁移 LocalSolver

- **WHEN** Sandbox 创建本地 Corin Session
- **THEN** MUST显式装配 Local Driver 与 Unity WorldSolver
- **AND** MUST不使用 authority enum

### Requirement: Correction 必须由 MotionStage 编排并通过正式端口应用

**Reason**：公共 MotionStage correction 泄漏 ServerAuthoritative history、ack 和 partial/full relocation 策略，并绕过 World snapshot 原子 restore。

**Migration**：删除 MotionStage correction plan、ExternalPoseCorrection 和 LogicPosePort 重定位；后续模型通过 Driver restore/input/ingress/OutputPlan 合同实现自己的 reconciliation。

#### Scenario: 删除旧 Correction

- **WHEN** 旧 ServerAuthoritative Character adapter 被移除
- **THEN** Character Core MUST不保留 correction application branch
- **AND** MUST不建立兼容 wrapper

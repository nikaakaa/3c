## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST在 Program Evaluate Pass中产生 portable contribution与 WorldRequest；正式 WorldSolve Pass MUST汇总当前 SimulationStep全部 Actor request并调用一次 `ICharacterWorldSolver.ResolveBatch`；Program Finalize Pass MUST使用精确匹配的 WorldSolverResult更新 Character/World working state并产生唯一 MotionResult。Graph、Timeline、Action、Session Source、其它 Pipeline Pass和 Presentation MUST不直接写 Transform或调用 concrete solver。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** compiled Timeline在当前 Step产生 Action motion contribution
- **THEN** Evaluate Pass MUST生成 portable world request
- **AND** request MUST与同 Step其它 Actor request一起进入唯一 ResolveBatch
- **AND** Finalize Pass MUST记录 Solver actual result

### Requirement: Logic Pose Port 必须唯一拥有逻辑位姿读写

WorldSimulationState中的 BodyState MUST替代 Logic Pose Port/Transform作为 Core逻辑位姿真值。Unity Host MUST只在 WorldSolver binding与 Presentation adapter边界将 committed BodyState对齐到场景对象，MUST不让 Transform、Character stage、Session Source、其它 Pipeline Pass或 Presentation保存第二份可反写逻辑真值。

#### Scenario: 应用 Solver Result

- **WHEN** WorldSolve Pass返回 Actor的 actual body result且 outer transaction成功
- **THEN** Execution Backend MUST在最终 Commit前更新唯一 WorldSimulationState
- **AND** Presentation MUST只从 committed body sample驱动 visual root

### Requirement: Unity CharacterController 必须只存在于正式 adapter 内

Unity CharacterController引用、Move调用和场景 body binding MUST只存在于 UnityCharacterControllerWorldSolver/Host adapter内。Graph、Timeline、Kernel、Session Source、Pipeline Pass、Execution Backend、Network Model、Committer和 Diagnostics MUST不直接调用它。

#### Scenario: 搜索 CharacterController.Move

- **WHEN** 迁移完成后检查 Character Gameplay主线
- **THEN** concrete Move调用 MUST只存在正式 Unity WorldSolver implementation

### Requirement: 确定性模拟必须属于独立完整 Network Model

Portable Core MUST统一 Gameplay Semantic IR operation、Target Compiler合同、Pipeline descriptor/product/ExecutionPlan、batch world solve形状和 snapshot ownership。确定性 Network Model MUST另外安装 Fixed Program Runtime、Deterministic Execution Backend、Rollback Source/Pass/Pipeline、Fixed Program/Kernel/CharacterState/WorldRequest/Snapshot ABI，以及 NumericProfile匹配且声明 Snapshotable与 DeterministicReplay的正式 WorldSolver。模型 MUST不复制 authoring node或业务 evaluator，也 MUST不把 Float32 Unity Solver、DotRecast navigation query或记录 BodyResult自动描述为完整确定性 KCC。

#### Scenario: 后续实现确定性模型

- **WHEN** 后续 change只完成 Rollback Source/Pass但没有 Fixed Backend与 deterministic WorldSolver
- **THEN** Composition Inspector MUST不显示完整可运行的 deterministic组合
- **AND** Graph、Timeline与 Action authoring MUST不增加另一套 deterministic node

### Requirement: World Solver 必须一次处理当前 Session 的 Actor batch

正式 WorldSolve Pass MUST按 stable ActorId顺序构造 WorldSolveBatchRequest，并在当前 SimulationStep全部 Actor Evaluate完成后调用一次当前唯一 WorldSolver。Solver MAY按自己的正式语义顺序处理角色或共同求解，但 MUST返回与 request set精确一一对应的 result。其它 Pass和 Character Host MUST不直接调用 world mutation。

#### Scenario: 两个 Actor 同 Tick 请求移动

- **WHEN** ActorA与 ActorB在同一 SimulationTick都产生 MotionRequest
- **THEN** 两个 request MUST进入同一个 batch
- **AND** MUST不由两个 Character runtime各自独立调用 world mutation

## REMOVED Requirements

### Requirement: Model Correction 必须由具体 Driver 拥有

**Reason**: 旧 Driver删除，correction所有权改由具体 Model Source及显式 Ingress/Schedule/Egress Pass承担。

**Migration**: correction必须产生正式 restore/replay ExecutionPlan，不能注入 motion contribution或直接修改 Transform。

## ADDED Requirements

### Requirement: Model Correction 必须由具体 Source 与 Pipeline Pass 拥有

Prediction reconciliation、authoritative restore、ack和 visual recovery policy MUST归需要它们的具体 Network Model Source及显式 Ingress/Schedule/Egress Pass。Kernel、motion operation、Common Host和 WorldSolver MUST不读取 server tick、ack、correction packet或 model policy。Schedule Pass只能通过完整 restore directive和 ordered SimulationStep影响 working state，MUST不向 motion contribution注入公共 correction。

#### Scenario: ServerAuthoritative 发现预测差异

- **WHEN** 后续 ServerAuthoritative Source/Pass发现 authoritative observation与预测状态不同
- **THEN** MUST在 model-owned history与 Schedule plan中决定 restore/replay sequence
- **AND** MUST不恢复 Character内部 correction branch或直接修改 Transform

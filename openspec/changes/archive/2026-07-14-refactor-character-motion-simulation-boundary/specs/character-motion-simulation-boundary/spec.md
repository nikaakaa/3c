## ADDED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

系统 MUST 将角色运动拆分为 gameplay `MotionIntent`、world-constrained Motion Executor 和 Logic Pose Port。`CharacterMotionStage` MUST 负责将 gameplay 来源解析为最终执行意图，并使用 executor result 生成 `MotionResult`；Motion Executor MUST 只负责在具体世界约束下执行运动；Logic Pose Port MUST 作为逻辑位姿读写入口。系统 MUST NOT 让 Graph、Timeline、Action 或 Network Model 直接调用具体运动组件。

#### Scenario: LocalSolver 执行一帧运动

- **WHEN** MotionStage 已完成 contribution 仲裁、modifier 和 correction plan
- **THEN** MotionStage MUST 将最终 `MotionIntent` 交给正式 Motion Executor
- **AND** executor MUST 返回实际执行结果
- **AND** MotionStage MUST 从该结果生成唯一 `MotionResult`

#### Scenario: Timeline 提交动作位移

- **WHEN** Timeline MotionCurve 提交 Action channel contribution
- **THEN** 该 contribution MUST 继续进入 MotionResolver
- **AND** Timeline MUST NOT 选择 executor、physics backend 或 logic pose implementation

### Requirement: Motion Executor 合同不得依赖 Unity 或业务作者结构

Motion Executor 的正式合同 MUST 使用项目自有的逻辑体状态、执行输入和执行结果。合同 MUST NOT 暴露 `CharacterController`、`Transform`、Unity collision type、Graph、Timeline、Action、Animancer、Network Model packet 或 transport。执行结果 MUST 至少表达 requested/actual displacement、最终 position/rotation、velocity、grounded 和可诊断碰撞摘要。

#### Scenario: Unity CharacterController 实现 executor

- **WHEN** 当前 Unity LocalSolver 执行运动
- **THEN** Unity adapter MAY 在实现内部调用 `CharacterController.Move`
- **AND** adapter MUST 将 Unity 结果转换为正式 Motion Execution Result
- **AND** CharacterMotionStage MUST NOT 读取 concrete `CharacterController`

#### Scenario: 后续纯 CSharp KCC 实现 executor

- **WHEN** 后续服务端模块提供纯 CSharp KCC
- **THEN** 该实现 MAY 消费同一业务执行输入并产生同一语义结果
- **AND** Character Graph、Timeline 和 Action authoring MUST 不因 backend 改变

### Requirement: Logic Pose Port 必须唯一拥有逻辑位姿读写

系统 MUST 使用正式 Logic Pose Port 读取当前逻辑体状态，并应用 ExternalPose 或显式 authority correction 重定位。Presentation MUST 只写 visual root，不得通过 Logic Pose Port 反写表现插值。系统 MUST NOT 从 Host transform、Animancer transform、场景搜索或默认组件猜测 logic root。

#### Scenario: ExternalPose 应用远端样本

- **WHEN** Character motion authority 为 ExternalPose 且收到合法 external pose sample
- **THEN** MotionStage MUST 通过 Logic Pose Port 应用样本
- **AND** MUST 不调用 Motion Executor
- **AND** MUST 不要求 `CharacterController`

#### Scenario: 缺少 Logic Pose Port

- **WHEN** 当前 authority mode 需要读取或写入逻辑位姿但未配置 Logic Pose Port
- **THEN** Host/Pipeline 初始化 MUST 失败并报告明确配置来源
- **AND** MUST 不使用 GameObject transform 作为 fallback

### Requirement: CharacterMotionAuthority 必须决定所需运动端口

`LocalSolver` MUST 要求 Logic Pose Port 和 Motion Executor；`ExternalPose` MUST 只要求 Logic Pose Port并禁止调用 LocalSolver executor；`None` MUST 不执行 gameplay motion。Host MUST 在 pipeline 创建前验证组合，运行时 MUST NOT 自动创建、切换或搜索 adapter。

#### Scenario: LocalSolver 缺少 executor

- **WHEN** Host 配置 LocalSolver 但没有正式 Motion Executor
- **THEN** pipeline 创建 MUST 失败
- **AND** MUST 不自动寻找 `CharacterController` 或创建默认 motor

#### Scenario: ExternalPose 配置了 Unity executor

- **WHEN** Host 配置 ExternalPose 且 scene 上仍存在 Unity executor component
- **THEN** Pipeline MUST 不调用该 executor
- **AND** external pose MUST 保持唯一逻辑位姿来源

### Requirement: Correction 必须由 MotionStage 编排并通过正式端口应用

MotionStage MUST 继续拥有 correction phase、application extent 和 acknowledgement provenance。可参与碰撞的 correction delta MUST 进入唯一 execution intent；需要显式重定位的正式 correction MUST 通过 Logic Pose Port 应用。Motion Executor MUST NOT 读取 server tick、input sequence、ack、prediction policy 或 Network Model 类型。

#### Scenario: 部分 correction 参与碰撞执行

- **WHEN** correction plan 选择本 tick 应用部分 position/yaw error
- **THEN** 该 delta MUST 在 MotionStage 中合入最终 execution intent
- **AND** executor actual result MUST 决定实际 application extent
- **AND** 同一 correction MUST 不再通过 pose port 重复应用

#### Scenario: 完整 correction 显式重定位

- **WHEN** correction plan 选择正式完整重定位
- **THEN** MotionStage MUST 通过 Logic Pose Port 应用目标 pose
- **AND** correction result MUST 记录实际 pose、input sequence 和 server tick
- **AND** Presentation MUST 不执行第二次逻辑重定位

### Requirement: Unity CharacterController 必须只存在于正式 adapter 内

当前 Unity 实现 MUST 使用唯一 `UnityCharacterControllerMotionExecutor` 或等价正式 adapter 包装 `CharacterController.Move`、rotation、grounded 和碰撞结果。`CharacterPipelineHost`、`CharacterPipeline` 和 `CharacterMotionStage` MUST NOT 持有 concrete `CharacterController` 依赖。迁移后 MUST 删除旧序列化字段、构造参数、direct Move 和 direct Transform 路径。

#### Scenario: Sandbox Corin 使用 LocalSolver

- **WHEN** Sandbox 创建 Corin pipeline
- **THEN** Host MUST 显式装配 Logic Pose Port 与 Unity Motion Executor
- **AND** executor MUST 显式绑定现有 `CharacterController`
- **AND** Host MUST 不保存第二份 `m_CharacterController` 引用

#### Scenario: 搜索直接 Move 调用

- **WHEN** 迁移完成后检查 CharacterPipeline 主线
- **THEN** `CharacterController.Move` MUST 只存在于正式 Unity executor implementation
- **AND** Graph、Timeline、MotionStage、Network 和 Presentation MUST 不直接调用它

### Requirement: 权威服务端必须独立生成并执行 canonical motion

服务端权威运动 MUST 从服务端接受的 canonical input、action state 和配置生成 motion intent，并使用选定 authoritative simulation backend 得到 canonical pose。客户端 `ResolvedCharacterMotionFact` MAY 用于 prediction comparison、diagnostics 或 correction calculation，但 MUST NOT 作为服务端唯一 canonical displacement 或 canonical pose 来源。

#### Scenario: Unity 权威服务端

- **WHEN** ServerAuthoritativeHybrid 选择 Unity process backend
- **THEN** 服务端 MUST 独立推进 canonical input/action motion 语义
- **AND** MUST 使用服务端 Unity executor 产生 canonical pose

#### Scenario: 纯 CSharp 权威服务端

- **WHEN** ServerAuthoritativeHybrid 选择纯 CSharp KCC backend
- **THEN** 服务端 MUST 独立推进 canonical input/action motion 语义
- **AND** MUST 使用纯 CSharp world constraint implementation 产生 canonical pose
- **AND** DotRecast navigation query MUST NOT 被当作完整 KCC

### Requirement: 确定性模拟必须属于独立完整 Network Model

确定性 KCC、lockstep 或 rollback MUST 作为独立完整 Network Model 设计，拥有自己的确定性数值、world state、input history、replay 和 side-effect commit 规则。当前 float Motion Executor 合同 MUST NOT 暴露未实现 deterministic enum、空 backend、量化 fallback 或运行时模型切换。

#### Scenario: 当前查看 Network Model 配置

- **WHEN** 确定性模型尚未完整实现
- **THEN** authoring UI MUST 不显示 deterministic/rollback 选项
- **AND** CharacterPipeline MUST 不增加 model switch

#### Scenario: 后续实现确定性模型

- **WHEN** 后续 change 完成确定性 runtime、配置、actor binding 和 tick integration
- **THEN** 它 MAY 复用 gameplay identity 与 authoring facts
- **AND** MUST 不被迫通过 float executor contract 表达确定性内部状态

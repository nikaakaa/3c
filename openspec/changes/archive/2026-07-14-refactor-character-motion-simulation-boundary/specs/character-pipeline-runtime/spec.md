## MODIFIED Requirements

### Requirement: CharacterPipelineHost 只负责装配和注册

系统 MUST 使用 `CharacterPipelineHost` 作为每个角色的 Unity 装配点。Host MUST 只负责序列化角色管线定义、Animancer、visual root、Logic Pose Adapter、按 authority mode 需要的 Motion Executor Adapter 和其它 Unity 组件引用，创建 pipeline，并注册和释放 pipeline。Host MUST NOT 直接序列化 BTSMTL RootTree 或 BTSMTL component 类型，MUST NOT 写入动作状态判断、状态切换、motion 结算或 GameplayResult 裁决逻辑。Host MUST NOT 把 concrete `CharacterController` 直接传入 CharacterPipeline。

#### Scenario: Host 创建 LocalSolver pipeline

- **WHEN** Host 以 LocalSolver 初始化
- **THEN** Host MUST 使用 `CharacterPipelineDefinition`、Animancer、显式 Logic Pose Port、显式 Motion Executor 和输入配置创建 `CharacterPipeline`
- **AND** Host MUST NOT 创建 BBB `PlayerBaseState` 或 `PlayerStateRegistry`
- **AND** BTSMTL RootTree MUST 通过 `CharacterPipelineDefinition` 间接进入 pipeline

#### Scenario: Host 创建 ExternalPose pipeline

- **WHEN** Host 以 ExternalPose 初始化
- **THEN** Host MUST 提供显式 Logic Pose Port
- **AND** MUST 不要求或调用 `CharacterController` Motion Executor

#### Scenario: Host 不承担业务逻辑

- **WHEN** 一帧 gameplay tick 执行
- **THEN** Host MUST 只作为已创建 pipeline 的持有者
- **AND** 输入处理、图执行、motion 和 presentation MUST 位于 pipeline 或 stage 中

### Requirement: 节点和 Timeline 不直接结算最终 Transform

系统 MUST 让 BTSMTL 节点和 Timeline 只产出意图、窗口、命令或 cue。最终角色运动 MUST 由 `CharacterMotionStage` 编排，并由当前 authority mode 的正式 Motion Executor 或 Logic Pose Port 应用。Timeline MUST NOT 直接宣称命中成立、直接扣血、直接改写角色 Transform 或选择具体运动 backend。

#### Scenario: Timeline 产出移动意图

- **WHEN** Timeline 轨道或节点表达某段动作位移
- **THEN** 该输出 MUST 进入 `MotionContribution` 或正式 `MotionIntent`
- **AND** `CharacterMotionStage` MUST 决定最终 execution intent
- **AND** 正式 Motion Executor MUST 返回实际运动结果

#### Scenario: Timeline 产出 gameplay window

- **WHEN** Timeline 表达攻击、无敌或取消窗口
- **THEN** 该输出 MUST 进入 pipeline output 的 window samples
- **AND** 命中、伤害和目标归属 MUST 留给后续 gameplay solver 或服务端裁决

### Requirement: CharacterPipeline 支持混合架构 authority mode

系统 MUST 使用独立 CharacterInputSource 与 CharacterMotionAuthority 表达行为。所有合法组合 MUST 继续使用同一 CharacterPipeline 主线；Network Model MUST 只在 actor binding 时选择组合，不得在 Pipeline 内按 model id 分支。LocalSolver MUST 使用显式 Logic Pose Port 与 Motion Executor；ExternalPose MUST 只使用 Logic Pose Port；None MUST 不执行 gameplay motion。系统 MUST NOT 使用 `LocalPredicted`、`RemoteProxy`、concrete `CharacterController` 或 backend enum 作为总控模式。

#### Scenario: 当前本地 Owner

- **WHEN** input source 是 LocalDevice 且 motion authority 是 LocalSolver
- **THEN** Pipeline MUST 采样本地输入并通过正式 Motion Executor 结算本地运动
- **AND** 是否网络预测 MUST 不由 Pipeline enum 决定

#### Scenario: 后续外部位姿角色

- **WHEN** input source 是 ExternalFacts 且 motion authority 是 ExternalPose
- **THEN** Pipeline MUST 使用外部输入驱动 gameplay/animation
- **AND** MUST 只通过 Logic Pose Port 应用外部位姿
- **AND** MUST 不调用 LocalSolver executor 修改逻辑位姿

#### Scenario: 纯展示角色

- **WHEN** input source 和 motion authority 都是 None
- **THEN** Pipeline MUST 不采样控制输入或结算 gameplay motion
- **AND** Presentation MAY 继续消费显式表现数据

#### Scenario: authority mode 依赖缺失

- **WHEN** Host 缺少当前 authority mode 要求的正式端口
- **THEN** Pipeline 创建 MUST 明确失败
- **AND** MUST 不自动搜索组件或回退到另一 authority mode

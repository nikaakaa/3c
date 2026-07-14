## MODIFIED Requirements

### Requirement: ActionProfile 必须收敛为 Transaction behavior 入口

系统 MUST 将现有 `ActionProfile` 视为 Transaction behavior 的专门 profile 或等价实现。`ActionProfile` MAY 保留动作事务的 window、motion、cue 和 gameplay result 细节，但它 MUST 与统一 behavior registry 使用同一 BehaviorId 身份。ActionProfile MUST 配置 prediction、authority、replication 和各输出网络策略，MUST NOT 配置 actor motion correction application 或 Action reject 处理方式。系统 MUST NOT 让 `ActionProfile` 和 Gameplay Behavior profile 为同一动作维护两套互相独立的身份和网络策略。

#### Scenario: Attack.Light.01 已有 ActionProfile

- **WHEN** `CharacterPipelineDefinition` 注册 `Attack.Light.01` ActionProfile
- **THEN** 统一 behavior registry MUST 能按 `BehaviorId = Attack.Light.01` 查询该 Transaction behavior
- **AND** ActionProfile 的 prediction、authority、replication 和输出策略 MUST 成为该 behavior 的 effective policy 来源
- **AND** actor motion correction application MUST 由 MotionSyncDomain 与 CharacterMotionStage 处理，不得来自 ActionProfile

#### Scenario: 重复身份

- **WHEN** 同一 `CharacterPipelineDefinition` 中同时存在 `Attack.Light.01` ActionProfile 和另一个同 id 的 GameplayBehaviorProfile
- **THEN** 配置校验 MUST 报告 duplicate BehaviorId
- **AND** runtime MUST NOT 随机选择其中一个作为策略来源

#### Scenario: Generic profile 误设为 Transaction

- **WHEN** 作者把普通 `GameplayBehaviorProfile` 设置为 `BehaviorKind.Transaction`
- **THEN** 配置校验 MUST 报告该事务行为必须使用 `ActionProfile`
- **AND** 系统 MUST NOT 为同一个事务动作同时维护 `ActionProfile` 和 `GameplayBehaviorProfile` 两套配置

#### Scenario: Runtime 查询事务行为策略源

- **WHEN** Adapter 需要按 `ActionInstanceId` 或 `ActionId` 查询事务行为 profile
- **THEN** ActionRuntime MUST 暴露 transaction-scoped policy source，例如 `ITransactionBehaviorPolicySource`
- **AND** 统一 `IBehaviorNetworkPolicySource` MAY 组合该事务源和非事务 BehaviorProfile
- **AND** 系统 MUST NOT 使用暗示“所有行为都属于 Action”的泛化 source 命名作为统一入口

### Requirement: Stream behavior 必须显式配置连续运动网络策略

系统 MUST 为 Stream behavior 显式配置连续行为的网络可见性策略，包括 command send policy、prediction、authority、snapshot、remote presentation、replication 和 history。普通 locomotion、瞄准或持续移动蓄力等连续行为 MUST 通过 Stream behavior policy 解析网络语义。actor motion correction 的逻辑应用算法 MUST 由 MotionSyncDomain 与 CharacterMotionStage 执行；Stream behavior MUST NOT 保存 partial/full application、Reject 或表现平滑策略。系统 MUST NOT 将 `ClientCommandFrame`、motion correction ack 或 remote movement presentation 长期硬编码为隐藏默认策略。

#### Scenario: 本地预测 locomotion

- **WHEN** `Movement.Locomotion.Move` 配置为本地预测、服务端权威和服务器 snapshot
- **THEN** resolver MUST 允许 input command 进入 MotionSyncDomain
- **AND** incoming correction MUST 由 CharacterMotionStage 使用当前唯一 correction phase 处理
- **AND** Adapter MUST 使用该 Stream policy 生成或过滤 ClientCommandFrame，而不是读取 Graph 节点字段决定发送

#### Scenario: 表现专用 Stream

- **WHEN** 某个 Stream behavior 配置为 local-only presentation
- **THEN** resolver MUST 标记该 behavior 不产生网络 outgoing packet
- **AND** 本地 MotionStage 或 PresentationStage MAY 正常使用该行为的输出

#### Scenario: Pipeline 绑定输入移动策略

- **WHEN** `CharacterPipelineDefinition` 用于本地预测角色
- **THEN** 它 MUST 显式绑定 client command stream behavior 和 motion correction ack stream behavior
- **AND** Adapter MUST 使用这些绑定解析 `ClientCommandFrame` 与 correction acknowledgement
- **AND** 系统 MUST NOT 保留 `Character.Motion.ClientCommandFrame` 或等价隐藏 policy fallback

#### Scenario: 解析 correction acknowledgement

- **WHEN** MotionStage 已产生 MotionCorrectionAcknowledgement SyncFact
- **THEN** resolver MUST 根据绑定 profile 的 Stream kind、Motion domain、authority 和 replication 决定是否发送
- **AND** resolver MUST NOT 读取 correction application result 或 extent 决定 Ack 网络可见性

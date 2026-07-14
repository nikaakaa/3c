# gameplay-behavior-policy-model Specification

## ADDED Requirements

### Requirement: Gameplay Behavior 必须提供统一行为身份

系统 MUST 使用 Gameplay Behavior 或等价模型为所有 gameplay 行为提供统一作者身份。每个 behavior MUST 至少声明稳定 `BehaviorId`、`BehaviorKind`、tags、display name、debug category 和网络策略摘要。Gameplay Behavior MUST 是作者和策略层身份，MUST NOT 直接替代 Graph 节点、Timeline clip、ActionInstance、MotionContribution、StateEffect 或 CueEvent。

#### Scenario: 作者配置轻攻击

- **WHEN** 作者配置 `Attack.Light.01`
- **THEN** 该行为 MUST 有稳定 `BehaviorId = Attack.Light.01`
- **AND** 该行为 MUST 标记为 `BehaviorKind.Transaction`
- **AND** 它的运行时执行 MUST 继续通过 ActionInstance 和 ActionSyncDomain，而不是通过 Graph 路径或 Timeline asset 身份同步

#### Scenario: 作者配置普通移动

- **WHEN** 作者配置普通 locomotion 或移动输入行为
- **THEN** 该行为 MUST 有稳定 BehaviorId，例如 `Movement.Locomotion.Move`
- **AND** 该行为 MUST 标记为 `BehaviorKind.Stream`
- **AND** 系统 MUST NOT 为每一帧普通移动创建 ActionInstance

### Requirement: BehaviorKind 必须决定运行时同步单位

系统 MUST 使用 `BehaviorKind` 决定 behavior 的运行时同步单位。`Transaction` MUST 使用 ActionInstance 和 ActionSyncDomain；`Stream` MUST 使用 input command、MotionSyncDomain、snapshot 和 correction；`State` MUST 使用 StateEffectSyncDomain；`Event` MUST 根据 policy 使用 GameplayResultSyncDomain 或 PresentationSyncDomain。系统 MUST NOT 把所有 behavior 强制映射到同一种 runtime identity。

#### Scenario: 连续移动和攻击同帧发生

- **WHEN** 本地玩家同一 tick 内持续移动并启动轻攻击
- **THEN** 移动 behavior MUST 通过 Stream 语义进入 MotionSyncDomain
- **AND** 攻击 behavior MUST 通过 Transaction 语义进入 ActionSyncDomain
- **AND** 两者 MAY 共享 input sequence 或 actor identity，但 MUST 使用不同同步单位

#### Scenario: 状态效果来源于动作

- **WHEN** `Guard.Counter` 成功后产生短暂无敌状态
- **THEN** 无敌 MUST 作为 State behavior 进入 StateEffectSyncDomain
- **AND** 它 MAY 记录来源 `ActionInstanceId`
- **AND** 它自身生命周期 MUST NOT 等同于来源 ActionInstance

### Requirement: ActionProfile 必须收敛为 Transaction behavior 入口

系统 MUST 将现有 `ActionProfile` 视为 Transaction behavior 的专门 profile 或等价实现。`ActionProfile` MAY 保留动作事务的 window、motion、cue 和 gameplay result 细节，但它 MUST 与统一 behavior registry 使用同一 BehaviorId 身份。系统 MUST NOT 让 `ActionProfile` 和 Gameplay Behavior profile 为同一动作维护两套互相独立的身份和网络策略。

#### Scenario: Attack.Light.01 已有 ActionProfile

- **WHEN** `CharacterPipelineDefinition` 注册 `Attack.Light.01` ActionProfile
- **THEN** 统一 behavior registry MUST 能按 `BehaviorId = Attack.Light.01` 查询该 Transaction behavior
- **AND** ActionProfile 的 prediction、authority、replication、correction 和输出策略 MUST 成为该 behavior 的 effective policy 来源

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

系统 MUST 为 Stream behavior 显式配置连续行为的网络策略，包括 command send policy、prediction、authority、snapshot、remote presentation、correction 和 history。普通 locomotion、瞄准或持续移动蓄力等连续行为 MUST 通过 Stream behavior policy 解析网络语义。系统 MUST NOT 将 `ClientCommandFrame`、motion correction ack 或 remote movement presentation 长期硬编码为隐藏默认策略。

#### Scenario: 本地预测 locomotion

- **WHEN** `Movement.Locomotion.Move` 配置为本地预测、服务端校正
- **THEN** resolver MUST 允许 input command 进入 MotionSyncDomain
- **AND** correction policy MUST 说明平滑纠偏、强制纠偏或拒绝纠偏的处理方式
- **AND** Adapter MUST 使用该 policy 生成或过滤 ClientCommandFrame，而不是读取 Graph 节点字段决定发送

#### Scenario: 表现专用 Stream

- **WHEN** 某个 Stream behavior 配置为 local-only presentation
- **THEN** resolver MUST 标记该 behavior 不产生网络 outgoing packet
- **AND** 本地 MotionStage 或 PresentationStage MAY 正常使用该行为的输出

#### Scenario: Pipeline 绑定输入移动策略

- **WHEN** `CharacterPipelineDefinition` 用于本地预测角色
- **THEN** 它 MUST 显式绑定 client command stream behavior 和 motion correction ack stream behavior
- **AND** Adapter MUST 使用这些绑定解析 `ClientCommandFrame` 与 correction ack
- **AND** 系统 MUST NOT 保留 `Character.Motion.ClientCommandFrame` 或等价隐藏 policy fallback

### Requirement: Behavior policy resolver 必须输出统一 effective policy

系统 MUST 提供 `BehaviorNetworkPolicyResolver` 或等价服务，将 BehaviorProfile、BehaviorKind、SyncFact 类型和可选输出类型解析为只读 effective policy。Effective policy MUST 至少包含是否发送、目标 SyncDomain、packet kind、policy id、过滤原因和 debug summary。Adapter、Inspector preview 和 Runtime Debug MUST 复用同一解析口径。

#### Scenario: 解析 HitWindow

- **WHEN** Runtime 使用 `Attack.Light.01 + WindowType.Hit` 请求策略解析
- **THEN** resolver MUST 返回 Transaction behavior 的 window policy
- **AND** 返回结果 MUST 指出是否进入 ActionSyncDomain、是否写 digest、是否需要 combat history

#### Scenario: 解析 ClientCommandFrame

- **WHEN** Runtime 准备发送本 tick 的移动输入 command
- **THEN** resolver MUST 根据 Stream behavior policy 返回 MotionSyncDomain 的 effective policy
- **AND** Adapter MUST 根据该结果发送或过滤 packet

### Requirement: BehaviorId 不得替代 SyncFacts 边界

系统 MUST 保持 `CharacterPipelineOutput.SyncFacts` 作为唯一网络事实出口。BehaviorId MAY 附着在 SyncFact、debug record 或 policy lookup context 上，但 NetworkSendStage 和 Adapter MUST NOT 直接同步 Graph 路径、SubTree membership、Timeline 结构、Blackboard key 或 BehaviorProfile 资产本身。

#### Scenario: Blackboard 变量参与行为判断

- **WHEN** Graph 读取 Pipeline Blackboard 的 `moveSpeed` 或 `targetKey` 来决定行为输出
- **THEN** 这些 blackboard 值 MAY 影响 MotionContribution、ActionActivationRequest 或 GameplayResult fact
- **AND** 网络层 MUST 只消费转换后的 SyncFacts
- **AND** 系统 MUST NOT 自动发送 blackboard key/value packet

#### Scenario: Behavior profile 只作为策略来源

- **WHEN** Adapter 打包某个 SyncFact
- **THEN** Adapter MAY 使用 BehaviorId 查询 effective policy
- **AND** Adapter MUST NOT 将 BehaviorProfile 序列化为 gameplay packet

### Requirement: Authoring 和 Debug 必须按 Behavior 展示同步闭环

系统 MUST 在 authoring 和 runtime debug 中按 BehaviorId 展示行为同步闭环。作者 MUST 能看到 behavior kind、tags、策略摘要、预计 SyncFacts、预计 SyncDomain packet、缺失策略和被过滤原因。Debug MUST 能从 outgoing/incoming record 追踪到 BehaviorId 或明确说明该 fact 没有关联 behavior。

#### Scenario: 查看角色行为目录

- **WHEN** 作者选中 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 展示该角色可用 behavior registry
- **AND** 每个 behavior MUST 显示 BehaviorId、BehaviorKind 和主要 SyncDomain

#### Scenario: 查看运行时过滤原因

- **WHEN** Runtime Debug 显示一个 local-only cue 或 stream fact 没有发送
- **THEN** Debug MUST 显示对应 BehaviorId、effective policy 和过滤原因
- **AND** 作者不需要回到 Adapter 代码里猜测为什么没发包

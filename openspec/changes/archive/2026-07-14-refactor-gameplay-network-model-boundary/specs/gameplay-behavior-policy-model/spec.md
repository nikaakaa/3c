## MODIFIED Requirements

### Requirement: Gameplay Behavior 必须提供统一行为身份

系统 MUST 使用 Gameplay Behavior identity 为 Transaction、Stream、State 和 Event 提供稳定 BehaviorId、BehaviorKind、display、tags 和 debug category。Gameplay Behavior identity MUST 不保存具体 Network Model 的 prediction、authority、replication、history、snapshot、command send 或 packet kind。

#### Scenario: 定义 Locomotion 行为

- **WHEN** 作者创建 Locomotion Stream behavior
- **THEN** identity MUST 保存 BehaviorId 和 BehaviorKind
- **AND** ServerAuthoritative 网络策略 MUST 保存在模型专属 profile

### Requirement: BehaviorKind 必须决定运行时同步单位

BehaviorKind MUST 决定 gameplay fact 的生命周期形态：Transaction 具有实例生命周期，Stream 是连续事实，State 具有状态实例，Event 是离散事件。BehaviorKind MUST NOT 自己决定特定模型的 packet、snapshot、history 或 remote presentation；这些规则 MUST 由 model policy 解析。

#### Scenario: Stream fact

- **WHEN** Character 产生连续 resolved motion fact
- **THEN** 其 BehaviorKind MUST 是 Stream
- **AND** 当前模型 MAY 按自己的 policy 映射 MotionCommand

### Requirement: ActionProfile 必须收敛为 Transaction behavior 入口

ActionProfile MUST 继续作为 Transaction gameplay identity 与动作定义入口，并使用 ActionId 作为 BehaviorId。ActionProfile MUST 保存 gameplay tags、block/cancel 和 target 语义，但 MUST 不保存任何具体 Network Model 的 prediction、authority、replication、window/motion/cue/result 网络策略。

#### Scenario: Attack.Light.01

- **WHEN** CharacterPipelineDefinition 注册 `Attack.Light.01` ActionProfile
- **THEN** ActionRuntime MUST 使用它建立动作身份和 gameplay 约束
- **AND** ServerAuthoritative model MUST 通过 ActionId 在自己的 profile 中解析网络策略

### Requirement: Stream behavior 必须显式配置连续运动网络策略

需要进入 ServerAuthoritative 模型的 Stream behavior MUST 在 `ServerAuthoritativeCharacterSyncProfile` 中显式配置 command send、prediction、authority、snapshot、remote presentation、replication 和 history。Gameplay behavior identity MUST 不保存这些字段，且模型 profile 缺失时 MUST 配置失败。

#### Scenario: Locomotion Stream policy

- **WHEN** Locomotion resolved motion 需要进入当前模型
- **THEN** model profile MUST 存在对应 BehaviorId 的 Stream policy
- **AND** adapter MUST 不从 GameplayBehavior identity 读取默认策略

### Requirement: Behavior policy resolver 必须输出统一 effective policy

每个 Network Model MUST 提供自己的 Behavior policy resolver。ServerAuthoritative resolver MUST 使用 model profile、BehaviorId、BehaviorKind、fact kind 和可选输出类型解析 effective packet policy。系统 MUST 不提供跨所有模型的统一 packet resolver，也 MUST 不让 CharacterPipeline 调用 model resolver。

#### Scenario: 解析 resolved motion

- **WHEN** ServerAuthoritative adapter 处理 resolved motion fact
- **THEN** resolver MUST 从当前 model profile 解析是否发送、history 和 packet kind
- **AND** 其它模型 MUST 不需要复用该 effective policy 类型

### Requirement: BehaviorId 不得替代 SyncFacts 边界

BehaviorId MUST 只作为 gameplay identity 与 model policy lookup key。Graph、Timeline 和 runtime MUST 继续产生正式 facts；model adapter MUST 不因为拥有 BehaviorId 而重新读取 Graph 或凭空构造未发生的事实。

#### Scenario: Window policy 存在但窗口未发生

- **WHEN** ServerAuthoritative profile 配置了 HitWindow policy，但本 tick 没有 HitWindow fact
- **THEN** adapter MUST 不生成 window packet
- **AND** policy MUST 不驱动 Timeline 或 Blackboard

### Requirement: Authoring 和 Debug 必须按 Behavior 展示同步闭环

Gameplay behavior authoring MUST 展示 identity 和 gameplay kind；model-specific authoring MUST 在自己的 profile Inspector 展示 prediction、authority、replication、history、snapshot 和 expected packet。Runtime Debug MUST 同时显示 BehaviorId 与 ModelId，避免把 model policy 误认为 gameplay identity 自身字段。

#### Scenario: 查看 Locomotion policy

- **WHEN** 作者查看 ServerAuthoritative Character Sync Profile 中的 Locomotion
- **THEN** UI MUST 显示引用的 gameplay BehaviorId
- **AND** MUST 显示该模型的 effective policy 与 packet preview


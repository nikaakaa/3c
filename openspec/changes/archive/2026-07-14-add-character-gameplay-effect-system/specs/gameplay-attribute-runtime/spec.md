## ADDED Requirements

### Requirement: Gameplay Attribute 必须保存 Base Current 与 Revision

系统 MUST 使用稳定 GameplayAttributeId 保存 float BaseValue、CurrentValue 和 ValueRevision。BaseValue MUST 表达持久资源或基础数值；CurrentValue MUST 表达聚合 Active Modifier 后的当前结果。系统 MUST NOT 用 Blackboard、字符串子属性或多个隐藏 Property 节点保存同一 Attribute 真相。

#### Scenario: 角色受到伤害

- **WHEN** Instant Damage Effect 将 Health BaseValue 从 100 修改为 75
- **THEN** Health CurrentValue MUST 从新 BaseValue 和现有 Active Modifier 重新聚合
- **AND** Health revision MUST 递增并记录 before/after

#### Scenario: Graph 读取耐力

- **WHEN** BTSMTL 读取 Stamina
- **THEN** ValueNode MUST 返回 GameplayAttributeStore 的 CurrentValue
- **AND** MUST NOT 从 Blackboard 中查找另一份 Stamina

### Requirement: Attribute 聚合顺序必须固定

系统 MUST 使用 `Base -> Additive -> Multiplicative -> highest-priority Override -> final Clamp` 的固定顺序计算 CurrentValue。相同优先级 MUST 使用稳定插入序列确定结果；最终 Clamp MUST 在 Override 后执行。

#### Scenario: 移速同时受加法和倍率影响

- **WHEN** MoveSpeed Base=5、Additive=1、Multiplicative=1.5
- **THEN** Clamp 前结果 MUST 为 9
- **AND** 计算顺序 MUST 不依赖 Modifier 列表偶然顺序

#### Scenario: Override 超过边界

- **WHEN** MoveSpeed Override=100 且正式最大边界为 10
- **THEN** 最终 CurrentValue MUST 为 10
- **AND** Override MUST NOT 绕过最终 Clamp

### Requirement: Modifier 必须具有稳定来源和精确移除能力

每个 Active Modifier MUST 保存 GameplayEffectHandle、operation、magnitude、priority 和 insertion sequence。Active Effect 移除时 MUST 按 handle 精确移除其全部 Modifier，不得按数值、Attribute 名或显示名搜索。

#### Scenario: 两个 Buff 提供相同加成

- **WHEN** 两个 Active Effect 都向 MoveSpeed 添加数值相同的 Additive Modifier
- **AND** 其中一个 Effect 被移除
- **THEN** Runtime MUST 只移除该 EffectHandle 对应的 Modifier
- **AND** 另一个 Effect 的加成 MUST 保持有效

### Requirement: Attribute 边界和依赖必须显式且无环

Attribute Definition MUST 能声明常量边界或另一 Attribute 提供的动态边界。所有依赖 MUST 在配置期验证存在且无环；缺失引用或环 MUST 配置失败，Runtime MUST NOT 用 0、1 或无限值 fallback。

#### Scenario: Health 使用 MaxHealth 上限

- **WHEN** Health 的最大边界引用 MaxHealth
- **AND** MaxHealth CurrentValue 发生变化
- **THEN** Health MUST 被标记为 dirty 并按新边界重算
- **AND** 依赖传播 MUST 不依赖手写字符串父属性通知

#### Scenario: 属性边界形成环

- **WHEN** Attribute A 的上限引用 B 且 B 的上限引用 A
- **THEN** CharacterGameplayEffectProfile 校验 MUST 失败
- **AND** Runtime MUST NOT 尝试自动打断依赖环

### Requirement: Attribute magnitude 必须使用声明式来源

Effect magnitude MUST 只使用 Constant、已声明 SetByCaller、Source/Target Attribute Snapshot 或 Target Attribute Live dependency。首批 Runtime MUST 拒绝跨角色 Source Attribute Live dependency，且 MUST NOT 执行任意反射表达式或字符串公式。

#### Scenario: Damage 使用 SetByCaller

- **WHEN** Damage Effect 声明 `DamageAmount` 参数并创建 Spec
- **THEN** Spec MUST 锁定该参数值
- **AND** 缺失或额外参数 MUST 导致 Spec 创建失败

#### Scenario: Buff 实时依赖目标属性

- **WHEN** Modifier magnitude 声明 Target Attribute Live dependency
- **THEN** 依赖 Attribute 变化 MUST 使目标 Aggregator 重算
- **AND** Effect 移除时 MUST 解除该依赖

### Requirement: 属性模型不得恢复旧任意 Property 图

系统 MUST 使用一个 AttributeId 对应一个正式聚合器。旧 `Value-Config`、`Value-Buff`、`Mul-Buff`、ComputedProperty 和字符串父依赖 MUST NOT 作为新 Runtime 数据模型继续存在；复杂战斗公式 MUST 进入显式 Effect Execution。

#### Scenario: 配置 MoveSpeed

- **WHEN** 作者配置 MoveSpeed
- **THEN** 系统 MUST 创建一个 MoveSpeed Attribute 与其 Modifier 聚合
- **AND** MUST NOT 要求作者维护一组隐藏字符串子属性

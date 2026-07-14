## ADDED Requirements

### Requirement: CharacterPipelineDefinition 必须提供唯一 Gameplay Effect 配置入口

`CharacterPipelineDefinition` MUST 直接持有一个 `CharacterGameplayEffectProfile`，由其声明该角色的初始 attributes、初始 loose tags、初始 effects 和可使用的 effect registry。Host、BTSMTL RootTree、ActionProfile 或场景组件 MUST NOT 再持有第二份角色 Gameplay Effect 配置。

#### Scenario: 配置 Corin 角色

- **WHEN** 作者打开 Corin 的 `CharacterPipelineDefinition`
- **THEN** 作者 MUST 能从唯一 Gameplay Effect 配置入口查看其初始属性和初始效果
- **AND** 不需要在 Host 或 RootTree 重复填写同一数据

#### Scenario: 缺少 Gameplay Effect 配置

- **WHEN** 一个可运行角色 Definition 没有绑定有效 `CharacterGameplayEffectProfile`
- **THEN** authoring validation MUST 报告明确错误并阻止构建 runtime
- **AND** 系统 MUST NOT 创建空配置 fallback

### Requirement: Gameplay 标识与引用闭包必须在 authoring 阶段严格校验

系统 MUST 对 GameplayTag、Attribute 和 GameplayEffect 使用稳定正式标识，并 MUST 校验重复标识、未知标识、父级 tag、attribute bound、effect requirement、component 引用和 Additional Effect 引用闭包。运行时 MUST 使用已经校验的 registry/index 解析，不得按资产名称、路径、显示文本或 Addressables key 猜测对象。

#### Scenario: Effect 引用未知 Attribute

- **WHEN** modifier authoring 引用了 registry 中不存在的 AttributeId
- **THEN** validation MUST 精确定位该 effect 与 modifier
- **AND** runtime definition MUST NOT 被创建

#### Scenario: Additional Effect 构成循环

- **WHEN** Effect A 的 Additional Effect 闭包最终重新引用 Effect A
- **THEN** validation MUST 显示完整循环链路
- **AND** 系统 MUST NOT 通过运行时深度限制或静默跳过作为 fallback

### Requirement: GameplayEffectDefinition 必须直接提供 Effect Behavior 身份

每个 `GameplayEffectDefinition` MUST 直接实现 Gameplay Contracts 中 Effect 类 `IGameplayBehaviorProfile` 或等价身份合同，并 MUST 使用 `EffectId` 作为唯一 `BehaviorId`。Definition MUST 只保存 gameplay identity、Tag 和 Effect 规则，不得保存 ServerAuthoritative prediction、authority、replication、history、packet 或 endpoint policy。当前网络模型的完整 Effect policy MUST 唯一保存在 `ServerAuthoritativeCharacterSyncProfile` 并按 Effect BehaviorId 引用。系统 MUST NOT 要求作者为同一个 Effect 再创建 generic BehaviorProfile。

#### Scenario: 配置预测 stamina cost

- **WHEN** 作者为一个 Instant stamina cost effect 配置 ClientPredicted Effect policy
- **THEN** EffectDefinition MUST 只提供对应 BehaviorId 和 Effect kind
- **AND** 作者 MUST 在 `ServerAuthoritativeCharacterSyncProfile` 中按该 BehaviorId 配置完整模型策略
- **AND** registry MUST NOT 存在同身份的额外 generic profile

#### Scenario: Effect 身份重复

- **WHEN** 两个 effect definition 声明相同 EffectId
- **THEN** authoring validation MUST 报告重复 Effect behavior identity
- **AND** runtime MUST NOT 按资产顺序选择其中一个

### Requirement: Effect Component authoring 必须保持无状态和类型化

Effect definition MUST 以类型化 component authoring 声明 modifier、granted tag、periodic execution、additional effect 和 gameplay cue 等行为。每个 component MUST 只保存不可变配置并在 spec 构建或 active effect 生命周期边界产生操作；component MUST NOT 保存 actor、active instance、计时器或 mutable stack state。开放新效果行为时 MUST 通过新增 component 实现，不得扩大一个通用 switch 或使用 `object[]` 参数。

#### Scenario: 新增周期伤害效果

- **WHEN** 作者配置 duration、periodic execution 和 Health modifier component
- **THEN** runtime state MUST 位于 `ActiveGameplayEffect`
- **AND** component asset MUST 在多个角色之间安全复用

#### Scenario: Component 保存角色引用

- **WHEN** 一个 component authoring 类型声明 scene actor 或 mutable runtime state 字段
- **THEN** validation MUST 拒绝该配置或类型
- **AND** 系统 MUST NOT 在 clone 后继续使用

### Requirement: Gameplay Effect authoring 必须构建不可变 Runtime Definition

CharacterPipeline 创建运行时前，系统 MUST 将 `CharacterGameplayEffectProfile`、Tag Catalog、Attribute Definition 和 Effect Definition 闭包校验并构建为不可变 `GameplayEffectRuntimeDefinition`。GameplayEffectRuntime MUST 只持有该运行数据，不得回读 CharacterPipelineDefinition、ScriptableObject authoring graph、Inspector context、asset path 或 Addressables key。构建失败 MUST 阻止创建角色运行时，不得创建空 registry 或默认 Effect fallback。

#### Scenario: 创建角色 Gameplay Effect

- **WHEN** CharacterPipelineDefinition 的 Gameplay Effect 配置闭包完整且通过校验
- **THEN** Builder MUST 生成不可变 runtime definition
- **AND** CharacterGameplayEffectAdapter MUST 使用该 runtime definition 创建 GameplayEffectRuntime

#### Scenario: Definition 闭包不完整

- **WHEN** Effect 引用了不在当前 registry 中的 Tag、Attribute 或 Additional Effect
- **THEN** runtime definition 构建 MUST 失败并报告精确引用
- **AND** Adapter MUST NOT 在运行时搜索其他资产补齐

### Requirement: 旧轻量 GE 只能作为迁移参考

本项目 MUST 在 `3C_Client` 现有 runtime 和 authoring 目录内实现正式 Gameplay Effect。系统 MUST NOT 引用 `KaaKaaFrameWork` assembly、复制其 Addressables/Coroutine 生命周期、保留 `BuffHandler`/`PropertyHandler` 并行运行路径或创建独立 GE package。可复用的 Buff definition/instance/module 与 Property modifier 思路 MUST 迁移为本提案规定的正式类型和命名。

#### Scenario: 迁移旧 Buff 概念

- **WHEN** 实现者参考旧 `BuffData`、`BuffInfo` 和 `BuffModuleBase`
- **THEN** 对应职责 MUST 分别落入 definition、spec/active instance 和 stateless typed component
- **AND** 新代码 MUST NOT 对旧项目目录建立 runtime dependency

#### Scenario: 发现旧 Handler 兼容需求

- **WHEN** 现有 3C 代码没有正式消费者依赖旧 `BuffHandler` 或 `PropertyHandler`
- **THEN** 实现 MUST 只保留新 Gameplay Effect 链路
- **AND** MUST NOT 新增旧 Handler compatibility adapter、wrapper 或双写

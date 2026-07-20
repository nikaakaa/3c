# character-gameplay-effect-authoring Specification

## Purpose
规定 Character Gameplay Effect 的唯一配置入口、稳定身份、引用闭包、有限数值校验和不可变 Runtime Definition 构建，保证 authoring 失败时直接阻止角色运行时创建，不产生默认配置或兼容链路。
## Requirements
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

系统 MUST 对 GameplayTag、Attribute 和 GameplayEffect 使用稳定正式标识，并 MUST 校验重复标识、未知标识、父级 tag、attribute bound、effect requirement、component 引用、Additional Effect 引用闭包和全部 authoring float。Attribute initial value、constant bound 与 Magnitude constant/coefficient/post-add MUST 为有限数值。SetByCaller 声明 MUST 使用精确必填参数集合，不得保存可选标记。Additional Effect 参数绑定 MUST 校验子参数完整且不重复、父参数来源已声明、常量来源有限。运行时 MUST 使用已经校验的 registry/index 解析，不得按资产名称、路径、显示文本或 Addressables key 猜测对象，也不得把非法数值替换为默认值。

#### Scenario: Effect 引用未知 Attribute

- **WHEN** modifier authoring 引用了 registry 中不存在的 AttributeId
- **THEN** validation MUST 精确定位该 effect 与 modifier
- **AND** runtime definition MUST NOT 被创建

#### Scenario: Additional Effect 构成循环

- **WHEN** Effect A 的 Additional Effect 闭包最终重新引用 Effect A
- **THEN** validation MUST 显示完整循环链路
- **AND** 系统 MUST NOT 通过运行时深度限制或静默跳过作为 fallback

#### Scenario: Effect 配置 Infinity Magnitude

- **WHEN** Effect component 的 Magnitude constant、coefficient 或 post-add 包含 Infinity
- **THEN** Runtime Definition build MUST 失败并精确定位该 Effect
- **AND** Adapter MUST NOT 创建部分可运行的 Effect registry

#### Scenario: Attribute 初值为 NaN

- **WHEN** CharacterGameplayEffectProfile 的 Initial Attribute 包含 NaN
- **THEN** authoring validation MUST 报告该 Attribute
- **AND** MUST NOT 创建 runtime state            

#### Scenario: Additional Effect 缺少子参数绑定

- **WHEN** 子 Effect 声明 SetByCaller 参数但 Additional Effect 引用没有完整绑定
- **THEN** Runtime Definition build MUST 精确报告父 Effect、子 Effect 和缺失参数
- **AND** 系统 MUST NOT 通过同名复制或默认值补齐

### Requirement: GameplayEffectDefinition 必须直接提供 Effect Behavior 身份

每个 `GameplayEffectDefinition` MUST直接实现Effect类 `IGameplayBehaviorProfile`，并使用 `EffectId`作为唯一 `BehaviorId`。Definition MUST只保存gameplay identity、Tag和Effect规则，不得保存Network Model参数。具体Model Egress MUST只按显式fact-kind coverage消费已提交GameplayFact；系统 MUST不要求同一Effect再创建generic BehaviorProfile，也 MUST不虚构逐Effect网络策略表。                                                                                                                                                          

#### Scenario: 配置 stamina cost      

- **WHEN** 作者配置一个 Instant stamina cost effect                                  
- **THEN** EffectDefinition MUST 只提供对应 BehaviorId 和 Effect kind
- **AND** ServerAuthoritative复制需求 MUST由Effect fact-kind coverage统一表达                          
- **AND** registry MUST NOT 存在同身份的额外 generic profile

#### Scenario: Effect 身份重复

- **WHEN** 两个 effect definition 声明相同 EffectId
- **THEN** authoring validation MUST 报告重复 Effect behavior identity
- **AND** runtime MUST NOT 按资产顺序选择其中一个

### Requirement: Effect Component authoring 必须保持无状态和类型化

Effect definition MUST 以类型化 component authoring 声明 modifier、granted tag、periodic execution、additional effect 和 gameplay cue 等行为。每个 component MUST 只保存不可变配置并在 spec 构建或 active effect 生命周期边界产生操作；component MUST NOT 保存 actor、active instance、计时器或 mutable stack state。开放新效果行为时 MUST 通过新增 component 实现，不得扩大一个通用 switch 或使用 `object[]` 参数。

#### Scenario: 新增周期伤害效果

- **WHEN** 作者配置 duration、periodic execution 和 Health modifier component
- **THEN** runtime state MUST 位于 Target aggregate      
- **AND** component asset MUST 在多个角色之间安全复用

#### Scenario: Component 保存角色引用

- **WHEN** 一个 component authoring 类型声明 scene actor 或 mutable runtime state 字段
- **THEN** validation MUST 拒绝该配置或类型
- **AND** 系统 MUST NOT 在 clone 后继续使用

### Requirement: Gameplay Effect authoring 必须构建不可变 Runtime Definition

Compiler MUST在生成 CharacterSimulationProgram 前闭包校验 CharacterGameplayEffectProfile、Tag Catalog、Attribute Definition 和 Effect Definition，并将其编译为不可变 portable GameplayEffect catalog/operation data。Runtime MUST不回读 CharacterPipelineDefinition、ScriptableObject、asset path 或 Inspector context，也 MUST不创建空 registry/default Effect fallback。

#### Scenario: 编译角色 Gameplay Effect

- **WHEN** CharacterPipelineDefinition 的 GE 配置闭包完整
- **THEN** Compiler MUST将 catalog写入 Program canonical bytes
- **AND** CharacterSimulationState MUST按对应 layout创建 GE slots

#### Scenario: Definition 闭包不完整

- **WHEN** Effect 引用未注册 Tag、Attribute 或 Additional Effect
- **THEN** Program build MUST失败并报告精确 authoring identity

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

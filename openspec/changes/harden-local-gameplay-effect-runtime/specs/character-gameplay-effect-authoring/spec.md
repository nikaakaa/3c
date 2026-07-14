## MODIFIED Requirements

### Requirement: Gameplay 标识、引用闭包与数值必须在 authoring 阶段严格校验

系统 MUST 对 GameplayTag、Attribute 和 GameplayEffect 使用稳定正式标识，并 MUST 校验重复标识、未知标识、父级 tag、attribute bound、effect requirement、component 引用、Additional Effect 引用闭包和全部 authoring float。Attribute initial value、constant bound 与 Magnitude constant/coefficient/post-add MUST 为有限数值。运行时 MUST 使用已经校验的 registry/index 解析，不得按资产名称、路径、显示文本或 Addressables key 猜测对象，也不得把非法数值替换为默认值。

#### Scenario: Effect 配置 Infinity Magnitude

- **WHEN** Effect component 的 Magnitude constant、coefficient 或 post-add 包含 Infinity
- **THEN** Runtime Definition build MUST 失败并精确定位该 Effect
- **AND** Adapter MUST NOT 创建部分可运行的 Effect registry

#### Scenario: Attribute 初值为 NaN

- **WHEN** CharacterGameplayEffectProfile 的 Initial Attribute 包含 NaN
- **THEN** authoring validation MUST 报告该 Attribute
- **AND** GameplayAttributeStore MUST NOT 被创建

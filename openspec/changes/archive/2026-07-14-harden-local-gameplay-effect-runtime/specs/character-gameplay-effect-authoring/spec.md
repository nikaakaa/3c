## MODIFIED Requirements

### Requirement: Gameplay 标识与引用闭包必须在 authoring 阶段严格校验

系统 MUST 对 GameplayTag、Attribute 和 GameplayEffect 使用稳定正式标识，并 MUST 校验重复标识、未知标识、父级 tag、attribute bound、effect requirement、component 引用、Additional Effect 引用闭包和全部 authoring float。Attribute initial value、constant bound 与 Magnitude constant/coefficient/post-add MUST 为有限数值。SetByCaller 声明 MUST 使用精确必填参数集合，不得保存可选标记。Additional Effect 参数绑定 MUST 校验子参数完整且不重复、父参数来源已声明、常量来源有限。运行时 MUST 使用已经校验的 registry/index 解析，不得按资产名称、路径、显示文本或 Addressables key 猜测对象，也不得把非法数值替换为默认值。

#### Scenario: Effect 配置 Infinity Magnitude

- **WHEN** Effect component 的 Magnitude constant、coefficient 或 post-add 包含 Infinity
- **THEN** Runtime Definition build MUST 失败并精确定位该 Effect
- **AND** Adapter MUST NOT 创建部分可运行的 Effect registry

#### Scenario: Attribute 初值为 NaN

- **WHEN** CharacterGameplayEffectProfile 的 Initial Attribute 包含 NaN
- **THEN** authoring validation MUST 报告该 Attribute
- **AND** GameplayAttributeStore MUST NOT 被创建

#### Scenario: Additional Effect 缺少子参数绑定

- **WHEN** 子 Effect 声明 SetByCaller 参数但 Additional Effect 引用没有完整绑定
- **THEN** Runtime Definition build MUST 精确报告父 Effect、子 Effect 和缺失参数
- **AND** 系统 MUST NOT 通过同名复制或默认值补齐

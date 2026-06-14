## ADDED Requirements
### Requirement: 轻攻击动作动画 Key
系统 MUST 为第一版三段轻攻击提供稳定动作动画 key。key MUST 表达动作语义和连段段位，不得表达具体角色、clip 文件名或导入来源。

#### Scenario: 第一段轻攻击 key
- **WHEN** 当前攻击段为 `Action.Attack01`
- **THEN** 动作动画 key MUST 为 `Action.Attack.Light.01` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: 第二段轻攻击 key
- **WHEN** 当前攻击段为 `Action.Attack02`
- **THEN** 动作动画 key MUST 为 `Action.Attack.Light.02` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: 第三段轻攻击 key
- **WHEN** 当前攻击段为 `Action.Attack03`
- **THEN** 动作动画 key MUST 为 `Action.Attack.Light.03` 或等价稳定 ID
- **AND** 该 key MUST 可由动作动画 Profile 解析

#### Scenario: key 不绑定具体角色资源
- **WHEN** 系统定义轻攻击动作动画 key
- **THEN** key MUST NOT 包含可琳、Corin、具体 fbx、具体 clip 文件名或 BBB 路径

### Requirement: 轻攻击动画绑定校验
系统 MUST 能校验轻攻击三段动作动画绑定是否完整。动作动画 Profile 或等价绑定入口 MUST 不决定攻击能否进入、能否接段或是否造成伤害。

#### Scenario: 三段动画引用完整
- **GIVEN** 动作动画 Profile 配置了 `Action.Attack.Light.01`
- **AND** 配置了 `Action.Attack.Light.02`
- **AND** 配置了 `Action.Attack.Light.03`
- **WHEN** 运行 Profile 校验
- **THEN** 校验 MUST 接受这三段轻攻击动画绑定

#### Scenario: 缺失轻攻击动画引用报错
- **GIVEN** 动作动画 Profile 缺少 `Action.Attack.Light.01`、`Action.Attack.Light.02` 或 `Action.Attack.Light.03` 的动画引用
- **WHEN** 运行轻攻击装配校验
- **THEN** 校验结果 MUST 包含 error
- **AND** 系统 MUST NOT 静默播放 fallback 动画

#### Scenario: Presenter 不决定连段
- **WHEN** 动画 Presenter 播放轻攻击动画
- **THEN** Presenter MUST NOT 消费 Attack 请求
- **AND** Presenter MUST NOT 判断 combo window
- **AND** Presenter MUST NOT 切换 `Action.Attack01`、`Action.Attack02` 或 `Action.Attack03`

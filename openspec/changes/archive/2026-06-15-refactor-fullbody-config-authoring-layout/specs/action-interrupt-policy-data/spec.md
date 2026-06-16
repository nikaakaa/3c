## ADDED Requirements
### Requirement: FullBody 请求策略集合命名和归属
系统 MUST 将同时包含 Dodge、TurnBack 或后续 FullBody 状态请求策略的默认策略集合命名并归属为 `CorinFullBodyStateRequestPolicySet.asset` 或批准的等价 FullBody state request policy，而不是 Dodge-only policy。策略集合的名称、目录和根配置引用 MUST 反映其覆盖范围，避免设计者误判该资产只影响 `Action.Dodge`。

#### Scenario: 多请求策略集合不使用 Dodge-only 命名
- **GIVEN** 默认策略集合同时包含 `Action.Dodge` 和 `FullBody/Locomotion/TurnBack` 或等价 TurnBack request policy
- **WHEN** 检查该策略集合资产
- **THEN** 资产名称 MUST 为 `CorinFullBodyStateRequestPolicySet.asset` 或批准的等价 FullBody state request policy 名称
- **AND** 资产 MUST NOT 使用 `DefaultDodgeInterruptPolicySet` 或等价 Dodge-only 名称作为正式资产名

#### Scenario: 策略集合位于动作请求归属目录
- **WHEN** 检查默认策略集合目录
- **THEN** 策略集合 MUST 位于 `Assets/Configs/3C/Action/FullBody/RequestPolicy/` 或批准的等价 FullBody 请求策略目录
- **AND** 它 MUST NOT 放在 Locomotion animation、StateMachine topology 或 Animancer transition 目录下

#### Scenario: 缺失策略集合不回退旧 Dodge 策略
- **GIVEN** 角色配置根或正式装配点缺失 FullBody 请求策略集合
- **WHEN** 请求准入需要 priority、resistance 或 timing window policy
- **THEN** 系统 MUST 报告配置错误或拒绝对应请求
- **AND** MUST NOT 自动查找旧 `DefaultDodgeInterruptPolicySet` 路径作为 fallback

## MODIFIED Requirements

### Requirement: 动作 Request 必须由 Authoring 声明业务 Timing Class

`CharacterActionRequestDefinition` MUST为每个离散request保存稳定timing class，当前正式值为`Immediate`与`Offensive`。Timing class MUST表达request的业务类别，不得保存具体Network Model、Tick延迟或packet policy。CharacterInputProfile Inspector与C#作者API MUST读写同一字段；缺失或非法值 MUST作为配置错误，MUST不按request id、InputAction显示名或字符串前缀推断类别。

#### Scenario: 作者配置攻击请求

- **WHEN** 作者把Corin Attack request标记为Offensive
- **THEN** CharacterInputProfile MUST保存该timing class
- **AND** C#作者API与Inspector MUST读取同一配置

#### Scenario: 请求没有合法 Timing Class

- **WHEN** CharacterInputProfile包含未定义的timing class值
- **THEN** 配置校验 MUST失败
- **AND** Runtime MUST不回退为Immediate

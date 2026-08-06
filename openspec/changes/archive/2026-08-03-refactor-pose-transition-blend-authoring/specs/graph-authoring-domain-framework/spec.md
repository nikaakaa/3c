## ADDED Requirements

### Requirement: 共享Details必须支持强类型条件资产字段

Authoring Capability Catalog MUST能为AssetReference字段声明精确Unity资产类型、稳定picker kind与基于同一selection其它字段的可见条件。共享Node Details与StateMachine Transition Details MUST使用同一类型合同创建受限ObjectField，并在Mutation前拒绝错误类型；MUST不把强类型资产引用降级为GUID、identity或自由文本框。不可见字段 MUST不接受Mutation，也 MUST不被Document codec当作合法nullable payload。

#### Scenario: Pose Transition选择Custom

- **WHEN** Pose Transition的Blend Mode从EaseOut切换为Custom
- **THEN** 共享Transition Details MUST显示只接受`CharacterAnimationBlendCurveAsset`的Custom Curve字段
- **AND** Blend Profile字段 MUST只接受`CharacterAnimationBlendProfile`

#### Scenario: Pose Transition选择内置模式

- **WHEN** Pose Transition使用Linear、EaseIn、EaseOut或EaseInOut
- **THEN** Custom Curve字段 MUST从Details隐藏
- **AND** Mutation与Document MUST拒绝该不适用字段

#### Scenario: Gameplay Transition被选择

- **WHEN** 当前selection属于BTSMTL Gameplay StateMachine Transition
- **THEN** 共享Details MUST继续只显示Gameplay transition字段
- **AND** MUST不显示Blend Mode、Custom Curve或Blend Profile

#### Scenario: Pose State被选择

- **WHEN** 当前selection属于Pose StateMachine State
- **THEN** 共享State Details MUST显示唯一`Always Reset on Entry`布尔字段
- **AND** Transition与Sequence Player Details MUST不再显示Reset On Entry字段

## ADDED Requirements
### Requirement: Corin 默认角色配置闭环资产
系统 MUST 维护一个 Corin 默认角色配置根资产，作为默认角色配置的唯一正式入口。该根资产 MUST 能解析状态机、基础移动、Locomotion 动画、FullBody 动作请求策略、Dodge 动作逻辑、动作动画或 Animancer rig variant、输入和相机配置。

#### Scenario: 根资产引用完整
- **WHEN** 自动校验加载 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
- **THEN** StateMachine、Movement、LocomotionAnimation、FullBodyStateRequestPolicy、DodgeAction、AnimancerRigVariant、InputActions、MoveAction、RunAction、LookAction 和 CameraConfig MUST 全部可解析
- **AND** 缺失任一必需引用 MUST 被报告为配置错误
- **AND** 系统 MUST NOT 使用旧 controller 字段补齐缺失引用

#### Scenario: 根资产不引用旧目录
- **WHEN** 自动校验追踪 Corin 根配置的正式引用链
- **THEN** 引用链 MUST NOT 包含 `Assets/Configs/3C/Animacer/`
- **AND** MUST NOT 包含 `Assets/Configs/3C/Statemachine/`
- **AND** MUST NOT 包含 `Pramater` 拼写目录
- **AND** MUST NOT 包含 `TestTurnback`、`turnback` 或 `testTurn` 命名资产作为正式配置

#### Scenario: 根资产引用无悬空 GUID
- **WHEN** 自动校验 Corin 根配置和关键子资产引用
- **THEN** 每个正式引用 MUST 能通过 AssetDatabase 或等价资产数据库解析
- **AND** dangling GUID、空引用或缺失 `.meta` MUST 被报告为配置错误

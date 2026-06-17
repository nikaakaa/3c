## MODIFIED Requirements

### Requirement: Corin 默认角色配置闭环资产
系统 MUST 维护一个 Corin 默认角色配置根资产，作为默认角色配置的唯一正式入口。该根资产 MUST 能解析状态机、基础移动、Locomotion 动画、Action Interrupt 策略、Action Catalog、BodyClaimPolicy、输入和相机配置。

#### Scenario: 根资产引用完整
- **WHEN** 自动校验加载 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
- **THEN** StateMachine、Movement、LocomotionAnimation、ActionInterruptPolicy、ActionCatalog、BodyClaimPolicy、InputActions、MoveAction、RunAction、LookAction、DodgeInputAction 和 CameraConfig MUST 全部可解析
- **AND** ActionCatalog MUST 能解析 `Action.Dodge` definition
- **AND** 缺失任一必需引用 MUST 被报告为配置错误
- **AND** 系统 MUST NOT 使用旧 controller 字段或旧 `DodgeAction` 平铺字段补齐缺失引用

#### Scenario: 根资产不引用旧目录
- **WHEN** 自动校验追踪 Corin 根配置的正式引用链
- **THEN** 引用链 MUST NOT 包含 `Assets/Configs/3C/Animacer/`
- **AND** MUST NOT 包含 `Assets/Configs/3C/Statemachine/`
- **AND** MUST NOT 包含 `Assets/Configs/3C/Action/FullBody/`
- **AND** MUST NOT 包含 `Pramater` 拼写目录
- **AND** MUST NOT 包含 `TestTurnback`、`turnback` 或 `testTurn` 命名资产作为正式配置

#### Scenario: 根资产引用无悬空 GUID
- **WHEN** 自动校验 Corin 根配置和关键子资产引用
- **THEN** 每个正式引用 MUST 能通过 AssetDatabase 或等价资产数据库解析
- **AND** dangling GUID、空引用或缺失 `.meta` MUST 被报告为配置错误

### Requirement: Character Runtime Controller 使用根配置
`CharacterFrameRuntimeController` 或等价正式角色入口 MUST 从 `CharacterConfigSO` 根配置追踪 Corin 当前 playable 主线需要的 StateMachine、Movement、LocomotionAnimation、Action Interrupt policy、Action Catalog、BodyClaimPolicy、Input 和 Camera 配置。它 MUST NOT 从 FullBody、Locomotion legacy serialized fields 或 `DodgeAction` 平铺字段建立正式 fallback 配置入口。

#### Scenario: Prefab 绑定根配置
- **WHEN** 检查 Corin 正式 prefab 上的角色 runtime 入口
- **THEN** `CharacterFrameRuntimeController` MUST 引用正式 `CharacterConfigSO`
- **AND** 该根配置 MUST 能追踪当前 playable 主线需要的子配置
- **AND** Action Catalog MUST 能解析 `Action.Dodge` definition
- **AND** FullBody、Locomotion legacy serialized config fields 或旧 `DodgeAction` 平铺字段 MUST NOT 成为正式 fallback

#### Scenario: Scene override 不恢复旧入口
- **WHEN** 检查纳入范围的 Corin playable scene override
- **THEN** override MUST 保持 `CharacterFrameRuntimeController` 作为正式入口
- **AND** MUST NOT 重新启用 FullBody 或 Locomotion autoUpdate 作为正式主线
- **AND** MUST NOT 通过 scene override 恢复 `DodgeAction` 平铺配置作为正式入口
- **AND** MUST NOT 新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter

#### Scenario: 缺失正式配置显式失败
- **GIVEN** `CharacterFrameRuntimeController` 缺少正式根配置或根配置缺少必要子配置
- **WHEN** 角色初始化或装配校验运行
- **THEN** 系统 MUST 报告明确错误
- **AND** MUST NOT 回退到 legacy flat fields 或旧 `DodgeAction` 字段
- **AND** MUST NOT 创建隐藏默认配置

## ADDED Requirements
### Requirement: Character Runtime Controller 使用根配置
`CharacterFrameRuntimeController` 或等价正式角色入口 MUST 从 `CharacterConfigSO` 根配置追踪 Corin 当前 playable 主线需要的 StateMachine、Movement、LocomotionAnimation、FullBody request policy、Dodge、Animancer、Input 和 Camera 配置。它 MUST NOT 从 FullBody 或 Locomotion legacy serialized fields 建立正式 fallback 配置入口。

#### Scenario: Prefab 绑定根配置
- **WHEN** 检查 Corin 正式 prefab 上的角色 runtime 入口
- **THEN** `CharacterFrameRuntimeController` MUST 引用正式 `CharacterConfigSO`
- **AND** 该根配置 MUST 能追踪当前 playable 主线需要的子配置
- **AND** FullBody 或 Locomotion legacy serialized config fields MUST NOT 成为正式 fallback

#### Scenario: Scene override 不恢复旧入口
- **WHEN** 检查纳入范围的 Corin playable scene override
- **THEN** override MUST 保持 `CharacterFrameRuntimeController` 作为正式入口
- **AND** MUST NOT 重新启用 FullBody 或 Locomotion autoUpdate 作为正式主线
- **AND** MUST NOT 新增第二 pipeline、第二 runner、第二 motion executor 或第二 animation presenter

#### Scenario: 缺失正式配置显式失败
- **GIVEN** `CharacterFrameRuntimeController` 缺少正式根配置或根配置缺少必要子配置
- **WHEN** 角色初始化或装配校验运行
- **THEN** 系统 MUST 报告明确错误
- **AND** MUST NOT 回退到 legacy flat fields
- **AND** MUST NOT 创建隐藏默认配置

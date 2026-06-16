## ADDED Requirements
### Requirement: 角色配置根作为作者总入口
系统 MUST 让 `CharacterConfigSO` 或批准的等价角色配置根成为默认角色配置的作者总入口。该入口 MUST 以命名子模块方式组织正式配置引用，使设计者能从一个资产追踪状态机、基础移动、动作逻辑、动作动画、Locomotion 动画、Animancer 表现、输入和相机配置。该入口 MUST NOT 通过旧平铺字段、Resources、全局单例或硬编码路径提供 fallback。

#### Scenario: 默认角色根位于角色目录
- **WHEN** 检查默认 Corin 角色配置根
- **THEN** 正式资产 MUST 位于 `Assets/Configs/3C/Character/Corin/CorinCharacterConfig.asset`
- **AND** `Assets/Configs/3C/CharacterConfig.asset` MUST NOT 作为第二正式入口保留
- **AND** 迁移 MUST 保留 Unity 引用所需的 `.meta` GUID 或更新所有正式引用

#### Scenario: 设计者从根入口追踪子配置
- **WHEN** 设计者打开默认角色配置根
- **THEN** 设计者 MUST 能定位 `stateMachine` 或等价状态机配置引用
- **AND** MUST 能定位 `movement` 或等价基础移动配置引用
- **AND** MUST 能定位 `fullBodyAction` 或等价动作逻辑配置引用
- **AND** MUST 能定位 `fullBodyActionAnimation` 或等价动作动画配置引用
- **AND** MUST 能定位 `locomotionAnimation` 或等价基础移动动画配置引用
- **AND** MUST 能定位 Generic 或等价正式 Animancer rig variant 配置引用
- **AND** MUST NOT 要求 Humanoid rig variant 作为本次默认角色配置根的必需引用
- **AND** MUST 能定位 `input` / `inputReferences` 或等价输入配置引用
- **AND** MUST 能定位 `camera` 或等价相机配置引用

#### Scenario: 缺失正式子配置不 fallback
- **GIVEN** 默认角色配置根缺失任一正式必需子配置
- **WHEN** 正式 gameplay 路径需要该配置
- **THEN** 系统 MUST 输出明确配置错误诊断
- **AND** MUST 停止对应状态机、动作、动画、输入或相机输出
- **AND** MUST NOT 从旧目录、旧字段、代码默认值或场景查找结果继续运行

#### Scenario: prefab 只装配正式根入口
- **WHEN** 检查默认可琳 prefab 或正式场景装配
- **THEN** 角色主调度入口 MUST 能通过角色配置根解析正式子配置
- **AND** 新增正式配置 Module 时 MUST 优先增加角色配置根的命名子模块引用
- **AND** 不应继续在 controller 上新增互不相干的平铺配置字段作为正式扩展方式

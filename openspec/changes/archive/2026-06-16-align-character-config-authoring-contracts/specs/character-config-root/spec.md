## ADDED Requirements
### Requirement: 配置作者入口校验口径
系统 MUST 将 `CharacterConfigSO` 视为角色配置作者总入口。controller 上的旧平铺序列化字段 MAY 在迁移期间保留数据，但 MUST NOT 作为正式运行时配置入口、fallback 或新增模块扩展方式。系统 MUST 提供自动校验报告旧字段、第二正式入口和缺失根配置。

#### Scenario: 旧字段只作为迁移残留
- **WHEN** 检查角色 prefab 或 scene 中的 `PlayerLocomotionController` 和 `PlayerFullBodyActionController`
- **THEN** `characterConfig` MUST 指向正式角色根配置
- **AND** `runAnimationConfig`、`config`、`stateMachineDefinition`、`interruptPolicySet`、`dodgeActionConfig` 等旧平铺字段 MUST NOT 被视为正式入口
- **AND** 系统 MUST NOT 通过这些旧字段补齐缺失的根配置子模块

#### Scenario: 新模块从根入口扩展
- **WHEN** 后续新增 Action、Input、Camera、UpperBody 或 LowerBody 配置模块
- **THEN** 新模块 MUST 优先作为 `CharacterConfigSO` 的命名子模块接入
- **AND** controller MUST NOT 新增同义平铺配置字段作为正式扩展方式

#### Scenario: 缺失根配置不会使用旧字段
- **GIVEN** controller 旧平铺字段仍持有可加载资产
- **AND** `CharacterConfigSO` 或对应子模块为空
- **WHEN** 正式 gameplay 路径读取该配置
- **THEN** 系统 MUST 报告配置缺失或停止对应输出
- **AND** MUST NOT 读取旧平铺字段继续运行

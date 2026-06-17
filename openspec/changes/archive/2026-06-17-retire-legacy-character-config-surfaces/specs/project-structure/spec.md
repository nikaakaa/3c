## ADDED Requirements

### Requirement: 废弃目录不得作为正式配置入口

项目正式配置目录 MUST 反映当前角色主线。旧 FullBody 目录、旧 FullBody 状态机目录和旧 FullBody 动画目录不得作为正式配置、测试样例或未来动作模板继续存在。

#### Scenario: 正式配置只使用角色专属 Action 目录

- **GIVEN** 配置资产目录被扫描
- **WHEN** 测试检查 Action 配置布局
- **THEN** Corin 的正式动作配置位于 `Assets/Configs/3C/Action/Corin`
- **AND** `Assets/Configs/3C/Action/FullBody` 不作为正式配置目录存在

#### Scenario: 正式状态图只使用 Locomotion 目录

- **GIVEN** 配置资产目录被扫描
- **WHEN** 测试检查状态机配置布局
- **THEN** Corin 的移动状态图位于 `Assets/Configs/3C/StateMachine/Locomotion/Corin`
- **AND** `Assets/Configs/3C/StateMachine/FullBody` 不作为正式状态机目录存在

#### Scenario: 正式动画配置只使用角色或模块明确目录

- **GIVEN** 动画配置目录被扫描
- **WHEN** 测试检查 Animancer profile 和动作动画配置布局
- **THEN** 正式动画配置位于角色或模块明确的当前目录
- **AND** `Assets/Configs/3C/Animation/FullBody` 不作为正式动画配置目录存在

### Requirement: 主动规格不得继续引用旧主线作为实现目标

Active specs 和新提案 MUST 不把旧 FullBody 主树、旧 Host Adapter、旧 tick adapter 或旧 presenter 描述为正式实现目标。历史内容若必须提及旧名称，必须明确标记为废弃、迁移或兼容只读语境。

#### Scenario: 新规格不复用旧 FullBody 主线

- **GIVEN** active specs 和未归档变更被检查
- **WHEN** 文档扫描发现 `Action/FullBody`、`StateMachine/FullBody`、`PlayerFullBodyActionController`、旧 tick adapter 或旧 presenter
- **THEN** 每处引用必须处于历史、废弃、迁移或只读兼容语境
- **AND** 不得作为未来动作实现或正式运行时接入路径

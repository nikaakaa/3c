## ADDED Requirements
### Requirement: 正式 Animancer 配置目录命名
系统 MUST 将正式角色 Animancer 播放配置放在 `Assets/Configs/3C/Animation/<角色>/Animancer/...` 或批准的等价 `Animation` 目录下。旧拼写 `Assets/Configs/3C/Animacer/...` MUST NOT 作为正式运行时配置入口。

#### Scenario: Corin Generic rig variant 位于正式目录
- **WHEN** 检查 Corin 默认 Generic Animancer transition library
- **THEN** 正式资产 MUST 位于 `Assets/Configs/3C/Animation/Corin/Animancer/RigVariants/Generic/`
- **AND** `Assets/Configs/3C/Animacer/` MUST NOT 被视为正式入口
- **AND** `Pramater` 拼写目录 MUST NOT 被视为正式参数目录

#### Scenario: 旧目录只能作为迁移残留
- **WHEN** 项目中仍存在旧 `Animacer`、`Statemachine` 或 `Pramater` 目录
- **THEN** 静态校验 MUST 报告它们不是正式入口
- **AND** 正式角色配置根 MUST NOT 引用这些旧目录中的资产

#### Scenario: 规格文字不再批准旧目录为正式入口
- **WHEN** 检查 OpenSpec 当前规格和 active changes
- **THEN** 若 `Animacer`、`Statemachine` 或 `Pramater` 出现，文本 MUST 明确标记为 legacy、迁移残留或反例
- **AND** MUST NOT 同时把旧目录和新目录描述为两个正式入口

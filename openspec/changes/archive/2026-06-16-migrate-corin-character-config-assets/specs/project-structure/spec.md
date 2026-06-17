## ADDED Requirements
### Requirement: Corin 配置资产迁移边界
系统 MUST 将 Corin 正式配置资产保持在 `Assets/Configs/3C` 的职责目录蓝图内。迁移资产时 MUST 保留 `.meta` GUID 或同步更新所有正式引用，并通过自动测试发现 dangling GUID。

#### Scenario: 资产位于正式目录
- **WHEN** 检查 Corin 默认配置资产
- **THEN** 角色根 MUST 位于 `Assets/Configs/3C/Character/Corin/`
- **AND** 状态机 MUST 位于 `Assets/Configs/3C/StateMachine/FullBody/`
- **AND** 动作逻辑 MUST 位于 `Assets/Configs/3C/Action/FullBody/`
- **AND** 动画表现 MUST 位于 `Assets/Configs/3C/Animation/Corin/`
- **AND** 基础移动 MUST 位于 `Assets/Configs/3C/Movement/`
- **AND** 输入和相机 MUST 位于各自正式目录

#### Scenario: GUID 迁移可追踪
- **WHEN** 正式资产需要移动或重命名
- **THEN** 实施 MUST 优先保留 `.meta` GUID
- **AND** 若 GUID 无法保留，必须更新所有正式 `.asset` 引用
- **AND** 自动测试 MUST 报告 dangling GUID 或空引用

#### Scenario: Prefab 和 Scene 不在本变更中迁移
- **WHEN** 实施 Corin 配置资产迁移
- **THEN** diff MUST NOT 修改 `Assets/Prefabs/Character/可琳.prefab`
- **AND** MUST NOT 修改 `Assets/Prefabs/Character/可琳_Humanoid.prefab`
- **AND** MUST NOT 修改正式场景 `.unity` 文件

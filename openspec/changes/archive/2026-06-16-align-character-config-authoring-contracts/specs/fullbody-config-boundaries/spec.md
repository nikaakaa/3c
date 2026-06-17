## ADDED Requirements
### Requirement: FullBody 配置作者口径先行对齐
系统 MUST 在迁移 FullBody 资产和 prefab 前先统一配置作者口径。FullBody 状态机、动作逻辑、动作动画、Locomotion 动画、Animancer 播放资产、输入和相机配置 MUST 能从角色配置根追踪，但每类资产仍 MUST 保持原有职责边界。

#### Scenario: 根配置追踪不合并职责
- **WHEN** 设计者从 `CorinCharacterConfig.asset` 追踪 FullBody 相关配置
- **THEN** 状态机拓扑 MUST 仍归状态机资产
- **AND** Dodge motion 和 request policy MUST 仍归动作逻辑资产
- **AND** 动作动画 Profile 和 Animancer Transition 资产 MUST 仍归动画表现资产
- **AND** 根配置 MUST NOT 复制这些子资产内部字段作为第二权威

#### Scenario: 规格冲突先报告
- **WHEN** 现有规格或测试同时允许旧目录和新目录作为正式入口
- **THEN** 本变更 MUST 先通过 spec delta 和静态测试明确唯一正式入口
- **AND** 后续资产迁移 MUST 以该入口为验收标准


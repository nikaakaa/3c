## ADDED Requirements
### Requirement: Corin Prefab Locomotion 动画绑定迁移
系统 MUST 让 Corin prefab 的 Locomotion 动画绑定从正式角色配置根和正式 animation presenter 路径解析。Prefab 迁移 MUST NOT 通过旧 `runAnimationConfig` 字段形成第二动画配置权威。

#### Scenario: Locomotion controller 从根配置解析动画配置
- **WHEN** 自动校验 Corin prefab 上的 `PlayerLocomotionController`
- **THEN** `characterConfig.LocomotionAnimation` MUST 是正式 Locomotion 动画配置来源
- **AND** 旧 `runAnimationConfig` 字段 MUST NOT 作为正式 fallback
- **AND** 缺失 Locomotion 动画配置 MUST 被报告为配置错误

#### Scenario: Presenter 引用不丢失
- **WHEN** prefab 迁移完成后运行角色帧输出
- **THEN** Locomotion animation presentation MUST 仍通过正式 presenter 或统一 presenter 路径提交
- **AND** 状态机、motion executor 和 prefab 迁移逻辑 MUST NOT 直接调用 Animancer runtime 对象

#### Scenario: Locomotion 运行时引用保持可解析
- **WHEN** 自动校验 Corin prefab 上的 `PlayerLocomotionController`
- **THEN** input source、motion executor、facing provider、camera reference 和 locomotion presenter 引用 MUST 保持可解析或明确为空且由正式 resolver 处理
- **AND** 迁移 MUST NOT 新增跨角色全局查找来补齐这些引用

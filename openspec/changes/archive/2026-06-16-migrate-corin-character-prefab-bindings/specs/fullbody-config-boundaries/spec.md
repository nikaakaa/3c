## ADDED Requirements
### Requirement: Corin Prefab 装配保持 FullBody 配置边界
系统 MUST 让 Corin prefab 的 FullBody 主入口只装配配置根和 runtime 组件引用，不在 prefab 上重复维护状态树、动作逻辑和动画表现的正式配置权威。

#### Scenario: FullBody 主入口不复制配置权威
- **WHEN** 自动校验 Corin prefab 上的 `PlayerFullBodyActionController`
- **THEN** 它 MUST 能通过角色配置根解析状态机、request policy 和 Dodge action config
- **AND** 它 MUST NOT 通过旧平铺字段与角色配置根形成两个不同正式配置源
- **AND** 它 MUST NOT 隐式从 Resources、全局单例或硬编码路径加载缺失配置

#### Scenario: 运行时组件引用保持清晰
- **WHEN** 自动校验 Corin prefab 上的 FullBody runtime 组件链
- **THEN** input buffer、locomotion controller、facing provider、motion executor 和 animation presenter 引用 MUST 可解析
- **AND** 这些引用 MUST NOT 替代配置资产职责


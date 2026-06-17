## ADDED Requirements
### Requirement: Corin FullBody 配置资产闭环
系统 MUST 让 Corin 的 FullBody 状态机、动作请求策略、Dodge 动作逻辑和动作动画或 Animancer 表现配置从 `CorinCharacterConfig.asset` 可追踪，同时保持各自职责边界，不复制同一运行时语义为多个权威。

#### Scenario: FullBody 子资产职责分离
- **WHEN** 自动校验 Corin FullBody 配置闭环
- **THEN** 状态机资产 MUST 只表达状态树拓扑、节点绑定、timeline policy 和纯数据输出
- **AND** Dodge 动作资产 MUST 只表达动作逻辑数值
- **AND** request policy 资产 MUST 只表达请求和打断策略
- **AND** 动画或 Animancer 资产 MUST 只表达表现绑定和播放参数

#### Scenario: Dodge motion 参数单一来源
- **WHEN** 校验 Corin `Action.Dodge` 配置
- **THEN** Directional 和 Backstep 的 motion duration/distance MUST 只来自正式 Dodge 动作逻辑配置
- **AND** 状态机资产 MUST NOT 并行保存能决定同一 motion 参数的旧输出字段


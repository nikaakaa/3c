## ADDED Requirements
### Requirement: Locomotion 诊断模块化迁移
系统 MUST 允许将 `PlayerLocomotionController` 中的 Locomotion 和 TurnBack 诊断日志迁移到 `LocomotionDiagnostics` 或等价模块。迁移 MUST 保持现有 eventId、等级和关键上下文，不得删除用户未要求删除的日志。

#### Scenario: 日志 key 保持稳定
- **WHEN** Locomotion 诊断日志从 controller 移动到 diagnostics 模块
- **THEN** 关键 eventId MUST 保持稳定
- **AND** 日志 MUST 继续包含 step/frame、状态路径、phase、gait 或 TurnBack 相关上下文

#### Scenario: Diagnostics 不成为行为模块
- **WHEN** diagnostics 模块输出 Locomotion 或 TurnBack 日志
- **THEN** 该模块 MUST NOT 计算状态转移
- **AND** MUST NOT 调用 motion executor
- **AND** MUST NOT 播放动画
- **AND** MUST NOT 改写 runtime blackboard

#### Scenario: 必要错误仍直接可见
- **WHEN** 缺正式配置、检测到退役直驱入口或发现双 driver 装配问题
- **THEN** 系统 MUST 继续输出明确 warning/error
- **AND** 不得因为诊断模块化而静默吞掉这些错误

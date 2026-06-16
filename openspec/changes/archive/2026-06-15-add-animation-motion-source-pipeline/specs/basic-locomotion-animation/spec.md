## ADDED Requirements

### Requirement: 已审批动画运动源边界
系统 MUST 允许经 OpenSpec 审批的基础移动或 FullBody locomotion 状态通过通用动画运动源管线贡献运动。第一版贡献 MUST 在 `TickSampledMotion` 模式下转换为纯数据 movement facts 并由统一 motion executor 应用。该能力不得改变普通 Walk/Run 动画只负责表现的默认边界。

#### Scenario: 普通基础移动仍只表现
- **WHEN** 角色播放 Idle、MoveStart、MoveLoop 或 MoveStop 的普通 Walk/Run 动画
- **THEN** 动画外观层 MUST 继续只消费移动动画上下文和暴露只读播放进度
- **AND** MUST NOT 直接移动角色根

#### Scenario: 已审批状态使用动画运动源
- **GIVEN** 当前逻辑状态声明了通用动画运动源策略
- **WHEN** 动画播放进度产生本 tick 采样窗口
- **THEN** 基础移动动画系统 MUST 能按策略提供该状态的 yaw 或 translation 数据
- **AND** MUST 提交为 movement facts

#### Scenario: TurnBack 作为首个使用者
- **GIVEN** 当前逻辑状态为 `FullBody/Locomotion/TurnBack`
- **WHEN** TurnBack 配置启用通用动画运动源策略
- **THEN** 系统 MUST 使用该通用策略解析 `Locomotion.Turn.Back` 的动画运动贡献
- **AND** 默认 MUST 选择 `TickSampledMotion` 以支持后续预测、回滚和预测矫正
- **AND** MUST NOT 依赖 TurnBack 专用 pending runtime root delta 分支作为默认运动来源

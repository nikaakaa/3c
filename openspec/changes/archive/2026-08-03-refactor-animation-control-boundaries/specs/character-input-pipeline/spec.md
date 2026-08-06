# character-input-pipeline Delta Specification

## ADDED Requirements

### Requirement: 数字方向冲突必须按输入值的显式策略解析

`CharacterInputValueDefinition` MUST为Vector2输入保存显式数字方向冲突策略。选择最近激活方向策略时，来源action MUST包含唯一且完整的上下左右Dpad composite；缺失part、重复composite或非Vector2输入 MUST作为配置错误。Float32与Fixed Unity Input Adapter MUST在本地设备采样边界执行同一策略，并把解析结果写入现有portable `CharacterSimulationInput`。系统 MUST不在Gameplay StateMachine、Rollback snapshot、网络协议或Pose Graph中保存物理按键冲突状态。

#### Scenario: W到S交接存在短暂重叠

- **WHEN** W仍按住时玩家按下S，随后才松开W
- **THEN** `MoveAxis`纵轴 MUST立即解析为最近激活的向后方向
- **AND** 中间输入 MUST不因Dpad相消成为零
- **AND** RunLoop MUST能用同一正式MovingTurn准入条件判断该方向

#### Scenario: 相反方向全部松开

- **WHEN** 玩家松开同一轴上的全部数字方向键
- **THEN** 该轴 MUST恢复InputAction的零值
- **AND** 解析器 MUST不延续最近激活方向形成粘键

#### Scenario: 输入没有相反方向冲突

- **WHEN** 同一轴没有同时按下正负方向part
- **THEN** Adapter MUST保留InputAction在该轴上的正式值
- **AND** 模拟摇杆、单方向键与非冲突斜向输入 MUST不被最近激活历史覆盖

#### Scenario: 配置无法唯一解析Dpad

- **WHEN** 作者为非Vector2输入或没有唯一完整Dpad composite的action选择最近激活方向策略
- **THEN** CharacterInputProfile配置校验 MUST失败
- **AND** Runtime MUST不回退为固定方向优先级或静默相消

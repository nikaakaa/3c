## ADDED Requirements
### Requirement: Locomotion Adapter 模块化边界
系统 MUST 将 `PlayerLocomotionController` 收窄为 FullBody pipeline 下的 Locomotion adapter facade。可测试的移动意图、空间事实、状态机 context、TurnBack intent、TurnBack motion facts 和状态输出到基础移动帧的构建逻辑 MUST 迁移到明确模块中，且不得改变现有 Locomotion 行为语义。

#### Scenario: Controller 只保留 facade 职责
- **WHEN** Locomotion 模块化重构完成
- **THEN** `PlayerLocomotionController` MUST 只负责 Unity 引用解析、生命周期、FullBody pipeline 调用入口和必要运行时缓存
- **AND** 它 MUST NOT 内联维护大段 TurnBack motion、diagnostics 或纯逻辑 facts 构建实现

#### Scenario: Facts builder 保持纯逻辑边界
- **WHEN** FullBody pipeline 请求 Locomotion facts
- **THEN** 系统 MUST 通过 `LocomotionFactsBuilder` 或等价模块生成移动意图、空间事实和状态机 context
- **AND** 该模块 MUST NOT 直接持有 `MonoBehaviour`、`Transform`、`Camera`、Animancer runtime、`InputAction` 或 motion executor

#### Scenario: TurnBack 模块不改变输出
- **GIVEN** 相同的输入快照、相机/facing 事实、上一帧移动方向和状态机 snapshot
- **WHEN** 拆分后的 TurnBack intent 与 motion 模块运行
- **THEN** 输出的 `LocomotionTurnBackIntent`、`BasicMovementMotionFacts`、input lock 和 yaw/delta 语义 MUST 与拆分前一致

#### Scenario: 状态输出构建不提交运动
- **WHEN** 拆分后的模块将 `CharacterStateMachineFrame` 转换为基础移动帧
- **THEN** 该模块 MAY 构建 `BasicLocomotionFrame`、`MovementCommand` 和动画上下文
- **AND** 该模块 MUST NOT 直接调用 motion executor
- **AND** 该模块 MUST NOT 直接播放 Animancer

### Requirement: Locomotion Adapter 重构可验证
系统 MUST 为 Locomotion adapter 模块化重构提供自动测试、静态边界测试和 Play Mode 手动验证，证明拆分只改变代码组织，不改变玩法输出或 runtime authority。

#### Scenario: 自动测试覆盖拆分前后一致性
- **WHEN** 运行 Locomotion module EditMode 测试
- **THEN** 测试 MUST 覆盖 locomotion facts 一致性
- **AND** MUST 覆盖 TurnBack intent 一致性
- **AND** MUST 覆盖 TurnBack motion facts 一致性
- **AND** MUST 覆盖 state frame 到 locomotion frame 的输出一致性

#### Scenario: 静态测试覆盖模块边界
- **WHEN** 运行边界测试或静态搜索
- **THEN** 验证 MUST 确认拆出的纯逻辑模块不引用 Unity 场景实例、Animancer runtime、`InputAction` 或 tick driver
- **AND** MUST 确认 `PlayerLocomotionController` 不创建 `CharacterStateMachineRunner`

#### Scenario: 手动验证现有移动不回退
- **WHEN** 开发者进入 Sandbox Play Mode
- **THEN** WASD Idle、MoveStart、MoveLoop、MoveStop MUST 保持可用
- **AND** RunLoop 反向输入 MUST 仍进入 TurnBack
- **AND** Dodge 后恢复 Locomotion 的现有行为 MUST 保持可用

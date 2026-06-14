## ADDED Requirements
### Requirement: Dodge Backstep 恢复退出条件
系统 SHALL 在统一角色逻辑状态机配置中表达 `Action.Dodge.Backstep` 的无输入回 Idle 退出规则。Backstep 的动作位移时长 MUST 与恢复退出条件分离；无输入回 Idle MUST 等待动作恢复退出事实，而不得只依赖动作位移 duration。

#### Scenario: Backstep 未恢复时保持 Dodge
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动意图
- **AND** Backstep 动作位移 duration 已达到
- **WHEN** 动作恢复退出事实为 false
- **THEN** 统一状态机 MUST 保持在 `FullBody/Action/Dodge`
- **AND** MUST NOT 切换到 `FullBody/Locomotion/Idle`

#### Scenario: Backstep 恢复完成后回 Idle
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** 本帧没有移动意图
- **WHEN** 动作恢复退出事实为 true
- **THEN** 统一状态机 MUST 切换到 `FullBody/Locomotion/Idle`
- **AND** Backstep MUST NOT 写入 Run latch

#### Scenario: Backstep 恢复段输入移动可提前回移动
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Backstep`
- **AND** Backstep 动作位移 duration 已达到
- **AND** 动作恢复退出事实为 false
- **WHEN** 本帧出现移动意图
- **THEN** 统一状态机 MUST 能切换到 `FullBody/Locomotion/MoveLoop` 或等价移动恢复阶段
- **AND** MUST NOT 等待 Backstep 动画完整播放结束
- **AND** Backstep MUST NOT 写入 Run latch

#### Scenario: Backstep 位移参数不被动画长度污染
- **WHEN** 设计者配置 Backstep 动作位移
- **THEN** Backstep 位移 duration 和 distance MUST 继续表达动作运动窗口
- **AND** MUST NOT 因等待动画恢复而被强制改成动画 clip 总长

#### Scenario: Directional Dodge 行为保持
- **GIVEN** 当前状态为 `FullBody/Action/Dodge`
- **AND** 当前变体为 `Directional`
- **AND** 本帧存在移动意图
- **WHEN** Directional 动作位移 duration 达到
- **THEN** 统一状态机 MUST 仍能切换到 `FullBody/Locomotion/MoveLoop`
- **AND** MUST 保持 Directional 完成后写入 Run latch 的现有行为

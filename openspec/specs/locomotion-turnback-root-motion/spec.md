# locomotion-turnback-root-motion Specification

## Purpose
TBD - created by archiving change add-moving-pivot-turn. Update Purpose after archive.
## Requirements
### Requirement: 删除旧转身运行系统
系统 MUST 删除旧 `TurnInPlace` 原地转身运行系统和旧 `MovingPivotTurn` baked yaw 运行系统。普通 locomotion frame、animation context、状态机 frame、runtime blackboard 和 rollback state MUST NOT 携带旧 TurnInPlace/MovingPivot plan。

#### Scenario: 默认状态图不含旧原地转身状态
- **WHEN** 创建默认角色状态机
- **THEN** 状态列表 MUST 包含 Idle、MoveStart、MoveLoop、MoveStop 和 Dodge
- **AND** 状态列表 MUST NOT 包含 `FullBody/Locomotion/TurnInPlace`

#### Scenario: locomotion frame 不承载旧转身 plan
- **WHEN** 基础移动 pipeline 生成 `BasicLocomotionFrame`
- **THEN** frame MUST 只表达输入、phase、gait、world direction 和 movement command
- **AND** frame MUST NOT 携带 TurnInPlace 或 MovingPivot plan

### Requirement: TurnBack 不再使用 baked yaw 补丁
系统 MUST NOT 使用 MovingPivotTurn config、selector、plan、baked yaw profile 或旧 TurnInPlace config 表达移动反向 TurnBack。TurnBack 后续 MUST 由明确逻辑状态和 root motion delta 处理。

#### Scenario: 旧配置类型不可被运行时代码引用
- **WHEN** 编译运行时代码
- **THEN** 运行时代码 MUST NOT 引用 `TurnInPlaceAnimationConfigSO`
- **AND** 运行时代码 MUST NOT 引用 `MovingPivotTurnAnimationConfigSO`
- **AND** 运行时代码 MUST NOT 引用 `TurnInPlacePlan` 或 `MovingPivotTurnPlan`

#### Scenario: 诊断保留但不绑定旧概念
- **WHEN** movement executor 应用 animation motion 或抑制普通输入运动
- **THEN** 系统 MUST 仍输出可诊断 yaw、位移、输入旋转和输入位移的信息
- **AND** 日志事件 MUST 使用通用 animation-motion 概念，而不是 moving-pivot-turn 运行概念

### Requirement: 移动 TurnBack 逻辑状态
系统 MUST 将移动反向急转表达为 `FullBody/Locomotion/TurnBack` 逻辑状态。角色在 MoveLoop 中收到与上一有效移动方向夹角达到阈值的移动输入时 MUST 进入 TurnBack；TurnBack MUST 播放 `Locomotion.Turn.Back`；TurnBack 结束后 MUST 根据当前输入回到 MoveLoop 或 Idle。

#### Scenario: MoveLoop 反向输入进入 TurnBack
- **GIVEN** 角色处于 `FullBody/Locomotion/MoveLoop`
- **AND** runtime blackboard 记录上一有效移动方向
- **WHEN** 当前移动输入方向与上一有效移动方向夹角达到 TurnBack 阈值
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/TurnBack`
- **AND** 当前 locomotion phase MUST 为 `TurnBack`

#### Scenario: TurnBack 动画结束后退出
- **GIVEN** 角色处于 `FullBody/Locomotion/TurnBack`
- **WHEN** locomotion animation facts 显示 `Locomotion.Turn.Back` 已结束
- **AND** 当前仍有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/MoveLoop`
- **WHEN** locomotion animation facts 显示 `Locomotion.Turn.Back` 已结束
- **AND** 当前没有移动输入
- **THEN** 状态机 MUST 转入 `FullBody/Locomotion/Idle`

### Requirement: TurnBack Root Motion 运动权威
系统 MUST 在 TurnBack 窗口内禁止普通输入旋转和普通输入平面位移，并通过统一 `MovementCommand` / motion executor 应用 Animator/Animancer root motion delta。TurnBack MUST NOT 通过 baked yaw/profile 或独立 CharacterController 路径移动角色。

#### Scenario: TurnBack 命令锁定普通输入运动
- **WHEN** locomotion controller 在 `TurnBack` phase 构建 movement command
- **THEN** command MUST 设置 `SuppressInputRotation=true`
- **AND** command MUST 设置 `SuppressInputPlanarMovement=true`

#### Scenario: TurnBack root motion 进入统一 executor
- **GIVEN** locomotion root motion source 提供本帧 local planar delta 和 yaw delta
- **WHEN** locomotion controller 在 `TurnBack` phase 构建 movement command
- **THEN** command MUST 携带该 animation delta
- **AND** movement executor MUST 通过通用 animation-motion 分支应用该 delta


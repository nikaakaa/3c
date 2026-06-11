# unityhfsm-locomotion Specification

## Purpose
记录基础 Locomotion 四阶段在当前角色系统中的语义。该能力已由统一层级角色逻辑状态机承载，不再要求独立 UnityHFSM Locomotion 状态机作为运行时权威。

## Requirements
### Requirement: 基础 Locomotion 四阶段归属统一状态机
系统 MUST 使用统一角色逻辑状态机表达 `Idle / MoveStart / MoveLoop / MoveStop` 四个基础移动阶段。Walk/Run MUST 作为基础移动档位事实进入 pipeline、命令和动画上下文，MUST NOT 被建模为逻辑状态。

#### Scenario: 初始化进入 Idle
- **WHEN** 统一角色逻辑状态机初始化
- **THEN** 当前状态 MUST 为 `FullBody/Locomotion/Idle`
- **AND** 阶段计时 MUST 为 0

#### Scenario: 有移动意图进入 MoveStart
- **GIVEN** 当前状态为 `FullBody/Locomotion/Idle`
- **WHEN** 本帧存在移动意图
- **THEN** 状态机 MUST 切换到 `FullBody/Locomotion/MoveStart`
- **AND** 阶段计时 MUST 从切换后重新开始

#### Scenario: 起步达到退出事实进入 MoveLoop
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStart`
- **AND** 本帧持续存在移动意图
- **WHEN** `PhaseCanExit` 或等价纯数据退出事实为 true
- **THEN** 状态机 MUST 切换到 `FullBody/Locomotion/MoveLoop`

#### Scenario: 停止完成回到 Idle
- **GIVEN** 当前状态为 `FullBody/Locomotion/MoveStop`
- **AND** 本帧没有移动意图
- **WHEN** `PhaseCanExit` 或等价纯数据退出事实为 true
- **THEN** 状态机 MUST 切换到 `FullBody/Locomotion/Idle`

#### Scenario: Walk/Run 不扩张逻辑阶段
- **WHEN** 普通移动选择 Walk 档位或 Run latch 选择 Run 档位
- **THEN** 状态机 MUST 仍只输出 Locomotion 四阶段或 Action 状态
- **AND** MUST NOT 输出 `WalkStart / WalkLoop / WalkEnd`
- **AND** MUST NOT 输出 `RunStart / RunLoop / RunEnd`

### Requirement: Locomotion Pipeline 接入统一状态机
系统 MUST 让基础 Locomotion pipeline 根据统一状态机给出的 phase 构建 `MovementCommand`，并 MUST 保持输入、相机相对方向、运动档位、运动执行、动画表现和相机 Resolve 的既有顺序。

#### Scenario: Pipeline 不推进状态
- **WHEN** 基础 Locomotion pipeline 处理一帧输入
- **THEN** pipeline MUST 使用调用方传入的 `BasicMovementPhase`
- **AND** MUST 构建携带档位的 `MovementCommand`
- **AND** MUST NOT 自行推进第二套 Locomotion 状态机

#### Scenario: 运动命令继续使用 BasicMovementPhase
- **WHEN** 统一角色逻辑状态机输出当前 Locomotion phase
- **THEN** `MovementCommand` MUST 继续携带 `BasicMovementPhase`
- **AND** `MovementAnimationContext` MUST 继续携带 `BasicMovementPhase`
- **AND** Walk/Run MUST 作为单独档位事实携带，不得替代 phase

#### Scenario: Pipeline 不依赖具体运动实现
- **WHEN** 基础 Locomotion pipeline 构建移动命令
- **THEN** pipeline MUST NOT 持有 `CharacterMotionDriver` 具体类型
- **AND** pipeline MUST NOT 调用 `CharacterController.Move`
- **AND** pipeline MUST NOT 调用 KCC API

### Requirement: 可替换输入和运动端口
系统 MUST 通过输入端口读取移动、视角和 Run 保持输入，并通过运动执行端口提交移动命令，使输入实现和运动实现不会影响统一状态机。

#### Scenario: Controller 只读取输入快照
- **WHEN** `PlayerLocomotionController` 执行一帧 tick
- **THEN** 它 MUST 从输入端口读取 `BasicLocomotionInputSnapshot` 或等价快照
- **AND** controller MUST NOT 直接读取 `InputAction`
- **AND** controller MUST NOT 硬编码读取键盘 Shift

#### Scenario: 运动执行只走端口
- **WHEN** `PlayerLocomotionController` 接收到统一状态机输出的基础移动帧
- **THEN** 它 MUST 将 `MovementCommand` 提交给运动执行端口
- **AND** 它 MUST NOT 直接调用 `CharacterController.Move`

#### Scenario: 状态机不依赖 adapter
- **WHEN** 统一角色逻辑状态机推进 Locomotion 四阶段
- **THEN** 状态机 runner MUST NOT 引用 `CharacterController`
- **AND** MUST NOT 引用 `UnityEngine.InputSystem`
- **AND** MUST NOT 引用 Animancer runtime

### Requirement: 可测试和可诊断
系统 MUST 为统一状态机中的 Locomotion 四阶段提供自动测试、静态边界验证和运行时诊断信息。

#### Scenario: 自动测试覆盖四阶段
- **WHEN** 运行统一状态机 EditMode 测试
- **THEN** 测试 MUST 覆盖 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop` 的主要流转
- **AND** MUST 覆盖 `MoveStop` 重新输入立即回到 `MoveStart`

#### Scenario: 状态机边界可静态验证
- **WHEN** 检查统一状态机 runner 和 transition evaluator 源码
- **THEN** 静态搜索 MUST 能确认它们不引用 Animancer
- **AND** MUST 能确认不引用 `CharacterController`
- **AND** MUST 能确认不引用 Unity Input System

#### Scenario: 当前状态可诊断
- **WHEN** 开发者调试当前 FullBody base layer
- **THEN** 系统 MUST 暴露当前状态路径
- **AND** SHOULD 暴露当前 Locomotion phase 和 Walk/Run 档位

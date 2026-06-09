# unityhfsm-locomotion Specification

## Purpose
TBD - created by archiving change integrate-unityhfsm-locomotion. Update Purpose after archive.
## Requirements
### Requirement: UnityHFSM 基础 Locomotion 阶段机
系统 MUST 使用项目已安装的 UnityHFSM 管理基础 Locomotion 的 `Idle / MoveStart / MoveLoop / MoveStop` 阶段，并 MUST 保留当前基础移动阶段语义。

#### Scenario: 初始化进入 Idle
- **WHEN** 基础 Locomotion 阶段机初始化
- **THEN** 当前阶段 MUST 为 `Idle`
- **AND** 阶段计时 MUST 为 0

#### Scenario: 有移动意图进入 MoveStart
- **GIVEN** 当前阶段为 `Idle`
- **WHEN** 本帧存在移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStart`
- **AND** 阶段计时 MUST 从切换后重新开始

#### Scenario: 起步达到最小时长进入 MoveLoop
- **GIVEN** 当前阶段为 `MoveStart`
- **AND** 本帧持续存在移动意图
- **WHEN** `MoveStart` 阶段计时达到 `MoveStartMinTime`
- **THEN** 阶段机 MUST 切换到 `MoveLoop`

#### Scenario: 起步中断进入 MoveStop
- **GIVEN** 当前阶段为 `MoveStart`
- **WHEN** 本帧没有移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStop`

#### Scenario: 循环移动停止进入 MoveStop
- **GIVEN** 当前阶段为 `MoveLoop`
- **WHEN** 本帧没有移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStop`

#### Scenario: 停止完成回到 Idle
- **GIVEN** 当前阶段为 `MoveStop`
- **AND** 本帧没有移动意图
- **WHEN** `MoveStop` 阶段计时达到 `MoveStopMinTime`
- **THEN** 阶段机 MUST 切换到 `Idle`

#### Scenario: 停止期间重新移动
- **GIVEN** 当前阶段为 `MoveStop`
- **WHEN** 本帧重新存在移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStart`

### Requirement: Locomotion Pipeline 接入 UnityHFSM
系统 MUST 将基础 Locomotion pipeline 的阶段来源切换为 UnityHFSM 适配器，同时 MUST 保持输入、相机相对方向、运动命令、运动执行、动画表现和相机 Resolve 的既有顺序。

#### Scenario: Pipeline 顺序保持
- **WHEN** 基础 Locomotion pipeline 处理一帧输入
- **THEN** 系统 MUST 先生成输入快照
- **AND** MUST 再生成移动意图
- **AND** MUST 再解析相机相对世界方向
- **AND** MUST 再推进 UnityHFSM Locomotion 阶段
- **AND** MUST 再构建 `MovementCommand`
- **AND** MUST 再提交给运动执行端口
- **AND** MUST 再提交 `MovementAnimationContext`
- **AND** MUST 最后完成相机 Resolve

#### Scenario: 运动命令继续使用 BasicMovementPhase
- **WHEN** UnityHFSM Locomotion 阶段机输出当前阶段
- **THEN** `MovementCommand` MUST 继续携带 `BasicMovementPhase`
- **AND** `MovementAnimationContext` MUST 继续携带 `BasicMovementPhase`

#### Scenario: Pipeline 不依赖具体运动实现
- **WHEN** 基础 Locomotion pipeline 执行移动
- **THEN** 系统 MUST 通过运动执行端口提交 `MovementCommand`
- **AND** pipeline MUST NOT 持有 `CharacterMotionDriver` 具体类型
- **AND** pipeline MUST NOT 调用 `CharacterController.Move`
- **AND** pipeline MUST NOT 调用 KCC API

### Requirement: 可替换输入端口
系统 MUST 通过基础 Locomotion 输入端口读取移动和视角输入，使 `PlayerLocomotionController` 不直接依赖具体 `InputActionReference`。

#### Scenario: Controller 只读取输入快照
- **WHEN** `PlayerLocomotionController` 执行一帧 tick
- **THEN** 它 MUST 从输入端口读取 `BasicLocomotionInputSnapshot` 或等价快照
- **AND** 它 MUST NOT 直接读取 `moveAction`
- **AND** 它 MUST NOT 直接读取 `lookAction`

#### Scenario: Input System 只存在于 adapter
- **WHEN** 当前实现需要使用 Unity Input System
- **THEN** `InputActionReference` MUST 只出现在输入 adapter 中
- **AND** `PlayerLocomotionController` MUST NOT 引用 `InputActionReference`
- **AND** `PlayerLocomotionController` MUST NOT 引用 `UnityEngine.InputSystem`

#### Scenario: 输入端口支持替换
- **WHEN** 后续接入输入服务、网络预测、回放或 AI 输入
- **THEN** 系统 MUST 能通过替换输入端口实现提供相同输入快照
- **AND** UnityHFSM 阶段机 MUST NOT 因输入来源替换而修改
- **AND** `BasicLocomotionPipeline` MUST NOT 因输入来源替换而修改

### Requirement: PlayerLocomotionController 入口命名
系统 MUST 将当前基础移动主调度入口从 WASD demo 命名直接迁移为 `PlayerLocomotionController`，并 MUST 迁移旧组件引用而不是保留旧类兼容包装。

#### Scenario: 新命名表达职责
- **WHEN** 基础移动主调度入口完成迁移
- **THEN** 新入口名称 MUST 为 `PlayerLocomotionController`
- **AND** 新入口名称 MUST NOT 继续把该主链描述为纯 WASD 键盘脚本

#### Scenario: 旧组件引用被迁移
- **WHEN** 重命名 `BasicWASDMovementController`
- **THEN** 实施 MUST 迁移 prefab/scene 引用到 `PlayerLocomotionController`
- **AND** 实施 MUST NOT 新增 `BasicWASDMovementController` 兼容包装
- **AND** 验证 MUST 能确认当前演示场景的组件引用仍有效

#### Scenario: 命名空间保持统一
- **WHEN** 新增或重命名基础 Locomotion 运行时代码
- **THEN** 运行时代码 MUST 使用现有 `ThirdPersonMovement` 命名空间
- **AND** MUST NOT 为本次迁移新增 BBB、KCC sample 或临时 movement namespace

#### Scenario: 旧命名从运行时代码中消失
- **WHEN** 实施完成
- **THEN** 新运行时代码和新测试 MUST 使用 `PlayerLocomotionController`
- **AND** `BasicWASDMovementController` MUST NOT 出现在新运行时代码中

### Requirement: 可替换运动执行端口
系统 MUST 通过基础 Locomotion 运动执行端口提交 `MovementCommand`，使当前 CharacterController 实现和未来 KCC 实现不会影响 UnityHFSM 状态机或 pipeline。

#### Scenario: 当前 CharacterController 实现仍可执行移动
- **WHEN** `PlayerLocomotionController` 生成 `MovementCommand`
- **THEN** 系统 MUST 能通过当前 CharacterController executor 或 adapter 执行移动
- **AND** `CharacterController.Move` MUST 只出现在该 executor 或 adapter 内

#### Scenario: 状态机不依赖运动实现
- **WHEN** UnityHFSM Locomotion 阶段机推进阶段
- **THEN** 阶段机 MUST NOT 引用 `CharacterMotionDriver`
- **AND** MUST NOT 引用 `CharacterController`
- **AND** MUST NOT 引用 `KinematicCharacterMotor`

#### Scenario: Controller 只提交端口
- **WHEN** `PlayerLocomotionController` 执行一帧 tick
- **THEN** 它 MUST 从输入端口读取输入快照
- **AND** MUST 将 `MovementCommand` 提交给运动执行端口
- **AND** 它 MUST NOT 直接调用 `CharacterController.Move`
- **AND** 它 MUST NOT 直接调用 KCC API

#### Scenario: 端口提供诊断数据
- **WHEN** 动画表现或调试需要读取当前移动结果
- **THEN** 运动执行端口 MUST 暴露当前平面速度
- **AND** SHOULD 暴露最后一次世界移动方向

### Requirement: BBB 参考边界
系统 MAY 参考 BBB 的状态装配和分层思想，但 MUST NOT 引入 BBB 运行时依赖或复制 BBB 主控路径。

#### Scenario: 不依赖 BBB 运行时
- **WHEN** UnityHFSM Locomotion 接入完成
- **THEN** 新增或修改的运行时代码 MUST NOT 引用 `BBBNexus` 命名空间
- **AND** MUST NOT 依赖 `Ref/BBB` 下的运行时类型、Prefab 或 ScriptableObject

#### Scenario: 不复制 BBB 主控
- **WHEN** 重命名和接入基础 Locomotion 主链
- **THEN** 系统 MUST NOT 新增 `BBBCharacterController` 等价的大型角色主控复制品
- **AND** MUST NOT 在本变更中引入完整 Brain、Registry、InputPipeline 或 Interceptor SO 主线

#### Scenario: 不复制状态内部互跳
- **WHEN** 基础 Locomotion 阶段需要切换
- **THEN** 切换规则 MUST 集中在 UnityHFSM Locomotion 阶段机或其 builder/adapter 中
- **AND** MUST NOT 通过多个状态类互相直接查找并切换来实现四阶段流转

### Requirement: 可测试和可诊断
系统 MUST 为 UnityHFSM Locomotion 接入提供自动测试、静态边界验证和运行时可诊断信息。

#### Scenario: 自动测试覆盖四阶段
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 覆盖 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop` 的主要流转
- **AND** MUST 覆盖阶段计时门槛
- **AND** MUST 覆盖 `Reset`

#### Scenario: 自动测试覆盖运动端口
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 能用 fake motion executor 验证 `MovementCommand` 提交
- **AND** MUST 验证 pipeline 不要求具体 `CharacterMotionDriver` MonoBehaviour

#### Scenario: 自动测试覆盖输入端口
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 能用 fake input source 驱动 `PlayerLocomotionController`
- **AND** MUST 验证 controller 不要求具体 `InputActionReference`

#### Scenario: 状态机边界可静态验证
- **WHEN** 实施完成
- **THEN** 静态搜索 MUST 能确认 UnityHFSM Locomotion 阶段机不引用 Animancer
- **AND** MUST 能确认不引用 `CharacterController`
- **AND** MUST 能确认不引用 KCC
- **AND** MUST 能确认不引用 `Camera.main`、`CinemachineFreeLook` 或具体相机实例

#### Scenario: Controller 输入边界可静态验证
- **WHEN** 实施完成
- **THEN** 静态搜索 MUST 能确认 `PlayerLocomotionController` 不引用 `InputActionReference`
- **AND** MUST 能确认 `PlayerLocomotionController` 不引用 `UnityEngine.InputSystem`

#### Scenario: 当前阶段可诊断
- **WHEN** 开发者调试当前基础 Locomotion
- **THEN** 系统 MUST 继续暴露当前阶段
- **AND** SHOULD 暴露 UnityHFSM active state 或 active hierarchy path 以便定位阶段流转


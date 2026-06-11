## MODIFIED Requirements

### Requirement: UnityHFSM 基础 Locomotion 阶段机
系统 MUST 使用项目已安装的 UnityHFSM 管理基础 Locomotion 的 `Idle / MoveStart / MoveLoop / MoveStop` 阶段，并 MUST 保留当前基础移动阶段语义。阶段机 MUST NOT 把 `Walk` 或 `Run` 建模为逻辑状态；Walk/Run MUST 作为基础移动档位事实进入 pipeline、命令和动画上下文。

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
- **WHEN** `MoveStart` 阶段计时达到当前阶段退出事实
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
- **WHEN** 当前停止动画或停止时长允许阶段退出
- **THEN** 阶段机 MUST 切换到 `Idle`

#### Scenario: 停止期间重新移动
- **GIVEN** 当前阶段为 `MoveStop`
- **WHEN** 本帧重新存在移动意图
- **THEN** 阶段机 MUST 切换到 `MoveStart`
- **AND** MUST NOT 等待当前 WalkEnd 或 RunEnd 结束

#### Scenario: Walk/Run 不扩张逻辑阶段
- **WHEN** 普通移动选择 Walk 档位或按住 Run 输入选择 Run 档位
- **THEN** 阶段机 MUST 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** MUST NOT 输出 `WalkStart / WalkLoop / WalkEnd`
- **AND** MUST NOT 输出 `RunStart / RunLoop / RunEnd`

### Requirement: Locomotion Pipeline 接入 UnityHFSM
系统 MUST 将基础 Locomotion pipeline 的阶段来源切换为 UnityHFSM 适配器，同时 MUST 保持输入、相机相对方向、运动档位、运动命令、运动执行、动画表现和相机 Resolve 的既有顺序。

#### Scenario: Pipeline 顺序保持
- **WHEN** 基础 Locomotion pipeline 处理一帧输入
- **THEN** 系统 MUST 先生成输入快照
- **AND** MUST 再生成移动意图和 Walk/Run 档位
- **AND** MUST 再解析相机相对世界方向
- **AND** MUST 再推进 UnityHFSM Locomotion 阶段
- **AND** MUST 再构建携带档位的 `MovementCommand`
- **AND** MUST 再提交给运动执行端口
- **AND** MUST 再提交携带档位的 `MovementAnimationContext`
- **AND** MUST 最后完成相机 Resolve

#### Scenario: 运动命令继续使用 BasicMovementPhase
- **WHEN** UnityHFSM Locomotion 阶段机输出当前阶段
- **THEN** `MovementCommand` MUST 继续携带 `BasicMovementPhase`
- **AND** `MovementAnimationContext` MUST 继续携带 `BasicMovementPhase`
- **AND** Walk/Run MUST 作为单独档位事实携带，不得替代 phase

#### Scenario: Pipeline 不依赖具体运动实现
- **WHEN** 基础 Locomotion pipeline 执行移动
- **THEN** 系统 MUST 通过运动执行端口提交 `MovementCommand`
- **AND** pipeline MUST NOT 持有 `CharacterMotionDriver` 具体类型
- **AND** pipeline MUST NOT 调用 `CharacterController.Move`
- **AND** pipeline MUST NOT 调用 KCC API

### Requirement: 可替换输入端口
系统 MUST 通过基础 Locomotion 输入端口读取移动、视角和 Run 保持输入，使 `PlayerLocomotionController` 不直接依赖具体 `InputActionReference` 或键盘按键。

#### Scenario: Controller 只读取输入快照
- **WHEN** `PlayerLocomotionController` 执行一帧 tick
- **THEN** 它 MUST 从输入端口读取 `BasicLocomotionInputSnapshot` 或等价快照
- **AND** 快照 MUST 能表达 move、look 和 Run 保持事实
- **AND** controller MUST NOT 直接读取 `moveAction`、`lookAction` 或 `runAction`

#### Scenario: Input System 只存在于 adapter
- **WHEN** 当前实现需要使用 Unity Input System 读取 Shift 或等价 Run 输入
- **THEN** `InputActionReference` 或 `InputAction` MUST 只出现在输入 adapter 中
- **AND** `PlayerLocomotionController` MUST NOT 引用 `InputActionReference`
- **AND** `PlayerLocomotionController` MUST NOT 引用 `UnityEngine.InputSystem`
- **AND** controller MUST NOT 硬编码读取键盘 Shift

#### Scenario: 输入端口支持替换
- **WHEN** 后续接入输入服务、网络预测、回放或 AI 输入
- **THEN** 系统 MUST 能通过替换输入端口实现提供相同输入快照
- **AND** UnityHFSM 阶段机 MUST NOT 因输入来源替换而修改
- **AND** `BasicLocomotionPipeline` MUST NOT 因输入来源替换而修改

### Requirement: 可测试和可诊断
系统 MUST 为 UnityHFSM Locomotion 接入提供自动测试、静态边界验证和运行时可诊断信息。

#### Scenario: 自动测试覆盖四阶段
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 覆盖 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop` 的主要流转
- **AND** MUST 覆盖阶段计时门槛
- **AND** MUST 覆盖 `Reset`

#### Scenario: 自动测试覆盖 Walk/Run 档位
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 覆盖普通移动输入生成 Walk 档位
- **AND** MUST 覆盖按住 Run 输入生成 Run 档位
- **AND** MUST 覆盖 Run 输入单独存在时不产生移动意图
- **AND** MUST 覆盖 Walk/Run 不新增状态机 phase

#### Scenario: 自动测试覆盖停止档位
- **WHEN** 角色从 `MoveLoop` 进入 `MoveStop`
- **THEN** EditMode 测试 MUST 覆盖停止阶段使用最后有效移动档位
- **AND** MUST 覆盖 WalkEnd 和 RunEnd 的退出事实
- **AND** MUST 覆盖停止期间重新输入立即回到 `MoveStart`

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

#### Scenario: 当前阶段和档位可诊断
- **WHEN** 开发者调试当前基础 Locomotion
- **THEN** 系统 MUST 继续暴露当前阶段
- **AND** SHOULD 暴露当前 Walk/Run 档位
- **AND** SHOULD 暴露 UnityHFSM active state 或 active hierarchy path 以便定位阶段流转

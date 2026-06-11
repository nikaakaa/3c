## MODIFIED Requirements

### Requirement: UnityHFSM 基础 Locomotion 阶段机
系统 MUST 使用项目已安装的 UnityHFSM 管理基础 Locomotion 的 `Idle / MoveStart / MoveLoop / MoveStop` 阶段，并 MUST 保留当前基础移动阶段语义。当前阶段机 MUST NOT 把 `Run` 或 `Walk` 建模为逻辑状态。

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
- **WHEN** `MoveStop` 阶段计时达到当前停止退出时长
- **THEN** 阶段机 MUST 切换到 `Idle`

#### Scenario: 停止期间重新移动
- **GIVEN** 当前阶段为 `MoveStop`
- **WHEN** 本帧重新存在移动意图
- **THEN** 阶段机 MUST 立即切换到 `MoveStart`
- **AND** MUST NOT 等待当前停止退出时长完成

#### Scenario: 不把 Run 建模为逻辑状态
- **WHEN** 当前版本只实现 Run 基础移动动画
- **THEN** 阶段机 MUST 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`
- **AND** MUST NOT 新增 `RunStart / RunLoop / RunEnd` 作为逻辑状态
- **AND** MUST NOT 新增 `WalkStart / WalkLoop / WalkEnd` 作为逻辑状态

### Requirement: 可测试和可诊断
系统 MUST 为 UnityHFSM Locomotion 接入提供自动测试、静态边界验证和运行时可诊断信息。

#### Scenario: 自动测试覆盖四阶段
- **WHEN** 实施完成
- **THEN** EditMode 测试 MUST 覆盖 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop` 的主要流转
- **AND** MUST 覆盖阶段计时门槛
- **AND** MUST 覆盖 `Reset`

#### Scenario: 自动测试覆盖停止退出时长
- **WHEN** RunEnd stop exit duration 被配置
- **THEN** EditMode 测试 MUST 覆盖 `MoveStop` 未达到该时长时保持 `MoveStop`
- **AND** MUST 覆盖达到该时长后回到 `Idle`
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

#### Scenario: 当前阶段可诊断
- **WHEN** 开发者调试当前基础 Locomotion
- **THEN** 系统 MUST 继续暴露当前阶段
- **AND** SHOULD 暴露 UnityHFSM active state 或 active hierarchy path 以便定位阶段流转

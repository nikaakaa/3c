## Context
项目当前没有已归档的 OpenSpec spec，也没有 active change。代码侧存在几条相关但未统一的路径：

- `Assets/Scripts/Camera/ThirdPersonYawPitchCamera.cs` 已能读取 Look 输入并更新 `CameraTarget` yaw/pitch，但尚未明确作为 Cinemachine target 驱动层，也没有移动方向 provider 边界。
- `Assets/AnimancerController/Scripts/AnimancerController/Camera/CameraController.cs` 已依赖 `CinemachineVirtualCamera` 做距离调整，但绑定旧 `InputService`，不适合作为本次最小 WASD 主入口。
- 旧 `PlayerMovementState` 会读取旧 `InputService`、相机 Transform，并直接影响角色朝向和动画状态，范围超过最小 WASD。
- `Ref/BBB` 中已有 `BBBCharacterController` 与 `MotionDriver`，但用户已确认它不是当前正式聚合点，本变更不得直接复用该参考路径作为新 WASD 主线。

因此本变更先定义一个非常小的第三人称 WASD 闭环：Cinemachine 负责最终相机输出，控制层只更新目标和方向数据；WASD 只生成移动意图与移动命令；实际位移必须进入本变更内的项目自有 `CharacterMotionDriver`。

## Goals / Non-Goals
- Goals:
  - 在一名角色上完成最小 WASD 第三人称移动演示。
  - 使用 Cinemachine 基础 vcam 作为相机输出，不手写最终相机跟随插值。
  - 让移动方向来自相机平面 forward/right，而不是在移动脚本中读取 `Camera.main`。
  - 把输入、意图、方向解析、状态阶段和运动执行拆开。
  - 保留后续接入 Animancer、Root Motion、动作位移和网络预测的数据边界。
- Non-Goals:
  - 不做完整相机模式系统。
  - 不做动作系统、技能镜头和战斗反馈。
  - 不做动画表现和 Root Motion。
  - 不替换完整旧角色状态机。

## Decisions
- Decision: 第一版相机只支持 Free 第三人称基础模式。
  - Reason: 用户当前目标是最小 WASD 第三人称，锁定、射击、轨道和技能镜头会扩大 proposal 范围。
  - Alternative: 一次性做完整相机模式底座。任务会过大，也会在 WASD 可跑之前引入过多未验证抽象。

- Decision: 相机控制层只驱动 `CameraTarget` 和方向输出，最终画面交给 Cinemachine vcam。
  - Reason: 这样能避免控制层和 Cinemachine 双重 damping，也让后续镜头参数在 vcam 上配置。
  - Alternative: 手写 follow position 再交给 Cinemachine。短期可跑，但会形成第二套相机数学。

- Decision: WASD 移动核心不直接依赖 `Camera.main`、相机 Transform、Animancer 或旧 `InputService`。
  - Reason: 输入意图和方向解析需要可测，也要为以后输入历史、预测和回滚保留纯数据边界。
  - Alternative: 在 MonoBehaviour `Update` 中直接读输入、读相机、移动角色。实现更快，但后续难以讲清架构价值。

- Decision: 位移只允许进入本变更内的 `CharacterMotionDriver`。
  - Reason: 用户确认不直接复用 BBB 聚合点；本轮快速纵切先把 `CharacterController.Move` 收敛到一个项目自有运动出口内。
  - Alternative: WASD 脚本直接 `CharacterController.Move`。这会让组装层绕过运动出口，后续难以接 Root Motion 和动作位移。

- Decision: 最小状态阶段只表达逻辑阶段，不播放动画。
  - Reason: 本阶段目标是可跑的输入驱动基础移动，动画接入应在后续 proposal 中单独审批。
  - Alternative: 直接复用旧 `PlayerMoveStartState` / `PlayerMoveLoopState`。会把 Animancer、旧输入服务、动画事件和攀爬跳跃逻辑一起带入。

## Proposed Runtime Shape
- `ThirdPersonCameraController`
  - 读取 Look 输入。
  - 更新 yaw/pitch。
  - 移动并旋转标准 `CameraTarget`。
  - 暴露 `CameraPlanarForward`、`CameraPlanarRight`、`LookDirection`。
- `CinemachineThirdPersonRig`
  - 绑定一个基础 `CinemachineVirtualCamera`。
  - 将 vcam 的 Follow/LookAt 指向 `CameraTarget`。
  - 应用最小距离、高度、FOV、damping 和碰撞参数。
- `MovementInputIntent`
  - 保存原始输入、归一化输入、输入强度和是否有移动意图。
- `CameraRelativeMovementResolver`
  - 使用相机平面方向将输入转换为世界平面移动方向。
- `BasicMovementStateMachine`
  - 管理 `Idle / MoveStart / MoveLoop / MoveStop`。
- `MovementCommand`
  - 保存世界移动方向、目标速度、期望朝向、deltaTime 和阶段。
- `CharacterMotionDriver` 适配点
  - 第一版基础移动命令最终进入 `CharacterMotionDriver`。
  - 旧 `Player` 路径启用时，最小 WASD 入口自动停止，避免同一角色双移动入口。

## Update Order
1. Input System 更新 `Player/Move` 与 `Player/Look`。
2. 相机控制层更新 yaw/pitch 与 `CameraTarget`。
3. 移动输入层生成 `MovementInputIntent`。
4. 方向解析层读取 `CameraPlanarForward` / `CameraPlanarRight` 并生成世界方向。
5. 状态机更新移动阶段。
6. 命令构建层生成 `MovementCommand`。
7. `CharacterMotionDriver` 执行位移和朝向。

## Risks / Trade-offs
- Risk: 当前项目里存在旧 `CharacterBase` / `PlayerMovementState` 路径和参考 BBBNexus 路径，容易产生双入口。
  - Mitigation: 本变更只使用 `CharacterMotionDriver`；同一对象上检测到旧 `Player` 启用时停止最小 WASD 入口。
- Risk: 后续正式聚合点确定后，`CharacterMotionDriver` 可能需要迁入更完整的角色聚合模块。
  - Mitigation: 当前只保留最小命令数据边界，不把输入、相机和位移细节混进一个组件。
- Risk: 第一版无动画，演示观感朴素。
  - Mitigation: 本阶段只保证第三人称方向和位移手感明确；动画接入在后续 proposal 中做。

## Manual Verification Notes
交付实现时建议用户在 Unity 内手动验证：进入 Play Mode，移动鼠标旋转视角，按 W/A/S/D 确认角色按相机相对方向移动，松开输入后停止，确认场景中只有一个启用的 WASD 移动主路径。该手动验证不写入 `tasks.md` 阻塞项。

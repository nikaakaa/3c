## Context
当前最小第三人称 WASD 已经具备基础闭环：输入从 Unity Input System 读取，`MovementInputIntent` 表达移动意图，`CameraRelativeMovementResolver` 用相机平面方向算世界移动方向，`BasicMovementStateMachine` 推进 `Idle / MoveStart / MoveLoop / MoveStop`，`MovementCommandBuilder` 构建命令，`CharacterMotionDriver` 执行 `CharacterController.Move`，`BasicLocomotionAnimancerPresenter` 播放表现。

问题不是功能缺失，而是 `BasicWASDMovementController.Tick` 把这些步骤直接揉在一起。后续战斗视角、锁定、动作打断、输入缓存和动画参数扩展会继续挤进这个入口，导致每个能力都要碰同一个 MonoBehaviour。

## Goals
- 保留当前可玩的 WASD 闭环。
- 将主链整理为清晰的 pipeline 顺序。
- 让每一步的数据输入和输出可读、可替换、可验证。
- 保持 `CharacterMotionDriver` 是位移权威。
- 保持 `BasicLocomotionAnimancerPresenter` 是表现层。
- 保持 `ICameraMovementBasisProvider` 是移动系统读取相机方向的边界。

## Non-Goals
- 不建立完整 BBB 黑板系统。
- 不把 `BasicWASDMovementController` 替换成新的大型 `PlayerController`。
- 不新增未审批的动作状态机、战斗控制器或相机影响源实现。
- 不改变 Cinemachine FreeLook 的手动配置权。
- 不新增测试文件；本阶段按快速开发偏好使用编译、静态搜索和 Unity 手动验证。

## Pipeline Order
实施后的主链顺序 MUST 固定为：

1. 读取本帧输入快照。
2. 将 Move 输入转换为 `MovementInputIntent`。
3. 通过 `ICameraMovementBasisProvider` 解析世界平面移动方向。
4. 推进 `BasicMovementStateMachine` 得到 `BasicMovementPhase`。
5. 基于意图、阶段和配置构建 `MovementCommand`。
6. 将命令提交给 `CharacterMotionDriver`。
7. 读取运动结果并构建 `MovementAnimationContext`。
8. 将表现上下文提交给 `BasicLocomotionAnimancerPresenter`。
9. 触发项目侧相机 Resolve，但不覆盖 FreeLook 手动配置。

## Boundaries

### Input Snapshot
输入快照只表达本帧 Move/Look 原始值和 deltaTime。它不依赖角色 Transform、Animancer 或 Cinemachine。

### Intent Processor
移动意图处理只负责死区、强度和移动意图存在性。第一版不做输入缓存、冲刺、走路、闪避或跳跃。

### Camera-Relative Resolver
相机相对移动只消费 `ICameraMovementBasisProvider.CameraPlanarForward` 和 `CameraPlanarRight`。移动逻辑不得直接读取 `Camera.main`、`CinemachineFreeLook` 或相机 Transform。

### Runtime Locomotion Data
主链可以引入一个轻量运行时数据容器，用来承载本帧输入、意图、世界方向、阶段、命令和运动结果。它只为整理数据流服务，不成为全局黑板，也不对外开放随意写入。

### Motion Authority
`CharacterMotionDriver` 继续是基础 WASD 位移权威。`CharacterController.Move` 只允许在它内部调用。若实现发现必须让其它类移动角色，必须停止并回到 OpenSpec 审批。

### Animation Presenter
`BasicLocomotionAnimancerPresenter` 只消费 `MovementAnimationContext`，并通过 Animancer 播放当前阶段表现。它不得写角色位置，不得调用 `CharacterController.Move`，不得拥有移动阶段真相。

### Camera Resolve
WASD 主链可以把 Look 输入交给项目相机入口，并在移动后触发 Resolve。该过程不得在运行时重写用户手动调好的 FreeLook 轨道、Follow、LookAt、轴范围或阻尼配置。

## BBB Reference
参考 BBB 的三个原则：

- `InputPipeline` 是输入快照生产者。
- `LocomotionIntentProcessor` 将输入转成世界移动意图。
- `MovementParameterProcessor` 从运行时数据推导动画表现参数。
- `MotionDriver` 保持实际运动权威。

本项目不直接复制 BBB 的类和黑板。当前目标只是让现有 WASD 主链具备同样清晰的职责顺序。

## Risks / Trade-offs
- 过度拆分类会让当前 demo 变重。缓解方式：只抽出当前确实需要命名和复用的步骤，优先保留已有模型和 solver。
- 只做手动验证可能漏掉边界回归。缓解方式：任务里加入静态搜索和编译检查；如果后续能力扩大，再补 EditMode 测试。
- `standardize-basic-locomotion-animation-config` 仍含旧动画表规划。缓解方式：本变更明确不恢复 `BasicLocomotionAnimationConfigSO`，实施时按当前代码事实走。

## Stop Conditions
实施时若出现以下情况 MUST 停止并重新提案：

- 需要新增绕过 `BasicWASDMovementController` 和 `CharacterMotionDriver` 的角色移动主路径。
- 需要让 Root Motion 接管基础 WASD 位移。
- 需要运行时强制改写 Cinemachine FreeLook 的手动配置。
- 需要引入完整 BBB 黑板、体力、动作、锁定或战斗状态机。
- 需要恢复或新增独立运行时动画配置表。

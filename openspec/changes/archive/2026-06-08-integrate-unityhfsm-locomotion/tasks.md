## 1. 准备与边界确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 读取 `docs/agents/unityhfsm-usage-guide.md`，确认 UnityHFSM 使用约束。
- [x] 1.3 读取 `refactor-wasd-to-locomotion-pipeline` 的 proposal/design/spec，确认不破坏既有 pipeline 顺序。
- [x] 1.4 搜索 `BasicWASDMovementController`、`BasicMovementStateMachine`、`BasicMovementPhase` 引用，记录需要迁移的位置。
- [x] 1.5 搜索 `InputActionReference`、`moveAction`、`lookAction`、`BasicLocomotionInputSnapshot` 引用，确认当前输入读取边界。
- [x] 1.6 搜索 `CharacterMotionDriver`、`CharacterController.Move` 引用，确认当前运动执行边界。
- [x] 1.7 搜索 `com.inspiaaa.unityhfsm` manifest/package 状态，确认 UnityHFSM 已安装。
- [x] 1.8 读取 BBB 的 `BBBCharacterController`、`InputPipeline`、`MotionDriver`、`PlayerStateRegistry`、`GlobalInterruptProcessor`，只记录可参考边界，不复制运行时代码。
- [x] 1.9 若实施需要绕过输入端口、动画外观层、相机边界、motion executor 端口或新增完整角色主控，停止并回到 OpenSpec。

## 2. 命名和 namespace 收敛
- [x] 2.1 新增或重命名运行时入口为 `PlayerLocomotionController`。
- [x] 2.2 确认 `PlayerLocomotionController` 使用现有 `ThirdPersonMovement` 命名空间。
- [x] 2.3 删除 `BasicWASDMovementController` 运行时类名，不新增兼容包装。
- [x] 2.4 迁移 scene/prefab 上旧组件引用到 `PlayerLocomotionController`。
- [x] 2.5 更新测试和新代码，统一引用 `PlayerLocomotionController`。
- [x] 2.6 保留旧日志语义或提供等价新日志，不主动删除现有 log。
- [x] 2.7 静态搜索确认新运行时代码中没有 `BasicWASDMovementController`。
- [x] 2.8 静态搜索确认没有新增 BBB、KCC sample 或临时 movement namespace。

## 3. Input source 端口
- [x] 3.1 定义基础 Locomotion 输入端口，例如 `IBasicLocomotionInputSource` 或等价名称。
- [x] 3.2 端口暴露读取 `BasicLocomotionInputSnapshot` 或等价快照的方法。
- [x] 3.3 快照继续携带 `Move`、`Look` 和 `DeltaTime`。
- [x] 3.4 将 `PlayerLocomotionController` 的输入读取目标改为该端口。
- [x] 3.5 新增 Unity Input System adapter，在 adapter 内持有 `InputActionReference`。
- [x] 3.6 将 `moveAction`、`lookAction` 和 action enable/disable 逻辑从 `PlayerLocomotionController` 移到 Input System adapter。
- [x] 3.7 确认 `PlayerLocomotionController` 不引用 `InputActionReference`。
- [x] 3.8 确认 `PlayerLocomotionController` 不引用 `UnityEngine.InputSystem`。
- [x] 3.9 准备 fake input source，供 EditMode 测试和后续预测/回放替换使用。

## 4. Motion executor 端口
- [x] 4.1 定义基础 Locomotion 运动执行端口，例如 `IBasicLocomotionMotionExecutor` 或等价名称。
- [x] 4.2 端口暴露执行 `MovementCommand` 的方法。
- [x] 4.3 端口暴露 `CurrentSpeed` 只读诊断信息。
- [x] 4.4 端口暴露 `LastWorldDirection` 或等价只读诊断信息。
- [x] 4.5 将 `PlayerLocomotionController` 的运动提交目标改为该端口。
- [x] 4.6 保持当前 CharacterController 执行实现可用。
- [x] 4.7 若保留 `CharacterMotionDriver : MonoBehaviour`，确认它只作为端口适配器，不承载状态规则。
- [x] 4.8 若拆出普通 C# executor，确认 MonoBehaviour 只负责 Unity 组件绑定和委托。
- [x] 4.9 确认 pipeline 和 UnityHFSM 状态机不引用 `CharacterMotionDriver` 具体类型。
- [x] 4.10 确认 pipeline 和 UnityHFSM 状态机不引用 `CharacterController` 或 `KinematicCharacterMotor`。

## 5. UnityHFSM Locomotion 阶段适配器
- [x] 5.1 新增或重写基础 Locomotion 阶段状态机类型，内部使用 `UnityHFSM.StateMachine<BasicMovementPhase>`。
- [x] 5.2 显式注册 `Idle`、`MoveStart`、`MoveLoop`、`MoveStop` 四个状态。
- [x] 5.3 显式设置 `Idle` 为 start state。
- [x] 5.4 保留 `Phase` 只读属性。
- [x] 5.5 保留 `PhaseTime` 或等价阶段计时输出。
- [x] 5.6 暴露 `ActivePath` 或等价 UnityHFSM 诊断路径。
- [x] 5.7 保留 `Reset()`，确保状态回到 `Idle` 且阶段计时清零。
- [x] 5.8 保留 `Tick(bool hasMoveIntent, float deltaTime, in BasicMovementSettings settings)` 或等价 API，降低 pipeline 迁移范围。
- [x] 5.9 确认适配器不依赖 Animancer、CharacterController、KCC、Camera、Cinemachine 或 InputAction。

## 6. 阶段转移语义
- [x] 6.1 实现 `Idle -> MoveStart`：有移动意图时切换。
- [x] 6.2 实现 `MoveStart -> MoveStop`：起步期间失去移动意图时切换。
- [x] 6.3 实现 `MoveStart -> MoveLoop`：持续移动且达到 `MoveStartMinTime` 后切换。
- [x] 6.4 实现 `MoveLoop -> MoveStop`：失去移动意图时切换。
- [x] 6.5 实现 `MoveStop -> MoveStart`：停止期间重新输入移动时切换。
- [x] 6.6 实现 `MoveStop -> Idle`：无移动意图且达到 `MoveStopMinTime` 后切换。
- [x] 6.7 确认负 deltaTime 按 0 处理，保持旧语义。
- [x] 6.8 确认阶段切换时 `PhaseTime` 重置。

## 7. Pipeline 接入
- [x] 7.1 将 `BasicLocomotionPipeline` 的阶段来源切到 UnityHFSM 适配器。
- [x] 7.2 保持输入快照、移动意图、相机相对方向、运动命令、动画上下文构建顺序不变。
- [x] 7.3 保持 `MovementCommandBuilder.Build` 的输入不变。
- [x] 7.4 保持 `MovementAnimationContext` 使用 `BasicMovementPhase`。
- [x] 7.5 确认基础移动速度、旋转速度和 deltaTime 计算不变。
- [x] 7.6 确认 `PlayerLocomotionController` 只按顺序协调 input source、pipeline、motion executor、Presenter 和 Camera Resolve。
- [x] 7.7 确认 `PlayerLocomotionController` 不直接维护阶段 switch。
- [x] 7.8 确认 `PlayerLocomotionController` 不直接调用 `CharacterController.Move` 或 KCC API。

## 8. BBB 参考边界
- [x] 8.1 记录 BBB 根 MonoBehaviour 组装点、`InputPipeline` 和普通 C# `MotionDriver` 的参考价值。
- [x] 8.2 记录 BBB 状态内部直接 `ChangeState(GetState<T>())` 的耦合风险。
- [x] 8.3 确认没有引用 `BBBNexus` 命名空间或 `Ref/BBB` 运行时类型。
- [x] 8.4 确认没有复制完整 `BBBCharacterController`、`PlayerBrainSO`、`PlayerBaseState`、`PlayerStateRegistry`、`InputPipeline` 或 Interceptor 主线。
- [x] 8.5 确认状态切换规则集中在 UnityHFSM transition/adapter 中，而不是散落在多个状态类直接互跳。

## 9. EditMode 测试
- [x] 9.1 新增 UnityHFSM Locomotion 状态机初始化测试，验证 start state 为 `Idle`。
- [x] 9.2 测试 `Idle -> MoveStart`。
- [x] 9.3 测试 `MoveStart -> MoveLoop` 的最小时长门槛。
- [x] 9.4 测试 `MoveStart -> MoveStop`。
- [x] 9.5 测试 `MoveLoop -> MoveStop`。
- [x] 9.6 测试 `MoveStop -> Idle` 的最小时长门槛。
- [x] 9.7 测试 `MoveStop -> MoveStart` 的重新输入。
- [x] 9.8 测试 `Reset()` 回到 `Idle` 并清零计时。
- [x] 9.9 测试 `ActivePath` 或等价诊断路径。
- [x] 9.10 测试 `BasicLocomotionPipeline` 使用新状态机后仍输出正确 `MovementCommand` 和 `MovementAnimationContext` 所需阶段。
- [x] 9.11 使用 fake input source 测试 `PlayerLocomotionController` 可被输入端口驱动。
- [x] 9.12 使用 fake motion executor 测试 `PlayerLocomotionController` 可向端口提交运动命令。
- [x] 9.13 测试 input source 端口不会要求 controller 持有具体 `InputActionReference`。
- [x] 9.14 测试 motion executor 端口不会要求 pipeline 持有具体 `CharacterMotionDriver` MonoBehaviour。

## 10. 自动验证
- [x] 10.1 使用 Unity MCP 运行新增/相关 EditMode 测试。
- [x] 10.2 运行 `openspec validate integrate-unityhfsm-locomotion --strict --no-interactive`。
- [x] 10.3 静态搜索确认 `BasicWASDMovementController` 不出现在新运行时代码中。
- [x] 10.4 静态搜索确认 `PlayerLocomotionController` 不引用 `InputActionReference` 或 `UnityEngine.InputSystem`。
- [x] 10.5 静态搜索确认 `CharacterController.Move` 只在当前 CharacterController executor/adapter 中出现。
- [x] 10.6 静态搜索确认基础移动状态机不引用 Animancer、Camera、Cinemachine、CharacterController、KCC 或 InputAction。
- [x] 10.7 静态搜索确认没有新增 BBB 运行时依赖。
- [x] 10.8 记录验证输出中的错误、警告和已知历史警告。

## 11. Unity 手动验证
- [x] 11.1 打开当前演示场景并进入 Play Mode。
- [x] 11.2 按 W/A/S/D，确认角色仍按 FreeLook 平面方向移动。
- [x] 11.3 转动 Look 输入，确认移动方向跟随相机平面方向。
- [x] 11.4 确认角色朝移动方向旋转。
- [x] 11.5 确认 Idle、MoveStart、MoveLoop、MoveStop 动画表现仍触发。
- [x] 11.6 确认重命名后 prefab/scene 上 `PlayerLocomotionController` 组件引用有效。
- [x] 11.7 确认当前 Input System adapter 可驱动 `PlayerLocomotionController`。
- [x] 11.8 检查 FreeLook 手动配置没有被运行时代码覆盖。

## 12. 收尾
- [x] 12.1 更新任务清单为真实完成状态。
- [x] 12.2 向用户说明实际改动文件。
- [x] 12.3 向用户说明自动验证和 Unity MCP 测试结果。
- [x] 12.4 向用户说明 Unity 手动验证步骤和结果。

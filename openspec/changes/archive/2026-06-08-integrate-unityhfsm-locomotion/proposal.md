# Change: 接入 UnityHFSM 并收敛 Player Locomotion 主链

## Why
当前基础移动主链已经从最初的 WASD demo 整理成 `BasicLocomotionPipeline`，但阶段推进仍由项目自研的 `BasicMovementStateMachine` 手写维护。后续要继续加入起步、停止、跳跃、闪避、动作接管或更复杂的状态层级时，继续扩展这个手写 FSM 会重复造轮子，也会和已经安装的 UnityHFSM 产生两条状态机路线。

当前 `BasicWASDMovementController` 和 `CharacterMotionDriver` 都是临时演示阶段产物。入口命名应直接收敛为 `PlayerLocomotionController`，旧 WASD 命名不再保留兼容包装。运动执行也不应长期绑定到一个业务 MonoBehaviour；BBB 的可取之处是根 MonoBehaviour 负责 Unity 绑定，`MotionDriver` 是普通 C# 对象，但 BBB 状态内部大量 `ChangeState(GetState<T>())` 和对 `BBBCharacterController` 的直接持有不适合作为本项目主线。

本变更准备把当前 Idle / MoveStart / MoveLoop / MoveStop 阶段推进迁移到项目已安装的 `com.inspiaaa.unityhfsm`，同时把输入读取和运动执行都拆成可替换端口。当前仍可用 Unity Input System 和 `CharacterController` 实现这些端口，但 `PlayerLocomotionController`、pipeline 和状态机不能知道具体 `InputActionReference`、`CharacterController`、`CharacterMotionDriver` MonoBehaviour 或未来 KCC 实现。

## What Changes
- 使用 UnityHFSM 作为基础 Locomotion 阶段状态机内核，替代 `BasicMovementStateMachine` 的手写 switch。
- 将 `BasicWASDMovementController` 直接迁移为 `PlayerLocomotionController`，不保留旧类名兼容包装。
- 保持 movement 相关运行时代码统一在现有 `ThirdPersonMovement` 命名空间内，不新开 BBB、KCC sample 或临时 namespace。
- 保留现有 `BasicMovementPhase`、`MovementCommand`、`MovementAnimationContext` 等数据边界，避免一次性扩大业务范围。
- 引入或明确基础 Locomotion 输入端口，让 `PlayerLocomotionController` 只读取输入快照，不直接序列化或读取每个 `InputActionReference`。
- 保持 `BasicLocomotionPipeline` 的输入快照、移动意图、相机相对方向、阶段推进、运动命令、动画表现和相机 Resolve 顺序。
- 引入或明确基础 Locomotion 的运动执行端口，让当前 `CharacterController` 实现和未来 KCC 实现都只作为端口适配器。
- 将 `CharacterMotionDriver` 从长期业务 MonoBehaviour 定位中移出；实施时优先迁移为普通 C# motion executor，或仅保留 Unity 适配器职责且不承载状态规则。
- 参考 BBB 的根节点组装、运行时数据、运动驱动分离和全局优先级思想，但不复制 BBB 类型，也不复制状态内部互相硬跳的写法。

## Non-Goals
- 不接入跳跃、闪避、翻滚、飞檐走壁、翻越、攻击、锁定、瞄准或完整角色状态机。
- 不接入 KCC 包或 KCC sample 运行时路径；本次只预留可替换运动端口。
- 不引入 BBB 的 `BBBCharacterController`、`PlayerStateRegistry`、`PlayerBaseState`、`PlayerBrainSO` 或 Interceptor 运行时依赖。
- 不删除现有自研 `Assets/Scripts/FSM/Core/HFSM.cs`，除非用户后续明确要求清理。
- 不让状态直接调用 Animancer 细节、`CharacterController.Move`、`KinematicCharacterMotor`、`Camera.main` 或 `CinemachineFreeLook`。
- 不让 `PlayerLocomotionController` 直接持有每个输入动作引用；Unity Input System 只能作为可替换输入 adapter。
- 不改变当前运动速度、死区、MoveStart/MoveStop 最小时长和动画表现语义。
- 不新增 Root Motion 驱动基础移动。

## Impact
- Affected specs: `unityhfsm-locomotion`
- Related changes:
  - `refactor-wasd-to-locomotion-pipeline`
  - `add-minimal-third-person-wasd`
  - `add-basic-locomotion-animation`
  - `add-pure-hfsm-core`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/BasicWASDMovementController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/CharacterMotionDriver.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver/BasicMovementStateMachine.cs`
  - 可能新增 `PlayerLocomotionController`
  - 可能新增基础 Locomotion input source/adapter 类型
  - 可能新增 `BasicLocomotionStateMachine` 或等价 UnityHFSM 适配器
  - 可能新增基础 Locomotion motion executor/port 类型
  - 可能新增/调整 EditMode tests
  - 需要迁移 scene/prefab 中旧组件类型引用
- Validation:
  - Unity EditMode 测试覆盖 Idle、MoveStart、MoveLoop、MoveStop 阶段流转。
  - Unity EditMode 测试覆盖 UnityHFSM 初始化、路径/当前阶段和时间门槛。
  - Unity EditMode 测试覆盖输入端口边界，确认 controller 可用 fake input source 驱动。
  - Unity EditMode 测试覆盖 motion executor 端口边界，确认 pipeline 不依赖具体 `CharacterController` 或 KCC 实现。
  - 静态搜索确认 `PlayerLocomotionController` 不直接引用 `InputActionReference` 或 `UnityEngine.InputSystem`。
  - 静态搜索确认基础移动状态机不直接依赖 Animancer、CharacterController、KCC、Camera 或 Cinemachine。
  - 静态搜索确认新运行时代码不再引用 `BasicWASDMovementController`。
  - Unity MCP 跑定向 EditMode 测试。
  - 手动验证当前演示场景 WASD/Look/动画表现不回退。

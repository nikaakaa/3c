# Change: 将 WASD 主链重构为最小 Locomotion Pipeline

## Why
当前 `BasicWASDMovementController` 在同一个 `Tick` 里同时承担输入读取、Look 转发、移动意图生成、相机相对方向解析、阶段推进、运动命令构建、位移执行、动画上下文提交和相机 Resolve。这个入口已经能跑通演示，但职责混在一起，后续接战斗、锁定、瞄准或动作打断时容易在同一个 MonoBehaviour 里继续堆逻辑。

BBB 的价值不在于直接复制 `BBBCharacterController`，而是它把输入快照、意图、运行时数据、运动权威和动画参数拆成顺序明确的处理链。本变更只吸收这个分层思路，把现有 WASD 闭环整理成项目侧最小 locomotion pipeline。

## What Changes
- 保留 `BasicWASDMovementController` 作为当前演示角色的临时主调度入口，但让它只负责按固定顺序协调 pipeline。
- 拆清输入快照、移动意图、世界方向、阶段、运动命令、运动结果和动画上下文之间的边界。
- 继续使用 `ICameraMovementBasisProvider` 作为相机相对移动的唯一相机方向接口，不让移动逻辑直接读取 `Camera.main`、FreeLook 或场景相机 Transform。
- 继续让 `CharacterMotionDriver` 作为 `CharacterController.Move` 的唯一位移权威。
- 继续让 `BasicLocomotionAnimancerPresenter` 只消费移动表现上下文，不接管位移，不维护独立移动状态。
- 不恢复 `BasicLocomotionAnimationConfigSO` 这类额外动画表；当前阶段动画配置由 Animancer/Presenter 自身序列化字段承接，后续编辑器工具另行规划。
- 保留现有调试日志，除非后续用户明确要求删除。

## Non-Goals
- 不复制 BBB 的完整 `BBBCharacterController`、黑板、体力、翻滚、闪避、跳跃、瞄准、战斗或网络预测结构。
- 不新增第二套角色控制器，不绕过现有 `CharacterMotionDriver`。
- 不引入 Root Motion 驱动基础 WASD 位移。
- 不在运行时初始化或 Tick 中覆盖用户手动调好的 Cinemachine FreeLook 轨道、Follow、LookAt、轴配置。
- 不新增 `BasicLocomotionAnimationConfigSO` 或类似运行时动画表。
- 不做 walk/jog/sprint、锁定移动、动作取消窗口、脚步相位、motion warping 等后续能力。

## Impact
- Affected specs: `wasd-locomotion-pipeline`
- Related changes: `add-minimal-third-person-wasd`, `add-basic-locomotion-animation`, `refactor-camera-to-cinemachine-freelook`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/BasicWASDMovementController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/CharacterMotionDriver.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Model/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver/*`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation/Runtime/BasicLocomotionAnimancerPresenter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Camera/Contracts/ICameraMovementBasisProvider.cs`
- Validation:
  - 使用现有编译检查验证代码可编译。
  - 使用静态搜索确认 `CharacterController.Move` 仍只在 `CharacterMotionDriver` 内部出现。
  - 使用静态搜索确认移动 pipeline 不直接读取 `Camera.main`、`CinemachineFreeLook` 或场景相机 Transform。
  - 通过 Unity 手动验证 WASD、FreeLook 相对方向、角色朝向和 Idle/MoveStart/MoveLoop/MoveStop 表现。

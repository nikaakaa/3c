# Change: 接入 Simulation Tick 驱动基础 Locomotion

## Why
`add-simulation-tick-system` 已经提供项目级 tick core、`UnitySimulationTickDriver` 和输入缓冲 tick 接点，但当前角色移动仍由 `PlayerLocomotionController.Update()` 每 Unity frame 直接驱动。这样 tick 系统只是地基，Locomotion 逻辑还没有真正进入固定 simulation tick。

本变更规划把当前基础移动主线接到 simulation tick runner：由场景中的 `UnitySimulationTickDriver` 产生固定 tick，再通过一个薄的 Locomotion tick adapter 调用现有 `PlayerLocomotionController.Tick`。重点是接入现有主线，不新增第二套移动控制器，并防止 `Update()` 与 tick driver 双驱动。

## What Changes
- 为 `PlayerLocomotionController` 增加可关闭自动 Unity frame `Update` 驱动的开关或等价机制。
- 新增 Locomotion tick adapter，注册到 `SimulationTickPhase.ExecuteMotion` 或等价 phase，按 simulation tick 调用现有 `PlayerLocomotionController.Tick`。
- adapter 使用现有 `IBasicLocomotionInputSource` 读取 Move/Look，并使用 tick context 的 fixed delta 生成 `BasicLocomotionInputSnapshot`。
- `UnitySimulationTickDriver` 继续只负责 delta accumulation 和 runner 调度，不直接引用 Locomotion 具体实现。
- 在 `Sandbox` 或当前演示场景中挂接一个场景级 tick driver，并把当前角色 Locomotion 接入该 driver。
- 保持当前 `PlayerLocomotionController -> BasicLocomotionPipeline -> IBasicLocomotionMotionExecutor -> BasicLocomotionAnimancerPresenter` 主线不变。
- 增加测试证明 frame `Update` 与 simulation tick 不会同时驱动同一 Locomotion。

## Non-Goals
- 不实现预测回滚、快照历史、状态校正或服务端权威模拟。
- 不修改 Fantasy proto、协议导出工具或真实网络发包流程。
- 不接入离散攻击/闪避/跳跃/交互输入缓冲的运行时消费。
- 不改变 `BasicLocomotionPipeline` 的移动意图、相机相对方向、状态阶段或运动命令语义。
- 不实现 `add-locomotion-state-graph-config` 中的状态图配置化。
- 不新增第二套 player controller、第二套 movement pipeline 或第二套 animation presenter。
- 不删除现有 debug log。

## Impact
- Affected specs: `simulation-tick-locomotion`
- Related changes:
  - `add-simulation-tick-system`
  - `integrate-unityhfsm-locomotion`
  - `add-local-preinput-buffer`
  - `add-locomotion-state-graph-config`
- Affected code/assets:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Runtime/PlayerLocomotionController.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Runtime/UnitySimulationTickDriver.cs`
  - 可能新增 `LocomotionTickAdapter` 或等价 runtime adapter
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/PlayerLocomotionControllerTests.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/SimulationTickSystemTests.cs` 或新增 tick/locomotion 接入测试
  - `3cDemo/Client/3C_Client/Assets/Scenes/Sandbox.unity` 或当前演示场景
- Validation:
  - `openspec validate integrate-simulation-tick-locomotion --strict --no-interactive`
  - Unity EditMode 测试覆盖 tick adapter 调用、fixed delta 输入、自动 Update 关闭、防双驱动
  - 静态搜索确认没有新增第二套 player controller、没有修改 Fantasy proto、没有 tick core 反向依赖 Locomotion
  - Play Mode 手动验证 WASD、Look、Idle、MoveStart、MoveLoop、MoveStop 行为不回退

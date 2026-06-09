## 1. 准备与边界确认
- [x] 1.1 读取本 change 的 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 1.2 读取 `openspec/project.md`，确认 OpenSpec、测试和边界约定。
- [x] 1.3 读取 `add-simulation-tick-system` 归档后的 `simulation-tick-system` spec，确认 tick core 已完成内容。
- [x] 1.4 读取 `integrate-unityhfsm-locomotion` 归档后的 `unityhfsm-locomotion` spec，确认当前 Locomotion 主线。
- [x] 1.5 读取 `add-locomotion-state-graph-config`，确认并行变更不会被本变更实现。
- [x] 1.6 搜索 `PlayerLocomotionController.Update`，确认当前 frame 驱动入口。
- [x] 1.7 搜索 `UnitySimulationTickDriver`，确认当前 driver 未接 Locomotion。
- [x] 1.8 搜索 `SimulationTickPhase.ExecuteMotion`，确认当前没有 Locomotion handler。
- [x] 1.9 检查 `Sandbox.unity` 当前绑定，记录 PlayerLocomotionController 所在对象。
- [x] 1.10 未发现需要修改协议、rollback、状态图配置或第二控制入口的情况。

## 2. PlayerLocomotionController 自动 Update 开关
- [x] 2.1 新增 `autoUpdate` 或等价序列化字段，默认保持 true。
- [x] 2.2 暴露只读/可设置属性，供 adapter 或测试关闭自动 Update。
- [x] 2.3 `Update()` 在 `autoUpdate=false` 时直接返回。
- [x] 2.4 `autoUpdate=false` 时不读取 input source。
- [x] 2.5 `autoUpdate=false` 时不提交 motion executor。
- [x] 2.6 `autoUpdate=true` 时保持现有 frame Update 行为。
- [x] 2.7 不删除现有 debug camera log。

## 3. Locomotion tick adapter
- [x] 3.1 新增 `LocomotionTickAdapter` 或等价组件。
- [x] 3.2 adapter 持有 `UnitySimulationTickDriver` 引用。
- [x] 3.3 adapter 持有 `PlayerLocomotionController` 引用。
- [x] 3.4 adapter 实现 `ISimulationTickPhaseHandler`。
- [x] 3.5 adapter 在启用时注册到 `SimulationTickPhase.ExecuteMotion`。
- [x] 3.6 adapter 在禁用时反注册。
- [x] 3.7 adapter 可自动查找同对象或父对象/子对象上的 driver 和 controller，但不得全局乱找。
- [x] 3.8 adapter 接管时关闭 controller 自动 Update。
- [x] 3.9 adapter 释放时根据策略恢复或保持 controller 自动 Update，策略已有测试覆盖。
- [x] 3.10 adapter 不直接调用 `BasicLocomotionPipeline`。
- [x] 3.11 adapter 不直接调用 motion executor。
- [x] 3.12 adapter 不直接播放 Animancer。

## 4. Tick 输入快照
- [x] 4.1 adapter 在 tick phase 中读取 controller 的现有 input source。
- [x] 4.2 读取输入时使用 `SimulationTickContext.FixedDeltaSecondsFloat`。
- [x] 4.3 构造或获得 `BasicLocomotionInputSnapshot` 后调用 `PlayerLocomotionController.Tick`。
- [x] 4.4 测试 fake input source 收到 fixed delta。
- [x] 4.5 测试 frame delta 不影响 tick adapter 的 fixed delta。
- [x] 4.6 保持 `PlayerLocomotionController` 不引用 `InputActionReference`。

## 5. UnitySimulationTickDriver 接入边界
- [x] 5.1 保持 `UnitySimulationTickDriver` 不直接引用 `PlayerLocomotionController`。
- [x] 5.2 保持 `UnitySimulationTickDriver` 不直接引用 Locomotion 命名空间。
- [x] 5.3 确认 driver 可以通过 runner 调度 adapter。
- [x] 5.4 未新增 driver 专属 Locomotion 诊断；driver 仍保持通用边界。
- [x] 5.5 不让 driver 负责输入读取、运动执行或动画表现。

## 6. 场景接入
- [x] 6.1 在 `Sandbox` 或当前演示场景增加一个明确的 `UnitySimulationTickDriver` 组件。
- [x] 6.2 将 driver 设置为自动运行。
- [x] 6.3 在当前角色或合适组装对象上增加 `LocomotionTickAdapter`。
- [x] 6.4 绑定 adapter 到场景 driver；已修复 `Sandbox` 中 `LocomotionTickAdapter.tickDriver` 为空导致注册失败的问题。
- [x] 6.5 绑定 adapter 到当前 `PlayerLocomotionController`。
- [x] 6.6 确认 controller 自动 Update 被禁用或由 adapter 接管。
- [x] 6.7 保存场景变更。
- [x] 6.8 检查没有第二个 tick driver 同时驱动同一角色。

## 7. EditMode 自动测试
- [x] 7.1 测试 `PlayerLocomotionController` 默认 `autoUpdate=true`。
- [x] 7.2 测试默认 frame Update 仍读取输入并执行 motion。
- [x] 7.3 测试 `autoUpdate=false` 后 frame Update 不读取输入。
- [x] 7.4 测试 `autoUpdate=false` 后 frame Update 不执行 motion。
- [x] 7.5 测试 adapter 注册到 `ExecuteMotion` phase 后 tick 调用 controller。
- [x] 7.6 测试 adapter 使用 fixed delta 读取输入。
- [x] 7.7 测试 driver emitted 多个 tick 时 Locomotion 被调用多次。
- [x] 7.8 测试 adapter 禁用后不再响应 runner。
- [x] 7.9 测试 adapter 接管时避免双驱动。
- [x] 7.10 测试 adapter 不需要真实 scene input source。
- [x] 7.11 增加 `Sandbox` 场景序列化回归测试，确认 adapter 引用场景 `UnitySimulationTickDriver`、driver 自动运行且 controller 关闭自动 Update。

## 8. 静态验证
- [x] 8.1 搜索 tick core 不引用 `ThirdPersonMovement`。
- [x] 8.2 搜索 tick core 不引用 Animancer。
- [x] 8.3 搜索 tick core 不引用 Cinemachine。
- [x] 8.4 搜索 tick core 不引用 `CharacterController`。
- [x] 8.5 搜索 tick driver 不直接引用 `PlayerLocomotionController`。
- [x] 8.6 搜索没有新增第二套 player controller。
- [x] 8.7 搜索本变更未修改 Fantasy proto。
- [x] 8.8 搜索本变更未新增 rollback runtime。

## 9. Unity 编译与测试
- [x] 9.1 请求 Unity 刷新和脚本编译。
- [x] 9.2 检查 Console error。
- [x] 9.3 运行 tick/locomotion 接入定向 EditMode 测试；本轮新增场景绑定回归测试后因 Unity MCP 未连接，仍需重新运行 Unity EditMode 复验。
- [x] 9.4 运行现有 `PlayerLocomotionControllerTests`。
- [x] 9.5 运行现有 `SimulationTickSystemTests`。
- [x] 9.6 运行 `openspec validate integrate-simulation-tick-locomotion --strict --no-interactive`。
- [ ] 9.7 本轮修复 `Sandbox` 场景 driver 空引用并新增回归测试后，重新运行 Unity EditMode 测试复验。

## 10. Play Mode 烟测与用户复验
- [x] 10.1 确认当前活动场景为 `Sandbox`。
- [x] 10.2 进入 Play Mode。
- [x] 10.3 退出 Play Mode，不留下编辑器运行中。
- [x] 10.4 通过编辑器日志计数确认 Console errors=0、warnings=0。
- [x] 10.5 提供用户复验步骤：在 `Sandbox` 进入 Play Mode 后按 W/A/S/D，确认移动方向、速度和停止行为无回退。
- [x] 10.6 提供用户复验步骤：停止移动，确认 MoveStop 和 Idle 回退正常。
- [x] 10.7 提供用户复验步骤：移动鼠标或摇杆 Look，确认相机行为无回退。
- [x] 10.8 提供用户复验步骤：观察 Idle、MoveStart、MoveLoop、MoveStop 动画表现无回退。
- [x] 10.9 提供用户复验步骤：确认 Console 无双驱动 error 或 missing reference error。

## 11. 收尾
- [x] 11.1 更新任务清单为真实完成状态。
- [x] 11.2 记录自动测试结果：EditMode `ThirdPersonMovement.Tests.PlayerLocomotionControllerTests` 和 `ThirdPersonSimulation.Tests.SimulationTickSystemTests` 曾通过，41 passed / 0 failed；本轮修复场景 driver 空引用并新增回归测试后，Unity MCP 未连接，需重新运行 Unity EditMode 复验。
- [x] 11.3 记录静态验证结果：tick core/driver 未反向依赖 Locomotion、Animancer、Cinemachine、CharacterController 或 Input System，未修改 Fantasy proto/server，未新增 rollback runtime。
- [x] 11.4 记录 Play Mode 烟测结果：进入并退出 `Sandbox` Play Mode，Console entries=5121、errors=0、warnings=0；WASD/Look/动画表现由用户按步骤复验。
- [x] 11.5 向用户说明哪些内容已真正接入、哪些仍未接入。
- [ ] 11.6 Unity MCP 重新连接后，记录本轮场景绑定修复后的 EditMode/Console 复验结果。

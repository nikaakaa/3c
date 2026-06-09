# Change: 增加项目级 Simulation Tick 系统

## Why
当前输入缓冲层已经使用本地整数 step 表达预输入窗口，但项目还没有统一的 Simulation Tick 语义。后续要做客户端预测、服务端权威模拟、回滚重放和输入网络同步时，如果客户端、服务端、输入层、Locomotion/HFSM 各自维护时间步，就会很快出现“同一帧到底是谁的帧”的分裂路径。

本变更规划一套项目级 tick 地基：用统一 tick id、固定 tick rate、tick accumulator 和有序 tick phase 把本地输入缓冲、现有 Locomotion 主线、未来 ActionArbiter/HFSM、服务端模拟和预测回滚预留到同一条模拟时间线上。

## What Changes
- 新增项目级 `SimulationTick` 语义，使用稳定、单调、可比较的整数 tick id。
- 新增固定 `SimulationTickRate` / settings，统一客户端与服务端的 tick per second 和 fixed delta。
- 新增客户端 tick accumulator，把 Unity 帧时间转换为每帧 0..N 个 simulation tick，并提供最大追帧上限。
- 新增服务端 tick driver 合约，使 Fantasy 服务端后续能用同一 tick rate 和 tick id 驱动权威模拟。
- 新增有序 tick phase runner，固定输入读取、输入缓冲更新、玩法判定、运动执行、快照/事件写入、表现桥接的顺序。
- 明确输入缓冲接入边界：`InputRequestBuffer` 的本地 step 后续应映射为 `SimulationTick`，但输入层仍只保存输入事实和请求，不决定动作结果。
- 明确 Locomotion 接入边界：当前 `PlayerLocomotionController -> BasicLocomotionPipeline -> motion executor -> presenter` 仍是唯一基础移动主线，tick 系统只能调度这条主线，不新增第二套控制器。
- 明确服务端参考 GGPO 的边界：采用固定 tick、输入历史、快照历史、回滚重放这些思想作为后续扩展方向，但本变更不引入 GGPO SDK。
- 为未来预测回滚预留接口边界，但不实现完整 rollback、状态快照历史、网络协议或服务器校正。

## Non-Goals
- 不在本变更中实现完整客户端预测回滚。
- 不在本变更中实现状态快照保存、恢复和重放。
- 不在本变更中修改 Fantasy proto、协议导出工具或真实发包流程。
- 不在本变更中实现服务端完整 3C 玩法模拟。
- 不在本变更中实现真实 ActionArbiter、攻击、闪避、跳跃、连招或取消窗口业务。
- 不在本变更中替换现有 `PlayerLocomotionController`、`BasicLocomotionPipeline`、motion executor 或 Animancer presenter 主线。
- 不新增绕过当前 Locomotion/HFSM 路径的第二套角色控制入口。

## Impact
- Affected specs: `simulation-tick-system`
- Related changes:
  - `add-local-preinput-buffer`
  - `integrate-unityhfsm-locomotion`
  - `refactor-wasd-to-locomotion-pipeline`
- Affected code:
  - 可能新增 `Assets/Scripts/Simulation` 或等价目录下的纯 C# tick core
  - 可能新增客户端 Unity adapter，将 `Time.deltaTime` 转换为 tick accumulator 输入
  - 可能新增服务端共享 tick contract 或 Fantasy 侧 tick driver 占位实现
  - 可能让 `InputRequestBuffer` 的 step 调用侧逐步改为传入 `SimulationTick` 的整数值
  - 后续接入时可能让 `PlayerLocomotionController.Tick` 由 tick runner 调度，但不改变其主线职责
- Validation:
  - OpenSpec 严格校验必须通过
  - 实施阶段必须添加 EditMode 测试覆盖 tick id、fixed delta、accumulator、追帧上限和 phase 顺序
  - 实施阶段必须添加纯 C# 测试证明 tick core 不依赖 Unity 场景对象
  - 实施阶段必须提供手动验证：当前 WASD/Look/基础移动动画行为不回退

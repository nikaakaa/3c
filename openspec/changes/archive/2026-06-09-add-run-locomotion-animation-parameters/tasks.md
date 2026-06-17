## 1. Proposal 边界确认

- [x] 1.1 确认旧 `update-locomotion-animation-parameters` 已标记为废弃，当前实现不沿用其中的 Walk/双映射扩张方案。
- [x] 1.2 确认本变更只做 Run-only，不新增 `WalkStart / WalkLoop / WalkEnd`。
- [x] 1.3 确认逻辑状态仍为 `Idle / MoveStart / MoveLoop / MoveStop`。
- [x] 1.4 确认不新增第二套角色控制器、第二套移动入口或 BBB 运行时依赖。

## 2. 数据模型

- [x] 2.1 新增 Run 基础移动动画 entry 数据。
- [x] 2.2 entry 包含 alias key。
- [x] 2.3 entry 包含 fade duration。
- [x] 2.4 entry 包含 speed。
- [x] 2.5 entry 包含 normalized start time。
- [x] 2.6 entry 包含 stop exit duration。
- [x] 2.7 entry 对负数 fade duration 使用默认播放路径。
- [x] 2.8 entry 对非正 speed fallback 到 1。
- [x] 2.9 entry 对负数 normalized start time 使用默认起播时间。
- [x] 2.10 entry 对负数 stop exit duration fallback 到 `moveStopMinTime`。

## 3. 配置资产

- [x] 3.1 新增或恢复 Run-only 基础移动动画配置 ScriptableObject。
- [x] 3.2 配置只暴露 `Idle / RunStart / RunLoop / RunEnd`。
- [x] 3.3 提供 `ResolveEntry(phase)` 或等价 API。
- [x] 3.4 提供 `ResolveMoveStopExitDuration(fallback)` 或等价 API。
- [x] 3.5 配置校验能报告空 alias key。
- [x] 3.6 配置校验能报告非法 speed。
- [x] 3.7 配置校验能报告缺失 RunEnd stop exit duration 且无 fallback 的情况。

## 4. 状态机时长输入

- [x] 4.1 在纯数据 settings 或 context 中加入当前 `MoveStop` 退出时长。
- [x] 4.2 `MoveStop` 退出时长缺失时 fallback 到 `moveStopMinTime`。
- [x] 4.3 `MoveStartMinTimeReached` 继续读取 `moveStartMinTime`。
- [x] 4.4 `MoveStopMinTimeReached` 读取当前 `MoveStop` 退出时长。
- [x] 4.5 保持 `MoveStop -> MoveStart` 的 `HasMoveIntent` 优先级高于 `MoveStop -> Idle`。

## 5. Presenter 接入

- [x] 5.1 `BasicLocomotionAnimancerPresenter` 从 Run 动画配置读取 alias key。
- [x] 5.2 Presenter 使用配置的 fade duration。
- [x] 5.3 Presenter 使用配置的 speed。
- [x] 5.4 Presenter 使用配置的 normalized start time。
- [x] 5.5 Presenter 对相同 phase 和 alias key 不重复从头播放。
- [x] 5.6 Presenter 不调用状态机切换 API。
- [x] 5.7 Presenter 不调用运动执行端口。
- [x] 5.8 Presenter 不写 Transform。

## 6. 主链接入

- [x] 6.1 `PlayerLocomotionController` 或等价主链引用 Run 动画配置。
- [x] 6.2 主链在 tick 前解析 RunEnd stop exit duration。
- [x] 6.3 主链将解析结果作为纯数据传给状态机。
- [x] 6.4 主链不直接调用 Animancer 播放 API。
- [x] 6.5 当前演示 prefab 或场景引用同一个 Run 动画配置资产。

## 7. 自动测试

- [x] 7.1 测试默认 Run entry 能解析 `Idle / RunStart / RunLoop / RunEnd`。
- [x] 7.2 测试 RunEnd stop exit duration override 生效。
- [x] 7.3 测试 RunEnd stop exit duration 缺失时 fallback 到 `moveStopMinTime`。
- [x] 7.4 测试 `MoveStop` 未达到 RunEnd exit duration 时保持 `MoveStop`。
- [x] 7.5 测试 `MoveStop` 达到 RunEnd exit duration 后进入 `Idle`。
- [x] 7.6 测试 `MoveStop` 中重新出现输入时立即进入 `MoveStart`。
- [x] 7.7 测试配置校验报告空 alias key。
- [x] 7.8 测试配置校验报告非法 speed。
- [x] 7.9 测试状态机、状态图 builder 和条件 evaluator 不引用 Animancer、CharacterController、KCC、Input System 或具体 Camera。
- [x] 7.10 测试 Presenter 不引用状态图 builder 或具体运动执行实现。

## 8. 验证命令

- [x] 8.1 运行 `openspec validate add-run-locomotion-animation-parameters --strict --no-interactive`。
- [x] 8.2 运行 Unity EditMode 定向测试 `PlayerLocomotionControllerTests`。
- [x] 8.3 检查 Unity Console 没有 C# 编译错误。
- [x] 8.4 如果 Unity MCP 或 Unity 测试不可用，记录原因和手动验证步骤，不伪造结果。

## 9. 手动端到端验证

- [x] 9.1 打开当前演示场景。
- [x] 9.2 持续输入移动，确认角色进入 `MoveStart` 后进入 `MoveLoop`。
- [x] 9.3 松开输入，确认角色进入 `MoveStop` 并播放 `RunEnd` alias。
- [x] 9.4 不再输入，确认角色按 RunEnd stop exit duration 回到 `Idle`。
- [x] 9.5 在 RunEnd 中途重新输入，确认角色立即进入 `MoveStart`。
- [x] 9.6 修改 RunEnd stop exit duration，重复验证回 `Idle` 的等待时间发生对应变化。

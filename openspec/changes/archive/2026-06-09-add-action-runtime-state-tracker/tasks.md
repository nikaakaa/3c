## 1. Scope 确认

- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 确认本变更只实现 Action 当前状态事实 tracker。
- [x] 1.3 确认本变更不实现完整状态机。
- [x] 1.4 确认本变更不使用 UnityHFSM 或其他状态机库。
- [x] 1.5 确认本变更不实现状态图、transition、condition 或 priority。
- [x] 1.6 确认本变更不实现自动退出。
- [x] 1.7 确认本变更不新增 ActionStateDefinition 或 catalog。
- [x] 1.8 确认本变更不接真实输入。
- [x] 1.9 确认本变更不消费 `InputRequestBuffer`。
- [x] 1.10 确认本变更不接 Animancer、Animator、AnimationClip 或 TransitionAsset。
- [x] 1.11 确认本变更不修改 `PlayerLocomotionController`。
- [x] 1.12 确认本变更不修改 `BasicLocomotionStateMachine`。
- [x] 1.13 确认本变更不迁移 `MoveStop -> MoveStart`。
- [x] 1.14 确认本变更不做 CharacterRoot 或黑板。
- [x] 1.15 如果实现需要新增运行时 MonoBehaviour 或挂 prefab，停止并回到 OpenSpec。

## 2. 目录与模块边界

- [x] 2.1 确认 `Assets/Scripts/Character/Action/Model/` 存在。
- [x] 2.2 确认 `Assets/Scripts/Character/Action/Solver/` 存在。
- [x] 2.3 新增 snapshot 放在 `Action/Model`。
- [x] 2.4 新增 tracker 放在 `Action/Solver`。
- [x] 2.5 确认本变更不新增 `Action/Runtime` MonoBehaviour。
- [x] 2.6 确认测试放在 `Assets/Tests/Editor/ActionRuntimeStateTrackerTests.cs` 或等价编辑器测试文件。

## 3. Runtime Snapshot

- [x] 3.1 新增 `ActionRuntimeStateSnapshot`。
- [x] 3.2 snapshot 包含 current state。
- [x] 3.3 snapshot 包含 elapsed seconds。
- [x] 3.4 snapshot 包含 current resistance。
- [x] 3.5 snapshot 包含 current tick。
- [x] 3.6 snapshot 对负 elapsed 安全处理。
- [x] 3.7 snapshot 对负 resistance 安全处理。
- [x] 3.8 snapshot 对负 tick 安全处理。
- [x] 3.9 snapshot 不包含 `UnityEngine.Object`。
- [x] 3.10 snapshot 不包含 Animancer 类型。

## 4. Tracker 核心状态

- [x] 4.1 新增 `ActionRuntimeStateTracker`。
- [x] 4.2 tracker 默认 current state 为 `Action.None` 或等价默认状态。
- [x] 4.3 tracker 默认 elapsed seconds 为 0。
- [x] 4.4 tracker 默认 current resistance 为 0。
- [x] 4.5 tracker 默认 current tick 为 0。
- [x] 4.6 tracker 暴露 current state。
- [x] 4.7 tracker 暴露 elapsed seconds。
- [x] 4.8 tracker 暴露 current resistance。
- [x] 4.9 tracker 暴露 current tick。
- [x] 4.10 tracker 暴露 snapshot 或等价读取入口。
- [x] 4.11 tracker 不调用 `ActionInterruptArbiter`。
- [x] 4.12 tracker 不消费输入。
- [x] 4.13 tracker 不切 Locomotion 状态。
- [x] 4.14 tracker 不播放动画。

## 5. Reset 与 EnterState

- [x] 5.1 tracker 提供 `Reset`。
- [x] 5.2 `Reset` 回到 `Action.None`。
- [x] 5.3 `Reset` 清零 elapsed seconds。
- [x] 5.4 `Reset` 清零 current resistance。
- [x] 5.5 `Reset` 保持或清零 current tick，并在测试中覆盖。
- [x] 5.6 tracker 提供 `EnterState` 或等价入口。
- [x] 5.7 `EnterState` 设置 current state。
- [x] 5.8 `EnterState` 设置 current resistance。
- [x] 5.9 `EnterState` 对无效 state 使用安全 fallback 或明确行为。
- [x] 5.10 `EnterState` 对负 resistance 安全处理。
- [x] 5.11 `EnterState` 重置 elapsed seconds。

## 6. Tick

- [x] 6.1 tracker 提供 `Tick`。
- [x] 6.2 `Tick` 增长 elapsed seconds。
- [x] 6.3 `Tick` 对负 delta 安全处理。
- [x] 6.4 `Tick` 更新 current tick。
- [x] 6.5 `Tick` 对负 current tick 安全处理。
- [x] 6.6 `Tick` 不执行自动退出。
- [x] 6.7 `Tick` 不调用动画 API。

## 7. Interrupt Context 输出

- [x] 7.1 tracker 提供 `CreateInterruptContext` 或等价方法。
- [x] 7.2 context current state 来自 tracker current state。
- [x] 7.3 context elapsed seconds 来自 tracker elapsed seconds。
- [x] 7.4 context resistance 来自 tracker current resistance。
- [x] 7.5 context current tick 来自 tracker current tick。
- [x] 7.6 context 不包含 Unity 组件引用。

## 8. Decision 应用

- [x] 8.1 tracker 提供 `ApplyDecision` 或等价入口。
- [x] 8.2 accepted decision 进入 target state。
- [x] 8.3 accepted decision 重置 elapsed seconds。
- [x] 8.4 accepted decision 使用调用方传入 target resistance。
- [x] 8.5 accepted decision 对负 target resistance 安全处理。
- [x] 8.6 accepted decision target state 无效时使用安全 fallback 或明确行为。
- [x] 8.7 rejected decision 不改变 current state。
- [x] 8.8 rejected decision 不改变 elapsed seconds。
- [x] 8.9 rejected decision 不改变 current resistance。
- [x] 8.10 rejected decision 不改变 current tick。

## 9. 仲裁器组合测试

- [x] 9.1 用代码构造 `ActionInterruptPolicy`。
- [x] 9.2 用代码构造 `ActionInterruptRequest`。
- [x] 9.3 从 tracker 生成 `ActionInterruptContext`。
- [x] 9.4 调用 `ActionInterruptArbiter.Arbitrate`。
- [x] 9.5 将 accepted decision 应用到 tracker。
- [x] 9.6 断言 tracker 进入 target state。
- [x] 9.7 断言 rejected decision 不改变 tracker。

## 10. 自动测试

- [x] 10.1 新增 `ActionRuntimeStateTrackerTests`。
- [x] 10.2 测试默认状态为 `Action.None`。
- [x] 10.3 测试 `EnterState` 设置 state。
- [x] 10.4 测试 `EnterState` 设置 resistance。
- [x] 10.5 测试 `EnterState` 重置 elapsed seconds。
- [x] 10.6 测试负 resistance 被安全处理。
- [x] 10.7 测试 `Tick` 增长 elapsed seconds。
- [x] 10.8 测试负 delta 不减少 elapsed seconds。
- [x] 10.9 测试 `Tick` 更新 current tick。
- [x] 10.10 测试 snapshot 输出正确。
- [x] 10.11 测试 `CreateInterruptContext` 输出正确。
- [x] 10.12 测试 accepted decision 进入目标 state。
- [x] 10.13 测试 accepted decision 重置 elapsed seconds。
- [x] 10.14 测试 accepted decision 使用 target resistance。
- [x] 10.15 测试 rejected decision 不改变状态事实。
- [x] 10.16 测试 tracker 不自动退出。
- [x] 10.17 测试仲裁器 accepted decision 驱动 tracker。
- [x] 10.18 测试 Locomotion runtime 不依赖 tracker。
- [x] 10.19 测试 tracker 不需要 Unity 场景对象。

## 11. 静态边界验证

- [x] 11.1 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Animancer`。
- [x] 11.2 静态搜索 `Assets/Scripts/Character/Action` 不引用 `AnimationClip`。
- [x] 11.3 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Animator`。
- [x] 11.4 静态搜索 `Assets/Scripts/Character/Action` 不引用 `CharacterController`。
- [x] 11.5 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Cinemachine`。
- [x] 11.6 静态搜索 `Assets/Scripts/Character/Action` 不引用 `UnityEngine.InputSystem`。
- [x] 11.7 静态搜索 `Assets/Scripts/Character/Action` 不引用 `BBBNexus`。
- [x] 11.8 静态搜索确认 `PlayerLocomotionController` 不依赖 `ActionRuntimeStateTracker`。
- [x] 11.9 静态搜索确认 `BasicLocomotionStateMachine` 不依赖 `ActionRuntimeStateTracker`。
- [x] 11.10 静态搜索确认 `BasicLocomotionAnimancerPresenter` 不依赖 `ActionRuntimeStateTracker`。

## 12. Unity 验证

- [x] 12.1 请求 Unity 刷新脚本。
- [x] 12.2 检查 Unity Console 没有 C# 编译错误。
- [x] 12.3 运行 Unity EditMode 定向测试 `ActionRuntimeStateTrackerTests`。
- [x] 12.4 运行 `ActionInterruptArbiterTests`，确认已有仲裁行为不回退。
- [x] 12.5 如果 Unity MCP 或测试不可用，记录原因和手动验证步骤，不伪造结果。

## 13. OpenSpec 验证

- [x] 13.1 运行 `openspec validate add-action-runtime-state-tracker --strict --no-interactive`。
- [x] 13.2 如果实现过程中调整范围，同步更新 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 13.3 完成实现后只把真实完成项标记为 `- [x]`。

## 14. 手动验证

- [x] 14.1 在 Unity Test Runner 中确认 `ActionRuntimeStateTrackerTests` 全部通过。
- [ ] 14.2 打开当前演示场景，确认 WASD、Look、Idle、MoveStart、MoveLoop、MoveStop 行为没有因为新增 tracker 变化。
- [x] 14.3 确认没有新增需要手动挂到角色 prefab 的 Action tracker 组件。
- [x] 14.4 确认没有新增第二套角色控制器或第二条基础移动入口。

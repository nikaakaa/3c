## 1. Scope 确认

- [x] 1.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 1.2 确认本变更只实现纯逻辑 `ActionInterruptArbiter`。
- [x] 1.3 确认本变更不播放 Animancer 动画。
- [x] 1.4 确认本变更不新增攻击、闪避、受击、死亡运行时状态。
- [x] 1.5 确认本变更不修改 `Idle / MoveStart / MoveLoop / MoveStop` 状态图。
- [x] 1.6 确认 `MoveStop -> MoveStart` 继续由 Locomotion 状态图处理。
- [x] 1.7 确认不复制 BBB 运行时代码或 namespace。
- [x] 1.8 如果实现需要绕过当前 `PlayerLocomotionController`、状态图或 tick phase，停止并回到 OpenSpec。

## 2. 目录与模块边界

- [x] 2.1 新建 `Assets/Scripts/Character/Action/Model/`。
- [x] 2.2 新建 `Assets/Scripts/Character/Action/Solver/`。
- [x] 2.3 确认 `Action/Model` 不引用 Unity 场景组件类型。
- [x] 2.4 确认 `Action/Solver` 不引用 Animancer。
- [x] 2.5 确认 `Action/Solver` 不引用现有 Locomotion runtime 具体 MonoBehaviour。
- [x] 2.6 确认测试放在 `Assets/Tests/Editor/ActionInterruptArbiterTests.cs` 或等价编辑器测试文件。

## 3. 纯数据模型

- [x] 3.1 定义 `ActionStateId`。
- [x] 3.2 `ActionStateId` 支持稳定字符串或等价稳定值。
- [x] 3.3 `ActionStateId` 能表达空/无效状态。
- [x] 3.4 定义 `ActionRequestType`。
- [x] 3.5 `ActionRequestType` 至少覆盖 `Locomotion / Attack / Dodge / HitReact / Death / Custom`。
- [x] 3.6 定义 `ActionInterruptTimingRule`。
- [x] 3.7 `ActionInterruptTimingRule` 至少覆盖 `Always / AfterElapsedTime / DuringElapsedTimeWindow`。
- [x] 3.8 定义 `ActionInterruptRejectReason`。
- [x] 3.9 拒绝原因至少覆盖 `NoRequest / Expired / NoPolicy / PriorityTooLow / BlockedByResistance / TimingNotSatisfied / InvalidPolicy`。
- [x] 3.10 定义 `ActionInterruptRequest`。
- [x] 3.11 request 包含 request id 或 sequence。
- [x] 3.12 request 包含 request type。
- [x] 3.13 request 包含 target state。
- [x] 3.14 request 包含 priority。
- [x] 3.15 request 包含 source tick 或 source order。
- [x] 3.16 request 包含过期信息或明确不过期语义。
- [x] 3.17 request 不包含 `AnimationClip`。
- [x] 3.18 request 不包含 `UnityEngine.Object`。
- [x] 3.19 request 不包含 Animancer 类型。
- [x] 3.20 定义 `ActionInterruptContext`。
- [x] 3.21 context 包含 current state。
- [x] 3.22 context 包含 current state elapsed seconds。
- [x] 3.23 context 包含 current state resistance。
- [x] 3.24 context 包含 current tick 或可选 tick。
- [x] 3.25 context 不包含 Unity 组件引用。
- [x] 3.26 定义 `ActionInterruptPolicy`。
- [x] 3.27 policy 包含 from state 或 from wildcard。
- [x] 3.28 policy 包含 target state 或 target wildcard。
- [x] 3.29 policy 包含 min priority。
- [x] 3.30 policy 包含 timing rule。
- [x] 3.31 policy 包含 window start。
- [x] 3.32 policy 包含 window end。
- [x] 3.33 policy 包含 force 标记。
- [x] 3.34 定义 `ActionInterruptDecision`。
- [x] 3.35 decision 包含 accepted。
- [x] 3.36 decision 包含 selected request。
- [x] 3.37 decision 包含 target state。
- [x] 3.38 decision 包含 reject reason。

## 4. 策略校验

- [x] 4.1 新建 `ActionInterruptPolicyValidator`。
- [x] 4.2 校验空 policy 集合是合法但不会接受请求。
- [x] 4.3 校验 `AfterElapsedTime` 的 window start 不为负。
- [x] 4.4 校验 `DuringElapsedTimeWindow` 的 window end 大于等于 window start。
- [x] 4.5 校验 min priority 不小于 0。
- [x] 4.6 校验目标状态无效时报错。
- [x] 4.7 校验重复 policy 以稳定规则处理或报告警告。
- [x] 4.8 校验器不依赖 Unity Editor API。

## 5. 仲裁器核心

- [x] 5.1 新建 `ActionInterruptArbiter`。
- [x] 5.2 输入参数包含 context。
- [x] 5.3 输入参数包含 request 集合。
- [x] 5.4 输入参数包含 policy 集合。
- [x] 5.5 无请求时返回 rejected。
- [x] 5.6 过期请求被跳过。
- [x] 5.7 无匹配 policy 的请求被拒绝。
- [x] 5.8 priority 低于 policy min priority 的请求被拒绝。
- [x] 5.9 非 force 请求 priority 小于或等于 current resistance 时被拒绝。
- [x] 5.10 force 请求有显式 policy 时可绕过 current resistance。
- [x] 5.11 `Always` policy 满足时序。
- [x] 5.12 `AfterElapsedTime` 在 elapsed time 未达 window start 时拒绝。
- [x] 5.13 `AfterElapsedTime` 在 elapsed time 达到 window start 时接受。
- [x] 5.14 `DuringElapsedTimeWindow` 在窗口前拒绝。
- [x] 5.15 `DuringElapsedTimeWindow` 在窗口内接受。
- [x] 5.16 `DuringElapsedTimeWindow` 在窗口后拒绝。
- [x] 5.17 多个可接受请求时选择 priority 最高者。
- [x] 5.18 priority 相同请求按稳定 source order 或提交顺序选择。
- [x] 5.19 输出 rejected 时携带最有用拒绝原因。
- [x] 5.20 仲裁器不缓存跨帧可变状态。

## 6. 自动测试

- [x] 6.1 新建 `ActionInterruptArbiterTests`。
- [x] 6.2 测试无请求返回 `NoRequest`。
- [x] 6.3 测试无 matching policy 返回 `NoPolicy`。
- [x] 6.4 测试 expired request 被拒绝。
- [x] 6.5 测试 `Always` policy 接受请求。
- [x] 6.6 测试 priority 低于 min priority 被拒绝。
- [x] 6.7 测试 priority 被 current resistance 阻挡。
- [x] 6.8 测试 force policy 绕过 current resistance。
- [x] 6.9 测试 `AfterElapsedTime` 未到时间拒绝。
- [x] 6.10 测试 `AfterElapsedTime` 到时间接受。
- [x] 6.11 测试 `DuringElapsedTimeWindow` 窗口前拒绝。
- [x] 6.12 测试 `DuringElapsedTimeWindow` 窗口内接受。
- [x] 6.13 测试 `DuringElapsedTimeWindow` 窗口后拒绝。
- [x] 6.14 测试多请求选择最高 priority。
- [x] 6.15 测试同 priority 稳定选择。
- [x] 6.16 测试 invalid policy 被 validator 报告。
- [x] 6.17 测试 request 不需要 Unity 对象即可完成仲裁。
- [x] 6.18 测试 Locomotion `MoveStop` 相关 transition 不依赖本仲裁器。

## 7. 静态边界验证

- [x] 7.1 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Animancer`。
- [x] 7.2 静态搜索 `Assets/Scripts/Character/Action` 不引用 `AnimationClip`。
- [x] 7.3 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Animator`。
- [x] 7.4 静态搜索 `Assets/Scripts/Character/Action` 不引用 `CharacterController`。
- [x] 7.5 静态搜索 `Assets/Scripts/Character/Action` 不引用 `Cinemachine`。
- [x] 7.6 静态搜索 `Assets/Scripts/Character/Action` 不引用 `UnityEngine.InputSystem`。
- [x] 7.7 静态搜索 `Assets/Scripts/Character/Action` 不引用 `BBBNexus`。
- [x] 7.8 静态搜索确认没有新增 `BBBCharacterController` 等价主控。
- [x] 7.9 静态搜索确认 `BasicLocomotionStateMachine` 不依赖 `ActionInterruptArbiter`。
- [x] 7.10 静态搜索确认 `BasicLocomotionAnimancerPresenter` 不依赖 `ActionInterruptArbiter`。

## 8. Unity 验证

- [x] 8.1 请求 Unity 刷新脚本。
- [x] 8.2 检查 Unity Console 没有 C# 编译错误。
- [x] 8.3 运行 Unity EditMode 定向测试 `ActionInterruptArbiterTests`。
- [x] 8.4 运行相关边界测试或已有 `PlayerLocomotionControllerTests`，确认基础移动未回退。
- [x] 8.5 如果 Unity MCP 或测试不可用，记录原因和手动验证步骤，不伪造结果。

## 9. OpenSpec 验证

- [x] 9.1 运行 `openspec validate add-action-interrupt-arbiter --strict --no-interactive`。
- [x] 9.2 如果实现过程中调整范围，同步更新 `proposal.md`、`design.md`、`tasks.md` 和 spec delta。
- [x] 9.3 完成实现后只把真实完成项标记为 `- [x]`。

## 10. 手动验证

- [x] 10.1 在 Unity Test Runner 中确认 `ActionInterruptArbiterTests` 全部通过。
- [x] 10.2 打开当前演示场景，确认 WASD、Look、Idle、MoveStart、MoveLoop、MoveStop 行为没有因为新增模块变化。
- [x] 10.3 确认 `MoveStop` 中重新输入仍立即进入 `MoveStart`。
- [x] 10.4 确认没有新增需要手动挂到角色 prefab 的仲裁组件。
- [x] 10.5 确认没有新增第二套角色控制器或第二条基础移动入口。

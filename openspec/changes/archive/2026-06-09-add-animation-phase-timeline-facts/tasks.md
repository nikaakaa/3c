## 0. 审批前检查
- [x] 0.1 确认 `add-locomotion-animation-phase-exit-policy` 已完成用户验证或明确允许作为依赖继续实现。
- [x] 0.2 确认本变更不实现可视化 Timeline 编辑器。
- [x] 0.3 确认本变更不新增 `Walk` 逻辑状态。
- [x] 0.4 确认本变更不新增第二套角色控制器路径。

## 1. 动画退出策略模型
- [x] 1.1 打开 `LocomotionAnimationExitPolicy`，确认现有枚举值顺序和序列化风险。
- [x] 1.2 为 `LocomotionAnimationExitPolicy` 增加 `OnAnimationEnd`。
- [x] 1.3 保留 `Manual` 和 `AfterDuration` 的现有语义。
- [x] 1.4 检查默认资产序列化值是否需要迁移说明。
- [x] 1.5 为 `RunLocomotionAnimationConfigSO.Validate` 增加 `OnAnimationEnd` 校验规则。
- [x] 1.6 验证 `OnAnimationEnd` 不要求 `exitDuration` 为正。
- [x] 1.7 验证 `AfterDuration` 仍然要求非负 `exitDuration`。

## 2. 播放进度快照模型
- [x] 2.1 在 `Animation/Model` 增加纯数据播放进度快照模型。
- [x] 2.2 快照记录当前 `BasicMovementPhase`。
- [x] 2.3 快照记录当前 alias key。
- [x] 2.4 快照记录 normalized time。
- [x] 2.5 快照记录是否有有效播放状态。
- [x] 2.6 快照记录当前动画是否已结束。
- [x] 2.7 快照不保存 `AnimancerState`。
- [x] 2.8 快照不保存 `AnimationClip`。
- [x] 2.9 快照不保存 `TransitionAsset`。
- [x] 2.10 快照不保存 `UnityEngine.Object`。

## 3. 动画事实模型
- [x] 3.1 在 `Animation/Model` 或合适纯数据目录增加 phase timeline facts。
- [x] 3.2 第一版 facts 只包含 `CanExit`。
- [x] 3.3 facts 提供 false 的默认值。
- [x] 3.4 facts 不依赖 Animancer。
- [x] 3.5 facts 不依赖 Unity 场景实例。

## 4. Timeline Fact Sampler
- [x] 4.1 在 `Animation/Solver` 增加 sampler 目录或确认现有目录。
- [x] 4.2 实现 sampler 的静态入口或小型服务对象。
- [x] 4.3 sampler 输入 phase config。
- [x] 4.4 sampler 输入 phaseTime。
- [x] 4.5 sampler 输入播放进度快照。
- [x] 4.6 `Manual` 策略输出 `CanExit=false`。
- [x] 4.7 `AfterDuration` 策略在 phaseTime 未达到时输出 `CanExit=false`。
- [x] 4.8 `AfterDuration` 策略在 phaseTime 达到时输出 `CanExit=true`。
- [x] 4.9 `AfterDuration` 使用现有浮点 epsilon 语义或等价边界保护。
- [x] 4.10 `OnAnimationEnd` 在没有有效播放进度时输出 `CanExit=false`。
- [x] 4.11 `OnAnimationEnd` 在播放未结束时输出 `CanExit=false`。
- [x] 4.12 `OnAnimationEnd` 在播放已结束时输出 `CanExit=true`。
- [x] 4.13 sampler 不读取 Animancer。
- [x] 4.14 sampler 不读取 AnimationClip。
- [x] 4.15 sampler 不读取 TransitionLibrary。

## 5. Movement Facts 边界
- [x] 5.1 在 `Movement/Model` 增加 `BasicMovementPhaseFacts` 或等价纯数据模型。
- [x] 5.2 movement facts 至少包含 `PhaseCanExit`。
- [x] 5.3 movement facts 默认 `PhaseCanExit=false`。
- [x] 5.4 movement facts 不引用 `ThirdPersonAnimation` 类型。
- [x] 5.5 movement facts 不引用 Animancer。
- [x] 5.6 movement facts 不引用 Unity 场景实例。

## 6. 状态图上下文接入
- [x] 6.1 扩展 `LocomotionStateGraphContext`，接收 movement facts。
- [x] 6.2 保留旧构造路径的默认 facts，避免测试大面积破坏。
- [x] 6.3 为 `LocomotionStateGraphCondition` 增加 `PhaseCanExit`。
- [x] 6.4 在 evaluator 中实现 `PhaseCanExit` 条件。
- [x] 6.5 确认 `PhaseExitTimeReached` 兼容旧测试或迁移计划。
- [x] 6.6 将默认 `MoveStart -> MoveLoop` 条件切换为 `HasMoveIntent + PhaseCanExit`。
- [x] 6.7 将默认 `MoveStop -> Idle` 条件切换为 `NoMoveIntent + PhaseCanExit`。
- [x] 6.8 保持 `MoveStop -> MoveStart` 的 `HasMoveIntent` 高优先级。
- [x] 6.9 保持 `MoveStart -> MoveStop` 的 `NoMoveIntent` 高优先级。

## 7. Pipeline / Controller 组装
- [x] 7.1 扩展 `BasicLocomotionStateMachine.Tick` 或 pipeline 输入，使其能接收 movement facts。
- [x] 7.2 保留无 facts 调用路径，默认 `PhaseCanExit=false` 或通过现有 timing 兼容。
- [x] 7.3 在 `PlayerLocomotionController` 中从 Run 动画配置解析当前 phase config。
- [x] 7.4 在 `PlayerLocomotionController` 中从 Presenter 读取当前播放进度快照。
- [x] 7.5 在 `PlayerLocomotionController` 中调用 sampler 生成动画 facts。
- [x] 7.6 在 `PlayerLocomotionController` 中将动画 facts 映射为 movement facts。
- [x] 7.7 在进入 pipeline/state machine 前完成 facts 计算。
- [x] 7.8 保证 motion executor 和 presenter 的调用顺序不被绕过。
- [x] 7.9 保证没有新增第二个 Update 驱动入口。

## 8. Presenter 播放进度暴露
- [x] 8.1 在 `BasicLocomotionAnimancerPresenter` 暴露只读播放进度快照。
- [x] 8.2 快照 phase 来自 presenter 当前 phase。
- [x] 8.3 快照 alias 来自 presenter 当前 alias。
- [x] 8.4 快照 normalized time 来自当前 Animancer state。
- [x] 8.5 快照 isEnded 的计算封装在 presenter 内。
- [x] 8.6 当前没有有效 state 时，快照标记无效。
- [x] 8.7 Presenter 不调用状态机 API。
- [x] 8.8 Presenter 不注册 `OnEnd` 来驱动基础移动状态切换。
- [x] 8.9 Presenter 不读取 `CanExit`。
- [x] 8.10 Presenter 不读取 action arbiter。

## 9. 配置资产迁移
- [x] 9.1 检查 `DefaultRunLocomotionAnimationConfig.asset` 当前 `MoveStop` 配置。
- [x] 9.2 将 `MoveStop` exit policy 迁移为 `OnAnimationEnd`。
- [x] 9.3 清理 `MoveStop` 对 `exitDuration` 的强依赖。
- [x] 9.4 保持 `MoveStart` 默认继续使用 `AfterDuration`。
- [x] 9.5 保持 `Idle` 默认 `Manual`。
- [x] 9.6 保持 `MoveLoop` 默认 `Manual`。
- [x] 9.7 检查 prefab/scene 没有新增隐式加载路径。

## 10. 自动测试：sampler
- [x] 10.1 增加 `Manual` 不产生 `CanExit` 的 EditMode 测试。
- [x] 10.2 增加 `AfterDuration` 未达到时不退出的 EditMode 测试。
- [x] 10.3 增加 `AfterDuration` 达到时退出的 EditMode 测试。
- [x] 10.4 增加 `AfterDuration` 浮点边界的 EditMode 测试。
- [x] 10.5 增加 `OnAnimationEnd` 无有效播放进度时不退出的 EditMode 测试。
- [x] 10.6 增加 `OnAnimationEnd` 播放未结束时不退出的 EditMode 测试。
- [x] 10.7 增加 `OnAnimationEnd` 播放结束时退出的 EditMode 测试。

## 11. 自动测试：状态图
- [x] 11.1 增加 `MoveStart` 在 `PhaseCanExit=false` 时保持 `MoveStart` 的测试。
- [x] 11.2 增加 `MoveStart` 在 `PhaseCanExit=true` 且有输入时进入 `MoveLoop` 的测试。
- [x] 11.3 增加 `MoveStop` 在 `PhaseCanExit=false` 且无输入时保持 `MoveStop` 的测试。
- [x] 11.4 增加 `MoveStop` 在 `PhaseCanExit=true` 且无输入时回到 `Idle` 的测试。
- [x] 11.5 增加 `MoveStop` 在 `PhaseCanExit=false` 但重新有输入时立即进入 `MoveStart` 的测试。
- [x] 11.6 增加 `PhaseExitTimeReached` 兼容或迁移后的回归测试。

## 12. 自动测试：Controller / Presenter 边界
- [x] 12.1 使用 fake presenter progress 验证 controller 能让 `OnAnimationEnd` 驱动 `MoveStop -> Idle`。
- [x] 12.2 使用 fake presenter progress 验证未结束时不会 `MoveStop -> Idle`。
- [x] 12.3 验证有输入时不等待 `OnAnimationEnd`，立即从 `MoveStop` 切 `MoveStart`。
- [x] 12.4 静态测试确认 Presenter 源码不包含 `OnEnd` 状态切换路径。
- [x] 12.5 静态测试确认 Presenter 不调用 `ChangeState`。
- [x] 12.6 静态测试确认 state machine 不引用 Animancer。
- [x] 12.7 静态测试确认 state machine 不引用 `AnimationClip`。
- [x] 12.8 静态测试确认 sampler 不引用 Animancer。

## 13. 配置校验测试
- [x] 13.1 测试 `OnAnimationEnd` 配置不要求 `exitDuration`。
- [x] 13.2 测试空 alias 仍然报错。
- [x] 13.3 测试 `AfterDuration` 负数仍然报错。
- [x] 13.4 测试 `OnAnimationEnd` 不校验 Animancer fade、speed、normalized start time。

## 14. 静态验证
- [x] 14.1 运行 `rg -n "OnEnd|SetOnEndCallback|ChangeState" BasicLocomotionAnimancerPresenter.cs` 并确认没有基础移动状态切换路径。
- [x] 14.2 运行 `rg -n "Animancer|AnimationClip|TransitionLibrary" 3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Solver 3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement/Model`。
- [x] 14.3 运行 `rg -n "Resources.Load|FindObjectOfType|Camera.main" 3cDemo/Client/3C_Client/Assets/Scripts/Character/Animation 3cDemo/Client/3C_Client/Assets/Scripts/Character/Movement`。
- [x] 14.4 运行 `openspec validate add-animation-phase-timeline-facts --strict --no-interactive`。

## 15. Unity 验证
- [x] 15.1 让 Unity Editor 完成脚本编译。
- [x] 15.2 检查 Console 没有新增编译错误。
- [x] 15.3 运行定向 EditMode 测试 `PlayerLocomotionControllerTests`。
- [x] 15.4 记录 Unity MCP 可用，并已运行同名测试；未使用 Unity batchmode。

## 16. 手动验证
- [ ] 16.1 打开当前 3C 演示场景。
- [ ] 16.2 确认角色绑定 `DefaultRunLocomotionAnimationConfig` 或等价 Run 配置。
- [ ] 16.3 确认 `MoveStop` 配置为 `OnAnimationEnd`。
- [ ] 16.4 进入 Play Mode。
- [ ] 16.5 按住移动键进入 `MoveLoop`。
- [ ] 16.6 松开移动键，观察播放 `RunEnd`。
- [ ] 16.7 不再输入，确认 `RunEnd` 播放结束后进入 `Idle`。
- [ ] 16.8 再次移动后松开，在 `RunEnd` 未播完时重新输入移动。
- [ ] 16.9 确认角色立即进入 `MoveStart` 或等价起步阶段。
- [ ] 16.10 确认中途输入不等待 `RunEnd` 播完。
- [ ] 16.11 确认没有额外角色控制器或第二套状态机参与。

## 17. 文档更新
- [x] 17.1 更新 `docs/agents/character-animation-state-roadmap.md` 当前基线。
- [x] 17.2 在路线规划中说明 `RunEnd` 已从手填时长升级为 Timeline Fact。
- [x] 17.3 在路线规划中保留未来 Timeline 编辑器顺序：Inspector、轻量窗口、调试面板、Timeline 编辑器。
- [x] 17.4 明确后续 `OnMarker`、cancel window、IK window 需要单独 OpenSpec。

## 18. 完成状态
- [ ] 18.1 确认所有实现任务完成后再更新本清单。
- [ ] 18.2 确认自动测试、静态验证和手动验证结果已记录。
- [ ] 18.3 将所有已完成任务标记为 `- [x]`。


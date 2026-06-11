## 0. 实施前收口

- [x] 0.1 读取本 change 的 `proposal.md`、`design.md` 和 spec delta。
- [x] 0.2 确认 `add-run-locomotion-animation-parameters` 不再作为独立 Run-only 实施路径推进。
- [x] 0.3 确认本变更只接入 Walk/Run，不接 Sprint。
- [x] 0.4 确认不新增第二套角色控制器或第二条移动入口。
- [x] 0.5 确认不复制 BBB 状态类、主控、InputPipeline 或 MotionDriver 主线。

## 1. 输入与档位事实

- [x] 1.1 新增 `BasicMovementGait` 或等价纯数据类型。
- [x] 1.2 定义 `Walk` 作为普通移动默认档位。
- [x] 1.3 定义 `Run` 作为按住 Run 输入时的基础移动档位。
- [x] 1.4 扩展 `BasicLocomotionInputSnapshot` 或等价输入快照以携带 Run 保持事实。
- [x] 1.5 扩展 `IBasicLocomotionInputSource` 的实现读取 Run 保持事实。
- [x] 1.6 在 `UnityInputSystemLocomotionInputSource` 中新增 Run action 名称配置。
- [x] 1.7 默认 Run action 名称使用 `Run`，实施时验证项目 InputActionAsset 绑定。
- [x] 1.8 保持 `PlayerLocomotionController` 不引用 `InputActionReference` 或 `UnityEngine.InputSystem`。

## 2. Intent 与移动参数

- [x] 2.1 扩展 `MovementInputIntent` 记录当前档位。
- [x] 2.2 无移动输入时 intent 不应把 Run 输入误判为移动。
- [x] 2.3 有移动输入且 Run 未保持时选择 Walk。
- [x] 2.4 有移动输入且 Run 保持时选择 Run。
- [x] 2.5 扩展 `BasicMovementSettings` 支持 Walk speed 和 Run speed。
- [x] 2.6 确认 `MovementCommandBuilder` 按档位选择 planar speed。
- [x] 2.7 `MovementCommand` 携带当前档位用于诊断和测试。

## 3. 状态机边界保持

- [x] 3.1 确认 `BasicLocomotionStateMachine` 仍只输出 `Idle / MoveStart / MoveLoop / MoveStop`。
- [x] 3.2 确认 `LocomotionStateGraphDefinition` 默认 states 不新增 Walk/Run。
- [x] 3.3 确认 `LocomotionStateGraphTransitionConfig` 默认转移不依赖 Walk/Run。
- [x] 3.4 确认 `LocomotionStateGraphConditionEvaluator` 不读取动画配置或输入 adapter。
- [x] 3.5 增加静态测试防止状态机引用 Animancer、Input System、CharacterController、KCC、BBB。

## 4. Last Moving Gait

- [x] 4.1 在 controller 或等价组装层维护 last moving gait。
- [x] 4.2 移动输入有效时更新 last moving gait。
- [x] 4.3 进入 `MoveStop` 且无移动输入时使用 last moving gait。
- [x] 4.4 `MoveStop` 中重新输入时使用当前输入档位进入下一次 `MoveStart`。
- [x] 4.5 重置或禁用 controller 时确保 last moving gait 有稳定默认值。

## 5. 动画上下文与配置

- [x] 5.1 扩展 `MovementAnimationContext` 携带当前档位。
- [x] 5.2 将 Run-only 配置升级或替换为 Walk/Run 基础移动动画配置。
- [x] 5.3 配置支持 `Idle`。
- [x] 5.4 配置支持 `WalkStart / WalkLoop / WalkEnd`。
- [x] 5.5 配置支持 `RunStart / RunLoop / RunEnd`。
- [x] 5.6 配置能按 `phase + gait` 解析 alias key。
- [x] 5.7 配置能按 `phase + gait` 解析退出策略。
- [x] 5.8 配置能按 `phase + gait + alias` 解析 motion profile。
- [x] 5.9 配置校验报告空 alias key。
- [x] 5.10 配置校验报告 binding phase/gait/alias/profile 不匹配。
- [x] 5.11 停止阶段使用 last moving gait 解析 WalkEnd 或 RunEnd。

## 6. Presenter 与 motion facts

- [x] 6.1 `BasicLocomotionAnimancerPresenter` 使用 `phase + gait` 解析 alias。
- [x] 6.2 Presenter 对相同 phase、gait、alias 避免重复重播。
- [x] 6.3 Presenter 继续只负责动画播放和 playback progress 暴露。
- [x] 6.4 Presenter 不调用状态机切换 API。
- [x] 6.5 Presenter 不调用运动执行端口。
- [x] 6.6 Presenter 不写 Transform。
- [x] 6.7 motion profile sampler 继续保持不引用 Animancer runtime。
- [x] 6.8 Movement layer facts 不引用 AnimationClip、AnimationCurve 或配置 SO。

## 7. 资产与 prefab 绑定

- [x] 7.1 迁移现有 Run 配置资产为单一 Walk/Run 配置资产，或创建新资产并替换引用。
- [x] 7.2 绑定 WalkStart/WalkLoop/WalkEnd alias 到 Animancer TransitionLibrary 中已有或新建 alias。
- [x] 7.3 绑定 RunStart/RunLoop/RunEnd alias，保留现有 Run 行为。
- [x] 7.4 确认 Walk/Run 配置资产由角色 prefab 持有。
- [x] 7.5 确认场景实例不重复维护 Walk/Run 动画配置。
- [x] 7.6 检查 prefab diff 只包含本变更需要的配置引用变更。

## 8. 自动测试

- [x] 8.1 测试普通移动输入生成 Walk intent。
- [x] 8.2 测试按住 Run 输入生成 Run intent。
- [x] 8.3 测试 Run 输入单独存在但没有移动输入时不产生移动意图。
- [x] 8.4 测试 Walk 使用 Walk speed。
- [x] 8.5 测试 Run 使用 Run speed。
- [x] 8.6 测试状态机不会因为 Walk/Run 增加 phase。
- [x] 8.7 测试 `MoveLoop` 中 Run 输入变化只切换档位，不强制切回 `MoveStart`。
- [x] 8.8 测试 `MoveStop` 使用 last moving gait 解析停止 alias。
- [x] 8.9 测试 `MoveStop` 中重新输入立即进入 `MoveStart`。
- [x] 8.10 测试 WalkEnd/RunEnd 的 `OnAnimationEnd` 或等价退出事实分别生效。
- [x] 8.11 测试 Walk/Run motion profile 匹配 phase、gait 和 alias。
- [x] 8.12 测试 Presenter 不引用状态图 builder 或具体运动执行实现。
- [x] 8.13 测试 `PlayerLocomotionController` 不引用 Input System 具体类型。

## 9. 验证命令

- [x] 9.1 运行 `openspec validate add-walk-run-locomotion-gait --strict --no-interactive`。
- [ ] 9.2 运行 Unity EditMode 定向测试 `ThirdPersonMovement.Tests.PlayerLocomotionControllerTests`。
- [ ] 9.3 运行新增或更新的 Walk/Run 定向 EditMode 测试。
- [ ] 9.4 检查 Unity Console 没有 C# 编译错误。
- [x] 9.5 如果 Unity MCP 或 Unity 测试不可用，记录原因和手动验证步骤，不伪造结果。

## 10. 手动端到端验证

- [ ] 10.1 打开当前 3C 演示场景。
- [ ] 10.2 不按 Shift，仅按 W/A/S/D，确认角色进入 WalkStart 后进入 WalkLoop。
- [ ] 10.3 普通移动松开输入，确认进入 MoveStop 并播放 WalkEnd。
- [ ] 10.4 按住 Shift + W/A/S/D，确认角色进入 RunStart 后进入 RunLoop。
- [ ] 10.5 Run 移动松开输入，确认进入 MoveStop 并播放 RunEnd。
- [ ] 10.6 MoveLoop 中按下 Shift，确认表现切到 RunLoop 且逻辑 phase 仍为 MoveLoop。
- [ ] 10.7 MoveLoop 中松开 Shift，确认表现切到 WalkLoop 且逻辑 phase 仍为 MoveLoop。
- [ ] 10.8 WalkEnd 或 RunEnd 未结束时重新输入，确认立即进入 MoveStart。
- [ ] 10.9 确认没有第二套角色控制器、第二套移动入口或 BBB 运行时依赖参与。

## 11. 文档收口

- [x] 11.1 更新相关路线文档，说明 Walk/Run 是基础移动档位，不是逻辑 phase。
- [x] 11.2 记录 Sprint 不在本变更内，后续需单独定义能力/FullBody 归属。
- [x] 11.3 完成实现后只把真实完成项标记为 `- [x]`。

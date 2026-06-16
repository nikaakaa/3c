# 正式化 TurnBack Locomotion 状态任务

## 1. 现状确认
- [x] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 读取 `refactor-locomotion-decision-pipeline`，确认 TurnBack intent 仍由统一决策事实提供。
- [x] 1.2A 标记 `refactor-locomotion-decision-pipeline` 中 `MoveStart/MoveStop -> TurnBack` 的宽入口需要被本变更收窄。
- [x] 1.3 读取 `CharacterStateMachineDefinition.CreateDefault`，确认已有 `FullBody/Locomotion/TurnBack` 节点。
- [x] 1.4 读取 `CharacterStateTransitionEvaluator.MoveTurnBackRequested`，确认 evaluator 不临时读取 Transform。
- [x] 1.5 读取 `PlayerLocomotionController.ResolveTurnBackRootMotionFacts`，列出当前散落的 TurnBack motion 特判。
- [x] 1.6 读取 `BasicLocomotionAnimancerPresenter.OnAnimatorMove`，确认 root motion 只在 TurnBack 采集。
- [x] 1.7 读取参考工程 `PlayerSprintingState`、`PlayerReturnRunState`、`CharacterMoveControllerBase.OnAnimatorMove`，确认参考行为模型。
- [x] 1.8 确认第一版只覆盖 Generic/Sandbox，不修改 Humanoid 资源。

## 2. 定义 TurnBack 状态 motion policy 模型
- [x] 2.1 新增或整理纯数据 `TurnBack` motion policy 定义。
- [x] 2.2 policy 至少包含动画 alias、目标 yaw/方向、输入旋转抑制、输入平面位移抑制。
- [x] 2.3 policy 至少包含 yaw source：`AnimationYawWindow` 或等价值。
- [x] 2.4 policy 至少包含 translation source：第一版默认 baked motion profile 或等价烘焙平移。
- [x] 2.5 policy 至少包含 turn complete normalized time。
- [x] 2.6 policy 至少预留 enter fade、start normalized time、lock window 和 exit window 字段。
- [x] 2.7 policy 至少预留 baked motion profile 或等价纯数据资产引用。
- [x] 2.8 policy 不引用 Animator、AnimancerState、Transform、CharacterController。
- [x] 2.9 为 policy 提供默认值：alias 为 `Locomotion.Turn.Back`，输入旋转和平面位移均抑制。
- [x] 2.10 增加 EditMode 测试覆盖默认 policy。
- [x] 2.11 增加 EditMode 测试覆盖 policy 可携带 baked 数据引用但运行时不依赖编辑器 API。

## 3. 状态机输出接入
- [x] 3.1 修改默认 `TurnBack` 节点输出，使其使用 TurnBack motion policy，而不是普通 `InputDrivenMovement`。
- [x] 3.2 默认只允许 `MoveLoop + Run` 消费 TurnBack intent。
- [x] 3.3 禁止 `MoveLoop + Walk` 直接进入 TurnBack。
- [x] 3.4 禁止 `MoveStart` 直接进入 TurnBack。
- [x] 3.5 禁止 `MoveStop` 直接进入 TurnBack。
- [x] 3.6 禁止 `Idle` 直接进入 TurnBack。
- [x] 3.7 进入 TurnBack 时锁定本次目标方向或目标 yaw。
- [x] 3.8 锁定目标不得每帧被相机方向变化覆盖。
- [x] 3.9 TurnBack 输出继续生成 `BasicMovementPhase.TurnBack`。
- [x] 3.10 TurnBack 输出继续请求 `Locomotion.Turn.Back` 动画。
- [x] 3.11 状态机 runner 不直接调用 Animancer。
- [x] 3.12 状态机 runner 不直接调用 motion executor 或 CharacterController。
- [x] 3.13 增加测试覆盖 TurnBack 节点输出 policy。
- [x] 3.14 增加测试覆盖 TurnBack 目标方向进入后保持稳定。
- [x] 3.15 增加测试覆盖只有 RunLoop 可进入 TurnBack。
- [x] 3.16 增加测试覆盖 WalkLoop、MoveStart、MoveStop、Idle 不进入 TurnBack。

## 4. TurnBack 运动命令合成
- [x] 4.1 将 TurnBack motion policy 转换为 `BasicMovementMotionFacts` 或等价运动事实。
- [x] 4.2 TurnBack 期间 `SuppressInputRotation` 必须为 true。
- [x] 4.3 TurnBack 期间 `SuppressInputPlanarMovement` 必须为 true。
- [x] 4.4 第一版 translation source 为 baked motion profile 时，只把烘焙转身窗口平移加入平面位移。
- [x] 4.5 yaw source 使用动画转身窗口事实，确保完整转身可达到目标 yaw。
- [x] 4.6 yaw 贡献仍通过 motion executor 应用到角色根。
- [x] 4.7 没有有效动画 yaw fact 时，输出可诊断 fallback，不静默恢复普通输入旋转。
- [x] 4.8 移除或收口 `PlayerLocomotionController.ResolveTurnBackRootMotionFacts` 中散落的 TurnBack 专项分支。
- [x] 4.9 保持普通 MoveStart/MoveLoop/MoveStop motion profile 行为不变。
- [x] 4.10 增加测试覆盖 TurnBack 使用 baked profile 位移并忽略 runtime root 平移。
- [x] 4.11 增加测试覆盖 TurnBack yaw 通过 motion executor 应用。
- [x] 4.12 增加测试覆盖 TurnBack 期间普通输入位移和旋转均不参与命令。

## 5. 动画外观和 root motion 事实
- [x] 5.1 Presenter 继续只播放 `MovementAnimationContext` 指定 alias。
- [x] 5.2 Presenter 暴露 TurnBack 播放进度事实。
- [x] 5.3 Presenter 可提供 TurnBack 动画 yaw/root motion 事实。
- [x] 5.4 Presenter 不直接切换逻辑状态。
- [x] 5.5 Presenter 不直接写角色根 Transform。
- [x] 5.6 root motion 采集仅作为纯数据事实进入 motion policy。
- [x] 5.7 保留现有 `locomotion-root-motion-delta` 日志。
- [x] 5.8 增加或调整日志输出 yaw source、translation source、turn complete normalized time。
- [x] 5.9 增加测试覆盖 Presenter 同 alias 不重复重播 TurnBack。

## 6. TurnBack 退出窗口
- [x] 6.1 用 policy 的 turn complete normalized time 产生 TurnBack 可退出事实。
- [x] 6.2 TurnBack 达到转完点且仍有移动输入时退出到 MoveLoop。
- [x] 6.3 TurnBack 达到转完点且没有移动输入时退出到 Idle。
- [x] 6.4 不要求等整段 TurnBack 动画播放结束。
- [x] 6.5 退出后普通 MoveLoop 立即恢复输入位移和输入旋转。
- [x] 6.6 退出后不继续消费 TurnBack 后半段跑步尾巴位移。
- [x] 6.7 增加测试覆盖未到转完点不能退出。
- [x] 6.8 增加测试覆盖到转完点后有输入回 MoveLoop。
- [x] 6.9 增加测试覆盖到转完点后无输入回 Idle。

## 7. 配置和资产边界
- [x] 7.1 第一版不要求用户删除 RootT/RootQ 曲线。
- [x] 7.2 第一版不要求强制使用编辑器生成的 turn-only clip。
- [x] 7.3 如使用 turn-only clip，只能作为可选资源替换，不改变运行时状态契约。
- [x] 7.4 保持 `Corin_TurnBack.asset` 或等价 TransitionAsset 仍通过现有 Animancer 配置进入。
- [x] 7.5 不把 TurnBack 动画硬编码到非配置路径。
- [x] 7.6 预留 baked motion profile 资产路径，用于后续保存 yaw、translation、marker 和 entry/exit timing。
- [x] 7.7 预留 editor authoring 边界：编辑器可生成/更新 baked motion profile，但运行时代码只读纯数据。
- [x] 7.7A 为 Generic TurnBack 生成 0.47 秒 baked motion profile，并绑定到 Run locomotion animation config。
- [x] 7.7B 为 Generic TurnBack 生成 TurnOnly 视觉 clip，清除 root 平面位移但保留高度和骨骼表现。
- [x] 7.8 增加配置校验或测试覆盖缺失 TurnBack alias 时给出诊断。
- [x] 7.9 增加配置校验或测试覆盖非法 entry/exit timing 时给出诊断。
- [x] 7.10 移除运行时生成默认状态机 fallback，缺少正式配置时只输出诊断并停止状态机更新。
- [x] 7.11 移除空状态机资产自动回退默认图的行为，空资产视为配置错误。
- [x] 7.12 将相关测试改为加载正式 `DefaultCharacterStateMachine.asset`，不再依赖代码生成默认图。

## 8. 诊断日志
- [x] 8.1 增加 `locomotion-turnback-state-policy` 或等价日志。
- [x] 8.2 日志输出状态路径、alias、locked target yaw、current yaw、yaw source、translation source。
- [x] 8.3 日志输出 suppress input rotation 和 suppress input planar movement。
- [x] 8.4 日志输出 normalized time、turn complete normalized time 和 can exit。
- [x] 8.5 日志继续受现有诊断系统开关控制。
- [x] 8.6 不删除已有 TurnBack/root motion 日志。

## 9. 自动验证
- [x] 9.1 运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`。
- [x] 9.2 运行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`。
- [x] 9.3 使用 Unity Test Runner 定向运行统一状态机 EditMode 测试。
- [x] 9.4 使用 Unity Test Runner 定向运行 TurnBack motion policy/EditMode 测试。
- [x] 9.5 使用 Unity Test Runner 定向运行 BasicLocomotionAnimation 相关 EditMode 测试。
- [x] 9.6 读取 Unity Console，确认相关 error 为 0。
- [x] 9.7 不运行 Unity batchmode。
- [x] 9.8 搜索确认状态机 `CreateDefault`、`CreateDefaultDefinition`、`ResetToDefault` fallback 入口已移除。

## 10. Sandbox 手动验证
- [ ] 10.1 打开 Sandbox 场景并使用 Generic 可琳。
- [ ] 10.2 启用 Locomotion、Animation 相关诊断日志。
- [ ] 10.3 按 W 跑动后切 S，确认进入 `FullBody/Locomotion/TurnBack`。
- [ ] 10.4 在 Walk 或未进入 RunLoop 时前后切换，确认不触发 TurnBack 动画。
- [ ] 10.5 在 MoveStart 和 MoveStop 窗口前后切换，确认不直接触发 TurnBack 动画。
- [ ] 10.6 TurnBack 期间确认普通输入位移和普通输入旋转均被抑制。
- [ ] 10.7 观察角色朝向完成约 180 度反向。
- [ ] 10.8 观察到转完点后快速回到 MoveLoop，不继续拖动画跑步尾巴。
- [ ] 10.9 持续按反向输入时确认回到普通移动速度，不再慢速倒走。
- [ ] 10.10 松开输入时确认 TurnBack 转完后回 Idle。
- [ ] 10.11 横向 A/D 切换不应误触发前后 TurnBack。
- [ ] 10.12 复制搜索 `locomotion-turnback-state-policy|turnback-root-motion-consumed|animation-motion-executor|locomotion-animation-played` 验证日志。

## 11. 收尾
- [x] 11.1 运行 `openspec validate formalize-turnback-locomotion-state --strict --no-interactive`。
- [x] 11.2 更新相关调试文档，记录 TurnBack 正式状态链路。
- [x] 11.3 检查是否需要更新 Path 文档；纯日志或测试不更新。
- [x] 11.4 确认没有恢复 TurnInPlace、MovingPivotTurn 或旧的散落式 baked yaw/profile 路线。
- [ ] 11.5 确认全部任务真实完成后再将 checklist 标为 `- [x]`。

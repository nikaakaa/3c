# 统一 Locomotion 决策管线任务

## 1. 现状确认
- [x] 1.1 读取 `proposal.md`、`design.md` 和全部 spec delta。
- [x] 1.2 确认 `refactor-turnback-intent-capture` 不存在，避免 TurnBack 专项分裂 proposal。
- [x] 1.3 读取 `add-moving-pivot-turn`，确认本变更不恢复旧 TurnInPlace/MovingPivot 路线。
- [x] 1.4 读取 `PlayerLocomotionController.TryEvaluateWithStateMachine`，标注当前隐式阶段顺序。
- [x] 1.5 读取 `BasicLocomotionPipeline.Tick`，确认现有 command 构建边界。
- [x] 1.6 读取 `CharacterStateMachineContext` 和 transition evaluator，确认状态机只应读取纯数据 facts。
- [x] 1.7 读取现有 TurnBack/root motion tests，列出会被替换或扩展的用例。

## 2. 定义统一帧事实
- [x] 2.1 新增 `LocomotionSpatialFacts` 或等价纯数据结构。
- [x] 2.2 `LocomotionSpatialFacts` 至少包含 world move direction、facing forward 和 camera basis 使用结果。
- [x] 2.3 新增 `LocomotionDecisionFacts` 或等价纯数据结构。
- [x] 2.4 `LocomotionDecisionFacts` 至少包含 movement intent、gait candidate、phase facts、spatial facts 和 TurnBack intent。
- [x] 2.5 确保事实结构不引用 Transform、Animator、AnimancerState、InputAction、CharacterController。
- [x] 2.6 为事实结构提供无效/空事实默认值。
- [x] 2.7 增加 EditMode 测试覆盖事实默认值和方向归一化。

## 3. 显式化 Locomotion 阶段
- [x] 3.1 在 `PlayerLocomotionController` 内拆出 `ResolveMovementIntent` 或等价方法。
- [x] 3.2 拆出 `ResolveSpatialFacts` 或等价方法。
- [x] 3.3 拆出 `DeriveLocomotionDecisionFacts` 或等价方法。
- [x] 3.4 拆出 `BuildStateMachineContext` 或等价方法。
- [x] 3.5 保持 `TryEvaluateWithStateMachine` 仍是同一主入口，不新增第二 controller。
- [x] 3.6 保持 `ExecuteLocomotionMotion` 和 `PresentLocomotionAnimation` 仍只消费 frame/command/context。
- [x] 3.7 增加测试确认阶段方法不会直接调用 motion executor 或 Animancer。
- [x] 3.8 `BasicLocomotionPipeline` 消费统一 `LocomotionDecisionFacts`，不再重新解析 raw input 或 camera basis。

## 4. TurnBack 作为管线派生事实
- [x] 4.1 在 `DeriveLocomotionDecisionFacts` 中捕获 TurnBack intent。
- [x] 4.2 捕获输入只使用 movement intent、world move direction、facing forward、当前 step、阈值和窗口 step。
- [x] 4.3 TurnBack intent 使用人物当前平面朝向与当前世界移动输入方向夹角。
- [x] 4.4 TurnBack intent 不使用上一有效移动方向作为触发来源。
- [x] 4.5 TurnBack intent 支持短 step 窗口。
- [x] 4.6 W/S 切换中的一帧无输入不应立刻丢失已捕获 TurnBack intent。
- [x] 4.7 TurnBack intent 过期后必须失效。
- [x] 4.8 进入 TurnBack 后必须清理或标记已消费 TurnBack intent。
- [x] 4.9 增加测试覆盖反向样本捕获。
- [x] 4.10 增加测试覆盖 W/S 空输入窗口。
- [x] 4.11 增加测试覆盖非反向输入不捕获。

## 5. 状态机 context 消费统一 facts
- [x] 5.1 扩展 `CharacterStateMachineContext` 携带 `LocomotionDecisionFacts` 或等价事实。
- [x] 5.2 保留兼容构造器，默认生成空 facts。
- [x] 5.3 修改 `MoveTurnBackRequested` 读取 facts 中的 TurnBack intent。
- [x] 5.4 transition evaluator 不再以即时 `FacingForward` 和 `WorldMoveDirection` 夹角作为唯一触发来源。
- [x] 5.5 保持 evaluator 不读取 Transform、InputAction、Animator、CharacterController。
- [x] 5.6 增加测试覆盖 valid TurnBack intent 能触发。
- [x] 5.7 增加测试覆盖无 TurnBack intent 不触发。

## 6. 默认状态机接入
- [x] 6.1 保留 `MoveLoop -> TurnBack`。
- [x] 6.2 增加 `MoveStart -> TurnBack`。
- [x] 6.3 增加 `MoveStop -> TurnBack`。
- [x] 6.4 不增加 `Idle -> TurnBack`。
- [x] 6.5 保持 TurnBack 退出仍由 `LocomotionAnimationCanExit` 控制。
- [x] 6.6 增加测试覆盖 MoveStart 可进入 TurnBack。
- [x] 6.7 增加测试覆盖 MoveStop 可进入 TurnBack。
- [x] 6.8 增加测试覆盖 Idle 不直接进入 TurnBack。

## 7. 统一运动和动画链路保持
- [x] 7.1 确认 TurnBack 进入后仍输出 `BasicMovementPhase.TurnBack`。
- [x] 7.2 确认 TurnBack 仍播放 `Locomotion.Turn.Back`。
- [x] 7.3 确认 TurnBack 仍 suppress 输入旋转。
- [x] 7.4 确认 TurnBack 仍 suppress 输入平面位移。
- [x] 7.5 确认 TurnBack root motion delta 仍进入 `MovementCommand`。
- [x] 7.6 确认 motion executor 仍是唯一运动出口。
- [x] 7.7 静态检查没有恢复 TurnInPlace、MovingPivotTurn、baked yaw/profile。
- [x] 7.8 增加测试确认 motion command 使用同一份 decision facts/world direction，不再在 command 构建阶段重算相机方向。
- [x] 7.9 修正 Generic TurnBack root motion 接管：TurnBack 时打开 Animator root motion 评价，仍由 `OnAnimatorMove` 采集并由 motion executor 应用到角色根。

## 8. Tick 和回滚边界
- [x] 8.1 确认 `LocomotionTickAdapter` 仍只调用 `PlayerLocomotionController` 主入口。
- [x] 8.2 确认 simulation tick phase 顺序不被本变更重排。
- [x] 8.3 确认 rollback/replay 输入仍先转为 `BasicLocomotionInputSnapshot`。
- [x] 8.4 如 snapshot 需要保存新 facts，只保存纯数据，不保存 Unity 对象。
- [x] 8.5 增加或更新 rollback 相关构造测试。

## 8A. FullBody Action 接入统一 facts
- [x] 8A.1 确认 `PlayerFullBodyActionController` 先调用 `TryPrepareDecisionFrame`，再构建 Action 输入请求。
- [x] 8A.2 确认 FullBody 状态机推进复用同一份 `LocomotionDecisionFrame`。
- [x] 8A.3 将动画播放进度推进收进 `TryPrepareDecisionFrame`，确保普通 Locomotion 和 FullBody 调度共用同一 prepare 时序。
- [x] 8A.4 增加静态测试防止动画播放进度推进再次回到单一路径外层。
- [x] 8A.5 修改 Dodge request builder，使 directional dodge 使用 `LocomotionDecisionFacts.SpatialFacts.WorldMoveDirection`。
- [x] 8A.6 修改 Dodge request builder，使 backstep 使用 `LocomotionDecisionFacts.SpatialFacts.FacingForward`。
- [x] 8A.7 确认 Action gate 不再引用 `ICameraMovementBasisProvider`。
- [x] 8A.8 确认 Action gate 不再调用 `CameraRelativeMovementResolver`。
- [x] 8A.9 确认 Action gate 不再调用 `MovementInputIntent.FromRaw`。
- [x] 8A.10 增加测试覆盖 raw input 与 facts world direction 不一致时，Dodge 使用 facts world direction。
- [x] 8A.11 增加静态测试防止 Action gate 恢复 raw input、camera basis 或 facing provider 重算路径。

## 9. 诊断日志
- [x] 9.1 增加 `locomotion-decision-pipeline` 或等价阶段日志。
- [x] 9.2 日志包含 input、intent、spatial facts、derived facts、state decision 的关键摘要。
- [x] 9.3 增加 `locomotion-turnback-intent` 子日志，包含 angle、threshold、origin、expire、valid、consume/clear reason。
- [x] 9.4 保留现有 root motion、animation-motion executor 和 state machine probe 日志。
- [x] 9.5 日志开关继续走现有诊断系统。
- [x] 9.6 更新 `animator-root-motion-policy` 日志，输出 `animatorApplyRootMotionBefore/After` 以确认 Generic TurnBack root motion 评价入口。

## 10. 自动验证
- [x] 10.1 运行 `dotnet build Assembly-CSharp.csproj --no-restore -v:minimal`。
- [x] 10.2 运行 `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`。
- [ ] 10.3 使用 Unity Test Runner 定向运行统一状态机 EditMode 测试。
- [ ] 10.4 使用 Unity Test Runner 定向运行 Locomotion/TurnBack/root motion EditMode 测试。
- [ ] 10.5 读取 Unity Console，确认相关 error 为 0。
- [x] 10.6 不运行 Unity batchmode。
- [ ] 10.7 当前工具不能直接连接 Unity Test Runner/Console；10.3-10.5 需在 Unity Editor 中执行。

## 11. Sandbox 手动验证
- [ ] 11.1 打开 Sandbox 场景。
- [ ] 11.2 启用 Locomotion、Animation、Input 相关诊断日志开关。
- [ ] 11.3 前进移动后切后退，确认先出现统一管线 derived facts，再出现 TurnBack 状态。
- [ ] 11.4 横向 A/D 切换不应误触发前后 TurnBack。
- [ ] 11.5 TurnBack 期间确认 `appliedInputRotation=False`。
- [ ] 11.6 TurnBack 期间确认 `appliedInputPlanarMovement=False`。
- [ ] 11.7 动画结束后确认回到 MoveLoop 或 Idle。

## 12. 文档和收尾
- [x] 12.1 更新 `docs/agents/turnback-rootmotion-debug-log.md`，记录统一管线后的 TurnBack 触发链路。
- [x] 12.2 如 Path 文档涉及管线顺序，按 `update-path-docs` 规则检查是否需要更新。
- [x] 12.3 运行 `openspec validate refactor-locomotion-decision-pipeline --strict --no-interactive`。
- [ ] 12.4 确认全部任务完成后再将 checklist 改为 `- [x]`。

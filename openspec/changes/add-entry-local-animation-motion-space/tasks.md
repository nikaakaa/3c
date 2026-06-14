# EntryLocal 动画运动坐标空间任务

## 1. 现状确认
- [ ] 1.1 读取本变更 `proposal.md`、`design.md` 和全部 spec delta。
- [ ] 1.2 读取 `add-animation-motion-source-pipeline`，确认 TurnBack 当前默认 source 为 `TickSampledMotion`。
- [ ] 1.3 读取 `LocomotionMotionProfileBakeUtility`，确认 profile 平面曲线是起点归零后的累计 local 数据。
- [ ] 1.4 读取 `AnimationMotionProfileSampler`，确认 runtime delta 由累计曲线差分得到。
- [ ] 1.5 读取 `PlayerLocomotionController.ResolveTurnBackRootMotionFacts`，确认 TurnBack profile delta 当前标记空间。
- [ ] 1.6 读取 `CharacterControllerBasicMotionExecutor.ResolveAnimationWorldDelta`，确认 `Local` 当前按 root 当前朝向解释。
- [ ] 1.7 读取状态机 restore state，确认目前只保存 `TurnBackWorldDirection`，没有保存 entry basis。

## 2. 纯数据模型
- [ ] 2.1 在 planar delta space 中正式加入 `EntryLocal`。
- [ ] 2.2 在 `BasicMovementMotionFacts` 中加入 normalized entry planar basis forward。
- [ ] 2.3 在 `MovementCommand` 中传递 entry planar basis forward。
- [ ] 2.4 对无效 basis 做纯数据归零处理，不在模型层读取 Transform。
- [ ] 2.5 保持 `Local` 和 `World` 默认构造兼容。

## 3. 状态机进入基准
- [ ] 3.1 在 TurnBack transition 进入时捕获 entry facing forward。
- [ ] 3.2 将 entry basis 与现有 `TurnBackWorldDirection` 分开保存，避免把输入锁定方向误当作运动基准。
- [ ] 3.3 将 entry basis 写入 `CharacterStateMachineFrame`。
- [ ] 3.4 将 entry basis 写入 `CharacterStateMachineRestoreState`。
- [ ] 3.5 在 restore 后恢复 entry basis，保证 rollback replay 不重新从当前 Transform 推导。
- [ ] 3.6 离开 TurnBack 时清空 entry basis。

## 4. TurnBack motion facts
- [ ] 4.1 TurnBack 使用 baked profile translation 时将 delta space 设为 `EntryLocal`。
- [ ] 4.2 TurnBack 将状态机 entry basis 传入 motion facts。
- [ ] 4.3 缺少有效 entry basis 时输出诊断并不静默改成 `Local`。
- [ ] 4.4 保持 yaw source 为 `BakedMotionProfile`，并继续按 sampled yaw delta 应用。
- [ ] 4.5 保持 motion window inactive 时不应用 profile 尾部 delta。
- [ ] 4.6 保持普通输入旋转和平面位移 suppress 语义不变。

## 5. Motion executor
- [ ] 5.1 为 `EntryLocal` 增加 world delta 解析：使用 entry forward/right 映射 local X/Z。
- [ ] 5.2 保持 `Local` 使用当前 root transform 解析。
- [ ] 5.3 保持 `World` 直接使用输入 delta。
- [ ] 5.4 basis 缺失时不隐式退回当前 root local，并输出可诊断结果。
- [ ] 5.5 日志加入 entry basis forward/right、delta space 和 resolved world delta。

## 6. 自动测试
- [ ] 6.1 增加 executor 测试：`EntryLocal` 在 root yaw 改变后仍按固定进入基准解析 delta。
- [ ] 6.2 增加 executor 测试：`Local` 行为不变，仍按当前 root yaw 解析 delta。
- [ ] 6.3 增加 executor 测试：`World` 行为不变，不旋转 delta。
- [ ] 6.4 更新 TurnBack 测试：profile translation command space 为 `EntryLocal`。
- [ ] 6.5 增加 TurnBack 测试：command 携带 entry basis，而不是 locked input direction。
- [ ] 6.6 增加状态机 restore 测试：TurnBack entry basis capture/restore 后保持一致。
- [ ] 6.7 增加本地 rollback/replay 测试：同一 TurnBack 输入重放后 root pose 收敛。
- [ ] 6.8 更新静态测试：没有新增 TurnBack 专用 executor 或第二 controller。

## 7. 编译和工具验证
- [ ] 7.1 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp.csproj --no-restore -v:minimal`。
- [ ] 7.2 运行 `dotnet build 3cDemo/Client/3C_Client/Assembly-CSharp-Editor.csproj --no-restore -v:minimal`。
- [ ] 7.3 使用 Unity Test Runner 定向运行 `UnifiedCharacterStateMachineTests`。
- [ ] 7.4 使用 Unity Test Runner 定向运行相关 Simulation rollback EditMode 测试。
- [ ] 7.5 运行 `openspec validate add-entry-local-animation-motion-space --strict --no-interactive`。
- [ ] 7.6 确认没有运行 Unity batchmode。

## 8. 手动验证
- [ ] 8.1 打开 Sandbox 或当前 TurnBack 验证场景。
- [ ] 8.2 启用 Locomotion、Animation 相关诊断 channel。
- [ ] 8.3 按 W 进入 RunLoop 后切 S，确认进入 `FullBody/Locomotion/TurnBack`。
- [ ] 8.4 搜索 `turnback-root-motion-consumed`，确认 `deltaSpace=EntryLocal`。
- [ ] 8.5 搜索 `animation-motion-executor`，确认 entry basis、animationLocalDelta、animationWorldDelta 和 actualRootDelta 方向一致。
- [ ] 8.6 确认 `presenter-delta-ignored` 仍只作为诊断出现，没有 pending root delta 被消费。
- [ ] 8.7 观察 TurnBack 期间角色不再倒向位移，且 yaw 仍按 profile 转身。
- [ ] 8.8 观察 motion window 结束后不继续应用 TurnBack root motion。
- [ ] 8.9 观察 exit window 后能回到 MoveLoop 或 Idle。
- [ ] 8.10 若动画在 normalized 约 0.67 提前结束，记录 timeline/TransitionAsset 配置，不通过代码旁路修正。

## 9. OpenSpec 收尾
- [ ] 9.1 对照 proposal 确认没有实现未审批 fallback 或第二运动路径。
- [ ] 9.2 全部自动和手动验证通过后再把任务勾选为完成。
- [ ] 9.3 用户确认测试通过后再归档本 change。

## 1. Scope Review
- [x] 1.1 读取本 proposal、design 和 spec deltas，确认只实现预测回滚权威域/比较域骨架。
- [x] 1.2 对照 `formalize-animation-playback-rollback-authority`，确认本变更不重新实现 TurnBack playback restore。
- [x] 1.3 对照 `add-entry-local-animation-motion-space`，确认本变更不修改 EntryLocal 坐标空间。
- [x] 1.4 对照 `add-animation-motion-source-pipeline`，确认本变更只引用运动来源语义，不新增 Animator root motion fallback。
- [x] 1.5 记录当前 comparer 中 `differences`、`presentationDifferences` 和 TurnBack hardcoded strict 的现状。

## 2. Authority Model
- [x] 2.1 定义 `AnimationAuthority` 或等价纯数据枚举。
- [x] 2.2 定义 `MotionAuthority` 或等价纯数据枚举。
- [x] 2.3 定义 `RollbackCompareScope` 或等价纯数据枚举。
- [x] 2.4 定义字段级或状态级 authority/scope 描述模型。
- [x] 2.5 确认模型不引用 Unity Object、Animancer runtime object、AnimationClip、TransitionAsset 或场景实例。
- [x] 2.6 为 TurnBack、MoveLoop、Action animation time 建立初始默认矩阵。

## 3. Snapshot Comparison Scope
- [x] 3.1 保留 strict gameplay differences 作为 `Matches` 的唯一失败依据。
- [x] 3.2 保留 presentation differences 作为诊断输出，不让其导致 strict fail。
- [x] 3.3 将 root position/yaw、motion executor root pose、状态机、locomotion/action gameplay facts 标为 strict。
- [x] 3.4 将 MoveLoop 视觉 normalized time 标为 presentation drift。
- [x] 3.5 将 Action animation normalized time 默认标为 presentation drift。
- [x] 3.6 将 TurnBack profile playback/window 相关 animation progress 标为 strict。
- [x] 3.7 移除或收口 comparer 内的状态/alias 硬编码，使其通过统一 scope resolver 判断。
- [x] 3.8 确认 first mismatch 优先记录 strict mismatch，只有没有 strict mismatch 时才记录 first presentation drift。

## 4. Runtime Blackboard Boundary
- [x] 4.1 标记 blackboard 中 Locomotion facts 的 strict gameplay 字段。
- [x] 4.2 标记 blackboard 中 Action facts 的 strict gameplay 字段。
- [x] 4.3 标记 blackboard 中 Animation facts 的 gameplay/presentation 字段。
- [x] 4.4 确认黑板 snapshot/restore 仍保存纯数据 facts。
- [x] 4.5 确认 blackboard 不自行决定 compare scope，只提供可被 scope resolver 读取的 facts。

## 5. Synctest and Soak Diagnostics
- [x] 5.1 更新 F6 pass 日志，包含 presentation drift 摘要但保持 PASS。
- [x] 5.2 更新 F6 fail 日志，明确 strict differences 和 presentationDifferences。
- [x] 5.3 更新 first mismatch 日志，区分 `first-mismatch` 和 `first-presentation-drift`。
- [x] 5.4 更新 F8 soak 结果，保留 strict failure 与 presentation drift 统计。
- [x] 5.5 确认 `LocalRollbackSynctestResult.Success` 只由 strict gameplay mismatch 决定。
- [x] 5.6 确认诊断日志能看到 action animation normalized time 的 expected/actual 摘要。

## 6. Automated Tests
- [x] 6.1 增加 comparer 测试：position/yaw 差异仍 strict fail。
- [x] 6.2 增加 comparer 测试：MoveLoop normalized time 差异只进入 presentation drift。
- [x] 6.3 增加 comparer 测试：TurnBack normalized time 差异进入 strict differences。
- [x] 6.4 增加 comparer 测试：Action animation normalized time 默认只进入 presentation drift。
- [x] 6.5 增加 runner 测试：只有 presentation drift 时 synctest PASS 且记录 first drift。
- [x] 6.6 增加 runner 测试：strict mismatch 优先于 presentation drift。
- [x] 6.7 增加 log formatter 测试：PASS/FAIL 都输出 scope 分组。
- [x] 6.8 增加 soak runner 测试：presentation drift 不触发 failure。
- [x] 6.9 增加静态边界测试：scope model 不引用表现层 runtime 类型。

## 7. Validation
- [x] 7.1 运行 C# 编译检查或 Unity Editor 编译日志检查。
- [x] 7.2 运行定向 EditMode 测试：`LocalRollbackSynctestFoundationTests`。
- [x] 7.3 运行定向 EditMode 测试：`FullBodyRollbackReplayTests`。
- [x] 7.4 运行新增 authority/scope 测试。
- [x] 7.5 运行 OpenSpec 校验：`openspec validate add-prediction-rollback-authority-scopes --strict --no-interactive`。

## 8. Manual Verification
- [ ] 8.1 Play Mode 触发 F6，确认只有 WalkLoop/Action animation drift 时输出 PASS + presentationDifferences。
- [ ] 8.2 Play Mode 制造或复现 TurnBack playback/profile 分叉，确认 F6 仍 FAIL。
- [ ] 8.3 Play Mode 触发 F8 soak，确认 presentation drift 不作为 failure，strict mismatch 仍停止或报告。
- [ ] 8.4 手动验证 WASD、Run、TurnBack、Dodge、Action 现有视觉行为不回退。
- [ ] 8.5 若失败，复制 `Simulation.synctest-first-mismatch`、`Simulation.synctest-fail-detail`、`Simulation.rollback-soak-result` 日志。

## 9. Completion
- [x] 9.1 确认没有新增第二套角色控制器、第二套 replay 或未审批 fallback。
- [x] 9.2 确认没有删除现有诊断日志。
- [ ] 9.3 确认 tasks.md 只在全部完成后统一勾选。

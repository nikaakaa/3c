## 1. Scope Review
- [ ] 1.1 读取本 proposal、design 和 spec deltas，确认只实现动画播放进度回滚权威。
- [ ] 1.2 对照 `add-entry-local-animation-motion-space`，确认本变更不修改 EntryLocal 坐标空间定义。
- [ ] 1.3 对照 `harden-local-prediction-rollback-tooling`，确认 F6/F8 工具只作为验收入口，不新增第二套 replay。
- [ ] 1.4 记录当前 F6 mismatch 样本中的 first mismatch 字段，作为回归前置证据。

## 2. Runtime State Audit
- [ ] 2.1 审计 `CharacterSimulationSnapshot` 中 animation progress 字段。
- [ ] 2.2 审计 `LocomotionRuntimeRollbackState` 中 previous motion playback progress 字段。
- [ ] 2.3 审计 `CharacterRuntimeBlackboardRestoreState` 中 locomotion animation facts。
- [ ] 2.4 审计 `PlayerLocomotionController.CaptureSimulationSnapshot` 的 capture 顺序。
- [ ] 2.5 审计 `PlayerLocomotionController.RestoreSimulationSnapshot` 的 restore 顺序。
- [ ] 2.6 审计 `BasicLocomotionAnimancerPresenter.RestorePlaybackProgress` 的 current state 重建行为。
- [ ] 2.7 审计 `BasicLocomotionAnimancerPresenter.Present` 的 same alias early-return 条件。
- [ ] 2.8 审计 `RestartOneShotStateIfNeeded` 的归零触发条件。

## 3. Playback Authority Implementation
- [ ] 3.1 明确首次进入 TurnBack 的 restart 标记或等价判定。
- [ ] 3.2 明确 rollback restore resume 的标记或等价判定。
- [ ] 3.3 确保 restore 后的 current phase/gait/alias/state 与 snapshot 进度一致。
- [ ] 3.4 确保同 alias `Present` 不覆盖 restore 后 normalized time。
- [ ] 3.5 确保真实新进入 TurnBack 仍从 policy `StartNormalizedTime` 开始。
- [ ] 3.6 确保 MoveStart、MoveStop、RunEnd 等非 TurnBack 现有播放语义不回退。
- [ ] 3.7 保持 Presenter 不写 Transform、不调用 motion executor、不消费 Animator root delta。

## 4. Sampling Window Implementation
- [ ] 4.1 确认 capture 时保存 current playback progress。
- [ ] 4.2 确认 capture 时保存 previous motion playback progress。
- [ ] 4.3 确认 restore 时先恢复 current progress，再恢复 previous window 或明确说明顺序。
- [ ] 4.4 确认 restore 后第一 replay tick 不把 previous window 重置为 0。
- [ ] 4.5 确认 phase/alias 真实变化时仍重置 sampling window。
- [ ] 4.6 确认 normalized time 回退且非 restore resume 时仍视为新播放段。
- [ ] 4.7 补充诊断日志字段或复用现有日志显示 previous/current window。

## 5. Automated Tests
- [ ] 5.1 增加首次进入 TurnBack 归零测试。
- [ ] 5.2 增加 restore 到 TurnBack 中段不归零测试。
- [ ] 5.3 增加 previous/current sampling window restore 测试。
- [ ] 5.4 增加 restore 后第一 replay tick profile delta 一致测试。
- [ ] 5.5 增加 FullBody replay 通过正式 `FullBodyRollbackSimulation` 的 TurnBack 中段恢复测试。
- [ ] 5.6 增加 Presenter restore 后同 alias `Present` 不 restart 测试。
- [ ] 5.7 增加真实新 alias 播放仍 restart 测试。
- [ ] 5.8 更新或新增静态边界测试，确认没有 `OnAnimatorMove` pending delta 进入 rollback state。
- [ ] 5.9 更新或新增日志格式测试，确认 first mismatch 可看到 playback/window 差异。

## 6. Validation
- [ ] 6.1 运行相关 C# 编译检查或 Unity Editor 编译日志检查。
- [ ] 6.2 运行定向 EditMode 测试：`LocalRollbackSynctestFoundationTests`。
- [ ] 6.3 运行定向 EditMode 测试：`FullBodyRollbackReplayTests`。
- [ ] 6.4 运行新增 TurnBack playback rollback 测试。
- [ ] 6.5 运行 OpenSpec 校验：`openspec validate formalize-animation-playback-rollback-authority --strict --no-interactive`。

## 7. Manual Verification
- [ ] 7.1 Play Mode 触发 TurnBack，确认首次进入仍从动画开头播放。
- [ ] 7.2 TurnBack 中段按 F6，确认 `[rollback-synctest] PASS` 或 first mismatch 不再是 `animationNormalizedTime=0`。
- [ ] 7.3 TurnBack 中段连续触发 F6 多次，确认 replay 后画面不会逐次漂移。
- [ ] 7.4 触发 F8 soak，确认 TurnBack 窗口不因 playback/window 分叉失败。
- [ ] 7.5 若失败，复制 `Simulation.synctest-first-mismatch`、`Simulation.synctest-fail-detail` 和 `TURNBACK_RM_CHAIN` 日志。

## 8. Completion
- [ ] 8.1 确认所有实现任务完成且没有新增未审批 fallback 配置。
- [ ] 8.2 确认没有删除现有诊断日志。
- [ ] 8.3 确认 tasks.md 只在全部完成后统一勾选。

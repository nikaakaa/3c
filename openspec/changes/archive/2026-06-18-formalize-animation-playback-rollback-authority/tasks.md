## 1. Scope Review
- [x] 1.1 读取本 proposal、design 和 spec deltas，确认只实现业务声明的动画驱动采样窗口回滚边界。
- [x] 1.2 对照当前 `character-frame-rollback-replay` 和 `animation-motion-source-pipeline` 规格，确认本变更不修改 EntryLocal 坐标空间定义。
- [x] 1.3 对照当前 `local-rollback-synctest-foundation` 规格，确认 F6/F8 工具只作为验收入口，不新增第二套 replay。
- [x] 1.4 记录当前 F6 mismatch 样本中的 first mismatch 字段，作为 sampled motion 回归前置证据。
- [x] 1.5 对齐 `wasd-locomotion-pipeline` 中 TurnBack 旧 `OnAnimatorMove` 口径，确认正式 simulation motion source 是 `TickSampledMotion`。

## 2. Runtime State Audit
- [x] 2.1 审计 `CharacterSimulationSnapshot` 中 sampled motion playback window 字段。
- [x] 2.2 审计 `LocomotionRuntimeRollbackState` 中 previous sampled motion playback progress 字段。
- [x] 2.3 审计 `CharacterRuntimeBlackboardRestoreState` 中 sampled motion animation facts。
- [x] 2.4 审计 `CharacterFrameRuntimeController.CaptureSimulationSnapshot` 的 capture 顺序。
- [x] 2.5 审计 `CharacterFrameRuntimeController.RestoreSimulationSnapshot` 的 restore 顺序。
- [x] 2.6 审计 `BasicLocomotionAnimancerPresenter.RestorePlaybackProgress` 的 sampled motion state 重建行为。
- [x] 2.7 审计 `BasicLocomotionAnimancerPresenter.Present` 的 same alias early-return 条件。
- [x] 2.8 审计 `RestartOneShotStateIfNeeded` 的归零触发条件。

## 3. Sampled Motion Playback Implementation
- [x] 3.1 明确首次进入 TurnBack 的 restart 标记或等价判定。
- [x] 3.2 明确 rollback restore resume 的标记或等价判定。
- [x] 3.3 确保 restore 后的 current phase/gait/alias/state 与 snapshot 进度一致。
- [x] 3.4 确保同 alias `Present` 不覆盖 restore 后 normalized time。
- [x] 3.5 确保真实新进入 TurnBack 仍从 policy `StartNormalizedTime` 开始。
- [x] 3.6 确保 MoveStart、MoveStop、RunEnd 等非 TurnBack 现有播放语义不回退。
- [x] 3.7 保持 Presenter 不写 Transform、不调用 motion executor、不消费 Animator root delta。
- [x] 3.8 增加纯表现动画不进入 sampled motion rollback authority 的断言或静态检查。

## 4. Sampling Window Implementation
- [x] 4.1 确认 capture 时保存 current playback progress。
- [x] 4.2 确认 capture 时保存 previous motion playback progress。
- [x] 4.3 确认 restore 时先恢复 current progress，再恢复 previous window 或明确说明顺序。
- [x] 4.4 确认 restore 后第一 replay tick 不把 previous window 重置为 0。
- [x] 4.5 确认 phase/alias 真实变化时仍重置 sampling window。
- [x] 4.6 确认 normalized time 回退且非 restore resume 时仍视为新播放段。
- [x] 4.7 补充诊断日志字段或复用现有日志显示 previous/current window。

## 5. Automated Tests
- [x] 5.1 增加首次进入 TurnBack 归零测试。
- [x] 5.2 增加 restore 到 TurnBack 中段不归零测试。
- [x] 5.3 增加 previous/current sampling window restore 测试。
- [x] 5.4 增加 restore 后第一 replay tick profile delta 一致测试。
- [x] 5.5 增加 Character frame replay 通过正式 `CharacterFrameRollbackSimulation` 的 TurnBack 中段恢复测试。
- [x] 5.6 增加 Presenter restore 后同 alias `Present` 不 restart 测试。
- [x] 5.7 增加真实新 alias 播放仍 restart 测试。
- [x] 5.8 增加纯表现动画不要求逐 tick 确定、不进入 sampled motion snapshot 的测试或静态边界验证。
- [x] 5.9 更新或新增静态边界测试，确认没有 `OnAnimatorMove` pending delta 进入 rollback state。
- [x] 5.10 更新或新增日志格式测试，确认 first mismatch 可看到 sampled motion playback/window 差异。

## 6. Validation
- [x] 6.1 运行相关 C# 编译检查或 Unity Editor 编译日志检查。
- [x] 6.2 运行定向 EditMode 测试：`LocalRollbackSynctestFoundationTests`。
- [x] 6.3 运行定向 EditMode 测试：`CharacterFrameRollbackReplayTests`。
- [x] 6.4 运行新增 TurnBack playback rollback 测试。
- [x] 6.5 运行 OpenSpec 校验：`openspec validate formalize-animation-playback-rollback-authority --strict --no-interactive`。

## 7. Completion
- [x] 7.1 确认所有实现任务完成且没有新增未审批 fallback 配置。
- [x] 7.2 确认没有删除现有诊断日志。
- [x] 7.3 确认 tasks.md 只在全部完成后统一勾选。

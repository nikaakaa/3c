## 1. 准备和复现
- [x] 1.1 读取本变更 `proposal.md`、`design.md`、`tasks.md` 和 spec deltas。
- [x] 1.2 读取相关现有 specs：`local-rollback-synctest-foundation`、`fullbody-rollback-replay`、`local-latency-reconciliation`。
- [x] 1.3 检查同时进行中的 `add-entry-local-animation-motion-space`、`add-local-rollback-soak-runner`、`refactor-fullbody-frame-pipeline` 是否有重叠任务。
- [x] 1.4 定位当前 `LocalRollbackSynctestRunner`、`LocalRollbackSoakRunner`、`LocalLatencyReconciliationRunner`、`FullBodyRollbackSimulation` 实现。
- [x] 1.5 复现或构造“最终快照一致但 `FirstMismatch.HasMismatch=true`”的最小测试用例。
- [x] 1.6 记录该用例的 expected/actual differences，作为后续严格模式回归测试输入。

## 2. Synctest 严格结果语义
- [x] 2.1 为 synctest runner 增加正式严格验收入口或配置，默认用于预测回滚验收。
- [x] 2.2 明确 strict success 规则：最终比较必须一致，且 `FirstMismatch.HasMismatch` 必须为 false。
- [x] 2.3 保留最终 comparison 字段，方便确认最终是否收敛。
- [x] 2.4 保留 first mismatch 字段，且 restore/replay 阶段都能填充。
- [x] 2.5 为“restore 后立即 mismatch”补测试。
- [x] 2.6 为“replay 中间 mismatch、最终收敛”补测试。
- [x] 2.7 为“最终 mismatch 但无中间快照可比对”补测试。
- [x] 2.8 确认 strict 模式不会改变 `ILocalRollbackSynctestSimulation` 接口。

## 3. First mismatch 诊断输出
- [x] 3.1 扩展或调整 `LocalRollbackSynctestLogFormatter`，输出 stage、tick、restore tick、end tick。
- [x] 3.2 输出 mismatch tick 的输入帧摘要。
- [x] 3.3 输出 expected/actual 快照的紧凑摘要。
- [x] 3.4 输出字段级 differences，覆盖 position、yaw、state、blackboard、action、animation。
- [x] 3.5 确认日志格式低噪声，能被 PowerShell 诊断脚本搜索。
- [x] 3.6 为 pass 日志补测试，确认没有错误输出 first mismatch。
- [x] 3.7 为 fail 日志补测试，确认 first mismatch 信息完整。

## 4. Soak runner 严格窗口
- [x] 4.1 让 soak runner 使用 strict synctest 结果。
- [x] 4.2 当某窗口存在 first mismatch 时，soak result MUST 失败。
- [x] 4.3 `stopOnFailure=true` 时在首个严格失败窗口停止。
- [x] 4.4 `stopOnFailure=false` 时继续跑完整轮次，但保留首个严格失败。
- [x] 4.5 输出 seed、window、restore tick、end tick、first mismatch stage/tick。
- [x] 4.6 为 `LocalRollbackSoakRunnerStopsOnFirstMismatch` 补严格中间分叉用例。
- [x] 4.7 为 `LocalRollbackSoakRunnerKeepsFirstMismatchWhenContinuing` 补严格中间分叉用例。

## 5. FullBody/动画驱动可回滚 fixture
- [x] 5.1 盘点 `CharacterSimulationSnapshot` 当前包含哪些 FullBody、Locomotion、runtime blackboard 和 animation facts。
- [x] 5.2 盘点 TurnBack EntryLocal/profile 采样需要的输入事实：phase、gait、variant、motion space、previous/current normalized window、root pose。
- [x] 5.3 确认上述事实中哪些已经由 tick 输入确定性推导，哪些必须 capture/restore。
- [x] 5.4 修正测试 fake driver：只替代不可控表现时钟，不绕过 FullBody 主线和正式 profile 采样边界。
- [x] 5.5 增加 TurnBack EntryLocal/profile replay 测试，覆盖一段带位移和旋转的转身。
- [x] 5.6 在该测试中断言 `FirstMismatch.HasMismatch=false`。
- [x] 5.7 在该测试中断言最终 position/yaw/action/animation facts 一致。
- [x] 5.8 增加“缺少动画播放进度恢复会失败”的负向测试或等价 guard。
- [x] 5.9 增加左右脚/方向变体选择的确定性测试，至少证明选择事实来自 tick 输入或快照，而不是表现层当前帧。
- [x] 5.10 确认测试不直接调用 `BasicLocomotionPipeline` 或 `CharacterController.Move`。

## 6. Latency/Reconciliation 诊断强化
- [x] 6.1 在 reconciliation 结果中区分 prediction mismatch 和 replay nondeterminism。
- [x] 6.2 输出 confirmed tick、first incorrect tick、restore tick、end tick、replay frame count。
- [x] 6.3 输出预测输入与真实/已确认输入的字段级差异。
- [x] 6.4 在 rollback 后 replay 阶段复用 strict synctest 或等价严格逐 tick 比较。
- [x] 6.5 当同输入 replay 仍出现 first mismatch 时，result MUST 标记为 replay nondeterminism，而不是普通 prediction correction。
- [x] 6.6 为“预测错误但 replay 收敛”补测试。
- [x] 6.7 为“预测错误且 replay 不确定”补测试。
- [x] 6.8 为“快照缺失停止”保留测试。

## 7. Debug runner 和手动验证
- [x] 7.1 确认 F6 synctest debug runner 默认使用严格验收语义。
- [x] 7.2 确认 F8 soak debug runner 默认使用严格验收语义。
- [x] 7.3 确认 F7 latency debug runner 输出 prediction/replay 分类。
- [x] 7.4 确认安全探针模式结束后恢复触发前现场快照。
- [x] 7.5 确认可见 correction 模式仍走正式配置，不新增 fallback。
- [x] 7.6 更新或补充诊断脚本搜索关键字。
- [x] 7.7 给用户记录手动验证命令和 Console 搜索词。

## 8. 自动测试和静态边界
- [x] 8.1 运行 EditMode 测试：`ThirdPersonSimulation.Tests.LocalRollbackSynctestFoundationTests`。
- [x] 8.2 运行 EditMode 测试：`ThirdPersonSimulation.Tests.FullBodyRollbackReplayTests`。
- [x] 8.3 运行 EditMode 测试：`ThirdPersonSimulation.Tests.LocalLatencyReconciliationTests`。
- [x] 8.4 运行静态边界测试，确认 rollback core 不引用 Animancer、Input System adapter、Cinemachine、`CharacterController`。
- [x] 8.5 运行相关诊断 PowerShell 脚本。
- [x] 8.6 如果 Unity 编辑器锁住 dll，仅记录为环境文件锁，不把它误判为源码编译错误。

## 9. 文档和收尾
- [x] 9.1 更新本变更任务勾选状态，确保只勾选真实完成项。
- [x] 9.2 更新必要的 Path/agent 文档，记录严格 first mismatch 验收口径。
- [x] 9.3 运行 `openspec validate harden-local-prediction-rollback-tooling --strict --no-interactive`。
- [x] 9.4 汇总自动测试结果、手动验证步骤和未解决风险。

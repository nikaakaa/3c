## Context
现有本地预测工具链已经具备基础构件：

- `PredictionInputHistory` 保存 tick 输入。
- `PredictionSnapshotHistory` 保存 tick 快照。
- `ILocalRollbackSynctestSimulation` 提供 `Restore -> Advance -> CaptureSnapshot` 边界。
- `LocalRollbackSynctestRunner` 能从历史 tick 恢复并重放到 end tick。
- `FullBodyRollbackSimulation` 已经把 replay 接到 `PlayerFullBodyActionController` 主线。
- `LocalRollbackSoakRunner` 和 `LocalLatencyReconciliationRunner` 已经能做多窗口和高延迟模拟。

现在没做好的地方不是“完全没有工具”，而是工具的验收口径还偏宽：它有 `FirstMismatch`，但最终 `Success` 仍主要看 end tick 快照。这样会放过“中间 tick 分叉，最终又碰巧收敛”的情况。

## Problem Analysis
1. 最终快照一致不等于可预测回滚确定性。
   - 格斗/动作游戏的命中、取消、派生、输入缓冲消费、事件去重和动画分支都发生在中间 tick。
   - tick N 错过一次动作准入，即使 tick N+10 位置/yaw 又对齐，玩法结果也可能已经不同。

2. `FirstMismatch` 已被采集，但还不是验收权威。
   - `LocalRollbackSynctestRunner` 当前可以记录 restore/replay 的首个 mismatch。
   - 但 result success 仍可能只由最终比较决定。
   - `LocalRollbackSoakRunner` 若只看 `result.Success`，也会放过暂态分叉。

3. FullBody/动画驱动状态比纯移动更容易隐藏缺口。
   - TurnBack EntryLocal/profile 采样依赖播放窗口、motion space、phase/gait/variant、previous normalized time 和根 pose。
   - 任一影响下一 tick 的事实没有 capture/restore，重放就会从不同相位采样。
   - 如果 fake animation driver 不模拟真实 production 输入边界，测试可能看起来通过但没有验证真实缺口。

4. Latency/reconciliation 需要区分两类失败。
   - “预测输入和真实输入不同”是正常 correction。
   - “同一段确认输入从旧快照重放仍不一致”是 replay 不确定，必须先修工具/状态快照。
   - 两类失败混在一起时，后续接 Fantasy 会非常难定位。

5. Animator 运行时 delta 不能作为预测回滚验收基准。
   - AnimatorDirect 可以吃 Unity 混合后的 root delta，适合表现兼容或非回滚动作。
   - 但它依赖 Unity 播放时序、blend state 和表现层生命周期，不适合作为 rollback 确定性验收。
   - rollback 工具应验收 tick 驱动的 profile/纯数据路径。

## Goals
- 用严格逐 tick 语义把 first mismatch 提升为预测回滚验收失败。
- 让 soak、synctest、latency/reconciliation 输出同一套可读的首个分叉诊断。
- 让 FullBody/动画驱动状态通过可控 fixture 证明 capture/restore 完整。
- 保持现有主线边界，不新增绕过系统的快捷路径。
- 给用户明确手动验证方式：按键、日志 channel、搜索关键字和 PASS/FAIL 判断。

## Non-Goals
- 不处理真实网络传输、服务器权威、proto 生成。
- 不保证 Unity AnimatorDirect root motion 可回滚。
- 不重做所有动画资产，不把 left/right foot blending 的美术策略塞进 rollback core。
- 不删除已有诊断日志。

## Decisions
### Decision: 严格逐 tick 结果作为预测回滚验收口径
用于预测回滚验收的 runner/debug runner/soak runner MUST 把 `FirstMismatch.HasMismatch` 当作失败。最终快照比较仍保留，但只作为补充信息。

原因：回滚确定性要求从恢复 tick 开始每一帧都能重放一致；最终收敛不能证明中间没有影响玩法。

### Decision: 保留非严格观察能力，但不能作为验收
如果实现需要保留“最终快照对齐但有 first mismatch”的观察模式，可以作为分析模式存在；默认工具验收和自动测试必须使用严格模式。

原因：非严格模式对调试视觉漂移有价值，但不能代表预测回滚安全。

### Decision: FullBody 动画驱动测试必须模拟 production 边界
TurnBack EntryLocal/profile replay 测试应通过 `FullBodyRollbackSimulation`、`PlayerFullBodyActionController`、`PlayerLocomotionController` 和正式 motion/profile 采样边界推进，不直接调用底层 pipeline。

原因：否则测试会绕过真正容易出错的状态恢复、输入缓冲、action/locomotion 仲裁和 runtime blackboard 写入。

### Decision: 影响下一 tick 的动画事实必须纯数据化
播放进度、profile previous/current window、motion space、phase/gait/variant、输入派生的左右脚/转身方向选择、混合权重或等价事实，只要影响下一 tick 采样，就必须能进入快照或由 tick 输入确定性重建。

原因：这些事实不进入 capture/restore，就会导致 replay 使用不同动画采样窗口。

### Decision: Reconciliation 必须先证明 replay 确定，再谈预测矫正
`LocalLatencyReconciliationRunner` 的诊断应能说明当前失败是：

- 预测输入与真实输入不同，系统执行合法 correction；
- 或同一段 resolved input 重放仍不一致，说明 rollback replay 本身不确定。

原因：后者必须先修，不能被包装成网络预测误差。

## Risks / Trade-offs
- 严格模式会暴露更多失败，短期内会让工具“更红”。这是预期结果，因为它把过去被最终快照掩盖的分叉显出来。
- 快照字段增加可能让比较结果更吵。缓解方式是字段级分类、首个 mismatch 日志和紧凑摘要。
- 动画 fixture 过度 fake 会失去价值。实现时必须让 fake 只替代不可控的表现播放时钟，不替代主线状态机和 motion/profile 采样。

## Manual Validation Shape
- F6 或等价 synctest：搜索 `rollback-synctest`、`first-mismatch`、`differences`。
- F8 或等价 soak：搜索 `ROLLBACK_SOAK_RESULT`、`ROLLBACK_SOAK_FIRST_MISMATCH`。
- F7 或等价 latency：搜索 `reconciliation`、`firstIncorrectTick`、`prediction-diff`、`replay-nondeterminism`。
- 安全探针模式下，工具执行结束后角色必须回到触发前现场。

## Open Questions
- 严格模式是否直接成为所有 debug runner 的默认行为，还是仅在 `rollbackAcceptanceMode` 正式配置开启时启用？本提案倾向默认严格，因为这些工具的用途是预测回滚验收。
- 左右脚 blend 的具体策略是否另开动画 profile variant 提案？本提案只要求影响下一 tick 的选择事实可恢复，不规定美术混合算法。

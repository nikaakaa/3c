# Change: 新增本地回滚 soak 长跑验证

## Why
单次 F6 synctest 只能证明一个短窗口可恢复和可重放，不能长期覆盖输入组合、回滚窗口和 hidden replay 对当前现场的污染风险。最终预测回滚 demo 需要一个可复现、低噪声、可长时间运行的本地验证入口。

## What Changes
- 新增本地 rollback soak runner，按固定 seed 生成多 tick 输入流并重复执行 restore/replay/compare。
- 输出低噪声可搜索诊断：`ROLLBACK_SOAK_RESULT` 和 `ROLLBACK_SOAK_FIRST_MISMATCH`。
- 复用现有 `PredictionInputHistory`、`PredictionSnapshotHistory`、`ILocalRollbackSynctestSimulation` 和 `LocalRollbackSynctestRunner`，不得新增第二套角色推进路径。
- 支持配置 tick 数、rollback window、seed、失败即停和是否应用 replay 结果到场景。

## Impact
- Affected specs: `local-rollback-synctest-foundation`
- Affected code: `Assets/Scripts/Simulation/Rollback`, `Assets/Tests/Editor/Simulation`
- 不修改 Fantasy 协议，不接真实网络，不新增完整 rollback runtime。

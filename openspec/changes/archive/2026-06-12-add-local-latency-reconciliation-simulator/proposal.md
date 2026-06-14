# Change: 新增本地高延迟和解同步模拟器

## Why
当前 `LocalRollbackSynctestRunner` 和 `FullBodyRollbackSimulation` 已能在本地证明"同一段输入重放可收敛到同一快照"，但这只是本地无延迟的理想条件。真实网络下远端输入会延迟到达、乱序或丢失，客户端必须先用预测输入推进、等真实输入到达后比对、预测错误时从旧 tick 恢复并重放。

如果现在直接接 Fantasy，网络延迟、tick 对齐、输入包顺序和本地预测/纠错逻辑会混在一起。本变更先做一个**完全本地的延迟与解同步模拟器**，用可配的假延迟和假远端输入源模拟"远端输入还没到→预测→真实输入到了→比对→回滚"全流程，作为 Fantasy 接入的前置质量门。

GGPO 的 `Sync`、`InputQueue` 和 `SyncTestBackend` 提供了参考模型（见 `Ref/ggpo/`），本变更吸收其核心机制：输入队列 per 玩家、缺输入时用上一帧预测、真实输入到达后比对、记录 first incorrect frame、从错误帧回滚重放。但不照搬 GGPO 的 P2P lockstep 架构或二进制 save/load。

## What Changes
- 新增伪造远端输入源，通过一个**可配延迟队列**模拟远端输入延迟到达。
- 新增**输入预测策略**：远端输入缺失时按规则生成预测输入（默认策略：重复上一帧）。
- 将现有 `ILocalRollbackSynctestSimulation` 的 restore/replay/compare 管线扩为 `check simulation → find first incorrect frame → rollback → replay → compare` 循环。
- 在 `LocalRollbackSynctestDebugRunner` 旁边新增 Play Mode 调试入口，可配延迟参数并按键触发延迟同步测试。
- 自动测试覆盖：远端输入按时到达不回滚、延迟到达触发回滚、预测正确不回滚、预测错误触发回滚并收敛。

## Impact
- Affected specs:
  - `local-latency-reconciliation`（新增）
- Related specs:
  - `fullbody-rollback-replay`（本变更的 replay 管线依赖于此）
  - `local-preinput-buffer`
  - `simulation-tick-system`
- Related active changes:
  - `update-fullbody-rollback-replay`：本变更以其 full-body replay adapter 为基础，不重新定义 restore/replay 边界。
  - `add-local-rollback-synctest-foundation`：本变更复用其输入历史、快照历史和本地 synctest runner。
- Affected code after approval:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/**`（新增 `LatencySimulator`、`PredictionStrategy`、`ReconciliationRunner`）
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation/**`（新增测试）
- Non-goals:
  - 不修改 Fantasy proto，不新增真实网络发送接收。
  - 不实现完整的多人房间管理或玩家匹配。
  - 不实现服务器权威快照校正（那是下一阶段的事）。
  - 不扩展 hitbox/hurtbox/伤害回滚。

# Change: 强化本地预测回滚工具

## Why
当前本地 rollback 工具已经能记录输入、快照、restore、replay 和最终快照比对，但它对预测回滚还不够严厉：中间 tick 已经出现 `FirstMismatch` 时，最终 tick 仍可能重新收敛并让工具误判为通过。对后续预测回滚、预测矫正、格斗判定、取消窗口和动画分支来说，中间任一 tick 分叉都必须被当成确定性失败。

TurnBack 的 EntryLocal/profile 驱动测试也暴露了同一类问题：动画位移、旋转、播放窗口、变体和混合权重只要有一项没有进入纯数据 capture/restore，就会在 replay 中出现暂态分叉。这个变更用于把本地预测工具收紧成可验收的开发工具，而不是只看最终站位的粗粒度探针。

## What Changes
- 强化 `LocalRollbackSynctestRunner` 的严格验收语义：用于预测回滚验收时，`FirstMismatch.HasMismatch` MUST 使本次检查失败，即使最终快照重新收敛。
- 强化 `LocalRollbackSoakRunner`：soak 窗口 MUST 使用严格逐 tick 结果，记录并输出第一个失败窗口。
- 统一 first mismatch 诊断：输出 stage、tick、输入帧、expected/actual 快照摘要和字段级 differences。
- 为 FullBody/动画驱动状态补齐可回滚测试契约：TurnBack EntryLocal/profile 采样、播放进度、motion space、变体/混合决策必须能用纯数据恢复并逐 tick 重放。
- 强化本地 latency/reconciliation 工具：区分“预测输入错误导致的合法矫正”和“同输入 replay 不确定导致的工具失败”。
- 保持现有 `ILocalRollbackSynctestSimulation`、FullBody 主线、Locomotion 主线和 motion executor 边界，不新增第二套角色推进路径。

## Non-Goals
- 不接入真实 Fantasy 网络传输，不修改 proto，不新增服务器输入队列。
- 不把 `AnimatorRuntimeDirect` 作为预测回滚验收路径；它可以继续作为非确定性表现/兼容模式单独规划。
- 不新增独立角色控制器、独立状态机或绕过 `PlayerFullBodyActionController` / `PlayerLocomotionController` 的 movement 路径。
- 不在本变更中重做动画资产、脚底 IK 或完整左右脚美术混合系统；本变更只要求影响下一 tick 的变体/混合事实可诊断、可测试、可恢复。

## Impact
- Affected specs:
  - `local-rollback-synctest-foundation`
  - `fullbody-rollback-replay`
  - `local-latency-reconciliation`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSynctestRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSoakRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSynctestLogFormatter.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSynctestDebugRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalRollbackSoakDebugRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalLatencyReconciliationRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/LocalLatencyReconciliationDebugRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/FullBodyRollbackSimulation.cs`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/Simulation/*`
- Validation:
  - EditMode 测试覆盖严格 first mismatch、soak 严格失败、FullBody TurnBack EntryLocal replay、latency/reconciliation 分支和静态边界。
  - 手动验证 Sandbox 中 F6/F7/F8 或等价工具日志，确认 PASS/FAIL、first mismatch 和安全探针恢复语义。

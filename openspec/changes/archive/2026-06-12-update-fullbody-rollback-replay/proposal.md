# Change: 更新 FullBody 回滚重放主线

## Why
当前 `LocalRollbackSynctestDebugRunner` 的 replay 只通过 `LocomotionRollbackSimulation -> PlayerLocomotionController.Tick(...)` 推进，未进入 `PlayerFullBodyActionController`。因此在 Sandbox 中移动、Run、Dodge 后按 F6，`position/yaw` 已经能对齐，但仍会出现 `stateTime`、`blackboard.action.sourceStep`、`blackboard.animation.*` 等差异。

如果现在直接接 Fantasy 并测试高延迟，网络延迟、tick 对齐、输入包顺序和本地 full-body replay 不一致会混在一起，无法判断问题来源。本变更先把本地 full-body/action replay 对齐，作为后续本地高延迟模拟器和 Fantasy 接入的前置条件。

## What Changes
- 新增或调整 full-body rollback replay adapter，使 synctest replay 走 `PlayerFullBodyActionController -> PlayerLocomotionController` 当前动作主线。
- 为 full-body action 状态、输入请求缓冲和相关 runtime facts 定义纯数据 capture/restore 边界。
- 将 `PredictionInputFrame` 的 Dodge/Attack/Jump/Interact 按钮事实回灌到 `InputRequestBuffer`，让 replay 重新经历动作准入和消费，而不是保存动作结果。
- 让 Play Mode debug runner 可以选择 full-body replay adapter，并继续保持默认安全探针语义。
- 更新自动测试和手动验证，证明 Move/Run/Dodge replay 不再停留在 locomotion-only 诊断阶段。

## Impact
- Affected specs:
  - `fullbody-rollback-replay`
- Related specs:
  - `local-preinput-buffer`
  - `action-interrupt-arbiter`
  - `action-interrupt-policy-data`
  - `simulation-tick-system`
  - `presentation-transform-interpolation`
- Related active changes:
  - `add-local-rollback-synctest-foundation` 已提供输入历史、快照历史和本地 synctest runner，本变更在其基础上补齐 full-body replay 主线。
  - `add-character-runtime-blackboard` 与本变更的 runtime facts capture/restore 有重叠，实施时必须复用其纯数据黑板，不新增第二套事实容器。
  - `refactor-unified-character-state-machine` 和 `add-turn-in-place-locomotion` 仍在活跃变更中，实施时必须以统一状态机主线为准，不恢复旧 FullBody/HFSM 缝合路线。
- Affected code after approval:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/Rollback/**`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/Action/FullBody/**`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Input/**`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/**`
- Non-goals:
  - 不修改 Fantasy proto，不新增真实网络发送接收。
  - 不实现本地高延迟模拟器；该能力应在本变更验收后单独规划。
  - 不实现远端输入预测、服务器权威快照校正或完整 rollback runtime。
  - 不实现 hitbox、hurtbox、伤害、受击、IK 或复杂攻击连段回滚。
  - 不新增第二套角色控制器、第二套状态机或绕过当前运动执行端口的移动路径。

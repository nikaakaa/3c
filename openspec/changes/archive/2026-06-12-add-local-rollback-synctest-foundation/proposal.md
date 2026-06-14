# Change: 增加本地回滚 Synctest 地基

## Why
动作格斗后续要接客户端预测和回滚，第一步不是直接接 Fantasy 网络，也不是直接做完整 rollback runtime，而是先证明当前本地动作模拟能被输入历史和状态快照稳定重放。没有这个本地 synctest 地基，后续网络延迟、远端输入预测和权威校正会把问题放大，并且容易诱发绕过当前 `PlayerLocomotionController` 和统一状态机的新路径。

## What Changes
- 新增本地预测输入帧模型和输入历史，用 `SimulationTick` 保存 Move、Look、Run 和离散按钮事实。
- 新增本地角色模拟快照 v0，用纯数据保存真实模拟根 pose、统一状态机事实、Locomotion 事实和最小动作事实。
- 新增快照历史 ring buffer，用于按 tick 保存、查询、裁剪和诊断缺失恢复点。
- 新增状态恢复边界，规划统一状态机、Locomotion controller 和 motion driver 需要恢复的最小事实。
- 新增本地 synctest 重放验证：同一段输入先正常跑，再从旧 tick 恢复并重放，比较最终快照。
- 增加 EditMode 自动测试、静态边界测试和手动验证步骤。

## Impact
- Affected specs:
  - `local-rollback-synctest-foundation`
- Related specs:
  - `simulation-tick-system`
  - `simulation-tick-locomotion`
  - `local-preinput-buffer`
  - `presentation-transform-interpolation`
  - `animation-phase-timeline-facts`
- Related active changes:
  - `refactor-unified-character-state-machine` 是后续实现基线；本变更不得恢复旧 Locomotion/Dodge/FullBody 缝合路线。
- Affected code after approval:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Simulation/**`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Input/**`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Character/**`
  - `3cDemo/Client/3C_Client/Assets/Tests/Editor/**`
- Non-goals:
  - 不接 Fantasy 协议，不新增真实网络发送接收。
  - 不实现远端输入预测、不实现完整 rollback runtime。
  - 不实现 hitbox、hurtbox、伤害、受击、IK 或复杂攻击连段回滚。
  - 不新增第二套角色控制器、第二套状态机或绕过当前运动执行端口的移动路径。
  - 不同步 Unity Object、Animancer state、Animator、AnimationClip 或场景实例引用。

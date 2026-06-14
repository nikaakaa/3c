## Context
当前项目已经具备本地回滚地基的一部分：

- `SimulationTick`、`SimulationTickRunner` 和固定 phase 顺序已经存在。
- `LocomotionTickAdapter` 已经把 `PlayerLocomotionController` 接入 `ExecuteMotion` phase，并避免 frame Update 双驱动。
- `InputRequestBuffer` 已能按 tick/step 记录本地预输入请求，但还不是每 tick 的完整输入历史。
- `CharacterStateMachineRunner` 已有 `Snapshot`，但没有恢复接口，且当前内部字段会影响下一 tick 输出。
- `PlayerLocomotionController` 已集中处理输入读取、统一状态机评估、运动执行、动画表现和诊断。
- `PresentationTransformInterpolator` 已分离真实模拟根和表现根，适合未来校正后由表现层处理可见收敛。
- `docs/agents/action-fighting-prediction-rollback-guide.md` 已总结 GGPO/synctest 思路和本项目推荐路线。

本变更只做本地 synctest 地基。它的目标是先证明“同一段输入可以重放出同一段状态”，不接真实网络。

## Goals / Non-Goals
- Goals:
  - 建立按 `SimulationTick` 对齐的本地输入历史。
  - 建立本地角色模拟快照 v0 和快照历史。
  - 明确统一状态机、Locomotion 和 motion driver 的恢复边界。
  - 提供本地 synctest：正常运行、保存快照、恢复旧 tick、重放输入、比较快照。
  - 用自动测试和静态验证证明 core 层不依赖表现、输入 adapter 或 `CharacterController`。
- Non-Goals:
  - 不实现远端输入预测。
  - 不实现服务器权威快照校正。
  - 不修改 Fantasy proto。
  - 不实现完整动作战斗回滚。
  - 不把 Unity 场景或 Animancer runtime 对象序列化进快照。

## Decisions
- Decision: 第一版只覆盖本地可控角色的 Move/Look/Run/Dodge 基线。
  - Reason: 当前动作格斗 demo 的第一条可验证链路是移动、停止、闪避和状态恢复。攻击、命中和伤害依赖后续 timeline/hitbox 事实，不应提前混入。

- Decision: 输入历史保存每 tick 输入事实，而不是保存动作结果。
  - Reason: 回滚重放必须重新计算“输入是否被消费、是否进入 Dodge、是否写 Run latch”。如果输入历史保存动作结果，会污染重放语义。

- Decision: 快照 v0 只保存恢复和比较所需的最小纯数据。
  - Reason: 快照太大容易把表现层和 Unity 对象带进 core；第一版目标是证明可恢复闭环。
  - Included: tick、真实模拟根 position/yaw、统一状态机快照、run latch、last moving gait、current world direction、locomotion phase/gait、动画事实 key/time 的最小只读事实。
  - Excluded: Transform、GameObject、CharacterController、Animator、AnimationClip、Animancer state、InputAction。

- Decision: 重放必须继续走现有 tick/状态机/运动主线。
  - Reason: 当前系统的权威路径是 `SimulationTickRunner -> PlayerLocomotionController -> BasicLocomotionPipeline -> motion executor`。synctest 不能创建第二条 movement controller。

- Decision: 恢复能力先暴露为明确 adapter/接口边界。
  - Reason: `CharacterStateMachineRunner` 和 `PlayerLocomotionController` 内部恢复字段需要逐项验证。实现时若发现 `CharacterController` 内部状态不可恢复，应停下来记录风险，不绕过系统。

## Proposed Runtime Order
```text
ReadInput
  采集 tick N 的 Move/Look/Run/Dodge 输入事实
  写 PredictionInputHistory

UpdateInputBuffer
  继续维护 InputRequestBuffer

GameplayDecision / BuildMotion / ExecuteMotion
  继续走统一状态机和 PlayerLocomotionController 主线

WriteSnapshotAndEvents
  采集 CharacterSimulationSnapshot v0
  写 PredictionSnapshotHistory

Synctest
  正常运行 A..B tick
  保存输入和快照
  加载 tick M 快照
  用 M+1..B 输入重放
  比较 B tick 快照
```

## Risks / Trade-offs
- Risk: `refactor-unified-character-state-machine` 尚未归档，状态机恢复字段可能继续变化。
  - Mitigation: 本变更实现阶段必须先以统一状态机最终主线为准；若旧 FullBody/HFSM 路线仍在工作树中，只能作为被取代对象识别，不得接入新 synctest。

- Risk: `CharacterController` 内部 grounded/碰撞状态不可完整恢复。
  - Mitigation: 第一版只恢复可控事实并用测试暴露漂移；若漂移无法接受，另开 proposal 处理 motion driver 可恢复状态。

- Risk: Animancer 播放进度影响 `CanExit`，导致重放结果漂移。
  - Mitigation: 快照和重放只依赖动画事实源输出的纯数据 key/time/progress，不保存 Animancer state；必要时用 fake playback progress source 做 deterministic 测试。

- Risk: 快照 checksum 过早追求 bit-perfect。
  - Mitigation: 第一版使用字段级比较和容差；checksum 作为诊断辅助，不作为唯一判断。

## Migration Plan
1. 先实现纯数据输入帧、输入历史、快照和快照历史。
2. 增加状态机和 Locomotion 恢复边界，不改变现有运行路径。
3. 用 fake input source / fake playback progress source 写本地 synctest。
4. 接入现有 tick phase 记录输入和快照。
5. 加入静态边界测试，证明没有新 movement controller 或表现层依赖。
6. 手动验证当前本地动作 demo 行为不变。

## Open Questions
- `CharacterSimulationSnapshot v0` 是否先只比较 position/yaw/state/run latch，还是同步纳入 animation progress，需要实现阶段按当前动画事实稳定性决定。
- motion driver 是否需要在第一版暴露 vertical velocity/grounded 恢复 API，需要通过重放漂移测试确认。

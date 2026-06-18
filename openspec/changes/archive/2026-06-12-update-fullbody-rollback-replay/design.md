## Context
当前本地 synctest 地基已经能保存 `PredictionInputFrame`、`CharacterSimulationSnapshot` 和历史 ring buffer，并能通过 `ILocalRollbackSynctestSimulation` 执行 restore + replay + compare。

问题在于现有 replay adapter 是 `LocomotionRollbackSimulation`，它只调用：

```text
PredictionInputFrame
  -> ToLocomotionInput
  -> PlayerLocomotionController.Tick(...)
```

而 Sandbox 动作 demo 的真实运行入口是：

```text
PlayerFullBodyActionController.Update/Tick
  -> InputRequestBuffer
  -> FullBodyActionInterruptGate
  -> PlayerLocomotionController.TryEvaluateWithStateMachine
  -> Action facts
  -> Animation facts
  -> Runtime blackboard
```

因此现有 F6 可以证明位置/yaw 的一部分重放能力，但不能证明 Dodge、Action facts、Animation facts 和 FullBody state snapshot 能重放一致。

## Goals / Non-Goals
- Goals:
  - 让 rollback replay adapter 能走 `PlayerFullBodyActionController` 当前主线。
  - 用 `PredictionInputFrame` 的按钮事实重建 `InputRequestBuffer`，而不是保存动作结果。
  - 捕获并恢复 replay 所需的 FullBody action 运行时事实。
  - 让 F6 synctest 对 Move/Run/Dodge 的 action/animation/blackboard differences 变得有意义。
  - 保持 debug runner 默认安全探针语义；可见 correction 只用于手动观察。
- Non-Goals:
  - 不接 Fantasy。
  - 不实现本地 latency simulator。
  - 不做远端输入预测。
  - 不扩展攻击、命中、伤害、受击等 combat rollback。
  - 不保存 Animancer runtime object、Animator、AnimationClip 或场景引用。

## Decisions
- Decision: 新增 full-body replay adapter，而不是扩大 `LocomotionRollbackSimulation` 职责。
  - Reason: `LocomotionRollbackSimulation` 是 locomotion-only adapter，继续保留可用于窄测试；full-body replay 需要接入 action/input buffer/animation facts，职责不同。

- Decision: replay 每 tick 必须先按 `PredictionInputFrame` 写入按钮请求，再调用 `PlayerFullBodyActionController.Tick(...)`。
  - Reason: Dodge 是否进入动作必须由 `InputRequestBuffer`、`FullBodyActionInterruptGate` 和统一状态机重新决定，不能把“已进入 Dodge”的结果写进输入历史。

- Decision: full-body restore 需要显式恢复 action controller 状态，而不是只恢复 locomotion controller。
  - Reason: `PlayerFullBodyActionController` 持有 `currentStateSnapshot`、pending path 诊断、已编译策略缓存和 action/animation presenter 事实。至少当前 action snapshot 与影响下一 tick 的消费/输出事实需要恢复边界。

- Decision: 第一版比较仍使用字段级 differences，不引入 checksum 作为唯一判断。
  - Reason: 动画事实和 Unity 表现层仍可能有容差问题，字段级诊断更容易定位 full-body replay 缺口。

- Decision: Fantasy 接入排在本变更之后。
  - Reason: 直接接 Fantasy 会把本地 replay 不一致和网络延迟混在一起；本变更通过后，再做本地 latency simulator，最后换 Fantasy transport。

## Proposed Runtime Order
```text
Synctest 正常运行
  ReadInput
    记录 PredictionInputFrame
  UpdateInputBuffer
    正常输入缓冲
  FullBody/Locomotion 主线
    PlayerFullBodyActionController.Tick
  WriteSnapshotAndEvents
    记录 CharacterSimulationSnapshot

Synctest replay
  Restore tick M snapshot
  Restore full-body replay state
  For tick M+1..B:
    从 PredictionInputFrame 写 InputRequestBuffer
    调 PlayerFullBodyActionController.Tick(input.ToLocomotionInput(fixedDelta))
    写 action/animation/runtime blackboard facts
  Capture B snapshot
  Compare expected B vs actual B
```

## Risks / Trade-offs
- Risk: `PlayerFullBodyActionController` 当前缺少 restore API。
  - Mitigation: 先定义最小 `CommittedActionRestoreState`，只包含影响 replay 的纯数据；若需要恢复 animation presenter 内部状态，记录为后续表现层/动画事实变更，不直接保存 Animancer 对象。

- Risk: `InputRequestBufferComponent.CurrentStep` 与 `SimulationTick` step 不一致。
  - Mitigation: replay 时用 input tick 显式 `SetStep(input.Tick.Value)`，并测试 Dodge pressed 在同 tick 可被 action gate 消费。

- Risk: Action animation facts 依赖真实 Animancer 播放进度，导致 replay 不稳定。
  - Mitigation: 自动测试用 fake action animation presenter / fake locomotion playback progress source；手动测试中保留 differences 输出，先目标是减少 action/sourceStep 漂移。

- Risk: 与活跃 turn-in-place / runtime blackboard 变更冲突。
  - Mitigation: 实施前重新读相关 active change；若字段名变化，复用最新主线，不新增兼容旁路。

## Migration Plan
1. 保留现有 locomotion-only replay adapter 和测试。
2. 新增 full-body replay adapter，并让 debug runner 可选择使用。
3. 为 full-body action controller 增加 capture/restore 纯数据边界。
4. 将 `PredictionInputFrame` 按钮事实回灌到 `InputRequestBuffer`。
5. 补自动测试覆盖 Move/Run/Dodge replay。
6. 手动在 Sandbox 用 F6 验证 differences 收敛。

## Open Questions
- 第一版是否只要求 Dodge replay 收敛，Attack/Jump/Interact 仅保留输入回灌路径和测试占位？
- FullBody action animation presenter 是否需要最小 restore API，还是第一版只把 animation facts 作为诊断差异输出？

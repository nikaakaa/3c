## Context
当前运行时路径已经收口为 FullBody 主线，但 `PlayerLocomotionController` 仍是一个胖 MonoBehaviour。它内部同时做 Unity 引用解析、输入读取、移动意图、相机空间事实、TurnBack intent、TurnBack root motion/profile 采样、基础移动帧构建、黑板写入、snapshot/restore 和大量诊断日志。

这不是新的运行时分裂，但会继续制造维护风险：后续修改 TurnBack、动画采样或 rollback 时，开发者很容易在同一个文件里绕过 FullBody pipeline，或者把诊断、表现和逻辑条件混在一起。

## Goals
- 让 `PlayerLocomotionController` 成为薄 facade：只负责 Unity 装配、生命周期、对 FullBody pipeline 暴露稳定调用点。
- 将可测试的 Locomotion 纯逻辑拆成普通 C# 模块。
- 让 TurnBack intent 和 TurnBack motion 变成明确模块，而不是 controller 内部长方法群。
- 将诊断日志集中到 Locomotion diagnostics 模块，保留 key 和语义。
- 用静态测试防止拆分过程中恢复 Locomotion 直驱、第二 runner、fallback 配置或绕过 motion executor。

## Non-Goals
- 不做玩法调参。
- 不改变状态机配置资产。
- 不重新设计 rollback snapshot 字段。
- 不删除日志。
- 不删除旧公开 API，除非静态搜索证明没有外部依赖并且任务清单明确执行该步骤。

## Proposed Module Shape
```text
Movement/Runtime/PlayerLocomotionController
  Unity 引用解析
  FullBody pipeline facade
  adapter 状态缓存

Movement/Model/Facts
  LocomotionDecisionFrame / LocomotionDecisionFacts / LocomotionSpatialFacts
  LocomotionStateDecisionFrame
  名称中的 Decision 只表示状态机判定前的 facts 聚合

Movement/Solver/Facts/LocomotionFactsBuilder
  input snapshot -> intent
  intent + camera/facing -> spatial facts
  spatial facts -> locomotion facts
  locomotion facts + blackboard snapshot -> state machine context

Movement/Model/Motion
  基础移动输出和 motion facts 中转模型

Movement/Solver/Motion/LocomotionStateMotionBuilder
  CharacterStateMachineFrame -> BasicLocomotionFrame
  state outputs -> MovementCommand / MovementAnimationContext

Movement/Model/TurnBack
  LocomotionTurnBackIntent
  TurnBack 专用纯数据

Movement/Solver/TurnBack/TurnBackIntentResolver
  previous direction + current spatial facts -> LocomotionTurnBackIntent
  intent clear/consume 规则

Movement/Solver/TurnBack/TurnBackMotionResolver
  TurnBack state output + animation/profile facts -> BasicMovementMotionFacts
  input lock / motion window / entry basis 计算

Movement/Model/Snapshot
  Movement 边界内的 snapshot/restore 数据模型

Movement/Solver/Snapshot/LocomotionSnapshotAdapter
  capture / restore controller-owned locomotion facts
  不定义动画播放进度权威，只调用已批准的 playback restore 入口

Movement/Diagnostics/LocomotionDiagnostics
  统一提交 RuntimeDiagnosticLog
  保持现有 eventId / channel key
```

## Decisions
- Decision: 将原先口语中的 decision 模块命名为 facts 模块。
  - Reason: Locomotion 只提供统一状态机判定所需 facts，不拥有状态选择权威，`Facts` 比 `Decision` 更不容易误导。
- Decision: 先抽纯逻辑，再收窄 MonoBehaviour。
  - Reason: 先移动最容易测试的 facts/TurnBack 逻辑，可以降低行为回退风险。
- Decision: `PlayerLocomotionController` 保留 facade 方法名直到所有调用点迁移完。
  - Reason: 直接删除公开 API 容易破坏测试和场景引用；删除必须基于静态搜索。
- Decision: 诊断日志只移动，不删除。
  - Reason: 项目规则要求 log 等用户明确删除再删，且当前 TurnBack/rollback 仍依赖诊断定位。
- Decision: Snapshot 拆分不改变动画播放进度语义。
  - Reason: 动画 playback rollback 正在由独立变更定义，本变更只负责模块边界。

## Risks / Trade-offs
- Risk: 机械拆分过大导致行为回退。
  - Mitigation: 每拆一个模块先加 characterization tests，再迁移一组方法。
- Risk: 提取 TurnBack motion 时碰到动画 playback rollback 活跃变更。
  - Mitigation: 如果相关文件已经被该变更修改，先只提取调用边界或等待该变更完成。
- Risk: 保留旧 facade API 让文件仍不够瘦。
  - Mitigation: tasks 中单独列出旧 API 删除审计，只有无外部依赖时才删除。
- Risk: 诊断模块化后日志 key 改名。
  - Mitigation: 增加测试锁定关键 eventId。

## Migration Plan
1. 加静态和行为锁定测试，确认当前唯一 runner owner、唯一 tick driver、无 fallback、日志 key。
2. 抽 `LocomotionFactsBuilder`，保持 public facade 结果不变。
3. 抽 `TurnBackIntentResolver`，保持 TurnBack 进入条件不变。
4. 抽 `TurnBackMotionResolver`，保持 root motion/profile/input lock 结果不变。
5. 抽 `LocomotionStateMotionBuilder`，保持 `BasicLocomotionFrame` 输出不变。
6. 抽 diagnostics，保持日志 eventId 和等级不变。
7. 审计并删除或隔离退役直驱 API 和 `LocomotionTickAdapter` 诊断壳。
8. 更新文档和任务清单，进行自动和手动验证。

## Open Questions
- 退役的 `TickFromInputSource` / `TryEvaluateLocomotion` 是本变更中直接删除，还是先保留 `[Obsolete]` facade 到下一轮清理？
- `LocomotionTickAdapter` 是否移动到 Editor/Diagnostics 命名空间，还是在 runtime 中保留一个版本用于迁移诊断？

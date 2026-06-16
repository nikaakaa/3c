## Context

当前运行时已经有目标架构的多个零件：

- `SimulationTickPhaseOrder` 已定义 `ReadInput / UpdateInputBuffer / GameplayDecision / BuildMotion / ExecuteMotion / WriteSnapshotAndEvents / PresentationBridge`。
- `PlayerFullBodyActionController.Tick` 当前实际完成读取输入、准备 Locomotion facts、Dodge 仲裁、统一状态机推进、输出执行、动画事实写入和相机完成。
- `PlayerLocomotionController` 已能准备 `LocomotionDecisionFrame`，但同时还负责 camera/look、TurnBack intent、状态机 context、motion facts、motion command、动画提交、runtime blackboard 和 snapshot/restore。
- `FullBodyActionTickAdapter` 当前注册在 `ExecuteMotion`，并在同一 phase 中调用 request buffer adapter 和完整 FullBody tick。
- `PredictionInputHistoryTickRecorder` 和 `LocomotionSnapshotHistoryRecorder` 已分别使用 `ReadInput` 和 `WriteSnapshotAndEvents`，说明 tick 外壳可用，但 gameplay 主体还没有真正拆进 phase。

本变更目标不是重写角色控制器，而是把已有顺序提升为一个可测试的 FullBody frame pipeline，使每个 step 的输入输出都是纯数据或外围 adapter 调用。

## Goals / Non-Goals

Goals:

- 让 FullBody 一帧顺序成为显式 contract。
- 让 `SimulationTickPhase` 的 gameplay 阶段真实承载对应工作，而不是只把完整 Tick 放在 `ExecuteMotion`。
- 将 Action 请求门从 Dodge 特例推进到可扩展请求门形态，但不实现 Attack 语义。
- 降低 `PlayerLocomotionController` 和 `PlayerFullBodyActionController` 的外部 Interface 复杂度。
- 保持本地 replay、synctest 和 live play 使用同一条推进路径。

Non-Goals:

- 不实现 `add-light-attack-combo-action`。
- 不新增 hitbox、hurtbox、伤害、受击、IK、VFX/SFX/camera event。
- 不修改 Fantasy proto，不接真实网络。
- 不新增第二套角色控制器、第二套状态机或绕过 motion executor 的运动路径。
- 不删除现有诊断 log。
- 不运行 Unity batchmode。

## Decisions

### Decision: 引入 FullBodyFramePipeline 作为深 Module

将 FullBody 一帧的状态保存在 pipeline context 中，而不是让多个 MonoBehaviour 互相通过字段和调用顺序传递隐式状态。

Pipeline step 至少包含：

1. ReadInput：读取或接收 `PredictionInputFrame` / `BasicLocomotionInputSnapshot`。
2. UpdateInputBuffer：将离散按钮事实写入 `InputRequestBuffer`，并同步 current step。
3. PrepareFacts：生成 `LocomotionDecisionFrame` 和 action request candidates。
4. GameplayDecision：执行 Action 仲裁并推进统一状态机。
5. BuildMotion：把状态机输出和 motion policy 转换为运动命令。
6. ExecuteMotion：只调用当前 owner 对应的 motion executor。
7. PresentationBridge：提交 Locomotion/Action 动画命令，采集/写入动画事实，并处理相机 resolve。
8. WriteSnapshotAndEvents：写 runtime facts、snapshot 和诊断事件。

Reason: 这样调用方只需要知道 pipeline 的输入和最终结果，不需要知道 `PlayerLocomotionController.TryPrepareDecisionFrame`、`FullBodyActionInterruptGate`、`ApplyStateFrameOutputs`、`WriteAnimationRuntimeFacts` 的内部顺序。

### Decision: phase handler 只调 pipeline step

`FullBodyActionTickAdapter` 不再在 `ExecuteMotion` 调完整 `fullBodyActionController.Tick(...)`。它应注册到所需 phase，或由一个 FullBody frame tick adapter 在每个 phase 调用同一个 pipeline context 的对应 step。

Reason: 当前 phase 顺序已有 spec，但 gameplay 主体没有按 phase 拆开，导致 `UpdateInputBuffer` 和 `GameplayDecision` 语义落空。

### Decision: 保留 MonoBehaviour 作为 Adapter

`PlayerFullBodyActionController`、`PlayerLocomotionController`、presenter、motion executor、input adapter 继续存在，但它们应成为 pipeline 外围 adapter 或小型装配点。

Reason: 不新增第二套控制路径，也不一次性重写已有场景装配。

### Decision: Action 请求门泛化，但不实现 Attack

本变更允许新增通用 `FullBodyActionRequestGate` 或等价接口来处理多个 `InputRequestKind`，但只要求迁移当前 Dodge 行为并保留 Attack 扩展点。

Reason: 如果 FullBody 主入口继续调用 `BuildDodgeRequestFact`，后续 Attack 会自然复制出 `BuildAttackRequestFact`，重新把每个动作塞进主入口。

### Decision: replay 使用同一 pipeline

`FullBodyRollbackSimulation.Advance` 应通过同一个 FullBody frame pipeline 推进 `PredictionInputFrame`，而不是单独执行 input buffer replay、controller Tick 和 action playback restore。

Reason: replay 是检验 pipeline 确定性的核心，不能成为另一条隐式推进路径。

## Risks / Trade-offs

- Risk: 分阶段后短期文件数量增加。
  - Mitigation: 先只创建一个深 pipeline module 和少量 step 数据，不为每个步骤过早拆独立 class。
- Risk: `PlayerLocomotionController` 变薄过程中容易打断 TurnBack。
  - Mitigation: 先只迁移调用顺序和 context，保持 `formalize-turnback-locomotion-state` 已验证的 motion policy 和测试。
- Risk: phase handler 分散后难以调试。
  - Mitigation: 每个 phase 输出同一个 frame id / tick / state path 的诊断摘要，测试覆盖 phase 顺序。
- Risk: 与 rollback layering active change 接触同一 snapshot 文件。
  - Mitigation: 实现前先确认 `refactor-rollback-layering-contract` 是否完成；如未完成，优先只改 pipeline adapter，不改 snapshot 数据结构。

## Migration Plan

1. 增加只包裹当前顺序的 FullBody frame pipeline，不改变行为。
2. 用测试锁定旧 `PlayerFullBodyActionController.Tick` 与新 pipeline 在 Move/Run/Dodge/TurnBack 输入下输出一致。
3. 将 tick adapter 分 phase 调用 pipeline。
4. 将 `PlayerFullBodyActionController.Tick` 改为调用 pipeline 的兼容入口。
5. 将 replay adapter 改为同一 pipeline。
6. 逐步把 Locomotion facts、Action request gate、motion build、presentation write-back 从 controller 私有方法迁入 pipeline 内部 helper。
7. 最后移除 tick adapter 对 locomotion-only adapter 的运行时压制依赖，改为场景装配/校验保证单驱动。

## Open Questions

- Live Play 是否第一版强制走 `UnitySimulationTickDriver`，还是保留 frame `Update` 兼容入口但内部也调用同一 pipeline？
- `WriteSnapshotAndEvents` 是否在本变更内集中 snapshot ownership，还是等 `refactor-rollback-layering-contract` 完成后再收口？

## Context
本地预测回滚 demo 当前依赖三条链路：

- 正常运行链路：tick 输入采集 -> FullBody 主线执行 -> 快照采集。
- 本地验证链路：F6/F8 从历史恢复、重放、比较，并恢复触发前现场。
- 表现链路：presentation interpolation 和 Cinemachine 输出玩家看到的画面。

问题在于代码命名和职责让这三条链路互相泄漏。例如 `CameraYaw` 容易被理解为真实相机状态，`PresentationTransformRollbackState` 容易被理解为 gameplay rollback 状态，`LocalRollbackSynctestDebugRunner` 同时承担 synctest 编排、表现恢复、相机探针和日志格式化。

## Goals
- 把可回滚 simulation 状态和 local-only 表现状态分清。
- 保留 `RollbackCameraBasisState` 作为 WASD camera-relative 输入解算事实。
- 禁止真实 Cinemachine / FreeLook / Main Camera 进入 gameplay rollback snapshot。
- 让 F6/F8 debug tooling 的 hidden replay 默认不永久污染 source、visual、presentation 或相机目标代理。
- 先用 fat simulation snapshot 收敛 demo，再通过测试保护逐步删除兼容字段。

## Non-Goals
- 不接入 Fantasy 真实网络。
- 不修改协议文件。
- 不新增独立角色控制器、独立状态机或绕过 FullBody 主线的 replay 路径。
- 不删除现有日志；只新增更清晰的标签和职责分离，日志删除需用户明确确认。

## Decisions

### Decision: 六层分层 contract
本地回滚相关代码按以下层次归类：

1. Rollback Core：纯数据和算法，例如输入历史、快照历史、synctest runner、snapshot comparer。
2. Simulation Adapter：把 Rollback Core 接到现有角色主线，例如 FullBody/Locomotion replay adapter。
3. Gameplay Runtime：角色真实逻辑，例如 FullBody controller、Locomotion controller、InputRequestBuffer、state machine、motion executor。
4. Simulation State：会影响 replay 结果的纯数据，例如 state machine restore、runtime blackboard、motion executor、animation clock、root motion pending state、camera basis。
5. Presentation Local-Only：只影响本地画面，例如 Cinemachine、FreeLook、camera targets、presentation interpolation、screen shake。
6. Debug Tooling：F6/F8、timing probe、HITL 脚本和日志格式化。

### Decision: Camera basis 是 simulation input fact，不是 camera rollback
`RollbackCameraBasisState` 保存的是“tick N 的 Move 输入应如何映射到世界方向”。它 MAY 包含 yaw 作为派生调试值，但 snapshot 不应暴露独立 `CameraYaw` 作为真实相机状态。真实 Cinemachine / FreeLook state 是 local-only，不参与 gameplay rollback。

### Decision: Presentation restore 只属于 debug tooling
F6/F8 hidden replay 可以临时捕获并恢复 presentation state，用于保护测试现场；该状态不得进入 `CharacterSimulationSnapshot`，也不得作为网络/预测状态传播。命名应体现 debug restore，而不是 gameplay rollback。

### Decision: Snapshot 先胖后瘦
实现阶段先把 simulation 相关状态收齐并通过 F6/F8 验证，再删除可重算字段。删除顺序必须受测试保护，不做一次性大删。

### Decision: Debug runner 拆为深 Module
`LocalRollbackSynctestDebugRunner` 应只编排按键触发和 runner 调用。presentation restore guard、timing probe、log formatter 应变成独立 Module，使 F6/F8 行为和日志可以单独测试。

## Cleanup Candidates
- 删除或内联 `CharacterSimulationSnapshot.CameraYaw`，以 `CameraBasisState.Yaw` 作为唯一 yaw 派生来源。
- 删除 `CharacterSimulationSnapshot.WithCameraYaw()`，调用方改用 `WithCameraBasis(...)`。
- 将 `PresentationTransformRollbackState` 重命名或迁移为 debug restore state。
- 将 `CommittedActionRestoreState` 中诊断日志字段拆到单独 diagnostic restore state。
- 将 timing probe 结构和日志格式化从 `LocalRollbackSynctestDebugRunner` 拆出。
- 保留但标注 `LocomotionRollbackSimulation` 为 locomotion-only narrow adapter，不作为 Sandbox FullBody demo 验收主路径。

## Risks / Trade-offs
- 拆分命名会触碰较多测试，必须按小任务提交。
- `CameraYaw` 删除会影响现有测试，必须先把测试迁移到 `CameraBasisState`。
- presentation debug restore 改名可能影响 F6/F8 soak 工具，需保留兼容窗口或一次性更新所有引用。
- active change `add-local-rollback-soak-runner` 未完全归档前，debug tooling 文件可能有重叠修改。

## Validation
- OpenSpec strict validate。
- EditMode tests 覆盖 snapshot 纯数据、camera basis、debug restore local-only、synctest runner core、F6/F8 日志格式化。
- 静态测试或脚本验证 Rollback Core 不引用 Cinemachine、Animancer runtime、Input System adapter、CharacterController。
- dotnet build Runtime/Editor。
- 手动 Play Mode：F6 hidden synctest、F8 soak、`ROLLBACK_TIMING_PROBE` 和 `ROLLBACK_SOAK_RESULT` 日志验证。

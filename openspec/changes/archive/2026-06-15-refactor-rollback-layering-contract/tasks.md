## 0. 准备和冲突检查
- [x] 0.1 确认 `add-local-rollback-soak-runner` 的最新文件状态，避免覆盖未归档的 F8 工具改动。
- [x] 0.2 记录当前 F6/F8 关键日志搜索词：`[rollback-synctest]`、`ROLLBACK_TIMING_PROBE`、`ROLLBACK_SOAK_RESULT`。
- [x] 0.3 运行 `rg` 静态扫描当前 `CameraYaw`、`WithCameraYaw`、`PresentationTransformRollbackState`、`CommittedActionRestoreState` 引用点。

## 1. Rollback Core 分层
- [x] 1.1 新增或调整测试，证明 `LocalRollbackSynctestRunner` 不引用 presentation、camera、Animancer、Input System adapter 或 `CharacterController`。
- [x] 1.2 将 synctest 比较和 first mismatch 日志数据保持为纯数据输出。
- [x] 1.3 确认 `PredictionInputHistory` 和 `PredictionSnapshotHistory` 仍只保存纯数据。

## 2. Camera Basis 清理
- [x] 2.1 新增测试，证明 `RollbackCameraBasisState` 是 replay 中 WASD 世界方向解算的唯一 camera-relative 输入事实。
- [x] 2.2 将现有 `CameraYaw` 测试迁移到 `CameraBasisState.Yaw`。
- [x] 2.3 删除或废弃 `CharacterSimulationSnapshot.WithCameraYaw()`。
- [x] 2.4 删除或内联 `CharacterSimulationSnapshot.CameraYaw`，保留日志需要时从 `CameraBasisState.Yaw` 读取。
- [x] 2.5 运行静态扫描，确认没有真实 Cinemachine capture/restore rollback 状态。

## 3. Simulation Snapshot 结构整理
- [x] 3.1 将 `CharacterSimulationSnapshot` 字段按 transform、state machine、blackboard、input buffer、camera basis、locomotion runtime、motion executor、animation clock 分组。
- [x] 3.2 保持 snapshot 不保存 Unity Object、Animancer state、Animator、AnimationClip、InputAction 或场景引用。
- [x] 3.3 为 snapshot constructor 或 factory 增加小颗粒测试，覆盖默认值、非法数值和 With 方法保持状态。
- [x] 3.4 保持 FullBody replay adapter 继续通过现有 `PlayerFullBodyActionController.Tick(...)` 主线推进。

## 4. FullBody Action Restore 拆分
- [x] 4.1 新增测试，区分 action gameplay restore state 与 diagnostic log restore state。
- [x] 4.2 将 `CommittedActionRestoreState` 中影响下一 tick 输出的字段保留在 gameplay restore state。
- [x] 4.3 将 `lastLogged...`、debug path 等诊断字段迁移到 diagnostic restore state。
- [x] 4.4 确认 snapshot compare 默认比较 gameplay 事实，不因为诊断日志字段导致 synctest 失败。

## 5. Presentation Debug Restore 本地化
- [x] 5.1 将 `PresentationTransformRollbackState` 命名或归属调整为 debug restore state。
- [x] 5.2 新增测试，证明 presentation debug restore state 不进入 `CharacterSimulationSnapshot`。
- [x] 5.3 新增测试，证明 hidden F6/F8 后 source、visual、presentation state 可恢复到触发前。
- [x] 5.4 保持 visible correction 模式继续可用，并明确它是用户显式开启的 debug 行为。

## 6. Debug Tooling 拆分
- [x] 6.1 从 `LocalRollbackSynctestDebugRunner` 拆出 presentation restore guard。
- [x] 6.2 从 `LocalRollbackSynctestDebugRunner` 拆出 timing probe。
- [x] 6.3 从 `LocalRollbackSynctestDebugRunner` 拆出 synctest log formatter。
- [x] 6.4 对 F8 soak debug runner 复用同一套 presentation restore guard 和日志字段语义。
- [x] 6.5 保留 `ROLLBACK_TIMING_PROBE`、`ROLLBACK_SOAK_RESULT`、`ROLLBACK_SOAK_FIRST_MISMATCH` 等可搜索固定标记。

## 7. 验证
- [x] 7.1 运行 `openspec validate refactor-rollback-layering-contract --strict --no-interactive`。
- [x] 7.2 运行 Runtime `dotnet build`。
- [x] 7.3 运行 Editor `dotnet build`。
- [x] 7.4 运行相关 EditMode tests：`LocalRollbackSynctestFoundationTests`、`FullBodyRollbackReplayTests`、`PresentationTransformInterpolatorTests`。
- [x] 7.5 运行 rollback 工具脚本自检：`Test-RollbackWiring.ps1`、`Test-RollbackHitlScripts.ps1`、`Test-UnityEditorCompileLog.ps1`。
- [x] 7.6 手动 Play Mode 触发 F6，确认 hidden 模式画面不永久变化，并复制最近一次 `[rollback-synctest]` 结果。
- [x] 7.7 手动 Play Mode 触发 F8，确认 `ROLLBACK_SOAK_RESULT` 可搜索且 hidden 模式画面不永久变化。

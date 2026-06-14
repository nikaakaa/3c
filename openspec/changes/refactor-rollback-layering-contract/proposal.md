# Change: 梳理本地回滚分层和清理误导状态

## Why
当前本地回滚 demo 已经具备输入历史、快照历史、F6 synctest 和 F8 soak 工具，但代码语义把 simulation rollback、camera-relative 输入事实、presentation 恢复现场和调试探针混在一起。继续按 mismatch 补字段会扩大混乱，也容易再次把 Cinemachine 误当成可回滚 gameplay 状态。

## What Changes
- 定义本地回滚分层 contract：Rollback Core、Simulation Adapter、Gameplay Runtime、Simulation State、Presentation Local-Only、Debug Tooling。
- 明确 `RollbackCameraBasisState` 是输入解算事实，真实 Cinemachine / FreeLook / Main Camera 不进入 gameplay rollback snapshot。
- 将 debug runner 中 presentation 现场恢复、相机 timing probe、日志格式化从 synctest orchestration 中拆出。
- 将 `CharacterSimulationSnapshot` 从平铺字段整理为语义清晰的 simulation 子状态，并为 `CameraYaw`、`WithCameraYaw()` 等兼容字段制定删除路径。
- 将 `PresentationTransformRollbackState` 的语义收窄为 debug restore state，不作为预测回滚状态。
- 将 `FullBodyActionRestoreState` 中 gameplay restore 与诊断日志 restore 拆分，避免诊断字段污染模拟真相。

## Impact
- Affected specs: `local-rollback-synctest-foundation`, `fullbody-rollback-replay`, `presentation-transform-interpolation`, `cinemachine-third-person-camera`
- Affected code: `Assets/Scripts/Simulation/Rollback`, `Assets/Scripts/Character/Movement`, `Assets/Scripts/Character/Action/FullBody`, `Assets/Scripts/Presentation`, `Assets/Scripts/Camera`, `Assets/Tests/Editor/Simulation`
- 不修改 Fantasy proto，不接真实网络，不新增第二套角色控制器或第二套状态机。
- 与当前 active change `add-local-rollback-soak-runner` 同时接触本地 rollback debug tooling；实现时必须先确认 soak runner 已归档或合并其最新语义。

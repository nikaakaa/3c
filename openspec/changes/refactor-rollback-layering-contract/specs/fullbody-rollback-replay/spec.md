## ADDED Requirements
### Requirement: Simulation Snapshot 分组状态
系统 MUST 将 FullBody rollback snapshot 视为 simulation 状态集合，而不是 presentation 或 camera 状态集合。snapshot MUST 能表达 transform authority、state machine restore、runtime blackboard、input buffer restore、camera-relative basis、locomotion runtime、motion executor、animation clock 和 root motion pending state。snapshot MUST NOT 保存 Unity Object、Animancer runtime object、Animator、AnimationClip、InputAction、Cinemachine state 或场景实例引用。

#### Scenario: 保存影响 replay 的 simulation 状态
- **WHEN** tick N 的 FullBody 快照被创建
- **THEN** 快照 MUST 包含影响 tick N+1 输出的纯数据状态
- **AND** MUST 包含用于 WASD replay 解算的 `RollbackCameraBasisState`
- **AND** MUST 包含会影响位移、朝向、动作结束或动画事实的 motion / animation clock / root motion 相关纯数据

#### Scenario: 不保存 local-only 表现状态
- **WHEN** 检查 FullBody rollback snapshot
- **THEN** 快照 MUST NOT 保存真实 Cinemachine、FreeLook、Main Camera、camera target proxy、presentation interpolation sample 或 screen effect 状态
- **AND** 若 debug 工具需要这些状态恢复现场，MUST 通过 Debug Tooling 层独立捕获

#### Scenario: 子状态命名表达 ownership
- **WHEN** 开发者阅读 snapshot 字段或 factory
- **THEN** 字段命名 SHOULD 表达其属于 transform、state machine、blackboard、input buffer、camera basis、locomotion runtime、motion executor 或 animation clock
- **AND** 不应暴露容易被理解为真实相机 rollback 的独立 camera state 字段

### Requirement: Camera Basis 清理路径
系统 MUST 使用 `RollbackCameraBasisState` 作为 replay 中 camera-relative 输入解算的唯一快照事实，并 MUST 逐步删除或内联独立 `CameraYaw` 兼容字段。真实 camera yaw/pitch、FreeLook 轴和 Main Camera transform MUST 保持 local-only。

#### Scenario: Replay 使用 camera basis
- **GIVEN** replay 从 tick N 快照恢复
- **WHEN** tick N+1 输入包含 Move 或 Look
- **THEN** replay MUST 使用快照中的 `RollbackCameraBasisState` 作为 WASD 世界方向解算起点
- **AND** replay MUST NOT 为此恢复真实 Cinemachine 或 FreeLook 轴

#### Scenario: 删除 CameraYaw 兼容语义
- **WHEN** 测试和日志已经改为读取 `CameraBasisState.Yaw`
- **THEN** 系统 SHOULD 删除 `CharacterSimulationSnapshot.CameraYaw` 或将其收敛为 `CameraBasisState.Yaw` 的只读兼容别名
- **AND** SHOULD 删除 `WithCameraYaw()` 并改用 `WithCameraBasis(...)`

#### Scenario: 静态验证无真实相机 rollback
- **WHEN** 运行静态边界验证
- **THEN** 系统 MUST 证明 FullBody rollback capture/restore 不调用真实 camera capture/restore
- **AND** MUST 证明不存在 `ThirdPersonCameraRollbackState` 或等价真实相机 rollback 状态

### Requirement: FullBody Restore 状态去诊断污染
系统 MUST 区分影响 replay 输出的 FullBody gameplay restore state 与只影响日志去重或调试显示的 diagnostic restore state。默认 snapshot comparison MUST 关注 gameplay facts，诊断字段不得导致本地 synctest 误判为 simulation mismatch。

#### Scenario: Gameplay restore 保留下一 tick 所需事实
- **WHEN** FullBody action 状态被 capture
- **THEN** gameplay restore state MUST 包含 owner、action state、state time、variant、pending transition、action direction 和会影响下一 tick 输出的状态机内部事实
- **AND** 捕获结果 MUST 是纯数据

#### Scenario: Diagnostic restore 单独存放
- **WHEN** 控制器需要恢复 last logged path、debug path 或日志去重状态
- **THEN** 这些字段 SHOULD 存放在 diagnostic restore state 或 Debug Tooling 层
- **AND** 它们 MUST NOT 与 gameplay restore state 混成同一不可区分的数据包

#### Scenario: Snapshot 比较不受诊断字段影响
- **WHEN** replay 后 gameplay facts 与 live 快照一致但诊断日志去重字段不同
- **THEN** synctest comparison MUST NOT 因诊断字段差异失败
- **AND** 如需定位诊断字段差异 SHOULD 使用单独 debug probe

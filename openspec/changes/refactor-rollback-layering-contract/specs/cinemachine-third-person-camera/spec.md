## ADDED Requirements
### Requirement: 相机本体保持 Local-Only
系统 MUST 将 Cinemachine、FreeLook、Main Camera 和相机目标代理视为本地表现层状态。预测回滚和本地 replay MUST NOT 捕获或恢复真实相机本体状态；需要重放 camera-relative 移动时 MUST 使用 simulation snapshot 中的 `RollbackCameraBasisState`。

#### Scenario: 回滚不恢复真实相机
- **WHEN** replay 从旧 tick 恢复角色 simulation 状态
- **THEN** 系统 MUST NOT 恢复 FreeLook X/Y 轴、Main Camera transform、CinemachineBrain 状态或真实 camera target 作为 gameplay rollback 状态
- **AND** 当前玩家看到的本地相机 MUST 保持由 local presentation/camera 主路径控制

#### Scenario: Camera-relative 输入使用 basis
- **WHEN** replay 需要用 Move 和 Look 重新计算世界移动方向
- **THEN** 系统 MUST 读取 `RollbackCameraBasisState` 作为输入解算起点
- **AND** MUST NOT 直接读取 `Camera.main`、FreeLook transform 或当前 live 相机 transform 作为 replay 起点

#### Scenario: Timing probe 只诊断不参与回滚
- **WHEN** Debug Tooling 输出相机 timing probe
- **THEN** probe MAY 记录 camera yaw、pitch、target position 或 Main Camera pose 用于诊断
- **AND** probe 数据 MUST NOT 写入 gameplay rollback snapshot
- **AND** probe 日志 MUST 标注 camera state 为 local-only 或等价语义

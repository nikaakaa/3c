## ADDED Requirements
### Requirement: Rollback Debug Rig 装配边界
系统 MUST 将本地 rollback / synctest / soak 的 Debug Tooling 装配在独立 `RollbackDebugRig` prefab 上。正式角色 prefab 和正式场景角色实例 MUST NOT 常驻挂载本地 rollback debug runner、history recorder、prediction input source 或 replay adapter 作为角色运行时能力。Debug Rig prefab 的场景实例 MAY 通过显式引用连接目标角色 runtime，但 MUST NOT 创建第二角色控制器、第二状态机、第二 motion executor、第二 animation presenter 或隐藏 fallback 配置。

#### Scenario: 正式角色不承载 Debug Tooling
- **WHEN** 自动校验 Corin 正式角色 prefab 或正式场景角色实例
- **THEN** 角色对象 MUST NOT 挂载 `LocalRollbackSynctestDebugRunner`
- **AND** MUST NOT 挂载 `LocalLatencyReconciliationDebugRunner`
- **AND** MUST NOT 挂载 `LocalRollbackSoakDebugRunner`
- **AND** MUST NOT 挂载 `PredictionInputHistoryTickRecorder`
- **AND** MUST NOT 挂载 `LocomotionSnapshotHistoryRecorder`
- **AND** MUST NOT 挂载 `FullBodyRollbackSimulation`

#### Scenario: Debug Rig 显式连接目标角色
- **GIVEN** 场景中存在独立 `RollbackDebugRig` prefab 实例
- **WHEN** Debug Rig 需要执行 F6/F7/F8 或等价本地 rollback 工具
- **THEN** Debug Rig prefab 实例 MUST 通过显式序列化引用注入目标 `CharacterFrameRuntimeController`、tick driver、prediction input source、history recorder 和 replay adapter
- **AND** 缺失必需引用时 MUST 输出诊断失败
- **AND** MUST NOT 通过隐藏默认配置继续运行

#### Scenario: Debug Tooling 不成为 gameplay 状态
- **WHEN** F6/F7/F8 工具捕获 input history、snapshot history、presentation probe 或 timing probe
- **THEN** 这些数据 MUST 只属于 Debug Tooling
- **AND** MUST NOT 写入角色正式配置根
- **AND** MUST NOT 成为后续 gameplay tick、网络同步或正式 rollback authority 的必需组件

#### Scenario: 自动查找不作为正式绑定
- **WHEN** Debug Rig 或 recorder 的显式引用缺失
- **THEN** 系统 MAY 在编辑期提供自动填充辅助
- **BUT** Play Mode 工具执行时 MUST 以显式引用是否完整作为成功条件
- **AND** MUST NOT 在角色层级中扫描第一个匹配 MonoBehaviour 作为正式 fallback 绑定

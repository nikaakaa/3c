## ADDED Requirements

### Requirement: Fake Transport Multi-Client Synctest
系统 MUST 提供 fake transport multi-client synctest，用于在不启动真实 Fantasy 进程的情况下验证 input submit、confirmed input set、prediction、rollback、checksum 和 correction 链路。Fake transport MUST 不模拟角色，不生成服务端 Transform。

#### Scenario: 多客户端 confirmed input
- **GIVEN** 两个或多个 fake client 提交输入
- **WHEN** fake room 确认 tick N
- **THEN** fake transport MUST 广播 confirmed input set
- **AND** 客户端 MUST 能将其用于 prediction/rollback closed loop

#### Scenario: 网络异常注入
- **WHEN** fake transport 配置 latency、reorder、duplicate、missing 或 late input
- **THEN** synctest MUST 产生对应 diagnostic
- **AND** MUST NOT 静默吞掉异常输入

### Requirement: Overnight Soak
系统 MUST 提供可复现的 frame sync soak 验证。Soak MUST 使用固定 seed、tickCount、clientCount 和 network chaos profile，并输出低噪声 summary。

#### Scenario: Soak 成功
- **GIVEN** 所有检查窗口通过
- **WHEN** soak 完成
- **THEN** 系统 MUST 输出 `FRAME_SYNC_SOAK_RESULT`
- **AND** MUST 包含 seed、tickCount、clientCount、checkedWindows、rollbackCount、correctionCount 和 result

#### Scenario: Soak 首个失败
- **GIVEN** 某个窗口出现 first mismatch
- **WHEN** stopOnFailure 为 true
- **THEN** soak MUST 停止
- **AND** 输出 `FRAME_SYNC_FIRST_MISMATCH`
- **AND** 包含 seed、tick、confirmed tick、restore tick、reason 和 differences

### Requirement: Motion Determinism Audit
系统 MUST 审计进入帧同步预测回滚范围的 motion source，并将其分类为 strict gameplay、presentation drift、predictive 或 unsupported/risk。风险项 MUST 被明确报告，不得通过扩大容差隐藏。

#### Scenario: Strict motion 字段
- **WHEN** motion source 声明参与 strict rollback
- **THEN** root position、yaw、motion executor state 或 profile playback window MUST 进入 strict comparison 或 checksum projection

#### Scenario: Presentation drift 字段
- **WHEN** motion 或 animation 字段只影响视觉表现
- **THEN** 该字段 MAY 进入 presentation diagnostic
- **AND** MUST NOT 单独导致 strict checksum mismatch

#### Scenario: Unsupported motion source
- **WHEN** motion source 依赖非确定性物理、runtime Animator delta 或 moving platform 且没有确定性输入
- **THEN** audit MUST 将其标为 risk 或 unsupported
- **AND** MUST NOT 声明 strict rollback 已覆盖

### Requirement: Frame Sync Observability
系统 MUST 提供低噪声 frame sync diagnostics，使开发者能定位 handshake、confirmed tick、prediction、rollback、checksum、correction 和 first mismatch。Diagnostics MUST 不进入 gameplay snapshot。

#### Scenario: 固定日志标记
- **WHEN** frame sync 产生关键事件
- **THEN** 日志 MUST 使用固定 marker
- **AND** 至少支持 `FRAME_SYNC_SOAK_RESULT` 和 `FRAME_SYNC_FIRST_MISMATCH`

#### Scenario: Diagnostics 不污染 Snapshot
- **WHEN** 系统记录 debug snapshot 或 overlay 数据
- **THEN** 这些数据 MUST 只属于 Debug Tooling
- **AND** MUST NOT 写入 rollback gameplay snapshot

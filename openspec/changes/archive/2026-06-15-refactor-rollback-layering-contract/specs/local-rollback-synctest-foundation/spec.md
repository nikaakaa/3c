## ADDED Requirements
### Requirement: 本地回滚分层 Contract
系统 MUST 将本地回滚相关代码按 Rollback Core、Simulation Adapter、Gameplay Runtime、Simulation State、Presentation Local-Only 和 Debug Tooling 分层维护。Rollback Core MUST 只依赖纯数据和算法；Simulation Adapter MUST 负责把 core 接到现有角色主线；Presentation Local-Only MUST 不进入 gameplay rollback snapshot。

#### Scenario: Core 不拥有 Unity 表现对象
- **WHEN** 检查 Rollback Core 模块
- **THEN** 它 MUST NOT 引用 Cinemachine、Animancer runtime、Input System adapter、`CharacterController`、`Transform` 写入逻辑或 presentation interpolator
- **AND** 它 MUST 通过纯数据输入、快照和比较结果表达行为

#### Scenario: Adapter 接入现有主线
- **WHEN** 本地 replay 需要推进角色
- **THEN** Simulation Adapter MUST 调用现有 FullBody 或 Locomotion 主线入口
- **AND** 它 MUST NOT 新增第二套 movement controller、第二套状态机或直接移动真实根的旁路

#### Scenario: Debug Tooling 不成为 gameplay 状态
- **WHEN** F6/F8 工具为了保护现场捕获 presentation、visual 或 camera probe 数据
- **THEN** 这些数据 MUST 只属于 Debug Tooling
- **AND** 它们 MUST NOT 写入 `CharacterSimulationSnapshot`
- **AND** 它们 MUST NOT 作为后续网络同步或 gameplay rollback 状态传播

### Requirement: Debug Runner 职责拆分
系统 MUST 将本地 synctest debug runner 的触发编排、presentation restore、timing probe 和日志格式化拆成可独立测试的 Module。F6/F8 默认 hidden 模式 MUST 在结束时恢复触发前现场，并以固定日志标记输出结果。

#### Scenario: Synctest runner 只编排测试
- **WHEN** 用户触发 F6 synctest
- **THEN** debug runner MUST 负责选择 restore/end tick、调用 synctest core 并恢复现场
- **AND** presentation restore、timing probe 和日志格式化 SHOULD 由独立 Module 承担

#### Scenario: Hidden replay 恢复现场
- **GIVEN** debug runner 未启用 apply replay result
- **WHEN** hidden replay 完成或失败
- **THEN** 系统 MUST 恢复触发前最新 live simulation snapshot
- **AND** MUST 恢复 Debug Tooling 捕获的 presentation 现场
- **AND** MUST NOT 将 replay 过程中间态永久留在 source、visual 或 camera target 上

#### Scenario: 固定日志标记
- **WHEN** F6/F8 输出诊断
- **THEN** 日志 MUST 保留可搜索标记 `[rollback-synctest]`、`ROLLBACK_TIMING_PROBE`、`ROLLBACK_SOAK_RESULT` 或 `ROLLBACK_SOAK_FIRST_MISMATCH`
- **AND** timing 或长跑相关日志 MUST 带固定标记，便于过滤刷屏日志

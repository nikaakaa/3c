## ADDED Requirements
### Requirement: 严格逐 Tick 一致性
系统 MUST 提供用于预测回滚验收的严格 synctest 语义。严格语义下，最终快照比较必须一致，且首个 restore/replay mismatch MUST 不存在；如果 `FirstMismatch.HasMismatch` 为 true，则本次 synctest MUST 失败，即使 end tick 快照最终重新收敛。

#### Scenario: 中间分叉但最终收敛
- **GIVEN** replay 从 tick A 恢复并重放到 tick B
- **AND** tick K 的重放快照与历史快照不一致
- **AND** tick B 的最终快照又与历史快照一致
- **WHEN** 严格 synctest 计算结果
- **THEN** 结果 MUST 失败
- **AND** first mismatch MUST 指向 tick K
- **AND** final comparison MUST 仍保留为匹配，供诊断说明“最终收敛但中间分叉”

#### Scenario: Restore 阶段分叉
- **GIVEN** synctest 从 tick A 快照恢复
- **WHEN** 恢复后立即 capture 的快照与 tick A 历史快照不一致
- **THEN** 结果 MUST 失败
- **AND** first mismatch stage MUST 为 `Restore`
- **AND** replay 阶段 MAY 继续执行以收集最终 comparison，但不得覆盖首个 mismatch

#### Scenario: 无中间分叉且最终一致
- **GIVEN** replay 每个可比较 tick 都与历史快照一致
- **AND** end tick 最终快照一致
- **WHEN** 严格 synctest 计算结果
- **THEN** 结果 MUST 通过
- **AND** first mismatch MUST 为空

### Requirement: First mismatch 字段级诊断
系统 MUST 为 synctest 的首个分叉输出结构化诊断，至少包含 stage、tick、restore tick、end tick、输入帧摘要、expected 快照摘要、actual 快照摘要和字段级 differences。诊断 MUST 能区分 restore mismatch、replay mismatch、缺失输入和缺失快照。

#### Scenario: Replay 分叉输出输入帧
- **GIVEN** first mismatch stage 为 `Replay`
- **WHEN** debug runner 输出失败日志
- **THEN** 日志 MUST 包含 mismatch tick
- **AND** MUST 包含该 tick 的 `PredictionInputFrame` 摘要
- **AND** MUST 包含 differences 字段列表

#### Scenario: Restore 分叉不伪造输入帧
- **GIVEN** first mismatch stage 为 `Restore`
- **WHEN** debug runner 输出失败日志
- **THEN** 日志 MUST 标记该 mismatch 没有关联输入帧
- **AND** MUST 包含 restore tick 的 expected/actual 摘要

#### Scenario: 缺失数据诊断
- **GIVEN** synctest 缺少恢复快照或输入帧
- **WHEN** runner 返回失败
- **THEN** failure reason MUST 包含缺失的 tick
- **AND** MUST NOT 把缺失数据伪装成 snapshot mismatch

### Requirement: Soak 严格窗口验收
系统 MUST 让本地 rollback soak 使用严格 synctest 语义。任一窗口出现 first mismatch 时，soak 结果 MUST 失败，并 MUST 保留首个失败窗口的 seed、restore tick、end tick、stage、mismatch tick 和 differences。

#### Scenario: 首个窗口分叉时停止
- **GIVEN** soak 配置 `stopOnFailure=true`
- **AND** 第一个失败窗口存在 first mismatch
- **WHEN** soak runner 执行
- **THEN** runner MUST 停止后续窗口
- **AND** result success MUST 为 false
- **AND** first failure MUST 指向该窗口

#### Scenario: 继续模式保留首个分叉
- **GIVEN** soak 配置 `stopOnFailure=false`
- **AND** 多个窗口存在 mismatch
- **WHEN** soak runner 执行完全部窗口
- **THEN** result success MUST 为 false
- **AND** first failure MUST 保留最早发现的严格失败窗口

#### Scenario: 所有窗口逐 Tick 一致
- **GIVEN** 所有 soak 窗口没有 first mismatch
- **AND** 所有 end tick 最终快照一致
- **WHEN** soak runner 执行完成
- **THEN** result success MUST 为 true

### Requirement: 严格工具不新增推进路径
系统 MUST 通过现有 `ILocalRollbackSynctestSimulation`、FullBody 主线、Locomotion 主线和 motion executor 边界执行严格验证。严格模式不得直接调用 `BasicLocomotionPipeline`、`CharacterController.Move`、Animancer runtime 或 Input System adapter。

#### Scenario: 严格模式复用现有接口
- **WHEN** synctest、soak 或 debug runner 执行 restore、advance、capture
- **THEN** 它们 MUST 通过 `ILocalRollbackSynctestSimulation` 或既有 adapter 边界执行
- **AND** MUST NOT 新增第二套角色推进路径

#### Scenario: 静态边界验证
- **WHEN** 运行 rollback core 静态边界测试
- **THEN** 测试 MUST 证明 core 不引用表现层和 Unity 运行时控制对象
- **AND** 失败信息 MUST 指出违规文件和违规类型

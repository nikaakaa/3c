## ADDED Requirements

### Requirement: Corin技能网络必须复用同一Program语义和正式载荷

Corin Rollback与Server Authority MUST通过现有正式SessionSource、Pipeline Pass与Adapter使用同一Skill执行合同。网络 MUST只同步Input、Canonical Request、权威状态Hash或Snapshot等正式数据，MUST不传输作者Graph、Blackboard名称、Timeline对象或最终Pose，也不得为技能建立第二执行链。

#### Scenario: 核对技能网络载荷

- **WHEN** Corin在正式Rollback或Server Authority Composition中释放并取消技能
- **THEN** 运行结果 MUST能关联精确Program、Session、请求与状态恢复身份
- **AND** 实际载荷 MUST符合正式输入/状态合同，静态Composition兼容不能代替载荷证据

### Requirement: Corin Replay必须锁定正式Session Composition

Corin Replay Request MUST 精确指定ProgramRuntime、ExecutionBackend、SimulationPipeline、SessionSource、WorldSolver、Numeric Target和对应网络模型。Session准备完成后不得替换这些身份或从目录扫描选择近似配置。

#### Scenario: Replay使用错误Composition

- **WHEN** Replay Request的Composition与Program、Snapshot Codec、Numeric Target或WorldSolver不匹配
- **THEN** Session preparation MUST 拒绝启动并保留兼容性诊断

## ADDED Requirements

### Requirement: Corin Replay必须锁定正式Session Composition

Corin Replay Request MUST 精确指定ProgramRuntime、ExecutionBackend、SimulationPipeline、SessionSource、WorldSolver、Numeric Target和对应网络模型。Session准备完成后不得替换这些身份或从目录扫描选择近似配置。

#### Scenario: Replay使用错误Composition

- **WHEN** Replay Request的Composition与Program、Snapshot Codec、Numeric Target或WorldSolver不匹配
- **THEN** Session preparation MUST 拒绝启动并保留兼容性诊断

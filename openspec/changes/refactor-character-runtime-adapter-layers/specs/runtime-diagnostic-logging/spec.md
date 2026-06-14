## ADDED Requirements
### Requirement: Character 领域 Diagnostics Module
系统 MUST 允许 Locomotion、Animation、MotionExecutor 和 FullBody 将日志格式化与提交迁移到领域 Diagnostics Module。Diagnostics Module MUST 通过统一 `RuntimeDiagnosticLog` 入口提交日志，MUST 保留现有关键 event id、category、level 和 channel key，MUST NOT 成为状态、运动或动画权威。

#### Scenario: 日志迁移保留 key
- **WHEN** Runtime Adapter 中的现有诊断日志迁移到 Diagnostics Module
- **THEN** 关键 event id、category、level 和 channel key MUST 保持稳定
- **AND** 自动测试 MUST 覆盖迁移后的关键日志 key

#### Scenario: Diagnostics 不计算权威结果
- **WHEN** Diagnostics Module 输出 FullBody、Locomotion、Animation 或 MotionExecutor 日志
- **THEN** 它 MUST 只读取已产生的 snapshot、facts、request、result 或只读 progress
- **AND** 它 MUST NOT 重新计算 transition 条件
- **AND** 它 MUST NOT 执行运动命令或播放动画

#### Scenario: 不删除现有日志
- **WHEN** Runtime Adapter 被拆分
- **THEN** 现有日志 MUST NOT 因拆分被删除
- **AND** 如果某条日志需要删除、合并或改名，必须另行获得用户明确批准

#### Scenario: 新增诊断不散落 Debug.Log
- **WHEN** 为拆分后的 Character Module 新增常规诊断
- **THEN** 实现 MUST 通过 `RuntimeDiagnosticLog` 或等价统一入口提交
- **AND** MUST NOT 在新增状态机、运动或动画诊断路径中散落直接 `Debug.Log`

## ADDED Requirements

### Requirement: Agent Snapshot必须只读投影Body Motion Profile

Agent compact/full Snapshot MUST从显式`CharacterPipelineDefinition`引用只读输出Body Motion Profile stable identity、content revision、GravityAcceleration、MaximumFallSpeed、semantic version、required AirborneVerticalMotion capability与正式Compiler配置状态。Snapshot MUST不输出runtime VerticalVelocity、pending integration plan或Solver mutable state。Agent v13 Patch MUST不增加Profile字段修改、任意SerializedProperty或第二Profile写入口；Validator MUST复用Definition/Profile正式校验并与Simulation Compiler报告一致。

#### Scenario: 导出Corin Character Snapshot

- **WHEN** Agent从Corin CharacterPipelineDefinition导出Snapshot
- **THEN** Snapshot MUST能说明当前Body Motion Profile与两个正式参数
- **AND** MUST显示Program是否要求AirborneVerticalMotion
- **AND** Patch catalog MUST不提供修改Profile的操作

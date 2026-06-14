## ADDED Requirements
### Requirement: 表现 Debug Restore 本地化
系统 MAY 为 F6/F8 Debug Tooling 捕获表现层恢复状态，但该状态 MUST 仅用于恢复本地画面现场。表现 debug restore state MUST NOT 被视为 gameplay rollback snapshot，也 MUST NOT 被网络同步、prediction snapshot 或 rollback core 持有。

#### Scenario: Debug restore 不进入 simulation snapshot
- **WHEN** 检查 `CharacterSimulationSnapshot` 或等价 simulation snapshot
- **THEN** 它 MUST NOT 包含 presentation interpolation sample、visual pose correction state 或表现层 restore state
- **AND** presentation restore 数据 MUST 只通过 Debug Tooling 层临时持有

#### Scenario: Hidden replay 后恢复表现现场
- **GIVEN** F6/F8 默认 hidden 模式触发前已有 visual pose 和 interpolation state
- **WHEN** hidden replay 结束
- **THEN** Debug Tooling MUST 恢复触发前表现状态或安全 reset 到触发前 visual pose
- **AND** 表现层 MUST NOT 将 replay 中间态保留为下一渲染帧的长期状态

#### Scenario: 命名避免误导 gameplay rollback
- **WHEN** 表现恢复状态类型或方法被命名
- **THEN** 命名 SHOULD 表达 debug restore 或 local presentation restore 语义
- **AND** SHOULD 避免让调用方误以为该状态属于预测 gameplay rollback snapshot

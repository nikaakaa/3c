## ADDED Requirements
### Requirement: Action Motion Resolver Result 参与回放一致性
FullBody rollback replay MUST 将 Action motion resolver result 视为 strict gameplay facts 的一部分。预测路径和正式路径 MUST 使用同一 action motion spec 与 resolver 输入，产出一致的 movement command、completed 和 run latch 派生。

#### Scenario: Dodge replay 结果一致
- **GIVEN** 相同输入序列触发 Dodge Directional
- **WHEN** rollback replay 从历史 tick 恢复并重放
- **THEN** replay 的 action motion resolver result MUST 与正式路径一致
- **AND** movement command planar distance、world direction、completed 和 source step MUST 匹配

#### Scenario: Backstep 不写 Run latch 保持一致
- **GIVEN** 相同输入序列触发 Dodge Backstep
- **WHEN** rollback replay 比较正式路径和重放路径
- **THEN** 两条路径 MUST 都不产生 run latch on complete
- **AND** 不得通过忽略 action facts 让测试通过

#### Scenario: Resolver 输入缺失时诊断失败
- **GIVEN** replay 恢复后缺少必要 action motion spec 或 locked direction
- **WHEN** resolver 无法产生 strict gameplay result
- **THEN** replay MUST 报告可读差异
- **AND** MUST NOT 使用默认 direction、默认 distance 或上一帧 result 作为 fallback

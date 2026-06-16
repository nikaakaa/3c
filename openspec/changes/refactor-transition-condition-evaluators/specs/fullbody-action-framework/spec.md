## ADDED Requirements
### Requirement: Character Frame Context 提供 Transition Facts Adapter
Character frame context MUST 为统一状态机 condition evaluator 提供本帧所需的纯数据 facts adapter。该 adapter MAY 聚合 Locomotion、Action、Animation 和 Timeline facts，但 MUST NOT 自行选择 transition。

#### Scenario: Character context 只提供 facts
- **WHEN** Character frame pipeline 准备状态机 context
- **THEN** Character frame context MUST 提供 Locomotion facts、Action facts、Animation facts 和 Timeline facts
- **AND** Character frame pipeline MUST NOT 在 evaluator adapter 之外直接判断某条 transition 是否通过

#### Scenario: 新 Action 不改 Character 核心循环
- **WHEN** 后续新增轻攻击或受击请求
- **THEN** Character frame phase order MUST 继续按同一顺序推进
- **AND** 新条件 MUST 通过新增 facts 或 evaluator adapter 接入

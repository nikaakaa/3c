## ADDED Requirements

### Requirement: RuntimeDebugSession必须统一承载Motion Matching诊断

Motion Matching MUST通过现有RuntimeDebugSession、interest、Capture与只读view发布结构化trace，不建立独立运行时调试管理器。Trace MUST覆盖Profile/Database identity、trajectory source/envelope、history状态、admission stage、reject reason、index visit/prune、Top-K exact cost、short-horizon plan、selected sample、continue/jump、Selection Generation、Blend Entry与reset。

#### Scenario: 未订阅候选详情

- **WHEN** RuntimeDebugSession没有Motion Matching candidate interest
- **THEN** Runtime MUST不构造Top-K详情或reject sample列表
- **AND** 正式search与selection结果 MUST保持相同

#### Scenario: 订阅一次查询

- **WHEN** Live Debug关注当前Actor的MM query
- **THEN** view MUST关联同一QueryId下的trajectory、admission、cost、plan和Blend Entry
- **AND** MUST不根据动画状态名重新推断选择原因

### Requirement: Search Replay Capture必须严格绑定数据库身份

显式Capture MAY保存Query payload、current plan、Search Policy、Database/Projection identity与expected result digest。Editor Replay MUST调用正式search implementation并要求exact identity；Replay结果 MUST不参与运行时selection、Program或authoring mutation。

#### Scenario: 重放相同查询

- **WHEN** Replay加载匹配Artifact和Projection
- **THEN** 它 MUST复现candidate顺序、reject、cost、plan与selection digest
- **AND** 差异 MUST作为结构化diagnostic显示

#### Scenario: 调试被禁用

- **WHEN** Release Player未启用MM trace与capture
- **THEN** Runtime MAY不保留Search Replay数据
- **AND** Database、query与selection MUST继续正常运行

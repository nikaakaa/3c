## ADDED Requirements

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

Timeline Editor MUST 显示 TreeClip 的 Decision/Commit 阶段、inline/shared ownership 和 Blackboard 输出摘要。Preview 只有在正式 preview target 提供所需 Pipeline Context 时才 MAY 执行 TreeClip；缺少上下文时 MUST 显示不可执行状态。Preview MUST NOT 创建临时 CharacterGraphContext、写入 authoring 默认值或形成第二套 TreeClip Tick 权威。

#### Scenario: Preview target 提供 Pipeline Context

- **WHEN** Timeline Preview target 提供正式 Pipeline Blackboard 和 Graph runtime context
- **THEN** Preview MAY 按正式 Prepare/Commit 顺序执行 TreeClip
- **AND** Preview MUST 使用与 runtime 相同的阶段和节点能力校验

#### Scenario: Preview target 缺少 Pipeline Context

- **WHEN** 作者打开含 TreeClip 的 Timeline 但没有绑定正式 preview target
- **THEN** Timeline Editor MUST 继续显示 Clip、阶段、Graph 和声明摘要
- **AND** Preview MUST 不执行 TreeClip
- **AND** 系统 MUST NOT 创建 fallback context

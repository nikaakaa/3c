## ADDED Requirements
### Requirement: Behavior Source 到 Committed Branch 导航
Character Behavior Editor MUST allow a designer to navigate from a `CommittedActionLeaf` in Behavior Source mode to Committed Branch mode in the same `CharacterBehaviorEditorWindow`. The navigation MUST use a deliberate node open gesture such as double click or approved equivalent, MUST keep single click as selection only, and MUST use stable node id rather than array index. The navigation MUST NOT create a second Branch window, duplicate menu entry, embedded Branch graph, embedded Timeline panel, or runtime data path.

#### Scenario: 双击 CommittedActionLeaf 进入 Branch mode
- **GIVEN** Character Behavior Editor is in Behavior Source mode
- **AND** the graph contains a `CommittedActionLeaf`
- **WHEN** the designer double-clicks or performs the approved open gesture on that node
- **THEN** the same editor window MUST switch to Committed Branch mode
- **AND** it MUST populate the branch graph from the selected or default formal `CharacterActionDefinitionSO`
- **AND** it MUST select Branch Root or an approved equivalent branch entry node

#### Scenario: 单击只选择节点
- **GIVEN** Character Behavior Editor is in Behavior Source mode
- **WHEN** the designer single-clicks a `CommittedActionLeaf`
- **THEN** the editor MUST select that source node
- **AND** it MUST NOT switch modes
- **AND** it MUST NOT open Timeline Editor

#### Scenario: 导航不新增窗口
- **WHEN** the designer opens a committed branch from Behavior Source mode
- **THEN** the system MUST reuse `CharacterBehaviorEditorWindow`
- **AND** it MUST NOT open `CommittedActionBranchEditorWindow`
- **AND** it MUST NOT add `Tools/3C/Committed Action Branch Editor`

#### Scenario: 缺少 ActionDefinition 只报诊断
- **GIVEN** a `CommittedActionLeaf` is opened from Behavior Source mode
- **AND** no current or default formal `CharacterActionDefinitionSO` can be resolved
- **WHEN** the editor handles the navigation
- **THEN** it MUST show a clear diagnostic
- **AND** it MUST NOT create a fallback branch, sample action definition, Resources lookup, or hidden runtime default

### Requirement: Behavior 与 Branch 图的编辑器关系可解释
Character Behavior Editor MUST present Behavior Source mode and Committed Branch mode as two editor views over different formal data sources. Behavior Source mode MUST represent source topology, while Committed Branch mode MUST represent a single action definition branch. The editor MAY provide navigation between the two views, but MUST NOT merge their authoring data or compiler responsibilities.

#### Scenario: 两张图使用不同 adapter
- **WHEN** Behavior Source mode populates the graph
- **THEN** it MUST use the behavior authoring graph adapter or approved equivalent
- **AND** it MUST read and write `CharacterBehaviorAuthoringAsset` source topology
- **WHEN** Committed Branch mode populates the graph
- **THEN** it MUST use the committed action branch adapter or approved equivalent
- **AND** it MUST read and write `CharacterActionDefinitionSO` branch authoring

#### Scenario: 导航不改变数据所有权
- **GIVEN** a designer navigates from `CommittedActionLeaf` to Committed Branch mode
- **WHEN** the designer edits selector, condition or TimelineNode data
- **THEN** those edits MUST be saved only in the selected `CharacterActionDefinitionSO`
- **AND** the behavior source authoring asset MUST NOT store a copy of selector, condition, TimelineNode, track, clip or payload data

## ADDED Requirements

### Requirement: Action Catalog 导航不改变 Behavior Source Schema
Behavior Source authoring MUST remain a source topology asset even when `CommittedActionLeaf` provides Action Catalog navigation. The asset MUST NOT serialize action definition lists, selected action ids, branch node trees, selector payload, timeline tracks or timeline clips as Behavior Source data.

#### Scenario: 保存 Source Graph 不保存 Action 选择
- **GIVEN** the designer opens `CommittedActionLeaf`
- **AND** chooses an action from the Action Catalog navigation UI
- **WHEN** the designer saves the Behavior Source graph
- **THEN** the saved Behavior Source asset MUST remain limited to source nodes, edges, order, schema/version data and editor positions
- **AND** it MUST NOT save the chosen action id or `CharacterActionDefinitionSO` reference as branch data

#### Scenario: Action Branch 仍由 ActionDefinition 保存
- **GIVEN** the designer chooses an action from `CommittedActionLeaf`
- **AND** edits a Branch node in Committed Branch mode
- **WHEN** the designer saves
- **THEN** the branch modification MUST write to the selected `CharacterActionDefinitionSO`
- **AND** the Behavior Source asset MUST NOT receive Selector, Condition, TimelineNode, track, clip, motion, animation, window or cue payload

#### Scenario: 新增 Action 通过 Catalog 暴露
- **GIVEN** a new `CharacterActionDefinitionSO` is created
- **AND** it is registered in the formal `CharacterActionCatalogSO`
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the new action MUST be discoverable through the catalog navigation flow
- **AND** no new Behavior Source node type or source graph schema field is required

### Requirement: Catalog 导航不得创建第二 Action 数据源
The editor MUST treat Action Catalog navigation as a locator for formal action definitions. It MUST NOT create duplicate action branch data in Behavior Source authoring, editor preferences, sample assets or generated hidden assets.

#### Scenario: 缺失 ActionDefinition 不生成隐藏资产
- **GIVEN** an Action Catalog entry has a missing action definition reference
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST report the invalid entry
- **AND** it MUST NOT create a generated ActionDefinition, sample branch asset or default Dodge branch behind the designer

#### Scenario: 重复 ActionId 不选择任意一个
- **GIVEN** the Action Catalog contains duplicate action ids
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST report the duplicate id diagnostic
- **AND** it MUST NOT choose the first entry as an implicit fallback

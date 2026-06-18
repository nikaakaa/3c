## ADDED Requirements
### Requirement: Source 到 Branch 导航不改变 Source 边界
Behavior Source graph MAY provide an editor-only navigation entry from `CommittedActionLeaf` to the formal committed action branch editor, but this navigation MUST NOT change the source authoring schema or runtime source boundary. `CommittedActionLeaf` MUST remain a source topology node and MUST NOT become a Branch Root, selector, condition, TimelineNode, action catalog entry, or runtime action branch.

#### Scenario: CommittedActionLeaf 只定位 ActionDefinition
- **GIVEN** behavior authoring graph contains a `CommittedActionLeaf`
- **WHEN** the designer opens that node in the editor
- **THEN** the editor MAY locate a formal `CharacterActionDefinitionSO`
- **AND** it MAY switch to Committed Branch mode for that action definition
- **AND** the behavior authoring graph MUST still only contain source topology data

#### Scenario: 保存 Source 图不写 Action Branch
- **GIVEN** the designer navigated from Behavior Source mode to Committed Branch mode and back
- **WHEN** the designer saves Behavior Source mode
- **THEN** the save MUST only write root, composite, Locomotion leaf, CommittedAction leaf, edge and editor position
- **AND** it MUST NOT write selector, condition, TimelineNode, track, clip, motion payload, animation key, window fact or cue data

#### Scenario: Branch 图不写 Source 拓扑
- **GIVEN** the designer navigated from a `CommittedActionLeaf` to Committed Branch mode
- **WHEN** the designer saves Committed Branch mode
- **THEN** the save MUST write only the selected `CharacterActionDefinitionSO` branch authoring
- **AND** it MUST NOT modify behavior source root, parallel, Locomotion leaf, CommittedAction leaf, edge or source editor position

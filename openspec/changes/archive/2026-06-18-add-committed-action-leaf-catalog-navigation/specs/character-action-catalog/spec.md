## ADDED Requirements

### Requirement: Action Catalog 支持编辑器 Action 导航
`CharacterActionCatalogSO` or an approved equivalent formal catalog MUST provide enough editor-readable data for Character Behavior Editor to list available committed actions. Each listed entry MUST expose a stable action id and a `CharacterActionDefinitionSO` reference or equivalent formal action definition reference. Editor navigation MUST reuse this catalog data and MUST NOT introduce a separate editor-only action registry.

#### Scenario: Catalog Entry 可被编辑器列出
- **GIVEN** Corin `CharacterConfigSO` references a formal Action Catalog
- **AND** the catalog contains `Action.Dodge`
- **WHEN** Character Behavior Editor builds the `CommittedActionLeaf` navigation list
- **THEN** the list MUST include `Action.Dodge`
- **AND** the entry MUST be able to locate the corresponding `CharacterActionDefinitionSO`
- **AND** the editor MUST NOT read a separate Dodge-specific editor field

#### Scenario: 新 Action 注册后可见
- **GIVEN** a new action definition is added to the formal Action Catalog
- **WHEN** Character Behavior Editor rebuilds the `CommittedActionLeaf` navigation list
- **THEN** the new action MUST appear as a selectable entry
- **AND** selecting it MUST open that action definition in Committed Branch mode
- **AND** no additional Behavior Source schema migration is required

#### Scenario: Invalid Catalog Entry 阻止导航
- **GIVEN** the catalog contains an entry with a missing definition reference
- **OR** the catalog contains duplicate action ids
- **WHEN** Character Behavior Editor builds the navigation list
- **THEN** the invalid entry or duplicate id MUST be reported
- **AND** the editor MUST NOT silently remove the problem and continue with a fallback action

### Requirement: Catalog 导航保持运行时边界
Action Catalog editor navigation MUST not change runtime catalog compilation or action request resolution. Runtime gameplay MUST continue to consume compiled action definitions through the approved Action Catalog / Action resolver path, while the editor navigation only locates which formal `CharacterActionDefinitionSO` the designer wants to edit.

#### Scenario: Editor Navigation 不进入 Runtime Definition
- **WHEN** action definitions are compiled for runtime
- **THEN** editor navigation UI state, selected catalog row, picker search text and GraphView selection MUST NOT appear in runtime action definitions
- **AND** runtime action request resolution MUST still be driven by formal catalog data and action lifecycle

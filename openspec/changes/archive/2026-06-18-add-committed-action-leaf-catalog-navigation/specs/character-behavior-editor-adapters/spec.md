## ADDED Requirements

### Requirement: CommittedActionLeaf 使用 Action Catalog 导航 ActionDefinition
Character Behavior Editor MUST make `CommittedActionLeaf` open a formal Action Catalog navigation flow instead of hardcoding `Action.Dodge`. The navigation flow MUST resolve editable actions from `CharacterConfigSO.ActionCatalog`, an explicitly selected `CharacterActionCatalogSO`, or an approved equivalent formal character action catalog source. The selected entry MUST switch the same `CharacterBehaviorEditorWindow` into Committed Branch mode bound to the selected `CharacterActionDefinitionSO`.

#### Scenario: 单个 ActionDefinition 直接进入 Branch
- **GIVEN** Behavior Source graph contains a `CommittedActionLeaf`
- **AND** the resolved Action Catalog contains exactly one valid `CharacterActionDefinitionSO`
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST switch the same window to Committed Branch mode
- **AND** the Branch graph MUST bind that action definition
- **AND** the editor MUST NOT use a hardcoded Dodge asset path, Resources lookup, sample asset or hidden branch fallback

#### Scenario: 多个 ActionDefinition 先选择 Action
- **GIVEN** the resolved Action Catalog contains multiple valid action definitions
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST show an in-window action selection entry or approved equivalent picker
- **AND** the selectable entries MUST come from the Action Catalog
- **WHEN** the designer chooses one entry
- **THEN** the editor MUST switch the same window to Committed Branch mode for the chosen `CharacterActionDefinitionSO`
- **AND** it MUST NOT open a second Branch editor window

#### Scenario: Catalog 缺失时只显示诊断
- **GIVEN** Behavior Source graph contains a `CommittedActionLeaf`
- **AND** no formal character config or Action Catalog can be resolved
- **WHEN** the designer opens `CommittedActionLeaf`
- **THEN** the editor MUST show a clear diagnostic
- **AND** it MUST remain outside Committed Branch editing for an unknown action
- **AND** it MUST NOT default to `Action.Dodge`

#### Scenario: Branch 节点不进入主图
- **GIVEN** the Action Catalog contains `Action.Dodge` and another action
- **WHEN** the Behavior Source graph is displayed
- **THEN** the graph MAY show one `CommittedActionLeaf`
- **AND** it MUST NOT show Branch Root, Selector, Condition or TimelineNode nodes inside the Behavior Source graph
- **AND** those branch nodes MUST only appear after an action definition is selected in Committed Branch mode

### Requirement: Action Catalog 导航与 Ref UI Shell 解耦
Action Catalog navigation MUST be implemented as an editor adapter/data flow that can be hosted by the current Character Behavior Editor shell or the Ref source-ported shell. The navigation MUST NOT depend on Ref runtime types, Taco runtime trees, GraphView object identity or Behavior Source serialized action copies.

#### Scenario: Ref shell 更换不改变 catalog 数据源
- **GIVEN** Character Behavior Editor uses a Ref-style source-ported graph shell
- **WHEN** `CommittedActionLeaf` is opened
- **THEN** the action list MUST still come from the formal Action Catalog
- **AND** the selected action MUST still bind a project `CharacterActionDefinitionSO`
- **AND** no Ref `BaseTree`, `RunnableTree`, `RunnableNode` or Taco runtime object may become the action source

#### Scenario: 选择状态不是运行时权威
- **WHEN** a designer selects an action from the in-window catalog navigation UI
- **THEN** the selection MAY update editor session state
- **AND** it MUST NOT be serialized as runtime authority into GraphView nodes, ports, layout, selection state or Ref shell objects

## RENAMED Requirements
- FROM: `### Requirement: Timeline Editor 嵌入 Branch TimelineNode`
- TO: `### Requirement: Timeline Editor 定位 Branch TimelineNode`

## MODIFIED Requirements
### Requirement: Timeline Editor 定位 Branch TimelineNode
Committed Action Timeline Editor MUST support opening or focusing an independent timeline window for the TimelineNode selected in Committed Action Branch Editor. The independent window MUST use the selected TimelineNode serialized adapter to read and write timeline authoring track, clip and payload. The Branch graph window MUST only select TimelineNode, show timeline summary and provide an open/focus action. Timeline Editor MUST NOT embed its field/track/clip editor inside the Branch graph window and MUST NOT use Dodge-specific Directional / Backstep fields as formal save targets.

#### Scenario: 独立 Timeline Window 编辑选中节点
- **GIVEN** Branch Editor selected TimelineNode A
- **WHEN** the designer opens Timeline Editor
- **THEN** the system MUST open or focus independent `CommittedActionTimelineEditorWindow`
- **AND** the Timeline Editor MUST read and write TimelineNode A timeline authoring data
- **AND** TimelineNode B timeline authoring data MUST remain unchanged
- **AND** preview outcome MUST use the same action definition compiled runtime branch

#### Scenario: 独立窗口只作为同一数据的编辑入口
- **WHEN** the designer opens `Tools/3C/Committed Action Timeline Editor`
- **THEN** the tool MUST edit a formal `CharacterActionDefinitionSO` timeline node through selected TimelineNode adapter or approved equivalent selection
- **AND** saving MUST write back to the branch authoring TimelineNode
- **AND** it MUST NOT create a timeline definition independent from branch authoring

#### Scenario: Branch Graph 不嵌入 Timeline Field
- **WHEN** checking Committed Branch graph implementation
- **THEN** the Branch graph window MUST NOT contain a Timeline field, track hierarchy, clip view or clip inspector as an embedded timeline editor
- **AND** it MUST only expose node selection, node summary, node property panel and open/focus Timeline Editor action

## ADDED Requirements
### Requirement: Ref TreeDesigner 体验映射到项目 Adapter
Character Behavior Editor and Committed Branch mode MAY reuse Ref/wly970123 TreeDesigner interaction patterns, including fixed root presentation, left or in-window inspector, GraphView node shell, node panel, port/edge interaction, SearchWindow grouping and protected node capabilities. These interactions MUST map to this project's editor adapters and formal ScriptableObject authoring data. The editor MUST NOT save or execute Taco `BaseTree`, `RunnableTree`, `RunnableNode`, `RootNode`, `TimelinePlayer` or PlayableGraph runner as formal gameplay data.

#### Scenario: 固定 Root 只映射为项目 Root
- **WHEN** Committed Branch mode uses a Ref-style root node experience
- **THEN** the root MUST map to `CommittedActionBranchAuthoring.rootNodeId`
- **AND** it MUST NOT instantiate or save Ref `RootNode` or `EnterNode`
- **AND** runtime compiler MUST only consume this project's committed action branch definition

#### Scenario: SearchWindow 只创建正式节点类型
- **WHEN** the designer opens the node creation search window in Committed Branch mode
- **THEN** available entries MUST be limited to approved project node types such as Selector, Condition and Timeline
- **AND** root MUST NOT appear as a generic creatable node
- **AND** entries MUST NOT create Taco runtime node instances

#### Scenario: 节点属性面板通过 Adapter 写回
- **WHEN** the designer edits a branch node property through Ref-style node panel or inspector UI
- **THEN** the change MUST be written through this project's serialized adapter
- **AND** saving the action definition MUST persist the change in formal project authoring fields
- **AND** editor UI selection, layout and panel state MUST NOT become runtime authority

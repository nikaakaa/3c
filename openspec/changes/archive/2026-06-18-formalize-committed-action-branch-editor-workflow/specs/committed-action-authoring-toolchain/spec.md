## ADDED Requirements
### Requirement: Committed Action Branch Root Workflow
Committed Action Branch Editor MUST provide a Ref TreeDesigner-style protected root workflow for each `CharacterActionDefinitionSO` committed action branch. The branch root MUST be represented by the formal branch `rootNodeId` and a stable node id in `CommittedActionBranchAuthoring`, not by a temporary GraphView object. The editor MUST NOT create root through generic node creation, and runtime/compiler MUST NOT silently fallback to generated root data when root is missing.

#### Scenario: Branch 打开时识别固定 Root
- **GIVEN** `CharacterActionDefinitionSO` committed action branch has `rootNodeId`
- **WHEN** Committed Branch mode populates the graph
- **THEN** the node matching `rootNodeId` MUST be marked as the protected root
- **AND** the graph MUST display this root as the formal branch entry
- **AND** root identity MUST come from action definition authoring data

#### Scenario: Root 不能被普通编辑删除
- **GIVEN** Committed Branch graph has a protected root node
- **WHEN** the designer deletes selected graph elements
- **THEN** the editor MUST reject deletion of the protected root
- **AND** non-root nodes MAY still be deleted through the formal adapter
- **AND** the action definition MUST keep a valid `rootNodeId`

#### Scenario: 空 Branch 显式初始化
- **GIVEN** an action definition has no valid committed action branch root
- **WHEN** the designer invokes the explicit initialize branch command
- **THEN** the editor MUST create formal branch authoring data with branch id, root node id, stable node ids and editor layout
- **AND** the created data MUST be saved in `CharacterActionDefinitionSO`
- **AND** this initialization MUST NOT run implicitly in runtime, compiler, validator or evaluator as fallback

#### Scenario: Dodge 模板初始化
- **GIVEN** the selected action definition is formal Dodge
- **WHEN** the designer invokes the explicit Dodge branch template command
- **THEN** the editor MUST create or repair a formal selector root, Directional condition, Backstep condition, Directional TimelineNode and Backstep TimelineNode
- **AND** Directional and Backstep timeline payloads MUST live inside the corresponding TimelineNode authoring data
- **AND** the template MUST NOT create a behavior source graph, sample asset or Dodge-only runtime branch path

### Requirement: Committed Action Branch Node Property Panel
Committed Action Branch Editor MUST provide an in-window node property panel or approved equivalent node-inside property surface for root, selector, condition and timeline nodes. The property surface MUST read and write the selected node through stable node id and serialized adapter. Designers MUST NOT need to edit raw serialized arrays to configure normal branch node payloads.

#### Scenario: Condition 属性写回正式配置
- **GIVEN** Committed Branch graph has a selected Condition node
- **WHEN** the designer edits condition kind, request kind, required fact id or expected variant in the node property panel
- **THEN** the editor MUST write the payload to the selected node in `CharacterActionDefinitionSO`
- **AND** saving and compiling the action definition MUST see the modified condition payload
- **AND** no behavior graph asset MUST store a copy of that condition data

#### Scenario: Timeline 节点属性和独立窗口入口
- **GIVEN** Committed Branch graph has a selected Timeline node
- **WHEN** the node property panel is shown
- **THEN** it MUST display timeline node id, duration seconds, body claim and channels from the selected TimelineNode authoring data
- **AND** it MUST provide an action to open or focus the independent `CommittedActionTimelineEditorWindow`
- **AND** the Branch graph window MUST NOT embed a timeline field/track/clip editor as a second timeline editing surface

#### Scenario: 结构变化后 Selection 保持稳定
- **GIVEN** a node property panel is bound to selected node id
- **WHEN** nodes are added, deleted or reordered
- **THEN** the panel MUST re-resolve serialized properties by stable node id
- **AND** it MUST NOT continue editing an unrelated node because an array index changed

## MODIFIED Requirements
### Requirement: Toolchain Validation
Committed Action authoring toolchain MUST provide automated tests and static boundary validation proving that editor adapter, serialized asset, compiler, runtime evaluator and preview use the same formal data. Tests MUST cover legal branch, illegal branch, Dodge template equivalence, protected root behavior, explicit branch initialization, TimelineNode writeback, no fallback and runtime boundaries.

#### Scenario: 自动测试覆盖完整数据闭环
- **WHEN** running Committed Action authoring toolchain EditMode tests
- **THEN** tests MUST cover branch editor adapter writeback to action definition
- **AND** MUST cover explicit branch root/template initialization
- **AND** MUST cover protected root deletion rejection
- **AND** MUST cover node property panel or equivalent adapter writeback for condition payload
- **AND** MUST cover save, reload and compile to `CommittedActionBranchDefinition`
- **AND** MUST cover evaluator selection for Directional / Backstep or equivalent timeline path
- **AND** MUST cover illegal branch not generating half-finished runtime-consumable data

#### Scenario: 静态边界验证
- **WHEN** running static boundary tests
- **THEN** runtime source MUST NOT reference UnityEditor, GraphView, TimelinePlayer, PlayableGraph, Taco runner or branch editor types
- **AND** branch editor source MUST NOT directly call motion executor, animation presenter, blackboard writer or `CharacterController.Move`
- **AND** tests MUST confirm no formal path restores a legacy Dodge branch, Resources lookup, sample asset, hidden root fallback or duplicate Branch node editor window

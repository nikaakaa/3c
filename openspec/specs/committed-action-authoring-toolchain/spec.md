# committed-action-authoring-toolchain Specification

## Purpose
TBD - created by archiving change formalize-committed-action-authoring-toolchain. Update Purpose after archive.
## Requirements
### Requirement: Committed Action Branch Authoring Model
系统 MUST 提供通用 Committed Action branch authoring model，作为 `CharacterActionDefinitionSO` 或批准等价 action definition 的子模块。该 model MUST 能表达 branch id、固定 Branch Root node、稳定节点 id、selector / condition / timeline 节点、稳定 child 顺序、默认 body claim、timeline authoring 数据和 editor layout。该 model MUST 能编译为现有 `CommittedActionBranchDefinition` 或批准等价纯 runtime model，且 runtime model MUST NOT 持有 Unity Editor、GraphView、ScriptableObject、scene object、Animancer runtime object 或 Ref/Taco runner。

#### Scenario: Action Definition 保存 Branch 节点树
- **WHEN** 设计者在正式 action definition 中配置 Committed Action branch
- **THEN** 该 action definition MUST 保存 branch id、固定 Branch Root node id、节点列表和稳定 child 顺序
- **AND** TimelineNode MUST 保存正式 timeline authoring track、clip 和 payload
- **AND** authoring 数据 MUST 不依赖 editor view index 作为 runtime 选择顺序

#### Scenario: Branch Authoring 编译为 Runtime Definition
- **GIVEN** branch authoring 包含 selector、condition 和 timeline node
- **WHEN** action definition compiler 使用固定 tick compile context 编译该 action definition
- **THEN** compiler MUST 输出 `CommittedActionBranchDefinition`
- **AND** 输出 MUST 保留 selector child 顺序
- **AND** TimelineNode authoring seconds MUST 被编译为 runtime local tick 数据
- **AND** runtime evaluator MUST 能消费该 branch definition

### Requirement: Committed Action Branch Editor
系统 MUST 通过现有 `CharacterBehaviorEditorWindow` 的 Committed Branch mode 或批准等价入口编辑正式 `CharacterActionDefinitionSO` 内的 Committed Action branch authoring。该入口 MUST 复用 `CharacterBehaviorRefPortedGraphView` 作为唯一 Ref 风格节点树 shell，通过 `CommittedActionBranchRefPortedGraphAdapter` 或批准等价 adapter 写回 action definition。Branch Editor MUST NOT 继续使用 `ScrollView + Box card` 或批准等价 card/list 伪图作为 Branch graph 编辑路径。Branch Editor MUST NOT 新增第二套专用 Branch GraphView / Branch node editor 窗口或重复菜单入口。Branch Editor MUST NOT 编辑 `CharacterFramePipeline` phase、behavior source graph、motion executor、animation presenter、blackboard writer 或 Unity scene object binding。

#### Scenario: 打开正式 Dodge Branch
- **WHEN** 设计者打开 `Tools/3C/Character Behavior Editor`
- **THEN** 系统 MUST 打开或聚焦 `CharacterBehaviorEditorWindow`
- **AND** 窗口 MUST 提供 Committed Branch mode
- **AND** ObjectField MUST 限定为 `CharacterActionDefinitionSO`
- **AND** 编辑器 MUST 默认定位正式 Corin Dodge action definition
- **AND** 图中 MUST 以 `CharacterBehaviorRefPortedGraphView` 节点树展示固定 Branch Root、Dodge selector、Directional condition、Backstep condition、Directional timeline 和 Backstep timeline 或等价通用节点树
- **AND** 图中 MUST 提供可见 edge 表达 `childNodeIds` 拓扑

#### Scenario: 编辑节点后保存回 Action Definition
- **GIVEN** 设计者新增、删除、重排、连线、断线、拖动或修改一个 branch node
- **WHEN** 设计者保存
- **THEN** 修改 MUST 写回所选 `CharacterActionDefinitionSO`
- **AND** 保存后 `CharacterActionDefinitionSO.ToDefinition()` MUST 看到同一份 branch 修改
- **AND** 行为图 authoring asset MUST 不保存该 branch 数据

#### Scenario: 错误 card/list 图被移除
- **WHEN** 检查 Committed Action Branch Editor 实现
- **THEN** Branch graph 区域 MUST NOT 由 `ScrollView` 中的 `Box card` 列表表示
- **AND** MUST NOT 保留 card/list 和 GraphView 两套并行 branch 编辑路径
- **AND** MUST NOT 存在专用 `CommittedActionBranchGraphView` 作为第二节点编辑器
- **AND** MUST NOT 存在 `Tools/3C/Committed Action Branch Editor` 重复菜单入口

### Requirement: TimelineNode Panel
Branch Editor MUST 将 TimelineNode 的 timeline 编辑收敛为独立 `CommittedActionTimelineEditorWindow` 或批准等价独立 timeline adapter。Branch 节点树只负责选择 TimelineNode、显示 timeline 摘要、提供打开或聚焦独立 Timeline Editor 的入口。Timeline Editor MUST 读写该 TimelineNode 内的正式 timeline authoring 数据，MUST 继续使用 seconds authoring 和 fixed tick compile context 进行 preview / validation，MUST NOT 创建第二套 selector 或 timeline 数据权威。

#### Scenario: 选择 TimelineNode 后打开独立 Timeline Editor
- **GIVEN** Branch Editor 中选中了一个 TimelineNode
- **WHEN** 设计者打开 Timeline Editor
- **THEN** 系统 MUST 打开或聚焦独立 `CommittedActionTimelineEditorWindow`
- **AND** Timeline Editor MUST 读写该 TimelineNode 的 timeline authoring 数据
- **AND** 其它 TimelineNode 的 track、clip 和 payload MUST 不被修改
- **AND** Branch 节点树窗口 MUST NOT 内嵌 timeline panel 作为第二编辑面

#### Scenario: Timeline 快捷入口不成为第二权威
- **WHEN** 设计者通过保留的 Timeline Editor 菜单打开工具
- **THEN** 工具 MUST 打开或聚焦独立 Timeline Editor
- **AND** MUST 读写同一份 branch authoring 数据中的 TimelineNode timeline 数据
- **AND** MUST NOT 使用独立 Directional / Backstep 特例字段作为正式保存目标

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

### Requirement: Committed Action Branch GraphView 数据权威
Committed Action Branch GraphView 映射 MUST 只是 Editor-only Presentation Layer。GraphView node、port、edge、selection 和 layout MUST 通过 `CommittedActionBranchRefPortedGraphAdapter` / `CommittedActionBranchSerializedAdapter` 或批准等价 adapter 映射到 `CommittedActionBranchAuthoring` 的 stable node id、node kind、childNodeIds、condition payload、timeline payload 和 editor position。正式 runtime MUST 只消费 action definition compiler 输出，不得消费 GraphView object、Ref node 或 Taco runtime tree。

#### Scenario: GraphView 从项目数据建立节点
- **GIVEN** `CharacterActionDefinitionSO` 包含 Committed Action branch authoring
- **WHEN** Committed Branch mode populate
- **THEN** `CharacterBehaviorRefPortedGraphView` MUST 为每个 authoring node 创建对应 view node
- **AND** view node 的 stable id、node kind 和 position MUST 来自本项目 authoring 数据
- **AND** MUST NOT 从 Ref `BaseTree`、`RunnableTree` 或 sample asset 生成正式 graph

#### Scenario: Edge 写回 childNodeIds
- **GIVEN** 设计者在 Committed Branch graph 中连接 parent node 到 child node
- **WHEN** GraphView 提交该 edge
- **THEN** adapter MUST 将 child stable id 写入 parent node 的 `childNodeIds`
- **AND** 保存后 runtime selector child 顺序 MUST 稳定可编译
- **AND** MUST NOT 依赖 GraphView 非确定枚举顺序作为 runtime 权威

#### Scenario: TimelineNode 选中打开独立 Timeline Editor
- **GIVEN** Committed Branch graph 中选中了一个 TimelineNode
- **WHEN** 设计者打开 Timeline Editor
- **THEN** 系统 MUST 打开或聚焦独立 `CommittedActionTimelineEditorWindow`
- **AND** Timeline Editor MUST 读写该 TimelineNode 内的 `CommittedActionBranchTimelineAuthoring`
- **AND** 其它 TimelineNode 的 timeline 数据 MUST 不被修改
- **AND** Branch 节点树窗口 MUST NOT 内嵌 timeline panel 作为第二编辑面

#### Scenario: Runtime 边界保持干净
- **WHEN** 检查正式 runtime 源码和 asmdef
- **THEN** runtime MUST NOT 引用 UnityEditor、GraphView、Committed Action Branch editor view、Ref `TreeRunner`、Ref `RunnableNode`、Ref `TimelinePlayer` 或 PlayableGraph runner
- **AND** GraphView layout、selection、port 和 edge object MUST NOT 出现在 rollback snapshot、action lifecycle frame 或 committed action runtime definition 中

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

### Requirement: Ref 源码级移植不得创建第二 Action Authoring 路径
Committed Action authoring toolchain MUST 将 Ref 源码级 editor 移植视为现有正式 authoring toolchain 的 UI 替换，而不是新的 action authoring path。Branch graph、Timeline editor、inspector、preview 和 tests MUST 继续通过 `CharacterActionDefinitionSO`、Committed Action branch authoring、TimelineNode authoring、project serialized adapter、validator、compiler 和 runtime evaluator 形成同一条数据链路。

#### Scenario: 替换 UI 后编译路径不变
- **GIVEN** 设计者通过 Ref-equivalent Branch Graph 和 Timeline Editor 修改 Dodge branch 与 timeline
- **WHEN** 保存并调用 action definition compiler
- **THEN** compiler MUST 从同一个 `CharacterActionDefinitionSO` 读取 branch、TimelineNode、track、clip 和 payload
- **AND** 输出 MUST 仍是项目正式 `CommittedActionBranchDefinition` 和 `ActionTimelineDefinition` 或批准等价 runtime model
- **AND** MUST NOT 读取 Ref `BaseTree`、`RunnableTree`、`Timeline`、`Track`、`Clip` 或 sample asset

#### Scenario: 无 fallback 和无重复入口
- **WHEN** 检查 editor 菜单、窗口和 adapter
- **THEN** MUST NOT 存在旧 card/list branch editor、旧 half-port timeline editor、Dodge-only branch authoring 正式入口或隐藏 fallback editor path
- **AND** 保留的菜单入口 MUST 指向同一套正式 Ref-equivalent editor shell 和同一份正式 serialized data


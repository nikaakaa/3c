# character-behavior-authoring-source-boundary Specification

## Purpose
定义 Character Behavior Authoring 与正式 ActionDefinition 的数据源边界，确保 Behavior Graph 只表达 Source 拓扑，Committed Action 的 selector、timeline、track、clip 和 payload 由正式 ActionDefinition 持有。
## Requirements
### Requirement: Behavior Authoring Graph 只表达 Source 拓扑
Character Behavior Authoring Graph MUST 只表达角色行为提交 source 的拓扑和顺序。Graph authoring MAY 保存 root、ordered composite、Locomotion leaf、CommittedAction leaf、edge 和 editor position，但 MUST NOT 保存 Dodge selector、Directional timeline、Backstep timeline、track、clip、motion payload、animation key、window 或 cue 作为正式运行时数据源。

#### Scenario: Graph 编译为 Source Runtime Definition
- **GIVEN** behavior authoring graph 包含 root、ordered composite、Locomotion leaf 和 CommittedAction leaf
- **WHEN** behavior compiler 编译该 graph
- **THEN** 输出 MUST 包含 `CharacterBehaviorRuntimeDefinition` 或等价 source runtime definition
- **AND** leaf 顺序 MUST 是 Locomotion 后 CommittedAction
- **AND** 输出 MUST NOT 包含 Dodge timeline clip payload

#### Scenario: Graph 不拥有 Dodge 数据
- **WHEN** 检查 behavior authoring graph 的正式 schema
- **THEN** schema MUST NOT 把 Dodge selector、Directional timeline 或 Backstep timeline 定义为正式 source graph 字段
- **AND** 迁移期 legacy 字段 MUST NOT 被正式 compiler 当作 fallback 消费

### Requirement: Committed Action 数据源属于正式 Action Definition
Dodge selector、Directional timeline、Backstep timeline 和其它 committed action timeline 数据 MUST 来自正式 `CharacterActionDefinitionSO`、action catalog 或批准的等价 action definition。Behavior graph MUST NOT 复制该数据，也 MUST NOT 在缺少正式 action definition 时生成隐藏默认 branch。

#### Scenario: Dodge Branch 从 ActionDefinition 编译
- **GIVEN** 正式 Dodge `CharacterActionDefinitionSO` 包含 Dodge selector 和两个 timeline
- **WHEN** action definition 被编译
- **THEN** 系统 MUST 通过 `CharacterActionDefinitionSO.ToDefinition()` 或等价正式编译路径得到 `CommittedActionBranchDefinition`
- **AND** Directional / Backstep timeline MUST 来自该 action definition

#### Scenario: 缺少正式 ActionDefinition 报错
- **GIVEN** behavior graph 包含 CommittedAction leaf
- **AND** runtime composition 缺少正式 action definition 或 action catalog reference
- **WHEN** 系统构建角色 runtime 配置
- **THEN** 系统 MUST 报告正式配置错误
- **AND** MUST NOT 从 `Behavior/Samples`、Resources、legacy embedded branch 或代码默认值补齐

### Requirement: Compiler 职责必须拆分
Behavior compiler MUST 只负责编译 source graph；Action definition compiler/validator MUST 负责编译 action branch、selector 和 timeline。组合层 MAY 同时引用两者，但 MUST 保持两类 compiler 的输出边界清晰。

#### Scenario: Behavior Compiler 不编译 Action Timeline
- **WHEN** behavior compiler 处理一个完整 source graph
- **THEN** 它 MUST 输出 source execution tree 和 `CharacterBehaviorRuntimeDefinition`
- **AND** MUST NOT 输出或修改 `ActionTimelineDefinition`
- **AND** MUST NOT 通过 Action.Dodge 字符串决定 timeline 结构

#### Scenario: Action Compiler 不编译 Source Graph
- **WHEN** action definition compiler/validator 处理 Dodge action definition
- **THEN** 它 MUST 输出或验证 `CommittedActionBranchDefinition` 和 `ActionTimelineDefinition`
- **AND** MUST NOT 创建 behavior root、parallel source node 或 Locomotion leaf

### Requirement: Editor 只跨窗口定位，不复制数据
Character Behavior Editor MAY 显示 CommittedAction leaf 并提供打开 Committed Action Timeline Editor 的入口，但 MUST NOT 复制、保存或持有 Dodge timeline 数据。Timeline Editor MUST 直接编辑正式 action definition。

#### Scenario: Graph Editor 打开 Timeline Editor
- **GIVEN** Graph Editor 中存在 CommittedAction leaf
- **WHEN** 设计者选择打开 Dodge timeline
- **THEN** 系统 MAY 打开 `Tools/3C/Committed Action Timeline Editor`
- **AND** MUST 传递或选择正式 `CharacterActionDefinitionSO`
- **AND** MUST NOT 在 graph asset 中创建第二份 Dodge timeline

#### Scenario: 保存 Graph 不改 Timeline
- **GIVEN** 设计者在 Graph Editor 中移动节点或修改 edge
- **WHEN** 设计者保存 graph
- **THEN** 保存 MUST 只影响 source graph topology 或 editor position
- **AND** MUST NOT 修改 `ActionTimelineTrackAuthoring`、`ActionTimelineClipAuthoring` 或 action timeline payload

### Requirement: Legacy Sample 与 Embedded Dodge 数据必须退役
系统 MUST 退役 sample-only behavior asset、sample-only compiled runtime definition 和 behavior asset 内嵌 Dodge branch 作为正式入口。迁移期可保留诊断或一次性迁移工具，但正式 runtime 和正式 editor 默认入口 MUST 不依赖它们。

#### Scenario: Legacy 数据只产生诊断
- **GIVEN** legacy behavior authoring asset 中仍有 Dodge branch 或 timeline 字段
- **WHEN** 正式 compiler 处理该 asset
- **THEN** compiler MUST 不使用该字段生成 committed action branch
- **AND** MAY 报告 legacy data migration diagnostic
- **AND** MUST 指向正式 action definition 作为修复路径

#### Scenario: Sample Asset 不作为默认入口
- **WHEN** 打开 Character Behavior Editor 或 Committed Action Timeline Editor
- **THEN** editor MUST NOT 默认加载 `Behavior/Samples` authoring asset
- **AND** MUST NOT 生成 sample-only runtime definition 作为正式 gameplay 输入

### Requirement: Authoring Source Boundary 可测试
系统 MUST 提供 EditMode 测试和静态边界测试，证明 behavior authoring、action definition 和 editor 边界没有双数据源。

#### Scenario: 自动测试覆盖边界
- **WHEN** 运行 behavior authoring source boundary 测试
- **THEN** 测试 MUST 覆盖 source graph 编译
- **AND** MUST 覆盖正式 action definition 编译 Dodge branch
- **AND** MUST 覆盖缺少 action definition 的错误
- **AND** MUST 覆盖 Graph Editor 保存不修改 timeline
- **AND** MUST 覆盖 sample-only 入口不再作为默认入口

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


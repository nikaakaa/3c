## MODIFIED Requirements
### Requirement: Committed Action Branch Editor
系统 MUST 通过现有 `CharacterBehaviorEditorWindow` 的 Committed Branch mode 或批准等价入口编辑正式 `CharacterActionDefinitionSO` 内的 Committed Action branch authoring。该入口 MUST 复用 `CharacterBehaviorRefPortedGraphView` 作为唯一 Ref 风格节点树 shell，通过 `CommittedActionBranchRefPortedGraphAdapter` 或批准等价 adapter 写回 action definition。Branch Editor MUST NOT 继续使用 `ScrollView + Box card` 或批准等价 card/list 伪图作为 Branch graph 编辑路径。Branch Editor MUST NOT 新增第二套专用 Branch GraphView / Branch node editor 窗口或重复菜单入口。Branch Editor MUST NOT 编辑 `CharacterFramePipeline` phase、behavior source graph、motion executor、animation presenter、blackboard writer 或 Unity scene object binding。

#### Scenario: 打开正式 Dodge Branch
- **WHEN** 设计者打开 `Tools/3C/Character Behavior Editor`
- **THEN** 系统 MUST 打开或聚焦 `CharacterBehaviorEditorWindow`
- **AND** 窗口 MUST 提供 Committed Branch mode
- **AND** ObjectField MUST 限定为 `CharacterActionDefinitionSO`
- **AND** 编辑器 MUST 默认定位正式 Corin Dodge action definition
- **AND** 图中 MUST 以 `CharacterBehaviorRefPortedGraphView` 节点树展示 Dodge selector、Directional condition、Backstep condition、Directional timeline 和 Backstep timeline 或等价通用节点树
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

## ADDED Requirements

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

## ADDED Requirements
### Requirement: Behavior / Branch Editor 必须源码级替换为 Ref TreeDesigner 组件结构
Character Behavior Editor 与 Committed Branch mode MUST 将当前半移植或自研节点树 shell 替换为 Ref/Taco TreeDesigner / GraphView 的源码级等价组件结构。实现可以使用项目命名和项目 adapter，但 MUST 提供 Ref 等价的 GraphView shell、node view、port view、edge view、SearchWindow、selection、fixed root 展示、node property panel 和受保护节点能力。旧 card/list 伪图、重复 branch editor 窗口或半自研节点编辑路径 MUST NOT 作为正式编辑入口保留。

#### Scenario: GraphView 从项目 adapter 建立节点树
- **WHEN** 打开 Character Behavior Editor 或 Committed Branch mode
- **THEN** GraphView MUST 从项目正式 behavior source authoring 或 committed action branch authoring adapter 建立节点、端口、连线和 layout
- **AND** MUST NOT 从 Ref `BaseTree`、`RunnableTree`、sample asset 或临时 editor object 生成正式 graph

#### Scenario: 固定 Root 与 SearchWindow 对齐 Ref 体验
- **WHEN** graph 包含固定 root
- **THEN** root MUST 作为 protected root node 展示并映射到项目正式 root id
- **AND** root MUST NOT 通过普通 SearchWindow 创建或删除
- **WHEN** 设计者打开 SearchWindow
- **THEN** entries MUST 只创建项目批准的 node kind
- **AND** MUST NOT 创建 Taco runtime node instance

#### Scenario: Node Panel 只写回项目正式数据
- **WHEN** 设计者在 node panel 修改 selector、condition、timeline node 或 behavior source node 属性
- **THEN** 修改 MUST 通过 stable node id 写回项目 adapter
- **AND** GraphView selection、layout、port 和 edge object MUST NOT 成为 runtime 权威

### Requirement: Behavior Graph 与 Timeline Editor 继续分窗且不分裂数据
Character Behavior Editor MUST 继续只编辑 behavior source topology 或 Committed Action branch 节点树；Committed Action Timeline Editor MUST 继续作为独立窗口编辑 selected TimelineNode 的 timeline field / track / clip / payload。两个窗口 MAY 互相打开或定位，但 MUST NOT 复制、保存或编译对方的数据。

#### Scenario: TimelineNode 打开独立 Timeline Editor
- **GIVEN** Committed Branch graph 中选中了 TimelineNode
- **WHEN** 设计者打开 timeline
- **THEN** 系统 MUST 打开或聚焦独立 `CommittedActionTimelineEditorWindow`
- **AND** 该窗口 MUST 读写该 TimelineNode 的 timeline authoring 数据
- **AND** Branch graph 窗口 MUST NOT 内嵌 timeline field、track view、clip view 或 clip inspector

#### Scenario: 保存 Graph 不修改 Timeline 数据
- **WHEN** 设计者在 Character Behavior Editor 中移动节点、创建 edge 或编辑 source node
- **THEN** 保存内容 MUST 限定为对应 graph / branch node authoring 数据
- **AND** MUST NOT 修改其它 TimelineNode 的 track、clip、motion payload、animation key、window 或 cue

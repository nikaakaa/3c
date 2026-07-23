# btsmtl-tree-inspector-information-architecture Specification

## Purpose

定义Tree Workspace中左侧Data、右侧Details、运行时观察和内部模块的正式信息架构。

## Requirements

### Requirement: Tree Workspace必须分离Data与Details区域

Tree Workspace MUST在左侧Navigator提供唯一Graph Data Catalog，并在右侧Details只显示当前选中Node/Edge的BTSMTL authoring内容，或无选择时的Graph Authoring Settings。Data与Details MAY同时可见，但 MUST不复制条目、字段、命令或可写状态。角色动画producer binding、Blend、playback lifecycle和Animancer配置 MUST不进入Tree Details可写内容。旧Data/Inspector互斥页签与左侧Inspector路径 MUST删除。

#### Scenario: 打开角色RootTree

- **WHEN** 作者从Character Pipeline打开RootTree
- **THEN** 左侧Navigator MUST显示唯一Graph Data Catalog
- **AND** 右侧Details MUST显示Graph Authoring Settings或明确空状态
- **AND** 两个区域 MUST不显示动画播放生命周期字段

#### Scenario: 选择Transition edge

- **WHEN** 作者选择一条StateMachine Transition edge
- **THEN** 右侧Details MUST显示priority、condition ownership、rule与interruption
- **AND** 左侧Data Catalog MUST保持当前context且不复制这些字段
- **AND** Details MUST不显示HandoffRole、animation strategy、duration、curve或producer binding

#### Scenario: 打开动画表现配置

- **WHEN** 作者需要调整Animation Track、Pose Graph、Blend Policy或producer source binding
- **THEN** Tree Workspace MUST精确导航到Timeline Editor或Character Animation Presentation正式入口
- **AND** Tree Details MUST不创建同一数据的第二写入口

### Requirement: Graph Authoring Settings 必须排除运行时生命周期字段

Tree Workspace Details与BaseTreeAsset Inspector的图级属性投影 MUST只显示可保存且可编辑的authoring配置。非序列化Tree lifecycle状态，包括`Running`、`State`及等价runtime status，MUST不通过通用属性扫描、字段注解或独立Details区块显示。

#### Scenario: Authoring模式查看RunnableTree

- **WHEN** 作者在Authoring模式打开任意RunnableTree
- **THEN** 左侧Data区域和右侧Details MUST不显示`Running`或`State`字段
- **AND** 系统 MUST不将这些状态写入authoring asset

#### Scenario: Live Debug观察执行状态

- **WHEN** 作者切换TreeWindow到Live Debug并选择有效runtime target/instance
- **THEN** Graph运行状态 MUST由RuntimeDebugSession的source-mapped Trace overlay与只读Live Details呈现
- **AND** 系统 MUST不恢复直接读取runtime Tree/Node字段的Inspector路径

### Requirement: Data区域筛选必须在窄栏中保持source-aware

左侧Data区域 MUST始终提供文本搜索和显式`All`、`Input`、`Blackboard` source切换。Blackboard专属Context与Scope条件 MUST只在需要时呈现；Input条目 MUST不被赋予虚假的Blackboard scope、owner或写入能力。筛选、分组折叠和条目展开状态 MUST保持editor-only view-state，并且Details selection变化 MUST不重置这些状态。

#### Scenario: 只查看Input

- **WHEN** 作者选择Input source
- **THEN** Data区域 MUST显示Input Values与Action Requests
- **AND** Blackboard Context/Scope控件 MUST不占用默认数据列表空间
- **AND** Input条目 MUST保持external read-only语义

#### Scenario: 过滤当前图的Blackboard

- **WHEN** 作者在Blackboard source下选择Current Context或具体Scope
- **THEN** Catalog MUST只显示匹配的Blackboard declaration
- **AND** 系统 MUST不修改declaration的owner、scope、identity或runtime address

### Requirement: TreeWindow 运行时模式必须保持窗口级边界

`Authoring / Live Debug` MUST是整个TreeWindow的模式，不得成为Navigator、Details或Bottom Dock的局部状态。Live Debug下全部authoring命令 MUST保持只读。TreeWindow MUST通过共享RuntimeDebugSession为当前binding获取/释放Graph与StateMachine Live interest，并读取共享provider current state或显式Capture history；它只持有当前TreeWindow自己的Graph runtime binding。

#### Scenario: Live Debug同时查看Data与Details

- **WHEN** 作者在Live Debug模式下同时查看左侧Data和右侧Details
- **THEN** 两个区域 MUST使用同一shared target与Capture history position
- **AND** Details selection变化 MUST不重置Graph Follow或Pin binding
- **AND** 作者不得通过任一区域写入Graph、Blackboard、Input或runtime state

#### Scenario: Graph与Timeline同时打开

- **WHEN** 作者同时打开TreeWindow和TimelineEditorWindow
- **THEN** TreeWindow MUST只修改自己的Graph runtime binding
- **AND** TimelineEditorWindow的Timeline playback binding MUST保持不变
- **AND** 停止Capture后两个窗口 MUST在同一shared Capture history position显示各自overlay

#### Scenario: 创建TreeWindow

- **WHEN** Editor创建TreeWindow及其Workspace视觉树
- **THEN** USS MUST使用当前Unity支持的选择器
- **AND** 创建过程 MUST不因不支持的选择器产生stylesheet parser error

#### Scenario: Play Mode domain reload后恢复当前Graph

- **WHEN** 当前TreeWindow经历Play Mode domain reload并重建UI
- **THEN** 窗口 MUST只按已保存serialized owner、property path与GraphAuthoringId恢复当前Graph
- **AND** 窗口 MUST重建自己的Graph runtime binding，不得恢复旧runtime instance
- **AND** locator缺失或identity不一致时 MUST停止恢复，不得按名称、路径近似或窗口顺序选择其它Graph

### Requirement: Tree Editor内部职责必须由独立模块拥有

Tree Editor MUST保留现有TreeWindow、TreeView、Graph Data Catalog和Details对外入口，但graph mutation、node/edge visual、selection Details、Graph Data Catalog、window navigation/domain reload restore与runtime overlay MUST由职责独立的内部模块拥有。mutation模块 MUST是create/link/delete/paste/condition cleanup与Undo的唯一owner；Details与Data Catalog MUST不互相持有可写状态；window navigation与runtime overlay MUST分别拥有authoring locator和window-local debug binding。系统 MUST不创建第二套Graph写入口、运行时Tree读取路径或按名称近似恢复。

#### Scenario: 删除StateMachine Transition edge

- **WHEN** 作者在TreeView删除带condition graph的Transition edge
- **THEN** TreeView MUST把唯一mutation request交给graph mutation模块
- **AND** mutation模块 MUST在同一Undo边界更新edge、condition ownership与identity
- **AND** Details或visual layer MUST不再次修改asset

#### Scenario: Data与Details同时可见

- **WHEN** 作者在同一TreeWindow选择Node或Edge
- **THEN** Data Catalog的source/filter/foldout状态 MUST由catalog模块保持
- **AND** Details MUST只根据当前selection投影authoring内容
- **AND** selection变化 MUST不改变Graph locator或runtime binding

#### Scenario: Play Mode domain reload

- **WHEN** TreeWindow经domain reload恢复当前Graph
- **THEN** navigation模块 MUST只使用serialized owner、property path和GraphAuthoringId恢复authoring target
- **AND** runtime overlay模块 MUST创建新的window-local binding
- **AND** MUST不恢复旧runtime instance或按窗口顺序选择其它Graph

#### Scenario: Graph与Timeline同时Live Debug

- **WHEN** TreeWindow和TimelineEditorWindow同时观察同一Session
- **THEN** Tree runtime overlay MUST只修改自己的Graph binding
- **AND** 共享provider current state与Capture history position MUST保持统一
- **AND** Timeline窗口本地binding MUST不被Tree导航或selection修改

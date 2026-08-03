## MODIFIED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

唯一`GraphAuthoringEditorShell` MUST提供Toolbar、Navigator、Graph Canvas、Details与可折叠Bottom Dock五个通用区域。Shell MUST通过Graph Authoring Domain Framework装配共享Navigator、Canvas、Node View、Port View、Details host和Bottom Dock，再从显式domain adapter取得领域document、capability、mutation与内容投影。其它区域 MUST不保存第二份node、edge或selection集合。

#### Scenario: 打开BTSMTL RootTree

- **WHEN** 作者通过正式入口打开RootTree
- **THEN** Shell MUST装配共享作者表面与BTSMTL domain adapters
- **AND** MUST不创建Pose document、Pose Preview或动画字段

#### Scenario: 打开Character Pose Graph

- **WHEN** 作者从显式CharacterAnimationPresentationProfile上下文打开Pose Graph
- **THEN** 同一Shell与共享作者表面 MUST装配Pose domain adapters
- **AND** MUST不创建BaseGraph、BaseNode或第二套GraphView

### Requirement: Graph Authoring Editor Shell必须只拥有通用编辑交互

系统 MUST提供唯一`GraphAuthoringEditorShell`，只拥有窗口生命周期、区域布局、模式切换、dirty owner协调与显式重操作入口。GraphView画布、node/port投影、selection、搜索、clipboard、Undo/Redo、breadcrumb和Details host MUST由Graph Authoring Domain Framework统一提供。Shell与共享框架 MUST不读取BTSMTL State、ConditionRule、Blackboard、BTAbortPolicy、Pose Bone Mask或动画业务字段。

#### Scenario: 打开BTSMTL Graph

- **WHEN** BaseTree asset通过正式入口打开
- **THEN** Shell MUST装配BTSMTL document、capability与mutation
- **AND** Shell或共享View MUST不按BaseNode subtype硬编码创建规则

#### Scenario: 打开Pose Graph

- **WHEN** CharacterPresentationPoseGraphAsset通过正式入口打开
- **THEN** 同一Shell与共享View MUST装配Pose document、capability与mutation
- **AND** MUST不调用Pose专用GraphView生命周期实现

### Requirement: 每个Graph领域必须拥有独立数据与端口合同

BTSMTL MUST继续唯一使用`BaseGraph`、`BaseNode`、`BaseEdge`、`PropertyPort`与`PropertyEdge`表达Gameplay authoring。Pose Graph MUST使用独立Pose Graph data、typed Pose node payload、typed Pose Port与Pose Edge表达Presentation pose composition。两者 MUST共同适配Graph Authoring Domain Framework的稳定document、node、port和mutation合同，但框架 MUST不要求它们继承同一runtime node或共享序列化edge。跨领域拖线、复制节点或粘贴payload MUST被拒绝。

#### Scenario: 从BTSMTL复制节点到Pose Graph

- **WHEN** clipboard payload的domain identity为BTSMTL且当前document为Pose Graph
- **THEN** 共享clipboard policy MUST拒绝粘贴并报告domain不匹配
- **AND** MUST不尝试把BaseNode字段映射为Pose node payload

#### Scenario: Pose端口连接

- **WHEN** 作者连接两个Pose domain ports
- **THEN** 共享Canvas MUST调用Pose port policy和Presentation Mutation
- **AND** MUST不调用BTSMTL PropertyPort兼容规则

### Requirement: 旧BTSMTL窗口路径必须迁移而不是并存

现有BTSMTL Graph作者入口 MUST通过原地抽象迁移到`GraphAuthoringEditorShell`、Graph Authoring Domain Framework和BTSMTL domain adapter。共享Canvas、Node View、Port View、Details、Navigator与StateMachine表面 MUST从`BaseTreeWindow`、`BaseTreeView`、`BaseNodeView`、`BasePortView`、`BaseTreeInspectorView`与`GraphDataCatalog`的现有实现提取，MUST不通过新写替代GraphView完成迁移。Pose Graph MUST随后迁移到该同一实现与Mutation宿主。系统只有在原BTSMTL全部操作已由提取后的同一实现承接且Pose已接入后，才 MUST删除被抽空的`BaseTreeWindow`/`BaseTreeView`专用壳、Pose专用GraphView/NodeView、重复catalog/clipboard/Undo/Inspector路径与领域外公共静态入口；不得保留Workbench或兼容窗口。

#### Scenario: 直接打开BaseTreeAsset

- **WHEN** 用户双击BaseTreeAsset
- **THEN** 正式入口 MUST打开基于Shell和共享领域框架的BTSMTL document
- **AND** MUST保持迁移前BTSMTL窗口分区、节点、端口、黑板拖拽、Inspector和导航行为
- **AND** MUST不同时打开第二套替代GraphView

#### Scenario: 直接打开Pose Graph asset

- **WHEN** 用户双击Pose Graph asset
- **THEN** 正式入口 MUST打开基于同一Shell和共享领域框架的Pose document
- **AND** MUST不保留Pose专用GraphView或旧Inspector

#### Scenario: 删除BTSMTL旧类型

- **WHEN** 计划删除一个BTSMTL Window、View、Node、Port、Inspector或Data Catalog类型
- **THEN** 同一类型拥有的每项现有操作 MUST已由从其代码提取出的唯一共享实现承接
- **AND** 任一操作尚未映射时 MUST保留该实现并停止删除

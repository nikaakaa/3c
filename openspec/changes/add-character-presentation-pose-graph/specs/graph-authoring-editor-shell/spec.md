## ADDED Requirements

### Requirement: Graph Authoring Editor Shell必须只拥有通用编辑交互

系统 MUST提供唯一`GraphAuthoringEditorShell`，只拥有窗口生命周期、GraphView画布、selection、搜索、创建菜单、clipboard、复制粘贴、Undo/Redo、breadcrumb、Inspector宿主、dirty owner协调和只读diagnostics overlay。Shell MUST通过显式domain adapter取得document、node catalog、port policy、mutation、Inspector和diagnostics，不得读取BTSMTL State、ConditionRule、Blackboard、BTAbortPolicy、Pose Bone Mask或动画业务字段。

#### Scenario: 打开BTSMTL Graph

- **WHEN** BaseTree asset通过正式入口打开
- **THEN** Shell MUST装配BTSMTL domain adapters并显示原有作者交互
- **AND** Shell MUST不包含按BaseNode subtype硬编码的创建或连接规则

#### Scenario: 打开Pose Graph

- **WHEN** CharacterPresentationPoseGraphAsset通过正式入口打开
- **THEN** 同一Shell MUST装配Pose Graph domain adapters
- **AND** MUST不创建BaseGraph、BaseNode或PropertyEdge副本

### Requirement: 每个Graph领域必须拥有独立数据与端口合同

BTSMTL MUST继续唯一使用`BaseGraph`、`BaseNode`、`BaseEdge`、`PropertyPort`与`PropertyEdge`表达Gameplay authoring。Pose Graph MUST使用独立Pose Graph data、Pose Node、typed Pose Port与Pose Edge表达Presentation pose composition。Shell MUST不要求两个领域继承同一runtime node或共享序列化edge；跨领域拖线、复制节点或粘贴payload MUST被拒绝。

#### Scenario: 从BTSMTL复制节点到Pose Graph

- **WHEN** clipboard payload的domain identity为BTSMTL且当前document为Pose Graph
- **THEN** Shell MUST拒绝粘贴并报告domain不匹配
- **AND** MUST不尝试把BaseNode字段映射为Pose Node

#### Scenario: Pose端口连接

- **WHEN** 作者连接两个Pose domain ports
- **THEN** Shell MUST调用Pose port policy和mutation adapter
- **AND** MUST不调用BTSMTL PropertyPort兼容规则

### Requirement: Graph Shell mutation必须落到真实Domain Owner

所有create/delete/connect/disconnect/paste/rename/subgraph reference mutation MUST通过当前domain adapter作用于真实serialized owner，并进入同一Undo组。Shell、GraphView元素和diagnostics model MUST不保存第二份node/edge集合。Inline和shared document切换 MUST保持各自真实dirty owner。

#### Scenario: 删除Pose节点

- **WHEN** 作者在Pose Graph画布删除一个节点及其edge
- **THEN** Pose mutation adapter MUST原子修改Pose Graph asset或inline owner
- **AND** GraphView MUST只从修改后的domain document重建显示

#### Scenario: Undo shared subgraph切换

- **WHEN** 作者把inline subgraph抽取为shared asset后执行Undo
- **THEN** Undo MUST恢复真实owner的互斥reference状态
- **AND** Shell MUST不从缓存节点集合伪造恢复

### Requirement: Graph Shell diagnostics必须只读且来自领域正式结果

Shell MUST只通过domain diagnostics adapter显示编译、validation或runtime snapshot的只读source mapping。Shell MUST不自行运行Gameplay Interpreter、Pose Evaluator、curve evaluator或状态选择来重建diagnostics。没有合法runtime target或artifact时 MUST显示明确Unavailable/Stale，而不是使用authoring默认值。

#### Scenario: Pose Runtime Live Debug

- **WHEN** 当前Pose Graph有匹配ProjectionRevision的runtime snapshot
- **THEN** overlay MAY按PoseNodeId显示availability与contribution
- **AND** 显示数据 MUST来自正式FinalAnimationPoseFrame/Trace

#### Scenario: Projection已Stale

- **WHEN** Pose Graph修改后Projection尚未重建
- **THEN** overlay MUST显示Stale并停止绑定旧node source map
- **AND** MUST不在Editor内临时编译一份未发布runtime program冒充Live结果

### Requirement: 旧BTSMTL窗口路径必须迁移而不是并存

现有BTSMTL Graph作者入口 MUST迁移到Graph Authoring Editor Shell和BTSMTL domain adapter。系统 MUST删除Shell已经接管的旧window/view交互实现、领域外公共静态入口和重复clipboard/Undo/Inspector路径。Pose Graph MUST通过同一Shell基础设施获得独立asset入口；不得新增Workbench或复制一套GraphView框架。

#### Scenario: 直接打开BaseTreeAsset

- **WHEN** 用户双击BaseTreeAsset
- **THEN** 正式入口 MUST打开基于Shell的BTSMTL document
- **AND** MUST不同时打开旧BaseTreeWindow实现或Workbench

#### Scenario: 直接打开Pose Graph asset

- **WHEN** 用户双击Pose Graph asset
- **THEN** 正式入口 MUST打开基于Shell的Pose domain document
- **AND** BTSMTL breadcrumb和runtime context MUST不被写入Pose asset

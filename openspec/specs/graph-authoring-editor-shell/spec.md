# graph-authoring-editor-shell Specification

## Purpose

定义Tree、AI与Character Pose Graph共享的唯一图作者工作区外壳、区域装配、编辑器状态和显式重操作边界。
## Requirements
### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

唯一`GraphAuthoringEditorShell` MUST提供Toolbar、Navigator、Graph Canvas、Details与可折叠Bottom Dock五个通用区域。Shell MUST继续通过显式domain adapter取得各区域内容，不得按BTSMTL Node、AI Node、Pose Node、AnimationChannel、Blackboard或Runtime Trace类型构造领域UI。Graph Canvas MUST继续承载唯一GraphView、selection、breadcrumb、搜索、clipboard和Undo链路；其它区域 MUST不保存第二份node、edge或selection集合。

#### Scenario: 打开BTSMTL RootTree

- **WHEN** 作者通过正式入口打开RootTree
- **THEN** Shell MUST在Navigator装配当前Graph Data Catalog、在Graph Canvas装配唯一`GraphAuthoringCanvasView`的BTSMTL domain adapter、在Details装配BTSMTL capability presenter
- **AND** MUST不创建Pose Graph Navigator、Pose Preview或动画字段

#### Scenario: 打开Character Pose Graph

- **WHEN** 作者从显式CharacterAnimationPresentationProfile上下文打开Pose Graph
- **THEN** 同一Shell MUST装配Pose domain Navigator、同一`GraphAuthoringCanvasView`、Details与Bottom Dock
- **AND** MUST不创建BaseGraph、BaseNode或第二套GraphView

### Requirement: Workspace布局状态必须是editor-only且不污染authoring

Navigator、Details和Bottom Dock的宽度、展开、折叠、选中页签、搜索、分组与Preview面板布局 MUST只保存为window-local或Editor view-state。任何布局变化 MUST不修改Graph、Timeline、Profile、Definition、Rig、Program或Projection revision。窗口尺寸不足时区域 MAY按确定规则折叠，但 MUST不切换到旧Data/Inspector互斥写路径。

#### Scenario: 折叠Bottom Dock

- **WHEN** 作者折叠Preview与Diagnostics区域
- **THEN** Graph Canvas MUST扩展使用可用空间
- **AND** 当前Graph asset MUST不变脏

#### Scenario: domain reload恢复窗口

- **WHEN** Editor domain reload后恢复Graph窗口
- **THEN** Shell MAY恢复editor-only布局状态
- **AND** document与runtime target仍 MUST按各自稳定identity重新绑定，不得恢复旧对象实例

### Requirement: Shell必须保持重操作的显式触发边界

Shell Toolbar MAY暴露domain提供的Compile或Build命令，但selection、Inspector focus、Graph mutation、窗口创建、窗口恢复、Preview target切换、AssetDatabase import或refresh MUST不自动触发Program、Projection、Foot Analysis、Motion Matching Database或AI Program构建。Shell MAY刷新轻量validator与Stale状态，但 MUST不自行修复Stale产物。

#### Scenario: 修改Pose Graph连线

- **WHEN** 作者连接一个Pose edge
- **THEN** mutation adapter MUST更新真实Pose Graph owner并允许轻量validation刷新
- **AND** Projection Build MUST保持未触发并显示Stale

#### Scenario: 显式点击Compile

- **WHEN** 作者点击当前domain正式提供的Compile或Build命令
- **THEN** Shell MUST只调用该domain唯一正式命令入口
- **AND** MUST不复制compiler、发布事务或AssetDatabase保存逻辑

### Requirement: 旧固定两栏布局必须原子迁移

Tree、AI与Pose Graph正式窗口 MUST迁移到同一Workspace region合同。系统 MUST删除旧固定`left-panel Inspector + right-panel Graph`装配、旧Data/Inspector互斥页签和重复selection projection；不得保留旧UXML入口、布局兼容开关、Pose Graph专用Shell或临时reparent桥接。

#### Scenario: 迁移后直接打开旧BTSMTL资产

- **WHEN** 作者双击任意现有BaseTreeAsset
- **THEN** 正式入口 MUST只打开新Workspace Shell
- **AND** MUST不同时创建旧TreeWindow布局或第二Inspector

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

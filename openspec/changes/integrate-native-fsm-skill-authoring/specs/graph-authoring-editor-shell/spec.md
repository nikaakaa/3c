## MODIFIED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

本要求对BTSMTL技能 MUST由FlowCanvas原生GraphEditor的canvas、panel和command表面及显式domain adapter履行，不再指定旧GraphView作为技能画布。Skill domain adapter只能向原生表面提供业务数据与Mutation，不得用自定义面板替换原生Panels。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

唯一`GraphAuthoringEditorShell` MUST提供Toolbar、Navigator、Graph Canvas、Details与可折叠Bottom Dock五个通用区域。Shell MUST继续通过显式domain adapter取得各区域内容，不得按BTSMTL Node、AI Node、Pose Node、AnimationChannel、Blackboard或Runtime Trace类型构造领域UI。Graph Canvas MUST继续承载唯一GraphView、selection、breadcrumb、搜索、clipboard和Undo链路；其它区域 MUST不保存第二份node、edge或selection集合。

#### Scenario: 打开正式BTSMTL Skill Graph

- **WHEN** 作者通过正式入口打开Skill Graph
- **THEN** Shell MUST在Navigator装配当前Skill Graph Data Catalog、在Graph Canvas装配唯一原生Skill Graph editor，并在原生Blackboard/Inspector/菜单扩展点装配BTSMTL provider-aware capability presenter
- **AND** MUST不创建Pose Graph Navigator、Pose Preview或动画字段

#### Scenario: Skill Graph使用原生Blackboard和Inspector
- **WHEN** 作者在Skill Graph中创建Skill Local变量、拖出Get/Set或引用外部provider
- **THEN** Shell MUST保持FlowCanvas原生Blackboard、节点/连线Inspector、变量拖拽和创建菜单可用
- **AND** 所有provider引用与写操作 MUST进入Skill domain Mutation和真实owner Undo

#### Scenario: Skill Graph禁止旁路右侧面板
- **WHEN** Skill domain adapter绑定到GraphEditor
- **THEN** Shell MUST不挂接自定义UI Toolkit Skill右栏或调用`SetDomainPanel`替换原生Panels
- **AND** 原生Blackboard、Inspector、selection、clipboard和GraphEditor菜单 MUST继续由同一宿主提供

#### Scenario: 打开BTSMTL Gameplay Graph

- **WHEN** 作者通过正式入口打开非Skill的BTSMTL Gameplay Graph
- **THEN** Shell MUST在Navigator装配当前Graph Data Catalog、在Graph Canvas装配唯一BTSMTL domain adapter、在Details装配BTSMTL capability presenter
- **AND** MUST不同时创建旧RootTree入口或第二Inspector

#### Scenario: 打开BTSMTL RootTree

- **WHEN** 未迁移领域通过正式入口打开Legacy RootTree
- **THEN** Shell MUST在Navigator装配当前Graph Data Catalog、在Graph Canvas装配唯一BTSMTL domain adapter、在Details装配BTSMTL capability presenter
- **AND** Character Skill正式入口 MUST不把该Legacy RootTree作为技能或角色主线入口

#### Scenario: 打开Character Pose Graph

- **WHEN** 作者从显式CharacterAnimationPresentationProfile上下文打开Pose Graph
- **THEN** 同一Shell MUST装配Pose domain Navigator、同一`GraphAuthoringCanvasView`、Details与Bottom Dock
- **AND** MUST不创建BaseGraph、BaseNode或第二套GraphView

### Requirement: Workspace布局状态必须是editor-only且不污染authoring

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

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

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

Shell Toolbar MAY暴露domain提供的Compile或Build命令，但selection、Inspector focus、Graph mutation、窗口创建、窗口恢复、Preview target切换、AssetDatabase import或refresh MUST不自动触发Program、Projection、Foot Analysis或Motion Matching Database构建。Shell MAY刷新轻量validator与Stale状态，但 MUST不自行修复Stale产物。Behavior Designer内容由插件自己的生命周期管理。

#### Scenario: 修改Pose Graph连线

- **WHEN** 作者连接一个Pose edge
- **THEN** mutation adapter MUST更新真实Pose Graph owner并允许轻量validation刷新
- **AND** Projection Build MUST保持未触发并显示Stale

#### Scenario: 显式点击Compile

- **WHEN** 作者点击当前domain正式提供的Compile或Build命令
- **THEN** Shell MUST只调用该domain唯一正式命令入口
- **AND** MUST不复制compiler、发布事务或AssetDatabase保存逻辑

### Requirement: 旧固定两栏布局必须原子迁移

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

Tree、AI与Pose Graph正式窗口 MUST迁移到同一Workspace region合同。系统 MUST删除旧固定`left-panel Inspector + right-panel Graph`装配、Skill专用固定右侧UI Toolkit面板、旧Data/Inspector互斥页签和重复selection projection；不得保留旧UXML入口、布局兼容开关、Pose Graph专用Shell或临时reparent桥接。Skill Graph不得通过`SetDomainPanel`或等价接口绕过FlowCanvas原生Panels。

#### Scenario: 迁移后直接打开旧BTSMTL资产

- **WHEN** 作者双击任意现有BaseTreeAsset
- **THEN** 正式入口 MUST只打开新Workspace Shell
- **AND** MUST不同时创建旧TreeWindow布局或第二Inspector

### Requirement: Graph Authoring Editor Shell必须只拥有通用编辑交互

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

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

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

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

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

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

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

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

本要求对BTSMTL技能 MUST由原生GraphEditor及技能区域装配履行，不再指定旧GraphView作为技能画布。显式重操作、editor-only状态和唯一数据源约束仍有效；以下旧Shell／GraphView实现要求对未迁移领域保持，不能据此迁移其他领域。

现有BTSMTL Graph作者入口 MUST迁移到Graph Authoring Editor Shell和BTSMTL domain adapter。系统 MUST删除Shell已经接管的旧window/view交互实现、领域外公共静态入口和重复clipboard/Undo/Inspector路径。Pose Graph MUST通过同一Shell基础设施获得独立asset入口；不得新增Workbench或复制一套GraphView框架。

#### Scenario: 直接打开BaseTreeAsset

- **WHEN** 用户双击BaseTreeAsset
- **THEN** 正式入口 MUST打开基于Shell的BTSMTL document
- **AND** MUST不同时打开旧BaseTreeWindow实现或Workbench

#### Scenario: 直接打开Pose Graph asset

- **WHEN** 用户双击Pose Graph asset
- **THEN** 正式入口 MUST打开基于Shell的Pose domain document
- **AND** BTSMTL breadcrumb和runtime context MUST不被写入Pose asset

## ADDED Requirements

### Requirement: Skill Graph Blackboard必须提供provider-aware作者入口

Skill Graph Shell MUST显示当前Skill Local Blackboard以及可引用的Character State、Ability Attribute、GameplayTag、Input/TargetData和Frame Fact provider。作者创建Skill变量时 MUST创建当前Graph的正式声明；作者引用外部provider时 MUST保存owner和稳定声明ID，不得复制值或按名称猜测。

#### Scenario: 从Blackboard拖出Get节点
- **WHEN** 作者把一个变量拖到Skill Graph空白画布
- **THEN** 编辑器 MUST创建带明确owner、声明ID和类型的Get节点
- **AND** 该操作 MUST进入当前domain的Mutation和Undo

#### Scenario: 创建Set节点
- **WHEN** 作者对允许写入的Skill Local或Ability provider变量选择Create Set
- **THEN** 编辑器 MUST创建typed Set节点
- **AND** 对只读Character State、Input、Target或Frame provider MUST拒绝写入

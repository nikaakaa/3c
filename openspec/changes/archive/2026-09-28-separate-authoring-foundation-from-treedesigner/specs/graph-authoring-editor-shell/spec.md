## ADDED Requirements

### Requirement: 原生图编辑器是唯一画布入口

技能、技能 FSM 和 Pose Graph MUST 通过各自正式原生编辑器保存和编辑节点、端口、连接与布局。共享作者集成只提供领域装配、Details、Navigator、Selection、Undo、Clipboard 与只读诊断合同，MUST 不创建第二套 GraphView、节点集合或可写文档。

#### Scenario: 打开技能图

- **WHEN** 作者从技能定义打开 FlowCanvas 图
- **THEN** 编辑器 MUST 直接绑定该图的真实 owner
- **AND** MUST 不创建 BaseGraph 或旧 TreeDesigner 画布副本

#### Scenario: 打开 Pose 图

- **WHEN** 作者从 Pose 资源打开原生 Pose 图
- **THEN** 编辑器 MUST 绑定 Pose 真实数据与领域适配
- **AND** MUST 不装配技能图数据、旧 GraphView 或第二状态机模型

## MODIFIED Requirements

### Requirement: Graph Authoring Editor Shell必须提供可组合工作区区域

共享编辑集成 MUST 保留当前正式入口使用的 Toolbar、Navigator、原生 Graph Canvas、Details 与 Bottom Dock 业务能力，通过领域 adapter 装配内容，不按领域节点或 Runtime Trace 类型硬编码业务字段。画布与节点/端口交互 MUST 由所属领域的正式原生编辑器提供，不要求使用 GraphAuthoringCanvasView、旧 GraphView 或统一旧窗口类。其它区域 MUST 不保存第二份 node、edge 或可写 selection 模型。Behavior Designer 继续使用插件自己的入口；本次 MUST 不重新设计区域布局或另建总窗口。

#### Scenario: 打开BTSMTL Gameplay Graph

- **WHEN** 作者通过正式入口打开仍受支持的 Gameplay 图内容
- **THEN** 领域入口 MUST 装配其正式画布、有效 Data Catalog 和 Details，不为复用区域合同创建旧 TreeDesigner 画布
- **AND** 如果合法内容尚无正式编辑去向，旧入口删除切片 MUST 报告缺口，不创建兼容窗口或静默隐藏内容

#### Scenario: 打开Character Pose Graph

- **WHEN** 作者从明确的动画表现上下文打开 Pose Graph
- **THEN** 入口 MUST 使用 NodeCanvas/FlowCanvas 原生画布并保留 Pose Navigator、Details 与当前有效 Bottom Dock 能力
- **AND** MUST 不实例化 GraphAuthoringCanvasView、BaseGraph 或第二套 GraphView

### Requirement: Workspace布局状态必须是editor-only且不污染authoring

Navigator、Details和Bottom Dock的宽度、展开、折叠、选中页签、搜索、分组与Preview面板布局 MUST只保存为window-local或Editor view-state。任何布局变化 MUST不修改Graph、Timeline、Profile、Definition、Rig、技能执行内容或领域资源 revision。窗口尺寸不足时区域 MAY按确定规则折叠，但 MUST不切换到旧Data/Inspector互斥写路径。

#### Scenario: 折叠Bottom Dock

- **WHEN** 作者折叠Preview与Diagnostics区域
- **THEN** Graph Canvas MUST扩展使用可用空间
- **AND** 当前Graph asset MUST不变脏

#### Scenario: domain reload恢复窗口

- **WHEN** Editor domain reload后恢复Graph窗口
- **THEN** Shell MAY恢复editor-only布局状态
- **AND** document与runtime target仍 MUST按各自稳定identity重新绑定，不得恢复旧对象实例

### Requirement: Shell必须保持重操作的显式触发边界

Shell Toolbar MAY暴露domain提供的Compile或Build命令，但selection、Inspector focus、Graph mutation、窗口创建、窗口恢复、Preview target切换、AssetDatabase import或refresh MUST不自动触发Graph artifact、领域 binding、Foot Analysis或Motion Matching Database构建。Shell MAY刷新轻量validator与Stale状态，但 MUST不自行修复Stale产物。Behavior Designer内容由插件自己的生命周期管理；Skill 编译、Pose 资源准备和 Timeline 播放也 MUST 由各自正式入口显式触发。

#### Scenario: 修改Pose Graph连线

- **WHEN** 作者连接一个Pose edge
- **THEN** mutation adapter MUST更新真实Pose Graph owner并允许轻量validation刷新
- **AND** 受影响的Graph artifact或领域 binding MUST保持未触发并显示Stale

#### Scenario: 显式点击Compile

- **WHEN** 作者点击当前domain正式提供的Compile或Build命令
- **THEN** Shell MUST只调用该domain唯一正式命令入口
- **AND** MUST不复制compiler、发布事务或AssetDatabase保存逻辑

### Requirement: Graph Authoring Editor Shell必须只拥有通用编辑交互

系统 MUST提供唯一共享编辑集成，只拥有窗口生命周期、当前领域原生 Graph 画布、selection、搜索、创建菜单、clipboard、复制粘贴、Undo/Redo、breadcrumb、Inspector宿主、dirty owner协调和只读diagnostics overlay。Shell MUST通过显式domain adapter取得document、node catalog、port policy、mutation、Inspector和diagnostics，不得拥有 BTSMTL、Skill、Pose 或 Timeline 的业务字段与运行规则。

#### Scenario: 打开BTSMTL Graph

- **WHEN** 当前正式原生技能图或技能 FSM 通过所属领域入口打开
- **THEN** Shell MUST装配BTSMTL domain adapters并显示原有作者交互
- **AND** Shell MUST 从领域能力读取创建和连接规则，不按旧节点基类硬编码

#### Scenario: 打开Pose Graph

- **WHEN** CharacterPresentationPoseGraphAsset通过正式入口打开
- **THEN** 同一Shell MUST装配Pose Graph domain adapters
- **AND** MUST不创建BaseGraph、BaseNode或PropertyEdge副本

### Requirement: 每个Graph领域必须拥有独立数据与端口合同

Gameplay Graph MUST使用其当前正式原生图与端口合同表达 authoring。Pose Graph MUST使用独立Pose Graph data、Pose Node、typed Pose Port与Pose Edge表达Presentation pose composition。Shell MUST不要求两个领域继承同一 runtime node 或共享序列化 edge；跨领域拖线、复制节点或粘贴payload MUST被拒绝。

#### Scenario: 从BTSMTL复制节点到Pose Graph

- **WHEN** clipboard payload的domain identity为BTSMTL且当前document为Pose Graph
- **THEN** Shell MUST拒绝粘贴并报告domain不匹配
- **AND** MUST不尝试把BaseNode字段映射为Pose Node

#### Scenario: Pose端口连接

- **WHEN** 作者连接两个Pose domain ports
- **THEN** Shell MUST调用Pose port policy和mutation adapter
- **AND** MUST不调用Gameplay Graph 端口规则

### Requirement: Graph Shell mutation必须落到真实Domain Owner

所有create/delete/connect/disconnect/paste/rename/subgraph reference mutation MUST通过当前domain adapter作用于真实serialized owner，并进入同一Undo组。Shell、画布元素和 diagnostics model MUST不保存第二份node/edge集合。私有和 shared document 切换 MUST保持各自真实dirty owner。

#### Scenario: 删除Pose节点

- **WHEN** 作者在Pose Graph画布删除一个节点及其edge
- **THEN** Pose mutation adapter MUST原子修改Pose Graph asset或inline owner
- **AND** 原生画布 MUST 从修改后的真实 owner 更新显示，不维护旧 GraphView 数据副本

#### Scenario: Undo shared subgraph切换

- **WHEN** 作者把inline subgraph抽取为shared asset后执行Undo
- **THEN** Undo MUST恢复真实owner的互斥reference状态
- **AND** Shell MUST不从缓存节点集合伪造恢复

### Requirement: Graph Shell diagnostics必须只读且来自领域正式结果

Shell MUST只通过领域 diagnostics adapter显示编译、validation或runtime snapshot的只读source mapping。Shell MUST不自行运行Gameplay、Pose、Timeline 运行器或状态选择来重建diagnostics。没有合法runtime target或artifact时 MUST显示明确Unavailable/Stale，而不是使用作者默认值。

#### Scenario: Pose Runtime Live Debug

- **WHEN** Pose owner 提供与作者图版本、所选角色实例和 ResetGeneration 匹配的已完成观察结果
- **THEN** overlay MAY按PoseNodeId显示availability与contribution
- **AND** 显示数据 MUST 来自 Pose 正式已完成结果和 source mapping，不读取尚未接受的中间状态

#### Scenario: Projection已Stale

- **WHEN** 作者图版本、正式资源绑定或实例代次与观察结果不一致
- **THEN** overlay MUST 显示 Stale 并停止绑定旧来源映射
- **AND** MUST 等待领域正式绑定与新完成结果，不生成 Pose IR、隐藏编译程序或独立预览求值器

## REMOVED Requirements

### Requirement: 旧固定两栏布局必须原子迁移

**Reason**: 固定两栏和旧 Data/Inspector 页签是 TreeDesigner UI 的实现约束，当前原生图编辑器与共享业务区域已承担需要保留的作者功能。

**Migration**: 保留布局状态隔离、Data/Details 分工和显式重操作边界，迁移仍在使用的面板到共享编辑集成后删除旧 UXML、旧窗口装配和兼容开关。

### Requirement: 旧BTSMTL窗口路径必须迁移而不是并存

**Reason**: 该条款仍把旧 BaseTreeAsset、BaseTreeWindow 和 GraphView 作为入口，和当前 FlowCanvas/NodeCanvas 及 Pose 原生入口冲突。

**Migration**: 由新增的原生图入口和共享编辑集成承接打开、导航、剪贴板、Undo、Details 和诊断；完成合法内容迁移后删除旧窗口路径，不保留双开或桥接。

## MODIFIED Requirements

### Requirement: 不新增 Graph 分裂路径

系统 MUST保持一套图数据、一套BTSMTL原生端口系统和一套编辑器资产入口。系统 MUST NOT因`BaseGraph`、`StateMachineGraph`、`ConditionRuleGraph`、`AIControllerTree`或BT edge decorator新增Workbench图、并行端口协议、旧数据fallback或重复序列化集合。

领域专用Tree Window MAY通过继承`BaseTreeWindow`提供独立Unity dockable窗口身份、标题和Inspector context，但 MUST复用同一个BaseTreeView、Graph Data Catalog、page stack、breadcrumb、selection、Undo、dirty、Node Search和authoring mutation服务。领域窗口 MUST NOT拥有第二套GraphView或Graph序列化模型。

#### Scenario: 结构链路唯一

- **WHEN** 新Graph能力接入BTSMTL
- **THEN** 它 MUST使用现有BaseGraph集合、PropertyPort/PropertyEdge和BaseTreeAsset入口
- **AND** 它 MUST NOT新增并行Workbench或fallback数据链路

#### Scenario: AI与Character窗口并排

- **WHEN** 作者同时打开Character RootTree和AIControllerTree
- **THEN** 两者 MAY显示为两个独立dockable EditorWindow
- **AND** 两个窗口 MUST复用同一BaseTreeWindow编辑器核心
- **AND** AI窗口 MUST NOT复制GraphView、Undo、Node Search或Data Catalog实现

#### Scenario: 规则图链路唯一

- **WHEN** StateMachine Transition或BT edge decorator需要条件求值图
- **THEN** 系统 MUST使用ConditionRuleGraph
- **AND** 系统 MUST NOT同时保留TransitionRuleGraph、旧BoolPort条件字段或IfNode作为第二套运行条件

## ADDED Requirements

### Requirement: Graph节点兼容性必须由稳定Authoring Capability裁决

每个可进入受限Graph的节点类型 MUST声明稳定authoring capability。Graph Role MUST通过唯一policy定义允许的capability；`CanCreateNodeType`、Node Search、拖拽、粘贴、脚本创建与Compiler Validator MUST复用该policy。系统 MUST为后续自动authoring暴露同一只读policy查询，但本change MUST NOT修改Agent schema。系统 MUST NOT按NodePath字符串、显示名、继承层次或窗口类型猜测节点兼容性。

#### Scenario: AI Graph创建Character动作节点

- **WHEN** 搜索、粘贴、脚本或Compiler尝试在AIControllerTree创建CharacterExecution节点
- **THEN** 统一Graph policy MUST拒绝该节点
- **AND** Graph数据 MUST不发生修改

#### Scenario: AI Graph创建共享纯值节点

- **WHEN** 作者在AIControllerTree创建声明为SharedPureValue的Compare节点
- **THEN** 统一Graph policy MUST允许该节点
- **AND** Editor与Compiler MUST读取同一capability identity

#### Scenario: AI节点缺少能力声明

- **WHEN** 未声明authoring capability的新节点尝试进入AIControllerTree
- **THEN** 创建与发布 MUST失败并报告节点类型和Graph Role
- **AND** 系统 MUST不按默认Base节点处理

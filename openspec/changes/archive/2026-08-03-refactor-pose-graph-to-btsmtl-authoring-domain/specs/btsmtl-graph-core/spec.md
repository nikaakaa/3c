## MODIFIED Requirements

### Requirement: 不新增 Graph 分裂路径

系统 MUST在BTSMTL Gameplay领域保持一套`BaseGraph`数据、一套`PropertyPort`/`PropertyEdge`端口系统和一套`BaseTreeAsset`入口。StateMachineGraph、ConditionRuleGraph和BT edge decorator MUST继续使用该正式BTSMTL链路。Character Presentation Pose Graph MUST保持独立Pose数据、typed Pose端口、validator、compiler和runtime program。两者 MUST复用唯一Graph Authoring Domain Framework的canvas、node、port、Details、Navigator、StateMachine表面、capability与mutation合同，但 MUST不共享runtime node、serialized edge或执行路径。

#### Scenario: BTSMTL新增规则图能力

- **WHEN** StateMachine Transition或BT edge decorator需要条件求值图
- **THEN** 它 MUST继续使用ConditionRuleGraph、PropertyPort和BaseTree authoring入口
- **AND** MUST不使用Pose Graph payload代替BTSMTL数据

#### Scenario: 打开Presentation Pose Graph

- **WHEN** 作者通过共享Editor Shell打开Pose Graph asset
- **THEN** 共享作者表面 MUST装配Pose domain document、capability与port policy
- **AND** MUST不创建BaseGraph、Blackboard或Gameplay runtime context

#### Scenario: 复用节点编辑交互

- **WHEN** BTSMTL Graph和Pose Graph都需要节点、端口、搜索、clipboard、Undo和Details
- **THEN** 两者 MUST复用同一Graph Authoring Domain Framework实现
- **AND** 每个领域 MUST只修改自己的正式serialized owner

#### Scenario: 抽象BTSMTL现有GraphView

- **WHEN** 系统把BTSMTL节点编辑交互提升为跨领域共享实现
- **THEN** 共享代码 MUST从现有`BaseTreeView`、`BaseNodeView`、`BasePortView`、`PropertyPortView`、Edge View与Inspector实现原地提取
- **AND** BTSMTL MUST继续保留黑板变量拖拽、Flow/Property Port、节点搜索创建、框选、复制粘贴、Undo和子树下钻的原有业务行为
- **AND** MUST不以新的简化GraphView替换这些行为

### Requirement: Graph节点兼容性必须由稳定Authoring Capability裁决

每个可进入Graph的节点类型 MUST在唯一Authoring Capability Catalog声明稳定domain、graph role、node kind、port、field和command能力。`CanCreateNodeType`、Node Search、Canvas投影、拖拽、粘贴、人工Mutation、Document Reconciler与Compiler Validator MUST复用该目录。系统 MUST不按NodePath字符串、显示名、继承层次、窗口类型或C#字段路径猜测兼容性。

#### Scenario: AI Graph创建Character动作节点

- **WHEN** 搜索、粘贴、Document或Compiler尝试在AIControllerTree创建CharacterExecution节点
- **THEN** 统一capability policy MUST拒绝该节点
- **AND** Graph数据 MUST不发生修改

#### Scenario: Pose Graph创建Sequence Player

- **WHEN** 作者或Document在允许该能力的Pose Graph role创建Sequence Player
- **THEN** UI、Mutation与Validator MUST读取同一capability identity
- **AND** MUST只生成Sequence Player的typed payload与正式端口

#### Scenario: 节点缺少能力声明

- **WHEN** 未声明authoring capability的新节点尝试进入任一Graph
- **THEN** 创建、Document apply与发布 MUST失败并报告domain、Graph Role与node kind
- **AND** 系统 MUST不按默认节点处理

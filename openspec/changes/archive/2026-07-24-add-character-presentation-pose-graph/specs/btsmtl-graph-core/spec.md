## MODIFIED Requirements

### Requirement: 不新增 Graph 分裂路径

系统 MUST在BTSMTL领域保持一套`BaseGraph`数据、一套`PropertyPort`/`PropertyEdge`端口系统和一套`BaseTreeAsset`入口。StateMachineGraph、ConditionRuleGraph和BT edge decorator MUST继续使用该正式BTSMTL链路，不得新增Workbench、并行BTSMTL端口协议、旧数据fallback或重复序列化集合。跨领域的Character Presentation Pose Graph MUST使用独立Pose数据、typed Pose端口、validator和compiler，并且只能复用通用`GraphAuthoringEditorShell`交互外壳；它 MUST不继承BTSMTL runtime node/edge语义，也 MUST不成为第二个BTSMTL Graph执行路径。

#### Scenario: BTSMTL新增规则图能力

- **WHEN** StateMachine Transition或BT edge decorator需要条件求值图
- **THEN** 它 MUST继续使用ConditionRuleGraph、PropertyPort和BaseTree authoring入口
- **AND** MUST不使用Pose Graph或通用Shell payload代替BTSMTL数据

#### Scenario: 打开Presentation Pose Graph

- **WHEN** 作者通过共享Editor Shell打开Pose Graph asset
- **THEN** Shell MUST装配Pose domain document与port policy
- **AND** MUST不创建BaseGraph、BaseNode、BaseEdge、Blackboard或runtime evaluation context

#### Scenario: 复用节点编辑交互

- **WHEN** BTSMTL Graph和Pose Graph都需要搜索、clipboard、Undo和Inspector宿主
- **THEN** 两者 MUST复用同一Graph Authoring Editor Shell实现
- **AND** 每个领域 MUST只修改自己的正式serialized owner

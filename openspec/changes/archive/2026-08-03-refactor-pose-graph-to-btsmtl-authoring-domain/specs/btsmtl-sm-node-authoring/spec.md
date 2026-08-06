## MODIFIED Requirements

### Requirement: 状态机创作 UI 遵守 inline-first 心智

Graph Authoring Domain Framework MUST为BTSMTL Gameplay StateMachine与PoseStateMachine提供统一Entry、State、Alias、Transition、下钻、breadcrumb、selection和edge Details表面。`StateMachineNode`、`StateNode`和Transition的BTSMTL适配 MUST继续与inline-first数据模型一致；默认创建必须可立即下钻，shared状态行为只能作为显式复用配置。共享表面 MUST不把PoseStateMachine的state payload、blend或sync字段写入BTSMTL Graph。

#### Scenario: StateMachineNode 默认 UI

- **WHEN** 用户选中`StateMachineNode`
- **THEN** Details MUST显示状态机引用ownership并提供Open
- **AND** 节点画布 MUST不强制显示Shared Graph配置齿轮

#### Scenario: StateNode 默认 UI

- **WHEN** 用户选中`StateNode`
- **THEN** Details MUST显示状态行为ownership并允许下钻到resolved行为图
- **AND** shared状态行为asset MUST只作为显式复用配置

#### Scenario: Transition Rule UI

- **WHEN** 用户选中BTSMTL StateMachine Transition edge
- **THEN** Details MUST显示priority、ownership、shared rule asset和rule graph操作
- **AND** MUST不显示Pose blend、sync或inertialization

### Requirement: BTSMTL StateMachine Editor 必须排除动画表现 authoring

共享StateMachine表面的BTSMTL domain adapter、Details和context menu MUST只编辑StateMachine逻辑结构、priority、ConditionRuleGraph、interruption和ownership。它们 MUST不显示或写入animation strategy、duration、curve、external animation exit、Pose source、sync、inertialization或Animation Layer。PoseStateMachine MUST由Presentation domain adapter提供独立payload、mutation、validator与compiler。

#### Scenario: 选择Gameplay Transition edge

- **WHEN** 作者选择BTSMTL StateMachine Transition edge
- **THEN** Details MUST显示From、To、priority、rule ownership和condition摘要
- **AND** MUST不显示动画表现字段

#### Scenario: 切换到PoseStateMachine页面

- **WHEN** 同一共享表面装配Presentation domain adapter
- **THEN** 它 MAY显示Pose transition的blend、sync和readiness
- **AND** MUST不修改BTSMTL StateMachine资产

#### Scenario: 提取共享StateMachine表面

- **WHEN** 实施者从BTSMTL现有StateMachine节点、Transition edge、Inspector与页面栈提取共享交互
- **THEN** BTSMTL inline-first、默认下钻、Condition Rule、priority、interruption和ownership操作 MUST保持
- **AND** MUST不新建一套缺少这些行为的StateMachine GraphView替换原实现

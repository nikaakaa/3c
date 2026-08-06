## MODIFIED Requirements

### Requirement: StateMachine作者表面必须复用且语义隔离

框架 MUST提供唯一共享Entry、State、Alias、Transition edge、画布平移缩放、节点拖动、selection、增选框选、下钻、breadcrumb与edge Details表面。共享框选与selection实现 MUST面向通用GraphView能力，不得要求`BaseTreeView`具体类型，也不得为Pose领域创建第二个Manipulator。BTSMTL Gameplay StateMachine与PoseStateMachine MUST分别提供状态payload、transition payload、rule surface、layout owner、validator与compiler adapter；Gameplay condition MUST不进入Pose transition，Pose blend与sync MUST不进入Gameplay transition。Entry、State与Alias位置变化 MUST通过共享`MoveElement`请求进入当前领域Mutation；只读状态 MUST禁止Mutation，但不得替换成另一套View。

#### Scenario: 打开Gameplay StateMachine

- **WHEN** 当前document role为BTSMTL StateMachine
- **THEN** 共享表面 MUST显示Condition Rule、priority与interruption，并保留现有节点拖动和增选框选行为
- **AND** MUST不显示blend duration、sync或inertialization

#### Scenario: 打开PoseStateMachine

- **WHEN** 当前document role为Pose StateMachine
- **THEN** 共享表面 MUST显示Pose State、Transition Rule、blend、sync与source readiness，并允许拖动Entry、State、Alias及增选框选
- **AND** MUST不创建BaseGraph、ConditionRuleGraph、Pose专用GraphView或第二框选器

#### Scenario: 在Pose StateMachine框选多个状态

- **WHEN** 作者从空白画布拖出选择矩形并覆盖多个Selectable State
- **THEN** 唯一共享框选器 MUST把这些State加入当前GraphView selection
- **AND** MUST不因当前画布不是`BaseTreeView`而忽略操作

#### Scenario: Live Debug期间拖动状态

- **WHEN** Pose StateMachine处于Live Debug只读模式且作者尝试拖动State
- **THEN** 共享表面 MUST拒绝位置Mutation并保持正式layout不变
- **AND** MUST不通过window-local缓存记录一个不可提交的位置


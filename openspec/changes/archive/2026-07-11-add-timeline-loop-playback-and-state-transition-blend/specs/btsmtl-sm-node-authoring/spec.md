## ADDED Requirements

### Requirement: Transition 动画混合元数据必须属于 Transition 边

状态切换的动画混合时长、曲线或 profile MUST 保存为 `StateMachineGraph` Transition edge 的调度 / 表现元数据。Condition rule graph MUST 继续只表达纯 Bool 条件，MUST NOT 保存动画混合字段，也 MUST NOT 创建 `TransitionNode`、Timeline 行为节点或旧动画策略引用来表达切换混合。

#### Scenario: 配置 Transition blend

- **WHEN** 作者选中 `WalkLoop -> RunLoop` Transition edge
- **THEN** Inspector MUST 能显示该 edge 的动画混合配置
- **AND** 混合配置 MUST 随 Transition edge 内联序列化
- **AND** 规则图 MUST 继续只保存条件节点和属性连线

#### Scenario: Condition rule 求值

- **WHEN** runtime 求值 Condition rule graph
- **THEN** rule graph MUST 只返回该 Transition 是否可通过
- **AND** rule graph MUST NOT 决定动画混合时长、曲线或 outgoing pose

### Requirement: StateMachine runtime 必须发布切换混合事实且不双 tick 状态

`StateMachineGraphRuntime` 命中 Transition edge 并切换 active state 时，MUST 能读取该 edge 的动画混合元数据并发布正式切换混合事实。runtime MUST 保持单 active state 行为语义；切换后旧状态行为图 MUST stop 或 exit，MUST NOT 因动画混合继续 tick。

#### Scenario: 命中带动画混合的 Transition

- **WHEN** 当前 active state 命中一条带动画混合元数据的 Transition
- **THEN** runtime MUST 切换到目标 active state
- **AND** runtime MUST 发布源状态、目标状态和动画混合元数据
- **AND** 旧状态行为图 MUST NOT 在后续 tick 中继续运行

#### Scenario: Transition 指向 Exit

- **WHEN** 命中的 Transition 指向 `Exit`
- **THEN** runtime MAY 发布离开状态的动画混合事实
- **AND** 本层状态机的 `Success` 语义 MUST 保持由 Exit transition 决定
- **AND** 系统 MUST NOT 创建额外状态节点承载该混合

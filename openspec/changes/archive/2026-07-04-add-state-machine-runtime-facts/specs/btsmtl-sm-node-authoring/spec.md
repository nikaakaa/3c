## ADDED Requirements

### Requirement: StateMachine runtime 必须暴露状态运行事实

系统 MUST 在 `StateMachineGraphRuntime` 的运行工作副本中维护当前 active state 的运行事实。运行事实至少 MUST 包含 active state identity、进入状态后的 elapsed ticks、elapsed seconds、状态 root 上次返回状态和状态 root 是否完成。运行事实 MUST 属于 runtime working copy，MUST NOT 写回 authoring graph data。

#### Scenario: RunStart 状态运行中

- **WHEN** `RunStart` 是当前 active `StateNode`
- **THEN** runtime MUST 能报告 `RunStart` 的 elapsed seconds 和 elapsed ticks
- **AND** runtime MUST 能报告该状态行为 root 最近一次返回 `Running`、`Success` 或 `Failure`
- **AND** authoring graph asset MUST NOT 因这些 runtime 值变脏

#### Scenario: 状态切换

- **WHEN** 状态机从 `WalkStart` 切换到 `WalkLoop`
- **THEN** runtime MUST 重置 active state elapsed 计数
- **AND** runtime MUST 将 active state identity 更新为 `WalkLoop`
- **AND** 旧状态的运行事实 MUST NOT 被新状态 transition rule 当作当前状态事实读取

### Requirement: TransitionRuleGraph 必须能读取当前状态运行事实

系统 MUST 提供正式 value node 或等价只读接口，让 `TransitionRuleGraph` 能读取当前 `StateMachineGraphRuntime` 的状态运行事实。该能力 MUST 保持 TransitionRuleGraph 的纯条件求值语义，MUST NOT tick 状态行为 SubTree、Timeline 或 Action 节点。

#### Scenario: Start 状态完成后切换 Loop

- **WHEN** 作者配置 `RunStart -> RunLoop`
- **THEN** TransitionRuleGraph MUST 能读取 `StateRootCompleted`
- **AND** TransitionRuleGraph MUST 能组合 `MoveMagnitude >= RunThreshold`
- **AND** runtime MUST 只在两者都成立时允许 transition

#### Scenario: End 状态完成后回 Idle

- **WHEN** 作者配置 `RunEnd -> Idle`
- **THEN** TransitionRuleGraph MUST 能读取 `StateRootCompleted`
- **AND** transition rule MUST NOT 通过 Timeline asset membership 或节点路径判断完成

### Requirement: StateBehaviorSubTree root 完成不自动退出状态

系统 MUST 区分状态行为 root 完成和状态离开。`StateBehaviorSubTree` 的 Root 返回 `Success` MAY 被记录为 `StateRootCompleted`，但 `StateNode` MUST 继续保持 active state，直到同层 Transition 明确切换到其它状态或 Exit。

#### Scenario: RunStart Timeline 播放完成

- **WHEN** `RunStart` 状态行为中的 TimelineNode 返回 `Success`
- **THEN** runtime MUST 将当前状态 root 标记为 completed
- **AND** `RunStart` MUST NOT 因 root completed 自动离开
- **AND** 只有 `RunStart -> RunLoop` transition 条件成立时才切换状态

#### Scenario: Idle 状态没有离开条件

- **WHEN** `Idle` 状态 root 行为返回 `Success`
- **THEN** `Idle` MUST 保持 active
- **AND** 状态机 MUST 等待 transition rule 决定是否离开

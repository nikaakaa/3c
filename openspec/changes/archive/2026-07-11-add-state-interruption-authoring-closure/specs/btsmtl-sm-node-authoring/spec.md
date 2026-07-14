## MODIFIED Requirements

### Requirement: StateBehaviorSubTree 提供状态生命周期入口

系统 MUST 让普通 `SubTree` 只表达 `RootNode` 行为入口。`StateBehaviorSubTree` MUST 固定拥有 `OnEnter`、`RootNode` 和 `OnExit` 生命周期入口。`OnEnter` 和 `OnExit` MUST 使用普通 `RunnableNode` flow 链路，MUST NOT 成为 `StateMachineGraph` Transition 端点。State Transition 或父 Tree graceful stop 离开 active State 时 MUST 先停止 Root，再在当前 StateExitContext scope 内运行 OnExit。

#### Scenario: 普通 SubTree

- **WHEN** 用户创建普通 `SubTree`
- **THEN** 新图 MUST 默认包含一个 `RootNode`
- **AND** 新图 MUST NOT 默认包含 `OnEnter` 或 `OnExit`

#### Scenario: StateBehaviorSubTree

- **WHEN** 用户创建 `StateBehaviorSubTree`
- **THEN** 新图 MUST 默认包含一个 `OnEnter`、一个 `RootNode` 和一个 `OnExit`
- **AND** 缺失或重复生命周期入口 MUST 被校验报告为非法结构

#### Scenario: State Transition 离开状态

- **WHEN** active StateNode 通过同层 Transition 离开
- **THEN** runtime MUST 先停止 State Root
- **AND** MUST 在 target State 进入前运行 OnExit

#### Scenario: Parent Tree abort StateMachineNode

- **WHEN** StateMachineNode 收到 Self、LowerPriority 或 Parent graceful stop
- **THEN** runtime MUST 先停止 active State Root
- **AND** MUST 在 SMNode StopCompleted 前运行 OnExit
- **AND** OnExit MUST 能读取对应 StateExitContext

### Requirement: 状态机运行时解释

系统 MUST 让 `StateMachineNode` 驱动自己 resolved `StateMachineGraph` 数据，并让 `StateMachineGraphRuntime` 以 `StateNode` 作为 active state。解释器 MUST 从 `Enter` 读取初始 Transition，并在每帧写入当前运行工作副本的 `BaseGraph.DeltaTime`。运行时 MUST 从 inline 或 shared authoring graph data 创建隔离工作副本。状态 root 完成 MUST 是可查询事实而不是所有 Transition 的隐式前置条件。State Transition 和父 Tree graceful stop MUST 复用统一 source-exit 内核。

#### Scenario: 父级 tick 状态机入口

- **WHEN** 父级行为图 tick 到 `StateMachineNode`
- **THEN** `StateMachineNode` MUST 进入 resolved `StateMachineGraph` 运行工作副本
- **AND** 父级行为图 MUST 只看到该节点的 `Running/Success/Failure`

#### Scenario: active state tick

- **WHEN** 状态机已有 active state
- **THEN** 本帧 MUST 只 tick active StateNode 状态行为工作副本
- **AND** 其它状态 MUST NOT 被 tick，除非 Transition 切换 active state

#### Scenario: AnyState 和 Exit

- **WHEN** 状态机已有 active state
- **THEN** runtime MUST 在 tick active state 前检查 `AnyState` Transition
- **AND** 命中 `Exit` MUST 让本层状态机返回 `Success`

#### Scenario: 未完成状态被 Transition 抢占

- **WHEN** active State root 仍为 Running
- **AND** 某条出边 ConditionRuleGraph 返回 true
- **THEN** runtime MUST 按 priority 和 flow order 选择 Transition
- **AND** MUST NOT 隐式等待 StateRootCompleted

#### Scenario: 父 Tree graceful stop

- **WHEN** StateMachineNode 收到 graceful stop request
- **THEN** runtime MUST 请求没有 target 的 active State exit
- **AND** OnExit Running 时 SMNode stop status MUST 保持 Running
- **AND** OnExit 完成后 MUST 发布 owner release 并 StopCompleted

#### Scenario: ForceStop

- **WHEN** StateMachineNode 因 Shutdown、Dispose 或强制 Reset 被 ForceStop
- **THEN** runtime MUST 立即停止 active State 和释放 owner
- **AND** MUST NOT 运行 gameplay OnExit 或进入 target State


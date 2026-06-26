# taco-runnable-timeline-node Specification

## MODIFIED Requirements

### Requirement: 状态行为 SubTree 集成
系统 MUST 允许 `TimelineNode` 作为状态具体行为的一部分被 tick。状态具体行为 MUST 位于 `StateNode` 引用的 `SubTree` 中。`TimelineNode` MUST NOT 被解释为 `StateMachineGraph` 的同层 State，也 MUST NOT 参与 Transition 端点。

#### Scenario: StateNode 下钻 SubTree 播放 Timeline
- **WHEN** LocomotionGraph 当前 active state 是 Idle `StateNode`
- **AND** Idle `StateNode` 引用 IdleSubTree
- **THEN** 普通 IdleSubTree MUST 能从 `RootNode` 链路 tick `TimelineNode`
- **AND** Idle 引用 `StateBehaviorSubTree` 时 MUST 能从 `OnEnter`、`RootNode` 或 `OnExit` 链路 tick `TimelineNode`
- **AND** `TimelineNode` MUST 能播放 IdleTimeline

#### Scenario: TimelineNode 不参与同层状态转换
- **WHEN** TimelineNode 位于状态行为 `SubTree` 内
- **THEN** 它 MUST NOT 被解释为状态机 Graph 的同层 State
- **AND** 状态机同层 Transition MUST 只连接 `StateNode`、状态机控制节点和 `Exit`

## ADDED Requirements

### Requirement: TimelineNode 不替代 StateNode
系统 MUST 保持 `TimelineNode` 为普通可执行行为节点。系统 MUST NOT 因为 Timeline 表达动画或动作片段而把 `TimelineNode` 升格为状态节点。

#### Scenario: 状态播放 Timeline
- **WHEN** 用户需要让 Idle 状态播放 Timeline
- **THEN** 用户 MUST 创建 Idle `StateNode`
- **AND** 用户 MUST 在 Idle 引用的 `SubTree` 中创建 `TimelineNode`
- **AND** 系统 MUST NOT 创建 `TimelineStateNode`

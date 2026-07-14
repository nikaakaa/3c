# btsmtl-sm-node-authoring Specification

## MODIFIED Requirements

### Requirement: 状态机运行时解释

系统 MUST让 `StateMachineNode` authoring 继续解析自己的 inline/shared `StateMachineGraph`，但正式 runtime MUST由 Compiler 将其转换为 Program transition/control-flow table，由 SimulationKernel 以 state slots 保存 active、exiting、pending state、condition state、execution path 和 stop barrier。Runtime MUST不从 authoring graph 创建工作副本，不写 `BaseGraph.DeltaTime`，也不使用随机 runtime Guid。状态 root 完成仍 MUST是可查询事实而不是 Transition 隐式前置；State Transition 与父 Tree graceful stop MUST复用统一 compiled source-exit operation。

#### Scenario: 父级执行 StateMachine operation

- **WHEN** Program 执行到 StateMachineNode 对应 operation
- **THEN** Kernel MUST进入该 Program 内的状态机 table
- **AND** 父级控制流 MUST只看到 Running/Success/Failure
- **AND** MUST不创建 StateMachineGraph runtime clone

#### Scenario: active state 被 Transition 抢占

- **WHEN** active state 仍 Running 且 compiled condition 返回 true
- **THEN** Kernel MUST按 priority 与 flow order 选择 edge
- **AND** MUST按统一 exit barrier 更新 state slots

#### Scenario: snapshot 恢复嵌套状态机

- **WHEN** Driver 恢复包含 nested StateMachine 的 snapshot
- **THEN**完整 outer-to-inner execution path、state scope generation 和 pending exit MUST恢复
- **AND** 后续 Tick MUST不依赖旧 runtime object

### Requirement: Timeline 和输入通过正式状态行为链路接入

TimelineNode 和 input authoring MUST继续通过 StateNode body/ConditionRuleGraph 接入，不成为 StateMachineGraph 同层 state。Compiler MUST把 Timeline request 和 stable input id编译为 Program operation/slot；runtime MUST不通过 `Owner.DeltaTime`、InputAction 或 CharacterGraphContext object读取。

#### Scenario: Idle 播放 Timeline

- **WHEN** Idle state body authoring 包含 TimelineNode
- **THEN** Compiler MUST把该节点编入 Idle body control flow
- **AND** Timeline gameplay time MUST来自 SimulationTick


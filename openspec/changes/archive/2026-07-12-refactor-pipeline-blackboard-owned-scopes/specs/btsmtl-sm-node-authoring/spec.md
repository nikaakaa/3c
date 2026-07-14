## ADDED Requirements

### Requirement: StateMachine runtime 必须向作用域服务发布完整状态激活身份

`StateMachineGraphRuntime` 在 State enter、Transition source-exit、parent graceful stop 和 force stop 生命周期中，MUST 使用完整 `StateMachineExecutionScope(RuntimeId, StateId, ActivationGeneration)` 通知依赖状态作用域的 runtime 服务。系统 MUST NOT 只传 `stateId` 表达 State activation ownership。

#### Scenario: 两个状态机并行运行

- **WHEN** Locomotion StateMachine 的 RunLoop 与 Action StateMachine 的 Attack1 同时 active
- **THEN** 两个状态 MUST 拥有不同 RuntimeId 的 execution scope
- **AND** Attack1 退出通知 MUST NOT 被解释为 RunLoop 退出

#### Scenario: 同一状态再次进入

- **WHEN** StateMachine 离开 Attack1 后再次进入 Attack1
- **THEN** 新 execution scope MUST 使用新的 ActivationGeneration
- **AND** 新 activation MUST NOT 复用上一次 activation 的 State scope values

#### Scenario: Transition rule 求值

- **WHEN** runtime 求值当前 active State 的 ConditionRuleGraph
- **THEN** evaluation context MUST 携带当前完整 execution scope
- **AND** rule 中的 State scope variable MUST 解析到该 activation

#### Scenario: ForceStop

- **WHEN** StateMachine runtime 被强制停止
- **THEN** scope service MUST 收到目标 execution scope 的释放通知
- **AND** scope service MUST 只清理该 execution scope 的 runtime data


# btsmtl-sm-node-authoring Specification

## MODIFIED Requirements

### Requirement: StateMachine runtime 必须向作用域服务发布完整状态激活身份

StateMachineGraphRuntime MUST使用完整 StateMachineExecutionScope(RuntimeId, StateId, ActivationGeneration) 维护 State Blackboard、状态运行事实与 nested execution path。该 scope 只属于状态逻辑，不得复制成动画 owner、animation ready、Tree animation activation 或 presentation lineage。

#### Scenario: target 首次执行

- **WHEN** target StateNode 首次获得正式 state body update
- **THEN** State 事实 MUST记录该 State execution scope 已执行
- **AND** 系统 MUST不从该 executed 事实推导动画已采样或动画可切换

#### Scenario: 同一状态再次进入

- **WHEN** StateMachine 离开 Attack1 后再次进入 Attack1
- **THEN** StateMachineExecutionScope MUST使用新的 activation generation
- **AND** 旧 activation 的 State Blackboard 数据 MUST不泄漏到新 activation

#### Scenario: Transition rule 求值

- **WHEN** runtime 求值当前 active State 的 ConditionRuleGraph
- **THEN** evaluation context MUST携带当前完整 execution scope
- **AND** rule 中的 State scope variable MUST解析到该 activation

#### Scenario: ForceStop

- **WHEN** StateMachine runtime 被强制停止
- **THEN** scope service MUST收到目标 execution scope 的释放通知
- **AND** scope service MUST只清理该 execution scope 的逻辑 runtime data

## REMOVED Requirements

### Requirement: StateMachine transition必须复用通用Tree control-flow fact

**Reason**: 该合同是为动画 Driver/ExecutionLineage 引入的中间路径。StateMachine transition 已由 State 事实、stop barrier 和 diagnostics 表达，不应为 Animation 模块发布第二套通用控制流事实。

#### Scenario: 删除动画用途的 control-flow fact

- **WHEN** StateMachine 提交 internal transition
- **THEN** runtime MUST完成 source exit、target activation 和状态事实提交
- **AND** MUST不向 Animation 模块提交 TreeControlFlowCommitted

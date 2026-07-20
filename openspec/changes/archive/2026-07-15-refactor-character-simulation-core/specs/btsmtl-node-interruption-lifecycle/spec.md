# btsmtl-node-interruption-lifecycle Specification

## MODIFIED Requirements

### Requirement: Tree 调度层不得越权产生业务生命周期

通用 Tree 调度层 MUST 只负责 child 选择、Runnable stop 传播、pending stop barrier 和结构执行结果。它 MUST 不产生 animation、camera、cue、GameplayEffect 或 network 业务输出，也 MUST 不因为表现需要改变合法 Runnable result。正式 Character runtime MUST 由 Compiler 将相同 interruption authoring 编译为 control-flow operation；Program operation 在完成 State/Action ownership 决策后 MAY 输出每层唯一 presentation producer command，但该输出不属于通用 Tree scheduler 生命周期，也不得通过 `CharacterGraphContext` 提交。

#### Scenario: Selector 抢占 Attack SMNode

- **WHEN** compiled LowerPriority replacement 停止 Attack StateMachine operation
- **THEN** Program MUST 传播 stop context 并等待 descendant stop barrier
- **AND** State/Action operation MUST 在完成所有权决策后输出新的每层唯一 producer command
- **AND** 通用 Tree scheduler MUST 不生成 animation release、Driver、handoff record 或 presentation command

#### Scenario: 空 StateBehaviorSubTree

- **WHEN** compiled 空 StateBehaviorSubTree 返回合法 State.None
- **THEN** control-flow operation MUST 保留该逻辑结果
- **AND** 系统 MUST 不因为动画表现合同把它转换成 InvalidExecuted 或抛出动画异常

#### Scenario: 非 Character 通用解释执行

- **WHEN** 其它工具显式使用通用 Tree scheduler 执行相同 interruption authoring
- **THEN** scheduler MUST 只产生结构生命周期和结果
- **AND** MUST 不访问 CharacterSimulationState、CharacterPresentationProjection 或 SimulationCommitter

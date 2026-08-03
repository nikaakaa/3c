## MODIFIED Requirements

### Requirement: Tree 调度层不得越权产生业务生命周期

通用 Tree 调度层 MUST只负责child选择、Runnable stop传播、pending stop barrier和结构执行结果。它 MUST不产生animation、camera、cue、GameplayEffect或network业务输出，也 MUST不因为表现需要改变合法Runnable result。正式Character runtime MUST由Compiler将相同interruption authoring编译为control-flow operation；Program operation在完成State/Action ownership决策后 MAY为每个AnimationChannelId输出唯一presentation producer command，但该输出不属于通用Tree scheduler生命周期，也不得通过`CharacterGraphContext`提交。

#### Scenario: Selector抢占Attack SMNode

- **WHEN** compiled LowerPriority replacement停止Attack StateMachine operation
- **THEN** Program MUST传播stop context并等待descendant stop barrier
- **AND** State/Action operation MUST在完成所有权决策后为每个受影响AnimationChannelId输出唯一producer command
- **AND** 通用Tree scheduler MUST不生成animation release、Driver、handoff record或presentation command

#### Scenario: 空StateBehaviorSubTree

- **WHEN** compiled空StateBehaviorSubTree返回合法State.None
- **THEN** control-flow operation MUST保留该逻辑结果
- **AND** 系统 MUST不因为动画表现合同把它转换成InvalidExecuted或抛出动画异常

#### Scenario: 非Character通用解释执行

- **WHEN** 其它工具显式使用通用Tree scheduler执行相同interruption authoring
- **THEN** scheduler MUST只产生结构生命周期和结果
- **AND** MUST不创建AnimationChannel command、PoseSlot binding或表现runtime

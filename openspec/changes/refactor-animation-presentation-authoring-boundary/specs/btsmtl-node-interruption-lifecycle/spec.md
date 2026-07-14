# btsmtl-node-interruption-lifecycle Specification

## MODIFIED Requirements

### Requirement: Tree 调度层不得越权产生业务生命周期

Tree 调度层 MUST只负责 child 选择、Runnable stop 传播、pending stop barrier 和结构执行结果。它 MUST不产生 AnimationLayerSelection，不发布动画 owner、ready、Driver、topology 或 transition 事实，也 MUST不因为动画需要改变合法 Runnable result。角色逻辑节点 MAY通过 CharacterGraphContext 的正式逻辑接口提交已解析动画选择，但该提交不属于 Tree scheduler 生命周期。

#### Scenario: Selector 抢占 Attack SMNode

- **WHEN** Selector 通过 LowerPriority replacement 停止 Attack StateMachineNode
- **THEN** Tree 调度层 MUST只传播 stop context 并等待 descendant stop barrier
- **AND** Action/State 逻辑 MUST在完成所有权决策后提交新的每层 AnimationLayerSelection
- **AND** Tree 调度层 MUST不生成 animation release、Driver 或 handoff record

#### Scenario: 空 StateBehaviorSubTree

- **WHEN** 没有状态行为内容的 StateBehaviorSubTree 返回合法 State.None
- **THEN** Runnable lifecycle MUST保留该逻辑结果
- **AND** 系统 MUST不因为动画表现合同把它转换成 InvalidExecuted 或抛出动画异常

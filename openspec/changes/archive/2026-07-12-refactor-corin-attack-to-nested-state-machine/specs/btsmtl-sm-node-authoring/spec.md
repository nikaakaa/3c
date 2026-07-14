## MODIFIED Requirements

### Requirement: 状态机层级角色分离

系统 MUST 使用 `StateMachineNode` 表达父级行为图进入状态机图的入口，使用 `StateMachineGraph` 表达同层状态结构，使用 `StateNode` 表达状态机图内普通状态。`StateMachineGraph` MUST NOT 直接包含另一个 `StateMachineNode`；需要嵌套状态机时，作者 MUST 在某个 StateNode 的 resolved `SubTree` 或 `StateBehaviorSubTree` 行为图中创建普通 `StateMachineNode`。每个 StateMachineNode 默认拥有的 StateMachineGraph MUST 是该节点内部的普通 C# inline graph data，只有显式复用时才可提升为 shared asset。

#### Scenario: 状态行为创建嵌套状态机

- **WHEN** 用户在 `Attack` StateNode 的 inline StateBehaviorSubTree Root 流程中创建状态机入口
- **THEN** 创建结果 MUST 是普通 `StateMachineNode`
- **AND** 编辑器 MUST 自动创建并绑定 inline `StateMachineGraph`
- **AND** 用户 MUST 能继续下钻编辑 Attack1、Attack2 与 Exit
- **AND** 系统 MUST NOT 创建 `AttackStateMachineNode` 或一次性 StateMachineGraph asset

#### Scenario: 状态机图拒绝直接嵌套节点

- **WHEN** 用户尝试在 StateMachineGraph 同层创建 StateMachineNode
- **THEN** `CanCreateNodeType` 和 validation MUST 拒绝该结构
- **AND** UI MUST 引导作者从某个 StateNode 的行为图继续下钻

## ADDED Requirements

### Requirement: 嵌套 StateMachine runtime 必须维护完整 execution path

系统 MUST 为嵌套 StateMachine runtime 维护从 outer 到 inner 的有序 execution path。每个 activation frame MUST 包含稳定 StateMachine runtime identity、State identity、activation generation 和对应 State body Graph owner/runtime identity。状态 body update、OnExit、ConditionRuleGraph、Timeline request 和 Blackboard access MUST 在相同 path 上执行。系统 MUST NOT 把单个最内层 scope 当作完整嵌套上下文。

#### Scenario: Attack1 在 Attack 状态中运行

- **WHEN** 外层 Action StateMachine 的 `Attack` active，且内层 Attack StateMachine 的 `Attack1` active
- **THEN** execution path MUST 同时包含外层 Attack activation frame 与内层 Attack1 activation frame
- **AND** Timeline request MUST 能识别 Attack1 为 presentation leaf
- **AND** 外层 Attack frame MUST 在 Attack1 -> Attack2 期间保持有效

#### Scenario: 嵌套 scope 栈不匹配

- **WHEN** runtime pop 的 frame 不是当前 execution path 最内层 frame
- **THEN** runtime MUST 报告 scope stack mismatch
- **AND** runtime MUST NOT 静默删除其它 active frame 或退化到 Character scope

### Requirement: 嵌套 StateMachine 必须继承根动画 transition domain

每个顶层并行 StateMachineNode MUST 创建独立 animation transition domain；其 State body 中嵌套的 StateMachineNode MUST 继承该 domain。同一 domain 同时最多一个 active animation transition，不同 domain MAY 并行。父层 transition MUST 能解析当前 active presentation leaf owner，MUST NOT 以不产动画的结构 State owner 替代 leaf owner。

#### Scenario: 内层连段切换

- **WHEN** 内层 Attack StateMachine 从 Attack1 切换到 Attack2
- **THEN** handoff MUST 使用 Attack1 leaf owner 作为 source、Attack2 leaf owner 作为 target
- **AND** transition MUST 属于外层 Action StateMachine 的 animation domain

#### Scenario: 父层离开 Attack

- **WHEN** 外层 Action StateMachine 从 Attack 切换到 DodgeForward
- **THEN** source MUST 解析为当前 Attack presentation leaf owner
- **AND** target MUST 解析为 DodgeForward owner
- **AND** 父子 runtime MUST NOT 并行发布两套有效 Action domain transition

#### Scenario: 并行 Locomotion 与 Action

- **WHEN** Locomotion domain 和 Action domain 同时发生 transition
- **THEN** 两个 domain MUST 分别推进
- **AND** Attack 内层 transition MUST NOT supersede Locomotion transition


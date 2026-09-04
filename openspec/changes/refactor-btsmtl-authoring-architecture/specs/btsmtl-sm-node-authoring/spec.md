## MODIFIED Requirements

### Requirement: 状态机层级角色分离

BTSMTL StateMachine MUST只用于技能实例内部的局部阶段和子流程；角色级移动模式、动作选择与跨技能协调 MUST由C#控制模块承担。局部StateMachineGraph、StateNode、StateBehaviorSubTree和ConditionRuleGraph MUST保持现有结构分工，不提供角色RootTree状态机入口，也不承载Pose状态机语义。

#### Scenario: 技能包含局部阶段

- **WHEN** 作者在蓄力技能内组织准备、保持与释放状态
- **THEN** MUST允许局部StateMachine与状态行为子图
- **AND** 执行状态 MUST属于本次ActionInstance

#### Scenario: 角色级状态入口

- **WHEN** 作者尝试建立Character RootTree控制状态机
- **THEN** 作者能力与构建 MUST拒绝并定位应由代码控制的范围

#### Scenario: 状态行为创建嵌套状态机

- **WHEN** 用户在技能内部的 `Attack` StateNode 的 inline StateBehaviorSubTree Root 流程中创建状态机入口
- **THEN** 创建结果 MUST 是普通 `StateMachineNode`
- **AND** 编辑器 MUST 自动创建并绑定 inline `StateMachineGraph`
- **AND** 用户 MUST 能继续下钻编辑 Attack1、Attack2 与 Exit
- **AND** 系统 MUST NOT 创建 `AttackStateMachineNode` 或一次性 StateMachineGraph asset

#### Scenario: 状态机图拒绝直接嵌套节点

- **WHEN** 用户尝试在 StateMachineGraph 同层创建 StateMachineNode
- **THEN** `CanCreateNodeType` 和 validation MUST 拒绝该结构
- **AND** UI MUST 引导作者从某个 StateNode 的行为图继续下钻


### Requirement: StateMachine 运行时必须由 Compiled Operation 执行

StateMachineNode、StateMachineGraph、StateNode、TransitionEdge 和 ConditionRuleGraph MUST编译为 CharacterSimulationProgram operation/table。Active、pending、exiting、transition、nested path 和 stop barrier MUST存入 CharacterSimulationState slot，MUST不由 StateMachineGraph runtime clone 持有。

#### Scenario: 进入嵌套状态机

- **WHEN** compiled State body 进入内层 StateMachineNode
- **THEN** Kernel MUST以稳定 execution path 访问内层 state slot
- **AND** MUST不创建 runtime Graph clone
#### Scenario: 同技能两个局部状态机实例

- **WHEN** 两个释放引用同一技能局部StateMachine
- **THEN** active／exiting／execution path MUST按ActionInstance和调用点隔离
- **AND** MUST不驱动角色级控制状态

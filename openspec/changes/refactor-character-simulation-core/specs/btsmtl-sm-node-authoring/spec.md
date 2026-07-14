# btsmtl-sm-node-authoring Specification

## ADDED Requirements

### Requirement: StateMachine 运行时必须由 Compiled Operation 执行

StateMachineNode、StateMachineGraph、StateNode、TransitionEdge 和 ConditionRuleGraph MUST编译为 CharacterSimulationProgram operation/table。Active、pending、exiting、transition、nested path 和 stop barrier MUST存入 SimulationState slot，MUST不由 StateMachineGraph runtime clone 持有。

#### Scenario: 进入嵌套状态机

- **WHEN** compiled State body 进入内层 StateMachineNode
- **THEN** Kernel MUST以稳定 execution path 访问内层 state slot
- **AND** MUST不创建 runtime Graph clone

## MODIFIED Requirements

### Requirement: StateMachine runtime 必须暴露状态运行事实

Compiled StateMachine operation MUST在正式 decision、enter、update、exit barrier、complete 和 interruption 边界输出结构化 state lifecycle facts。Fact MUST包含 Program/source identity、ActorId、activation identity、execution path 和 SimulationTick，MUST不依赖 runtime clone reference。

#### Scenario: Transition 成立

- **WHEN** compiled ConditionRuleGraph 选中 Transition
- **THEN** Kernel MUST输出可反查 authoring edge 的 decision/exit/enter facts

### Requirement: StateMachine上层停止必须使用普通Runnable release链

上层 Tree interruption、graceful stop 与 ForceStop MUST通过统一 compiled Runnable lifecycle 传播到 StateMachine、State body 与 nested StateMachine。StateMachine operation MUST不维护第二套停止或表现等待生命周期。

#### Scenario: 上层 LowerPriority 打断

- **WHEN** 上层 compiled Tree 要求释放正在运行的 StateMachine
- **THEN** release MUST按 outer-to-inner path 到达 active State body
- **AND** 逻辑退出 MUST不等待动画 fade

### Requirement: 嵌套 StateMachine runtime 必须维护完整 execution path

Program MUST为嵌套 StateMachine 编译稳定 outer-to-inner execution path。SimulationState MUST按 Actor/Graph activation/path 隔离 State slot 与 Blackboard State frame，不得按 runtime object identity 寻址。

#### Scenario: 内外层同名 State

- **WHEN** 两个层级包含同名 State
- **THEN** Kernel MUST以 compiled path/handle 定位不同 State slot

## REMOVED Requirements

### Requirement: 状态机运行时解释

**Reason**：运行时 clone StateMachineGraph 并调用 authoring node 会保留 Unity object 依赖和隐藏状态，无法成为 portable/snapshotable 核心。

**Migration**：保留 StateMachine authoring 与 Editor，将运行语义迁入 Program operation/table 和 SimulationState slot。

#### Scenario: 迁移 StateMachine Runtime

- **WHEN** Character Host 创建 Corin
- **THEN** MUST加载 compiled StateMachine operation
- **AND** MUST不创建 StateMachineGraph runtime clone

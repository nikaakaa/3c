# tengine-hotupdate-foundation Specification

## MODIFIED Requirements

### Requirement: BTSMTL authoring 和 runtime 主线必须保持统一

项目 MUST保持 BTSMTL 作为 Graph、StateMachine、ConditionRuleGraph 与 Timeline 的唯一 authoring source，并以该 source编译出的 CharacterSimulationProgram作为正式 Character runtime主线。TEngine FSM/GameEvent MUST不绕过 Program operation直接驱动角色状态、Timeline、WorldSolver或Action lifecycle。

#### Scenario: 状态机 Transition 求值

- **WHEN** Corin runtime判断 Transition
- **THEN** MUST执行由 BTSMTL ConditionRuleGraph编译的 operation
- **AND** MUST不使用 TEngine FSM或旧 StateMachineGraphRuntime替代

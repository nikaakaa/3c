## MODIFIED Requirements

### Requirement: Program 必须声明完整 Character State Layout

Program MUST为Runnable、StateMachine、Timeline、Blackboard、Input request、Action、GameplayEffect、RNG、counter和sequence中会影响当前Commit后或未来SimulationTick的数据分配明确、类型化的Character State Layout。每个StateSlot MUST声明稳定index、owner、semantic、typed value kind与default；Layout MUST不允许opaque Bytes kind。只服务同一Step的MotionContribution、MotionAccumulator、PendingWorldRequest、输出staging和State Transaction MUST由Target Evaluation/Pending产品拥有，不得进入committed Character State Layout。任何影响未来SimulationTick的Actor Gameplay数据 MUST不留在authoring object、operation、emitter或领域runtime隐藏字段内。Body/world/solver state MUST由独立WorldSimulationState layout拥有。

#### Scenario: 检查有状态 Operation

- **WHEN** Wait、StateMachine、Timeline、Input request、Action或GameplayEffect operation影响后续Tick
- **THEN** 其可变数据 MUST存入已声明typed Character state address
- **AND** operation object MUST保持不可变

#### Scenario: 检查同Step Motion transient

- **WHEN** Motion operation只为当前WorldSolve产生contribution与WorldRequest
- **THEN** Program State Layout MUST不声明MotionAccumulator或PendingWorldRequest committed slot
- **AND** Snapshot与StateHash MUST不包含该transient

#### Scenario: 编译未知复杂状态

- **WHEN** Target lowering需要保存没有正式typed value kind与canonical codec的领域状态
- **THEN** Target build MUST失败并指出source operation/state declaration
- **AND** MUST不回退为Bytes StateSlot


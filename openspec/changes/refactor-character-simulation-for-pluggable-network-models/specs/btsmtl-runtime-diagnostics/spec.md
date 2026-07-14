# btsmtl-runtime-diagnostics Specification

## MODIFIED Requirements

### Requirement: Runtime diagnostics 必须与执行对象布局解耦

运行时调试 MUST只依赖稳定 source identity、Program revision、operation handle、runtime instance identity、Debug Source Map 和结构化 Trace。Editor MUST不持有、反射或轮询 SimulationState、CharacterSimulationProgram mutable view、authoring clone 或 World Solver object。Compiled SimulationKernel MUST发布与现有 Graph/StateMachine/Timeline/Blackboard/Animation/Motion/GameplayEffect channel兼容的正式 trace。

#### Scenario: Compiled Program 执行 Graph

- **WHEN** SimulationKernel 执行某个 operation
- **THEN** Trace MUST携带 Program revision、SimulationTick、pass kind、ActorId、operation handle 和 runtime instance
- **AND** Editor MUST通过 Source Map 映射回 authoring source

#### Scenario: Replay 同一 operation

- **WHEN** 同一 SimulationTick 的 operation 在 Replay pass 再次执行
- **THEN** Trace MUST保留相同 source/operation identity并标记 Replay pass
- **AND** Editor MUST不把它伪装成新的 canonical Tick

### Requirement: Debug Source Map 必须严格映射执行元素到 authoring source

每个 compiled Program revision MUST携带只读 Debug Source Map，将 operation、state slot owner、Timeline segment 和 presentation producer handle 映射到 Graph、Node、Edge、Timeline、Track、Clip 或 declaration identity，并携带 ProgramId、CompilationRevision 与 SourceContentHash。Source revision mismatch MUST停止 overlay，MUST不按名称、index 或 path fallback。

#### Scenario: 一个 Node 生成多个 operation

- **WHEN** Compiler 为一个 StateMachineNode 生成 enter/tick/exit 等多个 handles
- **THEN** Source Map MUST允许它们映射回同一 authoring node
- **AND** Trace MUST仍能区分 operation kind

### Requirement: Trace 必须使用结构化事件和稳定时序

每条 simulation Trace MUST携带 session、Program revision、domain、SimulationTick 或 RenderFrame、pass kind、单调 trace sequence、ActorId、runtime instance、source operation、event kind 和结构化 payload。Forward、Prediction、Authoritative 与 Replay MUST明确区分；Diagnostics sequence MUST不进入 gameplay state hash。

#### Scenario: Rollback restore/replay

- **WHEN** Driver 恢复 Tick 119 并重演 120 至 126
- **THEN** Trace MUST记录 restore cause、snapshot tick、replay range、state hash 和每 Tick pass kind
- **AND** Presentation events MUST只对应 Committer 实际提交的 ledger

### Requirement: Runtime producer 必须在正式生命周期边界发布 Trace

Compiler/Program loader、Simulation Driver、SimulationKernel、Tree/StateMachine/Timeline/Blackboard operations、World Solver、Presentation Committer、Animation lifecycle 与 Animancer adapter MUST在各自正式边界发布对应 channel 事件。Diagnostics MUST观察正式 state/fact/command，不得建立第二套执行、Timeline sampling、rollback decision、solver 或 animation selection。

#### Scenario: World Solver 完成 motion

- **WHEN** solver 返回当前 Actor 的 motion result
- **THEN** Motion channel MUST显示 solver id、capability、requested/applied result和collision summary
- **AND** Diagnostics MUST不重新计算 motion

#### Scenario: Committer 去重 replay command

- **WHEN** replay 产生已存在 EventId
- **THEN** Presentation/Animation trace MUST显示 deduplicated/replaced 结果
- **AND** MUST不为调试再次提交 command

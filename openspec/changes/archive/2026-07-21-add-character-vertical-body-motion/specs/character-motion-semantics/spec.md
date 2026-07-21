## MODIFIED Requirements

### Requirement: Motion 必须经过 Contribution、Request、Solver Result 和 Body Sample

Compiled Locomotion与Timeline MotionCurve operation MUST只提交当前Numeric Target的`SimulationMotionContribution`。同一Actor/Tick的全部contribution MUST由唯一Target Motion accumulator按固定channel解析为`ResolvedMotionChannel`；每个channel MUST经过Operation Set规定的唯一Motion Modifier阶段，再由固定channel合成为`ResolvedGameplayMotion`。唯一Target Body Motion Integrator MUST读取committed WorldBodyState、compiled Body Motion descriptor与TickDelta，在Solver前产生唯一`CharacterMotionRequest`和同Step plan。正式WorldSolve Pass MUST把Session全部Actor request组成唯一batch；Solver返回实际结果后 MUST由同一Target Integrator Finalize垂直动力状态，随后Finalize Pass MUST更新World/Character state并产生committed body observation。Graph、Timeline、Action、Modifier、Presentation与concrete Solver MUST不直接实现重力、写Transform或调用其它Solver。

#### Scenario: Locomotion、Dodge、MotionWarp和重力同Tick输出

- **WHEN** Locomotion与Dodge Timeline同Tick提交motion contribution
- **AND** Dodge source成为Action channel resolved owner且其MotionWarp eligible
- **THEN** 唯一Motion accumulator MUST先按channel、priority、weight与blend规则解析玩法Motion
- **AND** MotionWarp MUST只修正resolved Action channel
- **AND** Body Motion Integrator MUST在全部Modifier之后加入重力
- **AND** WorldSolver MUST只消费最终唯一request

#### Scenario: Program没有玩法Motion

- **WHEN** 当前Tick没有任何有效MotionContribution或Motion Modifier delta
- **THEN** ResolvedGameplayMotion MAY为零
- **AND** Body Motion Integrator MUST仍根据committed VerticalVelocity、Gravity和Grounded语义产生最终request


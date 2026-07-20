## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST在Evaluate阶段产生当前Numeric Target的contribution；唯一Motion accumulator MUST按Channel、Priority、Weight、BlendMode与ConsumeLowerChannels将Locomotion和Timeline contribution解析为`ResolvedGameplayMotion`。当前Target唯一Body Motion Integrator MUST在全部Program Motion Modifier之后，根据committed WorldBodyState与compiled descriptor生成每Actor唯一`CharacterMotionRequest`和同Step plan。正式Execution Backend的WorldSolve Pass MUST汇总当前Step全部Actor request并调用一次`ICharacterWorldSolver.ResolveBatch`；Solver提供真实applied displacement、Grounded与Collision后 MUST通过Target唯一Body Motion Finalizer提交VerticalVelocity，Finalize Pass MUST再产生唯一`CharacterBodySample`与Motion GameplayFact。Graph、Timeline、Action、Source、Presentation与concrete Solver MUST不拥有第二份Motion仲裁、重力积分或逻辑Transform真值。

#### Scenario: Timeline MotionCurve提交动作Y位移

- **WHEN** compiled Timeline在当前Tick产生Action motion contribution且包含Y delta
- **THEN** Timeline module MUST只提交带稳定source、channel、priority、weight、space与blend mode的contribution
- **AND** 唯一Target Motion accumulator MUST先解析玩法Motion
- **AND** Body Motion Integrator MUST再加入环境gravity delta
- **AND** 最终request MUST与同Tick其它Actor request一起进入唯一ResolveBatch
- **AND** Finalize MUST记录Solver actual result与committed VerticalVelocity

#### Scenario: Timeline与Locomotion同Tick提交

- **WHEN** Timeline Action channel与普通Locomotion channel在同一Tick都有contribution
- **THEN** MUST由同一个Motion accumulator按正式channel消费和混合规则形成ResolvedGameplayMotion
- **AND** Timeline、StateMachine或Action module MUST不各自生成竞争的WorldRequest
- **AND** 任一channel MUST不消费Body Motion Integrator产生的重力


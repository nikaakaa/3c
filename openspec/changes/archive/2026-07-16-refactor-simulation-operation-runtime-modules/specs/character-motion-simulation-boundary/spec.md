## MODIFIED Requirements

### Requirement: 运动语义、世界约束执行和逻辑位姿必须分层

Compiled motion operations MUST在 Evaluate 阶段产生 portable contribution；当前 Numeric Target 的唯一 Motion accumulator MUST按 Channel、Priority、Weight、BlendMode 与 ConsumeLowerChannels 将 Locomotion、Timeline 和业务 modifier contribution 解析为每 Actor 唯一 `CharacterMotionRequest`。SimulationSessionRuntime MUST汇总当前 Session 全部 Actor request 并调用一次 ICharacterWorldSolver.ResolveBatch；Finalize MUST使用精确匹配的 WorldSolverResult 更新 Character/World state 并产生唯一 MotionResult。Graph、Timeline、Action、Driver 和 Presentation MUST不直接解析最终 MotionRequest、写 Transform 或调用 concrete solver。

#### Scenario: Timeline MotionCurve 提交位移

- **WHEN** compiled Timeline 在当前 Tick 产生 Action motion contribution
- **THEN** Timeline module MUST只提交带稳定 source、channel、priority、weight、space 与 blend mode 的 contribution
- **AND** 唯一 Target Motion accumulator MUST与同 Tick Locomotion contribution 一起解析出一个 CharacterMotionRequest
- **AND** request MUST与同 Tick其它 Actor request 一起进入唯一 ResolveBatch
- **AND** Finalize MUST记录 Solver actual result

#### Scenario: Timeline 与 Locomotion 同 Tick 提交

- **WHEN** Timeline Action channel 与普通 Locomotion channel 在同一 Tick 都有 contribution
- **THEN** MUST由同一个 Motion accumulator 按正式 channel 消费和混合规则处理
- **AND** Timeline、StateMachine 或 Action module MUST不各自生成竞争的 WorldRequest

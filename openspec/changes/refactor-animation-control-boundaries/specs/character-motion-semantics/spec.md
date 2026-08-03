# character-motion-semantics Specification

## ADDED Requirements

### Requirement: Pose State时间不得成为Gameplay Motion真相

PoseStateMachine active state、TimeInState、Sequence time、transition progress与Slot weight MUST只服务动画表现。CharacterMotionRequest MUST只来自Gameplay Program、Action Timeline Motion、Input/Locomotion Motion policy或其它正式Simulation source。Presentation Runtime MUST不把Pose root、Sequence displacement、PoseState name或transition progress直接写入CharacterSimulationState、World Body或Transform。

#### Scenario: Start动画比Gameplay加速更长

- **WHEN** Start Sequence仍在播放但Motor已经达到目标速度
- **THEN** Gameplay Body MUST继续由Motor结果决定
- **AND** PoseStateMachine MAY按Fact提前切到Locomotion

#### Scenario: Action Timeline提供Motion

- **WHEN** Dodge Timeline在Gameplay Tick产生Motion contribution
- **THEN** CharacterMotionRequest MUST按现有Simulation与WorldSolver链执行
- **AND** Action Slot MUST只消费匹配playback生成Pose


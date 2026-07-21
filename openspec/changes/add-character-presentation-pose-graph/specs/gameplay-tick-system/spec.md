## MODIFIED Requirements

### Requirement: GameplayTickSystem 必须每表现帧推进 PresentationFrame

PresentationFrame MUST继续以render/presentation delta推进visual interpolation、Timeline visual sampling、每PoseSlot Blend Stack clock、Animancer source sampling、Character Pose Graph、Foot Placement、Camera与committed command lifecycle。Rollback replay MUST只产生EventId output replacement，MUST不直接回卷PresentationFrame或用logic tick代替presentation delta。PresentationFrame MUST不调用Kernel Evaluate/Finalize、WorldSolver.ResolveBatch或修改Character/World state。

#### Scenario: 高渲染帧率下的表现帧

- **WHEN** 两个SimulationTick之间发生多个PresentationFrame
- **THEN** Body插值、slot淡入淡出、source sampling与Pose Graph输出 MUST连续推进
- **AND** Session runtime handle MUST不被额外推进

#### Scenario: Replay后替换动画选择

- **WHEN** Output Disposition Pass产生FullBodyAction EventId replacement
- **THEN** PresentationFrame MUST从该slot当前视觉结果处理新command
- **AND** MUST继续以presentation delta推进唯一Stack与Pose Graph

## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime 是 Unity 动画应用边界

SimulationCommitter与 Character Presentation adapter MUST共同构成 Unity animation application boundary。Committer MUST只提交 Egress OutputDisposition标记为 Publish、Replace或 Retire的 producer command；Presentation adapter MUST通过 Projection、AnimationPlaybackLifecycle和 Animancer应用。Program Runtime、Execution Backend、Pipeline Pass、WorldSolver与 Session Source MUST不引用 Animancer或直接播放动画。

#### Scenario: Commit Attack producer

- **WHEN** LocalImmediateOutputPass将 Attack presentation command标记为 Publish
- **THEN** Committer MUST将其送入唯一 animation command lifecycle
- **AND** Pipeline Runtime MUST不直接调用 Animancer


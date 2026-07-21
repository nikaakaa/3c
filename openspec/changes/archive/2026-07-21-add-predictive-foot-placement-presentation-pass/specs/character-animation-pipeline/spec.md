# character-animation-pipeline Specification

## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime 是 Unity 动画应用边界

SimulationCommitter 与唯一 `CharacterSimulationPresentationRuntime` 协调器 MUST共同构成 Unity animation application boundary。Standard Float32 与 ServerAuthoritative Egress MUST把最终 producer selection、sample、complete 或 release command 以 Publish disposition 提交，Float32 SimulationCommitter MUST拒绝 Presentation command 的 Replace 或 Retire disposition。Deterministic Rollback adapter MAY在 rollback 原子提交完成后，依据有界 EventId state journal 对已经应用的表现状态调用 `ICharacterPresentationRuntime.Replace` 或 `Retire`；该对账 MUST不建立第二套 Timeline、crossfade 或 Gameplay state。协调器 MUST通过 Projection 校验 producer，并将 playback command 唯一转发给 `CharacterAnimationPlaybackRuntime -> AnimationPlaybackLifecycle -> Animancer`。Animancer完成本帧最终pose后，协调器 MAY把该pose和只读visible playback contribution交给唯一注册的Presentation Pose Post Process Pass；该Pass MUST不选择producer、播放动画、修改Animancer state/layer/fade或生成Gameplay output。Program Runtime、Execution Backend、Pipeline Pass、WorldSolver、Session Source 与 Network adapter MUST不引用 Animancer、Final IK、Pose Post Process实现或直接播放/修改动画姿势。

#### Scenario: Commit Attack producer

- **WHEN** LocalImmediateOutputPass将 Attack presentation command标记为 Publish
- **THEN** Committer MUST将其送入唯一 animation command lifecycle
- **AND** Pipeline Runtime MUST不直接调用 Animancer

#### Scenario: 纠偏改变当前可见 producer

- **WHEN** ServerAuthoritative Egress确认预测 producer不再是当前最终选择
- **THEN** Egress MUST生成新的 release与最终 selection command并以 Publish提交
- **AND** MUST不向 Presentation Port提交历史 command的 Replace或 Retire

#### Scenario: Fixed Rollback 对账已应用的表现事件

- **WHEN** Fixed rollback 原子提交后 EventId state journal 判定既有表现事件被替换或退出有效历史
- **THEN** rollback presentation adapter MAY调用唯一 `ICharacterPresentationRuntime` 的 Replace 或 Retire
- **AND** Runtime MUST只修正表现生命周期，不修改 Character/World state 或重新执行 Gameplay operation

#### Scenario: Animancer完成最终pose

- **WHEN** AnimationPlaybackLifecycle已经提交本帧sample并调用Animancer Evaluate
- **THEN** 唯一Pose Post Process Pass MAY消费最终骨骼姿势和只读visible playback contribution
- **AND** MUST不建立另一份animation selection或crossfade权威

# character-animation-presentation-authoring Specification

## MODIFIED Requirements

### Requirement: Animation producer 必须拥有稳定 presentation identity

每个 Timeline animation producer MUST 拥有稳定 authoring producer identity。Compiler MUST 将该 identity 同时写入 CharacterSimulationProgram source map 与 CharacterPresentationProjection binding；Runtime playback identity MUST 由 Program producer identity、ActorId、activation identity 和 playback generation 组合，不得使用显示名、数组 index、asset path、breadcrumb 或当前 Tree object identity 作为 fallback。

#### Scenario: Timeline Track 重排

- **WHEN** 作者重排 AnimationTrack 或 Clip
- **THEN** 原 producer identity MUST 保持
- **AND** Program 与 Projection binding MUST 不因列表 index 变化而 orphan

#### Scenario: 复制 inline Timeline producer

- **WHEN** 作者复制一个 inline TimelineNode 或 animation producer
- **THEN** 新 producer MUST 获得新 identity
- **AND** 系统 MUST 不让两个 producer 共用同一 Program source 或 playback state key

#### Scenario: binding 指向未知 producer

- **WHEN** Projection binding 无法解析到 Program manifest 中的 producer identity
- **THEN** Compiler/Validator MUST 报告 orphan binding
- **AND** Runtime MUST 拒绝 Program/Projection 组合，不能按名称或 Clip 猜测目标

### Requirement: 播放生命周期调试必须只保留统一视图

RuntimeDebugSession 与 CharacterPipelineHost 调试视图 MUST 作为 committed producer command、Timeline visual sample、PendingFirstSample、Current、Outgoing、Retired、Animancer state key 与 fade progress 的唯一生命周期调试入口。CharacterPipelineDefinition Inspector MUST 不复制该 Trace UI。Editor MUST 不重新运行 Graph、重建 Program command、重采样 Gameplay Timeline 或自行混合。

#### Scenario: 排查攻击切换

- **WHEN** Base committed producer 从 Locomotion 变为 Attack1
- **THEN** Host Live Debug MUST 显示 Program command EventId、Attack1 首样本、Animancer state 与 outgoing Locomotion fade
- **AND** 数据 MUST 来自正式 Trace

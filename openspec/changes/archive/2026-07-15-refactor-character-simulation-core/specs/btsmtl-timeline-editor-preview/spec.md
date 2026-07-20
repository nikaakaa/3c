# btsmtl-timeline-editor-preview Specification

## MODIFIED Requirements

### Requirement: Timeline Preview 必须按正式阶段展示 TreeClip

Timeline Editor MUST 显示 TreeClip 的 Decision/Commit 阶段、inline/shared ownership 和 Blackboard 输出摘要。TreeClip Preview 只有在正式 preview target 提供匹配的 `CharacterSimulationProgram`、`CharacterPresentationProjection`、隔离 Preview Session state、输入和所需 WorldSolver capability 时才 MAY 执行。Preview MUST 通过与正式 Character runtime 相同的 Program operation、SimulationKernel 和四阶段 SessionRuntime 推进，MUST NOT 创建临时 `CharacterGraphContext`、`TimelineRunningTree` clone、写入 authoring 默认值或形成第二套 TreeClip 执行语义。

#### Scenario: Preview target 提供完整编译上下文

- **WHEN** Timeline Preview target 提供匹配 source revision 的 Program、Projection、隔离 state 和 required capabilities
- **THEN** Preview MAY 通过隔离 Preview Simulation Session 执行 TreeClip
- **AND** Preview MUST 使用正式 Evaluate、ResolveBatch、Finalize 与 commit 生命周期
- **AND** Preview state MUST 不影响 live Character Session

#### Scenario: Preview target 缺少正式上下文

- **WHEN** 作者打开含 TreeClip 的 Timeline 但没有绑定完整 preview target
- **THEN** Timeline Editor MUST 继续显示 Clip、阶段、Graph 和声明摘要
- **AND** Preview MUST 不执行 TreeClip
- **AND** 系统 MUST NOT 创建 fallback context 或解释器路径

#### Scenario: 只预览动画资源

- **WHEN** 作者只请求纯表现动画采样且不执行 TreeClip Gameplay
- **THEN** Timeline Editor MAY 使用 CharacterPresentationProjection 采样表现资源
- **AND** MUST 不产生 Motion、Window、Blackboard、Action 或 GameplayEffect 事实

### Requirement: 预览采样必须复用正式动画播放链路

纯动画 Timeline Preview MUST 通过 `CharacterPresentationProjection` 将稳定 producer identity 解析为表现资源，并复用正式 CharacterAnimationPlaybackCommandQueue、AnimationPlaybackLifecycle 与 AnimancerPlaybackAdapter。Preview session MUST 为每层生成零或一个带独立 preview EventId/playback generation 的 producer command 和 sample；它 MUST 不生成 `AnimationLayerSelection`、比较 Priority、直接播放 Clip 或实现第二套 layer mixing。

#### Scenario: 当前时间采样

- **WHEN** preview time 位于 AnimationTrack clip 范围
- **THEN** session MUST 提交该 producer 的唯一 preview command 与 sample
- **AND** AnimationPlaybackLifecycle MUST 完成 PendingFirstSample/Current 提交
- **AND** AnimancerPlaybackAdapter MUST 应用 Projection 中的正式 producer binding

#### Scenario: 同层多个 producer

- **WHEN** 一次 preview evaluation 发现多个 producer 声明同一 LayerId
- **THEN** session MUST 明确拒绝该 evaluation
- **AND** MUST 不按 Priority 或 Track 顺序选择赢家

#### Scenario: 非连续 seek

- **WHEN** preview time 非连续跳转
- **THEN** session MUST retire 旧 preview EventId 并清理对应 playback lifecycle 与 Animancer state
- **AND** 目标时间 MUST 使用新的 preview playback generation 建立 command/sample

#### Scenario: 连续播放

- **WHEN** session 连续播放
- **THEN** 同一 preview playback generation MUST 持续更新 producer sample time
- **AND** session MUST 不在每个表现帧重新创建隐藏 producer

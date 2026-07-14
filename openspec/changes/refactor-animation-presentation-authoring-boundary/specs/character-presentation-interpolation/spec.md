# character-presentation-interpolation Specification

## ADDED Requirements

### Requirement: Timeline pose time 与 Animancer fade time 必须独立连续推进

Animation state 的 pose time MUST来自每个 PresentationFrame 的 Timeline visual sampling；Animancer fade progress MUST使用真实 presentation delta 推进。两者 MUST不共用 logic tick 作为表现时钟。外部采样的 Current state MAY由 Timeline 每帧写入 time，Animancer 仍 MUST独立推进 fade weights。

#### Scenario: 30Hz logic 与 120Hz presentation

- **WHEN** 两个 logic tick 之间执行多个表现帧
- **THEN** 每帧 MUST重新计算 selected playback 的 visual Timeline time
- **AND** 每帧 MUST使用 presentation delta 推进 Animancer fade
- **AND** 动画 MUST不按 logic tick 离散跳动

#### Scenario: manual update

- **WHEN** Animancer 由项目使用 manual update
- **THEN** adapter MUST传入正式 presentation delta
- **AND** MUST不使用 Evaluate(0) 作为 fade 时钟

### Requirement: 动画重入必须从 Animancer 当前视觉图接管

同一 LayerId 在旧 state 尚未淡出时收到新 selected target，AnimancerPlaybackAdapter MUST调用 Animancer 正式 Play/Fade 从当前视觉图接管。项目 MUST不冻结 FinalOutput、回放中间逻辑状态、清空 layer 或建立 handoff stack。

#### Scenario: Dodge 淡出时进入 Run

- **WHEN** Dodge 仍为 Outgoing 且 Run target 首样本 ready
- **THEN** adapter MUST从当前 Animancer layer 状态播放 Run
- **AND** 画面 MUST不先跳回 Dodge 或 Idle 基准姿势

## MODIFIED Requirements

### Requirement: 角色表现插值必须基于 logic sample 历史

系统 MUST为角色表现层保存最近的 logic sample 历史。logic sample MUST来自正式 `CharacterPipeline` logic tick 结果，至少包含 local logic tick、logic pose、grounded 状态和 correction mode。PresentationFrame MUST使用最近 logic pose samples 和 `GameplayPresentationFrameContext.InterpolationAlpha` 生成表现根姿态。动画表现 MUST使用 Timeline scheduler 的前后逻辑时间计算 visual Timeline time，并在表现帧重采样 AnimationTrack。系统 MUST不在 PresentationFrame 重新 tick BTSMTL、推进 Timeline logic time、运行 MotionResolver 或 ActionRuntime。

#### Scenario: 渲染帧高于 logic tick

- **WHEN** 当前 render frame 没有新的 `LocalLogicTick`
- **THEN** `CharacterPipeline` MUST仍然调用 PresentationFrame
- **AND** 表现层 MUST使用最近保存的 logic pose samples 和 interpolation alpha 生成 visual root 输出
- **AND** 动画链路 MUST使用 visual Timeline time 重采样当前有效 AnimationTrack
- **AND** 表现层 MUST不因没有新 logic tick 而重新推进 Timeline

#### Scenario: 首个 logic sample

- **WHEN** 角色刚激活且只有一个 logic sample
- **THEN** 表现层 MUST将 visual pose 对齐到该 sample
- **AND** 动画表现 MUST对齐当前有效 playback/generation 的正式 AnimationProducerSample
- **AND** 系统 MUST不生成隐藏 Idle、隐藏动画 fallback 或额外 motion fact

### Requirement: 动画 visual playback 必须来自表现帧重采样和生命周期注册表

PresentationFrame MUST根据 Timeline logic sample history 与 InterpolationAlpha 计算 visual Timeline time，并为 selected 与 retained-outgoing playback 生成 AnimationProducerSample。AnimationPlaybackLifecycle MUST维护 PendingFirstSample、Current、Outgoing 与 Retired；AnimancerPlaybackAdapter MUST应用 sample 并让 Animancer 负责 fade。该链路 MUST不修改 Timeline logic time、ActionWindow、Motion、root motion 或 SyncFacts。

#### Scenario: 同一 playback 跨 tick

- **WHEN** selected Timeline playback 在 previous/current logic sample 间保持有效
- **THEN** AnimationTrack MUST更新同一 playback generation 的 visual clip time
- **AND** Current Animancer state MUST使用本帧 sample
- **AND** fade MUST使用 presentation delta

#### Scenario: gameplay 与 visual 分离

- **WHEN** HitWindow 已在 logic tick 产生
- **THEN** animation resampling、producer retention 与 Animancer fade MUST不重复产生该 fact

#### Scenario: manual Animancer evaluation

- **WHEN** Presenter 使用 manual evaluation 应用外部采样 time
- **THEN** adapter MUST显式接收真实 presentation delta
- **AND** Timeline sample MUST只控制 pose time
- **AND** Animancer MUST控制 fade progress

#### Scenario: incoming 延迟

- **WHEN** RequireOutput selected target 尚无第一份合法 sample
- **THEN** lifecycle MUST记录 PendingFirstSample 并继续显示 Current
- **AND** 画面 MUST不进入 Empty、bind pose 或默认 Idle

### Requirement: 表现插值必须提供调试可追踪性

系统 SHOULD暴露 previous/current logic tick、interpolation alpha、visual Timeline time、每层 selection、playback generation、PendingFirstSample、Current、Outgoing、Retired、Animancer state key、fade progress、retention 与错误。Graph、StateMachine、Timeline 和 Animation channel MUST区分逻辑执行、Timeline sample 与播放生命周期；Debug MUST不成为 gameplay、selection、Blackboard 或网络输入。

#### Scenario: 排查 Action 与 Locomotion 快速切换

- **WHEN** Action 结束、Locomotion selection 恢复且 MovingTurn 同 tick 生效
- **THEN** Logic Trace MUST显示最终 Base selection
- **AND** Timeline Trace MUST显示 target sample time
- **AND** Animation Trace MUST显示 Current/Outgoing 与 Animancer fade

#### Scenario: duplicate selection

- **WHEN** 同一 logic commit 为 Base 提交两个不同 playback
- **THEN** debug MUST显示两个逻辑来源与冲突
- **AND** MUST不显示伪 Selected Driver 或动画侧 winner

#### Scenario: missing first sample

- **WHEN** selected target 在 release 前始终没有合法 sample
- **THEN** debug MUST显示 playback generation、LayerId 与 lifecycle error
- **AND** MUST不伪造 fallback output

## REMOVED Requirements

### Requirement: Inertialization 必须基于最终输出姿态并保持表现层纯度

**Reason**: 项目自制 Inertialization output job 超出本 change 的播放生命周期职责，并依赖已删除 LayerPlan/HandoffId。

#### Scenario: 删除自制 Inertialization

- **WHEN** 迁移完成
- **THEN** runtime MUST不再创建项目自有 Inertialization session
- **AND** 旧 Inertialization 配置 MUST作为已确认的中间数据删除
- **AND** 正式 layer fade MUST只由 Animancer 原生配置驱动

### Requirement: Animation Transition 重入必须从当前视觉结果接管

**Reason**: 旧 requirement 由 custom LayerRuntime/ActiveHandoff 实现。新重入由 Animancer 当前视觉图直接处理。

#### Scenario: Animancer 重入

- **WHEN** fade 中收到新 target
- **THEN** adapter MUST调用 Animancer 正式 API

### Requirement: Presentation diagnostics 必须暴露完整动画仲裁链

**Reason**: Driver、ExecutionLineage、topology、causal component、Arbitrator 与 LayerPlan 已删除，diagnostics 改为 selection/sample/playback lifecycle。

#### Scenario: 新 diagnostics

- **WHEN** 排查动画切换
- **THEN** Trace MUST显示 logic selection、Timeline sample 与 Animancer fade
- **AND** MUST不显示已删除仲裁链

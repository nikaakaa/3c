# character-presentation-interpolation Specification

## Purpose
定义逻辑姿态到表现根姿态的插值边界，以及 visual Timeline 重采样、动画播放生命周期与 Animancer fade 独立连续推进的职责分离。
## Requirements
### Requirement: 角色表现插值必须基于 logic sample 历史

系统 MUST为角色表现层保存最近的 logic sample 历史。logic sample MUST来自正式 `CharacterPipeline` logic tick 结果，至少包含 local logic tick、logic pose、grounded 状态和 correction application extent。PresentationFrame MUST使用最近 logic pose samples 和 `GameplayPresentationFrameContext.InterpolationAlpha` 生成表现根姿态。动画表现 MUST使用 Timeline scheduler 的前后逻辑时间计算 visual Timeline time，并在表现帧重采样 AnimationTrack。系统 MUST不在 PresentationFrame 重新 tick BTSMTL、推进 Timeline logic time、运行 MotionResolver 或 ActionRuntime。

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

### Requirement: Motion visual pose 必须和逻辑 Transform 分离

系统 MUST 区分 logic root 和 visual root。正式 Logic Pose Port MUST 表达碰撞、判定、网络预测和 motion correction 使用的逻辑真值；具体实现 MAY 包装 Unity `CharacterController`、纯 CSharp body 或外部权威 pose。表现插值 MUST 只应用到显式配置的 visual root / model root。PresentationFrame MUST NOT 调用 Motion Executor，MUST NOT 通过 Logic Pose Port 反写 logic root，MUST NOT 修改 `MotionResult` 或 `MotionCorrectionApplicationResult`。Presentation MUST 从正式 correction application result 获取 application extent，MUST NOT 从 motion debug snapshot 获取运行决策。

#### Scenario: 本地 motion 插值

- **WHEN** previous logic sample 和 current logic sample 都有有效 logic pose
- **THEN** PresentationFrame MUST 使用 interpolation alpha 计算 visual position 和 visual rotation
- **AND** 计算结果 MUST 应用到 visual root
- **AND** logic root MUST 保持 MotionStage 通过正式 executor/pose port 结算出的状态

#### Scenario: 网络校正后表现贴合

- **WHEN** logic tick 收到 motion correction 并产生 MotionCorrectionApplicationResult
- **THEN** correction MUST 仍由 MotionStage 的 correction phase 处理
- **AND** Presentation MAY 对部分应用使用普通 logic sample interpolation，对完整应用维持当前立即贴合行为
- **AND** 表现层 MUST NOT 把 correction 当作新的 motion contribution
- **AND** diagnostics 开关 MUST NOT 改变表现结果

### Requirement: Visual root 必须是正式配置

系统 MUST 让 `CharacterPipelineHost` 或等价 Unity 装配点显式持有 visual root / model root 绑定，并独立持有正式 Logic Pose Adapter 绑定。缺少当前模式所需绑定时，系统 MUST 报告正式配置错误。系统 MUST NOT 自动使用 `CharacterController.transform`、Logic Pose Adapter 所在 transform、Animancer 所在 transform、子节点搜索、同名对象搜索或 prefab 目录扫描作为 fallback。

#### Scenario: Host 配置 visual root

- **WHEN** 角色 Host 创建 `CharacterPipeline`
- **THEN** Host MUST 将正式 visual root 绑定传入表现层
- **AND** 表现层 MUST 只通过该绑定应用 visual pose
- **AND** visual root MUST 不等同于 Logic Pose Port 的隐式默认目标

#### Scenario: 缺少 visual root

- **WHEN** 角色需要表现插值但 Host 没有配置 visual root
- **THEN** 系统 MUST 报告配置错误
- **AND** 系统 MUST NOT 静默把 logic root 当成 visual root 使用

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

### Requirement: 表现插值不得产生同步事实

系统 MUST 保持 PresentationFrame 为表现消费阶段。表现插值 MAY 产生 visual pose、visual animation plan 和 runtime debug snapshot，但 MUST NOT 写入 `StrictGameplayOutput`、`CharacterSyncFacts`、ActionRuntime、Graph blackboard 或 NetworkSendStage 输出。

#### Scenario: 高帧率表现帧

- **WHEN** 120fps 渲染帧在两个 30Hz logic tick 之间多次调用 PresentationFrame
- **THEN** 每次 PresentationFrame MAY 更新 visual root 和 Animancer 显示姿态
- **AND** 系统 MUST NOT 为这些表现帧创建额外 ClientCommand、ActionWindowSample、GameplayCueFact 或 MotionSnapshot

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


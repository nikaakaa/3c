## ADDED Requirements

### Requirement: StateMachine transition 必须提交动画 owner handoff

系统 MUST 让 StateMachine runtime 为每次 state activation 提供稳定 owner scope，并在状态 transition 完成时提交 source owner 到 target owner 的正式动画 handoff event。该 event MUST 与 Transition edge 的 condition evaluation 分离，MUST 携带 transition identity、source owner、target owner、duration 和 curve。Duration 为 0 时仍 MUST 发布 handoff，不能通过当帧 contribution 缺席隐式表达状态退场。

handoff event 到达统一 Registry 后 MUST 保持 pending，直到 target activation 的 state body 至少实际 tick 一次并提交 `OwnerReady`。`OwnerReady` MUST 表示 OnEnter 或 Root producer 已获得正式执行机会，MUST NOT 要求 target 一定存在动画 contribution。Target 缺少动画时，handoff MUST 暴露真实空输出，MUST NOT 隐式保留 source。

#### Scenario: Target state body 尚未执行

- **WHEN** source state 已完成 transition 并停止 tick
- **AND** target activation 的 state body 尚未实际执行
- **THEN** Registry MUST 保持 source owner 的最后合法 contribution
- **AND** handoff MUST 保持 pending
- **AND** 系统 MUST NOT 因 transition tick 与目标首次采样分属不同 logic tick 而生成空计划帧

#### Scenario: Target state body 首次执行

- **WHEN** target activation 的 OnEnter 或 Root 图首次被正式 tick
- **THEN** StateMachine runtime MUST 提交该 target owner 的 `OwnerReady`
- **AND** 同 tick Timeline 请求 MUST 可由 Scheduler 接管
- **AND** PresentationFrame MUST 在统一批次内应用 target Sample 与 pending handoff

#### Scenario: 非零时长状态切换

- **WHEN** StateMachine 从 source state activation 切换到 target state activation
- **AND** Transition edge 的 animation blend duration 大于 0
- **THEN** source owner 当前有效 contributions MUST 进入 outgoing handoff
- **AND** target owner contributions MUST 作为 incoming 进入统一动画层
- **AND** 旧状态行为 MUST NOT 为了动画混合继续 tick

#### Scenario: 零时长状态切换

- **WHEN** StateMachine 从 source state activation 切换到 target state activation
- **AND** Transition edge 的 animation blend duration 等于 0
- **THEN** runtime 仍 MUST 发布 owner handoff
- **AND** 统一动画运行时 MUST 在同一表现处理批次原子 retire source owner 并接受 target owner
- **AND** 中间 MUST NOT 生成由生命周期缺口导致的空计划帧

#### Scenario: 并行 Locomotion 和 Action 状态机

- **WHEN** Locomotion StateMachine 发生 transition
- **AND** Action StateMachine 的 contribution owner 仍有效
- **THEN** Locomotion owner handoff MUST NOT release 或重启 Action owner contributions
- **AND** 两个状态机 MUST 使用不同 runtime activation scopes

## MODIFIED Requirements

### Requirement: BTSMTL 内部 TimelinePlaybackScheduler 是 Timeline 播放权威

系统 MUST 使用 `CharacterBTSMTLPhase` 内部的 `TimelinePlaybackScheduler` 作为角色管线模式下 Timeline 播放时间、逻辑事实采样和表现帧动画采样的唯一权威。`TimelineNode`、Timeline 轨道和 `TimelinePlayer` MUST NOT 在该模式下自主推进同一个 Timeline。Scheduler MUST 为每次请求保存稳定 playback identity 和 owner scope；logic completion、terminal presentation handoff 与 runtime Timeline clone disposal MUST 是明确分离的阶段。

#### Scenario: TimelineNode 提交请求

- **WHEN** `TimelineNode` 被 BTSMTL RootTree tick 到
- **THEN** 节点 MUST 向正式管线上下文提交 Timeline 播放请求
- **AND** 请求 MUST 获得稳定 playback instance identity 和当前正式 owner scope
- **AND** `TimelinePlaybackScheduler` MUST 在本帧或后续帧接管该请求
- **AND** 节点 MUST NOT 直接调用 Timeline 播放 API

#### Scenario: Scheduler 推进 active Timeline

- **WHEN** `TimelinePlaybackScheduler` 拥有 active Timeline record
- **THEN** 它 MUST 在 logic tick 使用 pipeline tick context 的 fixed delta 推进播放时间
- **AND** 它 MUST 记录上一逻辑采样时间、当前逻辑采样时间和 loop cycle，用于后续表现帧动画采样
- **AND** 它 MUST 将完成、失败或取消状态写回请求状态表

#### Scenario: Once Timeline 到达结尾

- **WHEN** Once Timeline 在 logic tick 到达 duration
- **THEN** Scheduler MUST 立即写回 Succeeded 并停止产生后续 gameplay facts
- **AND** Scheduler MUST 保留完成 terminal animation handoff 所需的最小 presentation record
- **AND** runtime Timeline clone MUST NOT 在统一 Registry 接收 terminal lifecycle 之前被直接丢弃
- **AND** handoff 完成后 Scheduler MUST 释放该 record，不继续充当 outgoing mixer

### Requirement: Timeline 轨道采样输出管线数据

系统 MUST 让 Timeline 轨道按对应时钟采样并输出管线数据。AnimationTrack MUST 在表现帧按插值后的 active 或 terminal-pending Timeline 时间输出统一动画贡献 lifecycle submissions；Gameplay 窗口、VFX、SFX、Camera、Motion 和 FootPhase 等非动画轨道 MUST 在 logic tick 输出对应 pipeline 数据。轨道 MUST NOT 直接结算命中、扣血、改写角色 Transform 或绕过管线直接控制最终表现。

#### Scenario: 动画轨道采样

- **WHEN** `PresentationFrame` 采样 active 或 terminal-pending Timeline
- **AND** Timeline 时间落在 AnimationTrack 的有效 clip 范围内
- **THEN** AnimationTrack MUST 输出包含 playback、contribution、owner、来源、层、clip 时间、权重、fade 和 loop context 的 Sample
- **AND** clip 时间 MUST 来自表现帧采样时间而不是 logic tick 烘好的播放计划
- **AND** AnimationTrack MUST NOT 直接调用 Animator、TimelinePlayer 或 PlayableGraph

#### Scenario: 动画 clip 离开范围

- **WHEN** 表现采样确认某个上一帧有效的 None clip 已离开有效范围
- **THEN** Timeline producer MUST 对该 contribution identity 提交 Release
- **AND** 统一 Registry MUST NOT 把历史 sample 当作隐式 Hold

#### Scenario: 非动画轨道采样

- **WHEN** logic tick 推进 active Timeline
- **AND** active Timeline 时间落在 gameplay window、motion、camera 或表现 cue 轨道范围内
- **THEN** 轨道 MUST 将结果写入对应 pipeline output
- **AND** 结果 MUST NOT 绕过 strict gameplay、presentation 和 network 分层

### Requirement: Timeline 动画采样必须和逻辑事实采样分离

系统 MUST 将 Timeline 动画姿态采样和动作事实采样分离。logic tick MUST 推进 active Timeline 时间并采样 motion、window、cue、camera response 等事实，同时可以记录 Complete、Release 和 owner transition 等生命周期事件；presentation frame MUST 使用最近两个 logic Timeline 时间和 `InterpolationAlpha` 计算 visual Timeline time，再重新采样 AnimationTrack。系统 MUST NOT 把 logic tick 中采样出的 AnimationLayerPlaybackPlan 当作表现帧动画时钟，也 MUST NOT 在 terminal handoff 时重新产生 gameplay facts。

#### Scenario: 表现帧高于逻辑 tick

- **WHEN** 两次 logic tick 之间执行多个 `PresentationFrame`
- **THEN** 每个表现帧 MUST 使用当前 `InterpolationAlpha` 重新采样 AnimationTrack
- **AND** Sample MUST 更新同一个 playback/contribution instance
- **AND** 动画姿态 MUST NOT 停在上一 logic tick 的离散 clip time

#### Scenario: Loop Timeline 跨循环边界

- **WHEN** loop Timeline 在 logic tick 中从结尾回到开头
- **THEN** 表现帧 MUST 通过上一 cycle/time 和当前 cycle/time 计算连续 visual Timeline time
- **AND** AnimationTrack MUST 在 wrap 前后更新同一个 loop playback contribution identity
- **AND** 动作 window、motion 和 cue 仍 MUST 按 logic tick 的事实采样输出

#### Scenario: terminal handoff

- **WHEN** Timeline 已在 logic tick 完成
- **AND** terminal presentation sample 尚未提交
- **THEN** PresentationFrame MAY 采样 terminal-pending AnimationTrack，并让已提交的 Complete 作用于该 terminal Sample
- **AND** 该采样 MUST NOT 再次输出 window、cue、motion、camera 或 SyncFacts
- **AND** Registry 接受 terminal Sample 后 MUST 只清理临时 completed-playback metadata，不得释放 owner-held contribution

### Requirement: 动画混合模型是运行时核心

系统 MUST 使用统一动画贡献 Registry、`CharacterAnimationLayerRuntime` 和 `CharacterPresentationStage` 组成精简动画混合模型，用于合并来自 Timeline、StateMachine、Tree、Action 或后续其它来源的动画贡献。Registry MUST 表达播放实例、contribution 实例、owner 和 lifecycle；LayerRuntime MUST 表达层、权重、优先级和最终层结果；PresentationStage MUST 表达 visual time 和 transition outgoing/incoming。任意 producer MUST NOT 绕过该模型应用最终动画。

#### Scenario: 多来源贡献同一动画层

- **WHEN** Registry 中有多个来源向同一动画层提交有效 contributions
- **THEN** 动画层运行时 MUST 按正式规则生成该层最终结果
- **AND** 该结果 MUST 成为表现层应用动画的输入

#### Scenario: Timeline 和状态行为同时提交动画

- **WHEN** active state 行为和 Timeline 轨道同时提交动画贡献
- **THEN** 系统 MUST 在同一 Registry 和动画层模型中合并它们
- **AND** 系统 MUST NOT 让其中任意一方直接绕过 mixer 应用到 Animator

#### Scenario: Producer 当帧未提交

- **WHEN** 某 producer 当表现帧没有新的 Sample
- **THEN** LayerRuntime MUST 使用 Registry 已解析的当前快照
- **AND** LayerRuntime MUST NOT 直接根据 transient submission list 推断该 producer 已释放

### Requirement: CharacterPresentationStage 是 Unity 动画应用边界

系统 MUST 让 `CharacterPresentationStage` 或其下属正式 adapter 成为最终写入 Animator、PlayableGraph 和 Unity 表现对象的边界。`CharacterPresentationStage` MUST 消费统一 Registry 和 LayerRuntime 结果，建立 owner transition session，并在 handoff 完成后确认 outgoing retirement。Timeline 轨道、TimelineNode、状态机 runtime 和 Registry MUST NOT 直接应用最终动画。

#### Scenario: 应用动画混合结果

- **WHEN** presentation frame 动画混合模型生成本帧结果
- **THEN** `CharacterPresentationStage` MUST 消费该表现帧结果并写入 Animator 或 PlayableGraph
- **AND** 其它 stage MUST NOT 直接写入同一个最终动画状态

#### Scenario: 状态 transition 混合

- **WHEN** Registry 提供 source owner outgoing plans 和 target owner incoming plans
- **THEN** `CharacterPresentationStage` MUST 使用 Transition edge 的 duration 和 curve 生成 visual plans
- **AND** blend 完成后 MUST 向 Registry 确认 outgoing 可以 Retire
- **AND** Stage MUST NOT 通过继续 tick source state 获取 outgoing pose

#### Scenario: 表现 adapter 应用动画计划

- **WHEN** 表现层 adapter 应用动画层播放计划
- **THEN** 它 MUST 只消费 `AnimationLayerPlaybackPlan` 或等价正式结果
- **AND** 它 MUST NOT 成为自主 Timeline 播放来源或 contribution 生命周期权威

# Change: 重构统一动画贡献生命周期

## Why

当前角色动画主链路已经统一为：

`Timeline / State / Tree / Action -> AnimationContribution -> CharacterAnimationLayerRuntime -> AnimationLayerPlaybackPlan -> CharacterPresentationStage -> AnimancerAnimationPresenter`

但 `AnimationContribution` 目前只是一份表现帧临时列表，没有正式的播放实例生命周期。`CharacterAnimationLayerRuntime` 每帧只根据当前列表重新构建计划，`AnimancerAnimationPresenter` 又会停止本帧未出现的 state，因此“本帧没有再次提交”被隐式解释成“播放已经正式释放”。

这个隐式语义在一次性 Timeline 和状态切换处会产生确定的断帧：Timeline 在 logic tick 到达结尾后立即从 scheduler active records 中移除，行为树要到后续 logic tick 才观察到 `Succeeded` 并进入下一状态；中间的 `PresentationFrame` 得不到旧贡献，统一层生成空计划或只剩较低优先级贡献，Animancer 随即停止旧状态。现有 Transition Blend 又依赖上一表现帧播放计划，无法保证在空计划帧或单个 render frame 内发生多个 catch-up logic tick 时拿到正确 outgoing。

问题不能通过 Timeline 专属保活列表、Presenter 黏住上一帧、提高 logic tick 或 Animancer 自主播放来修补。项目需要把“提交、完成、释放、状态 owner 退场”提升为所有动画来源共享的正式合同，并继续让统一动画层成为唯一仲裁入口。

## What Changes

- 在所有动画来源和 `CharacterAnimationLayerRuntime` 之间增加来源无关的统一动画贡献生命周期注册表。
- 将动画提交合同扩展为稳定 playback instance、稳定 contribution instance、runtime owner scope 和显式 `Sample / Complete / Release` 生命周期命令。
- 将 StateMachine 每次 state activation 表达为独立 owner scope，使状态重入、并行 Locomotion/Action StateMachine 和状态退场不会只依赖 state GUID 猜测归属。
- 让 Timeline 继续只负责推进时间和提交数据：logic tick 产出事实与完成状态，presentation frame 产出动画采样；Timeline 不负责 layer 仲裁、状态混合或直接控制 Animancer。
- 让 Timeline clip 离开有效采样范围时显式释放对应 contribution；`ExtraPolationMode=None` 不得被统一注册表隐式变成 Hold。
- 让 Once Timeline 完成或 state-owned Timeline 被停止时，当前仍有效的动画 contribution 可以进入 `CompletedHeld`，直到 owner transition/release 正式接管；已提前离开 clip 范围的 contribution 不得被恢复。
- 将状态切换表现事件从“只有 blend duration 大于 0 才发布”收口为始终发布 owner handoff；duration 为 0 表达原子替换，duration 大于 0 表达 outgoing/incoming 混合。
- owner handoff 在 target activation 的 state body 首次实际 tick 前保持 pending；target 的 OnEnter/Root 已获得一次正式执行机会后提交 `OwnerReady`，再在同一表现批次完成零时长替换或启动非零混合，避免 transition tick 与目标 Timeline 首次采样之间出现空计划。
- 增加不会被 `CharacterPipelineFrame.Begin()` 或 `ClearTransient()` 覆盖的动画生命周期提交队列，保证单个 render frame 内多个 catch-up logic tick 的完成、取消和 transition 命令按序到达表现层。
- 让 `CharacterAnimationLayerRuntime` 继续只负责 layer、priority、override/additive 和权重仲裁，改为消费统一注册表快照，而不是直接把当帧临时提交当作完整生命周期真相。
- 让 `CharacterPresentationStage` 从统一注册表获得 outgoing/incoming 计划并负责 transition session；`AnimancerAnimationPresenter` 仍只应用最终计划。
- 让 Timeline 编辑器预览使用 preview session 私有的同类注册表，并在非连续 seek、切换 target 或停止预览时显式清理，避免运行时和预览形成两套语义。
- 删除被新合同取代的“上一帧计划是唯一 outgoing 真相”、Timeline 完成即销毁表现数据和基于提交缺席隐式停止的旧假设，不保留兼容路径。

## Out Of Scope

- 不修改 Corin 动画资源，不为 `Attack1/Attack2` 自动补 Recovery、Hold 或调整 window 时间，不为当前为空的 `WalkEnd` Timeline 自动选择动画。
- 不把动画资源缺口自动解释为 Hold、Idle 或 locomotion fallback；资产没有有效贡献时仍应暴露真实空输出。
- 不修改 `GameplayTickSystem` 默认 logic tick rate、accumulator 或网络发送频率。
- 不改变 action window、cue、motion、root motion、motion warp 或 SyncFacts 的 logic tick 发生时机。
- 不实现真实远端 actor 动画同步、服务端动画状态复制或网络 rollback。
- 不新增自动化测试；实现阶段只执行编译和 OpenSpec 校验。
- 不恢复 TimelinePlayer autonomous playback、Animator Controller fallback、旧 locomotion/action SO 或其它并行播放路径。

## Current Spec Comparison

- `character-animation-layer-runtime` 已规定动画贡献是唯一输入、LayerRuntime 负责仲裁、Animancer 只是 adapter；本 change 与该方向一致，但会修改“本帧所有贡献就是完整真相”和“计划为空即可停止”的未完整生命周期语义。计划只有在统一注册表完成显式释放与 transition handoff 后才可真正为空。
- `character-animation-pipeline` 已规定 TimelinePlaybackScheduler 是时间权威、AnimationTrack 在表现帧采样、所有来源进入同一混合模型；本 change 补齐 terminal sample、playback/contribution identity、owner scope 和显式释放，不允许 scheduler 形成 Timeline 专属 mixer。
- `btsmtl-runnable-timeline-node` 已规定 TimelineNode 只提交、查询和取消逻辑播放请求；本 change 明确该节点生命周期不直接拥有统一 Registry 中的 owner-scoped 表现退场，避免节点返回 Success 时提前清掉状态 outgoing。
- `character-pipeline-runtime` 已规定每个 logic tick 的 `CharacterPipelineFrame` 是 transient output；当前 `Begin()` 会清理上一 logic tick，因此 lifecycle 命令不能继续只依赖该 transient list。本 change 增加 presentation-owned 持久队列，不改变 strict gameplay 或 SyncFacts 的帧语义。
- `gameplay-tick-system` 已允许一个 render frame 执行多个 catch-up logic tick，并要求每 render frame 执行一次 PresentationFrame。本 change 在角色 pipeline 内保证这些 logic tick 的动画生命周期命令不丢失，与该 spec 没有冲突。
- `add-timeline-loop-playback-and-state-transition-blend` 的设计使用“上一表现帧播放计划”作为 outgoing pose。该做法在正常连续渲染时可用，但在 Timeline 完成后的空计划帧和 catch-up logic tick 中不稳定；本 change 用 owner handoff 和统一注册表取代其作为唯一权威的地位，Transition edge 上的 duration/curve authoring 保持不变。
- `add-character-presentation-interpolation/design.md` 仍包含 logic tick 捕获动画 plan 并在 plan 间插值的历史描述；现行 `character-animation-pipeline` 已改为表现帧重采样 Timeline 动画。本 change 以 current spec 和当前实现为准，不恢复 logic tick 动画 pose 烘焙路径。
- `btsmtl-timeline-editor-preview` 已要求预览复用正式采样、LayerRuntime 和 Animancer adapter；本 change 在中间补入 preview session 私有生命周期注册表，避免预览继续直接把单次采样列表当作全部运行状态。

没有发现需要恢复旧路径才能实现的 current spec 冲突。实现阶段如果无法通过 BTSMTL 的通用 runtime scope 合同把 state activation owner 传给内联状态 body，而必须引入 Character 专属旁路或按名称猜测 owner，必须停止并说明 tradeoff。

## Impact

- 影响 BTSMTL StateMachine runtime 的 state activation identity、transition presentation event 和执行 scope 传播。
- 影响 `TimelinePlaybackRequest`、`TimelinePlaybackScheduler`、AnimationTrack 表现采样和 TimelineNode 生命周期状态。
- 影响动画贡献数据合同、统一生命周期注册表、`CharacterAnimationLayerRuntime`、`CharacterPresentationStage` 和 Animancer adapter 的清理时机。
- 影响 `CharacterPipeline` 的 logic-to-presentation 持久命令队列和 transient frame 清理边界。
- 影响 Timeline 编辑器预览会话，但不改变 Timeline asset authoring 数据和 layer 表来源。
- Corin 当前攻击动画覆盖范围和空 WalkEnd Timeline 仍需作者后续正式配置；本 change 不隐藏这些资产事实。

# Design: 角色表现插值层

## Context

当前运行链路是：

`GameplayTickSystem.LogicTick -> CharacterPipeline.LogicTick -> Input -> BTSMTL/Timeline -> MotionStage -> NetworkSend`

以及：

`GameplayTickSystem.PresentationFrame -> CharacterPipeline.PresentationFrame -> CharacterPresentationStage -> AnimancerAnimationPresenter`

结构上已经分了逻辑和表现入口，但数据还没有形成表现历史：

- `TimelinePlaybackScheduler` 在 logic tick 内推进 Timeline，生成 `AnimationContribution`、root motion、window 和 cue。
- `CharacterMotionStage` 在 logic tick 内调用 `CharacterController.Move`，逻辑 Transform 立即改变。
- `CharacterPresentationStage` 只把当前 frame 的 animation contribution 转成 playback plan 并立即应用。
- `CharacterPipelineFrame.ClearTransient()` 会清空当前 output，所以表现帧没有稳定的 previous/current logic sample 可用。

## Goals

- 让 motion 和 animation 在高渲染帧率下使用正式 interpolation alpha 平滑显示。
- 保持 gameplay facts、motion resolve、Timeline window/cue/root motion 全部按 logic tick 发生。
- 明确 logic root 和 visual root 的职责，避免表现插值污染碰撞、网络预测和服务端校正。
- 保持现有 animation contribution / layer runtime / Animancer adapter 主线，不新增播放器权威。

## Non-Goals

- 不把 Timeline 改成渲染帧自主播放。
- 不让 PresentationFrame 再次运行 BTSMTL、Timeline、MotionResolver 或 `CharacterController.Move`。
- 不把 visual pose、visual clip time 或 visual weight 写入 SyncFacts。
- 不用隐藏 Idle、隐藏 visual root fallback 或旧 Animator controller 兜底。

## Proposed Architecture

### 1. Logic sample

新增角色表现样本模型，用于保存最近两个 logic tick 的表现输入。样本至少包含：

- `LocalLogicTick`
- logic world position
- logic world rotation
- grounded 或等价 motion 状态
- 本 tick 的 animation playback plans 或可重建 plans 的 animation contribution snapshot
- motion correction 标记或 correction debug 摘要，供表现层决定是否平滑或快速贴合

该样本属于 presentation runtime history，不属于 `CharacterPipelineOutput` 的 transient 输出。`CharacterPipelineOutput` 仍然表达本 tick 输出，帧末可以清理。

### 2. Visual pose

`CharacterMotionStage` 继续只负责逻辑移动：

`MotionContribution -> MotionResolver -> MotionModifier -> correction phase -> CharacterController.Move -> MotionResult`

表现层根据 previous/current logic sample 和 `InterpolationAlpha` 生成 visual pose：

`visualPosition = Lerp(previous.position, current.position, alpha)`

`visualRotation = Slerp(previous.rotation, current.rotation, alpha)`

visual pose 只能应用到显式配置的 visual root / model root。`CharacterController` 和 logic root 继续代表逻辑真值。

### 3. Visual root binding

`CharacterPipelineHost` 负责提供正式 Unity 绑定：

- logic root 来自 `CharacterController.transform` 或正式绑定对象。
- visual root / model root 是显式序列化字段。
- `AnimancerComponent` 应位于 visual hierarchy 上或与 visual root 绑定关系明确。

缺少 visual root 时，系统报告配置错误。实现不得静默把 logic root 当作 visual root fallback，因为这样会把表现插值重新写回逻辑 Transform。

### 4. Animation visual sample

logic tick 产生 animation contribution 后，表现层应在 logic sample 捕获阶段生成或保存 animation playback sample。PresentationFrame 不重新推进 Timeline，只在 previous/current animation samples 之间生成 visual playback plan。

同一个动画 plan 的稳定匹配键应来自：

- source id
- track name
- clip reference
- layer id
- Animancer layer index
- blend mode

同 key 同时存在于 previous/current 时，visual `clipTime`、`normalizedTime` 和 `weight` 使用 alpha 插值。只存在于 current 的 plan 可以从零权重或 current 值进入；只存在于 previous 的 plan 可以向零权重退出。该过渡只影响显示，不代表 gameplay window 或 Timeline 时间继续存在。

### 5. Presentation frame

`CharacterPresentationStage.Update(context, frame)` 应改为消费 presentation history：

- 如本帧有新的 logic sample，先确认 sample 已捕获。
- 使用 `context.InterpolationAlpha` 生成 visual pose。
- 使用 visual playback plans 应用 Animancer。
- 不写 `StrictGameplayOutput`。
- 不写 `SyncFacts`。
- 不调用 `CharacterController.Move`。

## Tradeoffs

### 方案 A：提高本地 logic tick 到 60/120Hz

业务收益是实现最少，当前系统已经能跑。代价是玩法事实、Timeline window、root motion 和网络 command 都被高频驱动，后续 20/30Hz 网络压力场景仍然要补表现插值。这个方案不解决分层问题，只是暂时掩盖视觉离散。

### 方案 B：让 Animancer 自己连续播放，Timeline 只发起一次

业务收益是动画看起来会顺。代价是 Timeline 事实时间和 Animancer 显示时间会分裂：window/cue/root motion 在 logic tick，动画 pose 在另一个自主时间源。攻击判定、取消帧、root motion 对齐会变难排查。这个方案违反当前 spec 中 TimelinePlaybackScheduler 作为播放权威的口径。

### 方案 C：在 PresentationFrame 重采样 Timeline

业务收益是能直接拿到渲染帧级 clipTime。代价是 PresentationFrame 会变成第二个 Timeline 推进/采样入口，容易重复产生 window/cue 或和 logic Timeline 时间不一致。即使只采动画，也会形成双采样语义，调试复杂。

### 方案 D：保存 logic samples，在表现层插值 visual pose 和 visual animation

业务收益是 gameplay facts 仍锁在 logic tick，视觉可以跟随渲染帧平滑。网络、motion correction、Timeline window 和动画显示之间的边界清楚。代价是需要新增 presentation history、visual root 绑定和 animation sample 插值规则。本 change 采用此方案。

## Risks

- 如果角色 prefab 没有独立 visual root，配置会暴露错误；这是刻意的，不用 fallback 掩盖层级问题。
- 如果动画 plan key 设计不稳定，会导致同一 clip 被反复 stop/start；实现时需要先稳定 key，再做插值。
- 如果 visual root 同时被别的脚本驱动，会出现表现层冲突；实现时需要排查并删除旧直接驱动路径。
- 如果强制 correction 仍使用长时间 visual 平滑，可能出现逻辑和视觉差距过大；实现应提供正式贴合策略或阈值，而不是隐藏补丁。

## Open Questions

- Corin 当前 prefab 的 model root / AnimancerComponent 层级需要实现阶段检查后确认；若没有独立 visual root，需要先调整 prefab 绑定。
- 相机是否跟随 visual root 还是 logic root 本 change 不直接决定；动作手感上通常应由相机系统显式选择跟随目标，后续可单独规划。

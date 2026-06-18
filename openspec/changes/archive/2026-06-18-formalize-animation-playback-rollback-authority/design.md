# 动画驱动运动采样窗口回滚边界设计

## Context

当前 TurnBack 的 root position/yaw 不再由 `OnAnimatorMove` 直接消费，而是由 motion profile 在 simulation tick 内采样：

- runtime 保存 profile 的累计 local X/Z 和累计 yaw。
- tick 内根据 animation normalized window 做差分采样。
- sampled delta/yaw 进入 movement facts，再由统一 motion executor 应用。

这条路径要满足预测回滚，关键输入必须在 restore/replay 时完全一致。F6 失败日志显示，restore 后第一次 replay 分叉点不是 position/yaw，而是 `animationNormalizedTime` 和 `blackboard.animation.locomotionNormalizedTime` 从历史中段变成 `0`。这说明状态机恢复到了 TurnBack，但用于采样 profile delta/yaw 的播放窗口仍可能被表现层的播放/重启逻辑覆盖。

这不代表所有动画播放进度都属于 simulation。纯表现动画、上身视觉、表情、VFX 节奏或不参与逻辑输出的 Animancer 播放可以保持表现层非确定。只有业务明确声明“动画时间参与 motion facts、root motion profile、warp window、命中/取消窗口或等价逻辑输出”时，相关 playback window 才进入可回滚纯数据状态。

## Goals

- 明确 animation-driven sampled motion 是业务可选能力，不是全局动画播放规则。
- 对声明为 sampled motion 的状态/动作，区分首次进入 state 的播放归零和 rollback restore 的中段 resume。
- 保证 `TickSampledMotion`、root motion profile 或等价采样窗口在 restore 后恢复，而不是从 0 重新采样。
- 允许纯表现动画不捕获 normalized time，不要求 replay 后视觉逐帧一致。
- 保持 Presenter 只负责播放和视觉跟随，不成为 movement facts 的权威 source。
- 让 F6/F8 严格工具能证明 TurnBack 从中段恢复后逐 tick 收敛。

## Non-Goals

- 不新增 Animator runtime delta 的回滚模式。
- 不把视觉 blend 权重、纯表现 normalized time、表情或 VFX 节奏全部纳入 simulation。
- 不把所有 Action/Attack 一次性迁移；只有后续业务声明使用 profile-driven motion、warp window 或等价采样输出时才复用本边界。
- 不重写状态机或 Character frame 主线。

## Decisions

### Decision: 采样窗口是否回滚由业务声明决定

默认情况下，动画播放进度是 Presentation Layer 细节，不要求逐 tick 确定，也不进入 rollback snapshot。状态、动作或 motion source 只有在声明使用 `TickSampledMotion`、root motion profile、Motion Warping playback window 或等价 animation-driven sampled output 时，才把相关 playback window 纳入 simulation restore state。

对这类 sampled motion，normalized time 不再只是视觉时间。它必须由 snapshot 或可确定性 runtime state 表达，并在 replay 中作为 profile sampling window 的权威输入。Presenter 可以提供只读进度，也可以在 restore 时被要求 seek 到某个进度，但它不能在 restore 后把同一 alias 当作首次进入重播并归零。

对纯表现动画，Presenter 可以自行播放、混合和恢复视觉，不得反向覆盖 simulation state。

### Decision: 首次进入和 restore resume 是不同事件

首次进入声明为 sampled motion 的 TurnBack：

- 进入状态时捕获 entry facts。
- 播放进度使用 policy 的 `StartNormalizedTime`。
- profile sampling window 可以从新播放段开始。

rollback restore 到声明为 sampled motion 的 TurnBack 中段：

- 播放进度恢复为 snapshot 中的 normalized time。
- previous motion playback progress 恢复为 snapshot 中的 previous value。
- 后续 `Present` 必须复用该恢复状态，不能执行 one-shot restart。

这不是 F6 特例，而是所有被业务声明为 profile-driven / sampled motion 状态的通用规则。未声明为 sampled motion 的动画不需要套用这套恢复语义。

### Decision: Sampling window 恢复比当前 normalized time 更重要

只恢复 current normalized time 不够。profile delta 是 `cumulative(current) - cumulative(previous)`。如果 previous window 丢失，即使 current 恢复到 `0.267`，也可能把第一帧采样成 `0 -> 0.267`，一次性吃掉过多 root motion。

因此 sampled motion restore state 必须能恢复或重建：

- 当前 phase/alias。
- 当前 normalized time。
- previous sampled phase/alias。
- previous normalized time。
- 是否存在有效 previous sampling window。

### Decision: Presenter restart 只响应真实新播放或纯表现播放

基础移动 Presenter 可以继续避免重复播放同一 alias。对于声明为 sampled motion 的 TurnBack 这种 one-shot，restart 只能发生在 phase/alias 的真实新进入，或明确检测到播放段不连续且不是 rollback restore resume。

对于纯表现动画，Presenter 可以按表现需求重播、blend 或从默认进度开始，只要该播放进度没有被用作 motion facts、warp window、hit/cancel window 或其他 simulation 输出的权威输入。

实现层可以通过显式 restore/resume 标记、simulation-owned playback source，或等价纯数据状态让 `Present` 判定“这是恢复后的同一播放段”。不得通过检查 F6、runner 名称或 debug flag 特判。

### Decision: 自动测试优先锁定语义

实现时应先用可控 fake playback source / fake presenter 建立 sampled motion 确定性测试，再补真实 `BasicLocomotionAnimancerPresenter` 的 restore 行为测试。测试必须覆盖：

- 首次进入 TurnBack 从 start normalized time 开始。
- restore 到 TurnBack 中段后不会归零。
- previous/current sampling window restore 后第一 replay tick 采样同一段。
- 纯表现动画没有被强制捕获为 rollback authority。
- `CharacterFrameRollbackSimulation` 或批准的 rollback adapter 走正式 Character frame runtime 主线，而不是直接调用底层 sampler。

## Risks / Trade-offs

- 风险：Presenter 的 Animancer `IsCurrent` 状态在 restore 后可能与 simulation 状态不同步。
  - Mitigation: restore/resume 语义必须重新建立 current phase/gait/key/state，并让后续同 alias `Present` 保持当前播放段。
- 风险：只修 TurnBack 会留下 Action root-motion profile 的同类问题。
  - Mitigation: 规格使用通用 sampled motion 采样窗口术语，TurnBack 只是第一验收状态；Action/Attack 只有声明使用 sampled motion 时才接入。
- 风险：自动测试使用 fake presenter 掩盖真实 Animancer 行为。
  - Mitigation: 必须增加真实 `BasicLocomotionAnimancerPresenter` 层级的恢复语义测试或静态验证，并保留 F6 手动验收。

## Migration Plan

1. 审计当前 snapshot、runtime blackboard、locomotion runtime state 和 presenter playback restore 的字段，并区分 sampled motion 字段与纯表现字段。
2. 明确 sampled motion current playback progress 与 previous motion playback progress 的 capture/restore 顺序。
3. 修正 TurnBack one-shot restart 条件，使 restore resume 不归零。
4. 确保 `BuildMotionPlaybackWindow` 在 restore 后使用恢复的 previous window。
5. 增加自动测试覆盖中段 restore、采样窗口、纯表现动画不进入权威和 Character frame replay。
6. 运行定向 EditMode 测试和 F6/F8 手动验收，复制 `[rollback-synctest] PASS` 或 first mismatch 日志。

## Verification Notes

- Play Mode 触发 TurnBack，确认首次进入仍从动画开头播放。
- TurnBack 中段按 F6，确认 `[rollback-synctest] PASS` 或 first mismatch 不再是 `animationNormalizedTime=0`。
- TurnBack 中段连续触发 F6 多次，确认 replay 后画面不会逐次漂移。
- 触发 F8 soak，确认 TurnBack 窗口不因 playback/window 分叉失败。
- 若失败，复制 `Simulation.synctest-first-mismatch`、`Simulation.synctest-fail-detail` 和 `TURNBACK_RM_CHAIN` 日志。

## Open Questions

- Action/Attack 的未来 profile-driven motion 是否直接复用 Locomotion playback progress 结构，还是抽象出通用 sampled motion playback runtime state；本变更只要求 TurnBack 路径不能写成专用旁路。

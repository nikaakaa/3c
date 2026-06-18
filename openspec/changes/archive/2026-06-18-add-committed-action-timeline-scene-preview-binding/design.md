## Context
Timeline Editor 当前已经有纯数据 preview：scrub locator 时用正式 action definition 编译并通过 `CommittedActionBranchEvaluator` / `ActionTimelineEvaluator` 得到 outcome。该 outcome 能说明当前 tick 选中了哪个 TimelineNode、哪个 animation key、motion spec、window facts 和 cue requests。

用户希望进一步绑定场景中的实际角色实例，在编辑器里直接预览动画姿态。`Ref/wly970123` 的 Taco Timeline 做法是将 Timeline 绑定到 `TimelinePlayer`，由 `PlayableGraph`、`AnimationPlayableOutput`、`AnimationLayerMixerPlayable`、`AnimationClipPlayable` 对目标 Animator 采样。这个思路适合用于 Editor-only preview session，但不能直接引入为 gameplay runner。

## Goals
- Timeline Editor 可以显式绑定场景中的实际角色实例。
- Preview 视觉结果以正式 compiler / evaluator outcome 为唯一数据真相。
- `ActionAnimationKey` 通过正式动画绑定入口解析为可采样动画资源。
- scrub 和 play 都能驱动绑定角色 Animator 的姿态预览。
- 缺失绑定、缺失 Animator、缺失 animation key 解析时显示明确状态，不做隐藏 fallback。
- 预览生命周期能安全清理，避免污染场景角色运行时状态。

## Non-Goals
- 不改变 ActionTimeline runtime 定义或采样权威。
- 不把 scene object、Animator、AnimationClip、PlayableGraph 写入 runtime definition。
- 不新增正式 gameplay Timeline runner。
- 不把 Motion clip 真实提交给 motion executor。
- 不让 Cue clip 播放正式表现事件。

## Decisions
- Decision: Preview target 必须显式绑定。
  - Rationale: 项目要求不要通过层级扫描、Resources 或全局单例创建 fallback 配置。
- Decision: Preview session 位于 Editor-only assembly。
  - Rationale: `PlayableGraph` 和 scene object 只服务编辑器预览，不进入 rollback、runtime definition 或角色帧管线。
- Decision: Preview session 先 evaluate 再 sample。
  - Rationale: timeline 选择、condition、tick 边界和 payload 必须继续由正式 evaluator 决定，视觉层只消费 outcome。
- Decision: 动画采样优先走正式动画绑定入口。
  - Rationale: `ActionAnimationKey` 是稳定语义 key，具体 clip / transition 归 Action Animation Profile、Animancer TransitionLibrary 或等价表现配置，不归 Timeline runtime。
- Decision: 第一版只采样单个 active action animation。
  - Rationale: 先闭合 Dodge / TimelineNode 的主路径，避免过早实现多层混合、IK、event 和 motion executor 预览。
- Decision: Motion 第一版展示为诊断，不执行角色位移。
  - Rationale: 真实位移权威仍属于 motion executor / `CharacterMotionDriver` 主线，Editor preview 不应形成第二运动路径。

## Ref Mapping
- 可参考：`TimelinePlayer.Init` 中创建 `PlayableGraph`、`AnimationPlayableOutput`、`AnimationLayerMixerPlayable`、`AnimatorControllerPlayable` 的方式。
- 可参考：`TimelineAnimationClipPlayable` 使用 `AnimationClipPlayable.SetTime` 并 `PlayableGraph.Evaluate(0)` 的姿态采样方式。
- 不复制：Taco `Timeline` / `Track` / `Clip` 数据模型、`TimelinePlayer` runtime、TreeDesigner runtime node 和 `TimelineRunningTree`。

## Preview Flow
1. Timeline window 持有 scene preview target 引用。
2. Preview session 从 target 解析 Animator 和可选正式动画表现入口。
3. Scrub / play 设置 preview local tick。
4. Preview adapter 使用正式 action definition 编译 runtime branch。
5. `CommittedActionBranchEvaluator` 得到当前 tick outcome。
6. Animation resolver 将 outcome 的 `ActionAnimationKey` 解析为 clip/transition。
7. Preview graph 对 target Animator 采样对应 local time。
8. Timeline UI 高亮 active clips、显示 binding status、motion/window/cue 摘要。

## Risks / Trade-offs
- Risk: 采样实际角色会覆盖场景中 Animator 当前姿态。
  - Mitigation: 预览必须有明确启停和清理；停止/关闭时销毁 graph 并恢复进入预览前的 transform / animator controller / enabled 状态或批准等价状态。
- Risk: 直接复用 runtime `CharacterAnimancerPresenter` 可能改变正式播放状态。
  - Mitigation: resolver 只读动画绑定信息；采样由独立 Editor-only graph 完成，不调用 presenter 的播放方法。
- Risk: 没有独立 ActionAnimationProfileSO 实现时 key 解析不稳定。
  - Mitigation: 第一版允许通过绑定角色的 Animancer TransitionLibrary 或批准等价正式动画绑定入口解析；解析失败必须显示错误状态。
- Risk: PlayMode 绑定真实角色会和 gameplay runtime 抢 Animator。
  - Mitigation: 第一版只支持 EditMode preview；PlayMode 下禁用采样或只显示数据预览。
- Risk: 后续引用旧 archive 或旧文档时可能重新带入 Timeline 嵌入 Branch Panel 的口径。
  - Mitigation: 以 current spec 的独立 Timeline Window 定位 TimelineNode 为准，Branch Graph 只提供打开或聚焦入口。

## Manual Verification Guidance
实现完成后，设计者可在 Unity EditMode 中打开 `Tools/3C/Committed Action Timeline Editor`，选择正式 action definition 和 TimelineNode，绑定场景中的 Corin 角色实例，拖动 locator 或播放 preview。预期角色 Animator 姿态随 active AnimationKey 采样变化；Motion / Window / Cue 在 Timeline UI 中显示诊断；解绑或关闭窗口后 preview graph 被清理，角色不会继续由 preview 驱动。

## Open Questions
- 第一版是否需要保存最近绑定的 scene target 到 EditorPrefs，还是只保留窗口内临时引用？
- 若 active animation transition 不是 `ClipTransition`，第一版是否只报告 unsupported，还是尝试从 TransitionAsset 解包更多类型？
- 是否需要第一版绘制 motion ghost/path，还是先只显示 motion spec 摘要？

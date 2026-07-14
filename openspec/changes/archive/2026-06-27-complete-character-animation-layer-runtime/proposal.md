# Change: 完成角色动画层运行时主链路

## Why

当前角色管线已经有三块零件：`TimelinePlaybackScheduler` 能从 Timeline 采样 `AnimationContribution`，`AnimationMixer` 能按层生成简单混合结果，`AnimancerAnimationPresenter` 能把结果写入 Animancer。但是这还不是完整动画层。

真正缺的是正式业务合同：谁可以产出动画贡献、动画层怎么定义、没有贡献时是否允许隐藏 fallback、Timeline 动画和后续状态行为/Action 动画如何统一仲裁，以及 Animancer 在链路里到底是业务层还是最终 Unity adapter。

本变更把口径收紧为：动画层属于角色管线运行时；Timeline、状态行为和后续 Action 只能提交动画贡献；动画层统一仲裁后生成播放计划；Animancer 只负责按播放计划写入 Unity 动画状态。系统不恢复旧 `AnimationPresentationPolicySO`、locomotion/action/footphase/bodyclaim 等分裂配置，也不新增第二套播放入口。

## What Changes

- 新增 `character-animation-layer-runtime` 能力，定义角色动画层的输入合同、层定义、混合规则、Animancer adapter 边界和清理规则。
- 将当前 `AnimationContribution` 语义提升为所有动画来源的唯一输入合同，而不只是 Timeline 的临时输出。
- 明确动画层定义来自 `CharacterPipelineDefinition` 的正式 layer 表，Timeline 和节点只引用 layer id，不能从旧表现策略 SO 或隐藏 fallback 读取。
- 明确 `CharacterPresentationStage` 内部的动画层负责仲裁，`AnimancerAnimationPresenter` 只应用最终播放计划。
- 明确 base pose、Idle、Move、Attack、Hit 等动画都必须作为正式贡献来源进入动画层；没有贡献时不自动播放隐藏 Idle。
- 清理角色管线运行路径中绕过动画层的 `Animator.Play`、`Animator.CrossFade`、`TimelinePlayer` autonomous playback 或其它直接写姿态入口。

## Non-Goals

- 不实现 FootPhase、HitWindow、IFrame、Parry、伤害、命中或服务端裁决。
- 不重做 BTSMTL graph inline 数据重构；该工作属于 `replace-btsmtl-subasset-graphs-with-inline-data`。
- 不重做完整 Animancer Transition Library；当前阶段 Timeline 仍然直接控制 clip time 和 weight。
- 不新增旧式 locomotion state、action SO、bodyclaim policy 或 animation presentation policy。
- 不实现完整编辑器动画调试窗口，只保留运行时 snapshot 和清晰数据边界。
- 不编写测试，除非后续明确要求。

## Current Reality

- `CharacterPipelineHost` 已经序列化 `AnimancerComponent` 并传入 `CharacterPipeline`。
- `CharacterPipeline.LatePhase()` 已经执行 `CharacterPresentationStage.Update()`。
- `TimelinePlaybackScheduler` 已经把 `AnimationTrack.Sample()` 的结果写入 `PresentationOutput.AnimationContributions`。
- `AnimationMixer` 当前只做按层、优先级、override/additive 的最小混合。
- `AnimancerAnimationPresenter` 当前已经能设置 `AnimancerLayer`、`AnimancerState.Time`、`Speed`、`Weight`、`Mask` 和 `IsAdditive`。
- 当前 current spec `btsmtl-runnable-timeline-node` 仍写着 `TimelineNode` 直接绑定 `TimelinePlayer` 播放；已完成但未归档的 `refactor-timeline-animation-pipeline-authority` 正在覆盖这个口径。本变更依赖该覆盖语义，不回到直接播放模式。

## Dependency

- `refactor-timeline-animation-pipeline-authority` 必须保持为前置语义：`TimelineNode` 只提交播放请求，`TimelinePlaybackScheduler` 是角色管线模式下的 Timeline 播放权威。
- `replace-btsmtl-subasset-graphs-with-inline-data` 可并行推进，但本变更不依赖它完成；本变更只消费运行时已经解析到的 Graph/Timeline 输出。

## Impact

- `Character/Pipeline/Presentation` 下的动画模型需要从“表现 stage 临时结构”收口为正式动画层模型。
- `Timeline.Animation` 的轨道字段需要和正式动画层定义对齐，避免继续靠裸 int layer 和每条轨道随意 mask 制造冲突。
- `CharacterPipelineDefinition` 承载最小动画层配置；Timeline/节点数据只引用 layer id，不再保存 layer 的固定 mask/additive 真数据。
- 运行时如果没有 base layer 动画贡献，应当暴露为空输出或报配置问题，而不是自动播放隐藏 Idle。
- 旧直接播放代码如果仍位于 BTSMTL 原生预览路径，可以保留为 BTSMTL 编辑器/参考链路；但角色管线运行链路不得引用它。

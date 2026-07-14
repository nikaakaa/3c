# Design: 角色动画层运行时主链路

## 目标

本设计只解决一件事：角色管线如何把 Timeline、状态行为和后续 Action 产出的动画意图，统一变成可提交给 Animancer 的播放计划。

目标链路：

```text
Graph / State / Timeline / Action
-> AnimationContribution
-> CharacterAnimationLayerRuntime
-> AnimationLayerPlaybackPlan
-> AnimancerAnimationPresenter
-> AnimancerComponent
```

Animancer 不决定“该不该攻击、该不该 Idle、能不能打断”；这些属于管线和图运行时。Animancer 只负责把已经仲裁好的 clip、time、weight、layer 和 mask 写到 Unity 动画系统。

## 当前误区纠正

“Animancer 接入”不等于“动画层完成”。当前 `AnimancerAnimationPresenter` 只是最后的播放出口。真正的动画层至少还需要：

- 统一动画贡献输入合同。
- 明确动画层定义来源。
- 明确多来源冲突时的仲裁规则。
- 明确无贡献时的处理策略。
- 明确哪些旧播放入口必须从角色管线里消失。

## 数据归属

动画数据分三类：

- **Authoring 数据**：Timeline clip、track layer id、priority、状态行为图引用，以及 `CharacterPipelineDefinition` 中的 layer 表。
- **Pipeline 运行数据**：本帧 `AnimationContribution`、混合结果、snapshot、播放计划。
- **Unity adapter 数据**：Animancer layer/state、AnimationClip、AvatarMask。

业务取舍：

- 不把 Animancer transition 当作 authoring 主数据，因为 Timeline 已经负责动作片段时间、权重和 overlap；再让 Animancer transition 重新决定 fade 会产生双权威。
- 不恢复 `AnimationPresentationPolicySO`，因为它会把 layer/mask/priority 从 Timeline/节点数据旁路出去，回到旧分裂配置。
- 不把 layer/mask 完全硬编码在 presenter，因为 presenter 应该是 adapter，不应该知道动作业务含义。

## 动画层定义方案

实现采用方案 A：`CharacterPipelineDefinition` 持有最小动画层表。Timeline track 和后续节点只引用 layer id，不重复定义 mask/additive 这类层级固定信息。

### 采用方案 A：管线定义持有最小动画层表

`CharacterPipelineDefinition` 持有角色动画层表，例如 Base、UpperBody、Additive。Timeline track 和后续节点只引用 layer id，不重复定义 mask/additive 这类层级固定信息。

优点：

- 角色层结构统一，Animancer layer 数量、mask、additive 规则可集中检查。
- Timeline track 不容易互相写出冲突 mask。
- 更符合求职 demo 的可解释性：面试时能说清楚角色动画层布局。

代价：

- `CharacterPipelineDefinition` 会承载少量动画表现配置。
- 需要迁移现有 `AnimationTrack.Layer/AvatarMask/BlendMode` 语义，避免两套数据并存。

未采用方案 B：Timeline/节点贡献完整 layer 信息。它改动小，但会让 mask/blend mode 散落在各个 Timeline，后续动作规模变大后难维护，也会重新制造多份真数据。

## 运行时职责

### Contribution 来源

所有动画来源只做一件事：写 `AnimationContribution`。

- Timeline `AnimationTrack`：按 Timeline time 输出 clip time、weight、priority、source id。
- 状态行为节点：后续可直接输出 base pose 或 motion pose contribution。
- Action runtime：后续也只输出 contribution，不直接写 Animator/Animancer。

### Mixer

`CharacterAnimationLayerRuntime` 或等价类负责：

- 收集本帧 contribution。
- 按 layer 分组。
- 校验 layer 是否存在。
- 按 priority 和 blend mode 计算最终结果。
- 生成播放计划和 snapshot。

### Presenter

`AnimancerAnimationPresenter` 负责：

- 按播放计划创建或复用 Animancer state。
- 设置 time、speed、weight、mask、additive 和 layer weight。
- 停掉本帧不再被计划引用的旧 state。

Presenter 不负责：

- 自动补 Idle。
- 判断动作能否打断。
- 读取输入。
- 读取 Timeline 资产。
- 参与状态机 transition。

## 无贡献策略

本变更禁止隐藏 fallback。

如果 base layer 没有 contribution，动画层可以让当前已管理状态归零或停止，但不能偷偷播放 Idle。Idle 必须由 Idle 状态行为、Timeline 或其它正式节点输出。

业务取舍：

- 隐藏 fallback 能减少报错，但会掩盖 Graph 没有产出 base pose 的结构问题。
- 对当前求职 demo 来说，宁可暴露空动画层，也不要让行为图缺口被 presenter 自动修好。

## 清理策略

角色管线运行链路中必须删除或隔离：

- `Animator.Play` / `Animator.CrossFade` 直接应用动画。
- `TimelinePlayer.FixedUpdate` 或 autonomous playback 作为角色动画来源。
- `TimelineNode` 直接实例化并推进 Timeline。
- 旧 `AnimationCommand` 命令式播放模型。
- 旧 `AnimationPresentationPolicySO` 或旧 locomotion/action 配置读取。

BTSMTL 原生 Timeline 编辑器预览可以继续存在，但它不能被 `CharacterPipelineHost`、`CharacterPipeline`、`TimelinePlaybackScheduler` 或 `CharacterPresentationStage` 当作运行依赖。

## 和其它 active change 的关系

`replace-btsmtl-subasset-graphs-with-inline-data` 改的是 Graph 数据归属；本设计不要求它先完成。只要运行时能拿到 Graph tick 和 Timeline 请求，本设计就能成立。

当前 `btsmtl-runnable-timeline-node` current spec 与已完成的 `refactor-timeline-animation-pipeline-authority` 存在口径差异：current spec 还描述 TimelineNode 直接 TimelinePlayer 播放，而新链路要求 TimelineNode 只提交请求。本变更按已完成 change 的新语义继续规划，不回退。

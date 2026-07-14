# Design: Timeline 编辑器预览权威收敛

## 当前链路

正式角色管线链路：

```text
TimelineNode
  -> ITimelinePlaybackService
  -> CharacterGraphContext
  -> CharacterBTSMTLPhase
  -> TimelinePlaybackScheduler
  -> AnimationTrack.Sample / MotionWarpTrack.Sample / ActionWindowTrack.Sample
  -> CharacterPipelineOutput
  -> CharacterAnimationLayerRuntime
  -> AnimancerAnimationPresenter
```

编辑器预览旧链路：

```text
TimelineEditorWindow
  -> TimelinePlayer target
  -> Timeline.Bind(TimelinePlayer)
  -> Timeline.Evaluate(deltaTime)
  -> Track.Evaluate(deltaTime)
  -> PlayableGraph / AnimationRootPlayable
```

问题是第二条链路没有经过 `CharacterPipelineDefinition.AnimationLayers`、`CharacterAnimationLayerRuntime` 和正式 Animancer adapter，无法代表角色管线实际播放结果。

## 目标链路

```text
TimelineEditorWindow
  -> TimelinePreviewSession
      -> TimelinePreviewTarget
      -> CharacterPipelineHost preview target
      -> runtime Timeline clone
      -> preview time/playSpeed/isPlaying
      -> AnimationTrack.Sample(...)
      -> AnimationContribution
      -> CharacterAnimationLayerRuntime.Build(...)
      -> AnimancerAnimationPresenter.Apply(...)
      -> AnimationLayerFrameSnapshot
```

`TimelinePreviewSession` 是 editor-only 预览控制器，不是新的 runtime 权威。它的价值是让编辑器播放按钮、时间轴游标和可视化数据都走正式采样和动画层规则。Editor asmdef 只依赖 `BTSMTL.Timeline` 中的 `TimelinePreviewTarget` 抽象，具体角色动画应用由 `CharacterPipelineHost` 实现，避免 Timeline Editor 直接引用角色管线程序集。

## 核心对象

### TimelinePreviewTarget

`TimelinePreviewTarget` 是 Timeline runtime 程序集中的预览目标抽象。它只表达三件事：

- 当前对象是否可以预览 Timeline。
- 在指定时间点评估 Timeline 预览。
- 清理预览输出。

它不保存播放状态，不持有 Timeline 资产，也不定义角色动画层规则。

### CharacterPipelineHost 预览目标实现

`CharacterPipelineHost` 继续负责角色管线装配和注册。为了让预览目标使用正式配置，它需要暴露只读属性：

- `Definition`
- `Animancer`

这些属性只是已序列化依赖的只读入口。Host 作为 `TimelinePreviewTarget` 实现动画预览应用，但不承担状态判断、motion 结算或 combat 裁决。

### TimelinePreviewSession

职责：

- 持有当前编辑的 Timeline 资产和运行时 clone。
- 持有当前 `TimelinePreviewTarget` 目标。
- 持有 `Time`、`PlaySpeed`、`IsPlaying`。
- 处理 `Play`、`Pause`、`SetTime`、`Tick`、`Dispose`。
- 调用预览目标在指定时间点评估 Timeline。
- 由 `CharacterPipelineHost` 实现正式采样、动画贡献转换、动画层仲裁和 Animancer 应用。

非职责：

- 不提交 gameplay action request。
- 不运行完整 `CharacterPipeline`。
- 不驱动 `TimelineNode`。
- 不直接裁决命中、窗口结果或网络事实。
- 不作为运行时 fallback。

## 预览目标选择

### 方案 A：预览目标是 `TimelinePlayer`

优点：

- 改动小。
- 旧 UI 和旧 PlayableGraph 代码可继续工作。

缺点：

- 继续保留编辑器和 runtime 两套播放真相。
- 无法自然读取 `CharacterPipelineDefinition.AnimationLayers`。
- 预览结果不能代表角色管线实际 Animancer layer、priority、weight 规则。
- 和“不新增 Timeline 播放分裂路径”冲突。

结论：不选。

### 方案 B：预览目标是 `Animator` 或 `AnimancerComponent`

优点：

- 目标比 `TimelinePlayer` 干净。
- 可以直接应用动画。

缺点：

- 仍然缺少 `CharacterPipelineDefinition` 动画层表。
- 需要额外配置 layer 规则，容易形成第二份配置。
- 不能表达“这是某个角色管线下的 Timeline 预览”。

结论：不选为正式目标。

### 方案 C：预览目标是 `CharacterPipelineHost`

优点：

- 直接复用正式角色装配入口。
- 能读取 `CharacterPipelineDefinition.AnimationLayers`。
- 能拿到正式 Animancer 应用目标。
- 不新增 layer 配置来源。
- 预览结果最接近 runtime 管线表现。

缺点：

- 需要 `CharacterPipelineHost` 暴露只读装配信息。
- 预览 session 要处理 editor-only 生命周期和 runtime clone 清理。

结论：选择。

### 方案 D：Editor 选择 `TimelinePreviewTarget`，`CharacterPipelineHost` 实现它

优点：

- Editor asmdef 只依赖 Timeline runtime 抽象，不直接依赖角色管线程序集。
- 仍然保证实际角色动画预览由 `CharacterPipelineHost` 使用正式配置完成。
- 后续如果有其它正式管线目标，也可以实现同一抽象，不增加第二套播放规则。

缺点：

- 比直接选择 `CharacterPipelineHost` 多一个很薄的抽象类型。
- 需要确保普通对象不会伪装成绕过正式管线的预览目标。

结论：实现选择。业务上它仍然是方案 C 的管线预览目标，只是在程序集边界上加了正式抽象。

## TimelinePlayer 去留

### 方案 A：短期保留 `TimelinePlayer` 作为 editor-only preview

业务取舍：

- 能最快保持 UI 可播放。
- 但用户看到的不是正式动画层输出，会继续误导调参。
- 之后仍要再删一次，形成二次迁移。

结论：不选。

### 方案 B：把 `TimelinePlayer` 改成表现层 adapter

业务取舍：

- 可以复用 PlayableGraph 封装。
- 但当前正式表现已经落到 Animancer adapter，继续保留 `TimelinePlayer` 会让“谁负责最终应用动画”变模糊。

结论：当前不选。后续若真的需要 PlayableGraph adapter，应以新的正式表现 adapter 命名和边界接入，而不是复活旧播放器。

### 方案 C：预览切到 `TimelinePreviewSession` 后删除 `TimelinePlayer`

业务取舍：

- 删除旧播放权威，链路最干净。
- 需要同步处理依赖 `Timeline.TimelinePlayer` 的旧轨道和 Tree 相关类。
- 当前实现成本高于只改 UI，但不会制造下一轮清理债。

结论：选择。

## 轨道处理

第一阶段只要求动画预览闭环：

- `AnimationTrack` 通过 `Sample` 输出动画贡献。
- root motion 可生成调试样本，但预览 session 不直接移动角色 Transform。
- motion warp、action window、action cue 可进入 preview snapshot 或调试列表，不直接改 gameplay。

旧依赖 `Timeline.TimelinePlayer` 的轨道必须处理：

- `Timeline.TimeControl`：旧播放器速度控制，删除或迁移成预览 session 控制。
- `Timeline.GameObject`、`Timeline.ParticleSystem`、`Timeline.Cinemachine`：如果继续保留，必须改成正式 cue 输出或预览专用 adapter；不能继续读 `Timeline.TimelinePlayer.transform`。
- `Timeline.Node`、`TimelineRunningTree`：保留 Timeline 驱动 Tree 能力时必须脱离 `TimelinePlayer`，否则从当前预览闭环移除。

## 数据和 UI

- Timeline 资产继续是可复用数据资产。
- Timeline 编辑器继续负责轨道、clip、字段 inspector、时间轴绘制。
- 播放状态从 `Timeline` 资产移出，进入 `TimelinePreviewSession`。
- 时间轴游标显示读取 `TimelinePreviewSession.Time`。
- target field 选择 `TimelinePreviewTarget`，当前正式实现是 `CharacterPipelineHost`。
- 没有 target 时，UI 可以编辑数据，但播放按钮和应用预览禁用。

## 清理原则

- 不保留 `TimelinePlayer` autonomous tick。
- 不保留 `Timeline.TimelinePlayer` 字段作为兼容入口。
- 不新增 fallback target。
- 不新增第二份 animation layer 配置。
- 不通过场景搜索自动补齐 Host、Animancer 或 Definition。

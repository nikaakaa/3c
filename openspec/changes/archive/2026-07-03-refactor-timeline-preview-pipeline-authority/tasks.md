## 1. 暴露预览所需正式装配入口

- [x] 1.1 在 `CharacterPipelineHost` 暴露只读 `Definition`。
- [x] 1.2 在 `CharacterPipelineHost` 暴露只读 `Animancer`。
- [x] 1.3 确认 Host 不新增状态切换、motion、combat 或 Timeline 业务逻辑。

## 2. 建立 TimelinePreviewSession

- [x] 2.1 新增 editor-only `TimelinePreviewSession` 类型。
- [x] 2.2 记录当前 Timeline 资产引用。
- [x] 2.3 为预览创建运行时 Timeline clone。
- [x] 2.4 在切换 Timeline 时释放旧 clone。
- [x] 2.5 记录当前 `TimelinePreviewTarget` 预览目标。
- [x] 2.6 在切换目标时清理旧目标预览输出，并把具体动画层 runtime 和 Animancer presenter 重建交给预览目标实现。
- [x] 2.7 记录 `Time`、`PlaySpeed`、`IsPlaying`。
- [x] 2.8 实现 `Play`。
- [x] 2.9 实现 `Pause`。
- [x] 2.10 实现 `SetTime(float time)`。
- [x] 2.11 实现 `Tick(float deltaTime)`。
- [x] 2.12 在 `Dispose` 中释放 clone 并停止预览应用状态。

## 3. 复用正式 Timeline 采样

- [x] 3.1 在 preview clone 刷新时调用 Timeline `Init()`。
- [x] 3.2 在 preview tick 中推进 session time。
- [x] 3.3 在 `CharacterPipelineHost` 预览实现中遍历 Timeline tracks。
- [x] 3.4 对 `AnimationTrack` 调用 `Sample(previousTime, time, ...)`。
- [x] 3.5 将 `TimelineAnimationContribution` 转成 `AnimationContribution`。
- [x] 3.6 使用 `CharacterAnimationLayerRuntime.Build(...)` 生成播放计划。
- [x] 3.7 使用 `AnimancerAnimationPresenter.Apply(...)` 应用播放计划。
- [x] 3.8 输出 `AnimationLayerFrameSnapshot` 供调试 UI 读取。

## 4. 改造 TimelineEditorWindow

- [x] 4.1 将 target field 类型从 `TimelinePlayer` 改为 `TimelinePreviewTarget`。
- [x] 4.2 target 改变时调用 preview session 设置目标。
- [x] 4.3 Timeline 初始化时调用 preview session 设置 Timeline。
- [x] 4.4 播放按钮改为控制 preview session。
- [x] 4.5 暂停按钮改为控制 preview session。
- [x] 4.6 播放速度输入改为控制 preview session。
- [x] 4.7 editor update 中只 tick preview session。
- [x] 4.8 删除窗口中基于选中 `TimelinePlayer.RunningTimelines` 自动切换 Timeline 的逻辑。
- [x] 4.9 窗口销毁和编译前释放 preview session。

## 5. 改造 TimelineFieldView

- [x] 5.1 marker 和 time locator 的启用状态改为读取 preview session 是否有目标和 Timeline。
- [x] 5.2 时间轴游标显示改为读取 preview session time。
- [x] 5.3 拖拽游标时调用 preview session `SetTime`。
- [x] 5.4 删除 `Timeline.TimelinePlayer.IsPlaying` 调用。
- [x] 5.5 删除 `Timeline.TimelinePlayer.Evaluate` 调用。

## 6. 清理 Timeline 资产旧播放状态

- [x] 6.1 从 `Timeline` 删除 `TimelinePlayer` 字段和属性。
- [x] 6.2 从 `Timeline` 删除 `PlayableGraph` 字段。
- [x] 6.3 从 `Timeline` 删除 `AnimationRootPlayable` 字段。
- [x] 6.4 从 `Timeline` 删除 `AudioRootPlayable` 字段。
- [x] 6.5 从 `Timeline` 删除 `Bind(TimelinePlayer)`。
- [x] 6.6 从 `Timeline` 删除 `Unbind()`。
- [x] 6.7 从 `Timeline` 删除旧 `Evaluate(float deltaTime)` 播放入口，或收敛为纯数据时间更新。
- [x] 6.8 从 `Timeline` 删除 `JumpTo`、`RebindAll` 和 `RebindTrack` 中依赖旧播放器的逻辑。

## 7. 处理旧 TimelinePlayer 依赖轨道

- [x] 7.1 删除或迁移 `Timeline.TimeControl` 对 `Timeline.TimelinePlayer.PlaySpeed` 的依赖。
- [x] 7.2 删除或迁移 `Timeline.GameObject` 对 `Timeline.TimelinePlayer.transform` 的依赖。
- [x] 7.3 删除或迁移 `Timeline.ParticleSystem` 对 `Timeline.TimelinePlayer.transform` 的依赖。
- [x] 7.4 删除或迁移 `Timeline.Cinemachine` 对 `Timeline.TimelinePlayer.transform` 的依赖。
- [x] 7.5 删除或迁移 `Timeline.Node` 对 `TimelinePlayer` 和 `AnimationRootPlayable` 的依赖。
- [x] 7.6 删除或迁移 `TimelineRunningTree` 对 `Timeline.TimelinePlayer` 的依赖。

## 8. 删除旧播放器路径

- [x] 8.1 删除 `TimelinePlayer.cs`。
- [x] 8.2 使用 `rg` 确认正式代码中没有 `Timeline.TimelinePlayer`。
- [x] 8.3 使用 `rg` 确认正式代码中没有 `typeof(TimelinePlayer)`。
- [x] 8.4 使用 `rg` 确认正式代码中没有 `TimelinePlayer autonomous tick` 入口。
- [x] 8.5 确认 `TimelineNode` 仍只通过正式 Timeline 播放请求服务工作。

## 9. 静态校验

- [x] 9.1 运行 `openspec validate refactor-timeline-preview-pipeline-authority --strict --no-interactive`。
- [x] 9.2 使用 Unity 项目生成的 C# 工程进行编译检查。

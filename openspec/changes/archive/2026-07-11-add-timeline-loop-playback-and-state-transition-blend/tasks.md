## 1. 合同梳理

- [x] 1.1 确认 `TimelineNode` 当前序列化字段、请求 handle 和状态查询接口。
- [x] 1.2 确认 `TimelinePlaybackScheduler` active record 的时间、duration、完成状态和采样入口。
- [x] 1.3 确认 `AnimationContribution`、`AnimationLayerPlaybackPlan` 和 `CharacterPresentationStage` 当前插值字段。
- [x] 1.4 确认 `StateMachineGraph` Transition edge 当前序列化、Inspector 和 runtime 选择逻辑。

## 2. TimelineNode loop authoring

- [x] 2.1 增加 `TimelineNode` 正式播放模式字段，默认值为 `Once`。
- [x] 2.2 让 `TimelineNode` 提交播放请求时携带播放模式。
- [x] 2.3 让 `TimelineNode` 在 `Once` 模式下保持现有 `Succeeded -> Success` 行为。
- [x] 2.4 让 `TimelineNode` 在 `Loop` 模式下对持续运行 request 保持 `Running`。
- [x] 2.5 在 `TimelineNode` Inspector 或节点配置视图显示播放模式。

## 3. TimelinePlaybackScheduler loop runtime

- [x] 3.1 扩展播放请求和 active record 保存播放模式。
- [x] 3.2 让 `Loop` request 到达 duration 后回绕播放相位并保持 `Running`。
- [x] 3.3 让 `Loop` request 的 handle、source id 和 source name 在循环期间保持稳定。
- [x] 3.4 对 duration 小于等于 0 的 `Loop` Timeline 暴露配置错误，不做 fallback。
- [x] 3.5 让 stop/reset 或状态离开能取消 `Loop` request 并清理 active record。

## 4. 回绕轨道采样

- [x] 4.1 将 scheduler 的采样区间扩展为可表达跨 duration 边界。
- [x] 4.2 跨边界时按尾段和头段采样动画轨道。
- [x] 4.3 跨边界时按尾段和头段采样 motion 轨道。
- [x] 4.4 跨边界时按尾段和头段采样 action window / cue 轨道。
- [x] 4.5 防止同一回绕边界的 window / cue 重复发样。

## 5. 循环动画插值

- [x] 5.1 为动画贡献增加循环上下文，能表达 loop flag、clip duration 和连续时间或 cycle 信息。
- [x] 5.2 让 Timeline 动画采样填充循环上下文。
- [x] 5.3 让动画层运行时保留循环上下文到播放计划。
- [x] 5.4 让 `CharacterPresentationStage` 对循环 clip time 使用前进方向插值。
- [x] 5.5 保持非循环 clip 的现有插值语义。

## 6. Transition 动画混合 authoring

- [x] 6.1 在 `StateMachineGraph` Transition edge 上增加动画混合时长字段。
- [x] 6.2 为 Transition edge 预留或增加混合曲线 / profile 字段。
- [x] 6.3 在 Transition Inspector 显示动画混合配置。
- [x] 6.4 保持 Condition rule graph 只表达 Bool 条件，不读取或保存 blend 调度逻辑。

## 7. Transition 动画混合 runtime

- [x] 7.1 状态切换时从命中的 Transition edge 读取动画混合元数据。
- [x] 7.2 将状态切换动画混合事实写入正式 pipeline presentation 输出。
- [x] 7.3 状态切换后只 tick 新 active state，不继续 tick 旧状态行为图。
- [x] 7.4 确保旧状态 Timeline 不再产生 gameplay window、cue、motion 或 action facts。

## 8. 表现层 outgoing / incoming 混合

- [x] 8.1 让 `CharacterPresentationStage` 保留上一帧动画播放计划作为 outgoing pose 候选。
- [x] 8.2 收到 transition blend 事实时创建表现层 blend 会话。
- [x] 8.3 blend 会话期间按 edge 时长和曲线淡出 outgoing 计划。
- [x] 8.4 blend 会话期间按 edge 时长和曲线淡入 incoming 计划。
- [x] 8.5 blend 结束后清理 outgoing 计划，不保留隐藏 fallback。

## 9. Corin 资产迁移

- [x] 9.1 移除 `Idle` 状态 body 中包住 Timeline 的普通 `LoopNode`。
- [x] 9.2 移除 `WalkLoop` 状态 body 中包住 Timeline 的普通 `LoopNode`。
- [x] 9.3 移除 `RunLoop` 状态 body 中包住 Timeline 的普通 `LoopNode`。
- [x] 9.4 将 `Idle` TimelineNode 配置为 `Loop` 播放模式。
- [x] 9.5 将 `WalkLoop` TimelineNode 配置为 `Loop` 播放模式。
- [x] 9.6 将 `RunLoop` TimelineNode 配置为 `Loop` 播放模式。
- [x] 9.7 确认 RootTree 顶层 `Runtime Loop` 保留。
- [x] 9.8 为 Corin locomotion/action 关键 Transition edge 写入正式 blend 配置字段。

## 10. 清理与验证

- [x] 10.1 搜索并删除因本 change 失效的 Timeline loop 临时配置或旧状态 body loop 用法。
- [x] 10.2 确认实现未新增旧 SO、旧 TimelinePlayer autonomous playback、fallback 配置或兼容路径。
- [x] 10.3 运行 `openspec validate add-timeline-loop-playback-and-state-transition-blend --strict --no-interactive`。

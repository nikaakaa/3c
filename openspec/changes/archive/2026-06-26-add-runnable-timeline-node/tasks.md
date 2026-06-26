## 1. 边界确认
- [x] 1.1 确认 `Timeline` 资产保持 `ScriptableObject`，不继承 `RunnableNode`。
- [x] 1.2 确认旧 `TimelineReferenceNode : BaseNode` 只是引用壳，并已改造为 `TimelineNode : RunnableNode`。
- [x] 1.3 确认 `TreeTrack/TreeClip/TimelineRunningTree` 保留。
- [x] 1.4 确认不新增 `TimelineStateNode`。

## 2. TimelineNode 节点
- [x] 2.1 新增或改造 `TimelineNode : RunnableNode`。
- [x] 2.2 让 `TimelineNode` 使用 `TimelineReferenceModule` 引用 Timeline。
- [x] 2.3 保留 module 字段扫描和 asset reference 验证链路。
- [x] 2.4 让节点创建菜单在行为 Graph/状态行为 Graph 中暴露 `TimelineNode`。
- [x] 2.5 移除或重命名会误导为纯引用节点的入口。

## 3. 播放生命周期
- [x] 3.1 定义 `OnStart` 初始化 Timeline 播放。
- [x] 3.2 定义 `TimelineNode` 从引用资产创建运行时 Timeline 实例。
- [x] 3.3 定义 `OnUpdate` 调用运行实例的 `Timeline.Evaluate(deltaTime)`，并通过同一 TimelinePlayer 评估本帧 PlayableGraph。
- [x] 3.4 定义 Timeline 未结束时返回 `Running`。
- [x] 3.5 定义 Timeline 播放完成时返回 `Success`。
- [x] 3.6 定义 Timeline 缺失或缺少 TimelinePlayer 时返回 `Failure`。
- [x] 3.7 定义 `OnStop` 释放运行实例绑定。
- [x] 3.8 定义 `OnReset` 清理运行实例并重置节点状态。

## 4. TimelinePlayer 上下文
- [x] 4.1 定义 `ITimelinePlayerProvider.GetTimelinePlayer()` 执行上下文接口。
- [x] 4.2 定义 Graph runtime context 通过 `ITimelinePlayerProvider.GetTimelinePlayer()` 提供 TimelinePlayer。
- [x] 4.3 定义 `TimelineNode` 从 Graph 执行上下文读取 `ITimelinePlayerProvider.GetTimelinePlayer()`。
- [x] 4.4 禁止在 `TimelineNode` 上保存场景 TimelinePlayer fallback 引用。
- [x] 4.5 缺失 TimelinePlayer 时提供明确错误路径。

## 5. 状态行为 Graph 集成
- [x] 5.1 允许 SMNode 下钻的行为 Graph 创建 `TimelineNode`。
- [x] 5.2 确认 `IdleGraph -> TimelineNode -> Timeline asset` 可以作为状态具体行为。
- [x] 5.3 确认 `TimelineNode` 不参与状态机同层 Transition。

## 6. 验证和文档
- [x] 6.1 扩展验证，报告 `TimelineNode` 缺失 Timeline 引用。
- [x] 6.2 记录 Taco 原有 `Timeline -> TreeClip -> TimelineRunningTree` 链路仍保留。
- [x] 6.3 记录新增 `Graph -> TimelineNode -> Timeline` 链路。
- [x] 6.4 记录当前不做运行时编译导出。

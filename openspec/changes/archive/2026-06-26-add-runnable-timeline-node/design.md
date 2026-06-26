## Context
当前 Taco Timeline 有两条已有事实：

```text
Timeline : ScriptableObject
  Tracks / Clips
  Init()
  Bind(TimelinePlayer)
  Evaluate(deltaTime)
  Unbind()

TreeTrack / TreeClip
  TreeClip.Evaluate(deltaTime)
    -> TimelineRunningTree.UpdateTree(deltaTime)
```

这说明 Taco 原本支持“Timeline 驱动 Tree”。当前缺的是 Graph/StateBody 中的可执行 Timeline 节点，也就是“Graph 驱动 Timeline”。

## Goals
- 提供 `TimelineNode : RunnableNode`，让行为 Graph 可以 tick Timeline。
- 复用现有 `TimelineReferenceModule` 和 `NodeModule` 字段扫描链路。
- 保持 `Timeline` 资产为纯数据/播放资产，不继承节点生命周期。
- 让 `TimelineNode` 能在 SMNode 下钻的行为 Graph 中播放 Timeline。
- 保留现有 `TreeClip -> TimelineRunningTree` 方向。
- 第一阶段只打通单 Timeline 播放生命周期，不做 Timeline 状态特化节点。

## Non-Goals
- 不新增 `TimelineStateNode`。
- 不让 `Timeline` 继承 `RunnableNode`。
- 不重写 TimelinePlayer、Track、Clip 架构。
- 不删除 `TreeTrack/TreeClip/TimelineRunningTree`。
- 不实现运行时编译导出。
- 不引入旧 Locomotion/FootPhase SO 配置。

## Decisions

### Decision: TimelineNode 是可执行包装器，Timeline 是资产
`TimelineNode` 位于 Graph 内，继承 `RunnableNode`，负责把 Graph tick 转换成 Timeline 播放。`Timeline` 仍是 `ScriptableObject`，负责 tracks/clips 和 `Evaluate`。

Alternatives considered:
- 让 `Timeline` 继承 `RunnableNode`：会把资产数据和图节点生命周期混在一起，破坏 Taco 原有 Timeline 资产模型。
- 继续只用 `TimelineReferenceNode : BaseNode`：只能引用，不能执行，无法放进 IdleGraph/WalkGraph 里被 tick。

### Decision: 复用 TimelineReferenceModule
`TimelineNode` 通过现有 `TimelineReferenceModule` 引用 Timeline 资产。该 module 继续走 `NodeModule` 和 Inspector 字段扫描，不新增并行引用系统。

Alternatives considered:
- 在 `TimelineNode` 上直接声明 Timeline 字段：短期简单，但会绕开当前模块化字段扫描方向。

### Decision: Runnable 生命周期映射到 Timeline 生命周期
第一阶段映射如下：

```text
TimelineNode.OnStart
  Timeline.Init()
  Timeline.Bind(player)

TimelineNode.OnUpdate
  Timeline.Evaluate(deltaTime)
  TimelinePlayer.EvaluatePlayableGraph(deltaTime)
  Timeline.Time < Timeline.Duration -> Running
  Timeline.Time >= Timeline.Duration -> Success

TimelineNode.OnStop
  Timeline.Unbind()

TimelineNode.OnReset
  Timeline.Time = 0
```

如果 Timeline 缺失，节点返回 `Failure`，并由验证报告缺失引用。

### Decision: TimelinePlayer 来源先走 Graph 执行上下文
`Timeline.Bind` 需要 `TimelinePlayer`。第一阶段定义正式的 `ITimelinePlayerProvider.GetTimelinePlayer()` 执行上下文接口，由 Graph runtime context 提供 TimelinePlayer；如果当前执行上下文没有实现该接口，节点返回 `Failure` 并报告明确错误，不新增 fallback 配置。

Alternatives considered:
- 在 `TimelineNode` 上配置 TimelinePlayer 引用：会让资产节点绑定场景对象，不利于复用。
- 自动创建 TimelinePlayer：隐藏依赖，容易制造播放图生命周期泄漏。

### Decision: TimelineNode 使用运行时 Timeline 实例
`Timeline` 资产包含 mutable `Time/Binding/TimelinePlayer` 状态。`TimelineNode` 播放时必须从引用资产创建运行实例，并在本次生命周期内只操作运行实例，避免多个节点或多个角色共享同一个 Timeline 资产播放状态。

Alternatives considered:
- 直接播放引用资产：实现最少，但多个节点共享资产时会互相覆盖 `Time`、`Binding` 和事件状态。
- 编辑器和运行时分两套规则：容易让预览和运行结果不一致。

### Decision: 保留 Taco 原有 Timeline 驱动 Tree
`TreeTrack/TreeClip/TimelineRunningTree` 保留。新的 `TimelineNode` 只补 Graph 驱动 Timeline，不替换 Timeline 内嵌 Tree 的能力。

## Data Flow
```text
IdleGraph : Behavior Graph
  TimelineNode : RunnableNode
    TimelineReferenceModule -> IdleTimeline.asset

运行：
  IdleGraph tick TimelineNode
  TimelineNode Bind Timeline
  TimelineNode Evaluate Timeline
  TimelineNode 返回 Running/Success/Failure
```

原有链路仍然存在：

```text
TimelinePlayer
  Timeline.Evaluate()
    TreeClip.Evaluate()
      TimelineRunningTree.UpdateTree()
```

## Risks / Trade-offs
- `Timeline.Bind` 需要 `TimelinePlayer`，所以 Graph 执行上下文需要明确提供该对象。
- `TimelineNode` 每次播放要创建运行实例，生命周期管理必须明确，否则会泄漏绑定和 playable 状态。
- 第一阶段只返回播放完成的 Success；循环、打断、暂停、同步外部时间以后再扩展。

## Open Questions
- 无。

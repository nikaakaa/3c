# Change: 新增可执行 TimelineNode

## Why
Taco 原本已有 Timeline 资产、TimelinePlayer、Track/Clip，以及 `TreeClip -> TimelineRunningTree` 这条“Timeline 驱动 Tree”的链路。但当前 Graph/StateBody 里只有 `TimelineReferenceNode : BaseNode` 引用壳，不能被行为 Graph tick，也不能向父级返回 `Running/Success/Failure`。

在统一递归 SMNode 图创作中，Idle/Walk 的下钻行为 Graph 需要能直接放一个可执行 Timeline 节点来播放动画、FootPhase、事件轨道等 Timeline 内容。因此需要补上“Graph 驱动 Timeline”的节点包装器，同时保留 Taco 原本的“Timeline 驱动 Tree”能力。

## What Changes
- 新增或改造 `TimelineNode : RunnableNode`，作为 Graph 中可执行的 Timeline 节点。
- `Timeline` 资产继续作为 `ScriptableObject` 数据资产，不继承 `RunnableNode`。
- `TimelineNode` 通过 `TimelineReferenceModule` 引用 Timeline 资产。
- `TimelineNode` 将 `RunnableNode` 生命周期映射到 Timeline 的 `Bind/Evaluate/Unbind/Reset`。
- `TimelineNode` 可以在行为 Graph 和 SMNode 下钻的状态行为 Graph 中创建。
- 保留 Taco 原有 `TreeTrack/TreeClip/TimelineRunningTree` 链路，不将其迁移成状态机节点。
- 不新增 `TimelineStateNode`，不把 Timeline 本身当状态层级节点。

## Impact
- Affected specs: `taco-runnable-timeline-node`
- Related changes: `add-unified-sm-node-authoring`
- Affected code:
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/Tree/TimelineNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/Timeline.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/TimelinePlayer.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/Tree/Timeline.Tree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/RunnableNode.cs`
  - Taco Graph editor node creation/search/inspector UI

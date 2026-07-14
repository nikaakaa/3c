# Proposal: 删除 TimelineNode 输出控制流

## Why

`TimelineNode` 当前声明了 `Output` 控制流 port，并在 Timeline 播放成功后继续 tick `m_Child`。这让一个看起来属于 `Base/Action/Timeline` 的普通动作节点同时承担了顺序编排职责。

当前项目口径中，`TimelineNode` 的职责是 Graph 驱动 Timeline：节点引用 Timeline 资产，通过正式执行上下文提交播放请求、查询状态并返回 `Running`、`Success` 或 `Failure`。行为图的后续编排应该由父级 `CompositeNode`、`DecoratorNode`、`SubTreeNode` 或状态行为结构表达，不应该由 Timeline 播放节点私下再挂一个 child。

保留该输出会带来几个问题：

- 作者会误以为 TimelineNode 既是 Action 又是 Sequence 容器，节点心智不统一。
- 同一条行为链可以通过父级 composite 和 TimelineNode 输出两种方式继续，形成分裂路径。
- TimelineNode 播放完成后的后续行为被隐藏在节点内部 child 上，不利于状态行为图调试和排序。
- 当前 `btsmtl-runnable-timeline-node` spec 没有要求输出控制流，反而强调 TimelineNode 是普通可执行节点和状态内部行为节点。

## What Changes

- `TimelineNode` 只保留输入控制流 port。
- `TimelineNode` 不再声明、持久化或解析输出控制流 port。
- `TimelineNode` 播放成功后直接返回 `Success`。
- 行为后续执行由父级图结构决定，不再由 `TimelineNode` 的 child 决定。
- 清理当前资产中 `TimelineNode` 的空输出 GUID 序列化残留。

## Non-Goals

- 不修改 `SubTreeNode`、`DecoratorNode`、`CompositeNode`、`RootNode` 或 `TriggerNode` 的输出控制流语义。
- 不删除 Timeline 内嵌 Tree 链路，即 `TreeTrack` / `TreeClip` / `TimelineRunningTree` 继续保留。
- 不新增 fallback、兼容端口、隐藏桥接字段或自动迁移旧输出边。
- 不引入新的 Timeline 状态节点或 Timeline 播放器。
- 不编写测试；由用户在 Unity 端做端到端验证。

## 当前代码事实

- `TimelineNode` 位于 `Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts/Tree/TimelineNode.cs`。
- `TimelineNode` 当前显式声明 `[Input("Input")]` 和 `[Output("Output", PortCapacity.Single)]`。
- `BaseNode.GetFlowPortDeclarations` 通过读取 `InputAttribute` / `OutputAttribute` 生成控制流端口，所以该输出不是默认生成。
- `TimelineNode.ResolveFlowLinks` 当前会查找 `"Output"` edge 并缓存 `m_Child`。
- `TimelineNode.OnUpdate` 当前在播放完成后执行 `m_Child.UpdateNode()`，没有 child 时返回 `Success`。
- 当前资产中只找到一个正式 `TimelineNode` 实例，位于 `CorinPlayableRootTree.asset`，其 `m_OutputEdgeGUID` 为空，且该节点没有作为输出边起点。

## 决策和 Tradeoff

### 方案 A：保留 TimelineNode 输出控制流

- 优点：作者可以直接把 Timeline 节点串到下一个 Runnable 节点，实现局部顺序链。
- 缺点：TimelineNode 变成 Action 与 Sequence 的混合体；父级行为图和节点内部 child 都能表达后续执行，图语义分裂。
- 业务取舍：短期方便拖线，但不利于求职 demo 展示清晰的动作 authoring 模型，后续讲解 Graph、State、Timeline 分工时容易自相矛盾。

### 方案 B：删除 TimelineNode 输出控制流

- 优点：TimelineNode 只负责播放请求和返回状态，后续编排统一交给行为图结构；节点职责更窄，调试路径更直。
- 缺点：如果未来确实需要“播放 Timeline 后自动接一个节点”，作者需要在父级 composite/subtree 中表达该顺序。
- 业务取舍：更符合当前项目“干净统一链路”的目标，也更适合展示 Gameplay 客户端中 Timeline 作为动作表现事实来源，而不是隐藏控制流容器。

本 proposal 选择方案 B。

## 与现有 Spec 的关系

- `btsmtl-runnable-timeline-node` 已要求 `TimelineNode : RunnableNode` 作为 Graph 中请求播放 Timeline 的节点，本变更收窄其职责，不改变 Graph 驱动 Timeline 的主线。
- `btsmtl-runnable-timeline-node` 已要求 TimelineNode 生命周期映射 Timeline 播放请求生命周期，本变更让播放完成直接返回 `Success`，符合该生命周期口径。
- `btsmtl-runnable-timeline-node` 没有要求 TimelineNode 暴露输出控制流；当前实现多出的输出 port 与 spec 不一致，本变更将实现收回到 spec 口径。
- `openspec/project.md` 强调不做分裂路径和临时桥接，本变更删除 TimelineNode 内部 child 路径，避免与父级行为图编排并存。

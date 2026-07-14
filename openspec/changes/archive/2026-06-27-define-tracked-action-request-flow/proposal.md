# Proposal: 定义 TrackedActionRequest 到 ActionInstance 的配置闭环

## Why

`refactor-ability-to-action-instance-network-policy` 已经把 UE/GAS 式 Ability 执行体收口为 `ActionProfile`、`ActionRuntime` 和 `ActionInstance`，但当前口径仍有一个关键缺口：

```text
Graph 到底如何产生一次 ActionInstance？
作者到底在哪里设置动作身份？
Timeline 或非 Timeline 动作如何挂到同一套实例归属？
```

上一份变更中出现过 `BeginTrackedAction Node Inspector` 这类说法，容易被误解为“默认需要特殊 Action node”。这会重新滑向旧 `ActionModule` 或 node membership table。当前项目必须避免这种回潮。

## What Changes

- 定义 `TrackedActionStartRequest` / `TrackedActionEndRequest` 作为 Graph 到 ActionRuntime 的正式请求语义。
- 明确 `ActionInstance` 是动作事务实例，不是 Graph、Tree、Timeline、Ability 或节点身份。
- 让 `CharacterPipelineDefinition` 配置 ActionProfile 列表，由 pipeline 初始化时注册到 `ActionRuntime`。
- 让 `CharacterGraphContext` 暴露 tracked action request service，Graph 使用同一条 context/service 模式提交请求。
- 定义 Graph authoring UI：普通 request 提交入口，配置 ActionProfile/ActionId、源输入 request、target key 和实例输出，不新增 ActionModule。
- 定义 Timeline window UI：只配置 WindowType、WindowId、时间和参数，不保存完整网络策略。
- 定义 Runtime Debug：按 input request -> tracked action request -> ActionInstance -> facts -> confirm/reject/correction 展示链路。

## Current Facts

当前代码事实：

- `CharacterGraphContext` 已有 `HasInputRequest`、`TryConsumeInputRequest` 和 `RequestTimelinePlayback`。
- `TimelineNode` 通过 `BaseGraph.User` 获取 `ITimelinePlaybackService`，属于正式 context service 模式。
- `CharacterPipelineDefinition` 目前只配置 RootTree 和 AnimationLayers，还没有 ActionProfile 列表。
- `Character/Action` 已有 `ActionProfile`、`ActionRuntime`、`ActionInstance`、`ActionStartRequest`、fact contract，但没有接入 pipeline，也没有闭环 UI。
- `ActionAuthoringContracts` 当前含有 `TrackedActionNodeContract`，命名会暗示特殊 node，需要收口为 request authoring contract。

需要补齐的是：

```text
Graph 通过正式 request/service 提交动作事务
ActionRuntime 接受 request 后生成 ActionInstance
Timeline / 非 Timeline 逻辑 / Motion / Combat / Cue 产出的事实挂 ActionInstanceId
作者 UI 可以完整配置和观察这条链路
```

## 非目标

- 不实现完整网络层、服务端裁决、combat rewind 或 correction smoothing。
- 不新增 `AbilityTree`、`ActionTree`、`NetworkedTree` 或特殊动作图类型。
- 不恢复旧 `ActionModule`、`ActionSubTreeNode`、节点 action identity 或 BBB 状态主线。
- 不把 Timeline 作为 ActionInstance 的唯一来源。
- 不要求所有 graph 流程都有 ActionInstance；locomotion、相机、普通状态逻辑可以保持普通 graph。

## 业务取舍

选择 Graph 提交 request：

- 好处：Graph 仍是唯一玩法编排层，适合格挡反击、蓄力、持续格挡、交互等非 Timeline 动作。
- 好处：ActionInstance 只作为网络和事实归属事务，不污染 Tree/SubTree/StateNode 结构语义。
- 好处：Timeline 只是事实来源之一，Timeline 动作和非 Timeline 动作共用同一套网络追踪模型。
- 代价：作者需要理解“Graph 编排”和“Tracked Action 事务”是两个维度，必须用 UI 和 debug 明确展示。

不选择 Tree/Node 标记：

- 好处：作者看结构时直观。
- 代价：会把“这个节点属于某个动作”的静态归属和“这一次动作启动”的运行时事务混在一起，回到旧 ActionModule。

不选择 Timeline 自动创建 ActionInstance：

- 好处：攻击动画类动作设置简单。
- 代价：格挡、蓄力、交互、持续状态和纯 Graph 事实无法自然表达，Timeline 会越权成为动作根。

## 影响范围

- `Character/Action`：请求命名、ActionRuntime 接口、ActionInstance debug 数据。
- `Character/Pipeline/Runtime`：PipelineDefinition、Pipeline 初始化、GraphContext 构造。
- `Character/Pipeline/Graph`：GraphContext 暴露 tracked action service。
- BTSMTL Graph authoring：新增或调整普通 request submit authoring UI。
- BTSMTL Timeline authoring：window clip inspector 收口到 WindowType/WindowId/参数。
- Runtime debug：显示 ActionInstance 和相关事实链路。

## 需要指出的现行矛盾

- `refactor-ability-to-action-instance-network-policy` 中 `BeginTrackedAction Node Inspector` 的表述过早假设了特殊 node。该表述应被本变更替换为“Graph tracked action request authoring”，实现可以使用普通 request submit 节点或已有 service 调用机制，但不得把节点或 subtree 标记成 action。
- 当前 `ActionStartRequest` 命名偏泛，和输入 request、Graph request 的关系不够清楚。实现阶段应重命名或替换为 `TrackedActionStartRequest`，并增加 source request、target key、source graph identity 等字段。

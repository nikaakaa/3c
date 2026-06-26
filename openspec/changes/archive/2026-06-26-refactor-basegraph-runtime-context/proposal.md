# Change: 统一 BaseGraph 运行上下文

## Why
当前 Taco 图运行链路已经出现职责错位：

- `BaseGraph` 已经是节点、边、属性端口和 `User` 的统一归属，但没有统一 `DeltaTime`。
- `RunnableTree` 仍然独占 `DeltaTime`，导致 `TimelineNode` 必须判断 `Owner is RunnableTree` 才能播放。
- `StateMachineGraph` 不继承 `RunnableTree`，由 `StateMachineGraphRuntime` 解释执行，但该解释器没有把本帧时间写回 Graph。
- `StateMachineNode` 下钻子 Graph 时存在 `Owner?.User ?? Owner` 这类隐式上下文兜底，和项目“不做 fallback 配置”的规则冲突。
- `TreeRunner` 当前用 `InitTree(this)` 注入上下文，Timeline 播放需要的 `ITimelinePlayerProvider` 没有正式配置入口。

这会让 Timeline、StateMachine、Tree 看起来能嵌套，但运行时上下文不是同一条链路。后续接动作管线时，Timeline 播放、状态机递归、行为树 tick 会继续依赖特化类型判断，形成新的分裂路径。

## What Changes
- `BaseGraph` 承担统一运行上下文：`User`、`DeltaTime` 和类型化上下文读取能力。
- `BaseGraph` 不承担执行生命周期：不新增 `Running`、`State`、`UpdateTree`、`ResetTree`，也不直接 tick 节点。
- `RunnableTree` 继续承担 Tree 生命周期，但 `UpdateTree(deltaTime)` 必须把时间写入继承自 `BaseGraph` 的上下文。
- `StateMachineGraphRuntime.Update(deltaTime)` 必须把时间写入当前 `StateMachineGraph`，再解释入口、AnyState、active state 和 transition。
- `StateMachineNode` 读取 `Owner.DeltaTime`，并把父 Graph 的正式 `User` 原样传给下钻 Graph，不再使用 `Owner` 自身作为 fallback 上下文。
- `TimelineNode` 读取 `Owner.DeltaTime`，不再要求 `Owner` 是 `RunnableTree`。
- `TimelineNode` 通过 `Owner.User` 中的正式 provider 获取 `TimelinePlayer`，不在节点或场景中自动寻找 fallback。
- `TreeRunner` 提供正式 runtime user 配置，并把该对象传入 `InitTree()`；没有配置就传空，缺失依赖由节点运行失败暴露。
- 继续保持 `TimelineNode` 不直接作为 `StateMachineGraph` 同层状态创建；Timeline 仍通过状态行为 Graph 接入。
- 不新增 `IRunnableGraph`、Graph adapter、Workbench port、并行端口注册表或兼容旧路径。

## Impact
- 影响的规格：
  - `taco-graph-core`
  - `taco-runnable-timeline-node`
  - `taco-sm-node-authoring`
- 影响的代码：
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/BaseGraph.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/RunnableTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Tree/OneRootTree.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/TreeRunner.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/StateMachineGraphRuntime.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Custom/StateMachineNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Custom/SubTreeNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/Tree/TimelineNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/TimelineRunningTree.cs`

## Conflicts To Resolve
- 当前 `taco-graph-core` 写着 `DeltaTime` 属于 `RunnableTree`，并要求运行时状态不下沉到 `BaseGraph`。本变更会修改这条要求：`BaseGraph` 可以保存运行上下文，但不能保存执行生命周期。
- 当前 `taco-sm-node-authoring` 写着不引入统一图运行接口。该要求继续保留；本变更只统一上下文，不新增 `IRunnableGraph`。
- 当前 `taco-runnable-timeline-node` 已要求 Timeline 使用当前 Graph deltaTime。本变更把“当前 Graph deltaTime”的来源明确为 `BaseGraph.DeltaTime`。

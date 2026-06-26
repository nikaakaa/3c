# Design: 统一 Graph 运行上下文

## 目标链路
目标链路是：

```text
TreeRunner
  -> InitTree(runtimeUser)
  -> RunnableTree.UpdateTree(deltaTime)
  -> BaseGraph.DeltaTime
  -> RootNode / RunnableNode
  -> StateMachineNode
  -> StateMachineGraphRuntime.Update(deltaTime)
  -> StateMachineGraph.DeltaTime
  -> active StateMachineNode
  -> 状态行为 Graph / TimelineNode
  -> Timeline.Evaluate(Owner.DeltaTime)
```

这条链路让 Tree、StateMachine、Timeline 共用同一个 Graph 上下文来源，但仍然保留各自的业务语义。

## 职责边界
`BaseGraph` 是图结构和运行上下文底座：

- 节点集合、边集合、属性边集合、暴露属性集合。
- GUID 映射。
- `User`。
- `DeltaTime`。
- 上下文读取辅助能力。

`BaseGraph` 不是执行器：

- 不新增 `UpdateGraph()`。
- 不新增 `Running`。
- 不新增 `State`。
- 不直接 tick 节点。

`RunnableTree` 继续是 Taco 行为树执行器：

- 保留 `Running`、`State`、`UpdateTree()`、`ResetTree()`。
- `UpdateTree(deltaTime)` 第一件事是写入 `BaseGraph.DeltaTime`。
- 其它树节点仍按 Taco 原生命周期执行。

`StateMachineGraphRuntime` 是状态机解释器：

- `StateMachineGraph` 仍不继承 `RunnableTree`。
- 每次 `Update(deltaTime)` 写入 `StateMachineGraph.DeltaTime`。
- 根据进入来源选择 `Root` 或 `Enter`。
- 只 tick 当前 active `StateMachineNode`。

`TimelineNode` 是普通可执行节点：

- 它不关心 owner 是不是 `RunnableTree`。
- 它只要求 owner graph 已经有本帧 `DeltaTime`。
- 它从 owner graph 的 `User` 中读取正式 `ITimelinePlayerProvider`。

## 不选的路径

### 继续让 TimelineNode 依赖 RunnableTree
业务取舍：短期改动最小，但会让 Timeline 无法干净地跟随状态机递归图。状态机图不是 `RunnableTree`，Timeline 一旦通过状态行为 Graph、子状态机或后续管线被间接 tick，就会继续写类型判断。

结论：不选。

### 让 StateMachineGraph 继承 RunnableTree
业务取舍：可以复用 `UpdateTree(deltaTime)`，但会把状态机同层 transition、active state、Root/Enter/AnyState/Exit 强行塞进行为树生命周期。状态机图不是树，它是被 `StateMachineNode` 解释的层级状态图。

结论：不选。

### 新增 IRunnableGraph 或 Graph adapter
业务取舍：抽象更完整，但当前第一阶段只需要统一上下文，不需要统一执行入口。新增接口会扩大迁移面，也会和当前 `taco-sm-node-authoring` 的“不引入图运行接口”要求冲突。

结论：不选。

### TreeRunner 自动找 TimelinePlayer
业务取舍：用户配置更少，但会把 Timeline 业务依赖偷偷塞进 TreeDesigner 基础运行器，并产生场景对象 fallback。

结论：不选。运行上下文必须通过正式字段配置。

### TimelineNode 直接进入 StateMachineGraph 同层状态
业务取舍：看起来少一层状态行为 Graph，但会破坏“`StateMachineNode` 是唯一普通状态节点”的状态机语义。Timeline 是状态内部行为，不是同层 active state。

结论：不选。

## 数据和运行边界
- `User` 是外部运行上下文，不序列化到 graph asset。
- `DeltaTime` 是本帧运行上下文，不序列化到 graph asset。
- 子 Graph 运行实例继承父 Graph 的正式 `User`。
- 子 Graph 不使用父节点、父 Graph 或 `TreeRunner` 自身作为隐式 fallback。
- 缺失 `TimelinePlayer` 是配置错误，运行时返回 Failure 并报告具体节点。

## 迁移后的判断标准
- 搜索 `Owner is RunnableTree` 时，不应再出现 Timeline 或通用上下文读取。
- 搜索 `InitTree(Owner?.User ?? Owner)` 时，不应再出现该 fallback。
- `StateMachineGraphRuntime.Update(deltaTime)` 能把时间写入当前 `StateMachineGraph`。
- `TimelineNode` 的播放时间来源只有 `Owner.DeltaTime`。

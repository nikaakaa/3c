# Design

## 目标模型

本轮把图分成三层：

```text
普通行为图 / OneRootTree
  RootNode
    -> StateMachineNode
         graph = LocomotionGraph

StateMachineGraph
  Enter / AnyState / Exit
  StateNode: Idle
  StateNode: Walk
  ValueNode: Bool 条件

StateNode 下钻普通 SubTree
  RootNode
    -> TimelineNode / ActionNode / CompositeNode / StateMachineNode

StateNode 下钻 StateBehaviorSubTree
  OnEnter
    -> ActionNode / TimelineNode / CompositeNode
  RootNode
    -> TimelineNode / ActionNode / CompositeNode / StateMachineNode
  OnExit
    -> ActionNode / TimelineNode / CompositeNode
```

`StateMachineNode` 是父级行为图中的入口节点。它不在状态机图内创建，也不表达一个状态。

`StateNode` 是状态机图内的普通状态。它只负责状态边界、Transition 端点和状态行为 `SubTree` 引用，不在状态机图本层直接连行为节点。

`RootNode` 是 Taco 行为图入口。它只存在于普通行为图或 State 下钻后的状态行为 `SubTree` 中，不存在于 `StateMachineGraph` 中。

## Port 语义

状态转换只使用同一套 Taco `BaseEdge` 和状态机 flow port：

```text
Enter.StateOut      -> StateNode.StateIn
AnyState.StateOut   -> StateNode.StateIn / Exit.StateIn
StateNode.StateOut  -> StateNode.StateIn / Exit.StateIn
```

状态具体行为不再通过 `StateNode.Behavior -> RunnableNode.Input` 表达。普通 `SubTree` 内部继续使用 Taco 原生行为流；`StateBehaviorSubTree` 在普通行为流之外额外提供 `OnEnter` 和 `OnExit` 入口：

```text
OnEnter.Output      -> RunnableNode.Input
RootNode.Output     -> RunnableNode.Input
OnExit.Output       -> RunnableNode.Input
RunnableNode.Output -> RunnableNode.Input
```

## 创建规则

普通行为图：

- 允许创建 `StateMachineNode`。
- 允许创建普通 runnable、Timeline、Tree 引用和值节点。
- 不允许创建 `StateNode`。
- 不允许创建状态机控制节点。

`StateMachineGraph`：

- 允许创建 `Enter`、`AnyState`、`Exit`，且每类只能一个。
- 允许创建 `StateNode`。
- 允许创建 `ValueNode` 作为 Transition 条件辅助。
- 不允许创建 `StateMachineNode`。
- 不允许创建 Taco 原生 `RootNode`。
- 不允许创建普通 runnable、Timeline、Tree 行为节点。

State 下钻普通 SubTree：

- 使用现有 `SubTree`。
- 固定包含 Taco 原生 `RootNode`。
- 不包含 `OnEnter` 或 `OnExit`。
- 可以创建普通行为节点、Timeline 节点、Tree 引用节点和 `StateMachineNode`。
- 不创建 `StateNode`。

State 下钻 StateBehaviorSubTree：

- 继承 `SubTree`。
- 固定包含 `OnEnter`、Taco 原生 `RootNode` 和 `OnExit`。
- 可以创建普通行为节点、Timeline 节点、Tree 引用节点和 `StateMachineNode`。
- 不创建 `StateNode`。

## Runtime 解释

`StateMachineGraphRuntime` 只从 `Enter` 开始解析初始状态。

运行顺序：

1. 没有 active state 时，从 `Enter.StateOut` 读取初始 Transition。
2. 新 active state 引用普通 `SubTree` 时，直接 tick 该 `SubTree` 的 `RootNode`。
3. 新 active state 引用 `StateBehaviorSubTree` 时，首帧先 tick `OnEnter`，完成后再 tick `RootNode`。
4. 有 active state 时，先检查 `AnyState` transition。
5. tick 当前 active `StateNode`。
6. 检查当前 active `StateNode.StateOut` transition。
7. 命中 Transition 时，当前 state 引用 `StateBehaviorSubTree` 才 tick `OnExit`。
8. 普通 `SubTree` 无 `OnExit`，直接切换到目标 `StateNode`，或命中 `Exit` 时本层 graph 返回 `Success`。

`StateNode` tick 行为：

- 如果引用普通 `SubTree`，active 时调用 `SubTree.UpdateTree(deltaTime)`。
- 如果引用 `StateBehaviorSubTree`，进入状态时先调用 `UpdateStateEnter(deltaTime)`。
- 如果引用 `StateBehaviorSubTree`，active 时调用 `UpdateStateRoot(deltaTime)`。
- 如果引用 `StateBehaviorSubTree`，离开状态前由状态机 runtime 调用 `UpdateStateExit(deltaTime)`。
- 如果没有 `SubTree`，保持 `Running`，等待 Transition。
- 如果需要层级状态机，在 `SubTree` 内创建 `StateMachineNode`。

## Tradeoff

### 保留 Root 在 StateMachineGraph

业务取舍：顶层状态机入口和子状态机入口可以区分，但状态机结构图会同时出现 `Root`、`Enter` 和状态节点，用户很容易把“当前层入口”和“下钻行为入口”混为一谈。当前需求明确要把 Root 放到 State 视图，所以不采用。

### 保留 StateNode inline behavior

业务取舍：简单状态可以少建一个行为图，但状态图里会混进 Transition 线和行为执行线，视觉和数据边界继续变脏。当前目标是先把结构图清干净，所以不采用。

### 强制 State 行为都使用 StateBehaviorSubTree

业务取舍：生命周期语义最统一，但很多状态只是简单 Root 行为，不需要 enter/exit。全部强制使用 `StateBehaviorSubTree` 会让简单状态多两个空入口。本轮不强制，允许 `StateNode` 引用普通 `SubTree`。

### 普通 SubTree 也承载 OnEnter/OnExit

业务取舍：类型更少，但普通子树、TreeClip、SubTreeNode 也会被状态生命周期污染。当前目标是保持图类型边界干净，所以不采用。

## 背景
Taco 已经提供 `BaseTree`、`BaseNode`、`RunnableNode`、`NodeModule`、`PropertyPort` 和 edge 序列化链路。当前目标不是另起 Workbench，也不是恢复旧状态配置，而是在 Taco 主链路上把状态机图创作收口。

之前的 `StateMachineNodeRole(State/Entry/Exit)` 会把普通 State 和图控制点混在一个枚举里，导致普通状态节点出现它不该拥有的控制语义。这个方向需要废弃。

## 目标
- 普通状态节点只有 `StateMachineNode`。
- `StateMachineNode` 没有 `Root/Enter/AnyState/Exit` 控制模块。
- `Root/Enter/AnyState/Exit` 是状态机图层级控制点，不是状态。
- `Root` 是行为树进入当前层状态机图的入口源，负责选择当前层初始 active state。
- `Enter` 是父级状态下钻进入下一层状态机图的入口源，负责选择下一层初始 active state。
- 控制点使用 `StateMachineControlNode` 基类和四个具体控制节点表达，保持直观和可创建。
- 每层 `StateMachineGraph` 都有清晰入口、全局跳转源和出口。
- Transition 保持 edge 语义，条件复用现有 Bool property port 主链路。
- 当前不做运行时数据编译导出。

## 非目标
- 不迁回旧 Locomotion、Action、FootPhase SO/config 数据。
- 不新增 `StateMachineTree`。
- 不把 `GraphProfile` 或 `Behavior` 图级字段塞进所有 `BaseTree`。
- 不新增并行 port registry。
- 不新增 `SMPort`、`TransitionPort` 或第二套 edge 类型。
- 不新增 `TransitionNode`。
- 不把 Timeline、Animation、FootPhase 做成状态机特化状态节点。
- 不把 `Root/Enter/AnyState/Exit` 模块挂到普通 `StateMachineNode` 上。
- 不为了控制节点强行新增 `StateMachineControlModule`。
- 不让 `TimelineNode` 直接成为 `StateMachineGraph` 同层状态节点。

## 决策

### 决策：StateMachineNode 只表达普通状态
`StateMachineNode` 是状态机图里的普通状态节点，也是父级行为图进入下一层 Graph 的递归边界。它继续继承或接入 `RunnableNode` 生命周期，并通过 `ScopedGraphReferenceModule` 引用下一层 Graph。

普通状态不携带 `Root/Enter/AnyState/Exit` 控制模块，也不能通过右键菜单切换成控制节点。

备选方案：
- 角色枚举：实现快，但会污染普通状态节点。
- 每个业务状态一个节点类：会把旧 Locomotion/Idle/Walk 重新写回 C# 继承树。
- 全 WorkbenchNode 化：方向大，但当前成本过高，会同时牵动 Timeline、BT、Action、Inspector 和 runtime。

### 决策：Root/Enter/AnyState/Exit 使用控制节点基类和具体节点
状态机控制点使用一个共同基类和四个具体节点表达：

```text
StateMachineControlNode : BaseNode
  StateMachineRootNode
  StateMachineEnterNode
  StateMachineAnyStateNode
  StateMachineExitNode
```

其中 `StateMachineRootNode` 是行为树进入当前层状态机图时使用的入口源。它和 Taco `RootNode` 的区别是：Taco `RootNode` 会作为行为树生命周期根 tick child；状态机 `Root` 只作为 `StateMachineGraphRuntime` 从行为树进入本层图时的解释入口，不持续 tick，也不代表普通状态。

`StateMachineEnterNode` 是父级状态下钻进入下一层状态机图时使用的入口源。它不从当前层 `Root` 接收输入，也不需要 input port；它通过自己的出边选择被下钻图的初始 active state。这样 Root 负责行为树进入当前层，Enter 负责状态下钻进入下一层，两者不再混用。

编辑器在节点搜索里显示 `Root`、`Enter`、`AnyState`、`Exit` 四个创建项，底层创建对应具体节点。显示名、颜色和基础端口由节点类型的 attribute 表达，验证和 runtime 通过 `StateMachineControlNode` 基类及具体类型判断控制语义。

普通 `StateMachineNode` 不拥有这些控制语义；普通 `BaseTree` 也不能创建这些控制节点。

备选方案：
- 控制模块容器：抽象更统一，但第一阶段会牵动搜索预设、端口策略、Inspector 和运行时判断；当前业务只需要三个稳定控制点，收益不够。
- 图级字段 `InitialStateNodeGuid`：数据少，但可视化弱，而且无法区分行为树进入当前层和父状态下钻进入下一层。

### 决策：StateMachineGraph 是状态机图资产边界
`StateMachineGraph : BaseTree` 表达状态机图资产。普通 `BaseTree` 保持 Taco 原有语义，不序列化状态机字段。

`StateMachineGraph` 不继承 `RunnableTree`。它不是被父图直接 `UpdateTree()` 的运行树，而是由父级 `StateMachineNode` 创建或引用后交给 `StateMachineGraphRuntime` 解释执行。这样状态机图本身保持状态机语义，父图只看到 `StateMachineNode` 的 `Running/Success/Failure`。

`StateMachineGraph` 自己决定创建菜单、节点集合、验证规则和 runtime 解释。父 `StateMachineNode` 只保存 Graph 引用，不复制保存子 Graph 类型声明。

### 决策：上下文 flow port 声明，不新增 port 底层
第一阶段不改 Taco 原有 `RunnableTree -> RootNode -> Composite/Decorator -> RunnableNode` 控制流。`Input/Output` 仍然是 Taco 树节点的 flow port，`BaseEdge` 仍然是 flow edge 数据。

需要改变的是端口声明来源：编辑器不再直接把节点类型上的 `InputAttribute` / `OutputAttribute` 作为唯一真相，而是通过节点在当前 `Owner` graph 下的 flow port 声明生成端口。

默认节点继续把 class attribute 转成 flow port。只有需要上下文语义的节点覆写声明：

```text
StateMachineNode 位于普通 BaseTree / RunnableTree:
  Input

StateMachineNode 位于 StateMachineGraph:
  Input
  Output
```

这不是新的 port 系统。它仍然生成 Taco 原生 port view，仍然保存 `BaseEdge`，仍然由 `StateMachineGraphRuntime` 在状态机图中解释为 Transition。

备选方案：
- 直接给 `StateMachineNode` 写死 `Output`：实现最少，但普通行为图也会显示 Output，误导它可以在外层树里直接串后继节点。
- 拆 `StateNode` 和 `StateMachineNode` 两个类：端口静态清楚，但会把普通状态重新拆成特化节点，和“普通状态只用 SMNode”目标冲突。
- Graph 级 port policy：抽象更完整，但当前只需要修正少数节点上下文，过早上图级策略会扩大实现面。
- `IRunnableGraph`：运行入口更统一，但当前主体仍是 Taco 树，状态机只是内部解释器，第一阶段收益不够。
- `StateMachineGraph : RunnableTree`：能让状态机图直接 `UpdateTree()`，但会把状态机语义伪装成行为树 root tick，职责不清。

### 决策：每层 Graph 必须有 Root、Enter、AnyState、Exit
每一层 `StateMachineGraph` 必须且只能有：

```text
1 个 Root
1 个 Enter
1 个 AnyState
1 个 Exit
```

`Root` 表示行为树进入当前层状态机图的入口源，`Enter` 表示父级状态下钻进入下一层状态机图的入口源，`AnyState` 表示全局跳转源，`Exit` 表示本层完成出口。

命名取舍：实现类型使用 `StateMachineRootNode`，避免和 Taco 现有 `RootNode` 混淆；两者同名概念不同，不能复用 Taco `RootNode`。

新建 `StateMachineGraph` 第一阶段默认创建 `Root`、`Enter`、`AnyState` 和 `Exit`，但不自动连接 `Root -> Enter`。不自动创建第一个普通 `StateMachineNode`。普通状态应该由用户按业务命名创建，否则默认 Idle/State 会重新变成隐式业务假设。

`Root` 第一阶段要求至少一条出边，目标必须是当前层 `StateMachineNode`。`Enter` 不允许入边，第一阶段要求至少一条出边，目标也必须是当前层 `StateMachineNode`。多出边按 transition 条件和优先级解释；如果后续编辑体验证明入口应强制唯一，再作为独立规则收紧。

### 决策：Transition 是 edge 语义
Transition 不做成节点。状态机图内的 flow edge 在 `StateMachineGraph` 中被解释为 Transition。

合法端点：

```text
Root     -> StateMachineNode
Enter    -> StateMachineNode
AnyState -> StateMachineNode | Exit
State    -> StateMachineNode | Exit
```

非法端点：

```text
Exit -> 任意节点
任意节点 -> Root
任意节点 -> Enter
任意节点 -> AnyState
非状态机图节点参与 Transition
跨层连接父图或子图内部节点
```

`StateMachineNode` 在状态机图内必须有可连接的 `Output` flow port，否则 `State -> State` 和 `State -> Exit` 无法通过 UI 创建。

`StateMachineNode` 在普通行为图中不显示 `Output`。它在父级树里是一个可执行节点，父级组合节点负责控制后续执行；SMNode 自己不通过外层 Output 暗示“状态机完成后直接串下一个节点”。

### 决策：Transition 第一阶段只引用 Bool 端口
Transition 第一阶段支持：

```text
priority
optional condition node guid
optional condition bool port id
```

没有条件时表示无条件转换。复杂条件必须先由同层 Graph 中的普通节点或模块计算成 Bool，再由 Transition 引用。

`AnyState` 的 Transition 必须配置条件，避免每帧无条件抢占当前状态。

### 决策：Timeline 通过状态行为图接入
`TimelineNode : RunnableNode` 不直接作为 `StateMachineGraph` 同层节点创建。状态机同层只放普通 `StateMachineNode`、控制节点和值节点。Timeline 的接入方式是：

```text
StateMachineGraph
  StateMachineNode
    ScopedGraphReference -> RunnableTree / OneRootTree / TimelineRunningTree
      TimelineNode
```

当前 `TimelineNode` 依赖 `Owner is RunnableTree` 获取 `DeltaTime`，所以承载它的状态行为图必须是 `RunnableTree` 体系。这样状态机只负责切换状态，Timeline 负责状态内部表现和事件轨道。

`TimelineNode` 如果需要串联后续行为，也必须提供 `Output` flow port；没有输出时只能作为单段行为播放完成后返回 `Success`。

## 数据模型草图
```text
RootGraph : RunnableTree / BaseTree
  SMNode: Locomotion : StateMachineNode
    ScopedGraphReference -> LocomotionGraph

LocomotionGraph : StateMachineGraph
  Control: Root
  Control: Enter
  Control: AnyState
  Control: Exit

  State: Idle : StateMachineNode
    ScopedGraphReference -> IdleGraph

  State: Walk : StateMachineNode
    ScopedGraphReference -> WalkGraph

  Transition: Root -> Idle
  Transition: Enter -> Idle
  Transition: Idle -> Walk + Bool condition
  Transition: Walk -> Idle + Bool condition
  Transition: Idle -> Exit

IdleGraph : BaseTree / RunnableTree
  TimelineNode
  ActionNode
  ValueNode
  普通逻辑节点
```

## 生命周期草图
```text
父级 Graph tick Locomotion SMNode
  Locomotion 进入 LocomotionGraph
  LocomotionGraph 如果由行为树进入，则从 Root 激活 Idle
  LocomotionGraph 如果由父状态下钻进入，则从 Enter 激活 Idle
  每帧先检查 AnyState transition
  tick 当前 active StateMachineNode
  active StateMachineNode 下钻自己的 Graph
  检查 active State 的 transition
  如果 active State 的子 Graph 返回 Success 但没有命中 transition，则本层继续 Running 并保持当前 active State
  命中 State -> Exit 时，本层 StateMachineGraph 返回 Success
```

父级 Graph 只接收 `Locomotion SMNode` 的 `Running/Success/Failure`，不能直接 tick `IdleGraph` 或 `WalkGraph` 内部节点。

## 风险与取舍
- 三个具体控制节点会保留少量继承式类型判断，但控制点数量固定，语义清楚，第一阶段成本低。
- Transition 继续作为 edge，图会更清爽；代价是需要专用 edge inspector，否则编辑体验不直观。
- AnyState 强制条件会牺牲少量自由度，但可以避免无条件全局跳转造成状态机不可用。
- 当前不做编译导出，runtime 直接解释 Graph 数据；后续管线稳定后再讨论导出数据。

## 迁移计划
- 删除 `InitialStateNodeGuid` 路径。
- 删除 `StateMachineNodeRole` 和状态角色模块路径。
- 普通 `StateMachineNode` 保留图引用模块和可执行生命周期。
- 保留 `StateMachineControlNode` 基类与 `StateMachineRootNode`、`StateMachineEnterNode`、`StateMachineAnyStateNode`、`StateMachineExitNode` 四个具体控制节点。
- 新建 `StateMachineGraph` 默认创建四个控制节点，不默认连接 `Root -> Enter`，不默认创建普通状态节点。
- 引入上下文 flow port 声明，使 `StateMachineNode` 在普通行为图中只显示 `Input`，在 `StateMachineGraph` 中显示 `Input + Output`。
- 补齐 `TimelineNode` 的 `Output` flow port，使状态行为图可以串联 Timeline 后续行为。
- 旧 SO/config 不迁移；确认不用的数据直接删除。

## 待确认问题
- `AnyState -> Exit` 第一阶段允许，但是否真的符合动作业务语义，需要后续在具体状态机编辑体验里确认。

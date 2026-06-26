# 实现说明：状态机控制节点与 Transition 创作

## 主链路
- `BaseTree` 保持 Taco 原有普通 Graph 资产语义，不新增 `GraphProfile` 或 `Behavior` 字段。
- `StateMachineGraph : BaseTree` 是状态机图资产边界。
- `StateMachineNode` 是普通状态节点，继承或接入 `RunnableNode`，继续通过 `ScopedGraphReferenceModule` 引用下一层 Graph。
- `Root/Enter/AnyState/Exit` 不挂在普通 `StateMachineNode` 上。
- `Root` 是行为树进入当前层 `StateMachineGraph` 的入口源，不是 Taco `RootNode`，也不是普通状态。
- `Enter` 是父级状态下钻进入下一层 `StateMachineGraph` 的入口源，没有 input port，不从当前层 `Root` 接收输入。
- 控制点目标形态是 `StateMachineControlNode` 基类加 `StateMachineRootNode`、`StateMachineEnterNode`、`StateMachineAnyStateNode`、`StateMachineExitNode` 四个具体节点。
- `NodeModule`、`PropertyPort`、`PropertyEdge` 仍是 Taco 原有模块字段扫描和端口序列化链路。

## 状态机图
- 每层 `StateMachineGraph` 必须且只能有一个 `Root`、一个 `Enter`、一个 `AnyState`、一个 `Exit`。
- 新建 `StateMachineGraph` 只默认创建这四个控制节点，不自动连接 `Root -> Enter`，不自动创建普通状态节点。
- `Root` 负责行为树进入当前层图时选择第一个普通状态。
- `Enter` 负责父级状态下钻进入下一层图时选择第一个普通状态。
- `Root` 第一阶段要求至少一条出边，目标必须是当前层普通状态。
- `Enter` 不允许入边，第一阶段要求至少一条出边，目标必须是当前层普通状态。
- `AnyState` 负责全局条件跳转。
- `Exit` 负责结束本层状态机 Graph。
- 普通状态只用 `StateMachineNode` 表达。
- active state 的子 Graph 返回 `Success` 但没有命中 Transition 时，本层继续返回 `Running` 并保持 active state，直到显式 Transition 或 Exit。
- `StateMachineGraph` 不继承 `RunnableTree`，由父级 `StateMachineNode` 通过 `StateMachineGraphRuntime` 解释执行。

## Transition
- Transition 是 `StateMachineGraph` 内 flow edge 的语义。
- 合法连接是 `Root/Enter -> State`、`AnyState/State -> State/Exit`。
- 编辑器拖拽 flow port 时会在兼容 port 候选阶段过滤非法状态机端点，避免 `Root` 或 `Enter` 连到普通 Taco 行为节点。
- Transition 第一阶段支持优先级和一个 Bool 条件端口引用。
- `AnyState` transition 必须有条件。
- 不新增 `TransitionNode`。
- `StateMachineNode` 在 `StateMachineGraph` 中需要 `Output` flow port 承载 `State -> State`、`State -> Exit`。
- `StateMachineNode` 在普通行为图中只显示 `Input`，作为父级 Taco 控制流下的可执行节点，不显示外层 `Output`。
- flow port 由节点在当前 `Owner` graph 下的声明生成；默认节点仍使用 Taco class attribute。

## Timeline 和 BT 关系
- Timeline、Action、Value、BT 子图属于某个状态下钻后的行为 Graph。
- 状态机同层 Graph 负责选择 active State 和 Transition，不直接承载具体动画/FootPhase 行为。
- `TimelineNode` 不直接放在 `StateMachineGraph` 同层，而是放在 `StateMachineNode` 下钻后的 `RunnableTree` 体系 Graph 中。
- `TimelineNode` 当前依赖 `RunnableTree.DeltaTime`，因此承载它的行为 Graph 必须能被 `UpdateTree(deltaTime)` 驱动。
- FootPhase 后续应作为 Timeline 或状态行为 Graph 内的数据表达，不回到旧 SO/config。

## 禁止路径
- 不恢复 `InitialStateNodeGuid`。
- 不恢复 `StateMachineNodeRole` 或状态角色模块。
- 不把 `Root/Enter/AnyState/Exit` 挂到普通 `StateMachineNode`。
- 不自动生成默认普通状态节点。
- 不新增 `StateMachineControlModule`。
- 不新增 `StateMachineTree`。
- 不新增 `GraphProfile` 或 `Behavior` 图级开关。
- 不新增并行 port registry。
- 不迁回旧 Locomotion、Action、FootPhase SO/config 数据。

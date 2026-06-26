# Change: 统一状态机控制节点与 Transition 创作

## Why
当前状态机创作需要在 Taco 主链路上收口：普通状态、图边界控制点、Transition 条件不能再混成一个 role 字段，也不能回到旧 Locomotion/Action/FootPhase 配置。

普通状态必须仍然是 `StateMachineNode`；每层 `StateMachineGraph` 需要一个固定 `Root` 控制节点作为行为树进入当前层状态机的入口。`Enter` 表达父级状态下钻进入下一层状态机时的入口，不是当前层里从 `Root` 接收输入的节点。`Root`、`Enter`、`AnyState`、`Exit` 都是状态机图层级控制节点，不能作为模块挂到普通状态节点上。Transition 继续使用边语义，避免新增转换节点导致图结构膨胀。

## What Changes
- 明确 `StateMachineNode` 只表达普通状态和递归下钻边界，不携带 `Root/Enter/AnyState/Exit` 控制模块。
- 使用 `StateMachineControlNode` 基类和 `StateMachineRootNode`、`StateMachineEnterNode`、`StateMachineAnyStateNode`、`StateMachineExitNode` 四个具体控制节点表达图层级控制语义。
- 明确 `StateMachineRootNode` 是行为树进入当前层状态机图的入口；它直接通过出边选择当前层 active state，不参与普通状态 tick。
- 明确 `StateMachineEnterNode` 是父级状态下钻进入下一层状态机图时使用的入口；它没有 input port，也不从当前层 `Root` 接收输入。
- `Root/Enter/AnyState/Exit` 只能在 `StateMachineGraph` 中创建；普通 `BaseTree` 和普通状态节点不能创建或挂载这些控制语义。
- 每一层 `StateMachineGraph` 必须且只能有一个 `Root`、一个 `Enter`、一个 `AnyState`、一个 `Exit`。
- 删除 `InitialStateNodeGuid` 路径；状态机根据进入来源从 `Root` 或 `Enter` 选择初始 active state，并从 `Exit` 控制节点完成。
- Transition 是状态机图内的 edge 语义，不新增 `TransitionNode`。
- Transition 支持优先级和一个可选 Bool 端口条件引用；复杂条件先由普通节点或模块计算为 Bool。
- `AnyState` transition 必须有条件，避免无条件全局抢跳。
- `StateMachineGraph` 保持 `BaseTree` 资产身份，不继承 `RunnableTree`；它由父级 `StateMachineNode` 通过 `StateMachineGraphRuntime` 解释执行。
- `TimelineNode` 不直接作为 `StateMachineGraph` 同层状态节点创建；Timeline 通过 `StateMachineNode` 下钻到 `RunnableTree`/`OneRootTree`/`TimelineRunningTree` 后播放。
- 新增上下文 flow port 声明策略：默认仍从 Taco `Input/Output` attribute 生成端口，但节点可以根据所在 `BaseGraph` 调整端口集合。
- 明确 `StateMachineNode` 在普通行为图中只暴露 `Input`，作为被父级 Taco 节点 tick 的可执行叶节点；在 `StateMachineGraph` 中暴露 `Input + Output`，用于同层状态 Transition。
- 明确上下文 flow port 不是新的 port 系统，不新增 `SMPort`、`TransitionPort`、并行 port registry 或并行 edge 类型。
- 明确 `TimelineNode` 的 flow output 端口策略仍只服务状态行为 Graph 内的普通行为串联，不让 `TimelineNode` 直接进入 `StateMachineGraph` 同层状态转换。
- 不实现运行时编译导出，不迁回旧 SO/config 数据，不保留旧 Locomotion/Action/FootPhase 特化数据路径。

## Impact
- 影响的规格：`taco-sm-node-authoring`
- 影响的代码：
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Custom/StateMachineNode.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Custom/StateMachineControlNodes.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Node_Extension.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/StateMachineGraph.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/StateMachineGraphRuntime.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/NestedGraphValidation.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/Node/BaseNodeView.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/Timeline/Scripts/Tree/TimelineNode.cs`
  - Taco Graph 编辑器的节点搜索、创建预设、Inspector、边菜单和下钻 UI

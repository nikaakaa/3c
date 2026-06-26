# Split Taco SM State Node Authoring

## Why

当前 `StateMachineNode` 同时承担三种职责：父级行为图进入状态机的入口、`StateMachineGraph` 内的普通状态、状态内部行为链路入口。为了让它在 `StateMachineGraph` 内同时表达 Transition 和 inline behavior，上一版又引入了 `StateIn`、`StateOut`、`Behavior` 多个 flow port。Taco 当前 flow port 视觉上不显示语义标签，这会让状态转换线、行为执行线和下钻入口混在一起，编辑体验不清晰。

这和当前目标冲突：状态机结构图需要只表达状态关系；普通 tree/runnable/timeline 行为应该在 `StateNode` 下钻后的状态行为 `SubTree` 中编辑。

## What Changes

- 拆分 `StateMachineNode` 和 `StateNode`。
- `StateMachineNode` 只表达父级行为图进入一个 `StateMachineGraph` 的入口节点。
- `StateNode` 只表达 `StateMachineGraph` 内的普通状态。
- `StateMachineGraph` 内 Transition 只连接控制节点、`StateNode` 和 `Exit`。
- `StateMachineGraph` 内不创建普通 tree/runnable/timeline 行为节点。
- `StateNode` 通过正式状态行为引用模块下钻到状态行为 `SubTree`。
- Taco 原生 `RootNode` 只出现在普通行为图或状态行为 `SubTree` 中，不出现在 `StateMachineGraph` 中。
- 普通 `SubTree` 保持只有 `RootNode` 的普通子树语义。
- 新增 `StateBehaviorSubTree`，固定拥有 `OnEnter`、`RootNode`、`OnExit` 三个入口。
- Runtime 对普通 `SubTree` 直接执行 `RootNode`；对 `StateBehaviorSubTree` 进入状态时先执行 `OnEnter`，active 期间执行 `RootNode`，切出前执行 `OnExit`。
- Runtime active state 从 `StateMachineNode` 改为 `StateNode`。
- 相关 current specs 改为 B 方案口径，删除 A2 中 `StateMachineNode` 在 `StateMachineGraph` 中作为 state 的要求。

## Impact

- 影响规格：
  - `taco-sm-node-authoring`
  - `taco-runnable-timeline-node`
  - `taco-graph-core`
  - `taco-componentized-node-authoring`
- 影响代码区域：
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Custom/StateMachineNode.cs`
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Custom/StateMachineControlNodes.cs`
  - 新增 `StateNode` 所在节点目录
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/StateMachineGraph.cs`
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/StateMachineGraphRuntime.cs`
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Graph/NestedGraphValidation.cs`
  - `Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/BaseTreeView.cs`

## Out Of Scope

- 不实现 gameplay runtime 管线。
- 不新增编译导出 runtime 数据。
- 不恢复旧 locomotion/action/footphase/bodyclaim SO/config 数据源。
- 不新增 Workbench、并行端口描述符、并行 graph window 或 fallback 配置。
- 不新增测试，除非后续明确要求。

## Current Spec Conflicts To Resolve

- `taco-sm-node-authoring` 当前写着 `StateMachineNode 是唯一普通状态节点`，与 B 方案冲突。
- `taco-sm-node-authoring` 当前写着 `StateMachineNode` 在 `StateMachineGraph` 中提供 `StateOut/Behavior`，与 B 方案冲突。
- `taco-sm-node-authoring` 当前把 Transition 端点写成 `StateMachineNode`，需要改为 `StateNode`。
- `taco-runnable-timeline-node` 当前还允许 `StateNode` inline behavior，需要改成只通过状态行为 `SubTree` 接入 Timeline。
- `taco-componentized-node-authoring` 当前把 StateMachine 节点和 Timeline 节点并列为同图创作，需要明确 StateMachine 结构图和 State 行为 `SubTree` 的图类型边界。

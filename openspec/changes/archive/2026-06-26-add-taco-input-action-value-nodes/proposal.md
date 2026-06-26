# Add Taco Input Action Value Nodes

## Why

当前项目已经依赖 Unity Input System，但 Taco 图内还没有正式的 InputAction 值来源节点。状态机 Transition 条件已经支持从同层 `ValueNode` 的 Bool 输出读取条件，项目主链路也要求后续 gameplay 走 `Input -> Action Request -> State/Graph Decision`，因此输入必须进入 Taco 正式节点和端口系统，而不是另建 SO/config、Workbench 路径或临时输入桥。

这个变更要解决的是：用户把 Unity Input System 的 `InputActionReference` 或 `InputActionAsset` 拖进 Taco 图时，编辑器能创建对应输入值节点；这些节点在图内以 `ValueNode` 暴露 Bool、Float、Vector2 等正式 `PropertyPort` 输出，可作为状态机 Transition 条件或后续行为图输入。

## What Changes

- 新增 Taco InputAction 值节点族，节点继承 `ValueNode`，不继承 `BaseTree`，不新增 Input 专用 Graph。
- 新增组件式输入绑定模块，用 `NodeModule` 承载 InputAction 绑定、显示信息和资产引用能力。
- 输入绑定使用正式 Unity Input System 资产和稳定 action identity，不依赖 action 显示名作为连接身份。
- 新增 Bool/Button、Float、Vector2 三类第一阶段输入输出节点，使用现有 `BoolPropertyPort`、`FloatPropertyPort`、`Vector2PropertyPort`。
- 在 Taco 图编辑器中接入现有 `DropArea` 拖拽扩展点，支持拖入 `InputActionReference` 和 `InputActionAsset`。
- 拖拽创建节点必须复用 `BaseTreeView.CreateNode()` 和当前图的 `CanCreateNodeType()`，不得绕过图类型创建规则。
- 输入节点只读取正式输入源提供的值，不在 `ValueNode` 内偷偷启用、禁用或全局查找 `PlayerInput`。
- `InputActionAsset` 批量创建时只为支持的 action value type 创建正式节点，不为不支持类型创建 object fallback 节点。

## Impact

- 影响规格：
  - 新增 `taco-input-action-node-authoring`
  - 关联 `taco-componentized-node-authoring`
  - 关联 `taco-graph-core`
  - 关联 `taco-sm-node-authoring`
- 影响代码区域：
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Node/Value`
  - `Assets/Scripts/Taco/TreeDesigner/Scripts/Node/NodeModule.cs` 周边模块使用方式
  - `Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/BaseTreeView.cs`
  - `Assets/Scripts/Taco/Editor/Scripts/Manipulator/DropArea.cs` 的使用点
  - 后续正式 input runtime/provider 所在目录

## Out Of Scope

- 不做完整角色 gameplay runtime。
- 不做完整输入重绑定 UI。
- 不做多玩家输入路由、远端玩家输入复制或网络预测。
- 不新增 Workbench、并行端口描述符、并行 graph window 或 fallback 配置。
- 不新增旧 locomotion/action SO/config 数据源。
- 不新增测试，除非后续明确要求。

## Current Spec Comparison

- 与 `taco-componentized-node-authoring` 一致：输入绑定属于节点组合能力，应放在 `NodeModule`，并通过字段访问器贡献字段和资产引用。
- 与 `taco-graph-core` 一致：输入节点必须通过 `BaseGraph` / `BaseTreeView` 的正式创建逻辑进入同一套节点集合，不新增输入专用树或重复节点集合。
- 与 `taco-sm-node-authoring` 一致：`StateMachineGraph` MAY 创建 `ValueNode` 作为 Transition 条件计算节点，InputAction Bool 节点可以作为条件来源；同时它不得成为 Transition flow 端点。
- 当前 specs 没有直接定义 InputSystem 节点，因此本 change 是新增能力，不需要修改已有 requirement。
- 需要避免的矛盾：拖拽入口不能绕过 `StateMachineGraph.CanCreateNodeType()`；如果未来某个图类型拒绝 `ValueNode`，拖拽也必须拒绝创建。

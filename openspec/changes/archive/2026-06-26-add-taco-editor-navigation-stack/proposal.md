# Change: 增加 Taco 编辑器页面栈导航

## Why
当前 Taco 编辑器已经可以通过节点的 Graph 引用打开子图：节点双击或右键 `Open Reference/Graph` 会调用 `OpenTree()` 切换到被引用的 `BaseTree`。这个能力能下钻，但不会保留用户从哪里进入，也没有返回、面包屑或当前编辑路径。

状态机和 Timeline 创作会频繁出现多层下钻：

```text
TopTree / Locomotion / Idle / Timeline
```

如果每次打开子图都直接替换窗口内容，用户会失去上下文，尤其在同一个 Graph 被多个 `StateMachineNode` 复用时，不能依赖全局父子关系反推“应该回到哪里”。因此需要一个以当前打开图为起点的窗口内页面栈，而不是全项目唯一层级树。

## What Changes
- 在 `BaseTreeWindow` 内增加 editor-only 页面栈，记录当前窗口这次编辑会话的访问路径。
- 直接打开 `BaseTree` 资产或 Tree Browser 中的图时，作为新的栈根 `ReplaceRoot`。
- 从节点的 Graph 引用下钻时，向当前窗口页面栈 `Push` 一个页面。
- 页面栈条目记录当前 `BaseTree`、显示名、来源图、来源节点 GUID 和引用 key。
- `StateMachineNode`、`TreeReferenceModule`、`ScopedGraphReferenceModule` 等 Graph 引用继续使用 `BaseTree` 资产字段，不新增并行引用模型。
- 在窗口顶部提供返回按钮和 breadcrumb。
- 点击 breadcrumb 中间层时回退到对应页面，并丢弃其后的页面。
- 没有可用 Graph 引用时，节点双击和右键打开入口保持禁用或无操作。
- 不做全局 Graph 层级树、不做多标签页、不做 Forward 历史、不把导航栈序列化进图资产。

## Impact
- 影响规格：
  - `taco-editor-navigation-stack`
  - `taco-graph-core`
- 影响代码：
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/Window/BaseTreeWindow.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/View/Node/BaseNodeView.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Scripts/Utility/TreeWindowUtility.cs`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Resources/VisualTree/BaseTreeWindow.uxml`
  - `3cDemo/Client/3C_Client/Assets/Scripts/Taco/TreeDesigner/Editor/Resources/StyleSheet/BaseTreeWindow.uss`

## Current Spec Comparison
- `taco-sm-node-authoring` 已经要求 `StateMachineNode` 支持递归下钻。本变更补的是编辑器会话导航，不改变 `StateMachineNode`、`StateMachineGraph` 或运行时语义。
- `taco-componentized-node-authoring` 已经要求 Graph 嵌套不是 PropertyPort 语义。本变更继续通过 `GetGraphReferences()` 打开子 Graph，不改 PropertyPort。
- `taco-graph-core` 当前要求“编辑器 MUST 继续通过 `OpenTree()` 打开该引用”。页面栈会让节点下钻走当前窗口的 `PushReference` 入口，因此本 proposal 同步修改该要求：直接资产打开仍走 `OpenTree()`；当前窗口内的引用下钻可以通过页面栈入口打开，但引用字段仍必须是 `BaseTree`。

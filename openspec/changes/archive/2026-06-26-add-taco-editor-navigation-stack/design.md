## 背景
Taco 现在的下钻能力是“打开引用资产”：`BaseNodeView` 从 `GetGraphReferences()` 拿到 `NodeGraphReference`，双击节点标题或右键 `Open Reference/Graph` 后调用 `reference.Tree.OpenTree()`。`TreeWindowUtility.OpenTree()` 根据目标 `BaseTree` 的 `TreeWindowAttribute` 找窗口并调用 `SelectTree()`。

这个链路足够打开子图，但它没有记录来源节点。对于状态机创作，用户需要知道当前图是从哪个状态节点进入的，并且能够快速回到上一层。

## 目标
- 页面栈从当前 open 的图开始，而不是从项目全局根节点开始。
- 节点下钻时 push 子图，保留来源节点上下文。
- 直接打开资产时 replace 当前栈根。
- 支持 Back 和 breadcrumb。
- 不新增图数据字段，不序列化导航状态。
- 不改变 `BaseTree`、`BaseGraph`、`StateMachineNode` 或 Graph 引用模块的数据模型。

## 非目标
- 不做全局 Graph 引用树。
- 不做多标签页。
- 不做 Forward 历史。
- 不做 Graph 资产自动创建和绑定。
- 不保存最近打开路径到资产。
- 不从全项目反推某个 Graph 的唯一父节点。
- 不新增 Workbench 窗口或并行编辑器路径。

## 决策

### 决策：页面栈属于窗口，不属于资产
页面栈保存在 `BaseTreeWindow` 实例里，只表达当前窗口这次编辑路径：

```text
LocomotionGraph / Idle / Timeline
```

如果用户直接打开 `LocomotionGraph`，它就是栈根。如果用户从 `TopTree` 的 `Locomotion` 节点下钻到 `LocomotionGraph`，栈路径会是：

```text
TopTree / Locomotion
```

这个设计不要求找到全项目根，也不要求判断某个 Graph 的唯一父节点。

备选方案：
- 全局引用树：能看全局结构，但同图复用会出现多父节点，第一阶段成本过高。
- 序列化导航路径：能恢复上次窗口，但会污染图资产，且路径是编辑器会话状态，不是 authoring 数据。

### 决策：直接打开资产是 ReplaceRoot，节点下钻是 Push
打开入口分成两类：

```text
OpenTree(asset) / Tree Browser / Inspector Open
  ReplaceRoot(asset)

Node double click / Open Reference
  Push(reference tree, source node, reference key)
```

这样直接打开任意图不会继承上一次无关路径，节点下钻又能保持当前上下文。

备选方案：
- 直接打开资产也 Push：会把无关编辑路径串在一起，breadcrumb 误导用户。
- 所有打开都 Replace：无法返回上一层，和当前问题相同。

### 决策：breadcrumb 显示进入该页面的上下文名
页面条目建议包含：

```text
BaseTree Tree
string DisplayName
BaseTree SourceTree
string SourceNodeGuid
string ReferenceKey
```

根页面显示 Graph asset 名。下钻页面优先显示来源节点名；如果节点名不可用，再显示引用 label 或 Graph asset 名。

原因是用户关心“我从 Locomotion 这个节点进入了它的子图”，而不是只关心某个 asset 名。多个节点复用同一个 Graph 时，breadcrumb 也能表达不同访问路径。

### 决策：复用 SelectTree 刷新窗口
页面栈只决定当前要显示哪个 `BaseTree`，真正刷新仍由 `BaseTreeWindow.SelectTree()` 负责。这样不重复实现 GraphView 填充、Inspector 填充、`CheckInit()`、`Refresh()` 和 `TreeWindowUtility.SelectTree()`。

需要避免的点：
- `SelectTree()` 不应该每次都清空导航栈，否则 Push 后路径会丢。
- `TreeWindowUtility.GetWindow()` 直接打开资产时应该走 replace root，而不是普通 push。

### 决策：第一阶段只做 Back，不做 Forward
Back 可以满足下钻后返回上一层。Forward 会引入分支历史，和点击 breadcrumb 后是否保留前进栈的问题相关，第一阶段先不做。

后续如果需要浏览器式历史，可以在页面栈基础上增加 `forwardStack`，但不作为本 change 目标。

## 数据草图
```text
GraphNavigationEntry
  Tree: BaseTree
  DisplayName: string
  SourceTree: BaseTree
  SourceNodeGuid: string
  ReferenceKey: string
```

窗口状态：

```text
BaseTreeWindow
  List<GraphNavigationEntry> navigationStack
  int currentIndex
```

第一阶段也可以不保留 `currentIndex`，只保留一个线性栈；点击 breadcrumb 中间层时移除其后的条目。

## UI 草图
```text
[<] TopTree / Locomotion / Idle
```

- Back 按钮：当前只有根页面时禁用。
- Breadcrumb segment：点击任一 segment 回到该页面。
- 当前页面 segment：不可点击或点击无操作。

## 风险与取舍
- 页面栈不展示全项目引用关系，但可以最小解决下钻迷路问题。
- 同一个 Graph 被多个节点复用时，页面栈显示访问路径，避免错误地声明唯一父节点。
- 不做 Forward 会少一个浏览器体验，但能保持实现简单。
- 不序列化导航栈意味着关闭窗口后路径消失，这是正确边界：导航路径是编辑器会话状态，不是创作数据。

## 迁移策略
- 保留已有 `OpenTree()` 作为直接打开资产入口。
- 节点双击和右键 `Open Reference` 改为优先调用当前 `BaseTreeWindow` 的页面栈下钻入口。
- `TreeWindowUtility.OnOpened` 事件继续可用，避免破坏现有 `NodeReferenceWindow`。

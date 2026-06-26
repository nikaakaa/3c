## 背景
当前代码里 `BaseTree` 是 `ScriptableObject`，直接持有：

- `m_Nodes`
- `m_Edges`
- `m_PropertyEdges`
- `m_ExposedProperties`
- GUID 映射
- `InitTree` / `DisposeTree`
- `CreateNode` / `DeleteNode`
- `Link` / `UnLink`
- `LinkProperty` / `UnLinkProperty`
- `Refresh` / `CheckInit`

编辑器 UI 入口也强依赖 `BaseTree`：

- `TreeWindowUtility.OpenTree(BaseTree tree)`
- `BaseTreeWindow.Tree`
- `BaseTreeView.PopulateView(BaseTree tree)`
- `NodeSearchWindow` 通过 `tree.CanCreateNodeType(type)` 过滤创建菜单
- `BaseTreeInspector` 使用 `[CustomEditor(typeof(BaseTree), true)]`

所以如果直接把 UI 全改成 `BaseGraph`，影响面会覆盖窗口、Inspector、搜索、序列化绑定、引用下钻和资源打开。当前更稳的路径是：`BaseGraph` 抽底座，`BaseTree` 保留为 UI-facing asset。

## 目标
- 让图数据和图结构编辑 API 有一个明确的 `BaseGraph` 底座。
- 让 `BaseTree` 变成图资产/编辑器入口，而不是唯一的图结构概念。
- 保持现有 Taco 编辑器 UI 继续打开 `BaseTree` 及其子类。
- 保持 `RunnableTree` 表达可执行 Tree，`StateMachineGraph` 表达状态机图语义。
- 让节点、边、属性边的 Owner 能指向 `BaseGraph`，避免底层结构永远只认 Tree 命名。
- 清理重复或旧路径，不保留两套节点列表、两套边列表或两套端口系统。

## 非目标
- 不把所有编辑器窗口改名为 GraphWindow。
- 不新增 `BaseGraphWindow`。
- 不把 `TreeWindowUtility.OpenTree` 改成打开任意 `BaseGraph`。
- 不新增 Workbench 图、Workbench 节点或 Workbench 端口路径。
- 不改 `PropertyPort` / `PropertyEdge` 的主链路。
- 不在这个变更里解决 TimelineNode 的执行上下文抽象。
- 不做运行时编译导出。
- 不恢复旧 Locomotion、Action、FootPhase SO/config 数据。

## 决策

### 决策：BaseGraph 是图结构底座
新增：

```text
BaseGraph : ScriptableObject
BaseTree : BaseGraph
RunnableTree : BaseTree
StateMachineGraph : BaseTree
```

`BaseGraph` 承载图结构数据和结构操作。`BaseTree` 继续承载 Taco 编辑器可打开资产的语义。

备选方案：
- 只改名 `BaseTree -> BaseGraph`：会直接打穿编辑器 UI 和资产入口，成本过大。
- 只新增空 `BaseGraph`：没有实际收益，只是多一层继承。
- 使用接口 `IGraph`：不会解决 Unity 序列化字段归属，也不能自然承载资产继承链。

### 决策：编辑器 UI 第一阶段继续依赖 BaseTree
`BaseTreeWindow`、`BaseTreeView`、`BaseTreeInspector`、`TreeWindowUtility` 第一阶段继续接收 `BaseTree`。它们能通过继承访问 `BaseGraph` 的节点、边和编辑操作。

这样做的业务取舍是：

- 优点：不重做 UI，不破坏资产打开，不影响当前节点搜索和 Inspector。
- 代价：编辑器命名里仍然有 Tree，Graph 命名不会一次性贯穿 UI。

这不是保留旧数据路径；这是保留当前 Taco 编辑器入口，避免为了命名把窗口系统整条链路重写。

### 决策：节点和边的 Owner 改为 BaseGraph
`BaseNode.Owner`、`BaseEdge.Owner`、`BaseNode.Init(...)`、`BaseEdge.Init(...)`、`PropertyEdge.Init(...)` 应改为 `BaseGraph`。节点和边只需要知道自己属于哪个图结构，不应该强制知道这个图是不是编辑器层的 Tree 资产。

Graph 引用模块和下钻 UI 仍然保存 `BaseTree`，因为当前能被 `OpenTree()` 打开的资产就是 `BaseTree` 及其子类。

### 决策：图结构 API 不分裂
`CreateNode`、`DeleteNode`、`Link`、`UnLink`、`LinkProperty`、`UnLinkProperty`、`Refresh`、`CheckInit` 只能保留一套正式实现。迁移时不能在 `BaseTree` 和 `BaseGraph` 各保留一份可修改的实现。

`BaseTree` 如果需要为了现有调用点保留薄包装，包装必须只转发到 `BaseGraph` 的正式实现，并在同一轮任务里把能直接改的调用点改到新边界。

### 决策：BaseGraph 不表达运行时生命周期
`BaseGraph` 不拥有 `Running`、`State`、`DeltaTime`、`UpdateTree`、`ResetTree`。这些仍属于 `RunnableTree`。

原因是 Graph 是结构底座，RunnableTree 是可执行图语义。把运行状态下沉到 `BaseGraph` 会让 `StateMachineGraph`、纯编辑图、Timeline 资产引用图都默认携带不必要的运行时含义。

### 决策：序列化字段只迁移一次
节点、边、属性边、暴露属性和编辑辅助集合迁移到 `BaseGraph` 后，`BaseTree` 不能再保留同名或镜像字段。旧路径不做 fallback 字段。若 Unity 资产需要用户重新保存，由用户端到端验证后接受该迁移结果。

## 风险与取舍
- 移动 `[SerializeField]` / `[SerializeReference]` 字段到基类可能影响已有资产反序列化；好处是结构边界干净，代价是需要用户在 Unity 里实际打开验证。
- `Owner` 改为 `BaseGraph` 会触发较多编译点；好处是底层不再被 Tree 命名锁死。
- UI 保持 `BaseTree` 会留下命名不彻底的问题；好处是避免把窗口系统和图结构抽层绑成一次大爆炸。
- 不处理 TimelineNode 执行上下文意味着 SM/Timeline 运行闭环仍需后续变更；好处是本提案不会把运行时问题伪装成图命名问题。

## 迁移计划
- 新增 `BaseGraph`。
- 将图结构字段和结构操作迁移到 `BaseGraph`。
- 修改 `BaseTree` 继承 `BaseGraph`，保留 TreeWindow/AcceptableNodePaths 等编辑器入口语义。
- 修改节点、边、属性边 Owner 和 Init 签名。
- 修正所有因 Owner 类型变化导致的编译点。
- 确认编辑器 UI 仍然只打开 `BaseTree`，不新增 `BaseGraphWindow`。
- 清理迁移后残留的重复字段、重复方法和旧路径。

## 待确认问题
- 是否在同一变更里把 `InitTree` / `DisposeTree` 这类方法名改成 `InitGraph` / `DisposeGraph`。当前建议：本变更只抽结构，不做全调用点命名重构；否则会把执行树 API 和编辑器 API 一起卷进来。

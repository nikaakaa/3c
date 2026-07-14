# Design: 内联图数据和 shared asset 外壳

## 当前代码事实
当前核心链路是：

- `BaseGraph : ScriptableObject` 直接持有 `m_Nodes`、`m_Edges`、`m_PropertyEdges`、`m_ExposedProperties`。
- `BaseTree : BaseGraph` 是可打开图的数据类型，同时也承载 BTSMTL 旧命名里的 Tree 语义；Unity Project 中的可打开资产入口由 `BaseTreeAsset` 外壳提供。
- `StateMachineNode.Graph` 来自 `ScopedGraphReferenceModule.m_Graph : BaseTree`。
- `StateNode.SubTree` 来自 `StateBehaviorGraphReferenceModule.m_SubTree : SubTree`。
- `BaseEdge.m_TransitionRuleGraph : TransitionRuleGraph` 保存 Transition rule graph 引用。
- `EmbeddedGraphOwnershipUtility` 通过 `ScriptableObject.CreateInstance` 和 `AssetDatabase.AddObjectToAsset` 创建私有状态机图、状态行为图和规则图。
- `StateMachineNode` 和 `TransitionRuleGraphRuntime` 运行时通过 `Object.Instantiate(graph)` 得到运行实例。

这说明问题不在端口系统。`PropertyPort` / `PropertyEdge` 可以继续承担数据流连接。真正的问题是图数据的持久化身份被 Unity `ScriptableObject` 锁死，导致“默认私有图”只能落到 subasset 或独立 asset。

## 目标模型
新的正式模型分成两层：

```text
Owner node / edge
  ├─ inline GraphData：默认私有，普通 C# 序列化字段
  └─ shared GraphAsset：显式复用，独立 ScriptableObject asset

BaseTreeAsset / shared GraphAsset
  └─ GraphData：可复用图数据
```

默认解析规则：

```text
如果 shared asset 存在：使用 shared asset.GraphData
否则：使用 owner inline GraphData
```

这不是 fallback，而是正式 ownership 规则。inline 和 shared 是互斥创作模式：切到 shared 后必须清理 owner 内联副本，避免同一引用同时有两份真数据。

## 命名边界
本 change 的推荐边界：

- `BaseGraph`：普通 C# 可序列化图数据基类，承载节点、边、属性边、暴露属性、GUID 映射、结构编辑 API 和运行上下文。
- `BaseTree`：继续继承 `BaseGraph`，作为 Tree 类图数据类型和窗口打开语义，不再继承或拥有 Unity asset 身份。
- `BaseTreeAsset`：Unity `ScriptableObject` 资产外壳，持有一份正式 `BaseTree` 图数据，用于 Project、Inspector、Tree Browser 和 shared graph 引用。
- `StateMachineGraph`、`StateBehaviorSubTree`、`TransitionRuleGraph`：优先表达图数据类型，而不是资产身份。
- shared asset 类型只负责项目文件、复用和 Project/Inspector 打开入口。

引入 `BaseTreeAsset` 而不是让 `BaseTree` 自己继续做 SO 的取舍：

- 业务好处：`BaseTree/StateMachineGraph/SubTree/TransitionRuleGraph` 都保持纯数据，可以内联在节点、边和模块中；Project 资产入口仍然明确存在，复用图不会丢。
- 代价：资产类型名多一层 `BaseTreeAsset`，旧 BTSMTL 的 `BaseTree` 名字仍然保留 Tree 历史语义；后续如果要进一步清理命名，可以单独做 `rename-btsmtl-tree-assets-to-graph-assets`。

直接本轮改成 `GraphAsset` 的取舍：

- 业务好处：名字更干净，`StateMachineGraphAsset`、`TransitionRuleGraphAsset` 心智更直。
- 代价：窗口、Inspector、菜单、引用模块和现有 asset 类型名会一起炸开，容易把资产命名清理和图 ownership 主线混在一起。

## 创建和下钻
`StateMachineNode` 创建后立即拥有内联 `StateMachineGraph` 数据：

- 图数据直接序列化在节点字段或节点模块字段中。
- 编辑器双击或 Open 命令打开该内联图数据。
- UI 显示其归属为 `Inline`。
- 不要求 owner tree 先保存成 asset。
- 不调用 `AssetDatabase.AddObjectToAsset`。

`StateNode` 创建或初始化状态行为时拥有内联状态行为图数据：

- 默认图数据包含状态行为入口，例如 `RootNode`，或 `StateBehaviorSubTree` 的 `OnEnter`、`RootNode`、`OnExit`。
- 状态行为图内可以创建 Timeline、Action、Composite、Decorator、Tree 引用或嵌套 `StateMachineNode`。

Transition 创建 rule 时拥有内联 `TransitionRuleGraph` 数据：

- Transition 本体仍是同层 `BaseEdge`。
- rule graph 只负责条件求值。
- 双击 Transition 或点击 rule 命令打开内联 rule graph。

## 复用和独立 asset
复用必须是显式动作：

- `Create Shared`：创建新的独立 graph asset 并绑定。
- `Extract Shared`：把当前 inline graph data 移动到独立 graph asset，然后清空 owner inline graph data。
- `Use Shared`：绑定已有 graph asset，并清空 owner inline graph data。

删除 owner 的规则：

- owner 使用 inline graph data：删除 owner 即删除图数据。
- owner 使用 shared asset：删除 owner 只断开引用，不删除 asset。

## 运行时工作副本
运行时不能直接修改 shared authoring data，也不能继续用 `Object.Instantiate(ScriptableObject graph)` 作为通用机制。

新的运行入口应通过图数据创建工作副本：

```text
resolved authoring GraphData
  -> GraphRuntimeFactory.CloneForRuntime(...)
  -> InitTree(user)
  -> Update / Evaluate
  -> DisposeTree()
```

业务取舍：

- 好处：多个角色、多个状态机节点、多个 Transition rule 引用同一 shared asset 时互不污染。
- 代价：需要一套正式 graph clone 工具，不能继续依赖 Unity Object clone 替我们处理引用。

## 删除旧路径
必须删除：

- `EmbeddedGraphOwnershipUtility`。
- 创建节点时自动 subasset 生成。
- 删除节点时递归销毁 owned embedded graph。
- `AssetDatabase.IsSubAsset` 判断 ownership。
- `SaveAsset/ImportAsset` 用于私有图生命周期的逻辑。
- `project.md` 和 current specs 中“默认 owned embedded sub-asset”的口径。

不删除：

- BTSMTL 原生 `PropertyPort` / `PropertyEdge`。
- 节点和模块字段扫描。
- Timeline asset 引用模式。
- shared graph asset 能力。

## 方案对比
### A. 继续 subasset
业务上不推荐。它能少改代码，但默认创建后项目文件里会出现隐藏 Unity Object，删除生命周期需要额外工具维护，且 owner 私有数据不是真正内联。

### B. 全部独立 SO asset
业务上不推荐。复用心智清楚，但默认创建 `StateMachineNode`、`StateNode`、Transition 时又回到“先创建或拖拽 asset”，打断动作编辑器的快速创作流程。

### C. 默认普通 C# 内联，复用才独立 SO asset
本 proposal 选择。它最贴近当前业务：动作编辑器里默认创建就是可下钻、可编辑、随 owner 生命周期走；需要复用时才显式资产化。

代价是底层必须破坏性拆开 `BaseGraph` 的数据身份和 Unity asset 身份。

# Design: Tree Inspector 信息架构收口

## Context

当前 `BaseTreeInspectorInside` 的层级等价于：

```text
Tab Bar
  Graph Page
    Generic Tree Property Container
  Selection Inspector Page
    Selected Node/Edge Inspector
  Graph Data Catalog
```

页签切换只控制前两个 page，导致 Catalog 始终可见。与此同时，通用 `BaseTreeInspector.PopulateProperties` 扫描所有 `ShowInInspector` 字段；`RunnableTree` 的 `Running` 与 `State` 是非序列化生命周期状态，因而在 authoring 页面显示成无业务价值的顶部字段。

目标布局为：

```text
Tab Bar: [Data] [Inspector]
  Data Page
    Graph Data Catalog
  Inspector Page
    Selected Node/Edge Inspector
    或没有选择时的 Graph Authoring Settings
```

## Goals

- 让作者始终知道左侧当前正在处理数据、选中对象，还是运行时观察。
- 保持 Graph Data Catalog 为 Input 与 Blackboard 的唯一正式作者入口。
- 保持 Node/Edge Inspector 的现有引用、Transition、StateMachine 与 Timeline 编辑能力。
- 让运行时观察只使用已建立的 `RuntimeDebugSession` 和 Trace 投影。
- 在窄左栏中减少默认占用高度，让数据条目优先可见。

## Non-Goals

- 不重做 GraphView、节点画布、Timeline Window、Pipeline Host Inspector 或 RuntimeDebugSession。
- 不增加第三个 Blackboard、Input、Graph Settings 或 Runtime 面板。
- 不更改 Blackboard declaration、InputProfile、Graph identity、序列化格式或 runtime address。
- 不为旧 `Running / State` 字段提供隐藏开关、兼容显示或 Debug fallback。

## Decisions

### 1. 使用两个互斥页签，而不是增加 Settings 页签

`Data` 页只表达“当前图可用的数据”；`Inspector` 页表达“当前选择的对象”。没有选择时，Inspector 展示图级 authoring settings。

业务取舍：第三个 `Settings` 页签能让概念更绝对独立，但 200px 左栏会迫使作者在数据、节点、图设置间高频跳转。将图级设置作为 Inspector 的空选择状态，保留两种稳定工作模式，同时不会让 Data 页混入不相关字段。

### 2. Data 页只承载唯一 Graph Data Catalog

Catalog UXML 必须成为 Data page 的子树，不能作为 tab pages 的同级元素。切换到 Inspector 时，Catalog 不可见、不响应输入，也不复制成第二套可滚动区域。

业务取舍：把 Catalog 固定在 Data 页会比“始终可见”少一次查看变量的便利，但换来明确页面语义、更多节点属性可用高度，以及不会把变量和 Transition 配置混在同一滚动流中。

### 3. Graph Settings 使用显式 authoring 属性投影

Graph Settings 必须只使用 authoring-facing、可序列化、可编辑的图属性。通用 Tree 属性扫描需要具备明确的 authoring surface 过滤，不能因为字段具有旧 `ShowInInspector` 注解就显示 runtime 生命周期数据。

`RunnableTree.Running` 与 `RunnableTree.State` 的 `ShowInInspector` 注解直接删除，不保留替代注解或旧 Inspector 分支。若当前图没有合法的图级 authoring settings，Inspector 只显示空选择/图上下文状态，不伪造配置字段。

业务取舍：删除字段会失去旧 Inspector 的即时状态提示，但这些值既不属于资产，也不能区分 runtime instance；新的 Debug Session 能表达 target、instance、tick、frame 与 source revision，才是可信的运行时观察面。

### 4. Live Debug 保持窗口级且只读

`Authoring / Live Debug` 属于整个 Graph window 的模式，继续位于导航/画布级工具栏。进入 Live Debug 后，Data 与 Inspector 的 authoring 命令按现有只读规则禁用；运行状态通过 overlay 和共享 Debug Session 呈现。

业务取舍：把调试字段塞回左侧 Inspector 看似方便，但会让普通 Inspector 与 runtime target/instance 选择发生混淆，并可能绕开 source revision 校验。窗口级模式让所有页面共享同一明确的运行时上下文。

### 5. Data 筛选采用 source-aware 紧凑布局

Source 使用 `All`、`Input`、`Blackboard` 的显式紧凑切换。搜索始终可见；Context 与 Scope 归入按需展开的 Blackboard filter surface。选择 Input 时不显示 Blackboard 专属条件；Blackboard filter 生效时，Catalog 继续沿用现有“Input 不伪造 scope/owner”的语义。

业务取舍：筛选要多一次展开操作，但默认列表可以更早展示真正可拖拽、可编辑的条目，减少 200px 面板里三组下拉框长期占用空间。

## Interaction Flow

```text
打开/下钻 Graph
  -> 重建同一个 Graph Data Catalog context
  -> 默认显示 Data 页

选择 Node 或 Edge
  -> 切换到 Inspector 页
  -> 显示该对象的正式 authoring fields/actions

手动打开 Inspector 且没有选择
  -> 显示 Graph Authoring Settings

切换到 Live Debug
  -> authoring commands 只读
  -> Graph overlay 从 RuntimeDebugSession 读取状态
  -> 不显示 Running / State 反射字段
```

Catalog 的搜索、filter、group foldout 与条目展开状态仍是 editor-only view state；下钻、返回和 context 切换继续按现有 stable identity 失效旧 command target，并按新图重建。

## Module Boundaries

| 模块 | 职责 | 不负责 |
| --- | --- | --- |
| `BaseTreeInspectorInside.uxml/.uss` | Data/Inspector 页面结构、窄栏布局与可见性 | 决定数据来源、写入资产或读取 runtime clone |
| `BaseTreeInspectorView` | 页签状态、空选择 Graph Settings、Catalog 刷新与 selection 切换 | 维护第二份 Blackboard/Input 数据 |
| `BaseTreeInspector` | 可复用的 authoring 属性投影和 field surface 过滤 | 显示 runtime lifecycle 状态 |
| `GraphDataCatalog` 与 source | Input/Blackboard 的正式投影、能力与命令 | 页签路由、runtime diagnostics |
| `BaseTreeWindow` / `RuntimeDebugSession` | 窗口级 Authoring/Live Debug 模式与 trace overlay | 重新引入字段反射 debug UI |

## Migration And Cleanup

1. 将 Catalog 放入 Data page，并删除旧 sibling layout、旧 Graph property page 命名和对应样式选择器。
2. 让 `BaseTreeInspectorView` 以 Data/Inspector 显式页签状态管理 selection 与空选择图设置。
3. 收口 `BaseTreeInspector` 的 authoring property surface，删除 `RunnableTree` 的旧 `Running / State` 展示注解。
4. 将筛选栏改为 source-aware 紧凑控件，保留同一个 filter state 与 Catalog source contract。
5. 搜索并删除遗留的旧字段展示、平行变量区或直接 runtime state Inspector 路径。

该迁移不触碰图资产、Blackboard declaration 或 runtime Trace 数据，因此不需要资产迁移、兼容 reader 或临时桥接。

## Risks

- 某些自定义 Tree 可能依赖通用属性扫描显示 authoring field：显式 authoring surface 必须保留这些合法序列化字段，而不是按类型粗暴清空整个 property container。
- UI Toolkit 的 source segmented control 与过滤面需在窄宽度下保持稳定尺寸：长标签必须截断，不允许随着筛选条件改变头部高度或遮挡 Catalog。
- Live Debug 激活期间仍可能发生 selection change：页面可以切换查看 source inspector，但不得执行 authoring 写入，也不得通过 Inspector 读取 runtime clone。

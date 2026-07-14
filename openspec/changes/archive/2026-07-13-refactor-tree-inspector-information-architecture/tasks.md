## 1. 基线与边界确认

- [x] 1.1 复读 current `btsmtl-graph-data-catalog-authoring`、`btsmtl-graph-core` 与 active runtime diagnostics delta，确认页签收口不改变 Catalog source 或 Trace contract
- [x] 1.2 盘点 `BaseTreeInspectorInside`、`BaseTreeInspectorView`、`BaseTreeInspector` 与 `RunnableTree` 中的 page、属性扫描和 runtime 状态展示入口
- [x] 1.3 确认当前 Graph Data Catalog 的 Input、Blackboard、创建、详情、拖拽与稳定 identity 行为不依赖旧 Graph property page
- [x] 1.4 确认 `Running`、`State` 不属于可序列化 authoring 设置，且正式 Live Debug 已可通过 RuntimeDebugSession 观察运行状态

## 2. Data 与 Inspector 页签结构

- [x] 2.1 将左侧第一个页签重命名为 `Data`，保留第二个页签为 `Inspector`
- [x] 2.2 将 Graph Data Catalog UXML 移入 Data page，删除作为两个页签同级元素的布局
- [x] 2.3 将 Data 与 Inspector page 的可见性收口为唯一页签状态，不允许 Catalog 在 Inspector page 可见或接收输入
- [x] 2.4 保持打开 Graph、下钻、返回和 authoring context 切换后默认进入 Data page
- [x] 2.5 保持选中 Node、Edge、StateMachine 或 Transition 后自动切换到 Inspector page
- [x] 2.6 为手动打开 Inspector 且没有选择的情况建立 Graph Authoring Settings 内容入口
- [x] 2.7 更新窄栏布局与滚动容器，确保 Data 与 Inspector 各自只拥有本页需要的滚动区域

## 3. Graph Authoring Settings 属性面

- [x] 3.1 为 Tree 属性投影定义明确的 authoring surface 过滤规则
- [x] 3.2 让 Graph Authoring Settings 只显示可编辑、可序列化且属于当前图的作者字段
- [x] 3.3 让 `BaseTreeAsset` 外部 Inspector 与 TreeWindow 复用同一 authoring property surface，避免产生两条字段筛选规则
- [x] 3.4 删除 `RunnableTree.Running` 的旧 `ShowInInspector` 展示注解
- [x] 3.5 删除 `RunnableTree.State` 的旧 `ShowInInspector` 展示注解
- [x] 3.6 删除由通用属性扫描显示运行时生命周期字段的残留路径
- [x] 3.7 确保空 Graph Settings 不构造伪配置字段、默认值或 runtime 镜像

## 4. Data Catalog 紧凑筛选与命令布局

- [x] 4.1 将 Source 筛选改为 `All`、`Input`、`Blackboard` 的紧凑显式切换
- [x] 4.2 保持搜索框始终可见，并继续覆盖名称、类型、category、owner 与 source
- [x] 4.3 将 Blackboard Context 与 Scope 收口到按需显示的筛选 surface
- [x] 4.4 让 Input source 选择不显示或伪造 Blackboard Context/Scope 条件
- [x] 4.5 保持 Blackboard filter 生效时 Input 条目不被赋予虚假 scope、owner 或编辑能力
- [x] 4.6 保持新增按钮只创建当前 owner 的 Blackboard declaration，Input 不获得写入入口
- [x] 4.7 保持条目展开、分组折叠、搜索与 filter 状态为 editor-only view state

## 5. Live Debug 边界与旧 UI 清理

- [x] 5.1 保持 `Authoring / Live Debug` 为 TreeWindow 级模式，不放入 Data 或普通 Inspector 内容区
- [x] 5.2 保持 Live Debug 下 authoring 命令只读，并继续由 RuntimeDebugSession 驱动 Graph overlay
- [x] 5.3 确认 Data、Inspector 与 Graph Settings 不直接持有或轮询 runtime Graph、Node、Track、Clip 或 Tree state
- [x] 5.4 删除旧 Graph property page、旧样式选择器和不再使用的成员命名
- [x] 5.5 使用 `rg` 确认不存在重复 Catalog、`Running / State` Inspector 展示或平行 runtime state 字段入口

## 6. 静态校验与规格收口

- [x] 6.1 使用 required flags 编译受影响的 Editor assembly
- [x] 6.2 编译结束后立即执行 `dotnet build-server shutdown`
- [x] 6.3 运行 `openspec validate refactor-tree-inspector-information-architecture --strict --no-interactive`

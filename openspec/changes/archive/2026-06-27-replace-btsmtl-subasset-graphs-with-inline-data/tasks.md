# Tasks

## 1. 清理错误的 subasset ownership 路径
- [x] 1.1 删除 `EmbeddedGraphOwnershipUtility` 代码文件和工程引用。
- [x] 1.2 删除 `BaseTreeView` 创建节点后调用 embedded graph 创建的逻辑。
- [x] 1.3 删除 `BaseTreeView` 删除节点前后确认和递归删除 embedded graph 的逻辑。
- [x] 1.4 删除粘贴流程里补建 embedded graph 的逻辑。
- [x] 1.5 删除 Transition rule graph 的 subasset 创建、删除和 ownership 判断入口。
- [x] 1.6 删除依赖 `AssetDatabase.IsSubAsset` 表达 graph ownership 的校验逻辑。

## 2. 拆分图数据和 Unity asset 身份
- [x] 2.1 将 `BaseGraph` 从 `ScriptableObject` 改为普通 C# 可序列化图数据基类。
- [x] 2.2 保留 `BaseGraph` 的节点、普通边、属性边、暴露属性和 GUID 映射为唯一正式集合。
- [x] 2.3 保留 `BaseGraph` 的结构编辑 API，避免在 asset 外壳中复制第二套实现。
- [x] 2.4 新增 `BaseTreeAsset` 作为 Unity `ScriptableObject` 资产入口。
- [x] 2.5 让 `BaseTreeAsset` 持有一个正式 `BaseTree` / `BaseGraph` 数据实例。
- [x] 2.6 调整 `TreeWindowUtility`、`BaseTreeWindow`、`BaseTreeView` 从 asset 外壳解析当前编辑的 graph data。
- [x] 2.7 调整 `BaseTreeInspector` 仍然打开 shared graph asset，但编辑内容来自 graph data。

## 3. 定义内联和 shared graph 引用模型
- [x] 3.1 新增正式 graph reference 数据结构，表达 inline graph data 和 shared asset 二选一。
- [x] 3.2 让引用解析规则固定为 shared asset 优先，否则 inline graph data。
- [x] 3.3 切换到 shared asset 时清理 inline graph data。
- [x] 3.4 切换回 inline 时要求创建新的 inline graph data，不从 shared asset 隐式复制。
- [x] 3.5 UI 明确显示当前引用是 `Inline` 还是 `Shared Asset`。
- [x] 3.6 删除所有“引用为空时临时创建 fallback graph”的逻辑。

## 4. 改造 StateMachineNode
- [x] 4.1 让 `StateMachineNode` 默认持有内联 `StateMachineGraph` 数据。
- [x] 4.2 创建 `StateMachineNode` 时初始化内联状态机图。
- [x] 4.3 初始化状态机图时创建一个 `Enter`、一个 `AnyState`、一个 `Exit` 和一个默认 `StateNode`。
- [x] 4.4 初始化默认 `Enter -> StateNode` Transition。
- [x] 4.5 让 `StateMachineNode` 下钻打开内联状态机图数据。
- [x] 4.6 让 `StateMachineNode` 支持显式绑定 shared `StateMachineGraph` asset。
- [x] 4.7 删除 `StateMachineNode` 对 owned embedded `StateMachineGraph` 的依赖。

## 5. 改造 StateNode
- [x] 5.1 让 `StateNode` 默认持有内联状态行为 graph data。
- [x] 5.2 初始化默认状态行为 graph 时创建正确入口节点。
- [x] 5.3 支持 `StateBehaviorSubTree` 的 `OnEnter`、`RootNode`、`OnExit` 生命周期入口。
- [x] 5.4 支持普通 `SubTree` 只使用 `RootNode`。
- [x] 5.5 让 `StateNode` 下钻打开内联状态行为图数据。
- [x] 5.6 让 `StateNode` 支持显式绑定 shared 状态行为 asset。
- [x] 5.7 删除 `StateNode` 对 owned embedded `SubTree` 的依赖。

## 6. 改造 Transition rule graph
- [x] 6.1 让 Transition edge 默认持有内联 `TransitionRuleGraph` 数据。
- [x] 6.2 合法 Transition 连接和缺失规则修复创建内联 rule graph data，不创建 subasset。
- [x] 6.3 双击 Transition 或点击 rule 命令打开内联 rule graph data。
- [x] 6.4 让 Transition 支持显式绑定 shared `TransitionRuleGraph` asset。
- [x] 6.5 删除 Transition 删除时递归销毁 rule subasset 的逻辑。
- [x] 6.6 保持 Transition 本体仍是同层 `BaseEdge`，不新增 `TransitionNode`。

## 7. 改造运行时图实例
- [x] 7.1 新增正式 graph runtime clone 工具。
- [x] 7.2 让 `StateMachineNode` 从 resolved graph data 创建运行工作副本。
- [x] 7.3 让 `StateNode` 从 resolved 状态行为 graph data 创建运行工作副本。
- [x] 7.4 让 `TransitionRuleGraphRuntime` 从 resolved rule graph data 创建运行工作副本。
- [x] 7.5 删除运行时对 `Object.Instantiate(ScriptableObject graph)` 的通用依赖。
- [x] 7.6 保持运行上下文通过 `BaseGraph.User` 和 `BaseGraph.DeltaTime` 传递。

## 8. 更新校验和清理规则
- [x] 8.1 校验 `StateMachineGraph` 每层只有一个 `Enter`、一个 `AnyState`、一个 `Exit`。
- [x] 8.2 校验 `StateMachineGraph` 至少有一个 `StateNode`。
- [x] 8.3 校验 `StateNode` 状态行为引用不能同时持有 inline 和 shared 两份真数据。
- [x] 8.4 校验 Transition rule 引用不能同时持有 inline 和 shared 两份真数据。
- [x] 8.5 校验 shared asset 删除不由 owner 删除流程触发。
- [x] 8.6 删除旧 embedded ownership 校验。

## 9. 更新文档和规格
- [x] 9.1 更新 `openspec/project.md` 中 BTSMTL authoring 方向。
- [x] 9.2 更新 current specs 中 embedded subasset 口径。
- [x] 9.3 确认 active change delta 与 current specs 不再互相矛盾。
- [x] 9.4 运行 `openspec validate replace-btsmtl-subasset-graphs-with-inline-data --strict --no-interactive`。
- [x] 9.5 运行相关 C# 工程编译检查。

## 10. 同步 inline-first UI 心智
- [x] 10.1 在 current specs 和 change delta 中补充下钻引用 UI 规则。
- [x] 10.2 移除 graph reference module 字段在节点画布配置面板中的默认暴露。
- [x] 10.3 让左侧 Inspector 在选中 `StateMachineNode` 时显示 ownership、Open、shared asset 和 Extract Shared。
- [x] 10.4 让左侧 Inspector 在选中 `StateNode` 时显示 ownership、Open、shared asset 和 Extract Shared。
- [x] 10.5 将 Transition rule Inspector 按钮从存储术语改成业务操作文案。
- [x] 10.6 运行 `openspec validate replace-btsmtl-subasset-graphs-with-inline-data --strict --no-interactive`。
- [x] 10.7 运行相关 C# 工程编译检查。
- [x] 10.8 修复 `BaseTreeAsset` 序列化回调中读取 Unity object `name` 的导入期错误。

## 11. 收紧 Transition rule 生命周期
- [x] 11.1 更新 spec，明确合法 Transition 连线即创建 inline rule graph。
- [x] 11.2 更新 spec，明确 deleted shared rule asset 自动断联并回到 inline rule graph。
- [x] 11.3 新增 `TransitionRuleGraph` 默认图创建入口。
- [x] 11.4 在 `BaseGraphAuthoring.Link()` 创建合法 SM Transition 时自动绑定 inline rule graph。
- [x] 11.5 在 `StateMachineGraph.CheckInit()` 修复缺失或 deleted shared rule graph 引用。
- [x] 11.6 复用默认图创建入口，清理重复创建 rule graph 的代码。
- [x] 11.7 运行 `openspec validate replace-btsmtl-subasset-graphs-with-inline-data --strict --no-interactive`。
- [x] 11.8 运行相关 C# 工程编译检查。
- [x] 11.9 移除 Transition UI 中的 `Create Rule` 和 inline 清空入口。
- [x] 11.10 让 shared rule 断联回到 inline rule graph。
- [x] 11.11 inline ownership 下不显示空的 shared asset 字段。

## 12. 节点显示名 Inspector
- [x] 12.1 在 `BaseNode` editor authoring 数据中新增可序列化 `DisplayName`。
- [x] 12.2 统一节点显示名解析：自定义显示名优先，空值回退节点类型显示名。
- [x] 12.3 让画布节点标题使用统一显示名解析。
- [x] 12.4 让左侧 Inspector 选中节点时显示并编辑 `Display Name`。
- [x] 12.5 让 Transition From/To 和默认 rule graph 命名使用节点显示名。

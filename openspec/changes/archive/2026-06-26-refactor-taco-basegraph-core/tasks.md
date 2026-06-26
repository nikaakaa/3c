## 1. 边界确认
- [x] 1.1 确认 `BaseTree` 当前直接持有节点、边、属性边、暴露属性和 GUID 映射。
- [x] 1.2 确认 `Tree_Extension.cs` 当前直接在 `BaseTree` 上实现结构编辑操作。
- [x] 1.3 确认编辑器窗口入口仍然以 `BaseTree` 为参数。
- [x] 1.4 确认 `BaseNode.Owner` 当前是 `BaseTree`。
- [x] 1.5 确认 `BaseEdge.Owner` 当前是 `BaseTree`。
- [x] 1.6 确认 Graph 引用模块当前保存 `BaseTree` 资产引用。

## 2. 新增 BaseGraph 底座
- [x] 2.1 新增 `BaseGraph : ScriptableObject`。
- [x] 2.2 将节点列表迁移到 `BaseGraph`。
- [x] 2.3 将普通边列表迁移到 `BaseGraph`。
- [x] 2.4 将属性边列表迁移到 `BaseGraph`。
- [x] 2.5 将暴露属性列表迁移到 `BaseGraph`。
- [x] 2.6 将节点 GUID 映射迁移到 `BaseGraph`。
- [x] 2.7 将普通边 GUID 映射迁移到 `BaseGraph`。
- [x] 2.8 将属性边 GUID 映射迁移到 `BaseGraph`。
- [x] 2.9 将暴露属性 GUID 映射迁移到 `BaseGraph`。
- [x] 2.10 将暴露属性名称映射迁移到 `BaseGraph`。
- [x] 2.11 将运行期 User/ID/IsValid 图上下文迁移到 `BaseGraph`。

## 3. 迁移图结构操作
- [x] 3.1 将初始化图映射和节点初始化流程迁移到 `BaseGraph`。
- [x] 3.2 将释放图映射和节点释放流程迁移到 `BaseGraph`。
- [x] 3.3 将 `GetInputEdges` 迁移到 `BaseGraph`。
- [x] 3.4 将 `GetOutputEdges` 迁移到 `BaseGraph`。
- [x] 3.5 将 `CanCreateNodeType` 默认实现迁移到 `BaseGraph`。
- [x] 3.6 将 `CreateNode` 迁移到 `BaseGraph`。
- [x] 3.7 将 `DeleteNode` 迁移到 `BaseGraph`。
- [x] 3.8 将 `AddNode` 迁移到 `BaseGraph`。
- [x] 3.9 将 `RemoveNode` 迁移到 `BaseGraph`。
- [x] 3.10 将 `Link` 迁移到 `BaseGraph`。
- [x] 3.11 将 `UnLink` 迁移到 `BaseGraph`。
- [x] 3.12 将 `AddLink` 迁移到 `BaseGraph`。
- [x] 3.13 将 `RemoveLink` 迁移到 `BaseGraph`。
- [x] 3.14 将 `LinkProperty` 迁移到 `BaseGraph`。
- [x] 3.15 将 `UnLinkProperty` 迁移到 `BaseGraph`。
- [x] 3.16 将 `AddPropertyLink` 迁移到 `BaseGraph`。
- [x] 3.17 将 `RemovePropertyLink` 迁移到 `BaseGraph`。
- [x] 3.18 将 `Refresh` 迁移到 `BaseGraph`。
- [x] 3.19 将 `CheckInit` 迁移到 `BaseGraph`。
- [x] 3.20 将暴露属性创建和删除迁移到 `BaseGraph`。

## 4. 保持 BaseTree 为编辑器资产入口
- [x] 4.1 修改 `BaseTree` 继承 `BaseGraph`。
- [x] 4.2 保留 `BaseTree` 上的 `[TreeWindow]`。
- [x] 4.3 保留 `BaseTree` 上的 `[AcceptableNodePaths]`。
- [x] 4.4 保持 `BaseTreeWindow` 的 `Tree` 类型为 `BaseTree`。
- [x] 4.5 保持 `BaseTreeView.PopulateView(BaseTree tree)` 的入口类型为 `BaseTree`。
- [x] 4.6 保持 `BaseTreeInspector` 的 `CustomEditor(typeof(BaseTree), true)`。
- [x] 4.7 保持 `TreeWindowUtility.OpenTree(BaseTree tree)`。
- [x] 4.8 不新增 `BaseGraphWindow`。

## 5. 调整节点和边归属
- [x] 5.1 将 `BaseNode.Owner` 类型改为 `BaseGraph`。
- [x] 5.2 将 `BaseNode.Init` 参数改为 `BaseGraph`。
- [x] 5.3 修正 `BaseNode.GetGraphReferences` 中依赖 `BaseTree` 的资产引用判断。
- [x] 5.4 将 `BaseEdge.Owner` 类型改为 `BaseGraph`。
- [x] 5.5 将 `BaseEdge.Init` 参数改为 `BaseGraph`。
- [x] 5.6 将 `PropertyEdge.Init` 参数改为 `BaseGraph`。
- [x] 5.7 修正依赖 `edge.Owner` 的编辑器菜单和验证代码。
- [x] 5.8 修正依赖 `node.Owner` 的 Timeline、SubTree、StateMachine 代码。

## 6. 保持 Graph 引用资产边界
- [x] 6.1 保持 `NodeGraphReference.Tree` 为 `BaseTree`。
- [x] 6.2 保持 `TreeReferenceModule` 保存 `BaseTree`。
- [x] 6.3 保持 `ScopedGraphReferenceModule` 保存 `BaseTree`。
- [x] 6.4 保持下钻 UI 通过 `BaseTree.OpenTree()` 打开引用资产。
- [x] 6.5 不允许 Graph 引用模块保存并行的 `BaseGraph` 字段。

## 7. 运行时语义不下沉
- [x] 7.1 保持 `RunnableTree : BaseTree`。
- [x] 7.2 保持 `RunnableTree` 持有 `Running`。
- [x] 7.3 保持 `RunnableTree` 持有 `State`。
- [x] 7.4 保持 `RunnableTree` 持有 `DeltaTime`。
- [x] 7.5 保持 `UpdateTree` 在 `RunnableTree`。
- [x] 7.6 保持 `ResetTree` 在 `RunnableTree`。
- [x] 7.7 确认 `BaseGraph` 不新增运行时状态字段。

## 8. 清理旧路径和重复路径
- [x] 8.1 删除 `BaseTree` 中迁移后的重复节点列表。
- [x] 8.2 删除 `BaseTree` 中迁移后的重复边列表。
- [x] 8.3 删除 `BaseTree` 中迁移后的重复属性边列表。
- [x] 8.4 删除 `BaseTree` 中迁移后的重复暴露属性列表。
- [x] 8.5 删除 `BaseTree` 中迁移后的重复 GUID 映射。
- [x] 8.6 删除迁移后只转发但已经没有调用点的旧包装方法。
- [x] 8.7 确认没有新增 Workbench 图路径。
- [x] 8.8 确认没有新增并行端口描述符。
- [x] 8.9 确认没有新增旧数据 fallback 字段。

## 9. 静态校验
- [x] 9.1 用文本搜索确认 `BaseTree : BaseGraph` 是唯一继承入口。
- [x] 9.2 用文本搜索确认编辑器窗口没有改为直接打开 `BaseGraph`。
- [x] 9.3 用文本搜索确认 `BaseNode.Owner` 不再是 `BaseTree`。
- [x] 9.4 用文本搜索确认 `BaseEdge.Owner` 不再是 `BaseTree`。
- [x] 9.5 用文本搜索确认 `BaseGraph` 不包含 `UpdateTree`。
- [x] 9.6 用文本搜索确认没有 `BaseGraphWindow`。
- [x] 9.7 运行 C# 编译相关的本地静态检查或由 Unity Editor 编译反馈修正报错。

## 10. OpenSpec 收尾
- [x] 10.1 对照 `refactor-taco-componentized-node-authoring`，确认没有新增并行端口系统。
- [x] 10.2 对照 `add-runnable-timeline-node`，确认没有改变 TimelineNode 的业务身份。
- [x] 10.3 对照 `add-unified-sm-node-authoring`，确认 `StateMachineGraph : BaseTree` 仍成立。
- [x] 10.4 运行 `openspec validate refactor-taco-basegraph-core --strict --no-interactive`。
- [x] 10.5 只有全部实现完成后再把任务勾选为完成。


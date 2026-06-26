## 1. 现状梳理
- [x] 1.1 确认 `BaseNodeView` 双击节点标题的下钻入口。
- [x] 1.2 确认 `BaseNodeView` 右键 `Open Reference` 的打开入口。
- [x] 1.3 确认 `TreeWindowUtility.OpenTree()` 当前直接打开资产的路径。
- [x] 1.4 确认 `BaseTreeWindow.SelectTree()` 当前刷新 GraphView 和 Inspector 的职责。
- [x] 1.5 确认 `BaseTreeWindow.m_OpenedTrees` 只用于窗口生命周期清理，不当作导航历史。

## 2. 导航模型
- [x] 2.1 新增 editor-only 页面栈条目类型。
- [x] 2.2 页面栈条目保存目标 `BaseTree`。
- [x] 2.3 页面栈条目保存显示名。
- [x] 2.4 页面栈条目保存来源 `BaseTree`。
- [x] 2.5 页面栈条目保存来源节点 GUID。
- [x] 2.6 页面栈条目保存 Graph 引用 key。
- [x] 2.7 确认页面栈不序列化到 `BaseTree` 或节点资产。

## 3. BaseTreeWindow 导航入口
- [x] 3.1 在 `BaseTreeWindow` 中保存页面栈列表。
- [x] 3.2 新增 `ReplaceNavigationRoot(BaseTree tree)`。
- [x] 3.3 `ReplaceNavigationRoot` 清空旧页面栈。
- [x] 3.4 `ReplaceNavigationRoot` 将目标图作为根页面。
- [x] 3.5 新增 `PushReferencedTree(BaseNode sourceNode, NodeGraphReference reference)`。
- [x] 3.6 `PushReferencedTree` 在引用为空时不改变页面栈。
- [x] 3.7 `PushReferencedTree` 将引用图追加为下一页。
- [x] 3.8 新增 `PopNavigationPage()` 返回上一页。
- [x] 3.9 新增 `PopNavigationTo(int index)` 回到 breadcrumb 指定页面。
- [x] 3.10 页面切换继续复用 `SelectTree()` 刷新窗口内容。
- [x] 3.11 关闭窗口时清理页面栈。

## 4. 直接打开资产行为
- [x] 4.1 调整 `TreeWindowUtility.GetWindow()` 或相关入口，使直接打开资产时 replace root。
- [x] 4.2 `OnOpenAsset` 打开 `BaseTree` 时使用 replace root。
- [x] 4.3 Inspector 的 Open 按钮打开 `BaseTree` 时使用 replace root。
- [x] 4.4 Tree Browser 打开图时使用 replace root。
- [x] 4.5 直接打开同一个图时不重复追加页面栈。

## 5. 节点下钻行为
- [x] 5.1 `BaseNodeView.OnTitleMouseDown` 双击时走 `PushReferencedTree`。
- [x] 5.2 右键 `Open Reference/Graph` 走 `PushReferencedTree`。
- [x] 5.3 多个 Graph 引用时沿用当前第一个可用引用作为双击目标。
- [x] 5.4 右键菜单继续列出每个 Graph 引用项。
- [x] 5.5 引用为空时菜单项禁用。
- [x] 5.6 下钻不调用 `TreeWindowUtility.OpenTree()` 直接替换窗口栈根。

## 6. Breadcrumb UI
- [x] 6.1 在 `BaseTreeWindow` 顶部增加导航 toolbar 容器。
- [x] 6.2 增加 Back 按钮。
- [x] 6.3 Back 按钮在根页面禁用。
- [x] 6.4 渲染页面栈 breadcrumb。
- [x] 6.5 当前页面 breadcrumb segment 不执行跳转。
- [x] 6.6 点击中间 breadcrumb segment 调用 `PopNavigationTo(index)`。
- [x] 6.7 根页面显示 Graph asset 名。
- [x] 6.8 下钻页面优先显示来源节点显示名。
- [x] 6.9 来源节点缺失时显示 Graph asset 名。
- [x] 6.10 更新 USS 使 toolbar、Back 和 breadcrumb 不遮挡 GraphView。

## 7. 与现有窗口行为对齐
- [x] 7.1 `Undo/Redo` 后保持当前页面栈。
- [x] 7.2 `SelectTree()` 仍更新 `CurrentSelectedTree`。
- [x] 7.3 `TreeWindowUtility.OnOpened` 事件继续触发。
- [x] 7.4 `SubTreeWindow` 继承 `BaseTreeWindow` 的导航行为时不需要单独分支。
- [x] 7.5 页面栈不修改 `m_OpenedTrees` 的清理职责。

## 8. OpenSpec 和编译检查
- [x] 8.1 运行 `openspec validate add-taco-editor-navigation-stack --strict --no-interactive`。
- [x] 8.2 刷新 Unity 脚本编译并读取 console 错误。

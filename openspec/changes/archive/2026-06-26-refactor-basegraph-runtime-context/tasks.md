# Tasks

## 1. BaseGraph 运行上下文
- [x] 1.1 在 `BaseGraph` 上新增非序列化 `DeltaTime` 存储。
- [x] 1.2 在 `BaseGraph` 上暴露只读 `DeltaTime`。
- [x] 1.3 在 `BaseGraph` 上新增正式写入本帧时间的方法。
- [x] 1.4 在 `BaseGraph` 上新增类型化读取 `User` 的方法。
- [x] 1.5 在 `InitTree(object user)` 中继续只接收外部传入的正式上下文。
- [x] 1.6 在 `DisposeTree()` 中清空 `User` 和本帧时间。

## 2. RunnableTree 时间来源收口
- [x] 2.1 移除 `RunnableTree` 独占的 `DeltaTime` 存储。
- [x] 2.2 让 `RunnableTree.UpdateTree(float deltaTime)` 写入 `BaseGraph` 上下文。
- [x] 2.3 确认 `OneRootTree` 读取的是继承自 `BaseGraph` 的 `DeltaTime`。
- [x] 2.4 确认 `TimelineRunningTree` 读取的是继承自 `BaseGraph` 的 `DeltaTime`。

## 3. StateMachineGraphRuntime 上下文传播
- [x] 3.1 让 `StateMachineGraphRuntime.Update(float deltaTime)` 先写入当前 `StateMachineGraph` 的时间上下文。
- [x] 3.2 保持 `Root` 和 `Enter` 只作为首次进入源，不持续 tick。
- [x] 3.3 保持 active state tick、AnyState 检查和 Exit 完成语义不变。
- [x] 3.4 确认 transition 条件仍通过现有 `PropertyPort` / `PropertyEdge` 读取。

## 4. StateMachineNode 下钻上下文
- [x] 4.1 让 `StateMachineNode` 从 `Owner.DeltaTime` 解析本帧时间。
- [x] 4.2 删除 `StateMachineNode` 对 `RunnableTree.DeltaTime` 的类型判断。
- [x] 4.3 删除 `Owner?.User ?? Owner` 隐式上下文 fallback。
- [x] 4.4 让下钻 Graph 只继承父 Graph 的正式 `User`。
- [x] 4.5 保持 `StateMachineGraph` 下钻从 `Enter` 入口进入。
- [x] 4.6 保持下钻到 `RunnableTree` 时仍调用 `UpdateTree(deltaTime)`。

## 5. TimelineNode 运行上下文
- [x] 5.1 删除 `TimelineNode` 对 `Owner is RunnableTree` 的要求。
- [x] 5.2 让 `TimelineNode` 使用 `Owner.DeltaTime` 驱动 `Timeline.Evaluate(deltaTime)`。
- [x] 5.3 让 `TimelineNode` 通过 `Owner` 的类型化 `User` 读取 `ITimelinePlayerProvider`。
- [x] 5.4 缺失 Timeline asset 时继续返回 `Failure`。
- [x] 5.5 缺失 TimelinePlayer provider 时继续返回 `Failure`。
- [x] 5.6 保持每个 `TimelineNode` 实例化独立运行时 Timeline。

## 6. TreeRunner 正式上下文配置
- [x] 6.1 在 `TreeRunner` 上新增正式 runtime user 序列化字段。
- [x] 6.2 让 `InitTree()` 传入该 runtime user 字段。
- [x] 6.3 不使用 `this` 作为隐式 fallback。
- [x] 6.4 不自动 `GetComponent` 查找 TimelinePlayer provider。
- [x] 6.5 保持 `CloneTree`、`DisposeTree`、`UpdateTree`、`ResetTree` 现有生命周期入口。

## 7. 旧上下文路径清理
- [x] 7.1 搜索并清理通用运行上下文里的 `Owner is RunnableTree` 判断。
- [x] 7.2 搜索并清理 `InitTree(Owner?.User ?? Owner)`。
- [x] 7.3 搜索并清理不再需要的局部 `m_DeltaTime` 缓存。
- [x] 7.4 确认没有新增 Graph adapter 或 `IRunnableGraph`。
- [x] 7.5 确认没有新增 Workbench port 或并行端口注册表。

## 8. OpenSpec 校验
- [x] 8.1 运行 `openspec validate refactor-basegraph-runtime-context --strict --no-interactive`。
- [x] 8.2 修复所有 OpenSpec 校验问题。

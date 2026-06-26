## 1. 收口规格口径
- [x] 1.1 current spec 删除 `StateMachineGraph` 内的 Root 要求。
- [x] 1.2 current spec 删除 `StateNode.Behavior` inline behavior 要求。
- [x] 1.3 current spec 明确 `StateMachineGraph` 只允许 `Enter/AnyState/Exit/StateNode/ValueNode`。
- [x] 1.4 current spec 明确 `StateNode` 下钻状态行为 `SubTree` 才包含 Taco `RootNode` 和行为节点。
- [x] 1.5 同步 active change 的 proposal、design 和 spec delta。

## 2. 修改状态机结构节点
- [x] 2.1 删除 `StateMachineRootNode` 类型。
- [x] 2.2 删除 `StateMachinePorts.Behavior`。
- [x] 2.3 保留 `Enter`、`AnyState`、`Exit` 控制节点。
- [x] 2.4 保持控制节点只能存在于 `StateMachineGraph`。

## 3. 修改 Graph 创建边界
- [x] 3.1 普通行为图允许创建 `StateMachineNode`。
- [x] 3.2 普通行为图禁止创建 `StateNode` 和状态机控制节点。
- [x] 3.3 `StateMachineGraph` 允许创建 `Enter`、`AnyState`、`Exit`。
- [x] 3.4 `StateMachineGraph` 允许创建 `StateNode`。
- [x] 3.5 `StateMachineGraph` 允许创建 `ValueNode` 作为 Transition 条件节点。
- [x] 3.6 `StateMachineGraph` 禁止创建 `StateMachineNode`。
- [x] 3.7 `StateMachineGraph` 禁止创建 Taco 原生 `RootNode` 和普通 `RunnableNode` 行为节点。
- [x] 3.8 新建 `StateMachineGraph` 默认只创建 `Enter`、`AnyState`、`Exit`。

## 4. 修改 StateNode 行为来源
- [x] 4.1 `StateNode` 只暴露 `StateIn` 和 `StateOut` flow port。
- [x] 4.2 删除 `StateNode` inline behavior edge 缓存。
- [x] 4.3 删除 `StateNode` inline behavior tick。
- [x] 4.4 `StateNode` 继续通过正式状态行为引用模块引用状态行为 `SubTree`。
- [x] 4.5 `StateNode` 引用 `SubTree` 时 tick `SubTree.UpdateTree(deltaTime)`。
- [x] 4.6 层级状态机通过 `SubTree` 内的 `StateMachineNode` 表达。
- [x] 4.7 `StateNode` 没有 `SubTree` 时保持 `Running`。

## 5. 修改状态机 runtime
- [x] 5.1 `StateMachineGraphRuntime` 删除 Root 入口分支。
- [x] 5.2 `StateMachineGraphRuntime` 初始状态只从 `Enter.StateOut` 解析。
- [x] 5.3 `StateMachineNode` 驱动状态机时使用同一套 Enter 入口。
- [x] 5.4 `StateMachineGraphRuntime.ActiveState` 保持 `StateNode`。
- [x] 5.5 Stop/Reset 继续传播到当前 active `StateNode`。

## 6. 修改编辑器连接过滤
- [x] 6.1 `StateMachineGraph` 中只允许 `StateOut -> StateIn` Transition flow。
- [x] 6.2 删除 `StateNode.Behavior -> RunnableNode.Input` 兼容规则。
- [x] 6.3 删除 `StateMachineGraph` 内普通 `RunnableNode.Output -> Input` 兼容规则。
- [x] 6.4 Transition 起点只允许 `Enter`、`AnyState`、`StateNode`。
- [x] 6.5 Transition 终点只允许 `StateNode`、`Exit`。
- [x] 6.6 `PropertyPort` 值口连接不受影响。

## 7. 修改嵌套 Graph 校验
- [x] 7.1 删除 Root 缺失/重复校验。
- [x] 7.2 校验 `Enter`、`AnyState`、`Exit` 各自唯一。
- [x] 7.3 校验至少一个 `StateNode`。
- [x] 7.4 校验 `StateMachineGraph` 不包含 `StateMachineNode`。
- [x] 7.5 校验 `StateMachineGraph` 不包含普通 runnable 行为节点。
- [x] 7.6 校验 Transition 端点规则。
- [x] 7.7 保留 Graph 引用循环检测。

## 8. 工具校验
- [x] 8.1 运行 `openspec validate split-taco-sm-state-node-authoring --strict --no-interactive`。
- [x] 8.2 运行 `openspec validate --all --strict --no-interactive`。

## 9. 拆分普通 SubTree 与 StateBehaviorSubTree 生命周期入口
- [x] 9.1 新增 `OnEnter` 固定入口节点。
- [x] 9.2 新增 `OnExit` 固定入口节点。
- [x] 9.3 普通 `SubTree` 新建时只创建 `RootNode`。
- [x] 9.4 新增 `StateBehaviorSubTree : SubTree`。
- [x] 9.5 `StateBehaviorSubTree` 新建时默认创建 `OnEnter`、`RootNode`、`OnExit`。
- [x] 9.6 `StateBehaviorSubTree.CheckInit()` 补齐缺失的生命周期入口。
- [x] 9.7 普通行为图和普通 `SubTree` 禁止直接创建状态生命周期入口节点。
- [x] 9.8 `StateBehaviorSubTree` 限制 `OnEnter`、`RootNode`、`OnExit` 各自唯一。
- [x] 9.9 `StateNode` 引用普通 `SubTree` 时直接执行 `RootNode`。
- [x] 9.10 `StateNode` 引用 `StateBehaviorSubTree` 时进入状态先执行 `OnEnter`。
- [x] 9.11 `StateNode` 引用 `StateBehaviorSubTree` 时 active 后执行 `RootNode`。
- [x] 9.12 `StateMachineGraphRuntime` 切出 `StateBehaviorSubTree` 状态前执行 `OnExit`。
- [x] 9.13 普通 `SubTree` 存在 `OnEnter` 或 `OnExit` 时校验报非法。
- [x] 9.14 嵌套 Graph 校验报告 `StateBehaviorSubTree` 缺失或重复 `OnEnter`、`RootNode`、`OnExit`。
- [x] 9.15 同步 current spec 和 active change spec delta。

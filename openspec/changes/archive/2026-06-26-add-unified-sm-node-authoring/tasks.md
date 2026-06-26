## 1. 清理旧状态路径
- [x] 1.1 删除 `InitialStateNodeGuid` 规格路径。
- [x] 1.2 删除 `StateMachineNodeRole` 规格路径。
- [x] 1.3 删除“普通 State 通过 role 切换为 Root/Enter/Exit”的编辑路径。
- [x] 1.4 确认普通 `StateMachineNode` 不挂 `Root/Enter/AnyState/Exit` 控制模块。
- [x] 1.5 确认不恢复旧 Locomotion、Action、FootPhase SO/config 数据路径。
- [x] 1.6 确认不新增 `GraphProfile`、`Behavior` 或 `StateMachineTree`。
- [x] 1.7 清理 `implementation.md`、spec delta 和任务中残留的 `StateMachineControlModule` 目标表述。
- [x] 1.8 明确第一阶段不新增 `TransitionIn/TransitionOut`、transition 专用上下文端口、并行 port registry 或 `IRunnableGraph`。

## 2. 普通 StateMachineNode
- [x] 2.1 保留 `StateMachineNode : RunnableNode` 作为普通状态节点。
- [x] 2.2 保留 `ScopedGraphReferenceModule` 作为下钻 Graph 引用。
- [x] 2.3 保留 `BaseNode -> NodeModule -> PropertyPort` 字段扫描链路。
- [x] 2.4 定义 `StateMachineNode` 在普通行为图中表示进入下一层 Graph。
- [x] 2.5 定义 `StateMachineNode` 在 `StateMachineGraph` 中表示同层 active state。
- [x] 2.6 移除普通状态 role UI 或 role 字段。
- [x] 2.7 `StateMachineNode` 可驱动 `StateMachineGraph` 子图。
- [x] 2.8 `StateMachineNode` 可驱动 `RunnableTree` 子图。
- [x] 2.9 定义 `StateMachineNode` 在非 `StateMachineGraph` 父图中不显示外层 `Output` flow port。
- [x] 2.10 实现 `StateMachineNode` 在普通行为图中只暴露 `Input` flow port。
- [x] 2.11 实现 `StateMachineNode` 在 `StateMachineGraph` 中暴露 `Input + Output` flow port。
- [x] 2.12 移除“父图通过 SMNode Output 串联后继节点”的规格表述和 UI 入口。
- [x] 2.13 明确并实现 active state 子 Graph 返回 Success 但没有命中 Exit transition 时的持续规则。

## 3. 上下文 Flow Port 声明
- [x] 3.1 新增轻量 flow port 声明数据结构，包含名称、方向和容量。
- [x] 3.2 在 `BaseNode` 提供默认 flow port 声明入口。
- [x] 3.3 默认 flow port 声明从现有 `InputAttribute` / `OutputAttribute` 生成。
- [x] 3.4 修改 `BaseNodeView.GeneratePorts()` 使用节点 flow port 声明，不直接遍历 attribute。
- [x] 3.5 保持生成结果仍使用 Taco 原生 port view 和 `BaseEdge`。
- [x] 3.6 确认该入口不影响 `PropertyPort`、`PropertyEdge` 和模块字段扫描。
- [x] 3.7 确认没有新增 `SMPort`、`TransitionPort`、并行 port registry 或并行 edge 类型。

## 4. 状态机控制节点
- [x] 4.1 保留 `StateMachineControlNode : BaseNode` 作为控制节点共同基类。
- [x] 4.2 新增 `StateMachineRootNode` 表达 Root。
- [x] 4.3 保留 `StateMachineEnterNode` 表达 Enter。
- [x] 4.4 保留 `StateMachineAnyStateNode` 表达 AnyState。
- [x] 4.5 保留 `StateMachineExitNode` 表达 Exit。
- [x] 4.6 确认不新增 `StateMachineControlModule`。
- [x] 4.7 确认普通 `StateMachineNode` 无法挂载控制语义。
- [x] 4.8 确认普通 `BaseTree` 无法创建控制节点。
- [x] 4.9 确认 `StateMachineGraph` 节点搜索能创建 Root、Enter、AnyState、Exit。
- [x] 4.10 确认 `StateMachineGraph` 限制 Root、Enter、AnyState、Exit 各自只能创建一个。
- [x] 4.11 控制节点不可删除、不可复制、不可分组，避免用户误以为它是普通状态。

## 5. StateMachineGraph 图资产
- [x] 5.1 保留 `StateMachineGraph : BaseTree` 作为状态机图资产边界。
- [x] 5.2 明确 `StateMachineGraph` 不继承 `RunnableTree`。
- [x] 5.3 普通 `BaseTree` 不序列化状态机字段。
- [x] 5.4 新建 `StateMachineGraph` 时默认创建 Root、Enter、AnyState、Exit。
- [x] 5.5 新建 `StateMachineGraph` 时不默认连接 `Root -> Enter`。
- [x] 5.6 `StateMachineGraph` 查询 Root、Enter、AnyState、Exit 时基于具体控制节点类型。
- [x] 5.7 `StateMachineGraph` 创建限制基于具体控制节点类型。
- [x] 5.8 新建 `StateMachineGraph` 时不自动创建第一个普通 `StateMachineNode`。

## 6. Transition 数据和编辑入口
- [x] 6.1 Transition 继续作为 edge 语义，不新增 `TransitionNode`。
- [x] 6.2 Transition 支持优先级。
- [x] 6.3 Transition 支持一个可选 Bool 条件引用。
- [x] 6.4 条件来源复用现有 property port 主链路。
- [x] 6.5 运行时按 `TransitionPriority` 排序检查 transition。
- [x] 6.6 运行时可读取 Bool property port 作为 transition 条件。
- [x] 6.7 Edge 右键菜单提供 priority 和 condition 入口。
- [x] 6.8 Edge 右键菜单支持 `Root/Enter -> State`、`AnyState -> State/Exit`、`State -> State/Exit`。
- [x] 6.9 拖拽 flow port 时只暴露状态机合法 Transition 端点。
- [x] 6.10 在 edge 视觉上显示 priority 和 condition 摘要。
- [x] 6.11 禁止 `AnyState` 无条件 transition。

## 7. Transition 端点规则
- [x] 7.1 允许 `Root -> StateMachineNode`。
- [x] 7.2 允许 `Enter -> StateMachineNode`。
- [x] 7.3 允许 `StateMachineNode -> StateMachineNode`。
- [x] 7.4 允许 `StateMachineNode -> Exit`。
- [x] 7.5 允许 `AnyState -> StateMachineNode`。
- [x] 7.6 允许 `AnyState -> Exit`。
- [x] 7.7 禁止 `Exit -> 任意节点`。
- [x] 7.8 禁止 `任意节点 -> Root`。
- [x] 7.9 禁止非 Root 节点进入 Enter。
- [x] 7.10 禁止 `任意节点 -> AnyState`。
- [x] 7.11 禁止非状态机节点参与 Transition。
- [x] 7.12 禁止跨层 Transition 连接父图或子图内部节点。

## 8. 状态机运行时
- [x] 8.1 行为树进入状态机图时运行时从 Root 激活第一个状态。
- [x] 8.2 父状态下钻下一层状态机图时运行时从 Enter 激活第一个状态。
- [x] 8.3 运行时维护当前 active `StateMachineNode`。
- [x] 8.4 每帧先检查 AnyState transition。
- [x] 8.5 每帧 tick 当前 active state 的下钻 Graph。
- [x] 8.6 active state transition 命中时 stop 当前状态并切换目标。
- [x] 8.7 transition 指向 Exit 时本层 Graph 返回 Success。
- [x] 8.8 父级 stop/reset 时传播到当前 active state 链路。
- [x] 8.9 `StateMachineGraph` 由 `StateMachineGraphRuntime` 解释，不要求自身实现 `UpdateTree()`。
- [x] 8.10 active state 子 Graph 返回 Success 但没有 transition 时保持 Running。

## 9. 状态机图验证
- [x] 9.1 验证每层 Graph 至少一个普通 `StateMachineNode`。
- [x] 9.2 验证每层 Graph 必须且只能有一个 Root。
- [x] 9.3 验证每层 Graph 必须且只能有一个 Enter。
- [x] 9.4 验证每层 Graph 必须且只能有一个 AnyState。
- [x] 9.5 验证每层 Graph 必须且只能有一个 Exit。
- [x] 9.6 验证 Root 不能有入边。
- [x] 9.7 验证 Root 至少有一条指向普通 State 的出边。
- [x] 9.8 验证 Enter 不能有入边。
- [x] 9.9 验证 Enter 至少有一条出边。
- [x] 9.10 验证 AnyState 不能有入边。
- [x] 9.11 验证 Exit 不能有出边。
- [x] 9.12 验证 Exit 必须有入边。
- [x] 9.13 验证 Transition 条件节点必须存在。
- [x] 9.14 验证 Transition 条件端口必须存在。
- [x] 9.15 验证 Transition 条件端口必须是 Bool。
- [x] 9.16 验证动态端口 ID 丢失时报告具体节点、模块和端口。
- [x] 9.17 验证状态机递归引用不能形成 Graph 循环。
- [x] 9.18 验证 AnyState 出边必须有 Bool 条件。
- [x] 9.19 验证普通行为图中的 `StateMachineNode` 不允许保存外层 Output flow edge。
- [x] 9.20 验证缺少普通 State 时不提前跳过控制节点验证。

## 10. 下钻和编辑体验
- [x] 10.1 双击 `StateMachineNode` 时进入它引用的下一层 Graph。
- [x] 10.2 Inspector 显示普通 SMNode 的图引用和模块字段。
- [x] 10.3 控制节点不显示普通状态 Graph 引用。
- [x] 10.4 控制节点视觉和能力收紧，避免用户误以为它是普通状态。
- [x] 10.5 状态机 edge 编辑体验能直接选择 Bool 条件端口并显示当前选择。
- [x] 10.6 状态机图缺失必需节点时提供可恢复的创建入口。
- [x] 10.7 Root 在状态机图中有独立视觉标识。

## 11. Timeline 接入边界
- [x] 11.1 记录 Timeline 不作为 `StateMachineGraph` 同层状态节点。
- [x] 11.2 记录 Timeline 通过 `StateMachineNode` 下钻到状态行为 Graph 接入。
- [x] 11.3 记录承载 `TimelineNode` 的状态行为 Graph 需要是 `RunnableTree` 体系。
- [x] 11.4 确认 `StateMachineGraph.CanCreateNodeType()` 不允许直接创建 `TimelineNode`。
- [x] 11.5 补齐 `TimelineNode` 的 `Output` flow port，使 Timeline 后续行为可在 UI 中串联。

## 12. 文档同步
- [x] 12.1 记录“普通状态是 `StateMachineNode`”。
- [x] 12.2 记录“Root/Enter/AnyState/Exit 是控制节点，不是普通状态”。
- [x] 12.3 记录“Root 是图运行根，不是 Taco RootNode”。
- [x] 12.4 记录“Enter 是状态区域入口，不再兼任图 root”。
- [x] 12.5 记录“普通 State 不挂控制模块”。
- [x] 12.6 记录“Transition 是 edge，不是 node”。
- [x] 12.7 记录“当前不做运行时编译导出”。
- [x] 12.8 同步 spec delta，使其不再要求 `StateMachineControlModule`。
- [x] 12.9 同步 `implementation.md`，使其不再写旧控制模块路线。
- [x] 12.10 运行 `openspec validate add-unified-sm-node-authoring --strict --no-interactive`。

## 1. 现状确认

- [x] 1.1 搜索 `IfNode` 的代码引用和节点菜单入口。
- [x] 1.2 搜索项目资产中是否存在序列化 `IfNode`。
- [x] 1.3 确认 `CompositeNode` 当前 child 缓存、flow order 和 reset 逻辑。
- [x] 1.4 确认 `SelectorNode` 当前 running child 逻辑。
- [x] 1.5 确认 `SequenceNode` 当前 running child 逻辑。
- [x] 1.6 确认 `ParallelNode` 当前 completed child 逻辑。
- [x] 1.7 确认 `BaseEdge` 当前 transition rule 字段和普通 BT edge 字段空白。
- [x] 1.8 确认 `BaseEdgeView` 当前只为 StateMachine transition 显示规则摘要。
- [x] 1.9 确认 `NestedGraphValidation` 当前如何校验普通行为图 edge。

## 2. 条件图命名收口

- [x] 2.1 将 `TransitionRuleGraph` 设计迁移为通用 `ConditionRuleGraph`。
- [x] 2.2 将 `TransitionRuleResultNode` 设计迁移为通用 `ConditionRuleResultNode`。
- [x] 2.3 将 transition rule runtime 设计迁移为 condition rule runtime。
- [x] 2.4 保留纯 ValueNode / PropertyPort / PropertyEdge 求值能力。
- [x] 2.5 保留规则图拒绝 `RunnableNode`、`TimelineNode`、`StateMachineNode` 和状态生命周期节点的限制。
- [x] 2.6 更新状态机 transition 引用到 `ConditionRuleGraph`。
- [x] 2.7 删除旧 `TransitionRuleGraph` 类型、菜单、文案和字段命名，不保留 alias。
- [x] 2.8 如果序列化类型迁移无法安全完成，停止并说明缺口。

## 3. BT edge decorator 数据模型

- [x] 3.1 为普通 BT composite output edge 增加 condition graph inline 引用。
- [x] 3.2 为普通 BT composite output edge 增加 shared condition graph asset 引用。
- [x] 3.3 保证 inline condition graph 与 shared asset 互斥。
- [x] 3.4 为普通 BT composite output edge 增加 `AbortPolicy` 字段。
- [x] 3.5 明确空 condition graph 表示 slot 无条件可进入。
- [x] 3.6 明确非法 condition graph 表示 slot 不可进入并报告校验错误。
- [x] 3.7 确认 StateMachine transition edge 的现有 priority 和 blend 元数据不被 BT edge decorator 字段污染。

## 4. Composite child slot runtime

- [x] 4.1 将 `CompositeNode` 运行缓存从 child 列表扩展为 child slot 列表。
- [x] 4.2 child slot 保存 edge、child、flow order、condition graph runtime 和 abort policy。
- [x] 4.3 child slot 初始化时按 edge flow order 保持稳定顺序。
- [x] 4.4 child slot dispose 时释放 condition graph runtime。
- [x] 4.5 child slot reset 时重置 child 和 condition graph runtime 状态。
- [x] 4.6 condition graph runtime 求值时继承当前 graph user 和 deltaTime。

## 5. Selector abort runtime

- [x] 5.1 Selector 无 running child 时选择第一个条件成立 slot。
- [x] 5.2 Selector running child 自身条件失效且 policy 为 `Self` 时 stop 当前 child。
- [x] 5.3 Selector running child 自身条件失效且 policy 为 `Both` 时 stop 当前 child。
- [x] 5.4 Selector 更高优先级 slot 条件成立且该 slot policy 为 `LowerPriority` 时 stop 当前低优先级 child。
- [x] 5.5 Selector 更高优先级 slot 条件成立且该 slot policy 为 `Both` 时 stop 当前低优先级 child。
- [x] 5.6 Selector child 返回 `Failure` 时继续尝试后续条件成立 slot。
- [x] 5.7 Selector child 返回 `Success` 时保持现有返回 `Success` 语义。

## 6. Sequence 和 Parallel runtime

- [x] 6.1 Sequence 进入 child 前必须检查该 slot condition。
- [x] 6.2 Sequence running child 自身条件失效且 policy 为 `Self` 时 stop child 并返回 `Failure`。
- [x] 6.3 Sequence 拒绝 `LowerPriority` 和 `Both` 配置。
- [x] 6.4 Parallel 每 tick 只更新条件成立的参与 child。
- [x] 6.5 Parallel running child 自身条件失效且 policy 为 `Self` 时 stop child 并移出参与集合。
- [x] 6.6 Parallel `JumpComplete` completed child 在条件失效后清出 completed 集合。
- [x] 6.7 Parallel 拒绝 `LowerPriority` 和 `Both` 配置。

## 7. 编辑器与校验

- [x] 7.1 在 BT edge context menu 增加 `Condition Rule/Open`。
- [x] 7.2 在 BT edge context menu 增加 `Condition Rule/Extract Shared`。
- [x] 7.3 在 BT edge context menu 增加 `Condition Rule/Use Inline Rule`。
- [x] 7.4 在 BT edge Inspector 显示 condition ownership。
- [x] 7.5 在 BT edge Inspector 显示 abort policy。
- [x] 7.6 在 BT edge 或 child slot 附近显示 condition summary。
- [x] 7.7 在节点搜索菜单中移除 `If`。
- [x] 7.8 校验 BT edge condition graph inline/shared 双持有为非法。
- [x] 7.9 校验 condition graph 缺失 result node 为非法。
- [x] 7.10 校验 condition graph 含 runnable 行为节点为非法。
- [x] 7.11 校验非 Selector edge 上的 `LowerPriority` 或 `Both` 为非法。
- [x] 7.12 校验资产中残留 `IfNode` 为非法结构。
- [x] 7.13 双击、右键 `Condition Rule/Open` 和 Inspector `Open Rule` 在 edge 没有条件引用时创建 inline `ConditionRuleGraph` 并打开。
- [x] 7.14 `Open Rule` 不静默替换 configured 但无法解析的 shared condition rule asset。

## 8. 旧 IfNode 清理

- [x] 8.1 删除 `IfNode` runtime 类。
- [x] 8.2 删除 `IfNode` meta 或确认 Unity 资产迁移后删除。
- [x] 8.3 删除 `IfNode` 节点路径注册。
- [x] 8.4 删除文档或示例中推荐 `IfNode` 的内容。
- [x] 8.5 如果发现 Corin RootTree 或状态行为图使用 `IfNode`，迁移到对应 Composite output edge condition。
- [x] 8.6 如果某个 `IfNode` 条件无法映射到 edge condition，停止并说明缺口。

## 9. Corin 配置收口

- [x] 9.1 检查 Corin RootTree 的顶层 Selector / Sequence / Parallel 分支。
- [x] 9.2 确认 Corin 当前没有纯 BT 攻击入口分支；攻击入口继续由 Action StateMachine transition edge condition 表达，未强行改成并行 BT edge condition。
- [x] 9.3 确认 Corin 当前没有闪避入口分支；未伪造 Dodge 分支或 fallback 条件路径。
- [x] 9.4 确认移动入口由常驻 Locomotion StateMachine 和其 transition condition 表达，未把并行 locomotion edge 改成会停 tick 的 BT condition。
- [x] 9.5 确认当前 Corin 顶层是 Parallel 并无需要抢占的 Selector 分支；LowerPriority/Both 能力已在 Selector edge runtime/editor/validation 支持，等待实际 Selector 分支配置。
- [x] 9.6 保持 StateMachine 内部状态切换仍使用状态机 Transition edge condition。
- [x] 9.7 确认 BT edge abort 不直接伪造 ActionLifecycleTransition。

## 10. 清理与验证

- [x] 10.1 搜索确认没有 `IfNode` 类型、菜单、节点路径或旧资产引用。
- [x] 10.2 搜索确认没有新增旧 Workbench、旧 BoolPort 条件或 fallback 条件路径。
- [x] 10.3 搜索确认普通 BT edge 和 StateMachine transition edge 都使用 `ConditionRuleGraph` 条件图。
- [x] 10.4 确认 `add-timeline-loop-playback-and-state-transition-blend` 中的 `TransitionRuleGraph` 文案已 rebase 为 `ConditionRuleGraph`。
- [x] 10.5 搜索确认 BT edge decorator、Composite runtime 和 condition graph 不直接引用 `IGameplaySyncPeer`、Fantasy Session、transport 或 backend mode。
- [x] 10.6 确认网络可见结果只通过 `SyncFacts -> CharacterNetworkSendStage -> CharacterGameplaySyncAdapter -> GameplaySyncRuntime` 输出。
- [x] 10.7 运行 `openspec validate refactor-bt-edge-condition-decorators --strict --no-interactive`。

# Change: BT Edge Condition Decorator 重构

## Why

普通 BT 分支条件现在通过执行树里的 `IfNode` 表达，无法支持 UE 式 edge decorator 和 lower priority abort。项目目标是让 RootTree 和状态行为图表达业务主流程，而不是被条件节点包裹污染；同时保持所有网络可见结果只通过正式 `SyncFacts` 和可插拔 `GameplaySyncRuntime` 链路输出。

## 背景

当前普通行为树的条件能力主要靠 `IfNode` 作为可执行 decorator 节点表达。这个模型能做局部门控，但不能表达 UE 行为树里更核心的语义：条件挂在 Composite 到 Child 的边上，Composite 根据条件选择 child，并在条件变化时按 abort 策略停止当前运行分支。

现在 `SelectorNode`、`SequenceNode` 和 `ParallelNode` 只缓存 child 列表和 flow order，没有 child edge 条件、没有 abort policy，也没有 lower priority 抢占扫描。继续用 `IfNode` 拼，会把“能不能进入分支”和“分支里面做什么”混在一起，主行为树会被判断节点污染，Timeline、Action、Motion 等节点也更容易因为局部 decorator 生命周期被错误 stop / restart。

状态机已经有一个更接近正确方向的先例：Transition 条件属于 edge，规则图是纯 Bool 条件，调度优先级也属于 edge。纯 BT 分支应该采用同一种 authoring 心智，但不能把状态机 Transition 语义直接搬进普通 BT。

## What Changes

### 目标

- 删除 `IfNode` 作为正式 authoring / runtime 节点，不保留兼容 alias、fallback 菜单或旧节点主路径。
- 将纯 BT 分支条件从执行节点迁移到 Composite output edge。
- 在 Composite output edge 上支持 inline/shared 条件图、条件摘要和下钻编辑。
- 双击 edge、右键 `Condition Rule/Open` 或 Inspector `Open Rule` 时，如果该 edge 没有条件引用，则创建 owner 内部 inline `ConditionRuleGraph` 并立即下钻编辑。
- 引入通用 `ConditionRuleGraph` 作为纯 Bool 条件图，替代仅以状态机命名的 `TransitionRuleGraph`。
- 让状态机 Transition 和 BT edge decorator 共用同一套条件图能力、ValueNode、PropertyPort 和 inline/shared ownership 规则。
- 在 BT edge 上支持 `AbortPolicy`: `None`、`Self`、`LowerPriority`、`Both`。
- 让 `Selector` 支持 lower priority 抢占，让 `Sequence` / `Parallel` 支持自身条件失效停止。
- 保持 BT runtime 完全由 gameplay logic tick 驱动，不引入异步黑板 observer 或渲染帧驱动打断。
- 保持网络层可插拔：BT edge decorator 不直接连接 peer、transport 或 Fantasy Session，只通过正式 `SyncFacts` 和 `GameplaySyncRuntime` adapter 链路影响网络输出。

### 非目标

- 不实现 Timeline 循环播放、状态切换动画混合或 motion warping；这些属于 `add-timeline-loop-playback-and-state-transition-blend`。
- 不新增测试任务；用户会做 Unity 端到端验证。
- 不运行 Unity batchmode。
- 不恢复旧 Workbench、旧 BoolPort 条件、旧 BBB registry 或旧节点注册路径。
- 不新增第二套 BT 条件图、临时 `IfNode` 兼容节点或自动 fallback 配置。
- 不把 BT edge decorator 当作 ActionRuntime、Timeline window 或网络权威；它只负责行为树分支选择和停止。
- 不新增 BT 专用网络 packet、BT abort packet、Graph 直连 `IGameplaySyncPeer` 或 Fantasy 专用路径。

### 方案摘要

把 Composite 的 child 从“纯 `RunnableNode` 列表”提升为“child slot”：slot 由 output `BaseEdge`、child node、可选 `ConditionRuleGraph`、`AbortPolicy` 和 flow order 组成。Composite runtime 每个 logic tick 按 slot 求值条件，再决定是否进入、继续或停止 child。

`ConditionRuleGraph` 是通用纯 Bool 图。它使用现有 `BaseGraph` / `BaseTree` 数据、ValueNode、PropertyPort、PropertyEdge、inline/shared graph reference 和唯一 result node。它不能创建 `RunnableNode`、`TimelineNode`、`StateMachineNode`、`StateNode` 或生命周期节点。状态机 Transition 和 BT edge decorator 都通过它求值，但各自传入不同 runtime facts。

`Selector` 是优先级 Composite。更高优先级 slot 的条件成立且该 slot 配置 `LowerPriority` 或 `Both` 时，正在运行的低优先级 child 必须被 stop，然后 Selector 转去 tick 高优先级 child。`Self` 或 `Both` 表示当前 child 自己的条件失效时必须 stop 自己。

`Sequence` 和 `Parallel` 不表达“更高优先级分支抢占低优先级分支”的业务心智。它们只支持 `None` 和 `Self`；如果作者在非 Selector 上配置 `LowerPriority` 或 `Both`，校验必须报错，而不是静默忽略。

网络层保持现有可插拔口径：BT edge condition 可以读取由正式输入、黑板、网络接收 stage 或 gameplay result 注入的运行事实；BT abort 本身不发包。分支被抢占后，只有该分支或新分支显式提交的 `ActionActivationRequest`、`ActionLifecycleTransition`、window、motion、cue、gameplay result 等 facts 会进入 `CharacterPipelineOutput.SyncFacts`，再由 `CharacterNetworkSendStage -> CharacterGameplaySyncAdapter -> GameplaySyncRuntime -> IGameplaySyncPeer` 决定是否发送。`None`、`LocalLoopback` 和未来 Fantasy peer 都复用同一条 facts 到 packet 映射。

## 与现有规格关系

- `btsmtl-graph-core` 已规定私有下钻 Graph 默认 inline、规则图不新增分裂路径。本 change 把通用条件图命名收口为 `ConditionRuleGraph`。
- `btsmtl-sm-node-authoring` 已规定状态机 Transition 条件属于 edge。本 change 保留该语义，但把具体规则图类型从 `TransitionRuleGraph` 迁移为 `ConditionRuleGraph`。
- `btsmtl-componentized-node-authoring` 已规定新能力必须接入 `BaseNode`、`BaseEdge`、`NodeModule`、`PropertyPort` 和字段访问器。本 change 不新增 Workbench 或并行端口协议。
- `add-pipeline-blackboard-authoring` 已完成黑板 ValueNode 和 TransitionRuleGraph 读取链路。本 change 复用这些 ValueNode 能力，但通过 `ConditionRuleGraph` 名称统一到 BT 与状态机。
- `add-timeline-loop-playback-and-state-transition-blend` 当前 active change 文档仍引用 `TransitionRuleGraph`。如果两个 change 都继续实施，Timeline change 需要在实施前 rebase 到 `ConditionRuleGraph` 命名，否则 spec 文案会冲突。
- `character-gameplay-sync-adapter` 和 `gameplay-sync-backend-selection` 已规定 `CharacterPipeline` 不直接持有 peer，网络后端通过 `GameplaySyncRuntime` 和 `IGameplaySyncPeer` 可插拔。本 change 继续遵守该边界，不让 BT edge decorator 认识 backend mode。
- `openspec/project.md` 仍写 `add-pipeline-blackboard-authoring` 未完成，但 `openspec list` 显示该 change 已 Complete；这是项目说明状态滞后，不改变本 change 的设计边界。

## Impact

- `BaseEdge` 的普通 BT output edge 条件图引用、abort policy 和 editor summary。
- `CompositeNode` child slot 缓存和 runtime 选择逻辑。
- `SelectorNode`、`SequenceNode`、`ParallelNode` 的分支进入、停止和返回状态。
- `TransitionRuleGraph` / `TransitionRuleResultNode` 到 `ConditionRuleGraph` / `ConditionRuleResultNode` 的命名和引用迁移。
- `NestedGraphValidation` 对 BT edge decorator、条件图、abort policy 和旧 `IfNode` 的校验。
- `BaseEdgeView` / Inspector / context menu 对 BT edge condition 的打开、摘要、ownership 和 abort policy 显示。
- Corin RootTree 和状态行为图中任何旧 `IfNode` 用法的删除或迁移。
- `CharacterNetworkSendStage` / `CharacterGameplaySyncAdapter` 的边界说明；BT edge decorator 不新增网络 stage，只确保抢占结果通过现有 facts 链路体现。

## 风险与缺口

- 如果现有资产中存在复杂 `IfNode` 用法，且无法安全映射到 Composite output edge condition，实施阶段必须停止并说明缺口，不得生成兼容节点。
- 如果 Unity 序列化无法安全完成 `TransitionRuleGraph` 到 `ConditionRuleGraph` 的类型迁移，实施阶段必须停止并说明 tradeoff。
- 如果当前 `BaseEdge` 无法在不破坏 StateMachine Transition 元数据的情况下承载 BT edge decorator 字段，实施阶段必须停止并重新设计 edge metadata 模块。
- 如果 Composite abort 会让旧分支继续产出 Timeline、Motion、Action window 或 Cue 事实，实施阶段必须停止；正确语义是 stop 被打断 child，后续事实只能来自仍在运行的分支。

# Design: BT Edge Condition Decorator 重构

## 当前问题

`IfNode` 把条件做成了一个可执行 decorator 节点。作者需要在图里额外摆一个节点，再把实际行为挂在它下面。这个模型有三个问题：

- 条件占据执行树结构，主流程从 `Attack / Dodge / Move / Idle` 变成 `If -> Branch`，业务阅读成本上升。
- `IfNode` 只有被 tick 到时才判断，不能表达“高优先级分支条件变真后抢占低优先级 running child”。
- 条件节点和行为节点共享执行生命周期，错误 stop 或 reset 会影响 TimelineNode、Action 节点和 motion 输出。

UE 式 BT 的关键不是节点形状，而是条件属于 Composite child slot。Composite 才知道 child 的顺序、当前 running child、哪些 slot 比当前 slot 高优先级，也只有 Composite 能正确执行 lower priority abort。

## ConditionRuleGraph

`ConditionRuleGraph` 是通用纯 Bool 条件图。它继承当前 `TransitionRuleGraph` 的核心能力，但去掉状态机专属命名。

它的接口很小：

- 一个唯一 result node 输出 Bool。
- 使用 ValueNode、输入读取节点、黑板读取节点、Compare、And、Or、Not 等纯求值节点。
- 使用现有 PropertyPort / PropertyEdge。
- 支持 owner 内 inline graph data 和显式 shared graph asset。
- runtime evaluate 时接收当前 graph user、deltaTime 和可选 facts。

它的实现可以内部复用或重命名现有 `TransitionRuleGraphRuntime`，但 authoring 和 runtime 对外不再出现 `TransitionRuleGraph` 作为通用条件图名称。

业务取舍：

- 统一命名的收益是状态机和 BT 不会出现两套几乎相同的条件图。
- 代价是一次破坏性迁移，所有状态机 transition 文案、类型、字段、校验、editor 菜单和资产序列化都要同步改名。

## BT Edge Decorator 数据

普通 BT graph 中，从 `CompositeNode.Output` 到 child `RunnableNode.Input` 的 flow edge 承载 edge decorator 数据：

- `ConditionRuleGraph` 引用：inline 或 shared，允许为空。
- `AbortPolicy`：`None`、`Self`、`LowerPriority`、`Both`。
- 条件摘要：显示规则图名称、缺失状态和 abort policy。

空条件表示该 slot 可进入。配置了条件图时，result 为 true 才可进入。条件图非法或 result node 缺失时，该 slot 不可进入，并由校验报告非法结构。

状态机 Transition edge 继续有 transition priority、condition rule 和后续 animation blend 元数据。BT edge decorator 不继承状态机的 transition priority；普通 BT child 顺序仍由 flow order 表达。

## Runtime 行为

Composite runtime 需要从 `m_Children` 扩展为 child slot 列表。slot 必须稳定包含 edge、child、flow order、condition rule runtime 和 abort policy。每次 logic tick 都按当前 graph deltaTime 求值必要条件。

### Selector

Selector 是优先级 Composite。slot 顺序就是优先级，越靠前优先级越高。

- 没有 running child 时，Selector 从头到尾选择第一个条件成立的 slot tick。
- 当前 running child 的自身条件失效，且该 slot 配置 `Self` 或 `Both` 时，Selector stop 当前 child。
- 更高优先级 slot 条件成立，且该高优先级 slot 配置 `LowerPriority` 或 `Both` 时，Selector stop 当前低优先级 child，并 tick 高优先级 child。
- 当前 child 返回 `Running` 时 Selector 返回 `Running`。
- child 返回 `Success` 时 Selector 返回 `Success`。
- child 返回 `Failure` 时 Selector 继续尝试下一个条件成立的 slot。

### Sequence

Sequence 表达顺序执行，不表达抢占优先级。slot 条件用于进入当前步骤。

- 下一步骤条件不成立时，Sequence 不能进入该 child。
- running child 自身条件失效，且 policy 为 `Self` 时，Sequence stop 该 child 并返回 `Failure`。
- `LowerPriority` 和 `Both` 在 Sequence 上非法，校验必须报错。

### Parallel

Parallel 表达多个 child 同帧参与。slot 条件用于决定 child 是否参与。

- 条件不成立的 child 本 tick 不参与更新。
- running child 自身条件失效，且 policy 为 `Self` 时，Parallel stop 该 child，并从当前参与集合移除。
- `LowerPriority` 和 `Both` 在 Parallel 上非法，校验必须报错。
- `JumpComplete` 的 completed child 只在条件仍成立时保持 completed；条件失效后再变真，需要重新参与。

## Abort 和 gameplay facts

Abort 只停止 BT 分支。被 stop 的 child 必须走正式 `StopNode` / `OnStop` 链路，让 TimelineNode 取消播放请求，让 action lifecycle 节点或状态机自行通过正式节点提交生命周期事实。

Abort 本身不自动制造 `ActionLifecycleTransition`。比如攻击被 Dodge 分支抢占时，是否提交 `Cancel(DodgeCancel)` 应由新分支或当前动作结构中的正式 Action lifecycle 节点负责。这样可以避免“树结构停止”被误当成“动作事务结束”。

业务取舍：

- 收益是分支抢占不会隐式关闭 ActionInstance，也不会伪造网络事实。
- 代价是动作取消仍需要明确节点或状态机流程提交 lifecycle transition，作者不能只靠 edge abort 偷懒。

## 可插拔网络层接入

BT edge decorator 不直接接网络层。它只改变本 tick Graph 执行路径，间接影响哪些正式 gameplay facts 被产出。

现有网络主链路保持不变：

```text
BT / StateMachine / Timeline
-> CharacterGraphContext 提交 facts
-> CharacterPipelineOutput.SyncFacts
-> CharacterNetworkSendStage
-> CharacterGameplaySyncAdapter
-> GameplaySyncRuntime
-> IGameplaySyncPeer
```

这条链路的业务含义是：

- BT edge condition 可以读取输入、黑板、状态事实、网络 receive stage 注入的 correction / action decision / gameplay result 等运行数据。
- BT edge abort 只 stop 分支，不直接生成 outgoing packet。
- 如果分支切换代表动作启动，必须由正式 Action activation 节点提交 `ActionActivationRequest`。
- 如果分支切换代表动作取消或外部打断，必须由正式 lifecycle 节点或状态流程提交 `ActionLifecycleTransition(Cancel/Interrupt/Abort)`。
- 如果 Timeline window、motion、cue 或 result 需要同步，仍由 Timeline / Graph 节点提交对应 facts。
- `CharacterGameplaySyncAdapter` 根据 resolved behavior policy 决定这些 facts 是否变成 packet。
- `GameplaySyncRuntime` 再交给当前 `IGameplaySyncPeer`；backend 可以是 `None`、`LocalLoopback` 或未来 Fantasy peer。

因此新增 BT edge decorator 不需要新增网络 packet 类型。真正需要网络可见的不是“发生了 BT abort”，而是 abort 之后产生的业务事实：动作取消、闪避启动、受击状态、motion correction acknowledgement、cue 或 gameplay result。

这个选择的业务取舍是：网络层继续可插拔，Graph 不认识 backend mode；代价是作者必须显式提交动作生命周期事实，不能指望 BT 分支被停止就自动同步“取消动作”。

## 为什么删除 IfNode

保留 `IfNode` 作为局部门控看似方便，但会留下两种条件心智：

- 图上普通节点条件。
- Composite edge decorator 条件。

这会让作者不知道攻击入口、闪避入口、调试分支和 Timeline 分支到底该用哪一个。按照本项目“旧路径直接删、不要兼容和分裂路径”的口径，`IfNode` 应直接删除。需要条件的分支必须挂在 Composite edge；需要数值组合的条件在 `ConditionRuleGraph` 中用 ValueNode 拼。

如果实施阶段发现现有资产仍依赖 `IfNode`，只做可证明安全的迁移；不能安全迁移时停止并说明，而不是保留 `IfNode` fallback。

## Tick 驱动而不是异步 observer

UE BT 可以通过黑板 observer 触发 abort。当前项目的 gameplay 管线是固定逻辑 tick 驱动，Graph、Timeline、Motion、NetworkSend 都在 tick 中有顺序。这个 change 不引入异步 observer；Composite 在每个 BT tick 内扫描 edge condition，得到确定性的抢占结果。

业务取舍：

- 收益是 deterministic，和当前 prediction / SyncFacts / Timeline 输出顺序一致。
- 代价是输入或黑板变化最多等到下一次 logic tick 被 BT 看到，不追求同帧异步立即抢占。

## UI 口径

BT edge condition 应显示在 edge / child slot 附近，而不是显示成普通节点。

- 双击可配置条件的 BT edge 或 Transition edge 时，如果已有 resolved `ConditionRuleGraph` 就直接打开；如果没有 inline/shared 条件引用，就创建 owner 内部 inline `ConditionRuleGraph` 并立即打开。
- 选中 edge 的 Inspector 显示 condition ownership、shared asset、abort policy 和 Open Rule。
- Inspector 的 `Open Rule` 与右键 `Condition Rule/Open` 走同一条语义；配置了 shared asset 但无法解析为 `ConditionRuleGraph` 时，不静默创建 inline fallback，作者必须显式切换 inline 或替换 shared asset。
- 搜索菜单不再出现 `If` 节点。
- 条件摘要显示类似 `Self | CanDodge`、`LowerPriority | HasAttackInput`。

这让作者看到的主树直接是业务分支：Attack、Dodge、Move、Idle，而不是 If 节点套行为节点。

# btsmtl-bt-edge-condition-decorators Specification

## ADDED Requirements

### Requirement: 纯 BT 分支条件必须属于 Composite output edge
系统 MUST 将普通 BT `CompositeNode` 到 child `RunnableNode` 的分支条件保存为 output edge decorator 数据。系统 MUST NOT 使用 `IfNode`、旧 BoolPort 条件字段、同层条件行为节点或外部配置表达 Composite child 是否可进入。

#### Scenario: 配置攻击分支条件
- **WHEN** 作者在 `Selector` 下配置 `Attack Branch`
- **THEN** 攻击输入、黑板变量和其它谓词 MUST 通过该 child output edge 的条件图表达
- **AND** `Attack Branch` 本体 MUST 只表达攻击行为
- **AND** 图中 MUST NOT 需要额外 `IfNode` 包住攻击行为

#### Scenario: 分支没有条件
- **WHEN** Composite child edge 没有配置条件图
- **THEN** 该 child MUST 被视为可进入
- **AND** runtime MUST NOT 查找旧节点、旧配置或 fallback 条件

### Requirement: ConditionRuleGraph 必须表达通用纯 Bool 条件
系统 MUST 使用 `ConditionRuleGraph` 作为 BT edge decorator 和 StateMachine transition 的通用纯 Bool 条件图。`ConditionRuleGraph` MUST 使用现有 `BaseGraph` 数据、字段访问器、`PropertyPort` 和 `PropertyEdge`。它 MUST NOT 继承或模拟 `RunnableTree` 生命周期。

#### Scenario: BT edge 创建默认条件图
- **WHEN** 作者为 Selector child edge 创建条件
- **THEN** 系统 MUST 创建该 edge 内部的 inline `ConditionRuleGraph`
- **AND** 默认图 MUST 包含唯一 `ConditionRuleResultNode`
- **AND** 作者 MUST 能立即下钻编辑 ValueNode、Compare、And、Or、Not、Input 和 Blackboard 读取节点

#### Scenario: 条件图拒绝行为节点
- **WHEN** 创建路径尝试向 `ConditionRuleGraph` 创建 `RunnableNode`、`TimelineNode`、`StateMachineNode`、`StateNode` 或状态生命周期节点
- **THEN** 创建逻辑 MUST 拒绝该节点
- **AND** 该节点 MUST NOT 进入条件图节点集合

#### Scenario: 状态机 transition 复用同一条件图类型
- **WHEN** 作者配置 `Idle -> Attack` transition 条件
- **THEN** 该 transition edge MUST 使用 `ConditionRuleGraph`
- **AND** 系统 MUST NOT 保留单独的 `TransitionRuleGraph` 作为第二套条件图类型

### Requirement: BT edge decorator 必须支持 inline 和 shared 条件图
BT edge decorator 的条件图引用 MUST 支持 owner 内部 inline graph data 和显式 shared graph asset。inline 与 shared MUST 互斥。默认私有条件 MUST 使用 inline graph data，需要复用时才显式抽取或分配 shared asset。

#### Scenario: 默认私有条件
- **WHEN** 作者为 Move Branch edge 添加条件
- **THEN** 条件图 MUST 保存为该 edge 的 inline graph data
- **AND** 系统 MUST NOT 创建 subasset 或一次性外部 asset

#### Scenario: 复用条件
- **WHEN** 多条 BT edge 需要复用同一个条件
- **THEN** 作者 MAY 显式抽取 shared condition rule asset
- **AND** owner edge MUST 清理原 inline 真数据
- **AND** 删除 edge 时 MUST NOT 删除 shared asset

#### Scenario: 非法双持有
- **WHEN** BT edge 同时持有 inline condition graph 和 shared condition graph asset
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST NOT 静默选择其中一个作为 fallback

### Requirement: BT edge 条件编辑入口必须属于 edge
编辑器 MUST 允许作者从合法 Composite child edge 直接打开或创建该 edge 的 `ConditionRuleGraph`。双击 edge、右键 `Condition Rule/Open` 和 Inspector `Open Rule` MUST 使用同一条打开语义：已有 resolved 条件图时打开该图；没有条件引用时创建 owner 内部 inline `ConditionRuleGraph` 后立即打开。

#### Scenario: 双击无条件 edge 创建 inline 条件图
- **WHEN** 作者双击一个合法 BT Composite child edge
- **AND** 该 edge 没有 inline 条件图，也没有 shared 条件图 asset
- **THEN** 编辑器 MUST 在该 edge 内创建 inline `ConditionRuleGraph`
- **AND** 编辑器 MUST 立即打开该 `ConditionRuleGraph`
- **AND** 默认 `ConditionRuleResultNode` MUST 返回 true，避免作者开始编辑前把原本无条件分支阻塞
- **AND** 创建流程 MUST NOT 创建 subasset、一次性外部 asset 或旧 `IfNode`

#### Scenario: 打开已有条件图
- **WHEN** 作者双击或点击 `Open Rule` 打开已有 resolved 条件图的 BT edge
- **THEN** 编辑器 MUST 打开该 edge 当前 resolved `ConditionRuleGraph`
- **AND** 如果该 edge 使用 shared asset，编辑器 MUST 打开 shared asset 内的规则图，而不是复制一份 inline 图

#### Scenario: configured shared 条件图无效
- **WHEN** BT edge 配置了 shared condition rule asset，但该 asset 不能解析为 `ConditionRuleGraph`
- **THEN** 编辑器 MUST NOT 将该 edge 当成无条件分支
- **AND** 编辑器 MUST NOT 静默创建 inline fallback 覆盖该 shared 配置
- **AND** 作者 MUST 显式切换到 inline rule 或替换 shared asset

### Requirement: BT edge 必须提供 AbortPolicy
系统 MUST 在 Composite child edge 上提供 `AbortPolicy`，至少包含 `None`、`Self`、`LowerPriority` 和 `Both`。Abort policy MUST 属于 edge 调度数据，条件图 MUST NOT 保存 abort policy、child 选择顺序或 priority 逻辑。

#### Scenario: 自身条件失效
- **WHEN** 当前 running child 的 edge condition 变为 false
- **AND** 该 edge 的 policy 是 `Self`
- **THEN** Composite MUST stop 该 child
- **AND** 后续返回状态 MUST 由该 Composite 类型的 runtime 规则决定

#### Scenario: 高优先级分支抢占
- **WHEN** Selector 正在运行低优先级 child
- **AND** 更高优先级 child edge condition 变为 true
- **AND** 更高优先级 edge 的 policy 是 `LowerPriority`
- **THEN** Selector MUST stop 当前低优先级 child
- **AND** Selector MUST tick 更高优先级 child

#### Scenario: 两种 abort 同时启用
- **WHEN** Selector child edge policy 是 `Both`
- **THEN** runtime MUST 同时应用自身条件失效停止和 lower priority 抢占语义

### Requirement: Selector 必须按 edge decorator 执行优先级抢占
`SelectorNode` MUST 将 child flow order 作为优先级顺序。每个 logic tick 中，Selector MUST 基于 edge condition 和 abort policy 决定是否继续当前 child、停止当前 child 或切到更高优先级 child。

#### Scenario: 更高优先级攻击输入出现
- **WHEN** Selector 正在运行 Move Branch
- **AND** Attack Branch 排在 Move Branch 前面
- **AND** Attack Branch condition 为 true 且 policy 为 `LowerPriority`
- **THEN** Selector MUST stop Move Branch
- **AND** Selector MUST tick Attack Branch

#### Scenario: 当前 child 继续运行
- **WHEN** 当前 running child condition 仍为 true
- **AND** 没有更高优先级 edge 可以抢占它
- **THEN** Selector MUST 继续 tick 当前 child
- **AND** Selector MUST 保持该 child 的 running 生命周期

### Requirement: Sequence 和 Parallel 必须限制 lower priority abort
`SequenceNode` 和 `ParallelNode` MUST 支持 edge condition 作为 child 进入条件，并 MAY 支持 `Self` 停止自身 running child。`LowerPriority` 和 `Both` MUST 只允许用于 `SelectorNode` child edge。非 Selector 上配置 `LowerPriority` 或 `Both` MUST 被校验为非法。

#### Scenario: Sequence 当前步骤条件失效
- **WHEN** Sequence 正在运行某个 child
- **AND** 该 child edge condition 变为 false
- **AND** edge policy 为 `Self`
- **THEN** Sequence MUST stop 该 child
- **AND** Sequence MUST 返回 `Failure`

#### Scenario: Parallel child 不再参与
- **WHEN** Parallel 的某个 running child edge condition 变为 false
- **AND** edge policy 为 `Self`
- **THEN** Parallel MUST stop 该 child
- **AND** 该 child MUST 从本轮参与集合移除

#### Scenario: 非法 lower priority 配置
- **WHEN** 作者在 Sequence 或 Parallel child edge 上配置 `LowerPriority`
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST NOT 静默忽略该配置

### Requirement: BT abort 必须停止分支但不伪造动作事实
BT edge abort MUST 只停止被抢占或失效的 child 分支。它 MUST 通过正式 `StopNode` / `OnStop` 链路传播停止，让节点自行取消 Timeline request 或清理运行状态。Abort MUST NOT 自动提交 `ActionLifecycleTransition`、Action window、motion、cue 或网络事实。

#### Scenario: 攻击分支被闪避分支抢占
- **WHEN** Selector 根据 edge decorator 从 Attack Branch 切到 Dodge Branch
- **THEN** Attack Branch MUST 被 stop
- **AND** Attack Branch 中运行的 TimelineNode MUST 通过正式 stop 取消播放请求
- **AND** 系统 MUST NOT 因 BT abort 自动伪造 `ActionLifecycleTransition(Cancel)`

#### Scenario: 新分支显式提交动作取消
- **WHEN** Dodge Branch 需要取消当前攻击动作
- **THEN** Dodge Branch 或正式状态流程 MUST 通过 Action lifecycle 节点提交 `Cancel`
- **AND** 该 lifecycle fact MUST 进入正式 `SyncFacts`

### Requirement: BT edge decorator 必须保持网络后端无关
BT edge decorator、Composite runtime 和 `ConditionRuleGraph` MUST NOT 直接持有或调用 `IGameplaySyncPeer`、Fantasy Session、transport client、backend mode 或 `GameplaySyncRuntime` peer 发送接口。网络可见结果 MUST 只来自 Graph、StateMachine、Timeline 或 Action 节点显式提交到 `SyncFacts` 的业务事实，并继续通过 `CharacterNetworkSendStage`、`CharacterGameplaySyncAdapter` 和 `GameplaySyncRuntime` 接入可插拔 backend。

#### Scenario: LocalLoopback 后端
- **WHEN** backend mode 是 `LocalLoopback`
- **AND** Selector edge abort 从 Attack Branch 切到 Dodge Branch
- **THEN** BT runtime MUST NOT 直接调用 loopback peer
- **AND** 只有 Dodge Branch 或正式 action lifecycle 节点提交的 facts MAY 被 adapter 映射为 outgoing packet

#### Scenario: None 后端
- **WHEN** backend mode 是 `None`
- **AND** BT edge condition 触发分支抢占
- **THEN** CharacterPipeline MUST 继续本地执行 Graph、Timeline、Motion 和 Presentation
- **AND** outgoing facts MAY 被收集到 `SyncFacts`
- **AND** 系统 MUST NOT 因无 peer 而启用 BT 专用 fallback 网络路径

#### Scenario: 未来 Fantasy peer
- **WHEN** 后续 change 增加 Fantasy peer
- **THEN** Fantasy peer MUST 作为 `IGameplaySyncPeer` adapter 接在 `GameplaySyncRuntime` 后面
- **AND** BT edge decorator、Composite runtime 和 `ConditionRuleGraph` MUST 不需要修改

### Requirement: IfNode 必须从正式 BT authoring 中删除
系统 MUST 删除 `IfNode` 作为正式节点类型、节点搜索入口和推荐 authoring 模式。旧资产中残留的 `IfNode` MUST 被迁移到 Composite edge condition；无法安全迁移时 MUST 被报告为非法结构，不得保留兼容执行路径。

#### Scenario: 节点搜索
- **WHEN** 作者在普通行为图中打开节点搜索
- **THEN** 搜索结果 MUST NOT 提供 `If` 节点
- **AND** 条件分支入口 MUST 通过 edge condition authoring 完成

#### Scenario: 旧资产残留 IfNode
- **WHEN** 校验发现 Graph 中存在 `IfNode`
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST NOT 使用 `IfNode` 作为 fallback 条件执行路径

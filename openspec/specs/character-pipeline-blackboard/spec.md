# character-pipeline-blackboard Specification

## Purpose
定义角色 Pipeline Blackboard 的声明、类型、作用域、运行时读写、ConditionRuleGraph 读取和 SyncFacts 边界。
## Requirements
### Requirement: Pipeline Blackboard 必须统一图变量和运行时黑板

系统 MUST 使用 Pipeline Blackboard 作为角色 pipeline 内部的统一变量模型。BTSMTL declaration、角色 pipeline 运行时变量和可调参数 MUST 解析到同一套 declaration/reference/runtime 服务。系统 MUST NOT 长期保留一套 graph exposed property、一套 runtime dictionary、一套局部状态变量和一套网络变量的分裂真相。单节点常量和单次求值中间值 MUST 能继续使用节点字段、`PropertyPort` 默认值或 ValueNode/PropertyEdge，而不要求声明 Blackboard variable。

#### Scenario: 作者声明共享移动阈值

- **WHEN** 作者为 Corin locomotion 配置会被多个 Transition 读取的 `WalkThreshold` 或 `RunThreshold`
- **THEN** 该值 MUST 作为 RootTree Character scope 的 Pipeline Blackboard declaration
- **AND** Graph、ConditionRuleGraph 和 Runtime Debug MUST 通过同一 declaration identity 和类型读取它
- **AND** 系统 MUST NOT 要求状态行为 Graph 复制同 key declaration

#### Scenario: 节点只使用一个常量

- **WHEN** 某个数值只被一个节点或一次 PropertyEdge 求值链使用
- **THEN** 作者 MUST 能把它保留为节点字段、端口默认值或 ValueNode 输出
- **AND** 系统 MUST NOT 要求该数值进入 Pipeline Blackboard

#### Scenario: 动作节点写入临时运行值

- **WHEN** Action 或 Timeline 节点写入当前 ActionInstance 的临时值
- **THEN** 该值 MUST 写入同一 Pipeline Blackboard runtime instance 的目标 ActionInstance bucket
- **AND** 该写入 MUST 受 declaration scope、lifetime 和当前 `ActionInstanceId` 约束

### Requirement: Blackboard Variable 必须声明类型、作用域和生命周期

每个 Pipeline Blackboard variable MUST 声明稳定 declaration identity、owner 内唯一 key、值类型、默认值、declaration owner、作用域、生命周期、authority、sync policy 和层级 category path。运行时 MUST 按 declaration 初始化、校验、寻址、清理和展示变量。`Config` lifetime MUST 是 runtime 只读。系统 MUST NOT 依赖散字符串 key、`object` 值或非法 scope/lifetime 组合作为正式业务合同。

#### Scenario: State 作用域变量

- **WHEN** 某个变量声明为 State scope 且生命周期为 StateEnterToExit
- **THEN** 进入 `StateMachineExecutionScope` 时 runtime MUST 为该 activation 初始化独立 bucket
- **AND** 离开该 execution scope 时 runtime MUST 只清理该 activation 的值
- **AND** 其它并行状态机和后续 activation MUST NOT 被清理或读到遗留值

#### Scenario: Graph 局部配置

- **WHEN** 一个状态行为 Graph 需要只在该 Graph 内可见的只读调参值
- **THEN** 作者 MUST 能声明 `Graph + Config` variable
- **AND** declaration MUST 随该 inline/shared Graph 序列化
- **AND** runtime MUST 拒绝对该 Config variable 的写入

#### Scenario: 非法 scope 和 lifetime

- **WHEN** 作者将 State scope 配成 ManualClear 或将 Frame scope 配成 Spawn
- **THEN** authoring validation MUST 报告非法组合
- **AND** runtime MUST NOT 猜测清理时机或降级成 Character scope

#### Scenario: 类型不匹配

- **WHEN** 节点以 Float 读取声明为 Vector2 的 variable
- **THEN** graph validation 和 runtime MUST 报告类型不匹配
- **AND** 系统 MUST NOT 尝试字符串转换、默认零值或其它 fallback

### Requirement: ExposedProperty 必须成为 Pipeline Blackboard 的 authoring 表面

角色 pipeline 图中的 `BaseExposedProperty` MUST 被定义为 Pipeline Blackboard declaration 的 authoring/serialization 表面。Character declaration MUST 归属 RootTree；局部 declaration MUST 归属当前 inline/shared Graph。下钻 Graph MAY 显式引用可见的上层 declaration，但 MUST NOT 为跨 Graph 访问复制同 key declaration。系统 MUST NOT 让 ExposedProperty、CharacterGraphContext dictionary 和局部状态字段形成多套互不映射的变量系统。

#### Scenario: 当前状态创建局部变量

- **WHEN** 作者在某个 StateNode 的 inline state body 中创建 State scope variable
- **THEN** declaration MUST 保存于该 state body Graph
- **AND** UI MUST 显示该 declaration 为 `Local`
- **AND** 其它无 owner 关系的 Graph MUST NOT 自动看到该 declaration

#### Scenario: 状态 body 引用 Character variable

- **WHEN** Dodge state body 需要写 RootTree 的 Character `IsDodging`
- **THEN** 节点 MUST 保存对 RootTree declaration 的显式 reference
- **AND** UI MUST 显示该 declaration 为 `Inherited` 及其 owner
- **AND** state body MUST NOT 创建第二份 `IsDodging` declaration

#### Scenario: UI 显示变量

- **WHEN** 作者在角色 pipeline Graph 或 Transition rule 中查看 Pipeline Blackboard
- **THEN** UI MUST 通过同一面板按 scope、当前上下文、category path 和搜索展示可见 declarations
- **AND** UI MUST 区分 Local 与 Inherited declaration
- **AND** 系统 MUST NOT 同时暴露两个需要重复维护的变量面板

### Requirement: Transition Rule 必须通过纯 ValueNode 读取黑板

ConditionRuleGraph 中读取 Pipeline Blackboard variable 的节点 MUST 是纯 ValueNode 兼容节点，并保存显式 declaration reference。该节点 MUST 不 tick Timeline、Action、RunnableNode 或状态行为 graph。现有 Runnable 形态的 ExposedPropertyNode MUST NOT 被放入 ConditionRuleGraph。读取失败 MUST 使本次条件求值失败，系统 MUST NOT 向输出端口写入零值、空值或 authoring 默认值后继续比较。

#### Scenario: 读取移动阈值

- **WHEN** Idle 到 WalkStart 的 Transition 需要比较输入幅度和 `WalkThreshold`
- **THEN** 规则图 MUST 通过显式 reference 读取 Character `WalkThreshold`
- **AND** Compare/And/Or 等纯条件节点 MUST 负责组合最终 Bool
- **AND** 规则图 MUST NOT 使用 Runnable `ExposedPropertyNode` 或裸字符串 key

#### Scenario: 规则图引用缺失 declaration

- **WHEN** ConditionRuleGraph 引用的 declaration 已删除、不可见或无法解析 owner
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST 让本次规则求值失败
- **AND** runtime MUST NOT 用硬编码默认值让 Transition 继续求值

### Requirement: Runtime Fact 和 Blackboard Variable 必须命名分层

系统 MUST 将 Blackboard variable 作为运行时变量、调参入口或当前作用域状态，将 SyncFacts 作为本 Tick 已发生且可被记录、调试、回放、loopback 或网络 backend 消费的事实。Graph 内部临时读写 MUST 命名为 Blackboard，已经输出的同步事实 MUST 命名为 fact。Blackboard value MUST NOT 因 key、category、类型或 true 值自动成为事实；只有 declaration 明确配置合法 fact projection 且当前写入具备所需 provenance 时，统一 projection stage 才能产生对应正式 SyncFact。

#### Scenario: Timeline 产出攻击窗口

- **WHEN** Decision TreeClip 写入显式 ActionWindow-bound `Attack1Hit=true`
- **THEN** 同一 variable MUST 可供后续 Graph 读取
- **AND** 统一 projection MUST 将该次写入转换为 `SyncFacts.Action.WindowSamples`
- **AND** NetworkSendStage MUST NOT 直接读取 Blackboard key/value

#### Scenario: 调参变量参与本地条件

- **WHEN** `RunThreshold` 被 ConditionRuleGraph 用于判断跑步
- **THEN** 该 variable MUST 保持 Config Blackboard 语义
- **AND** 它 MUST NOT 被当成本 Tick运行事实写入 SyncFacts

#### Scenario: 本地时间门参与状态转换

- **WHEN** `CanDodgeMoveCancel=true` 且 declaration 的 projection 为 None
- **THEN** Transition MUST 能读取该 Frame variable
- **AND** 系统 MUST NOT 产生 ActionWindowSample

### Requirement: Runtime value 必须按 declaration 与 scope owner 共同寻址

Pipeline Blackboard runtime MUST 使用 declaration identity 与实际 scope owner identity 共同生成 value address。Character、Graph、State、ActionInstance 和 Frame MUST 分别使用 Character runtime、Graph runtime instance、完整 `StateMachineExecutionScope`、`ActionInstanceId` 和 local logic tick 作为 owner。系统 MUST NOT 使用裸 `BlackboardKey` 作为全角色 runtime value 主键。

#### Scenario: 并行状态机退出状态

- **WHEN** Action StateMachine 的 Attack1 scope 退出，而 Locomotion StateMachine 的 RunLoop scope 仍 active
- **THEN** runtime MUST 只清理 Attack1 execution scope 的 State values
- **AND** RunLoop scope 的 values MUST 保持不变

#### Scenario: 清理一个 ActionInstance

- **WHEN** `ActionInstanceId=42` 进入 terminal 并请求清理
- **THEN** runtime MUST 只清理 owner 为 42 的 ActionInstance values
- **AND** 其它 active ActionInstance values MUST 保持不变

#### Scenario: shared graph 多实例

- **WHEN** 两个 runtime instance 同时执行同一 shared graph declaration
- **THEN** Graph scope values MUST 按各自 Graph runtime identity 隔离
- **AND** 一个 instance 的写入 MUST NOT 修改另一个 instance

### Requirement: Pipeline Blackboard authoring 必须提供上下文化分类视图

Pipeline Blackboard authoring MUST 提供 scope、当前上下文、层级 `CategoryPath` 和文本搜索视图。Graph tab 与 Transition selection MUST 都能访问该视图。分类 MUST 是 declaration metadata 与 UI 视图，MUST NOT 创建按分类拆分的 Blackboard asset 或第二套配置来源。

#### Scenario: 在 Transition 中选择变量

- **WHEN** 作者打开 Transition rule 并添加 blackboard ValueNode
- **THEN** picker MUST 展示当前 rule 可见且类型兼容的 declarations
- **AND** 作者 MUST 能按 scope 和 category 定位变量
- **AND** 选择 inherited declaration MUST 只创建 reference

#### Scenario: 查看局部动作变量

- **WHEN** 作者打开 Attack1 state body 并筛选 `Current Context + Action`
- **THEN** 面板 MUST 只展示当前上下文可见的 ActionInstance declarations
- **AND** 每项 MUST 显示 declaration owner 与 Local/Inherited 状态

### Requirement: Decision TreeClip 必须通过声明式 Frame Blackboard 输出决策

Decision TreeClip 写入的变量 MUST 来自 ExposedProperty 对应的 Pipeline Blackboard declaration，并且 MUST 使用 `Frame` scope 和 `Frame` lifetime。运行时 MUST 在 Frame 开始清理旧值，在当前 clip active 时重新求值并写入，在 State.OnExit 完成后的 Frame 结束统一清理。Decision Blackboard 写入 MUST NOT 自动产生 SyncFact。

#### Scenario: Dodge 恢复段开放移动取消

- **WHEN** Dodge Timeline 的 Decision TreeClip 在当前 Tick active
- **THEN** Tree MUST 写入声明为 Bool 的 `CanDodgeMoveCancel=true`
- **AND** Dodge Transition ConditionRuleGraph MUST 能在同一 Tick通过纯 ValueNode 读取该值
- **AND** 该写入 MUST NOT 产生 ActionWindowSample

#### Scenario: Decision clip 不再 active

- **WHEN** 新 logic frame 中 Decision TreeClip 不在 active 时间范围
- **THEN** Frame Blackboard MUST 不保留上一 Tick的 true 值
- **AND** runtime MUST NOT 依赖 OnDisable 写 false 才能清理 gate

#### Scenario: 声明策略冲突

- **WHEN** Timeline inline Tree 与 RootTree 对同一 Blackboard key 声明不同类型、scope、lifetime、authority 或 sync policy
- **THEN** validator 或 runtime MUST 报告配置错误
- **AND** 系统 MUST NOT 选择任一声明作为 fallback

### Requirement: Decision TreeClip 必须保持纯决策边界

Decision TreeClip graph MUST 只包含允许的纯读取、值转换、条件组合和 Blackboard 写入能力。它 MUST NOT 包含跨 Tick Running、Wait、TimelineNode、Action lifecycle、Motion、Cue、Camera、GameplayResult、网络发送或场景副作用节点。

#### Scenario: Decision Tree 包含副作用节点

- **WHEN** 作者在 Decision TreeClip 下钻 Graph 中加入 Motion 或 Cue 提交节点
- **THEN** graph validator MUST 报告非法节点能力
- **AND** runtime MUST NOT 执行该 Decision Graph

### Requirement: Blackboard declaration 必须显式声明 fact projection

Pipeline Blackboard declaration MAY 保存一个显式 fact projection。ActionWindow projection MUST 只允许 Bool、Frame scope、Frame lifetime 和 SyncFact policy，并 MUST 保存稳定 WindowType、WindowId 与 Digest。Projection MUST NOT 保存完整网络 policy；ActionWindowSample 的 effective policy MUST 由当前 Network Model adapter 使用 ActionInstance 对应的稳定 ActionId 从 model profile 解析。ActionProfile、Blackboard declaration、Graph 与 Timeline MUST NOT 复制该策略。非法 projection MUST 由 authoring validator 和 runtime 拒绝，不得 fallback 为普通变量或默认 Window。

#### Scenario: ActionWindow-bound Frame variable

- **WHEN** active Decision TreeClip 在当前 Tick 写入合法 ActionWindow-bound variable=true
- **AND** 写入 provenance 包含有效 Action Context
- **THEN** runtime MUST 记录一个本帧 projection candidate
- **AND** RootTree 决策后的统一 projection MUST 最多生成一个对应 ActionWindowSample
- **AND** 后续网络处理 MUST 从当前 Network Model profile 解析 effective policy

#### Scenario: 缺失 Action Context

- **WHEN** ActionWindow-bound variable 的写入 provenance 没有有效 Action Context
- **THEN** validator 或 runtime MUST 报告错误
- **AND** 系统 MUST NOT 使用 ambient current action、最后 active action 或默认 ActionInstance 补齐

#### Scenario: 同一变量被不同 ActionInstance 写入

- **WHEN** 同一 declaration 在同一 Tick 由两个不同 ActionInstance provenance 写入 true
- **THEN** projection MUST 按 ActionInstance 保留两个独立 candidate
- **AND** 最终单一 Blackboard Bool value MUST NOT 导致任一 ActionInstance 身份丢失

### Requirement: Blackboard 写入 provenance 必须支撑事实投影

需要 fact projection 的 Blackboard 写入 MUST 携带结构化 provenance，包括 local logic tick、source Graph/runtime owner 和 projection 所需的业务上下文。Timeline TreeClip 写入 MUST 从正式 Clip runtime context 获得 playback/clip/cycle 与 Action Context；非 Timeline action 写入 MUST 显式提供 Action Context。Provenance MUST 只服务当前 frame 的正式投影与 debug，不得成为第二套持久化 Blackboard 或 authoring 数据。

#### Scenario: Timeline TreeClip 写入

- **WHEN** Scheduler 求值某个 Action Timeline 的 Decision TreeClip
- **THEN** Blackboard write provenance MUST 包含该 playback 和 Action Context
- **AND** runtime MUST NOT 从 Timeline asset 本身推导 ActionInstance

#### Scenario: Frame cleanup

- **WHEN** 当前 logic frame 完成
- **THEN** Frame variable 与未消费 projection candidate MUST 被清理
- **AND** 后续 Tick MUST NOT读取或投影上一 Tick的 Window 值

### Requirement: Pipeline Blackboard declaration 必须作为 Graph Data Catalog 的正式来源

Pipeline Blackboard authoring MUST 将当前 authoring context 可见的 `BaseExposedProperty` declaration 投影到统一 `Graph Data Catalog`。每个条目 MUST 保留 declaration identity、实际 owner、local/inherited 可见性、值类型、scope、lifetime、authority、sync policy、category 和默认值语义。该投影 MUST NOT 复制 declaration，也 MUST NOT 建立 ExposedProperty 与 Pipeline Blackboard 之外的第二套变量配置。

#### Scenario: 显示当前 Graph 本地 declaration

- **WHEN** 作者打开拥有本地 `CanDodgeMoveCancel` declaration 的 Dodge state body
- **THEN** 目录 MUST 将其显示为当前 owner 的 local editable Blackboard 条目

#### Scenario: 显示 RootTree declaration

- **WHEN** inline state body 可见 RootTree 声明的 `RunThreshold`
- **THEN** 目录 MUST 将其显示为 inherited read-only 条目并标明真实 owner

#### Scenario: 同 key 不同 owner

- **WHEN** 两个合法 owner 各自存在显示名相同但 identity 不同的 declaration
- **THEN** 目录 MUST 通过 declaration identity 和 owner 区分条目，MUST NOT 按显示名合并

### Requirement: Blackboard Catalog source 必须按 declaration 所有权限制写操作

Blackboard catalog source MUST 只允许作者编辑或删除当前 owner 持有的本地 declaration。继承 declaration MUST 是只读投影，并 MAY 提供定位原 owner 的命令。新增 declaration MUST 使用当前 owner 的正式 authoring API，并 MUST 遵守既有 scope/lifetime 合法组合。系统 MUST NOT 在当前 Graph 复制继承 declaration、静默改变 owner 或使用 fallback scope。

#### Scenario: 编辑本地默认值

- **WHEN** 作者在目录详情中修改当前 owner 的本地 Config declaration 默认值
- **THEN** 系统 MUST 更新该 owner 的原 declaration

#### Scenario: 删除继承 declaration

- **WHEN** 作者查看从 RootTree 继承的 declaration
- **THEN** 目录 MUST 不提供针对当前 inline graph 的删除命令

#### Scenario: 新建 State variable

- **WHEN** 当前 Graph owner 支持 State scope 且作者通过目录创建 State variable
- **THEN** 系统 MUST 创建属于当前 owner 的合法 declaration

### Requirement: Blackboard Catalog source 必须复用上下文化可见性和节点引用链路

Blackboard catalog source MUST 复用 Pipeline Blackboard 已有的 Graph/Transition context、local/inherited 可见性解析和显式 declaration reference 节点工厂。目录 MUST NOT 重新实现一套 owner 查找、裸 key 匹配或 runtime dictionary 查询。拖拽创建失败时 MUST 保持失败并报告原因，MUST NOT 写入零值、默认值或 object fallback 后继续 authoring。

#### Scenario: Transition 读取阈值

- **WHEN** 作者把可见的 `RunThreshold` 从目录拖入 ConditionRuleGraph
- **THEN** 系统 MUST 创建保存显式 declaration reference 的纯 ValueNode 兼容节点

#### Scenario: declaration 在当前 context 不可见

- **WHEN** 某 declaration 不属于当前 Graph 的 local/inherited 可见集合
- **THEN** 目录 MUST 不展示该条目，也 MUST NOT 通过裸 key 搜索把它加入结果

#### Scenario: 引用目标已失效

- **WHEN** 条目对应 declaration 在拖拽完成前被删除或 owner context 已切换
- **THEN** 节点创建 MUST 失败并报告失效引用，MUST NOT 创建绑定默认值的节点

### Requirement: 嵌套状态机必须按 declaration owner 解析 State activation frame

Pipeline Blackboard access context MUST 携带完整 StateMachine execution path。读取或写入 State scope declaration 时，resolver MUST 根据 declaration owner 和 Graph ownership 选择唯一对应 activation frame，而不是始终使用最内层 frame。找不到或找到多个候选 frame MUST 作为配置/runtime 错误，MUST NOT fallback 到 Character、Graph 或栈顶 State scope。

#### Scenario: 外层 Attack 状态变量跨连段保持

- **WHEN** declaration 归属外层 Attack State body
- **AND** 内层状态从 Attack1 切换到 Attack2
- **THEN** 该 declaration MUST 继续绑定外层 Attack activation bucket
- **AND** Attack1 exit MUST NOT 清理该值
- **AND** 外层 Attack exit MUST 清理该值

#### Scenario: Attack1 局部状态变量退出清理

- **WHEN** declaration 归属 Attack1 State body
- **AND** Attack1 退出到 Attack2
- **THEN** runtime MUST 只清理 Attack1 activation bucket
- **AND** 外层 Attack bucket 与 Attack2 bucket MUST 保持独立

#### Scenario: 内层引用外层 declaration

- **WHEN** Attack2 ConditionRuleGraph 显式引用外层 Attack body declaration
- **THEN** resolver MUST 使用 declaration owner 定位外层 Attack frame
- **AND** 系统 MUST NOT 复制同 key declaration 到 Attack2 graph
- **AND** 系统 MUST NOT 按最近 key 隐式 shadow

#### Scenario: declaration owner 不在 execution path

- **WHEN** State declaration reference 的 owner 不对应当前 execution path 中任何 frame
- **THEN** access MUST 失败并报告 owner/path 断裂
- **AND** Compare、And、Or 或 lifecycle 节点 MUST NOT 获得默认值继续执行

### Requirement: Gameplay Effect 不得存入 Pipeline Blackboard

GameplayTag、Attribute Base/Current、ActiveGameplayEffect、stack、duration、period、inhibition 和 prediction journal MUST 由通用 `GameplayEffectRuntime` 正式持有。CharacterGameplayEffectAdapter 只委托端口和投影 ChangeSet；Blackboard MAY 保存 Graph 局部计算值或显式 fact projection，但 MUST NOT 作为上述 Gameplay Effect 的真相源、缓存副本或双写目标。

#### Scenario: Graph 读取 Health

- **WHEN** ValueNode 需要当前 Health
- **THEN** 它 MUST 通过 Gameplay Attribute 查询接口读取
- **AND** MUST NOT 从同名 Blackboard variable 读取或回写同步

#### Scenario: Transition 使用临时比较结果

- **WHEN** Graph 把 `Health < Threshold` 的本地计算结果写入 Frame Blackboard
- **THEN** Blackboard MAY 保存该临时 Bool
- **AND** Health 的 Base、Current 与 Revision MUST 仍只归属 Gameplay Effect


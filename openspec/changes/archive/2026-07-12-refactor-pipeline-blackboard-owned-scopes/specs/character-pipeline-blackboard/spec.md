## MODIFIED Requirements

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

系统 MUST 将 blackboard variable 作为运行时变量或调参入口，将 SyncFacts 作为本 tick 已发生且可被记录、调试、回放、loopback 或网络 backend 消费的事实。Graph 内部临时读写 MUST 命名为 blackboard，已经输出的同步事实 MUST 命名为 fact。Blackboard declaration 的 Authority 或 SyncPolicy MUST NOT 直接产生通用网络 key/value packet。

#### Scenario: Timeline 产出攻击窗口

- **WHEN** Timeline 触发 `Attack1Hit` window
- **THEN** 最近 window MAY 写入当前 ActionInstance scope variable 供后续 graph 读取
- **AND** 可同步窗口事实 MUST 写入 `SyncFacts.Action.WindowSamples`
- **AND** 系统 MUST NOT 因为某个 blackboard value 存在就自动认为网络事实已经产生

#### Scenario: 调参变量参与本地条件

- **WHEN** `RunThreshold` 被 ConditionRuleGraph 用于判断跑步
- **THEN** 该 variable MUST 保持 Character Config 语义
- **AND** 它 MUST NOT 被当成本 tick 运行事实写入 SyncFacts

## ADDED Requirements

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

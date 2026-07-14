# character-pipeline-blackboard Specification

## ADDED Requirements

### Requirement: Pipeline Blackboard 必须统一图变量和运行时黑板

系统 MUST 使用 Pipeline Blackboard 作为角色 pipeline 内部的统一变量模型。BTSMTL 图变量、角色 pipeline 运行时临时变量和可调参数 MUST 能解析到同一套 blackboard declaration。系统 MUST NOT 长期保留一套 graph exposed property、一套 runtime dictionary 和一套网络变量的分裂真相。

#### Scenario: 作者声明移动阈值

- **WHEN** 作者为 Corin locomotion 配置 `WalkThreshold` 或 `RunThreshold`
- **THEN** 该值 MUST 作为 Pipeline Blackboard variable 声明
- **AND** Graph、TransitionRuleGraph 和 Runtime Debug MUST 能按同一 key 和类型读取它
- **AND** 系统 MUST NOT 要求作者同时维护另一个 runtime blackboard key

#### Scenario: 动作节点写入临时运行值

- **WHEN** Action 或 Timeline 节点写入最近 window、cue 或 result
- **THEN** 该值 MUST 写入 Pipeline Blackboard runtime instance
- **AND** 该写入 MUST 受 declaration 的 scope 和 lifetime 约束

### Requirement: Blackboard Variable 必须声明类型、作用域和生命周期

每个 Pipeline Blackboard variable MUST 声明稳定 key、值类型、默认值、作用域、生命周期、写入权限、同步策略和 debug 分类。运行时 MUST 按 declaration 初始化、校验、清理和展示变量。系统 MUST NOT 依赖散字符串 key 和 `object` 值作为正式业务合同。

#### Scenario: State 作用域变量

- **WHEN** 某个变量声明为 State scope 且生命周期为 StateEnterToExit
- **THEN** 进入状态时 runtime MUST 能初始化该变量
- **AND** 离开状态时 runtime MUST 清理该变量
- **AND** 后续状态 MUST NOT 读到上一个状态遗留值

#### Scenario: 类型不匹配

- **WHEN** 节点以 Float 读取声明为 Vector2 的 blackboard variable
- **THEN** runtime 或 graph validation MUST 报告类型不匹配
- **AND** 系统 MUST NOT 尝试字符串转换、默认零值或其它 fallback

### Requirement: ExposedProperty 必须成为 Pipeline Blackboard 的 authoring 表面

角色 pipeline 图中的 ExposedProperty MUST 被定义为 Pipeline Blackboard variable 的作者入口或序列化来源。系统 MAY 复用现有 ExposedProperty 序列化字段，但运行时语义 MUST 归入 Pipeline Blackboard。系统 MUST NOT 让 ExposedProperty 和 CharacterGraphContext blackboard 形成两套互不映射的变量系统。

#### Scenario: 读取已有 ExposedProperty

- **WHEN** pipeline 图资产中存在 FloatExposedProperty
- **THEN** 实现阶段 MUST 将其解析为 Float 类型的 blackboard declaration
- **AND** declaration MUST 补齐 scope、lifetime 和 sync policy
- **AND** 运行时读取 MUST 通过 Pipeline Blackboard 而不是直接绕过 context 读取图字段

#### Scenario: UI 显示变量

- **WHEN** 作者在角色 pipeline 图中查看变量面板
- **THEN** UI MUST 使用同一入口展示变量的默认值和 blackboard 元数据
- **AND** 系统 MUST NOT 同时暴露两个需要重复维护的变量面板

### Requirement: Transition Rule 必须通过纯 ValueNode 读取黑板

TransitionRuleGraph 中读取 Pipeline Blackboard variable 的节点 MUST 是纯 ValueNode 兼容节点。该节点 MUST 不 tick Timeline、Action、RunnableNode 或状态行为 graph。现有 Runnable 形态的 ExposedPropertyNode MUST NOT 被放入 TransitionRuleGraph。

#### Scenario: 读取移动阈值

- **WHEN** Idle 到 WalkStart 的 Transition 需要比较输入幅度和 `WalkThreshold`
- **THEN** 规则图 MUST 通过 ValueNode 读取输入幅度和 blackboard float
- **AND** Compare/And/Or 等纯条件节点 MUST 负责组合最终 Bool
- **AND** 规则图 MUST NOT 使用 Runnable `ExposedPropertyNode`

#### Scenario: 规则图缺少变量声明

- **WHEN** TransitionRuleGraph 引用不存在的 blackboard key
- **THEN** 校验 MUST 报告非法结构
- **AND** runtime MUST NOT 用硬编码默认值让 Transition 通过

### Requirement: Runtime Fact 和 Blackboard Variable 必须命名分层

系统 MUST 将 blackboard variable 作为运行时变量或调参入口，将 SyncFacts 作为本 tick 已发生且可被记录、调试、回放、loopback 或网络 backend 消费的事实。Graph 内部临时读写 MUST 命名为 blackboard，已经输出的同步事实 MUST 命名为 fact。

#### Scenario: Timeline 产出攻击窗口

- **WHEN** Timeline 触发 `Attack1Hit` window
- **THEN** 最近 window MAY 写入 Pipeline Blackboard 供后续 graph 读取
- **AND** 可同步窗口事实 MUST 写入 `SyncFacts.Action.WindowSamples`
- **AND** 系统 MUST NOT 因为某个 blackboard key 存在就自动认为网络事实已经产生

#### Scenario: 调参变量参与本地条件

- **WHEN** `RunThreshold` 被 TransitionRuleGraph 用于判断跑步
- **THEN** 该变量 MUST 保持 blackboard/config 语义
- **AND** 它 MUST NOT 被当成本 tick 运行事实写入 SyncFacts

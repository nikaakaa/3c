## MODIFIED Requirements

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

## ADDED Requirements

### Requirement: Blackboard declaration 必须显式声明 fact projection

Pipeline Blackboard declaration MAY 保存一个显式 fact projection。ActionWindow projection MUST 只允许 Bool、Frame scope、Frame lifetime 和 SyncFact policy，并 MUST 保存稳定 WindowType、WindowId 与 Digest。Projection MUST NOT 保存完整网络 policy；ActionWindowSample 的 effective policy MUST 继续通过 ActionInstance 对应 ActionProfile 解析。非法 projection MUST 由 authoring validator 和 runtime 拒绝，不得 fallback 为普通变量或默认 Window。

#### Scenario: ActionWindow-bound Frame variable

- **WHEN** active Decision TreeClip 在当前 Tick写入合法 ActionWindow-bound variable=true
- **AND** 写入 provenance 包含有效 Action Context
- **THEN** runtime MUST 记录一个本帧 projection candidate
- **AND** RootTree 决策后的统一 projection MUST 最多生成一个对应 ActionWindowSample

#### Scenario: 缺失 Action Context

- **WHEN** ActionWindow-bound variable 的写入 provenance 没有有效 Action Context
- **THEN** validator 或 runtime MUST 报告错误
- **AND** 系统 MUST NOT 使用 ambient current action、最后 active action 或默认 ActionInstance 补齐

#### Scenario: 同一变量被不同 ActionInstance 写入

- **WHEN** 同一 declaration 在同一 Tick由两个不同 ActionInstance provenance 写入 true
- **THEN** projection MUST 按 ActionInstance 保留两个独立 candidate
- **AND** 最终单一 Blackboard Bool value MUST NOT导致任一 ActionInstance 身份丢失

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


# character-pipeline-runtime Specification

## ADDED Requirements

### Requirement: CharacterGraphContext 必须通过 Pipeline Blackboard 暴露黑板

系统 MUST 让 `CharacterGraphContext` 通过 Pipeline Blackboard runtime instance 提供 blackboard 读写入口。`CharacterGraphContext` MAY 保留兼容命名的 `TryGetBlackboardValue` 和 `SetBlackboardValue` 方法作为内部 API，但这些方法 MUST 委托到正式 Pipeline Blackboard runtime，并执行 declaration、类型、作用域和生命周期校验。

#### Scenario: 节点读取黑板值

- **WHEN** BTSMTL 节点通过 `CharacterGraphContext` 读取 blackboard 值
- **THEN** context MUST 从 Pipeline Blackboard runtime 读取
- **AND** 读取结果 MUST 受变量 declaration 的类型和 scope 约束
- **AND** context MUST NOT 直接访问未声明的散 dictionary key

#### Scenario: 动作结束清理变量

- **WHEN** ActionInstance 进入 Complete、Cancel、Interrupt 或 Abort 终态
- **THEN** Pipeline Blackboard runtime MUST 清理该 ActionInstance scope 的变量
- **AND** 其它 action 或后续状态 MUST NOT 读取到已结束动作的临时值

### Requirement: Pipeline 输出事实必须继续通过 SyncFacts 边界产生

系统 MUST 保持 `CharacterPipelineOutput.SyncFacts` 作为 pipeline 输出事实边界。Blackboard 写入 MAY 为后续图节点提供运行时上下文，但 MUST NOT 取代 Action、Motion、GameplayResult、StateEffect 或 Presentation SyncDomain output。

#### Scenario: 提交 Action window

- **WHEN** 节点提交 Action window sample
- **THEN** runtime MAY 将最近 window 写入 Pipeline Blackboard
- **AND** runtime MUST 将可同步 window 写入 `SyncFacts.Action.WindowSamples`
- **AND** NetworkSendStage MUST 继续从 SyncFacts 收集该事实

#### Scenario: 写入 local-only 临时值

- **WHEN** 节点写入仅供本地表现或本状态内部读取的 blackboard variable
- **THEN** 该值 MUST NOT 自动进入 `SyncFacts`
- **AND** NetworkSendStage MUST NOT 因该变量存在而生成 outgoing packet

### Requirement: Pipeline Blackboard 生命周期必须进入 frame cleanup

系统 MUST 在角色 pipeline 生命周期中清理 Pipeline Blackboard 的 transient 值。Frame、State、ActionInstance 和 Character scope 的变量 MUST 在对应生命周期结束时清理或重置。系统 MUST NOT 依赖节点作者手动用 null 写回清理所有临时 key。

#### Scenario: Frame scope 变量

- **WHEN** 某个变量声明为 Frame scope
- **THEN** frame end cleanup MUST 清理该变量
- **AND** 下一帧读取该变量 MUST 得到未设置状态或默认值

#### Scenario: Character scope 变量

- **WHEN** pipeline Dispose
- **THEN** Pipeline Blackboard runtime MUST 清理 Character scope 的值
- **AND** 已销毁角色 MUST NOT 继续持有 scene object、action handle 或 graph context 引用

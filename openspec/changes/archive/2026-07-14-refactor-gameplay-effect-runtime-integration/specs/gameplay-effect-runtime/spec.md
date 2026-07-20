## ADDED Requirements

### Requirement: GameplayEffectRuntime 必须是单一公开门面而不是规则大类

`GameplayEffectRuntime` MUST 只负责唯一状态所有权、窄端口实现、固定 Tick 入口、命令编排和释放。Spec 构建、应用事务、生命周期调度、Component 执行、预测协调和 ChangeSet 记录 MUST 由内部单职责协作者承担。内部协作者 MUST NOT 各自拥有 Tag、Attribute、ActiveEffect、PredictionJournal、Tick 或公开命令入口，MUST NOT 形成第二套 runtime。

#### Scenario: 应用一个 Duration Effect

- **WHEN** 外部通过 `IGameplayEffectCommandSink` 提交合法 Duration Effect
- **THEN** Runtime MUST 依次委托 SpecFactory、ApplicationService、ComponentExecutor 和 ChangeRecorder 完成同一事务
- **AND** 外部 MUST 仍然只面对 Runtime 的窄合同
- **AND** 任一内部协作者 MUST NOT 建立自己的 ActiveEffect Container

#### Scenario: 增加新的 Component 类型

- **WHEN** 系统增加一种正式类型化 Effect Component
- **THEN** 该 Component 的执行扩展 MUST 位于 Component 数据和 ComponentExecutor 边界
- **AND** Runtime 门面 MUST NOT 增加按具体 EffectId 判断的业务 switch

### Requirement: GE 内部 mutation 必须由单一事务提交

Effect Component MUST 只产生类型化 Attribute、Tag、AdditionalEffect 和 Cue 操作；`GameplayEffectApplicationService` 或等价应用服务 MUST 在同一个 mutation transaction 中完成校验和提交。AdditionalEffect MUST 通过事务内命令队列继续使用同一应用服务，Component MUST NOT 反向持有 Runtime。事务失败 MUST 不留下部分 Attribute、Tag、ActiveEffect、PredictionJournal 或 Cue 修改。

#### Scenario: Additional Effect 配置非法

- **WHEN** 主 Effect 的 AdditionalEffect 在事务提交前发现缺失 Definition 或非法参数
- **THEN** 主 Effect 与 AdditionalEffect 的 mutation MUST 全部拒绝
- **AND** Runtime MUST 不留下已授予 Tag、已修改 Attribute 或已创建 ActiveEffect

#### Scenario: 预测事务成功提交

- **WHEN** Predicted Effect 的全部 mutation 成功提交
- **THEN** PredictionJournal 与 ChangeSet MUST 从同一提交结果记录 identity 和 revision
- **AND** 系统 MUST NOT 通过另一次状态扫描猜测预测修改

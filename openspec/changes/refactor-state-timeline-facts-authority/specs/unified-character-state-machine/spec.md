## ADDED Requirements
### Requirement: 单帧 Timeline Facts 权威
统一状态机 MUST 在同一帧内消费由 Character frame context 提供的 current timeline facts。Action request submission / interrupt arbitration、transition 条件和状态输出 MUST 使用同一个 current facts 包；需要预判或目标状态进入的 facts MUST 使用不同命名的 projected 或 target facts。

#### Scenario: Request submission 与 transition 使用同一 current facts
- **GIVEN** 当前状态已有一个 current `StateTimelineWindowFacts`
- **WHEN** 本帧先处理 Action request submission / interrupt arbitration 再推进统一状态机
- **THEN** Action request submission / interrupt arbitration MUST 消费该 current facts
- **AND** transition evaluator MUST 消费同一个 current facts
- **AND** 二者 MUST NOT 分别自行采样 current facts

#### Scenario: Projected facts 不覆盖 current facts
- **GIVEN** transition evaluator 需要基于 `StateTime + DeltaTime` 判断自然退出
- **WHEN** runner 计算 projected timeline facts
- **THEN** projected facts MUST 只用于 transition evaluation
- **AND** MUST NOT 替换 Action request submission / interrupt arbitration 已经消费的 current facts

#### Scenario: Target facts 显式归属目标状态
- **GIVEN** 本帧发生状态切换
- **WHEN** 新状态 Enter 或 Tick 需要 timeline facts
- **THEN** 系统 MUST 使用目标状态、目标变体和目标 state time 生成 target facts
- **AND** target facts MUST 在诊断 trace 中区别于 current facts

#### Scenario: 缺少 current facts 不使用 fallback
- **GIVEN** 当前 transition 或 request policy 需要 timeline window
- **AND** 本帧没有有效 current timeline facts
- **WHEN** 统一状态机推进或 request submission / interrupt arbitration 执行
- **THEN** 系统 MUST 报告配置或帧输入错误
- **AND** MUST NOT 用 elapsed time、默认窗口或空 facts 伪造通过结果

### Requirement: 状态机 Runner 不直接提交 Timeline 诊断
状态机 runner MUST 只产出纯数据状态推进结果和诊断 trace。Timeline facts、transition condition probe 或等价运行时观测日志 MUST 由 Character diagnostics adapter 或等价外围模块提交。

#### Scenario: Runner 输出 trace
- **WHEN** runner 完成一帧 transition evaluation
- **THEN** runner MAY 返回 current、projected 和 target facts trace
- **AND** runner MUST NOT 直接调用 `RuntimeDiagnosticLog.Submit`

#### Scenario: 外围 adapter 提交日志
- **GIVEN** runner 返回 timeline facts trace
- **WHEN** Character diagnostics adapter 处理本帧结果
- **THEN** adapter MUST 能提交与现有 `state-timeline-window-facts` 等价的日志
- **AND** 日志 MUST 能说明 facts 来源阶段

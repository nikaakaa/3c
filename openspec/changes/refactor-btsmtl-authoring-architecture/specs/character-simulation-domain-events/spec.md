## ADDED Requirements

### Requirement: 领域事件输出通道

仿真每 Tick 输出 MUST 在 presentation command 与 gameplay fact 之外提供领域事件通道。领域事件 MUST 表达一次性的语义变化，MUST 与 presentation command 在同一 Step 事务内、使用同一 `SimulationEventHeader` 信封（EventId／Actor／Tick／Activation／Sequence／Channel）发布。领域事件 MUST 随 Tick 输出一起被 rollback 重放，MUST NOT 独立存储、MUST NOT 使用进程内事件总线，MUST NOT 接受表现层或 gameplay 系统向仿真侧的反向调用。

#### Scenario: 段转移发布事件

- **WHEN** 技能状态机在同一激活的 ActionInstance 内完成一次段转移（旧段退出、新段激活）
- **THEN** Float32 与 Fixed 的状态机运行时 MUST 在同一转移事务点发布 `ActionSegmentChanged` 事件
- **AND** 事件 MUST 只含 typed 身份（源 ActionInstanceId、前一状态、目标状态），MUST NOT 含动画或剪辑引用
- **AND** 同一转移在两 Target 上产生相同的事件序列

#### Scenario: 事件不进入播放命令路径

- **WHEN** 表现层收到 `ActionSegmentChanged` 事件
- **THEN** 表现层 MUST 用它终结该 ActionInstance 的旧 playback 条目并让新段 Select 以新 generation 生效
- **AND** 播放命令（Select／Sample／Complete／Release）的语义 MUST 保持不变
- **AND** 表现层 MUST NOT 从播放命令形状推断技能组织形态

#### Scenario: 事件通道回放一致

- **WHEN** 同一输入在 rollback 恢复后重放同一 Tick
- **THEN** 该 Tick 的领域事件 MUST 与原始输出一致
- **AND** 过期 Tick 的领域事件 MUST 与命令流一同被丢弃

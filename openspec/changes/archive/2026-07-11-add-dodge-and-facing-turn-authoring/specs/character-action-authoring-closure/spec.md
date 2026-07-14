## ADDED Requirements

### Requirement: Dodge Action 必须通过 pipeline blackboard 公布 locomotion ownership

Corin DodgeForward 和 DodgeBack MUST 保持为 Action StateMachine 中唯一 Dodge 业务状态。Dodge OnEnter MUST 在 ActionInstance 成功激活后写入 pipeline blackboard `IsDodging=true`；所有 source-exit 的 OnExit MUST 写入 `IsDodging=false`。Locomotion MUST 只读取该 ownership fact，不得复制 Dodge request 消费、ActionProfile、Timeline、motion curve 或 IFrame。

#### Scenario: Dodge 激活后让渡 locomotion 所有权

- **WHEN** DodgeForward 或 DodgeBack 成功激活 ActionInstance
- **THEN** 对应 OnEnter MUST 写入 `IsDodging=true`
- **AND** Locomotion StateMachine MUST 能读取该值进入 ActionOverride

#### Scenario: Dodge 正常完成或被打断

- **WHEN** Dodge state 正常完成、被 State transition 抢占或被上层 tree stop
- **THEN** source OnExit MUST 写入 `IsDodging=false`
- **AND** Locomotion MUST 能按当前 MoveAxis 收回所有权

#### Scenario: 单一 Dodge 动作真相

- **WHEN** Locomotion 处理 Dodge 活跃期间的所有权
- **THEN** Locomotion MUST NOT 创建第二个 Dodge action state 或引用 Dodge Timeline
- **AND** Dodge request MUST 继续只由 Action 激活接受点消费

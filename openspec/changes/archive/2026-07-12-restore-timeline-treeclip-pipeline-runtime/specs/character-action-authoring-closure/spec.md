## MODIFIED Requirements

### Requirement: Dodge Action 必须通过 pipeline blackboard 公布 locomotion ownership

Corin DodgeForward 和 DodgeBack MUST 保持为 Action StateMachine 中唯一 Dodge 业务状态。Dodge OnEnter MUST 在 ActionInstance 成功激活后写入 pipeline blackboard `IsDodging=true`；所有 source-exit 的 OnExit MUST 写入 `IsDodging=false`。Dodge Timeline 的本地移动恢复时间门 MUST 由 Decision TreeClip 写入 Frame Blackboard `CanDodgeMoveCancel`，不得伪装成 Action CancelWindow。Locomotion MUST 只读取 ownership fact，不得复制 Dodge request、ActionProfile、Timeline、motion curve、IFrame 或恢复门。

#### Scenario: Dodge 激活后让渡 locomotion 所有权

- **WHEN** DodgeForward 或 DodgeBack 成功激活 ActionInstance
- **THEN** 对应 OnEnter MUST 写入 `IsDodging=true`
- **AND** Locomotion StateMachine MUST 能读取该值进入 ActionOverride

#### Scenario: Dodge 恢复段有移动输入

- **WHEN** Decision TreeClip 当前 Tick写入 `CanDodgeMoveCancel=true`
- **AND** MoveAxis 大于 stop threshold
- **THEN** Dodge MUST 通过状态边离开
- **AND** source OnExit MUST 写入 `IsDodging=false`
- **AND** source OnExit MUST 提交 `Cancel(DodgeMoveToRun)`
- **AND** Dodge ActionProfile MUST NOT 要求对应 Cancel window policy

#### Scenario: Dodge 自然完成

- **WHEN** Dodge Timeline 正常完成且本 Tick没有恢复门移动取消
- **THEN** source OnExit MUST 写入 `IsDodging=false`
- **AND** source MUST 提交 `Complete(DodgeComplete)`

#### Scenario: Dodge 被上层 Tree 停止

- **WHEN** Dodge state 因上层 Tree graceful abort 退出且 Action Context 仍 active
- **THEN** source OnExit MUST 写入 `IsDodging=false`
- **AND** source MUST 提交 `Abort(TreeAbort)`

#### Scenario: 单一 Dodge 动作真相

- **WHEN** Locomotion 处理 Dodge 活跃期间的所有权
- **THEN** Locomotion MUST NOT 创建第二个 Dodge action state 或引用 Dodge Timeline
- **AND** Dodge request MUST 继续只由 Action 激活接受点消费
- **AND** Dodge IFrame MUST 继续由 ActionWindowTrack 表达

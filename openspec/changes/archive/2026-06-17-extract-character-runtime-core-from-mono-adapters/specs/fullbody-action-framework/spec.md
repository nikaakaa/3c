## ADDED Requirements
### Requirement: Action Runtime 迁出 Mono Adapter
Action 的正式 state machine runtime、action lifecycle runtime、request/lifecycle state、output runtime host、snapshot/restore 和 diagnostics state MUST 由 core-owned Action runtime module 持有。`FullBodyActionRuntime` MAY 作为 Unity adapter 或迁移期 facade 保留，但 MUST NOT 作为正式 Action state owner 或正式 tick owner。

#### Scenario: State Machine Runtime 归属 Pure Runtime
- **WHEN** Action module 处理 Dodge、LightAttack 或后续 action request
- **THEN** `CharacterStateMachineRuntime` 或批准的等价 action graph runtime MUST 由 core-owned Action runtime module 持有
- **AND** `FullBodyActionRuntime` MUST NOT 成为该 runner 的 authoritative owner

#### Scenario: Lifecycle Runtime 归属 Pure Runtime
- **WHEN** Action module 接受、持续、打断或结束 action lifecycle
- **THEN** `ActionLifecycleRuntime` 或批准的等价 lifecycle state MUST 由 core-owned Action runtime module 持有
- **AND** Mono adapter MUST NOT 通过自身字段成为 lifecycle authoritative owner

#### Scenario: FullBody 只表示输出占用或表现语义
- **WHEN** Action module 输出 Dodge、LightAttack 或后续 action candidate
- **THEN** full-body 语义 MAY 表达 body/channel claim 或 animation layer profile
- **AND** MUST NOT 恢复为 Action runtime owner、Locomotion owner 或第二角色帧主线

#### Scenario: Dodge 行为语义保持
- **GIVEN** 玩家触发 Dodge request
- **WHEN** Action runtime 通过 core-owned module 推进 lifecycle
- **THEN** Directional Dodge 和 Backstep Dodge MUST 继续按现有 action claim、motion 和 animation 合同输出
- **AND** lifecycle 结束时 MUST 释放 claim，让后续帧可重新采用 Locomotion candidate

#### Scenario: Snapshot Restore 不依赖 Mono 生命周期
- **WHEN** rollback/replay 或测试对 Action runtime 执行 capture/restore
- **THEN** capture/restore MUST 作用于 core-owned pure runtime state
- **AND** MUST NOT 依赖启用、禁用或重新创建 `FullBodyActionRuntime` 才能恢复一致状态

## ADDED Requirements

### Requirement: 作者 UI 必须能从 ActionProfile 追到输出预览

系统 MUST 让作者从 Graph action request、Timeline window、非 Timeline 输出和 Runtime Debug 追溯到同一个 `ActionProfile` 的策略预览。Graph 和 Timeline MAY 显示只读 preview profile 或 runtime context，但 MUST NOT 保存完整网络策略。

#### Scenario: 从 Graph request 查看策略

- **WHEN** 作者选中提交 `Guard.ParryCounter` 的 Graph 节点
- **THEN** UI MUST 显示它引用的 ActionProfile
- **AND** MAY 提供跳转或只读预览，展示该 profile 的 activation、window、motion、cue 和 result 策略
- **AND** Graph 节点 MUST NOT 复制这些策略字段

#### Scenario: 从 Timeline HitWindow 查看策略

- **WHEN** 作者选中 Timeline 中的 HitWindow clip
- **THEN** UI MUST 允许选择 preview ActionProfile 或使用 runtime context 查看 resolved policy
- **AND** clip MUST 继续只保存 WindowType、WindowId、时间和业务参数

### Requirement: 非 Timeline 输出必须共享同一套策略解析

系统 MUST 允许 Timeline 和非 Timeline 输出使用同一套 Action Context 与 policy resolver。非 Timeline 输出 MUST NOT 引入第二套 action identity、ActionModule 或 per-node 网络策略。

#### Scenario: 持续格挡窗口

- **WHEN** Guard 状态的 Graph 或 Stage 直接产出 GuardWindow sample
- **THEN** 输出 MUST 携带 Action Context 或等价 ActionInstance 归属
- **AND** GuardWindow 的预测、历史和复制策略 MUST 从 ActionProfile 解析

#### Scenario: 非 Timeline 运动输出

- **WHEN** 某个 dodge 或 knockback 逻辑直接产出 motion sample
- **THEN** 输出 MUST 使用 MotionSourceType 和 Action Context 描述事实
- **AND** 网络策略 MUST 通过 resolver 获取

### Requirement: Runtime Debug 必须展示配置和运行事实的差异

Runtime Debug MUST 按 `ActionInstance` 展示 resolved policy、实际产生的 SyncFacts、adapter 生成的 outgoing packets、incoming decision/correction，以及被过滤或未发送的原因。Debug MUST 帮助作者判断是配置问题、输出事实缺失，还是网络映射问题。

#### Scenario: Window 没有发送

- **WHEN** 作者预期 HitWindow 会同步但运行时没有 outgoing packet
- **THEN** Debug MUST 能显示该 ActionInstance 是否产生了 HitWindow SyncFact
- **AND** MUST 能显示 resolver 是否将该 window 标记为 local only、digest only 或 missing policy

#### Scenario: 服务端纠正动作

- **WHEN** 收到 ActionInstance correction 或 reject
- **THEN** Debug MUST 显示对应 ActionProfile、ActionInstance、prediction key、resolved correction policy 和 incoming reason
- **AND** MUST 能关联后续 lifecycle transition 或表现修正

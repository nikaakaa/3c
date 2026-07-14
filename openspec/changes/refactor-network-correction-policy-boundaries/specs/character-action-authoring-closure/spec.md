## MODIFIED Requirements

### Requirement: ActionProfile Inspector 必须是策略主编辑入口

`ActionProfile` Inspector MUST 按 Identity、Network、Tags、Windows、Motion、Cues、Debug 分区展示。ActionProfile MUST NOT 引用 Graph、Timeline 或 Motion runtime 对象，也 MUST NOT 保存 actor motion correction application 或 Reject 处理方式。

#### Scenario: 配置攻击动作

- **WHEN** 作者编辑 `Attack.Light.01`
- **THEN** Identity 分区 MUST 配置 action id、display name 和 debug category
- **AND** Network 分区 MUST 配置 prediction、authority 和 replication policy
- **AND** Windows、Motion、Cues 分区 MUST 配置输出类型对应策略
- **AND** Inspector MUST NOT 显示 SmoothCorrection、ForceCorrection 或 CancelOnReject

#### Scenario: 动作策略复用

- **WHEN** 多个 Graph 分支都提交同一个 ActionProfile
- **THEN** 它们 MUST 使用同一份 profile 策略
- **AND** Graph 分支 MUST NOT 复制完整网络策略字段

### Requirement: 作者 UI 必须能从 ActionProfile 追到输出预览

系统 MUST 让作者从 Graph action request、TreeClip scope output、非 Timeline projected variable 和 Runtime Debug 追溯到同一个 ActionProfile 的策略预览。Graph Data Catalog 或 TreeClip Inspector MAY 显示只读 preview profile 或 runtime context，但 declaration projection MUST NOT 保存完整网络策略。actor motion correction MUST 通过 Motion/Network runtime debug 查看，不得投影为 ActionProfile 策略，也不得在本 change 中新增直接纠偏算法的作者配置。

#### Scenario: 从 Graph request 查看策略

- **WHEN** 作者选中提交 `Guard.ParryCounter` 的 Graph 节点
- **THEN** UI MUST 显示它引用的 ActionProfile
- **AND** MAY 提供跳转或只读预览，展示该 profile 的 window、motion、cue 和 result 策略

#### Scenario: 从 projected variable 查看策略

- **WHEN** 作者查看 `Attack1Hit` declaration 的 ActionWindow projection
- **THEN** UI MUST 显示 WindowType、WindowId、Digest 和可解析的 preview ActionProfile policy
- **AND** declaration MUST NOT 复制 authority、history、replication 或 actor correction application

### Requirement: Runtime Debug 必须展示配置和运行事实的差异

Runtime Debug MUST 按 `ActionInstance` 展示 resolved policy、实际产生的 SyncFacts、adapter 生成的 outgoing packets、incoming decision，以及被过滤或未发送的原因。Motion correction application 与 acknowledgement MUST 在 Motion/Network debug 中按 actor、input sequence 和 server tick 展示，不得伪装为 ActionProfile correction policy。Debug MUST 帮助作者判断是配置问题、输出事实缺失，还是网络映射问题。

#### Scenario: Window 没有发送

- **WHEN** 作者预期 HitWindow 会同步但运行时没有 outgoing packet
- **THEN** Debug MUST 能显示该 ActionInstance 是否产生了 HitWindow SyncFact
- **AND** MUST 能显示 resolver 是否将该 window 标记为 local only、digest only 或 missing policy

#### Scenario: 服务端纠正动作

- **WHEN** 收到 ActionInstance Correct 或 Reject decision
- **THEN** Debug MUST 显示对应 ActionProfile、ActionInstance、prediction key、incoming transition 和 reason
- **AND** 如果同 tick 另有 actor motion correction，Debug MUST 通过 MotionSyncDomain 记录单独关联其 application result 与 acknowledgement
- **AND** Debug MUST NOT 显示不存在的 resolved Action correction policy

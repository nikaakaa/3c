## MODIFIED Requirements

### Requirement: Corin 基础连招必须使用 Action StateMachine + Timeline 编排

Corin 基础连招 MUST 使用 Action StateMachine、ActionProfile 和带 Action Context 的 TimelineNode。状态至少包含 `None`、`Attack1` 和 `Attack2`。Attack Hit/Cancel 时间范围 MUST 由各自 Timeline 内的 Decision TreeClip 写入 Bool Frame/Frame scope variable；Hit/Cancel declaration MUST 使用显式 ActionWindow projection。连段条件 MUST 通过通用 Blackboard ValueNode 读取，不得使用 ActionWindowTrack、ActionWindowClip、ActionWindowActiveInfoNode 或 SubmitActionWindowSampleNode。连段 MUST 继续使用统一 Runnable stop、State source-exit、Action lifecycle 和 Timeline cancel 分层。

#### Scenario: Attack1 进入 Attack2

- **WHEN** Attack1Cancel variable 在当前 Tick为 true 且存在 Attack request
- **THEN** Action StateMachine MUST 从 Attack1 抢占到 Attack2
- **AND** source OnExit MUST 提交 `Cancel(ComboWindow)`
- **AND** Attack1Cancel projection MUST 生成归属 source ActionInstance 的 Window fact
- **AND** target activation MUST 消费 request

#### Scenario: Attack2 回到 Attack1

- **WHEN** Attack2Cancel variable 在当前 Tick为 true 且存在 Attack request
- **THEN** Action StateMachine MUST 从 Attack2 抢占到 Attack1
- **AND** condition query MUST NOT消费 request
- **AND** condition MUST NOT读取专用 ActionWindow cache

#### Scenario: 攻击正常结束

- **WHEN** Attack1 或 Attack2 root 正常完成且没有 Cancel variable
- **THEN** source MUST 提交 Complete 并通过完成边回到 None
- **AND** OnExit MUST 走无操作成功分支，不提交第二条 terminal transition

#### Scenario: Corin 资产迁移完成

- **WHEN** 作者检查 Attack1、Attack2、DodgeForward 和 DodgeBack Timeline
- **THEN** 四个资产 MUST 不包含 ActionWindowTrack 或 ActionWindowClip
- **AND** Hit、Cancel 和 IFrame 时间范围 MUST 只由 Decision TreeClip 表达
- **AND** 项目 MUST 不创建一次性 Tree asset


## MODIFIED Requirements

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

OnExit 与 Transition 条件 MUST 使用 `StateExitCauseInfoNode`、Action Context reader、Pipeline Blackboard ValueNode 和通用 Equal/And/Or/Not 等纯条件节点组合。所有 Timeline 时间门，包括需要 ActionInstance、策略解析或同步/debug 身份的动作窗口，都 MUST 由 Decision TreeClip 写入 scope variable；ConditionRuleGraph MUST NOT 使用 ActionWindow reader 或专用 timeline decision window cache。Action terminal lifecycle MUST 由显式 lifecycle 节点提交，StateMachine runtime MUST NOT 自动推导 Action lifecycle。

#### Scenario: ComboWindow 离开攻击

- **WHEN** Attack1 的 `Attack1Cancel` Decision TreeClip 在当前 Tick写入 true
- **AND** Attack request 成立且 source Action Context 仍 active
- **THEN** Attack1 Transition MUST 通过 Blackboard Bool reader 离开 source State
- **AND** Attack1 OnExit MUST 显式提交 `Cancel(ComboWindow)`
- **AND** 同一 declaration 的 ActionWindow projection MUST 保持 ActionInstance、policy 和 debug 身份

#### Scenario: Dodge 本地恢复门离开动作

- **WHEN** Dodge Decision TreeClip 在当前 Tick写入 `CanDodgeMoveCancel=true`
- **AND** 当前移动输入成立且 source Action Context 仍 active
- **THEN** Dodge Transition MUST 能离开 source State
- **AND** Dodge OnExit MUST 显式提交 `Cancel(DodgeMoveToRun)`
- **AND** Projection=None 的本地 gate MUST NOT产生 ActionWindowSample

#### Scenario: Locomotion 状态抢占

- **WHEN** RunEnd 通过普通输入 Transition 离开
- **THEN** runtime MUST 处理状态退出和 animation owner transition
- **AND** MUST NOT生成 Action Cancel、Interrupt 或 Abort


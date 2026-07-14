## MODIFIED Requirements

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

OnExit 分支 MUST 使用 `StateExitCauseInfoNode`、Action Context reader、ActionWindow reader、Pipeline Blackboard ValueNode 和通用 Equal/And/Or/Not 等纯条件节点组合条件。Action terminal lifecycle MUST 由显式 lifecycle 节点提交。需要 ActionInstance、策略解析或同步/debug 身份的动作时间窗 MUST 使用 ActionWindow；只表达本地 State Transition eligibility 的时间门 MAY 由 Decision TreeClip 写入 Frame Blackboard。系统 MUST NOT 新增状态或动作特化条件节点，也 MUST NOT 由 SM runtime 自动推导 Action lifecycle。

#### Scenario: ComboWindow 离开攻击

- **WHEN** Attack1 通过 Action CancelWindow Transition 离开
- **AND** source Action Context 仍 active
- **THEN** Attack1 OnExit MUST 显式提交 `Cancel(ComboWindow)`
- **AND** 连招 window MUST 保持 ActionWindow 身份

#### Scenario: Dodge 本地恢复门离开动作

- **WHEN** Dodge Decision TreeClip 在当前 Tick写入 `CanDodgeMoveCancel=true`
- **AND** 当前移动输入成立
- **AND** source Action Context 仍 active
- **THEN** Dodge Transition MUST 能离开 source State
- **AND** Dodge OnExit MUST 显式提交 `Cancel(DodgeMoveToRun)`
- **AND** 系统 MUST NOT 为该本地 gate 伪造 ActionWindowSample

#### Scenario: Locomotion 状态抢占

- **WHEN** RunEnd 通过普通输入 Transition 离开
- **THEN** runtime MUST 处理状态退出和 animation owner transition
- **AND** MUST NOT 生成 Action Cancel、Interrupt 或 Abort

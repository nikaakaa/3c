## ADDED Requirements

### Requirement: 状态抢占必须复用分层停止协议

StateMachine Transition 在 source root 尚未完成时条件成立，MUST 通过统一 source-exit 内核停止 source root、运行 State.OnExit 并进入 target。父 Tree abort `StateMachineNode` 时 MUST 通过 Runnable stop 协议请求同一个 source-exit 内核，并在没有 target 的情况下完成 SMNode 停止。系统 MUST NOT 为 State Transition 和 Tree abort 维护两套 State.OnExit 路径。

#### Scenario: RunEnd 被输入 Transition 抢占

- **WHEN** RunEnd root 仍为 Running
- **AND** `RunEnd -> RunStart` 条件成立
- **THEN** source root MUST 停止
- **AND** RunEnd OnExit MUST 完成后才进入 RunStart

#### Scenario: 上层 Selector 抢占 SMNode

- **WHEN** 上层 Selector 对正在运行的 StateMachineNode 发出 Self 或 LowerPriority stop request
- **THEN** StateMachineNode MUST 请求 active State graceful exit
- **AND** active State OnExit MUST 完成后 StateMachineNode 才能 StopCompleted
- **AND** StateMachineNode MUST NOT 进入另一个内部 target State

### Requirement: StateExitContext 必须保持层间翻译边界

StateMachine runtime MUST 将 `NodeStopContext` 或 State Transition 选择翻译为 transient `StateExitContext`。StateExitContext MUST 包含退出原因、source State、可选 target State、可选 Transition edge 和可选 parent Tree source/replacement identity。它 MUST NOT 写入 authoring asset、Pipeline Blackboard 或网络协议。

#### Scenario: LowerPriority abort 进入 State.OnExit

- **WHEN** SMNode 因 parent LowerPriority abort 进入 active State.OnExit
- **THEN** OnExit MUST 能读取退出来源为 Tree LowerPriority abort
- **AND** target State identity MUST 为空
- **AND** replacement Tree node identity MAY 可读

### Requirement: 状态退出业务必须通过纯条件读取与显式 lifecycle 节点表达

OnExit 分支 MUST 使用 `StateExitCauseInfoNode`、Action Context reader、ActionWindow reader 和通用 Equal/And/Or/Not 等 ConditionRuleGraph 节点组合条件。Action terminal lifecycle MUST 由显式 lifecycle 节点提交。系统 MUST NOT 新增 `OnLowerPriorityCancelNode`、`InterruptRunEndNode` 或由 SM runtime 自动推导 Action lifecycle。

#### Scenario: ComboWindow 离开攻击

- **WHEN** Attack1 通过 CancelWindow Transition 离开
- **AND** source Action Context 仍 active
- **THEN** Attack1 OnExit MUST 显式提交 `Cancel(ComboWindow)`

#### Scenario: Locomotion 状态抢占

- **WHEN** RunEnd 通过普通输入 Transition 离开
- **THEN** runtime MUST 处理状态退出和 animation owner transition
- **AND** MUST NOT 生成 Action Cancel、Interrupt 或 Abort

### Requirement: 状态退出逻辑屏障与表现收尾必须分离

source State root、Action lifecycle、Timeline gameplay output 和 animation owner MUST 在逻辑 stop barrier 内完成关闭或归属切换。Animation Presentation MAY 在逻辑退出后使用上一正式 outgoing plan 继续 blend，但 MUST NOT 为了表现淡出继续 tick source State、Timeline 或 Action 逻辑。

#### Scenario: Tree abort 后动画淡出

- **WHEN** SMNode graceful stop 完成
- **AND** Presentation 仍持有 outgoing animation plan
- **THEN** 父 Tree MAY 启动 replacement child
- **AND** Presentation MAY 继续输出 outgoing pose blend
- **AND** 旧 State MUST NOT 再产生 gameplay facts


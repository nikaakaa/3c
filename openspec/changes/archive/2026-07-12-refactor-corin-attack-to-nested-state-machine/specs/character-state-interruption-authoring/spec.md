## ADDED Requirements

### Requirement: 嵌套 StateMachine 停止必须逐层复用同一 source-exit 协议

当父 State root 中运行的嵌套 StateMachineNode 被 State transition、Tree graceful abort 或 ForceStop 停止时，stop context MUST 沿 execution path 逐层传播。内层 active State MUST 先停止 Root producer、运行 State.OnExit 并关闭 Action lifecycle；外层 State MUST 等待嵌套 StateMachineNode terminal 后完成自己的 OnExit。系统 MUST NOT 跳过内层 OnExit，也 MUST NOT 让父子 State 各自提交一条相同业务 terminal transition。

#### Scenario: 外层 Attack 被 Dodge replacement 抢占

- **WHEN** 外层 Attack State 收到指向 Dodge 的 replacement stop
- **AND** 内层 Attack1 仍 active
- **THEN** Attack1 Timeline MUST 在逻辑 stop barrier 内停止 gameplay 采样
- **AND** Attack1 OnExit MUST 根据原始 StateExitContext 提交一次 Cancel 或 Interrupt
- **AND** 外层 Attack OnExit MUST NOT 再提交 Action lifecycle
- **AND** replacement MUST 等待嵌套 stop 完成后启动

#### Scenario: Parent Tree LowerPriority abort

- **WHEN** LowerPriority abort 传播到包含嵌套 Attack StateMachineNode 的 Action StateMachineNode
- **THEN** inner leaf 读取的 OriginCause MUST 仍是 LowerPriorityAbort
- **AND** replacement edge/node identity 与 logic tick MUST 保持
- **AND** 内层和外层 MUST 共用同一 stop barrier

#### Scenario: 嵌套 ForceStop

- **WHEN** pipeline deactivate、dispose 或 Reset 对外层 StateMachineNode 执行 ForceStop
- **THEN** runtime MUST 立即释放所有 descendant State、Timeline、Blackboard、Action Context 和 animation membership
- **AND** runtime MUST NOT 伪造 gameplay Cancel、Interrupt 或 Abort
- **AND** 不得残留 descendant execution path frame

### Requirement: 嵌套停止的动画表现必须收敛到单一 transition domain

父子 StateMachine 的逻辑退出 MAY 在同一 Tick 产生多个 lifecycle 命令，但同一根 animation transition domain MUST 只保留一个最终有效 handoff。该收敛 MUST 使用显式 domain、leaf owner 和 supersede 语义，MUST NOT 通过忽略某个 runtime 的 release 或继续 tick source 动画实现。

#### Scenario: 内层 release 后父层 replacement

- **WHEN** inner Attack leaf release 与 outer Attack -> Dodge handoff 在同一 Tick 发生
- **THEN** outer replacement request MUST supersede inner terminal request
- **AND** frozen source contribution 或最终 visual pose MAY 被新 request 继续使用
- **AND** inner Attack 逻辑 MUST 已停止

## MODIFIED Requirements

### Requirement: 作者必须能显式配置动作退出语义

系统 MUST 让作者在动作流程离开点配置退出语义，而不是只配置普通 graph exit。至少 MUST 支持 `Complete`、`Cancel`、`Interrupt` 和 `Abort`；`Reject` 和 `Correct` MAY 来自网络 decision。State Transition、Tree graceful abort 和 ForceStop MUST 保持分层：State.OnExit 或正式 lifecycle 节点负责业务 terminal transition，Tree edge、通用 Runnable stop 和 TimelineNode MUST NOT 自动推导动作语义。

#### Scenario: 状态机正常结束攻击

- **WHEN** 作者配置攻击正常完成
- **THEN** root 或等价生命周期节点 MUST 提交 `Complete`
- **AND** 完成 Transition MUST NOT 再提交第二条 terminal transition

#### Scenario: CancelWindow 连段

- **WHEN** Attack1 或 Attack2 在 root 完成前通过 CancelWindow Transition 离开
- **THEN** source State.OnExit MUST 在 target Action 激活前提交 `Cancel(ComboWindow)`
- **AND** source Timeline MUST 通过 State Root stop 取消
- **AND** target State MUST 使用新的 Action Context

#### Scenario: Parent Tree abort 攻击 SMNode

- **WHEN** 攻击 StateMachineNode 因 Self、LowerPriority 或 Parent abort graceful stop
- **AND** source Action Context 仍 active
- **THEN** source State.OnExit MUST 能根据 StateExitContext 显式提交 `Cancel`、`Interrupt` 或 `Abort`
- **AND** SM runtime MUST NOT 自动选择其中一种业务语义
- **AND** parent Composite MUST 等待该 lifecycle 收口后启动 replacement

#### Scenario: Pipeline ForceStop

- **WHEN** Pipeline Shutdown 或 Dispose ForceStop 攻击 SMNode
- **THEN** runtime MUST 释放本地 Action/Timeline/animation owner runtime 资源
- **AND** MUST NOT 伪造 gameplay Cancel、Interrupt 或 Abort 网络事实


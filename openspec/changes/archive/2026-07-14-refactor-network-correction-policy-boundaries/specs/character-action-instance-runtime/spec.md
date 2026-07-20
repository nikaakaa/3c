## MODIFIED Requirements

### Requirement: ActionRuntime 必须是动作事务层而不是执行编排层

`ActionRuntime` MUST 只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition 状态流转。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Motion、应用 actor motion correction、播放 Cue 或裁决命中。生命周期变化 MUST 通过 `ActionLifecycleTransition` 或等价事实进入 runtime，而不是由 graph 是否继续 tick 隐式推断。

#### Scenario: 动作激活成功

- **WHEN** `ActionRuntime` 接受 `ActionActivationRequest`
- **THEN** 它 MUST 创建 `ActionInstance` 并返回 Action Context 所需的 instance id、prediction key、input sequence 和 start tick
- **AND** 后续 Timeline 播放、Motion 结算和 GameplayResult 裁决 MUST 由对应 stage 或 Graph 继续处理

#### Scenario: 动作生命周期变化

- **WHEN** Graph、Timeline、NetworkReceiveStage 或系统生命周期提交 `ActionLifecycleTransition`
- **THEN** `ActionRuntime` MUST 按 transition type 更新 `ActionInstance` 的 state、phase 和 reason
- **AND** terminal transition MUST 关闭 active instance，non-terminal transition MUST NOT 默认关闭 active instance

#### Scenario: 新动作覆盖旧动作

- **WHEN** `ActionRuntime` 接受新的 `ActionActivationRequest` 且当前 active action 可被新动作替换
- **THEN** 它 MUST 生成并应用旧动作的 `ActionLifecycleTransition(Cancel, reason = CancelledByNewAction)`
- **AND** activation outcome MUST 携带该 transition，供 Graph 或 Pipeline 转发到 `SyncFacts.Action.LifecycleTransitions`
- **AND** Graph 或 Pipeline MUST NOT 重新构造另一条等价 cancel transition 作为正式事实

#### Scenario: 动作事务校正

- **WHEN** 服务端 ActionInstance Correct decision 到达
- **THEN** `ActionRuntime` MUST 只更新 `ActionInstance` 的 corrected 状态和原因
- **AND** actor 位姿 correction MUST 作为独立 MotionSyncDomain 输入由 CharacterMotionStage 处理
- **AND** 表现修正 MUST 由 Presentation 根据正式运行结果处理

### Requirement: ActionRuntime 必须区分 terminal 和 non-terminal transition

系统 MUST 明确区分会结束动作事务的 terminal transition 和只更新状态的 non-terminal transition。`Complete`、`Cancel`、`Interrupt`、`Reject` 和 `Abort` MUST 关闭对应 active action instance；`Confirm` 和 `Correct` 默认 MUST NOT 关闭 active action instance，除非 incoming decision 明确携带终止语义。该规则 MUST 是 ActionRuntime invariant，MUST NOT 由 ActionProfile correction policy 配置。

#### Scenario: Confirm 不结束动作

- **WHEN** 服务端确认本地预测攻击成立
- **THEN** `ActionRuntime` MUST 将该实例标记为 confirmed 或等价状态
- **AND** 该动作 MAY 继续输出后续 window、motion、cue 或 result

#### Scenario: Reject 结束动作

- **WHEN** 服务端拒绝本地预测攻击
- **THEN** `ActionRuntime` MUST 将该实例标记为 rejected
- **AND** 后续节点读取该 Action Context MUST 失败
- **AND** 系统 MUST NOT 查询 CancelOnReject 配置决定是否结束

#### Scenario: Interrupt 结束动作

- **WHEN** 受击结果要求打断当前动作
- **THEN** `ActionRuntime` MUST 将当前动作标记为 interrupted 或 cancelled-like terminal state
- **AND** 后续受击表现、击退或硬直 MUST 通过新的状态/动作输出表达

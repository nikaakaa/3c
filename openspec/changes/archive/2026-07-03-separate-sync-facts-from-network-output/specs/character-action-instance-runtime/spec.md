## MODIFIED Requirements

### Requirement: ActionRuntime 必须是动作事务层而不是执行编排层

`ActionRuntime` MUST 只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition 状态流转。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Motion、播放 Cue 或裁决命中。生命周期变化 MUST 通过 `ActionLifecycleTransition` 或等价事实进入 runtime，而不是由 graph 是否继续 tick 隐式推断。

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

#### Scenario: 动作校正

- **WHEN** 服务端 correction 到达
- **THEN** `ActionRuntime` MUST 只更新 `ActionInstance` 的 corrected 状态和原因
- **AND** Motion 或 Presentation 修正 MUST 由后续 stage 根据 correction 输出处理


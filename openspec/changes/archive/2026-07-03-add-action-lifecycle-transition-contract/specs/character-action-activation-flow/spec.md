## REMOVED Requirements

### Requirement: ActionEndRequest 必须显式关闭动作事务

旧 `ActionEndRequest` 只表达单一 end 语义，不能区分正常完成、主动取消、外部打断、权威拒绝、权威修正和系统中止。本要求被 `ActionLifecycleTransition` 取代。

#### Scenario: 移除单一 End 语义

- **WHEN** 本变更实现完成
- **THEN** 作者和网络输出 MUST NOT 以 `ActionEndRequest` 作为动作离开的唯一正式语义
- **AND** 正常完成 MUST 迁移为 `ActionLifecycleTransition(Complete)`

## ADDED Requirements

### Requirement: 动作生命周期变化必须通过 ActionLifecycleTransition 表达

系统 MUST 使用 `ActionLifecycleTransition` 或等价生命周期事实表达动作事务的确认、完成、取消、打断、拒绝、修正和中止。系统 MUST NOT 因为 Graph、StateMachine 或 Timeline 在某一 tick 没有继续 tick 到某个节点，就隐式关闭 action context 或 action instance。

#### Scenario: Timeline 正常完成

- **WHEN** 带 Action Context 的攻击 Timeline 播放完成并需要结束该动作
- **THEN** Graph、Timeline 调度器或明确生命周期节点 MUST 提交 `ActionLifecycleTransition(Complete, reason = TimelineCompleted)`
- **AND** `ActionRuntime` MUST 将对应 `ActionInstance` 标记为完成并关闭 active context

#### Scenario: 闪避取消攻击

- **WHEN** 作者配置攻击可被闪避取消，并且 Graph 决定从攻击流程切到闪避流程
- **THEN** 系统 MUST 对旧攻击提交 `ActionLifecycleTransition(Cancel, reason = DodgeCancel)`
- **AND** 新闪避动作 MAY 通过新的 `ActionActivationRequest` 创建新的 `ActionInstance`

#### Scenario: 受击打断动作

- **WHEN** 角色在动作期间收到受击、硬直、击飞或控制结果
- **THEN** 系统 MUST 对当前动作提交 `ActionLifecycleTransition(Interrupt, reason = HitReact)` 或等价业务 reason
- **AND** 后续 hit react 或 knockback 输出 MUST NOT 自动继承被打断动作的 ActionInstanceId

#### Scenario: 服务端拒绝或修正

- **WHEN** NetworkReceiveStage 收到服务端对某次预测动作的 reject 或 correct decision
- **THEN** 系统 MUST 提交 `ActionLifecycleTransition(Reject)` 或 `ActionLifecycleTransition(Correct)`
- **AND** reject MUST 关闭对应 active context，correct MUST 保留或关闭 context 取决于 correction payload 的终止语义

#### Scenario: 系统中止

- **WHEN** actor despawn、组件禁用、场景切换或 pipeline dispose 时仍有 active action
- **THEN** 系统 MUST 提交或记录 `ActionLifecycleTransition(Abort, reason = SystemAbort)`
- **AND** 系统 MUST 清理该 action context，避免后续输出继续挂到旧实例

### Requirement: Action Context 必须是动作期间输出的显式输入

系统 MUST 让 action activation 成功后产生可传递的 Action Context。Timeline、Window、Motion、Cue、GameplayResult 和生命周期 transition 节点只有在显式接收到 Action Context 时，才 MAY 产出带 `ActionInstanceId` 的动作归属输出。系统 MUST NOT 默认读取 ambient current active action 作为输出归属来源。

#### Scenario: 轻攻击动作过程

- **WHEN** Graph 激活 `Attack.Light.01` 并得到 Action Context
- **THEN** 后续 Timeline、HitWindow、RootMotion、Cue 和 GameplayResult 输出 MAY 使用该 Action Context 写入同一个 `ActionInstanceId`
- **AND** 这些输出 MUST 能被 Runtime Debug 按同一次 ActionInstance 聚合显示

#### Scenario: 普通 Timeline 表现

- **WHEN** Graph 播放一个没有 Action Context 的普通表现 Timeline
- **THEN** Timeline MUST 正常输出 animation/cue 表现
- **AND** 系统 MUST NOT 自动把这些输出挂到当前 active action 上

#### Scenario: 生命周期结束后读取旧 Context

- **WHEN** 某个 Action Context 对应的 ActionInstance 已经 Complete、Cancel、Interrupt、Reject 或 Abort
- **THEN** 后续节点读取该 Action Context MUST 失败
- **AND** 系统 MUST NOT 继续产出带旧 ActionInstanceId 的动作 window、motion、cue 或 result

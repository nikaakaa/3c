## ADDED Requirements

### Requirement: Graph 必须通过 TrackedActionRequest 产生 ActionInstance
系统 MUST 让 Graph/BTSMTL 通过正式 `TrackedActionStartRequest` 或等价请求语义提交一次可追踪动作启动。`ActionRuntime` MUST 在接受请求后创建 `ActionInstance`。系统 MUST NOT 通过静态标记 Tree、SubTree、StateNode、TimelineNode 或节点模块来表达某个结构“属于某个动作”。

#### Scenario: 格挡反击启动
- **WHEN** Graph 消费 `Guard` 输入 request 并确认 `ReceivedAttackInParryWindow` 成立
- **THEN** Graph MUST 提交 `TrackedActionStartRequest(ActionId = Combat.ParryCounter, SourceInputRequest = Guard, TargetKey = LastAttacker)`
- **AND** `ActionRuntime` 接受后 MUST 返回包含 instance id、prediction key、input sequence、start tick 和 target snapshot 的 `ActionInstance`

#### Scenario: 普通 locomotion graph
- **WHEN** Graph 只读取移动输入并提交 locomotion motion intent
- **THEN** 系统 MUST NOT 强制创建 `ActionInstance`
- **AND** 该 Graph MUST NOT 被标记为 action tree 或 networked tree

### Requirement: TrackedActionStartRequest 必须携带动作事务来源
`TrackedActionStartRequest` MUST 表达 action id 或 action profile identity、source input request id、input sequence、simulation tick、target key、target snapshot 和 source graph identity。系统 MUST 使用这些字段把输入、Graph 决策、预测动作和服务端确认关联起来。

#### Scenario: 从输入 request 启动动作
- **WHEN** Graph 使用 `TryConsumeInputRequest("LightAttack")` 后提交攻击启动
- **THEN** `TrackedActionStartRequest` MUST 携带 source input request id 和 input sequence
- **AND** Debug MUST 能显示该 ActionInstance 来自哪次输入 request

#### Scenario: 从非输入事实启动动作
- **WHEN** Graph 因 `ReceivedAttackInParryWindow`、资源条件或 AI 决策启动动作
- **THEN** `TrackedActionStartRequest` MUST 允许 source input request id 为空
- **AND** MUST 仍携带 source graph identity 和 simulation tick 便于 debug

### Requirement: TrackedActionEndRequest 必须显式关闭动作事务
系统 MUST 提供 `TrackedActionEndRequest` 或等价请求语义关闭当前或指定 `ActionInstance`。关闭后，后续普通 Graph、Timeline、Motion、Combat 或 Cue facts MUST NOT 自动继承旧 ActionInstanceId。

#### Scenario: Timeline 动作结束
- **WHEN** `ParryCounterTimeline` 播放成功结束
- **THEN** Graph 或 pipeline MUST 提交 `TrackedActionEndRequest(instanceId, reason = TimelineCompleted)`
- **AND** `ActionRuntime` MUST 将该 instance 标记为 ended

#### Scenario: 动作被拒绝
- **WHEN** NetworkReceiveStage 收到服务端拒绝某次预测动作
- **THEN** `ActionRuntime` MUST 将匹配的 ActionInstance 标记为 rejected
- **AND** 后续 facts MUST NOT 继续挂到该 rejected instance

### Requirement: Timeline 必须只是可选事实来源
Timeline MAY 在当前 ActionContext 存在时产出带 ActionInstanceId 的 window、motion 或 cue facts。Timeline MUST NOT 自动创建 ActionInstance，也 MUST NOT 成为动作事务的唯一根。

#### Scenario: Timeline 攻击
- **WHEN** Graph 启动 `Attack.Light.01` 后播放 `LightAttack01Timeline`
- **THEN** Timeline 采样出的 HitWindow、CancelWindow、RootMotion 和 Cue facts MAY 携带当前 ActionInstanceId
- **AND** 这些 facts 的网络策略 MUST 通过 `ActionProfile + FactType` 解析

#### Scenario: 普通 Timeline 表现
- **WHEN** Graph 播放一个不属于 tracked action 的普通表现 Timeline
- **THEN** Timeline MUST 继续正常播放
- **AND** 系统 MUST NOT 因播放 Timeline 自动创建 ActionInstance

### Requirement: 非 Timeline 动作必须能使用同一 ActionInstance
系统 MUST 支持没有 Timeline 的动作事务通过 Graph、Motion、Combat 或 Presentation 直接产出 facts，并挂到当前 ActionInstanceId。

#### Scenario: 持续格挡
- **WHEN** Graph 启动 `Combat.Guard` 后没有播放 Timeline
- **THEN** Graph MAY 在持续按住期间产出 Guard window fact
- **AND** 该 fact MUST 能携带 `Combat.Guard` 的 ActionInstanceId

#### Scenario: 蓄力动作
- **WHEN** Graph 启动 `Attack.HeavyCharge` 并持续更新 charge amount
- **THEN** charge 相关 cue 或 state fact MUST 能挂到同一 ActionInstanceId
- **AND** 释放阶段 MAY 选择播放 Timeline 或直接产出 combat event

### Requirement: ActionRuntime 必须保持事务层职责
`ActionRuntime` MUST 只负责 profile 查询、start 验证、ActionInstance 创建和 confirm/reject/cancel/correct/end 状态流转。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Motion、播放 Cue 或裁决命中。

#### Scenario: 动作启动成功
- **WHEN** `ActionRuntime` 接受 `TrackedActionStartRequest`
- **THEN** 它 MUST 创建 ActionInstance 并更新 ActionContext
- **AND** 后续 Timeline 播放、Motion 结算和 Combat 裁决 MUST 由对应 stage 或 Graph 继续处理

#### Scenario: 动作校正
- **WHEN** 服务端 correction 到达
- **THEN** `ActionRuntime` MUST 只更新 ActionInstance 的 corrected 状态和原因
- **AND** Motion 或 Presentation 修正 MUST 由后续 stage 根据 correction fact 处理

### Requirement: 系统不得恢复结构身份式 ActionModule
系统 MUST NOT 恢复旧 `ActionModule`、`ActionSubTreeNode`、`ActionStateNode`、节点 action identity、ActionTree、AbilityTree 或 node membership table。任何 editor authoring 元素都 MUST 表达 request 提交，不得表达结构归属。

#### Scenario: 作者配置轻攻击
- **WHEN** 作者在 Graph 中配置轻攻击启动
- **THEN** 作者 MUST 配置提交 `TrackedActionStartRequest`
- **AND** MUST NOT 把 LightAttack SubTree 或 StateNode 标记为 `Attack.Light.01`

#### Scenario: 同一状态有多个事务
- **WHEN** 一个 GuardState 中既可以启动 `Combat.Guard` 又可以启动 `Combat.ParryCounter`
- **THEN** 系统 MUST 支持 Graph 在不同分支提交不同 tracked action request
- **AND** MUST NOT 要求 GuardState 静态绑定唯一 ActionProfile


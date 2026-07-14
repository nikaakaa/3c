## MODIFIED Requirements

### Requirement: Graph 必须通过 ActionActivationRequest 产生 ActionInstance
系统 MUST 让 Graph/BTSMTL 通过正式 `ActionActivationRequest` 提交一次动作激活意图。`ActionRuntime` MUST 在接受请求后创建 `ActionInstance`，并 MUST 返回可被后续 Timeline、Graph 输出或 SyncDomain output 显式携带的 action runtime context 或 handle。系统 MUST NOT 通过静态标记 Tree、SubTree、StateNode、TimelineNode 或节点模块来表达某个结构“属于某个动作”。

#### Scenario: 格挡反击启动
- **WHEN** Graph 消费 `Guard` 输入 request 并确认 `ReceivedAttackInParryWindow` 成立
- **THEN** Graph MUST 提交 `ActionActivationRequest(ActionId = Guard.ParryCounter, SourceInputRequest = Guard, TargetKey = LastAttacker)`
- **AND** `ActionRuntime` 接受后 MUST 返回包含 instance id、prediction key、input sequence、start tick 和 target snapshot 的显式 action runtime context

#### Scenario: 普通 locomotion graph
- **WHEN** Graph 只读取移动输入并提交 locomotion motion intent
- **THEN** 系统 MUST NOT 强制创建 `ActionInstance`
- **AND** 该 Graph MUST NOT 被标记为 action tree、ability tree 或 networked tree

### Requirement: Timeline 必须只是可选动作输出来源
Timeline MAY 在播放请求或等价显式参数中携带 action runtime context，从而产出带 `ActionInstanceId` 的 window sample、motion sample 或 cue event。Timeline MUST NOT 自动创建 `ActionInstance`，也 MUST NOT 通过 ambient current active action、Timeline asset membership 或 clip membership 自动继承动作归属。

#### Scenario: Timeline 攻击
- **WHEN** Graph 激活 `Attack.Light.01` 后播放 `LightAttack01Timeline`
- **THEN** Timeline playback request MUST 能显式携带该 action runtime context
- **AND** Timeline 采样出的 HitWindow、CancelWindow、RootMotion 和 Cue 输出 MAY 使用该 context 写入 `ActionInstanceId`
- **AND** 这些输出的网络策略 MUST 通过 `ActionProfile + OutputType` 解析

#### Scenario: 普通 Timeline 表现
- **WHEN** Graph 播放一个不属于动作事务的普通表现 Timeline
- **THEN** Timeline MUST 继续正常播放
- **AND** 系统 MUST NOT 因播放 Timeline 自动创建 `ActionInstance`
- **AND** 该 Timeline 的输出 MUST NOT 自动继承最后一个 active action

### Requirement: 非 Timeline 动作必须能使用同一 ActionInstance
系统 MUST 支持没有 Timeline 的动作事务通过 Graph、Motion、GameplayResult 或 Presentation 直接产出动作输出，并通过显式 action runtime context、instance id 或等价 handle 关联到同一 `ActionInstance`。系统 MUST NOT 让输出节点默认读取 ambient current active action 作为归属来源。

#### Scenario: 持续格挡
- **WHEN** Graph 激活 `Guard.Hold` 后没有播放 Timeline
- **THEN** Graph MAY 在持续按住期间产出 Guard window sample
- **AND** 该 sample MUST 通过显式 action context 携带 `Guard.Hold` 的 `ActionInstanceId`

#### Scenario: 输出缺少动作上下文
- **WHEN** Graph 或 Timeline 试图提交 action window、action cue 或 action-scoped gameplay result
- **AND** 没有提供有效 action runtime context 或 instance id
- **THEN** 系统 MUST 拒绝该 action-scoped 输出或将其作为非 action SyncDomain 输出处理
- **AND** 系统 MUST NOT 自动使用当前 active action 补齐归属

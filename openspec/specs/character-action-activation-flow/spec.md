# character-action-activation-flow Specification

## Purpose
定义 Graph/BTSMTL 如何通过动作激活请求建立 `ActionInstance`，以及 Timeline、Motion、Cue、GameplayResult 输出如何在运行时关联该实例。
## Requirements
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

### Requirement: ActionActivationRequest 必须携带动作事务来源

`ActionActivationRequest` MUST 表达 action id 或 action profile identity、source input request id、input sequence、local logic tick、target key、target snapshot 和 source graph identity。系统 MUST 使用这些字段把输入、Graph 决策、本地预测动作和服务端确认关联起来。服务端确认或拒绝 MAY 额外携带 `ServerTick`，但 `ActionActivationRequest` 的本地来源 tick MUST NOT 使用服务端 tick。

#### Scenario: 从输入 request 启动作

- **WHEN** Graph 使用 `TryConsumeInputRequest("LightAttack")` 后提交攻击激活
- **THEN** `ActionActivationRequest` MUST 携带 source input request id、input sequence 和 local logic tick
- **AND** Debug MUST 能显示该 `ActionInstance` 来自哪次输入 request

#### Scenario: 从非输入条件激活动作

- **WHEN** Graph 因 `ReceivedAttackInParryWindow`、资源条件或 AI 决策激活动作
- **THEN** `ActionActivationRequest` MUST 允许 source input request id 为空
- **AND** MUST 仍携带 source graph identity 和 local logic tick 便于 debug

### Requirement: Timeline 必须只是可选动作输出来源

Timeline MAY 在播放请求中携带显式 Action Context，使 Decision TreeClip 写入的 projected scope variable 生成带 ActionInstanceId 的 Window sample，并使其它正式 Track 生成 motion sample 或 cue event。Timeline MUST NOT 自动创建 ActionInstance，也 MUST NOT 通过 ambient current action、Timeline asset membership、TreeClip membership 或 declaration owner 自动继承动作归属。Timeline 与 ActionProfile MUST NOT 保存 WindowType 对应的网络策略；当前 Network Model adapter MUST 使用 Action Context 对应的稳定 ActionId 从 model profile 解析 effective policy。

#### Scenario: Timeline 攻击

- **WHEN** Graph 激活 `Attack.Light.01` 后播放 `LightAttack01Timeline`
- **THEN** Timeline playback request MUST 携带该 Action Context
- **AND** Hit/Cancel Decision TreeClip 的 projected variable MUST 使用该 context 生成 ActionWindowSample
- **AND** RootMotion 和 Cue 输出 MAY 使用相同 context 写入 ActionInstanceId
- **AND** 后续网络策略解析 MUST 由当前 Network Model adapter 完成

#### Scenario: 普通 Timeline 表现

- **WHEN** Graph 播放不属于动作事务的普通表现 Timeline
- **THEN** Timeline MUST 继续正常播放
- **AND** Projection=None 的 TreeClip variable MAY 作为本地条件
- **AND** ActionWindow-bound variable MUST 因缺少 Action Context 而拒绝事实投影

### Requirement: 非 Timeline 动作必须能使用同一 ActionInstance

系统 MUST 支持没有 Timeline 的动作事务通过 Graph 写入有 scope 的 Blackboard variable，并通过相同显式 fact projection 产出动作输出。需要 ActionWindow projection 的写入 MUST 携带显式 Action Context；系统 MUST NOT保留 SubmitActionWindowSampleNode，也 MUST NOT默认读取 ambient current active action。

#### Scenario: 持续格挡

- **WHEN** Graph 激活 `Guard.Hold` 后没有播放 Timeline
- **THEN** Graph MAY 在持有显式 Action Context 时每 Tick写入 Guard window Frame variable
- **AND** 相同 projection stage MUST 生成携带 `Guard.Hold` ActionInstanceId 的 sample

#### Scenario: 输出缺少动作上下文

- **WHEN** Graph 或 Timeline 写入 ActionWindow-bound variable
- **AND** 没有提供有效 Action Context
- **THEN** 系统 MUST 拒绝该 action-scoped projection
- **AND** 系统 MUST NOT自动使用当前 active action 补齐归属

### Requirement: ActionRuntime 必须保持事务层职责
`ActionRuntime` MUST 只负责 profile 查询、activation 验证、ActionInstance 创建和 lifecycle transition 状态流转。`ActionRuntime` MUST NOT tick Graph、播放 Timeline、采样 Motion、播放 Cue 或裁决命中。

#### Scenario: 动作激活成功
- **WHEN** `ActionRuntime` 接受 `ActionActivationRequest`
- **THEN** 它 MUST 创建 `ActionInstance` 并更新 ActionContext
- **AND** 后续 Timeline 播放、Motion 结算和 GameplayResult 裁决 MUST 由对应 stage 或 Graph 继续处理

#### Scenario: 动作校正
- **WHEN** 服务端 correction 到达
- **THEN** `ActionRuntime` MUST 只更新 `ActionInstance` 的 corrected 状态和原因
- **AND** Motion 或 Presentation 修正 MUST 由后续 stage 根据 correction 输出处理

### Requirement: 系统不得恢复结构身份式 ActionModule
系统 MUST NOT 恢复旧 `ActionModule`、`ActionSubTreeNode`、`ActionStateNode`、节点 action identity、ActionTree、AbilityTree 或 node membership table。任何 editor authoring 元素都 MUST 表达 action activation request 或 action output，不得表达结构归属。

#### Scenario: 作者配置轻攻击
- **WHEN** 作者在 Graph 中配置轻攻击启动
- **THEN** 作者 MUST 配置提交 `ActionActivationRequest`
- **AND** MUST NOT 把 LightAttack SubTree 或 StateNode 标记为 `Attack.Light.01`

#### Scenario: 同一状态有多个事务
- **WHEN** 一个 GuardState 中既可以激活 `Combat.Guard` 又可以激活 `Combat.ParryCounter`
- **THEN** 系统 MUST 支持 Graph 在不同分支提交不同 action activation request
- **AND** MUST NOT 要求 GuardState 静态绑定唯一 ActionProfile

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
- **AND** reject MUST 关闭对应 active context，correct 默认保留 context，只有 incoming decision 明确携带终止语义时才关闭

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

### Requirement: Action 必须使用统一 Gameplay Effect 作为玩法状态输入

Action activation 和 lifecycle 决策 MUST 从角色统一 Gameplay Effect 读取 tag、attribute 与 effect 事实。`ActionRuntime` MUST 删除私有字符串 tag 集合、`SetTag` 和等价状态副本，并 MUST NOT 承担 effect tick、modifier 聚合或 attribute 存储。

#### Scenario: Graph 判断动作是否可激活

- **WHEN** 动作要求 `State.Grounded`、不存在 `State.CrowdControl.Stun` 且 Stamina 足够
- **THEN** Graph MUST 从统一 Gameplay Effect 读取这些条件后提交 `ActionActivationRequest`
- **AND** `ActionRuntime` MUST 只处理事务 profile、验证结果和实例生命周期

#### Scenario: Action 生命周期结束

- **WHEN** ActionInstance 完成且存在以该 ActionInstanceId 为 source 的临时 effect
- **THEN** 正式协调边界 MAY 按显式 removal policy 移除对应 effect
- **AND** `ActionRuntime` MUST NOT 遍历或直接修改 active effect collection


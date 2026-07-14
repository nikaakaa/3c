# character-action-authoring-closure Specification

## Purpose
定义动作 profile、Graph activation request、Timeline Decision TreeClip 时间事实、Blackboard fact projection 和 Runtime Debug 的作者闭环。
## Requirements
### Requirement: CharacterPipelineDefinition 必须配置 ActionProfile 库
系统 MUST 让 `CharacterPipelineDefinition` 或等价角色管线配置持有正式 ActionProfile 列表。Pipeline 初始化时 MUST 将这些 profile 注册到 `ActionRuntime`。缺失、空 action id 或重复 action id MUST 作为配置错误报告，不得通过字符串全局搜索或 fallback profile 继续运行。

#### Scenario: 角色配置动作库
- **WHEN** 作者打开 `CharacterPipelineDefinition`
- **THEN** Inspector MUST 允许配置该角色可用的 ActionProfile 列表
- **AND** 初始化 pipeline 时 MUST 注册这些 profile

#### Scenario: 重复 action id
- **WHEN** 两个 ActionProfile 使用同一个 action id
- **THEN** 配置校验 MUST 报错
- **AND** 系统 MUST NOT 随机选择其中一个作为 fallback

### Requirement: Graph authoring 必须表达 request 提交而不是结构归属
Graph authoring UI MUST 提供普通 request submit authoring 入口，用于配置 action activation request 的 action profile、source input request、target key、是否消费输入 request 和 instance id blackboard 输出。Graph 内部临时读写 MUST 命名为 blackboard，不得命名为 fact。该 UI MUST NOT 命名或实现为 ActionModule、AbilityNode、ActionSubTree 或静态 node identity。

#### Scenario: 创建格挡反击提交入口
- **WHEN** 作者在 GuardState 的行为 Graph 中创建 action activation request 提交入口
- **THEN** UI MUST 允许选择 `Guard.ParryCounter` ActionProfile
- **AND** UI MUST 允许选择 `Guard` 作为 source input request
- **AND** UI MUST 允许填写 `LastAttacker` 作为 target key

#### Scenario: 编辑网络策略
- **WHEN** 作者选中 Graph 中的 action activation request 提交入口
- **THEN** UI MUST NOT 暴露 HitWindow authority、Motion network visibility 或 Cue playback policy
- **AND** 作者 MUST 到 ActionProfile Inspector 中修改这些策略

### Requirement: ActionProfile Inspector 必须是策略主编辑入口

`ActionProfile` Inspector MUST 是 gameplay 动作定义主入口，按 Identity、Tags、Block/Cancel、Target 和 Debug 分区展示。它 MUST 不编辑任何具体 Network Model 的 prediction、authority、replication、window/motion/cue/result 网络策略，也 MUST 不提供 packet preview。模型策略 MUST 由对应 model profile Inspector 编辑。

#### Scenario: 编辑 Attack ActionProfile

- **WHEN** 作者选中 Attack ActionProfile
- **THEN** UI MUST 展示动作身份与 gameplay 约束
- **AND** MUST 提供到已绑定 model policy 的只读导航或缺失提示，而不是内联编辑网络字段

### Requirement: Runtime Debug 必须展示 request 到 outputs 的完整链路
系统 MUST 提供或预留 Runtime Debug 数据，按 input request、action activation request、ActionInstance、window sample、motion sample、gameplay result、cue event 和 network result 展示链路。

#### Scenario: 本地预测格挡反击
- **WHEN** 本地预测启动 `Guard.ParryCounter`
- **THEN** Debug MUST 显示 source input request、ActionInstanceId、ActionId、PredictionKey、InputSequence、StartTick、Phase 和 State
- **AND** Debug MUST 能关联显示该实例产生的 HitWindow、InvulnerableWindow、RootMotion、ParryFlash 和 GameplayResult

#### Scenario: 服务端拒绝
- **WHEN** 服务端拒绝该 ActionInstance
- **THEN** Debug MUST 显示 rejected instance id、prediction key 和 reason
- **AND** Debug MUST 显示后续 terminal lifecycle 与表现取消状态

### Requirement: UI 闭环必须支持 Timeline 和非 Timeline 动作

作者 MUST 能使用同一套 ActionProfile、Graph request submit UI、scope variable fact projection 和 Runtime Debug 配置 Timeline 动作与非 Timeline 动作。Timeline 时间窗口 MUST 使用 Decision TreeClip 写 scope variable；非 Timeline 持续窗口 MUST 使用具有显式 Action Context provenance 的 scope variable 写入。系统 MUST NOT 要求非 Timeline 动作创建虚假 Timeline，也 MUST NOT 保留 SubmitActionWindowSampleNode 作为第二输出路径。

#### Scenario: Timeline 攻击

- **WHEN** 作者配置轻攻击
- **THEN** Graph request submit UI MUST 提交 `Attack.Light.01`
- **AND** Hit 和 Cancel 时间范围 MUST 由 Decision TreeClip 配置
- **AND** 对应 scope variable MUST 通过 projection 生成 Window facts

#### Scenario: 非 Timeline 格挡

- **WHEN** 作者配置持续格挡
- **THEN** Graph MUST 能在持有显式 Action Context 时写入 Guard window scope variable
- **AND** 相同 projection stage MUST 生成 Guard ActionWindowSample

### Requirement: 作者 UI 必须使用 Action Context 口径

系统 MUST 在作者可见 UI 中使用 `Action Context` 表达动作期间输出的归属输入/输出。作者主要编辑界面 MUST NOT 使用 `Action Handle Slot`、`ActionInstanceHandle` 或等价内部句柄词作为主要概念。内部实现 MAY 使用 slot、handle 或引用，但必须被封装在 Action Context 口径下。

#### Scenario: 配置动作激活节点

- **WHEN** 作者选中 `Activate Action Instance` 或等价动作激活节点
- **THEN** Inspector MUST 显示 `Output Action Context` 或等价业务字段
- **AND** MUST 通过正式 `ActionProfile` 或等价动作定义资产确定动作身份
- **AND** MUST NOT 要求作者手敲 `attack.handle`、`ActionId` 或等价字符串 key

#### Scenario: 配置 Timeline 节点

- **WHEN** 作者希望某个 Timeline 输出归属到一次动作
- **THEN** Timeline 节点 MUST 暴露 `Action Context` 输入或等价引用
- **AND** 空 Action Context MUST 表示普通 Timeline，不自动继承当前 active action

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

### Requirement: ActionScope 若引入必须只是作者组织层

如果系统引入 `ActionScope`、`ActionBody` 或等价编辑节点，它 MUST 只作为作者组织和默认 Action Context 继承工具。它 MUST NOT 让 subtree、StateNode、Timeline asset 或节点 membership 成为网络同步真相。

#### Scenario: Scope 内默认继承 Context

- **WHEN** 作者在 `ActionScope(Attack.Light.01)` 内放置 Timeline、Window、Cue 或 Result 节点
- **THEN** 这些节点 MAY 默认使用 scope 提供的 Action Context
- **AND** 最终 runtime 输出仍 MUST 携带 `ActionInstanceId` 和 lifecycle transition，而不是 subtree id

#### Scenario: Scope 离开

- **WHEN** `ActionScope` 的子流程正常完成、被取消或被打断
- **THEN** Scope MUST 将离开原因翻译为明确 `ActionLifecycleTransition`
- **AND** MUST NOT 靠 scope 停止 tick 来隐式销毁动作事务

### Requirement: 作者 UI 必须能从 ActionProfile 追到输出预览

作者 MUST 能从 ActionProfile、Graph request、TreeClip projection 和 Runtime Debug 追踪 ActionId/ActionInstanceId 与 gameplay outputs。网络 packet preview MUST 只出现在显式选择的 model profile/Debug 中，并通过稳定 ActionId 关联；ActionProfile MUST 不持有 expected packet 配置。

#### Scenario: 从 TreeClip 查看 HitWindow

- **WHEN** 作者查看 Attack HitWindow projection
- **THEN** UI MUST 显示 WindowType、WindowId 和 Action identity
- **AND** MAY 导航到 ServerAuthoritative model policy 的只读匹配结果
- **AND** MUST 不把该 model policy 复制到 TreeClip

### Requirement: 非 Timeline 输出必须共享同一套策略解析

Timeline 与非 Timeline 动作 MUST 继续产生相同 gameplay facts，并通过 ActionId/ActionInstanceId 关联。具体网络策略 MUST 由当前 model profile/resolver 统一解析；ActionProfile、Node 和 Blackboard declaration MUST 不成为第二 policy 来源。

#### Scenario: 非 Timeline GuardWindow

- **WHEN** 非 Timeline 动作产生 GuardWindow fact
- **THEN** fact MUST 使用正式 Action Context
- **AND** ServerAuthoritative adapter MUST 从 model Action policy 解析网络行为

### Requirement: Runtime Debug 必须展示配置和运行事实的差异

Runtime Debug MUST 按 `ActionInstance` 展示 resolved policy、实际产生的 SyncFacts、adapter 生成的 outgoing packets、incoming decision，以及被过滤或未发送的原因。Motion correction application 与 acknowledgement MUST 在 Motion/Network debug 中按 actor、input sequence 和 server tick 展示。Debug MUST 帮助作者判断是配置问题、输出事实缺失，还是网络映射问题。

#### Scenario: Window 没有发送

- **WHEN** 作者预期 HitWindow 会同步但运行时没有 outgoing packet
- **THEN** Debug MUST 能显示该 ActionInstance 是否产生了 HitWindow SyncFact
- **AND** MUST 能显示 resolver 是否将该 window 标记为 local only、digest only 或 missing policy

#### Scenario: 服务端纠正动作

- **WHEN** 收到 ActionInstance Correct 或 Reject decision
- **THEN** Debug MUST 显示对应 ActionProfile、ActionInstance、prediction key、incoming transition 和 reason
- **AND** 如果同 tick 另有 actor motion correction，Debug MUST 通过 MotionSyncDomain 记录其 application result 与 acknowledgement

### Requirement: Timeline 攻击闭环不得依赖 RootTree 平铺测试输出

作者配置 Timeline 攻击时，攻击时间事实 MUST 由 Timeline 内的 Decision TreeClip 写入 scope variable；Cue 仍由其正式 Timeline/Graph 输出模型表达。RootTree 主流程 MUST NOT 平铺 `SubmitActionWindowSample`、`SubmitGameplayCueNode` 或测试 GameplayResult 节点补充动作 body 事实，系统也 MUST NOT保留 ActionWindowTrack 作为另一条 Timeline Window 作者路径。

#### Scenario: Corin Attack1

- **WHEN** 作者配置 `Attack1` 为 Timeline 攻击
- **THEN** Hit/Cancel 时间范围 MUST 位于 `Attack1` Timeline 的 Decision TreeClip
- **AND** TreeClip MUST 写入对应 Bool Frame variables
- **AND** Gameplay/VFX/Camera cue MUST 继续位于其正式 Timeline 输出
- **AND** RootTree 主流程 MUST NOT平铺窗口、Cue 或结果测试节点

#### Scenario: 非 Timeline 动作

- **WHEN** 作者配置不播放 Timeline 的持续格挡或其它动作
- **THEN** Graph MAY 写入具有显式 projection 的 scope variable
- **AND** 输出仍 MUST 使用 Action Context 和 ActionProfile 策略解析

### Requirement: Dodge Action 必须通过 pipeline blackboard 公布 locomotion ownership

Corin DodgeForward 和 DodgeBack MUST 保持为 Action StateMachine 中唯一 Dodge 业务状态。Dodge OnEnter MUST 在 ActionInstance 成功激活后写入 pipeline blackboard `IsDodging=true`；所有 source-exit 的 OnExit MUST 写入 `IsDodging=false`。Dodge Timeline 的移动恢复门和 IFrame 时间范围 MUST 都由 Decision TreeClip 写入 Bool Frame variables：`CanDodgeMoveCancel` 保持 Projection=None，Dodge IFrame declaration 使用显式 ActionWindow projection。Locomotion MUST 只读取 ownership fact，不得复制 Dodge request、ActionProfile、Timeline、motion curve、IFrame 或恢复门。

#### Scenario: Dodge 激活后让渡 locomotion 所有权

- **WHEN** DodgeForward 或 DodgeBack 成功激活 ActionInstance
- **THEN** 对应 OnEnter MUST 写入 `IsDodging=true`
- **AND** Locomotion StateMachine MUST 能读取该值进入 ActionOverride

#### Scenario: Dodge 正常完成或被打断

- **WHEN** Dodge state 正常完成、被 State transition 抢占或被上层 tree stop
- **THEN** source OnExit MUST 写入 `IsDodging=false`
- **AND** Locomotion MUST 能按当前 MoveAxis 收回所有权

#### Scenario: 单一 Dodge 动作真相

- **WHEN** Locomotion 处理 Dodge 活跃期间的所有权
- **THEN** Locomotion MUST NOT创建第二个 Dodge action state 或引用 Dodge Timeline
- **AND** Dodge request MUST 继续只由 Action 激活接受点消费
- **AND** Dodge IFrame MUST 由 Decision TreeClip scope variable projection 产生，不得保留 ActionWindowTrack

### Requirement: TreeClip 与 Scope Variable 必须是 Timeline Window 唯一作者入口

Decision TreeClip 与 Bool Frame scope variable MUST 继续作为 Timeline Window 唯一时间作者入口。Projection MUST 只保存 WindowType、WindowId 和 Digest gameplay fact 声明；authority、history、replication 和 packet policy MUST 来自当前 Network Model profile，不得保存在 ActionProfile、TreeClip 或 declaration。

#### Scenario: Attack HitWindow

- **WHEN** TreeClip 在本 tick 写入 HitWindow declaration
- **THEN** projection MUST 生成对应 ActionWindow fact
- **AND** ServerAuthoritative model policy MUST 决定是否进入 history/packet

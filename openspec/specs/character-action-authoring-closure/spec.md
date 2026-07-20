# character-action-authoring-closure Specification

## Purpose
定义动作 profile、Graph activation request、Timeline Decision TreeClip 时间事实、Blackboard fact projection 和 Runtime Debug 的作者闭环。
## Requirements
### Requirement: CharacterPipelineDefinition 必须配置 ActionProfile 库
系统 MUST让 `CharacterPipelineDefinition` 持有正式 ActionProfile 列表。Compiler MUST将这些 profile 编译为 Program Action catalog，CharacterSimulationState 的 Action operation MUST只读取该 catalog。缺失、空 action id 或重复 action id MUST作为编译错误报告，不得通过字符串全局搜索或 fallback profile 继续运行。

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
- **THEN** UI MUST不暴露Action级网络策略                                                     
- **AND** Model Definition只配置fact-kind与producer coverage       

### Requirement: ActionProfile Inspector 必须是策略主编辑入口

`ActionProfile` Inspector MUST是gameplay动作定义主入口，按Identity、Tags、Block/Cancel、Target和Debug分区展示。它 MUST不编辑Network Model的prediction、authority、replication、history或packet参数，也 MUST不提供packet preview或逐Action model policy导航。                                                                      

#### Scenario: 编辑 Attack ActionProfile

- **WHEN** 作者选中 Attack ActionProfile
- **THEN** UI MUST 展示动作身份与 gameplay 约束
- **AND** MUST不显示逐Action网络字段或虚构的policy绑定                                             

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
- **AND** 相同 projection stage MUST 生成 Guard `ActionWindowFact`

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

#### Scenario: 语义窗口替换动作

- **WHEN** Attack leaf 在 root 完成前通过 ComboAccept、RecoveryEarly 或 RecoveryLate 离开
- **THEN** source State.OnExit MUST 在 target 激活前提交 `Cancel(RecoveryCancel)`
- **AND** source Timeline MUST 通过 State root stop 取消
- **AND** target MUST 使用新的 Action Context
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

作者 MUST从ActionProfile、Graph request、TreeClip projection和Runtime Debug追踪ActionId/ActionInstanceId与gameplay outputs。Model Debug MAY显示fact kind、ProducerId、packet与发送结果；ActionProfile MUST不持有expected packet或逐Action网络策略。                                         

#### Scenario: 从 TreeClip 查看 HitWindow

- **WHEN** 作者查看 Attack HitWindow projection
- **THEN** UI MUST 显示 WindowType、WindowId 和 Action identity
- **AND** MAY导航到Model的fact-kind与producer coverage                   
- **AND** MUST不把Model配置复制到TreeClip        

### Requirement: 非 Timeline 输出必须共享同一套策略解析

Timeline与非Timeline动作 MUST产生相同GameplayFacts并以ActionId/ActionInstanceId关联。Model Egress MUST只按显式fact kind与producer coverage消费Finalize输出；ActionProfile、Node和Blackboard MUST不成为网络配置来源。                            

#### Scenario: 非 Timeline GuardWindow

- **WHEN** 非 Timeline 动作产生 GuardWindow fact
- **THEN** fact MUST 使用正式 Action Context
- **AND** 未映射ActionWindow packet时 MUST保留为本地Gameplay输出          

### Requirement: Runtime Debug 必须展示配置和运行事实的差异

Runtime Debug MUST按 `ActionInstance` 展示GameplayFact、PresentationCommand与incoming ingress。Model Debug MUST按actor、input sequence、server tick、fact kind与ProducerId展示packet、过滤原因、reconciliation和ack。Debug MUST区分动作输出缺失、模型coverage不支持与网络运行错误。                                                                                                                                                   

#### Scenario: Window 没有发送

- **WHEN** 作者预期 HitWindow 会同步但运行时没有 outgoing packet
- **THEN** Debug MUST 能显示该 ActionInstance 是否产生了 HitWindow SyncFact
- **AND** MUST显示Model是否正式支持ActionWindow fact kind                                          

#### Scenario: 服务端纠正动作

- **WHEN** 收到 ActionInstance Correct 或 Reject decision
- **THEN** Debug MUST 显示对应 ActionProfile、ActionInstance、prediction key、incoming transition 和 reason
- **AND** 同tick存在body correction时Model Debug MUST记录restore/replay与ack                                                          

### Requirement: Timeline 攻击闭环不得依赖 RootTree 平铺测试输出

Timeline 攻击的时间事实 MUST 由 inline Timeline Decision TreeClip 写 owner-local scope variable；Cue 由正式 Timeline/Graph 输出。RootTree MUST NOT 平铺 window、cue 或测试 GameplayResult 节点，也 MUST NOT 保留 ActionWindowTrack 第二路径。

#### Scenario: Corin Attack1..5

- **WHEN** 作者配置五段 Timeline 攻击
- **THEN** Hit、ComboAccept、RecoveryEarly 与 RecoveryLate MUST 位于各自 inline Timeline TreeClip
- **AND** TreeClip MUST 写对应 owner-local Bool Frame declaration
- **AND** RootTree MUST NOT 平铺窗口、Cue 或结果测试节点

#### Scenario: 非 Timeline 动作

- **WHEN** 作者配置非 Timeline 持续动作
- **THEN** Graph MAY 写具有显式 Action Context projection 的 scope variable
- **AND** 输出仍 MUST 使用正式 Action Context
### Requirement: Full-body Action 必须通过唯一 pipeline blackboard 事实公布 locomotion ownership

Attack、Dodge 与未来 full-body Action MUST 只通过 pipeline Blackboard `HasActionLocomotionOwnership` 让渡 locomotion。ActionInstance 成功激活后写 true，所有 source exit 对称写 false。Locomotion MUST 只读 ownership，不得复制 request、ActionProfile、Timeline、motion、window 或 lifecycle。系统 MUST 删除按动作种类选择恢复状态的路由事实。

#### Scenario: Full-body Action 激活

- **WHEN** Attack 或 Dodge 激活成功
- **THEN** OnEnter MUST 写 `HasActionLocomotionOwnership=true`
- **AND** Locomotion MUST 进入无表现输出的 ActionOverride

#### Scenario: Full-body Action 结束

- **WHEN** Action 完成、被替换或被上层 stop
- **THEN** OnExit MUST 写 `HasActionLocomotionOwnership=false`
- **AND** Locomotion MUST 按 Move input 进入 RunLoop 或 Idle

#### Scenario: 单一 Action 真相

- **WHEN** Locomotion 处理 ownership
- **THEN** MUST NOT 创建第二个 Action state 或引用 Action Timeline
- **AND** request MUST 只由 target activation 消费
### Requirement: TreeClip 与 Scope Variable 必须是 Timeline Window 唯一作者入口

Decision TreeClip 与 owner-local Bool Frame scope variable MUST 是 Timeline Window 唯一时间入口。Projection MUST 只保存 WindowType、WindowId、Digest 和 Action Context provenance；网络策略只属于当前 Network Model。ConditionRuleGraph MAY 用 `ActionWindowActiveInfoNode` 只读同帧 candidate，但 MUST NOT 建第二份 fact、Blackboard key、cache 或 registry。RootTree MUST NOT 暴露逐段 Cancel/MoveCancel declaration。

#### Scenario: Attack HitWindow

- **WHEN** TreeClip 写 HitWindow declaration
- **THEN** projection MUST 生成同一 ActionInstance 的 Window fact
- **AND** Network Model 决定 history 与 packet

#### Scenario: Transition 读取 RecoveryEarly

- **WHEN** TreeClip 写 projected `RecoveryEarly`
- **THEN** typed query MUST 同帧读取同一 ActionInstance、WindowId 和 Digest

#### Scenario: Projection=None

- **WHEN** TreeClip 写普通本地变量
- **THEN** ValueNode MAY 读取
- **AND** typed WindowType query MUST NOT 命中                                                                                                                                                                                                                                                                                               
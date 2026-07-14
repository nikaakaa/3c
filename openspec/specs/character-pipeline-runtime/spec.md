# character-pipeline-runtime Specification

## Purpose
定义 `CharacterPipeline` 的 Unity 装配、分阶段运行和正式边界：Host 只负责装配注册，pipeline 通过 input、BTSMTL、motion、presentation、network receive/send 和 frame cleanup 交换数据，不恢复 BBB 状态类、旧 SO 或分裂控制器路径。
## Requirements
### Requirement: CharacterPipelineHost 只负责装配和注册

系统 MUST 使用 `CharacterPipelineHost` 作为每个角色的 Unity 装配点。Host MUST 只负责序列化唯一 ActorId、角色管线定义、Animancer、visual root、Logic Pose Adapter、按 authority mode 需要的 Motion Executor Adapter 和其它 Unity 组件引用，创建 pipeline，并注册和释放 pipeline。Host MUST NOT 直接序列化 BTSMTL RootTree 或 BTSMTL component 类型，MUST NOT 写入动作状态判断、状态切换、motion 结算或 GameplayResult 裁决逻辑。Host MUST NOT 把 concrete `CharacterController` 直接传入 CharacterPipeline。

#### Scenario: Host 创建 LocalSolver pipeline

- **WHEN** Host 以 LocalSolver 初始化
- **THEN** Host MUST 使用 `CharacterPipelineDefinition`、Animancer、显式 Logic Pose Port、显式 Motion Executor 和输入配置创建 `CharacterPipeline`
- **AND** Host MUST NOT 创建 BBB `PlayerBaseState` 或 `PlayerStateRegistry`
- **AND** BTSMTL RootTree MUST 通过 `CharacterPipelineDefinition` 间接进入 pipeline

#### Scenario: Host 创建 ExternalPose pipeline

- **WHEN** Host 以 ExternalPose 初始化
- **THEN** Host MUST 提供显式 Logic Pose Port
- **AND** MUST 不要求或调用 `CharacterController` Motion Executor

#### Scenario: Host 不承担业务逻辑

- **WHEN** 一帧 gameplay tick 执行
- **THEN** Host MUST 只作为已创建 pipeline 的持有者
- **AND** 输入处理、图执行、motion 和 presentation MUST 位于 pipeline 或 stage 中

### Requirement: Character ActorId 必须由 Host 单点装配

每个可运行 CharacterPipelineHost MUST 持有唯一非空 ActorId，并在创建时传给 CharacterPipeline。Pipeline MUST 将同一 ActorId 提供给 CharacterGraphContext 与角色 Gameplay Effect 适配层。其它 binding MAY 读取 Host.ActorId，但 MUST NOT 保存可独立编辑的重复角色 identity。

#### Scenario: Host 缺少 ActorId

- **WHEN** CharacterPipelineHost 的 ActorId 为空
- **THEN** Host MUST 明确拒绝创建 CharacterPipeline
- **AND** 系统 MUST NOT 从 GameObject 名称、instance id 或网络配置生成 fallback identity

#### Scenario: 角色被模型 binding 注册

- **WHEN** 模型 binding 需要 subject actor identity
- **THEN** binding MUST 读取 CharacterPipelineHost.ActorId
- **AND** CharacterPipeline、Graph 与 GE Self Context MUST 使用同一值

### Requirement: CharacterPipeline 是纯 C# 运行时主体

系统 MUST 将 `CharacterPipeline` 实现为纯 C# 对象，并由 GameplayTickSystem 提供 logic/presentation 时间。Tick context MUST 表达 fixed/presentation delta、render frame、local logic tick、input sequence 和 interpolation alpha；MUST 不保存具体 Network Model authority mode。CharacterPipeline 自身 MUST 显式持有 CharacterInputSource 与 CharacterMotionAuthority，且 MUST 不直接读取 Unity Time、transport 或 model packet。

#### Scenario: GameplayTickSystem 驱动角色

- **WHEN** tick system 推进 CharacterPipeline
- **THEN** context MUST 提供统一时间和 sequence
- **AND** CharacterPipeline MUST 从自身正式配置读取 input source 与 motion authority

### Requirement: Graph 执行上下文来自 CharacterGraphContext

系统 MUST 使用 CharacterGraphContext 作为 RootTree context，提供 Timeline 请求、typed input、gameplay blackboard、tick pose、Character 语义外部输入、动画选择和 diagnostics。Context MAY 暴露 input source 与 motion authority，但 MUST 不暴露 Network Model id、model packet、endpoint、history 或 transport。

#### Scenario: Graph 读取外部动作事实

- **WHEN** ExternalFacts input source 注入动作 request
- **THEN** Graph MUST 通过 CharacterInputFrame/request buffer 读取
- **AND** MUST 不读取 ServerAuthoritative ActionReplication packet

### Requirement: BTSMTLPhase 驱动 BTSMTL RootTree 和 Timeline playback

系统 MUST 使用 `CharacterBTSMTLPhase` 作为 BT、SM 和 Timeline 的统一角色逻辑编排 phase。该 phase MUST 内部持有 `BehaviorTreeRuntime` 和 `TimelinePlaybackScheduler`，并保持 BTSMTL 节点解释链路。Timeline scheduler MUST 在 RootTree 前求值无副作用 Decision TreeClip，使其写入 Frame Blackboard；RootTree 后的统一 WindowFactProjection MUST 将显式 ActionWindow-bound 写入转换为正式 facts；Scheduler Commit MUST 再推进存活 playback 的非决策输出。系统 MUST NOT维护 ActionWindowTrack 专用预采样、timeline decision window cache 或第二个 Window reader。

#### Scenario: RootTree 每帧运行

- **WHEN** runner 调用 pipeline update phase
- **THEN** BTSMTLPhase MUST 先准备 active playback 的 Decision TreeClip Blackboard 输出
- **AND** MUST 再 tick RootTree 完成 Transition、State exit 和 lifecycle
- **AND** MUST 再投影显式 Window candidates
- **AND** MUST 最后收口 cancel 并提交存活 playback 的非决策贡献

#### Scenario: Window 触发同 Tick状态抢占

- **WHEN** active Timeline 的 Cancel Decision TreeClip 写入对应 Bool Frame variable
- **AND** ConditionRuleGraph 选择离开 source State
- **THEN** 同一 Blackboard variable MUST 在该次 Transition 和 OnExit 求值中可见
- **AND** 显式 ActionWindow projection MUST 最多提交一次 fact
- **AND** 被取消 source playback MUST NOT提交本 Tick非决策贡献

#### Scenario: Tree abort 产生 pending stop

- **WHEN** RootTree Composite 等待 child graceful stop
- **THEN** BTSMTLPhase MUST 保持按 Logic Tick推进 RootTree stopping lifecycle
- **AND** replacement child MUST NOT在 StopCompleted 前产生 Timeline request
- **AND** 已停止 source Timeline MUST NOT继续产生 motion、cue、camera 或 animation

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

CharacterPipeline MUST 保持 external semantic input、input、BTSMTL、motion、presentation、fact collection 和 cleanup 的明确阶段。External semantic input MUST 在 input/Graph 前进入；fact collection MUST 在本 tick事实产生后运行。Model-owned binding/adapter MUST 位于 Pipeline 外围，不得把 endpoint pump 或 packet mapping塞入 Pipeline stage。

#### Scenario: 当前模型注入 correction

- **WHEN** ServerAuthoritative adapter 产生 ExternalPoseCorrection
- **THEN** external input stage MUST 在 Graph/Motion 前收集该语义输入
- **AND** endpoint MUST 不由 CharacterPipeline Pump

### Requirement: 节点和 Timeline 不直接结算最终 Transform

系统 MUST 让 BTSMTL 节点和 Timeline 只产出意图、窗口、命令或 cue。最终角色运动 MUST 由 `CharacterMotionStage` 编排，并由当前 authority mode 的正式 Motion Executor 或 Logic Pose Port 应用。Timeline MUST NOT 直接宣称命中成立、直接扣血、直接改写角色 Transform 或选择具体运动 backend。

#### Scenario: Timeline 产出移动意图

- **WHEN** Timeline 轨道或节点表达某段动作位移
- **THEN** 该输出 MUST 进入 `MotionContribution` 或正式 `MotionIntent`
- **AND** `CharacterMotionStage` MUST 决定最终 execution intent
- **AND** 正式 Motion Executor MUST 返回实际运动结果

#### Scenario: Timeline 产出 gameplay window

- **WHEN** Timeline 表达攻击、无敌或取消窗口
- **THEN** 该输出 MUST 进入 pipeline output 的 window samples
- **AND** 命中、伤害和目标归属 MUST 留给后续 gameplay solver 或服务端裁决

### Requirement: CharacterPipeline 支持混合架构 authority mode

系统 MUST 使用独立 CharacterInputSource 与 CharacterMotionAuthority 表达行为。所有合法组合 MUST 继续使用同一 CharacterPipeline 主线；Network Model MUST 只在 actor binding 时选择组合，不得在 Pipeline 内按 model id 分支。LocalSolver MUST 使用显式 Logic Pose Port 与 Motion Executor；ExternalPose MUST 只使用 Logic Pose Port；None MUST 不执行 gameplay motion。系统 MUST NOT 使用 `LocalPredicted`、`RemoteProxy`、concrete `CharacterController` 或 backend enum 作为总控模式。

#### Scenario: 当前本地 Owner

- **WHEN** input source 是 LocalDevice 且 motion authority 是 LocalSolver
- **THEN** Pipeline MUST 采样本地输入并通过正式 Motion Executor 结算本地运动
- **AND** 是否网络预测 MUST 不由 Pipeline enum 决定

#### Scenario: 后续外部位姿角色

- **WHEN** input source 是 ExternalFacts 且 motion authority 是 ExternalPose
- **THEN** Pipeline MUST 使用外部输入驱动 gameplay/animation
- **AND** MUST 只通过 Logic Pose Port 应用外部位姿
- **AND** MUST 不调用 LocalSolver executor 修改逻辑位姿

#### Scenario: 纯展示角色

- **WHEN** input source 和 motion authority 都是 None
- **THEN** Pipeline MUST 不采样控制输入或结算 gameplay motion
- **AND** Presentation MAY 继续消费显式表现数据

#### Scenario: authority mode 依赖缺失

- **WHEN** Host 缺少当前 authority mode 要求的正式端口
- **THEN** Pipeline 创建 MUST 明确失败
- **AND** MUST 不自动搜索组件或回退到另一 authority mode

### Requirement: NetworkStage 是正式边界但不实现真实 transport

CharacterPipeline 中的 network/fact stages MUST 只暴露 Character gameplay facts和接收 Character 语义外部输入。它们 MUST 不认识 ServerAuthoritative packet、Rollback bundle、LocalLoopback、Fantasy Session、endpoint、transport 或 model policy resolver。Model-owned adapter MUST 在 Pipeline 外完成 policy 和 packet 映射。

#### Scenario: NetworkSendStage 收集输出

- **WHEN** Pipeline 本 tick 产生 resolved motion 和 Action facts
- **THEN** stage MUST 保留稳定 fact identity
- **AND** MUST 不构造 MotionCommand 或 ActionActivation packet

#### Scenario: NetworkReceiveStage 接收输入

- **WHEN** model adapter 注入 `ActionLifecycleTransition`
- **THEN** stage MUST 缓存并交给正式 action stage
- **AND** MUST 不保存原始 model packet

### Requirement: Timeline 和动画 tick 权威归属 pipeline

GameplayTickSystem MUST通过 CharacterPipeline 成为 Timeline logic time 与动画表现更新入口。TimelinePlaybackScheduler MUST在 logic tick 推进 Timeline request；PresentationFrame MUST使用 InterpolationAlpha 对 selected 与 retained-outgoing playback 重新采样。AnimancerPlaybackAdapter MUST只消费正式 sample 与 lifecycle decision，MUST不自主推进同一个 Timeline。

#### Scenario: TimelineNode 提交 Timeline 请求

- **WHEN** TimelineNode 在 CharacterBTSMTLPhase 内执行
- **THEN** 它 MUST提交 Timeline playback request
- **AND** Scheduler MUST使用 logic tick context 推进
- **AND** 表现 adapter MUST不再次推进 Timeline logic time

#### Scenario: 表现帧重新采样 Timeline 动画

- **WHEN** 当前 render frame 没有新的 logic tick
- **THEN** pipeline MUST仍为 selected/outgoing playback 生成 visual animation sample
- **AND** Animancer state time MUST使用本帧 sample
- **AND** 系统 MUST不复用上一 logic tick 的离散 clip time

#### Scenario: 禁止旧播放器权威

- **WHEN** 项目启用 CharacterPipeline
- **THEN** Timeline 播放 MUST由 pipeline 显式推进
- **AND** 系统 MUST不保留旧播放器 autonomous tick

### Requirement: 不恢复 BBB 和旧 SO 数据源
系统 MUST NOT 将 BBB 的代码状态机或旧动作 SO/config 作为 `CharacterPipeline` 的数据主源。BBB 只能作为运行时组织参考。

#### Scenario: 参考 BBB
- **WHEN** 实现 `CharacterPipeline`
- **THEN** 系统 MAY 借鉴 BBB 的单入口、输入清洗、分阶段和帧末清理思想
- **AND** 系统 MUST NOT 复制 BBB `PlayerBaseState`、`PlayerStateRegistry`、`PlayerSO` 动作配置或 locomotion 特化状态类作为主链路

#### Scenario: 旧动作配置存在
- **WHEN** 项目中存在旧 locomotion、action、footphase、bodyclaim 或 animation presentation 配置
- **THEN** `CharacterPipeline` MUST NOT 从这些配置读取动作语义
- **AND** 动作语义 MUST 来自 BTSMTL Graph、NodeModule、Timeline 轨道或后续正式 runtime output

### Requirement: 角色管线路径使用 Character 命名
系统 MUST 将新角色 pipeline 代码放在正式 `Character` 命名路径中。系统 MUST NOT 继续扩展旧拼写 `Charactor` 路径。

#### Scenario: 新增 pipeline 文件
- **WHEN** 实现本能力
- **THEN** 新文件 MUST 位于 `Assets/GameScripts/Main/Runtime/Character/Pipeline`
- **AND** 新命名空间和类型名 MUST 使用 `Character` 或 `CharacterPipeline` 语义

#### Scenario: 旧空路径清理
- **WHEN** 旧 `Assets/Scripts` 或旧 `Charactor/Pipeline` 没有有效代码
- **THEN** 实现阶段 MUST 删除该旧路径
- **AND** 系统 MUST NOT 在该路径下新增新 runtime 文件

### Requirement: CharacterPipeline 是 GameplayTickSystem 的 tick target

系统 MUST 使用 `GameplayTickSystem` 作为 gameplay 统一 tick 源。`CharacterPipeline` MUST 作为 `IGameplayTickTarget` 注册到 `GameplayTickSystem`，由它统一调度本地逻辑 tick 和表现帧。`CharacterPipeline` MUST NOT 自己拥有 Unity `Update`、`LateUpdate`、`FixedUpdate` 或其它自主 tick 来源。

#### Scenario: 多个角色被统一调度

- **WHEN** 场景中存在多个启用的 `CharacterPipelineHost`
- **THEN** 每个 Host 创建的 `CharacterPipeline` MUST 注册到同一个 `GameplayTickSystem`
- **AND** tick system MUST 在同一 `LocalLogicTick` 中按注册列表调度它们
- **AND** 单个 `CharacterPipeline` MUST NOT 自己从 Unity 生命周期拉取 tick

#### Scenario: 角色被禁用

- **WHEN** 某个 `CharacterPipelineHost` 被禁用
- **THEN** 该 Host 的 pipeline MUST 从 `GameplayTickSystem` 反注册
- **AND** 后续 tick system MUST NOT 再调度该 pipeline

### Requirement: Pipeline 输出分为 strict、presentation 和 sync facts

CharacterPipelineOutput MUST 继续区分 StrictGameplayOutput、PresentationOutput 和 SyncFacts。SyncFacts MUST 表达已发生、可被 recording、debug 或 Network Model 消费的事实，MUST 不等同于 packet、model command、history 或 transport API。Resolved motion 与 correction application result MUST 作为事实提供，具体模型 adapter MAY 据此构造自己的协议输出。

#### Scenario: 单机 Pipeline

- **WHEN** 没有 Network Model endpoint
- **THEN** Pipeline MUST 继续产出必要 gameplay facts
- **AND** 不得因为无人发送而构造空 packet 或 fallback model

### Requirement: CharacterPipelineDefinition 持有角色输入合同

CharacterPipelineDefinition MUST持有正式 CharacterInputProfile。CharacterPipelineHost MUST不单独持有 input profile。运行时创建 CharacterPipeline 时，Host MUST从 Definition 读取 input profile、RootTree、Animation Presentation Definition 与 ActionProfiles。

#### Scenario: Host 创建 pipeline

- **WHEN** CharacterPipelineHost 创建角色 pipeline
- **THEN** Host MUST使用 Definition.InputProfile 创建输入阶段
- **AND** MUST使用同一 Definition 的 Animation Presentation 配置装配动画
- **AND** Host MUST不保存第二份输入或动画层配置

#### Scenario: Definition 配置缺失 input profile

- **WHEN** CharacterPipelineDefinition 没有配置 CharacterInputProfile
- **THEN** definition validator MUST报告错误
- **AND** 系统 MUST不从 Host、场景对象或默认资源寻找 fallback

#### Scenario: 输入 profile 配置错误

- **WHEN** CharacterInputProfile 存在缺失 action、重复 input id 或 request id
- **THEN** validator MUST暴露错误
- **AND** Graph authoring MUST继续以该 profile 为唯一输入合同

### Requirement: CharacterPipelineDefinition 提供 RootTree authoring context
系统 MUST 允许 editor 从 `CharacterPipelineDefinition` 打开 RootTree，并将 definition 和 input profile 作为 editor-only authoring context 传给 TreeWindow。该 context 只服务 authoring UI，不改变 runtime Graph 执行语义。

#### Scenario: 从 Definition 打开 RootTree
- **WHEN** 用户从 `CharacterPipelineDefinition` editor 打开 RootTree
- **THEN** TreeWindow MUST 获得当前 definition 和 `InputProfile`
- **AND** Input authoring 素材区 MUST 使用该 context 展示输入定义

#### Scenario: 多个 Definition 复用 RootTree
- **WHEN** 多个 `CharacterPipelineDefinition` 引用同一个 RootTree
- **THEN** Input authoring 素材区 MUST 使用打开入口传入的 definition
- **AND** 系统 MUST NOT 通过 AssetDatabase 反查猜测唯一 definition

### Requirement: CharacterGraphContext 必须通过 Pipeline Blackboard 暴露黑板

系统 MUST 让 `CharacterGraphContext` 通过 Pipeline Blackboard runtime instance 提供 blackboard 读写入口。`CharacterGraphContext` MAY 保留兼容命名的 `TryGetBlackboardValue` 和 `SetBlackboardValue` 方法作为内部 API，但这些方法 MUST 委托到正式 Pipeline Blackboard runtime，并执行 declaration、类型、作用域和生命周期校验。

#### Scenario: 节点读取黑板值

- **WHEN** BTSMTL 节点通过 `CharacterGraphContext` 读取 blackboard 值
- **THEN** context MUST 从 Pipeline Blackboard runtime 读取
- **AND** 读取结果 MUST 受变量 declaration 的类型和 scope 约束
- **AND** context MUST NOT 直接访问未声明的散 dictionary key

#### Scenario: 动作结束清理变量

- **WHEN** ActionInstance 进入 Complete、Cancel、Interrupt 或 Abort 终态
- **THEN** Pipeline Blackboard runtime MUST 清理该 ActionInstance scope 的变量
- **AND** 其它 action 或后续状态 MUST NOT 读取到已结束动作的临时值

### Requirement: Pipeline 输出事实必须继续通过 SyncFacts 边界产生

系统 MUST 保持 `CharacterPipelineOutput.SyncFacts` 作为 pipeline 输出事实边界。Blackboard variable MAY 为 Graph 提供运行时上下文；只有显式合法 fact projection 才能将当前写入转换为 Action、GameplayResult、GameplayEffect 或 Presentation SyncDomain output。NetworkSendStage MUST 只读取投影后的 SyncFacts，不得直接读取 Blackboard values。

#### Scenario: 投影 Action window

- **WHEN** WindowFactProjection 收到合法 ActionWindow-bound variable candidate
- **THEN** runtime MUST 生成 ActionWindowSample
- **AND** MUST 将其写入 `SyncFacts.Action.WindowSamples`
- **AND** NetworkSendStage MUST 继续从 SyncFacts 收集该事实

#### Scenario: 写入 local-only 临时值

- **WHEN** 节点写入 Projection=None 的本地 Blackboard variable
- **THEN** 该值 MUST NOT自动进入 SyncFacts
- **AND** NetworkSendStage MUST NOT因该变量存在生成 outgoing packet

#### Scenario: 缺失 projection provenance

- **WHEN** ActionWindow-bound 写入缺少显式 Action Context
- **THEN** runtime MUST 拒绝生成 ActionWindowSample 并报告原因
- **AND** MUST NOT将该写入降级为无 ActionInstance 的默认 window fact

### Requirement: Pipeline Blackboard 生命周期必须进入 frame cleanup

系统 MUST 在角色 pipeline 生命周期中清理 Pipeline Blackboard 的 transient 值。Frame、State、ActionInstance 和 Character scope 的变量 MUST 在对应生命周期结束时清理或重置。系统 MUST NOT 依赖节点作者手动用 null 写回清理所有临时 key。

#### Scenario: Frame scope 变量

- **WHEN** 某个变量声明为 Frame scope
- **THEN** frame end cleanup MUST 清理该变量
- **AND** 下一帧读取该变量 MUST 得到未设置状态或默认值

#### Scenario: Character scope 变量

- **WHEN** pipeline Dispose
- **THEN** Pipeline Blackboard runtime MUST 清理 Character scope 的值
- **AND** 已销毁角色 MUST NOT 继续持有 scene object、action handle 或 graph context 引用

### Requirement: 角色管线必须保留跨 logic tick 的动画生命周期命令

系统 MUST使用 presentation-owned 持久队列保存尚未消费的 AnimationLayerSelection、AnimationProducerSample、Complete、Release 与 terminal metadata。命令 MUST独立于单个 CharacterPipelineFrame.Output，并按 local logic tick、sequence 与 playback generation 保序。队列 MUST不保存 Tree topology、Driver、ready、causal component、LayerPlan 或 arbitration ledger。

#### Scenario: 单 render frame 多个 logic tick

- **WHEN** 一个 PresentationFrame 前发生多个 logic selection 与 Timeline lifecycle command
- **THEN** 队列 MUST保留全部 generation 的 Complete/Release 顺序
- **AND** 每层只把最终 selection 交给 lifecycle

#### Scenario: transient output 清理

- **WHEN** Pipeline 清理 transient gameplay/presentation output
- **THEN** 未被 PresentationFrame acknowledge 的 animation commands MUST保留

#### Scenario: lifecycle commit 前不得确认

- **WHEN** Stage 已复制 command batch
- **AND** Timeline sample、lifecycle 或 Animancer adapter 尚未完成
- **THEN** queue MUST不提前 acknowledge

#### Scenario: Pipeline 释放

- **WHEN** pipeline deactivate 或 dispose
- **THEN** pending commands、playback lifecycle、Animancer states 与 retention MUST清理
- **AND** MUST不等待 fade

### Requirement: PresentationFrame 必须输出逐层最终动画结果

CharacterPipelineFrame 的动画调试输出 MUST保存每层 AnimationPlaybackLifecycleSnapshot，至少表达 selected playback、PendingFirstSample、Current、Outgoing、Retired、sample time 与 Animancer fade 状态。该 snapshot 只用于 diagnostics；最终 pose 由 Animancer 直接应用，frame MUST不再保存 LayerPlan、DesiredCandidate、Driver 或项目自算 state weights。

#### Scenario: Base Current

- **WHEN** Base 拥有合法 Current
- **THEN** frame snapshot MUST引用该 playback generation 与 Animancer state key
- **AND** Presenter MUST不消费另一份 LayerPlan

#### Scenario: Base PendingFirstSample

- **WHEN** selected target 尚未产生 sample
- **THEN** snapshot MUST同时显示 Current 与 PendingFirstSample
- **AND** MUST不以空 plan 隐藏等待状态

#### Scenario: Base Invalid

- **WHEN** RequireOutput Base 没有合法 selection/current
- **THEN** snapshot MUST显示明确错误 provenance
- **AND** Animancer adapter MUST不选择默认 clip

### Requirement: CharacterPipeline 必须作为显式 diagnostics target

每个 active `CharacterPipeline` MUST 能通过 `CharacterPipelineHost` 注册为独立 diagnostics target，并提供 session identity、source/program revision、默认关闭的 diagnostics store 和只读 target metadata。store MUST 按 target-level Live interest 维护 current state，且只在显式 Capture 期间保存有界历史。Host 或 Pipeline MUST NOT 向 editor 暴露 runtime Graph/Node/Timeline 对象作为正式调试 API。

#### Scenario: Host 激活 Pipeline

- **WHEN** `CharacterPipelineHost` 激活一个有效 Pipeline
- **THEN** diagnostics target registry MUST 注册该 runtime target
- **AND** target MUST 提供稳定 session identity 和 definition/source revision

#### Scenario: Host 禁用或销毁

- **WHEN** Host deactivate 或 dispose Pipeline
- **THEN** diagnostics target MUST 注销
- **AND** attached Debug Session MUST 收到正式 detach lifecycle
- **AND** editor MUST 不继续持有 Pipeline runtime 对象

### Requirement: Pipeline domain debug 必须进入统一 Trace

Action、Blackboard、Motion、Timeline、Animation selection、producer sample、playback lifecycle、Animancer fade、Presentation 与 Camera runtime debug MUST投影到统一 Trace/view model。CharacterPipelineHostEditor MUST消费该 view model，不得遍历 runtime service 私有集合形成平行调试链。Trace MUST不包含已删除的 Driver、ExecutionLineage、causal component、Arbitrator 或 LayerPlan。

#### Scenario: 查看 Pipeline Inspector

- **WHEN** 用户选择附着 active Debug Session 的 Host
- **THEN** Inspector MUST显示当前 Action、Blackboard、Motion、selection、playback lifecycle 与 Camera snapshot
- **AND** Graph/Timeline/Presentation 窗口 MUST引用同一 event identity

#### Scenario: 持续运行

- **WHEN** Play Mode 中 runtime target 持续产生 Trace
- **THEN** Inspector MUST按统一 editor update schedule 刷新
- **AND** MUST不依赖鼠标事件触发更新

### Requirement: CharacterPipeline 必须提交逻辑侧唯一动画选择

CharacterPipeline MUST在每次 logic tick 完成 State、Action、interruption 与 Timeline request 处理后，汇总每层 AnimationLayerSelection。每个 LayerId 最多一个 selected AnimationPlaybackId。该汇总属于 Logic/BTSMTL phase 的业务结果，MUST在 PresentationFrame 前完成；Presentation 与 Animation 模块 MUST不修改选择结果。

#### Scenario: 同层存在两个业务 owner

- **WHEN** Action 与 Locomotion 逻辑同时声称 Base 所有权
- **THEN** CharacterPipeline MUST在逻辑边界报告冲突并拒绝该层 selection
- **AND** MUST不把两个候选交给 Animation 模块

#### Scenario: 最终选择已确定

- **WHEN** ActionOverride 已决定 Action 获得 Base 所有权
- **THEN** pipeline MUST提交 Action playback identity
- **AND** PresentationFrame MUST按该 identity 请求 sample

### Requirement: PresentationFrame 必须原子提交动画播放生命周期

PresentationFrame MUST按固定顺序读取未消费 selection/complete/release、确定每层最终 selection、采样 selected 与 retained-outgoing AnimationTrack、更新 AnimationPlaybackLifecycle、调用 AnimancerPlaybackAdapter、用 presentation delta 推进 Animancer、退休完成的 outgoing，最后 acknowledge 批次。该阶段 MUST不重新 tick RootTree、Timeline gameplay、TreeClip、Motion、ActionWindow 或 SyncFacts。

#### Scenario: target sample 与 selection 同批

- **WHEN** target selection 与第一份合法 sample 在同一表现批次
- **THEN** lifecycle MUST原子地将 target 设为 Current
- **AND** source MUST进入 Outgoing
- **AND** 中间 Empty MUST不可见

#### Scenario: target sample 延迟

- **WHEN** 最终 selection 已是 B 但 B 尚无第一份合法 sample
- **THEN** lifecycle MUST记录 PendingFirstSample B 并保持 Current A
- **AND** MUST不超时选择 fallback

#### Scenario: source 已逻辑释放

- **WHEN** source gameplay 已停止但其 Animancer state 仍在淡出
- **THEN** PresentationFrame MUST只请求 outgoing animation presentation sample
- **AND** MUST不重新执行 source gameplay

#### Scenario: 表现帧不产生 gameplay facts

- **WHEN** PresentationFrame 更新 sample、lifecycle 与 Animancer
- **THEN** StrictGameplayOutput 与 SyncFacts MUST不新增事件

### Requirement: CharacterPipeline 必须编排唯一 Gameplay Effect 阶段

`CharacterPipeline` MUST 只持有 `CharacterGameplayEffectAdapter`，由 Adapter 唯一持有通用 `GameplayEffectRuntime`。Pipeline MUST 在每个固定逻辑 tick 将 Adapter 编排进 NetworkReceive 之后、Input/BTSMTL 之前的 Begin 阶段，以及 Motion 之后、NetworkSend 之前的 CommitFacts 阶段。Pipeline MUST NOT 访问 GE Container 或实现 GE 规则；Presentation frame MUST 只消费已提交 cue，不得推进 effect runtime。

#### Scenario: Pipeline 执行逻辑 tick

- **WHEN** 当前 tick 已完成 incoming network/result 注入
- **THEN** pipeline MUST 调用 Adapter 将 semantic input 映射并推进 GameplayEffectRuntime 的 incoming effect、period、expiry 和 inhibition
- **AND** MUST 再让 Input 与 BTSMTL 使用协调后的统一状态

#### Scenario: Pipeline 执行表现帧

- **WHEN** PresentationStage 消费本 tick 的 gameplay cues
- **THEN** 它 MUST 不改变 tag count、attribute value、active effect 或 prediction journal
- **AND** 下一逻辑 tick 的 Gameplay Effect 结果 MUST 不依赖 render frame 数量

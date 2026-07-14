# character-pipeline-runtime Specification

## ADDED Requirements

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

## MODIFIED Requirements

### Requirement: Graph 执行上下文来自 CharacterGraphContext

系统 MUST使用 CharacterGraphContext 作为 BTSMTL RootTree 的 BaseGraph.User。该 context MUST直接提供 Timeline 播放请求服务、InputAction value source、authority mode、network tick context、gameplay blackboard、tick 起点 actor pose snapshot、correction 输入、角色逻辑 AnimationLayerSelection 提交接口与 diagnostics。Context MUST不保存 AnimationOwnerScopeId、Tree animation activation、Animation ExecutionLineage、Driver binding、Animation topology records 或可见动画后代状态，也 MUST不依赖场景搜索或 fallback 补齐缺失引用。Actor pose snapshot MUST在 BTSMTL 决策前由 pipeline 从显式注入的 actor Transform 捕获。

#### Scenario: TimelineNode 获取 Timeline 播放请求入口

- **WHEN** TimelineNode 在角色 pipeline 中被 tick
- **THEN** TimelineNode MUST通过 BaseGraph.User 获取 ITimelinePlaybackService
- **AND** service MUST由 CharacterGraphContext 暴露

#### Scenario: InputAction ValueNode 读取输入

- **WHEN** InputAction ValueNode 被请求输出值
- **THEN** 节点 MUST通过 BaseGraph.User 获取 IInputActionValueSource
- **AND** value source MUST使用当前帧输入读取 Button、Float 或 Vector2

#### Scenario: 捕获 tick 起点角色姿态

- **WHEN** CharacterPipeline 开始新的 logic tick
- **THEN** pipeline MUST在 BTSMTL 执行前捕获 actor 平面位置与朝向
- **AND** 同 tick 内所有 ConditionRuleGraph MUST读取同一个只读 snapshot

#### Scenario: 缺失上下文引用

- **WHEN** graph context 缺少 Timeline 服务、输入资产或 actor pose snapshot
- **THEN** 对应节点 MUST按 BTSMTL 规则报告缺失来源
- **AND** context MUST不通过全局搜索补齐

#### Scenario: Graph 读取网络上下文

- **WHEN** Graph 逻辑需要读取网络 tick、authority mode、confirmed event 或 correction
- **THEN** 它 MUST通过 CharacterGraphContext 正式接口读取
- **AND** MUST不直接读取 transport 或服务端对象

#### Scenario: nested producer 执行

- **WHEN** RootTree 通过 SubTree、StateMachine 和 TimelineNode 进入 nested producer
- **THEN** 每层逻辑 MUST继承同一个 CharacterGraphContext 与 Timeline playback identity
- **AND** Animation 模块 MUST不需要 Runnable parent lineage

#### Scenario: 非 Animation Graph 逻辑

- **WHEN** Runnable 只执行普通逻辑
- **THEN** Context MUST不要求其发布动画 activation 或 owner fact
- **AND** Runnable MUST不依赖 Character Animation 类型

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

### Requirement: Pipeline 分阶段处理输入、图、motion、表现和网络边界

系统 MUST将角色每帧处理拆成明确 phase。第一阶段 MUST至少包含 network receive、input、BTSMTL、motion resolve、presentation resolve、sync fact collection/network send boundary 和 frame end cleanup。Phase MUST通过 frame/context/output 交换数据，MUST不互相直接控制对方的内部状态。Network receive MUST发生在 input 和 Graph 前；network send boundary MUST发生在本 tick 可同步事实产生后，并由 adapter 交给 GameplaySyncRuntime 或后续 backend。

#### Scenario: Update phase

- **WHEN** pipeline update phase 执行
- **THEN** NetworkReceiveStage MUST先读取已注入的 snapshot、action decision、gameplay result 或 correction 缓存
- **AND** InputStage MUST更新当前帧输入快照
- **AND** CharacterBTSMTLPhase MUST使用当前 frame/context tick BTSMTL RootTree 和 active Timeline playback
- **AND** CharacterBTSMTLPhase 输出的可同步事实 MUST写入 `CharacterPipelineOutput.SyncFacts`

#### Scenario: Late phase

- **WHEN** pipeline late phase 执行
- **THEN** MotionStage MUST消费 `MotionIntent`、`MotionContribution` 和 motion modifier 数据并产生 `MotionResult`
- **AND** BTSMTLPhase MUST在 presentation frame 为已选或 retained outgoing Timeline producer 采样 AnimationTrack 并输出 `AnimationProducerSample`
- **AND** PresentationStage MUST原子消费 AnimationLayerSelection、AnimationProducerSample、Complete 或 Release
- **AND** AnimationPlaybackLifecycle MUST NOT消费 PresentationSync cue
- **AND** NetworkSendStage MUST从 `SyncFacts` 收集 client command、action activation、lifecycle transition、motion snapshot、gameplay result 或 window digest
- **AND** frame transient 数据 MUST在帧末被清理

## REMOVED Requirements

### Requirement: PresentationFrame 必须完成统一动画 lifecycle handoff

**Reason**: 旧 requirement 规定 Adapter -> Registry -> Arbitrator -> LayerPlan -> custom LayerRuntime 链。它由原子 selection/sample/lifecycle/Animancer 提交替代。

#### Scenario: 删除旧 handoff commit

- **WHEN** PresentationFrame 更新动画
- **THEN** MUST不解析 Driver、causal component 或 LayerPlan

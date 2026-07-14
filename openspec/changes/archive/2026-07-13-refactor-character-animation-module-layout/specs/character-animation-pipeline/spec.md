## ADDED Requirements

### Requirement: 动画生命周期通道必须分离事实写入与批次消费权限

系统 MUST 使用 `IAnimationLifecycleFactSink` 作为 Graph、StateMachine、Timeline 与其它 animation producer 的唯一 lifecycle 写入合同，并使用 `IAnimationLifecycleBatchSource` 作为 CharacterPresentationStage 的唯一 ordered batch 消费合同。`CharacterAnimationLifecycleCommandQueue` MUST 是两个合同的唯一具体实现并由 `CharacterPipeline` 唯一构造。系统 MUST NOT为 producer、Presentation 或 Preview 创建并行 Queue、event bus 或 command mirror。

#### Scenario: StateMachine 提交 handoff

- **WHEN** StateMachine transition 发布 None/Driver handoff 或 AnimationOwnerReady
- **THEN** CharacterGraphContext MUST 只通过 IAnimationLifecycleFactSink提交事实
- **AND** Graph runtime MUST NOT读取 pending batch、acknowledge sequence或清理 Queue

#### Scenario: Timeline 提交 contribution lifecycle

- **WHEN** TimelinePlaybackScheduler 在 logic/presentation sampling边界提交 Sample、Complete或Release
- **THEN** Scheduler MUST 只通过同一个 IAnimationLifecycleFactSink写入
- **AND** Scheduler MUST NOT维护第二份待提交动画命令列表作为播放权威

#### Scenario: Presentation 消费批次

- **WHEN** CharacterPresentationStage 开始一次 animation commit
- **THEN** Stage MUST 只通过 IAnimationLifecycleBatchSource复制完整有序 batch
- **AND** Stage MUST 在 Registry、Arbitrator、LayerRuntime 与 Presenter成功提交后 acknowledge该 batch
- **AND** Stage MUST NOT通过 FactSink伪造 producer 事实

#### Scenario: Pipeline 重置生命周期通道

- **WHEN** CharacterPipeline deactivate 或 dispose
- **THEN** composition root MUST 清理同一个具体 Queue
- **AND** producer 与 Stage MUST NOT各自保留未清理的镜像 command state

### Requirement: Animation 与 Presentation 模块必须保持单向依赖

角色管线 MUST 将 animation identity、contribution、lifecycle、candidate、plan 与 playback output 定义在正式 Animation 合同边界；Lifecycle、Arbitration、Playback 与 Diagnostics 实现 MUST 归属于 Animation 模块。Presentation MUST 只负责表现帧聚合、logic pose 插值、presentation cue 与具体 Animancer adapter。Animation 模块 MUST NOT依赖具体 Presentation/Animancer 实现，Logic producer MUST NOT为提交动画事实依赖具体 Presentation 类型。

#### Scenario: Logic 发布动画事实

- **WHEN** CharacterGraphContext 或 TimelinePlaybackScheduler 需要构造 animation contribution、handoff或 owner lifecycle fact
- **THEN** 它们 MUST 只引用 Animation contracts与 FactSink
- **AND** 它们 MUST NOT引用 AnimancerAnimationPresenter、CharacterPresentationStage或 AnimationLayerPlaybackState

#### Scenario: Stage 聚合动画事务

- **WHEN** CharacterPresentationStage 执行表现帧
- **THEN** Stage MAY 依赖 Animation Lifecycle、Arbitration、Playback 与具体 Animancer adapter
- **AND** Stage MUST 保持 Queue -> Registry -> Arbitrator -> LayerRuntime -> Presenter -> acknowledge的唯一顺序
- **AND** 任意下游模块 MUST NOT反向调用 Stage生成动画事实

#### Scenario: Animancer 应用最终输出

- **WHEN** Presenter 收到 AnimationLayerPlaybackOutput集合
- **THEN** Presenter MUST 只应用最终 layer/state/time/weight/mask/additive结果
- **AND** Presenter MUST NOT引用 Ledger、CausalGraphBuilder、HandoffResolver或 producer command channel

#### Scenario: Timeline Preview

- **WHEN** Timeline Editor 创建私有 Preview session
- **THEN** Preview MUST 组合与正式角色相同的 Queue、Registry、Arbitrator、LayerRuntime与 Presenter
- **AND** Preview MUST NOT因为目录拆分创建专用 Resolver、专用 Registry或直接 Animancer播放路径

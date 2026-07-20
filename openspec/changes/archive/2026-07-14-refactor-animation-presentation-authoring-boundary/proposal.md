# Change: 重构动画表现边界为逻辑选择与播放生命周期

## Why

当前实现把动画表现问题错误地建模成 Tree 因果仲裁问题：

- BTSMTL 为动画增加 Runnable activation、control-flow topology、Driver 和 ExecutionLineage；
- Animation 模块接收多个候选后，再按 Priority、authority 和因果组件选择可见 owner；
- 自制 LayerPlan、ActiveHandoff 和 LayerRuntime 又重复实现 Animancer 已经提供的状态播放、layer 混合和 fade；
- 合法的 Tree 行为因此被动画合同反向约束。当前空 StateBehaviorSubTree 返回 State.None 时，通用 Tree lifecycle 会把它当成无效动画执行结果并抛异常；
- Priority 同时出现在 Timeline contribution、动画仲裁和逻辑状态选择中，动作覆盖 locomotion 的业务决定被下沉到了表现层；
- Graph、StateMachine、Presentation Definition 和 Agent schema 都保存了动画交接语义，形成多份真相。

这不是局部判空错误，而是职责边界错误。逻辑层必须先完成状态、动作、打断和优先级决策，并为每个语义动画层提交唯一的期望播放实例。Timeline 只负责该播放实例的逻辑时间与表现采样。Animation 模块只维护播放实例从等待首样本、当前可见、淡出到退休的生命周期。真正的状态混合、layer weight 和 fade 由 Animancer 执行。

## What Changes

- 新增逻辑侧 AnimationLayerSelection 合同。每个 LayerId 在一次逻辑提交中最多选择一个 AnimationPlaybackId；选择结果不携带动画侧 Priority。
- 状态机 Transition priority、Selector/Parallel interruption、ActionOverride 和其它业务竞争继续留在逻辑层。Animation 模块不得重新比较优先级或从多个 producer 中选赢家。
- TimelinePlaybackScheduler 继续作为 Timeline 时间权威。它为当前选中 producer 和仍处于表现淡出的 outgoing producer生成 AnimationProducerSample。
- AnimationTrack 保留 LayerId、clip 时间、ease 和同一 producer 内部的 Weight；删除供跨 producer 仲裁使用的 Priority。
- 将动画运行时收敛为每层播放生命周期：PendingFirstSample、Current、Outgoing、Retired。它只接收已解析 selection、sample、complete 和 release。
- 首样本尚未到达时保持当前合法播放，直到同一目标的正式 sample 到达后原子切换；不得替换成默认 Idle、当前 clip 副本或隐藏 fallback。
- Animancer 成为实际播放与混合权威。Adapter 创建或更新 AnimancerState/ManualMixerState，写入 Timeline 采样时间和 producer 内部权重，并调用 Animancer Play/Fade。
- 跨 producer fade 的权重、重入和退休由 Animancer 状态图决定；项目不再计算 LayerPlan、StateWeight、ActiveHandoff 或自定义 crossfade 权重。
- 使用项目已安装的 Animancer TransitionLibrary、ITransition、FadeMode 和 FadeGroup easing 能力表达转场。不得再建立 Pipeline 自有的 Layer + SourceProducer + TargetProducer 转场表。
- CharacterPipelineDefinition Inspector 作为唯一 Presentation 配置入口，编辑 Layer catalog、producer 到 Animancer transition key 的绑定并定位 Animancer 原生转场数据。Graph、StateMachine 和 Timeline 不保存这些配置的副本，也不再提供独立 Animation Presentation 窗口。
- 删除 TreeExecutionLifecycle 的动画用途、CharacterAnimationPresentationAdapter、CharacterAnimationExecutionLineage、AnimationTopologyRecord、Driver binding、CausalGraph、Arbitrator、LayerPlan、ActiveHandoff、旧 Registry 仲裁语义和 Graph presentation leaf。
- 保留 BTSMTL 自身所需的 State scope、状态运行事实、stop barrier 和 diagnostics；这些逻辑合同不得再依赖 Character Animation 类型。
- Corin 迁移为逻辑层显式提交 Base layer selection。Action/Dodge 通过现有动作所有权覆盖 locomotion，动作结束后由逻辑状态决定选择 RunLoop、RunEnd 或其它 locomotion producer。
- Timeline Scheduler 只收集活跃 producer；逻辑选择器以当前唯一 ActionInstance 所有权解析 Action 与 Locomotion，禁止用启动时 `ActionContext.IsValid` 猜测赢家。
- MotionContribution 区分“本 tick 有位移 delta”和“本 tick 占用并消费低层 channel”。MotionCurveClip 分开保存曲线结束帧与 clip 占权结束帧，使攻击 recovery 可以保持零位移但不恢复 locomotion。
- Decision TreeClip 按本次 logic tick 穿过的时间区间求值；一次 tick 完整跨过短窗口或 loop 边界时不得漏执行。
- Graph 初始化收敛为非虚统一入口与派生初始化钩子；TimelineRunningTree 必须先提供正式 TreeClip runtime context，不能依赖重载内的虚调用顺序。
- CharacterAnimationLayerDefinition 删除没有运行语义的 apply flag；正式 Layer catalog 中的每层都由 Animancer adapter 应用。
- Agent schema 删除 Presentation Driver、Tree lifecycle animation site、animation priority 和 `configure_animation_layer` 写操作；Presentation Layer 与 producer binding 仅保留只读 Snapshot 投影。
- 更新 current specs 和 openspec/project.md，删除 Registry -> Arbitrator -> LayerPlan 的旧架构口径。

## Responsibility Boundary

| 模块 | 负责 | 不负责 |
| --- | --- | --- |
| BTSMTL / StateMachine / Action | 状态、条件、打断、priority、当前 ActionInstance 所有权、每层唯一选择 | clip 混合、fade、动画 owner 推断 |
| TimelinePlaybackScheduler | playback 时间、loop、clip 区间、表现采样、outgoing 表现保留采样、producer 收集 | 跨 producer 选赢家、业务打断 |
| Animation Playback Lifecycle | selection/sample 对齐、Current/Outgoing/Retired、Animancer state 资源寿命 | priority、Tree topology、业务状态 |
| Animancer | state/mixer、layer weight、fade、重入、最终 Animator 输出 | gameplay 选择、Timeline 逻辑事实 |
| Pipeline Definition Inspector | Layer catalog、producer binding、TransitionLibrary 引用与正式资产定位 | 修改 Graph 条件、State priority、Timeline clip 数据、复制运行时调试 |

## Capabilities

### New Capabilities

- character-animation-presentation-authoring：定义 Pipeline Definition Inspector 中的唯一 Presentation 配置入口、producer identity、Animancer TransitionLibrary 绑定和单一写权限边界。

### Modified Capabilities

- btsmtl-node-interruption-lifecycle：明确 Tree stop 生命周期只服务逻辑结构，不发布动画 owner 或转场事实。
- btsmtl-graph-core：Graph 初始化使用统一非虚入口，Timeline TreeClip 的正式 runtime context 在初始化前完成校验。
- btsmtl-runnable-timeline-node：TimelineNode 请求只捕获 playback identity/generation，不再捕获动画 owner scope 或释放 Registry entry。
- btsmtl-sm-node-authoring：删除通用 Tree animation control-flow 合同，保留状态作用域、状态事实与停止屏障。
- btsmtl-timeline-editor-preview：预览复用 selection/sample/playback lifecycle/Animancer 正式链路，不再构建私有 Registry/Arbitrator。
- btsmtl-runtime-diagnostics：动画 Trace 改为 selection、sample、playback lifecycle 和 Animancer state，不再显示 Driver/Lineage/Arbitrator。
- btsmtl-tree-inspector-information-architecture：Graph Inspector 只编辑逻辑 priority、condition、ownership 和 interruption。
- character-animation-pipeline：新增逻辑唯一选择到 Timeline 表现采样再到播放生命周期的单向链路。
- character-animation-layer-runtime：动画层从候选仲裁器改为已选 producer 的播放生命周期管理器。
- character-gameplay-pipeline-closure：动画选择与采样从 gameplay facts 分离，Presentation 只消费正式播放命令。
- character-pipeline-runtime：跨 logic tick 保存 selection/sample/release，并在 PresentationFrame 原子提交给 Animancer。
- character-presentation-interpolation：Timeline 控制 pose time，Animancer 控制 fade time，两者都在表现帧连续推进。
- character-root-motion-curves：删除项目自有 Inertialization output job 合同，保持 Animancer pose fade 与 gameplay motion 单向隔离。
- character-motion-semantics：Motion contribution 区分位移 delta 与低层 channel 占用，MotionCurve 可在曲线结束后继续保持正式动作移动所有权。
- character-state-interruption-authoring：打断立即结束逻辑所有权，动画 outgoing 独立淡出，不反向阻塞 Tree。
- character-state-timeline-authoring-loop：Corin 的 ActionOverride、Dodge 和 Locomotion 输出每层唯一 selection，不再依赖 Driver 或动画 Priority。
- agent-character-controller-synthesis：删除 Agent v5 Presentation Driver 路径并破坏性升级为只读 Presentation 投影的 schema v6。

## Impact

- 运行时代码会影响 Character/Pipeline/Animation、Timeline animation sampling、CharacterPresentationStage、BTSMTL 中间实现清理和 Animancer Presenter。
- Editor 会删除旧 Driver 工作台、Graph 动画 Inspector 和独立 Animation Presentation 窗口；Layer、TransitionLibrary 与 producer binding 收敛到 CharacterPipelineDefinition Inspector，Graph 和 Timeline 保持两个正式编辑窗口。
- 资产会迁移 CharacterPipelineDefinition 的 Layer catalog、Timeline producer identity、Corin 每层逻辑选择与 MotionCurve 占权区间；旧 StateMachine 动画字段、旧 Driver binding、无效 layer apply flag 和旧自制 transition 数据属于此前中间实现，直接删除，不作为需要保真的业务配置。
- 当前 change 已经落地但方向错误的代码属于待删除中间实现，不保留兼容层、fallback、双写或临时桥接。
- 不运行 Unity batchmode，不新增测试或人工验证 task。

## Current Spec Comparison

- character-animation-layer-runtime 当前要求动画层负责候选仲裁、因果链、ActiveHandoff 和 LayerPlan；本 change 删除这些要求，改为已选 producer 的生命周期。
- character-animation-pipeline 当前要求 Tree lifecycle -> Adapter -> ExecutionLineage -> Arbitrator；本 change 改为 Logic Selection -> Timeline Sample -> Playback Lifecycle -> Animancer。
- character-pipeline-runtime 当前把 PresentationFrame 定义为 Driver/LayerPlan commit；本 change 改为 selection/sample 的原子消费和 Animancer 应用。
- character-presentation-interpolation 当前把自制 Inertialization、Driver conflict 和仲裁链作为正式表现合同；本 change 删除这些项目自有混合算法。
- btsmtl-timeline-editor-preview 当前要求私有 Registry、Arbitrator、LayerPlan 与 LayerRuntime；本 change 让预览复用正式播放生命周期与 Animancer adapter。
- character-gameplay-pipeline-closure 当前仍把 AnimationContribution、Tree Driver 与 LayerPlan 写入表现闭环；本 change 改为 selection/sample/lifecycle。
- character-root-motion-curves 当前仍要求已删除的 Inertialization output job；本 change 改为约束 Animancer pose fade 不得进入 gameplay motion。
- btsmtl-sm-node-authoring 当前要求 StateMachine transition 发布通用动画 control-flow fact；本 change 删除该反向依赖。
- character-state-timeline-authoring-loop 当前要求 Corin 使用完整 Tree Presentation Driver 和 Previous/Desired 仲裁；本 change 改为逻辑侧唯一播放选择。
- agent-character-controller-synthesis 当前 schema v5 可以编辑 Presentation Driver；本 change 破坏性删除该路径。
- openspec/project.md 当前主链仍写 Registry -> Arbitrator -> LayerPlan -> LayerRuntime；apply 收口时必须同步改正，否则 change 与架构真相冲突。

## Tradeoffs

- 选择逻辑侧唯一 selection，会要求 Corin 明确表达 Action 对 Locomotion 的所有权；代价是逻辑配置冲突会直接报错，收益是表现层不再猜业务优先级。
- 选择 Animancer 原生 TransitionLibrary，而不是自制精确转场表，会受 Animancer 原生 key、modifier 和 fade 能力约束；收益是播放与混合只有一个权威。
- 选择 Timeline 继续采样 outgoing presentation，会保留一个纯表现 retention 生命周期；代价是 scheduler 需要区分 gameplay release 与 presentation retire，收益是淡出期间动画时间不会冻结。
- 选择首样本门闩会在 target 未准备好时短暂保持 current；这是原子提交语义，不是配置 fallback。若 Base layer 在没有 current 的情况下缺首样本，系统直接报错。

## Stop Conditions

- 若已安装 Animancer 无法用正式 API 接收外部 Timeline 采样时间并同时推进 fade，停止实施并说明需要修改时间权威或播放器的 tradeoff。
- 旧自定义 Inertialization、strategy、duration 与 curve 是此前中间实现生成的数据，不进入迁移门禁；apply MUST删除这些数据，并用 Animancer 原生 authoring 创建新的正式转场配置。
- 若 Corin 现有逻辑无法在不新增第二个仲裁器的情况下产出每层唯一 selection，停止并说明需要修改逻辑 authoring 的业务代价。
- 若 Unity 序列化资产无法安全迁移 stable producer identity，停止并保留原资产文件，不先删旧字段。

## Out of Scope

- 不实现 Motion Matching、Pose Search、速度匹配、root motion steering 或移动控制权 fade。
- 不实现项目自有 Inertialization 算法；需要时通过独立 change 评估 Animancer 或其它正式播放器能力。
- 不增加第二张 Animation Graph、Animator Controller、Presentation Policy fallback 或 runtime 默认动画。
- 不让 Pipeline Definition Inspector 修改 Tree topology、Transition priority、interruption、Blackboard 或 Timeline clip payload，也不在其中复制 RuntimeDebugSession 视图。

## Context

当前错误链路是：

    Runnable / StateMachine lifecycle
      -> Animation Adapter
      -> ExecutionLineage / Driver / Topology
      -> Registry
      -> Arbitrator
      -> LayerPlan / ActiveHandoff
      -> custom LayerRuntime
      -> Presenter

它把三个不同问题混在一起：

1. 逻辑层决定谁拥有动作和动画层。
2. Timeline 决定 clip 在什么时间被采样。
3. 播放器决定如何从当前姿势淡入目标状态。

正确链路必须把这三件事拆开，但每件事只有一个权威。

## Goals

- 逻辑层在每次提交时为每个 LayerId 产生零或一个已解析 AnimationPlaybackId。
- Animation 合同中不存在 Priority、authority、Driver、Tree activation 或 causal topology。
- Timeline 逻辑时间和表现采样继续分离。
- Animation 模块只维护 producer 的可见播放生命周期。
- Animancer 负责实际 layer、state、mixer、fade weight 和重入。
- outgoing 动画在逻辑 source 结束后仍能在表现域连续采样，直到 Animancer fade 完成。
- Graph、Timeline 与 Pipeline Definition Inspector 各自只有一个可写数据边界。

## Non-Goals

- 不让 Animation 模块解释 Selector、Parallel、StateMachine 或 interruption。
- 不把逻辑 Priority 重命名后继续塞进 AnimationContribution。
- 不自制 Layer Mixer、crossfade 曲线求值器或 Transition 仲裁器。
- 不用默认 Idle、当前 clip 副本或隐式 Immediate 掩盖缺失选择。
- 不改变 Motion delta 的生成算法、root motion 曲线内容或 gameplay window 业务；只补齐零 delta 仍可占用低层 channel 的正式仲裁语义。

## Target Architecture

    Logic Tick
      BTSMTL / StateMachine / ActionOverride
        -> AnimationLayerSelection per LayerId
      TimelinePlaybackScheduler
        -> playback logic time

    Presentation Frame
      TimelinePlaybackScheduler
        -> AnimationProducerSample for selected and retained outgoing playbacks
      AnimationPlaybackLifecycle
        -> PendingFirstSample / Current / Outgoing / Retired
      AnimancerPlaybackAdapter
        -> AnimancerState or ManualMixerState
        -> TransitionLibrary.Play / AnimancerLayer.Play
      Animancer
        -> final Animator pose

## Ownership Decisions

### Logic owns selection

AnimationLayerSelection 是逻辑输出，不是动画候选：

    LayerId
    AnimationPlaybackId or None
    LogicTick
    Sequence

它不携带 Priority、Weight、Transition duration、Tree route 或 State identity。

StateMachine edge priority、Selector 选择、LowerPriority interruption 和 ActionOverride 先完成业务决策，再提交 selection。一个 layer 在同一次逻辑提交出现两个不同 playback selection 是逻辑错误，Pipeline 必须报告两个来源并拒绝提交该批次；Animation 模块不得选择其中一个。

None 只允许用于 OutputPolicy=AllowEmpty 的 layer。某次逻辑提交没有该 LayerId 的 selection 表示“本 tick 不改变已提交选择”，已有 Current 继续作为正式输出；RequireOutput layer 首次启动既没有 Current 也没有 selection 时直接报错。

Timeline request 携带的 ActionContext 只说明 producer 的业务归属，不等于当前所有权。逻辑选择器必须读取 ActionRuntime 当前唯一 ActionInstance：匹配该实例的 producer 可以覆盖 locomotion；不存在当前动作时只能选择唯一 locomotion producer；同一所有权域同层出现多个 producer 必须报错。TimelinePlaybackScheduler 只提供活跃 producer 集合，不自行维护 Action/Locomotion 两套赢家状态。

### Motion channel ownership

MotionContribution 将 `HasDelta` 与 `ClaimsLowerChannels` 分开。零位移 Override contribution 只要显式 `ConsumeLowerChannels`，仍可成为该 channel 的 winner 并清空此前累计的低层 motion；零位移 Additive/WeightedBlend 不产生占权。

MotionCurveClip 的 `EndFrame` 表示 contribution 与 channel claim 的作者区间，`CurveEndFrame` 表示累计位移曲线到达终值的时刻。`CurveEndFrame` 之后至 `EndFrame` 继续输出零 delta claim，不拉伸已有位移曲线。所有现有 MotionCurveClip 必须显式迁移该字段，不提供缺省兼容解释。

### Decision TreeClip sampling

Decision TreeClip 使用本次 logic tick 的 `[previousTime, currentTime]` 区间判断相交，而不是只检查 target time。Loop 跨边界时按尾段、完整中间 cycle 和头段逐段求值，并继续用 track/clip/cycle identity 保证每 tick 每 cycle 最多执行一次。

### Graph initialization

BaseGraph 的公开初始化入口不再依靠重载内部虚调用。统一入口先校验 parent/authoring route 与派生上下文，再建立 runtime maps，最后调用派生初始化钩子。TimelineRunningTree 在入口前设置正式 TimelineTreeClipRuntimeContext，并在校验钩子拒绝普通 InitTree 调用。

### Timeline owns sample time

TimelinePlaybackScheduler 继续维护每个 AnimationPlaybackId 的逻辑时间、loop mode、速度和 clip 区间。表现帧根据 visual Timeline time 生成：

    AnimationProducerSample
      PlaybackId
      LayerId
      SampleTime
      Clip states
      Clip local time
      Clip weight
      Loop metadata

Clip weight 只描述同一个 producer 内部 Timeline overlap、ease-in/ease-out 或 mixer 输入。它不参与不同 playback 之间的胜负。

AnimationTrack.Priority 从表现合同删除。若业务需要 priority，必须在提交 AnimationLayerSelection 之前由逻辑节点或状态关系解决。

### Animation owns playback lifetime

每个 layer 只有以下状态：

- PendingFirstSample：逻辑已经选择 target，但 target 尚无正式表现 sample。
- Current：当前由 Animancer 显示的 producer。
- Outgoing：此前的 Current 已交给 Animancer 淡出。
- Retired：fade 完成并释放 retention、state 和 producer 资源。

状态转换：

    No Current + Selection + Sample
      -> Current

    Current A + Selection B without Sample B
      -> PendingFirstSample B, keep A current

    Current A + Selection B + Sample B
      -> Animancer Play/Fade B
      -> A Outgoing
      -> B Current

    Current A + Selection None on AllowEmpty layer
      -> Animancer fade layer/state out
      -> A Outgoing

    Outgoing A fade complete
      -> Retired A

    Current B is replaced by C during A fade
      -> call Animancer from current visual graph
      -> lifecycle updates references
      -> no custom handoff stack

PendingFirstSample 只等待目标本身，不重新选择其它 producer。若目标 complete/release 后仍从未产生 sample，RequireOutput layer 报错，AllowEmpty layer按正式空选择处理。

### Animancer owns blending

AnimancerPlaybackAdapter 只做协议适配：

- 由一个 producer sample 创建或复用一个 AnimancerState 或 ManualMixerState；
- 写入 Timeline 的 clip time、normalized time、loop 和 producer 内部 child weights；
- 使用 stable producer key 保证 state 可复用；
- 调用 TransitionLibrary.Play 或 AnimancerLayer.Play；
- 将配置的 easing 交给 Animancer FadeGroup；
- 观察 Animancer state/fade 完成并通知 lifecycle retire；
- 不计算 outgoing/incoming state weight，不输出 LayerPlan。

Timeline sample time 与 fade time 是两个正交时钟：

- pose time 由 Timeline 在每个 presentation frame 重采样；
- fade progress 由 Animancer 使用 presentation delta 推进。

不得继续使用 Evaluate(0) 加项目自算 weight 的方式伪装成 Animancer fade。若管线采用 manual update，传给 Animancer 的必须是正式 presentation delta，Timeline state 的播放速度由外部采样合同控制。

## Outgoing Presentation Retention

逻辑 release 立即停止 gameplay Timeline、TreeClip、Motion、window 和 sync facts，但不能立刻销毁 Animancer 正在淡出的视觉 producer。

TimelinePlaybackScheduler 为 outgoing 提供只读 PresentationRetention：

    PlaybackId
    Last logic timeline state
    Visual sample cursor
    Retention owner = AnimationPlaybackLifecycle

retention 只允许生成 animation sample，不得重新运行 Timeline logic track、TreeClip、Motion、root motion 或 gameplay window。Animancer fade 完成后 lifecycle 释放 retention；Pipeline deactivate 时立即清理，不等待 fade。

## Transition Authoring

项目已安装的 Animancer 提供 TransitionLibraryAsset、TransitionLibrary.Play、ITransition、FadeMode、source-to-target fade duration modifier 和 FadeGroup easing。它们是转场播放权威。

CharacterAnimationPresentationDefinition 只保存：

- Layer catalog 与 OutputPolicy；
- 每个 Timeline animation producer 的 stable presentation key；
- producer 到 Animancer transition key/transition source 的绑定；
- 对单一正式 TransitionLibraryAsset 的引用；
- 编辑器投影所需的稳定 source identity。

它不保存：

- Tree Driver site；
- HandoffRole；
- Priority；
- Previous/Desired owner；
- Pipeline 自有 source-target duration/curve 表；
- LayerPlan 或运行时状态。

Pipeline Definition Inspector 只根据 RootTree 发现正式 animation producer，并按稳定 identity 显示其 Layer 与 binding；它不推导或复制 StateMachine producer 流向。作者通过 Graph 查看逻辑关系，通过 Timeline 编辑采样内容，并从 Definition Inspector 定位 Animancer TransitionLibrary 的 transition 或 modifier。

若需要 Animancer 原生能力之外的 pair-specific curve，本 change 不新增自制曲线混合器。该需求必须单独评估。

## Same-Frame Ordering

一个 PresentationFrame 按固定顺序处理：

1. 读取自上次表现帧以来的全部 logic selections、complete 和 release。
2. 根据最终 selection 请求 selected playback 的 visual sample。
3. 为仍被 lifecycle retention 持有的 outgoing playback 请求 visual sample。
4. 完整采样 Timeline animation tracks。
5. 将 selection 与 sample 作为同一批次交给 AnimationPlaybackLifecycle。
6. 对 ready target 调用 Animancer Play/Fade。
7. 更新 current/outgoing Animancer states。
8. 用 presentation delta 推进 Animancer。
9. 收集 fade completion，退休 outgoing 并释放 retention。
10. 成功后 acknowledge 整个批次。

同一表现帧包含多个 logic tick 时，只提交每层最终 selection，但 complete/release 和 playback generation 必须保序，避免旧 generation 的 sample 被新 selection 接受。

## Interruption Semantics

打断的业务生命周期和动画播放生命周期明确分离：

- LowerPriority、Self、State transition 或 ForceStop 先结束 source 的逻辑所有权；
- source 不再产生输入、motion、window、hit 或 sync facts；
- 逻辑层选择 target playback；
- target 首样本 ready 后，Animancer 从当前视觉结果切换；
- source 只作为 outgoing 视觉状态存在；
- fade 完成不反向延迟 State.OnExit、Tree terminal 或 target 逻辑执行；
- ForceStop/deactivate 直接清空视觉状态，不读取转场配置。

## Editor Boundaries

### Graph / StateMachine Editor

可写：topology、condition、priority、ownership、interruption、State scope。

不可写：animation transition、fade、Driver、presentation layer。

### Timeline Editor

可写：AnimationTrack LayerId、clip、时间、loop、ease、producer 内部 Weight。

不可写：跨 producer priority、source-target transition、logic ownership。

每个 Timeline 页面拥有独立 preview session、runtime clone、Queue、AnimationPlaybackLifecycle 和 Animancer preview state。一个 CharacterPipelineHost/AnimancerComponent 只有一份物理输出，因此同一时刻只接受一个 preview session；第二个页面绑定同一目标时明确失败。要同时观察两份动画预览必须选择两个独立 Preview target，不创建第二套 PlayableGraph 或共享 Graph clock。

### Character Pipeline Definition Inspector

可写：Layer catalog、TransitionLibrary 引用、producer presentation binding。

只读：从 RootTree 递归发现的 producer stable identity、LayerId 与来源 Timeline。

它是 CharacterPipelineDefinition 的唯一 Presentation 配置入口，不再存在独立 Animation Presentation 窗口。Graph 与 Timeline 仍是两个可同时打开的独立窗口；运行时 lifecycle 只由统一 RuntimeDebugSession/Host 调试视图显示。

## Diagnostics

Animation channel 只显示：

- logic selection；
- selected playback generation；
- Timeline sample time；
- PendingFirstSample / Current / Outgoing / Retired；
- Animancer state key、fade progress 和 retirement；
- duplicate selection、missing first sample、unknown layer/producer 等错误。

Graph channel 只显示 Tree/State 的逻辑执行和停止。不得再显示 Animation Driver、ExecutionLineage、CausalGraph、LayerPlan 或 MissingDriver。

## Migration

迁移顺序必须是：

1. 固化旧资产 inventory，识别旧 transition、Driver、LayerId 和 producer 路由的删除边界。
2. 验证 Animancer 原生 transition/easing、外部 Timeline 采样和表现帧 fade 能力。
3. 新增正式 Layer catalog、producer key、TransitionLibrary binding 和 Corin logic selection；旧自定义 Inertialization、strategy、duration 与 curve 不迁移。
4. 保存并静态检查新资产引用。
5. 切换 runtime 到新 selection -> lifecycle -> Animancer 链。
6. 删除旧 Tree lifecycle animation records、Driver、Lineage、Arbitration 和 custom LayerRuntime。
7. 删除旧序列化字段、旧 Inspector、Agent v5 operations 和一次性 migrator。
8. 更新 current specs 与 project.md。

旧自定义 transition 数据属于已确认可删除的中间实现，不作为业务资产保真。只有 stable producer identity、正式 LayerId 或 Timeline 外部采样无法安全闭环时才停止迁移。

## Deletion Set

- TreeExecutionLifecycle 中为动画新增的 activation/control-flow 合同和 sink。
- CharacterAnimationPresentationAdapter。
- CharacterAnimationExecutionLineage。
- AnimationTopologyRecord、AnimationHandoffIntent、AnimationOwnerReady。
- DriverBindingIndex、TreeLifecycleAuthoringKey 的动画用途。
- AnimationHandoffCausalGraphBuilder、Arbitrator、LayerPlan、ActiveHandoff、HandoffLedger。
- custom crossfade/inertialization layer state machine。
- AnimationContribution.Priority 与 AnimationTrack.Priority 的表现用途。
- StateMachineGraph/StateMachineNode 的旧动画 transition 字段。
- Graph presentation leaf、旧 Animation Inspector 和 Agent v5 Driver schema。

## Risks

- Corin 当前可能依赖 animation priority 才能让 Action 覆盖 Locomotion。迁移必须先把该决定放回 ActionOverride 或等价逻辑所有权，不能在 Animation 模块复刻 priority。
- 外部采样 Timeline state 与 Animancer 自动 fade 同时工作，需要明确 manual update 顺序。若无法通过正式 API闭环，按 Stop Conditions 停止。
- 旧自定义 transition curve 可能比 Animancer TransitionLibrary 原生 modifier 更丰富；不得静默降级。
- 当前工作树已有大量中间实现和序列化改动，迁移只能按文件 inventory 精确删除，不能回退用户其它改动。

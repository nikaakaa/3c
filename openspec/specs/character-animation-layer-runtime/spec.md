# character-animation-layer-runtime Specification

## Purpose
定义角色动画层运行时：逻辑侧为每层提交唯一 `AnimationLayerSelection`，Timeline 在表现帧提供匹配 generation 的 `AnimationProducerSample`，`AnimationPlaybackLifecycle` 管理可见 producer 寿命，Animancer 负责实际 state、layer 与 fade，避免逻辑优先级和动画混合形成两套仲裁真相。
## Requirements
### Requirement: 动画层定义来自管线定义

`CharacterPipelineDefinition` 引用的 `CharacterAnimationPresentationProfile` MUST作为动画 Layer catalog 与producer resource binding的唯一authoring来源。唯一Presentation Projection Compiler MUST将layer identity、order、Animancer layer index、mask、blend mode、output policy和producer binding编入target-neutral `CharacterPresentationProjection`；Runtime MUST只读取匹配`CharacterPresentationSemanticContract`、Gameplay SourceRevision与ProjectionRevision的Projection。ProgramHash、NumericProfile与Target ABI只属于目标Program和Session compatibility，MUST不进入Projection payload或动画层选择。Definition、Timeline、Graph、Presenter、旧SO或独立Layer asset MUST不保存另一份layer真数据。

#### Scenario: Base layer 要求持续输出

- **WHEN** Corin Base layer 在 Profile 中配置为 RequireOutput 并编入 Projection
- **THEN** 正常激活期间该层 MUST 拥有 Current、PendingFirstSample 或明确 Invalid 状态
- **AND** 系统 MUST 不静默把该层解释为 Empty

#### Scenario: Optional layer 允许为空

- **WHEN** 某 layer 在 Profile 中显式配置为 AllowEmpty 并编入 Projection
- **THEN** Program MAY 输出该层 None command
- **AND** Animancer MUST 按正式 transition 将该层淡出到空
- **AND** 系统 MUST 不创建 fallback clip

#### Scenario: producer command 引用缺失 layer

- **WHEN** committed producer command 或 Projection binding 的 LayerId 不存在
- **THEN** Program/Projection 组合校验 MUST 报告配置错误
- **AND** 对应 command MUST 不进入播放生命周期

#### Scenario: Float32与Fixed复用动画层Projection

- **WHEN** Float32与Fixed Program由相同SemanticHash和producer contract生成
- **THEN** 两个Presentation contract Adapter MUST加载同一套Layer与producer binding
- **AND** Runtime MUST不按ProgramHash复制、选择或降级Projection

### Requirement: 基础姿态必须由正式来源输出

Base pose、Idle、Move 与其它基础动画 MUST来自正式 Graph/State/Action 所选择的 Timeline animation producer。RequireOutput layer 在 target 首样本到达前 MAY保持已有 Current，但 MUST保留 PendingFirstSample target identity。Pipeline、lifecycle 与 Animancer adapter MUST不内置隐藏基础姿态 producer。

#### Scenario: 首次激活缺少基础动画

- **WHEN** RequireOutput Base 没有 Current
- **AND** 逻辑层没有合法 selection 或 selected target 没有 sample
- **THEN** lifecycle MUST报告明确 Invalid
- **AND** 系统 MUST不选择 bind pose clip、旧 locomotion 或隐藏 Idle

#### Scenario: 已有输出后 incoming 延迟

- **WHEN** Base 已有 Current A 且 selection 已变为 B
- **AND** B 的第一份 sample 尚未到达
- **THEN** lifecycle MUST保持 A 并记录 PendingFirstSample B
- **AND** MUST不把 A 重新声明为逻辑 winner

### Requirement: 角色管线不依赖旧动画播放路径

角色管线和 BTSMTL Timeline 编辑器预览 MUST共用一条语义：逻辑提交每层 selection，Timeline 生成 animation sample，AnimationPlaybackLifecycle 管理 producer 寿命，Animancer 应用 state/mixer/fade。系统 MUST不读取旧 AnimationPresentationPolicySO、旧 locomotion/action SO、旧 bodyclaim policy，也 MUST不依赖 TimelinePlayer autonomous playback、Animator.Play、Animator.CrossFade 或独立 PlayableGraph 作为另一权威。

#### Scenario: 搜索旧直接播放入口

- **WHEN** 实现阶段发现角色运行路径仍直接调用旧动画播放入口
- **THEN** 该引用 MUST删除或迁移到正式 Animancer adapter
- **AND** 系统 MUST不保留兼容分支

#### Scenario: BTSMTL 编辑器预览播放 Timeline

- **WHEN** Timeline 编辑器预览角色动画
- **THEN** 预览 MUST复用正式 Timeline sampling、AnimationPlaybackLifecycle 与 Animancer adapter
- **AND** 预览 MUST不创建独立仲裁器或 PlayableGraph 权威

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环 producer 的 continuous visual time MUST由 committed Timeline logic sample、cycle identity 与 PresentationFrame interpolation计算。AnimationPlaybackLifecycle MUST只关联 selected/current/outgoing producer，不得推进 CharacterSimulationState Timeline clock。逻辑 producer release 后，只要视觉生命周期仍持有该 playback，PresentationRetention MUST继续 animation-only sampling，且 MUST不执行 Gameplay operation。

#### Scenario: 循环回绕

- **WHEN** committed loop sample 从末尾回绕到开头
- **THEN** animation track MUST使用连续 visual time重采样同一 playback generation

#### Scenario: Source 已停止

- **WHEN** producer Gameplay ownership 已 release 且 Animancer state仍 Outgoing
- **THEN** Presentation MAY继续 animation-only sample
- **AND** TreeClip、Motion、Window 与 Cue fact MUST不再产生

### Requirement: 动画片段 membership 必须显式提交和释放

Timeline producer MUST显式提交 AnimationProducerSample、Complete 与 Release。进入或继续处于有效动画片段时 MUST提交 Sample；离开 ExtraPolationMode=None 片段、playback 失败或 producer 正式销毁时 MUST提交 Release。AnimationPlaybackLifecycle MUST不因当帧缺少 Sample 自动释放 Current，也 MUST不因历史 sample 存在而把无效 target 当作 ready。

#### Scenario: None 片段结束但 Timeline 继续

- **WHEN** Timeline 时间已经超过某 AnimationClip 的 EndTime
- **AND** 该 clip 的 ExtraPolationMode 是 None
- **THEN** producer MUST对该 clip slot 提交 Release
- **AND** 后续 sample MUST不继续包含该历史 clip

#### Scenario: Hold 片段结束但 Timeline 继续

- **WHEN** Timeline 时间已经超过某 AnimationClip 的 EndTime
- **AND** 该 clip 的 ExtraPolationMode 是 Hold
- **THEN** AnimationTrack MUST继续提交正式 Hold sample
- **AND** Hold MUST不来自 lifecycle 或 Presenter 的隐式 fallback

### Requirement: 动画层输入必须是已解析播放选择与正式采样

Animation module MUST只接收 Program Finalize 已解析的 Layer selection command，以及 Presentation sampler 生成的 ProducerSample、Complete 和 Release。Selection MUST表达 LayerId、PlaybackId、generation、SimulationTick、sequence 与 EventId，MUST不携带 Priority、Driver、Tree route 或候选列表。

#### Scenario: Base 收到唯一 Target

- **WHEN** committed batch 为 Base 选择一个 PlaybackId
- **THEN** Animation module MUST只等待和播放该 target

#### Scenario: 同层重复选择

- **WHEN** 同一 Tick result 为同一 LayerId 输出两个不同 target
- **THEN** Finalize MUST报告逻辑冲突并拒绝 Tick

### Requirement: 动画播放生命周期必须只管理可见 producer 寿命

每个 LayerId MUST拥有一个 AnimationPlaybackLifecycleState，并只使用 PendingFirstSample、Current、Outgoing 与 Retired 表达播放寿命。PendingFirstSample MUST等待选中 target 的第一份合法 sample；Current MUST对应当前交给 Animancer 的 target；Outgoing MUST对应 Animancer 正在淡出的旧 state；Retired MUST释放该 producer 的表现 retention 与播放资源。该生命周期 MUST不解释 State、Action、Tree interruption 或业务 Priority。

#### Scenario: target 首样本延迟

- **WHEN** Current A 已存在且逻辑选择 B
- **AND** B 尚未产生第一份合法 sample
- **THEN** lifecycle MUST记录 PendingFirstSample B 并继续显示 A
- **AND** MUST不选择默认 Idle、Empty、当前 clip 副本或其它 producer

#### Scenario: target 首样本到达

- **WHEN** PendingFirstSample B 收到匹配 playback generation 的合法 sample
- **THEN** lifecycle MUST原子地请求 Animancer 播放 B
- **AND** A MUST进入 Outgoing
- **AND** B MUST进入 Current

#### Scenario: outgoing 淡出完成

- **WHEN** Animancer 报告 A 的 fade 已完成
- **THEN** lifecycle MUST将 A 标记 Retired
- **AND** MUST释放 A 的 PresentationRetention

### Requirement: Animancer 必须是实际动画混合权威

Animancer MUST负责 state/mixer 创建后的 layer 混合、fade weight、重入和最终 Animator 输出。AnimancerPlaybackAdapter MAY创建或复用 AnimancerState/ManualMixerState、写入 Timeline 采样时间和 producer 内部 child weights、调用 TransitionLibrary.Play 或 AnimancerLayer.Play，并将 easing 交给 FadeGroup。项目代码 MUST不计算 LayerPlan、incoming/outgoing state weight、ActiveHandoff 或自定义 crossfade 进度。

#### Scenario: producer 包含多个 clip

- **WHEN** 同一 Timeline producer 在一个 layer 内采样到多个重叠 clip
- **THEN** Adapter MUST用 ManualMixerState 表达 producer 内部 clip weights
- **AND** Animancer MUST负责该 state 与其它 state 的 fade

#### Scenario: fade 期间再次切换

- **WHEN** 当前 Animancer 视觉图仍在淡出 A 时逻辑选择 C
- **THEN** Adapter MUST从 Animancer 当前视觉状态播放 C
- **AND** 项目 MUST不建立 handoff stack 或恢复中间逻辑状态

### Requirement: 同组 producer handoff 必须按 Marker Segment 映射

当同一Layer从outgoing Current切换到incoming target，且两者Projection binding均为MarkerGroup并拥有相同canonical SyncGroupId时，Animation Runtime MUST只从AnimationPlaybackLifecycle取得这两个真实playback，并按两侧SyncRole解析唯一leader与follower。默认由outgoing领导；incoming为AlwaysLeader或outgoing为AlwaysFollower时 MUST反向由incoming领导。Runtime MUST按leader effective time所在的有向MarkerId pair与segment fraction映射follower Timeline time，不得使用StateMachine上一状态、Graph edge、producer显示名、clip名称、Action名称、逻辑priority或Animancer weight推导方向。

#### Scenario: WalkLoop切换RunLoop

- **WHEN** Base层实际Current为WalkLoop且incoming target为同组RunLoop
- **THEN** Runtime MUST读取WalkLoop当前effective marker segment与fraction
- **AND** MUST在RunLoop的相同有向marker pair occurrence中计算target effective time

#### Scenario: source或target显式None

- **WHEN** source或target AnimationTrack的Projection binding为None
- **THEN** Runtime MUST明确记录NotApplicable
- **AND** target effective time MUST等于其raw visual time

#### Scenario: 同组数据损坏

- **WHEN** source与target声明同组但Projection缺少segment、duration或sampling state
- **THEN** Animation Runtime MUST进入明确Invalid并报告稳定diagnostic code
- **AND** MUST不静默退回normalized time、隐藏Idle或Animancer自动同步

#### Scenario: incoming finite producer要求领导

- **WHEN** outgoing Current为CanBeLeader且incoming target为AlwaysLeader
- **THEN** Runtime MUST建立`incoming -> outgoing` relation
- **AND** incoming MUST继续使用自己的raw表现节奏

#### Scenario: handoff角色冲突

- **WHEN** 两侧同时为AlwaysLeader或同时为AlwaysFollower
- **THEN** Runtime MUST以typed invalid reason失败
- **AND** 不得静默选择outgoing或incoming

### Requirement: Marker Sync 必须在共同可见期间持续求值

Marker Sync MUST在source与target共同可见的每个PresentationFrame重新使用source effective marker segment求target effective time，不得仅在target首样本时保存固定phase offset。target MUST以mapped time重新采样整个producer，包括所有AnimationClip membership、ClipIn、ease和内部weight。Gameplay提交的raw Timeline sample、cycle和logic completion MUST保持不变。

#### Scenario: 不同时长循环动画fade

- **WHEN** 1.0秒WalkLoop与0.6秒RunLoop在Animancer fade期间共同可见
- **THEN** RunLoop MUST在每个PresentationFrame持续对齐WalkLoop marker fraction
- **AND** MUST不因两个producer各自速度不同而在fade后半段重新漂移

#### Scenario: 多clip target producer

- **WHEN** target AnimationTrack在mapped time采样到两个重叠AnimationClip
- **THEN** 两个clip的membership、time与内部weight MUST由同一个effective Timeline time重新采样
- **AND** Marker Sync MUST不选择某一个clip作为第二phase authority

#### Scenario: Gameplay状态立即切换

- **WHEN** Program在logic tick将状态从Walk切换到Run
- **THEN** Gameplay state、Motion与World request MUST在该tick按原规则推进
- **AND** Presentation MUST不等待marker边界后才提交状态切换

### Requirement: Finite 与 Cyclic producer 必须使用明确拓扑映射

Runtime MUST支持`Cyclic -> Cyclic`、`Cyclic -> Finite`、`Finite -> Cyclic`和`Finite -> Finite`同组映射。Cyclic source/target MAY按duration回绕并保持展开cycle；Finite source/target MUST不回绕，target occurrence MUST单调前进。target首次存在多个相同有向pair occurrence时，Runtime MUST按与raw target time最小距离选择，并以frame和MarkerAuthoringId稳定破同；relation存活期间 MUST保持该occurrence连续性。

#### Scenario: RunLoop进入RunEnd

- **WHEN** Cyclic RunLoop切换到同组Finite RunEnd
- **THEN** Runtime MUST选择RunEnd中与当前raw time最近的兼容marker pair occurrence
- **AND** 后续共同可见帧 MUST沿RunEnd有限序列向前推进

#### Scenario: Finite source返回循环移动

- **WHEN** 同组Finite Turn或End producer切换到Cyclic locomotion producer
- **THEN** Runtime MUST从Finite source当前非回绕segment映射target最近展开cycle
- **AND** target成为独立Current后 MUST继续该展开cycle而不跳回cycle 0

#### Scenario: Finite覆盖耗尽

- **WHEN** relation要求Finite target前进到其marker coverage之外
- **THEN** Runtime MUST报告FiniteCoverageExceeded
- **AND** MUST不回绕Finite producer或静默解除同步

### Requirement: Sync relation 必须服从播放生命周期并连续脱离

Sync relation MUST以完整AnimationPlaybackId为key，并且只依赖AnimationPlaybackLifecycle的Current、PendingFirstSample、Outgoing与Retired事实。快速连续切换 MUST按实际relation依赖形成无环effective-time图并拓扑求值，不得假设leader generation一定早于follower。source正式Retired时，Runtime MUST以target最后effective time和raw time建立continuation anchor，删除relation，并让target按后续raw delta连续推进。Reset、target Retired和Dispose MUST清除对应relation与anchor。

#### Scenario: 连续A到B到C

- **WHEN** B仍跟随Outgoing A时Current B又切换到C
- **THEN** 当帧求值顺序 MUST为`A effective -> B effective -> C effective`
- **AND** C MUST读取B的effective time而不是B未经映射的raw time

#### Scenario: source淡出完成

- **WHEN** Animancer与lifecycle将source正式标记Retired
- **THEN** target MUST从最后mapped effective time建立continuation anchor
- **AND** 下一帧 MUST按target raw delta连续推进而不跳回原始Timeline time

#### Scenario: relation拓扑非法

- **WHEN** Runtime检测到relation环、同一target拥有两个source或跨Layer依赖
- **THEN** 对应Layer MUST进入明确Invalid
- **AND** MUST不依赖集合遍历顺序选择任意relation

### Requirement: outgoing producer 必须使用纯表现 retention

逻辑 producer release 后，只要该 playback 仍为 Selected、PendingFirstSample、Current 或 Outgoing，AnimationPlaybackLifecycle MUST持有只读 PresentationRetention，让纯表现 sampler继续生成 animation sample直到视觉生命周期真正 Retired。Runtime MUST不因逻辑 SampleProducer event 先于视觉 fade 退役而删除该 sampling state。Retention MUST不恢复 Program membership，也 MUST不运行 TreeClip、Motion、root motion、window或cue operation，且 MUST不生成GameplayFact或新的PresentationCommand。

#### Scenario: 攻击逻辑结束但动画淡出

- **WHEN** Attack playback Gameplay 已停止且 Animancer state仍 Outgoing
- **THEN** sampler MUST只推进 animation visual sample

#### Scenario: 逻辑采样先于淡出退役

- **GIVEN** outgoing playback 仍由 AnimationPlaybackLifecycle 持有
- **WHEN** 对应逻辑 SampleProducer event 已 release
- **THEN** PresentationRetention MUST继续按表现时间产生该 playback 的 animation-only sample
- **AND** sampling state MUST只在视觉生命周期进入 Retired 后删除

#### Scenario: Session dispose

- **WHEN** Actor/Session dispose
- **THEN** lifecycle MUST立即清理 Current、Outgoing、PendingFirstSample 与 retention

### Requirement: Animancer 必须继续独占 Fade 与最终 Pose

MarkerSyncRuntime MUST只提供producer effective sample time。Animancer MUST继续独占state/mixer、TransitionLibrary、FadeMode、duration modifier、easing、layer weight、outgoing retirement与最终pose；AnimancerPlaybackAdapter MUST继续对Timeline控制的child使用`DontSynchronize`。项目 MUST不新增自定义crossfade weight、Animancer automatic synchronization或第二动画时钟。

#### Scenario: 同步target开始播放

- **WHEN** matched target首份合法sample进入lifecycle
- **THEN** Adapter MUST通过正式Animancer transition播放target
- **AND** MarkerSyncRuntime MUST不写入fade progress或state weight

#### Scenario: source retirement由Animancer确认

- **WHEN** source逻辑ownership已释放但Animancer仍在淡出
- **THEN** source MUST继续通过PresentationRetention提供animation-only sample与effective time
- **AND** relation MUST只在正式Retired后脱离

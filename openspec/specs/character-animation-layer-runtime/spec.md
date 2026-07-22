# character-animation-layer-runtime Specification

## Purpose
定义角色动画通道到最终Pose的正式运行时：逻辑侧为每个AnimationChannelId提交唯一selection，Timeline在表现帧生成匹配generation的ResolvedAnimationPoseRequest，AnimationPlaybackLifecycle管理Selected/Retained producer寿命，每PoseSlot Blend Stack负责transition，Pose Graph负责跨slot空间组合与最终Animator输出。
## Requirements
### Requirement: 动画通道与Pose Slot定义必须分离

Timeline、Semantic IR、Program producer contract、selection command与Playback Lifecycle MUST只使用稳定AnimationChannelId表达逻辑仲裁通道。`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一声明PoseSlotId、channel-to-slot一对一binding与OutputPolicy；Blend Library MUST按PoseSlotId声明Stack policy与transition matrix。Projection Compiler MUST把producer resource、AnimationChannelId、PoseSlotId、compiled Stack、Rig与Pose Program编入target-neutral `CharacterPresentationProjection`。Runtime MUST不读取Layer catalog、Animancer layer index、Profile layer order或旧LayerId，并 MUST不按ProgramHash选择Projection。

#### Scenario: BaseLocomotion要求持续输出

- **WHEN** Corin BaseLocomotion channel绑定到RequireOutput BaseLocomotionSlot
- **THEN** 正常激活期间该slot MUST拥有Selected Stack Entry、PendingFirstSample或明确Invalid状态
- **AND** 系统 MUST不静默把该slot解释为Empty

#### Scenario: FullBodyAction允许为空

- **WHEN** FullBodyAction channel提交None且对应slot为AllowEmpty
- **THEN** 该slot Stack MUST按exact source-to-Empty transition输出typed NoPose
- **AND** Pose Graph MUST让BaseLocomotion继续通过且不创建fallback clip

#### Scenario: producer command引用未知channel

- **WHEN** committed producer command的AnimationChannelId不存在或没有精确PoseSlot binding
- **THEN** Program/Projection组合校验 MUST报告配置错误
- **AND** 对应command MUST不进入Lifecycle、Stack或Pose Graph

#### Scenario: Float32与Fixed复用动画Projection

- **WHEN** Float32与Fixed Program由相同SemanticHash和producer contract生成
- **THEN** 两个Presentation contract Adapter MUST加载同一套channel/slot/pose program与producer binding
- **AND** Runtime MUST不按ProgramHash复制、选择或降级Projection

### Requirement: 基础姿态必须由正式来源输出

Base pose、Idle、Move 与其它基础动画 MUST来自正式 Graph/State/Action 所选择的 Timeline animation producer。RequireOutput PoseSlot在target首份合法`ResolvedAnimationPoseRequest`到达前 MAY保持既有Retained Stack Entry，但Lifecycle MUST保留PendingFirstSample target identity。Pipeline、Lifecycle、Blend Stack与source sampling backend MUST不内置隐藏基础姿态producer。

#### Scenario: 首次激活缺少基础动画

- **WHEN** RequireOutput BaseLocomotionSlot没有Selected或Retained pose
- **AND** 逻辑层没有合法 selection 或 selected target 没有 sample
- **THEN** lifecycle MUST报告明确 Invalid
- **AND** 系统 MUST不选择 bind pose clip、旧 locomotion 或隐藏 Idle

#### Scenario: 已有输出后 incoming 延迟

- **WHEN** BaseLocomotionSlot已有Retained A且selection已变为B
- **AND** B 的第一份 sample 尚未到达
- **THEN** Lifecycle MUST保持A的Stack Entry并记录PendingFirstSample B
- **AND** MUST不把 A 重新声明为逻辑 winner

### Requirement: 角色管线不依赖旧动画播放路径

角色管线和BTSMTL Timeline编辑器预览 MUST共用一条语义：逻辑按AnimationChannel提交selection，Timeline生成`ResolvedAnimationPoseRequest`，AnimationPlaybackLifecycle管理Selected/Retained producer寿命，每PoseSlot Blend Stack管理transition，Pose Graph写回最终pose。系统 MUST不读取旧AnimationPresentationPolicySO、旧locomotion/action SO、旧bodyclaim policy，也 MUST不依赖TimelinePlayer autonomous playback、Animator.Play、Animator.CrossFade或独立PlayableGraph作为另一权威。

#### Scenario: 搜索旧直接播放入口

- **WHEN** 实现阶段发现角色运行路径仍直接调用旧动画播放入口
- **THEN** 该引用 MUST删除或迁移到正式PlaybackRuntime
- **AND** 系统 MUST不保留兼容分支

#### Scenario: BTSMTL 编辑器预览播放 Timeline

- **WHEN** Timeline 编辑器预览角色动画
- **THEN** 预览 MUST复用正式Timeline sampling、AnimationPlaybackLifecycle、Blend Stack、source backend与Pose Graph
- **AND** 预览 MUST不创建独立仲裁器或 PlayableGraph 权威

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环producer的continuous visual time MUST由committed Timeline logic sample、cycle identity与PresentationFrame interpolation计算。AnimationPlaybackLifecycle MUST只关联Selected/Retained producer，不得推进CharacterSimulationState Timeline clock。逻辑producer release后，只要Blend Stack仍保留对应source，PresentationRetention MUST继续animation-only sampling，且 MUST不执行Gameplay operation。

#### Scenario: 循环回绕

- **WHEN** committed loop sample 从末尾回绕到开头
- **THEN** animation track MUST使用连续 visual time重采样同一 playback generation

#### Scenario: Source 已停止

- **WHEN** producer Gameplay ownership已release且Blend Stack仍Retained该source
- **THEN** Presentation MAY继续 animation-only sample
- **AND** TreeClip、Motion、Window 与 Cue fact MUST不再产生

### Requirement: 动画片段 membership 必须显式提交和释放

Timeline producer MUST显式提交PoseRequest、Complete与Release。进入或继续处于有效动画片段时 MUST提交`ResolvedAnimationPoseRequest`；离开ExtraPolationMode=None片段、playback失败或producer正式销毁时 MUST提交Release。AnimationPlaybackLifecycle MUST不因当帧缺少PoseRequest自动释放Selected或Retained source，也 MUST不因历史request存在而把无效target当作ready。

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

### Requirement: 动画通道输入必须是已解析播放选择与正式Pose Request

Animation module MUST只接收Program Finalize已解析的channel selection command，以及Presentation sampler生成的PoseRequest、Complete和Release。Selection MUST表达AnimationChannelId、PlaybackId、generation、SimulationTick、sequence与EventId，MUST不携带Priority、Driver、Tree route或候选列表。

#### Scenario: Base 收到唯一 Target

- **WHEN** committed batch为BaseLocomotion channel选择一个PlaybackId
- **THEN** Animation module MUST只等待和播放该 target

#### Scenario: 同通道重复选择

- **WHEN** 同一Tick result为同一AnimationChannelId输出两个不同target
- **THEN** Finalize MUST报告逻辑冲突并拒绝 Tick

### Requirement: 动画播放生命周期必须只管理可见 producer 寿命

每个AnimationChannelId MUST拥有一个AnimationPlaybackLifecycleState，并只使用PendingFirstSample、Selected、Retained与Retired表达播放寿命。PendingFirstSample MUST等待选中target的第一份合法request；Selected MUST对应当前逻辑选择并交给PoseSlot Stack的target；Retained MUST对应Stack仍为视觉连续性保留的旧source；Retired MUST在Stack exact completion发布release后释放该producer的表现retention与source playable。该生命周期 MUST不解释State、Action、Tree interruption或业务Priority。

#### Scenario: target 首样本延迟

- **WHEN** Selected A已存在且逻辑选择B
- **AND** B 尚未产生第一份合法 sample
- **THEN** Lifecycle MUST记录PendingFirstSample B并继续保留A的Stack输出
- **AND** MUST不选择默认 Idle、Empty、当前 clip 副本或其它 producer

#### Scenario: target 首样本到达

- **WHEN** PendingFirstSample B 收到匹配 playback generation 的合法 sample
- **THEN** Lifecycle MUST原子地向对应PoseSlot Stack push B
- **AND** A MUST按Stack状态进入Retained
- **AND** B MUST进入Selected

#### Scenario: Retained source完成transition

- **WHEN** Blend Stack在exact completed frame后发布A的source release
- **THEN** Lifecycle MUST将A标记Retired
- **AND** MUST释放 A 的 PresentationRetention

### Requirement: PoseSlot Blend Stack必须是transition权威

每PoseSlot的`AnimationBlendStackRuntime` MUST唯一负责entry、transition clock、Per-Bone weight、Stored Pose、Inertial状态与source release。Animancer source backend MUST只创建或复用source playable、写入Timeline采样时间与producer内部clip weights，并把source capture job安装到同一PlayableGraph。Pose Graph MUST唯一负责跨slot空间组合与最终AnimationStream写回。其它项目代码 MUST不复制Stack entry/weight算法、建立managed evaluator或执行第二次Evaluate。

#### Scenario: producer 包含多个 clip

- **WHEN** 同一Timeline producer采样到多个重叠clip
- **THEN** source backend MUST在同一source playable内表达producer内部clip weights
- **AND** PoseSlot Stack MUST负责该source与其它source之间的transition

#### Scenario: transition期间再次切换

- **WHEN** 当前PoseSlot Stack仍保留A时逻辑选择C
- **THEN** Stack MUST从唯一正式entry/Stored/Inertial状态push C
- **AND** PlaybackRuntime MUST不建立第二个handoff stack或恢复中间逻辑状态

#### Scenario: slot概览权重为零但骨骼仍有贡献

- **WHEN** Stack完成帧的OutputWeight为零但dense per-bone output仍至少有一个非零权重
- **THEN** PoseSlot availability MUST保持Pose
- **AND** Pose Graph MUST按dense per-bone weight执行空间合成
- **AND** MUST不使用OutputWeight裁掉仍然有效的骨骼姿势

### Requirement: 同组 producer handoff 必须按 Marker Segment 映射

当同一AnimationChannel从Retained source切换到incoming Selected target，且两者Projection binding均为MarkerGroup并拥有相同canonical SyncGroupId时，Animation Runtime MUST只从AnimationPlaybackLifecycle取得这两个真实playback，并按两侧SyncRole解析唯一leader与follower。默认由Retained source领导；incoming为AlwaysLeader或source为AlwaysFollower时 MUST反向由incoming领导。Runtime MUST按leader effective time所在的有向MarkerId pair与segment fraction映射follower Timeline time，不得使用StateMachine上一状态、Graph edge、producer显示名、clip名称、Action名称、逻辑priority或Stack weight推导方向。

#### Scenario: WalkLoop切换RunLoop

- **WHEN** BaseLocomotion channel的Retained source为WalkLoop且incoming Selected target为同组RunLoop
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

- **WHEN** Retained source为CanBeLeader且incoming target为AlwaysLeader
- **THEN** Runtime MUST建立`incoming -> outgoing` relation
- **AND** incoming MUST继续使用自己的raw表现节奏

#### Scenario: handoff角色冲突

- **WHEN** 两侧同时为AlwaysLeader或同时为AlwaysFollower
- **THEN** Runtime MUST以typed invalid reason失败
- **AND** 不得静默选择outgoing或incoming

### Requirement: Marker Sync 必须在共同可见期间持续求值

Marker Sync MUST在source与target共同可见的每个PresentationFrame重新使用source effective marker segment求target effective time，不得仅在target首样本时保存固定phase offset。target MUST以mapped time重新采样整个producer，包括所有AnimationClip membership、ClipIn、ease和内部weight。Gameplay提交的raw Timeline sample、cycle和logic completion MUST保持不变。

#### Scenario: 不同时长循环动画fade

- **WHEN** 1.0秒WalkLoop与0.6秒RunLoop在PoseSlot transition期间共同可见
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
- **AND** target成为独立Selected source后 MUST继续该展开cycle而不跳回cycle 0

#### Scenario: Finite覆盖耗尽

- **WHEN** relation要求Finite target前进到其marker coverage之外
- **THEN** Runtime MUST报告FiniteCoverageExceeded
- **AND** MUST不回绕Finite producer或静默解除同步

### Requirement: Sync relation 必须服从播放生命周期并连续脱离

Sync relation MUST以完整AnimationPlaybackId为key，并且只依赖AnimationPlaybackLifecycle的Selected、PendingFirstSample、Retained与Retired事实。快速连续切换 MUST按实际relation依赖形成无环effective-time图并拓扑求值，不得假设leader generation一定早于follower。source正式Retired时，Runtime MUST以target最后effective time和raw time建立continuation anchor，删除relation，并让target按后续raw delta连续推进。Reset、target Retired和Dispose MUST清除对应relation与anchor。

#### Scenario: 连续A到B到C

- **WHEN** B仍跟随Retained A时Selected B又切换到C
- **THEN** 当帧求值顺序 MUST为`A effective -> B effective -> C effective`
- **AND** C MUST读取B的effective time而不是B未经映射的raw time

#### Scenario: source淡出完成

- **WHEN** Stack release与Lifecycle将source正式标记Retired
- **THEN** target MUST从最后mapped effective time建立continuation anchor
- **AND** 下一帧 MUST按target raw delta连续推进而不跳回原始Timeline time

#### Scenario: relation拓扑非法

- **WHEN** Runtime检测到relation环、同一target拥有两个source或跨AnimationChannel依赖
- **THEN** 对应AnimationChannel MUST进入明确Invalid
- **AND** MUST不依赖集合遍历顺序选择任意relation

### Requirement: outgoing producer 必须使用纯表现 retention

逻辑producer release后，只要该playback仍为Selected、PendingFirstSample或Retained，AnimationPlaybackLifecycle MUST持有只读PresentationRetention，让纯表现sampler继续生成pose request直到视觉生命周期真正Retired。Runtime MUST不因逻辑producer先于Stack release退役而删除该sampling state。Retention MUST不恢复Program membership，也 MUST不运行TreeClip、Motion、root motion、window或cue operation，且 MUST不生成GameplayFact或新的PresentationCommand。

#### Scenario: 攻击逻辑结束但动画淡出

- **WHEN** Attack playback Gameplay已停止且Stack仍Retained该source
- **THEN** sampler MUST只推进 animation visual sample

#### Scenario: 逻辑采样先于淡出退役

- **GIVEN** Retained playback仍由AnimationPlaybackLifecycle持有
- **WHEN** 对应逻辑 SampleProducer event 已 release
- **THEN** PresentationRetention MUST继续按表现时间产生该 playback 的 animation-only sample
- **AND** sampling state MUST只在视觉生命周期进入 Retired 后删除

#### Scenario: Session dispose

- **WHEN** Actor/Session dispose
- **THEN** Lifecycle MUST立即清理Selected、Retained、PendingFirstSample与retention

### Requirement: source backend必须只负责采样

MarkerSyncRuntime MUST只提供producer effective sample time。Animancer source backend MUST只拥有source playable与producer内部clip采样；PoseSlot Blend Stack MUST唯一拥有transition clock、curve、Per-Bone weight、Stored/Inertial与release；Pose Graph MUST唯一拥有跨slot空间组合与最终pose。项目 MUST不新增第二套crossfade weight、Animancer automatic synchronization、managed evaluator或第二动画时钟。

#### Scenario: 同步target开始播放

- **WHEN** matched target首份合法sample进入lifecycle
- **THEN** Lifecycle MUST把target的ResolvedAnimationPoseRequest push到正式PoseSlot Stack
- **AND** MarkerSyncRuntime MUST不写入transition progress或source weight

#### Scenario: source retirement由Stack exact completion确认

- **WHEN** source逻辑ownership已释放但Stack仍保留其entry
- **THEN** source MUST继续通过PresentationRetention提供animation-only sample与effective time
- **AND** relation MUST只在正式Retired后脱离

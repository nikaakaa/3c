# character-animation-layer-runtime Specification

## MODIFIED Requirements

### Requirement: 动画通道与Pose节点定义必须分离

有限Action Timeline、Semantic IR、Program producer contract、selection command与Playback Lifecycle MUST只使用稳定AnimationChannelId表达Gameplay已经完成的动作仲裁。持续Locomotion MUST由Presentation Fact、PoseStateMachine、Graph-owned Source Slot与Profile-owned Binding表达，不得拥有BaseLocomotion AnimationChannel。`CharacterAnimationPresentationProfile`引用的Pose Graph MUST唯一声明Fact Input、PoseStateMachine、ActionPlaybackInput、AnimationSlot、Player、组合、world-aware节点与Output topology；Marker时间映射 MUST只属于PoseState Transition或AnimationSlot的source-local plan，Transition与Inertialization Policy MUST由明确Pose transition owner拥有。Projection Compiler MUST把Pose source降低为Projection-local dense index，并把Action producer、AnimationChannel、PoseNode、compiled Pose Plan、Policy与Rig编入target-neutral Projection。Runtime MUST不读取Source Slot、Binding、旧PoseSlot、旧BaseLocomotion channel、Layer catalog、Animancer layer index或Profile layer order。

#### Scenario: Locomotion要求持续输出

- **WHEN** Corin进入Presentation且当前Body Fact合法
- **THEN** Locomotion PoseStateMachine MUST拥有明确active state与ready、pending或invalid source状态
- **AND** 系统 MUST不等待BaseLocomotion Selection Input

#### Scenario: FullBodyAction允许为空

- **WHEN** FullBodyAction channel提交None
- **THEN** AnimationSlot MUST透传同帧Source Pose
- **AND** MUST不创建fallback clip、默认Idle或第二条locomotion路径

#### Scenario: Action command引用未知channel

- **WHEN** committed Action producer command的AnimationChannelId不存在或没有精确Slot/Input binding
- **THEN** Program与Projection组合校验 MUST报告配置错误
- **AND** 对应command MUST不进入Lifecycle、Slot或Pose Graph

### Requirement: 基础姿态必须由正式来源输出

Base Pose、Idle、Move、Start、Stop、Turn与其它持续Locomotion姿态 MUST来自Pose Graph中PoseStateMachine选择的SequencePlayer、BlendSpacePlayer或Motion Matching source。Gameplay Program、Timeline、`CharacterActionPlaybackRuntime`与AnimationChannel MUST不提供隐藏BaseLocomotion producer。Required PoseState source缺失、binding invalid或target首Pose不可用时 MUST报告typed invalid，MUST不回退旧Timeline、默认Idle或bind pose。

#### Scenario: 首次进入Idle

- **WHEN** PoseStateMachine Entry选择Idle且Idle source binding合法
- **THEN** Idle SequencePlayer MUST生成首份Base Pose
- **AND** Runtime MUST不等待BaseLocomotion AnimationSelectionFrame

#### Scenario: Locomotion source缺失

- **WHEN** active State的Player引用未编译进当前Projection的dense source index
- **THEN** Projection Build或Runtime preparation MUST失败
- **AND** MUST不恢复旧Locomotion Timeline producer

### Requirement: 角色管线不依赖旧动画播放路径

角色管线 MUST共用唯一正式Pose Plan：Program提交committed Body/Intent和有限Action playback，Presentation构造Fact、求值PoseStateMachine、采样Pose source、消费Action Timeline visual sample、执行AnimationSlot与下游Pose Graph。Timeline Editor Preview MUST只预览有限Action Timeline；Pose Graph Preview MUST预览PoseState与Slot组合。系统 MUST不读取旧AnimationPresentationPolicySO、旧locomotion/action SO、旧bodyclaim policy，也 MUST不依赖旧BaseLocomotion Timeline、TimelinePlayer autonomous playback、Animator.Play、Animator.CrossFade或独立PlayableGraph作为另一权威。

#### Scenario: 搜索旧直接播放入口

- **WHEN** 实现阶段发现角色运行路径仍直接调用旧动画播放入口或提交BaseLocomotion selection
- **THEN** 该引用 MUST删除或迁移到正式PoseState、Action Playback或source backend职责
- **AND** 系统 MUST不保留兼容分支

#### Scenario: 编辑器预览动画

- **WHEN** 作者预览Action Timeline或Locomotion PoseState
- **THEN** 对应工作区 MUST复用正式Projection、source backend、Transition Routing与Pose Plan
- **AND** Preview MUST不创建独立仲裁器或PlayableGraph权威

### Requirement: 循环动画必须由连续 visual Timeline time 重采样

循环Action producer的continuous visual time MUST由committed Timeline logic sample、cycle identity与PresentationFrame interpolation计算。循环与有限Pose source MUST由SequencePlayer、BlendSpacePlayer或Motion Matching source在Presentation workspace推进，MUST不读取Gameplay Timeline或MotionCurve sample。Action Playback Lifecycle MUST只关联有限Action与其它明确Gameplay-owned有限playback；PoseState relevance MUST只关联SequencePlayer、BlendSpacePlayer与Motion Matching state-local source。任一source被Slot、BlendStack或State transition保留时 MUST继续animation-only sampling，且 MUST不执行Gameplay operation。

#### Scenario: 循环Action回绕

- **WHEN** committed Action loop sample从末尾回绕到开头
- **THEN** AnimationTrack MUST使用连续visual time重采样同一playback generation

#### Scenario: 循环Pose source回绕

- **WHEN** Run SequencePlayer跨过Cyclic source duration
- **THEN** Player MUST保留连续cycle identity并按绑定source重采样
- **AND** MUST不推进或创建Gameplay Timeline

#### Scenario: Source 已停止

- **WHEN** Action Gameplay ownership已release但Slot仍Retained该source
- **THEN** Presentation MAY继续animation-only sample
- **AND** TreeClip、Motion、Window与Cue fact MUST不再产生

### Requirement: 动画片段 membership 必须显式提交和释放

有限Action Timeline producer MUST显式提交Action Select、Sample、Complete与Release command。进入或继续处于有效Action Animation Clip时 MUST提交`ActionAnimationPlaybackCommand`与committed raw sample；离开ExtraPolationMode=None片段、playback失败或producer正式销毁时 MUST提交Release。Pose source MUST通过PoseStateMachine compiled relevance、Sequence/BlendSpace player readiness和Transition release管理，不得伪造Timeline membership。`CharacterActionPlaybackRuntime` MUST不因当帧缺少Action sample自动释放Selected或Retained source，也 MUST不因历史Action frame存在而把无效Pose target当作ready。

#### Scenario: Action None片段结束但Timeline继续

- **WHEN** Action Timeline时间已经超过ExtraPolationMode=None的AnimationClip EndTime
- **THEN** producer MUST对该Action clip membership提交Release
- **AND** 后续sample MUST不继续包含该历史clip

#### Scenario: Pose State离开但transition继续

- **WHEN** source State不再active但仍对State transition输出Pose
- **THEN** PoseState relevance MUST保持其source membership直到compiled release
- **AND** `CharacterActionPlaybackRuntime` MUST不创建对应Gameplay playback

### Requirement: 动画通道输入必须是已解析Animation Selection与正式参数页

Character Animation Runtime MUST在每个PresentationFrame按固定顺序消费`CharacterPresentationFactFrame`、求值PoseStateMachine、采样active State source、消费Action exact playback、执行Slot、Transition Routing、Pose composition、FootPlacement和Final publication。Action Timeline visual sampler MUST只生成Action playback；PoseStateMachine MUST只读取Fact与自己的workspace。两者 MUST不重新仲裁对方的选择。

#### Scenario: 全身攻击期间角色减速到零

- **WHEN** FullBodyAction Slot权重为1且Body速度在攻击期间变为零
- **THEN** Locomotion PoseStateMachine MUST根据新Fact更新到Idle或Stop目标
- **AND** Attack结束时Slot MUST回到该当前基础Pose

### Requirement: 动画播放生命周期必须只管理可见 producer 寿命

`CharacterActionPlaybackRuntime`中的Action Playback Lifecycle MUST只管理有限Action Timeline与其它明确Gameplay-owned有限playback的PendingFirstSample、Selected、Retained与Retired source。SequencePlayer、BlendSpacePlayer、Motion Matching与PoseStateMachine state relevance MUST使用各自compiled workspace，不得伪造Gameplay playback identity。Slot与Action Player完成后 MUST按exact action source usage释放Action资源；State transition完成后 MUST按state relevance释放持续Pose source。

#### Scenario: Action source完成淡出

- **WHEN** Slot完成Action到Source Pose的transition
- **THEN** Lifecycle MUST release该Action playback的Presentation retention
- **AND** MUST不改变Locomotion active State

#### Scenario: Sequence State离开

- **WHEN** PoseStateMachine完成从Start到Locomotion的transition
- **THEN** State runtime MUST按compiled relevance释放Start Sequence source
- **AND** MUST不向Gameplay提交Timeline release

### Requirement: 显式动画Player节点必须拥有各自时间连续性

`SequencePlayer`、`BlendSpacePlayer`与`SelectedPosePlayer` MUST只管理自身state-local source sample和discontinuity；`AnimationSlot` MUST管理Source Pose与有限Action playback之间的插入和source retirement；显式`BlendStack` MUST只管理直接连接的多source entry；`Inertialization` MUST独占completed Pose history、residual与rebase。项目 MUST不为旧BaseLocomotion AnimationChannel、旧PoseSlot、Provider或Output自动创建隐藏Player、Stack、StateMachine、Slot或Inertialization。

#### Scenario: PoseState切换Sequence source

- **WHEN** PoseStateMachine从Start切换到Locomotion
- **THEN** State transition MUST按compiled Blend Logic管理两侧State Pose
- **AND** SequencePlayer MUST不创建私有CrossFade或Gameplay release

#### Scenario: Standard Blend期间target再次切换

- **WHEN** PoseStateMachine正在从source向target执行Standard Blend，且最新Presentation Fact命中target State的另一条出边
- **THEN** target MUST已经是逻辑active State并从其编译Transition Rule选择新Transition
- **AND** 新Transition MUST通过唯一Transition Routing替换旧实例并从当前最终混合Pose接管
- **AND** 旧source与target MUST保持共同ready直到旧混合完成或替换事务成功
- **AND** 系统 MUST不等待旧Standard Blend完成后再消费该Fact，也 MUST不建立第二层transition stack

#### Scenario: Action Slot被连续打断

- **WHEN** FullBodyAction Slot从Attack切换到Dodge
- **THEN** Slot MUST按node-local rule管理Action source handoff
- **AND** Locomotion PoseStateMachine MUST不保存该Action transition

### Requirement: 同组 producer handoff 必须按 Marker Segment 映射

当两个共同可见source均声明MarkerGroup和相同canonical SyncGroupId时，Animation Runtime MUST按两侧SyncRole解析唯一leader/follower，并按leader effective time所在的有向MarkerId pair与segment fraction映射follower source time。Action exact playback MUST从`CharacterActionPlaybackRuntime`取得source usage；PoseState source MUST从State transition workspace取得source usage。Runtime MUST不使用Gameplay State名称、Pose State显示名、Clip名称、Action名称、priority或blend weight推导同步。

#### Scenario: Walk State切换Run State

- **WHEN** Walk与Run Presentation Pose source共享Locomotion.Gait且Transition启用State Source Sync
- **THEN** Runtime MUST读取Walk当前effective marker segment
- **AND** MUST在Run相同有向pair中计算target sample time

#### Scenario: Attack切换Dodge

- **WHEN** Action Slot两侧exact playback显式加入同一有限SyncGroup
- **THEN** Runtime MUST从Slot action source usage建立relation
- **AND** MUST不读取Locomotion PoseState relation

### Requirement: Marker Sync 必须在共同可见期间持续求值

Marker Sync MUST在source与target共同可见的每个PresentationFrame重新使用leader effective marker segment求follower effective time，不得只在target首样本保存固定phase offset。Action target MUST以mapped time重新采样Timeline producer内部clip membership；PoseState target MUST以mapped time重新采样Sequence或BlendSpace source。Gameplay Timeline sample、Pose transition Rule与World Body MUST保持不变。

#### Scenario: 不同时长Walk与Run transition

- **WHEN** Walk与Run SequencePlayer在State transition期间共同可见
- **THEN** Run MUST每个PresentationFrame持续对齐Walk marker fraction
- **AND** Transition Rule和Gameplay movement MUST不等待marker边界

#### Scenario: 多clip Action target

- **WHEN** Action target在mapped time采样到两个重叠clip
- **THEN** 两个clip MUST由同一个effective Timeline time重新采样
- **AND** Marker Sync MUST不选择某个clip作为第二phase authority

### Requirement: Finite 与 Cyclic producer 必须使用明确拓扑映射

Runtime MUST支持Cyclic与Finite source之间的显式同组映射。Presentation Pose source的topology MUST来自Profile binding；Action producer topology MUST来自Timeline AnimationTrack binding。Cyclic source MAY按duration回绕并保持展开cycle；Finite source MUST不回绕，target occurrence MUST单调前进。多个相同pair occurrence MUST按与raw target time最小距离选择并稳定破同。

#### Scenario: Cyclic Locomotion进入Finite Stop

- **WHEN** PoseState transition从Cyclic Run source进入同组Finite Stop source
- **THEN** Runtime MUST选择Stop中与当前raw time最近的兼容pair occurrence
- **AND** 后续共同可见帧 MUST沿Stop有限序列前进

#### Scenario: Finite Action覆盖耗尽

- **WHEN** relation要求Finite Action target前进到marker coverage之外
- **THEN** Runtime MUST报告FiniteCoverageExceeded
- **AND** MUST不回绕或静默解除同步

### Requirement: Sync relation 必须服从播放生命周期并连续脱离

Action sync relation MUST以完整AnimationPlaybackId为key并服从Selected、PendingFirstSample、Retained与Retired；PoseState sync relation MUST以PoseStateMachine、Transition generation和source player operation identity为key并服从state relevance。快速连续切换 MUST按真实relation依赖形成无环effective-time图。source正式release时Runtime MUST从target最后effective/raw time建立continuation anchor并删除relation；Reset、target release与Dispose MUST清理对应relation。

#### Scenario: PoseState连续A到B到C

- **WHEN** B仍跟随A时StateMachine又启动B到C
- **THEN** 当帧 MUST按`A effective -> B effective -> C effective`拓扑求值
- **AND** C MUST读取B的effective time

#### Scenario: Action source淡出完成

- **WHEN** Slot release permission使Action source进入Retired
- **THEN** target MUST建立continuation anchor
- **AND** 下一帧 MUST按raw delta连续推进

### Requirement: outgoing producer 必须使用纯表现 retention

有限Action逻辑producer release后，只要AnimationSlot或其它exact Player仍正式使用该playback，`CharacterActionPlaybackRuntime` MUST持有只读PresentationRetention并继续animation-only sample。PoseState source离开active state后，只要State transition仍共同可见，PoseStateMachine workspace MUST保持对应Presentation source relevance。两类retention MUST不恢复Gameplay membership，不运行TreeClip、Motion、Window或Cue，也 MUST在各自release完成后精确清理。

#### Scenario: Attack逻辑结束但Slot仍淡出

- **WHEN** Attack Gameplay已停止且Slot仍Retained该source
- **THEN** sampler MUST只推进Attack animation visual sample
- **AND** MUST不产生Gameplay fact

#### Scenario: Start State已切出但仍在transition

- **WHEN** active target已是Locomotion但Start Pose仍有blend贡献
- **THEN** State runtime MUST保持Start source relevance直到transition release
- **AND** MUST不创建Gameplay Timeline retention

### Requirement: source backend必须只负责采样

Action MarkerSync与PoseState Source Sync MUST只提供对应source的effective sample page与relation snapshot。Animancer source backend MUST只拥有source playable、Action producer内部clip membership采样和Pose source采样；PoseState Transition、AnimationSlot与显式BlendStack MUST分别唯一拥有自身transition clock、curve、Stored与release；局部Inertialization MUST唯一拥有residual与rebase；Pose Graph MUST唯一拥有组合与最终Pose。项目 MUST不新增第二套crossfade weight、Animancer automatic synchronization、managed evaluator或第二动画时钟。

#### Scenario: Action同步target开始播放

- **WHEN** matched Action target首份合法sample进入Lifecycle
- **THEN** Lifecycle MUST把target Selection发布到正式Slot/Player路径
- **AND** MarkerSync MUST不写入transition progress或source weight

#### Scenario: PoseState source retirement

- **WHEN** source State已离开active但Transition仍保留其Pose
- **THEN** backend MUST按State source usage继续提供animation-only sample
- **AND** State Source Sync relation MUST只在正式release后脱离

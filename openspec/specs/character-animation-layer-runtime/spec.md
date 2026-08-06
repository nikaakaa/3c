# character-animation-layer-runtime Specification

## Purpose

定义Presentation Fact、PoseState source、有限Action playback、显式transition owner、source backend和最终Pose之间的唯一角色动画运行链。

## Requirements

### Requirement: 持续Pose与有限Action控制边界必须分离

持续Locomotion MUST由committed Body/Intent构造`CharacterPresentationFactFrame`，再由PoseStateMachine选择state-local `PresentationPoseSourceSample`。只有有限Action Timeline与其它明确Gameplay-owned有限动画 MAY使用AnimationChannelId、Program producer、`ActionAnimationPlaybackCommand`和AnimationPlaybackId。Projection Compiler MUST把Pose source binding、Action producer binding、PoseNode、Routing Plan、Rig与固定Pose Plan编入同一target-neutral Projection。Runtime MUST不读取旧BaseLocomotion channel、旧PoseSlot、Layer catalog、Animancer layer index、Profile layer order或旧LayerId。

#### Scenario: Locomotion持续输出

- **WHEN** 当前Body Fact合法
- **THEN** PoseStateMachine MUST拥有明确active State与Pending、Ready或Invalid source状态
- **AND** 系统 MUST不等待BaseLocomotion Selection Input

#### Scenario: FullBodyAction为空

- **WHEN** FullBodyAction channel没有活动playback
- **THEN** AnimationSlot MUST透传同帧Source Pose
- **AND** MUST不创建fallback clip、默认Idle或第二条Locomotion路径

#### Scenario: Action command引用未知binding

- **WHEN** command的producer、channel或Slot binding不能精确匹配Projection
- **THEN** Program与Projection组合校验 MUST失败
- **AND** command MUST不进入Lifecycle或Pose Plan

### Requirement: 基础Pose必须由正式state-local source输出

Base Pose、Idle、Move、Start、Stop、Turn与可选Motion Matching MUST来自Pose Graph中PoseStateMachine选择的SequencePlayer、BlendSpacePlayer或SelectedPosePlayer provider。Gameplay Program、Timeline、Action Lifecycle与AnimationChannel MUST不提供持续BaseLocomotion producer。Required source缺失、binding无效或首Pose不可用时 MUST报告typed Pending或Invalid，不得回退旧Timeline、默认Idle、bind pose或历史sample。

#### Scenario: 首次进入Idle

- **WHEN** Entry选择Idle且source binding合法
- **THEN** Idle provider MUST向State Player发布Ready sample
- **AND** Runtime MUST不等待Gameplay Timeline

#### Scenario: Locomotion binding缺失

- **WHEN** active State引用缺失Source Slot、缺失Binding或Projection中不存在的dense source index
- **THEN** Projection Build或Runtime preparation MUST失败
- **AND** MUST不恢复旧BaseLocomotion producer

### Requirement: 动画帧必须按固定职责顺序执行

每个PresentationFrame MUST按固定顺序读取committed Body/Intent与Program parameter、构造Fact、求值PoseStateMachine、提交target provider demand、解析readiness、采样state-local source、消费有限Action frame、执行Transition Routing与AnimationSlot、执行Local Pose composition与Virtual Bone派生、显式转换到Component Pose、执行TwoBoneIK等Component Pose控制、由FootPlacement规划pelvis与typed双腿targets、由LegIK求解Physical腿链、显式转回Local Pose，最后发布FinalAnimationPoseFrame。Action visual sampler MUST只生成有限Action sample；PoseState provider MUST只处理其state-local source。任一阶段 MUST不重新仲裁其它阶段的选择或写回Gameplay。

#### Scenario: 攻击期间角色速度归零

- **WHEN** FullBodyAction Slot仍有完整权重但Body速度已经归零
- **THEN** PoseStateMachine MUST继续更新到Stop或Idle目标
- **AND** Action结束时Slot MUST回到当时的当前Source Pose

### Requirement: 有限Action Timeline必须显式提交和释放playback

Action producer MUST显式提交Select、Sample、Complete与Release command。进入或继续合法AnimationClip membership时 MUST提交匹配generation的committed raw sample；离开ExtraPolationMode=None片段、playback失败或producer销毁时 MUST提交terminal command。`CharacterActionPlaybackRuntime` MUST只管理有限playback的PendingFirstSample、Selected、Retained与Retired，以及其committed sample history和Slot binding。历史sample不得把无效target伪装为Ready。

#### Scenario: None片段结束

- **WHEN** Action Timeline已经超过ExtraPolationMode=None的clip EndTime
- **THEN** producer MUST提交Release
- **AND** 后续sample MUST不包含该历史clip

#### Scenario: Hold片段结束

- **WHEN** Action Timeline超过ExtraPolationMode=Hold的clip EndTime
- **THEN** AnimationTrack MUST继续提交正式Hold sample
- **AND** Hold MUST不来自Lifecycle或Presenter隐式补值

### Requirement: PoseState source必须按provider demand和state relevance管理

PoseStateMachine MUST只向相关State的显式source plan提交固定容量demand，并以Projection-local dense source index、PlayerNodeId、SourceGeneration、continuity identity和frame lease接收sample。Pending target MUST不启动transition，Ready target MAY进入Routing，Invalid MUST阻止正式publication。State离开active后只要transition仍需要其Pose，state relevance MUST保持source；release完成后 MUST精确清理。Pose source MUST不创建作者Source字符串、Gameplay PlaybackId或Action retention。

#### Scenario: Start State切向Locomotion

- **WHEN** Locomotion target Ready且transition仍共同显示Start
- **THEN** Start与Locomotion source MUST同时保持relevant
- **AND** transition完成后 MUST只释放Start source

### Requirement: 每类连续性必须只有一个明确owner

SequencePlayer、BlendSpacePlayer与SelectedPosePlayer MUST只管理自身source sample和discontinuity；PoseStateMachine Transition MUST拥有State到State的clock、blend和release；AnimationSlot MUST拥有Source Pose与Action source之间的handoff；显式BlendStack MUST只拥有自身连接source的entry、Stored Pose、dense per-bone blend和retirement；Inertialization MUST独占局部completed Pose history、residual与rebase。Runtime MUST不为AnimationChannel、Graph branch或Output自动创建隐藏Stack、StateMachine、Slot或全局Inertialization。

#### Scenario: PoseState连续切换

- **WHEN** A到B transition尚未结束又接受合法B到C切换
- **THEN** PoseState compiled transition policy MUST处理现有Pose历史
- **AND** MUST不把历史注入无关BlendStack

#### Scenario: Action连续打断

- **WHEN** Slot从Attack切换到Dodge
- **THEN** Slot MUST按node-local route处理handoff
- **AND** PoseStateMachine MUST不保存Action transition

### Requirement: Marker同步必须编入对应source-local计划

MarkerGroup binding MUST提供canonical SyncGroupId、topology、SyncRole、marker occurrence和duration。PoseState relation MUST以StateMachine、Transition generation和两侧Player operation identity为key；Action relation MUST以Slot、AnimationPlaybackId和source usage为key。Runtime MUST在source采样前按leader有向Marker pair与segment fraction生成effective sample，并在共同可见期间每帧持续求值。Pose Graph MUST不序列化MarkerSync节点，Runtime MUST不按State名、clip名、Action名、priority或weight推导relation。

#### Scenario: Walk切换Run

- **WHEN** Pose Transition两侧State的唯一同步候选source同组
- **THEN** Source Sync Plan MUST持续映射Walk effective segment到Run
- **AND** Gameplay movement MUST不等待marker边界

#### Scenario: Action同步binding损坏

- **WHEN** Slot要求同步但任一source缺少合法marker coverage
- **THEN** Runtime MUST报告稳定typed invalid
- **AND** MUST不退回normalized time或Animancer自动同步

#### Scenario: 两侧source没有共同MarkerGroup

- **WHEN** Compiler无法从两侧binding找到共同canonical MarkerGroup
- **THEN** 两侧source MUST使用各自raw time
- **AND** compiled plan MUST为None

### Requirement: Finite与Cyclic source时间必须保持明确拓扑

Runtime MUST支持Cyclic与Finite source之间的显式同组映射。Cyclic source MAY按duration回绕并维持展开cycle；Finite source MUST不回绕，target occurrence MUST单调前进。首次存在多个相同有向pair occurrence时 MUST按与raw target time的最小距离选择，并以稳定authoring identity破同；relation存活期间 MUST保持occurrence连续性。source正式release时 MUST以target最后effective/raw time建立continuation anchor，之后按raw delta连续推进。

#### Scenario: Run进入Finite Stop

- **WHEN** Run到Stop Transition启用同组同步
- **THEN** Runtime MUST选择Stop中最近的兼容pair occurrence
- **AND** 后续共同可见帧 MUST沿Stop有限序列前进

#### Scenario: Finite coverage耗尽

- **WHEN** relation要求Finite target越过marker coverage
- **THEN** Runtime MUST报告FiniteCoverageExceeded
- **AND** MUST不回绕或静默解除同步

### Requirement: Source retention和物理释放必须精确握手

有限Action逻辑producer release后，只要Slot仍正式使用该playback，Action Lifecycle MUST持有只读animation-only retention。PoseState source离开active后，只要Transition仍共同显示它，state relevance MUST保持provider source。两类retention MUST不运行TreeClip、Motion、Window、Cue或Gameplay operation。Consumer完成视觉使用后 MUST发布retirement permission；source backend完成物理资源释放后 MUST发布匹配identity和generation的completion；owner只有在两步完成后才能进入Retired并清理sample history。

#### Scenario: Attack逻辑结束但仍淡出

- **WHEN** Attack Gameplay membership已经释放而Slot仍保留Action Pose
- **THEN** sampler MUST只推进animation visual sample
- **AND** Lifecycle MUST等待Slot permission与backend completion

#### Scenario: Actor Dispose

- **WHEN** Presentation Runtime被Dispose
- **THEN** Action lifecycle、PoseState relevance、relation、continuation anchor与source backend资源 MUST全部清理
- **AND** MUST不发布伪造Gameplay terminal fact

### Requirement: Source backend必须只负责采样和物理资源释放

Animancer source backend MUST只按完整Action playback或Presentation Pose source identity创建、复用和释放source playable，采样producer内部clip membership或state-local source，并把capture job安装到同一PlayableGraph。它 MUST不拥有Gameplay/PoseState仲裁、跨source transition weight、AnimationSlot、Inertialization、Pose composition、IK或Final writer。每个表现帧 MUST只执行一次正式Pose Plan和一次PlayableGraph Evaluate。

#### Scenario: Transition同时采样两侧source

- **WHEN** State或Slot transition要求两个source共同可见
- **THEN** backend MUST分别提供两个source capture
- **AND** transition weight MUST只由对应owner计算

### Requirement: Float32与Fixed必须共享同一Presentation Projection

由同一SemanticHash和producer contract生成的Float32 Program与Fixed Program wrapper MUST引用同一套Presentation Projection、Pose source binding、Action binding、Pose Plan、Routing Plan和Rig revision。Runtime MUST不按ProgramHash复制、选择或降级Projection。任一Program、Projection、Rig或authoring revision不匹配 MUST在preparation阶段失败。

#### Scenario: 构建Fixed wrapper

- **WHEN** Fixed Program由当前Definition和Float32 Program生成
- **THEN** wrapper MUST保留同一SemanticHash与Presentation contract
- **AND** MUST不生成第二套动画Projection

### Requirement: Runtime、Preview和Live Debug必须使用同一事实源

正式Runtime、Action Timeline Preview、Pose Graph Fact Preview、MM Query Fixture和Live Debug MUST复用匹配revision的Projection、source backend、Routing Plan、Pose Plan与completion语义。Preview入口 MUST分别只提交Action command、Presentation Fact或state-local query fixture。Diagnostics MUST按Action playback identity或Provider/Player/Source/generation显示各自生命周期、effective sample、transition、release和Pose contribution；不得从Animancer weight或Animator骨骼反推第二份事实。

#### Scenario: Projection变为Stale

- **WHEN** authoring revision变化而Projection尚未显式Build
- **THEN** Preview与Runtime preparation MUST停止
- **AND** MUST不创建临时Plan、旧Projection fallback或独立PlayableGraph

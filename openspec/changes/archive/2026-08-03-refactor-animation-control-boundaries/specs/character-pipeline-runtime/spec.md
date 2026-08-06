# character-pipeline-runtime Specification

## MODIFIED Requirements

### Requirement: Timeline 和动画 tick 权威归属 pipeline

Gameplay Timeline logic time MUST归Program与CharacterSimulationState并按SimulationTick推进；每个有限Action Sample command MUST只表达committed playback identity、raw visual time、cycle与time scale锚点，MUST不表达最终骨骼Pose或要求Player只在SimulationTick推进。有限Action projected visual time、PoseStateMachine、Sequence/BlendSpace/MM source clock、AnimationSlot、显式Player、Animancer source sampling与Pose Plan evaluation MUST归PresentationFrame。Pipeline Runtime MUST通过committed Body/Intent构造Presentation Fact，并通过有限Action producer/playback identity连接Timeline与Slot。Program MUST不读取PoseState、SequencePlayer、Slot或Pose Graph时间，Presentation MUST不推进Gameplay Timeline。系统 MUST不提供让同一Timeline在Gameplay-owned与Presentation-owned时钟之间切换的运行模式；有限Action与持续Pose source MUST由其正式authoring owner进入唯一对应链路。

#### Scenario: 无新Logic Tick的RenderFrame

- **WHEN** PresentationFrame到达但没有新SimulationTick
- **THEN** Action visual time projector、PoseState source、Slot transition、Player与Pose Graph MAY按presentation delta继续推进
- **AND** Timeline Gameplay state与Action lifecycle MUST不改变

#### Scenario: 新Action逻辑Sample到达

- **WHEN** 同一Action playback identity收到下一份committed raw sample
- **THEN** Action表现投影 MUST按该锚点重基线并继续在PresentationFrame采样
- **AND** MUST不把到达前的projected time写回Timeline logic time

#### Scenario: 作者选择持续Locomotion播放

- **WHEN** 作者需要Idle、Walk或Run持续Pose
- **THEN** 该动画 MUST通过PoseStateMachine内的Sequence、BlendSpace或MM source按presentation delta推进
- **AND** MUST不通过Timeline TimingMode创建第二种Action playback语义

### Requirement: Program Finalize 必须提交逻辑侧唯一动画选择

Program Finalize MUST在State、Action、interruption与Timeline request处理后，为每个有限Gameplay-owned `AnimationChannelId`最多产生一个selected producer/playback command。持续BaseLocomotion MUST不再是Program animation channel；其表现输入 MUST来自committed Body/Intent的Presentation Fact。Committer、Projection、Slot与Pose Graph MUST不重新仲裁同一Action channel候选，Program MUST不读取PoseStateId、PoseNodeId、Bone Mask、Slot或Pose Graph topology决定winner。

#### Scenario: FullBodyAction所有权冲突

- **WHEN** Program无法为FullBodyAction channel产生唯一Action selection
- **THEN** 当前Tick MUST报告明确冲突
- **AND** Slot MUST不选择默认赢家

#### Scenario: Locomotion与Dodge并行

- **WHEN** Body正在移动且FullBodyAction选择Dodge
- **THEN** Program MUST提交Dodge command和普通Body结果
- **AND** Presentation MUST先求值Locomotion PoseStateMachine再由Slot组合Dodge

### Requirement: 角色管线必须保留跨 logic tick 的动画生命周期命令

SimulationCommitter MUST使用presentation-owned持久队列保存未消费的有限Action producer selection、sample、complete、release与EventId lifecycle。Queue MUST独立于transient Tick result，并按SimulationTick、event sequence与playback generation保序；queue MUST不保存Character/World mutable state。持续Locomotion PoseState、source relevance和transition MUST只存在于Presentation workspace，不得写入该Gameplay command queue。

#### Scenario: 一个PresentationFrame前多个SimulationTick

- **WHEN** Committer连续提交多个Action generation
- **THEN** queue MUST保留Complete与Release顺序直到Presentation acknowledge
- **AND** MUST不为Body速度变化追加Run或Idle animation command

### Requirement: PresentationFrame必须输出完整最终Pose Plan结果

PresentationFrame MUST消费committed Body/Intent、构造typed Presentation Fact，并消费完整有限Action Selection batch与Parameter page；随后按Projection编译顺序执行PoseState selection、State source sampling、Action playback、Marker time resolve、AnimationSlot、Transition Routing、native composition、world-aware postprocess与Output阶段。只有唯一OutputPose及全部必需阶段完成后才可发布`FinalAnimationPoseFrame`并推进Camera；任一Fact、PoseState、Action sample、MarkerSync、Player、Slot、Pose operation、FootPlacement或Solver失败 MUST阻止部分最终结果发布，不得沿用上一帧或绕过节点。

#### Scenario: Action等待第一Selection sample

- **WHEN** Program已经选择Action但Presentation尚无合法Action sample
- **THEN** AnimationSlot MUST按compiled pending/availability policy处理
- **AND** Locomotion PoseState MUST继续来自同帧Fact而不是历史BaseLocomotion selection

#### Scenario: Locomotion Pose source不可用

- **WHEN** active PoseState的Required source尚未ready
- **THEN** Pose Plan MUST报告typed availability结果
- **AND** MUST不回退上一帧Pose、默认Idle或Action source

### Requirement: PresentationFrame必须原子提交动画播放与Pose节点生命周期

PresentationFrame MUST在同一外层事务中提交Presentation Fact page、PoseStateMachine active/target state、Sequence/Selection source usage、Marker relation/effective sample page、AnimationSlot state、BlendStack状态、Transition Routing capture/release、Inertialization、Pose operation completion、world-aware plan、Solver结果和final publication。Reset、branch replacement、Projection replacement或失败 MUST按编译Plan逆序清理全部stateful节点；不得只提交Action playback或State transition而保留旧Output。

#### Scenario: Action Selection与首个Sample同批

- **WHEN** 新Action Selection与首份合法source sample在同一PresentationFrame到达
- **THEN** Slot MUST原子初始化并参与本帧Pose Plan
- **AND** FinalAnimationPoseFrame MUST只反映完整事务结果

#### Scenario: Fact generation发生重置

- **WHEN** Body correction提升Presentation Fact generation
- **THEN** PoseStateMachine与下游Inertialization MUST按compiled reset顺序处理
- **AND** MUST不恢复旧BaseLocomotion Selection cache

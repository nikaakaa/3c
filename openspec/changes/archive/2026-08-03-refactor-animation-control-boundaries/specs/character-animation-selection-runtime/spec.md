# character-animation-selection-runtime Specification

## MODIFIED Requirements

### Requirement: Gameplay与搜索层必须只提交Animation Selection

Action Timeline MUST向`CharacterActionPlaybackRuntime`提交版本化`ActionAnimationPlaybackCommand`；Motion Matching Module MAY只向当前PoseState内部Player提交state-local `PresentationPoseSourceSample`。Action Playback MUST表达Gameplay已经确认的有限Action producer、source、generation和committed raw sample；Motion Matching sample MUST表达PoseState内部搜索得到的Projection-local dense source index、PlayerNodeId、source generation、frame lease和pose time，MUST不进入Action Playback生命周期或创建`AnimationPlaybackId`。BTSMTL Program MUST不为持续BaseLocomotion提交Idle、Walk、Run、Start、Stop或Turn具体Selection。PoseStateMachine MUST从Presentation Fact选择持续Pose State。Gameplay已经提交Motion后，Presentation Fact MUST携带不含Action owner的稳定Movement Mode作者身份；PoseStateMachine MUST不得按速度阈值猜测Walk与Run，也不得按Facing Error独立猜测Gameplay MovingTurn。

#### Scenario: Gameplay从静止开始移动

- **WHEN** Program接受移动输入并更新Body/Intent
- **THEN** Presentation MUST收到typed movement fact
- **AND** movement fact MUST包含已提交Locomotion或Gameplay Result所属State的稳定作者身份
- **AND** MUST不收到WalkStart或RunLoop Gameplay Animation Selection

#### Scenario: MovingTurn已经由Gameplay提交

- **WHEN** Gameplay StateMachine提交MovingTurn的Motion Curve结果
- **THEN** Presentation MUST以该提交结果所属State身份进入Turn Pose State
- **AND** MUST不通过Facing Error重新判断Gameplay是否处于MovingTurn

#### Scenario: Action Timeline播放Attack1

- **WHEN** Gameplay已激活Attack1并推进其Timeline
- **THEN** Presentation MUST收到Attack1 exact Action Selection
- **AND** Slot MUST不重新仲裁Attack1与其它Action候选

#### Scenario: Motion Matching在Locomotion State选择新姿势

- **WHEN** Locomotion PoseState中的MM provider选择新pose
- **THEN** Module MUST向该State的显式Player提交普通Selection generation
- **AND** MUST不成为BTSMTL AnimationChannel gameplay winner

### Requirement: Pose Graph必须显式选择Animation Player

`SequencePlayer` MUST直接消费Presentation Pose Source binding；`SelectedPosePlayer`、`BlendSpacePlayer`与`BlendStack` MUST只消费连接的exact Selection provider；`AnimationSlot` MUST消费Action Playback并透传Source Pose。各节点 MUST只管理自己的source usage、sample与已编译连续性语义。Compiler与Runtime MUST不把SequencePlayer包装成虚假Timeline Selection，也 MUST不把Action Selection送入Locomotion PoseState rule。

#### Scenario: Idle SequencePlayer运行

- **WHEN** PoseStateMachine保持Idle
- **THEN** SequencePlayer MUST按Presentation时间采样Idle source
- **AND** `CharacterActionPlaybackRuntime` MUST不要求对应Gameplay producer

#### Scenario: Action Slot切换Attack1到Attack2

- **WHEN** Action exact Selection generation变化
- **THEN** Slot MUST按其node-local rule处理source handoff
- **AND** Locomotion State transition MUST不参与该Action切换

### Requirement: Marker时间映射必须属于source-local采样计划

Marker topology、SyncGroup、SyncRole与marker occurrence MUST来自Presentation source binding或Action producer binding。PoseState source同步 MUST由具体Transition的Source Sync Plan拥有；Action同步 MUST由具体AnimationSlot route和Action source usage拥有。Runtime MUST在source采样前生成effective sample，并在共同可见期间持续按有向Marker pair和segment fraction求值。Pose Graph MUST不序列化独立MarkerSync节点，Runtime与Preview MUST不在图外扫描同名State、clip名称、Action名称或weight自动建立relation。

#### Scenario: Action source同步

- **WHEN** FullBodyAction Slot route显式启用MarkerGroup同步
- **THEN** Slot的source-local sync plan MUST按Action producer binding解析effective time
- **AND** Slot MUST不复制第二套marker算法或创建MarkerSync节点

#### Scenario: Walk Pose State切换Run Pose State

- **WHEN** Transition edge显式启用State Source Sync且两侧source属于Locomotion.Gait
- **THEN** compiled sync plan MUST在target采样前解析effective time
- **AND** MUST不创建BaseLocomotion Animation Selection

### Requirement: Source usage、retention与release必须由实际consumer闭环

PoseState transition MUST按state relevance保留共同可见source；AnimationSlot和显式BlendStack MUST按自身source usage保留Action或exact source。Action lifecycle MUST只管理有限playback的PendingFirstSample、Selected、Retained与Retired；Pose source MUST使用Projection-local dense source index、PlayerNodeId、provider generation与SourceGeneration，不得读取作者Slot对象或伪造Action lifecycle。transition或Slot完成后，consumer MUST先发布typed retirement permission，source backend完成物理释放后再发布completion，owner才能最终清理资源。

#### Scenario: Action Slot保留Attack并接收Dodge

- **WHEN** Slot source usage包含Retained Attack与incoming Dodge
- **THEN** Slot MUST只保留这两个Action playback的source usage
- **AND** Slot MUST独立计算Blend Logic并发布source retirement permission

#### Scenario: Locomotion State transition同步

- **WHEN** Walk与Run State player共同可见
- **THEN** State Source Sync Plan MUST只读取两侧Presentation source usage
- **AND** `CharacterActionPlaybackRuntime` MUST不为它们创建Gameplay playback retention

### Requirement: Blend Stack节点必须独占自身时间连续性

每个编译后的显式BlendStack节点 MUST拥有唯一runtime identity、active player顺序、CrossFade clock、Stored Pose、Per-Bone Blend Profile、source retention与exact release。节点 MUST只消费连接到自身的exact Selection source与node-local Blend Policy，输出普通Pose Value。PoseState transition与AnimationSlot MUST拥有各自独立的transition workspace，不得借用全局或每channel隐藏BlendStack。BlendStack MUST不读取Gameplay State、PoseState transition Rule、Motion Matching query、下游Bone Mask、Foot Placement或Output topology。

#### Scenario: Action exact source连续A到B到C

- **WHEN** 显式BlendStack在A到B期间收到C Selection
- **THEN** 节点 MUST按编译Policy保留或压缩当前历史并开始到C的连续过渡
- **AND** MUST不要求Gameplay重新提交A或B

#### Scenario: PoseState发生连续切换

- **WHEN** PoseStateMachine在一条Transition尚未结束时接受合法高优先级切换
- **THEN** 必须由PoseState compiled transition policy处理当前Pose历史
- **AND** MUST不把状态历史注入无关BlendStack

### Requirement: Blend Policy必须按节点物化完整transition

每个transition owner MUST引用唯一合法Policy。AnimationSlot与保留的exact Selection BlendStack MUST枚举全部可达Action/Selection endpoint，并把authoring default与exact override物化为完整source-target/`SourcePoseEndpoint` table。每条PoseState Transition edge MUST只物化自身source-target Blend Logic，不得复制Slot完整表。Compiler MUST把canonical curve与dense Blend Profile编入Projection；Runtime MUST只按稳定owner和endpoint exact lookup，缺失pair、重复override、未知source或Rig不匹配 MUST失败且不得fallback。

#### Scenario: Action Slot缺少Attack到Source Pose规则

- **WHEN** Compiler发现FullBodyAction Slot可达Attack与SourcePoseEndpoint但无法物化合法pair
- **THEN** Projection Build MUST失败并定位Slot与endpoint

#### Scenario: PoseState edge配置Blend

- **WHEN** 作者为Start到Locomotion edge选择Standard Blend
- **THEN** Compiler MUST只把该edge的duration、curve与Blend Profile写入对应transition operation
- **AND** MUST不生成全局State pair table

### Requirement: Selection Preview必须执行正式Pose Plan

有限Action Timeline Preview与Motion Matching Query Fixture MUST把Editor输入降低为正式exact Selection并执行匹配Projection；Locomotion PoseState Preview MUST由Pose Graph Workspace使用正式Fact、PoseStateMachine、source player、Transition Routing与Pose Plan。AnimationSlot、BlendStack与Inertialization存在时Preview MUST复用正式workspace、history、capture与release语义。Preview MUST不创建BaseLocomotion Timeline Selection、简化Player、固定per-slot隐藏Stack、全局惯性器、临时PlayableGraph或Animancer direct Play路径。

#### Scenario: Timeline Preview seek到另一个Action producer

- **WHEN** 作者在Action Preview中非连续seek
- **THEN** Preview MUST按正式Slot、Player与reset policy更新Pose
- **AND** MUST不为了平滑预览后台插入BlendStack

#### Scenario: PoseState Preview改变速度Fact

- **WHEN** 作者在Pose Graph Preview把HorizontalSpeed从零改为移动值
- **THEN** 正式Transition Rule MUST驱动Idle到Locomotion
- **AND** Preview MUST不发送PlayRun Gameplay事件

### Requirement: Animancer必须只负责source采样

Animancer source backend MUST只按完整Action producer或Presentation Pose source identity创建或复用Sequence/ManualMixer playable，应用对应effective sample time、loop、play rate与source-local clip weight并提供Pose capture。它 MUST不仲裁AnimationChannel winner、不求值PoseState Transition Rule、不查询Blend Policy、不拥有跨source weight、不执行Slot、Layer composition或FootPlacement，也 MUST不发布最终Pose。

#### Scenario: PoseState transition同时采样Walk与Run

- **WHEN** Transition要求两侧Sequence source共同可见
- **THEN** Animancer MUST分别提供两个source Pose capture
- **AND** source间weight MUST只由PoseState transition计算

#### Scenario: Slot切换Action source

- **WHEN** FullBodyAction Slot要求同时采样Attack与Dodge
- **THEN** Animancer MUST分别采样两个Action source
- **AND** transition与release MUST只由Slot和正式Lifecycle决定

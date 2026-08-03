# character-animation-selection-runtime Specification

## Purpose

定义持续Pose source、有限Action playback、显式Player、source-local时间映射、连续性、释放和Preview之间的唯一表现边界。

## Requirements

### Requirement: 持续Pose source与有限Action必须使用不同ABI

持续Idle、Start、Move、Stop、Turn和Motion Matching MUST由当前Pose State内部source plan发布`PresentationPoseSourceSample`。有限Action Timeline MUST向`CharacterActionPlaybackRuntime`提交`ActionAnimationPlaybackCommand`。Pose sample MUST携带Projection-local dense source index、PlayerNodeId、SourceGeneration、continuity identity、frame lease、source kind与sample payload；Action command MUST携带正式Program producer、AnimationChannel、AnimationPlaybackId和committed Timeline sample。两条ABI MUST不相互伪装，持续Pose source MUST不携带作者Slot/Binding对象或source字符串，也 MUST不创建Gameplay producer、AnimationChannel winner或AnimationPlaybackId。

#### Scenario: 角色从静止进入移动

- **WHEN** committed Body与Intent使PoseStateMachine选择移动State
- **THEN** 对应state-local provider MUST发布该State Player的Pose source sample
- **AND** Gameplay Program MUST不提交Walk或Run Animation Selection

#### Scenario: Attack Timeline开始播放

- **WHEN** Gameplay已经激活有限Attack Action并推进其Timeline
- **THEN** Action runtime MUST收到匹配producer与generation的Action playback command
- **AND** PoseState provider MUST不承接该Action生命周期

#### Scenario: Motion Matching选择新姿势

- **WHEN** 当前Pose State中的Motion Matching provider完成查询
- **THEN** provider MUST向该State绑定的显式Player发布新的source generation
- **AND** MUST不成为Gameplay animation channel winner

### Requirement: Pose source readiness必须显式表达Pending、Ready与Invalid

每个state-local provider demand MUST返回`Pending`、`Ready`或`Invalid`。`Ready` MUST包含合法raw/effective sample、clip plan、typed parameter page和可选Foot Feature；`Pending`与`Invalid` MUST不携带可采样payload，`Invalid` MUST带稳定failure reason。PoseStateMachine只有在target为Ready时才可提交transition generation；已有合法source时Pending target MUST保持当前source，Entry target Pending时 MUST不发布Final Pose，Invalid MUST终止该帧正式publication。Runtime MUST不以旧sample、bind pose、默认Idle或Action playback补洞。

#### Scenario: MM首个查询尚未完成

- **WHEN** Transition Rule已经选中MM State但provider返回Pending
- **THEN** PoseStateMachine MUST保持现有合法State输出
- **AND** MUST不启动target transition

#### Scenario: Entry source binding无效

- **WHEN** Entry State provider返回SourceBindingMissing
- **THEN** 该表现帧 MUST报告Invalid
- **AND** MUST不发布历史Pose或默认Clip

### Requirement: Source请求工作区必须按表现帧租约复用

Pose source request、sample、clip、parameter和completion row MUST只属于当前表现帧。`BeginFrame` MUST使上一帧租约失效并回收固定容量；Projection容量 MUST表示单帧最大并发source数量，不得按历史generation持续增长。异步或延迟completion MUST同时匹配Projection-local dense source index、PlayerNodeId、SourceGeneration和frame lease，任一不匹配 MUST拒绝。

#### Scenario: 连续产生多个source generation

- **WHEN** 同一Player在不同表现帧依次选择多个姿势
- **THEN** 每帧 MUST只占用当帧实际解析的source row
- **AND** 旧frame completion MUST不能写入新generation

### Requirement: Pose Graph必须显式选择source Player和transition owner

`SequencePlayer`、`BlendSpacePlayer`、`SelectedPosePlayer`和`BlendStack` MUST只消费自身绑定或连接的state-local source。`PoseStateMachine` MUST唯一拥有State到State的transition workspace；`AnimationSlot` MUST唯一拥有Source Pose与有限Action playback之间的插入和handoff；显式`BlendStack` MUST只拥有其连接source的多entry连续性；`Inertialization` MUST只拥有直接上游Player或transition consumer的residual与rebase。Compiler与Runtime MUST不在Provider、AnimationChannel、Graph branch或OutputPose背后自动插入Player、Stack、Slot或Inertialization。

#### Scenario: Idle使用SequencePlayer

- **WHEN** PoseStateMachine保持Idle State
- **THEN** SequencePlayer MUST按Presentation时间采样正式source binding
- **AND** Action playback lifecycle MUST不创建对应producer

#### Scenario: Action Slot从Attack1切换到Attack2

- **WHEN** Slot收到新的Action playback generation
- **THEN** Slot MUST按node-local compiled route处理handoff
- **AND** Locomotion State transition MUST不保存该Action历史

#### Scenario: MM Player连接局部Inertialization

- **WHEN** Motion Matching sample发生source discontinuity
- **THEN** Player MUST发布typed discontinuity
- **AND** residual MUST只由连接的Inertialization拥有

### Requirement: Marker时间映射必须属于source-local采样计划

Marker topology、SyncGroup、SyncRole与marker occurrence MUST来自Presentation source binding或Action producer binding。PoseState source同步 MUST由Compiler根据Transition两侧State唯一的同步候选与共同canonical MarkerGroup生成具体Source Sync Plan；Transition不得保存同步开关。Action同步 MUST由具体AnimationSlot route和Action source usage拥有。Runtime MUST在source采样前生成effective sample，并在共同可见期间持续按有向Marker pair和segment fraction求值。Pose Graph MUST不序列化独立MarkerSync节点，Runtime与Preview MUST不按同名State、clip名称、Action名称或weight建立relation。

#### Scenario: Walk State切换Run State

- **WHEN** Transition两侧State的唯一同步候选属于同一canonical SyncGroup
- **THEN** Source Sync Plan MUST持续把leader segment fraction映射到target sample
- **AND** MUST不创建BaseLocomotion Gameplay Selection

#### Scenario: Transition两侧没有共同同步组

- **WHEN** 两侧source binding未声明同一canonical MarkerGroup
- **THEN** 两侧Player MUST使用各自raw source time
- **AND** Compiler MUST生成None计划

#### Scenario: Action source同步数据损坏

- **WHEN** Slot route要求同步但binding缺少合法segment、duration或role
- **THEN** Runtime MUST报告稳定typed failure
- **AND** MUST不回退normalized time或Animancer自动同步

### Requirement: Source usage、retention与release必须由实际consumer闭环

PoseState transition MUST按state relevance保留共同可见source；AnimationSlot和显式BlendStack MUST按自身source usage保留Action或exact source。Action lifecycle MUST只管理有限playback的PendingFirstSample、Selected、Retained与Retired；Pose source MUST使用Projection-local dense source index、PlayerNodeId、SourceGeneration与frame lease，不得伪造作者Source字符串或Action lifecycle。transition或Slot完成后，consumer MUST先发布typed retirement permission，source backend完成物理释放后再发布completion，owner才能最终清理资源。

#### Scenario: Action逻辑结束但Slot仍在淡出

- **WHEN** Attack producer已经离开Gameplay membership但Slot仍保留其Pose
- **THEN** Action lifecycle MUST只维持animation-only retention
- **AND** TreeClip、Motion、Window、Cue与Gameplay fact MUST不再执行

#### Scenario: Start State已经切出

- **WHEN** Start到Locomotion transition仍需要Start Pose
- **THEN** State relevance MUST保留Start provider source
- **AND** Action lifecycle MUST不创建对应PlaybackId

### Requirement: Transition Policy必须按明确owner完整编译

每条PoseState Transition、每个AnimationSlot和每个保留的显式BlendStack MUST拥有明确Policy owner。Projection Compiler MUST把exact endpoint、Standard Blend或Inertialization、duration、canonical curve、dense Blend Profile、capture/release request layout、PlanId与Revision编入固定Routing Plan。Runtime与Preview MUST只装载匹配Projection revision的计划，不得现场编译、缺省补pair或使用旧plan。

#### Scenario: Slot缺少Action到Source Pose规则

- **WHEN** Compiler无法为可达Action endpoint物化`Action -> SourcePoseEndpoint`
- **THEN** Projection Build MUST失败并定位Slot与endpoint
- **AND** Runtime MUST不把Source Pose解释为Empty

#### Scenario: PoseState edge选择Inertialization

- **WHEN** target Ready且edge的compiled route为Inertialization
- **THEN** owner MUST提交typed capture/release请求
- **AND** source MUST在正式capture permission前保持相关资源

### Requirement: Animancer必须只负责source采样

Animancer source backend MUST只按完整Action playback或Presentation Pose source identity创建或复用Sequence/ManualMixer playable，应用effective sample、loop、play rate和source-local clip weight，并把source capture job安装到同一PlayableGraph。它 MUST不仲裁Pose State或Action winner、不查询Transition Policy、不拥有跨source weight、不执行AnimationSlot、Layer composition、IK或FootPlacement，也 MUST不发布最终Pose。

#### Scenario: PoseState transition共同采样两个source

- **WHEN** Standard Blend要求source与target同时可见
- **THEN** Animancer MUST分别提供两份source capture
- **AND** source间weight MUST只由PoseState transition计算

### Requirement: Preview必须执行正式Projection和Pose Plan

Action Timeline Preview、Pose Graph Fact Preview和Motion Matching Query Fixture MUST共用唯一`AnimationPreviewRuntime`、匹配revision的Projection、source backend与Pose Plan。三类入口 MUST分别只提交Action command、Presentation Fact或state-local query fixture。Preview MUST复用正式readiness、Player、Routing、Slot、Inertialization、release与reset语义，不得创建BaseLocomotion Timeline、Gameplay winner、简化Player、隐藏Stack、临时PlayableGraph或Animancer direct Play路径。

#### Scenario: Pose Preview改变速度Fact

- **WHEN** 作者把HorizontalSpeed从零改为移动值
- **THEN** 正式Transition Rule MUST驱动Pose State变化
- **AND** Preview MUST不发送PlayRun Gameplay事件

#### Scenario: Timeline Preview非连续Seek

- **WHEN** 作者seek到另一Action sample
- **THEN** Preview MUST按正式Action lifecycle、Slot和reset policy更新
- **AND** MUST不为预览平滑额外插入BlendStack

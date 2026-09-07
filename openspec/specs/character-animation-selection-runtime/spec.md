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

`ClipPlayer`、`BlendSpacePlayer`、`SelectedPosePlayer`和`BlendStack` MUST只消费自身绑定或连接的state-local source。`PoseStateMachine` MUST唯一拥有State到State的transition workspace；`AnimationSlot` MUST唯一拥有Source Pose与有限Action playback之间的插入和handoff；显式`BlendStack` MUST只拥有其连接source的多entry连续性；`Inertialization` MUST只拥有直接上游Player或transition consumer的residual与rebase。Compiler与Runtime MUST不在Provider、AnimationChannel、Graph branch或OutputPose背后自动插入Player、Stack、Slot或Inertialization。

#### Scenario: Idle使用ClipPlayer

- **WHEN** PoseStateMachine保持Idle State
- **THEN** ClipPlayer MUST按Presentation时间采样正式direct Clip binding
- **AND** Action playback lifecycle MUST不创建对应producer

#### Scenario: Corin Locomotion切换

- **WHEN** Corin PoseState edge从Turn切换到RunLoop
- **THEN** edge MUST执行显式编译的Standard Blend与可选Phase relation
- **AND** MUST不自动插入Inertialization

### Requirement: Source usage、retention与release必须由实际consumer闭环

PoseState Transition MUST按state relevance保留共同可见source；ActionPlaybackInput MUST唯一拥有有限Action的PendingFirstSample、Selected、Retained、Retired、command cursor与generation；AnimationSlot与显式BlendStack MUST按自身usage保留Action或exact source。上述逻辑consumer及其状态 MUST由`CharacterPoseProgramRuntime`中的正式节点Implementation拥有，并通过typed Source Demand、Usage与Retirement Permission输出需要的source身份；MUST不直接创建、销毁或复用Playable资源。

唯一`CharacterPoseSourceModule` MUST接收这些typed结果并拥有Physical Pose Source Registry、prepared resource、capture binding、Action sample readiness、retirement validation、deferred release与release completion。Source Module MUST不仲裁Action winner或推进ActionPlaybackInput lifecycle。Pose source MUST继续使用Projection-local dense source index、PlayerNodeId、SourceGeneration与frame lease。consumer发布匹配permission且完整Frame成功后，Source Module才能物理释放并回报completion；Program Runtime收到匹配completion后才能最终清理逻辑usage。外层Runtime、Diagnostics和Preview MUST不拥有第二Action lifecycle或release路径。

#### Scenario: Action逻辑结束但Slot仍在淡出

- **WHEN** Attack producer已经离开Gameplay membership但Program中的Slot仍保留其Pose
- **THEN** Slot MUST继续发布对应usage且Source Module MUST保留物理source
- **AND** Gameplay Timeline、外层Runtime与Program-owned Action lifecycle MUST不提前destroy该Playable

#### Scenario: Source获得最终释放许可

- **WHEN** Program consumer发布匹配identity的retirement permission且当前Frame成功Seal
- **THEN** Source Module MUST执行唯一deferred physical release并在后续正式结果中发布completion
- **AND** Program Runtime MUST不在收到匹配completion前复用逻辑slot或伪造释放成功

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

唯一`CharacterPoseSourceModule`内部的Animancer source backend MUST只按完整Action playback或Presentation Pose source identity创建或复用Clip/ManualMixer Playable，应用compiled effective sample、loop、play rate和source-local clip weight，安装source capture binding并管理物理source寿命。Program Runtime MUST拥有PoseState、Player endpoint、ActionPlaybackInput lifecycle、Transition、Slot、Blend Stack和Inertialization逻辑；Source Module与Animancer MUST不仲裁State或Action winner、不推进Action lifecycle、不解析AnimationClip Curve、不选择Phase leader、不拥有跨source weight、不执行AnimationSlot、Layer composition、Foot Placement、Goal Assembly、FBBIK或Final Publication。

#### Scenario: PoseState transition共同采样两个source

- **WHEN** Program中的Standard Blend要求source与target同时可见
- **THEN** Program Runtime MUST发布两份typed Demand与各自effective sample要求，Source Module MUST提供两份capture
- **AND** source间weight、Transition clock和release permission MUST仍由Program节点Implementation计算

#### Scenario: Source backend尝试选择State

- **WHEN** Source readiness或Playable状态发生变化
- **THEN** Source Module MUST只发布Pending、Ready、Invalid或release completion结果
- **AND** MUST不直接修改PoseState、Player generation、Transition或OutputPose

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

### Requirement: Locomotion Phase映射必须属于source-local采样计划

Direct Clip与Blend Space Locomotion source MUST各自编译为`AnimationSourcePhasePlan`。Direct Clip endpoint MUST使用该Clip的forward/inverse Phase plan；Blend Space endpoint MUST使用显式Phase Reference Sample作为raw clock carrier，并让全部正权重Dynamic Sample通过各自per-clip inverse plan采样同一unwrapped phase。PoseState source同步 MUST由Compiler根据Transition两侧唯一source usage和共同Profile Locomotion Sync Group生成relation；Transition authoring MUST不保存同步开关或leader override。Compiler MUST按clock authority与完整Blend窗口coverage写入固定leader，Runtime MUST用TransitionGeneration建立relation lifecycle并在source采样前生成effective phase/time。Runtime MUST不读取AnimationCurve、Profile或Foot Analysis，也 MUST不按State名、Clip名、weight或最高权重样本动态选择leader。

#### Scenario: Walk State切换Run State

- **WHEN** Transition两侧source endpoint属于同一Locomotion Sync Group
- **THEN** relation MUST把leader source phase映射到target source endpoint
- **AND** MUST不创建BaseLocomotion Gameplay Selection

#### Scenario: Relation正常完成

- **WHEN** TransitionGeneration完成并释放Phase relation
- **THEN** follower MUST从最后effective time建立自身continuation anchor后关闭该generation
- **AND** 下一次独立Transition MUST不复用旧leader、cycle或generation state

#### Scenario: Transition两侧没有共同同步组

- **WHEN** 两侧source endpoint不属于同一Profile Group
- **THEN** 两侧Player MUST使用各自raw source time
- **AND** Compiler MUST生成None relation

#### Scenario: Phase plan损坏

- **WHEN** source endpoint的Clip identity、Curve hash、coverage或inverse knots无效
- **THEN** Runtime MUST报告稳定typed invalid并阻止本帧Pose publication
- **AND** MUST不回退normalized time、Marker或Animancer自动同步

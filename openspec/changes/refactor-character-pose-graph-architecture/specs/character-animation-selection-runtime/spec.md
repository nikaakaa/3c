## MODIFIED Requirements

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

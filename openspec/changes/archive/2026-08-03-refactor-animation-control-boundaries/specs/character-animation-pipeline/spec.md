# character-animation-pipeline Specification

## ADDED Requirements

### Requirement: Animation Pipeline必须同时投影Presentation Fact与有限Action playback

Simulation Commit MUST产生构造Presentation所需的committed Body、Intent、Motion phase和有限Action producer事实。Presentation Runtime MUST从前者构造`CharacterPresentationFactFrame`，从活动Action Timeline producer构造exact Action playback。Program MUST不为持续Locomotion提交具体animation winner；Presentation MUST不重复执行Timeline Gameplay track或推导Action admission。

#### Scenario: 普通Locomotion表现帧

- **WHEN** 当前没有Action Timeline且Body正在移动
- **THEN** Runtime MUST用Presentation Fact驱动Locomotion PoseStateMachine
- **AND** Action Slot MUST透传Source Pose

#### Scenario: Attack Timeline同帧产生Window与动画

- **WHEN** Attack Timeline在Simulation Tick推进HitWindow并选择Attack animation track sample
- **THEN** Gameplay Window MUST只在Simulation执行一次
- **AND** Presentation MUST按同一committed playback identity生成Action playback

### Requirement: 动画表现协调器与有限Action Playback运行时必须分离

`CharacterAnimationPresentationRuntime` MUST是单角色动画表现帧的唯一协调器，按编译顺序组织Presentation Fact、PoseStateMachine、state-local source provider、`CharacterActionPlaybackRuntime`、AnimationSlot、Transition Routing、source backend、Pose Plan与唯一final publication。`CharacterActionPlaybackRuntime` MUST只管理Gameplay已经确认的有限Action playback identity、committed raw visual sample、PendingFirstSample/Selected/Retained/Retired生命周期、command acknowledgement与exact retirement。旧`CharacterAnimationPlaybackRuntime`总管类型和转发入口 MUST删除，不得保留兼容壳或第二条Preview路径。

`CharacterActionPlaybackRuntime` MUST不推进或求值PoseStateMachine、SequencePlayer、BlendSpacePlayer、Motion Matching provider或完整Pose Runtime，MUST不计算Marker effective time、transition weight、Stored Pose、Inertialization、Bone composition或Final Pose。持续Pose source和Motion Matching state-local selection MUST只按PoseState relevance存在，不得创建`AnimationPlaybackId`或进入Action Playback生命周期。

#### Scenario: 无Action的普通Locomotion表现帧

- **WHEN** 当前帧只有合法Presentation Fact且没有有限Action command
- **THEN** `CharacterAnimationPresentationRuntime` MUST推进PoseStateMachine和state-local source并求值唯一Pose Plan
- **AND** `CharacterActionPlaybackRuntime` MUST不创建Locomotion playback或空占位实例

#### Scenario: Attack Timeline提交首份合法sample

- **WHEN** ordered command batch包含Gameplay已经确认的Attack selection与首份committed raw visual sample
- **THEN** `CharacterActionPlaybackRuntime` MUST建立对应exact playback并发布给绑定Slot
- **AND** Slot、MarkerSync与source backend MUST分别拥有transition、effective time与Pose采样责任

#### Scenario: Action逻辑结束但Slot仍在淡出

- **WHEN** Gameplay已经提交Action release而Slot仍声明该playback的正式source usage
- **THEN** `CharacterActionPlaybackRuntime` MUST保持只读Presentation retention
- **AND** 只有Slot提交release permission且不存在其它exact usage后，协调器才可发起物理source释放
- **AND** 只有完整source set全部返回匹配completion后才可进入Retired

#### Scenario: 动画表现帧求值失败

- **WHEN** 任一Required PoseState source、Action sample、Slot operation或Pose operation失败
- **THEN** `CharacterAnimationPresentationRuntime` MUST阻止部分Final Pose发布并按正式事务规则清理
- **AND** `CharacterActionPlaybackRuntime` MUST不单独提交一个与Pose Plan不一致的生命周期结果

### Requirement: 持久Action command与帧内Pose workspace必须分离

Gameplay提交的Select、Sample、Complete与Release MUST进入跨帧持久`ActionPlaybackCommandInbox`，并按EventId、producer、ActionInstance与generation保序。PoseRequest、PoseUnavailable、provider demand、source usage与operation completion MUST只存在于当前`PresentationFrameWorkspace`，MUST不进入Action command kind或持久inbox。外部Publish、Replace与Retire MUST只修改Inbox，MUST不直接修改live Action lifecycle。

#### Scenario: Action command到达但本帧Pose求值失败

- **WHEN** Inbox含有合法Action Sample而唯一Pose Plan求值失败
- **THEN** command MUST保持未确认并可由下一帧重新读取
- **AND** 帧内PoseRequest与PoseUnavailable MUST整体丢弃

#### Scenario: 无Action Locomotion表现帧

- **WHEN** 当前Inbox为空且PoseState source有效
- **THEN** PresentationFrameWorkspace MUST仍能完成完整Pose Plan
- **AND** 系统 MUST不创建空Action command或playback

### Requirement: Action生命周期必须按逐Playback registry推进

`ActionAnimationPlaybackLifecycleRegistry` MUST按完整`AnimationPlaybackId`保存每个有限Action entry，并保存ActionInstance、producer、channel、generation、latest EventId、first-sample readiness、logic terminal、Slot usage set、retirement permission、backend release request/completion与phase。生命周期 MUST按`PendingFirstSample -> Selected -> Retained -> RetirementPermitted -> Retired`推进。Lifecycle MUST不依赖具体Pose Runtime，不得按channel当前winner或Pose Runtime反查推断Retained与Retired。

#### Scenario: 旧Action在新Action进入后继续淡出

- **WHEN** Attack1已经logic terminal但Slot仍保留Attack1并选择Attack2
- **THEN** Registry MUST同时保存Attack1 Retained entry与Attack2 Selected entry
- **AND** snapshot MUST不只显示channel当前winner

#### Scenario: 同一Playback的ActionInstance变化

- **WHEN** 后续Sample command携带与Select不同的ActionInstanceId
- **THEN** command处理 MUST失败并保持原entry
- **AND** MUST不把该Sample合并进现有Playback

### Requirement: Action释放必须经过usage、permission与backend completion

AnimationSlot MUST按`SlotId + ActionPlaybackId + usage kind + completion identity`发布Action-only source usage。全部exact consumer usage消失并取得release permission后，协调器 MUST向Physical Pose Source Registry提交带request identity与完整source set的释放请求。只有全部source与capture资源返回匹配completion后，Action registry才 MUST提交Retired。Sequence、BlendSpace、Motion Matching与PoseState source usage MUST不进入该Action握手。

#### Scenario: 一个Action被两个消费者引用

- **WHEN** FullBody Slot与另一个exact consumer仍分别引用同一Action
- **THEN** 任一单独usage消失 MUST不触发retirement
- **AND** 必须等待全部usage和完整backend completion

#### Scenario: backend只释放部分source

- **WHEN** release request包含playable与Stored Pose capture而只有playable完成释放
- **THEN** Action entry MUST保持RetirementPermitted
- **AND** MUST不发布Retired

### Requirement: committed raw时间与表现采样时间必须分离

`CharacterActionPlaybackRuntime` MUST只保存Gameplay已经提交的`CommittedRawVisualTime` history。动画表现协调链 MAY在两个committed sample之间计算`ProjectedPresentationSampleTime`，并在Retained期间按最后确认visual time scale继续animation-only采样；finite source MUST钳制在合法coverage，cyclic source MUST保持展开cycle。MarkerSync MUST从projected raw sample计算effective sample。projected或effective time MUST不写回Gameplay Timeline、Window、Motion、Cue或Action lifecycle。

#### Scenario: 两个Simulation Tick之间插值Action

- **WHEN** render frame位于两个committed Timeline sample之间
- **THEN** Presentation MAY插值得到projected sample
- **AND** diagnostics MUST保留committed raw与projected sample的区别

#### Scenario: Action逻辑结束但Slot仍淡出

- **WHEN** Action已Retained且Slot仍声明Sample usage
- **THEN** sampler MAY按正式retention规则继续animation-only采样
- **AND** MUST不把投影时间提交为新的Gameplay raw sample

### Requirement: 动画表现帧必须原子提交

`CharacterAnimationPresentationRuntime` MUST为Action inbox读取、Action registry、sample projector、Marker cursor、PoseState/provider、Slot、Transition、source usage、release completion、diagnostics与Final Pose建立同一个有界staged transaction。只有唯一Pose Plan成功后才 MUST同时提交状态、acknowledgement、retirement、diagnostics与`FinalAnimationPoseFrame`；失败时 MUST回滚全部staged mutation，不得部分消费command、推进Action phase、释放source或发布Pose。

#### Scenario: Slot route成功但下游Pose operation失败

- **WHEN** Slot已经在staged workspace选择新route而Pose Plan后续operation失败
- **THEN** live Slot route、Action lifecycle与Marker cursor MUST保持上一已提交帧
- **AND** Inbox command MUST保持未确认

### Requirement: 动画启动必须由Required Pose Plan readiness决定

Animation Presentation启动 MUST检查committed Body/Fact、有效Projection与Pose Plan、Entry PoseState及Required Pose source readiness。系统 MUST删除以committed Action Selection为真相的`RequireCommittedSelection`、`AwaitCommittedSelection`或等价门禁。没有Action时 MUST不等待Action Runtime或创建空playback。

#### Scenario: 观察角色首次出现且没有Action

- **WHEN** observed actor具有有效Body Fact、Entry PoseState与ready Idle source
- **THEN** Presentation MUST立即执行Base Pose
- **AND** MUST不等待Gameplay Animation Selection

### Requirement: Pose执行Module不得形成新的动画总管

动画表现实现 MUST分离PoseState/source provider、AnimationSlot、Pose Plan execution与Physical Pose Source Registry的状态所有权。外层协调器 MUST只通过typed frame按编译顺序编排，不得按channel扫描内部Player或通过Pose Runtime查询Action retention。Pose Plan execution MUST只装载编译plan、执行Pose operation并发布completion。

#### Scenario: Action frame进入FullBody Slot

- **WHEN** Action Runtime输出已解析playback frame
- **THEN** frame MUST按compiled ActionPlaybackInput精确路由到Slot Action Player
- **AND** 协调器 MUST不扫描全部Player猜测consumer

## MODIFIED Requirements

### Requirement: Timeline轨道采样必须输出Animation Selection数据

Compiler MUST只把有限Action Timeline Animation Track降低为source-neutral action selection binding和marker binding。SimulationTick MUST推进Gameplay Timeline并为每个有限Action channel提交唯一playback；PresentationFrame sampler MUST按raw visual time、cycle、PlaybackMode和source-local clip weight生成Action Animation Selection与typed Parameter page。持续Locomotion MUST由Presentation Fact与PoseStateMachine生成，MUST不要求Timeline Animation Track。Timeline MUST不解析PoseState transition、Marker Sync effective time、Blend Logic、Bone Mask或IK plan。

#### Scenario: Attack Timeline同时产生Window与动画

- **WHEN** Attack Timeline在SimulationTick推进HitWindow并选择Attack animation producer
- **THEN** Program MUST提交有限Action playback identity
- **AND** Presentation MUST按同一playback生成Action Selection

#### Scenario: 普通Run Locomotion

- **WHEN** committed Body显示角色正在持续跑动
- **THEN** PoseStateMachine MUST选择Run对应Pose source
- **AND** Program MUST不运行RunLoop Animation Timeline

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

Runtime MUST在同一正式PlayableGraph执行compiled Fact Input、PoseStateMachine、Sequence/BlendSpace/MM source、Action Slot、Transition Routing、Inertialization、composition、ModifyBone、FootPlacement与Output Pose。Runtime MUST不创建旧BaseLocomotion Selection Player、Animator Controller、Animator.CrossFade、Timeline autonomous player或第二PlayableGraph。

#### Scenario: Corin运行Locomotion和Action

- **WHEN** Corin同时具有移动Fact和Attack playback
- **THEN** 唯一Pose Plan MUST先生成Locomotion Source Pose再由FullBodyAction Slot插入Attack
- **AND** MUST只发布一个Final Pose

### Requirement: 逻辑层必须为每个动画通道提交唯一播放选择

Program Finalize MUST只为有限Action、Equipment action或其它明确Gameplay-owned animation channel提交至多一个selected producer/playback command。持续BaseLocomotion MUST不再是Gameplay AnimationChannel；其Pose由Presentation Fact与PoseStateMachine选择。Program MUST不读取PoseStateId、PoseNodeId、Blend Logic、Slot topology或AnimationClip决定Gameplay winner。

#### Scenario: 同一FullBodyAction通道发生冲突

- **WHEN** 同Tick中两个Action都尝试占有FullBodyAction
- **THEN** Gameplay Program MUST按Action lifecycle解析唯一winner或报告冲突
- **AND** Slot MUST不重新仲裁候选

#### Scenario: Locomotion与Dodge并行

- **WHEN** Body仍产生Locomotion事实且FullBodyAction选择Dodge
- **THEN** Program MUST只提交Dodge action playback
- **AND** Presentation MUST并行求值Locomotion PoseStateMachine后由Slot覆盖

### Requirement: 动画预览只读取正式调试Snapshot

系统 MUST分别从正式Action Runtime、PoseState/source runtime、AnimationSlot、MarkerSync、Transition Routing、source backend与Pose Plan导出只读Action lifecycle snapshot、Pose Plan snapshot、Slot snapshot和relation snapshot，再由`CharacterAnimationPresentationRuntime`在成功commit后组合统一Debug View。Action snapshot MUST不保存PoseNode weight或Pose availability；Pose snapshot MUST不拥有Action lifecycle。Timeline Preview与Pose Graph Preview MUST只读取同一种组合Debug View，MUST不参与Gameplay决策或最终播放。

#### Scenario: 生成每帧预览数据

- **WHEN** 正式或Preview session更新动画
- **THEN** 系统 MAY导出当前PoseState、Action playback与Pose operation snapshot
- **AND** Editor MUST只读取该snapshot

#### Scenario: 运行时禁用调试历史

- **WHEN** 项目关闭动画历史采集
- **THEN** 系统 MAY不保存历史snapshot
- **AND** 正式播放 MUST不依赖snapshot

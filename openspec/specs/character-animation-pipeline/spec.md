# character-animation-pipeline Specification

## Purpose

定义Gameplay Timeline、Presentation Fact、state-local Pose source、有限Action playback、唯一编译Pose Plan与预分配表现帧事务之间的角色动画输出链。

## Requirements

### Requirement: Gameplay Timeline只能提交有限Action播放事实

Compiler MUST把有限Action Timeline AnimationTrack降低为稳定producer binding、直接AnimationClip计划、committed sample contract与source-local Clip Weight计划。Producer binding MUST只保存Timeline/Track引用；Foot Analysis MUST从Profile Analysis Source、角色Rig与Clip Analysis Input Hash解析，Foot Placement Weight MUST通过唯一Clip Curve catalog从`presentation.foot-placement-weight`降低为`animation.foot-placement-weight`Runtime参数。SimulationTick MUST只推进Gameplay Timeline logic time并提交Select、Sample、Complete或Release command；PresentationFrame sampler MUST按committed raw sample、cycle、PlaybackMode和source-local clip weight生成Action playback frame与typed parameter page。Timeline MUST不解析Locomotion Phase、不创建Pose、transition、Bone Mask或IK plan。持续Idle、Walk、Run、Start、Stop与Turn MUST不依赖Gameplay Timeline或AnimationChannel。

#### Scenario: Attack Timeline同时产生Window与动画

- **WHEN** Attack Timeline在一个SimulationTick推进Window并选择直接AnimationClip producer
- **THEN** Window MUST进入Gameplay事实链
- **AND** Action playback command MUST进入Presentation-owned inbox
- **AND** Timeline MUST不创建Sequence或Marker binding

#### Scenario: Locomotion持续播放

- **WHEN** 角色保持Run
- **THEN** PoseStateMachine的state-local provider MUST推进Run source
- **AND** Program MUST不创建Run Timeline producer

### Requirement: Timeline逻辑采样与表现采样必须分离

Gameplay Timeline sampling MUST只按SimulationTick/canonical fraction发生；Action visual sampling与state-local Pose sampling MUST只按PresentationFrame发生。两个Simulation Tick之间的多个PresentationFrame MUST不重复产生TreeClip、Motion、ActionWindow、Cue fact或Effect mutation。Presentation sample MUST不推进CharacterSimulationState的Timeline clock。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** PresentationFrame多次采样同一Action或Pose source
- **THEN** 动画Pose MAY连续变化
- **AND** Gameplay state与facts MUST保持不变

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application boundary。Runtime MUST消费committed Body/Intent、Program parameter和有限Action command，构造Presentation Fact，并按Projection编译的ordered staged Pose Plan执行PoseStateMachine、state-local provider demand/readiness、ActionPlaybackInput、AnimationSlot、Local Pose composition、显式`LocalToComponentPose`、Component Pose骨骼控制、world-aware FootPlacement规划与pelvis输出、typed双腿targets、pure pose LegIK、显式`ComponentToLocalPose`、后续Pose stage及FinalPublication。所有Player、Routing、Inertialization、source capture、空间转换、target value和output completion MUST位于同一帧固定计划和同一次PlayableGraph Evaluate。任一stage失败 MUST阻断后续stage与FinalPublication；若已跨过Animancer Evaluate Barrier，同一Actor Animation Runtime MUST进入Faulted且不得逆序恢复状态或Physical Bone快照。Runtime MUST不创建图外基础动画、Stack、FootPlacement、LegIK、隐式Pose空间转换、world-aware postprocess、第二Pose Graph或第二final writer。

#### Scenario: FootPlacement完成后LegIK失败

- **WHEN** FootPlacement已发布pelvis Pose与targets但LegIK报告退化bend plane
- **THEN** Runtime MUST阻断ComponentToLocalPose与FinalPublication并进入正式Faulted路径
- **AND** MUST不发布只有pelvis补偿而没有双腿求解的部分Pose

#### Scenario: 正常Foot Placement表现帧

- **WHEN** FootPlacement与LegIK依次完成且全部completion匹配
- **THEN** Runtime MUST只发布LegIK及后续stage形成的唯一OutputPose
- **AND** MUST不在图外再次执行Foot Placement或腿部solver

#### Scenario: Commit Attack producer

- **WHEN** Program提交FullBodyAction Attack command
- **THEN** Runtime MUST把Action frame送入绑定的ActionPlaybackInput与AnimationSlot
- **AND** 如何覆盖Source Pose与何时释放 MUST只由compiled Routing Plan决定

#### Scenario: Locomotion target Pending

- **WHEN** PoseStateMachine选择新target但provider尚未Ready
- **THEN** Runtime MUST保持现有合法Source Pose
- **AND** MUST不启动target transition或使用旧Timeline fallback

#### Scenario: Player source槽位复用

- **WHEN** consumer发布retirement permission且backend完成旧source物理释放
- **THEN** Runtime MAY把workspace槽位分配给新source
- **AND** 旧CaptureJob与新CaptureJob MUST不在同一次Evaluate写入同一槽位

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST从`CharacterActionPlaybackRuntime`、PoseState provider、Player、Routing、source backend与Pose Plan导出只读snapshot。Snapshot MAY包含Action PlaybackId/channel/lifecycle、Projection-local dense source index、PlayerNodeId、generation、frame lease、Pending/Ready/Invalid、raw/effective sample、relation、source usage、transition、Inertialization residual、Pose contribution及ordered stage/FinalPublication completion。Snapshot MUST不参与Gameplay、source选择或最终播放，Editor MUST不从Animancer weight重建事实。Runtime MUST先读取显式Live、Capture、Pose Watch与detail interest；没有任何interest时 MUST不执行BlendStack、StateMachine、Inertialization、Operation contribution、Final Pose或逐骨骼weight复制。有interest时 MUST只从成功Seal的Committed页复制到预分配diagnostics页，不得读取或发布Pending帧。

#### Scenario: 导出每帧调试数据

- **WHEN** 正式Runtime或Preview完成表现帧且存在匹配diagnostics interest
- **THEN** MAY发布匹配Projection revision的只读snapshot
- **AND** Snapshot MUST只表达同一completion identity的Committed结果
- **AND** 关闭调试历史 MUST不影响正式播放

#### Scenario: 没有调试关注者

- **WHEN** 当前Actor没有Live、Capture、Pose Watch或detail interest
- **THEN** Runtime MUST跳过Operation、Final Pose、Pose Watch和逐骨骼diagnostics复制
- **AND** 正式Pose求值、Final Writer与completion MUST保持不变

### Requirement: 不得恢复Timeline或Preview分裂路径

系统 MUST只有一条Gameplay Timeline Program operation路径和一条Presentation Pose Plan路径。两者只通过committed Body/Intent、EventId和有限Action command连接；不得保留旧TimelinePlaybackScheduler、Timeline.Bind/Evaluate/Unbind、自主TreeClip runtime、AnimationClip root motion、Animancer direct Play或独立PlayableGraph。Timeline Authoring Preview MUST只通过Action adapter进入统一`AnimationPreviewRuntime`，不得执行Program TreeClip或Simulation Session；Pose Graph Preview与MM Fixture MUST使用各自typed adapter进入同一Runtime。

#### Scenario: Runtime与Preview并存

- **WHEN** Editor预览Attack Timeline且游戏运行Corin Program
- **THEN** Preview state MUST不影响CharacterSimulationState
- **AND** Live Runtime MUST独占执行Program operation

### Requirement: Timeline回绕必须完整采样Gameplay边界

Compiled Timeline operation MUST在一个SimulationTick跨越loop边界时按尾段、中间cycle和头段稳定采样Gameplay tracks。Presentation sampler MAY按visual time回绕Action动画，但 MUST不补发Gameplay facts。持续Pose source的cycle MUST由state-local Player独立维护。

#### Scenario: 一Tick跨越Loop终点

- **WHEN** logic time从cycle尾部前进到下一cycle头部
- **THEN** Program MUST按正式区间顺序采样两侧Gameplay segment
- **AND** Presentation MUST不重复提交Window或Cue

### Requirement: 动画command写入与消费权限必须单向

Kernel Finalize MUST只写有限Action的EventId producer select/sample/complete/release command；SimulationCommitter MUST只按已校验OutputDisposition写presentation-owned queue；PresentationFrame MUST在外层事务中原子消费并acknowledge。Portable Core MUST只定义model-neutral command，不引用Unity Animation/Presentation模块；Presentation adapter MUST不反向修改Program或Character state。

#### Scenario: 一个RenderFrame前发生多个SimulationTick

- **WHEN** queue包含多个generation的complete/release
- **THEN** PresentationFrame MUST按Tick/Event sequence消费
- **AND** 任一阶段 MUST不双写同一command

### Requirement: 有限Action readiness必须来自第一份合法Sample

Action lifecycle MUST只以所选producer的第一份匹配generation的合法visual sample作为PendingFirstSample到Selected的readiness。Runnable completion、Kernel Finalize或Pipeline Commit MUST不伪造Ready。PoseState readiness MUST独立来自`PresentationPoseSourceSample`的Availability。

#### Scenario: 新Action尚无Sample

- **WHEN** Select已提交但合法Sample未到
- **THEN** Lifecycle MUST保持PendingFirstSample
- **AND** Slot MUST按compiled语义维持当前合法输出

### Requirement: 动画表现帧必须使用预分配暂存事务

`CharacterAnimationPresentationRuntime` MUST为每个Actor使用唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现帧事务。Runtime创建时 MUST按Projection编译容量一次分配Committed/Pending Dense Pose页、Native workspace页、Inertialization页、Final Pose页、pending scalar state、mutation journal与source lifecycle command batch。PresentationFrame MUST只读取Committed状态并写Pending页或journal；MUST不通过`CaptureState`、`Clone`、`ToArray`、新建数组、Dictionary或List复制完整旧状态以建立回滚点。成功帧 MUST通过页索引交换和已验证journal提交新状态；Animancer Evaluate Barrier前失败 MUST只丢弃Pending，不恢复Committed对象图；Barrier期间或之后失败 MUST使同一Actor Animation Runtime进入Faulted，不逆序恢复状态或Physical Bone快照。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法Projection完成普通PresentationFrame且没有诊断interest
- **THEN** Runtime MUST直接生成Pending Pose与Pending Module状态并在成功后交换Committed页
- **AND** 事务、Pose发布与关闭的diagnostics MUST不产生每帧托管分配
- **AND** Runtime MUST不复制完整BlendStack、Pose workspace、Inertialization history、Physical Source registry或骨骼Transform

#### Scenario: Evaluate前验证失败

- **WHEN** Pending帧在进入Animancer Evaluate前发现identity、容量、source ownership或release依赖不合法
- **THEN** Runtime MUST丢弃本帧Pending页、journal和prepared resource
- **AND** 已提交Action、PoseState、Slot、Transition、source ownership、Final Pose与Physical Bones MUST保持不变

### Requirement: Dense状态与稀疏生命周期必须使用不同暂存策略

每帧完整生成的Dense Pose、velocity、weight、parameter、Native result与Inertialization next state MUST直接写入固定Committed/Pending双页。Action registry、command inbox cursor、sample/Marker cursor、source ownership、usage、retirement与release handshake MUST使用固定容量pending scalar或mutation journal。journal MUST在Evaluate前完成identity、顺序、重复项、容量和依赖验证，Seal MUST只按固定顺序应用已验证mutation。系统 MUST不为了统一API把稀疏Registry整页复制，也 MUST不把Dense Pose降低为逐骨骼托管mutation对象。

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式Animancer Graph Evaluate MUST作为动画表现帧不可逆Animancer Evaluate Barrier。进入Barrier前，Runtime MUST完成全部托管identity、容量、readiness、source、release、Job binding与Final Writer binding验证，并且 MUST不消费command acknowledgement、不提交lifecycle、不销毁Committed source、不发布release completion或Final Pose。Barrier之后的Seal MUST不执行动态查找、编译、扩容或业务输入验证，只可交换固定页、应用已验证mutation并发布成功结果。

#### Scenario: Barrier前Module准备失败

- **WHEN** Player、Slot、Routing或source backend在Prepare阶段报告不可提交结果
- **THEN** Runtime MUST不调用Animancer Evaluate
- **AND** MUST不修改Physical Bones或任何已提交外部资源

#### Scenario: Barrier成功完成

- **WHEN** Pending状态、Job binding与Final Writer binding全部通过验证且Graph Evaluate成功
- **THEN** Runtime MUST以同一completion identity Seal全部Module状态
- **AND** command acknowledgement、lifecycle、retirement、release completion与Final Pose MUST只属于该成功帧

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

`AnimationFinalPosePhysicalWriter` MUST同时读取当前Committed Final Pose与本帧Pending Final Pose，并在写入任何Physical Bone前验证全部Physical Bone Transform binding、PhysicalBoneCount、Pose availability、continuity identity、graph completion和frame completion。全部合法时 MUST写入Pending Pose；typed Pending、Unavailable或Invalid时 MUST保持Committed Pose并禁止提交Pending页。由于该outcome在Evaluate Barrier内产生，外层Runtime MUST进入Faulted。Writer MUST不先写部分骨骼再报告失败，Physical Bone local pose MUST不再由表现帧事务提前捕获或在失败后恢复。

#### Scenario: Pending Pose全部合法

- **WHEN** Pose Graph完成且全部Physical Bone handle和Pending local pose合法
- **THEN** Final Writer MUST在同一Evaluate Barrier写入完整Pending Physical Pose
- **AND** Seal MUST把对应Pending Final Pose页提升为Committed

#### Scenario: Pending Pose无效

- **WHEN** Pose Graph发布typed Invalid、completion不匹配或任一Physical Bone在写入前验证失败
- **THEN** Final Writer MUST不把部分Pending Pose留在可见Rig
- **AND** Runtime MUST保持上一Committed Pose并丢弃本帧Pending结果
- **AND** 当前Actor Animation Presentation Runtime MUST进入Faulted并拒绝下一帧
- **AND** MUST不分配或恢复Physical Transform快照

### Requirement: Physical Source资源生命周期必须延迟提交

新Source Visual、Mixer、Capture Playable、Clip State与物理source slot MAY在Prepare阶段创建为prepared resource，但 MUST不在Seal前取代Committed ownership。Prepare失败 MUST只释放本帧新建prepared resource。Committed source的disconnect、destroy、slot reuse与backend release MUST只由成功帧的固定deferred lifecycle command执行；容量 MUST来自Projection并在Runtime创建或Prepare时严格验证，不得动态扩容或回退其它source。

#### Scenario: 新Action source准备后帧失败

- **WHEN** Prepare为新Action创建了Source Visual但Pose Plan没有成功跨过Animancer Evaluate Barrier
- **THEN** Runtime MUST释放本帧prepared resource
- **AND** 原Committed source、usage和slot ownership MUST保持有效

#### Scenario: 旧source获得释放许可

- **WHEN** Slot usage消失、retirement permission和backend release依赖全部在成功帧内匹配
- **THEN** Runtime MUST在Seal后执行唯一deferred release command
- **AND** MUST不在Pose Plan成功前销毁旧Playable或复用其workspace槽位

### Requirement: 动画表现异常必须区分Discard与Fault

预期的source Pending、readiness等待与Pose Unavailable MUST在Animancer Evaluate Barrier前通过正式outcome关闭Pending帧，不得依赖异常恢复。若typed Invalid只能在Barrier内确定，Runtime MUST保持Committed Physical Pose并进入Faulted。任何异常发生在Animancer Evaluate Barrier前时，Runtime MUST Discard Pending并向上抛错；发生在Barrier期间或之后时，Runtime MUST记录一次结构化Actor、PresentationFrame、BodyTick、completion与phase上下文，使该Actor的Animation Presentation Runtime进入Faulted并继续向上抛错。Faulted Runtime MUST拒绝后续Present调用，MUST不捕获全部骨骼和状态后尝试继续旧动画，也 MUST不自动重建Runtime或切换fallback路径。

#### Scenario: Prepare阶段发生不变量异常

- **WHEN** Animancer Evaluate Barrier前检测到非法generation或workspace identity
- **THEN** Runtime MUST Discard Pending并保留Committed状态
- **AND** 原异常 MUST继续向上报告

### Requirement: Rollback Action 生命周期必须尊重确认终态

Rollback产生的Action Select与Sample属于可重基的预测生命周期；Complete与Release属于确认后终态。Action Playback Runtime MUST在现有Animancer Evaluate Barrier前的事务内原子处理受影响generation的lifecycle、sample history、Slot usage、source continuity与release ownership。回滚撤销未确认Select或Sample MUST不转换为业务Release；confirmed terminal提交后，同generation的Sample MUST拒绝并进入正式Faulted。

#### Scenario: Graph Evaluate发生不可预期异常

- **WHEN** Animancer Graph Evaluate或Barrier后的Seal发生无法证明无外部副作用的异常
- **THEN** 当前Actor Animation Presentation Runtime MUST进入Faulted并拒绝下一帧
- **AND** 系统 MUST不执行Physical Transform全量恢复后继续运行

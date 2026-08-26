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

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application seam。Runtime MUST消费Committed Body/Intent、Program parameter和有限Action command，构造Presentation Fact，并按Projection编译的有序Pose Plan执行PoseStateMachine、state-local provider、AnimationSlot、Local/Component Pose转换、Component Pose控制、Foot Placement、Goal Contribution汇聚、唯一FullBodyIK、后续Pose stage与FinalPublication。

唯一`PosePlanExecutionRuntime` MUST构造并持有唯一`CharacterPoseConstraintRuntime`与根Bank；正式Runtime和Preview MUST通过同一Factory取得该所有权关系。Staged Executor只能按编译Pose阶段调用该实例，MUST不为Foot Placement、Goal Assembler、FBBIK或Diagnostics创建第二根事务、第二Committed identity或独立Seal顺序。根Runtime MUST只拥有阶段顺序、lineage、页选择和事务生命周期，不得实现Foot、Pelvis、Goal Assembly或Solver数学。

Foot Placement与PoseBone Goal来源 MUST只发布typed Goal Contribution。唯一Goal Assembler MUST在预分配页中验证Frame、Completion、Rig、Slot与重复贡献并发布一个Goal Set；唯一FullBodyIK MUST只消费该Goal Set与同一Component Pose。Foot Placement、Goal Assembler、FBBIK BendHistory和紧凑Outcome MUST写入同一CharacterPoseConstraint Pending Bank。任一stage失败 MUST阻断后续stage与FinalPublication；跨过Animancer Evaluate Barrier后的失败 MUST使同一Actor Runtime进入Faulted。

#### Scenario: 正常执行Foot Placement与FBBIK

- **WHEN** Foot Placement与PoseBone贡献通过唯一Assembler形成合法Goal Set且FBBIK成功
- **THEN** Runtime MUST只发布同一Completion的一个Goal Set、一次FBBIK结果和唯一OutputPose
- **AND** Foot、Pelvis与BendHistory MUST属于同一Pending Bank

#### Scenario: Goal Slot重复

- **WHEN** 两个Goal Contribution尝试写入同一FBBIK Effector Slot
- **THEN** Goal Assembler MUST在不可逆Writer前使整帧typed invalid
- **AND** Runtime MUST不按顺序覆盖、择优或创建第二Goal Set

### Requirement: 唯一FBBIK必须使用单数运行合同与显式产出证明

`CharacterAnimationPresentationProfile` MUST是FullBodyIK Profile唯一作者Owner；FullBodyIK Pose节点 MUST只表达拓扑且不得保存第二份Profile引用。Compiler MUST从当前Presentation Profile生成Descriptor，Descriptor MUST冻结Profile Id与Revision并在Runtime构造前和当前Profile精确对账。

Pose Constraint Runtime MUST只保存一个Solver、一个Goal Set、一个BendHistory和一个Solver Outcome，不得使用长度为1的Solver、Outcome、Goal Set或Goal Set Index数组。Solver Outcome MUST显式记录Produced、Frame、Completion与Rig lineage；默认值 MUST表示本帧未执行并阻止Physical Writer。

#### Scenario: Solver未执行

- **WHEN** Goal Assembler已经完成但当前Frame与Completion没有产生FBBIK Solver Outcome
- **THEN** Physical Writer前验证 MUST失败并Discard根Pending Bank
- **AND** 默认Result MUST不能被解释为本帧Solver成功

#### Scenario: Profile修改后使用旧Projection

- **WHEN** Descriptor冻结的Profile Revision与当前唯一Profile Revision不一致
- **THEN** Runtime构造 MUST拒绝旧Projection
- **AND** MUST不把旧Plan identity与新Solver参数组合运行

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST从同一Pending Bank已经完成并通过容量、Frame、Completion与Rig lineage验证的Runtime Result、source backend、Pose Plan、Goal Assembler、FBBIK与待写Final Pose冻结只读Diagnostics页。运行Result与Diagnostics MUST严格分型；任何运行算法 MUST不读取Diagnostics决定source、Foot Proposal、Ownership、Pelvis、Goal、Bend或最终Pose。

Runtime MUST在Frame开始冻结并预验证Live、Capture、Pose Watch与detail interest及固定容量。没有interest时 MUST跳过大页、逐骨骼和逐接触Diagnostics复制；有interest时 MUST在Physical Writer前从已完成Pending Result no-throw地写入Pending Diagnostics页。根Bank成功切换后只能发布已随Bank提交的Committed Diagnostics，不得继续写Committed页或在回调中补算。Diagnostics interest、Projector和发布回调 MUST不改变正式求解路径、状态容量与结果。

#### Scenario: Diagnostics interest中途变化

- **WHEN** Editor在当前表现帧中途打开Foot Placement或FBBIK detail interest
- **THEN** 本帧运行Result MUST保持不变且完整诊断 MAY从下一成功帧开始
- **AND** Runtime MUST不读取Pending页补齐半帧Snapshot

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

`CharacterAnimationPresentationRuntime` MUST为每个Actor使用唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现帧事务。Pose Constraint阶段 MUST预分配两个根Bank，统一持有Foot Placement运行页、Primary Support/Pelvis页、Goal Contribution/Goal Set页、FBBIK BendHistory/紧凑Outcome页与按interest启用的Diagnostics页。根Bank和大页 MUST是预分配引用对象；运行方法 MUST不按值传递完整Bank、Ground Path固定页、FixedList payload或Diagnostics聚合体。

每帧 MUST只读取Committed Bank并写另一Pending Bank。Foot、Pelvis、Goal与Bend不得各自拥有对外Committed identity或由调用方顺序Seal。进入Writer前 MUST完成全部lineage、容量、Goal重复、FBBIK binding、Solver outcome和Writer binding验证；Writer成功后Seal MUST只执行no-throw的根Committed Bank identity切换与已验证journal发布。Discard MUST不切换根identity。大页归属根Bank MUST不允许把其业务数学搬入根Runtime。

存在Diagnostics interest时，Pending Diagnostics页 MUST在进入Writer前从同一Pending Runtime Result完成Foot、Pelvis、Goal与Bend字段的固定容量冻结与验证；Writer成功Apply时 MUST把实际Write Completion与最终Physical Bone位置写入同一Pending页。根Bank切换后只发布该Committed页。Diagnostics投影不得发生在根Bank切换之后，也不得成为修改Committed状态的延迟步骤。

#### Scenario: FBBIK后续阶段失败

- **WHEN** FBBIK已经更新Pending BendHistory但后续Pose stage或Writer验证失败
- **THEN** Committed Foot、Pelvis、Goal与BendHistory MUST全部保持上一成功帧
- **AND** 下一帧FBBIK MUST从上一Committed BendHistory重建Vendor状态

#### Scenario: Vendor存在未建模跨帧状态

- **WHEN** FBBIK Vendor对象中任一字段会影响下一帧结果但不能从Committed BendHistory、Profile和当前Goal精确重建
- **THEN** BendHistory迁移 MUST阻止实施完成并报告该状态所有权
- **AND** Runtime MUST不使用默认值、近似初始化或视觉相似结果替代8fc行为

#### Scenario: 正常提交Pose Constraint Bank

- **WHEN** Foot Placement、Goal Assembler、FBBIK和Final Writer全部通过同一Completion验证
- **THEN** Seal MUST只发布一个新的Committed Bank identity
- **AND** 任一正式读者 MUST不观察到左右脚、盆骨或BendHistory的部分提交

### Requirement: Dense状态与稀疏生命周期必须使用不同暂存策略

每帧完整生成的Dense Pose、velocity、weight、parameter、Native result与Inertialization next state MUST直接写入固定Committed/Pending双页。Action registry、command inbox cursor、sample/Phase cursor、source ownership、usage、retirement与release handshake MUST使用固定容量pending scalar或mutation journal。journal MUST在Evaluate前完成identity、顺序、重复项、容量和依赖验证，Seal MUST只按固定顺序应用已验证mutation。系统 MUST不为了统一API把稀疏Registry整页复制，也 MUST不把Dense Pose降低为逐骨骼托管mutation对象。

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式Animancer Graph Evaluate MUST作为动画表现帧不可逆Barrier。进入Barrier前，Runtime MUST完成Projection/Profile/Rig/World Context、全部托管identity、静态Goal Slot冲突、固定容量、readiness、source、Diagnostics interest/capacity、FinalIK binding和Final Writer binding验证，并且不得声称已经生成依赖同帧Component Pose的Foot Result、Contact Patch、运行时Goal或FBBIK Pending State，也不得提交Foot、Pelvis、BendHistory、Diagnostics或Final Pose。

Animancer Evaluate产生同帧Component Pose后，Barrier内的world-aware Foot Placement、Goal Assembler与FBBIK MUST按编译阶段生成Pending Result、Goal Set、Pending Component Pose与Pending BendHistory，并完成运行时lineage、重复Slot、非有限值与Solver outcome验证。Barrier之后只可按已冻结interest把已完成Pending Result投影到固定Pending Diagnostics页、验证Native/Writer outcome、执行唯一Physical Writer和发布已验证根Bank；不得动态查找、编译、扩容或重新计算Foot Placement业务。Barrier内或之后失败 MUST Discard根Pending Bank并使Actor Runtime进入Faulted；Writer之后若出现Unity引擎异常，系统不得恢复旧Transform后继续运行。

#### Scenario: Barrier前Foot Placement静态准备失败

- **WHEN** Foot Placement的Profile、Rig、World Context、编译容量、静态Goal Slot或binding在进入Barrier前Invalid
- **THEN** Runtime MUST不执行FBBIK或Physical Writer
- **AND** 根Pending Bank MUST被Discard

#### Scenario: Barrier内Foot Placement运行结果失败

- **WHEN** Animancer Evaluate已经产生Component Pose，但Foot Placement Patch、运行时Goal lineage、Goal Assembler或FBBIK outcome在Barrier内Invalid
- **THEN** Runtime MUST阻断后续Pose stage与Physical Writer并Discard根Pending Bank
- **AND** 同一Actor Animation Runtime MUST进入Faulted，不得把该失败降级成可恢复的Barrier前Discard

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

`AnimationFinalPosePhysicalWriter` MUST同时读取当前Committed Final Pose与本帧Pending Final Pose，并在写入任何Physical Bone前验证全部Physical Bone Transform binding、PhysicalBoneCount、Pose availability、continuity identity、graph completion、Goal/FBBIK completion和frame completion。全部合法时 MUST一次写入完整Pending Physical Pose；Invalid时 MUST保持Committed Pose并阻止根Bank发布。

Physical Writer成功之后不得再执行可能失败的Foot、Pelvis、Goal、Bend或Diagnostics业务验证。随后根Bank Seal只可进行no-throw identity切换；Writer抛出Unity引擎异常时Actor MUST进入Faulted，不能伪装成可回滚继续运行。

#### Scenario: Writer成功后发布根Bank

- **WHEN** Writer已经完整写入匹配Completion的Pending Pose
- **THEN** Runtime MUST发布同Completion的Foot、Pelvis、Goal与BendHistory根Bank
- **AND** MUST不在发布前后执行新的业务查询或重新选择Goal

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

### Requirement: Foot Motion数据基础阶段不得改变Runtime动画行为

Definition Build MUST把新增22条Foot Motion Data Curve计入AnimationClip Registered Curve Hash、dependency与Editor质量诊断，但在本change内 MUST不把它们降低为Presentation Projection Runtime payload、Pose Parameter、Foot State输入、Goal、Pelvis或FBBIK配置。

Player Runtime MUST继续使用归档基线的Foot Placement数据与公式，不得读取`Clip Curves`接收器字段、AnimationClip EditorCurve、Library Artifact或未消费Projection字段。后续行为change只有在本change归档后 MAY按独立小步新增正式消费者。

#### Scenario: 新曲线Apply后重建当前产品

- **WHEN** Corin AnimationClip已经Apply合法Foot Motion Curve组并执行当前Definition Build
- **THEN** Projection dependency revision MUST因Registered Curve Hash变化
- **AND** 当前Runtime Foot Goal、状态、Pelvis和FBBIK行为 MUST保持基线逐帧语义

#### Scenario: Player中不存在Editor Artifact

- **WHEN** Player只包含已发布Program与Projection
- **THEN** Player MUST不需要Library Foot Analysis Artifact或`Clip Curves`组件实例
- **AND** 新Foot Motion Curve在没有正式消费者时 MUST不占用Runtime payload

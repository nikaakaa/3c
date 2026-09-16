# character-animation-pipeline Specification

## Purpose

定义 Gameplay Timeline、Presentation Fact、state-local Pose source、有限 Action playback、原生 Pose Graph 与预分配表现帧事务之间的角色动画输出链。

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
- **AND** Gameplay/Ability owner MUST 不创建 Run Timeline producer

### Requirement: Timeline逻辑采样与表现采样必须分离

Gameplay Timeline sampling MUST只按SimulationTick/canonical fraction发生；Action visual sampling与state-local Pose sampling MUST只按PresentationFrame发生。两个Simulation Tick之间的多个PresentationFrame MUST不重复产生TreeClip、Motion、ActionWindow、Cue fact或Effect mutation。Presentation sample MUST不推进CharacterSimulationState的Timeline clock。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** PresentationFrame多次采样同一Action或Pose source
- **THEN** 动画Pose MAY连续变化
- **AND** Gameplay state与facts MUST保持不变

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一原生Pose链

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application seam。其内部唯一`CharacterAnimationPresentationRuntime` MUST消费Committed Body/Intent、Program parameter与有限Action command并构造Presentation Fact，唯一拥有根`CharacterPoseFrameTransaction`，但 MUST只负责actor-local Tuning协调、Frame Lease、固定Module顺序、Animancer Evaluate Barrier、统一Seal/Discard/Fault和外部输入输出装配。根Transaction MUST只保存lineage、阶段、Module lease/result与Outcome，不得保存Module内部Workspace。

正式运行 MUST 由唯一原生 FlowCanvas Pose Graph 实例执行 PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、Local/Component Pose、Constraint 与 Output；唯一 `CharacterPoseSourceModule` 负责 source sample、Animancer/Playable 与物理 source 生命周期；唯一 `CharacterPoseConstraintRuntime` 负责 Foot Placement、PoseBone Goal、Goal Contribution、Assembler、唯一 Goal Set、FBBIK 与 BendHistory；唯一 `CharacterFinalPosePublication` 负责唯一 Committed/Pending Final Pose 物理页与 Physical Writer。图实例 MUST 按固定阶段调用各 Constraint 入口一次，Constraint Module MUST 不扫描 Graph 或维护第二份 Schedule。Module 间 MUST 只交换同 Frame、Completion、Graph、Rig 与 Tuning Generation lineage 的 typed Result。外层 Runtime、ScenePlay 与 Diagnostics MUST 不创建第二 Graph 实例、第二 Action lifecycle、第二 Constraint 事务、第二 Goal Set、第二 FBBIK、第二 Final Pose 页或第二 Writer。

Constraint外部Owner变化 MUST整体保留指定提交ad3527e103cc3235a63e8a1c1dbd26df5155e0ba的动画时钟／混合、Foot、Pelvis、Goal与FBBIK实现、公式、配置和数值顺序；成功Reset MUST保留第一阶段通过后的正式结果，第一阶段批准的Reset差异单独引用证据；本次Goal MUST先完成并验证IK维护重构，再以其通过提交串行接入；第一阶段结构变化与独立Reset修正的证据 MUST保留，总基线不变。其它Foot待办或未归档不构成前置。本change不得恢复旧中央Foot状态机、已撤除业务Reach硬夹紧／末端夹脚、已撤销SmoothKnee或恢复第一阶段已删除的结构，不得接管其它未实施IK行为任务。

#### Scenario: 正常执行Foot Placement与FBBIK

- **WHEN** Program Runtime执行到Foot Placement和PoseBone Goal Operation并由Constraint Module形成合法Goal Set与FBBIK结果
- **THEN** 同一Frame MUST只发布一个Constraint Result、一个Program Output和一个Final Publication Result
- **AND** 外层Runtime MUST不理解Foot Context、Goal workspace或BendHistory

#### Scenario: Source target Pending

- **WHEN** Program发布候选target Demand且Source Module返回Pending
- **THEN** 是否保持当前合法State MUST只由Program节点语义决定并在Barrier前关闭Pending帧
- **AND** Source Module MUST不选择State，外层Runtime MUST不使用旧Timeline或默认Idle补洞

#### Scenario: Goal Slot重复

- **WHEN** 两个Goal Contribution尝试写入同一FBBIK Effector Slot
- **THEN** Constraint Module MUST在Physical Writer前使同一Frame typed Invalid
- **AND** Runtime MUST不按连接顺序覆盖、创建第二Goal Set或绕过FBBIK

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

系统 MUST 在 Frame 开始冻结 Live、Capture、Pose Watch 与 detail interest 及容量。Source、Graph、Constraint 和 Final Publication Module MUST 只在有匹配 interest 时从已完成 Pending Result 向各自固定诊断页深冻结数据；成功 Seal 后，唯一 Diagnostics Projector MUST 从匹配同一 lineage 与 Tuning Generation 的 Committed Result 生成只读 Snapshot。Snapshot MAY 包含 Action lifecycle、source readiness/usage、PoseState、Player、Transition、Slot、Blend、Inertialization、节点、Pose、Goal Contribution、Goal Set、FBBIK、Final Pose 与 Physical 结果，但 MUST 不参与运行计算。

Diagnostics Projector MUST不持有Program Runtime、Source Module、Constraint Module或Final Publication的可变引用，不得读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推，也不得从Animancer weight重建事实。没有interest时 MUST跳过对应大页与逐骨骼复制，但正式执行结果不变。

成功Seal后，Runtime Snapshot或具体`CharacterFootIkCommittedCaptureViewLease` MAY继续服务Live、Trace与Gizmo，但 Foot采样 MUST在同步Commit调用栈内从同一lineage的已提交Owner直接取得Left／Right与公共Fact Root，并以`in`执行一行target-scoped `DiagnosticEvent` partial调用，不得消费Runtime Snapshot、Capture View、万能Committed View、Consumer／Binding或第二事实页。帧开始的可选partial Query只决定是否冻结昂贵事实；Disabled构建中Event／Query调用及参数求值都必须消失。`generated-diagnostic-sampling-framework`唯一拥有通用AOT生成、typed packet、Capability Session、Writer和Host Finalizer合同，`character-foot-ik-diagnostic-sampling`只拥有字段分类、Sampler／Program Definitions和Editor workflow。PoseGraph不得认识Schema、Generated Program、packet、Host或字段映射。旧Foot单体Analyzer／Publisher、Diagnosis Store与旧格式兼容路径直接删除；独立Host-only Foot诊断器 MAY在Capability manifest完成后读取生成Artifact并执行当前Plan、Operator、评分与报告，但不得形成动画Runtime、PoseGraph或采样Session的第二报告状态机。

#### Scenario: 导出每帧调试数据

- **WHEN** 当前Frame成功Seal且存在匹配diagnostics interest
- **THEN** Snapshot MUST只表达同一Frame、Completion、Program、Projection与Rig的Committed结果
- **AND** 关闭或打开调试历史 MUST不改变正式播放、Goal或Final Pose

#### Scenario: Module结果未提交

- **WHEN** Program或Constraint Pending Result完成但后续Writer失败
- **THEN** Diagnostics MUST不发布该Pending结果
- **AND** Projector MUST继续只见上一Committed Snapshot或Actor Fault事实

#### Scenario: Diagnostics interest中途变化

- **WHEN** Editor在当前表现帧中途打开Foot Placement或FBBIK detail interest
- **THEN** 本帧运行Result MUST保持不变且完整诊断 MAY从下一成功帧开始
- **AND** Runtime MUST不读取Pending页补齐半帧Snapshot

#### Scenario: Host分析已封存Artifact

- **WHEN** 动画Runtime已经退出本次Capture且通用Host完成Capability封存
- **THEN** 独立Analyzer MAY读取不可变Artifact产生诊断结果
- **AND** Analyzer MUST不回调动画Runtime、重新求值Pose或改变任何Committed页

### Requirement: 不得恢复Timeline或Preview分裂路径

系统 MUST 只有一条正式 Timeline Runtime 路径和一条原生 Pose Graph 路径。两者只通过 committed Body/Intent、EventId 和有限 Action command 连接；不得保留旧 TimelinePlaybackScheduler、Timeline.Bind/Evaluate/Unbind、自主 TreeClip runtime、AnimationClip root motion、Animancer direct Play 或独立 PlayableGraph。Timeline UI、Pose Graph UI 与 MM UI MUST 只通过 ScenePlay 正式 Actor binding 观察，不得创建 `AnimationPreviewRuntime`、Preview Fixture、Program TreeClip 或独立 Simulation Session。

#### Scenario: Runtime与Preview并存

- **WHEN** ScenePlay 观察 Attack Timeline 且正式 Actor 正在运行
- **THEN** Preview state MUST不影响CharacterSimulationState
- **AND** Live Runtime MUST 独占执行正式 Timeline/Graph domain

### Requirement: Timeline回绕必须完整采样Gameplay边界

Timeline Runtime MUST 在一个 SimulationTick 跨越 loop 边界时按尾段、中间 cycle 和头段稳定采样 Gameplay tracks。Presentation sampler MAY 按 visual time 回绕 Action 动画，但 MUST 不补发 Gameplay facts。持续 Pose source 的 cycle MUST 由 state-local Player 独立维护。

#### Scenario: 一Tick跨越Loop终点

- **WHEN** logic time从cycle尾部前进到下一cycle头部
- **THEN** Timeline Runtime MUST 按正式区间顺序采样两侧 Gameplay segment
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

`CharacterAnimationPresentationRuntime` MUST 为每个 Actor 使用唯一 `Apply Pending Tuning -> Prepare -> Validate -> Animancer Evaluate Barrier -> Seal` 表现帧事务。Runtime 创建时 MUST 从正式 Profile、原生 Graph binding、Rig 与资源容量建立 Actor-local Graph instance，并一次分配 `CharacterPoseActorState`、根 `CharacterPoseFrameTransaction`、Source 页、Constraint Bank、Final Publication 唯一 Committed/Pending Pose 物理页、actor-local Tuning Snapshot、pending scalar state、mutation journal、prepared/deferred source 命令与 interest-gated Diagnostics 页。Graph instance、Source workspace 与 Final Publication MUST 不再分配第二 Final Pose buffer。

每帧 MUST 只读取 Committed Actor/Module 状态并写各 Owner Pending 页。Graph instance 与 actor-local execution view MUST 不保存 Pending/Committed 页、当前 Frame 状态或运行时 Tuning；Actor State MUST 不复制 Source 物理资源、Constraint Bank 或 Final Pose；根 Frame Transaction MUST 只持有 Module typed lease/result，不得成为允许任意 Module 读写的共享黑板。所有 Module MUST 共享根 Frame lineage 与 Tuning Generation 并由唯一 Seal/Discard 决定提交，MUST 不通过 `CaptureState`、`Clone`、`ToArray`、新建 Dictionary/List 或完整旧状态复制建立回滚点。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法Program Image完成普通Presentation Frame
- **THEN** 各Module MUST直接写自己的Pending页并由根事务统一提升同lineage结果
- **AND** 任一读者 MUST不观察到PoseState、Source、Foot、Goal、BendHistory或Final Pose的部分提交

#### Scenario: Evaluate前验证失败

- **WHEN** Pending Frame在Barrier前发现identity、容量、source ownership或binding非法
- **THEN** 根事务 MUST Discard全部Module Pending页、journal和prepared resource
- **AND** Program Image、Committed Actor State、Source ownership、Constraint Bank、Final Pose和Physical Bones MUST保持不变

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

每帧完整生成的 Pose、velocity、weight、parameter、Value、节点 completion、Inertialization next state、Constraint Result 和 Final Pose MUST 直接写各 owner 固定 Committed/Pending 页。PoseState、Player、ActionPlaybackInput lifecycle、Slot 与 Transition 的小型状态 MUST 由原生 Graph instance 和对应 domain owner 保存。Action command cursor、source ownership、usage、retirement 与 release handshake MUST 使用固定容量 mutation journal 或 prepared/deferred resource 命令。在线调参 MUST 使用 Frame 外的 actor-local Graph/Source/Constraint Candidate Snapshot 并一次提升 Tuning Generation，不得写入共享 Graph asset 或混入 Frame journal。

#### Scenario: 本帧只有一个source release

- **WHEN** 当前Frame只释放一个旧source而其它source ownership不变
- **THEN** Source Module MUST只记录对应预验证release mutation与deferred command
- **AND** MUST不复制完整Physical Source Registry或把release字段写进Program Image

#### Scenario: Program产生下一帧Pose

- **WHEN** Program Runtime执行当前Frame全部Pose Operation
- **THEN** MUST把Value与completion直接写入Program Runtime自有Frame Pending页并只向根Transaction返回typed lease/result
- **AND** MUST不先复制上一Committed Value Workspace或通过旧Native Program持有两种寿命

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式 Animancer Graph Evaluate MUST 继续作为动画表现帧不可逆 Barrier。进入 Barrier 前，根 Runtime MUST 先完成 Graph/Source/Constraint Tuning Candidate 原子 Generation 提升，再完成 Graph instance/Profile/Rig/World Context、Module 容量、source readiness/ownership、Diagnostics interest、Constraint 静态 binding、Final Writer binding 和 Frame lineage 验证；Pose Graph MUST 完成 Control 与 Source Demand，Source Module MUST 完成 sample/Playable/capture 准备，但不得提交 Actor State、source ownership、Constraint Bank、Final Pose 或 command acknowledgement。打开 Frame 后 MUST 不改变 Tuning Generation。

Barrier内 MUST按唯一Program Stage Schedule完成source capture、Pose Operation、world-aware Constraint、Goal Assembly、FBBIK、Output和Final Publication。每个Operation MUST由Program Runtime调度一次；每个Constraint Family Operation MUST在自己的Stage位置调用一次Constraint Module对应入口，Constraint `Complete`只验证完整闭包；Writer MUST只由Final Publication执行一次。Barrier之后只可统一提升已验证Pending页、应用journal、acknowledge command、执行deferred release并发布结果；不得动态查找、编译、扩容、再次执行Operation或补算Diagnostics。

#### Scenario: Barrier前Source Module失败

- **WHEN** Source sample或Prepared Resource在Barrier前Invalid
- **THEN** Runtime MUST不调用Animancer Evaluate并Discard全部Pending结果
- **AND** Program Runtime MUST不使用历史sample或默认Playable继续

#### Scenario: Barrier内Constraint失败

- **WHEN** Animancer Evaluate已经产生Component Pose但Constraint Result Invalid
- **THEN** Runtime MUST阻断后续Operation和Final Publication、Discard Pending并使Actor Runtime Faulted
- **AND** MUST不提交已经推进的Program、Source或BendHistory局部状态

#### Scenario: Barrier成功完成

- **WHEN** Program、Source、Constraint和Final Publication Result全部匹配同一lineage并完成
- **THEN** 根Seal MUST只执行预验证的no-throw页切换、journal、acknowledgement与deferred release
- **AND** Writer成功后 MUST不再运行可能失败的动画业务逻辑

#### Scenario: Barrier前Foot Placement静态准备失败

- **WHEN** Foot Placement的Profile、Rig、World Context、编译容量、静态Goal Slot或binding在进入Barrier前Invalid
- **THEN** Runtime MUST不执行FBBIK或Physical Writer
- **AND** 根Pending Bank MUST被Discard

#### Scenario: Barrier内Foot Placement运行结果失败

- **WHEN** Animancer Evaluate已经产生Component Pose，但Foot Placement Patch、运行时Goal lineage、Goal Assembler或FBBIK outcome在Barrier内Invalid
- **THEN** Runtime MUST阻断后续Pose stage与Physical Writer并Discard根Pending Bank
- **AND** 同一Actor Animation Runtime MUST进入Faulted，不得把该失败降级成可恢复的Barrier前Discard

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

唯一 `CharacterFinalPosePublication` MUST 同时拥有当前 Committed Final Pose 物理页、本帧 Pending Final Pose 物理页、完整 Physical Bone binding、Final Writer binding 和 Publication Result。Graph Output MUST 只保存稳定的 Final Publication layout handle，不得包含 Actor 页指针；Actor Runtime 创建时 MUST 由 Final Publication 把它绑定到唯一 Pending 页，原生 Graph instance 通过 actor-local binding 写入 Output Pose 并发布只读 Result，不得在 Graph workspace 保存第二 Final Pose 页。Graph/Runtime MUST 只证明唯一 Output 与 Publication requirement；具体 Writer 唯一性 MUST 由 Runtime Factory 与 Final Publication 构造验证。Final Publication MUST 在写任何 Physical Bone 前验证 PhysicalBoneCount、Pose availability、continuity、Graph completion、Constraint completion、Rig 与 Frame lineage；全部合法时一次写入完整 Pending Physical Pose，Invalid 时保持 Committed Pose 并阻止所有 Pending Module Result 提交。Source Module、Constraint Module、Diagnostics 和外层 Runtime MUST 不写 Physical Transform 或保存第二 Final Pose 真相。

#### Scenario: Pending Pose全部合法

- **WHEN** Graph Output、Constraint Result 和全部 Physical binding 合法
- **THEN** Final Publication MUST在同一Barrier一次写入完整Pending Pose并发布匹配completion的Result
- **AND** 根Seal MUST只提升该Result对应的Program、Source、Constraint与Final Pose页

#### Scenario: Pending Pose无效

- **WHEN** OutputPose、completion或任一Physical binding在Apply前无效
- **THEN** Final Publication MUST不留下部分Pending Physical Pose且根事务 MUST不提交任何Pending Module页
- **AND** Actor Runtime MUST进入现有Faulted路径，不得切换第二Writer、恢复Transform后继续或自动重建Runtime

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

### Requirement: FBBIK初始化和清空历史必须使用正式方向准备结果

FBBIK方向初始化 MUST由现有Rig参考姿态与Profile准备过程精确取得，并保存为项目拥有、身份匹配的typed只读准备结果。准备阶段无法得到合法几何方向时 MUST明确失败，不新增默认世界轴、近似初值、Transform名称搜索或另一套初始化算法。

参考初值与已发生的运行历史 MUST分型。准备结果存在 MUST不能把Stable/Applied历史有效标记提前置为已成立，也不能让可靠动画首帧额外接受历史符号限制；只有实际正式求值产生的方向才能随根Bank成为运行历史。

明确清空Solver历史的初始化、Reset及调参清历史路径 MUST使后续方向仅由当前Pose、Goal、Profile、正式准备结果与根Bank BendHistory决定。Solver MUST在调用Vendor Update前由这些正式输入设置Vendor工作字段，不能在历史为空时读取Vendor上次留下的`bend.direction`或其它可变字段作为历史。

本要求 MUST不扩大Foot Reset与Solver Reset的现有触发范围。普通已有历史帧的方向符号、稳定化算法及权重政策 MUST保持；完全Reset后不再继承旧Vendor方向属于独立行为修正，必须与结构迁移分开记录。

普通帧基线 MUST采用233436恢复的205014保留行为：可靠动画通过原腿轴到目标腿轴的旋转运输有符号膝向，Stable保存运输前动画方向，Applied保存实际请求；退化时继续原有历史分支。初始化准备数据的校验 MUST集中在现有准备入口，不在每层重复验证相同Rig和有限值条件。

#### Scenario: 新建与完全Reset面对同一退化腿姿态

- **WHEN** 新建Runtime与已运行后完全Reset的Runtime具有同一正式准备结果、当前Pose、Goal和Profile，且当前动画腿不足以给出可靠弯曲方向
- **THEN** 两者 MUST从同一正式初始化方向建立本帧方向输入
- **AND** Reset前动作留下的Vendor方向 MUST不影响结果

#### Scenario: Pending求解后未发布

- **WHEN** Vendor已经处理Pending方向但后续阶段未能成功Seal
- **THEN** 下一次合法求解 MUST由上一Committed BendHistory或正式初始化状态重建Vendor输入
- **AND** MUST不继承被丢弃的Vendor工作字段

#### Scenario: 普通历史帧

- **WHEN** 上一Committed BendHistory合法且本帧不触发完全Reset
- **THEN** 本次所有权迁移 MUST保留已接受的有符号膝向运输、退化分支和权重数学
- **AND** MUST不恢复可靠动画半球强翻或已否决的SmoothKnee后处理

#### Scenario: 首帧动画方向可靠

- **WHEN** 正式参考初值已准备但尚无Committed运行历史，当前动画能提供可靠方向
- **THEN** Solver MUST保持原首次帧动画方向选择
- **AND** MUST不把参考初值伪装成上一帧方向后额外翻转或限制本帧方向

#### Scenario: 普通退化帧不能冒充Reset覆盖

- **WHEN** 回放中的退化腿姿态具有合法上一Committed历史
- **THEN** 该记录 MUST只用于普通历史分支的行为保持对账
- **AND** MUST不据此声称已验证空历史初始化或完全Reset边界

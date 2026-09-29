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

Gameplay Timeline sampling MUST只按SimulationTick/canonical fraction发生；Action visual sampling、Presentation TreeClip与state-local Pose sampling MUST只按PresentationFrame发生。两个Simulation Tick之间的多个PresentationFrame MUST不重复产生Logic TreeClip、Motion、ActionWindow、Cue fact或Effect mutation。Presentation sample MUST不推进CharacterSimulationState的Timeline clock。

#### Scenario: 两个逻辑Tick之间多次渲染

- **WHEN** PresentationFrame多次采样同一Action或Pose source
- **THEN** 动画Pose MAY连续变化
- **AND** Gameplay state与facts MUST保持不变

### Requirement: Timeline Track / Clip 执行域必须分离

Timeline Track MUST唯一声明 `Logic` 或 `Presentation`；Clip 与同级 Marker MUST继承所在 Track，MUST NOT提供 Clip 域覆盖或 DualProjection。Logic MUST在 SimulationTick 消费外部逻辑进度并遵守 Commit / Discard；Presentation MUST在表现帧消费外部采样并只产生表现结果。两类轨道 MUST共用正式播放管理者提供的动作进度，表现采样 MUST遵守已有 sample / horizon 合同，不得自行外推未来事件。逻辑结果传给表现 MUST走原提交链。Presentation MUST不执行 Logic TimelineBody 图或写 Gameplay fact；域声明 MUST不成为自主时钟或隐式进度策略开关。

#### Scenario: 表现域 TreeClip 由节点图结束

- **WHEN** Presentation TreeClip 的 TimelineBody 图在当前表现候选帧发出结束片段请求
- **THEN** PresentationFrame MUST 在同一候选帧执行该片段的 OnDisable 并撤下其持续表现输出
- **AND** Commit 与 Discard MUST 同时决定片段结束状态和表现输出是否生效
- **AND** MUST 不修改 Gameplay Timeline 时钟、Logic TreeClip 或 Simulation state

#### Scenario: TreeClip 触发表现事件

- **WHEN** TreeClip 的 Presentation Marker 被视觉游标跨过
- **THEN** PresentationFrame MUST直接产生 Presentation Event
- **AND** 该事件 MUST不等待 SimulationTick
- **AND** Logic TreeClip 的逻辑输出仍 MUST按 Logic Tick 与 Commit / Discard 执行

### Requirement: 表现运行时必须执行唯一原生Pose链

SimulationCommitter与唯一 `CharacterPresentationDomainRuntime` MUST 共同构成 Unity animation application seam。表现运行时 MUST 唯一持有该 Actor 的 Pose 域实例，提供同一表现帧的 frameInput、动作命令及表现时钟，并消费整帧最终结果。DomainSession MUST 将完整帧执行交给 FrameCoordinator；FrameCoordinator MUST 单点执行准备、求值、校验、提交或丢弃的阶段门：preparation 为 Prepared 才能继续 PrepareEvaluation，evaluation 为 Evaluated 才能继续 ValidatePending 与 Commit。任何不满足阶段门的结果 MUST 在协调点按 FailureCode 终止后续阶段，Barrier 前关闭并丢弃 Pending 帧，Barrier 开始后的失败 MUST 按既有 Fault 合同处理，不得伪装物理回滚。外层表现运行时 MUST NOT 再维护一套逐阶段驱动或重复解析同一阶段结果。整帧 lease 与最终成败由 FrameCoordinator 协调，根图及子图仅保留各自实际执行所需的局部状态；正式调用链为 PresentationDomainRuntime → Pose DomainInstance/Session → FrameCoordinator → RoleRuntime（唯一驱动 Source Module 帧生命周期）→ 根 GraphRuntime（FlowCanvas 原生图，状态机子图经 Subgraph 边界派生并共享同一帧谱与模块租约）→ 节点 Handler。只有正式发布成功才允许进入其后的 Constraint 历史提升、Source 提交及动作／时钟确认；失败结果 MUST NOT 继续走成功提交尾部。

正式运行 MUST 由唯一原生 FlowCanvas Pose Graph 实例执行 PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、Local/Component Pose、Constraint 与 Output；唯一 `CharacterPoseSourceModule` 负责 source sample、Animancer/Playable 与物理 source 生命周期，其帧打开、收帧与丢弃 MUST 由 RoleRuntime 单点驱动，模块开帧失败 MUST 丢弃已开图帧并上抛；唯一 `CharacterPoseConstraintRuntime` 负责 Foot Placement、PoseBone Goal、Goal Contribution、Assembler、唯一 Goal Set、FBBIK 与 BendHistory；唯一 `CharacterFinalPosePublication` 负责唯一 Committed/Pending Final Pose 物理页与 Physical Writer。图实例 MUST 按固定阶段调用各 Constraint 入口一次，Constraint Module MUST 不扫描 Graph 或维护第二份 Schedule。Module 间 MUST 只交换同 Frame、Completion、Graph、Rig 与 Tuning Generation lineage 的 typed Result。外层 Runtime、ScenePlay 与 Diagnostics MUST 不创建第二 Graph 实例、第二 Action lifecycle、第二 Constraint 事务、第二 Goal Set、第二 FBBIK、第二 Final Pose 页或第二 Writer。

Constraint外部Owner变化 MUST整体保留指定提交ad3527e103cc3235a63e8a1c1dbd26df5155e0ba的动画时钟／混合、Foot、Pelvis、Goal与FBBIK实现、公式、配置和数值顺序；成功Reset MUST保留第一阶段通过后的正式结果，第一阶段批准的Reset差异单独引用证据；本次Goal MUST先完成并验证IK维护重构，再以其通过提交串行接入；第一阶段结构变化与独立Reset修正的证据 MUST保留，总基线不变。其它Foot待办或未归档不构成前置。本change不得恢复旧中央Foot状态机、已撤除业务Reach硬夹紧／末端夹脚、已撤销SmoothKnee或恢复第一阶段已删除的结构，不得接管其它未实施IK行为任务。

#### Scenario: 正常执行Foot Placement与FBBIK

- **WHEN** Native Pose Graph Runtime执行到Foot Placement和PoseBone Goal Operation并由Constraint Module形成合法Goal Set与FBBIK结果
- **THEN** 同一Frame MUST只发布一个Constraint Result、一个Pose Output和一个Final Publication Result
- **AND** 外层Runtime MUST不理解Foot Context、Goal workspace或BendHistory

#### Scenario: Source target Pending

- **WHEN** Pose Graph Runtime发布候选target Demand且Source Module返回Pending
- **THEN** 是否保持当前合法State MUST只由Graph节点语义决定并在Barrier前关闭Pending帧
- **AND** Source Module MUST不选择State，外层Runtime MUST不使用旧Timeline或默认Idle补洞

#### Scenario: Goal Slot重复

- **WHEN** 两个Goal Contribution尝试写入同一FBBIK Effector Slot
- **THEN** Constraint Module MUST在Physical Writer前使同一Frame typed Invalid
- **AND** Runtime MUST不按连接顺序覆盖、创建第二Goal Set或绕过FBBIK

#### Scenario: 整帧入口遇到准备失败

- **WHEN** 表现运行时提交一帧且原生 Pose 准备未返回 Prepared
- **THEN** 帧协调点 MUST 按正式失败结果关闭已经取得的图帧和模块租约，并返回最终结果
- **AND** 外层 MUST NOT 再次推进该帧求值或以另一条阶段调用路径继续执行

#### Scenario: 最终发布失败

- **WHEN** 同一 Pose 帧最终发布失败
- **THEN** 系统 MUST NOT 将该帧作为成功帧提升 Constraint 历史、提交 Source 或确认动作与表现时钟
- **AND** 已跨过 Animancer Evaluate Barrier 的失败 MUST 进入正式 Fault 状态并保留故障上下文，MUST NOT 宣称已经回滚骨骼写入

### Requirement: 唯一FBBIK必须使用单数运行合同与显式产出证明

`CharacterAnimationPresentationProfile` MUST是FullBodyIK Profile唯一作者Owner；FullBodyIK Pose节点 MUST只表达拓扑且不得保存第二份Profile引用。Compiler MUST从当前Presentation Profile生成Descriptor，Descriptor MUST冻结Profile Id与Revision并在Runtime构造前和当前Profile精确对账。

Pose Constraint Runtime MUST只保存一个Solver、一个Goal Set、一个BendHistory和一个Solver Outcome，不得使用长度为1的Solver、Outcome、Goal Set或Goal Set Index数组。Solver Outcome MUST显式记录Produced、Frame、Completion与Rig lineage；默认值 MUST表示本帧未执行并阻止Physical Writer。

#### Scenario: Solver未执行

- **WHEN** Goal Assembler已经完成但当前Frame与Completion没有产生FBBIK Solver Outcome
- **THEN** Physical Writer前验证 MUST失败并Discard根Pending Bank
- **AND** 默认Result MUST不能被解释为本帧Solver成功

#### Scenario: Profile修改后使用旧Presentation binding

- **WHEN** Descriptor冻结的Profile Revision与当前唯一Profile Revision不一致
- **THEN** Runtime构造 MUST拒绝旧Presentation binding
- **AND** MUST不把旧Plan identity与新Solver参数组合运行

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST 在 Frame 开始冻结 Live、Capture、Pose Watch 与 detail interest 及容量。Source、Graph、Constraint 和 Final Publication Module MUST 只在有匹配 interest 时从已完成 Pending Result 向各自固定诊断页深冻结数据；成功 Seal 后，唯一 Diagnostics Projector MUST 从匹配同一 lineage 与 Tuning Generation 的 Committed Result 生成只读 Snapshot。Snapshot MAY 包含 Action lifecycle、source readiness/usage、PoseState、Player、Transition、Slot、Blend、Inertialization、节点、Pose、Goal Contribution、Goal Set、FBBIK、Final Pose 与 Physical 结果，但 MUST 不参与运行计算。

Diagnostics Projector MUST不持有Native Pose Graph Runtime、Source Module、Constraint Module或Final Publication的可变引用，不得读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推，也不得从Animancer weight重建事实。没有interest时 MUST跳过对应大页与逐骨骼复制，但正式执行结果不变。

成功Seal后，Runtime Snapshot或具体`CharacterFootIkCommittedCaptureViewLease` MAY继续服务Live、Trace与Gizmo，但 Foot采样 MUST在同步Commit调用栈内从同一lineage的已提交Owner直接取得Left／Right与公共Fact Root，并以`in`执行一行target-scoped `DiagnosticEvent` partial调用，不得消费Runtime Snapshot、Capture View、万能Committed View、Consumer／Binding或第二事实页。帧开始的可选partial Query只决定是否冻结昂贵事实；Disabled构建中Event／Query调用及参数求值都必须消失。`generated-diagnostic-sampling-framework`唯一拥有通用AOT生成、typed packet、Capability Session、Writer和Host Finalizer合同，`character-foot-ik-diagnostic-sampling`只拥有字段分类、Sampler／Graph Definitions和Editor workflow。PoseGraph不得认识Schema、Generated Graph artifact、packet、Host或字段映射。旧Foot单体Analyzer／Publisher、Diagnosis Store与旧格式兼容路径直接删除；独立Host-only Foot诊断器 MAY在Capability manifest完成后读取生成Artifact并执行当前Plan、Operator、评分与报告，但不得形成动画Runtime、PoseGraph或采样Session的第二报告状态机。

#### Scenario: 导出每帧调试数据

- **WHEN** 当前Frame成功Seal且存在匹配diagnostics interest
- **THEN** Snapshot MUST只表达同一Frame、Completion、Pose binding与Rig的Committed结果
- **AND** 关闭或打开调试历史 MUST不改变正式播放、Goal或Final Pose

#### Scenario: Module结果未提交

- **WHEN** Pose Graph或Constraint Pending Result完成但后续Writer失败
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

系统 MUST只有一条技能 Timeline 运行路径和一条原生 Pose 图运行路径，通过 committed Body／Intent、EventId 和有限动作请求连接。旧 TimelinePlaybackScheduler、自主 TreeClip runtime、图外 root motion、Animancer direct Play 或独立 PlayableGraph MUST不得恢复。Timeline Preview MUST只通过正式 ScenePlay Session 观察同一表现运行，不创建 Preview 专用 Action adapter、动作状态或姿态执行器；Pose Preview 与 MM Fixture 使用各自 typed 输入适配，不运行另一份姿态执行器。

#### Scenario: Runtime与Preview并存

- **WHEN** 作者在正式 ScenePlay Session 中观察 Attack Timeline
- **THEN** Timeline 的调用、状态和表现 MUST全部来自正在运行的 Corin Program
- **AND** TimelineEditorWindow MUST不创建或修改独立 Preview state，Live Runtime MUST独占执行技能运行操作

### Requirement: Timeline回绕必须完整采样Gameplay边界

正式Timeline Runtime MUST在一个SimulationTick跨越loop边界时按尾段、中间cycle和头段稳定采样Gameplay tracks。Presentation sampler MAY按visual time回绕Action动画，但 MUST不补发Gameplay facts。持续Pose source的cycle MUST由state-local Player独立维护。

#### Scenario: 一Tick跨越Loop终点

- **WHEN** logic time从cycle尾部前进到下一cycle头部
- **THEN** Timeline Runtime MUST按正式区间顺序采样两侧Gameplay segment
- **AND** Presentation MUST不重复提交Window或Cue

### Requirement: 动画command写入与消费权限必须单向

Kernel Finalize MUST只写有限Action的EventId producer select/sample/complete/release command；SimulationCommitter MUST只按已校验OutputDisposition写presentation-owned queue；PresentationFrame MUST在外层事务中原子消费并acknowledge。Portable Core MUST只定义model-neutral command，不引用Unity Animation/Presentation模块；Presentation adapter MUST不反向修改Graph/domain state或Character state。

#### Scenario: 一个RenderFrame前发生多个SimulationTick

- **WHEN** queue包含多个generation的complete/release
- **THEN** PresentationFrame MUST按Tick/Event sequence消费
- **AND** 任一阶段 MUST不双写同一command

### Requirement: 有限Action readiness必须来自第一份合法Sample

Action lifecycle MUST只以所选producer的第一份匹配generation的合法visual sample作为PendingFirstSample到Selected的readiness。Runnable completion、Kernel Finalize或Pipeline Commit MUST不伪造Ready。PoseState readiness MUST独立来自`PresentationPoseSourceSample`的Availability。

#### Scenario: 新Action尚无Sample

- **WHEN** Select已提交但合法Sample未到
- **THEN** Lifecycle MUST保持PendingFirstSample
- **AND** Slot MUST按已绑定播放语义维持当前合法输出

### Requirement: 动画表现帧必须使用预分配暂存事务

每 actor MUST使用唯一调参应用、Prepare、Validate、Animancer Evaluate Barrier 和 Seal 事务。原生图实例和对应节点 MUST在初始化时准备可复用状态与缓冲，跨帧 Committed 历史与本帧 Pending 结果分离；Source、Constraint 和 Final Publication 唯一拥有各自资源与结果页。根事务只保存身份、阶段和 typed 结果；正常帧不得通过复制整个图、状态或骨骼建立回滚点。所有 owner MUST共享帧身份与调参 generation，只有统一提交或丢弃能改变可见结果，不再按 Image 容量清单分配。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法原生图资产完成普通Presentation Frame
- **THEN** 各Module MUST直接写自己的Pending页并由根事务统一提升同lineage结果
- **AND** 任一读者 MUST不观察到PoseState、Source、Foot、Goal、BendHistory或Final Pose的部分提交

#### Scenario: Evaluate前验证失败

- **WHEN** Pending Frame在Barrier前发现identity、容量、source ownership或binding非法
- **THEN** 根事务 MUST Discard全部Module Pending页、journal和prepared resource
- **AND** 原生图资产、Committed Actor State、Source ownership、Constraint Bank、Final Pose和Physical Bones MUST保持不变

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

每帧完整生成的Pose、velocity、weight、parameter、Value、Operation completion、Inertialization next state、Constraint Result和Final Pose MUST直接写各Owner固定Committed/Pending页。PoseState、Player、ActionPlaybackInput lifecycle、Slot与Transition的小型状态 MUST使用原生图实例固定布局的节点小型 Pending 状态。Action command cursor、source ownership、usage、retirement与release handshake MUST使用固定容量mutation journal或prepared/deferred resource命令。在线调参 MUST使用Frame外的actor-local Graph/Source/Constraint Candidate Snapshot并一次提升Tuning Generation，不得写入原生图资产或混入Frame journal。系统 MUST不为了统一Interface复制完整Registry，也 MUST不把Dense Pose、Goal或Operation结果降低为逐项托管mutation。

#### Scenario: 本帧只有一个source release

- **WHEN** 当前Frame只释放一个旧source而其它source ownership不变
- **THEN** Source Module MUST只记录对应预验证release mutation与deferred command
- **AND** MUST不复制完整Physical Source Registry或把release字段写进原生图资产

#### Scenario: Program产生下一帧Pose

- **WHEN** 原生Pose图实例执行当前Frame全部活跃Pose节点
- **THEN** MUST把Value与completion直接写入原生Pose图实例自有Frame Pending页并只向根Transaction返回typed lease/result
- **AND** MUST不先复制上一Committed Value Workspace或通过旧Native Program持有两种寿命

#### Scenario: 本帧只有一个Action生命周期变化

- **WHEN** 当前帧只新增或推进一个Action playback而其它Registry entry不变
- **THEN** Runtime MUST只在固定journal中记录对应mutation
- **AND** MUST不复制完整Action registry或全部source ownership集合

#### Scenario: Pose Graph生成下一帧结果

- **WHEN** Native Pose Graph为当前帧求值全部PoseBone
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

#### Scenario: Pose Graph产生下一帧Pose

- **WHEN** 原生Pose图实例在当前Frame求值全部活跃Pose节点
- **THEN** Job MUST把结果直接写入Pending Native/Pose页
- **AND** MUST不先把Committed Pose页复制为Pending页

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一 Animancer Evaluate MUST继续作为表现帧不可逆 Barrier。进入前 MUST完成 Graph／Source／Constraint 调参候选的原子应用，以及图实例、资源、Rig、World、容量、readiness、观察需求和最终写入绑定检查。原生图准备入口 MUST完成活跃状态与 source demand，Source MUST完成对应采样／Playable／capture 准备，不提前提交任何 actor 状态或资源所有权。Barrier 及随后求值 MUST通过原生节点调用已有算法，各 Constraint 节点只调用一次对应模块，Final Publication 唯一写入。成功后只统一安装已验证 Pending 结果、journal、acknowledgement 和 deferred release；不得临时编译、扩容或重算诊断。Barrier 前失败丢弃，Barrier 内或之后失败阻止 Pending 发布并进入原故障边界。

#### Scenario: Barrier前Source Module失败

- **WHEN** Source sample或Prepared Resource在Barrier前Invalid
- **THEN** Runtime MUST不调用Animancer Evaluate并Discard全部Pending结果
- **AND** 原生Pose图实例 MUST不使用历史sample或默认Playable继续

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

Final Publication MUST唯一拥有 Committed／Pending 最终姿态、完整物理骨骼绑定和 Publication Result。原生 Output 节点通过本 actor 的正式绑定交付结果，不保存编译 Output Family、layout handle 或第二份 Final Pose。静态图校验检查唯一 Output，实例工厂检查唯一 Writer。写入前 MUST验证骨骼数、availability、continuity、图／Constraint 完成状态、Rig 和本帧身份；合法时整体写入，失败时保持原帧事务的丢弃／故障语义。其它模块与诊断 MUST不写 Transform 或保存第二最终姿态。

#### Scenario: Pending Pose全部合法

- **WHEN** Program Output、Constraint Result和全部Physical binding合法
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

新Source Visual、Mixer、Capture Playable、Clip State与物理source slot MAY在Prepare阶段创建为prepared resource，但 MUST不在Seal前取代Committed ownership。Prepare失败 MUST只释放本帧新建prepared resource。Committed source的disconnect、destroy、slot reuse与backend release MUST只由成功帧的固定deferred lifecycle command执行；容量 MUST来自明确图实例与资源绑定并在Runtime创建或Prepare时严格验证，不得动态扩容或回退其它source。

#### Scenario: 新Action source准备后帧失败

- **WHEN** Prepare为新Action创建了Source Visual但原生Pose图没有成功跨过Animancer Evaluate Barrier
- **THEN** Runtime MUST释放本帧prepared resource
- **AND** 原Committed source、usage和slot ownership MUST保持有效

#### Scenario: 旧source获得释放许可

- **WHEN** Slot usage消失、retirement permission和backend release依赖全部在成功帧内匹配
- **THEN** Runtime MUST在Seal后执行唯一deferred release command
- **AND** MUST不在原生Pose图成功前销毁旧Playable或复用其workspace槽位

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

### Requirement: 表现外围业务提交与释放必须遵守同一故障归属

角色表现 owner MUST 将 Pose 不可逆求值及其后的 Timeline、桥、时钟和 Camera 业务收尾纳入同一 Actor 表现故障归属。Pose 返回成功不表示整个表现帧已经完成；之后的业务提交失败 MUST 保留已提交事实、实际失败阶段和原因，并阻止该 Actor 继续下一表现帧，不得以丢弃残余 Pending 宣称物理回滚。Pose 局部阶段与资源所有权 MUST 保持，不得另建外层 Pose 逐阶段执行路径或让多个独立故障状态各自决定恢复。

已提交结果的纯观察输出 MUST 与业务提交区分；观察错误按正式诊断错误通道报告，不得将已完成业务事实改写成回滚成功。停止与故障清理 MUST 按实际资源 owner 尝试释放全部已取得资源；一个释放失败不得跳过其他 owner 或覆盖最初的业务故障，MUST NOT 吞掉失败并报告成功。

#### Scenario: Pose成功后Timeline业务收尾失败

- **WHEN** Pose 和动作确认已经提交，而 Timeline 表现业务收尾失败
- **THEN** 角色表现 owner MUST 保留该阶段失败并使 Actor 表现不可继续
- **AND** MUST NOT 因 Pose 局部 RunFrame 已成功而继续下一帧，或宣称已经回滚骨骼和动作确认

#### Scenario: 已完成结果的观察发布失败

- **WHEN** 已完成业务结果的纯诊断观察发布失败
- **THEN** 系统 MUST 按正式诊断通道记录观察失败并保留真实业务提交状态
- **AND** MUST NOT 把观察异常记录成业务帧已成功回滚

#### Scenario: Session释放失败而Services仍待释放

- **WHEN** 表现 owner 正在释放已取得的 Session 与 Services，而先释放的 owner 抛出异常
- **THEN** 系统 MUST 继续尝试释放剩余 owner，并保留首故障与清理失败信息
- **AND** MUST NOT 因第一个释放异常而保留其余租约或以默认成功结束

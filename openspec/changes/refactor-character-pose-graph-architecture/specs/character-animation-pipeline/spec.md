## MODIFIED Requirements

### Requirement: CharacterSimulationPresentationRuntime必须执行唯一编译Pose Plan

SimulationCommitter与唯一`CharacterSimulationPresentationRuntime` MUST共同构成Unity animation application seam。其内部唯一`CharacterAnimationPresentationRuntime` MUST消费Committed Body/Intent、Program parameter与有限Action command并构造Presentation Fact，唯一拥有根`CharacterPoseFrameTransaction`，但 MUST只负责actor-local Tuning协调、Frame Lease、固定Module顺序、Animancer Evaluate Barrier、统一Seal/Discard/Fault和外部输入输出装配。根Transaction MUST只保存lineage、阶段、Module lease/result与Outcome，不得保存Module内部Workspace。

正式运行 MUST由唯一`CharacterPoseProgramRuntime`执行Projection内部Program Image中的PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、Local/Component Pose、Constraint Family与Output Operation；唯一`CharacterPoseSourceModule`负责source sample、Animancer/Playable与物理source生命周期；唯一`CharacterPoseConstraintRuntime`负责Foot Placement、PoseBone Goal、Goal Contribution、Assembler、唯一Goal Set、FBBIK与BendHistory；唯一`CharacterFinalPosePublication`负责唯一Committed/Pending Final Pose物理页与Physical Writer。Program Runtime MUST在每个Constraint Family Operation位置通过typed编译Handle调用一次对应Constraint入口，Constraint Module MUST不扫描Program或维护第二份Schedule。Module间 MUST只交换同Frame、Completion、Program、Projection、Rig与Tuning Generation lineage的typed Result。外层Runtime、Preview与Diagnostics MUST不创建第二Program Image语义、同一Actor第二Execution View、第二Program State、第二Action lifecycle、第二Operation执行、第二Constraint事务、第二Goal Set、第二FBBIK、第二Final Pose页或第二Writer。

Constraint外部Owner变化 MUST整体保留指定提交ad3527e103cc3235a63e8a1c1dbd26df5155e0ba的动画时钟／混合、Foot、Pelvis、Goal与FBBIK实现、公式、配置、数值顺序和成功Reset结果；Foot／IK未完成或未归档不构成本change前置。本change不得恢复旧中央Foot状态机、已撤除业务Reach硬夹紧／末端夹脚、已撤销SmoothKnee或接管未实施IK维护任务。

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

### Requirement: 动画调试只能读取正式Snapshot

系统 MUST在Frame开始冻结Live、Capture、Pose Watch与detail interest及容量。Source、Program、Constraint和Final Publication Module MUST只在有匹配interest时从已完成Pending Result向各自固定诊断页深冻结数据；成功Seal后，唯一Diagnostics Projector MUST从匹配同一lineage与Tuning Generation的Committed Result生成只读Snapshot。Snapshot MAY包含Action lifecycle、source readiness/usage、PoseState、Player、Transition、Slot、Blend、Inertialization、Operation、Pose、Goal Contribution、Goal Set、FBBIK、Final Pose与Physical结果，但 MUST不参与运行计算。

Diagnostics Projector MUST不持有Program Runtime、Source Module、Constraint Module或Final Publication的可变引用，不得读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推，也不得从Animancer weight重建事实。没有interest时 MUST跳过对应大页与逐骨骼复制，但正式执行结果不变。

新的Runtime Snapshot MUST接回现有唯一Sampler、Analyzer、Publisher、小报告／明细存储与七维评分链。该迁移不改变采样窗口、输入、字段业务含义、规则阈值、评分权重、资格和分母，不新增第二采样／分析／发布或旧格式兼容路径；历史原包 MUST保持。

#### Scenario: 导出每帧调试数据

- **WHEN** 当前Frame成功Seal且存在匹配diagnostics interest
- **THEN** Snapshot MUST只表达同一Frame、Completion、Program、Projection与Rig的Committed结果
- **AND** 关闭或打开调试历史 MUST不改变正式播放、Goal或Final Pose

#### Scenario: Module结果未提交

- **WHEN** Program或Constraint Pending Result完成但后续Writer失败
- **THEN** Diagnostics MUST不发布该Pending结果
- **AND** Projector MUST继续只见上一Committed Snapshot或Actor Fault事实

### Requirement: 动画表现帧必须使用预分配暂存事务

`CharacterAnimationPresentationRuntime` MUST为每个Actor使用唯一`Apply Pending Tuning -> Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现帧事务。Runtime创建时 MUST从Projection内部不可变`CharacterPoseProgramImage`的Capacity Manifest建立该Program Runtime唯一的actor-local只读Execution View，并一次分配`CharacterPoseActorState`、`CharacterPoseProgramFramePages`、根`CharacterPoseFrameTransaction`、Source页、Constraint Bank、Final Publication唯一Committed/Pending Pose物理页、actor-local Tuning Snapshot、pending scalar state、mutation journal、prepared/deferred source命令与interest-gated Diagnostics页。Program Image、Execution View与Program Workspace MUST不再分配第二Final Pose buffer。

每帧 MUST只读取Committed Actor/Module状态并写各Owner Pending页。Program Image与actor-local Execution View MUST不保存Pending/Committed页、当前Frame状态或运行时Tuning；Actor State MUST不复制Source物理资源、Constraint Bank或Final Pose；根Frame Transaction MUST只持有Module typed lease/result，不得成为允许任意Module读写的共享黑板。所有Module MUST共享根Frame lineage与Tuning Generation并由唯一Seal/Discard决定提交，MUST不通过`CaptureState`、`Clone`、`ToArray`、新建Dictionary/List或完整旧状态复制建立回滚点。

#### Scenario: 普通动画表现帧成功

- **WHEN** 一个Actor使用合法Program Image完成普通Presentation Frame
- **THEN** 各Module MUST直接写自己的Pending页并由根事务统一提升同lineage结果
- **AND** 任一读者 MUST不观察到PoseState、Source、Foot、Goal、BendHistory或Final Pose的部分提交

#### Scenario: Evaluate前验证失败

- **WHEN** Pending Frame在Barrier前发现identity、容量、source ownership或binding非法
- **THEN** 根事务 MUST Discard全部Module Pending页、journal和prepared resource
- **AND** Program Image、Committed Actor State、Source ownership、Constraint Bank、Final Pose和Physical Bones MUST保持不变

### Requirement: Dense状态与稀疏生命周期必须使用不同暂存策略

每帧完整生成的Pose、velocity、weight、parameter、Value、Operation completion、Inertialization next state、Constraint Result和Final Pose MUST直接写各Owner固定Committed/Pending页。PoseState、Player、ActionPlaybackInput lifecycle、Slot与Transition的小型状态 MUST使用Program Image固定布局的Program pending state。Action command cursor、source ownership、usage、retirement与release handshake MUST使用固定容量mutation journal或prepared/deferred resource命令。在线调参 MUST使用Frame外的actor-local Program/Source/Constraint Candidate Snapshot并一次提升Tuning Generation，不得写入Program Image或混入Frame journal。系统 MUST不为了统一Interface复制完整Registry，也 MUST不把Dense Pose、Goal或Operation结果降低为逐项托管mutation。

#### Scenario: 本帧只有一个source release

- **WHEN** 当前Frame只释放一个旧source而其它source ownership不变
- **THEN** Source Module MUST只记录对应预验证release mutation与deferred command
- **AND** MUST不复制完整Physical Source Registry或把release字段写进Program Image

#### Scenario: Program产生下一帧Pose

- **WHEN** Program Runtime执行当前Frame全部Pose Operation
- **THEN** MUST把Value与completion直接写入Program Runtime自有Frame Pending页并只向根Transaction返回typed lease/result
- **AND** MUST不先复制上一Committed Value Workspace或通过旧Native Program持有两种寿命

### Requirement: Animancer Evaluate必须是唯一不可逆提交门槛

唯一正式Animancer Graph Evaluate MUST继续作为动画表现帧不可逆Barrier。进入Barrier前，根Runtime MUST先完成Program/Source/Constraint Tuning Candidate原子Generation提升，再完成Program Image/Execution View/Profile/Rig/World Context、Module容量、source readiness/ownership、Diagnostics interest、Constraint静态binding、Final Writer binding和Frame lineage验证；Program Runtime MUST完成Control与Source Demand，Source Module MUST完成sample/Playable/capture准备，但不得提交Actor State、source ownership、Constraint Bank、Final Pose或command acknowledgement。打开Frame后 MUST不改变Tuning Generation。

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

### Requirement: Final Pose写入必须在整Rig验证后原子选择Committed或Pending结果

唯一`CharacterFinalPosePublication` MUST同时拥有当前Committed Final Pose物理页、本帧Pending Final Pose物理页、完整Physical Bone binding、Final Writer binding和Publication Result。Program Image的Output Family MUST只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不得包含Actor页指针；Actor Runtime创建时 MUST由Final Publication把它绑定到唯一Pending页，Program Runtime通过actor-local binding写入Output Pose并发布只读Result，不得在Program Workspace保存第二Final Pose页。Compiler MUST只证明唯一Output与Publication requirement；具体Writer唯一性 MUST由Runtime Factory与Final Publication构造验证。Final Publication MUST在写任何Physical Bone前验证PhysicalBoneCount、Pose availability、continuity、Program completion、Constraint completion、Rig与Frame lineage；全部合法时一次写入完整Pending Physical Pose，Invalid时保持Committed Pose并阻止所有Pending Module Result提交。Source Module、Constraint Module、Diagnostics和外层Runtime MUST不写Physical Transform或保存第二Final Pose真相。

#### Scenario: Pending Pose全部合法

- **WHEN** Program Output、Constraint Result和全部Physical binding合法
- **THEN** Final Publication MUST在同一Barrier一次写入完整Pending Pose并发布匹配completion的Result
- **AND** 根Seal MUST只提升该Result对应的Program、Source、Constraint与Final Pose页

#### Scenario: Pending Pose无效

- **WHEN** OutputPose、completion或任一Physical binding在Apply前无效
- **THEN** Final Publication MUST不留下部分Pending Physical Pose且根事务 MUST不提交任何Pending Module页
- **AND** Actor Runtime MUST进入现有Faulted路径，不得切换第二Writer、恢复Transform后继续或自动重建Runtime

## ADDED Requirements

### Requirement: 动画表现根必须只编排typed Pose Frame结果

唯一`CharacterAnimationPresentationRuntime` MUST只拥有表现Frame Lease、固定Module调用顺序、Animancer Evaluate Barrier、统一Seal/Discard/Fault和外部输入输出装配。它 MUST通过typed Demand、Prepared、Result和Publication合同调用Pose Program、Source、Constraint与Final Publication Module；MUST不保存Node业务状态、不解释Operation Payload、不索引内部Workspace，也 MUST不实现source选择、blend、Foot、Goal、FBBIK或Writer数学。

#### Scenario: 正常执行完整表现帧

- **WHEN** 当前Frame的Presentation Fact、Action输入、Projection、Rig、Source与World Context全部合法
- **THEN** 根Runtime MUST按固定阶段交换同一lineage的typed结果并只执行一次统一Seal
- **AND** 调用方 MUST不取得任何Module内部页或逐个提交节点状态

#### Scenario: 根Runtime需要读取Goal offset

- **WHEN** 新实现要求根Runtime读取Goal offset、Operation index或Foot Context才能继续调度
- **THEN** 架构校验 MUST把该依赖视为Module Interface泄露并拒绝收口
- **AND** 对应知识 MUST迁入Program Runtime或Constraint Module唯一Owner

### Requirement: Pose运行必须形成四个唯一业务Owner

系统 MUST使用唯一`CharacterPoseProgramRuntime`拥有逻辑节点与Operation执行，唯一`CharacterPoseSourceModule`拥有source sample与物理Playable生命周期，唯一`CharacterPoseConstraintRuntime`拥有Foot/Goal/FBBIK，唯一`CharacterFinalPosePublication`拥有Final Pose与Physical Writer。Diagnostics MUST只作为Committed Result Projector存在。任何业务状态、结果页或写入动作 MUST恰有一个Owner，不得由外层协调器、Preview或Diagnostics复制。

#### Scenario: Constraint Family Operations执行

- **WHEN** Program Stage依次到达Foot Placement、PoseBone Contribution、Goal Assembler与FBBIK Operation
- **THEN** Program Runtime MUST在每个Operation位置通过对应typed编译Handle恰好调用一次Constraint Runtime入口并写入各自唯一completion
- **AND** Constraint Module MUST不扫描Program或维护第二份Stage Schedule，Source Module、外层Runtime与Diagnostics MUST不再次执行或修改Constraint结果

#### Scenario: 最终Physical Pose写入

- **WHEN** Program Output和Constraint completion均合法
- **THEN** Final Publication MUST是唯一能够写Physical Bones的Module
- **AND** Program Runtime与Constraint Runtime MUST只发布数据结果而不直接写Transform

### Requirement: Program Image、Actor State与Frame Transaction必须完全分型

`CharacterPoseProgramImage` MUST是由Projection Build发布并保存在`CharacterPresentationProjection`内部的唯一不可变Pose程序，只保存schema、identity、Rig、Stage、Operation Header、Family Payload、typed Value layout、Workspace layout、Source Map和容量。Runtime MUST不从Projection复制、转换或构造第二Native Program容器。`CharacterPoseActorState` MUST只保存Pose Program逻辑节点的跨帧Committed状态。`CharacterPoseFrameTransaction` MUST只保存当前Frame的Pending控制、Value、completion、Module Result引用、journal和可选Diagnostics页。三者 MUST不互相复制不同寿命的真相。

#### Scenario: 同一Program供两个Actor使用

- **WHEN** 两个Actor引用同一Projection和Program Image
- **THEN** 两者 MUST共享同一不可变Program Image并各自拥有独立Actor State与Frame Transaction
- **AND** 一个Actor的PoseState、Inertialization或Fault MUST不修改Program Image或另一Actor状态

#### Scenario: Frame被Discard

- **WHEN** 当前Frame在Barrier前失败并Discard
- **THEN** Program Image和Committed Actor State MUST保持不变
- **AND** 被丢弃Frame的Value、completion和Module Result MUST不能被下一帧或Diagnostics读取

### Requirement: Frame数据必须通过唯一写入页和typed只读View单向流动

每个Frame页 MUST声明唯一写入Owner和合法读取阶段。Module间 MUST只交换带Frame、Completion、Program、Projection和Rig lineage的typed只读View；不得使用共享无类型黑板、公开NativeArray集合、反射查找或调用方约定offset传递业务结果。Frame Transaction MAY持有预分配页，但任何Module MUST只能写入其正式Owned页。

#### Scenario: Source结果进入Program

- **WHEN** Source Module完成当前Demand的sample、binding与readiness
- **THEN** Program Runtime MUST只通过`CharacterPoseSourceFrameResult`只读View消费结果
- **AND** Program Runtime MUST不直接修改Physical Source Registry或Source Module pending页

#### Scenario: lineage不匹配

- **WHEN** 下游收到不同Frame、Completion、Program、Projection或Rig的Result View
- **THEN** 当前Frame MUST在写入Final Pose前失败
- **AND** 系统 MUST不重标身份、复制到当前页或使用上一帧结果补洞

### Requirement: Pose Program Runtime必须是唯一Operation执行Owner

`CharacterPoseProgramRuntime` MUST按Program Image的Stage Schedule执行每个Operation恰好一次，并在唯一Operation Completion页记录结果。Foot Placement、PoseBone Contribution、Goal Assembler与FBBIK等Constraint Family MUST各自在自己的Operation位置通过typed编译Handle调用Constraint Module一次；Constraint `Complete`只能验证完整闭包并发布最终Constraint Result，不得重新执行Operation。外层Runtime MUST不预执行World-aware Operation，Source Module MUST不扫描Operation决定逻辑状态，Constraint Module MUST不反向扫描Program或拥有第二Schedule，Diagnostics与Pose Watch MUST不重放Operation。Program Runtime MUST使用持久Executor Implementation，不得每帧通过巨型构造重新展开Program和Workspace全部页。

#### Scenario: World-aware Foot节点

- **WHEN** Stage Schedule包含一个Foot Placement Operation
- **THEN** Program Runtime MUST在该Operation位置调用一次Constraint Module并写入唯一completion
- **AND** 后续FBBIK MUST消费该次结果而不是外层提前写入的副本

#### Scenario: 同一Operation重复完成

- **WHEN** 任一路径尝试在同一Frame第二次写入同一Operation completion
- **THEN** Program Runtime MUST使当前Frame Invalid并阻止Final Publication
- **AND** MUST不按最后写入覆盖第一次结果

### Requirement: Source Module必须独占物理source与release生命周期

`CharacterPoseSourceModule` MUST只消费typed Source Demand与Program发布的Usage/Retirement Permission，唯一拥有Clip、Blend Space、Motion Matching和Action sample Adapter、Animancer/Playable source、Physical Pose Source Registry、capture binding、prepared resource、deferred release及release completion。PoseState、Player endpoint、Transition、Slot、Blend Stack和Inertialization的逻辑状态 MUST仍由Program Runtime拥有；Source Module MUST不决定State、cross-source weight或OutputPose。

#### Scenario: PoseState target等待首份sample

- **WHEN** Program Runtime为候选target发布Demand且Source Adapter返回Pending
- **THEN** Source Module MUST发布typed Pending而不伪造sample
- **AND** 是否保持当前State与是否启动Transition MUST只由Program Runtime按正式节点语义决定

#### Scenario: 旧source获得释放许可

- **WHEN** Program Runtime发布匹配identity的retirement permission且完整Frame最终成功
- **THEN** Source Module MUST在Seal后的deferred release阶段完成唯一物理释放并发布completion
- **AND** 其它Module MUST不提前disconnect、destroy或复用该source slot

### Requirement: Constraint Module Interface不得泄露Program布局

`CharacterPoseConstraintRuntime` MUST只接收typed Component Pose、Constraint Frame facts、固定编译Handle和共享lineage，并只输出`CharacterPoseConstraintResult`。Foot Placement、PoseBone Goal、Goal Contribution、唯一Assembler、唯一Goal Set、FBBIK、BendHistory与Solver Outcome MUST全部属于其Implementation。外部Interface MUST不出现NativeSlice、Goal offset/count、Operation index、Callsite index、Foot Context、Bank内部页或Diagnostics页。

#### Scenario: Foot与PoseBone Goal共同求解

- **WHEN** 当前Component Pose和Frame facts产生多个合法Goal Contribution
- **THEN** Constraint Module MUST内部完成唯一Assembly与FBBIK并发布一个Constraint Result
- **AND** Program Runtime MUST不理解Contribution workspace或BendHistory布局

#### Scenario: Constraint失败

- **WHEN** Goal lineage、重复Slot或Solver Outcome Invalid
- **THEN** Constraint Result MUST携带匹配Frame的typed失败并阻止后续Program Operation与Publication
- **AND** Constraint Module MUST不发布部分Goal、部分BendHistory或独立Committed identity

### Requirement: Final Pose Publication必须原子拥有最终结果与Physical写入

`CharacterFinalPosePublication` MUST唯一拥有Committed/Pending Final Pose物理页、完整Physical Bone binding、Final Writer binding、整Rig预验证、唯一Apply和Publication Result。Program Image的Output Family MUST只保存指向Publication Pending页的typed write handle；Program Runtime通过该handle写入Output Pose并发布只读`ProgramOutputPoseResult`，MUST不在Program Workspace分配第二Final Pose buffer。Final Publication MUST在写任何Physical Bone前验证Pose availability、Rig、continuity、Program completion、Constraint completion和Frame lineage；合法时一次写入完整Pending Pose，非法时保持Committed Pose并返回正式失败。当前只有一个Writer Implementation，系统 MUST不建立第二Writer、第二Final Pose页、图外Transform写入或运行时Writer选择。

#### Scenario: Pending Pose完整合法

- **WHEN** 当前Program Result、Constraint Result和Final Pose全部匹配同一lineage
- **THEN** Final Publication MUST一次写入全部Physical Bones并发布匹配completion的Result
- **AND** Writer成功后 MUST不再执行可能失败的动画业务计算

#### Scenario: 一个Physical binding无效

- **WHEN** 任一Physical Bone binding在Apply前无效
- **THEN** Final Publication MUST不写入任何Pending Physical Bone
- **AND** 当前Frame MUST遵守Barrier后的Fault政策而不得切换第二Writer或恢复后继续

### Requirement: 所有Module必须服从唯一表现帧事务和Barrier

每个Actor MUST继续使用唯一`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现事务。Module MAY拥有内部预分配双页、pending state、journal和prepared resource，但 MUST共享根Frame lineage并只由根事务决定Seal/Discard。Barrier前失败 MUST丢弃Pending且保持Committed；Barrier内或之后失败 MUST阻止Pending发布并使Actor Animation Runtime进入Faulted；Writer成功后的Seal MUST只执行已验证的no-throw页切换、journal、acknowledgement与deferred release。

#### Scenario: Source准备阶段失败

- **WHEN** Source Module在Barrier前报告Invalid binding或容量不足
- **THEN** 根事务 MUST Discard全部Program、Source、Constraint和Publication Pending结果
- **AND** Animancer Evaluate与Physical Writer MUST不执行

#### Scenario: FBBIK在Barrier内失败

- **WHEN** Animancer Evaluate已经产生同帧Pose但Constraint Module报告Solver Invalid
- **THEN** 根事务 MUST阻止Final Publication、Discard Pending并使Actor Runtime Faulted
- **AND** MUST不只回滚Constraint后继续提交Source或Program状态

### Requirement: Diagnostics必须只投影Committed typed Result

系统 MUST在Frame开始冻结Diagnostics interest和容量，并只在有interest时从Module Pending Result向预分配诊断页深冻结允许观察的数据。成功Seal后，`CharacterPoseDiagnosticsProjector` MUST只读取匹配同一lineage的Committed Source、Program、Constraint和Final Publication Result；MUST不持有Runtime Module引用、不读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推结果，也 MUST不参与任何运行决定。

#### Scenario: 同时观察Player、Foot和FBBIK

- **WHEN** 当前Frame开始时存在对应Pose Watch和detail interest且Frame成功Seal
- **THEN** Projector MUST从同一Committed lineage发布Player Pose、Foot Contribution、Goal Set、FBBIK Pose和Physical结果
- **AND** MUST不重新采样source、执行world query或调用FBBIK

#### Scenario: interest在Frame中途打开

- **WHEN** Editor在当前Frame开始之后增加detail interest
- **THEN** 当前运行结果 MUST保持不变且完整详情 MAY从下一成功Frame开始
- **AND** Projector MUST不读取Pending页补齐半帧Snapshot

### Requirement: Preview与正式Runtime必须复用同一Module Factory和Program Image

Pose Graph Preview与正式Runtime MUST使用同一Program Image schema、Program Runtime、Source Module、Constraint Module、Final Publication、Frame Transaction和completion语义。两者 MAY通过正式Adapter提供不同Presentation Fact、Action、World Context、source sample与Physical Rig host；Preview MUST不创建简化Executor、临时Program、第二PlayableGraph、默认Foot结果或Stale Projection fallback。

#### Scenario: Preview缺少world context

- **WHEN** Preview执行到需要精确World Context的Foot Placement Operation但Adapter不可用
- **THEN** 同一Program Runtime MUST发布typed Unavailable并停止该Frame publication
- **AND** Preview MUST不跳过Constraint或伪造地面结果

### Requirement: Reset、Replacement与Dispose必须按Owner清理状态

Program replacement、Projection revision变化、Preview非连续seek、Actor reset、Fault和Dispose MUST由根Runtime生成typed reset reason，并按固定顺序让Program Runtime、Source Module、Constraint Module和Final Publication各自清理Owned状态。Reset MUST提升相关generation并使旧Frame lease、source completion、constraint result和diagnostics失效；MUST不由一个Module直接清空另一Module内部页，也 MUST不保留旧Program reader或source资源fallback。

#### Scenario: Projection被显式重建

- **WHEN** Actor从旧Program Image切换到新Projection revision
- **THEN** 旧Actor State、Frame lease和延迟completion MUST失效，旧source资源 MUST按正式dispose/release顺序关闭
- **AND** 新Runtime MUST只从新Program Image和新容量建立状态，不迁移旧ABI字段

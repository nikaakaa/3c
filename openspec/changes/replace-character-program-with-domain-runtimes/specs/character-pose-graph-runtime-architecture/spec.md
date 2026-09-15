## MODIFIED Requirements

### Requirement: 动画表现根必须只编排typed Pose Frame结果

唯一动画表现宿主 MUST拥有表现帧身份、手动图驱动、Source 准备、Animancer Evaluate Barrier、统一提交／丢弃／故障、调参协调及输入输出装配。它 MUST通过 typed 输入、阶段和结果调用原生图、Source、Constraint 和 Final Publication，不解释节点 payload、不保存节点私有状态、不建立另一份操作计划，也不重做采样、混合或 IK 数学。

#### Scenario: 正常执行完整表现帧

- **WHEN** 当前Frame的Presentation Fact、Action输入、动画绑定、Rig、Source与World Context全部合法
- **THEN** 根Runtime MUST按固定阶段交换同一lineage与Tuning Generation的typed结果并只执行一次统一Seal
- **AND** 调用方 MUST不取得任何Module内部页或逐个提交节点状态

#### Scenario: 根Runtime需要读取Goal offset

- **WHEN** 新实现要求根Runtime读取Goal offset、Operation index或Foot Context才能继续调度
- **THEN** 架构校验 MUST把该依赖视为Module Interface泄露并拒绝收口
- **AND** 对应知识 MUST迁入原生Pose图运行实例或Constraint Module唯一Owner


### Requirement: Pose运行必须形成四个唯一业务Owner

Pose 原生图实例 MUST唯一拥有 PoseState、Player、ActionPlaybackInput、Slot、Blend 和 Inertialization 的逻辑状态与节点调用；Source MUST唯一拥有采样与物理资源，Constraint MUST唯一拥有 Foot／Goal／FBBIK，Final Publication MUST唯一拥有最终姿态与物理写入。每项状态和结果 MUST只有一个 owner，外层、预览和诊断不得复制或再次执行。

#### Scenario: Constraint Family Operations执行

- **WHEN** 原生求值阶段依次到达Foot Placement、PoseBone Contribution、Goal Assembler与FBBIK Operation
- **THEN** 原生Pose图运行实例 MUST在每个节点位置通过对应实例绑定的typed handle恰好调用一次Constraint Runtime入口并写入各自唯一completion
- **AND** Constraint Module MUST不扫描图或维护第二份执行顺序，Source Module、外层Runtime与Diagnostics MUST不再次执行或修改Constraint结果

#### Scenario: 最终Physical Pose写入

- **WHEN** 原生图输出和Constraint completion均合法
- **THEN** Final Publication MUST是唯一能够写Physical Bones的Module
- **AND** 原生Pose图运行实例与Constraint Runtime MUST只发布数据结果而不直接写Transform


### Requirement: Program Image、Execution View、Actor State、Owned Frame页与根Transaction必须完全分型

原生作者图和资源 MUST只读，角色／子图调用实例 MUST独立保存逻辑节点跨帧状态，本帧 Pending 结果与缓存 MUST和 Committed 历史分开。Source、Constraint 与 Final Publication MUST继续各自拥有物理资源和结果页，根事务只保存阶段／身份与 typed 结果。系统 MUST不再创建 ProgramImage、其 ExecutionView、全图操作表或编译工作区；原生实例初始化只做端口、资源和复用缓冲绑定。

#### Scenario: 同一Program供两个Actor使用

- **WHEN** 两个Actor引用同一动画绑定和原生图资产
- **THEN** 两者 MUST共享同一不可变原生图资产，并各自拥有独立实例绑定、Actor State、Program Frame Pages、Module页与根Frame Transaction
- **AND** 一个Actor的PoseState、Inertialization或Fault MUST不修改原生图资产或另一Actor状态

#### Scenario: Frame被Discard

- **WHEN** 当前Frame在Barrier前失败并Discard
- **THEN** 原生图资产和Committed Actor State MUST保持不变
- **AND** 被丢弃Frame的Value、completion和Module Result MUST不能被下一帧或Diagnostics读取


### Requirement: Frame数据必须通过唯一写入页和typed只读View单向流动

每个Frame页 MUST声明唯一写入Owner和合法读取阶段。Module间 MUST只交换带Frame、Completion、图调用实例、动画绑定、Rig和Tuning Generation lineage的typed只读View；不得使用共享无类型黑板、公开NativeArray集合、反射查找或调用方约定offset传递业务结果。根Frame Transaction MUST不持有或索引Module内部页；任何Module MUST只能写入其正式Owned页并向根Transaction返回typed lease/result。

#### Scenario: Source结果进入Program

- **WHEN** Source Module完成当前Demand的sample、binding与readiness
- **THEN** 原生Pose图运行实例 MUST只通过`CharacterPoseSourceFrameResult`只读View消费结果
- **AND** 原生Pose图运行实例 MUST不直接修改Physical Source Registry或Source Module pending页

#### Scenario: lineage不匹配

- **WHEN** 下游收到不同Frame、Completion、图调用实例、动画绑定或Rig的Result View
- **THEN** 当前Frame MUST在写入Final Pose前失败
- **AND** 系统 MUST不重标身份、复制到当前页或使用上一帧结果补洞


### Requirement: 校验必须按输入边界唯一分责

显式作者校验 MUST负责类型、引用、递归、唯一输出和静态写冲突；实例创建或替换 MUST负责真实 Rig／资源／空间绑定与缓冲容量。帧内跨 owner 交接 MUST只校验本次身份、readiness、generation、动态用量和完成状态。最终输出 MUST检查完整姿态及物理绑定；下游不得重复整图扫描或重新求解 Foot／Goal／FBBIK。

#### Scenario: Build已经证明静态布局

- **WHEN** Runtime已加载并绑定同identity的原生图资产且本帧未更换产物
- **THEN** Operation执行 MUST直接使用该固定布局，只检查自己接收的本帧变化事实
- **AND** 根Runtime、Module与Executor MUST不分别重跑同一静态拓扑／布局验证


### Requirement: Pose 原生Pose图实例必须是唯一Operation执行Owner

原生 Pose 图运行实例 MUST通过原生节点及连接执行当前活跃图，按调用实例和阶段缓存结果。Foot、Goal、Assembler、FBBIK MUST在其正式节点位置恰好调用 Constraint 一次，Complete 只检查结果闭合。Source、Constraint、外层和 Diagnostics MUST不扫描或再次执行图；宿主 MUST不生成 Stage／Operation 计划或每帧重建整图实例。

#### Scenario: World-aware Foot节点

- **WHEN** 原生图包含一个活跃Foot Placement节点
- **THEN** 原生Pose图运行实例 MUST在该节点位置调用一次Constraint Module并写入唯一completion
- **AND** 后续FBBIK MUST消费该次结果而不是外层提前写入的副本

#### Scenario: 同一Operation重复完成

- **WHEN** 任一路径尝试在同一Frame第二次写入同一Operation completion
- **THEN** 原生Pose图运行实例 MUST使当前Frame Invalid并阻止Final Publication
- **AND** MUST不按最后写入覆盖第一次结果


### Requirement: Source Module必须独占物理source与release生命周期

`CharacterPoseSourceModule` MUST只消费typed Source Demand与原生图发布的Usage/Retirement Permission，唯一拥有Clip、Blend Space、Motion Matching和Action sample Adapter、Animancer/Playable source、Physical Pose Source Registry、capture binding、prepared resource、deferred release及release completion。PoseState、Player endpoint、ActionPlaybackInput lifecycle、Transition、Slot、Blend Stack和Inertialization的逻辑状态 MUST仍由原生Pose图运行实例拥有；Source Module MUST只返回Action sample readiness而不得仲裁Action winner、推进lifecycle、决定State、cross-source weight或OutputPose。

#### Scenario: PoseState target等待首份sample

- **WHEN** 原生Pose图运行实例为候选target发布Demand且Source Adapter返回Pending
- **THEN** Source Module MUST发布typed Pending而不伪造sample
- **AND** 是否保持当前State与是否启动Transition MUST只由原生Pose图运行实例按正式节点语义决定

#### Scenario: 旧source获得释放许可

- **WHEN** 原生Pose图运行实例发布匹配identity的retirement permission且完整Frame最终成功
- **THEN** Source Module MUST在Seal后的deferred release阶段完成唯一物理释放并发布completion
- **AND** 其它Module MUST不提前disconnect、destroy或复用该source slot


### Requirement: Constraint Module Interface不得泄露Program布局

`CharacterPoseConstraintRuntime` MUST只接收typed Component Pose、Constraint Frame facts、实例绑定的typed handle和共享lineage，并只输出`CharacterPoseConstraintResult`。Foot Placement、PoseBone Goal、Goal Contribution、唯一Assembler、唯一Goal Set、FBBIK、BendHistory与Solver Outcome MUST全部属于其Implementation。外部Interface MUST不出现NativeSlice、Goal offset/count、Operation index、Callsite index、Foot Context、Bank内部页或Diagnostics页。

#### Scenario: Foot与PoseBone Goal共同求解

- **WHEN** 当前Component Pose和Frame facts产生多个合法Goal Contribution
- **THEN** Constraint Module MUST内部完成唯一Assembly与FBBIK并发布一个Constraint Result
- **AND** 原生Pose图运行实例 MUST不理解Contribution workspace或BendHistory布局

#### Scenario: Constraint失败

- **WHEN** Goal lineage、重复Slot或Solver Outcome Invalid
- **THEN** Constraint Result MUST携带匹配Frame的typed失败并阻止后续Program Operation与Publication
- **AND** Constraint Module MUST不发布部分Goal、部分BendHistory或独立Committed identity


### Requirement: Final Pose Publication必须原子拥有最终结果与Physical写入

Final Publication MUST唯一拥有 Committed／Pending 最终姿态、完整物理骨骼绑定、整 Rig 检查与唯一 Writer。原生 Output 节点 MUST通过对应 actor 的正式绑定发布结果，不另分配第二份最终姿态页。写骨骼前 MUST检查姿态可用性、Rig、连续性、原生图／Constraint 完成状态及本帧身份；合法时整体提交，失败时遵守正式丢弃／故障合同。静态校验负责唯一 Output 与 Final Publication 要求，实例工厂负责实际 Writer 唯一性，不新增 Writer 节点或运行时 Writer 选择。

#### Scenario: Pending Pose完整合法

- **WHEN** 当前Program Result、Constraint Result和Final Pose全部匹配同一lineage
- **THEN** Final Publication MUST一次写入全部Physical Bones并发布匹配completion的Result
- **AND** Writer成功后 MUST不再执行可能失败的动画业务计算

#### Scenario: 一个Physical binding无效

- **WHEN** 任一Physical Bone binding在Apply前无效
- **THEN** Final Publication MUST不写入任何Pending Physical Bone
- **AND** 当前Frame MUST遵守Barrier后的Fault政策而不得切换第二Writer或恢复后继续


### Requirement: 所有Module必须服从唯一表现帧事务和Barrier

每个Actor MUST继续使用唯一`Apply Pending Tuning -> Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`表现事务。Tuning Candidate MUST在根Frame打开前完成原子Generation提升；Frame内不得变更Tuning Generation。Module MAY拥有内部预分配双页、pending state、journal和prepared resource，但 MUST共享根Frame lineage与Tuning Generation并只由根事务决定Seal/Discard。Barrier前失败 MUST丢弃Pending且保持Committed；Barrier内或之后失败 MUST阻止Pending发布并使Actor Animation Runtime进入Faulted；Writer成功后的Seal MUST只执行已验证的no-throw页切换、journal、acknowledgement与deferred release。

#### Scenario: Source准备阶段失败

- **WHEN** Source Module在Barrier前报告Invalid binding或容量不足
- **THEN** 根事务 MUST Discard全部Graph、Source、Constraint和Publication Pending结果
- **AND** Animancer Evaluate与Physical Writer MUST不执行

#### Scenario: FBBIK在Barrier内失败

- **WHEN** Animancer Evaluate已经产生同帧Pose但Constraint Module报告Solver Invalid
- **THEN** 根事务 MUST阻止Final Publication、Discard Pending并使Actor Runtime Faulted
- **AND** MUST不只回滚Constraint后继续提交Source或Program状态


### Requirement: 在线调参必须使用actor-local原子Snapshot

原生图资产和配置 MUST只保存默认值。每个 actor MUST拥有独立调参快照和单调 generation，按 Graph／Source／Constraint 分区，在打开帧前完整检查身份、容量、值域和 reset owner 要求后一次安装。失败 MUST保持全部已提交快照和节点状态；不得修改共享资产或另一 actor。

#### Scenario: 一个Constraint调参字段非法

- **WHEN** 原生图与Source Candidate合法但Constraint Candidate包含非法值
- **THEN** 根Runtime MUST拒绝整个Tuning Generation且三个Module继续使用上一Committed Snapshot
- **AND** 下一Frame MUST不观察到部分Program/Source新值或被重置一半的Owner状态

#### Scenario: 两个Actor共享同一Program Image

- **WHEN** 作者只对Actor A应用新的Pose Tuning Block
- **THEN** Actor A MUST提升自己的Tuning Generation，Actor B、共享原生图资产与两个actor-local 实例绑定 MUST保持不变
- **AND** Runtime与Preview MUST保持现行字段、生效时机与`resetOwnerState`语义


### Requirement: Diagnostics必须只投影Committed typed Result

系统 MUST在Frame开始冻结Diagnostics interest和容量，并只在有interest时从Module Pending Result向预分配诊断页深冻结允许观察的数据。成功Seal后，`CharacterPoseDiagnosticsProjector` MUST只读取匹配同一lineage与Tuning Generation的Committed Source、Graph、Constraint和Final Publication Result；MUST不持有Runtime Module引用、不读取Pending Workspace、Actor State私有页、Foot Context、FBBIK Vendor对象或Physical Transform反推结果，也 MUST不参与任何运行决定。

Runtime MUST在Frame开始通过可消除的partial Query冻结Foot IK target interest，并只在成功Seal后的同步Commit调用栈内，从Constraint、Final Publication与当前Source的已提交Owner直接取得同lineage Left／Right与公共Fact Root，以`in`执行一行`DiagnosticEvent` partial调用。采样链 MUST不创建或消费`CharacterFootIkCommittedCaptureViewLease`、Runtime Snapshot、Dimension View、Consumer／Binding或第二事实页；Live、Trace与Gizmo所需的具体View MAY继续由Runtime Projector独立生产，但不得成为采样输入。PoseGraph只拥有业务Event时机和已提交事实，不拥有Diagnostic Capability、字段Attribute、Sampler Definition、Schema Compiler、AOT Generated Capture Graph、typed packet、Host Adapter、CSV、geometry、Analyzer、Publisher或评分。独立`character-foot-ik-diagnostic-sampling`Program从业务程序集生成的公开Event合同订阅并调用通用Capture；框架Session MUST不索取或保存业务Owner与View。旧Foot单体Analyzer／Publisher、Diagnosis Store与兼容Reader删除，不再形成PoseGraph必须维护的下游合同；独立Foot Analysis只消费Completed Artifact。Disabled构建的Event与Query调用及参数求值 MUST被编译消除，PoseGraph Runtime MUST不保留Sampling AssemblyRef、Event identity、领域Graph、packet、Host或Build类型，也 MUST不为旧Sampler建立表达式／反射路径、兼容DTO、第二Snapshot或临时Adapter。

#### Scenario: 同时观察Player、Foot和FBBIK

- **WHEN** 当前Frame开始时存在对应Pose Watch和detail interest且Frame成功Seal
- **THEN** Projector MUST从同一Committed lineage发布Player Pose、Foot Contribution、Goal Set、FBBIK Pose和Physical结果
- **AND** MUST不重新采样source、执行world query或调用FBBIK

#### Scenario: interest在Frame中途打开

- **WHEN** Editor在当前Frame开始之后增加detail interest
- **THEN** 当前运行结果 MUST保持不变且完整详情 MAY从下一成功Frame开始
- **AND** Projector MUST不读取Pending页补齐半帧Snapshot

#### Scenario: Foot IK AOT Capture消费已提交事实根

- **WHEN** 匹配Sampler Set与Schema identity的Foot IK Capture Session在Frame开始前声明detail interest且当前Frame成功Seal
- **THEN** Runtime MUST在同步Commit调用栈内执行一行Foot `DiagnosticEvent` partial调用并传入target、真实lineage、Left／Right与公共Fact Root
- **AND** generated Program handler MAY调用Editor／IL2CPP AOT Capture Program写入预分配packet，但PoseGraph Runtime MUST不解释Sampler字段、执行生成程序、生成CSV或保留Consumer／Binding／旧事件／View二次join


#### Scenario: 离线诊断不进入PoseGraph

- **WHEN** Host在Runtime之外对已封存Foot Artifact执行当前诊断Plan
- **THEN** PoseGraph MUST不接收Plan、Operator结果、评分或报告
- **AND** 离线诊断失败 MUST不影响PoseGraph运行状态或已封存Capture
### Requirement: Preview与正式Runtime必须复用同一Module Factory和Program Image

Pose 预览与正式运行 MUST使用同一原生图实例规则、节点算法、Source／Constraint／Final Publication、表现帧事务及调参和完成语义。Adapter 可以提供不同 Fact、动作、World 和 Rig host；预览 MUST不创建旧 Image、简化执行器、第二 PlayableGraph 或默认 Foot 结果。

#### Scenario: Preview缺少world context

- **WHEN** Preview执行到需要精确World Context的Foot Placement Operation但Adapter不可用
- **THEN** 同一原生Pose图运行实例 MUST发布typed Unavailable并停止该Frame publication
- **AND** Preview MUST不跳过Constraint或伪造地面结果


### Requirement: Reset、Replacement与Dispose必须按Owner清理状态

原生图替换、资源绑定变化、非连续 seek、actor reset、Fault 和 Dispose MUST由宿主产生 typed reason，让 Graph／Source／Constraint／Final Publication 各自清理状态。Reset MUST提升 generation，使旧帧、调参、源／Constraint 完成结果和诊断失效；替换 MUST停止旧原生实例、完成在途工作并释放资源，再创建新实例。节点 MUST不得跨 owner 清空状态。成功 Constraint Reset MUST保持原已确认初始化和方向准备结果，不能顺手修改算法或恢复旧 vendor 读取。

#### Scenario: Projection被显式重建

- **WHEN** Actor从旧原生图资产切换到新动画绑定版本
- **THEN** 旧Actor State、Frame lease和延迟completion MUST失效，旧source资源 MUST按正式dispose/release顺序关闭
- **AND** 新Runtime MUST只从新原生图资产和新容量建立状态，不迁移旧ABI字段

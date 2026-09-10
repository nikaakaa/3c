# PoseGraph 任务进度

当前决定：保留Pose Compiler、Program Image、Native／Job及正式运行链，只复用FlowCanvas作者UI并观察Unity Play中的真实Actor。作者接入与观察由[独立提案](../integrate-pose-flowcanvas-editor-preview/proposal.md)负责；技能留在其独立提案。此前23.x／24.x的原生runtime实验和替换计划已撤回，从可执行任务清单移除，历史见[决策记录](flowcanvas-experiment.md)。1—22节保留既有重构事实，22.x中被替代的UI任务以新提案为准，不自动勾选或归档。

## 1. 冻结当前保留IK与完整迁移清单

- [x] 1.1 对照用户指定提交`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`与behavior-baseline.md核对当前动画／IK源码、Profile／Rig／作者数据、generated artifact及已有正式输入／诊断证据；后续相关差异单独报告，不等待Foot／IK全部归档
- [x] 1.2 记录当前Foot Motion实际输入、输出、lineage、Curve消费与未完成行为，不按旧spec补实现剩余Foot能力
- [x] 1.3 对账当前保留Foot、Support、Pelvis、Goal、FBBIK与Physical结果，列明已知问题、未覆盖输入、已撤除Reach硬夹紧和已撤销SmoothKnee，不把它们改成重构修复目标
- [x] 1.4 从current外部合同与当前已存在实现固定Clip、Blend Space、Linked Pose、Motion Matching、Transition Routing、Blend Stack和Inertialization迁移目录，不接入未配置内容
- [x] 1.5 盘点`PosePlanExecutionRuntime`、根`AnimationPresentationFrameTransaction`、Native Program、Staged Executor、Workspace、Action lifecycle、Source backend、Constraint、Writer、在线调参、Diagnostics和Compiler全部状态、页、索引、生命周期与调用顺序
- [x] 1.6 为每项现有字段标注唯一目标Owner、寿命类别、写入阶段、读取者和删除位置，分别识别静态Program、actor-local Execution View、Actor State、Program Frame Page、Module Pending页与根事务，拒绝无法归属的共享可变字段
- [x] 1.7 为全部现行Operation Code建立`新Family / 跨帧状态Owner / Frame页Owner / Execution Domain / Workspace需求 / 删除字段`迁移表，覆盖Parameter、ActionPlaybackInput lifecycle、Motion Matching、Pose History与Tuning读取
- [x] 1.8 固定本change不修改PoseState选择、source时间、Action lifecycle、生效中的Tuning值、Blend权重、Transition、Slot、Inertialization、Foot、Goal、FBBIK和Physical Pose结果

- [x] 1.9 记录第一阶段IK维护重构通过提交和证据作为串行接入点，保留其Foot请求／最终结果、Interpolation历史、独立Reset修正与诊断列绑定，不恢复旧结构；总基线仍为ad3527e，实际冲突单独报告
- [x] 1.10 按behavior-baseline.md逐项核对动画时钟、Transition／Blend／Slot顺序、Foot source选择、IK计算与持久状态、Root Bone写入政策；被下一帧消费的内部Fact不得当诊断冗余删除

## 2. 建立统一lineage、根事务与typed Result合同

- [x] 2.1 建立统一`CharacterPoseFrameLineage`，固定Actor、Frame、Completion、Program、Projection、Rig和Tuning Generation identity并删除各Module自行生成的重复完成身份
- [x] 2.2 建立Source Demand、Source Frame、Program Prepared、per-operation Completion、Program Result、Constraint Result和Final Publication Result的typed合同及合法Availability/Outcome
- [x] 2.3 建立由`CharacterAnimationPresentationRuntime`唯一拥有的根`CharacterPoseFrameTransaction`，只保存lineage、阶段、Module lease/result与统一Outcome，不保存任一Module内部Workspace
- [x] 2.4 为Program、Source、Constraint与Final Publication分别建立Owned Pending页和typed lease，明确唯一写入Owner、只读下游view与根Seal/Discard权限
- [x] 2.5 让现有单一运行路径先携带统一lineage、root lease和typed Result，不提前创建空壳Module、wrapper、第二Frame事务或第二执行路径
- [x] 2.6 对齐现有Animancer Evaluate Barrier，固定Barrier前验证、Barrier内执行、Writer后no-throw Seal和Fault语义

- [x] 2.7 按Build、Runtime创建、根Frame／跨Owner交接和Writer分配检查责任，删除迁移新增的重复静态扫描与多层完整校验，保留必要动态检查和原Fault政策

## 3. 收紧Pose Constraint外部边界并保留内部IK

- [x] 3.1 迁移当前`CharacterPoseConstraintRuntime`及根Bank外部归属并保留唯一构造路径，不重做Foot内部阶段或状态布局
- [x] 3.2 在Constraint内部整体保留当前Foot Placement、Pelvis、PoseBone Goal、Goal Contribution、Assembler、Goal Set、FBBIK和历史状态；只替换外部依赖，不改变公式、参数、准入、权重或数值顺序
- [x] 3.3 为Foot Placement、PoseBone Contribution、Goal Assembler和FBBIK建立各自typed编译Handle与per-operation Result
- [x] 3.4 让Program Runtime在每个Constraint Family Operation位置恰好调用一次对应入口并写入唯一completion
- [x] 3.5 让Constraint `Complete`只验证完整闭包并发布一个Constraint Result，不扫描Program、不维护第二Stage Schedule也不重新执行Operation
- [x] 3.6 删除调用方可见的NativeSlice、Goal offset/count、Operation index、Callsite index、内部Bank页和Diagnostics页
- [x] 3.7 让Constraint内部Pending页只响应根Frame lineage和唯一Seal/Discard，不再拥有可与根事务分离的完成身份
- [x] 3.8 将Foot Placement与FBBIK调参接入Constraint-owned Candidate Tuning Snapshot，保持当前字段、值域、成功resetOwnerState结果和生效时机，保留第一阶段独立验证的Vendor方向与BendHistory Reset结果，本阶段不另改行为
- [ ] 3.9 对账Foot、Support、Pelvis、Goal、Assembler、Bend与最终骨骼保持冻结基线；发现差异定位外层迁移，不修改已保留IK公式或配置

## 4. 建立CharacterPoseSourceModule

- [x] 4.1 新增深`CharacterPoseSourceModule`及固定容量Source Demand、Source Binding、Prepared Resource、Usage、Release和Completion页
- [x] 4.2 迁移Clip、Blend Space、Motion Matching和有限Action sample Adapter装配，保持各source-local时间、Action sample readiness与数学不变
- [x] 4.3 迁移Animancer source backend、Physical Pose Source Registry、capture binding和唯一Playable资源所有权
- [x] 4.4 迁移prepared source创建、deferred release、slot reuse、retirement permission与release completion闭包
- [x] 4.5 让Source Module只消费Program发布的Demand/Usage并只输出Source Frame Result，不读取PoseState、Action winner、Transition、Slot或Blend内部状态
- [x] 4.6 从旧Pose runtime删除source数组、physical identity scratch、release pool、Dictionary/List控制逻辑和重复Seal/Discard顺序
- [x] 4.7 将Clip、Blend Space、Motion Matching与Action sample-local调参改为Source-owned Candidate Tuning Snapshot，不修改Program Image或actor-local Execution View
- [x] 4.8 搜索并消除第二Animancer direct Play、第二Physical Source Registry、第二capture owner和图外source fallback

## 5. 分离Program Image、Execution View、Actor State、Owned Frame Pages与根事务

- [x] 5.1 将`CharacterPoseProgramImage`作为`CharacterPresentationProjection`内部唯一语义Pose程序，保存Program identity、ProjectionRevision、PoseProgramImageHash、Rig、Stage、Operation Header、Family Payload、Value layout、Workspace layout、Source Map和容量；Gameplay ContractHash只由外层Presentation Contract与Projection拥有
- [x] 5.2 建立可选`CharacterPoseProgramExecutionView`，每个Program Runtime最多一份，只逐值materialize同Image并验证相同identity/hash，不得编译、重排、补字段或拥有Actor/Frame状态
- [x] 5.3 让`CharacterPoseProgramRuntime`唯一Dispose自己的Execution View，删除第二View、旧Native Program语义容器和旧Runtime Compile路径
- [x] 5.4 新增`CharacterPoseActorState`，迁移PoseState、Player continuity、ActionPlaybackInput lifecycle/command cursor、Slot、Blend Stack、Routing、Inertialization和其它跨帧节点状态
- [x] 5.5 新增`CharacterPoseProgramFramePages`，保存Pending node control、Source Demand输出、当前帧Value、Operation completion和Program diagnostics
- [x] 5.6 让根`CharacterPoseFrameTransaction`只持有Program/Source/Constraint/Publication typed lease/result，不取得或索引各Module内部页
- [x] 5.7 将Dense跨帧状态改为明确Committed/Pending页，将稀疏节点与source生命周期变化保持为固定pending state或journal
- [x] 5.8 删除`CharacterPoseGraphNativeProgram`中的Frame identity、Pending/Committed控制、Goal workspace、运行时Tuning Weight和其它可变状态
- [x] 5.9 删除Actor State对Source物理资源、Constraint Bank、Final Pose和Diagnostics真相的复制
- [x] 5.10 对账Reset、Projection replacement、Preview seek、actor-local Execution View、Dispose和Actor Fault，确保静态、执行View、Actor、Module Frame与根事务寿命各自只由唯一Owner清理

## 6. 建立唯一CharacterPoseProgramRuntime与持久Executor

- [x] 6.1 新增`CharacterPoseProgramRuntime`，唯一持有Program Image只读引用或自己的actor-local Execution View、Actor State、Program Frame Pages和持久Executor Implementation，并只接收根Frame Lease
- [x] 6.2 将PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、BlendStack、Transition消费、Inertialization和其它逻辑节点执行迁入Program Runtime
- [x] 6.3 将每帧Executor构造改为持久绑定Program Image/Execution View和Program自有固定页，只切换根Frame Lease与Pending页索引
- [x] 6.4 按Stage Schedule执行每个Operation恰好一次并写入唯一Operation Completion页
- [x] 6.5 让Program Runtime通过typed Result调用Source Module，并通过typed编译Handle逐Operation调用Constraint Module
- [x] 6.6 删除外层Runtime对World-aware Operation的扫描和内部输入装配，删除Staged Executor对同一Operation的第二解释或完成检查
- [x] 6.7 删除Constraint Module扫描Program、Source Module扫描Operation以及Diagnostics重放Operation的路径
- [x] 6.8 将旧`CharacterPoseGraphStagedExecutor`巨型字段和构造整体替换，删除旧类型而不保留wrapper
- [x] 6.9 将Node Weight、PoseState、Slot、BlendStack、Routing与Inertialization调参改为Program-owned Candidate Tuning Snapshot
- [x] 6.10 搜索并消除第二Action lifecycle Owner、第二Pose Operation执行Owner、第二Value writer和任何图外隐式Pose stage

## 7. 建立CharacterFinalPosePublication与单一Final Pose物理页

- [x] 7.1 新增具体`CharacterFinalPosePublication` Module并迁移唯一Committed/Pending Final Pose物理页、完整Rig binding和Publication Result
- [x] 7.2 让Program Image的Output Family只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不保存Actor页引用且不分配第二Final Output buffer
- [x] 7.3 在Actor Runtime创建时由Final Publication把layout handle绑定到唯一Pending Final Pose页，Program Output Operation通过actor-local binding写入并发布只读`ProgramOutputPoseResult`
- [x] 7.4 让Compiler只证明唯一Output与Publication requirement，让Runtime Factory和Final Publication构造证明唯一具体Writer与完整binding
- [x] 7.5 在写任何Physical Bone前统一验证Pose availability、Rig、continuity、Program completion、Constraint completion和Frame lineage
- [x] 7.6 让唯一Physical Writer一次应用完整Pending Pose，Invalid时保持Committed Pose并遵守现有Fault政策
- [x] 7.7 确保Writer成功后不再执行Foot、Goal、FBBIK、Diagnostics或其它可能因业务输入失败的计算
- [x] 7.8 从Program Runtime、Source Module、Constraint Module和外层Runtime删除Physical Transform写入与第二Final Pose页所有权
- [x] 7.9 不建立Writer Graph节点、Writer抽象接口或第二Implementation，搜索并删除旧final writer旁路

## 8. 建立actor-local原子在线调参

- [x] 8.1 建立`CharacterPoseTuningSnapshot`、单调`TuningGeneration`和Program/Source/Constraint分区Candidate合同
- [x] 8.2 让根Runtime在打开新Frame前收集三个Module Candidate并完成identity、容量、值域与resetOwnerState预验证
- [x] 8.3 让全部Candidate成功后一次提升同一TuningGeneration，任一失败时保持三个Committed Snapshot不变
- [x] 8.4 删除先修改运行对象、失败后反向Apply旧Block的回滚路径
- [x] 8.5 删除Program Image、actor-local Execution View、静态Projection和跨Actor对象上的可变Tuning字段
- [x] 8.6 对账Runtime与Preview的调参字段、生效时机、resetOwnerState与逐Actor隔离，保持现行作者行为

## 9. 收窄唯一动画表现协调根

- [ ] 9.1 在Program、Source、Constraint与Final Publication全部接通后，让`CharacterAnimationPresentationRuntime`唯一拥有根Frame Transaction，只创建Frame Lease、按固定阶段调用Module、传播Outcome并执行唯一Seal/Discard/Fault
- [ ] 9.2 删除协调根对Native offset、Operation字段、Program Frame页、Foot Context、Goal页、FBBIK状态、source资源页和Physical Bone业务字段的读取
- [x] 9.3 让全部Module只提交同一Frame lineage与Tuning Generation并由根事务统一提升，不允许Module自行提前Seal
- [x] 9.4 对账Barrier前Discard、Barrier内/后Fault和Writer后no-throw Seal，确保收窄根Runtime不改变失败政策

## 10. 建立唯一Node Definition Module

- [x] 10.1 新增`CharacterPoseNodeDefinition`合同和`CharacterPoseNodeDefinitionModule`唯一目录
- [ ] 10.2 为全部正式Node Kind建立唯一Definition Adapter，声明Payload、字段、固定端口、条件portVariants、动态端口、Graph Role、Execution Domain、Operation Family、Graph dependency投影、局部校验、Rig校验和typed lowering
- [ ] 10.3 将Pose Capability Catalog改为从Node Definition投影，不再保存与Definition重复的Payload、端口、domain和compiler binding真相
- [x] 10.4 让唯一`GraphAuthoringNodePortShapeProjector`从Capability、typed properties与node-local动态端口投影完整形状，拒绝固定/条件/动态端口identity重叠
- [x] 10.5 将Canvas创建、Details字段、Authoring Adapter、Clipboard和typed Mutation迁移为消费Capability与统一Port Shape
- [x] 10.6 将Document v4模型、Presentation Exporter、strict parser、Target Mapper、Reconciler、Mutation preflight与Validator迁移为消费同一Capability与统一Port Shape
- [x] 10.7 保证Definition不得直接修改Unity对象、执行Document apply、接管五个MCP生命周期或建立第二Reconciler/Transaction Service
- [ ] 10.8 将Graph dependency、局部Validator和Source Map命名迁移到Node Definition，保持跨节点全局规则只属于Topology Pass
- [x] 10.9 删除`ICharacterPoseCompilerHandler`、泛型Handler、Handler Registry、反射注册和Player/Slot/Blend等布尔能力矩阵
- [ ] 10.10 搜索并删除Agent exporter、Package codec、Target Mapper、Profile Inspector、Clipboard、Canvas和Compiler中可由Definition/Capability/Port Shape表达的重复NodeKind switch
- [ ] 10.11 校验全部正式节点恰有一个Definition且Capability、Document、Mutation、Clipboard和Compiler不存在第二catalog；若Agent可见语义变化则同步`btsmtl-agent-authoring`当前合同
- [x] 10.12 为全部Operation Family在唯一Node Definition中固定Execution Domain与只读线程安全能力，向Agent context投影只读事实；确认Canvas、Inspector、Document、Clipboard与MCP不存在Burst、线程、Job、Batch size或调度策略可编辑字段

## 11. 将Pose Compiler拆为不可变Pass

- [x] 11.1 建立唯一`CharacterPoseCompilationRequest/Result`和结构化Pass Diagnostic合同
- [x] 11.2 实现Graph Closure Pass，只从root flat catalog、State引用与Node Definition Graph dependency投影展开State Graph、Subgraph和Linked Pose call closure
- [x] 11.3 实现Typed Lowering Pass，只通过Node Definition把authoring node降低为typed IR
- [ ] 11.4 实现Topology Pass，统一验证typed edge、空间、Graph Role、唯一Output/Assembler/Goal Set/FBBIK、唯一Final Publication requirement和写冲突；递归只由前置Graph Closure验证，具体Writer唯一性只由Runtime Factory验证
- [x] 11.5 实现Symbolic Family Lowering Pass，为每个节点生成唯一Family、symbolic typed value依赖、跨帧状态需求、Frame页需求和Workspace需求，不分配物理index
- [x] 11.6 实现Stage Schedule Pass，按typed依赖和Execution Domain生成唯一有序Stage并证明每Operation恰好一次
- [x] 11.7 实现Value Lifetime Pass，按固定Schedule为Pose、Parameter、Discontinuity、Goal Contribution、Goal Set与控制Value计算typed地址和寿命
- [x] 11.8 实现Workspace Plan Pass，按Schedule、Value寿命、Rig、节点状态、Source、Constraint、Inertialization和Diagnostics manifest分配固定容量
- [x] 11.13 在Workspace Plan之后实现Worker Batch Plan，按Execution Domain、Rig执行布局、Family Kernel、Actor Batch Key、typed read/write range和Completion依赖生成固定批次并静态拒绝别名写冲突、托管捕获与缺失AOT Kernel
- [ ] 11.9 实现Bind Family Payload Pass，只把symbolic引用绑定为stage/value/workspace/batch typed handle，不得发现新的Operation、状态页、批次或容量需求
- [x] 11.10 实现Seal Program Image Pass，校验全部pass identity、source map、Worker Batch Plan、Kernel Set、Execution Policy、Rig执行布局、容量、PoseProgramImageHash和schema后发布Projection内不可变Program Image
- [ ] 11.11 删除中央`CompilationState`、原地跨阶段mutation、重复Graph dependency/拓扑扫描和Runtime二次Compile
- [ ] 11.12 删除只做参数转发的Compiler入口；保留的外部入口只能调用唯一Compiler Module

## 12. 原子替换Operation与Projection ABI

- [x] 12.1 新增`CharacterPoseOperationHeader`和typed `CharacterPoseValueReference`表，只保存公共调度、Family Payload index和输入输出range
- [x] 12.2 为Parameter Input/Resolve、Player、StateMachine、Action Input、AnimationSlot、Blend、Inertialization、Composition、Space Conversion、Component Control、Motion Matching、Pose History、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose和Output建立固定Payload页
- [x] 12.3 对照迁移表确认全部现行Operation Code恰有一个Family且没有Operation继续读取万能记录
- [x] 12.4 让Program Image Seal验证Header/Family/Payload、Value Kind、Stage Domain、Workspace Handle和唯一write set
- [x] 12.5 修改Projection codec、source map、PoseProgramImageHash、schema version和Runtime reader只读Projection内新Program Image，保持Gameplay ContractHash、SemanticHash与Float32/Fixed ProgramHash不变
- [x] 12.6 修改Runtime Family Evaluator只读取自身Payload页，不访问万能Operation无关字段
- [x] 12.7 删除`CharacterPresentationPoseOperation`万能记录、旧Native Operation镜像、无意义`-1`组合和旧字段Validator
- [x] 12.8 删除旧Projection reader、旧Native Program语义构造、旧schema兼容、默认字段补齐、双codec和运行时版本fallback；每个Program Runtime只保留一份同identity只读Execution View materialization
- [ ] 12.9 通过正式显式Character Build入口重建受影响generated Projection和Program Image，不在asset import、Inspector或Runtime自动重建
- [x] 12.10 扩展Program Image、Projection codec与actor-local Execution View，封存Worker Batch、Kernel Set、Execution Policy、Rig执行布局和平台能力identity；任一身份变化提升PoseProgramImageHash与ProjectionRevision
- [x] 12.11 为全部Pure Pose Operation Family实现有限AOT可知Burst/HPC# Kernel，按Family处理跨ActorWork Item，不创建Node级或Bone级Job，不捕获Unity／托管对象或动态分配
- [x] 12.12 建立唯一会话级`CharacterPoseWorkerScheduler`，按Program、Rig布局、依赖波次与Family Kernel跨Actor合批；Program Runtime只提交typed Batch Lease并接收Completion，World Query、Foot、Goal、FinalIK/FBBIK和Final Publication保留Compiler指定Managed域
- [x] 12.13 在同一次ABI切换中删除旧串行`CharacterPoseGraphStagedExecutor`、运行时Operation switch、Managed重算、Burst缺失fallback、每Actor临时Job图和逐Actor立即`Schedule/Complete`路径

## 13. 收口Diagnostics、Pose Watch与Preview

- [x] 13.1 建立Source、Program、Constraint和Final Publication Committed Result诊断投影合同
- [x] 13.2 在Frame开始分别冻结Live、Pose Watch、detail interest以及具体`CharacterFootIkCaptureInterest`和View固定容量；PoseGraph不得读取Sampler Set、Schema、Program或packet容量解释interest
- [x] 13.3 在各Module Pending Result完成时按interest深冻结Pose、Value、Contribution、Goal、Constraint、Operation和Physical结果
- [x] 13.4 让Foot/Goal/FBBIK diagnostics只进入Constraint Committed Result，Physical diagnostics只进入Final Publication Committed Result
- [x] 13.5 让Diagnostics Projector只按同lineage组合Committed Result并发布PoseGraph-owned短租约`CharacterFootIkCommittedCaptureViewLease`，唯一控制其生产、有效期与失效；确认不存在万能Committed View、第二Snapshot，也不持有Program Runtime、Workspace、Constraint Module、Physical Transform、Diagnostic Capability、Sampler Definition、Schema Compiler、Generated Program、typed packet、Host、CSV或Analyzer知识
- [x] 13.6 删除Snapshot Publisher从Native Program、Pending Workspace、Foot Context、FBBIK Vendor对象和多个Owner反推同一事实的路径
- [x] 13.7 让Pose Watch只读取已冻结Committed页，不重新采样source、执行world query、运行FBBIK或推导Physical结果
- [ ] 13.8 让正式Runtime与Preview通过同一Factory装配Projection内Program Image、actor-local Execution View、Program Runtime、Source Module、Constraint Module、Final Publication、根Frame Transaction与Tuning Snapshot
- [ ] 13.9 删除Preview简化Executor、逐Preview第二Native Program、临时Program、默认World Context和Stale Projection fallback
- [ ] 13.10 将具体`CharacterFootIkCommittedCaptureViewLease`在既有Post-Commit短租约内交给外部Foot Diagnostics consumer，由consumer绑定Left View／Metadata与Right View／Metadata并发布CommittedSample Event；Generated Lifecycle自动租packet、调用AOT Program和提交，PoseGraph不得引用框架Runtime或Foot插件的Event、Generator、Generated Program、packet、Host、Build类型，也不得增加Bridge、临时DTO、第二Snapshot或双写链
- [ ] 13.11 对账框架sealed packet经Schema-driven Host自动生成的主表／子表字段业务含义、原始输入／几何引用、评分权重／资格／分母保持；确认不存在Foot Host Adapter、手写Column／CsvBinding或第二Schema，保留历史原包且不用总分变化替代行为对账
- [x] 13.12 让Runtime、Preview、Pose Watch与Live Debug只在全部Worker／Managed Completion完成且根事务成功Seal后读取Committed Result，保存Batch／Kernel／Completion lineage；删除诊断触发等待、重放或重新调度Kernel的路径

## 14. 激进清理与最终一致性

- [x] 14.1 删除旧`PosePlanExecutionRuntime`巨型Implementation并以薄帧协调根或正式新命名整体替换，不保留兼容wrapper
- [x] 14.2 删除旧`CharacterPoseGraphNativeProgram`、旧`CharacterPoseGraphStagedExecutor`、旧万能Operation、旧Compiler Handler Registry和旧中央CompilationState
- [x] 14.3 搜索并消除第二Program Image语义、同一Actor第二Execution View、第二Program State、第二根Frame Transaction、第二Action lifecycle Owner、第二Source owner、第二Operation executor、第二Constraint owner、第二Goal Set、第二FBBIK、第二Final Pose页和第二Physical Writer
- [x] 14.4 搜索并消除Runtime对authoring asset、NodeKind字符串、AssetDatabase、旧Projection schema和动态编译的读取
- [x] 14.5 检查Module依赖方向，确保Contracts不引用Implementation、Runtime不引用Editor、Diagnostics不反向驱动运行结果且不存在asmdef循环
- [ ] 14.6 更新`openspec/project.md`为实际PoseGraph Module、根事务/Owned页数据流、Projection内Program Image、actor-local Execution View与Tuning、Compiler Pass和ABI真相
- [ ] 14.7 使用规定参数编译Runtime与Editor工程，并在每次构建后立即执行`dotnet build-server shutdown`
- [ ] 14.8 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验
- [x] 14.9 核对未恢复中央Foot状态机、骨盆Reach硬夹紧、末端夹脚、已撤销SmoothKnee或CurrentSupport替代Swing包络候选，保留指定基线的有符号膝向运输，已保留第一阶段IK维护成果，未接管其它未实施IK行为任务
- [ ] 14.10 每个代码小步复用现有正式输入Replay／Proof和诊断链，对指定基线与上一保留小步分别保存输入、Body、source时间、Foot／Pelvis／Goal／Solved／Physical的差异；未解释业务差异时停止，不用调参或改评分补偿
- [x] 14.11 将Reset、Projection Replacement与Dispose接入唯一Scheduler fence，完成Outstanding Job后再释放actor-local Execution View、Frame页与Module状态；搜索并消除悬空Native页与跨Actor状态污染
- [ ] 14.12 执行正式IL2CPP／Burst AOT产物闭包检查和Performance Capture，分别记录总Presentation、Main Thread、Worker、Job等待与多Actor批次规模；不通过运行时fallback适配缺失平台能力
- [x] 14.13 搜索并确认不存在动画预算、Phase Offset、跳帧、旧Pose复用或插值补帧路径；Worker资源压力只按现有精确Completion与Fault政策处理

## 15. 退役TrainingEnemy完整内容岛

- [x] 15.1 补齐`character-targeted-motion-warp-demo` delta，删除Standalone双Actor、玩家绑定训练敌人和训练敌人范围Requirement，并把正式结果固定为只保留Corin、target input为None、五段攻击继续执行原始MotionCurve
- [ ] 15.2 按`TrainingEnemy`名称、`corin-training-enemy` ActorId、`gameplay-lab-target`绑定、资产路径和GUID建立完整引用闭包，确认删除范围覆盖GameplayLab composition、Variant、AssetBundle collector、默认目录、构建入口、Profile、Prefab与generated数据
- [ ] 15.3 从GameplayLab prefab与Session composition删除TrainingEnemy嵌套实例、roster注册、AI control source和玩家target-provider绑定，使保留的Corin按既有`OptionalSnapshot`无目标语义运行且不新增占位目标
- [x] 15.4 删除`3cDemo/Client/3C_Client/Assets/Configs/Character/TrainingEnemy`、`TrainingEnemyMonster.prefab`、`TrainingEnemyMonsterPresentation.prefab`及对应meta和generated产物，不迁移其中PoseGraph、动画、AI、Rig、Foot或Profile资产
- [ ] 15.5 删除`TrainingEnemyAnimationAssetAuthoring`、`TrainingEnemyRuntimeSceneBuilder`及仅为TrainingEnemy存在的作者／构建代码，并从GameplayLab builder、launcher、startup validator、root hierarchy builder、Shape Projection installer、collector和默认目录配置删除其专用分支、路径与GUID
- [ ] 15.6 使用`rg`和Unity资产依赖结果确认项目不再包含TrainingEnemy路径、类型、ActorId、Prefab／Profile GUID、Missing Script、Missing Asset或collector条目；随后把`openspec/project.md`更新为单Corin且TrainingEnemy已退役的实际真相

## 16. 建立唯一FlowCanvas Pose作者资产

- [x] 16.1 在Pose作者程序集建立正式FlowCanvas作者依赖和唯一`CharacterPoseCanvasGraph`、`CharacterPoseCanvasNode`、`CharacterPoseCanvasConnection`，输入为Pose Node Definition与typed payload，输出为可序列化的稳定Graph／Node／Port／Edge identity
- [ ] 16.2 让`CharacterPresentationPoseGraphAsset`只拥有Canvas Graph与flat graph catalog，移除`CharacterTypedPoseGraph`字段和第二拓扑存储；资源引用、StateMachine、子图和布局全部进入同一作者资产
- [x] 16.3 限制Pose Canvas只使用FlowCanvas图数据、选择和视图生命周期，禁止FlowCanvas Flow／Value执行、自动类型转换、反射方法、事件和Graph Update进入Character运行装配
- [ ] 16.4 让资产反序列化、复制和保存保持NodeId、EdgeId、logical port identity、Pose空间与Graph Role，任一未知Node Definition或非法端口在写入前返回稳定诊断

## 17. 将Pose作者交互接入唯一Mutation链

- [x] 17.1 实现`CharacterPoseCanvasDefinitionProjection`，从唯一Node Definition和`GraphAuthoringNodePortShapeProjector`生成标题、字段、固定／条件／动态端口、创建菜单、颜色与只读执行域，不在Canvas重复维护NodeKind表
- [x] 17.2 实现`CharacterPoseCanvasMutationAdapter`，把创建、拖线、删除、复制粘贴、移动、Details和StateMachine编辑转换为typed Presentation Mutation，再由Document Transaction／Undo修改唯一Canvas Graph
- [ ] 17.3 关闭Pose Graph子类中FlowCanvas直接增删节点、连接、字段写入和独立Undo入口，确保人工UI、Document、MCP与Clipboard只通过同一Mutation preflight和Reconciler写资产
- [ ] 17.4 用受影响投影刷新替换`OnInspectorGUI`或普通Repaint中的整图扫描与重建，Selection、Navigator、Pose Watch和Details只保存Editor view-state

## 18. 让Compiler直接消费Canvas作者数据

- [ ] 18.1 实现只读`CharacterPoseCanvasAuthoringView`，只输出稳定Graph、Node、Payload、Port、Edge、Graph Role、资源引用和Source Map，不暴露Canvas运行委托、GraphOwner或运行状态
- [x] 18.2 将`CharacterPoseCompilationRequest`、Graph Closure和Typed Lowering原子切换到Canvas Authoring View与唯一Node Definition，拒绝通用Flow、Event、Method、反射节点和Canvas自动转换
- [ ] 18.3 只把保留的Corin Pose Graph、PoseStateMachine、节点、端口、边、布局、子图、identity和资源引用一次性迁入Canvas Graph，并通过正式Character Build生成新的ProjectionRevision与PoseProgramImageHash
- [ ] 18.4 对账迁移前后Corin Graph closure、typed IR、Stage、Operation Family、Program Image source map和资源引用一一对应；TrainingEnemy不得出现在迁移输入、输出或generated manifest
- [ ] 18.5 新Compiler输入闭合后删除`CharacterTypedPoseGraph`、旧Pose `GraphAuthoringCanvasView`、旧StateMachine画布、旧codec、迁移器临时入口及全部镜像、双写、反向同步和兼容读取

## 19. 通过正式Scene Play提供Pose预览

- [ ] 19.1 让Pose窗口只调用`rebuild-btsmtl-preview-with-scene-play`提供的场景启动、暂停、单步、输入与重建入口，并以稳定Actor／Node identity选择观察目标
- [ ] 19.2 让预览只读取正式Session在Worker与Managed Completion完成且根事务Seal后的Committed Result，Graph或Kernel Set变更后停止旧Projection并等待显式Character Build
- [ ] 19.3 删除Pose窗口独立`AnimationPreviewRuntime`、第二播放时钟、简化Executor、默认World Context、临时Program和直接修改PoseState／Action／Player／IK状态的Seek路径

## 20. 收口Canvas迁移一致性

- [ ] 20.1 搜索并确认Pose只有一个Canvas作者资产、一个Mutation写入Owner、一个Node Definition目录、一个Compiler输入和一个Runtime Program链；BTSMTL与AI具体Canvas及资产未被迁移
- [ ] 20.2 同步`openspec/project.md`与受影响Agent作者合同中的实际Canvas、Compiler、Scene Play和TrainingEnemy退役边界，不把未实施结构提前写成current truth
- [ ] 20.3 执行`git diff --check`、本change严格OpenSpec校验和全量严格OpenSpec校验，确认没有fallback、兼容alias、旧Canvas入口或TrainingEnemy残留清单

## 任务真相修正（2026-09-05）

本次复审确认：旧Runtime大类、Staged Executor和Native Program已经删除，现有Runtime Module也已经形成实际深度；但这不等于Compiler、Family Binding、Projection Validator、Canvas读写边界和Preview已经收口。以下已勾选项只要描述了这些未完成边界，就撤回勾选；没有被事实否定的已完成项保留。`CharacterPresentationProjectionCompiler`、`CharacterPoseFamilyPayloadBindingPass`、`CharacterPoseGraphProjectionValidator`、`CharacterPresentationPoseGraphEditorWindow`、`CharacterPosePreviewViewport`和`CharacterPoseCanvasView`的现状不得再被描述为已完成的薄入口、唯一lowering Owner、完整CanvasCore编辑表面或Scene Play预览。

复审后重新打开的项为：`9.1`、`9.2`、`10.2`、`10.3`、`10.8`、`10.10`、`10.11`、`11.4`、`11.9`、`11.11`、`11.12`、`12.9`、`13.8`、`13.9`、`14.6`、`14.7`、`17.3`、`18.1`。`14.1`、`14.2`及已确认形成深度的Runtime Module不撤回；`19.x`继续保持未完成。

ACL依赖边界：当前Pose Graph迁移和质量整改没有直接引用ACL类型、路径或资产，现行Pose仍使用项目自己的typed Source Slot、AnimationClip、Pose Projection和Runtime链。`codex/acl`对共享Runtime/Projection文件的修改属于重叠源码，不自动构成Pose前置，也不能用其Program或Projection替代本分支产物。若后续`21.1`或`21.3`实际跨入ACL修改过的Source/Projection合同，必须接入完整ACL祖先链和作者版本，再通过唯一正式Character Build重建对应Target与同组Projection；不得只取最后五个收尾提交或复制脏工作区文件。ACL现有19/19质量报告可发布，但最终发布后的Build状态是`job_lost_after_publication_domain_reload`，不作为正常结束或游戏画面E2E证据；已确认的Scalar门限固定为`0.001`。

## 21. 后续架构质量收口

以下每一步都必须同时闭合Interface、Implementation、Depth和Locality，再进入下一步；不通过拆文件把同一个中央Owner改名成多个浅Module，不把未闭合的Runtime编译当成业务完成。

- [ ] 21.1 将`CharacterPresentationProjectionCompiler`拆为按Pose Source、Foot事件、Producer/Camera/Cue、Blend/State transition、Motion Matching、Equipment和Projection Revision分域的typed Compiler Module；每个Module拥有自己的输入、输出和诊断，根入口只组合结果、汇总诊断并执行一次原子发布。业务取舍：分域后单一领域变化不会牵动全部Projection，但需要为跨域身份和诊断定义明确交接合同。
- [ ] 21.2 将`CharacterPoseFamilyPayloadBindingPass`中的Family payload binding按真实Operation Family下沉到Node Definition或Family Adapter；Pass只遍历symbolic operation、分配typed handle并汇总结果，不再理解全部Family payload。业务取舍：Family新增字段只影响所属领域，代价是每个Family必须维护自己的固定ABI适配边界。
- [ ] 21.3 将`CharacterPoseGraphProjectionValidator`收窄为sealed Program/Projection身份、容量和发布合同验证；节点局部规则归Definition，跨节点edge/reachability与唯一Output/Assembler/FBBIK规则只归Topology Pass，删除第二套递归拓扑Compiler。业务取舍：错误归属更清楚，代价是Build错误需要携带完整Pass和Source Map路径。
- [ ] 21.4 将`CharacterPresentationRuntime`收敛为typed根事务调用；Workspace、Action Sampling、Slot、Motion Matching、Linked Pose Commit/Discard等知识留在对应Module Implementation，根只管理固定阶段、lineage、Result和Seal/Discard/Fault。业务取舍：根Runtime更稳定，代价是各Module必须提供足够完整的typed Result，不能让根读取内部字段补逻辑。
- [ ] 21.5 将Pose窗口拆为Graph、StateMachine、TransitionRule和Tuning/Diagnostics Presenter，窗口只负责页面组合与导航；Scene Play接入前不改变`19.x`未完成状态，接入后删除旧Preview的target、fixture、时钟、seek和简化Executor职责。业务取舍：作者操作与预览生命周期分离，代价是需要把旧Preview状态迁入正式Scene/Session入口。
- [x] 21.6 选择方案A并固化实现边界：保留FlowCanvas Graph作者表面，把可变对象限制在唯一Editor Mutation Owner，作者和Compiler只读Projection；接受GraphView非virtual增删API无法从基类类型层彻底阻止绕过，依靠源码审计守住唯一写入口。方案B不引入第二层Adapter或第二写链，因此不把`17.3`或`18.1`错误描述为完成。
- [ ] 21.7 （2026-09-08由Decision 25取代，撤回完成勾选）明确`CharacterPoseCanvasView`仍是877行自建GraphView，保留Pose专用Node/Port/StateMachine交互与typed Mutation，并接受项目自行维护GraphView交互成本；未宣称接入ParadoxNotion现成Graph Editor，也未把当前实现描述成完整CanvasCore编辑表面。旧取舍作废：编辑表面改按`22.x`接入CanvasCore GraphEditor，本项保留为历史记录。
- [ ] 21.8 在Compiler与Runtime依赖闭合后，让唯一正式`CharacterSimulationBuildOrchestrator.Build(request)`同时生成所选Numeric Target和同组Presentation Projection，按同一Definition、Semantic IR、Contract和identity发布；旧Program/Projection产物只由该入口替换，不复制主目录生成文件或建立第二Builder。
- [ ] 21.9 只有在后续Pose整改实际依赖ACL修改过的共享Source/Projection合同时，才接入完整ACL源码与作者版本；保留ACL的资源归属、Scalar门限和发布生命周期，不把ACL Program/Projection或发布证据直接当作本分支产物与E2E结果。

## 22. Pose编辑表面接入FlowCanvas GraphEditor（2026-09-08新增，Decision 25）

本批代码、调用链、删除项和真实验证范围见[Canvas接入记录](canvas-integration.md)。Document身份误报已修复，用户已确认拖动恢复；空节点清理尚未apply，后续dry-run存在布局Conflict，用户已选择保留清理前布局。22.3／22.5仍未闭环，19.x Scene Play不在本批冒领。

- [x] 22.1 解锁`CharacterPoseCanvasGraph`的8个写方法（AddNode/AddNode\<T\>/RemoveNode/ConnectNodes/RemoveConnection等）：方法体从抛异常改为把FlowCanvas编辑器原语翻译成typed Mutation——端口索引反查端口ID、粘贴时检测无效或重复NodeId并重建、经`CharacterPoseCanvasMutationPreflight`校验后应用；Mutation合同不变，非法操作仍被preflight拦截。
- [x] 22.2 在FlowCanvas/NodeCanvas编辑器源码加节点位置与名称的变更事件钩子（改动点全部`// 3C`标记），路由进Document记录，保持Layout进入Undo与迁移对账；不引入第二写入链。
- [ ] 22.3 Undo owner定界：编辑器会话内CanvasCore Undo唯一，Mutation应用后不双记Document；外部入口（MCP、Inspector、Clipboard、正式写入命令）Document Transaction唯一不变；验收为编辑器内连续撤销不跳步、不残留半程状态。
- [x] 22.4 创建菜单过滤：FlowCanvas右键菜单只放行`CharacterPoseNodeDefinition`注册的类型，菜单文案、分组与颜色由Definition投影提供；通用Flow/Event/反射节点不得出现在Character作者菜单（16.3禁令）。
- [ ] 22.5 用FlowCanvas `GraphEditor.OpenWindow`打开迁移后的Corin图验证：全部节点与边（Root图12条边、7个子图各1条）正确渲染，建/删节点、拖线、删线、复制粘贴、撤销全部可用，编辑后canonical作者表达式对账通过并走正式Character Build。
- [x] 22.6 命名端口视觉适配：为`CharacterPoseCanvasNode`实现NodeCanvas端口绘制，显示Definition投影的命名端口（pose/parameter-source等）；适配完成前接受默认端口视觉降级，数据与编译不受影响。
- [ ] 22.7 StateMachine子图导航（ChildSurface等价物）、Pose Watch与Preview Dock挂进GraphEditor面板体系；其中预览部分依赖`19.x`Scene Play，保持未完成状态不并入本项验收。
- [x] 22.8 删除自建`CharacterPoseCanvasView`及其窗口装配，Pose图入口切换到`GraphEditor.OpenWindow(asset.Graph)`；全项目搜索确认无第二画布、无残留引用。

## 23. FlowCanvas作者表面、正式Preview与Build收口（2026-09-10当前口径）

- [x] 23.1 确认PoseGraph使用FlowCanvas原生`GraphEditor`、`FlowGraph`、`FlowNode`、`Port`、`BinderConnection`、breadcrumb、Node/Connection Inspector、创建菜单与子图下钻；FlowCanvas只承担Editor交互，不进入Pose Runtime。
- [x] 23.2 确认`CharacterPoseCanvasNode.OnNodeInspectorGUI`与`CharacterPoseCanvasNodeEditorHooks`已接入节点Inspector；字段选项仍由Pose Capability、Profile和Rig上下文提供，不把Unavailable归因于FlowCanvas能力。
- [x] 23.3 删除或关闭Pose authoring默认的自定义`domainPanel`、重复Details和Graph Navigator，让FlowCanvas原生Inspector/Connection Inspector成为作者字段入口；项目面板只保留Runtime Observation、诊断、跨Graph检索和正式Preview/Build状态。
- [x] 23.4 将复杂数组字段（Parameter Policy、IK Goal Binding等）从项目右侧UI Toolkit Details迁入FlowCanvas节点Inspector，仍复用同一typed Mutation，不创建第二写入链。
- [x] 23.5 通过精确Definition的`character.build_fixed_products`发布Float32、Fixed和共享Presentation Projection；Corin产物已生成7条`FullBodyAction` Action Playback input。正式入口：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`与`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`。
- [ ] 23.6 正式Preview继续使用`CharacterAnimationPreviewFixture`与统一Preview/Scene Session链；PoseGraph作者窗口不创建第二Preview UI、第二时钟、临时Program或简化Executor。当前Pose reset observation仍需先完成ACL资源加载/提交，不能登记为Preview通过。

## 24. PoseGraph复用与Character Binding分离（2026-09-10当前决定）

- [x] 24.0 先收口持续动画源：Clip Player与Blend Space Player只保存Graph-owned Source Slot，实际AnimationClip／BlendSpace由Presentation Profile绑定；删除Compiler、Source Plan、Build Input和Authoring中的direct source索引与直连路径，并完成Corin现有7个Clip Player的Source Slot/Profile Binding迁移。其余Policy、Rig、Mask、IK和Foot资源仍待24.2继续迁移，不能把本步描述为完整复用。
- [x] 24.0b 将Pose节点的Blend Policy、Inertialization Policy、Bone Mask、Foot Profile／Calibration与Root Curve迁入Graph-owned Resource Slot和Profile Resource Binding，并让Projection的Blend、Inertialization、Foot、Mask与Root lowering从Binding目录解析；Motion Matching专属资源、Rig/Bone合同和Linked Pose映射仍待24.2继续收口。
- [ ] 24.1 将Pose Graph资产收敛为可复用Graph：只保存稳定拓扑、参数、StateMachine、Transition、Graph Role和抽象能力合同，不保存Corin/Character资源引用。
- [ ] 24.2 将Pose Source Slot、AnimationClip/BlendSpace/Motion Matching、Rig/Bone/Mask、IK/Foot、Slot/Channel、Blend/Inertialization Policy、Action producer和Linked Pose映射收归Profile或独立PoseGraph Binding。
- [ ] 24.3 将Compiler输入固定为`Reusable PoseGraph + Character PoseGraph Binding + Rig/Profile + exact Definition`，Projection、Program Image和资源目录按具体Character实例化。
- [ ] 24.4 让FlowCanvas直接打开Reusable PoseGraph；没有Character上下文时仍可编辑拓扑和抽象字段，只有Binding/Preview/Build页面要求精确Profile、Rig和Definition。
- [ ] 24.5 为同一PoseGraph建立至少两个合法Binding的结构化对账入口，并验证缺失能力、Rig冲突和Slot/Policy冲突都按稳定Graph/Node/Field路径失败，不保留fallback或角色名猜测。

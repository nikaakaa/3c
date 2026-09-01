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
- [ ] 2.4 为Program、Source、Constraint与Final Publication分别建立Owned Pending页和typed lease，明确唯一写入Owner、只读下游view与根Seal/Discard权限
- [x] 2.5 让现有单一运行路径先携带统一lineage、root lease和typed Result，不提前创建空壳Module、wrapper、第二Frame事务或第二执行路径
- [x] 2.6 对齐现有Animancer Evaluate Barrier，固定Barrier前验证、Barrier内执行、Writer后no-throw Seal和Fault语义

- [ ] 2.7 按Build、Runtime创建、根Frame／跨Owner交接和Writer分配检查责任，删除迁移新增的重复静态扫描与多层完整校验，保留必要动态检查和原Fault政策

## 3. 收紧Pose Constraint外部边界并保留内部IK

- [ ] 3.1 迁移当前`CharacterPoseConstraintRuntime`及根Bank外部归属并保留唯一构造路径，不重做Foot内部阶段或状态布局
- [ ] 3.2 在Constraint内部整体保留当前Foot Placement、Pelvis、PoseBone Goal、Goal Contribution、Assembler、Goal Set、FBBIK和历史状态；只替换外部依赖，不改变公式、参数、准入、权重或数值顺序
- [ ] 3.3 为Foot Placement、PoseBone Contribution、Goal Assembler和FBBIK建立各自typed编译Handle与per-operation Result
- [ ] 3.4 让Program Runtime在每个Constraint Family Operation位置恰好调用一次对应入口并写入唯一completion
- [ ] 3.5 让Constraint `Complete`只验证完整闭包并发布一个Constraint Result，不扫描Program、不维护第二Stage Schedule也不重新执行Operation
- [ ] 3.6 删除调用方可见的NativeSlice、Goal offset/count、Operation index、Callsite index、内部Bank页和Diagnostics页
- [ ] 3.7 让Constraint内部Pending页只响应根Frame lineage和唯一Seal/Discard，不再拥有可与根事务分离的完成身份
- [ ] 3.8 将Foot Placement与FBBIK调参接入Constraint-owned Candidate Tuning Snapshot，保持当前字段、值域、成功resetOwnerState结果和生效时机，保留第一阶段独立验证的Vendor方向与BendHistory Reset结果，本阶段不另改行为
- [ ] 3.9 对账Foot、Support、Pelvis、Goal、Assembler、Bend与最终骨骼保持冻结基线；发现差异定位外层迁移，不修改已保留IK公式或配置

## 4. 建立CharacterPoseSourceModule

- [ ] 4.1 新增深`CharacterPoseSourceModule`及固定容量Source Demand、Source Binding、Prepared Resource、Usage、Release和Completion页
- [ ] 4.2 迁移Clip、Blend Space、Motion Matching和有限Action sample Adapter装配，保持各source-local时间、Action sample readiness与数学不变
- [ ] 4.3 迁移Animancer source backend、Physical Pose Source Registry、capture binding和唯一Playable资源所有权
- [ ] 4.4 迁移prepared source创建、deferred release、slot reuse、retirement permission与release completion闭包
- [ ] 4.5 让Source Module只消费Program发布的Demand/Usage并只输出Source Frame Result，不读取PoseState、Action winner、Transition、Slot或Blend内部状态
- [ ] 4.6 从旧Pose runtime删除source数组、physical identity scratch、release pool、Dictionary/List控制逻辑和重复Seal/Discard顺序
- [ ] 4.7 将Clip、Blend Space、Motion Matching与Action sample-local调参改为Source-owned Candidate Tuning Snapshot，不修改Program Image或actor-local Execution View
- [ ] 4.8 搜索并消除第二Animancer direct Play、第二Physical Source Registry、第二capture owner和图外source fallback

## 5. 分离Program Image、Execution View、Actor State、Owned Frame Pages与根事务

- [ ] 5.1 将`CharacterPoseProgramImage`作为`CharacterPresentationProjection`内部唯一语义Pose程序，保存Program identity、ProjectionRevision、PoseProgramImageHash、Rig、Stage、Operation Header、Family Payload、Value layout、Workspace layout、Source Map和容量；Gameplay ContractHash只由外层Presentation Contract与Projection拥有
- [ ] 5.2 建立可选`CharacterPoseProgramExecutionView`，每个Program Runtime最多一份，只逐值materialize同Image并验证相同identity/hash，不得编译、重排、补字段或拥有Actor/Frame状态
- [ ] 5.3 让`CharacterPoseProgramRuntime`唯一Dispose自己的Execution View，删除第二View、旧Native Program语义容器和旧Runtime Compile路径
- [ ] 5.4 新增`CharacterPoseActorState`，迁移PoseState、Player continuity、ActionPlaybackInput lifecycle/command cursor、Slot、Blend Stack、Routing、Inertialization和其它跨帧节点状态
- [ ] 5.5 新增`CharacterPoseProgramFramePages`，保存Pending node control、Source Demand输出、当前帧Value、Operation completion和Program diagnostics
- [ ] 5.6 让根`CharacterPoseFrameTransaction`只持有Program/Source/Constraint/Publication typed lease/result，不取得或索引各Module内部页
- [ ] 5.7 将Dense跨帧状态改为明确Committed/Pending页，将稀疏节点与source生命周期变化保持为固定pending state或journal
- [ ] 5.8 删除`CharacterPoseGraphNativeProgram`中的Frame identity、Pending/Committed控制、Goal workspace、运行时Tuning Weight和其它可变状态
- [ ] 5.9 删除Actor State对Source物理资源、Constraint Bank、Final Pose和Diagnostics真相的复制
- [ ] 5.10 对账Reset、Projection replacement、Preview seek、actor-local Execution View、Dispose和Actor Fault，确保静态、执行View、Actor、Module Frame与根事务寿命各自只由唯一Owner清理

## 6. 建立唯一CharacterPoseProgramRuntime与持久Executor

- [ ] 6.1 新增`CharacterPoseProgramRuntime`，唯一持有Program Image只读引用或自己的actor-local Execution View、Actor State、Program Frame Pages和持久Executor Implementation，并只接收根Frame Lease
- [ ] 6.2 将PoseStateMachine、Player、ActionPlaybackInput lifecycle、AnimationSlot、BlendStack、Transition消费、Inertialization和其它逻辑节点执行迁入Program Runtime
- [ ] 6.3 将每帧Executor构造改为持久绑定Program Image/Execution View和Program自有固定页，只切换根Frame Lease与Pending页索引
- [ ] 6.4 按Stage Schedule执行每个Operation恰好一次并写入唯一Operation Completion页
- [ ] 6.5 让Program Runtime通过typed Result调用Source Module，并通过typed编译Handle逐Operation调用Constraint Module
- [ ] 6.6 删除外层Runtime对World-aware Operation的扫描和内部输入装配，删除Staged Executor对同一Operation的第二解释或完成检查
- [ ] 6.7 删除Constraint Module扫描Program、Source Module扫描Operation以及Diagnostics重放Operation的路径
- [ ] 6.8 将旧`CharacterPoseGraphStagedExecutor`巨型字段和构造整体替换，删除旧类型而不保留wrapper
- [ ] 6.9 将Node Weight、PoseState、Slot、BlendStack、Routing与Inertialization调参改为Program-owned Candidate Tuning Snapshot
- [ ] 6.10 搜索并消除第二Action lifecycle Owner、第二Pose Operation执行Owner、第二Value writer和任何图外隐式Pose stage

## 7. 建立CharacterFinalPosePublication与单一Final Pose物理页

- [ ] 7.1 新增具体`CharacterFinalPosePublication` Module并迁移唯一Committed/Pending Final Pose物理页、完整Rig binding和Publication Result
- [ ] 7.2 让Program Image的Output Family只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不保存Actor页引用且不分配第二Final Output buffer
- [ ] 7.3 在Actor Runtime创建时由Final Publication把layout handle绑定到唯一Pending Final Pose页，Program Output Operation通过actor-local binding写入并发布只读`ProgramOutputPoseResult`
- [ ] 7.4 让Compiler只证明唯一Output与Publication requirement，让Runtime Factory和Final Publication构造证明唯一具体Writer与完整binding
- [ ] 7.5 在写任何Physical Bone前统一验证Pose availability、Rig、continuity、Program completion、Constraint completion和Frame lineage
- [ ] 7.6 让唯一Physical Writer一次应用完整Pending Pose，Invalid时保持Committed Pose并遵守现有Fault政策
- [ ] 7.7 确保Writer成功后不再执行Foot、Goal、FBBIK、Diagnostics或其它可能因业务输入失败的计算
- [ ] 7.8 从Program Runtime、Source Module、Constraint Module和外层Runtime删除Physical Transform写入与第二Final Pose页所有权
- [ ] 7.9 不建立Writer Graph节点、Writer抽象接口或第二Implementation，搜索并删除旧final writer旁路

## 8. 建立actor-local原子在线调参

- [ ] 8.1 建立`CharacterPoseTuningSnapshot`、单调`TuningGeneration`和Program/Source/Constraint分区Candidate合同
- [ ] 8.2 让根Runtime在打开新Frame前收集三个Module Candidate并完成identity、容量、值域与resetOwnerState预验证
- [ ] 8.3 让全部Candidate成功后一次提升同一TuningGeneration，任一失败时保持三个Committed Snapshot不变
- [ ] 8.4 删除先修改运行对象、失败后反向Apply旧Block的回滚路径
- [ ] 8.5 删除Program Image、actor-local Execution View、静态Projection和跨Actor对象上的可变Tuning字段
- [ ] 8.6 对账Runtime与Preview的调参字段、生效时机、resetOwnerState与逐Actor隔离，保持现行作者行为

## 9. 收窄唯一动画表现协调根

- [ ] 9.1 在Program、Source、Constraint与Final Publication全部接通后，让`CharacterAnimationPresentationRuntime`唯一拥有根Frame Transaction，只创建Frame Lease、按固定阶段调用Module、传播Outcome并执行唯一Seal/Discard/Fault
- [ ] 9.2 删除协调根对Native offset、Operation字段、Program Frame页、Foot Context、Goal页、FBBIK状态、source资源页和Physical Bone业务字段的读取
- [ ] 9.3 让全部Module只提交同一Frame lineage与Tuning Generation并由根事务统一提升，不允许Module自行提前Seal
- [ ] 9.4 对账Barrier前Discard、Barrier内/后Fault和Writer后no-throw Seal，确保收窄根Runtime不改变失败政策

## 10. 建立唯一Node Definition Module

- [ ] 10.1 新增`CharacterPoseNodeDefinition`合同和`CharacterPoseNodeDefinitionModule`唯一目录
- [ ] 10.2 为全部正式Node Kind建立唯一Definition Adapter，声明Payload、字段、固定端口、条件portVariants、动态端口、Graph Role、Execution Domain、Operation Family、Graph dependency投影、局部校验、Rig校验和typed lowering
- [ ] 10.3 将Pose Capability Catalog改为从Node Definition投影，不再保存与Definition重复的Payload、端口、domain和compiler binding真相
- [ ] 10.4 让唯一`GraphAuthoringNodePortShapeProjector`从Capability、typed properties与node-local动态端口投影完整形状，拒绝固定/条件/动态端口identity重叠
- [ ] 10.5 将Canvas创建、Details字段、Authoring Adapter、Clipboard和typed Mutation迁移为消费Capability与统一Port Shape
- [ ] 10.6 将Document v4模型、Presentation Exporter、strict parser、Target Mapper、Reconciler、Mutation preflight与Validator迁移为消费同一Capability与统一Port Shape
- [ ] 10.7 保证Definition不得直接修改Unity对象、执行Document apply、接管五个MCP生命周期或建立第二Reconciler/Transaction Service
- [ ] 10.8 将Graph dependency、局部Validator和Source Map命名迁移到Node Definition，保持跨节点全局规则只属于Topology Pass
- [ ] 10.9 删除`ICharacterPoseCompilerHandler`、泛型Handler、Handler Registry、反射注册和Player/Slot/Blend等布尔能力矩阵
- [ ] 10.10 搜索并删除Agent exporter、Package codec、Target Mapper、Profile Inspector、Clipboard、Canvas和Compiler中可由Definition/Capability/Port Shape表达的重复NodeKind switch
- [ ] 10.11 校验全部正式节点恰有一个Definition且Capability、Document、Mutation、Clipboard和Compiler不存在第二catalog；若Agent可见语义变化则同步`btsmtl-agent-authoring`当前合同

## 11. 将Pose Compiler拆为不可变Pass

- [ ] 11.1 建立唯一`CharacterPoseCompilationRequest/Result`和结构化Pass Diagnostic合同
- [ ] 11.2 实现Graph Closure Pass，只从root flat catalog、State引用与Node Definition Graph dependency投影展开State Graph、Subgraph和Linked Pose call closure
- [ ] 11.3 实现Typed Lowering Pass，只通过Node Definition把authoring node降低为typed IR
- [ ] 11.4 实现Topology Pass，统一验证typed edge、空间、Graph Role、唯一Output/Assembler/Goal Set/FBBIK、唯一Final Publication requirement和写冲突；递归只由前置Graph Closure验证，具体Writer唯一性只由Runtime Factory验证
- [ ] 11.5 实现Symbolic Family Lowering Pass，为每个节点生成唯一Family、symbolic typed value依赖、跨帧状态需求、Frame页需求和Workspace需求，不分配物理index
- [ ] 11.6 实现Stage Schedule Pass，按typed依赖和Execution Domain生成唯一有序Stage并证明每Operation恰好一次
- [ ] 11.7 实现Value Lifetime Pass，按固定Schedule为Pose、Parameter、Discontinuity、Goal Contribution、Goal Set与控制Value计算typed地址和寿命
- [ ] 11.8 实现Workspace Plan Pass，按Schedule、Value寿命、Rig、节点状态、Source、Constraint、Inertialization和Diagnostics manifest分配固定容量
- [ ] 11.9 实现Bind Family Payload Pass，只把symbolic引用绑定为stage/value/workspace typed handle，不得发现新的Operation、状态页或容量需求
- [ ] 11.10 实现Seal Program Image Pass，校验全部pass identity、source map、容量、PoseProgramImageHash和schema后发布Projection内不可变Program Image
- [ ] 11.11 删除中央`CompilationState`、原地跨阶段mutation、重复Graph dependency/拓扑扫描和Runtime二次Compile
- [ ] 11.12 删除只做参数转发的Compiler入口；保留的外部入口只能调用唯一Compiler Module

## 12. 原子替换Operation与Projection ABI

- [ ] 12.1 新增`CharacterPoseOperationHeader`和typed `CharacterPoseValueReference`表，只保存公共调度、Family Payload index和输入输出range
- [ ] 12.2 为Parameter Input/Resolve、Player、StateMachine、Action Input、AnimationSlot、Blend、Inertialization、Composition、Space Conversion、Component Control、Motion Matching、Pose History、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose和Output建立固定Payload页
- [ ] 12.3 对照迁移表确认全部现行Operation Code恰有一个Family且没有Operation继续读取万能记录
- [ ] 12.4 让Program Image Seal验证Header/Family/Payload、Value Kind、Stage Domain、Workspace Handle和唯一write set
- [ ] 12.5 修改Projection codec、source map、PoseProgramImageHash、schema version和Runtime reader只读Projection内新Program Image，保持Gameplay ContractHash、SemanticHash与Float32/Fixed ProgramHash不变
- [ ] 12.6 修改Runtime Family Evaluator只读取自身Payload页，不访问万能Operation无关字段
- [ ] 12.7 删除`CharacterPresentationPoseOperation`万能记录、旧Native Operation镜像、无意义`-1`组合和旧字段Validator
- [ ] 12.8 删除旧Projection reader、旧Native Program语义构造、旧schema兼容、默认字段补齐、双codec和运行时版本fallback；每个Program Runtime只保留一份同identity只读Execution View materialization
- [ ] 12.9 通过正式显式Character Build入口重建受影响generated Projection和Program Image，不在asset import、Inspector或Runtime自动重建

## 13. 收口Diagnostics、Pose Watch与Preview

- [ ] 13.1 建立Source、Program、Constraint和Final Publication Committed Result诊断投影合同
- [ ] 13.2 在Frame开始冻结Live、Capture、Pose Watch和detail interest及固定容量
- [ ] 13.3 在各Module Pending Result完成时按interest深冻结Pose、Value、Contribution、Goal、Constraint、Operation和Physical结果
- [ ] 13.4 让Foot/Goal/FBBIK diagnostics只进入Constraint Committed Result，Physical diagnostics只进入Final Publication Committed Result
- [ ] 13.5 让Diagnostics Projector只按同lineage组合Committed Result，不持有Program Runtime、Workspace、Constraint Module或Physical Transform引用
- [ ] 13.6 删除Snapshot Publisher从Native Program、Pending Workspace、Foot Context、FBBIK Vendor对象和多个Owner反推同一事实的路径
- [ ] 13.7 让Pose Watch只读取已冻结Committed页，不重新采样source、执行world query、运行FBBIK或推导Physical结果
- [ ] 13.8 让正式Runtime与Preview通过同一Factory装配Projection内Program Image、actor-local Execution View、Program Runtime、Source Module、Constraint Module、Final Publication、根Frame Transaction与Tuning Snapshot
- [ ] 13.9 删除Preview简化Executor、逐Preview第二Native Program、临时Program、默认World Context和Stale Projection fallback
- [ ] 13.10 将新Runtime Result接回现有Sampler、Analyzer、Publisher、小报告／明细存储及七维评分，不新增第二列映射、采样或离线发布链
- [ ] 13.11 对账诊断字段含义、原始输入／几何引用、评分权重／资格／分母保持；保留历史原包，不用总分变化替代行为对账

## 14. 激进清理与最终一致性

- [ ] 14.1 删除旧`PosePlanExecutionRuntime`巨型Implementation并以薄帧协调根或正式新命名整体替换，不保留兼容wrapper
- [ ] 14.2 删除旧`CharacterPoseGraphNativeProgram`、旧`CharacterPoseGraphStagedExecutor`、旧万能Operation、旧Compiler Handler Registry和旧中央CompilationState
- [ ] 14.3 搜索并消除第二Program Image语义、同一Actor第二Execution View、第二Program State、第二根Frame Transaction、第二Action lifecycle Owner、第二Source owner、第二Operation executor、第二Constraint owner、第二Goal Set、第二FBBIK、第二Final Pose页和第二Physical Writer
- [ ] 14.4 搜索并消除Runtime对authoring asset、NodeKind字符串、AssetDatabase、旧Projection schema和动态编译的读取
- [ ] 14.5 检查Module依赖方向，确保Contracts不引用Implementation、Runtime不引用Editor、Diagnostics不反向驱动运行结果且不存在asmdef循环
- [ ] 14.6 更新`openspec/project.md`为实际PoseGraph Module、根事务/Owned页数据流、Projection内Program Image、actor-local Execution View与Tuning、Compiler Pass和ABI真相
- [ ] 14.7 使用规定参数编译Runtime与Editor工程，并在每次构建后立即执行`dotnet build-server shutdown`
- [ ] 14.8 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验
- [ ] 14.9 核对未恢复中央Foot状态机、骨盆Reach硬夹紧、末端夹脚、已撤销SmoothKnee或CurrentSupport替代Swing包络候选，保留指定基线的有符号膝向运输，已保留第一阶段IK维护成果，未接管其它未实施IK行为任务
- [ ] 14.10 每个代码小步复用现有正式输入Replay／Proof和诊断链，对指定基线与上一保留小步分别保存输入、Body、source时间、Foot／Pelvis／Goal／Solved／Physical的差异；未解释业务差异时停止，不用调参或改评分补偿

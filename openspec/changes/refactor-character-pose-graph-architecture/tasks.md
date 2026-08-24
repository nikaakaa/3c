## 1. 前置、行为Oracle与完整迁移清单

- [ ] 1.1 从current spec建立唯一Pose Constraint Bank、Goal Assembler、Goal Set、FBBIK和Final Writer行为Oracle
- [ ] 1.2 从current spec建立Foot Motion Data输入、输出、lineage与正式Curve消费Oracle
- [ ] 1.3 从current `character-foot-placement-presentation`建立Foot、Support、Pelvis、Goal与FBBIK逐帧结果Oracle
- [ ] 1.4 从current spec建立Clip、Blend Space、Linked Pose、Motion Matching、Transition Routing、Blend Stack和Inertialization正式节点迁移目录
- [ ] 1.5 盘点`PosePlanExecutionRuntime`、Native Program、Staged Executor、Workspace、Source backend、Constraint、Writer、Diagnostics和Compiler全部状态、页、索引、生命周期与调用顺序
- [ ] 1.6 为每项现有字段标注唯一目标Owner、寿命类别、写入阶段、读取者和删除位置，拒绝无法归属的共享可变字段
- [ ] 1.7 为全部现行Operation Code建立`新Family / 跨帧状态Owner / Frame页Owner / Execution Domain / Workspace需求 / 删除字段`迁移表，覆盖Parameter、Action、Motion Matching与Pose History
- [ ] 1.8 固定本change不修改PoseState选择、source时间、Blend权重、Transition、Slot、Inertialization、Foot、Goal、FBBIK和Physical Pose结果

## 2. 建立统一lineage与typed Result合同

- [ ] 2.1 建立统一`CharacterPoseFrameLineage`，固定Actor、Frame、Completion、Program、Projection和Rig identity并删除各Module自行生成的重复完成身份
- [ ] 2.2 建立Source Demand、Source Frame、Program Prepared、per-operation Completion、Program Result、Constraint Result和Final Publication Result的typed合同及合法Availability/Outcome
- [ ] 2.3 建立唯一`CharacterPoseFrameTransaction`页目录，明确每页唯一写入Owner、只读下游view与根Seal/Discard权限
- [ ] 2.4 让现有单一运行路径先携带统一lineage和typed Result，不提前创建空壳Module、wrapper、第二Frame事务或第二执行路径
- [ ] 2.5 对齐现有Animancer Evaluate Barrier，固定Barrier前验证、Barrier内执行、Writer后no-throw Seal和Fault语义

## 3. 深化Pose Constraint Module

- [ ] 3.1 将`CharacterPoseConstraintRuntime`及其根Bank移入正式Pose Constraints目录并保留唯一构造路径
- [ ] 3.2 将Foot Placement、PoseBone Goal、Goal Contribution、Goal Assembler、Goal Set、FBBIK、BendHistory和Solver Result全部收进Constraint Module Implementation
- [ ] 3.3 为Foot Placement、PoseBone Contribution、Goal Assembler和FBBIK建立各自typed编译Handle与per-operation Result
- [ ] 3.4 让Program Runtime在每个Constraint Family Operation位置恰好调用一次对应入口并写入唯一completion
- [ ] 3.5 让Constraint `Complete`只验证完整闭包并发布一个Constraint Result，不扫描Program、不维护第二Stage Schedule也不重新执行Operation
- [ ] 3.6 删除调用方可见的NativeSlice、Goal offset/count、Operation index、Callsite index、内部Bank页和Diagnostics页
- [ ] 3.7 让Constraint内部Pending页只响应根Frame lineage和唯一Seal/Discard，不再拥有可与根事务分离的完成身份
- [ ] 3.8 确认Foot、Support、Pelvis、Goal编码、Assembler和BendHistory逐值保持行为Oracle，不把架构迁移变成算法修改

## 4. 建立CharacterPoseSourceModule

- [ ] 4.1 新增深`CharacterPoseSourceModule`及固定容量Source Demand、Source Binding、Prepared Resource、Usage、Release和Completion页
- [ ] 4.2 迁移Clip、Blend Space、Motion Matching和有限Action sample Adapter装配，保持各source-local时间与readiness数学不变
- [ ] 4.3 迁移Animancer source backend、Physical Pose Source Registry、capture binding和唯一Playable资源所有权
- [ ] 4.4 迁移prepared source创建、deferred release、slot reuse、retirement permission与release completion闭包
- [ ] 4.5 让Source Module只消费Program发布的Demand/Usage并只输出Source Frame Result，不读取PoseState、Transition、Slot或Blend内部状态
- [ ] 4.6 从旧Pose runtime删除source数组、physical identity scratch、release pool、Dictionary/List控制逻辑和重复Seal/Discard顺序
- [ ] 4.7 搜索并消除第二Animancer direct Play、第二Physical Source Registry、第二capture owner和图外source fallback

## 5. 分离Projection Program Image、Actor State与Frame Transaction

- [ ] 5.1 将`CharacterPoseProgramImage`作为`CharacterPresentationProjection`内部唯一Pose程序，保存Program identity、Rig、Stage、Operation Header、Family Payload、Value layout、Workspace layout、Source Map和容量
- [ ] 5.2 删除Projection外第二Native Program生成、复制或转换路径，Runtime只按Projection内Image装配Module
- [ ] 5.3 新增`CharacterPoseActorState`，迁移PoseState、Player continuity、Slot、Blend Stack、Routing、Inertialization和其它跨帧节点状态
- [ ] 5.4 将Dense跨帧状态改为明确Committed/Pending页，将稀疏节点与source生命周期变化保持为固定pending state或journal
- [ ] 5.5 将当前帧Value、Operation completion、Module Result引用和interest页迁入`CharacterPoseFrameTransaction`
- [ ] 5.6 删除`CharacterPoseGraphNativeProgram`中的Frame identity、Pending/Committed控制、Goal workspace和其它可变状态
- [ ] 5.7 删除Actor State对Source物理资源、Constraint Bank、Final Pose和Diagnostics真相的复制
- [ ] 5.8 对账Reset、Projection replacement、Preview seek、Dispose和Actor Fault，确保三种寿命各自只由唯一Owner清理

## 6. 建立唯一CharacterPoseProgramRuntime与持久Executor

- [ ] 6.1 新增`CharacterPoseProgramRuntime`，唯一持有Program Image只读引用、Actor State、Frame Transaction和持久Executor Implementation
- [ ] 6.2 将PoseStateMachine、Player、AnimationSlot、BlendStack、Transition消费、Inertialization和其它逻辑节点执行迁入Program Runtime
- [ ] 6.3 将每帧Executor构造改为持久绑定Program Image和固定页，只切换Frame Lease与Pending页索引
- [ ] 6.4 按Stage Schedule执行每个Operation恰好一次并写入唯一Operation Completion页
- [ ] 6.5 让Program Runtime通过typed Result调用Source Module，并通过typed编译Handle逐Operation调用Constraint Module
- [ ] 6.6 删除外层Runtime对World-aware Operation的扫描和内部输入装配，删除Staged Executor对同一Operation的第二解释或完成检查
- [ ] 6.7 删除Constraint Module扫描Program、Source Module扫描Operation以及Diagnostics重放Operation的路径
- [ ] 6.8 将旧`CharacterPoseGraphStagedExecutor`巨型字段和构造整体替换，删除旧类型而不保留wrapper
- [ ] 6.9 搜索并消除第二Pose Operation执行Owner、第二Value writer和任何图外隐式Pose stage

## 7. 建立CharacterFinalPosePublication与单一Final Pose物理页

- [ ] 7.1 新增具体`CharacterFinalPosePublication` Module并迁移唯一Committed/Pending Final Pose物理页、完整Rig binding和Publication Result
- [ ] 7.2 让Program Image的Output Family只保存指向Publication Pending页的typed write handle，不分配第二Final Output buffer
- [ ] 7.3 让Program Output Operation通过typed write handle写Pending Final Pose并发布只读`ProgramOutputPoseResult`
- [ ] 7.4 在写任何Physical Bone前统一验证Pose availability、Rig、continuity、Program completion、Constraint completion和Frame lineage
- [ ] 7.5 让唯一Physical Writer一次应用完整Pending Pose，Invalid时保持Committed Pose并遵守现有Fault政策
- [ ] 7.6 确保Writer成功后不再执行Foot、Goal、FBBIK、Diagnostics或其它可能因业务输入失败的计算
- [ ] 7.7 从Program Runtime、Source Module、Constraint Module和外层Runtime删除Physical Transform写入与第二Final Pose页所有权
- [ ] 7.8 不建立Writer抽象接口或第二Implementation，搜索并删除旧final writer旁路

## 8. 收窄唯一动画表现协调根

- [ ] 8.1 在Program、Source、Constraint与Final Publication全部接通后，让`CharacterAnimationPresentationRuntime`只创建Frame Lease、按固定阶段调用Module、传播Outcome并执行唯一Seal/Discard/Fault
- [ ] 8.2 删除协调根对Native offset、Operation字段、Foot Context、Goal页、FBBIK状态、source资源页和Physical Bone业务字段的读取
- [ ] 8.3 让全部Module只提交同一Frame lineage并由根事务统一提升，不允许Module自行提前Seal
- [ ] 8.4 对账Barrier前Discard、Barrier内/后Fault和Writer后no-throw Seal，确保收窄根Runtime不改变失败政策

## 9. 建立唯一Node Definition Module

- [ ] 9.1 新增`CharacterPoseNodeDefinition`合同和`CharacterPoseNodeDefinitionModule`唯一目录
- [ ] 9.2 为全部正式Node Kind建立唯一Definition Adapter，声明Payload、字段、固定/动态端口、Graph Role、Execution Domain、Operation Family、局部校验、Rig校验和typed lowering
- [ ] 9.3 将Pose Capability Catalog改为从Node Definition投影，不再保存与Definition重复的Payload、端口、domain和compiler binding真相
- [ ] 9.4 将Canvas创建、Details字段、Authoring Adapter和typed Mutation迁移到Node Definition投影
- [ ] 9.5 将Document v4节点局部schema、Exporter、strict codec、Reconciler与Clipboard迁移为读取Definition投影，保留现有Document package、diff、Undo、rollback、save和reverse export唯一生命周期
- [ ] 9.6 保证Definition不得直接修改Unity对象、执行Document apply或建立第二Reconciler/Transaction Service
- [ ] 9.7 将局部Validator和Source Map命名迁移到Node Definition，保持跨节点全局规则只属于Topology Pass
- [ ] 9.8 删除`ICharacterPoseCompilerHandler`、泛型Handler、Handler Registry、反射注册和Player/Slot/Blend等布尔能力矩阵
- [ ] 9.9 搜索并删除Agent exporter、Profile Inspector、Clipboard、Canvas和Compiler中可由Definition表达的重复NodeKind switch
- [ ] 9.10 校验全部正式节点恰有一个Definition且Capability、Document、Mutation、Clipboard和Compiler不存在第二catalog

## 10. 将Pose Compiler拆为不可变Pass

- [ ] 10.1 建立唯一`CharacterPoseCompilationRequest/Result`和结构化Pass Diagnostic合同
- [ ] 10.2 实现Graph Closure Pass，唯一展开root flat catalog、State Graph、Subgraph和Linked Pose call closure
- [ ] 10.3 实现Typed Lowering Pass，只通过Node Definition把authoring node降低为typed IR
- [ ] 10.4 实现Topology Pass，统一验证typed edge、空间、Graph Role、递归、唯一Output/Assembler/Goal Set/FBBIK/Writer和写冲突
- [ ] 10.5 实现Symbolic Family Lowering Pass，为每个节点生成唯一Family、symbolic typed value依赖、跨帧状态需求、Frame页需求和Workspace需求，不分配物理index
- [ ] 10.6 实现Stage Schedule Pass，按typed依赖和Execution Domain生成唯一有序Stage并证明每Operation恰好一次
- [ ] 10.7 实现Value Lifetime Pass，按固定Schedule为Pose、Parameter、Discontinuity、Goal Contribution、Goal Set与控制Value计算typed地址和寿命
- [ ] 10.8 实现Workspace Plan Pass，按Schedule、Value寿命、Rig、节点状态、Source、Constraint、Inertialization和Diagnostics manifest分配固定容量
- [ ] 10.9 实现Bind Family Payload Pass，只把symbolic引用绑定为stage/value/workspace typed handle，不得发现新的Operation、状态页或容量需求
- [ ] 10.10 实现Seal Program Image Pass，校验全部pass identity、source map、容量、hash和schema后发布Projection内不可变Program Image
- [ ] 10.11 删除中央`CompilationState`、原地跨阶段mutation、重复拓扑扫描和Runtime二次Compile
- [ ] 10.12 删除只做参数转发的Compiler入口；保留的外部入口只能调用唯一Compiler Module

## 11. 原子替换Operation与Projection ABI

- [ ] 11.1 新增`CharacterPoseOperationHeader`和typed `CharacterPoseValueReference`表，只保存公共调度、Family Payload index和输入输出range
- [ ] 11.2 为Parameter Input/Resolve、Player、StateMachine、Action Input、AnimationSlot、Blend、Inertialization、Composition、Space Conversion、Component Control、Motion Matching、Pose History、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose和Output建立固定Payload页
- [ ] 11.3 对照迁移表确认全部现行Operation Code恰有一个Family且没有Operation继续读取万能记录
- [ ] 11.4 让Program Image Seal验证Header/Family/Payload、Value Kind、Stage Domain、Workspace Handle和唯一write set
- [ ] 11.5 修改Projection codec、source map、ContractHash、schema version和Runtime reader只读Projection内新Program Image
- [ ] 11.6 修改Runtime Family Evaluator只读取自身Payload页，不访问万能Operation无关字段
- [ ] 11.7 删除`CharacterPresentationPoseOperation`万能记录、旧Native Operation镜像、无意义`-1`组合和旧字段Validator
- [ ] 11.8 删除旧Projection reader、旧Native Program构造、旧schema兼容、默认字段补齐、双codec和运行时版本fallback
- [ ] 11.9 通过正式显式Character Build入口重建受影响generated Projection和Program Image，不在asset import、Inspector或Runtime自动重建

## 12. 收口Diagnostics、Pose Watch与Preview

- [ ] 12.1 建立Source、Program、Constraint和Final Publication Committed Result诊断投影合同
- [ ] 12.2 在Frame开始冻结Live、Capture、Pose Watch和detail interest及固定容量
- [ ] 12.3 在各Module Pending Result完成时按interest深冻结Pose、Value、Contribution、Goal、Constraint、Operation和Physical结果
- [ ] 12.4 让Foot/Goal/FBBIK diagnostics只进入Constraint Committed Result，Physical diagnostics只进入Final Publication Committed Result
- [ ] 12.5 让Diagnostics Projector只按同lineage组合Committed Result，不持有Program Runtime、Workspace、Constraint Module或Physical Transform引用
- [ ] 12.6 删除Snapshot Publisher从Native Program、Pending Workspace、Foot Context、FBBIK Vendor对象和多个Owner反推同一事实的路径
- [ ] 12.7 让Pose Watch只读取已冻结Committed页，不重新采样source、执行world query、运行FBBIK或推导Physical结果
- [ ] 12.8 让正式Runtime与Preview通过同一Factory装配Projection内Program Image、Program Runtime、Source Module、Constraint Module和Final Publication
- [ ] 12.9 删除Preview简化Executor、临时Program、默认World Context和Stale Projection fallback

## 13. 激进清理与最终一致性

- [ ] 13.1 删除旧`PosePlanExecutionRuntime`巨型Implementation并以薄帧协调根或正式新命名整体替换，不保留兼容wrapper
- [ ] 13.2 删除旧`CharacterPoseGraphNativeProgram`、旧`CharacterPoseGraphStagedExecutor`、旧万能Operation、旧Compiler Handler Registry和旧中央CompilationState
- [ ] 13.3 搜索并消除第二Program Image、第二Program State、第二Frame Transaction、第二Source owner、第二Operation executor、第二Constraint owner、第二Goal Set、第二FBBIK、第二Final Pose页和第二Physical Writer
- [ ] 13.4 搜索并消除Runtime对authoring asset、NodeKind字符串、AssetDatabase、旧Projection schema和动态编译的读取
- [ ] 13.5 检查Module依赖方向，确保Contracts不引用Implementation、Runtime不引用Editor、Diagnostics不反向驱动运行结果且不存在asmdef循环
- [ ] 13.6 更新`openspec/project.md`为实际PoseGraph Module、数据流、Projection内Program Image、Compiler Pass和ABI真相
- [ ] 13.7 使用规定参数编译Runtime与Editor工程，并在每次构建后立即执行`dotnet build-server shutdown`
- [ ] 13.8 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验

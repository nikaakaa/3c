## 1. 前置、行为Oracle与迁移清单

- [ ] 1.1 确认`refactor-character-pose-constraint-transaction`已经完成、由用户验收并归档，current spec只存在一个Pose Constraint Bank、Goal Assembler、Goal Set、FBBIK和Final Writer
- [ ] 1.2 确认`improve-character-foot-placement-behavior`已经完成、由用户验收并归档，把归档后的Foot、Support、Pelvis、Goal与FBBIK逐帧结果固定为本change不可修改Oracle
- [ ] 1.3 确认`replace-animation-sequence-with-clip-authoring`与`add-character-presentation-blend-space`已经完成并归档，把Clip、Blend Space、Linked Pose、Motion Matching、Transition Routing、Blend Stack和Inertialization正式节点目录固定为迁移输入
- [ ] 1.4 盘点`PosePlanExecutionRuntime`、Native Program、Staged Executor、Workspace、Source backend、Constraint、Writer、Diagnostics和Compiler全部状态、页、索引、生命周期与调用顺序
- [ ] 1.5 为每项现有字段标注唯一目标Owner、寿命类别、写入阶段、读取者和删除位置，拒绝无法归属的共享可变字段
- [ ] 1.6 固定本change不修改PoseState选择、source时间、Blend权重、Transition、Slot、Inertialization、Foot、Goal、FBBIK和Physical Pose结果

## 2. 建立统一lineage、typed Result与根帧协调合同

- [ ] 2.1 建立统一`CharacterPoseFrameLineage`，固定Actor、Frame、Completion、Program、Projection和Rig identity并删除各Module自行生成的重复完成身份
- [ ] 2.2 建立Source Demand、Source Frame、Program Prepared、Program Result、Constraint Result和Final Publication Result的typed合同及合法Availability/Outcome
- [ ] 2.3 建立唯一`CharacterPoseFrameTransaction`页目录，明确每页唯一写入Owner、只读下游view与根Seal/Discard权限
- [ ] 2.4 让`CharacterAnimationPresentationRuntime`只创建Frame Lease、按固定阶段调用Module、传播Outcome并执行唯一Seal/Discard/Fault
- [ ] 2.5 删除外层协调器对Native offset、Operation字段、Foot Context、Goal页、FBBIK状态和Physical Bone业务字段的读取
- [ ] 2.6 对齐现有Animancer Evaluate Barrier，保证Barrier前验证、Barrier内执行、Writer后no-throw Seal和Fault语义不变

## 3. 深化并抽离Pose Constraint Module

- [ ] 3.1 将`CharacterPoseConstraintRuntime`及其根Bank从巨型Pose runtime文件迁入正式Pose Constraints目录并保留唯一构造路径
- [ ] 3.2 将Foot Placement、PoseBone Goal、Goal Contribution、Goal Assembler、Goal Set、FBBIK、BendHistory和Solver Result全部收进Constraint Module Implementation
- [ ] 3.3 将Constraint外部Interface收窄为typed Component Pose、Frame facts、固定Handle和Constraint Result
- [ ] 3.4 删除调用方可见的NativeSlice、Goal offset/count、Operation index、Callsite index、内部Bank页和Diagnostics页
- [ ] 3.5 让Constraint内部Pending页只响应根Frame lineage和唯一Seal/Discard，不再拥有可与根事务分离的完成身份
- [ ] 3.6 确认Foot、Support、Pelvis、Goal编码、Assembler和BendHistory逐值保持行为Oracle，不把架构迁移变成算法修改

## 4. 建立CharacterPoseSourceModule

- [ ] 4.1 新增深`CharacterPoseSourceModule`及固定容量Source Demand、Source Binding、Prepared Resource、Usage、Release和Completion页
- [ ] 4.2 迁移Clip、Blend Space、Motion Matching和有限Action sample Adapter装配，保持各source-local时间与readiness数学不变
- [ ] 4.3 迁移Animancer source backend、Physical Pose Source Registry、capture binding和唯一Playable资源所有权
- [ ] 4.4 迁移prepared source创建、deferred release、slot reuse、retirement permission与release completion闭包
- [ ] 4.5 让Source Module只消费Program发布的Demand/Usage并只输出Source Frame Result，不读取PoseState、Transition、Slot或Blend内部状态
- [ ] 4.6 从旧Pose runtime删除source数组、physical identity scratch、release pool、Dictionary/List控制逻辑和重复Seal/Discard顺序
- [ ] 4.7 搜索并消除第二Animancer direct Play、第二Physical Source Registry、第二capture owner和图外source fallback

## 5. 分离Program Image、Actor State与Frame Transaction

- [ ] 5.1 新增不可变`CharacterPoseProgramImage`根类型，保存Program identity、Rig、Stage、Operation Header、Family Payload、Value layout、Workspace layout、Source Map和容量
- [ ] 5.2 新增`CharacterPoseActorState`，迁移PoseState、Player continuity、Slot、Blend Stack、Routing、Inertialization和其它跨帧节点状态
- [ ] 5.3 将Dense跨帧状态改为明确Committed/Pending页，将稀疏节点与source生命周期变化保持为固定pending state或journal
- [ ] 5.4 将当前帧Value、Operation completion、Module Result引用和interest页迁入`CharacterPoseFrameTransaction`
- [ ] 5.5 删除`CharacterPoseGraphNativeProgram`中的Frame identity、Pending/Committed控制、Goal workspace和其它可变状态
- [ ] 5.6 删除Actor State对Source物理资源、Constraint Bank、Final Pose和Diagnostics真相的复制
- [ ] 5.7 对账Reset、Projection replacement、Preview seek、Dispose和Actor Fault，确保三种寿命各自只由唯一Owner清理

## 6. 建立唯一CharacterPoseProgramRuntime与持久Executor

- [ ] 6.1 新增`CharacterPoseProgramRuntime`，唯一持有Program Image、Actor State、Frame Transaction和持久Executor Implementation
- [ ] 6.2 将PoseStateMachine、Player、AnimationSlot、BlendStack、Transition消费、Inertialization和其它逻辑节点执行迁入Program Runtime
- [ ] 6.3 将每帧Executor构造改为持久绑定Program Image和固定页，只切换Frame Lease与Pending页索引
- [ ] 6.4 按Stage Schedule执行每个Operation恰好一次并写入唯一Operation Completion页
- [ ] 6.5 让Program Runtime按typed Handle调用Source Module、Constraint Module和Final Publication，不向其暴露Workspace布局
- [ ] 6.6 删除外层Runtime对World-aware Foot Operation的预执行，删除Staged Executor对同一Foot Operation的第二解释或完成检查
- [ ] 6.7 删除Constraint Module扫描Program、Source Module扫描Operation以及Diagnostics重放Operation的路径
- [ ] 6.8 将旧`CharacterPoseGraphStagedExecutor`巨型字段和构造整体替换，删除旧类型而不保留wrapper
- [ ] 6.9 搜索并消除第二Pose Operation执行Owner、第二Value writer和任何图外隐式Pose stage

## 7. 建立CharacterFinalPosePublication

- [ ] 7.1 新增具体`CharacterFinalPosePublication` Module并迁移Committed/Pending Final Pose页、完整Rig binding和Publication Result
- [ ] 7.2 在写任何Physical Bone前统一验证Pose availability、Rig、continuity、Program completion、Constraint completion和Frame lineage
- [ ] 7.3 让唯一Physical Writer一次应用完整Pending Pose，Invalid时保持Committed Pose并遵守现有Fault政策
- [ ] 7.4 确保Writer成功后不再执行Foot、Goal、FBBIK、Diagnostics或其它可能因业务输入失败的计算
- [ ] 7.5 从Program Runtime、Source Module、Constraint Module和外层Runtime删除Physical Transform写入与Final Pose页所有权
- [ ] 7.6 不建立Writer抽象接口或第二Implementation，搜索并删除旧final writer旁路

## 8. 建立唯一Node Definition Module

- [ ] 8.1 新增`CharacterPoseNodeDefinition`合同和`CharacterPoseNodeDefinitionModule`唯一目录
- [ ] 8.2 为全部正式Node Kind建立唯一Definition Adapter，声明Payload、字段、固定/动态端口、Graph Role、Execution Domain、Operation Family、局部校验、Rig校验和typed lowering
- [ ] 8.3 将Capability Catalog改为从Node Definition投影，不再保存与Definition重复的Payload、端口、domain和compiler binding真相
- [ ] 8.4 将Canvas创建、Details字段、Authoring Adapter和typed Mutation迁移到Node Definition
- [ ] 8.5 将Document v4 schema/export/import/reconcile与Clipboard codec迁移到Node Definition投影
- [ ] 8.6 将局部Validator和Source Map命名迁移到Node Definition，保持跨节点全局规则只属于Topology Pass
- [ ] 8.7 删除`ICharacterPoseCompilerHandler`、泛型Handler、Handler Registry、反射注册和Player/Slot/Blend等布尔能力矩阵
- [ ] 8.8 搜索并删除Agent exporter、Profile Inspector、Clipboard、Canvas和Compiler中可由Definition表达的重复NodeKind switch
- [ ] 8.9 校验全部正式节点恰有一个Definition且Capability、Document、Mutation、Clipboard和Compiler不存在第二catalog

## 9. 将Pose Plan Compiler拆为不可变Pass

- [ ] 9.1 建立唯一`CharacterPoseCompilationRequest/Result`和结构化Pass Diagnostic合同
- [ ] 9.2 实现Graph Closure Pass，唯一展开root flat catalog、State inline graph、Subgraph和Linked Pose call closure
- [ ] 9.3 实现Typed Lowering Pass，只通过Node Definition把authoring node降低为typed IR
- [ ] 9.4 实现Topology Pass，统一验证typed edge、空间、Graph Role、递归、唯一Output/Assembler/Goal Set/FBBIK/Writer和写冲突
- [ ] 9.5 实现Value Plan Pass，为Pose、Parameter、Discontinuity、Goal Contribution、Goal Set与控制Value分配typed地址和生命周期
- [ ] 9.6 实现Workspace Plan Pass，按Rig、Value、节点状态、Source、Constraint、Inertialization和Diagnostics interest manifest分配固定容量
- [ ] 9.7 实现Family Payload Pass，把节点编译为只属于对应Operation Family的不可变payload plan
- [ ] 9.8 实现Stage Schedule Pass，按typed依赖和Execution Domain生成唯一有序Stage并证明每Operation恰好一次
- [ ] 9.9 实现Seal Program Image Pass，校验全部pass identity、source map、容量、hash和schema后发布不可变Program Image
- [ ] 9.10 删除中央`CompilationState`、原地跨阶段mutation、重复拓扑扫描和Runtime二次Compile
- [ ] 9.11 删除只做参数转发的Compiler入口；保留的外部入口只能调用唯一Compiler Module

## 10. 原子替换Operation与Projection ABI

- [ ] 10.1 新增`CharacterPoseOperationHeader`和typed `CharacterPoseValueReference`表，只保存公共调度、Family Payload index和输入输出range
- [ ] 10.2 为Player、StateMachine、AnimationSlot、Blend、Inertialization、Composition、Space Conversion、Component Control、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose和Output建立固定Payload页
- [ ] 10.3 让Program Image Seal验证Header/Family/Payload、Value Kind、Stage Domain、Workspace Handle和唯一write set
- [ ] 10.4 修改Projection codec、source map、ContractHash、schema version和Runtime reader只读新Program Image
- [ ] 10.5 修改Runtime Family Evaluator只读取自身Payload页，不访问万能Operation无关字段
- [ ] 10.6 删除`CharacterPresentationPoseOperation`万能记录、旧Native Operation镜像、无意义`-1`组合和旧字段Validator
- [ ] 10.7 删除旧Projection reader、旧schema兼容、默认字段补齐、双codec和运行时版本fallback
- [ ] 10.8 通过正式显式Character Build入口重建受影响generated Projection和Program Image，不在asset import、Inspector或Runtime自动重建

## 11. 收口Diagnostics、Pose Watch与Preview

- [ ] 11.1 建立Source、Program、Constraint和Final Publication Committed Result诊断投影合同
- [ ] 11.2 在Frame开始冻结Live、Capture、Pose Watch和detail interest及固定容量
- [ ] 11.3 在各Module Pending Result完成时按interest深冻结Pose、Value、Contribution、Goal、Constraint、Operation和Physical结果
- [ ] 11.4 让Diagnostics Projector只读取同lineage Committed Result，不持有Program Runtime、Workspace、Constraint Module或Physical Transform引用
- [ ] 11.5 删除Snapshot Publisher从Native Program、Pending Workspace、Foot Context、FBBIK Vendor对象和多个Owner拼装同一事实的路径
- [ ] 11.6 让Pose Watch只读取已冻结Committed页，不重新采样source、执行world query、运行FBBIK或推导Physical结果
- [ ] 11.7 让正式Runtime与Preview通过同一Factory装配Program Image、Program Runtime、Source Module、Constraint Module和Final Publication
- [ ] 11.8 删除Preview简化Executor、临时Program、默认World Context和Stale Projection fallback

## 12. 激进清理与最终一致性

- [ ] 12.1 删除旧`PosePlanExecutionRuntime`巨型Implementation并以薄帧协调根或正式新命名整体替换，不保留兼容wrapper
- [ ] 12.2 删除旧`CharacterPoseGraphNativeProgram`、旧`CharacterPoseGraphStagedExecutor`、旧万能Operation、旧Compiler Handler Registry和旧中央CompilationState
- [ ] 12.3 搜索并消除第二Program State、第二Frame Transaction、第二Source owner、第二Operation executor、第二Constraint owner、第二Goal Set、第二FBBIK和第二Physical Writer
- [ ] 12.4 搜索并消除Runtime对authoring asset、NodeKind字符串、AssetDatabase、旧Projection schema和动态编译的读取
- [ ] 12.5 检查Module依赖方向，确保Contracts不引用Implementation、Runtime不引用Editor、Diagnostics不反向驱动运行结果且不存在asmdef循环
- [ ] 12.6 更新`openspec/project.md`为实际PoseGraph模块、数据流、Program Image、Compiler Pass和ABI真相
- [ ] 12.7 使用规定参数编译Runtime与Editor工程，并在每次构建后立即执行`dotnet build-server shutdown`
- [ ] 12.8 执行`git diff --check`、本change严格校验和全量严格OpenSpec校验

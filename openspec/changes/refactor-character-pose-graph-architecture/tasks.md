# PoseGraph任务

本文件只列实现与文档修改任务。已完成运行基础见[归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/proposal.md)，原验证、对账及撤销说明见[历史记录](verification-history.md)。只读Blackboard范围划分见第25组，EventGraph事件与Set由独立规划窗口负责。

## 1. 冻结当前保留IK与完整迁移清单

本组10项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 2. 建立统一lineage、根事务与typed Result合同

本组7项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 3. 收紧Pose Constraint外部边界并保留内部IK

- [x] 3.1 迁移当前`CharacterPoseConstraintRuntime`及根Bank外部归属并保留唯一构造路径，不重做Foot内部阶段或状态布局
- [x] 3.2 在Constraint内部整体保留当前Foot Placement、Pelvis、PoseBone Goal、Goal Contribution、Assembler、Goal Set、FBBIK和历史状态；只替换外部依赖，不改变公式、参数、准入、权重或数值顺序
- [x] 3.3 为Foot Placement、PoseBone Contribution、Goal Assembler和FBBIK建立各自typed编译Handle与per-operation Result
- [x] 3.4 让Program Runtime在每个Constraint Family Operation位置恰好调用一次对应入口并写入唯一completion
- [x] 3.5 让Constraint `Complete`只验证完整闭包并发布一个Constraint Result，不扫描Program、不维护第二Stage Schedule也不重新执行Operation
- [x] 3.6 删除调用方可见的NativeSlice、Goal offset/count、Operation index、Callsite index、内部Bank页和Diagnostics页
- [x] 3.7 让Constraint内部Pending页只响应根Frame lineage和唯一Seal/Discard，不再拥有可与根事务分离的完成身份
- [x] 3.8 将Foot Placement与FBBIK调参接入Constraint-owned Candidate Tuning Snapshot，保持当前字段、值域、成功resetOwnerState结果和生效时机，保留第一阶段独立验证的Vendor方向与BendHistory Reset结果，本阶段不另改行为

## 4. 建立CharacterPoseSourceModule

本组8项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 5. 分离Program Image、Execution View、Actor State、Owned Frame Pages与根事务

本组10项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 6. 建立唯一CharacterPoseProgramRuntime与持久Executor

本组10项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 7. 建立CharacterFinalPosePublication与单一Final Pose物理页

本组9项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 8. 建立actor-local原子在线调参

本组6项原完成记录已移至[运行基础归档](../archive/2026-09-12-complete-pose-graph-runtime-foundation/completion-record.md)，保留原任务编号，不再重复列为本change任务。

## 9. 收窄唯一动画表现协调根

- [ ] 9.1 在Program、Source、Constraint与Final Publication全部接通后，让`CharacterAnimationPresentationRuntime`唯一拥有根Frame Transaction，只创建Frame Lease、按固定阶段调用Module、传播Outcome并执行唯一Seal/Discard/Fault
- [ ] 9.2 删除协调根对Native offset、Operation字段、Program Frame页、Foot Context、Goal页、FBBIK状态、source资源页和Physical Bone业务字段的读取
- [x] 9.3 让全部Module只提交同一Frame lineage与Tuning Generation并由根事务统一提升，不允许Module自行提前Seal

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
- [ ] 10.11 同步正式Node Definition、Capability、Document、Mutation、Clipboard与Compiler的单一目录约束；Agent可见语义变化时同步btsmtl-agent-authoring当前合同。
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
- [x] 13.12 让Runtime、Preview、Pose Watch与Live Debug只在全部Worker／Managed Completion完成且根事务成功Seal后读取Committed Result，保存Batch／Kernel／Completion lineage；删除诊断触发等待、重放或重新调度Kernel的路径

## 14. 激进清理与最终一致性

- [x] 14.1 删除旧`PosePlanExecutionRuntime`巨型Implementation并以薄帧协调根或正式新命名整体替换，不保留兼容wrapper
- [x] 14.2 删除旧`CharacterPoseGraphNativeProgram`、旧`CharacterPoseGraphStagedExecutor`、旧万能Operation、旧Compiler Handler Registry和旧中央CompilationState
- [x] 14.3 搜索并消除第二Program Image语义、同一Actor第二Execution View、第二Program State、第二根Frame Transaction、第二Action lifecycle Owner、第二Source owner、第二Operation executor、第二Constraint owner、第二Goal Set、第二FBBIK、第二Final Pose页和第二Physical Writer
- [x] 14.4 搜索并消除Runtime对authoring asset、NodeKind字符串、AssetDatabase、旧Projection schema和动态编译的读取
- [ ] 14.6 更新`openspec/project.md`为实际PoseGraph Module、根事务/Owned页数据流、Projection内Program Image、actor-local Execution View与Tuning、Compiler Pass和ABI真相
- [x] 14.11 将Reset、Projection Replacement与Dispose接入唯一Scheduler fence，完成Outstanding Job后再释放actor-local Execution View、Frame页与Module状态；搜索并消除悬空Native页与跨Actor状态污染

## 15. 退役TrainingEnemy完整内容岛

- [x] 15.1 补齐`character-targeted-motion-warp-demo` delta，删除Standalone双Actor、玩家绑定训练敌人和训练敌人范围Requirement，并把正式结果固定为只保留Corin、target input为None、五段攻击继续执行原始MotionCurve
- [ ] 15.3 从GameplayLab prefab与Session composition删除TrainingEnemy嵌套实例、roster注册、AI control source和玩家target-provider绑定，使保留的Corin按既有`OptionalSnapshot`无目标语义运行且不新增占位目标
- [x] 15.4 删除`3cDemo/Client/3C_Client/Assets/Configs/Character/TrainingEnemy`、`TrainingEnemyMonster.prefab`、`TrainingEnemyMonsterPresentation.prefab`及对应meta和generated产物，不迁移其中PoseGraph、动画、AI、Rig、Foot或Profile资产
- [ ] 15.5 删除`TrainingEnemyAnimationAssetAuthoring`、`TrainingEnemyRuntimeSceneBuilder`及仅为TrainingEnemy存在的作者／构建代码，并从GameplayLab builder、launcher、startup validator、root hierarchy builder、Shape Projection installer、collector和默认目录配置删除其专用分支、路径与GUID
- [ ] 15.6 在TrainingEnemy退役内容完成后，更新openspec/project.md中的GameplayLab角色、目标输入与资源归属说明。

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
- [ ] 18.5 新Compiler输入闭合后删除`CharacterTypedPoseGraph`、旧Pose `GraphAuthoringCanvasView`、旧StateMachine画布、旧codec、迁移器临时入口及全部镜像、双写、反向同步和兼容读取

## 19. 通过正式Scene Play提供Pose预览

- [ ] 19.1 让Pose窗口只调用`rebuild-btsmtl-preview-with-scene-play`提供的场景启动、暂停、单步、输入与重建入口，并以稳定Actor／Node identity选择观察目标
- [ ] 19.2 让预览只读取正式Session在Worker与Managed Completion完成且根事务Seal后的Committed Result，Graph或Kernel Set变更后停止旧Projection并等待显式Character Build
- [ ] 19.3 删除Pose窗口独立`AnimationPreviewRuntime`、第二播放时钟、简化Executor、默认World Context、临时Program和直接修改PoseState／Action／Player／IK状态的Seek路径

## 20. 收口Canvas迁移一致性

- [ ] 20.2 同步`openspec/project.md`与受影响Agent作者合同中的实际Canvas、Compiler、Scene Play和TrainingEnemy退役边界，不把未实施结构提前写成current truth

## 21. 后续架构质量收口

- [ ] 21.1 将`CharacterPresentationProjectionCompiler`拆为按Pose Source、Foot事件、Producer/Camera/Cue、Blend/State transition、Motion Matching、Equipment和Projection Revision分域的typed Compiler Module；每个Module拥有自己的输入、输出和诊断，根入口只组合结果、汇总诊断并执行一次原子发布。业务取舍：分域后单一领域变化不会牵动全部Projection，但需要为跨域身份和诊断定义明确交接合同。
- [ ] 21.2 将`CharacterPoseFamilyPayloadBindingPass`中的Family payload binding按真实Operation Family下沉到Node Definition或Family Adapter；Pass只遍历symbolic operation、分配typed handle并汇总结果，不再理解全部Family payload。业务取舍：Family新增字段只影响所属领域，代价是每个Family必须维护自己的固定ABI适配边界。
- [ ] 21.3 将`CharacterPoseGraphProjectionValidator`收窄为sealed Program/Projection身份、容量和发布合同验证；节点局部规则归Definition，跨节点edge/reachability与唯一Output/Assembler/FBBIK规则只归Topology Pass，删除第二套递归拓扑Compiler。业务取舍：错误归属更清楚，代价是Build错误需要携带完整Pass和Source Map路径。
- [ ] 21.4 将`CharacterPresentationRuntime`收敛为typed根事务调用；Workspace、Action Sampling、Slot、Motion Matching、Linked Pose Commit/Discard等知识留在对应Module Implementation，根只管理固定阶段、lineage、Result和Seal/Discard/Fault。业务取舍：根Runtime更稳定，代价是各Module必须提供足够完整的typed Result，不能让根读取内部字段补逻辑。
- [ ] 21.5 将Pose窗口拆为Graph、StateMachine、TransitionRule和Tuning/Diagnostics Presenter，窗口只负责页面组合与导航；Scene Play接入前不改变`19.x`未完成状态，接入后删除旧Preview的target、fixture、时钟、seek和简化Executor职责。业务取舍：作者操作与预览生命周期分离，代价是需要把旧Preview状态迁入正式Scene/Session入口。
- [x] 21.6 选择方案A并固化实现边界：保留FlowCanvas Graph作者表面，把可变对象限制在唯一Editor Mutation Owner，作者和Compiler只读Projection；接受GraphView非virtual增删API无法从基类类型层彻底阻止绕过，依靠源码审计守住唯一写入口。方案B不引入第二层Adapter或第二写链，因此不把`17.3`或`18.1`错误描述为完成。
- [ ] 21.8 在Compiler与Runtime依赖闭合后，让唯一正式`CharacterSimulationBuildOrchestrator.Build(request)`同时生成所选Numeric Target和同组Presentation Projection，按同一Definition、Semantic IR、Contract和identity发布；旧Program/Projection产物只由该入口替换，不复制主目录生成文件或建立第二Builder。
- [ ] 21.9 只有在后续Pose整改实际依赖ACL修改过的共享Source/Projection合同时，才接入完整ACL源码与作者版本；保留ACL的资源归属、Scalar门限和发布生命周期，不把ACL Program/Projection或发布证据直接当作本分支产物与E2E结果。

## 22. Pose编辑表面接入FlowCanvas GraphEditor（2026-09-08新增，Decision 25）

- [x] 22.1 解锁`CharacterPoseCanvasGraph`的8个写方法（AddNode/AddNode\<T\>/RemoveNode/ConnectNodes/RemoveConnection等）：方法体从抛异常改为把FlowCanvas编辑器原语翻译成typed Mutation——端口索引反查端口ID、粘贴时检测无效或重复NodeId并重建、经`CharacterPoseCanvasMutationPreflight`校验后应用；Mutation合同不变，非法操作仍被preflight拦截。
- [x] 22.2 在FlowCanvas/NodeCanvas编辑器源码加节点位置与名称的变更事件钩子（改动点全部`// 3C`标记），路由进Document记录，保持Layout进入Undo与迁移对账；不引入第二写入链。
- [ ] 22.3 统一编辑器会话内CanvasCore Undo owner，Mutation应用后不双记Document；MCP、Inspector、Clipboard及正式外部写入继续由Document Transaction唯一拥有Undo。
- [x] 22.4 创建菜单过滤：FlowCanvas右键菜单只放行`CharacterPoseNodeDefinition`注册的类型，菜单文案、分组与颜色由Definition投影提供；通用Flow/Event/反射节点不得出现在Character作者菜单（16.3禁令）。
- [x] 22.6 命名端口视觉适配：为`CharacterPoseCanvasNode`实现NodeCanvas端口绘制，显示Definition投影的命名端口（pose/parameter-source等）；适配完成前接受默认端口视觉降级，数据与编译不受影响。
- [ ] 22.7 StateMachine子图导航（ChildSurface等价物）、Pose Watch与Preview Dock挂进GraphEditor面板体系；其中预览部分依赖`19.x`Scene Play，保持未完成状态不并入本项验收。
- [x] 22.8 删除自建CharacterPoseCanvasView及其窗口装配，Pose图入口切换到GraphEditor.OpenWindow(asset.Graph)，移除旧画布的全部装配与引用。

## 23. FlowCanvas作者表面、正式Preview与Build收口（2026-09-10当前口径）

- [x] 23.3 删除或关闭Pose authoring默认的自定义`domainPanel`、重复Details和Graph Navigator，让FlowCanvas原生Inspector/Connection Inspector成为作者字段入口；项目面板只保留Runtime Observation、诊断、跨Graph检索和正式Preview/Build状态。
- [x] 23.4 将复杂数组字段（Parameter Policy、IK Goal Binding等）从项目右侧UI Toolkit Details迁入FlowCanvas节点Inspector，仍复用同一typed Mutation，不创建第二写入链。
- [x] 23.5 通过精确Definition的`character.build_fixed_products`发布Float32、Fixed和共享Presentation Projection；Corin产物已生成7条`FullBodyAction` Action Playback input。正式入口：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`与`Assets/Configs/Simulation/DeterministicRollback/Programs/CorinFixedProgram.asset`。
- [ ] 23.6 正式Preview继续使用`CharacterAnimationPreviewFixture`与统一Preview/Scene Session链；PoseGraph作者窗口不创建第二Preview UI、第二时钟、临时Program或简化Executor。当前Pose reset observation仍需先完成ACL资源加载/提交，不能登记为Preview通过。

## 24. PoseGraph复用与Character Binding分离（2026-09-10当前决定）

- [x] 24.0 先收口持续动画源：Clip Player与Blend Space Player只保存Graph-owned Source Slot，实际AnimationClip／BlendSpace由Presentation Profile绑定；删除Compiler、Source Plan、Build Input和Authoring中的direct source索引与直连路径，并完成Corin现有7个Clip Player的Source Slot/Profile Binding迁移。其余Policy、Rig、Mask、IK和Foot资源仍待24.2继续迁移，不能把本步描述为完整复用。
- [x] 24.0b 将Pose节点的Blend Policy、Inertialization Policy、Bone Mask、Foot Profile／Calibration与Root Curve迁入Graph-owned Resource Slot和Profile Resource Binding，并让Projection的Blend、Inertialization、Foot、Mask与Root lowering从Binding目录解析；Motion Matching专属资源、Rig/Bone合同和Linked Pose映射仍待24.2继续收口。
- [ ] 24.1 将Pose Graph资产收敛为可复用Graph：只保存稳定拓扑、参数、StateMachine、Transition、Graph Role和抽象能力合同，不保存Corin/Character资源引用。
- [ ] 24.2 将Pose Source Slot、AnimationClip/BlendSpace/Motion Matching、Rig/Bone/Mask、IK/Foot、Slot/Channel、Blend/Inertialization Policy、Action producer和Linked Pose映射收归Profile或独立PoseGraph Binding（Motion Matching Binding、Jump Blend Policy、StateMachine Blend Curve与Blend Profile源码已迁移；Selected Pose Player与Blend Stack已改用通用Source Slot；Resource Slot已有通用Mutation链，Document local Resource Slot也已接入创建、解析与删除顺序；Corin资产与其余绑定仍待收口）。
- [ ] 24.3 将Compiler输入固定为`Reusable PoseGraph + Character PoseGraph Binding + Rig/Profile + exact Definition`，Projection、Program Image和资源目录按具体Character实例化。
- [ ] 24.4 让FlowCanvas直接打开Reusable PoseGraph；没有Character上下文时仍可编辑拓扑和抽象字段，只有Binding/Preview/Build页面要求精确Profile、Rig和Definition。
- [ ] 24.5 实现同一PoseGraph的多Binding资源解析与错误诊断，按稳定Graph/Node/Field报告缺失能力、Rig冲突和Slot/Policy冲突，不保留fallback或角色名猜测。

## 25. PoseGraph只读Blackboard与输入范围划分（待实施）

- [ ] 25.1 定义Graph输入的正式分类、声明owner、消费者和可访问范围，分清动画实例变量、只读表现事实、Pose曲线和节点/资源配置。
- [ ] 25.2 根据EventGraph正式规划确定PoseGraph变量读取接口，绑定同一稳定变量身份、类型和实例值来源；缺少正式接口时明确记录依赖，不自建兼容布局或更新器。
- [ ] 25.3 定义Root、StatePose、普通Subgraph与Linked Pose入口的可访问输入和公开参数规则，区分外部变量读取与子图调用参数，不把根图声明复制到全部子图。
- [ ] 25.4 让FlowCanvas原生Blackboard投影当前图可访问的正式输入，显示名称、类型、来源、作用范围和使用情况；合法未使用声明保持可见或可筛选，不因暂未连线而禁止使用。
- [ ] 25.5 让拖拽只创建绑定正确声明的Get，禁止PoseGraph主图创建共享动画变量Set；编辑绑定和引用继续使用唯一typed Mutation。
- [ ] 25.6 移除动画属性导入器向每张PoseGraph复制BlendShape等声明的路径，改由正式曲线/资源合同提供编译所需完整曲线清单，保持最终属性写入消费者。
- [ ] 25.7 区分动画输入读取和指定输入Pose的曲线读取，补齐类型、来源、作用范围、Stage依赖和缺失数据诊断；停止依靠清空root.Parameters缩减运行时数据布局。
- [ ] 25.8 为Body内部FootPlacement权重建立明确曲线绑定或公开输入合同；保留已有曲线混合、惯性响应与Foot权重作用，迁移完成后删除不再需要的根图Get和透传端口。
- [ ] 25.9 在Blackboard、Get、子图入口和Slot主要显示区域使用作者名称，内部稳定ID只用于引用与详情诊断，重命名不破坏连接。
- [ ] 25.10 通过正式Document/Mutation删除确认无引用的重复子图与废弃声明；存在用户改动冲突时保留现场并交由用户决策。
- [ ] 25.11 同步共享Capability、Document字段投影、Exporter、Reconciler、Mutation、Validator、Compiler source map和只读观察，使人工编辑与Agent使用同一正式语义。
- [ ] 25.12 更新对应spec与项目当前状态，明确EventGraph接口、曲线传播与只读消费边界的实际交付范围，移除迁移后的旧入口和过期说明，不把未交付能力写成current truth。

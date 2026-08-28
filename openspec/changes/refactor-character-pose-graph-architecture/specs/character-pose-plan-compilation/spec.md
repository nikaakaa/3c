## ADDED Requirements

### Requirement: 每种Pose节点必须只有一个Node Definition真相

Editor MUST提供唯一`CharacterPoseNodeDefinitionModule`，并为每个正式Node Kind注册恰好一个`CharacterPoseNodeDefinition` Adapter。Definition MUST集中声明Capability identity、Payload类型、字段合同、固定端口、条件`portVariants`、动态端口政策、允许Graph Role、Execution Domain、Operation Family、Authoring codec、Graph dependency投影、局部Payload/Rig校验、typed lowering和Source Map命名。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Canvas、Document v4、Clipboard、Reconciler、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency与typed lowering。系统不得复制第二字段表、端口表、compiler binding或NodeKind特例目录。

#### Scenario: 新增正式Pose节点

- **WHEN** 项目增加一个新的正式Pose Node Kind
- **THEN** 作者创建、Document往返、Clipboard、统一Port Shape、Graph dependency、局部校验和typed lowering MUST由同一个Definition Adapter及其Capability投影提供
- **AND** 缺少任一必要合同 MUST使Editor初始化或Character Build失败而不得由调用方补默认逻辑

#### Scenario: 同一Node Kind重复定义

- **WHEN** Definition Module发现两个Adapter声明相同Node Kind或Capability identity
- **THEN** 系统 MUST拒绝目录并报告两个定义来源
- **AND** MUST不按注册顺序选择其中一个

### Requirement: Node Definition与全局Topology规则必须分离

Node Definition MUST只拥有单节点局部语义与该节点直接引用的Graph dependency，不得查询其它节点或决定Graph全局合法性。Graph Closure Pass MUST组合这些dependency并唯一验证可达call graph、悬空引用与递归；typed edge兼容、唯一Output、唯一Goal Assembler、唯一Goal Set、唯一FBBIK、唯一Final Publication requirement、重复Goal Slot、写冲突和Stage依赖 MUST只由Compiler Topology Pass验证。具体Final Publication实例与Physical Writer唯一性 MUST只由Runtime Factory和Final Publication构造验证。系统 MUST删除以大量节点特例布尔值表达全局编译分支的`ICharacterPoseCompilerHandler`与Registry。

#### Scenario: 两个Goal Source写入同一Slot

- **WHEN** 两个各自局部合法的Goal Contribution节点在同一Graph写入相同Effector Slot
- **THEN** Topology Pass MUST报告两个NodeId和冲突Slot
- **AND** 任一Node Definition MUST不自行扫描Graph或按顺序覆盖

#### Scenario: 删除旧Compiler Handler

- **WHEN** 全部Node Definition已经接管局部Payload、端口和lowering知识
- **THEN** 旧Handler Interface、反射Registry和Player/Slot/Blend等布尔能力矩阵 MUST删除
- **AND** Compiler MUST不保留兼容Adapter调用旧Handler

### Requirement: Pose Compiler必须使用固定不可逆Pass链

唯一Pose Compiler Module MUST按`Graph Closure -> Typed Lowering -> Topology -> Symbolic Family Lowering -> Stage Schedule -> Value Lifetime -> Workspace Plan -> Bind Family Payload -> Seal Program Image`顺序执行。Graph Closure MUST只通过root catalog、PoseState引用与Node Definition Graph dependency投影展开Subgraph和Linked Pose call，不得中央switch具体Payload。Symbolic Family Lowering MUST先固定每个Operation的Family、symbolic typed value依赖、跨帧状态需求、Frame页需求与Workspace需求；Stage固定后 Value Lifetime才能计算真实消费寿命，Workspace Plan才能分配容量，Bind Family Payload只能绑定既有typed handle而不得发现新的Operation或容量需求。每个Pass MUST只消费上一个或明确前置Pass的不可变Result，不得原地修改共享`CompilationState`、回读后续Pass的临时字段或让多个Pass共同拥有同一可变集合。Compiler外部Interface MUST只接受一个typed Compilation Request并返回一个Program Image或结构化失败。

#### Scenario: Value规划失败

- **WHEN** Typed Topology合法但Value生命周期或类型无法分配
- **THEN** Value Plan Pass MUST返回带Pass、GraphId、NodeId、PortId和reason的失败
- **AND** 已生成的前置不可变Result MUST只作为本次失败的内部诊断上下文而不得发布，Workspace、Payload和Program Image MUST不生成部分产物

#### Scenario: Compiler入口调用

- **WHEN** Character Build编译一个合法Pose Graph
- **THEN** 唯一Compiler Module MUST依次产生不可变Pass Result并Seal一个Program Image
- **AND** 其它Compiler facade MUST不复制参数、重跑Pass或现场修改Result

### Requirement: Graph Closure Pass必须唯一展开全部可达图

Graph Closure Pass MUST从root-owned flat graph catalog、PoseState inline GraphId和每个Node Definition发布的Graph dependency建立稳定可达闭包。Subgraph call与Linked Pose call target MUST只由匹配Definition从typed Payload投影，中央Compiler不得按NodeKind、Payload C#类型或显示名解释目标。Closure MUST验证GraphId、Entry、Output、call identity、递归与悬空引用，并按稳定identity生成唯一call-site lineage；后续Pass MUST只读取该Closure，不得再次遍历authoring对象树或动态展开Subgraph。

#### Scenario: Subgraph递归

- **WHEN** root、State inline、Subgraph或Linked Pose call形成递归依赖
- **THEN** Closure Pass MUST报告完整稳定call链并停止编译
- **AND** Runtime MUST不包含动态递归或最大深度fallback

### Requirement: Typed Lowering Pass必须只通过Node Definition生成IR

Typed Lowering Pass MUST把Closure中的authoring node、payload和edge降低为不引用Unity Editor对象的typed Pose IR。每个节点 MUST通过匹配Node Definition完成Payload读取、局部校验和lowering；IR MUST保存稳定Graph/Node/call-site identity、typed ports、Graph Role、Execution Domain和source path。Compiler MUST不在中央switch中再次解释同一Payload字段。

#### Scenario: Payload类型与Definition不匹配

- **WHEN** authoring node的Payload类型不符合其唯一Node Definition
- **THEN** Typed Lowering Pass MUST在进入Topology前报告稳定失败
- **AND** MUST不通过Activator默认创建Payload或按NodeKind猜测字段

### Requirement: Topology Pass必须统一证明全局执行闭包

Topology Pass MUST从typed IR建立唯一有向拓扑和稳定执行依赖，验证Port kind、Pose空间、Graph Role、source/parameter使用、write set、Operation Domain、World-aware依赖、Goal Contribution、唯一Assembler、唯一Goal Set、唯一FBBIK、唯一Output和唯一Final Publication requirement。它 MUST生成后续Value与Stage规划使用的不可变Topology Plan；Runtime MUST不重复这些静态证明。Graph call递归只属于前置Closure Pass；Physical Writer不是Graph节点，Topology MUST不引用具体Writer Implementation或证明Runtime实例数量。

#### Scenario: World-aware节点位于非法阶段

- **WHEN** typed edge要求一个PurePose Operation在尚未完成的World-aware结果之前消费该结果
- **THEN** Topology Pass MUST拒绝Graph并报告依赖链
- **AND** Stage Scheduler MUST不通过重排无关节点或Runtime等待补救非法拓扑

#### Scenario: 无Goal贡献角色

- **WHEN**正式Graph保留唯一Assembler和FBBIK但没有有效Goal Contribution
- **THEN** Topology Pass MUST生成固定零贡献拓扑并允许Assembler发布`GoalCount=0`
- **AND** MUST不插入Empty Goal、删除FBBIK或建立第二输出路径

### Requirement: Value与Workspace必须由独立Pass按类型和寿命规划

Value Lifetime Pass MUST按已经固定的Stage Schedule为Local Pose、Component Pose、Parameter、Discontinuity、Source Demand、Goal Contribution、Goal Set、Control与Output分配typed Value地址和生命周期。Workspace Plan Pass MUST按Symbolic Operation需求、Stage、Value寿命、Rig、节点状态、source并发、Constraint容量、Inertialization、Operation completion、Final Publication layout requirement和Diagnostics manifest分配固定页与handle。Final Pose物理页 MUST由Final Publication按该requirement分配，Program Workspace不得分配第二份。两者 MUST不使用作者字符串、运行时动态扩容或万能Value slot，也 MUST不允许后续Bind Family Payload回写容量。

#### Scenario: 两类Value错误复用

- **WHEN** 某Operation尝试把Goal Contribution地址作为Goal Set或Pose地址使用
- **THEN** Value Lifetime或Program Image Seal MUST拒绝该引用
- **AND** Runtime MUST不通过共享整数index解释不同Value种类

#### Scenario: 编译容量不足

- **WHEN** Graph topology要求的并发source、Stored Pose、Goal或workspace超过正式编译上限
- **THEN** Workspace Plan Pass MUST在Build时报告精确Owner和所需容量
- **AND** Runtime MUST不动态扩容、裁剪节点或复用仍存活slot

### Requirement: Operation必须使用公共Header与分段Family Payload

Program ABI MUST使用公共`CharacterPoseOperationHeader`保存Operation index、Operation Code/Family、Execution Domain、Payload index和typed输入输出Value range。Parameter Input/Resolve、Player、StateMachine、Action Input、AnimationSlot、Blend、Inertialization、Composition、Space Conversion、Component Control、Motion Matching、Pose History、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose与Output MUST分别保存到对应固定Family Payload页。系统 MUST不保留包含全部节点可选字段的万能Operation记录，也 MUST不使用大量`-1`组合表达字段不适用。

#### Scenario: Player Operation被错误绑定到FBBIK Payload

- **WHEN** Program Image Seal发现Header Family与Payload页或Payload index不匹配
- **THEN** Projection Build MUST失败并定位Operation与Node source path
- **AND** Runtime MUST不尝试按字段存在性猜测Operation种类

#### Scenario: 修改一种Operation Family

- **WHEN** Foot Placement或Linked Pose Family新增一个正式编译字段
- **THEN** schema变化 MUST只修改对应Definition、Family Payload、Family Evaluator和必要source map
- **AND** 无关Player、Blend、Slot和Output Payload MUST不增加占位字段

### Requirement: 全部现行Operation必须进入唯一Family迁移表

ABI切换前 MUST为全部现行Operation Code建立并封存唯一迁移表，至少记录新Operation Family、跨帧状态Owner、Frame页Owner、Execution Domain、symbolic Value依赖、Workspace需求和删除的旧万能字段。Parameter、Action Input、Motion Matching chooser/capture/processing/internal blend、Pose History、Constraint与Output MUST全部进入该表；未映射Operation MUST阻止Projection schema切换，系统 MUST不保留旧reader处理遗漏项。

#### Scenario: Motion Matching History Commit未映射

- **WHEN** 迁移表缺少当前正式`PoseHistoryCommit`或任一Motion Matching Operation Code
- **THEN** Character Build MUST在创建新Program Image前失败并定位缺失Code
- **AND** MUST不把该Operation降级为通用Payload、跳过执行或交给旧Native Program

### Requirement: Stage Schedule必须由typed依赖和Execution Domain唯一生成

Stage Schedule Pass MUST按Topology Plan、Symbolic Operation依赖、Pose空间和Execution Domain生成固定`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwareValue`、`PureValue`与`FinalPublication`Stage，并为每个Operation分配恰好一个Stage位置。Schedule MUST静态保证source每帧最多capture一次、每Operation最多执行一次、每个Constraint Family Operation在自己的位置调用一次、Constraint完整结果先于消费者、唯一Output layout在全部依赖完成后产生。Physical Writer由Runtime Factory装配的Final Publication在该结果之后执行，不属于Graph Operation。Runtime与Preview MUST只执行该Schedule，不得现场重新排序。

#### Scenario: Operation出现在两个Stage

- **WHEN** Stage Schedule生成结果包含重复Operation index或遗漏可达Operation
- **THEN** Seal Program Image Pass MUST拒绝Program
- **AND** Runtime MUST不通过completion检查跳过重复项或后台补执行遗漏项

### Requirement: Program Image必须在Seal后不可变且自描述完整

Seal Program Image Pass MUST验证全部Pass identity、schema、Rig、Operation Header、Family Payload、typed Value、Workspace handle、Stage、Source Map、容量和PoseProgramImageHash，并把不可变`CharacterPoseProgramImage`作为`CharacterPresentationProjection`内部唯一语义Pose程序随同一ProjectionRevision发布。Program Image MUST包含Runtime装配所需的完整固定数据，不得保存authoring asset、Editor对象、Actor状态、Frame页、运行时Tuning或运行时编译器。Runtime MUST不重新编译、重排或构造第二语义Program；如需不可序列化执行存储，每个Program Runtime只能建立最多一份同identity、actor-local、只读的Execution View并唯一Dispose。任何内容或schema变化 MUST提升PoseProgramImageHash与ProjectionRevision并要求显式Build。

#### Scenario: Runtime加载Program Image

- **WHEN** Character Presentation Runtime加载匹配Profile、Rig和Projection revision的Program Image
- **THEN** Runtime MUST只按Image容量建立自己的actor-local Execution View、Actor State、Program Frame Pages、根Frame Transaction和Module实例
- **AND** MUST不读取Pose Graph资产、Capability Catalog、Node Definition或Compiler

#### Scenario: Program Image与Rig不匹配

- **WHEN** Program Image的Rig identity或PoseBone layout与运行角色不一致
- **THEN** Runtime创建 MUST失败并报告typed配置错误
- **AND** MUST不现场重绑骨骼、重编Program或使用旧Image

### Requirement: Compiler Diagnostic必须保留稳定source lineage

每个Compiler Pass失败 MUST发布结构化Diagnostic，至少包含Pass、stable reason、GraphId、NodeId、可选PortId/call-site、source path和相关identity。Program Image Source Map MUST把Operation、Value、Stage和Family Payload映射回同一稳定authoring lineage。Editor、Build日志与Live Debug MUST读取该信息，不得用Runtime index、异常文本解析或显示名搜索重建来源。

#### Scenario: Goal Slot冲突

- **WHEN** Topology Pass发现两个Contribution写入同一Slot
- **THEN** Diagnostic MUST同时定位两个Graph/Node/call-site和冲突Slot
- **AND** 作者工具 MUST不需要反查Runtime数组或字符串匹配节点标题

### Requirement: 新Program ABI必须破坏性替换旧Projection

分段Operation ABI接入时 MUST提升Presentation Projection schema、Program Image schema、PoseProgramImageHash与ProjectionRevision，并通过显式Character Build重新发布正式generated资产。Gameplay-owned`CharacterPresentationSemanticContract.ContractHash`、SemanticHash、Float32/Fixed ProgramHash与Network identity MUST保持不变。旧`CharacterPresentationPoseOperation`、旧Native Operation镜像、旧codec reader、旧字段默认补齐、兼容schema和Runtime fallback MUST删除；项目 MUST不同时维护两套Program reader或根据版本选择Executor。

#### Scenario: 加载旧Projection

- **WHEN** Runtime或Preview收到旧万能Operation schema生成的Projection
- **THEN** 系统 MUST报告Stale/Invalid并要求显式Build
- **AND** MUST不迁移内存字段、调用旧reader或切换旧Executor

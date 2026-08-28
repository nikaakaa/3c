## MODIFIED Requirements

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

唯一Pose Compiler Module MUST通过`Graph Closure -> Typed IR -> Topology -> Symbolic Family Lowering -> Stage Schedule -> Value Lifetime -> Workspace Plan -> Bind Family Payload -> Seal Program Image`固定Pass链，把同一Pose DAG编译为`CharacterPresentationProjection`内部唯一不可变`CharacterPoseProgramImage`。Graph Closure MUST只通过root catalog、PoseState引用与Node Definition Graph dependency投影展开Subgraph/Linked Pose call。Program Image MUST按typed依赖、Pose空间与Execution Domain保存有序`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwareValue`、`PureValue`与`FinalPublication`Stage，并使用公共Operation Header、typed Value Reference和分段Operation Family Payload；MUST不保存万能Operation可选字段、Actor State、Frame Pending页或运行时Tuning。Runtime MUST不构造第二语义Program，每个Program Runtime只可建立最多一份同identity、actor-local、只读Execution View。

Goal Contribution收集、唯一Goal Assembler、唯一Goal Set、FBBIK和OutputPose MUST进入固定阶段，Projection MUST静态证明每条正式路径最多一个Assembler、一个Goal Set、一个FBBIK、一个OutputPose和一个Final Publication requirement。每个Constraint Family Operation MUST在自己的Stage位置通过typed编译Handle调用Constraint Module一次，Constraint不得扫描Program或维护第二Schedule。Output Family MUST只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不得保存Actor页指针或分配第二Final Pose buffer。具体Final Publication、Physical Bone binding与Writer唯一性 MUST由Runtime Factory和Final Publication构造验证，Compiler不得创建Writer Graph节点。每个source每帧 MUST最多capture一次，每个Operation MUST恰好执行一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由Final Publication中的唯一Writer写一次。Runtime MUST不重新编译、重排、补执行或解释authoring Graph。

#### Scenario: Foot Placement后执行FullBodyIK

- **WHEN** Foot Placement与其它Goal Source完成同Frame Goal Contribution
- **THEN** Stage Schedule MUST在其后执行唯一Goal Assembler和唯一FullBodyIK
- **AND** 后续节点 MUST消费FBBIK输出而不是输入Pose或外层Runtime预先生成的副本

#### Scenario: 编译无Goal贡献的角色

- **WHEN** 某角色的正式Pose Graph没有任何有效Goal Contribution
- **THEN** 唯一Assembler MUST编译为固定容量零贡献并发布`GoalCount=0`
- **AND** Compiler MUST不插入Empty Goal fallback、Goal Set copy或第二Assembler

#### Scenario: Operation被重复调度

- **WHEN** Stage Schedule包含重复Operation index、遗漏可达Operation或不匹配Execution Domain
- **THEN** Program Image Seal MUST拒绝Projection发布
- **AND** Runtime MUST不通过completion检查掩盖非法Schedule

### Requirement: Pose Watch必须只观察已完成Pose与typed目标Value

Editor MUST允许按稳定PoseNodeId与call-site订阅Pose Watch，并允许Goal Contribution、Goal Set与Constraint结果使用只读Target Watch。Watch selection、颜色、显隐和面板状态 MUST只属于Editor view-state。Runtime MUST在Frame开始冻结interest，并在各正式Module完成Pending Result时向固定容量诊断页冻结被订阅的Pose、Value、Contribution、Goal、FBBIK与Physical结果；成功Seal后Watch MUST只读取同Frame、Completion、Program、Projection与Rig lineage的Committed Result。Watch MUST不访问Program内部Workspace、Actor State、Foot Context、FBBIK Vendor对象或Physical Transform反推，也不得重新执行节点、source sample、world query或FBBIK。

#### Scenario: 同时观察FootPlacement和FullBodyIK

- **WHEN** Frame开始时Foot Placement Goal与FullBodyIK Pose都启用Watch且Frame成功Seal
- **THEN** 两者 MUST来自同一Committed Program与Constraint Result lineage
- **AND** Watch MUST不重新执行Foot Placement、Goal Assembly或FBBIK

#### Scenario: Frame中途启用Watch

- **WHEN** 作者在当前Frame已经开始后启用新的Pose Watch
- **THEN** 当前正式结果 MUST保持不变且新详情 MAY从下一成功Frame开始
- **AND** Diagnostics MUST不读取Pending Workspace补齐半帧结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为`CharacterPresentationProjection`内部唯一不可变`CharacterPoseProgramImage`，并由同一Factory装配actor-local Execution View、`CharacterPoseProgramRuntime`、`CharacterPoseSourceModule`、`CharacterPoseConstraintRuntime`、`CharacterFinalPosePublication`、根Frame Transaction和actor-local Tuning Snapshot。正式Runtime与Preview MUST直接读取同一Projection内Program Image并让各自Program Runtime遵守同一Execution View materialization/Dispose规则，不得创建第二语义Program；二者 MUST使用同一Program Image schema、Stage Schedule、Operation Family evaluator、source backend、world-query Adapter、FinalIK Pose Buffer backend、Final Writer和completion语义；Live Debug MUST只读取对应Committed Result。每帧每个source、Player、Action lifecycle、Transition、Slot、composition、转换、Goal Source、Assembler、FBBIK和Writer MUST只执行一次正式计划。Graph mutation或Stale Projection时Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间、Node Definition字段或Foot Placement使Projection变为Stale
- **THEN** Preview MUST停止消费旧Program Image
- **AND** MUST不创建临时Program、旧ABI reader、默认空间转换或旧Projection fallback

#### Scenario: Preview缺少world context

- **WHEN** Preview执行同一Program Image到Foot Placement但精确World Context Adapter不可用
- **THEN** Program Runtime MUST发布typed Unavailable并停止该Frame publication
- **AND** Preview MUST不使用简化Constraint或跳过该Operation

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port与Edge MUST继续使用共享typed domain document。每个正式Node Kind MUST通过唯一`CharacterPoseNodeDefinition` Adapter声明Payload字段、固定端口、条件`portVariants`、动态端口政策、Graph Role、Execution Domain、Operation Family、Graph dependency与typed lowering。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Canvas、Document v4 Exporter/strict parser/Target Mapper、Clipboard、Reconciler、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency、typed lowering与Source Map。系统 MUST不保留第二节点目录、重复字段switch、`ICharacterPoseCompilerHandler`布尔能力矩阵或独立Compiler binding真相。跨节点拓扑规则 MUST只属于唯一Topology Pass。

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、Document v4、Clipboard、统一Port Shape、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少Document投影

- **WHEN** 一个Definition无法为正式Document/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** Agent authoring MUST不使用通用SerializedProperty或自由文本绕过

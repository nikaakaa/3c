## ADDED Requirements

### Requirement: Pose只读Blackboard必须按正式输入范围投影

FlowCanvas原生Blackboard MUST展示当前Root、StatePose、Subgraph或Linked Pose入口可访问的正式输入，区分动画实例变量、只读角色事实和显式子图参数。输入来源和跨图可见性 MUST由唯一声明与接口合同提供，MUST不在每张图复制共享声明。节点配置与随Pose传播的曲线 MUST使用各自正式作者入口，不混入共享变量列表。

面板 MUST以作者名称、类型、来源、作用范围和使用情况为主要信息；内部稳定identity只用于引用、详情和诊断。合法未使用声明 MAY通过筛选展示，但筛选 MUST不改变正式声明或连接。拖拽 MUST通过原typed Mutation创建绑定正确声明的Get，PoseGraph主图 MUST不提供共享动画变量Set。重命名 MUST不改变稳定引用身份。

#### Scenario: 子图显示可访问输入

- **WHEN** 作者进入一个具有公开参数与共享动画变量读取权限的子图
- **THEN** 面板 MUST只列该接口和范围允许读取的声明，并显示各自来源
- **AND** MUST不复制根图全部变量或全部BlendShape曲线

#### Scenario: 变量声明尚未连线

- **WHEN** 一个可访问的合法输入尚未被节点消费
- **THEN** 作者 MUST仍能找到该声明并拖出Get，界面 MAY标记未使用
- **AND** 系统 MUST不因没有消费者删除声明或强制要求根Blackboard为空

#### Scenario: 输入跨范围或类型失配

- **WHEN** Get引用不可访问声明、已删除变量或类型不匹配的输入
- **THEN** Mutation或Compiler MUST在对应Graph/Node/Field返回明确诊断
- **AND** MUST不按显示名猜测另一个声明、创建默认变量或绕过正式接口

## MODIFIED Requirements

### Requirement: Pose参数必须通过typed页面和显式解析传播

Pose Graph MUST通过正式合同引用稳定ParameterId、类型、默认值与允许来源。动画实例变量的声明与更新 MUST由同一正式动画变量owner提供，PoseGraph主图 MUST只读取该实例交接的值，不提供共享变量Set、不复制运行时Blackboard；EventGraph的事件、Set和生命周期不由PoseGraph消费侧另行定义。角色表现事实 MUST只从正式同帧Fact合同读取。

source-local曲线参数 MUST随指定Pose Value传播；既有显式`Base | Overlay | Weighted | Max | Min`解析和适用Inertialization响应 MUST保持。曲线与BlendShape MUST不作为每张图重复拥有的外部变量声明。Compiler MUST区分输入变量与Pose曲线的读取来源、类型、作用范围和执行依赖，并完整收集可达输入及曲线布局；MUST不通过清空root.Parameters省略仍被运行时消费的数据。节点 MUST不按字符串、GameplayTag或State显示名查找运行参数。

#### Scenario: Blend权重读取Program参数

- **WHEN** BlendPose权重连接ProgramParameterInput
- **THEN** Compiler MUST校验ParameterId、类型、可访问范围和正式page layout
- **AND** Runtime MUST只读取同一动画实例提供的值，不读取可变Gameplay对象或建立第二变量表

#### Scenario: Body读取混合后的脚权重

- **WHEN** Body内部FootPlacement权重绑定输入Pose的正式曲线
- **THEN** FootPlacement MUST读取该Pose上游混合后的同一曲线值
- **AND** 根图无需为了内部曲线读取复制变量声明；有真实外部控制需求时 MAY由Body显式公开输入
- **AND** 权重 MUST保持既有Goal可见权重作用，不释放Anchor、不清零连续历史、不改变Landing Reach准入

#### Scenario: 从Blackboard移除BlendShape属性投影

- **WHEN** 动画属性不再被投影为每张PoseGraph的变量
- **THEN** 正式曲线采样、混合、编译布局与最终BlendShape写入 MUST继续由原唯一链路完成
- **AND** 系统 MUST不丢弃仍被消费的曲线或用默认值掩盖缺失依赖

### Requirement: Pose Plan必须按拓扑编译为有序执行阶段

唯一Pose Compiler Module MUST通过`Graph Closure -> Typed Lowering -> Topology -> Symbolic Family Lowering -> Stage Schedule -> Value Lifetime -> Workspace Plan -> Worker Batch Plan -> Bind Family Payload -> Seal Program Image`固定Pass链，把同一Pose DAG编译为`CharacterPresentationProjection`内部唯一不可变`CharacterPoseProgramImage`。Graph Closure MUST只通过root catalog、PoseState引用与Node Definition Graph dependency投影展开Subgraph/Linked Pose call。Program Image MUST按typed依赖、Pose空间与Execution Domain保存有序`FactAndDemand`、`SourceCapture`、`PurePose`、`WorldAwareValue`、`PureValue`与`FinalPublication`Stage，并使用公共Operation Header、typed Value Reference、分段Operation Family Payload、Worker Batch Plan、Kernel Set、Rig执行布局与Execution Policy；MUST不保存万能Operation可选字段、Actor State、Frame Pending页、平台线程对象或运行时Tuning。Runtime MUST不构造第二语义Program，每个Program Runtime只可建立最多一份同identity、actor-local、只读Execution View。

Goal Contribution收集、唯一Goal Assembler、唯一Goal Set、FBBIK和OutputPose MUST进入固定阶段，Projection MUST静态证明每条正式路径最多一个Assembler、一个Goal Set、一个FBBIK、一个OutputPose和一个Final Publication requirement。每个Constraint Family Operation MUST在自己的Stage位置通过typed编译Handle调用Constraint Module一次，Constraint不得扫描Program或维护第二Schedule。Output Family MUST只保存稳定`CharacterFinalPosePublicationLayoutHandle`，不得保存Actor页指针或分配第二Final Pose buffer。具体Final Publication、Physical Bone binding与Writer唯一性 MUST由Runtime Factory和Final Publication构造验证，Compiler不得创建Writer Graph节点。每个source每帧 MUST最多capture一次，每个Operation MUST恰好执行一次，PlayableGraph MUST最多Evaluate一次，Physical Transform MUST只由Final Publication中的唯一Writer写一次。Runtime MUST不重新编译、重排、补执行或解释authoring Graph。

Worker Batch Plan MUST只把Compiler静态证明线程安全的Pure Pose Operation归入AOT可知Family Kernel，并为跨域交接保存精确Completion依赖。会话级唯一Worker Scheduler MAY按相同Program、Rig执行布局、依赖波次与Kernel identity跨Actor组成批次，但 MUST保持每个Actor内部Stage、Operation次数、source时间、Fault和Final Publication语义；不得为每个Node／Bone创建Job，不得让每Actor独立调度后立即等待，也不得通过跳帧、旧Pose复用或插值补帧形成动画预算。

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

#### Scenario: Pure Pose Operation缺少线程安全Kernel

- **WHEN** 可达Pure Pose Operation无法映射到唯一Family Kernel或其write set与同波次批次冲突
- **THEN** Program Image Seal MUST拒绝Projection发布并定位Definition、Operation与冲突范围
- **AND** Runtime MUST不把该Operation交给旧串行Executor或Managed fallback

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

#### Scenario: 同时观察State Player和FootPlacement

- **WHEN** 两个节点都启用Pose Watch
- **THEN** diagnostics MUST从同一frame lineage发布Local State Player Pose、FootPlacement Goal与FullBodyIK输出Pose
- **AND** MUST不额外Evaluate PlayableGraph或读取Transform反推结果

### Requirement: Preview、Runtime与Live Debug必须复用同一固定Pose Plan

Projection Compiler MUST把Pose Graph降低为`CharacterPresentationProjection`内部唯一不可变`CharacterPoseProgramImage`。正式Runtime与Scene Play Preview MUST通过正式Character Session运行同一Simulation Program、Presentation Projection、Program Image、Stage Schedule、Worker Batch Plan、Family Kernel、Execution Policy、Operation Family evaluator、source backend、world-query backend、FinalIK Pose Buffer backend、Final Writer和completion语义。Pose作者窗口 MUST只发起Scene Play、选择观察目标并读取全部Job完成且根事务成功Seal后的Committed Result；MUST不创建独立Animation Preview Runtime、第二播放时钟、第二语义Program或直接修改Gameplay状态的Seek路径。每帧每个source、Player、Action lifecycle、Transition、Slot、composition、转换、Goal Source、Assembler、FBBIK和Writer MUST只由正式Session执行一次。Graph mutation、Kernel Set变化或Stale Projection时Scene Play Preview MUST停止并等待显式Build。

#### Scenario: Graph修改后继续Preview

- **WHEN** 作者修改State、Slot、Rig、Pose空间、Node Definition字段、Execution Domain或Foot Placement使Projection变为Stale
- **THEN** Scene Play Preview MUST停止消费旧Program Image与旧Worker Kernel
- **AND** MUST不创建临时Program、串行Executor、旧ABI reader、默认空间转换或旧Projection fallback

#### Scenario: Preview缺少world context

- **WHEN** Preview执行同一Program Image的前置Worker批次后到达Foot Placement但精确World Context Adapter不可用
- **THEN** Program Runtime MUST发布typed Unavailable并停止该Frame publication
- **AND** Scene Play Preview MUST不使用简化Constraint、跳过该Operation或以Worker Kernel伪造地面结果

#### Scenario: 作者定位到非连续时间

- **WHEN** 作者要求Scene Play Preview定位到与当前时间不连续的目标时间
- **THEN** Preview MUST从正式场景初始状态按已选输入重新推进Session到目标时间
- **AND** Pose窗口 MUST不直接写PoseState、Action、Player、Inertialization、Foot、Goal或FBBIK状态

#### Scenario: Live Debug观察Worker Pose

- **WHEN** 当前Frame的Worker与Managed批次全部成功并由根事务Seal
- **THEN** Live Debug与Pose Watch MUST只读取同Program／Batch／Completion lineage的Committed Pose与Operation结果
- **AND** MUST不等待、重放或再次调度Worker Kernel以补齐诊断

### Requirement: Pose authoring必须使用共享Capability与类型化Presentation Mutation

Pose Graph、PoseStateMachine、Node、Port、Edge、布局与子图 MUST只保存在唯一Pose作者资产模型中，并由唯一Pose Canvas编辑。每个正式Node Kind MUST通过唯一`CharacterPoseNodeDefinition` Adapter声明Payload字段、固定端口、条件`portVariants`、动态端口政策、Graph Role、Execution Domain、线程安全能力、Operation Family、Graph dependency与typed lowering。Definition MUST先投影共享`GraphAuthoringCapabilityCatalog`，再由唯一`GraphAuthoringNodePortShapeProjector`把完整端口形状提供给Pose Canvas、Document v4 Exporter/strict parser/Target Mapper、Clipboard、Reconciler、Mutation preflight与局部Validator；Compiler MUST只从同一Definition读取Graph dependency、typed lowering、线程安全事实与Source Map。Canvas创建、连线、删除、复制粘贴、Undo和Details编辑 MUST通过typed Presentation Mutation提交；当编辑来自CanvasCore `GraphEditor`表面时，写入 MUST经由Graph原生API入口（AddNode/ConnectNodes/RemoveNode/RemoveConnection等）路由到同一typed Mutation与preflight，Undo在编辑器会话内由CanvasCore唯一拥有，外部入口（MCP、Inspector、Clipboard、正式写入命令）仍由Document Transaction唯一拥有；Canvas运行委托、反射方法和事件流 MUST不成为Pose执行语义。旧Pose作者资产和旧Canvas在迁移后 MUST删除，系统 MUST不保留第二节点目录、镜像Graph、双写、反向同步、`ICharacterPoseCompilerHandler`布尔能力矩阵或独立Compiler binding真相。迁移清单 MUST只包含明确保留并能进入正式Character Build的Pose内容；被批准整套退役的内容 MUST在迁移前删除，迁移器 MUST不为其生成Canvas资产、空Profile或兼容占位。

#### Scenario: CanvasCore GraphEditor原语路由Mutation

- **WHEN** 作者在CanvasCore `GraphEditor`表面执行建节点、拖线、删除、粘贴或字段编辑原语
- **THEN** Graph写方法 MUST把该原语（含端口索引反查与粘贴NodeId重建）翻译为typed Mutation并经同一preflight校验后应用
- **AND** 编辑器会话内撤销 MUST由CanvasCore Undo唯一记录，不与Document Transaction双记；外部写入入口的Undo归属不变

#### Scenario: 状态机与规则使用同窗口编辑视图

- **WHEN** 作者从Pose节点进入StateMachine或TransitionRule
- **THEN** 同一GraphEditor MAY使用不保存的视图节点显示原Document的稳定实体、端口和布局
- **AND** 该视图 MUST NOT成为第二作者资产、进入Document清单／Compiler或建立运行逻辑；修改 MUST提交原Document Mutation，Undo MUST覆盖实际存储内容的owner

#### Scenario: 观察与重绘保持作者内容

- **WHEN** Canvas重绘、选择节点、恢复窗口或显示运行高亮
- **THEN** 系统 MUST只更新视图状态，不改变作者位置、字段值或业务revision
- **AND** 只有明确的作者编辑操作 MUST提交Mutation；Undo后 MUST按稳定identity读取当前对象，不沿用旧状态机对象

#### Scenario: 新增Pose节点能力

- **WHEN** 新Pose节点注册唯一Definition Adapter
- **THEN** 人工创建菜单、Document v4、Clipboard、统一Port Shape、Validator、Graph Closure和Compiler MUST识别同一Capability与Payload合同
- **AND** MUST不要求在多个Catalog、Handler或NodeKind switch中重复声明同一字段和端口

#### Scenario: Node Definition缺少Document投影

- **WHEN** 一个Definition无法为正式Document/Mutation合同提供完整typed字段、条件端口或Graph dependency
- **THEN** Definition目录或Character Build MUST失败并定位Node Kind
- **AND** Agent authoring MUST不使用通用SerializedProperty或自由文本绕过

#### Scenario: 迁移保留的旧Pose作者资产

- **WHEN** 一个明确保留的现有Pose Graph被迁入新的唯一作者资产
- **THEN** Graph、StateMachine、Node、Port、Edge、布局、子图、稳定identity和资源引用 MUST完整保留
- **AND** 迁移成功后旧资产入口 MUST不可再读写且不得成为运行时fallback

#### Scenario: 遇到已退役的TrainingEnemy PoseGraph

- **WHEN** 作者迁移清单发现`TrainingEnemy`配置或其PoseGraph引用
- **THEN** 该内容 MUST从GameplayLab、构建清单与作者资产中整套删除
- **AND** 迁移器 MUST不读取、转换或保留该Graph

### Requirement: Pose Graph UI必须保留准确术语和serialized identity

Pose编辑表面 MUST为CanvasCore `GraphEditor`（`OpenWindow(graph)`入口），节点视觉（标题、分组、颜色、端口）MUST由唯一Node Definition投影提供；自建GraphView画布在接入完成后 MUST删除，系统 MUST不保留第二画布。Pose Canvas、Document、Mutation、Compiler source map和Diagnostics MUST对同一节点使用相同稳定identity与业务术语，包括Clip Player、Blend Space Player、Selected Pose Player、Animation State Machine、Slot、Layered Blend Per Bone、Inertialization、Locomotion Phase Group、Pose Watch和Output Pose。Canvas MAY提供领域图标、颜色、节点折叠和搜索表现，但 MUST不改变serialized kind、端口identity或Pose空间；MUST不保留旧node kind alias或按Canvas类型名生成业务kind。命名端口视觉完成适配前 MAY接受默认端口显示降级，但端口identity数据与编译语义 MUST不受影响。

#### Scenario: 作者添加单Clip播放器

- **WHEN** 作者在Pose Canvas添加单AnimationClip state-local player
- **THEN** Capability、节点标题、Document kind和编译诊断 MUST统一显示Clip Player
- **AND** MUST不存在Clip Player兼容名称

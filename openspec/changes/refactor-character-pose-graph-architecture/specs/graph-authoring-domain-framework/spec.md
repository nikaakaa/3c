## MODIFIED Requirements

### Requirement: Graph Authoring必须拥有唯一领域框架

系统 MUST提供唯一Graph Authoring Domain Framework，统一承载Graph document、capability catalog、selection、clipboard、search、Details host、Navigator host、Mutation、事务和diagnostics合同。每个领域 MUST只有一个正式作者资产模型和一个具体Canvas；Character Presentation Pose Graph MUST使用自己的唯一Pose Canvas，BTSMTL Gameplay Graph与AI Graph继续使用各自现行具体Canvas。不同领域 MAY使用不同Canvas实现，但 MUST通过同一Framework合同接入，且 MUST不共享runtime node或compiler operation。

#### Scenario: 打开不同领域Graph

- **WHEN** 作者分别打开BTSMTL Graph与Pose Graph
- **THEN** 两者 MUST加载各自唯一具体Canvas，并通过同一Framework合同提供selection、clipboard、Details、Navigator、Mutation和diagnostics
- **AND** 每个document MUST只加载本领域的asset adapter、capability与mutation

#### Scenario: 跨领域粘贴节点

- **WHEN** clipboard的domain identity与当前document不一致
- **THEN** Framework MUST在mutation前拒绝粘贴
- **AND** MUST不猜测或转换另一领域的payload

#### Scenario: Pose尝试保留第二作者画布

- **WHEN** Pose迁移已经发布新的唯一作者资产和具体Canvas
- **THEN** 旧Pose Canvas、旧作者资产入口和双向同步 MUST不可再写
- **AND** 系统 MUST不通过只读镜像、兼容导入或旧窗口保存建立第二作者路径

### Requirement: Graph Canvas必须复用统一节点与端口投影

每个领域的具体Canvas MUST通过document projection和Capability生成Node View、Port View、Edge View、创建菜单、搜索结果与clipboard payload，并通过正式Mutation提交作者操作。Pose Canvas MUST从唯一Pose Node Definition投影字段、固定端口、条件端口、动态端口、Pose空间和连接政策；不得从Canvas运行节点、反射方法、现有edge或显示名推断业务语义。固定端口 MUST来自Capability，动态端口 MUST由node-local稳定identity声明并接受同一port policy裁决。转换节点 MUST作为普通serialized authoring节点显示，compiled stage与运行时index MUST不进入作者数据。

#### Scenario: 作者连接Goal Contribution与Assembler

- **WHEN** 作者从FootPlacement拖出Goal Contribution并连接唯一Goal Assembler
- **THEN** Pose Canvas MUST显示typed Contribution edge并保留稳定port identity
- **AND** 框选、复制粘贴、Undo与Document往返 MUST保持Contribution到Assembler的完整拓扑

#### Scenario: 节点拥有动态输入

- **WHEN** GraphInput或GraphOutput增加一个显式Component Pose动态port
- **THEN** Pose Canvas MUST使用节点局部稳定identity投影并保存该port
- **AND** clipboard与Document往返 MUST保留其Pose空间

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph包含LocalToComponentPose
- **THEN** Pose Canvas MUST显示Local输入与Component输出
- **AND** Diagnostics MAY显示compiled stage但作者数据 MUST不保存stage index

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST继续通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、固定端口、条件`portVariants`、动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、只读线程安全能力、允许连接、资源引用、创建菜单、显示标题、Details provider与Mutation入口。Pose领域 MUST由唯一`CharacterPoseNodeDefinitionModule`为每个正式Node Kind集中声明Payload、字段、端口、Graph Role、Execution Domain、线程安全能力、Operation Family、Graph dependency、局部校验与typed lowering，并向共享Capability投影同一节点局部语义；Capability MUST不再保存与Pose Definition重复的Compiler Handler或布尔能力矩阵。线程安全能力由代码Definition固定，用于Compiler生成Worker Batch并只读投影给Agent context；作者UI、Document v4正文、Clipboard与Mutation MUST不提供Burst、线程、Job、Batch size或调度策略字段。

BTSMTL、AI与其它Graph领域 MAY通过各自正式Definition Adapter向同一Framework提供Capability，但 MUST不被迫引用Pose运行类型。唯一`GraphAuthoringNodePortShapeProjector` MUST只从Capability、typed properties与node-local动态端口合成固定、唯一命中的条件端口和动态端口，并拒绝三类端口identity重叠。人工UI、Document exporter、strict parser、Target Mapper、Clipboard、Reconciler、Mutation preflight和Validator MUST只消费同一Capability与Port Shape；不得各自判断mode、构造默认Node或从现有edge反推端口。Compiler MUST从同一Pose Definition读取Graph dependency与typed lowering。Definition与Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

Pose Node Definition只拥有节点局部作者语义、直接Graph dependency与lowering语义，MUST不接管Document package路径、文件闭包、diff、Undo、rollback、save、reverse export或五个MCP生命周期；现有Reconciler与Document Transaction Service MUST继续分别拥有唯一对账和事务生命周期。Definition变化如果改变Agent能看到、创建、连接或必须验证的语义，MUST同步Document v4模型、Presentation codec/exporter、Target Mapper、唯一Reconciler、typed Presentation Mutation、Validator与`btsmtl-agent-authoring`当前合同。

#### Scenario: FootPlacement声明Goal Contribution输出

- **WHEN** FootPlacement Pose Definition声明`pose.component`与`component.full-body-ik-goal-contribution`两个输出
- **THEN** Capability与唯一Port Shape Projector MUST让Canvas、Document、Reconciler、Mutation、Validator与Compiler识别两个稳定port及其lineage规则
- **AND** MUST不把Goal Contribution伪装成Pose、动态字符串port或隐藏Compiler字段

#### Scenario: Goal Contribution连接错误节点

- **WHEN** 作者或Document把`component.full-body-ik-goal-contribution`连接到未声明该输入类型的节点
- **THEN** Mutation MUST在写资产前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者注册一个新的Component Pose骨骼控制节点
- **THEN** 唯一Pose Definition MUST声明其Component Pose端口、execution domain、typed payload、Operation Family、Graph dependency与typed lowering
- **AND** Capability、人工创建菜单、Document、Validator和Compiler MUST同时识别该能力而不得注册第二Compiler Handler

#### Scenario: 作者尝试修改线程执行方式

- **WHEN** UI、Document或MCP命令尝试为Pose节点写入Burst、线程、Job、Batch size或调度策略字段
- **THEN** Mutation MUST在写资产前拒绝该命令并返回稳定诊断
- **AND** Compiler MUST只使用Node Definition的只读线程安全事实生成Worker Batch

#### Scenario: capability未声明字段

- **WHEN** UI或Document尝试写入当前node Definition未声明的字段
- **THEN** Mutation MUST拒绝该命令并返回稳定诊断
- **AND** MUST不通过SerializedProperty path、自由文本或Reconciler特例绕过目录

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或Document创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: Definition尝试接管Document事务

- **WHEN** Pose Definition Adapter尝试直接修改Unity对象、执行apply、创建Undo或发布canonical package
- **THEN** Framework MUST拒绝该依赖并保持正式Reconciler与Transaction Service调用链
- **AND** MUST不建立Pose专用Document入口或第二事务Owner

## MODIFIED Requirements

### Requirement: Graph Authoring必须拥有唯一领域框架

系统 MUST在BTSMTL作者层提供唯一Graph Authoring Domain Framework，统一承载Graph document、capability catalog、canvas、node view、port view、selection、clipboard、search、Details host、Navigator host、Mutation和diagnostics契约。技能与AI领域 MUST复用既有共享作者基础；它们与Character Presentation Pose领域分别适配该框架。Pose MUST保持独立正式Graph数据、runtime node和compiler operation，不能被迫继承BTSMTL图类型。

#### Scenario: 打开不同领域Graph

- **WHEN** 作者分别打开BTSMTL Graph与Pose Graph
- **THEN** 两者 MUST复用同一套canvas、node、port与selection交互
- **AND** 每个document MUST只加载本领域的asset adapter、capability与mutation

#### Scenario: 跨领域粘贴节点

- **WHEN** clipboard的domain identity与当前document不一致
- **THEN** 框架 MUST在mutation前拒绝粘贴
- **AND** MUST不猜测或转换另一领域的payload
#### Scenario: 角色控制配置入口

- **WHEN** 作者打开角色控制binding
- **THEN** MUST显示已登记代码模块与允许编辑的参数
- **AND** MUST不生成角色RootTree页面或把C#控制伪装为万能节点


### Requirement: Navigator与Data Catalog必须复用统一信息架构

框架 MUST提供统一Navigator、breadcrumb、Data Catalog、搜索与Open命令宿主。领域adapter MUST只投影真实owner、引用、页面和业务分组，并使用业务显示名与Unity资源名作为作者标签；不得保存第二份authoring数据，也不得在缺失显示名时回退显示GUID、hash或stable identity。跨资产字段修改 MUST通过Open Owner导航到唯一正式编辑入口。

#### Scenario: Pose Navigator显示Producer

- **WHEN** 作者从精确Character Definition上下文打开Pose Graph
- **THEN** Navigator MUST投影Profile、Pose Source Slot、实际资源、Action producer、Pose graph页面与引用
- **AND** MUST不复制resource binding或Timeline字段供当前页面直接修改
- **AND** MUST不把内部identity作为Navigator项目名称
#### Scenario: 技能子图导航

- **WHEN** 作者从SkillDefinition进入多层inline／shared子图
- **THEN** Navigator MUST保持真实owner、签名、调用点和页面路径
- **AND** 控制模块数据 MUST以自己的正式配置入口显示，不复制到技能Blackboard


### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST继续通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、固定端口、条件`portVariants`、动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider与Mutation入口。Pose领域 MUST由唯一`CharacterPoseNodeDefinitionModule`为每个正式Node Kind集中声明Payload、字段、端口、Graph Role、Execution Domain、Operation Family、Graph dependency、局部校验与typed lowering，并向共享Capability投影同一节点局部语义；Capability MUST不再保存与Pose Definition重复的Compiler Handler或布尔能力矩阵。

BTSMTL、AI与其它Graph领域 MAY通过各自正式Definition Adapter向同一Framework提供Capability，但 MUST不被迫引用Pose运行类型。唯一`GraphAuthoringNodePortShapeProjector` MUST只从Capability、typed properties与node-local动态端口合成固定、唯一命中的条件端口和动态端口，并拒绝三类端口identity重叠。人工UI、Document exporter、strict parser、Target Mapper、Clipboard、Reconciler、Mutation preflight和Validator MUST只消费同一Capability与Port Shape；不得各自判断mode、构造默认Node或从现有edge反推端口。Compiler MUST从同一Pose Definition读取Graph dependency与typed lowering。Definition与Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

Pose Node Definition只拥有节点局部作者语义、直接Graph dependency与lowering语义，MUST不接管Document package路径、文件闭包、diff、Undo、rollback、save、reverse export或五个MCP生命周期；现有Reconciler与Document Transaction Service MUST继续分别拥有唯一对账和事务生命周期。Definition变化如果改变Agent能看到、创建、连接或必须验证的语义，MUST同步Document v5模型、Presentation codec/exporter、Target Mapper、唯一Reconciler、typed Presentation Mutation、Validator与`btsmtl-agent-authoring`当前合同。

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


### Requirement: 人工编辑与Document Apply必须复用同一类型化Mutation

窗口交互与Agent Authoring Document Reconciler MUST分别把用户操作或目标状态差异降低为同一领域类型化Mutation，再由同一Validator、transaction、dirty owner和Undo边界应用。系统 MUST不允许Document直接写Unity YAML、SerializedObject path、AnimationClip序列化文本或构造第二套Pose/Clip资产写服务。

#### Scenario: UI与Document修改同一Transition

- **WHEN** 人工UI或Document v5修改Pose transition blend policy
- **THEN** 两条入口 MUST生成同一种Presentation Mutation
- **AND** 最终资产约束、诊断和revision变化 MUST一致

#### Scenario: Animation Window入口与Document修改同一Clip Curve

- **WHEN** 两条入口替换同一注册Curve
- **THEN** 两条入口 MUST调用同一Clip Curve validator与Mutation语义
- **AND** MUST进入各自单一Undo事务并产生相同canonical结果

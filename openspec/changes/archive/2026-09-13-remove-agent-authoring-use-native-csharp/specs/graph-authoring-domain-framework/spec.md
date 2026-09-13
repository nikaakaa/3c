## MODIFIED Requirements

### Requirement: Graph Canvas必须复用统一节点与端口投影

Graph Canvas MUST通过document projection和Capability生成通用Node View、Port View、Edge View、创建菜单、搜索结果与clipboard payload。领域adapter MAY提供业务标题、图标、颜色、状态badge与特殊交互命令，但 MUST不重新实现selection、拖线、框选、复制粘贴、Undo或GraphView生命周期。固定端口 MUST来自Capability；动态端口 MUST由node-local稳定identity声明并接受同一port policy裁决。Pose端口 MUST从stable type投影Local/Component空间颜色和标签；非Pose瞬时control value MUST使用独立稳定类型、标签与颜色。转换节点 MUST作为普通serialized authoring节点显示。Canvas MUST不根据C#类型名、显示名或Compiler operation猜测空间，也 MUST不隐藏插入未序列化节点。

#### Scenario: 作者连接Goal Contribution与Assembler

- **WHEN** 作者从FootPlacement拖出Goal Contribution并连接唯一Goal Assembler
- **THEN** Canvas MUST显示typed Contribution edge并保留稳定port identity
- **AND** 框选、复制粘贴、Undo与资产保存重载 MUST保持Contribution到Assembler的完整拓扑

#### Scenario: 节点拥有动态输入

- **WHEN** GraphInput或GraphOutput增加一个显式Component Pose动态port
- **THEN** Canvas MUST使用节点局部稳定identity投影并保存该port
- **AND** clipboard与资产保存重载 MUST保留其Pose空间

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph包含LocalToComponentPose
- **THEN** Canvas MUST显示Local输入与Component输出
- **AND** Diagnostics MAY显示compiled stage但作者数据 MUST不保存stage index

### Requirement: Authoring节点与Runtime执行描述必须分离

Graph Authoring Domain Framework MUST只理解稳定作者identity、typed payload、port与mutation，不得要求authoring node继承runtime node。领域compiler MUST把authoring graph编译为领域自己的中间表示和runtime program；Runtime性能枚举、线性index与switch MAY存在于compiled层，但 MUST不反向成为创建菜单、Details或C#作者参数。

#### Scenario: Runtime增加优化字段

- **WHEN** Pose Runtime为执行计划增加内部offset或buffer index
- **THEN** Authoring capability、Details与C#作者API MUST不自动暴露该字段
- **AND** Compiler MUST负责从Pose IR生成该内部值

## REMOVED Requirements

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“Authoring Capability Catalog必须是人工与C#作者共享的唯一语义目录”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

### Requirement: 人工编辑与Document Apply必须复用同一类型化Mutation

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“人工编辑与C#调用必须复用同一类型化Mutation”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

### Requirement: Formal authoring metadata必须是Agent与UI共享的唯一语义来源

**Reason**: 旧要求把Agent/Document作为正式作者或校验参与方；删除该入口及相应协议场景。

**Migration**: 使用本规范新增的“Formal authoring metadata必须是C#与UI共享的唯一语义来源”。原有领域业务规则和非协议场景随新要求保留；人工和C#直接调用正式业务API，不保留Agent流程或旧场景别名。

## ADDED Requirements

### Requirement: 图代码导出必须复用正式对象与配置合同

正式Graph代码导出 MUST读取当前领域对象、metadata、端口与引用合同，输出对应正式创建和配置调用。导出所需的支持判定、字段读取、依赖/身份、特殊值表达、连接与根挂接 MUST由该领域薄适配提供，公共输出只处理依赖排序、对象变量和C#表达式；系统 MUST不建立第二套节点/字段/owner模型，不先构造Agent JSON或读取旧源码。生成代码 MUST与人工交互共用同一业务规则；缺少任何正式内容的完整输出支持 MUST明确失败，不能通过修改共享定义或省略内容绕过。

#### Scenario: 新增正式节点配置

- **WHEN** 某节点通过正式领域合同增加一个作者配置
- **THEN** 图导出 MUST从该合同及对应配置调用表达该字段
- **AND** 若尚无完整输出方式 MUST明确拒绝导出，不静默使用默认值或另建Agent字段表

### Requirement: Authoring Capability Catalog必须是人工与C#作者共享的唯一语义目录

唯一Framework MUST继续通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、固定端口、条件`portVariants`、动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider与Mutation入口。Pose领域 MUST由唯一`CharacterPoseNodeDefinitionModule`为每个正式Node Kind集中声明Payload、字段、端口、Graph Role、Execution Domain、Operation Family、Graph dependency、局部校验与typed lowering，并向共享Capability投影同一节点局部语义；Capability MUST不再保存与Pose Definition重复的Compiler Handler或布尔能力矩阵。

BTSMTL与其它正式Graph领域 MAY通过各自正式Definition Adapter向同一Framework提供Capability，但 MUST不被迫引用Pose运行类型。BTSMTL Skill正式节点上的Editor-only metadata marker、Gameplay Graph descriptor与正式Mutation binding MUST通过同一Capability投影供C#作者API、原生UI、Validator和Compiler消费。Behavior Designer AI使用插件自己的作者框架，不向该Framework提供Capability。唯一`GraphAuthoringNodePortShapeProjector` MUST只从Capability、typed properties与node-local动态端口合成固定、唯一命中的条件端口和动态端口，并拒绝三类端口identity重叠。人工UI、C#作者API、Clipboard、Mutation preflight和Validator MUST只消费同一Capability与Port Shape；不得各自判断mode、构造默认Node或从现有edge反推端口。Compiler MUST从同一Pose Definition读取Graph dependency与typed lowering。Definition与Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

Pose Node Definition MUST只拥有节点局部作者语义、直接Graph dependency与lowering语义，不拥有Undo、rollback或save；这些继续由正式领域编辑入口管理。删除Document协议后，Definition变化 MUST同步正式Capability、Mutation与Compiler消费者，不得要求新增Agent模型、Exporter、Reconciler或专属Validator。

#### Scenario: FootPlacement声明Goal Contribution输出

- **WHEN** FootPlacement Pose Definition声明`pose.component`与`component.full-body-ik-goal-contribution`两个输出
- **THEN** Capability与唯一Port Shape Projector MUST让Canvas、C#作者API、Mutation、Validator与Compiler识别两个稳定port及其lineage规则
- **AND** MUST不把Goal Contribution伪装成Pose、动态字符串port或隐藏Compiler字段

#### Scenario: Goal Contribution连接错误节点

- **WHEN** 作者或C#代码把`component.full-body-ik-goal-contribution`连接到未声明该输入类型的节点
- **THEN** Mutation MUST在写资产前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者注册一个新的Component Pose骨骼控制节点
- **THEN** 唯一Pose Definition MUST声明其Component Pose端口、execution domain、typed payload、Operation Family、Graph dependency与typed lowering
- **AND** Capability、人工创建菜单、C#作者API、Validator和Compiler MUST同时识别该能力而不得注册第二Compiler Handler

#### Scenario: capability未声明字段

- **WHEN** UI或C#作者API尝试写入当前node Definition未声明的字段
- **THEN** Mutation MUST拒绝该命令并返回稳定诊断
- **AND** MUST不通过SerializedProperty path、自由文本或入口私有分支绕过目录

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或C#代码创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: Definition尝试接管编辑事务

- **WHEN** Pose Definition Adapter尝试直接修改Unity对象、创建Undo或直接保存资产
- **THEN** Framework MUST拒绝该依赖并保持正式领域修改入口的事务边界
- **AND** MUST不建立另一套Pose修改入口或第二事务Owner

### Requirement: 人工编辑与C#调用必须复用同一类型化Mutation

窗口交互与C#作者代码 MUST调用同一领域类型化Mutation，由对应正式领域校验、dirty owner和Undo边界应用。系统 MUST不允许C#调用方直接写Unity YAML、私有SerializedObject path、AnimationClip序列化文本或构造第二套Pose/Clip资产写服务；MUST不要求经过Document、Reconciler或Agent Validator。

#### Scenario: UI与C#代码修改同一Transition

- **WHEN** 人工UI或C#作者代码修改Pose transition blend policy
- **THEN** 两条入口 MUST生成同一种Presentation Mutation
- **AND** 最终资产约束、诊断和revision变化 MUST一致

#### Scenario: Animation Window入口与C#代码修改同一Clip Curve

- **WHEN** 两条入口替换同一注册Curve
- **THEN** 两条入口 MUST调用同一Clip Curve validator与Mutation语义
- **AND** MUST进入各自单一Undo事务并产生相同canonical结果

### Requirement: Formal authoring metadata必须是C#与UI共享的唯一语义来源

Formal authoring type、字段、引用方法和正式Mutation写入方法上的metadata MUST是作者可见kind、typed field、logical port、owner和连接规则的唯一语义来源。共享Capability、正式C#作者API、领域Validator、Compiler和原生UI MUST从该metadata投影同一语义；MUST不维护Agent专用节点模型、字段表、端口表、Pose模型或owner规则副本。

metadata的Editor查找实现 MAY生成不可编辑的静态索引，但该索引 MUST不成为第二作者真相，也不得要求运行时反射、Unity序列化字段或Compiler operation作为作者合同。

#### Scenario: C#和原生UI使用同一作者字段

- **WHEN** 正式作者类型metadata声明一个可写typed field和合法port
- **THEN** C#作者API、原生Details/创建菜单、Validator和Compiler MUST使用同一字段和port语义
- **AND** 不得保留另一套手写Agent或UI能力定义继续接受旧字段

#### Scenario: C#调用未声明作者内容

- **WHEN** C#作者API尝试写入metadata未声明的字段、port、引用或owner关系
- **THEN** 共享Capability或Mutation preflight MUST拒绝该目标并返回稳定诊断
- **AND** MUST不按C#类型名、显示名、SerializedProperty路径或runtime index猜测能力

#### Scenario: 正式作者类型内部重构

- **WHEN** 正式作者类型的C#实现或文件组织变化，但metadata的稳定kind、typed field、logical port和owner语义不变
- **THEN** 原生UI与正式作者能力 MUST保持相同业务语义；C#调用按公开API更新
- **AND** MUST不为此建立Document schema或额外作者协议

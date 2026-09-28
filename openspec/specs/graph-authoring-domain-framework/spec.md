# graph-authoring-domain-framework Specification

## Purpose

定义BTSMTL Gameplay Graph与Character Presentation Pose Graph共享的唯一作者交互框架，同时保持各领域数据、Mutation、Validator、Compiler与Runtime语义隔离。Behavior Designer AI不注册为BTSMTL Graph domain。

## Requirements

### Requirement: Graph Authoring必须拥有唯一领域框架

系统 MUST 复用唯一的公共作者定义和有效编辑合同，包括 document、capability、逻辑端口、selection 接入、clipboard、search、Details、Navigator、Mutation 与 diagnostics。各领域 MUST 由自己的正式原生编辑器提供画布、节点和端口交互，不强制共享旧 GraphView 实现或窗口基类。领域 MUST 不共享正式序列化图基类、运行节点或编译操作，公共定义 MUST 不反向引用具体领域实现。

#### Scenario: 打开不同领域Graph

- **WHEN** 作者分别打开正式技能图与 Pose Graph
- **THEN** 两者 MUST 消费同一公共描述与编辑合同，并使用各自当前正式的原生画布入口
- **AND** 每个 document MUST 只加载本领域的 asset adapter、capability 与 mutation，不创建旧图或画布副本

#### Scenario: 跨领域粘贴节点

- **WHEN** clipboard 的 domain identity 与当前 document 不一致
- **THEN** 框架 MUST 在 mutation 前拒绝粘贴
- **AND** MUST 不猜测或转换另一领域的 payload

### Requirement: 唯一领域框架必须从现有BTSMTL作者UI原地抽象

共享编辑能力 MUST 从当前仍被使用的实现中提取，保留字段编辑、资源选择、Data Catalog、导航、selection、clipboard、Undo 与只读观察行为。旧文件中的有效合同 MUST 与旧 Canvas、Node/Port/Edge View、窗口装配分离；合同使用者 MUST 不被视为旧视图的保留理由。GraphAuthoringCanvasView 及其旧 GraphView 视图 MUST 在有效依赖迁出后删除，不换目录或改名保留。原生 NodeCanvas/FlowCanvas/Slate 类型 MUST 保持原归属，不新增替代画布或功能缩水的写入口。

#### Scenario: 拖出黑板变量

- **WHEN** 作者从正式 Data Catalog 把变量拖到所属领域画布
- **THEN** 迁移 MUST 保留该入口原有变量身份、合法节点创建、typed 端口与领域 mutation 语义
- **AND** MUST 不为保留拖拽而恢复旧 GraphView，也不把有效操作降级成缺失规则的通用创建

### Requirement: Authoring Capability Catalog必须是UI与C#作者的唯一语义目录

唯一Framework MUST继续通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、固定端口、条件`portVariants`、动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider与Mutation入口。Pose领域 MUST由唯一`CharacterPoseNodeDefinitionModule`为每个正式Node Kind集中声明Payload、字段、端口、Graph Role、Execution Domain、Operation Family、Graph dependency、局部校验与typed lowering，并向共享Capability投影同一节点局部语义；Capability MUST不再保存与Pose Definition重复的Compiler Handler或布尔能力矩阵。

BTSMTL与其它正式Graph领域 MAY通过各自正式Definition Adapter向同一Framework提供Capability，但 MUST不被迫引用Pose运行类型。BTSMTL Skill正式节点上的Editor-only metadata marker、Gameplay Graph descriptor与正式Mutation binding MUST通过同一Capability投影供C#作者API、原生UI、Validator和Compiler消费。Behavior Designer AI使用插件自己的作者框架，不向该Framework提供Capability。唯一`GraphAuthoringNodePortShapeProjector` MUST只从Capability、typed properties与node-local动态端口合成固定、唯一命中的条件端口和动态端口，并拒绝三类端口identity重叠。人工UI、C#作者API、Clipboard、Mutation preflight和Validator MUST只消费同一Capability与Port Shape；不得各自判断mode、构造默认Node或从现有edge反推端口。Compiler MUST从同一Pose Definition读取Graph dependency与typed lowering。Definition与Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

Pose Node Definition只拥有节点局部作者语义、直接Graph dependency与lowering语义，MUST不接管Undo、rollback或save；这些继续由正式领域编辑入口管理。删除旧协议后，Definition变化 MUST同步正式Capability、Mutation与Compiler消费者，不得要求新增协议模型、导出器、对账器或专属Validator。

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
- **AND** MUST不通过SerializedProperty path、自由文本或入口私有特例绕过目录

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或C#代码创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: Definition尝试接管编辑事务

- **WHEN** Pose Definition Adapter尝试直接修改Unity对象、创建Undo或直接保存资产
- **THEN** Framework MUST拒绝该依赖并保持正式领域修改入口的事务边界
- **AND** MUST不建立另一套Pose修改入口或第二事务Owner

### Requirement: Graph Canvas必须复用统一节点与端口投影

各领域原生 Canvas、正式创建菜单、搜索和 clipboard MUST 从现有领域定义与共享 Capability 消费同一字段和端口语义。实际节点、端口、连接和交互 MUST 使用当前正式原生类型，不为共享描述创建另一套 GraphView View。固定端口来自正式定义，动态端口保留 node-local 稳定 identity、类型、容量、顺序与同一连接规则；Pose 空间、瞬时值类型、颜色和标签保持。领域特殊命令继续调用唯一正式 mutation；MUST 不根据类名、显示名或编译操作猜测空间，不隐藏插入未序列化节点。

#### Scenario: 作者连接Goal Contribution与Assembler

- **WHEN** 作者从 FootPlacement 拖出 Goal Contribution 并连接唯一 Goal Assembler
- **THEN** 原生画布 MUST 显示 typed Contribution edge 并保留稳定 port identity
- **AND** 框选、复制粘贴、Undo 与资产保存重载 MUST 保持完整拓扑

#### Scenario: 节点拥有动态输入

- **WHEN** GraphInput 或 GraphOutput 增加显式 Component Pose 动态 port
- **THEN** 正式入口 MUST 使用节点局部稳定 identity 保存该 port
- **AND** clipboard 与资产保存重载 MUST 保留其空间和真实业务顺序

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph 包含 LocalToComponentPose
- **THEN** 原生画布 MUST 显示 Local 输入与 Component 输出
- **AND** 作者数据 MUST 不保存执行阶段 index，诊断只消费当前领域正式结果

### Requirement: Details必须只显示当前作者需要的业务字段

Details MUST只投影当前selection、当前capability与当前authoring mode允许查看或修改的字段和命令。Unity资源关系 MUST使用Capability声明的精确对象类型和对象选择器；领域identity关系 MUST使用精确上下文提供的可读选项目录。IdentityReference缺少选项目录时 MUST显示Unavailable并禁止编辑，不得退化为TextField；选项标签 MUST不拼接内部value。稳定identity、revision、GUID、local file id、compiled index、runtime handle、generated path、内部枚举载荷、缓存、Projection中间值与不适用nullable字段 MUST默认隐藏；只读References与Diagnostics MUST放入明确折叠区，且 MUST不伪装成可编辑属性。

#### Scenario: 选择Clip Player

- **WHEN** 作者在Authoring模式选择Clip Player
- **THEN** Details MUST显示类型受限的Source Slot对象选择、loop、play rate、sync与该节点真实可写策略
- **AND** References MUST显示解析后的动画资源与唯一owner
- **AND** MUST不显示Source Id、内部Operation Family、compiled offset或联合体空字段

#### Scenario: identity选项目录不可用

- **WHEN** 当前页面缺少解析某个IdentityReference所需的精确Definition或owner上下文
- **THEN** Details MUST显示该引用不可用及缺失上下文原因
- **AND** MUST不允许作者输入任意字符串绕过目录

### Requirement: Navigator与Data Catalog必须复用统一信息架构

框架 MUST提供统一Navigator、breadcrumb、Data Catalog、搜索与Open命令宿主。领域adapter MUST只投影真实owner、引用、页面和业务分组，并使用业务显示名与Unity资源名作为作者标签；不得保存第二份authoring数据，也不得在缺失显示名时回退显示GUID、hash或stable identity。跨资产字段修改 MUST通过Open Owner导航到唯一正式编辑入口。

#### Scenario: Pose Navigator显示Producer

- **WHEN** 作者从精确Character Definition上下文打开Pose Graph
- **THEN** Navigator MUST投影Profile、Pose Source Slot、实际资源、Action producer、Pose graph页面与引用
- **AND** MUST不复制resource binding或Timeline字段供当前页面直接修改
- **AND** MUST不把内部identity作为Navigator项目名称

### Requirement: StateMachine作者表面必须复用且语义隔离

共享编辑集成 MUST 保留有效的状态、转移、页面、导航与 Details 合同；技能 FSM 和 Pose 状态机继续使用各自正式数据和原生编辑交互，MUST 不强制继承 BaseTreeView、GraphAuthoringCanvasView 或复用旧 GraphView 操作器。状态 payload、transition payload、rule surface、layout owner 与运行语义由各领域拥有。Gameplay condition MUST 不进入 Pose transition，Pose blend/sync MUST 不进入 Gameplay transition。移动、选择、下钻和布局持久化 MUST 保留当前正式 mutation/Undo 边界，Live Debug 不允许作者写入，不新增第二状态机数据模型。

#### Scenario: 打开Gameplay StateMachine

- **WHEN** 作者打开正式技能 FSM
- **THEN** 原生入口及领域 Details MUST 保留条件、priority、interruption 和当前节点交互
- **AND** MUST 不显示 Pose blend duration、sync 或 inertialization

#### Scenario: 打开PoseStateMachine

- **WHEN** 作者打开 Pose StateMachine
- **THEN** 当前原生入口 MUST 保留 Pose State、Transition Rule、blend、sync、source readiness 和已有布局编辑
- **AND** MUST 不创建 BaseGraph、ConditionRuleGraph、旧 GraphView 或第二套状态机模型

#### Scenario: 在Pose StateMachine框选多个状态

- **WHEN** 作者从原生画布空白处框选多个可选状态
- **THEN** 正式原生选择实现 MUST 更新当前选择并由共享合同提供给 Details
- **AND** MUST 不依赖 BaseTreeView 类型判断或新增旧 GraphView 框选器

#### Scenario: Live Debug期间拖动状态

- **WHEN** Pose StateMachine 处于 Live Debug 只读模式且作者尝试拖动状态
- **THEN** 正式入口 MUST 拒绝位置 mutation 并保持 layout 不变
- **AND** MUST 不通过 window-local 缓存记录不可提交的位置

### Requirement: 人工编辑与C#调用必须复用同一类型化Mutation

窗口交互与C#作者代码 MUST调用同一领域类型化Mutation，由对应正式领域校验、dirty owner和Undo边界应用。系统 MUST不允许C#调用方直接写Unity YAML、私有SerializedObject path、AnimationClip序列化文本或构造第二套Pose/Clip资产写服务；MUST不要求经过旧协议对账器或中央作者Validator。

#### Scenario: UI与C#代码修改同一Transition

- **WHEN** 人工UI或C#作者代码修改Pose transition blend policy
- **THEN** 两条入口 MUST生成同一种Presentation Mutation
- **AND** 最终资产约束、诊断和revision变化 MUST一致

#### Scenario: Animation Window入口与C#代码修改同一Clip Curve

- **WHEN** 两条入口替换同一注册Curve
- **THEN** 两条入口 MUST调用同一Clip Curve validator与Mutation语义
- **AND** MUST进入各自单一Undo事务并产生相同canonical结果

### Requirement: Authoring节点与Runtime执行描述必须分离

Graph Authoring Domain Framework MUST只理解稳定作者identity、typed payload、port与mutation。编译执行领域 MUST不要求authoring node继承runtime node；需要编译的领域可以建立自己的内部执行表示，但该表示 MUST不反向成为创建菜单、Details或C#作者参数。明确采用原生执行的 Pose Graph MUST直接使用正式 FlowCanvas 图实例与领域运行宿主，不生成 Pose IR、ProgramImage 或第二套 Pose 执行器。

采用公共C#作者入口不改变领域运行方式。明确采用原生执行的通用 EventGraph MUST复用其原生图实例与节点行为，通过宿主合同规定事件、输入输出、类型和生命周期，不要求再生成项目专用事件IR或解释器。原生实例 MUST与可编辑资产隔离。该执行边界 MUST不自动扩展到现有Skill、FSM或PoseGraph，不放开它们的作者图直接运行。

#### Scenario: Runtime增加优化字段

- **WHEN** 某个编译领域为执行计划增加内部offset或buffer index
- **THEN** Authoring capability、Details与C#作者API MUST不自动暴露该字段
- **AND** 该内部值 MUST只由该领域自己的正式编译链拥有

#### Scenario: 原生事件图调用变量节点

- **WHEN** 已明确采用原生执行的事件图使用宿主准入的 Get/Set
- **THEN** 系统 MUST执行原生节点并遵守唯一变量合同
- **AND** MUST不要求建立同义项目指令或启动另一个备用执行器

#### Scenario: Pose作者图被尝试直接启动

- **WHEN** 调用方试图因新增事件图能力而直接启动不属于正式 Pose 宿主的作者图
- **THEN** 系统 MUST继续拒绝，Pose MUST沿其唯一原生 FlowCanvas 运行入口执行

### Requirement: Formal authoring metadata必须是C#与UI共享的唯一语义来源

Formal authoring type、字段、引用方法和正式Mutation写入方法上的metadata MUST是作者可见kind、typed field、logical port、owner和连接规则的唯一语义来源。共享Capability、正式C#作者API、领域Validator、Compiler和原生UI MUST从该metadata投影同一语义；MUST不维护旧协议专用节点模型、字段表、端口表、Pose模型或owner规则副本。

metadata的Editor查找实现 MAY生成不可编辑的静态索引，但该索引 MUST不成为第二作者真相，也不得要求运行时反射、Unity序列化字段或Compiler operation作为作者合同。

对于BTSMTL Skill领域，formal metadata MUST同时提供当前节点的typed字段读取、真实引用目标读取、稳定identity投影和正式创建/配置入口描述。Skill原节点、FlowCanvas目录、人工Inspector、步骤编辑器和Skill compiler MUST消费这些入口；字段的本地序列化形态与编译所需identity可以不同，但两者 MUST由同一业务定义明确映射，MUST不各自读取或维护第二份字段规则。

#### Scenario: C#和原生UI使用同一作者字段

- **WHEN** 正式作者类型metadata声明一个可写typed field和合法port
- **THEN** C#作者API、原生Details/创建菜单、Validator和Compiler MUST使用同一字段和port语义
- **AND** 不得保留另一套手写作者或UI能力定义继续接受旧字段

#### Scenario: C#调用未声明作者内容

- **WHEN** C#作者API尝试写入metadata未声明的字段、port、引用或owner关系
- **THEN** 共享Capability或Mutation preflight MUST拒绝该目标并返回稳定诊断
- **AND** MUST不按C#类型名、显示名、SerializedProperty路径或runtime index猜测能力

#### Scenario: 正式作者类型内部重构

- **WHEN** 正式作者类型的C#实现或文件组织变化，但metadata的稳定kind、typed field、logical port和owner语义不变
- **THEN** 原生UI与正式作者能力 MUST保持相同业务语义；C#调用按公开API更新
- **AND** 不得仅因内部实现变化建立协议schema

### Requirement: 共享metadata必须提供正式字段读取与配置绑定

共享字段描述 MUST由所属领域的正式能力提供，包含稳定字段identity、typed值类型、默认值/可写性、必要约束，以及读取当前正式对象和调用正式配置API所需的绑定信息。人工UI、原节点适配、FlowCanvas和C#领域输出适配 MUST消费同一信息，不维护第二份节点/字段/owner模型。绑定只表达正式方法与参数对应，不保存源码模板、语法树或操作历史。

#### Scenario: 输出器请求一个正式字段

- **WHEN** C#领域输出适配按字段identity读取当前正式对象
- **THEN** 共享合同 MUST给出当前typed值及所属领域的正式配置入口
- **AND** 该配置入口 MUST与人工编辑使用同一业务规则

#### Scenario: 某字段缺少读取或配置能力

- **WHEN** 字段没有完整的正式读取或配置绑定
- **THEN** 共享合同 MUST明确指出字段和缺失能力
- **AND** 调用者 MUST不回退到私有字段反射、旧协议数据或另建字段表

### Requirement: 共享端口结果必须来自领域正式声明

完整端口形状 MUST由共享Capability、当前typed参数和正式动态接口决定，包含稳定identity、类型、方向、容量与顺序。各领域保持自己的能力模块、业务校验和创建/保存责任；共享层 MUST只组合正式声明，不建立中央角色Validator、业务节点全集副本或整包同步事务。调用者不能为了代码导出自行增加同义端口规则。

#### Scenario: 人工连接与生成代码连接同一端口

- **WHEN** 两个入口提交相同正式参数和连接目标
- **THEN** 它们 MUST获得同一端口形状并调用原领域连接规则
- **AND** 动态端口顺序 MUST保留真实业务顺序，不为源码排版重排

#### Scenario: 新领域能力接入

- **WHEN** 领域模块增加正式节点或引用能力
- **THEN** 它 MUST通过唯一共享描述合同提供自身定义
- **AND** 共享层不得接管该领域运行、Undo或保存实现

### Requirement: 作者合同归位不得演变为框架重写

现有字段、类型、逻辑端口、连接规则和作者操作 MUST 保留原语义。独立公共定义及其依赖闭包 MUST 移出 TreeDesigner，具体对象读取、框架适配和领域规则仍由所属实现拥有；混合类型 MUST 按成员依赖拆分并同步直接调用。MUST 不借机建立新 metadata、总节点注册表、通用 DTO 或新适配层。公共定义仍反向依赖具体图实现时，MUST 不认定完成。

#### Scenario: 分离端口定义与显示描述

- **WHEN** 一个文件同时定义值端口规则和菜单颜色
- **THEN** MUST 按职责拆出类型，人工编辑、C# authoring 和编译读取继续消费现有正式规则
- **AND** MUST 不改变端口身份、合法连接、默认值或运算行为

#### Scenario: 现有消费者不能直接迁移

- **WHEN** 消除一项旧类型依赖需要改变节点表达或运行求值
- **THEN** MUST 报告具体依赖、保留与改变的业务取舍，交用户决定
- **AND** MUST 不复制定义或增加临时接口绕过该依赖

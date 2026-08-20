# graph-authoring-domain-framework Specification

## Purpose

定义BTSMTL Gameplay Graph、AI Graph与Character Presentation Pose Graph共享的唯一作者交互框架，同时保持各领域数据、Mutation、Validator、Compiler与Runtime语义隔离。
## Requirements
### Requirement: Graph Authoring必须拥有唯一领域框架

系统 MUST在BTSMTL作者层提供唯一Graph Authoring Domain Framework，统一承载Graph document、capability catalog、canvas、node view、port view、selection、clipboard、search、Details host、Navigator host、Mutation和diagnostics契约。BTSMTL Gameplay Graph与Character Presentation Pose Graph MUST分别适配该框架，但 MUST不共享正式序列化Graph基类、runtime node或compiler operation。

#### Scenario: 打开不同领域Graph

- **WHEN** 作者分别打开BTSMTL Graph与Pose Graph
- **THEN** 两者 MUST复用同一套canvas、node、port与selection交互
- **AND** 每个document MUST只加载本领域的asset adapter、capability与mutation

#### Scenario: 跨领域粘贴节点

- **WHEN** clipboard的domain identity与当前document不一致
- **THEN** 框架 MUST在mutation前拒绝粘贴
- **AND** MUST不猜测或转换另一领域的payload

### Requirement: 唯一领域框架必须从现有BTSMTL作者UI原地抽象

共享Canvas、Node View、Port View、Edge View、Details、Navigator、Data Catalog与StateMachine表面 MUST以现有BTSMTL作者UI实现作为提取基线。系统 MUST通过抽取domain-neutral交互并注入document、capability、mutation与presenter边界完成共享化；MUST不新建功能更少的替代GraphView再切换BTSMTL入口。BTSMTL现有布局、节点信息、黑板变量拖拽、Flow/Property Port、节点搜索与创建、selection、框选、clipboard、Undo、Inspector、子树/StateMachine下钻和Live Debug行为 MUST保持。

#### Scenario: 拖出黑板变量

- **WHEN** 作者从现有Data Catalog把黑板变量拖到BTSMTL画布
- **THEN** 共享实现 MUST保留原拖拽手势、变量节点表现、Property Port和正式BTSMTL mutation语义
- **AND** MUST不把该操作降级成功能不完整的通用节点创建

### Requirement: Authoring Capability Catalog必须是UI与Document的唯一语义目录

唯一Framework MUST通过`GraphAuthoringCapabilityCatalog`查询每个domain的Graph kind、node kind、typed payload、静态/动态logical port、数据类型、Pose空间、非Pose瞬时value空间、execution domain、允许连接、资源引用、创建菜单、显示标题、Details provider、Mutation入口与Compiler handler。人工UI、Document exporter、strict parser、Reconciler、Validator和Compiler MUST读取同一Capability；固定port MUST不在实例数据中复制。Capability未声明的字段、port、Pose空间转换、瞬时value lineage或execution domain MUST不被任何入口创建或保存，系统 MUST不按C#类型名、显示名、窗口类型或字段路径重复硬编码能力。

#### Scenario: FootPlacement声明双输出

- **WHEN** FootPlacement Capability声明`pose.component`与`component.biped-leg-targets`两个输出
- **THEN** Canvas、Document、Validator与Compiler MUST从同一Capability识别两个稳定port及其lineage规则
- **AND** MUST不把targets伪装成Pose、动态字符串port或隐藏Compiler字段

#### Scenario: targets连接错误节点

- **WHEN** 作者或Document把`component.biped-leg-targets`连接到未声明该输入类型的节点
- **THEN** Mutation MUST在写资产前拒绝
- **AND** Compiler MUST继续执行同一规则作为完整性校验

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者注册一个新的Component Pose骨骼控制节点
- **THEN** 同一Capability MUST声明其Component Pose端口、execution domain、typed payload与compiler handler
- **AND** 人工创建菜单、Document、Validator和Compiler MUST同时识别该能力

#### Scenario: capability未声明字段

- **WHEN** UI或Document尝试写入当前node capability未声明的字段
- **THEN** Mutation MUST拒绝该命令并返回稳定诊断
- **AND** MUST不通过SerializedProperty path绕过目录

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或Document创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler MUST继续执行同一规则作为完整性校验

### Requirement: Graph Canvas必须复用统一节点与端口投影

Graph Canvas MUST通过document projection和Capability生成通用Node View、Port View、Edge View、创建菜单、搜索结果与clipboard payload。领域adapter MAY提供业务标题、图标、颜色、状态badge与特殊交互命令，但 MUST不重新实现selection、拖线、框选、复制粘贴、Undo或GraphView生命周期。固定端口 MUST来自Capability；动态端口 MUST由node-local稳定identity声明并接受同一port policy裁决。Pose端口 MUST从stable type投影Local/Component空间颜色和标签；非Pose瞬时control value MUST使用独立稳定类型、标签与颜色。转换节点 MUST作为普通serialized authoring节点显示。Canvas MUST不根据C#类型名、显示名或Compiler operation猜测空间，也 MUST不隐藏插入未序列化节点。

#### Scenario: 作者连接FootPlacement与LegIK

- **WHEN** 作者从FootPlacement拖出Component Pose和Biped Leg Targets到LegIK
- **THEN** Canvas MUST显示两条不同类型edge并保留各自稳定port identity
- **AND** 框选、复制粘贴、Undo与Document往返 MUST保持完整双edge拓扑

#### Scenario: 节点拥有动态输入

- **WHEN** GraphInput或GraphOutput增加一个显式Component Pose动态port
- **THEN** Canvas MUST使用节点局部稳定identity投影并保存该port
- **AND** clipboard与Document往返 MUST保留其Pose空间

#### Scenario: 作者查看空间转换

- **WHEN** Pose Graph包含LocalToComponentPose
- **THEN** Canvas MUST显示Local输入与Component输出
- **AND** Diagnostics MAY显示compiled stage但作者数据 MUST不保存stage index

### Requirement: Details必须只显示当前作者需要的业务字段

Details MUST只投影当前selection、当前capability与当前authoring mode允许查看或修改的字段和命令。Unity资源关系 MUST使用Capability声明的精确对象类型和对象选择器；领域identity关系 MUST使用精确上下文提供的可读选项目录。IdentityReference缺少选项目录时 MUST显示Unavailable并禁止编辑，不得退化为TextField；选项标签 MUST不拼接内部value。稳定identity、revision、GUID、local file id、compiled index、runtime handle、generated path、内部枚举载荷、缓存、Projection中间值与不适用nullable字段 MUST默认隐藏；只读References与Diagnostics MUST放入明确折叠区，且 MUST不伪装成可编辑属性。

#### Scenario: 选择Clip Player

- **WHEN** 作者在Authoring模式选择Clip Player
- **THEN** Details MUST显示类型受限的Source Slot对象选择、loop、play rate、sync与该节点真实可写策略
- **AND** References MUST显示解析后的动画资源与唯一owner
- **AND** MUST不显示Source Id、TwoBoneIK、Slot、compiled offset或联合体空字段

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

框架 MUST提供唯一共享Entry、State、Alias、Transition edge、画布平移缩放、节点拖动、selection、增选框选、下钻、breadcrumb与edge Details表面。共享框选与selection实现 MUST面向通用GraphView能力，不得要求`BaseTreeView`具体类型，也不得为Pose领域创建第二个Manipulator。BTSMTL Gameplay StateMachine与PoseStateMachine MUST分别提供状态payload、transition payload、rule surface、layout owner、validator与compiler adapter；Gameplay condition MUST不进入Pose transition，Pose blend与sync MUST不进入Gameplay transition。Entry、State与Alias位置变化 MUST通过共享`MoveElement`请求进入当前领域Mutation；只读状态 MUST禁止Mutation，但不得替换成另一套View。

#### Scenario: 打开Gameplay StateMachine

- **WHEN** 当前document role为BTSMTL StateMachine
- **THEN** 共享表面 MUST显示Condition Rule、priority与interruption，并保留现有节点拖动和增选框选行为
- **AND** MUST不显示blend duration、sync或inertialization

#### Scenario: 打开PoseStateMachine

- **WHEN** 当前document role为Pose StateMachine
- **THEN** 共享表面 MUST显示Pose State、Transition Rule、blend、sync与source readiness，并允许拖动Entry、State、Alias及增选框选
- **AND** MUST不创建BaseGraph、ConditionRuleGraph、Pose专用GraphView或第二框选器

#### Scenario: 在Pose StateMachine框选多个状态

- **WHEN** 作者从空白画布拖出选择矩形并覆盖多个Selectable State
- **THEN** 唯一共享框选器 MUST把这些State加入当前GraphView selection
- **AND** MUST不因当前画布不是`BaseTreeView`而忽略操作

#### Scenario: Live Debug期间拖动状态

- **WHEN** Pose StateMachine处于Live Debug只读模式且作者尝试拖动State
- **THEN** 共享表面 MUST拒绝位置Mutation并保持正式layout不变
- **AND** MUST不通过window-local缓存记录一个不可提交的位置

### Requirement: 人工编辑与Document Apply必须复用同一类型化Mutation

窗口交互与Agent Authoring Document Reconciler MUST分别把用户操作或目标状态差异降低为同一领域类型化Mutation，再由同一Validator、transaction、dirty owner和Undo边界应用。系统 MUST不允许Document直接写Unity YAML、SerializedObject path、AnimationClip序列化文本或构造第二套Pose/Clip资产写服务。

#### Scenario: UI与Document修改同一Transition

- **WHEN** 人工UI或Document v4修改Pose transition blend policy
- **THEN** 两条入口 MUST生成同一种Presentation Mutation
- **AND** 最终资产约束、诊断和revision变化 MUST一致

#### Scenario: Animation Window入口与Document修改同一Clip Curve

- **WHEN** 两条入口替换同一注册Curve
- **THEN** 两条入口 MUST调用同一Clip Curve validator与Mutation语义
- **AND** MUST进入各自单一Undo事务并产生相同canonical结果

### Requirement: Authoring节点与Runtime执行描述必须分离

Graph Authoring Domain Framework MUST只理解稳定作者identity、typed payload、port与mutation，不得要求authoring node继承runtime node。领域compiler MUST把authoring graph编译为领域自己的中间表示和runtime program；Runtime性能枚举、线性index与switch MAY存在于compiled层，但 MUST不反向成为创建菜单、Details或Document schema。

#### Scenario: Runtime增加优化字段

- **WHEN** Pose Runtime为执行计划增加内部offset或buffer index
- **THEN** Authoring capability、Details与Document MUST不自动暴露该字段
- **AND** Compiler MUST负责从Pose IR生成该内部值

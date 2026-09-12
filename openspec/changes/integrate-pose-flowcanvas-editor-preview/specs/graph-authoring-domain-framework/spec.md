## MODIFIED Requirements

### Requirement: Graph Authoring必须拥有唯一领域框架

各领域 MUST复用正式作者对象及Capability、Port Shape、接口、selection、clipboard、Details、Navigator、Mutation和diagnostics合同。Pose作者的AnimGraph、Layer、State、Rule和Rig使用同一原生图编辑基础，并分别拥有业务语义；不能为新的Control Rig表面复制第二套编辑器或节点目录。技能与其它领域保留其正式入口，不因Pose组织重构被隐式迁移。

#### Scenario: 切换图角色
- **WHEN** 作者从AnimGraph进入状态机或Rig图
- **THEN** 编辑器 MUST装配对应角色目录、连接语义和字段，保持同一交互与选择基础
- **AND** MUST不加载另一领域的payload或Mutation

#### Scenario: 跨领域粘贴
- **WHEN** 剪贴板的领域或图角色与目标不相容
- **THEN** 系统 MUST在提交前拒绝，不猜测转换另一领域数据

#### Scenario: 打开不同领域Graph

- **WHEN** 作者分别打开BTSMTL Graph与Pose Graph
- **THEN** 两者 MUST复用同一套canvas、node、port与selection交互
- **AND** 每个作者上下文 MUST只加载本领域的asset adapter、capability与mutation

#### Scenario: 跨领域粘贴节点

- **WHEN** clipboard的domain identity与当前作者上下文不一致
- **THEN** 框架 MUST在mutation前拒绝粘贴
- **AND** MUST不猜测或转换另一领域的payload

### Requirement: 唯一领域框架必须从现有BTSMTL作者UI原地抽象

尚未迁移领域 MUST保持原有作者交互和行为；Pose MUST复用已建立的领域合同并采用原生图编辑基础，不受必须使用旧GraphView画布的限制。新增状态／Rig外观和连接策略应作为领域适配，不复制框架或重建一份作者拓扑。

#### Scenario: 其它领域编辑
- **WHEN** 作者在技能图编辑黑板或Timeline
- **THEN** 原正式能力 MUST保持，不被Pose图角色、状态连线或Rig配置替换

#### Scenario: 拖出黑板变量

- **WHEN** 作者从现有Data Catalog把黑板变量拖到BTSMTL画布
- **THEN** 共享实现 MUST保留原拖拽手势、变量节点表现、Property Port和正式BTSMTL mutation语义
- **AND** MUST不把该操作降级成功能不完整的通用节点创建

### Requirement: Details必须只显示当前作者需要的业务字段

Details MUST按当前图角色和选中对象显示可编辑资源、数值、Mask、Slot、过渡或Rig设置，以及必要导航命令。骨骼范围、层Alpha、Blend Profile和Effector权重必须分开表达；默认不显示空Runtime栏、hash、GUID、compiled index或内部组装字段。失败 MUST保留错误原因并恢复正式值。

#### Scenario: 选中转换边
- **WHEN** 作者选中状态转换
- **THEN** Details MUST提供混合设置和条件入口，不显示普通Pose数据边的属性

#### Scenario: 选中Slot
- **WHEN** 作者选中层内Slot
- **THEN** Details MUST显示Slot引用和源更新设置，不虚构私有Mask或动作播放器

#### Scenario: 选择Clip Player
- **WHEN** 作者选择Sequence Player
- **THEN** Details MUST显示实际Clip或typed资源参数、Loop、速率和可写策略
- **AND** 不显示Source Slot两次选择、offset或空联合字段

#### Scenario: identity选项目录不可用

- **WHEN** 当前页面缺少解析某个IdentityReference所需的精确Definition或owner上下文
- **THEN** Details MUST显示该引用不可用及缺失上下文原因
- **AND** MUST不允许作者输入任意字符串绕过目录

### Requirement: Navigator与Data Catalog必须复用统一信息架构

目录 MUST按作者职责列出根图、层、状态机、控制图、实际动画资源和必要Rig资产；所有打开入口必须产生完整调用上下文。相同Graph或Layer多个调用不能按名称或排列默选。窗口视图状态和调用导航不得保存另一份业务图或选择集合。

#### Scenario: 多次引用同一层
- **WHEN** 目录需要定位一个具有多个调用位置的Layer
- **THEN** 作者 MUST能明确选择调用或以独立资产上下文打开，两者不能混淆运行来源

#### Scenario: Pose Navigator显示Producer
- **WHEN** 作者从精确Definition打开动画工作区
- **THEN** 目录 MUST显示实际图、Layer、Rig、资源和Action Timeline来源
- **AND** 不复制Timeline正文，不用内部identity作为标签

### Requirement: StateMachine作者表面必须复用且语义隔离

共享StateMachine表面 MUST区分Gameplay与Pose语义；Pose表面表达Entry、State、Alias和Transition，状态转换使用专门的边命中、箭头及详情，不伪装成普通Value Port。状态、Alias和转换编辑必须提交对应领域Mutation，状态子图及Rule通过同一导航进入。

#### Scenario: 编辑Alias成员
- **WHEN** 作者修改Pose Alias成员
- **THEN** 系统 MUST通过Pose状态机合同处理合法性与引用，不产生Gameplay状态或新的运行State

#### Scenario: 打开Gameplay StateMachine

- **WHEN** 当前作者图role为BTSMTL StateMachine
- **THEN** 共享表面 MUST显示Condition Rule、priority与interruption，并保留现有节点拖动和增选框选行为
- **AND** MUST不显示blend duration、sync或inertialization

#### Scenario: 打开PoseStateMachine
- **WHEN** 当前图为Pose StateMachine
- **THEN** 同一作者表面 MUST提供State、Alias、转换、readiness、混合详情与拖动框选
- **AND** 不创建第二画布或使用Gameplay条件

#### Scenario: 在Pose StateMachine框选多个状态
- **WHEN** 作者框选多个状态
- **THEN** 唯一选择机制 MUST选中对应状态，不依赖旧BaseTreeView类型

#### Scenario: Live Debug期间拖动状态

- **WHEN** Pose StateMachine处于Live Debug只读模式且作者尝试拖动State
- **THEN** 共享表面 MUST拒绝位置Mutation并保持正式layout不变
- **AND** MUST不通过window-local缓存记录一个不可提交的位置

### Requirement: Authoring节点与Runtime执行描述必须分离

Pose作者节点 MUST表达动画或控制意图，Pose Compiler可以为一个作者节点生成多个内部operation及接口边界转换。每个生成步骤必须可追溯到稳定作者owner和call-site；Pose Runtime只消费正式产物，不执行Pose作者getter或读取窗口图对象。事件图按独立宿主合同原生执行并生产只读变量Frame，MUST不由本要求强制增加事件编译器。帧页地址、offset、Goal打包和调度步骤不能成为作者必接节点。

#### Scenario: Slot编译展开
- **WHEN** Slot被展开为动作读取、混合及Curve处理
- **THEN** 三者 MUST共用Slot作者来源并进入同一计划，不在作者图写回三个系统节点

#### Scenario: Runtime增加优化字段

- **WHEN** Pose Runtime为执行计划增加内部offset或buffer index
- **THEN** Authoring capability、Details与C#作者API MUST不自动暴露该字段
- **AND** Compiler MUST负责从Pose IR生成该内部值

### Requirement: Graph Canvas必须复用统一节点与端口投影

作者画布 MUST从唯一正式作者对象、Capability和Port Shape生成节点、端口、菜单与可编辑连接；领域适配可提供图角色、标题、颜色、状态标记和特殊命令，不重建选择、框选、Undo或另一画布。固定、条件与动态端口必须保持稳定identity及空间／目标类型，不能从显示名、现有连线或operation位置猜测。

显式空间转换在作者图中正常显示；由Slot／Control Rig等接口合同展开的内部转换、参数读取及Goal组装只属于编译计划与按需诊断，不向作者图插入不可见的可编辑节点。UI不能同时保留内部组装图作为第二正式拓扑。

#### Scenario: 作者连接Goal Contribution与Assembler
- **WHEN** 旧数据或创建请求仍要求在作者画布连接Goal Contribution与Assembler
- **THEN** 新目录 MUST要求转换为Rig目标与FBIK接口，内部组装只由编译展开
- **AND** 来源与typed依赖继续存在于运行计划，不能退回旧作者节点

#### Scenario: 节点拥有动态输入
- **WHEN** Layer接口或图输入增加明确的typed Pose／目标端口
- **THEN** 画布 MUST按该owner的稳定端口identity更新，并保持正式对象与剪贴板的相同空间合同

#### Scenario: 作者查看空间转换
- **WHEN** 作者显式放置Local To Component节点
- **THEN** 画布 MUST显示对应输入输出空间，作者数据不能保存compiled Stage index

## ADDED Requirements

### Requirement: Pose作者能力必须由正式API共享字段与端口

每个Pose作者能力 MUST在唯一目录声明领域、图角色、typed字段、固定／条件／动态端口、接口依赖和可用作者操作。直接资源Player、层调用、Slot、按骨骼混合、状态／Alias／Rule和Rig目标 MUST使用该目录；UI、C#作者API、Clipboard、正式Mutation和Compiler不得分别声明同一语义。

合法创建与连接必须由完整Port Shape和角色合同确定，不能从默认构造节点猜端口。内部Action输入、Goal组装和参数解析operation MUST不出现在可创建作者目录；缺少正式投影或lowering的能力不能降级成自由字段节点。

固定端口、条件portVariants与node-local动态端口 MUST由唯一Projector从同一字段合同合成，条件必须唯一命中，三类端口identity不能重叠。节点Definition只拥有局部作者语义、直接依赖与展开，不得接管package路径、diff、Undo、保存、回滚、反向导出或MCP生命周期。目录变化必须同步正式Pose读取/配置及消费者，不允许UI与C#输出适配各自解释mode或使用自由SerializedProperty路径；不新增Agent模型、中央Validator或整包同步事务。

#### Scenario: Layer接口变化
- **WHEN** 作者修改层的Pose或资源接口
- **THEN** 同一合同 MUST更新所有调用端口和正式对象引用，缺少适配的调用明确失败
- **AND** MUST不依靠旧端口别名继续连接

#### Scenario: FootPlacement声明Goal Contribution输出
- **WHEN** Foot Placement声明Rig目标并由lowering生成内部Goal Contribution
- **THEN** Capability MUST声明作者目标与空间，Compiler保留内部Contribution的同一来源
- **AND** 不把内部组装暴露为作者必接节点

#### Scenario: Goal Contribution连接错误节点
- **WHEN** 旧作者数据试图把内部Goal Contribution接入普通Pose输入
- **THEN** Mutation MUST拒绝，内部typed依赖继续由唯一Topology处理

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者注册一个新的Component Pose骨骼控制节点
- **THEN** 唯一Pose Definition MUST声明其Component Pose端口、execution domain、typed payload、Operation Family、Graph dependency与typed lowering
- **AND** Capability、人工创建菜单、C#作者API、领域Validator和Compiler MUST同时识别该能力而不得注册第二Compiler Handler

#### Scenario: capability未声明字段

- **WHEN** UI或C#作者API尝试写入当前node Definition未声明的字段
- **THEN** Mutation MUST拒绝该命令并返回稳定诊断
- **AND** MUST不通过SerializedProperty path、自由文本或独立协议特例绕过目录

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或C#代码创建空间不兼容的Pose edge
- **THEN** 共享connection policy MUST在Mutation前拒绝
- **AND** Compiler Topology Pass MUST继续执行同一规则作为完整性校验

#### Scenario: Definition尝试接管作者保存

- **WHEN** Pose Definition Adapter尝试直接接管Undo、保存或公共代码生成生命周期
- **THEN** Framework MUST拒绝该依赖，继续由正式领域修改/保存入口拥有这些职责
- **AND** MUST不建立Pose专用Document入口、中央Validator或整包同步事务

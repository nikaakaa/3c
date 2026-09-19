## ADDED Requirements

### Requirement: 业务作者合同必须独立于画布与编辑展示

共享作者合同 MUST 表达字段、类型、引用、逻辑端口、能力与正式变更边界，MUST 不依赖旧图模型、旧节点执行器、画布对象或 Editor API。领域 MUST 唯一拥有节点局部业务规则；菜单、颜色、标题和字段呈现 MUST 由编辑描述消费规则，不能反向决定执行域或连接合法性。共享层 MUST 不接管各领域的节点实现、运行、保存或事务。

#### Scenario: 领域修改端口规则

- **WHEN** 领域定义修改某节点允许连接的类型
- **THEN** 原生适配、人工编辑、C# 创建及编译/准备 MUST 消费同一结果
- **AND** 修改菜单或窗口布局 MUST 不改变该规则

#### Scenario: 消费独立黑板声明

- **WHEN** 领域编译准备读取局部变量作用域和事实投射
- **THEN** MUST 可以直接读取独立自有合同
- **AND** MUST 不需要加载旧图执行器或编辑程序集

## MODIFIED Requirements

### Requirement: Graph Authoring必须拥有唯一领域框架

系统 MUST 提供唯一共享作者合同与编辑集成，覆盖领域身份、只读文档投影、能力查询、选择、剪贴板、搜索、Details、Navigator、类型化变更和诊断。正式原生图编辑器 MUST 拥有画布及框架交互，各领域 MUST 提供自己的资产、业务定义与变更适配。共享 MUST 不要求技能与 Pose 使用同一业务图基类、运行节点或执行内容，也 MUST 不复制原生画布实现。

#### Scenario: 打开不同领域Graph

- **WHEN** 作者分别打开技能与 Pose 图
- **THEN** MUST 使用原生画布及同一共享作者集成合同
- **AND** 每个页面 MUST 只装配本领域 owner、能力与正式变更入口

#### Scenario: 跨领域粘贴节点

- **WHEN** 剪贴板领域身份与目标领域不一致
- **THEN** MUST 在修改前拒绝
- **AND** MUST 不猜测或自动转换另一领域 payload

### Requirement: Authoring Capability Catalog必须是UI与C#作者的唯一语义目录

共享能力目录 MUST 从领域正式定义投影 Graph Role、Node Kind、typed 字段、固定/条件/动态端口、值类型、Pose 空间、非 Pose 瞬时值空间与 lineage、执行域、资源引用、依赖和连接约束。编辑菜单、显示标题和 Details provider MUST 通过独立编辑描述关联同一稳定能力，不成为业务定义来源。Skill 编译与 Pose 资源准备/原生运行绑定 MUST 读取各自同一领域定义，MUST 不保存重复 handler、能力矩阵或节点局部规则。

端口形状 MUST 只由正式定义、当前 typed 参数和节点局部动态声明组成；固定、唯一命中的条件形状和动态端口身份不得重叠。人工 UI、C# authoring、剪贴板、变更预检查与领域校验 MUST 消费同一形状，MUST 不构造默认节点、读取运行 getter 或从已有连接反推端口。未声明的字段、空间转换、lineage 和执行域 MUST 被拒绝，不能按类型名、显示名或字段路径猜测。

每个领域 MUST 保持自己的定义所有权，不强迫其它领域引用 Pose 运行类型。Pose Definition 只拥有节点语义、直接依赖与正式运行/准备绑定，不接管 Undo、rollback 或保存。Behavior Designer AI 继续使用插件作者框架，不注册为本项目技能图领域。能力变化 MUST 直接反映到现有消费者，不引入旧协议、导出器、对账器或专属中央校验器。

#### Scenario: FootPlacement声明Goal Contribution输出

- **WHEN** 正式定义声明 Component Pose 与 FullBodyIK Goal Contribution 输出
- **THEN** 原生端口、C# 创建、变更、校验和领域准备 MUST 识别两个稳定类型及其 lineage
- **AND** MUST 不把贡献伪装成 Pose、自由字符串端口或隐藏运行字段

#### Scenario: Goal Contribution连接错误节点

- **WHEN** 作者或代码将 Goal Contribution 接到不支持的输入，或将 Local Pose 直接接到 Component Pose
- **THEN** 正式变更 MUST 在写资产前拒绝
- **AND** 领域准备 MUST 使用同一规则检查完整内容

#### Scenario: 新增Pose节点能力

- **WHEN** 开发者提供一个具有新字段、端口和执行域的正式节点定义
- **THEN** 菜单、Details、C# authoring、校验和对应编译/运行准备 MUST 消费同一业务定义
- **AND** MUST 不注册第二个语义 handler 或另一份字段表

#### Scenario: capability未声明字段

- **WHEN** UI 或 C# 尝试配置定义中不存在的字段
- **THEN** 正式变更 MUST 返回明确诊断
- **AND** MUST 不通过私有字段路径或入口特例绕过

#### Scenario: Definition尝试接管编辑事务

- **WHEN** 节点定义试图直接保存资产或创建另一套 Undo 事务
- **THEN** 作者框架 MUST 拒绝该依赖
- **AND** 保存与事务 MUST 仍归正式领域修改入口

#### Scenario: Local Pose连接Component Pose

- **WHEN** 作者或 C# 将 Local Pose 直接连接到只接受 Component Pose 的输入
- **THEN** 正式变更 MUST 在写入前拒绝，作者校验与运行实例绑定 MUST 使用同一空间规则
- **AND** 必需空间转换 MUST 通过真实保存的转换节点表达，不生成旧 Pose IR 或隐藏转换

### Requirement: Graph Canvas必须复用统一节点与端口投影

原生画布的节点、端口、连接、搜索与剪贴板 MUST 使用正式领域定义及编辑描述。逻辑端口的稳定身份、类型、方向、容量和业务顺序 MUST 来自领域，框架端口对象只作适配。颜色、标签、位置或视图重建 MUST 不改变端口身份；Pose 空间与非 Pose 瞬时值 MUST 有准确类型呈现。空间转换 MUST 由真实保存的转换节点表达，不能隐式插入未保存节点。

#### Scenario: 节点拥有动态输入

- **WHEN** 作者新增动态 Component Pose 端口并保存、复制或重新打开
- **THEN** 原生端口 MUST 仍对应同一逻辑身份、类型、顺序与连接
- **AND** MUST 不按画布位置或显示标题重建业务身份

#### Scenario: 作者连接Goal Contribution与Assembler

- **WHEN** 作者把 FootPlacement 贡献接到合法 Assembler
- **THEN** 画布 MUST 显示准确 typed 连接
- **AND** Undo、粘贴与重载 MUST 保留完整拓扑

#### Scenario: 作者查看空间转换

- **WHEN** 图包含 LocalToComponentPose
- **THEN** 画布 MUST 显示真实 Local 输入和 Component 输出
- **AND** 运行准备内部索引 MUST 不写入作者参数

### Requirement: StateMachine作者表面必须复用且语义隔离

状态机 MUST 复用原生画布的平移、缩放、拖动、选择、框选与连接能力，项目共享 Entry/State/Alias/Transition 的导航和 Details 集成。技能 FSM 与 Pose 状态机 MUST 由各自真实数据 owner 提供 payload、规则、布局与校验；临时编辑投影 MUST 不保存第二份状态机或参与运行。Gameplay 条件、priority、interruption 与 Pose blend、sync、readiness MUST 分属各自领域。位置修改 MUST 经正式 owner 的变更与 Undo，Live Debug MUST 拒绝全部作者修改。

#### Scenario: 打开Gameplay StateMachine

- **WHEN** 当前页面是技能 FSM
- **THEN** MUST 呈现条件规则、priority 与 interruption，并支持原生拖动和框选
- **AND** MUST 不出现 Pose blend、sync 或 inertialization 配置

#### Scenario: 打开PoseStateMachine

- **WHEN** 当前页面是 Pose 状态机
- **THEN** MUST 呈现状态、转移规则、blend、sync 与 readiness，并允许选择和移动 Entry/State/Alias
- **AND** 修改 MUST 落到原 Pose owner，不能保存临时画布数据作为第二真相

#### Scenario: Live Debug期间拖动状态

- **WHEN** 作者在 Live Debug 模式尝试拖动状态
- **THEN** MUST 拒绝变更并保持正式布局
- **AND** MUST 不在窗口缓存中保存无法提交的替代位置

#### Scenario: 在Pose StateMachine框选多个状态

- **WHEN** 作者在原生状态机画布框选多个 State 并移动
- **THEN** MUST 使用原生选择与拖动交互，把位置变更提交到真实 Pose owner 并纳入同一 Undo
- **AND** MUST 不新建项目专用框选器、旧 GraphView 或第二份持久化状态机

### Requirement: Authoring节点与Runtime执行描述必须分离

共享作者合同 MUST 只表达稳定作者身份、typed 数据、逻辑端口和正式变更，不把运行内部索引、缓存或性能枚举暴露为作者参数。Skill 与技能 FSM MUST 继续编译为领域执行内容，禁止启动技能作者图原生执行。Pose MUST 继续由领域准备资源并在角色独立的原生实例中求值。已明确采用原生执行的 EventGraph MUST 保留自己的宿主、变量和生命周期合同，不建立项目专用事件 IR 或备用执行器。所有可变运行状态 MUST 与作者资产隔离。

#### Scenario: Runtime增加优化字段

- **WHEN** Skill 执行内容或 Pose 运行实例新增 buffer index、缓存或内部 offset
- **THEN** 作者字段、Details 和 C# 创建接口 MUST 不自动暴露它们
- **AND** 对应领域编译或资源准备 MUST 负责建立内部绑定

#### Scenario: 运行技能与 Pose

- **WHEN** 宿主加载技能和 Pose 资源
- **THEN** 技能 MUST 走既有编译执行链，Pose MUST 走其角色独立原生实例
- **AND** MUST 不以统一作者合同为理由把 Pose 改回 IR 或允许技能作者节点直接执行

#### Scenario: 原生事件图调用变量节点

- **WHEN** 正式事件图调用宿主允许的 Get/Set
- **THEN** MUST 执行原生节点并遵守该领域唯一变量合同
- **AND** MUST 不影响技能和 Pose 的既有执行边界

#### Scenario: Pose作者图被尝试直接启动

- **WHEN** 调用方试图直接执行可编辑 Pose 资产并在其中保存角色状态
- **THEN** 系统 MUST 拒绝这种资产执行，转由既有 Pose 宿主创建角色独立原生实例
- **AND** 正式实例 MUST 执行原生节点和连接，不恢复旧编译程序，也不把此限制解释为禁止原生 Pose 运行

### Requirement: Formal authoring metadata必须是C#与UI共享的唯一语义来源

Formal authoring type、字段、引用方法和正式Mutation写入方法上的metadata MUST是作者可见kind、typed field、logical port、owner和连接规则的唯一语义来源。共享Capability、正式C#作者API、领域Validator、Compiler和原生UI MUST从该metadata投影同一语义；MUST不维护旧协议专用节点模型、字段表、端口表、Pose模型或owner规则副本。

metadata的Editor查找实现 MAY生成不可编辑的静态索引，但该索引 MUST不成为第二作者真相，也不得要求运行时反射、Unity序列化字段或Compiler operation作为作者合同。

对于BTSMTL Skill领域，formal metadata MUST同时提供当前节点的typed字段读取、真实引用目标读取、稳定identity投影和正式创建/配置入口描述。Skill正式节点适配、FlowCanvas目录、人工Inspector、步骤编辑器和Skill compiler MUST消费这些入口；字段的本地序列化形态与编译所需identity可以不同，但两者 MUST由同一业务定义明确映射，MUST不各自读取或维护第二份字段规则。

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

共享字段描述 MUST由所属领域的正式能力提供，包含稳定字段identity、typed值类型、默认值/可写性、必要约束，以及读取当前正式对象和调用正式配置API所需的绑定信息。人工UI、现有正式节点适配、FlowCanvas和C#领域输出适配 MUST消费同一信息，不维护第二份节点/字段/owner模型。绑定只表达正式方法与参数对应，不保存源码模板、语法树或操作历史。

#### Scenario: 输出器请求一个正式字段

- **WHEN** C#领域输出适配按字段identity读取当前正式对象
- **THEN** 共享合同 MUST给出当前typed值及所属领域的正式配置入口
- **AND** 该配置入口 MUST与人工编辑使用同一业务规则

#### Scenario: 某字段缺少读取或配置能力

- **WHEN** 字段没有完整的正式读取或配置绑定
- **THEN** 共享合同 MUST明确指出字段和缺失能力
- **AND** 调用者 MUST不回退到私有字段反射、Agent JSON或另建字段表

## REMOVED Requirements

### Requirement: 唯一领域框架必须从现有BTSMTL作者UI原地抽象

**Reason**: 画布已经由原生编辑器承担，继续要求从旧 GraphView 提取会恢复已替代 UI。

**Migration**: 保留本规范的端口、拖拽、领域修改及编辑集成能力，将有效合同迁出旧目录，删除被原生框架替代的渲染和交互实现。

### Requirement: Formal authoring metadata必须是Agent与UI共享的唯一语义来源

**Reason**: 该条款仍要求已退役的 Agent Document、Exporter 和 Reconciler，与现行正式 C# 作者路径冲突。

**Migration**: 由“Formal authoring metadata必须是C#与UI共享的唯一语义来源”承接共享规则，不恢复协议模型或兼容读取器。

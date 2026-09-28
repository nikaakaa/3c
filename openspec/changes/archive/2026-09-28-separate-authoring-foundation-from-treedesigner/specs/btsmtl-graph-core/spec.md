## ADDED Requirements

### Requirement: 正式图资产必须通过所属领域原生入口打开

Skill、技能 FSM 与 Pose 图 MUST 通过各自正式原生资产入口打开，Timeline MUST 保持正式 Slate 入口。Project、Inspector、业务导航和运行来源定位 MUST 指向同一真实 owner，不创建旧 BaseTreeWindow、Tree Browser 或第二 GraphView。仍有数据/运行职责的 BaseTreeAsset/BaseGraph 类型 MUST 保持唯一实现；它们的存在 MUST 不自动恢复已退役的旧编辑器。删除旧入口前 MUST 确认没有合法内容失去编辑去向。

#### Scenario: 直接打开正式图资产

- **WHEN** 作者从 Project 或业务定义打开当前正式 Skill/Pose 资产
- **THEN** 领域 MUST 使用其原生图与真实 owner 打开唯一正式入口
- **AND** MUST 不要求旧 TreeDesigner 窗口或旧画布副本

### Requirement: 图代码归位必须以真实职责和消费者为界

图代码整理 MUST 保留有效数据、端口和求值语义，仅移动或拆分混合文件并调整直接引用。BaseGraph、BaseNode、PropertyPort、BaseExposedProperty MUST 不因旧 UI 退出而被预定删除。删除 MUST 有代码、注册、生成内容和资产引用证据，确认不再拥有有效消费者与独有语义；仍在使用的实现保留或归位不属于兼容路径。

#### Scenario: 旧画布文件包含有效合同

- **WHEN** 当前原生编辑入口仍使用旧画布文件中的绑定类型
- **THEN** MUST 先原样迁出合同并更新直接引用，再删除旧画布
- **AND** MUST 不为分文件增加新的图结构、同步器或执行器

#### Scenario: 旧类型仍承载求值语义

- **WHEN** PropertyPort 或相关类型仍提供未被正式链完整承接的类型、值来源或求值行为
- **THEN** MUST 保留该有效实现，记录消费者和语义去向
- **AND** 若删除必须改变业务合同，MUST 报告具体缺口交用户决定，不按“旧类型”推断可以删除

#### Scenario: 附属代码确实无人使用

- **WHEN** 引用清单确认某旧 UI 附属实现无代码、注册或合法资产消费者
- **THEN** MUST 删除该实现及孤立注册引用
- **AND** MUST 不保留兼容窗口、空壳或第二写入口

#### Scenario: 旧窗口仍有打开入口

- **WHEN** Inspector、资产打开回调、调试定位或菜单仍指向旧窗口
- **THEN** MUST 沿真实调用和合法内容确认业务去向，将有效请求接入所属领域正式入口，删除无消费者的旧回调
- **AND** 合法内容缺少等价去向时 MUST 停止受影响的删除切片并报告对象及缺失能力，不删入口掩盖问题

## MODIFIED Requirements

### Requirement: Graph 引用和页面栈保持 editor-only

节点、引用和 TreeClip 的导航 MUST 通过所属领域的正式作者引用和原生编辑入口定位真实内容。导航历史、窗口绑定、selection restore 和来源 identity MUST 保持 editor-only，不参与运行状态或保存成第二份业务数据。Timeline MUST 由其正式独立编辑入口显示，不能因删除旧 TreeDesigner 页栈改变内容、调用身份或调度协议。系统 MUST 不为沿用旧页面实现创建 TimelineRunningTree 或 BaseGraph 编辑副本。

#### Scenario: 节点下钻到内联 Graph

- **WHEN** 作者打开正式节点拥有的私有子图
- **THEN** 所属领域 MUST 定位原子图 owner，并记录来源节点和引用身份用于导航
- **AND** MUST 不复制节点或图数据

#### Scenario: 节点下钻到 shared Graph

- **WHEN** 作者打开节点显式引用的共享图
- **THEN** 原生入口 MUST 定位该真实共享资产
- **AND** 作者界面 MUST 保留共享关系和来源导航

#### Scenario: TimelineNode 下钻到 inline Timeline

- **WHEN** 作者从正式图节点打开其私有 Timeline 内容
- **THEN** Timeline 正式入口 MUST 使用实际内容 owner 与来源身份，来源图导航保持
- **AND** MUST 不把 Timeline 转成旧 Graph 页或修改运行内容

#### Scenario: TimelineNode 下钻到 shared Timeline

- **WHEN** 作者从正式图节点打开共享 TimelineAsset
- **THEN** Timeline 正式入口 MUST 绑定该资产与节点来源
- **AND** TreeClip 下钻 MUST 继续使用真实作者引用

#### Scenario: Timeline 下钻到 TreeClip

- **WHEN** 作者从 Timeline 打开 TreeClip 图
- **THEN** MUST 通过现有正式原生图入口定位该 Clip 的内容和来源
- **AND** Timeline 窗口保持当前内容，不恢复旧 TreeDesigner 页栈或隐式创建另一运行实例

#### Scenario: 保存双窗口内容

- **WHEN** 作者修改正式图或 Timeline 内容
- **THEN** dirty 与 Undo MUST 作用于对应真实 owner
- **AND** 窗口导航、preview state 和返回位置 MUST 不进入业务资产

### Requirement: Graph Authoring Editor Shell 支持 editor-only authoring context

正式原生编辑入口及共享面板 MUST 使用所属领域明确提供的作者上下文定位 Details、Navigator 和资源选择。该上下文 MUST 不写入节点、端口或图运行数据；子图/规则导航按现有领域规则保留来源关系。系统 MUST 不依赖已删除的 CharacterPipelineAuthoringContext、BaseTreeWindow 或旧 Shell 类提供上下文。

#### Scenario: 从业务定义打开 RootTree

- **WHEN** 作者从业务定义打开当前正式技能或 Pose 图
- **THEN** 所属入口 MUST 使用定义与实际图 owner 提供作者上下文
- **AND** MUST 不因历史场景名称恢复旧 RootTree

#### Scenario: 直接打开孤立 TreeAsset

- **WHEN** 作者直接打开缺少业务上下文的受支持原生图资产
- **THEN** 依赖上下文的面板 MUST 明确显示缺失状态
- **AND** MUST 不猜测角色或写入 fallback 配置

#### Scenario: 下钻 Graph

- **WHEN** 作者从正式图进入私有、共享或规则内容
- **THEN** 原生入口 MUST 按领域已有规则保持实际来源上下文
- **AND** 子图 MUST 不保存另一份窗口状态

### Requirement: 不新增 Graph 分裂路径

仍由 BaseGraph 表达且有有效消费者的 BTSMTL 内容 MUST 继续使用唯一数据、PropertyPort/PropertyEdge 语义与正式 owner，不新增 Workbench、并行端口协议、旧数据 fallback 或重复序列化集合。本次整理 MUST 不把其它已采用原生图的领域改回 BaseGraph，也不因原生图存在就删除仍有独有语义的旧类型。Pose MUST 保持现行原生图、typed 端口、领域校验和唯一运行方式，不继承 BTSMTL runtime node/edge 语义。跨领域 MUST 共享公共作者描述与有效编辑合同，画布与 Node/Port 交互由各自正式原生编辑器提供，不再要求同一旧 GraphView 或窗口类。共享层只消费 document、Capability、typed payload、port policy、mutation 与观察合同，不拥有第二套业务数据或执行器。

#### Scenario: BTSMTL新增规则图能力

- **WHEN** 仍由 ConditionRuleGraph 表达的有效 StateMachine Transition 或 BT edge decorator 需要规则图能力
- **THEN** 它 MUST 继续使用所属领域唯一数据及端口语义，本次文件整理不重写该运行合同
- **AND** MUST 不用 Pose Graph 或通用 Shell payload 代替其数据；旧作者入口没有正式去向时按删除缺口处理

#### Scenario: 打开Presentation Pose Graph

- **WHEN** 作者通过正式编辑入口打开 Pose Graph asset
- **THEN** 原生编辑器及共享编辑集成 MUST 装配 Pose domain document 与 port policy
- **AND** MUST 不为编辑创建 BaseGraph、BaseNode、BaseEdge、Blackboard 或额外 runtime evaluation context

#### Scenario: 复用节点编辑交互

- **WHEN** 不同领域需要搜索、clipboard、Undo 与 Details 宿主
- **THEN** MUST 复用有效编辑合同和所属正式原生交互，不为复用创建旧 GraphView
- **AND** 每个领域 MUST 只修改自己的正式 serialized owner

## REMOVED Requirements

### Requirement: BaseTreeAsset 保持资产和 Graph Authoring Editor Shell 入口

**Reason**: 该条款要求已经被原生入口替代的 TreeDesigner 专用窗口与 Tree Browser。实施盘点中，Unity AssetDatabase 的 BaseTreeAsset 资产数为零，现行 Skill/Pose/Timeline 已使用自己的正式入口。

**Migration**: 保留仍有职责的 BaseTreeAsset/BaseGraph 数据与运行类型；删除无合法资产消费者的旧 Inspector、浏览器、打开回调和创建菜单。当前正式图的打开要求由“正式图资产必须通过所属领域原生入口打开”承接，不重建旧图 UI。

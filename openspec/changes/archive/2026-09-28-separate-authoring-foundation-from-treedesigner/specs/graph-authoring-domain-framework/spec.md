## ADDED Requirements

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

## MODIFIED Requirements

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

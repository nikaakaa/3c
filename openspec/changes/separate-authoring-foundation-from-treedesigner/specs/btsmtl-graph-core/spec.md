## ADDED Requirements

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

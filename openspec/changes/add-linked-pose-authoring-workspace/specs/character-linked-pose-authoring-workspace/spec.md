# character-linked-pose-authoring-workspace Specification

## ADDED Requirements

### Requirement: Linked Pose必须在唯一Animation Workspace中形成完整作者闭环

Definition-scoped `GraphAuthoringEditorShell` MUST成为Linked Pose人工作者的唯一正式工作区，并使用同一Toolbar、Navigator、Graph Canvas、Details、breadcrumb与Bottom Dock承载Interface、Group、selector、Implementation、Entry Graph与root Call。系统 MUST不创建Linked Pose专用Workbench、第二GraphView或依赖多个Unity Asset Inspector完成一次配置。

#### Scenario: 作者打开一个武器Implementation Entry

- **WHEN** 作者在Group下打开Rifle Implementation的required Entry
- **THEN** 同一Graph Canvas MUST导航到该Entry Graph且breadcrumb MUST显示Profile、Group、Implementation与Entry业务名
- **AND** Undo、selection、Details与返回root graph MUST继续使用同一Shell链路

#### Scenario: Profile没有Linked Pose数据

- **WHEN** 作者从Definition上下文打开Linked Pose且当前Profile没有Group
- **THEN** Details MUST显示按Interface、Group/selector、Implementation与root Call依赖顺序组织的可执行空状态
- **AND** MUST不只显示空列表或要求作者改JSON

### Requirement: Linked Pose Navigator必须以Group工作上下文组织真实引用

Navigator MUST按`Group -> Contract | Selection | Implementations | Host Calls`投影真实Profile与资产引用。每个required Entry MUST显示Implementation Entry完整性与root Call coverage的`Placed | Missing | Duplicate`状态。共享Interface MAY在多个Group下显示引用，但 MUST只指向一个正式资产；候选闭包、signature、revision、GUID、hash与runtime handle MUST不作为树项名称或第二份数据保存。

#### Scenario: 一个Group缺少root Call

- **WHEN** Interface声明两个required Entry而root只放置一个Call
- **THEN** Host Calls分组 MUST明确标记缺失Entry并提供定位root与创建缺失Call命令
- **AND** MUST不伪造已覆盖或自动在selection时创建节点

### Requirement: Interface合同必须拥有受影响闭包可见的正式作者命令

工作区 MUST通过typed Interface Mutation支持创建Interface、创建/删除/配置/排序Entry与typed port。稳定identity MUST由正式allocator生成且默认隐藏，signature hash MUST保持派生只读。Interface提交前 MUST计算并显示受影响Group、Implementation、Call、edge与Projection；提交后 MAY形成明确Invalid authoring供作者继续修复，但 MUST不按名称重绑、自动删除edge或改写Implementation Graph。

#### Scenario: 作者删除仍被连接的Interface端口

- **WHEN** 一个root Call edge或Implementation boundary仍引用该端口
- **THEN** 工作区 MUST在提交前显示全部依赖与将产生的Invalid结果
- **AND** 确认提交 MUST只执行明确合同mutation，不得静默删除或猜测替代连接

### Requirement: Implementation命令必须原子管理required Entry Graph闭包

从Interface创建Implementation时，系统 MUST在一个Undo/rollback事务中创建Implementation资产、全部required Entry binding、每个Entry的Graph owner、Graph、与Interface一致的GraphInput/GraphOutput typed boundary及初始layout。普通创建 MUST不生成业务节点、隐式连线或fallback。复制 MUST复制全部Entry authoring与layout并生成全新稳定identity；Empty Implementation MUST通过单独显式模板命令创建。

#### Scenario: 从Equipment Interface创建Rifle Implementation

- **WHEN** 作者提交合法业务名与目标Group
- **THEN** 系统 MUST为该Interface全部required Entry创建可打开的完整Graph闭包
- **AND** 任一owner、Graph或binding创建失败 MUST回滚全部新对象

#### Scenario: 删除仍被selector引用的Implementation

- **WHEN** Equipment mapping或Empty mapping仍指向该Implementation
- **THEN** 删除命令 MUST拒绝并列出可跳转引用
- **AND** MUST不自动改为另一Implementation或保留孤立Entry Graph

### Requirement: selector作者页必须通过可扩展能力保持核心业务无关

Linked Pose工作区 MUST通过selector authoring capability取得显示名、业务对象目录、字段presenter、typed mutation lowering与validator，不得在通用页面按Equipment、Vehicle或Gameplay State建立中央switch。首个Equipment presenter MUST使用正式Equipment Slot与Equipment catalog对象选择器编辑必填Empty mapping和精确Equipment mapping，并只允许选择实现同一Interface的Implementation。Candidate Closure MUST由mapping派生且只读。

#### Scenario: 作者增加Rifle映射

- **WHEN** 作者从当前Definition的Equipment catalog选择Rifle和兼容Implementation
- **THEN** presenter MUST生成正式Equipment selector mapping mutation并刷新派生Candidate Closure
- **AND** MUST不保存显示名、路径或第二份candidates数组

#### Scenario: 未来注册状态selector

- **WHEN** 新selector capability提供自己的typed presenter与lowering
- **THEN** 同一Group工作流 MUST能够装配该presenter
- **AND** Linked Pose核心页面与runtime MUST不增加业务类型分支

### Requirement: LinkedPoseCall Details必须使用上下文选项并保护现有连线

root `LinkedPoseCall` 的人工字段 MUST只有当前Profile中的Group与该Group Interface中的Entry；Interface、signature和typed ports MUST由选择结果派生。改变Group或Entry前 MUST验证全部现有edge仍匹配port identity、方向与类型；不兼容时 MUST拒绝mutation并显示阻塞edge。Entry Graph context MUST继续拒绝嵌套`LinkedPoseCall`。

#### Scenario: 重绑到端口不兼容的Entry

- **WHEN** 当前Call的输出edge在目标Entry不存在或类型不同
- **THEN** Details MUST禁止提交并逐条列出阻塞连接
- **AND** MUST不先删除edge再完成重绑

#### Scenario: 显式创建缺失Calls

- **WHEN** 作者对Group执行Create Missing Required Calls
- **THEN** 系统 MUST只在root创建缺失的Group+Entry Call节点及其派生端口
- **AND** MUST不连接现有节点、替换root分支或插入隐藏连续化节点

### Requirement: Linked Pose工作区必须用可读命令状态和依赖跳转代替内部identity

Details MUST为每个创建、重绑、删除、Preview与Build命令提供Enabled、Disabled或Unavailable状态及朴素原因。业务关系 MUST使用可读显示名和类型受限对象目录；LinkedPoseInterfaceId、EntryId、ImplementationId、GroupId、revision、signature、GUID、local file id、generated path与compiled handle MUST默认隐藏，仅在明确Diagnostics折叠区只读显示。所有错误 MUST定位到可跳转的Group、selector、Implementation、Entry、Call、port或edge。

#### Scenario: 缺少Definition上下文

- **WHEN** shared Pose Graph没有精确Profile与Definition上下文
- **THEN** Linked Pose创建、mapping与Call Group选择 MUST显示Unavailable及缺失上下文原因
- **AND** MUST不搜索任意使用方、恢复上次上下文或要求输入identity字符串

### Requirement: Preview override必须只驱动匹配Projection的正式Preview session

Bottom Dock MAY提供Group与compiled Implementation的Preview-only override，并只把它提交给当前Preview session-local selection provider。override、展开状态与选择 MUST属于editor view-state，不得修改selector资产、Equipment committed state或正式Runtime session。Projection Stale、revision不匹配或候选未进入compiled catalog时 Preview MUST停止并要求显式Build，不得创建临时Projection或authoring evaluator。

#### Scenario: 作者预览Rifle与Pistol切换

- **WHEN** 当前Projection Ready且两个Implementation都在compiled candidate catalog
- **THEN** Preview MUST通过正式Linked Pose runtime显示选择revision、generation、Entry completion、Call contribution与discontinuity
- **AND** Profile、selector与Equipment状态 MUST不变脏

#### Scenario: 作者修改Rifle Entry Graph

- **WHEN** Graph mutation使Projection Stale
- **THEN** Preview MUST停止消费旧fragment并显示Build Required
- **AND** MUST不因选择Rifle而自动Build

### Requirement: Linked Pose资产Inspector必须收口为轻量工作区入口

Standalone Profile、Interface、Implementation与selector Custom Inspector MUST只显示轻量摘要、只读诊断与`Open in Animation Workspace`命令。创建、配置、删除、依赖扫描、codec、Build、Apply与资产闭包操作 MUST不在`OnInspectorGUI`执行。正式人工写入 MUST只从Animation Workspace降低为typed Presentation Mutation。

#### Scenario: 作者双击Implementation资产

- **WHEN** Unity显示Implementation Custom Inspector
- **THEN** Inspector MUST提供打开精确Profile/Group/Implementation工作上下文的命令或明确Unavailable原因
- **AND** MUST不暴露raw serialized字段或建立第二编辑路径

### Requirement: Linked Pose重操作必须保持显式发布边界

工作区 MUST统一显示`Dirty | Invalid | Stale | Ready | Live`状态。选择对象、打开Entry、修改authoring、Undo/Redo、Inspector focus、窗口恢复、Preview target切换、AssetDatabase refresh或import MUST不自动执行Validate以外的重型扫描、Compile、Character Build、Projection Build、Document Apply、rebase或资产迁移。Toolbar命令 MUST只调用现有正式Validate、Compile与Build入口。

#### Scenario: 新建Implementation后切换到root graph

- **WHEN** 新Entry Graph尚未完成且Projection已Stale
- **THEN** 工作区 MUST保持Invalid/Stale并允许作者继续编辑
- **AND** MUST不自动生成业务节点、构建Projection或选择默认Implementation

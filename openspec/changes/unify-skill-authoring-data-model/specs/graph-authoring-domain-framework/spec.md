## MODIFIED Requirements

### Requirement: Formal authoring metadata必须是Agent与UI共享的唯一语义来源

Formal authoring type、字段、引用方法和正式Mutation写入方法上的metadata MUST是Agent可见kind、typed field、logical port、owner和连接规则的唯一语义来源。共享Capability、Document parser、Exporter、Reconciler、Validator、Compiler和原生UI MUST从该metadata投影同一语义；MUST不维护Agent专用节点模型、字段表、端口表、Pose模型或owner规则副本。

Skill的正式节点定义 MUST集中提供唯一业务参数的字段访问、默认值、约束、端口与引用关系。画布宿主与Document包对象 MUST只适配同一定义，MUST不各自决定可写字段或端口形状；图拓扑仍由原正式图拥有。其他领域 MUST保留自身payload、空间和执行语义，共享定义基础不构成合并业务模型的许可。

原业务节点与FlowCanvas对应节点经核对为同义时，其目录与宿主适配 MUST消费同一业务定义，MUST不得分别声明同义参数、默认值、校验和逻辑端口。框架基类、真实图owner和本地端口映射可以不同，但 MUST不在适配中再次定义业务规则；仅让Agent统一导出两套重复定义不构成该要求的实现。

metadata的Editor查找实现 MAY生成不可编辑的静态索引，但该索引 MUST不成为第二作者真相，也不得要求运行时反射、Unity序列化字段或Compiler operation作为作者合同。

#### Scenario: Agent和原生UI使用同一作者字段

- **WHEN** 正式作者类型metadata声明一个可写typed field和合法port
- **THEN** Agent Document、原生Details/创建菜单、Validator和Compiler MUST使用同一字段和port语义
- **AND** 不得保留另一套手写Agent或UI能力定义继续接受旧字段

#### Scenario: Agent提交未声明作者内容

- **WHEN** Document目标提交metadata未声明的字段、port、引用或owner关系
- **THEN** 共享Capability或Mutation preflight MUST拒绝该目标并返回稳定诊断
- **AND** MUST不按C#类型名、显示名、SerializedProperty路径或runtime index猜测能力

#### Scenario: 正式作者类型内部重构

- **WHEN** 正式作者类型的C#实现或文件组织变化，但metadata的稳定kind、typed field、logical port和owner语义不变
- **THEN** Agent Document和原生UI MUST保持相同可见行为
- **AND** 不得仅因内部实现变化升级Document schema

#### Scenario: 修改Skill字段定义

- **WHEN** Skill正式定义修改一个字段的合法范围或条件端口
- **THEN** 原生UI、Document和编译 MUST采用相同定义
- **AND** MUST不通过另行维护节点实例原型或包端口表继续接受旧规则

#### Scenario: 两个作者宿主编辑同义业务节点

- **WHEN** 原作者入口与FlowCanvas入口分别编辑相同业务种类的节点
- **THEN** 两者 MUST使用同一参数与规则定义，各自保留实例值和图身份
- **AND** 差异只允许属于已声明的宿主映射或明确不同的业务语义

## MODIFIED Requirements

### Requirement: Formal authoring metadata必须是Agent与UI共享的唯一语义来源

Formal authoring type、字段、引用方法和正式Mutation写入方法上的metadata MUST是Agent可见kind、typed field、logical port、owner和连接规则的唯一语义来源。共享Capability、Document parser、Exporter、Reconciler、Validator、Compiler和原生UI MUST从该metadata投影同一语义；MUST不维护Agent专用节点模型、字段表、端口表、Pose模型或owner规则副本。

metadata的Editor查找实现 MAY生成不可编辑的静态索引，但该索引 MUST不成为第二作者真相，也不得要求运行时反射、Unity序列化字段或Compiler operation作为作者合同。

原业务节点与FlowCanvas对应节点经核对为同义时，参数、默认值、校验、逻辑端口和引用 MUST由同一业务定义提供。两侧目录及作者宿主只投影该定义；本地identity/端口映射与框架事件可以不同，但 MUST不各自重写业务规则。每个节点实例 MUST独立保存自己的值；图拓扑与现有公开字段、端点和owner形状 MUST保持各自正式来源。

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

#### Scenario: 两个作者宿主使用同义节点

- **WHEN** 原作者入口与FlowCanvas入口提交相同业务配置
- **THEN** 两者 MUST采用同一参数约束和逻辑端口规则，保留各自实例值和来源identity
- **AND** 单独让Agent导出两套重复定义 MUST不视为满足此要求

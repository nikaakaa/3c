## ADDED Requirements

### Requirement: Formal authoring metadata必须是Agent与UI共享的唯一语义来源

Formal authoring type、字段、引用方法和正式Mutation写入方法上的metadata MUST是Agent可见kind、typed field、logical port、owner和连接规则的唯一语义来源。共享Capability、Document parser、Exporter、Reconciler、Validator、Compiler和原生UI MUST从该metadata投影同一语义；MUST不维护Agent专用节点模型、字段表、端口表、Pose模型或owner规则副本。

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

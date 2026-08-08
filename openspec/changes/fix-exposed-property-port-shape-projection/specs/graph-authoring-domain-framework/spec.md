## ADDED Requirements

### Requirement: Capability 必须统一投影 typed discriminator 条件端口

Graph Authoring Capability MUST区分类型级固定端口、由节点typed property discriminator决定的条件投影端口，以及作者显式拥有的动态端口。条件端口 MUST由唯一纯Node Port Shape Projector根据capability identity与strict typed properties计算；Canvas、Details、Document catalog、连接策略、Mutation和Compiler MUST消费同一结果。系统 MUST NOT通过默认构造节点、C#字段初值、当前edge、selection或现有资产snapshot猜测条件端口形状。

#### Scenario: Set Blackboard节点投影输入端口

- **WHEN** `exposed-property`节点的`exposedProperty.mode`为`Set`
- **THEN** projector MUST投影`Input` Flow输入单连接端口和`m_Value` Property输入单连接端口
- **AND** Canvas MUST不绑定默认Get节点的输出端口形状

#### Scenario: Get Blackboard节点投影输出端口

- **WHEN** `exposed-property`节点的`exposedProperty.mode`为`Get`
- **THEN** projector MUST不投影Set专用`Input` Flow端口
- **AND** MUST投影`m_Value` Property输出多连接端口

#### Scenario: discriminator无法确定唯一变体

- **WHEN** typed properties缺少discriminator、值非法或同时匹配多个端口变体
- **THEN** Capability projection MUST明确失败
- **AND** 系统 MUST NOT使用默认变体、首个变体或现有PortView继续authoring

### Requirement: 条件端口 identity 不得作为实例端口镜像保存

条件端口的稳定identity、方向、容量、required与value type MUST由Capability变体声明。Graph实例 MUST只保存决定变体的typed properties及引用端口identity的edge，MUST不复制一份条件端口metadata。作者可编辑动态端口继续使用node-local稳定identity，不得与条件端口或固定端口重复。

#### Scenario: sparse Graph保存ExposedProperty Set

- **WHEN** Document导出一个Set `exposed-property`节点
- **THEN** Node MUST保存`exposedProperty.mode=Set`及其它typed properties
- **AND** Property edge MAY引用`m_Value`
- **AND** Node MUST不保存`m_Value`的方向或容量镜像

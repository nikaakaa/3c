## ADDED Requirements

### Requirement: Node Catalog 必须机器可读地声明条件端口变体

`context/node-catalog.json` MUST为具有typed discriminator端口的node kind声明严格条件端口变体。每个变体 MUST包含唯一匹配条件及完整条件Flow/Property端口集合；projector MUST将固定端口与唯一命中的条件集合合并成最终形状。同一typed node properties MUST恰好匹配一个变体。Exporter、strict Package Mapper、Reconciler、Mutation preflight与Validator MUST通过同一Capability projector解析变体，MUST不维护各自的mode判断。

#### Scenario: Catalog导出ExposedProperty变体

- **WHEN** service生成包含`exposed-property`能力的node catalog
- **THEN** Catalog MUST分别声明Get和Set的匹配条件及端口形状
- **AND** MUST不把默认Get实例的`m_Value`输出端口声明为全部实例的固定端口

#### Scenario: Document连接Set输入值

- **WHEN** sparse Graph的Property edge把输出值连接到mode为Set的`exposed-property.m_Value`
- **THEN** Package Mapper MUST按Set变体接受该Input endpoint
- **AND** Reconciler与Mutation preflight MUST使用同一目标形状建立连接

#### Scenario: Document把Set端口当作输出

- **WHEN** sparse Graph把mode为Set的`exposed-property.m_Value`作为Property edge输出endpoint
- **THEN** strict parse MUST报告带node、mode、port和期望方向的机器可读错误
- **AND** MUST不通过当前Unity snapshot端口或默认Get capability放行

### Requirement: 条件端口变体切换必须进入同一对账事务

节点typed properties变化导致端口变体切换时，Reconciler MUST从完整目标Graph计算删边、节点配置与建边顺序，并将其写入同一immutable Mutation Plan。Preflight MUST在修改Unity对象前证明目标端口和edge闭合；任一步失败 MUST使整次apply回滚，不得留下旧edge、半换向端口或Document与Unity树分叉。

#### Scenario: Get节点改为Set节点

- **WHEN** Document把已有`exposed-property`从Get改为Set并提交相应目标edges
- **THEN** plan MUST先删除引用旧Output形状的不兼容edge，再配置Set形状并建立目标Input edge
- **AND** node identity与Blackboard declaration reference MUST保持不变

#### Scenario: mode变化但旧edge仍保留

- **WHEN** Document目标把Get改为Set但仍把`m_Value`作为输出endpoint
- **THEN** dry-run MUST拒绝该完整目标
- **AND** 系统 MUST不自动删除Document仍声明的edge或忽略mode变化

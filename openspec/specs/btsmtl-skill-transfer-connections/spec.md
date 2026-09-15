# btsmtl-skill-transfer-connections Specification

## Purpose
TBD - created by archiving change add-skill-transfer-connections. Update Purpose after archive.

## Requirements

### Requirement: 技能状态机转移由连线承载

系统 MUST 将技能图状态机（StateMachine role 图）内的状态转移表达为携带语义数据的连接：条件图引用、优先级、中止策略保存在连接对象上。`BtsmtlSkillStateFlowNode`、状态机入口与任意状态节点 MUST NOT 暴露步骤（steps）端口；系统 MUST NOT 将转移条件、优先级或中止策略保存在节点步骤数据中。顺序/选择/并行/循环等流程复合节点的步骤端口不属于状态机转移，其表达方式不在本能力范围内。

#### Scenario: 在状态机图创建转移

- **WHEN** 作者在状态机图内从状态节点或任意状态节点拖出转移连线
- **THEN** 新建连接 MUST 携带条件图引用、优先级与中止策略字段
- **AND** 源节点 MUST NOT 因建立转移而新增步骤端口

#### Scenario: 打开状态机图查看转移

- **WHEN** 作者在画布上查看状态机图的转移连线
- **THEN** 每条连线 MUST 在画布上显示其条件摘要
- **AND** 挂有非默认优先级或非默认中止策略的连线 MUST 在摘要中一并显示

### Requirement: 转移连线编辑走正式技能 Mutation

转移连线上的条件图引用、优先级与中止策略的编辑 MUST 通过技能图统一 Mutation 事务完成；非法修改（条件图 role 不是 ConditionRule、负数优先级、未定义的中止策略）MUST 在写入前被拒绝。连线编辑 MUST 纳入技能图撤销链与内容版本。

#### Scenario: 编辑转移连线

- **WHEN** 作者选中转移连线并修改条件、优先级或中止策略
- **THEN** 修改 MUST 通过正式 Mutation 写入连接对象
- **AND** 条件图引用 role 不合法时 MUST 拒绝并显示原因

#### Scenario: 撤销连线编辑

- **WHEN** 作者对转移连线的编辑执行撤销
- **THEN** 连接对象的条件、优先级与中止策略 MUST 恢复到编辑前状态

### Requirement: 转移调度元数据属于连线

转移的优先级与同优先级稳定顺序 MUST 属于连线调度数据。多条转移的条件同时成立时，运行 MUST 先按连线优先级选择，优先级相同 MUST 按连线创建顺序保持稳定。任意状态节点发出的转移 MUST 携带条件；缺失条件的任意状态转移 MUST 被校验报告为非法结构，系统 MUST NOT 将其降级为无条件转移或自动补条件。

#### Scenario: 多条转移同时成立

- **WHEN** 同一源状态的多条转移条件同时为真
- **THEN** MUST 按连线优先级选择目标
- **AND** 优先级相同时 MUST 按连线创建顺序保持稳定选择

#### Scenario: 任意状态转移缺条件

- **WHEN** 任意状态节点发出的转移连线未挂条件图
- **THEN** 校验 MUST 报告该连线非法并指明连线位置
- **AND** 系统 MUST NOT 自动补条件或将其视为无条件转移

### Requirement: 编译以连线为唯一转移来源

技能图编译 MUST 从转移连线读取条件、优先级与中止策略；状态机结构节点上不存在可供编译读取的步骤转移数据，若发现残留步骤数据结构 MUST 报告迁移残留错误。转移条件图的求值语义保持纯条件图（只读当前状态运行事实与黑板），与现行 ConditionRule 合同一致。

#### Scenario: 编译状态机图

- **WHEN** 技能编译遍历状态机图
- **THEN** 每条转移的运行语义 MUST 由其连线上的条件、优先级与中止策略决定
- **AND** 状态机结构节点携带旧步骤数据时 MUST 报告迁移残留错误而非静默忽略

#### Scenario: 转移连线未挂条件

- **WHEN** 非任意状态来源的转移连线未挂条件图
- **THEN** 该转移 MUST 视为无条件转移并允许通过
- **AND** 运行 MUST NOT 因缺失条件而失败

### Requirement: 连线数据纳入技能图身份与 Document

转移连线 MUST 纳入技能图节点/连线身份合同与 Document 导出：保存重载后连线身份与转移数据保持一致；正式 checkout 与 dry-run 对已迁移资产 MUST 返回零差异。Document 编解码 MUST 拒绝携带未知连接类型的包，不回退为普通连线。

#### Scenario: 保存重载连线

- **WHEN** 含转移连线数据的技能图被保存并重新加载
- **THEN** 连线身份与条件、优先级、中止策略 MUST 保持一致

#### Scenario: Document 校验已迁移资产

- **WHEN** 对已完成迁移的技能根执行正式 checkout 与 dry-run
- **THEN** 返回 MUST 为零差异（Clean）
- **AND** 包内连接类型 MUST 与代码登记一致，未知类型被拒绝

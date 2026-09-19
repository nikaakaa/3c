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

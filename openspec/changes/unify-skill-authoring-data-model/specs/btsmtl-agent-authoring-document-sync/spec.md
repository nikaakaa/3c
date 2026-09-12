## MODIFIED Requirements

### Requirement: Agent Document必须使用唯一Skill节点与边语义

Agent Document MUST从正式Skill authoring metadata和共享Capability catalog投影节点的typed properties、logical ports、引用和owner。Skill Graph拓扑仍以FlowCanvas正式资产为唯一真相；Document MUST不维护第二份可写拓扑或节点语义。

状态机转移 MUST 从唯一Edge payload投影`conditionGraphId`、`priority`、`abortPolicy`和`order`。状态节点、anchor和普通组合Step MUST不再复制状态机转移字段。普通Sequence、Selector、Parallel的Step目标仍位于对应节点的`properties.steps`。

#### Scenario: 状态机边包含转移数据

- **WHEN** Document导出或接收StateMachine Edge
- **THEN** Edge MUST拥有稳定`id`、逻辑端点、条件图、priority、abortPolicy和非负order
- **AND** 同一source下的order MUST唯一

#### Scenario: 状态机旧Step字段出现

- **WHEN** editable StateMachine anchor或state目标包含旧`steps`字段
- **THEN** strict parser、Validator或Reconciler MUST拒绝该目标
- **AND** 普通组合节点的`properties.steps` MUST继续按其自身Capability校验

### Requirement: Edge条件图owner必须闭合

状态机Edge的ConditionRule owner MUST 使用`kind=edge`、所属`graphId`、源`nodeId`、`edgeId`和`referenceKey=condition`。Exporter、OwnerCollector、Closure、ClosureIndex、GraphCopy、Validator、Reconciler、Applier和Compiler MUST使用同一owner关系；不得按显示名、节点位置或旧Step补读。

#### Scenario: Edge条件图可达

- **WHEN** Edge引用ConditionRule graph
- **THEN** 该graph MUST通过edge owner进入所属Skill闭包、owner检查、可达性和循环检查
- **AND** condition graph缺失、role错误、edgeId错误或owner字段不一致 MUST返回稳定路径诊断

### Requirement: Document版本必须切换到v8

系统 MUST只接受`btsmtl-agent-authoring-document.v8`。manifest、sync、Report、Codec、Store和五个生命周期工具 MUST共享同一版本常量；v7及更早版本 MUST返回unsupported schema，不得保留converter、兼容reader、一次性migrator或旁路JSON写入。

#### Scenario: 旧包重新checkout

- **WHEN** v7或更早package进入checkout
- **THEN** 系统 MUST要求重新checkout并生成v8 package
- **AND** checkout MUST不修改Unity authoring、不自动apply、不启动第二事务

#### Scenario: v8整包往返

- **WHEN** v8 package经过checkout、editable修改、dry-run、同hash apply和重新checkout
- **THEN** 五个生命周期工具 MUST使用同一整包hash和唯一资产事务
- **AND** Node、Edge、引用、owner和order MUST在反向导出后保持稳定

### Requirement: package重建不得增加迁移路径

存量package重建 MUST先删除被替代的旧package目录，再调用正式`checkout_document`生成新包。删除前的代码和资产历史由Git保留；系统 MUST不新增独立迁移器、旧字段兼容读或Document专用写服务。

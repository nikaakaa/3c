## Why

Skill authoring 当前有两类重复来源：状态机转移参数同时存在于状态节点的 `BtsmtlSkillStepPort` 和转移边，FlowCanvas节点、共享Capability与Agent包又分别描述节点字段和端口。这样同一条业务规则需要改两处，复制、闭包、导出和编译也可能读取不同来源。

本change把Skill authoring收成一条正式数据链：FlowCanvas继续拥有正式图拓扑，节点和边各自只有一个typed payload，Capability与Port Shape只有一个投影入口，Document只通过既有五个生命周期工具读写整包。

## What Changes

- 状态机转移统一使用Edge-owned `BtsmtlSkillTransferPayload`，由Edge唯一保存条件图、priority、abortPolicy和显式order；状态节点不再保存转移副本。
- 状态机使用固定逻辑端点：`@enter/@any/state.Transfer`只能连接到`state/@exit.StateIn`；普通Sequence、Selector、Parallel继续使用节点自身的 `properties.steps`，不与状态机转移混用。
- StateMachine条件图的owner统一为`kind=edge`、`graphId`、`nodeId`、`edgeId`、`referenceKey=condition`。Closure、ClosureIndex、GraphCopy、Exporter、Validator、Applier、Occurrence和SourceMap全部读取同一Edge数据。
- 正式节点定义由共享Capability catalog和唯一`GraphAuthoringNodePortShapeProjector`投影。Agent、Inspector、创建菜单、复制、Validator和编译不再维护第二套字段、端口或owner规则。
- Agent Authoring Document切换为`btsmtl-agent-authoring-document.v8`。v7及更早包只返回unsupported schema；不增加converter、兼容reader、一次性migrator或旁路写入入口。
- 存量包按“删除旧包、保留Git代码与资产历史、使用正式`checkout_document`重新生成v8包”的方式处理；Document apply仍只通过现有Reconciler、Mutation和唯一资产事务完成。
- 同步现行spec、技能合同、MCP说明和change任务记录；不改变运行时第二套执行器，不新增测试代码。

## Capabilities

### Modified Capabilities

- `btsmtl-skill-authoring-model`：补齐节点typed payload、Edge transfer payload、唯一Port Shape和实例值独立保存规则。
- `graph-authoring-domain-framework`：明确Capability与Port Shape是UI、Document、复制和编译的唯一节点语义来源。
- `btsmtl-agent-authoring-document-sync`：切换到Document v8，补齐Edge owner/order/condition闭包和严格拒绝旧包规则。
- `btsmtl-agent-authoring-mcp-bridge`：五个生命周期工具统一接受和返回v8结果，不增加Skill专用工具。

## Impact

- Runtime Skill authoring：`FlowConnection`、状态结构节点、节点Capability和Graph Closure。
- Editor SkillDocument：模型、Exporter、Package Projection、Validator、OwnerCollector、Applier、Diff和Occurrence。
- Agent Document：schema常量、Codec、Store、MCP描述、checkout/dry-run/apply/reverse-export链。
- 正式资产：Attack、DodgeBack、DodgeForward及其共享条件图按稳定identity保留；状态机旧steps删除，普通组合steps保留。
- 文档：current spec、技能合同和本change记录同步为v8及唯一Edge数据合同。

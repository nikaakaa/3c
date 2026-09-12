## Context

FlowCanvas是Skill authoring的正式拓扑和资产存储。问题不是再造一张图，而是同一条业务边界在节点、边、Capability、Document和编译链中重复表达。

当前状态机转移参数已经迁到`BtsmtlSkillFlowConnection.Payload`，状态节点只提供固定逻辑端点。普通组合节点仍需要自己的`properties.steps`来表达Sequence、Selector和Parallel的执行顺序；它们不是状态机Transition，不能合并。

## Goals / Non-Goals

**Goals:**

- 状态机转移参数只有Edge一份typed payload，并有稳定order。
- 节点定义、字段、引用和Port Shape只有共享Capability一份；每个节点实例独立保存值。
- 条件图、Macro、Timeline、Blackboard、Node、Edge和owner进入同一引用闭包。
- Document v8严格读写，旧v7包显式重新checkout，不保留兼容路径。
- 通过正式JSON checkout、dry-run、apply和validate完成存量资产重建，不手写Unity YAML，不新增migrator。

**Non-Goals:**

- 不把FlowCanvas拓扑复制到Agent或运行时，不新增第二个图编辑器或第二个Mutation服务。
- 不把普通组合steps删除；只删除状态机anchor和状态节点上的旧转移steps。
- 不把Pose、Foot、AI、网络或运行时执行器改造成Skill authoring模型。
- 不新增测试代码；手动端到端验证不写入tasks。

## Decisions

### D1 唯一数据owner

| 内容 | 唯一来源 |
|---|---|
| Skill Graph拓扑、Node/Edge identity和布局 | FlowCanvas正式资产 |
| 普通组合分支 | `BtsmtlSkillCompositeFlowNode.Steps`及Document节点`properties.steps` |
| 状态机转移条件、priority、abortPolicy、order | `BtsmtlSkillFlowConnection.Payload` |
| 节点字段、逻辑端口、引用和创建/删除规则 | Formal authoring metadata与共享Capability catalog |
| Document文件、hash、diff和事务生命周期 | Agent Document Store、Reconciler、Mutation和Transaction Service |

业务取舍：继续让State节点保存一份转移参数可以少改代码，但会继续产生运行和导出分歧。本change选择Edge唯一owner，迁移成本集中在一次资产重建，之后状态机顺序和条件只有一个可审查来源。

### D2 Edge transfer payload

`BtsmtlSkillTransferPayload`只允许ConditionRule graph、非负priority、合法`ProgramAbortPolicy`和非负order。`BtsmtlSkillFlowConnection.Configure`是唯一写入口；创建边时按同一source的已有Transfer边分配下一个order，Document apply按目标order写回。

状态机的Transfer输出允许多条边，StateIn输入按正式状态拓扑规则接收；同一source的order必须唯一。`@any`转移必须有条件图。非状态机普通边不携带Transfer owner或状态机条件含义。

### D3 唯一Port Shape

`GraphAuthoringCapabilityDescriptor`声明固定端口、动态端口政策、字段和Graph role；`GraphAuthoringNodePortShapeProjector`根据Capability、typed properties和稳定动态端口生成完整Port Shape。Canvas、Document、Clipboard、Validator、Reconciler和Compiler只消费该结果。

本地框架端口ID可以不同，但映射必须显式。不得按C#类型名、显示名、节点位置、SerializedProperty或编译index推断端口，也不得把状态机旧step临时投影为anchor动态端口。

### D4 引用闭包和owner

状态机Edge的ConditionRule owner固定为`kind=edge`、`graphId`为所属状态机、`nodeId`为源状态、`edgeId`为转移边、`referenceKey=condition`。Graph Closure、ClosureIndex、GraphCopy、Exporter、OwnerCollector、Validator和Applier必须沿Edge读取同一条件引用。

普通组合节点的条件仍由`properties.steps`表达并使用`kind=step` owner。两种条件owner不互读、不补读、不按显示名猜测。

### D5 Document v8与存量重建

schema唯一值为`btsmtl-agent-authoring-document.v8`。Codec、Store、MCP工具和所有Report从同一常量读取版本；v7及更早manifest、sync或输入包直接返回unsupported schema，不做转换。

存量处理顺序是：保留Git提交历史和正式Unity资产变更记录，删除被忽略的旧`.btsmtl`目录，调用正式`checkout_document`生成v8，再进行无修改dry-run和validate。需要改变资产时只提交完整Document hash给`apply_document`，失败由现有Undo和package publish回滚。

### D6 代码与文档归并

本change承接节点/边数据统一、Port Shape、闭包和Document v8合同。其它并行change可以继续处理原生FSM、Timeline或观察界面，但不能在Skill authoring中再引入第二份转移payload、节点目录或Document版本。若并行改动尚未形成可编译、可闭包的正式资产，checkout必须报告阻塞，不绕过它生成假包。

## Risks / Trade-offs

- [Edge与旧Step值不同]：按精确identity逐条核对；不自动选择一侧。当前已将资产值写入Edge，旧Step删除。
- [v7包无法直接打开]：这是有意的破坏性切换；代价是旧包必须重新checkout，收益是没有长期双读。
- [并行authoring改动未闭合]：正式checkout会拒绝当前目标；保留外部未提交改动，待其形成完整作者闭包后再重试。
- [普通组合steps误删]：只删除anchor.steps和状态机旧steps，保留普通节点`properties.steps`及其Step owner。

## Migration Plan

1. 固定正式资产、Document和Git工作区边界，输出状态机Step/Edge逐实体对账。
2. 使用唯一Transfer payload和固定端口收口状态机资产，补齐order、edge owner和条件引用闭包。
3. 让Closure、Copy、Exporter、Validator、Applier、Occurrence和Compiler只读正式Edge数据。
4. 删除旧package目录，修改schema为v8，通过正式checkout重建整包；不添加迁移器。
5. 对v8执行无修改dry-run、validate、必要的正式编译和重新checkout，核对identity、owner、condition、priority、abortPolicy、order和hash。
6. 删除旧anchor.steps、旧补读、重复字段和残余错误提示，更新current spec、技能合同、MCP说明和任务记录。

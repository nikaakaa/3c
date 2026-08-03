# Design: Agent Authoring 可直接编辑的同步 JSON 文档包

## Context

当前实现已经把低层Patch链迁移为：

```text
single JSON Document
  -> strict codec
  -> AgentDocumentReconciler
  -> immutable AgentMutationPlan
  -> typed handler
  -> formal BTSMTL authoring API
  -> Validator
```

后半段的Reconciler、Mutation、事务、Undo、Validator和正式authoring API方向正确。问题集中在AI-facing外部表示与MCP边界：

- 当前单文件为4.35 MB、99,821行。
- Graph和Timeline占压缩editable的绝大多数。
- Node DTO是Unity/C#对象投影，不是面向Graph组合的authoring语言。
- exporter与creator的Node能力集合不闭合。
- 一个MCP工具用`action`复用五种不同安全语义。

目标不是恢复`create_node`、`link_edge`之类局部工具。AI应直接修改目标JSON，系统只在显式生命周期边界把目标状态降低为内部Mutation。

## Goals

- 让AI按文件夹和业务实体直接编辑Graph，而不是编排工具调用。
- 让Graph JSON只表达业务结构，不复制Unity序列化细节。
- 支持Node、Flow Edge、Property Edge、Graph reference、Condition、Timeline和领域配置的完整目标状态闭包。
- 保持Unity资产为唯一正式authoring真相。
- 保持一个逻辑Document、一份基线、一个整包hash和一次事务。
- 保持文件保存轻量，不自动compile、build或apply。
- 让MCP工具数量与Node种类解耦。

## Non-Goals

- 不提供节点级、边级、字段级或JSON patch MCP工具。
- 不把JSON包打进Player或Runtime。
- 不做文件级提交、文件级Conflict或自动merge。
- 不做文件watcher、后台daemon或自动构建。
- 不创建缺失Definition根。
- 不把Presentation和generated analysis变为Agent可写配置。

## Decision 1: 一个逻辑Document，物理上使用确定性目录包

正式路径：

```text
<UnityProject>/AgentAuthoring/Documents/<domain>/<root-key>.btsmtl/
```

目录结构：

```text
<root-key>.btsmtl/
  manifest.json
  editable/
    controller.json
    blackboard.json
    actions.json
    graphs/
      <graph-id>/
        graph.json
        layout.json
    timelines/
      <timeline-id>/
        timeline.json
        curves.json
    ai/
      perception.json
  context/
    node-catalog.json
    graph-kinds.json
    asset-catalog.json
    dependencies.json
  .sync.json
```

Character与AI只生成本domain有意义的文件；不生成空领域占位文件。`manifest.json`记录schema、domain、root identity和规范文件清单。`.sync.json`只保存service-owned基线，不保存业务authoring。

物理拆分只解决选择性读取和局部编辑，不改变同步粒度。checkout、rebase、dry-run、apply、Conflict和反向导出始终针对整个文档包。系统不暴露“只应用一个graph.json”。

文档包位于`Assets/`之外，不触发AssetDatabase import，不进入Player或Bundle，也不作为版本控制中的正式authoring来源。

## Decision 2: schema v2是稀疏规范authoring语言

外部schema固定为：

```text
btsmtl-agent-authoring-document.v2
```

Graph示意：

```json
{
  "id": "local:attack-body",
  "kind": "state-body",
  "owner": {
    "entityId": "local:attack-state",
    "slot": "body"
  },
  "nodes": [
    {
      "id": "local:attack-timeline",
      "kind": "timeline",
      "properties": {
        "timeline": "local:attack-timeline-data",
        "actionContext": "Attack"
      }
    }
  ],
  "flowEdges": [
    {
      "id": "local:attack-entry",
      "from": { "node": "@root", "port": "out" },
      "to": { "node": "local:attack-timeline", "port": "in" }
    }
  ],
  "propertyEdges": []
}
```

规则：

- `kind`是稳定外部标识，不是C# type name、namespace或显示名。
- `properties`只输出当前kind有意义且偏离正式默认值的字段。
- 不输出空集合、无关nullable字段、重复port声明、route/path和ownership派生字段。
- 端口只使用catalog定义的逻辑key。
- Edge是完整目标集合；从集合移除即表达删除。
- Graph必须拥有`owner.entityId + owner.slot`，不能创建裸Graph。
- 已有实体使用stable authoring identity。
- 新实体使用文档包内唯一`local:<meaningful-id>`。
- Node kind与Graph kind不可原地改变。类型变化必须删除旧identity并创建新local identity。

稀疏并不意味着宽松。缺省值由对应kind的唯一catalog schema定义；未知字段、未知kind和未知port仍严格失败。

## Decision 3: 系统节点投影为只读anchor

Unity内部需要Root、Enter、Exit、Any、OnEnter、OnExit、TimelineEnter和ConditionRuleResult等系统Node。AI不应创建、删除或配置它们。

文档包按Graph kind暴露保留anchor：

```text
@root
@enter
@exit
@any
@onEnter
@onExit
@timelineEnter
@result
```

anchor只允许作为Edge endpoint。`graph-kinds.json`定义每种Graph允许的anchor与逻辑port。Exporter把内部系统Node endpoint转换为anchor；Reconciler再解析回当前Graph真实系统Node。anchor不拥有editable identity、layout或properties。

这样AI仍能完整连接Graph，但无需理解系统Node构造和内部类型。

## Decision 4: Graph逻辑与布局分离

`graph.json`只表达Graph业务逻辑。`layout.json`只按Node identity保存位置和可视分组，不复制Edge或Node配置。

已有Node位置在checkout时保留。AI可以：

- 修改`layout.json`显式布局。
- 完全不碰layout，让现有位置保持。
- 为新Node省略位置，由唯一确定性自动布局器按Graph kind、拓扑层级和identity顺序生成位置。

自动布局是正式明确规则，不是隐藏fallback配置。相同Graph目标状态必须生成相同初始位置。布局变化进入editable hash，但不触发Program/Projection build。

## Decision 5: Timeline结构与Curve payload分离

`timeline.json`保存Timeline、Track、Clip、Marker、ownership和引用关系。`curves.json`按stable/local curve identity保存完整Curve payload。

Curve规则：

- 只输出影响正式语义的wrap mode和key字段。
- 与正式默认值相同的weight、weighted mode等字段省略。
- 同一Curve完整替换，不提供key级MCP操作。
- registered Channel仍由catalog identity约束，不能按字段名猜测。

这样AI修改Timeline拓扑时无需加载全部Curve key，修改Curve时也不会重写Graph文件。

## Decision 6: 一个能力catalog驱动export、edit、reconcile和validate

当前`AgentNodeEmitterRegistry`只能创建部分exporter可输出Node。v2将其提升为唯一`AgentAuthoringCapabilityCatalog`，每个Node kind声明：

- 稳定kind。
- 允许的Graph kind。
- typed properties及正式默认值。
- 逻辑Flow/Property ports。
- 资产引用类型。
- create/configure/delete lowering。
- read-only或system-owned性质。

Graph kind catalog声明：

- Graph kind。
- owner slot。
- 系统anchor。
- 允许的Node capability。
- inline/shared ownership规则。

同一catalog必须被exporter、strict parser、Reconciler、handler preflight、Validator和checkout context writer复用。任何editable实体无法完整往返时，checkout以`authoring_capability_incomplete`失败；系统不输出“可看但不可改”的假可编辑Node。

只读`node-catalog.json`和`graph-kinds.json`是该正式catalog对AI需要部分的紧凑投影，不是第二个手写schema。

## Decision 7: context只保存AI作出编辑决策所需的信息

`context`只包含：

- Node与Graph kind catalog。
- 当前Definition可引用的Input、Action、Timeline、Blackboard、Perception和资产identity。
- owner与dependency关系。
- AI受控Character input/request合同。
- 对编辑有影响的只读Presentation、Body Motion和generated product状态摘要。

以下内容不进入文档包：

- Unity managed-reference布局。
- C#类型全名和SerializedProperty path。
- runtime state、对象实例ID和时间戳。
- Validator可以直接从当前Unity资产读取、但AI编辑不需要的数据。
- 大型generated Foot Analysis、Program、Projection或Database payload。

context文件由service独占写入。AI修改context时报告`readonly_context_modified`。

## Decision 8: 整包canonical hash与四态同步

每个JSON文件使用strict parser和canonical writer：

- UTF-8无BOM。
- 拒绝重复属性和未知字段。
- 稳定字段顺序。
- 稳定entity顺序。
- 明确有限数值格式。

整包hash：

```text
editableHash = H(sorted(editable relative path + file semantic hash))
contextHash = H(sorted(context relative path + file semantic hash))
documentHash = H(schema + domain + root identity + editableHash + contextHash)
```

`.sync.json`保存：

```text
baseSourceRevision
baseEditableHash
baseContextHash
```

`.sync.json`不参与editable/context hash。manifest与sync身份不一致、文件清单缺失、出现未登记JSON文件或context被修改都属于非法文档包。

状态：

| Unity侧变化 | editable变化 | 状态 |
|---|---|---|
| false | false | `Clean` |
| true | false | `TreeDirty` |
| false | true | `DocumentDirty` |
| true | true | `Conflict` |

Unity侧变化包含live可写authoring revision或current context hash变化。状态每次显式调用重新计算，不保存可编辑dirty bool。

## Decision 9: Document Store使用目录级staging与原子发布

checkout和apply反向导出先生成完整staging目录：

1. 写入全部规范文件。
2. 严格重读并计算整包hash。
3. 校验manifest文件清单和root身份。
4. 将当前正式目录改为rollback目录。
5. 将staging目录原子切换为正式目录。
6. 成功后删除rollback目录；失败时恢复上一目录。

Store只接受由domain、root path和root identity计算的确定性路径。调用方不能提交任意文档目录，Store也不扫描其它目录寻找替代包。

apply期间文档包发布与Unity资产事务属于同一应用服务成功边界。反向发布失败时不得报告`Clean`。

## Decision 10: Reconciler消费目标状态，不消费编辑操作

正式链：

```text
document package
  -> strict multi-file parse
  -> package/root/context validation
  -> current canonical Unity projection
  -> AgentDocumentReconciler
  -> immutable AgentMutationPlan
  -> preflight
  -> typed handlers
```

Reconciler负责：

- stable/local identity索引。
- owner和Graph创建顺序。
- Node创建、属性更新和删除。
- Flow Edge与Property Edge完整增删。
- endpoint变化降低为旧Edge删除和新Edge创建。
- Graph reference、ConditionRule、StateMachine和Timeline引用绑定。
- Timeline、Track、Clip、Marker和Curve完整目标状态。
- Blackboard、Action与AI领域配置。
- 受影响serialized owner收集和删除顺序。

AI不填写mutation kind、handler、前序输出或执行顺序。Patch/operation catalog不再是外部合同。

## Decision 11: 五个独立MCP工具只表达生命周期

删除`manage_btsmtl_agent_authoring(action, ...)`，正式工具固定为：

| Tool | 输入 | 行为 |
|---|---|---|
| `btsmtl.checkout_document` | `domain`, `root_asset_path` | 创建或刷新文档包 |
| `btsmtl.rebase_document` | `domain`, `root_asset_path`, `confirm_rebase` | 接受当前Unity基线 |
| `btsmtl.dry_run_document` | `domain`, `root_asset_path` | 严格解析、reconcile、preflight |
| `btsmtl.apply_document` | `domain`, `root_asset_path`, `expected_document_hash` | 事务apply并反向同步 |
| `btsmtl.validate` | `domain`, `root_asset_path` | 只读验证正式Unity树 |

每个工具：

- input schema设置`additionalProperties: false`。
- 拥有独立output schema和`structuredContent`。
- 业务/schema错误用tool execution error返回`code/path/message/suggestion`。
- 不返回或嵌入完整文档包JSON，只返回绝对路径、状态、hash、diff摘要和诊断。

建议annotations：

| Tool | readOnlyHint | destructiveHint | idempotentHint | openWorldHint |
|---|---:|---:|---:|---:|
| checkout | false | false | true | false |
| rebase | false | true | true | false |
| dry-run | true | false | true | false |
| apply | false | true | false | false |
| validate | true | false | true | false |

五个工具的数量永远不随Node、Edge、Timeline类型增长。

## Decision 12: 文件编辑属于宿主，不属于BTSMTL领域工具

checkout返回文档包绝对路径。AI使用Codex系统文件工具、通用filesystem MCP或其它宿主已有文件能力直接读写JSON。

BTSMTL不提供：

```text
create_node
delete_node
link_edge
configure_node
edit_curve_key
write_document_file
apply_json_patch
```

官方MCP将Tools定义为模型控制的外部动作，将Resources定义为上下文数据。直接文件编辑已经由通用filesystem能力覆盖；再建立BTSMTL局部工具只会复制文件编辑和Reconciler职责。

## Decision 13: checkout、dry-run、apply和rebase闭环

AI编辑流程：

```text
显式checkout
  -> 返回文档包路径与Clean/TreeDirty状态
  -> AI只读取相关catalog和editable文件
  -> AI直接修改JSON文件
  -> 文件保存不触发Unity工作
  -> 显式dry-run
  -> 返回整包documentHash、planned diff和诊断
  -> AI按诊断继续修改并重新dry-run
  -> 显式apply(expected documentHash)
  -> 事务Mutation + Validator + Save
  -> 从最终Unity树反向导出整个文档包
  -> local identity转stable identity
  -> Clean
  -> Character需要正式产物时显式精确Build
  -> 显式validate读取最终正式树
```

Conflict时：

```text
Unity与Document都变化
  -> dry-run/apply拒绝
  -> AI读取当前Unity差异摘要
  -> AI把需要保留的人工变化合入editable
  -> 显式rebase(confirm)
  -> 当前Unity成为新基线，AI目标正文保留
  -> DocumentDirty
  -> 重新dry-run
```

rebase不修改Unity资产、不build、不自动merge。

## Decision 14: 所有重操作只允许明确触发

以下事件不得触发checkout、reconcile、dry-run、apply、validate、compile、Program build或Projection build：

- JSON文件保存。
- Graph/Timeline/Inspector编辑。
- selection与focus变化。
- AssetDatabase refresh。
- domain reload。

`btsmtl.apply_document`只负责正式authoring事务、保存和反向文档发布。AI domain继续在自身事务内发布AIIntentProgram；Character Program与Projection必须在apply成功后通过精确Definition的独立Build生命周期显式发布。布局修改本身不得导致Program/Projection重建。

## Failure Semantics

- root缺失或类型不符：失败，不扫描替代资产。
- 文档包缺失：只有checkout可创建。
- v1单文件或旧Patch输入：失败，不迁移、不转换。
- manifest、sync或文件清单非法：失败。
- context被修改：`readonly_context_modified`。
- editable kind未形成完整能力闭包：`authoring_capability_incomplete`。
- unknown kind、property、port、anchor、owner或reference：reconcile前失败。
- TreeDirty或Conflict：dry-run/apply失败。
- document hash变化：apply前失败。
- transaction owner不完整：mutation前失败。
- handler或Validator失败：回滚全部Unity owner。
- AI generated product发布失败：回滚并保持DocumentDirty。
- Character Build失败：保持已保存authoring与stale generated product，返回Target与Projection诊断，不伪装为Document apply失败。
- 最终文档包反向发布失败：不得报告Clean或完整成功。

## Migration

1. 冻结当前单文件v1，不再补字段或兼容逻辑。
2. 建立v2 manifest、分片schema、strict codec、package hash与目录Store。
3. 将NodeEmitter registry收敛为唯一authoring capability catalog。
4. 改写Graph/Timeline/AI exporter，输出稀疏模型、anchor和context catalog。
5. 改写Reconciler，补齐Flow/Property Edge及全部目标状态CRUD。
6. 将application service切换为整包读取、hash和反向发布。
7. 用五个独立生命周期MCP工具替换action multiplexer。
8. 更新Window、技能、current-contract、project与Corin active change。
9. 删除单文件v1、旧Store、旧tool、Patch/Macro/bootstrap和全部兼容入口。

迁移结束时只允许：

```text
JSON document package -> Reconciler -> Mutation Plan -> formal authoring APIs
```

## Tradeoffs

### 多文件包与单文件

多文件包让AI按Graph或Timeline选择性读取，显著降低局部修改上下文。代价是Store、canonical hash和原子发布复杂度增加，因此同步粒度必须继续保持整包，不能再引入文件级状态。

### 稀疏规范schema与Unity对象快照

稀疏schema更接近AI实际要组合的Graph，并隔离C#重命名和序列化细节。代价是需要维护正式kind/property/port catalog，但这个catalog同时解决exporter与creator能力漂移。

### 直接文件编辑与Node级工具

直接文件编辑让AI一次描述完整目标状态，Reconciler统一决定顺序和最小Mutation。Node级工具更适合交互式单步操控，但会增加工具选择、往返、部分失败和中间状态；本业务的核心是生成整张图，因此不采用。

### 五个生命周期工具与单action工具

五个工具让模型、客户端和权限层清楚识别只读、破坏性、必需参数和输出schema。代价是工具数从1变为5，但它仍是固定小集合，不会随Graph能力增长。

### Unity树继续是正式真相

保留现有Graph/Timeline人工编辑与Program编译链。代价是JSON包是工作副本而不是实时镜像，必须显式处理TreeDirty和Conflict。

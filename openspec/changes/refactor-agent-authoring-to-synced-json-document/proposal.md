# Change: 将 Agent Authoring 收敛为可直接编辑的同步 JSON 文档包

## Why

当前工作区已经实现`Document checkout -> Reconciler -> Mutation Plan -> handler -> Validator`主链，但AI面对的外部格式仍然是一次性导出的完整Unity对象投影：

- 当前Character Document为4,349,874字节、99,821行，包含143个Graph、830个Node、159条Flow Edge和454条Property Edge。
- 仅压缩后的`editable`仍约2.19 MB，其中Graph约1.08 MB、Timeline约0.94 MB；只读context只有约20 KB，体积问题主要来自可编辑模型本身。
- Node重复输出C#`typeName`、显示名、完整PropertyPort定义、空`assetReferences`、空`graphReferences`及多个与当前Node无关的nullable字段。
- 系统拥有的Root、Enter、Exit、Any、OnEnter、OnExit和Condition Result节点被当作普通可编辑Node输出，AI必须理解内部节点类型才能连接Graph。
- Graph逻辑、画布位置、Timeline结构和大量Curve key混在同一个文件中，AI修改一个局部Graph也必须面对整份Document。
- exporter可以输出的Node类型多于`AgentNodeEmitterRegistry`能够创建和配置的类型，当前外部合同没有形成“可导出即可完整往返”的闭包。
- `AgentGraphLinkMutationHandler`已有Flow删除、Flow连接和Property连接，但缺少Property Edge删除等完整目标状态降低能力。
- 当前MCP把五种生命周期混在`manage_btsmtl_agent_authoring(action, ...)`中。不同action拥有不同必需参数、只读性和破坏性，却共享一个宽输入schema。

用户需要的不是把节点级命令换一层包装，而是让AI直接编辑一份适合Graph组合的JSON表示。BTSMTL MCP只负责把文件工作区与Unity正式authoring安全地同步，不负责代替文件编辑器，也不为每种Node增加工具。

## What Changes

- 将单文件`btsmtl-agent-authoring-document.v1`替换为`btsmtl-agent-authoring-document.v2`逻辑文档包；物理形式为确定性`.btsmtl/`目录。
- 文档包按`editable/graphs`、`editable/timelines`、领域配置和`context`拆分JSON；AI可以只读取和修改相关文件，但同步、dry-run、apply和Conflict仍以整包为唯一提交单元。
- Graph改用稀疏、规范、声明式authoring语言：
  - Node只输出稳定`kind`和当前节点真正有意义的typed`properties`。
  - Edge直接表达完整目标拓扑，不暴露`link_*`、`delete_*`或handler顺序。
  - 端口使用catalog中的稳定逻辑key，不在每个Node重复输出端口元数据。
  - 系统节点改为`@root`、`@enter`、`@exit`、`@any`、`@onEnter`、`@onExit`、`@timelineEnter`和`@result`等只读anchor。
  - 新Graph必须声明正式owner与slot，不允许创建无归属Graph。
  - 已有实体保持stable identity；新实体使用`local:<meaningful-id>`，apply后由反向导出替换为正式identity。
- Graph逻辑与画布布局分离为`graph.json`和`layout.json`。AI不关心布局时可以不修改layout；缺少新Node位置时只执行唯一确定性自动布局规则。
- Timeline结构与Curve payload分离为`timeline.json`和`curves.json`；默认值和空字段不重复输出，Curve key只保留影响正式语义的字段。
- checkout生成紧凑只读`node-catalog.json`、`graph-kinds.json`、`asset-catalog.json`和`dependencies.json`。只用于Validator或Compiler、但AI不需要的Unity内部数据不序列化进文档包。
- 建立“可导出即可往返”的能力闭包：任何进入editable的Node kind、property、port和Graph kind都必须来自同一authoring capability catalog，并同时被exporter、Reconciler、handler和Validator支持；否则checkout明确失败。
- `AgentDocumentReconciler`按整包目标状态生成唯一immutable `AgentMutationPlan`，补齐Node、Flow Edge、Property Edge、Graph reference、条件、Timeline与领域配置的创建、更新和删除闭包。
- 删除单个`manage_btsmtl_agent_authoring`多action工具，改为五个固定生命周期工具：
  - `btsmtl.checkout_document`
  - `btsmtl.rebase_document`
  - `btsmtl.dry_run_document`
  - `btsmtl.apply_document`
  - `btsmtl.validate`
- 每个工具使用独立严格input/output schema、MCP行为annotations和结构化执行错误；返回绝对文档包路径、hash、状态、diff摘要和诊断，不嵌入完整JSON正文。
- AI通过宿主现有的通用文件读写能力直接编辑文档包。BTSMTL不新增Node级、Edge级、Timeline级或任意JSON patch MCP工具。
- 保留`Clean`、`TreeDirty`、`DocumentDirty`和`Conflict`四态脏标记。文件保存只在下一次显式生命周期调用时被发现，不触发compile、build、apply或文件监听。
- 文档包以规范相对路径和各文件canonical hash计算整包hash。不存在文件级apply、文件级Conflict或文件级基线。
- apply成功后从最终Unity树反向导出整个文档包，新local identity转为stable identity，状态回到`Clean`。
- 激进删除单文件v1、v15-v17 Snapshot/Patch、operation catalog、Macro、bootstrap、旧MCP action、兼容reader、converter、alias、watcher和临时桥接。

## Scope

### In Scope

- 已有合法`CharacterPipelineDefinition`与`AIControllerDefinition`的按需checkout。
- Character与AI Graph的完整声明式组合，包括Node、Flow Edge、Property Edge、Graph reference、Condition和owner关系。
- StateMachine、State body、ConditionRule、TreeClip与Timeline inline/shared ownership。
- Blackboard、Action、Input binding、MotionWarp、Marker、registered Curve Channel、AI Perception、Observation、Memory和Character intent binding。
- 文档包v2、strict multi-file codec、canonical package hash、Document Store和四态同步。
- 统一Node/Graph authoring capability catalog及其checkout只读投影。
- Document Reconciler与内部Mutation链的完整目标状态闭包。
- 五个固定生命周期MCP工具和同一Editor Window application service。
- `btsmtl-agent-authoring`技能、current-contract、`openspec/project.md`和`add-corin-training-ai-demo`工作流同步。

### Out of Scope

- Node级、Edge级、字段级、Timeline级或任意JSON patch MCP工具。
- 让MCP代替宿主通用文件读写能力。
- 运行时LLM、运行时读取文档包或运行时Graph解释。
- 自动文件watcher、自动checkout、自动dry-run、自动apply、自动compile或自动build。
- 文件级dirty、文件级apply、文件级Conflict、自动三方merge或后台同步进程。
- 创建不存在的Character或AI Definition根资产。
- Presentation Profile、Pose Graph、Blend、Rig、Virtual Bone、Foot Analysis generated data和Body Motion Profile写入。
- 把JSON文档包提升为Unity正式authoring真相或版本控制资产。

## Impact

### Specs

- 新增并安装`btsmtl-agent-authoring-document-sync`的文档包v2合同。
- 修改`agent-character-controller-synthesis`。
- 修改`agent-ai-controller-synthesis`。
- 修改`btsmtl-agent-authoring-mcp-bridge`。
- `btsmtl-graph-core`继续保持Unity侧唯一Graph数据与正式编辑API；本变更只定义Agent外部规范投影，不建立第二套Graph runtime或Unity序列化模型。

### Code

- `AgentAuthoringDocumentModels`拆为manifest、editable分片、context catalog与sync基线模型。
- `AgentAuthoringDocumentStore`从单文件/sidecar原子写入改为目录级staging、规范文件清单和整包原子发布。
- `AgentAuthoringDocumentCodec`改为逐文件strict parser、canonical writer和package hash。
- `AgentAuthoringDocumentExporter`、Character/AI Snapshot exporter与live revision/context hash计算。
- `AgentNodeEmitterRegistry`提升为唯一Node/Graph authoring capability catalog。
- `AgentDocumentReconciler`、`AgentMutationPlanner`与Graph/Timeline/AI handler闭包。
- `AgentGraphLinkMutationHandler`补齐Property Edge删除和全部目标状态重接。
- `AgentAuthoringDocumentApplicationService`继续拥有checkout、rebase、dry-run、apply、validate和事务。
- `ManageBtsmtlAgentAuthoringMcpTool`删除，由五个薄MCP工具替代。
- `AgentCharacterControllerSynthesisWindow`只显示文档包、状态、diff、诊断和显式生命周期按钮。

### Active Change关系

- 本change继续作为现有Agent重构的唯一计划，不新建平行JSON、Graph MCP或Patch change。
- `add-corin-training-ai-demo`必须在本change安装后直接使用文档包v2，不得继续使用v17 Snapshot/Patch或bootstrap。
- 前置动画change只扩大只读context catalog，不扩大Agent对Presentation的写权限。

## Breaking Changes

- 单个`.btsmtl.json`与`.sync`被整个`.btsmtl/`文档包替换。
- `btsmtl-agent-authoring-document.v1`被v2替换，不提供迁移reader或双写。
- Node不再输出C#`typeName`、`nodeTypeDisplayName`、重复PropertyPort定义和无意义空字段。
- 系统节点不再作为普通editable Node出现，只能通过保留anchor连接。
- Node kind与Graph kind不可原地改变；类型变化必须显式删除旧实体并以新local identity创建新实体。
- 单个`manage_btsmtl_agent_authoring`工具及`action`参数被删除。
- AI不再通过BTSMTL MCP提交JSON正文或局部操作，只直接修改文档包文件。
- apply的hash锁定从单文件semantic hash改为整包document hash。

## Current Spec Comparison

- current `btsmtl-agent-authoring-mcp-bridge`要求一个`manage_btsmtl_agent_authoring`工具并仍描述v17 Snapshot/Patch、bootstrap和`patch_json`。本change用五个固定生命周期工具完整替换，保留统一service和事务，不保留旧tool alias。
- current `agent-character-controller-synthesis`把AI-facing结构建立在宽Snapshot/Patch DTO上。现有change虽已改为Document v1，但当前4.35 MB/近10万行输出仍复制大量Unity/C#投影。本次改为稀疏规范Graph语言和多文件逻辑Document。
- current `agent-ai-controller-synthesis`仍要求v17 Snapshot/Patch和AI bootstrap。本change改为已有root的文档包v2工作流，删除bootstrap与旧schema。
- current `btsmtl-graph-core`要求`BaseGraph`拥有唯一Node/Edge集合、正式创建/连接/断开API和stable authoring identity。本change完全复用这些约束；JSON只是在apply前的目标工作副本。
- `openspec/project.md`已把Agent现状写成Document checkout与五种生命周期，但仍称Document v1且未描述多文件包和五个独立MCP工具；实施收口时必须更新。
- 工作区已经实现单文件Document、Reconciler、Mutation命名和四态同步，因此本change不是重新推翻后半段，而是替换外部schema、Store、catalog闭包和MCP表面。

## Success Criteria

- AI只读取相关Graph/Timeline文件即可完成局部编辑，不必加载近10万行单文件。
- AI能通过声明Node、Edge、Graph owner与Timeline目标状态完整拼装Graph，不调用任何节点级BTSMTL工具。
- editable Graph中不出现C#类型名、重复端口元数据、系统Node对象和与当前kind无关的空字段。
- checkout输出的每个editable kind都能被同一catalog完整创建、配置、连接、删除和反向导出。
- AI保存JSON后Unity树、Program和Projection不变化，只在下一次显式调用时推导`DocumentDirty`。
- dry-run以整包hash建立唯一Mutation Plan；apply只消费同一整包hash。
- apply成功后整个文档包从最终树规范化，新identity稳定，状态回到`Clean`。
- 五个MCP工具的schema和安全语义清楚，工具数量不随Node种类增加。
- 旧单文件Document、Patch、Macro、bootstrap、action multiplexer和兼容路径全部删除。

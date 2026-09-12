# Skill authoring 数据模型统一实施记录

## 2026-09-13 r2规划入口

当前执行范围以[proposal.md](proposal.md)、[design.md](design.md)和[tasks.md](tasks.md)为准。下方原句保留规划阶段语境；用户随后授权按r2开始实现。共同参数/规则与共享Capability归本任务，两个作者MCP、JSON binding和Agent协议退役归C# authoring，FSM与事件图业务由各自Owner负责。

下方既有实施记录保留其记录时语境，其中v8包重建、checkout/dry-run/apply等待和协议发布已撤出r2任务，不再按旧“未完成”段继续实施。TransferPayload、真实端口、普通步骤和已正确迁移结果保持有效；协议退役不回退这些业务代码，也不丢弃失败记录。

本轮读取期间新增了2907713a5的旧Step DTO/死路径清理记录，原文保留。旧tasks 2.4未完成与5.3已勾选的记录也同时保存，不能将任一记录扩大为r2全量共同定义已经完成。

## 2026-09-13 r2执行进度

已完成共享字段与端口合同的第一段实现：

- `BtsmtlSkillCapabilityCatalog.cs` 为Skill字段提供当前typed读取、默认值、约束、引用目标和稳定identity入口；不经过JObject、AgentPackage或第二份节点快照。
- `BtsmtlSkillGraphAuthoringMetadata.cs` 暴露能力、字段、引用、配置绑定和正式字段读取入口，Skill compiler从该入口读取叶节点参数。
- 原业务节点与FlowCanvas节点共享输入身份、ActionContext、ActionWindow、CanActivate目标快照、生命周期、Gameplay、Blackboard和Locomotion authoring合同；Locomotion默认值与组合校验集中到同一业务规则。
- 固定、条件和动态端口继续由共享Capability与唯一`GraphAuthoringNodePortShapeProjector`投影；已有TransferPayload、真实端口和普通组合步骤未被改写。

当前已完成tasks 1.1、1.2、1.3、2.3、3.1、3.2。2.1、2.2和3.3仍未完成：已确认的Skill/原业务同义合同和Skill消费方已经接通，但原节点与FlowCanvas的完整字段对照、原节点目录/人工入口的全量改接仍需继续；并行Native FSM的Inspector改动只保留在工作树，不作为本记录提交范围。4.1至4.3仍按文档保留未完成状态。

## 2026-09-13 r2字段与引用链收口

本轮把“字段身份”和“引用目标”分成两个正式读取结果：

- `BtsmtlSkillGraphAuthoringMetadata.ReadField`返回当前节点的typed业务值；Timeline、Skill状态机和状态主体返回真实对象，不把资产对象硬转成编译器所需的字符串。
- `ReadIdentity`专门把同一typed值降为稳定identity，Skill compiler只从该入口取得Timeline和图引用ID；`ReadReferences`与`ReadGraphReferences`返回实际目标、identity和缺失状态。
- Timeline节点的TimelineAsset引用已登记到正式metadata，Inspector通过引用入口读取真实资产；字段目录、Inspector和compiler不再各读一套Timeline字段。
- Skill字段、节点引用和图引用按稳定fieldId排序；provider owner、Character State fieldId、Blackboard declaration/owner的必填事实进入共享字段约束。

本轮补齐的业务配置入口包括：原生Gameplay Tag、Tag Query、Attribute、Apply/Remove Gameplay Effect节点的`ConfigureAuthoring`，以及原生与Skill Action Window、Gameplay节点共用的合法性规则。Locomotion两类节点的位移模式、执行模式和持续时间默认值也由同一业务规则提供。

本轮没有修改`BtsmtlSkillNodeAuthoringBinding.cs`、`TimelineAuthoringClipBinding.cs`、JSON/Agent协议、FSM状态迁移或事件图路径。C# authoring负责的代码输出与JSON退役只消费本轮提供的typed metadata/API，不由本change新增第二套模型。

## r2前任务状态存档

本表保存原任务文字和状态，不是新的执行清单；当前tasks只列r2剩余实现。

| 原编号 | 原状态 | 原任务文字 |
|---|---|---|
| 1.1 | 原记录已勾选 | 确认状态机转移同时存在 Step 与 Edge 两份条件、priority、abortPolicy，固定 Edge 为唯一转移来源。 |
| 1.2 | 原记录已勾选 | 确认普通 Sequence、Selector、Parallel 的 steps 是另一种组合语义，保留在普通节点 properties 中。 |
| 1.3 | 原记录已勾选 | 固定 Corin 状态机的节点、边、条件图、owner、UID和并列顺序基线，避开其它未提交资产。 |
| 2.1 | 原记录已勾选 | 建立 `BtsmtlSkillTransferPayload`，统一条件图、priority、abortPolicy和order的类型与写入口。 |
| 2.2 | 原记录已勾选 | 状态 Enter、Any、State、Exit改为固定逻辑端口；Transfer输出容量和StateIn输入规则由正式Closure校验。 |
| 2.3 | 原记录已勾选 | 接入共享 `GraphAuthoringCapabilityCatalog` 与唯一 `GraphAuthoringNodePortShapeProjector`，删除anchor动态steps投影。 |
| 2.4 | 原记录未勾选 | 完成原业务节点与FlowCanvas同义节点的全量字段、默认值、校验、引用和编译对照；未确认同义的能力不合并。 |
| 3.1 | 原记录已勾选 | Closure、ClosureIndex、GraphCopy、Exporter、Validator、Applier和Occurrence改为读取Edge transfer payload。 |
| 3.2 | 原记录已勾选 | ConditionRule owner统一为`kind=edge`、`edgeId`、`referenceKey=condition`，并纳入可达性、循环和owner校验。 |
| 3.3 | 原记录已勾选 | 删除状态机anchor.steps的DTO、导出、应用、投影、引用闭包和循环检查路径；普通节点properties.steps保留。 |
| 3.4 | 原记录未勾选 | 等并行 Native FSM/Timeline authoring闭包稳定后，完成当前正式编译和SourceMap对账，不覆盖其未提交改动。 |
| 4.1 | 原记录已勾选 | 将唯一 `AgentAuthoringSchema.Version`、Report、Codec、Store、作者窗口和五个MCP工具说明切换为 v8，并严格拒绝v7及更早包。 |
| 4.2 | 原记录已勾选 | 删除独立 `BtsmtlSkillTransferConnectionMigrator`；删除前代码由Git提交历史保留，不建立旁路迁移入口。 |
| 4.3 | 原记录已勾选 | 删除被忽略的旧 v4/v5/v7 package目录，保留正式Unity资产和Git历史。 |
| 4.4 | 原记录未勾选 | 在当前 authoring闭包可导出后，通过正式checkout生成v8 package，核对manifest/sync、完整闭包、owner/order和hash。 |
| 4.5 | 原记录未勾选 | 对v8执行无修改dry-run、validate、重新checkout；需要改资产时才使用同hash apply，并交付Clean结果。 |
| 5.1 | 原记录已勾选 | 重写本change的proposal、design和implementation，移除与实际Edge/v8实现矛盾的旧口径。 |
| 5.2 | 原记录已勾选 | 对照并更新现行 `openspec/specs/`、`openspec/project.md` 与 `btsmtl-agent-authoring` 技能合同，保留非本change场景和历史archive。 |
| 5.3 | 原记录已勾选 | 完成原业务/FlowCanvas定义清单、源码路径、删除项和业务行为对照；不以Agent往返成功代替模型统一证明。 |
| 5.4 | 原记录未勾选 | 汇总小步提交、正式Unity实例、checkout/dry-run/apply/validate结果与未提交外部改动边界。 |

## 既有实施记录

以下“当前”“已完成”“验证记录”和“未完成”均保留原记录含义；旧协议后续动作已由上方r2入口替代。

## 当前状态

核心数据模型和代码链已经落地，正式 v8 package 重建尚未完成。当前 Unity 工程有其它任务留下的未提交 Native FSM、Timeline 和生成物改动；正式 checkout 已正确拒绝不完整的当前 authoring 闭包，没有绕过它们生成假包。

## 已完成

### 状态机转移唯一来源

- `BtsmtlSkillFlowConnection` 只序列化 `BtsmtlSkillTransferPayload`。
- Payload集中保存 `condition`、`priority`、`abortPolicy` 和 `order`。
- 状态 Enter、Any、State 和 Exit 使用固定逻辑端点；状态机边从 `Transfer` 到 `StateIn`。
- 同一来源的转移按显式 `order`排序并校验唯一性，不能按UID、位置或字典顺序推断。
- 普通 Sequence、Selector、Parallel 仍从节点 `properties.steps` 读取自身分支顺序；它们不再被误当成状态机转移。

### 统一消费者

以下链路已经改为读取同一 Edge 数据：

`GraphClosure → ClosureIndex → GraphCopy → Document Exporter → Package Validator → Mutation Applier → Skill Occurrence/Compiler`

状态机条件图的 owner 使用 `kind=edge`、`graphId`、`nodeId`、`edgeId` 和 `referenceKey=condition`。旧的 anchor.steps 已从 package DTO、Exporter、Projection、Applier、Validator、Closure 和循环检查中删除。

- 旧的 `AgentPackageSkillFlowStep` DTO、`ExportStep` 和无调用方的 `ValidateSteps` 已删除；普通组合仍由 `BtsmtlSkillStepPort` 和 `properties.steps` 处理。

### Document v8代码合同

- `AgentAuthoringSchema.Version` 已切换为 `btsmtl-agent-authoring-document.v8`。
- Codec、Store、Report、作者窗口、Presentation 错误信息和五个 MCP 生命周期工具说明均已统一到 v8。
- 旧 v7 及更早包不会被兼容读取；Store 会要求重新 checkout。
- 独立 `BtsmtlSkillTransferConnectionMigrator` 已删除，Git提交 `a1738ec56` 保留删除前历史。

### 源码入口与删除边界

| 业务责任 | 正式入口 | 当前处理 |
|---|---|---|
| FlowCanvas拓扑与状态结构 | `BtsmtlSkillFlowGraph.cs`、`BtsmtlSkillFlowNode.cs`、`BtsmtlSkillStructuralFlowNodes.cs` | 保留正式Graph、State和固定逻辑端口 |
| 状态机转移数据 | `BtsmtlSkillFlowConnection.cs` 的 `BtsmtlSkillTransferPayload` | Edge唯一保存condition、priority、abortPolicy和order |
| 节点定义与Port Shape | `BtsmtlSkillCapabilityCatalog.cs`、`BtsmtlSkillGraphAuthoringMetadata.cs` | 由Capability和唯一投影入口提供 |
| 引用闭包与复制 | `BtsmtlSkillGraphClosure.cs`、`BtsmtlSkillGraphClosureIndex.cs`、`BtsmtlSkillGraphCopy.cs` | 沿正式Edge/Step/Node引用闭合；两种条件owner不互读 |
| Document读写与校验 | `AgentSkillFlowDocumentModels.cs`、`AgentSkillFlowDocumentExporter.cs`、`AgentSkillFlowDocumentValidator.cs`、`BtsmtlSkillGraphAuthoringApplier.cs` | 统一v8 Graph/Edge/owner/order链 |
| 编译消费与SourceMap | `BtsmtlSkillGraphOccurrence.cs`、`BtsmtlSkillGraphFlowEmitter.cs`、`BtsmtlSkillGraphCompiler.cs` | 状态机按显式Edge order发射Transfer控制流 |
| 已删除路径 | `BtsmtlSkillTransferConnectionMigrator`、`BtsmtlSkillLegacyMigrationWorkflow`、状态机anchor/State旧steps | 不再有迁移菜单、旁路转换或状态机Step副本 |
| 保留的相似语义 | `BtsmtlSkillCompositeFlowNode.Steps` 与 Document `properties.steps` | 仅服务Sequence/Selector/Parallel，不属于状态机Transition |

### 正式资产处理

之前通过正式 Document JSON checkout/dry-run/apply 已把当前 Corin 状态机的 20 条转移边写成 Edge Payload，并确认旧状态 step 节点为 0、转移端点为 `Transfer → StateIn`。后续 v8切换前已删除被忽略的旧 v4/v5/v7 package目录，准备由正式 checkout重新生成唯一 v8包。

## 验证记录

- Runtime、Editor和`BTSMTL.TreeDesigner.csproj`的最新增量编译均为0错误；编译使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`，每次结束后均执行了`dotnet build-server shutdown`。当前只剩既有警告：Runtime 1条、TreeDesigner 16条、Editor 32条，未出现Skill字段/引用链错误。
- 旧记录曾有并行Pose `CharacterPoseGraphWorkspace.cs`的`SetEditorAnimationVariables`错误；最新Editor编译已不再复现该错误。并行Pose/FSM/EventGraph工作树仍不作为本change的实现证据。
- Unity 正确实例为 `3C_Client@e852139597e42532`；没有向并行测试实例 apply。
- v8代码加载后，MCP tool description 已显示 Document v8。
- 当前最新正式 `checkout_document` 没有生成 package，返回真实 authoring错误：当前未提交 Native FSM/相关节点闭包存在缺失，且当前目录有并行 Timeline 改动；这是正确阻断。

## 未完成

1. 等待并行 Native FSM/Timeline 改动形成可编译、可闭包的正式 authoring，不能覆盖或代替其未提交内容。
2. 在 Unity authoring可完整导出后，重新执行 v8 `checkout_document`，核对manifest/sync schema、完整Graph闭包、Edge owner/order和hash。
3. 对新 v8包执行无修改 `dry_run_document`、`validate` 和重新 checkout；必要时才执行同hash `apply_document`。
4. 对本 change 的现行 spec、`openspec/project.md` 和 `btsmtl-agent-authoring` 技能合同完成 v8对账；历史 archive只保留追溯，不作为当前完成证明。
5. 清理当前工作区中明确属于本 change的剩余重复节点定义；不触碰其它 active task 的未提交文件。

本轮复核确认并行 Native FSM 改动中的 `BtsmtlSkillNativeConnection` 已读取共享 `BtsmtlSkillTransferPayload`，未再保留第二份转移字段；未提交的 `BtsmtlSkillLegacyMigrationWorkflow` 及菜单入口已删除。Native FSM 本身仍属于并行未提交改动，不能作为 FlowCanvas 正式拓扑统一完成的证据，也不能在本 change 中继续扩展第二条正式 authoring 路径。

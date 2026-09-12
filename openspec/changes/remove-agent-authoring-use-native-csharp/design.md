## Context

动机见 [proposal.md](proposal.md)。r2（2026-09-13）按用户最终选择收敛：**两个显式作者MCP：从资产导出C#，执行C#创建并保存资产。人工编辑资产不会生成代码。** 当前图直接作为导出输入，导出不读取旧源码。

原r1的“Unity资产永久作为唯一来源”被替代。讨论过的源码解析、语法节点定位、增量写回、增长日志、源码Undo和自动同步不进入本次实现。两个MCP各自完成明确的输出写入；人工编辑保存机制不扩展。

已核对的正式入口包括：

| 代码 | 现有能力 |
|---|---|
| `BtsmtlSkillFlowGraph.AddNode / CreatePortConnection` | 原生图创建及正式节点/端口约束 |
| `BtsmtlSkillFlowEditorMutation` | 单图编辑事务、序列化、闭包校验与私有资产清理 |
| `BtsmtlSkillGraphAssetFactory` | 根/私有/共享对象创建与owner规则，部分入口要求已保存owner |
| `TimelineData.AddTrack / AddClip / AddSection` | 带catalog约束的正式Timeline创建 |
| `BtsmtlSlateTimelineProjection` | 临时Slate界面投影，最终回写TimelineData |
| `BtsmtlSkillNodeAuthoringBinding`、`TimelineAuthoringClipBinding` | 目前仍以JSON读写配置；可复用真实字段读取和正式Configure/Set调用知识，不保留JSON中转 |
| `CharacterPoseNodeDefinitionModule`、正式Presentation Mutation | Pose字段、端口、创建/配置与局部业务约束 |

这些为静态代码证据，不是Unity编译或运行证明。完整依赖清点由实施记录保存，不能把目录改名当成删除协议。

## Goals / Non-Goals

**Goals:**

- 从一个明确的正式图或Timeline及其拥有的完整闭包，输出可编译、可执行的C#创建代码。
- 执行输出代码可重建相同的节点、配置、连接、时间、曲线、布局和合法引用，不依赖旧生成资产作为内容来源。
- 人工修改后可显式完整导出新代码，代码只描述当前结构，不留下修改历史；手动修改或保存资产本身不会导出。
- 删除Agent协议与重复校验，生成代码与人工编辑都调用原正式业务API。

**Non-Goals:**

- 不实现C#源码解析器、语法节点定位、任意源码逆向、增量源码编辑、事件日志或通用Reconciler。
- 不保留原始创建算法的循环、条件、局部函数、变量命名、注释或手工排版；图到代码输出统一格式。
- 不把Roslyn作为必需依赖，不新增C#解释器、第二领域DTO/节点模型或运行时规则表。
- 不设计新的保存按钮、自动导出时机、源码Undo、文件监听或重载后界面恢复。现有人工编辑与Undo继续使用。
- 不改变Graph、Timeline、Pose的运行/编译语义，不替换Slate数据源，不新增测试。

## Decisions

### D0. 公共机制与共享文件的唯一归属

本次按用户广播`2026-09-13-authoring-r2-plan`及协调提案`2026-09-13-eventgraph-authoring-r2`补齐分工。已撤销“资产永久为唯一来源”“专用MCP为零”和“新建全角色统一事务服务”的旧建议。

| 内容或共享文件 | 唯一负责方 | 本任务的动作 |
|---|---|---|
| 两个作者MCP、最小生成入口、通用输出/生成与领域扩展合同、旧Agent公共协议 | 本任务 | 实现公共机制与退役计划，不接管各领域规则 |
| `BtsmtlSkillNodeAuthoringBinding.cs`、`TimelineAuthoringClipBinding.cs` | 本任务 | 删除JSON适配，消费正式业务定义，不改正确参数/引用规则 |
| `BtsmtlSlateTimelineProjection.cs` | Timeline任务 | 本任务提供typed binding接口；UI接入由Timeline任务修改，本任务不直接删除或重写该文件 |
| `EventGraphAuthoringDocument.cs`、`HostEventGraph.cs`、`HostEventGraphEditorMutation.cs` | 事件图任务 | 消费该任务提供的正式读取/创建/保存和输出适配；不按Document名字删除文件 |
| `CharacterPoseGraphAuthoringAdapter.cs`、Pose Mutation与输入消费文件 | Pose任务 | 消费领域API/适配；本任务不重写Pose输入、变量或运行消费逻辑 |
| `BtsmtlSkillGraphAuthoringApplier.cs`内有效FSM操作迁出 | FSM任务 | 依赖FSM任务完成业务操作迁出后，核对仅剩协议及消费者，再按明确交付边界清理 |
| 共享Capability、字段与端口定义 | 数据统一任务 | 复用正式metadata和端口结果；本任务不新建或修改第二定义来源 |

共享文件的修改归属不因文件位于SkillDocument/PresentationDocument目录或包含Agent/Document命名而改变。未满足交付条件时记录具体依赖，不能整体删除这些目录或为绕过依赖复制一份业务实现。

事件图任务拥有EventGraph运行与变量业务，Pose任务拥有其输入消费。本任务只负责其正式内容能按公共扩展合同导出和重建的接入结果；运行时固定motor参数桥的替换/删除不是本任务目标。

### D1. 输入当前完整图，输出新的创建代码

导出输入为明确的根Graph或Timeline对象、所属Definition/owner上下文、目标生成入口名称与输出位置。输入不包含旧C#、Document或操作日志。

输出为完整的一份或按根/子图划分的若干C#源文件，以及明确的根输出和外部资源依赖。AI可以直接写并执行同样的正式API调用；导出器只是把当前已有内容表达为同类代码，不是生成代码执行前的强制中间层。

```text
C#编译并显式执行 → 正式业务API → Graph / Timeline
人工修改当前Graph / Timeline → 显式export_code → 新的C#创建代码
```

生成范围内，已导出的C#是可重建内容来源；生成资产可以替换、删除后重建。尚未导出的人工编辑仍只在当前对象中，不能声称已经保存进源码。只有显式调用export_code才更新代码；显式generate_assets以指定代码重建其声明范围，未导出的人工调整不会自动合并。用户应先显式导出希望保留的调整，系统不增加rebase或同步状态。

| 方案 | 业务收益 | 业务代价 |
|---|---|---|
| 当前图完整导出（采用） | 输入清楚，不必理解旧代码，输出总能整理成干净当前结构 | 不保留原创建算法和手写组织 |
| 定位并增量改旧C# | 可以保留作者代码组织 | 要维护源码到对象映射、语法限制及冲突处理 |
| 保存树的操作历史 | 容易记录单次变化 | 输出会积累已删除内容及历史操作，重放负担随编辑增长 |

### D2. 遍历对象和依赖，不沿执行连线递归输出

图可能有环、多处共享引用和系统节点，不能把它当成只访问一次子节点的普通树递归打印。

1. 从明确根收集其正式拥有的对象，按对象身份去重，区分内部生成对象与外部资源。
2. 按owner依赖先输出根、子图、状态机、Timeline、轨道等对象的创建，再创建节点和片段。工厂已有的系统入口复用，不重复创建。
3. 输出标量、枚举、布局、曲线和动态端口等配置，再解析内部跨对象引用。需要先存在对象才能配置的引用放在对应对象创建之后。
4. 所有端口形状明确后输出连线、状态转移及最终根绑定。

以上是代码输出顺序，不是持久化Mutation Plan。导出过程只需当前调用内的“对象到局部变量”字典和依赖集合；不保存另一份Graph或可编辑结构模型。

同一个共享子图只生成一次。共享但不属于本次根拥有范围的资产作为明确外部引用，不能因为可达就复制或删除。

### D3. 用正式读取与写入合同产生代码

| 内容 | 读取来源 | 输出 |
|---|---|---|
| 节点/FSM/Macro | 当前正式类型、配置、接口、系统入口与owner | 现有factory/AddNode及Configure/Set调用 |
| Blackboard/动态端口 | 正式声明、身份、类型与顺序 | 对应声明、配置与端口创建调用 |
| Edge/Transition | 实际端点、稳定端口及edge payload | 正式连接调用和条件/priority/order等配置 |
| Track/Clip/Section/Binding | TimelineData及其正式内容 | AddTrack/AddClip/AddSection及typed配置 |
| Curve | 正式完整关键帧、切线、权重、WeightedMode、wrap | 对应C#值构造与正式完整曲线配置 |
| 布局 | 正式layout owner与元素位置 | 作者布局配置，不发射runtime字段 |
| 资源 | 精确对象及其正式资产身份 | 明确类型的外部资源解析或入口参数 |

通用输出处理字符串转义、数值精度与非区域性格式、枚举、向量、数组和C#标识符。名称只用于可读变量名，重名由当前调用内的确定性命名解决，不作为业务identity。

新增部分只有：

- 导出遍历与依赖排序；
- 通用C#表达式/语句输出；
- 领域对象到正式配置调用的薄适配。

正式metadata已经表达的字段、引用和方法信息直接复用；缺少“如何读取并通过哪个方法恢复”的部分补在对应领域。普通节点不另写整套解析器；特殊Curve、Macro、动态端口由所属模块补充。不能从现有JSON先导出再翻译，也不能把整个Agent Exporter改名保留。

### D3.1 公共输出与领域薄扩展合同

通用机制由本任务提供，领域适配由D0对应任务提供或维护。扩展接收正式对象与只读导出上下文，直接向公共C#输出器写出本领域正式API调用；不先建立领域Snapshot/DTO，不把业务校验迁入公共输出器。

| 扩展责任 | 输入 | 输出及约束 |
|---|---|---|
| 识别与完整性 | 正式根/元素类型和领域合同 | 明确是否支持，缺字段/类型时定位对象并失败；一个对象不能由多个适配抢占 |
| 读取依赖与身份 | 正式对象、owner、图/节点/变量identity | 内部拥有对象与外部资源引用；只是当前导出调用的引用集合，不保存第二作者树 |
| 输出创建 | 对象正式类型、业务identity、owner变量 | 调用该领域已存在的创建API，系统入口按原工厂规则取得 |
| 输出配置与连接 | 正式字段值、端口形状、引用和公共对象变量映射 | 输出配置、内部引用和连线语句；不猜私有字段，不重复定义端口或业务约束 |
| 恢复根挂接 | 新生成根对象、明确的Profile/Definition等owner | 调用领域正式绑定API；不依赖旧生成子资产GUID，不扫描全项目猜消费者 |

公共上下文只拥有输出目标、对象到局部变量映射、依赖排序、完整性诊断及通用表达式写出；各领域保留正式对象读取、业务规则、owner、Undo和保存能力。生成服务通过最小入口合同协调已有API，不新增中央Validator或统一整角色事务。

EventGraph可作为同一扩展合同的领域提供者。其接口尚未接通或某项正式内容不支持时，export_code必须拒绝该根的完整导出并保留目标源码，不能省略EventGraph、变量或引用后报告成功；本任务不为通过导出而补造事件图runtime。

生成代码直接调用正式API，不输出私有字段名、Unity YAML或私有序列化路径。未知类型、无法输出的正式字段或引用必须报告精确对象和原因，不能默默省略后称为完整导出。

### D4. 生成范围与外部资源分开

本次新增导出目标是正式Graph/Timeline及其拥有的闭包，包括Skill FlowGraph、原生FSM、Macro、Blackboard、Timeline TreeClip子图以及正式Pose图的可达内部结构。EventGraph通过事件图任务提供的领域薄适配接入；本任务不提前实现其运行/变量业务，缺少正式输出支持时明确拒绝完整导出。

Definition、Profile、Rig、AnimationClip、外部共享图和分析产物如果不属于明确生成范围，就作为输入引用，不隐式复制其正文。原Document曾能修改的Profile/Curve/Control/Action等能力仍保留正式C# API，不等于导出一个图必须生成整个角色与全部素材。

生成入口返回明确根输出，并通过正式调用把它绑定回本次指定的owner。删除重建后必须恢复范围内引用及明确的外部消费挂接；不能只创建一份孤立Graph。范围外Prefab或Definition若需重新绑定，必须作为本次明确的生成目标参数，不全局扫描猜消费者。

业务图、节点、变量及其他正式元素的稳定identity写入C#；重建后必须保持这些逻辑身份。生成资产的Unity实例、物理GUID/local file ID可以变化；必须通过正式输出绑定策略明确恢复Profile/Definition等根挂接，不得使用旧生成子资产GUID找内部对象。实现若只支持原位更新，必须明确未完成删除重建，不能把它当成最终能力。

导出代码不应依赖当前生成子资产的GUID去找同一批待重建对象；内部引用使用本次生成的对象变量，只有真正外部输入才使用明确资源身份。

### D5. 完整重建与干净输出

再次执行完整生成入口时，只替换其明确拥有的输出范围，不向旧图不断追加。可以先校验外部输入，再按正式owner/API建立新内容；完成后更新本次明确的根绑定并清理旧生成内容。具体资源生命周期复用现有资产服务，不恢复整包Reconciler。

人工删除节点后，重新导出的代码不含该节点、配置和相关边；不输出“先创建再删除”。输出按稳定身份及显式业务顺序确定，不能为排序源码而改变Track/Clip顺序、FSM order、动态端口顺序或其它业务顺序。

成立的往返条件为：在相同外部资源和正式API版本下，导出当前结构G得到代码C，执行C得到G'，G与G'在节点类型、身份、配置、拓扑、owner、资源、曲线和布局上等价。C再导出应具有确定性。暂不承诺任意外部手写C#文件的格式或算法可逆。

原始素材、分析文件、源码和范围外资产不属于可清理的生成输出。不能把“资产可重建”解释成现在直接删除用户现有资产；首次迁移必须先获得完整代码及对应重建证据。

### D6. 校验、人工编辑和Build仍由原模块负责

Graph校验节点与端口，Timeline校验片段、channel、owner、曲线与引用，Pose校验自身局部和拓扑，Character编译器检查完整产品。导出器只增加输出完整性检查：是否认识对象、能否表达其字段及引用；不复制业务规则。

Agent中的schema/hash/sync/reconcile规则删除，重复业务校验删除，真正仅存在Agent的规则补到已有业务模块。正式整角色诊断直接调用现有编译器，不再套Agent Report。

人工操作继续走已有领域Undo和保存，Slate继续投影TimelineData。export_code读取完整输入后显式写源码，不dirty输入；generate_assets显式创建并保存生成资产。两者都不自动Build，不实现自动导出、源码Undo或每次鼠标操作写文件。

Character Build及其现有精确MCP独立保留；五个BTSMTL authoring MCP、Agent窗口及专属scheduler删除。两个新作者MCP只转发正式服务，不新增server、任意eval入口、Document生命周期或节点级工具。

### D6.1 两个显式MCP公共入口

| 工具 | 明确输入 | 动作与输出 |
|---|---|---|
| `btsmtl.export_code` | 精确`asset_path`、`definition_asset_path`、位于项目Editor代码目录的`output_code_path` | 读取该Graph/Timeline闭包，完整产生并写出C#；返回代码路径、生成入口类型、外部依赖和诊断，不修改或保存输入资产 |
| `btsmtl.generate_assets` | 精确`source_code_path`、`recipe_type`、`definition_asset_path`、`output_asset_path` | 执行对应已编译的正式创建入口，创建/替换指定生成范围、绑定根引用并保存；返回实际根路径、创建/替换/删除范围与保存结果，不写回源码 |

生成代码及AI手写创建代码使用同一个小型Editor C#入口合同：输入明确生成上下文，调用现有业务API，返回根输出。工具只接受实现该合同的精确类型，不接受任意方法名、C#正文、反射字段或Node/Edge操作参数。入口合同不拥有节点模型、字段规则或Agent Session。

source_code_path必须对应recipe_type的当前编译结果；代码未编译、编译失败或Unity忙时明确返回错误，不执行旧程序集里的同名类型。导出源码后发生正常Unity编译不自动调用generate_assets，调用方准备好后再次明确调用。

export_code覆盖的是调用方明确指定的完整目标代码文件，先完成对象/字段/引用输出检查再写入，失败不以半份源码替换已有文件。它不读取旧源码来做增量合并，不保留其任意算法组织。多文件输出须明确返回文件集合，不自动扫描清理其他源码。

两个MCP之外不新增validate、checkout、rebase、dry-run、apply、sync/status领域工具；诊断随当前操作返回。Play或切换Play期间拒绝执行，不刷新/构建。所有Unity MCP调用仍须显式unity_instance。

### D7. 删除边界

| 删除对象 | 仍需保留的能力 |
|---|---|
| AgentAuthoring内Document/Snapshot/Codec/Store/Exporter/Reconciler/Mutation/Session/Validator/Report | 现有正式业务创建、校验、资源与owner能力，不能整包搬家 |
| SkillDocument/PresentationDocument中已确认无消费者的协议DTO与对账适配 | D0共享文件及有效业务能力不在整目录删除范围；FSM操作由FSM任务迁出 |
| 节点/Timeline JSON binding | 本任务清理两份binding；Slate BuildClipProperties由Timeline任务改接同一typed入口 |
| Agent窗口、导航、旧五工具、专属scheduler | 正式Graph/Timeline/Profile编辑器、两个新显式作者MCP、Build与其他非Agent工具 |
| 无消费者的协议测试、asmdef依赖、工作包、skill说明 | 非Agent业务测试、源码、原始资源与有效领域合同 |

不改角色runtime、Semantic IR、Program/Projection ABI、Body Motion/Foot Analysis内部算法或TrainingEnemy资产。注册Curve和Action/FSM规则保留；移除协议不扩大业务编辑权限。

### D7.1 旧协议删除门槛与运行闭环分别记录

旧公共协议删除前，必须确认所有受影响作者调用者已脱离Agent，领域正式编辑、代码生成与资产保存链可用；共享文件里的有效业务操作已由负责领域承接。具体依赖未交付时只保留该删除项待完成，不复制旁路或保留兼容开关；满足门槛后旧协议、工具和无消费者代码直接删除。

在execution.md分别登记“作者协议退役门槛”和“外部运行依赖”。前者是本任务的完成条件；固定motor参数桥删除、动画变量运行推进与消费闭环属于事件图/Pose等任务，不能把它们当成同一个删除门槛，也不宣称本任务完成就代表运行闭环完成。

## Risks / Trade-offs

- [对象能创建但不能完整读出配置] → 补所属领域的读取/输出合同；缺字段明确失败，不用反射私有字段或JSON旁路。
- [生成代码只恢复拓扑，丢失曲线、动态端口、layout或资源] → 按完整输出清单核对，往返证据必须包含这些内容。
- [生成代码把旧生成资产当成外部输入] → 区分生成闭包与真实外部资源；删除旧输出后仍须能够重建。
- [原代码用循环或共享计算] → 图导出输出当前展开结果，不保证保留算法。需要算法复用仍可手写，下一次从图导出会规范化其结果。
- [保存/重载交互扩张] → 本次只提供明确调用，不新增编辑会话、源码Undo或自动同步机制。

## Spec Comparison

现行三份Agent专属规范仍要求目录包、Agent Validator和五工具，本change原有删除delta继续有效。r2的新导出能力写入 `character-csharp-authoring`，`graph-authoring-domain-framework`追加导出复用正式合同的要求。

原r1新能力中“资产是唯一来源”“不能全量重建”“只允许显式删除操作”与本次选择冲突，全部在r2替换：完整生成范围由源码决定，人工修改只有显式完整导出才成为新的可重建来源。旧协调稿“专用MCP为零”及“新建全角色统一事务服务”也不再适用。当前正式业务对象仍是Unity资产类型，不改变各领域compiler读取接口。

现行Foot Analysis场景曾要求apply内Build，与独立Build合同冲突；仍按独立Build处理。现行Pose Source Slot/直接资源及Loop等表述与近期代码有差异，实施须以已确认的当前领域合同输出真实内容；本change不借导出去回退Pose业务。

已知active change交叉点：
- 原生FSM：由FSM任务保留原生类型、edge、owner与编译，并迁出applier内有效FSM操作；本任务随后退役旧公共协议。
- attribute-driven/共同节点定义：共享Capability/端口由数据统一任务维护，本任务只消费其正式定义。
- 事件图：EventGraphAuthoringDocument、HostEventGraph、HostEventGraphEditorMutation及运行/变量由事件图任务收口，并提供薄输出适配；未就绪根拒绝完整导出。
- Pose：adapter、Mutation与输入消费由Pose任务维护，本任务只接入公共输出/生成合同，不承担固定motor桥替换。
- Slate UI：BtsmtlSlateTimelineProjection由Timeline任务修改UI接入，本任务提供新的typed binding，不另做Timeline界面。

这些是已知对比结果，不代表相关任务已被修改或冲突自动解决。实施前重读实际diff，具体冲突交用户裁决，不取消已正确的工作。

## Migration Plan

1. 核对基线、生成范围、领域输出覆盖和其他任务的真实冲突。
2. 复用正式API，清理本任务两份binding的JSON边界；领域读取/配置缺口由D0对应任务承接，记录依赖而不复制实现。
3. 实现对象遍历、依赖排序、通用C#输出和公共扩展合同，接入各领域提供的薄适配，通过export_code显式写出完整代码。
4. 实现明确生成范围的重建、根输出绑定和旧生成内容清理，通过generate_assets显式执行并保存；原始外部资源保持独立。
5. 以当前正式图为输入导出并经相同正式API重建，交付语义往返及确定性证据，不新增测试代码；不可执行时如实记录未验证。
6. 确认D7.1作者调用者脱离Agent且正式编辑/生成/保存可用后，删除旧协议、工具和无消费者代码；遵守共享文件例外，更新本change规范计划，不留下兼容链。
7. 维护执行记录、中文小步提交、适用编译与严格规范校验。本轮仅更新文档，不运行Unity。

图导出、生成、删除重建和两个显式MCP是同一完整交付，不能只提供示例打印器。手动编辑不生成代码，不因“自动同步更方便”而增加隐式执行。

## Evidence Contract

验收证据由本任务execution.md归集，静态检查与实际执行分别标注；不新增测试代码，不把用户手动验收写入tasks。

| 条件 | 必须保存的证据 |
|---|---|
| 完整导出 | 精确输入根/外部依赖、完整输出文件及各领域对象/字段覆盖；未知正式内容必须失败 |
| 删除输出后重建 | 旧生成范围不存在时，用同一源码及外部输入恢复结构；不能只展示在旧资产上增量修改 |
| 根引用与identity恢复 | 新旧图/节点/变量identity对照，内部引用绑定新对象，Profile/Definition明确挂接；无旧生成子资产GUID依赖 |
| 确定性 | 相同输入重复导出的代码比较；生成后再次导出的业务结构/顺序/Curve/layout等价对照 |
| 错误不覆盖源码 | 故意选择未支持正式内容时的对象/字段诊断及目标源码保持证据；不用默认内容补齐 |
| 人工编辑不导出 | 人工修改/保存前后源码保持及入口触发关系；只有显式export_code写源码 |
| 删除门槛 | 作者调用者无Agent依赖、正式编辑/生成/保存可用、共享文件业务迁出完成；motor桥运行闭环另列外部依赖 |

## Workflow Binding

- planning_revision: r2
- planning_date: 2026-09-13
- coordination_revision: r2-coordination-2026-09-13
- coordination_proposal: 2026-09-13-eventgraph-authoring-r2
- planning_status: ready_for_implementation_request
- planner_thread_id: 01a09634-fc59-7192-8cda-25fdd142b82d
- implementation_thread_id: 01a09635-2024-74b2-98b4-28c1e17d548b
- implementation_status: WAITING_FOR_PLANNING_DOCUMENT
- planning_document_paths: 当前目录proposal.md、design.md、specs/、tasks.md
- implementation_document_path: 当前目录execution.md
- direction_confirmed_by_user: true
- implementation_dispatched: false

本轮仅执行用户单向PLAN广播，更新本任务唯一规划文档；不修改业务代码、资产或其他任务文档，不回复广播或向任何窗口发送消息，不下发IMPLEMENT_FROM_DOCUMENT。后续实施仍以已绑定实现窗口及新的明确授权为准。

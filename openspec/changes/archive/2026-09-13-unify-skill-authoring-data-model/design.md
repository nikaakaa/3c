## Context

公共基线：[remove-agent-authoring-use-native-csharp/design.md](../2026-09-13-remove-agent-authoring-use-native-csharp/design.md) r2，协调提案2026-09-13-eventgraph-authoring-r2。规划阶段只修订本目录规划；执行阶段按本change tasks修改共享业务定义与metadata，不修改资产、其它任务文档或其它Owner路径。

本任务的目标仍是原业务节点与FlowCanvas共用参数、字段、端口、引用和业务规则。旧稿把FSM迁移、全局Document v8、Store/Reconciler和整包重建再次放进本任务，与用户已确认的收窄范围及r2冲突，本版撤出这些职责。

### 已有成果与本轮目标分开

| 内容 | 已有记录 | r2处理 |
|---|---|---|
| TransferPayload、固定真实端口、显式order与Edge引用消费 | af8dacc2e、1b55253d1等提交及implementation.md记录已落地 | 保留正确代码和业务值，不重做迁移；FSM任务维护其后续资产、转移和生命周期 |
| 普通Sequence/Selector/Parallel步骤 | 仍拥有合法组合分支语义 | 保留正式步骤数据，不因旧JSON路径出现steps而删除 |
| Agent v8代码、五工具、旧包处理 | implementation.md记录阶段实现及checkout未完成 | 只作历史；包重建、dry-run、apply和版本发布不再是当前任务 |
| 全量同义节点统一 | 旧tasks 2.4仍未勾选，5.3又声称已形成清单 | 保留两条原记录，不据此推断全部定义已共用；r2只列具体剩余实现 |
| 共享Capability与端口投影 | 已有正式目录和部分消费者接入 | 复用已正确能力，补足正式API缺口，不重新造目录或端口协议 |

以上是已记录源码与阶段事实，不是本轮执行验证或r2完成声明。完整原记录保留在[implementation.md](implementation.md)，不能再照其旧协议“未完成”段继续工作。

## Goals / Non-Goals

**Goals:**

- 同义原节点、FlowCanvas节点、人工UI与正式C#创建使用一份业务参数/规则定义；各实例值独立。
- 共享字段、固定/条件/动态端口和引用描述由本任务唯一维护，领域业务模块继续各自拥有正式能力。
- C#输出器能从现有正式定义读取参数与引用，表达对应正式创建/配置调用；缺少的读取/写入知识回到所属业务模块。
- 删除共享metadata中已无合法消费者的Agent专属字段及本任务范围内重复规则，原合法端口、组合步骤和已正确迁移成果保留。

**Non-Goals:**

- 不实现export_code、generate_assets、公共C#输出器、生成调度或旧Agent协议整体删除。
- 不修改BtsmtlSkillNodeAuthoringBinding.cs、TimelineAuthoringClipBinding.cs的JSON退役实现；这两份文件由C# authoring任务唯一修改。
- 不承担FSM资产/转移/order/生命周期及迁移，不承担事件执行、变量生产或Pose输入消费。
- 不建设中央Validator、Agent专属校验、全角色整包同步事务、Reconciler、源码解析或源码同步。
- 不新增测试或验证任务，不操作Unity，不把规划对齐称为实现完成。

## Decisions

### D1 共同定义归业务，框架适配只接入

参数类型、默认值、字段访问、合法值、逻辑端口和业务引用归原Motion、Input、Action及对应结构模块。仍有正式用途的原节点和FlowCanvas节点消费同义定义；各自只保存本实例的一份值，不把旧BaseNode整体嵌入FlowNode，也不创建第二份领域对象树。

两个框架的基类、端口对象和本地ID可以不同，通过明确映射进入共同业务合同。定义负责规则，适配器不再写另一份默认值或合法范围。确有语义差别的节点保持区别，不以显示名相同强行合并；零消费者旧适配在其引用迁出后删除。

业务取舍：共用完整定义有一次接入成本，但后续改规则只改所属业务模块；仅共用读取接口而继续分别保存字段规则，不能减少重复维护。

### D2 共享文件与领域文件只由一个Owner修改

| 文件或能力 | 唯一Owner | 本任务边界 |
|---|---|---|
| GraphAuthoringCapabilityCatalog.cs及共享字段/端口描述、NodePortShapeProjector | 本任务 | 维护通用定义与唯一投影，处理确属旧Agent协议的字段 |
| BtsmtlSkillCapabilityCatalog、Skill Graph metadata与共同参数模块 | 本任务的Skill数据范围 | 维护Skill共同规则与正式读取/配置入口，不接管其它领域的能力模块 |
| Pose、FSM、Timeline、EventGraph各自正式能力模块 | 各对应领域任务 | 本任务提供共享合同，各领域实现本领域投影与业务方法 |
| BtsmtlSkillNodeAuthoringBinding.cs、TimelineAuthoringClipBinding.cs | C# authoring任务 | 对方删除JSON边界并消费本任务提供的定义，本任务不重复编辑 |
| 两个作者MCP、通用代码输出、编译入口执行与Agent协议删除 | C# authoring任务 | 本任务只供给正式metadata/API，不能另建生成器或协议版本 |
| BtsmtlSkillGraphAuthoringApplier内有效FSM操作迁出 | FSM任务 | 不因位于SkillDocument目录而由本任务删除 |
| EventGraphAuthoringDocument、HostEventGraph、HostEventGraphEditorMutation | 事件图任务 | 不按Document命名删文件，不接管事件/变量运行 |
| BtsmtlSlateTimelineProjection、Pose adapter与Mutation | Timeline/Pose任务 | 本任务不改其UI与运行消费文件 |

共享Catalog位于Runtime/BTSMTL/TreeDesigner/Scripts/Authoring。它的物理文件由本任务维护，但领域descriptor内容仍由各领域正式模块提供，不把所有业务分支搬进Catalog。

业务取舍：共享合同一个Owner可以避免并行修改互相覆盖；领域仍拥有自己的规则，避免公共目录膨胀成中央业务实现。

### D3 必要公共API/metadata的输入输出

下表是本任务允许补齐的接口范围；已有能力直接复用，只有缺口才实现。输出均为当前调用使用的typed结果或正式方法描述，不保存第二份节点/字段/owner模型。

| 合同 | 输入 | 输出 | 消费方 |
|---|---|---|---|
| 正式能力查询 | 稳定业务kind、领域/图role | 唯一正式类型、字段、端口政策和能力引用 | 原节点适配、FlowCanvas菜单、UI、C#输出器、compiler |
| 字段读取与访问描述 | 正式节点实例、字段identity | 当前typed值、默认值、可写性、局部约束、正式读取与配置入口 | 人工UI、C#领域输出适配、compiler |
| 创建/配置入口描述 | 正式类型/能力、owner上下文、明确参数集合 | 已有factory/Configure/Set入口及参数对应关系；缺口由所属业务API补齐 | C# authoring的代码输出/生成适配、人工创建入口 |
| 完整端口形状 | Capability、typed参数、原生Macro/变量动态接口 | 稳定逻辑端口、类型、方向、容量、顺序 | 原节点/FlowCanvas、C#连接输出、现有领域连接规则、compiler |
| 正式引用读取 | 当前正式对象、业务引用槽 | 实际目标、稳定identity、领域owner与内部/外部引用所需事实 | C#输出器的调用内依赖排序、领域复制/保存、compiler |

创建/配置描述只是指向正式业务API及字段对应，不保存C#源码、语法树、脚本模板或执行历史。通用标识符、字符串/数值格式、依赖排序、代码文件写入及编译入口执行由C# authoring负责。

本任务不从JObject或AgentPackage反推出正式字段；输出器也不能自己补一份字段表。恢复某个字段的正式方法缺失时，在对应领域补API或登记具体依赖，不能反射私有字段或静默使用默认值。

### D4 唯一字段/端口描述，不建设中央校验

固定、条件与动态端口统一由正式定义和NodePortShapeProjector输出。FlowCanvas只物化真实端口；原生Macro接口、变量声明和合法组合步骤仍是它们各自的正式来源。参数值按实例保存，未接线字面量只使用正式默认值存储，不复制到第二个payload。

正式业务API保留原有局部合法性判断，compiler保留完整产品约束。共享metadata只声明字段/端口并做自身一致性约束，不收集全角色内容再运行中央Validator。输出器仅检查能否完整表达对象与字段，不复制业务规则。

GraphAuthoringCapabilityDescriptor已删除仅供旧Agent协议使用的DocumentCodecId字段及其登记参数，继续保留真正服务UI、domain role、端口、正式方法和compiler的内容。不能因为类型名含Document就删掉仍有正式用途的领域编辑投影，也不能为避免编译依赖保留旧协议兼容字段。

### D5 r2下的来源与显式操作

共同metadata是业务规则的唯一来源；当前资产是人工编辑与export_code的读取对象。显式导出的C#是其声明生成范围的重建来源，并非必须永久依赖旧生成资产。

    原节点 / FlowCanvas / 人工UI / 正式C#创建
                       ↓
              同一正式业务定义与API
                       ↓
          当前Graph、节点、参数、连接与引用
                       ↓ 显式export_code
                 完整C#创建代码
                       ↓ 已编译入口 + 显式generate_assets
             重建并保存明确范围的正式资产

两个MCP由C# authoring任务提供。人工修改或保存不会自动生成代码；重新生成不自动合并未导出修改。公共基线不要求源码解析、增量回写、sync状态、rebase或整包事务。本任务的字段和引用API不引入这些机制。

业务取舍：显式完整导出与重建让输入、输出范围清楚；作者需要先导出希望保留的人工调整。自动合并旧代码和未导出资产会需要另一套同步模型，r2明确不采用。

### D6 历史成果和真实冲突

已有TransferPayload、Edge引用、order和状态机旧steps删除成果保留。普通组合Step继续由真实节点数据表达，旧properties.steps只是当时包路径，不是需要保留的协议结构。FSM任务负责这些数据如何进入其正式资产和执行生命周期，本任务只让已确认业务规则可以通过共同metadata读取。

本轮没有自动裁定任何Edge/旧Step真实值。若出现条件、priority、abortPolicy或order差异，在本文件记录精确根、双方字段值、来源、已有正确成果和待决影响；未经明确决策不覆盖，不重新创建迁移器。历史记录“已完成20条边迁移”不证明后来未提交资产也已一致。

### D7 依赖和职责接入

| 依赖 | 本任务提供 | 接收Owner负责 |
|---|---|---|
| C# authoring r2 | D3字段/类型/端口/引用与正式API描述 | 两个MCP、代码输出/生成、两份binding的JSON退役、旧公共协议删除 |
| FSM | 共享Capability/端口合同和已确认的共同业务参数 | 原生FSM、资产、转移/order/生命周期、实际迁移及applier业务迁出 |
| 事件图 | 通用字段/端口合同，领域由其注册 | 事件执行、变量声明/生产、正式图及领域输出适配 |
| Pose/Timeline | 公共metadata基础 | 各自能力模块、Mutation、UI/输入消费及领域输出适配 |

共享字段的移除必须同时有合法消费者去向；依赖未接通就记录该具体实现未完成，不复制其它Owner代码，不用兼容层绕行。事件运行和变量消费未完成不等于要求本任务继续保留Agent协议。

## Spec Comparison

| 现行或旧规划条款 | r2处理 |
|---|---|
| current的Agent Document/五工具/整包同步要求 | 与r2冲突，由remove-agent-authoring-use-native-csharp提供删除delta；本目录撤掉旧Document sync delta，不重复接管协议退役 |
| 本目录旧proposal中的全局v8和MCP bridge职责 | 从当前目标删除，仅保留implementation历史；不存在“再发布一个同义版本” |
| 本目录旧Skill spec中的Document往返与固定FSM端点要求 | 改为正式业务定义被C#和人工使用；FSM具体形状由FSM规划负责，已正确数据规则保留 |
| graph-authoring-domain-framework的Agent消费者条款 | C# authoring任务负责删除/替换原协议要求；本目录仅新增共享字段/端口/API输入输出合同，避免两份delta修改同一Requirement |
| 资产永久唯一来源与普通编辑自动同步 | 不属于r2：当前资产显式导出C#，代码显式重建范围，未导出调整不自动合并 |
| 当前正式btsmtl-skill-authoring-model spec | 本轮检查不存在current文件，作为New Capability规划；旧任务勾选不替代实际安装状态 |

本轮不修改current或其它任务文档。旧共同定义方向与后来扩大的FSM/v8 design差异在本版已显式分开；不回退对应源码或篡改历史完成记录。

## Risks / Trade-offs

- [只移动字段位置，仍由两套代码维护规则] → 两侧适配都改用共同定义，删除本任务拥有的重复规则。
- [C#输出缺少正式读取/配置方式] → 缺口落回D3和所属领域API，不让输出器新建字段模型。
- [共享协议字段仍被旧入口引用] → 已确认当前代码没有读取者，删除DocumentCodecId；协议删除和JSON binding重写仍由C# authoring负责。
- [合法步骤/端口被误当旧协议] → 按实际业务用途区分，保留组合步骤、真实端口和正式引用。
- [文档完成记录混入当前执行] → implementation保留原结果与失败证据，r2 tasks只列剩余实现，不列旧包重建或验证工作。

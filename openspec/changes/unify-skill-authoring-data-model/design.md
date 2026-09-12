## Context

动机见 [proposal.md](proposal.md)。用户于 2026-09-12 选择“集中节点参数和规则，继续让 FlowCanvas 保存图”。本设计是待实施目标；代码审计基准为主工作区 `6c6c82a94`，不覆盖工作区未提交改动。

### 已核对的数据链

| 位置 | 当前实际职责 | 本次要处理的差异 |
|---|---|---|
| `BtsmtlSkill*FlowNode`、`BtsmtlSkillNodeInspector.cs` 中的 Ability 节点 | 序列化参数、校验、插件端口、Inspector 入口 | 业务参数与插件节点耦合；5 个 Ability 节点定义混在 Inspector 文件中 |
| `BtsmtlSkillCapabilityCatalog`、`BtsmtlSkillGraphAuthoringMetadata` | 特性转目录、节点类型/variant、固定和动态端口 | 固定端口仍构造节点并 GatherPorts；Blackboard Get/Set 又维护 ProjectBlackboardPorts 与 PortVariants |
| `BtsmtlSkillNodeAuthoringBinding` | 按 FlowNode 类型分别读取/写入 JObject | 与 Inspector、节点字段声明及编译分支重复维护同一字段身份 |
| `BtsmtlSkillGraphClosure`、ClosureIndex、GraphCopy、Exporter、Validator、Applier | 各自发现、重映射或校验引用 | Edge 条件已进入部分链路，但 Copy、Index、Exporter 引用发现等仍依赖 Step |
| `BtsmtlSkillGraphOccurrence`、LeafEmitter、OperationBindings | 读取实际 FlowNode/Port，发射现有操作 | 插件对象与业务 lowering 耦合；编译映射不是应删除的作者配置副本 |
| 旧 `BtsmtlGraphAuthoringCapabilities` | 为旧 BaseGraph 领域登记输入、状态、动作等节点 | 仍有 SharedGraph、状态机及目录消费者，不能凭名称或目录批量删除 |
| Pose 的 payload、NodeDefinition、CanvasDefinitionProjection | 参数与定义分开，画布读取投影 | 可复用共用字段/端口基础；不把 Pose 算法、空间、Slot 或运行 ABI 搬入 Skill |

静态扫描得到 43 个 Skill kind 特性声明和 15 个原生逻辑 wrapper 登记；Macro 系统节点另有登记。这是盘点起点，不是运行时完整目录数量。实施必须对照实际正式注册集合，覆盖继承、variant、系统 anchor 和未在 Corin 使用的能力。

已发现可定位的作者差异：`gameplay-effect-remove` 的 `query` 可由 Document 写入，原生 Inspector 却只把旧 Query 传回，没有编辑控件。模型统一必须交付作者可用的同等字段能力，不能仅把这些分支搬到另一个文件。

## Goals / Non-Goals

**Goals:**

- 一个节点参数只保存一次；节点定义是字段读写、默认值、端口、约束和引用语义的唯一来源。
- 增加一个普通字段或节点时，扩展对应业务族定义与编译 binding，不要求修改多个集中类型分支。
- UI、Document 与编译接受同一合法目标；复制、删除、事务和诊断使用同一稳定身份与引用关系。
- 消除状态机转移双写，并保留普通组合流程、Macro 等现有业务的完整表达能力。
- 通过任务交接把跨 change 的数据层工作集中到一份执行清单，历史证据不等于新模型完成证据。

**Non-Goals:**

- 不抽走 FlowCanvas 的正式节点/连线集合，不重做 GraphEditor、图序列化、Macro 编辑器、selection 或布局系统。
- 不把所有领域合成同一种业务节点，不把普通步骤全部改为状态转移，也不把运行操作码当作者 kind。
- 不改变 SkillDefinition、ControlModule、Input/TargetData、GameplayEffect、Timeline、Pose、Session 和网络的业务所有权；不迁移 TrainingEnemy。
- 不新增第二个 Snapshot、领域 JSON 作者模型、Mutation 服务、事务、MCP 局部工具或运行执行器。
- 不新增测试代码；任务中的检查使用现有正式编译、Validator、Document 和数据报告。手动端到端验证不写入 tasks。

## Decisions

### D1 参数独立，拓扑继续由 FlowCanvas 保存

正式 Skill Node/Connection 是插件承载壳，持有唯一 typed payload。payload 保存业务参数和强类型引用，不包含 position、selection、委托、FlowCanvas Port 或运行状态；相同参数不得同时保留在壳字段和 payload 内。Node/Edge UID、端点、图集合、布局仍只保存于原生图。Macro 的 inputDefinitions/outputDefinitions、Blackboard 原生 Variable 的 identity/name/type/default 继续是现有各自来源，不复制成另一份可写列表。

例如 Locomotion 的速度、位移模式、转向速度、时长与曲线进入同一个业务参数对象；原生节点仅提供端口物化和事件接入。对仅表达标记的节点允许无字段 payload，但不能创造虚假参数。

未接线值输入的作者字面量继续使用唯一原生默认值存储，作为正式定义下的独立值输入槽读取/修改，payload不再复制它；定义负责默认值与合法类型，原生读取适配只读取已保存字面量，不调用运行求值getter。Document的properties与values分别表达节点参数和这些字面量，不能把同一字段同时放入两者。

业务取舍：用户选择的方案保留成熟画布交互，同时减少新增技能能力时的同步成本；代价是资产与图拓扑仍依赖插件。完整自有拓扑能让图存储脱离插件，但需要接管全部保存、Macro、复制与 Undo，不属于本提案。

### D2 每个节点种类由一个定义提供完整作者规则

按结构/条件、Input/Blackboard/CharacterState、Action/Ability、Motion、Macro/Timeline 引用组织定义模块。正式注册根只聚合这些模块，不能形成另一个包含全部业务 switch 的总定义文件。

每个定义提供稳定 kind 与 variant、payload 类型、默认构造、字段读取/写入与值约束、条件可见性、固定/条件/动态端口、引用访问及重映射、编译 binding identity。字段身份通过同一访问定义连接到真实参数；特性可以作为声明语法，共享 Capability 是投影，不能另外维护相同字段的人工白名单。参数和字段定义层不依赖 JObject、AgentPackage 或 Unity SerializedProperty 路径。

原生 Inspector 使用正式字段控件、资源选择和定义提供的业务命令；复杂 TagQuery 等使用 typed 字段控件，同样读取这一字段定义。文案和布局可因界面调整，字段是否可写、类型、必填和合法值不得自行决定。能力未同时提供创建、修改、导出、校验和编译 binding 时，不进入可编辑目录。

业务取舍：集中完整定义让新增节点按业务局部扩展，代价是每个节点必须登记完整合同。仅移动现有大段 Apply/Export/Draw switch 能减少文件位置混乱，但不能减少重复维护；不作为交付结果。

### D3 端口方向由参数和正式接口投影，插件只物化

唯一共享 Port Shape projector 接收节点定义、typed 参数和正式动态接口，输出端口 identity、类型、方向、容量和顺序。Skill Canvas、Document 与编译读取相同结果；原生 GatherPorts 只负责构建实际插件端口及绑定稳定 identity，禁止反向构造默认节点来定义合同。

Blackboard 的 Get/Set 和值类型只判定一次；Macro 参数从同一原生接口读取；组合步骤从唯一步骤集合读取。原生逻辑 wrapper 保留 AND/OR/NOT 和 12 项数值比较的作者能力，正式定义明确其逻辑端口与既有 Program 端口映射，第三方同名执行行为不成为技能语义来源。必要的插件接入只扩展已有域钩子并登记补丁，不能新建旁路画布。

业务取舍：以正式定义生成端口，作者改模式后 UI、导出与编译一致；需要迁移默认值和现有连线。沿用节点原型反推的实现工作较少，但无法消除当前第二份动态规则。

### D4 引用定义必须覆盖生命周期，不止导出

正式引用描述包含引用槽 identity、实际目标、目标角色、私有/共享方式、调用方身份和重映射方法。Graph、节点 payload、Step、Transfer、Macro 接口与 Timeline TreeClip 各通过既有领域合同提供引用。公共遍历只组合这些事实，并检查唯一身份、私有所有权、完整闭包和环；不在每个消费者内重新判断具体节点类型。

Closure、ClosureIndex、Document 导出/校验、资产 owner 收集、复制、删除回收、迁移和作者语义 hash 使用同一关系。复制生成独立私有闭包并重映射其引用；共享目标仍指向原资产。删除仅回收当前根中失去正式引用的私有对象，不通过显示名或路径猜 owner。

业务取舍：统一关系能覆盖“改后能导出”和“复制后不串到原图”等完整作者行为；代价是需要明确每种引用的生命周期。只共用一个收集 Graph 的列表不能表达调用槽和重映射，因此不足以承接复制与删除。

### D5 状态转移与普通步骤各自只有一个来源

StateMachine 图的 state、@enter、@any 移除 Composite/steps。状态节点使用稳定 `StateIn` 输入和 `Transfer` 输出；@enter/@any 只有 `Transfer` 输出；@exit 只有 `StateIn` 输入。转移输出允许多条连线，目标状态输入允许多条转移；其他图的端口容量保持各自合同。

转移条件、优先级、中止策略和显式 `order` 只属于 Edge payload。优先级方向保持既有Skill转移合同，同优先级按正式 order；order 在同一来源节点下唯一，重命名或保存重载不改变排序。不得按随机 UID、字典枚举或节点位置重新推定执行次序。迁移记录原有效顺序；源码当前按 UID 遍历与原转移 spec 的“创建顺序”不一致时，在迁移报告中列出会改变选择结果的并列转移，不能用文档裁决覆盖用户已确认行为。

Sequence/Selector/Parallel 保留步骤参数及各自顺序/条件/中止语义。Loop 当前是固定端口节点，不因旧文档把它写在“steps”列表中而增加步骤集合。结构统一的目标是同一业务没有两个配置来源，不要求不同业务拥有同一字段集合。

业务取舍：独立转移参数让作者在线上编辑状态切换而不维护端口配置；保留组合步骤避免改变技能执行顺序。把所有边升级成转移会扩大业务语义和资产迁移，不属于本次选择。

### D6 编译读取业务数据，适配插件结构一次

保留现有 Skill 编译入口与调用 occurrence。一个原生图读取适配器将 Node/Edge identity、typed payload、正式端口、引用和默认输入暴露为只读编译输入；它不持久化另一份图，不重新命名字段或推断业务规则。

各业务族 compiler binding 接收这些业务数据和现有编译 context，发射既有操作与 typed port 映射。Occurrence 只负责调用路径、owner 和来源信息；Program Builder 继续负责通用 IR 写入。停止在多个 emitter 内识别 FlowNode 的 C# 类型或调用实际 Port getter决定语义。原生逻辑到操作码的转换、Semantic IR、Numeric Program 是必要派生产物，不视为作者双写。

复用 `ILocomotionInputMotionAuthoring` 等已有业务合同及 Pose 共用的字段/端口基础。旧 BaseGraph 节点逐项记录实际消费者：仅剩旧 Skill 用途的迁移后删除；被合法非 Skill 领域使用的类型保留其业务边界，语义相同的参数/校验可以共用，不能因为 kind 名称相同而合并不同执行模型。

业务取舍：注册式 lowering 保留各业务实现独立演进，同时减少插件耦合；代价是需要维护清晰的编译输入接口。让所有节点直接引用 Builder 会把编译实现塞回作者数据层，不采用。

### D7 作者语义版本与画布布局版本分离

作者语义 hash 只消费正式参数、端口、连接、引用、声明、Macro/Timeline 语义及明确执行顺序；布局 hash 只消费节点位置、分组和视图内容。Document 整包 hash 仍覆盖所有 editable 文件，所以移动节点会改变 package hash，但不能使相同业务产物或运行来源失配。

复用当前 source map 和版本链，明确作者布局 hash 与 Program State LayoutHash 不是同一概念。不要因为 asset dependency hash 含画布位置而把布局重新混入语义 hash。

业务取舍：拖动节点不再要求重建技能，代价是所有内容依赖必须通过正式语义访问收集；仅排除节点 position 无法排除 canvasGroups 或被引用图的布局污染。

### D8 Document v8 承接真正的外部格式变化

payload 抽取保持稳定 kind/字段时本可沿用 v7；但本次同时删除状态机 steps、改为固定转移端口、新增条件图 edge owner 与 order，属于 Agent 可见合同变化，按现行 metadata 规则升级整包至 `btsmtl-agent-authoring-document.v8`。所有非 Skill 分片只切换所属整包版本，业务字段不借机重做。

v8 中状态机 Edge 继续保存稳定 id/from/to，增加明确 order，条件图 owner 使用 `kind=edge`、`graphId`、`edgeId`、`referenceKey=condition`。此归属不混用旧 nodeId/stepId；私有条件由精确 Edge 唯一拥有，共享内容只能使用显式共享合同。状态机节点/anchor 目标不再接受 steps。纯值边或非转移边不得携带转移参数。

v8 正式 checkout/rebase/dry-run/apply/validate 不接受 v7 文件，旧包需要显式重新 checkout；未处理的旧 DocumentDirty/Conflict 必须先封存差异并裁决，不能覆盖。迁移只保留显式一次性读取/转换能力，结束后删除；它不成为生命周期工具的兼容 reader。

业务取舍：新版本明确拒绝旧形状，作者需要刷新已有工作包；继续用同一版本容纳两种 owner/端口形状会让旧 Agent 看似成功却丢数据，不采用。跨 Skill/Presentation 仍使用唯一 Document hash、Mutation dispatcher 和事务，apply 成功后另行显式 Build。

### D9 文档按实际交付范围交接

本表是本次唯一交接映射。原已完成任务、commit、job/hash 是其当时范围的证据，不重新当成新模型任务已完成；本 change 的实现任务全部从未完成开始。

| 原文档与条目 | 本 change 承接 | 原文档保留 |
|---|---|---|
| `refactor-btsmtl-flowcanvas-authoring` 2.2.3、2.3.2/2.3.3/2.3.4、3.1.3、3.2.1/3.2.3 的 Skill 定义、端口与引用一致性 | tasks 2、3、4、5、6；已实现原生接入作为基线，补齐实际分散规则 | 原生 UI 宿主、宏业务行为及历史代码记录 |
| 同 change 2.4.2、5.3 的 Skill owner/失败回滚部分 | tasks 5.3、5.4、7.4；原任务保留结果接收入口 | 非本次数据迁移引起的完整交互/跨域事务验收 |
| 同 change 3.3.1 状态机步骤、6.2 作者来源版本部分 | tasks 4、6.3/6.4；普通步骤与运行身份保持原合同 | generation、运行观察、7.x、网络4.5.5与端到端8.1 |
| `refactor-agent-authoring-attribute-driven` 2.1–2.3、3.1–3.3、4.1、5.1 的 Skill 定义完整性 | tasks 2、3、5、6；复用已有共享目录、正式 binding 和事务 | 既有删除成果、Presentation/Control/Clip 与6.1–6.3的v7证据 |
| `add-skill-transfer-connections` 4.4、5.1、5.3、6.1、6.3 | tasks 4.1–4.5、7.1–7.4、8.2；原任务只跟踪接收，不重复实施 | 转移线上编辑/显示合同、1.x–4.3和5.2的历史实现及迁移证据 |
| `refactor-btsmtl-authoring-architecture` 3.2 能力/Emitter一致性中的 Skill 数据输入部分 | tasks 1.2、6.1/6.2的结果作为其消费证据 | Control、Equipment、Timeline语义、runtime及产品职责 |
| Pose、Scene Play、Behavior Designer、Foot、Center 各 change | 仅引用最终公开合同，不复制其任务 | 各自业务实现与验证职责 |

转移专项仍拥有原转移可见行为 delta，本 change 不复制该 capability；共同交付后按规范对账同步并按用户要求处理归档。不得先把“任务已转交”标成“功能已完成”。历史流水账不搬进新 tasks；跨 change 引用指向本表及精确任务。

### D10 与现行规范及在途 delta 的对账

| 文档 | 一致部分 | 冲突或缺口与处置 |
|---|---|---|
| current `graph-authoring-domain-framework` 的 Formal metadata requirement | 唯一语义来源、拒绝未声明字段 | 本 change delta补充payload/definition与插件适配边界，完整保留原Scenario |
| current `btsmtl-agent-authoring-document-sync` | 完整Skill/Presentation、五工具、唯一事务 | current固定v7，与目标v8冲突；本change修改所有受版本影响的Requirement并保留原Scenario；current在实施前仍表示v7 |
| current `btsmtl-graph-core` 与 FlowCanvas 原 change delta | 一个正式拓扑、不恢复旧运行路径 | current仍有BaseGraph宽泛条款；旧change已有Skill适用范围修正，本次不重复造第二份delta，回写时必须先完成该边界对账 |
| FlowCanvas 原 change design/2.2.3 | 原生GraphEditor、直接编译、独立Skill根 | “唯一端口已贯通”只表示旧阶段；新增完整定义工作见交接表。新模型仍使用原生拓扑，未改变UI宿主选择 |
| metadata 原 change design/5 | 内部重构不触发schema变化 | 该原则保留；本次转移外部格式变化明确触发v8，旧v7证据不升级为v8证明 |
| 转移原 change D1/D4/D6 | 条件在Edge、普通流程不改、不双读 | 其“不抽中立层”只约束当时局部转移实现；本次只抽参数/定义、不抽拓扑。旧v7迁移不构成完整edge owner和稳定order的证明 |
| 原转移 spec 的并列转移创建顺序 | 保留作者可理解的稳定顺序 | 当前Occurrence按UID遍历有行为差异风险，迁移必须报告，不用重排掩盖矛盾 |
| `.codex/skills/btsmtl-agent-authoring` 与 current-contract | 禁止第二模型、旁路写入与事务 | 实施v8时同步正式入口/owner/版本说明；提案阶段不提前让技能指导操作尚不存在的v8工具 |

安装 delta 前必须对当前 spec 最新版本重新合并，不能用本次规划覆盖其他 change 后来新增的 Requirement/Scenario。原 FlowCanvas 与 metadata 的 v7 delta 在安装时只保留其独有业务增量，不得把 current回写成v7；本次任务8.2负责最终归并，不以归档顺序碰运气。

## Risks / Trade-offs

- [删除旧字段后反序列化丢数据] → 在旧类型仍可读取时封存精确根与Document差异，逐字段映射；未核对的资产不得进入旧字段删除步骤。
- [Step与Edge已经被分别修改] → 输出每条转移的字段差异；不自动选边或用零值盖有效值，只阻塞有冲突的精确资产，独立定义工作可继续。
- [定义错误同时影响多个消费者] → 复用现有注册完整性、端口、引用与编译校验；报告覆盖所有正式kind/variant，而不是仅构造一个代表节点。
- [共享Macro修改影响其他根] → 正式引用关系收集精确调用者与实际owner，全部进入同一事务；不能只保存当前窗口所在图。
- [全部机械类型判断迁入新的大类] → 按业务族拥有定义与lowering，中心只登记与调度；以新增字段需要改动的位置和旧分支删除结果验收。
- [v8破坏仍未提交的工作包] → 先记录包状态及差异；正式入口显式拒绝旧版并提示重新checkout，不覆盖DocumentDirty/Conflict。
- [多个旧change重新提出同一实现] → 原tasks改为结果接收/范围链接，源码完成只在本change勾选；保留旧证据而不保留第二执行清单。

## Migration Plan

1. 先完成注册集合、旧消费者与精确资产清单；封存现有v7包、作者语义值、Step/Edge对应和有效顺序，记录用户未提交差异。
2. 实现正式参数/定义、端口和引用合同及全部节点业务族，接通原生UI、Document和编译适配。迁移读取仅服务显式迁移，旧配置不能继续作为正常写入口或fallback。
3. 在删除旧字段前输出精确迁移计划：节点参数到payload、Step转移到Edge、旧输出端口到Transfer、条件图owner、稳定order、Macro/Timeline/Blackboard引用。保留合法UID，列出缺失、歧义或冲突。
4. 新代码对未迁移资产给出明确迁移错误，不自动补建；使用既有唯一Document/资产事务完成目标写入、owner保存和反向导出。一次性旧数据读取/转换属于该显式迁移步骤，不新建长期reader或第二apply服务。
5. 同一事务失败时恢复旧owner、引用与正式package，清理仅本次新建对象；迁移报告和失败证据保留。代码回退使用明确提交，不能用资产checkout抹掉用户改动。
6. 迁移成功后删除旧字段、状态机steps、旧type-switch重复规则、legacy补读、无消费者类型及一次性迁移器；用重新checkout/dry-run/validate和现有编译报告核对唯一链路。
7. 正式产物由精确Definition显式Build发布，重新读取当前Console；模型语义、UID、调用路径和运行产物差异分开报告，不把包Clean当作运行验证。
8. 同步相关current specs、project入口和技能说明，合并旧delta的独有业务要求并核对Scenario保留；原change保留未完成的非数据层任务，不因本提案创建或转交而归档。

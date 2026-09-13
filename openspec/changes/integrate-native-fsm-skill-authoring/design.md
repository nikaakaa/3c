## Context

见[proposal](proposal.md)。2026-09-13按公共[原生C#作者基线r2](../remove-agent-authoring-use-native-csharp/design.md)更新剩余计划。旧实现及任务映射保留在[归档交接](../archive/2026-09-12-refactor-btsmtl-flowcanvas-authoring/split-handoff.md)和本目录历史实施记录；已勾选工作不撤销，但不代表新导出/重建或协议删除完成。

此前只读源码已确认原生FSM类型、配置/创建及领域合同存在，插件runtime被拒绝。当前任务表已将r2各项勾选，本轮保留这些实现记录；它们只说明当时的图/FSM范围，不能扩大为r3的完整Ability外壳已输出。当前`BindSkillRoot`仍只登记`Definition.SkillGraphs`，角色外壳另存于`SkillDefinitions`。实施前重读实际差异，不从旧截图恢复steps、不覆盖其它窗口正确改动。

## Goals / Non-Goals

**Goals:** 保留已正确原生FSM与Program链；把旧Applier内真实资产操作落到正式Skill/FSM API；让公共C#作者入口完整输出、重建明确FSM拥有范围并恢复根挂接；迁离旧Agent调用依赖。

**Non-Goals:** 不实现公共MCP/通用输出器、中央Validator、整包同步事务、源码解析/自动同步；不重做共同节点定义或端口规则；不承担事件图运行、Pose变量、通用观察、网络和Replay；不恢复Character RootTree，不新增测试。本次仅写规划，不运行Unity、Build或Play，不联系其它窗口。

## Decisions

### D1 状态机换正式资产，执行体保留原业务

| 作者内容 | 唯一存储与owner |
|---|---|
| GameplayAbilityDefinition | r3独立技能主资产，拥有规则及唯一AbilityGraph；角色通过AbilityGrant引用 |
| AbilityGraph（原Skill执行图） | Ability私有执行根，保存于Ability文件；不再与外壳分开登记为第二个技能入口 |
| StateMachine | 原生FSM，唯一属于调用它的节点，私有内容保存于Ability或正式共享Macro实际文件 |
| State | FSMState业务适配，唯一引用StateBody |
| Transition | FSMConnection业务适配，唯一保存condition、priority、abortPolicy、order；私有条件由Edge拥有 |
| StateBody、ConditionRule、Macro、TimelineBody | FlowCanvas正式图及原接口，不再镜像状态机拓扑 |
| Sequence/Selector/Parallel/Timeline | 保留现有组合、等待、并行和停止合同，不硬改为FSM状态 |

同一GraphEditor负责创建、导航、原生Inspector、Blackboard provider、复制和Undo。共同业务定义来自数据层change；FSM只增加宿主适配，不维护第二份规则。业务取舍：继续自定义状态图迁移成本较低；本次选择原生FSM以统一状态机作者体验，承担插件扩展与资产类型适配成本，但不接管插件运行。

### D2 原生名字不代表相同执行语义

Entry/Prime只在单一无条件入口时直接表达默认目标；条件/多分支入口保留正式路由，不复制第二份Prime配置。Any保持既有条件准入，Exit表示退出当前FSM调用。退出阶段由状态运行代码处理；r4不把原生FSM钩子或State.OnExit暴露为作者必配的清理/终态图，不能把它们当作决定转移或Ability终结的入口。

原生FSM无ConditionTask的边表示OnFinish，不能替代BTSMTL无条件边；主体完成仍显式用state-root-completed。原生Stacked/Clean不等于abortPolicy，未登记语义拒绝。正式业务只通过typed定义与编译binding进入Program。先停止源状态主体再进入目标的生命周期由原运行模块实现；Ability终态和动画表现分别按D10/D11处理，不由作者OnExit补齐。

插件FSMNode固定outConnectionType，FSM限制变量拖拽。连接参数、能力过滤和provider入口通过正式插件扩展点接入并登记补丁，不用图外可写转移表或旁路Inspector绕过。Inspector禁止做迁移、闭包重建、Build等重操作。

### D3 公共双工具与FSM领域API各自负责什么

公共基线只提供`btsmtl.export_code`与`btsmtl.generate_assets`：前者从当前明确资产完整写出C#，后者执行已编译的正式生成入口并保存指定范围。工具参数、当前编译结果校验、通用输出器与旧五工具退役由C# authoring任务负责，本change不再发布Document v8、不新建FSM工具或同步状态。

生成范围内，已导出的C#是可重建内容来源；人工修改仍先保存在当前资产，只有显式export_code才进入新源码。重新generate_assets不自动合并未导出的修改。导出不读取旧源码做增量合并，不产生源码Undo、rebase、DocumentDirty/Conflict、反向导出或自动Build。业务取舍：完整当前图输出能覆盖实际结构，代价是不保留原创建算法/手写排版，未导出的调整不会被重新生成自动保留。

### D3.1 Applier内真实FSM能力逐项迁出

以下是r2对旧`Editor/CharacterPipeline/Authoring/SkillDocument/BtsmtlSkillGraphAuthoringApplier.cs`的迁出来源记录。当前任务已记录有效能力迁出和旧协议移除；本表用于说明能力去向，不表示该旧文件仍应存在或需要恢复。r3继续消费已落到正式模块的能力。

| 现有位置 | 保留能力与已有模块去向 | 退役部分 |
|---|---|---|
| CreateNativeStateMachine / ResolveNativeStateMachineTarget | FSM创建、明确父图/调用节点owner、系统入口初始化；落实到BtsmtlSkillGraphAssetFactory及原生FSM模块 | AgentPackage目标、local表、Session.Touch和按旧包asset identity找生成对象 |
| SyncNativeStateMachine | 状态创建/配置、name/layout、StateBody挂接与无效私有内容回收；复用原生State和工厂API | 对比Document节点列表的增删同步、JObject/包字段分派 |
| SyncNativeAnchors / NativeAnchor | 系统Entry/Any/Exit复用、身份读取和恢复；落实到NativeStateMachineContract/正式身份接口 | 包anchor描述和协议local分支 |
| SyncNativeEdges / ResolveNativeEndpoint | 明确端点、连接/改接、条件引用、priority/abortPolicy/order及identity恢复；复用NativeConnection与领域连接API | AgentPackage Edge、旧包差异删除及默认协议值解释 |
| ValidateNativeOwner / ValidateNativeAppliedIdentityContracts | owner一致、身份唯一/可恢复、条件角色、顺序等真正业务约束；并入已有FSM Contract/GraphClosure/节点配置入口 | Applied包回读、协议格式/hash/sync和重复规则 |

只暴露当前正式业务需要的typed读取/配置方法，不让生成代码依赖AgentPackage、JToken、私有字段反射或SerializedObject路径。校验继续分布在已有FSM/Skill模块，公共导出器只判断输出是否完整，不重新维护业务规则。

r2盘点曾发现缺失State/Edge创建受`local:`限制、系统入口只能对照已有UID；身份恢复已按任务记录迁入正式接口，r3不能退回协议local表或旧子资产GUID查找。工厂生成的系统入口和默认StateBody继续复用或通过明确领域合同一次建立，不能重复创建后靠名字合并。

### D3.2 完整输出与生成范围

输入是明确FSM/Skill根、所属Definition/owner上下文、生成范围和范围外精确资源。由公共C# exporter收集FSM、StateBody、ConditionRule、内部Macro、合法组合/Timeline引用与layout等正式拥有闭包；数据由本领域读取合同提供，不先转JSON。按对象身份在本次调用内去重，状态机有环不按执行边递归打印。

输出顺序为：创建根/子图/状态及复用系统入口 → 配置业务identity、参数、声明、动态端口和布局 → 绑定Body/Condition/Macro等内部对象与精确外部资源 → 按明确端点创建连接并恢复identity、priority、abortPolicy、order → 显式恢复指定Definition/Skill入口及根引用。字段依赖对象存在时延后配置，不生成持久化Mutation Plan。

内部共享对象只创建一次并用生成局部变量引用；共享但在范围外的资产作为精确外部输入，不因为可达就复制、覆盖或删除。只有真正外部资源使用资源identity；不能把同批待重建子资产的旧GUID/local file ID当依赖。内部条件、StateBody、Macro和系统入口的业务identity/order必须保留，物理对象、GUID和实例可以替换。

每个正式状态字段、生命周期引用、原生配置、条件、边顺序、动态接口、布局和根绑定都须有输出/恢复办法；没有对应调用时报告精确对象/字段/原因，拒绝完整导出，不静默丢字段或临时用插件runtime解释。公共写出器在完整性检查成功后写源码，FSM适配只提供所需读取与调用描述。

r4正式作者模型不再包含用于系统清理/终态推导的OnExit执行图；内部退出函数属于代码，不导出成作者节点。旧资产通过明确领域操作迁移后输出新模型，不能一边保留旧字段一边静默省略。完整Ability输出逻辑规则、执行内容、Timeline片段局部混合及明确Slot/表现引用；跨动作过渡和基础姿态规则随其对应动画作者根输出。

### D3.3 原模块的保存、失败和Build边界

人工编辑继续走已有BtsmtlSkillFlowEditorMutation的领域Undo/保存。生成代码调用同一正式API，按现有资产模块建立与保存声明范围、返回实际创建/替换/清理和失败结果；不新建跨领域整包事务或中央Validator，不把部分生成报告为完整成功。

删除后重建必须恢复指定Definition/Skill根挂接，不能只生成孤立FSM。清理只覆盖明确旧输出，不删除源码、原始素材、外部共享资源或未列入范围的消费者；范围外消费者若需重挂，须作为本次明确目标输入。首次迁移先完整导出并取得重建证据，再清理旧输出，不因资产可重建直接删除现有正确资产。

Build独立显式执行，仍由现有Character编译器读取正式资产产生Program/Projection。声明范围的结构/配置往返不等于运行测试，文档更新不执行生成、删除、Build或Play。

### D4 编译只汇入现有Program

去除Occurrence对全部节点为FlowNode、全部边为BinderConnection的假设。不同图适配各自真实资产，向同一只读编译输入提供业务参数、引用与来源；不先重建旧FlowGraph。保留状态/边identity、逻辑端点、调用路径、条件、priority、abortPolicy和有效order；不按位置、随机UID或插件数组顺序重排。必要资产对象重建在同一事务完整重映射并报告。

本change只提供原生State/Connection到Program SourceMap及只读绘制接口。实例选择、导航会话、订阅生命周期和采样性能归观察change；两者共用正式诊断，不为显示高亮启动FSM。网络与Motion/Body/WorldSolver执行链不变。

### D5 Corin按消费者清理，不借迁移改规则

2026-09-13最新交接：DodgeBack/DodgeForward已通过正式generate_assets落盘为Native FSM + StateBody + Timeline，根图Startup Branches、Enter Dodge与Directional Dodge Run Intent Setup/Clear已清零。本窗口只读生成源码确认StateBody内仍有Action Exit Selector、四个Submit终态分支和Action Exit_To_Succeed_Rule；落盘与根图清理的完成记录不等于StateBody退出形状已最终收敛。以下旧“保留并行”表述只保留及时监控/取消的业务，不要求恢复已删除的根图Parallel外壳。

| 对象 | 处理 |
|---|---|
| Attack Startup Branches、OnEnter Action Setup、Clear意图写入 | 删除实例、边、无人读取的图内声明及失去引用的私有空条件/layout，Root直连状态机 |
| Attack连段与15条Exit边 | 保留所有条件/排序/中止/identity；摘要显示主体完成、可闪避且收到请求、移动取消、连段目标 |
| Dodge根图Setup/Enter/Set | 已按交接清零，保持现有Native FSM与Timeline；不恢复旧根并行外壳 |
| Dodge StateBody的Action Exit Selector与空Rule | 按D10—D12把有效结束决定迁回Ability生命周期，清理由代码完成，再删除旧壳及空作者OnExit；不换成OnExit分派节点 |
| Hit/Recovery/IFrame和ActionTarget | 按Timeline、Frame Fact、targetSnapshot、输入绑定和编译引用保留，不以缺少Get判定无消费 |
| StopThreshold | 保留现有有效值和引用；Attack的声明仍被两个Dodge使用，未决定新归属前不删除或临时替换 |
| 正式跑步意图 | 保留ControlModule在Idle清理、DodgeForward完成后置位的时机，不新增攻击开始清理行为 |

StopThreshold若统一ControlModule阈值可保持移动/取消一致；Skill私有取消阈值可独立调手感。二者是未来业务变更，不是本次迁移的隐藏选项，本change不以选择未定永久阻塞FSM交付；清理验收明确排除仍有消费者的正式声明。Corin控制文件中的MovingTurn来源和60Hz时长改动不覆盖。

m_Name问题已在任务4.5记录归属Slate.CutsceneGroupInspector；不再作为本Ability阶段的修改目标，不回头改Skill字段或清缓存。

### D6 r2规范对账与职责

原先继承的Document同步delta从本change删除；公共协议规范退役由remove-agent-authoring-use-native-csharp唯一负责。本change剩余四份delta只保留原生作者、领域API、稳定身份与编译隔离；移除其中要求Agent Snapshot/Reconciler/Document往返的增量，不把历史完成记录改写为未曾实现。

| 现行差异 | 处理 |
|---|---|
| current及旧提案仍含Document/五工具/重复Validator | r2公共change退役；本change不再安装v8或重建中央校验，历史3.1/3.2仅保留证据 |
| graph-core/domain/editor-shell已有current但与旧delta不全相同 | 只归并Skill适用范围和已交付原生入口，保留后来加入的其它领域要求；原Scene/Pose表面不借本次重做 |
| 外层None/Attack/Dodge状态机、禁止全部Sequence、BTSMTL/Pose共享旧StateMachine View | 与当前独立Skill/C#控制、有效组合流程和原生FSM目标冲突，交付时明确修订范围，不恢复旧RootTree |
| Skill Program的Pipeline要求 | 保留为下游合同；网络Adapter实现与载荷证据由Corin闭环change负责，不进入本tasks |
| 原“Unity资产永久唯一来源/不能替换物理身份”的解释 | 生成范围以已导出C#重建；保留业务identity/order和根关系，允许替换物理对象；未导出修改不自动同步 |
| GraphAuthoringApplier混合协议与真实操作 | 本任务迁出有效FSM能力，C# authoring在调用者脱离后清理协议；不能先整目录删或整包改名 |

### D7 r3完整Ability入口与命名

用户已确认采用GameplayAbilityDefinition作为完整技能入口。本节是r2之后的新目标；前文的Skill/FSM名称用于识别现有代码与历史接口，不意味着保留两套正式作者名称。

| 业务对象 | 正式职责 | 从现有模型迁移 |
|---|---|---|
| GameplayAbilityDefinition | 可独立打开的技能资产；唯一拥有技能身份、激活/阻断/取消/目标要求、效果引用、后续能力关系和执行图 | CharacterSkillAuthoringDefinition及ActionProfile中对应技能规则；从角色内联配置迁到明确Ability资产 |
| AbilityGraph | 激活后的执行内容，保留原生FSM、Sequence/Parallel/Macro/Timeline语义 | 原Skill执行图；私有图归Ability文件，共享资源保持明确外部引用 |
| AbilityGrant | 某角色/装备授予哪个Ability、输入映射及已有授予参数 | 角色侧Skill登记和输入绑定；不复制技能规则/执行图，不新增等级系统 |
| AbilityExecution | 一次实际释放的身份、阶段和生命周期 | 复用现有Skill相关Action实例状态和Program布局，不并行创建第二实例 |
| AbilityExecutionContext | 本次释放的owner、目标、输入来源、代次及任务/Timeline关联 | 当前运行上下文；普通技能不暴露必须手配的空ActionContextSlot |

AbilityGrant对应授予记录，不是一次执行，不把UE AbilitySpec当成AbilityExecution。命名借用UE的职责划分，执行仍是本项目的Program/Simulation Pipeline。

作者打开一个Ability页面即可编辑基本信息、激活规则、目标要求、执行图和资源引用。专用规则随Ability保存；实际共同维护的规则可显式共享，并直接在该入口显示其来源，禁止本地副本加共享副本同时生效。独立动画、效果和共享Macro保持资源身份，不将整个角色或素材库塞入Ability。业务取舍：聚合入口减少拼配置的负担；显式共享能统一多个技能的策略，但修改会影响全部消费者，必须显示共享关系。同值不自动等于应共享。

规则只有一个正式对象来源：默认由Ability拥有，真正共享时引用明确共享对象，不同时保存内联值和外部覆盖。共享规则identity与Ability identity分开；DodgeBack/DodgeForward不能因迁入两个Ability就改变原Dodge准入分组、并发上限、互斥和取消边界。旧ActionId等仍参与执行的身份按业务角色明确映射，不机械替换成AbilityId。

Action不再作为技能作者层与Ability并列的“另一种技能”。迁移ActionProfile/Context/Instance前按消费者区分：Skill专属职责迁入以上对象；若通用Action确实服务独立非Skill业务，保留其明确内部职责，不全库机械改名。相同状态不因作者术语变化复制，已正确激活、取消、预测身份与Timeline停止顺序保留。专属旧类型/字段/入口迁完即删除，无兼容别名或旧新双写。

### D8 r3完整Ability的C#范围

完整Ability输出从GameplayAbilityDefinition开始，覆盖外壳字段、自有激活规则、唯一执行图及私有FSM/条件/Macro/Timeline；外部共享策略、效果和素材只输出精确引用。公开export/generate工具仍只有原两个，扩展其正式领域根支持，不另造Ability MCP。

完整生成顺序为创建Ability资产和自有内容 → 配置能力规则及业务identity → 恢复资源和内部引用 → 连接执行图 → 挂接唯一AbilityGraph → 对明确指定角色/装备恢复AbilityGrant及输入映射。若本次只生成可复用Ability资产，可以明确不授予角色；若声明恢复某角色能力，则不得只返回孤立图或依赖该角色残留旧SkillDefinition。删除输出后重建遵循同一明确范围。

子图导出只覆盖该子图拥有内容及指向既有Ability的owner绑定，不克隆外壳、规则或其它技能。完整Ability与子图范围必须显式区分，不能把当前仅BindSkillRoot/SetSkillGraphs的代码作为完整Ability实现。只操作FSM不得修改准入、阈值、目标策略、输入绑定或后续能力。

### D9 r3与现行规范的实际差异

| 当前合同/代码 | r3目标与处理 |
|---|---|
| character-pipeline-definition-authoring要求SkillDefinitions + SkillGraphs及EntryGraphAuthoringId跨表解析 | 改为AbilityGrant精确引用GameplayAbilityDefinition，编译由Ability直接进入其执行图；对应正式规范在实施时同步，当前不伪装为已变更 |
| 本change原“技能根是独立FlowGraph主资产” | 明确替换为独立GameplayAbilityDefinition，私有AbilityGraph归它拥有，不能同时保留两个主入口 |
| 当前GA外壳要求输入、目标、ActionContext混存 | 输入映射归授予，目标要求归Ability，当前目标与执行关联归ExecutionContext；保留有效业务值和使用时机 |
| 当前C#生成只把Graph加入Definition.SkillGraphs | 扩展为完整外壳/规则/图/授予范围，旧图重建记录继续有效，但不证明此新增范围完成 |
| ActionProfile被DodgeBack/DodgeForward共享 | 按明确共享策略保留单一规则来源，不拆成会漂移的同值副本，不改闪避完成或取消行为 |

本轮只修改本change工件，未提前写current specs、公共C#基线或其它规划。名称和所有权迁移属于新实现任务；既有r2勾选保持不动。成本、冷却等只展示和连接已有能力，不凭UE命名扩大系统范围。

### D10 r4 Ability终结由生命周期代码统一处理

撤销此前未提交的“State.OnExit分派四种终态”方案。它把已决定退出后的回调当成结束决策入口，不能作为实现依据。旧Action Exit Selector中的Ability终结判断应迁回生命周期模块，不能只把Selector换成更小的OnExit节点。

| 责任 | 正式owner | 作者表达 |
|---|---|---|
| 激活、取消与替换/中断准入 | GameplayAbilityDefinition规则与既有准入模块 | 规则、窗口/输入条件及明确请求 |
| 一次执行以何种结果结束 | 同一AbilityExecution的生命周期代码 | 必要的提前结束请求，普通自然完成不要求补节点 |
| 停止子内容、内部OnExit与资源回收 | 现有FSM/State/Timeline运行模块 | 不创建作者清理图 |
| 一条Timeline内部的片段混合 | Timeline动画轨道/片段 | 素材、区间、重叠与局部权重曲线 |
| 跨动作播放接替及其混合历史 | 现有动作Slot转移规则与动画模块 | Slot配置交接，代码维护历史与回收 |
| 基础姿态和动画变量 | Pose动画状态机及动画EventGraph | 状态机配置基础转移，事件图计算变量，不转发全部播放请求 |

结束请求可来自执行根完成、Ability取消规则、正式外部取消/替换或执行错误，只由一个生命周期入口接受和处理。沿现有实例状态表达“已接受结束结果”和“逻辑停止完成”，不建立第二个Ability状态对象或运行状态机。保留原准入与请求竞争规则，以及Complete、Cancel、Interrupt、Abort和对应业务原因；将有效条件迁到决定结束的位置，不从StateExitCause反推结果。

正常结束流程为：接受并记住结果/原因 → 停止尚在运行的内容 → 各owner自动处理状态、Timeline、窗口和执行资源 → 按既有停止完成边界收尾。自然执行根已经完成时不重复退出已停止状态。结束已决定后，内部回调只读取上下文，不覆盖结果或再结束一次。强制Abort复用原ForceStop，必要释放由代码保证，不能依赖普通OnExit一定执行。

当前源码已有执行根成功/失败后的FinishFromControl，以及ActionSkillLifecycleFlow的Submit/Stop/Interrupt入口。OperationStateMachineRuntime在选定转移后先停止StateBody，再执行退出阶段，最后完成状态退出。ActionContextEnded经MapExitCause会落入TreeParentStop，已经不能区分外层Cancel/Interrupt等终态，不能拿它重新决定Ability结束结果。

State.OnExit保留为代码内部生命周期，不作为当前Ability作者编排清理、动画释放或终态的入口。各模块自动释放自己拥有的内容。当前Dodge没有独立退出玩法，迁回结束责任后删除空作者OnExit节点/执行页，不为假想需求保留空包装；合法非Skill内部生命周期按实际消费者保留。

Attack1到Attack2是Ability内部状态转换，不能结束整个Ability。Timeline完成也不必然结束Ability；只有执行根完成或明确结束请求才进入Ability生命周期。窗口/输入条件图保持纯求值，不能通过条件求值或退出回调偷偷写终态。

### D11 r4动画状态机、Timeline、动作Slot与EventGraph的固定分工

只采用以下方案，撤销“所有动画混合都放Timeline”和“所有技能播放/退出必须经过动画EventGraph”的草案。Timeline仍能配置动画，但片段内部混合、跨动作接替和基础姿态转移分别有明确owner，不重复配置同一个过渡。

| 内容 | 作者配置位置 | 执行职责 |
|---|---|---|
| 站立、走、跑、起停、转身等基础姿态 | PoseGraph动画状态机及其转移 | 根据动画变量选择与过渡基础姿态 |
| 一次Ability中的动画素材、区间、片段重叠、WeightCurve、EaseIn/EaseOut | Timeline动画轨道/片段 | 按逻辑时间输出该播放的时间与局部权重 |
| 攻击Timeline到闪避Timeline等不同动作播放的接替，以及动作返回基础姿态 | 对应动作Slot的正式转移规则 | 由现有动画模块从当前混合结果过渡到目标来源 |
| 连续打断留下的旧动作来源、权重历史、容量与释放 | 动作Slot自己的动作混合实现 | 保留必要历史、推进混合并在无贡献后回收 |
| Pose分支确实需要的多来源历史 | 该分支显式BlendStack及其策略 | 仅管理本分支历史，不因存在Slot自动新增全局BlendStack |
| 速度、方向、阶段等动画专用变量 | 动画EventGraph | 从原始事实计算同次动画变量，不成为技能播放/停止的必经转发层 |

固定数据方向：

    Gameplay原始事实 → 动画EventGraph → 动画变量 → PoseGraph动画状态机
                                                         ↓ 基础姿态
    Ability Timeline → 正式动画播放/停止请求 ──────────────→ 动作Slot → 后续姿态处理/输出

Ability内部的Attack1到Attack2可以是Gameplay连段FSM；动画侧不为同一动作时序再复制一套Attack状态机。MovingTurn等基础动画仍由Pose状态机选择，真实Gameplay Motion仍走既有唯一运动链，不因为动画编排变化迁入EventGraph或Pose求解。

Timeline的片段EaseIn/EaseOut描述自身时间区间内的权重，不能替代“在任意时刻被另一Ability接替”的过渡。中途打断不跳到旧片段末尾；对应Slot按源/目标正式规则，从当前输出接入新播放。片段权重与Slot交接权重可以按各自职责组合，但同一次交接的时长/曲线不能在Timeline和Slot分别配置、重复执行。基础动画状态机的边混合仍属于基础分支，不复制到技能Timeline。

例：攻击A尚未完全混向闪避B又进入受击C，动作混合模块保留当前A/B必要历史并接入C，不能清空后从完整B姿态重新开始。已有CharacterAnimationBlendStackPolicy、CharacterAnimationBlendTransitionRule与精确源/目标转移合同继续使用；不让Ability或EventGraph手工维护栈，也不在Slot外再套一层管理相同动作历史的BlendStack。

Ability结束或被打断时，运行代码自动停止其拥有的Timeline/播放，并沿现有CompleteProducer/ReleaseProducer通道进入Presentation及Slot。通过精确执行实例、producer/generation和采样身份定位对应播放，不按名称或一个全局“当前动画”误停其它实例。正常片段完成保留局部EaseOut；无新动作时动作贡献退出后显露动画状态机按当前变量选择的基础姿态，不在OnExit硬播Idle。

玩法停止与表现淡出不同步完成是允许的：旧动画可留纯表现尾部，但旧Timeline不能继续命中、效果或技能Motion；新能力仅等待必要逻辑停止，不因旧动画权重尚未归零被阻塞。真正影响操作的恢复时间仍明确属于Ability逻辑，不用视觉淡出时间代替。强制卸载沿原模块释放，不能依赖普通OnExit回调。

BTSMTL不是骨骼求解器，也不是完全不认识动画的纯逻辑容器：它拥有Ability执行、Timeline逻辑时间、窗口/Motion及动画请求、播放时间和片段权重；最终骨骼采样、跨来源混合历史、Pose组合和资源尾部属于动画模块。动画EventGraph计算变量与确有需要的动画判断；不结束Ability、不反写Gameplay，不在逻辑退出到Slot释放之间增加强制事件转发、全局总线或第二播放控制器。

完整Ability的C#输出保留自有Timeline片段及局部混合参数、指定Slot/表现资源引用；跨动作混合规则由其Slot所在动画owner输出，基础状态机/显式BlendStack配置由对应Pose作者根输出。不会因为导出完整Ability就复制全套角色动画配置；范围外共享资源仍精确引用。

本任务实现Ability/Timeline自动停止及正式播放/结束请求接入，沿已有动画侧转移与混合模块消费，不重做Pose/BlendStack算法。add-flowcanvas-event-graph继续负责原始事实到动画变量，既有动画规划继续负责Pose/Slot配置；这里不额外要求它们新增Ability退出事件仲裁。共享接口存在真实冲突时明确处理，不复制一份同义配置或执行链。

业务取舍：Timeline就地编辑单次动作内容，Slot统一管理不同动作接替，基础状态机管理常态，作者可以按问题找到唯一位置。相较把所有混合塞进Timeline，跨技能过渡不会分散到每个技能；相较让事件图转发所有播放，已有直接请求链不会多一层必需状态。这是本项目的明确分工，不声称UE所有混合也由Slot统一决定。

### D12 r4旧ActionExit清理与规范冲突

保留DodgeBack/DodgeForward已落盘的Native FSM、StateBody、Timeline及已清零根图。把四个旧Submit分支的有效结束条件和请求来源迁入Ability规则或其生命周期输入，保留Complete/Cancel/Interrupt/Abort和原业务原因。结束后的清理由代码正常完成，不以“无活动实例导致Submit失败再走空Succeed”作为作者流程。

责任迁回后删除Action Exit Selector、Action Exit_To_Succeed_Rule空图/layout、被替代Submit实例/连线/私有包装及空作者OnExit页，不用“生命周期分派节点”或另一层Macro替代旧壳。其它图仍有合法消费者的通用Selector或显式结束请求保持边界，Attack连段和真实取消条件不按名称批量删除。Ability生成源码表达最终逻辑规则、执行图、Timeline局部混合与Slot/表现引用，不重建旧清理图或复制跨动作过渡配置。

现行btsmtl-sm-node-authoring要求StateBehaviorSubTree固定生成可编辑OnExit、缺失即非法，与新目标冲突。后续须明确：内部退出阶段与资源释放仍由代码拥有，Ability作者不必提供OnExit图；动画退出策略是播放数据，不是退出回调。此处不取消正常状态转移或合法非Skill生命周期。本次不写current specs或业务代码。

r4由tasks 7.8—7.12承接，已有24项完成记录保持。新增任务只有实现和清理，不含测试、验证或证据收集任务。

## Risks / Trade-offs

- [已有Edge与旧Step不一致] → 输出逐字段与并列顺序冲突，禁止自动选一侧。
- [生成后丢业务身份、条件顺序或根引用] → 完整输出后核对等价重建与删除输出后重建；使用明确生成变量及外部输入，不用旧子资产GUID查找。
- [缺少字段仍宣称完整导出] → 对具体对象/字段明确失败；只补领域读取/配置合同，不复制业务校验。
- [数据层窗口继续修改] → 消费其正式提交与定义，不覆盖其规划或代码；实际同段冲突明确交由用户裁决。
- [观察或网络收尾拖长FSM任务] → 本change只交付原生作者API、C#输出适配、编译、来源映射与重建；下游各自按任务收口。

## Migration Plan

先记录真实差异及有效操作清单 → 将读取/配置/身份/owner/局部校验补入现有FSM模块 → 公共C# authoring接通export/generate并消费领域API → 完整结构/配置/顺序/根引用往返及删除输出后重建 → 人工编辑和生成调用者脱离Agent → 按唯一公共退役计划清理协议并核对本领域Agent依赖归零。Build继续独立，实际运行轨迹由Corin闭环负责。

## Planning Revision

- revision: r4（Ability结束、内部退出清理与动画停止策略）
- baseline: remove-agent-authoring-use-native-csharp/design.md r2，2026-09-13
- scope: 只更新本目录proposal/design/tasks/specs；r2已有勾选和implementation记录保留，新增Ability任务不含验证任务。
- dispatch: 未向规划、实现或协调窗口发送消息；本次不修改业务代码/资产。

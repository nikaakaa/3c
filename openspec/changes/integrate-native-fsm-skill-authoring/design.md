## Context

见[proposal](proposal.md)。2026-09-13按公共[原生C#作者基线r2](../remove-agent-authoring-use-native-csharp/design.md)更新剩余计划。旧实现及任务映射保留在[归档交接](../archive/2026-09-12-refactor-btsmtl-flowcanvas-authoring/split-handoff.md)和本目录历史实施记录；已勾选工作不撤销，但不代表新导出/重建或协议删除完成。

本轮只读源码已确认`BtsmtlSkillNativeStateMachine : FSM`、原生State/Connection适配、`ConfigureIdentity/ConfigureOwner/SetBody/Configure`、工厂创建及`BtsmtlSkillNativeStateMachineContract.Validate/Populate`存在，启动插件runtime仍被拒绝。任务4.3等实际资产收尾尚未勾选；不能用源码或旧Document记录证明当前资产已完成往返。实施前重读实际差异，不从旧截图恢复steps、不覆盖其它窗口正确改动。

## Goals / Non-Goals

**Goals:** 保留已正确原生FSM与Program链；把旧Applier内真实资产操作落到正式Skill/FSM API；让公共C#作者入口完整输出、重建明确FSM拥有范围并恢复根挂接；迁离旧Agent调用依赖。

**Non-Goals:** 不实现公共MCP/通用输出器、中央Validator、整包同步事务、源码解析/自动同步；不重做共同节点定义或端口规则；不承担事件图运行、Pose变量、通用观察、网络和Replay；不恢复Character RootTree，不新增测试。本次仅写规划，不运行Unity、Build或Play，不联系其它窗口。

## Decisions

### D1 状态机换正式资产，执行体保留原业务

| 作者内容 | 唯一存储与owner |
|---|---|
| Skill根 | 独立FlowGraph主资产，Definition只引用 |
| StateMachine | 原生FSM，唯一属于调用它的节点，私有内容仍保存于Skill或共享Macro实际文件 |
| State | FSMState业务适配，唯一引用StateBody |
| Transition | FSMConnection业务适配，唯一保存condition、priority、abortPolicy、order；私有条件由Edge拥有 |
| StateBody、ConditionRule、Macro、TimelineBody | FlowCanvas正式图及原接口，不再镜像状态机拓扑 |
| Sequence/Selector/Parallel/Timeline | 保留现有组合、等待、并行和停止合同，不硬改为FSM状态 |

同一GraphEditor负责创建、导航、原生Inspector、Blackboard provider、复制和Undo。共同业务定义来自数据层change；FSM只增加宿主适配，不维护第二份规则。业务取舍：继续自定义状态图迁移成本较低；本次选择原生FSM以统一状态机作者体验，承担插件扩展与资产类型适配成本，但不接管插件运行。

### D2 原生名字不代表相同执行语义

Entry/Prime只在单一无条件入口时直接表达默认目标；条件/多分支入口保留正式路由，不复制第二份Prime配置。Any保持既有条件准入，Exit表示退出当前FSM调用。OnFSMEnter/Exit是整个FSM的钩子，不是转移端点，也不等于每个State的OnEnter/Root/OnExit；无业务时不创建空包装。

原生FSM无ConditionTask的边表示OnFinish，不能替代BTSMTL无条件边；主体完成仍显式用state-root-completed。原生Stacked/Clean不等于abortPolicy，未登记语义拒绝。ActionList/ConditionTask只允许正式typed定义与编译binding；引用执行体/条件图时只保存该引用，不同时保存另一份同义任务列表。停止源主体、执行OnExit/StateExitContext、处理Action/Timeline取消、进入目标的顺序保持既有Program合同。

插件FSMNode固定outConnectionType，FSM限制变量拖拽。连接参数、能力过滤和provider入口通过正式插件扩展点接入并登记补丁，不用图外可写转移表或旁路Inspector绕过。Inspector禁止做迁移、闭包重建、Build等重操作。

### D3 公共双工具与FSM领域API各自负责什么

公共基线只提供`btsmtl.export_code`与`btsmtl.generate_assets`：前者从当前明确资产完整写出C#，后者执行已编译的正式生成入口并保存指定范围。工具参数、当前编译结果校验、通用输出器与旧五工具退役由C# authoring任务负责，本change不再发布Document v8、不新建FSM工具或同步状态。

生成范围内，已导出的C#是可重建内容来源；人工修改仍先保存在当前资产，只有显式export_code才进入新源码。重新generate_assets不自动合并未导出的修改。导出不读取旧源码做增量合并，不产生源码Undo、rebase、DocumentDirty/Conflict、反向导出或自动Build。业务取舍：完整当前图输出能覆盖实际结构，代价是不保留原创建算法/手写排版，未导出的调整不会被重新生成自动保留。

### D3.1 Applier内真实FSM能力逐项迁出

`Editor/CharacterPipeline/Authoring/SkillDocument/BtsmtlSkillGraphAuthoringApplier.cs`仍有协议DTO、会话、local identity表和以下真实操作混合。此文件的有效FSM操作迁出由本change唯一负责，不允许公共清理任务先整目录删除，也不能把原文件换名搬进领域。

| 现有位置 | 保留能力与已有模块去向 | 退役部分 |
|---|---|---|
| CreateNativeStateMachine / ResolveNativeStateMachineTarget | FSM创建、明确父图/调用节点owner、系统入口初始化；落实到BtsmtlSkillGraphAssetFactory及原生FSM模块 | AgentPackage目标、local表、Session.Touch和按旧包asset identity找生成对象 |
| SyncNativeStateMachine | 状态创建/配置、name/layout、StateBody挂接与无效私有内容回收；复用原生State和工厂API | 对比Document节点列表的增删同步、JObject/包字段分派 |
| SyncNativeAnchors / NativeAnchor | 系统Entry/Any/Exit复用、身份读取和恢复；落实到NativeStateMachineContract/正式身份接口 | 包anchor描述和协议local分支 |
| SyncNativeEdges / ResolveNativeEndpoint | 明确端点、连接/改接、条件引用、priority/abortPolicy/order及identity恢复；复用NativeConnection与领域连接API | AgentPackage Edge、旧包差异删除及默认协议值解释 |
| ValidateNativeOwner / ValidateNativeAppliedIdentityContracts | owner一致、身份唯一/可恢复、条件角色、顺序等真正业务约束；并入已有FSM Contract/GraphClosure/节点配置入口 | Applied包回读、协议格式/hash/sync和重复规则 |

只暴露当前正式业务需要的typed读取/配置方法，不让生成代码依赖AgentPackage、JToken、私有字段反射或SerializedObject路径。校验继续分布在已有FSM/Skill模块，公共导出器只判断输出是否完整，不重新维护业务规则。

当前代码创建缺失State/Edge时要求`local:`，系统入口主要校验已有UID；这不足以证明删除后可按指定业务identity重建。生成所需的identity恢复应成为正式领域接口，不能复用协议local表或依赖旧子资产GUID。工厂当前会填入系统入口和默认StateBody，输出/重建必须复用这些实际对象或经明确领域创建合同一次建立，不能重复创建后靠按名字合并。

### D3.2 完整输出与生成范围

输入是明确FSM/Skill根、所属Definition/owner上下文、生成范围和范围外精确资源。由公共C# exporter收集FSM、StateBody、ConditionRule、内部Macro、合法组合/Timeline引用与layout等正式拥有闭包；数据由本领域读取合同提供，不先转JSON。按对象身份在本次调用内去重，状态机有环不按执行边递归打印。

输出顺序为：创建根/子图/状态及复用系统入口 → 配置业务identity、参数、声明、动态端口和布局 → 绑定Body/Condition/Macro等内部对象与精确外部资源 → 按明确端点创建连接并恢复identity、priority、abortPolicy、order → 显式恢复指定Definition/Skill入口及根引用。字段依赖对象存在时延后配置，不生成持久化Mutation Plan。

内部共享对象只创建一次并用生成局部变量引用；共享但在范围外的资产作为精确外部输入，不因为可达就复制、覆盖或删除。只有真正外部资源使用资源identity；不能把同批待重建子资产的旧GUID/local file ID当依赖。内部条件、StateBody、Macro和系统入口的业务identity/order必须保留，物理对象、GUID和实例可以替换。

每个正式状态字段、生命周期引用、原生配置、条件、边顺序、动态接口、布局和根绑定都须有输出/恢复办法；没有对应调用时报告精确对象/字段/原因，拒绝完整导出，不静默丢字段或临时用插件runtime解释。公共写出器在完整性检查成功后写源码，FSM适配只提供所需读取与调用描述。

### D3.3 原模块的保存、失败和Build边界

人工编辑继续走已有BtsmtlSkillFlowEditorMutation的领域Undo/保存。生成代码调用同一正式API，按现有资产模块建立与保存声明范围、返回实际创建/替换/清理和失败结果；不新建跨领域整包事务或中央Validator，不把部分生成报告为完整成功。

删除后重建必须恢复指定Definition/Skill根挂接，不能只生成孤立FSM。清理只覆盖明确旧输出，不删除源码、原始素材、外部共享资源或未列入范围的消费者；范围外消费者若需重挂，须作为本次明确目标输入。首次迁移先完整导出并取得重建证据，再清理旧输出，不因资产可重建直接删除现有正确资产。

Build独立显式执行，仍由现有Character编译器读取正式资产产生Program/Projection。声明范围的结构/配置往返不等于运行测试，文档更新不执行生成、删除、Build或Play。

### D4 编译只汇入现有Program

去除Occurrence对全部节点为FlowNode、全部边为BinderConnection的假设。不同图适配各自真实资产，向同一只读编译输入提供业务参数、引用与来源；不先重建旧FlowGraph。保留状态/边identity、逻辑端点、调用路径、条件、priority、abortPolicy和有效order；不按位置、随机UID或插件数组顺序重排。必要资产对象重建在同一事务完整重映射并报告。

本change只提供原生State/Connection到Program SourceMap及只读绘制接口。实例选择、导航会话、订阅生命周期和采样性能归观察change；两者共用正式诊断，不为显示高亮启动FSM。网络与Motion/Body/WorldSolver执行链不变。

### D5 Corin按消费者清理，不借迁移改规则

| 对象 | 处理 |
|---|---|
| Attack Startup Branches、OnEnter Action Setup、Clear意图写入 | 删除实例、边、无人读取的图内声明及失去引用的私有空条件/layout，Root直连状态机 |
| Attack连段与15条Exit边 | 保留所有条件/排序/中止/identity；摘要显示主体完成、可闪避且收到请求、移动取消、连段目标 |
| Dodge Setup/Enter/Set | 确认全部typed消费者后删除无消费写入副本，保留Body/Exit Monitor并行 |
| Hit/Recovery/IFrame和ActionTarget | 按Timeline、Frame Fact、targetSnapshot、输入绑定和编译引用保留，不以缺少Get判定无消费 |
| StopThreshold | 保留现有有效值和引用；Attack的声明仍被两个Dodge使用，未决定新归属前不删除或临时替换 |
| 正式跑步意图 | 保留ControlModule在Idle清理、DodgeForward完成后置位的时机，不新增攻击开始清理行为 |

StopThreshold若统一ControlModule阈值可保持移动/取消一致；Skill私有取消阈值可独立调手感。二者是未来业务变更，不是本次迁移的隐藏选项，本change不以选择未定永久阻塞FSM交付；清理验收明确排除仍有消费者的正式声明。Corin控制文件中的MovingTurn来源和60Hz时长改动不覆盖。

m_Name问题单独取得完整错误、实际类型和继承链后修复；目前命中的普通SkillStepPort字段不足以确认MonoBehaviour错误来源，不全局改名或清缓存。若证实属于其它领域，记录精确归属，不扩大FSM修复范围。

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

## Risks / Trade-offs

- [已有Edge与旧Step不一致] → 输出逐字段与并列顺序冲突，禁止自动选一侧。
- [生成后丢业务身份、条件顺序或根引用] → 完整输出后核对等价重建与删除输出后重建；使用明确生成变量及外部输入，不用旧子资产GUID查找。
- [缺少字段仍宣称完整导出] → 对具体对象/字段明确失败；只补领域读取/配置合同，不复制业务校验。
- [数据层窗口继续修改] → 消费其正式提交与定义，不覆盖其规划或代码；实际同段冲突明确交由用户裁决。
- [观察或网络收尾拖长FSM任务] → 本change只交付原生作者API、C#输出适配、编译、来源映射与重建；下游各自按任务收口。

## Migration Plan

先记录真实差异及有效操作清单 → 将读取/配置/身份/owner/局部校验补入现有FSM模块 → 公共C# authoring接通export/generate并消费领域API → 完整结构/配置/顺序/根引用往返及删除输出后重建 → 人工编辑和生成调用者脱离Agent → 按唯一公共退役计划清理协议并核对本领域Agent依赖归零。Build继续独立，实际运行轨迹由Corin闭环负责。

## Planning Revision

- revision: r2
- baseline: remove-agent-authoring-use-native-csharp/design.md r2，2026-09-13
- scope: 只更新本目录proposal/design/tasks/specs；已有任务勾选与implementation记录不视为新计划完成证据。
- dispatch: 未向规划、实现或协调窗口发送消息；本次不修改业务代码/资产。

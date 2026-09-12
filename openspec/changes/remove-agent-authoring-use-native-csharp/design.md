## Context

动机见 [proposal.md](proposal.md)。本设计采用用户纠正后的方向：Agent 层整体退役；不把专属 Validator、Session、Reconciler 或事务编排改名保留。

已核对的当前代码链：

| 位置（Unity 项目 `Assets/` 下） | 实际职责与证据 |
|---|---|
| `ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowGraph.cs` | 提供 AddFlowNode / CreatePortConnection，节点创建调用 AddNode |
| `GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillFlowGraph.cs:193` | 项目重写 AddNode / CreatePortConnection，已经检查节点登记和端口并进入 Skill 编辑事务 |
| 同目录 `BtsmtlSkillFlowEditorMutation.cs:222` | 已有 Graph 序列化、Undo、闭包校验和私有资产清理；不是 Agent 专用能力 |
| `GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts/TimelineData.Runtime.cs:288` | AddTrack / AddClip / AddSection 已有正式 catalog 与 placement 约束 |
| 同文件 `:445` | ApplyModify 记录 owner Undo 和 dirty，不等于跨资产磁盘原子提交 |
| `GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/BtsmtlSlateTimelineProjection.cs:630` | Slate Cutscene 为 HideAndDontSave 临时投影，编辑回写正式 TimelineData |
| 同文件 `:914`、`:946` | 人工新增 Clip 仍构造 JObject，再调用 Timeline binding，删除 JSON 需一起迁移 |
| `GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillNodeAuthoringBinding.cs:23` | JSON resolver / Apply 包装了现有节点 Configure / Set；这些 JSON 包装可删 |
| `GameScripts/Main/Editor/CharacterPipeline/Authoring/SkillDocument/` | applier 已调用原生 AddNode / CreatePortConnection，但以 AgentPackage DTO 驱动，不是可直接搬走的公共服务 |
| `GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/AgentCharacterAuthoringValidator.cs:69` | 一边调用 Document mapper，一边转发正式 Build DryRun；删除前者和包装，直接使用后者 |

当前工作区相关 Skill、Timeline、Pose、Agent 和规范有其他任务的改动；以上是静态代码证据，不表示本轮编译、运行或保存重载验证通过。

## Goals / Non-Goals

**Goals:**

- 正式 C# 可以创建、配置、连接、删除现有 authoring 对象；人工编辑与其结果相同。
- 删除 Agent 协议、专属业务投影和重复校验，使业务入口自身保证合法性。
- 覆盖原 Document 可写的全部已存在业务能力；不能只完成节点创建就删掉 Profile / Curve 的唯一实际写入能力。
- 保留真实资产与稳定身份，删除无消费者旧代码，形成可解释的唯一修改链。

**Non-Goals:**

- 不做 C# 全量内容声明、C# 与资产双向同步、自动 regenerate、通用 authoring DSL 或任意 C# 执行服务。
- 不替换 FlowCanvas / NodeCanvas / Slate，不把 BTSMTL Timeline 改成 Slate 数据源。
- 不迁移角色运行逻辑、Program / Projection ABI、游戏内 Behavior Designer AI 或其他业务系统。
- 不借此扩大到 Rig/Body Motion/Foot Analysis/generated artifact 编辑、素材创建或对 TrainingEnemy 的修改。
- 不新增测试，不自动触发 Unity、Build、Play 或向实现窗口下发任务。

## Decisions

### D1. Unity 资产是内容来源，C# 是普通编辑代码

链路固定为：`C# 调用 / 人工交互 -> 正式领域 API -> Unity authoring 资产 -> 显式正式编译`。C# 保存到 Editor 程序集并经 Unity 编译后执行，不通过 JSON，不直接改 YAML、私有序列化字段或集合来绕开 API。

调用输入是精确 Definition、实际 Graph / Timeline / Profile、稳定身份和强类型资源；输出是同一批被修改资产及现有业务诊断。使用已有明确 Editor 方法/命令入口执行具体操作，不新增“选择任意脚本并解释”的通用窗口或监听器。写入 `.cs` 不代表已经修改资产。

| 方案 | 业务收益 | 业务代价 |
|---|---|---|
| 资产为来源，C# 显式修改（采用） | 作者可以拖 Clip、调曲线，AI 下一次接着现状修改 | 重现完整内容仍需资产版本；删一行 C# 不自动删资产 |
| C# 为全量来源，界面只读 | 可以从完整源码生成全部内容 | 需要长期生成器与声明所有权；作者不能自由微调资产 |

这里不增加全量声明和对账，避免删 JSON 后又用 C# 重做一遍相同系统。

### D2. 按业务规则去重，删除 Agent Validator

每项 Agent 校验只做以下归类，结果记录在实现文档，不新建一套可执行规则注册表：

| 发现的代码 | 处理 | 正式归属 |
|---|---|---|
| JSON schema、manifest、hash、sync state、local identity、package path、planned diff | 随协议删除 | 无继任模块 |
| 重复的节点准入、端口、Track/Clip、Pose 或资源检查 | 删除重复实现 | 已有正式节点/Graph/Timeline/Presentation API |
| 转发编译诊断、Definition 语义校验 | 删除 Agent 报告包装，直接调用现有正式入口 | Character 编译器与正式诊断入口 |
| 仅在 Agent 中实现的真实业务约束 | 先证明其他正式入口缺失，再补到该业务 API/validator | 例如 Timeline owner、Clip Curve 数据组、Action target 引用所属模块 |
| 只为 Agent 样例打分、修复轮数或目录包覆盖统计 | 删除 | 不变成全局业务验收要求 |

Graph 检查节点与连接；Timeline 检查片段、帧、通道、引用与曲线；Pose 检查自身节点/拓扑；编译器检查整个 Definition 可否生成合法产品。普通 UI 不每次刷新都跑完整编译。

| 方案 | 业务收益 | 业务代价 |
|---|---|---|
| 直接使用领域校验（采用） | 人工与代码都在出错的业务入口得到相同错误，新增能力无需再登记 Agent | 需要清点过去放错位置的规则 |
| 保留中央 authoring Validator 聚合所有规则 | 单入口可汇总所有报错 | 容易复制领域规则，业务再次依赖总管；这正是本次要消除的结构 |

不得生成新 `CharacterAuthoringValidator` 仅为了接收搬家的 Agent 代码；已有正式整角色诊断入口可以继续调用各领域规则。

### D3. 业务 API 保留，JSON binding 删除

Skill 调用正式 Graph / FSM / Macro 和节点 Configure / Set，复用现有 capability、系统入口、Blackboard 与动态端口规则。Timeline 调用 TimelineData 创建函数与正式 Track / Clip 配置接口。Presentation 复用现有类型化 Mutation、资源与曲线服务。

删除 `BtsmtlSkillNodeAuthoringBinding` 的 JSON Apply/Export/Parse/Resolver；如果其分支仅调用现有 Configure，直接移除包装。需要新增公共方法时，以明确业务输入输出定义，不接受 JObject、任意字段字典、反射字段路径或另一套 package DTO。

`TimelineAuthoringClipBinding` 与 Slate `BuildClipProperties` 同时转成强类型配置。人工 popup 中确实需要的短期输入草稿可以保留，直接对应正式类型，不扩张成全量资产目标模型。新 Clip 类型的业务校验在本类型及正式 catalog 内扩展，不注册 Agent handler。

| 方案 | 业务收益 | 业务代价 |
|---|---|---|
| 直接复用正式类型（采用） | 类型错误可在编译时发现，业务字段只有一份 | 公共 API 必须表达完整配置，少量缺失入口需补齐 |
| 保留 JSON binding 作为通用适配 | UI 可继续拼接任意字段字典 | 仍需字符串字段映射和运行时解析，不符合删除中间层的目标 |

### D4. Slate 只负责现有界面，TimelineData 继续负责内容

Slate 的 `Cutscene.Create / AddGroup / AddTrack / AddAction` 创建 Slate 组件层级，不直接产出 BTSMTL Timeline。当前临时代理和回写方向保持，C# 不绕道创建代理再提取 BTSMTL 数据。

| 方案 | 业务收益 | 业务代价 |
|---|---|---|
| 保留 TimelineData + Slate 界面（采用） | 保持帧域、channel、TreeClip、MotionWarp 和已有编译链 | 保留必要的 UI 投影适配 |
| Slate 成为正式来源 | 作者可以直接使用 Slate 自身资产模型 | 要迁移整个 Timeline 数据、owner 和编译语义，属于独立业务决策 |

删除本次 JSON 工具，不删除插件内部序列化或与 Agent 无关的 JSON 文件。

### D5. 身份、重复执行与删除由具体操作表达

- 原位修改已有对象，保留 Unity GUID/local file ID 以及 Graph、Node、Edge、Timeline、Track、Clip 身份；不清空重建整图。
- 创建操作返回新对象。Add 本来就表示创建，不能宣称任意 C# 重复执行都自动幂等。具体“创建某项固定内容”操作应按明确 owner 与稳定身份判断：不存在则建，已经存在则明确报告，不靠名称猜测；配置操作可重复设置同一值而不更换身份。
- 不新建通用 Ensure/Reconcile 框架、持久 local/stable 映射或 shadow target。需要复用既有稳定身份构造函数时遵守各领域格式：Skill 当前 GUID N 与 Timeline GUID D 不可混用。
- 作者修改过的内容是下一次合法输入。具体操作只改明确字段；旧期望与当前值冲突则报出，不默默覆盖，更不为此恢复整包 hash / rebase 协议。
- 删除必须显式指定对象与范围。先解除本次授权范围内的引用，再由正式 API 删除；共享资产不能因为单个引用者解除就销毁。没有消费者的私有页沿现有 owner 清理。

### D6. 使用现有编辑事务，不重造全角色同步事务

单图编辑继续使用 Skill 编辑事务；Timeline / Pose 使用自身 owner 和正式修改入口。一个实际业务操作同时修改 Graph、Timeline、Profile 时，才在相同 Undo 分组下组合这些 owner，并复用已有创建/删除/恢复能力。

不整包迁移 `AgentMutationSession`、Document transaction 或遍历全角色的 collector。保留真实业务需要的 Graph 序列化、owner 移动和清理逻辑，归到现有所属领域；若已有实现直接复用。组合操作不能让内层提前保存部分结果，也不能在单个节点操作时扫描所有角色资产。

保存前运行该操作需要的正式校验，保存后报告实际成功或失败。Undo 管理内存编辑，不等于多资产文件磁盘原子性；现有保存失败如果不能完整恢复，应准确报告受影响资产，不能声称所有文件已原子回滚。本次不新建持久化事务 journal 或通用 C# dry-run 沙箱。

| 方案 | 业务收益 | 业务代价 |
|---|---|---|
| 按具体操作组合现有事务（采用） | 保持作者一次操作一次 Undo，不让小改动承担整个角色扫描成本 | 跨 owner 操作需要明确其实际闭包 |
| 每次 C# 都进入全角色事务 | 任意批量目标容易统一收集 | 重建被删除的总事务与 snapshot，范围大且容易卡编辑器 |

### D7. MCP authoring 为零，Build 保持独立

删除 `btsmtl.checkout_document / rebase_document / dry_run_document / apply_document / validate`、专属 scheduler、Agent Window 和导航。现有 Character Build MCP、Preview MCP、Unity MCP 不属于本次删除对象；它们仍调用各自正式服务。

C# 通过既有明确 Editor 执行方式调用正式 API。本次不另造自动执行通道；未来若明确需要自动化，只能调用正式入口，不引入任意 eval、节点级 MCP 或脚本监听。修改资产与显式 Build 保持两个步骤。

### D8. 删除覆盖到目录外的依赖，不吞掉既有业务限制

| 删除对象 | 必须核对的真实业务去向 |
|---|---|
| AgentAuthoring 全部类型与专用 assembly/tests | 业务校验已在正式模块；Inspector 不再打开 Agent Window |
| SkillDocument / PresentationDocument 的 DTO、Exporter、Mapper、Reconciler、Applier | SkillDefinition、FSM、Macro、TreeClip、Control、Action target、Pose/Profile、Linked Pose、Curve 等既有业务都有正式直接调用 |
| 节点和 Clip 的 JSON binding | typed 节点配置、值端口、资源引用、曲线和人工 popup 同步迁移 |
| `.btsmtl` 工作包、专属技能与协议说明 | 正式资产保留；若实际存在未应用用户内容，列出具体差异请用户决定，不自动丢弃 |
| Agent source、Document 测试、专用报告 | 无消费者即删除，不保留空 facade、转发 alias 或备用开关 |

领域界限仍生效：Foot Analysis 产物不变成编辑输入，22 条 Foot Motion Curve 保持正式整组约束，Clip 秒域与 Timeline 帧域分开；Action target、Animation channel、FSM edge ownership 保留原规则。删除 Document 是移除传输层，不自动授权重写 Rig/Body Motion、创建 AnimationClip、运行 Analyzer 或改变角色数据。

## Risks / Trade-offs

- [某条真实业务规则仅存在 Agent 中] → 按 D2 对照正式消费者后归属；不整包迁移，也不丢弃业务约束。
- [删除 JSON binding 导致人工新增 Clip 失效] → C#、Slate popup 与 Clip 配置在同一次切换中使用强类型入口。
- [重跑创建代码生成重复内容] → Add 语义明确；具体固定创建操作使用稳定身份并报告已存在，不宣称所有 C# 幂等。
- [跨 owner 保存和恢复能力不足] → 明确实际操作范围和现有机制的限度，失败不报成功；不能用临时资产或另一个事务系统绕过。
- [运行中的其他作者重构发生冲突] → 重读具体文件和已确认合同，保留已正确的业务改动；只有冲突项交用户决策。

### 现行 spec 对比

本提案的 delta 删除三份 Agent 专属规范的全部 requirement；其中仍有效的独立 Build 工具条款移入 `btsmtl-compiled-simulation-program`，跨模块业务限制由新直接 authoring 合同和原有领域规范承接。其余 14 份现行能力只修改 Agent/Document 依赖和相关场景，保留无关业务正文。

明确冲突：当前 `Agent Validator必须…`、`Document必须降低为…`、`五个MCP…`、`Corin资产迁移必须通过正式Agent Document事务` 均与本提案相反，不能同时作为实施合同。提案阶段只写增量，现行 spec 尚未切换。

另发现现行 `agent-character-controller-synthesis` 的 Foot Analysis 场景要求在 apply 内 Build，与现行独立 Build 规范冲突；退役该场景，保留显式独立 Build。现行 Pose 直接资源/Slot 表述与部分近期代码亦有差异，本 change 不裁决或回滚该业务，只删除其中 Agent 适配职责。

### active change 对比

| 关联 change | 已看到的冲突 | 本任务边界 |
|---|---|---|
| `integrate-native-fsm-skill-authoring` | 3.1/3.2 已扩展 v8；4.3 仍要求反向导出和 Clean | 保留原生 FSM、edge、owner、编译工作；Document 收尾在本方案生效后退役，不能再补回 |
| `refactor-agent-authoring-attribute-driven` | 保留 JSON、五工具、AgentMutation 为原目标 | 保留真正的共享 metadata；协议目标由本变更替代，历史完成记录不篡改 |
| `unify-skill-authoring-data-model` | 包含共同节点定义的 Document 消费适配 | 保留共同业务定义，删除与协议唯一消费者绑定的适配 |
| `add-flowcanvas-event-graph` | D8 明确要求新增 event-graphs 包、Exporter、Reconciler、schema 发布 | 事件图与变量运行合同不归本任务；其 Document 扩展与本变更直接冲突，需相关规划正式收敛，不能偷偷改它的计划 |
| `restyle-timeline-editor-slate-style` | 同一 Slate projection / Clip popup / 保存交互在改动 | 保留已正确 UI 交互；仅修改 JSON binding 的调用边界，具体冲突交用户判断 |

上表不是对这些任务的实施优先级排序。本提案不修改它们的规划文件，也不自动创建协调窗口。新建方案不等于这些交叉合同已经解决。

## Migration Plan

1. 固定实际源码、资产和规范基线，核对 active change 的 Agent 扩展冲突；只登记本次改动，保留其他工作成果。
2. 清点 Agent 校验、资产写入和目录外消费者，分别标为协议删除、重复删除、已有业务复用、确实缺失的领域补齐；不生成执行时注册表。
3. 在现有 Graph / Timeline / Presentation API 补齐所需 typed 配置和具体 owner 操作，人工入口同步接入；不提供第二套实现。
4. 删除 Agent 目录、Document 领域适配、JSON binding、MCP、Window 与失效导航。保留现有 Unity 资产及身份，不做全量重建。
5. 清理专属测试、程序集引用、工作包和 skill；更新项目入口与现行规范。只有有实际用户未应用数据或业务合同冲突时，暂停该删除项等待裁决。
6. 进行依赖、引用、规范与适用编译检查，维护执行记录与中文小步提交。按项目规则，dotnet 编译必须禁用 build server 并在结束后 shutdown；本轮不执行编译。

回退仅以本 change 的独立提交恢复代码和相应规范，不保留运行时开关、兼容包或旧新双写。涉及用户资产时不自动回退其编辑。

本轮交付 proposal、design、spec delta 与细分 tasks；任务中不写手动验证，不新增测试。端到端由用户验收；静态规范校验不代表 Unity 编译或运行通过。

## Workflow Binding

- planning_revision: r1
- planning_status: awaiting_review
- planner_thread_id: 01a09634-fc59-7192-8cda-25fdd142b82d
- implementation_thread_id: 01a09635-2024-74b2-98b4-28c1e17d548b
- implementation_status: WAITING_FOR_PLANNING_DOCUMENT
- planning_document_paths: 当前目录 proposal.md、design.md、specs/、tasks.md
- implementation_document_path: 当前目录 execution.md（由已有实现窗口在收到正式授权后创建）
- confirmed_by_user: false

原 `docs/agent-authoring-csharp-evaluation-2026-09-12.md` 只保留指向本提案的索引。用户本次只授权 openspec-propose；不下发 IMPLEMENT_FROM_DOCUMENT，不启动 apply，不创建额外任务。

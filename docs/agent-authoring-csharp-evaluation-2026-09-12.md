# AgentAuthoring 移除与 C# authoring 评估

## 状态与任务绑定

- 版本：r1，2026-09-12，待用户确认的评估与条件实施方案。
- 目标：判断 Agent JSON / Document / MCP 是否还需要，给出删除边界及正式 C# 创建链。
- 规划窗口：`01a09634-fc59-7192-8cda-25fdd142b82d`。
- 实现窗口：`01a09635-2024-74b2-98b4-28c1e17d548b`，状态仍为 `WAITING_FOR_PLANNING_DOCUMENT`。
- 项目：`local-9736456dbc652f9fbe79876a1464c9fb`，`D:/Unity_Project_1/3C`。
- 唯一规划文档：本文件。确认后的实现记录预留为 `docs/agent-authoring-csharp-implementation-2026-09-12.md`，本轮不创建。
- 本轮只读取当前工作区并写此文档；未改代码、资产、skill 或现行 spec，未启动 Unity、编译、测试或运行验证。
- 本文没有实施授权。尚未记录 `confirmed_by_user: true`，未发送 `IMPLEMENT_FROM_DOCUMENT`。

## 1. 结论与建议

**可以移除项目的 AgentAuthoring JSON 目录包、Document 同步层和五个 BTSMTL MCP authoring 工具。不能把这些目录里的代码不加区分地整体删除。**

FlowCanvas 已提供节点和连线创建函数，项目也已经实际调用。BTSMTL `TimelineData` 已提供带业务合同的 Track / Clip / Section 创建函数。AI 编写 C# 后，经 Unity Editor 执行这些正式 API 即可修改并保存同一批资产；JSON 不是创建这些资产的必要条件。

建议选择：**Unity authoring 资产继续是唯一内容来源，C# 是显式创建、批量修改入口，Graph / Timeline / Pose 人工编辑器继续可写。** C# 操作完成后，资产中的现状就是下一次编辑的起点，不存在要求资产持续匹配某份 C# 全量声明的后台同步。

删除 Agent 层所节省的是目录包导出、字段映射、schema 演进、hash 对账、local identity 反向发布及 MCP 生命周期维护。仍然需要的业务责任是：合法节点与端口、资源类型、owner 关系、稳定身份、删除引用清理、Undo、保存、失败恢复和正式编译校验。这些责任必须归到正式 authoring 模块，供人工界面和 C# 共用。

**Slate 原生 builder 不适合作为当前 BTSMTL Timeline 的正式来源。** 它创建的是 Slate `Cutscene / Group / Track / ActionClip`，当前项目保存和编译的是 BTSMTL `TimelineData`。继续用 Slate 提供编辑界面，创建和提交内容仍走 BTSMTL API。把 Slate 改成正式来源会额外引入整个 Timeline 数据与编译链迁移，不能作为“删除 Agent 工具”的顺带清理。

## 2. 三层代码各自负责什么

| 层 | 当前代码与输入输出 | 删除 Agent 后的处理 |
|---|---|---|
| 插件原生创建能力 | FlowCanvas `AddNode / AddFlowNode / CreatePortConnection` 接收节点类型、位置、端口，创建图对象；Slate builder 接收片段类型和时间，创建 Slate 组件层级 | 保留。它们不负责角色技能、动画通道、共享资产 owner 或 Character 编译合同 |
| 项目业务 authoring API | `BtsmtlSkillFlowGraph`、Skill factory / closure / mutation、`TimelineData`、Timeline binding / catalog、Presentation Mutation 接收正式类型和资源，修改正式资产 | 保留并整理；这是 C# 与人工界面的共同入口 |
| Agent 适配和协议 | Document codec / store / exporter / reconciler，把 JSON 目录包解释为资产修改；MCP / Agent Window 触发生命周期 | 删除 JSON 协议部分；把仍需要的事务、校验、owner 收集和业务修改职责迁出后删除 Agent 类型 |

不能把“AI 直接写 C#”解释成 AI 任意改内部字段、Node 列表或 Unity YAML。C# 应调用现有正式类型的 `Configure / Set / Add / Remove` 及事务入口，缺少公开的业务操作时，补齐该领域的正式 API。

## 3. 当前工作区证据

以下均为本轮读取的代码证据，不代表当前 Editor 编译通过或运行行为已验证。链接指向仓库文件，行号对应本轮读取时的位置。

| 证据 | 观察到的行为 | 对方案的影响 |
|---|---|---|
| [FlowGraph.cs][flowgraph]，64、74 行 | 原生 `CreatePortConnection`、`AddFlowNode`，后者调用 `AddNode` | 函数式创建图已经可用 |
| [BtsmtlSkillFlowGraph.cs][skillgraph]，193–233 行 | 重写 AddNode / CreatePortConnection / Remove；检查节点登记和端口，进入 `BtsmtlSkillFlowEditorMutation` | 应创建正式 Skill 图，不用裸 FlowGraph 绕开业务约束 |
| [BtsmtlSkillGraphAuthoringApplier.cs][graphapplier]，372–413、620–660 行 | JSON 目标最终调用 `graph.AddNode`、`graph.CreatePortConnection`；按 identity 增改删 | 当前 Document 是 API 上方的适配层，不是原生创建能力的来源 |
| [BtsmtlSkillFlowEditorMutation.cs][skillmutation]，222–284 行 | 序列化 Graph，记录 Undo owner，执行修改，检查闭包，释放无引用私有资产，dirty；异常时反序列化恢复 | 有现成正式图事务，不应再写一个 AI 专用图事务 |
| [BtsmtlSkillGraphAssetFactory.cs][graphfactory]，17、110、129 行附近 | 稳定种子函数、共享 Macro / 私有 Timeline 创建；部分 factory 自己 `SaveAssets` | 可复用所有权创建逻辑，但跨 owner 操作前必须统一保存边界 |
| [TimelineData.Runtime.cs][timelinedata]，288–353 行 | `AddTrack` 检查 catalog；`AddClip` 检查 placement 并撤销失败的新增；`AddSection` 拒绝重名 | 正式 Timeline C# 创建入口已存在，但 Add 不是重复执行安全的 Ensure |
| [TimelineData.Runtime.cs][timelinedata]，445–452 行 | `ApplyModify` 只记录 serialized owner 的 Undo，执行 action 后 dirty；此方法没有完整校验、异常恢复或保存 | 不能把现有 Timeline Apply 当作完整跨资产事务 |
| [TimelineEditorSessionContext.cs][timelinesession]，280–285 行 | 人工界面 Apply 调用 `Timeline.ApplyModify` | Timeline 人工入口必须一起接入整理后的事务边界 |
| [BtsmtlSlateTimelineProjection.cs][slateprojection]，630–730、767–920、1229–1388 行 | 生成 `HideAndDontSave` 的 Slate 代理；创建轨道、片段和编辑 diff 回写 `m_Request.Timeline` | Slate 是编辑投影，BTSMTL 数据才是修改目标 |
| [Cutscene.cs][cutscene]，1010、1079 行；[CutsceneGroup.cs][cutscenegroup]，446、477 行；[CutsceneTrack.cs][cutscenetrack]，193、315 行 | Slate 有 Create / AddGroup / AddTrack / AddAction，包含 Editor 与非 Editor 分支 | “Slate 有 builder”成立；“builder 产出 BTSMTL Timeline”不成立 |
| [BtsmtlSkillNodeAuthoringBinding.cs][nodebinding]，23–45 行；[TimelineAuthoringClipBinding.cs][clipbinding]，57、151、199 行 | 所谓正式 binding 仍使用 `JObject / JToken` | 纯 C# 入口需要强类型化这些边界，不能仅删除 Agent 目录 |
| [BtsmtlSlateTimelineProjection.cs][slateprojection]，914、946–976 行 | 人工新增 Clip 也经过 JSON property 构造和 binding | 删除 JSON binding 前必须同步迁移人工新增界面 |
| [AgentAuthoringDocumentTransactionService.cs][transaction]，219–283、967–1010 行 | 总 Undo、领域校验、SaveAssets、反向导出、创建路径清理和失败恢复集中在此 | JSON 发布可删除；资产事务职责必须迁出 |
| [AgentCharacterAuthoringValidator.cs][agentvalidator]，20–88 行 | 校验 Definition，并调用正式 Build Orchestrator DryRun；仍引用 Skill Document mapper | 保留领域校验和精确编译入口，删除为 Document 而生成的验证投影 |
| [CharacterPipelineDefinitionEditor.cs][definitioneditor]，345 行 | Inspector 导航打开 Agent Controller Window | 删除工具需同时删导航，保留正式 Graph / Timeline / Profile 导航 |

## 4. 内容来源：需要用户选择的两个有效方案

这两个方案是不同作者工作方式，不能混成同一资产上两个可写真相。

| 决策 | A：资产为来源，C# 显式修改（建议） | B：C# 全量声明为来源 |
|---|---|---|
| 作者如何调动作 | 可在 Timeline 拖时间、改曲线；AI 下一次读取现有资产再修改 | 在 C# 修改并重新生成；生成范围内的可视化界面只读观察 |
| C# 的含义 | 一次明确编辑意图，可以执行创建、修改或删除；不是长期全量内容副本 | 长期维护的完整构建描述，资产是派生产物 |
| 删一行 C# | 不代表删除已经保存的 Clip；必须显式调用删除操作 | 在声明拥有的完整范围中代表删除，生成器必须自动清理旧对象 |
| 重复执行 | 按稳定身份执行相同目标设置，结果不新增重复对象；不重建整图 | 重新对账完整声明，保留不变实体的身份并清理未声明内容 |
| 资产被人工改动 | 是合法的新现状，旧 C# 意图若与之冲突应明确拒绝 | 拒绝对生成范围直接编辑，否则下一次生成会覆盖 |
| 业务收益 | 保留调手感的直接操作；AI 批量修改与人工细调可以交替进行 | 空工程可重建全部声明内容，代码审查直接表达内容差异 |
| 业务代价 | 不承诺仅凭某份旧 C# 就能恢复所有人工调整；资产仍需版本管理 | 拖动调参变为改代码、编译、再生成；必须维护完整生成器和生成所有权 |

B 可以彻底去掉 JSON，但无法同时去掉“全量目标与现有对象对账”的工作：这项工作将由 C# 构建服务承担。若选 B，本文第 5 节的操作式入口需修订为完整声明范围及清理规则后才能实施。不得在 A 的链上顺手增加后台全量重建。

不建议双向 C# 代码生成：把人工图编辑反向翻译成任意 C# 涉及控制流、变量、资源表达等问题，维护成本超出删 Agent 工具本身，也会再次引入同步系统。

## 5. 最小正式 C# authoring 链（按 A）

### 5.1 输入、输出与变化

```text
AI 编写受版本管理的 Editor C# authoring 操作
  -> 作者显式执行正式 Editor 入口，传入精确 Definition
  -> Character authoring 事务收集本次 owner 和修改基线
  -> Skill / Timeline / Presentation 领域 API
  -> FlowCanvas 对象、BTSMTL TimelineData、正式 Profile / Pose 资产
  -> 领域校验 + 引用闭包校验
  -> 序列化并保存本次修改的 authoring owner
  -> 返回资产路径、稳定身份、实际修改和错误报告
  -> 需要运行产物时另行显式调用现有 Character Build
```

- 输入：精确 `CharacterPipelineDefinition` 对象或 `Assets/...` 路径、本次允许修改的 owner、已有对象的稳定身份、正式资源对象引用、强类型参数和已读取的修改基线。
- 输出：保存后的正式资产及其现有身份，实际新建／改动／删除清单，校验与保存结果。没有 JSON 目录包、canonical export、runtime payload 或第二个 Graph 模型。
- 处理前：Agent 必须 checkout、修改 JSON、对账、apply，再反向导出。
- 处理后：C# 与作者点击都调用正式业务操作，保存同一对象；减少表示转换，保留业务约束。
- C# 仍须在 Unity Editor 中编译和执行。系统文件工具负责写 `.cs`；普通 shell 中的 `dotnet` 不能替代 `AssetDatabase` 执行环境。
- 正式启动入口按明确 Definition 执行，没有 selection 猜测、文件监听、脚本重载自动应用或 `OnInspectorGUI` 重操作。

### 5.2 模块职责与公共契约

以下是建议的责任划分，不表示这些新名称已经存在，也不要求为每行新增一个类。

| 模块 | 输入 → 输出 | 本模块拥有的责任 |
|---|---|---|
| Character authoring 事务 | 明确 Definition、owner 集合、C# 操作 → 本次提交报告 | 合并同一次编辑的 Undo；收集创建、删除和迁移的资产；统一校验、保存、恢复；迁移现有 Agent 事务责任 |
| Skill authoring | 正式 Graph / Node 类型、typed 参数、实际端口 → 正式节点、边、FSM、Macro、Blackboard | 复用 `BtsmtlSkillFlowGraph`、factory、closure；系统入口、动态端口、transfer、私有 owner 和孤立内容清理 |
| Timeline authoring | `TimelineData`、catalog、Track / Clip 类型、帧、资源和配置 → 正式 Track / Clip / Section / Binding | 复用 Add / Remove、contract 和现有 typed clip 能力；强类型配置替换 JSON binding；Animation channel / TreeClip / MotionWarp 引用合法性 |
| Presentation authoring | Profile、Pose Graph、Slot / Binding、正式 Mutation → 同一批表现资产 | 复用已有 Presentation Mutation、Pose capability、资源与曲线服务；不再通过 Document 外壳转换 |
| Character authoring 校验 | 实际 Definition 和 owner 闭包 → 领域错误；显式要求时得到精确编译诊断 | 把 `AgentCharacterAuthoringValidator` 中正式校验迁出；不为了校验重新构造 Document |
| C# authoring 调用代码 | 上述模块的公开函数 → 特定角色的修改 | 只表达“建什么、配置什么、删什么”；不再实现节点 catalog、通用 dispatcher、JSON handler 或独立回滚 |

“共用入口”不是把所有领域做成一个巨型 `switch`。各领域保留自己的具体类型和操作；只有提交、owner 和回滚生命周期共用。扩展一个 Clip 类型时修改该类型及正式合同，不要求再维护一份 Agent schema 和字段映射。

现有图 `Execute / Apply`、Timeline `ApplyModify`、Presentation `Apply / ApplyWithoutUndo` 必须在一个正式事务上下文中组合：已经有上下文就加入，独立人工操作由同一入口建立上下文。不能让 AI 外层事务内部再产生数个独立 Undo 或提前 `SaveAssets`。迁移现有入口后删除旧独立实现，不新增一套旁路服务与它们长期并存。

### 5.3 稳定身份与重复执行

1. 已有资产原位修改，保留 Unity GUID、子资产 local file ID、Graph / Node / Edge / Track / Clip authoring identity。不能清空重建来实现“看起来一样”。
2. 定位已有实体使用正式 owner 加稳定身份；显示名、列表 index、compiler index 不参与身份判断。改名与排序不更换 identity；复制才获得新 identity。
3. 新建时由正式 API 接收或生成合法稳定身份，重复执行先按 identity 找对象，类型和 owner 一致则更新，不一致则明确报冲突。若现有 factory 只会随机创建，扩展其正式身份参数；不要另建 AI factory。
4. 新对象的固定创建键可由明确 owner identity 加不变的业务键确定；键与显示名称分开。新键只在创建操作中使用，不给既有资产重新计算身份，也不保留 JSON `local:* -> stable` 工作副本映射。
5. 当前 `BtsmtlSkillGraphAssetFactory.StableIdentity` 输出 GUID `N` 格式，而 BTSMTL `AuthoringIdentity.IsValid` 要求 `D` 格式。不同领域继续遵守自己的正式格式，不能拿一个字符串工具跨所有对象套用。
6. 相同操作对相同现状第二次执行应成为无变更，不重复 Add、不增长资产集合、不无故更新 revision。正式类型只提供 Add 并不自动满足此条件。
7. 如果作者在第一次执行后调过相同字段，旧操作再次执行必须展示或报出冲突。判断顺序是：当前值已等于目标值则无变更；否则比较已读取的旧值／revision，不匹配则停止该事务。不要用静默覆盖或新增“强制回退”配置解决。
8. C# 的重新编译不触发执行。新一次修改可接受当前资产为新输入，但不自动 rebase 旧意图。

### 5.4 删除与跨 owner 修改

- A 中的删除必须显式表达：删除指定 identity 的 Clip、边、节点或明确 owner 内的内容，不把“脚本里没出现”当成删除指令。
- 删除前核对引用与权限范围。先解除或替换引用，再删对象；跨 owner 引用尚未处理时使事务失败，不保留悬空引用。
- 图私有内容继续由正式 owner 闭包清理；共享 Macro / Timeline 不因为一个引用者删除就被销毁。跨 Definition 的共享资产不能仅凭当前 Definition 不再可达就删除。
- Graph、FSM、Timeline TreeClip body 的 owner 移动要保留 Unity 引用，并进入同一事务；当前 timeline applier 的 rehome / rollback 职责要迁入正式资产模块。
- 修改 Timeline channel 时，同一次事务内处理其有限 Action producer 和 Pose AnimationSlot consumer。人工单项编辑若不能满足闭包，明确报出问题，不偷偷补一个默认 channel。
- 曲线、BlendSpace、Source Slot、Linked Pose Implementation 等原 Document 可写能力必须逐项有正式去向后再删除对应 handler；不能只迁移 Skill 图就宣称整个 Agent 系统已移除。

### 5.5 Undo、保存、校验与执行限制

- 业务不变量由领域 API 校验，跨对象引用在最终闭包校验；C# 编译只能发现类型与语法问题，不能证明资源、channel、owner 或运行合同正确。
- Graph 修改前后必须按现有 `SelfSerialize / SelfDeserialize` 协议处理；Timeline 内容的 Undo owner 是承载它的资产，不是 Slate 代理或普通 Track 实例。
- 一次操作的外层事务统一记录被修改 owner、创建对象、删除对象和 owner 迁移；失败时恢复这些内容。领域函数只加入事务，不私自保存。
- 显式 C# 提交在全部校验成功后保存本次 owner；人工交互保持一次可撤销编辑及已有用户保存语义。二者共用修改与事务实现，差别只在显式保存时机。
- Undo 是内存恢复机制，不能直接宣称多个资产文件保存具有磁盘原子性。跨资产保存失败恢复需覆盖已有文件、`.meta`、新建路径和删除路径；可以迁移为正式事务的技术快照，但不能把快照变成可编辑的第二内容来源。
- 若不能完成磁盘失败恢复，必须报告保存未完成及受影响路径，不能返回成功或“已完整回滚”。进程崩溃恢复是否需要持久化事务记录，应在确定保存实现时明确，不凭当前代码声称已经具备。
- 删除 `dry_run_document` 不意味着任意 C# 都能安全 dry-run。任意 C# 可以包含文件 I/O 等副作用，不能靠执行两次自动产生无副作用差异预览。保留输入预检、正式验证、显式执行后的实际改动报告，不再承诺通用 C# dry-run。
- 调用代码只操作该事务暴露的 authoring API；事务只承诺管理其登记的 Unity owner，不承诺撤销任意 C# 外部副作用。
- Authoring 提交不自动 Build，不自动 Play。生成物按现有 source revision / stale 机制处理；正式 Character Build 单独显式执行。

## 6. 删除与保留清单

| 范围 | 最终处理 | 删除前必须完成的事 |
|---|---|---|
| `Editor/CharacterPipeline/AgentAuthoring/Mcp/` | 删除五个 `btsmtl.*` authoring 工具及专属 job scheduler | 确认正式 C# 执行方式；移除注册引用 |
| Agent Controller Window 与 Definition 的 `Open Agent Controller` 导航 | 删除 | 保留正式 Graph / Timeline / Profile 导航与正式校验入口 |
| `AgentAuthoringDocumentModels / Codec / Exporter`、PackageStore、LiveTargetExporter、TargetMapper、GraphPackageProjection、package catalog validator | 删除 Document 专用实现 | 从混合文件中移走仍被正式 UI / compiler 使用的非协议定义 |
| Document Reconciler、Document MutationCompiler、Document support、`AgentPackage*` DTO、`BtsmtlSetSkillDocumentMutation` | 删除 | 所有业务写入由强类型领域 API 覆盖；不把 DTO 仅改名为 C# Document 继续保留 |
| `AgentAuthoringDocumentApplicationService`、`AgentMutationSession`、transaction owner collector、asset resolver、Agent Validator | 迁出业务责任后删除旧类型 | 正式事务、资源解析、身份、owner 收集和校验不依赖 Agent report / DTO |
| `Authoring/SkillDocument/` | 拆出 Graph / Timeline / SkillDefinition 创建、引用和清理能力，删除 JSON 对账部分并重命名目录 | applier 目前直接依赖 `AgentPackageSkillFlowDocument`，不可原样作为新公共服务 |
| `Authoring/PresentationDocument/`、Action / Control Document 模块 | 删除协议映射；保留并归并真正的 Pose / Profile / Curve / Control / Action 业务操作 | 所有旧可写业务能力有对应入口；复用现有 Presentation Mutation |
| `BtsmtlSkillNodeAuthoringBinding` 中 JSON Apply / Export / Parse 与 resolver | 删除 JSON 适配，节点配置继续使用 typed `Configure / Set` | 检查 value input、Blackboard、Macro、FSM、ActionContext 与节点变体覆盖 |
| `TimelineAuthoringClipBinding` 和 Slate `BuildClipProperties` | 替换为强类型业务参数并删除 authoring JSON 转换 | 人工新增、资源配置、曲线与 MotionWarp 引用一同迁移；共用正式类型 |
| Capability、marker、端口形状投影、Timeline contract catalog、正式 validator | 保留真实消费者需要的业务规则 | 删除仅供目录包导出的元数据／投影；不能按名字含 Authoring 或 Agent 就批量删除 |
| `.btsmtl` 目录包及相关使用说明、skill | 删除失效协议和使用路径；文档改成正式 C# 路径 | 先确认没有尚未应用的用户正文；未应用差异必须由用户决定迁入还是丢弃，本轮不删除任何包 |
| Agent 专用测试与 asmdef | 废弃协议测试随代码删除；保留独立领域规则验证 | 不在本次规划擅自添加测试；不能连领域校验代码一并删掉 |
| FlowCanvas / NodeCanvas / Slate、本项目 Skill FSM / Macro、BTSMTL TimelineData、Pose runtime / compiler | 保留 | 本任务不重做插件数据模型，不回退当前正确的新 FSM / ownership 改动 |

“删除 JSON”特指 authoring 中间协议及其 typed binding 替代范围，不是删除整个项目的 Newtonsoft 或插件内部序列化。FlowCanvas 自身的资产序列化、诊断文件、其他系统 JSON 与本任务没有必然关系。

### MCP 的独立取舍

| 选择 | 业务收益 | 业务代价与范围 |
|---|---|---|
| 删除 BTSMTL authoring MCP，作者显式执行 C#（本文建议基线） | 自定义 authoring MCP 可为零，代码入口最直接 | AI 写完 C# 后，作者负责在 Editor 执行；无需把执行也塞回 JSON |
| 需要 AI 自动执行正式入口 | AI 能完成资产创建、校验闭环，减少作者切换 | 必须有稳定的 Unity 调用通道；可用正式 CLI 或有限入口的薄桥，不能恢复任意字段／节点工具或任意 C# eval 服务 |

`character.build_float32_products / character.build_fixed_products` 位于独立 Character Build 模块；其底层 Build 能力必须保留。这次删除五个 `btsmtl.*` 不自动授权删除 Build MCP、Preview MCP 或整个 Unity MCP 插件。“项目所有自定义 MCP 为零”需要用户另行确认执行范围；不把这项扩张默认为本任务。

## 7. 与现行规范及工作区的冲突

本次没有调用 OpenSpec workflow，不创建 proposal 或修改 `openspec/project.md`。为评估能否删除，已读取相关现行 spec；下列要求与建议方向明确冲突，实施前必须更新，不能一边保留旧要求一边实现第二条路径。

| 当前规范 | 冲突或保留内容 | 需要处理 |
|---|---|---|
| `btsmtl-agent-authoring-document-sync/spec.md`，8、213 行 | 要求唯一 v8 包、Document Reconciler 和 immutable AgentMutationPlan | 退役 Document 协议要求；稳定 identity、正式 owner、校验等业务约束迁到正式 authoring 合同 |
| `btsmtl-agent-authoring-mcp-bridge/spec.md`，99、137、153 行 | 要求五个 MCP、共用 Document service、hash gate 和反向发布 | 删除 authoring MCP 与 Document 条款；独立 Build 生命周期条款应移到对应 Build 规范，不能整份一删了之 |
| `character-state-timeline-authoring-loop/spec.md`，169–182 行 | 要求资产迁移只能通过 Document v8 事务 | 改为正式 C# / 人工共享 authoring 事务；显式 Build、Gameplay 与 Presentation 分工保留 |
| `agent-character-controller-synthesis` 及 Presentation / Pose / Graph 相关现行 spec | 存在 Agent 写入方式和共享 capability 约束 | 实施前逐条区分传输约束与领域约束；此次只完成关联检索，未逐条穷尽所有引用 |
| `btsmtl-timeline-editor-preview/spec.md`，128、244 行 | 要求稳定 identity，不得建立第二份 TimelineData | 与建议一致，应保留 |
| `.codex/skills/btsmtl-agent-authoring/SKILL.md` | 当前规定 Agent 只能改 Document，禁止第二 Mutation 和临时菜单 | 替换失效入口说明；保持禁止旁路和唯一业务链。本次只评估，未按新方案修改资产 |

当前 `git status` 显示相关 skill、Graph applier、Timeline projection、Skill mutation / factory、Pose 文件和多份现行 spec 已有未提交修改。这些是本轮开始前／其他工作进行中的内容，不属于本方案实施产物。

实施前必须重新读取当前 diff。允许在已确认的新合同上做增量改动，不允许恢复旧版本覆盖现状。若本方案需要改变用户已经改对的 ownership、原生 FSM、端口、Curve 或 Pose 合同，必须列出具体冲突交用户决策，不能用“统一清理”自行撤销。此文档不预先授权解决这些冲突。

## 8. 迁移顺序与完成定义

下面是依赖顺序，不是替用户判断业务实施优先级。

1. **确认来源与执行方式。** 选择 A 或 B、人工编辑权限，以及是否需要 AI 自动执行。若选 B，先修订全量声明 owner、生成范围和删除合同，不按 A 开工。
2. **明确最新基线与旧包处理。** 核对既有改动；列出 Agent 协议及外部引用；检查是否存在未应用目录包。保留既有 Unity authoring 身份与资产，不做数据全量重建。
3. **整理正式事务和校验。** 将 Agent 内非协议职责迁到正式模块；让已有 Skill / Timeline / Presentation 入口加入同一上下文。把提前 Save、Graph 序列化、owner 移动、失败恢复统一到既有业务链的正式边界。
4. **强类型化节点与 Timeline binding。** 迁移 C# 调用和人工 UI，删除 `JObject / JToken` authoring 路径。完成所有原可写业务能力的覆盖清单，包含 SkillDefinition、Control、Action、Blackboard、Pose / Profile、Clip curve，不只包含 AddNode / AddClip。
5. **建立正式 C# 调用入口。** 使用精确 Definition 和类型化操作；完善按 identity 的重试与显式删除；正式报告实际资产变化。入口不通过 JSON、不假装支持任意 C# 无副作用 dry-run。
6. **整体切换并删除旧层。** 删除 Document 模型、导出、对账、MCP、Window 和旧目录说明；更新现行规范与 skill。依赖重构可以分小步提交，但不得发布两个并行 authoring 入口或保留兼容开关。
7. **交付清理证据。** 实现窗口记录代码链、删掉的类型和文件、现有能力去向、引用检索、编译结果及未完成项。每步只提交本任务差异，中文提交说明；不提交其他窗口的资产或代码。

完成条件：

- 正式人工入口和 C# 入口修改同一批 Unity 资产，正式修改链不依赖 `AgentPackage*`、Document 或 authoring JSON。
- 相同创建／配置意图可重复执行且不重复建对象；重命名排序不换 identity；删除清理正确 owner 下的引用。
- Graph / Timeline / Presentation 跨 owner 改动由一个提交边界管理，失败不报告成功；保存与恢复能力有明确证据和限制。
- Slate 代理仍临时存在，正式 Timeline 来源仍为 `TimelineData`；Build 仍是独立显式生命周期。
- 原 Document 的业务编辑能力逐项保留到正式 C# API 或被用户明确取消；没有“先支持 Graph，其余以后”的隐藏缺口。
- 五个 BTSMTL authoring MCP、Document 窗口、包同步实现、失效导航与说明被删除；不误删插件内部 JSON、Build 或运行编译链。
- 与现行 spec 的入口冲突已解决，无旧新并行合同；既有正确改动未被覆盖。

本轮不写测试、不执行验证。未来实施的编译与静态引用检查是交付证据，不能替代作者的端到端验收；运行行为、Undo / 保存重载、跨 owner 失败恢复尚未验证。没有用户明确要求时不新增测试代码。

## 9. 确认时需要定下来的决策

1. 内容来源选择 A（资产为准、C# 显式修改）还是 B（C# 全量声明、生成范围界面只读）。
2. 是否保留人工 Graph / Timeline / Pose 编辑；若选 A，本文按保留并共用正式 API 设计。
3. 执行边界是作者执行 C#，还是需要 AI 自动触发 Unity；删除五个 authoring MCP 与删除所有自定义 MCP 是不同范围。

建议组合为 A + 保留人工编辑 + 删除五个 BTSMTL authoring MCP。尚未执行的旧包如何处理、与其他工作区改动是否冲突，要依据实施前的实际内容处理，不凭本轮静态评估直接删除。

用户确认后，由本规划窗口记录确认版本并向现有实现窗口发送一次文档指针；不创建新窗口、不提前下发实施任务。

[flowgraph]: ../3cDemo/Client/3C_Client/Assets/ParadoxNotion/FlowCanvas/Modules/FlowGraphs/FlowGraph.cs
[skillgraph]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillFlowGraph.cs
[skillmutation]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillFlowEditorMutation.cs
[graphfactory]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillGraphAssetFactory.cs
[graphapplier]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/SkillDocument/BtsmtlSkillGraphAuthoringApplier.cs
[timelinedata]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts/TimelineData.Runtime.cs
[timelinesession]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/TimelineEditorSessionContext.cs
[slateprojection]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/BtsmtlSlateTimelineProjection.cs
[cutscene]: <../3cDemo/Client/3C_Client/Assets/ParadoxNotion/SLATE Cinematic Sequencer/Framework/Cutscene.cs>
[cutscenegroup]: <../3cDemo/Client/3C_Client/Assets/ParadoxNotion/SLATE Cinematic Sequencer/Framework/CutsceneGroup.cs>
[cutscenetrack]: <../3cDemo/Client/3C_Client/Assets/ParadoxNotion/SLATE Cinematic Sequencer/Framework/CutsceneTrack.cs>
[nodebinding]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Control/Authoring/FlowGraphs/BtsmtlSkillNodeAuthoringBinding.cs
[clipbinding]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts/TimelineAuthoringClipBinding.cs
[transaction]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/AgentAuthoringDocumentTransactionService.cs
[agentvalidator]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/AgentAuthoring/AgentCharacterAuthoringValidator.cs
[definitioneditor]: ../3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/Definition/CharacterPipelineDefinitionEditor.cs

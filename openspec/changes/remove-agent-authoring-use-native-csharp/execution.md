# 实施记录

## 1. 实施基线

- 实施版本：`r2-coordination-2026-09-13`。
- 规划入口：`proposal.md`、`design.md`、`tasks.md` 与 `specs/`。
- 实施入口：本文件。
- 当前首步只覆盖 tasks 1.1—1.4 的基线记录，以及 tasks 3.1、4.1 的公共合同骨架；不提前删除旧 Agent，不改共享程序集、其它领域文件或生成资产。
- 规划提交：`00ea9a4e8`（规划：补齐作者r2公共扩展合同与共享文件边界）。

## 2. 基线与边界

### 2.1 共享文件归属与排除范围

本任务负责公共 C# 导出/生成合同、后续两个显式 MCP、两份 JSON binding 的退役，以及满足门槛后的旧 Agent 公共协议清理。以下文件属于其它领域任务，不能因为名称或目录包含 Agent/Document 而删除或整体改名：

| 文件 | 负责领域 | 本任务动作 |
| --- | --- | --- |
| `Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Editor/Scripts/BtsmtlSlateTimelineProjection.cs` | Timeline | 仅消费本任务提供的 typed binding；UI 接入由 Timeline 任务完成 |
| `Assets/GameScripts/Main/Runtime/BTSMTL/EventGraphs/EventGraphAuthoringDocument.cs` | EventGraph | 保留并等待事件图任务收口 |
| `Assets/GameScripts/Main/Runtime/BTSMTL/EventGraphs/HostEventGraph.cs` | EventGraph | 保留运行与变量业务 |
| `Assets/GameScripts/Main/Runtime/BTSMTL/EventGraphs/HostEventGraphEditorMutation.cs` | EventGraph | 保留正式编辑 mutation |
| `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphAuthoringAdapter.cs` | Pose | 保留领域读取、输入消费与适配 |
| `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/PoseGraph/CharacterPoseGraphMutation.cs` | Pose | 保留正式 Pose mutation |
| `Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/SkillDocument/BtsmtlSkillGraphAuthoringApplier.cs` | FSM/Skill | 等待有效 FSM 操作迁出；不能整文件删除 |
| `Assets/GameScripts/Main/Runtime/BTSMTL/TreeDesigner/Scripts/Authoring/GraphAuthoringCapabilityCatalog.cs` | 数据统一 | 只消费正式 capability，不新建第二目录 |

当前工作区已有其它窗口的未提交改动，包含上述共享文件、角色资产和 Agent 文件。本任务不回退、不覆盖、不使用整树删除；后续每一步只处理本任务明确拥有的路径。

### 2.2 旧 Agent 与目录外消费者

当前源码计数如下，计数包含 `.cs` 文件，不包含 `.meta`：

| 区域 | 文件数 | 当前职责 |
| --- | ---: | --- |
| `Editor/CharacterPipeline/AgentAuthoring` | 28 | Document、Snapshot/Model、Codec、Store、Exporter、Reconciler、Mutation、Session、Validator、Window、MCP |
| `Editor/CharacterPipeline/Authoring/SkillDocument` | 16 | Skill 正式适配与仍待退役的 Document 协议混合 |
| `Editor/CharacterPipeline/Authoring/PresentationDocument` | 15 | Presentation 正式适配与仍待退役的 Document 协议混合 |

当前旧 MCP 注册仍为：

- `btsmtl.checkout_document`
- `btsmtl.rebase_document`
- `btsmtl.dry_run_document`
- `btsmtl.apply_document`
- `btsmtl.validate`

目录外存在 33 个匹配 Agent/Document 名称的消费者或混合文件。它们不能仅凭字符串匹配删除；必须先区分四类去向：

1. 协议删除：旧 package、hash/sync、Document codec/store/exporter/reconciler、专属 mutation/session/report/validator、旧窗口和五工具。
2. 重复校验删除：仅 Agent 层重复的规则删除，真实规则回到已有 Graph、Timeline、Presentation 或 Character 编译模块。
3. 正式能力复用：`BtsmtlSkillFlowGraph`、`BtsmtlSkillFlowEditorMutation`、`BtsmtlSkillGraphAssetFactory`、`TimelineData`、Pose 正式 Definition/Mutation 等继续保留。
4. 缺失能力补齐：由 D0 指定的领域任务补正式读取、配置、FSM、Pose、EventGraph 或 Slate 接入；不能复制一套旁路实现。

### 2.3 支持根、闭包与外部资源

首期公共合同固定以下输入边界，具体领域适配按正式类型提供闭包：

| 生成根 | 内部拥有闭包 | 外部输入 |
| --- | --- | --- |
| Skill Flow Graph | Graph、节点、Edge、Blackboard、Macro、原生 FSM、状态 Body/Condition、Skill-owned Timeline/TreeClip、layout | Definition、已存在的共享 Macro、Rig、AnimationClip、分析产物及范围外资产 |
| Timeline | `TimelineData`、Track、Clip、Section、外部 binding、Timeline-local Curve | AnimationClip、Rig、Profile、共享资源及范围外消费者 |
| Pose Graph | 正式 Pose Graph、Graph-owned Source Slot、节点、动态端口、Edge、Pose layout 及领域声明的内部对象 | Profile、Rig、AnimationClip、Analysis artifact、外部实现 |
| EventGraph | 仅在 EventGraph 薄适配完成后纳入正式闭包 | 事件图任务声明的变量、资源和 owner；适配未就绪时必须拒绝完整导出 |

导出输入必须由调用方提供精确根对象、Definition 上下文和输出源码路径；不得通过 selection、显示名、目录扫描、旧源码或生成子资产 GUID 猜测根和引用。生成输入必须提供精确源码路径、recipe、已编译入口类型、Definition 路径和输出资产范围。

输出代码按 `Create`、`Configure`、`Bind`、`Connect`、`RootBinding` 五个阶段表达正式 API 调用。内部引用使用本次调用的对象变量；真正外部资源使用类型化依赖描述。物理 Unity 实例身份可以重建时变化，业务 authoring identity 必须由领域适配显式写入和恢复。

### 2.4 未应用内容与删除门槛

- 本首步不操作现有资产，不删除 `.btsmtl` 工作包，不清理用户当前未提交内容。
- 当前 Agent、SkillDocument、PresentationDocument 的修改属于现有工作区状态；它们是否已被其它窗口应用不能由静态文件名推断。
- 只有作者调用者已经脱离 Agent、正式编辑/导出/生成/保存链可用，且混合文件中的有效领域操作已经由负责任务承接，才允许执行 D7.1 后的旧协议删除。
- 固定 motor 参数桥、动画变量运行推进和消费闭环单独记录为外部运行依赖，不作为恢复旧 Agent 协议的理由，也不由本任务宣称完成。

## 3. 首步公共实现

新增目录：`Assets/GameScripts/Main/Editor/CharacterPipeline/Authoring/CodeGeneration/`。

已实现的公共边界：

- `IBtsmtlAuthoringCodeDomainAdapter`：领域适配只接收正式根对象和只读意义上的导出上下文，负责报告支持范围、身份/owner/字段读取和正式 API 语句；不拥有第二领域模型。
- `BtsmtlAuthoringCodeExportContext`：只在当前调用内维护对象到局部变量、稳定身份、外部依赖、五阶段语句和完整性诊断；共享对象按引用复用，重复稳定身份直接报错。
- `BtsmtlAuthoringCodeExportService`：要求恰好一个领域适配认领根对象；缺少适配、多个适配、未知字段或领域导出异常均返回失败诊断，不能输出省略内容的源码。
- `BtsmtlAuthoringCodeSyntax`：提供 C# 标识符、字符串、有限浮点数、枚举表达式和确定性身份 token 的基础输出，使用 invariant culture。
- `IBtsmtlAuthoringGenerationEntry` 与 `BtsmtlAuthoringGenerationContext`：生成入口必须是当前编译出的明确类型，接收精确 source/recipe/Definition/output 上下文，返回明确根输出和创建/替换/删除范围；不接受任意 C# 正文、任意方法名、反射字段或节点参数。
- `BtsmtlAuthoringGenerationService`：校验请求、入口和上下文的 recipe、入口类型及全部精确路径一致后才调用入口；不负责领域对象模型、Undo、保存或 Build。

该首步还没有接入具体 Skill、Timeline、Pose 或 EventGraph 薄适配，也没有写入源码文件。后续适配必须直接消费正式对象/API；导出失败时由公共服务阻止源码替换，生成时由正式领域入口负责创建、替换、根挂接、保存和业务校验。

当前已继续接入 Skill 正式对象：`BtsmtlSkillAuthoringCodeAdapter` 直接遍历 `BtsmtlSkillGraphClosure`，按对象身份收集 Skill Graph、Macro、原生 FSM、Timeline、节点、连线、Blackboard、Track、Clip、Section 与外部 Binding，并按五个代码阶段输出正式 API 调用。`BtsmtlSkillAuthoringCode` 只包装已有 Graph mutation、GraphAssetFactory、TimelineData 与 Curve Catalog，不创建第二套节点或字段模型；生成范围清理也只接收当前导出的稳定 identity 集合。

该适配器已覆盖当前正式 Skill/Timeline 节点和片段的类型化配置、动态步骤、FSM Transfer、MotionWarp source、完整 AnimationCurve、外部资源引用与根绑定表达。遇到内联旧 Tree 或未持久化资源时返回明确不支持诊断，不用默认值或旧 JSON 补齐。

已新增两个显式 MCP：`btsmtl.export_code` 只接收精确资产、Definition、Editor 源码输出路径、recipe、命名空间和入口类型，成功完成完整性检查后原子写出源码；`btsmtl.generate_assets` 只接收精确源码路径、recipe、已编译入口类型、Definition 和输出资产路径，校验入口的 `RecipeType`、`EntryTypeName`、`SourceCodePath` 及上下文完全一致后执行。两者都拒绝 Play、编译、导入忙状态，不调用旧 Document 生命周期，不自动 Build 或 Refresh。

## 4. 当前验证记录

- 规划文档已由规划窗口执行 `openspec validate remove-agent-authoring-use-native-csharp --strict` 并通过；本首步未修改规划文件。
- 当前代码目录在首步前不存在；新文件和目录 `.meta` 已按 Unity Editor 目录规则创建。
- 适用 Editor 编译：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Editor.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false`，结果为 `0 个错误`、`93 个警告`，成功生成 `ThirdPersonClient.Editor.dll`；输出中没有首步 `CodeGeneration` 文件的诊断。
- 编译结束后立即执行 `dotnet build-server shutdown`，MSBuild 与 VB/C# 编译器服务器均成功关闭。
- `git diff --check` 对首步 execution 与 CodeGeneration 文件通过。
- 关键词合法性修正后的同一 Editor 增量编译再次成功：`0 个警告`、`0 个错误`，成功生成 `ThirdPersonClient.Editor.dll`；随后 `dotnet build-server shutdown` 成功关闭全部编译服务器。该结果只证明静态程序集编译，不替代 Unity Editor Console 或运行验证。
- 接入 Skill 适配器后，使用 Unity 2022 Editor 引用与当前 `Temp/bin/Debug` 程序集直接编译 CodeGeneration 全部 `.cs`，结果为 `0` 错误；该检查覆盖新文件自身语法和类型引用。完整 `ThirdPersonClient.Editor.csproj` 编译另有并行任务现存错误：`BtsmtlScenePlayGraphShellToolbar.cs:37` 找不到 `BtsmtlScenePlayPreviewPresenter`，不是本任务新增文件的诊断，已保留未覆盖。
- 使用临时检查项目编译 CodeGeneration 全部 `.cs`（包含两个 MCP）成功：`0 个错误`、`2 个引用版本警告`；检查项目已删除，不作为正式工程路径。该检查证明新增代码可编译，不证明 Unity Editor 已刷新、MCP 已加载或资产往返已执行。
- 尚未运行 Unity、Unity MCP、Play、Build 或资产生成；本首步未新增测试代码。
- 待完成验证：Editor 程序集编译、具体领域适配、两个 MCP 显式入口、导出/删除重建往返、D7.1 删除门槛以及 Unity Console 实际状态。

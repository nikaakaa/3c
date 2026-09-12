# 执行记录

## 当前基线

本次按 `r2@fb937f4415ff6593e1c9affbea37e465c77d617f` 实施。r2 已把事件图作者入口从 Agent Document 改为原生直接 API 和公共 C# 输出薄适配：事件图不再新增 Document 分片、Document schema、专用 Reconciler、专用 MCP 或整图 DTO 中转。

本窗口只维护事件图运行/变量文件、事件图直接作者 API、事件图 C# 输出薄适配和本执行记录。Pose adapter、公共 Agent 协议迁移、两个公共 MCP、Pose consumer 和固定 motor 桥分别由其所属任务维护；未满足删除先决条件的旧调用没有被旁路掩盖。

## 本步已完成

### 1. 原生直接作者 API

在 `HostEventGraph` 与 `HostEventGraphEditorMutation` 中补齐了人工编辑和 C# 创建共用的直接入口：

- 以稳定 `Variable.ID` 声明精确类型和初值的 Float、Int32、Bool、Vector2、Vector3 Blackboard 变量；禁止重复 ID、重复名称、属性绑定和未登记类型。
- 按稳定 node identity 创建正式节点，并复用原生 `CanAuthorNodeType`、节点端口收集和局部 Undo/dirty/save 路径。
- 配置节点 metadata、Canvas layout、宿主输入、零间隔 Update、Instant Split、Sequence、Get/Set 变量绑定、Set operation/per-second 以及 Macro 引用。
- 通过稳定节点和端口 identity 建立连接；连接前继续执行方向、精确类型、单输入和 FlowCanvas binder 规则。
- 为未连接的 typed value input 提供直接配置入口，避免 C# 输出写入序列化字段或构造第二份节点模型。

每个操作都通过现有 `HostEventGraphEditorMutation.Apply` 进入原生 Undo、rollback、revision、序列化和 dirty 边界。没有把整图变量、节点、边集合换名后重新作为 API 参数。

### 2. EventGraph C# 输出薄适配

新增 `EventGraphAuthoringCodeAdapter`，实现公共 `IBtsmtlAuthoringCodeDomainAdapter`。它直接读取当前 `HostEventGraph` 对象，不读取旧源码、不经过 JSON、不创建持久化 Snapshot/DTO，向公共输出上下文写入五个阶段的正式 API 调用：

`Create → Configure → Bind → Connect → RootBinding`

覆盖内容包括：

- 根图类型、稳定 graph identity、content revision、Canvas category/comments/translation/zoom；不支持的外部序列化文件和 CanvasGroup 会明确失败。
- 原生 Blackboard 变量 ID、名称、精确类型、初值、public 标记和属性绑定检查。
- 节点真实类型、稳定 UID、位置、tag/comments/breakpoint、宿主输入、Update/Split/Sequence、Get/Set 绑定和 assignment 配置。
- 所有未连接 typed value input 的精确默认值，以及连接值端口的类型完整性。
- BinderConnection 的稳定 identity、真实端点、稳定端口 ID 和连接顺序。
- Macro 的接口类型、共享引用、外部资产路径和生成代码中的明确 `AssetDatabase.LoadAssetAtPath` 引用；不把旧 GUID 当作生成范围内部依赖。

未知节点、未支持值类型、非法 Macro、缺失端点、非法布局和不能恢复的外部引用都会返回精确错误，公共服务不会生成半份源码替换目标文件。适配实例为 `EventGraphAuthoringCodeAdapter.Instance`，由公共 C# authoring 任务接入其 adapter 集合。

### 3. 运行基线保持不变

原生 FlowScript Manual 执行、动画宿主、typed variable frame、实例隔离、Reset/Replacement 和 Graph failure sink 保持上一有效实现。r2 只收口作者路径，没有把事件图重新编译为第二套 runtime，也没有修改 Skill/Pose 作者图的运行禁令。

`EventGraphAuthoringDocument`、`HostEventGraph.ApplyAuthoringDocument` 和 `HostEventGraphEditorMutation.ApplyDocument` 当前仍存在，是因为工作树中的 Pose adapter 和 Agent Presentation 调用者尚未迁出。它们是明确的待删除旧调用链，不是新的兼容入口；调用者迁出后必须直接删除，不能保留转发或改名。

## 直接 API 输入输出

输入是稳定 identity、正式节点/变量对象、typed 配置、端口 ID、Macro 外部引用和 Canvas layout。输出是原生 Graph/Blackboard/Node/Connection 对象及其序列化结果；代码输出适配器的输出是公共 C# 语句，不是另一份可编辑图数据。

生成代码先创建根图和变量/节点，再配置节点和外部 Macro，随后绑定 Get/Set、建立连接，最后恢复 graph identity/revision。内部引用使用本次调用创建的局部对象，范围外 Macro 使用明确资产路径。

## 尚未满足的删除依赖

- Pose 任务仍需将 `CharacterPoseGraphAuthoringAdapter.ApplyEventGraphMutation` 迁移为直接 API 调用，并完成其自身输入消费归属。
- C# authoring 任务仍需迁出 Agent Mapper 和 Presentation/Skill 的公共协议调用，随后才能删除 `EventGraphAuthoringDocument`、`AgentAuthoringEventGraphDocumentMapper` 以及事件图 Document 分片处理。
- 公共 C# authoring 任务仍需把 `EventGraphAuthoringCodeAdapter.Instance` 接入公共 `export_code` adapter 集合，并完成两个显式 MCP 的实际调用链；本窗口不复制公共 MCP。
- 固定 motor 参数桥、Pose 条件/BlendSpace/运行/Preview consumer 属于独立运行闭环，不能作为本步作者协议删除的理由，也不能因为作者 API 已有就提前删除。

在这些调用者和公共入口迁出前，本窗口不删除共享 Agent/Presentation 文件，不修改 Pose adapter/Mutation，不创建 Corin 事件图资产，不通过 fallback 或旁路保持旧协议。

## 验证记录

源码构建使用了 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，每次构建后立即执行 `dotnet build-server shutdown`。

- `BTSMTL.EventGraphs.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 warning，0 error。
- `ThirdPersonClient.Editor.csproj` Rebuild，`/p:BuildProjectReferences=false`：当前被其它窗口新增的 `BtsmtlScenePlayGraphShellToolbar.cs` 对缺失 `BtsmtlScenePlayPreviewPresenter` 的引用阻塞；本步事件图文件没有产生编译错误。
- 上一步 `ThirdPersonClient.Runtime.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 error，1 个现有 `CharacterInputValueNodes.cs` warning。
- `git diff --check`：没有发现空白错误；LF/CRLF 输出只是 Git 行尾提示。

本步没有运行 Unity MCP、Play Mode、Build、资产生成或端到端回放。静态源码构建不等于 Unity Console 清洁，也不等于事件图删除重建往返已经通过。

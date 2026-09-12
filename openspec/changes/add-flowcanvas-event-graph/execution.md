# 执行记录

## 当前基线

本次按 `r2@fb937f4415ff6593e1c9affbea37e465c77d617f` 实施。r2 已把事件图作者入口从 Agent Document 改为原生直接 API 和公共 C# 输出薄适配：事件图不再新增 Document 分片、Document schema、专用 Reconciler、专用 MCP 或整图 DTO 中转。

本窗口只维护事件图运行/变量文件、事件图直接作者 API、事件图 C# 输出薄适配和本执行记录。Pose adapter、公共 Agent 协议迁移、两个公共 MCP、Pose consumer 和固定 motor 桥分别由其所属任务维护；未满足删除先决条件的旧调用没有被旁路掩盖。

本步提交：`873c497eb`，`事件图：收口原生直接作者API与C#输出适配`。
后续小步提交：`746ce0c67` 补齐目录元数据，`01194e852` 改用公共生成上下文恢复外部Macro引用。

本轮继续基于最新工作树推进：公共 C# 输出入口已在 `6c80aee6` 接入事件图适配器；Pose 旧参数帧链路由 `f1dc9f9eb` 及后续预览收口提交退役。本轮没有重建已完成的旧链路，只补齐了变量帧在 Pose 收尾阶段的传递，并新增 Corin 正式事件图生成入口。

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

新增 `EventGraphAuthoringCodeAdapter`，实现公共 `IBtsmtlAuthoringCodeDomainAdapter`。它直接读取当前 `HostEventGraph` 对象，不读取旧源码、不经过 JSON、不创建持久化 Snapshot/DTO，向公共输出上下文写入四个阶段的正式 API 调用：

`Create → Configure → Bind → Connect`

覆盖内容包括：

- 根图类型、稳定 graph identity、content revision、Canvas category/comments/translation/zoom；不支持的外部序列化文件和 CanvasGroup 会明确失败。
- 原生 Blackboard 变量 ID、名称、精确类型、初值、public 标记和属性绑定检查。
- 节点真实类型、稳定 UID、位置、tag/comments/breakpoint、宿主输入、Update/Split/Sequence、Get/Set 绑定和 assignment 配置。
- 所有未连接 typed value input 的精确默认值，以及连接值端口的类型完整性。
- BinderConnection 的稳定 identity、真实端点、稳定端口 ID 和连接顺序。
- Macro 的接口类型、共享引用、外部资产路径和生成代码中的明确 `AssetDatabase.LoadAssetAtPath` 引用；不把旧 GUID 当作生成范围内部依赖。

未知节点、未支持值类型、非法 Macro、缺失端点、非法布局和不能恢复的外部引用都会返回精确错误，公共服务不会生成半份源码替换目标文件。适配实例为 `EventGraphAuthoringCodeAdapter.Instance`，由公共 C# authoring 任务接入其 adapter 集合。根创建语句统一输出 `EventGraphAuthoringCode.EnsureRoot<T>(context, identity, revision, name)`，由正式上下文的 `OutputAssetPath` 负责创建或替换持久化根资产，不能退化为只存在于内存的 `CreateInstance`。

### 3. 运行基线保持不变

原生 FlowScript Manual 执行、动画宿主、typed variable frame、实例隔离、Reset/Replacement 和 Graph failure sink 保持上一有效实现。r2 只收口作者路径，没有把事件图重新编译为第二套 runtime，也没有修改 Skill/Pose 作者图的运行禁令。

`EventGraphAuthoringDocument`、`HostEventGraph.ApplyAuthoringDocument` 和 `HostEventGraphEditorMutation.ApplyDocument` 当前仍存在，是因为工作树中的 Pose adapter 和 Agent Presentation 调用者尚未迁出。它们是明确的待删除旧调用链，不是新的兼容入口；调用者迁出后必须直接删除，不能保留转发或改名。

### 4. Pose 只读变量消费者接入

Pose 的只读 Blackboard 来源现在来自动画 EventGraph 的正式变量合同。运行帧从 `CharacterPresentationRuntime` 进入 `CharacterPoseFrameCoordinator`、`CharacterPoseProgramRuntime` 和 `CharacterPoseProgramActorRuntime`，Advance 与 Finalize 使用同一份 `CharacterAnimationPoseInputFrame`；状态机转换规则也从该 typed frame 读取 Bool、Float、Int 动画变量。

这条链路没有再保留固定 motor 参数桥或旧 `CharacterPresentationProgramParameterFrame`。Pose Graph 编辑器只投影 EventGraph 声明的变量，作者侧不创建第二份可写 Blackboard。

### 5. Corin 正式生成入口

新增 `CorinAnimationEventGraphAuthoringCode`，recipe 为 `character.animation-event-graph.corin/v1`。入口通过公共生成上下文解析精确 Definition 和 Profile，按原生直接 API 创建或清空正式 `CharacterAnimationEventGraph`，只建立宿主必须的 Start/Update 生命周期节点并最后绑定 Profile。当前 Corin 没有由 EventGraph 写入的动画实例变量：Action Weight 由 Action Playback 负责，Foot Placement Weight 由输入 Pose source 曲线负责，避免重新生成一个无正式 owner 的第二变量。

入口的 `SourceCodePath` 与公共 bridge 使用同一套项目绝对路径计算，输出路径固定为 `Assets/Configs/Character/Corin/Pipeline/Presentation/EventGraphs/CorinAnimationEventGraph.asset`。正式 `btsmtl.generate_assets` 首次创建并在 helper 收口后再次替换成功，事件图 GUID 保持为 `d90ccc65c39b2d84a8b06ef1ae46b885`，并把该引用写入 Corin Profile；Projection 尚未生成。Corin recipe 与公共导出代码共用同一个 `EnsureRoot`，不再维护单独的 `AssetDatabase.CreateAsset` 根创建实现。

## 直接 API 输入输出

输入是稳定 identity、正式节点/变量对象、typed 配置、端口 ID、Macro 外部引用和 Canvas layout。输出是原生 Graph/Blackboard/Node/Connection 对象及其序列化结果；代码输出适配器的输出是公共 C# 语句，不是另一份可编辑图数据。

生成代码先通过 `EnsureRoot` 按上下文输出路径创建或替换持久化根并恢复 graph identity/revision，再创建变量/节点、配置节点和外部 Macro，随后绑定 Get/Set、建立连接。内部引用使用本次调用创建的局部对象，范围外 Macro 使用明确资产路径。

## 尚未满足的删除依赖

- Pose 任务仍需将 `CharacterPoseGraphAuthoringAdapter.ApplyEventGraphMutation` 迁移为直接 API 调用，并完成其自身输入消费归属。
- C# authoring 任务仍需迁出 Agent Mapper 和 Presentation/Skill 的公共协议调用，随后才能删除 `EventGraphAuthoringDocument`、`AgentAuthoringEventGraphDocumentMapper` 以及事件图 Document 分片处理。
- 公共 C# authoring 任务已把 `EventGraphAuthoringCodeAdapter.Instance` 接入公共 `export_code` adapter 集合，两个公共 MCP 均已注册并完成事件图现场验证。当前不复制公共 MCP，也不调用旧 Document MCP。
- 固定 motor 参数桥、Pose 条件/BlendSpace/运行/Preview consumer 属于独立运行闭环，不能作为本步作者协议删除的理由，也不能因为作者 API 已有就提前删除。

在这些调用者和公共入口迁出前，本窗口不删除共享 Agent/Presentation 文件，不删除 `EventGraphAuthoringDocument` 及其 mapper，不通过 fallback 或旁路保持旧协议。Corin 资产只由正式 C# generation recipe 创建。

## 验证记录

源码构建使用了 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，每次构建后立即执行 `dotnet build-server shutdown`。

- `BTSMTL.EventGraphs.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 warning，0 error。
- `ThirdPersonClient.Runtime.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 error，1 个现有 `CharacterInputValueNodes.cs` warning。
- `ThirdPersonClient.Editor.csproj` Rebuild：0 error，32 个现有 ACL artifact identity warning；此前 Pose 编辑器的 3 个跨程序集可见性错误已由 Pose 任务将 `SetEditorAnimationVariables` 正式公开后清零。
- 公共 `btsmtl.export_code` 已针对 Corin EventGraph 现场执行成功；生成源码的根创建语句已确认是 `EventGraphAuthoringCode.EnsureRoot<...>(context, ...)`，随后删除验证用输出文件，没有留下第二个 authoring 入口。
- 正式 `character.build_float32_products` 在移除 Corin EventGraph 的 Action/Foot 写入后再次执行；Foot Placement 内部曲线暴露诊断已消失，但当前仍被两项既有链路拒绝：AuthoringDiscovery 对 Corin Definition 返回 `Object reference not set to an instance of an object`，Pose Graph `ed8ff472330e4057a900af3eae5dfb8f` 的 State `12a31544976ddf152639d62c6d19142c` 参数合同不完整；因此 Presentation Projection 未生成。本窗口没有给 EventGraph 增加旁路。
- Unity Editor 日志已记录本轮 Tundra 编译成功、无 C# 编译 error；域重载后出现 RendererFeature/空对象编辑器警告，属于当前编辑器状态，不是 EventGraph 编译证据。
- Unity MCP 目标实例 `3C_Client@e852139597e42532` 已恢复；更新后的正式 `btsmtl.generate_assets` 成功替换事件图，磁盘复核确认 Action/Foot 变量、Set 节点和连接均已移除，Profile 仍绑定同一事件图 GUID。Projection 因上一条既有资产链诊断仍未生成。
- `git diff --check`：没有发现空白错误；LF/CRLF 输出只是 Git 行尾提示。

本轮没有进入 Play Mode，也没有运行端到端回放。事件图资产生成和 Profile 绑定已完成，但 Float32/Projection 构建仍被 Skill/Pose 资产链阻塞；源码构建成功、Tundra 成功和单次生成成功，都不等于 Unity Console 清洁、Projection 已更新或事件图删除重建往返已经通过。

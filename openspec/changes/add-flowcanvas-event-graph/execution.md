# 执行记录

## 当前基线

本次按 `r2@fb937f4415ff6593e1c9affbea37e465c77d617f` 实施。r2 已把事件图作者入口从 Agent Document 改为原生直接 API 和公共 C# 输出薄适配：事件图不再新增 Document 分片、Document schema、专用 Reconciler、专用 MCP 或整图 DTO 中转。

本窗口只维护事件图运行/变量文件、事件图直接作者 API、事件图 C# 输出薄适配和本执行记录。Pose adapter、公共 Agent 协议迁移、两个公共 MCP、Pose consumer 和固定 motor 桥分别由其所属任务维护；未满足删除先决条件的旧调用没有被旁路掩盖。

本步提交：`873c497eb`，`事件图：收口原生直接作者API与C#输出适配`。
后续小步提交：`746ce0c67` 补齐目录元数据，`01194e852` 改用公共生成上下文恢复外部Macro引用。

本轮继续基于最新工作树推进：公共 C# 输出入口已在 `6c80aee6` 接入事件图适配器；Pose 旧参数帧链路由 `f1dc9f9eb` 及后续预览收口提交退役。本轮没有重建已完成的旧链路，只补齐了变量帧在 Pose 收尾阶段的传递，并新增 Corin 正式事件图生成入口。C# authoring 窗口随后完成公共 Agent、Skill、Presentation 旧文档链迁出；本窗口再删除最后没有调用者的 EventGraph Document API。

本次继续推进已解决上一版记录中的构建阻塞：原生 FSM 的发现、条件作用域、条件代次归属、程序来源容器和 Debug SourceMap 调用者均已按正式对象关系收口，Float32 Program 与 Presentation Projection 已在当前工作树正式生成。

本次收口提交：`89084a7f9` 接通原生 FSM 编译生命周期，`4997648fa` 修复 Discovery、Program 来源和 Runtime Debug SourceMap。

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

`EventGraphAuthoringDocument`、`HostEventGraph.ApplyAuthoringDocument` 和 `HostEventGraphEditorMutation.ApplyDocument` 已在 `ad698053a` 删除。删除前全局引用核对只剩这三个定义及其专用辅助代码，没有保留转发、改名或兼容入口；当前 HostEventGraph 的直接 API 是唯一事件图作者链。

### 4. Pose 只读变量消费者接入

Pose 的只读 Blackboard 来源现在来自动画 EventGraph 的正式变量合同。运行帧从 `CharacterPresentationRuntime` 进入 `CharacterPoseFrameCoordinator`、`CharacterPoseProgramRuntime` 和 `CharacterPoseProgramActorRuntime`，Advance 与 Finalize 使用同一份 `CharacterAnimationPoseInputFrame`；状态机转换规则也从该 typed frame 读取 Bool、Float、Int 动画变量。

这条链路没有再保留固定 motor 参数桥或旧 `CharacterPresentationProgramParameterFrame`。Pose Graph 编辑器只投影 EventGraph 声明的变量，作者侧不创建第二份可写 Blackboard。

### 5. Corin 正式生成入口

新增 `CorinAnimationEventGraphAuthoringCode`，recipe 为 `character.animation-event-graph.corin/v1`。入口通过公共生成上下文解析精确 Definition 和 Profile，按原生直接 API 创建或清空正式 `CharacterAnimationEventGraph`，只建立宿主必须的 Start/Update 生命周期节点并最后绑定 Profile。当前 Corin 没有由 EventGraph 写入的动画实例变量：Action Weight 由 Action Playback 负责，Foot Placement Weight 由输入 Pose source 曲线负责，避免重新生成一个无正式 owner 的第二变量。

入口的 `SourceCodePath` 与公共 bridge 使用同一套项目绝对路径计算，输出路径固定为 `Assets/Configs/Character/Corin/Pipeline/Presentation/EventGraphs/CorinAnimationEventGraph.asset`。正式 `btsmtl.generate_assets` 已成功创建并替换该资产，事件图 GUID 保持为 `d90ccc65c39b2d84a8b06ef1ae46b885`，并把该引用写入 Corin Profile；随后正式 Float32/Projection 构建也已成功。Corin recipe 与公共导出代码共用同一个 `EnsureRoot`，不再维护单独的 `AssetDatabase.CreateAsset` 根创建实现。

### 6. 原生 FSM 编译与 Debug SourceMap 收口

为使当前正式 Skill 资产能够被唯一编译链完整消费，补齐了以下对象关系：

- Discovery 遍历 `NativeStateMachine` 的 State Body 和 Edge Condition；Edge Condition 同时继承其源 State Body 的可见变量作用域，恢复 `RecoveryEarly`、`RecoveryLate`、`ComboAccept` 等条件查询。
- Native FSM 的 Entry、Any、Exit 条件使用外层 StateMachine operation 作为代次 owner，普通 State 条件使用自己的 operation，避免把不可运行的伪状态当作执行 owner。
- Asset 来源携带 Definition 的 `SourceRevision`；程序级 `CharacterSkillProgramBinding` 使用保留的 `Program` 来源图标识；Native edge 来源按 `edgeId` 写入 SourceMap，避免把 edge identity 错位成 clip identity。
- Runtime Debug SourceMap 按正式 SourceMap entry 解析跨 Native FSM 图的 Edge/Node caller，不再假设 caller 一定属于父 invocation graph；缺失容器时保留完整对象定位信息。

这些修复都作用在现有 Semantic IR、Program SourceMap 和 Runtime Debug 入口，没有新增旁路运行时、第二份作者模型或 fallback 来源。

## 直接 API 输入输出

输入是稳定 identity、正式节点/变量对象、typed 配置、端口 ID、Macro 外部引用和 Canvas layout。输出是原生 Graph/Blackboard/Node/Connection 对象及其序列化结果；代码输出适配器的输出是公共 C# 语句，不是另一份可编辑图数据。

生成代码先通过 `EnsureRoot` 按上下文输出路径创建或替换持久化根并恢复 graph identity/revision，再创建变量/节点、配置节点和外部 Macro，随后绑定 Get/Set、建立连接。内部引用使用本次调用创建的局部对象，范围外 Macro 使用明确资产路径。

## 闭环结论与验收边界

- EventGraph 旧 Document、Mapper 和 Presentation/Skill 旧文档调用者已经迁出；在 `Assets/**/*.cs` 范围内扫描不到 `EventGraphAuthoringDocument`、`ApplyAuthoringDocument`、`HostEventGraphEditorMutation.ApplyDocument` 或事件图 Mapper 的业务代码调用。变更文档和 Git 历史中的旧名称仅作为历史记录保留，不是运行路径。
- 公共 C# authoring 任务已把 `EventGraphAuthoringCodeAdapter.Instance` 接入公共 `export_code` adapter 集合，两个公共 MCP 均已注册并完成事件图现场验证。当前不复制公共 MCP，也不调用旧 Document MCP。
- 固定 motor 参数桥、Pose 条件/BlendSpace/运行/Preview consumer 属于独立运行闭环，不能作为本步作者协议删除的理由，也不能因为作者 API 已有就重新引入旁路。
- Character Float32/Projection 正式构建已经成功；Corin Profile 绑定的事件图、Pose 只读消费者和同一份 Definition SourceRevision 已进入生成产物。当前剩余的是 Unity Editor 验收边界，不是本次源码构建阻塞：Live Console 仍有 3 条既有编辑器空引用错误，尚未进入 Play Mode 或端到端回放。

Corin 资产只由正式 C# generation recipe 创建；旧 EventGraph Document 路径已经删除，不再作为兼容入口恢复。

## 验证记录

源码构建使用了 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，每次构建后立即执行 `dotnet build-server shutdown`。

- `BTSMTL.EventGraphs.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 warning，0 error。
- `ThirdPersonClient.Runtime.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 error，1 个现有 `CharacterInputValueNodes.cs` warning。
- `ThirdPersonClient.Editor.csproj` 源码构建：0 error；当前仅保留 34 个既有 ACL artifact identity warning。此前 Pose 编辑器的跨程序集可见性错误已由 Pose 任务将正式 API 公开后清零。
- 公共 `btsmtl.export_code` 已针对 Corin EventGraph 现场执行成功；生成源码的根创建语句已确认是 `EventGraphAuthoringCode.EnsureRoot<...>(context, ...)`，随后删除验证用输出文件，没有留下第二个 authoring 入口。
- 正式 `btsmtl.generate_assets` 已成功写回 Pose 资产，`LocomotionFullBodyPoseGraph.asset` 生成诊断为 0；当前注册的作者工具只保留 `export_code`、`generate_assets` 和非 authoring 的 `scene_play`。
- 正式 `character.build_float32_products` 最新 job `8a40f68244b34acc92b5d620e804be42` 返回成功：`Exact Float32 Program and Presentation Projection were published.` ProgramId 为 `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`，SourceRevision 为 `aa43ee9c9f02ac8de30c6c8dc3c0cdad54f3b6defaeed1ea54bcd425ee3a6285`，SemanticHash 为 `54f5241e0aee361b758c273957bbe4f766b9b0c05ae2f82fd211ce3b0c69f7ae`，ProgramHash 为 `13b21c66820073e108c63f64eee960f70f65459e468b535f128559643be61cfe`，LayoutHash 为 `9a5b94f9c1cd9536f4dd8949be162bf02ff79d63ad6645e81c9a3815f647d874`，CanonicalBytesHash 为 `6aeffb23dd594588da85610e30e94d836a1c9609b1223df3a7a0f75ad1834f3f`。
- 同一 job 生成的 Presentation Projection 使用相同 ProgramId、SourceRevision 和 SemanticHash，ContractHash 为 `b56b38e5bf340cb6c2876b0c291d855404167807316c683128593a18a7411a9c`，ProjectionRevision 为 `c0f3985f670358d320b6973afb6883300e9410c8a660beeb73e7976ce2bf236f`；数值配置为 `float32-ieee754`，Target ABI 为 `8`，CanonicalBytes 长度为 `2109553`。
- 删除旧 EventGraph Document 后，`BTSMTL.EventGraphs.csproj` 源码构建通过：0 warning，0 error。历史上一次完整 Runtime 构建曾命中 Unity 生成的旧 csproj 文件列表并引用已删除的 `EventGraphAuthoringDocument.cs`；随后刷新并重新生成项目文件，当前完整 Editor 源码构建已通过。
- Unity Editor 日志已记录本轮 Tundra 编译成功、无 C# 编译 error；域重载后出现 RendererFeature/空对象编辑器警告，属于当前编辑器状态，不是 EventGraph 编译证据。
- Unity MCP 目标实例 `3C_Client@e852139597e42532` 当前不在 Play Mode；磁盘复核确认 Corin EventGraph 只保留正式 Start/Update 生命周期节点、无变量和连接，Profile 仍绑定同一事件图 GUID。生成的 Simulation Program 与 Presentation Projection 时间戳分别为 `2026-09-13 06:41:26` 和 `2026-09-13 06:41:29`。
- `git diff --check`：没有发现空白错误；LF/CRLF 输出只是 Git 行尾提示。

本轮没有进入 Play Mode，也没有运行端到端回放。源码、正式作者资产、Pose 只读消费者、Corin EventGraph/Profile 绑定以及 Float32/Projection 发布链已经闭环；Unity Console 清洁和实机端到端行为仍需用户按项目验收边界自行确认。

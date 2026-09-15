# 执行记录

## r4 当前实施状态

r4 `ue-animation-update` 已替代此前允许 Corin 空图或删除 Corin 接入目标的方向。本窗口已将 Corin EventGraph recipe 按正式 API 生成到实际资产，并把 Profile 绑定到同一 GUID；随后通过正式资源配置和 Foot 校准发布链修复既有依赖，Float32 Program 与 Presentation Projection 已按本次合同重新发布。

r4 要求的事实链是：正式原始 Body/Intent 观测 → Corin 原生 EventGraph 计算七项已有动画派生量 → 唯一 typed 变量帧 → Pose/RootOrientationWarp/MM 等原消费者。当前 `CharacterPresentationFactFrame` 已改为提供 EventGraph 所需的原始 `Velocity`、`Rotation`、`DesiredPlanarVelocity`、`DesiredFacing` 和 `HasMotion`，Host Contract 已改为声明并读取这些原始输入；旧派生计算和旧历史已从 Projector 删除。

此前的 Owner 冲突已在本次实现中直接收口：FactFrame 只保留原始输入和帧身份，Pose/Presentation 通过同一次更新传递的 typed frame 读取派生结果；没有引入兼容桥、Fact 回填或第二份原始观测。r4 D7 的目标边界仍保持，Action/Foot/Root Warp 的局部算法没有移入 EventGraph。

本窗口已完成的 r4 前置审计：完整重读 r4 规划与四份规范；确认 `CharacterAnimationEventGraphHost` 已有原生实例/生命周期壳；确认 Corin recipe 已改为真实输入、计算、分支、Set/Get、历史和连接；确认旧 Document/motor 桥没有恢复；确认 Pose 消费链已接入同一 typed frame，未向规划窗口发送消息。

当前源码链、Corin EventGraph 资产和 Float32/Projection 发布物已完成；剩余边界是当前 Unity Editor 的通用 Console 历史错误和 Play Mode/端到端行为验收。不手改 YAML，不绕过任何完整性门禁。

## 当前基线

本次按 `r2@fb937f4415ff6593e1c9affbea37e465c77d617f` 实施。r2 已把事件图作者入口从 Agent Document 改为原生直接 API 和公共 C# 输出薄适配：事件图不再新增 Document 分片、Document schema、专用 Reconciler、专用 MCP 或整图 DTO 中转。

本轮直接收口事件图运行/变量文件、事件图直接作者 API、事件图 C# 输出薄适配，以及由 Fact 派生量迁移所必需的 Pose/Presentation 消费接口；没有恢复固定 motor 桥，也没有把 Action/Foot 局部 owner 复制进 EventGraph。

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

`CorinAnimationEventGraphAuthoringCode` 已升级为 `character.animation-event-graph.corin/v2`。入口通过公共生成上下文解析精确 Definition 和 Profile，按原生直接 API 创建或清空正式 `CharacterAnimationEventGraph`，声明七个公开动画变量和两个私有历史变量，创建原始 Host 输入、数学节点、分支、Start/Update、Get/Set，并以稳定 node/connection identity 完整接线。Action Weight 和 Foot Placement Weight 仍由各自正式 owner 管理，没有被复制进事件图。

入口的 `SourceCodePath` 与公共 bridge 使用同一套项目路径计算，输出路径固定为 `Assets/Configs/Character/Corin/Pipeline/Presentation/EventGraphs/CorinAnimationEventGraph.asset`。当前源码、EventGraph 工程、Runtime 工程和 Editor 工程均已编译通过；正式 `btsmtl.generate_assets` 已成功替换资产。磁盘复核为 v2、9 个变量（7 个公开、2 个私有历史）、47 个节点、50 条连接、1 个 Start、1 个 Update，且没有旧 `presentation.*` 派生 Fact ID。Profile 和 Projection 均引用该 EventGraph GUID。

### 6. 原生 FSM 编译与 Debug SourceMap 收口

为使当前正式 Skill 资产能够被唯一编译链完整消费，补齐了以下对象关系：

- Discovery 遍历 `NativeStateMachine` 的 State Body 和 Edge Condition；Edge Condition 同时继承其源 State Body 的可见变量作用域，恢复 `RecoveryEarly`、`RecoveryLate`、`ComboAccept` 等条件查询。
- Native FSM 的 Entry、Any、Exit 条件使用外层 StateMachine operation 作为代次 owner，普通 State 条件使用自己的 operation，避免把不可运行的伪状态当作执行 owner。
- Asset 来源携带 Definition 的 `SourceRevision`；程序级 `CharacterSkillProgramBinding` 使用保留的 `Program` 来源图标识；Native edge 来源按 `edgeId` 写入 SourceMap，避免把 edge identity 错位成 clip identity。
- Runtime Debug SourceMap 按正式 SourceMap entry 解析跨 Native FSM 图的 Edge/Node caller，不再假设 caller 一定属于父 invocation graph；缺失容器时保留完整对象定位信息。

这些修复都作用在现有 Semantic IR、Program SourceMap 和 Runtime Debug 入口，没有新增旁路运行时、第二份作者模型或 fallback 来源。

## 直接 API 输入输出

输入是稳定 identity、正式节点/变量对象、Float/Int32/Bool/Vector2/Vector3/Quaternion/Enum typed 配置、端口 ID、Macro 外部引用和 Canvas layout。输出是原生 Graph/Blackboard/Node/Connection 对象及其序列化结果；代码输出适配器的输出是公共 C# 语句，不是另一份可编辑图数据。

生成代码先通过 `EnsureRoot` 按上下文输出路径创建或替换持久化根并恢复 graph identity/revision，再创建变量/节点、配置节点和外部 Macro，随后绑定 Get/Set、建立连接。内部引用使用本次调用创建的局部对象，范围外 Macro 使用明确资产路径。

## 闭环结论与验收边界

- EventGraph 旧 Document、Mapper 和 Presentation/Skill 旧文档调用者已经迁出；在 `Assets/**/*.cs` 范围内扫描不到 `EventGraphAuthoringDocument`、`ApplyAuthoringDocument`、`HostEventGraphEditorMutation.ApplyDocument` 或事件图 Mapper 的业务代码调用。变更文档和 Git 历史中的旧名称仅作为历史记录保留，不是运行路径。
- 当前源码链已改为原始 Fact → 一次 Native EventGraph Update → 唯一 typed variable frame → Pose/RootOrientationWarp/Foot/诊断读取；七个派生量不再由 FactProjector 生产。
- `BTSMTL.EventGraphs`、`ThirdPersonClient.Runtime`、`ThirdPersonClient.Editor` 已用规定参数编译通过。Unity 实例 `3C_Client@e852139597e42532` 已连接且不在 Play Mode；正式 EventGraph 资产生成成功。
- 最新 `character.build_float32_products` job `1de6a33268a6408f9dfbb263e572b95d` 返回成功：Float32 Program 与 Presentation Projection 已发布，Target ABI 为 8，ProgramId 为 `character:c7a7c1e3f7e64d81b5a04a90cbeb8d4e`，SourceRevision 为 `f3d9cc6d338fe304c2e696f76d9fad2761c4ec646d291901e7f67b811d60fb89`，SemanticHash 为 `6a3e94dd0386e7308c0de9cf69d0e054117f231fed252faef57104d25fbbf4fd`，ContractHash 为 `dc4259e3f9ed270be6d3d9ec970c2153c5c53502db1a8088a52ba64c0b5ebdcd`，ProjectionRevision 为 `24d4f2e52bac9b0413aca0183d77c55e031be9982cdc95ef0399afe970bd21e0`。
- 当前 Console 总错误仍有 3 条通用编辑器错误（`ArgumentNullException`、`SerializedObjectNotCreatableException`、`NullReferenceException`）；按 `EventGraph`、`CharacterAnimation`、`CorinAnimation`、`Presentation Projection`、`Foot Analysis` 过滤均为 0 条。本次没有出现 EventGraph 编译或生成错误；尚未进入 Play Mode 或端到端回放。

Corin 资产只由正式 C# generation recipe 创建；旧 EventGraph Document 路径已经删除，不再作为兼容入口恢复。

## 验证记录

源码构建使用了 `--disable-build-servers /nr:false /p:UseSharedCompilation=false`，每次构建后立即执行 `dotnet build-server shutdown`。

- `BTSMTL.EventGraphs.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 warning，0 error。
- `ThirdPersonClient.Runtime.csproj` Rebuild，`/p:BuildProjectReferences=false`：0 error，1 个现有 `CharacterInputValueNodes.cs` warning。
- `ThirdPersonClient.Editor.csproj` 源码构建：0 error；当前仅保留 34 个既有 ACL artifact identity warning。此前 Pose 编辑器的跨程序集可见性错误已由 Pose 任务将正式 API 公开后清零。
- 公共 `btsmtl.export_code` 的历史现场验证确认了正式根创建语句为 `EventGraphAuthoringCode.EnsureRoot<...>(context, ...)`，没有留下第二个 authoring 入口。
- 正式 `btsmtl.generate_assets` 当前已成功写回 Corin EventGraph，诊断为空；资产内容为 v2、7 个公开变量、2 个私有历史变量、47 个节点和 50 条连接，Profile 绑定 GUID 与资产 meta 一致。
- 正式 `character.configure_animation_resources` 分析计划 `4e99bc3d403bb9ba1b168fa0b6c717e9ff588763e96202ea5afe8a9b17677d84` 已按事务应用，Profile 的 41 个 blendshape 绑定和 6 个 Corin Prefab 的 `Corin_face` Mesh hash 已统一为当前 Mesh。
- 正式 `character.foot_rig_calibration` 已发布 `Corin.FootPlacementRig` 的当前几何 identity `068338447a188c3cc55ab1e91857277a0a432f68b514f9e270e7c9dda6636bf6`，没有新建 Foot owner。
- 正式 `character.build_float32_products` job `1de6a33268a6408f9dfbb263e572b95d` 返回成功，Presentation Projection 的 `ContractHash` 为 `dc4259e3f9ed270be6d3d9ec970c2153c5c53502db1a8088a52ba64c0b5ebdcd`，`ProjectionRevision` 为 `24d4f2e52bac9b0413aca0183d77c55e031be9982cdc95ef0399afe970bd21e0`，Projection 中的 `m_AnimationEventGraph` GUID 为 `d90ccc65c39b2d84a8b06ef1ae46b885`。
- 删除旧 EventGraph Document 后，`BTSMTL.EventGraphs.csproj` 源码构建通过：0 warning，0 error。历史上一次完整 Runtime 构建曾命中 Unity 生成的旧 csproj 文件列表并引用已删除的 `EventGraphAuthoringDocument.cs`；随后刷新并重新生成项目文件，当前完整 Editor 源码构建已通过。
- Unity Editor 日志已记录本轮 Tundra 编译成功、无 EventGraph C# 编译 error；当前通用 Console 错误未被本任务冒充为已解决。
- Unity MCP 目标实例 `3C_Client@e852139597e42532` 当前不在 Play Mode；磁盘复核确认 Corin EventGraph 已包含正式 Start/Update 生命周期、7 个公开变量、2 个私有历史变量和 50 条连接，Profile/Projection 仍绑定同一事件图 GUID。当前 Projection 已由成功 job 覆盖。
- 本任务相关文件的 `git diff --check` 通过；全工作树仍有其它既有资产的 trailing whitespace，未扩大范围清理。

本轮没有进入 Play Mode，也没有运行端到端回放。源码、正式 EventGraph 资产、Pose 只读消费者、Profile/Projection 引用和本次 Float32/Projection 合同发布均已完成；当前剩余的是用户按项目边界进行 Play Mode/端到端行为验收，以及处理与本任务无关的通用 Unity Console 错误。

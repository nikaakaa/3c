# PoseGraph串行实施记录

## 当前执行入口（COMM-20260906-01）

当前规划与审查的唯一执行入口为`D:/Unity_Project_1/3C/openspec/changes/refactor-character-pose-graph-architecture/execution.md`。实现读取其中`COMM-20260906-01`、`POSE-REVIEW-20260906-01`、`POSE-EXEC-20260906-01`和`POSE-DEPENDENCY-20260906-01`四节连续推进；后文历史阶段通过只覆盖原固定提交，不能用来宣称当前Canvas迁移或整体重构已经完成。

- 实现继续在`C:/Users/Lenovo/.codex/worktrees/a323/3C`、分支`codex/posegraph-luna`工作，不新建任务或工作树。详细剩余项继续使用本目录`tasks.md`，当前整体为`CHANGES_REQUESTED`，第21.x项不能因文档已更新而勾选完成。
- 规划维护主目录execution中的范围、接口选择、完整执行要求、审查结果及协调事件；实现把代码提交与验证证据写回本文件，并按真实结果更新tasks，不维护第二份方案，不从长聊天记录拼接要求。
- 跨任务决定由规划自行读取`D:/Unity_Project_1/3C/docs/coordination-progress.md`再落实。协调文档仅协调窗口写；实现仍只联系原Pose规划，跨任务不绕过既有配对。
- 每批工作记录一次写全任务/事件、稳定提交、绝对文件路径、业务输入/处理/输出、状态和资源归属、旧路径删除、验证命令/实例/版本、结果、证据路径、失败及剩余项。源码编译、正式产物发布和端到端验收分别说明，不扩大历史通过结论。
- `TASK_READY`、`CROSS_TASK_QUESTION`、`COORDINATION_INVALIDATION`作为本文档事件标签；提报问题先集中写全背景、固定源码证据、影响、已尝试处理、需要的决定和方案业务取舍，规划审查后收回主目录执行文档。
- 普通提交、编译、进度、收到、已读、仍在等、无变化不发消息、不索取回执、不定时轮询。只有需要开始/调整执行、处理真实阻塞或接收已审查交付时，发一次文档绝对路径、章节/事件编号与所需动作的简短通知。同一问题补充先在文档合并，不逐条转发。
- 已授权工作持续推进，小步中文提交，保护其它任务的未提交改动；只有无法独立解决的实际交叉依赖才形成协调事件，减少消息不等于增加确认或停工。

## 协作切换记录（POSE-COMM-20260906-01）

本次只更新现有执行文档的读取入口与写入分工，业务代码、迁移资产、编译和Build未执行。当前源码审查仍对应`bfe2b8aa27b43c832180794aeab8cc9f1f5ddd0a`；迁移器局部批准和整体质量未通过的边界详见规划文档当前审查节。后续实际执行结果由实现继续写在本文件中，不能把本次规则切换记成第21.x项完成。

## 21.1 Pose Source Compiler分域小步（POSE-EXEC-20260906-02）

状态：提交`203264abdda0c486e97b6c4960eae153bb033337`完成21.1的Pose Source子步；21.1整体仍未完成，其他Projection领域仍由总Compiler掌握。

- 输入：`CharacterAnimationPresentationProfile`，包括root与Linked Pose Graph owners、reachable Pose Graph、Graph-owned Source Slot、Profile Source Binding和Rig。
- 处理：新增`CharacterPresentationPoseSourceCompiler` Module。该Module集中处理Source Slot资产归属、reachable Graph遍历、Slot唯一性、Binding路径与类型校验、Binding.RequireValid、稳定SourceIndex分配和孤立Binding诊断；输出`CharacterPresentationPoseSourceCompilationResult`，其中包含typed `CharacterPresentationPoseSourceCompilationCatalog`与诊断列表。
- 输出：`CharacterPresentationProjectionCompiler`只接收Source Compilation Result，把诊断合并到原有Projection诊断链，并继续把Catalog交给Motion Matching、Blend Space、Pose Source和Pose Compilation输入；Projection字段、Source identity、排序、错误文本顺序和发布调用没有改变。
- 资源/状态归属：Source Compiler只拥有本次编译期的Catalog与诊断，不创建Runtime资源、Playable、缓存、Writer、第二Projection或fallback；总Compiler仍拥有整体Projection组合与唯一发布。
- 删除旧路径：从总Compiler删除嵌套`PoseSourceCompilationEntry/Catalog`、`CompilePoseSourceCatalog`及其Pose Graph owner/reachability遍历；没有保留兼容别名或反向同步。
- 修改文件：`C:/Users/Lenovo/.codex/worktrees/a323/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Compilation/Presentation/CharacterPresentationProjectionCompiler.cs`、`C:/Users/Lenovo/.codex/worktrees/a323/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Compilation/Presentation/CharacterPresentationPoseSourceCompiler.cs`及其`.meta`。
- Center change：`faadf46ee02e41b99c38eca310e7f923`；RunId：`e2a433e9d7144452b6c61c13865768de`。
- 静态证据：`git diff --check`通过；提交范围只有三个Pose Editor文件。
- Compile证据：统一RunHost执行后状态为Faulted，日志实际使用`D:/Unity_Project_1/camera-zzz/3cDemo/Client/3C_Client`，不是本worktree，不能作为当前源码通过证据。日志：`D:/Unity_Project_1/3C-Artifacts/3c-gameplay/.staging/58af558e543f45f79855db6d833f8f62/Logs/unity-editor.log`。
- 直接Unity batchmode也未形成当前worktree编译结果，只记录Licensing Client validation/access token失败。日志：`C:/Users/Lenovo/.codex/visualizations/pose-source-compile-20260906.log`。
- 剩余项：未执行Corin迁移、正式Build或Projection重建；21.1的Foot事件、Producer/Camera/Cue、Blend/State、Motion Matching、Equipment和Revision Compiler尚未分域。外部未提交Runtime改动和Unity生成meta继续保留，不纳入本步。

## 21.1 Animation Blend Compiler分域小步（POSE-EXEC-20260906-03）

状态：提交`494f80280`、`b82e472b9`和`78158c722`完成21.1的Animation Blend子步；21.1整体仍未完成。

- 输入：现有Pose Graph、Animation Blend Policy、Pose StateMachine Transition、Direct Inertialization Policy、Motion Matching Jump Blend Policy、Rig和Semantic Producer entries。
- 处理：新增`CharacterPresentationAnimationBlendCompiler` Module，整体承接Blend curve/profile catalog、State Transition与Inertialization规则收集、Blend authoring selection拓扑、AnimationSlot/Source producer endpoint解析、BlendStack transition payload与Transition Routing plan生成。
- 输出：Module提供typed `Compilation`，包含Curve/Profile catalog和稳定index字典；Projection总Compiler只调用`CompileCatalog`与`CompileNodes`，把结果传给既有Blend Space、Pose Source和Pose Program组装链。
- 资源/状态归属：该Module只处理Editor编译期canonical payload和诊断，不创建Runtime BlendStack、Playable、Writer、第二Projection或fallback；现有Blend曲线、Profile、State Transition、Inertialization和Motion Matching语义保持原实现。
- 删除旧路径：总Compiler中的Animation Blend catalog类型、Blend authoring selection拓扑、Transition Routing和Node payload编译实现已删除，没有保留转发类或双写路径。
- 修改文件：`C:/Users/Lenovo/.codex/worktrees/a323/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Compilation/Presentation/CharacterPresentationProjectionCompiler.cs`、`C:/Users/Lenovo/.codex/worktrees/a323/3C/3cDemo/Client/3C_Client/Assets/GameScripts/Main/Editor/CharacterSimulation/Compilation/Presentation/CharacterPresentationAnimationBlendCompiler.cs`及其`.meta`。
- 静态证据：`git diff --check`通过；三个中文小提交只包含上述Pose Editor文件，外部Runtime未提交现场未暂存。
- Compile证据：本小步未获得当前worktree的有效Unity编译结果；既有RunHost编译记录错误指向其他项目，直接batchmode受Licensing阻断。源码移动未执行Corin迁移、正式Build或产物重建，不能把该小步描述为整体21.1完成。
- 剩余项：21.1仍剩Producer/Camera/Cue、Foot事件、Motion Matching、Equipment和Projection Revision分域；21.2 Family Binding、21.3 Validator、21.4 Runtime根、21.5–21.9继续未完成。

## 21.1 Projection领域Compiler分域收口候选（POSE-EXEC-20260906-04）

状态：提交`55ccc309c`、`e38a7b523`、`e21ebe0d3`、`8125bcad5`、`c658cf7f7`、`ce82d2169`、`0b0aec097`和`6889c30fa`完成Projection领域Compiler分域候选，任务21.1仍未勾选，因为当前worktree尚未获得有效Unity编译结果。

- 输入：既有`CharacterAnimationPresentationProfile`、Canvas Pose Graph、Semantic Producer、Foot Analysis、Rig、Blend/State、Motion Matching、Equipment和Projection ABI输入。
- 处理与输出：
  - `CharacterPresentationAnimationBlendCompiler`输出Blend curve/profile、State Transition、Inertialization、BlendStack routing和typed Blend Node payload。
  - `CharacterPresentationFootEventCompiler`输出注册曲线归一化、Foot Step observation curves和Landing phase边界。
  - `CharacterPresentationProducerCompiler`输出Animation/Camera/Cue Producer、Timeline call site和Playback mode typed result。
  - `CharacterPresentationMotionMatchingCompiler`输出Motion Matching payload、Foot Analysis Source依赖和revision tokens。
  - `CharacterPresentationEquipmentCompiler`输出Equipment visual binding typed result与未解析引用诊断。
  - `CharacterPresentationProjectionRevisionCompiler`输出Projection ABI、Profile/Equipment依赖、Motion Matching和Foot Analysis组成的Revision。
  - `CharacterPresentationPoseSourcePlanCompiler`输出Blend Space plan、Clip source plan、Clip Player一致性和Blend Space parameter contract诊断。
  - `CharacterPresentationProjectionCompiler`只保留根请求检查、各Module调用、诊断合并、Pose Compiler调用、Projection对象组合与唯一发布。
- 资源/状态归属：上述Module只拥有Editor编译期typed result和diagnostics；Runtime Source、Program、Constraint、Writer、Playable、缓存、第二Projection和fallback均未新增。Projection根仍是唯一发布Owner。
- 删除旧路径：总Compiler中的对应Blend、Foot Event、Producer、Motion Matching、Equipment、Revision和Source Plan实现已删除，没有保留转发类型、镜像结果或反向同步。
- 修改文件：上述提交只涉及Projection Editor源码和对应`.meta`，外部Fixed/Float32未提交Runtime现场未暂存。
- 静态证据：每个小步提交前执行`git diff --check`；Projection总Compiler已由3051行缩至约379行，剩余内容是组合、诊断合并和唯一发布。
- Compile证据：既有RunHost compile记录`e2a433e9d7144452b6c61c13865768de`错误使用`D:/Unity_Project_1/camera-zzz/3cDemo/Client/3C_Client`，不能作为当前worktree依据；直接Unity batchmode受Licensing Client validation/access token阻断，日志为`C:/Users/Lenovo/.codex/visualizations/pose-source-compile-20260906.log`。本批没有有效当前worktree编译通过证据。
- 剩余项：21.1需要有效编译闭环；21.2 Family Binding、21.3 Validator、21.4 Runtime根、21.5–21.9作者/Canvas/Scene Play/正式Build和迁移删除继续未完成。没有执行Corin迁移、正式Build或生成Projection/Program产物。

## 21.2–21.4 Family、Projection Validator与根帧协调小步（POSE-EXEC-20260906-05）

状态：提交`1f59e899a`和`2864ff961`完成Family payload按真实Operation Family下沉并保持原descriptor索引顺序的候选；提交`7e4ac5c4b`完成sealed Projection身份、Producer集合和语义合同验证的独立Owner；提交`e171ae5f7`把旧递归图规则Owner明确命名为`CharacterPoseTopologyValidator`，把Projection sealed验证Owner明确命名为`CharacterPoseGraphProjectionValidator`；提交`2a3b413fa`完成Presentation根帧事务的typed Coordinator收口；提交`c972496fa`补齐按显式Numeric Target列表调用正式Character Build的入口。21.2、21.3、21.4仍不能勾选，原因是当前worktree没有有效Unity/Editor编译证据，且21.3的Topology职责仍需继续拆至最终目标边界、21.5–21.9仍未全部完成。

- 21.2输入：`CharacterPoseSymbolicOperation`、Node Definition声明的`CharacterPoseOperationFamily`、当前图节点payload、Graph/StateMachine作用域和已分配的通用Value索引。处理：新增`CharacterPoseFamilyPayloadBindingAdapters`注册表，按Player、StateMachine、AnimationSlot、Inertialization、Composition、ComponentControl、GoalContribution、FullBodyIK分别编译所属typed payload，并返回单一`CharacterPoseFamilyPayloadBindingResult`；主Plan Pass只分配通用句柄、保留Symbolic顺序并汇总结果。StateMachine adapter在消费外层symbolic operation前执行，保持原递归State Graph的Symbolic cursor、Value index和payload排序。
- 21.2输出：`CharacterPoseBoundOperation`字段来源不变，`CharacterPoseFamilyPayloadPlan`、Worker Plan、Seal和Runtime ABI仍只消费一份绑定结果。Adapter只拥有Editor编译期descriptor数组和索引，不创建Runtime页、Playable、Writer或第二Projection；旧的主遍历内按Requirement直接调用各类payload编译路径已删除，没有兼容转发。
- 21.3输入：编译后的`CharacterPresentationProjection`与`CharacterPresentationSemanticContract`。处理：新增`CharacterPoseGraphProjectionValidator`，集中验证Projection ABI、非空身份、Projection/Contract hash、Producer数量、稳定index、Producer identity、channel/source合同，并在这些检查通过后调用`RequireContract`完成最终sealed合同验证；`CharacterSimulationBuildOrchestrator`删除同一组字段的第二份解释；旧递归图校验Owner改名为`CharacterPoseTopologyValidator`，避免把Topology规则继续伪装成Projection验证。节点局部规则、Topology规则和Projection sealed验证仍未完全拆成最终目标的三个实现层。
- 21.4输入：Body、Fact、Animation、Equipment、Linked Pose Session、Camera、Diagnostics和PoseRoot正式模块。处理：新增`CharacterPresentationFrameCoordinator`，整体拥有Frame active、Pending Body/Animation/Camera、sample clock、pose output、Pose Plan diagnostics、performance context和Begin/Advance/Complete/Abort/Reset/Dispose；`CharacterSimulationPresentationRuntime`只保留输入接入、Producer/Camera/Cue命令分发、模块装配和Coordinator转交。所有Root Frame阶段仍沿用原lineage、Result、唯一Complete/Abort和Failure日志边界，没有新增第二事务或fallback。
- 21.8入口：`CharacterSimulationBuildOrchestrator.Build(definition, targets)`与`DryRun(definition, targets)`显式接收有序Target Adapter列表；正式`Build(request)`继续先编译同一Semantic IR和Presentation Projection，再为全部Target建立Publish Stage，最后由同一事务发布，未复制主目录generated文件。当前没有用该入口重建Corin产物。
- 资源/状态归属：Family Adapter只拥有编译期payload；Projection Validator只拥有验证诊断；Frame Coordinator只拥有actor-local根帧状态；Formal Build只拥有一次Semantic/Target/Projection原子发布。外部Fixed/Float32未提交Runtime修改、`Runtime/Character/Pipeline/Simulation/`目录和Unity生成meta均未纳入提交。
- 验证：各代码小步执行`git diff --check`通过；当前worktree不存在Unity生成的`ThirdPersonClient.Editor.csproj`，尝试的dotnet命令因项目文件不存在退出，并已执行`dotnet build-server shutdown`；RunHost旧记录仍指向`D:/Unity_Project_1/camera-zzz/3cDemo/Client/3C_Client`，直接Unity batchmode仍因Licensing Client validation/access token失败。因此本批没有当前worktree编译通过、正式Build、Corin产物或E2E证据。
- 剩余项：21.2需在有效Editor编译后确认所有Family Adapter调用和ABI未变；21.3需继续删除/收窄Topology与Definition中的重复职责；21.5 Pose窗口与旧`AnimationPreviewRuntime`仍未迁入正式Scene Play；21.8需实际执行一次多Target正式Build；21.9当前没有Pose对ACL共享Source/Projection合同的直接引用，不接入ACL。

## 21.5–21.7作者表面与Preview边界审计（POSE-EXEC-20260906-06）

状态：本次不修改作者资产或Preview运行路径，只固定现有代码事实和决策边界。21.5仍等待`rebuild-btsmtl-preview-with-scene-play`的正式Scene Play协调器，不能把当前Preview Fixture Scene误记为Scene Play完成；21.6选择方案A；21.7接受当前自建GraphView并明确维护成本，不能把它描述成完整CanvasCore编辑器。

- 21.6方案A的现行证据：`CharacterPoseCanvasView`的投影创建、拖线、删除、移动、StateMachine transition和节点创建都把请求交给`CharacterPoseCanvasEditorMutationAdapter`/StateMachine Adapter；Clipboard使用同一`IGraphAuthoringDomainMutation`，Document、Undo和MCP继续通过既有Mutation Owner。CanvasCore GraphView的非virtual物理API仍无法从基类类型层禁止，但当前源码没有第二个Pose Graph对象写入Owner或绕过Mutation的Pose Canvas路径。业务取舍是保留GraphView交互和较低迁移成本，接受以后需要用源码审计守住唯一写入口。
- 21.7当前实现边界：`CharacterPoseCanvasView.cs`为877行自建GraphView，负责Node/Port/Edge投影和交互事件；它不提供通用Flow执行、反射调用、自动类型转换或第二Graph数据源。选择保留它是因为现有Pose专用端口、StateMachine页面和typed Mutation已经接通；代价是节点渲染、端口交互、GraphView版本变化和交互回归由项目自行维护。未宣称接入ParadoxNotion现成Graph Editor。
- 21.5当前实现边界：`CharacterPoseAuthoringBottomDock`仍持有`CharacterAnimationPreviewFixtureSession`、播放/暂停/单步/seek和Edit Mode Preview Scene；`AnimationPreviewRuntime`仍存在于`CharacterPipelineAuthoringPreviewController`和相关查询Fixture。它不是正式Scene Play Session，因此不删除这些路径，也不建立第二Preview入口，等待共享Scene Play change的唯一协调器和明确提交。

## Center当前worktree编译闭环（POSE-EXEC-20260906-07）

状态：使用正式 Center change `faadf46ee02e41b99c38eca310e7f923`和正确 worktree `C:/Users/Lenovo/.codex/worktrees/a323/3C`连续执行 compile。Run `f96920fe5295479a8e462e45260a266a`首次实际进入当前工程并暴露`RuntimeDiagnosticsContext`引用；提交`912eddd9e`修复后，Run `a8fc23a8a28f42ad976803473ee292c0`暴露Canvas migration state、Legacy字段访问级别和主角色注册漏传ActorId；提交`66162de95`、`ed60d8f98`后，Run `1748ac596c604b4a8f7b217122932b1a`只剩Fixed注册漏传ActorId；提交`ccdf754b1`后，Run `9c436bdc999b4877b958cc3f79970bf9`只剩并行DeterministicRollback注册与Performance Capture链错误；提交`7a17e03aa`修复Rollback注册后，最终Run `e9c992969065417db8feb5e667427f44`确认Pose/Presentation/Fixed/Rollback相关错误已消失。

- 当前 Center 失败原因只剩`ThirdPersonPerformanceCaptureAgent.cs`引用缺失的并行性能API：`PerformanceCameraInputOverride`、`PrepareReplay`双参数重载、`FixedCharacterInputTraceMode.ReplayPaused`、`GameplayLabBootstrap.Current`和`FixedCharacterInputTraceModule.ResumeReplay`。这些属于性能链现有接口不一致，不属于本Pose改动；没有新增fallback、兼容入口或临时API。
- 本次编译同时确认：`CharacterPresentationFrameCoordinator`、`CharacterPoseGraphProjectionValidator`、Canvas migration state、`CharacterSimulationActorRegistration`、Fixed和DeterministicRollback注册器均已进入编译，当前没有它们的错误。Center证据目录：`D:/Unity_Project_1/3C-Artifacts/3c-gameplay/Runs/e9c992969065417db8feb5e667427f44/`。
- 结论：当前 worktree 的 Pose/Presentation 代码已通过“无本域编译错误”的边界，但整个 Unity Runtime/Performance 程序集仍不能宣称编译通过；未执行正式 Corin Build、Scene Play或E2E。
## 文档协作与写入分工（COMM-20260906-01）

自2026-09-06起，规划与实现按本节协作。本节及下面的当前执行要求优先于后文历史阶段记录；历史提交、编译与回放通过只覆盖原记录中的固定版本，不代表当前Canvas迁移或整体架构已通过审查。

- 跨任务决定唯一入口为`D:/Unity_Project_1/3C/docs/coordination-progress.md`。规划自行读取其中的决定、负责方、动作和交付条件，该文件只由协调窗口维护。
- 本文件`D:/Unity_Project_1/3C/openspec/changes/refactor-character-pose-graph-architecture/execution.md`由Pose规划维护当前范围、接口选择、完整执行要求、审查结论和需要协调的事件。详细要求在文档中合并成完整的一批，再通知实现读取具体章节。
- 实现固定使用`C:/Users/Lenovo/.codex/worktrees/a323/3C`与`codex/posegraph-luna`，将提交、改动范围、验证及失败证据写回该工作树现有的`openspec/changes/refactor-character-pose-graph-architecture/execution.md`，并按实际完成状态更新同目录`tasks.md`。不再从聊天记录拼接要求，不另外建立一份方案或工作记录。
- 原规划/实现配对保持不变：规划`01a06ca7-e964-7eb3-bbb0-fe97b2b26930`，实现`01a06ca7-e503-7e01-8943-7993b5aa81c4`。实现只联系本规划；跨任务问题由规划交协调窗口，不直接联系其它规划或实现。
- 普通提交、编译、进度、已读、收到、仍在等和无变化只写记录，不发消息、不索取回执、不定时轮询。已授权的工作连续推进，不因减少消息而增加确认或停工。
- 仅在需要开始或调整执行、处理无法自行解决的阻塞、接收已审查交付时发送一次简短通知；内容只包含现有文档绝对路径、章节或事件编号及所需动作。同一问题的连续补充先在文档中合并，不逐条发送。
- `TASK_READY`、`CROSS_TASK_QUESTION`、`COORDINATION_INVALIDATION`保留为文档事件标签。提报前一次写全背景、固定源码与具体问题、证据绝对路径、受影响的输入输出、已尝试处理、需要决定的事项，以及可行方案的业务取舍；接收协调决定后由规划将执行要求收回本文档。只有现有资料确实不足的特殊情况才补一次针对性沟通，并回写结论。

## 当前范围与审查（POSE-REVIEW-20260906-01）

审查对象为实现工作树提交`bfe2b8aa27b43c832180794aeab8cc9f1f5ddd0a`及本次只读源码核对。整体结论仍是`CHANGES_REQUESTED`，没有整体`APPROVED`或`TASK_READY`。

已批准范围与未完成范围如下：

| 范围 | 当前结论与执行边界 |
| --- | --- |
| 既有Runtime模块 | 旧PosePlanExecutionRuntime、StagedExecutor、NativeProgram已删除，Source、Program、Constraint、Final Publication已形成真实职责。保留正确实现，继续收回根Runtime掌握的Workspace、Action、Slot、Motion Matching和Linked Pose内部提交/丢弃知识；不能用旧大类删除证明整体重构完成。 |
| Corin一次性迁移器 | `4a3cb9114`、`a30c2ec79`、`1b26bb098`仅限迁移器源码审查通过，包含Legacy语义对账、写入前拒绝混合状态、事务恢复和保存后反序列化对账。尚未证明实际Corin迁移、正式Build及旧模型删除，不能扩大批准范围。 |
| Compiler与Validator | ProjectionCompiler文件3051行、Family Payload Plan文件2164行、ProjectionValidator文件1609行仍承担多领域逻辑和递归图语义。工作树`tasks.md`第21.1–21.3项保持未完成，拆文件或增加转发入口不算职责分离。 |
| 作者窗口与Canvas | PoseGraphEditor文件2204行，BottomDock文件1690行。CanvasCore数据模型已接入，877行CanvasView仍是自建GraphView；AuthoringView暴露可变图/节点，基类非virtual写API也未形成完整隔离。第21.5–21.7项及对应旧项仍未完成。 |
| Preview | 正式Scene Play会话、场景、时钟和旧完整预览生命周期由既有预览任务负责。本任务消费正式命令与提交后的观察数据，并删除Pose窗口的独立预览运行/时钟/Seek链；第19.x项仍未完成，不扩大成第二预览系统。 |
| TrainingEnemy | 整套退役范围保持。配置根、Prefab等删除成果保留，引用闭包、collector、场景/启动/构建专用引用按第15.x项继续对账；不迁移TrainingEnemy，不为缺失正式业务创建占位目标。 |

`8fd3b010f`与`bfe2b8aa2`只修正任务真相和依赖说明，并未完成上述代码整改。当前完整剩余项以实现工作树`C:/Users/Lenovo/.codex/worktrees/a323/3C/openspec/changes/refactor-character-pose-graph-architecture/tasks.md`为执行清单；本节补足审查边界，不复制或另设第二份任务勾选。

## 连续执行要求（POSE-EXEC-20260906-01）

本批继续已有授权与现有工作树，不新建任务、分支或worktree，不重置、回退或改写历史，不夹带其它工作的未提交差异。下面是模块边界与交付要求，不新增本轮MR，也不替用户改变业务优先级。

| 现有任务 | 输入、处理与输出 | 可审查的完成条件 |
| --- | --- | --- |
| 21.1 Projection编译 | 各领域接收所属作者数据和已验证输入，产出所属typed编译结果及诊断；总入口仅组合结果、统一身份并交唯一发布入口。 | 动画源、Foot事件、Producer/Camera/Cue、Blend/State、Motion Matching、Equipment与Revision计算按职责归属，修改某一领域无需修改总入口内部算法。既有字段和正确行为完整保留。 |
| 21.2 Family绑定 | Node Definition或所属Family Adapter解释自己的payload；Pass消费已确定的symbolic operation和typed布局。 | Family内部知道如何绑定自身数据，中央Pass不再递归发现图语义或集中理解全部节点；Schedule、容量、Workspace、Batch确定后再封存ABI。 |
| 21.3 验证 | 节点局部规则归Definition，跨节点连线、可达性和唯一Output/Assembler/FBBIK归Topology；最终校验消费封存结果。 | 删除第二套递归拓扑规则，Projection校验只承担版本、身份、容量及发布合同；错误保留Pass、节点和Source Map定位。 |
| 21.4 根Runtime | 根接收帧输入，调用各模块正式协议，消费同一lineage的结果。 | 根只组织固定阶段、完成、Seal/Discard/Fault，模块自己掌握内部状态和资源；不得重新引入第二根、第二执行器或第二Writer。 |
| 21.5–21.7 作者与Canvas | Graph、StateMachine、TransitionRule、Tuning/Diagnostics分别处理所属作者操作；所有资产写入经唯一Mutation，编译只读作者投影。 | 窗口只组合页面和导航。Canvas写入隔离与现成Graph Editor接入仍是未完成决策，不把当前自建表面描述为完整复用；工作树tasks中两组并列方案保留业务取舍，确需协调决定时一次成文，其他独立整改继续。 |
| 18.x、19.x、20.x、21.8–21.9 | 完成保留资产迁移、正式Scene Play消费及唯一Character Build接线，接收必要且已提交的外部依赖。 | 用唯一Build生成对应Target和同组Projection，迁移对账后删除旧模型、旧Canvas、迁移入口及兼容读取；只在真实落地后更新任务与当前架构文档。 |

每个代码小步必须说明业务输入、处理、输出、状态/资源归属、依赖和删除的旧路径，形成能独立解释和审查的中文提交；不以行数变少、空接口、转发类或同一中央状态拆成多个文件代替模块化。

必要编译和正式构建使用已有统一入口，不新增测试代码、不把手动验证写进tasks。dotnet build/msbuild按项目要求使用`--disable-build-servers /nr:false /p:UseSharedCompilation=false`并在结束后立即执行`dotnet build-server shutdown`。Unity调用显式指定对应实例/项目路径，保留主验收Editor。已有证据只按其真实覆盖范围引用，源码编译、产物发布与用户端到端验收分别记录；失败保留原日志，不绕校验、不复制生成文件、不新增fallback。

## 跨任务依赖与交付记录规则（POSE-DEPENDENCY-20260906-01）

跨任务动态状态直接读取协调文档的对应问题/决定。当前本轮MR顺序是ACL、Timeline、Camera，PoseGraph整支不是其前置；本任务继续原Pose/Canvas范围，仅对实际共享边界作必要复核。

- main已有正式Pose根事务及四类lease，不再为ACL提供另一套根。ACL共享Source/资源、根调用、属性发布、编译序列化的必要增量由主线按协调决定接收；历史源码来源核对不等于接收组合或产物已批准。
- ACL的Projection v14与相机的v14字段布局不同，不能互读。唯一组合版本、完整字段、Create/codec/校验/hash/消费者和正式生成由主线统一；具体当前版本及构建状态读取协调文档P-07/P-08，不将旧数字固定成后续接入要求。
- 当前Canvas迁移与独立质量整改不因ACL整支尚未合入而停工。实际触及共享Source/Projection时，只接完整、已提交、已审查的必要依赖，保留正确源代码、原meta/asmdef和完整字段，不从其它任务脏工作区复制，不将来源锚点当完整cherry-pick白名单。
- Build来源与主线Foot前置分别读取协调文档P-02/P-14。TrainingEnemy的collector归属和实际引用问题需独立取证，不能用ACL交付或另一实例的几何验证替代本任务对账。

实现写回一批完整记录时，至少包含对应任务/事件、稳定提交及绝对路径、实际调用链、输入输出与旧路径删除、运行命令/实例/版本、结果及证据路径、失败与剩余项。规划在本文件收回审查结论，逐项注明批准范围；只有接收方需要行动才发送该文档定位，不发送普通状态回执。

## 本次文档切换证据（POSE-COMM-20260906-01）

实现工作树已提交`415f1422e9502aff30a1137e81451d1d62a4933b`，仅给现有execution增加18行读取入口、写入分工和协作切换记录；没有修改业务代码、资产或任务完成勾选。两处文档的定向`git diff --check`均通过，原历史记录没有删除。本次未执行代码编译、迁移或正式Build。

主目录执行要求已保存；当前主目录存在`index.lock`且有其它任务暂存内容，本次未操作主目录索引、删除锁或提交该工作区。锁沿用协调文档P-05的既有负责方处理，不因同一已登记状态再次发协调消息；实现可直接读取上述当前章节继续工作。

以下为原有历史实施记录。

## 固定接入

- 总源码及行为基线固定为`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`。
- 第一阶段IK通过接入提交为`f32e419`，最后运行实现提交为`5b551cb`，证据包为`20260901-070946-569-c14830f966ee465c887849cfc66b1f2a`。
- 第二阶段每个代码闭环同时比较上一通过提交和固定总基线；新Program／Projection identity允许按ABI更新，但输入、Body、source时间、Pose、Foot、Pelvis、Goal、Solved与Physical业务结果必须对账。
- 工作区原有`.gitignore`、`ProjectSettings.asset`、`stabilize` proposal和`project.md`修改不属于本change，不能夹带提交或回退。

## 统一根帧lineage与事务

状态：候选`0b66bf0`已完成Runtime编译和固定Record正式回放；只有既有Input Value未使用字段警告，0错误，build server已关闭。

- 新增`CharacterPoseFrameLineage`，一次保存Actor、根Frame identity、Presentation Frame、Body Tick、Program Id、Pose Program identity、Projection Revision、Rig Id／Revision和actor-local Tuning Generation。
- 旧`AnimationPresentationFrameTransaction`直接改名并替换为`CharacterPoseFrameTransaction`；旧文件和类型不存在。根事务只保存统一Lineage、现有Owner的typed lease、阶段、Outcome和提交时批次，不保存Program、Source、Constraint或Final Pose内部页。
- `CharacterAnimationPresentationRuntime`在Pending Tuning应用后、打开任一Frame页前构造一次Lineage。成功应用Tuning Candidate时只推进该Actor的Generation；没有新增静态或跨Actor状态。
- 现有Action、Sampling、Slot、Motion Matching、Pose和Workspace lease继续走唯一正式路径，并统一与Lineage的Frame／Presentation身份对账。Barrier、Discard、Fault、Writer与Seal顺序没有变化。
- 本步没有新增Module空壳、wrapper、第二事务或第二执行路径。Source／Program／Constraint／Publication的细分Result仍待后续闭环，不能把本步称为全部任务2完成。

验证包为`Diagnostics/FootPlacementRuns/20260901-073338-183-725de431cb724bd69b05022e5f073450`。正式Proof对第一阶段接入包匹配1044输入、aggregate mismatch 0、divergent frame 0；与固定总基线的trace、runtime、起始Body、tick drive、presentation clock、输入／Body hash和1044帧数组也一致。

两组对照均为2086脚行、1215列，1191业务列逐值相同，24列只含运行时间和实例身份变化且23个identity列一一映射无冲突。Source normalized time／cycle／completion、Presentation Delta、Body Tick／Alpha、Transition前后Reason／Source／Target、Blend／Slot、Action、Foot、Pelvis149、Goal30、Knee15、Solver／Physical101与Time19均无业务差异；几何67186行中的22个业务列相同。

facts71、42个Target、20447条detail、规则、资格、计数、Health／Evidence与quality-score保持，总分61.9只作辅助。正式summary、events和frame查询成功；Unity回到Edit／Idle，workflow failure为空，Console无错误。由此确认统一lineage和根事务未改变本Record覆盖的Barrier、时钟、状态、IK与Physical行为。

## Program Prepare与Result收口

状态：`b2966a8`已完成Runtime编译和固定Record正式回放；只有既有Input Value未使用字段警告，0错误，build server已关闭。

- `PosePlanFrameLease`与`PosePlanPreparedEvaluation`直接改为`CharacterPoseProgramFrameLease`和`CharacterPoseProgramPrepared`，没有保留旧别名。
- Program Prepare只接收根事务的open lineage，在生成现有Completion后返回补齐Completion的同一lineage；根事务只接受其它身份完全一致的completed lineage，外层不再单独传Actor和Render Frame给Barrier。
- `ExecuteEvaluateBarrier`返回`CharacterPoseProgramResult`，集中发布lineage、Frame Outcome、Output Availability、Output Invalid Reason、Graph Invalid Reason和Invalid Operation。外层只消费该typed Result判断是否可提交和生成错误信息，不再读取`AnimationFinalPoseNativeReadBinding`内部Slice解释结果。
- 本步没有改变Native Workspace、Operation调度、Constraint、Writer或Seal顺序。Source Frame、Constraint Result、Final Publication Result和per-operation completion仍待各自Owner迁移，因此任务2.2保持未完成。

验证包为`Diagnostics/FootPlacementRuns/20260901-092212-871-cd74efd1c5414a3f889c4bf95c701bed`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-092319-923-f050f55f05b14b6d8cb7e981269e0af0.json`。固定Record完整消费1044帧，并与上一Proof匹配1044帧、aggregate mismatch 0、divergent frame 0；该包同时作为下一小步的工作区内A基线。

## Program、Constraint与Final Publication Result分型

状态：本步候选已完成Runtime编译、Unity脚本刷新和固定Record正式回放；只有既有Input Value未使用字段警告，0错误，build server已关闭。

- Program Result只表达Stage完成后的Output availability、Output/Graph invalid reason和invalid operation，不再用Physical Writer结果反推Program是否完成。
- `CharacterPoseConstraintRuntime.CompleteFrame`在现有同一调用位置发布typed Constraint Result，携带同一lineage、Goal数量、Solver是否产出及FBBIK Result；外层错误报告直接读取该Result，旧`TryGetFullBodyIkFailure`跨Bank反查入口已删除。
- Final Publication Result单独表达Writer outcome、Pose availability、Applied Completion和Output/Graph failure；根`CharacterPoseFrameTransaction`在Evaluate Barrier后绑定Program、Constraint和Publication三个同lineage Result，只有三者都成功才允许Seal。
- 本步只把`staged Pose完成 -> Constraint闭包验证 -> Physical Writer -> Pending完成 -> 根Seal`之间的事实分型，没有改Operation顺序、Foot/Goal/FBBIK数学、Writer骨骼顺序、Barrier或Bank提交时机。具体Final Publication Module与Writer所有权仍留给任务7迁移，不在这里创建wrapper或第二Writer。

改前A包为`Diagnostics/FootPlacementRuns/20260901-092212-871-cd74efd1c5414a3f889c4bf95c701bed`，改后B包为`Diagnostics/FootPlacementRuns/20260901-093635-128-5af8dc0e351c416fbc292ec4ea4eae5b`，输入Record均为`43357ff3cd384e5cba75d2c31175b116`。B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-093743-709-ac0d05ccdb0e40199172d15d15d88ab5.json`，与A匹配1044帧、aggregate mismatch 0、divergent frame 0。

两包均为2086脚行、1215列；1191个业务列逐值相同，24个运行／实例／Surface／Path identity列变化且全部一一映射。两包Ground Path Geometry均为67186行、27列；22个业务列逐值相同，5个identity列变化且全部一一映射。正式Analyzer schema、覆盖、各规则eligible/matched计数、七维分项和84.2浅层参考分一致；`analysis.json`仅Sample/文件hash、detail/index字节与hash及分析耗时不同。由此确认本Record覆盖的Body、source时钟、Foot、Pelvis、Goal、Solver与Physical结果没有因Result分型改变；其它路线和非固定表现调度仍未由本包覆盖。

## Source Demand与Source Frame typed交接候选

状态：Runtime按规定参数编译成功，Unity脚本刷新0错误；Foot Calibration与Projection由对应Owner补齐后，同输入Record已重新完整消费1044帧。因为补采样时同时包含外部Foot／Projection身份更新，本证据用于确认当前完整工作区可作为下一小步A基线，不把它错误归因为Source候选的隔离A/B；任务2.2仍因per-operation completion未建立而保持未完成。

- Program在原`PrepareEvaluation`紧邻调用前生成一次完整completion lineage和`CharacterPoseSourceDemand`，Demand只引用现有只读provider demand及本帧Action／Provider source数量，不取得Workspace写权限。
- 现有唯一source准备路径成功后发布`CharacterPoseSourceFrameResult`，显式区分Pending、Ready、Invalid与Prepared/Awaiting/Invalid outcome；`CharacterPoseProgramPrepared`绑定该Result，根`CharacterPoseFrameTransaction`保存Demand与Source Result并验证与后续Program／Constraint／Publication相同lineage。
- Completion数值的成功帧生成次数、source采样、Playable准备、capture、release、Program workspace、Barrier和Writer顺序没有移动；本步没有建立Source Module空壳、第二source页或fallback。Source物理资源与Owned Pending页仍由任务2.4和任务4迁移。

原请求在0输入帧时失败，正式错误为`Canonical Fixed input replay timed out while starting Gameplay Lab`；根因是并行外部改动已把`CharacterFootPlacementRigCalibration.CurrentSchemaVersion`从4提升到5并新增Current Support Footprint字段，但当时Calibration asset仍是旧内容且Projection未显式重建，导致`CharacterPresentationProjection.IsValid=false`、Actor roster为空。本change没有修改其资产、构建产物或加入兼容绕过。

对应Owner闭合后，补采样包为`Diagnostics/FootPlacementRuns/20260901-110537-059-6498a7fef1cc44319a37d751e921506e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-110644-970-32ecf37405fa451ebc1a893f21731215.json`。它对上一正式Proof报告Program／Projection七个aggregate identity字段变化，但`DivergentFrameCount=0`、`FirstDivergentRelativeFrame=-1`、`FirstFrameFields=[]`。因此该包只证明外部身份更新后的当前工作区逐帧行为仍与原Record一致，并作为下一小步A；不把叠加外部改动后的结果伪装成Source候选的单改动归因证据。

## Program Prepared实现所有权收口

状态：`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有包或既有Input Value未使用字段，build server已关闭。Unity脚本刷新完成且Console 0错误，同输入A/B正式回放通过。

- `CharacterPoseProgramPrepared`只保留`CharacterPoseSourceFrameResult`、同一`CharacterPoseFrameLineage`与typed Source outcome，不再向动画表现根暴露Presentation Delta、`CharacterPoseGraphNativeBinding`、`CharacterPoseGraphStagedExecutor`、Pending／Committed Final Read binding或Committed Final存在性。
- `PosePlanExecutionRuntime`把上述实现数据保存为Program-owned pending prepared状态；`PrepareEvaluation`对同一打开Frame只允许发布一次，`ExecuteEvaluateBarrier`按同一lineage和Completion验证后一次消费并清空。重复Prepare、跨Frame prepared或重复Barrier执行不再能借外层复制的Native struct进入实现。
- Seal、Discard、Reset和Dispose统一清空Program prepared状态；根`CharacterAnimationPresentationRuntime`仍只读取Source Frame与lineage并把typed prepared合同送回同一Program Runtime。Animancer Evaluate、Stage循环、world-aware输入装配、Constraint Complete、Physical Writer、Pending完成和根Seal顺序没有移动。
- 本步只建立Program prepared实现的Owned Pending边界。Source、Constraint与Final Publication各自Owned Pending页、根typed lease以及per-operation completion仍未完成，因此任务2.4和2.2都不提前勾选。

A包为`Diagnostics/FootPlacementRuns/20260901-110537-059-6498a7fef1cc44319a37d751e921506e`，B包为`Diagnostics/FootPlacementRuns/20260901-111208-798-b1a8446ff183468d9eb63f531a00f08d`，输入Record均为`43357ff3cd384e5cba75d2c31175b116`。B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-111310-573-dd19de7b80bc406a8c7ab741cbaac122.json`，与A精确匹配1044帧。

两包均为2086脚行、1215列；1191个业务列逐值相同，24个Run／实例／Surface／Path identity列变化且全部一一映射。Ground Path Geometry均为67186行、27列；22个业务列逐值相同，5个identity列变化且全部一一映射。Analyzer schema、Program／Projection／Pose／Profile identity、覆盖、全部规则计数、七维分项和84.2浅层参考分一致；`analysis.json`只在Sample／文件hash、detail／index大小与hash和分析耗时上变化。由此确认本Record覆盖的Body、source时间、Foot、Pelvis、Goal、Solver与Physical结果没有因Program prepared所有权收口改变。

## Program Prepared原子Pending页候选

状态：`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有包或既有Input Value未使用字段，build server已关闭。Unity脚本刷新完成且Console 0错误；同输入B执行和Foot诊断封存完成，但正式Replay Proof在发布前被并行外部脚本编译触发的程序集重载中断，因此本步保留为独立候选，不能作为下一步正式A基线。

- 将Program-owned prepared的lineage、Presentation Delta、Native Frame、Staged Executor、Pending／Committed Final Read与Committed Final存在性合并为单一`CharacterPoseProgramPreparedPage`，`HasValue`只在全部字段写入后提升。
- Pending Page只接受同一typed `CharacterPoseProgramPrepared`，一次`Consume`先冻结只读State再原子Clear；重复Prepare、缺失Page、跨lineage Consume和内部Completion不一致保持fail-closed。Runtime不再分别维护八个可独立更新和清理的prepared字段。
- Begin、Seal、Discard、Reset与Dispose都只操作同一Page；Barrier取得Page State后仍按原顺序执行Animancer Evaluate、Stage、Constraint、Writer与Pending完成。Foot、Pelvis、Goal、FBBIK、Physical Writer和Operation数据均未修改。
- 当前Page是Program Frame Pages的正式组成边界，不建立第二Program、第二Frame或兼容路径。完整`CharacterPoseProgramFramePages`、Operation Completion和其它Module Pending页仍待后续迁移，任务2.2、2.4和5.5均不提前勾选。

A包为`Diagnostics/FootPlacementRuns/20260901-111208-798-b1a8446ff183468d9eb63f531a00f08d`，B1包为`Diagnostics/FootPlacementRuns/20260901-112222-563-5270427812e2458d8ec0fd886437271e`，输入Record均为`43357ff3cd384e5cba75d2c31175b116`。回放状态在封存前已报告1044输入帧执行完成；Editor日志随后记录B1封存1043表现帧、1个既有Pending丢弃帧、2086脚行、67186几何行和完整9份诊断，紧接着出现`Reloading assemblies after forced synchronous recompile`，因此没有生成新的Proof文件。

A/B的1215列Foot CSV中1191个业务列逐值相同，24个Run／实例／Surface／Path identity列变化且全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列变化且全部一一映射。排除Sample／文件hash、detail／index大小与hash和分析耗时后，`analysis.json`完全相同；排除Sample identity与index hash后，`quality-score.json`完全相同且总分均为84.2。由此把运行数据一致与Proof发布器受外部domain reload中断明确分开；并行Foot源文件已在B1封存后变化，必须等其Owner闭合并重建新A，不能继续叠加下一小步。

## Program Prepared合同归位候选

状态：`ThirdPersonClient.Runtime.csproj`按规定参数静态编译成功，0错误；27个警告均来自既有包或既有Input Value未使用字段，build server已关闭。后续`ThirdPersonClient.Editor.csproj`也以规定参数编译成功，0错误，build server已关闭。Foot Owner释放Unity后，已在其它Unity源码写入冻结窗口内重新完成同状态隔离A/B；HEAD合同状态已恢复，Replay Proof、Foot CSV、Ground Path Geometry和全部诊断报告均已对账，候选验证完成。

- 将跨Owner使用的`CharacterPoseProgramPrepared`从`PosePlanExecutionRuntime.cs`移入统一`CharacterPoseFrameContracts.cs`，与Source Demand、Source Frame、Program Result、Constraint Result和Publication Result使用同一合同目录及`ThirdPersonCharacter.Pipeline.Animation`命名空间。
- Runtime文件中的旧定义直接删除；全仓搜索只保留一个正式Prepared合同，不保留别名、转发类型或兼容namespace。现有Program-owned Pending Page继续消费同一typed合同，字段、构造校验和调用顺序均未改变。
- 本步只修正抽象与实现的物理归属，不改变Source采样、Native Workspace、Executor、Constraint、Foot／Pelvis／Goal／FBBIK、Writer或Seal／Discard生命周期。任务2.2仍因per-operation completion未建立而保持未完成。

固定Record仍为`43357ff3cd384e5cba75d2c31175b116`。A通过临时反向应用`4a570788`恢复提交前合同位置后执行，包为`Diagnostics/FootPlacementRuns/20260901-122052-732-2960e8c0f0c04f75980af233629242f3`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-122151-505-6f2fb5d9e32f4949bb8d32148a1293da.json`。A完整封存1043表现帧、2086脚行和67186几何行；相对旧Proof只报告Program／Projection七个aggregate identity字段变化，`DivergentFrameCount=0`、`FirstDivergentRelativeFrame=-1`、`FirstFrameFields=[]`，因此它是当前Foot／Network身份更新后的正式提交前基线。

A完成后已原样恢复HEAD合同位置，全仓仍只有`CharacterPoseFrameContracts.cs`中的一份`CharacterPoseProgramPrepared`定义，Runtime文件只保留其它任务未提交的Performance Marker差异。由于旧A完成后Foot与Network窗口重建过正式产品和诊断基线，本步没有把跨源码状态的旧A硬接到B，而是在二者明确冻结Unity写入后重新建立同状态隔离对：A临时只反向移动`4a570788`的合同定义，包为`Diagnostics/FootPlacementRuns/20260901-125139-634-d1ccc86b44b2482f984ddc88b0b91c00`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-125245-959-c0a9359062294e55b51318c6a0aa4516.json`；B恢复HEAD合同位置后执行，包为`Diagnostics/FootPlacementRuns/20260901-125338-003-e15cbcfa576045d68a62b71b5095bb84`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-125437-102-198c4580f3a84dea9600ef3e848ce9bc.json`。B Proof对A报告`matched=true`、`compared_frame_count=1044`、`aggregate_mismatches=[]`、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空`first_frame_mismatches`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同，24个Run／实例／Surface／Path identity列全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列全部一一映射，没有其它差异。`analysis.json`、`quality-score.json`及八份规则报告在排除Sample／Surface identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认合同物理归位没有改变Source采样、动画时钟、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；当前B可作为下一项Operation Completion迁移的正式A基线。

## Typed Operation Completion页

状态：提交`f70ad67de`已将旧`NativeArray<ulong> FrameCacheCompletedAt`原子替换为typed Operation Completion entry/page。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误、0警告，build server已关闭；固定Trace回放、Foot诊断与Replay Proof均完成，任务2.2的全部typed合同已建立。

- 新`CharacterPoseOperationCompletion`同时保存Completion identity与`Completed / Skipped / TypedInvalid` Outcome；默认值只表示尚未完成。`CharacterPoseOperationCompletionPage`唯一接受首次合法完成，第二次写同一Operation返回失败且不覆盖第一次结果。
- `AnimationPoseNativeWorkspace`的Committed/Pending页不再分配或暴露裸`ulong`完成数组，而是分配固定Operation Count的typed entry；Binding只暴露typed page。Stage完成与最终Program完成仍保持各自现行合同，本步不提前迁移5.5的完整Program Frame Pages。
- Staged Executor在执行Operation前先拒绝已有completion；重复执行会记录`PoseGraphOperationInvalid`与对应Operation index，使整帧Invalid并阻止正常Final Publication。正常Operation按原执行顺序写`Completed`，非活动Linked Pose分支写`Skipped`，原有typed失败写`TypedInvalid`；Preview同样填充typed completion，不再批量伪写裸identity。
- Diagnostics只从typed completion读取既有Completion identity和完成匹配结果，未获得Outcome写权限，也不把Outcome送回任何Runtime决策；现有Snapshot、Pose Watch、Sampler、Analyzer和评分字段语义保持不变。

A为上一步HEAD合同状态的`Diagnostics/FootPlacementRuns/20260901-125338-003-e15cbcfa576045d68a62b71b5095bb84`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-125437-102-198c4580f3a84dea9600ef3e848ce9bc.json`。B为typed Completion状态的`Diagnostics/FootPlacementRuns/20260901-130336-194-9e188a814d0b4271a5eef0b9baf04778`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-130437-539-13dbb6d69362418ab8f045f5ea139e7f.json`。B Proof对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同，24个Run／实例／Surface／Path identity列全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列全部一一映射，没有其它差异。十份诊断报告在排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认typed完成页没有改变动画时钟、Operation顺序、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；该B成为下一项Owned Pending页／typed lease迁移的正式A基线。

## Program Owned Pending Lineage Lease候选

状态：提交`95c5e3644`已将Program Frame Lease从实现文件中的裸Frame identity提升为统一合同目录中的完整open lineage。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。固定Trace回放、Foot诊断与Replay Proof均完成。本步只完成Program lease，Source、Constraint与Final Publication的Owned Pending页／lease仍待后续，因此任务2.4保持未完成。

- `CharacterPoseProgramFrameLease`现在绑定Actor、Frame、Presentation Frame、Body Tick、Program、Pose Program、Projection、Rig和Tuning Generation，只允许Completion identity尚未分配的open lineage；合同不再定义在`PosePlanExecutionRuntime.cs`实现内部。
- 根Runtime先建立一次open lineage，再把同一个lineage同时交给Program `BeginPendingFrame`与根`CharacterPoseFrameTransaction.Begin`。Program Runtime保存该typed lease，后续Seal／Discard／Mutation必须与活动lease完整lineage相同，不再只比较Frame number。
- Source准备完成后根Transaction仍按现行顺序补入Completion identity；`PoseLease.Matches`只把这一项归零后比较其余完整lineage，因此不会建立第二身份或改变Completion生成时机。Program Pending内部Workspace、Source Backend、Constraint与Final Publisher本步均未搬移。

A为typed Operation Completion状态的`Diagnostics/FootPlacementRuns/20260901-130336-194-9e188a814d0b4271a5eef0b9baf04778`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-130437-539-13dbb6d69362418ab8f045f5ea139e7f.json`。B为Program lineage lease状态的`Diagnostics/FootPlacementRuns/20260901-131316-839-a9253d4b1afe4d07874492e537b81e6e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-131413-452-22929101ca0648f394536dce3633b66a.json`。B Proof对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同，24个运行identity列全部一一映射；27列Geometry中22个业务列逐值相同，5个identity列全部一一映射，没有其它差异。十份诊断报告在排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认完整Program lease没有改变Source准备、动画时钟、Operation、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；该B成为Source Owned Pending lease迁移的正式A基线。

## Source Owned Pending页与Lineage Lease候选

状态：提交`4cb9c072a`已由实际`AnimancerPoseSamplingBackend` Source Owner独占Source Pending页及其typed lease。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有Unity包、第三方包或既有Input字段，build server已关闭。固定Trace的直接A/B、完整Foot／Geometry／诊断对账和后续内建Replay Proof均已闭合。本步只完成Source lease，Constraint与Final Publication的Owned Pending页／lease仍待后续，因此任务2.4保持未完成。

- 新`CharacterPoseSourceFrameLease`绑定与Program lease相同的完整open lineage。根`CharacterPoseFrameTransaction`单独保存Program Lease与Source Lease，并在Source补入Completion identity后仍按除Completion以外的完整lineage验证Demand、Result、Seal和Discard。
- `AnimancerPoseSamplingBackend`内部唯一`CharacterPoseSourcePendingPage`保存当前lease、唯一Demand和唯一Source Frame Result；Begin、Demand、Result、Validate、Evaluate Barrier、Commit与Discard全部要求同一个typed lease。旧`PosePlanExecutionRuntime.m_PendingSourceDemand`已删除，不保留镜像字段或兼容路径。
- 根Runtime只持有Source Lease和只读Demand／Result，Seal／Discard时把lease交回Source Owner；Program Runtime只生成Demand并消费Source Result，不取得Pending页。Animancer资源准备、Source-local时间、Clip／Blend Space／Action sample、deferred release和Playable调用顺序均未修改。

正式A为Program lineage lease状态的`Diagnostics/FootPlacementRuns/20260901-131316-839-a9253d4b1afe4d07874492e537b81e6e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-131413-452-22929101ca0648f394536dce3633b66a.json`。第一次候选包`Diagnostics/FootPlacementRuns/20260901-140248-359-3ca202b622c445c997d26d9fae98149b`在Body Tick 367→368期间额外采样两个Presentation Frame，Proof `20260901-140353-434-056e6c2e2069471dab7066432fb675aa.json`只报告`sampling_relative_frame_count: 1043→1045`且`divergent_frame_count=0`；异常前366个表现帧的1191个业务列逐值相同。该包保留为Editor表现调度反例，不作为代码A/B成功证据。

同状态补跑B2为`Diagnostics/FootPlacementRuns/20260901-140725-120-fb6a3adfe8284cddbbaeed32fc97b59a`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-140831-534-f31eba0e0aca44298f216a8860849200.json`。B2直接对正式A的schema、Trace、Runtime、Start Body、Tick／Presentation Clock、1044个frames、Input hash、Body hash和`sampling_relative_frame_count=1043`全部相同。两包均为2086脚行、1215列，其中1191个业务列逐值相同、24个运行identity列一一映射；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项和总分84.2不变。

为让工具自己的链式基线也闭合，最终B3为`Diagnostics/FootPlacementRuns/20260901-141357-584-1f1b11082a7245f9b9c31dd07e123429`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-141501-810-0e4583d4cb2843ec9a8ee50189c671a1.json`；它对B2正式报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。由此确认Source Pending页所有权收口没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、FBBIK、Physical Pose或诊断业务事实；B3成为Constraint Owned Pending lease迁移的正式A基线。

## Constraint双Bank Lineage Lease候选

状态：提交`0fb3cb432`已让现有`CharacterPoseConstraintRuntime`双Bank绑定完整typed Constraint lease，并由根事务持有其Seal／Discard权限。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有Unity包、第三方包或既有Input字段，build server已关闭。固定Trace、Foot、Geometry与诊断对账全部完成。本步不移动Foot／Goal／FBBIK内部页，也不提前拆Final Publication；任务2.4仍只差Final Publication lease。

- 新`CharacterPoseConstraintFrameLease`绑定完整open lineage。Constraint `BeginFrame`一次创建lease并把它写入唯一Pending Bank；旧Bank中的Frame identity、Render Frame、Rig Id和Rig Revision重复字段已删除，所有帧／Rig判断只读lease lineage。
- 根`CharacterPoseFrameTransaction`单独保存Constraint Lease。Program在Begin、Complete、Barrier后Fault、Seal和Discard时必须交回同一个lease；Constraint Result补入Completion identity后仍按除Completion外的完整lineage匹配，不建立第二完成身份。
- 双Bank选择、Foot Placement Bank、Pelvis、Goal Contribution、Goal Set、BendHistory、FBBIK Solver、Physical Writer调用和诊断发布顺序均保持原实现；本步只收紧Bank外部生命周期与身份，不改变任何IK公式、参数、准入或数值顺序。

A为Source lease最终状态的`Diagnostics/FootPlacementRuns/20260901-141357-584-1f1b11082a7245f9b9c31dd07e123429`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-141501-810-0e4583d4cb2843ec9a8ee50189c671a1.json`。B为Constraint lease状态的`Diagnostics/FootPlacementRuns/20260901-142115-026-3d661384834a42669887b2d1a51022b6`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-142225-539-a238d21b26944e57875927593ad9886f.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异和`divergent_frame_count=0`。

A/B均封存1043表现帧、2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同、24个运行identity列一一映射；27列Geometry中22个业务列逐值相同、5个identity列一一映射，没有其它差异。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项和总分84.2不变。由此确认Constraint Bank lineage收口没有改变动画时钟、Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Physical Pose或诊断业务事实；B成为Final Publication lease迁移的正式A基线。

## Final Publication Owned Pending页与Lineage Lease

状态：提交`20dac2d15`已让现有最终姿势发布器独占正式`CharacterFinalPosePublicationPendingPage`并绑定完整typed Publication lease；提交`94a8c001e`修正了第一次Unity运行暴露的大结构自动属性setter问题。修正后`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告均来自既有Unity包、第三方包或既有Input字段，build server已关闭。Unity重新加载后固定Trace、Foot、Geometry、十份诊断报告与Replay Proof全部闭合，Console 0错误。Program、Source、Constraint与Final Publication四个现行Owner现均拥有独立Pending页和typed lease，任务2.4完成。

- 新`CharacterFinalPosePublicationFrameLease`绑定与其它三个Module相同的完整open lineage。根`CharacterPoseFrameTransaction`单独保存Publication Lease，并在Begin、Evaluate Barrier、Seal前验证、成功发布、Barrier前Discard与Barrier后Fault清理中始终交回同一lease。
- `ComposedAnimationPoseFramePublisher`内部唯一`CharacterFinalPosePublicationPendingPage`保存lease、选中的双Buffer页、Publication Result和只读Composed Frame；旧的Pending Page index、Completion identity与Frame三个可独立更新字段已删除。Publication Result不再由外层`PosePlanExecutionRuntime`拼装，而由该Pending页Owner按同一完成lineage产出。
- 成功路径仍保持原顺序：Constraint Complete、Physical Writer、Workspace确认Write Outcome、Final Publication冻结Pending结果、根Seal、deferred source release、Publication页提升。失败路径只清理匹配同一lease的Pending页，不改变Committed。Foot、Pelvis、Goal、FBBIK、Physical Writer公式与调用顺序均未修改。
- 本步只完成现行Final Publication的Owned Pending生命周期，不声称已完成第7章：`AnimationFinalPosePhysicalWriter`仍由Constraint外层持有，唯一Final Pose Native物理页仍在`AnimationPoseNativeWorkspace`。后续第7章会把Writer与物理页整体迁入具体`CharacterFinalPosePublication`，不会保留当前分裂归属或建立第二Writer／第二Final Pose页。

第一次候选回放包为`Diagnostics/FootPlacementRuns/20260901-144934-470-9e0237954789465e840ccfebfea044d9`。它在正式采样前的Pose Branch Reset进入`ComposedAnimationPoseFramePublisher.Invalidate`时抛出`InvalidProgramException: Passing an argument of size '10048'`，Accepted Frames为0；Unity `Editor.log`保留了Sample identity、调用栈和失败位置。原因是初版Pending页把约10KB的`ComposedAnimationPoseFrame`保存为自动属性，`Frame = default`生成了Mono不接受的大按值setter参数；离线Roslyn编译能够通过，但Unity Mono执行失败。`94a8c001e`把该Pending页全部存储改为直接字段，删除大结构setter；失败候选不作为A/B成功证据，也没有修改IK输入或输出。

正式A为Constraint lease状态的`Diagnostics/FootPlacementRuns/20260901-142115-026-3d661384834a42669887b2d1a51022b6`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-142225-539-a238d21b26944e57875927593ad9886f.json`。修正后的B为`Diagnostics/FootPlacementRuns/20260901-145615-867-82e9a3802a6a4b2ba49e3b70c64c75d4`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-145714-058-456b2612adfd4f90ae75c8b1bd02992b.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空首帧差异，二者`sampling_relative_frame_count`均为1043。

A/B均封存2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射、0个其它差异；27列Geometry中22个业务列逐值相同、5个identity列一一映射、0个其它差异。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认Publication Pending所有权和lease收口没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、Assembler、Bend、FBBIK或Physical Pose业务事实；B成为下一项外层重构的正式A基线。

## Pose帧合同校验责任分层

状态：提交`0c7e51c6c`已收敛任务2阶段新增的重复静态身份检查与多层Result完整校验。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。固定Trace、Foot、Geometry、十份诊断报告与Replay Proof全部通过，Unity Console 0错误，任务2.7完成。

- Character Build已有的typed拓扑、静态写冲突、Operation／Value／Workspace布局与schema证明保持原Owner；本步没有在Runtime、Module或Executor新增Graph／Program静态扫描，也不提前处理后续Compiler Pass任务。
- Runtime创建继续由`CharacterPresentationRuntimeFactory`与`PosePlanExecutionRuntime`一次验证Projection、Rig、资源绑定和固定容量。`CharacterPoseConstraintRuntime.BeginFrame`不再每帧把Rig字符串转换为`FixedString`后重复核对；Final Publisher也不再每帧重复比较Pose Program、Rig与Rig Revision。它们只接收由当前Runtime装配链生成的typed lease。
- Program、Source、Constraint与Publication四种lease只在构造时验证完整open lineage并冻结合法性；后续`IsValid`不再重新扫描Actor、Program、Projection、Rig与Tuning字符串，`Matches`只比较同一完整lineage并忽略尚未分配／已经分配的Completion项。根事务删除`IsValid + Matches`双重调用，只保留当前lease匹配、阶段和Completion交接检查。
- Program Result、Constraint Result、Publication Result与组合Execution Result在Owner构造结果时冻结一次合同合法性；根`CharacterPoseFrameTransaction`后续只读取冻结结果、同lineage和Published Outcome，不在Owner、组合Result与根Seal三层重新计算同一完整合法性。
- Source readiness、动态容量、release闭包、Goal／FBBIK闭包、Operation completion、Final Pose availability、continuity、Write Outcome和Physical binding仍由拥有当前动态输入的边界检查。全仓仍只有一处`new AnimationFinalPosePhysicalWriter`分配；Writer在Evaluate前验证全部Transform binding并保持原Fault政策。本步没有删除任何算法必要动态检查，也没有改变Writer、Foot、Goal或FBBIK执行顺序。

正式A为Final Publication lease状态的`Diagnostics/FootPlacementRuns/20260901-145615-867-82e9a3802a6a4b2ba49e3b70c64c75d4`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-145714-058-456b2612adfd4f90ae75c8b1bd02992b.json`。B为校验责任分层状态的`Diagnostics/FootPlacementRuns/20260901-153824-053-e3bd86ceb07d4741aa12ca1a856e4fae`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-153939-043-da8cbb1602604e0c83b6544619be66c2.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空首帧差异，二者`sampling_relative_frame_count`均为1043。

A/B之间主线另有提交`7534b6bf0`把GM／NetworkTest Editor工具合同迁入仓库级本地UPM包；该提交不包含Pose Runtime、产品Program或诊断语义修改。B Proof中的Program、Projection、Source Revision、Semantic／Contract、World、Trace、Start Body、Tick／Presentation Clock、Input和Body identity均与A相同，Foot与Geometry也没有出现第三类差异，因此该Editor-only目录迁移未污染本次Pose行为结论。

A/B均封存2086脚行和67186几何行。1215列Foot CSV中1191个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射、0个其它差异；27列Geometry中22个业务列逐值相同、5个identity列一一映射、0个其它差异。十份诊断报告排除运行identity、文件hash、detail／index大小与分析耗时后全部相同，七维分项与总分84.2不变。由此确认校验责任分层没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Physical Pose或原Fault外的正常业务事实；B成为Constraint外层边界迁移的正式A基线。

## Constraint根Bank生命周期外层归属

状态：提交`6045f40f0`已把现有`CharacterPoseConstraintRuntime`与其双Bank生命周期所有权从旧`PosePlanExecutionRuntime`提升到唯一帧协调根`CharacterAnimationPresentationRuntime`。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0警告、0错误，build server已关闭。固定Trace、Foot、Geometry、十份诊断报告、Replay Proof、退出Play后的Dispose和Unity Console均通过，任务3.1完成。

- `CharacterAnimationPresentationRuntime`现在保存唯一`m_PoseConstraints`引用并负责最终Dispose；`PosePlanExecutionRuntime`只借用同一实例执行现有Constraint调用，不再在自身Dispose中销毁Bank。根Runtime构造失败时按`Pose Runtime -> Constraint`顺序清理，正常销毁也先结束Pose Runtime使用，再销毁Constraint与Foot内部资源。
- 全仓仍只有`PosePlanExecutionRuntime`构造链中的一处`new CharacterPoseConstraintRuntime`，没有第二Factory、wrapper、可选实现或兼容路径。构造成功后同一实例的生命周期所有权立即交给帧协调根；Program执行侧和根Owner没有复制Bank、Foot页、Goal页、BendHistory或Solver状态。
- `CharacterPoseConstraintRuntime`内部Bank类型、双页选择、Foot Placement Bank、Pelvis、Goal Contribution、Goal Set、BendHistory、FBBIK Solver与Diagnostics布局均未修改；Foot Module仍只随Constraint Owner销毁一次。Physical Writer暂时仍由Constraint持有，按任务7整体迁入Final Publication，本步不建立中间Writer Owner。
- Program执行侧仍通过现有入口调用同一Constraint实例；typed编译Handle、per-operation Result以及NativeSlice／offset／Operation字段收窄属于任务3.3至3.6，本步不借生命周期迁移提前改调用公式或数据布局，因此任务3.2及后续任务不提前勾选。

正式A为校验责任分层状态的`Diagnostics/FootPlacementRuns/20260901-153824-053-e3bd86ceb07d4741aa12ca1a856e4fae`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-153939-043-da8cbb1602604e0c83b6544619be66c2.json`。B为Constraint外层生命周期Owner状态的`Diagnostics/FootPlacementRuns/20260901-155530-797-513d233becd14a5c8ab066a33eecf972`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-155637-385-7ce395f427fa4b9c8b851cbd466cffa5.json`；B对A报告`matched=true`、`compared_frame_count=1044`、空aggregate/frame差异、`divergent_frame_count=0`、`first_divergent_relative_frame=-1`和空首帧差异，二者`sampling_relative_frame_count`均为1043。退出Play触发新Owner Dispose链后Console仍为0错误，确认没有双Dispose、漏Dispose或悬空Bank访问。

A/B之间性能任务把其独立IPC源码从Named Pipe迁向Loopback TCP，但没有修改Pose Program、Constraint、Foot／IK、产品Program或诊断字段；B Proof中的Runtime identity、Trace、Start Body、Tick／Presentation Clock、Input与Body identity全部与A相同。A/B均封存2086脚行和67186几何行：1215列Foot CSV中1191个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射、0个其它差异；27列Geometry中22个业务列逐值相同、5个identity列一一映射、0个其它差异。十份诊断报告归一化后全部相同，七维分项与总分84.2不变。由此确认Constraint生命周期Owner提升没有改变动画时钟、Source采样、Operation、Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Physical Pose或正常销毁行为；B成为Constraint typed入口收窄的正式A基线。

## Foot Placement typed Handle候选与并行Foot行为混入

状态：提交`39180583d`已把Foot Placement的Operation、Callsite、Contribution Value和Goal Workspace地址固化为`CharacterFootPlacementConstraintHandle`，并让Constraint入口返回`CharacterFootPlacementConstraintOperationResult`。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。本步尚未形成可接受的行为A/B，不勾任务3.3，也不把混合候选当成下一步基线。

- Native Program只在编译Foot Operation时构造有效Handle；默认struct通过独立有效位保持无效，避免索引默认值0把非Foot Operation误判为Foot。`PosePlanExecutionRuntime`与World-aware Stage Input不再分别传递Operation Index和Goal Offset。
- `CharacterPoseConstraintRuntime`现在以Handle执行Ready或World Context Unavailable两条现有Foot路径，并由typed Result冻结Producer、Callsite、Goal Offset、Availability、Frame和Completion匹配。Foot内部Evaluate顺序、公式、参数、历史页、Goal编码、Assembler、FBBIK与Physical Writer均未修改。
- 第一次候选为`Diagnostics/FootPlacementRuns/20260901-161932-208-257452ac7fdc4ac4a97f8d62ccc3651e`。工具返回的Proof路径为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-162046-483-e365cd3d1559423a914ad43ef86f3bff.json`；它相对`155530`报告1044个Replay Frame无分歧、Input Sequence与Body Trajectory Hash相同、1043个表现采样帧相同，但Program／Projection的7个aggregate identity变化。外部重启Unity后该Temp Proof已不存在，因此本记录只保留工具已经返回的失败身份与持久run，不能把路径引用当成仍存在的Proof文件。
- 两包均有2086脚行；候选Foot CSV因并行诊断修改从1215列增加为1222列，新增7个`PelvisSameLevel`列。1215个同名列中1110列逐值相同、105列变化；其中24个既有运行／实例／Surface／Path identity列一一映射，另有Program、Projection、Pose Plan与Foot Profile身份随产品重建变化，其余差异集中在Stride、Pelvis、FBBIK和Physical结果。Geometry仍为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射。
- 差异归属已由Foot Owner确认：并行提交`68dde5c0c`在同一工作区加入“同平面双脚阶段限制骨盆世界下降”行为、7个正式诊断字段、Profile变化和产品重建；该提交的父提交正是`39180583d`，且候选采样发生在Foot提交前但源码已在工作区。因此`161932`同时包含PoseGraph Handle与Foot行为，不能证明或否定单独的PoseGraph行为等价，必须从本change的通过证据中排除。

恢复结果：Foot与Performance Owner暂停源码、产品、Git和Unity写入后，正式A为`Diagnostics/FootPlacementRuns/20260901-165648-064-ed90c98bff504e6eb99e47c30e164195`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-165746-842-f6cb9db6199f4956ac36df4dac7e58e1.json`；同状态B为`Diagnostics/FootPlacementRuns/20260901-165834-116-4f8dec5c15b8469c98f6498774091a2b`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-165933-491-4c8b5c15e2db400eb5ad7f53d10c72a1.json`。B对A正式报告`matched=true`、`compared_frame_count=1044`，两包均封存1043表现采样帧、2086脚行和67186几何行。

A/B的1222列Foot CSV中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列全部一一映射且无冲突；27列Geometry中22个业务列逐值相同、5个identity列全部一一映射。十份正式报告在排除Sample／文件hash、detail／index大小与hash和分析耗时，并按已证明的一一identity映射归一化后全部相同；退出Play后的Console为0错误。该A/B只证明`68dde5c0c + 39180583d`当前组合状态稳定，并为后续PoseGraph小步建立同schema、同Program／Projection身份的新A；它不把`161932`改写为PoseGraph单步通过证据，也不替代`155530`作为上一份已通过的PoseGraph基线和总基线对照。

后续Foot任务独立提交`943641f8c`并以`707b0df15`记录双脚落地后的骨盆共同高度实验；Performance任务随后完成Player Build、Smoke、1044 Tick零丢帧Replay和正式Capture，并删除5个已过期、会在Domain Reload误触发PrepareReplay的EditorPrefs Pending状态。两项任务均封口并释放后，Foot正式A为`Diagnostics/FootPlacementRuns/20260901-173426-115-81a3c9d1689c48159cbb5a0ff14d8e8d`，其Proof副本保存在run根。当前最终工作区重新建立的A为`Diagnostics/FootPlacementRuns/20260901-183922-499-1330f06a34634d34b614d6909519f38b`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-184022-060-351f7af3851f4f5b83f489a0aa07d80d.json`；同状态B为`Diagnostics/FootPlacementRuns/20260901-184047-794-d3be6ac77c254b32a23077ef35ca71cb`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-184146-483-295b0755a82644c9bf365d4421b7a12d.json`，B正式匹配A的1044帧。

`173426 -> 183922`与`183922 -> 184047`两段均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射。两段十份报告归一化后均为0差异，退出Play后Console为0错误。由此确认Performance最终工具改动、EditorPrefs陈旧Pending清理和当前重启状态没有继续改变Foot／Pelvis／Goal／FBBIK／Physical业务；`184047`成为PoseBone Contribution typed入口迁移的正式A。`943641f8c`的明确Foot业务变化仍按其独立实验归属，不伪装为PoseGraph无行为变化。

## PoseBone Contribution typed Handle

状态：提交`79a53af1e`已为PoseBone Contribution建立独立编译Handle、描述符Catalog和per-operation Result。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已关闭。Unity完成Domain Reload后Console为0错误，同输入A/B通过；任务3.3仍需等待Goal Assembler和FBBIK两个Family完成后整体勾选。

- Native Program只在`PoseBoneIKGoals` Operation编译时固化Input Pose Value、Contribution Value、Goal Workspace和Descriptor范围；旧`AnimationPoseGraphNativePoseBoneIkGoalRange`页及Operation上的`PoseBoneIkGoalsIndex`已经从Native执行记录删除。Program继续唯一持有同一份Descriptor NativeArray，并只把typed Catalog交给Constraint Runtime。
- Staged Executor现在只检查Handle和Component Pose是否可用，再在原Stage位置调用一次`ExecutePoseBoneContribution`；它不再读取Descriptor数组、构造Descriptor Slice、解释Goal Offset或检查Constraint Bank容量。Constraint Runtime是唯一Handle解析者，仍调用原`CharacterPoseBoneIkGoalSource.BuildGoals`并写入原Contribution槽位；typed Result冻结Producer、Callsite、Goal范围、Frame和Completion匹配。
- PoseBone Goal公式、描述符顺序、Goal编码、Assembler输入、FBBIK、Physical Writer、Operation Stage和数值顺序均未改。Handle内部不嵌套`NativeSlice`，避免把Native Container嵌入存放Operation的NativeArray；没有新增兼容入口、fallback或第二Descriptor Owner。

正式A为`Diagnostics/FootPlacementRuns/20260901-184047-794-d3be6ac77c254b32a23077ef35ca71cb`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-184146-483-295b0755a82644c9bf365d4421b7a12d.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-185755-638-046be3a0a7474c458df6df3e59202359`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-185856-535-aa40943181f94445a406f230c2117421.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列全部一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列全部一一映射且0冲突。十份正式报告在排除Sample／文件hash、detail／index大小与hash和分析耗时，并按已证明的Surface identity映射归一化后全部相同。退出Play后的Console为0错误；editor-state最后一份快照为`is_playing=false`、Idle，但之后遥测标记`stale_status`，因此不把`ready_for_tools`作为本步通过证据。由此确认当前固定Trace覆盖的动画时钟、Foot、Pelvis、PoseBone Goal、Assembler、FBBIK和Physical结果未因PoseBone typed入口迁移改变；`185755`成为Goal Assembler typed入口迁移的正式A。

## Goal Assembler typed Handle

状态：提交`ea2ece645`已为Full Body IK Goal Assembler建立独立编译Handle、Contribution输入Catalog和per-operation Result。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。Unity Refresh完成Domain Reload并返回ready，Console为0错误；任务3.3还剩FBBIK Family，暂不整体勾选。

- Native Operation中的Goal Set输出Value和Contribution输入range现在由`CharacterFullBodyIkGoalAssemblerConstraintHandle`承载；非Assembler Operation保持无效Handle。Program仍唯一持有Contribution Value索引数组，只把验证过Goal Set容量的typed Catalog交给Constraint Runtime。
- Staged Executor删除了Contribution索引NativeArray字段、range校验和NativeSlice构造，只在原Operation位置传Handle并验证typed Result。Constraint Runtime是唯一Catalog解析者，并继续按原输入顺序调用同一个`CharacterFullBodyIkGoalAssembler.Assemble`；Goal冲突规则、Goal排序、Goal Set写入、Producer身份和Frame／Completion没有改。
- 失败仍沿原`CharacterFullBodyIkResult`返回并使当前Operation失败；没有吞错、默认Goal、兼容入口或第二Assembler路径。Handle不嵌套Native Container，Catalog只借用Program拥有的只读数组。

正式A为`Diagnostics/FootPlacementRuns/20260901-185755-638-046be3a0a7474c458df6df3e59202359`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-185856-535-aa40943181f94445a406f230c2117421.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-191034-951-23c75316aae14fa5849ef2aa6179f84e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-191131-417-22e056a627a245688aea35a1664481d6.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列全部一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列全部一一映射且0冲突。十份正式报告按既定Sample／文件hash、detail／index大小与hash、分析耗时和Surface identity规则归一化后0差异；退出Play后的Console为0错误。由此确认当前固定Trace覆盖的Contribution输入顺序、Goal Set、Foot、Pelvis、PoseBone Goal、FBBIK和Physical结果未因Goal Assembler typed入口迁移改变；`191034`成为FBBIK typed入口迁移的正式A。

## FBBIK typed Handle与Constraint Family合同闭合

状态：提交`2f9642805`已为FBBIK建立独立编译Handle和per-operation Result。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。Unity Refresh完成Domain Reload并返回ready，Console为0错误；Foot Placement、PoseBone Contribution、Goal Assembler和FBBIK四个Family现在都具备typed编译Handle与per-operation Result，任务3.3完成。

- `CharacterFullBodyIkConstraintHandle`把Solver、输入／输出Pose Value、输入Goal Set Value、Operation和Callsite固定为同一编译合同；Native Operation不再保存独立FBBIK索引，既有Diagnostics只通过Handle派生只读身份。
- Pose workspace仍由Program执行侧拥有：Staged Executor按原顺序验证输入并把Input Pose复制到Output Pose，只把Output Component Pose写View和Handle交给`ExecuteFullBodyIk`。Constraint Runtime继续调用原`SolvePrepared`，并保持Goal Set、BendHistory、Solver Outcome、Effector／Limb诊断采集及失败返回顺序不变。
- typed Result冻结Handle、Solve结果、Frame和Completion；Solver失败仍把同一Output Value标记为`FullBodyIkSolverInvalid`。没有复制Solver、默认Goal、兼容入口、fallback或第二Pose写入路径。

正式A为`Diagnostics/FootPlacementRuns/20260901-191034-951-23c75316aae14fa5849ef2aa6179f84e`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-191131-417-22e056a627a245688aea35a1664481d6.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-192117-923-ad1dd64ee4e24e4ea8d0576f406fb253`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-192220-365-11c3b45ab4f54c568b06188339f5727f.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列全部一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列全部一一映射且0冲突。十份正式报告按既定规则归一化后0差异；退出Play后的Console为0错误。由此确认当前固定Trace覆盖的Goal Set消费、Solver、BendHistory、Foot、Pelvis、PoseBone Goal、FBBIK和Physical结果未因FBBIK typed入口迁移改变；`192117`成为后续Constraint Operation completion收口的正式A。该次提交只据此完成任务3.3，任务3.4至3.6留待独立代码对账。

## Constraint Operation调用与Complete闭包对账

四个Family完成typed入口后重新全仓核对调用点：PoseBone、Goal Assembler和FBBIK各只有Staged Executor原Operation分支中的一次调用；Foot同一Operation分支按World Context在`EvaluateFootPlacement`与`RecordUnavailableFootPlacement`之间互斥选择一次，不存在外层预执行、Constraint扫描Program、Diagnostics重放或第二Stage调度。`ExecuteStage`在调用前要求对应`CharacterPoseOperationCompletionPage`槽为空，调用后统一通过`TryCompleteOperation`写入一次`Completed`或`TypedInvalid`；`TryComplete`拒绝非空槽并记录重复Operation，因此任务3.4完成。

`CharacterPoseConstraintRuntime.CompleteFrame`只绑定同一Completion，核对Pending lease／lineage、Goal Set闭包、Solver Outcome和Foot pending frame，再构造唯一`CharacterPoseConstraintResult`。它不读取Operation数组、不维护Stage Schedule、不调用四个Execute入口、不重新运行Goal Assembler或Solver；任务3.5完成。连续三段`184047 -> 185755 -> 191034 -> 192117`固定Trace又证明内部Foot Placement、Pelvis、PoseBone Goal、Contribution、Assembler、Goal Set、FBBIK和BendHistory业务结果保持，因此任务3.2完成。任务3.6仍有调用方可见的Handle内部地址和Constraint诊断页访问，需要下一步实际收窄，不能由本次代码审计代替。

## Constraint Result与Pending Bank读取收窄

状态：提交`02ccb1b4d`已让四个per-operation Result把Handle、Contribution Header、Goal Set和Solve明细改为私有，只向Program执行侧提供身份匹配；Foot额外只公开业务Availability。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。

- Staged Executor不再读取Foot Handle中的Foot Descriptor／Contribution Value容量，也不再从Result读取Goal offset/count或回读Constraint Pending Goal Contribution。Ready与World Context Unavailable现在由同一次Foot typed Result直接映射到原`None`或`WorldContextUnavailable`结果，Constraint Runtime继续独占Contribution槽和Goal workspace范围校验。
- Executor初始化只通过`MatchesCompiledLayout`核对Program与Constraint容量，不再分别读取Constraint内部两个Bank数组长度；Operation ABI范围继续由Program自身容量验证。旧`GetPendingGoalContribution`入口已删除，没有保留诊断fallback或第二错误来源。

正式A为`Diagnostics/FootPlacementRuns/20260901-192117-923-ad1dd64ee4e24e4ea8d0576f406fb253`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-192220-365-11c3b45ab4f54c568b06188339f5727f.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-193623-780-a483ca530d484c69abd2208ef698c394`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-193724-165-1ddd495782e2425f8be587bdb9c9ba05.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同，24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同，5个identity列一一映射且0冲突；十份正式报告归一化后0差异，退出Play后的Console为0错误。由此确认错误来源收窄没有改变World Context缺失政策、Foot、Pelvis、Goal、FBBIK或Physical业务；`193623`成为下一项Constraint外层可见面收窄的正式A。任务3.6仍未完成，因为Committed Goal／Foot／Solver／Physical Diagnostics页仍由外层与Snapshot Publisher直接读取，必须接入唯一Committed Diagnostics链后再删除，不能在本步伪装完成。

## 根执行Result接入Post-Seal诊断入口

状态：提交`b79b40c05`已让根`CharacterAnimationPresentationRuntime`在成功Seal后把同一`CharacterPoseFrameExecutionResult`直接交给Pose诊断入口。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告均来自既有依赖和未使用字段，build server已关闭。

- `BeginCommittedDiagnostics`现在要求Execution Result已Published，并核对Program／Constraint／Publication lineage、Committed Native页、Final Read和Constraint Bank属于同一Completion。旧入口不能再只凭Native Binding自行认定本帧已提交。
- `AnimationPresentationRuntimeSnapshotPublisher`的Final Summary已从typed Program／Publication Result读取Availability、Invalid Reason、Invalid Operation、PoseGraph Completion和Final Applied Completion。Native Final Read暂时只提供尚未迁移的Continuity、Foot Feature和Pose／Contribution明细，没有建立第二Snapshot或Sampler路径。

正式A为`Diagnostics/FootPlacementRuns/20260901-193623-780-a483ca530d484c69abd2208ef698c394`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-193724-165-1ddd495782e2425f8be587bdb9c9ba05.json`。候选B为`Diagnostics/FootPlacementRuns/20260901-200151-618-05e4e2bbaf264900b9e4916ca41089af`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-200300-335-62d17c8ab95346c380b1459c47dc9e6f.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异，退出Play后的Console为0错误。由此确认Final Summary来源迁移没有改变Foot、Pelvis、Goal、FBBIK、Physical或正式诊断产物；`200151`成为Constraint Committed Diagnostics View迁移的正式A。任务13.1只完成根Execution Result接线，Source／Constraint／Final Publication的完整Committed诊断投影合同仍未闭合，不提前勾选。

## Constraint Result随Bank原子提交

状态：提交`8732331ce`让`CharacterPoseConstraintRuntime.CompleteFrame`产生的typed Result进入Pending状态，并在`SealFrame`与同一Bank一起提升为Committed Result；Discard同时清除Pending Result。Post-Seal诊断改为匹配根Execution中的Constraint Result，不再读取`CommittedBankIdentity`或`CommittedRenderFrame`。

第一次候选run为`Diagnostics/FootPlacementRuns/20260901-202209-561-eb7ae2d705544428bf4459712ab17f08`，在第1个Replay Tick后于Frame Commit失败，Console明确报告`Pose Constraint result is incomplete at seal`。原因是候选把Begin阶段Completion为0的lease lineage与完成后的Result lineage直接全等比较；这违反既有lease合同。失败run原样保留，不作为A/B通过证据。提交`700b29347`改为调用现有`lease.Matches(completedLineage)`，只修正完成身份匹配，不改变Constraint数据或执行顺序；修正后`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，build server已关闭。

正式A为`Diagnostics/FootPlacementRuns/20260901-200151-618-05e4e2bbaf264900b9e4916ca41089af`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-200300-335-62d17c8ab95346c380b1459c47dc9e6f.json`。修正后的候选B为`Diagnostics/FootPlacementRuns/20260901-202513-054-4eb0ef66f0504dfc9a4bddfb0063eadf`，Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-202615-147-3f76faf457604e078d9bb63b24c618da.json`；工具对A正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告排除既定identity、hash、分析耗时和文件大小后0差异；本轮`analysis.performance.reportBytes`增加10，精确来自十个代表事件的Surface实例ID由5位变6位，使`contact-plane-penetration.json`增加10字节，Surface映射后的报告业务内容完全相同。退出Play后的Console为0错误。由此确认Constraint Result原子提交没有改变Foot、Pelvis、Goal、FBBIK、Physical或诊断业务；`202513`成为Constraint详细Committed Diagnostics View迁移的正式A。

## Constraint详细Committed Diagnostics短租约

状态：提交`5876c279a`已建立预分配的`CharacterPoseConstraintCommittedDiagnosticsView`。Constraint Runtime只在当前诊断Interest需要时，把已提交Goal Contribution、Goal Set和FBBIK Solver明细复制进单一Committed Diagnostics页；每次捕获都会更新代际身份，旧View在下一次捕获后失效。`AnimationPresentationRuntimeSnapshotPublisher`不再持有Constraint Runtime，也不再直接调用其Goal／Solver getter，只消费与根`CharacterPoseFrameExecutionResult`同lineage的短租约。运行求解、Goal装配、Bend History、Foot、Physical写入和Final Publication顺序未改；没有第二Snapshot、兼容入口或运行时分配。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0警告、0错误，build server已关闭。Unity Force Refresh与Domain Reload完成，回放前Console为0错误。正式A为`Diagnostics/FootPlacementRuns/20260901-202513-054-4eb0ef66f0504dfc9a4bddfb0063eadf`，候选B为`Diagnostics/FootPlacementRuns/20260901-203902-659-f1c1b305b4c64584b7395173f8fc52d7`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-203958-168-8a501c3144f644039da32345edcb516f.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件hash、detail／index大小与hash、分析耗时和Surface identity规则归一化后0差异。退出Play后的Console为0错误。由此确认Goal Contribution、Goal Set与FBBIK详细诊断的读取边界迁移没有改变Foot、Pelvis、Goal、FBBIK、Physical或正式诊断业务；`203902`成为Foot Committed Diagnostics并入同一View的正式A。任务3.6仍未完成，因为Foot诊断页与Physical写入诊断还暴露在Constraint Runtime外层。

## Foot Committed Diagnostics并入Constraint短租约

状态：提交`f803f6acb`已把`CharacterFootLandingPredictionDiagnostics`并入同一`CharacterPoseConstraintCommittedDiagnosticsView`。Constraint Runtime删除`CommittedFootDiagnostics`、`HasCommittedFootDiagnostics`和空Foot页入口，只在当前Interest确实需要Foot诊断且Committed Bank持有完成值时，把该只读Frame引用复制进同一代际页。Pose执行外层与Snapshot Publisher不再单独持有或传递Foot诊断页；Foot、Goal和Solver明细现在由同一Constraint Result lineage与同一短租约提供。Foot求解、Landing Prediction生成、Debug Registry发布、Physical写入和最终采样内容未改。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告来自既有包和既有未使用字段，build server已关闭。Unity Force Refresh后、回放启动前出现两条既有FinalIK序列化深度日志，均指向`RootMotion.FinalIK::FBIKChain.reachSmoothing`，与本次未修改的FinalIK序列化类型一致；该刷新噪声单独保留，不计入运行时Console。清空后执行固定Trace，正式A为`Diagnostics/FootPlacementRuns/20260901-203902-659-f1c1b305b4c64584b7395173f8fc52d7`，候选B为`Diagnostics/FootPlacementRuns/20260901-204547-204-ce91af9890d84365a04fe6e3478924a3`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-204649-938-ab7905d1aaee442c9397ce89a255e9d3.json`，工具对A Proof正式报告`matched:1044`。

诊断封存末尾的MCP轮询一度路由到另一个Unity实例`pik@78ca1587a25567ad`并返回不支持`character.fixed_input_trace`；重新把活动实例固定为`3C_Client@e852139597e42532`后读取到完成状态、Proof和全部产物，未刷新或中断本项目。A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。回放运行期间与退出Play后的Console均为0错误。由此确认Foot诊断页收口没有改变Foot、Pelvis、Goal、FBBIK、Physical或正式诊断业务；`204547`成为Physical Committed Diagnostics迁移的正式A。任务3.6只剩Physical诊断外层读取尚未收口。

## Physical Writer与Committed Diagnostics归还Final Publication

状态：提交`b58bf055c`已把`AnimationFinalPosePhysicalWriter`的Binding校验、物理骨骼写入、Pending结果、Commit和Committed Diagnostics短租约整体迁入`ComposedAnimationPoseFramePublisher`。Final Publication Pending页要求Published Result与同Completion的Physical Write同时完成，Commit再原子提升Publication Result、Physical Write和最终Pose Frame。Constraint Runtime删除Physical Writer字段、构造依赖、Bank字段、写入入口、Committed getter和Physical Interest判断；它现在只负责Foot／Goal／FBBIK约束闭包。Snapshot Publisher同时消费Constraint与Final Publication两个同lineage短租约，不再从Constraint反推Physical事实。任务3.6和13.4完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；最终增量编译只有1个既有Input Value未使用字段警告，build server已关闭。Unity Force Refresh与进入Play期间共出现一条同类FinalIK序列化深度日志，仍只指向未修改的`RootMotion.FinalIK::FBIKChain.reachSmoothing`；它单独归为Domain Reload序列化问题，没有PoseGraph、Constraint、Final Publication或Physical Writer异常。正式A为`Diagnostics/FootPlacementRuns/20260901-204547-204-ce91af9890d84365a04fe6e3478924a3`，候选B为`Diagnostics/FootPlacementRuns/20260901-205740-295-b4b71ad545274c708ba65c3e3f964c43`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-205842-557-4019941246c8422a806011001837571f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。另一个Unity实例`pik`在活动路由被外部切换后记录的`character.fixed_input_trace`不支持错误不属于`3C_Client`；重新固定目标后，`3C_Client`只有上述一条FinalIK序列化日志，清空后Console为0。由此确认Physical Owner迁移没有改变最终骨骼写入、Foot、Pelvis、Goal、FBBIK或正式诊断业务；`205740`成为下一项Committed Results／Diagnostics Projector收口的正式A。任务13.1仍未完成，因为Source与Program的完整Committed诊断投影合同尚未闭合。

## Final Summary与Detail消费已提交Pose Frame

状态：提交`6616494e2`已让Final Publication Committed Diagnostics短租约同时公开同一Commit原子提升的`ComposedAnimationPoseFrame`。Snapshot Publisher的Final Summary改从该Frame读取Continuity与Foot Features，Final Detail改从该Frame读取参数、可用性、已解析Contribution和dense骨骼权重；旧Native Final Read参数和Final Contribution二次转换已删除。第一次编译发现Slot／Operation／Pose Watch三类尚未迁移的Program Native诊断仍使用原`ConvertContribution`与Workspace Player映射，因此只恢复这三类现有依赖，没有把尚未完成的Program迁移伪装成本步完成，也没有建立第二映射。

`ThirdPersonClient.Runtime.csproj`按规定参数修正后编译成功，0错误、1个既有Input Value未使用字段警告，build server已关闭。Unity调用前均重新固定`3C_Client@e852139597e42532`，没有向`pik`发送本轮命令。正式A为`Diagnostics/FootPlacementRuns/20260901-205740-295-b4b71ad545274c708ba65c3e3f964c43`，候选B为`Diagnostics/FootPlacementRuns/20260901-211231-564-91170212f5ff4e81b74b8f185487655a`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-211331-224-0edf692479ec4354a64d6514b6603bca.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。3C退出Play后只记录同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`序列化深度日志，没有PoseGraph、Final Publication或Physical Writer异常，清空后Console为0。由此确认Final Summary／Detail来源切换没有改变最终Pose、Foot Features、Contribution、Foot、Pelvis、Goal、FBBIK、Physical或正式诊断业务；`211231`成为Program Committed Diagnostics迁移的正式A。任务13.1仍不勾选。

## Source Committed Result与物理身份短租约

状态：提交`f2928d966`已建立`CharacterPoseSourceCommittedResult`和预分配、代际校验的`CharacterPoseSourceCommittedDiagnosticsView`。Physical Source Registry在正式Commit与已准备Release全部完成后，按当前Source Frame lineage深冻结SourceId、Player Node、Owner和Physical generation映射；下一帧开始即使旧View失效。Snapshot Publisher删除`PhysicalPoseSourceRegistry`参数和所有直接Require调用，Slot／Operation／Pose Watch中的Live primitive Contribution只能通过Source View解析。运行时Final Publication仍使用正式Registry，不由诊断View反向驱动，没有第二Registry、默认Source或兼容解析路径。

`ThirdPersonClient.Runtime.csproj`按规定参数修正属性`in`局部值后编译成功，0错误、1个既有Input Value未使用字段警告，build server已关闭。Unity每次调用前均重新固定`3C_Client@e852139597e42532`。正式A为`Diagnostics/FootPlacementRuns/20260901-211231-564-91170212f5ff4e81b74b8f185487655a`，候选B为`Diagnostics/FootPlacementRuns/20260901-212326-424-516539050652496690b232e9e080844e`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-212424-716-58d8d8ef8c584788bd5b42ed5d5377c4.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。Editor.log证明本包先封存1043表现帧与全部诊断并报告`replay completed`，随后自由运行才在Frame 2781／Body Tick 3283的`ActionSampling`抛出`Predicted opposing landing pair is incomplete`；该阶段位于Source View与PoseGraph之前，不属于本步固定1044 Tick回放结果，也不伪装为FinalIK刷新噪声。日志中的Frame 2 Constraint Seal错误明确紧邻旧失败包`20260901-202209-561-eb7ae2d705544428bf4459712ab17f08`，不是本轮。停止后清空3C Console为0。由此确认当前固定Trace覆盖的Source映射、Contribution、Foot、Pelvis、Goal、FBBIK和Physical业务未因Source View迁移改变；`212326`成为Program Committed Diagnostics迁移的正式A。任务13.1仍不勾选，因为Program完整投影尚未闭合。

## Program Committed Result与interest分组冻结页

状态：提交`c9c3d2d1e`已建立预分配、代际校验的`CharacterPoseProgramCommittedDiagnosticsView`。Program Owner始终冻结typed Program Result、Operation Completion和PoseGraph invalid header；只在Live／Capture时冻结Slot primitive Contribution、dense权重与动态StateMachine骨骼权重，只在Operation Detail时冻结Value header、primitive Contribution与dense权重，只在Pose Watch时冻结Value header、primitive Contribution与完整Value Pose。下一Program Frame开始旧View立即失效。PlayerIndex到NodeId不新增静态副本，Projector只在有诊断interest时扫描现有Program Image Operation映射。

Snapshot Publisher已删除`CharacterPoseGraphNativeBinding`、`CharacterPoseGraphNativeProgram`、`AnimationPoseNativeWorkspace`和`PhysicalPoseSourceRegistry`类型引用；Slot／Operation／LinkedPose Completion／Pose Watch全部消费Program View，并用Source View解析Live primitive identity，用Constraint View读取Foot／Goal／FBBIK，用Final Publication View读取Final Pose与Physical。Pose Watch不再读取Workspace、重采样Source、执行World Query、调用FBBIK或反推Physical，因此任务13.1与13.7完成。Publisher仍读取Stack、Route、StateMachine、Inertialization与LinkedPose Actor Runtime快照，任务13.5和13.6暂不勾选。

`ThirdPersonClient.Runtime.csproj`按规定参数修正一次`in frame.Layout`属性局部值后编译成功，0错误、1个既有Input Value未使用字段警告，build server已关闭。Unity每次调用前均重新固定`3C_Client@e852139597e42532`。正式A为`Diagnostics/FootPlacementRuns/20260901-212326-424-516539050652496690b232e9e080844e`，候选B为`Diagnostics/FootPlacementRuns/20260901-214438-507-584a118b94b54a6b925bd47eb128147f`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-214536-991-ea955085cc19405d8d9de586e73ebe7f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后0差异。诊断封存完成后立即停止3C，没有再出现上一包自由运行阶段的opposing landing pair错误；Console只记录同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload日志，清空后为0。由此确认Program冻结页与Projector消费切换没有改变Slot、Operation、Value、Pose Watch、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`214438`成为Actor Runtime诊断投影收口的正式A。

## LinkedPose Committed Diagnostics短租约

状态：提交`64200fb54`已让`CharacterLinkedPoseRuntimeSession`拥有预分配、代际校验的`CharacterLinkedPoseCommittedDiagnosticsView`。LinkedPose仍按原顺序执行Selector、Group写入与Seal；Seal后才把同一Program Result和各Group已提交快照冻结进唯一诊断页。下一次Prepare或Reset会使旧View失效。Snapshot Publisher不再持有LinkedPose Runtime，也不再现场调用`CreateCommittedSnapshot`，只消费与根`CharacterPoseFrameExecutionResult`同lineage的短租约。提交同时修正前序精确暂存遗留在提交树中的构造器和`BeginCommittedDiagnostics`错位行，使提交本身不再依赖工作区残留。Stack、Route、StateMachine、Inertialization和RootWarp仍待迁移，所以任务13.5和13.6保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0警告、0错误，build server已关闭。Unity首次重连刷新出现一次SourceAssetDB文件时间戳不同步，强制全量刷新后消失；回放和退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有PoseGraph、LinkedPose、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-214438-507-584a118b94b54a6b925bd47eb128147f`，候选B为`Diagnostics/FootPlacementRuns/20260901-215843-383-9b8dbac4d3c646b58e10695f2287f1d9`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-215943-008-0835d958c03e4e41b4a2424c17ce3ba2.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件hash、detail／index大小与hash、分析耗时和Surface identity规则归一化后10／10相同，七维分项与总分84.2不变。由此确认LinkedPose诊断读取边界迁移没有改变Linked Pose Group、Slot、Operation、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`215843`成为剩余Actor Runtime诊断投影收口的正式A。

## Root Orientation Warp并入Actor Committed Diagnostics页

状态：提交`92ebd3984`已建立预分配、代际校验的`CharacterPoseActorCommittedDiagnosticsView`并先接入Root Orientation Warp。每个Pose帧开始会立即使上一代View失效；只有已完成Program Result且Live／Capture确实需要基础状态时，Projector才从各Warp的Committed页复制只读快照。Snapshot Publisher删除RootWarp Runtime列表参数和现场`CreateDiagnosticsSnapshot`调用，只消费与根Execution Result同lineage的Actor View。RootWarp的Begin、Prepare、Commit、曲线采样、Facing Error与Yaw Offset公式均未改。Stack、Route、StateMachine与Inertialization仍由Snapshot现场读取，所以任务13.5和13.6保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity脚本刷新重连时出现一次Snapshot Publisher文件时间戳不同步，强制全量刷新后消失；回放和退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有RootWarp、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-215843-383-9b8dbac4d3c646b58e10695f2287f1d9`，候选B为`Diagnostics/FootPlacementRuns/20260901-221205-709-14169caf31c54ac9941f5f68c966f9d6`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-221307-811-afa95c8ad09d4e58b73d3d55708eb85f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Root Orientation Warp诊断读取边界迁移没有改变Warp输出、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`221205`成为Stack／Route诊断投影收口的正式A。

## Blend Stack与Transition Route并入Actor Committed Diagnostics页

状态：提交`03a86742d`已把Blend Stack、Entry、Entry／Stored骨骼权重与Animation Slot Route快照并入同一`CharacterPoseActorCommittedDiagnosticsView`。Projector只在Program Result已完成且Live／Capture需要基础状态时调用现有Stack／Route快照入口，把结果写进构造期预分配的固定容量页；Snapshot Publisher删除Stack与Route Runtime参数、现场`CopyDiagnostics`和`CreateSlotSnapshot`调用，只复制短租约中的已冻结数组。Stack Advance、Source更新、Entry权重、Stored Pose、Route决策、Release权限、Inertialization请求和Commit顺序均未改。StateMachine与Inertialization仍由Snapshot现场读取，所以任务13.5和13.6保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新后只有既有FinalIK序列化深度日志。回放封存和A/B完成后读取Console时，另一个MCP观察请求在`MCPForUnity.Editor.Resources.Scene.GameObjectComponentsResource`中反射Animator属性，产生两条OnAnimatorIK限制、三条无AnimatorController和一条非Playback状态错误；调用栈全部位于用户目录下的MCPForUnity `GameObjectSerializer`，没有项目Runtime、Pose、Stack、Route或IK调用栈。该外部观察干扰单独记录，清空后Console为0，不计作运行逻辑回归。

正式A为`Diagnostics/FootPlacementRuns/20260901-221205-709-14169caf31c54ac9941f5f68c966f9d6`，候选B为`Diagnostics/FootPlacementRuns/20260901-221941-106-77c66c8fd22b46898deb7876a8598bc4`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-222039-293-2dac8b71bd0947eda9374049ba410aff.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Stack／Route诊断读取边界迁移没有改变动画选择、过渡、Stored Pose、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`221941`成为StateMachine／Inertialization诊断投影收口的正式A。

## Pose StateMachine并入Actor Committed Diagnostics页

状态：提交`cda8bbbcc`已把Pose StateMachine的Committed header并入同一`CharacterPoseActorCommittedDiagnosticsView`。Projector在基础状态Interest下复制现有`CreateSnapshot`结果；StateMachine动态骨骼权重继续只来自已验证的Program Committed Diagnostics页。Snapshot Publisher删除StateMachine Runtime列表参数和现场快照调用，用Actor header与Program权重组合最终诊断页。State选择、Transition、权重生成、Source准备、StateMachine Commit和Linked Pose关系均未改。当前Actor运行对象中只剩Inertialization仍由Snapshot现场读取，所以任务13.5和13.6继续保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有StateMachine、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-221941-106-77c66c8fd22b46898deb7876a8598bc4`，候选B为`Diagnostics/FootPlacementRuns/20260901-222523-572-cc193515ef1c401893575fb46a03b060`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-222620-545-d5077e9ee35649258985d39c37dba17f.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认StateMachine诊断读取边界迁移没有改变状态选择、过渡、骨骼权重、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`222523`成为Inertialization诊断投影收口的正式A。

## Inertialization并入Actor Committed Diagnostics页

状态：提交`6c05135c1`已把Inertialization header、规则解析结果、每骨骼Position／Rotation／Scale residual和Envelope并入同一`CharacterPoseActorCommittedDiagnosticsView`。原Snapshot中的读取与转换代码原样迁入Actor Projector，写入构造期预分配数组；Snapshot Publisher只复制短租约，不再接收`PoseInertializationNativeProgram`。至此Snapshot `BeginFrame`只组合Source、Program、LinkedPose、Actor、Constraint与Final Publication六种同lineage committed view，不再持有Stack、Route、StateMachine、RootWarp或Inertialization Runtime。Inertialization事件、规则、历史、Accumulator、Residual计算、Commit和Pose输出均未改。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有Inertialization、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-222523-572-cc193515ef1c401893575fb46a03b060`，候选B为`Diagnostics/FootPlacementRuns/20260901-223152-465-a85a2c8b60e241cba07a6f2177d34128`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-223250-104-0d890701c71549b4b4a0d7108b69a43d.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Inertialization诊断读取边界迁移没有改变过渡连续性、Residual、Envelope、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`223152`成为Source Release／Clip／BlendSpace诊断投影收口的正式A。任务13.5和13.6仍不勾选，因为Snapshot `Publish`尚直接读取Source Runtime。

## Clip与BlendSpace并入Actor Committed Diagnostics页

状态：提交`99e2af1fc`已把Clip Player的Foot Step observation和Blend Space Player／Sample深冻结进现有`CharacterPoseActorCommittedDiagnosticsView`。Clip／BlendSpace的选择、时钟和Player状态继续属于Program Actor State，没有被塞进Physical Source Registry或新建第二Source owner。Clip在Owner Capture时按原SampleTime、Cycle、Duration、loop flag和曲线生成零权重observation，Snapshot再用同页已冻结Final Contribution的dominant source weight构造最终值；Blend Space在下一Begin／Advance覆盖前深拷贝Player与Sample，并继续用已冻结Operation Result补Availability。`AnimationPresentationRuntimeSnapshotPublisher.Publish`删除Clip／BlendSpace Runtime参数，只剩release数组尚待迁移。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有Clip、BlendSpace、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-223152-465-a85a2c8b60e241cba07a6f2177d34128`，候选B为`Diagnostics/FootPlacementRuns/20260901-225118-668-312b83f59ad84af8a3119303602e15ce`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-225216-854-00387762475341d8bea73b427587e7c1.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认Clip observation与Blend Space诊断读取时机迁移没有改变source时间、曲线采样、Blend权重、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`225118`成为release completion迁入Source committed view的正式A。任务13.5和13.6只剩Snapshot `Publish`的release数组旁路。

## Release completion归还Source Committed Diagnostics页

状态：提交`23bc1279b`已把release diagnostics interest、固定容量journal、三类release记录、Discard／Reset取消和最终冻结归还`PhysicalPoseSourceRegistry`的`CharacterPoseSourceCommittedDiagnosticsView`。Prepare阶段在真实release发生前冻结是否记录；Stack、Standalone和Action release继续在原调用点、原completion identity记录；全部deferred release执行完成后关闭journal，Capture才把它与当前物理source mapping冻结进同一Source committed页。旧`PosePlanExecutionRuntime.m_ReleasedSources`、count和record flag全部删除；`AnimationPresentationRuntimeSnapshotPublisher.Publish()`变为零参数，只提升`BeginFrame`已经按同lineage组合好的页。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity包、第三方包和既有Input字段，build server已关闭。Unity全量刷新、回放与退出Play期间只有同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有release lifecycle、PoseGraph、IK或采样异常，清空后Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-225118-668-312b83f59ad84af8a3119303602e15ce`，候选B为`Diagnostics/FootPlacementRuns/20260901-230258-692-d855ef3956c74e8290b94323219372a5`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-230359-450-847a166b52ea4caab319436325cad50a.json`，工具对A Proof正式报告`matched:1044`。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告归一化后10／10相同，七维分项与总分84.2不变。由此确认release journal Owner迁移没有改变source释放时机、slot复用、Action completion、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`230258`成为正式Source Module提取的下一A。Snapshot对象本身已不持有Runtime参数，但Actor capture仍由外层读取多个运行Implementation，任务13.5和13.6继续不勾选，待Program Runtime／Source Module内聚后闭合。

## CharacterPoseSourceModule物理资源唯一Owner

状态：提交`625074c30`已建立真实`CharacterPoseSourceModule`，由它唯一构造、持有和销毁`AnimancerPoseSamplingBackend`、`PhysicalPoseSourceRegistry`与source fan-in Playable，并在同一Module内完成物理source注册、capture binding安装、fan-in连接、release identity验证、物理断连、deferred release封口和Committed Source diagnostics冻结。`PosePlanExecutionRuntime`已删除backend、registry、fan-in、previous output和release identity set所有权；Blend Stack与Final Publication只通过Source Module解析物理identity，不再接收Registry实现对象。全仓搜索确认上述三个物理资源各只有Source Module一处构造，因此任务4.3完成。

本步没有把Clip／Blend Space Player时钟、PoseState、Action lifecycle、Slot或Blend逻辑迁入Source Module。它们继续留在现有逻辑Owner，Source Module只接收已经解析的request、capture binding与release许可。Clip／Blend／Action Adapter装配、Source-owned固定页、完整release journal迁移及旧Runtime数组删除仍属于4.1、4.2、4.4至4.8，均未提前勾选。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一工程警告为既有`PipelineBlackboardValueInfoNode.m_ReportedSourceError`未使用字段，build server已立即关闭。Unity脚本刷新、回放和退出Play期间只出现同一条`RootMotion.FinalIK::FBIKChain.reachSmoothing`Domain Reload序列化深度日志，没有Source Module、Playable、release、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-230258-692-d855ef3956c74e8290b94323219372a5`，候选B为`Diagnostics/FootPlacementRuns/20260901-233212-574-ed0f12eb26024a79906148a33263ab74`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-233313-409-4bbe705712294d1ea4f42adc6a1f2474.json`，工具对A Proof正式报告`matched:1044`、`divergent_frame_count=0`和`first_divergent_relative_frame=-1`，Runtime identity、Trace、Start Body、Tick／Presentation Clock、Input hash、Body trajectory hash与1043个表现采样帧均保持一致。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告只在Sample identity、文件／index hash、detail字节与分析耗时以及已证明的一一Surface identity上变化，归一化后10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认本Trace覆盖的source采样与释放时机、slot复用、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose和Physical业务没有因物理Source所有权迁移改变；`233212`成为Source Adapter与Owned页迁移的下一A。

## Source-owned Release Preparation页

状态：提交`4f54a06ab`已在`CharacterPoseSourceModule`内新增固定容量、单调generation的release preparation页。每次release validation后，Module把Physical Registry token与Animancer backend token成对写入自己的私有槽位，只向旧协调Runtime签发包含槽位、generation、physical identity、Source和Node identity的typed `ReleasePreparation`。Standalone、Stack和Action三类外层journal不再分别保存两个Implementation token；成功帧后的断连、backend release与Registry apply统一由Source Module按同一handle执行，重复、过期或未消费handle直接拒绝。Discard会随Physical Source页一起清空Pending preparation，下一Frame和deferred release封口都会拒绝残留handle。

本步没有改变谁决定retirement、Stack／Slot何时允许释放、Action completion identity、Route notification或release diagnostics记录时机；这些逻辑仍在原调用位置按原顺序执行。外层三类journal、Action request／acknowledgement与release completion尚未整体迁入Source-owned页，因此任务4.1和4.4继续保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；唯一工程警告为既有Input Value未使用字段，build server已立即关闭。Unity全量刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有release generation、capacity、stale handle、Source lifecycle、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-233212-574-ed0f12eb26024a79906148a33263ab74`，候选B为`Diagnostics/FootPlacementRuns/20260901-234825-639-d86985e088c8402d9d0f7c6315a4223d`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-234924-881-6531da2c29b149e9a57d163b87218989.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件／index hash、detail字节、分析耗时与Surface identity规则归一化后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认release token成对所有权迁移没有改变source释放时机、slot复用、Action completion、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`234825`成为完整release journal迁移的下一A。

## Source-owned Frame Demand与Result页

状态：提交`d938f7a96`已把唯一`CharacterPoseSourceFrameLease`、Demand、Result、ready、Seal和Discard状态从`AnimancerPoseSamplingBackend`迁入`CharacterPoseSourceModule.SourceFramePage`。Source Module现在先创建同lineage lease，再让backend只按该lease的Frame identity打开物理mutation；Demand与Source Frame Result只写Source-owned页，Validate和Evaluate Barrier前由Module验证ready，Commit按原顺序先关闭backend、Seal页，再提交Physical Registry；Barrier前Discard仍先丢backend mutation和页，之后按原顺序丢Physical Registry页。

`AnimancerPoseSamplingBackend`已删除`CharacterPoseSourcePendingPage`、Demand／Result字段、lease创建、Bind／Require接口与Seal／Discard业务页调用，只保留SourceVisual、owner slot、clip plan、release permission、deferred resource和Playable capture mutation。全仓搜索确认`new CharacterPoseSourceFrameLease`及Demand／Result Pending字段只剩Source Module一处。Source Binding、Prepared Resource、Usage、Release completion完整页以及只消费Program Demand／Usage的最终窄Interface仍未全部闭合，因此任务4.1和4.5保持未完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；依赖警告和既有Input Value未使用字段不属于本步，build server已立即关闭。Unity刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有Source lease、Pending page、Barrier、Playable、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-234825-639-d86985e088c8402d9d0f7c6315a4223d`，候选B为`Diagnostics/FootPlacementRuns/20260901-235713-270-10be4886ce4a48f0ab7eedb009127664`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260901-235813-753-468dd009d8d240e587fd65dad221fb24.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告只在既定Sample identity、文件／index hash、detail／index字节、分析耗时和一一Surface identity上变化；其中`analysis.index.bytes`及`performance.indexBytes`仅少1字节，排除既定index大小字段后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认Source Frame页所有权迁移没有改变Demand消费、Source采样、Barrier时机、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`235713`成为Source Binding／Usage页迁移的下一A。

## Direct／Clip／BlendSpace Source Binding页

状态：提交`358647863`已在`CharacterPoseSourceModule`内建立固定容量Direct、Clip和Blend Space Binding页。每个Prepare阶段按同一Completion identity清空并打开页；物理source注册、backend prepare、capture binding安装和fan-in连接完成后，Module把`AnimationPhysicalSourceIdentity + capture SourceIndex`作为typed `SourceBinding`写入对应Player槽位。Program侧准备Player Job时只按当前Completion读取Binding；未激活Player读取默认无效Physical identity与`SourceIndex=-1`，保持原行为。

`PosePlanExecutionRuntime`已删除`m_DirectPhysicalSources`、`m_SequencePhysicalSources`、`m_BlendSpacePhysicalSources`和三组SourceIndex数组，以及对应构造、每帧清空和写入代码。Player的Relevant判断、Clip／BlendSpace时钟、source-local sample、continuity、PrepareCapture与PrepareJob仍由原逻辑Owner执行；Source Module不选择Player、State或跨source权重。旧Runtime仍保存release pool、usage和其它source控制journal，因此任务4.1与4.6均未整体完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有Source Binding、Completion、Player Job、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260901-235713-270-10be4886ce4a48f0ab7eedb009127664`，候选B为`Diagnostics/FootPlacementRuns/20260902-000722-129-1675f4cae70142a1a4c177739208b7bd`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-000845-600-5dd258f8bc9c4492b4e44f76922fc0a1.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件／index hash与大小、分析耗时和Surface identity规则归一化后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认physical identity scratch迁入Source Binding页没有改变Direct／Clip／BlendSpace绑定、Source时间、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`000722`成为Source Usage／Adapter迁移的下一A。

## CharacterPoseSourceFrameResult唯一Publisher

状态：提交`a8422da09`已让`CharacterPoseSourceModule`从自己的Pending Demand页读取唯一Demand，构造、校验并绑定`CharacterPoseSourceFrameResult`后把typed Result返回Program侧。`PosePlanExecutionRuntime`不再直接`new CharacterPoseSourceFrameResult`或调用Source页`BindResult`，只保留原`IsReady`失败政策并把成功Result交给后续Program Prepared合同。Action／provider sample字典、availability判断、字段顺序和Source Adapter数学均未改变。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；警告只来自既有Unity／第三方依赖与Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play期间只出现同一条FinalIK Domain Reload序列化深度日志，没有Source Result、Pending page、PoseGraph或IK异常；单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-000722-129-1675f4cae70142a1a4c177739208b7bd`，候选B为`Diagnostics/FootPlacementRuns/20260902-001521-259-0a58993b2ae64106b5e93e82413de6b7`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-001644-239-6ae2ea944e0e404db45e61069be963dc.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突；十份正式报告按既定Sample identity、文件／index hash与大小、分析耗时和Surface identity规则归一化后10／10相同，七维分项、总分84.2和weighted evidence 96.5不变。由此确认Source Frame Result唯一写入Owner迁移没有改变sample readiness、Source时间、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose或Physical业务；`001521`成为新诊断架构文档对账后的下一PoseGraph实现基线。

## Constraint Pending页删除独立Completion身份

状态：候选已删除`CharacterPoseConstraintRuntime.Bank.CompletionIdentity`及`BindCompletion`。Constraint Pending页现在只保存根`CharacterPoseConstraintFrameLease`的开放lineage，并只响应根Seal／Discard；Foot、PoseBone Contribution、Goal Set、Solver Outcome和最终`CharacterPoseConstraintResult`仍各自携带并验证原Completion identity，`CompleteFrame`继续以根完成lineage核对全部typed结果。正常执行顺序、公式、Goal、BendHistory与Fault政策均未改变。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-001521-259-0a58993b2ae64106b5e93e82413de6b7`，候选B为`Diagnostics/FootPlacementRuns/20260902-004806-171-217a25ba6e794a92b95a49f903d35030`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-004904-251-bce67b695be34b298b17e657414381fb.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告除Sample／文件／index hash、detail字节与分析耗时外归一化后10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认删除Constraint Pending的第二Completion身份没有改变Foot、Pelvis、Goal、Assembler、Bend、FBBIK、Final Pose、Physical或正式诊断业务；任务3.7完成，`004806`成为下一PoseGraph小步的正式A。

## Source release成对Owner收口

状态：候选已删除`CharacterPoseSourceModule`对外暴露的原始`AnimationPhysicalSourceReleaseToken`准备／应用入口。Blend Stack正常retirement以及Direct、Clip、Blend Space清理现在全部取得Module-owned `ReleasePreparation`；该token在同一固定页中成对保存backend与physical release，并由Source Module唯一执行fan-in断开、backend release和Physical Registry release。Player／Stack仍只提交自己的逻辑release，不接触Playable或Physical资源。

本步没有勾选任务4.4：prepared source创建、slot reuse、retirement permission和全部release completion还需继续从旧Runtime迁入Source Module；这里只关闭了已确认的physical-only旁路。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-004806-171-217a25ba6e794a92b95a49f903d35030`，候选B为`Diagnostics/FootPlacementRuns/20260902-005827-531-c0d4c41e222548d4b3f49b20e40662ef`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-005923-994-17e8b72516ff4cbaac3c542ab77a5b1d.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告在排除既定运行identity、文件／index hash与大小和分析耗时后逐字段语义对账10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认成对release Owner收口没有改变source retirement时机、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`005827`成为下一Source生命周期小步的正式A。

## Source Frame原子打开backend与physical页

状态：候选已把Physical Source页的`BeginFrame`归入`CharacterPoseSourceModule.BeginFrame`。Source Module现在原子打开自己的Frame页、backend页与physical registry页；physical页打开失败时由Module先Discard backend，再丢弃自己的Frame页。`PosePlanExecutionRuntime`不再知道Source Frame还需要第二次`BeginPhysicalFrame`，正常帧中的页面打开顺序和后续Source准备顺序保持。

本步仍不勾选任务4.4；完整Discard、retirement permission、slot reuse和release completion还未全部藏入Source Module。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-005827-531-c0d4c41e222548d4b3f49b20e40662ef`，候选B为`Diagnostics/FootPlacementRuns/20260902-010730-937-b4dbbc29df944f31919080c7110ccd6a`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-010827-022-5beb8a8de1404acd9eca3428001c3237.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。十份正式报告排除既定运行identity、文件／index hash与大小和分析耗时后逐字段语义对账10／10相同；七维分项、总分84.2和weighted evidence 96.5不变。由此确认Source三页原子打开没有改变source准备、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`010730`成为下一Source生命周期小步的正式A。

## Source Binding页由Demand唯一开启

状态：候选已把`BeginBindingFrame`收为`CharacterPoseSourceModule`私有方法，并在`BindDemand`内直接使用唯一`CharacterPoseSourceDemand.Lineage.CompletionIdentity`清空并开启Direct、Clip与Blend Space Binding页。`PosePlanExecutionRuntime.PrepareEvaluation`不再单独解释Source Binding页的Completion；它继续只负责Program Workspace和各逻辑Player自己的Frame页。

结合前两步的成对ReleasePreparation与三页原子Begin，本步完成任务4.4：全文审计确认`AnimancerPoseSamplingBackend`、`PhysicalPoseSourceRegistry`、backend／physical release token、Source slot reuse、精确retirement校验和deferred release completion只在`CharacterPoseSourceModule`内部；外层只保留Program-owned Player／Stack逻辑release token和不透明Module `ReleasePreparation`。任务4.5与4.6仍未完成，Source Demand／Usage装配和旧Runtime控制journal继续后续迁移。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-010730-937-b4dbbc29df944f31919080c7110ccd6a`，候选B为`Diagnostics/FootPlacementRuns/20260902-011531-535-ebf6e24df7c048589d9fc5d797ab68f4`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-011629-347-466ed65576c148058206b4e506426714.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告的规则、eligible／matched、七维分项、总分84.2和weighted evidence 96.5保持；只允许既定运行identity、文件／index hash与大小和分析耗时变化。由此确认Binding页Owner迁移没有改变source绑定、动画时钟、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或正式诊断业务；`011531`成为下一Source生命周期小步的正式A。

## 建立Foot IK具体Committed Capture存储Owner

状态：候选把旧`AnimationFootPlacementRuntimeSnapshotPage`原地改名为`CharacterFootIkCommittedCaptureViewPage`。该页仍是原来的唯一预分配Foot／FBBIK／Physical存储，旧`AnimationFootPlacementRuntimeSnapshot`只作为同一页、同一`FinalAnimationPoseFramePageLease`的只读Live外观；没有新增第二页、复制字段或改变租约失效规则。后续小步将在这份唯一页上补齐Foot Motion／Observation和具体`CharacterFootIkCommittedCaptureViewLease`，而不是从旧Snapshot再拼一份DTO。

本步不勾选任务13.5：具体View Lease尚未公开，旧Foot Sampler仍通过Snapshot join消费。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-011531-535-ebf6e24df7c048589d9fc5d797ab68f4`，候选B为`Diagnostics/FootPlacementRuns/20260902-012614-285-9e69b176ff8e45108b947330ca3927d3`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-012710-474-40cf684b38ff434eaf4f96f818c5f7ba.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告规则、七维分项、总分84.2和weighted evidence 96.5保持。由此确认具体Foot IK页命名收口没有改变任何Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`012614`成为具体View内容迁移的正式A。

## Foot Motion与Observation归入具体View页

状态：候选把`LeftFootSteps`、`RightFootSteps`、`HasFootFeatures`与`FootStepObservation`从通用Snapshot根页迁入唯一`CharacterFootIkCommittedCaptureViewPage`。旧`AnimationPresentationRuntimeSnapshot`不再保存这些字段的独立值，其公开属性只通过同一`AnimationFootPlacementRuntimeSnapshot`租约读取具体Foot页；Foot／FBBIK／Physical数据仍在同一页，未增加复制或第二事实源。

本步不勾选任务13.5：具体`CharacterFootIkCommittedCaptureViewLease`和完整lineage仍待发布，旧Sampler也尚未切换。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；27个警告只来自既有Unity／第三方依赖和Input Value未使用字段，build server已立即关闭。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-012614-285-9e69b176ff8e45108b947330ca3927d3`，候选B为`Diagnostics/FootPlacementRuns/20260902-013455-708-d230b17fe48240658b668d27e5ec1741`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-013550-868-1924e050c6e946e08584db38174c3a73.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告规则、七维分项、总分84.2和weighted evidence 96.5保持。由此确认Foot Motion／Observation存储归位没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`013455`成为具体View Lease建立的正式A。

## 发布具体CharacterFootIkCommittedCaptureViewLease合同

状态：候选已删除`AnimationFootPlacementRuntimeSnapshot`旧类型名，唯一替换为公开`CharacterFootIkCommittedCaptureViewLease`。具体页在同一Committed Result投影中保存完整`CharacterPoseFrameLineage`，View继续通过同一`FinalAnimationPoseFramePageLease`验证有效期，并公开Foot Motion、FootStep Observation、Foot／Pelvis／Goal、FBBIK与Physical只读事实。通用`AnimationPresentationRuntimeSnapshot`只保留`FootIkCommittedCaptureView`这一具体属性；Trace、Overlay、Gizmo、Visual Validation和旧Sampler都已编译切换到该类型，不保留旧类型别名或wrapper。

本步不勾选任务13.5：Pose Runtime的`PublishDiagnostics`尚未把具体View作为独立Post-Seal结果交给领域Bridge，旧Sampler仍从Debug View取得它并进行二次join。Runtime与Editor工程均按规定参数编译成功、0错误，构建后立即关闭build server；警告只来自既有Unity／第三方依赖与未使用字段。Unity刷新、回放和退出Play只出现同一条FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-013455-708-d230b17fe48240658b668d27e5ec1741`，候选B为`Diagnostics/FootPlacementRuns/20260902-014359-013-28fb4dea2ad6400cac2a3a4c3ded43a7`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-014455-432-8ad76a8566f74f2b814e23afa5297708.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告规则、七维分项、总分84.2和weighted evidence 96.5保持。由此确认具体View类型与完整lineage合同没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`014359`成为独立Post-Seal交付的正式A。

## 由PoseGraph直接交付Post-Seal Foot IK View

状态：`PosePlanExecutionRuntime.PublishDiagnostics`不再只返回是否发布的布尔值，而是在唯一Diagnostics页成功发布后直接返回同页、同`FinalAnimationPoseFramePageLease`的`CharacterFootIkCommittedCaptureViewLease`。`CharacterAnimationPresentationRuntime`把这个Post-Seal结果串行交给现有Foot Trace入口，不再从`m_DebugView.PosePlan`注册结果回读同一View。具体View仍只由Committed Source、Program、Actor、Constraint与Final Publication诊断投影组成，完整核对`CharacterPoseFrameLineage`；它不持有Program Runtime、Workspace、Constraint Module或Physical Transform，也不知道Capability、Sampler、Schema、Generated Program、packet、Host、CSV和Analyzer。通用Snapshot只引用同一具体页租约，没有第二Foot事实页或第二Snapshot。

由此完成任务13.5。任务13.6与13.10仍不勾选：旧Sampler仍在Foot事件后把这份View与旧Snapshot外层信息二次join，尚未由Foot Bridge在租约内调用生成Program并提交typed packet。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功、0错误；27个警告只来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。Unity回放与退出Play只出现既有FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-014359-013-28fb4dea2ad6400cac2a3a4c3ded43a7`，候选B为`Diagnostics/FootPlacementRuns/20260902-015216-834-ccd49fe20da6479b91b0913c7c96627e`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-015313-475-06524d6bc7d546b3a2b9e4a2d2981ab2.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式报告的Analyzer版本75、coverage、规则、eligible／matched、七维分项、总分84.2和weighted evidence 96.5保持；只允许既定运行identity、文件／index hash与大小和分析耗时变化。由此确认Post-Seal直接交付没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`015216`成为Foot Bridge接入前的正式A。

## Source Module原子丢弃完整内部帧

状态：`CharacterPoseSourceModule.DiscardFrame`现在一次接收唯一`CharacterPoseSourceFrameLease`，在Module内部逆序断开本帧pending physical registration，并尽量完成Backend页、Source Frame页、Physical Registry页、Binding与Release Preparation清理，最后统一抛出聚合故障。`PosePlanExecutionRuntime`无论Begin失败还是正式Discard都只请求Source Module丢弃自己的帧，不再读取`HasBackendFrame`、`HasPhysicalFrame`、`PendingRegistrationCount`，也不再取得physical identity后手动断Fan-In端口。旧分裂Discard入口和外层`DiscardPreparedPhysicalSource`已删除。

任务4.6仍不勾选：外层尚有Standalone／Action release journal及Clip、Blend Space、Motion Matching控制集合需要继续归入对应Program或Source Owner。本步Runtime工程按规定参数编译成功、0错误；27个警告只来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。Unity刷新恢复连接后编译成功；回放与退出Play只出现既有FinalIK Domain Reload序列化深度日志，单独记录后清空，3C Console为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-015216-834-ccd49fe20da6479b91b0913c7c96627e`，候选B为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-020430-968-69ab7492738548ec83425931c993bc4d.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。正式评分版本`foot-quality-seven-dimensions/3`、七维分项、总分84.2和weighted evidence 96.5保持。由此确认Source失败清理归位没有改变成功帧的Source、Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`020333`成为下一Source外层清理小步的正式A。

## 根Frame lineage补齐PoseGraph identity

状态：`CharacterPoseFrameLineage`现在除Actor、Frame、Completion、Presentation Frame、Body Tick、Program、PlanHash、Projection、Rig与Tuning Generation外，显式携带`PoseGraphId`和`PoseGraphRevision`。唯一根Frame在`CharacterAnimationPresentationRuntime.BeginFrameTransaction`开始时从同一Projection写入这两个值；`WithCompletion`、有效性、等价和hash均保留完整身份，因此Source、Program、Constraint、Final Publication及具体Foot IK View不再需要从Runtime Target或通用Snapshot补查PoseGraph identity。

本步不勾选Foot任务2.1：具体`CharacterFootIkCaptureInterest`尚未接入Frame开始冻结。Runtime工程按规定参数构建成功、0错误，27个警告只来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。3C Unity force refresh同时验收通用框架`d633e83c1` codec小步和本次lineage改动，未出现Generated Sampling、codec或Character编译错误；回放退出只出现既有FinalIK Domain Reload日志，已清空。

正式A为`Diagnostics/FootPlacementRuns/20260902-020333-815-08bc7f3b04b44d2daa27b8a4919ce442`，候选B为`Diagnostics/FootPlacementRuns/20260902-024336-767-aae40504baa6455e88eeed312795f33f`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-024433-867-aeefd9a688ca4f64bac1a6de2c56cf58.json`，工具对A Proof正式报告`matched:1044`，无failure且Foot Finalizing已结束。

A/B均为2086脚行、1222列，其中1198个业务列逐值相同、24个运行／实例／Surface／Path identity列一一映射且0冲突；Geometry均为67186行、27列，其中22个业务列逐值相同、5个identity列一一映射且0冲突。`character-foot-quality-score/4`七维分项、总分84.2和weighted evidence 96.5保持。由此确认根lineage身份闭合没有改变任何运行或诊断业务；`024336`成为specific Foot interest接入前的正式A。

## Foot IK具体View脱离万能Snapshot Publisher

状态：新增独立`CharacterFootIkCommittedCaptureViewProjector`，唯一拥有两份预分配`CharacterFootIkCommittedCaptureViewPage`、独立Lease、Pending／Active切换和失效。Projector只接收同lineage的Actor、Constraint与Final Publication Committed diagnostics及根Execution Result，在成功Seal后的`BeginCommittedDiagnostics`内组合Foot Feature、Foot Step Observation、Landing／Motion、Goal、FBBIK与Physical事实；Observation直接读取Final Publication Frame的正式Contribution，不再依赖Snapshot页先复制Final Contribution。

`AnimationPresentationRuntimeSnapshotPublisher.Page`已删除Foot页分配、Lineage写入、Foot／Solver／Physical复制、Foot Step Observation解析和Clear；`CreateSnapshot`只接收独立Projector发布的同一短租约，供尚未迁走的Overlay／Gizmo／Visual Validation读取，不产生第二事实页。若任一Publisher失败，Pending页会丢弃或独立Lease整体失效；Reset、显式Invalidate与Dispose同时失效两边。任务13.6仍不勾选，因为万能Snapshot对非Foot的Operation／Pose／Linked等旧读取还在，旧Foot Sampler的事件等待与外层Snapshot join也尚未删除。

Runtime工程使用显式保留build退出码的规定命令构建成功、0错误；1个warning为既有未使用字段，构建后立即关闭build server。3C Unity编译无Projector错误。正式A为`Diagnostics/FootPlacementRuns/20260902-024336-767-aae40504baa6455e88eeed312795f33f`，候选B为`Diagnostics/FootPlacementRuns/20260902-030456-895-95d26ff9b1cb422693202d73c63036ec`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-030553-686-4f82add68b76439baafa50be2de960f3.json`，工具对A Proof报告`matched:1044`，无failure且Foot Finalizing结束。

A/B均为2086脚行、1222列，其中1198业务列逐值相同、24 identity列一一映射、0冲突；Geometry均为67186行、27列，其中22业务列逐值相同、5 identity列一一映射、0冲突。七维分项、总分84.2与weighted evidence 96.5保持。由此确认Foot View Owner拆分没有改变Foot、Pelvis、Goal、FBBIK、Final Pose、Physical或诊断业务；`030456`成为specific Foot interest接入的正式A。

## 建立specific Foot IK interest登记与根帧冻结合同

状态：新增只包含固定View容量的`CharacterFootIkCaptureInterest`与同步`ICharacterFootIkCommittedCaptureConsumer`合同。`AnimationPresentationRuntimeTarget`通过唯一owner成对登记interest和consumer；`CharacterAnimationPresentationRuntime`拒绝第二owner，并在每个`Present`开始把当前绑定复制为`CharacterFootIkCaptureBinding`，随唯一`CharacterPoseFrameTransaction.Begin`冻结，Reset后保持会话登记、Dispose时释放consumer引用。该合同不引用Capability、Sampler、Schema、Generated Program、packet、Session、Writer或Host类型。

本步不勾选任务13.2：Constraint、Final Publication与独立Foot Projector尚未改为消费该specific interest，当前未登记时只冻结default值，也不触发consumer。Runtime工程用显式保留build退出码的规定命令构建成功、0错误；1个warning为既有未使用字段，构建后立即关闭build server。首次Unity增量导入出现一次SourceAssetDB修改时间不一致，文件稳定后force refresh消失，清空Console后为0。

正式A为`Diagnostics/FootPlacementRuns/20260902-030456-895-95d26ff9b1cb422693202d73c63036ec`，候选B为`Diagnostics/FootPlacementRuns/20260902-031403-657-15e34fd7f366452493b4ac159f321244`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-031459-554-babcde41dc0048fe88fbc7ec03c4ef44.json`，工具报告`matched:1044`，无failure且Foot Finalizing结束。A/B主表1198业务列和Geometry 22业务列逐值相同，identity列一一映射，0冲突；总分84.2与weighted evidence 96.5保持。`031403`成为specific interest向各Owner传播的正式A。

## specific Foot interest只开启Foot所需Committed页

状态：根Frame冻结的`CharacterFootIkCaptureBinding`现在随`BeginPendingFrame`进入Constraint Owner。Constraint Bank分别保存general diagnostics与specific Foot interest；Foot Placement页和FBBIK诊断只在general需要或specific Foot开启时冻结，Foot-only不打开Pose Watch Goal页。Actor Committed Projector在Foot-only时只冻结相关Clip Foot Step Observation，不复制Stack、StateMachine、Inertialization、Blend Space或Warp页面。Post-Seal入口在Foot-only时只取得Actor、Constraint与Final Publication Committed View并发布具体Foot租约，不捕获Source、Program、Linked或万能Runtime Snapshot。

`CharacterAnimationPresentationRuntime`只在general或specific任一存在时进入Post-Seal diagnostics；旧Trace与Debug View仍只响应general interest，specific Foot租约只同步交给帧开始冻结的唯一consumer。consumer返回false由其自身Capability Session处理；若consumer违反`TryCapture`合同抛错，Runtime只调用`CaptureFault`并隔离其二次错误，不改变已Seal表现帧或下一帧事实。由此完成任务13.2；PoseGraph只看到容量1的typed interest和领域consumer，不读取Capability、Sampler、Schema、Generated Program或packet容量。

Runtime工程用显式保留build退出码的规定命令构建成功、0错误，27个warning来自既有Unity／第三方依赖与未使用字段，构建后立即关闭build server。Unity编译无错误，退出仅有既有FinalIK日志并清空。正式A为`Diagnostics/FootPlacementRuns/20260902-031403-657-15e34fd7f366452493b4ac159f321244`，候选B为`Diagnostics/FootPlacementRuns/20260902-032312-398-b58a9d6e91894276b0610ca19a27affa`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-032408-358-0e43d95f5b2e425fb360e7ea276a84c8.json`，工具报告`matched:1044`。A/B主表1198业务列、Geometry 22业务列逐值相同，identity列一一映射、0冲突；总分84.2、weighted evidence 96.5不变。`032312`成为Final Publication世界事实扩展前的正式A。

## Final Publication按specific interest封存Root与Physical世界事实

状态：唯一`AnimationFinalPosePhysicalWriter`现在接收正式`CharacterRootHierarchyBinding`，构造时确认其Animator根就是`PoseRoot`。根Frame冻结的`CharacterFootIkCaptureInterest`随Final Publication Pending页保存；只有specific Foot interest开启时，Physical Writer才在完成最终骨骼写入的同一位置一次读取LogicRoot、VisualRoot、PoseRoot以及左右脚踝的最终世界／局部空间事实，写入`CharacterFootIkPhysicalCapture`。未登记Foot Capability的正式运行只保留既有Physical诊断，不构造这组Foot payload。

`CharacterFootIkCommittedCaptureViewLease`只从Final Publication Committed页公开这些值；具体Foot Projector与Foot插件目录全文搜索均不包含`Transform`、`RootHierarchy`、`PhysicalBones`或Animator骨骼查询，因此后续Bridge和Generated Program不再反向读取场景。Runtime与Editor工程按规定参数构建成功、0错误；Editor的30个warning只来自既有第三方包，构建后立即关闭build server。Unity只刷新改动脚本，回放与退出Play仅出现既有FinalIK序列化深度日志，已清空。

正式A为`Diagnostics/FootPlacementRuns/20260902-032312-398-b58a9d6e91894276b0610ca19a27affa`，候选B为`Diagnostics/FootPlacementRuns/20260902-033516-613-1591907a78214a389ab18cdc9835fe66`；B Proof为`Temp/CharacterInputReplayProofs/v4/43357ff3cd384e5cba75d2c31175b116/20260902-033613-172-f30d20daa7554b24b89ed641ecc21a3d.json`，工具报告`matched:1044`。A/B均为2086脚行、1222列，其中1198业务列逐值相同、24个运行身份列一一映射且0冲突；Geometry均为67186行、27列，其中22业务列逐值相同、5个身份列一一映射且0冲突；总分84.2与weighted evidence 96.5不变。由此完成Foot采样重构任务2.2与2.4；`033516`成为Foot Generated Field继续迁移的正式A。

## Source Demand／Prepared Resource／Usage／Retirement外层收口

状态：提交`9260c9d81`、`80645b9d3`、`b036c171d`、`d8732cfb7`、`0916a15d2`、`3758a2bf2`、`d7b0a4cfa`、`de938f796`、`44c25704a`、`0971cd5c8`与`6b08ffeb9`连续完成Source外层收口。`CharacterPoseSourceModule`现在唯一拥有固定容量Binding、prepared resource、Usage、Retirement、release completion与acknowledgement页；Program-owned `CharacterPoseSourcePreparationPage`随`CharacterPoseSourceDemand`发布同Completion短租约，每条命令仍按原顺序追加后立即由Source消费，Source Frame Result发布前验证连续index和零遗漏。

Action、Motion Matching provider、Direct、Clip与Blend Space统一为一个`CharacterPoseSourcePreparation`合同。Source Module内部唯一识别具体Source Adapter，并持有从已校验Projection一次构造的不可变`CharacterPoseSourceCatalog`；旧Runtime的三组Binding数组、physical identity／SourceIndex scratch、Clip Catalog scratch、四套每帧Catalog Builder、MM专属Usage数组／count、release completion List、ack位图和Module嵌套Release token类型均已删除。Program继续唯一决定Player relevance、State、Transition、Slot、Blend、source-local时间、Capture和Retirement Permission；Source不读取这些逻辑Owner状态。

3C Unity通过正常Assets Refresh逐步编译上述Source、Program、Motion Matching与合同文件，当前没有这些文件的编译错误。全局编译停在并行Foot Sampling迁移已删除旧`DiagnosticField` ABI、但旧逐字段声明尚未切换的明确边界；因此本阶段不伪造全工程绿灯，也不在PoseGraph增加兼容Attribute。任务4.1、4.2和4.5按实际所有权闭合完成；任务4.6仍保持未完成，因为旧Runtime还持有Program逻辑release pool／List、Player／Stack集合及其控制journal，后续必须迁入Program Actor State／Frame Pages后删除。为避免在采样ABI切换期间产生无效中间证据，本组合只做一次后续统一Replay，不把当前编译阻塞误报为运行行为差异。

## Program-owned Source Retirement与节点运行索引

状态：提交`8a1410879`先把Direct、Clip与Blend Space的Standalone退休数组、owner分发、Source→Player应用顺序和release诊断记录整体迁入`CharacterPoseProgramSourceRetirementState`。提交`6c8b21e00`继续把Pending Pose与Action backend两套pool／List、request与completion identity、source scratch、exact-source集合、resource identity、Retirement Permission准备、deferred apply、discard边界和跨协议route notification全部迁入同一Program Actor State。Source Module仍唯一执行physical/backend退休；Program State只拥有逻辑Player／Stack token、usage许可和等待外部Action backend确认的协议状态。

提交`72779800a`新增唯一`CharacterPoseProgramNodeRuntimeIndex`，收拢Stack、Route、Player index、Source owner与Direct Player查找，并内聚Motion Matching selection投递和Player source usage判断。旧`PosePlanExecutionRuntime`已删除5个Dictionary字段、三类release对象、三套pool／List、两套scratch、HashSet、resource-id数组与request／completion计数；全文只剩构造期把Direct Player临时收集后转为固定数组的局部List。Source Begin／Validate／Commit／Discard仍在唯一根帧生命周期各执行一次，未新增第二Seal或Discard Owner，因此任务4.6完成。3C Unity正常Assets Refresh没有上述Program State、Node Runtime Index或旧Runtime编译错误；全局失败仍只来自并行Foot Sampling旧字段声明尚未消费0.3 ABI，不影响本项所有权结论。

## Source物理采样旁路审计

状态：全仓Runtime搜索确认`PhysicalPoseSourceRegistry`与`AnimancerPoseSamplingBackend`均只有`CharacterPoseSourceModule`一处构造，`InsertOutputPlayable`也只有该Module安装source fan-in的一处调用；Character Runtime不存在直接`.Play(...)`，不存在以Legacy／Default／Fallback命名或语义补洞的source路径。唯一`AnimationScriptPlayable.Create`位于该Backend内部，source物理注册、Playable连接、capture binding安装、断连与销毁仍由同一Module闭合。

Clip、Blend Space、Motion Matching、Action与Blend Stack保留的多个`PrepareCapture`只按各自Program节点语义准备同帧目标缓冲和typed binding；它们不持有Animancer Graph、Physical Registry或capture Playable，也不能自行连接、播放或发布物理source。Source Module消费这些命令后才由唯一Backend创建／更新capture Playable。因此这些入口是Program逻辑Owner到Source物理Owner的正式输入，不是第二capture owner。审计没有发现需删除的旁路，任务4.8完成；本步没有代码或行为变化，不单独运行回放。

## Source-owned Candidate Tuning Snapshot

状态：新增`CharacterPoseSourceTuningState`、不可变`CharacterPoseSourceTuningSnapshot`与只读`CharacterPoseSourceTuningView`，由`CharacterPoseSourceModule`构造和唯一持有。初始Snapshot从已校验Projection的Tuning Layout与Default Block建立，逐项确认Clip Player的默认`play-rate`与编译Descriptor相同；每次在线候选先验证Program、Projection、Pose Plan、Rig与Layout identity，再建立下一连续`TuningGeneration`的完整候选。Program／Constraint现有调参入口成功后Source才一次提升Committed Snapshot，失败时直接丢弃候选，不对Source执行反向Apply。

`AnimationClipPlayerRuntime`已删除可变`m_PlayRate`、`PlayRate`读取和`ApplyTuning`写入口。Program Actor State仍唯一保存连续时间、cycle、movement clock、continuity和relevance；每帧从同generation的Source View读取Clip倍率，继续按原公式推进普通时钟与Committed Movement时钟，并把同一倍率作为capture的`VisualTimeScale`。Source Module在`BeginFrame`验证根lineage的Tuning Generation，Program Image与旧Native Execution存储均未被修改。

当前发布Tuning schema中，Source sample-local可调字段只有Clip的`play-rate`；Blend Space、Motion Matching与Action没有独立sample-local调参字段。本步不为零字段Adapter制造占位配置、默认值或备用路径；以后新增这三类字段时必须扩展同一Source Snapshot。Program／Constraint仍保留旧可变Apply与失败回滚，留待任务8整体迁移，不能把本步误报为全局Tuning原子化完成。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建后已关闭MSBuild与编译器服务器。3C Unity普通Assets Refresh后新增文件进入正式工程，改动文件没有编译错误；全局Unity编译仍停在并行Foot Sampling尚未从旧`DiagnosticField` ABI迁到0.3 Generated Projection的已知边界。本步不增加兼容Attribute，也不在该外部阻塞期间伪造Replay证据。任务4.7完成，Source阶段4.1至4.8全部闭合。

## Program控制双页归还Frame Pages

状态：新增`CharacterPoseProgramFramePages`，由它一次分配并唯一销毁StateMachine、Animation Slot、Root Orientation Warp、Linked Pose Call和Linked Pose Active Fragment的Committed／Pending两套Native控制页。旧`CharacterPoseGraphNativeProgram`已删除内部`Page`、五组当前页字段、Committed／Pending页引用、Bind／Capture／Allocate／Dispose页逻辑与自己的`m_FrameOpen`，只在现有外层API内把控制读写转交Frame Pages。

Begin仍只切换Pending页并清空本帧Linked Pose选择，Commit仍交换双页，Discard仍恢复Committed页；StateMachine／Slot未初始化存储、Root Warp清零、Linked Call inactive和Fragment清零的构造选项与原实现相同。静态Operation／Stage／Rig常量、运行Tuning Weight、Value Workspace、Operation Completion和Committed Diagnostics尚未迁移，因此任务5.5与5.8不提前勾选，也没有把旧Native Program改名成新Program Image冒充完成。

Unity普通Assets Refresh把新增文件写入正式Runtime工程；补齐现有Presentation control类型的唯一命名空间引用后，`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭。本步不改变公式、Operation顺序、Source、Constraint、Final Publication或IK数据，不单独运行回放。

## Program Committed Diagnostics页归还Frame Pages

状态：`CharacterPoseProgramFramePages`继续接管预分配Committed Program Diagnostics页、单调发布identity、下一帧Begin失效和Dispose失效。`CharacterPoseGraphNativeProgram`已删除嵌套`CommittedDiagnosticsPage`、页字段与diagnostics identity计数，只保留按现有字段语义从已完成Frame复制数据的投影过程；`CharacterPoseProgramCommittedDiagnosticsView`直接租用Frame Pages持有的同一页，不建立第二Snapshot或复制Owner。

诊断interest分组、Operation Completion、StateMachine骨骼权重、Slot Contribution、Value header／Contribution／dense weight／Pose的冻结条件与遍历顺序未改。任务5.5仍不勾选，因为Program Value、Operation Completion的运行写页和Source Demand输出尚未归入Frame Pages；本步只闭合Committed diagnostics物理页与租约寿命。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭；不单独运行回放。

## Program Value与Operation Completion Workspace归还Frame Pages

状态：`CharacterPoseProgramFramePages`现在唯一持有并销毁原`AnimationPoseNativeWorkspace`实现，向Program执行链提供Current Value、Player写页、Operation／Stage Completion、Pose Graph失败、Final Read／Write outcome和Committed Final只读绑定。`PosePlanExecutionRuntime`已删除`m_Workspace`字段与独立Dispose，只保存正式`m_ProgramFrames`引用；构造时创建Workspace、取得初始layout并交给旧Native Program后立即清空局部引用，正常帧不再存在Frame Pages之外的Workspace Owner。

旧`CharacterPoseGraphNativeProgram`构造时验证传入Workspace与自身layout完全一致，并把同一Workspace交给Frame Pages；其Dispose只通过Frame Pages完成一次释放。Begin／RequireStagesCompleted／Commit／Discard、Player／Value／Final binding与容量统计仍调用原Workspace实现，数据布局、清零顺序、completion identity和双页交换没有改变。本步没有建立第二Value页或兼容入口。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭。任务5.5暂不勾选，只剩Program-owned Source Demand／Preparation输出仍由外层字段持有；该页迁入后再按完整Owner闭包判断。

## Program Source Demand输出归还Frame Pages

状态：`CharacterPoseProgramFramePages`新增固定容量`CharacterPoseSourcePreparationPage`和当前typed `CharacterPoseSourceDemand`，统一负责Begin、Bind、逐条Preparation追加、短租约校验与Clear。`PosePlanExecutionRuntime`已删除`m_SourcePreparationPage`字段和构造分配；创建Demand时先由Frame Pages开启唯一Preparation页并保存完整Demand，再把同一只读命令交给Source Module。Prepare Evaluation同时验证调用方Demand、Program-owned Demand与Source收到的Demand具有相同lineage、计数、provider引用和Preparation页identity。

Discard、Reset与Dispose继续在原时机清空Demand，但现在只调用Frame Pages Owner；成功帧仍保留短租约到下一次Demand Begin，生命周期与原实现一致。Source Module的Demand副本只是本Module接收的typed输入，不取得Program Preparation页写权限，也不成为第二Program输出Owner。

至此`CharacterPoseProgramFramePages`已实际持有Pending node control、Source Demand／Preparation输出、Current Value、Operation／Stage Completion、Program失败／Final outcome和Committed diagnostics，任务5.5完成。旧Native Program仍持有静态执行存储及运行Tuning Weight，所以任务5.8不提前完成。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭；本步不单独回放。

## Frame Pages脱离旧Native Program所有权

状态：`PosePlanExecutionRuntime`在构造期建立唯一`CharacterPoseProgramFramePages`，把新建Value Workspace的所有权一次交给它，再将同一Frame Pages只读引用用于旧Native Program静态layout核对和Staged Executor帧绑定。旧`CharacterPoseGraphNativeProgram`已删除Frame Pages字段、Frame Pages构造、Frame Pages Dispose、Begin／Commit／Discard与StateMachine／Slot／Root Warp控制写入口；根当前Program Owner直接驱动Frame Pages，Dispose也只执行一次。

`CharacterPoseGraphStagedExecutor`现在显式接收Frame Pages并从该Owner取得五组当前控制页，静态Program只继续提供Operation、Stage、Rig、Blend catalog及其它执行常量。StateMachine transition发布、Slot与Root Warp控制、Linked Pose选择都写入同一Frame Pages；Linked Pose静态candidate解析仍由旧Native Program提供，但必须显式传入当前已打开Frame Pages，不再依赖其内部可变引用。Committed Diagnostics投影同样显式接收同一页Owner。

`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭。旧Native Program已不持有Frame identity、Pending／Committed控制页或diagnostics页；任务5.8仍不勾选，因为`SetOperationWeight`仍直接修改Native Operation，必须在Program-owned Candidate Tuning Snapshot建立时删除。

## Native Operation Weight迁入Program Tuning Snapshot

状态：新增`CharacterPoseProgramTuningState`、不可变`CharacterPoseProgramTuningSnapshot`与只读`CharacterPoseProgramTuningView`。初始Snapshot逐项保存全部native operation编译权重；Blend Pose、Layered Bone Blend与Additive Pose的可调字段从同identity Tuning Layout／Default Block建立精确映射，其它Operation保持编译默认。候选使用独立Persistent Native权重页，完整验证后才与Source Candidate按同一连续`TuningGeneration`提交；失败直接Dispose候选，不修改Committed页。

`CharacterPoseGraphNativeProgram.SetOperationWeight`与运行时Native Operation回写已删除，Native Operation数组只在构造编译时写一次。`CharacterPoseGraphStagedExecutor`显式接收同generation Program Tuning View，在唯一Stage执行循环读取对应native index权重并构造本次只读Operation；静态Program Image候选、旧Native执行存储和其它Actor不会被在线调参污染。Sequence Preview只执行不可调Clip Player Operation，保持原静态入口。

全文搜索确认旧Native Program不再保存Frame identity、Pending／Committed控制／诊断页、Goal workspace、Tuning Generation、Candidate或运行调参值；Native Operation赋值只剩构造期`CompileOperations`。因此任务5.8完成。任务6.9仍不勾选：PoseState、Blend Stack与Inertialization调参尚未迁入同一Program Candidate Snapshot。`ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误，构建服务器已关闭；全局Unity仍由并行Foot 0.3迁移编译边界阻塞，本步不伪造Replay。

## 建立Program Actor State所有权边界

状态：新增`CharacterPoseActorState`，先整体接管PoseState／Player continuity集合、Blend Stack、Transition Routing、Root Orientation Warp、Inertialization、Program节点运行索引与逻辑Source Retirement状态。`PosePlanExecutionRuntime`删除这些对象各自的存储字段，只保留唯一`m_ActorState`；现有执行代码经内部只读访问器取得同一对象，没有复制状态、第二套页或兼容路径。

Actor State统一负责Stack、Direct Player、Clip Player、Blend Space Player与Inertialization的销毁，顺序与异常聚合规则保持不变。Source物理资源仍由`CharacterPoseSourceModule`拥有，Constraint Bank、Final Publication、Diagnostics、Program Frame Pages、Program Tuning与静态Program存储均未进入Actor State，因此没有复制其它Module真相。

任务5.4暂不勾选：`ActionPlaybackInput` lifecycle／command cursor尚未从外层执行器迁入Actor State，Slot控制的跨帧状态也仍需继续对账。Unity MCP已显式路由到`3C_Client@e852139597e42532`完成脚本编译；本次两个文件的错误筛选均为0。全局错误只来自并行Foot生成采样迁移缺失`KK.GeneratedDiagnosticSampling` ABI，本步不添加兼容类型，也不把外部编译阻塞误报为PoseGraph回归；不单独运行回放。

## ActionPlayback与Slot跨帧状态归入Actor State

状态：`CharacterPoseActorState`继续接管唯一`CharacterActionPlaybackRuntime`与`AnimationSlotRuntime`。其中Action对象保存command inbox、`PendingFirstSample／Selected／Retained／Retired` lifecycle、Latest Command Sequence和Committed sample history；Slot对象保存Current／Outgoing／Retained Action、Selection Generation与固定Pending页。外层`CharacterAnimationPresentationRuntime`已删除这两个存储字段，只在构造期创建后一次交给Pose Runtime的Actor State，并经只读访问器调用同一实例。

原外层`m_NextPresentationRequestSequence`也迁入Actor State，Slot Action source发布和Slot target发布统一从同一递增cursor取号。Begin、sampling、usage／retirement、Validate、Seal、Discard和Reset调用顺序未改变；本步只改变跨帧状态归属，没有把节点执行阶段提前迁入Program Runtime，任务6.2仍保持未完成。

全仓审计确认`ActionAnimationPlaybackLifecycleRegistry`只有`CharacterActionPlaybackRuntime`一处构造，外层不再保存Action lifecycle、Slot runtime或presentation request cursor。Actor State也不引用Animancer、Physical Source Registry、Constraint Bank、Final Publication、Diagnostics Publisher或其Committed页；逻辑Source Retirement只保存Program发布的usage／permission与Action release协议状态，不拥有物理资源。由此任务5.4与5.9完成。Unity MCP显式路由到`3C_Client@e852139597e42532`后，三个触碰文件的编译错误筛选均为0；全局仍只被并行Foot 0.3 ABI迁移阻塞，不新增兼容路径，也不单独运行回放。

## 唯一Pose语义对象改名为Program Image

状态：原`CharacterPresentationPosePlan`类型直接改名为`CharacterPoseProgramImage`，三个partial声明、Projection序列化字段类型、Compiler输出以及Runtime／Editor全部强类型引用一次切换；主定义文件与原meta guid同步改名。没有保留旧类型、派生alias、wrapper、转换器或第二份数组，因此当前Projection仍只有一个Pose语义对象，现有序列化字段名、schema值、hash、Operation／Stage顺序和Workspace容量均未改变。

任务5.1暂不勾选：Program Image内部还保存旧`CharacterPresentationPoseOperation`万能记录，尚未切成Operation Header、typed Value Reference和Family Payload页；`CharacterPoseGraphNativeProgram`也仍在Actor构造时从Image materialize并带有运行时Compile命名。下一步继续把它收窄为只读Execution View并删除运行时补齐能力。Unity MCP server域重载后已自动重连，所有调用都显式路由`3C_Client@e852139597e42532`；新旧类型名的编译错误筛选均为0。全局Foot ABI阻塞不属于本步，也不为它增加兼容路径或额外回放。

## Native Program改名并收口为Execution View

状态：原`CharacterPoseGraphNativeProgram`类型与文件直接改名为`CharacterPoseProgramExecutionView`并移入Program目录，旧类型全文删除。`PosePlanExecutionRuntime`保存字段同步改为`m_ExecutionView`，构造、校验、Executor绑定、Linked Pose查询、Committed diagnostics与Dispose全部继续指向同一实例；没有同时保留Native Program与Execution View两套对象。

任务5.2与5.3暂不勾选：当前Execution View仍在Actor创建时从Program Image、Rig和Blend Catalog执行`Compile*` materialize，也尚未携带并核对Program、Projection Revision和Pose Program Image Hash完整身份；它仍由旧`PosePlanExecutionRuntime`而不是最终`CharacterPoseProgramRuntime`释放。后续必须把可执行静态页在Projection Build时写入Image，让View只逐值分配只读Native存储，并删除运行时Compile入口。Unity经3C MCP `Assets/Refresh`重建项目文件后，旧文件路径、旧类型、新Execution View和Pose Runtime的错误筛选均为0；不单独运行回放。

## Execution View Operation集合归还Program Image

状态：决定哪些语义Operation需要进入Unity Execution View的唯一`IsExecutionViewOperation`判定已迁入`CharacterPoseProgramImage`。Image自身的Stage closure校验与Execution View的容量计算、materialize过滤现在读取同一判定；Execution View删除原`IsNativePoseOperation`静态语义入口，不再被Image反向依赖。判定集合逐项保持原Runtime值，因此没有改变Operation数量、Stage范围或执行顺序。

Editor旧Builder仍有一份自己的Native Operation判定，且其集合包含Motion Matching／Pose History差异；本步为保持现有生成资产与运行行为不修改它，也不把任务5.1或Compiler任务11误报完成。该重复会在Operation Header／Family Payload与Compiler固定Pass迁移时删除。3C MCP对新判定、Program Image和Execution View的编译错误筛选均为0；不单独运行回放。

## Program Image与Execution View绑定完整Projection身份

状态：`CharacterPoseProgramImage`在Projection完成装配时一次绑定ProgramId与ProjectionRevision，并以现有PlanHash公开`PoseProgramImageHash`；重复绑定只允许完全相同的身份。`CharacterPresentationProjection.RequirePosePayload`在每次Runtime入口先建立／核对该绑定，因此磁盘只保留外层Projection一份序列化身份，不复制第二套可漂移字段，也不需要旧资产fallback。

`CharacterPoseProgramExecutionView`构造入口收窄为完整Projection、Frame Pages与layout，不再让调用方分别传Image、Rig和两个Blend Catalog。View从同一Projection取得这些只读输入，在materialize前核对ProgramId、ProjectionRevision、PoseProgramImageHash与Rig identity，并用固定字符串保存这组身份；自身有效性同时要求四组身份非空。原数组容量、Compile顺序、Operation内容和执行阶段不变。

任务5.2仍不勾选，因为View还在运行时执行`CompileRig／CompilePayloads／CompileOperations／CompileStages`，尚未做到只逐值materialize Image内已编译静态页。3C MCP域重载后按同一实例重试成功，身份绑定、Execution View与Pose Runtime的错误筛选均为0；通用采样器0.4不要求PoseGraph增加View／DTO／Event，因此本步也未改变诊断或回放链。

## Execution View停止写入Actor Frame页

状态：`CharacterPoseProgramFramePages`构造入口改为直接接收唯一Program Image，自身从Image取得StateMachine、Slot、Root Warp、Linked Call与Fragment固定容量，并按原值初始化Committed StateMachine entry control。`PosePlanExecutionRuntime`不再逐项拆出五个容量传参，Frame Pages layout只由Image决定。

`CharacterPoseProgramExecutionView.CompilePayloads`删除Frame Pages参数和StateMachine control写入，只materialize自己的静态Mask、Additive Reference、Modify Bone、Root Warp、Pose Bone Goal与Goal input页。初始化写入时机仍在Actor构造期、首帧之前，control内容和首帧Begin／Commit／Discard语义不变；区别是Actor页只由其Owner写，Execution View不再接触可变节点状态。任务5.2仍等待静态页与Operation／Stage materialize彻底去除运行时语义编译后再勾选。3C MCP对Frame Pages、Execution View和Pose Runtime的编译错误筛选均为0；不单独运行回放。

## Execution View构造移除Frame Pages依赖

状态：`CharacterPoseProgramExecutionView`构造现在只接收完整Projection与只读Workspace layout，不再接收`CharacterPoseProgramFramePages`。View保存Image的Linked Pose Fragment固定数量并以它校验Candidate索引，自身`RequireValid`只检查静态Native页、身份和静态layout，不调用Frame Pages有效性或读取其当前页。

Execution View与Frame Pages之间的Control容量一致性改由`CharacterPoseGraphStagedExecutor`绑定时校验：先分别验证View与Frame Pages，再核对Root Warp、Linked Call和Linked Fragment容量。这样静态View不再把Actor帧对象当作构造依赖，而真正执行时仍保留同样的交叉边界检查。View上的Committed diagnostics投影与Linked selection方法仍显式接收短期Frame Pages参数，留待Program Runtime迁移执行职责时删除，任务5.2暂不提前完成。3C MCP对View、Executor和Pose Runtime的编译错误筛选均为0；不单独运行回放。

## Program Committed Diagnostics移出Execution View

状态：新增独立`CharacterPoseProgramCommittedDiagnosticsProjector`，连同`CharacterPoseProgramCommittedDiagnosticsView`一起归入PoseGraph Diagnostics目录。原Execution View中的Committed页冻结、Operation／Slot／Value复制、StateMachine诊断权重计算、diagnostics layout核对和interest分组逻辑全部删除；View文件不再引用Diagnostics命名空间，也没有诊断页类型。

`PosePlanExecutionRuntime`在Actor构造期以唯一Execution View建立一个无运行状态的Projector，Post-Seal且Runtime diagnostics interest命中时才调用它冻结Frame Pages的Committed diagnostics。复制字段、遍历顺序、曲线／Profile权重公式、interest gating和页发布identity完全保持；正常无诊断运行不调用Projector，Execution View执行链不再承担诊断职责。Linked Pose selection仍暂留View，等待Program Runtime执行迁移。3C MCP最终清空重编后Projector、Execution View和Pose Runtime错误均为0；不单独运行回放。

## 建立CharacterPoseProgramRuntime组合Owner

状态：新增`CharacterPoseProgramRuntime`，唯一保存同一Actor的Program Image只读引用、唯一Execution View、Actor State、Program Frame Pages与Program Tuning。构造时逐项核对ProgramId、ProjectionRevision、PoseProgramImageHash和Rig identity；Dispose按原顺序统一释放Actor State、Tuning、Execution View与Frame Pages，并保持异常聚合。

旧`PosePlanExecutionRuntime`删除上述四个独立存储字段，只保留一个`m_ProgramRuntime`；当前大类内部的过渡访问器全部指向该Owner中的同一对象，没有复制页、状态或第二View。Source Module、Constraint Runtime、Final Publication和Diagnostics Projector未进入Program Runtime。任务5.3与6.1暂不勾选：Execution View仍含运行时Compile，Executor仍是每帧构造且旧大类尚未收窄为根协调器。3C MCP重建新文件后Program Runtime与旧Pose Runtime错误筛选均为0；不单独运行回放。

## Staged Executor改为Actor级持久Implementation

状态：`CharacterPoseGraphStagedExecutor`从每帧值类型改为每Actor唯一引用类型，只在`CharacterPoseProgramRuntime`构造时建立一次。Program Runtime保存该Executor，并以`BindExecutor`在每帧绑定Committed Program Tuning、当前Program Frame页、当前Inertialization页、当前Workspace binding、completion identity与diagnostics开关；Prepared页保存同一Executor引用，不再复制大结构体。

Execution View的Operation／Stage／Rig／Mask／Blend／Linked Pose静态页，以及Inertialization规则页和Constraint入口，只在持久构造时绑定一次。Root Warp／Linked／StateMachine／Slot control、Inertialization active／committed页和Workspace Slot／Value／Completion页仍按原时机从当前Pending／Committed页取得，所有Evaluate、Stage和Operation顺序未改。全文只有Program Runtime一处`new CharacterPoseGraphStagedExecutor`，任务6.3完成；任务6.1仍等待Program Runtime直接接收根Frame lease和旧大类外层收窄。3C MCP对Executor、Program Runtime和Pose Runtime错误筛选均为0；不单独运行回放。

## Linked Pose selection写入归还Program Runtime

状态：`CharacterPoseProgramExecutionView`删除`SetLinkedPoseGroupSelection`，只保留按index读取Call Group／Interface以及查找静态Candidate的只读查询。`CharacterPoseProgramRuntime`接管Group selection验证与Frame Pages的Call Control／Active Fragment写入，旧Pose Runtime只把根会话给出的generation handle交给Program Runtime。

Call遍历顺序、Group／Interface／Implementation匹配、重复Control保护、Candidate选择、generation、Pose discontinuity和active fragment写值逐项保持。Execution View不再写Program Frame页；Program Runtime成为Linked Pose节点控制的唯一写入入口。任务6.2暂不勾选，其他PoseState／Player／Slot／Blend／Inertialization执行仍需继续迁移。3C实例重连后Program Runtime、Execution View和Pose Runtime错误筛选均为0；不单独运行回放。

## 现有Final双页实现正式归名Publication Module

状态：原`ComposedAnimationPoseFramePublisher`类型和文件直接改名为`CharacterFinalPosePublication`并移入PoseGraph Final目录，旧类型全文删除。旧Pose Runtime字段同步从`m_FramePublisher`改为`m_FinalPublication`，Begin、预验证、唯一Physical Write、Pending Result准备、Commit／Discard、Committed diagnostics、Invalidate和容量统计全部继续调用同一实例，没有新增wrapper或第二发布路径。

该具体Module已经实际持有唯一双页Dense Final Pose、Parameter、Contribution、Bone Kind与page lease，唯一Pending页、Committed Publication Result、完整Rig布局和唯一`AnimationFinalPosePhysicalWriter`。因此任务7.1完成。Output layout handle绑定、Writer前完整统一验证、Writer后no-throw Seal和旧外层调用清理仍分别留在7.2至7.9，不提前勾选。3C MCP执行`Assets/Refresh`后新Publication、旧文件路径和Pose Runtime错误筛选均为0；不单独运行回放。

## 唯一Physical Writer归入Final Publication

状态：`AnimationFinalPosePhysicalWriter`直接改名为`CharacterFinalPosePhysicalWriter`并从Presentation／Animancer目录移入PoseGraph Final目录，旧类型与旧文件路径删除。`CharacterFinalPosePublication`现在接收正式Rig Binding与Root Hierarchy Binding并在Module内部唯一构造Writer；旧Pose Runtime删除writer局部变量、构造和传递，只构造Publication。

全仓只有Final Publication一处`new CharacterFinalPosePhysicalWriter`和一处`Write`调用；Physical Transform的position／rotation／scale写入只存在该具体Writer，没有Writer Graph节点、接口、第二Implementation或旁路，因此任务7.9完成。任务7.8暂不勾选：Program Workspace仍保存Output read buffer，Publication仍把结果复制到自己的Committed／Pending双页，必须先用正式Publication layout handle闭合7.2与7.3，不能仅凭唯一Transform写入冒充唯一Final Pose页。3C MCP重连后新旧Writer、Publication和Pose Runtime错误筛选均为0；不单独运行回放。

## Final Physical Write统一前置结果验证

状态：`CharacterFinalPosePublication.WritePhysicalPose`现在同时接收Publication lease、完整lineage、Program Result、Constraint Result与Final read binding，在进入具体Writer前统一核对lease、Program／Constraint有效性及同lineage、两者Outcome、Completion、PoseGraph Completed、成功帧continuity、availability／invalid metadata、invalid operation和Rig identity。TypedInvalid帧不强行要求Pose continuity，继续走既有Committed／reference fallback。

具体Writer仍先遍历全部Physical Bone验证Transform和解析后的Pose，再在第二次唯一循环一次写入全部local position／rotation／scale；任一无效输入在首个物理写之前发布原Fault outcome并抛错，成功Pose用Pending，TypedInvalid用上一Committed或reference。由此任务7.5与7.6完成。任务7.7仍不勾选：Writer之后的Publication frame复制和部分Seal准备仍包含可抛验证，必须继续前移后才能证明Writer后no-throw。3C MCP对Publication和Pose Runtime错误筛选均为0；不单独运行回放。

## Final Publication全部可抛准备前移到Writer之前

状态：Final Publication Pending页拆成`Prepare`与`RequireReady`两段。`PreparePending`现在在Physical Writer之前完成lineage／Program／Constraint验证、页选择、Final Pose／Parameter／Contribution／Bone Kind复制、source identity展开、TypedInvalid判定和Publication Result构造；成功帧预期Applied identity由lineage确定，TypedInvalid预期为0，不再等待Writer后反读运行页。

随后唯一Writer只执行既有全量预检和完整骨架写入，并把实际Physical diagnostics赋给已准备Pending页；Writer返回后没有Foot、Goal、FBBIK、source展开、Frame复制、Outcome switch或其它业务验证。旧Pose Runtime的Writer后`RequireFinalWriteOutcome`、Outcome switch和后置`PreparePending`已删除，Workspace与Frame Pages两层无调用转发也删除。Seal前`RequireReady`只核对已准备页和成功帧Physical write完成事实。由此任务7.7完成；7.2／7.3／7.8仍等待消除Program Final buffer到Publication双页的复制。3C MCP四个触碰文件错误筛选均为0；不单独运行回放。

## Output与唯一Writer责任对账

状态：Compiler只在根Graph lowering时接受一次`OutputPose`并记录唯一`OutputOperationIndex`，Program Image Seal再次要求Output数量为1、位于Final Publication domain且占据记录的index；Compiler和Image均不创建Writer、不读取Rig Binding或Root Hierarchy。Runtime全文只有旧Pose Runtime装配路径一处`new CharacterFinalPosePublication`，而具体`CharacterFinalPosePhysicalWriter`只能在Publication构造内部建立。

Writer构造立即验证完整`CharacterAnimationRigBinding`、正式`CharacterRootHierarchyBinding`、Physical Bone数量、PoseRoot和Root Bone policy；Program Image／Rig payload不匹配也在Publication构造失败。不存在第二Factory、Writer接口或Graph节点，因此任务7.4完成。任务7.2／7.3仍需把Output Family改成正式Publication layout handle。该步只做现有责任审计，不修改代码或运行行为，也不单独运行回放。

## 建立Final Publication稳定布局句柄

状态：`CharacterPoseProgramImage`现在从唯一Output Operation与已编译固定容量产生`CharacterFinalPosePublicationLayoutHandle`，句柄只包含layout slot、Output Operation／Value identity、逻辑Pose Value数量、Bone／Parameter数量和单Value Contribution容量，不包含Actor实例、页引用或Physical Writer。Image Seal同时证明Output Value是最后一个逻辑Pose Value，并把原先散落在Workspace与Publication构造中的Contribution整除检查收回Image边界。

`AnimationPoseNativeWorkspace`与`CharacterFinalPosePublication`不再各自重新查找万能Operation并推算Output布局，二者均消费同一个handle；Workspace仍暂时按旧数量分配Output Value页，Publication仍暂时复制到自身双页，因此本步不提前勾选7.2／7.3／7.8。3C MCP清空后仅存在既有FinalIK序列化深度错误，三个触碰文件的编译错误筛选均为0；不单独运行回放。

## Physical Writer回退只读Publication Committed Frame

状态：`CharacterFinalPosePhysicalWriter`在Pending Output为Typed Invalid时不再接收或读取Program Workspace的上一帧Output slot，而只接收`CharacterFinalPosePublication`自身保存且已成功发布的Committed `ComposedAnimationPoseFrame`。Publication以自身Committed Result、Physical Write diagnostics与Frame completion共同决定回退页是否可用，并在进入Evaluate前完成相同Physical Bone binding与Committed Pose验证。

旧Pose Runtime的Prepared页删除`HasCommittedFinal`与`CommittedFinalRead`，不再为了Physical Writer取得Program Workspace Committed Output binding；Pending Output执行、Typed Invalid回退顺序、Root Bone policy、整骨预检、唯一Transform写循环和Physical diagnostics保持不变。Program Workspace的Committed Output目前仍供旧Program diagnostics读取，待actor-local Publication binding完成后一起删除，因此7.8暂不提前勾选。3C MCP对Writer、Publication和Pose Runtime编译错误筛选均为0；不单独运行回放。

## Output Operation直接写入Publication Pending页

状态：Final Publication在Actor装配时保存唯一Program Image layout handle与Source identity resolver，每帧由根Publication lease把该handle绑定到下一张唯一Pending页并生成`CharacterFinalPosePublicationOutputBinding`。持久Program Executor在唯一Output Operation位置通过该actor-local binding直接写入Dense Pose、Parameter、Contribution、Dense Weight与Foot Feature，并发布只读`CharacterPoseProgramOutputResult`；旧Pose Runtime不再构造、保存或反读`AnimationFinalPoseNativeReadBinding`。

具体Physical Writer改为只消费同一Pending `ComposedAnimationPoseFrame`与Program Output Result，Program diagnostics在Post-Commit阶段通过Publication的短期Committed Frame view读取最终Output，非Output Value仍读取Program Committed页。Output continuity组合、Typed Invalid reason、source contribution展开、Preview输出、Root Bone policy、完整骨骼预检和唯一Transform写循环保持原顺序与数值；运行时不双写，Diagnostics也不复制第二份最终Pose。3C MCP对Runtime触碰文件编译错误筛选为0；不单独运行回放。

## 删除Program第二Final Output页并修正静态Workspace合同

状态：`AnimationPoseNativeAggregateLayout`现在明确区分逻辑`PoseValueCount`与真实`PoseValueWorkspaceCount`，后者等于唯一Output Value index，所有Program Pose／Parameter／Contribution／Header Native页只按真实Workspace数量分配。旧`FinalAppliedAt`、`FinalWriteOutcome` Native数组、Committed Final binding、Final read binding类型与Frame Pages转发入口全部删除；Final write outcome只存在于Publication Result，最终页只存在于Final Publication。

Program Image对外明确逻辑Value数量、真实Workspace数量和单Value Contribution容量；Compiler的Workspace计划排除最后一个Output Value，Inertialization重建保持三者原值。审计确认全仓只有Final Publication一处具体Writer构造，只有Writer一处Physical Bone position／rotation／scale写循环，不存在第二Final页、旧Final Native ABI或Diagnostics Pose复制。由此任务7.2、7.3与7.8完成；3C MCP对Program Image、Execution View、Compiler、Workspace、Executor与Publication相关文件错误筛选均为0，当前全局Editor错误来自另一个Foot采样迁移窗口，不属于本change；不单独运行回放。

## Constraint与FBBIK改为Candidate调参提交

状态：`CharacterFinalIkFullBodySolver`的调参入口拆成Prepare／Commit／Discard。Prepare仍按原Layout、字段、值域和`ActiveTuning.RequireValid`构造候选，但不再修改Active Tuning、FinalIK Vendor属性、Effector或Bend状态；`CharacterPoseConstraintRuntime`保存候选generation与`resetOwnerState`，Foot Placement对当前无独立可调字段的合同只做Block预验证。全部前置成功后Commit才一次应用同一FBBIK值，并按原顺序执行Effector Reset、Solver Bend Reset与Constraint Bank BendHistory清理。

Constraint在Begin Frame时要求自己的Committed generation与根lineage一致；失败候选只Discard且不会改变已提交Vendor方向或BendHistory。由此任务3.8完成。3C MCP对FBBIK Solver、Constraint Runtime、Foot Placement Module与Pose Runtime编译错误筛选均为0；没有改IK公式、参数范围或运行阶段，也不单独运行回放。

## 统一Program／Source／Constraint原子调参快照

状态：Program-owned Candidate现在同时准备Operation Weight、Pose StateMachine Transition duration、Blend Stack policy与Inertialization rule；各对象只保存候选值，Commit前不修改当前Actor状态。Source继续准备Clip play rate Candidate，Constraint继续准备FBBIK／Foot Candidate。新增`CharacterPoseTuningSnapshot`组合三个同generation Committed View，根Runtime在打开Frame前验证它与lineage一致。

`CharacterAnimationPresentationRuntime.ApplyPendingTuning`仍在`BeginFrameTransaction`之前运行；Pose Runtime依次准备三个分区，任一失败统一Discard，全部成功后分别Commit并只发布一个新的统一Snapshot，外层随后只提升一次`m_TuningGeneration`。旧`RestoreMutableTuning`、先改StateMachine／Stack／Inertialization／Vendor再反向Apply旧Block的路径全部删除。Sequence Preview继续进入同一`Present`和同一actor-local Runtime，不复制调参对象或generation。由此任务6.9与8.1至8.6完成；3C MCP对六个Runtime触碰文件与新Snapshot类型错误筛选均为0，不单独运行回放。

## Program Frame Pages入口归还Program Runtime

状态：`CharacterPoseProgramRuntime`接管Program Frame的Begin／Commit／Discard、Evaluation页、Source Demand／Preparation、Slot与Root Warp控制、Player写Binding、Pose Value读Binding、Stage completion、Committed diagnostics和容量查询。旧`PosePlanExecutionRuntime`删除对`CharacterPoseProgramFramePages`与`CharacterPoseProgramTuningState`的直接引用，只通过Program Runtime的typed入口处理同一根Frame lease。

Frame Pages与Tuning属性改为Program Runtime私有，原调用顺序、completion identity、Pending／Committed页选择和失败清理保持不变；外层尚保留逻辑节点调度及Execution View过渡访问，所以本步不提前完成任务5.6、6.1或6.2。3C MCP对Program Runtime与旧Pose Runtime编译错误筛选均为0，不单独运行回放。

## Program Evaluation与World Context装配归还Program Runtime

状态：`CharacterPoseProgramRuntime`现在保存Prepared Evaluation、Pending／Committed Evaluation binding并直接驱动唯一持久Executor的Stage Schedule。旧`PosePlanExecutionRuntime`删除raw `CharacterPoseGraphNativeBinding`、Executor引用、Pending／Committed Frame标记、World-aware Stage扫描和Foot Placement输入装配，只在Animancer Barrier前后调用Program Runtime的typed Prepare／Complete／Mark入口。

新增唯一`CharacterPoseWorldContextAdapter`，整体接管原有Contribution展开、主导Live source选择、Clip／Timeline Foot Step曲线解析和`CharacterFootPlacementFrameInput`构造；Program Runtime只在已编译WorldAware Stage遇到Foot handle时传入自己的Pose Value只读Binding与根Body／Fact frame。没有改Foot、Pelvis、Goal、FBBIK公式、参数、Unavailable政策、Operation顺序或Physical Writer顺序。由此任务6.1完成；任务6.6仍等待把旧`CharacterPoseGraphStagedExecutor`的Operation switch整体并入正式Program执行实现，当前不提前勾选。3C MCP清空并重编后触碰文件均无编译错误，全局仅剩既有FinalIK序列化深度Import Error；不单独运行回放。

## World-aware Operation恢复唯一解释

状态：Program Runtime不再为WorldAware Stage预扫描Operation，也不再按`Operation.Code`提前组装Foot输入；它只建立一份带根Actor／Frame／Delta／Body／Fact／Completion的`CharacterPoseWorldFrameInput`并按Stage Schedule交给持久Executor。Executor在唯一Foot Operation位置读取Program自有Pose Value、调用World Context Adapter组装原业务输入，再调用Constraint typed handle一次。

旧`CharacterPoseWorldAwareStageInput`及其`HasFootPlacement`／handle对照页删除，World Context unavailable仍由同一个Constraint入口记录，Foot可用时的Contribution顺序、Live source选择、曲线采样、权重读取与Constraint调用顺序保持不变。旧Pose Runtime与Program Runtime均不再二次解释World-aware Operation，由此任务6.6完成。同时Program Runtime不再向旧Pose Runtime暴露整个Execution View或Actor State，只保留当前尚待迁移的窄节点实现访问。3C实例Domain Reload后自动重连，三个触碰文件编译错误均为0，全局仍只有既有FinalIK序列化深度Import Error；不单独运行回放。

## Program唯一Operation执行链对账与Preview状态归位

状态：全仓执行入口审计确认只有`CharacterPoseProgramRuntime.CompleteEvaluation`逐Stage调用一次持久Executor，只有该Executor逐Operation调用一次`TryCompleteOperation`并写Program Frame Pages唯一Completion页；Stage构造验证覆盖连续range与全部Operation，重复completion会直接失败。Constraint Module与Source Module均不读取Program Operation表，Diagnostics Projector只冻结Committed Result与静态显示身份，不调用Operation、Source、World Query、Foot或FBBIK。因此任务6.4与6.7完成。

Sequence Preview的player／operation选择、sample time、continuity reset与启用状态迁入Program Runtime。Preview Advance现在由Program Runtime设置同一Clip Player relevance与time，Prepare只查询Program-owned选择，Complete直接由同一持久Executor执行原Preview Operation；旧Pose Runtime只转交作者输入并遵守同一根Frame／Barrier。运行扫描顺序、Clip Player选择、Blend Space关闭、Preview时间和Output生成保持不变；任务6.2与13.9仍等待其余逻辑节点和Preview Factory收口，不提前勾选。3C实例重连后两个触碰文件编译错误均为0，全局只有既有FinalIK序列化深度错误；不单独运行回放。

## Linked Pose Fragment状态归入Program Actor

状态：新增唯一`CharacterPoseLinkedFragmentState`，由Program Image一次建立Player、StateMachine、Root Orientation Warp与Inertialization到Linked Fragment的归属表，并持有每帧Active／Reset位。`CharacterPoseActorState`保存该状态，`PoseStateAndSourceRuntime`改为消费同一对象，不再各自保存外部数组引用；旧Pose Runtime删除六组fragment数组、四套归属表构造函数和全部active／reset判断。

Linked Group incoming selection、Program Frame call control写入、Entry Fragment激活、branch replacement reset、Stack／Direct Player／StateMachine／Inertialization／Root Warp reset及帧末Clear现在都由Program Runtime按原顺序驱动。映射构造、重复归属拒绝、未归属拒绝、无Linked Fragment早退和reset completion identity保持原值；本步推进任务6.2但其余PoseState／Slot／Blend／Transition执行尚在旧外层，因此不提前勾选。3C MCP全局编译被并行Foot诊断Attribute ABI迁移阻塞，但五个本步触碰文件的错误筛选均为0；不修改或兼容外部诊断代码，也不单独运行回放。

## PoseState Advance与Finalize归还Program Runtime

状态：`CharacterPoseProgramRuntime.Advance`现在按原顺序执行Preview覆盖、PoseState Prepare、活动Stack Route release刷新、Animation Slot control发布、Blend Stack时钟推进和Clip／BlendSpace／Motion Matching source逻辑推进；Source Module只向它提供同generation的只读Source Tuning View。`FinalizePoseStateFrame`同样迁入Program Runtime，在同一Frame Pages上执行Transition求值并发布活动Root Orientation Warp control。

旧Pose Runtime的两个公开阶段入口只验证根Mutation并传递Fact、Parameter、Workspace与typed Tuning，不再遍历PoseState、Route、Stack或Root Warp，也不再写Program control页。Preview早退、Linked Fragment gating、循环顺序、Delta、Transition workspace、Root Warp Prepare时机和异常合同保持不变；任务6.2继续推进，但Player／Source准备、Action／Slot生命周期与Commit／Discard仍待迁移，因此不提前勾选。3C MCP对五个相关文件错误筛选均为0；全局外部Foot诊断ABI错误不由本change兼容，不单独运行回放。

## Program Actor帧生命周期收口

状态：Program Runtime新增Actor State Frame的Begin、Commit和分阶段Discard入口，整体接管Inertialization、Transition Route、Root Orientation Warp、PoseState Source、Blend Stack与Direct Player的帧打开、提交和回滚循环。Program Frame Commit继续保持Evaluation页、Inertialization、Frame Pages、Source Module、Actor节点、Constraint的原相对顺序；为保留Source提交夹在Program页与Actor节点之间的既有顺序，Program Runtime用typed committing lease验证后一阶段提交，不向外暴露内部对象。

旧Pose Runtime删除`BeginPendingModuleFrames`／`DiscardPendingModuleFrames`及全部节点生命周期循环，只在原Source清理位置调用Program的Node Discard与Root Warp Discard两个阶段。Evaluation、Inertialization和Frame Pages的回滚统一进入Program `DiscardFrame`，并继续聚合失败；Linked selection清理、Source usage清理、Constraint与Publication顺序不变。任务6.2与5.10继续推进但尚未闭合Action／Slot／source preparation及整体Dispose审计。3C MCP对相关Runtime文件错误筛选均为0；不处理并行诊断Attribute迁移错误，也不单独运行回放。

## Program节点完成与Reset阶段收口

状态：成功Output与Physical Publication完成后，Stack、Direct Player、Clip Player、Blend Space Player的Complete以及Route／PoseState的Native completion通知统一进入`CharacterPoseProgramRuntime.CompleteNodeEvaluation`，最后由Program内部标记唯一Pending Evaluation完成。旧Pose Runtime的Seal区段不再遍历任何Program节点。

Reset保持现有三段顺序：Program先清Evaluation／Demand并Reset Inertialization，Constraint按原位置Reset Solver；生成新completion identity后Program Reset Route与Stack，外层完成既有Source release；最后Program Reset Direct Player、PoseState与Root Warp control，再由外层释放物理Source。旧Pose Runtime删除对应节点循环，既有Release插入点、reason、completion identity和Root Warp默认control值不变。任务6.2继续推进但Source preparation与Action／Slot生命周期仍未全部迁入。3C实例Domain Reload重连后两个触碰文件错误筛选均为0；不单独运行回放。

## Motion Matching逻辑状态归还Program Runtime

状态：`CharacterPoseProgramRuntime`接管Motion Matching的PoseState Demand构造、Selection应用入口、`IPoseStateSourceSelectionSink`、Player路由、Source usage记录、History读准备和Pose Plan Completion。所需固定数组按Actor的Motion Matching Provider容量在Program Runtime构造时一次分配；旧Pose Runtime删除对应跨Barrier字段、数组、sink实现和Player／Source反查辅助方法。

独立`CharacterMotionMatchingPresentationModule`仍唯一负责查询与Selection Resolution，Program Runtime只消费typed Resolution并通过`CharacterPoseSourceModule`正式usage入口记录物理Source使用；History仍从同一Pending Player Pose读取，Foot feature、bone顺序、selection completion、pose completion和reset sequence保持不变。由此推进任务6.2与6.5，但Clip／BlendSpace／Action的物理Source准备仍在旧外层，暂不勾选。3C MCP对Program Runtime、Pose Runtime与PoseState Source错误筛选均为0；不单独运行回放。

## Source Preparation逻辑迁入Program Runtime

状态：Program Runtime接管Evaluation Workspace打开，以及Stack、Direct Player、Clip Player和Blend Space Player逐Family的Source Preparation。它按原Linked Fragment／Preview relevance、Stack entry去重和Player relevance读取逻辑节点状态，生成typed `CharacterPoseSourcePreparation`，再通过`CharacterPoseSourceModule.Prepare`唯一物理入口提交；Provider sample解析和Source owner映射也不再由旧Pose Runtime处理。

Source Module继续唯一拥有Animancer／Playable物理Source、Prepared Resources与Source Frame Result，外层仍保留现有Profiler阶段、Job binding、Final Publication binding和Writer预验证顺序。Action与Provider字典、Clip play rate、Capture binding、Preparation index、循环顺序和异常文本保持不变；任务6.2与6.5继续推进，但Job安装与Source Result闭包尚未归入Program Runtime，暂不勾选。3C MCP对两个触碰文件错误筛选均为0；不单独运行回放。

## Program Job绑定与Source结果消费收口

状态：Program Runtime现在自有Slot、Direct Player、Clip Player与Blend Space Player的Job data数组及对应Output Playable，消费Source Module发布的typed `CharacterPoseSourcePreparedResources`绑定Program Frame Player页，并在原位置处理prior-frame Source retirement staging。Final Publication仍先产生actor-local Output binding，Program随后绑定同lineage Executor并安装或更新Job，Writer预验证与Animancer Evaluate顺序未变。

Program Runtime成为这些Job／Playable的唯一Owner；旧Pose Runtime删除数组、Playable、安装、更新、移除和Stage Completed Source扫描，只在原Dispose位置要求Program先Detach，再Dispose Source Module与Program Actor State，保持既有销毁顺序。Program对Source只使用Demand／Preparation／Prepared Resources／Usage／Retirement等typed合同，对Constraint仍只由唯一Executor在Operation位置使用typed Handle和per-operation Result，因此任务6.5完成。任务6.2仍等待Action／Slot生命周期入口收口。3C MCP在补齐正式Lifecycle命名空间后两个触碰文件错误筛选均为0；不单独运行回放。

## Source Retirement生命周期归入Program Runtime

状态：Program Runtime整体接管Source Retirement State的Frame Begin／Complete／Discard、Direct／Clip／BlendSpace standalone release准备、Action／Pose pending retirement、容量预验证、提交后Apply、Reset时Stack及全部Player Source释放，以及Action Backend pending request查询与执行。它只通过Source Module的typed identity、permission、retirement、usage和completion入口操作物理Source。

旧Pose Runtime删除Source Retirement State访问器、三类Player遍历、Stack release遍历和Prepare／Apply辅助方法；根仍按原位置调用Program的分阶段入口，使Constraint、Source Frame、Program Node、Publication、Action Backend acknowledgement与Motion Matching usage的相对顺序保持不变。任务6.2与5.10继续推进但ActionPlayback／AnimationSlot根事务仍待收口。3C MCP对Program Runtime与Pose Runtime错误筛选均为0；不单独运行回放。

## Action Source路由归入Program Runtime

状态：Program Runtime接管Action Frame到`AnimationPoseSampleRequest`的构造、Animation Slot route解析、可选Selection发布、重复Source拒绝、Foot Feature携带，以及Source Pose target发布。旧Pose Runtime不再读取Program Node Runtime Index或直接操作Stack Route，只验证根Mutation并携带当前Program Frame lease调用Program入口。

Action source id、presentation request sequence、sample time、clip、parameter page、pose parameter、Foot Feature、Slot route与Selection发布顺序保持不变；Retained Action继续只登记Source而不推送Selection。任务6.2继续推进，但Action Playback与Animation Slot的根Frame生命周期仍在外层，暂不勾选。3C MCP对Program Runtime与Pose Runtime错误筛选均为0；不单独运行回放。

## Action Playback与Animation Slot逻辑帧归入Program Runtime

状态：Program Runtime现在持有Action Playback与Animation Slot的唯一Pending事务，按原顺序接收Action command、消费Backend release completion、推进Lifecycle、仲裁Slot winner、生成Action source、发布Slot usage与Retirement permission、准备Backend release request并提交或回滚。根Frame Transaction删除Action事务、Slot lease与Backend completion副本；外层Runtime只保留外部command校验、Action采样器调用时机、Module阶段顺序和诊断结果发布。

Action inbox读取、Lifecycle／sample history mutation、Slot selection generation、Current／Outgoing／Retained选择、Source请求序号、release acknowledgement、Retired列表排序及Commit／Discard顺序均保持原值。PoseStateMachine、全部Player、ActionPlaybackInput、AnimationSlot、BlendStack、Transition、Inertialization与Linked Fragment的逻辑执行现均只由Program Runtime驱动，因此任务6.2完成；Action采样物理适配仍随Source Module边界和根事务收窄继续处理。3C MCP对Program Runtime、Pose Runtime、根Runtime与根Transaction错误筛选均为0；不单独运行回放。

## 收紧Action与Slot实现可见性

状态：Action Playback事务、Lifecycle Runtime、Animation Slot frame plan、source plan、mutation lease与Runtime类型全部降为程序集内部实现；外部仍只通过既有`IActionPlaybackCommandPublisher`发布command，不能取得内部事务、Slot pending state或直接推进Lifecycle。Program Runtime保持这些实现的唯一调用者，Action采样只在Program入口内读取同一活动事务。

本步不改任何状态、容量、顺序或结果，只删除可形成第二逻辑Owner的公开入口。3C MCP对两个触碰文件错误筛选均为0；任务6.10仍等待Operation／Value全局唯一性审计，不提前勾选，也不单独运行回放。

## Node Definition与统一Port Shape作者链

状态：29个正式`CharacterPoseNodeKind`由唯一`CharacterPoseNodeDefinitionModule`显式登记Definition；重复Kind、重复Payload、缺失Definition、Capability缺失或重复Capability在目录封口时失败。Definition统一提供Payload、字段、固定／条件／动态端口、Graph Role、Execution Domain、Operation Family、Graph dependency、局部／Rig校验、typed lowering、Source Map命名、Canvas创建与Clipboard政策。

Pose Capability改为由Definition投影到共享`GraphAuthoringCapabilityCatalog`；旧Handler Registry、逐节点`compilerBindingId`和Motion Matching第二Capability注册删除。统一Port Shape同时供Canvas、Connection、Clipboard、typed Mutation、Document strict parser、Target Mapper、Reconciler、Mutation preflight与Validator消费，并在字段改变条件端口时验证全部既有edge两端的存在性、方向与value type。Profile Inspector的Pose Source consumer扫描也只读取Definition dependency与能力，不再维护另一套NodeKind分支。

Definition Module不引用Document Store、Reconciler、Mutation Service、Undo、SaveAssets、MCP或事务类型；Agent可见字段、端口、role、identity和JSON schema没有改变。任务10.1至10.8与10.11完成；10.9仍等待旧Compiler Handler实现删除，10.10仍等待Compiler侧剩余NodeKind分支随不可变Pass收口。3C MCP对Definition、Authoring、Document、Mutation、Clipboard、Profile与Compiler接线文件错误筛选均为0；不单独运行回放。

## 唯一Pose Compilation Request／Result入口

状态：新增`CharacterPoseCompilationRequest`、`CharacterPoseCompilationResult`、固定Pass枚举与结构化`CharacterPoseCompilationDiagnostic`。Diagnostic携带Pass、severity、stable reason、Graph／Node／Port、call-site、source path与related identity；Projection Compiler只把结构化错误格式化到既有Build错误集合，不再从异常文本反推Node位置。

旧参数转发Compiler入口和`CharacterPoseNativePlanBuilder`命名删除。`CharacterPresentationProjectionCompiler`构造唯一typed Request并直接调用`CharacterPoseCompilerModule.Compile`，Module只返回Program Image或结构化失败，不保留第二Compiler facade、旧入口别名或兼容分派。任务11.1与11.12完成；中央Compilation State、旧Handler实现和其余独立Pass仍待后续删除。与Node Definition整合后的3C MCP编译错误筛选均为0，`git diff --check`与本change严格OpenSpec校验通过；不单独运行回放。

## Graph Closure不可变Pass

状态：新增独立`CharacterPoseGraphClosurePass`，先为每个Graph owner建立一次flat catalog，再只从root、Node Definition投影的State Pose／Subgraph／Motion Matching Entry依赖和Linked Pose selector候选展开唯一闭包。闭包按`owner identity + graph id`保存不可变Entry，递归路径、缺失Graph、重复catalog identity、缺失Linked Group／Interface／Selector／Implementation／Entry owner均发布`GraphClosure`结构化Diagnostic，失败时不进入Topology或Lowering。

旧Compiler递归Lowering删除自己的Graph call stack与动态`RequireGraph`发现；StateMachine、Subgraph和Linked Pose Entry只读取前置闭包中已证明的Graph，并保持原递归Lowering顺序、scope、call chain、Graph dependency hash输入和最终Program Image不变。任务11.2完成；Topology Validator当前仍保留自身图内校验，待11.4改为消费同一闭包后删除重复递归判断。3C MCP对新Pass与Compiler入口错误筛选均为0；不单独运行回放。

## Typed Lowering不可变Pass

状态：新增`CharacterPoseTypedLoweringPass`，对Graph Closure中的每个Graph一次建立只读authoring node索引、incoming typed edge页和typed IR node页。每个节点只通过唯一Node Definition执行Payload／Rig校验与Lower；Capability identity、source path、typed input link、目标／来源Port和Value Kind在该Pass内冻结，失败发布带Graph／Node／Port／source path的`TypedLowering`结构化Diagnostic。

旧`CharacterPoseIrCompiler`不再读取authoring Payload或调用Handler／Definition Lower，改为只消费已冻结的`CharacterPoseTypedIrGraph`并保留原确定性拓扑顺序与Graph Role边界选择。全仓实际Node Lower调用只剩Typed Lowering Pass到Node Definition这一处，任务11.3完成；拓扑排序、Graph Role、唯一Output与写冲突仍待11.4整体迁入独立Topology Pass。3C MCP强制Asset刷新后对新Pass、IR与Compiler入口错误筛选均为0；不单独运行回放。

## Topology不可变Pass

状态：新增`CharacterPoseTopologyPass`与只读Topology Catalog。Graph Closure现在同时冻结每条Node Definition dependency reference；Topology据此为root、State-local、Subgraph、Motion Matching Entry与Linked Pose Entry分配明确Graph Role，并对同一typed IR按Role生成确定性有序IR，不再在递归Lowering过程中临时排序或决定边界。

Compiler路径改为逐闭包Graph执行局部Topology验证，typed edge、Port方向／Value Kind、Pose Space、required input、fan-in、Graph Role、Output／Graph boundary、Goal Contribution／Goal Set／Assembler／FBBIK闭包、Motion Matching history、Root Warp、可达性、环与写入归属仍沿原规则验证；Program级额外固定唯一root Final Publication boundary、Goal Assembler和FBBIK。State／Subgraph／Motion Matching引用只通过Graph Closure resolver读取，Topology不再递归发现或验证Graph cycle；root flat catalog的可达性与单Owner计数也前移到Closure Pass。具体Physical Writer没有进入Compiler，继续只由Runtime Factory和Final Publication构造证明唯一。

任务11.4完成。原Graph Validator的公开作者校验入口仍保留自身递归诊断，但Compiler只调用`ValidateClosedGraph`的非递归路径；3C MCP对Closure、Topology、Validator、IR与Compiler入口错误筛选均为0，不单独运行回放。

## Stage Schedule独立Pass入口

状态：新增`CharacterPoseStageSchedulePass`，从当前唯一线性Operation序列按Execution Domain、Output Pose Space和Linked Fragment identity生成固定连续Stage，并独立计算Native Operation range、Pose Value range与Linked Fragment Stage range。Pass构造时证明Stage从Operation 0连续覆盖到末尾，每个Operation恰好落入一个Stage；Fragment必须拥有完整且隔离的Stage范围，否则编译失败。

旧Compiler内联`CompileStages`与原地扫描Fragment范围删除；Compiler只在Pass完成后把已计算range绑定到最终Fragment Payload，Stage分组、顺序、Native计数和Program hash输入保持不变。任务11.6仍等待Symbolic Family Lowering产出的typed dependency直接成为此Pass输入，当前不提前勾选。3C MCP对新Pass与Compiler入口错误筛选均为0；不单独运行回放。

## Value Lifetime与Workspace Plan分离

状态：新增`CharacterPoseValueLifetimePass`，只消费固定Stage Schedule和唯一线性Operation页，冻结Pose、Parameter address、Pose Discontinuity、Goal Contribution与Goal Set的producer／last-use页；同时验证每个typed输入只能读取更早的唯一producer、Goal值必须有consumer、Linked Fragment输出必须在Call前完成、最终Output只延长Final Pose寿命而不分配第二Final页。Parameter使用固定schema地址，Discontinuity与对应Pose Value共享同一寿命。

旧Compiler中的producer／last-use临时数组和六组Register辅助方法删除。`CharacterPoseWorkspacePlanPass`开始独立消费Value Lifetime、Player／Blend Stack容量与Parameter schema，保持现有Pose workspace排除唯一Final Output、Contribution stride总量、Parameter stride和Frame cache数值不变。任务11.7完成；11.8仍等待把Rig、节点状态、Source、Constraint、Inertialization与Diagnostics manifest的全部容量收进同一Workspace Plan。3C MCP对新Pass与Compiler入口错误筛选均为0；不单独运行回放。

## 完整Workspace Plan

状态：`CharacterPoseWorkspacePlanPass`现在一次冻结Rig Pose／Physical／Virtual Bone布局，Pose／Parameter／Contribution／Frame cache，Player、Inertialization、StateMachine state／transition、Blend Stack entry、Source catalog、Constraint operation／goal以及Diagnostics stage／operation的全部固定容量。Pass只消费前置Schedule、Value Lifetime、Rig和已编译节点描述，不读取Runtime、Actor或动态资源；任一计数不一致在Program Image发布前失败。

现有Program Image参数仍取同一Plan中的Pose workspace、Parameter stride、Contribution capacity和Frame cache，所以生成数值与Hash输入不变；其余容量先作为后续Family Payload与ABI绑定的唯一计划真相，不再由中央Compiler临时推算。任务11.8完成。3C MCP对Workspace Plan与Compiler入口错误筛选均为0；不单独运行回放。

## Symbolic Family Lowering与Stage依赖闭包

状态：新增`CharacterPoseSymbolicFamilyLoweringPass`，沿Topology Catalog的确定顺序展开root、State、Subgraph和全部Linked Pose candidate Fragment。每个可执行节点只从Node Definition取得唯一Operation Family与Execution Domain，并冻结symbolic typed input／output identity、Pose Space、跨帧Actor State需求、Program Frame页需求、Workspace需求、Graph Role、source path和稳定Fragment identity；Value只使用符号端点，不分配Operation、Pose、Parameter、Goal、Player或Workspace物理index。

中央物理绑定每生成一个Operation必须按序消费并精确匹配同一个Symbolic Operation的Node、Kind、Code、Family、Domain、Pose Space与Fragment identity；多出、缺失或重排都会在Program Image发布前失败，因此旧绑定不再发现第二套Operation序列。State Graph与Linked candidate的隐藏Pose依赖也显式进入Symbolic输入。

Stage Schedule改为以Symbolic Program作为分组与依赖真相，只用已绑定Operation读取最终Pose workspace range。它验证除明确Pose History时间边外的每个typed输入都已有更早唯一producer，再按Symbolic Execution Domain、Output Space和Fragment identity分段，并继续证明全部Operation恰好一次。任务11.5与11.6完成。3C MCP对Symbolic Pass、Stage Pass与Compiler入口错误筛选均为0；不单独运行回放。

## Presentation Workspace归入Program Runtime

状态：`PresentationFrameWorkspace`及其双页lease从根Runtime与根Frame Transaction迁入Program Runtime。Program在原位置先打开Workspace再打开Action lifecycle，后续由同一Owner处理Action sample frame落页、Slot usage／retirement、Provider demand、Motion Matching selection与Source Demand读取；根只按原阶段调用Program，不再取得Workspace实例或页索引。

Commit仍严格保持Workspace、Action Sampling、Slot、Action Playback、Motion Matching、Program／Source／Constraint的原顺序；Discard仍保持Program、Motion Matching、Slot、Sampling、Action、Workspace顺序。Reset和Begin失败清理也保留原相对位置。任务5.6继续推进，但根事务仍暂存Action Sampling与Motion Matching内部lease，暂不勾选。3C MCP对Program、Pose协调层、根Runtime与根Transaction错误筛选均为0；不单独运行回放。

## Action Sampling归入Source Module

状态：`ActionPresentationSamplingRuntime`及其Pending transaction从根Runtime和根Frame Transaction迁入`CharacterPoseSourceModule`。Source Module在原阶段打开、投影、解析、验证、提交或丢弃Action sample页；Program Runtime只把自己的活动Action lifecycle事务和Program-owned Presentation Workspace交给Source入口，根不再取得采样Runtime或lease。

Action sample window、presentation tick、delta、Interpolation／Retention投影、Foot Feature解析、诊断冻结、Commit／Discard／Reset相对顺序和容量均保持不变。由此任务4.2中有限Action sample Adapter的实际Owner与文档一致；任务5.6仅剩Motion Matching内部lease仍暴露给根事务，暂不勾选。3C MCP对Source、Program、Pose协调层、根Runtime与根Transaction错误筛选均为0；不单独运行回放。

## Motion Matching归入Source组合模块

状态：新增`CharacterPoseMotionMatchingSourceRuntime`，由`CharacterPoseSourceModule`唯一持有Motion Matching查询模块及其Pending lease。独立组合模块负责Trajectory／Preview输入、Demand Resolution、Completion、Commit／Discard、Reset与提交后诊断；Program Runtime持有固定容量Provider sample页并消费Selection，Pose协调层只负责Program与Source之间的typed阶段编排。

根Runtime删除Motion Matching模块、Provider sample字典和容量，根`CharacterPoseFrameTransaction`也删除Motion Matching lease及其索引。Begin、Resolve、Completion、Commit、Discard、Reset和诊断发布仍位于原阶段，Action／Workspace／Slot／Program／Source／Constraint／Publication相对顺序未变。任务5.6完成；3C MCP强制刷新后对新增组合模块、Source、Program、Pose协调层、根Runtime与根Transaction错误筛选均为0；不单独运行回放。

## 删除Pose Compiler Handler层

状态：`CharacterPoseNodeDefinition`改为真正的抽象定义合同，typed泛型Definition直接实现Payload创建／字段读取、局部与Rig校验、Graph dependency、Source Map和Lowering；Definition Module直接登记29个具体Definition，不再先登记Handler再包装一次。Full Body IK、Linked Pose与Motion Matching定义文件同步改名，旧`ICharacterPoseCompilerHandler`、泛型Handler和Handler命名全部删除。

Player、Action Input、Slot、Blend、StateMachine、Inertialization、Additive、Component Control与Clip需求由单一`CharacterPoseNodeRuntimeRequirement`位集表达，Compiler与Symbolic Lowering只消费该typed需求，不再读取逐项布尔矩阵。任务10.9完成；3C MCP强制刷新后所有触碰文件错误筛选均为0，当前唯一全局编译错误来自范围外ASP Local Integration；不单独运行回放。

## 拆出Family Payload绑定Pass入口

状态：唯一`CharacterPoseCompilerModule`现只编排Capability、Graph Closure、Typed Lowering、Topology与Symbolic Family Lowering，并把后续物理绑定交给独立`CharacterPoseFamilyPayloadBindingPass`。原2463行中央Compiler文件整体改名为该Pass实现，新建的小型Compiler入口不再持有Binding字段、递归Graph lowering或Payload索引分配逻辑。

本步只移动唯一调用边界，输入Request、前置Pass结果、异常归属、Operation顺序、物理索引、Hash和Program Image输出不变。11.9暂不勾选：Stage／Lifetime／Workspace与Image Seal仍在Binding Pass尾部，下一步继续拆成明确Result与Seal Pass；3C MCP对Compiler入口与Binding Pass错误筛选均为0，不单独运行回放。

## 分离Family Binding Result与Program Image Seal

状态：Family Payload Binding Pass现在只递归消费Topology与Symbolic Operation序列，产出`CharacterPoseFamilyPayloadBinding`。Result把Parameter／Blend／Constraint／Player／StateMachine／Slot／Linked Pose等已绑定Family页、唯一Operation序列、Source Map、Graph dependency和物理布局计数分开保存；原中央`CompilationState`改为Pass私有`BindingBuilder`，不再跨Pass流动。

Compiler入口随后按固定顺序运行Stage Schedule、Value Lifetime和Workspace Plan，并由独立`CharacterPoseProgramImageSealPass`绑定Linked Fragment stage range、计算原样Hash并唯一构造Program Image。原Binding Pass中的Hash与Image构造已删除，所有Hash token、数组顺序、Workspace数值和构造参数保持不变。11.9仍等待把万能Operation改为分Family typed payload handle后闭合；3C MCP对Compiler、Binding Result／Pass与Seal Pass错误筛选均为0，不单独运行回放。

## 收口Constraint Runtime销毁所有权

状态：`PosePlanExecutionRuntime`成为`CharacterPoseConstraintRuntime`的唯一生命周期Owner，并在原根销毁时机负责Dispose；根`CharacterAnimationPresentationRuntime`删除Constraint字段、转发属性、构造失败清理和第二次Dispose。Pose协调层内部仍在相同位置调用Constraint Frame，业务输入和执行顺序不变。

销毁顺序保持Source／Motion Matching、Program、Graph Clock、Constraint，任一前置Dispose失败仍继续清理Constraint并汇总原异常。任务5.10继续推进，尚需对账Projection replacement、Preview与Fault；3C MCP对Pose协调层和根Runtime错误筛选均为0，不单独运行回放。

## 拆出只读Committed Diagnostics组合模块

状态：新增`CharacterPoseDiagnosticsRuntime`，唯一持有Runtime Snapshot Publisher、Foot committed capture projector、Actor projector与Program projector。它只在根Frame已Seal后接收同lineage的Program／Source／Constraint／Publication Result，完成验证、只读投影、短租约发布与interest生命周期；Pose协调层删除四个诊断字段和全部Snapshot／Foot View内部装配逻辑。

Frame Begin仍只清理Actor projector的帧内投影页，Discard、Reset、Invalidate、Dispose和Post-Commit发布顺序保持不变；无interest时仍在原位置直接返回，不查询世界、不执行Constraint、不读取Physical Transform反推结果，也不新增每帧分配。Foot committed事件继续通过现有partial边界发布，通用采样器、Foot插件与Performance不进入该模块。3C MCP对Diagnostics组合模块与Pose协调层错误筛选均为0；不单独运行回放。

## 建立Pose Runtime组合工厂

状态：新增`CharacterPoseRuntimeCompositionFactory`，在唯一构造入口完整装配Execution View、Program Frame Pages、Tuning、Player／Stack／Route Actor State、Source、Constraint、Final Publication与Diagnostics；`CharacterPoseRuntimeComposition`保存五个正式模块并按原顺序统一销毁。Pose协调层删除350余行具体节点和资源构造、Source容量扫描、Blend Stack Operation查找及逐类型失败清理，只保留组合请求和Frame阶段。

正式Runtime与Preview仍调用同一个`PosePlanExecutionRuntime`构造，因此自动共享该组合工厂；Animator准入、初始Frame layout、Player索引、Slot control、Source容量、Graph Pause、初始Frame discard及正常Dispose顺序不变。构造失败清理继续覆盖Source、全部Player、Diagnostics、Constraint、Execution View、Inertialization、Tuning与Frame Pages，并补齐Graph已Pause后的恢复。任务5.10与14.1继续推进，等待Preview根事务与最终类型替换一起闭合；3C MCP对组合与协调层错误筛选均为0，不单独运行回放。

## 拆出Executor执行上下文与Composition Family模块

状态：旧Staged Executor的Program常量、Actor控制、Inertialization、Slot、Value、Completion与Frame binding字段，以及通用Value复制／缩放／参数／Contribution／Foot Feature／空间数学／Invalid传播原语，整体迁入`CharacterPoseExecutionContext`。Executor只继承同一上下文，不再重复声明或持有第二份页引用。

新增`CharacterPoseCompositionOperationModule`并迁移Blend Pose、Layered Bone Blend、Additive Pose与Pose Parameter Resolve四类实际执行；唯一Operation dispatch在原Stage位置调用该组合模块，使用相同Context页和原辅助原语，没有委托分配、第二执行路径或结果复制。6.8继续推进，待其余Family迁出后直接删除旧Staged Executor类型；3C MCP对Context、Composition模块和Executor错误筛选均为0，不单独运行回放。

## 拆出Transform Operation模块

状态：新增`CharacterPoseTransformOperationModule`，整体迁移Modify Bone、Root Orientation Warp、Local-to-Component、Component-to-Local及其Component descendant重建逻辑。模块直接消费同一`CharacterPoseExecutionContext`的已绑定Value与Rig parent页，仍由唯一Stage dispatch在原Operation位置调用。

Bone遍历顺序、Local／Component数学、Root yaw乘法顺序、Invalid reason、continuity和完成页写入均未改变；没有复制Pose页、Transform写入或额外空间转换。旧Executor进一步缩减但仍保留Player、State、Inertialization、Constraint、Linked与Output Family，6.8暂不勾选；3C MCP对Transform模块与Executor错误筛选均为0，不单独运行回放。

## 拆出Constraint Operation模块

状态：新增`CharacterPoseConstraintOperationModule`，迁移Pose Bone Goal Contribution、Foot Placement、Goal Assembler与Full Body IK四类Operation调用。模块只从共享Execution Context取得当前Value read/write binding、Frame／Completion identity和唯一Constraint Runtime，并返回原typed per-operation Result匹配结果。

Foot world input、Goal workspace、FBBIK输出Pose、invalid reason、completion校验和Constraint调用次数均保持不变；模块不拥有Constraint Pending页、不扫描Program，也不复制Goal或Pose。旧Executor只保留原Stage dispatch，6.8继续推进；3C MCP对Constraint Operation模块与Executor错误筛选均为0，不单独运行回放。

## 拆出Linked与Output Operation模块

状态：新增`CharacterPoseLinkedOperationModule`与`CharacterPoseOutputOperationModule`。Linked模块唯一消费编译后的Call／Candidate、活动Fragment页和Call control，把选中Fragment的Pose与discontinuity写回同一Value页；Output模块唯一完成最终输入检查、deep validation和Final Publication binding写入。

Stage对非活动Fragment的跳过仍查询同一个Linked模块，candidate范围、generation合并、Branch Replacement discontinuity、Final invalid保持及Output continuity顺序均未改变。两个模块不分配第二Value或Final页，旧Executor只做调度；6.8继续推进，3C MCP对Linked、Output模块与Executor错误筛选均为0，不单独运行回放。

## 拆出Player与Inertialization Operation模块

状态：新增`CharacterPosePlayerOperationModule`，迁移Selected／Clip／Blend Space／Blend Stack输入投影和完整Animation Slot合成；新增`CharacterPoseInertializationOperationModule`，迁移普通Inertialization与Slot共享的History、Residual、Envelope、Parameter、Foot Feature和Commit逻辑。Player模块只组合调用同一Inertialization模块，不复制算法。

Sequence Preview也改为调用唯一Player模块；Player/Slot source range、Contribution、Foot Feature、continuity、selection policy、Inertial rule、History写入、delta与completion顺序保持不变。旧Executor从2598行降到1535行，只余Stage协调、StateMachine和静态配置验证；6.8继续推进，3C MCP对Player、Inertialization模块和Executor错误筛选均为0，不单独运行回放。

## 拆出State Operation模块

状态：新增`CharacterPoseStateOperationModule`，迁移State Pose Output、Pose StateMachine、Standard Blend、Parameter／Contribution／Foot Feature合并和Prediction传递。模块直接读取同一State control与Value页，仍由唯一Stage dispatch按原Operation顺序调用。

State selection、source／target availability、curve与profile采样、bone顺序、continuity、discontinuity、Contribution去重和Foot prediction权重均未改变。旧Executor只剩Frame绑定、Stage调度、完成页和静态配置验证，已降到1036行；下一步直接替换旧类型并完成6.8。3C MCP对State模块与Executor错误筛选均为0，不单独运行回放。

## 删除旧Staged Executor

状态：旧`CharacterPoseGraphStagedExecutor`类型与文件已直接删除，替换为749行`CharacterPoseProgramExecutor`。新Executor只绑定Execution Context、组合有限Family模块、按编译Stage顺序dispatch一次Operation并写Completion；原近300行构造期全量配置验证独立为`CharacterPoseProgramExecutorConfiguration`，不参与普通帧。

Program Runtime是新Executor的唯一Owner，入口同步改为`BeginEvaluation／ExecuteStage／CompleteEvaluation`，不存在旧类型wrapper、第二dispatch或兼容路径。Program／Frame／Value巨型字段属于共享Context，Player、State、Inertialization、Composition、Transform、Constraint、Linked和Output实际执行属于各组合模块。任务6.8完成；3C MCP对Executor、Configuration与Program Runtime错误筛选均为0，不单独运行回放。

## 提升Operation Family为Runtime ABI事实

状态：`CharacterPoseOperationFamily`从Editor Node Definition实现移入Runtime Program Image合同，Definition、Symbolic Lowering、Family Binding和后续Runtime Payload共用同一有限枚举。数值与29个Node Kind映射不变；本步不新增Payload页或第二Operation结构，12.1随分段ABI继续推进。3C MCP对Program Image与Definition错误筛选均为0，不单独运行回放。

## Runtime唯一执行Owner审计

状态：全仓运行时代码搜索确认Action Playback与Animation Slot只在根构造一次并由Program Actor State持有唯一Pending事务；Pose Operation只有`CharacterPoseProgramExecutor.ExecuteStage`一处按Stage dispatch，各Family模块没有第二Stage循环；Sequence Preview也调用同一Player模块。Value与Final Pose分别只写Program Frame Pages的当前Value页和Final Publication的唯一Pending页。

`AnimationPoseRequestWorkspaceLayoutFactory`与Runtime组合工厂中的Operation switch只在Runtime创建前计算固定容量，不执行Pose、不打开Frame页也不写Value；Diagnostics只读Committed Result。未发现第二Action lifecycle Owner、第二Operation执行Owner、第二Value writer或图外隐式Pose stage，任务6.10完成。分段ABI仍会删除这些构造期万能Operation扫描，但不影响本项执行唯一性结论。

## 分离编译期Operation绑定结果

状态：新增Editor内部`CharacterPoseBoundOperation`作为Bind Family Payload Pass的唯一物理绑定结果。Stage Schedule、Value Lifetime与Workspace Plan只读取该编译期结果；`CharacterPresentationPoseOperation`不再穿过多个编译Pass，只在Seal Program Image边界由逐字段映射一次生成当前Runtime ABI。

本步没有新增运行时读取路径，也没有改变字段值、Operation顺序、Hash输入或Program Image序列化结果；它把后续分段Header／Family Payload替换限制在Seal边界，避免Runtime万能记录继续反向约束前置Pass。3C MCP完成脚本重编，相关`CharacterPose`错误筛选仅剩既有Import Error；不单独运行回放。

## 固定Operation Code与Family对应关系

状态：Runtime ABI新增唯一`CharacterPoseOperationFamilies.RequireFamily`，覆盖全部现行Operation Code。编译期Bound Operation显式携带Symbolic Family并立即核对Code／Family；Stage Schedule同时核对Symbolic与Bound结果。State Graph内部合成的`StatePoseOutput`明确归入StateMachine Family，不再沿用作者Output节点的Final Output Family。

本步只固定后续Header与Payload页的归属事实，没有改变现有Program Image字段、Hash、Stage顺序或Runtime执行数据。3C MCP完成脚本重编，相关`CharacterPose`错误筛选仅剩既有Import Error；不单独运行回放。

## 原子替换序列化Operation ABI

状态：`CharacterPresentationPoseOperation`万能记录已从Runtime合同删除。Program Image v24／Runtime ABI v27改为`CharacterPoseOperationHeader`、单一typed Value Reference表和18个固定Family Payload页；Header只保存调度、Family索引、输入／输出range、Linked Fragment归属和公共Weight。Parameter、Pose、Action Control、Goal Contribution与Goal Set全部通过typed引用表达，不再以成组`-1`字段表达缺席。

Bind Family Payload阶段一次生成全部Header、typed引用与Family页；Seal阶段只计算包含Family／Source／Policy在内的完整Image Hash并封口。Runtime构造、Linked／Motion Matching计划、调参、Diagnostics与Editor Preview均改读Header和自身Family Payload；Execution View仍在唯一构造边界逐值materialize当前Native执行数据，没有第二序列化reader或旧schema fallback。任务11.9、12.1、12.2与12.4完成。

3C MCP脚本重编无C#错误。正式Character Build已从唯一显式入口执行，但被现有Foot Analysis geometry validation identity stale拒绝，未写入v24 generated Projection；本change没有绕过或修改Foot数据。Native万能Operation的Family化与生成资产重建继续作为后续原子步骤，不单独运行回放。

## 删除Native万能Operation镜像

状态：`AnimationPoseGraphNativeOperation`已删除。Execution View现在只materialize有限`CharacterPoseNativeOperationHeader`和Parameter Resolve、Player、StateMachine、AnimationSlot、Blend、Inertialization、Composition、Space Conversion、Component Control、Goal Contribution、Goal Assembler、FullBodyIK、Linked Pose、Output固定Native页；Header只负责Stage dispatch、Family索引、Output Pose、Fragment归属、Completion与公共Weight。

Program Executor按Header的Family Payload index取得唯一typed页后调用现有Family模块；各模块签名改为Header加自身Payload，Execution Context的合成辅助入口只接收实际所需Weight、Mask、Policy或Value参数。旧万能Native字段、`WithWeight`整记录复制、State blend临时万能记录和跨Family`-1`校验矩阵全部删除；Stage顺序、Value地址、Weight覆盖、Constraint handle、Slot合成、Inertialization及Final Publication调用位置不变。任务5.1至5.3、11.10、11.11、12.3、12.6至12.8完成。

3C MCP完成全脚本重编且C#错误为0。generated Projection仍只等待既有Foot Analysis身份恢复后从正式Character Build入口重建；本步不回放。

## 分离Program Evaluation状态页

状态：新增`CharacterPoseProgramEvaluationState`，唯一持有Prepared、Pending Completed与Committed Evaluation binding。Prepare／Consume、Mark Completed、Commit、Discard Pending、Reset和Committed Diagnostics读取全部通过该状态页完成；`CharacterPoseProgramRuntime`删除三份binding字段、两份布尔状态和内嵌Prepared页实现。

帧内Prepare消费、Stage完成确认、提交前验证、Motion Matching history读取、Committed Diagnostics和Reset／Dispose顺序保持原样；失败Discard只清Pending，成功Commit才提升Committed。Dense跨帧Evaluation现在有明确Committed／Pending所有权，Source Retirement继续使用既有固定pending state与journal，任务5.7完成。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 拆出Program Motion Matching模块

状态：新增`CharacterPoseProgramMotionMatchingRuntime`，接管Provider Source Sample表、Demand／Selection、source usage、history read、Pose Plan Completion以及全部pending completion字段，并成为唯一`IPoseStateSourceSelectionSink`。`CharacterPoseProgramRuntime`只保留根Frame／Workspace lease验证和窄入口转交，不再持有Motion Matching数组、Dictionary或完成状态。

Selection顺序、Stack／Direct Player usage扫描、history Bone校验、Pending Player Pose读取、Foot Feature携带、Source usage completion和Reset清理顺序保持不变；没有修改搜索、评分或Source选择逻辑。Program Runtime由3129行降至2738行。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 拆出Program Tuning模块

状态：新增`CharacterPoseProgramTuningRuntime`，整体拥有Program Tuning State，并统一协调Pose StateMachine、Blend Stack、Inertialization与Operation Weight的Candidate Prepare／Commit／Discard。Program Runtime只保留同名typed入口，不再解释各Owner的调参提交顺序。

Candidate失败回收、全部成功后的提交顺序、generation验证和Dispose所有权保持不变；没有增加回滚Apply或第二Snapshot。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 拆出Program Action与Slot模块

状态：新增`CharacterPoseProgramActionRuntime`，整体拥有Action Playback事务、Animation Slot mutation lease、Presentation Workspace lease、backend／slot release页、Retired Playback页和Slot Frame Plan。Action命令、生命周期解析、采样、Source发布、Release Protocol、三类提交／回滚与Reset都进入该模块；Program Runtime只验证自己的根Frame lease并转交frame identity与业务输入。

Action／Slot／Workspace打开与关闭顺序、Backend acknowledgement、request sequence、Source采样键、route选择、retirement permission和已退休列表保持不变；Motion Matching仍复用同一Workspace lease，没有复制第二事务或第二Source路径。Program Runtime由2738行降至2252行。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 旧Pose执行类型删除审计

状态：全仓运行时与Editor源码搜索确认`CharacterPoseGraphNativeProgram`、`CharacterPoseGraphStagedExecutor`、`CharacterPresentationPoseOperation`、`AnimationPoseGraphNativeOperation`、`ICharacterPoseCompilerHandler`、Handler Registry和中央`CompilationState`类型均已删除；命中只存在于OpenSpec迁移历史与基线说明，不存在可编译实现、wrapper或Runtime reader。任务14.2完成。

## 拆出根Pose调参协调器

状态：新增`CharacterPoseTuningCoordinator`，唯一协调Program、Source与Constraint三个分区的Candidate Prepare／Commit／Discard并持有统一Committed Snapshot。旧Pose外层删除分区调参顺序和Snapshot构造，只在新Frame打开时读取协调器的同generation Committed结果。

Source先准备、Program次之、Constraint最后准备以及Program／Source／Constraint提交顺序保持不变；任一失败仍只Discard已经准备的Candidate，不修改Committed状态。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 拆出根Pose Frame Coordinator

状态：新增`CharacterPoseFrameCoordinator`，唯一持有Completion计数、活动Program lease、Commit验证位、Pending Outcome与Frame completion context，并接管Begin、Source Demand、Source Prepare、Animancer Evaluate Barrier、Program／Constraint／Publication typed结果、Seal、Discard、Committed Finalize和Pose State推进。旧Pose外层不再保存或修改任何根帧状态，只保留当前上层API转交。

Begin／Prepare／Evaluate／Final Write／Node Complete／Seal顺序、Barrier失败清理、Writer后提交政策、Reset时completion递增和Profiler Marker名称保持不变；没有复制第二Frame事务、Workspace或Final页。旧Pose外层由1417行降至863行。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 拆出根Motion Matching协调器

状态：新增`CharacterPoseMotionMatchingCoordinator`，统一连接Program-owned Demand／Selection／Pose Completion与Source-owned查询、Frame Completion、Replay capture、Trajectory intent、Preview query及Diagnostics发布。旧Pose外层不再解释Motion Matching frame work或在Program与Source之间拼接completion。

Demand生成、HasFrameWork、Resolve、Selection应用、Pose completion准备与最终Source completion顺序保持不变；Reset与可选模块Unavailable语义未变，也没有接管搜索内部算法。3C MCP全脚本重编且C#错误为0，不单独运行回放。

## 分离Committed诊断事件发布边界

状态：新增`CharacterPoseCommittedDiagnosticsEventPublisher`，只持有Runtime identity，并在调用时接收同一Committed lineage的Program、Constraint与Final Publication事实。`CharacterPoseDiagnosticsRuntime`直接拥有该发布器；旧Pose外层不再实现诊断Event Sink接口，也不再通过partial方法读取Constraint、Publication或Player内部状态。

Foot诊断事件identity、interest查询、字段来源、发布时机和最终Pose对应的Foot Motion解析顺序保持不变；未修改生成采样框架、Foot字段或IK算法。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 删除旧Pose Plan执行外层

状态：`PosePlanExecutionRuntime`及其meta已删除。唯一`CharacterAnimationPresentationRuntime`现在直接装配`CharacterPoseRuntimeComposition`、Tuning Coordinator、Frame Coordinator和Motion Matching Coordinator，并只按固定根帧顺序调用Program、Source、Constraint、Final Publication与Diagnostics模块；不再经过第二个同构外层转发Action、Frame、Reset、Diagnostics或Source生命周期。

旧外层的存活检查由公开Runtime保留，根Frame lease检查由Frame Coordinator保留，Program Action的frame检查仍由Program Action模块保留；Reset、提交、丢弃、Evaluate Barrier、物理发布与释放顺序逐项原样迁入唯一根协调链。全仓可编译源码已无`PosePlanExecutionRuntime`及旧Event Sink接口，任务14.1完成。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 拆出Program Source准备与退休模块

状态：新增`CharacterPoseProgramSourcePreparationRuntime`与`CharacterPoseProgramSourceRetirementRuntime`。前者唯一持有Clip／BlendSpace／Direct／Slot采样Job和Playable安装状态，并接管Sequence Preview、Source Preparation与Job绑定；后者唯一解释Player／Stack的待退休Source、物理Source permission、Action backend release和Committed后释放。`CharacterPoseProgramRuntime`只保留根Program lease验证与两个模块的窄调用。

Stack、Direct、Clip、BlendSpace的准备顺序，旧Source识别、Job插入顺序、先Stage再验证再Committed释放的生命周期以及Reset释放顺序均保持不变；Source Module仍是物理资源Owner，Program Actor State仍是节点连续状态Owner。Program Runtime由2252行降至1514行。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 根Frame Transaction一致性审计

状态：可编译源码中只有`CharacterAnimationPresentationRuntime`构造并持有`CharacterPoseFrameTransaction`。它只生成lineage与typed lease，按Action、Pose Advance、Motion Matching、Source Demand、Prepare、Evaluate Barrier、Release、Seal和Post-Commit的固定顺序调用模块；Operation、Native offset、Program Frame页、Foot Context、Goal页、FBBIK状态、Source物理页与Physical Bone字段均未进入根协调代码。

Program、Source、Constraint与Final Publication的Begin／Result／Seal都由`CharacterPoseFrameCoordinator`按同一lineage和Tuning Generation校验；Barrier前仍执行可回滚Discard，进入Barrier后仍丢弃Constraint／Publication Pending页并把Actor标记Faulted，物理Writer后的Seal路径没有新增业务计算。任务9.1至9.4完成。

## 拆出Program Evaluation模块

状态：新增`CharacterPoseProgramEvaluationRuntime`，整体拥有Evaluation状态页，并接管Frame绑定、Prepared消费、Stage执行、Sequence Preview执行、节点完成、Pending／Committed提升及Committed诊断View。Program Runtime不再解释Evaluation workspace状态、Stage循环、World输入或完成页，只在自己的根Program lease通过后转交。

Stage顺序、提前停止条件、World Context输入、Preview Operation位置、Player／Stack／Route完成通知、Frame页提交顺序和Committed诊断来源保持不变；Motion Matching继续读取同一个Evaluation状态实例，没有复制第二Committed状态。Program Runtime由1514行降至1367行。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 拆出Program Actor状态协调模块

状态：新增`CharacterPoseProgramActorRuntime`，统一拥有Pose State推进、Transition与Root Orientation Warp控制、Linked Pose generation选择与局部Reset、Actor节点Frame的Begin／Commit／Discard。Program Runtime只保留根Program active／committing lease，再把同一Actor State与Frame页交给该模块执行。

Sequence Preview短路位置、State准备与最终Transition时机、Slot control写入、Linked Fragment匹配、generation reset、节点打开／回滚顺序和Root Orientation Warp reset值保持不变。Actor State仍由Program Runtime唯一释放，没有新增第二状态页。Program Runtime由1367行降至1029行。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 拆出Presentation提交诊断协调器

状态：新增`CharacterAnimationPresentationDiagnosticsCoordinator`，独立持有Action lifecycle、Action时间、Source同步、Retired Playback与Debug View的Committed投影，并统一处理interest合并、无interest清理、Foot短租约发布和Trace发布。根Presentation Runtime不再持有或拼装任何诊断Snapshot列表，只在Post-Commit把同帧typed Result交给该协调器。

无interest时的跳过计数、仅Foot Event时不生成Runtime Snapshot、Live／Capture状态快照条件、Foot短租约失效时机和Trace输入保持不变；该模块不参与Program、Source、Constraint或Final Pose结果计算。根Runtime由约1380行降至1202行。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## Runtime与Preview统一装配审计

状态：正式Runtime与`AnimationPreviewRuntime`都只构造`CharacterAnimationPresentationRuntime`；全仓只有其构造函数调用一次`CharacterPoseRuntimeCompositionFactory.Create`，也只有该Factory创建actor-local Execution View和Program Runtime。Preview只提供显式Body／Fact／World／Equipment／Timeline／Motion Matching Query输入，再调用同一个Present、Reset、Tuning与Sequence Preview入口。

搜索确认不存在Preview专用Pose Executor、第二Native Program、临时Program、运行时Compile、默认World Context、旧Projection reader或Stale Projection fallback；Projection不匹配只报告Stale并拒绝继续。任务13.8与13.9完成。

## 删除Seal后的第二Program Image构造

状态：Inertialization descriptor编译已进入唯一Pose Compiler Module，在Family Payload绑定、Stage／Value／Workspace完成后、Program Image Seal之前执行。Seal Pass现在一次接收全部descriptor、沿用原先“基础PlanHash + Inertialization schema与规则”的最终Hash算法，并只构造一个`CharacterPoseProgramImage`；Projection Compiler删除Seal后重建整个Image的路径。

Inertialization owner查找仍使用同一已绑定Operation顺序、Player Source index、StateMachine transition和Policy，规则排序与Hash token逐项保持不变；失败统一成为Seal Program Image编译诊断，不发布半成品Image。全仓Editor源码现在只有Seal Pass一处`new CharacterPoseProgramImage`。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 删除Seal后的Motion Matching程序修改

状态：Motion Matching Pose Plan现在从同一Family Payload Binding生成typed compilation结果，并在Workspace Plan阶段计入每个Pose Value的Contribution容量；Seal Pass一次写入Node、History Collector、Entry Program与Blend Plan页。`CharacterPoseProgramImage.ConfigureMotionMatching`及Projection Compiler的Seal后修改已删除，最终Hash继续按原先Inertialization Hash后追加Motion Matching schema、Node与Blend token。

Motion Matching节点、History边、Entry Graph容量、Blend catalog、Provider binding和Operation value index的解析顺序保持不变；无Motion Matching节点时仍写空页并追加同一schema hash。Program Image构造完成后不再修改PlanHash或Workspace容量。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## Program Image与唯一Owner最终审计

状态：Program Image schema固定为v24、Runtime ABI固定为v27，Source Map的Operation／Graph／Node／CallSite现已进入PoseProgramImageHash；Runtime只从`CharacterPresentationProjection.PosePlan`建立同identity的actor-local Execution View，没有旧schema reader、运行时补齐或动态Compile。Gameplay Semantic Contract与Projection顶层Program／Semantic／Contract Hash代码相对指定基线未修改。任务12.5完成。

全仓构造点逐项核对为：一个Program Image Seal、一个Execution View Factory、一个Program Evaluation State、一个根Frame Transaction、一个Action Playback、一个Source Module、一个Operation Executor、一个Constraint Runtime、一个Goal Assembler、一个FBBIK Solver、一个Final Publication和一个Physical Writer。Runtime Pose链搜索不存在authoring asset、NodeKind、AssetDatabase、旧Projection版本分支或动态编译。任务14.3与14.4完成。

## 归还Program诊断页投影所有权

状态：Actor State与Program Evaluation的Committed Diagnostics Projector已从`CharacterPoseDiagnosticsRuntime`移入唯一`CharacterPoseProgramRuntime`。Program在根Frame打开时失效自己的Actor诊断页、Reset时清理，并只通过两个Committed View入口向外发布；Diagnostics Runtime不再持有Execution View，也不再接收Stack、Route、StateMachine、Inertialization、Clip、BlendSpace或Root Warp集合。

Actor与Program诊断字段、interest条件、快照排序和读取的Committed Evaluation binding保持不变；这里只收紧Owner，尚未改变外部Foot采样链。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 在Program Pending完成时冻结诊断页

状态：Program Evaluation完成全部Stage和节点完成通知后，`CharacterPoseFrameCoordinator`在根Seal之前把同lineage的Program Result、Program Output与Final Publication Pending Frame交回Program。Program-owned Actor与Evaluation projector立即按冻结interest深拷贝State、Operation、Value、Contribution、Pose与最终Output；Post-Commit Diagnostics只取得已准备View，不再读取Actor State或Native Evaluation binding生成新事实。

无diagnostics interest时不执行拷贝；Discard与Reset清除本帧View，成功根提交后才允许读取。原有字段内容、数组容量、排序和短租约有效期保持不变。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 在各Module完成边界冻结诊断事实

状态：Source在Committed退休应用完成后冻结物理Source与release页，Constraint在Bank Seal并发布Foot内部Committed诊断后冻结Foot／Goal／Solver页，Final Publication在Pending Frame提升时冻结Program Output、Physical Write与最终Frame页；Program已在前一步于根Seal前冻结Actor／Operation／Value／Contribution／Pose页。全部冻结都使用Frame开始确定的interest，未观察帧不复制诊断payload。

Post-Commit `CharacterPoseDiagnosticsRuntime`现在只验证同lineage并取得四个Owner已经冻结的Committed View，Snapshot Publisher不再从Native Program、Pending Workspace、Foot Context、FBBIK Vendor对象或Physical Transform反推事实。任务13.3与13.6完成。3C MCP完成全脚本重编且C#错误为0，不单独运行回放。

## 生命周期、Node Definition与依赖方向审计

状态：Reset只清actor-local Program／Source／Constraint／Publication／Diagnostics状态，不修改Projection内Program Image或Execution View；Preview Seek走同一Reset，Projection切换只允许释放整个旧Runtime后由正式Factory重建，Actor Fault禁止继续Present，Dispose由Composition依次失效Publication、解绑Job、释放Source、Program Execution View／Actor State／Frame页并恢复Graph Clock。任务5.10完成。

Agent exporter不判断Node Kind；Package codec的Subgraph／Graph Input／Graph Output规则通过Definition取得Capability；Target Mapper无Pose Kind分支；Profile Inspector只读Capability字段；Clipboard通过`RequireCapability`解析Definition；Canvas使用统一Port Shape；Compiler中的Kind判断只剩Definition注册、节点局部Lowering与Topology全局唯一性规则。不存在可由Definition／Capability／Port Shape替代的消费端switch，任务10.10完成。

Pose Contracts目录不引用Program／Source／Constraint／Publication Implementation或Editor命名空间，Pose Runtime目录不引用UnityEditor／AssetDatabase；Diagnostics只消费冻结Result View，不参与运行结果选择。3C MCP完整脚本编译同时证明现有asmdef引用无循环，任务14.5完成。

## 更新项目Pose Graph架构真相

状态：`openspec/project.md`已写入实际Program Image v24／Runtime ABI v27、固定Compiler Pass、Seal前Inertialization与Motion Matching、actor-local Execution View、Program内部组合模块、根Frame Transaction、三分区Tuning、四个Owner诊断冻结页与唯一Physical Writer；目录职责同步区分Program、Sources、Constraints、Final和Diagnostics。任务14.6完成。

## Runtime与Editor工程编译

状态：使用`dotnet build ThirdPersonClient.Runtime.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false`完成Runtime工程编译，结果0错误、27个既有Package／Analyzer警告；随后立即执行`dotnet build-server shutdown`。使用相同参数完成`ThirdPersonClient.Editor.csproj`编译，结果0错误、30个既有Package警告；随后再次关闭MSBuild与VB/C#编译服务器。任务14.7完成。

本change的`openspec validate refactor-character-pose-graph-architecture --strict --no-interactive`通过。全量strict实际执行为100项通过、8项失败，失败均来自其它现存change／spec；全工作区`git diff --check`只命中其它Performance工作修改的六个Prefab空值行尾空格。因此14.8暂不标完成，也不跨范围修改这些文件。

## IK保护项源码审计

状态：当前HEAD搜索不存在`CharacterFootStateMachine`、SmoothKnee、Pelvis Reach硬夹紧或末端Foot Effector夹紧实现；Current Support只作为正式接触观察与Unavailable结果存在，`CharacterFootSwingMotionBuilder`不使用它替代Ground Envelope候选。FBBIK的可靠动画膝向仍通过`Quaternion.FromToRotation(originalAnkle-originalHip, targetAxis) * animatedDirection`保留有符号运输，只对无可靠动画方向的历史／参考候选执行同半球翻转。

本change提交未修改Foot／Pelvis／FBBIK算法与Profile，也没有接管外部仍在进行的Foot行为任务；只迁移Constraint typed边界、根事务和Committed诊断页。任务14.9完成；数值行为任务3.9与14.10仍等待正式Projection重建后用同一Replay闭合。

## 最终Projection重建阻塞复核

状态：再次通过3C实例的正式`character_build_float32_products`入口执行Corin Character Build，Job `f894dcbec04e4e82b2928117b29da433`仍在Presentation Projection阶段被现有Foot Analysis geometry validation identity stale拒绝，覆盖Attack1至Attack5、Dodge、Idle、Walk／Run／Turn等正式Timeline与Pose Source绑定。构建没有发布半成品，generated Projection仍是旧Program Image v23／Runtime ABI v26。

本change不能绕过Foot校验、伪造新identity或修改Foot资产，因此12.9无法完成；没有v24／v27正式Projection也不能运行当前Runtime的同输入Replay，3.9与14.10随之保持未完成。14.8已实际执行，但全工作区diff与全量OpenSpec的失败均来自其它并行工作，未越界修复。

## 发布正式Worker Program Image

状态：后续已通过正式Character Build发布Corin Program Image v24／Runtime ABI v27，generated Projection包含`character-pose-worker-plan/v1`、`worker-required/v1`、Rig执行布局、Kernel Set及三个正式Batch。该事实由提交`e03ca7cfa`保存，上一节“仍是v23／v26”的阻塞结论只属于当时证据，不再代表当前工作区。

Worker计划不再是孤立数据：正式`GameplayTickSystem -> CharacterPoseWorkerPresentationSession -> CharacterPoseWorkerScheduler`链按当前表现帧收集全部Actor，同Program内按Stage／Dependency Wave／Family Kernel组合Handle，不同Program再组合，并只在进入后续Managed依赖前完成一次。Worker直接读写各Actor自己的Pending Frame页，不建立第二Workspace，也没有逐Stage全量CopyIn／CopyOut。

## 分离Fixed Replay与Foot诊断

状态：基础Fixed Replay与Foot诊断已经拆成两个显式操作。普通Replay不检查、不启动、不等待Foot采样，基础Proof升级为`character-fixed-input-replay-proof/5`且不包含`foot_sample`；显式Diagnostic Replay才组合Foot采样并发布独立`diagnostic-v1`证据。原先尝试自动修改`PlayerSettings`编译符号的`CharacterReplayDiagnosticCompilationSession`已删除，没有保留项目级编译状态机或恢复路径。提交为`440cf1d3a`。

## 1044帧Replay运行时诊断

状态：使用固定trace `43357ff3cd384e5cba75d2c31175b116`实际启动基础Replay后，已依次定位并修正四个外层迁移错误：非活动Player错误要求Physical Source Binding、Final Publication把打开中的零Completion lineage当作已完成输出身份、Worker Tuning Weight把完整Operation index误当成Native Operation index、IJobParallelFor把共享只读NativeArray藏在嵌套输入结构中触发Unity ParallelFor限制。对应提交为`e6688e4d9`、`bdcbcbe81`和`f3bc39a6c`。

Managed与Worker现在共用`CharacterPoseValuePageSlice`和`CharacterPosePureMath`，Managed Stage遇到Worker Domain会拒绝，不存在Pure Pose重算或串行fallback。Scheduler按Registration与ActorId拒绝重复提交，检查Actor写页不重叠；任何已成功Schedule的Handle立即进入Outstanding，后续Schedule、Complete、Reset、注销或Dispose失败都先Fence再释放页。正式`ICharacterPresentationRuntime.Present`单角色立即Schedule／Complete旁路已经删除。对应提交为`0fff2bf6f`、`c48669ef0`与`0775ab65c`。

Program Image加载现在重新从Operation Header的typed输入输出计算每个Batch的精确读写集合、输入Completion和Workspace范围，并检查同Stage同Wave跨Kernel的写写／读写别名；序列化声明与实际Kernel Operation不一致会直接拒绝，不再只做数组越界检查。Worker Batch Planning也拥有独立Compiler Pass Diagnostic，可报告Node、Family、Execution Domain与source path，不再统一伪装成Seal失败。对应提交为`b6e31c429`与`b71bbe293`。

Runtime与Editor工程均使用规定参数编译通过，结果0错误；每次编译后均已执行`dotnet build-server shutdown`。

## Worker架构仍未闭合的证据

状态：当前不能勾选11.9、11.13、12.12、12.13、14.10或14.11。

- Compiler实际仍先运行`CharacterPoseFamilyPayloadBindingPass`，再运行Stage、Value Lifetime、Workspace与Worker Batch Plan；这与批准的“Workspace -> Worker Batch Plan -> Bind Family Payload -> Seal”顺序不一致。当前新增的Worker Pass Diagnostic只修正错误归属，没有把Bind移动到Worker之后。
- Worker Seam虽然不再复制Workspace，但`CharacterPoseWorkerActorSlice`仍通过`CharacterPoseValuePageSlice`取得完整Actor Value页裸指针。Program Image typed range现在能在Seal／加载时证明正确，Kernel Interface仍没有把访问能力物理收窄到对应Batch声明的范围；该Memory Interface的Locality仍需继续闭合。
- 正式Gameplay已删除单Actor旁路，但Preview的同步`CharacterAnimationPresentationRuntime.Present`仍按单个Preview Actor立即Begin／Submit／Complete。Preview必须改为显式调度Adapter，根动画Runtime不能继续拥有第二套Scheduler驱动方式。
- 本轮旧Burst异常后Unity完成脚本域重载并重新连接`3C_Client@e852139597e42532`，但主线程持续对MCP返回`ping not answered`。因此最新Worker修正尚未重新跑完1044帧，3.9、14.10及行为等价结论继续保持未完成；不得以源码编译替代Replay证据。

## 延后Family ABI绑定并分离Preview调度

状态：前一节列出的Compiler顺序与Preview根调度问题已经继续修正。Compiler先生成不含Runtime Operation Pages的`CharacterPoseFamilyPayloadPlan`，Stage、Value Lifetime、Workspace和Worker Batch Plan只消费该计划；Worker Plan完成后，唯一`CharacterPoseFamilyPayloadBindingPass`才创建Header／typed Value Reference／18个Family Payload页，并立即用Worker读写、Completion、Workspace、Rig与Kernel身份验证绑定结果，最后交给Seal。Runtime ABI不再先于Worker安全计划封存，提交为`b6c99a1e0`。

`CharacterAnimationPresentationRuntime`已经删除同步`Present`、Sequence Preview旁路、Scheduler字段和Scheduler生命周期所有权，只保留Begin／TryAdvance／Complete的根帧协议。正式Gameplay由`CharacterPoseWorkerPresentationSession`驱动；Preview由显式`CharacterPoseWorkerPreviewAdapter`驱动并独立拥有自己的Scheduler，两个Adapter共用同一个Program Runtime、Frame页和Kernel，不再让根动画Runtime维护第二套调度路径。未使用的`CharacterSimulationPresentationRuntime.PresentAnimation`旁路同时删除，提交为`6e83d88e6`。

上述修改后Runtime与Editor工程再次使用规定参数编译通过，均为0错误，并已关闭全部.NET Build Server。11.9、11.13与12.13的源码实现已经闭合；最终勾选仍与12.12、14.10、14.11一起等待同一1044帧Replay确认。当前剩余结构问题只保留Worker Actor写页的typed物理切片，以及Unity主线程恢复后的实际Burst执行证据。

## 收口Worker写页、诊断与资源压力政策

状态：Worker Actor不再只用Pose首地址代表整个可写Frame。`CharacterPoseValuePageSlice`现在逐项比较Pose、Velocity、Parameter、Contribution、Foot Feature、Availability、Continuity、Discontinuity、Operation Completion和Graph Invalid全部可写页；任一页共享都会在Submit阶段拒绝，只有明确互不重叠的Actor Pending页才能进入IJobParallelFor。提交为`90c18c43d`。

Operation Detail诊断现在为每个Operation保存Worker Batch Index／Identity、Family Kernel与Dependency Wave，并与既有Operation Completion Identity一起只在根事务成功提交后的Committed页中发布；Managed Operation使用显式None／-1身份。Diagnostics代码不调用Scheduler、不等待Job、不重放或重新调度Kernel，任务13.12完成，提交为`3693b257b`。

运行时代码搜索没有动画预算、Phase Offset、跳帧、旧Pose复用、Pose插值补帧、节流、LOD或按帧率降级路径。Animator构造仍强制`AlwaysAnimate`；每个有效表现帧都经Begin／Worker与Managed Completion／Complete／Seal，资源压力继续按现有精确Completion与Fault政策失败，不复用旧Pose，任务14.13完成。Reset、注销、Projection整体替换和Dispose均通过Actor Registration先Fence Outstanding Handle，再释放Execution View、Frame页与Module状态；Submit同时拒绝重复Actor和任一共享写页，任务14.11完成。

本change strict校验再次通过；全量strict为100项通过、9项失败，失败属于其它现存change／spec，未跨范围修改。MCP Server已独立重启并重新监听8080，精确实例`3C_Client@e852139597e42532`重新注册后仍对主线程命令返回`ping not answered`，进一步确认阻塞位于Unity Editor进程而非MCP Server。

## Worker运行时错误闭合与基础Replay通过

状态：Unity实例`3C_Client@e852139597e42532`恢复后，使用固定trace `43357ff3cd384e5cba75d2c31175b116`重新执行正式基础Replay。此前第2帧的`IJobParallelFor`异常来自共享只读Program页仍受单个`actorIndex`范围限制；现只对Operation index、Header、Family Payload、Parameter Default／Policy、Mask、Reference与Rig Parent等不可变共享页同时声明`ReadOnly`和`NativeDisableParallelForRestriction`，Actor切片本身仍按`Execute(actorIndex)`读取，并继续由Submit阶段逐项拒绝任何共享可写页，没有关闭Actor写页隔离检查。

空间转换随后暴露`in/out`自别名错误：共享`CharacterPosePureMath.TryToModel/TryToLocal`把Pose输入改成`in`后，Worker仍以同一局部变量同时传入`in pose`与`out pose`，函数开头清空`out`时也清空了输入。主线程对同一root／bone1数据可正常转换，证明Rig、AnimationSlot和Parameter Resolve输出没有损坏。两个转换函数现恢复指定基线的按值输入语义，Worker与共享Value页全部继续调用同一套纯数学，不增加Managed重算或fallback。提交为`b9337dfb8`。

完成Pose计算后又定位两处外层迁移遗漏：Source诊断页曾在Action Backend deferred release与release diagnostics关闭前冻结，现移回Post-Commit诊断协调器，在正式发布已提交结果且Source release闭包完成后才按interest冻结；Stage Snapshot的合法Execution Domain上界同步包含`ManagedControl`与`ManagedConstraint`，不再把v27正式Stage误判为非法。运行计算与Final Publication不再由提前诊断冻结阻断。提交为`bfe626415`。

修正后同一trace连续完成两次1044帧基础Replay。第一份Proof为`Temp/CharacterInputReplayProofs/v5/43357ff3cd384e5cba75d2c31175b116/20260903-141127-650-56fb01c86a65466da168a80093ce1a20.json`，建立1044帧v5基准；第二份为`Temp/CharacterInputReplayProofs/v5/43357ff3cd384e5cba75d2c31175b116/20260903-141550-925-fa340caf0a2b4068bd1047bdf8ab8f10.json`，工作流结果为`matched:1044`。两次均使用Program Hash `d4cf63902d75c88bc9d7883a81ab89fbce7e6269cfaf457bb07b2a9053386301`、Projection Revision `9119b4d7d702461ade4bd9a42069caaf6bce6878d74383f8d6c0cc2287352e91`与相同input/body hashes；基础Replay全过程`foot_sampling=false`且`foot_sampling_available=false`。

这份证据证明正式Worker链可跨两个Gameplay Lab Actor连续完成、基础Fixed输入／Body结果可重复，并证明普通Replay不依赖Foot采样。它不包含Foot、Support、Pelvis、Goal、Solved与Physical逐项数值，因此不把任务3.9或14.10标成完成；这些任务仍需外部Foot诊断能力按独立操作提供指定基线A/B证据。此前Burst abort在当前Unity进程留下重复`ALLOC_TEMP_MAIN`原生报警，停止Play Mode后仍存在；成功Replay没有再产生Pose Worker、空间转换、提交或Snapshot异常，但干净Console证据需新的Unity Editor进程，不能把该进程残留写成当前实现的新分配泄漏。

最终源码状态再次使用规定参数编译Runtime与Editor工程，分别为0错误／1个既有警告和0错误／57个既有Package及Analyzer警告；两次构建后均已执行`dotnet build-server shutdown`。本change strict校验继续通过。

## 重新开启正式诊断编译并完成重构前对比

状态：通过显式的`Tools/3C/Diagnostics/Enable Foot Capture Compilation`入口开启`KK_DIAGNOSTIC_SAMPLING`与`KK_DIAGNOSTIC_FOOT`，没有再引入自动修改`PlayerSettings`的回放状态机。当前Standalone编译选项保持开启，普通Fixed Replay仍不检查、不启动也不等待Foot采样；只有显式Diagnostic Replay才拥有Foot能力。对应入口提交为`334dfd64d`，生成诊断能力的外部包Identity修正提交为`7e983d6`。

使用固定trace `43357ff3cd384e5cba75d2c31175b116`再次完成1044帧Diagnostic Replay。最新证明文件为`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/diagnostic-v1/43357ff3cd384e5cba75d2c31175b116/20260903-154821-658-60c1c98fb34742018b3461381c7811f8.json`，结果为`matched:1044`。生成采样manifest为`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260903-074709-8b00a2b85c07406a9e693727ec0690e7/capability.manifest.json`，Full CSV包含2088行（1044帧、左右脚各一行）和1026列；分析报告为`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/FootAnalysis/20260903-074709-8b00a2b85c07406a9e693727ec0690e7-20260903-074840-cf3477cb7f634e90a6ab208788cfa613/report.md`。分析器的5项通过、15项失败、3项不适用和5项证据缺失是场景健康结论，不能直接当作重构回归。

重构前对照使用保留的正式A样本`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/20260902-033516-613-1591907a78214a389ab18cdc9835fe66`；它的来源链对应指定基线`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`，当前环境没有重新在该历史提交上启动Unity，而是用这份已归档的基线链A与当前生成字段做语义对齐。对齐结果如下：

- 2086个旧行与当前行匹配，当前多出的2行是当前采样从第2帧开始、旧A从第3帧开始造成的边界差异；
- 当前生成字段映射到839个旧业务字段，共比较1,750,154个单元；布尔、枚举、数值容差和不可用默认值归一化后，1,749,453个单元等价，语义等价率为99.9599%；
- 最终脚底、脚踝和物理发布字段的最大位置差约5.5e-6米，未见跨帧或跨侧的大范围漂移；
- 严格逐值仍有701个单元不同，主要是中间诊断事实：膝盖`solved-bend-degrees`最大差0.004501347度，少量Support／Goal中间值在第382帧附近变化，另有连续性Identity和Unavailable字段的语义变化。它们不能被隐藏在“完全一致”结论中。

因此当前证据支持“重构后最终表现结果没有发现可见的大范围回归，最终Pose数值保持在微米级差异内”，但还不支持“所有IK中间诊断逐值bit-exact”。任务3.9与14.10仍保持未完成，后续若要求严格等价，必须针对上述中间值逐项确定允许的浮点／不可用语义边界后再闭合；不应修改IK行为去迎合诊断表的旧列格式。

## 诊断编译开启时普通Replay仍保持独立

状态：在同一Standalone编译选项保持开启的前提下，再次使用trace `43357ff3cd384e5cba75d2c31175b116`执行普通Fixed Replay。证明文件为`3cDemo/Client/3C_Client/Temp/CharacterInputReplayProofs/v5/43357ff3cd384e5cba75d2c31175b116/20260903-155458-200-f27935372b4a49af9b074643cabb1729.json`，工作流结果为`matched:1044`，状态同时明确`foot_sampling=false`、`foot_sampling_available=true`。这证明诊断程序集已编译并不等于普通回放会启动或等待Foot采样，普通Replay与显式Diagnostic Replay仍是两条业务操作而不是隐藏耦合。

## 当前生成诊断的重复运行校验

状态：对两次独立Diagnostic Replay生成的Full CSV（`20260903-070157-780a1e7d190b47f996b5671e2aa34534`与`20260903-074709-8b00a2b85c07406a9e693727ec0690e7`）按`lineage.high + dimension`对齐，忽略采样Identity、时间、实例、Revision和Frame Identity等运行身份字段，并使用同一布尔／枚举／`1e-4`数值归一化规则比较。两次均为2088行；剩余1,897,992个行为单元全部一致，没有发现当前实现自身的随机漂移。由此可把前一节相对旧A的701个差异归为旧数据几何／身份或中间诊断语义差异候选，而不是当前Worker链每次运行不稳定。

## 恢复Launcher诊断状态与产物快捷入口

状态：修复了诊断窗口在Play／编译状态变化后不重绘，以及Unity域重载后丢失最近产物路径的问题。采样Workflow现在从持久化的最近manifest或`Diagnostics/GeneratedFootSampling`下最新有效manifest恢复Full／Core CSV、manifest和目录；分析Workflow恢复最近报告。Launcher的`Reveal Sample Folder`、`Analyze Last Capture`和`Open Last Report`因此不再依赖本次域生命周期内的静态字段；不在Play时仍明确提示“先点击Play Selected Variant”，但已完成产物可在编辑模式打开和分析。

通过3C MCP验证：编辑模式下状态为`capture_compilation=true`、`sampling_available=true`，最近恢复的有效采样位于`3cDemo/Client/3C_Client/Diagnostics/GeneratedFootSampling/20260903-080624-395d5f0538484f499eb8fc3ff436b6d8`（199帧）；直接执行`analyze_last`成功生成对应报告，随后`open_report`入口成功。此前本轮1044帧Diagnostic Replay仍保存在`20260903-074709-8b00a2b85c07406a9e693727ec0690e7`，两者都在同一`Diagnostics/GeneratedFootSampling`根目录下。

## FlowCanvas原生Pose Inspector收口（POSE-EXEC-20260911-01）

状态：提交`73433946f`移除Pose Workspace对自定义`domainPanel`的挂载，FlowCanvas原生Node／Connection Inspector恢复为可见作者入口；提交`d52641f4e`把Parameter Policy和IK Goal Binding的数组编辑迁入`CharacterPoseCanvasNode.OnNodeInspectorGUI`，增删、枚举、骨骼、偏移和权重修改均通过同一`SetNodeField` typed Mutation提交。IK骨骼选项只从当前精确Profile的Rig上下文取得，缺少上下文时明确显示Unavailable，不接受自由文本。

Unity MCP目标实例`3C_Client@e852139597e42532`刷新后Console为0 error，仅有既有Package／AssetDatabase warning。自定义Details presenter仍作为未挂载的历史实现保留，23.3与23.4已按当前可见入口完成；23.5正式Build、23.6 Scene Play Preview、24.1–24.5可复用Graph Binding和22.5交互验收继续保持未完成，不以编译替代窗口操作证据。

## Pose-only观察发现Profile派生版本待重建（POSE-EXEC-20260911-02）

状态：通过Unity MCP执行`character.pose_reset_observation`时，正式运行链在`CharacterPresentationFullBodyIkDescriptor.RequireValid`处报告`Full Body IK descriptor is invalid`。对照当前工作区发现`CorinFullBodyIkProfile.asset`的左右腿`Pull`已由`0`改为`1`，但`m_Revision`仍为旧值`e83001…`；未回退这两个作者字段，已通过正式ScriptableObject属性写入同步当前派生revision `c88bb920961075b08561c03e92a16d70f06fad08560267e1516f4fd76c9adbb2`。

当时生成Projection仍保存`m_ProfileRevision: e83001…`，因此必须在Agent并发重构恢复可编译后重新执行唯一Character Build，不能手改Projection或以旧产物继续观察。当前Unity Console阻塞来自Agent未提交文件中的字段迁移错误，不属于Pose代码；Pose作者入口本身没有新增编译错误。

随后按当前Profile字段完成正式Character Build，Unity MCP返回Float32、Fixed与共享Presentation Projection发布成功，ProjectionRevision为`7fd5162562a8292210661579963a92822664db5d1b2f0fa67ff098fcebd71089`。Pose-only观察继续推进到资源加载入口，内层错误变为`YooAssets not initialize !`；已在Preview统一`Present`入口补齐`CharacterAnimationResourceScope.AdvancePreparation()`，但编辑模式Fixture没有正式Scene资源包初始化。此处不添加AssetDatabase fallback loader，等待正式Scene Play／资源启动链后再登记Pose reset观察通过。

正式Build之后执行只读`btsmtl.validate`，job `25b186b21478421981fd5bbf6e458954`完成且`success=true`；报告为`compileSuccessCount=1`、`semanticValidCount=1`、`compileFailureCount=0`、`semanticInvalidCount=0`，`plannedDiff`与`appliedDiff`均为空。验证覆盖当前CharacterController Document闭包与两种Numeric Target，未改变作者资产。

## 删除Pose Graph旧迁移路径（POSE-EXEC-20260911-03）

状态：当前Canvas Graph已经是唯一Pose Graph作者数据后，删除`CharacterPresentationPoseGraphAsset`中未再被调用的`LegacyPoseGraph`序列化字段、迁移状态对象、旧Graph转换器和混合迁移校验入口；Graph、Graph Catalog、Source Slot和Resource Slot的正式作者数据与Profile绑定入口保持不变。提交为`4b1673e60`。这一步只清理旧路径，没有改动Corin Graph资产或用户未提交的Profile／Document文件。

## 可复用Graph直接资源残留审计（POSE-EXEC-20260911-04）

状态：当时的源码审计确认Graph payload仍有两类直接资源引用尚未迁移。`CharacterPoseStateMachineAuthoringContracts`的Transition直接保存`CharacterAnimationBlendCurveAsset`与`CharacterAnimationBlendProfile`，Motion Matching payload直接保存`CharacterMotionMatchingBinding`与`CharacterAnimationBlendPolicy`。这两类源码直连随后都已迁移为Graph-owned Slot；Corin旧序列化数据仍待正式Unity Mutation重写，因此24.1与24.2仍不能勾选，不能在Graph外加兼容读取或角色名推断。

## 正式Scene Play Pose观察入口（POSE-EXEC-20260911-05）

状态：删除`character.pose_reset_observation`对`CharacterAnimationPreviewFixtureSession`的直接依赖，改为只观察正式Scene Play注册的`AnimationPresentationRuntimeTarget`。任务通过`start/status`异步等待已提交帧，使用统一Session Tick Drive暂停并单步；Reset经同一`CharacterSimulationPresentationRuntime`的target合同执行，完成后恢复原实时驱动。运行时Reset合同提交为`a777da720`，MCP观察实现提交为`14f9926d9`，scheduler依赖修正为`23e414871`。代码入口已经切换，但尚未用目标Unity MCP跑出成功观察结果，因此23.6仍不勾选。

## 将Motion Matching资源迁入Pose Resource Slot（POSE-EXEC-20260911-07）

状态：提交`7a53e62e8`将`CharacterMotionMatchingPosePayload`的Binding与Jump Blend Policy改为Graph-owned `CharacterPoseResourceSlot`，新增`MotionMatchingBinding`资源种类；作者Definition、Mutation、Input Contract、Projection Resource Catalog、Motion Matching Projection、Blend Catalog、Pose Plan和Graph Validator全部从Profile Resource Binding解析，不再从节点读取角色资源。资源仍由精确Profile按Slot提供，节点只保留稳定Slot身份和规则字段。使用临时仅限`ThirdPersonClient.Editor`的MSBuild注入编译，Editor产物0错误；临时注入已删除并执行`dotnet build-server shutdown`。当前StateMachine Transition的Blend Curve／Blend Profile直连仍未迁移，24.2保持未完成。

## 删除PoseGraph自定义GraphEditor面板扩展（POSE-EXEC-20260911-06）

状态：提交`45acadc47`删除`GraphEditor`的`domainPanel`字段、挂载方法、画布尺寸预留与原生Inspector跳过分支，同时删除Pose Workspace的对应清理调用。Pose作者编辑表面不再存在项目自定义Domain Panel；FlowCanvas原生Inspector、Connection Inspector、Blackboard、breadcrumb和画布直接负责作者交互。Workspace仍作为非可视生命周期／Mutation／导航协调对象存在，未改变Pose节点数据、运行时或`CreatePoseOnlyInput()`链路。目标Unity MCP当前主线程仍返回`ping not answered`，因此本步尚未登记窗口重载后的视觉证据。

## 完成StateMachine资源直连源码迁移（POSE-EXEC-20260911-08）

状态：提交`e60c2ebfd`将StateMachine Transition的Custom Blend Curve与Blend Profile改为Graph-owned `CharacterPoseResourceSlot`，新增`BlendCurve`与`BlendProfile`资源种类；StateMachine合同只验证Slot kind，Blend Compiler与Family Payload Binding Pass再从精确Profile Resource Binding取得实际资产。人工创建、字段Mutation、Details和Graph Validator均已切换。带临时scheduler注入的完整Editor源码编译为0错误、93个既有警告；临时文件已删除并执行`dotnet build-server shutdown`。Corin旧Graph序列化数据尚未通过正式Unity Mutation迁移，当前不重建Projection。

## StateMachine资源槽迁移后的当前边界

状态：Pose Graph作者合同源码中已不存在`CharacterAnimationBlendCurveAsset`、`CharacterAnimationBlendProfile`、`CharacterMotionMatchingBinding`或`CharacterAnimationBlendPolicy`的直接字段；Agent文档仍按现行合同表达Curve/Profile asset identity，由Reconciler在精确Profile的`poseResources`中解析到Slot。Corin Graph asset与Profile仍是并行未提交改动，待目标Unity MCP恢复后创建／绑定新增Slot并保存，不能手改序列化JSON或把源码编译当作资产迁移完成。

## 收口通用Pose Source Slot（POSE-EXEC-20260911-09）

状态：提交`e5928500a`将`Selected Pose Player`与`Blend Stack`的作者payload、Node Definition和FlowCanvas Capability从`CharacterMotionMatchingPoseSourceSlot`收窄为通用`CharacterPresentationPoseSourceSlot`。这两个节点不再因为运行实现而绑定 Motion Matching；Clip Player和Blend Space Player仍保留各自明确的源类型约束。当前工作区另有通用Resource Slot Create／Rename／Delete Mutation待与AgentAuthoring拆分提交合并。

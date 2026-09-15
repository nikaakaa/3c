# PoseGraph只读Blackboard实施记录

## 2026-09-12 当前小步

已完成：

- `216bccd99`：FlowCanvas原生Pose Blackboard只投影外部Control输入，AnimatedProperty不再进入作者变量列表。
- `f8d6e78d4`：动画输入合同从根图Control声明和Profile属性绑定分别构建参数布局；属性导入器不再把BlendShape声明复制到每张PoseGraph。
- `779e9273f`、`1208a06f8`、`e260de9e2`：统一作者显示名、输入类别/作用范围投影、Document参数的Usage/displayName导出解析，以及Graph参数声明校验。
- `fb89142d4`、`9d916f0a9`、`8818c7d1c`、`9fdbc321b`：FootPlacement默认从输入Pose的内部曲线参数列读取；只有直接连接公开Pose输入Get时才允许外部覆盖，旧Body GraphInput透传会在编译时明确失败。

代码输入输出边界：

```text
Graph Control declaration -> read-only Blackboard/Get
Profile BlendShape binding -> animation input curve declaration -> source scalar page
Input Pose source-local Foot curve -> FootPlacement internal weight read
```

检查记录：

- `ThirdPersonClient.Runtime.csproj`按规定参数编译成功，0错误；编译后已执行`dotnet build-server shutdown`。
- `ThirdPersonClient.Editor.csproj`被工作区已有的`TimelineClipCreationPopup.cs`未定义`TimelineBindingValueKind`、`TimelineBindingAccess`和`TimelineBindingLifetime`阻塞；未修改该交叉文件，随后已执行`dotnet build-server shutdown`。
- 未运行Unity、Character Build、Play或资产导入；未修改用户未提交的PoseGraph/Profile/Scene资产。

仍未完成：

- EventGraph正式变量Contract/Layout/Frame尚未交付，本change没有创建第二变量更新器。
- 当前Corin Pose资产仍需要通过正式Document/Mutation删除旧的Action/Foot声明、根图Get、Body透传端口和确认无引用的重复子图。
- Subgraph/Linked Pose跨图可访问范围、完整曲线依赖编译收口、全部Document/Exporter/Reconciler/Validator同步和最终现行spec更新仍待继续。

## 2026-09-13 当前小步

已完成：

- `d82a7afbb`、`3e788506a`：为Pose子图接口增加内部Foot曲线端口门禁，并修正签名校验调用位置；内部曲线不得通过GraphInput、GraphOutput或Subgraph Call公开透传。
- `32f2cbba1`：原生Pose Blackboard主菜单不再显示内部Owner identity，只显示作者需要的输入信息。
- `9a448cfb8`、`4a8625e95`、`43cfcb34c`：纯十六进制稳定身份不进入作者主显示；Root、Animation Layer、Control Rig、Transition Rule及参数/骨骼下拉统一使用语义名称。
- `67e688fad`：FootPlacement和普通参数节点在输入合同缺项时返回带图/节点范围的明确编译诊断。
- `6034ba2a0`、`3367b6495`：拓扑校验接收同一动画输入合同，允许合法输入Pose曲线参与Resolve策略校验，并同步编辑器 Validate 与 Document Apply 的合同来源。
- `f612f8449`：节点和动态端口缺少显式作者名时使用语义显示名兜底，连接与稳定identity不变。
- `9427ba641`、`b0672667d`：Blackboard重命名同步到复用的原生变量对象；State与Subgraph页面标题过滤不透明GUID名称。
- `8b2d611d4`：拓扑校验要求跨图复用的同一外部参数保持类型、单位、默认值和Usage一致，避免形成第二份输入定义。
- `90df0a710`：Animation Input Contract从所有可达Pose Graph收集正式外部变量，按稳定ParameterId形成唯一运行时布局并拒绝跨图声明冲突；原有EventGraph合同改动保持未提交。
- `a42d1275e`：简化输入合同的根图存在校验；该提交同时包含索引中另一条已暂存的Skill规范变更，未回退其内容。
- `6d55f3288`：删除无调用者且会按单个Graph参数构建布局的遗留重载，保留唯一Animation Input Contract布局和用户要求的`CreatePoseOnlyInput()`隔离入口。
- `7fb3412a0`：Pose Get参数选择器只展示正式外部输入；该提交同时包含索引中原已暂存的EventGraph owner/apply改动，未回退其内容。
- `3cb2370bf`：Linked Pose、Pose Graph、Slot Group和物理骨骼引用选择器统一使用作者可读标签，写回仍绑定稳定identity。
- `902c6060f`：Resolve策略新增候选限制为正式外部输入，禁止从Inspector新增内部Foot曲线或BlendShape策略。
- `3c26e3525`：SetPoseGraphParametersMutation仅允许正式外部只读输入，防止Mutation绕过Blackboard边界写入内部Foot曲线或BlendShape声明。
- `902c6060f` 后的 Unity MCP Console 复查仍未出现Pose编译错误；返回项保持为未定位编辑器异常和SkillDefinition先验错误。
- `bd5dbfb71`：运行时Pose宿主接入Native EventGraph变量帧，参数帧持有冻结typed frame并只暴露外部控制参数，避免把BlendShape/Foot曲线当作事件图变量读取。
- `bd5dbfb71` 后的 Unity MCP Console 复查未发现编译错误；返回项仍为既有编辑器异常和重复的SkillDefinition先验错误。
- `b14900e69`：Animation Input Contract持有唯一EventGraph变量Contract、Revision和Layout identity，曲线、Fact、Slot与外部变量仍保持各自来源。
- `b14900e69` 后的 Unity MCP Console 复查没有新增Pose编译错误；当前返回项仍是未定位编辑器异常和SkillDefinition先验错误。
- `f1dc9f9eb`：运行、Preview、StateSource、BlendSpace统一消费`CharacterAnimationPoseInputFrame`，删除旧`CharacterPresentationProgramParameterFrame`与旧PresentationFrameCoordinator；显式Preview fixture保留为唯一测试输入入口。
- 旧Frame引用审计与Unity MCP Console复查均未发现残留Pose消费或新增编译错误。
- `b5e2590c4`：Preview Host和Authoring Controller移除direct参数接口，完整Pose Preview统一由Native EventGraph变量帧驱动。
- `f8cf7bfaa`：固化Character Animation Event Graph类型、Host和变量合同，并让Profile/Projection强制挂接唯一动画输入宿主。
- `60888aabb`：删除无调用者且无法满足严格EventGraph输入合同的Projection Draft备用路径。
- 当前 Transition Rule 作者面已接入唯一 EventGraph variable contract：Animation Variable 节点的类型、连线兼容性、详情值类型和选择列表均从同一 typed declaration 读取，支持 Bool/Int32/Float32，不再把该字段当作普通字符串。
- `f7023b27f`：Blackboard 改为由编辑上下文注入的唯一 EventGraph variable contract 投影，只创建精确 Bool/Int32/Float32 的只读变量；Pose 图本地 `m_Parameters` 不再驱动变量列表，Action Weight 与 Foot Placement 内部项不进入普通 Get。
- `32a8b1f90`：Pose 参数选择器的 `pose-parameter` 来源改为同一 EventGraph contract，策略引用继续保存稳定 ParameterId。
- 当前未提交小步已把 workspace、节点内参数策略下拉和 Animation Input Contract 接到上述合同；其中输入合同仍与并行 Pose typed-frame 改动同文件，待共享链路稳定后再单独提交。

正式 Document 流程：

- 已通过 Unity MCP CLI 以实例 `e852139597e42532` 调用 `btsmtl.checkout_document`，目标为 `Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`。
- checkout 在 SkillDefinition 完整性检查阶段失败，报告缺少原生状态机资产以及 Blackboard/ActionTargetSnapshot declaration；没有进入 Pose Document 写入或资产 Apply，也没有生成 `AgentAuthoring` 半成品目录。
- 随后以同一实例调用只读 `btsmtl.validate`，在相同 SkillDefinition 入口返回相同错误；因此当前还没有可用于 Pose 资产迁移的有效 Document hash 或 Mutation plan。
- Unity MCP Console 轻量检查返回的新增错误仅为工作区既有 Timeline `RuntimeDebugEventView.Status` 缺失，以及上述 SkillDefinition 缺失；未发现本change最近Pose文件的编译错误。
- 后续同一实例的轻量 Console 读取返回 `SerializedObjectNotCreatableException`、`ArgumentNullException`、`NullReferenceException` 和上述 SkillDefinition 错误；MCP/Console 未提供堆栈，暂不能把前三项归因到本change。
- 在 Skill 侧提交 `654327ce0` 后重新调用同一实例 `btsmtl.validate`，仍在同一 SkillDefinition 节点返回相同的原生状态机/声明缺失错误；Pose 校验和资产计划仍未开始。
- Native EventGraph API、Pose typed Frame和输入Contract提交后再次调用 `btsmtl.validate`，仍在同一 SkillDefinition 节点失败；没有进入Pose阶段。

当前约束：

- 上述代码提交未修改当前用户未提交的PoseGraph、Profile、Definition或Scene资产。
- 当前工作区的Character Document package尚未checkout，不能绕过Document/Mutation直接编辑PoseGraph YAML；旧的Action/Foot声明、根图Get、Body透传端口和孤立重复子图仍需在正式Document/Mutation流程中删除。
- `CharacterPoseGraphProjectionValidator` 的子图签名门禁已进入代码，但尚未对当前资产执行Unity Validate/Build；编辑器交叉编译仍受工作区既有Timeline改动影响。
- 上述 Transition Rule 作者接入仍处于共享 worktree 的未提交小步，待同批 EventGraph 输入合同相关文件统一落盘后再单独提交；没有改动 PoseGraph 资产或覆盖其它窗口的未提交文件。
- 以 Unity MCP 实例 `e852139597e42532` 复查时，Editor 已退出 Play、没有编译或资源刷新进行中；EventGraph `SourceCodePath` 合同错误已在后续刷新中消失，当前 Console 没有新增 Pose runtime 错误，剩余是其它窗口的旧 Agent 导航、Skill emitter 类型引用和 Timeline 运行观察接口错误。
- 上述资产统计是维护前快照；`LocomotionFullBodyPoseGraph.asset` 已通过 Unity MCP 正式维护入口完成边界清理，结果为 `declarations=387`、`ports=2`、`gets=1`、`orphanGraphs=2`。维护后磁盘复核显示 9 个目录引用图的旧 Action/Foot/BlendShape 声明、Foot 内部接口和根 Foot Get 均为 0；Body 图保留 Foot 节点读取输入 Pose 曲线的正常拓扑。

## 2026-09-13 Pose 输入边界维护

已完成：

- `e73e107b2`：增加 `CharacterPoseGraphAuthoringMaintenance` 正式维护入口，复用 `CharacterPresentationMutationService` 清理指定 Pose 资产的废弃输入声明、根图 Foot Get、Body/Call 内部 Foot 曲线接口，并删除同业务 Graph ID 的未引用重复图子资产。
- Unity MCP 以实例 `e852139597e42532` 调用该入口，未运行 Build 或 Play；资产写入范围仅为 `Assets/Configs/Animation/Presentation/PoseGraphs/LocomotionFullBodyPoseGraph.asset`。
- 清理后根图从 `5` 条边变为 `4` 条边，Body 子图从 `7` 条边变为 `6` 条边；Foot 节点的 `pose -> Foot Placement -> Full Body IK` 曲线读取链保留，权重不再由根图或子图接口透传。

当前仍未完成：

- Unity 维护入口只解决当前 Corin 资产的正式边界迁移；Pose 对旧 Agent EventGraph Mapper/DTO 的正式替换、完整 Pose C# 输出适配、跨图输入作用域和最终 spec 对账仍需继续。
- 当前 Unity 工程仍有其它窗口的 Skill/编辑器改动在编译或刷新，不能把本次资产维护结果等同于完整 Character Build 已通过。

## 2026-09-13 最新 Pose 编译边界

已确认：

- `0f752d9b3`：Corin 原生 EventGraph 移除没有正式所有者的 Action/Foot 权重变量及 Set 分支；Foot Placement 不再由 EventGraph 写入。
- `26f684d9e`：Pose State Graph 不再复制根图或运行时输入参数声明，只允许空的图本地参数集合，运行时统一读取 Animation Input Contract。
- `5dc21dc88`：Inertialization 规则改为稀疏参数 override，缺省保持 `Snap`；Corin policy 的 43 条旧 Action/Foot/BlendShape filter 已通过正式入口删除。
- 第二轮正式 Float32/Projection 构建已确认 Pose、Foot Placement 和 Inertialization 阶段通过；此前的 Foot 接口、State Parameter contract 和 Inertialization full-parameter 错误均已消失。
- 当前构建剩余错误为 Character Definition `AuthoringDiscovery` 的 `Value cannot be null. Parameter name: key`，尚未归因到 Pose 编译；C# authoring 任务正在处理 Definition/Skill 发现链和 Pose 薄输出适配。

当前 Pose 资产事实：

- `LocomotionFullBodyPoseGraph.asset` 有 9 个目录引用图；各图 `m_Parameters` 均为空，根图无 Foot Get/内部 Foot 动态接口，Body 图只保留输入 Pose 到 Foot Placement、Full Body IK 和输出的正式拓扑。
- BlendShape 仍由 Profile 的 AnimationPropertyBindings 和正式曲线资源合同提供，不再复制进任何 Pose Graph 参数列表；黑板/Transition 普通变量只接受 EventGraph 的精确 Bool、Int32、Float32 声明。

## 2026-09-13 C# authoring 与旧链删除

已确认：

- `62eb28721`、`95eea34ff`、`bb5005be3`、`d5af35ba4`：公共 `export_code/generate_assets` 已注册 Pose 正式 C# 薄适配；它读取 `CharacterPresentationPoseGraphAsset` 的 Graph、Node、Edge、Slot、StateMachine、Transition Rule、动态端口和正式资源引用，不读取旧 JSON 或 Agent DTO。Pose 根生成会恢复 Profile/Definition 挂接，并保持业务 identity。
- Pose 正式 `generate_assets` 已对 `LocomotionFullBodyPoseGraph.asset` 执行成功，返回 0 diagnostics；回写后 9 个有效图仍保持 `m_Parameters=0`、Foot 内部接口/Get=0、Body 输入 Pose 曲线链完整。
- `6b7be5c7b`、`9c9520afa`、`ad698053a` 及后续清理已移除 Pose EventGraph 旧 Mutation/Agent Mapper、Presentation/Skill Document 链和 EventGraphAuthoringDocument；业务代码中的旧入口扫描为 0。公共 authoring 注册表现在只保留正式 `export_code`、`generate_assets` 和非 authoring 的 Scene Play 工具。
- 项目 `.codex/skills/btsmtl-agent-authoring` 三份旧 Agent current 说明已按 r2-skill-and-generated-source revision 删除；`CodeGeneration/Generated` 下的正式 Pose/Skill C# 创建源码保留为唯一生成入口，旧业务工具和旧 Document 协议不再恢复。

未把以下结果写成 Pose 完成证据：

- 最后一轮完整 Character Float32/Projection 在 Pose、Foot、Inertialization 均通过后，仍有 Definition/Skill `AuthoringDiscovery` 的 null key 诊断；该错误位于并行 Skill/Definition 清理，不是 Pose compiler。作者可自行进行最终端到端验证。

## 2026-09-14 r3 原生 Runtime 第一步

- `CharacterPoseNativeRuntimeContracts.cs` 建立 Pose 图准备、实例上下文、采用版本、帧 lineage、source demand、阶段结果和节点观察的 typed 合同；合同不携带 Pose Image、IR 或全图操作计划。
- `CharacterPoseCanvasGraph.cs` 增加原生 Runtime 挂载、初始化、启动、停止和输出读取入口；未挂载实例时明确失败，不再用“Pose 图只能编译”拒绝原生生命周期。
- `CharacterPoseCanvasNode.cs` 取消运行时不注册端口的条件，节点输出通过挂载 Runtime 读取。
- `CharacterPoseCanvasConnection.cs` 接入 FlowCanvas ValueInput/ValueOutput 的真实绑定和解绑，删除“Pose connection 只能编译”的占位行为。
- `CharacterPoseCanvasNativePorts.cs` 移除 Editor-only 端口注册和抛异常输出，建立运行时节点静态端口形状；后续以此作为原生图与作者面的共同端口定义收口重复声明。
- 本步静态编译已确认新增 Pose 文件无新增类型错误；当前完整 Editor 工程仍被共享工作区既有 `GameplayAbilityDataAsset.cs` 对 `Float32GameplayAbilityExecutionData` 的缺失引用阻断。编译后已执行 `dotnet build-server shutdown`。

本步未删除旧 Pose IR/Image/Compiler 链，也未改共享表现 Host、IK 算法或角色快照；这些删除必须等原生 Pose 消费者切换完成后进行。

## 2026-09-14 r3 原生端口值收口

- `CharacterPoseNativePortValues.cs` 为 FlowCanvas 运行端口提供 Local/Component Pose、动画参数、Discontinuity、Action Playback、Full Body IK Goals/Contribution、Pose History、Trajectory、Presentation Facts 和 Motion Matching Binding 的独立 typed 值。
- `CharacterPoseCanvasNativePorts.cs` 的端口注册改为使用上述运行值，不再使用无数据的 Editor marker；输出通过原生 Runtime 读取，静态端口形状继续覆盖现有 Pose Node Kind 与动态接口端口。
- 静态 Editor 工程本步未产生新增 Pose 类型错误；完整工程仍被共享 Ability 改动的 `Float32GameplayAbilityExecutionData.FromProgram` 缺失引用阻断，编译后已执行 `dotnet build-server shutdown`。

## 2026-09-14 r3 原生图校验门禁

- `CharacterPoseNativeGraphValidator.cs` 增加不依赖 Pose IR 的原生 Graph 校验：检查节点/端口身份、typed 连接、输入单连接、可执行环、根/边界 Output 约束，并递归检查 StateMachine、Pose Subgraph 和 Motion Matching Entry Graph 的引用闭包。
- 校验失败保留 `FailureCode` 与图/节点/端口路径，不能读旧 Image 或默认姿态继续运行。
- Runtime 工程未发现本步新增错误；完整 Editor 依赖链仍被共享 Ability 文件 `GameplayAbilityDataAsset.cs:81` 的 `Float32GameplayAbilityExecutionData.FromProgram` 缺失成员阻断，编译后已执行 `dotnet build-server shutdown`。

## 2026-09-14 r3 角色边界解析收口

- 原生校验不再把序列化的 `m_Role` 当作唯一边界真相；当前资产的目录图角色值仍可能全部为 `AnimGraph`，但根图、状态图和 Body/Linked/Subgraph 边界由图引用关系确定。
- 根图与状态图要求唯一 `Output Pose`；被 StateMachine/子图/Entry 引用的边界图要求唯一 `Graph Output`。这样可以识别角色边界，同时不修改现有 Pose 资产和作者显示命名。
- 本步静态编译通过，0 errors；编译结束已执行 `dotnet build-server shutdown`。尚未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 原生图实例与阶段驱动外壳

- `CharacterPoseNativeGraphRuntime.cs` 建立按准备绑定创建的独立 Graph 实例，使用 FlowCanvas `Manual` 更新模式挂载原生 Runtime，并提供 `BeginFrame -> Prepare -> Evaluate -> Commit/Discard -> Stop/Dispose` 阶段门禁。
- Native Runtime 以 `CharacterPoseNativeFrameLineage`、source demand、typed evaluation output 和 completion identity 串起单帧；节点输出使用 `(NodeId, PortId, Stage)` 缓存，递归读取会被拒绝，不复制全图 ValueLifetime 或旧 Program Image。
- 当前评估器只完成原生端口读取、参数 Get、History 透传和 Graph Output 透传；现有 Player、StateMachine、Slot、BlendStack、Inertialization、Foot/Goal/FBBIK、Final Publication 尚未切换到该评估器，不能把本步记为完整运行时替换。
- Runtime 工程已通过：`dotnet build 3cDemo/Client/3C_Client/ThirdPersonClient.Runtime.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false --no-restore`，0 errors；完成后已执行 `dotnet build-server shutdown`。结果只证明 C# 静态编译，不证明 Unity 资源刷新、Build、Play 或端到端行为。

本步仍未删除旧 Pose IR/Image/Compiler/Worker 链；待全部正式消费者改由原生 Graph Runtime 读取后，再按消费者证据垂直删除，并记录删除后的残留扫描与构建边界。

## 2026-09-14 r3 原生观察与执行边界

- `CharacterPoseNativeGraphRuntime` 增加按 `(NodeId, PortId, Stage)` 保存的节点观察；观察只读取已经产生的输出缓存和完成身份，不重新求值、不创建 Image。
- 删除未接入 Source/Constraint/Publication 的占位 `CharacterPoseNativeGraphEvaluator`，原生 Runtime 现在只接受主装配提供的正式 typed evaluator，不再存在会伪装成完整运行时的空实现路径。
- `ThirdPersonClient.Runtime.csproj` 独立编译通过，0 errors；完整依赖编译仍被共享 Float32 `CharacterSimulationProgramCodec.cs` 的参数类型顺序错误阻断。两次编译结束均已执行 `dotnet build-server shutdown`。
- 节点观察分成当前帧工作页和最后一次成功提交页；Discard、Commit 失败或 Stop 只清理工作页，不覆盖上一帧合法观察。`TryObserve` 只读已提交页，不会重新触发节点求值。
- `CharacterPoseNativeInstanceContext` 增加同版本 `CharacterAnimationRigPayload`，创建实例时强制校验 Rig 与 `CharacterAnimationRigBinding` 的 ID/Revision 一致；native 节点后续可据此分配实例级骨骼/虚拟骨骼缓冲并接入现有 IK 服务。
- 本步独立 Runtime 编译通过，0 errors；首次编译发现并修正的是新增合同自身的引用类型判断，不涉及共享工程错误。编译后已执行 `dotnet build-server shutdown`。
- `CharacterPoseNativeSourceDemand` 拒绝同一图调用内同一节点的重复 Source request，保证一个播放器/资源准备只对应一个节点 demand，不恢复旧全图 Operation index 消重。
- `CharacterPoseNativeGraphRuntime.PrepareChild` 为 State/ControlRig/Linked Pose 子图生成同一资源、Rig 和 EventGraph 输入合同下的独立准备请求；子图后续通过独立 `InstanceId/ResetGeneration` 采用，不复制父图执行计划。
- `CharacterPoseNativeFrameInput` 在合同入口冻结动作命令数组，保证 Prepare 到 Evaluate 期间使用同一帧动作输入；Body/Facts/EventGraph/Pose 数据仍由各自只读合同持有，不复制整份姿态缓冲。

## 2026-09-14 r3 激进裁剪同步

- 并行提交 `a31c27b79` 已删除约 10,974 行 Pose/Timeline 旧编译链，包含 `CharacterPoseCompilerModule`、Pose Closure/Topology/Typed Lowering、ValueLifetime、Stage Schedule、Worker Batch、ProgramImage Seal 及旧 Timeline Semantic Compiler/Emitter；本记录只登记已发生的结构变化，不重复删除这些文件。
- 旧 Runtime 载体仍有残留：`CharacterPoseProgramImage`、`CharacterPoseProgramExecutionView`、`CharacterPoseProgramExecutor`、`CharacterPoseProgramRuntime`、`AnimationPoseNativeWorkspace` 和 Worker Scheduler 仍被 `CharacterPresentationRuntime`/Pose Runtime 引用。它们尚未具备删除证据，下一刀是切换这些消费者到原生 Graph/Source/Constraint/Publication 接口。
- 因此当前状态是“旧编译入口已激进裁掉、旧运行载体正在断链迁移”，不能宣称 Pose Runtime 已完成或旧链已全部删除。

## 2026-09-14 r3 删除旧 ExecutionView 载体

- 删除 `PoseGraph/Program/CharacterPoseProgramExecutionView.cs` 及其 `.meta`；该文件只负责把旧 `CharacterPoseProgramImage` 展开为全图 Operation/Stage/Linked Pose 索引，属于本次要退出的旧载体。
- 删除后静态残留集中在 `CharacterPoseProgramExecutor`、`CharacterPoseProgramRuntime`、`CharacterPoseProgramEvaluationRuntime`、`CharacterPoseProgramActorRuntime`、`CharacterPoseRuntimeComposition` 和 Worker Scheduler；这些消费者下一步统一改接原生 Graph Runtime，不新增同名兼容类型。
- 本步未运行 Unity/Build；当前残留引用是预期的删除后接线状态，尚不能作为完成证据。

## 2026-09-14 r3 删除旧执行页、配置与节点索引

- 删除 `CharacterPoseProgramFramePages`、`CharacterPoseProgramExecutorConfiguration`、`CharacterPoseProgramEvaluationState` 和 `CharacterPoseProgramNodeRuntimeIndex` 及其 `.meta`；它们分别承载旧 Program 的帧页、执行器配置、评估状态和 Node 到 Operation 的索引，属于旧执行链的纯载体。
- 保留现有 `CharacterPoseProgramExecutor`、`CharacterPoseProgramRuntime`、`CharacterPoseProgramEvaluationRuntime`、`CharacterPoseProgramActorRuntime` 等未提交消费者改动，不在本步为已删除类型补兼容壳；下一步继续沿引用残留切换到原生 Graph/Source/Constraint/Publication 链。
- 本步未运行 Unity/Build；删除后的消费者引用残留是激进裁剪过程中的预期中间状态，不能作为完成证据。

## 2026-09-14 r3 删除旧 Program Image

- 删除 `Contracts/Pose/CharacterPoseProgramImage.cs` 及其 `.meta`；该类型只保存旧编译器生成的节点、端口、Operation、Stage 和 Linked Pose 全图描述，不属于原生 FlowCanvas Graph 实例或正式 Pose 数据合同。
- 旧 Runtime、诊断和资源选择代码仍可能引用 `CharacterPoseProgramImage`，这些引用作为下一步消费者断链清单保留，不恢复 Image 或同名兼容类型。
- 本步未运行 Unity/Build；当前状态仍是旧消费者与原生 Runtime 并行断链中的中间状态，不能宣称 Pose Runtime 已完成。

## 2026-09-14 r3 删除旧 Workspace 与 Runtime Composition

- 删除 `PoseGraph/AnimationPoseNativeWorkspace.cs` 及其 `.meta`；该类型是旧 Program 的姿态缓冲、历史页和运行时临时状态容器，不是原生 FlowCanvas 图实例缓存。
- 删除 `PoseGraph/Program/CharacterPoseRuntimeComposition.cs` 及其 `.meta`；该类型负责把旧 Worker、Program Executor、Workspace、Source/IK 模块拼成另一套 Pose 执行组合，属于需要退出的旧编排入口。
- 保留 `CharacterPoseFrameCoordinator` 等现有未提交消费者改动，不为已删除组合补兼容壳；后续由正式 Presentation Host 直接装配原生 Graph Runtime 及 Source/Constraint/Publication evaluator。
- 本步未运行 Unity/Build；当前残留引用是激进删除后的预期断链，不是完成证据。

## 2026-09-14 r3 删除旧 Worker 调度器

- 删除 `PoseGraph/Worker/CharacterPoseWorkerScheduler.cs` 及其 `.meta`；该文件定义旧 Worker Actor 注册、阶段 Lease、Program Batch 和 Scheduler，服务的是旧 Program 的批量执行调度。
- 原生 Graph Runtime 使用实例级 `Manual` 阶段和自己的 frame lease，不恢复旧 Worker 类型或兼容调度层；`CharacterPresentationRuntime`、Tick Targets、Preview 和旧 Program 消费者中的引用作为下一步断链清单保留。
- 本步未运行 Unity/Build；当前消费者引用残留是激进删除的中间状态，不能作为完成证据。

## 2026-09-14 r3 删除旧 Program 状态与资源生命周期模块

- 删除 `CharacterPoseActorState`、`CharacterPoseLinkedFragmentState`、`CharacterPoseProgramActionRuntime`、`CharacterPoseProgramMotionMatchingRuntime`、`CharacterPoseProgramSourcePreparationRuntime`、`CharacterPoseProgramSourceRetirementRuntime`、`CharacterPoseProgramSourceRetirementState`、`CharacterPoseProgramTuningRuntime` 和 `CharacterPoseProgramTuningState` 及其 `.meta`。
- 这些类型把动作播放、Motion Matching、Source Playable 准备/退休、Linked Fragment 和操作权重都绑在旧 Program Image/Frame Pages/Worker 生命周期上；新的 Pose 图应由原生节点实例、Source 服务和唯一 EventGraph 输入分别持有，不恢复旧 Program 状态总线。
- 现有脏的 Program Executor/Runtime/Evaluation/Actor 文件及 Presentation 消费者不在本步改动；它们对已删除类型的引用作为下一步断链清单保留。
- 本步未运行 Unity/Build；当前断链仍是激进删除的中间状态，不能作为完成证据。

## 2026-09-14 r3 删除旧 Operation 页与值页

- 删除 `CharacterPoseNativeOperationPages`、`CharacterPoseInertializationOperationModule`、`CharacterPoseLinkedOperationModule`、`CharacterPoseManagedValuePage`、`CharacterPosePlayerOperationModule`、`CharacterPosePureMath`、`CharacterPoseStateOperationModule` 和 `CharacterPoseValuePageSlice` 及其 `.meta`。
- 这些类型是旧 Operation 表的页存储、输入输出值页、播放器/状态/Linked/Inertialization 操作适配器和配套数学入口；原生 Graph 不再通过 Operation code、Value page 或 Worker batch 执行节点。
- 保留外部 `Constraints`、`Final` 和现有 Inertialization/IK 服务文件；它们是算法/发布边界，不随旧 Program 操作适配器一起删除。旧 Executor/Runtime 的脏引用不在本步补兼容。
- 本步未运行 Unity/Build；当前断链仍是激进删除的中间状态，不能作为完成证据。

## 2026-09-14 r3 删除旧 Worker Plan 与 Operation Pages

- 删除 `Contracts/Pose/CharacterPoseWorkerPlan.cs` 和 `CharacterPoseOperationPages.cs` 及其 `.meta`；前者是可序列化的 Worker 批次/值范围/骨骼执行布局，后者是全图 Operation header、payload page 和 Value reference 表。
- 原生 FlowCanvas 图的 Node/Port/Connection 与 `CharacterPoseNativeGraphValidator` 直接确定执行拓扑，不再生成或读取这两种全图执行描述。
- 诊断、Projection 和旧 Program 消费者的引用暂不补兼容；它们将在切换原生观察/发布合同时删除或改为节点观察数据。
- 本步未运行 Unity/Build；当前残留引用是激进裁剪过程中的预期中间状态，不能作为完成证据。

## 2026-09-14 r3 删除旧 Worker Kernel 与 Operation 适配器

- 删除 `PoseGraph/Worker/CharacterPoseWorkerKernels.cs`、`CharacterPoseWorkerActorSlice.cs`、`PoseGraph/Constraints/CharacterPoseConstraintOperationModule.cs`、`PoseGraph/Final/CharacterPoseOutputOperationModule.cs` 和 `PoseGraph/Diagnostics/CharacterPoseProgramCommittedDiagnosticsProjector.cs` 及其 `.meta`。
- 这些文件只把旧 Operation header、Value page、Worker batch 接到节点执行；Constraint/Final 的正式能力仍由 `CharacterPoseConstraintRuntime`、`CharacterFinalPosePublication` 和现有 Foot/Goal/FBBIK 服务负责，不删除它们。
- 旧 Program Executor/Evaluation 与诊断消费者的引用继续作为断链清单保留，不建立新的 Worker 或 Operation 兼容层。
- 本步未运行 Unity/Build；当前断链仍是激进裁剪中间状态，不能作为完成证据。

## 2026-09-14 r3 删除旧 Pose 协调器

- 删除 `PoseGraph/CharacterPoseTuningCoordinator.cs` 和 `CharacterPoseMotionMatchingCoordinator.cs` 及其 `.meta`；前者只把旧 Program Runtime 接到调参入口，后者只把旧 Program frame lease 接到 Motion Matching 完成/回放入口。
- Tuning 继续使用正式的 Tuning Binding，Motion Matching 继续由 `CharacterMotionMatchingPresentationModule` 和原生 Pose 节点输入合同负责；不恢复旧协调器或双重状态。
- 本步未运行 Unity/Build；现有 Presentation Host 的旧引用仍作为下一步断链清单，不能作为完成证据。

## 2026-09-14 r3 移除残留 Image partial 扩展

- 从 `CharacterMotionMatchingPosePlan.cs` 和 `CharacterLinkedPosePosePlan.cs` 移除 `CharacterPoseProgramImage` 的 partial 字段、属性和旧 Operation/Stage 校验扩展；保留 Motion Matching/Linked Pose 的独立 descriptor 类型，因为它们仍属于资源/接口合同。
- 删除后这些 descriptor 不再反向恢复旧 Image 类型；它们由原生图节点和对应的 Motion Matching/Linked Pose 服务按节点/接口身份消费。
- 本步未运行 Unity/Build；Projection、诊断和旧 Presentation Host 的 Image 引用仍待后续改为原生图/节点观察数据。

## 2026-09-14 r3 删除孤立旧 Tuning Snapshot

- 删除 `PoseGraph/CharacterPoseTuningSnapshot.cs` 及其 `.meta`；该文件只组合已退出的 Program/Source/Constraint 调参 view，且没有外部消费者。
- 调参状态继续由正式 Tuning Binding/Parameter Block 持有，不恢复旧的 Pose 总调参快照。
- 本步未运行 Unity/Build；没有涉及共享 Host、Projection、IK 算法或现有未提交改动。

## 2026-09-14 r3 删除旧 Pose Diagnostics facade

- 删除 `PoseGraph/Diagnostics/CharacterPoseDiagnosticsRuntime.cs` 及其 `.meta`；该类型只把旧 Program/FrameResult/ConstraintResult 组合后喂给旧 Snapshot Publisher，不拥有快照数据或 IK 算法。
- Snapshot Publisher 文件暂留，后续改为消费原生节点观察与正式 Source/Constraint/Publication 结果；不恢复旧 Program Diagnostics facade。
- 本步未运行 Unity/Build；Presentation Diagnostics Coordinator 的旧引用保留为后续断链。

## 2026-09-14 r3 删除旧 Pose 诊断事件 facade

- 删除 `Presentation/CharacterPoseCommittedDiagnosticsEventPublisher.cs`、`CharacterPoseCommittedDiagnosticsEventPublisher.Foot.cs` 及其 `.meta`；两者只把旧 Program/Frame/Constraint/Final 结果转换为事件，原生节点观察已拥有节点/阶段/提交身份。
- 保留快照数据结构与现有正式 IK/Final Publication 服务；旧 Presentation Diagnostics Coordinator 的引用作为后续原生观察接线清单，不恢复旧 Program 诊断事件链。
- 本步未运行 Unity/Build；当前断链仍是激进裁剪的中间状态。

## 2026-09-14 r3 激进删除批次执行收据

- Scope：退出 Pose 专属 IR、Program Image/ExecutionView、全图 Operation/Value/Worker 编排、旧 Program 状态生命周期和旧 Pose 诊断 facade；保留 FlowCanvas 原生图合同、节点/端口校验、Source/Constraint/Foot/Goal/FBBIK、Final Publication、共享快照/Projection 和所有并行脏文件。
- Retired：本轮连续小步删除了旧执行描述、页、调度器、Kernel、操作适配器、旧协调器、Tuning Compiler、Image partial 扩展和旧诊断 facade；没有新增 fallback、兼容 reader 或第二运行时路径。
- Artifacts：每个删除均同步移除对应 `.meta`；仅维护本文件，删除提交按职责拆分为 `98bf408e5`、`eefcb4111`、`72a2a92d0`、`91ebda04d`、`194bd230b`、`042d2c1ec`、`e189ce453`、`d70759d79`、`ccf98bb1b`、`d1c097cda`、`36b446883`、`f7cc5b126`、`934c9b7b6`、`13eb38a76`。
- Verification：通过当前工作树的精确 `rg` 残留扫描、目标文件状态检查和每步暂存范围审计；按当前约束未运行 Unity、Build、Play 或资源刷新，因此这些检查只证明删除边界，不证明运行时闭环。
- Residual risk：旧 Presentation Host、Projection、Snapshot Publisher 和少量并行脏消费者仍引用已退出类型；原生 evaluator 尚未接入 Source/Constraint/Final Publication，当前不能宣称 3.1—3.18 全部完成。
- Undo：这些是源文件与 `.meta` 的 Git 删除，可按上述 Pose 提交逆序逐个 revert；不要回退或覆盖同期间的并行提交。

## 2026-09-14 r3 统一 NativePorts 形状来源

- `CharacterPoseCanvasNativePorts.Shape` 现在始终返回同一份 `RuntimeShape`；删除编辑器专用 `PoseCanvasEditorBridge.PortShape` 分支和注册，FlowCanvas 的编辑端口、连接类型、索引与运行端口不再各自投影一份形状。
- 保留 `CharacterPoseAuthoringPortProjection` 供正式 Mutation/Clipboard/作者字段规则使用，但它不再成为 FlowCanvas 运行端口的第二来源；节点的 typed value 仍由 NativePorts 注册表创建。
- 本步保留并行已有的 NativePorts/Graph 泛型约束改动；未运行 Unity/Build，下一步仍需把原生 evaluator 接到正式 Source/Constraint/Final 服务。

## 2026-09-14 r3 建立原生节点 evaluator 注册边界

- 新增 `PoseGraph/CharacterPoseNativeGraphEvaluator.cs` 及其 `.meta`；FlowCanvas Runtime 只负责图生命周期、端口读取和阶段门禁，节点语义通过 `ICharacterPoseNativeNodeHandler` 显式注册，避免把第二套节点执行器写回 Graph Runtime。
- evaluator 已直接实现 EventGraph 参数 Get 和 Action Playback 输入，并能从真实 GraphOutput/OutputPose 的 ValueInput 返回 typed 结果；Player、State、Blend、Inertialization、Foot/Goal/FBBIK、Linked Pose 和 Final Publication 没有注册时明确失败，不返回零值、旧缓存或默认姿态。
- Source demand、EvaluateFrame、Commit/Discard/Stop/Dispose 都沿 handler 注册表传播；handler 状态属于注入的领域服务，Graph Runtime 不复制 Source/Constraint/Final 状态。
- 本步未运行 Unity/Build；当前还缺少正式服务 handler 与角色 Host 的 Create/Replace 接线，不能把 evaluator 注册边界记为完整 Pose Runtime。

## 2026-09-14 r3 暴露显式 handler 创建入口

- `CharacterPoseNativeGraphRuntime` 增加只接受 `ICharacterPoseNativeNodeHandler` 列表的 `Create` 重载；角色装配必须明确提供节点领域服务，缺失 handler 仍在图初始化时失败。
- 原有直接注入 `ICharacterPoseNativeNodeEvaluator` 的入口保留，两个入口都不生成默认 evaluator、不恢复旧 Program reader，也不切换第二运行路径。
- 本步未运行 Unity/Build；正式 Source/Player/State/Blend/Constraint/Final handler 和共享 Host 接线仍未完成。

## 2026-09-14 r3 删除旧 PortShape bridge 字段

- 删除 `PoseCanvasEditorBridge.PortShape` 字段；编辑器 Hook 不再注册它，`NativePorts` 已统一从 `RuntimeShape` 取形状，避免留下可被重新接回的第二端口投影入口。
- Graph/Node 其它作者 UI 与正式 Mutation 入口保持原样；本步不把作者规则投影误当成运行端口定义。
- 本步未运行 Unity/Build；节点领域 handler 及共享 Host 接线仍未完成。

## 2026-09-14 r3 收口实例 Reset 生命周期

- `CharacterPoseNativeResetResult` 增加 Reset 的 typed 成功/失败结果；同一实例只接受严格递增的 `ResetGeneration`。
- `CharacterPoseNativeGraphRuntime.ResetInstance` 在重置前丢弃在途帧，调用所有 handler 的 Reset，再清空本帧/已提交输出与观察，防止旧代际结果继续被读取；Reset 失败不恢复旧输出。
- handler/evaluator 注册合同同步增加 Reset；Stop/Dispose 仍负责终止图和释放资源，Replacement 继续通过新 Prepared/Adopted 实例完成，不复用旧实例身份。
- 本步未运行 Unity/Build；正式 handler 与角色 Host 尚未接入，不能宣称 3.15 完成。

## 2026-09-14 r3 建立 Replacement 原语

- `CharacterPoseNativeGraphRuntime.Replace` 先创建并启动新实例，只有新实例实际 Adopted 后才停止并释放旧实例；新建或旧实例停止失败都会销毁新实例并返回 typed 失败。
- Replacement 不修改旧实例的 ResetGeneration，不共享节点缓存，不回滚 EventGraph 输入，也不保留旧实例作为兼容路径。
- 本步未运行 Unity/Build；共享 Host 尚未调用该原语，不能宣称角色替换链已完成。

## 2026-09-14 r3 原生阶段接线收据

- 本轮新增的原生阶段能力已提交为 `333a3a1a4`、`6a28afae6`、`5bb83d3af`、`15631b7b5`、`4663e6d76`、`597f31ec4`；目标文件当前无未提交差异，暂存区为空。
- 当前 `rg` 结果显示 `CharacterPoseNativeGraphRuntime.Create` 只有 Runtime 工厂重载自身，没有角色 Host 的实际调用点；因此只能确认合同、handler 注册、Reset/Replace 和 FlowCanvas 生命周期已建立，不能确认角色运行闭环。
- 未运行 Unity、Build、Play、资源刷新或端到端验证；下一步必须由现有 Source/Player/State/Blend/Constraint/Foot/Goal/FBBIK/Final 服务提供真实 handler，并由共享 Host 安装调用。

## 2026-09-14 r3 接线阻断事实收据

- 当前 Pose 侧已提交 `333a3a1a4`、`6a28afae6`、`5bb83d3af`、`15631b7b5`、`4663e6d76`、`597f31ec4`；截至本次扫描，原生 `Create/Replace` 没有任何角色 Host 调用点，handler 也没有除参数/action 内置实现之外的正式服务实现。
- 不能通过添加空 handler、默认 Pose、旧 Program reader 或伪造调用点消除该缺口；否则会把 Source Pending、IK/Final Publication 和真实采用状态隐藏成成功。
- 该事实不是不可推进的外部阻断：下一步可在不改共享 Host 的前提下继续实现具体领域 handler；完成证据仍要求真实 Host 调用、静态残留清理和用户侧 Unity/Play 验收。

## 2026-09-14 r3 补齐 required input 校验

- `CharacterPoseNativeGraphValidator` 现在在类型、重复连接和环检查前，逐节点检查所有 `Required` 输入是否确实有一条连接；悬空必需输入返回带图/节点/端口路径的 `PortInvalid`。
- 可选输入仍由节点/服务自己的正式配置处理；本步没有补零值、默认 Pose 或隐式连接。
- 本步未运行 Unity/Build；当前服务 handler 与角色 Host 接线缺口不变。

## 2026-09-14 r3 删除孤立 Pose Tuning Compiler

- 删除 `Editor/CharacterSimulation/Compilation/Presentation/CharacterPoseTuningParameterCompiler.cs` 及其 `.meta`；该入口没有外部引用，职责只是从旧 Program Image 的 Operation/Weight 表生成调参布局。
- 正式 Tuning Binding/Parameter Block 不再依赖旧 Pose Compiler；并行 Projection Compiler 的脏改动不在本步触碰。
- 本步未运行 Unity/Build；残留引用扫描以当前并行消费者状态为准。

## 2026-09-14 r3 按节点实例注册原生 handler

- `CharacterPoseNativeGraphEvaluator` 的 handler 索引由 `CharacterPoseNodeKind` 改为 `PoseNodeId`；同一种节点可以拥有多个独立实例，不再共享一个按类型索引的隐式状态容器。
- Parameter Input 与 Action Playback Input 只在实际图节点初始化时各自创建内置 handler；外部 handler 必须声明真实节点身份和节点类型，孤立 handler、边界 handler 和类型错配都会在图初始化时失败。
- 这一步只修正原生节点实例边界，没有添加空的 Source/Constraint/Publication handler，也没有把旧 Program reader 接回去；正式服务接线仍是后续独立步骤。
- 未运行 Unity、Build、Play 或资源刷新；只完成源码边界和执行记录更新，验证范围不包含运行时闭环。

## 2026-09-14 r3 增加原生 Pending 验证阶段

- `CharacterPoseNativeGraphRuntime` 增加独立 `ValidatePending` 阶段；Evaluate 的成功结果必须先经过 evaluator/节点验证，Commit 不再直接接受未验证的候选姿态。
- 验证结果带同一图实例、帧完成身份、阶段和 typed 失败原因；验证失败仍由调用方走 Discard，不会把候选结果当成已提交姿态。
- handler 生命周期同步增加 `ValidatePending`，为后续 Source/Constraint/Final Publication 的真实整帧校验保留唯一接点；当前内置 Parameter/Action handler 无需额外校验。
- 未运行 Unity、Build、Play 或资源刷新；仅完成源码阶段门禁和执行记录更新。

## 2026-09-14 r3 收回合同层的具体服务依赖

- 通用 `CharacterPoseNativeInstanceContext` 不再持有具体 `CharacterPoseSourceModule`、`CharacterPoseConstraintRuntime` 或 `CharacterFinalPosePublication`；这些实现类不能反向进入 Pose Contracts。
- 原生 `Create` 已有 per-node handler 注入边界，具体 handler 负责持有其正式领域服务引用；服务不从全局查找、不复制状态，通用 Context 只负责 Actor、Rig、Graph 和运行环境身份。
- 本次修正保持子图实例 API 与旧 Program 退出方向，不宣称三类服务已经被节点 handler 调用；旧 Program 仍未接回。
- 未运行 Unity、Build、Play 或资源刷新；仅完成源码边界和执行记录更新。

## 2026-09-14 r3 接通子图 GraphInput 的 typed 帧绑定

- 原生 runtime 增加 `BeginFrame(input, completionIdentity)`，子图可以沿用父调用的同次完成身份，但仍保留自己的 Graph、InstanceId 和节点缓存。
- `GraphInput` 现在按实际边界创建内置 handler；父调用方可在子图 Prepare 前绑定 typed `Local/Component/Parameter/Goal` 等输入，子图只在当前阶段读取绑定值，缺失、重复或类型不符直接失败。
- GraphInput 值必须匹配子图当前 completion identity，避免父帧值晚到或跨调用实例泄漏；绑定只存帧内引用，Close/Reset/Dispose 时清空。
- 未运行 Unity、Build、Play 或资源刷新；仅完成原生子图输入边界和执行记录更新。

## 2026-09-14 r3 接通 PoseSubgraph handler 的真实调用生命周期

- 新增 `CharacterPoseNativeSubgraphHandler`，在初始化时通过明确的 child handler factory 创建并启动子图实例；子图不是父图的共享节点集合，也不通过旧 Operation index 解析。
- Prepare 阶段先以父调用的 completion identity 打开 child frame，按 `InterfacePortId` 把父 Call 的输入端口绑定到 child GraphInput，再把 child 的真实 source demand 返回给父图。
- Evaluate、ValidatePending、Commit、Discard、Reset、Stop 和 Dispose 均沿 child runtime 的 typed 结果执行；child GraphOutput 按接口端口映射回父 Call 输出，任何缺失或错配都会失败。
- 本步建立了可复用子图调用路径，但尚未为 Player/State/Blend/Constraint/Final 节点提供正式 handler；未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 固化 Barrier 前 Job 准备接点

- 原生 runtime 增加 `PrepareEvaluation`，顺序固定为 `PrepareFrame -> PrepareEvaluation -> Animancer Barrier -> Evaluate -> ValidatePending -> Commit`。
- handler 可在唯一 Barrier 前安装或更新自己的 Native/Playable Job；Barrier 后的 `EvaluateFrame` 只消费已完成的同次结果，不再把 Job 安装延迟到求值之后。
- `CharacterPoseNativeSubgraphHandler` 把该接点递归传给 child runtime，父子图共享同一 barrier identity，但不共享执行缓存。
- 未运行 Unity、Build、Play 或资源刷新；Player/State/Blend/Constraint/Final 的具体 Job 和服务 handler 仍待接入。

## 2026-09-14 r3 接入 ClipPlayer 原生 Job 与单节点 buffer

- `AnimationPlayerPoseNativeWriteBinding` 增加直接由 Native slice 构造的入口；新增 `CharacterPoseNativeNodePoseBuffer`，每个 Player handler 独占双页姿态、参数、贡献、脚特征和完成标记，不再依赖旧全图 aggregate，上一已提交页不会在下一帧被覆盖。
- 新增 `CharacterPoseNativeClipPlayerHandler`：Prepare 生成真实 Clip source capture 和 demand，`PrepareEvaluation` 通过明确的 Source binding 调用 `CharacterPoseSourceModule` 并在 Barrier 前安装现有 `AnimationSelectedPosePlayerJob`，Barrier 后读取完成的单节点 Pose，随后沿 typed Validate/Commit/Discard 生命周期收口 Player 状态。
- `CharacterPoseNativeClipSourceModuleBinding` 只持有当前 Source frame provider 和 binding index，复用唯一 Source module 的 ACL/Playable 注册、资源解析和连接；不复制 source 状态，不创建 direct Play 旁路。
- 本步只完成 Clip Player；BlendSpace、Selected、State、Slot、BlendStack、Inertialization、Constraint 和 Final handler 仍未接通。未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 接通单节点 Pose 到 Final Publication 的读绑定

- `AnimationPoseValueNativeReadBinding` 增加从 `CharacterPoseNativePoseReadBinding` 构造的只读视图，`CharacterFinalPosePublicationOutputBinding.WriteNativePose` 可以直接消费单节点 Native slice。
- Final Publication 仍是唯一骨骼/属性写入者；这一步只复用现有整 Rig 校验、贡献展开和双页发布，不让节点直接写 Transform，也不复制 Pose 数据页。
- 旧 Program executor 的读绑定仍作为待清理消费者保留，新的 Native handler 使用独立 `WriteNativePose` 接口；本步未宣称 Final handler 已安装到角色 Host。
- 未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 增加子图实例创建入口

- `CharacterPoseNativeGraphRuntime.CreateChild` 复用父实例已经确认的 Graph Asset、Profile、Rig 和 EventGraph 输入合同，只替换子图身份、实例身份和 reset 代际；具体 Source/Constraint/Final 引用仍由子图 handler 注入。
- 子图准备失败返回 typed adopted failure；子实例仍通过同一 `Create` 路径初始化、启动和注入 per-node handler，禁止把子图折叠成父图共享状态或旧 Program 索引。
- 子实例的停止、Discard、Reset 和 Dispose 仍由持有它的 `PoseSubgraph/StateMachine/Linked Pose` 节点 handler 管理；本步没有增加全局 child registry 或第二个生命周期 owner。
- 未运行 Unity、Build、Play 或资源刷新；仅完成子图实例边界和执行记录更新。

## 2026-09-14 r3 接入 BlendSpacePlayer 原生 Job 与单节点 buffer

- 新增 `CharacterPoseNativeBlendSpacePlayerHandler`；它沿用 ClipPlayer 的同一 `PrepareFrame -> PrepareEvaluation -> Barrier -> Evaluate -> Validate/Commit/Discard` 链，输入参数和 BlendSpace sample phase 仍由现有 BlendSpace runtime 负责。
- `CharacterPoseNativeBlendSpaceSourceModuleBinding` 通过唯一 Source module 的 BlendSpace readiness、ACL/Playable backend 和 physical connection 取得真实 binding，输出复用现有 `AnimationSelectedPosePlayerJob`，不新增 BlendSpace 求解或 direct Play 路径。
- Clip 与 BlendSpace 各自使用节点级 buffer/Player 实例；共享的是既有 Job/Source 算法，不共享可变时间、权重、relevance 或输出页。
- 本步仍未接入 Selected Player、PoseStateMachine、AnimationSlot、BlendStack、Inertialization、Constraint、Final Publication 和共享 Host；未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 接入 SelectedPosePlayer 原生 Job

- 新增 `CharacterPoseNativeSelectedPosePlayerHandler` 与 Source module binding；Selected 节点从注入的 Motion Matching sample 建立选择、capture、ACL/Playable source binding 和现有 `AnimationSelectedPosePlayerJob`，不复制 Motion Matching 选择状态。
- Selected 节点的 `pose` 与 `discontinuity` 均从独占双页 Native buffer 读取，沿同一 `PrepareEvaluation -> Barrier -> Evaluate -> Validate/Commit/Discard` 生命周期收口。
- 本步仍未接入 StateMachine、AnimationSlot、BlendStack、Inertialization、Constraint、Final Publication 和共享 Host；未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 接入 Local/Component Pose 空间转换节点

- 新增 `CharacterPoseNativeSpaceConversionHandler`；Local→Component 和 Component→Local 都按共享 Rig 的父骨骼索引调用现有 `CharacterPoseConstraintMath`，不把空间转换退化成同一数组的无标记透传。
- 原生 Pose read binding 增加 `CharacterPoseSpace`，Local/Component 端口现在会拒绝错误空间；转换节点复用节点级双页 buffer，并保留参数、贡献、脚特征、连续性和完成身份。
- 本步未运行 Unity、Build、Play 或资源刷新；Blend、Layered、Additive、State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-14 r3 对齐原生 handler 阶段合同

- `ICharacterPoseNativeNodeHandler` 补齐 `PrepareEvaluation` 与 `ValidatePending`；evaluator、子图、Clip Player 和 BlendSpace Player 现在共享同一套 Barrier 前准备与提交前校验合同。
- 这只是收口现有实现与接口的断裂，不改变节点身份、Source/Constraint/Final 所有权，也没有把旧 Program 执行器接回原生图。
- 本步未运行 Unity、Build、Play 或资源刷新；没有提交 LFS。

## 2026-09-14 r3 接入 BlendPose 原生值节点

- 新增 `CharacterPoseNativeBlendPoseHandler`；它读取两个 Local Pose 和可选 Float32 权重，在原生节点 buffer 中完成骨骼变换、速度、参数、贡献和 Foot 特征混合，保留每节点连续性身份。
- 权重未连线时使用节点自身的正式字段；连线时只接受 EventGraph typed Float32 Get，不创建 Pose 专属变量或隐式类型转换。
- 本步未运行 Unity、Build、Play 或资源刷新；Layered、Additive、State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-14 r3 接入 AdditivePose 原生值节点

- 新增 `CharacterPoseNativeAdditivePoseHandler`；它按正式 RigReference/Local 参考姿态计算位置、旋转、Scale policy 和速度增量，输入权重仍只接受节点字段或 EventGraph Float32 Get。
- Additive 不把增量分支伪装成新的 Live source contribution，保留 Base 的来源、Foot 特征和参数元数据，避免 Foot/Final 把增量误判为独立源。
- 本步未运行 Unity、Build、Play 或资源刷新；Layered、State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-15 r3 隔离画布节点复制数据

- `CharacterPoseCanvasNode.CloneAuthoring` 与编辑器 `DuplicateNode` 现在深复制节点 Payload 和 DynamicPort 对象，复制图或节点后修改作者字段不会污染原对象。
- 提交为 `3d4b08bd3`、`3447372af`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 让原生 Managed 节点工厂接收实例上下文

- `CharacterPoseNativeManagedNodeRegistration` 的 StateMachine、Linked Pose、Motion Matching、History、Entry、Subgraph 和 Root Orientation 工厂现在都接收 `CharacterPoseNativeInstanceContext`。
- `Creator`、`PreparedCreator` 和共享 source/buffer 创建路径会把同一图调用实例上下文传到资源、历史、子图身份和节点缓冲工厂，避免按节点身份创建跨 Actor 或跨调用实例的隐式共享对象。
- 本步只收紧原生 handler 装配契约，不修改 State/Linked/Motion Matching 算法，也不伪造角色 Host 调用点；共享 Host 接线仍是独立待办。
- 本步未运行 Unity、Build、Play 或资源刷新；没有提交或暂存 LFS 文件。

## 2026-09-14 r3 收口全部原生节点工厂的实例上下文

- `CharacterPoseNativeSourceNodeRegistration` 将 Clip、BlendSpace、Selected、BlendStack 和 AnimationSlot 的 player、stack、sample、binding index 与 Native buffer 工厂改为接收 `CharacterPoseNativeInstanceContext`。
- `CharacterPoseNativeConstraintNodeRegistration` 将 Foot、Goal、FBBIK 的约束句柄和输出 buffer 工厂改为接收同一实例上下文；`CharacterPoseNativePureNodeHandlerRegistration` 与 `CharacterPoseNativeInertializationNodeRegistration` 同样把 buffer、Bone Mask 和 policy 工厂绑定到实例。
- `BlendStack` 与 `AnimationSlot` 的 Action/Provider sample provider 也接收实例上下文，再传给既有 Source binding，避免相同作者节点在不同子图调用实例中取到错误 sample。
- 这四步只收紧注册表到 handler 的装配契约，不改变 Source/Blend/IK/Inertialization 算法，也不新增角色 Host 或第二执行链；共享 Host 的实际调用点仍待并行 owner 接线。
- 对应提交为 `9bf0646be`、`a179cf52a`、`ff8e44ff0`、`710fea12f`、`15a6c44c8`；只提交普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 收紧原生图与 Actor 实例身份

- `CharacterPoseNativeNodeHandlerRegistry.Create` 现在要求 `PreparedBinding.ActorId` 与 `CharacterPoseNativeInstanceContext.ActorId` 完全一致，再创建任何节点资源、历史或 Native buffer。
- 该校验阻止一个角色的准备图被装配到另一个角色实例；不改变节点算法、共享 Source/Constraint owner 或 Host 生命周期。
- 提交为 `7db543a46`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 修正 Linked Pose 接口边界校验

- `CharacterPoseNativeGraphValidator` 将 `LinkedPoseCall` 纳入接口边界，允许其正式动态端口携带并校验 `InterfacePortId`。
- 这修正合法 Linked Pose 调用被误判为普通节点接口身份非法的问题；实际 Linked Pose 签名解析仍复用既有正式 owner，没有新增映射或兼容路径。
- 提交为 `4d368145d`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 校验复用 PoseGraph 的边界角色

- `CharacterPoseNativeGraphValidator` 将 `visited` 从单纯的 `GraphId` 集合改为 `GraphId -> BoundaryKind`，相同边界的复用只校验一次，跨 Root、State、Boundary 角色复用直接报告冲突。
- 这样不会因去重而跳过第二种入口合同的校验，同时保留同一图在同一正式边界下被多个调用点复用的能力。
- 提交为 `407db6a03`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 收紧画布连接端点身份

- `CharacterPoseCanvasGraph.RequireValid` 现在确认每条连接的 source/target 节点属于当前图，并且序列化 NodeId 与实际端点对象一致。
- 陈旧端点、跨图 Connection 会在进入 NativePorts 或 FlowCanvas typed bind 前失败；不改变连接的作者 Mutation 或运行时求值语义。
- 提交为 `eb992392b`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 校验 Entry Pose 位于最终输出路径

- `CharacterPoseNativeGraphValidator` 对含 `EntryPoseInput` 的 Boundary 图做反向可达性检查，要求 Entry Pose 实际连接到 `GraphOutput`，而不是只声明节点后被求值依赖图忽略。
- 提交为 `51b0e2e78`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 沿 FlowCanvas 停止原生子图

- `CharacterPoseNativeSubgraphHandler.Stop` 现在优先调用 child FlowCanvas `Graph.Stop(false)`，让 child 的 `isRunning`、节点停止回调和 Native Runtime 生命周期同步结束。
- child Graph 已停止时才直接调用 `StopInstance` 收束 Runtime；不新增第二套停止路径或兼容执行链。
- 提交为 `ac078c5ee`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 失效停止后的子图帧句柄

- `CharacterPoseNativeSubgraphHandler.Stop` 在停止 child Graph 后同步清除 child lease、准备结果和求值结果，停止帧不能再进入提交路径。
- 提交为 `7d17cbf17`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 统一 Boundary 输入诊断

- Native Boundary 图的错误信息现在明确允许 `GraphInput` 或 `EntryPoseInput`，与实际边界计数规则一致。
- 提交为 `a2fe9d4fe`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-15 r3 限制参数 Get 只读入口

- `CharacterPoseCanvasEditorWriteSession.CreateParameterGet` 复用 `CharacterPoseParameterAccess.IsBlackboardInput`，只允许 EventGraph Control 变量创建 Pose Get。
- Foot Placement、Action 和 BlendShape 等曲线或播放事实不能再从程序化 Mutation 入口伪装成 Blackboard Get；提交为 `b861930d0`，只包含普通 C#，没有提交或暂存 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 接入原生子图签名校验

- `CharacterPoseNativeGraphValidator` 在递归检查子图前复用正式 `CharacterPoseSubgraphSignatureValidator`，校验调用节点声明的输入/输出端口与被调用图边界完全一致。
- 子图校验继续沿同一 `CharacterPresentationPoseGraphAsset` 解析并检测递归访问；没有新增旧 IR、Program 或 fallback 路径。
- 本步只修改原生图校验代码，未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 收紧 Pose Blackboard 可读变量分类

- 将 Blackboard 投影改为复用 `CharacterPoseParameterAccess.IsBlackboardInput`；`ActionWeight`、`FootPlacementWeight` 和 `animation.blendshape.*` 不再伪装成普通 EventGraph 控制变量。
- 普通动画实例控制量继续由 FlowCanvas 原生 Blackboard 投影并只能创建只读 Get；Foot 曲线与 Action 播放事实留在各自正式消费者。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 收紧 Motion Matching Entry 图边界

- 原生图校验在递归进入 Motion Matching Entry 图前要求恰好一个 `EntryPoseInput`，避免普通 `GraphInput` 通过静态校验后才在运行期失败。
- Entry 图仍沿现有 `EntryPoseInput -> GraphOutput` 入口和同一原生生命周期处理，没有新增第二套输入或兼容路径。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 记录 Motion Matching Entry 边界实现

- `CharacterPoseNativeGraphValidator` 已实际接入 Motion Matching Entry 图边界检查，并继续使用同一图资产递归校验路径。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 校验 Boundary 图输出端口

- 原生 Boundary 图校验现在要求 `GraphOutput` 至少声明一个输入端口；空输出子图在实例创建前即失败，不延迟到 Evaluate。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 校验原生连接端点身份

- `CharacterPoseCanvasConnection.Bind` 在调用 FlowCanvas typed binding 前，要求实际 Node/Port 端点与序列化业务身份一致；陈旧或错绑连接直接失败。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 修正 EntryPoseInput 接口边界分类

- `EntryPoseInput` 的固定 `pose.local` 不再被误判为外部接口端口；只有 `GraphInput`、`GraphOutput` 和 `PoseSubgraph` 检查 `InterfacePortId`，并拒绝重复接口身份。
- 合法 Motion Matching Entry 图不再因固定入口缺少接口 ID 而被原生校验错误拒绝。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 建立原生节点组合注册表

- 新增 `CharacterPoseNativeNodeHandlerRegistry`，按 `CharacterPoseNodeKind` 注册并创建节点 Handler；`GraphInput`、参数 Get、Action 输入和输出边界继续由原生评估器处理。
- 注册表只负责实例装配，Source、State、Slot、Constraint、Linked Pose 等服务由调用方注入；子图复用同一注册表，不复制图数据或执行链。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 收紧原生 Handler 装配身份

- 注册表在返回 Handler 前校验 NodeId 与 NodeKind 必须匹配当前图节点；错误实例立即释放，不延迟到 Graph Initialize 才发现。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 注册 Pose 纯值原生节点

- 新增纯值节点注册适配：`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`ModifyBone`、`PoseParameterResolve`、Local/Component 空间转换均由同一 Handler 注册表创建。
- 每个节点的 Native Pose buffer 由实例装配方提供；Rig 与 Bone Mask 显式注入，构造失败会释放已申请的 buffer，不引入全图 Workspace 或旧 Program 计划。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 统一编辑器与运行时端口投影

- `CharacterPoseAuthoringPortProjection.Get` 改为直接读取 `CharacterPoseCanvasNativePorts.GetRuntimeShape`；编辑器连接、重连和字段前的实际端口形状与 FlowCanvas 原生端口一致。
- Node Definition 仍保留字段、动态端口和作者能力校验，不再同时维护一份固定运行端口形状。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 注册 Pose 惯性化原生节点

- 注册表新增 `Inertialization` Handler 创建入口；其 Policy 与每实例 Native buffer 由调用方显式提供，保留独立的历史与残差状态。
- 惯性化不被归类为无状态纯值节点，也不重新引入旧 `PoseInertializationNativeProgram` 全图编排。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 Pose 原生 Source 节点组合

- 新增 Source 注册适配，将现有 `CharacterPoseSourceModule` binding 接入 `ClipPlayer`、`BlendSpacePlayer`、`SelectedPosePlayer` Handler；播放器、Source lease、binding index 和实例 buffer 均显式注入。
- Handler 创建失败会释放当次播放器与 buffer；Source 资源所有权仍由既有 Source 模块持有，不创建 direct Play、资源副本或第二播放器。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 Pose 原生 Slot 与 BlendStack

- Source 注册适配新增 `BlendStack` 与 `AnimationSlot`；复用现有 StackRuntime、Action/Provider sample、Source lease 和 Slot source binding。
- Animation Slot 的内部 Stack buffer 与最终输出 buffer 分开按实例申请；提交/丢弃/释放沿现有 Handler 和 Stack 生命周期，不复制 Action 或 Source 播放器。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 Pose 原生 Constraint 节点组合

- 新增 Constraint 注册适配，将现有 Constraint service/handle 接入 `FootPlacement`、`PoseBoneIKGoals`、`FullBodyIkGoalAssembler` 与 `FullBodyIK` Handler。
- FBBIK 输出 buffer 由实例装配方独占；handle 由正式调用方解析，IK 算法、Foot 状态和 Goal 状态仍由 Constraint owner 持有。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 Pose 原生托管节点组合

- 新增托管节点注册适配，将 StateMachine、Linked Pose、Motion Matching、History Collector、Entry Pose 的现有服务接口接入 Handler Registry。
- 每个调用实例独立创建 source 与 Pose buffer；状态转换、调用共享、History 和 Entry 业务仍由注入服务持有，不复制旧 Program 状态。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 封存原生节点注册规则

- Handler Registry 在第一次实例创建后封存注册表；同一父/子图实例族不能中途改变节点实现集合，子图只复用已注册规则。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 Pose 子图与 Root Orientation 节点组合

- 托管注册适配新增 `PoseSubgraph` 与 `RootOrientationWarp`；子图按显式请求/实例/Reset 身份创建并复用同一 Handler Registry，Root Warp 显式注入曲线、服务与 buffer。
- 两类节点均沿原生实例生命周期管理，不生成第二张图、不复制 Root Motion 或子图业务状态。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 收紧原生值节点提交生命周期

- Blend、Layered、Additive、Modify Bone、Parameter Resolve 和空间转换 Handler 的 `CommitFrame` 现在统一要求实例存活且当前帧已打开，错误阶段不会静默接纳候选状态。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 拆分惯性化注册职责

- 将 `Inertialization` 注册入口从纯值节点注册文件拆出，独立表达其历史/残差状态；纯值注册文件只负责无状态值节点。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 Workspace 字段级保留审计

- `AnimationPoseNativeWorkspaceContracts` 仍被新 Native handler、Animation Player、BlendStack 和旧 Program 直接使用；其 Native slice binding 不能随旧 Operation 表一起删除。
- `CharacterPoseOperationCompletionPage`、Stage completion、Operation index 和旧全图容量字段仍被旧 Program/Diagnostics 读取，待共享 Host/Diagnostics 切换后再做字段级删除；本次没有添加兼容镜像或第二套 Workspace。
- 本次只更新 execution 记录，没有运行 Unity、Build、Play，也没有提交 LFS。

## 2026-09-14 r3 原生边界与作者映射清理收口

- `17f57d895` 补齐根/State/Boundary 图的 GraphInput、EntryPoseInput、GraphOutput 数量校验；`6de63d214` 补齐 Boundary 动态端口的 `InterfacePortId` 校验。
- `6ab0beae4`、`2bbeb7a40`、`dd0708d46`、`b150ff216`、`a596adee9` 分别删除 `NativeRole`、`OperationFamily`、`RuntimeRequirements`、节点 `OperationCode` 和无消费者的 `AnimationChannel` authoring 映射；保留真实消费者使用的 Source Slot 和旧运行时结果类型。
- `AnimationPoseNativeWorkspaceContracts` 仍同时承载 Source/BlendStack/原生 buffer 与旧 Program 的 Operation completion page，`CharacterPoseFrameContracts` 仍被共享 Host/Diagnostics 使用；这些不是当前可独立删除的孤立文件。
- 本组提交只涉及普通 C# 文件，没有 LFS；未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 接入 Native 节点写读 binding

- `9c686a379` 在既有 `AnimationPoseNativeWorkspaceContracts` 中增加 Native 节点独占页的写 binding，以及从 `CharacterPoseNativePoseReadBinding` 建立的只读读 binding；新 handler 复用同一 Native slice 合同。
- `CharacterPoseOperationCompletionPage`、Stage/Operation 表和 Worker 编排未在本步修改，因为仍有旧 Program/Diagnostics 消费者；本步只提交普通 C#，没有 LFS，也未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 删除 authoring RuntimeRequirements 分类

- `dd0708d46` 删除 `CharacterPoseNodeRuntimeRequirement`、各节点的 `RuntimeRequirements` 覆盖和 `Requires` 判断；Projection 的 AnimationSlot/StateMachine 校验改为直接比较 `CharacterPoseNodeKind`。
- `UsesPoseSourceSlot` 保留为四类实际资源源节点的直接分类，因为 Profile Inspector 仍需要它；没有删除该真实 authoring 需求。
- 该提交只包含普通 C# 文件，没有 LFS；未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 收紧原生图边界输入校验

- `17f57d895` 让 `CharacterPoseNativeGraphValidator` 镜像正式 Projection 的边界规则：根/State 必须恰好一个 `OutputPose` 且无 GraphInput/GraphOutput；Subgraph、Linked Entry、MotionMatching Entry 必须恰好一个 `GraphInput` 或 `EntryPoseInput` 和一个 `GraphOutput`。
- 该校验位于 Prepare 前，不改变节点算法、Source、Constraint 或 Final Publication；普通 C# 文件提交，无 LFS，未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 收紧 Native 动态接口端口身份

- `6de63d214` 让 Native graph validator 镜像 authoring Projection 的接口端口规则：GraphInput、EntryPoseInput、GraphOutput、PoseSubgraph 的动态端口必须有 `InterfacePortId`，普通节点不得携带该身份。
- LinkedPoseCall 仍由其正式接口签名 owner 校验，Native validator 不重复实现 LinkedPose 的接口解析；本步只提交普通 C#，没有 LFS，也未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 删除作者侧旧节点映射

- `b150ff216` 删除 Pose authoring 节点定义、metadata 和 Projection 判断中的 `CharacterPoseOperationCode` 节点映射，ParameterResolve 改由正式 `CharacterPoseNodeKind` 判断。
- `a596adee9` 进一步删除无消费者的 `UsesAnimationChannel/AnimationChannel` authoring requirement；保留仍被 Profile Inspector 使用的 `UsesPoseSourceSlot`，并保持其它 flag 的原数值不变。
- 两步均只涉及普通 C# 文件，没有提交 LFS；旧运行时 `CharacterPoseOperationCode` 消费者仍保留到共享 Host/Projection 切换完成。未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 删除重复 NativeRole 节点映射

- `6ab0beae4` 将 Projection Validator、authoring adapter 的边界计数、EntryPoseInput、Subgraph、Output 和端口边界判断全部改为直接读取 `CharacterPoseNodeKind`。
- 删除 `CharacterPoseNativeNodeRole`、metadata 转发和各节点 override；全仓 `NativeRole/CharacterPoseNativeNodeRole` residue check 无命中，未改变 EntryPoseInput 作为 GraphInput、PoseSubgraph 作为 Subgraph 的既有语义。
- Adapter 中并行的 EventGraph descriptor 改动保留在工作区；本步只提交本次 NativeRole hunk，没有提交 LFS，也未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 删除 authoring OperationFamily 映射

- `2bbeb7a40` 将 Profile Inspector 的 StateMachine 判断、Graph Mutation 的 LinkedPose 判断改为直接比较 `CharacterPoseNodeKind`，删除 `CharacterPoseNodeDefinition` 的 `OperationFamily/ResolveFamily` 和 metadata 转发。
- 运行时旧 `CharacterPoseOperationFamily` 仍有 Program/Final/Diagnostics 消费者，因此没有删除运行时枚举或旧链路；本步只清理作者侧重复映射。
- `a596adee9` 与 `2bbeb7a40` 均只提交普通 C# 文件，没有 LFS；未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 删除无消费者的作者侧 Worker 投影

- `CharacterPoseNodeDefinition` 与 `CharacterPoseAuthoringNodeMetadata` 不再暴露 `WorkerThreadSafe/WorkerKernel`；全仓扫描确认没有调用者，这两个字段只把旧 Worker 编排泄漏到作者面板/Clipboard 元数据。
- 保留仍被旧 Projection、Diagnostics 和旧 Program 消费的 `OperationCode/OperationFamily`，不把当前消费者误判为可删除。
- 提交为 `5456c277a`，只含两个普通 C# 文件；未提交 LFS，也未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 共享 Host 接线审计

- 对核心 Host 任务做了有句柄的短等待，权威状态仍为 `active`，没有新的完成结果或接线提交；该等待不是完成证明。
- 当前精确扫描中，`CharacterPoseNativeGraphRuntime.Create/Replace` 仍只有 runtime 定义和 Subgraph 内部调用，没有角色 Host 的实际入口；`NativeRuntimeContracts`、`NativeGraphEvaluator`、`NativeGraphRuntime` 仍为并行 `MM`，本步未修改。
- 旧 `ProgramImage/ExecutionView/Worker/Operation` 仍有共享 Host、Diagnostics、Projection 和旧 Program 消费者；继续保留到消费者切换，避免错误删除。未运行 Unity、Build、Play，也没有提交 LFS。

## 2026-09-14 r3 删除已确认孤立的 Runtime Pose IR 合同

- 删除 `Contracts/Pose/CharacterPoseIrContracts.cs` 及其 `.meta`，当前 Pose/Runtime、Editor 和作者入口已没有该合同的实际消费者。
- `ProgramImage`、`ExecutionView`、全图 `Worker/Operation` 仍被旧 Host、Diagnostics、Projection 和旧 Program 文件引用，本步不扩大删除范围，不用缺失类型或兼容路径掩盖迁移未完成。
- 本步只提交 `86bd55c15` 的两个普通源码文件；未提交 LFS，也未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 删除空的 Pose Worker 目录标记

- `PoseGraph/Worker` 目录没有任何文件或代码消费者，仓库中唯一残留是 `Worker.meta` 的空目录标记。
- 删除该 `.meta`，不触碰仍有旧消费者的 `Program` 目录或 Worker 类型；提交为 `dbc9a3249`，未包含 LFS，也未运行 Unity、Build、Play 或资源刷新。

## 2026-09-14 r3 小步提交记录补齐

- `f7ee93fe5` 将作者节点定义与旧 IR 编译文件分离，删除 `Lower` 和旧闭图 IR 入口；`85c7c85f5` 收紧 Connection/NativePorts 的 typed bind。
- `345c298d2` 收口 Native Port Values；`08dc8cec2` 将原生 Source demand 准备接回正式 Source module；`05d94d70b` 接通 Final writer 的 Native 接收入口。
- 这些提交只包含源码或源码 `.meta`，均未包含 LFS；`NativeRuntimeContracts`、`NativeGraphEvaluator`、`NativeGraphRuntime` 的混合 staged/worktree 改动仍原样保留，未整文件提交。

## 2026-09-14 r3 原生节点职责提交收口

- `51dde9457` 提交 Graph/Node 的原生已提交观察桥；`aa9cae8e1` 提交纯值节点与实例 Native buffer；`edf85d96b` 提交 Clip、BlendSpace、Selected、BlendStack、Animation Slot 的 Source 节点。
- `9e0debb80` 提交 Foot/Goal/FBBIK Constraint 节点；`85f04ff7f` 提交 StateMachine、Inertialization、History、Root 控制节点；`fbbf99005` 提交 Subgraph、LinkedPose、MotionMatching、Entry 调用节点；`7268fd7b7` 提交 Final Publication 接收端。
- 上述提交只包含 C# 与 `.meta`，各路径 `filter: unspecified`，没有提交 LFS；既有索引内容和共享 Host 脏改动未被带入。
- 这些提交完成了 Pose 侧节点职责与服务适配，不等同于生产闭环：角色 Host 仍未实际调用 `CharacterPoseNativeGraphRuntime.Create/Replace`，旧 `ProgramImage/ExecutionView/Worker/Operation` 消费者仍待共享 owner 切换后清理。

## 2026-09-14 r3 接入 Inertialization 原生节点

- 新增 `CharacterPoseNativeInertializationHandler`；节点从输入 Pose 的 Discontinuity 识别切换，读取节点绑定的正式 Inertialization Policy，维护实例级历史 Pose、速度、参数、Foot 特征与残差，并在原生输出页完成惯性响应。
- 直接惯性化沿既有位置/旋转/Scale、速度、参数过滤、Foot envelope 和曲线/Profile 规则实现；Hard Cut 不创建额外历史路径，输入无 Pose 时清空该节点的活动状态。
- 节点的 committed/pending 历史随原生 graph 的 Commit/Discard 交换或撤销，输出不写 Transform，不依赖 `ProgramImage`、旧 Operation 页或全图 Worker。
- 本步只建立节点实现，尚未由共享 Host/handler factory 安装；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入原生 Constraint handler

- 新增 `CharacterPoseNativeConstraintServiceBinding` 及 Foot Placement、Pose Bone IK Goals、Full Body IK Goal Assembler、Full Body IK handler；它们只调用现有 `CharacterPoseConstraintRuntime`、`CharacterPoseWorldContextAdapter` 和既有 solver，不复制 Foot/Goal/FBBIK 状态。
- Foot Placement 沿正式 source contribution、Pose 曲线和世界帧构造 `CharacterFootPlacementFrameInput`；Goal Contribution 与 Goal Set 直接读取 Constraint pending bank 的 Native slice。
- Full Body IK 将 Component Pose 写入节点级工作页后调用现有 `ExecuteFullBodyIk`，输出保留 Component 空间和原生元数据，不直接写 Transform；外部 Goal Set 通过 Constraint 的 pending binding 接入。
- 本步未运行 Unity、Build、Play 或资源刷新；handler 尚未由共享 Host 安装，Final Publication 仍待原生输出接线；没有提交 LFS。

## 2026-09-14 r3 接入 BlendStack 原生 Job

- 新增 `CharacterPoseNativeBlendStackHandler` 与 Source module binding；Stack 只为当前选择引用的 Timeline/Provider source 建立 capture，Barrier 前调用既有 `AnimationSlotBlendJob`，Barrier 后调用既有 `AnimationBlendStackRuntime.CompleteFrame`。
- Stack 的多个 source 使用 `(ScopeInstanceId, NodeId, SourceId)` 请求身份；source capture、release、Stored/History 和 BlendStack workspace 继续由原 Stack/Source owner 管理，不建立第二份 Blend 算法或时间状态。
- 本步未运行 Unity、Build、Play 或资源刷新；State、AnimationSlot selection、Inertialization、Final Publication 和共享 Host 仍待接线；没有提交 LFS。

## 2026-09-14 r3 修正原生执行语义审查 R1-R7

- R1：`CharacterPoseNativeSubgraphHandler` 在 Prepare 只绑定参数、Action、Fact、Trajectory 和 Motion Matching binding 等控制输入；Local/Component、History、Goal 等 Pose 依赖延迟到 Barrier 后的 Evaluate 前，再以同一 completion identity 绑定 child，禁止提前读取未完成 Pose。
- R2：`CharacterPoseNativeGraphEvaluator` 在初始化时从 Output/GraphOutput 反向建立可达节点集合，Prepare 和各执行阶段只驱动实际输出依赖的 handler，未接入输出的 Player 不再推进；选择分支的动态参与仍由对应 State/Linked handler 管理。
- R3：Clip、BlendSpace、Selected、Blend、Layered、Additive、空间转换、ModifyBone 和参数解析 handler 都区分 committed page 与 working page；下一帧只写非提交页，Discard 不交换提交页，Commit 才接纳工作页。
- R4：普通 Blend 与 Layered Bone Blend 的四元数项统一相对 Base 四元数对齐，修复 `q/-q` 等价姿态在等权时相消为零的问题。
- R5：原生 Source request 增加 `ScopeInstanceId`，需求去重键改为 `(ScopeInstanceId, NodeId)`；同一子图实例仍拒绝重复请求，不同子图调用实例可保留同一作者节点身份。
- R6：`ValidatePending` 不再接收整图 output；每个 handler 校验自己的候选结果，图边界继续负责最终输出类型，避免空间转换链用根输出误校验中间节点。
- R7：Blend、Layered 和 Additive 的连续性历史分成 committed/candidate 两组，Discard 恢复最后提交历史，只有候选输出确实 Commit 后才写入下一帧比较基准。
- 本轮修正只涉及原生 runtime/handler/contracts 和本 execution 记录；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。审查正文保持不变。

## 2026-09-14 r3 接入 AnimationSlot 原生合成节点

- 新增 `CharacterPoseNativeAnimationSlotHandler`；Slot 节点只读取输入的基础 Local Pose，并通过注入的 Action source 服务取得当前 Action Pose，完成逐骨骼 Pose/速度、参数、贡献、Foot 特征和 Discontinuity 合成。
- 新增 `CharacterPoseNativeAnimationSlotSourceBinding`；它复用既有 `AnimationBlendStackRuntime`、`AnimationSlotBlendJob` 和唯一 Source module，按同一 Barrier 完成 Action source 的准备、采样、提交或撤销。
- Action 选择、采样、Source demand、Barrier 后完成和释放仍由注入服务持有；节点不复制 Action/Slot 选择状态，也不把 `ProgramImage` 作为路由或布局来源。
- Native Source request 允许 Timeline Action 使用 source-less request；资源型 Clip/BlendSpace/MotionMatching 仍必须带正式 source slot，避免把 Action 节点硬绑到资源槽。
- Slot 节点保留 `RequireSelection`/`AllowEmpty` 语义、实例级连续性和 committed/pending 生命周期，最终输出仍只写原生节点页，不直接写 Transform。
- 本步只建立原生 Slot 节点与 Action source 接口，尚未由共享 Host/handler factory 安装；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 PoseStateMachine 原生端口边界

- 新增 `CharacterPoseNativeStateMachineHandler`；StateMachine 节点将状态 owner 产生的当前 Local Pose 复制到自己的原生节点页，并沿同一 Prepare/Evaluate/Validate/Commit/Discard 生命周期发布。
- 状态选择、Transition rule、时间、状态源和子图调用继续由注入的 `ICharacterPoseNativeStateMachineSource` 持有；节点不创建第二个 FSM、不使用 `ProgramImage` 索引，也不直接写骨骼。
- StateMachine handler 对原生 Pose 的空间、布局、完成身份和 Pending 输出进行严格绑定，保留状态 owner 的故障结果，不把 Unavailable 转成默认 Pose。
- 本步只完成 StateMachine 与原生 evaluator 的领域适配边界，具体 source owner/共享 Host 安装仍待接线；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 更正 Source demand 身份记录

- 原生 Source demand 的实际去重键是 `(ScopeInstanceId, NodeId, SourceId)`；同一作者节点在同一调用实例中请求两个不同 Source 时不会被错误合并，不同子图实例也不会互相覆盖。
- 资源型 Source request 继续要求正式 Source Slot，Timeline Action 可使用 source-less request；该规则与 `CharacterPoseNativeSourceRequest` 当前校验保持一致。

## 2026-09-14 r3 删除无消费者 Pose IR

- 删除 `Contracts/Pose/CharacterPoseIrContracts.cs` 及其 `.meta`；当时的 Runtime/原生 Pose 入口没有这些类型的消费者，旧 Editor IR 编译链的引用随后作为待清理残留处理。
- 该删除只移除孤立的 Pose IR 数据模型，不触碰仍由旧 Host 引用的 `ProgramImage`、ExecutionView 或共享 Projection；没有新增兼容 reader、反向导出或 fallback。
- 未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 修正 IR 删除扫描范围

- 后续全仓扫描发现 `CharacterPoseIrContracts` 仍被旧 Editor `CharacterPoseIrCompilation`/`CharacterPoseGraphProjectionValidator` 引用；这些是待删除的旧 Pose 编译链消费者，不是运行时消费者。
- 因此上一条“只有文件自身声明”的表述仅对当时限定的 Runtime/当前 Pose 运行入口成立，不能作为全仓无引用结论；旧 Editor 编译链仍需垂直清理后，才可把 IR 残留扫描记为通过。

## 2026-09-14 r3 分离作者节点定义与旧 IR 文件名

- 保留 `CharacterPoseNodeDefinition` 及各节点的作者字段、Capability、Clipboard、Mutation 和资源依赖读取能力，移除其旧 `Lower`/IR 拓扑编译职责。
- 将实际只剩作者节点定义的源码改名为 `CharacterPoseNodeDefinitions.cs`，同步保留原 `.meta` GUID；旧 `CharacterPoseIrCompilation.cs` 文件名和旧 `CharacterPoseIrGraphRole`/拓扑编译入口不再存在。
- 删除 `ValidateClosedGraph` 对旧 IR role 的依赖；运行时图校验和 FlowCanvas NativePorts 继续使用正式 Pose Graph/Node/Connection 定义，不生成 IR。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS；Projection Compiler 的剩余旧入口和共享 Host 仍由后续 owner 收口。

## 2026-09-14 r3 收紧原生 Parameter Get 合同

- `CharacterPoseNativeGraphEvaluator` 的内置 Parameter Get 在初始化时核对节点 ID、输入合同声明、Control 用途和只读 EventGraph 分类；Action Weight、Foot 曲线等非 Blackboard 输入不能伪装成 Get。
- 求值时继续使用同次发布的 `CharacterAnimationVariableFrame`，并按声明精确拒绝 Float32/Int32/Bool 类型不匹配；没有新增变量副本或 Pose Set。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 RootOrientationWarp 原生节点

- 新增 `CharacterPoseNativeRootOrientationWarpHandler`；节点读取同次 EventGraph 发布的 `animation.facing-error`，通过注入的当前源采样进度和正式 RootMotion 曲线计算 RootYawOffset，并只修改原生 Pose 页的 Root 骨骼旋转。
- capture target angle、源身份、Body discontinuity generation 和当前偏差都按节点实例保存，Commit/Discard 分离；RootOrientationWarp 不再依赖旧 `CharacterRootOrientationWarpNativeControl` 或 Operation 页。
- 本步只建立 Root Orientation Warp 节点算法和 source owner 边界，尚未由共享 Host/handler factory 安装；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 PoseHistoryCollector 原生节点

- 新增 `CharacterPoseNativeHistoryCollectorHandler`；历史 owner 在实例帧开始时提供上一提交的只读 `History` view，当前 Local Pose 复制到节点独占页，禁止在 Evaluate 阶段提前写入历史。
- Commit 才把当前原生 Pose 页交回 `ICharacterPoseNativeHistoryCollectorSource`，Discard/Stop 则撤销 owner 的 pending frame；历史数据、时间、源 lineage 和 Foot placement 仍由既有 History/MotionMatching owner 持有。
- 本步只完成原生端口与历史生命周期适配，具体 History source owner/共享 Host 安装仍待接线；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 收紧 Graph Output completion 合同

- `CharacterPoseNativeGraphRuntime.Evaluate` 现在要求 evaluator 返回的 Graph Output 具备有效 typed identity，并严格匹配当前 frame completion identity；缺失、过期或默认值不能进入 Validate/Commit。
- 该校验位于图边界，不复制节点算法或 Source/Constraint 状态；handler 仍只负责自己的候选页和观察。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 收口 Connection typed bind

- `CharacterPoseCanvasConnection.Bind` 现在在调用 FlowCanvas 原生 `ValueInput.BindTo` 前，统一通过 `CharacterPoseCanvasNativePorts` 核对 source/target 领域端口、方向和 Native value 类型。
- 连接加载、编辑重连和原生图初始化都不能绕过该 typed 边界；失败直接报告 Edge 身份，不生成隐式转换或占位值。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 MotionMatchingPose 原生节点边界

- 新增 `CharacterPoseNativeMotionMatchingHandler`；它把 Motion Matching owner 产生的当前 Local Pose 复制到节点独占原生页，并通过原生端口承接其同帧完成身份。
- History、Trajectory、Facts、Binding、选择/数据库查询、Source readiness 和提交撤销仍由注入的 MotionMatching owner 持有；handler 不复制查询算法或 selection state。
- 本步只完成 MotionMatching Pose 的原生端口与生命周期适配，具体 owner/共享 Host 安装仍待接线；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 LinkedPoseCall 原生调用边界

- 新增 `CharacterPoseNativeLinkedPoseHandler`；Linked Pose 的 Group/Interface/Entry 结果被复制到当前调用实例的独占 Local Pose 页，动态输出端口保持作者声明。
- Linked Pose owner 继续持有组选择、调用共享、子图资源和历史状态；handler 不把 Linked Pose 展平为父图节点，不复制其运行状态，只沿原生 Prepare/Evaluate/Commit/Discard/Stop/Dispose 交接。
- 本步只完成 Linked Pose 的原生端口与调用实例适配，具体 owner/共享 Host 安装仍待接线；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 EntryPoseInput 原生入口

- 新增 `CharacterPoseNativeEntryPoseHandler`；Entry Pose owner 产生的 Local Pose 经过当前 completion 校验后复制到入口节点的独占页，供 MotionMatching entry graph 使用。
- Entry Pose 的来源、准备、资源状态和提交撤销由注入 owner 持有；入口节点不读旧 Program 缓存、不生成默认 Pose，也不把入口图展平到父图。
- 本步完成 Entry Pose 的原生 typed 端口和生命周期适配，未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 旧 Program 残留审计

- 当前 `CharacterPoseProgramImage`/`ExecutionView`/`Worker`/`Operation` 命中集中在旧 Presentation Host、Action/Slot/State/Diagnostics/Projection 消费者和 `PoseGraph/Program` 目录；这些文件存在并行脏改动，且仍承担共享壳或待迁移消费者，暂不强删。
- `PoseInertializationNativeProgram` 仍被旧 Program/Diagnostics/State source 引用；新 Inertialization handler 已接管 direct policy 算法，但在消费者切换前不能删除旧类型而制造跨窗口覆盖。
- `CharacterPoseTransitionRuleCompiler` 当前只有自身定义命中，但文件有并行 EventGraph typed 规则改动；保留该文件，待旧 Projection/State 消费者切换后再判断是否删除或迁移其纯规则编译职责。
- 本次审计未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 第3组当前完成边界审计

- 3.1—3.6、3.13、3.15 的 Pose 原生合同、FlowCanvas Manual 生命周期、typed port/connection、实例 buffer、Reset/Replace 原语已具备源码实现；3.8、3.12、3.14 已有 Source/Constraint/Final 的原生 handler 或接收端。
- 3.9、3.10、3.11、3.16、3.17 的节点适配边界已补齐到 StateMachine、Slot、BlendStack、Inertialization、MotionMatching、LinkedPose、History、Entry、Root 等类型，但具体 source owner 与共享 Host 尚未安装，不能把类存在当成生产闭环。
- 3.18 的 IR 合同、旧拓扑编译入口和 `Lower` 已删除；仍命中的 `ProgramImage`/ExecutionView/Worker/Operation 是旧 Host、Projection、Diagnostics 和旧 Program 消费者，因并行脏改动与共享 owner 暂不强删。
- 当前精确 `rg` 未发现角色 Host 对 `CharacterPoseNativeGraphRuntime.Create/Replace` 的实际调用点；这是完成运行闭环和继续删除旧消费者所需的下一处外部接线事实，不用 fallback 或伪造调用点代替。
- 本次审计未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。

## 2026-09-14 r3 接入 LayeredBoneBlend 原生值节点

- 新增 `CharacterPoseNativeLayeredBoneBlendHandler`；它消费正式的完整 Rig Bone Mask，按每根骨骼混合 Local Pose、速度、参数、贡献和 Foot 特征，不把 mask 简化成一个全局权重。
- Bone Mask 由外部资源绑定提供只读 dense 数组，节点不复制资源所有权；错误 Rig 长度、权重或缺失资源直接失败。
- 本步未运行 Unity、Build、Play 或资源刷新；State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-14 r3 暴露原生节点已提交观察

- `CharacterPoseCanvasGraph` 增加 `TryObserveNativeNode`，`CharacterPoseCanvasNode` 增加 `TryObserveNativeOutput`；两者只转发 Native Runtime 已提交的节点/端口观察。
- 观察仍由实例级 `CharacterPoseNativeGraphRuntime.TryObserve` 按 Node、Port、Instance 和最近完成身份读取；未挂载运行时或没有已提交结果时返回 false，不读取旧 `ProgramImage/ExecutionView`，不重新求值，也不引入订阅状态。
- 本步未运行 Unity、Build、Play 或资源刷新，也没有提交或暂存任何 LFS 文件。

## 2026-09-14 r3 建立原生 Final Publication 接收端

- 新增 `CharacterFinalPoseNativePublication`，直接接收原生 Local Pose 页，按原生图/资源/ Rig/输入合同建立最终候选帧；不再要求 `ProgramImage` 作为最终写入布局来源。
- 原有 `CharacterFinalPosePhysicalWriter` 增加原生 Pose 写入入口，继续执行整 Rig 预检查、Committed 姿态保留、骨骼物理写入和 Foot 物理事实采集；`CharacterFinalPosePropertyWriter` 增加基于 `CharacterAnimationInputContract` 的属性绑定校验，避免属性写入依赖旧 Program 参数表。
- 原生最终发布复用同一双页 `ComposedAnimationPoseFrame`/lease 和 Source contribution 解析；Live/Stored contribution 的稳定玩家身份与 Source 身份集中由 `CharacterFinalPoseContributionResolver` 解析，不复制 Source 或 IK 状态。
- 本步只建立真实 Final 服务入口，尚未由共享 Host 调用，也未把旧 `CharacterFinalPosePublication`/`ProgramImage` 消费者宣称为迁移完成；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。
- `CharacterPoseNativeGraphRuntime` 增加带 Final Publication 的 Commit 入口：先准备原生输出并完成唯一物理写入，再提交节点的 Pending 历史，最后采用最终帧；任一前置阶段失败都会丢弃原生节点和 Final pending 页，不新增第二个图执行链。

## 2026-09-14 r3 修正实现审查 R1-R7

- R1：子图 Prepare 只绑定控制型入口；Local/Component、History、Goal 等 Pose 输入延迟到父图进入 Evaluate、child 仍在 Prepare 时绑定，随后沿同一 completion identity 求值。
- R2：evaluator 初始化时从 Output/GraphOutput 反向计算可达节点，只驱动输出依赖的 handler，孤立 Player 不再推进；动态状态选择仍由 State/Linked handler 决定。
- R3：所有现有双页原生 handler 由 committed page 和 working page 管理，工作页只在 Commit 后接纳，Discard 保留已提交页，避免 Native slice 观察引用被候选帧覆盖。
- R4：Blend 与 Layered 的旋转混合统一以 Base 四元数为参考对齐，消除 `q/-q` 等价姿态等权相消。
- R5：Source request 增加 `ScopeInstanceId`，需求去重键使用 `(ScopeInstanceId, NodeId)`，同一作者子图节点在不同调用实例中不会互相冲突。
- R6：handler `ValidatePending` 不再接收整图 output，改由节点检查自己的候选结果；整图边界仍负责最终端口类型与最终输出检查。
- R7：Blend、Layered、Additive 的连续性比较状态拆分为 committed/candidate，Discard 恢复最近提交历史，候选状态只在 Commit 接纳。
- 本步只修改原生 runtime、handler、contracts 和本 execution 记录；未运行 Unity、Build、Play 或资源刷新，也没有提交 LFS。审查正文未修改。

## 2026-09-14 r3 接入 PoseParameterResolve 原生值节点

- 新增 `CharacterPoseNativeParameterResolveHandler`；姿态骨骼与来源贡献沿 Base 原样传递，只按节点声明的参数策略更新参数页，不重新求值 Source 或复制 EventGraph 帧。
- Base、Overlay、Weighted、Max、Min 都读取同一 `CharacterAnimationInputContract` 的参数顺序和 typed 可用性；未知参数或非法策略直接失败。
- 本步未运行 Unity、Build、Play 或资源刷新；State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-14 r3 接入 ModifyBone 原生值节点

- 新增 `CharacterPoseNativeModifyBoneHandler`；它消费 Component Pose，按共享 Rig 定位目标骨骼，并分别处理 Local/Mesh 参考空间下的位置、旋转和 Scale 修改。
- 修改后的 Component Pose 继续沿原生 buffer 和空间标记输出，参数、贡献、Foot 特征和连续性元数据保持来自输入；没有直接写 Transform。
- 本步未运行 Unity、Build、Play 或资源刷新；State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-14 r3 接入 AdditivePose 原生值节点

- 新增 `CharacterPoseNativeAdditivePoseHandler`；它按正式 RigReference/Local 参考姿态计算位置、旋转、Scale policy 和速度增量，输入权重仍只接受节点字段或 EventGraph Float32 Get。
- Additive 不把增量分支伪装成新的 Live source contribution，保留 Base 的来源、Foot 特征和参数元数据，避免 Foot/Final 把增量误判为独立源。
- 本步未运行 Unity、Build、Play 或资源刷新；Layered、State、Slot、BlendStack、Inertialization、Constraint、Final 和共享 Host 仍待接线。

## 2026-09-15 文档闭环收口

- 本窗口完成本 change 的文档收口：`proposal.md` 说明范围与破坏性变化，`design.md` 说明所有权、输入输出、生命周期、删除顺序和业务取舍，`tasks.md` 保留唯一实施清单，`specs/character-presentation-pose-graph/spec.md` 保留领域增量，`implementation-review-2026-09-14.md` 保留审查与修正边界，本文件登记按职责发生的实际改动。
- 生成 Pose 图和验证不属于本窗口本次文档交付范围，因此没有运行 Unity、Build、Play、资源刷新、图生成或验证命令；这些未执行项不是实现阻塞，也不构成“已验证”证据。
- 文档统一使用以下完成口径：第1、2组继续按既有记录视为已完成；第3组每项只有在对应代码接线和实际消费者收口后才能把 `tasks.md` 的复选框改为完成。已有 handler、接口、Final 接收端和删除步骤不能单独替代角色 Host 接入或旧消费者迁移。
- 当前可执行的剩余工作已经从“阻塞”改记为“实现收口”：由实现窗口继续把原生 Create/Replace、唯一 Barrier、Source/Constraint/Final 消费者接到角色表现链，并在消费者迁移后删除仍有引用的旧 Program/Image/ExecutionView/Worker/Operation；不恢复 fallback、兼容 reader、第二运行链或伪造完成状态。
- 当前静态事实仍保持如实记录：原生 Pose runtime 内已有创建、替换和阶段 API，旧 Presentation/Preview/Diagnostics/Projection 消费者仍存在旧 Program 依赖，角色 Host 尚未形成原生 Create/Replace 调用点。该事实用于指导下一步代码修改，不再作为停止推进的理由。
- 本次只更新执行记录，不改用户或其它窗口的未提交代码、资产和既有审查结论；没有暂存或提交任何 LFS 文件。

## 2026-09-15 r3 建立正式角色入口

- 新增 `CharacterPoseNativeRoleRuntime`，由角色入口统一持有已采用的原生 Pose 图实例和 `CharacterFinalPoseNativePublication`；角色侧不再直接管理 GraphRuntime 的内部创建、替换和最终发布细节。
- 入口把 `Prepare`、`Create/Replace`、`BeginFrame`、Source demand、Barrier 后 `Evaluate`、`ValidatePending`、Final `Commit`、`Discard`、`Reset`、`Stop`、`Observe` 与 `Dispose` 收成一条明确调用面；创建失败或替换失败会释放未采用的图与 Final 服务，不发布假采用结果。
- 本步提交为 `a1a2d8587`、`533896f40`，只包含普通 C# 与 `.meta`，没有提交 LFS；未运行 Unity、Build、Play、图生成或验证。
- 该步解决了“没有正式角色入口”的类型与生命周期缺口；角色表现 Host 尚需把自身的 Profile、Rig、EventGraph 输入合同、真实 handler factory 和 Final 服务装配到该入口，下一步不能停留在只调用接口声明。
- 入口补充暴露当前 Frame Lineage、Frame Input、OpenFrame 和最近已提交 Output，保证上层观察、Barrier 与采用结果继续使用同一实例/完成身份；提交为 `a11d4332d`。
- 原生角色入口新增 `CharacterPoseNativeRoleDependencies`，以 typed 依赖一次接收 `CharacterPoseSourceModule`、`CharacterPoseConstraintRuntime`、handler factory 和 `CharacterFinalPoseNativePublication`，并在入口停止/失败时按图、Final、Constraint、Source 顺序释放；提交为 `14ae1c1c1`。这只收口所有权，不把尚未装配的服务伪装成已接线。
- 新增 `CharacterPoseNativeFrameCoordinator`，将单个角色帧的 `BeginFrame → PrepareFrame → PrepareEvaluation → Evaluate → ValidatePending → Commit/Discard` 收成唯一阶段驱动，持有单一帧租约并在准备失败、停止和释放时清理；提交为 `4fa391ee3`。它不创建第二时钟、不复制 Source/Constraint 状态，尚未替共享 Host 调用。

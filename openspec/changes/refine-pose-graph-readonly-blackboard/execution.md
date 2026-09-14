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

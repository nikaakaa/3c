# Timeline 实施基线登记

## 版本与范围

- worktree：`D:/Unity_Project_1/3C-worktrees/timeline-runtime`
- branch：`codex/decouple-timeline-from-skill`
- 代码基线：`ad6b50c2f`（封装 Timeline 领域目标，时间核心不再持有业务 target）
- Unity 项目：`3cDemo/Client/3C_Client`
- 角色基线：`Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset`，GUID `c7a7c1e3f7e64d81b5a04a90cbeb8d4e`
- 现有共享 Timeline：`Assets/Configs/Character/Corin/Pipeline/Graphs/SharedTimelines/CorinAttack1Timeline.asset`，GUID `be588770448d17444828b566954e2709`
- 固定输入基线：`Diagnostics/CharacterInputTraces/20260827-183705-081-43357ff3cd384e5cba75d2c31175b116.json`，`trace_id=43357ff3cd384e5cba75d2c31175b116`，`tick_rate=60`，`frame_count=1044`，`content_hash=ebca979bc81ea309495ebfabff6a7ab5d3bdf368d86c42092aebb2add4286647`
- 当前实现提交：`77e006586`，在 `c1e7030fb` 的共用 Timeline 内容发现、执行运行时和角色接入基础上，接入独立 Semantic IR 根、统一 artifact/store/codec/原子发布、Float32/Fixed Target lowering、正式 Build 入口和非 Skill 生命周期；Timeline 窗口新增领域目录与 typed 外部目标/参数编辑面板，字段先留在草稿、Apply 才经 Undo 写回。Timeline 的 catalog、producer、reference、绑定签名和 MotionWarp owner constant 仍按调用身份生成；根引用由根装配方唯一声明。Character 与独立播放现在共用 `TimelineControlRuntime`，旧私有 codec、第二套 Program 拓扑、作者对象解释器和兼容路径没有恢复。样例还包含 ScenePresentation 的标量/布尔参数、Decision TreeClip、真实 Unity 双面板目标和业务主机。
- 合并前正式 Center compile：`7b64617bc96e46c680d000aa29006f1e`，状态 `Completed`；`editor-result.succeeded=true` 且 `source_changed=false`；对应源码 identity 为 `121f389a423ffabb75de4bc402c0f3f19ee31a781d31ee1bf5b40340e8f67fea`。合并后本地 Core、Fixed、Float32、Timeline、Timeline.Tree、Timeline.Runtime 项目均以规定参数构建通过；Center run `facb28631fba40048c12d0aa99302492` 返回 `Faulted` 且没有输出编译诊断，因此不把它登记为合并后全量 Unity 成功证据。
- 正式 Unity batchmode 已用同一 Build bridge 发布真实独立样例：Float32 wrapper `Assets/Configs/Timeline/ScenePresentation/Generated/CorinPanelExpandTimeline.TimelineProgram.asset`，`programHash=2a2ac173693dd69b0eef294cdf8c2b168abd99969dc16bd647f4c0b0ea62affb`，`canonicalBytesHash=046b864cd28c035921c95e08534997b0ed1a04a8829ef09886531f8102f4f42b`；Fixed wrapper `Assets/Configs/Timeline/ScenePresentation/Generated/CorinPanelExpandTimeline.FixedTimelineProgram.asset`，`programHash=6eafd3d3ddef81b50dcd5ba7187c104878f82463ed04c9f61bbb114c02513584`，`canonicalBytesHash=6019558e266f14069dca604b1e9103add4b2a8bdb0d836ea9d6f4f65f2ae6a1b`。两者的 `rootKind=Timeline`、`rootIdentity=b634b12aecb74f24b5a625f9aec73259`、`entryIdentity=timeline:f7cb540c-51dc-4348-a226-b71a55ecba38`、`contentIdentity=30d726a423cb8e02` 一致；Program 内含四个场景参数片段和一个 Decision TreeClip。正式场景运行日志返回 `status=Completed`、`targets=2`、`left=1`、`right=1`、`valid=True`。
最新正式场景运行日志 `Logs/timeline-panel-run-lifecycle.log` 返回 `status=Completed`、`targets=2`、`left=1`、`right=1`、`lifecycle=True`、`valid=True`；同一入口额外记录了两个不同 owner/call/instance 的并发播放（`scene.presentation.concurrent.a/.../11`、`scene.presentation.concurrent.b/.../12`）、第二实例 force stop、第一实例自然完成，以及错误目标 identity 被拒绝。

该登记只依据已提交基线和可读的 Unity YAML。主目录未提交的角色重构、AI、相机和预览代码不属于本 worktree 的输入。

## COMM-20260906-01 执行记录

- 本 worktree 只维护本任务的实现、提交和验证证据，没有改动其它任务的正确成果。
- 普通提交、编译、独立 Build 和产物重读均写入本登记及 Center 记录；发射、发布、加载和运行状态仍按不同 owner 分开。
- 独立入口只接入共享 IR、Target lowering、artifact/store 和正式运行合同；没有恢复私有 codec、第二套 root/Builder、临时播放器或兼容 reader。
- Timeline Editor 的 `Bindings` 面板只展示正式 Track/Clip contract 和 Timeline 自己声明的 typed target/parameter；Apply、Add、Remove 都走 `TimelineEditorSessionContext.Apply` 的 Undo owner，仍被片段引用的 binding 会被拒绝删除，Live Debug 只读。
- 无来源 Graph 的 shared Timeline 通过 `TimelineEditorWindow.OpenStandalone` 的 `TimelineAsset` 选择进入同一窗口；TreeClip 下钻在没有 source Graph 时调用现有 `TreeWindowUtility` 的 Graph Shell，并继续传入 inline/shared tree、serialized owner/path 和稳定 TreeClip identity，没有创建假父 Graph。

## 当前代码链

| 当前入口 | 当前实际职责 | 本变更的目标 owner |
| --- | --- | --- |
| `Runtime/BTSMTL/Timeline/Scripts/TimelineData.cs`、`TimelineData.Runtime.cs` | 保存 Timeline、Track、Clip、区段、作者 identity；同时混入运行时间、Unity serialized owner 和编辑操作 | 作者数据只保存内容与声明；播放时间和播放状态移到调用方实例 |
| `Runtime/BTSMTL/Timeline/Scripts/TimelineAsset.cs` | 以 shared ScriptableObject 持有 `TimelineData`，绑定 serialized owner 并调用 `Init` | 继续作为 shared 内容根，不保存运行目标、句柄或进度 |
| `Runtime/BTSMTL/Timeline/Scripts/Tree/TimelineNode.cs` | Graph 节点持有 inline/shared owner、ActionContext、播放服务、句柄和完成状态 | 只做 Graph/C# 调用点适配；内容根不再要求节点 |
| `Editor/CharacterSimulation/Compilation/Semantic/CharacterAuthoringSourceCompilationModel.cs` | `CharacterAuthoringTimelineRecord` 必须持有 `TimelineNode`、GraphRoute 和 Graph occurrence | 内容闭包与 Graph/C# 调用来源分开 |
| `Editor/CharacterSimulation/Compilation/Semantic/CharacterSemanticEmitter.cs` | `CompileTimeline` 从 TimelineNode/Graph route 建 Timeline catalog，再调用 Character 专属 emitter | 共用内容发现/发射由 Timeline 领域拥有；Character 只负责组合绑定 |
| `Editor/CharacterSimulation/Compilation/Semantic/CharacterSimulationTimelineEmitterRegistry.cs` | 唯一 Track/Clip emitter registry，接收调用身份、Timeline operation 和内容 hash | 同一 registry 同时服务 Character Graph 与 Independent Root；调用来源不再成为内容发现前提 |
| `Runtime/Simulation/Core/Execution/TimelineControlRuntime.cs` | 时间分段、循环和 TreeClip 生命周期由共用控制器推进；领域片段通过 typed target leaf 进入 | 时间/生命周期与领域输出分离；领域片段负责自己的端口 |
| `Runtime/Simulation/Core/Float32/Execution/Float32TimelineTarget.cs`、Fixed 对应文件 | 读取 Program catalog 和角色状态，捕获/验证 ActionInstance，写 Timeline 状态，提交 Motion/Presentation/Facts/Trace | Numeric Target 仍负责具体数值和角色领域适配；共用控制不再反向要求 Action |
| `Runtime/BTSMTL/Timeline/Runtime/Float32TimelinePlayback.cs`、`FixedTimelinePlayback.cs` | 非 Skill 调用方拥有 Prepare/Start/Advance/Status/Stop/ForceStop/Dispose；状态在调用实例内 | 复用 `TimelineControlRuntime`、`OperationControlRuntime`、TreeClip 生命周期和 typed target 绑定 |
| `Runtime/BTSMTL/Timeline/Unity/ScenePresentationTimelineHost.cs` | 真实 Unity 业务组件加载已发布 Timeline wrapper，显式绑定面板目标并以业务 delta 推进 | Scene target 只消费公开标量/布尔参数，不获得 Character、World 或反射写入权限 |

当前分支已把独立 Timeline 接到正式 IR、Target、artifact/store 和同一个 `TimelineControlRuntime`；正式 Character 仍通过唯一 evaluator 推进，Skill 与非 Skill 只共享内容发射、时间/树控制和生命周期合同，不共享业务状态容器。

## 已确认的共享运行交接

主重构已提供并由本 worktree 消费以下正式提交：

- `a47532948539cff390817dbcc1b0ce40037cd3d2`（本分支 `4be2ae474`）：ActionInstance 的 `SkillId`、`SkillEntryOperation`、`SkillExecutionGeneration`、状态编码/hash，以及 `FixedActionStateStore`/`Float32ActionStateStore` 的 `TryGetCurrentSkillExecution`、`FindActive`、`RequireActive`、`BindSkillExecution`、`PushSkillExecution`。
- `ceb50eb9ef9926898b0f8b273de25f8b6b786fa3`（本分支 `df870f1b2`）：Skill/Character 正式控制编译、SemanticEmitter、Corin Program 生成和复读闭环；Timeline 不复制这条角色组合根。
- `d703af97f6bb0fbd720b283e9676ddbfe59d899b` 已是代码基线 `229d2a9d0` 的祖先，`SimulationExecutionSource` 及其 codec 属于继承内容，不再重复包装或重新 cherry-pick。

本次 Timeline 接管的共享运行文件与方法范围已由规划窗口确认：`Core/Execution/TimelineControlRuntime.cs` 负责 PrepareDecisionTimelines、Tick、完成/停止、TreeClip 生命周期和 segment/cycle；`TimelineControlContracts.cs` 承载共用时间、片段、状态、生命周期和领域输出合同；Float32/Fixed 独立播放通过 `ITimelineControlStatePort` 与 `ITimelineTargetLeaf<TTime>` 接入同一控制器，Scene Presentation 片段由 target leaf 的 typed hook 处理。Fixed/Float32 `TimelineTarget` 由 evaluator 装配给各自领域执行器，负责 Numeric Target、角色 Action 捕获、Blackboard、Motion、Camera、Cue 与 Presentation 端口。OperationControlRuntime、ActionStore、ActionInstance/SkillExecutionState、状态地址、generation、角色 codec 仍归主重构 owner。

## Skill Timeline 状态链核对

本次对 Float32/Fixed 共用状态链做了静态核对，输入、处理和输出如下：

- 输入：Character Program 的 SkillProgram binding、Timeline/TreeClip operation state slots，以及当前 ActionInstance 的 SkillId、EntryOperation、PredictionKey、SkillExecutionGeneration。
- 处理：`CharacterSimulationProgramBuilder.DeclareOperationStateSlots` 为 Timeline/TreeClip 声明 playback、loop、cycle、retention identity 和 logic time；`ProgramExecutionLayout.BuildSkillExecutionStateSlots` 从 SkillProgram 可达操作收集 owner 为 Runnable、Timeline、MotionModifier 或 Blackboard 的 slot。Float32/Fixed `EvaluationFrame` 绑定各自 `ActionStateStore`，`EnterSkillExecution` 建立或读取按 ActionInstanceId 保存的 SkillExecutionStateFrame；活动技能的局部槽路由到该 frame，其余槽维持原事务归属。
- 输出：TimelineControlRuntime 的 PrepareDecision 与 Skill EntryOperation Tick 使用同一 ActionInstance 的 frame；ActionStateStore 以 SkillId、EntryOperation、PredictionKey 和 generation 对齐 frame，`SimulationStateCodec` 将 ActionInstanceReference、SkillExecutionState frame 的 slot/value、Program/Layout hash 写入并在 Read 时校验后恢复。Float32 codec identity 为 `character-state/float32/v13`，Fixed codec identity 为 `character-state/fixed-q32.32/v12`。

Evaluate 的现行顺序是 `Setup → Ingress → GameplayEffects → Input → PrepareTimelineDecision → CharacterControl → TickSkillPrograms → ResolveMotion → FinalizeBlackboard`。Skill Tick 内部在 EntryOperation Tick 前后分别建立 Enter/Push scope，并在结果仍有效时以 EntryOperation generation 更新 ActionInstance。该顺序与当前 Character 唯一 Evaluate/Resolve/Finalize 链一致，没有为 Timeline 新增状态镜像或旁路执行器。

已有正式 1044 帧 Character replay `f7d63d8ab2454e3e898e434609a5777d` 使用 trace `43357ff3cd384e5cba75d2c31175b116` 成功，继续作为角色链基线；本变更没有把它冒充为独立 Timeline 证据。独立样例 `RunPanelExampleScene` 通过真实场景加载、`PrepareAndStart` 和显式 `1/60` 增量推进 90 帧，日志返回 `status=Completed`、`targets=2`、`left=1`、`right=1`、`valid=True`；未创建临时编译工程或作者对象运行解释器。

- 本分支已将共用时间/生命周期收敛到 `TimelineControlRuntime<TOperationTarget, TTime>`、`ITimelineControlStatePort`、`ITimelineTargetLeaf<TTime>` 和 `TimelineSegment<TTime>`；Character Target 在边界处提供完整 `TimelineActionContextIdentity`，独立调用方将自身 owner/call/instance 映射为不带 Skill 字段的同一上下文身份。共用控制器只使用 typed state/target 合同；角色侧继续负责七字段 Action 身份校验与 MotionWarp 生命周期，独立 Scene Presentation 只负责自己的 typed sink。旧 `TimelineExecutionRuntime`、`ITimelineExecution*`、`TimelineDomainExecution*` 和 `TimelineMotionWarpExecutionContracts` 已删除，没有第二套时间调度器。

`TimelineData.Time` 是未序列化字段，当前代码没有发现正式 Gameplay 读写者；编辑预览实际由 `TimelinePreviewSession.Time` 持有游标，Gameplay 使用 Program state 的 `TimelineLogicTime`。因此拆分时删除的是作者数据上的遗留运行字段，不是迁移一个正在被 Gameplay 使用的状态槽。

## 共同内容发射接线

共同发射输入现在由 `TimelineSemanticEmissionRequest` 统一承载：已验证的 `TimelineSemanticContentRecord` 与本次调用的 `TimelineSemanticInvocation`、已声明 Timeline operation、Tree state owner、Action Context 和 Character Tree 编译回调分开传入。`TimelineSemanticInvocation` 只允许两种来源：Character Graph 必须同时有真实 GraphId/NodeId，Independent Root 必须保持两者为空，并携带真实 root、entry、content hash 和调用 route；content hash 必须等于发现出的 `TimelineContentUnit.ContentHash`，两种来源不能通过伪造 Graph 节点合并。发射结果返回 Timeline operation、本次 Clip operations 和同一份 `CharacterSimulationCompileReport`，结果有效性不再绕过报告中的错误。

唯一 `TimelineSemanticEmitter` 负责 Timeline catalog、Track/Clip catalog、曲线、producer/reference、segment control flow、MotionWarp source 修补和 TreeClip 生命周期。它只遍历已发现的 `Content.Tracks` 与每个 `TrackRecord.Clips` 一次；本次调用的 Clip catalog、producer 和普通 reference identity 均带有 `Invocation.Identity`，MotionWarp 的 `TimelineOwnerOperation` constant 也使用调用专属 source，因此重复调用不会复用另一调用的绑定，而 Builder 的通用去重规则保持不变。Program 级 `program:root-operation` 保留为每个 Program 唯一引用，由根装配方声明一次，Root adapter 不重复声明。`TimelineSemanticRootEmitter` 不再拥有 Track/Clip 遍历，只把预先声明的根 operation 接到同一 Timeline operation 和共同发射结果。`CharacterSemanticEmitter.CompileTimeline` 只校验并装配真实 Graph/Node、播放模式、Action Context、状态 owner 和 `CompileTimelineTree` 回调后提交请求。

Independent Root 现在由 `SimulationProgramRootDescriptor` 明确标识，`TimelineSemanticFrontendCompiler` 以 Timeline 资产 GUID、Timeline authoring identity 和内容 hash 建立 root/entry/content 三元身份；`CharacterSimulationBuildOrchestrator.Build(TimelineSimulationBuildRequest)` 通过同一 Semantic IR artifact/store 和 Target adapter 发布，不再要求 Character Definition、TimelineNode 或 Skill Root。

本轮用正式 `TimelineSimulationBuildBatchEntryPoint` 调用现有 `CharacterSimulationBuildMcpBridge`，对精确资产 `Assets/Configs/Timeline/ScenePresentation/CorinPanelExpandTimeline.asset` 分别发布 Float32 与 Fixed wrapper；Build 响应均返回 `success=true`、`rootKind=Timeline`、精确 root/entry/content identity 和 canonical bytes hash。`Assets/Scenes/Timeline/CorinPanelExpandTimelineScene.unity` 由正式 Editor builder 生成，绑定 `scene.panel.left` 与 `scene.panel.right` 两个实际 `Graphic` target；业务主机通过 `Float32TimelinePlayback.Advance(Float32Scalar delta)` 推进，不读取 Unity Time 的核心执行代码。
`TimelinePlaybackObservation` 位于共用 Timeline 执行合同，Float32/Fixed 独立播放和 `ScenePresentationTimelineHost` 都暴露同一只读根、产物、播放状态、执行 identity 与声明 binding 视图；它不拥有时间推进、会话或 Gameplay seek。

## Track、Clip 与现行行为

| 领域 kind | 作者类型 | 当前执行登记 | 现行行为与保护范围 |
| --- | --- | --- | --- |
| Animation | `AnimationTrack`、`AnimationClip` | `TimelineAnimation`、Presentation producer | 按片段时间和 Weight/Ease 曲线采样动画 producer；输出带 source/generation/cycle。动画数学与现有 Projection 不在本变更中重写 |
| MotionCurve | `MotionCurveTrack`、`MotionCurveClip` | `TimelineMotionCurve` | 采样位置、Yaw、Weight、空间、通道、优先级并提交 Motion contribution；世界求解和最终 Body 仍由角色正式链处理 |
| MotionWarp | `MotionWarpTrack`、`MotionWarpClip` | `TimelineMotionWarp` | 依赖同 Timeline 的 MotionCurve source 和 Action Context；source operation 由发射后引用绑定，状态与限制数学归 Motion domain |
| Tree | `TreeTrack`、`TreeClip` | `TimelineTreeClip` | `Decision` 只生成候选，`Commit` 执行 OnEnable/Root/OnDisable/OnDestroy；子树必须编译，不能运行作者 Graph clone |
| Action Cue | `ActionCueTrack`、`ActionCueClip` | `TimelineCue` | 在片段开始边界产生一次 Cue/Facts；不能因提取时间核心而变成通用对象写入 |
| Camera State | `CameraStateTrack`、`CameraStateClip` | `TimelineCameraState` | 229 基线实际由 Character Camera consumer 在 `weight <= 0` 时移除状态；持续效果仍采样 Weight/Ease、Mode、Priority、TargetKey、BlendOut。零权重保留身份、显式 Complete/Release 和 Camera 自己推进视觉尾段属于已确认目标/后续相机交接，不是本基线现状 |
| Camera Cue | `CameraCueTrack`、`CameraCueClip` | `TimelineCameraCue` | 在片段起点发出一次 Shake/FOV/Recoil 等 cue；具体字段与 consumer 由相机领域交接 |
| Camera Response | `CameraResponseTrack`、`CameraResponseClip` | `TimelineCameraResponse` | 持续输入响应，采样 Response 权重与三项输入权重；不与 Camera State 通过轨道数组顺序合成 |

当前 `Clip.UpdateMix` 按同轨片段区间计算 EaseIn/EaseOut 覆盖，是否拒绝重叠及领域混合不能继续依赖这个通用数组扫描；迁移时要把规则放入 Track/Clip 合同。轻重震等只有资源/参数差异的内容继续使用同一行为 kind。

## 现有资源、managed-reference 与产物闭包

静态 YAML 引用解析得到：

- shared `TimelineAsset` 脚本 GUID `fa5ae3c048510ba4bb65d1cfb507eb53` 当前被两个角色内容引用：Corin 的 `CorinAttack1Timeline.asset` 和 TrainingEnemy 的 `TrainingEnemyAttackTimeline.asset`。
- `TimelineNode` managed-reference 类型为 `BTSMTL.Timeline.TimelineNode`，程序集为 `BTSMTL.Timeline.Tree`；当前可见 owner 为 Corin `CorinPlayableRootTree.asset` 和 TrainingEnemy `TrainingEnemyCharacterRootTree.asset`。
- Corin shared Timeline 内包含 Animation、MotionCurve、MotionWarp、Camera、TreeClip 等可达内容；其 TreeClip 既有 inline `TimelineRunningTree`，也有 `BaseTreeAsset` shared 引用位。
- Corin 角色产物组当前位于 `Assets/Configs/Character/Corin/Pipeline/Definition/Generated/`，包括 `CorinCharacterPipelineDefinition.SimulationProgram.asset` 和 `CorinCharacterPipelineDefinition.PresentationProjection.asset`。它们是角色产物，不是独立 Timeline 产物。

程序集边界当前为：

- `BTSMTL.Timeline` 引用 TreeDesigner、RootMotion 和 `ThirdPersonSimulation.Core`，所以当前名字不能证明 portable 独立性。
- `BTSMTL.Timeline.Tree` 引用 Timeline、TreeDesigner 及 Graph 相关程序集，承载作者 TreeClip 与 TimelineNode。
- `ThirdPersonSimulation.Core` 已承载 `TimelineControlRuntime`、Timeline catalog/operation/state 合同和 Character Timeline target leaf；独立调用身份与 Scene Presentation sample 归 `BTSMTL.Timeline` 公共合同，Core 不保存独立调用实例。
- `BTSMTL.Timeline.Runtime` 只依赖 Timeline、Tree 和 Float32/Fixed/Core simulation，承载独立 Float32/Fixed playback、typed binding set 和帧输出提交。
- `BTSMTL.Timeline.Unity` 单向依赖 Runtime 与 `ThirdPersonSimulation.Unity`，承载 Unity 面板目标、业务主机和样例场景接线。
- Float32/Fixed 各自有 `TimelineTarget`、`TimelinePolicy`、Timeline codec/state layout 和唯一 `OperationEvaluator` 接线；当前 Program catalog 中已有 `Timeline`、`TimelineTrack`、`TimelineClip`、`MotionCurve` 条目及 `TimelinePlayback`、`TimelineLoop`、`TimelineTreeClipCycle`、`TimelineRetentionIdentity`、`TimelineLogicTime` 状态语义。

上述闭包是基于提交基线和本轮正式产物的静态 owner/GUID/程序集解析。独立产物已通过统一 IR、Float32/Fixed target lowering、artifact/store 原子发布和 wrapper 重读；旧候选路径生成的私有 Program、MCP、Host、PanelTarget 和 second runtime 均不属于当前交付。1044 帧角色输入仍只作为 Character 回放基线，不把它冒充为独立 Timeline 运行证据。

## 现有入口—新 owner—保留行为—删除项

| 现有入口 | 新 owner | 保留行为 | 接管后删除 |
| --- | --- | --- | --- |
| TimelineData/TimelineAsset authoring | Timeline authoring domain | inline/shared 唯一 owner、authoring identity、曲线与区段 | 运行目标、播放句柄、作者数据内运行状态 |
| CharacterSemanticEmitter.CompileTimeline | 共用 Timeline content emitter；Character integration 负责调用来源和 Skill 组合 | 角色 Skill 的 catalog、TreeClip、Numeric Target、原组合 root | Timeline 内容必须伪造 TimelineNode/Graph 的要求、重复 emitter |
| TimelineControlRuntime | 共用时间/生命周期模块 + Tree extension + typed target leaf | Once/Loop、跨边界进入/采样/退出、graceful/force stop | 核心对角色 Blackboard、Motion/Camera/Cue 具体实现的直接知识 |
| Float32/Fixed TimelineTarget | 各 Numeric Target 的正式适配层 | 角色状态布局、唯一 Evaluate/Finalize/Commit、ActionInstance generation 和领域输出 | 独立调用构造空 Character 或复制 State mirror |
| TimelineNode | Graph 调用适配 | 控制流入口、所属调用点、Skill ActionInstance 状态映射 | Timeline 独立内容的假 Graph 根、旧 playback service 旁路 |
| TimelineRunningTree | 编译树/TreeClip extension | 子树局部状态、父子停止、Decision/Commit 顺序 | 作者对象运行 clone、RunnableTree 例外执行 |
| Document/MCP Timeline 路由 | 主重构提供唯一 v5 文档 owner；本分支提供精确 Timeline Build bridge | shared root、typed 声明、独立产物与运行绑定不混写 | 本分支不复制文档 codec、事务、AI 路由或旧 v4 兼容链 |

## 当前缺口与交接限制

1. Character Graph 的 Timeline 调用和 Independent Root 都已接入同一 Semantic emitter；Skill 状态继续由 Character ActionInstance/SkillExecutionState 持有，非 Skill 状态由 Float32/Fixed Playback 实例持有，二者没有状态镜像。
2. 已允许修改四个共享 Timeline 运行文件，但不得改写 OperationControlRuntime、ActionStore、角色 codec 或复制主重构 owner；Camera/ScenePlay 的新增正式接口仍按各自提供提交接入。
3. Camera/ScenePlay 的新增正式接口尚未进入此基线。相机持续/瞬时、零权重、cycle/sample、显式逐实例停止和视觉尾段必须按提供合同接入，不能在通用核心里猜测。
4. 当前独立样例覆盖双面板标量曲线、显式目标绑定、并发占用隔离、自然完成、停止释放和 Float32/Fixed 产物；样例本身没有加入角色 Motion、Camera 或 Character Blackboard。遇到这些能力时编译/运行会按正式能力合同拒绝，不会降级写场景对象。

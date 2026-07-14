# Project Context

## Purpose

本项目是求职向 Gameplay 客户端程序 demo。目标不是完整 PvPvE 产品、MMO、纯网络框架或通用编辑器产品，而是展示第三人称动作客户端能力：输入响应、角色控制、相机、动作状态、动画表现、战斗窗口、受击反馈、调试可视化，以及在 `2v2vE / 2v2 + PvE` 服务端权威压力下保持手感。

当前真实重心是先把 BTSMTL authoring 底座打干净，再用它承载 StateMachine、Timeline、Tree、Action 等玩法创作数据。Gameplay runtime 和网络演示要建立在这条干净数据链路上，不从旧 SO/config 分裂路径恢复。

## Current State

- `refactor-pipeline-blackboard-owned-scopes`、`restore-timeline-treeclip-pipeline-runtime`、Timeline inline/TreeClip 收口、Corin nested Attack 重构、`refactor-gameplay-network-model-boundary`、Gameplay Effect/Tag/Attribute runtime 与 `refactor-character-motion-simulation-boundary` 均已归档，其 delta 已合并进 current specs。
- `refactor-animation-presentation-authoring-boundary`、`refactor-runtime-diagnostics-capture-lifecycle`、`refactor-network-correction-policy-boundaries`、`refactor-live-debug-view-bindings` 与 `refactor-gameplay-effect-runtime-integration` 已完成但尚待归档。
- `add-local-two-client-gameplay-network-closure` 的运动边界前置条件已经完成，仍需先选择并实现唯一正式服务端权威运动 backend。
- `openspec/specs/` 当前包含 48 个已归档 current spec：
  - `agent-character-controller-synthesis`
  - `btsmtl-agent-authoring-mcp-bridge`
  - `btsmtl-bt-edge-condition-decorators`
  - `btsmtl-componentized-node-authoring`
  - `btsmtl-graph-core`
  - `btsmtl-graph-data-catalog-authoring`
  - `btsmtl-input-action-node-authoring`
  - `btsmtl-node-interruption-lifecycle`
  - `btsmtl-runnable-timeline-node`
  - `btsmtl-runtime-diagnostics`
  - `btsmtl-sm-node-authoring`
  - `btsmtl-timeline-editor-preview`
  - `btsmtl-tree-inspector-information-architecture`
  - `character-action-activation-flow`
  - `character-action-authoring-closure`
  - `character-action-instance-runtime`
  - `character-action-network-policy-authoring`
  - `character-animation-layer-runtime`
  - `character-animation-pipeline`
  - `character-animation-presentation-authoring`
  - `character-camera-pipeline`
  - `character-gameplay-pipeline-closure`
  - `character-gameplay-effect-authoring`
  - `character-gameplay-effect-integration`
  - `character-gameplay-sync-adapter`
  - `character-input-node-authoring`
  - `character-input-pipeline`
  - `character-motion-semantics`
  - `character-motion-simulation-boundary`
  - `character-network-sync-domain-contract`
  - `character-pipeline-blackboard`
  - `character-pipeline-runtime`
  - `character-presentation-interpolation`
  - `character-root-motion-curves`
  - `character-state-interruption-authoring`
  - `character-state-timeline-authoring-loop`
  - `character-syncfact-behavior-binding`
  - `gameplay-behavior-policy-model`
  - `gameplay-attribute-runtime`
  - `gameplay-effect-runtime`
  - `gameplay-network-model-boundary`
  - `gameplay-sync-backend-selection`
  - `gameplay-sync-runtime`
  - `gameplay-tag-runtime`
  - `gameplay-tick-system`
  - `local-gameplay-sync-loopback`
  - `server-authoritative-hybrid-sync-model`
  - `tengine-hotupdate-foundation`
- 客户端主目录是 `3cDemo/Client/3C_Client`。
- 当前稳定脚本主模块位于 `Assets/GameScripts/Main/Runtime`，包括 `Camera`、`Rendering`、`BTSMTL`、`Gameplay`、`Networking/GameplayNetwork`、`Networking/ServerAuthoritativeHybrid` 和 `Character/Pipeline`；旧 `Assets/Scripts` 不再作为正式代码根目录。
- 服务端 `3cDemo/Server` 只保留 Fantasy 骨架，不再保留旧 FrameSyncAuthority 业务。
- `Ref` 是参考代码来源，不是运行时依赖。

## Tech Stack

- Unity 2022.3.62f2c1。
- C#、UI Toolkit、GraphView。
- URP 14、Cinemachine 2.10、Unity Input System、Unity Timeline。
- BTSMTL / TreeDesigner / Timeline 本地代码。
- Fantasy.Unity / Fantasy.Net 骨架。
- OpenSpec 用于能力规划和归档。

## Architecture

### Gameplay Client Direction

- 对外作品口径是 `Network-aware Third Person Action Combat Prototype`。
- 第一目标是 Gameplay 客户端纵切，不是完整网络产品。
- 客户端主链路：`Input -> Action Request -> State/Graph Decision -> Timeline/Animation Presentation -> GameplayWindow Facts -> Prediction Presentation -> Server Result -> Motion Correction Application -> Presentation Sampling`。
- 动画表现主链路：`State/Action 逻辑所有权 -> per-layer AnimationLayerSelection -> TimelinePlaybackScheduler visual sample -> CharacterAnimationPlaybackCommandQueue -> AnimationPlaybackLifecycle -> AnimancerPlaybackAdapter -> Animancer layer/state/fade -> output`。CharacterPipeline 唯一构造 Queue；逻辑侧每层只提交一个已解析 playback，PresentationFrame 原子消费 selection、sample、complete 与 release，不从 Tree 结构再次推断赢家。
- Timeline Scheduler 只收集活跃 animation producer；逻辑选择器以 ActionRuntime 当前唯一 ActionInstance 解析动作覆盖，并在动作 Timeline 已结束但 terminal lifecycle 尚未提交时保持已有选择。启动时保存的 ActionContext 不等于当前所有权，也不能作为表现赢家判断。
- `CharacterPipelineDefinition.AnimationPresentation` 是角色动画 Layer catalog、唯一 Animancer `TransitionLibraryAsset` 引用和 Timeline producer binding 的唯一正式来源。Layer 显式保存 OutputPolicy；producer binding 使用稳定 Timeline/Track identity 绑定 Animancer transition 与 easing。Priority、Tree site、Driver、source-target transition table 和运行时 lifecycle 不进入该配置。
- 表现插值只对 logic pose 生成 visual root；Timeline 动画在表现帧按 visual Timeline time 重采样，Animancer fade 按真实 presentation delta 独立推进。`AnimationPlaybackLifecycle` 只管理 PendingFirstSample、Current、Outgoing、Retired 与纯表现 retention；Animancer 是状态复用、层混合、权重和淡入淡出的唯一执行权威。
- 本地相机链路：`CameraStateRequest / CameraCue / CameraResponsePolicy / CameraTargetRequest -> CharacterCameraStage -> CameraPosePlan -> CameraRigAdapter`；相机状态不进入 gameplay 同步真相。
- CharacterPipeline 不解析网络策略。当前唯一 `ServerAuthoritativeHybrid` 模型通过 `ServerAuthoritativeCharacterSyncProfile` 保存 Behavior/Action policy，并由模型专属 Behavior/Transaction resolver 构造 packet；ActionProfile、GameplayBehaviorProfile、Graph、Timeline 与 Blackboard 不保存该模型策略。
- Action decision、actor 逻辑位姿 correction 与表现采样使用独立合同：Reject/Correct 只改变 ActionInstance lifecycle，incoming Correction 只进入 CharacterMotionStage，Presentation 只消费正式 `MotionCorrectionApplicationResult`，不得从 MotionDebug 反向读取运行决策。
- 角色运动链路固定为 `MotionContribution -> MotionResolver/Modifier -> MotionIntent -> Motion Executor -> MotionResult`。`CharacterMotionStage` 只编排 gameplay intent、correction 和结果；正式 Logic Pose Port 唯一读写逻辑位姿；当前 Unity `CharacterController.Move` 只允许存在于 `UnityCharacterControllerMotionExecutor` adapter 内。Graph、Timeline、Action、Presentation 和 Network Model 不直接调用具体运动组件。
- Timeline 的时间窗口统一由 Decision TreeClip 写入 Bool Frame/Frame Pipeline Blackboard variable；需要动作事实的 declaration 通过显式 ActionWindow projection，在 RootTree 决策后生成 `ActionWindowSample`。Timeline 不再提供 ActionWindowTrack/Clip 或专用 Window reader。
- ActionWindow projection 只保存 WindowType、WindowId 和 Digest；ActionInstance 来自 playback/Graph 的显式 Action Context。NetworkSendStage 只收集 SyncFacts，完整网络策略由绑定角色的 `ServerAuthoritativeCharacterSyncProfile` 解析。
- Timeline 不直接宣称命中成立；命中、伤害、目标归属必须由服务端或权威 gameplay solver 裁决。
- Gameplay Effect 通用模块只拥有 Tag、Attribute、ActiveEffect、PredictionJournal 和每 tick ChangeSet；它不引用 Character、BTSMTL、Network Model、Presentation 或 Diagnostics。`GameplayEffectRuntime` 是五个窄合同的门面，Spec 构建、应用事务、生命周期、Component 执行、预测协调和变更记录由内部协作者完成。
- Character 通过 `CharacterGameplayEffectAdapter` 接入 GE。Graph 只获得不可变的 Tag Reader、Attribute Reader 和 Effect Command Sink；incoming lifecycle、attribute 与 result application 由 InputMapper 转为 authority input；同一 ChangeSet 在 commit 时只 drain 一次，再分别投影为正式 Effect/Attribute facts、Gameplay Cue 和结构化 diagnostics。

### BTSMTL Authoring Direction

- BTSMTL 是当前 authoring 基座，不是必须照搬的 runtime。
- `BaseGraph` 是图数据和结构编辑底座，`BaseTree` 继续作为图数据类型，`BaseTreeAsset` 是当前可打开的 Unity asset / editor 入口。
- `BaseNode` 是节点 authoring entity，可以承载 `NodeModule`。
- 字段扫描走 `NodeFieldAccessor`，同时支持节点字段和模块字段。
- Port 系统继续使用 BTSMTL 原生 `PropertyPort` / `PropertyEdge`，连接身份使用稳定 `PortId`。
- 不新增 `WorkbenchPortDescriptor`、并行注册表或并行 WorkbenchTree。
- 默认创作心智是 private-first：节点、边、模块和端口默认内联在所属 Graph；可下钻 Graph 默认作为 owner 内部普通 C# inline graph data 自动创建和绑定；需要复用时才显式提升或分配 shared `BaseTreeAsset`。
- Unity sub-asset 不再表达 BTSMTL 私有下钻 Graph ownership；SMNode -> SMGraph、TransitionEdge -> ConditionRuleGraph 等关系必须通过 inline/shared 引用、ownership 和校验维护。
- `StateMachineGraph : BaseTree`，`StateMachineNode` 表达父级行为图进入状态机图的入口；创建 `StateMachineNode` MUST 自动创建并绑定 inline `StateMachineGraph`，用户不需要先手动创建或拖拽引用。
- 嵌套 StateMachine 通过 StateNode 的 inline StateBehaviorSubTree Root 内普通 `StateMachineNode` 表达；runtime 使用 outer-to-inner `StateMachineExecutionPath` 按 declaration owner 解析 State Blackboard frame。StateMachine 只负责 transition decision、State scope、source exit barrier 和状态运行事实，不创建动画 owner、ready、control-flow topology、可见 leaf 或专用动画 lifecycle。
- Animation incoming readiness 只来自所选 Timeline producer 的第一份合法表现 Sample，不来自 Runnable executed 或额外 ready fact。逻辑 playback complete/release 后，outgoing 只通过 `PresentationRetention` 继续采样 animation track，不能继续执行 TreeClip、Motion、root motion、window 或 sync facts；Animancer fade 完成后 lifecycle 将其 Retired。
- Corin 外层 Action StateMachine 只包含 `None`、`Attack`、`DodgeBack`、`DodgeForward`；`Attack` body 内的 inline `Attack Combo StateMachine` 包含 `Attack1`、`Attack2` 与 Exit，具体攻击 leaf 独占 Action activation、Action Context、inline Timeline、Hit/Cancel TreeClip 和 terminal lifecycle。
- `StateNode` 表达状态机图内普通状态和状态行为边界，它本身与 Transition edge 都是 `StateMachineGraph` 内联数据，不是独立 asset。
- `Enter`、`AnyState`、`Exit` 是 StateMachineGraph 层级控制节点，不是普通状态模块。
- `StateNode` 可引用普通 `SubTree` 或 `StateBehaviorSubTree`；普通 `SubTree` 只执行 `RootNode`，`StateBehaviorSubTree` 使用 `OnEnter`、`RootNode`、`OnExit` 表达状态生命周期。
- Transition 是 edge 语义，不新增 `TransitionNode`；Transition 条件通过 edge 内部 inline `ConditionRuleGraph` 或显式 shared `ConditionRuleGraph` asset 表达，不回退到同层 BoolPort 条件。
- Graph Editor 中的 Node、Edge 和 StateMachine Transition 只保存逻辑结构、条件、优先级、打断与 ownership；`CharacterPipelineDefinition` Inspector 是 Layer catalog、producer binding 和 Animancer library 定位的唯一 Presentation 配置入口，只按 RootTree 正式 producer identity 显示绑定，不复制逻辑 flow。Graph 数据不保存动画策略，不存在独立 Animation Presentation 窗口。
- 普通 Tree、StateMachine、graceful stop 与 ForceStop 只发布和消费逻辑执行事实。State/Action 逻辑在所有权决策后通过 `CharacterGraphContext` 提交每层唯一 `AnimationLayerSelection`；Tree priority 和 interruption 不进入 Animation 合同，逻辑 State 在 stop barrier 内退出且不等待表现淡出。
- 动画转场时长、transition source 与 easing 由 Animancer 原生 TransitionLibrary、state 和 FadeGroup 执行；项目不维护自有 CrossFade/Inertialization 状态机。动画表现不得写 root motion、逻辑 Transform、MotionCurve、黑板或同步事实。
- BT Composite child 条件与 StateMachine Transition 共用 `ConditionRuleGraph`；`IfNode` 和状态机专属 `TransitionRuleGraph` 不再保留。
- Runnable stop 使用自然完成、graceful stop 和 force stop 分层协议；State Transition 与父 Tree abort 共用 source-exit 和 OnExit 内核。
- `TimelineNode : RunnableNode`，用于 Graph 驱动 Timeline；默认唯一持有 inline `TimelineData`，只有作者显式 Use Shared 或 Extract Shared 时才引用 `TimelineAsset`，inline/shared 不允许双写。
- `BaseTreeWindow` 的页面栈只承载 Graph 与 TreeClip resolved Graph，Timeline 不进入 Graph breadcrumb；`TimelineEditorWindow` 独立绑定 TimelineNode/TimelineAsset 的 TimelineData，使 Graph 与 Timeline 可同时观察。Timeline 中打开 TreeClip 时由来源 Graph 窗口下钻，并显式传递 Character authoring context，不复制 Blackboard declaration。
- Graph、Node、Edge、Timeline、Track、Clip 和 Blackboard declaration 使用稳定 authoring identity；runtime clone 保留 source identity，但每个 Character、Graph、State activation、Timeline playback 和 TreeClip cycle 使用独立 runtime instance identity。
- BTSMTL 运行调试边界固定为 `Authoring Source -> Debug Source Map -> structured Trace -> RuntimeDebugSession -> Graph/Timeline/Host view`。Graph channel 显示逻辑 child 选择、Runnable result 与 stop；StateMachine channel 只显示 transition decision、State scope 与 exit barrier；Animation channel 显示 selection、Timeline sample、PendingFirstSample、Current、Outgoing、Retired 与 Animancer fade。Editor 不绑定 runtime clone，也不重建第二份 selection、Timeline 时间或播放生命周期。
- 每个 active CharacterPipeline 注册独立 diagnostics target，默认不启用采集。Graph、StateMachine、Timeline、Blackboard、Motion、Animation 六类 channel 由 Graph、Timeline、Host Inspector 的 target-level Live interest 取并集；Live State 只保存当前事实。作者显式开始 Capture 后才建立有界 segment history，停止后由 Graph、Timeline 与 Host Inspector 共享同一冻结 Capture position。Graph 和 Timeline 分别在各自 editor-only binding 中保存 Follow/Pin 与 runtime instance；Host Inspector 只读共享 provider，不拥有 Graph 或 Timeline 实例选择。
- BTSMTL `TreeTrack / TreeClip / TimelineRunningTree` 已接入 `TimelinePlaybackScheduler` 唯一运行权威：Decision 在 RootTree tick 前无状态求值并只写 Frame Blackboard，Commit 在 RootTree tick 后保持 Enter/Update/Exit/Destroy 与 stop 生命周期；不恢复 `Timeline.Bind/Evaluate/Unbind` 自主播放路径。
- Decision TreeClip 按每次 logic tick 穿过的 Timeline segment 求值；Loop 跨边界时拆分尾段、中间 cycle 与头段，不能只检查 target time。`BaseGraph.InitTree` 使用统一非虚入口，TimelineRunningTree 必须先提供正式 TreeClip runtime context。
- Motion contribution 明确区分位移 delta 与 Override channel claim。MotionCurveClip 的 `CurveEndFrame` 决定位移曲线终点，`EndFrame` 决定贡献/占权终点；零 delta Action claim 可以在 recovery 中消费 Locomotion，但 Additive/WeightedBlend 零值不占权。
- TreeClip 默认在 Timeline managed-reference 中拥有 inline `TimelineRunningTree`，复用时才显式 Extract Shared，inline/shared 只能保留一个真数据来源；Corin Attack Hit/Cancel、Dodge IFrame 与恢复段都由 Decision TreeClip 写 Root-owned Frame variable，状态边和 OnExit 统一通过 Blackboard ValueNode 读取。
- Blackboard fact projection 的运行顺序固定为 `BeginFrame -> Decision TreeClip write -> RootTree/StateMachine decision -> WindowFactProjection -> Timeline Commit -> SyncFacts/NetworkSendStage`；projection candidate 只保存当前帧写入 provenance，不是第二套 Blackboard。
- Pipeline Blackboard 继续使用 `BaseExposedProperty` 作为唯一 declaration/调参表面；Character declaration 归属 RootTree，Graph、State、ActionInstance、Frame declaration 归属实际使用它们的 Graph，不创建第二套 Blackboard asset 或局部字典服务。
- Blackboard 节点保存 declaration identity 与 declaration owner 的显式 reference，`BlackboardKey` 只在同一 owner 内唯一并用于作者显示；下钻 Graph 读取上层可见 declaration 时只建立引用，不复制 declaration，也不按名称隐式 shadow。
- Blackboard runtime address 由 declaration identity 与 Character runtime、Graph runtime instance、完整 `StateMachineExecutionScope`、`ActionInstanceId` 或 local logic tick owner 共同组成；State、ActionInstance、Frame 和 GraphInstance 清理只能命中对应 owner bucket。
- 正式 scope/lifetime 组合固定为 Character 的 Config/Spawn/ManualClear、Graph 的 Config/GraphInstance、State 的 StateEnterToExit、ActionInstance 的 ActionInstance、Frame 的 Frame；Config 在 runtime 只读，缺失 owner、断裂引用和类型错误直接失败。
- Agent authoring 是 editor-only 编译链路：`Snapshot -> Intent/Macro -> Patch IR -> Compiler -> Validator -> Report -> BTSMTL assets`；schema v6 输出完整 Graph、Node、Edge、Graph reference、Timeline、Track、Clip、Blackboard declaration 与 animation producer 稳定 identity，并只读展示 Layer、TransitionLibrary 与 producer binding。Patch IR 只修改正式 Graph、StateMachine、Timeline 和 Blackboard authoring，不提供 `configure_animation_layer`，也不编辑动画 Priority、Tree presentation site、Animancer transition 或播放 lifecycle；Validator 不建立第二套 Presentation 校验。不保留 v5、path/index、display-name apply fallback、旧动画字段或双写入口。

### Network Boundary

- 求职目标是 Gameplay 客户端程序，不是 Network Engineer。
- 网络压力场景按 `2v2vE / 2v2 + PvE` 设计：两队玩家在共享空间内互相打断、支援、集火，并争夺 PvE 单位或目标点。
- 网络架构是混合模型：不做全局确定性帧同步，也不是纯 snapshot；owner actor 使用 tick command prediction/reconciliation，动作使用 ActionInstance transaction sync，combat 使用 window/history rewind，远端 actor 使用 snapshot interpolation，PvE/objective 使用 server event replication。
- 网络装配边界固定为 `GameplayNetworkSessionHost -> GameplayNetworkModelDefinition -> model session`。一个 Session 只装配一个完整模型，Character、Graph、State、Action 和 Timeline 不选择模型；当前唯一完整模型是 `ServerAuthoritativeHybrid`。
- `ServerAuthoritativeHybridSession` 唯一拥有 packet、精确 SubjectActorId 队列、history、debug 和 endpoint。Character binding 只保存 SessionHost、CharacterPipelineHost、SubjectActorId 与模型 profile；多个角色共享同一 session runtime。
- CharacterPipeline 只输出 CharacterInputFrame、ResolvedCharacterMotionFact、Action/window/result、GameplayEffect lifecycle、Attribute value、GameplayCue 与 correction application result，并只接收 `ActionLifecycleTransition`、external pose、result、GameplayEffect lifecycle、Attribute value 与 GameplayCue 等语义输入。`ResolvedCharacterMotionFact` 表达客户端本地已发生的 prediction result，只用于预测对账、诊断或 correction provenance，不是服务端 canonical motion intent。`PredictionKey` 仍是 ActionRuntime 生成的动作事务关联身份，并由模型 adapter 复制到模型 envelope；authority tick、defense-favor、packet identity 与网络裁决 metadata 留在模型内部。
- `ServerAuthoritativeHybrid` 按每条 Effect fact 的 `BehaviorId` 解析生命周期策略，Effect 引起的 Attribute fact 按 cause BehaviorId 解析，无 Effect cause 的 Attribute correction 必须使用显式 fact binding。模型使用类型化 lifecycle、attribute 与 cue payload；Character 与 GE 不保存 packet、模型 policy 或 history。
- endpoint 由模型专属 EndpointDefinition 创建。未配置表示明确 disconnected；当前唯一实现是 LocalLoopback；未来 Fantasy 必须增加独立 EndpointDefinition，不修改模型核心 enum/switch，也不得在连接失败时回退 Loopback。
- 模型队列区分业务可靠性：MotionCommand/MotionSnapshot 只可替换同 actor 同类旧流样本；Action、Result、CorrectionAck 等事务事实容量不足时明确失败，不能静默丢弃。
- 本地玩家可以预测自己控制 actor 的移动、转向、闪避、防守、攻击启动、支援动作、动画、特效和镜头表现。
- 队友玩家、敌方玩家和 PvE 单位使用服务器快照、确认动作事件和插值，不复制完整本地预测。
- 服务端裁决位置真值、动作真值、窗口真值、命中、伤害、目标归属、怪物状态和局内事件。权威位置必须由服务端从 canonical input、accepted action state、角色配置和当前 body state 独立生成 motion intent，再由唯一正式 simulation backend 求解；不得把客户端 applied displacement 累加为 canonical pose。
- 当前 owner 位姿 correction 只保留 CharacterMotionStage 内部唯一 correction phase：partial delta 进入正式 Motion Executor，full relocation 进入 Logic Pose Port；不在 ActionProfile、GameplayBehaviorProfile 或 CharacterPipelineDefinition 暴露算法配置。每次成功应用后只输出包含 input sequence 与 server tick 的独立 acknowledgement。
- PvPvE 局内业务压力包括 team/player/actor ownership、objective ownership、capture/contest、PvE aggro/threat/break、assist/rescue、resource/cooldown、downed/revive/respawn 和 score/result event。
- PvP 命中使用服务端权威加局部 combat rewind，只回溯 pose、hurtbox、action window，不回滚整个世界。
- 防守相关动作采用“模糊边界防守优先，明确命中攻击成立”的裁决策略：本地防守立即预测，服务端在可配置模糊窗口内优先承认防守，命中、伤害和目标归属仍由服务端确认。
- `ServerAuthoritativeHybrid` 后续可选择 Unity authoritative process 或 Fantasy 纯 C# KCC 作为一次实现中的唯一权威运动 backend；两者共享 canonical input/action 与 snapshot/correction 模型语义，但不能同场双算或互为 fallback。DotRecast 只提供导航查询，不等于 KCC。
- 确定性 KCC、lockstep 或 rollback 必须作为另一完整 Network Model，拥有自己的定点数、world state、input history、replay 和 side-effect commit；在完整实现前不进入当前配置。当前不做全局帧同步、不做完整 rollback、不做客户端权威。

## Code Organization

- `Assets/GameScripts/Main`：TEngine AOT 启动入口和 Procedure 流程。
- `Assets/GameScripts/Main/Runtime/BTSMTL/Scripts`：BTSMTL 基础工具、反射、通用属性。
- `Assets/GameScripts/Main/Runtime/BTSMTL/Diagnostics`：编译无关 source identity、Source Map、按需 Live State/显式 Capture store、共享增量 Editor Session 和只读 view model。
- `Assets/GameScripts/Main/Runtime/BTSMTL/TreeDesigner/Scripts`：Graph、Tree、Node、Edge、PropertyPort、ExposedProperty。
- `Assets/GameScripts/Main/Runtime/BTSMTL/TreeDesigner/Editor`：节点图窗口、节点视图、端口视图、搜索和 inspector。
- `Assets/GameScripts/Main/Runtime/BTSMTL/Timeline/Scripts`：`TimelineData`、显式共享 `TimelineAsset`、Track、Clip、Playable、TimelineNode。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Animation`：角色动画公共合同，以及 Lifecycle、Diagnostics 的正式业务实现；不包含候选 Arbitration 或第二套混合器。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline/Presentation`：表现帧事务聚合、logic pose 插值与具体 Animancer adapter；不承载 Action/Cue 同步事实或动画业务仲裁合同。
- `Assets/GameScripts/Main/Runtime/Character/Pipeline` 其它目录：角色 pipeline 稳定入口、Graph/Logic/Motion/Network、Unity 绑定和资产引用类型。
- `Assets/GameScripts/Main/Runtime/Camera`：第三人称相机模型、solver、runtime adapter。
- `Assets/GameScripts/Main/Runtime/Rendering`：动作表现相关后处理和 VFX runtime。
- `Assets/GameScripts/Main/Runtime/Networking/GameplayNetwork`：model-neutral model definition、model session 和 SessionHost 生命周期边界，不包含具体 packet/policy。
- `Assets/GameScripts/Main/Runtime/Networking/ServerAuthoritativeHybrid`：当前模型的 profile/resolver、Character adapter/binding、packet、queue、history/debug、EndpointDefinition 与 LocalLoopback endpoint。
- `Assets/GameScripts/HotFix`：TEngine 热更程序集目录，按 `GameBase`、`GameProto`、`BattleCore`、`GameLogic` 分层。
- `3cDemo/Server`：Fantasy skeleton，只作为后续最小权威服务端基础。

## Conventions

- 生成代码尽量少写注释，只有关键复杂边界写少量注释。
- 不做 fallback 配置、兼容镜像、临时桥接路径或双主线。
- 旧数据、旧路径、旧命名确认不用就直接删除。
- 修改代码不用 MCP 写文件；Unity MCP 只用于查看状态、console 或编辑器操作。
- 永远不要运行 Unity batchmode。
- 文档读取必须显式 UTF-8。
- 默认不新增测试，除非用户明确要求。
- 用户负责 Unity 端到端验证；不要把手动验证写进 OpenSpec task。

## Cleanup Rules

- 旧 Workbench 路径不恢复。
- 旧 locomotion 特化 SO/config 不恢复。
- 旧 action SO、footphase profile、bodyclaim policy、AnimationPresentationPolicy 等如果脱离节点/模块/Timeline 继续作为当前数据源，应迁移或删除。
- `Ref` 中代码只能复制进正式模块后改名归属，不能作为运行时依赖。
- archive 只查历史，不作为当前实现目标。

## Open Questions

- 动态 `List<PropertyPort>` 的通用编辑器 UI 还需要继续收口。
- 最小网络压力场景仍停留在 disconnected/LocalLoopback endpoint；真实 Fantasy EndpointDefinition、生成协议、服务端双人 Room、远端角色 roster 与快照表现纵切尚未实现，后继 change 是 `add-local-two-client-gameplay-network-closure`。远端角色必须复用 `ExternalFacts + ExternalPose`，不得恢复 `RemoteProxy` 总控枚举或每角色网络 runtime。

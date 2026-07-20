## 1. 固定现状清单与迁移边界

- [x] 1.1 盘点 CharacterPipelineDefinition 引用的 RootTree、nested Graph、StateMachine、ConditionRuleGraph、Timeline、TreeClip、Blackboard、Action、Behavior、GameplayEffect 和 motion curve。
- [x] 1.2 记录 Corin 正式资产可达 Node、NodeModule、Edge、Track、Clip、ValueNode 和 authoring reference 类型。
- [x] 1.3 为每个可达类型记录当前执行入口、输入、可变状态、输出和外部副作用。
- [x] 1.4 盘点 RunnableNode、StateMachineGraphRuntime、TimelinePlaybackScheduler、TimelineRunningTree 和 PipelineBlackboardRuntime 中影响未来 Tick 的字段。
- [x] 1.5 盘点 ActionRuntime、GameplayEffectRuntime、CharacterMotionStage、CharacterInputStage 和 CharacterPipelineFrame 中影响未来 Tick 的字段。
- [x] 1.6 盘点 gameplay runtime 对 UnityEngine.Object、Time、Random、Input System、Camera、Transform、CharacterController 和 Animancer 的直接依赖。
- [x] 1.7 盘点 CharacterMotionAuthority、LogicPosePort、MotionExecutor、ExternalPose、MotionStage correction、NetworkSendStage 和 NetworkReceiveStage 的全部使用点。
- [x] 1.8 盘点 CharacterServerAuthoritativeBinding/Adapter 对旧 Character Pipeline stages 和 correction 结果的全部依赖。
- [x] 1.9 盘点 current specs 中 runtime clone、ExternalPose、NetworkStage、当前唯一可用 ServerAuthoritative 和旧数值边界要求。
- [x] 1.10 建立 source type -> emitter -> operation -> character state slot -> world request -> output channel 的唯一迁移表。
- [x] 1.11 建立旧 runtime type/field/asset -> 最终类型或删除原因的迁移清单。
- [x] 1.12 锁定 Program、CharacterState、WorldState、Kernel、SessionRuntime、Driver、Solver、Projection 和 Committer 的 assembly ownership。
- [x] 1.13 确认 assembly 依赖方向不形成 Runtime Core -> Unity/Editor/Networking/Presentation 反向引用。

## 2. 建立 portable 身份、Tick 与 Numeric Target 合同

- [x] 2.1 建立 ProgramId、ProgramRevision、ProgramHash、LayoutHash、ProgramCatalogHash、WorldRevision 和 SolverImplementationId。
- [x] 2.2 建立 ActorId、SimulationTick、OperationHandle、ActivationId、WorldRequestId 和 EventId。
- [x] 2.3 增加 SemanticHash、OperationSetVersion、NumericProfileId 和 TargetAbiVersion。
- [x] 2.4 定义 Numeric Target manifest，明确 scalar、vector、yaw、rounding、overflow、codec 和 deterministic capability。
- [x] 2.5 定义 canonical source numeric literal，保留 authoring 原值、类型、source identity 和目标精度要求。
- [x] 2.6 禁止 Semantic literal 保存公共 Fixed raw、公共 Float runtime value 或 float/fixed 双值。
- [x] 2.7 建立 target-parameterized scalar/vector/yaw contract，不让公共 Program ABI继续暴露唯一 SimScalar。
- [x] 2.8 建立 Float32 scalar、vector、yaw、normalized direction 和 finite-value 校验。
- [x] 2.9 建立 Float32 的除零、溢出、比较、插值和 canonical serialization 规则。
- [x] 2.10 建立 authoring literal -> Float32 constant lowering，并报告 NaN、Infinity 和不支持精度。
- [x] 2.11 建立 Unity/input float -> Float32 input 边界，不做未来 Fixed target 的预量化。
- [x] 2.12 建立 SimulationTickSourceIdentity，区分 LocalLogic、Authoritative 和 Replay 来源。
- [x] 2.13 明确 SimulationTick 不等于 RenderFrame、LocalLogicTick 或 ServerTick 的数值别名。
- [x] 2.14 建立 NumericProfile 匹配的 CharacterSimulationInput 连续值、离散 request、sequence、source tick 和 source identity 布局。
- [x] 2.15 建立 target-neutral SimulationIngress header，并让含数值 payload 显式属于当前 NumericProfile。
- [x] 2.16 建立 gameplay facts、presentation commands 和 structured trace 的公共 identity header与 target 数值 payload 边界。
- [x] 2.17 将 identity、Tick、Semantic literal、Numeric Target contract 和 Float32 ABI放入不引用 UnityEngine 的正式 source set。
- [x] 2.18 使用搜索确认公共合同没有按 Driver、Model 或 Node 选择 NumericProfile 的运行时 switch。

## 3. 建立 Numeric-Neutral Gameplay Semantic IR

- [x] 3.1 建立 Semantic IR manifest、source revision、compiler version、operation-set version 和 canonical header。
- [x] 3.2 建立 numeric-neutral operation、control-flow、reference 和 state declaration table。
- [x] 3.3 建立 source literal catalog，不提前生成 Float32 或 Fixed constant。
- [x] 3.4 建立 semantic input、world request、output channel、producer 和 capability declaration。
- [x] 3.5 建立 Semantic source map，将 operation、state declaration、literal 和 producer 映射回 Graph/Node/Edge/Timeline/Clip。
- [x] 3.6 建立 canonical string、identity、array、map 和 catalog 排序规则。
- [x] 3.7 建立 Semantic IR canonical writer/reader，不使用 Unity serializer、BinaryFormatter 或运行时反射。
- [x] 3.8 以 canonical Semantic IR bytes 计算稳定 SemanticHash。
- [x] 3.9 重复编译未修改 authoring 时生成相同 Semantic IR bytes 与 SemanticHash。
- [x] 3.10 禁止 Runtime Host加载或解释 Semantic IR作为 stale Program fallback。
- [x] 3.11 建立普通 .NET 可读取的 Semantic IR diagnostic artifact，不复制第二套 schema。

## 4. 建立 Numeric Target Compiler 与稳定 Program Artifact

- [x] 4.1 建立 Numeric Target registry，当前只注册正式 Float32 target。
- [x] 4.2 建立 Target Compiler context，只允许从 Semantic IR降低 constants、operations、state layout、world/output layout 和 source map。
- [x] 4.3 禁止 Target Compiler 改写 Semantic control flow、跳过 operation 或增加 Model 分支。
- [x] 4.4 建立 Float32 CharacterSimulationProgram manifest，记录 SemanticHash、NumericProfile、TargetAbiVersion 和 OperationSetVersion。
- [x] 4.5 建立 Float32 typed operation、constant、control-flow 和 reference table。
- [x] 4.6 建立 Float32 Character state、scope、world request 和 output channel layout。
- [x] 4.7 建立 Program gameplay capability 与 required world capability manifest。
- [x] 4.8 将 Semantic source map稳定投影到 target operation、constant、state slot 和 producer。
- [x] 4.9 建立 Float32 Program canonical writer/reader，并拒绝未知 NumericProfile 或 ABI version。
- [x] 4.10 以 SemanticHash、compiler、operation set、NumericProfile、capability 与 Program bytes 计算 ProgramHash。
- [x] 4.11 以 NumericProfile 与 canonical Character state layout 计算 LayoutHash。
- [x] 4.12 建立 Program artifact 的 source revision、SemanticHash、ProgramHash、LayoutHash 和 capability 校验。
- [x] 4.13 stale Program、target ABI 不匹配和 source revision 不一致时直接拒绝加载。
- [x] 4.14 重构 CharacterSimulationProgramAsset 为单 target artifact 容器，不保存双 numeric payload。
- [x] 4.15 建立普通 .NET 可加载的 Float32 Program 文件输出，不复制 Program schema。
- [x] 4.16 建立按 ProgramId 稳定排序的 SimulationProgramCatalog 与 canonical CatalogHash。
- [x] 4.17 校验 Catalog 中 ProgramId、ProgramHash、LayoutHash、SemanticHash 与 capability 唯一且完整。
- [x] 4.18 要求同一 Catalog 全部 Program 使用同一 TickRate、NumericProfile、TargetAbiVersion 和 OperationSetVersion。
- [x] 4.19 合并全部 Program required world capability，并在 Session 创建时校验唯一匹配 target 的 Solver 满足并集。

## 5. 建立 Target-Specific Character、World 与 Snapshot 状态模型

- [x] 5.1 建立 Float32 CharacterSimulationState storage 与按 target State Layout 索引的 typed slot API。
- [x] 5.2 建立 Runnable lifecycle、child cursor、stop barrier 和 activation generation slots。
- [x] 5.3 建立 StateMachine active/pending/exiting/transition/execution path slots。
- [x] 5.4 建立 Timeline playback、loop、TreeClip cycle、retention identity 和 Float32 logic time slots。
- [x] 5.5 建立 Blackboard value、scope owner、generation、lifetime 和 write provenance slots。
- [x] 5.6 建立 Action instance、request buffer、lifecycle、ActionContext 和 event sequence slots。
- [x] 5.7 建立 GameplayEffect tag、Float32 attribute、active effect、period、journal 和 ChangeSet cursor slots。
- [x] 5.8 建立 Float32 motion accumulator、pending world request、RNG、handle allocator 和 fact sequence slots。
- [x] 5.9 建立 Float32 WorldSimulationState 的 stable actor body table、world revision 和 solver state payload。
- [x] 5.10 建立 Character state canonical codec 与包含 NumericProfile 的 CharacterStateHash。
- [x] 5.11 建立 World state canonical codec，并要求 Solver 提供同 target reconstruct 或 snapshot codec。
- [x] 5.12 建立 SimulationWorldSnapshot header、NumericProfile、ordered actor state set 和 world state payload。
- [x] 5.13 建立 SimulationWorldHash，并让 deterministic validity 同时依赖 Numeric Target、Program 与 Solver capability。
- [x] 5.14 建立 snapshot capture 的只读冻结语义，不暴露可变 state reference。
- [x] 5.15 建立 restore 的 ProgramCatalogHash、NumericProfile、TargetAbiVersion、Actor Program/Layout、Solver、WorldRevision、Tick 和 roster 校验。
- [x] 5.16 建立 restore 原子替换，任一 actor/world payload 失败时保持当前状态不变。
- [x] 5.17 拒绝跨 NumericProfile 或跨 target ABI恢复 Snapshot，不做转换或量化 fallback。
- [x] 5.18 将 Driver/model history 与 Presentation state 排除在 Gameplay snapshot/hash 之外。

## 6. 建立 Compiler Frontend、Emitter 与 Target Lowering

- [x] 6.1 建立以 CharacterPipelineDefinition 为唯一 root 的 Compiler Frontend composition。
- [x] 6.2 建立 authoring identity resolver，拒绝空、重复、断裂和 owner 不匹配 identity。
- [x] 6.3 建立 Frontend Emitter registry，每个可执行 authoring type只能有一个正式 emitter。
- [x] 6.4 建立 Emitter context，只允许声明 Semantic operations、literals、state、world、outputs 和 source map。
- [x] 6.5 禁止 Emitter 保存 Unity runtime object、静态 mutable cache、Numeric Target 或 Driver/Model 分支。
- [x] 6.6 建立 nested Graph、inline/shared Graph 和 SubTree 的 Semantic 递归编译与循环引用校验。
- [x] 6.7 建立 StateMachineGraph、StateNode body、Transition edge 和 ConditionRuleGraph Semantic 编译。
- [x] 6.8 建立 TimelineNode inline/shared TimelineData、Track、TreeClip 和 MotionCurveClip Semantic 编译。
- [x] 6.9 建立 Blackboard declaration、reference、scope、lifetime 和 projection Semantic 编译。
- [x] 6.10 建立 Action、Behavior、GameplayEffect、Tag、Attribute 和 catalog Semantic 编译。
- [x] 6.11 建立 animation/camera/cue producer identity，但不把 Unity 资源或 target 数值写入 Semantic IR。
- [x] 6.12 建立 Float32 Target lowering pass，消费 Semantic IR生成唯一 Program artifact。
- [x] 6.13 建立 compile report，区分 Frontend identity/语义错误与 Target lowering/能力错误。
- [x] 6.14 缺失 emitter 时终止 Frontend，不调用旧节点虚方法或 runtime clone。
- [x] 6.15 Target 不支持 operation 或 literal 时终止 build，不跳过、不替换业务语义。
- [x] 6.16 重复编译未修改 authoring 时生成相同 SemanticHash、Float32 Program bytes 与 ProgramHash。
- [x] 6.17 CharacterPresentationProjection 从同一 source map编译并绑定 target producer identity，不再次遍历推断 gameplay flow。

## 6A. 建立 Float32 Kernel Evaluate/Finalize 与 Session 四阶段执行

- [x] 6A.1 建立 versioned Semantic operation-set contract 与唯一 Float32 Kernel backend。
- [x] 6A.2 建立无状态 Float32 SimulationKernel.Evaluate 输入与输出合同。
- [x] 6A.3 固定 Evaluate 的 operation、child、fact 和 event sequence 顺序。
- [x] 6A.4 让 Evaluate 只读取当前 Float32 Character state、control input、ordered SimulationIngress 和上一 Tick body observation。
- [x] 6A.5 建立 PendingCharacterEvaluation，并限制其只存在于当前 Tick 内。
- [x] 6A.6 建立 Float32 WorldSolveBatchRequest 的 ActorId、request id、before body、motion request 和 required capability。
- [x] 6A.7 建立无状态 Float32 SimulationKernel.Finalize 输入与输出合同。
- [x] 6A.8 让 Finalize 校验 world result 的 NumericProfile、ActorId、request id、Tick 和 solver identity。
- [x] 6A.9 让 Finalize 原子生成新 Character state、typed facts、body sample、presentation commands 和 trace records。
- [x] 6A.10 建立 SimulationTickResult，保存 target identity、ordered actor results、world result summary 和 snapshot identity。
- [x] 6A.11 建立 SimulationSessionRuntime 的 actor roster、NumericProfile 与 stable ActorId order。
- [x] 6A.12 建立每个 roster entry 的 ActorId、ProgramId、Character layout 与 World body binding。
- [x] 6A.13 让 SessionRuntime 按 Actor binding 从同 target ProgramCatalog 选择唯一 Program执行。
- [x] 6A.14 Session 启动后锁定 NumericProfile、ProgramCatalog 与 roster，拒绝 Driver TickPlan 中的未知 Actor、target switch 或动态增删。
- [x] 6A.15 实现 Tick 开始前 restore request target 校验与原子应用。
- [x] 6A.16 实现全部 Actor Evaluate 后只调用一次匹配 Float32 ABI的 ResolveBatch。
- [x] 6A.17 实现全部 Actor Finalize 与 OutputPlan 校验成功后才发布 current Character/World state。
- [x] 6A.18 任一 Evaluate、world solve、Finalize 或 OutputPlan 校验失败时禁止部分 actor state publish。
- [x] 6A.19 禁止 Kernel 或 SessionRuntime 读取 Unity Time、Camera、InputAction、Transport、packet、Presentation object 或运行时 Numeric Target switch。
- [x] 6A.20 使用搜索确认没有 Network Model 专用 opcode、节点或 Authoring 业务 runtime；当前只安装 Float32 backend。

## 7. 建立批量 WorldSolver 与 Unity 正式实现

- [x] 7.1 建立 target-parameterized ICharacterWorldSolver shape 的 identity、NumericProfile、ABI version、capability 和 world feature 合同。
- [x] 7.2 建立 Reconstructible、Snapshotable 与 DeterministicReplay 能力定义，并要求 deterministic validity 同时检查 Numeric Target。
- [x] 7.3 建立 Float32 WorldSolveBatchRequest 与 WorldSolveBatchResult 的 canonical portable 数据。
- [x] 7.4 要求每个 request 精确产生一个 result，并拒绝 target 不匹配、缺失、重复或未知 ActorId。
- [x] 7.5 建立 Float32 Solver world state create、reconstruct、capture、restore 和 dispose 生命周期。
- [x] 7.6 建立 Float32 UnityCharacterControllerWorldSolver composition，不让 portable assembly 引用 UnityEngine。
- [x] 7.7 将现有 UnityCharacterControllerMotionExecutor 能力收口到唯一 Unity Solver adapter。
- [x] 7.8 让 Unity Solver 按 stable ActorId order 执行当前 batch。
- [x] 7.9 保持 CharacterController.Move 只存在于 Unity Solver concrete implementation。
- [x] 7.10 将 Unity position、rotation、velocity、grounded 和 collision summary 转为 Float32 portable result。
- [x] 7.11 让 Unity Solver 从显式 WorldSimulationState 重建场景 body 对齐状态。
- [x] 7.12 Unity Solver 明确声明 Float32/Reconstructible，不声明 Snapshotable hidden internals 或 DeterministicReplay。
- [x] 7.13 缺失 CharacterController binding、Float32 body、target ABI 或 required capability 时创建失败，不搜索默认组件。
- [x] 7.14 删除旧 LogicPosePort 与 scene Transform 作为第二份逻辑位姿真值的路径。

## 8. 迁移 Runnable、Tree 与 StateMachine

- [x] 8.1 为 RootNode、Composite、Decorator、Leaf 和当前 Corin 可达 RunnableNode 建立 typed operation emitter。
- [x] 8.2 迁移 Runnable begin/update/complete/fail/cancel/interrupt/abort 生命周期。
- [x] 8.3 迁移 child cursor、edge condition、priority 和 LowerPriority/Self interruption。
- [x] 8.4 迁移 graceful stop、force stop、source exit barrier 和 OnExit 顺序。
- [x] 8.5 迁移 inline/shared SubTree instance、generation 和 owner scope。
- [x] 8.6 为 StateMachineNode、StateNode、Enter、AnyState、Exit 建立 operation emitter。
- [x] 8.7 迁移 StateMachine active、pending、exiting、transition decision 和 execution path。
- [x] 8.8 迁移嵌套 StateMachine outer-to-inner declaration owner scope。
- [x] 8.9 迁移 Transition ConditionRuleGraph 求值和 edge interruption metadata。
- [x] 8.10 迁移 State body Enter/Root/Exit 与父 Tree stop 的统一生命周期。
- [x] 8.11 StateMachine operation 只输出逻辑事实，不生成动画 lifecycle 或表现 owner。
- [x] 8.12 删除 RunnableTree、StateMachineGraphRuntime 和 State body runtime clone 的正式执行入口。
- [x] 8.13 保留非 Character 通用解释器时，将其 assembly、composition 和 state 与 Character Program runtime 明确隔离。
- [x] 8.14 删除通用 Tree scheduler 通过 CharacterGraphContext 提交 AnimationLayerSelection 的路径。
- [x] 8.15 将 State interruption、LowerPriority/Self、stop barrier 与 producer release 统一编译为 control-flow operation。

## 9. 迁移 Timeline、TreeClip 与 Blackboard

- [x] 9.1 为 TimelineNode request、playback、loop、complete、stop 和 release 建立 operation emitter。
- [x] 9.2 将 Timeline logic time 表达为 SimulationTick 与当前 NumericProfile 的时间值，不读取表现帧 delta。
- [x] 9.3 迁移 Decision TreeClip 在 RootTree Evaluate 前的无状态区间采样。
- [x] 9.4 迁移 Decision Loop 跨边界尾段、中间 cycle 和头段顺序。
- [x] 9.5 迁移 Commit TreeClip Enter、Update、Exit、Destroy 和 stop 生命周期。
- [x] 9.6 迁移 Timeline motion contribution、CurveEndFrame、EndFrame 和 channel claim。
- [x] 9.7 迁移 TreeClip ActionWindow projection、Action Context、WindowId 和 Digest。
- [x] 9.8 迁移 Timeline animation producer identity 与 presentation sample command。
- [x] 9.9 为 Blackboard declaration/reference 建立 Program catalog 与 state address emitter。
- [x] 9.10 迁移 Character、Graph、State、ActionInstance 和 Frame owner bucket 寻址。
- [x] 9.11 迁移 Config、Spawn、ManualClear、GraphInstance、StateEnterToExit、ActionInstance 和 Frame lifetime。
- [x] 9.12 迁移 Blackboard write provenance 与 fact projection，不创建第二份 window 数据源。
- [x] 9.13 在对应 lifecycle 终点清理 owner bucket，不依赖手动 null 写回。
- [x] 9.14 删除 TimelineRunningTree runtime clone、Timeline 自主播放路径和旧 Blackboard runtime dictionary。
- [x] 9.15 删除 Character gameplay 专用 InitTimelineTree 运行入口，让 TimelineRunningTree 只作为 authoring/compiler 输入。
- [x] 9.16 让 inline/shared TreeClip playback 共享不可变 operation 数据并使用独立 state address。
- [x] 9.17 在 Decision/Commit 采样前校验 Timeline retention ActionInstance，失效时统一停止 TreeClip、producer 与 camera，并禁止旧 Action 输出。
- [x] 9.18 让 ActionWindow-bound Frame 写入从正式 Timeline 或显式 FactContext 解析 ActionInstance provenance，不读取 ambient action。

## 10. 迁移 Input、Action 与 GameplayEffect

- [x] 10.1 将 InputAction 采样、Camera-relative 换算和 render-frame input capture留在 Unity Input Adapter。
- [x] 10.2 将连续输入按当前 Session NumericProfile 写入 CharacterSimulationInput，并保留离散 request、sequence 和 source tick。
- [x] 10.3 让 Graph input operation 只读取当前 Tick plan input，不读取 InputAction、Camera 或 CharacterInputStage。
- [x] 10.4 将输入 request buffering、consume 和 expiry 状态迁入 CharacterSimulationState。
- [x] 10.5 迁移 Action profile portable catalog 与 tag requirement。
- [x] 10.6 迁移 Action request buffer、ActionInstanceId、activation、accept、reject、cancel 和 complete。
- [x] 10.7 迁移 Attack1/Attack2 combo request、window gate、interrupt 和 terminal lifecycle。
- [x] 10.8 迁移 GameplayEffect runtime definition 为 Semantic catalog，并由 Float32 Target降低数值 constant。
- [x] 10.9 迁移 Tag、Float32 Attribute、ActiveEffect、period、inhibition、PredictionJournal 和 ChangeSet 状态。
- [x] 10.10 保持 GE transaction 与稳定顺序，并让异常回滚只修改 CharacterSimulationState。
- [x] 10.11 将 Action、Effect、Cue 和 Attribute 输出转换为 typed SimulationOutput facts。
- [x] 10.12 删除 Action/GE/Input 中影响未来 Tick但未进入 CharacterSimulationState 的字段。

## 11. 建立最小 Driver、Local 组合与 Committer

- [x] 11.1 建立带 NumericProfile 的 SimulationTickPlan、ordered ActorInput set 和 required capability 数据。
- [x] 11.2 建立 ordered SimulationIngress set，按 ActorId、source tick、sequence 和 fact identity 排序。
- [x] 11.3 建立 SimulationRestoreRequest，只允许引用完整 SimulationWorldSnapshot。
- [x] 11.4 建立 ISimulationDriver.PrepareTick、TryBuildRestoreRequest、ObserveTickResult 和 BuildOutputPlan。
- [x] 11.5 禁止 SimulationIngress 保存 packet、endpoint、history、server policy 或 transport metadata。
- [x] 11.6 禁止 Driver 获得 mutable Character/World state、调用 Kernel 或直接调用 Solver。
- [x] 11.7 建立 SimulationOutputPlan 与 Publish、Replace、Retire、Suppress EventId lifecycle。
- [x] 11.8 建立 Float32 LocalSimulationDriver，将 LocalLogicTick 映射为当前 Session SimulationTick。
- [x] 11.9 让 Local Driver 从 Float32 Unity Input Adapter 构造唯一 Actor input set，并提交空 ingress set。
- [x] 11.10 让 Local Driver 不创建 history/replay，成功 Tick 后为全部新 EventId 产生 Publish disposition。
- [x] 11.11 禁止 Driver 通过 OutputPlan 接受、拒绝或部分改写 staged Gameplay state。
- [x] 11.12 在 OutputPlan 完整校验后原子发布全部 Character/World state。
- [x] 11.13 建立 SimulationCommitter 的 gameplay/model-neutral output 与 presentation 端口。
- [x] 11.14 Committer port 失败时 fail-stop 并报告 EventId，不自动重放或回滚已触发副作用。
- [x] 11.15 禁止 Committer 修改 CharacterSimulationState、WorldSimulationState 或重新执行 operation。
- [x] 11.16 将 CharacterPipelineHost 收口为 Float32 ProgramCatalog、Projection registry、Float32 Kernel/SessionRuntime、Local Driver、Unity Solver、Committer 和 diagnostics 装配器。
- [x] 11.17 让 GameplayTickSystem 只调度 Simulation Session target，不再逐个调用 CharacterPipeline LogicTick。
- [x] 11.18 保持 PresentationFrame target 与 logic Session 分离，并读取最近 published samples。

## 12. 迁移 Presentation 与外部副作用

- [x] 12.1 建立 CharacterPresentationProjection 与 Program producer identity 的一对一绑定。
- [x] 12.2 将 AnimationClip、Animancer transition、Camera 和 Cue Unity 引用限制在 Projection/adapter assembly。
- [x] 12.3 建立 presentation command 的稳定 EventId 生成与 source map。
- [x] 12.4 建立 Committer 对 animation selection、sample、complete 和 release 的唯一提交入口。
- [x] 12.5 保持 Animancer 对 state、fade、layer 和 transition library 的执行权威。
- [x] 12.6 保持 Timeline logic time、visual sample time 和 Animancer presentation delta 分层。
- [x] 12.7 保持 published logic body sample 与 visual root 插值分离。
- [x] 12.8 将 Camera、GameplayCue、VFX 和 UI 命令接入明确 Committer port。
- [x] 12.9 Local Driver 对全部新 EventId 生成 Publish，不伪造未实现的 rollback Replace/Retire 策略。
- [x] 12.10 删除从 Tree runtime clone、MotionDebug、Graph traversal 或 scene state 反向推断表现结果的路径。
- [x] 12.11 将 CharacterAnimationPresentationDefinition 编译为唯一 CharacterPresentationProjection layer catalog 与 producer binding。
- [x] 12.12 校验 Program producer manifest 与 Projection binding 的 SemanticHash、identity、source revision、NumericProfile 和 ProgramHash 一致。
- [x] 12.13 将 State/Action ownership 的每层唯一结果编译为 producer command，不再提交 AnimationLayerSelection runtime object。

## 13. 删除旧网络耦合并固定模型插件边界

- [x] 13.1 删除 CharacterNetworkSendStage、CharacterNetworkReceiveStage 和 CharacterNetworkInput 正式路径。
- [x] 13.2 删除 ExternalPoseSample、ExternalPoseCorrection 和 ActionLifecycleInputStage 公共注入路径。
- [x] 13.3 删除 CharacterMotionAuthority LocalSolver/ExternalPose/None enum 与 Host 分支。
- [x] 13.4 删除 MotionStage correction plan、application extent 和 acknowledgement 构造。
- [x] 13.5 删除 CharacterServerAuthoritativeBinding 对旧 CharacterPipeline stage 的 tick hook。
- [x] 13.6 删除 CharacterServerAuthoritativeAdapter 的旧 stage/ExternalPose/correction mapping。
- [x] 13.7 保留 model-neutral SessionHost/ModelDefinition 生命周期，不让其解释 packet、history 或 world solve。
- [x] 13.8 为 ModelDefinition 增加正式 Simulation Driver composition capability 声明。
- [x] 13.9 缺少 Driver/actor binding/solver/endpoint capability 的 ModelDefinition 必须不可选。
- [x] 13.10 将当前 ServerAuthoritative model 标记为缺少新 Driver adapter，而不是回退旧 LocalLoopback 闭环。
- [x] 13.11 从单机 Sandbox 删除 SessionHost、旧 model binding 和 LocalLoopback 装配。
- [x] 13.12 保证 model packet/session/endpoint code 不被 Character Core 引用。
- [x] 13.13 使用搜索确认不存在公共 correction/history/rollback 类型回流 Core。
- [x] 13.14 将 ServerAuthoritative backend capability 改为 Driver、Program、SessionRuntime 与唯一 WorldSolver 的完整组合。
- [x] 13.15 删除 ServerAuthoritative 直接调用旧 Motion Executor 或接受 client resolved displacement 的路径。

## 14. 迁移 Corin Program、Projection 与单机 Sandbox

- [x] 14.1 编译 Corin RootTree 与全部 inline/shared nested Graph。
- [x] 14.2 编译 Locomotion Idle、WalkStart、WalkLoop、WalkEnd、RunStart、RunLoop、RunEnd 和 MovingTurn。
- [x] 14.3 迁移 Shift 单次闪避、闪避结束有输入进 Run 和无输入进 End。
- [x] 14.4 编译外层 None、Attack、DodgeBack、DodgeForward StateMachine。
- [x] 14.5 编译 Attack body 内 Attack1、Attack2 和 Exit nested StateMachine。
- [x] 14.6 迁移 combo request、cancel window、interrupt 和 terminal lifecycle。
- [x] 14.7 编译 Attack/Dodge Timeline、TreeClip Window、motion curve、cue 和 animation producer。
- [x] 14.8 编译 Corin Blackboard declaration、scope、value 和 ConditionRuleGraph reference。
- [x] 14.9 编译 Corin Action、Behavior、GameplayEffect、Tag 和 Attribute catalog。
- [x] 14.10 从唯一 Corin Semantic IR 生成 Float32 CharacterSimulationProgramAsset 与 CharacterPresentationProjection。
- [x] 14.11 将 Sandbox Host 改为显式 Float32 Local Driver、Float32 Unity Solver、Input Adapter 和 Committer 组合。
- [x] 14.12 迁移 Corin 初始 body state 与 Unity Solver actor binding。
- [x] 14.13 删除 Corin 旧 runtime clone/config、authority mode、network stage 和双数据源字段。
- [x] 14.14 删除一次性 asset migrator，只保留最终序列化数据。

## 15. 建立 Editor、Agent 与 Diagnostics 闭环

- [x] 15.1 在 CharacterPipelineDefinition Inspector 显示 source revision、SemanticHash、NumericProfile、compile status、ProgramHash 和 LayoutHash。
- [x] 15.2 在 Inspector 显示 Program gameplay capability 与 required world capability。
- [x] 15.3 在 Inspector 显示 source literal、target lowering、缺失 emitter、断裂 reference、target capability 和 stale artifact 错误。
- [x] 15.4 将 Agent snapshot/validator 改为只读取正式 authoring 与 compile report。
- [x] 15.5 Agent 不生成第二份 Semantic IR、Program schema、operation table 或 Numeric Target 配置。
- [x] 15.6 将 Program operation、state slot 和 source identity 接入现有 Debug Source Map。
- [x] 15.7 将 Evaluate、world batch、Finalize、restore 和 commit lifecycle 接入 structured Trace。
- [x] 15.8 保证成功、失败、restore 与 replay Trace 不经过 Driver OutputPlan 或 Committer disposition。
- [x] 15.9 将 CharacterStateHash、WorldHash validity、Solver capabilities 和 snapshot identity 接入 diagnostics。
- [x] 15.10 RuntimeDebugSession/Live/Capture 保持只读，不暴露 mutable state 或 pending evaluation。
- [x] 15.11 删除 Editor 对 runtime Graph/Node/Timeline clone 和旧 CharacterPipeline stage 私有集合的绑定。
- [x] 15.12 为 Timeline TreeClip Preview 建立隔离 Preview Simulation Session 装配。
- [x] 15.13 Preview target 必须提供匹配 source revision 的 Program、Projection、state、input 与 required WorldSolver capability。
- [x] 15.14 TreeClip Preview 只通过正式 Program operation 与 Session 四阶段执行，不创建 TimelineRunningTree 或 CharacterGraphContext fallback。
- [x] 15.15 纯动画 Preview 只消费 PresentationProjection，不产生 Gameplay 事实或修改 Preview Simulation state。
- [x] 15.16 纯动画 Preview 通过正式 command queue、AnimationPlaybackLifecycle 与 Animancer adapter 采样。
- [x] 15.17 非连续 Preview seek 必须 retire 旧 EventId/generation，不重建隐藏 producer 或直接播放 Clip。

## 16. 清理、文档、编译与严格校验

- [x] 16.1 删除角色主线 old interpreter、runtime clone、runtime authoring reflection 和废弃序列化 type。
- [x] 16.2 删除 runtime compile fallback、stale Program fallback、Transform fallback、默认 Solver、旧公共 SimScalar ABI 和一次性 parser。
- [x] 16.3 使用 rg 确认 Corin runtime 不再调用 RunnableTree/StateMachineGraphRuntime/TimelineRunningTree clone 执行入口。
- [x] 16.4 使用 rg 确认 CharacterController.Move 只存在于正式 Unity WorldSolver adapter。
- [x] 16.5 使用 rg 确认不存在 CharacterMotionAuthority、ExternalPose、Character NetworkSend/ReceiveStage 和 MotionStage correction。
- [x] 16.6 使用 rg 确认 Character Core 不引用 Network Model packet、endpoint、policy、history 或 transport。
- [x] 16.7 使用 rg 确认 Semantic/target portable source set 不引用 UnityEngine、UnityEditor、Animancer、InputSystem 或 Cinemachine。
- [x] 16.8 使用 rg 确认本 change 只安装 Float32 Target，且不存在可选择的空 Fixed/Deterministic target。
- [x] 16.9 使用 rg 确认 Runtime 不解释 Semantic IR、不切换 NumericProfile，也不存在两套手写业务 evaluator。
- [x] 16.10 更新 openspec/project.md 的 Current State、Gameplay Client、BTSMTL runtime、Motion、Presentation、Diagnostics 和 Network Boundary。
- [x] 16.11 删除 project.md 中已归档 change、已删除旧双客户端 change 和 ExternalPose/NetworkStage 旧口径。
- [x] 16.12 更新本 change 影响的 current specs，删除 object interpreter、runtime clone、单角色 MotionStage、CharacterMotionAuthority、公共 correction stage 和公共强制定点旧真相。
- [x] 16.13 使用 `dotnet build --disable-build-servers /nr:false /p:UseSharedCompilation=false` 编译 Semantic IR、Float32 core 与普通 .NET reader 工程。
- [x] 16.14 portable core 编译后立即执行 `dotnet build-server shutdown`。
- [x] 16.15 使用相同参数编译 Unity Runtime、Networking 和 Presentation 相关工程。
- [x] 16.16 Unity Runtime 编译后立即执行 `dotnet build-server shutdown`。
- [x] 16.17 使用相同参数编译 Editor 与 Agent 相关工程。
- [x] 16.18 Editor 编译后立即执行 `dotnet build-server shutdown`。
- [x] 16.19 运行 `openspec validate refactor-character-simulation-core --strict --no-interactive` 并解决全部问题。

## 17. 清除 Program 不变布局的 Tick 热路径重建

- [x] 17.1 建立不可变 ProgramExecutionLayout，并与唯一 CharacterSimulationProgram 实例绑定。
- [x] 17.2 在 ProgramExecutionLayout 一次构建 outgoing control-flow index。
- [x] 17.3 在 ProgramExecutionLayout 一次构建 incoming value-flow index。
- [x] 17.4 在 ProgramExecutionLayout 一次构建 operation reference index。
- [x] 17.5 在 ProgramExecutionLayout 缓存 root operation 与 operation state-slot semantic index。
- [x] 17.6 将 SimulationKernel 的 ProgramHash 到 ProgramExecutionLayout 映射限制在 Session 装配期建立。
- [x] 17.7 将 SimulationOperationMachine 改为消费已构建布局，不再为每 Tick 创建三组 operation List。
- [x] 17.8 将 Edges 与 References 改为读取预分组只读集合，不再使用 Where().ToList()。
- [x] 17.9 保持 ProgramExecutionLayout 不含 Actor、Tick、mutable state 或 Network Model 数据。
- [x] 17.10 确认同一 Program 的多 Actor Evaluate 共享同一只读布局。

## 18. 清除 GameplayEffect 每 Tick 不变重建与无变化写回

- [x] 18.1 将 SimulationGameplayEffectProgram 作为 ProgramExecutionLayout 的只读缓存成员。
- [x] 18.2 SimulationGameplayEffectState 复用缓存 Program，不再每 Tick解析 catalog。
- [x] 18.3 为 Tags、Attributes、ActiveEffects、Periods、Journal 与 ChangeCursor 建立独立 dirty 标记。
- [x] 18.4 所有修改 tag 的路径只标记 Tags dirty。
- [x] 18.5 所有修改 attribute 的路径只标记 Attributes dirty。
- [x] 18.6 所有修改 active effect 与 period 的路径标记对应 dirty 状态。
- [x] 18.7 所有修改 prediction journal 与 cursor 的路径标记对应 dirty 状态。
- [x] 18.8 Save 只编码 dirty 状态，未修改 byte payload 保持原引用和值。
- [x] 18.9 保持 Snapshot Capture/Restore 继续覆盖完整 GameplayEffect 状态。
- [x] 18.10 保持失败 Tick 不发布部分 GameplayEffect 状态。

## 19. 交付 portable DotNet 工程并落实 Numeric Target 物理边界

- [x] 19.1 新增受版本控制的普通 .NET portable reader 工程。
- [x] 19.2 reader 工程直接编译 canonical portable source，不引用 Unity 生成 csproj。
- [x] 19.3 reader 工程不引用 UnityEngine、UnityEditor、Animancer、InputSystem 或 Cinemachine。
- [x] 19.4 将 identity、Semantic IR、operation-set 与 Numeric Target contract 保持在 neutral Core ownership。
- [x] 19.5 将 Float32 Program、State、Codec、Kernel、Session 与 GE backend 明确归入 Float32 source set。
- [x] 19.6 新增 ThirdPersonSimulation.Float32 Unity asmdef，并保持无 UnityEngine 引用。
- [x] 19.7 Unity runtime assembly 显式引用 neutral Core 与 Float32 backend。
- [x] 19.8 Editor compiler assembly 显式引用 neutral Core 与 Float32 backend。
- [x] 19.9 删除把 Float32 specialization 伪装成可多 target 的公共构造入口。
- [x] 19.10 保持本 change 只安装 Float32 target，不新增空 Fixed target。
- [x] 19.11 保持 Session 启动前锁定完整 Program/Kernel/Solver NumericProfile 组合。
- [x] 19.12 确认未来 Fixed target 可以新增完整 source set，而不修改 Float32 backend 业务文件。

## 20. 压缩 Program canonical source map

- [x] 20.1 为 Program codec 增加版本化 canonical string table。
- [x] 20.2 对重复 identity、source type、GUID 与 display-path segment 只编码一次。
- [x] 20.3 SourceMap entry 只保存 string table index 与结构化 path segment index。
- [x] 20.4 reader 在加载时恢复与原 source map 完全相同的只读字符串值。
- [x] 20.5 ProgramHash 继续覆盖压缩后的完整 canonical payload。
- [x] 20.6 删除旧 Program codec 版本读取兼容，不保留双格式 parser。
- [x] 20.7 重新生成 Corin Program 与 Presentation Projection 正式资产。
- [x] 20.8 记录重建前后 canonical bytes 与 Unity YAML asset 大小。

## 21. 拆分 CharacterPipelineHost Preview 职责

- [x] 21.1 将 PreviewSession 从 CharacterPipelineHost 迁入独立文件。
- [x] 21.2 将 PreviewSimulationExecution 从 CharacterPipelineHost 迁入独立文件。
- [x] 21.3 将 PreviewPresentationOutputPort 从 CharacterPipelineHost 迁入独立文件。
- [x] 21.4 将 PreviewPlaybackEngine 与 ActivePreviewProducer 从 CharacterPipelineHost 迁入独立文件。
- [x] 21.5 CharacterPipelineHost 只保留 runtime composition 与 preview controller 协调入口。
- [x] 21.6 Preview 继续复用正式 Session、Projection、AnimationPlaybackLifecycle 和 WorldSolver 合同。

## 22. 重新核对完成状态

- [x] 22.1 使用 rg 确认 Tick 路径不再构建 Program edge/reference index。
- [x] 22.2 使用 rg 确认 Tick 路径不再通过 LINQ 复制 edge/reference 分组。
- [x] 22.3 使用 rg 确认 GameplayEffect 未变化时不重新编码五份 byte state。
- [x] 22.4 使用 rg 确认 portable reader csproj 已受版本控制且无 Unity 引用。
- [x] 22.5 使用 rg 确认 neutral Core 与 Float32 backend ownership 和依赖方向唯一。
- [x] 22.6 使用规定参数编译 portable reader，并立即关闭 build server。
- [x] 22.7 使用规定参数编译 Unity Runtime，并立即关闭 build server。
- [x] 22.8 使用规定参数编译 Editor 与 Agent，并立即关闭 build server。
- [x] 22.9 运行 `openspec validate refactor-character-simulation-core --strict --no-interactive`。
- [x] 22.10 核对 tasks.md 每个勾选都与仓库事实一致。

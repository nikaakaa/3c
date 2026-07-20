# Gameplay Runtime 与 Tooling 当前实施清单

本文记录本 change 当前已经落地的唯一 owner、输入、处理、输出和已删除路径，并明确列出尚未闭环的工作。它不提供旧产物兼容规则；Program、manifest 或 editor identity 不匹配时必须重新生成或明确失败。

## Program 与 Numeric Target

### 共同输入

- 唯一业务源：BTSMTL Graph、StateMachine、Timeline、TreeClip、Blackboard、Action、GameplayEffect、Motion 与 Camera authoring。
- 唯一语义产物：numeric-neutral `.csir`。
- 唯一 operation-set owner：`SimulationProgramSemantics.cs` 的 `CharacterGameplayOperationSet.Version`，当前工作树为 `character-gameplay-operations/6`。
- Frontend、Float32 lowerer 与 Fixed compiler都必须接受同一完整operation-set，不允许Target跳过operation或选择旧schema。

### Target 输出

- Float32：`float32-ieee754`、Target ABI 2、独立Program/State codec、ProgramHash与LayoutHash。
- Fixed：`fixed-q32.32`、Target ABI 1、独立Program/State codec、ProgramHash与LayoutHash。
- 两个Target共享业务控制，不共享二进制ABI、Snapshot或Hash；Session启动后不能切换Target。
- Corin正式产物由`CharacterSimulationProgramBuildService`和`FixedCharacterSimulationTargetCompiler`生成，Unity asset只包装exact canonical bytes，不手写或兼容旧版本。

## Portable Runtime 控制

### Timeline

- owner：`TimelineControlRuntime<TOperationTarget,TTime>`。
- 输入：Program operation、typed Timeline state port、sample range、cycle与control-flow edge。
- 处理：segment/cycle、loop、TreeClip enter/hold/exit、window、cue与terminal顺序。
- 输出：portable transition/output命令，由Float32/Fixed `TimelineTarget`转换为typed state、motion、fact与presentation payload。
- 已删除：Float32/Fixed各自推进segment、cycle、TreeClip和cue生命周期的第二实现。

### GameplayEffect

- application准入owner：`GameplayEffectApplicationAdmissionRuntime<TApplication,TSpec,TScalar>`。
- 生命周期owner：`GameplayEffectControlRuntime<TTarget,...>`。
- 输入：typed application、definition、source/target identity、SetByCaller、attribute snapshot、tag与prediction identity。
- 处理：准入、stack、period、expire、remove、prediction confirm/reject、change顺序。
- 输出：portable runtime change；Target leaf只负责数值magnitude、seconds-to-ticks、typed attribute/state和输出码映射。
- 已删除：Float32/Fixed重复的准入分支、descriptor builder、lifecycle/stack/period/prediction控制与Target专用失败策略。

### Pipeline Transaction

- owner：`PipelineTransactionCoordinator<TPort,...>`。
- 输入：Ingress、唯一Schedule产生的ExecutionPlan、可选restore、Target transaction port。
- 处理：`Ingress -> Schedule -> Restore -> N x compiled Step Passes -> Egress -> Publish -> Commit`；每个 Step 中唯一 `Evaluate -> ResolveBatch -> Finalize` 是有序核心锚点，Rollback History 等附加 Pass 依 descriptor 顺序和 Product 依赖在 completed step 冻结前执行。
- 输出：原子提交后的Character/World/Pipeline state、Snapshot、SourceEgress与外部output disposition。
- 已删除：Float32/Fixed两份outer transaction编排、第二publish/commit入口和重复rollback顺序。

## Execution Services 与 Workspace

- Program owner：Float32/Fixed各自的`ProgramExecutionLayout`与immutable execution services；同Program的Actor复用topology、SourceMap、Timeline lookup、GE descriptor/index和state policy。
- Session owner：Session execution workspace复用roster、completed actor、egress和state staging容量。
- Actor owner：Actor execution workspace复用facts、trace、Timeline segment、GE、value与motion scratch。
- Tick输入adapter已经复用input value/request集合，并减少一部分中间物化；热路径中的`AsReadOnly`、`ToArray`与LINQ清理尚未完整完成，对应`tasks.md` 6.14仍保持未勾选。
- Program Evaluate复用固定容量PendingEvaluation batch，不再每Step创建`List + ReadOnlyCollection`。
- Snapshot、history、published output、SourceEgress和diagnostics不得引用下一Tick会reset的workspace；完整的跨事务冻结/复制审计尚未完成，对应`tasks.md` 6.15仍保持未勾选。

## Host Product 与 Network Test Product

### Host Profile

- neutral Core只拥有通用Host product token、Program/Pipeline/ABI与Solver capability合同。
- Unity Authority Product拥有Unity worker Host identity、Unity Solver声明、launch lowering与server manifest字段。
- DotRecast Authority Product拥有普通.NET scene Host identity、DotRecast Solver声明、launch lowering与server manifest字段。
- 已删除：Core中的`UnityAuthorityWorker`、`DotRecastAuthorityScene`枚举/factory、旧Profile reader与旧manifest映射。

### Build Workflow

- 公共owner：`NetworkTestProductBuildWorkflow`。
- 通用adapter工具：`NetworkTestProductAdapterUtility`。
- 服务端manifest/hash owner：`ServerProductBuildManifestUtility`。
- 外部进程owner：`NetworkTestExternalProcessExecutor`，所有dotnet调用固定带`--disable-build-servers /nr:false /p:UseSharedCompilation=false`并立即shutdown。
- 产品adapter：Unity Authority、DotRecast Authority、Deterministic Rollback；三者只依赖公共合同，不互相调用。
- 输出：`Build/Network/UnityAuthority`、`Build/Network/DotRecastAuthority`、`Build/Network/DeterministicRollback`，同产品原子覆盖，不同产品互不覆盖。
- 已删除：三个工具各自的process、directory replace、manifest、exact-file和隐式Build/Run helper。

## Fantasy Unity Endpoint

- `ServerAuthoritativeConnectionCoordinator`唯一拥有endpoint状态、failure与dispose门闩。
- `ControlSessionModule`负责connect/register/join/roster/heartbeat/leave。
- `DatagramChannelModule`负责ticket、handshake、command与snapshot datagram。
- `CheckpointReconstructionModule`负责baseline、delta与full checkpoint。
- `PredictionEvidenceModule`负责ack、observation、liveness与metrics。
- `ServerAuthoritativeFantasyEndpointRuntime`只编排typed module result并把失败提交给coordinator；module自己的`Dispose`只释放私有资源，不拥有endpoint terminal state。
- 已删除：endpoint大类中的重复字段、第二failure/status owner和第二transport路径。

## Remote Presentation

- canonical remote Body tick选择owner：ServerAuthoritative Prediction Schedule/History。
- egress：成功Current step唯一写出`SelectedRemoteBodyBatch`。
- presentation owner：通用`CharacterBodyPresentationRuntime`；`CharacterRemotePresentationProfile`只提供角色表现参数。
- 输入：Model Egress已经选择的相邻Body区间、stream reset与PresentationFrame delta。
- 处理：区间插值、target replacement后的有界收敛和HardRecovery reset；Network adapter不再持有第二份visual pose/filter。
- 输出：供visual root、Animation与可选Camera共同消费的`CharacterBodyPresentationFrame`及结构化diagnostics。
- 已删除：`ServerAuthoritativeRemoteVisualPoseFilter`、直接snap、第二`MoveTowards`裁剪、raw authority重选、独立delay cursor和visual-to-WorldSolver writeback。

## Camera 编译闭环

- authoring：`RequestCameraStateNode`、`EmitCameraCueNode`、`SetCameraResponseNode`、`SetCameraTargetNode`。
- compiler：四个唯一node emitter生成versioned operation与stable Source Map。
- Target：Float32/Fixed operation evaluator输出同语义强类型`PresentationCommand`。
- runtime node：不直接控制Camera；在对象解释路径中Action返回Failure、Value保持invalid，不再抛throw-only异常。
- 已删除：未注册Camera helper、旧operation schema和runtime throw-only路径。

## Editor 内部模块

### Timeline

- `TimelineFrameGeometry`：frame/time、clip rect、overlap与hit-test。
- `TimelineInteractionState`：只读selection、pan、move、resize、ease与Undo transaction；只依赖`ITimelineInteractionHost`窄回调。
- `TimelineRendering`：显式消费frame range、viewport height、playhead、track/clip与runtime overlay输入，不读取`TimelineFieldView`内部状态。
- `TimelineFieldView`：UI控件、事件绑定、Inspector与模块适配。
- 已删除：主View中的geometry/render helper、可变selection暴露和renderer/interaction对整个View的反向引用。

### Tree

- graph mutation、selection inspector、data catalog、navigation与runtime overlay均有独立internal owner。
- `BaseTreeView`与Window只承担visual surface、selection forwarding和模块调用。
- asset identity、property path、Undo/Redo、domain reload locator与Graph/Timeline双窗口binding保持不变。

## 唯一总链

```text
Authoring
-> Semantic IR
-> Float32 / Fixed Program Target
-> ProgramExecutionServices + Session/Actor Workspace
-> Session Composition
-> Runtime Launcher
-> Pipeline Transaction Coordinator
-> Target Kernel
-> WorldSolver
-> Snapshot / Replication
-> Character Body Presentation Runtime
-> Animation / Camera Presenter
```

任何新Numeric Target、Network Model、Authority backend或Editor surface都必须接入上述正式合同；不得恢复旧parser、旧runtime、兼容映射、fallback配置、双写入口或运行时model switching。当前仍需完成`tasks.md` 6.14、6.15和最终真实性核对，完成前本清单不得称为最终闭环。

# Pose域第3组收口运行记录（2026-09-17）

## 四项任务状态

### 3.9 完成
- PoseStateMachine真实接线路径：ValueHandlerRegistration.Register → registry.Register(profile) → CharacterPoseNativeStateMachineRegistration.Register，带preparedBinding与嵌套GraphRuntime驱动。
- 前期"RegisterStateMachine无调用方"结论为浅查误报：managed注册中的孤儿扩展从未被调用且缺preparedBinding签名，已作为死代码删除（aa0a7a073）。

### 3.16 完成
- 深查确认链路闭合：CharacterPresentationDomainRuntime.RunPoseFrame每帧EventGraph.Update → CharacterAnimationPoseInputFrame.FromPublishedVariables（缺失变量/重复声明精确失败）→ CharacterPoseNativeFrameInput.ParameterFrame → ProgramParameterInput handler ParameterFrame.RequireValue读取。前期"事件变量帧无handler消费者"结论为误报。

### 3.10 未实现（本轮）
- 五个managed源接口（LinkedPose/MotionMatching/HistoryCollector/EntryPose/RootOrientation）无实现类，composition传Throw精确失败。
- 已完成的实施测绘：
  - HistoryCollector：CharacterPoseHistoryCollectorRuntime（MotionMatching/Runtime）已有BeginFrame/PrepareCommit/CommitFrame/DiscardFrame，与接口近一一对应；适配器需从FrameInput/lineage构建CharacterMotionMatchingFrameContext（FrameIdentity/RigLineage/ResetSequence/Facts.PresentationTime），CommitFrame需从Pose read binding提取rootPose、featureBone局部位置与FootPlacement样本。
  - LinkedPose：与StateMachineSource同构的嵌套图驱动，catalog.RequireLinkedPose(groupId)已备；参照CharacterPoseNativeStateMachineSource（约千行）。
  - MotionMatching：catalog.CreateMotionMatchingSource/CreateMotionMatchingSelection已备，需适配MotionMatchingPoseSourceRuntime到source接口。
  - RootOrientation：catalog.RequireRootOrientationCurve已备；source需yaw曲线播放语义（IsRelevant/SourceId/SampleTime/Duration），需结合消费场景定相关性行为。
  - EntryPose：Payload层无EntryPoseInput payload类，节点种类EntryPoseInput=35已声明但无可authoring内容，实现前需先定authoring合同。

### 3.11 未实现（本轮）
- 断点：BlendStack/Slot的Timeline类entry需要AnimationResolvedPoseSourceSample，全仓只有Provider（MotionMatching）与SelectedPosePlayer两个生产点，Timeline侧无桥。
- Timeline侧已有：ActionCommittedSampleHistory（按playback存committed raw sample：visualTime/continuousTime/cycle/loop/timeScale/weight，但不含clip集）、CharacterTimelineHost的CharacterTimelinePlaybackObservation（ClipTime/Weight/ClipAuthoringId）。
- 需补：per-frame步骤把committed sample与该源clip集组装成AnimationPoseSampleRequest，经SourceModule.PrepareFrameResult登记帧结果，actionSampleProvider改读帧结果。clip集与源id的映射合同需与Timeline owner确认。

## GraphInvalid疑点排查结论（非四项范围，资产缺口）

- 现象：Pose Native Domain creation failed: GraphInvalid。
- 根因（源码+资产双证）：CharacterPoseNativeGraphValidator第359-365行规定输出路径GoalAssembler与FullBodyIK必须成对（solvers.Count==1 && assemblers.Count==0 → GraphInvalid "found 0 and 1"）。Corin图LocomotionFullBodyPoseGraph.asset实测：CharacterFullBodyIkPosePayload×1，CharacterFullBodyIkGoalAssemblerPayload×0。
- 修复路径：作者在图编辑器为FullBodyIK补GoalAssembler节点并连线（或授权按authoring API修改资产，改动清单：新增1个GoalAssembler节点+2条连接）。修复前Play无法进入Pose域创建。
- 该结论与3.10/3.11无因果，四项做完也不会消除此失败。

## 编译证据

- dotnet build ThirdPersonClient.Runtime.csproj --disable-build-servers /nr:false /p:UseSharedCompilation=false：0错误；结束已执行dotnet build-server shutdown。

## GoalAssembler修复执行记录（2026-09-17，阻塞中）

- 已按agent工厂链路执行：btsmtl_export_code成功导出（Definition新路径Assets/Configs/Character/Corin/Pipeline/Definition/CorinCharacterPipelineDefinition.asset，旧路径被并行Corin资产重组移动）。
- Root.cs已补goal-assembler节点（corin.control-rig.body.graph/goal-assembler）、goals→full-body-ik.goals连线与布局项，提交c76ff5c3f。
- 阻塞：generate_assets前置的ThirdPersonClient.Editor编译被并行Timeline窗口的在途错误阻断（CharacterTimelineHost.cs 659-663行，非本域文件）。按skill口径记录阻塞、不修改他人文件、不轮询。
- 恢复条件：并行窗口编译清零后重跑dotnet build ThirdPersonClient.Editor.csproj，0错误后执行btsmtl.generate_assets（同export参数），资产生效后GraphInvalid应消除；Play验证归用户。

## GoalAssembler修复完成（2026-09-18）

- generate_assets成功替换LocomotionFullBodyPoseGraph.asset；重新export_code验证资产内容含goal-assembler节点、FullBodyIkGoalAssemblerPayload与assembler-to-ik连线（goals→full-body-ik.goals），validator成对规则静态满足。
- 过程中发现并绕过导出器缺陷：BtsmtlAuthoringCodeGenerationService的外部资产分类把Definition/Presentation Profile发为BuildRoot局部var，而FinalizeAuthoring经rootParts.asset/asset1消费，导致generate报"requires a Definition and Presentation Profile"。当前以生成源码内补parts.asset赋值绕过（再导出会被冲掉，需重打补丁）；分类漏FinalStatements使用的根因修复归authoring工具owner。
- 静态校验：Editor编译0错误；validator成对规则以资产内容+源码规则核对通过。Play端到端验证归用户。


## GraphInvalid第二根因修复与诊断透传（2026-09-18）

- 现象复核：9981cee4c（GoalAssembler资产修复）后受控Play仍报"Pose Native Domain creation failed: GraphInvalid"且Message为空。
- 根因（源码证据）：CharacterPoseNativeDomainRuntimeFactory.Validate成功路径误用Failed工厂构造"通过"标记（Failed(None,"","")），而Failed工厂带None→GraphInvalid归一化，IsAdopted判定为Session!=null在预检阶段恒false；Create/Replace两处gate因此永远把"通过"当失败返回（GraphInvalid+空消息）。自bd2562486接入正式装配入口起，域创建从未通过预检，真实图校验器从未运行——此前各次Play的GraphInvalid均为此gate产物，GoalAssembler资产修复本身正确但被该gate掩盖。
- 修复：CharacterPoseNativeDomainCreateResult新增Valid()工厂与IsFailure判定；Validate成功返回Valid()；Create/Replace gate改判IsFailure。提交c6f2e6180。
- 诊断透传补全（审计发现的真实缺口）：AdoptedResult原无Source属性，validator的Origin在RoleEntry边界丢失；validator外层catch与工厂catch重包装异常只拷Message丢类型名。修复：AdoptedResult新增Source（preparation重载内部取preparation.Source，请求重载显式传Pose/Create与Pose/Replace）；DomainCreateResult.Failed(adopted)改用adoption.Source；重包装消息统一为"类型名: 消息"；创建失败日志追加Source字段。提交960d117f7（内容与工作区改动一致，由并行窗口代为入库）。
- 编译证据：ThirdPersonClient.Runtime.csproj与ThirdPersonClient.Editor.csproj均0错误0警告（--disable-build-servers /nr:false /p:UseSharedCompilation=false），结束后已执行dotnet build-server shutdown。
- 后续：gate修复后validator首次真实运行，Play端到端验证归用户；若仍有结构失败，日志将携带来源与异常类型名可直接定位。3.11的clip集与源id映射合同仍待Timeline owner确认。


## ResourceSet编译工具链重建（2026-09-18）

- 运行日志定位：gate修复后暴露下一层真实失败 Compiled Presentation Clip source 'Moving Turn' is invalid（CharacterPresentationClipSourcePlan.RequireValid）。
- 静态审计结论：CorinPoseNativeDomainResourceSet.asset共28条source plans，0条含m_FootStepObservation（v2合同硬性要求），全部指向旧clip guid；并行窗口已把Profile七条locomotion源绑定换绑ZZZ新clip。CorinFootPlacementAnalysisSource.asset同样只覆盖旧clip（4个引用），无新clip的Motion Reference绑定。
- 工具链缺口（源码证据）：全仓无任何m_SourcePlans写入方；CharacterPresentationFootEventCompiler.CompileFootStepObservation零调用方；旧总编译器CharacterPresentationPoseSourcePlanCompiler（412行）在15d742722被删，删除记录明确"不新增替代编译器"。ResourceSet资产（82a25af8d）为一次性灌入，从未有正式重建入口。
- 重建（本次）：ResourceSet新增internal ReplaceSourcePlans变更合同（空表/空条目/索引重复精确失败，RequireValid逐条校验）；新增编辑器CharacterPoseNativeDomainResourceSetCompiler，链路=Profile源目录（CharacterPresentationPoseSourceCompiler.Compile）→逐clip ResolveIdentity+ValidateFootMotionGroupRequired+FootPlacementWeight曲线→AnimationFootAnalysisArtifactBuilder.Build正式分析产物→CompileFootStepObservation→低层plan构造器，NativeClip后端scalarPage=null合法；菜单3C/Character/Animation/Compile Pose Domain Resource Set，操作选中的Presentation Profile。BlendSpace等其他绑定类型显式报不支持，不做容忍。
- 编译证据：ThirdPersonClient.Editor.csproj 0错误（92条警告均为并行窗口ACL文件预存CS0649）；强制参数+build-server shutdown已执行。
- 待办（运行该菜单的前置）：CorinFootPlacementAnalysisSource需为新ZZZ clip补Motion Reference绑定（authoring决策：每个target clip对应哪条motion reference，归Corin资产owner），随后artifact首次Build会自动生成到Library/CharacterFootAnalysis；Timeline编译错误清零后一起验证。


## PoseGraph runtime生命周期归属与repeat replay语义收口（2026-09-19）

- 生命周期归属（审计后固定）：CharacterPoseNativeDomainInstance（包装器）唯一持有DomainSession；CharacterPresentationDomainRuntime唯一持有包装器并在Dispose销毁；DomainRuntimeFactory的catch为幂等二次销毁；FrameCoordinator→RoleRuntime→GraphRuntime单向持有；子图child由SubgraphHandler持有并随evaluator级联销毁；每个runtime通过NodeCanvas Graph.Clone持有独立graph实例，跨host无共享销毁面。
- Replace链删除：DomainRuntimeFactory/RoleEntry/RoleRuntime/GraphRuntime四级Replace无任何调用方，且语义破损（销毁旧session但新session无法回绑进仍存活的包装器，一旦被调用必然产生"包装器存活+底层图已销毁"的ObjectDisposedException错配）。按激进清理整体删除，repeat replay不引入Replace语义。
- Repeat replay正式语义：同一Pose实例跨轮次复用，轮次间经presentation.Reset→ResetPose→ResetInstance按递增resetGeneration重置状态；重置失败（Disposed/Stale/内部异常）由吞结果改为精确抛InvalidOperationException，失败后不再推进带脏状态的帧。
- 实例身份：presentation装配原对每个host硬编码instanceId=1（子图id=parent*4099+seq也随之跨host相同），fixed-player与fixed-target存在实例身份冲突；改为StableHash.Compute(actorId.Value)派生，跨host唯一、跨轮次稳定、非零校验。
- 双FixedCharacterHost隔离：各自Registration独立持有表现运行时，SessionHost按ActorId去重注册，注册销毁链各自独立，无互相接管路径。
- 编译验证：Runtime全量编译被Timeline窗口在途TimelineData.cs（52个语法错误，非本域文件）阻塞；本域6个改动文件无任何错误输出。同一trace两轮ReplayProof验证待Timeline清零后由Play侧执行。

- 子图实例身份溢出修复（04d587e16）：父实例身份改为64位Actor哈希后，原checked(parent*4099+seq)必然Overflow；子实例身份改为StableHash(父实例身份,子图节点Id,序列)取前16位hex，天然防溢出、跨Host唯一、跨轮次稳定，零值与父身份碰撞显式抛错。

- ODE二次复盘（Play复现）：Editor.log显示自动退出由PlayModeErrorAutoExit对首错的响应触发；最终会话进入Play后无主异常、首帧BeginFrame即ObjectDisposed——静态三轮审计（包装器/表现/注册/静态tick驻留/回滚宿主/子图child全链）确认主线所有权闭合下该状态不可达，判定存在审计链外销毁者；GraphRuntime.Dispose入口已加正式销毁溯源日志（实例/图/启动状态+调用栈），下次运行按日志指认越权销毁者后收口修复。

- ODE根因定案与修复（2866df5d6）：销毁溯源日志指认越权销毁者为PoseCanvasGraphEditorEntryPoint.Restore的DestroyTransientPoseGraphs——Editor delay call把Play中运行时正挂接的正式瞬态图当编辑态残留DestroyImmediate，经OnGraphObjectDestroy反调Dispose。修复：清理增加NativeRuntime挂接判据，仅清理无挂接残留；溯源诊断完成使命移除。spec同步新增"编辑器残留清理MUST NOT销毁运行中挂接的Pose瞬态图"条款。
## 图启动初始化恢复、校验边界显式化与失败归一化修复（2026-09-19）

- 图启动初始化恢复（7eb4133e7，另一窗口代为落地）：前轮激进清理误删AttachAndStart/InitializeGraph调用链，恢复后图可正常启动挂接；Actor稳定哈希→instanceId转换同步修正（actorId.Value为hex字符串，取前16位Convert.ToUInt64(hex.Substring(0,16),16)），与子图StableHash派生口径一致。
- 校验边界显式化（33d3c144b）：隐式边界推断删除，BoundaryKind收敛为合同层CharacterPoseNativeGraphBoundary枚举，PrepareRequest与PreparedBinding全程携带；根图/子图校验按实例边界判定，不再依赖位置猜测。
- spec口径入档（38d9da97e）：character-pose-graph-runtime-architecture新增单owner不可运行期替换、repeat replay同实例按递增resetGeneration重置且失败精确抛错、Actor派生实例身份跨Host唯一三条合同。
- 失败归一化二次异常修复（6a90d57d3）：PrepareFrame catch把原始异常转Failed结果时，若exception.Message为空，CharacterPoseNativePreparationResult构造器因source/message非空白合同自身抛ArgumentException吞掉真正根因（Replay推进报"Pose native preparation result is invalid"即此形态）。修复：包装消息统一为"类型名: 消息"保证非空；CompletedLineage无效时带InnerException抛InvalidOperationException显式上抛，不再构造非法结果。
- 编译证据：ThirdPersonClient.Runtime.csproj与ThirdPersonClient.Editor.csproj均0错误（--disable-build-servers /nr:false /p:UseSharedCompilation=false），结束后已执行dotnet build-server shutdown。固定输入Replay录制重跑与端到端验证归用户Play侧。

## 源模块帧生命周期接回（2026-09-19）

- 现象：StateMachine子图溢出与帧谱合同修复后，Replay推进报 Native Clip source 'corin.locomotion.idle.sequence' has no active source frame（ClipSourceModuleBinding.Prepare ← ClipPlayerHandler.PrepareEvaluation ← StateMachineSource.PrepareEvaluation）。
- 根因：CharacterPoseSourceModule.BeginFrame全仓零调用方。旧Program/Projection执行壳（9ee3b6eed删除的797行CharacterPoseFrameCoordinator）是唯一驱动方，删除时模块帧生命周期未接回新管线；CurrentLease恒为默认值，所有Clip/BlendSpace/Slot绑定的Prepare必然失败。这不是路由或子图建帧问题——子图与根图共享同一域级模块，模块帧从没开过。
- 接线（8d824d0bf）：RoleRuntime持有模块生命周期——根图BeginFrame成功后按开帧谱（completion==0）打开模块帧，失败回滚丢弃图帧再上抛；Commit成功后模块收帧；Discard/Stop时模块同步丢弃。子图经StateMachineSource共享根帧的模块租约。
- CommitFrame合同对齐（8d824d0bf）：旧合同要求demand/result页完成装配才许收帧，但装配链生产者（BindDemand/PrepareFrameResult）随执行壳删除后零调用，新管线永远无法满足。收帧改为开帧校验+后端CommitFrame+物理源CommitFrame+页面清理；已提交源不重连（PrepareNativeClipPlayer按ContainsCommitted跳过catalog重建），每帧仅pending登记走正常提交。
- 死面登记：模块BindDemand/RequireDemand/PrepareFrameResult/RequirePendingReady/SealReadiness/HasPreparedSource/RequirePreparedResources/EnterEvaluateBarrier/CaptureUsage/ClearUsage/RequireTuning及FramePage的demand/result半边在新管线零调用方，属执行壳残留，待专门清理pass统一删除。
- 编译证据：Runtime 0错误（强制参数+build-server shutdown）。Play侧Replay重跑归用户。

## RunPoseFrame评估状态门补齐（2026-09-19）

- 现象：源模块生命周期接回后Replay推进到ValidatePending报 Pose native pending validation input is invalid（GraphRuntime.ValidatePending:693），PlayModeErrorAutoExit杀Play，Trace无法保存。
- 定案：693门三条件（result无效/lineage不等/status非Evaluated）中唯一可达是第三个——根图Evaluate内部异常被catch转Faulted结果返回，而RunPoseFrame只gate了preparation没gate evaluation，Faulted结果直冲ValidatePending触发合同异常。lineage由同帧m_CompletedLineage构造不可能不等，Evaluated结果IsValid恒真。
- 修复（3e99c84c1）：RunPoseFrame评估后状态门——非Evaluated时Warning日志携带evaluation.Source与Message（把被吞的底层真因暴露到下一轮Play日志），按失败码Discard会话并丢弃表现帧，录制不再被合同异常打断。
- 待办：下一轮Play按Warning日志指认的底层Evaluate失败收口（静态候选：native playable Job完成时机与EnterEvaluateBarrier零调用——同步评估屏障属旧执行壳流程，新管线未接；ClipPlayerHandler.CompleteFrame的CompletedAt检查可能每帧错帧）。
- 编译证据：Runtime 0错误（强制参数+shutdown）。Play验证归用户。

## Replay收口当前卡点与诊断增强（2026-09-19）

- 运行日志真因（评估门Warning暴露）：state corin.locomotion.idle did not produce the current Local Pose——状态机子图Evaluate的Local Pose校验失败，每帧Faulted经阶段门降级Discard，Play不再崩、Trace可保存，但姿态内容为空，Replay尚无法matched。
- 生成→编译→装配→帧推进链路已通：域创建、图启动、源模块开帧、绑定Prepare、根图与子图Evaluate均进入；唯一断点在子图Local Pose产出校验，五条件（结果无效/status非Evaluated/输出非LocalPose/Native无效/completion不等）静态无法区分。
- 诊断增强（38c2a27c3）：StateMachineSource.Evaluate失败异常携带详情——有效结果输出status/输出端口类型/输出completion与期望completion对照，无效结果输出子图Source与Message；下一轮Play按详情直接定位（首要嫌疑：native Job完成时机逐帧错位，同步评估屏障在新管线无调用方）。
- 输入/输出/Timeline交接合同已入运行时架构spec（8090f9076）：帧输入显式字段、事件图typed变量帧只读消费、输出带CompletionIdentity的typed端口值与四类阶段结果、Timeline唯一交接点为已提交Action command集与Fact帧事实。

## 子图Local Pose诊断修正与参数边界确认（2026-09-19）

- 详情日志到手：status=Faulted, output=null——子图Evaluate内部异常转Faulted，不是completion错位。但诊断分支有缺陷：评估合同允许Faulted为有效结果（FailureCode非零即通过IsValid），走了对照分支没输出Message。修正（7f73670d8）：按Status==Evaluated分流，非Evaluated输出status/failureCode/Source/Message真因。下一轮Play即得子图内部真实异常。
- 参数边界确认（已实现已提交）：Control参数由InputContract构造校验强制只经事件图typed变量帧（未发布/类型不符精确失败）；AnimatedProperty（blendshape与FootPlacementWeight）只由CharacterFinalPosePropertyWriter消费，BlendSpace等读取侧有用途检查守卫。合同入运行时架构spec（57a76e426），含阶段门Warning收口期责任条款。

## 参数单路消费强制点核实（2026-09-19）

- 图内参数节点（CharacterPoseNativeGraphEvaluator参数node.Initialize）：声明必须Usage==Control且IsBlackboardInput，否则抛"does not reference a read-only EventGraph Control parameter"——图内不存在AnimatedProperty参数节点。
- 表现侧变量帧（CharacterPresentationDomainRuntime构造）：m_PoseParameterIds只收集Control参数，CharacterAnimationPoseInputFrame.FromPublishedVariables消费面天然不含AnimatedProperty。
- 唯一属性写入（CharacterFinalPosePropertyWriter:103）：只接受AnimatedProperty参数，Final Publication属性页单路写入。
- 混合节点守卫（AnimationBlendSpacePlayerRuntime:610）：AnimatedProperty参数在变量读取路径被用途检查拒绝。
- 合同构造（CharacterAnimationInputContract:252/403/412）：Control必须事件图发布且类型一致；AnimatedProperty仅由blendshape绑定与FootPlacementWeight声明产生。
- 结论：Control→事件图变量帧、AnimatedProperty→Final Publication Property Writer两条链各自单路，无交叉查询点、无伪装填充路径；合同文本见character-pose-graph-runtime-architecture spec（57a76e426）。
- 剩余问题不变：①子图Local Pose真因待下一轮Play日志（诊断分支已修正，7f73670d8）；②3.19源模块死面清理；③评估阶段Job完成时机按日志收口。

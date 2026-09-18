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
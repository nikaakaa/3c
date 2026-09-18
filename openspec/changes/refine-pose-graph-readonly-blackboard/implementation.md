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
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
# 动画表现帧事务实现清单

## 正式调用顺序

`CharacterSimulationPresentationRuntime.Present`的动画相关顺序固定为：

1. Equipment外观提交。
2. Body committed/selected区间采样与VisualRoot更新。
3. Presentation Fact与Program Parameter投影。
4. `CharacterAnimationPresentationRuntime.Present`建立唯一外层帧事务。
5. Action inbox、lifecycle、sample、Marker、Slot、PoseState与可选Motion Matching写入Pending状态。
6. `PosePlanExecutionRuntime.PrepareEvaluation`准备source、Player、BlendStack、Native Pose、Job binding、Final Writer binding和release token。
7. `ValidatePendingSeal`完成identity、容量、ownership、release依赖与固定应用顺序验证。
8. 外层事务进入`EvaluateBarrier`，执行唯一Animancer Graph Evaluate、ordered staged Pose Plan、world-aware Foot Placement和Final Physical Writer。
9. `CommitFrameTransaction`按Workspace、Action Sampling、Slot、Action Lifecycle、Motion Matching、Pose Plan顺序交换Committed/Pending页并提交journal。
10. `FinalizeCommittedFrame`应用预验证source release token并交换Final Pose页。
11. 应用预验证Action backend acknowledgement，执行deferred disconnect/release/destroy。
12. 有diagnostics interest时只从Committed页发布snapshot；无interest时只增加skip计数。
13. Camera消费同帧Body结果。

## Physical Bone写入边界

- Animancer source graph在Evaluate内生成source stream；`AnimationSourcePoseCaptureJob`只读取Physical `TransformStreamHandle`并写Native Pending source page，同时派生Virtual Bone。
- Pure Pose与world-aware stage只写Native Pending Pose workspace，不写场景Transform。
- `AnimationFinalPosePhysicalWriter`是唯一Physical Bone写入点。它先完整检查全部Physical Transform、Pending/Committed local pose、Pose availability、continuity、graph completion与frame completion，随后才按`PhysicalBoneCount`写`localPosition/localRotation/localScale`。
- `CharacterSimulationPresentationRuntime.CommitFinalPose`只提交只读frame与诊断阶段结果，不执行第二次骨骼或AnimationRoot纠正写入。
- Body VisualRoot和Equipment socket Transform属于各自表现owner，不是动画骨骼回滚或Final Pose第二写入链。

## 删除项审计

- 正式Animation与Presentation Runtime中不存在`CaptureState`、`RestoreState`、`CaptureFrameState`、`RestoreFrameState`、`CapturePhysicalTransformState`或`RestorePhysicalTransformState`调用。
- 不存在`PosePlanExecutionRuntime.FrameState`、递归State DTO、Physical Bone local pose before-image或`HeldCommittedPose`继续运行分支。
- Native Pose、Inertialization与Final Pose不通过`Clone`、`ToArray`或NativeArray到托管数组复制建立帧回滚点。
- Runtime创建阶段仍会按Projection建立固定数组、NativeArray、Dictionary索引和只读catalog；这些不是PresentationFrame热路径分配。
- `PoseStateAndSourceRuntime.BuildMotionMatchingRelevance`的`List/ToArray`只在Runtime构造时编译固定布局；Motion Matching replay/codec中的Clone与数组构造只属于fixture、artifact或离线诊断边界。
- 正常帧复用Action/Provider Dictionary、diagnostics List和固定buffer；调用前按Projection容量检查，不在帧内构造容器或扩容。

## 可变状态分类

| Owner | 策略 | 固定容量来源 |
|---|---|---|
| Action inbox cursor | pending scalar | Action playback layout |
| Action lifecycle registry | 固定entry页与mutation journal | finite Action/Slot source峰值 |
| Action committed sample、projected time、Marker relation/cursor | 固定entry页、pending scalar与journal | playback/marker relation布局 |
| PoseStateMachine | Committed/Pending scalar页 | compiled StateMachine数 |
| Sequence、BlendSpace、Selected Player | Committed/Pending clock、selection、usage页与release journal | compiled Player/source容量 |
| AnimationSlot | Committed/Pending state页与dirty journal | compiled Slot数及BlendStack source容量 |
| Transition Routing | Committed/Pending控制页与固定event页 | compiled route/event容量 |
| BlendStack | Committed/Pending entry metadata、dense weight页与固定dirty/release journal | `BlendStackWorkspace.Capacity`与Rig PoseBoneCount |
| Native Pose workspace | 两个常驻Native页 | Projection Native Pose layout |
| Inertialization | current/next Native页 | compiled inertialization operation、Rig与parameter布局 |
| Final Pose publisher | Committed/Pending双页与固定lease | PoseBone、parameter、contribution布局 |
| Physical Source registry | Committed owner表、pending registration journal与prepared release token | compiled physical source峰值 |
| Animancer source backend | 固定owner slot、clip plan、frame mutation、release permission与deferred resource batch | physical source与clip catalog容量 |
| Motion Matching | Pending selection/plan/history append与fixed completion | MM payload存在时的provider/database容量；无payload不创建Module |
| Diagnostics | interest bitset与预分配双页 | compiled operation/player/bone/watch容量 |

## Barrier后固定操作

Barrier前已经准备并验证Player、BlendStack、Physical Source与backend release token，以及每个route最后一次release的通知位。Barrier后只允许：

- 读取Native completion与Final Writer outcome。
- 交换Committed/Pending页。
- 按固定ordinal/index应用journal和release token。
- 断开并销毁已准备的Playable资源。
- 发布Action acknowledgement、retirement、release completion与Final Pose。
- 按interest复制Committed diagnostics页。

Barrier后不再按SourceId查找Player、BlendStack workspace或Physical Source owner，也不扫描release集合决定route完成。任一Barrier内或Barrier后异常都记录Actor、PresentationFrame、BodyTick、phase和completion，令该Animation Runtime进入Faulted并继续向上抛出；不会恢复全量骨骼或状态后继续下一帧。

## Final Pose与Diagnostics页

- Final publisher只有Committed与Pending两个常驻页。Prepare只写Pending，Seal只交换页索引，Discard只失效Pending lease；Committed lease保持到下一次合法写入完成。
- Diagnostics publisher继续使用预分配双页。Live、Capture、Pose Watch、Operation detail与Final detail由interest bitset决定复制范围。
- 无任何interest时不调用snapshot `BeginFrame`、Operation、Final Pose、Pose Watch或逐骨骼复制，只记录饱和skip计数。
- 容量指标在Runtime创建时冻结，公开Native Pose、Inertialization、Final Pose双页payload字节数以及Action、Sampling、Slot、source lifecycle journal和prepared resource容量；字节数不估算托管对象头或引用对象内容。

## 最终任务证据

| tasks范围 | 当前实现证据 |
|---|---|
| 1 | 本清单固定调用顺序、Physical写入点、旧快照删除清单、可变owner分类、容量来源与Barrier后操作。 |
| 2-3 | `AnimationPresentationFrameTransaction`、`PresentationFrameWorkspace`和`CharacterAnimationPresentationRuntime`实现唯一frame identity、阶段转换、反向Discard、Evaluate Barrier、固定Seal与Fault边界。 |
| 4 | Action inbox使用pending cursor；Lifecycle与sample history使用固定Header/payload journal并在Validate前拒绝重复、乱序、容量与identity错误；acknowledgement只在成功Seal推进。 |
| 5-6 | PoseState、三类Player、Slot、Routing与BlendStack使用Committed/Pending页、fixed dirty/release journal和固定completion；旧完整state capture/restore入口已删除。 |
| 7-8 | `AnimationPoseNativeWorkspace`、`PoseInertializationNativeProgram`和`FinalAnimationPoseFramePublisher`各自拥有两个常驻页；Begin只准备Pending，成功交换，Discard失效Pending。 |
| 9 | `AnimationFinalPosePhysicalWriter`同时绑定Committed/Pending Final Pose，先验证整Rig和全部local pose再进行唯一Physical Bone写入；typed invalid保持Committed Physical Pose并令外层Faulted。 |
| 10 | Physical registry、source pose workspace、三类Player与BlendStack在Barrier前生成固定release token；Barrier后按slot/index/ordinal应用，backend只执行prepared deferred lifecycle command。 |
| 11 | MM payload不存在时不创建Module；存在时Selection、Trajectory、Envelope、History均执行Begin/Commit/Discard，history completion绑定同一Pose Plan completion，branch replacement走唯一Reset。 |
| 12 | 首次Barrier内/后失败保存Actor、frame、body tick、phase与completion；Faulted Runtime拒绝后续Present，外层只记录一次结构化失败，异常继续传播。 |
| 13 | Snapshot publisher以固定owner bitset合并Live/Capture/Pose Watch/detail interest，只从成功Seal的Committed页复制；None直接跳过并增加skip计数。 |
| 14 | Animation/Presentation正式Runtime搜索不到旧`FrameState`、`CaptureState/RestoreState`、Physical Transform snapshot或`HeldCommittedPose`；未保留第二事务路径。 |
| 15 | 保留总Profiler marker并增加Prepare、Validate、GraphEvaluate、Seal、Diagnostics；Runtime公开常驻payload、journal/prepared容量、outcome、discard、fault phase和no-interest skip。 |
| 16 | Rollback snapshot与Prediction history源码不引用Animation Pose/Workspace；Body branch replacement继续通过ResetSequence，BoundedCorrection只由Body VisualRoot owner执行。 |
| 17 | current animation spec、project Current State与旧control-boundaries完成结论均已同步到Committed/Pending、Barrier、Fault和interest口径；插值与Rollback current spec未由本change改写。 |

## 允许执行的校验

- Animation与Presentation Runtime旧快照/恢复标识搜索为0。
- 旧Player、BlendStack、Physical Source release API搜索为0；无调用者的`AnimationPoseRequestWorkspace.ReleaseSource`已删除。
- Unity显式重导入本change涉及的Runtime脚本后Console error为0。
- `dotnet build Assembly-CSharp.csproj --no-restore --disable-build-servers /nr:false /p:UseSharedCompilation=false`成功，0 error；随后`dotnet build-server shutdown`成功。
- 以上证据只能证明已删除的复制、分配和双链路，不能代替Unity Profiler与本地/双端实机结果，也不构成“50 FPS已解决”的结论。

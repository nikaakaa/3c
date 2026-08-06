## 1. 固定事务现状与删除清单

- [x] 1.1 记录`CharacterSimulationPresentationRuntime -> CharacterAnimationPresentationRuntime -> PosePlanExecutionRuntime -> AnimancerGraph`当前调用顺序。
- [x] 1.2 记录Animancer Evaluate前、Evaluate内和Evaluate后的全部Physical Bone写入点。
- [x] 1.3 枚举动画正式Runtime中全部`CaptureState`调用点。
- [x] 1.4 枚举动画正式Runtime中全部`RestoreState`调用点。
- [x] 1.5 枚举`Clone`、`ToArray`、数组构造、Dictionary构造和List构造的PresentationFrame热路径调用点。
- [x] 1.6 枚举Action、Marker、PoseState、Player、Slot、Routing、MM和Physical Source的可变状态所有者。
- [x] 1.7 将每项可变状态分类为Dense双页、pending scalar、mutation journal、prepared resource或deferred command。
- [x] 1.8 记录每个Module由Projection决定的固定容量和当前动态扩容点。
- [x] 1.9 记录Animancer Evaluate后仍可能抛出业务异常的Complete、Validate、Publish和Release调用。
- [x] 1.10 记录`FinalAnimationPoseFramePublisher`现有Prepared/Published Page和Lease语义。
- [x] 1.11 记录`AnimationPresentationRuntimeSnapshotPublisher`当前无interest仍执行的复制链。
- [x] 1.12 将`PosePlanExecutionRuntime.FrameState`及其递归State类型列为破坏性删除目标。

## 2. 建立唯一帧阶段与所有权合同

- [x] 2.1 定义`AnimationPresentationFramePhase`的Begin、Prepare、Validated、EvaluateBarrier、Sealed、Discarded与Faulted状态。
- [x] 2.2 定义带frame identity、pending page index和phase的`AnimationPresentationFrameLease`。
- [x] 2.3 让frame identity只由`CharacterAnimationPresentationRuntime`分配。
- [x] 2.4 禁止Module自行把裸`ulong`解释为外层frame identity。
- [x] 2.5 定义`AnimationPresentationFrameOutcome`区分Committed、TypedInvalid与Faulted；TypedInvalid只表示Final Writer保持Committed Physical Pose，不建立第二种Held状态。
- [x] 2.6 定义Committed Page与Pending Page索引交换合同。
- [x] 2.7 定义Pending Page在Discard后必须提升generation并失效旧Lease。
- [x] 2.8 定义固定容量mutation journal的Header、owner domain、operation kind和payload索引。
- [x] 2.9 定义journal Prepare阶段的重复mutation拒绝规则。
- [x] 2.10 定义journal Validate阶段的容量、identity、依赖和顺序验证规则。
- [x] 2.11 定义journal Seal阶段不得失败的固定应用顺序。
- [x] 2.12 定义prepared resource在Commit与Discard之间的唯一所有权。
- [x] 2.13 定义deferred lifecycle command的Connect、Disconnect、Release与Destroy顺序。
- [x] 2.14 定义Actor Presentation Fault的phase、frame、body tick和completion上下文。
- [x] 2.15 将全部新容量从Projection编译布局或现有固定Runtime layout取得。
- [x] 2.16 禁止Runtime根据峰值临时扩容journal、page或resource batch。

## 3. 重写外层动画表现帧事务

- [x] 3.1 将`BeginFrameTransaction`改为选择Pending页并初始化固定journal计数。
- [x] 3.2 保留Action command inbox的Committed读取游标，不在Begin阶段消费acknowledgement。
- [x] 3.3 让Begin阶段只建立Lease，不调用任何Module完整状态捕获。
- [x] 3.4 将现有Action、Sampling、Slot、MM与Pose mutation lease统一绑定同一外层frame identity。
- [x] 3.5 将Prepare完成条件集中到外层事务的Validated转换。
- [x] 3.6 在进入Animancer Evaluate前执行唯一托管合同验证。
- [x] 3.7 将`m_Animancer.Evaluate`调用标记为唯一Animancer Evaluate Barrier入口。
- [x] 3.8 将Barrier前异常处理改为按反向所有权顺序Discard Pending。
- [x] 3.9 删除Barrier前异常处理中的Committed状态恢复调用。
- [x] 3.10 将Barrier后流程收敛为固定Seal顺序。
- [x] 3.11 将Action acknowledgement移到成功Seal。
- [x] 3.12 将Action lifecycle和retirement提交移到成功Seal。
- [x] 3.13 将source usage和release completion发布移到成功Seal。
- [x] 3.14 将Final Pose publication移到成功Seal。
- [x] 3.15 将diagnostics publication移到成功Seal之后并受interest控制。
- [x] 3.16 让TypedInvalid帧保持Committed Physical Pose且不交换Pending状态页，并在Evaluate Barrier后进入Actor Faulted。
- [x] 3.17 让Faulted状态拒绝下一次`Present`调用。
- [x] 3.18 保持原始异常向`CharacterSimulationPresentationRuntime`传播。

## 4. 暂存Action命令与生命周期

- [x] 4.1 将Action inbox staged read改为pending cursor，不复制command集合。
- [x] 4.2 让pending cursor在Seal时一次推进Committed cursor。
- [x] 4.3 让Discard只丢弃pending cursor。
- [x] 4.4 为Action playback registry定义固定容量mutation journal operation。
- [x] 4.5 将Select mutation写入journal。
- [x] 4.6 将Sample mutation写入journal。
- [x] 4.7 将Complete mutation写入journal。
- [x] 4.8 将Release mutation写入journal。
- [x] 4.9 让同帧registry读取通过Committed registry与pending journal统一view完成。
- [x] 4.10 在Validate阶段拒绝同Playback identity的非法mutation顺序。
- [x] 4.11 在Validate阶段证明journal应用不会超过registry容量。
- [x] 4.12 将Action sample history更新改为pending entry或pending cursor。
- [x] 4.13 将Action presentation projected time改为pending scalar。
- [x] 4.14 将Marker effective sample relation与cursor改为pending scalar。
- [x] 4.15 让Action usage batch写入固定容量pending batch。
- [x] 4.16 让retirement permission只读取成功Pose usage结果。
- [x] 4.17 删除Action Runtime只为帧回滚存在的完整State捕获。
- [x] 4.18 删除Sampling Runtime只为帧回滚存在的完整State捕获。

## 5. 暂存PoseState与Player状态

- [x] 5.1 为每个PoseStateMachine分配Committed/Pending固定状态槽。
- [x] 5.2 将active state、target state、transition generation和TimeInState写入Pending槽。
- [x] 5.3 让Transition Rule只读取同帧Pending Fact view与Committed/Pending统一状态view。
- [x] 5.4 让target provider readiness在Pending页记录。
- [x] 5.5 让Pending target未Ready时保持Committed source，不提交状态切换。
- [x] 5.6 为SequencePlayer分配Committed/Pending clock与source identity槽。
- [x] 5.7 为BlendSpacePlayer分配Committed/Pendingphase、weight和source identity槽。
- [x] 5.8 为SelectedPosePlayer分配Committed/Pending selection和usage槽。
- [x] 5.9 将Player pending release写入固定容量journal。
- [x] 5.10 在Validate阶段证明Player release引用的Physical Source仍由Committed或prepared owner持有。
- [x] 5.11 让Player Complete只写Pending completion outcome。
- [x] 5.12 让Seal交换Player状态槽并发布usage。
- [x] 5.13 让Discard失效本帧Player completion和release请求。
- [x] 5.14 删除PoseState Runtime的完整State capture/restore。
- [x] 5.15 删除SequencePlayer完整State capture/restore。
- [x] 5.16 删除BlendSpacePlayer完整State capture/restore。
- [x] 5.17 删除SelectedPosePlayer完整State capture/restore。

## 6. 暂存Slot、BlendStack与Transition Routing

- [x] 6.1 为AnimationSlot workspace分配Committed/Pending固定状态页。
- [x] 6.2 将Slot target、source usage、transition identity和completion写入Pending页。
- [x] 6.3 让Slot读取同帧Action journal的统一view。
- [x] 6.4 为Transition Routing workspace分配Committed/Pending控制状态页。
- [x] 6.5 将Prepared、AwaitingCompletion、Committed和release permission写入Pending页。
- [x] 6.6 让过期capture/release completion只产生typed outcome，不修改Committed route。
- [x] 6.7 为BlendStack entry metadata分配固定Committed/Pending布局。
- [x] 6.8 让BlendStack每帧算法输出直接写Pending weight和entry结果页。
- [x] 6.9 将Stored Pose capture request写入固定pending batch。
- [x] 6.10 将Stored Pose release request写入固定pending batch。
- [x] 6.11 在Validate阶段证明capture和release不会复用同一Physical Source槽位。
- [x] 6.12 让Seal按Routing、Stored Pose、Slot usage顺序提交状态。
- [x] 6.13 让Discard失效本帧Routing request和completion。
- [x] 6.14 删除Slot完整State capture/restore。
- [x] 6.15 删除BlendStack完整State capture/restore。
- [x] 6.16 删除Transition Route完整State capture/restore。

## 7. 双页化Native Pose与Inertialization

- [x] 7.1 按Projection Native layout为`AnimationPoseNativeWorkspace`创建两个常驻页。
- [x] 7.2 将所有Slot Dense Local Pose缓冲移动到页内。
- [x] 7.3 将所有Value Dense Local Pose缓冲移动到页内。
- [x] 7.4 将velocity、parameter、availability、contribution和weight缓冲移动到页内。
- [x] 7.5 将Pose Graph completion、invalid reason和operation index移动到页内。
- [x] 7.6 让`BeginFrame`只清理Pending页的必要count、status和completion字段。
- [x] 7.7 让Player和Slot Job binding只指向Pending页。
- [x] 7.8 让Pose Graph Job只读取Committed历史并写Pending结果。
- [x] 7.9 为Inertialization state、history pose和history velocity建立current/next页。
- [x] 7.10 为Inertialization position、rotation、scale和velocity residual建立current/next页。
- [x] 7.11 为Inertialization parameter residual建立current/next页。
- [x] 7.12 让Inertialization Job从current读取并向next写入。
- [x] 7.13 让成功Seal交换Native Workspace与Inertialization页。
- [x] 7.14 让TypedInvalid或Discard保持current页索引不变。
- [x] 7.15 删除`AnimationPoseNativeWorkspace.CaptureState`与`RestoreState`。
- [x] 7.16 删除NativeArray到托管数组的通用`Capture<T>`热路径。
- [x] 7.17 删除`PoseInertializationNativeProgram.CaptureState`与`RestoreState`。
- [x] 7.18 删除惯性化历史和residual的每帧托管数组复制。

## 8. 收口Final Pose双页

- [x] 8.1 保留`FinalAnimationPoseFramePublisher`现有Prepared/Published双Page作为唯一Final Pose页。
- [x] 8.2 将Published Page正式命名为Committed Page。
- [x] 8.3 将Prepared Page正式命名为Pending Page。
- [x] 8.4 让Prepare只写Pending Page并返回固定completion token。
- [x] 8.5 让Seal只交换Committed/Pending Page索引。
- [x] 8.6 让Discard只失效Pending Page Lease。
- [x] 8.7 让Committed Page Lease在下一帧Final Writer完成前保持可读。
- [x] 8.8 删除Final Publisher的DenseLocalPoses Clone。
- [x] 8.9 删除Final Publisher的PoseParameters Clone。
- [x] 8.10 删除Final Publisher的Contribution和DenseWeight Clone。
- [x] 8.11 删除Final Publisher Page Lease的帧回滚State捕获。
- [x] 8.12 删除Final Publisher完整`CaptureState/RestoreState`。

## 9. 重构Animancer Final Writer与提交门槛

- [x] 9.1 扩展Final Writer binding同时携带Committed与Pending Final Pose只读页。
- [x] 9.2 在写骨骼前验证全部Physical Bone Transform binding。
- [x] 9.3 在写骨骼前验证全部Physical Bone Pending local pose。
- [x] 9.4 在写骨骼前验证Pending availability、continuity和completion identity。
- [x] 9.5 在写骨骼前验证Pose Graph completion和invalid reason。
- [x] 9.6 Pending合法时按PhysicalBoneCount完整写入Pending Pose。
- [x] 9.7 Pending typed Invalid时按合同保持或写入Committed Pose。
- [x] 9.8 禁止Final Writer在验证完成前写入任何单根骨骼。
- [x] 9.9 将Final Writer outcome写入Pending Frame Outcome字段。
- [x] 9.10 将RootBonePolicy处理纳入同一Final Writer提交结果。
- [x] 9.11 删除Evaluate后的第二Physical Root纠正写入路径，或将其收敛为Final Writer唯一职责。
- [x] 9.12 将全部Job和Writer binding验证移动到Animancer Evaluate前。
- [x] 9.13 将Evaluate后的CompleteFrame方法改为不分配、不查找、不扩容的固定completion读取。
- [x] 9.14 让Evaluate后的completion mismatch进入Faulted，而不是启动全量恢复。
- [x] 9.15 保持唯一Animancer Graph和唯一Playable output链。

## 10. 暂存Physical Source与Playable生命周期

- [x] 10.1 为Physical Source registry定义Projection容量锁定的Committed owner表。
- [x] 10.2 为本帧Physical Source注册定义固定pending journal。
- [x] 10.3 为新Source Visual定义prepared resource owner。
- [x] 10.4 让`PrepareOrUpdate`区分Committed source update与新prepared source。
- [x] 10.5 让新Mixer、CapturePlayable和Clip State在Seal前不替换Committed owner。
- [x] 10.6 让Discard只销毁本帧prepared source资源。
- [x] 10.7 让旧source disconnect进入deferred command。
- [x] 10.8 让旧CapturePlayable destroy进入deferred command。
- [x] 10.9 让旧Mixer destroy进入deferred command。
- [x] 10.10 让Physical Source slot reuse发生在release completion成功Seal之后。
- [x] 10.11 在Validate阶段拒绝同槽位的capture/release冲突。
- [x] 10.12 在Validate阶段拒绝超出Projection峰值source容量。
- [x] 10.13 删除`PhysicalPoseSourceRegistry.CaptureState/RestoreState`。
- [x] 10.14 删除`AnimancerPoseSamplingBackend.CapturePhysicalTransformState`。
- [x] 10.15 删除`AnimancerPoseSamplingBackend.RestorePhysicalTransformState`。
- [x] 10.16 删除`PosePlanExecutionRuntime`对Physical Transform State的引用。

## 11. 接入Motion Matching帧事务

- [x] 11.1 保持无MM payload时不分配MM pending page或journal。
- [x] 11.2 将MM Resolve frame identity绑定外层Animation frame lease。
- [x] 11.3 将MM selection、plan和frozen output变化写入Module pending state。
- [x] 11.4 将MM Pose History append准备为pending mutation。
- [x] 11.5 让MM Complete只在Pose Plan completion匹配时提交pending history。
- [x] 11.6 让Discard失效本帧MM completion和history append。
- [x] 11.7 让branch replacement继续通过唯一MM Reset清理旧history。
- [x] 11.8 禁止MM状态进入Gameplay Snapshot或动画全量FrameState。
- [x] 11.9 删除MM Module只为外层帧回滚存在的完整State capture。
- [x] 11.10 保持MM search、candidate和selection算法不变。

## 12. 建立Actor Presentation Fault边界

- [x] 12.1 在`CharacterAnimationPresentationRuntime`增加唯一Faulted状态。
- [x] 12.2 保存首次Fault的ActorId、PresentationFrame、BodyTick、phase和completion identity。
- [x] 12.3 Barrier前异常执行统一`DiscardPending`。
- [x] 12.4 Barrier前异常不得修改Faulted状态之前的Committed页。
- [x] 12.5 Barrier期间异常设置Faulted并保留原异常。
- [x] 12.6 Barrier后Seal异常设置Faulted并保留原异常。
- [x] 12.7 Faulted Runtime后续`Present`立即拒绝调用。
- [x] 12.8 保持`CharacterSimulationPresentationRuntime`只记录一次结构化失败。
- [x] 12.9 保持异常继续传播到现有Actor/Session上层边界。
- [x] 12.10 删除“恢复完整Physical Pose后继续下一帧”的路径。
- [x] 12.11 禁止Fault时自动重建Animancer Graph或Animation Runtime。
- [x] 12.12 禁止Fault时切换旧Pose、Animator Controller或fallback动画。

## 13. 让Diagnostics按Interest工作

- [x] 13.1 定义Animation Runtime Snapshot Publisher读取当前interest位集的只读入口。
- [x] 13.2 将Live State interest映射为基础状态复制范围。
- [x] 13.3 将Capture interest映射为capture页发布范围。
- [x] 13.4 将Pose Watch interest映射为指定Pose Value与骨骼范围。
- [x] 13.5 将Candidate Detail interest保持在MM Module内部。
- [x] 13.6 无任何interest时跳过`BeginFrame`全部复制。
- [x] 13.7 无Operation detail interest时跳过`CopyOperations`。
- [x] 13.8 无Final Pose detail interest时跳过`CopyFinal`逐骨骼数据。
- [x] 13.9 无Pose Watch interest时跳过`CopyPoseWatches`。
- [x] 13.10 有interest时只读取成功Seal的Committed页。
- [x] 13.11 禁止Diagnostics读取Pending页或改变Pending Lease。
- [x] 13.12 复用现有Diagnostics双Page，不在PresentationFrame动态分配页。
- [x] 13.13 interest变化时只更新固定bitset和count。
- [x] 13.14 删除无interest仍执行的逐Stack、逐Operation和逐骨骼复制。
- [x] 13.15 保持Diagnostics不参与Pose选择、Gameplay或Final Writer。

## 14. 删除旧快照事务

- [x] 14.1 删除`PosePlanExecutionRuntime.FrameState`类型。
- [x] 14.2 删除`PosePlanExecutionRuntime.CaptureFrameState()`。
- [x] 14.3 删除`PosePlanExecutionRuntime.RestoreFrameState()`。
- [x] 14.4 删除`m_FrameState`字段。
- [x] 14.5 删除旧`PosePlanFrameMutationLease`的快照语义。
- [x] 14.6 删除旧`BeginMutation`入口或改名为唯一Pending Frame入口。
- [x] 14.7 删除旧`Rollback`中逐Module恢复步骤。
- [x] 14.8 删除旧rollback physical source收集列表。
- [x] 14.9 删除旧rollback release validation临时集合中失去用途的字段。
- [x] 14.10 删除所有只由`CaptureFrameState`调用的Clone helper。
- [x] 14.11 删除所有只由旧FrameState引用的State DTO。
- [x] 14.12 删除所有旧State DTO对应的Restore方法。
- [x] 14.13 搜索并删除Animation Presentation热路径剩余`CaptureState/RestoreState`。
- [x] 14.14 搜索并删除Animation Presentation热路径剩余`Clone/ToArray`状态备份。
- [x] 14.15 搜索并删除Physical Bone local pose快照类型。
- [x] 14.16 确认Runtime只剩唯一Committed/Pending事务链。

## 15. 收口Profiler标记与运行时不变量

- [x] 15.1 保留`ThirdPerson.Presentation.Animation`总标记。
- [x] 15.2 将旧Snapshot capture标记删除。
- [x] 15.3 增加Prepare阶段标记。
- [x] 15.4 增加Validate阶段标记。
- [x] 15.5 保留Graph Evaluate阶段标记。
- [x] 15.6 增加Seal阶段标记。
- [x] 15.7 将Diagnostics标记包围真实interest复制范围。
- [x] 15.8 在Runtime创建时记录Dense Page常驻字节数。
- [x] 15.9 在Runtime创建时记录journal和prepared resource容量。
- [x] 15.10 暴露只读Frame Outcome、DiscardCount和Fault phase诊断。
- [x] 15.11 暴露无interest diagnostics skip count。
- [x] 15.12 禁止Profiler和Diagnostics字段参与Gameplay hash或Pose选择。

## 16. 对账表现纠偏与网络边界

- [x] 16.1 保持Deterministic Rollback Snapshot不包含动画Pose和Runtime workspace。
- [x] 16.2 保持ServerAuthoritative Prediction History不包含动画Pose和Runtime workspace。
- [x] 16.3 保持Action committed raw sample作为表现时间锚点。
- [x] 16.4 保持Body branch replacement通过ResetSequence进入Presentation。
- [x] 16.5 让动画branch replacement从当前Committed Pose建立现有Blend或Inertialization接管。
- [x] 16.6 禁止branch replacement创建逐PresentationFrame骨骼历史。
- [x] 16.7 保持Gameplay hard recovery先恢复Character/World/Pipeline真相。
- [x] 16.8 保持`BoundedCorrection`只收敛visual root误差。
- [x] 16.9 保持离散Action Replace/Retire由EventId和Playback identity决定。
- [x] 16.10 禁止visual correction写回Gameplay、WorldSolver或KCC。

## 17. 同步正式文档

- [x] 17.1 将本change delta合并后的动画事务口径同步到current `character-animation-pipeline`。
- [x] 17.2 更新`openspec/project.md`的动画运行Current State为Committed/Pending staged transaction。
- [x] 17.3 在`project.md`明确Normal PresentationFrame不建立before-image快照。
- [x] 17.4 在`project.md`明确Animancer Evaluate Barrier和Actor Presentation Fault语义。
- [x] 17.5 在`project.md`明确Diagnostics按interest复制。
- [x] 17.6 保持`character-presentation-interpolation`的Gameplay hard recovery与visual bounded correction口径不变。
- [x] 17.7 保持`deterministic-rollback-network-model`的Snapshot Window与Recovery口径不变。
- [x] 17.8 更新`refactor-animation-control-boundaries`完成结论的对账说明，指出旧任务34和37.23由本change纠正。
- [x] 17.9 删除文档中把before-image restore描述为真实staging的残留表述。
- [x] 17.10 对账最终文档只描述一条Animation Presentation Runtime和一条Final Writer路径。

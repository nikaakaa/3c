# Change: 重构动画表现帧事务为预分配暂存提交

## Why

当前`CharacterAnimationPresentationRuntime`虽然对外提供`begin/commit/rollback`事务，但`PosePlanExecutionRuntime.BeginMutation`会在每个PresentationFrame调用`CaptureFrameState()`，递归复制PoseState、Player、BlendStack、Transition Routing、Native Pose workspace、Inertialization history、Physical Source、Final Pose publisher以及全部Physical Bone local pose。成功帧只把快照引用清空，导致正常运行仍承担完整备份成本。

Gameplay Lab Local Fixed双Actor现场采样中，动画表现约占7.7毫秒，采样帧产生3,694,031字节GC Alloc。GPU均值约4.64毫秒，问题主要位于CPU和托管分配。该场景使用Standard Local Pipeline，不保存prediction rollback history，因此这笔骨骼与运行状态复制和Gameplay预测回滚无关。

已完成的`refactor-animation-control-boundaries`在proposal与design中要求“真实staged transaction”，其任务34要求为各Module建立staged state/page/batch，任务37.23要求清除Presentation每帧分配或重复工作；但当前实现仍以全量before-image快照完成回滚，任务完成结论与运行时代码及现场数据不一致。本change不建立第二动画路径，而是破坏性纠正唯一正式事务实现。

## What Changes

- 将动画表现帧固定为`Prepare -> Validate -> Animancer Evaluate Barrier -> Seal`唯一阶段链。
- 对每帧必然完整生成的Dense Pose、Native workspace、Final Pose和Inertialization结果使用运行时创建时一次分配的Committed/Pending双页。
- 对Action lifecycle、command inbox cursor、Marker cursor、source ownership和release handshake等稀疏状态使用固定容量pending scalar或mutation journal，不复制完整Registry、Dictionary或List。
- 将Animancer Graph Evaluate定义为唯一不可逆提交门槛；进入门槛前不得消费command、销毁已提交source、发布completion或改变正式状态。
- 让Final Pose Stream Writer在Pending Pose合法时写入新Pose；Pending结果无效时保持已提交Pose，不发布部分Pose。
- 把Physical Source创建、连接、更新和释放降低为有界prepared resource与deferred lifecycle command；旧source只在完整帧成功后释放。
- 删除`PosePlanExecutionRuntime.FrameState`、`CaptureFrameState()`、`RestoreFrameState()`和只为表现帧回滚存在的各Module `CaptureState/RestoreState`。
- 删除每帧Physical Bone Transform捕获与恢复；真实Rig只由唯一Final Pose写入边界修改。
- 把预期的Pending、Unavailable和Invalid保持为typed outcome；不可预期异常在提交门槛前只丢弃Pending，在提交门槛后使当前Actor Presentation Runtime进入Faulted并继续向上抛错，不尝试全量恢复后继续运行。
- 让动画诊断先检查显式interest；无interest时不得复制BlendStack、Operation、逐骨骼贡献、Final Pose或Pose Watch数据。
- 保持Gameplay rollback snapshot、input history、hard recovery、Body interval history和`BoundedCorrection`现有合同不变；网络仍不保存或发送动画Pose。

## Non-Goals

- 不修改Deterministic Rollback或ServerAuthoritative Prediction的Gameplay Snapshot、输入历史、Restore/Replay和EventId disposition算法。
- 不把骨骼Pose、Animancer state、PoseState workspace、Slot weight或MM history加入Gameplay Snapshot或网络协议。
- 不新增第二PlayableGraph、第二Animator、隐藏Sampling Rig、兼容Runtime、配置开关或旧快照fallback。
- 不改变Pose Graph作者拓扑、AnimationClip资源、Transition Blend数学、IK算法、Foot Placement算法或Motion Matching搜索算法。
- 不把表现误差插值写回Character State、World State、KCC或Gameplay Action状态。
- 不新增自动化测试或手动验证任务。

## Current Spec Comparison

- current `character-animation-pipeline`要求PresentationFrame原子消费command，并要求唯一Pose Plan与唯一Final Writer，但没有定义Committed/Pending存储、不可逆提交门槛和异常后的Faulted语义；该缺口允许实现用全量快照替代真实staging。本change增加明确事务要求。
- current `character-animation-pipeline`允许每帧发布正式diagnostics snapshot，但没有要求无interest时跳过逐骨骼和逐Operation复制。本change修改该requirement。
- current `character-presentation-interpolation`已经规定Gameplay rollback只提交修正后的Body/Intent与Action identity，动画在本地重求值，网络不发送Pose，并由`BoundedCorrection`只平滑visual error。本change保持该设计，不新增动画历史回滚。
- current `character-animation-transition-routing-module`明确Inertialization Request不携带Pose或骨骼数组，和pending journal设计一致，不需要修改。
- current `character-motion-matching-presentation-module`要求MM状态不进入Gameplay Snapshot，并在branch replacement时清理history。本change只替换其帧内staging接入方式，不改变MM能力合同。
- completed `refactor-animation-control-boundaries`的design已经描述真实staged transaction，但其tasks未锁定零托管分配、双页所有权、Final Pose写入门槛或禁止before-image快照。本change以新的严格delta纠正实施缺口，不恢复旧Runtime。

## Impact

- Runtime协调：`CharacterAnimationPresentationRuntime`、`CharacterSimulationPresentationRuntime`。
- 动画运行Module：Action Playback、Presentation Sample、Marker、PoseState、Sequence/BlendSpace/Selected Player、AnimationSlot、Transition Routing、BlendStack、Motion Matching frame integration。
- Native Pose：`AnimationPoseNativeWorkspace`、`CharacterPoseGraphNativeProgram`、`PoseInertializationNativeProgram`、`FinalAnimationPoseFramePublisher`和Page Lease。
- Animancer边界：`AnimancerPoseSamplingBackend`、source visual registry、capture jobs、`AnimationFinalPosePhysicalWriter`、root policy和source release。
- Diagnostics：`AnimationPresentationRuntimeSnapshotPublisher`、target registry和interest读取。
- 内存：每Actor增加由Projection容量决定的常驻Pending页和有界journal，删除每帧临时数组、字典和骨骼快照。
- 失败语义：提交门槛后的不可预期异常不再尝试恢复并继续；对应Actor Presentation Runtime明确Faulted并向上报错。
- 文档：修改current `character-animation-pipeline`，实现完成后同步`openspec/project.md`。

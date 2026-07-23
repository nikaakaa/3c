# Implementation Audit

## 迁移前Owner

- `CharacterPresentationRuntimeFactory`通过`CreateMotionMatchingTrajectorySource`创建Accepted Intent或Selected Body具体Adapter。
- `CharacterSimulationPresentationRuntime`保存`m_LatestTrajectoryIntent`、`m_HasTrajectoryIntent`、`m_SelectedTrajectorySequence`，并通过具体类型判断发布trajectory frame。
- `CharacterAnimationPlaybackRuntime`保存MM producer、sampling、frozen output、resolved producer、frame selection与清理集合，并直接执行Resolve、retained request恢复、History追加、Replay查找、Reset和Dispose。
- Timeline sampling、通用Animation Selection lifecycle、显式Player节点、唯一Pose Graph与FootPlacement算法不属于Module所有权迁移范围。

## 迁移后唯一链

```text
正式Body frame / 可选Accepted Intent
  -> CharacterAnimationPlaybackRuntime降低MM playback demand
  -> CharacterMotionMatchingPresentationModule.ResolveFrame
       -> internal Trajectory Adapter
       -> CharacterMotionMatchingProducerRuntime
       -> AnimationSelectionFrame batch
  -> MotionMatchingSelectionInput
  -> 显式SelectedPosePlayer或BlendStack
  -> 唯一Pose Graph Plan
  -> CharacterMotionMatchingPresentationModule.CompleteFrame
       -> bound PoseNode Pose History
  -> FootPlacement world-aware phase
  -> FinalAnimationPoseFrame
  -> Camera
```

## 删除清单

- 删除外部`ICharacterMotionMatchingTrajectorySource`。
- 删除外部`AcceptedIntentMotionMatchingTrajectorySource`与`SelectedBodyMotionMatchingTrajectorySource`。
- 删除Factory的trajectory source变量、创建方法、失败释放分支和所有权转移字段。
- 删除Simulation Presentation的Adapter字段、具体类型判断、Intent缓存、Selected sequence、trajectory publish helper、独立Reset与Dispose。
- 删除Playback的MM producer、sampling、frozen output、resolved producer、frame selection与remove集合。
- 删除Playback直接Resolve、retained request恢复、History追加、output prune、producer Reset、Replay查找和Dispose helper。
- 不保留wrapper、fallback、双写状态、私有Blend Stack、私有Pose Graph或第二个PlayableGraph。

## Current Spec对比

- current `character-animation-pipeline`的唯一Simulation Presentation协调器保持不变；Pose执行升级为唯一编译Pose Plan。
- 动画选择边界change拥有Selection与显式Player生命周期；MM Module只消费正式demand并发布Selection batch。
- active `add-character-motion-matching-pose-source`的source-neutral Selection、bound PoseNode History、Replay与Preview描述已同步为深Module owner。
- active `add-character-presentation-pose-graph`的同一PlayableGraph、单次Evaluate和FinalAnimationPoseFrame合同保持不变。
- `refactor-animation-playback-to-blend-stack`的显式Blend Stack节点继续是多source CrossFade、Stored Pose与source release唯一owner；`refactor-inertial-blending-to-local-pose-node`的局部Inertialization节点是单Pose residual、history与rebase唯一owner。

## 实施中澄清

原设计把frozen output清理绑定到固定Stack release与整个Playback Lifecycle Retired。长期Playback会在同一activation内产生多个Selection Generation；若等待整个Playback Retired，旧generation output会无界积累。正式口径改为：Module只保留显式Player节点通过Pose Plan completion报告仍在使用的selection output；全部Player release后立即清理。该澄清不增加第二份source usage状态。

## 剩余矛盾

- 已识别并由`refactor-animation-selection-pose-graph-boundary`解决固定PoseSlot Stack与新显式Player边界的矛盾。
- `add-character-motion-matching-pose-source`仍有独立验证Definition与内容资产任务，不属于本次表现Module所有权迁移，也没有用临时配置代替。

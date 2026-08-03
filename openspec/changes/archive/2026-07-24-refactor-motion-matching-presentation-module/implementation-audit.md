# Implementation Audit

> 本审计只说明条件式MM基础设施代码的迁移程度。当前没有正式角色、Definition或Projection启用MM，下面的链是含合法MM payload时才可能执行的目标链，不能作为“MM已经接入”的证明。

## 迁移前Owner

- `CharacterPresentationRuntimeFactory`通过`CreateMotionMatchingTrajectorySource`创建Accepted Intent或Selected Body具体Adapter。
- `CharacterSimulationPresentationRuntime`保存`m_LatestTrajectoryIntent`、`m_HasTrajectoryIntent`、`m_SelectedTrajectorySequence`，并通过具体类型判断发布trajectory frame。
- `CharacterAnimationPlaybackRuntime`保存MM producer、sampling、frozen output、resolved producer、frame selection与清理集合，并直接执行Resolve、retained request恢复、History追加、Replay查找、Reset和Dispose。
- Timeline sampling、通用Animation Selection lifecycle、显式Player节点、唯一Pose Graph与FootPlacement算法不属于Module所有权迁移范围。

## 唯一正式链

```text
正式Body frame / 可选Accepted Intent
  -> PoseStateMachine发布MM relevance demand
  -> CharacterMotionMatchingPresentationModule.ResolveFrame
       -> internal Trajectory Adapter
       -> CharacterMotionMatchingProducerRuntime
       -> State内部Selection batch
  -> PoseState MM Player input
  -> 显式SelectedPosePlayer或BlendStack
  -> 唯一Pose Graph Plan
  -> CharacterMotionMatchingPresentationModule.CompleteFrame
       -> bound PoseNode Pose History
  -> FootPlacement world-aware phase
  -> FinalAnimationPoseFrame
  -> Camera
```

## 当前已落地

- Factory按Projection MM payload构造唯一Module，并把所有权原子转移给Playback；构造失败时释放Module，无MM payload时不构造MM工作区。
- Accepted Intent与Selected Body Adapter、Intent单调sequence、Selected Body trajectory sequence和Reset都已经移动到Module内部。
- Simulation Presentation只委托`AcceptsTrajectoryIntent`、提交Accepted Intent和正式Body frame，不再识别具体Adapter。
- producer runtime、sampling映射、frozen output、resolved producer、frame selection、query、search、plan、history、Replay、Reset与Dispose状态已经移动到Module。
- 外部`ICharacterMotionMatchingTrajectorySource`、旧具体Adapter类型、Factory创建方法、Simulation Presentation旧缓存与Playback逐producer owner已经删除。
- Query Fixture Preview已经显式选择Definition、producer与Preview Target，构造同一Module并经过正式Pose Source、Selection、显式Player、编译Pose Plan和Complete合同。
- History已经从固定Slot迁移为绑定MM Player PoseNode的正式完成Pose；Foot feature从同一完成结果读取。
- Playback、Module和Query Fixture都没有构造MM私有Blend Stack、Inertialization、Pose Graph或第二个PlayableGraph。

## 当前实施闭合

1. Module返回`MotionMatchingFrameResolution`，只公开固定`MotionMatchingSelectionBatchItem`集合、Selection count、resolved provider count、history completion需求与非零completion identity。
2. PoseState runtime批量接收Module结果并向精确State内部Player提交`PresentationPoseSourceSample`；外层动画协调器不逐项查找Player或组装source usage。
3. `ResolveFrame`只接收Body frame、表现delta、PoseState demand与diagnostics，Module独占Selection workspace和sequence分配。
4. Pose Plan通过`MotionMatchingPosePlanCompletion`发布固定容量Player source usage、绑定PoseNode completion、Foot Feature与Pose Plan completion identity。
5. `CompleteFrame`只消费Resolution与typed Pose Plan completion，不接收整个Pose Runtime，不调用`TryCopyPlayerPose`或`RetainsSource`。
6. diagnostics发布Selection count、Resolve/Complete identity、history appended/gap与retained frozen output count；Player引用缺失frozen output会产生`RetainedOutputMissing` typed failure。

当前Module代码断点已经清零。尚未完成的是后续change拥有的正式MM内容配置、Database Artifact、Projection发布与角色接线。

## Current Specs对比

- current `character-animation-pipeline`的唯一Simulation Presentation协调器保持不变；Pose执行升级为唯一编译Pose Plan。
- current `character-animation-selection-runtime`拥有Selection与显式Player生命周期；MM Module只消费正式demand并发布Selection batch。
- active `add-character-motion-matching-pose-source`的source-neutral Selection、bound PoseNode History、Replay与Preview描述已同步为深Module owner。
- current `character-presentation-pose-graph`的同一PlayableGraph、单次Evaluate和FinalAnimationPoseFrame合同保持不变。
- current `character-animation-layer-runtime`中的显式Blend Stack节点继续是多source CrossFade、Stored Pose与source release唯一owner；局部Inertialization节点是单Pose residual、history与rebase唯一owner。
- current `character-presentation-interpolation`继续规定Body、表现时钟与Camera顺序；Module不得重写Body或另算Remote轨迹真相。

## 实施中澄清

原设计把frozen output清理绑定到固定Stack release与整个Playback Lifecycle Retired。长期Playback会在同一activation内产生多个Selection Generation；若等待整个Playback Retired，旧generation output会无界积累。正式口径改为：Module只保留显式Player节点通过Pose Plan completion报告仍在使用的selection output；全部Player release后立即清理。该澄清不增加第二份source usage状态。

## 剩余边界

- Selection、显式Player、Pose Plan、局部Inertialization与Blend Stack边界已经安装到current specs，不再是本change的外部阻塞项。
- `add-character-motion-matching-pose-source`只剩独立正式验证Definition与完整内容identity，不能用Corin、fallback或临时配置代替。
- 已导入MxM插件目前只存在于`Assets/Plugins/MotionMatching`，GameScripts与正式配置没有引用。它的Runtime animator、search、trajectory、mixer、layer、transition、root motion和PlayableGraph不得接入本链。
- 若未来复用MxM内容，必须另开change定义Editor-only显式导入器，转换为项目正式Database或Artifact；这不是本change剩余任务。

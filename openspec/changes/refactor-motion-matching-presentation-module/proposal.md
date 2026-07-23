# Change: 深化 Motion Matching 表现 Module

## 重新基线

`refactor-animation-selection-pose-graph-boundary`把Module输出从公共`ResolvedAnimationPoseRequest`改为固定容量`AnimationSelectionFrame`集合。Module继续拥有MM查询、选择、history、Replay、Reset与帧事务，但不再与固定PoseSlot Stack或Base Slot结果耦合。

Resolve阶段输出Selection batch与completion identity；Pose Graph通过`MotionMatchingSelectionInput`消费它；Complete阶段读取匹配PoseNodeId的正式Pose Plan结果并追加下一帧History。直接Player或显式Blend Stack均由Pose Graph决定，Module不持有其transition或retention状态。

## Why

现有 Motion Matching producer runtime 已经集中实现Trajectory Envelope、Pose History、Query、Admission、Search、Plan、Selection与Pose Source，但它上方的表现协调仍分散在三处：

- `CharacterPresentationRuntimeFactory`选择并创建Accepted Intent或Selected Body trajectory Adapter。
- `CharacterSimulationPresentationRuntime`缓存Intent、维护Selected Body sequence、按具体Adapter类型发布帧并负责trajectory Reset与Dispose。
- `CharacterAnimationPlaybackRuntime`直接拥有MM producer集合、MM sampling、frozen output、frame selection、retention恢复、history追加、Replay代理与输出清理。

因此`ICharacterMotionMatchingTrajectorySource`虽然有两个真实Adapter，外部调用者仍必须通过具体类型判断才能写入；Playback也必须知道MM查询、选择、保留和history完成的顺序。Remote Body、Prediction、Replay或Preview只要改变一项，就会同时修改Factory、Simulation Presentation与Playback，正式验证面无法收敛成一份表现帧合同。

本change把这些MM特有职责收敛进唯一内部`CharacterMotionMatchingPresentationModule`。该Module输出公共`AnimationSelectionFrame`，仍由唯一Animation Selection lifecycle和编译Pose Graph Plan完成播放、连续化、合成与FootPlacement。它不是第二个动画协调器、第二个播放器或第二条姿势路径。

## What Changes

- 新增唯一`CharacterMotionMatchingPresentationModule`，当且仅当Projection包含合法MM payload时构造。
- Module唯一拥有trajectory Adapter、最新Accepted Intent、Selected Body sequence、producer runtime集合、MM sampling、frozen selection output、frame completion、Pose History、diagnostics、Replay、Reset与Dispose。
- Accepted Intent与Selected Body继续是两个内部Adapter，但Factory、Simulation Presentation与Playback不再识别其具体类型；外部只提交正式Body frame和可选Accepted Intent。
- 将MM表现帧定义为同一个逻辑事务的两个阶段：
  - Resolve阶段读取Body/Intent与当前MM playback demand，生成固定容量`AnimationSelectionFrame`集合与completion identity。
  - Complete阶段只在匹配PoseNode的Pose Plan阶段完成后读取正式Pose Value，追加Pose History并完成本帧清理。
- `CharacterAnimationPlaybackRuntime`继续唯一拥有通用channel selection、`AnimationPlaybackLifecycle`、Command Queue、Timeline sampling、Pose Plan求值与batch acknowledge；它只通过窄MM Module接口取得Selection batch并提交completion。
- `SelectedPosePlayer`与`BlendStack`分别发布各自真实source usage；局部`Inertialization`只消费完成Pose而不保留旧source。MM Module只保存Pose Plan completion明确报告仍在使用的selection，不复制entry、transition clock、Stored Pose、Inertial residual或release算法。
- Query Fixture Preview复用同一MM Module、Selection lowering、编译Pose Plan、显式Player与Pose Graph；Editor-only query输入不执行Program、WorldSolver、Foot Physics或Camera。
- 删除Factory和Simulation Presentation中的具体trajectory Adapter创建、`is`判断、Intent缓存与Selected sequence；删除Playback中的MM producer map、sampling map、output cache、frame selection和history/prune helper。
- 不保留旧调用路径、兼容wrapper、fallback trajectory source或双写状态。

## Impact

### Specs

- 新增`character-motion-matching-presentation-module`。
- 不修改现有`character-animation-pipeline`、`character-animation-layer-runtime`和`character-presentation-interpolation`权威：它们已经规定唯一Lifecycle、Blend Stack、Pose Graph与Pose Post Process，本change只深化MM在该链之前的内部Module。
- 实施完成后同步更新`openspec/project.md`与`add-character-motion-matching-pose-source`中仍描述旧分散owner的设计和任务口径。

### Code

- `CharacterPresentationRuntimeFactory`
- `CharacterSimulationPresentationRuntime`
- `CharacterAnimationPlaybackRuntime`
- `MotionMatchingTrajectoryContracts`
- `CharacterMotionMatchingProducerRuntime`
- MM diagnostics、Search Replay与Query Fixture Preview
- Animation Presentation Runtime Snapshot provider

### Active Change 关系

- 依赖`add-character-motion-matching-pose-source`已经建立的Profile、Projection payload、Runtime Database、Query/Search/Plan、Pose Source和Replay合同。本change不复制这些实现，只迁移其表现层owner。
- 依赖`refactor-animation-playback-to-blend-stack`的显式Blend Stack节点算法与source usage语义；MM Module不得实现自己的transition或source release算法。
- 依赖`refactor-inertial-blending-to-local-pose-node`提供可选`SelectedPosePlayer -> Inertialization`局部连续化；MM Module只发布Discontinuity所需Selection identity，不拥有residual或clock。
- 依赖`add-character-presentation-pose-graph`的唯一Pose Graph和FinalAnimationPoseFrame；MM History completion必须发生在正式Pose Graph完成后，Foot Placement之前。
- 本change可以在上述Runtime合同稳定后实施，但必须在`add-character-motion-matching-pose-source`归档前同步其设计与任务状态，避免两个active change描述不同owner。

## Breaking Changes

- `ICharacterMotionMatchingTrajectorySource`不再作为Factory、Simulation Presentation或Playback可见的读帧接口；两个具体Adapter降为MM Module内部实现。
- `CharacterSimulationPresentationRuntime`不再保存`m_LatestTrajectoryIntent`、`m_HasTrajectoryIntent`与`m_SelectedTrajectorySequence`。
- `CharacterAnimationPlaybackRuntime`不再保存MM producer、sampling、frozen output、resolved producer与frame selection集合，也不再直接追加MM Pose History。
- MM表现帧从nullable trajectory frame参数迁移为Module消费的正式Body/Intent frame input与两阶段completion合同。
- Query Fixture Preview删除任何绕过MM Module直接拼装Pose Source或临时PlayableGraph的入口。
- 不提供旧Interface Adapter、双调用、默认trajectory source或旧字段兼容。

## Current Spec Comparison

- current `character-animation-pipeline`要求`CharacterSimulationPresentationRuntime`仍是唯一Unity动画应用协调器，并把command交给唯一Playback/Lifecycle/Blend Stack/Pose Graph链。本change保留唯一协调器，但按选择边界change把执行对象升级为编译Pose Plan；MM Module不成为并列协调器。
- current `character-animation-layer-runtime`要求通用Lifecycle管理Selected、Pending、Retained与Retired。本change让显式Player节点发布source usage，Blend Stack只管理其多source transition与release，局部Inertialization只管理残差；MM Module只消费完成事实，不夺取任何权威。
- active `add-character-motion-matching-pose-source`已经重新规定MM输出Animation Selection、History按绑定PoseNode的完成Pose追加。本change把Resolve与Complete收敛为同一帧事务，不改变搜索算法语义。
- active `add-character-presentation-pose-graph`要求同一PlayableGraph只Evaluate一次并发布唯一FinalAnimationPoseFrame。本change的Complete阶段必须复用该完成结果，不能新增第二次求值。
- 未发现需要删除的current Requirement；需要删除的是现有分散Implementation和active设计文档中的旧owner描述。

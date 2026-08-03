# Change: 深化 Motion Matching 表现 Module

## 当前实施基线

本change只描述尚未被正式角色启用的MM基础设施内部重构，不代表Motion Matching已经接入项目。当前没有含MM payload的正式Definition、Profile、Database、Projection或运行角色。串行顺序固定为先完成`refactor-animation-control-boundaries`建立PoseState relevance和State内部Player边界，再完成本Module剩余重构，最后由`add-character-motion-matching-pose-source`创建正式内容配置；不得先发布基于旧BaseLocomotion demand的MM Projection。

`refactor-animation-control-boundaries`把`PoseState relevance -> State内部PresentationPoseSourceSample -> 显式Player -> branch-local Inertialization -> 编译Pose Plan`定义为唯一MM表现入口。Module继续拥有MM查询、选择、history、Replay、Reset与帧事务，不再与Gameplay AnimationChannel、AnimationPlaybackId、固定PoseSlot Stack或Base Slot结果耦合。

Resolve阶段输出State内部`PresentationPoseSourceSample` batch与completion identity；当前relevant PoseState的显式Player消费它；Complete阶段读取匹配PoseNodeId的正式Pose Plan结果并追加下一帧History。直接Player或显式BlendStack均由Pose Graph决定，Module不持有其transition或retention状态。

当前代码已经完成Module构造、内部Trajectory Adapter、provider runtime、固定PoseState demand、state-local Selection batch、Reset、Replay、Query Fixture与Player Pose History读取。旧`ResolvedFrameRequest`/`RequestCount`包装、Playback手工遍历MM结果、手工组装Player source usage和Module直接查询Pose Runtime retention均已删除；Pose Plan现在发布固定容量typed completion，Module只消费其中的source usage、绑定PoseNode结果与completion identity。`tasks.md`已经回填该代码事实，但它不代表正式MM内容已经接入。

## Why

现有 Motion Matching producer runtime 已经集中实现Trajectory Envelope、Pose History、Query、Admission、Search、Plan、Selection与Pose Source，但它上方的表现协调仍分散在三处：

- `CharacterPresentationRuntimeFactory`选择并创建Accepted Intent或Selected Body trajectory Adapter。
- `CharacterSimulationPresentationRuntime`缓存Intent、维护Selected Body sequence、按具体Adapter类型发布帧并负责trajectory Reset与Dispose。
- 旧`CharacterAnimationPlaybackRuntime`直接拥有MM producer集合、MM sampling、frozen output、frame selection、retention恢复、history追加、Replay代理与输出清理。

因此`ICharacterMotionMatchingTrajectorySource`虽然有两个真实Adapter，外部调用者仍必须通过具体类型判断才能写入；Playback也必须知道MM查询、选择、保留和history完成的顺序。Remote Body、Prediction、Replay或Preview只要改变一项，就会同时修改Factory、Simulation Presentation与Playback，正式验证面无法收敛成一份表现帧合同。

本change把这些MM特有职责收敛进唯一内部`CharacterMotionMatchingPresentationModule`。该Module只按PoseState relevance输出State内部`PresentationPoseSourceSample`，仍由State内部显式Player和编译Pose Graph Plan完成播放、连续化、合成与FootPlacement。它不是Gameplay channel producer、第二个动画协调器、第二个播放器或第二条姿势路径。

## What Changes

- 新增唯一`CharacterMotionMatchingPresentationModule`，当且仅当Projection包含合法MM payload时构造。
- Module唯一拥有trajectory Adapter、最新Accepted Intent、Selected Body sequence、producer runtime集合、MM sampling、frozen selection output、frame completion、Pose History、diagnostics、Replay、Reset与Dispose。
- Accepted Intent与Selected Body继续是两个内部Adapter，但Factory、Simulation Presentation与Playback不再识别其具体类型；外部只提交正式Body frame和可选Accepted Intent。
- 将MM表现帧定义为同一个逻辑事务的两个阶段：
  - Resolve阶段读取Body/Intent与当前PoseState relevance demand，生成固定容量State内部`PresentationPoseSourceSample`集合与completion identity。
  - Complete阶段只在匹配PoseNode的Pose Plan阶段完成后读取正式Pose Value，追加Pose History并完成本帧清理。
- `CharacterAnimationPresentationRuntime`继续编排PoseState relevance、MM Resolve与Pose Plan求值；Action channel lifecycle与Timeline raw sample由独立`CharacterActionPlaybackRuntime`处理。MM demand只来自PoseStateMachine，协调器通过窄MM Module接口取得state-local sample batch并提交completion。
- `SelectedPosePlayer`与`BlendStack`分别发布各自真实source usage；局部`Inertialization`只消费完成Pose而不保留旧source。MM Module只保存Pose Plan completion明确报告仍在使用的selection，不复制entry、transition clock、Stored Pose、Inertial residual或release算法。
- Query Fixture Preview复用同一MM Module、Selection lowering、编译Pose Plan、显式Player与Pose Graph；Editor-only query输入不执行Program、WorldSolver、Foot Physics或Camera。
- 删除Factory和Simulation Presentation中的具体trajectory Adapter创建、`is`判断、Intent缓存与Selected sequence；删除Playback中的MM producer map、sampling map、output cache、frame selection和history/prune helper。
- 不保留旧调用路径、兼容wrapper、fallback trajectory source或双写状态。

## 后续动画职责重构关系

本change收敛的MM Module、frame transaction、Pose History、Resolve/Complete和普通Selection合同继续保留。`refactor-animation-control-boundaries`先安装PoseState relevance和State内部Player边界；本change剩余任务直接以该边界为唯一demand与消费点。MM Module仍不得拥有Gameplay movement、Pose transition、Slot或Inertialization算法。

## Impact

### Specs

- 新增`character-motion-matching-presentation-module`。
- 不修改Action Gameplay lifecycle、PoseState transition、AnimationSlot、Pose Graph与Pose Post Process权威；本change只深化PoseState relevance与State Player之间的MM内部Module。
- 实施完成后同步更新`openspec/project.md`与`add-character-motion-matching-pose-source`中仍描述旧分散owner的设计和任务口径。

### Code

- `CharacterPresentationRuntimeFactory`
- `CharacterSimulationPresentationRuntime`
- `CharacterAnimationPresentationRuntime`
- `MotionMatchingTrajectoryContracts`
- `CharacterMotionMatchingProducerRuntime`
- MM diagnostics、Search Replay与Query Fixture Preview
- Animation Presentation Runtime Snapshot provider

### Current Spec与Active Change关系

- 依赖`add-character-motion-matching-pose-source`已经建立的Profile、Projection payload、Runtime Database、Query/Search/Plan、Pose Source和Replay合同。本change不复制这些实现，只迁移其表现层owner。
- current `character-animation-selection-runtime`已经安装显式SelectedPosePlayer、BlendStack source usage与Selection Preview合同；MM Module不得实现自己的transition或source release算法。
- `refactor-animation-control-boundaries`已经安装PoseState transition、局部Inertialization、单一source backend与State Player source usage；MM Module只发布Discontinuity所需state-local source identity，不拥有residual或clock。
- current `character-animation-pipeline`与`character-presentation-pose-graph`已经安装唯一Pose Plan和FinalAnimationPoseFrame；MM History completion必须消费同一次Pose Plan完成结果，不能新增第二次求值。
- 本change不再等待动画基座依赖，可以直接继续实施；完成前必须同步`add-character-motion-matching-pose-source`中的旧owner、旧request和旧Base Slot描述。
- 已导入的第三方MxM不属于Runtime依赖。其`MxMAnimator`、Search Manager、Trajectory、Mixer、Layer、Transition、Root Motion与PlayableGraph不得进入正式管线；若以后复用其离线数据，必须另建Editor-only显式Importer change并输出项目正式Artifact。

## Breaking Changes

- `ICharacterMotionMatchingTrajectorySource`不再作为Factory、Simulation Presentation或Playback可见的读帧接口；两个具体Adapter降为MM Module内部实现。
- `CharacterSimulationPresentationRuntime`不再保存`m_LatestTrajectoryIntent`、`m_HasTrajectoryIntent`与`m_SelectedTrajectorySequence`。
- `CharacterAnimationPresentationRuntime`不保存MM provider、sampling、frozen output、resolved provider与frame selection集合，也不直接追加MM Pose History。
- MM表现帧使用Module消费的正式Body/Intent frame input、固定state-local Selection batch与两阶段completion合同。
- Query Fixture Preview删除任何绕过MM Module直接拼装Pose Source或临时PlayableGraph的入口。
- 不提供旧Interface Adapter、双调用、默认trajectory source或旧字段兼容。

## Current Spec Comparison

- current `character-animation-pipeline`要求`CharacterSimulationPresentationRuntime`仍是唯一Unity动画应用协调器，并把command交给唯一Playback/Lifecycle/Player/Pose Plan链。本change保留唯一协调器；MM Module不成为并列协调器。
- `refactor-animation-control-boundaries`要求PoseStateMachine唯一发布MM relevance、显式Player唯一发布source usage，PoseState edge和AnimationSlot分别管理自己的transition与release。本change让MM Module只消费这些完成事实，不夺取任何权威。
- active `add-character-motion-matching-pose-source`已经重新规定MM输出Animation Selection、History按绑定PoseNode的完成Pose追加。本change把Resolve与Complete收敛为同一帧事务，不改变搜索算法语义。
- current `character-presentation-pose-graph`要求同一PlayableGraph只Evaluate一次并发布唯一FinalAnimationPoseFrame。本change的Complete阶段必须复用该完成结果，不能新增第二次求值。
- 未发现需要删除的current Requirement；需要删除的是现有分散Implementation和active设计文档中的旧owner描述。

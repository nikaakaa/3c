# Change: 重构动画选择与显式 Pose Graph 边界

## Why

当前动画表现链把每个`AnimationChannelId`先绑定到隐藏`PoseSlotId`，再由Runtime为每个Pose Slot自动创建固定Blend Stack，最后只把已经完成时间混合的`PoseSlotFrame`交给Pose Graph。这个实现统一了transition，但把真正影响画面的播放与过渡藏在Pose Graph外：

- BTSMTL StateMachine、Timeline与Motion Matching都已经能够决定当前动画选择和表现参数，却只能进入固定Stack。
- Pose Graph作者只能看到`PoseSlotInput`之后的Layered/Additive，不能决定某条选择使用直接播放、普通Blend、Blend Stack或硬切。
- 普通Timeline动作、稳定单Clip和动态Motion Matching都承担相同的Stack、Stored Pose、完整matrix与retention成本。
- Blend Stack被误写成每个Slot的基础设施，而不是“把离散动画选择连续化”的可选表现节点。
- Foot Placement与IK继续位于Pose Graph外，作者无法在同一拓扑中看到最终骨骼处理顺序。

GASP和UE的关键分工不是“所有动画强制进入Blend Stack”，而是状态机、Chooser或Motion Matching先决定选择；Blend Stack只在需要保存历史播放器和处理中断时把离散选择转换为连续姿势；AnimGraph再负责Layer、Additive、骨骼修改、IK与最终输出。本项目需要采用这条职责边界，同时保留BTSMTL作为唯一Gameplay逻辑和参数来源。

本change建立唯一正式链路：

```text
BTSMTL / Motion Matching
  -> Animation Selection + Presentation Parameters
  -> compiled Character Presentation Pose Graph
       -> Selection Input
       -> 可选 Marker Sync
       -> Selected Pose Player 或显式 Blend Stack
       -> Blend / Layered / Additive
       -> Modify Bone
       -> Foot Placement / IK phase
       -> Output Pose
  -> FinalAnimationPoseFrame
```

## What Changes

- 新增target-neutral`AnimationSelectionFrame`，只表达选择身份、source、采样时间、循环、播放倍率、generation与正式参数页，不携带transition、Bone Mask、IK或最终权重。
- `AnimationChannelId`继续是BTSMTL Program每通道唯一逻辑选择身份；删除作为隐藏Stack owner的`PoseSlotId`绑定，不再让Program或Presentation维持第二个同义入口身份。
- Pose Graph新增typed `AnimationSelection`与Parameter输入；`AnimationSelectionInput`显式绑定一个Program Animation Channel，`MotionMatchingSelectionInput`显式绑定一个MM producer output。
- Pose Graph新增显式`MarkerSync` Selection节点。Timeline只提交raw visual sample与编译后的marker binding；节点根据与其一对一配对Player发布的source usage解析raw-to-effective time，不采样Pose、不计算权重、不拥有source retention。
- Pose Graph新增`SelectedPosePlayer`。它消费一份Selection并只采样当前source，可用于明确硬切或由上游稳定控制的动画。
- Blend Stack改为显式Pose Graph节点。它消费Selection，唯一拥有该节点的多source历史、CrossFade、Stored Pose、Blend Profile、retention和transition clock，并输出普通Pose Value。惯性残差由`refactor-inertial-blending-to-local-pose-node`定义的局部`Inertialization`节点独占。
- StateMachine/Timeline选择与Motion Matching选择复用同一个Selection合同、Player节点集合和Pose Plan执行链；是否使用直接Player、局部Inertialization或Blend Stack只由显式图拓扑决定。MM不得创建私有播放器、私有crossfade或私有惯性器。
- Blend配置从“每Pose Slot完整matrix”迁移为具体Blend Stack节点引用的唯一Blend Policy。Compiler只为该节点可达选择物化exact transition table，Runtime不使用default fallback猜测缺失pair。
- Pose Graph Runtime节点扩展为`AnimationSelectionInput`、`MotionMatchingSelectionInput`、`MarkerSync`、`SelectedPosePlayer`、`BlendStack`、`Inertialization`、`BlendPose`、`LayeredBoneBlend`、`AdditivePose`、`PoseParameterResolve`、`ModifyBone`、`FootPlacement`与`OutputPose`。
- BTSMTL只决定Gameplay winner、Timeline raw visual time和表现参数；AnimationTrack继续唯一保存SyncGroup、Topology、SyncRole与Point Marker作者数据，但Timeline Runtime不得在图外解析effective time。BTSMTL不得保存Blend entry、历史权重、Bone Mask、IK plan或最终骨骼结果。
- Animancer只创建和采样source playable，并作为Unity PlayableGraph执行宿主；不得拥有业务selection、自动fade、Layer composition或最终拓扑。
- Pose Graph Compiler把单张authoring图编译为固定`CharacterPresentationPosePlan`，明确划分source/playback、native pose composition、world-aware pose post process和final publication阶段。
- Foot Placement作者节点复用现有Planner、PhysicsScene查询、Rig Calibration与Solver，不复制算法；它在编译计划中成为显式world-aware阶段，最终`FinalAnimationPoseFrame`只在IK/Solver完成后发布。
- Timeline Preview、MM Query Fixture、Live Debug和正式Runtime必须执行同一编译Pose Plan；图中未连接Blend Stack时不得在Preview或Runtime后台补建Stack。
- 删除固定per-slot Stack装配、`PoseSlotFrame`专属入口、隐藏Stack transition matrix、Timeline携带transition identity和所有旧路径兼容。
- 删除Pose Graph外自动运行的`AnimationMarkerSyncRuntime`装配、对隐藏Stack entry的扫描和Timeline预先写入effective time的路径；Marker Sync只在图中存在显式节点时生效。

## Impact

### Specs

- 新增`character-animation-selection-runtime`。
- 修改`character-animation-layer-runtime`。
- 修改`character-animation-pipeline`。
- 修改`character-animation-presentation-authoring`。
- 修改`character-presentation-interpolation`。
- 修改`character-foot-placement-presentation`。
- 修改`btsmtl-timeline-editor-preview`。
- 修改`character-pipeline-runtime`。
- 同步重基线active `character-presentation-pose-graph`与`character-motion-matching-presentation`能力。

### Active Changes

- `refactor-animation-playback-to-blend-stack`只保留Blend Stack的CrossFade、Stored Pose、多source history、Per-Bone Blend Profile与source lifetime实现；现有Inertial数学迁移给局部Inertialization节点，隐藏per-slot owner由本change删除，旧change不得独立归档。
- `refactor-inertial-blending-to-local-pose-node`建立`SelectedPosePlayer -> Inertialization`局部连续化路径，并删除Stack内Inertial technique；它与本change属于同一迁移序列。
- `add-character-presentation-pose-graph`改为完整显式表现图，删除固定`PoseSlotInput -> hidden Stack`前置假设；Corin资产迁移必须按新节点集重写。
- `add-character-motion-matching-pose-source`改为输出`AnimationSelectionFrame`和参数，不再输出带transition identity的`ResolvedAnimationPoseRequest`。
- `refactor-motion-matching-presentation-module`继续深化MM查询/选择Module，但Resolve输出改为Selection batch，Complete只消费匹配PoseGraph节点的正式结果。
- `refactor-timeline-animation-authoring-boundary`保持Timeline作者工具边界，只把Preview正式链改为Selection输入与编译Pose Plan，不复制播放器。

### Code

- Animation command、selection、sampling与lifecycle合同。
- Character Presentation Projection与Profile authoring。
- Pose Graph authoring、validator、compiler、program、runtime workspace与diagnostics。
- Blend Stack owner、policy、runtime node与source retirement。
- Animancer source sampling backend与PlayableGraph编排。
- Motion Matching Presentation Module与Timeline Preview。
- Foot Placement Planner/Solver调度和FinalAnimationPoseFrame发布边界。
- Corin Animation Presentation Profile、Pose Graph、Blend Policy、Rig Binding与generated Projection。

## Breaking Changes

- 删除`PoseSlotId`作为AnimationChannel到隐藏Stack的一对一绑定身份。
- 删除每个Pose Slot自动创建`AnimationBlendStackRuntime`的装配规则。
- 删除`PoseSlotInput`只读隐藏Stack输出的节点语义。
- 删除`ResolvedAnimationPoseRequest`同时承载source sample与transition identity的合同。
- 删除全局`CharacterAnimationBlendLibrary`按Pose Slot保存完整matrix的authoring模型。
- 删除`PoseSlotFrame`作为Pose Graph唯一输入类型；所有播放器节点统一输出普通Pose Value和source contribution。
- 删除Foot Placement位于Pose Graph外且早于最终帧发布的口径。
- 不提供旧Projection schema、旧PoseSlot payload、旧Blend Library、旧Preview链或runtime converter。

## Current Spec Comparison

- current `character-animation-layer-runtime`要求PoseSlot Blend Stack是transition权威。本change将其改为“显式播放节点拥有各自时间连续性”：直接播放器允许明确硬切，Blend Stack只对连接到它的选择负责。
- current `character-animation-layer-runtime`还要求Timeline与Lifecycle在Pose Graph前完成Marker handoff。本change保留Track marker作者数据和segment映射算法，但把运行时权威迁入显式`MarkerSync`节点；没有该节点时Player只使用raw visual time。
- current `character-animation-presentation-authoring`要求Blend Library按每Pose Slot保存transition。本change把authoring owner改为具体Blend Stack节点引用的Blend Policy，避免未使用Stack的分支仍被迫配置matrix。
- current `character-animation-pipeline`要求Timeline降低为source-neutral Pose Request。本change进一步拆为Selection与source sample，Timeline不再携带Stack transition identity。
- current `character-presentation-interpolation`按PoseSlot Stack描述表现时钟、重入与调试。本change把这些合同迁移到显式Player、PoseNodeId与Pose Plan completion。
- current `character-foot-placement-presentation`要求Foot Placement是Pose Graph之后的唯一Pose Post Process。本change保留Planner/Solver唯一性，但把该阶段纳入编译Pose Plan和authoring拓扑，最终帧改在Solver完成后发布。
- current `btsmtl-timeline-editor-preview`要求复用正式播放链。本change保留该要求，并明确Preview按图决定是硬切、经过局部Inertialization还是经过Blend Stack。
- active `add-character-presentation-pose-graph`和`refactor-animation-playback-to-blend-stack`与本目标直接矛盾，必须同步修改后才能继续实施或归档。

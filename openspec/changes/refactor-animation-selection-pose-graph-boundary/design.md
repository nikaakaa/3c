# Design: 动画选择驱动的显式 Presentation Pose Graph

## Context

当前链路把Selection、时间连续性和Pose Graph输入提前固定：

```text
AnimationChannel Selection
  -> PoseSlot binding
  -> hidden per-slot Blend Stack
  -> PoseSlotFrame
  -> limited Pose Graph
  -> FinalAnimationPoseFrame
  -> Foot Placement
```

这个结构保证所有producer共享Stack，却让作者无法表达“这条稳定选择直接播放”“这条状态机输出需要Blend Stack”“这条MM输出使用同一Stack算法”“这里先做Additive再做IK”。Pose Graph只剩下隐藏管线之后的空间合成器，和项目希望由表现图完整解释画面结果的目标不一致。

## Goals

- BTSMTL唯一回答Gameplay状态、Timeline时间、每AnimationChannel winner和表现参数。
- Motion Matching作为另一种Selection provider，不成为第二条播放链。
- Pose Graph显式表达Selection、Player、Blend Stack、空间混合、骨骼修改、IK与Output顺序。
- Blend Stack只解决离散选择频繁变化时的历史播放器与连续中断。
- 普通稳定动画可以明确使用直接播放器，不承担Stack workspace和matrix。
- Runtime只执行编译Plan，不解释authoring graph，不动态发现节点。
- Preview、Live Debug、MM Query Fixture和正式Runtime共享同一Plan与节点状态语义。
- Foot Placement Planner、world query与Solver保持唯一实现，同时在PoseGraph作者拓扑中可见。
- 迁移后删除PoseSlot隐藏owner、旧request混合合同与兼容数据。

## Non-Goals

- 不把BTSMTL Gameplay Graph复制成第二张Animation Blueprint Event Graph。
- 不让Pose Graph重新仲裁同一AnimationChannel的Gameplay winner。
- 不让Motion Matching搜索逻辑进入BTSMTL Timeline或普通Player节点。
- 不提供任意用户脚本节点、反射节点发现或Player中的动态图编辑。
- 不恢复Animancer FadeGroup、Animancer Layer、Animator.CrossFade或Timeline autonomous player。
- 不新增第二套Foot Placement Planner、地面查询或IK Solver。
- 不在本change扩展完整Control Rig生态；只安装当前角色纵切需要的稳定Bone Modify与Foot Placement节点边界。

## Terms

### Animation Selection

逻辑或搜索结果产生的离散播放目标：

```text
AnimationChannelId
ProgramProducerId / ProgramProducerIndex
AnimationPoseSourceId
SelectionGeneration
SampleTime / ContinuousTime / Cycle
RawVisualSampleTime
Loop
PlayRate
Source-local clip sample descriptor
Presentation parameter page
```

Selection不是Pose，也不是transition。Selection Input输出的时间是raw visual time；显式`MarkerSync`可以为Player source usage生成effective sample page，但不覆盖raw time。Selection不携带旧source、fade clock、Bone Mask、IK或最终weight。

### Pose Player

把Selection解析并采样为Pose Value的节点。`SelectedPosePlayer`只保留当前source；`BlendStack`保留历史source并执行transition。

### Pose Value

节点间统一传递的表现值，包含Availability、dense local TRS、Pose Parameter、source contribution、foot feature与continuity identity。它不暴露Animancer state或authoring object。

### Character Presentation Pose Plan

Pose Graph Compiler输出的不可变执行计划。它包含节点拓扑、typed port layout、source/player实例、native workspace、world-aware postprocess阶段、final publication和固定diagnostics source map。

## Decision 1: Gameplay只输出Selection与参数

BTSMTL Program Finalize继续为每个`AnimationChannelId`提交至多一个winner。Timeline sampling只根据committed逻辑时间与表现插值形成Selection的raw visual sample字段，并附带Projection中已编译的Marker binding identity。BTSMTL可以决定Speed、Direction、AimWeight、ActionWeight、FootPlacementWeight等参数，但不得解析Marker effective time、决定Pose Graph节点连接或保存表现历史。

Motion Matching Module消费trajectory、history和数据库后输出相同Selection合同。它选择AnimationClip、起始时间、generation和source-local sample，不直接产生最终Pose Request或私有播放器。

### Tradeoff

- 收益：StateMachine、Timeline和MM只替换选择算法，播放与最终表现拓扑保持独立。
- 代价：需要把当前`ResolvedAnimationPoseRequest`拆成Selection、source sample和player state三层合同。

## Decision 2: 删除PoseSlot隐藏owner

`AnimationChannelId`已经足够表达Program输出身份。Pose Graph通过`AnimationSelectionInput`显式引用channel，不再要求中间`PoseSlotId`一对一映射。Selection Input节点自身拥有稳定`PoseNodeId`和`RequireSelection | AllowEmpty`策略。

同一channel可以被多个只读Selection Input引用，但Compiler只创建一份frame selection cache；每个下游Player拥有独立播放状态。作者若重复创建两个Blend Stack，就是明确创建两个表现历史，不是Runtime偷偷复制。

### Tradeoff

- 收益：删除同义identity和隐藏装配，图上入口与逻辑channel直接对应。
- 代价：现有Projection、diagnostics、Preview和Corin资产中的PoseSlotId必须一次性迁移删除。

## Decision 3: Marker Sync是显式Selection节点

`MarkerSync`位于Selection Input与一个stateful Player之间：

```text
AnimationSelectionInput
  -> MarkerSync
  -> SelectedPosePlayer 或 BlendStack
```

它唯一拥有该节点的marker relation、leader/follower解析、raw-to-effective time映射与continuation anchor。SyncGroupId、Finite/Cyclic topology、SyncRole和Point Marker仍由各AnimationTrack唯一保存并编入Projection；Pose Graph节点不复制这些作者数据，只通过稳定binding读取。

每个`MarkerSync`输出 MUST精确连接一个`SelectedPosePlayer`或`BlendStack`，Compiler据此建立一对一的`PlayerSourceUsage`合同。usage显式区分`Sample`、`HandoffReference`与`Release`：Player先声明本帧需要采样的source以及只用于切换参照的source；MarkerSync只对这些正式usage解析effective sample page；Player再按该page采样`Sample` source。`SelectedPosePlayer`在selection identity变化的边界帧声明旧source为一次性`HandoffReference`、新source为`Sample`，完成映射后立即release旧source，不保留旧Pose；`BlendStack`把当前与尚未exact release的历史source都声明为`Sample`。MarkerSync不得扫描Stack entry、读取权重、延长source寿命或决定release。

节点继续采用当前项目的有向handoff语义：默认outgoing领导incoming；incoming为`AlwaysLeader`或outgoing为`AlwaysFollower`时反向；同组且marker pair完整时映射segment fraction。SelectedPosePlayer的一次性`HandoffReference`足以完成切换边界映射；BlendStack的多个`Sample` usage允许relation在CrossFade共同可见期持续更新。`None`、不同SyncGroup或没有合法handoff pair时明确记录NotApplicable并保持raw time。角色冲突、缺失segment或损坏Projection必须返回typed Invalid，不得退回normalized time。

这不是UE完整的多播放器动态Sync Group：本change不按blend weight选leader，也不让任意Pose分支自动入组。它只把项目已有的同一Selection流handoff同步改成可见、可编译、可关闭的节点。未来若要支持BlendSpace/Montage式多输入组，需要独立扩展节点输入与leader policy，不能借当前节点名称暗中扩大语义。

### Tradeoff

- 收益：图上能直接看到“这条Selection是否需要Marker Sync”；Timeline、Lifecycle和BlendStack都不再暗中改采样时间。
- 收益：MarkerSync与BlendStack分工稳定——前者只算时间，后者只算source保留和混合权重。
- 代价：Compiler需要为MarkerSync与其唯一Player生成source-usage预阶段，Player执行从一次求值拆成membership、time resolve、source sample三个固定步骤。
- 代价：同一个Selection若进入两个Player，必须显式创建两个MarkerSync节点；两个Player不会共享隐藏relation状态。

## Decision 4: Blend Stack是显式Player节点

`BlendStack`输入为Animation Selection，输出为Pose Value。节点唯一拥有：

- active animation players与稳定push order；
- CrossFade transition；
- per-entry clock与per-bone Blend Profile；
- Stored Pose与容量压缩；
- source retention与exact release；
- node-local diagnostics与continuity。

节点不拥有Gameplay winner、Motion Matching query、Layered Bone Mask、Additive、Foot Placement或Output Pose。

`SelectedPosePlayer`使用同一source backend，但只保存当前Selection。Selection identity变化时输出新Pose与typed discontinuity事实；没有下游`Inertialization`时按图定义执行硬切。Compiler和Runtime不得自动插入Stack、Inertialization或fade。

局部`Inertialization`节点由`refactor-inertial-blending-to-local-pose-node`定义：它消费直接Player的Pose与discontinuity，只保存单Pose输出历史、速度残差与衰减clock，不保留旧source。Blend Stack不得同时执行同一Inertial算法。

### Tradeoff

- 收益：Blend Stack只在真正需要动态中断的分支付费，作者能从图上看到连续性边界。
- 代价：作者必须明确选择Player类型；错误选择直接Player会产生可见硬切，而不是被后台Stack掩盖。

## Decision 5: Blend Policy属于具体Stack节点

每个Blend Stack节点引用唯一`CharacterAnimationBlendPolicy`：

```text
MaxActiveSources
StoredPose policy
Default authored rule
exact source-target overrides
canonical curves
Blend Profiles
```

默认规则只用于Editor编译物化。Compiler必须枚举该节点所有可达Selection endpoint并生成完整exact table；Runtime缺少pair必须失败。其它Stack节点可以引用同一Policy资产复用配置，也可以引用不同Policy，但不得复制为Timeline字段或Program参数页。

参数输入可以在编译允许的字段上覆盖节点标量，例如动态Blend Time；override来源和范围必须由节点typed input明确声明，不能让Runtime按GameplayTag或动画名称猜测。

### Tradeoff

- 收益：配置靠近实际使用Stack的图节点，同时保留完整exact runtime table和可复用资产。
- 代价：不再存在一张覆盖所有Pose Slot的中心matrix；跨节点一致性由共享Policy和Compiler diagnostics保证。

## Decision 6: Pose Graph安装完整表现节点集

正式节点分为四组。

### Selection与Player

- `AnimationSelectionInput`
- `MotionMatchingSelectionInput`
- `MarkerSync`
- `SelectedPosePlayer`
- `BlendStack`
- `Inertialization`

### Pose Composition

- `BlendPose`
- `LayeredBoneBlend`
- `AdditivePose`
- `PoseParameterResolve`
- `PoseSubgraph`

### Procedural Pose

- `ModifyBone`
- `FootPlacement`

### Boundary

- compiler-only `GraphInput` / `GraphOutput`
- root `OutputPose`

`BlendPose`处理两个已知Pose的普通标量混合；`LayeredBoneBlend`处理Bone Mask覆盖；`AdditivePose`处理相对Rig Reference的delta；`PoseParameterResolve`只处理参数合并；`ModifyBone`执行受Rig BoneId约束的有限local/mesh-space修正；`FootPlacement`声明唯一world-aware Planner/Solver阶段。

## Decision 7: Program Parameter使用typed input进入图

Pose Parameter不再只能附着在某个Pose分支上间接传播。Pose Graph声明稳定ParameterId、类型、默认值和允许来源，`ProgramParameterInput`从Projection绑定的committed presentation parameter page读取。Blend、Additive、ModifyBone和FootPlacement权重必须通过typed edge或显式常量提供。

Pose Value仍可携带source-local curve参数，`PoseParameterResolve`负责把source-local参数和Program参数按`Base | Overlay | Weighted | Max | Min`合成为下游值。

### Tradeoff

- 收益：作者能在图上追踪一个权重来自BTSMTL参数、动画曲线还是常量。
- 代价：Parameter layout、端口类型和Projection binding需要版本化升级。

## Decision 8: Pose Plan采用显式分阶段执行

单张作者图由Compiler分区为：

```text
Phase A Selection
  committed channel/MM raw selection + parameters

Phase B Source Membership and Time Resolve
  Player声明source usage -> 显式MarkerSync解析effective sample page

Phase C Source and Native Pose
  source sample -> Player/BlendStack或Inertialization -> Blend/Layered/Additive/ModifyBone
  -> ComposedAnimationPoseFrame

Phase D World-Aware Pose Post Process
  FootPlacement Planner
  -> PhysicsScene queries
  -> CharacterFootPlacementPlan
  -> configured IK Solver

Phase E Final Publication
  OutputPose
  -> FinalAnimationPoseFrame
  -> Camera
```

`FootPlacement`在作者图中是显式节点，但Compiler把它降低到Phase D，不要求Unity Animation Job内部执行PhysicsScene查询。现有Planner、query workspace和Solver继续唯一存在；Runtime scheduler按编译Plan只执行一次该节点，不从图外另行追加默认Foot Placement。

没有`FootPlacement`节点的图不得由Profile或Prefab自动补建它。启用该节点但缺少Profile、Rig、Calibration、PhysicsScene或Solver时构建或Runtime创建失败。

### Tradeoff

- 收益：作者看到完整最终表现顺序，同时保持Unity Physics与Animation Job的真实执行约束。
- 代价：Pose Graph不再等同于单个native job DAG，Compiler和diagnostics必须表达阶段边界与中间completion。

## Decision 9: Preview与Runtime执行同一Plan

Timeline Preview把预览时间降低为正式raw Animation Selection，MM Query Fixture输出正式raw MM Selection；二者都创建匹配Projection的Pose Plan实例。图中存在MarkerSync才显示effective time、leader/follower与segment；没有MarkerSync时必须使用raw time。图中只使用直接Player就硬切，连接局部Inertialization就复用正式history、residual与rebase，连接Blend Stack就保留多source历史；使用FootPlacement但缺少正式Body/Physics上下文则明确显示该节点不可执行，不能伪造平面或静默跳过后仍声称是Final Pose。

纯动画Preview可以停在`ComposedAnimationPoseFrame`并把Phase D标记为Unavailable；Live Debug与Play Mode必须发布Phase E完成的FinalAnimationPoseFrame。

## Decision 10: Animancer只是Source Backend与执行宿主

Animancer按完整source identity创建Sequence/ManualMixer playable、应用sample time和source-local clip weights，并向Player节点提供source pose capture。Animancer不读取Blend Policy、不决定entry weight、不做AnimationChannel winner选择、不保存PoseGraph topology，也不在Output外执行第二套Layer/Fade。

## Migration

迁移必须由本change统一编排：

1. 建立Selection与typed Parameter合同。
2. 扩展Pose Graph port、node、validator与compiler schema。
3. 将现有Blend Stack中的CrossFade、Stored Pose、多source retention与release封装为显式runtime node实例，并把Inertial数学整体迁移到局部Inertialization节点。
4. 建立SelectedPosePlayer并复用唯一Animancer source backend。
5. 把Timeline与MM降低为raw Selection provider，并将既有Marker relation算法迁入显式MarkerSync节点。
6. 建立Player source-usage预阶段和Marker effective sample page，删除图外Marker Sync装配与Stack entry扫描。
7. 把现有Layered/Additive/Parameter算法迁入新Pose Value布局。
8. 将Foot Placement注册为编译Plan的world-aware节点并迁移final publication顺序。
9. 迁移Preview、Live Debug、Replay与diagnostics source map。
10. 重建Corin图、MarkerSync、Blend Policy、Rig Binding与Projection。
11. 删除PoseSlotId、固定Stack数组、PoseSlotFrame、旧Blend Library和混合request合同。

任何阶段若需要旧PoseSlot链和新Selection图同时运行，必须停止并调整提交范围；正式结果不允许双写、fallback或converter。

## Corin Target Graph

```text
AnimationSelectionInput(BaseLocomotion)
  -> MarkerSync
  -> SelectedPosePlayer
  -> Inertialization(LocomotionInertialPolicy)
  -> BasePose

AnimationSelectionInput(FullBodyAction)
  -> BlendStack(ActionPolicy)
  -> ActionPose

BasePose + ActionPose
  -> LayeredBoneBlend(FullBodyMask, ActionWeight)
  -> PoseParameterResolve
  -> ModifyBone(optional authored corrections)
  -> FootPlacement
  -> OutputPose
```

Corin当前仍使用BTSMTL StateMachine/Timeline选择；未来启用MM时，只把BaseLocomotion Selection provider替换为`MotionMatchingSelectionInput`，下游SelectedPosePlayer、Inertialization与完整图无需复制。

## Rejected Alternatives

### 保留隐藏per-slot Stack，只在Editor显示摘要

能减少Runtime改动，但作者仍不能决定直接Player或Stack，Pose Graph也不能解释完整表现链。拒绝。

### 所有Selection都直接进入Pose Graph普通Blend

简单，但MM和连续中断必须在上游重建历史播放器，最终产生多个私有transition实现。拒绝。

### Timeline在进入Pose Graph前固定完成Marker Sync

能复用当前代码，但Timeline看不到最终由哪个Player保留source，也只能扫描隐藏Stack状态；图中删除或增加BlendStack都不会改变同步行为，作者无法从拓扑解释effective time。拒绝。

### MarkerSync作为Pose混合节点放在Player之后

节点外观直观，但Pose已按错误时间完成采样，除非节点再次采样source或侵入BlendStack权重，两种做法都会复制Player职责。MarkerSync必须位于Selection层，并由Compiler安排在source sample之前。拒绝。

### Motion Matching内部私有Stack，BTSMTL使用另一套Stack

接近UE内部类层次，但项目会出现两套retention、Stored Pose和diagnostics。两种Selection provider必须复用同一runtime node实现。拒绝。

### Foot Placement继续作为图外固定Pass

实现已经存在，但作者无法看到IK顺序，OutputPose也不再代表最终骨骼结果。改为图中显式、编译后分阶段执行。拒绝。

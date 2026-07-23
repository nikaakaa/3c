# Implementation Inventory

## 迁移前旧身份与Projection

- `PoseSlotId`、`CharacterPoseSlotDeclaration`与`PoseSlotOutputPolicy`定义在`CharacterPoseAuthoringContracts.cs`。
- Profile、Pose Graph、Projection、Binding Index、Pose Program、Blend payload、Lifecycle、Blend Stack、native workspace和diagnostics都保存或索引`PoseSlotId`。
- `CharacterAnimationPresentationProfile`保存全局`CharacterAnimationBlendLibrary`；Projection保存与Pose Slot一一对应的`AnimationBlendSlotPayload[]`。
- Corin现有`CorinAnimationPresentationProfile.asset`仍是更早的Layer/TransitionLibrary序列化结构，generated Projection也不是本change目标schema。

## 迁移前隐藏Stack链

- `AnimationPosePlayableGraphRuntime`按Binding Index的Pose Slot列表自动创建`AnimationBlendStackRuntime`。
- `CharacterAnimationPlaybackRuntime.Present`先解析全部`ResolvedAnimationPoseRequest`，再由`AnimationPlaybackLifecycle`按channel-to-slot绑定向固定Stack执行Push/Empty/Release。
- `AnimationPosePlayableGraphRuntime.Advance/Evaluate/Reset/Dispose`统一遍历固定Stack数组；Pose Graph只读取Stack产生的`PoseSlotFrame`。
- 迁移前`AnimationBlendStackRuntime`及其state、source workspace、frame plan和native job同时拥有entry、clock、CrossFade、Stored Pose、Inertial、retention与release；当前实现已删除该Inertial owner。

## 迁移前混合Request合同

- `ResolvedAnimationPoseRequest`同时保存playback/source/generation、Pose Slot、sample时间、loop/cycle/play rate、Timeline clip sample、transition rule、Foot Feature和参数页引用。
- Timeline入口是`TimelineAnimationPoseRequestResolver`；MM入口是`MotionMatchingResolvedPoseRequestFactory`与`CharacterMotionMatchingPresentationModule`。
- command queue、Lifecycle、Animancer backend、Blend Stack与trace都直接消费该混合合同。

## 迁移前Pose Graph

- authoring节点只有`PoseSlotInput`、`LayeredBoneBlend`、`AdditivePose`、`PoseCurveResolve`、`PoseSubgraph`、`OutputPose`及子图边界。
- compiled operation只有`PoseSlotInput`、`LayeredBoneBlend`、`AdditivePose`、`PoseCurveResolve`和`OutputPose`。
- `CharacterPoseGraphNativeJob`消费固定Slot capture，执行Layered/Additive/Parameter合并并写最终stream。
- native workspace按Slot、Pose Value、Parameter、Contribution和operation frame cache预分配。

## 迁移前Preview、MM、Replay与Diagnostics

- Timeline Preview通过`PreviewPlaybackEngine`进入正式Presentation runtime，但仍继承固定Pose Slot Stack装配。
- MM Query Fixture与Presentation Module最终仍生成`ResolvedAnimationPoseRequest`并按Pose Slot定位Stack。
- Rollback从相同Gameplay command重建旧request/lifecycle；没有独立播放器，但诊断身份仍以Pose Slot为主。
- Live Debug由`AnimationPresentationRuntimeSnapshot`、publisher和`CharacterAnimationTracePublisher`发布Slot、Stack与final pose事实。

## Foot Placement顺序

- `CharacterSimulationPresentationRuntime`在`CharacterAnimationPlaybackRuntime.Present`发布`FinalAnimationPoseFrame`后，从图外调用`CharacterFootPlacementRuntime.Present`。
- Planner、PhysicsScene query workspace和`ICharacterFootPlacementSolver`已经是唯一实现，应迁入Pose Plan的world-aware阶段而不复制算法。
- Camera在图外Foot Placement完成后推进；最终帧命名早于IK/Solver真实完成。

## 删除目标

- 删除`PoseSlotId`、`CharacterPoseSlotDeclaration`、channel-to-slot binding与所有序列化payload。
- 删除固定Stack数组、按Slot自动构造/Advance/Evaluate/Reset/Dispose和Empty push。
- 删除`PoseSlotFrame`专属合同、workspace命名与Graph输入语义。
- 删除`ResolvedAnimationPoseRequest`及Timeline/MM transition identity传递。
- 删除全局`CharacterAnimationBlendLibrary`与按Slot完整matrix。
- 删除图外Foot Placement自动Pass和旧Projection reader/schema。
- 不保留`FormerlySerializedAs`、converter、fallback或旧/新双写路径。

## Corin迁移范围

- 保留`BaseLocomotion`与`FullBodyAction`两个AnimationChannel身份。
- 新图目标为Base Selection Input -> Selected Pose Player -> Inertialization；Action Selection Input -> Blend Stack；再经Layered Bone Blend、Parameter Resolve、Foot Placement和唯一Output。
- Blend配置改为具体Blend Stack节点引用的Policy；Profile只装配Pose Graph、Rig、producer binding、MM与Foot Analysis。
- 所有Character prefab继续显式绑定正式Foot Placement Profile、Rig Calibration和Solver；generated Projection/Float32/Fixed wrapper只能由用户显式触发正式Build Request重建。

## 已安装结果

- 唯一链为`AnimationSelection -> MarkerSync可选 -> SelectedPosePlayer或BlendStack -> 局部Inertialization可选 -> Pose composition -> FootPlacement -> OutputPose`。
- BlendStack只拥有多source CrossFade、Stored Pose与exact release；Inertialization只拥有直接Player的单Pose history、residual、rebase与Accumulator。
- Corin BaseLocomotion使用`SelectedPosePlayer -> Inertialization`，FullBodyAction使用独立BlendStack；Projection同时发布Float32与Fixed目标。

# Corin正式配置字段与数值盘点

状态：规划阶段基线，来源为当前工作区资产；不作为第二份配置真相。

## 1. 根与运行装配

| 资产 | 当前字段/值 |
| --- | --- |
| `Pipeline/Definition/CorinCharacterPipelineDefinition.asset` | `ControlModuleId=character.corin.control`；`SimulationTickRate=60`；`EquipmentCapabilityEnabled=0`；装配InputProfile、GameplayEffectProfile、BodyMotionProfile、AnimationPresentationProfile、CameraProfile、SkillDefinitions、SkillGraphs、ActionProfiles、BehaviorProfiles、Float32/Generated Program和Projection |
| `Pipeline/Motion/Profiles/CorinBodyMotionProfile.asset` | `GravityAcceleration=-25`；`MaximumFallSpeed=40` |
| `Pipeline/Simulation/Compositions/CorinLocalSimulationSessionComposition.asset` | `SessionId=Corin.Local`；`WorldId=Local.World`；`MapId=Local.Map`；`WorldRevision=Local.World.v1`；`SourceClockId=Local.Logic`；`TickRate=60`；显式ProgramRuntime、ExecutionBackend、Pipeline、SessionSource、WorldSolver |

Definition下的Skill、Graph、Timeline、Presentation、Motion和Session引用必须继续从实际GUID/owner解析，不能用本表中的显示名替代。

## 2. Input、Skill和Gameplay

| 领域 | 正式资产/字段范围 |
| --- | --- |
| Input | `Pipeline/Input/CorinCharacterInputProfile.asset`；SourceAsset、BindingGroup、InputValues、ActionRequests；四个InputActionReference：Move、Look、Dodge、Fire |
| Skill | 三个 `CorinCharacterPipelineDefinition.Skill.*.asset`、四个SharedGraph、Attack/Dodge ActionProfile和ActionContextSlot、Skill Local Blackboard、Macro、Entry Graph、State/Condition、TreeClip |
| Gameplay | `CorinCharacterGameplayEffectProfile.asset`；TagCatalog、AttributeDefinitions、InitialAttributes、InitialTags、EffectDefinitions；当前独立资产包括DamageEffect、GameplayTagCatalog、HealthAttribute |
| Motion/Behavior | `CorinLocomotionMoveBehaviorProfile.asset`、`CorinMotionCorrectionAckBehaviorProfile.asset`；必须标出哪些是Skill读取的事实、哪些是Control/Pipeline配置 |

Skill Timeline字段必须逐项记录Animation Track/Clip、Animation Channel/Slot、播放模式、Play Rate、Blend In/Out、停止/取消、Hit/Recovery/Cancel窗口和Camera/Scene请求；不把这些字段写进Pose Graph或Runtime对象。

## 3. Presentation、Pose、Animation资源

| 资产 | 当前可见字段/值 |
| --- | --- |
| `Presentation/Profiles/CorinAnimationPresentationProfile.asset` | PoseGraph、RigDefinition、MotionMatchingProfile（当前为空）、FullBodyIkProfile、LinkedPose Groups/Implementations/Selectors、ProducerBindings、PoseSourceBindings、PoseResourceBindings、SourceResourceBindings、AnimationPropertyBindings、AnimationCompression、LocomotionSyncGroups、`FootPlacementAnalysisMode=1`、`FootPlacementAnalysisSourceAssetGuid=2487156154864bb5b49cb283c2583808` |
| `Presentation/Rig/CorinAnimationRigDefinition.asset` | `Schema=character-animation-rig/v4`；`RigId=corin.animation-rig`；`Revision=87731f0d0ca14844bbf861368b9847b3`；RootBonePolicy、ScalePolicy、SolverRootBoneId、PelvisBoneId、Spine/Arm/Leg/Head BoneIds、AnimationSlots、BlendProfiles |
| `Presentation/Sources/Actions/*.asset` | Attack1～Attack5、DodgeBack、DodgeForward Animation Source；每个Source的Slot、Binding、Clip、Revision和来源Dump identity必须逐项记录 |
| `Presentation/Blend/Action/CorinActionBlendPolicy.asset` | `Schema=character-animation-blend-policy/v3`；`MaxActiveSourceEntries=4`；`StoredPosePolicy=2`；`MaxBlendInTimeToReplaceNewest=0.05`；`DepthBlendTimeMultiplier=1`；默认/Override Transition：`BlendLogic=2`、`DurationSeconds=0.16`、`BlendMode=4`、CustomCurve、BlendProfile |
| `Presentation/Blend/Action/CorinActionBlendProfile.asset` | `Schema=character-animation-blend-profile/v2`；`ProfileId=corin.animation-rig.action-blend-profile`；RigId/Revision；`GlobalDurationMultiplier=1`；BoneOverrides |
| `Presentation/Blend/Locomotion/CorinLocomotionBlendProfile.asset` | `Schema=character-animation-blend-profile/v2`；`ProfileId=corin.animation-rig.locomotion-blend-profile`；RigId/Revision；`GlobalDurationMultiplier=1`；BoneOverrides |
| `Presentation/Blend/Locomotion/CorinPoseInertializationPolicy.asset` | `Schema=character-pose-inertialization-policy/v4`；PolicyId/Revision；ParameterFilters及其Mode；DirectPlayerRule；references |

Animation/Pose Transition必须逐条记录：源/目标owner、EndpointKind、BlendLogic、DurationSeconds、BlendMode、CustomBlendCurve、BlendProfile、Slot/Layer/Mask、Inertialization Policy、SourceSlot和AnimationChannel。不能只记录“有BlendPolicy”。

## 4. Foot、IK与Pose数值

| 资产 | 当前可见数值 |
| --- | --- |
| `Presentation/FootPlacement/CorinFootPlacementProfile.asset` | CurrentSupportQuery：LayerMask `4608`、HitCapacity `16`、CastAbove/Below `0.5/0.5`、MaxSlope `55`；LandingPrediction：HitCapacity `16`、SphereRadius `0.08`、Cast `0.35/0.75`、MaxSlope `55`、MaxPrediction `2`、VelocityDelta `0.001`、SmoothSpeed `60`、MaxSpeed `8`、InputDistance `0.05`、ComponentUpAngle `1`；GroundDetection：SegmentCapacity `16`、ContactCapacity `64`、CapsuleRadius `0.1`、MaxAxisSegment `0.18`、Cast `0.45/0.85`、MaxReachableEdge `0.3` |
| FootMotion同一Profile | LandingAcceptance `0.02`；PathRevision `0.02`；SwingResidual `0.02`；ReleaseCompletion `0.02`；CorrectionHalfLife `0.03`；TargetHeightMode `1`；MaxVerticalTargetSpeed `0.6`；ForceRefreshDistance `0.3`；CorrectionIncrease/Decrease `1.8/1.5`；MaxDirectionChange `10`；GroundPenetration `0.01`；LandingLockCompletion `0.01`；EnableLockFoot `0`；LockHeight/Speed/Range `0.1/0.1/0.2`；Damping/Stiffness `0.6/0.2`；CrossCheck `0`；LockDistance `0.08`；SlideDistance `0.2`；PelvisSpringFrequency `3`；PelvisSameLevelTolerance `0.01`；PelvisMaxDownVelocity `0.6`；MinimumLandingLegCompressionReserve `0.02` |
| `Presentation/FootPlacement/CorinFootPlacementAnalysisSource.asset` | AnalysisSourceId `Corin.FootPlacementAnalysis`；AnalysisVersion `1`；SamplingRigAssetGuid；RigDefinition/RigCalibration；CalibrationPreviewNormalizedTime `0`；SampleRate `60`；Thresholds、Reduction、MotionRootBoneId、MotionReferences |
| `Presentation/IK/CorinFullBodyIkProfile.asset` | `Schema=character-full-body-ik-profile/v1`；Iterations `4`；FabrikPass `1`；SpineStiffness `1`；PullBodyVertical/Horizontal `0/0`；NodeWeight `1`；四肢分别记录 Pin、Pull、Push、PushParent、Reach、ReachSmoothing、PushSmoothing、MappingWeight、MaintainRotationWeight、BendConstraintWeight、BendClamp（当前BendClamp `0.505`） |

## 5. Camera

| 资产 | 当前字段/值 |
| --- | --- |
| `Presentation/Camera/CorinCharacterCameraProfile.asset` | `Schema=character-camera-profile/v1`；ProfileId `corin.camera.profile`；DefaultSequence；Zooms `18`项；Stretches `18`项；Curves `3`项；DefaultSphere Height/Radius `0.225/3.75`；DefaultOrbit三点 `(2.225,2.5)`、`(0.225,3.75)`、`(-0.775,2.2)`；Near/Far `0.1/2000`；LocateRadius `3.75`；Elevation `3.4336302`；FOV `50`；Smooth `0.15`；Rotation/AvatarTransition `3/0.3`；Input Sensitivity `(0.12,0.0025)`、Pitch `(-70,70)`、Response `1/1/1`；Lock默认关闭、Transition `0.2`、Weight `1`；Collision默认关闭、LayerMask `-1`、Radius `0.2`、NearClip `0.05`、Smooth `0.08`、TimeDomain `1` |
| `Presentation/Camera/CorinCameraDefaultSequence.asset` | `Schema=character-camera-sequence/v1`；SequenceId `corin.camera.default.normal`；Default Stage；PlayLength `-1`；三组Orbit `(2.225,2.5)`、`(0.225,3.75)`、`(-0.775,2.2)`；ScreenOffsets `(0,0.35)`、`(0,0.5)`、`(0,0.5)`；Aspect `1.7777778`；FOV `50`；ElevationRatio `0.5`；PolarAngle `0`；TimeDomain `1` |
| Camera Curve | Default Curve 01/02/04：`Schema=character-camera-curve/v1`；TimeDomain `1`；Range `0..1`；Unit `normalized`；Pre/PostWrap `2`；实际曲线keys和引用必须由Dump/Camera资产索引逐项记录 |

## 6. Replay与生成产物

- ReplayRequest必须绑定SourceManifestHash、AuthoringClosureHash、DocumentHash、Program/Layout/ProjectionHash、SessionCompositionHash、Prefab/Scene、FixedInputTraceHash、CameraInputTraceHash、初始Actor/World状态和运行版本。
- Generated Program、Projection、Fixed wrapper、Timeline Program、Runtime Dump和Compare只记录依赖与hash，不作为作者配置字段复制回Definition。
- 当前任何一项数值、曲线、Transition、Camera参数变更，都必须从其正式owner重新Build并重新Replay；不能复用旧Dump或其他worktree产物。

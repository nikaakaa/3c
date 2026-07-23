# Design: 显式Animation Blend Stack节点算法内核

## 重新基线

`refactor-animation-selection-pose-graph-boundary`已经替换本文原先的隐藏owner和管线边界。显式Pose Graph `BlendStack`节点只保留entry、clock、CrossFade、Stored Pose、Per-Bone Blend Profile、source capture与release；Inertial数学、history与rebase属于`refactor-inertial-blending-to-local-pose-node`定义的局部`Inertialization`节点。本文中的per-slot runtime、`ResolvedAnimationPoseRequest`、`PoseSlotFrame`、Stack Inertial和全局Blend Library均为迁移前清单，不再是当前合同。

最终链路是：

```text
AnimationSelectionFrame
  -> SelectedPosePlayer
       -> 可选局部Inertialization
     或显式BlendStack
  -> 普通Pose Value
  -> Pose Graph composition / ModifyBone / FootPlacement
  -> FinalAnimationPoseFrame
```

没有显式Blend Stack节点的分支不创建历史播放器；每个显式节点拥有独立算法状态和node-local Blend Policy。

## Context

迁移前代码安装的是以下动画路径：

```text
CharacterAnimationPlaybackRuntime.Present
  -> committed AnimationChannel selection
  -> AnimationPlaybackLifecycle采样需求与Marker Sync
  -> ResolvedAnimationPoseRequest
  -> AnimationBlendStackRuntime.Advance / Push
  -> AnimationPosePlayableGraphRuntime.Evaluate
  -> source capture
  -> AnimationSlotBlendJob
  -> CharacterPoseGraphNativeJob
  -> AnimationFinalPoseStreamWriterJob
  -> Animancer PlayableGraph单次Evaluate
  -> FinalAnimationPoseFrame
  -> Foot Placement
```

`AnimationPosePlayableGraphRuntime`已经唯一拥有per-slot Stack、source backend、Native workspace、Slot Job、Pose Graph Job和final writer的装配顺序。Animancer只提供同一PlayableGraph与source playable采样，不再拥有transition clock、source间weight、跨slot合成或最终Pose决策。

历史问题是Lifecycle寿命与Animancer fade权重并存，且早期Blend Stack设计又试图同时承担时间历史和跨slot空间合成。最终职责已经固定为：

- Blend Stack回答“同一个Pose Slot的旧source如何连续过渡到新source”。
- Pose Graph回答“多个Pose Slot如何按骨骼与参数组合成最终pose”。

本design现在记录已安装实现和剩余配置闭环，不再把旧Animancer FadeGroup链描述为当前Runtime。

## Goals

- 每个显式Blend Stack节点拥有稳定、有序、可诊断的时间历史。
- 每个entry拥有独立Fade Clock、Curve、depth-adjusted duration和每骨骼transition weight。
- 在固定active source预算下通过Stored Pose保持pose、velocity、parameter和feature连续。
- Blend Stack只提供CrossFade；现有Inertial实现完整迁移到局部Inertialization节点。
- 让Animancer继续采样Clip/ManualMixer，不再拥有fade和source间权重。
- 输出统一Pose Value供下游Pose节点消费。
- Marker Sync、Preview、Rollback replacement和source retirement只读取同一显式节点事实。
- 以source-neutral resolved request承接未来Motion Matching。
- 所有Runtime内存按Rig、Slot和容量预分配。

## Non-Goals

- 不实现跨Pose Slot Bone Mask、Override/Additive、composition order或最终Animator写回。
- 不实现Pose Graph authoring、Pose Parameter最终resolve或Linked Anim Layer。
- 不实现Pose Search、Motion Matching database、candidate cost或search throttle。
- 不实现动画重定向。
- 不从AnimationClip root motion修改Gameplay Body。
- 不把Notify、Window、Cue或Gameplay事件交给动画播放器。
- 不保留Animancer FadeGroup、旧Layer compositor或TransitionLibrary fallback。

## Installed Architecture

```text
Committed AnimationChannel selection
              |
              v
AnimationPlaybackLifecycle
  selected / pending / retention / marker coordination
              |
              v
ResolvedAnimationPoseRequest
  AnimationChannelId + PoseSlotId + AnimationPoseSourceId + resolved samples
              |
       +------+------+
       |             |
       v             v
AnimationBlendStackRuntime    AnimancerPoseSamplingBackend
  entries / clocks / capacity Clip / ManualMixer source pose
  stored / inertial / retirement     no fade / no layer composition
  owns SourceWorkspace + double-page SlotWorkspace / FramePlan
  no managed evaluator / no second Evaluate
       |                               v
       +--------------------> Source Pose Capture (same completion)
                                       |
                                       v
                         PrepareSlotJob(...)
                                       |
                                       v
                         AnimationSlotBlendJob
                           CrossFade / Stored / Inertial
                                       |
                                       v
                                  PoseSlotFrame
                                       |
                                       v
                           CharacterPoseGraphNativeJob
                                       |
                                       v
 AnimationFinalPoseStreamWriterJob -> CompleteFrame(...) -> Foot Placement
```

Stack Runtime唯一拥有Source Workspace、双页Slot Workspace/FramePlan和entry状态；`PrepareSlotJob(...)`只从inactive page提交不可变plan并返回`AnimationSlotBlendJob`。Source capture、Slot Job和Pose Graph在同一非零completion、同一个PlayableGraph Evaluate内按依赖顺序执行，Evaluate成功后Runtime才调用`CompleteFrame(...)`提交release与retirement。不存在managed evaluator、第二次Evaluate、Animancer state weight或Lifecycle集合重建第二份计划。

## Identity

### AnimationPlaybackId

producer一次activation的完整逻辑/表现identity，拥有Timeline sample、Marker relation和PresentationRetention。

### AnimationPoseSourceId

一次可独立采样的姿势来源identity，由`AnimationPlaybackId + AnimationPoseSourceKind + AnimationPoseSelectionGeneration`构成。Timeline一次activation使用稳定generation；Motion Matching Continue保持generation，Jump提升generation。同一Playback的不同generation必须能同时作为旧、新source存活。

### AnimationBlendEntryId

一次表现blend request identity：

```text
AnimationBlendEntryId
  PoseSlotId
  AnimationPoseSourceId
  PresentationRequestSequence
```

同SourceId连续sample只更新source；同SourceId在另一个target之后重新成为target时创建新EntryId与独立clock；同Playback不同generation一定是不同source。

### Live Source Entry

引用ResolvedAnimationPoseRequest与Animancer source visual，拥有push order、transition rule和clock。

### Stored Pose Entry

不引用AnimationPoseSourceId。保存capture边界的slot local pose、pose velocity、Pose Parameter aggregate与per-foot feature aggregate，只作为连续性来源。

### Inertial Accumulator

每slot最多一个，保存当前slot result相对新target的position、rotation、scale、parameter与velocity residual，不是animation source或graph node。

## Authoring

### CharacterAnimationRigDefinition

Rig Definition保存RigId、revision、父节点优先稳定BoneId、ParentIndex、root exclusion、scale policy与左右脚语义BoneId。Prefab`CharacterAnimationRigBinding`按dense顺序显式绑定Transform。Runtime不得按名称、path、Humanoid或层级搜索补全。

### CharacterAnimationBlendProfile

Blend Profile保存匹配Rig identity、global duration multiplier和按BoneId override。Compiler展开dense positive multiplier；未知/重复BoneId、非正数或Rig不匹配均失败。

### CharacterAnimationBlendLibrary

Library按PoseSlotId保存：

```text
PoseSlotStackPolicy
  MaxActiveSourceEntries >= 2
  MaxBlendInTimeToReplaceNewest >= 0
  DepthBlendTimeMultiplier > 0

DefaultTransitionRule
  CrossFade | Inertial
  Duration
  CanonicalCurve
  BlendProfile

SourceTargetOverrides[]
```

Compiler按该slot绑定的AnimationChannel producer枚举全部source/target/Empty组合，将default与override物化成exact matrix。Runtime不知道某项来自default还是override；缺失、重复、跨slot或orphan pair均失败。

### Transition Curve

Curve规范化为`0..1 -> 0..1`，首尾必须为0/1，time严格递增，value单调且有限。Runtime使用唯一`AnimationBlendCurveEvaluator`，不调用Animancer easing。

## ResolvedAnimationPoseRequest

Lifecycle在Marker Sync与Timeline membership解析后生成：

```text
AnimationChannelId
PoseSlotId
AnimationPoseSourceId
PresentationRequestSequence
ProgramProducerIndex
resolved visual time / cycle / scale
ClipSamplePlan列表
exact transition source/target identity
Pose Parameter samples
Foot Analysis samples
```

它不包含State、Action、priority、Graph edge、Pose Graph node、Bone Mask或最终weight。未来Motion Matching必须通过正式adapter产生同一request。

## Entry Lifecycle

### First Sample Push

1. Lifecycle完成Marker Sync并创建request。
2. Stack exact lookup同slot transition matrix。
3. Animancer backend准备完整AnimationPoseSourceId source visual与capture binding。
4. Stack按replace/capacity policy决定普通push或capture。
5. Stack原子创建EntryId、clock和frame plan。

任一步失败时该slot进入typed Invalid，不留下半创建source或半切换entry。

`RequireOutput` Slot的`Empty -> producer` exact transition必须在Compiler阶段被强制为零时长。Lifecycle在`PendingFirstSample`期间不得先启动transition；首个合法source pose完成准备后，Stack在同一原子提交中以完整权重初始化该entry。Runtime不得新增`Uninitialized`混合状态、临时改写duration，或使用bind pose、默认Idle和上一帧残留姿势填补首帧。

### Continuous Update

同AnimationPoseSourceId后续sample更新仍引用该source的entry，不增加entry、不重启clock。Timeline producer内部多clip membership继续由一个ManualMixer表达。

### Re-selection

同SourceId在另一个target之后重新成为target时创建新EntryId。多个entry可以引用同一source capture结果；Evaluator按BoneId合并权重，不重复采样。同Playback不同SelectionGeneration不得复用capture结果。

### Retirement

entry每根骨骼贡献归零、且不再被capture/relation/selection引用后才退出。source只有在没有entry、relation、pending和retention引用后退役。

## CrossFade

每entry保存elapsed、base duration、curve、Blend Profile和push depth。每骨骼duration为：

```text
baseDuration * boneDurationMultiplier * depthMultiplier
```

Evaluator从最新到最旧按nested residual计算weight，并保证每骨骼live/Stored contribution规范化。AllowEmpty使用透明NoPose entry消耗slot output weight，不创建bind pose。

零duration仍通过一次原子frame plan完成，不允许中途source未准备却先切weight。

## Capacity and Stored Pose

每slot容量至少2。新push超过容量或命中快速替换阈值时，在切换边界捕获当前完整PoseSlotFrame核心数据：

- dense local pose。
- current/previous pose velocity。
- Pose Parameter aggregate。
- per-source contribution aggregate。
- Left/Right Foot Analysis aggregate。

capture完成后原子移除被压缩entry并释放不再引用的source。Stored Pose不推进Timeline、Marker、Notify或root motion。

## 旧Inertial Blend迁移清单

当前Inertial rule捕获切换前current/previous slot pose与新target pose，计算每骨骼TRS和velocity residual。该数学、history与rebase必须移动到唯一Inertialization节点；旧entry在capture后退出的source lifetime语义不再由Blend Stack的Inertial分支保留。

尚未完成时再次切换，Evaluator先求当前修正pose/velocity，再相对新target重建同一accumulator。不得叠加第二个accumulator或返回旧target原始pose。

Pose Parameter和per-foot feature按相同正式progress从capture aggregate过渡到target sample。

## PoseSlotFrame

Slot Job每帧发布不可变：

```text
PoseSlotFrame
  PoseSlotId
  CompletionIdentity
  Availability: Pose | NoPose | Invalid
  OutputWeight
  DenseLocalPose
  PoseParameterBuffer
  Live/StoredContribution
  Left/RightFeatureAggregate
  ContinuityIdentity
```

该frame只是slot内部结果。其per-bone/per-foot contribution还未经过跨slot Bone Mask，不得直接称为最终可见贡献。只有Character Pose Graph输出可以供Foot Placement使用。

## Animancer Source Backend

`AnimancerPoseSamplingBackend`只按完整AnimationPoseSourceId创建AnimationClip state或producer内部ManualMixerState，应用resolved sample time、loop和child weight，管理playable寿命。Timeline控制state保持Speed 0，child保持DontSynchronize；`ResolvedAnimationPoseRequest.VisualTimeScale`仍表达有效视觉时间推进率，不等同于Animancer state Speed。

Backend不得调用AnimancerLayer.Play、StartFade、FadeGroup、automatic layer weight或transition lookup，也不得决定entry weight、retirement、PoseSlot composition或Animator最终pose。

## Slot Pose Evaluation

Runtime按Rig bone count、slot count和每slot容量预分配source、Stored、history、Inertial、parameter与weight Native workspace。Source capture把Animancer source AnimationStream写入独立buffer slice；`AnimationSlotBlendJob`按不可变frame plan生成PoseSlotFrame buffer。

迁移前每个`AnimationBlendStackRuntime`同时保存Live、Stored与Inertial状态。当前显式BlendStack只把Live与Stored写入预分配workspace；局部Inertialization使用独立双页history与residual workspace，两者由同一Pose Plan completion提交。

Source playable、capture job、slot blend job、Pose Graph job与最终writer必须位于同一Animancer PlayableGraph并在一次Evaluate中按同一completion顺序完成；成功后Runtime才调用`CompleteFrame(...)`确认该completion并发布release。Runtime不得先Evaluate source、回到托管代码逐骨复制，再第二次Evaluate最终pose，也不得保留managed pose evaluator。`AnimationPoseSourceCaptureBinding`只借用Workspace拥有的Native slice，不拥有Allocator或Dispose职责。

Slot Job不写VisualRoot、Gameplay Body或最终Animator output，不读取Pose Graph authoring，也不在表现帧扩容。最终`CharacterPoseGraphNativeJob`在同一正式PlayableGraph拓扑中消费这些buffer。

## Marker Sync

本节原先描述的图外handoff runtime不再是目标架构。Marker Sync必须迁入Selection路径上的显式`MarkerSync`节点，并与一个stateful Player一对一配对。

Blend Stack只在membership预阶段发布该节点当前与尚未exact release的live source usage；MarkerSync随后根据AnimationTrack编入Projection的binding解析leader、segment fraction与effective sample page；Blend Stack最后按该page采样source并独立计算CrossFade weight。Stored Pose不是live source usage，不加入Marker relation；Inertialization也不加入relation。

source被Stored capture或exact release移除时，Blend Stack只发布release usage。MarkerSync根据该正式usage建立continuation anchor并detach relation。Blend Stack不得读取MarkerId、SyncRole或relation，也不得扫描自己的entry来复制同步算法。

## Foot Feature Boundary

Live source按effective visual time采样Projection feature。CrossFade按左右脚BoneId实际transition weight形成slot内aggregate；Stored Pose保存capture aggregate；Inertial连续过渡capture与target aggregate。

Pose Graph再按最终Bone Mask和slot composition传播这些aggregate，生成FinalAnimationPoseFrame中的左右脚输入。Foot Placement不得直接消费本层未合成的slot scalar。

## Runtime Lifecycle

创建顺序：

```text
Projection / Rig / Blend Library / Pose Program validation
  -> fixed slot workspaces
  -> Animancer source backend
  -> source capture
  -> slot blend jobs
  -> CharacterPoseGraphNativeProgram / CharacterPoseGraphNativeJob
  -> AnimationFinalPoseStreamWriterJob
  -> Lifecycle / Marker Sync / Stack
```

每帧顺序：

```text
consume channel commands
  -> raw Selection cache
  -> BlendStack membership预阶段与source usage
  -> 显式MarkerSync解析effective sample page
  -> BlendStack Apply selection与Empty
  -> BeginFrame并准备全部source capture
  -> PrepareSlotJob生成全部不可变slot plan
  -> CharacterPoseGraphNativeJob与final writer入图
  -> Animancer PlayableGraph单次Evaluate
  -> CompleteFrame并发布FinalAnimationPoseFrame
  -> Foot Placement
```

销毁时先停止外部注册，reset Foot Placement，清理Lifecycle/Stack/relation，完成jobs，dispose workspace，再释放Animancer source。

## Validation and Failure

以下情况必须失败且不得fallback：

- Rig、Binding、Blend Profile identity或dense长度不匹配。
- Stack容量、阈值或倍率非法。
- transition matrix缺少任一可达同slot pair。
- curve非规范、非有限或非单调。
- RequireOutput slot被要求到Empty。
- source/Stored workspace无效、局部Inertialization workspace无效或pose非有限。
- 同一PresentationFrame重复推进slot Stack。
- PoseSlotFrame未完成却进入Pose Graph。

不得改用Animancer fade、default profile、Humanoid mapping、bind pose、旧TransitionLibrary或global Layer compositor。

## Motion Matching目标边界

Motion Matching只输出Animation Selection。Continue保持同Selection identity并更新sample；Jump提升SelectionGeneration，由`SelectedPosePlayer`发布typed discontinuity。推荐图通过局部`Inertialization`处理高频jump；作者明确需要多source共同可见期时才连接Blend Stack并使用其CrossFade/Stored Pose合同。MM不得选择transition technique，也不得建立私有fade、Stack或惯性器。

## Implementation Status and Remaining Closure

已经安装：

1. Rig、Blend Profile、per-slot Blend Library、canonical curve与完整matrix的代码合同。
2. AnimationChannelId/PoseSlotId binding、PoseSlotFrame与FinalAnimationPoseFrame合同。
3. `AnimancerPoseSamplingBackend`、source physical registry和完整SourceId隔离。
4. `AnimationBlendStackRuntime`、Stored Pose、容量压缩与source retirement；局部Inertialization独立拥有history/residual/rebase。
5. `AnimationSlotBlendJob`、`CharacterPoseGraphNativeJob`与final writer在同一PlayableGraph单次Evaluate中的正式编排。
6. Foot feature、Preview、Trace与Runtime diagnostics的新链路。
7. 旧Animancer fade、Layer weight、global compositor、managed evaluator与兼容代码删除。

尚未收口：

1. Blend Profile Inspector显示Rig identity、BoneId与最终duration multiplier。
2. Corin正式Rig Definition、dense BoneId、左右脚语义与root exclusion。
3. Corin Blend Profiles和BaseLocomotionSlot/FullBodyActionSlot完整matrix。
4. Corin Profile、Pose Graph、Runtime Rig Binding、Projection与Float32/Fixed wrapper原子重建。
5. Corin旧`m_Layers`、`m_TransitionLibrary`和旧producer binding序列化数据随正式资产重写直接删除。
6. current specs中的Animancer transition权威、旧LayerId和单Base Corin口径通过两个change的delta统一替换。

Corin资产实施唯一归属`add-character-presentation-pose-graph`第15章。本change不得单独创建另一份Rig、Blend Library、Profile、Binding或Projection，也不得在该资产迁移完成前单独归档成已闭合角色链路。

## Tradeoffs

### 选择：Stack只拥有多source时间混合

业务收益：需要共同可见期的Base、FullBody、UpperBody或未来Equipment分支可以独立CrossFade，跨路组合可以单独改图；高频MM jump则不必承担多source Stack。

技术代价：必须定义稳定Pose Value、PoseNodeId与source usage，并协调Player、Stack和final Pose Plan顺序。

### 选择：项目拥有source间weight，Animancer只采样

业务收益：CrossFade与Stored Pose的source权重只有一份事实；局部Inertialization只处理完成Pose残差，不再复制source混合权重。

技术代价：项目同时承担Stack的source pose/clock/curve内存，以及独立Inertialization的history/residual内存，但二者所有权不重叠。

### 选择：CrossFade与Inertialization是两个显式节点

Blend Stack CrossFade保留多个source细节和Marker共同可见期；局部Inertialization不保留旧source，适合高频取消与MM pose jump。作者通过图连接选择，Compiler分别物化node-local Blend Policy和Inertialization Policy，Runtime不猜测。

Marker共同可见期只描述Blend Stack发布的source usage寿命，不表示Blend Stack拥有同步算法。作者需要同步时必须在Selection路径显式连接MarkerSync；没有该节点时同一共同可见期内各source仍按raw time采样。

### 选择：容量触发时捕获完整slot输出

capture边界严格连续，source释放清楚。代价是被压缩entry的独立时间identity结束；Gameplay事实不受影响。

### 未选择：Stack继续做global Layer composition

它能少一个runtime module，但会再次把时间历史、Bone Mask、Additive、curve和最终Pose压在一起，并与已安装Pose Graph重复，因此已经删除。

## Spec Conflicts and Resolution

- `character-animation-layer-runtime`中的隐藏Layer/PoseSlot Stack owner删除；Stack owner为显式PoseNodeId。
- Animancer独占fade/final pose改为显式Blend Stack独占多source CrossFade、局部Inertialization独占单Pose残差，Pose Graph Plan独占组合与最终pose。
- `character-animation-presentation-authoring`的Layer catalog与Animancer TransitionLibrary改为Pose Graph、Blend Library与Rig。
- `character-animation-pipeline`链路改为`Selection -> explicit Player -> Pose Graph Plan -> world-aware FootPlacement -> Final Publication`。
- `character-foot-placement-presentation`只消费Pose Graph最终每脚贡献。
- `add-character-presentation-pose-graph`与本change的Runtime已经按同一PlayableGraph原子安装；剩余Corin资产和spec收口仍必须由同一最终配置链完成。
- current `character-animation-presentation-authoring`仍保留Animancer原生transition权威，本change归档时必须由现有MODIFIED delta替换，不得与新的Blend Library Requirement并存。

## Risks

- Player source capture、显式Blend Stack和final Pose Graph必须在同一PlayableGraph顺序中工作，不能用独立graph或Transform回写拼接。
- Stored Pose失去旧source时间语义，Marker detach与feature capture必须同边界提交。
- Per-Bone transition导致单一scalar无法代表slot source贡献，所有diagnostics与feature调用方必须同步迁移。
- 多change共同修改Projection，剩余Corin资产必须只按`add-character-presentation-pose-graph`第15章落地，不能再从本change复制第二份旧Layer迁移清单。

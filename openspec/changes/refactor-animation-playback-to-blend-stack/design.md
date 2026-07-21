# Design: 每Pose Slot完整Animation Blend Stack

## Context

当前正式动画路径为：

```text
Program Finalize
  -> Layer selection
  -> Timeline visual sample
  -> AnimationPlaybackLifecycle
  -> AnimancerLayer.Play / FadeGroup
  -> Animancer Layer composition
  -> Final Animator Pose
  -> Foot Placement
```

这条路径同时存在Lifecycle的Playback寿命和Animancer state graph的实际权重，且不同generation可能复用同一producer visual。Stored Pose、Per-Bone transition和Inertial都需要项目直接拥有时间weight与pose history，不能继续把FadeGroup作为隐藏权威。

此前设计又把所有Layer的空间合成塞进同一个Blend Stack evaluator。`add-character-presentation-pose-graph`现已明确：

- Blend Stack回答“同一个Pose Slot的旧source如何连续过渡到新source”。
- Pose Graph回答“多个Pose Slot如何按骨骼与参数组合成最终pose”。

本设计只保留第一类职责。

## Goals

- 每个Pose Slot拥有稳定、有序、可诊断的时间Blend Stack。
- 每个entry拥有独立Fade Clock、Curve、depth-adjusted duration和每骨骼transition weight。
- 在固定active source预算下通过Stored Pose保持pose、velocity、parameter和feature连续。
- 提供CrossFade与Inertial两种显式transition technique。
- 让Animancer继续采样Clip/ManualMixer，不再拥有fade和source间权重。
- 输出完整PoseSlotFrame供Pose Graph唯一消费。
- Marker Sync、Preview、Rollback replacement和source retirement只读取同一Stack事实。
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

## Target Architecture

```text
Committed AnimationChannel selection
              |
              v
AnimationPlaybackLifecycle
  selected / pending / retention / marker coordination
              |
              v
ResolvedAnimationPoseRequest
  AnimationChannelId + PoseSlotId + PlaybackId + sample
              |
       +------+------+
       |             |
       v             v
AnimationBlendStackRuntime    AnimancerPoseSamplingBackend
  entries / clocks            Clip / ManualMixer source pose
  capacity / stored           no fade / no layer composition
  inertial / retirement                |
       |                               v
       +--------------------> Source Pose Capture
                                       |
                                       v
                         AnimationSlotBlendPoseEvaluator
                           CrossFade / Stored / Inertial
                                       |
                                       v
                                  PoseSlotFrame
                                       |
                                       v
                         Character Presentation Pose Graph
                                       |
                                       v
                           Final Pose -> Foot Placement
```

Stack Runtime拥有状态与frame plan；Slot Evaluator拥有该slot骨骼求值；Pose Graph只读取完成的PoseSlotFrame。不存在Animancer state weight或Lifecycle集合重建第二份计划。

## Identity

### AnimationPlaybackId

producer一次activation的完整逻辑/表现identity，拥有Timeline sample、Marker relation和PresentationRetention。

### AnimationBlendEntryId

一次表现blend request identity：

```text
AnimationBlendEntryId
  PoseSlotId
  PlaybackId
  PresentationRequestSequence
```

同Playback连续sample只更新source；在另一个target之后重新成为target时创建新EntryId与独立clock。

### Live Source Entry

引用ResolvedAnimationPoseRequest与Animancer source visual，拥有push order、transition rule和clock。

### Stored Pose Entry

不引用PlaybackId。保存capture边界的slot local pose、pose velocity、Pose Parameter aggregate与per-foot feature aggregate，只作为连续性来源。

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
AnimationBlendEntryId
AnimationPlaybackId
ProgramProducerIndex
resolved visual time / cycle / scale
clip pose samples
exact transition source/target identity
Pose Parameter samples
Foot Analysis samples
```

它不包含State、Action、priority、Graph edge、Pose Graph node、Bone Mask或最终weight。未来Motion Matching必须通过正式adapter产生同一request。

## Entry Lifecycle

### First Sample Push

1. Lifecycle完成Marker Sync并创建request。
2. Stack exact lookup同slot transition matrix。
3. Animancer backend准备完整PlaybackId source visual。
4. Stack按replace/capacity policy决定普通push或capture。
5. Stack原子创建EntryId、clock和frame plan。

任一步失败时该slot进入typed Invalid，不留下半创建source或半切换entry。

`RequireOutput` Slot的`Empty -> producer` exact transition必须在Compiler阶段被强制为零时长。Lifecycle在`PendingFirstSample`期间不得先启动transition；首个合法source pose完成准备后，Stack在同一原子提交中以完整权重初始化该entry。Runtime不得新增`Uninitialized`混合状态、临时改写duration，或使用bind pose、默认Idle和上一帧残留姿势填补首帧。

### Continuous Update

同PlaybackId后续sample更新仍引用该source的entry，不增加entry、不重启clock。Timeline producer内部多clip membership继续由一个ManualMixer表达。

### Re-selection

同Playback在另一个target之后重新成为target时创建新EntryId。多个entry可以引用同一source capture结果；Evaluator按BoneId合并权重，不重复采样。

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

## Inertial Blend

Inertial rule捕获切换前current/previous slot pose与新target pose，计算每骨骼TRS和velocity residual。旧entry在capture后退出，新target成为唯一live source。rotation使用最短弧并保持单位四元数。

尚未完成时再次切换，Evaluator先求当前修正pose/velocity，再相对新target重建同一accumulator。不得叠加第二个accumulator或返回旧target原始pose。

Pose Parameter和per-foot feature按相同正式progress从capture aggregate过渡到target sample。

## PoseSlotFrame

Slot evaluator每帧发布不可变：

```text
PoseSlotFrame
  PoseSlotId
  CompletionIdentity
  Availability: Pose | NoPose | Invalid
  OutputWeight
  DenseLocalPose
  PoseParameterBuffer
  Live/Stored/InertialContribution
  Left/RightFeatureAggregate
  ContinuityIdentity
```

该frame只是slot内部结果。其per-bone/per-foot contribution还未经过跨slot Bone Mask，不得直接称为最终可见贡献。只有Character Pose Graph输出可以供Foot Placement使用。

## Animancer Source Backend

`AnimancerPoseSamplingBackend`只按完整PlaybackId创建AnimationClip state或producer内部ManualMixerState，应用resolved sample time、loop和child weight，管理playable寿命。Timeline控制state保持Speed 0，child保持DontSynchronize。

Backend不得调用AnimancerLayer.Play、StartFade、FadeGroup、automatic layer weight或transition lookup，也不得决定entry weight、retirement、PoseSlot composition或Animator最终pose。

## Slot Pose Evaluation

Runtime按Rig bone count、slot count和每slot容量预分配source、Stored、history、Inertial、parameter与weight Native workspace。Source capture把Animancer source AnimationStream写入独立buffer slice；`AnimationSlotBlendPoseEvaluator`按不可变frame plan生成PoseSlotFrame buffer。

Slot evaluator不写VisualRoot、Gameplay Body或最终Animator output，不读取Pose Graph authoring，也不在表现帧扩容。最终`CharacterPoseGraphEvaluator`在同一正式PlayableGraph拓扑中消费这些buffer。

## Marker Sync

Marker Sync只在同AnimationChannelId/PoseSlotId的live Current与incoming target之间工作，并在Stack push前解析effective time。Stored Pose和Inertial不成为relation节点。

source被capture或Inertial接管后，relation按最后effective/raw time建立continuation anchor再detach，防止target时间跳回。

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
  -> slot evaluators
  -> final Pose Graph evaluator
  -> Lifecycle / Marker Sync / Stack
```

每帧顺序：

```text
consume channel commands
  -> retained visual sampling / Marker Sync
  -> slot Stack frame plans
  -> Animancer source sampling
  -> all PoseSlotFrame evaluation
  -> final Pose Graph
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
- source/Stored/Inertial workspace无效或pose非有限。
- 同一PresentationFrame重复推进slot Stack。
- PoseSlotFrame未完成却进入Pose Graph。

不得改用Animancer fade、default profile、Humanoid mapping、bind pose、旧TransitionLibrary或global Layer compositor。

## Motion Matching Extension Boundary

Motion Matching位于ResolvedAnimationPoseRequest之前。continuation更新同Playback/sample；pose jump提交新EntryId并使用matrix中的CrossFade或Inertial。它必须复用当前slot容量、Stored Pose和退役合同，不得在Stack旁建立私有fade。

## Migration

1. 安装Rig、Blend Profile与per-slot Library schema。
2. 安装AnimationChannelId/PoseSlotId binding与PoseSlotFrame合同。
3. 将Animancer降为source backend。
4. 将Stack evaluator收窄为per-slot输出。
5. 与Pose Graph change在同一Runtime切换中接入final evaluator。
6. 迁移Foot feature、Preview、Trace与Corin资产。
7. 删除Animancer fade、旧Layer Stack、global compositor与旧schema。

不得先保留旧global compositor再套Pose Graph。

## Tradeoffs

### 选择：Stack只拥有时间混合

业务收益：Base、FullBody、UpperBody或未来Equipment每一路都能独立高频切换，跨路组合可以单独改图。Motion Matching无需替换空间compositor。

技术代价：必须定义稳定PoseSlotFrame并协调slot evaluator与final Pose Graph job顺序。

### 选择：项目拥有source间weight，Animancer只采样

业务收益：Stored Pose、Per-Bone transition与Inertial共享同一个事实。

技术代价：项目承担pose buffer、clock、curve、quaternion residual与内存管理。

### 选择：CrossFade与Inertial显式二选一

CrossFade保留多个source细节和Marker共同可见期；Inertial适合高频取消与MM pose jump。Compiler物化完整matrix，Runtime不猜测。

### 选择：容量触发时捕获完整slot输出

capture边界严格连续，source释放清楚。代价是被压缩entry的独立时间identity结束；Gameplay事实不受影响。

### 未选择：Stack继续做global Layer composition

它能少一个runtime module，但会再次把时间历史、Bone Mask、Additive、curve和最终Pose压在一起，并与已规划Pose Graph重复，因此删除。

## Spec Conflicts and Resolution

- `character-animation-layer-runtime`中的LayerId改为AnimationChannelId/PoseSlotId；Stack owner为PoseSlotId。
- Animancer独占fade/final pose改为Stack独占slot内时间混合，Pose Graph独占跨slot与最终pose。
- `character-animation-presentation-authoring`的Layer catalog与Animancer TransitionLibrary改为Pose Graph、Blend Library与Rig。
- `character-animation-pipeline`链路改为`Lifecycle -> PoseSlot Stack -> Source Backend/Slot Evaluator -> Pose Graph -> Pose Post Process`。
- `character-foot-placement-presentation`只消费Pose Graph最终每脚贡献。
- `add-character-presentation-pose-graph`与本change的Runtime activation必须原子完成。

## Risks

- Slot source capture和final Pose Graph必须在同一PlayableGraph顺序中工作，不能用独立graph或Transform回写拼接。
- Stored Pose失去旧source时间语义，Marker detach与feature capture必须同边界提交。
- Per-Bone transition导致单一scalar无法代表slot source贡献，所有diagnostics与feature调用方必须同步迁移。
- 多change共同修改Projection，实施前必须按最终共同合同重读current specs与代码，不能按旧Layer字段落地。

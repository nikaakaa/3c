# Design

## 2026-07-31 MovingTurn参考实现对账

本地参考工程：

- `D:/UE_Project/游戏动画示例`：Game Animation Sample Project。
- `D:/UE_Project/AdvancedLocomotionSystemV`：Advanced Locomotion System V4。

ALS V4的Turn In Place把动画根旋转烘焙为`RotationAmount`。每个曲线值表达当前帧到下一帧的Yaw差，角色Blueprint逐帧调用`AddActorWorldRotation`，因此动画曲线直接推进Actor真实朝向。ALS同时用独立`FootLock_L`和`FootLock_R`曲线保存支撑脚局部锁定状态，并按目标角与动画标称角计算播放缩放。该方案适合非确定性单机Actor驱动，但若直接搬入本项目，会让Presentation曲线进入Rollback Body所有权。

GASP的Character Movement Capsule保持运动主体。AnimGraph的`OffsetRootBone` Rotation使用`Accumulate`抵消Capsule当帧旋转，使骨架根节点可暂时独立；Steering再按未来Trajectory给出视觉目标，结束时Root Offset使用`Release`回到Capsule。GASP当前为Turn In Place额外放置第二Steering节点是其节点属性限制产生的实现细节，本项目不得据此建立第二Pose入口。

本change采用两者职责的交集：

```text
ALS作者Yaw曲线与目标角缩放
  + GASP Capsule/Body权威与视觉Root Offset
  -> Character RootOrientationWarp Pose节点
```

`RootOrientationWarp`只接收上游有限`SequencePlayer` Pose，Compiler从同一图依赖编译精确Sequence索引，从配置的`RootMotionCurveAsset.LocalYaw`编译作者Yaw正文，并绑定Rig唯一Root Physical Bone。Runtime在节点首次relevant时捕获带符号`FacingError`，按`sourceYaw / totalYaw`得到作者进度，再计算：

```text
animationFacingDelta = capturedTargetAngle * sourceYaw / totalYaw
rootLocalYawOffset = currentFacingError - capturedTargetAngle + animationFacingDelta
```

进入首帧该偏移为零；Body先转时它反向抵消Body旋转；作者Yaw推进时骨架逐步追上；结尾`sourceYaw == totalYaw`且Body已经对准目标时偏移严格为零。原X/Z曲线不编译进该descriptor。Body discontinuity、source continuity变化和节点退出都会清空捕获状态。

该节点不是Action MotionWarp：它不读取Action target、不提交Motion Request、不改KCC或Body。它也不是FootLock：现有FootPlacement继续处理地面高度，若运行证据仍显示支撑脚绕点滑动，再通过独立Pose Capability增加FootLock，不把两种约束塞进同一节点。

## Context

当前链路是：

```text
BTSMTL Locomotion StateMachine
  -> Idle/WalkStart/WalkLoop/RunStart/RunLoop/RunEnd/MovingTurn Timeline
  -> Program Finalize选择BaseLocomotion producer
  -> AnimationSelectionFrame
  -> MarkerSync
  -> SelectedPosePlayer或BlendStack
  -> Inertialization
  -> Composition
```

Gameplay Program既决定角色行为，也决定具体Locomotion动画。Pose Graph只消费已经选好的source。

现有代码已经具有两个更新域，不应把本change理解成新增“逻辑Tick与表现Tick分离”：

```text
Simulation Tick
  -> BTSMTL Gameplay Timeline推进logic time
  -> Window / Motion / Warp / Cue / Action lifecycle
  -> committed raw animation sample anchor

Presentation Tick
  -> 消费committed sample history
  -> 在锚点之间按presentation delta投影visual time
  -> Marker effective-time映射
  -> source sampling / blend / IK / Final Pose
```

Simulation提交的是playback identity、raw time、cycle和time scale等可重放事实，不提交最终骨骼Pose。Presentation没有新Simulation Tick时仍可继续求值动画，但不得推进Gameplay Timeline或产生Window、Motion、Cue和Action lifecycle。当前问题不是缺少两个Tick，而是共享`CharacterAnimationPlaybackRuntime`仍同时承担command消费、sample history、表现投影、Pose source、混合与最终Pose协调，且持续Locomotion的具体source仍由Gameplay选择。

目标链路是：

```text
Committed Simulation/Body/Intent
  -> CharacterPresentationFactFrame
  -> Locomotion PoseStateMachine
       -> SequencePlayer / BlendSpacePlayer / MotionMatching source
       -> State transition
  -> Base Locomotion Pose

BTSMTL Action StateMachine
  -> Action Timeline
       -> Gameplay Window / Motion / Cue
       -> exact ActionAnimationPlaybackFrame
  -> FullBodyAction Slot

Base Locomotion Pose + Action playback
  -> Slot
  -> Composition
  -> Inertialization / FootPlacement
  -> Final Pose
```

两条链共享committed Simulation时间和Action Timeline identity，但不共享选择权：

- Gameplay选择动作是否发生、哪个Action实例发生。
- PoseStateMachine选择持续Locomotion如何表现。
- Slot只把已经确认的Action playback插入基础Pose。

## Goals

- 让Pose Graph拥有持续姿势状态选择。
- 让Gameplay Program不再知道具体Idle、Walk、Run、Start、Stop、Turn动画资源。
- 保留Action Timeline对Motion、Window、Cue、生命周期和有限动作权威时间的所有权。
- 让全身Action期间Locomotion持续求值，Action结束后回到当前正确基础Pose。
- 让Transition Routing模块同时服务PoseState edge和Slot handoff，不复制Blend选择算法。
- 原子迁移Corin，不保留旧BaseLocomotion Selection旁路。
- 作者术语对齐UE的State Machine、Sequence Player、Transition、Slot、Inertialization。

## Non-Goals

- 不把BTSMTL Gameplay StateMachine替换成PoseStateMachine。
- 不让PoseStateMachine驱动Character Motor、Action准入、命中、窗口或网络状态。
- 不在本change实现完整Montage资产、Montage Section或Montage编辑器。
- 不恢复Animator Controller、Animator.CrossFade或第二PlayableGraph。
- 不接入尚未完成的Hit、Death或完整Combat solver。
- 不新增测试或自动Build入口。
- 本次设计补强不新增孤儿资源删除、Profile schema旧资产清理或Corin额外资产迁移任务；资产处理继续位于既有原子迁移阶段，并在运行Module与编译合同闭合后最后执行。

## Decision 1: 两类表现输入

### Continuous Presentation Facts

`CharacterPresentationFactFrame`只保存当前表现帧需要的committed事实：

```text
FrameIdentity
SimulationTick
PresentationTime
Grounded
HorizontalSpeed
HorizontalAcceleration
VerticalSpeed
MovementDirection
FacingError
DesiredDirection
MotionPhase
BodyDiscontinuity
```

这些值来自当前唯一Simulation/Body/Intent结果。它们不携带AnimationClip、PoseNodeId、Blend Logic或state名称。

Pose transition读取fact，例如：

```text
Idle -> Start:
HorizontalSpeed > StartThreshold

Locomotion -> Stop:
HorizontalSpeed <= StopThreshold
AND DesiredSpeed == 0

Grounded -> Fall:
Grounded == false
AND VerticalSpeed < 0
```

### Finite Action Playback

`ActionAnimationPlaybackFrame`保存已由Gameplay确认的有限动作表现：

```text
AnimationChannelId
ActionInstanceId
ProgramProducerId
PlaybackGeneration
RawVisualTime
ContinuousTime
Loop
PlayRate
SourceLocalClipSamples
PresentationParameters
```

它仍从Action Timeline的committed playback生成。Slot不能更换Attack1为Attack2，也不能根据输入自行启动Dodge。

## Decision 2: PoseStateMachine是Pose Graph节点

`PoseStateMachine`不是BTSMTL节点，也不进入Gameplay Semantic IR。它属于`CharacterPresentationPoseGraphAsset`并编译进Presentation Projection。

作者结构：

```text
PoseStateMachine
  Entry
  State Idle
  State Start
  State Locomotion
  State Stop
  State Turn
  State Alias ToGrounded
  Transition edges
```

每个State拥有一个inline Pose subgraph并输出一个Pose。State subgraph只能使用表现节点和只读Presentation Fact：

- SequencePlayer
- BlendSpacePlayer
- MotionMatching state-local provider input与对应Player
- MarkerSync
- Additive
- ModifyBone
- Pose Parameter
- 其它已安装Pose节点

State subgraph不能使用：

- Action activation
- Gameplay State mutation
- Blackboard write
- Timeline logic operation
- Motion contribution
- WorldSolver request
- GameplayEffect

## Decision 3: Transition Rule是纯表现表达式

Transition Rule使用Pose Graph内部的typed pure expression，不复用BTSMTL ConditionRuleGraph运行时。允许的第一阶段操作：

- Bool fact
- Float fact
- Enum fact
- `Not`
- `And`
- `Or`
- `Equal`
- `NotEqual`
- `Greater`
- `GreaterOrEqual`
- `Less`
- `LessOrEqual`
- `TimeInState`
- `StatePoseRemainingTime`

Compiler把表达式降低为固定operation span。Runtime只读当前Fact page和本PoseStateMachine workspace，不读取CharacterSimulationState mutable address。

每条Transition edge显式保存：

- Source State
- Target State
- Priority
- Rule
- Blend Logic
- Duration
- Curve
- Blend Profile
- Reset Target Player

`State Alias`只复用入边来源集合，不拥有Pose，也不成为runtime active state。它用于减少Any State、Grounded family和Airborne family的蜘蛛网。

## Decision 4: SequencePlayer直接属于表现资源

持续Locomotion不再通过Timeline AnimationTrack获得AnimationClip。

Pose Graph中的`SequencePlayer`引用Graph-owned、类型化的`CharacterSequencePoseSourceSlot`子资产。`CharacterAnimationPresentationProfile`用Profile-owned`CharacterSequencePoseSourceBinding`子资产精确引用该Slot，并绑定：

- AnimationClip resource
- Rig identity
- Loop mode
- 默认play rate
- Marker topology与marker sequence
- source-local Foot Placement Weight typed curve
- Foot Analysis identity

Graph节点保存Source Slot对象引用和node-local播放设置，Profile保存角色资源binding子资产。SequencePlayer只使用`PresentationDelta`时钟。Gameplay只提交Body位移、Body朝向、MovementMode与discontinuity事实；原地转身的表现角度由`RootOrientationWarp`按Turn Sequence自己的sample time读取作者Yaw曲线，不把Gameplay MotionCurve相位投影回Pose。Projection Compiler按精确对象关系解析binding，拒绝缺失、重复、跨owner、类型不匹配、Rig不匹配或marker不完整的binding，并为当前Projection生成dense source index。

有限Action的AnimationTrack继续由Timeline拥有Clip、raw time、clip weights和可选Marker Group。不能把Action Timeline资源复制进Sequence source binding作为第二份真相。

### 作者入口、Analysis与Preview归属

持续Pose source和有限Action producer使用相同的底层采样、marker segment、Foot Analysis artifact与typed curve能力，但作者数据不能共用owner：

| 数据 | 持续Pose source | 有限Action producer |
|---|---|---|
| Clip resource | Profile Pose source binding | Timeline AnimationTrack/Clip binding |
| Marker | Profile source editor | Timeline Editor |
| Foot Placement Weight | Profile source typed curve | Timeline Clip typed curve |
| Foot Analysis identity | Profile source binding | Profile中的Action producer source binding |
| Window、Motion、Cue | 不允许 | Action Timeline |
| Transition Blend | PoseState edge | AnimationSlot或显式BlendStack |

Profile Inspector是Pose source唯一写入口，Timeline Editor是有限Action Timeline数据唯一写入口，Pose Graph只保存source引用和transition owner。跨工作区面板只能只读显示并跳转到真实owner，不能复制mutation。

Timeline Preview只把有限Action游标降低为正式Action Selection并通过AnimationSlot执行。Locomotion Preview只在Pose Graph Workspace中构造typed Presentation Fact并执行正式PoseStateMachine。两者复用同一Projection、source backend、Transition Routing、FootPlacement和Final Pose Plan，不建立第二套preview runtime。

RuntimeDebugSession统一发布Presentation Fact、PoseState active/target、source relevance、Action playback、Slot route、Marker relation、Inertialization和最终Pose completion。Timeline Live Debug只解释Action Timeline relation，Pose Graph Live Debug解释PoseState Source Sync；允许互相导航，但不能把Pose source伪装成Timeline playback。

## Decision 5: Slot是有限动作插入点

`Slot`是显式Pose节点：

```text
Inputs:
  Source Pose
  Action Playback

Output:
  Pose
```

无Action时：

```text
Output = Source Pose
```

Action首样本就绪后：

```text
Output = Transition(Source Pose, Action Pose)
```

Action结束后：

```text
Output = Transition(Action Pose, 当前Source Pose)
```

Slot绑定一个稳定AnimationChannelId和node-local Blend Policy。它内部编译为明确可见的source usage、action player、BlendStack capacity和Transition Routing plan；Workspace必须在Pose Graph工作区展示这些compiled operations，不得形成不可诊断的隐藏播放器。

第一阶段Corin使用`FullBodyAction Slot`。以后上半身动作通过：

```text
Cached Locomotion Pose
  -> UpperBody Slot
  -> Layered Blend Per Bone
```

实现上不得把Bone Mask塞进Slot；骨骼范围继续由Layered Blend Per Bone拥有。

## Decision 6: Action运动权与Pose覆盖分离

当前`HasActionLocomotionOwnership`同时表达：

- Action控制角色移动。
- Locomotion不再输出动画。

目标拆为：

```text
Action Motion Ownership
  -> Motion arbitration / Character Motor

Action Pose Playback
  -> FullBodyAction Slot
```

Action可以有四种组合：

| 动作 | Motion | Pose |
|---|---|---|
| 原地上半身射击 | Locomotion Motor | UpperBody Slot |
| 全身原地攻击 | Locomotion受限或停止 | FullBody Slot |
| Root Motion闪避 | Action Motion | FullBody Slot |
| 纯Gameplay击退 | Gameplay Motion Source | 可选Hit Slot |

PoseStateMachine不得因为FullBody Slot权重为1而停止读取最新Body事实。Action结束时Slot回到当前Locomotion Pose，不需要Gameplay选择RunLoop或Idle恢复动画。

## Decision 7: Locomotion Gameplay保留什么

BTSMTL仍然可以管理真正影响Gameplay的移动状态：

- 是否允许接收移动输入。
- Grounded、Airborne或受控移动模式。
- Action是否取得Motion authority。
- 转向规则、加速度规则和移动约束。
- 离散动作对Locomotion请求的打断。

BTSMTL不再管理：

- Idle动画。
- Walk动画。
- Run动画。
- Start/Stop/Turn动画资源。
- 动画状态间CrossFade。
- 动画marker handoff。
- 动作结束后恢复到哪个动画producer。

如果现有Locomotion Timeline包含Gameplay MotionCurve，迁移时必须按语义分类：

- 控制真实Body运动的曲线迁入唯一Gameplay Motion Profile或现有Motion operation。
- 只为动画播放服务的曲线迁入Presentation source binding。
- 无消费方的旧曲线删除。

不得让PoseStateMachine的State时间成为Motor运动真相。

## Decision 8: Transition Routing接入点

已实现的Transition Routing模块保持算法与握手所有权。

PoseState transition：

```text
Active State Pose + Target State Pose
  -> Transition edge exact Blend Logic
  -> Standard Blend由StateMachine transition runtime执行
  或
  -> typed Inertialization request
  -> branch-local Inertialization
```

Slot transition：

```text
Source Pose / Current Action + Incoming Action或SourcePoseEndpoint
  -> Slot exact Blend Logic
  -> Standard Blend由Slot BlendStack执行
  或
  -> typed Inertialization request
  -> branch-local Inertialization
```

Routing模块不读取State Rule、Action类型、Timeline、Fact或Bone Mask。调用者只提交已解析source/target endpoint、readiness、generation、capture和release事实。

## Decision 9: Corin目标拓扑

```text
Presentation Fact Inputs
  -> Corin Locomotion PoseStateMachine
       Idle: SequencePlayer
       Start: SequencePlayer
       Locomotion: BlendSpacePlayer
       Stop: SequencePlayer
       Turn: SequencePlayer
  -> Locomotion Inertialization
  -> FullBodyAction Slot
       Action source: Timeline Action Playback
  -> Pose Parameter Resolve
  -> FootPlacement
  -> Output Pose
```

如果当前动画资源不足以建立正式BlendSpace，Locomotion State可以使用明确SequencePlayer状态，不得保留旧Timeline Selection作为fallback。资源缺失应使Projection Build失败或明确缩小已配置State集合。

## Decision 10: 动画表现协调与有限Action Playback必须分离

当前`CharacterAnimationPlaybackRuntime`同时消费Gameplay animation command、维护全部playback生命周期、解析source demand、推进Pose Runtime、直接协调Motion Matching、执行最终Pose Plan并发布diagnostics。这个对象同时承担有限动作实例和整帧动画执行器两种责任，无法准确表达新架构中Action playback与PoseState source的不同寿命。

目标运行结构固定为：

```text
CharacterSimulationPresentationRuntime
  -> CharacterAnimationPresentationRuntime
       -> 构造并提交CharacterPresentationFactFrame
       -> 更新PoseStateMachine与state-local source provider
       -> CharacterActionPlaybackRuntime
            -> 消费有限Action command batch
            -> 保存exact playback identity与raw visual sample
            -> 管理PendingFirstSample/Selected/Retained/Retired
       -> AnimationSlot
            -> 声明Action source usage
            -> 执行Action handoff与release permission
       -> MarkerSync / source backend / Pose Plan
       -> 唯一FinalAnimationPoseFrame
```

### CharacterAnimationPresentationRuntime

它是动画表现帧的唯一协调器，负责：

- 接收同帧committed Body、Intent、Presentation interpolation和有限Action command batch。
- 开启、提交或回滚本帧动画表现workspace。
- 按编译顺序推进PoseStateMachine、SequencePlayer、BlendSpace、Motion Matching provider、Action Playback、AnimationSlot、Transition Routing与Pose operation。
- 唯一调用Pose Runtime的frame advance与evaluate。
- 在完整Pose Plan成功后发布final pose、diagnostics、acknowledgement与retirement结果。

它不拥有Action准入、Timeline logic time、具体PoseState选择规则、Action混合Policy、Marker映射算法、Animancer source playable或任何第二份播放生命周期。

### CharacterActionPlaybackRuntime

它只管理Gameplay已经确认的有限Action playback：

```text
Input:
  ordered Action selection/sample/complete/release command
  committed raw visual time
  Slot/Player exact source usage与release permission

State:
  AnimationPlaybackId
  ActionInstanceId
  producer与generation
  raw sample
  PendingFirstSample / Selected / Retained / Retired

Output:
  current Action playback frame
  Action raw sample demand
  lifecycle snapshot
  exact retire result与command acknowledgement
```

它不得：

- 构造或选择Locomotion PoseState。
- 为SequencePlayer、BlendSpacePlayer或Motion Matching state-local selection创建`AnimationPlaybackId`。
- 推进Gameplay Timeline或自行累计Action权威visual time。
- 查询PoseStateMachine relevance或Motion Matching query。
- 计算Marker effective time、CrossFade weight、Stored Pose、Inertialization residual、Bone composition或最终Pose。
- 调用整个Pose Runtime的advance/evaluate。

Action Timeline的committed raw visual time继续是Action动画采样权威。`CharacterActionPlaybackRuntime`只保存该committed history，不按render delta推进任何表现时钟。动画表现协调链中的`ActionPresentationSampleProjector`根据committed history投影visual time，并在新sample到达时按完整playback identity重基线；这样Window、Motion与Cue只读取Gameplay已经提交的Timeline事实，Action Pose则在不改写这些事实的前提下连续重采样。

### Slot与Playback的释放握手

Gameplay提交Action release只表示逻辑producer已经结束，不表示source pose可以立即销毁：

```text
Gameplay release
  -> Action Playback进入只读Presentation retention
  -> Slot继续声明Sample或HandoffReference usage
  -> Slot transition完成并提交release permission
  -> 协调器向PhysicalPoseSourceRegistry提交完整source set
  -> playable与capture资源全部返回匹配completion
  -> Action Playback进入Retired
```

`AnimationSlot`唯一拥有Action到Action、Action到Source Pose和Source Pose到Action的transition、weight、Stored Pose capacity策略及release permission。`CharacterActionPlaybackRuntime`唯一拥有有限Action实例是否仍存在以及何时完成retirement。两者不能复制对方的状态。

Slot的无Action占用 MUST表示为`SourcePoseEndpoint`。`SourcePoseEndpoint`表示Slot当前没有有限Action且输出同帧持续更新的Source Pose；它与没有可用Pose的`NoPose`不是同一状态。Routing plan、snapshot与作者UI不得继续使用`Empty`同时表达这两个语义。

### 与UE术语的对应

| 本项目 | UE中最接近的职责 | 明确差异 |
|---|---|---|
| PoseStateMachine与state-local Player | AnimGraph State Machine与Asset Player | 只读取Presentation Fact，不读取Gameplay mutable state |
| CharacterActionPlaybackRuntime | `FAnimMontageInstance`的有限实例寿命 | Action权威时间来自committed BTSMTL Timeline，不由实例自由推进 |
| AnimationSlot | AnimGraph Slot | Blend Logic、Transition Routing和release permission全部显式编译 |
| CharacterAnimationPresentationRuntime | `UAnimInstance/FAnimInstanceProxy`的update/evaluate边界 | 执行项目唯一编译Pose Plan和表现事务 |
| BTSMTL Action Timeline | Montage Timeline加Gameplay动作轨道的业务组合 | 同时拥有Motion、Window、Cue和Action lifecycle，不降格为纯动画资产 |

本change不实现完整Montage资产、Montage Section或Montage编辑器。该映射只锁定学习口径和职责边界。

## Decision 11: Action playback与state-local Pose source使用不同ABI

旧`AnimationSelectionFrame`同时要求`AnimationChannelId`、`ProgramProducerIndex`、`AnimationPlaybackId`和Pose采样结果，使Motion Matching、Blend Space与Sequence source必须伪装成Gameplay playback。目标合同固定拆为两类：

```text
ActionAnimationPlaybackCommand
  Select / Sample / Complete / Release
  EventId
  AnimationPlaybackId
  ActionInstanceId
  AnimationChannelId
  ProgramProducerId
  Generation
  CommittedRawSample

ActionAnimationPlaybackFrame
  exact Action identity
  latest committed raw sample
  lifecycle phase
  raw source-local clip samples

PresentationPoseSourceSample
  Projection-local dense source index
  PlayerNodeId与frame lease
  source generation
  raw/effective presentation sample
  availability
  source-local clip samples
```

只有有限Action合同携带Gameplay channel、producer、ActionInstance和`AnimationPlaybackId`。SequencePlayer、BlendSpacePlayer与Motion Matching使用`Projection-local dense source index + PlayerNodeId + source generation + frame lease`，不得把作者Slot对象带入Runtime，也不得填充伪造的channel、producer index或playback generation。

Projection不再把`AnimationSelectionInput`、`MotionMatchingSelectionInput`与`ActionPlaybackInput`编译进同一个Selection Input表。正式计划拆为：

- `ActionPlaybackInputPlan`：只解析有限Action channel、producer、Slot和Action Player。
- `PoseStateSourceProviderPlan`：只解析PoseState、provider、player、Presentation source与readiness。
- `PosePlanExecutionPlan`：只保存已经解析的Pose operation连接和workspace layout。

旧通用`AnimationSelectionInput`、`CharacterAnimationPresentationBindingIndex`中跨Action/Pose的索引以及Pose Runtime按channel扫描Player的查询接口必须删除，不保留adapter。

业务取舍：两类frame不能再通过同一个数组和resolver统一遍历，但有限Action的顺序、身份和释放语义不会继续污染Locomotion、Blend Space与MM，后续source provider可以独立扩展而不修改Gameplay playback。

## Decision 12: Action command、生命周期与释放使用显式registry

持久Gameplay命令与帧内Pose请求使用两条不同的接口：

```text
ActionPlaybackCommandInbox
  跨帧保存未提交的Select/Sample/Complete/Release
  按EventId、producer与generation保序
  支持Replace、Retire与acknowledgement

PresentationFrameWorkspace
  只保存当前表现帧的provider demand、Pose request、usage和completion
  frame结束后提交或整体丢弃
```

`Publish`、`Replace`与`Retire`只能修改Action inbox，不能在外部调用时直接修改live lifecycle registry。`PoseRequest`与`PoseUnavailable`不再属于Action command kind。

`ActionAnimationPlaybackLifecycleRegistry`按完整`AnimationPlaybackId`保存独立entry，而不是按channel只保存当前winner。每个entry至少保存：

```text
ActionInstanceId
ProgramProducerId
AnimationChannelId
Generation
LatestEventId
FirstSampleReadiness
LogicTerminal
SlotUsageSet
RetirementPermission
BackendReleaseRequest
BackendReleaseCompletion
LifecyclePhase
```

生命周期顺序固定为：

```text
PendingFirstSample
  -> Selected
  -> Retained
  -> RetirementPermitted
  -> 等待source backend exact release completion
  -> Retired
```

Gameplay Complete或Release只建立logic terminal，不直接等于Retired。Slot必须按`SlotId + ActionPlaybackId + usage kind + completion identity`提交Action-only usage batch；多个exact consumer的usage全部消失并取得release permission后，协调器才向physical source registry发起带request identity与完整source set的释放。所有source完成后Action registry才能提交Retired。

`ActionAnimationPlaybackLifecycle`不得依赖`AnimationPosePlayableGraphRuntime`或任何具体Pose Runtime类型，不得通过扫描channel、Player、BlendStack或source backend反推出生命周期。

业务取舍：释放链多了一次显式completion，但连续打断、多个Slot和Stored Pose历史都能精确说明“谁还在使用旧Action”，不会因第一个source释放就提前销毁整个playback。

## Decision 13: committed时间、表现时间与整帧事务必须分离

本决策保留并模块化现有双Tick语义，不引入第二套Timeline runtime，也不为Timeline本身增加可切换时钟。作者通过业务owner选择正式入口：有限Gameplay Action使用BTSMTL Action Timeline和committed anchor；循环或非曲线驱动的持续Locomotion使用PoseStateMachine内Sequence/BlendSpace/MM source的presentation-owned clock；显式由Gameplay Locomotion MotionCurve驱动的有限Pose source可通过SequencePlayer clock binding消费同一已提交曲线相位。

原嵌套`AnimationSamplingState`拆为三个Module：

- `ActionCommittedSampleHistory`属于Action Playback，只保存按EventId确认的raw sample、cycle、loop和visual time scale。
- `ActionPresentationSampleProjector`属于动画表现协调链，在两个committed sample之间插值；进入Presentation retention后可按最后确认的visual time scale继续animation-only投影，finite source在合法coverage末端钳制，cyclic source保持展开cycle。
- `ActionMarkerEffectiveSampleState`属于MarkerSync，只把projected raw sample映射为effective sample并维护relation/rebase。

`CommittedRawVisualTime`只表示Gameplay Timeline已经提交的权威样本。`ProjectedPresentationSampleTime`只服务render采样、transition和Marker，不得写回Gameplay、Window、Motion、Cue或Action lifecycle，也不得在diagnostics中继续显示为committed raw time。

表现时间的推进规则固定为：

- 两个committed sample都存在时，Projector按它们的SimulationTick/EventId顺序和当前presentation cursor插值。
- 只有最新committed sample时，Projector可按其visual time scale在表现帧间继续animation-only投影。
- 新committed sample或branch replacement到达时，Projector按完整playback identity重基线，不把旧投影时间冒充新逻辑事实。
- finite source到达合法coverage末端后钳制；cyclic source保持展开cycle。
- Locomotion SequencePlayer只使用presentation delta推进，不读取Gameplay Timeline或MotionCurve sample。Turn的作者Yaw由同一Sequence sample驱动`RootOrientationWarp`，且不创建Action committed sample history。

因此每个Simulation Tick产生的Action Sample是校准锚点，不是“采一次骨骼Pose”，也不是要求Animancer在该Tick直接前进一步。真正的Clip采样、Slot过渡和最终Pose求值仍发生在每个PresentationFrame。

动画表现帧使用真实staged transaction：

```text
Begin
  stage Action inbox read与registry mutation
  stage sample projector与Marker cursor
  stage PoseState/provider/Slot/Transition workspace
  stage source usage与release completion
Evaluate唯一Pose Plan
Commit
  commit全部workspace
  acknowledgement
  lifecycle与retirement
  diagnostics
  FinalAnimationPoseFrame
Rollback
  不消费command
  不推进Action phase
  不发布部分Pose或completion
```

sequence、request workspace、source continuity、usage和completion identity分别由产生它们的Module分配，必须保存identity domain，禁止跨domain比较裸`ulong`。

业务取舍：staging需要更多有界workspace，但能保证Action command、PoseState、Marker、Slot和Final Pose属于同一帧结果，不会在Evaluate失败后留下半推进状态。

## Decision 14: 编译计划、Module所有权与source readiness

`CharacterAnimationPresentationRuntime`是唯一外层协调器，但不得把全部实现重新集中到一个巨型Pose Runtime。运行Module固定为：

```text
CharacterAnimationPresentationRuntime
  CharacterActionPlaybackRuntime
  PoseStateAndSourceRuntime
  AnimationSlotRuntime
  PosePlanExecutionRuntime
  PhysicalPoseSourceRegistry
  MarkerSync runtimes
```

- `PoseStateAndSourceRuntime`拥有PoseState workspace、state relevance、Sequence/BlendSpace/MM provider demand与source readiness。
- `AnimationSlotRuntime`拥有Source Pose/Action占用、Transition Routing、weight、Stored Pose capacity和Action usage。
- `PosePlanExecutionRuntime`只装载Projection中的native plan、执行Pose operation和发布operation completion。
- `PhysicalPoseSourceRegistry`只拥有playable/capture的创建、采样与物理释放。
- 外层协调器只按编译顺序交换typed frame，不扫描内部Player或Dictionary。

`AnimationPosePlayableGraphRuntime`中用于反查Action生命周期的`CollectRetainedPlaybackDemand`、`RetainsPlayback`、`TryGetPlaybackStatus`、`TryGetHandoffSource`、按channel发布Selection和扫描Player等接口必须删除。保留的source-neutral执行能力迁入上述正式Module。

PoseState target使用两阶段ready barrier：

```text
选择候选target
  -> 发布target provider demand
  -> provider返回Pending / Ready / Invalid
  -> Ready时提交Transition Routing
  -> 采样共同可见source并执行Pose Plan
```

- Entry required source为Pending时不发布Final Pose。
- 已有合法source时target Pending只保持当前source，不提交transition generation。
- Invalid必须报告typed failure并阻止frame publication。
- 不得用历史Selection、bind pose、默认Idle或旧Timeline作为fallback。

Transition Routing的exact plan、endpoint matrix、capture/release request layout必须由Projection Compiler完整生成。角色Runtime只能装载并校验`PlanId/Revision`，不得调用`TransitionRoutingCompiler.Compile`。

业务取舍：编译产物会更完整，Projection schema也需要提升，但正式运行、Preview和调试使用完全相同的计划，运行帧不再临时重建路由。

## Decision 15: PoseState authoring存储与producer binding必须收紧

State仍在作者语义上拥有inline Pose subgraph，但Unity序列化不再递归内联`CharacterPoseGraphData`。`CharacterPresentationPoseGraphAsset`使用root-owned graph catalog：

```text
PoseGraphId -> flat CharacterPoseGraphData record
PoseState -> stable PoseGraphId
PoseSubgraph call -> stable PoseGraphId
```

Compiler对catalog执行可达性、唯一输出和递归调用检查；Runtime只读取编译后的flat plan。Editor导航、Undo、source map和validator都按GraphId工作，不依赖嵌套对象引用或递归序列化。

Action producer authoring收窄为有限Timeline Action：

- `AnimationProducerPresentationBinding`不再提供Motion Matching或Blend Space producer kind。
- Profile Inspector和authoring mutation只能创建、修改或删除有限Action Timeline producer binding。
- Blend Space、Motion Matching与Sequence使用`PresentationPoseSourceBinding`和state-local provider descriptor。
- Projection Compiler拒绝把MM/Blend Space绑定到Gameplay channel或Action Playback Input。

对应binding index拆为Action-only index与Pose source/provider index。Action lifecycle只读取前者；PoseState/source runtime只读取后者。

业务取舍：作者不能再把任意表现source塞进Gameplay producer列表，但界面会准确表达“有限Action Timeline”和“持续Pose source”两类业务，减少错误接线与两套选择权。

## Decision 16: 启动、Reset、Preview与Diagnostics使用正式分层

删除`RequireCommittedSelection`、`AwaitCommittedSelection`和以Action Selection判断`HasRequiredOutput`的启动策略。动画表现启动只检查：

```text
committed Body/Fact有效
Projection与Pose Plan有效
Entry PoseState有效
Required Pose source Ready
```

没有Action时不得构造空playback或等待Action Runtime。

Reset原因固定为：

| 原因 | Action inbox/registry | committed raw history | PoseState/source | Slot/Marker | MM |
|---|---|---|---|---|---|
| PresentationReset | 清空 | 清空 | 清空 | 清空 | 清空 |
| BodyDiscontinuity | 保持identity并重基线 | 保持committed事实 | reset/rebase | reset effective relation | reset/rebase |
| ActionCommandReplace | 只替换目标entry | 替换对应history | 不变 | 按新handoff | 不变 |
| PreviewSeek | 清空后由fixture重建 | 清空 | 清空 | 清空 | 清空 |
| ProjectionReplacement | 逆序销毁 | 清空 | 逆序销毁 | 逆序销毁 | 逆序销毁 |

Preview保持一个正式`AnimationPreviewRuntime`，输入分为：

- `TimelineActionPreviewAdapter`生成session-scoped非零ActionInstance并写入Action inbox。
- `PoseGraphFactPreviewAdapter`只提交Fact，不创建虚假Action playback。
- `MotionMatchingQueryPreviewAdapter`只提交state relevance/query fixture，不创建Gameplay producer或PlaybackId。

Diagnostics分别发布：

- `ActionPlaybackLifecycleSnapshot`
- `PosePlanRuntimeSnapshot`
- `AnimationSlotRuntimeSnapshot`
- Action Marker relation与PoseState Source Sync relation
- 最终`AnimationPresentationDebugView`

Action snapshot不包含PoseNode weight，Pose snapshot不拥有Action lifecycle。外层协调器只在成功commit后组合只读Debug View。

## Atomic Migration

实施分为模块构建和一次正式切换：

1. 先完成Action/Pose source ABI、Action command inbox、逐playback registry、sample projector、typed usage/release completion与整帧事务。
2. 完成root-owned Pose graph catalog、typed compiled plans、source readiness barrier和运行Module拆分。
3. 直接把已实现的Transition Routing模块按Projection plan接入PoseState edge和Slot；Runtime不重新编译。
4. 把`CharacterAnimationPlaybackRuntime`一次性替换为`CharacterAnimationPresentationRuntime`与`CharacterActionPlaybackRuntime`，迁移正式运行、Preview、diagnostics、command acknowledgement和release调用方。
5. 删除旧共享Selection ABI、BindingIndex、Pose Runtime playback查询、旧总管名字、旧debug和旧preview分支。
6. 运行Module与编译合同全部闭合后，最后执行既有Corin Pose Graph、Profile、Gameplay Graph与generated artifact原子迁移，并删除BaseLocomotion channel、Locomotion producer binding、ActionOverride和旧selection数据。

期间不得提供角色级开关在旧BaseLocomotion Selection与PoseStateMachine之间切换。

## Rejected Alternatives

### 保留BaseLocomotion Selection并让PoseStateMachine消费它

PoseStateMachine只能在已经选定的动画内部做第二次包装，无法决定Idle/Walk/Run，仍然保留Gameplay动画权威。拒绝。

### PoseStateMachine直接读取BTSMTL Blackboard

接线少，但会让表现图依赖Gameplay mutable address、Numeric Target ABI和状态命名，无法保持Projection target-neutral。拒绝。

### 把Attack做成PoseStateMachine State

会把Action准入、连段和Timeline生命周期复制到表现层，并重新产生Any State蜘蛛网。拒绝。

### 删除Action Timeline，只发送Play Animation事件

会分裂Motion、Window、Cue和动画时间，破坏现有Action闭环。拒绝。

### Slot直接修改Character Transform

会绕过Character Motor、WorldSolver、预测与碰撞。拒绝。

### 保留CharacterAnimationPlaybackRuntime作为全部动画总管

可以减少类迁移，但会让有限Action实例寿命继续和PoseState、Motion Matching、Pose Plan求值共享一个owner；Locomotion虽然删除Gameplay selection，运行时仍然被名义上的Playback总管控制。拒绝。

### 删除Action Playback并让Slot直接消费Gameplay command

可以减少一层对象，但Slot将同时拥有command保序、Timeline raw time、PendingFirstSample、retention、transition与Pose输出，无法保持有限动作实例和Pose插入点分离。拒绝。

## Spec Conflicts And Resolution

- `character-animation-selection-runtime`当前要求BTSMTL唯一仲裁每个AnimationChannel的Gameplay winner，并以Run到Stop为BaseLocomotion示例。本change把该要求收窄到有限Action playback；Locomotion PoseStateMachine只读取Presentation Fact。
- `character-animation-layer-runtime`当前要求Base pose、Idle、Move来自Graph/State选择的Timeline producer。本change替换为Presentation Sequence/BlendSpace/MM source。
- `character-state-timeline-authoring-loop`当前要求Corin Locomotion至少包含按动画命名的状态、Timeline和ActionOverride。本change删除这些硬要求，保留真正的Gameplay movement control与Action StateMachine。
- `character-action-authoring-closure`当前要求`HasActionLocomotionOwnership`让Locomotion进入ActionOverride。本change以Motion arbitration取代，并让PoseStateMachine持续求值。
- `character-animation-pipeline`当前把SimulationTick提交AnimationChannel winner作为全部动画输入。本change增加Presentation Fact输入，并把exact winner限制到有限Action Timeline。
- `character-animation-pipeline`与`project.md`当前把`CharacterAnimationPlaybackRuntime`描述为正式运行和Preview共用的动画总调度。本change将其拆为`CharacterAnimationPresentationRuntime`整帧协调器与`CharacterActionPlaybackRuntime`有限动作生命周期运行时；归档时必须更新current truth，不保留旧名转发层。
- `character-animation-selection-runtime`当前让Timeline与Motion Matching共用`AnimationSelectionFrame`，并让Selection Input保存channel和producer。本change把有限Action改为Action command/frame，把MM、Blend Space与Sequence改为state-local`PresentationPoseSourceSample`，删除跨两类来源共用的Selection Input与binding index。
- `character-presentation-pose-graph`当前把Selection Input定义为Program channel或MM producer output。本change只保留有限Action Playback Input；PoseState source provider使用独立typed plan，不再回指Gameplay producer。
- `character-animation-presentation-authoring`当前按Gameplay producer与Selection Input组织Profile producer binding。本change把producer authoring收窄为有限Action Timeline，持续Pose source只使用Presentation source/provider binding。
- 当前PoseState authoring实现把`CharacterPoseGraphData`递归内联进State，和Unity可序列化深度不兼容。本change保留State拥有inline subgraph的作者语义，但把存储改为root-owned graph catalog与stable GraphId。
- 旧`integrate-animation-transition-routing-pipeline`的Corin topology和任务19依赖BaseLocomotion Selection，且尚未实施，已经由本change吸收并删除。
- `add-character-presentation-blend-space`仍有旧Playback协调器名字且缺少state-local resolver迁移任务，已经重基线为PoseState provider plan、Presentation source sample与readiness。
- `add-character-motion-matching-pose-source`与`refactor-motion-matching-presentation-module`原先仍保留MM Program producer、PlaybackId与共享Selection frame结论，已经重基线为PoseState relevance、state-local source sample与`CharacterAnimationPresentationRuntime`协调。

## 2026-08-01 MovingTurn相反方向交接

### 问题链路

Corin `MoveAxis`使用Unity Input System的Dpad composite。该composite在W与S或A与D同时按下时把相反分量相消为零。输入适配器每个表现帧只采样一次，再把该值锁存给同一表现帧内的多个60Hz模拟Tick。因此W到S的自然手指交接会产生持续若干模拟Tick的零输入：`MoveAxis=(0,0)`让`RunLoop -> MovingTurn`条件失败，同时让`RunLoop -> RunEnd`成立。问题发生在Gameplay角度判断之前，不是Pose Graph或Transition Routing漏播。

保留同一侧方向键时还存在独立的正确业务结果。例如W+A切到S+A的目标方向只变化90度，不应触发固定180度MovingTurn；W切到S+A恰好位于135度门槛，Body已经发生的正常转向会使它低于门槛。这部分继续由正式`MoveFacingAngle`判断，不通过输入解析伪造成180度。

### 决策

`CharacterInputValueDefinition`为Vector2输入保存显式数字方向冲突策略。默认策略保持Unity原始相消；Corin `MoveAxis`显式选择`LatestActuatedCardinal`。输入适配器启用InputAction后监听该action的started、performed与canceled事件，并在每次表现帧采样时比较四个数字part的按下边沿，根据实际激活顺序更新横纵轴最近方向。回调提供及时顺序，采样边沿覆盖Input System因composite驱动控件选择而未单独通知某个part的情况。采样时只在同一轴的正负part同时按下时用最近激活方向替换该轴；没有冲突时完全保留InputAction原值。

解析发生在Camera-relative转换与portable input构造之前。Float32、Fixed、本地和Rollback本地玩家因此记录同一已经解析的`MoveAxis`；replay与网络继续使用现有canonical输入，不保存键盘状态或解析器状态。解析器状态只描述本地物理设备的按键先后，不属于Gameplay Simulation State。

### 拒绝方案

- StateMachine增加一到数Tick零输入宽限：会延迟真实停步，而且零帧本身没有新目标方向，仍不能可靠进入MovingTurn。
- 增加`RunEnd -> MovingTurn`：复制准入逻辑并违反RunEnd先回到唯一RunLoop入口的正式链路。
- 降低135度门槛：无法修复W/S相消，还会把真实90度转向错误识别为180度Turn。
- 固定让S或D胜出：W到S与S到W行为不对称，作者无法得到一致手感。

## 2026-08-02 MovingTurn连续CrossFade抢占

### 问题链路

Corin MovingTurn的Gameplay根运动Timeline在第28帧完成，Turn Pose Clip正文为71帧，离开Turn到Walk、Run或Idle使用0.3秒Standard Blend。第28帧是作者选择的根运动与逻辑退出边界，不要求播放完整71帧Pose Clip；剩余姿势由CrossFade接回持续移动。

Standard Blend开始后，旧Runtime仍把Turn source保存为逻辑active State，直到0.3秒混合完整结束才切换为Walk、Run或Idle target。因此混合期间新的committed MovingTurn事实只能继续检查Turn的出边，无法检查当前target的`Walk/Run -> Turn`入边。新的Gameplay MovingTurn和根运动已经开始，Pose却要等待旧CrossFade结束后才重进Turn，形成根运动与动画重启错相，并让连续转身看起来像叠了两层混合。

### 决策

Standard Blend提交成功后，target立即成为Pose StateMachine的逻辑active State；source与target仍作为共同可见Pose source保持ready和采样，直到混合完成或被正式替换。active transition期间继续从target State的编译Transition Rule读取最新Presentation Fact。若新的事实命中不同Transition，沿既有Transition Routing、selection generation和native control generation替换旧transition。

连续MovingTurn期间，Walk或Run target命中既有`Walk/Run -> Turn` Inertialization。Turn的`Always Reset on Entry`在target demand阶段把Sequence Player重置到第0帧，既有Inertialization从当前最终混合Pose重捕获后接入新Turn。Gameplay Timeline、Root Motion曲线、0.3秒退出混合参数、IK、FootPlacement、snapshot和网络协议均不改变。

### 业务取舍

- 不增加Gameplay冷却：不会把快速反向输入吞掉，也不会让普通移动在Turn后额外停住。
- 不延长28帧Timeline到71帧：不会为了播放完整收势而继续占用Gameplay MovingTurn或冻结正常移动。
- 不建立Turn自循环边：当前Movement Mode在一次Turn内持续为Turn，单靠自循环规则无法区分同一次播放与下一次激活，反而会逐帧或按固定时间重复重置。
- 不创建第二层CrossFade：新Transition使用唯一Routing替换旧实例，保持同一StateMachine最多一个active transition。

# Design: Motion Matching 表现 Module 深化

## 当前基线

本设计针对未启用MM内容时已经存在的条件式代码，不是当前角色运行链。Corin和其它正式Definition都没有MM payload，所以下述Module边界目前不会被正式运行时构造；实际继续MM工作的入口是先完成独立正式配置与Projection接入。

`refactor-animation-control-boundaries`安装的`PresentationPoseSourceSample`、显式Player、显式`BlendStack`、局部`Inertialization`与编译Pose Plan是本change的直接基线。Module Resolve最终输出state-local sample batch；Complete按PoseNodeId和Pose Plan completion读取正式Pose Value。旧request、Gameplay playback identity、每PoseSlot固定Stack和Base Slot完成合同只属于删除清单。

## Context

当前代码已经创建`CharacterMotionMatchingPresentationModule`，并把Trajectory Adapter、provider runtime、sampling、frozen output、history、Replay与Reset移入Module；Factory负责构造并转移所有权，Simulation Presentation只委托Body/Intent。Module与PoseState relevance、State内部Player之间已经使用最终Selection和completion合同：

```text
CharacterAnimationPresentationRuntime
  -> 从PoseState relevance建立固定MM demand
  -> 调用Module ResolveFrame取得MotionMatchingFrameResolution
  -> PoseState runtime批量提交PresentationPoseSourceSample
  -> Pose Plan发布MotionMatchingPosePlanCompletion
CharacterMotionMatchingPresentationModule
  -> internal trajectory / producer / query / search / plan / selection
  -> 返回固定state-local Selection batch
  -> 只消费typed Pose Plan completion
```

旧Gameplay demand、Request wrapper和Playback的MM结果遍历已经删除。PoseState relevance生成固定demand，Module返回State内部Selection batch；Pose Plan发布显式completion，Module只消费该completion中的Player source usage、绑定PoseNode结果与Foot Feature aggregate，不再持有Pose Runtime查询Interface。正式内容配置由后续`add-character-motion-matching-pose-source`创建，不是本重构的前置条件。

## Goals

- 让外部调用者只提交正式Body frame与可选Accepted Intent，不识别trajectory Adapter具体类型。
- 让PoseState/Playback编排层只知道“一组State relevance demand、一组State内部Selection、一份completion”，不直接拥有MM算法状态。
- 让Remote Body、Prediction、Replay与Preview变化集中在MM Module内部。
- 保留唯一PoseState relevance、编译Pose Plan、显式Player、PlayableGraph Evaluate与world-aware FootPlacement阶段。
- 无MM payload时不创建Module、不分配MM workspace，也不发布伪能力状态。

## Non-Goals

- 不改变MM Feature Schema、Database、Admission、Exact Search、Plan或Selection算法。
- 不把MM状态写入Gameplay State、Snapshot、Hash或Network packet。
- 不让Program、AnimationChannel winner或Gameplay playback参与MM relevance与Selection。
- 不让Module拥有Player source usage、Blend Stack transition/Stored Pose、Inertialization residual、Pose Graph或FootPlacement算法。
- 不为旧trajectory Interface保留兼容Adapter。
- 不接入第三方MxM Runtime，不引用`MxMAnimator`、`MxMSearchManager`、`IMxMTrajectory`、MxM Mixer、Layer、Transition、Root Motion或PlayableGraph。

## Ownership Decision

`CharacterAnimationPresentationRuntime`继续是通用动画表现协调器，并唯一持有一个可空的`CharacterMotionMatchingPresentationModule`字段。Module是其内部深Module，不是与协调器并列的第二运行时。

| 职责 | 唯一Owner |
|---|---|
| Body interval、selected cursor、visual correction | `CharacterBodyPresentationRuntime` |
| MM trajectory Adapter、Intent缓存、Selected sequence | `CharacterMotionMatchingPresentationModule` |
| MM producer、query、selection、history、frozen output | `CharacterMotionMatchingPresentationModule` |
| MM relevance与State内部Selection消费 | `PoseStateMachine` / State内部显式Player |
| 直接source usage与discontinuity | 显式`SelectedPosePlayer`节点 |
| 多source retention、CrossFade、Stored Pose与release | 显式`BlendStack`节点 |
| 单Pose history、residual与rebase | 局部`Inertialization`节点 |
| 姿势合成、world-aware阶段与最终姿势 | 唯一编译Pose Graph Plan |

Module可在Implementation内部组合多个producer runtime、两个trajectory Adapter、固定buffer和Replay helper；这些内部对象不成为外部Interface。

## Composition

Factory仍是构造期唯一组合入口，但只做所有权转移：

1. Projection无MM payload时向动画表现协调器传入无Module配置。
2. Projection有MM payload时，以ActorId、Body SourceMode、Projection identity、Rig payload和MM payload构造唯一Module。
3. Module在内部选择Accepted Intent或Selected Body Adapter。
4. Factory把Module所有权交给`CharacterAnimationPresentationRuntime`，不把Adapter引用交给Simulation Presentation。
5. 任一后续构造失败时，Module由统一Animation Module lifetime顺序释放。

这不是运行时feature toggle。Module是否存在只由已校验Projection决定，构造后不按Network Model、场景名、Actor名或帧输入切换。

## External Interface Shape

外部Interface保持小而明确，具体名称在实施时可按现有命名统一，但语义必须包含：

- `Enabled`：当前Projection是否真正构造MM Module。
- `AcceptsTrajectoryIntent`：当前正式Body SourceMode是否接受Intent输入。
- `CaptureTrajectoryIntent`：提交单调sequence、匹配Actor与ResetSequence的正式Intent。
- `ResolveFrame`：消费本帧Body、表现时间、reset identity和PoseState relevance demand，返回固定容量State内部`PresentationPoseSourceSample`集合与completion identity。
- `CompleteFrame`：在绑定PoseNode的Pose Plan阶段完成后，以匹配completion identity追加history并完成清理。
- `TryCaptureSearchReplay`：从当前正式producer读取Replay。
- `Reset`与`Dispose`：原子清理全部MM状态。

调用者不读取trajectory Adapter、不拉取nullable frame、不判断Accepted Intent或Selected Body具体类。

## Frame Transaction

正式顺序固定为：

```text
Body.Present
  -> PoseState relevance resolution
  -> MM Module ResolveFrame
       -> resolve internal trajectory Adapter
       -> query / search / plan / selection
       -> PresentationPoseSourceSample batch
  -> relevant State内部MM Player input
  -> SelectedPosePlayer
       -> optional local Inertialization
     or explicit BlendStack
  -> Pose Graph Plan Evaluate once
  -> MM Module CompleteFrame
       -> copy bound PoseNode completed pose
       -> append Pose History or mark typed gap
       -> prune selection output unused by every Player
  -> FootPlacement world-aware phase
  -> FinalAnimationPoseFrame
  -> Camera
```

Resolve与Complete属于同一个逻辑帧事务，必须通过非零且单调的completion identity关联。以下情况必须失败：

- 同一PresentationFrame重复Resolve。
- 未Resolve就Complete。
- completion identity、presentation frame或reset identity不匹配。
- 上一帧未Complete又开始下一帧。
- 绑定PoseNode没有合法完成Pose却追加history。

绑定PoseNode没有合法完成Pose时Module记录typed history gap并完成事务；不得复制上一帧Pose或伪造bind pose。

## PoseState Relevance Demand

PoseStateMachine按当前active/target State、transition可见性和State source usage生成固定`MotionMatchingPoseStateDemand`buffer。每项只包含Module解析所需的稳定provider identity、State index、Player index、relevance weight与reset identity，不携带Gameplay AnimationChannel、AnimationPlaybackId、PoseSlot或transition算法。

Module不得读取Gameplay State、Action、Tree route、Priority或Action候选列表，也不得查询AnimationChannel winner。PoseState relevance是唯一MM demand；Module返回state-local sample，再由绑定Player进入编译Pose Plan。Action覆盖期间Locomotion State仍然relevant，因此MM query、plan、Player Pose与History继续更新。

## Retention And Frozen Output

显式Player节点共同形成某个`AnimationPoseSourceId`的唯一source usage事实。Module只负责：

- 保存仍被至少一个Player使用的不可变MM Selection output。
- 根据Pose Plan completion发布的source usage维持selection到source descriptor的精确映射。
- 在全部Player发布该source的正式release后清理frozen output；MM selection sample lifetime只服从State Player source usage，不等待Gameplay playback lifecycle。

Module不得复制Player usage、entry列表、transition clock、Stored Pose、Inertial residual或release条件。若Player报告仍使用MM source但Module缺少对应frozen output，必须typed失败，不能重新Search或改用当前winner。不能等待整个长期Locomotion Playback进入Retired才释放旧Selection Generation，否则同一Playback持续Jump会让frozen output无界增长。

## Trajectory Adapters

Accepted Intent与Selected Body是Module内部真实seam上的两个Adapter：

- Accepted Intent Adapter消费最新合法Intent，同时使用Body visible pose与velocity建立当前表现起点。
- Selected Body Adapter只消费Body target pose、velocity、yaw velocity、grounded、selected tick与sample age。

Adapter统一向Module返回`MotionMatchingTrajectorySourceFrame`，但该读帧合同不再暴露给Factory、Simulation Presentation或动画表现协调器。新增Remote Body或Prediction来源时，只有确实产生第三种输入语义才增加第三个Adapter；Network Model名称本身不能成为Adapter类型。

## Reset And Replacement

Body ResetSequence变化、Committed branch replacement、Selected stream reset、EventId Replace/Retire、Projection replacement、Presentation Reset与Dispose必须通过Module唯一入口清理：

- trajectory Adapter状态；
- Intent与Selected sequence；
- producer current domain、query、plan与selection；
- Pose History与protected contact；
- frozen output与frame completion；
- Replay capture引用与diagnostics live state。

PoseStateMachine、全部Player节点、Pose Graph Plan与Module必须按同一外层Reset顺序清理，但不得互相维护第二份reset sequence。

## Diagnostics And Replay

Module聚合producer diagnostics，但仍向统一`RuntimeDebugSession`发布现有payload。interest关闭时不得构造candidate detail集合。

Replay入口只按稳定MM provider identity委托给Module当前正式provider。Module保存Projection、Profile、Database和Artifact exact identity；identity不匹配时拒绝，不迁移旧capture。

诊断必须额外显示帧事务状态：Resolve identity、selection count、Pose Plan completion identity、Complete identity、history appended/gap、retained frozen output count和reset reason。

## Preview

Query Fixture Preview通过Editor-only显式输入创建同一种Module与正式PoseState/Pose Runtime。Fixture query可以从Module内部query seam注入，但选择结果必须继续经过正式State内部`PresentationPoseSourceSample` lowering、编译Pose Plan、显式Player和Pose Graph。

Preview不得执行Program、WorldSolver、Foot Physics或Camera，也不得创建简化MM runtime、直接Animancer Play、临时PlayableGraph或第二份history实现。

## Migration

已完成的迁移：

1. 已建立唯一Module、固定demand与Resolve/Complete事务。
2. 已把producer runtime、sampling、frozen output、history与Replay所有权移入Module。
3. 已把两个Trajectory Adapter、Intent缓存、Selected sequence与具体类型判断移入Module。
4. 已让Factory构造并转移Module，Simulation Presentation只提交Body/Intent。
5. 已接通Reset、replacement、diagnostics、Replay与Query Fixture Preview。
6. 已删除外部trajectory Interface、具体Adapter类型、旧Factory创建方法与Playback直接Append History。

剩余正式工作不再属于本Module内部重构：`add-character-motion-matching-pose-source`必须创建独立验证Definition、Profile、Source Set、Database Artifact、Projection payload和PoseState接线；缺少正式内容时保持停止，不得恢复旧request、Gameplay channel demand或临时Player。任何后续实现若必须用wrapper让新旧两套状态同步，应停止迁移并调整提交范围；正式结果不允许双写。

## Tradeoffs

### 选择：Module作为动画表现编排内部深Module

业务收益：保持唯一动画应用协调器，MM变化集中，同时Action Timeline与MM仍共享最终Pose Graph Plan，但MM relevance不再伪装成Gameplay playback。代价：外层编排仍负责在PoseState relevance之后调用Resolve、在Pose Plan之后调用Complete。

### 选择：保留relevance与source usage权威在PoseState/Player

业务收益：MM只在PoseState需要时工作，Action覆盖也不会停止Locomotion更新；连续性由显式节点决定，不会形成第二条动画路径。代价：Module必须消费PoseState生成的固定demand和Pose Plan发布的正式Player source usage结果。

### 选择：两阶段帧事务而不是单方法

业务收益：Pose History读取绑定PoseNode真正完成的Pose Value，查询仍只读上一帧history。代价：Interface必须表达completion identity和严格调用顺序，不能把history完成伪装成Resolve阶段内部行为。

### 选择：内部保留两个trajectory Adapter

业务收益：Accepted Intent和Selected Body的业务差异仍然明确，新Remote/Prediction语义有稳定扩展点。代价：Module内部增加一个聚合结构，但外部不再承受具体类型知识。

### 选择：第三方MxM只作为离线参考，不进入Runtime

业务收益：正式Runtime继续只有一套Selection、Player、Pose Plan、Reset和Replay权威，不会引入MxM自己的Search Manager、FSM、Mixer、Layer、Transition、Root Motion或更新时序。代价：不能直接复用MxM整套播放器；若以后需要复用其`MxMAnimData`，必须另建Editor-only显式Importer，把数据转换成项目正式Database Artifact。

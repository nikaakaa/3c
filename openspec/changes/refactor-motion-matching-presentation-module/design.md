# Design: Motion Matching 表现 Module 深化

## 重新基线

`refactor-animation-selection-pose-graph-boundary`覆盖本文原先的`ResolvedAnimationPoseRequest`与固定Base Slot完成合同。Module Resolve最终输出`AnimationSelectionFrame` batch；Complete按PoseNodeId和plan completion读取正式Pose Value。本文后续出现的request、每PoseSlot Stack和Base Slot均为当前实现迁移清单，不再是目标接口。

## Context

当前代码已经有深的`CharacterMotionMatchingProducerRuntime`，但producer上方的表现职责没有形成同样深的Module：

```text
Factory
  -> 创建具体Trajectory Adapter
CharacterSimulationPresentationRuntime
  -> 识别具体Adapter
  -> 缓存Intent / 生成Selected Body sequence
  -> 读取Trajectory Frame
CharacterAnimationPlaybackRuntime
  -> MM producer / sampling / output / retention / history / prune
CharacterMotionMatchingProducerRuntime
  -> trajectory / query / search / plan / selection / pose source
```

删除现有trajectory Interface并不会删除复杂度，具体Adapter判断会重新散落到Factory和Presentation；删除Playback中的MM helper也会让producer、retention、history和清理顺序重新出现在多个调用点。根据删除检验，这些职责应由一个更深Module统一隐藏。

## Goals

- 让外部调用者只提交正式Body frame与可选Accepted Intent，不识别trajectory Adapter具体类型。
- 让Playback只知道“一组MM demand、一组Animation Selection、一份completion”，不直接拥有MM算法状态。
- 让Remote Body、Prediction、Replay与Preview变化集中在MM Module内部。
- 保留唯一通用Lifecycle、编译Pose Plan、显式Player、PlayableGraph Evaluate与world-aware FootPlacement阶段。
- 无MM payload时不创建Module、不分配MM workspace，也不发布伪能力状态。

## Non-Goals

- 不改变MM Feature Schema、Database、Admission、Exact Search、Plan或Selection算法。
- 不把MM状态写入Gameplay State、Snapshot、Hash或Network packet。
- 不改变Program对AnimationChannel winner的唯一仲裁。
- 不让Module拥有Player source usage、Blend Stack transition/Stored Pose、Inertialization residual、Pose Graph或FootPlacement算法。
- 不为旧trajectory Interface保留兼容Adapter。

## Ownership Decision

`CharacterAnimationPlaybackRuntime`继续是通用动画表现运行时，并唯一持有一个可空的`CharacterMotionMatchingPresentationModule`字段。Module是其内部深Module，不是与Playback并列的协调器。

| 职责 | 唯一Owner |
|---|---|
| Body interval、selected cursor、visual correction | `CharacterBodyPresentationRuntime` |
| MM trajectory Adapter、Intent缓存、Selected sequence | `CharacterMotionMatchingPresentationModule` |
| MM producer、query、selection、history、frozen output | `CharacterMotionMatchingPresentationModule` |
| AnimationChannel selection与Playback生命周期 | `CharacterAnimationPlaybackRuntime` / `AnimationPlaybackLifecycle` |
| 直接source usage与discontinuity | 显式`SelectedPosePlayer`节点 |
| 多source retention、CrossFade、Stored Pose与release | 显式`BlendStack`节点 |
| 单Pose history、residual与rebase | 局部`Inertialization`节点 |
| 姿势合成、world-aware阶段与最终姿势 | 唯一编译Pose Graph Plan |

Module可在Implementation内部组合多个producer runtime、两个trajectory Adapter、固定buffer和Replay helper；这些内部对象不成为外部Interface。

## Composition

Factory仍是构造期唯一组合入口，但只做所有权转移：

1. Projection无MM payload时向Playback传入无Module配置。
2. Projection有MM payload时，以ActorId、Body SourceMode、Projection identity、Rig payload和MM payload构造唯一Module。
3. Module在内部选择Accepted Intent或Selected Body Adapter。
4. Factory把Module所有权交给Playback，不把Adapter引用交给Simulation Presentation。
5. 任一后续构造失败时，Module由统一Animation Module lifetime顺序释放。

这不是运行时feature toggle。Module是否存在只由已校验Projection决定，构造后不按Network Model、场景名、Actor名或帧输入切换。

## External Interface Shape

外部Interface保持小而明确，具体名称在实施时可按现有命名统一，但语义必须包含：

- `Enabled`：当前Projection是否真正构造MM Module。
- `AcceptsTrajectoryIntent`：当前正式Body SourceMode是否接受Intent输入。
- `CaptureTrajectoryIntent`：提交单调sequence、匹配Actor与ResetSequence的正式Intent。
- `ResolveFrame`：消费本帧Body、表现时间、reset identity和MM playback demand，返回固定容量Animation Selection集合与completion identity。
- `CompleteFrame`：在绑定PoseNode的Pose Plan阶段完成后，以匹配completion identity追加history并完成清理。
- `TryCaptureSearchReplay`：从当前正式producer读取Replay。
- `Reset`与`Dispose`：原子清理全部MM状态。

调用者不读取trajectory Adapter、不拉取nullable frame、不判断Accepted Intent或Selected Body具体类。

## Frame Transaction

正式顺序固定为：

```text
Body.Present
  -> MM Module ResolveFrame
       -> resolve internal trajectory Adapter
       -> query / search / plan / selection
       -> AnimationSelectionFrame batch
  -> Timeline selection resolution
  -> AnimationPlaybackLifecycle Apply
  -> MotionMatchingSelectionInput
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

## Playback Demand And Generic Lifecycle

Program仍为每个AnimationChannel提交唯一producer/playback selection。Playback按通用Lifecycle计算本帧demand，再把其中source kind为Motion Matching的项降低为固定`MotionMatchingPlaybackDemand`buffer。每项只包含Module解析所需的稳定producer index、producer identity、AnimationPlaybackId和AnimationChannelId，不携带PoseSlot或transition。

Module不得读取State、Action、Tree route、Priority或候选列表，也不得重新选择AnimationChannel winner。Timeline与MM最终都返回同一种`AnimationSelectionFrame`，再由各自Selection Input进入编译Pose Plan。

## Retention And Frozen Output

显式Player节点共同形成某个`AnimationPoseSourceId`的唯一source usage事实。Module只负责：

- 保存仍被至少一个Player使用的不可变MM Selection output。
- 根据Pose Plan completion发布的source usage维持selection到source descriptor的精确映射。
- 在全部Player发布该source的正式release后清理frozen output；Playback级sampling仍只在通用Lifecycle不再Retain该Playback后清理。

Module不得复制Player usage、entry列表、transition clock、Stored Pose、Inertial residual或release条件。若Player报告仍使用MM source但Module缺少对应frozen output，必须typed失败，不能重新Search或改用当前winner。不能等待整个长期Locomotion Playback进入Retired才释放旧Selection Generation，否则同一Playback持续Jump会让frozen output无界增长。

## Trajectory Adapters

Accepted Intent与Selected Body是Module内部真实seam上的两个Adapter：

- Accepted Intent Adapter消费最新合法Intent，同时使用Body visible pose与velocity建立当前表现起点。
- Selected Body Adapter只消费Body target pose、velocity、yaw velocity、grounded、selected tick与sample age。

Adapter统一向Module返回`MotionMatchingTrajectorySourceFrame`，但该读帧合同不再暴露给Factory、Simulation Presentation或Playback。新增Remote Body或Prediction来源时，只有确实产生第三种输入语义才增加第三个Adapter；Network Model名称本身不能成为Adapter类型。

## Reset And Replacement

Body ResetSequence变化、Committed branch replacement、Selected stream reset、EventId Replace/Retire、Projection replacement、Presentation Reset与Dispose必须通过Module唯一入口清理：

- trajectory Adapter状态；
- Intent与Selected sequence；
- producer current domain、query、plan与selection；
- Pose History与protected contact；
- frozen output与frame completion；
- Replay capture引用与diagnostics live state。

Playback的通用Lifecycle、全部Player节点、Pose Graph Plan与Module必须按同一外层Reset顺序清理，但不得互相维护第二份reset sequence。

## Diagnostics And Replay

Module聚合producer diagnostics，但仍向统一`RuntimeDebugSession`发布现有payload。interest关闭时不得构造candidate detail集合。

Replay入口只按ProgramProducerId委托给Module当前正式producer。Module保存Projection、Profile、Database和Artifact exact identity；identity不匹配时拒绝，不迁移旧capture。

诊断必须额外显示帧事务状态：Resolve identity、request count、Complete identity、history appended/gap、retained frozen output count和reset reason。

## Preview

Query Fixture Preview通过Editor-only显式输入创建同一种Module与正式Playback/Pose Runtime。Fixture query可以从Module内部query seam注入，但选择结果必须继续经过正式Animation Selection lowering、编译Pose Plan、显式Player和Pose Graph。

Preview不得执行Program、WorldSolver、Foot Physics或Camera，也不得创建简化MM runtime、直接Animancer Play、临时PlayableGraph或第二份history实现。

## Migration

迁移必须一次收敛，不能让新旧owner同时存在：

1. 先建立Module与固定帧合同，但不接入第二个运行入口。
2. 把producer runtime、sampling、frozen output、frame selection、history与Replay所有权从Playback整体移动进Module。
3. 把trajectory Adapter构造、Intent缓存、Selected sequence与具体类型判断整体移动进Module。
4. 把Playback接为唯一调用者，并让Simulation Presentation只提交Body/Intent。
5. 接通Reset、replacement、diagnostics与Preview。
6. 删除旧Interface暴露、旧字段、旧helper和旧Factory创建方法。
7. 搜索并拒绝任何残留`is AcceptedIntent...`、`is SelectedBody...`、Playback直接持有producer runtime或直接追加MM History的调用点。

任何阶段若必须用wrapper让新旧两套状态同步，应停止迁移并调整提交范围；正式结果不允许双写。

## Tradeoffs

### 选择：Module作为Playback内部深Module

业务收益：保持唯一动画应用协调器和唯一Playback入口，MM变化集中，同时Timeline与MM继续共享Lifecycle、Player节点集与Pose Graph Plan。代价：Playback仍负责调用Resolve/Complete两个阶段，但不再知道其内部状态。

### 选择：保留通用Lifecycle与source usage权威在Playback/Player

业务收益：Timeline与MM具有完全相同的Pending、Selected、Retained、Retired语义，而连续性由显式节点决定，不会形成第二条动画路径。代价：Module必须消费经过Playback降低的MM demand buffer和正式Player source usage结果。

### 选择：两阶段帧事务而不是单方法

业务收益：Pose History读取绑定PoseNode真正完成的Pose Value，查询仍只读上一帧history。代价：Interface必须表达completion identity和严格调用顺序，不能把history完成伪装成Resolve阶段内部行为。

### 选择：内部保留两个trajectory Adapter

业务收益：Accepted Intent和Selected Body的业务差异仍然明确，新Remote/Prediction语义有稳定扩展点。代价：Module内部增加一个聚合结构，但外部不再承受具体类型知识。

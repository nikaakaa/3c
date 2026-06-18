# Character Body Slot Data Model Goal

> 历史目标记录：本目标已通过 `refine-character-body-slot-data-model` 落地并归档。当前架构真相以 `openspec/specs/character-body-slot-data-model/spec.md` 和相关 active spec 为准；本文只保留讨论脉络，不再作为新的任务清单或目标架构入口。

## 背景
当前角色主线已经收口到 `CharacterRuntimeCore -> CharacterFramePipeline -> CharacterBehaviorSubmissionRunner`。这个方向的核心不是恢复旧 FullBody 主状态机，也不是把 Ref 或 BBB 的层级直接搬进来，而是让多个行为来源以纯数据方式提交候选输出，再由角色级计划统一选择和应用。

现在需要先统一数据模型语言。否则 `FullBody`、`UpperBody`、`Facial`、`CommittedAction`、`Animation Layer` 会被混成同一类概念，后续 Editor、Timeline、Preview 和 runtime 扩展都会变得不可解释。

## 目标
- 明确行为来源、动作语义、身体占用、仲裁 slot、输出通道和表现层之间的边界。
- 明确当前项目已有的数据模型事实。
- 明确 `FullBody`、`UpperBody`、`Facial` 在目标模型中应该处在哪一层。
- 在用户确认前，不新增 FullBody 节点、UpperBody runtime、Facial slot 或第二条表现/runtime 路径。
- 为后续是否创建 OpenSpec change 提供共同语言。

## 非目标
- 不重写 `CharacterRuntimeCore`。
- 不重写 `CharacterFramePipeline`。
- 不把 BBB 的 FullBody / UpperBody 状态机结构直接搬进当前项目。
- 不把 Ref/wly970123 的 TimelinePlayer、PlayableGraph 或 runtime runner 作为正式 gameplay。
- 不把 FullBody、UpperBody、Facial 直接做成三个平级 behavior graph 节点。
- 不在未确认前实现 UpperBody、Facial、IK、Additive 或等价并行表现层。

## 当前事实
当前项目已有的身体仲裁模型在：

- `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Model/CharacterBodyArbitration.cs`
- `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Runtime/DefaultBodyArbiter.cs`
- `3cDemo/Client/3C_Client/Assets/Scripts/Character/Pipeline/Model/CharacterFramePlan.cs`

当前模型已经存在：

```text
CharacterBodyDomain:
- None
- Locomotion
- CommittedAction
- UpperBody

BodyOccupancyKind:
- None
- FullBody
- UpperBody

CharacterFrameOutputChannel:
- Motion
- Animation
```

当前模型真正拥有的仲裁位置是：

```text
Base slot:
- Locomotion
- CommittedAction

UpperBody slot:
- UpperBody
```

也就是说，当前不是 `FullBody slot / UpperBody slot / Facial slot` 三个平级 slot。当前更接近：

```text
BaseSlotOwner
UpperBodySlotOwner
```

`BaseLayerOwner` 是历史错误命名。它想表达的是 BaseSlot 的 gameplay owner，却用了 Layer 这个表现层词。Layer 应该留给 Animancer layer、Animator layer、AvatarMask 这类 presentation 概念，不应该出现在身体仲裁结果里。

所以这里说的“更新”，不是增加一套新的动画层字段，也不是把 BBB 的 slot/layer 原样搬过来，而是把 runtime、compiler、editor adapter 和测试统一改成下面这套读取口径：

```text
claim kind: FullBody / UpperBody
slot owner: BaseSlotOwner / UpperBodySlotOwner
channel output: Motion / Animation / Window / Cue / facts
presentation: Animancer layer / AvatarMask / Timeline view
```

`FullBody` 在当前模型里是 claim，不是独立 slot。它表达“这个动作要全身接管”，仲裁结果通常是：

```text
BaseSlotOwner = CommittedAction
UpperBodySlotOwner = None
AllowUpperBody = false
```

`UpperBody` 在当前模型里已经有 domain 和 claim，但还不是完整运行时 source。它是已预留的数据 slot / 扩展位。

`Facial` 或 `FaceBody` 当前不在身体仲裁模型里。项目当前没有 `FacialCandidate`、`FacialOwner`、`FacialClaim` 或对应 frame plan 字段。

## 分层模型
后续所有设计先按六层理解。

### 1. 行为来源层
这一层回答：谁在本帧提交候选输出。

当前正式来源：

```text
LocomotionSource
CommittedActionSource
```

未来可能来源：

```text
UpperBodyActionSource
FacialExpressionSource
CueSource
IKSource
```

来源不是身体部位，也不是动画层。来源只表示“谁提交数据”。

### 2. 动作语义层
这一层回答：动作或行为是什么。

示例：

```text
Dodge
Attack
Shoot
Reload
HitReact
Death
Expression
```

`Dodge` 是动作语义。`Shoot` 是动作语义。`FullBody` 和 `UpperBody` 不是动作语义。

### 3. 身体占用层
这一层回答：这个动作声明占用身体哪里。

当前已有：

```text
FullBody claim
UpperBody claim
```

含义：

```text
FullBody claim:
  动作要全身接管，通常压制 Locomotion 的 base 输出和 UpperBody 输出。

UpperBody claim:
  动作只占上身，通常允许 Locomotion 继续拥有 base slot。
```

### 4. 仲裁 slot 层
这一层回答：这一帧每个可仲裁位置最终归谁。

当前目标先保持：

```text
Base slot:
  None / Locomotion / CommittedAction

UpperBody slot:
  None / UpperBody
```

未来如果确认需要 Facial，必须先决定它是：

```text
纯表现 channel
独立 presentation slot
参与 gameplay 仲裁的 slot
```

这三种不是同一个设计。

### 5. 输出通道层
这一层回答：被采用的来源输出什么。

当前已有：

```text
Motion
Animation
```

Committed Action Timeline 还会产生或间接影响：

```text
Window
Cue
Runtime facts
Input consume
Diagnostics
```

后续可能扩展：

```text
IK
FacialAnimation
BlendShape
VFX
SFX
CameraCue
```

这些通道不一定都需要进入 BodyArbiter。是否进入仲裁取决于它们是否和其他来源互斥。

### 6. 执行表现层
这一层回答：最终怎样播、怎样动、怎样表现。

示例：

```text
MotionExecutor / CharacterMotionDriver
Animancer base layer
Animancer upper-body masked layer
Animancer facial layer
VFX presenter
SFX presenter
Camera presenter
IK presenter
```

表现层不是数据模型权威。表现层只消费 `CharacterFramePlan` 和 frame output 的最终结果。

## 例子

### 普通移动
```text
来源:
  LocomotionSource

候选:
  Locomotion motion
  Locomotion animation

claim:
  None

仲裁结果:
  BaseSlotOwner = Locomotion
  UpperBodySlotOwner = None
```

### Dodge
```text
来源:
  CommittedActionSource

动作:
  Dodge

claim:
  FullBody

channels:
  Motion
  Animation

仲裁结果:
  BaseSlotOwner = CommittedAction
  UpperBodySlotOwner = None
  AllowUpperBody = false
```

### 未来边跑边射击
```text
来源:
  LocomotionSource
  UpperBodyActionSource

动作:
  Shoot

claim:
  UpperBody

channels:
  Animation

仲裁结果:
  BaseSlotOwner = Locomotion
  UpperBodySlotOwner = UpperBody
  AllowUpperBody = true
```

### 未来表情
表情不应默认进身体仲裁。需要先选定语义：

```text
方案 A: Facial 只是表现 channel
  适合普通表情、口型、眨眼。

方案 B: Facial 是独立 presentation slot
  适合需要和其它表情互斥、可预览、可覆盖的表情层。

方案 C: Facial 是 gameplay slot
  适合表情与受击、状态、交互强绑定的设计。
```

未确认前，不新增 `FaceBody`。

## 命名边界
建议后续文档和代码用词保持下面的边界：

```text
Source:
  谁提交候选输出。

Action:
  具体动作语义，例如 Dodge、Shoot、Attack。

Claim:
  动作声明占用身体资源，例如 FullBody、UpperBody。

Slot:
  仲裁后的资源位置，例如 BaseSlot、UpperBodySlot。

Channel:
  输出类型，例如 Motion、Animation、Window、Cue。

Presentation Layer:
  表现实现，例如 Animancer layer / AvatarMask / presenter。
```

避免继续使用下面的混合说法：

```text
FullBody 节点
FullBody-as-owner 说法
FaceBody
UpperBody 只是表现层
CommittedAction 等于 FullBody
Animancer layer 等于 gameplay slot
```

## 当前设计判断
当前 runtime 的正确方向是：

```text
CharacterRuntimeCore
-> CharacterFramePipeline
-> CharacterBehaviorSubmissionRunner
-> LocomotionSource + CommittedActionSource
-> BodyArbiter
-> CharacterFramePlan
-> OutputApplier / Presenter
```

当前不应该新增：

```text
第二 motion executor
第二 animation presenter
第二 blackboard writer
第二角色控制入口
FullBody 运行时主状态树
UpperBody runtime source
Facial runtime slot
```

除非先通过新的 OpenSpec 明确需求和边界。

## 当前执行口径
本轮文档入口按下面口径更新：

```text
BaseSlot:
  当前正式基础身体仲裁位置。

UpperBodySlot:
  当前正式上身扩展仲裁位置，但不代表 UpperBody runtime source 已完成。

FullBody:
  claim，不是 slot、source、graph node 或 gameplay owner。

Dodge:
  Action.Dodge，由 CommittedAction source 提交 FullBody claim。

Facial / FaceBody:
  未进入当前 BodyArbiter 和 frame plan。后续必须先通过 OpenSpec 决定是 channel、presentation slot 还是 gameplay slot。
```

## 实施检查结果
`CharacterBodyArbitration` 当前模型检查结论：

```text
CharacterBodyDomain.CommittedAction:
  表示 CommittedAction source 的 FullBody claim 被采纳后的 action-side BaseSlot owner。

BodyOccupancyKind.FullBody:
  claim kind，不是 slot。

BodyOccupancyDecision.BaseSlotOwner:
  当前正式基础身体 slot owner 读取面。

BodyOccupancyDecision.UpperBodySlotOwner:
  当前正式上身 slot owner 读取面。

BodyOccupancyDecision.UpperBodySlotSuppressed:
  当前正式上身 slot 压制读取面，FullBody claim 被采纳时为 true。

旧 layer owner 读取面:
  已从正式读取面删除，后续不再作为兼容入口保留。

UpperBody:
  当前只表示 claim/slot 扩展位和候选合同，不表示 runtime source 已实现。

Facial / FaceBody:
  当前 runtime source、BodyArbiter 和 CharacterFramePlan 均不引入。
```

本变更已将正式运行时和测试中的旧 action-side owner、旧 committed action candidate 和旧 locomotion preemption reason 收敛为 CommittedAction 口径。`FullBody` 只保留在 `BodyOccupancyKind.FullBody` 和 `CommittedActionFullBody` claim factory 中，用来表达 claim kind。

## 历史开放问题
1. `UpperBody` 是近期要做的正式 runtime source，还是只保留合同扩展位？
2. Facial 未来是表现 channel、presentation slot，还是 gameplay slot？
3. Timeline 的 track 是否应该按 channel 分组，例如 Animation / Motion / Window / Cue / Facial / IK？
4. Editor 里要不要把 claim 显示成单独 lane，而不是把 FullBody 做成节点？

这些问题不再是 `refine-character-body-slot-data-model` 的归档阻塞项。后续如果要实现 UpperBody、Facial、IK、Additive 或新的 timeline track 分组，必须另开 OpenSpec change 明确 source / action / claim / slot / channel / presentation layer 的归属。

## 已处理的旧术语风险
本 goal 曾记录 active specs 和 goal 文档中存在的历史口吻风险，容易让人误解为旧 FullBody 主线、FullBody 状态树或 FullBody presenter 仍是目标架构。当前已按归档后的语义更新这些入口：FullBody 保留为 claim 或历史兼容术语，不再作为 source、slot、graph node、主调度入口或正式 presenter 语义。

重点关注：

```text
openspec/specs/action-animation-profile/spec.md
openspec/specs/basic-locomotion-animation/spec.md
openspec/specs/runtime-diagnostic-logging/spec.md
openspec/specs/simulation-tick-system/spec.md
openspec/specs/wasd-locomotion-pipeline/spec.md
```

## 历史下一步建议
当时建议在继续实现编辑器或 timeline 前，先做一个小 OpenSpec change，只处理数据模型语言和边界：

```text
refine-character-body-slot-data-model
```

该 change 已完成并归档，当前不应再按下面列表创建重复 change。以下内容仅保留为当时的切分依据：

```text
1. 固定 source / action / claim / slot / channel / presentation layer 术语。
2. 决定是否引入 BaseSlot 作为正式命名。
3. 决定 UpperBody 当前是保留合同还是进入正式 runtime。
4. 明确 Facial 暂不进入身体仲裁，或正式定义为某种 slot/channel。
5. 增加测试，证明 Dodge 仍通过 FullBody claim 进入 base slot，而不是 FullBody 节点。
```

当前后续工作应以已归档后的 spec 语义继续推进 Character Behavior Editor 或 Committed Action Timeline Editor 的数据结构和 UI。

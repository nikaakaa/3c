# Design: 统一角色姿态约束事务与预测脚约束状态机

## Context

当前代码是`91758ff7`基线：Landing Lifecycle、Ground Path、GoalTransition、Contact State、Primary Support和Pelvis由`CharacterFootPlacementRuntime`按固定顺序直接编排，各自持有局部Pending/Committed；Pose Graph使用多个Goal Set输入，FBBIK腿部历史不属于Foot同一根事务。该基线保留了开始落脚时锁点、LandingPreparation和GoalTransition连续性，但仍存在多状态Owner、Path与Contact双重过渡、Pelvis实时Path依赖、Sliding削弱和顺序提交。

KKK参考提供了两个应保留的工程事实：FootPath是相对BasePath的增量；动画开始落脚后必须停止采样当前事件的新FootPath并把脚锁向稳定落点。GDC提供了三个上层约束：保留动画水平运动和抬脚轮廓、Foot Path是脚不能穿过的地面下界、Locked/Sliding/Unlocked必须按误差选择而不是把脚无限拉向Anchor。

本设计不照搬KKK的Current Trace取高、Set Mesh和预测/传统IK双路径，也不把GDC没有定义的Revision、Transition或FinalIK细节伪装成原文。项目选择是：单一Predictive正式链、每脚一个显式typed状态机上下文、五状态Foot Constraint、唯一Effective Correction、Resolved Foot Pair、根Bank原子提交和未来Reactive统一输入入口。

## Goals

- 让FootPath采样严格恢复为动画空间进度与`Envelope - Baseline`增量。
- 让每脚全部持久事实集中在一个显式typed State Context中，并由一个State Machine唯一写入。
- 让全脚只有一个Effective Correction/Velocity连续性Owner，并在开始落脚前明确Stable或Rebasing。
- 让每脚Constraint状态与真实控制权一致，锁点前后没有隐藏权重或第二平滑。
- 让左右脚、Support、Pelvis、Goal、BendHistory和Physical Write服从同一根事务。
- 让Foot Placement形成外部Interface窄、内部Implementation完整的深Module，不再要求调用方编排浅Module链。
- 为未来Reactive保留唯一Target语义，不创建未接线状态或第二运行链。

## Non-Goals

- 不增加Current Foot Trace、传统Foot IK、iStep、Heel/Toe、脚掌旋转、移动平台、Pivot或专用楼梯动画。
- 不修改Gameplay、KCC、网络或rollback。
- 不把Foot Constraint做成作者可编辑Graph；它是固定Runtime状态机。

## Terminology

本设计不再使用含义模糊的`FootDown`。正式时刻固定为：

```text
开始落脚（LandingStarted）
    同一权威Landing Event的Constraint Weight从接近0开始上升
    表示动画开始把脚从摆动交给接触约束

完全踩实（FullyPlanted）
    Constraint Weight到达1且Support Weight非零
    表示动画已经进入完整承重区间

开始抬脚（ReleaseStarted）
    Constraint Weight开始下降，或权威Step进入正式Release区间
    表示动画开始把脚从接触约束交还给摆动
```

`LandingStarted`不是Physics命中、Body Grounded、脚骨高度阈值、`TimeToLanding == 0`或已经Locked。`FullyPlanted`也不等于Solver已经把物理脚写到Anchor；Locked仍必须等待Constraint状态、Goal、FBBIK和Physical结果全部成立。

## Decision 1: 保留唯一Pose Constraint根事务

唯一外部Interface继续为：

```text
BeginFrame(FrameHeader)
PrepareFootPlacement(FrameInput, Producer)
AssembleGoals(Contributions)
SolveFullBodyIk(ComponentPose, GoalSet)
CompletePhysicalWrite(WriteOutcome)
CompleteFrame(Seal | Discard)
Invalidate(Reset | Retarget | Discontinuity | Dispose)
```

`PosePlanExecutionRuntime`是唯一物理Owner。调用方不得取得根Bank、左右Foot页、BendHistory或GoalAssembly的可变引用。所有Foot内部Implementation通过根Runtime提供的窄操作写Pending页；Seal只切换一个已验证Bank identity。

每个Bank固定包含：

```text
Frame/Completion/Rig lineage
左右CharacterFootStateContext及其固定容量Ground Path payload页
左右Resolved Foot
Primary Support
Pelvis Spring
Body Trajectory memo
Goal Contributions与唯一Goal Set
左右BendHistory与Solver Outcome
Foot/FBBIK/Physical Diagnostics冻结页
```

Reset、Retarget、Pose discontinuity与Dispose统一失效整个Bank、左右Foot State Context、Frozen Patch、Consumed Event、Effective Correction/Velocity、Pelvis Spring和BendHistory；不逐模块补清理。

## Decision 2: Foot Placement使用深Module与显式typed状态机上下文

对外只有一个`CharacterFootPlacementModule` Interface：

```text
PrepareFootPlacement(CharacterFootPlacementFrameInput)
    -> CharacterFootPlacementResult
```

调用方只提供同帧不可变输入并接收最终Result，不知道Landing Prediction、Ground Path、状态转换、Support、Pelvis或Goal编码的执行顺序，也不能取得任何可变状态引用。

Module Implementation内部固定为：

```text
同帧不可变Frame Input
-> Left CharacterFootStateMachine
-> Right CharacterFootStateMachine
-> CharacterResolvedFootPair
-> Support/Pelvis内部计算
-> 三个Goal Contribution
-> CharacterFootPlacementResult
```

每脚状态机读取上一Committed Context并生成同一Bank的Pending Context与Resolved Foot。正式Context固定为：

```text
CharacterFootStateContext
    ConstraintState
    ActiveEventIdentity / ConsumedEventIdentity
    LastCommittedContact
    NextLandingProposal / GroundPathIdentity / GroundPathPayloadHandle
    PathTargetCorrection / PathTrackingStatus / SettledFrameCount
    FrozenPatch
    EffectiveCorrection / EffectiveVelocity
    TransitionCause / StartCorrection / StartConstraintWeight / Progress
    ReleaseTargetState
```

本文把该Context称为“显式typed状态机黑板”，但它不是Gameplay Blackboard：没有运行时字符串Key、共享Dictionary、动态字段、跨系统读写或按名称fallback。Raw Contact、Edge与Envelope使用Context显式引用的固定容量payload页；World Query Adapter和纯Builder只能在State Machine一次Evaluate期间填充各自的Pending payload，payload不保存Constraint、Transition、Correction或跨帧控制状态，因此不是第二状态Owner。`CharacterFootStateMachine`是Context的唯一写入者；Diagnostics、Pelvis、Goal Encoder和其它Module都只能读取已完成Result。

Landing Predictor、Proposal死区、Ground Envelope Builder、Swing Target Calculator、Trigger Resolver、Constraint数学和Resolved Foot Builder可以在Implementation内部拆成纯函数或内部Module，但它们不形成外部Seam，不保存自己的Pending/Committed、Output、Velocity、Residual或状态页。删除`CharacterFootPlantTransaction`、旧`CharacterFootPlantModule`、`CharacterFootRouteModule`及包含实现状态的`*Contracts`杂糅Interface。

## Decision 3: State Machine唯一拥有Path跟踪与Effective Correction

Ground Path只生成不可变事实：

```text
HasPath
TrackedEventIdentity
LastCommittedContact
NextLandingProposal
GroundPathIdentity
TargetCorrectionAlongUp
```

同一Event的新Landing在更新死区内时复用现有Proposal与Ground Path。超过死区后重建Ground Path并只替换Context中的Path Target。Ground Path没有自己的Output、Velocity、Lerp、Spring、Pending/Committed或Stable状态机。

Swing状态使用Context中唯一的Effective Correction/Velocity执行一维Component Up临界阻尼，不使用每次Revision重启的固定时长Lerp：

```text
omega = PathCorrectionFrequency * 2 * PI
x0 = EffectiveCorrectionAlongUp - PathTargetCorrectionAlongUp
j0 = EffectiveVelocityAlongUp + omega * x0
decay = exp(-omega * dt)

NextEffectiveCorrectionAlongUp =
    PathTargetCorrectionAlongUp + (x0 + j0 * dt) * decay
NextEffectiveVelocityAlongUp =
    (EffectiveVelocityAlongUp - omega * j0 * dt) * decay
```

新Target到来时直接以上一Committed Effective Correction/Velocity继续。`Stable/Rebasing`是State Context发布的Path Tracking Status，不是另一个状态机：

```text
Stable =
    ConstraintState == Swing
    && abs(EffectiveCorrection - PathTarget) <= PathSettledDistance
    && abs(EffectiveVelocity) <= PathSettledSpeed
    && 连续满足PathSettledFrameCount帧

其余合法跟踪状态 = Rebasing
无合法Path = Unavailable
```

正式配置：

```text
PathCorrectionFrequency
PathSettledDistance
PathSettledSpeed
PathSettledFrameCount
ContactLossReleaseSeconds
LockDistance
ReleaseDistance
```

删除`ContactTransitionSeconds`、Ownership HalfLife、Landing Preparation Start Time和实时Landing Height下限。

## Decision 4: Swing严格使用动画空间进度与FootPath增量

输入：

```text
Animated Sole
LastCommittedContact
NextLandingProposal
Ground Envelope与Path Target事实
Component Up
```

空间进度：

```text
direction = ProjectOnPlane(NextLanding - LastContact, Up)
progress = clamp01(
    dot(ProjectOnPlane(AnimatedSole - LastContact, Up), normalize(direction))
    / length(direction))
```

Baseline与Envelope必须按同一纵向进度采样：

```text
BaselineSample = Lerp(LastContact, NextLanding, progress)
EnvelopeSample = SampleEnvelopeByLongitudinalProgress(progress)

RawPathDelta = max(
    0,
    dot(EnvelopeSample - BaselineSample, Up))
```

Swing Target：

```text
RawSwingCorrection = Up * RawPathDelta
PathTargetCorrection =
    animation.foot-placement-weight * RawSwingCorrection

Swing状态下：
EffectiveCorrection/Velocity
    临界阻尼追踪PathTargetCorrection
```

动画仍决定脚的水平位置、原生抬脚高度、最高点时刻和旋转；FootPath只抬高其运行地面基线。地形高度、坡度和Correction大小不产生Lift状态。

Rebasing时仍使用同一个Effective Correction驱动Swing，保证当前脚连续；但开始落脚时只有Path Tracking Status为Stable才能冻结Patch。这样平滑和必达不再互相补偿：Path未收敛时允许本次不锁，不允许为了必达突然加速。

## Decision 5: Constraint使用五状态固定图

正式状态：

```text
Swing
Landing
Locked
Releasing
UnlockedSupport
```

辅助事实：

```text
ActiveEventIdentity
ConsumedEventIdentity
FrozenPatch
TransitionCause
TransitionStartConstraintWeight
TransitionProgress
TransitionResidual
ReleaseTargetState
```

状态图：

```text
Swing
  -> Landing:
       LandingStarted && Path Stable && Proposal/Event有效
  -> UnlockedSupport:
       LandingStarted && Path未Stable或Proposal无效

Landing
  -> Locked:
       FullyPlanted && Contact可达
  -> Releasing:
       完全踩实前已经开始抬脚、Grounded丢失或Contact超距
  -> UnlockedSupport:
       锁入失效且没有可释放Patch

Locked
  -> Releasing:
       ReleaseStarted、Grounded丢失、Contact超距或Contact不可达

Releasing
  -> Swing:
       正常开始抬脚/Safety Release完成且当前进入Swing
  -> UnlockedSupport:
       Release完成但动画仍处于Support

UnlockedSupport
  -> Swing:
       新Swing Event开始
```

同一Event完成Landing、Locked、Release或直接进入UnlockedSupport后写入`ConsumedEventIdentity`；相同Event不得重新进入Landing。Path Tracking Status只是Context事实，不是Constraint State；`Closed`由Consumed Event表达；`Committed`改名Locked以避免与根Bank Commit混淆；`Acquiring`语义由Landing承担。

有限Action占脚不进入Releasing：当帧清Patch与Residual、消费当前Event，并根据动画Phase落到Swing或UnlockedSupport。Reset/Retarget/Discontinuity清Consumed Event并重建lineage。

## Decision 6: Landing与Release只有一个连续性Owner

五个Constraint状态始终写同一组`EffectiveCorrection/EffectiveVelocity`。不存在Path Output、GoalTransition Output、Contact Ownership Output或状态间复制的第二修正。任何状态转换入口的第一帧 MUST逐值保持上一帧Effective Correction；Velocity继续来自同一Context，不能由另一个Module恢复旧值。

### Landing锁入

进入Landing时原子冻结：

```text
FrozenPatch = Stable Proposal的Event/Path/Surface/Point/Normal
AcquireResidual =
    CurrentEffectiveCorrection - FrozenContactCorrectionAtEntry
StartConstraintWeight = CurrentConstraintWeight
Progress = 0
```

后续：

```text
p = clamp01(
    (CurrentConstraintWeight - StartConstraintWeight)
    / (1 - StartConstraintWeight))
p = max(PreviousProgress, p)

EffectiveCorrection =
    CurrentFrozenContactCorrection
    + AcquireResidual * (1 - p)
```

`CurrentFrozenContactCorrection`允许随同帧Animated Sole变化，但Frozen世界Point/Normal/Surface/Path identity不可变化。动画Foot Analysis/Build必须证明Constraint曲线在正式循环步中从接近0连续到1；若进入Landing时已经直接为1且无法形成连续区间，Build拒绝该素材合同，Runtime不补固定Duration。

### Locked

```text
EffectiveCorrection = FrozenAnchor - AnimatedSole
```

Locked非零Goal权重严格为1。`animation.foot-placement-weight`只用于Swing；进入Landing必须证明Constraint/Foot Placement语义允许完整锁定，Locked不得再乘全局Strength、Ownership或horizontalWeight。

当水平误差超过`LockDistance`但未超过`ReleaseDistance`时只发布NearRelease事实，不移动Anchor；超过`ReleaseDistance`进入Releasing。本change不实现GDC Sliding状态，因为移动Anchor会重新引入第二位置Owner；未来若业务需要Sliding必须另立change替换该决策。

### 正常Release

入口：

```text
ReleaseResidual = CurrentEffectiveCorrection
ReleaseStartConstraintWeight = CurrentConstraintWeight
TransitionCause = AnimationReleaseStarted
ReleaseTargetState = Swing
```

后续：

```text
p = clamp01(1 - CurrentConstraintWeight / ReleaseStartConstraintWeight)
p = max(PreviousProgress, p)
EffectiveCorrection = ReleaseResidual * (1 - p)
```

Release目标是原生动画脚Correction零，不消费实时Path。下一Event的Prediction、Ground Path和Path Target事实可以继续更新，但没有后台Path Output。Release完成进入Swing后，同一个Effective Correction从当前零值、同一个Effective Velocity开始追踪最新Path Target，不复制、恢复或重置第二份连续状态。

### Safety Release

Grounded丢失、Contact超距或Contact不可达使用：

```text
p = SmoothStep(elapsed / ContactLossReleaseSeconds)
EffectiveCorrection = ReleaseResidual * (1 - p)
```

Safety与正常Release由`TransitionCause`互斥选择，不同时计算。开始抬脚优先使用动画曲线；Safety只处理没有正常曲线的外部接触丢失。

## Decision 7: Path Revision的打断规则

```text
Swing + PathRevision:
    State Machine只替换Path Target
    同一个Effective Correction/Velocity继续追踪并发布Rebasing

Landing + PathRevision:
    Active FrozenPatch不变，只更新下一Event的Path事实

Locked + PathRevision:
    Anchor不变，只更新下一Event的Path事实

Releasing + PathRevision:
    Release目标不变，只更新下一Event的Path事实

UnlockedSupport + PathRevision:
    只准备下一Swing Event
```

同帧优先级：

```text
1. Reset / Retarget / Pose discontinuity / Dispose
2. Action Foot Occupancy
3. 非有限或lineage invalid
4. Grounded丢失 / Contact不可达
5. Contact超距
6. 开始抬脚（ReleaseStarted）
7. 开始落脚（LandingStarted）
8. Path Revision
```

State Machine内部的Trigger Resolver先把同帧事实归一为一个Constraint Trigger，再由State Machine执行最多一次Constraint状态转换。Prediction与Ground Path内部计算可以在同帧更新下一Event事实，但只有State Machine能写Context，并且不得修改已经冻结的Active Patch。

## Decision 8: Resolved Foot Pair与Pelvis

每脚输出：

```text
PathEventIdentity/Identity/TrackingStatus/Target
ConstraintState/Trigger/TransitionCause
Active/Consumed Event
FrozenPatch
Effective Correction/Velocity
Final Sole/Ankle
Resolved Contact Reference
Support Intent
typed Outcome
```

Contact Reference规则：

- Swing且Path Stable时使用稳定Next Landing Proposal。
- Swing且Path Rebasing时标记UnavailableForStride，不把不稳定终点送入Pelvis。
- Landing/Locked/Releasing使用同一FrozenPatch。
- UnlockedSupport使用Final Sole与动画Support Intent，不伪造Patch。

Primary Support只比较左右Support Intent与上一Committed选择。Pelvis只读取Resolved Pair、Support选择、腿长和Pelvis Spring；不读取Foot State Context、GroundPath、Prediction或World Query。Target先受支撑腿可达区间限制，再用临界阻尼Spring输出；Path Rebasing不能改变当前Pelvis Stride终点。

## Decision 9: Goal、FBBIK与Physical结果

Foot与Pelvis只发布真正独立的`CharacterFullBodyIkGoalContribution`。Contribution不复用GoalSet Header；唯一Assembler验证Slot、Application、Frame、Completion、Rig与容量后生成一个Goal Set。FBBIK只消费一个GoalSet value index。

Goal Encoder规范：

```text
GoalPosition = AnimationAnkle + EffectiveCorrection
GoalWeight = EffectiveCorrection非零 ? 1 : 0
```

编码后不得再平滑、缩放、Clamp或读取Constraint状态。

FBBIK输出必须区分Target、Solved Position与Physical Write Position。Locked脚若在正式支撑腿可达限制后仍产生超过Solver残差容差的结果，Frame发布typed invalid并阻止根Bank Seal；不得静默保留Locked状态和穿模Physical Pose。

## Decision 10: Diagnostics与Reactive扩展边界

Diagnostics在BeginFrame冻结interest，只从已完成Pending Context与Result深复制。正式CSV删除旧Plant State、LandingPreparation、OwnershipHalfLife、CurrentTrace和重复Goal权重，新增：

```text
PathTrackingStatus/Identity/Target/SettledFrames
ConstraintState/Trigger/TransitionCause
Active/ConsumedEventIdentity
FrozenPatchIdentity/Point/Normal/Surface
EffectiveCorrection/EffectiveVelocity
Acquire/ReleaseResidual与Progress
ResolvedFinalSole
Pelvis读取的Reference identity
Goal/Solved/Physical Position与Residual
```

未来Reactive只能在独立change中向`CharacterFootFrameInput`新增typed高优先级意图，并由同一`CharacterFootStateMachine`按Trigger优先级处理。它必须在Constraint进入Landing前完成Target选择；Landing/Locked/Releasing不能被Reactive直接改Context、Anchor、Effective Correction或Goal。Reactive不得创建第二状态写入者、第二Goal、第二Pelvis、第二Solver、第二Physical Writer或第二根Bank。本change不创建空SourceKind、Reactive状态或未接线Adapter。

## Alternatives Considered

### 多个typed状态页与浅Module链

可以让Path、Constraint和Transition分别拥有单一Writer，也方便独立检查纯数学，但调用方或维护者必须理解`Route -> Swing Resolver -> Constraint Reducer -> Constraint Resolver -> Resolved Foot Builder`的顺序、页有效期和交接合同。Path Output/Velocity与Constraint Residual仍形成两个连续性Owner，Releasing到Swing必须跨Module复制或重置状态。拒绝，改为一个深Foot Placement Module和每脚一个State Context。

### 通用共享Blackboard

把字段放进共享Dictionary或运行时Key系统，短期容易查看和让Reactive接线，但任何Module都可能写入，字段有效期、清理和顺序只能靠约定，旧值也可能跨Event残留。拒绝。正式“状态机黑板”只指固定布局的`CharacterFootStateContext`，它随根Bank原子提交且只有`CharacterFootStateMachine`能写。

### 显式typed状态机上下文

一个Context集中一只脚的Event、Path Target、Constraint State、Frozen Patch、Transition和唯一Effective Correction/Velocity；外部只调用深`CharacterFootPlacementModule`并读取Result。代价是State Machine Implementation更集中、Context字段必须按状态维护严格不变量；收益是状态、控制权、清理和诊断具有单点Locality。本设计采用该方案。

### 原样恢复917基线

可以快速恢复已知视觉水平，但会恢复GoalTransition、多个独立Pending/Committed owner、Sliding水平削弱和Pelvis实时Path旁路，继续无法解释控制权限。拒绝。

### 完整照搬KKK

可以获得FootPath增量、Current Trace取高和Predictive/传统IK切换，但会引入Set Mesh、第二IK路径和边缘高度跳变。只采用FootPath增量、空间采样和开始落脚时的锁点时机。

### 只按GDC概念实现

概念边界正确，但GDC没有定义项目所需的Path Revision、状态事务、Transition数学和FinalIK提交语义。必须补项目正式决策。

### Path变化使用固定Duration Lerp

实现简单，但频繁Path变化会不断重启、产生滞留；临近开始落脚又会在“没到点”和“强制跳点”之间摆动。采用保留同一个Effective Correction/Velocity的临界阻尼Target跟随。

### 开始落脚时强制追到新Path

保证落点必达，但剩余时间越短速度越大，直接制造垂直设置、腿奇异点和Pelvis拉扯。采用Path未Settled则UnlockedSupport，不强制锁。

### 增加Sliding状态

符合GDC完整约束模型并能减轻水平误差，但当前静态楼梯范围的首要目标是明确锁点和释放边界；Sliding需要正式Anchor移动策略，否则会恢复第二位置Owner。本change明确不实现。

### 现在预建Reactive状态

方便未来接线，但没有生产者、准入、Transition和验收合同，会形成未接线正式代码。只在设计中固定未来统一Target边界。

## Risks And Tradeoffs

- Path未Settled时不锁，极端急转的一步可能按动画落地甚至出现局部穿模；收益是不会为了必达制造整腿跳变和骨盆拉扯。
- Locked严格Anchor会更依赖腿可达和Pelvis预限制；收益是状态名称与物理目标一致，不再用隐藏权重掩盖不可达。
- 不实现Current Trace意味着Ground Envelope必须真正覆盖中间障碍；若仍穿模，应修正Path采集/采样，不在最终Goal后追加第二高度来源。
- 不实现Sliding会让大水平误差更早Release；收益是静态地面Anchor只有一个Owner。
- Constraint曲线成为正常Lock/Release唯一进度，需要Foot Analysis/Build严格验证连续覆盖；收益是删除任意0.12秒和双重平滑。
- 单脚State Machine的Implementation会比拆散的浅Module更集中；收益是外部Interface更小，一只脚的状态、写权限、清理和转换都具有单点Locality。内部纯算法仍可拆文件，但不能拥有第二份跨帧状态。
- 根双Bank和Diagnostics深冻结增加固定内存，但替代散落状态和可变页浅引用，不增加运行真相数量。

## Migration Plan

1. 保留并复核根Bank、Goal Assembler、唯一FBBIK/Writer与BendHistory事务骨架。
2. 新增左右`CharacterFootStateContext`并让`CharacterFootStateMachine`成为唯一写入者，删除Route/Constraint/GoalTransition各自的Pending/Committed与状态页。
3. 把Landing Prediction、Ground Path、Swing Target、Trigger和Constraint数学收进深`CharacterFootPlacementModule`的Implementation，删除对外浅Module调用链。
4. 把Swing采样恢复为Animated Sole空间进度和`Envelope - Baseline`增量，并让唯一Effective Correction/Velocity临界阻尼追踪Path Target。
5. 实现`Swing/Landing/Locked/Releasing/UnlockedSupport`五状态图，确保所有转换继续写同一个Effective Correction/Velocity。
6. 迁移Support/Pelvis只消费新Resolved Foot Pair，并阻止Rebasing Path进入Stride。
7. 完成独立Goal Contribution ABI和唯一GoalSet workspace清理。
8. 完成Physical Writer预验证、BendHistory lineage与Solver/Physical残差验证。
9. 重写Diagnostics Projector、CSV、Gizmo、Pose Watch和Live字段，只读取Committed深冻结页。
10. 删除旧Plant类型、Route命名、状态、配置、CSV列、兼容Goal容器和临时junction/重复文件路径。
11. 更新`openspec/project.md`为实际实现状态，执行严格OpenSpec校验、Runtime/Editor编译和静态唯一性检查。

# Design: 统一角色姿态约束事务与预测脚约束状态机

## Context

当前Committed代码基线是`8fc704a`。它已删除旧`CharacterFootGoalTransition`并用`CharacterFootEffectiveConstraint`统一单脚Output Correction，但仍使用`None/Acquiring/Locked/Sliding/Releasing`实验状态、实时Path向上硬地面下限、PlantConfidence直接Ownership和分散Pending/Committed。`194953`采样证明它能41/42次Locked且Anchor正确，也证明实时Path硬下限会把Path Revision直接变成18cm级脚高跳变。

后续实验形成五条不能回退的证据：

- Swing FootPath必须是非负`Envelope - Baseline`；删除负Swing修正后，29/29次Acquire成功Locked，最大Solver残差从49.96cm降到0.23cm。
- Constraint从脚到最高点后的Approach开始上升；直接用未来Patch平面驱动脚会产生最低约-1.07m修正和完全伸直奇异位形。
- PlantConfidence不是单调Transition Alpha；55次Landing中54次Progress始终为0，Plant帧再从0跳到1，最大修正跳变28.68cm。
- Prediction Point、Contact Patch和Committed Anchor必须分型；提前冻结Prediction XYZ会产生最大2.17m水平过期。
- 视觉Path Settled不能成为Landing事件准入；5mm准入实验最终0次Locked。

因此本设计保留五状态，不增加Approach或Acquiring状态，但要求Foot Analysis/Projection先发布一段真正晚期、单调、覆盖到Plant的Landing Height计划。没有通过Build质量门槛的素材不能接入正式Predictive Foot Placement，Runtime不提供固定Duration或原始曲线fallback。

## Goals

- 对外只暴露一个深`CharacterFootPlacementModule` Interface。
- 每脚状态集中在显式typed `CharacterFootStateContext`，由一个State Machine唯一写入。
- 全脚只有一个Effective Correction/Velocity连续性Owner。
- 严格分开Prediction、Frozen Contact Patch和Committed Anchor。
- Swing只使用非负FootPath；Landing只在已验证晚期窗口沿Component Up交接。
- Support Intent不依赖Anchor是否Locked。
- Foot、Pelvis、Goal、Solver、Writer和Diagnostics服从一个根事务。

## Non-Goals

- 不增加Current Foot Trace、传统Foot IK、iStep、Heel/Toe双点、脚掌旋转、移动平台、Pivot或专用楼梯动画。
- 不修改Gameplay、KCC、网络或rollback。
- 不把Foot状态机做成作者可编辑Graph。
- 不在本change接入Reactive，只固定以后必须复用的Patch准入入口。

## Terminology

```text
ApproachStarted
    脚从最高点开始下降的分析事实
    不触发Foot状态或脚Goal

LandingStarted
    Foot Analysis/Projection发布的唯一晚期高度交接起点
    触发Swing进入Landing
    冻结Contact Patch，但不创建Anchor

LandingHeightProgress
    Projection发布的0到1单调进度
    必须在LandingStarted到PlantStarted之间完整覆盖
    不能直接使用Constraint或PlantConfidence原值

PlantStarted
    Projection发布的唯一权威Plant onset
    触发Landing进入Locked，不是额外状态

ReleaseStarted
    Projection发布的唯一正常Release事实
```

Build从显式Foot Contact Marker或versioned推断算法中只选择一个规范PlantStarted；Runtime不按PlantConfidence阈值生成第二Trigger。

## Decision 1: 唯一根事务与深Module

```text
BeginFrame
-> CharacterFootPlacementModule
-> Goal Assembler
-> FBBIK
-> Physical Writer
-> Seal | Discard
```

每个根Bank固定包含左右Foot Context与Ground Path payload、Resolved Foot Pair、Primary Support、Pelvis Spring、Goal Contributions/Goal Set、BendHistory/Solver Outcome和Diagnostics页。调用方不得取得可变Bank或逐模块Seal。

Foot Placement外部只调用：

```text
PrepareFootPlacement(FrameInput)
    -> CharacterFootPlacementResult
```

Implementation内部执行左右State Machine、Resolved Foot Pair、Support/Pelvis和三个Goal Contribution。Landing Predictor、Ground Builder、Swing Target、Trigger Resolver和Constraint数学只能是纯函数或内部Module，不能拥有第二份跨帧状态。

## Decision 2: 显式typed Foot State Context

```text
ConstraintState
ActiveEventIdentity / ConsumedEventIdentity
LastCommittedContact
NextLandingProposal / GroundPathIdentity / payload handle
PathTargetCorrection / EffectiveCorrection / EffectiveVelocity
PathTrackingStatus / SettledFrameCount
FrozenContactPatch
CommittedAnchor
LandingResidual / ReleaseResidual
LandingHeightProgress / ReleaseProgress
TransitionCause
```

Context不是Gameplay Blackboard：没有字符串Key、共享Dictionary、动态字段或跨系统写入。只有`CharacterFootStateMachine`能写。

## Decision 3: Prediction、Patch与Anchor严格分型

```text
NextLandingProposal
    Swing中允许更新的预测位置
    不能直接控制当前脚

FrozenContactPatch
    Event / Path / Surface / PlanePoint / PlaneNormal
    表示选中了哪块地面
    不拥有最终脚XZ

CommittedAnchor
    Event / Patch / WorldPoint / WorldNormal
    Plant后真正锁住的世界点
```

Landing只冻结Patch。Plant时：

```text
CurrentEffectiveSole = AnimatedSole + EffectiveCorrection
CommittedAnchor = ProjectAlongComponentUp(
    CurrentEffectiveSole,
    FrozenContactPatch)
```

Anchor XZ来自Plant当帧脚底，不来自旧Prediction Point。Patch不可投影、身份失效或准入差异超容差时进入UnlockedSupport，不创建替代Anchor。

## Decision 4: Swing只使用非负FootPath

```text
progress = AnimatedSole在LastContact到NextLanding方向上的空间投影
BaselineSample = Lerp(LastContact, NextLanding, progress)
EnvelopeSample = SampleEnvelope(progress)

RawPathDelta = max(0, EnvelopeHeight - BaselineHeight)
PathTargetCorrection =
    animation.foot-placement-weight * Up * RawPathDelta
```

动画决定脚XZ、下降轨迹、最高点和旋转。Swing不得使用`Baseline - AnimatedSole`或未来Landing高度向下拉脚，也不得把实时Path Target作为硬地面下限。

Path变化只替换Target，保留Effective Correction/Velocity。`Stable/Rebasing/Unavailable`只表示跟踪质量，不是第二状态机，也不决定Landing事件是否存在。

## Decision 5: 五状态图

```text
Swing
Landing
Locked
Releasing
UnlockedSupport
```

```text
Swing
  -> Landing:
       LandingStarted && Patch/Event有效
       && Grounded && Action未占用
  -> UnlockedSupport:
       LandingStarted但没有合法Patch
       或PlantStarted到达时仍没有Frozen Patch

Landing
  -> Locked:
       PlantStarted
       && LandingHeightProgress已完整覆盖
       && Frozen Patch有效
       && 当帧Effective Sole可在容差内投影到Patch
  -> UnlockedSupport:
       PlantStarted但进度/Patch/投影准入失败
  -> Releasing:
       ReleaseStarted、Grounded丢失或Patch失效

Locked
  -> Releasing:
       ReleaseStarted、Grounded丢失、Anchor超距或不可达

Releasing
  -> Swing:
       Release完成且新Swing Event有效
  -> UnlockedSupport:
       Release完成但动画仍处于Support

UnlockedSupport
  -> Swing:
       新Swing Event开始
```

PlantStarted只是一条`Landing -> Locked` Transition，不增加Planting/Acquiring状态。`Sliding`不属于正式状态。相同Event完成或失败后写入Consumed Event，不能晚到重锁。

## Decision 6: Landing使用编译后的单调高度计划

进入Landing时：

```text
FrozenPatch = Event/Path/Surface/PlanePoint/Normal

LandingCorrection =
    ProjectAlongComponentUp(AnimatedSole, FrozenPatch)
    - AnimatedSole

LandingResidual =
    CurrentEffectiveCorrection - LandingCorrection
```

后续只读取Projection计划：

```text
p = max(PreviousProgress, LandingHeightProgress)

EffectiveCorrection =
    CurrentLandingCorrection
    + LandingResidual * (1 - p)
```

Landing第一帧等于进入前Output；动画XZ保持不变。实时Path Revision只能准备下一Event。Landing不执行实时Path硬地面下限。

Build必须证明：

- `LandingHeightProgress`在实际source coverage中从0单调到1。
- 结束不晚于唯一PlantStarted。
- Landing开始时Frozen Patch垂直目标处于正式腿长/Pelvis可达范围。
- Progress不能由Runtime固定Duration、Constraint原值或PlantConfidence原值补造。

## Decision 7: Plant原子创建Anchor并进入Locked

```text
CurrentEffectiveSole = AnimatedSole + EffectiveCorrection
CommittedAnchor = ProjectAlongComponentUp(
    CurrentEffectiveSole,
    FrozenPatch)

AnchorEntryDifference =
    CurrentEffectiveCorrection
    - (CommittedAnchor - AnimatedSole)
```

AnchorEntryDifference必须处于正式几何容差内。State Machine在同一次`Landing -> Locked` Transition中创建Anchor并输出严格Anchor；不存在Plant后的固定Duration Acquire或隐藏HasAnchor子状态。

```text
Locked EffectiveCorrection = CommittedAnchor - AnimatedSole
Locked GoalWeight = 1
```

Locked后Path、Prediction和Patch Proposal失去当前脚位置控制权；Anchor超距进入Releasing，不实现Sliding。

## Decision 8: Release从当前Output返回动画

```text
ReleaseResidual = CurrentEffectiveCorrection
p = Projection发布的单调ReleaseProgress
EffectiveCorrection = ReleaseResidual * (1 - p)
```

Grounded丢失、Anchor超距或不可达使用正式Safety Release。正常与Safety Release互斥。Release期间Path只更新下一Event；完成后同一个Effective Correction/Velocity进入Swing。

## Decision 9: 数据更新与Trigger顺序

```text
1. 生成本帧Prediction/Patch/Path事实
2. 更新Swing Target与Tracking Status
3. 读取Projection的Landing/Plant/Release事实
4. 归一一个Constraint Trigger
5. 最多执行一次状态转换
6. 生成Resolved Foot
```

Landing后Frozen Patch不变，Locked后Anchor不变，Releasing目标不变。Trigger优先级为Reset/Retarget/Dispose、Action、invalid、Grounded/Anchor invalid、Anchor超距、ReleaseStarted、PlantStarted、LandingStarted。

## Decision 10: Support、Pelvis与最终Goal

Resolved Foot包含Path tracking、状态与Trigger、Frozen Patch、Committed Anchor、Effective Correction/Velocity、Final Sole/Ankle、Contact Reference、Support Intent与typed Outcome。

Support Intent来自动画Biomechanical Support事实，与Contact Ownership分离。Landing开始即可发布Support Intent；Primary Support不能要求Locked。Pelvis同时约束上一支撑腿与正在Landing腿的可达区间，不能等Locked帧才突然下移。

Goal Encoder只执行：

```text
GoalPosition = AnimationAnkle + EffectiveCorrection
GoalWeight = EffectiveCorrection非零 ? 1 : 0
```

编码后不得再平滑、缩放、Clamp或读取状态。Landing腿不可达和Locked Solver/Physical残差必须发布typed invalid并阻止根Bank Seal。

## Decision 11: Diagnostics与Reactive

Diagnostics记录Path Tracking、LandingHeightProgress、状态与Trigger、唯一PlantStarted、Frozen Patch、Committed Anchor、Effective Correction/Velocity、Landing/Release Residual、Support Intent、Pelvis双腿可达、Goal/Solved/Physical结果。Diagnostics不能反向影响状态。

未来Reactive只能通过Frame Input提供Patch或Plant Observation；Landing前可参与Patch选择，Plant时可验证Patch，Locked/Releasing不能移动Anchor。不得创建第二状态写入者、Goal、Pelvis、Solver、Writer或根Bank。

## Rejected Alternatives

- 917多Owner与8fc实时Path硬地面下限。
- 所有状态使用一个固定半衰期。
- Path残差小于5mm才允许Landing。
- Landing提前冻结完整Prediction XYZ。
- Runtime直接读取PlantConfidence作为进度或Ownership。
- 从Approach Contact开始直接把未来Patch平面变成脚Goal。
- 增加Planting/Acquiring状态掩盖Landing没有完成。

## Risks And Tradeoffs

- 实时Swing Path不执行硬地面下限；极端迟到高台阶可能短暂穿模，但不会垂直设置。
- 没有合法单调Landing Height计划的素材将Build失败；不提供Runtime fallback。
- Plant时Landing未完成会进入UnlockedSupport；不为显示Locked瞬移脚。
- Locked严格Anchor依赖Landing腿可达和Pelvis预限制。
- 不实现Sliding会让大水平误差更早Release，但Anchor只有一个Owner。

## Migration Plan

1. 以`8fc704a`和实验账本为行为证据，保留唯一Correction与非负Swing FootPath。
2. 在Foot Analysis/Projection新增唯一LandingStarted、单调LandingHeightProgress、唯一PlantStarted与ReleaseProgress合同。
3. 建立左右Foot State Context并删除分散Pending/Committed。
4. 把Prediction、Ground Path、Swing Target、Trigger和Constraint数学收进深Foot Placement Module。
5. 实现`Swing/Landing/Locked/Releasing/UnlockedSupport`五状态。
6. 新增Frozen Patch与Committed Anchor分型；Landing冻结Patch，Plant生成Anchor。
7. 迁移Landing Support Intent与双腿可达区间。
8. 完成Goal Contribution、唯一GoalSet、BendHistory和Writer闭包。
9. 重写Diagnostics并保留精简实验账本。
10. 删除旧状态、GoalTransition、Sliding、PlantConfidence Ownership和兼容路径。
11. 更新project truth并执行严格校验、Runtime/Editor编译和静态唯一性检查。

# Design: 在统一Foot Placement Module内替换接触行为政策

## Context

前置重构已经负责Module、Context、Resolved Pair、根Bank、Goal和Writer所有权。本change不再移动这些职责，只替换Module内部由动画事实、Path事实和当前脚Pose生成Effective Correction的政策。

8fc等价重构完成后，正式状态仍为`Swing/Landing/Locked/Releasing/UnlockedSupport`，但其行为映射来自旧None/Acquiring/Locked/Sliding/Releasing。本change把这些状态从“8fc行为重解释”升级为新的权威Contact Plan行为，不增加状态。

## Decision 1: 权威Foot Contact Plan

Projection为每脚原子发布：

```text
AnimationFootContactPlanSample
    LandingEventIdentity
    PlanSourceIdentity
    ApproachStarted
    LandingStarted
    LandingHeightProgress
    PlantStarted
    SupportWeight
    ReleaseStarted
    ReleaseProgress
```

Started字段是Event内保持的事实，不是单帧脉冲。第一版保留现有LandingPhase作为Plant onset，把ReleasePhase到LiftOffPhase编译为Release计划，并由versioned Analyzer从完整sole下降轨迹生成独立LandingStarted和单调Landing Height曲线。Artifact identity与Projection revision覆盖全部onset、曲线和algorithm version。

Blend Space从拥有当前Step/Event/Route的同一authoritative source原子选择整组计划；不同Event不得按权重平均。State Context只允许对同Event Progress取单调最大值。

## Decision 2: Swing空间进度与唯一Target

新Swing Event入口捕获：

```text
SwingOriginSole = PreviousFrame.FinalSole
```

每帧计算：

```text
progress = Project01(AnimatedSole, SwingOriginSole, NextLanding)
baseline = Lerp(SwingOriginSole, NextLanding, progress)
envelope = SampleEnvelope(progress)
raw = Up * max(0, EnvelopeHeight - BaselineHeight)
PathTarget = FootPlacementWeight * raw
```

Path Target变化不建立新输出，不捕获第二残差，也不重启固定Duration。Swing状态保留唯一Effective Correction/Velocity并以正式临界阻尼追踪。Stable/Rebasing/Unavailable只表示跟踪质量。

## Decision 3: 有限Contact Patch

Ground Path从包含Next Landing的连续同Surface接触段生成：

```text
SupportDomain
    Tangent
    AlongMin / AlongMax
    LateralRadius
    DomainIdentity
```

Along范围来自接触段端点，LateralRadius使用本次Ground Path查询半径，identity覆盖Path、Surface、端点Candidate和几何。它只验证Sole中心，不承诺Heel/Toe完整覆盖。

Landing冻结`Event/Path/Surface/Plane/Normal/SupportDomain`。当前Animated Sole沿Component Up的入口投影和Landing期间每帧投影都必须在域内；系统不查询Current Trace，不Clamp到域边界，也不把无限平面视为合法Patch。

## Decision 4: Landing与Plant

Landing入口只捕获一次：

```text
LandingCorrection = ProjectAlongUpWithinDomain(AnimatedSole, FrozenPatch) - AnimatedSole
LandingResidual = EffectiveCorrection - LandingCorrection
```

后续：

```text
p = max(PreviousProgress, LandingHeightProgress)
EffectiveCorrection = CurrentLandingCorrection + LandingResidual * (1 - p)
```

动画继续拥有XZ和旋转。PlantStarted当帧：

```text
CurrentEffectiveSole = AnimatedSole + EffectiveCorrection
Anchor = ProjectAlongUpWithinDomain(CurrentEffectiveSole, FrozenPatch)
```

只有Progress完整、Patch有效、投影在域内且Anchor入口差异处于正式容差时进入Locked；否则消费Event并进入UnlockedSupport。Prediction Point不能成为Anchor。

## Decision 5: Locked、Release与Pelvis

Locked严格输出：

```text
EffectiveCorrection = Anchor - AnimatedSole
GoalWeight = 1
```

删除Sliding水平权重。Anchor超距、不可达或Grounded丢失进入Safety Release。正常Release入口捕获一次Residual，只按同Event ReleaseProgress衰减；完成后同一Correction/Velocity进入Swing。

Support Intent继续来自动画事实并与Anchor分离。State Machine在Resolved Foot中发布Support Intent Weight和typed Pelvis Reach Reference；Landing开始后，Pelvis只读取Resolved Pair中的这些字段，同时求上一支撑腿和Landing腿的可达区间。Pelvis不读取Foot State、Lock Response、Context或Rebasing Proposal，也不伪造Anchor。

## Risks And Tradeoffs

- 删除实时硬地面下限后，迟到高台阶可能短暂穿模，但不会把Path Revision直接设置成脚高。
- 有限SupportDomain可能在台阶边缘拒绝锁脚；这比锁到错误踏面更保守。
- 没有合法Contact Plan的素材将Build失败，不提供Runtime fallback。
- 删除Sliding后，大水平误差会更早释放，但Anchor只有一个位置Owner。
- 本阶段仍只约束Sole中心，Heel/Toe完整接触属于后续能力。

## Migration

1. 前置重构完成并归档。
2. 发布Foot Contact Plan与Build质量门槛。
3. 在现有Foot Placement Module内部替换Swing政策。
4. 增加有限SupportDomain并替换Landing/Plant政策。
5. 替换Locked/Release/Pelvis行为。
6. 删除8fc行为字段、配置和Diagnostics，不保留开关。

# Change: 重构角色脚步与全身IK的统一姿态约束事务

## Why

当前代码已经回到`91758ff7`基线：`CharacterFootPlacementRuntime`直接编排左右Landing Lifecycle、Ground Path、Swing、独立GoalTransition、Contact State、Primary Support与Pelvis；每个局部对象各自保存Pending/Committed并由调用方顺序Seal。Contact State使用`Acquiring/Locked/Sliding/Releasing`，GoalTransition另外保存`Smooth/LandingPreparation`连续性，Pose Graph仍让多个Goal Set直接进入唯一FBBIK，Solver腿部历史仍在根事务之外。该基线是当前已知视觉效果最好的备份，但控制权、提交身份和数据流仍然分裂。

current `character-foot-placement-presentation`已经给出正确Swing口径：用原生Animated Sole在`LastLanding -> NextLanding`之间的空间进度，同时采样Landing Baseline和Ground Envelope，只把`Envelope - Baseline`的非负高度增量叠加到动画脚。当前代码却按动画Phase采样Envelope，并使用`Envelope - Animated Sole`再叠加实时Landing Preparation高度，导致Path变化直接改变脚的世界高度目标。

KKK参考同样把FootPath视为相对BasePath的增量，并明确指出FootPath平滑会让落地脚来不及到点，因此在FootDown时停止采样当前Path、把脚过渡并锁定到落点。参考实现中的直接Current Trace取高、Set Mesh、预测IK/传统IK双路径和边缘跳变属于实现补丁，不作为本项目正式链路。本change采用KKK的运动口径与锁点时机、GDC的约束分层和项目现有根事务，重新定义一条可解释的正式链。

本次继续使用唯一change-id，不新建第二Foot IK proposal。基线中仍正确的Landing Prediction、Ground Path、空间FootPath增量、FootDown锁点和Pelvis业务约束迁入新模型；旧GoalTransition、LandingLifecycle、Contact State、顺序Seal、plural GoalSet和Solver对象历史不作为兼容路径保留。

## What Changes

- 保留唯一`CharacterPoseConstraintRuntime`双Bank根事务，Foot、Pelvis、Goal、BendHistory、Solver Outcome、Physical Write与Diagnostics继续按同一Frame/Completion/Rig lineage统一Seal或Discard；外部不得读取可变Bank或逐模块提交。
- 删除巨型`CharacterFootPlantTransaction`和同时处理左右脚、Support、Pelvis的`CharacterFootPlantModule`。正式Foot内部拆为固定调用链：`Route Module -> Swing Resolver -> Constraint Reducer -> Constraint Resolver -> Resolved Foot Builder`，随后左右Resolved Foot形成Pair，再进入Support与Pelvis Module。
- Route只拥有下一Landing Event的Prediction、Proposal、Ground Path与Path连续性。Path连续性状态固定为`Stable/Rebasing`；Path Target变化时保留当前Correction和Velocity，只替换Target，不重启定时Lerp。
- Swing只消费同帧原生动画脚与稳定Path结果。空间进度来自Animated Sole在`LastLanding -> NextLanding`方向上的投影；FootPath修正严格为`Envelope Sample - Landing Baseline Sample`的非负Component Up增量，不增加实时Landing Height下限，不按地形高度创建Lift状态，不修改动画脚水平位置或旋转。
- 每脚Constraint状态固定为`Swing/Landing/Locked/Releasing/UnlockedSupport`。`ConsumedEventIdentity`负责阻止同一事件晚到重锁，不保留`Tracking/Closed`状态；`Landing`承担锁入过程，`Releasing`承担正常或安全释放，`UnlockedSupport`显式表达动画已经承重但本次没有世界Anchor。
- `Swing`中的Path Revision只触发Path Rebase。FootDown时只有`Path Stable + Proposal有效 + Event匹配 + Grounded + Action未占用 + 目标可达`才能冻结完整Patch并进入Landing；Path仍在Rebasing或Proposal无效时进入UnlockedSupport，本事件不得为了必达而垂直设置或晚到重锁。
- Landing入口只捕获一次`AcquireResidual = CurrentEffectiveCorrection - FrozenContactCorrection`，进度使用动画Biomechanical `ConstraintWeight`的单调上升；Locked严格输出Frozen Contact Correction，不能再乘小于1的Contact/FootPlacement权重，也不能通过`horizontalWeight`削弱Anchor。
- 正常FootUp进入Releasing，入口只捕获一次相对原生动画脚的Release Residual，进度使用动画Constraint下降；Grounded丢失或Contact超距使用正式`ContactLossReleaseSeconds`安全释放。Release期间当前Path不改变目标，只更新Next Route；结束后才由Swing Path Rebase接入新Path。
- 有限Action占脚属于硬抢占：当帧清Patch、Residual和当前Event，事件记为Consumed，Foot IK Correction归零，连续性只由既有Action Slot Pose Blend负责。Reset、Retarget、Pose discontinuity与Dispose清除全部Route/Constraint/Consumed状态并回到与新lineage一致的初始状态。
- `ResolvedFootResult`是单脚唯一正式输出，聚合Route lineage、Path稳定性、Constraint State、Frozen Patch、Effective Correction、Final Sole/Ankle、Support Intent与typed Outcome。Primary Support和Pelvis只能消费左右Resolved Foot Pair；Swing Path处于Rebasing时不得把不稳定Stride终点送入Pelvis。
- Foot Placement、PoseBone等来源发布真正独立的typed Goal Contribution；唯一Assembler发布一个Goal Set，唯一FBBIK只消费该Goal Set。删除Contribution复用GoalSet Header、plural GoalSet workspace/input和旧兼容端口。
- FBBIK BendHistory继续属于根Bank，并补齐SourceCompletionIdentity与Revision；Solver不得根据Foot SourceKind启用隐藏膝盖规则，也不得把Vendor对象内部状态作为跨帧真相。
- Diagnostics只从已完成Pending Result深冻结，记录Route Stable/Rebasing、Constraint State、Consumed/Active Event、Patch、Path Target/Output/Velocity、Acquire/Release Residual、Goal、Solved与最终Physical Pose；不保留旧Plant、LandingPreparation、OwnershipHalfLife、CurrentTrace或兼容CSV列。
- 未来Reactive能力只能作为统一Foot Adjustment Target的另一来源，并通过同一Source Transition进入同一Constraint Resolver；本change不创建空Reactive状态、iStep Adapter、第二Goal链、第二Pelvis或第二Solver。

## Impact

- Affected specs: `character-foot-placement-presentation`、`character-animation-foot-analysis-artifact`、`character-animation-pipeline`、`character-presentation-pose-graph`
- Affected runtime: Landing Prediction、Proposal、Ground Path、Path连续性、Swing采样、Foot Constraint状态、Resolved Foot、Primary Support、Pelvis、Pose Constraint根Bank、Goal ABI、FBBIK BendHistory、Physical Writer
- Affected editor: Foot Analysis Constraint/Support连续性校验、Projection Compiler、Pose Plan ABI、Goal拓扑Validator、CSV、Gizmo、Pose Watch、Live Diagnostics
- Affected authoring/config: 删除`ContactTransitionSeconds`与旧Plant配置；新增正式Path Correction响应、Path Settled容差和`ContactLossReleaseSeconds`；不增加fallback配置
- 不修改Gameplay Body、KCC、VisualRoot、网络状态、rollback snapshot、动画水平运动、专用上下楼动画或场景地面职责

## Current Spec Comparison

- current `character-foot-placement-presentation`要求Animated Sole空间进度、Landing Baseline与Ground Envelope共同形成纯垂直增量。本change保留并强化该口径；现有active change中的Phase采样、`Envelope - Animated Sole`和Landing Preparation实时高度全部删除。
- current spec禁止Foot Lock、Pelvis和跨帧连续性，因为它表达的是早期Swing-only阶段。本change用正式Constraint状态、Frozen Patch、Resolved Foot Pair和Pelvis Result替换该阶段边界，同时保留“不重建动画水平步态”和“FootPath只提供地面增量”的原始约束。
- current Ground Path要求同一事件Prediction持续更新并按死区重建Path。本change新增Stable/Rebasing输出连续性；Prediction和Ground Path事实仍可更新，但FootDown冻结后不得改变Active Patch。
- current `character-animation-foot-analysis-artifact`已经保存连续脚部feature、Plant confidence与Landing onset，但没有证明Runtime正常Lock/Release所需的Constraint从0到1再回0的实际coverage。本change新增Build期连续性门槛，拒绝用Runtime固定Duration替代缺失动画语义。
- current `character-presentation-pose-graph`仍要求多个Goal Set直接汇入FBBIK。本change继续使用现有active delta的Goal Contribution与唯一Assembler模型，并完成底层ABI清理。
- current `character-animation-pipeline`仍使用FootPlacement Targets与LegIK术语。本change继续把正式链收敛为Goal Contributions、唯一Goal Set与唯一FBBIK，并让全部Foot/FBBIK跨帧事实随根Bank原子提交。
- `openspec/project.md`当前Foot Placement条目仍描述已经被本提案否决的Tracking/Committed/Releasing/Closed与Ownership模型，且部分“根Runtime仍暴露Bank、Retarget未失效”等实现描述已经过期。实施完成前必须改为“当前迁移中”；只有代码与任务真实收口后才能写入最终状态机真相。

## Non-Goals

- 不实现Heel/Toe双点、脚掌旋转、移动平台SurfaceLocalAnchor、攀爬、跳跃空中贴脚、手部IK、接触脚转向Pivot或专用上下楼动画。
- 不接入Reactive、传统IK、iStep、Current Foot Trace、第二World Query Adapter、第二Goal或第二Solver；只规定未来接入必须复用的统一Target边界。
- 不把动画Lift高度、地形坡度或VerticalCorrection阈值变成Foot Constraint状态。
- 不修改FinalIK核心求解算法；只整理Goal输入、BendHistory、结果验证和事务边界。
- 不新增自动测试；实施阶段只执行项目要求的编译、静态一致性检查和严格OpenSpec校验，端到端由用户负责。

## Success Criteria

```text
Foot内部不存在CharacterFootPlantTransaction或第二总处理器
Constraint State只包含Swing/Landing/Locked/Releasing/UnlockedSupport
Route State只包含Path Stable/Rebasing及typed availability
同一Landing Event最多冻结一次、锁定一次，Consumed事件不得晚到重锁
Swing Progress来自Animated Sole空间投影，不来自Phase Lerp
Swing Correction逐值等于非负(Envelope Sample - Baseline Sample)沿Component Up的增量
Path Target变化不重置Correction或Velocity
FootDown时Path未Settled不得强制追点或创建Anchor
Landing/Locked/Releasing期间Path Revision只更新Next Route
Locked Final Sole逐值等于Frozen Anchor，不受FootPlacementWeight或horizontalWeight削弱
正常Release由Constraint下降驱动，Grounded/超距只由正式Safety Release驱动
Pelvis只读取Resolved Foot Pair且不消费Rebasing中的不稳定Stride终点
运行方法签名不存在*Diagnostics或*Snapshot业务输入输出
每帧只有一个Goal Set、一次FBBIK和一个Physical Writer
全链只发布一个Committed Bank identity
Diagnostics开关不改变正式Result
不存在LandingPreparation、ContactTransitionSeconds、旧Plant状态、CurrentTrace、GoalSet兼容容器或并行Reactive路径
```

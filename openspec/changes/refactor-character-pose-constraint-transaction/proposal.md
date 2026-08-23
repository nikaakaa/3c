# Change: 重构角色脚步与全身IK的统一姿态约束事务

## Why

当前Committed代码基线是`8fc704a`：旧`CharacterFootGoalTransition`已经删除，`CharacterFootEffectiveConstraint`统一保存单脚Output Correction，并保留`None/Acquiring/Locked/Sliding/Releasing`实验状态。该备份在`194953`采样中恢复41/42次Locked和正确Anchor，但实时Path向上硬地面下限产生36次超过5cm、15次超过10cm的普通帧修正跳变。后续未提交实验又证明：删除硬下限并要求Path残差先进入5mm会让40次Plant全部无法Locked；提前冻结完整预测XYZ会让Plant水平误差达到2.17m；把所有状态交给同一个0.03秒低通则42次Acquire只有1次Locked。工作区仍有行为实验改动，它们只作为证据，不属于current truth或本提案目标。

current `character-foot-placement-presentation`已经给出正确Swing口径：用原生Animated Sole在`LastLanding -> NextLanding`之间的空间进度，同时采样Landing Baseline和Ground Envelope，只把`Envelope - Baseline`的非负高度增量叠加到动画脚。实验同时证明现有Constraint和PlantConfidence都不能直接当Landing插值：Constraint从脚到最高点后的Approach开始上升，直接投影未来Patch会产生米级向下目标；PlantConfidence会先升后降再脉冲跨阈值，55次Landing中54次Transition Progress始终为0。Foot Analysis必须发布独立、晚期、单调覆盖到Plant的`LandingStarted + LandingHeightProgress`，Runtime不能现场解释原始曲线。

KKK参考同样把FootPath视为相对BasePath的增量，并明确指出FootPath平滑会让落地脚来不及到点，因此Approach开始后停止让实时Path改变当前步。实验进一步收紧该边界：Approach只能冻结`Surface/Plane/Normal/Event`组成的Contact Patch并保留动画脚XZ；实际Plant时才把当帧Effective Sole沿Component Up投影到Frozen Patch，生成最终世界Anchor。预测Point只能选择Patch，不能提前充当Anchor。参考实现中的直接Current Trace取高、Set Mesh、预测IK/传统IK双路径和边缘跳变不作为本项目正式链路。

本次继续使用唯一change-id，不新建第二Foot IK proposal。`8fc704a`中已经证明有效的单一Output Owner、空间FootPath增量、Plant时Anchor稳定和Goal/FBBIK唯一链迁入最终模型；实时Path硬地面下限、PlantConfidence直接Ownership、Sliding、分散Pending/Committed和顺序Seal不作为兼容路径保留。

## What Changes

- 保留唯一`CharacterPoseConstraintRuntime`双Bank根事务，Foot、Pelvis、Goal、BendHistory、Solver Outcome、Physical Write与Diagnostics继续按同一Frame/Completion/Rig lineage统一Seal或Discard；外部不得读取可变Bank或逐模块提交。
- 删除巨型`CharacterFootPlantTransaction`、旧`CharacterFootPlantModule`和对外暴露的`Route -> Swing Resolver -> Constraint Reducer -> Constraint Resolver -> Resolved Foot Builder`浅链。正式外部Interface收敛为一个深`CharacterFootPlacementModule`；其Implementation为左右脚各执行一次`CharacterFootStateMachine`，形成Resolved Foot Pair后在内部计算Support、Pelvis与三个Goal Contribution。
- 每脚只有一个固定布局的`CharacterFootStateContext`。它是该状态机专属的显式typed状态机黑板，集中保存Constraint State、Active/Consumed Event、下一落点与Ground Path事实、Path Target/Tracking、Frozen Patch、Committed Anchor、唯一Effective Correction/Velocity、Landing/Release Residual和Transition事实；它不使用字符串Key、共享Dictionary、Gameplay Blackboard或可变Diagnostics。`CharacterFootStateMachine`是Context的唯一写入者。
- Landing Prediction、Proposal死区、Ground Envelope Builder、Swing Target计算、Trigger归一和Constraint数学降为`CharacterFootStateMachine` Implementation内部的纯计算或World Query Adapter。它们只返回不可变事实，不保存Pending/Committed、Path Output、Residual或第二份Correction。
- Swing Path只产生Target。空间进度来自Animated Sole在`LastLanding -> NextLanding`方向上的投影；FootPath Target严格为`Envelope Sample - Landing Baseline Sample`的非负Component Up增量，不增加实时Path硬地面下限，不按地形高度创建Lift状态，不修改动画脚水平位置或旋转。
- 每脚Constraint状态固定为`Swing/Landing/Locked/Releasing/UnlockedSupport`。`Landing`从Projection发布的晚期`LandingStarted`开始，显式表达冻结Patch、保留动画XZ和只沿Component Up完成高度交接；`PlantStarted`是`Landing -> Locked`的Transition Trigger，不是额外状态。`Locked`才拥有固定世界Anchor；`ConsumedEventIdentity`阻止同一事件晚到重锁。
- `EffectiveCorrection`与`EffectiveVelocity`是唯一跨帧脚修正。`Swing`中它们以临界阻尼连续追踪Path Target；Path Target变化只替换目标，不复制、重置或启动第二个Lerp。`Stable/Rebasing`是Context根据Target误差、Effective Velocity与Settled帧数发布的跟踪事实，不是独立状态机，也不拥有Output。
- `ApproachStarted`只保留为脚从最高点开始下降的动画分析事实，不触发状态或脚Goal。Foot Analysis/Projection必须为每个正式Event发布晚期`LandingStarted`和从0单调到1、在PlantStarted前完整覆盖的`LandingHeightProgress`；Build必须验证该窗口的实际source coverage和腿可达质量。`LandingStarted`才触发`Swing -> Landing`、冻结Patch并捕获垂直入口残差。Path是否Settled只影响Tracking诊断和Stride引用，不得成为Landing准入门槛。
- `PlantStarted`固定为Foot Analysis/Projection发布的唯一权威Plant onset；Build从显式Foot Contact Marker或versioned推断算法中只选择一个规范事实。Runtime不得再按PlantConfidence阈值生成第二Trigger，PlantConfidence也不作为连续进度。该Trigger在同一次`Landing -> Locked` Transition中使用当帧Effective Sole投影Frozen Patch生成Committed Anchor；预测Point不拥有Anchor XZ。若Patch无效、当帧Sole不能合法投影或Landing垂直残差仍超正式容差，则进入UnlockedSupport并消费事件，不得强制设置到地面。
- Anchor准入保证Plant当帧目标与当前Effective Correction只相差正式几何容差，因此不需要Plant后的第二Acquire状态或Contact Progress。Locked严格输出`CommittedAnchor - AnimatedSole`，不能再乘小于1的FootPlacement/Contact权重、通过`horizontalWeight`削弱Anchor或使用实时Path硬Clamp。
- 正常开始抬脚时进入Releasing，入口只捕获一次相对原生动画脚的Release Residual，进度使用动画Constraint下降；Grounded丢失或Contact超距使用正式`ContactLossReleaseSeconds`安全释放。Release期间Path事实可以为下一Event更新，但不能改变当前Effective Correction的释放目标。Releasing结束时同一个Effective Correction已经回到零，进入Swing后直接从该值和同一Velocity追踪最新Path Target，不执行跨Module交接。
- 有限Action占脚属于硬抢占：当帧清Patch、Transition和当前Event，事件记为Consumed，Effective Correction/Velocity归零，连续性只由既有Action Slot Pose Blend负责。Reset、Retarget、Pose discontinuity与Dispose一次清除整个Foot State Context并回到与新lineage一致的初始状态。
- `CharacterResolvedFootResult`是单脚唯一正式输出，聚合Event/Path lineage、Path跟踪事实、Constraint State、Frozen Patch、Committed Anchor、Effective Correction、Final Sole/Ankle、Support Intent与typed Outcome。动画Support Intent从Landing开始就可存在，并与Contact Ownership分离；Primary Support和Pelvis只能消费左右Resolved Foot Pair，且必须同时约束旧支撑腿与正在Landing的腿可达。
- Foot Placement、PoseBone等来源发布真正独立的typed Goal Contribution；唯一Assembler发布一个Goal Set，唯一FBBIK只消费该Goal Set。删除Contribution复用GoalSet Header、plural GoalSet workspace/input和旧兼容端口。
- FBBIK BendHistory继续属于根Bank，并补齐SourceCompletionIdentity与Revision；Solver不得根据Foot SourceKind启用隐藏膝盖规则，也不得把Vendor对象内部状态作为跨帧真相。
- Diagnostics只从已完成Pending Context与Result深冻结，记录Path Target、Stable/Rebasing、唯一Effective Correction/Velocity、Constraint State、Approach/Landing/Plant/Release事实、LandingHeightProgress、Consumed/Active Event、Frozen Patch、Committed Anchor、Residual、Support Intent、Goal、Solved与最终Physical Pose；不保留旧PlantConfidence Ownership、LandingPreparation、OwnershipHalfLife、CurrentTrace或兼容CSV列。
- 未来Reactive能力只能作为`CharacterFootFrameInput`中的typed高优先级意图进入同一`CharacterFootStateMachine`；它不能直接写Context、Patch、Correction或Goal。本change不创建空Reactive状态、iStep Adapter、第二Goal链、第二Pelvis或第二Solver。

## Impact

- Affected specs: `character-foot-placement-presentation`、`character-animation-foot-analysis-artifact`、`character-animation-pipeline`、`character-presentation-pose-graph`
- Affected runtime: Foot Placement深Module、单脚typed State Context、Landing Prediction、Proposal、Ground Path、Swing Target、Foot Constraint状态、Resolved Foot、Primary Support、Pelvis、Pose Constraint根Bank、Goal ABI、FBBIK BendHistory、Physical Writer
- Affected editor: Foot Analysis Constraint/Support连续性校验、Projection Compiler、Pose Plan ABI、Goal拓扑Validator、CSV、Gizmo、Pose Watch、Live Diagnostics
- Affected authoring/config: 删除旧GoalTransition、ContactTransition与PlantConfidence Ownership配置；新增正式Path Correction响应、Path Tracking容差、Landing Height窗口质量门槛、Plant准入容差和`ContactLossReleaseSeconds`；不增加fallback配置
- 不修改Gameplay Body、KCC、VisualRoot、网络状态、rollback snapshot、动画水平运动、专用上下楼动画或场景地面职责

## Current Spec Comparison

- current `character-foot-placement-presentation`要求Animated Sole空间进度、Landing Baseline与Ground Envelope共同形成纯垂直增量。本change保留并强化该口径；现有active change中的Phase采样、`Envelope - Animated Sole`和Landing Preparation实时高度全部删除。
- current spec禁止Foot Lock、Pelvis和跨帧连续性，因为它表达的是早期Swing-only阶段。本change用正式Constraint状态、Frozen Patch、Resolved Foot Pair和Pelvis Result替换该阶段边界，同时保留“不重建动画水平步态”和“FootPath只提供地面增量”的原始约束。
- current Ground Path要求同一事件Prediction持续更新并按死区重建Path。本change保留事实更新与死区，但删除Ground Path自己的Output/Velocity；每脚唯一Effective Correction负责连续追踪Path Target，进入Approach并冻结Patch后新Path事实不得改变当前Patch或后续Anchor。
- current `character-animation-foot-analysis-artifact`已经保存连续脚部feature、Plant confidence、Approach Contact、Landing与Release事实。本change明确Constraint只证明Approach垂直交接，PlantConfidence/Landing onset只触发Plant；Runtime不得把任一原始数值直接当Contact Ownership。
- current `character-presentation-pose-graph`仍要求多个Goal Set直接汇入FBBIK。本change继续使用现有active delta的Goal Contribution与唯一Assembler模型，并完成底层ABI清理。
- current `character-animation-pipeline`仍使用FootPlacement Targets与LegIK术语。本change继续把正式链收敛为Goal Contributions、唯一Goal Set与唯一FBBIK，并让全部Foot/FBBIK跨帧事实随根Bank原子提交。
- current `character-pipeline-blackboard`定义Gameplay Program的authoring声明、scope和typed state address。本change的`CharacterFootStateContext`只属于Presentation根Bank、固定布局且由单脚State Machine唯一写入，不注册Gameplay Blackboard key、不进入Character Simulation State，也不与该current capability形成第二解释路径。
- `openspec/project.md`继续明确Committed代码是`8fc704a`行为备份而非最终架构，并把本change中的深Foot Placement Module、typed State Context、Patch/Anchor分离和根Bank标为目标。只有代码与任务真实收口后才能把这些目标改写成current实现状态。

## Non-Goals

- 不实现Heel/Toe双点、脚掌旋转、移动平台SurfaceLocalAnchor、攀爬、跳跃空中贴脚、手部IK、接触脚转向Pivot或专用上下楼动画。
- 不接入Reactive、传统IK、iStep、Current Foot Trace、第二World Query Adapter、第二Goal或第二Solver；只规定未来接入必须复用的统一Target边界。
- 不把动画Lift高度、地形坡度或VerticalCorrection阈值变成Foot Constraint状态。
- 不修改FinalIK核心求解算法；只整理Goal输入、BendHistory、结果验证和事务边界。
- 不新增自动测试；实施阶段只执行项目要求的编译、静态一致性检查和严格OpenSpec校验，端到端由用户负责。

## Success Criteria

```text
外部只存在一个深CharacterFootPlacementModule Interface
每脚只存在一个CharacterFootStateMachine和一个CharacterFootStateContext
State Context使用固定typed字段且只由State Machine写入，不使用共享Blackboard或可变Diagnostics
Constraint State只包含Swing/Landing/Locked/Releasing/UnlockedSupport
Stable/Rebasing只表示唯一Effective Correction对Path Target的跟踪状态，不是第二状态机
同一Landing Event最多冻结一次、锁定一次，Consumed事件不得晚到重锁
Swing Progress来自Animated Sole空间投影，不来自Phase Lerp
Raw Swing Correction逐值等于非负(Envelope Sample - Baseline Sample)沿Component Up的增量
Path Target逐值等于animation.foot-placement-weight乘Raw Swing Correction
全脚只存在一个Effective Correction与一个Effective Velocity
Path Target变化不重置Effective Correction或Velocity
Path未Settled不得阻止有效Landing事件，也不得触发实时硬地面下限
Landing只冻结Patch并保留动画XZ，Plant之前不存在世界Anchor
Plant时Anchor XZ来自当帧Effective Sole，预测Point不能充当Anchor
Landing/Locked/Releasing期间Path Revision只更新下一Event事实
Releasing到Swing不复制或恢复第二份Path Output
Locked Final Sole逐值等于Committed Anchor，不受FootPlacementWeight或horizontalWeight削弱
Runtime只消费Projection发布的LandingStarted、单调LandingHeightProgress与唯一PlantStarted，不现场解释Constraint或PlantConfidence
正常Release由Constraint下降驱动，Grounded/超距只由正式Safety Release驱动
Landing开始即可发布动画Support Intent，Pelvis同时保证支撑腿与落地腿可达
Pelvis只读取Resolved Foot Pair且不消费Rebasing中的实时Stride终点
运行方法签名不存在*Diagnostics或*Snapshot业务输入输出
每帧只有一个Goal Set、一次FBBIK和一个Physical Writer
全链只发布一个Committed Bank identity
Diagnostics开关不改变正式Result
不存在LandingPreparation、ContactTransitionSeconds、旧Plant状态、CurrentTrace、GoalSet兼容容器或并行Reactive路径
```

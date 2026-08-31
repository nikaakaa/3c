# PoseGraph重构行为保护清单

## 唯一基线

用户指定提交：`ad3527e103cc3235a63e8a1c1dbd26df5155e0ba`。这是动画和IK行为的唯一比较起点，不自动跟随HEAD、其它active change或新的实验结果。

该提交记录“233436回放恢复205014脚部与膝盖结果”。2026-09-01代码检查时，当前动画Runtime、Presentation／Foot、Corin配置、PoseGraph作者代码、Presentation编译器和相关Diagnostics目录与该提交无差异。Input Focus等无关后续改动不属于IK重构目标，不回退；实际回放输入仍必须与基线一致。

本文件记录的是代码检查和既有证据，不是本轮重新运行的Replay，更不是已经完成重构的等价证明。尚未运行的节点、初始化／Reset边界和非固定帧率不能由已有样本推断为全部通过。

## 已有输入与回放证据

- 输入Record：`43357ff3cd384e5cba75d2c31175b116`。
- 保留结果：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/20260831-205014-114-dc157fde9c004846a72e9cd1fa1b5b01`。
- 恢复结果：`3cDemo/Client/3C_Client/Diagnostics/FootPlacementRuns/20260831-233436-894-d1564c7fa0b442f6aef02bb470ca0b1b`。
- 两个原包均保留`samples.csv`、`ground-path-geometry.csv`与`replay-proof.json`，不得覆盖或删除。
- 指定提交中的实验记录报告：1044输入帧，1043输出帧，2086脚行，1215列，67186几何行；相对205014，1191业务列逐值一致，仅24个采样／实例／Surface／Path身份列变化且映射无冲突。
- 既有口径为facts71／diagnosis40／quality-score3；42项规则、计数与覆盖恢复。61.9总分是已有粗略参考，不是重构通过门槛。
- 完整裁决见[有符号膝向与恢复记录](../stabilize-character-foot-path-and-landing/experiments/20260830-signed-animation-bend.md)，以本文件指定提交中的版本为证据，后续追加记录不自动更换基线。

## 实际调用链与保护点

以下代码路径均相对于`3cDemo/Client/3C_Client/Assets/GameScripts/Main/Runtime/Character/Pipeline/`。它们是本次检查的关键入口；实现每个Family前仍需完整盘点该Family消费者，不能将本表当作已穷举所有分支。

| 阶段 | 已检查入口 | 必须保持的实际行为 |
|---|---|---|
| 外层帧 | `Presentation/CharacterPresentationRuntime.cs::Present` | Pending Tuning先于Frame；消费release completion后生成Action lifecycle／Slot plan，采样Action，再推进Pose source、处理可选MM、裁决Transition，发布Action／Slot目标，准备资源和release，最后进入Evaluate Barrier |
| Source时间 | `Animation/Presentation/CharacterPoseStateSourceRuntime.cs::AdvanceSources`、`AnimationClipPlayer.cs::SynchronizeMovementClock/Advance/PrepareCapture` | Movement clock的owner、generation、origin、offset、TransitionSource继续规则，以及raw/effective time、cycle、loop／finite截断、play rate、零dt、Preview seek和State entry reset；不得都改成简单dt累加 |
| Pose混合 | `Animation/Presentation/PosePlanExecutionRuntime.cs::Advance/FinalizePoseStateFrame`、`Animation/PoseGraph/CharacterPoseGraphStagedExecutor.cs::EvaluateStateMachineStandardBlend` | State／relevance／readiness裁决与source推进顺序；edge曲线、Duration、GlobalDurationMultiplier、逐骨骼Profile、Parameter、左右脚feature与Contribution混合；不得改变累加顺序或增加Foot专用第二混合 |
| Native执行 | `PosePlanExecutionRuntime.cs::PrepareEvaluation/ExecuteEvaluateBarrier` | 一次Animancer Evaluate后按现有依赖执行捕获Pose、Local／Component转换、控制、Contribution、Assembler、FBBIK与Output。改Family布局不等于允许提前采样、重复求值或改变数值顺序 |
| Foot来源 | `PosePlanExecutionRuntime.cs::BuildFootPlacementInput/ResolveFootStepObservationFrame/RequireFootStepObservationContribution` | 当前Foot Motion选取最大Weight的Live Contribution；相同Weight保留遍历中首项，排除Stored。然后使用该source实际dominant Clip sample的continuous／normalized time、cycle和曲线。当前正式分支为Timeline／Clip，重构不补造其它来源 |
| Foot主链 | `Presentation/FootPlacement/CharacterFootPlacementModule.cs::EvaluateFrame` | 捕获真实Foot／Toe／Heel，当前Support查询、Prediction、Plant Verification、GroundPath与Swing目标形成后调用每脚Lifecycle；随后Primary Support／Pelvis，FinalizeLanding，再编码Goal并记录Visible Sole。不得重排内部阶段 |
| Foot状态 | `CharacterFootLifecycle.cs::Evaluate/FinalizeLanding` | Pre Transition、Contact Verification、State Target、Interpolation、Post Transition和Post Constraint保持；Landing完成仍由Pelvis后的既有Reach可用性参与第二次收口，不擅自引入新的请求／最终Resolved流程 |
| 连续历史 | `CharacterFootInterpolationRuntime.cs::ApplyCorrectionResponse` | AnimationRelativeScalar与ContactWorldResidual分域、target height、world residual、退出接触时的接管规则和初始化条件保持。方向上限当前按每次调用施加，scalar最大变化为选定速度乘dt；不得把前者改为度／秒或合并两种历史 |
| Pelvis | `CharacterFootStrideHipsBuilder.cs::ResolvePelvis/AdvancePelvisResponse` | 双脚目标与动画Sole最低高度差、当前软姿态偏好范围、Support／Slope／TargetCrossedOutput与反向速度清零条件、同一Critical Spring及频率保持。Reach保留观察／Landing资格，不恢复业务层骨盆或末端脚硬夹紧 |
| Goal | `CharacterFootPlacementModule.cs::CreateFootGoal/CreatePelvisGoal`、`Animation/PoseConstraints/CharacterFullBodyIkGoalAssembler.cs::Assemble` | Ready与Position／Rotation Weight资格、零Correction仍有效、world到component转换、Pelvis pre-solve translation、Contribution既有顺序和同Slot拒绝保持；不重新排序或降低权重补偿误差 |
| FBBIK | `Animation/PoseConstraints/CharacterFinalIkFullBodySolver.cs::SolvePrepared/ApplyGoals/ApplyLegBendStabilization` | 绑定同一Component Pose后，先Pelvis translation，再Foot pre-solve rotation、identity PoseBone Slot识别、ResetEffectors、Effector Goal、腿弯曲方向；有有效Goal才Vendor Update，随后Virtual Bone重建与原结果验证。Operation一次不意味着无有效Goal也强制Update |
| 膝盖方向 | 同一Solver的`ApplyLegBendStabilization` | 保留a40b71f已确认的可靠动画有符号方向及FromToRotation腿轴运输；退化时保留原历史／投影／符号策略和Bend权重公式，不当作SmoothKnee删除，第一阶段独立验证的Reset方向修正作为接入成果保留，本阶段不再改方向政策 |
| Physical写入 | `Presentation/Animancer/AnimationFinalPosePhysicalWriter.cs::Write/ResolvePose` | 整Rig预检后按现有bone顺序写Local Position／Rotation／Scale；ExcludeSourceRoot仍用Rig reference root pose。OutputPose数据与实际Physical写入结果不能合并成错误的同一事实；保留原Committed／Reference选择和Fault语义 |
| 跨帧提交 | `Animation/Presentation/CharacterPoseConstraintRuntime.cs::SealFrame/DiscardFrame/ResetSolvers`、`CharacterFootPlacementBank.Begin` | 保留Committed到Pending的实际历史、Prediction／BodyTrajectory、Pelvis Spring、Bend、Anchor／Path以及页有效性。不得因重命名HasFrame或“清理无用字段”启用基线未启用的PreviousVisibleOutput接管 |
| 诊断发布 | `CharacterPresentationRuntime.cs::Present`的PostCommit与现有Sampler／Analyzer／Publisher | 只迁移事实来源，不改采样窗口、规则／评分、存储和数据解释；PostCommit异常及停止政策不作顺手修复，无法证明重排等价时报告冲突 |

## 不能按名字删除的状态

`CharacterFootInterpolationState.CorrectionResponseFact.ResponseDirection`当前会被下一帧读取。它虽然名为Fact，却不是可随诊断关闭而删除的冗余；迁移必须保留其真实消费者和时序。第一阶段将该消费者迁入正式Interpolation历史后，本阶段沿用新的唯一历史，不能恢复从Fact反读的路径。

同理，BendHistory、Stable／Applied方向、Movement clock、Continuation anchor、Phase游标、Slot／BlendStack时钟、Inertialization history／residual、source generation和retirement handshake都需要逐字段列出初始化、写入、下一帧消费者及Reset。不能只保留最终Pose而丢掉影响下一帧的值。

## 配置与内容保护

保持指定提交中的Corin `CorinFootPlacementProfile.asset`、`CorinFootPlacementRigCalibration.asset`、`CorinFullBodyIkProfile.asset`、Animation Rig、Presentation Profile、Pose Graph、Blend Curve／Profile、Body Presentation Profile和原生AnimationClip曲线业务内容，不重新调参、重烘焙或替换素材。

Program Image／Projection ABI变化允许显式Build生成新schema、layout与Presentation identity；Gameplay ContractHash和Float32／Fixed ProgramHash不得因纯Pose重构变化。不得为“hash一致”复用旧Projection，也不得为“新架构更干净”更改作者参数。

## 允许变化与判定

- 允许改变类名、目录、外部接口、Owner、页布局和编译产物ABI；必须保留表达式、浮点类型、求值顺序、空间转换、采样时机与历史推进。
- 新生成的Operation／Value index、ProjectionRevision、ProgramImageHash或运行实例身份可以变化，但要以稳定Graph／Node／call-site／Source／Event建立一一对照。不能因此改变tie-break、relevance、reset次数、查询次数、release时机或输出。
- 每个代码小步通过现有正式记录回放，分别对指定基线和上一保留小步对账。先确认输入、Body、Presentation dt／schedule和source sample一致，再比较Pose、Foot／Pelvis／Goal、Solved与Physical；固定规则计数及总分只作辅助证据。
- 原有对比已区分四元数q／-q的旋转等价与身份映射，继续沿用同一口径；其它业务差异不得擅自提高容差、取绝对值、删列、换规则或调参数消掉。发现差异先停在当前小步报告。
- 当前原包不覆盖最终Physical Knee、其它路线与真实非固定帧率的全部表现。没有覆盖只能明确写未验证，不能以代码审阅或一次Replay保证所有输入完全不变。
- 不新增测试工程、第二回放驱动器或临时采样链。Reset／零dt／reentry等基线外覆盖只使用现有正式入口；需要新增能力时报告，不绕过当前系统。

## 已撤销与已保留必须区分

保留：指定提交中的动画相对有符号膝向运输、GroundPath／Envelope对Swing可见高度的作用、世界Contact Anchor与残差、原有Foot／Pelvis权重和唯一Spring。

不恢复：SmoothKnee后处理、CurrentSupport零净空替代Swing包络、业务层骨盆Reach硬夹紧及末端夹脚。它们与基线仍存在的Reach观察、Landing完成资格和既有Swing地面约束不是同一件事，不能按“删除Reach”一并删掉。

用户本次Goal明确要求先完成并验证IK维护提案，本change串行接入其通过提交。其它未完成Foot行为与已有可见问题仍留在原范围，不成为隐含修复目标。第一阶段结构变化应对233436保持行为；其独立Reset修正需附单独证据，本阶段同时对固定总基线和第一阶段通过提交对账，不能把接入点偷偷改成总基线。

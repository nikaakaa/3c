# Design: 用统一姿态约束事务重新解释8fc Foot Placement

## Context

`8fc704a74ed3548c3357eff5c2d45f52d8366a4b`定义本change的行为Oracle。Oracle包括逐帧输入、跨帧状态、公式、阈值和输出，不包括旧文件、旧类型、旧调用方向或旧状态命名。

本设计的难点不是保留旧结构，而是在不改变行为的前提下建立最终所有权。旧Lifecycle和Effective Constraint可以删除，旧状态可以重新分类，但不能借分类变化修改任何输出。

## Decision 1: 唯一根事务

```text
BeginFrame
-> CharacterFootPlacementModule
-> Goal Assembler
-> FBBIK
-> Physical Writer
-> Seal | Discard
```

唯一`CharacterPoseConstraintRuntime`预分配两个根Bank。每个Bank持有Foot Context页、Ground Path页、Resolved Foot Pair页、Primary Support/Pelvis页、Goal Contribution/Goal Set页、BendHistory/Solver Outcome页和Diagnostics页。

根Bank与大页使用引用对象。运行方法不得按值传递完整Bank、FixedList Ground Path payload或Diagnostics聚合体。调用方不能取得可变Bank，也不能逐模块Seal。Writer成功后只允许no-throw根identity切换。

根Runtime的Implementation只允许包含阶段顺序、lineage校验、页选择、Seal/Discard/Invalidate和失败传播。Foot、Pelvis、Goal Assembly与Solver数学必须留在各自深Module；不得为了统一事务把所有算法搬进根Runtime形成God Object。

## Decision 2: 深Foot Placement Module

外部Interface只有：

```text
PrepareFootPlacement(FrameInput)
    -> CharacterFootPlacementResult
```

Implementation内部执行：

```text
Animation/Body/World Observation
-> Landing Prediction与Ground Path
-> 左右Foot State Machine
-> Resolved Foot Pair
-> Primary Support与Pelvis
-> 三个Goal Contribution
```

Prediction、Ground Builder、Swing数学、Constraint数学、Support选择和Pelvis数学可以是内部纯函数或内部Module，但不能拥有独立Pending/Committed、Output或Seal。删除任一内部纯函数时，复杂度会留在Foot Placement Implementation，而不会泄漏给调用方。

## Decision 3: 单一Foot Context与Observation Seam

每脚Context集中保存8fc全部跨帧真相：

```text
ConstraintState
PlantCycleConsumed
Landing Lifecycle Event与Previous/Next Landing
Swing Path identity / point / residual
Contact Event / Anchor / Progress
Effective Correction
Acquire Residual
Release Target / Residual / Start Residual
Lock Response
Transition Cause
```

只有`CharacterFootStateMachine`写Context。World Query Adapter先生成不可变Observation Page；State Machine不得直接调用SphereCast、访问Collider或保存Unity查询对象。Pelvis和Goal只能读取Resolved Result。Diagnostics可以从Context、Observation与Result深冻结，但不能写回。

## Decision 4: 状态重新解释

新状态不是新行为，而是对8fc字段的确定分类：

```text
8fc None && !PlantCycleConsumed -> Swing
8fc None && PlantCycleConsumed  -> UnlockedSupport
8fc Acquiring                  -> Landing
8fc Locked                     -> Locked / FullAnchor Response
8fc Sliding                    -> Locked / Sliding Response
8fc Releasing                  -> Releasing
```

`LockResponse = FullAnchor | Sliding`是Locked内部计算事实。它没有Event identity、Anchor生命周期、进入/离开Trigger或独立Output，因此不是第二状态机。Context只保存重现8fc“进入Sliding首帧保留Output”所需的上一帧Response。

状态转换保持8fc：

- PlantConfidence低于0.5时允许清除PlantCycleConsumed。
- 首次达到0.5时消费本轮Plant；只有合法Landing且水平误差不超过LockDistance才进入Landing并创建8fc Anchor。
- Landing的ContactProgress为历史最大`InverseLerp(0.5, 0.75, PlantConfidence)`；达到1进入Locked。
- Landing在PlantConfidence低于0.5或水平误差超过SlideDistance时进入Releasing。
- Locked在PlantConfidence低于0.75或水平误差超过SlideDistance时进入Releasing。
- Locked水平误差超过LockDistance时使用Sliding Response，否则使用FullAnchor Response。
- Releasing在PlantConfidence低于0.5且Output与Swing Correction距离不超过LandingUpdateDistance时回到Swing。
- Action硬失去所有权时清空Context；若PlantConfidence仍不低于0.5，则进入映射后的UnlockedSupport并保持本轮已消费。

## Decision 5: 8fc Swing与Output数学不变

Swing进度保持：

```text
phase = InverseLerp(LiftOffPhase, LandingPhase, EventPhase)
progress = SmoothStep(0, 1, phase)
baseline = Lerp(LastLanding, NextLanding, progress)
envelope = SampleEnvelopeByArcLength(progress)
vertical = max(0, dot(envelope - baseline, Up))
         + LandingConstraintWeight * dot(baseline - AnimatedSole, Up)
SwingCorrection = Up * vertical
```

同Event Path可按LandingUpdateDistance更新。Path可用性、Event或Landing Point发生正式Revision时：

```text
SwingResidual = PreviousOutput - NewSwingCorrection
SwingResidual = HalfLifeAdvance(SwingResidual, 0)
Output = RaiseToFloor(NewSwingCorrection + SwingResidual, NewSwingCorrection)
```

不得改为空间Swing、纯非负Path、临界阻尼或有限SupportDomain；这些属于后续行为change。

## Decision 6: 8fc Contact数学不变

Landing入口：

```text
Anchor = AcceptedLanding.Point
ContactCorrection = Anchor - AnimatedSole
Output = RaiseToFloor(PreviousOutput, ContactCorrection)
AcquireResidual = Output - ContactCorrection
ContactProgress = 0
```

Landing每帧：

```text
ContactProgress = max(ContactProgress, InverseLerp(0.5, 0.75, PlantConfidence))
Output = ContactCorrection + AcquireResidual * (1 - ContactProgress)
```

Locked FullAnchor Response直接使用ContactCorrection。Sliding Response保持：

```text
horizontalWeight = InverseLerp(SlideDistance, LockDistance, HorizontalError)
Desired = HorizontalCorrection * horizontalWeight + VerticalCorrection
```

进入Sliding Response的首帧保留旧Output，后续按EffectiveCorrectionHalfLife追踪Desired。所有非Releasing状态继续执行8fc向上`RaiseToFloor`。

Releasing保持移动Target残差：

```text
ReleaseResidual += PreviousSwingTarget - CurrentSwingCorrection
ReleaseResidual = HalfLifeAdvance(ReleaseResidual, 0)
Output = CurrentSwingCorrection + ReleaseResidual
```

Contact Ownership、Support Weight和Goal Position Weight逐值保持8fc。

## Decision 7: 紧凑Resolved Foot Result与Support Eligibility

State Machine输出：

```text
CharacterResolvedFootResult
    Frame / Completion / Rig / Side
    FinalSole / FinalAnkle
    EffectiveCorrection / GoalWeight
    ContactReference / ContactOwnership
    SupportEligibility
    SupportWeight / SupportIntentWeight
    SupportHorizontalError / SupportEventIdentity
    PelvisReachReference
    Outcome
```

`SupportEligibility`固定为：

```text
None
RetainOnly
AcquireAndRetain
```

8fc映射为：Swing、Landing和UnlockedSupport发布None；Releasing发布RetainOnly；Locked无论FullAnchor或Sliding Response均发布AcquireAndRetain。重构阶段SupportIntentWeight逐值等于8fc SupportWeight；PelvisReachReference在现有Contact可参与Pelvis时与ContactReference相同，其余状态为Unavailable。后续行为change只能改变这些typed字段的填充值，不修改Module Interface或让Pelvis读取State。

State、Lock Response、Path、Anchor内部历史、Acquire/Release Residual和其它Context字段不得复制进正式Resolved Result，只供State Machine和Diagnostics使用。Resolved Pair只是左右Result的同Frame/Completion/Rig组合，不重新计算Foot状态，也不是第二Blackboard。

Primary Support只读取Eligibility、Support Weight、Horizontal Error、Support Event identity和Contact Reference：AcquireAndRetain可获取并保留，RetainOnly只能保留，None不能参与。权重相同时仍按8fc Support Weight和Horizontal Error选择。Selector不得读取Foot State、Lock Response或Context。

Stride、支持腿可达区间、Pelvis Target、Handoff和Spring公式保持8fc。它们只读取Primary Support Result与Resolved Pair中的Final Sole、Pelvis Reach Reference和lineage，不提前接入Landing腿，不读取State/Lock Response，不改变Primary Support缺失时的释放行为。

## Decision 8: Goal、FBBIK与Writer

Foot Placement和PoseBone来源发布独立typed Goal Contribution。唯一Assembler验证Frame、Completion、Rig、Slot和重复贡献后形成一个Goal Set。Assembler不平滑、不Clamp、不选择Foot行为。

Foot Goal Encoder只读取Resolved Result中的Effective Correction与Goal Weight，Pelvis Encoder只读取Pelvis Result；二者不得读取Foot State、Lock Response、Context或Diagnostics。Contribution逐值编码8fc的左右脚和Pelvis Goal；FBBIK输入顺序、Profile、Bend规则、Update次数和Physical Writer输入保持不变。

BendHistory迁入根Bank前必须枚举Vendor FBBIK全部跨帧可观察状态，并证明每个影响下一帧结果的字段能从Committed BendHistory、Profile和当前Goal精确重建。Pending初始化顺序、Stable/Applied Bend、SourceCompletionIdentity和Revision必须明确。发现无法捕获的Vendor隐式状态时，迁移在此阻断；不得用默认值、近似值或视觉相似替代8fc结果。

## Decision 9: Diagnostics

Diagnostics只从Pending Context、Observation、Resolved Result和后续阶段Result单向深冻结到Pending Diagnostics页，并随根Bank提交。它可以使用新状态名和Lock Response，但必须保留足够事实证明8fc等价：Phase Progress、Baseline、Envelope、Swing Correction、Residual、Anchor、Contact Progress、Ownership、Support Eligibility、Support、Pelvis与Goal/Solved/Physical结果。

Diagnostics不能读取世界、选择Support、生成Goal或修改Context。无interest时不复制大页。

## Decision 10: 唯一FBBIK配置与单数运行合同

`CharacterAnimationPresentationProfile`是FullBodyIK Profile唯一作者Owner。FullBodyIK Pose节点只表达拓扑，不保存Profile；Compiler必须从当前Presentation Profile生成Descriptor。Descriptor必须冻结Profile Id与Revision，并在加载与构造Runtime时和当前Profile精确对账。

运行时固定只有一个FBBIK Solver、一个BendHistory、一个Goal Set和一个Solver Outcome，不得继续使用长度为1的数组、Goal Set索引或遍历接口。Solver Outcome必须显式保存Produced、Frame、Completion与Rig lineage；默认值表示未执行，不能通过Physical Writer前验证。

## Decision 11: 子模块页所有权与固定预测Workspace

根Bank直接拥有Foot Committed/Pending页并把本帧页显式交给Foot Module单次Evaluate。Foot Module可以保存不可变依赖与算法Implementation，但不得保存第二套Committed/Pending指针、Begin/Complete/Discard事务状态。State Machine必须用一个Evaluate调用内部完成Landing晋升、Next Swing捕获和Constraint解析，外层不得编排其内部转换顺序。

Future Body Translation Source必须写入调用方预分配的固定容量Workspace。一次预测只更新Sample数量和内容，不得创建Trajectory对象、临时Sample数组或构造时复制数组；同一根Bank在一帧内继续复用一次预测结果。

## Rejected Alternatives

- 原样保留Landing Lifecycle和Effective Constraint类作为兼容层。
- 同时运行8fc和新Module后择一输出。
- 用“重构顺便优化”修改Swing、Landing、Sliding、Release或Pelvis。
- 为保持旧逻辑继续让Runtime编排内部步骤或逐模块Seal。
- 把Sliding继续建成顶层生命周期状态。
- 让Primary Support、Pelvis或Goal读取Foot State、Lock Response或Context内部字段。
- 把全部Foot、Pelvis和Solver数学集中进根Runtime。
- 在正式类型名、配置或运行分支中写入8fc commit identity。

## Migration

1. 建立根Bank与新Module数据合同，使工程允许暂时编译失败但不建立第二运行路径。
2. 把Landing Lifecycle和Effective Constraint全部状态迁入Foot Context并按确定映射生成新状态。
3. 原顺序迁入Prediction、Ground Path、Swing与Contact公式，并由State Machine生成紧凑Resolved Result和Support Eligibility。
4. 让Primary Support、Pelvis和Goal只消费Resolved字段，接入唯一Assembler、FBBIK和Writer。
5. 切换唯一调用点并删除旧类、旧字段、旧Seal和兼容Goal路径。
6. 重写Diagnostics并对账逐帧基线事实。
7. 编译和严格校验后由用户端到端确认8fc等价；只有归档后，后续行为change才可实施。

## RENAMED Requirements

- FROM: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`
- TO: `### Requirement: Foot Placement必须发布完整预测IK的步伐盆骨与双脚Goal`

## MODIFIED Requirements

### Requirement: Landing Prediction必须形成独立世界事实

每只脚 MUST先处理事件完成与查询事件选择，再执行世界查询。Current Step已完成且identity等于该脚Cached Accepted `NextSwingLanding`时，Runtime MUST把该Accepted事实原值晋级为`LastLanding`，包括点、法线、SurfaceIdentity、Landing Event identity与Accepted trajectory revision identity；完成帧 MUST不得为晋级重新查询。随后 Runtime MUST在未完成且identity不同于LastLanding的权威Current/Incoming PreSwing或Swing header中选择`TimeToLandingSeconds`较小者，相等时稳定选择Current；每脚每个表现帧 MUST只为该唯一事件执行零次或一次正式Landing SphereCast，不得先查询Current与Incoming再择优，也不得在选中事件失败后查询另一事件作为fallback。

Landing Prediction MUST按`Selected Step -> committed Body Target世界速度 + Timeline段边界/Continuation -> KCC Future Body Translation -> Foot Placement trajectory revision Pose -> Raw Landing -> Future Landing SphereCast -> Accepted/Rejected Landing`执行。有效Pivot时：

```text
VisiblePositionForRevision = CommittedRevisionPosition
                           + (CurrentVisiblePosition - CommittedVisiblePositionAtCommit)
VirtualBodyPosition = LastLanding
                    + RotateAroundUp(VisiblePositionForRevision - LastLanding, PivotDelta)
VirtualBodyRotation = PivotRotation * CommittedRevisionRotation
RevisionPosition = VirtualBodyPosition
RevisionRotation = VirtualBodyRotation
RawLanding = VirtualBodyPosition
           + FutureBodyTranslationWorld
           + VirtualBodyRotation * RootLocalLanding
```

`CurrentVisiblePosition/Rotation` MUST来自尚未被Foot Placement改写的当前Component Pose；`VisiblePositionForRevision = CommittedRevisionPosition + (CurrentVisiblePosition - CommittedVisiblePositionAtCommit)`，只表示上一提交虚体沿当前可见世界位移推进后的临时Pose。没有有效Pivot时Revision Position/Rotation MUST对齐本帧Visible Position/Rotation。`RevisionPosition`与`RevisionRotation`只属于Foot Placement Pending/Committed状态，不是Gameplay或KCC未来朝向Plan。Future Body Translation MUST保持KCC在原世界空间积分和裁剪的向量，不得随Pivot yaw、输入方向、速度方向或Body方向再次旋转。RootLocalLanding MUST只乘本帧受角限后的Virtual Body Rotation；系统 MUST不把瞬时Yaw Velocity外推到Landing时刻，不得旋转旧Route、Surface、Hull或Envelope冒充新查询结果。

SphereCast MUST从Raw Landing上方沿Component Down使用Profile声明的半径和有限距离查询。查询 MUST过滤自身Collider、初始重叠、非法点、非法法线与超坡度命中，并在固定容量返回集合中按距离和稳定identity选择最近合法命中。没有合法命中时 MUST发布`GroundQueryMissed`，不得创建默认Surface或沿用旧revision Path。

#### Scenario: Current与Incoming同时可预测

- **WHEN** 同一只脚的Current与Incoming header都满足预查询候选合同
- **THEN** Runtime MUST在查询前选择TimeToLandingSeconds较小的事件，相等时选择Current
- **AND** 本帧该脚正式Landing SphereCast次数 MUST为一

#### Scenario: 完成事件晋级LastLanding

- **WHEN** Current Step完成且identity等于Cached Accepted NextSwingLanding
- **THEN** Runtime MUST把最后Accepted点、法线、Surface与revision identity原值晋级为LastLanding
- **AND** MUST不使用完成帧新查询、Animated Sole或默认点替换它

#### Scenario: Future Landing命中

- **WHEN** Selected Step的SphereCast返回合法Surface
- **THEN** diagnostics MUST发布唯一Accepted Landing、独立trajectory revision identity、Surface identity、点、法线与实际查询距离

#### Scenario: Landing输入不可用

- **WHEN** Selected Step、Motion Timeline、Body Target、Future Body Translation、revision Pose或合法Surface不可用
- **THEN** 该脚 MUST发布明确Rejected原因
- **AND** MUST不查询另一Step、沿用旧Landing或生成替代落点

### Requirement: Foot Placement必须发布完整预测IK的步伐盆骨与双脚Goal

Foot Placement MUST在同一Pending Frame内按固定顺序发布最多三个有效Goal：先从Committed逐字段初始化Landing、Ground Path、trajectory revision、锁脚、释放与骨盆弹簧Pending状态，再解析同帧正式输入；完成事件晋级并选择每脚唯一查询Step后，建立必要的trajectory revision Pose，执行唯一Landing查询并更新同事件落点，消费该revision后的Ground Envelope，再判定当前步伐，最后计算摆动脚包络增量、支撑脚接地与锁脚、盆骨平移和支撑脚朝向。输出仍 MUST是Pelvis、LeftFoot、RightFoot三个slot。有效转向 MUST重新执行唯一Landing SphereCast、Capsule、Reachability、Hull和Envelope，不得刚体旋转旧Route、Surface或Envelope。

有限Action占脚 MUST只从同帧`CharacterFootPlacementPoseInput.Contributions`解析：Contribution必须为Live、`SourceId.SourceActionInstanceId != 0`且对应`LeftFootWeight`或`RightFootWeight`大于几何容差。Captured/Stored、普通Locomotion或零脚权重Contribution MUST不创建Action占用。Grounded MUST读取`CharacterPresentationFactFrame.Grounded`，跑步朝向阈值 MUST读取同帧`CharacterPresentationFactFrame.HorizontalSpeed`；Runtime MUST不查询第二Action状态、从Step猜速度或从Transform差分重建这些输入。

摆动脚合同：Current Step权威且处于Swing、Landing Event与NextSwingLanding一致、Ground Path全部Edge可达且Accepted、Envelope端点合法且垂直增量大于几何容差时，MUST沿`Component Up`把原生动画Ankle抬高`Envelope Sample - Baseline Sample`。MUST保留动画水平进度和摆动脚旋转。同一帧两脚都满足该合同时，MUST只保留垂直增量更大的一只作为摆动脚。

支撑脚在Grounded、拥有LastLanding、不是当前摆动脚且未被有限Action占用时，MUST先按`plantHeight = max(0, dot(LastLanding - originalSole, ComponentUp))`把Sole和Ankle沿`Component Up`落到不低于LastLanding的高度，再按水平误差进入Locked、Sliding或Unlocked。Locked/Sliding的Sole水平位置 MUST使用LastLanding方向的水平偏移叠加到`plantedSole`，不得把`LastLanding + up * plantHeight`作为目标。Locked/Sliding的Ankle目标 MUST保留同一帧原生`originalAnkle - originalSole`偏移。Pelvis MUST使用现有`PelvisPreSolveTranslation`：步伐起点是支撑脚LastLanding，终点是revision后的摆动脚NextSwingLanding；主目标按Pose Root在步伐水平轴上的进度采样；上坡在支撑落地后抬升，下坡在支撑仍接触时下降；支撑切换时旧步伐相对高度 MUST先按旧起点到新起点重基，Pending弹簧 MUST从Committed输出、速度和旧起点初始化，再用Profile频率执行闭式临界阻尼积分。必要地面升高 MUST一次加上，诊断分解不得重复加到最终Goal。双脚垂直目标确定后，若盆骨相对更低修正脚的净空低于同帧原生动画净空，MUST把差值补进盆骨平移。

摆动脚有效Position Weight只有包络垂直增量超过几何容差时才可使用同帧`animation.foot-placement-weight`；Pelvis只有最终平移超过几何容差时才可非零。有效Locked/Sliding支撑脚即使plantHeight和水平误差为零，Position Weight仍 MUST 等于同帧`animation.foot-placement-weight`，以持续维护锁脚；Unlocked才按正式解锁时间降到零。摆动脚Rotation Weight MUST为零。支撑脚Rotation Weight MUST只在朝向合同成立时非零。缺少步伐两端时，Pelvis和摆动路径权重 MUST为零，但有效GroundedStationary支撑脚仍可复用支撑锁脚合同；缺少LastLanding、Ground Path Rejected或无效支撑合同时，对应Goal MUST发布原生事实和零权重。系统 MUST不叠加预测误差权重、摆动相位、Set Mesh、VisualRoot写入、Gameplay Body写入或第二Grounding查询。

#### Scenario: 上楼时一只脚摆动一只脚支撑

- **WHEN** 右脚是权威Swing且拥有Accepted Ground Envelope，左脚拥有LastLanding且不是摆动脚
- **THEN** 右脚 MUST发布包络垂直增量Goal
- **AND** 左脚 MUST发布不低于LastLanding的支撑Goal
- **AND** Pelvis MUST发布沿Component Up指向当前revision步伐采样高度的`PelvisPreSolveTranslation`

#### Scenario: 双 Swing 只收敛为一个摆动脚

- **WHEN** 两脚同时是权威Swing且两脚都有可用的包络垂直增量
- **THEN** Runtime MUST只选择垂直增量较大的一脚作为当前摆动脚
- **AND** 另一脚只有拥有LastLanding且未被有限Action占用时才进入支撑合同，否则 MUST发布原生事实和零权重

#### Scenario: GroundedStationary 两脚保持锁脚

- **WHEN** 两脚都非Swing、各自拥有有效LastLanding且未被有限Action占用
- **THEN** Pelvis和摆动Envelope权重 MUST为零
- **AND** 两脚 MUST继续按同一支撑接地与Locked/Sliding合同发布位置Goal
- **AND** Runtime MUST延续仍合法的上一提交Pivot主支撑；仅在其失效时按较小水平误差和稳定Side顺序重选
- **AND** Pivot主脚 MUST保持Locked，另一脚 MUST重新判定Locked、Sliding或Unlocked

#### Scenario: 平地锁脚仍保持位置权重

- **WHEN** 支撑脚拥有LastLanding、处于Locked、plantHeight为零且水平误差为零
- **THEN** 该脚 MUST继续发布等于同帧动画位置权重的Locked Position Goal
- **AND** 该脚 MUST不因没有垂直修正而退回原生动画所有权

#### Scenario: 没有完整步伐

- **WHEN** 两脚都没有LastLanding、Step identity不一致、或没有唯一可用的摆动脚且不满足GroundedStationary
- **THEN** Pelvis与锁脚Goal权重 MUST为零
- **AND** 若其中一脚自身仍满足摆动包络合同，该脚 MUST继续只发布包络垂直Goal

### Requirement: Ground Path必须使用上一已提交落点与下一事件落点

每只脚 MUST按Landing Event identity缓存Accepted Landing。PreSwing或Swing阶段的每个有效表现帧 MUST执行一次且仅一次正式Landing SphereCast。同一事件的后续权威Accepted结果 MUST只有在`SurfaceIdentity`相同且沿Component Up的高度差不超过Profile显式`MaximumSameEventVerticalJump`时更新NextSwingLanding。任一条件不满足都算换级。更新距离小于正式Profile死区时 MUST保留原落点并复用Ground Path，但 MUST不停止下一表现帧的正式Landing预测。换级时 MUST丢弃该命中、保留本事件已接受落点并发布Warning，MUST不把新踏面写进Ground Path。该事件实际落地后最新NextSwingLanding MUST晋级为LastLanding，之后才为新的Swing事件建立下一落点。

上述“一次” MUST指查询前已经选中的唯一Current或Incoming事件；没有候选时为零次，不能理解成Current与Incoming各一次。NextSwingLanding MUST记录产生它的独立Accepted trajectory revision identity。事件完成时 MUST把缓存Accepted事实原值晋级，完成帧查询结果不得覆盖点、法线、Surface或revision identity。

Ground Path MUST只使用LastLanding与revision后的NextSwingLanding构造查询输入。没有LastLanding时 MUST发布`CurrentLandingUnavailable`；不得用Animated Sole、Transform、固定高度或默认地面补起点。

#### Scenario: 同一Landing Event持续多个表现帧且同踏面未变

- **WHEN** NextSwingLanding Event identity没有变化、SurfaceIdentity相同、高度差未超过`MaximumSameEventVerticalJump`且新的Accepted Landing移动超过更新死区
- **THEN** Runtime MUST提交新的NextSwingLanding并重建同一Foot Placement事务中的Ground Path
- **AND** Ground Path重建 MUST消费该表现帧已经产生的唯一SphereCast结果，不得为重建再执行第二次Landing查询

#### Scenario: 同一Landing Event的小幅预测误差

- **WHEN** 新的Accepted Landing与缓存点的距离小于正式更新死区
- **THEN** Runtime MUST复用缓存落点与Committed Ground Path
- **AND** MUST继续执行下一表现帧的唯一Landing预测，但不得因毫米级误差触发新的Capsule Ground Detection

#### Scenario: 同一Landing Event换级

- **WHEN** 新的Accepted Landing属于不同SurfaceIdentity，或沿Component Up的高度差大于`MaximumSameEventVerticalJump`
- **THEN** Runtime MUST保留本事件已接受的NextSwingLanding
- **AND** MUST发布Warning
- **AND** MUST不重建指向新踏面的Ground Path

#### Scenario: 下一Swing Event完成

- **WHEN** NextSwingLanding对应的事件成为已完成Swing Event
- **THEN** Runtime MUST把该Accepted Landing晋级为新的LastLanding
- **AND** 晋级后的点、法线、Surface与revision identity MUST逐值等于完成前最后Accepted NextSwingLanding
- **AND** MUST只为新的PreSwing或Swing Event建立新的NextSwingLanding

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST保留上一已提交Accepted Landing、下一Landing Event的Cached Accepted Landing、左右脚Ground Envelope和上游Invalid Segment，并显示当前摆动脚Original/Corrected Sole、支撑脚Original/Planted Sole、步伐起点到终点的细线、盆骨目标标记、锁脚状态对应的脚标记以及支撑脚朝向短法线。标记不得使用文字。Active Swing或换级被拒绝 MUST继续在对应Sole显示红色线框。

只读摘要与CSV MUST记录Selected Query Step、每脚查询次数、尝试与Accepted trajectory revision identity、Current Visible Position/Rotation、Virtual Body Position/Rotation、revision Position/Rotation/Forward、visible/applied/residual yaw、Pivot主支撑Side/Event、ActionInstance与左右脚占用权重、Fact HorizontalSpeed、事件SurfaceIdentity、晋级来源Accepted revision、步伐支撑侧、起止点、progress、上下坡判定、盆骨重基前后target、spring input/output/velocity、支撑脚plantDelta、锁脚状态、水平误差、锁入准备、释放起点修正/权重/剩余时间和朝向权重，以及既有Swing Foot Motion字段。CSV MUST另外记录final writer之后的物理盆骨、物理脚踝、写入Completion identity及相对Goal残差。Diagnostics与Gizmo MUST不重新采样动画、查询世界、计算Reachability、采样Envelope或执行FBBIK。

最终骨骼消费仍 MUST通过同帧FootPlacement Goal Target Watch与FullBodyIK Pose Watch验证；两者 MUST具有相同Frame、Completion和Rig lineage。用户 MUST不从Scene Gizmo或身体弹跳单独推断盆骨或脚已经写入Physical Bones。

#### Scenario: 查看有效步伐盆骨

- **WHEN** 用户查看最近一次成功Seal且Pelvis Position Weight大于零的诊断
- **THEN** 步伐细线端点 MUST等于该帧支撑脚LastLanding与revision后的摆动脚NextSwingLanding
- **AND** CSV中的最终Pelvis Goal、权重和物理盆骨残差 MUST来自同一Completion

#### Scenario: 查看换级被拒绝的下一落点

- **WHEN** 同一Landing Event的新查询命中了不同SurfaceIdentity或高度超过阈值
- **THEN** Scene诊断 MUST继续显示本事件已接受的下一落点
- **AND** CSV MUST记录Warning对应的typed原因且不得把新踏面写成Accepted NextSwingLanding

## ADDED Requirements

### Requirement: 支撑脚必须按水平误差进入Locked Sliding或Unlocked

拥有LastLanding且不是当前摆动脚的脚 MUST计算`horizontalError = |ProjectOnPlane(LastLanding - originalSole, ComponentUp)|`。该距离小于等于Profile `LockDistance`时 MUST为Locked：`lockedSole = plantedSole + ProjectOnPlane(LastLanding - originalSole, ComponentUp)`，水平位置对齐LastLanding而垂直位置仍等于plantedSole；该距离大于`LockDistance`且小于等于`SlideDistance`时 MUST为Sliding：水平位置按`slideT = 1 - (horizontalError - LockDistance) / (SlideDistance - LockDistance)`在LastLanding方向和原生动画之间插值，垂直位置仍使用非负plantHeight。

第一次接受Landing Event的NextSwingLanding时 MUST冻结`LockPreparationStartTimeToLandingSeconds`。同事件准备权重 MUST为`max(committedWeight, saturate(1 - TimeToLandingSeconds / StartTimeToLandingSeconds))`，只在Seal后单调增加；Swing期间只准备交接，不发布第二Goal。事件完成时准备权重 MUST为1。Locked/Sliding Position Weight MUST为`animation.foot-placement-weight * LockPreparationWeight`，因此正常完成后即使plantHeight与水平误差为零也等于同帧动画权重。系统 MUST不新增Lock Duration、Lock Curve或按表现delta独立推进的锁入时钟。

该距离大于`SlideDistance`且上一Committed状态为Locked/Sliding时 MUST进入Unlocked连续释放：首帧冻结`previousCommittedTargetAnkle - currentOriginalAnkle`与上一提交Position Weight，并以完整`UnlockBlendSeconds`输出与上一Goal相同的目标和权重；后续帧只从Committed remaining计算`pendingRemaining = max(0, committedRemaining - deltaSeconds)`，目标为`currentOriginalAnkle + frozenCorrection`，权重为`frozenWeight * pendingRemaining / UnlockBlendSeconds`。remaining只有Seal后递减；Discard不得消耗。归零时目标 MUST回到当帧Original、权重为零并清除释放状态。Locked或Sliding期间该脚 MUST不再采样Ground Envelope，也 MUST不追新的NextSwingLanding。空中、Fact Grounded为false、无权威Step或有限Action占用该脚时 MUST立即发布原生事实和零权重，不得用Unlocked继续携带旧世界锚。Profile MUST显式给出有限正数`LockDistance`、`SlideDistance`与`UnlockBlendSeconds`，且`LandingUpdateDistance < LockDistance < SlideDistance`。系统 MUST不引入第二套脚下Trace或独立传统IK。

#### Scenario: 支撑脚几乎踩在LastLanding上

- **WHEN** 非摆动脚拥有LastLanding且水平误差不超过LockDistance
- **THEN** 该脚 MUST发布Locked位置Goal
- **AND** MUST保持同帧动画位置权重，即使plantHeight为零
- **AND** MUST允许旋转自由度，不得把Pitch和Roll锁死为动画原值或完全贴法线

#### Scenario: 支撑脚离LastLanding过远

- **WHEN** 非摆动脚拥有LastLanding且水平误差超过SlideDistance
- **THEN** 进入Unlocked首帧的目标与权重 MUST连续等于上一Committed Locked或Sliding Goal
- **AND** 后续位置权重 MUST在UnlockBlendSeconds内单调降到零
- **AND** MUST不重新追踪过远LastLanding或一帧切回Original

### Requirement: 支撑脚朝向必须受坡度与跑步关闭约束

Locked或Sliding的支撑脚 MAY发布非零Rotation Weight。移动步伐的目标旋转 MUST由落点法线与revision后步伐前进方向构造；GroundedStationary没有步伐时 MUST使用同一Pending trajectory revision的`RevisionForward`。前进方向投影退化、法线无效或两种正式前向都不可用时Rotation Weight MUST为零。上坡Pitch MUST比完全贴面更靠近水平，下坡Pitch MUST更靠近落点法线，Pitch与Roll绝对值 MUST不超过Profile显式`MaximumPitchDegrees`与`MaximumRollDegrees`。`UphillLevelBlend`、`DownhillSlopeBlend` MUST是(0,1]的正式Profile值。`CharacterPresentationFactFrame.HorizontalSpeed >= OrientationRunSpeed`时朝向 MUST关闭且Rotation Weight MUST为零。摆动脚Rotation Weight MUST保持为零。

#### Scenario: 上坡慢走的支撑脚

- **WHEN** 支撑脚处于Locked、正式前向与落点法线有效且同帧HorizontalSpeed未达到跑步关闭阈值
- **THEN** 该脚 MUST发布受角限约束的旋转Goal
- **AND** 目标Pitch MUST不等于完全沿落点法线躺平

#### Scenario: 坡面GroundedStationary没有步伐

- **WHEN** 角色在坡面站住、支撑脚处于Locked或Sliding、当前没有有效Stride Forward且同一Pending RevisionForward与落点法线有效
- **THEN** 朝向 MUST使用同一Pending trajectory revision的RevisionForward与该脚LastLanding法线
- **AND** MUST不因Stride为空突然回到原生旋转，也 MUST不猜测第二前向

#### Scenario: 跑步关闭朝向

- **WHEN** 同帧`CharacterPresentationFactFrame.HorizontalSpeed`达到Profile跑步关闭阈值
- **THEN** 左右脚Rotation Weight MUST为零
- **AND** MUST不把坡面法线写进脚踝旋转

### Requirement: 转向时必须建立唯一Foot Placement trajectory revision

Foot Placement MUST拥有左右脚共享的唯一trajectory revision Pending/Committed状态，至少包含独立单调identity、Visible Position/Rotation/Forward at Commit、Revision Position/Rotation/Forward、Residual Yaw、Pivot Support Side与Pivot Support Landing Event。该identity MUST不复用Future Body TrajectoryGeneration、Timeline tick、Landing Event或Render Frame。Pending MUST从Committed逐字段初始化，只有Seal提交；Discard完整恢复。

同一主支撑连续帧 MUST先以`VisiblePositionForRevision = committedRevisionPosition + (currentVisiblePosition - committedVisiblePositionAtCommit)`推进上一提交虚体，再用`requestedYaw = committedResidualYaw + SignedAngle(committedVisibleForward, currentVisibleForward, ComponentUp)`得到待消化yaw，按`MaximumPivotYawDeltaDegrees`限制为`pivotDelta`。Revision Position MUST逐值等于`LastLanding + RotateAroundUp(VisiblePositionForRevision - LastLanding, pivotDelta)`；Revision Rotation MUST逐值等于`PivotRotation * committedRevisionRotation`；Residual Yaw MUST保存`requestedYaw - pivotDelta`。Raw Landing MUST使用这份Virtual Body Position/Rotation与未旋转的世界Future Body Translation重新计算。Committed Revision只提供虚体Pose连续性，旧Route、Surface和Ground Envelope MUST不参与本帧新查询。摆动脚的NextSwingLanding与Envelope不得通过刚体旋转旧Route、Surface或Ground Envelope伪造。

所有双支撑帧、尤其GroundedStationary，MUST延续仍合法且Locked的上一提交主支撑；仅在其失效时从Locked候选中按较小水平误差、再按稳定Side顺序重选。Sole前后位置、当前yaw符号和每帧法线不得作为主条件。Pivot主脚 MUST继续锁在自身LastLanding，另一脚 MUST重新判定Locked/Sliding/Unlocked。前后可见方向退化、没有合法Locked主支撑或revision输入非法时本帧不得猜方向。系统 MUST不写VisualRoot、MUST不修改KCC朝向、MUST不把Gameplay胶囊绕支撑脚旋转。

#### Scenario: 支撑脚锁定时角色转向

- **WHEN** 左脚Locked且本帧可见yaw发生变化
- **THEN** Runtime MUST建立新的Foot Placement trajectory revision并重新查询右脚未来落点和Ground Path
- **AND** 右脚摆动目标 MUST来自revision后的Accepted Envelope
- **AND** 左脚位置Goal MUST保持在LastLanding合同内
- **AND** VisualRoot与Gameplay Body朝向 MUST不由Foot Placement改写

#### Scenario: GroundedStationary连续转向

- **WHEN** 两脚都有LastLanding且上一提交Pivot主脚仍处于Locked
- **THEN** Runtime MUST连续使用同一主支撑Side与Landing Event，不得左右帧交替
- **AND** 另一脚 MUST根据自身水平误差进入Locked、Sliding或Unlocked
- **AND** 超过单帧角限的yaw MUST保存在Residual Yaw并由后续成功Seal帧继续消化

### Requirement: 步伐盆骨必须由唯一Foot Placement事务拥有

步伐判定、盆骨平移、锁脚准备/释放、朝向、trajectory revision和弹簧状态 MUST只存在于Foot Placement Pending/Committed页，并随外层表现事务`Seal`或`Discard`。每次Prepare MUST先从Committed逐字段初始化全部Pending页，不得Clear后从零历史计算。纯计算模块 MUST不引用Physics、FinalIK、Transform或Editor类型。FinalIK MUST继续只把`PelvisPreSolveTranslation`加到当前Component Pose盆骨，不得选择支撑腿或采样步伐。KCC、`character-vertical-body-motion`与VisualRoot MUST不消费该平移。

Discard或Animation Runtime Fault MUST恢复上一提交弹簧、trajectory revision和锁脚状态；MUST不留下半帧盆骨或锁脚历史，也 MUST不用上一帧目标填补本帧失败步伐。

#### Scenario: 外层事务丢弃Pending Frame

- **WHEN** Foot Placement已算出非零盆骨平移或Locked目标但外层事务Discard
- **THEN** Committed弹簧、trajectory revision、锁脚准备/释放与三Goal MUST回到Discard前状态
- **AND** 下一帧 MUST不得读到被丢弃的盆骨或锁脚目标

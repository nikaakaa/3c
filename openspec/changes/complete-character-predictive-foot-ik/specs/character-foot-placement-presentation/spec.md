## RENAMED Requirements

- FROM: `### Requirement: 当前阶段必须只生成Swing脚垂直Goal`
- TO: `### Requirement: Foot Placement必须发布完整预测IK的步伐盆骨与双脚Goal`

## MODIFIED Requirements

### Requirement: Foot Placement必须发布完整预测IK的步伐盆骨与双脚Goal

Foot Placement MUST在同一Pending Frame内按固定顺序发布最多三个有效Goal：先读取可见yaw并建立必要的唯一trajectory revision，再更新同事件落点并消费该revision后的Ground Envelope，再判定当前步伐，最后计算摆动脚包络增量、支撑脚接地与锁脚、盆骨平移和支撑脚朝向。输出仍 MUST是Pelvis、LeftFoot、RightFoot三个slot。有效转向 MUST重新执行唯一Landing SphereCast、Capsule、Reachability、Hull和Envelope，不得刚体旋转旧Route、Surface或Envelope。

摆动脚合同：Current Step权威且处于Swing、Landing Event与NextSwingLanding一致、Ground Path全部Edge可达且Accepted、Envelope端点合法且垂直增量大于几何容差时，MUST沿`Component Up`把原生动画Ankle抬高`Envelope Sample - Baseline Sample`。MUST保留动画水平进度和摆动脚旋转。同一帧两脚都满足该合同时，MUST只保留垂直增量更大的一只作为摆动脚。

支撑脚在拥有LastLanding且不是当前摆动脚时，MUST先按`plantHeight = max(0, dot(LastLanding - originalSole, ComponentUp))`把Sole和Ankle沿`Component Up`落到不低于LastLanding的高度，再按水平误差进入Locked、Sliding或Unlocked。Locked/Sliding的Sole水平位置 MUST 使用LastLanding方向的水平偏移叠加到`plantedSole`，不得把`LastLanding + up * plantHeight`作为目标。Locked/Sliding的Ankle目标 MUST 保留同一帧原生`originalAnkle - originalSole`偏移。Pelvis MUST使用现有`PelvisPreSolveTranslation`：步伐起点是支撑脚LastLanding，终点是revision后的摆动脚NextSwingLanding；主目标按Pose Root在步伐水平轴上的进度采样；上坡在支撑落地后抬升，下坡在支撑仍接触时下降；支撑切换时旧步伐相对高度 MUST 先按旧起点到新起点重基，之后才由Profile声明的临界阻尼弹簧消化，必要地面升高MUST一次加上，且诊断分解不得重复加到最终Goal。双脚垂直目标确定后，若盆骨相对更低修正脚的净空低于同帧原生动画净空，MUST把差值补进盆骨平移。

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
- **AND** MUST只为新的PreSwing或Swing Event建立新的NextSwingLanding

### Requirement: Foot Placement诊断必须只显示当前事实

Scene诊断 MUST保留上一已提交Accepted Landing、下一Landing Event的Cached Accepted Landing、左右脚Ground Envelope和上游Invalid Segment，并显示当前摆动脚Original/Corrected Sole、支撑脚Original/Planted Sole、步伐起点到终点的细线、盆骨目标标记、锁脚状态对应的脚标记以及支撑脚朝向短法线。标记不得使用文字。Active Swing或换级被拒绝 MUST继续在对应Sole显示红色线框。

只读摘要与CSV MUST记录事件SurfaceIdentity、trajectory revision、步伐支撑侧、起止点、progress、上下坡判定、盆骨平移与弹簧、支撑脚plantDelta、锁脚状态、水平误差、锁入/解锁剩余时间和朝向权重，以及既有Swing Foot Motion字段。CSV MUST另外记录final writer之后的物理盆骨、物理脚踝、写入Completion identity及相对Goal残差。Diagnostics与Gizmo MUST不重新采样动画、查询世界、计算Reachability、采样Envelope或执行FBBIK。

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

拥有LastLanding且不是当前摆动脚的脚 MUST计算`horizontalError = |ProjectOnPlane(LastLanding - originalSole, ComponentUp)|`。该距离小于等于Profile `LockDistance`时 MUST为Locked：`lockedSole = plantedSole + ProjectOnPlane(LastLanding - originalSole, ComponentUp)`，水平位置对齐LastLanding而垂直位置仍等于plantedSole；即使plantHeight和水平误差为零，Position Weight仍 MUST 等于同帧`animation.foot-placement-weight`。该距离大于`LockDistance`且小于等于`SlideDistance`时 MUST为Sliding：水平位置按`slideT = 1 - (horizontalError - LockDistance) / (SlideDistance - LockDistance)`在LastLanding方向和原生动画之间插值，垂直位置仍使用非负plantHeight，Position Weight仍 MUST 等于同帧动画位置权重。该距离大于`SlideDistance`时 MUST为Unlocked：位置回到原生动画，位置权重在Profile显式`UnlockBlendSeconds`内降到零。从摆动进入Locked的混合时间 MUST使用该脚事件的`TimeToLandingSeconds`时基，并记录事件 identity，不能新增第二条锁入曲线。Locked或Sliding期间该脚 MUST不再采样Ground Envelope，也 MUST不追新的NextSwingLanding。空中、无权威Step或有限Action占用该脚时 MUST不进入这三态且位置权重为零。Profile MUST显式给出有限正数`LockDistance`、`SlideDistance`与`UnlockBlendSeconds`，且`LandingUpdateDistance < LockDistance < SlideDistance`。系统 MUST不引入第二套脚下Trace或独立传统IK。

#### Scenario: 支撑脚几乎踩在LastLanding上

- **WHEN** 非摆动脚拥有LastLanding且水平误差不超过LockDistance
- **THEN** 该脚 MUST发布Locked位置Goal
- **AND** MUST保持同帧动画位置权重，即使plantHeight为零
- **AND** MUST允许旋转自由度，不得把Pitch和Roll锁死为动画原值或完全贴法线

#### Scenario: 支撑脚离LastLanding过远

- **WHEN** 非摆动脚拥有LastLanding且水平误差超过SlideDistance
- **THEN** 该脚 MUST发布Unlocked零位置权重Goal
- **AND** MUST不把脚踝目标继续钉在过远的旧落点上

### Requirement: 支撑脚朝向必须受坡度与跑步关闭约束

Locked或Sliding的支撑脚 MAY发布非零Rotation Weight。目标旋转 MUST由落点法线与步伐前进方向构造；前进方向投影退化、法线无效或步伐不成立时Rotation Weight MUST为零。上坡Pitch MUST比完全贴面更靠近水平，下坡Pitch MUST更靠近落点法线，Pitch与Roll绝对值 MUST不超过Profile显式`MaximumPitchDegrees`与`MaximumRollDegrees`。`UphillLevelBlend`、`DownhillSlopeBlend` MUST是(0,1]的正式Profile值。当前权威Step达到Profile显式`OrientationRunSpeed`跑步阈值时，朝向 MUST关闭且Rotation Weight MUST为零。摆动脚Rotation Weight MUST保持为零。

#### Scenario: 上坡慢走的支撑脚

- **WHEN** 支撑脚处于Locked且未达到跑步关闭阈值
- **THEN** 该脚 MUST发布受角限约束的旋转Goal
- **AND** 目标Pitch MUST不等于完全沿落点法线躺平

#### Scenario: 跑步关闭朝向

- **WHEN** 当前权威Step达到Profile跑步关闭阈值
- **THEN** 左右脚Rotation Weight MUST为零
- **AND** MUST不把坡面法线写进脚踝旋转

### Requirement: 转向时必须建立唯一Foot Placement trajectory revision

当本帧相对上一提交帧存在可见yaw增量且存在有效支撑LastLanding时，Foot Placement MUST以该支撑落点为Pivot输入建立唯一trajectory revision，并用revision后的Future Body预测执行本表现帧唯一Landing SphereCast、Capsule、Reachability、Hull和Envelope。摆动脚的NextSwingLanding与Envelope不得通过刚体旋转旧Route、Surface或Ground Envelope伪造。Pivot yaw MUST受Profile显式`MaximumPivotYawDeltaDegrees`限制，超过部分留给后续表现帧。前后可见方向退化或支撑落点无效时本帧不得猜方向。系统 MUST不写VisualRoot、MUST不修改KCC朝向、MUST不把Gameplay胶囊绕支撑脚旋转。

#### Scenario: 支撑脚锁定时角色转向

- **WHEN** 左脚Locked且本帧可见yaw发生变化
- **THEN** Runtime MUST建立新的Foot Placement trajectory revision并重新查询右脚未来落点和Ground Path
- **AND** 右脚摆动目标 MUST来自revision后的Accepted Envelope
- **AND** 左脚位置Goal MUST保持在LastLanding合同内
- **AND** VisualRoot与Gameplay Body朝向 MUST不由Foot Placement改写

### Requirement: 步伐盆骨必须由唯一Foot Placement事务拥有

步伐判定、盆骨平移、锁脚状态、朝向、trajectory revision和弹簧状态 MUST只存在于Foot Placement Pending/Committed页，并随外层表现事务`Seal`或`Discard`。纯计算模块 MUST不引用Physics、FinalIK、Transform或Editor类型。FinalIK MUST继续只把`PelvisPreSolveTranslation`加到当前Component Pose盆骨，不得选择支撑腿或采样步伐。KCC、`character-vertical-body-motion`与VisualRoot MUST不消费该平移。

Discard或Animation Runtime Fault MUST恢复上一提交弹簧、trajectory revision和锁脚状态；MUST不留下半帧盆骨或锁脚历史，也 MUST不用上一帧目标填补本帧失败步伐。

#### Scenario: 外层事务丢弃Pending Frame

- **WHEN** Foot Placement已算出非零盆骨平移或Locked目标但外层事务Discard
- **THEN** Committed弹簧、trajectory revision、锁脚与三Goal MUST回到Discard前状态
- **AND** 下一帧 MUST不得读到被丢弃的盆骨或锁脚目标
